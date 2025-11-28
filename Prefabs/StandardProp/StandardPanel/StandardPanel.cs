using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
namespace CommonScripts;

public partial class StandardPanel : StandardProp {
	[Export] public Area2D ClickArea = null!;
	[Export] public Area2D ActivationArea = null!;
	[Export] public int ActivationRadius = 32;
	[Export] public AudioStream? ActivationSound = null;

	public enum PanelTypes {
        Required,
		SingleUse
    }

	public enum ActivationMethods {
        Radius,
		Area
    }

	[Export] public PanelTypes PanelType = PanelTypes.Required;
	[Export] public ActivationMethods ActivationMethod = ActivationMethods.Radius;

	[Export] public bool Activated = false;
	[Export] public bool IsEnabled = true;


	public void ScanForPlayer() {
		if (!IsEnabled) return;

		// Check if there are any characters within the activation radius
		StandardCharacter? closestCharacter = null;
		foreach (StandardCharacter character in Commander.GetAllUnits()) {
			if (!character.IsAlive) continue;

			bool isUnitActivating = ScanForUnit(character);
			if (!isUnitActivating) continue;

			if (closestCharacter == null) {
				closestCharacter = character;
				continue;
			}

			// Check if this character is closer than the current closest
			float currentClosestDistance = closestCharacter.GlobalPosition.DistanceTo(GlobalPosition);
			float newDistance = character.GlobalPosition.DistanceTo(GlobalPosition);
			if (newDistance < currentClosestDistance) closestCharacter = character;
		}

		if (closestCharacter != null) {
			Activated = true;
			Interact(closestCharacter);
		}
	}


	public bool ScanForUnit(StandardCharacter unit) {
		switch (ActivationMethod) {
			case ActivationMethods.Area:
				if (ActivationArea.OverlapsBody(unit)) return true;
				break;

			case ActivationMethods.Radius:
				float distance = unit.GlobalPosition.DistanceTo(GlobalPosition);
				if (distance <= ActivationRadius) return true;

				break;
		}

		return false;
	}


	[Export] private double HighlightSpeed = 0.5d;
	private double _highlightAmount = 1d;
	private int _highlightDirection = -1;


	/// <summary>
    /// Highlights the panel by oscillating its brightness.
    /// </summary>
	protected void HighlightPanel(double delta) {
		if (Sprite == null) {
			Log.Err(() => "Sprite is not assigned. Cannot highlight panel.");
			return;
		}

		if (!IsEnabled || Activated) {
			Sprite.Modulate = Colors.White;
			_highlightAmount = 1d;
			_highlightDirection = -1;
			return;
		}

		// Update highlight amount
		_highlightAmount += delta * HighlightSpeed * _highlightDirection;

		// Reverse direction at bounds
		if (_highlightAmount >= 1d) {
			_highlightAmount = 1d;
			_highlightDirection = -1;
		}
		
		// Clamp at lower bound
		else if (_highlightAmount <= 0.5d) {
			_highlightAmount = 0.5d;
			_highlightDirection = 1;
		}

		Sprite.Modulate = Colors.Black.Lerp(Colors.White, (float)_highlightAmount);
	}



	/// <summary>
	/// Called when a character interacts with the panel.
	/// </summary>
	/// <remarks>
	/// Override this method to implement custom interaction behavior. <br/>
	/// This method is called every frame that a character is within the activation radius.
	/// </remarks>
	public virtual void Interact(StandardCharacter unit) {
		Log.Warn(() => $"`Interact` on {PropID} is not implemented! Override to add custom functionality.");
		return;
	}

	public override void _EnterTree() {
		base._EnterTree();
	}

	public override void _Process(double delta) {
        ScanForPlayer();
		HighlightPanel(delta);
    }

	public static StandardPanel? ScanForPanel(Vector2 position) {
		StandardPanel[] panels = [.. EntityManager.Entities.OfType<StandardPanel>()];
		foreach (StandardPanel panel in panels) {
			if (!panel.IsEnabled) continue;

			Area2D clickArea = panel.ClickArea;
			CollisionShape2D shape = clickArea.GetChild<CollisionShape2D>(0);

			// Skip if not rectangle
			if (shape.Shape is not RectangleShape2D rectShape) {
				Log.Warn(() => $"{panel.InstanceID}'s {shape.Shape} is not supported for scanning.");
				continue;
            }

			Vector2 size = rectShape.Size;
			Rect2 rect = new(clickArea.GlobalPosition - size / 2, size);

			bool isPointInRect = rect.HasPoint(position);
			if (isPointInRect) return panel;
		}

		return null;
	}

	public static Vector2? GetNavigablePosition(StandardCharacter unit, StandardPanel panel) {
		NavigationAgent2D agent = unit.AIAgent.NavAgent;
		Vector2 panelPos = panel.GlobalPosition;
		RandomNumberGenerator rng = new();
		Vector2 currentUnitTargetPos = agent.TargetPosition;
		int attemptCountRemaining = 10;
		

		switch (panel.ActivationMethod) {
			// Try to get a position 
			case ActivationMethods.Radius:
				// Try to get a random point within the activation radius
				float radius = panel.ActivationRadius;

				GetRandomPointInRadius:

				// Pick a random angle and distance
				float angle = rng.RandfRange(0f, (float) Math.PI * 2f);
				float distance = rng.RandfRange(0f, radius);
				Vector2 offset = new(
					x: Mathf.Cos(angle) * distance,
					y: Mathf.Sin(angle) * distance
				);
				Vector2 targetPos = panelPos + offset;

				// Check if the point is navigable
				bool canReach = unit.AIAgent.GoTo(targetPos);

				// Reset agent to previous target
				unit.AIAgent.GoTo(currentUnitTargetPos);

				if (canReach) return targetPos;

				attemptCountRemaining--;

				if (attemptCountRemaining > 0) goto GetRandomPointInRadius;
				else break;

			case ActivationMethods.Area:
				Area2D activationArea = panel.ActivationArea;
				CollisionShape2D shapeNode = activationArea.GetNode<CollisionShape2D>("CollisionShape2D");
				Shape2D shape = shapeNode.Shape;
				Vector2 shapePos = shapeNode.GlobalPosition; // Centered
				Vector2 topLeftPos = shapePos - shape.GetRect().Size / 2;
				Vector2 size = shape.GetRect().Size;

				GetRandomPointInArea:

				// Try to get a random point within the activation area
				float randomPosX = rng.RandfRange(topLeftPos.X, topLeftPos.X + size.X);
				float randomPosY = rng.RandfRange(topLeftPos.Y, topLeftPos.Y + size.Y);
				Vector2 randomPointInArea = new(
					x: randomPosX,
					y: randomPosY
				);

				// Check if the point is navigable
				bool canReachAreaPoint = unit.AIAgent.GoTo(randomPointInArea);

				// Reset agent to previous target
				unit.AIAgent.GoTo(currentUnitTargetPos);

				if (canReachAreaPoint) return randomPointInArea;

				attemptCountRemaining--;

				if (attemptCountRemaining > 0) goto GetRandomPointInArea;
				else break;
		}

		Log.Me(() => $"Unable to find navigable target position for {unit.InstanceID} to reach panel {panel.InstanceID}.");
		return null;
    }
}
