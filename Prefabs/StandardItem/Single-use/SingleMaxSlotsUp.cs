using CommonScripts;
using Godot;
namespace Game;

public partial class SingleMaxSlotsUp : UpgradeItem {

    public override Variant? Use() {
        if (OwnerCharacter == null) {
            Log.Err(() => "OwnerCharacter is null in SingleMaxSlotsUp. Cannot use item.");
            return null;
        }

        OwnerCharacter.UpgradeManager.CurrentMaxSlots += 1;
        AudioManager.StreamAudio("max_slots_up", 0.8f);
        UIManager.SetOpenSlots(OwnerCharacter.UpgradeManager.CurrentMaxSlots);

        UIManager.SetBottomOverlayText("Inventory Slot Increased!", 2.0f);
        return null;
    }
}