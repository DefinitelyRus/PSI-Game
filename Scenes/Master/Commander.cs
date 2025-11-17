using Godot;
using System.Collections.Generic;
using System.Linq;

namespace CommonScripts;

public partial class Commander : Node {

	#region Instance Members

	[Export] public PackedScene[] InitialUnits = [];

	#region Debugging

	[Export] public bool LogReady = true;
	[Export] public bool LogInput = false;

	#endregion

	#region Godot Callbacks

	public override void _Ready() {
		if (Instance != null) {
			Log.Err("Multiple instances of Commander detected. There should only be one Commander in the scene.");
			QueueFree();
			return;
		}

		Instance = this;
		Initialize();
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
	}

	#endregion

	#region Macro Unit Control

	private static List<StandardCharacter> Units { get; set; } = [];


	public static void RegisterUnit(StandardCharacter unit)
	{
		if (Units.Contains(unit))
		{
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
		if (index < 0 || index >= Units.Count) {
			Log.Me(() => $"Cannot select unit at index {index} in an array of {Units.Count} units.");
			return;
		}

		Units[index].AIAgent.IsSelected = true;

		SingleUnitControl();
	}


	public static void SingleUnitControl() {
        if (Units.Count == 0) {
			Log.Warn("No units are registered. Cannot select.", true, true);
			return;
		}

		if (GetSelectedUnitCount() != 1) return;

		StandardCharacter unit = GetSelectedUnits().First();

		UIManager.SetHUDVisible(true, 0);
		UIManager.SetHealth(unit.Health, unit.CurrentMaxHealth);
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

		UIManager.SetHUDVisible(false, 0);
	}

	#endregion

	#region Micro Unit Control

	public static StandardCharacter FocusedUnit { get; private set; } = null!;
	public static List<StandardItem> UnitInventory => FocusedUnit.Inventory;
	public static StandardCharacter TargetedUnit { get; private set; } = null!;

	public static bool PrimeDrop { get; set; } = false;


	public static void SetFocusedUnit(int index, bool moveCamera = true)
	{
		if (index < 0 || index > Units.Count) {
			Log.Err(() => $"Unit at index {index} does not exist. Cannot focus.");
			return;
		}

		DeselectAllUnits();
		FocusedUnit = Units[index];
		SelectUnit(index);

		if (moveCamera) CameraMan.SetTarget(FocusedUnit);
	}


	public static void ClearFocusedUnit()
	{
		FocusedUnit = null!;

		CameraMan.ClearTarget();
	}


	public static void SelectItem(int itemIndex) {
		if (FocusedUnit == null) {
			Log.Me(() => "No focused unit set.", Instance.LogInput);
			return;
		}

		if (itemIndex < 0 || itemIndex >= FocusedUnit.Inventory.Count) {
			Log.Me(() => $"Item index {itemIndex} is out of range (0 to {FocusedUnit.Inventory.Count - 1}).", Instance.LogInput);
			return;
		}

		if (PrimeDrop) FocusedUnit.RemoveItemFromInventory(itemIndex, true, out var _);
		else FocusedUnit.ToggleEquipItem(itemIndex);
	}


	public static void MoveAndSearch(Vector2 mousePos) {
		foreach (StandardCharacter unit in GetSelectedUnits()) {
			AIAgentManager agent = unit.AIAgent;
			agent.Action1(mousePos);
			agent.Searching = true;
			agent.Targeting = false;
		}
	}


	public static void MoveAndTarget(Vector2 mousePos) {
		foreach (StandardCharacter unit in GetSelectedUnits()) {
			AIAgentManager agent = unit.AIAgent;
			AITargetingManager targeter = unit.TargetingManager;

			agent.Stop();
			agent.CurrentTarget = null;
			targeter.ClearTarget();

			agent.GoTo(mousePos);
			agent.Targeting = true;
			agent.Searching = false;

			bool pointingAtEntity = EntityManager.HasEntityAtPosition(mousePos, out var entity);
			
			if (!pointingAtEntity) {
				Log.Me(() => $"MoveAndTarget: No entity found at ({mousePos.X:F2}, {mousePos.Y:F2}). Moving only.", Instance.LogInput);
				continue;
			}

			if (entity is StandardCharacter targetUnit && targetUnit.IsAlive && targetUnit != unit) {
				agent.CurrentTarget = targetUnit;
				targeter.ManualTarget = targetUnit;
				targeter.CurrentTarget = targetUnit;
				
				targeter.SetAimDirection();

				Log.Me(() => $"MoveAndTarget: {unit.InstanceID} targeting unit {targetUnit.InstanceID} at ({mousePos.X:F2}, {mousePos.Y:F2}).", Instance.LogInput);
			}
			
			else {
				Log.Me(() => $"MoveAndTarget: Entity at ({mousePos.X:F2}, {mousePos.Y:F2}) not a valid StandardCharacter target. Moving only.", Instance.LogInput);
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
