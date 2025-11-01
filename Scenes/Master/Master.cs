using Godot;
namespace CommonScripts;

public partial class Master : Node {

    #region Instance Members
    
    #region Nodes & Components

    [ExportGroup("Nodes & Components")]
    [Export] public SceneLoader SceneLoader { get; private set; } = null!;
    [Export] public InputManager InputManager { get; private set; } = null!;

    #endregion

    #region Debugging

    [ExportGroup("Debugging")]
    [Export] public bool LogReady = false;

    #endregion

    #region Godot Callbacks

    public override void _Ready() {
        Log.Me(() => $"Readying Master. Passing to StandardItem...", LogReady);

        if (SceneLoader == null) {
            Log.Err(() => "SceneLoader is not assigned. Cannot proceed.", LogReady);
            DebugPause();
            return;
        }

        if (InputManager == null) {
            Log.Err(() => "InputManager is not assigned. Cannot proceed.", LogReady);
            DebugPause();
            return;
        }

        Log.Me(() => "Done!", LogReady);
        return;
    }
    
    #endregion

    #endregion

    #region Static Members

    public static Master Instance { get; private set; } = null!;

    #endregion

    #region Methods

    public static void DebugPause() {
        Log.Me(() => "Freezing game...");

        Instance.GetTree().Paused = true;

        Log.Me(() => "Done!");
        return;
    }

    #endregion

}
