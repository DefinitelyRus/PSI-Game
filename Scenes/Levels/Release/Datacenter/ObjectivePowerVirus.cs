using Godot;
using CommonScripts;
namespace Game;

public partial class ObjectivePowerVirus : StandardPanel {

    public override async void Interact(StandardCharacter character) {
        // Check if the virus has already been deployed
        Variant? alreadyActivated = GameManager.GetGameData("L4_PowerVirusInstalled", null);
        if (alreadyActivated != null) return;

        Log.Me(() => $"{character.CharacterName} has deployed the power virus!");

        GameManager.SetGameData("L4_PowerVirusInstalled", null, true);

    // Count this as a required objective completion for pacing.
    AIDirector.RegisterRequiredObjectiveCompletion();

        // Disable panel after interaction
        IsEnabled = false;
        Activated = true;

        if (ActivationSound != null) AudioManager.StreamAudio(ActivationSound);

        UIManager.SetBottomOverlayText("The power virus is installed!", 2f);

        await ToSignal(GetTree().CreateTimer(2f), "timeout");

        UIManager.SetBottomOverlayText("Let's disable the heat control system...", 3f);
    }
}