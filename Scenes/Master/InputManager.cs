global using IM = CommonScripts.InputManager;
using Godot;
namespace CommonScripts;

public partial class InputManager : Node2D {

	#region Debugging

	[ExportGroup("Debugging")]
	[Export] protected bool LogReady = true;
	[Export] protected bool LogProcess = false;
	[Export] protected bool LogPhysics = false;
	[Export] protected bool LogInput = false;

	#endregion

	#region Enums

	public enum InputModes {
		TopDown,
		RTS
	}

	public InputModes Mode { get; set; } = InputModes.RTS;

	#endregion

	#region Constants

	public const string CameraUp = "move_camera_up";
	public const string CameraDown = "move_camera_down";
	public const string CameraLeft = "move_camera_left";
	public const string CameraRight = "move_camera_right";

	public const string LeftClick = "mouse_action_1";
	public const string RightClick = "mouse_action_2";
	public const string MiddleClick = "mouse_action_3";

	public const string StopAction = "stop_action";

	public const string SelectItem = "select_item";
	public const string DropItem = "queue_drop_item";

	#endregion

	#region Signals

	[Signal] public delegate void ActionCommandEventHandler(string actionName, Variant args);

	#endregion

	#region Input Listeners

	private void ReceiveRTSInputs() {
		Vector2 mousePos = GetGlobalMousePosition();
		var action = SignalName.ActionCommand;

		// Move camera with WASD
		if (Input.IsActionPressed("move_up")) ; //Camera.Move(Vector2.Up);
		if (Input.IsActionPressed("move_down")) ; //Camera.Move(Vector2.Down);
		if (Input.IsActionPressed("move_left")) ; //Camera.Move(Vector2.Left);
		if (Input.IsActionPressed("move_right")) ; // Camera.Move(Vector2.Right);
		if (Input.IsActionPressed("select_3")) ; //Camera.Pan(mousePos); // Pan camera to mouse position

		// Unit interactions
		if (Input.IsActionJustPressed("select_1")) EmitSignal(action, LeftClick, mousePos);		// Select + Move & Attack
		if (Input.IsActionJustPressed("select_2")) EmitSignal(action, RightClick, mousePos);	// Move
		if (Input.IsActionJustPressed("stop_action")) EmitSignal(action, StopAction, new());    // Stop
		if (Input.IsActionJustPressed("select_unit_1")) Commander.SelectUnit(0);
		if (Input.IsActionJustPressed("select_unit_2")) Commander.SelectUnit(1);
		if (Input.IsActionJustPressed("select_all_units")) Commander.SelectAllUnits();
		if (Input.IsActionJustPressed("deselect_all_units")) Commander.DeselectAllUnits();

		// Toggle item use with 1-5
		//if (Input.IsActionJustPressed("item_1")) Commander.FocusedUnit.UseItem(0);
		//if (Input.IsActionJustPressed("item_2")) Commander.FocusedUnit.UseItem(1);
		//if (Input.IsActionJustPressed("item_3")) Commander.FocusedUnit.UseItem(2);
		//if (Input.IsActionJustPressed("item_4")) Commander.FocusedUnit.UseItem(3);
		//if (Input.IsActionJustPressed("item_5")) Commander.FocusedUnit.UseItem(4);

		// Drop item with Q
		if (Input.IsActionPressed("queue_drop_item")) EmitSignal(action, DropItem); // Drops the currently selected item.
	}

	#endregion

	#region Godot Callbacks

	public override void _Process(double delta) {
		if (Mode == InputModes.RTS) {
			CallDeferred(nameof(ReceiveRTSInputs));
		}
	}

	#endregion

}
