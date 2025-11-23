using CommonScripts;
using Godot;
namespace Game;

public partial class ObjectivePowerBox : StandardPanel {
    [Export] public uint Id = 0;

    public override void Interact(StandardCharacter character) {
        Variant? alreadyActivated = GameManager.GetGameData($"L3_PowerRestored{Id}", null);
        if (alreadyActivated != null) return;

        Log.Me(() => $"{character.CharacterName} has restored power to the warehouse!");

        GameManager.SetGameData($"L3_PowerRestored{Id}", null, true);

        //Check if all 6 power boxes are activated
        for (uint i = 0; i <= 0; i++) {
            Variant? powerBoxData = GameManager.GetGameData($"L3_PowerRestored{i}", null);
            if (powerBoxData == null) return;
        }

        Log.Me(() => "All power boxes have been restored! The main power is back online.");
        GameManager.SetGameData("L3_AllPowerRestored", null, true);

        UIManager.SetBottomOverlayText("To the elevator, now!");

        // Disable panel after interaction
        IsEnabled = false;
        Activated = true;
    }

    public override void _Process(double delta) {
        ScanForPlayer();
        HighlightPanel(delta);
    }

}