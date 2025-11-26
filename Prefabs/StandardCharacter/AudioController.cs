using Godot;
using CommonScripts;
using Godot.Collections;
namespace Game;

public partial class AudioController : Node2D {
    [Export] public Dictionary<string, AudioStream> AudioClips { get; set; } = [];

    public void PlayAudio(string clipName, float volume = 1.0f) {
        bool hasClip = AudioClips.TryGetValue(clipName, out AudioStream? value);

        // NOTE: We use this instead of AudioManager.StreamAudio2D
        // because, for some reason, it complains that it's not running
        // on the main thread.
        if (hasClip) {
            AudioStreamPlayer2D audioPlayer = new() {
                Stream = value,
                Autoplay = false,
                VolumeLinear = volume * AudioManager.Instance.SFXVolume * AudioManager.Instance.UniversalVolume
            };

            AddChild(audioPlayer);
            
            audioPlayer.Play();
            audioPlayer.Connect("finished", Callable.From(audioPlayer.QueueFree));
        }
        
        else {
            Log.Err(() => $"Audio clip '{clipName}' not found in AudioClips dictionary.");
        }
    }

}