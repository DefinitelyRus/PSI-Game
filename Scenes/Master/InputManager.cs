global using IM = CommonScripts.InputManager;
using System.Reflection.Metadata;
using Godot;
namespace CommonScripts;

public partial class InputManager : Node2D {

	#region Instance Members

	[Signal] public delegate void ActionCommandEventHandler(string actionName, Variant args);

	#region Debugging

	[ExportGroup("Debugging")]
	[Export] protected bool LogReady = true;
	[Export] protected bool LogProcess = false;
	[Export] protected bool LogPhysics = false;
	[Export] protected bool LogInput = false;

	#endregion

	#region Godot Callbacks

	public override void _EnterTree() {
		if (Instance != null) {
			Log.Err("Multiple instances of InputManager detected. There should only be one InputManager in the scene.");
			QueueFree();
			return;
		}

		Instance = this;
	}

	public override void _Process(double delta) {
		if (Mode == InputModes.RTS) {
			CallDeferred(nameof(ReceiveRTSInputs));
		}
	}

	public override void _Input(InputEvent @event) {
		if (Mode == InputModes.RTS) {
			ReceiveCameraInputs(@event);
		}
	}

	#endregion

	#endregion

	#region Static Members

	public static InputManager Instance { get; private set; } = null!;

	#region Input Handling

	public static InputModes Mode { get; set; } = InputModes.RTS;

	public enum InputModes {
		TopDown,
		RTS
	}

	public const string LeftClick = "mouse_action_1";
	public const string RightClick = "mouse_action_2";
	public const string StopAction = "stop_action";


	private void ReceiveCameraInputs(InputEvent input) {
        if (input is not InputEventMouseMotion) return;
		if (Input.IsActionPressed("select_3")) CameraMan.Drag(input);
    }


	private void ReceiveCameraInputs() {
		if (Input.IsActionPressed("move_up")) CameraMan.Move(Vector2.Up);
		if (Input.IsActionPressed("move_down")) CameraMan.Move(Vector2.Down);
		if (Input.IsActionPressed("move_left")) CameraMan.Move(Vector2.Left);
		if (Input.IsActionPressed("move_right")) CameraMan.Move(Vector2.Right);
		if (Input.IsActionJustReleased("select_3")) CameraMan.StopDragging();
	}
	

	private void ReceiveRTSInputs() {
		Vector2 mousePos = GetGlobalMousePosition();
		var action = SignalName.ActionCommand;

		// Camera controls
		ReceiveCameraInputs();

		// Unit interactions
		if (Input.IsActionJustPressed("select_1")) EmitSignal(action, LeftClick, mousePos);     // Select, Move + Attack / Interact (in range)
		if (Input.IsActionJustPressed("select_2")) EmitSignal(action, RightClick, mousePos);    // Deselect, Move, Move + Attack / Interact (targeted)
		if (Input.IsActionJustPressed("stop_action")) EmitSignal(action, StopAction, new());    // Stop
		if (Input.IsActionJustPressed("select_unit_1")) Commander.SetFocusedUnit(0);
		if (Input.IsActionJustPressed("select_unit_2")) Commander.SetFocusedUnit(1);
		if (Input.IsActionJustPressed("select_all_units")) Commander.SelectAllUnits();
		if (Input.IsActionJustPressed("deselect_all_units")) Commander.DeselectAllUnits();

		// Toggle item use with 1-5
		if (Input.IsActionJustPressed("item_1")) Commander.SelectItem(0);
		if (Input.IsActionJustPressed("item_2")) Commander.SelectItem(1);
		if (Input.IsActionJustPressed("item_3")) Commander.SelectItem(2);
		if (Input.IsActionJustPressed("item_4")) Commander.SelectItem(3);
		if (Input.IsActionJustPressed("item_5")) Commander.SelectItem(4);

		// Prime to drop item with CTRL
		Commander.PrimeDrop = Input.IsActionPressed("prime_drop_item");
	}
	
	#endregion

	#endregion

}
