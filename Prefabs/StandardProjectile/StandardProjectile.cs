using System;
using System.Linq;
using Game;
using Godot;
namespace CommonScripts;

public partial class StandardProjectile : RigidBody2D
{
	#region Basic Properties

	[ExportGroup("Basic Properties")]
	[Export] public string ProjectileName = "Standard Projectile";
	[Export] public string ProjectileID { get; protected set; } = string.Empty;
	[Export] public string[] Tags = [];
	[Export] public Area2D HitArea = null!;
	[Export] public StandardCharacter WeaponOwner = null!;
	[Export] public StandardProjectileWeapon Weapon = null!;
	[Export] public Node2D[] Targets { get; set; } = [];

	[Export] public TargetModes TargetMode { get; protected set; } = TargetModes.Any;

	public enum TargetModes {
		Any,
		Whitelist,
		Blacklist
	}

	[Export] public float Lifespan {
		get => _lifespan;
		set => _lifespan = Mathf.Clamp(value, 0f, float.MaxValue);
	}

	private float _lifespan = 5f;

	#endregion

	#region Physics

	[Export] public float Force {
		get => _force;
		set => _force = Mathf.Clamp(value, 0f, float.MaxValue);
	}

	private float _force = 500f;

	[Export] public ForceTypes ForceType { get; protected set; } = ForceTypes.Constant;

	public enum ForceTypes {
		Impulse,
		Acceleration,
		Constant
	}

	#region Force Application

	private void ApplyImpulseForce() {
		Vector2 direction = Vector2.Up.Rotated(Rotation);
		ApplyImpulse(Force * direction);
	}

	private void ApplyAccelerationForce(double delta) {
		float force = Force * (float) delta;
		ApplyCentralForce(force * Vector2.Up.Rotated(Rotation)); //NOTE: Code syle -- multipliers go before the vector even if there is only one.
	}

	private void ApplyConstantForce(double delta) {
		float speed = Force * (float) delta;
		Vector2 direction = Vector2.Up.Rotated(Rotation);
		MoveAndCollide(speed * direction); //NOTE: Code style -- multipliers go before the vector even if masked under variable names.
	}

	#endregion

	#endregion

	#region Debugging

	[ExportGroup("Debugging")]
	[Export] protected bool LogReady = true;
	[Export] protected bool LogProcess = false;
	[Export] protected bool LogPhysics = false;
	[Export] protected bool LogCollision = false;

	#region Instance ID

	/// <summary>
	/// This is the unique identifier for the projectile instance. <br/><br/>
	/// This ID is generated automatically if not set manually. <br/>
	/// This is a setter/getter for <see cref="_instanceID"/>.
	/// </summary>
	[ExportSubgroup("Instance ID")]
	[Export] public string InstanceID {
		get => _instanceID;
		private set => _instanceID = value ?? throw new ArgumentNullException(nameof(value), "InstanceID cannot be null.");
	}

	/// <summary>
	/// This is the unique identifier for the projectile instance. <br/><br/>
	/// Do not use this value directly; use <see cref="InstanceID"/> instead.
	/// </summary>
	private string _instanceID = "";

	/// <summary>
	/// A custom prefix for the instance ID. <br/><br/>
	/// If not provided, it will default to the <see cref="ProjectileName"/>. <br/>
	/// </summary>
	[Export] public string CustomPrefix = "";

	/// <summary>
	/// The action taken when a space is encountered in the character ID prefix. <br/><br/>
	/// <list type="bullet">
	/// <item><c>Keep</c>: Keeps the space. <b>Not recommended.</b></item>
	/// <item><c>Remove</c>: Removes the space.</item>
	/// <item><c>Underscore</c>: Replaces with underscores.</item>
	/// <item><c>Hyphen</c>: Replaces with hyphens.</item>
	/// </list>
	/// </summary>
	public enum SpaceReplacement {
		Keep,
		Remove,
		Underscore,
		Hyphen
	}

	#endregion

	#region Ignore Unassigned Nodes

	[ExportSubgroup("Ignore Unassigned Nodes")]
	[Export] public bool AllowNoProjectileName = false;
	[Export] public bool AllowNoHitArea = false;
	[Export] public bool AllowNoWeapon = false;
	[Export] public bool AllowNoOwner = false;
	[Export] public bool AllowNoIcon = false;
	[Export] public bool AllowNoSprite = false;
	[Export] public bool AutoAssignInstanceID = true;

	#endregion

	/// <summary>
	/// The action taken when a space is encountered in the character ID prefix.
	/// </summary>
	[Export] public SpaceReplacement ReplaceSpaceWith = SpaceReplacement.Remove;

	/// <summary>
	/// What symbol, if any, to insert between the prefix and the random ID suffix.
	/// </summary>
	[Export] public string Separator = "#";

	/// <summary>
	/// The character selection for the random ID suffix.
	/// </summary>
	[Export] public string SuffixChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

	/// <summary>
	/// How long the random ID suffix should be.
	/// </summary>
	[Export] public int SuffixLength = 5;

	/// <summary>
	/// Generates a unique `InstanceID` if one is not already assigned manually.
	/// </summary>
	private void GenerateInstanceID() {
		// Cancel if already assigned.
		if (!string.IsNullOrEmpty(InstanceID)) return;

		string prefix;
		string randomID = string.Empty;

		// Use default name if ProjectileName is blank.
		if (string.IsNullOrEmpty(ProjectileName)) {
			Log.Warn("`ProjectileName` is empty. Using default: \"Unnamed Projectile\"");
			ProjectileName = "Unnamed Projectile";
		}

		// Use ProjectileName if CustomPrefix is blank.
		if (string.IsNullOrEmpty(CustomPrefix)) {
			prefix = ProjectileName;
		}

		// Use CustomPrefix if provided.
		else prefix = CustomPrefix;

		// Replace space with the specified type.
		switch (ReplaceSpaceWith) {

			case SpaceReplacement.Remove:
				prefix = prefix.Replace(" ", "");
				break;

			case SpaceReplacement.Underscore:
				prefix = prefix.Replace(" ", "_");
				break;

			case SpaceReplacement.Hyphen:
				prefix = prefix.Replace(" ", "-");
				break;

			default:
				Log.Warn("An invalid `SpaceReplacement` value was provided. Keeping spaces instead...");
				// Do nothing.
				break;

		}

		GenerateInstanceID:
		for (int i = 0; i < SuffixLength; i++) {
			randomID += SuffixChars[(int) (GD.Randi() % SuffixChars.Length)];
		}

		InstanceID = prefix + Separator + randomID;

		// Check if the ID is already taken
		if (GetNodeOrNull<StandardCharacter>(InstanceID) != null) {
			randomID = string.Empty;
			goto GenerateInstanceID;
		}
	}

	#endregion

	#region Collision

	protected virtual void OnAreaEntered(Area2D area) {
		
		Log.Me(() => $"Area entered: {area.Name}", LogCollision);


		bool isTarget = false;

		foreach (Node2D target in Targets) {
            if (area.GetParent() == target) {
                isTarget = true;
				break;
            }
		}

		// Whitelist
		if (TargetMode == TargetModes.Whitelist) {
			if (Targets.Length == 0) Log.Warn(() => "No targets set. Ignoring area.", LogCollision);

			if (isTarget) Impact(area);
		}

		// Blacklist
		else if (TargetMode == TargetModes.Blacklist) {
			if (!isTarget) Impact(area);
		}

		// Any
		else if (TargetMode == TargetModes.Any) Impact(area);

		Log.Me(() => "Done!", LogCollision);
		
		return;
	}

	//Usually called when the projectile impacts a target.
	protected virtual void Impact(Area2D area) {
		if (area.GetParent() is StandardCharacter character) {
			if (character == WeaponOwner) return;
			float damage = Weapon.Damage;
			bool wasAlive = character.IsAlive;
			character.TakeDamage(damage);
			bool didKill = wasAlive && !character.IsAlive;
			if (WeaponOwner is StandardEnemy && character.Tags.Contains("Unit")) {
				CombatAnalytics.Record(WeaponOwner, character, damage, didKill);
			}
			QueueFree();
		}
	}

	//Usually called when the projectile is to be destroyed regardless of impact.
	protected virtual void Detonate() {
		Log.Warn(() => $"`Detonate` on {ProjectileName} (ProjectileID: {ProjectileID}) is not implemented! Override to add custom functionality.");
	}

	#endregion

	#region Godot Callbacks

	public override void _EnterTree() {
		Log.Me(() => "A StandardProjectile has entered the tree. Checking properties...", LogReady);

		if (string.IsNullOrEmpty(ProjectileID)) {
			Log.Err("ProjectileID must not be null or empty. Ready failed.");
			return;
		}

		if (string.IsNullOrEmpty(ProjectileName) && !AllowNoProjectileName) Log.Warn("`ProjectileName` should not be null or empty.");
		
		if (HitArea == null && !AllowNoHitArea) {
			Log.Err(() => "`HitArea` must not be null. Ready failed.");
			return;
		}

		if (Weapon == null && !AllowNoWeapon) {
			Log.Err(() => "`Weapon` must not be null. Ready failed.");
			return;
		}

		if (WeaponOwner == null && !AllowNoOwner) Log.Warn("`WeaponOwner` should not be null. Assign a `WeaponOwner` to `Weapon`.");

		if (string.IsNullOrEmpty(InstanceID)) {
			if (!AutoAssignInstanceID) Log.Me(() => "`InstanceID` is empty. Generating a new one...", LogReady);
			GenerateInstanceID();
		}

		Log.Me(() => "Done!", LogReady);
	}

	public override void _Ready() {
		Log.Me(() => $"Readying {ProjectileID}...", LogReady);

		Name = InstanceID;
		HitArea.AreaEntered += OnAreaEntered;
		PhysicsServer2D.BodyAddCollisionException(GetRid(), WeaponOwner.GetRid());

		if (ForceType == ForceTypes.Impulse) ApplyImpulseForce();

		if (WeaponOwner == null) return;

		WeaponOwner.AudioController.PlayAudio("attack_1", 1.0f);

		Log.Me(() => "Done!", LogReady);
	}

	public override void _Process(double delta) {
		if (Lifespan <= 0f) {
			QueueFree(); //TODO: Implement a proper deletion system.
			return;
		}

		Lifespan -= (float) delta;
	}

	public override void _PhysicsProcess(double delta) {
		switch (ForceType) {
			case ForceTypes.Impulse: break; // Impulse is applied in _Ready().

			case ForceTypes.Acceleration:
				ApplyAccelerationForce(delta);
				break;

			case ForceTypes.Constant:
				ApplyConstantForce(delta);
				break;

			default:
				Log.Err(() => $"Unknown force type: {ForceType}");
				break;
		}
	}

	#endregion
}
