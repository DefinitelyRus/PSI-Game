using CommonScripts;
namespace Game;

public partial class Lethality : UpgradeItem {

    public override void PowerOn() {
        StandardWeapon? weapon = OwnerCharacter?.Weapon;

        if (weapon == null) {
            Log.Warn(() => $"PowerOn called on {ItemName}, but OwnerCharacter has no weapon.");
            return;
        }

        StatModifier modifier = new(
            $"Lethality_{InstanceID}",
            "Damage",
            Value,
            StatModifier.ModifierType.Multiply
        );

        weapon.Modifiers.Add(modifier);
        base.PowerOn();
        return;
    }

    public override void PowerOff() {
        StandardWeapon? weapon = OwnerCharacter?.Weapon;

        if (weapon == null) {
            Log.Warn(() => $"PowerOff called on {ItemName}, but OwnerCharacter has no weapon.");
            return;
        }

        weapon.Modifiers.RemoveAll(m => m.ID == $"Lethality_{InstanceID}");
        base.PowerOff();
        return;
    }
}