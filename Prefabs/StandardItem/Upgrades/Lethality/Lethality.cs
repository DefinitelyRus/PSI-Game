using CommonScripts;
using Godot;
namespace Game;

public partial class Lethality : StandardItem {
    
    [Export] public float StatMultiplier { get; private set; } = 1.5f;

    public StandardCharacter? OwnerCharacter { get; private set; } = null;

    public void SetOwner(StandardCharacter owner) {
        OwnerCharacter = owner;
    }

    public virtual void PowerOn() {
        StandardWeapon? weapon = OwnerCharacter?.Weapon;

        if (weapon == null) {
            Log.Warn(() => $"PowerOn called on {ItemName}, but OwnerCharacter has no weapon.");
            return;
        }

        StatModifier modifier = new(
            $"Lethality_{InstanceID}",
            "Damage",
            StatMultiplier,
            StatModifier.ModifierType.Multiply
        );

        weapon.Modifiers.Add(modifier);
        return;
    }

    public virtual void PowerOff() {
        StandardWeapon? weapon = OwnerCharacter?.Weapon;

        if (weapon == null) {
            Log.Warn(() => $"PowerOff called on {ItemName}, but OwnerCharacter has no weapon.");
            return;
        }

        weapon.Modifiers.RemoveAll(m => m.ID == $"Lethality_{InstanceID}");
        return;
    }
}