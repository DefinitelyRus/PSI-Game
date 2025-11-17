using System.Collections.Generic;
using CommonScripts;
using Godot;
namespace Game;

public partial class UpgradeManager : Node2D {

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
    [Export] public int MaxSlots { get; private set; } = 5;
    public int CurrentMaxSlots { get; private set; } = 2;
    public List<StandardItem> Items { get; private set; } = [];

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


}
