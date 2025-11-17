using CommonScripts;
using Godot;
namespace Game;

public partial class Speed : StandardItem {
    
    [Export] public float StatMultiplier { get; private set; } = 1.5f;

    public StandardCharacter? OwnerCharacter { get; private set; } = null;

    public void SetOwner(StandardCharacter owner) {
        OwnerCharacter = owner;
    }

    public virtual void PowerOn() {
        return;
    }

    public virtual void PowerOff() {
        return;
    }
}