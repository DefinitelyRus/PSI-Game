using CommonScripts;
using Godot;
namespace Game;

public partial class SingleHeal : UpgradeItem {

    [Export] public float HealAmount { get; private set; } = 25f;

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