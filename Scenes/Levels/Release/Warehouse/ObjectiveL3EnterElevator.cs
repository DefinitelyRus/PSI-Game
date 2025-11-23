using CommonScripts;
using Godot;
namespace Game;

public partial class ObjectiveL3EnterElevator : StandardPanel {
    [Export] public uint RequiredPowerBoxes = 3;
    public override void Interact(StandardCharacter character) {

        for (uint i = 0; i < RequiredPowerBoxes; i++) {
            Variant? powerBoxData = GameManager.GetGameData($"L3_PowerRestored{i}", null);

            if (powerBoxData == null) {
                Log.Me(() => $"{character.CharacterName} tried to access the elevator but the power is not fully restored.");

                UIManager.SetBottomOverlayText("The power needs to be fully restored first...");
                return;
            }
        }

        // Check if all alive units are at the elevator
        foreach (StandardCharacter unit in Commander.GetAllUnits()) {
            if (!unit.IsAlive) continue;

            bool isAtLocation = ScanForUnit(unit);
            if (!isAtLocation) return;
        }
        
        GameManager.SetGameData("L3_EnteredElevator", null, true);
        Log.Me(() => $"All units have entered the elevator!");

        // Disable panel after interaction
        IsEnabled = false;
        Activated = true;

        SceneLoader.NextLevel();
    }

	public override void _Process(double delta) {
        ScanForPlayer();
        HighlightPanel(delta);
    }

}
