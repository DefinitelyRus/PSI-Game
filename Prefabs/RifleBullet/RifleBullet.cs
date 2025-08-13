using CommonScripts;
using Godot;
namespace Game;

public partial class RifleBullet : StandardProjectile
{
    protected override void Impact(Node2D body, bool v = false, int s = 0)
    {
        Log.Me(() => $"RifleBullet impacting {body.Name}...", v, s + 1);

        if (body is StandardCharacter character)
        {
            Log.Me(() => $"Dealing damage to {character.Name}...", v, s + 1);
            character.TakeDamage(Weapon.Damage, v, s + 1);
        }

        Log.Me(() => "Queueing self for despawn...", v, s + 1);
        QueueFree();

        Log.Me(() => "Done!", v, s + 1);
        return;
    }
}
