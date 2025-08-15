using CommonScripts;
using Godot;
namespace Game;

public partial class RifleBullet : StandardProjectile
{
	protected override void Impact(Area2D area, bool v = false, int s = 0)
	{
		if (area.GetParent() is StandardCharacter character)
		{
			Log.Me(() => $"{InstanceID} impacting {character.InstanceID}...", v, s + 1);

			Log.Me(() => $"Dealing damage...", v, s + 1);
			character.TakeDamage(Weapon.Damage, v, s + 1);

			Log.Me(() => "Queueing self for despawn...", v, s + 1);
			QueueFree();

			Log.Me(() => "Done!", v, s + 1);
			return;
		}
	}
}
