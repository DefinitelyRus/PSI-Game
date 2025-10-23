using CommonScripts;
using Godot;
namespace Game;

public partial class RifleBullet : StandardProjectile
{
	protected override void Impact(Area2D area, Context c = null!)
	{
		if (area.GetParent() is StandardCharacter character)
		{
			character.TakeDamage(Weapon.Damage, c);
			QueueFree();

			return;
		}
	}
}
