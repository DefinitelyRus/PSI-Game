using CommonScripts;
using Godot;
namespace Game;

public partial class ObjectiveL2EnterElevator : StandardPanel {
    public override void Interact(StandardCharacter character) {
        Variant? gotKeycard1Data = GameManager.GetGameData("L2_GotKeycard1", null);

        if (gotKeycard1Data == null) {
            Log.Me(() => $"{character.CharacterName} tried to enter the building but doesn't have the keys!");
            Node currentLevel = SceneLoader.Instance.LoadedScene;

            if (currentLevel is Level level) {
                Node2D objective = level.CameraNodePaths[0];
                CameraMan.SetTarget(objective);
            }

            UIManager.SetBottomOverlayText("We need the keys from the car first.");
            return;
        }

        Variant? gotKeycard2Data = GameManager.GetGameData("L2_GotKeycard2", null);

        if (gotKeycard2Data == null) {
            Log.Me(() => $"{character.CharacterName} tried to enter the building but doesn't have the keys!");
            Node currentLevel = SceneLoader.Instance.LoadedScene;

            if (currentLevel is Level level) {
                Node2D objective = level.CameraNodePaths[0];
                CameraMan.SetTarget(objective);
            }

            UIManager.SetBottomOverlayText("We need the keys from the car first.");
            return;
        }

        GameManager.SetGameData("L2_EnteredElevator", null, true);

        Log.Me(() => $"{character.CharacterName} has entered the building!");

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
