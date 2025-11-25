using CommonScripts;
using Godot;
namespace Game;

public partial class ObjectiveL4EnterElevator : StandardPanel {
    [Export] public float PromptDelay = 10f;
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

        // Check if all alive units are at the elevator
        foreach (StandardCharacter unit in Commander.GetAllUnits()) {
            if (!unit.IsAlive) continue;

            bool isAtLocation = ScanForUnit(unit);
            if (!isAtLocation) return;
        }
        
        GameManager.SetGameData("L4_EnteredElevator", null, true);
        Log.Me(() => $"All units have entered the elevator!");

        // Disable panel after interaction
        IsEnabled = false;
        Activated = true;

        UIManager.StartTransition("Mission Complete");
        await ToSignal(GetTree().CreateTimer(5.0f), "timeout");
        SceneLoader.UnloadLevel(true);
        UIManager.EndTransition();
    }

	public override void _Process(double delta) {
        ScanForPlayer();
        HighlightPanel(delta);

        if (_promptTimer > 0f) {
            _promptTimer -= (float) delta;
        }
    }

}
