using System.Collections.Generic;
using System.Linq;
using Godot;
namespace CommonScripts;

public partial class AIDirector : Node2D {

    #region Instance Members

    [Export] public SpawnMode Mode = SpawnMode.Static;

    [Export] public int SpawnpointSelectionCount = 10;

    #region Godot Callbacks

    public override void _EnterTree() {
        if (Instance != null) {
            Log.Err("Multiple instances of AIDirector detected. There should only be one AIDirector in the scene.");
            QueueFree();
            return;
        }

        Instance = this;
	}

    public override void _Ready() {
    }

    public override void _Process(double delta) {
        switch (Mode) {
            case SpawnMode.Static:
                CallDeferred(nameof(UpdateStaticSpawning), delta);
                break;
            case SpawnMode.Dynamic:
                break;
        }

        SearchAndDestroy();
    }

    #endregion

    #endregion

    #region Static Members

    public static AIDirector Instance { get; private set; } = null!;

    public static Level CurrentLevel { get; set; } = null!;

    public enum SpawnMode {
        Static,
        Dynamic
    }


    #region Static Spawning

    private static double _timeDelayRemaining = 0d;


    public static void UpdateStaticSpawning(double delta) {

        if (_timeDelayRemaining > 0d) {
            _timeDelayRemaining -= delta;
            return;
        }

        if (CurrentLevel == null) return;

        if (CurrentLevel.EnemyTypes.Length == 0) {
            Log.Warn(() => "No enemy types assigned to the current level for spawning.");
            return;
        }

        RandomNumberGenerator rng = new();
        List<StandardCharacter> units = [.. Commander.GetAllUnits()];

        int targetUnitIndex = rng.RandiRange(0, units.Count - 1);
        Vector2 targetUnitPosition = units[targetUnitIndex].GlobalPosition;

        List<StandardEnemy> enemySelection = [];
        int totalWeight = 0;

        foreach (PackedScene enemyScene in CurrentLevel.EnemyTypes) {
            Node node = enemyScene.Instantiate<StandardEnemy>();

            if (node is not StandardEnemy enemy) {
                Log.Warn(() => $"Enemy scene '{enemyScene.ResourcePath}' is not a StandardEnemy. Skipping.");
                continue;
            }

            enemySelection.Add(enemy);

            totalWeight += enemy.StaticSpawnWeight;
        }

        int roll = rng.RandiRange(1, totalWeight);
        int remainingRoll = roll;
        enemySelection.Sort((a, b) => b.StaticSpawnWeight.CompareTo(a.StaticSpawnWeight));

        // Subtracts weights until it finds the selected enemy
        foreach (StandardEnemy enemy in enemySelection) {
            remainingRoll -= enemy.StaticSpawnWeight;

            if (remainingRoll <= 0) {
                bool wasSpawned = TrySpawnEnemy(enemy);

                if (!wasSpawned) {
                    enemy.QueueFree();
                    return;
                }

                _timeDelayRemaining = enemy.DelayAfterSpawn;
                return;
            }
        }
    }

    #endregion

    #region Spawn Management

    /// <summary>
    /// Enemies that have been spawned in on the game world.
    /// </summary>
    public static List<StandardCharacter> Enemies { get; private set; } = [];

    public static bool TrySpawnEnemy(StandardCharacter enemy) {
        List<StandardCharacter> playerUnits = [.. Commander.GetAllUnits()];
        if (playerUnits.Count == 0) {
            Log.Warn(() => "No player units registered in EnemyManager. Cannot select spawn points based on player unit positions.");
            return false;
        }

        if (CurrentLevel == null) {
            Log.Err(() => "No current level registered in EnemyManager. Cannot spawn enemies.");
            return false;
        }

        RandomNumberGenerator rng = new();
        int targetUnit = rng.RandiRange(0, playerUnits.Count - 1);
        int spawnIndex = rng.RandiRange(0, Instance.SpawnpointSelectionCount - 1);

        Node2D? spawnPoint = CurrentLevel.FindNearbyEnemySpawnPoint(
            playerUnits[targetUnit].GlobalPosition,
            float.MaxValue,
            spawnIndex,
            true
        );

        if (spawnPoint != null) {
            float distanceToTargetUnit = spawnPoint.GlobalPosition.DistanceTo(playerUnits[targetUnit].GlobalPosition);
            float targetingRadius = enemy.TargetingManager.TargetDetectionRadius;
            if (distanceToTargetUnit <= targetingRadius) return false;
            CurrentLevel.SpawnCharacter(enemy, spawnPoint);
            Enemies.Add(enemy);
            return true;
        }

        else return false;
    }


    public static void ClearEnemies() {
        foreach (StandardCharacter enemy in Enemies) {
            if (IsInstanceValid(enemy)) enemy.QueueFree();
            EntityManager.RemoveCharacter(enemy);
        }

        Enemies.Clear();
    }


    public static void PurgeDeadEnemies() {
        List<StandardCharacter> deadEnemies = [.. Enemies.Where(e => !e.IsAlive)];

        foreach (StandardCharacter enemy in deadEnemies) {
            enemy.QueueFree();
            EntityManager.RemoveCharacter(enemy);
            Enemies.Remove(enemy);
        }
    }

    #endregion

    #region Navigation

    public static StandardCharacter? FindNearestPlayer(Vector2 position) {
        List<StandardCharacter> playerUnits = [.. Commander.GetAllUnits()];

        if (playerUnits.Count == 0) {
            Log.Warn(() => "No player units registered in EnemyManager. Cannot find nearest player unit.");
            return null;
        }

        StandardCharacter? nearestUnit = null;
        foreach (StandardCharacter unit in playerUnits) {
            if (!unit.IsAlive) continue;

            float current = unit.GlobalPosition.DistanceTo(position);
            if (nearestUnit == null || current < nearestUnit.GlobalPosition.DistanceTo(position)) nearestUnit = unit;
        }

        return nearestUnit;
    }

    private static void SearchAndDestroy() {
        foreach (StandardCharacter enemy in Enemies) {
            if (!enemy.IsAlive || !IsInstanceValid(enemy)) continue;

            StandardCharacter? player = FindNearestPlayer(enemy.GlobalPosition);
            if (player == null) return;

            AIAgentManager agentManager = enemy.AIAgent;
            if (agentManager.HasDestination) continue;
            if (!IsInstanceValid(agentManager)) continue;

			void GoToPlayer() {
                agentManager.GoTo(player.GlobalPosition);
            }

            enemy.GetTree().CreateTimer(1f).Timeout += GoToPlayer;
        }
    }

    #endregion

    #endregion

}