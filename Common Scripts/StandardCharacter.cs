using System;
using Godot;
namespace CommonScripts;

public partial class StandardCharacter : CharacterBody2D {
	#region Permanent Properties

	/// <summary>
	/// The name of the character. <br/><br/>
	/// This name is used for logging and debugging purposes, and can be used to identify the character in the game.<br/>
	/// It is not unique and can be shared by multiple characters.
	/// If such a unique identifier is needed, use <see cref="InstanceID"/> instead.<br/><br/>
	/// </summary>
	[ExportGroup("Permanent Properties")]
	[Export] public string CharacterName = string.Empty;

	[Export] public string CharacterID { get; protected set; } = string.Empty;

	/// <summary>
	/// The maximum health of the character when unaffected by external modifiers.
	/// </summary>
	[Export] public float MaxHealth = 100f;

	/// <summary>
	/// The maximum speed of the character when unaffected by external modifiers.
	/// </summary>
	[Export] public float MaxSpeed = 64f;

	/// <summary>
	/// The time it takes for the character to accelerate to its maximum speed.
	/// </summary>
	[Export] public float AccelerationTime = 0.5f;

	/// <summary>
	/// The time it takes for the character to decelerate to a stop from its maximum speed.
	/// </summary>
	[Export] public float DecelerationTime = 0.5f;

	/// <summary>
	/// The tags that help describe the character's role, type, or behavior in the game.
	/// </summary>
	[Export] public string[] Tags = [];

	/// <summary>
	/// Whether the character should be despawned (removed from the tree) when it dies.
	/// </summary>
	[Export] public bool DespawnOnDeath = false;

	#endregion

	#region Live Properties

	#region Health

	/// <summary>
	/// The current health of the character. <br/><br/>
	/// Do not use this value directly except for getters and setters, as it is not clamped.<br/>
	/// </summary>
	[ExportGroup("Live Properties")]
	[ExportSubgroup("Health")]
	private float _health = 100f;

	/// <summary>
	/// The current health of the character. <br/><br/>
	/// This is a getter/setter for <see cref="_health"/>.
	/// Setting this value will clamp it between <c>0</c> and <see cref="CurrentMaxHealth"/>.
	/// </summary>
	[Export] public float Health {
		get => _health;
		set { _health = Mathf.Clamp(value, 0, _currentMaxHealth); }
	}

	/// <summary>
	/// The maximum health of the character. <br/><br/>
	/// Do not use this value directly except for getters and setters, as it is not clamped.<br/>
	/// </summary>
	private float _currentMaxHealth = 100f;

	/// <summary>
	/// How much health the character can have at maximum. <br/><br/>
	/// This value is intended to be used to shrink or grow the character's health pool temporarily.<br/><br/>
	/// This is a getter/setter for <see cref="_currentMaxHealth"/>.
	/// Setting this value will clamp it between <c>0</c> and <see cref="float.MaxValue"/>.<br/>
	/// </summary>
	[Export] public float CurrentMaxHealth {
		get => _currentMaxHealth;
		set { _currentMaxHealth = Mathf.Clamp(value, 0, float.MaxValue); }
	}

	/// <summary>
	/// Whether the character is alive or dead. <br/><br/>
	/// This value cannot be set manually and returns true if <see cref="Health"/> is greater than 0.
	/// </summary>
	public bool IsAlive = true;

	/// <summary>
	/// Make this character take damage, apply audio/visual effects, and kill if health reaches 0.
	/// </summary>
	/// <param name="amount">How much health to reduce the health by.</param>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	public void TakeDamage(float amount, bool v = false, int s = 0) {
		Log.Me($"Giving {amount:F2} to {InstanceID}...", v, s + 1);

		// Checks
		if (amount < 0) {
			Log.Err($"Cannot give negative damage. Use Heal() instead if this is intended.", v, s + 1);
			return;
		}

		Health -= amount;

		//TODO: AVFX here.
		//

		// Kill on 0 health.
		if (Health == 0) {
			Log.Me($"Reached 0 health. Killing...", v, s + 1);
			Kill(v, s + 1);
		}

		Log.Me($"{InstanceID} now has {Health:F2}/{MaxHealth:F2} health.", v, s + 1);
		Log.Me($"{InstanceID} took {amount:F2} damage. (IsAlive: {IsAlive})", true, s + 1);
		return;
	}

	/// <summary>
	/// Heals the character by the specified amount and applying audio/visual effects if available.
	/// </summary>
	/// <param name="amount">How much health to increase the health by.</param>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	public void Heal(float amount, bool v = false, int s = 0) {
		Log.Me($"Healing {InstanceID} for {amount:F2}...", v, s + 1);

		// Checks
		if (amount < 0) {
			Log.Err($"Cannot heal negative amount. Use TakeDamage() instead if this is intended.", v, s + 1);
			return;
		}

		if (!IsAlive) {
			Log.Me($"{InstanceID} is dead. Cannot heal.", v, s + 1);
			return;
		}

		Health += amount;

		//TODO: AVFX here.
		//

		// Health cap
		if (Health > CurrentMaxHealth) {
			Health = CurrentMaxHealth;
		}

		Log.Me($"{InstanceID} now has {Health:F2}/{CurrentMaxHealth:F2} health.", v, s + 1);
		Log.Me($"{InstanceID} healed for {amount:F2}.", true, s + 1);
		return;
	}

	/// <summary>
	/// Kills the character and applies audio/visual effects if available.
	/// It will also despawn the character if <see cref="DespawnOnDeath"/> is <c>true</c>.
	/// </summary>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	public void Kill(bool v = false, int s = 0) {
		Log.Me(() => $"Killing {InstanceID}...", v, s + 1);

		if (!IsAlive) {
			Log.Me($"{InstanceID} is already dead.", v, s + 1);
			return;
		}

		// Set health to 0
		Health = 0; //Redundancy; for when `Kill` is called without dealing any damage.
		IsAlive = false;

		bool allowDespawn = false;

		#region AVFX

		Log.Me(() => $"Playing death animation for {InstanceID}...", v, s + 1);

		// Set animation direction
		if (AnimationTree != null) AnimationTree.Set("parameters/Death/blend_position", new Vector2(Control.MovementDirection.X, -Control.MovementDirection.Y));
		else allowDespawn = true;

		// Set animation state, call lambda function if DespawnOnDeath
		if (AnimationState != null) {
			AnimationState.Travel("Death");
			AnimationState.Changed += () => {
				Log.Me(() => $"Finished playing death animation for {InstanceID}. Allowing despawn...", v, s + 1);
				if (DespawnOnDeath) {
					Log.Me("Queueing despawn...", v, s + 1);
					QueueFree();
				}
			};
		}
		else allowDespawn = true;

		// Allow immediate despawn if an animation node is null
		if (allowDespawn && DespawnOnDeath) {
			Log.Me("Queueing despawn...", v, s + 1);
			QueueFree();
		}

		#endregion

		Log.Me(() => $"Killed {InstanceID}.", true, s + 1);
		return;
	}

	#endregion

	#region Speed

	/// <summary>
	/// The target speed the character will try to reach. <br/><br/>
	/// Do not use this value directly except for getters and setters, as it is not clamped.<br/>
	/// </summary>
	private float _speed = 0f;

	/// <summary>
	/// The target speed the character will try to reach. <br/><br/>
	/// This is a getter/setter for <see cref="_speed"/>.<br/>
	/// Setting this value will clamp it to the range of <c>0</c> to <see cref="CurrentMaxSpeed"/>.
	/// </summary>
	/// <remarks>This value will often be equal to <see cref="CurrentMaxSpeed"/> unless using analog inputs.</remarks>
	[ExportSubgroup("Speed")]
	[Export] public float Speed {
		get => _speed;
		set { _speed = Mathf.Clamp(value, 0, _currentMaxSpeed); }
	}

	/// <summary>
	/// The maximum speed of the character. <br/><br/>
	/// Do not use this value directly except for getters and setters, as it is not clamped.<br/>
	/// </summary>
	private float _currentMaxSpeed = 64f;

	/// <summary>
	/// The maximum speed of the character. <br/><br/>
	/// This value is intended to be used to shrink or grow the character's speed temporarily.<br/><br/>
	/// This is a getter/setter for <see cref="_currentMaxSpeed"/>.
	/// Setting this value will clamp it to the range of <c>0</c> to <see cref="float.MaxValue"/>.<br/>
	/// </summary>
	[Export] public float CurrentMaxSpeed {
		get => _currentMaxSpeed;
		set { _currentMaxSpeed = Mathf.Clamp(value, 0, float.MaxValue); }
	}

	protected Vector2 LastMovementDirection = Vector2.Zero;

	/// <summary>
	/// Moves the character based on the current control inputs and delta time.
	/// </summary>
	/// <param name="delta">The time since the last physics frame.</param>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	private void Move(double delta, bool v = false, int s = 0) {
		Log.Me(() => $"Moving...", v, s + 1);

		// Accelerate if input is detected
		if (Control.MovementMultiplier > 0f) {
			Log.Me(() => $"Accelerating at {Control.MovementMultiplier:F2}x rate...", v, s + 1);
			Speed += (float) (Control.MovementMultiplier * MaxSpeed * (delta / AccelerationTime));
			Log.Me(() => $"Speed after acceleration: {Speed:F2}", v, s + 1);
		}

		// Calculate deceleration
		else if (Speed > 0f) {
			Log.Me(() => $"Decelerating...", v, s + 1);
			Speed -= (float) (MaxSpeed * (delta / DecelerationTime));
			Log.Me(() => $"Speed after deceleration: {Speed:F2}", v, s + 1);
		}

		//Apply speed
		Velocity = Speed * LastMovementDirection;
		MoveAndSlide();

		if (Control.MovementDirection != Vector2.Zero) LastMovementDirection = Control.MovementDirection;

		Log.Me(() => "Done!", v, s + 1);
	}

	#endregion

	#endregion

	#region AVFX

	/// <summary>
	/// Updates the sprite animations based on the current player state. <br/><br/>
	/// <b>Important</b>: This method is meant to be called in <see cref="_Process"/> only. <br/><br/>
	/// <b>Warning</b>: This current implementation is <b>project-specific</b>. It only supports walking, shooting, and idle.
	/// </summary>
	/// <param name="v"></param>
	/// <param name="s"></param>
	protected void UpdateAnimations(bool v = false, int s = 0) {
		Log.Me(() => "Updating animations...", v, s + 1);

		bool isWalking = Speed > 0;
		bool isAttacking = Weapon.Control.IsAttacking;
		//^ TECHNICAL DEBT: This is inherently bugged. Animates attack even when not intended.
		//TODO: Replace with an IsAttacking property in StandardWeapon itself.

		if (isWalking) {
			Log.Me($"Walking at {Speed / CurrentMaxSpeed:F2}% ({Speed:F2}/{CurrentMaxSpeed:F2}) speed...", v, s + 1);
			AnimationTree.Set("parameters/Walk/blend_position", new Vector2(Control.MovementDirection.X, -Control.MovementDirection.Y));
			AnimationState.Travel("Walk");
			AnimationPlayer.SpeedScale = Speed / CurrentMaxSpeed;
		}

		if (isAttacking) {
			Log.Me("Attacking...", v, s + 1);
			AnimationTree.Set("parameters/Attack/blend_position", new Vector2(Control.FacingDirection.X, -Control.FacingDirection.Y));
			AnimationState.Travel("Attack");
		}

		if (!isWalking && !isAttacking) {
			Log.Me("Idling...", v, s + 1);
			AnimationTree.Set("parameters/Idle/blend_position", new Vector2(Control.FacingDirection.X, -Control.FacingDirection.Y));
			AnimationState.Travel("Idle");
		}

		Log.Me($"Facing {Control.FacingDirection.X:F2}/{Control.FacingDirection.Y:F2}. Speed: {Speed}", v, s + 1);

		Log.Me(() => "Done!", v, s + 1);
	}

	#endregion

	#region Nodes & Components

	[ExportGroup("Nodes & Components")]
	[Export] public ControlSurface Control = null!;
	[Export] public StandardWeapon Weapon = null!;
	[Export] public AnimationPlayer AnimationPlayer = null!;
	[Export] public AnimationTree AnimationTree = null!;
	public AnimationNodeStateMachinePlayback AnimationState => (AnimationNodeStateMachinePlayback) AnimationTree.Get("parameters/playback");

	#endregion

	#region Debugging

	[ExportGroup("Debugging")]
	[Export] protected bool LogReady = true;
	[Export] protected bool LogProcess = false;
	[Export] protected bool LogPhysics = false;

	#region Instance ID

	/// <summary>
	/// The character's unique identifier. <br/><br/>
	/// This ID is generated automatically if not set manually. <br/>
	/// This is a setter/getter for <see cref="_instanceID"/>.
	/// </summary>
	[ExportSubgroup("Instance ID")]
	[Export] public string InstanceID {
		get => _instanceID;
		private set => _instanceID = value ?? throw new ArgumentNullException(nameof(value), "InstanceID cannot be null.");
	}

	/// <summary>
	/// The character's unique identifier. <br/><br/>
	/// Do not use this value directly except for getters and setters; use <see cref="InstanceID"/> instead.<br/>
	/// </summary>
	private string _instanceID = "";

	/// <summary>
	/// A custom prefix for the character ID. <br/><br/>
	/// If not provided, it will default to the <see cref="CharacterName"/>. <br/>
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
		Log.Me($"Generating a `InstanceID`...", v, s + 1);

		// Cancel if already assigned.
		if (!string.IsNullOrEmpty(InstanceID)) {
			Log.Me($"`InstanceID` is already assigned a value (\"{InstanceID}\"). Skipping...", v, s + 1);
			return;
		}

		string prefix;
		string randomID = string.Empty;

		// Use default name if CharacterName blank.
		if (string.IsNullOrEmpty(CharacterName)) {
			Log.Warn("`CharacterName` is empty. Using default: \"Unnamed Character\"", v, s + 1);
			CharacterName = "Unnamed Character";
		}

		// Use CharacterName if CustomPrefix is blank.
		if (string.IsNullOrEmpty(CustomPrefix)) {
			Log.Me($"`CustomPrefix` is empty. Using `CharacterName` \"{CharacterName}\"...", v, s + 1);
			prefix = CharacterName;
		}

		// Use CustomerPrefix if provided.
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

		GenerateCharacterID:
		Log.Me("Generating a unique ID...", v, s + 1);
		for (int i = 0; i < SuffixLength; i++) {
			randomID += SuffixChars[(int) (GD.Randi() % SuffixChars.Length)];
		}

		InstanceID = prefix + Separator + randomID;

		// Check if the ID is already taken
		if (GetNodeOrNull<StandardCharacter>(InstanceID) != null) {
			Log.Me($"Character ID \"{InstanceID}\" is already taken. Generating a new one...", v, s + 1);
			randomID = string.Empty;
			goto GenerateCharacterID;
		}

		Log.Me($"Generated ID \"{InstanceID}\"!", v, s + 1);
	}

	#endregion

	#region Ignore Unassigned Values

	[ExportSubgroup("Ignore Unassigned Values")]
	[Export] public bool SilentlyAutoAssignDefaultName = false;
	[Export] public bool AllowNoWeapon = false;
	[Export] public bool AllowNoAnimationPlayer = false;
	[Export] public bool AllowNoAnimationTree = false;
	[Export] public bool SilentlyAutoAssignInstanceID = true;

	#endregion

	#endregion

	#region Godot Callbacks

	public override void _EnterTree() {
		Log.Me($"A StandardCharacter has entered the tree. Checking properties...", LogReady);

		if (string.IsNullOrEmpty(CharacterName)) {
			if (!SilentlyAutoAssignDefaultName) Log.Warn("CharacterName should not be empty. Using default: \"Unnamed Character\"");
			CharacterName = "Unnamed Character";
		}

		if (Control == null) Log.Err("ControlSurface is not assigned. This character cannot be controlled.");

		if (Weapon == null && !AllowNoWeapon) Log.Warn("StandardWeapon is not assigned. This character cannot attack.");

		if (AnimationPlayer == null && !AllowNoAnimationPlayer) Log.Warn("AnimationPlayer is not assigned. This character will not be animated.");

		if (AnimationTree == null && !AllowNoAnimationTree) Log.Warn("AnimationTree is not assigned. This character will not be animated properly.");

		if (string.IsNullOrEmpty(InstanceID)) {
			if (!SilentlyAutoAssignInstanceID)Log.Me("InstanceID is not assigned. Generating a new one...", LogReady);
			GenerateInstanceID(LogReady);
		}

		Log.Me("Done!", LogReady);
	}

	public override void _Process(double delta) {
		Log.Me(() => $"Processing {InstanceID}...", LogProcess);

		UpdateAnimations(LogProcess);

		Log.Me(() => "Done!", LogProcess);
	}

	public override void _PhysicsProcess(double delta) {
		Log.Me(() => $"Processing physics for {InstanceID}...", LogPhysics);

		Move(delta, LogPhysics);

		Log.Me(() => "Done!", LogPhysics);
	}

	#endregion
}
