using CommonScripts;
using Godot;
namespace Game;

public partial class ObjectiveL4EnterElevator : StandardPanel {
    [Export] public float PromptDelay = 10f;
    [Export] public ObjectivePlantCharge? ChargeObjective = null;
    private float _promptTimer = 0f;

    public override async void Interact(StandardCharacter character) {
        // Check if the charge has been planted
        Variant? chargePlantedData = GameManager.GetGameData($"L4_ChargePlanted", null);
        bool isChargePlanted = chargePlantedData != null && chargePlantedData!.Value.AsBool();
        if (!isChargePlanted && _promptTimer <= 0f) {
            Log.Me(() => $"{character.CharacterName} tried to access the elevator but the charge has not been planted.");
            UIManager.SetBottomOverlayText("The charge needs to be planted first...", 5f);
            _promptTimer = PromptDelay;
            return;
        }

        // Check if the power virus has been installed
        Variant? powerVirusData = GameManager.GetGameData($"L4_PowerVirusInstalled", null);
        bool isPowerVirusInstalled = powerVirusData != null && powerVirusData!.Value.AsBool();
        if (!isPowerVirusInstalled && _promptTimer <= 0f) {
            Log.Me(() => $"{character.CharacterName} tried to access the elevator but the power virus has not been installed.");
            UIManager.SetBottomOverlayText("We need to install the power virus.", 5f);
            _promptTimer = PromptDelay;
            return;
        }

        // Check if the heat management system has been sabotaged
        Variant? heatSabotagedData = GameManager.GetGameData($"L4_HeatManagementSabotaged", null);
        bool isHeatSabotaged = heatSabotagedData != null && heatSabotagedData!.Value.AsBool();
        if (!isHeatSabotaged && _promptTimer <= 0f) {
            Log.Me(() => $"{character.CharacterName} tried to access the elevator but the heat management system has not been sabotaged.");
            UIManager.SetBottomOverlayText("The heat control is still online!", 5f);
            _promptTimer = PromptDelay;
            return;
        }

        // Check if the time has more than 25 seconds left.
        if (GameManager.TimeRemaining > 25.0) {
            if (_promptTimer <= 0f) {
                UIManager.SetBottomOverlayText("The elevator's coming down...", 2.2f);
                Log.Me(() => $"{character.CharacterName} tried to access the elevator but there is still time left on the charge.");
                _promptTimer = PromptDelay;
            }
            return;
        }

        // Check if all alive units are at the elevator
        foreach (StandardCharacter unit in Commander.GetAllUnits()) {
            if (!unit.IsAlive) continue;

            bool isAtLocation = ScanForUnit(unit);
            if (!isAtLocation) return;
        }
        
        await CompleteAndExitAsync(character);
    }

	public override void _Process(double delta) {
        if (Master.IsPaused) return;

        ScanForPlayer();
        HighlightPanel(delta);

        if (_promptTimer > 0f) {
            _promptTimer -= (float) delta;
        }
    }

    public async void TryAutoCompleteAtFinalCountdown() {
        // Only auto-complete if we are in the last 25 seconds
        if (GameManager.TimeRemaining > 25.0) return;

        // Ensure all alive units are at the elevator
        foreach (StandardCharacter unit in Commander.GetAllUnits()) {
            if (!unit.IsAlive) continue;
            bool isAtLocation = ScanForUnit(unit);
            if (!isAtLocation) return;
        }

        // Use commander focused unit or first alive for logging purposes
        StandardCharacter? any = null;
        foreach (StandardCharacter unit in Commander.GetAllUnits()) { if (unit.IsAlive) { any = unit; break; } }
        if (any == null) return;
        await CompleteAndExitAsync(any);
    }

    private async System.Threading.Tasks.Task CompleteAndExitAsync(StandardCharacter character) {
        GameManager.SetGameData("L4_EnteredElevator", null, true);
        Godot.Collections.Dictionary extra = new() {
            {"objectiveKey", "L4_EnteredElevator"},
            {"objectiveValue", true}
        };
        DataManager.RecordPanelInteraction(this, character, extra);
        Log.Me(() => $"All units have entered the elevator!");

        // Count this final extraction as a required objective completion for pacing.
        AIDirector.RegisterRequiredObjectiveCompletion();

        // Disable panel after interaction
        IsEnabled = false;
        Activated = true;

        AudioManager.StopMusic("AmbientAudio");
        UIManager.StartTransition("Mission Complete");
        UIManager.SetHUDVisible(false);
        if (SceneLoader.Instance.LoadedScene is Level lvl) DataManager.RecordLevelCompletion(lvl);
        await ToSignal(GetTree().CreateTimer(5.0f), "timeout");

        // Go to main menu
        Commander.Initialize();
        SceneLoader.LoadLevel(0);
    }

}
