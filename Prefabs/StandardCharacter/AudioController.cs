using Godot;
namespace CommonScripts;

public partial class AudioController : Node2D {
    [Export] public AudioStream[] GunShot { get; set; } = [];
    [Export] public AudioStream[] GunReady { get; set; } = [];


    public void PlayGunShot(int level) {
        if (level < 0 || level >= GunShot.Length) {
            Log.Err($"No gun shot audio for level {level}.");
            return;
        }

        AudioStream stream = GunShot[level];
        AudioManager.PlaySFX2D(stream, GlobalPosition);
    }


    public void PlayGunReady(int level) {
        if (level < 0 || level >= GunReady.Length) {
            Log.Err($"No gun ready audio for level {level}.");
            return;
        }

        AudioStream stream = GunReady[level];
        AudioManager.PlaySFX2D(stream, GlobalPosition);
    }
}