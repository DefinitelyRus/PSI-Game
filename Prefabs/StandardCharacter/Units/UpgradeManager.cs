using System.Collections.Generic;
using System.Linq;
using CommonScripts;
using Godot;
namespace Game;

public partial class UpgradeManager : Node {

    #region Static
    public static UpgradeManager Instance { get; private set; } = null!;
    #endregion

    #region Properties

    #region Power

    [ExportGroup("Power Management")]
    [Export] public int MaxPower { get; private set; } = 2;
    public int CurrentMaxPower { get; set; } = 1;
    public int CurrentPower { get; set; } = 0;
    // Tracks powered items separately from any item equip state.
    private readonly HashSet<UpgradeItem> _poweredItems = new();

    [Export] public float PowerOnCooldown { get; private set; } = 0.5f;
    private double _cooldownTimer = 0d;
    public bool IsOnCooldown => _cooldownTimer > 0d;


    public int[] GetPoweredItems() {
        List<int> indices = [];
        for (int i = 0; i < Items.Count; i++) {
            if (_poweredItems.Contains(Items[i])) indices.Add(i);
        }
        return [.. indices];
    }


    public int GetPoweredItemCount() {
    return _poweredItems.Count;
    }


    public bool IsItemPowered(int index) {
        if (index < 0 || index >= Items.Count) return false;
        return _poweredItems.Contains(Items[index]);
    }

    public bool IsPowered(UpgradeItem item) => item != null && _poweredItems.Contains(item);
    public bool IsPoweredIndex(int index) => index >= 0 && index < Items.Count && _poweredItems.Contains(Items[index]);


    public void SetItemPower(int index, bool powered) {
        if (index < 0 || index >= Items.Count) return;

        UpgradeItem item = Items[index];

        if (powered) {
            // Toggle: if already powered, power off
            if (_poweredItems.Contains(item)) {
                item.PowerOff();
                _poweredItems.Remove(item);
                CurrentPower = Mathf.Max(CurrentPower - 1, 0);
                AudioManager.StreamAudio("unpower_item");
                UIManager.SetPower(CurrentPower, CurrentMaxPower);
                RefreshInventoryUI();
                return;
            }

            // Capacity check before powering on
            if (CurrentPower >= CurrentMaxPower) {
                AudioManager.StreamAudio("error");
                return;
            }

            item.PowerOn();
            _poweredItems.Add(item);
            CurrentPower++;
            AudioManager.StreamAudio("power_item");
            UIManager.SetPower(CurrentPower, CurrentMaxPower);
            RefreshInventoryUI();
            return;
        }

        // Explicit off request
        if (_poweredItems.Contains(item)) {
            item.PowerOff();
            _poweredItems.Remove(item);
            CurrentPower = Mathf.Max(CurrentPower - 1, 0);
            AudioManager.StreamAudio("unpower_item");
            UIManager.SetPower(CurrentPower, CurrentMaxPower);
            RefreshInventoryUI();
        }
    }

    #endregion

    #region Items

    [ExportGroup("Item Management")]
    [Export] public float PickupRadius { get; private set; } = 16f;
    [Export] public float ScanInterval { get; private set; } = 0.5f;
    private double _scanTimer = 0d;
    public bool IsScanReady => _scanTimer <= 0d;
    [Export] public int MaxSlots { get; private set; } = 5;
    public int CurrentMaxSlots { get; set; } = 1;
    public List<UpgradeItem> Items { get; private set; } = [];
    [Export] public float DropDistance { get; private set; } = 32f;


    public void ScanAndPickup() {
        if (!IsScanReady) return;
        if (Character == null) return;
        bool isUnit = Character.Tags != null && Character.Tags.Contains("Unit");
        if (!isUnit) return;
        _scanTimer = ScanInterval;

        foreach (PhysicsBody2D body in EntityManager.Entities) {
            // Skip invalid bodies
            if (body == null || !IsInstanceValid(body)) continue;
            if (body.IsQueuedForDeletion()) continue;

            // Get body position safely
            Vector2 bodyPos;
            try { bodyPos = body.GlobalPosition; } catch { continue; }

            // Skip bodies outside pickup radius
            float distance = Character.GlobalPosition.DistanceTo(bodyPos);
            if (distance > PickupRadius) continue;

            // Skip self
            if (body == Character) continue;

            // Skip this character's children
            List<Node2D> children = [.. Character.GetChildren(true).OfType<Node2D>()];
            if (children.Contains(body)) continue;

            // Skip non-StandardItem bodies
            if (body is not StandardItem standardItem) continue;

            // Process only world items
            if (standardItem.EntityType != StandardItem.EntityTypes.World) continue;

            //Skip if inventory is full
            if (Items.Count >= CurrentMaxSlots) continue;

            // Pickup powerable items
            if (standardItem is UpgradeItem upgrade) {
                if (Items.Contains(upgrade)) continue;
                Pickup(upgrade);
            }
            
            // Use single-use items immediately
            else {
                standardItem.Use();
                standardItem.QueueFree();
            }

            break;
        }
    }


    public void Pickup(UpgradeItem item) {
        if (item == null) return;
        item.EntityType = StandardItem.EntityTypes.Inventory;
        item.PickUp();
        AddItem(item);
    }

    public void AddItem(UpgradeItem item) {
        if (item == null) return;
        if (Items.Count >= CurrentMaxSlots) {
            AudioManager.StreamAudio("error");
            return;
        }

        item.SetOwner(Character);
        item.Reparent(Character);
        Items.Add(item);
        RefreshInventoryUI();
    }


    public async void RemoveItem(UpgradeItem item) {
        if (item == null) return;
        if (_poweredItems.Contains(item)) {
            item.PowerOff();
            _poweredItems.Remove(item);
            CurrentPower = Mathf.Max(CurrentPower - 1, 0);
            AudioManager.StreamAudio("unpower_item");
        }

        Vector2 dropVector = Character.Control.FacingDirection * DropDistance;
        Vector2 offset = dropVector.Normalized() * 32f;

        // Do not drop if there is anything blocking the drop position
        Vector2 dropPosition = Character.GlobalPosition + dropVector;
        PhysicsRayQueryParameters2D rayParams = PhysicsRayQueryParameters2D.Create(Character.GlobalPosition, dropPosition);
        rayParams.Exclude = [Character.GetRid()];

        //Wait for physics process notification
        await ToSignal(GetTree(), "physics_frame");

        // Get space state and perform raycast
        PhysicsDirectSpaceState2D spaceState = GetViewport().World2D.DirectSpaceState;
        var result = spaceState.IntersectRay(rayParams);
        if (result.Count > 0) {
            AudioManager.StreamAudio("error");
            return;
        }

        Items.Remove(item);
        item.SetOwner(null);
        item.Reparent(SceneLoader.Instance.LoadedScene!);
        item.SpawnInWorld();

        // Move item 32 pixels in front of where the character is facing
        item.GlobalPosition = Character.GlobalPosition + offset;

        RefreshInventoryUI();
    }

    #endregion

    #region Character

    [ExportGroup("Character")]
    public StandardCharacter Character { get; set; } = null!;

    #endregion

    #endregion

    public override void _Ready() {
        _cooldownTimer = 0d;
        _scanTimer = 0d;
    }

    public override void _EnterTree() {
        Instance = this;
    }

    public override void _Process(double delta) {
        if (IsOnCooldown) {
            _cooldownTimer -= delta;
            if (_cooldownTimer < 0d) _cooldownTimer = 0d;
        }
        if (!IsScanReady) {
            _scanTimer -= delta;
            if (_scanTimer < 0d) _scanTimer = 0d;
        }
        ScanAndPickup();
    }


    public void PowerOffAll() {
        foreach (UpgradeItem item in _poweredItems) {
            item.PowerOff();
        }
        _poweredItems.Clear();
        CurrentPower = 0;
    UIManager.SetPower(CurrentPower, CurrentMaxPower);
    RefreshInventoryUI();
    }


    public void OnItemUsed(UpgradeItem item) {
        if (item == null) return;

        // Immediately remove the item if it's used up
        if (item.UseCount == 0) {
            RemoveItem(item);
        }
    }


    public void OnCharacterChanged() {
        // Update references or perform actions needed when the character changes
    }

    #region Inventory UI

    private Texture2D? GetItemTexture(UpgradeItem item) {
        if (item == null) return null;
        // Try to find a Sprite2D within item for texture extraction
        Sprite2D? sprite = item.GetChildren().OfType<Sprite2D>().FirstOrDefault();
        if (sprite == null || sprite.Texture == null) return null;
        if (sprite.RegionEnabled) {
            AtlasTexture atlas = new() { Atlas = sprite.Texture, Region = sprite.RegionRect };
            return atlas;
        }
        return sprite.Texture;
    }

    public void RefreshInventoryUI() {
        // Update slot count (open slots is CurrentMaxSlots - Items.Count)
        UIManager.SetOpenSlots(CurrentMaxSlots);
        // Fill each slot with item or null
        for (int i = 0; i < CurrentMaxSlots; i++) {
            if (i < Items.Count) {
                UpgradeItem item = Items[i];
                Texture2D? texture = GetItemTexture(item);
                if (texture != null) UIManager.SetItemIcon(i, texture);
                else UIManager.SetItemIcon(i, (Texture2D)null!); // clear if no texture
                float alpha = IsPowered(item) ? 1f : UIManager.Instance.UnpoweredAlpha;
                UIManager.SetItemAlpha(i, alpha);
            }
            else {
                UIManager.SetItemIcon(i, (Texture2D)null!);
                UIManager.SetItemAlpha(i, 1f);
            }
        }
        // Optionally highlight powered items by re-setting icon (could extend UI for highlight later)
    }

    #endregion
}
