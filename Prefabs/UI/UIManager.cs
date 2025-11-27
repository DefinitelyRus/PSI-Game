using Godot;
using Game;
namespace CommonScripts;

public partial class UIManager : CanvasLayer {

	#region Instance Members

	#region Nodes & Components

	[Export] MarginContainer HUDNode = null!;
	[Export] Control PopupNode = null!;
	[Export] TextureRect Transition = null!;
	[Export] Control OnScreenText = null!;
	[Export] public float UnpoweredAlpha { get; private set; } = 0.5f;

	#endregion

	#region Godot Callbacks

	public override void _EnterTree() {
		if (HUDNode == null) {
            Log.Err(() => "HUDNode is null in UIManager. Please assign it in the inspector.");
			return;
        }

		if (PopupNode == null) {
			Log.Err(() => "PopupNode is null in UIManager. Please assign it in the inspector.");
			return;
		}

		if (Transition == null) {
			Log.Err(() => "Transition is null in UIManager. Please assign it in the inspector.");
			return;
		}

		if (OnScreenText == null) {
			Log.Err(() => "OnScreenText is null in UIManager. Please assign it in the inspector.");
			return;
		}

		Instance = this;
	}

	public override void _Ready() {
		SetItemIcon(0, (Texture2D)null!);
		SetItemIcon(1, (Texture2D)null!);
		SetItemIcon(2, (Texture2D)null!);
		SetItemIcon(3, (Texture2D)null!);
		SetItemIcon(4, (Texture2D)null!);

		// Start with zero accessible slots until a character explicitly sets them
		SetOpenSlots(0);
	}

	#endregion

	#endregion

	#region Static Members

	public static UIManager Instance { get; private set; } = null!;
	private static MarginContainer HUD => Instance.HUDNode;
	public static Control Popup => Instance.PopupNode;

	public static void EnableUI(bool enable) {
		Instance.Visible = enable;
		if (enable && UpgradeManager.Instance != null) {
			UpgradeManager.Instance.RefreshInventoryUI();
		}

	// Timer is visible only when UI is enabled, HUD is visible, and a level timer is active
	bool shouldShowTimer = enable && HUD.Visible && GameManager.TimeRemaining < double.MaxValue;
	SetTimerEnabled(shouldShowTimer);
	}

	#region Popup

	public static void SetPopupVisible(bool visible) {
		Popup.Visible = visible;
	}

	public static void SetPopupText(string header, string message) {
		Popup.CallDeferred("set_text", header, message);
	}

	public static void SetButtonEnabled(int row, int index, bool enabled) {
		Popup.CallDeferred("set_btn", enabled, row, index);
	}

	public static void SetButtonText(int row, int index, string text) {
		Popup.CallDeferred("set_btn_text", text, row, index);
	}

	#endregion

	#region HUD

	/// <summary>
	/// Sets HUD visibility. <br/>
	/// <list type="bullet">
	/// <item><c>-1</c> to set the HUD as a whole.</item>
	/// <item><c>0</c> to set only the HUD itself.</item>
	/// <item><c>1</c> to set only the character info.</item>
	/// <item><c>2</c> to set only the inventory.</item>
	/// </list>
	/// </summary>
	/// <param name="visible"></param>
	/// <param name="segment"></param>
	public static void SetHUDVisible(bool visible, int segment = 0) {
		if (segment == -1) {
			HUD.Visible = visible;
			HUD.CallDeferred("set_visibility", visible, 0);
			HUD.CallDeferred("set_visibility", visible, 1);
			HUD.CallDeferred("set_visibility", visible, 2);
			// Control timer with main HUD visibility
			bool shouldShowTimerAll = visible && Instance.Visible && GameManager.TimeRemaining < double.MaxValue;
			SetTimerEnabled(shouldShowTimerAll);
			return;
		}

		HUD.CallDeferred("set_visibility", visible, segment);

		// If toggling the main HUD segment, mirror timer visibility
		if (segment == 0) {
			bool shouldShowTimer = visible && Instance.Visible && GameManager.TimeRemaining < double.MaxValue;
			SetTimerEnabled(shouldShowTimer);
		}
	}

	public static void SetHealth(float health, float maxHealth) {
		HUD.CallDeferred("update_health", health, maxHealth);
	}

	public static void SetHealthColor(string character) {
		HUD.CallDeferred("set_health_color", character);
	}

	public static void SetCharacterName(string character) {
		HUD.CallDeferred("set_character_name", character);
	}

	public static void SetPower(int power, int maxPower) {
		HUD.CallDeferred("set_power", power, maxPower);
	}

	public static void SetOpenSlots(int count) {
		HUD.CallDeferred("manage_slots", count);
	}

	public static void SetItemIcon(int slotIndex, Texture2D icon) {
		HUD.CallDeferred("set_item", icon, slotIndex);
	}

	public static void SetItemIcon(int slotIndex, Sprite2D? icon) {
		if (icon == null) {
			SetItemIcon(slotIndex, (Texture2D)null!);
			return;
		}

		Texture2D? texture;
		if (icon.RegionEnabled) {
			AtlasTexture atlasTexture = new() {
				Atlas = icon.Texture,
				Region = icon.RegionRect
			};
			texture = atlasTexture;
		}
		
		else texture = icon.Texture;

		HUD.CallDeferred("set_item", texture, slotIndex);
	}

	public static void SetItemAlpha(int slotIndex, float alpha) {
		HUD.CallDeferred("set_item_alpha", slotIndex, alpha);
	}

	public static void SetTimerEnabled(bool enabled) {
		Instance.OnScreenText.CallDeferred("set_timer_enabled", enabled);
	}

	public static void SetTimerText(double time) {
		int minutes = (int)(time / 60);
		int seconds = (int)(time % 60);
		string text = $"{minutes:00}:{seconds:00}";
		Instance.OnScreenText.CallDeferred("set_timer_text", text);
	}

	public static void SetTimerText(string text) {
		Instance.OnScreenText.CallDeferred("set_timer_text", text);
	}

	public static void SetTimerColor(Color color) {
		Instance.OnScreenText.CallDeferred("set_timer_color", color);
	}

	public static void SpawnIndicator(StandardCharacter owner, Vector2 position) {
		MovementMarker indicator = Commander.Instance.IndicatorNodeScene.Instantiate<MovementMarker>();
		if (SceneLoader.Instance.LoadedScene is Level currentLevel) {
			indicator.OwnerCharacter = owner;
			currentLevel.AddChild(indicator);
			indicator.GlobalPosition = position;
		}

		else {
			Log.Warn("Cannot add indicator node to current scene. Scene is not a Level.", true, true);
			indicator.QueueFree();
		}
	}

	#endregion

	#region Transition

	public static void StartTransition(string text = "") {
		Instance.Transition.CallDeferred("start_transition", text);
    }

	public static void EndTransition() {
		Instance.Transition.CallDeferred("end_transition");
	}

	public static void Reset() {
		Instance.Transition.CallDeferred("reset");
    }

	#endregion

	#region Overlay Text

	public static void SetCenterOverlayText(string text, float duration = 2f) {
		Instance.OnScreenText.CallDeferred("show_center_text", text, duration);
	}

	public static void SetBottomOverlayText(string text, float duration = 2f) {
		Instance.OnScreenText.CallDeferred("show_subtitle", text, duration);
    }

	#endregion

	#endregion
}
