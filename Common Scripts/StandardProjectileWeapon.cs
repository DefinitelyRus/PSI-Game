using Godot;
using Godot.Collections;
namespace CommonScripts;

public partial class StandardProjectileWeapon : StandardWeapon
{
	#region Nodes & Components

	public Dictionary<string, StandardProjectile> Projectiles { get; private set; }

	#endregion
}
