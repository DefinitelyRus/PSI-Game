using System.Collections.Generic;
using System.Linq;
using Godot;
namespace CommonScripts;

public partial class EnemyManager : Node2D {

    #region Instance Members

    [Export] public int SpawnpointSelectionCount = 4;
    [Export] public int SpawnpointSelectionRange = 512;

    #region Godot Callbacks

    public override void _Ready() {
        if (Instance != null)
        {
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
    
    private static Level CurrentLevel = null!;

    /// <summary>
    /// Enemies that have been spawned in on the game world.
    /// </summary>
    public static List<StandardCharacter> Enemies { get; private set; } = [];

    private static List<StandardCharacter> PlayerUnits = [];

    #region Spawn Management

    public static bool TrySpawnEnemy(StandardCharacter enemy) {
        Enemies.Add(enemy);

        if (PlayerUnits.Count == 0) {
            Log.Warn(() => "No player units registered in EnemyManager. Cannot select spawn points based on player unit positions.");
            return false;
        }

        RandomNumberGenerator rng = new();
        int targetUnit = rng.RandiRange(0, PlayerUnits.Count - 1);
        int spawnIndex = rng.RandiRange(0, Instance.SpawnpointSelectionCount - 1);

        Node2D? spawnPoint = CurrentLevel.FindNearbySpawnPoint(
            PlayerUnits[targetUnit].GlobalPosition,
            float.MaxValue,
            spawnIndex,
            true
        );

        if (spawnPoint != null) {
            CurrentLevel.SpawnEnemy(enemy, spawnPoint);
            return true;
        }

        else return false;
    }

    public static bool KillEnemy(StandardCharacter enemy) {
        enemy.Kill();
        bool wasRemoved = Enemies.Remove(enemy);
        return wasRemoved;
    }

    public static void InitializeLevel(Level level) {
        CurrentLevel = level;

        Enemies.Clear();

        PlayerUnits = [.. Commander.GetAllUnits()];
    }

    #endregion

    #endregion

}