using Godot;
namespace CommonScripts;

public partial class StandardProjectileWeapon : StandardWeapon
{
	#region Nodes & Components

	[ExportGroup("Nodes & Components")]
	[Export] public PackedScene Projectile { get; private set; } = null!;
	[Export] public float AttackCameraShakeIntensity = 0.3f;

	#endregion

	#region Overrides

	protected override void Attack() {
		if (Projectile == null) {
			Log.Warn(() => "No projectile assigned. Cannot attack.");
			return;
		}

		if (WeaponOwner == null) {
			Log.Warn(() => "No weapon owner assigned. Cannot assign as projectile owner.");
			return;
		}

		if (!IsInstanceValid(WeaponOwner)) {
			Log.Warn(() => $"{WeaponOwner.InstanceID} is not valid. Cannot assign as projectile owner.");
			return;
		}

		CameraMan.Shake(AttackCameraShakeIntensity, GlobalPosition);

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
		
		Log.Me(() => $"A StandardProjectileWeapon has entered the tree. Passing to StandardWeapon...", LogReady);
		base._EnterTree();

		if (Projectile == null) Log.Err(() => "No projectile assigned. Cannot attack.", LogReady);

		Log.Me(() => $"Done checking properties for {ItemID}.", LogReady);
		
	}

	public override void _Ready() {
		
		Log.Me(() => $"Readying {InstanceID}. Passing to StandardWeapon...", LogReady);
		base._Ready();

		Log.Me(() => "Done!", LogReady);
		
	}

	public override void _Process(double delta) {
		
		Log.Me(() => $"Processing {ItemID} as StandardProjectileWeapon. Passing to StandardWeapon...", LogProcess);
		base._Process(delta);

		Log.Me(() => "Done processing!", LogProcess);
		
	}

	#endregion
}
