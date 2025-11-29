using CommonScripts;
using Godot;
using Godot.Collections;
namespace Game;

public partial class UpgradeItem : StandardItem {
    
    [Export] public float Value { get; private set; } = 1.5f;

    public float OriginalValue { get; protected set; } = 1.0f;

    [Export] public Dictionary<string, AudioStream> SFX { get; private set; } = [];

    public StandardCharacter? OwnerCharacter { get; private set; } = null;

    [Export] public int ChanceWeight { get; private set; } = 1;

    public void SetOwner(StandardCharacter? owner) {
        OwnerCharacter = owner;
    }

    public virtual void PowerOn() {
        AudioManager.StreamAudio(SFX["activate"], $"activate_{InstanceID}", AudioManager.AudioChannels.SFX, 0.8f);
        return;
    }

    public virtual void PowerOff() {
        AudioManager.StreamAudio(SFX["deactivate"], $"deactivate_{InstanceID}", AudioManager.AudioChannels.SFX, 0.8f);
        return;
    }
}