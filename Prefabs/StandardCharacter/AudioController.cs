using Godot;
using CommonScripts;
using Godot.Collections;
namespace Game;

public partial class AudioController : Node2D {
    [Export] public Dictionary<string, AudioStream> AudioClips { get; set; } = new();

    public void PlayAudio(string clipName, float volume = 1.0f) {
        bool hasClip = AudioClips.TryGetValue(clipName, out AudioStream? value);

        if (hasClip) {
            AudioStreamPlayer2D audioPlayer = new() {
                Stream = value,
                Autoplay = false,
                VolumeLinear = volume
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