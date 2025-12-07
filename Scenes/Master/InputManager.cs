global using IM = CommonScripts.InputManager;
using System.Linq;
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

		if (Input.IsActionJustReleased(Cancel)) _exitHoldTimer = 0f;
		if (Input.IsActionPressed(Cancel)) {
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

	public static IM Instance { get; private set; } = null!;

	#region Input Handling

	public static InputModes Mode { get; set; } = InputModes.RTS;

	public enum InputModes {
		TopDown,
		RTS
	}

	public const string LeftClick = "mouse_action_1";
	public const string RightClick = "mouse_action_2";
	public const string Halt = "halt";
	public const string DebugNextLevel = "debug_next_level";
	public const string DebugPrevLevel = "debug_prev_level";
	public const string DebugEndGame = "debug_end_game";
	public const string Cancel = "cancel";
	public const string Help = "help";

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

		if (Input.IsActionJustPressed(Cancel) && CameraMan.IsPathActive) {
			CameraMan.FinishPathInstantly();
			return;
		}

		if (Input.IsActionJustPressed(Help)) {
			UIManager.ToggleHelp();
			return;
		}

		ReceiveCameraInputs();

		// Only allow selection while controls are locked
		if (CameraMan.IsControlsLocked) {
			if (!AllowOverride) return;
			Commander.PrimeDrop = false;
			return;
		}

		// RTS Unit Selection Inputs
		bool select1 = Input.IsActionJustPressed("select_unit_1");
		bool select2 = Input.IsActionJustPressed("select_unit_2");
		if (select1 || select2) {
			int idx = select1 ? 0 : 1;
			Commander.SelectUnit(idx);
			if (ctrlPressed) {
				var unit = Commander.GetSelectedUnits().First();
				if (CameraMan.IsPathActive) CameraMan.FinishPathInstantly(skipFocus: true);
				CameraMan.SetTarget(unit);
				_ctrlFocusActive = true;
				_ctrlFocusLatched = true;
				_ctrlFocusedUnit = unit;
			}
		}

		// RTS Command Inputs
		if (Input.IsActionJustPressed("select_1")) Commander.MoveAndSearch(mousePos);
		if (Input.IsActionJustPressed("select_2")) Commander.MoveAndTarget(mousePos);
		if (Input.IsActionJustPressed("halt")) Commander.StopSelectedUnits();
		
		if (Input.IsActionJustPressed("select_all_units")) Commander.SelectAllUnits();
		if (Input.IsActionJustPressed("deselect_all_units")) Commander.DeselectAllUnits();

		// Item Management Inputs
		Commander.PrimeDrop = ctrlPressed;
		if (Input.IsActionJustPressed("item_1")) Commander.SelectItem(0);
		if (Input.IsActionJustPressed("item_2")) Commander.SelectItem(1);
		if (Input.IsActionJustPressed("item_3")) Commander.SelectItem(2);
		if (Input.IsActionJustPressed("item_4")) Commander.SelectItem(3);
		if (Input.IsActionJustPressed("item_5")) Commander.SelectItem(4);

		int selectedCount = Commander.GetSelectedUnitCount();
		if (ctrlPressed && selectedCount >= 1) {
			var unit = Commander.GetSelectedUnits().First();
			bool needRetarget = !_ctrlFocusActive || !ReferenceEquals(unit, _ctrlFocusedUnit);
			if (needRetarget) {
				_ctrlFocusActive = true;
				_ctrlFocusedUnit = unit;
				if (CameraMan.IsPathActive) CameraMan.FinishPathInstantly(skipFocus: true);
				CameraMan.SetTarget(unit);
			}
			if (CameraMan.HasArrivedAtTarget()) _ctrlFocusLatched = true;
		}
		else {
			if (_ctrlFocusActive) {
				_ctrlFocusActive = false;
				_ctrlFocusedUnit = null;
				if (!_ctrlFocusLatched && !CameraMan.IsPathActive) CameraMan.ClearTarget();
			}
		}

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
	private bool _ctrlFocusActive = false;
	private bool _ctrlFocusLatched = false;
	private StandardCharacter? _ctrlFocusedUnit = null;
	
	#endregion

	#endregion

}
