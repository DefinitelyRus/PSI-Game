using Godot;
using CommonScripts;
using System.Threading.Tasks;
namespace Game;

public partial class ObjectivePlantCharge : StandardPanel {

    [Export] public AudioStream? MusicOnPlant = null;
    [Export] public Node2D[] CameraNodes = [];
    [Export] public NavigationRegion2D? Nav = null;
    [Export] public ObjectiveL4EnterElevator? ElevatorObjective = null;
    [Export] public float ExplosionShakeIntensity = 10f;
    // Duration of the charge timer (seconds). Adjust as needed for this level.
    [Export] public double ChargeDuration = 221.5;

    public override async void Interact(StandardCharacter character) {
        Variant? alreadyActivated = GameManager.GetGameData("L4_ChargePlanted", null);
        if (alreadyActivated != null) return;

        // Append the interacting character as the final camera target if a path is provided
        if (CameraNodes.Length > 0) CameraNodes[^1] = character;

        if (CameraNodes.Length > 0) CameraMan.SetTarget(CameraNodes[^1]);

        // Disable panel after interaction
        IsEnabled = false;
        Activated = true;
        InputManager.AllowOverride = false;
        UIManager.SetHUDVisible(false);

        await ToSignal(GetTree().CreateTimer(1.5f), "timeout");

        if (ActivationSound != null) AudioManager.StreamAudio(ActivationSound);

        if (CameraNodes.Length > 0) CameraMan.SetCameraPath(CameraNodes);

        UIManager.SetBottomOverlayText($"Install the power virus...", 2f);

        await ToSignal(GetTree().CreateTimer(2.7f), "timeout");
        UIManager.SetBottomOverlayText($"Disable the heat control...", 2f);

        await ToSignal(GetTree().CreateTimer(3f), "timeout");
        UIManager.SetBottomOverlayText($"And escape right before it detonates...", 3f);

        await ToSignal(GetTree().CreateTimer(4.1f), "timeout");

        UIManager.SetBottomOverlayText($"Good luck.", 3f);
        Log.Me(() => $"{character.CharacterName} has planted the charge at the datacenter!");

        if (MusicOnPlant != null) AudioManager.StreamAudio(MusicOnPlant, "L4_Music", AudioManager.AudioChannels.Music, 1.2f);
        GameManager.SetGameData("L4_ChargePlanted", null, true);

        // Register this critical step as a required objective completion for pacing.
        AIDirector.RegisterRequiredObjectiveCompletion();

        // Wait until the cinematic camera path has fully completed before starting the timer
        while (CameraMan.IsPathActive) {
            await ToSignal(GetTree(), "process_frame");
        }

        // Start the actual level countdown now (show and tick the HUD timer)
        GameManager.TimeRemaining = ChargeDuration;
        GameManager.ManualTimerCheck = false;

        InputManager.AllowOverride = true;
        AIDirector.AllowSpawning = true;
        if (Nav != null) Nav.Enabled = true;

        // Schedule timed events relative to countdown start
        double t = ChargeDuration;
        // 60s remaining notification
        if (t > 60.0) {
            await ToSignal(GetTree().CreateTimer((float)(t - 60.0)), "timeout");
            UIManager.SetBottomOverlayText($"60 seconds until detonation!", 5f);
        }

        // Open the elevator at 25s remaining
        if (t > 25.0) {
            if (t > 60.0) {
                await ToSignal(GetTree().CreateTimer(35f), "timeout"); // from 60 -> 25
            } else {
                await ToSignal(GetTree().CreateTimer((float)(t - 25.0)), "timeout");
            }
            UIManager.SetBottomOverlayText($"The elevator's open!", 5f);
            ElevatorObjective?.GetChild<AnimationPlayer>(3).Play("Door Open");
        }

        // At 10s remaining try to auto-complete if units are in position
        if (t > 10.0) {
            if (t > 25.0) await ToSignal(GetTree().CreateTimer(15f), "timeout");
            else await ToSignal(GetTree().CreateTimer((float)(t - 10.0)), "timeout");
            ElevatorObjective?.TryAutoCompleteAtFinalCountdown();
        }
    }

    public override void _Process(double delta) {
        ScanForPlayer();
        HighlightPanel(delta);
    }

	public override void _Ready() {
        AIDirector.AllowSpawning = false;

        // Disable nav region until charge is planted
        if (Nav != null) Nav.Enabled = false;

        // For this level, do not show or tick the HUD timer until the charge is planted
        GameManager.ManualTimerCheck = true;
        GameManager.TimeRemaining = double.MaxValue;

		base._Ready();
	}

}