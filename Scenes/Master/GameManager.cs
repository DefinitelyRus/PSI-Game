using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
namespace CommonScripts;

public partial class GameManager : Node2D {

    #region Instance Members

    #region Godot Callbacks

    public override void _EnterTree() {
        if (Instance != null) {
            Log.Err("Multiple instances of GameManager detected! There should only be one instance in the scene tree.");
            QueueFree();
            return;
        }

        Instance = this;
    }

	public override void _Process(double delta) {
		CallDeferred(nameof(CheckLoseConditions));
	}

    #endregion

    #endregion

    #region Static Members

    public static GameManager Instance { get; private set; } = null!;

    #region Game Data Management

    public static System.Collections.Generic.Dictionary<string, Data> GameData { get; private set; } = [];


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

    public static async void CheckLoseConditions() {
        if (GameEnded) return;

        // Lose if both units are dead.
        if (!Commander.GetAllUnits().Where(u => u.IsAlive).Any()) {
            GameEnded = true;

            UIManager.StartTransition("Mission Failed");

            void TryAgain() {
                Commander.Initialize();
                SceneLoader.LoadLevel(0);
            }

            void ExitToMainMenu() {
                SceneLoader.UnloadLevel(true);
            }

            await Instance.ToSignal(Instance.GetTree().CreateTimer(4.0), "timeout");

            //TEMP
            TryAgain();
            return;

            //TODO: Fix popup UI
            // UIManager.SetPopupText("Mission Failed", "Try again?");
            // UIManager.SetPopupVisible(true);
            // UIManager.SetButtonEnabled(1, 1, true);
            // UIManager.SetButtonText(1, 1, "Restart");
            // UIManager.SetButtonEnabled(2, 1, true);
            // UIManager.SetButtonText(2, 1, "Main Menu");

            // // Connect to button signals
            // Control popup = UIManager.Popup;
            // Button retryButton = popup.Get("row1_buttons").AsGodotArray<Button>()[1];
            // Button mainMenuButton = popup.Get("row2_buttons").AsGodotArray<Button>()[1];
            // retryButton.Pressed += TryAgain;
            // mainMenuButton.Pressed += ExitToMainMenu;
        }
    }

    #endregion

    #endregion

}

public class Data(string key, string? ownerID, Variant value) {
	public string Key { get; set; } = key;
	public string? OwnerID { get; set; } = ownerID;
	public Variant Value { get; set; } = value;
}