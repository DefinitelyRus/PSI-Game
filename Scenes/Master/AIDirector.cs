using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
namespace CommonScripts;

public partial class AIDirector : Node2D {

    #region Instance Members

    [Export] public SpawnMode Mode = SpawnMode.Static;

    [Export] public int SpawnpointSelectionCount = 10;
    [Export] public PackedScene[] EnemyScenes = [];

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
                CallDeferred(nameof(UpdateDynamicSpawning), delta);
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

    public static bool AllowSpawning { get; set; }= true;

    public static void UpdateStaticSpawning(double delta) {
        if (_timeDelayRemaining > 0d) {
            _timeDelayRemaining -= delta;
            return;
        }

        if (!AllowSpawning) return;

        if (CurrentLevel == null) return;

        if (CurrentLevel.StaticEnemyTypes.Length == 0) {
            Log.Warn(() => "No enemy types assigned to the current level for spawning.");
            return;
        }

        // Get all alive enemies
        List<StandardCharacter> aliveEnemies = [.. Commander.GetAllUnits().Where(unit => unit.IsAlive)];
        if (aliveEnemies.Count >= CurrentLevel.EnemyCountLimit) return;

        RandomNumberGenerator rng = new();
        List<StandardCharacter> units = [.. aliveEnemies];

        int targetUnitIndex = rng.RandiRange(0, units.Count - 1);

        if (targetUnitIndex < 0 || targetUnitIndex >= units.Count) return;

        StandardCharacter targetUnit = units[targetUnitIndex];
        if (!IsInstanceValid(targetUnit)) return;
        Vector2 targetUnitPosition = targetUnit.GlobalPosition;

        List<StandardEnemy> enemySelection = [];
        int totalWeight = 0;

        foreach (PackedScene enemyScene in CurrentLevel.StaticEnemyTypes) {
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

                _timeDelayRemaining = enemy.DelayAfterSpawn * CurrentLevel.EnemyStaticSpawningDelayMultiplier;
                return;
            }
        }
    }

    #endregion

    #region Dynamic Spawning

    /*
     * Dynamic Difficulty Mode
     * This mode adjusts the spawning rate and enemy strength based on player performance.

     * Enemy spawn budget is adjusted based on player performance:
     * If players are doing well (high health, quick completion), the enemy's spawn budget increases.
     * If players are struggling (low health, slow completion), the enemy's spawn budget decreases.
     * 
     * Enemy types are selected based on player performance metrics:
     * If the player performs well on speed, spawn more drones and sentries to slow them down.
     * If the player performs well on health, spawn more cyborgs to increase damage output.
     * If the player performs well at range, spawn sentinels and juggernauts to force close combat.
     * If the player is too slow, spawn juggernauts to force movement.
     * If the player completes too many optional objectives, spawn more juggernauts and sentinels.

     * This mode requires tracking player performance metrics over time.
     * 
     * Combined, these metrics are used together to determine when and how to spawn enemies dynamically.
     */

    // Enemy Types
    public enum EnemyType {
        Flakfly,
        Drone,
        Sentry,
        Cyborg,
        Sentinel,
        Juggernaut
    }

    /// <summary>
    /// Spawn weights for each enemy type based on player performance metrics.
    /// <br/><br/>
    /// How it works:
    /// 1. Each enemy type has a base weight.
    /// 2. Based on player performance metrics (health, pace, range, optional objectives ), weights are adjusted up or down.
    /// 3. When spawning an enemy, a random number is rolled against the total weight to select which enemy type to spawn.
    /// 
    /// </summary>
    private static Dictionary<EnemyType, float> _spawnWeights = [];

    private static void RebuildSpawnWeights() {
        _spawnWeights = [];

        // Speed: fast players get slowed (Drones, Sentries)
        float speed = PaceRatio;
        AddWeight(EnemyType.Drone, speed);
        AddWeight(EnemyType.Sentry, speed);

        // Slow players: spawn Juggernauts to force movement
        if (speed < 1f) AddWeight(EnemyType.Juggernaut, 1f - speed);

        // Health: high HP -> more Cyborgs
        float health = HealthRatio;
        AddWeight(EnemyType.Cyborg, health);

        // Range: strong at range -> force close combat
        float range = RangeRatio;
        AddWeight(EnemyType.Sentinel, range);
        AddWeight(EnemyType.Juggernaut, range);

        // Optional objectives: too many = big threats
        float optional = OptionalRatio;
        AddWeight(EnemyType.Juggernaut, optional * 1.5f);
        AddWeight(EnemyType.Sentinel, optional * 1.5f);
    }

    private static void AddWeight(EnemyType type, float amount) {
        if (!_spawnWeights.ContainsKey(type)) {
            _spawnWeights[type] = 0f;
        }

        _spawnWeights[type] += amount;
    }

    
    private static StandardEnemy? ChooseSpawnType() {
        if (CurrentLevel == null || CurrentLevel.DynamicEnemyTypes.Length == 0) {
            Log.Warn(() => "No dynamic enemy types assigned to the current level for spawning.");
            return null;
        }

        // Compute weights only for available types
        float total = 0f;
        List<(StandardEnemy enemy, float weight)> candidates = [];
        foreach (PackedScene enemyScene in CurrentLevel.DynamicEnemyTypes) {
            Node node = enemyScene.Instantiate<StandardEnemy>();
            if (node is not StandardEnemy enemy) {
                Log.Warn(() => $"Enemy scene '{enemyScene.ResourcePath}' is not a StandardEnemy. Skipping.");
                continue;
            }

			_spawnWeights.TryGetValue(enemy.EnemyType, out float weight);
			candidates.Add((enemy, weight));
            total += weight;
        }

        if (candidates.Count == 0) return null;

        // If all weights are zero or negative, fallback to equal weights among available types
        if (total <= 0f) {
            total = candidates.Count;
            for (int i = 0; i < candidates.Count; i++) {
                candidates[i] = (candidates[i].enemy, 1f);
            }
        }

        float pick = (float) GD.RandRange(0, total);
        // For each candidate...
        for (int candIdx = 0; candIdx < candidates.Count; candIdx++) {
            pick -= candidates[candIdx].weight;

            // If picked...
            if (pick <= 0f) {

                // For all others, free them
                for (int j = 0; j < candidates.Count; j++) {
                    if (j == candIdx) continue;
                    candidates[j].enemy.QueueFree();
                }

                return candidates[candIdx].enemy;
            }
        }

        // Fallback: choose first and free others
        for (int j = 1; j < candidates.Count; j++) candidates[j].enemy.QueueFree();

        return candidates[0].enemy;
    }


    // Wave Management
    public static float WaveDuration { get; set; } = 10f;
    public static float TimeUntilNextWave { get; set; } = 15f;
    private static float _waveTimer;
    private static float _waveCooldownTimer;
    private static bool _isWaveActive;
    public static float EnemyOverflowThreshold { get; set; } = 0.25f;
    private static bool _hasStartedAtLeastOneWave; // tracks if initial wave has fired

    private static float GetCurrentEnemyTokenValue() {
        float total = 0f;
        foreach (StandardCharacter enemy in Enemies) {
            if (!IsInstanceValid(enemy) || !enemy.IsAlive) continue;
            if (enemy is StandardEnemy se) {
                total += Mathf.Max(0f, se.DynamicSpawnCost);
            }
        }
        return total;
    }

    private static float ComputeNextWaveBudgetPreview() {
        if (CurrentLevel == null) return 0f;
        float scaled = CurrentLevel.BaseBudget;
        float difficulty = Mathf.Clamp(
            (HealthRatio + PaceRatio + OptionalRatio + RangeRatio) * 0.25f,
            0.5f, 2f
        );
        return Mathf.Clamp(scaled * difficulty, MinBudget, MaxBudget);
    }
    private static void StartWave() {
        if (CurrentLevel == null) return;

        RecalculateBudget();
        _isWaveActive = true;
        _waveTimer = WaveDuration;
    if (!_hasStartedAtLeastOneWave) _hasStartedAtLeastOneWave = true;

        Log.Me(() => "Starting new wave.");
    }

    private static void EndWave() {
    _isWaveActive = false;
    Log.Me(() => "Ending current wave.");
    }

    private static void UpdateWave(float delta) {
        if (_isWaveActive) {
            _waveTimer -= delta;
            if (_waveTimer <= 0f) EndWave();
        }

        else {
            // First wave: start immediately (no cooldown)
            if (!_hasStartedAtLeastOneWave) {
                StartWave();
                return;
            }

            // Subsequent waves: manage cooldown gating
            if (_waveCooldownTimer <= 0f) {
                float currentTokens = GetCurrentEnemyTokenValue();
                float nextBudget = ComputeNextWaveBudgetPreview();
                float threshold = Mathf.Clamp(EnemyOverflowThreshold, 0f, 1f);

                // Hold off starting the cooldown until enemy field is below threshold
                if (_waveCooldownTimer == 0f && nextBudget > 0f && currentTokens >= nextBudget * threshold) {
                    return; // wait before initiating cooldown
                }

                // If cooldown not set yet, set it
                if (_waveCooldownTimer == 0f) {
                    _waveCooldownTimer = TimeUntilNextWave;
                    Log.Me(() => "Starting inter-wave cooldown.");
                    return; // begin countdown next frame
                }
            }

            // Countdown
            _waveCooldownTimer -= delta;
            if (_waveCooldownTimer <= 0f) {
                StartWave();
            }
        }
    }


    private static bool SpawnEnemy(StandardEnemy enemy) {
        bool wasSpawned = TrySpawnEnemy(enemy);

        if (wasSpawned) return true;

        return false;
    }




    // Budget
    public static float MinBudget { get; set; } = 25f;
    public static float MaxBudget { get; set; } = 500f;
    public static double SpendCooldown { get; set; } = 0.5f;
    private static double _spendTimer;
    private static float _tokensLeftThisWave;
    private static void RecalculateBudget() {
        float scaled = CurrentLevel.BaseBudget;

        // Clamp difficulty multiplier
        float difficulty = Mathf.Clamp(
            (HealthRatio + PaceRatio + OptionalRatio + RangeRatio) * 0.25f,
            0.5f, 2f
        );

        _tokensLeftThisWave = Mathf.Clamp(scaled * difficulty, MinBudget, MaxBudget);
    }


    private static void SpendBudget(float delta) {
        if (_tokensLeftThisWave <= 0f) return;

        if (_spendTimer > 0d) {
            _spendTimer -= delta;
            return;
        }

        // Refresh weights before choosing
        RebuildSpawnWeights();
        if (_spawnWeights.Count == 0) {
            Log.Warn(() => "Dynamic spawn weights empty; skipping spawn.");
            _spendTimer = SpendCooldown;
            return;
        }
        StandardEnemy? chosen = ChooseSpawnType();
        if (chosen == null) {
            Log.Warn(() => "No enemy type chosen for dynamic spawning.");
            _spendTimer = SpendCooldown;
            return;
        }

        float cost = chosen.DynamicSpawnCost;
        bool wasSpawned = SpawnEnemy(chosen);
        bool hasBudget = cost <= _tokensLeftThisWave;

        // Not enough budget: try cheaper enemy
        if (!hasBudget) {
            Log.Me(() => $"Insufficient budget for enemy cost {cost}. Remaining {_tokensLeftThisWave}.");

            chosen.QueueFree();

            if (CurrentLevel == null || CurrentLevel.DynamicEnemyTypes.Length == 0) return;
            StandardEnemy?[] enemies = [.. CurrentLevel.DynamicEnemyTypes.Select(scene => scene.Instantiate<StandardEnemy>())];

            foreach (StandardEnemy? enemy in enemies) {
                if (enemy == null) continue;
                if (enemy.DynamicSpawnCost >= cost) continue;

                Log.Me(() => $"Found cheaper enemy with cost {enemy.DynamicSpawnCost}. Trying to spawn it instead.");
                bool cheaperSpawned = SpawnEnemy(enemy);

                if (!cheaperSpawned) {
                    enemy.QueueFree();
                    continue;
                }

                _tokensLeftThisWave -= enemy.DynamicSpawnCost;
                _spendTimer = SpendCooldown;
                return;
            }

            if (!wasSpawned) Log.Me(() => "Failed to spawn chosen enemy (placement near player invalid). Retrying later.");
            return;
        }

        // Failed to spawn: free and try again later
        if (!wasSpawned) chosen.QueueFree();

        _tokensLeftThisWave -= cost;
        _spendTimer = SpendCooldown;
    }




    // Player Performance Metrics
    /// <summary>
    /// 0–1 based on total unit HP
    /// <br/><br/>
    /// Updated over time.
    /// </summary>
    public static float HealthRatio { get; set; } = 1f;

    /// <summary>
    /// >1 means fast, <1 means slow
    /// <br/><br/>
    /// Updated over time.
    /// </summary>
    public static float PaceRatio { get; set; } = 1f;

    /// <summary>
    /// % of kills done at long range
    /// <br/><br/>
    /// Updated over time.
    /// </summary>
    public static float RangeRatio { get; set; } = 0f;

    /// <summary>
    /// completed_optional / total_optional
    /// <br/><br/>
    /// Updated at the end of each level.
    /// </summary>
    public static float OptionalRatio { get; set; } = 0f;




    #endregion

    #region Spawn Management

    /// <summary>
    /// Enemies that have been spawned in on the game world.
    /// </summary>
    public static List<StandardCharacter> Enemies { get; private set; } = [];

    /// <summary>
    /// Attempts to spawn an enemy at a spawn point near a random player unit.
    /// </summary>
    /// <returns></returns>
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

    // Entry point for Dynamic mode
    public static void UpdateDynamicSpawning(double delta) {
    if (!AllowSpawning) return;
    if (CurrentLevel == null) return; // wait for level to be set before any dynamic work

        UpdateWave((float)delta);
        if (_isWaveActive) {
            SpendBudget((float)delta);
        }
    }

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
            if (!IsInstanceValid(player)) return;

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