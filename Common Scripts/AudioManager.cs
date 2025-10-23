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


	public void PlayMusic(AudioStream stream, float volume = 0f, Context c = null!) {
		MusicPlayer.Stream = stream;
		MusicPlayer.VolumeLinear = volume * MusicVolume;
		MusicPlayer.Play();
	}


	public void StopMusic(Context c = null!) {
		MusicPlayer.Stop();
	}

	#endregion

	#region SFX

	[Export] public float SFXVolume = 1.0f;

	public AudioStreamPlayer PlaySFX(AudioStream stream, float volume = 0f, Context c = null!) {
		AudioStreamPlayer sfxPlayer = new();
		AddChild(sfxPlayer);

		sfxPlayer.Stream = stream;
		sfxPlayer.VolumeLinear = volume * SFXVolume;
		sfxPlayer.Autoplay = false;
		sfxPlayer.Finished += () => sfxPlayer.QueueFree();

		sfxPlayer.Play();
		return sfxPlayer;
	}

	public AudioStreamPlayer2D PlaySFX2D(AudioStream stream, Vector2 position, float volume = 0f, Context c = null!) {
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

	#region Godot Callbacks

	public override void _Ready() {
		MusicPlayer = new();
		AddChild(MusicPlayer);
	}

	#endregion
}
