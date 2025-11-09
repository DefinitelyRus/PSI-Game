using System;
using System.Linq;
using Godot;
namespace CommonScripts;

public partial class Level : Node2D {

	[Export] public Node2D SpawnParent = null!;
	[Export] public Node2D[] CameraNodePaths = [];
	private int CurrentSpawnIndex = 0;
	[Export] public NavigationRegion2D NavRegion = null!;
	[Export] public Node2D PropsParent = null!;


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


	public override void _EnterTree() {
		if (NavRegion == null) {
			Log.Err(() => "NavRegion is not assigned. Please assign a NavigationRegion2D to the level.");
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
		
		foreach (Node2D child in PropsParent.GetChildren().OfType<Node2D>()) {
			foreach (Node2D grandChild in child.GetChildren().Cast<Node2D>()) {
				foreach (Node2D greatGrandChild in grandChild.GetChildren().Cast<Node2D>()) {
					if (greatGrandChild is StandardProp prop) {
						Rid mapRid = NavRegion.GetNavigationMap();
						Log.Me(() => $"Setting nav map for prop {prop.InstanceID} to {mapRid}...");
						prop.NavObstacle.SetNavigationMap(mapRid);
					}
				}
			}
		}

		foreach (StandardCharacter character in GetChildren(true).OfType<StandardCharacter>()) {
			Rid mapRid = NavRegion.GetNavigationMap();
			Log.Me(() => $"Setting nav map for character {character.InstanceID} to {mapRid}...", true, true);
			character.AIManager.NavAgent.SetNavigationMap(mapRid);
		}
	}
}
