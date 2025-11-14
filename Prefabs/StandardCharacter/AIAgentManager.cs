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
	public bool HasDestination => TargetPosition != GlobalPosition;
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
		if (!Character.IsAlive) return;

		NavAgent.TargetPosition = target;
	}


	public void Stop() {
		ControlSurface.MovementDirection = Vector2.Zero;
		ControlSurface.MovementMultiplier = 0f;
		NavAgent.TargetPosition = GlobalPosition;
		NavAgent.Velocity = Vector2.Zero;
	}


	private void MoveTo() {

		#region Validation

		if (!IsInstanceValid(Character)) Log.Warn(() => "Character instance is not valid. Behavior may be abnormal.");

		bool deadCharacter = !Character.IsAlive;
		bool noDestination = !HasDestination;
		bool targetUnreachable = !NavAgent.IsTargetReachable();
		bool doneNavigating = NavAgent.IsNavigationFinished();
		
		// Stop if any of the conditions are met.
		if (deadCharacter || noDestination || doneNavigating) {
			Stop();
			return;
		}

		if (targetUnreachable) {
			Log.Me(() => $"{Character.InstanceID} cannot reach target at ({NavAgent.TargetPosition.X:F2}, {NavAgent.TargetPosition.Y:F2}). Stopping movement.", LogInput);
			Stop();
			return;
		}

		#endregion

		// If within weapon range, stop moving and target.
		AITargetingManager targetingManager = Character.GetNode<AITargetingManager>("AI Targeting Manager");
		if (targetingManager.CurrentTarget != null) {
			float distanceToTarget = GlobalPosition.DistanceTo(targetingManager.CurrentTarget.GlobalPosition);

			// If within target detection radius, stop and target.\
			bool isPlayer = Character.Tags.Contains("Player");
			bool isEnemy = Character.Tags.Contains("Enemy");
			bool targetReached = distanceToTarget <= targetingManager.TargetDetectionRadius;
			bool playerFoundEnemy = isPlayer && Searching && targetReached;
			bool enemyFoundPlayer = isEnemy && targetReached;

			if (playerFoundEnemy || enemyFoundPlayer) {
				OnTargetReached();
				return;
			}
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

	private void OnTargetReached() {
		ControlSurface.MovementDirection = Vector2.Zero;
		ControlSurface.MovementMultiplier = 0f;
		NavAgent.Velocity = Vector2.Zero;
		Searching = false;
		Targeting = false;
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
