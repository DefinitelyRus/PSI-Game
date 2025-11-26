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
    public double TimeLeft { get; set; } = 0;

    public override async void Interact(StandardCharacter character) {
        Variant? alreadyActivated = GameManager.GetGameData("L4_ChargePlanted", null);
        if (alreadyActivated != null) return;

        CameraNodes[^1] = character;

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

        if (MusicOnPlant != null) AudioManager.StreamAudio(MusicOnPlant);
        GameManager.SetGameData("L4_ChargePlanted", null, true);
        InputManager.AllowOverride = true;
        AIDirector.AllowSpawning = true;
        if (Nav != null) Nav.Enabled = true;
        
        GameManager.TimeRemaining = 221.5;
        GameManager.ManualTimerCheck = true;
        TimeLeft = 221.5;
        await ToSignal(GetTree().CreateTimer(221.5 - 60f), "timeout");

        UIManager.SetBottomOverlayText($"60 seconds until detonation!", 5f);
        await ToSignal(GetTree().CreateTimer(40f), "timeout");

        UIManager.SetBottomOverlayText($"The elevator's open!", 5f);
        ElevatorObjective?.GetChild<AnimationPlayer>(3).Play("Door Open");

        await ToSignal(GetTree().CreateTimer(20f), "timeout");
        Variant? missionSuccess = GameManager.GetGameData("L4_EnteredElevator", null);
        if (missionSuccess == null) {
            Log.Me(() => "Mission failed: Charge detonated before escape.");
            CameraMan.Shake(ExplosionShakeIntensity);

            foreach (StandardCharacter unit in Commander.GetAllUnits()) {
                if (unit.IsAlive) {
                    unit.Kill();
                }
            }
        }
    }

    public override void _Process(double delta) {
        ScanForPlayer();
        HighlightPanel(delta);

        TimeLeft -= delta;
        if (TimeLeft < 0) TimeLeft = 0;
    }

	public override void _Ready() {
        AIDirector.AllowSpawning = false;

        // Disable nav region until charge is planted
        if (Nav != null) Nav.Enabled = false;

		base._Ready();
	}

}