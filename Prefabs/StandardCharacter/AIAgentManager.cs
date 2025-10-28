using Godot;
namespace CommonScripts;

public partial class AIAgentManager : Node2D {
	[Export] public StandardCharacter Character = null!;
	[Export] public ControlSurface ControlSurface = null!;
	[Export] public NavigationAgent2D NavAgent = null!;

	private Master Master => GetTree().Root.GetNode<Master>("Master");

	[Export] public bool LogReady = true;
	[Export] public bool LogProcess = false;
	[Export] public bool LogPhysics = false;

	public Vector2 TargetPosition => NavAgent.TargetPosition;

	private bool _hasTarget = false;

	public bool IsSelected { get; set; } = false;

	private void InputListener(string actionName, Variant args = new()) {
		Log.Me(() => $"{Character.InstanceID} received action command: {actionName}.", true);

		if (args.VariantType != Variant.Type.Vector2) return;

		Vector2 mousePos = (Vector2) args;

		switch (actionName) {
			case "select":
				Select(mousePos);
				break;

			case "cancel":
				Deselect(mousePos);
				break;
		}
	}

	public bool CheckIfClickedOn(Vector2 target) {
		var clickArea = Character.ClickArea;
		var shapeNode = clickArea.GetNode<CollisionShape2D>("CollisionShape2D"); // Apparently null?
		var shape = shapeNode.Shape;

		Vector2 localPoint = clickArea.ToLocal(target);
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

	public void Select(Vector2 mousePos) {
		bool clickedOn = CheckIfClickedOn(mousePos);
		Log.Me(() => $"{Character.InstanceID} -> clickedOn: {clickedOn}, IsSelected: {IsSelected}.", true);

		// Select if clicked on.
		if (!IsSelected && clickedOn) IsSelected = true;

		// Start moving if clicked elsewhere.
		else if (IsSelected && !clickedOn) GoTo(mousePos);
	}

	public void Deselect(Vector2 mousePos) {
		if (IsSelected) {
			bool clickedOn = CheckIfClickedOn(mousePos);
			Log.Me(() => $"{Character.InstanceID} -> clickedOn: {clickedOn}.", true);

			// Deselect if clicked on.
			if (clickedOn) IsSelected = false;

			// Stop if clicked elsewhere.
			else Stop();
		}
	}

	public void GoTo(Vector2 target) {
		Log.Me(() => $"{Character.InstanceID} received move command to ({target.X:F2}, {target.Y:F2}).", true);
		NavAgent.TargetPosition = target;
		_hasTarget = true;
	}

	public void Stop() {
		_hasTarget = false;
		NavAgent.TargetPosition = GlobalPosition; // Set target to current position to stop movement.
	}

	private void MoveTo() {
		if (!_hasTarget || NavAgent.IsNavigationFinished()) {
			ControlSurface.MovementDirection = Vector2.Zero;
			ControlSurface.MovementMultiplier = 0f;
			return;
		}

		Vector2 nextPos = NavAgent.GetNextPathPosition();
		Vector2 currentPos = GlobalPosition;
		Vector2 dir = nextPos - currentPos;

		Log.Me(() => $"Heading towards ({dir.X:F2}, {dir.Y:F2})...");
		ControlSurface.MovementDirection = dir;	// Normalized in setter.
		ControlSurface.FacingDirection = dir;	// Normalized in setter.
		ControlSurface.MovementMultiplier = 1f;
		NavAgent.SetVelocity(dir * Character.Speed);

		Log.Me(() => "Done!");
	}

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

		Log.Me(() => $"AIAgentManager is ready for {Character.InstanceID}.", true, enabled: LogReady);
	}

	public override void _PhysicsProcess(double delta) {
		MoveTo();
	}

	#endregion
}
