using Godot;
using CommonScripts;
using System.Threading.Tasks;
namespace Game;

public partial class ObjectivePlantCharge : StandardPanel {

    [Export] public AudioStream? MusicOnPlant = null;
    [Export] public Node2D[] CameraNodes = [];
    [Export] public NavigationRegion2D? Nav = null;

    public override async void Interact(StandardCharacter character) {
        Variant? alreadyActivated = GameManager.GetGameData("L4_ChargePlanted", null);
        if (alreadyActivated != null) return;

        CameraNodes[^1] = character;

        if (CameraNodes.Length > 0) CameraMan.SetTarget(CameraNodes[^1]);

        // Disable panel after interaction
        IsEnabled = false;
        Activated = true;
        InputManager.AllowOverride = false;
        UIManager.SetHUDVisible(false);
        AIDirector.AllowSpawning = true;
        if (Nav != null) Nav.Enabled = true;

        await ToSignal(GetTree().CreateTimer(1.5f), "timeout");

        if (ActivationSound != null) AudioManager.StreamAudio(ActivationSound);

        if (CameraNodes.Length > 0) CameraMan.SetCameraPath(CameraNodes);

        UIManager.SetBottomOverlayText($"Install the power virus...", 2f);

        await ToSignal(GetTree().CreateTimer(2.7f), "timeout");
        UIManager.SetBottomOverlayText($"Disable the heat control...", 2f);

        await ToSignal(GetTree().CreateTimer(3f), "timeout");
        UIManager.SetBottomOverlayText($"And escape right before it detonates...", 3f);

        await ToSignal(GetTree().CreateTimer(4.1f), "timeout");

        UIManager.SetBottomOverlayText($"Good luck.", 3f);
        Log.Me(() => $"{character.CharacterName} has planted the charge at the datacenter!");

        if (MusicOnPlant != null) AudioManager.StreamAudio(MusicOnPlant);
        GameManager.SetGameData("L4_ChargePlanted", null, true);
        InputManager.AllowOverride = true;
    }

    public override void _Process(double delta) {
        ScanForPlayer();
        HighlightPanel(delta);
    }

	public override void _Ready() {
        AIDirector.AllowSpawning = false;

        // Disable nav region until charge is planted
        if (Nav != null) Nav.Enabled = false;

		base._Ready();
	}

}