using Godot;
namespace CommonScripts;

public partial class AIAgentManager : Node2D {
	[Export] public StandardCharacter Character = null!;
	[Export] public ControlSurface ControlSurface = null!;
	[Export] public NavigationAgent2D NavAgent = null!;

	[Export] public bool LogReady = true;
	[Export] public bool LogProcess = false;
	[Export] public bool LogPhysics = false;

	public Vector2 TargetPosition => NavAgent.TargetPosition;

	private bool _hasTarget = false;

	public void GoTo(Vector2 target, Context c = null!) {
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

	public override void _Ready() {

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

		Log.Me(() => $"AIAgentManager is ready for {Character.InstanceID}.", true, enabled: LogReady);
	}

	public override void _PhysicsProcess(double delta) {
		MoveTo();
	}
}
