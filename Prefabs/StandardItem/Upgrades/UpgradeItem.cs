using CommonScripts;
using Godot;
using Godot.Collections;
namespace Game;

public partial class UpgradeItem : StandardItem {
    
    [Export] public float StatMultiplier { get; private set; } = 1.5f;

    [Export] public Dictionary<string, AudioStream> SFX { get; private set; } = [];

    public StandardCharacter? OwnerCharacter { get; private set; } = null;

    public void SetOwner(StandardCharacter owner) {
        OwnerCharacter = owner;
    }

    public virtual void PowerOn() {
        AudioManager.PlaySFX(SFX["activate"], 0.8f);
        return;
    }

    public virtual void PowerOff() {
        AudioManager.PlaySFX(SFX["deactivate"], 0.8f);
        return;
    }
}