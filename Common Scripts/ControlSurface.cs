using Godot;
namespace CommonScripts;

/// <summary>
/// An intermediary layer that allows control of a <see cref="StandardCharacter"/> without specifying the exact input method.
/// Since it only provides values to modify, both manual (hardware) and automatic (AI) input methods can use it.<br/><br/>
/// This class assumes the existence of a <see cref="StandardCharacter"/> as its parent node and assign it as <see cref="Character"/> within <see cref="_Ready"/>.
/// </summary>
public partial class ControlSurface : Node2D
{
	#region Movement Controls

	/// <summary>
	/// Whether the control surface is enabled for movement.
	/// </summary>
	[ExportGroup("Movement Controls")]
    [Export] public bool EnableMovement = true;

	/// <summary>
	/// The direction of movement for the character. <br/><br/>
	/// Do not use this value directly except for getters and setters, as it is not clamped.<br/>
	/// </summary>
	private Vector2 _movementDirection = Vector2.Zero;

	/// <summary>
	/// The direction of movement for the character. <br/><br/>
	/// This is a setter/getter for <see cref="_movementDirection"/>.
	/// Setting this value will automatically normalize it to ensure consistent movement direction.
	/// </summary>
	[Export] public Vector2 MovementDirection {
        get => _movementDirection;
        set => _movementDirection = value.Normalized();
    }

	/// <summary>
	/// The magnitude of the movement direction, which can be used to control speed on analog inputs. <br/><br/>
	/// Do not use this value directly except for getters and setters, as it is not clamped.<br/>
	/// </summary>
	private float _magnitude = 0f;

	/// <summary>
	/// The magnitude of the movement direction, which can be used to control speed on analog inputs. <br/><br/>
	/// This is a setter/getter for <see cref="_magnitude"/>.
	/// Setting this value will clamp it to the range of <c>0</c> to <c>1</c>.
	/// </summary>
	[Export] public float MovementMultiplier {
        get => _magnitude;
        set { _magnitude = Mathf.Clamp(value, 0f, 1f); }
    }

	#endregion

	#region Heading Controls

	/// <summary>
	/// Whether the control surface is enabled for heading controls.
	/// </summary>
	[ExportGroup("Heading Controls")]
    [Export] public bool EnableFaceDirection = true;

	/// <summary>
	/// The direction where the character is facing, used for aiming and visual representation. <br/><br/>
	/// Do not use this value directly except for getters and setters, as it is not clamped.<br/>
	/// </summary>
	private Vector2 _facingDirection = Vector2.Down;

	/// <summary>
	/// The direction where the characer is facing, used for aiming and visual representation.<br/><br/>
	/// This is a setter/getter for <see cref="_facingDirection"/>.
	/// Setting this value will normalize it and ignore the assignment if the value is a zero vector.<br/>
	/// </summary>
	[Export] public Vector2 FacingDirection {
        get => _facingDirection;
        set {
			if (value == Vector2.Zero) return; // Ignore zero vectors.
			_facingDirection = value.Normalized();
		}
	}

	#endregion

	#region Nodes & Components

	[ExportGroup("Nodes & Components")]
	[Export] private StandardCharacter Character = null!;

	#endregion

	#region Debugging

	[ExportGroup("Debugging")]
    [Export] protected bool LogReady = true;
    [Export] protected bool LogProcess = false;
    [Export] protected bool LogPhysics = false;

	#endregion

	#region Godot Callbacks

	public override void _EnterTree() {
		Log.Me(() => $"A ControlSurface has entered the tree. Checking properties...", LogReady);

		Character = GetParentOrNull<StandardCharacter>();
		if (Character == null) Log.Err("ControlSurface must be a child of a StandardCharacter. Inputs will not reach its intended target.");

		Log.Me(() => "Done checking properties.", LogReady);
	}

    #endregion
}
