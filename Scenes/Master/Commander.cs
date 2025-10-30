using Godot;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CommonScripts;

public partial class Commander : Node
{

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
			if (unit.AIManager.IsSelected)
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
			if (unit.AIManager.IsSelected)
			{
				count++;
			}
		}
		return count;
	}


	public static void SelectUnit(int index)
	{
		if (index < 0 || index > Units.Count) {
			Log.Me(() => $"Cannot select unit at index {index} in an array of {Units.Count} units.");
			return;
		}

		Units[index].AIManager.IsSelected = true;
	}


	public static void SelectAllUnits()
	{
		if (Units.Count == 0)
		{
			Log.Warn("No units are registered. Cannot select.", true, true);
			return;
		}

		foreach (StandardCharacter unit in Units)
		{
			unit.AIManager.IsSelected = true;
		}
	}


	public static void DeselectAllUnits()
	{
		if (Units.Count == 0)
		{
			Log.Warn("No units are registered. Cannot deselect.", true, true);
			return;
		}
		
		foreach (StandardCharacter unit in Units)
		{
			unit.AIManager.IsSelected = false;
		}
	}

	#endregion

	#region Micro Unit Control

	public static StandardCharacter FocusedUnit { get; private set; } = null!;
	//public static List<StandardItem> UnitInventory => FocusedUnit.Inventory;

	public static void SetFocusedUnit(int index)
	{
		if (index < 0 || index > Units.Count) {
			Log.Err(() => $"SetFocusedUnit: Index {index} is out of range (0 to {Units.Count - 1}).");
			return;
		}

		FocusedUnit = Units[index];
	}

	public static void ClearFocusedUnit()
	{
		FocusedUnit = null!;
	}

	public static void SelectItem(int itemIndex)
	{
		if (FocusedUnit == null) {
			Log.Me(() => "SelectItem: No focused unit set.", Instance.LogInput);
			return;
		}

		if (itemIndex < 0 || itemIndex >= FocusedUnit.Inventory.Count) {
			Log.Err(() => $"SelectItem: Item index {itemIndex} is out of range (0 to {FocusedUnit.Inventory.Count - 1}).");
			return;
		}

		//FocusedUnit.SelectItem(itemIndex);
	}

	#endregion
}
