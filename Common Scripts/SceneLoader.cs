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

    public PackedScene GetLevel(uint levelIndex, bool v = false, int s = 0)
    {
        Log.Me(() => $"Retrieving level: {levelIndex}...", v, s + 1);

        PackedScene retrievedLevel = (levelIndex < Levels.Length) ? Levels[levelIndex] : null!;

        Log.Me(() => $"Retrieved level: {retrievedLevel.ResourceName} ({retrievedLevel.GetType().Name}).", v, s + 1);
        Log.Me(() => "Done!", v, s + 1);
        return retrievedLevel;
    }

    public void LoadLevel(uint levelIndex, bool v = false, int s = 0) {
        Log.Me(() => $"Loading level {levelIndex}...", v, s + 1);

        PackedScene levelToLoad = GetLevel(levelIndex, v, s + 1);

        if (levelToLoad == null) {
            Log.Me(() => $"Level not found: {levelIndex}.", v, s + 1);
            return;
        }

        Theatre.AddChild(levelToLoad.Instantiate());

        Log.Me(() => "Done!", v, s + 1);
        return;
    }

    public void UnloadLevel(bool returnToMainMenu = true, bool v = false, int s = 0) {
        Log.Me(() => $"Unloading level: {LoadedScene.Name}...", v, s + 1);

        if (LoadedScene == null || Theatre.GetChildCount() == 0) {
            Log.Me(() => $"No level is currently loaded.", v, s + 1);
            return;
        }

        Theatre.RemoveChild(LoadedScene);

        if (returnToMainMenu) {
            Log.Me(() => "Returning to main menu...", v, s + 1);
            Theatre.AddChild(MainMenu.Instantiate());
        }

        Log.Me(() => "Done!", v, s + 1);
        return;
    }

    #endregion

    #region Godot Callbacks

    public override void _EnterTree() {
        Log.Me(() => "Entering SceneLoader...", LogReady);

        #region Node Checks

        if (Master == null) {
            Log.Err(() => "Master is not assigned. Cannot proceed.", LogReady);
            return;
        }

        if (Theatre == null) {
            Log.Err(() => "Theatre is not assigned. Nowhere to put loaded scenes into.", LogReady);
            return;
        }

        if (MainMenu == null) {
            Log.Err(() => "MainMenu is not assigned. Cannot proceed.", LogReady);
            return;
        }

        #endregion

        #region Level Checks

        Log.Me(() => "Checking levels...", LogReady);
        
        if (Levels.Length == 0 && !SuppressWarnings) Log.Warn(() => "No levels assigned.", LogReady);

        for (int i = 0; i < Levels.Length; i++) {
            if (Levels[i] == null) Log.Warn(() => $"Level of index {i} is not assigned.", LogReady);
        }

        #endregion

        Log.Me(() => "Done!", LogReady);
        return;
    }

    public override void _Ready() {
        Log.Me(() => "Readying SceneLoader...", LogReady);

        if (DevScene != null) {
            if (!SuppressWarnings) Log.Warn(() => "Currently using `DevScene`. `MainMenu` will not be loaded.", LogReady);
            Theatre.AddChild(DevScene.Instantiate());
        }

        else if (MainMenu != null) {
            Log.Me(() => "Loading `MainMenu`...", LogReady);
            Theatre.AddChild(MainMenu.Instantiate());
        }

        Log.Me(() => "Done!", LogReady);
        return;
    }

    #endregion

}
