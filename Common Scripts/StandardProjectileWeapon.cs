using Godot;
namespace CommonScripts;

public partial class StandardProjectileWeapon : StandardWeapon
{
	#region Nodes & Components

	[ExportGroup("Nodes & Components")]
	[Export] public PackedScene Projectile { get; private set; } = null!;

	#endregion

	#region Overrides

	public override void Attack(bool v = false, int s = 0) {
		Log.Me(() => "Spawning projectile...", v, s + 1);

		if (Projectile == null) {
			Log.Warn(() => "No projectile assigned. Cannot attack.", v, s + 1);
			return;
		}

		StandardProjectile projectileInstance = Projectile.Instantiate<StandardProjectile>();
		projectileInstance.GlobalPosition = AttackOrigin;
		projectileInstance.RotationDegrees = AimDirection;
		projectileInstance.WeaponOwner = WeaponOwner;
	}

	#endregion

	#region Godot Callbacks

	public override void _EnterTree() {
		Log.Me(() => $"A StandardProjectileWeapon has entered the tree. Passing to StandardWeapon...", LogReady);

		base._EnterTree();

		Log.Me(() => "Checking properties...", LogReady);

		if (Projectile == null) Log.Err(() => "No projectile assigned. Cannot attack.", LogReady);

		Log.Me(() => $"Done checking properties for {ItemID}.", LogReady);
	}

	#endregion
}
