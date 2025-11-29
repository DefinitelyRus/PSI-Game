using CommonScripts;
using Godot;
namespace Game;

public partial class SingleMaxHealthUp : UpgradeItem {

    [Export] public float IncreaseAmount { get; private set; } = 100f;

    public override Variant? Use() {
        if (OwnerCharacter == null) {
            Log.Err(() => "OwnerCharacter is null in SingleMaxHealthUp. Cannot use item.");
            return null;
        }

        OwnerCharacter.CurrentMaxHealth += IncreaseAmount;
        OwnerCharacter.Heal(IncreaseAmount);
        AudioManager.StreamAudio("max_health_up", volume: 0.8f);
        QueueFree();
        UIManager.SetBottomOverlayText("Increased max health!", 2f);
        return null;
    }
}