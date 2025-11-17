using Godot;
namespace CommonScripts;

public partial class UIManager : CanvasLayer {

	#region Instance Members

	#region Nodes & Components

	[Export] MarginContainer HUDNode = null!;
	[Export] Control PopupNode = null!;

	#endregion

	#region Godot Callbacks

	public override void _EnterTree() {
		Instance = this;
	}

	public override void _Ready() {
		SetItemIcon(0, null!);
		SetItemIcon(1, null!);
		SetItemIcon(2, null!);
		SetItemIcon(3, null!);
		SetItemIcon(4, null!);
	}


	public override void _Process(double delta) {
		Commander.SingleUnitControl();
	}

	#endregion

	#endregion

	#region Static Members

	public static UIManager Instance { get; private set; } = null!;
	private static MarginContainer HUD => Instance.HUDNode;
	private static Control Popup => Instance.PopupNode;

	public static void EnableUI(bool enable) {
		Instance.Visible = enable;
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

	public static void SetPower(int power, int maxPower) {
		HUD.CallDeferred("set_power", power, maxPower);
	}

	public static void SetOpenSlots(int count) {
		HUD.CallDeferred("manage_slots", count);
	}

	public static void SetItemIcon(int slotIndex, Texture2D icon) {
		HUD.CallDeferred("set_item", icon, slotIndex);
	}

	public static void SetCharPreview(Sprite2D sprite) {
		AtlasTexture? atlas = sprite.Texture as AtlasTexture;
		Image? source = atlas?.Atlas.GetImage();
		Rect2? region = atlas?.Region;

		if (atlas == null) {
			Log.Me(() => "SetCharPreview: Sprite atlas is null. Cannot set character preview.");
			return;
		}

		if (source == null) {
			Log.Me(() => "SetCharPreview: Sprite atlas image is null. Cannot set character preview.");
			return;
		}

		if (region == null) {
			Log.Me(() => "SetCharPreview: Sprite atlas region is null. Cannot set character preview.");
			return;
		}

		Rect2I regionI = new(
			(int)region.Value.Position.X,
			(int)region.Value.Position.Y,
			(int)region.Value.Size.X,
			(int)region.Value.Size.Y
		);

		Image image = source.GetRegion(regionI);
		ImageTexture frameTexture = ImageTexture.CreateFromImage(image);
		HUD.CallDeferred("set_char_preview", frameTexture);
	}

	#endregion

	#endregion
}
