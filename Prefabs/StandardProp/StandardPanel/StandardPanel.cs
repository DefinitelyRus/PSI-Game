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
		Optional
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
}
