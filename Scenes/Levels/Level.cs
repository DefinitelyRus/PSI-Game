using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
namespace CommonScripts;

public partial class Level : Node2D {

	[Export] public Node2D SpawnParent = null!;
	[Export] public Node2D EnemySpawnParent = null!;
	[Export] public Node2D[] CameraNodePaths = [];
	private int CurrentSpawnIndex = 0;
	[Export] public Node2D PropsParent = null!;
	[Export] public Node2D RegionsParent = null!;
	[Export] public AudioStream BackgroundMusic = null!;
	[Export] public float EnemyStaticSpawningDelayMultiplier = 1f;
	[Export] public uint LevelIndex = 0;


	#region Debug

	[ExportGroup("Debug")]
	[Export] public bool LogReady = true;

	#endregion


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


	#region Enemy Spawning

	[ExportGroup("Enemy Spawning")]
	[Export] public PackedScene[] EnemyTypes = [];


	public void SpawnCharacter(StandardCharacter character, Vector2 position) {
		AddChild(character);
		character.GlobalPosition = position;
	}


	public void SpawnCharacter(StandardCharacter character, Node2D spawnPoint) {
		SpawnCharacter(character, spawnPoint.GlobalPosition);
	}


	/// <summary>
	/// Finds the nearest spawn point to the given position within the specified radius.
	/// </summary>
	/// <param name="position">The position to find the nearest spawn point to.</param>
	/// <param name="radius">The distance within which to search for spawn points.</param>
	/// <param name="index">The nth closest spawn point to find.</param>
	/// <returns>A Node2D or null if no suitable spawn point is found within the radius.</returns>
	public Node2D? FindNearbyEnemySpawnPoint(Vector2 position, float radius = float.MaxValue, int index = 0, bool avoidCamera = true) {

		#region Validation

		if (index < 0) {
			Log.Err(() => $"FindNearbySpawnPoint: Index {index} cannot be negative. Canceling.");
			return null;
		}

		if (radius <= 0f) {
			Log.Err(() => $"FindNearbySpawnPoint: Radius {radius} must be greater than zero. Canceling.");
			return null;
		}

		Node2D[] EnemySpawns = [.. EnemySpawnParent.GetChildren(true).OfType<Node2D>()];
		if (EnemySpawns.Length == 0) {
			Log.Err("No spawn points defined for this level. Canceling.");
			return null;
		}

		if (index >= EnemySpawns.Length) {
			Log.Warn(() => $"FindNearbySpawnPoint: Index {index} exceeds available spawn points ({EnemySpawns.Length}). Setting random index within range.");
			index = new RandomNumberGenerator().RandiRange(0, EnemySpawns.Length - 1);
		}

		#endregion

		// Sort by distance to the given position
		float distance(Node2D spawn) => spawn.GlobalPosition.DistanceTo(position);
		List<Node2D> sortedSpawns = [.. EnemySpawns.OrderBy(distance)];

		// Remove spawns outside the radius
		bool inRadius(Node2D spawn) => distance(spawn) <= radius;
		List<Node2D> spawnsInRange = [.. sortedSpawns.Where(inRadius)];

		if (spawnsInRange.Count == 0) {
			Log.Warn(() => $"No spawn points found within radius {radius} of position {position}.", true, true);
			return null;
		}

		// Optionally remove spawns currently in camera view
		if (avoidCamera) {
			foreach (Node2D spawn in spawnsInRange.ToList()) {
				if (CameraMan.IsPointVisible(spawn.GlobalPosition)) {
					spawnsInRange.Remove(spawn);
				}
			}
		}

		return spawnsInRange.Count > index ? spawnsInRange[index] : null;
	}

	#endregion

	#region Navigation

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

		if (PropsParent.GetChildren().Count == 0) PropsParent.QueueFree();

		// Reparent each prop to the correct navigation region
		// and set its nav map to the region's map rid
		foreach (StandardProp prop in props) {
			//Log.Me(() => $"Reparenting prop {prop.Name} for navigation.");
			Rid? mapRid = GetMapRid(prop.NavObstacle.GlobalPosition, out NavigationRegion2D? newRegion);

			if (mapRid == null || newRegion == null) {
				Log.Me(() => $"No navigation region found for {prop.InstanceID} at {prop.GlobalPosition}. Skipping reparent.", LogReady);
				continue;
			}

			Log.Me(() => $"Setting navigation map for prop {prop.InstanceID} to region {newRegion.Name}.", LogReady);
			prop.NavObstacle.SetNavigationMap(mapRid.Value);
			prop.Reparent(newRegion, true);
		}

		// Bake navigation polygons for all regions
		foreach (NavigationRegion2D region in regions) {
			Log.Me(() => $"Baking navigation polygon for region {region.Name}.", LogReady);

			if (region.NavigationPolygon == null) {
				Log.Warn(() => $"Region {region.Name} has no NavigationPolygon assigned. Skipping bake.");
				continue;
			}

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

	#endregion

	#region Godot Callbacks

	public override void _EnterTree() {
		if (SpawnParent == null) {
			Log.Err(() => "SpawnParent is not assigned. Please assign a Node2D to hold spawn points in the level.");
			return;
		}

		if (EnemySpawnParent == null) {
			Log.Err(() => "EnemySpawnParent is not assigned. Please assign a Node2D to hold enemy spawn points in the level.");
			return;
		}

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
		if (CameraNodePaths.Length != 0) CameraMan.SetCameraPath(CameraNodePaths);
		else CameraMan.SetTarget(SpawnParent, true);

		ReparentAllProps();

		AIDirector.CurrentLevel = this;

		if (BackgroundMusic != null) AudioManager.PlayMusic(BackgroundMusic);
	}
	
	#endregion
}
