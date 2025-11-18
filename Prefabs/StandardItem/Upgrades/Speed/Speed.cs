using CommonScripts;
namespace Game;

public partial class Speed : UpgradeItem {
    
    public override void PowerOn() {
        StandardCharacter? character = OwnerCharacter;

        if (character == null) {
            Log.Warn(() => $"PowerOn called on {ItemName}, but OwnerCharacter is null.");
            return;
        }

        OriginalValue = character.CurrentMaxSpeed;

        character.CurrentMaxSpeed *= Value;
        base.PowerOn();
        return;
    }

    public override void PowerOff() {
        StandardCharacter? character = OwnerCharacter;

        if (character == null) {
            Log.Warn(() => $"PowerOff called on {ItemName}, but OwnerCharacter is null.");
            return;
        }

        character.CurrentMaxSpeed = OriginalValue;
        base.PowerOff();
        return;
    }
}