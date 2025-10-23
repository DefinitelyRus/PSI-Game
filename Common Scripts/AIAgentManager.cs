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

	public void GoTo(Vector2 target, bool v = false, int s = 0) {
		Log.Me(() => $"Setting target to ({target.X:F2}, {target.Y:F2})...", v, s + 1);
		NavAgent.TargetPosition = target;
		_hasTarget = true;

		Log.Me(() => "Done!", v, s + 1);
	}

	public void Stop(bool v = false, int s = 0) {
		Log.Me(() => "Clearing target...", v, s + 1);
		_hasTarget = false;
		NavAgent.TargetPosition = GlobalPosition; // Set target to current position to stop movement.
		Log.Me(() => "Done!", v, s + 1);
	}

	private void MoveTo(bool v = false, int s = 0) {
		if (!_hasTarget || NavAgent.IsNavigationFinished()) {
			ControlSurface.MovementDirection = Vector2.Zero;
			ControlSurface.MovementMultiplier = 0f;
			return;
		}

		Vector2 nextPos = NavAgent.GetNextPathPosition();
		Vector2 currentPos = GlobalPosition;
		Vector2 dir = nextPos - currentPos;

		Log.Me(() => $"Heading towards ({dir.X:F2}, {dir.Y:F2})...", v, s + 1);
		ControlSurface.MovementDirection = dir;	// Normalized in setter.
		ControlSurface.FacingDirection = dir;	// Normalized in setter.
		ControlSurface.MovementMultiplier = 1f;
		NavAgent.SetVelocity(dir * Character.Speed);

		Log.Me(() => "Done!", v, s + 1);
	}

	public override void _Ready() {

		if (Character == null) {
			Log.Err(() => "StandardCharacter is not assigned. Cannot proceed.", LogReady);
			return;
		}

		if (ControlSurface == null) {
			Log.Err(() => $"ControlSurface is not assigned for {Character.InstanceID}.", LogReady);
			return;
		}

		if (NavAgent == null) {
			Log.Err(() => $"NavigationAgent2D is not assigned for {Character.InstanceID}.", LogReady);
			return;
		}

		Log.Me(() => $"AIAgentManager is ready for {Character.InstanceID}.", LogReady);
	}

	public override void _PhysicsProcess(double delta) {
		MoveTo(LogPhysics);
	}
}
