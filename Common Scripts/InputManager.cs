using Godot;
namespace CommonScripts;

public partial class InputManager : Node2D {

    public StandardCharacter Character = null!;
    public ControlSurface Control = null!;

	#region Debugging

	[ExportGroup("Debugging")]
    [Export] protected bool LogReady = true;
    [Export] protected bool LogProcess = false;
    [Export] protected bool LogPhysics = false;

	#endregion

	#region Input Listeners

	#region Movement Inputs

	[ExportGroup("Input Listeners")]
    [ExportSubgroup("Movement Inputs")]
    [Export] public string MoveUp = "move_up";
    [Export] public string MoveDown = "move_down";
    [Export] public string MoveLeft = "move_left";
    [Export] public string MoveRight = "move_right";

    private MovementModes _movementMode = MovementModes.Static;
	[Export] public MovementModes MovementMode {
		get => _movementMode;
		set {
			bool moveFollows = _movementMode == MovementModes.Follow || _movementMode == MovementModes.AlwaysFollow;
			bool faceFollows = _facingMode == FacingModes.Follow || _facingMode == FacingModes.AlwaysFollow;

			if (moveFollows && faceFollows) {
				Log.Warn(() => "Both MovementMode and FacingMode are set to `Follow` or `AlwaysFollow`. Setting to `Static` instead.");
				_movementMode = MovementModes.Static;
			}
			else _movementMode = value;
		}
	}

    [Export] public Vector2 MoveOverride = new();

	/// <summary>
	/// The different ways a character can receive movement direction inputs. <br/><br/>
	/// <b>Important</b>: If either <c>Follow</c> or <c>AlwaysFollow</c> is used,
	/// <see cref="FacingMode"/> <b>must not</b> be set to <c>Follow</c> or <c>AlwaysFollow</c>
	/// as they will recursively call copy other, causing an infinite loop.
	/// </summary>
	public enum MovementModes {
		/// <summary>
		/// Does not change direction.
		/// </summary>
		Static,

		/// <summary>
		/// Controlled by the player through a separate input.
		/// </summary>
		Dedicated,

		/// <summary>
		/// Follows <see cref="ControlSurface.FacingDirection"/> unless overridden.
		/// </summary>
		Follow,

		/// <summary>
		/// Always follows <see cref="ControlSurface.FacingDirection"/>.
		/// </summary>
		AlwaysFollow,

		/// <summary>
		/// Moves toward a specific vector position.
		/// </summary>
		Vector
	}

	// TODO: Allow gamepad inputs.
	private void ReceiveMovementInputs(bool v = false, int s = 0) {
        Log.Me(() => "Listening for movement inputs...", v, s + 1);

        if (!Control.EnableMovement) {
            Log.Me(() => "Movement inputs are disabled. Skipping input processing.", v, s + 1);
            return;
        }

        Control.MovementMultiplier = 0f;
        Vector2 moveDirection = new();

        switch (MovementMode) {

            case MovementModes.Static:  break;

            case MovementModes.Dedicated:
				if (Input.IsActionPressed(MoveUp)) moveDirection += new Vector2(0, -1);
                if (Input.IsActionPressed(MoveDown)) moveDirection += new Vector2(0, 1);
                if (Input.IsActionPressed(MoveLeft)) moveDirection += new Vector2(-1, 0);
                if (Input.IsActionPressed(MoveRight)) moveDirection += new Vector2(1, 0);
                break;

            case MovementModes.Follow:
                if (MoveOverride != Vector2.Zero) {
					Log.Me(() => $"Following `MoveOverride`...", v, s + 1);
					moveDirection = MoveOverride;
                }
                else {
					Log.Me(() => $"Following `Controls.FacingDirection`...", v, s + 1);
					moveDirection = Control.FacingDirection;
				}
				break;

            case MovementModes.AlwaysFollow:
                moveDirection = Control.FacingDirection;
                break;

            case MovementModes.Vector:
                moveDirection = MoveOverride;
				break;

            default:
                Log.Err(() => "An invalid movement mode was used. Cannot update `Controls.MovementDirection`.", v, s + 1);
                return;
        }

		Control.MovementDirection = moveDirection;
        Control.MovementMultiplier = moveDirection.Length();

		Log.Me(() => $"MovementDirection ({moveDirection.X:F2}, {moveDirection.Y:F2}) -> ({Control.MovementDirection.X}, {Control.MovementDirection.Y}) at {Control.MovementMultiplier:F2}x...", v, s + 1);

		Log.Me(() => "Done!", v, s + 1);
    }

	#endregion

	#region Facing Inputs

	[ExportSubgroup("Facing Inputs")]
    [Export] public string FaceUp = "face_up";
    [Export] public string FaceDown = "face_down";
    [Export] public string FaceLeft = "face_left";
    [Export] public string FaceRight = "face_right";

    private FacingModes _facingMode = FacingModes.Static;
	[Export] public FacingModes FacingMode {
        get => _facingMode;
        set {
			bool moveFollows = _movementMode == MovementModes.Follow || _movementMode == MovementModes.AlwaysFollow;
			bool faceFollows = _facingMode == FacingModes.Follow || _facingMode == FacingModes.AlwaysFollow;

			if (moveFollows && faceFollows) {
				Log.Err(() => "Both MovementMode and FacingMode are set to `Follow` or `AlwaysFollow`. Setting to `Static` instead.");
				_facingMode = FacingModes.Static;
			}
			else _facingMode = value;
		}
	}

    [Export] public Vector2 FaceOverride = Vector2.Zero;

	/// <summary>
	/// The different ways a character can receive face direction inputs. <br/><br/>
	/// <b>Important</b>: If either <c>Follow</c> or <c>AlwaysFollow</c> is used,
	/// <see cref="MovementMode"/> <b>must not</b> be set to <c>Follow</c> or <c>AlwaysFollow</c>
	/// as they will recursively call copy other, causing an infinite loop.
	/// </summary>
	public enum FacingModes {
		/// <summary>
		/// Does not change direction.
		/// </summary>
		Static,

		/// <summary>
		/// Controlled by the player through a separate input.
		/// </summary>
		Dedicated,

		/// <summary>
		/// Follows the movement direction unless overridden.
		/// </summary>
		Follow,

		/// <summary>
		/// Always follows the movement direction of the character.
		/// </summary>
		AlwaysFollow,

		/// <summary>
		/// Heads towards a specific vector position.
		/// </summary>
		Vector
	}

	private void ReceiveFacingInputs(bool v = false, int s = 0) {
        Log.Me(() => "Listening for facing inputs...", v, s + 1);

        if (!Control.EnableFaceDirection) {
            Log.Me(() => "Facing inputs are disabled. Skipping input processing.", v, s + 1);
            return;
        }

        Vector2 faceDirection = new();

        switch (FacingMode) {
            case FacingModes.Static: break;

            case FacingModes.Dedicated:
				if (Input.IsActionPressed(FaceUp)) faceDirection += new Vector2(0, -1);
				if (Input.IsActionPressed(FaceDown)) faceDirection += new Vector2(0, 1);
				if (Input.IsActionPressed(FaceLeft)) faceDirection += new Vector2(-1, 0);
				if (Input.IsActionPressed(FaceRight)) faceDirection += new Vector2(1, 0);
                break;

            case FacingModes.Follow:
                if (FaceOverride != Vector2.Zero) {
                    Log.Me(() => $"Following `FaceOverride`...", v, s + 1);
                    faceDirection = FaceOverride;
                }

                else {
                    Log.Me(() => $"Following `Controls.MovementDirection`...", v, s + 1);
                    faceDirection = Control.MovementDirection;
				}
                break;

            case FacingModes.AlwaysFollow:
                faceDirection = Control.MovementDirection;
                break;

            case FacingModes.Vector:
                faceDirection = FaceOverride;
				break;

            default:
                Log.Err(() => "An invalid facing mode was used. Cannot update `Controls.FacingDirection`.", v, s + 1);
                break;
		}

        Control.FacingDirection = faceDirection;
        
        Log.Me(() => $"FacingDirection ({faceDirection.X:F2}, {faceDirection.Y:F2}) -> ({Control.FacingDirection.X}, {Control.FacingDirection.Y})...", v, s + 1);

		Log.Me(() => "Done!", v, s + 1);
	}

	#endregion

	#region Attack Inputs
	
	[ExportSubgroup("Attack Inputs")]
	[Export] public string Attack = "attack";

	private void ReceiveAttackInputs(bool v = false, int s = 0) {
		Log.Me(() => "Listening for attack inputs...", v, s + 1);

		if (!Control.EnableCombat) {
			Log.Me(() => "Combat inputs are disabled. Skipping input processing.", v, s + 1);
			return;
		}

		Control.IsAttacking = Input.IsActionPressed(Attack);
		Control.JustAttacked = Input.IsActionJustPressed(Attack);

		Log.Me(() => "Done!", v, s + 1);
	}

	#endregion

	#endregion

	#region Godot Callbacks

	public override void _EnterTree() {
		Log.Me(() => $"An InputManager has entered the tree. Checking properties...", LogReady);

		Control = GetNode<ControlSurface>("../Control Surface");
		if (Control == null) Log.Err("InputManager must have a ControlSurface sibling. Inputs will not reach its intended target.", LogReady);

		Character = GetNodeOrNull<StandardCharacter>("../");
		if (Character == null) Log.Err("InputManager must be a child of a StandardCharacter. Inputs may not reach its intended target.", LogReady);
	}

	public override void _Process(double delta) {
		Log.Me(() => $"Processing ControlSurface for {Character.InstanceID}...", LogProcess);

		ReceiveMovementInputs(LogProcess);
		ReceiveFacingInputs(LogProcess);
		ReceiveAttackInputs(LogProcess);

		Log.Me(() => "Done!", LogProcess);
	}

    #endregion

}
