using System;
using System.Linq;
using Godot;
namespace CommonScripts;

public partial class StandardProjectile : RigidBody2D
{
	#region Basic Properties

	[ExportGroup("Basic Properties")]
	[Export] public string ProjectileName = "Standard Projectile";
	[Export] public string ProjectileID { get; protected set; } = string.Empty;
	[Export] public string[] Tags = [];
	[Export] public StandardCharacter WeaponOwner = null!;
	[Export] public StandardProjectileWeapon Weapon = null!;
	[Export] public Node2D[] Targets { get; private set; } = [];

	[Export] public TargetModes TargetMode { get; protected set; } = TargetModes.Whitelist;

	public enum TargetModes {
		Any,
		Whitelist,
		Blacklist
	}

	[Export] public float Lifespan {
		get => _lifespan;
		set => Mathf.Clamp(value, 0f, float.MaxValue);
	}

	private float _lifespan = 5f;

	#endregion

	#region Physics

	[Export] public float Force {
		get => _force;
		set => Mathf.Clamp(value, 0f, float.MaxValue);
	}

	private float _force = 500f;

	[Export] public ForceTypes ForceType { get; protected set; } = ForceTypes.Constant;

	public enum ForceTypes {
		Impulse,
		Acceleration,
		Constant
	}

	#region Force Application

	private void ApplyImpulseForce(bool v = false, int s = 0) {
		Log.Me(() => $"Applying {Force}uf to {InstanceID}...", v, s + 1);

		Vector2 direction = Vector2.Up.Rotated(Rotation);

		ApplyImpulse(Force * direction);

		Log.Me(() => $"Applied {Force}uf heading {RotationDegrees}°", v, s + 1);
	}

	private void ApplyAccelerationForce(double delta, bool v = false, int s = 0) {
		Log.Me(() => "Applying acceleration...", v, s + 1);

		float force = Force * (float) delta;
		ApplyCentralForce(force * Vector2.Up.Rotated(Rotation)); //NOTE: Code syle -- multipliers go before the vector even if there is only one.

		Log.Me(() => $"Applied {Force}uf heading {RotationDegrees}°", v, s + 1);
	}

	private void ApplyConstantForce(double delta, bool v = false, int s = 0) {
		Log.Me(() => "Applying constant force...", v, s + 1);

		float speed = Force * (float) delta;
		Vector2 direction = Vector2.Up.Rotated(Rotation);

		MoveAndCollide(speed * direction); //NOTE: Code style -- multipliers go before the vector even if masked under variable names.

		Log.Me(() => $"Applied {speed}uf heading {RotationDegrees}°", v, s + 1);
	}

	#endregion

	#endregion

	#region Debugging

	[ExportGroup("Debugging")]
	[Export] protected bool LogReady = true;
	[Export] protected bool LogProcess = false;
	[Export] protected bool LogPhysics = false;

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


	#region Ignore Unassigned Nodes

	[ExportSubgroup("Ignore Unassigned Nodes")]
	[Export] public bool AllowNoProjectileName = false;
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
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	private void GenerateInstanceID(bool v = false, int s = 0) {
		Log.Me($"Generating an `InstanceID`...", v, s + 1);

		// Cancel if already assigned.
		if (!string.IsNullOrEmpty(InstanceID)) {
			Log.Me($"`InstanceID` is already assigned a value (\"{InstanceID}\"). Skipping...", v, s + 1);
			return;
		}

		string prefix;
		string randomID = string.Empty;

		// Use default name if ProjectileName is blank.
		if (string.IsNullOrEmpty(ProjectileName)) {
			Log.Warn("`ProjectileName` is empty. Using default: \"Unnamed Projectile\"", v, s + 1);
			ProjectileName = "Unnamed Projectile";
		}

		// Use ProjectileName if CustomPrefix is blank.
		if (string.IsNullOrEmpty(CustomPrefix)) {
			Log.Me($"`CustomPrefix` is empty. Using `ProjectileName` \"{ProjectileName}\"...", v, s + 1);
			prefix = ProjectileName;
		}

		// Use CustomPrefix if provided.
		else {
			Log.Me($"Using `CustomPrefix` \"{CustomPrefix}\"...", v, s + 1);
			prefix = CustomPrefix;
		}

		// Replace space with the specified type.
		switch (ReplaceSpaceWith) {

			case SpaceReplacement.Keep:
				Log.Me("Keeping spaces...", v, s + 1);
				break;

			case SpaceReplacement.Remove:
				Log.Me("Removing spaces...", v, s + 1);
				prefix = prefix.Replace(" ", "");
				break;

			case SpaceReplacement.Underscore:
				Log.Me("Replacing spaces with underscores...", v, s + 1);
				prefix = prefix.Replace(" ", "_");
				break;

			case SpaceReplacement.Hyphen:
				Log.Me("Replacing spaces with hyphens...", v, s + 1);
				prefix = prefix.Replace(" ", "-");
				break;

			default:
				Log.Warn("An invalid `SpaceReplacement` value was provided. Keeping spaces instead...", v, s + 1);
				break;

		}

		GenerateInstanceID:
		Log.Me("Generating a unique ID...", v, s + 1);
		for (int i = 0; i < SuffixLength; i++) {
			randomID += SuffixChars[(int) (GD.Randi() % SuffixChars.Length)];
		}

		InstanceID = prefix + Separator + randomID;

		// Check if the ID is already taken
		if (GetNodeOrNull<StandardCharacter>(InstanceID) != null) {
			Log.Me($"Instance ID \"{InstanceID}\" is already taken. Generating a new one...", v, s + 1);
			randomID = string.Empty;
			goto GenerateInstanceID;
		}

		Log.Me($"Generated ID \"{InstanceID}\"!", v, s + 1);
	}

	#endregion

	#region Godot Callbacks

	public override void _EnterTree() {
		Log.Me(() => "A StandardProjectile has entered the tree. Checking properties...", LogReady);

		if (string.IsNullOrEmpty(ProjectileID)) {
			Log.Err("ProjectileID must not be null or empty. Ready failed.");
			return;
		}

		if (string.IsNullOrEmpty(ProjectileName)) Log.Warn("`ProjectileName` should not be null or empty.");

		if (WeaponOwner == null) Log.Warn("`WeaponOwner` should not be null.");

		if (Weapon == null) {
			Log.Err(() => "`Weapon` must not be null. Ready failed.", LogReady);
			return;
		}

		if (string.IsNullOrEmpty(InstanceID)) {
			Log.Warn(() => "`InstanceID` is empty. Generating a new one...", LogReady);
			GenerateInstanceID(LogReady);
		}

		Log.Me(() => "Done!", LogReady);
	}

	public override void _Ready() {
		Log.Me(() => $"Readying {ProjectileID}...", LogReady);

		if (ForceType == ForceTypes.Impulse) {
			Log.Me(() => "Applying force...");
			ApplyImpulseForce(LogReady);
		}

		Log.Me(() => "Done!", LogReady);
	}

	public override void _PhysicsProcess(double delta) {
		Log.Me(() => $"Processing physics for {ProjectileID}...", LogPhysics);

		switch (ForceType) {
			case ForceTypes.Impulse: break; // Impulse is applied in _Ready().

			case ForceTypes.Acceleration:
				ApplyAccelerationForce(delta, LogPhysics);
				break;

			case ForceTypes.Constant:
				ApplyConstantForce(delta, LogPhysics);
				break;

			default:
				Log.Err(() => $"Unknown force type: {ForceType}", LogPhysics);
				break;
		}
	}

	#endregion
}
