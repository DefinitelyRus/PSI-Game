using CommonScripts;
using Godot;
namespace Game;

public partial class SingleMaxPowerUp : UpgradeItem {

    public override Variant? Use() {
        if (OwnerCharacter == null) {
            Log.Err(() => "OwnerCharacter is null in SingleMaxPowerUp. Cannot use item.");
            return null;
        }
    OwnerCharacter.UpgradeManager.CurrentMaxPower += 1;
    AudioManager.StreamAudio("max_power_up", 0.8f);

    int currentPowered = OwnerCharacter.UpgradeManager.CurrentPower;
    int newMax = OwnerCharacter.UpgradeManager.CurrentMaxPower;

    // Do not exceed upgrade manager's max power
    if (newMax > OwnerCharacter.UpgradeManager.MaxPower) {
        newMax = OwnerCharacter.UpgradeManager.MaxPower;
        OwnerCharacter.UpgradeManager.CurrentMaxPower = newMax;
    }

    if (UIManager.SelectedCharacter == OwnerCharacter)
        UIManager.SetPower(newMax - currentPowered, newMax); // show available vs max

        if (UIManager.SelectedCharacter == OwnerCharacter)
            UIManager.SetBottomOverlayText("Power Capacity Increased!", 2.0f);
        return null;
    }
}