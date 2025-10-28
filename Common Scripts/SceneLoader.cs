using Godot;
namespace CommonScripts;

public partial class SceneLoader : Node
{   
    #region Nodes & Components

    [ExportGroup("Nodes & Components")]
    [Export] public Master Master { get; private set; } = null!;

    [Export] public Node Theatre { get; private set; } = null!;

    [Export] public PackedScene MainMenu { get; private set; } = null!;

    [Export] public PackedScene DevScene { get; private set; } = null!;

    [Export] public PackedScene[] Levels { get; private set; } = [];

    [Export] public Node LoadedScene { get; private set; } = null!;

    #endregion

    #region Debugging

    [ExportGroup("Debugging")]
    [Export] public bool LogReady = false;
    [Export] public bool SuppressWarnings = false;

    #endregion

    #region Level Management

    public PackedScene? GetLevel(uint levelIndex) {
        PackedScene? retrievedLevel = (levelIndex < Levels.Length) ? Levels[levelIndex] : null;

        if (retrievedLevel == null) Log.Err(() => $"Level of index {levelIndex} not found.");

        return retrievedLevel;
    }

    public void LoadLevel(uint levelIndex) {
        UnloadLevel(false);

        PackedScene? levelToLoad = GetLevel(levelIndex);

        if (levelToLoad == null) {
            Log.Err(() => $"Level of index {levelIndex} not found.");
            return;
        }

        Theatre.AddChild(levelToLoad.Instantiate());
        return;
    }

    public void UnloadLevel(bool returnToMainMenu = true) {
        if (LoadedScene == null || Theatre.GetChildCount() == 0) return;

        Theatre.RemoveChild(LoadedScene);

        if (returnToMainMenu) Theatre.AddChild(MainMenu.Instantiate());
    }

    #endregion

    #region Godot Callbacks

    public override void _EnterTree() {
		Log.Me(() => "A SceneLoader has entered the tree. Checking properties...", LogReady);

        #region Node Checks

        if (Master == null) {
            Log.Err(() => "Master is not assigned. Cannot proceed.");
            return;
        }

        if (Theatre == null) {
            Log.Err(() => "Theatre is not assigned. Nowhere to put loaded scenes into.");
            return;
        }

        if (MainMenu == null) {
            Log.Err(() => "MainMenu is not assigned. Cannot proceed.");
            return;
        }

        #endregion

        #region Level Checks
        
        if (Levels.Length == 0 && !SuppressWarnings) Log.Warn(() => "No levels assigned.");

        for (int i = 0; i < Levels.Length; i++) {
            if (Levels[i] == null) Log.Warn(() => $"Level of index {i} is not assigned.");
        }

        #endregion

        Log.Me(() => "Done!", LogReady);
		return;
    }

    public override void _Ready() {
		Log.Me(() => "Readying SceneLoader...", LogReady, true);

        if (DevScene != null) {
            if (!SuppressWarnings) Log.Warn(() => "Currently using `DevScene`. `MainMenu` will not be loaded.", LogReady);
            Theatre.AddChild(DevScene.Instantiate());
        }

        else if (MainMenu != null) {
            LoadedScene = MainMenu.Instantiate();
            Theatre.AddChild(LoadedScene);
        }

        Log.Me(() => "Done!", LogReady);
		return;
    }

    #endregion

}
