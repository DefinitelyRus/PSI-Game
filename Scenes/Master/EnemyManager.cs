using System.Collections.Generic;
using System.Linq;
using Godot;
namespace CommonScripts;

public partial class EnemyManager : Node2D {

    #region Instance Members

    [Export] public int SpawnpointSelectionCount = 10;

    #region Godot Callbacks

    public override void _Ready() {
        if (Instance != null) {
            Log.Err("Multiple instances of EnemyManager detected. There should only be one EnemyManager in the scene.");
            QueueFree();
            return;
        }

        Instance = this;
    }

    #endregion

    #endregion

    #region Static Members

    public static EnemyManager Instance { get; private set; } = null!;
    
    private static Level CurrentLevel => AIDirector.CurrentLevel;

    /// <summary>
    /// Enemies that have been spawned in on the game world.
    /// </summary>
    public static List<StandardCharacter> Enemies { get; private set; } = [];

    private static List<StandardCharacter> _playerUnits => [.. Commander.GetAllUnits()];

    #region Spawn Management

    public static bool TrySpawnEnemy(StandardCharacter enemy) {
        if (_playerUnits.Count == 0) {
            Log.Warn(() => "No player units registered in EnemyManager. Cannot select spawn points based on player unit positions.");
            return false;
        }

        if (CurrentLevel == null) {
            Log.Err(() => "No current level registered in EnemyManager. Cannot spawn enemies.");
            return false;
        }

        RandomNumberGenerator rng = new();
        int targetUnit = rng.RandiRange(0, _playerUnits.Count - 1);
        int spawnIndex = rng.RandiRange(0, Instance.SpawnpointSelectionCount - 1);

        Node2D? spawnPoint = CurrentLevel.FindNearbyEnemySpawnPoint(
            _playerUnits[targetUnit].GlobalPosition,
            float.MaxValue,
            spawnIndex,
            true
        );

        if (spawnPoint != null) {
            CurrentLevel.SpawnCharacter(enemy, spawnPoint);
			Enemies.Add(enemy);
			return true;
        }

        else return false;
    }

    #endregion

    #endregion

}