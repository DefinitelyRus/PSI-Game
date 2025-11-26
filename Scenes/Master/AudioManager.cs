using System;
using System.Collections.Generic;
using System.Threading.Channels;
using Godot;
namespace CommonScripts;

public partial class AudioManager : Node2D {
	
	#region Instance Members

	#region Volumes

	[Export] public float UniversalVolume = 0.6f;

	[Export] public float SFXVolume = 0.8f;

	[Export] public float MusicVolume = 1.0f;
	[Export] public float AmbientVolume = 1.0f;
	[Export] public float FadeOutSpeed = 1.0f;
	private bool _isFadingOut = false;

	#endregion

	#region Nodes & Components

	[ExportGroup("Nodes & Components")]
	[Export] public Godot.Collections.Dictionary<string, AudioStream> SFXLibrary { get; private set; } = [];

	public List<AudioStreamPlayer> AudioStreams { get; private set; } = [];

	#endregion

	#region Debugging

	[Export] public bool LogReady = true;
	[Export] public bool LogPlayback = false;

	#endregion

	#region Godot Callbacks

	public override void _EnterTree() {
		Log.Me(() => "A ControlSurface has entered the tree. Checking properties...", LogReady, true);

		Instance = this;

		Log.Me("Done!", true);
	}

	public override void _Process(double delta) {
		UpdateFadeOut(delta);
	}

	#endregion

	#endregion

	#region Static Members

	public static AudioManager Instance { get; private set; } = null!;
	public enum AudioChannels {
		Master,
		Music,
		SFX,
		Ambient
	}

	#region Media Control
	/// <summary>
	/// Searches the SFX Library for a sound effect by name and plays it.
	/// </summary>
	public static AudioStreamPlayer? StreamAudio(string sfxName, float volume = 1f) {
		if (!Instance.SFXLibrary.TryGetValue(sfxName, out AudioStream? stream)) {
			Log.Warn(() => $"SFX '{sfxName}' not found in SFX Library!", true, true);
			return null;
		}

		RandomNumberGenerator rng = new();
		int suffix = rng.RandiRange(0, 99999);
		string id = $"{sfxName}#{suffix:D5}";

		return StreamAudio(stream, id, volume);
	}


	/// <summary>
	/// Plays an audio stream.
	/// </summary>
	public static AudioStreamPlayer StreamAudio(AudioStream stream, string? uniqueName = null, float volume = 1f) {
		uniqueName ??= Guid.NewGuid().ToString();

		AudioStreamPlayer musicPlayer = new() {
			Name = uniqueName,
			Stream = stream,
			VolumeLinear = volume * Instance.MusicVolume,
			Autoplay = false
		};

		Instance.AudioStreams.Add(musicPlayer);
		Instance.AddChild(musicPlayer);

		musicPlayer.Finished += () => {
			Instance.AudioStreams.Remove(musicPlayer);
			musicPlayer.QueueFree();
		};

		musicPlayer.Play();
		return musicPlayer;
	}


	/// <summary>
	/// Plays an audio stream at a specific 2D position.
	/// </summary>
	public static AudioStreamPlayer2D StreamAudio2D(AudioStream stream, Vector2 position, AudioChannels channel, float volume = 1f) {

		AudioStreamPlayer2D sfxPlayer = new() {
			Stream = stream,
			Position = position,
			Autoplay = false,

			VolumeLinear = channel switch {
				AudioChannels.Master => volume * Instance.UniversalVolume,
				AudioChannels.Music => volume * Instance.UniversalVolume * Instance.MusicVolume,
				AudioChannels.SFX => volume * Instance.UniversalVolume * Instance.SFXVolume,
				AudioChannels.Ambient => volume * Instance.UniversalVolume * Instance.AmbientVolume,
				_ => volume * Instance.UniversalVolume
			},
		};

		sfxPlayer.Finished += sfxPlayer.QueueFree;

		Instance.AddChild(sfxPlayer);
		sfxPlayer.Play();

		return sfxPlayer;
	}


	/// <summary>
    /// Searches the SFX Library for a sound effect by name and plays it at a specific 2D position.
    /// </summary>
	public static AudioStreamPlayer2D? StreamAudio2D(string sfxName, Vector2 position, AudioChannels channel, float volume = 1f) {
		if (!Instance.SFXLibrary.TryGetValue(sfxName, out AudioStream? stream)) {
			Log.Warn(() => $"SFX '{sfxName}' not found in SFX Library!", true, true);
			return null;
		}

		return StreamAudio2D(stream, position, channel, volume);
	}


	/// <summary>
    /// Stops a currently playing audio stream by its unique name.
    /// </summary>
	public static void StopMusic(string uniqueName) {
		AudioStreamPlayer? player = Instance.AudioStreams.Find(stream => stream.Name == uniqueName);

		if (player == null) {
			Log.Warn(() => $"No audio stream with the name '{uniqueName}' is currently playing.");
			return;
		}

		player.Stop();
		Instance.AudioStreams.Remove(player);
		player.QueueFree();
	}


	public static void FadeInAudio() {
        
    }


	/// <summary>
	/// Fades out all currently playing audio streams.
	/// </summary>
	public static void FadeOutAudio() {
		Instance._isFadingOut = true;
	}


	private static List<AudioStreamPlayer> StreamsToRemove { get; set; } = [];
	

	/// <summary>
    /// Updates the fade-out effect for all audio streams.
    /// </summary>
	public static void UpdateFadeOut(double delta) {
		if (!Instance._isFadingOut) return;

		foreach (AudioStreamPlayer player in Instance.AudioStreams) {
			float decrement = (float) (Instance.FadeOutSpeed * delta);
			player.VolumeLinear = (float) Math.Clamp(player.VolumeLinear - decrement, 0, 1);

			if (player.VolumeLinear <= 0) {
				player.VolumeLinear = 0;

				player.Stop();
				StreamsToRemove.Add(player);
				player.QueueFree();
			}
		}

		foreach (AudioStreamPlayer player in StreamsToRemove) {
			Instance.AudioStreams.Remove(player);
		}
		
		StreamsToRemove.Clear();

		Instance._isFadingOut = Instance.AudioStreams.Count > 0;
	}

	#endregion

	#endregion

}
