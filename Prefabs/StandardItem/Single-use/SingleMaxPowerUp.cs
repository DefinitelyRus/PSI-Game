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
        OwnerCharacter.UpgradeManager.CurrentPower += 1;
        AudioManager.StreamAudio("max_power_up", 0.8f);
        
        int currentPower = OwnerCharacter.UpgradeManager.CurrentPower;
        int maxPower = OwnerCharacter.UpgradeManager.CurrentMaxPower;
        UIManager.SetPower(currentPower, maxPower);
        return null;
    }
}