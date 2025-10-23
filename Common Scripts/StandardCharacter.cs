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
	[Export] public float AccelerationTime = 0.25f;

	/// <summary>
	/// The time it takes for the character to decelerate to a stop from its maximum speed.
	/// </summary>
	[Export] public float DecelerationTime = 0.2f;

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
	public void TakeDamage(float amount, Context c) {

		// Checks
		if (amount < 0) {
			c.Err($"Cannot give negative damage. Use Heal() instead if this is intended.");
			return;
		}

		Health -= amount;

		//TODO: AVFX here.
		//

		// Kill on 0 health.
		if (Health == 0) ; //Kill(v, s + 1);
		return;
	}

	/// <summary>
	/// Heals the character by the specified amount and applying audio/visual effects if available.
	/// </summary>
	/// <param name="amount">How much health to increase the health by.</param>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	public void Heal(float amount, Context c = null!) {
		// Checks
		if (amount < 0) {
			c.Err($"Cannot heal negative amount. Use TakeDamage() instead if this is intended.");
			return;
		}

		if (!IsAlive) return;

		Health += amount;

		//TODO: AVFX here.
		//

		// Health cap
		if (Health > CurrentMaxHealth) Health = CurrentMaxHealth;

		return;
	}

	/// <summary>
	/// Kills the character and applies audio/visual effects if available.
	/// It will also despawn the character if <see cref="DespawnOnDeath"/> is <c>true</c>.
	/// </summary>
	/// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
	/// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
	public void Kill(Context c = null!) {

		if (!IsAlive) return;

		// Set health to 0
		Health = 0; //Redundancy; for when `Kill` is called without dealing any damage.
		IsAlive = false;

		bool useAnimation = AnimationTree != null && AnimationState != null && AnimationPlayer != null;

		#region AVFX

		if (useAnimation) {
			// Set animation direction
			AnimationTree!.Set("parameters/Death/blend_position", new Vector2(Control.MovementDirection.X, -Control.MovementDirection.Y));

			// Set animation state, call lambda function if DespawnOnDeath
			AnimationState!.Travel("Death");


			var playback = (AnimationNodeStateMachinePlayback)AnimationTree.Get("parameters/playback");

			//TODO: Fix!
			// Since AnimationPlayer does not control the animation and AnimationTree does not emit the AnimationFinished signal,
			// the only ways to detect the end of the animation are to either poll the animation state or use a call_method animation track.
			if (DespawnOnDeath) {
				AnimationPlayer!.AnimationFinished += animation => {
					if (animation != "Death") return;
					QueueFree();

					//AVFX here.
				};
			}
		}

		else if (DespawnOnDeath) {
			QueueFree();

			//AVFX here.
		}

		#endregion

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
	private void Move(double delta, Context c = null!) {
		// Accelerate if input is detected
		if (Control.MovementMultiplier > 0f) Speed += (float) (Control.MovementMultiplier * MaxSpeed * (delta / AccelerationTime));

		// Calculate deceleration
		else if (Speed > 0f) Speed -= (float) (MaxSpeed * (delta / DecelerationTime));

		//Apply speed
		Velocity = Speed * LastMovementDirection;
		MoveAndSlide();

		if (Control.MovementDirection != Vector2.Zero) LastMovementDirection = Control.MovementDirection;
	}

	#endregion

	#endregion

	#region AVFX

	/// <summary>
	/// Updates the sprite animations based on the current player state. <br/><br/>
	/// <b>Important</b>: This method is meant to be called in <see cref="_Process"/> only. <br/><br/>
	/// <b>Warning</b>: This current implementation is <b>project-specific</b>. It only supports walking, shooting, and idle.
	/// </summary>
	protected void UpdateAnimations(Context c = null!) {
		if (!IsAlive) return;

		bool isWalking = Speed > 0;
		bool isAttacking = Weapon.Control.IsAttacking;
		//^ TECHNICAL DEBT: This is inherently bugged. Animates attack even when not intended.
		//					But so is the attack function itself.
		//TODO: Replace with an IsAttacking property in StandardWeapon itself.

		if (isWalking) {
			AnimationTree.Set("parameters/Walk/blend_position", new Vector2(Control.MovementDirection.X, -Control.MovementDirection.Y));
			AnimationState.Travel("Walk");
			AnimationPlayer.SpeedScale = Speed / CurrentMaxSpeed;
		}

		if (isAttacking) {
			AnimationTree.Set("parameters/Attack/blend_position", new Vector2(Control.FacingDirection.X, -Control.FacingDirection.Y));
			AnimationState.Travel("Attack");
		}

		if (!isWalking && !isAttacking) {
			AnimationTree.Set("parameters/Idle/blend_position", new Vector2(Control.FacingDirection.X, -Control.FacingDirection.Y));
			AnimationState.Travel("Idle");
		}
	}

	#endregion

	#region Nodes & Components

	[ExportGroup("Nodes & Components")]
	[Export] public ControlSurface Control = null!;
	[Export] public StandardWeapon Weapon = null!;
	[Export] public HitArea HitArea = null!;
	[Export] public AnimationPlayer AnimationPlayer = null!;
	[Export] public AnimationTree AnimationTree = null!;
	public AnimationNodeStateMachinePlayback AnimationState => (AnimationNodeStateMachinePlayback) AnimationTree.Get("parameters/playback");

	#endregion

	#region Debugging

	[ExportGroup("Debugging")]
	[Export] protected bool LogReady = false;
	[Export] protected bool LogProcess = false;
	[Export] protected bool LogPhysics = false;
	[Export] protected bool LogCollision = false;

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
	private void GenerateInstanceID(Context c = null!) {
		// Cancel if already assigned.
		if (!string.IsNullOrEmpty(InstanceID)) return;

		string prefix;
		string randomID = string.Empty;

		// Use default name if CharacterName blank.
		if (string.IsNullOrEmpty(CharacterName)) {
			c.Warn("`CharacterName` is empty. Using default: \"Unnamed Character\"");
			CharacterName = "Unnamed Character";
		}

		// Use CharacterName if CustomPrefix is blank.
		if (string.IsNullOrEmpty(CustomPrefix)) prefix = CharacterName;

		// Use CustomerPrefix if provided.
		else prefix = CustomPrefix;

		// Replace space with the specified type.
		switch (ReplaceSpaceWith) {

			case SpaceReplacement.Keep: break;

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
				c.Warn("An invalid `SpaceReplacement` value was provided. Keeping spaces instead...");
				break;

		}

		GenerateCharacterID:
		for (int i = 0; i < SuffixLength; i++) {
			randomID += SuffixChars[(int) (GD.Randi() % SuffixChars.Length)];
		}

		InstanceID = prefix + Separator + randomID;

		// Check if the ID is already taken
		if (GetNodeOrNull<StandardCharacter>(InstanceID) != null) {
			randomID = string.Empty;
			goto GenerateCharacterID;
		}
	}

	#endregion

	#region Ignore Unassigned Values

	[ExportSubgroup("Ignore Unassigned Values")]
	[Export] public bool SilentlyAutoAssignDefaultName = false;
	[Export] public bool AllowNoWeapon = false;
	[Export] public bool AllowNoHitArea = false;
	[Export] public bool AllowNoAnimationPlayer = false;
	[Export] public bool AllowNoAnimationTree = false;
	[Export] public bool SilentlyAutoAssignInstanceID = true;

	#endregion

	#endregion

	#region Overridable Methods

	public virtual void OnAreaEntered(Area2D area)
	{
		if (area.GetParent() is StandardProjectile projectile) return;

		new Context().Warn(() => $"`OnAreaEntered` on {CharacterName} (CharacterID: {CharacterID}) is not implemented! Override to add custom functionality.");
		return;
	}

	#endregion

	#region Godot Callbacks

	public override void _EnterTree() {
		Context c = new();
		c.Trace($"A StandardCharacter has entered the tree. Checking properties...", LogReady);

		if (string.IsNullOrEmpty(CharacterName)) {
			if (!SilentlyAutoAssignDefaultName) c.Warn("CharacterName should not be empty. Using default: \"Unnamed Character\"...");
			CharacterName = "Unnamed Character";
		}

		if (Control == null) c.Err("ControlSurface is not assigned. This character cannot be controlled.");

		if (Weapon == null && !AllowNoWeapon) c.Warn("StandardWeapon is not assigned. This character cannot attack.");

		if (HitArea == null && !AllowNoHitArea) c.Warn("HitArea is not assigned. This character will have buggy hit behavior.");

		if (AnimationPlayer == null && !AllowNoAnimationPlayer) c.Warn("AnimationPlayer is not assigned. This character will not be animated.");

		if (AnimationTree == null && !AllowNoAnimationTree) c.Warn("AnimationTree is not assigned. This character will not be animated properly.");

		if (string.IsNullOrEmpty(InstanceID)) {
			if (!SilentlyAutoAssignInstanceID) c.Trace("InstanceID is not assigned. Generating a new one...", LogReady);
			GenerateInstanceID(c);
		}

		c.Trace("Done!", LogReady);
		c.End();
	}

	public override void _Ready()
	{
		Context c = new();
		c.Trace(() => $"Readying {InstanceID}...", LogReady);

		c.Trace(() => $"Changing node name to \"{InstanceID}\"...", LogReady);
		Name = InstanceID;

		c.Trace(() => "Connecting HitArea.BodyEntered to OnBodyEntered...", LogReady);
		HitArea.AreaEntered += OnAreaEntered;

		c.Trace(() => "Done!", LogReady);
		c.End();
		return;
	}

	public override void _Process(double delta) {
		Context c = new();

		UpdateAnimations(c);

		c.End();
	}

	public override void _PhysicsProcess(double delta) {
		Context c = new();

		Move(delta, c);

		c.End();
	}

	#endregion
}
