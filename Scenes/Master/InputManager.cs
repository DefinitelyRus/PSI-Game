global using IM = CommonScripts.InputManager;
using System.Reflection.Metadata;
using Godot;
namespace CommonScripts;

public partial class InputManager : Node2D {

	#region Instance Members

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

		if (Input.IsActionJustReleased(ExitGame)) _exitHoldTimer = 0f;
		if (Input.IsActionPressed(ExitGame)) {
			_exitHoldTimer += (float)delta;
			if (_exitHoldTimer >= 2f) {
				GetTree().Quit();
			}
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
	public const string DebugNextLevel = "debug_next_level";
	public const string DebugPrevLevel = "debug_prev_level";
	public const string DebugEndGame = "debug_end_game";
	public const string ExitGame = "exit_game";

	public static bool AllowOverride { get; set; } = true;


	private void ReceiveCameraInputs(InputEvent input) {
        if (input is not InputEventMouseMotion) return;
		if (CameraMan.IsControlsLocked) return;
		if (Input.IsActionPressed("select_3")) CameraMan.Drag(input);
    }


	private void ReceiveCameraInputs() {
		if (CameraMan.IsControlsLocked) return;
		if (Input.IsActionPressed("move_up")) CameraMan.Move(Vector2.Up);
		if (Input.IsActionPressed("move_down")) CameraMan.Move(Vector2.Down);
		if (Input.IsActionPressed("move_left")) CameraMan.Move(Vector2.Left);
		if (Input.IsActionPressed("move_right")) CameraMan.Move(Vector2.Right);
		if (Input.IsActionJustReleased("select_3")) CameraMan.StopDragging();
	}
	

	private void ReceiveRTSInputs() {
		Vector2 mousePos = CameraMan.GetCleanMousePosition();
		bool ctrlPressed = Input.IsActionPressed("ctrl_modifier");

		ReceiveCameraInputs();

		// Only allow focusing on units while controls are locked
		if (CameraMan.IsControlsLocked) {
			if (!AllowOverride) return;

			if (ctrlPressed && Input.IsActionJustPressed("select_unit_1")) Commander.SetFocusedUnit(0, true);
			if (ctrlPressed && Input.IsActionJustPressed("select_unit_2")) Commander.SetFocusedUnit(1, true);
			Commander.PrimeDrop = false;

			return;
		}

		// RTS Unit Selection Inputs
		if (Input.IsActionJustPressed("select_unit_1")) {
			if (ctrlPressed) Commander.SetFocusedUnit(0, true);
			else Commander.SelectUnit(0);
		}

		if (Input.IsActionJustPressed("select_unit_2")) {
			if (ctrlPressed) Commander.SetFocusedUnit(1, true);
			else Commander.SelectUnit(1);
		}

		// RTS Command Inputs
		if (Input.IsActionJustPressed("select_1")) Commander.MoveAndSearch(mousePos);
		if (Input.IsActionJustPressed("select_2")) Commander.MoveAndTarget(mousePos);
		if (Input.IsActionJustPressed("stop_action")) Commander.StopSelectedUnits();
		
		if (Input.IsActionJustPressed("select_all_units")) Commander.SelectAllUnits();
		if (Input.IsActionJustPressed("deselect_all_units")) Commander.DeselectAllUnits();

		// Item Management Inputs
		Commander.PrimeDrop = ctrlPressed;
		if (Input.IsActionJustPressed("item_1")) Commander.SelectItem(0);
		if (Input.IsActionJustPressed("item_2")) Commander.SelectItem(1);
		if (Input.IsActionJustPressed("item_3")) Commander.SelectItem(2);
		if (Input.IsActionJustPressed("item_4")) Commander.SelectItem(3);
		if (Input.IsActionJustPressed("item_5")) Commander.SelectItem(4);

		if (Input.IsActionJustPressed(DebugNextLevel)) SceneLoader.NextLevel();
		if (Input.IsActionJustPressed(DebugPrevLevel)) SceneLoader.PreviousLevel();
		if (Input.IsActionJustPressed(DebugEndGame)) TriggerDebugEndGame();
	}

	private async void TriggerDebugEndGame() {
		AudioManager.StopMusic("AmbientAudio");
		UIManager.StartTransition("Mission Complete");
		UIManager.SetHUDVisible(false);
		if (SceneLoader.Instance.LoadedScene is Level lvl) DataManager.RecordLevelCompletion(lvl);
		await ToSignal(GetTree().CreateTimer(5.0f), "timeout");
		Commander.Initialize();
		SceneLoader.LoadLevel(0);
	}

	private float _exitHoldTimer = 0f;
	
	#endregion

	#endregion

}
