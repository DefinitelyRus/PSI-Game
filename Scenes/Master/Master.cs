using Godot;
namespace CommonScripts;

public partial class Master : Node {

    #region Instance Members
    
    #region Nodes & Components

    [ExportGroup("Nodes & Components")]
    [Export] public SceneLoader SceneLoader { get; private set; } = null!;
    [Export] public InputManager InputManager { get; private set; } = null!;
    [Export] public TextureRect Background { get; private set; } = null!;

    #endregion

    #region Debugging

    [ExportGroup("Debugging")]
    [Export] public bool LogReady = false;

	#endregion

	#region Godot Callbacks

	public override void _EnterTree() {
        // Check if there's already a Master instance
        if (Instance != null) {
            Log.Err(() => "Another Master instance already exists! This instance will remove itself.", LogReady);
            QueueFree();
            return;
        }

        Instance = this;
    }


    public override void _Ready() {
        Log.Me(() => $"Readying Master. Passing to StandardItem...", LogReady);

        if (SceneLoader == null) {
            Log.Err(() => "SceneLoader is not assigned. Cannot proceed.", LogReady);
            return;
        }

        if (InputManager == null) {
            Log.Err(() => "InputManager is not assigned. Cannot proceed.", LogReady);
            return;
        }

        Log.Me(() => "Done!", LogReady);
        return;
    }
    
    #endregion

    #endregion

    #region Static Members
    public static Master Instance { get; private set; } = null!;

    public static bool IsPaused { get; set; } = false;

    #endregion

}
