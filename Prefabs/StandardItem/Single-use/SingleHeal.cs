using CommonScripts;
using Godot;
namespace Game;

public partial class SingleHeal : StandardItem {

    [Export] public float HealAmount { get; private set; } = 25f;
    [Export] public StandardCharacter? OwnerCharacter { get; private set; } = null;

    public void SetOwner(StandardCharacter owner) {
        OwnerCharacter = owner;
    }

    public override Variant? Use() {
        if (OwnerCharacter == null) {
            Log.Err(() => "OwnerCharacter is null in SingleHeal. Cannot use item.");
            return null;
        }

        OwnerCharacter.Heal(HealAmount);
        AudioManager.StreamAudio("heal", 0.8f);
        QueueFree();
        return null;
    }
}