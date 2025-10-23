using Godot;
namespace CommonScripts;

public partial class StandardProjectileWeapon : StandardWeapon
{
	#region Nodes & Components

	[ExportGroup("Nodes & Components")]
	[Export] public PackedScene Projectile { get; private set; } = null!;

	#endregion

	#region Overrides

	protected override void Attack(Context c = null!) {
		if (Projectile == null) {
			c.Warn(() => "No projectile assigned. Cannot attack.");
			return;
		}

		StandardProjectile projectileInstance = Projectile.Instantiate<StandardProjectile>();
		projectileInstance.GlobalPosition = GlobalPosition + AttackOrigin;
		projectileInstance.RotationDegrees = AimDirection;
		projectileInstance.Weapon = this;
		projectileInstance.WeaponOwner = WeaponOwner;

		WeaponOwner.GetParent().AddChild(projectileInstance);
	}

	#endregion

	#region Godot Callbacks

	public override void _EnterTree() {
		Context c = new();
		c.Trace(() => $"A StandardProjectileWeapon has entered the tree. Passing to StandardWeapon...", LogReady);
		base._EnterTree();

		if (Projectile == null) c.Err(() => "No projectile assigned. Cannot attack.", LogReady);

		c.Log(() => $"Done checking properties for {ItemID}.", LogReady);
		c.End();
	}

	public override void _Ready() {
		Context c = new();
		c.Trace(() => $"Readying {InstanceID}. Passing to StandardWeapon...", LogReady);
		base._Ready();

		c.Log(() => "Done!", LogReady);
		c.End();
	}

	public override void _Process(double delta) {
		Context c = new();
		c.Trace(() => $"Processing {ItemID} as StandardProjectileWeapon. Passing to StandardWeapon...", LogProcess);
		base._Process(delta);

		c.Log(() => "Done processing!", LogProcess);
		c.End();
	}

	#endregion
}
