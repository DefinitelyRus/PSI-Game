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
				new Ctx().Warn(() => "Both MovementMode and FacingMode are set to `Follow` or `AlwaysFollow`. Setting to `Static` instead.");
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
		/// Faces towards the cursor position.
		/// </summary>
		Mouse,

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
	private void ReceiveMovementInputs(Context c = null!) {
		if (!Control.EnableMovement) return;

		Control.MovementMultiplier = 0f;
		Vector2 moveDirection = new();

		switch (MovementMode) {

			case MovementModes.Static: break;

			case MovementModes.Mouse:
				moveDirection = GetGlobalMousePosition() - Control.GlobalPosition;
				break;

			case MovementModes.Dedicated:
				if (Input.IsActionPressed(MoveUp)) moveDirection += new Vector2(0, -1);
				if (Input.IsActionPressed(MoveDown)) moveDirection += new Vector2(0, 1);
				if (Input.IsActionPressed(MoveLeft)) moveDirection += new Vector2(-1, 0);
				if (Input.IsActionPressed(MoveRight)) moveDirection += new Vector2(1, 0);
				break;

			case MovementModes.Follow:
				if (MoveOverride != Vector2.Zero) moveDirection = MoveOverride;
				else moveDirection = Control.FacingDirection;
				break;

			case MovementModes.AlwaysFollow:
				moveDirection = Control.FacingDirection;
				break;

			case MovementModes.Vector:
				moveDirection = MoveOverride;
				break;

			default:
				c.Err(() => "An invalid movement mode was used. Cannot update `Controls.MovementDirection`.");
				return;
		}

		Control.MovementDirection = moveDirection;
		Control.MovementMultiplier = moveDirection.Length();
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
				new Ctx().Err(() => "Both MovementMode and FacingMode are set to `Follow` or `AlwaysFollow`. Setting to `Static` instead.");
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
		/// Faces towards the cursor position.
		/// </summary>
		Mouse,

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

	private void ReceiveFacingInputs(Context c = null!) {
		if (!Control.EnableFaceDirection) return;

		Vector2 faceDirection = new();

		switch (FacingMode) {
			case FacingModes.Static: break;

			case FacingModes.Mouse:
				faceDirection = GetGlobalMousePosition() - Control.GlobalPosition;
				break;

			case FacingModes.Dedicated:
				if (Input.IsActionPressed(FaceUp)) faceDirection += new Vector2(0, -1);
				if (Input.IsActionPressed(FaceDown)) faceDirection += new Vector2(0, 1);
				if (Input.IsActionPressed(FaceLeft)) faceDirection += new Vector2(-1, 0);
				if (Input.IsActionPressed(FaceRight)) faceDirection += new Vector2(1, 0);
				break;

			case FacingModes.Follow:
				if (FaceOverride != Vector2.Zero) faceDirection = FaceOverride;
				else faceDirection = Control.MovementDirection;
				break;

			case FacingModes.AlwaysFollow:
				faceDirection = Control.MovementDirection;
				break;

			case FacingModes.Vector:
				faceDirection = FaceOverride;
				break;

			default:
				c.Err(() => "An invalid facing mode was used. Cannot update `Controls.FacingDirection`.");
				break;
		}

		Control.FacingDirection = faceDirection;
	}

	#endregion

	#region Attack Inputs
	
	[ExportSubgroup("Attack Inputs")]
	[Export] public string Attack = "attack";

	private void ReceiveAttackInputs(Context c = null!) {
		if (!Control.EnableCombat) return;

		Control.IsAttacking = Input.IsActionPressed(Attack);
		Control.JustAttacked = Input.IsActionJustPressed(Attack);
	}

	#endregion

	#endregion

	#region Godot Callbacks

	public override void _EnterTree() {
		Context c = new();
		c.Trace(() => $"An InputManager has entered the tree. Checking properties...", LogReady);

		Control = GetNode<ControlSurface>("../Control Surface");
		if (Control == null) c.Err("InputManager must have a ControlSurface sibling. Inputs will not reach its intended target.", LogReady);

		Character = GetNodeOrNull<StandardCharacter>("../");
		if (Character == null) c.Err("InputManager must be a child of a StandardCharacter. Inputs may not reach its intended target.", LogReady);
		c.End();
	}

	public override void _Process(double delta) {
		Context c = new();
		c.Trace(() => $"Processing ControlSurface for {Character.InstanceID}...", LogProcess);

		ReceiveMovementInputs(c);
		ReceiveFacingInputs(c);
		ReceiveAttackInputs(c);

		c.Trace(() => "Done!", LogProcess);
		c.End();
	}

	#endregion

}
