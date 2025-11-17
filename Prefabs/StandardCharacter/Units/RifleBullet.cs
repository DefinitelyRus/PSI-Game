using System.Linq;
using CommonScripts;
using Godot;
namespace Game;

public partial class RifleBullet : StandardProjectile
{
	public override void _Ready() {
		Targets = [.. Commander.GetAllUnits()];

		base._Ready();
	}
	
	protected override void Impact(Area2D area)
	{
		if (area.GetParent() is StandardCharacter character)
		{
			character.TakeDamage(Weapon.Damage);
			QueueFree();
		}
	}
}
