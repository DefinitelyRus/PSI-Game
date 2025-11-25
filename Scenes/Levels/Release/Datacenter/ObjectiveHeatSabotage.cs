using Godot;
using CommonScripts;
namespace Game;

public partial class ObjectiveHeatSabotage : StandardPanel {

    public override async void Interact(StandardCharacter character) {
        // Check if the heat management system has already been sabotaged
        Variant? alreadyActivated = GameManager.GetGameData("L4_HeatManagementSabotaged", null);
        if (alreadyActivated != null) return;

        // Check if the power virus has been installed
        Variant? powerVirusData = GameManager.GetGameData("L4_PowerVirusInstalled", null);
        if (powerVirusData == null) return;

        Log.Me(() => $"{character.CharacterName} has disabled the heat management system!");
        GameManager.SetGameData("L4_HeatManagementSabotaged", null, true);

        // Disable panel after interaction
        IsEnabled = false;
        Activated = true;

        if (ActivationSound != null) AudioManager.StreamAudio(ActivationSound);

        UIManager.SetBottomOverlayText("Heat management system sabotaged!", 2f);
        await ToSignal(GetTree().CreateTimer(2f), "timeout");

        UIManager.SetBottomOverlayText("RUN TO THE ELEVATOR!", 10f);
    }
}