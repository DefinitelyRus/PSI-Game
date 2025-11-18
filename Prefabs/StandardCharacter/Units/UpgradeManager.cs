using System.Collections.Generic;
using System.Linq;
using CommonScripts;
using Godot;
namespace Game;

public partial class UpgradeManager : Node {

    #region Properties

    #region Power

    [ExportGroup("Power Management")]
    [Export] public int MaxPower { get; private set; } = 2;
    public int CurrentMaxPower { get; private set; } = 1;
    public int CurrentPower { get; private set; } = 0;

    [Export] public float PowerOnCooldown { get; private set; } = 0.5f;
    private double _cooldownTimer = 0d;
    public bool IsOnCooldown => _cooldownTimer > 0d;


    public int[] GetPoweredItems() {
        List<int> indices = [];

        for (int i = 0; i < Items.Count; i++) {
            if (Items[i].IsEquipped) {
                indices.Add(i);
            }
        }

        return [.. indices];
    }


    public int GetPoweredItemCount() {
        int count = 0;

        foreach (StandardItem item in Items) {
            if (item.IsEquipped) {
                count++;
            }
        }

        return count;
    }


    public bool IsItemPowered(int index) {
        if (index < 0 || index >= Items.Count) return false;

        return Items[index].IsEquipped;
    }


    public void SetItemPower(int index, bool powered) {
        if (index < 0 || index >= Items.Count) return;

        StandardItem item = Items[index];

        if (powered) {
            if (CurrentPower >= CurrentMaxPower) {
                AudioManager.PlaySFX("error");
                return;
            }

            if (!item.IsEquipped) {
                item.Equip();
                CurrentPower++;
                AudioManager.PlaySFX("power_item");
            }
        }
        
        else {
            if (item.IsEquipped) {
                item.Unequip();
                CurrentPower = Mathf.Max(CurrentPower - 1, 0);
                AudioManager.PlaySFX("unpower_item");
            }
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
    public int CurrentMaxSlots { get; private set; } = 2;
    public List<StandardItem> Items { get; private set; } = [];


    public void ScanAndPickup(double delta) {
        if (!IsScanReady) return;
        _scanTimer = ScanInterval;

        foreach (PhysicsBody2D body in EntityManager.Entities) {

            // Ignore out of range
            float distance = Character.GlobalPosition.DistanceTo(body.GlobalPosition);
            if (distance > PickupRadius) continue;

            // Ignore self
            if (body == Character) continue;

            // Ignore own children
            List<Node2D> children = [.. Character.GetChildren(true).OfType<Node2D>()];
            if (children.Contains(body)) continue;

            // Ignore non-items
            if (body is not StandardItem item) continue;

            // Ignore non-world items
            if (item.EntityType != StandardItem.EntityTypes.World) continue;

            Pickup(item);
            break;
        }
    }


    public void Pickup(StandardItem item) {
        item.PickUp();

        // Do not add if the item is single-use.
        if (item.UseCount == 0) AddItem(item);
    }


    public void AddItem(StandardItem item) {
        if (Items.Count >= CurrentMaxSlots) {
            AudioManager.PlaySFX("error");
            return;
        }

        Items.Add(item);
    }


    public void RemoveItem(int index) {
        if (index < 0 || index >= Items.Count) return;

        StandardItem item = Items[index];

        if (item.IsEquipped) {
            CurrentPower = Mathf.Max(CurrentPower - 1, 0);
            item.Unequip();
        }

        item.Drop();
        Items.RemoveAt(index);
    }

    #endregion

    #endregion
    
    #region Nodes & Components

    [ExportGroup("Nodes & Components")]
    [Export] public StandardCharacter Character { get; set; } = null!;

	#endregion

	#region Godot Callbacks

	public override void _Process(double delta) {
        // Cooldown Timer
        if (IsOnCooldown) {
            _cooldownTimer -= delta;
            if (_cooldownTimer < 0d) _cooldownTimer = 0d;
        }

        // Scan Timer
        if (!IsScanReady) {
            _scanTimer -= delta;
            if (_scanTimer < 0d) _scanTimer = 0d;
        }

        ScanAndPickup(delta);
    }

    #endregion
}
