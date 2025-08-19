using System;
using Godot;
namespace CommonScripts;

/// <summary>
/// A standard weapon class that can be derived from to create specific weapon types. <br/><br/>
/// Given its nature, it does not imply whether the weapon is ranged or melee, or if it's a projectile or hitscan weapon.
/// Such details are left to the implementation of the weapon itself. <br/><br/>
/// Nodes assigned this node must be a child of a <see cref="StandardCharacter"/> node, which is the owner of the weapon. <br/><br/>
/// </summary>
public partial class StandardWeapon : StandardItem
{
	#region Weapon Stats

	#region Damage

	/// <summary>
	/// The base damage dealt by the weapon.
	/// </summary>
	[ExportGroup("Weapon Stats")]
	[ExportSubgroup("Damage")]
	[Export] public float BaseDamage = 10f;

	/// <summary>
	/// Returns the calculated damage value after applying all relevant modifiers. <br/><br/>
	/// This is calculated based on <see cref="BaseDamage"/>
	/// and any modifiers targeting <c>"BaseDamage"</c> or <c>"Damage"</c>, in that order. <br/>
	/// However, this value is <i>before</i> applying any critical damage modifiers.
	/// For that, add <see cref="CriticalDamage"/> to this value. <br/><br/>
	/// <b>Important</b>: Setting this value ignores the value provided
	/// and instead recalculates it based on the current modifiers. <br/><br/>
	/// Related: <seealso cref="StatModifier"/>
	/// </summary>
	[Export] public float Damage {
		get => _damage;
		set => _damage = CalculateModifiedValue(nameof(BaseDamage), nameof(Damage), BaseDamage, false);
	}

	/// <summary>
	/// The calculated damage value after applying all relevant modifiers. <br/><br/>
	/// Do not set this value directly; use <see cref="Damage"/> instead.
	/// </summary>
	private float _damage = 0f;

	#endregion

	#region Attack Range

	/// <summary>
	/// The base range of the weapon, in pixels.
	/// </summary>
	[ExportSubgroup("Range")]
	[Export] public float BaseRange = 640f;

	/// <summary>
	/// Gets the effective range value after applying all relevant modifiers. <br/><br/>
	/// This is calculated based on <see cref="BaseRange"/>
	/// and any modifiers targeting <c>"BaseRange"</c> or <c>"Range"</c>, in that order. <br/><br/>
	/// <b>Important</b>: Setting this value ignores the value provided
	/// and instead recalculates it based on the current modifiers. <br/><br/>
	/// Related: <seealso cref="StatModifier"/>
	/// </summary>
	[Export] public float Range {
		get => _range;
		set => _range = CalculateModifiedValue(nameof(BaseRange), nameof(Range), BaseRange, false);
	}

	/// <summary>
	/// The effective range value after applying all relevant modifiers. <br/><br/>
	/// Do not set this value directly; use <see cref="Range"/> instead.
	/// </summary>
	private float _range = 0f;

	#endregion

	#region Attack Speed
	//NOTE: Code style -- BaseValue -> ModifiedValue -> _internalModifiedValue -> DerivedValue -> EnumValue -> EnumList
	/// <summary>
	/// Defines how long the weapon waits before a subsequent attack.
	/// </summary>
	[ExportSubgroup("Weapon Speed")]
	[Export] public float BaseAttackSpeed = 1f;

	/// <summary>
	/// Returns the calculated attack speed of the weapon after applying all relevant modifiers. <br/><br/>
	/// This is calculated based on <see cref="BaseAttackSpeed"/>
	/// and any modifiers targeting <c>"BaseAttackSpeed"</c> or <c>"AttackSpeed"</c>, in that order. <br/><br/>
	/// <b>Important</b>: Setting this value ignores the value provided
	/// and instead recalculates it based on the current modifiers. <br/><br/>
	/// Related: <seealso cref="StatModifier"/>
	/// </summary>
	[Export] public float AttackSpeed {
		get => _attackSpeed;
		set => _attackSpeed = CalculateModifiedValue(nameof(BaseAttackSpeed), nameof(AttackSpeed), BaseAttackSpeed, false);
	}

	/// <summary>
	/// The calculated attack speed of the weapon after applying all relevant modifiers. <br/><br/>
	/// </summary>
	private float _attackSpeed = 0f;

	/// <summary>
	/// Returns the auto-calculated interval between attacks in seconds.
	/// </summary>
	public float AttackInterval {
		get {
			return SpeedType switch {
				SpeedCalculation.Hertz => 1f / BaseAttackSpeed,
				SpeedCalculation.Interval => BaseAttackSpeed,
				_ => throw new Exception("Invalid Speed Calculation Type."),
			};
		}
	}

	/// <summary>
	/// The remaining time before the next attack can be performed, in seconds. <br/><br/>
	/// This is a setter/getter for <see cref="_remainingAttackInterval"/>.
	/// </summary>
	[Export] public float RemainingAttackInterval {
		get => _remainingAttackInterval;
		private set => _remainingAttackInterval = Mathf.Clamp(value, 0f, float.MaxValue);
	}

	/// <summary>
	/// The remaining time before the next attack can be performed, in seconds. <br/><br/>
	/// Do not set this value directly; use <see cref="RemainingAttackInterval"/> instead.
	/// </summary>
	private float _remainingAttackInterval = 0f;

	/// <summary>
	/// The type of speed calculation used for the weapon's attack speed.
	/// </summary>
	[Export] public SpeedCalculation SpeedType = SpeedCalculation.Hertz;

	/// <summary>
	/// Specifies the method used to calculate speed in a system.
	/// </summary>
	public enum SpeedCalculation {
		/// <summary>
		/// Represents speed as a frequency, measured in cycles per second.
		/// </summary>
		Hertz,

		/// <summary>
		/// Represents speed as a time interval between events, typically in seconds.
		/// </summary>
		Interval
	}

	[Export] public FiringModes FiringMode = FiringModes.Single;

	public enum FiringModes {
		/// <summary>
		/// Attacks once per input. <br/><br/>
		/// <b>Warning</b>: This mode has not yet been properly implemented. <br/><br/>
		/// TODO: Implement single firing logic.
		/// </summary>
		Single,

		/// <summary>
		/// Attacks multiple times per input, with a short delay between each attack. <br/><br/>
		/// <b>Warning</b>: This mode has not yet been properly implemented. <br/><br/>
		/// TODO: Implement burst firing logic.
		/// </summary>
		Burst,

		/// <summary>
		/// Attacks continuously while the input is held down.
		/// </summary>
		Automatic
	}

	#region Burst-specific Properties

	[Export(PropertyHint.Range, "0, 25, 1, or_greater")] public int BurstCount {
		get => _burstCount;
		set => _burstCount = Mathf.Clamp(value, 1, int.MaxValue);
	}

	private int _burstCount = 3;

	[Export] public float BurstInterval {
		get => _burstInterval;
		set => _burstInterval = Mathf.Clamp(value, 0f, float.MaxValue);
	}

	private float _burstInterval = 0.1f;

	[Export] public float RemainingBurstInterval {
		get => _remainingBurstInterval;
		private set => _remainingBurstInterval = Mathf.Clamp(value, 0f, float.MaxValue);
	}

	private float _remainingBurstInterval = 0f;

	#endregion

	#endregion

	#endregion

	#region Critical Attacks

	#region Activation

	/// <summary>
	/// The activation condition wherein a critical attack can occur.
	/// </summary>
	[ExportGroup("Critical Attacks")]
	[Export] public CriticalActivationTypes CriticalActivationType = CriticalActivationTypes.Chance;

	/// <summary>
	/// The different types of critical activation conditions.
	/// </summary>
	public enum CriticalActivationTypes {
		/// <summary>
		/// Critical hits are determined by a random chance based on <see cref="CriticalChance"/>.
		/// </summary>
		Chance,

		/// <summary>
		/// Critical hits occur when attacking a weak point on a target character.
		/// </summary>
		Weakpoint,

		/// <summary>
		/// Critical hits can occur both by chance or by hitting a weak point.
		/// </summary>
		Both
	}

	/// <summary>
	/// The base chance of a critical attack occurring. <br/><br/>
	/// This is a setter/getter for <see cref="_baseCriticalChance"/>. <br/>
	/// Setting this value will clamp it between <c>0.0</c> and <c>1.0</c>.
	/// </summary>
	[Export(PropertyHint.Range, "0, 1, 0.01")] public float BaseCriticalChance {
		get => _baseCriticalChance;
		set => _baseCriticalChance = Mathf.Clamp(value, 0f, 1f);
	}

	/// <summary>
	/// The chance of a critical attack occurring, as a percentage in the range of <c>0.0</c> to <c>1.0</c>. <br/><br/>
	/// Do not set this value directly; use <see cref="BaseCriticalChance"/> instead.
	/// </summary>
	private float _baseCriticalChance = 0.1f;

	/// <summary>
	/// The chance of a critical attack occurring, as a percentage in the range of <c>0.0</c> to <c>1.0</c>. <br/><br/>
	/// This is calculated based on <see cref="BaseCriticalChance"/>
	/// and any modifiers targeting <c>"BaseCriticalChance"</c> or <c>"CriticalChance"</c>, in that order. <br/><br/>
	/// <b>Important</b>: Setting this value ignores the value provided
	/// and instead recalculates it based on the current modifiers. <br/><br/>
	/// Related: <seealso cref="StatModifier"/>
	/// </summary>
	[Export] public float CriticalChance {
		get => _criticalChance;
		set {
			float result = CalculateModifiedValue(nameof(BaseCriticalChance), nameof(CriticalChance), BaseCriticalChance, false);
			_criticalChance = Mathf.Clamp(result, 0, 1);
		}
	}

	/// <summary>
	/// The chance of a critical attack occurring, as a percentage in the range of 0 to 1. <br/><br/>
	/// Do not set this value directly; use <see cref="CriticalChance"/> instead.
	/// </summary>
	private float _criticalChance = 0f;

	#endregion

	#region Damage

	/// <summary>
	/// The base critical damage dealt by the weapon.
	/// </summary>
	[Export] public float BaseCriticalDamage = 0f;

	/// <summary>
	/// The critical damage dealt by the weapon after applying all relevant modifiers. <br/><br/>
	/// This is calculated based on <see cref="BaseCriticalDamage"/>
	/// and any modifiers targeting <c>"BaseCriticalDamage"</c> or <c>"CriticalDamage"</c>, in that order. <br/><br/>
	/// <b>Important</b>: Setting this value ignores the value provided
	/// and instead recalculates it based on the current modifiers. <br/><br/>
	/// <b>Important</b>: This value is to be added to the <see cref="Damage"/> value;
	/// it is not the total damage dealt. <br/><br/>
	/// Related: <seealso cref="StatModifier"/>
	/// </summary>
	[Export] public float CriticalDamage {
		get => _criticalDamage;
		set => _criticalDamage = CalculateModifiedValue(nameof(BaseCriticalDamage), nameof(CriticalDamage), BaseCriticalDamage, false);
	}

	/// <summary>
	/// The critical damage dealt by the weapon after applying all relevant modifiers. <br/><br/>
	/// Do not set this value directly; use <see cref="CriticalDamage"/> instead.
	/// </summary>
	private float _criticalDamage = 0f;

	#endregion

	#endregion

	#region Attack Origin

	/// <summary>
	/// The <see cref="Vector2"/> position where the projectile/raycast will originate from.
	/// </summary>
	[ExportGroup("Attack Origin")]
	[Export] public Vector2 AttackOrigin { get; protected set; } = Vector2.Zero;

	/// <summary>
	/// The offset from the weapon position where the attack will originate. <br/><br/>
	/// This is used for instances where an attack always originates from a specific point that isn't the center of the weapon.
	/// For example, in most top-down or RPG games, the camera faces the player neither from the side nor directly from the top.
	/// The player's sprite will be offset so that (0, 0) is at the player's feet, not the center of the sprite.
	/// Similarly, the weapon will also be raised to the player's center instead of remaining at (0, 0).
	/// </summary>
	[Export] public Vector2 AttackOriginPositionOffset = Vector2.Zero;

	/// <summary>
	/// Offsets the attack origin to make it longer in one axis and shorter in another. <br/><br/>
	/// This is used for when the character sprite makes the character attack closer to its center of mass
	/// when facing vertically, and attack further when facing horizontally, or vice versa.
	/// </summary>
	[Export] public Vector2 AttackOriginSizeMultiplier = new(1f, 1f);

	public enum AttackOriginTypes {
		/// <summary>
		/// Attacks come directly from the weapon position.
		/// </summary>
		Centered,

		/// <summary>
		/// Attacks start a certain distance away from the weapon position.
		/// </summary>
		Concentric,

		/// <summary>
		/// Attacks start from a custom position defined by the user.
		/// </summary>
		Custom
	}

	[Export] public AttackOriginTypes AttackOriginType = AttackOriginTypes.Concentric;

	[Export] public float AttackOriginDistance = 35f;

	[Export] public Vector2 AttackOriginDirection {
		get => _attackOriginDirection;
		set => _attackOriginDirection = value.Normalized();
	}

	private Vector2 _attackOriginDirection = Vector2.Zero;

	public void UpdateAttackOrigin(bool v = false, int s = 0) {
		Log.Me(() => $"Updating attack origin for \"{ItemName}\" (ItemID: {ItemID})...", v, s + 1);

		switch (AttackOriginType) {
			case AttackOriginTypes.Centered:
				AttackOrigin = Vector2.Zero;
				Log.Me(() => "Attack origin is centered. Setting position to (0.00, 0.00)...", v, s + 1);
				break;

			case AttackOriginTypes.Concentric:
				AttackOriginDirection = AimDirectionVector.Rotated(Mathf.DegToRad(-90));
				AttackOrigin = AttackOriginPositionOffset + (AttackOriginDistance * AttackOriginSizeMultiplier * AttackOriginDirection);
				Log.Me(() => $"Attack origin is concentric. Set position to ({AttackOrigin.X:F2}, {AttackOrigin.Y:F2})...", v, s + 1);
				break;

			case AttackOriginTypes.Custom: return; // Custom origin is set by the user, no need to update.

			default:
				Log.Warn(() => $"`UpdateAttackOrigin` on \"{ItemName}\" (ItemID: {ItemID}) has an invalid `AttackOriginType`!", v, s + 1);
				break;
		}
	}

	#endregion

	#region Aiming

	/// <summary>
	/// The direction the weapon is currently aiming at, as an angle in degrees. <br/><br/>
	/// This is a setter/getter for <see cref="_aimDirection"/>. <br/>
	/// Setting this value will automatically normalize it to ensure it is a unit vector. <br/><br/>
	/// </summary>
	[ExportGroup("Aiming")]
	[Export] public float AimDirection {
		get => _aimDirection;
		set => _aimDirection = Mathf.Clamp(value, 0f, 360f);
	}

	/// <summary>
	/// The direction the weapon is currently aiming at, as an angle in degrees. <br/><br/>
	/// Do not set this value directly; use <see cref="AimDirection"/> instead.
	/// </summary>
	private float _aimDirection = 180;

	/// <summary>
	/// The direction the weapon is currently aiming at, as an angle in <see cref="Vector2"/>. <br/><br/>
	/// To change this value, set <see cref="AimDirection"/> to a new value. <br/>
	/// </summary>
	public Vector2 AimDirectionVector {
		get {
			float result = Mathf.DegToRad(_aimDirection);
			return new(Mathf.Cos(result), Mathf.Sin(result));
		}
	}

	/// <summary>
	/// The base spread of the weapon, in degrees. <br/><br/>
	/// This is a setter/getter for <see cref="_baseSpread"/>. <br/>
	/// Setting this value will clamp it between <c>0.0</c> and <c>360.0</c>.
	/// </summary>
	[Export] public float BaseSpread {
		get => _baseSpread;
		set => _baseSpread = Mathf.Clamp(value, 0f, 360f);
	}

	/// <summary>
	/// The base spread of the weapon, in degrees. <br/><br/>
	/// Do not set this value directly; use <see cref="BaseSpread"/> instead.
	/// </summary>
	private float _baseSpread = 0f;

	/// <summary>
	/// The spread of the weapon, in degrees. <br/><br/>
	/// This is calculated based on <see cref="BaseSpread"/> and any modifiers targeting <c>"BaseSpread"</c> or <c>"Spread"</c>, in that order. <br/><br/>
	/// <b>Important</b>: Setting this value ignores the value provided and instead recalculates it based on the current modifiers. <br/><br/>
	/// Related: <seealso cref="StatModifier"/>
	/// </summary>
	[Export] public float Spread {
		get => _spread;
		set {
			float result = CalculateModifiedValue(nameof(BaseSpread), nameof(Spread), BaseSpread, false);
			_spread = Mathf.Clamp(result, 0f, 360f);
		}
	}

	/// <summary>
	/// The spread of the weapon, in degrees. <br/><br/>
	/// Do not set this value directly; use <see cref="Spread"/> instead.
	/// </summary>
	private float _spread = 0f;

	/// <summary>
	/// Gets a random spread value based on <see cref="Spread"/>.
	/// </summary>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	/// <returns></returns>
	public float GetSpread(bool v = false, int s = 0) {
		Log.Me(() => "Generating random spread value...", v, s + 1);

		if (Spread <= 0f) {
			Log.Me(() => "Spread is 0, returning 0.", v, s + 1);
			return 0f;
		}

		float randomSpread = (float) GD.RandRange(-Spread, Spread);

		Log.Me(() => $"Done! Generated aim offset: {randomSpread}°", v, s + 1);
		return randomSpread;
	}

	/// <summary>
	/// The spread of the weapon as a <see cref="Vector2"/>. <br/><br/>
	/// To change this value, set <see cref="Spread"/> to a new value. <br/>
	/// </summary>
	public Vector2 SpreadVector {
		get {
			float angle = Mathf.DegToRad(Spread);
			return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
		}
	}

	public Vector2 GetSpreadVector(bool v = false, int s = 0) {
		Log.Me(() => "Passing to GetSpread...", v, s + 1);

		if (Spread <= 0f) {
			Log.Me(() => "Spread is 0, returning Vector2(0, 0).", v, s + 1);
			return Vector2.Zero;
		}

		float randomSpread = GetSpread(v, s + 1);
		float angle = Mathf.DegToRad(randomSpread);
		Vector2 spreadVector = new(Mathf.Cos(angle), Mathf.Sin(angle));

		Log.Me(() => $"Done! Generated aim offset vector: {spreadVector}", v, s + 1);
		return spreadVector;
	}

	#endregion

	#region Nodes & Components

	[ExportGroup("Nodes & Components")]
	[Export] public StandardCharacter WeaponOwner = null!;

	public ControlSurface Control => WeaponOwner.Control;

	#endregion

	#region Overridable Methods

	public virtual void QueueAttack(bool v = false, int s = 0) {
		Log.Me(() => $"Queueing attack for {InstanceID}...", v, s + 1);

		//Handle weapon cooldown
		if (RemainingAttackInterval == 0f)
		{
			Log.Me(() => "Executing attack...", v, s + 1);
			Attack(v, s + 1);
			RemainingAttackInterval = AttackInterval;
		}

		return;
	}

	protected virtual void Attack(bool v = false, int s = 0) {
		Log.Me(() => $"`QueueAttack` on \"{ItemName}\" (ItemID: {ItemID}) is not implemented! Override to add custom functionality.", v, s + 1);
	}

	#endregion

	#region Godot Callbacks

	public override void _EnterTree() {
		Log.Me(() => "A StandardWeapon has entered the tree. Passing to StandardItem...", LogReady);

		base._EnterTree();

		Log.Me(() => "Checking properties...", LogReady);

		if (WeaponOwner == null) Log.Err(() => "No weapon owner assigned. Attacks cannot be rooted back to its owner.", LogReady);

		Log.Me(() => "Calculating initial stats...", LogReady);

		Damage = 0;
		Range = 0;
		AttackSpeed = 0;
		CriticalChance = 0;
		CriticalDamage = 0;
		Spread = 0;

		Log.Me(() => $"Done checking properties for {ItemID}.", LogReady);
	}

	public override void _Ready()
	{
		Log.Me(() => $"Readying {InstanceID}. Passing to StandardItem...", LogReady);
		base._Ready();

		Log.Me(() => "Done!", LogReady);
	}

	public override void _Process(double delta) {
		Log.Me(() => $"Processing {ItemID} as StandardWeapon.", LogProcess);

		AimDirection = Mathf.PosMod(Mathf.RadToDeg(Control.FacingDirection.Angle()) + 90f, 360f);
		Log.Me(() => $"AimDirection set to {AimDirection:F2}°.", LogProcess);

		Log.Me(() => "Updating attack origin...", LogProcess);
		//AttackOriginAngle = new Vector2(Mathf.Cos(Mathf.DegToRad(AimDirection)), Mathf.Sin(Mathf.DegToRad(AimDirection)));
		UpdateAttackOrigin(LogProcess);

		Log.Me(() => "Checking for attack inputs...", LogProcess);
		if (Control.IsAttacking) {
			QueueAttack(LogProcess);
		}

		if (RemainingAttackInterval > 0f) {
			RemainingAttackInterval -= (float) delta;
			if (RemainingAttackInterval < 0f) RemainingAttackInterval = 0f;
		}

		if (FiringMode == FiringModes.Burst && RemainingBurstInterval > 0f) {
			RemainingBurstInterval -= (float) delta;
			if (RemainingBurstInterval < 0f) RemainingBurstInterval = 0f;
		}

		Log.Me(() => "Done processing!", LogProcess);
		return;
	}

	#endregion
}