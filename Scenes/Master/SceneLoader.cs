using Godot;
namespace CommonScripts;

public partial class SceneLoader : Node
{   
    
    #region Instance Members

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

        #region Instance Check

        if (Instance != null) {
            Log.Err("Multiple instances of SceneLoader detected. There should only be one SceneLoader in the scene.");
            QueueFree();
            return;
        }

        Instance = this;

        #endregion

        Log.Me(() => "Done!", LogReady);
        return;
    }

    public override void _Ready() {
		Log.Me(() => "Readying SceneLoader...", LogReady, true);

        if (DevScene != null) {
            if (!SuppressWarnings) Log.Warn(() => "Currently using `DevScene`. `MainMenu` will not be loaded.", LogReady);
            LoadLevel(DevScene);
		}

        else if (MainMenu != null) {
            LoadedScene = MainMenu.Instantiate();
            Theatre.AddChild(LoadedScene);
        }

        Log.Me(() => "Done!", LogReady);
		return;
    }

    #endregion

    #endregion

    #region Static Members

    public static SceneLoader Instance { get; private set; } = null!;

    #region Level Management

    public static PackedScene? GetLevel(uint levelIndex) {
        PackedScene? retrievedLevel = (levelIndex < Instance.Levels.Length) ? Instance.Levels[levelIndex] : null;

        if (retrievedLevel == null) Log.Err(() => $"Level of index {levelIndex} not found.");

        return retrievedLevel;
    }

    public static async void LoadLevel(uint levelIndex) {
        PackedScene? levelToLoad = GetLevel(levelIndex);

        if (levelToLoad == null) {
            Log.Err(() => $"Level of index {levelIndex} not found.");
            return;
        }

        LoadLevel(levelToLoad);
        return;
    }

    public static async void LoadLevel(PackedScene levelScene) {
        Level level = levelScene.Instantiate<Level>();
        UIManager.SetHUDVisible(false);
        UIManager.StartTransition($"Loading {level.Name}...");
        AudioManager.FadeOutAudio();
        await Instance.ToSignal(Instance.GetTree().CreateTimer(2f), "timeout");

        StandardCharacter[] units = [.. Commander.GetAllUnits()];

        if (units.Length > 0) {
            foreach (StandardCharacter unit in units) {
                level.SpawnUnit(unit);
            }
        }

        else Log.Err(() => "No units registered with Commander to spawn in the level.");

        UnloadLevel(false);
        Instance.Theatre.AddChild(level);
        Instance.LoadedScene = level;

        await Instance.ToSignal(Instance.GetTree().CreateTimer(.5f), "timeout");
        GameManager.GameEnded = false;
        UIManager.EndTransition();
        return;
	}


	public static void UnloadLevel(bool returnToMainMenu = true) {
        if (Instance.LoadedScene == null || Instance.Theatre.GetChildCount() == 0) return;

        AIDirector.ClearEnemies();

        Instance.Theatre.RemoveChild(Instance.LoadedScene);
        Instance.LoadedScene.QueueFree();
        Instance.LoadedScene = null!;

        if (returnToMainMenu) Instance.Theatre.AddChild(Instance.MainMenu.Instantiate());
    }


    public static void NextLevel() {
        if (Instance.LoadedScene is Level currentLevel) {
            uint nextLevelIndex = currentLevel.LevelIndex + 1;
            LoadLevel(nextLevelIndex);
        }
    }

    #endregion

    #endregion

}
