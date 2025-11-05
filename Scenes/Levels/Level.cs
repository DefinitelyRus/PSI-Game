using System;
using Godot;
namespace CommonScripts;

public partial class Level : Node2D {

	[Export] public Node2D SpawnParent = null!;
	[Export] public Node2D[] CameraNodePaths = [];
	private int CurrentSpawnIndex = 0;


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


	public override void _Ready() {
		// Set node paths for CameraMan
		CameraMan.SetCameraPath(CameraNodePaths);

		// If no node paths set, use spawn point.
		if (CameraNodePaths.Length == 0) {
			CameraMan.SetTarget(SpawnParent, true);
		}
	}
}
