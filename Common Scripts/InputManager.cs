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

	#region Signals

	[Signal] public delegate void ActionCommandEventHandler(string actionName, Variant args);

	#endregion

	#region Input Listeners

	private void ReceiveRTSInputs() {
		Vector2 mousePos = GetGlobalMousePosition();
		var action = SignalName.ActionCommand;

		// Move camera with WASD
		if (Input.IsActionPressed("move_up")) EmitSignal(action, "move_camera_up", new());
		if (Input.IsActionPressed("move_down")) EmitSignal(action, "move_camera_down", new());
		if (Input.IsActionPressed("move_left")) EmitSignal(action, "move_camera_left", new());
		if (Input.IsActionPressed("move_right")) EmitSignal(action, "move_camera_right", new());

		// Select/interact with left click
		if (Input.IsActionJustPressed("select_1")) EmitSignal(action, "select", mousePos);
		if (Input.IsActionJustPressed("select_2")) EmitSignal(action, "cancel", mousePos);

		// Toggle item use with 1-5
		if (Input.IsActionJustPressed("item_1")) EmitSignal(action, "select_item", 1);
		if (Input.IsActionJustPressed("item_2")) EmitSignal(action, "select_item", 2);
		if (Input.IsActionJustPressed("item_3")) EmitSignal(action, "select_item", 3);
		if (Input.IsActionJustPressed("item_4")) EmitSignal(action, "select_item", 4);
		if (Input.IsActionJustPressed("item_5")) EmitSignal(action, "select_item", 5);

		// Drop item with Q
		if (Input.IsActionJustPressed("drop_item")) EmitSignal(action, "queue_drop"); // Drops the currently selected item.
	}

	private void InputTester(string actionName, Variant args = new()) {
		if (!LogInput) return;

		string argsStr = string.Join(", ", args);
		Log.Me($"Action Command Received: {actionName} ({argsStr})");
	}

	#endregion

	#region Godot Callbacks

	public override void _Ready() {
		// Connect input tester for debugging.
		Log.Me(() => "Connecting ActionCommandEventHandler to InputTester for debugging...");

		Connect(nameof(ActionCommand), new Callable(this, nameof(InputTester)));
	}

	public override void _Process(double delta) {
		if (Mode == InputModes.RTS) {
			CallDeferred(nameof(ReceiveRTSInputs));
		}
	}

	#endregion

}
