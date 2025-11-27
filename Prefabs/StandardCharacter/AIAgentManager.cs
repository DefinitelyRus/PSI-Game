using System;
using System.Linq;
using Godot;
namespace CommonScripts;

public partial class AIAgentManager : Node2D {

	#region Nodes & Components
	
	[ExportGroup("Nodes & Components")]
	[Export] public ControlSurface ControlSurface = null!;
	[Export] public NavigationAgent2D NavAgent = null!;

	private Master Master => GetTree().Root.GetNode<Master>("Master");
	public StandardCharacter Character => GetParent<StandardCharacter>();

	#endregion

	#region Debugging

	[Export] public bool LogReady = true;
	[Export] public bool LogProcess = false;
	[Export] public bool LogPhysics = false;
	[Export] public bool LogInput = false;

	#endregion

	#region Inputs

	public Vector2 TargetPosition => NavAgent.TargetPosition;
	public bool HasDestination { get; private set; } = false;
	public bool IsSelected { get; set; } = false;

	public bool Searching = false;
	public bool Targeting = false;
	public PhysicsBody2D? CurrentTarget = null;


	private void InputListener(string actionName, Variant args = new()) {
		if (!Character.IsAlive) return;

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
				if (IsSelected) Stop();
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


	// TODO: Do nothing if character is enemy.
	public void Action1(Vector2 mousePos) {
		if (!Character.IsAlive) return;

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
			Targeting = false;
		}
	}

	// TODO: Handle inputs as enemy. (Target this instead of commanding it.)
	public void Action2(Vector2 mousePos) {
		if (!Character.IsAlive) return;

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
			else GoTo(mousePos);

			// If an enemy or interactable object is clicked on, assign it as the current target.
			EntityManager.HasEntityAtPosition(mousePos, out CurrentTarget);
		}
	}

	#endregion

	#region Navigation

	public void GoTo(Vector2 target) {
		if (!IsInstanceValid(this)) return;
		if (!IsInstanceValid(Character)) return;
		if (!Character.IsAlive) return;

		NavAgent.TargetPosition = target;
		bool isReachable = NavAgent.IsTargetReachable();
		bool isUnit = Character.Tags.Contains("Unit");

		HasDestination = true;

		if (!isReachable) {
			Log.Me(() => $"{Character.InstanceID} cannot reach target at ({target.X:F2}, {target.Y:F2}).", LogPhysics);

			if (isUnit) AudioManager.StreamAudio("error");
			HasDestination = false;
			return;
		}

		if (isUnit) {
			UIManager.SpawnIndicator(Character, target);
			AudioManager.StreamAudio("move_command");
		}
	}


	public void Stop() {
		ControlSurface.MovementDirection = Vector2.Zero;
		ControlSurface.MovementMultiplier = 0f;
		NavAgent.Velocity = Vector2.Zero;
		HasDestination = false;
		Searching = false;
		Targeting = false;
	}


	private void MoveTo() {

		if (ShouldStop()) {
			Stop();
			return;
		}

		if (EnemyStop()) {
			Stop();
			return;
		}

		// Soft pause for combat while navigating (does not clear destination).
		if (ShouldPause()) {
			ControlSurface.MovementDirection = Vector2.Zero;
			ControlSurface.MovementMultiplier = 0f;
			NavAgent.Velocity = Vector2.Zero;
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
	
	private bool EnemyStop() {

		if (!IsInstanceValid(Character)) return false;

		// Stop if within combat range of current target.
		AITargetingManager targetingManager = Character.TargetingManager;
		if (targetingManager == null) return false;

		if (targetingManager.CurrentTarget != null) {
			if (!IsInstanceValid(targetingManager.CurrentTarget)) return true;
			
			Vector2 targetPos = targetingManager.CurrentTarget.GlobalPosition;
			float distanceToTarget = GlobalPosition.DistanceTo(targetPos);
			bool isEnemy = Character.Tags.Contains("Enemy");
			bool targetReached = distanceToTarget <= targetingManager.TargetDetectionRadius;

			if (isEnemy && targetReached) return true;
		}

		return false;
	}


	public bool ShouldStop() {

		// Stop if the character instance is invalid.
		if (!IsInstanceValid(Character)) {
			Log.Warn(() => "Character instance is not valid. Stopping movement.");
			return true;
		}

		// Stop if the character is not alive.
		if (!Character.IsAlive) {
			Log.Me(() => $"{Character.InstanceID} is not alive. Stopping movement.", LogPhysics);
			return true;
		}

		// Stop if there is no destination set.
		if (!HasDestination) {
			Log.Me(() => $"{Character.InstanceID} has no destination. Stopping movement.", LogPhysics);
			return true;
		}

		// Stop if the target is unreachable.
		if (!NavAgent.IsTargetReachable()) {
			Log.Me(() => $"{Character.InstanceID} cannot reach target at ({NavAgent.TargetPosition.X:F2}, {NavAgent.TargetPosition.Y:F2}). Stopping movement.", LogPhysics);
			return true;
		}

		// Stop if navigation is finished.
		if (NavAgent.IsNavigationFinished()) {
			Log.Me(() => $"{Character.InstanceID} has finished navigation. Stopping movement.", LogPhysics);
			return true;
		}

		return false;
	}


	private bool ShouldPause() {
		// Only player-controlled characters pause for combat while navigating.
		bool isUnit = Character.Tags.Contains("Unit");
		if (!isUnit) return false;

		AITargetingManager targetingManager = Character.TargetingManager;
		if (targetingManager == null) return false;
		if (targetingManager.CurrentTarget == null) return false;

		Vector2 targetPos = targetingManager.CurrentTarget.GlobalPosition;
		float distanceToTarget = GlobalPosition.DistanceTo(targetPos);
		bool targetReached = distanceToTarget <= targetingManager.TargetDetectionRadius;

		// Within range of target, pause navigation.
		if (targetReached) {
			Log.Me(() => $"{Character.InstanceID} is pausing navigation for combat with target at ({targetPos.X:F2}, {targetPos.Y:F2}).", LogPhysics);
			return true;
		}

		return false;
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
		if (Character.Tags.Contains("Enemy")) GetTree().CreateTimer(1.0f).Timeout += () => GoTo(GlobalPosition);

		Log.Me(() => $"AIAgentManager is ready for {Character.InstanceID}.", LogReady);
	}

	public override void _PhysicsProcess(double delta) {
		MoveTo();
	}

	#endregion

}
