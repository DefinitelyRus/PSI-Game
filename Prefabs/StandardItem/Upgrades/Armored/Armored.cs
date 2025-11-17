using CommonScripts;
using Godot;
namespace Game;

public partial class Armored1 : StandardItem {
    
    [Export] public float StatMultiplier { get; private set; } = 1.5f;

    public override Variant? Equip() {
        return StatMultiplier;
    }

    public override Variant? Unequip() {
        return 1.0f;
    }
}