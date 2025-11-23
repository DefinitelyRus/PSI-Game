using CommonScripts;
using Godot;
namespace Game;

public partial class ObjectiveGetKeycard : StandardPanel {
    
    [Export] public int KeycardNumber = 1;

    public override void Interact(StandardCharacter character) {
        GameManager.SetGameData($"L2_GotKeycard{KeycardNumber}", null, true);

        Log.Me(() => $"{character.CharacterName} has obtained the office keycard!");

        UIManager.SetBottomOverlayText("Got one elevator keycard!");

        // Disable panel after interaction
        IsEnabled = false;
        Activated = true;
    }
    
}