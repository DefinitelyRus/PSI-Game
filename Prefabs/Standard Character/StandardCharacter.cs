using CommonScripts;
using Godot;

public partial class StandardCharacter : CharacterBody2D
{
	[ExportGroup("Character Properties")]
    [Export] public string CharacterName = "Unnamed Character";

	[Export] public float MaxHealth = 100f;

	[Export] public float MaxSpeed = 64f;

	[Export] public string[] Tags = [];

	[Export] public bool DespawnOnDeath = false;

	[ExportGroup("Nodes & Components")]
	[Export] public ControlSurface Control;
	//[Export] public Node2D CharacterSprite;
	//[Export] public Node2D CharacterHitbox;

	// Called when the node enters the scene tree for the first time.

	[ExportGroup("Live Status")]

    #region Health

	
    private float _currentHealth = 100f;

    [Export] public float CurrentHealth {
		get => _currentHealth;
		set {
			if (value < 0) _currentHealth = 0;
			else if (value > _currentMaxHealth) _currentHealth = _currentMaxHealth;
			else _currentHealth = value;
        }
    }

	private float _currentMaxHealth = 100f;

	[Export] public float CurrentMaxHealth {
		get => _currentMaxHealth;
		set {
			if (value < 0) _currentMaxHealth = 0;
			else if (value > MaxHealth) _currentMaxHealth = MaxHealth;
			else _currentMaxHealth = value;
		}
	}

    /// <summary>
    /// Make this character take damage, apply audio/visual effects, and kill if health reaches 0.
    /// </summary>
    /// <param name="amount">How much health to reduce the health by.</param>
    /// <param name="v">Do verbose logging? Use <c>v</c> to follow the same verbosity as the encapsulating function, if available.</param>
    /// <param name="s">Stack depth. Use <c>0</c> if on a root function, or <c>s + 1</c> if <c>s</c> is available in the encapsulating function.</param>
    public void TakeDamage(float amount, bool v = false, int s = 0) {
		Log.Me($"Giving {amount:F2} to {CharacterName}...", v, s + 1);

		// Checks
		if (amount < 0) {
			Log.Me($"WARN: Cannot give negative damage. Use Heal() instead if this is intended.", v, s + 1);
			return;
		}

		CurrentHealth -= amount;

		//TODO: AVFX here.
		//

		// Kill on 0 health.
		if (CurrentHealth == 0) {
			Log.Me($"Reached 0 health. Killing...", v, s + 1);
			Kill(v, s + 1);
        }

		Log.Me($"{CharacterName} now has {CurrentHealth:F2}/{MaxHealth:F2} health.", v, s + 1);
		Log.Me($"{CharacterName} took {amount:F2} damage. (IsAlive: {IsAlive})", true, s + 1);
        return;
	}

	public void Heal(float amount, bool v = false, int s = 0) {
		Log.Me($"Healing {CharacterName} for {amount:F2}...", v, s + 1);

		// Checks
		if (amount < 0) {
			Log.Me($"WARN: Cannot heal negative amount. Use TakeDamage() instead if this is intended.", v, s + 1);
			return;
		}

		if (!IsAlive) {
			Log.Me($"{CharacterName} is dead. Cannot heal.", v, s + 1);
			return;
		}

		CurrentHealth += amount;

        //TODO: AVFX here.
		//

        // Health cap
        if (CurrentHealth > CurrentMaxHealth) {
			CurrentHealth = CurrentMaxHealth;
		}

		Log.Me($"{CharacterName} now has {CurrentHealth:F2}/{CurrentMaxHealth:F2} health.", v, s + 1);
		Log.Me($"{CharacterName} healed for {amount:F2}.", true, s + 1);
		return;
    }

	public void Kill(bool v = false, int s = 0) {
        Log.Me($"Killing {CharacterName}...", v, s + 1);

        if (!IsAlive) {
			Log.Me($"{CharacterName} is already dead.", v, s + 1);
			return;
        }

		IsAlive = false;
		if (DespawnOnDeath) {
			Log.Me("Queueing despawn...", v, s + 1);
			QueueFree();
		}

        //TODO: AVFX here.
		//

        Log.Me($"Killed {CharacterName}.", true, s + 1);
        return;
	}

    #endregion

    #region Speed

    private float _currentSpeed = 0f;

    [Export] public float CurrentSpeed {
		get => _currentSpeed;
		set {
			if (value < 0) _currentSpeed = 0;
			else if (value > CurrentMaxSpeed) _currentSpeed = CurrentMaxSpeed;
			else _currentSpeed = value;
        }
	}

	private float _currentMaxSpeed = 64f;

    [Export] public float CurrentMaxSpeed {
		get => _currentMaxSpeed;
		set {
			if (value < 0) _currentMaxSpeed = 0;
			else if (value > MaxSpeed) _currentMaxSpeed = MaxSpeed;
			else _currentMaxSpeed = value;
        }
    }

    #endregion

    [Export] public bool IsAlive = true;



    #region Godot Callbacks

    public override void _Ready() {
		Log.Me($"Readying \"{CharacterName}\"...");
		DespawnOnDeath = true;
		TakeDamage(101, true, 0);
		Log.Me("Done!");
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {

	}

    public override void _PhysicsProcess(double delta) {
        #region Movement



        #endregion
    }

    #endregion
}
