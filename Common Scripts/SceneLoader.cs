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

    public PackedScene? GetLevel(uint levelIndex, Context c = null!) {
        PackedScene? retrievedLevel = (levelIndex < Levels.Length) ? Levels[levelIndex] : null;

        if (retrievedLevel == null) c.Err(() => $"Level of index {levelIndex} not found.");

        return retrievedLevel;
    }

    public void LoadLevel(uint levelIndex, Context c = null!) {
        UnloadLevel(false);

        PackedScene? levelToLoad = GetLevel(levelIndex);

        if (levelToLoad == null) {
            c.Err(() => $"Level of index {levelIndex} not found.");
            return;
        }

        Theatre.AddChild(levelToLoad.Instantiate());
        return;
    }

    public void UnloadLevel(bool returnToMainMenu = true, Context c = null!) {
        if (LoadedScene == null || Theatre.GetChildCount() == 0) return;

        Theatre.RemoveChild(LoadedScene);

        if (returnToMainMenu) Theatre.AddChild(MainMenu.Instantiate());
    }

    #endregion

    #region Godot Callbacks

    public override void _EnterTree() {
		Context c = new();
		c.Trace(() => "A SceneLoader has entered the tree. Checking properties...", LogReady);

        #region Node Checks

        if (Master == null) {
            c.Err(() => "Master is not assigned. Cannot proceed.", LogReady);
            return;
        }

        if (Theatre == null) {
            c.Err(() => "Theatre is not assigned. Nowhere to put loaded scenes into.", LogReady);
            return;
        }

        if (MainMenu == null) {
            c.Err(() => "MainMenu is not assigned. Cannot proceed.", LogReady);
            return;
        }

        #endregion

        #region Level Checks
        
        if (Levels.Length == 0 && !SuppressWarnings) c.Warn(() => "No levels assigned.", LogReady);

        for (int i = 0; i < Levels.Length; i++) {
            if (Levels[i] == null) c.Warn(() => $"Level of index {i} is not assigned.", LogReady);
        }

        #endregion

        c.Trace(() => "Done!", LogReady);
		c.End();
		return;
    }

    public override void _Ready() {
		Context c = new();
		c.Trace(() => "Readying SceneLoader...", LogReady);

        if (DevScene != null) {
            if (!SuppressWarnings) c.Warn(() => "Currently using `DevScene`. `MainMenu` will not be loaded.", LogReady);
            Theatre.AddChild(DevScene.Instantiate());
        }

        else if (MainMenu != null) {
            LoadedScene = MainMenu.Instantiate();
            Theatre.AddChild(LoadedScene);
        }

        c.Trace(() => "Done!", LogReady);
		c.End();
		return;
    }

    #endregion

}
