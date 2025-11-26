using Godot;
using CommonScripts;
using Godot.Collections;
namespace Game;

public partial class AudioController : Node2D {
    [Export] public Dictionary<string, AudioStream> AudioClips { get; set; } = new();

    public void PlayAudio(string clipName, float volume = 1.0f) {
        bool hasClip = AudioClips.TryGetValue(clipName, out AudioStream? value);

        if (hasClip) AudioManager.StreamAudio2D(value!, GlobalPosition, AudioManager.AudioChannels.SFX, volume);
        
        else Log.Err(() => $"Audio clip '{clipName}' not found in AudioClips dictionary.");
    }

}