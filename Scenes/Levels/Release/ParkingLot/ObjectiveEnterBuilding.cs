using CommonScripts;
using Godot;
namespace Game;

public partial class ObjectiveEnterBuilding : StandardPanel {
    [Export] public float CameraPromptCooldown = 5f;
    private float _cameraPromptTimer = 0f;
    [Export] public float InteractCooldown = 5f;
    private float _interactCooldownTimer = 0f;
    public override void Interact(StandardCharacter character) {
        // Suppress repeated interactions for a short period
        if (_interactCooldownTimer > 0f) return;
        Variant? gotKeysData = GameManager.GetGameData("L1_GotKeys", null);

        if (gotKeysData == null) {
            Log.Me(() => $"{character.CharacterName} tried to enter the building but doesn't have the keys!");
            Node currentLevel = SceneLoader.Instance.LoadedScene;

            if (_cameraPromptTimer <= 0f) {
                if (currentLevel is Level level) {
                    Node2D objective = level.CameraNodePaths[0];
                    CameraMan.SetTarget(objective);
                }
                _cameraPromptTimer = CameraPromptCooldown;
            }

            UIManager.SetBottomOverlayText("We need the keys from the car first.");
            _interactCooldownTimer = InteractCooldown;
            return;
        }

        foreach (StandardCharacter unit in Commander.GetAllUnits()) {
            if (!unit.IsAlive) continue;

            bool isAtLocation = ScanForUnit(unit);
            if (!isAtLocation) return;
        }

        GameManager.SetGameData("L1_EnteredBuilding", null, true);

        Log.Me(() => $"{character.CharacterName} has entered the building!");

    // Count this as a required objective completion for pacing.
    AIDirector.RegisterRequiredObjectiveCompletion();

        // Disable panel after interaction
        IsEnabled = false;
        Activated = true;

        SceneLoader.NextLevel();
    }

	public override void _Process(double delta) {
        ScanForPlayer();
        HighlightPanel(delta);
    if (_cameraPromptTimer > 0f) _cameraPromptTimer -= (float) delta;
    if (_interactCooldownTimer > 0f) _interactCooldownTimer -= (float) delta;
    }

}
