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
			return;
		}

		HUD.CallDeferred("set_visibility", visible, segment);
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
			HUD.CallDeferred("set_item", new(), slotIndex);
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
