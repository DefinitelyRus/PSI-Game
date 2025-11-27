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
    if (CurrentLevel != null) LevelElapsedTime += (float)delta;
    UpdatePerformanceMetrics(delta);
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

        float total = 0f;
        List<(StandardEnemy enemy, float weight)> candidates = [];
        foreach (PackedScene enemyScene in CurrentLevel.DynamicEnemyTypes) {
            Node node = enemyScene.Instantiate<StandardEnemy>();
            if (node is not StandardEnemy enemy) {
                Log.Warn(() => $"Enemy scene '{enemyScene.ResourcePath}' is not a StandardEnemy. Skipping.");
                continue;
            }

			_spawnWeights.TryGetValue(enemy.EnemyType, out float weight);
            float gatedWeight = ApplyExtremeSpawnGates(enemy.EnemyType, weight);
			candidates.Add((enemy, gatedWeight));
            total += gatedWeight;
        }

        if (candidates.Count == 0) return null;

        if (total <= 0f) {
            total = candidates.Count;
            for (int i = 0; i < candidates.Count; i++) {
                candidates[i] = (candidates[i].enemy, 1f);
            }
        }

        float pick = (float) GD.RandRange(0, total);
        for (int candIdx = 0; candIdx < candidates.Count; candIdx++) {
            pick -= candidates[candIdx].weight;
            if (pick <= 0f) {
                for (int j = 0; j < candidates.Count; j++) {
                    if (j == candIdx) continue;
                    candidates[j].enemy.QueueFree();
                }

                return candidates[candIdx].enemy;
            }
        }

        for (int j = 1; j < candidates.Count; j++) candidates[j].enemy.QueueFree();

        return candidates[0].enemy;
    }

    private static float ApplyExtremeSpawnGates(EnemyType type, float baseWeight) {
        float pace = PaceRatio;
        float range = RangeRatio;

        float veryLowPace = 0.3f;
        float cruisingMin = 1.1f;
        float cruisingMax = 1.4f;
        float mediumPace = 1.4f;
        float veryHighPace = 1.7f;
        float veryHighRange = 1.5f;

        switch (type) {
            case EnemyType.Juggernaut:
                if (pace <= veryLowPace) return Mathf.Max(baseWeight, 1f);
                return 0f;
            case EnemyType.Sentinel:
                if (range >= veryHighRange) return Mathf.Max(baseWeight, 1f);
                return 0f;
            case EnemyType.Drone:
                if (pace >= cruisingMin && pace <= cruisingMax) return Mathf.Max(baseWeight, 1f);
                if (pace < cruisingMin) return 0f;
                if (pace >= mediumPace) return 0f;
                return baseWeight;
            case EnemyType.Sentry:
                if (pace >= veryHighPace) return Mathf.Max(baseWeight, 1f);
                return 0f;
            default:
                return baseWeight;
        }
    }


    // Wave Management
    public static float WaveDuration { get; set; } = 10f;
    public static float TimeUntilNextWave { get; set; } = 15f;
    private static float _waveTimer;
    private static float _waveCooldownTimer;
    private static bool _isWaveActive;
    public static float EnemyOverflowThreshold { get; set; } = 0.25f;
    private static bool _hasStartedAtLeastOneWave; // tracks if initial wave has fired
    /// <summary>
    /// Caps how much the budget can increase per wave (0.0–1.0). Default 0.25 means max +25% increase per wave.
    /// Decreases are uncapped.
    /// </summary>
    public static float PerWaveMaxIncrease { get; set; } = 0.25f;
    // Tracks the most recently computed/used budget to apply the per-wave increase cap.
    private static int _lastWaveBudgetComputed;

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
        Log.Me(() => $"Wave metrics: Health={HealthRatio:F2}, Pace={PaceRatio:F2}, Range={RangeRatio:F2}, Optional={OptionalRatio:F2}. Budget={_tokensLeftThisWave}");
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
            // Before starting a wave, re-check enemy overflow threshold to enforce lock
            if (_waveCooldownTimer <= 0f) {
                float currentTokens2 = GetCurrentEnemyTokenValue();
                float nextBudget2 = ComputeNextWaveBudgetPreview();
                float threshold2 = Mathf.Clamp(EnemyOverflowThreshold, 0f, 1f);
                if (nextBudget2 > 0f && currentTokens2 >= nextBudget2 * threshold2) {
                    // Reset cooldown to a small delay to re-evaluate soon rather than insta-starting
                    _waveCooldownTimer = Mathf.Max(1f, TimeUntilNextWave * 0.25f);
                    return;
                }
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
    public static int MinBudget { get; set; } = 25;
    public static int MaxBudget { get; set; } = 500;
    public static double SpendCooldown { get; set; } = 0.5f;
    private static double _spendTimer;
    private static int _tokensLeftThisWave;
    private static void RecalculateBudget() {
    float scaled = CurrentLevel.BaseBudget;

        // Clamp difficulty multiplier
        float difficulty = Mathf.Clamp(
            (HealthRatio + PaceRatio + OptionalRatio + RangeRatio) * 0.25f,
            0.5f, 2f
        );

        int computed = Mathf.RoundToInt(Mathf.Clamp(scaled * difficulty, MinBudget, MaxBudget));

        // Apply per-wave max increase cap relative to last budget if a prior wave exists
        if (_hasStartedAtLeastOneWave && _lastWaveBudgetComputed > 0) {
            int maxIncrease = Mathf.RoundToInt(_lastWaveBudgetComputed * Mathf.Clamp(PerWaveMaxIncrease, 0f, 1f));
            int cappedUpper = _lastWaveBudgetComputed + maxIncrease;
            if (computed > cappedUpper) computed = cappedUpper;
        }

        _tokensLeftThisWave = computed;
        _lastWaveBudgetComputed = computed;
    }


    private static void SpendBudget(float delta) {
        if (_tokensLeftThisWave <= 0) return;

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

        int cost = Mathf.RoundToInt(chosen.DynamicSpawnCost);
        bool wasSpawned = SpawnEnemy(chosen);
        bool hasBudget = cost <= _tokensLeftThisWave;

        // Not enough budget: try cheaper enemy
        if (!hasBudget) {

            chosen.QueueFree();

            if (CurrentLevel == null || CurrentLevel.DynamicEnemyTypes.Length == 0) return;
            StandardEnemy?[] enemies = [.. CurrentLevel.DynamicEnemyTypes.Select(scene => scene.Instantiate<StandardEnemy>())];

            foreach (StandardEnemy? enemy in enemies) {
                if (enemy == null) continue;
                if (enemy.DynamicSpawnCost >= cost) continue;

                bool cheaperSpawned = SpawnEnemy(enemy);

                if (!cheaperSpawned) {
                    enemy.QueueFree();
                    continue;
                }

                _tokensLeftThisWave -= Mathf.RoundToInt(enemy.DynamicSpawnCost);
                _spendTimer = SpendCooldown;
                return;
            }

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
    public static float LevelElapsedTime { get; set; } = 0f;
    public static int RequiredObjectivesTotal { get; set; } = 0;
    public static int RequiredObjectivesCompleted { get; set; } = 0;
    public static void SetRequiredObjectives(int totalIncludingCompletionObjective, int completedEligibleObjectives) {
        RequiredObjectivesTotal = Mathf.Max(0, totalIncludingCompletionObjective);
        RequiredObjectivesCompleted = Mathf.Max(0, completedEligibleObjectives);
    }

    /// <summary>
    /// % of kills done at long range
    /// <br/><br/>
    /// Updated over time.
    /// </summary>
    public static float RangeRatio { get; set; } = 1f;

    /// <summary>
    /// completed_optional / total_optional
    /// <br/><br/>
    /// Updated at the end of each level.
    /// </summary>
    public static float OptionalRatio { get; set; } = 0f;
    public static int OptionalObjectivesTotal { get; set; } = 0;
    public static int OptionalObjectivesCompleted { get; set; } = 0;

    public static float HealthRatingFixedPoint { get; set; } = 1200f;
    public static float RangeRatingFixedPoint { get; set; } = 360f;
    private static float _killDistanceAccum;
    private static int _killDistanceSamples;
    public static void RegisterKillDistance(float distance) {
        if (distance < 0f) return;
        _killDistanceAccum += distance;
        _killDistanceSamples++;
        if (_killDistanceSamples == 1) Log.Me(() => $"First kill distance sample registered (distance={distance:F1}). Range metric will begin updating.");
    }

    public static void ResetMetrics() {
        HealthRatio = 1f;
        PaceRatio = 1f;
        RangeRatio = 1f;
        OptionalRatio = 0f;
        LevelElapsedTime = 0f;
        _killDistanceAccum = 0f;
        _killDistanceSamples = 0;
        RequiredObjectivesTotal = 0;
        RequiredObjectivesCompleted = 0;
        OptionalObjectivesTotal = 0;
        OptionalObjectivesCompleted = 0;
    }

    private static void UpdatePerformanceMetrics(double delta) {
        List<StandardCharacter> units = [.. Commander.GetAllUnits().Where(u => u.IsAlive)];
        if (units.Count == 0) return;

        float dt = (float)delta;
        UpdateHealthMetric(units, dt);
        UpdatePaceMetric(dt);
        UpdateRangeMetric(dt);
        UpdateOptionalMetric(dt);
    }

    private static void UpdateHealthMetric(List<StandardCharacter> units, float delta) {
        float sumHealth = 0f;
        foreach (StandardCharacter u in units) sumHealth += u.Health;
        float avgHealth = sumHealth / units.Count;
        float healthDenom = Mathf.Max(1f, HealthRatingFixedPoint);
        float targetHealth = avgHealth / healthDenom * 2f;
        HealthRatio = Mathf.Lerp(HealthRatio, targetHealth, delta * 2f);
    }

    private static void UpdatePaceMetric(float delta) {
        float levelTimeLimit = CurrentLevel != null ? (float)CurrentLevel.LevelTimeLimit : 1f;
        float elapsed = Mathf.Clamp(LevelElapsedTime, 0f, levelTimeLimit);
        int eligibleObjectivesTotal = Mathf.Max(0, RequiredObjectivesTotal - 1);
        int eligibleObjectivesCompleted = Mathf.Clamp(RequiredObjectivesCompleted, 0, eligibleObjectivesTotal);
        float completionRatio = eligibleObjectivesTotal > 0 ? (float)eligibleObjectivesCompleted / eligibleObjectivesTotal : 0f;
        float timePercent = levelTimeLimit > 0f ? elapsed / levelTimeLimit : 0f;
        float fastGate = 0.3f;
        float slowGate = 0.7f;
        float targetPace = 0f;

        if (completionRatio >= 1f) {
            targetPace = timePercent <= fastGate ? 2f : Mathf.Lerp(2f, 1f, Mathf.InverseLerp(fastGate, 1f, timePercent));
        } else if (completionRatio <= 0f) {
            targetPace = timePercent >= slowGate ? 0f : Mathf.Lerp(1f, 0f, Mathf.InverseLerp(0f, slowGate, timePercent));
        } else {
            float expectedAtFastGate = 1f;
            float rateRequired = expectedAtFastGate / fastGate;
            float currentRate = completionRatio / Mathf.Max(0.0001f, timePercent);
            float efficiency = Mathf.Clamp(currentRate / rateRequired, 0f, 1f);
            float earlyBonus = Mathf.Lerp(0f, 1f, Mathf.InverseLerp(0f, fastGate, timePercent));
            float midBlend = Mathf.Lerp(efficiency, completionRatio, earlyBonus);
            targetPace = Mathf.Lerp(0f, 2f, midBlend);
        }

        PaceRatio = Mathf.Lerp(PaceRatio, targetPace, delta * 2f);
    }

    private static void UpdateRangeMetric(float delta) {
        if (_killDistanceSamples > 0) {
            float avgKillDistance = _killDistanceAccum / _killDistanceSamples;
            float rangeDenom = Mathf.Max(1f, RangeRatingFixedPoint);
            float targetRange = Mathf.Clamp(avgKillDistance / rangeDenom * 2f, 0f, 2f);
            RangeRatio = Mathf.Lerp(RangeRatio, targetRange, delta * 2f);
        } else {
            RangeRatio = Mathf.Lerp(RangeRatio, 1f, delta * 2f);
        }
    }

    private static void UpdateOptionalMetric(float delta) {
        if (OptionalObjectivesTotal > 0) {
            float targetOptional = Mathf.Clamp((float)OptionalObjectivesCompleted / OptionalObjectivesTotal, 0f, 1f);
            OptionalRatio = Mathf.Lerp(OptionalRatio, targetOptional, delta * 2f);
        }
    }

    public static void SetOptionalObjectiveProgress(int completed, int total) {
        OptionalObjectivesTotal = Mathf.Max(0, total);
        OptionalObjectivesCompleted = Mathf.Clamp(completed, 0, OptionalObjectivesTotal);
        OptionalRatio = OptionalObjectivesTotal > 0 ? Mathf.Clamp((float)OptionalObjectivesCompleted / OptionalObjectivesTotal, 0f, 1f) : 0f;
    }

    public static void SetOptionalObjectives(int total) {
        OptionalObjectivesTotal = Mathf.Max(0, total);
        OptionalObjectivesCompleted = Mathf.Min(OptionalObjectivesCompleted, OptionalObjectivesTotal);
    }

    public static void RegisterOptionalObjectiveCompletion() {
        if (OptionalObjectivesTotal <= 0) return;
        if (OptionalObjectivesCompleted >= OptionalObjectivesTotal) return;
        OptionalObjectivesCompleted++;
    }




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