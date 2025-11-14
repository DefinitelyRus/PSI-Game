using CommonScripts;
using Godot;
namespace Game;

public partial class DroneBullet : StandardProjectile {
    
    protected override void Impact(Area2D area) {
        if (area.GetParent() is StandardCharacter character) {
            character.TakeDamage(Weapon.Damage);
            QueueFree();

            return;
        }
    }
}
