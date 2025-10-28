using Godot;
namespace CommonScripts;

public partial class AudioManager : Node2D
{
	#region Universal

	[Export] public float UniversalVolume = 1.0f;

	#endregion

	#region Music

	public AudioStreamPlayer MusicPlayer { get; private set; } = null!;

	[Export] public float MusicVolume = 1.0f;


	public void PlayMusic(AudioStream stream, float volume = 0f) {
		MusicPlayer.Stream = stream;
		MusicPlayer.VolumeLinear = volume * MusicVolume;
		MusicPlayer.Play();
	}


	public void StopMusic() {
		MusicPlayer.Stop();
	}

	#endregion

	#region SFX

	[Export] public float SFXVolume = 1.0f;

	public AudioStreamPlayer PlaySFX(AudioStream stream, float volume = 0f) {
		AudioStreamPlayer sfxPlayer = new();
		AddChild(sfxPlayer);

		sfxPlayer.Stream = stream;
		sfxPlayer.VolumeLinear = volume * SFXVolume;
		sfxPlayer.Autoplay = false;
		sfxPlayer.Finished += () => sfxPlayer.QueueFree();

		sfxPlayer.Play();
		return sfxPlayer;
	}

	public AudioStreamPlayer2D PlaySFX2D(AudioStream stream, Vector2 position, float volume = 0f) {
		AudioStreamPlayer2D sfxPlayer = new();
		AddChild(sfxPlayer);

		sfxPlayer.Stream = stream;
		sfxPlayer.VolumeLinear = volume * SFXVolume;
		sfxPlayer.Position = position;
		sfxPlayer.Autoplay = false;
		sfxPlayer.Finished += () => sfxPlayer.QueueFree();

		sfxPlayer.Play();
		return sfxPlayer;
	}

	#endregion

	#region Debugging

	[Export] public bool LogReady = true;

	#endregion

	#region Godot Callbacks

	public override void _EnterTree() {
		Log.Me(() => "A ControlSurface has entered the tree. Checking properties...", LogReady, true);

		MusicPlayer = new();
		AddChild(MusicPlayer);

		Log.Me("Done!", true);
	}

	#endregion
}
