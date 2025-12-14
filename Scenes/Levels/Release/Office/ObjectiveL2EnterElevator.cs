using CommonScripts;
using Godot;
namespace Game;

public partial class ObjectiveL2EnterElevator : StandardPanel {
    [Export] public float CameraPromptCooldown = 5f;
    private float _cameraPromptTimer = 0f;
    [Export] public float InteractCooldown = 5f;
    private float _interactCooldownTimer = 0f;
    public override void Interact(StandardCharacter character) {
        // Suppress repeated interactions for a short period
        if (_interactCooldownTimer > 0f) return;
        Variant? gotKeycard1Data = GameManager.GetGameData("L2_GotKeycard1", null);
        Variant? gotKeycard2Data = GameManager.GetGameData("L2_GotKeycard2", null);

        if (gotKeycard1Data == null) {
            Log.Me(() => $"{character.CharacterName} tried to access the elevator but does not have keycard 1.");
            Node currentLevel = SceneLoader.Instance.LoadedScene;

            if (_cameraPromptTimer <= 0f) {
                if (currentLevel is Level level) {
                    Node2D objective = level.CameraNodePaths[0];
                    CameraMan.SetTarget(objective);
                }
                _cameraPromptTimer = CameraPromptCooldown;
            }

            UIManager.SetBottomOverlayText("We need at least 2 keycards...");
            _interactCooldownTimer = InteractCooldown;
            return;
        }


        if (gotKeycard2Data == null) {
            Log.Me(() => $"{character.CharacterName} tried to access the elevator but does not have keycard 2.");
            Node currentLevel = SceneLoader.Instance.LoadedScene;

            if (_cameraPromptTimer <= 0f) {
                if (currentLevel is Level level) {
                    Node2D objective = level.CameraNodePaths[1];
                    CameraMan.SetTarget(objective);
                }
                _cameraPromptTimer = CameraPromptCooldown;
            }

            UIManager.SetBottomOverlayText("We need at least 2 keycards...");
            _interactCooldownTimer = InteractCooldown;
            return;
        }

        // Check if all alive units are at the elevator
        foreach (StandardCharacter unit in Commander.GetAllUnits()) {
            if (!unit.IsAlive) continue;

            bool isAtLocation = ScanForUnit(unit);
            if (!isAtLocation) return;
        }
        
        GameManager.SetGameData("L2_EnteredElevator", null, true);
        Log.Me(() => $"All units have entered the elevator!");

    // Count this as a required objective completion for pacing.
    AIDirector.RegisterRequiredObjectiveCompletion();

        // Disable panel after interaction
        IsEnabled = false;
        Activated = true;

        SceneLoader.NextLevel();
    }

	public override void _Process(double delta) {
        if (Master.IsPaused) return;

        ScanForPlayer();
        HighlightPanel(delta);
        
        if (_cameraPromptTimer > 0f) _cameraPromptTimer -= (float) delta;
        if (_interactCooldownTimer > 0f) _interactCooldownTimer -= (float) delta;
    }

}
