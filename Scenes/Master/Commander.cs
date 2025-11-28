using Game;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace CommonScripts;

public partial class Commander : Node {

	#region Instance Members

	[Export] public PackedScene[] InitialUnits = [];
	[Export] public PackedScene IndicatorNodeScene = null!;

	#region Debugging

	[Export] public bool LogReady = true;
	[Export] public bool LogInput = false;

	#endregion

	#region Godot Callbacks

	public override void _EnterTree() {
		Log.Me(() => "Commander _EnterTree called.", true, true);
		if (Instance != null) {
			Log.Err("Multiple instances of Commander detected. There should only be one Commander in the scene.");
			QueueFree();
			return;
		}

		if (InitialUnits == null) {
			Log.Err("InitialUnits is null in Commander. Please assign initial unit scenes in the inspector.");
			InitialUnits = [];
		}

		if (IndicatorNodeScene == null) {
			Log.Err("IndicatorNodeScene is null in Commander. Please assign an indicator node scene in the inspector.");
		}
	}

	public override void _Ready() {
		if (Instance != null) {
			Log.Err("Multiple instances of Commander detected. There should only be one Commander in the scene.");
			QueueFree();
			return;
		}

		Instance = this;
		Initialize();
	}

	public override void _Process(double delta) {
		if (GetSelectedUnits().Count() == 1) {
			StandardCharacter character = GetSelectedUnits().First();
			UIManager.SetHealth(character.Health, character.CurrentMaxHealth);
		}
	}

	#endregion

	#endregion

	#region Static Members

	#region Initialization

	public static Commander Instance { get; private set; } = null!;

	public static void Initialize()
	{
		foreach (PackedScene unitScene in Instance.InitialUnits)
		{
			StandardCharacter unit = unitScene.Instantiate<StandardCharacter>();
			RegisterUnit(unit);
		}

		UIManager.SetPower(1, 1);
	}

	#endregion

	#region Macro Unit Control

	private static List<StandardCharacter> Units { get; set; } = [];


	public static void RegisterUnit(StandardCharacter unit)
	{
		if (!unit.IsAlive) return;
		if (Units.Contains(unit)) {
			Log.Warn("This unit has already been added to the list. Cannot add.", true, true);
			return;
		}
		
		Units.Add(unit);
	}


	public static void UnregisterUnit(StandardCharacter unit)
	{
		bool wasRemoved = Units.Remove(unit);
		if (!wasRemoved) Log.Warn(() => $"Unit {unit.InstanceID} does not exist in the unit list. Cannot remove.", true, true);
    }


	public static void ClearUnits()
	{
		Units.Clear();
	}

	public static void PurgeDeadUnits()
	{
		for (int i = Units.Count - 1; i >= 0; i--) {
			StandardCharacter u = Units[i];
			if (!u.IsAlive) Units.RemoveAt(i);
		}
	}


	public static IEnumerable<StandardCharacter> GetAllUnits()
	{
		return Units;
	}


	public static IEnumerable<StandardCharacter> GetSelectedUnits()
	{
		foreach (StandardCharacter unit in Units)
		{
			if (unit.AIAgent.IsSelected)
			{
				yield return unit;
			}
		}
	}


	public static int GetSelectedUnitCount()
	{
		int count = 0;
		foreach (StandardCharacter unit in Units)
		{
			if (unit.AIAgent.IsSelected)
			{
				count++;
			}
		}
		return count;
	}


	public static void SelectUnit(int index)
	{
		if (index < 0 || index >= Units.Count) return;

		// Select exactly one unit
		for (int i = 0; i < Units.Count; i++) {
			Units[i].AIAgent.IsSelected = i == index;
		}

		UIManager.SetCharacterName("");
		SingleUnitControl();
	}


	public static void SingleUnitControl() {
        if (Units.Count == 0) {
			Log.Warn("No units are registered. Cannot select.", true, true);
			return;
		}

		if (GetSelectedUnitCount() != 1) {
			UIManager.SetSelectedCharacter(null);
			UIManager.SetHUDVisible(false, 0);
			return;
		}

		StandardCharacter unit = GetSelectedUnits().First();
		// Ensure HUD is bound to this character
		UIManager.SetSelectedCharacter(unit);

		// Check which unit is selected by name
		string unitName = unit.CharacterID;
		UIManager.SetHealthColor(unitName);
		UIManager.SetCharacterName(unitName);
		UIManager.SetHUDVisible(true, 0);
		UIManager.SetHealth(unit.Health, unit.CurrentMaxHealth);
		// Refresh inventory + power fully for selected character
		unit.UpgradeManager.RefreshAll();

		// Update inventory UI
		UpgradeManager upMan = unit.UpgradeManager;
		UpgradeItem[] items = [.. upMan.Items];
		int[] poweredItems = [.. upMan.GetPoweredItems()];
		for (int i = 0; i < upMan.CurrentMaxSlots; i++) {
			UpgradeItem? item = i < items.Length ? items[i] : null;
            Sprite2D? itemIcon = item?.Sprite;
			itemIcon ??= null;

            UIManager.SetItemIcon(i, itemIcon);

			if (item == null) continue;
			UIManager.SetItemAlpha(i, poweredItems.Contains(i) ? 1.0f : 0.5f);
        }
	}


	public static void SelectAllUnits()
	{
		if (Units.Count == 0)
		{
			Log.Warn("No units are registered. Cannot select.", true, true);
			return;
		}

		foreach (StandardCharacter unit in Units) {
			unit.AIAgent.IsSelected = true;
		}

		ClearFocusedUnit();

		UIManager.SetSelectedCharacter(null);
		UIManager.SetCharacterName("BOTH");
		UIManager.SetHUDVisible(false, 0);
		UIManager.SetTimerEnabled(true);
	}


	public static void DeselectAllUnits()
	{
		if (Units.Count == 0) {
			Log.Warn("No units are registered. Cannot deselect.", true, true);
			return;
		}
		
		foreach (StandardCharacter unit in Units)
		{
			unit.AIAgent.IsSelected = false;
		}

		ClearFocusedUnit();

		UIManager.SetCharacterName("");
		UIManager.SetSelectedCharacter(null);
		UIManager.SetHUDVisible(false, 0);
		UIManager.SetTimerEnabled(true);
	}

	#endregion

	#region Micro Unit Control

	public static StandardCharacter FocusedUnit { get; private set; } = null!;
	public static List<StandardItem> UnitInventory => FocusedUnit.Inventory;
	public static StandardCharacter TargetedUnit { get; private set; } = null!;

	public static bool PrimeDrop { get; set; } = false;


	public static void SetFocusedUnit(int index, bool moveCamera = true)
	{
		if (index < 0 || index >= Units.Count) return;

		if (CameraMan.IsPathActive) CameraMan.FinishPathInstantly(skipFocus: true);
		DeselectAllUnits();
		
		FocusedUnit = Units[index];
		SelectUnit(index);

		if (moveCamera) CameraMan.SetTarget(FocusedUnit);
	}


	public static void ClearFocusedUnit()
	{
		FocusedUnit = null!;
		if (!CameraMan.IsPathActive) CameraMan.ClearTarget();
	}


	public static void SelectItem(int itemIndex) {
		if (GetSelectedUnitCount() != 1) return;
		StandardCharacter unit = GetSelectedUnits().First();
		UpgradeManager upMan = unit.UpgradeManager;

		if (itemIndex < 0 || itemIndex >= upMan.Items.Count) {
			Log.Me(() => $"Item index {itemIndex} is out of range (0 to {upMan.Items.Count - 1}).", Instance.LogInput);
			return;
		}

		UpgradeItem item = upMan.Items[itemIndex];
		if (PrimeDrop) upMan.RemoveItem(item);
		else unit.ToggleEquipItem(itemIndex);
	}


	public static async void MoveAndSearch(Vector2 mousePos) {
		foreach (StandardCharacter unit in GetSelectedUnits()) {
			Vector2 position = mousePos;
			
			// Randomize position slightly if more than one unit is selected
			if (GetSelectedUnits().Count() > 1) {
				RandomNumberGenerator rng = new();
				Vector2 offset = new(rng.RandfRange(-8f, 8f), rng.RandfRange(-8f, 8f));
				mousePos = new(mousePos.X + offset.X, mousePos.Y + offset.Y);
			}

			AIAgentManager agent = unit.AIAgent;
			agent.Action1(mousePos);
			agent.Searching = true;
			agent.Targeting = false;

			await Instance.ToSignal(Instance.GetTree().CreateTimer(0.15f), "timeout");
		}
	}


	public static async void MoveAndTarget(Vector2 mousePos) {
		foreach (StandardCharacter unit in GetSelectedUnits()) {
			Vector2 position = mousePos;

			// Randomize position slightly if more than one unit is selected
			if (GetSelectedUnits().Count() > 1) {
				RandomNumberGenerator rng = new();
				Vector2 offset = new(rng.RandfRange(-8f, 8f), rng.RandfRange(-8f, 8f));
				mousePos = new(mousePos.X + offset.X, mousePos.Y + offset.Y);
			}

			AIAgentManager agent = unit.AIAgent;
			AITargetingManager targeter = unit.TargetingManager;

			agent.Stop();
			agent.CurrentTarget = null;
			targeter.ClearTarget();

			agent.GoTo(position);
			agent.Targeting = true;
			agent.Searching = false;

			bool pointingAtEntity = EntityManager.HasEntityAtPosition(position, out var entity);
			
			await Instance.ToSignal(Instance.GetTree().CreateTimer(0.15f), "timeout");

			if (!pointingAtEntity) {
				Log.Me(() => $"MoveAndTarget: No entity found at ({position.X:F2}, {position.Y:F2}). Moving only.", Instance.LogInput);
				continue;
			}

			if (entity is StandardCharacter targetUnit && targetUnit.IsAlive && targetUnit != unit) {
				agent.CurrentTarget = targetUnit;
				targeter.ManualTarget = targetUnit;
				targeter.CurrentTarget = targetUnit;
				
				targeter.SetAimDirection();

				Log.Me(() => $"MoveAndTarget: {unit.InstanceID} targeting unit {targetUnit.InstanceID} at ({position.X:F2}, {position.Y:F2}).", Instance.LogInput);
			}
			
			else {
				Log.Me(() => $"MoveAndTarget: Entity at ({position.X:F2}, {position.Y:F2}) not a valid StandardCharacter target. Moving only.", Instance.LogInput);
			}
		}
	}


	public static void StopSelectedUnits() {
		foreach (StandardCharacter unit in GetSelectedUnits()) {
			AIAgentManager agent = unit.AIAgent;
			agent.Stop();
			agent.Targeting = false;
			agent.Searching = false;
		}
    }

	#endregion

	#endregion

}
