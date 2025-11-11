using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
namespace CommonScripts;

public partial class Level : Node2D {

	[Export] public Node2D SpawnParent = null!;
	[Export] public Node2D[] CameraNodePaths = [];
	private int CurrentSpawnIndex = 0;
	[Export] public Node2D PropsParent = null!;
	[Export] public Node2D RegionsParent = null!;


	public void SpawnUnit(StandardCharacter unit) {
		Node2D[] SpawnPoints = [.. SpawnParent.GetChildren().OfType<Node2D>()];

		if (SpawnPoints.Length == 0) {
			Log.Err("No spawn points defined for this level. Canceling.");
			return;
		}

		foreach (Node2D sp in SpawnPoints) {
			if (sp == null) {
				Log.Warn(() => "One of the spawn points is null. Please check the level setup.", true, true);
			}
		}

	AttemptSpawn:
		try {
			// Rid mapRid = NavRegion.GetNavigationMap();
			// unit.AIManager.NavAgent.SetNavigationMap(mapRid);
			AddChild(unit);
			unit.GlobalPosition = SpawnPoints[CurrentSpawnIndex].GlobalPosition;
			CurrentSpawnIndex = (CurrentSpawnIndex + 1) % SpawnPoints.Length;
		}

		catch (IndexOutOfRangeException) {
			Log.Warn(() => $"There are not enough spawn points ({SpawnPoints.Length}) to spawn unit at index {CurrentSpawnIndex}. Resetting spawn index to 0 and retrying.", true, true);
			CurrentSpawnIndex = 0;
			goto AttemptSpawn;
		}

		catch (NullReferenceException) {
			Log.Err(() => $"Spawn point at index {CurrentSpawnIndex} is null. Canceling.", true, true);
			return;
		}
	}

	private void ReparentAllProps() {
		IEnumerable<NavigationRegion2D> regions = RegionsParent.GetChildren().OfType<NavigationRegion2D>();
		IEnumerable<Node2D> categories = PropsParent.GetChildren().OfType<Node2D>();
		IEnumerable<StandardProp> props = [];

		// List all props from all categories
		foreach (Node2D category in categories) {
			IEnumerable<StandardProp> categoryProps = category.GetChildren().OfType<StandardProp>();
			props = props.Concat(categoryProps);
			category.QueueFree();
		}

		PropsParent.QueueFree();

		// Reparent each prop to the correct navigation region
		// and set its nav map to the region's map rid
		foreach (StandardProp prop in props) {
			//Log.Me(() => $"Reparenting prop {prop.Name} for navigation.");
			Rid? mapRid = GetMapRid(prop.GlobalPosition, out NavigationRegion2D? newRegion);

			if (mapRid == null || newRegion == null) {
				//Log.Warn(() => $"No navigation map found for prop {prop.Name} at position {prop.GlobalPosition}. Skipping setting nav map.");
				continue;
			}

			Log.Me(() => $"Setting navigation map for prop {prop.Name} to region {newRegion.Name}.");
			prop.NavObstacle.SetNavigationMap(mapRid.Value);
			prop.Reparent(newRegion, true);
		}

		// Bake navigation polygons for all regions
		foreach (NavigationRegion2D region in regions) {
			Log.Me(() => $"Baking navigation polygon for region {region.Name}.");
			region.BakeNavigationPolygon(true);
		}
	}
	
	private Rid? GetMapRid(Vector2 position, out NavigationRegion2D? newRegion) {
		List<NavigationRegion2D> regions = [.. RegionsParent.GetChildren().OfType<NavigationRegion2D>()];
		foreach (NavigationRegion2D region in regions) {
			if (region.GetBounds().HasPoint(position)) {
				newRegion = region;
				return region.GetNavigationMap();
			}
		}
		newRegion = null;
		return null;
	}


	#region Godot Callbacks

	public override void _EnterTree() {
		if (RegionsParent == null) {
			Log.Err(() => "RegionsParent is not assigned. Please assign a Node2D to hold regions in the level.");
			return;
		}

		if (PropsParent == null) {
			Log.Err(() => "PropsParent is not assigned. Please assign a Node2D to hold props in the level.");
			return;
		}
	}


	public override void _Ready() {
		// Set node paths for CameraMan
		CameraMan.SetCameraPath(CameraNodePaths);

		// If no node paths set, use spawn point.
		if (CameraNodePaths.Length == 0) {
			CameraMan.SetTarget(SpawnParent, true);
		}

		ReparentAllProps();
	}
	
	#endregion
}
