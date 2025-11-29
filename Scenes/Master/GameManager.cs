using System.Collections.Generic;
using System.Linq;
using Game;
using Godot;
namespace CommonScripts;

public partial class GameManager : Node2D {

    #region Instance Members

    // Timer but easily accessible from GDscript
    public double Timer => TimeRemaining;

    #region Godot Callbacks

    public override void _EnterTree() {
        if (Instance != null) {
            Log.Err("Multiple instances of GameManager detected! There should only be one instance in the scene tree.");
            QueueFree();
            return;
        }

        Instance = this;
        DataManager.RecordGameStart();
    }

	public override void _Process(double delta) {
        // Update timer text and color before checking lose conditions
        if (!GameEnded) {
            // Only update timer if time limit is active
            if (!ManualTimerCheck && TimeRemaining < double.MaxValue) {
                // Clamp to non-negative for display and stop at zero
                double displayTime = TimeRemaining;
                if (displayTime <= 0) {
                    UIManager.SetTimerText(0);
                    UIManager.SetTimerColor(Colors.Red);
                }

                else {
                    UIManager.SetTimerText(displayTime);
                    // Switch to red at 30s remaining
                    if (displayTime <= 30.0) UIManager.SetTimerColor(Colors.Red);
                    else UIManager.SetTimerColor(Colors.White);
                }
            }
        }

        CallDeferred(nameof(CheckLoseConditions), false);
	}

    #endregion

    #endregion

    #region Static Members

    public static GameManager Instance { get; private set; } = null!;

    #region Game Data Management

    public static Dictionary<string, Data> GameData { get; private set; } = [];


    public static Variant? GetGameData(string key, string? ownerID) {
        bool hasData = GameData.TryGetValue(key, out Data? data);
        if (hasData) {
            if (data == null) {
                Log.Err(() => $"Data for key '{key}' is null. This should not happen.");
                return null;
            }

            if (data.OwnerID == ownerID) return data.Value;
        }

        return null;
    }


    public static void SetGameData(string key, string? ownerID, Variant value) {
        // Try to find existing data
        bool hasData = GameData.TryGetValue(key, out Data? data);
        if (hasData) {
            if (data == null) {
                Log.Err(() => $"Data for key '{key}' is null. This should not happen.");
                return;
            }

            // Assign value if ownerID matches
            if (data.OwnerID == ownerID) {
                data.Value = value;
                return;
            }
        }

        // Create new data if not found
        Data newData = new(key, ownerID, value);
        GameData.Add(key, newData);
    }


    public static void RemoveGameData(string key, string? ownerID) {
        bool hasData = GameData.TryGetValue(key, out Data? data);
        if (hasData) {
            if (data == null) {
                Log.Err(() => $"Data for key '{key}' is null. This should not happen.");
                return;
            }

            if (data.OwnerID == ownerID) {
                GameData.Remove(key);
                return;
            }
        }
    }


    public static void RemoveAllWithKey(string key) {
        bool hasData = GameData.TryGetValue(key, out Data? data);
        if (hasData) GameData.Remove(key);
    }


    public static void RemoveAllFromOwner(string? ownerID) {
        foreach (Data data in GameData.Values) {
            if (data.OwnerID == ownerID) {
                GameData.Remove(data.Key);
            }
        }
    }


    public static void ClearAllData() {
        GameData.Clear();
    }


    public static void PrintAllData() {
        foreach (Data data in GameData.Values) {
            Log.Me(() => $"Key: {data.Key}, OwnerID: {data.OwnerID}, Value: ({data.Value.VariantType}) {data.Value}");
        }
    }

    #endregion

    #region Game State Management

    public static bool GameEnded { get; set; } = false;

    public static double TimeRemaining { get; set; } = double.MaxValue;

    public static bool ManualTimerCheck { get; set; } = false;

    public static void ResetGame()
    {
        // Reset core flags
        GameEnded = false;
        ManualTimerCheck = false;
        TimeRemaining = double.MaxValue;

        // Clear dynamic game data
        ClearAllData();
        AIDirector.ResetMetrics();
        Commander.ResetUnits();
        Commander.Initialize();
        UIManager.SetTimerEnabled(false);
        UIManager.SetHUDVisible(false, 0);
    }

    public static async void HandleWin(Level level)
    {
        if (GameEnded) return;
        GameEnded = true;
        DataManager.RecordLevelCompletion(level);

        UIManager.StartTransition("Mission Complete");
        await Instance.ToSignal(Instance.GetTree().CreateTimer(3.0), "timeout");

        ResetGame();
        SceneLoader.UnloadLevel(true); // Return to main menu
    }

    public static async void CheckLoseConditions(bool loseOverride = false) {
        if (GameEnded) return;

        Node? levelNode = SceneLoader.Instance.LoadedScene;
        if (levelNode == null) return;
        if (levelNode is not Level level) return;

        bool timesUp = false;
        if (!ManualTimerCheck && TimeRemaining < double.MaxValue) {
            TimeRemaining -= Instance.GetProcessDeltaTime();
        }
        if (level.LevelTimeLimit > 0) {
            timesUp = TimeRemaining <= 0 && !ManualTimerCheck;
        }

        bool allDead = !Commander.GetAllUnits().Where(u => u.IsAlive).Any();

        if (allDead || timesUp || loseOverride) {
            GameEnded = true;
            DataManager.RecordLevelCompletion(level);
            TimeRemaining = double.MaxValue;

            UIManager.StartTransition("Mission Failed");
            if (timesUp) {
                foreach (StandardCharacter unit in Commander.GetAllUnits().Where(u => u.IsAlive)) {
                    unit.Kill();
                }
            }
            await Instance.ToSignal(Instance.GetTree().CreateTimer(4.0), "timeout");

            ResetGame();
            SceneLoader.LoadLevel(0);
            return;
        }
    }

    #endregion

    #region Random Drops

    [Export] public PackedScene[] RandomDropItems = [];

    public static UpgradeItem? GetRandomDropItem(Vector2? position = null) {
        if (Instance.RandomDropItems.Length == 0) {
            Log.Warn(() => "No random drop items defined in GameManager.", true, true);
            return null;
        }

        // Get random item based on weight
        int totalWeight = 0;
        List<UpgradeItem> sortedItems = [.. Instance.RandomDropItems
            .Select(scene => scene.Instantiate<UpgradeItem>())
            .OrderBy(item => item.ChanceWeight)
        ];

        foreach (UpgradeItem item in sortedItems) totalWeight += item.ChanceWeight;

        int randomValue = new RandomNumberGenerator().RandiRange(1, totalWeight);
        int cumulativeWeight = 0;
        UpgradeItem? toReturn = null;
        foreach (UpgradeItem item in sortedItems) {
            cumulativeWeight += item.ChanceWeight;

            // Select item if randomValue is within weight range
            if (randomValue <= cumulativeWeight) {
                if (position != null) item.GlobalPosition = position.Value;
                toReturn = item;
                break;
            }
        }

        foreach (UpgradeItem item in sortedItems) {
            if (item != toReturn) item.QueueFree();
        }

        return toReturn;
    }

    #endregion

    #endregion

}

public class Data(string key, string? ownerID, Variant value) {
	public string Key { get; set; } = key;
	public string? OwnerID { get; set; } = ownerID;
	public Variant Value { get; set; } = value;
}