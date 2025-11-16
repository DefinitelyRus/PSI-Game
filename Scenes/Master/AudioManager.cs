using Godot;
namespace CommonScripts;

public partial class AudioManager : Node2D {
	
	#region Instance Members

	#region Volumes

	[Export] public float UniversalVolume = 1.0f;

	[Export] public float SFXVolume = 1.0f;

	[Export] public float MusicVolume = 1.0f;

	#endregion

	#region Nodes & Components

	public AudioStreamPlayer MusicPlayer { get; private set; } = null!;

	#endregion

	#region Debugging

	[Export] public bool LogReady = true;
	[Export] public bool LogPlayback = false;

	#endregion

	#region Godot Callbacks

	public override void _EnterTree() {
		Log.Me(() => "A ControlSurface has entered the tree. Checking properties...", LogReady, true);

		MusicPlayer = new();
		AddChild(MusicPlayer);

		Instance = this;

		Log.Me("Done!", true);
	}

	#endregion

	#endregion

	#region Static Members

	public static AudioManager Instance { get; private set; } = null!;

	#region Media Control

	public static AudioStreamPlayer PlaySFX(AudioStream stream, float volume = 1f) {
		AudioStreamPlayer sfxPlayer = new();
		Instance.AddChild(sfxPlayer);

		sfxPlayer.Stream = stream;
		sfxPlayer.VolumeLinear = volume * Instance.SFXVolume;
		sfxPlayer.Autoplay = false;
		sfxPlayer.Finished += () => sfxPlayer.QueueFree();

		sfxPlayer.Play();
		return sfxPlayer;
	}


	public static AudioStreamPlayer2D PlaySFX2D(AudioStream stream, Vector2 position, float volume = 1f) {
		AudioStreamPlayer2D sfxPlayer = new();
		Instance.AddChild(sfxPlayer);

		sfxPlayer.Stream = stream;
		sfxPlayer.VolumeLinear = volume * Instance.SFXVolume;
		sfxPlayer.Position = position;
		sfxPlayer.Autoplay = false;
		sfxPlayer.Finished += () => sfxPlayer.QueueFree();

		sfxPlayer.Play();
		return sfxPlayer;
	}


	public static void PlayMusic(AudioStream stream, float volume = 1f) {
		Instance.MusicPlayer.Stream = stream;
		Instance.MusicPlayer.VolumeLinear = volume * Instance.MusicVolume;
		Instance.MusicPlayer.Play();
	}


	public static void StopMusic() {
		Instance.MusicPlayer.Stop();
	}

	#endregion

	#endregion

}
