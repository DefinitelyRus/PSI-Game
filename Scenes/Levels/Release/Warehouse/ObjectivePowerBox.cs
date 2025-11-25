using System.Linq;
using CommonScripts;
using Godot;
namespace Game;

public partial class ObjectivePowerBox : StandardPanel {
    [Export] public uint Id = 0;

    public override void Interact(StandardCharacter character) {
        Node loadedScene = SceneLoader.Instance.LoadedScene;
        ObjectivePowerBox[] powerBoxes = [.. loadedScene.GetChildren().OfType<ObjectivePowerBox>()];

        Variant?[] gameData = [
            GameManager.GetGameData("L3_PowerRestored0", null),
            GameManager.GetGameData("L3_PowerRestored1", null),
            GameManager.GetGameData("L3_PowerRestored2", null)
        ];

        // Return if this power box has already been restored
        bool hasGameData = gameData[Id] != null;
        if (hasGameData) {
            bool isRestored = gameData[Id]!.Value.AsBool();
            if (isRestored) return;
        }

        // Restore power for this box
        Log.Me(() => $"{character.CharacterName} has restored power station {Id}.");
        UIManager.SetBottomOverlayText($"Power station {Id + 1} restored.");
        GameManager.SetGameData($"L3_PowerRestored{Id}", null, true);

        // Disable all other boxes with the same ID
        foreach (ObjectivePowerBox box in powerBoxes) {
            if (box.Id != Id) continue;

            Log.Me(() => $"Disabling power box with ID {box.Id}");

            box.IsEnabled = false;
            box.Activated = true;
        }

        // Check if all power stations have been restored
        bool allRestored = true;

        // Refresh game data
        gameData = [
            GameManager.GetGameData("L3_PowerRestored0", null),
            GameManager.GetGameData("L3_PowerRestored1", null),
            GameManager.GetGameData("L3_PowerRestored2", null)
        ];

        foreach (Variant? data in gameData) {
            if (data == null || !data.Value.AsBool()) {
                allRestored = false;
                break;
            }
        }

        if (allRestored) {
            Log.Me(() => "All power boxes have been restored! The main power is back online.");
            UIManager.SetBottomOverlayText("To the elevator, now!");
            GameManager.SetGameData("L3_AllPowerRestored", null, true);
        }
    }

    public override void _Process(double delta) {
        ScanForPlayer();
        HighlightPanel(delta);
    }

}