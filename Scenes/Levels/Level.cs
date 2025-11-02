using System;
using Godot;
namespace CommonScripts;

public partial class Level : Node2D
{
	[Export] public Node2D[] SpawnPoints = [];
	private int CurrentSpawnIndex = 0;


	public void SpawnUnit(StandardCharacter unit) {
		AddChild(unit);

		if (SpawnPoints.Length == 0) {
			Log.Err("No spawn points defined for this level. Canceling.");
			return;
		}

	AttemptSpawn:
		try {
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
}
