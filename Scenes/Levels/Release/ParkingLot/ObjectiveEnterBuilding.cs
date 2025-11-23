using CommonScripts;
using Godot;
namespace Game;

public partial class ObjectiveEnterBuilding : StandardPanel {
    public override void Interact(StandardCharacter character) {
        Variant? gotKeysData = GameManager.GetGameData("L1_GotKeys", null);

        if (gotKeysData == null) {
            Log.Me(() => $"{character.CharacterName} tried to enter the building but doesn't have the keys!");
            Node currentLevel = SceneLoader.Instance.LoadedScene;

            if (currentLevel is Level level) {
                Node2D objective = level.CameraNodePaths[0];
                CameraMan.SetTarget(objective);
            }

            UIManager.SetBottomOverlayText("We need the keys from the car first.");
            return;
        }

        GameManager.SetGameData("L1_EnteredBuilding", null, true);

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
