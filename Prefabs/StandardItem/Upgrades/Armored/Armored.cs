using CommonScripts;
namespace Game;

public partial class Armored : UpgradeItem {

    public override void PowerOn() {
        StandardCharacter? character = OwnerCharacter;

        if (character == null) {
            Log.Warn(() => $"PowerOn called on {ItemName}, but OwnerCharacter is null.");
            return;
        }

        OriginalValue = character.CurrentMaxHealth;

        character.CurrentMaxHealth = (int) (character.CurrentMaxHealth * Value);
        character.Health *= Value;
        base.PowerOn();
        return;
    }

    public override void PowerOff() {
        StandardCharacter? character = OwnerCharacter;

        if (character == null) {
            Log.Warn(() => $"PowerOff called on {ItemName}, but OwnerCharacter is null.");
            return;
        }

        character.CurrentMaxHealth = (int) OriginalValue;
        character.Health /= Value;

        base.PowerOff();
        return;
    }
}