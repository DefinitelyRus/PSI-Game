using Godot;
using CommonScripts;
namespace Game;

public partial class ObjectivePlantCharge : StandardPanel {

    [Export] public AudioStream? MusicOnPlant = null;
    [Export] public Node2D[] CameraNodes = [];
    public override void Interact(StandardCharacter character) {
        Variant? alreadyActivated = GameManager.GetGameData("L4_ChargePlanted", null);
        if (alreadyActivated != null) return;

        Log.Me(() => $"{character.CharacterName} has planted the charge at the datacenter!");

        GameManager.SetGameData("L4_ChargePlanted", null, true);

        if (CameraNodes.Length > 0) CameraMan.SetCameraPath(CameraNodes);

        if (MusicOnPlant != null) AudioManager.StreamAudio(MusicOnPlant);
        if (ActivationSound != null) AudioManager.StreamAudio(ActivationSound);

        // Disable panel after interaction
        IsEnabled = false;
        Activated = true;
    }

    public override void _Process(double delta) {
        ScanForPlayer();
        HighlightPanel(delta);
    }

}