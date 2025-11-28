using CommonScripts;
using Godot;
namespace Game;

public partial class ObjectiveL3EnterElevator : StandardPanel {
    [Export] public uint RequiredPowerBoxes = 3;
    [Export] public float PromptDelay = 10f;
    private float _promptTimer = 0f;

    public override void Interact(StandardCharacter character) {
        // Check if the power has been fully restored
        Variant? powerBoxData = GameManager.GetGameData($"L3_AllPowerRestored", null);
        bool isPowerRestored = powerBoxData != null && powerBoxData!.Value.AsBool();
        if (!isPowerRestored && _promptTimer <= 0f) {
            Log.Me(() => $"{character.CharacterName} tried to access the elevator but the power is not fully restored.");

            UIManager.SetBottomOverlayText("The power needs to be fully restored first...", 5f);
            _promptTimer = PromptDelay;
            return;
        }

        // Check if all alive units are at the elevator
        foreach (StandardCharacter unit in Commander.GetAllUnits()) {
            if (!unit.IsAlive) continue;

            bool isAtLocation = ScanForUnit(unit);
            if (!isAtLocation) return;
        }
        
        GameManager.SetGameData("L3_EnteredElevator", null, true);
        Log.Me(() => $"All units have entered the elevator!");

    // Count this as a required objective completion for pacing.
    AIDirector.RegisterRequiredObjectiveCompletion();

        // Disable panel after interaction
        IsEnabled = false;
        Activated = true;

        SceneLoader.NextLevel();
    }

	public override void _Process(double delta) {
        ScanForPlayer();
        HighlightPanel(delta);

        if (_promptTimer > 0f) {
            _promptTimer -= (float) delta;
        }
    }

}
