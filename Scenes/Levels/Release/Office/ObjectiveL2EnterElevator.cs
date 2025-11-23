using CommonScripts;
using Godot;
namespace Game;

public partial class ObjectiveL2EnterElevator : StandardPanel {
    public override void Interact(StandardCharacter character) {
        Variant? gotKeycard1Data = GameManager.GetGameData("L2_GotKeycard1", null);
        Variant? gotKeycard2Data = GameManager.GetGameData("L2_GotKeycard2", null);

        if (gotKeycard1Data == null) {
            Log.Me(() => $"{character.CharacterName} tried to access the elevator but does not have keycard 1.");
            Node currentLevel = SceneLoader.Instance.LoadedScene;

            if (currentLevel is Level level) {
                Node2D objective = level.CameraNodePaths[0];
                CameraMan.SetTarget(objective);
            }

            UIManager.SetBottomOverlayText("We need at least 2 keycards...");
            return;
        }


        if (gotKeycard2Data == null) {
            Log.Me(() => $"{character.CharacterName} tried to access the elevator but does not have keycard 2.");
            Node currentLevel = SceneLoader.Instance.LoadedScene;

            if (currentLevel is Level level) {
                Node2D objective = level.CameraNodePaths[1];
                CameraMan.SetTarget(objective);
            }

            UIManager.SetBottomOverlayText("We need at least 2 keycards...");
            return;
        }

        // Check if all alive units are at the elevator
        foreach (StandardCharacter unit in Commander.GetAllUnits()) {
            if (!unit.IsAlive) continue;

            bool isAtLocation = ScanForUnit(unit);
            if (!isAtLocation) return;
        }
        
        GameManager.SetGameData("L2_EnteredElevator", null, true);
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
