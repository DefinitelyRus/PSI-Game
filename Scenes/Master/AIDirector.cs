using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
namespace CommonScripts;

public partial class AIDirector : Node2D {

    #region Instance Members

    [Export] public PackedScene[] EnemyTypes = null!;

    [Export] public SpawnMode Mode = SpawnMode.Timed;

	#region Godot Callbacks

	public override void _EnterTree() {
        if (Instance != null) {
            Log.Err("Multiple instances of AIDirector detected. There should only be one AIDirector in the scene.");
            QueueFree();
            return;
        }

        Instance = this;

        if (EnemyTypes.Length == 0) {
            Log.Warn(() => "No enemy types assigned to AIDirector. No enemies will be spawned.");
        }

        foreach (PackedScene enemyType in EnemyTypes) {
            if (enemyType == null) {
                Log.Warn(() => "One of the enemy types assigned is null. Please check the setup.");
                return;
            }

            Node instance = enemyType.Instantiate();
            if (instance is not StandardEnemy) {
                Log.Warn(() => $"One of the enemy types assigned is not a StandardEnemy. Please check the setup.");
                return;
            }
        }
	}

    public override void _Ready() {
    }

    public override void _Process(double delta) {
        switch (Mode) {
            case SpawnMode.Timed:
                UpdateTimedSpawning(delta);
                break;
            case SpawnMode.Dynamic:
                break;
        }
    }

    #endregion

    #endregion

    #region Static Members

    public static AIDirector Instance { get; private set; } = null!;

    private static Level CurrentLevel = null!;

    public enum SpawnMode {
        Timed,
        Dynamic
    }

    public static void StartLevel(Level level) {
        CurrentLevel = level;
    }

    #region Timed Spawning

    private static double _timeDelayRemaining = 0d;

    /// <summary>
    /// Spawns enemies at a set interval
    /// </summary>
    /// <param name="delta"></param>
    public static void UpdateTimedSpawning(double delta) {
        // Spawn every X seconds to a random spawn point near a random player unit.
        // Add a delay after spawning each enemy, based on the enemy's DelayAfterSpawn property.

        if (_timeDelayRemaining > 0d) {
            _timeDelayRemaining -= delta;
            return;
        }

        if (CurrentLevel == null) return;

        RandomNumberGenerator rng = new();
        List<StandardCharacter> units = [.. Commander.GetAllUnits()];

        int targetUnitIndex = rng.RandiRange(0, units.Count - 1);
        int enemyTypeIndex = rng.RandiRange(0, Instance.EnemyTypes.Length - 1);

        Vector2 targetUnitPosition = units[targetUnitIndex].GlobalPosition;
        PackedScene enemyScene = Instance.EnemyTypes[enemyTypeIndex];
        StandardEnemy enemy = enemyScene.Instantiate<StandardEnemy>();

        bool wasSpawned = EnemyManager.TrySpawnEnemy(enemy);

        if (!wasSpawned) {
            enemy.QueueFree();
            return;
        }

        _timeDelayRemaining = enemy.DelayAfterSpawn;
        enemy.AIManager.GoTo(targetUnitPosition);
    }

    #endregion

    #endregion

}