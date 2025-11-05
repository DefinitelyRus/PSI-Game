using Godot;
namespace CommonScripts;

public partial class AIAgentManager : Node2D {

	#region Nodes & Components
	
	[ExportGroup("Nodes & Components")]
	[Export] public StandardCharacter Character = null!;
	[Export] public ControlSurface ControlSurface = null!;
	[Export] public NavigationAgent2D NavAgent = null!;

	private Master Master => GetTree().Root.GetNode<Master>("Master");

	#endregion

	#region Debugging

	[Export] public bool LogReady = true;
	[Export] public bool LogProcess = false;
	[Export] public bool LogPhysics = false;
	[Export] public bool LogInput = false;

	#endregion

	#region Inputs

	public Vector2 TargetPosition => NavAgent.TargetPosition;
	private bool _hasDestination = false;
	public bool IsSelected { get; set; } = false;

	public bool Searching = false;
	public bool Targeting = false;
	public PhysicsBody2D? CurrentTarget = null;


	private void InputListener(string actionName, Variant args = new()) {
		Log.Me(() => $"{Character.InstanceID} received action command: {actionName}.", LogInput);

		Vector2 mousePos = (Vector2) args;

		switch (actionName) {
			case IM.LeftClick:
				if (args.VariantType != Variant.Type.Vector2) {
					Log.Err(() => $"{Character.InstanceID} received Variant of type {args.VariantType} on a {Variant.Type.Vector2} parameter.");
					break;
				}

				Action1(mousePos);
				break;

			case IM.RightClick:
				if (args.VariantType != Variant.Type.Vector2)  {
					Log.Err(() => $"{Character.InstanceID} received Variant of type {args.VariantType} on a {Variant.Type.Vector2} parameter.");
					break;
				}

				Action2(mousePos);
				break;

			case "stop_action":
				Stop();
				break;
		}
	}


	private bool CheckIfClickedOn(Vector2 mousePos) {
		Area2D clickArea = Character.ClickArea;
		CollisionShape2D shapeNode = clickArea.GetNode<CollisionShape2D>("CollisionShape2D");
		Shape2D shape = shapeNode.Shape;

		Vector2 localPoint = clickArea.ToLocal(mousePos);
		bool isInside = false;

		switch (shape) {
			case RectangleShape2D rect:
				isInside = new Rect2(-rect.Size / 2, rect.Size).HasPoint(localPoint);
				break;

			case CircleShape2D circle:
				isInside = localPoint.Length() <= circle.Radius;
				break;
		}

		return isInside;
	}


	public void Action1(Vector2 mousePos) {
		/*
		 * Left click should:
		 * - Select the character if clicked on it.
		 * - Move to the position if clicked elsewhere while selected.
		 *   - Pause and attack if an enemy is within range (Not implemented yet).
		 *   - Pause and interact if an interactable object is within range (Not implemented yet).
		 */

		bool clickedOn = CheckIfClickedOn(mousePos);

		// Select if clicked on.
		if (clickedOn) {
			IsSelected = true;
		}

		// Move to position if clicked elsewhere while already selected.
		else if (IsSelected) {
			GoTo(mousePos);
			Searching = true;
		}
	}


	public void Action2(Vector2 mousePos) {
		/*
		* Right click should:
		* - Deselect the character if clicked on it.
		* - Move to the position if clicked elsewhere while selected.
		*   - Does not pause to attack or interact.
		* - Approach and attack if an enemy is clicked on while selected (Not implemented yet).
		* - Approach and interact if an interactable object is clicked on while selected (Not implemented yet).
		*/

		if (IsSelected) {
			bool clickedOn = CheckIfClickedOn(mousePos);

			// Deselect if clicked on.
			if (clickedOn) IsSelected = false;

			// Move to position if clicked elsewhere while selected.
			else {
				GoTo(mousePos);
			}

			// If an enemy or interactable object is clicked on, assign it as the current target.
			EntityManager.HasEntityAtPosition(mousePos, out CurrentTarget);
		}
	}

	#endregion

	#region Navigation

	public void GoTo(Vector2 target) {
		Log.Me(() => $"{Character.InstanceID} received move command to ({target.X:F2}, {target.Y:F2}).", LogInput);
		_hasDestination = true;
		NavAgent.TargetPosition = target;
	}


	public void Stop() {
		if (!IsSelected) return;
		
		_hasDestination = false;
		NavAgent.TargetPosition = GlobalPosition;
		NavAgent.Velocity = Vector2.Zero;
	}


	private void MoveTo() {
		if (!_hasDestination || NavAgent.IsNavigationFinished()) {
			ControlSurface.MovementDirection = Vector2.Zero;
			ControlSurface.MovementMultiplier = 0f;
			NavAgent.Velocity = Vector2.Zero;
			return;
		}

		if (!NavAgent.IsTargetReachable()) {
			Log.Me(() => $"{Character.InstanceID} cannot reach target at ({NavAgent.TargetPosition.X:F2}, {NavAgent.TargetPosition.Y:F2}). Stopping movement.", LogInput);
			Stop();
			return;
		}

		Vector2 nextPos = NavAgent.GetNextPathPosition();
		Vector2 currentPos = GlobalPosition;
		Vector2 dir = nextPos - currentPos;

		if (dir == Vector2.Zero) {
			ControlSurface.MovementDirection = Vector2.Zero;
			ControlSurface.MovementMultiplier = 0f;
			return;
		}

		ControlSurface.MovementDirection = dir; // Normalized in setter.
		ControlSurface.FacingDirection = dir;   // Normalized in setter.
		ControlSurface.MovementMultiplier = 1f;
		NavAgent.SetVelocity(dir * Character.Speed);
	}

	#endregion
	
	#region Godot Callbacks

	public override void _EnterTree() {
		if (Master == null) {
			Log.Err(() => "Master node not found in scene tree. Cannot proceed.");
			return;
		}

		if (Character == null) {
			Log.Err(() => "StandardCharacter is not assigned. Cannot proceed.");
			return;
		}

		if (ControlSurface == null) {
			Log.Err(() => $"ControlSurface is not assigned for {Character.InstanceID}.");
			return;
		}

		if (NavAgent == null) {
			Log.Err(() => $"NavigationAgent2D is not assigned for {Character.InstanceID}.");
			return;
		}
	}


	public override void _Ready() {
		// Connect InputListener to the ActionCommand signal.
		StringName signal = nameof(InputManager.ActionCommand);
		Callable callable = new(this, nameof(InputListener));
		Master.InputManager.Connect(signal, callable);

		Log.Me(() => $"AIAgentManager is ready for {Character.InstanceID}.", LogReady);
	}

	public override void _PhysicsProcess(double delta) {
		MoveTo();
	}

	#endregion
}
