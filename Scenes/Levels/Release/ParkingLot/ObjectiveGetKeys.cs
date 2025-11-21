using CommonScripts;
namespace Game;

public partial class ObjectiveGetKeys : StandardPanel {
    public override void Interact(StandardCharacter character) {
        GameManager.SetGameData("L1_GotKeys", null, true);

        Log.Me(() => $"{character.CharacterName} has obtained the parking lot keys!");

        UIManager.SetBottomOverlayText("Got the keys! Let's head to the building.");

        // Disable panel after interaction
        IsEnabled = false;
        Activated = true;
    }
}
