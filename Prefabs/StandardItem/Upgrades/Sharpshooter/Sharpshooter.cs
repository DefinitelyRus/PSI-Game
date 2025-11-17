using CommonScripts;
using Godot;
namespace Game;

public partial class Sharpshooter : StandardItem {
    
    [Export] public float StatMultiplier { get; private set; } = 1.5f;

    public StandardCharacter? OwnerCharacter { get; private set; } = null;

    public void SetOwner(StandardCharacter owner) {
        OwnerCharacter = owner;
    }

    public virtual void PowerOn() {
        Log.Warn(() => $"PowerOn called on {ItemName}, but not implemented.");
        return;
    }

    public virtual void PowerOff() {
        Log.Warn(() => $"PowerOff called on {ItemName}, but not implemented.");
        return;
    }
}