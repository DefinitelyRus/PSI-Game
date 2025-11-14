using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
namespace CommonScripts;

public partial class AIDirector : Node2D {

    #region Instance Members

    [Export] public SpawnMode Mode = SpawnMode.Timed;

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
            case SpawnMode.Timed:
                CallDeferred(nameof(UpdateTimedSpawning), delta);
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

    public static Level CurrentLevel { get; private set; } = null!;

    public enum SpawnMode {
        Timed,
        Dynamic
    }

    public static void StartLevel(Level level) {
        CurrentLevel = level;
    }

    #region Timed Spawning

    private static double _timeDelayRemaining = 0d;


    public static void UpdateTimedSpawning(double delta) {

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
                bool wasSpawned = EnemyManager.TrySpawnEnemy(enemy);

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



    #region Navigation

    public static StandardCharacter? FindNearestPlayer(Vector2 position) {
        List<StandardCharacter> playerUnits = [.. Commander.GetAllUnits()];

        if (playerUnits.Count == 0) {
            Log.Warn(() => "No player units registered in EnemyManager. Cannot find nearest player unit.");
            return null;
        }

        return playerUnits.OrderBy(unit => unit.GlobalPosition.DistanceTo(position)).FirstOrDefault();
    }

    private static void SearchAndDestroy() {
        foreach (StandardCharacter enemy in EnemyManager.Enemies) {
            if (!enemy.IsAlive) continue;

            StandardCharacter? player = FindNearestPlayer(enemy.GlobalPosition);
            if (player == null) return;

            AIAgentManager agentManager = enemy.AIAgent;
            if (agentManager.HasDestination) continue;

			void GoToPlayer() {
                agentManager.GoTo(player.GlobalPosition);
            }

            enemy.GetTree().CreateTimer(1f).Timeout += GoToPlayer;
        }
    }

    #endregion

    #endregion

}