using Godot;
namespace CommonScripts;

public partial class StandardCharacter : CharacterBody2D
{
    #region Character Properties

    /// <summary>
    /// The name of the character. <br/><br/>
    /// This name is used for logging and debugging purposes, and can be used to identify the character in the game.<br/>
    /// It is not unique and can be shared by multiple characters.
    /// If such a unique identifier is needed, use <see cref="CharacterID"/> instead.<br/><br/>
    /// </summary>
    [ExportGroup("Character Properties")]
    [Export] public string CharacterName = "Unnamed Character";

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

    #region Nodes & Components

    [ExportGroup("Nodes & Components")]
	[Export] public ControlSurface Control;
    //[Export] public Node2D CharacterSprite;
    //[Export] public Node2D CharacterHitbox;

    // Called when the node enters the scene tree for the first time.

    #endregion

    #region Live Status

    #region Health

    /// <summary>
    /// The current health of the character. <br/><br/>
    /// Do not use this value directly except for getters and setters, as it is not clamped.<br/>
    /// </summary>
    [ExportGroup("Live Status")]
    [ExportSubgroup("Health")]
	private float _health = 100f;

    /// <summary>
    /// The current health of the character. <br/><br/>
	/// This is a getter/setter for <see cref="_health"/>.
	/// Setting this value will clamp it to the range of <c>0</c> to <see cref="CurrentMaxHealth"/>.
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
	/// Setting this value will clamp it to the range of <c>0</c> to <see cref="float.MaxValue"/>.<br/>
    /// </summary>
    [Export] public float CurrentMaxHealth {
		get => _currentMaxHealth;
		set { _currentMaxHealth = Mathf.Clamp(value, 0, float.MaxValue); }
	}

    /// <summary>
    /// Make this character take damage, apply audio/visual effects, and kill if health reaches 0.
    /// </summary>
    /// <param name="amount">How much health to reduce the health by.</param>
    /// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
    /// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
    public void TakeDamage(float amount, bool v = false, int s = 0) {
		Log.Me($"Giving {amount:F2} to {CharacterID}...", v, s + 1);

		// Checks
		if (amount < 0) {
			Log.Me($"WARN: Cannot give negative damage. Use Heal() instead if this is intended.", v, s + 1);
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

		Log.Me($"{CharacterID} now has {Health:F2}/{MaxHealth:F2} health.", v, s + 1);
		Log.Me($"{CharacterID} took {amount:F2} damage. (IsAlive: {IsAlive})", true, s + 1);
        return;
	}

    /// <summary>
    /// Heals the character by the specified amount and applying audio/visual effects if available.
    /// </summary>
    /// <param name="amount">How much health to increase the health by.</param>
    /// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
    /// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
    public void Heal(float amount, bool v = false, int s = 0) {
		Log.Me($"Healing {CharacterID} for {amount:F2}...", v, s + 1);

		// Checks
		if (amount < 0) {
			Log.Me($"WARN: Cannot heal negative amount. Use TakeDamage() instead if this is intended.", v, s + 1);
			return;
		}

		if (!IsAlive) {
			Log.Me($"{CharacterID} is dead. Cannot heal.", v, s + 1);
			return;
		}

		Health += amount;

        //TODO: AVFX here.
		//

        // Health cap
        if (Health > CurrentMaxHealth) {
			Health = CurrentMaxHealth;
		}

		Log.Me($"{CharacterID} now has {Health:F2}/{CurrentMaxHealth:F2} health.", v, s + 1);
		Log.Me($"{CharacterID} healed for {amount:F2}.", true, s + 1);
		return;
    }

    /// <summary>
    /// Kills the character and applies audio/visual effects if available.
	/// It will also despawn the character if <see cref="DespawnOnDeath"/> is <c>true</c>.
    /// </summary>
    /// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
    /// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
    public void Kill(bool v = false, int s = 0) {
        Log.Me($"Killing {CharacterID}...", v, s + 1);

        if (!IsAlive) {
			Log.Me($"{CharacterID} is already dead.", v, s + 1);
			return;
        }

		IsAlive = false;
		if (DespawnOnDeath) {
			Log.Me("Queueing despawn...", v, s + 1);
			QueueFree();
		}

        //TODO: AVFX here.
		//

        Log.Me($"Killed {CharacterID}.", true, s + 1);
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
        else if (Velocity != Vector2.Zero) {
            Log.Me(() => $"Decelerating...", v, s + 1);
			Speed -= (float) (MaxSpeed * (delta / DecelerationTime));
            Log.Me(() => $"Speed after deceleration: {Speed:F2}", v, s + 1);
        }

        //Apply speed
        Velocity = Speed * Control.MovementDirection;
        MoveAndSlide();

		Log.Me(() => "Done!", v, s + 1);
    }

	#endregion

	/// <summary>
	/// The character's unique identifier. <br/><br/>
	/// </summary>
	[ExportSubgroup("Others")]

	[Export] public bool IsAlive = true;

	[ExportSubgroup("Character ID")]
	[Export] public string CharacterID = string.Empty;

    [Export] public string CustomPrefix = string.Empty;

    public enum SpaceReplacement {
        Keep,
		Remove,
		Underscore,
        Hyphen
	}

    [Export] public SpaceReplacement ReplaceSpaceWith = SpaceReplacement.Remove;

	[Export] public string Separator = "#";

	[Export] public string SuffixChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    [Export] public int SuffixLength = 5;

	private void GenerateCharacterID(bool v = false, int s = 0) {
        Log.Me($"Generating a `CharacterID`...", v, s + 1);

        // Cancel if already assigned.
        if (!string.IsNullOrEmpty(CharacterID)) {
            Log.Me($"`CharacterID` is already assigned a value (\"{CharacterID}\"). Skipping...", v, s + 1);
            return;
        }

        string prefix;
        string randomID = string.Empty;

        // Use default name if CharacterName blank.
        if (string.IsNullOrEmpty(CharacterName)) {
            Log.Me("WARN: `CharacterName` is empty. Using default: \"Unnamed Character\"", v, s + 1);
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
                Log.Me("WARN: An invalid `SpaceReplacement` value was provided. Keeping spaces instead...", v, s + 1);
                break;

        }

        GenerateCharacterID:
        Log.Me("Generating a unique ID...", v, s + 1);
        for (int i = 0; i < SuffixLength; i++) {
            randomID += SuffixChars[(int) (GD.Randi() % SuffixChars.Length)];
        }

        CharacterID = prefix + Separator + randomID;

		// Check if the ID is already taken
		if (GetNodeOrNull<StandardCharacter>(CharacterID) != null) {
		    Log.Me($"Character ID \"{randomID}\" is already taken. Generating a new one...", v, s + 1);
		    randomID = string.Empty;
		    goto GenerateCharacterID;
		}

        Log.Me($"Generated ID \"{CharacterID}\"!", v, s + 1);
	}

	#endregion

	#region Debugging

	[ExportGroup("Debugging")]
	[Export] protected bool LogReady = true;
	[Export] protected bool LogProcess = false;
	[Export] protected bool LogPhysics = false;

	#endregion

	#region Godot Callbacks

	public override void _Ready() {
		Log.Me($"Readying \"{CharacterID}\"...");

		GenerateCharacterID(LogReady);

		Log.Me("Done!");
    }
    
	public override void _Process(double delta) {

	}

    public override void _PhysicsProcess(double delta) {
        Log.Me(() => $"Processing physics for {CharacterID}...", LogPhysics);

		Move(delta, LogPhysics);

        Log.Me(() => "Done!", LogPhysics);
	}

    #endregion
}
