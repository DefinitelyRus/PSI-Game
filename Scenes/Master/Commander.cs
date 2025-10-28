using CommonScripts;
using Godot;
using System.Collections.Generic;

namespace CommonScripts;

public partial class Commander : Node
{
	#region Macro Unit Control

	private static List<StandardCharacter> Units { get; set; } = [];


	public static void RegisterUnit(StandardCharacter unit)
	{
		if (!Units.Contains(unit))
		{
			Units.Add(unit);
		}
	}


	public static void UnregisterUnit(StandardCharacter unit)
	{
		if (Units.Contains(unit))
		{
			Units.Remove(unit);
		}
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
			Log.Err(() => $"SelectUnit: Index {index} is out of range (0 to {Units.Count - 1}).");
			return;
		}

		Units[index].AIManager.IsSelected = true;
	}


	public static void SelectAllUnits()
	{
		foreach (StandardCharacter unit in Units)
		{
			unit.AIManager.IsSelected = true;
		}
	}


	public static void DeselectAllUnits()
	{
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
			Log.Err(() => "SelectItem: No focused unit set.");
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
