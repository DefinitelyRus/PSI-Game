using CommonScripts;
using Godot;
namespace Game;

public partial class FlakflyBomb : StandardProjectile {
    protected override void Impact(Area2D area) {
        base.Impact(area);

        WeaponOwner.Kill();
        
        // Explosion AVFX
    }
}
