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

    [ExportGroup("Spawning")]
    [Export] public bool AllowSpawningInstance { get; set; } = true;

    [ExportGroup("Wave & Budget")]
    [Export] public float WaveDurationInstance { get; set; } = 10f;
    [Export] public float TimeUntilNextWaveInstance { get; set; } = 15f;
    [Export] public float EnemyOverflowThresholdInstance { get; set; } = 0.25f;
    [Export] public float PerWaveMaxIncreaseInstance { get; set; } = 0.25f;
    [Export] public int MinBudgetInstance { get; set; } = 25;
    [Export] public int MaxBudgetInstance { get; set; } = 500;
    [Export] public double SpendCooldownInstance { get; set; } = 0.5f;

    [ExportGroup("Performance Metrics")]
    [Export] public float HealthRatioInstance { get; set; } = 1f;
    [Export] public float PaceRatioInstance { get; set; } = 1f;
    [Export] public float RangeRatioInstance { get; set; } = 1f;
    [Export] public float OptionalRatioInstance { get; set; } = 1f;
    [Export] public float HealthRatingFixedPointInstance { get; set; } = 1200f;
    [Export] public float RangeRatingFixedPointInstance { get; set; } = 360f;

    [ExportGroup("Metric Thresholds")]
    [Export] public int KillDistanceWindowInstance { get; set; } = 15;
    [Export] public float RangeMinDistanceInstance { get; set; } = 64f;
    [Export] public float RangeMaxDistanceInstance { get; set; } = 320f;
    [Export] public float PaceMinOpmInstance { get; set; } = 0.5f;
    [Export] public float PaceMaxOpmInstance { get; set; } = 3.0f;
    [Export] public float PaceWindowSecondsInstance { get; set; } = 60f;
    [Export] public float PaceVeryLowThresholdInstance { get; set; } = 0.15f;
    [Export] public float PaceCruisingMinInstance { get; set; } = 1.1f;
    [Export] public float PaceCruisingMaxInstance { get; set; } = 1.4f;
    [Export] public float PaceMediumThresholdInstance { get; set; } = 1.4f;
    [Export] public float PaceVeryHighThresholdInstance { get; set; } = 1.7f;
    [Export] public float RangeVeryHighThresholdInstance { get; set; } = 1.5f;

    #region Godot Callbacks

    public override void _EnterTree() {
        if (Instance != null) {
            Log.Err("Multiple instances of AIDirector detected. There should only be one AIDirector in the scene.");
            QueueFree();
            return;
        }

        Instance = this;
	}

    public override void _Process(double delta) {
        if (Master.IsPaused) return;

        if (CurrentLevel != null) LevelElapsedTime += (float) delta;

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
    private static Level? _currentLevel;

    public static Level CurrentLevel {
        get => _currentLevel!;
        set {
            if (!ReferenceEquals(_currentLevel, value)) {
                Level? previous = _currentLevel;
                _currentLevel = value;

                if (previous != null) FinalizeOptionalRatioFromPreviousLevel();
                else Instance.OptionalRatioInstance = 1f;

                OptionalObjectivesCompleted = 0;
                OptionalObjectivesTotal = 0;
                _hasCompletedFirstRequiredObjective = false;
                _paceUpdateAccumulator = 0f;
                Instance.PaceRatioInstance = 1f;
            
                ResetWaveState();
            }
        }
    }

    public enum SpawnMode {
        Static,
        Dynamic
    }


    #region Static Spawning

    private static double _timeDelayRemaining = 0d;

    public static bool AllowSpawning { get => Instance.AllowSpawningInstance; set => Instance.AllowSpawningInstance = value; }

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

    public enum EnemyType {
        Flakfly,
        Drone,
        Sentry,
        Cyborg,
        Sentinel,
        Juggernaut
    }

    private static Dictionary<EnemyType, float> _spawnWeights = [];

    private static void RebuildSpawnWeights() {
        _spawnWeights = [];

        float speed = Instance.PaceRatioInstance;
        AddWeight(EnemyType.Drone, speed);
        AddWeight(EnemyType.Sentry, speed);

        if (speed < 1f) AddWeight(EnemyType.Juggernaut, 1f - speed);

        float health = Instance.HealthRatioInstance;
        AddWeight(EnemyType.Cyborg, health);

        float range = Instance.RangeRatioInstance;
        AddWeight(EnemyType.Sentinel, range);
        AddWeight(EnemyType.Juggernaut, range);

        float optional = Instance.OptionalRatioInstance;
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
        float pace = Instance.PaceRatioInstance;
        float range = Instance.RangeRatioInstance;
        float veryLowPace = Instance.PaceVeryLowThresholdInstance;
        float cruisingMin = Instance.PaceCruisingMinInstance;
        float cruisingMax = Instance.PaceCruisingMaxInstance;
        float mediumPace = Instance.PaceMediumThresholdInstance;
        float veryHighPace = Instance.PaceVeryHighThresholdInstance;
        float veryHighRange = Instance.RangeVeryHighThresholdInstance;

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
    public static float WaveDuration { get => Instance.WaveDurationInstance; set => Instance.WaveDurationInstance = value; }
    public static float TimeUntilNextWave { get => Instance.TimeUntilNextWaveInstance; set => Instance.TimeUntilNextWaveInstance = value; }
    private static float _waveTimer;
    private static float _waveCooldownTimer;
    private static bool _isWaveActive;
    public static float EnemyOverflowThreshold { get => Instance.EnemyOverflowThresholdInstance; set => Instance.EnemyOverflowThresholdInstance = value; }
    private static bool _hasStartedAtLeastOneWave;
    public static float PerWaveMaxIncrease { get => Instance.PerWaveMaxIncreaseInstance; set => Instance.PerWaveMaxIncreaseInstance = value; }
    private static int _lastWaveBudgetComputed;
    private static void ResetWaveState() {
        _waveTimer = 0f;
        _waveCooldownTimer = 0f;
        _isWaveActive = false;
        _hasStartedAtLeastOneWave = false;
        _lastWaveBudgetComputed = 0;
        _tokensLeftThisWave = 0;
        _spendTimer = 0d;
        _spawnWeights.Clear();
    }

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
            (Instance.HealthRatioInstance + Instance.PaceRatioInstance + Instance.OptionalRatioInstance + Instance.RangeRatioInstance) * 0.25f,
            0.5f, 2f
        );
        int preview = Mathf.RoundToInt(scaled * difficulty);
        float activeTokens = GetCurrentEnemyTokenValue();
        int maxAllowed = Mathf.Max(0, CurrentLevel.MaxTotalBudget - Mathf.RoundToInt(activeTokens));
        preview = Mathf.Min(preview, maxAllowed);
        preview = Mathf.Max(preview, CurrentLevel.MinSpendBudget);
        return preview;
    }
    private static void StartWave() {
        if (CurrentLevel == null) return;

        RecalculateBudget();
        float activeNow = GetCurrentEnemyTokenValue();
        int deduction = Mathf.RoundToInt(activeNow * 0.5f);
        int before = _tokensLeftThisWave;

        _tokensLeftThisWave = Mathf.Max(0, _tokensLeftThisWave - deduction);
        _tokensLeftThisWave = Mathf.Max(CurrentLevel.MinSpendBudget, _tokensLeftThisWave);
        _lastWaveBudgetComputed = _tokensLeftThisWave;

        if (deduction > 0) {
            Log.Me(() => $"Wave budget adjusted: base={before}, activeTokens={activeNow:F0}, deduction(50%)={deduction}, final={_tokensLeftThisWave}");
        }

        _isWaveActive = true;
        _waveTimer = WaveDuration;

        if (!_hasStartedAtLeastOneWave) _hasStartedAtLeastOneWave = true;

        Log.Me(() => "Starting new wave.");
        Log.Me(() => $"Wave metrics: Health={Instance.HealthRatioInstance:F2}, Pace={Instance.PaceRatioInstance:F2}, Range={Instance.RangeRatioInstance:F2}, Optional={Instance.OptionalRatioInstance:F2}. Budget={_tokensLeftThisWave}");
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
            if (!_hasStartedAtLeastOneWave) {
                StartWave();
                return;
            }

            if (_waveCooldownTimer <= 0f) {
                float currentTokens = GetCurrentEnemyTokenValue();
                float nextBudget = ComputeNextWaveBudgetPreview();
                float baseDelay = TimeUntilNextWave;
                float ratio = 0f;

                if (nextBudget > 0f) ratio = Mathf.Max(0f, currentTokens / nextBudget);

                float addedDelay = baseDelay * ratio;
                _waveCooldownTimer = baseDelay + addedDelay;

                Log.Me(() => $"Starting inter-wave cooldown. baseDelay={baseDelay:F1}, activeTokens={currentTokens:F0}, nextBudget={nextBudget:F0}, ratio={ratio:F2}, totalDelay={_waveCooldownTimer:F1}");

                return;
            }

            _waveCooldownTimer -= delta;
            if (_waveCooldownTimer <= 0f) StartWave();
        }
    }


    private static bool SpawnEnemy(StandardEnemy enemy) {
        bool wasSpawned = TrySpawnEnemy(enemy);

        if (wasSpawned) return true;

        return false;
    }




    // Player Performance Metrics
    public static float HealthRatio { get => Instance.HealthRatioInstance; set => Instance.HealthRatioInstance = value; }
    public static float PaceRatio { get => Instance.PaceRatioInstance; set => Instance.PaceRatioInstance = value; }
    public static float RangeRatio { get => Instance.RangeRatioInstance; set => Instance.RangeRatioInstance = value; }
    public static float OptionalRatio { get => Instance.OptionalRatioInstance; set => Instance.OptionalRatioInstance = value; }
    public static int OptionalObjectivesTotal { get; set; } = 0;
    public static int OptionalObjectivesCompleted { get; set; } = 0;

    public static float HealthRatingFixedPoint { get => Instance.HealthRatingFixedPointInstance; set => Instance.HealthRatingFixedPointInstance = value; }
    public static float RangeRatingFixedPoint { get => Instance.RangeRatingFixedPointInstance; set => Instance.RangeRatingFixedPointInstance = value; }
    public static int KillDistanceWindow { get => Instance.KillDistanceWindowInstance; set => Instance.KillDistanceWindowInstance = value; }
    private static readonly Queue<float> _killDistances = new();
    public static void RegisterKillDistance(float distance) {
        if (distance < 0f) return;

        _killDistances.Enqueue(distance);

        while (_killDistances.Count > KillDistanceWindow) _killDistances.Dequeue();

        if (_killDistances.Count == 1) Log.Me(() => $"First kill distance sample registered (distance={distance:F1}). Range metric will begin updating.");
    }

    public static void ResetMetrics() {
        HealthRatio = 1f;
        PaceRatio = 1f;
        RangeRatio = 1f;
        LevelElapsedTime = 0f;

        _hasCompletedFirstRequiredObjective = false;
        _paceUpdateAccumulator = 0f;
        _requiredObjectiveCompletionTimes.Clear();
        _killDistances.Clear();

        RequiredObjectivesTotal = 0;
        RequiredObjectivesCompleted = 0;
        OptionalObjectivesTotal = 0;
        OptionalObjectivesCompleted = 0;
    }

    public static float LevelElapsedTime { get; set; } = 0f;
    public static int RequiredObjectivesTotal { get; set; } = 0;
    public static int RequiredObjectivesCompleted { get; set; } = 0;
    public static void SetRequiredObjectives(int totalIncludingCompletionObjective, int completedEligibleObjectives) {
        RequiredObjectivesTotal = Mathf.Max(0, totalIncludingCompletionObjective);
        RequiredObjectivesCompleted = Mathf.Max(0, completedEligibleObjectives);
    }

    private static bool _hasCompletedFirstRequiredObjective = false;
    private static float _paceUpdateAccumulator = 0f;
    private static readonly List<float> _requiredObjectiveCompletionTimes = new();
    public static void RegisterRequiredObjectiveCompletion() {
        RequiredObjectivesCompleted++;
        if (!_hasCompletedFirstRequiredObjective) {
            _hasCompletedFirstRequiredObjective = true;
            _paceUpdateAccumulator = 1f;
            Log.Me(() => "Pace unlocked: first required objective completion registered.");
        }
        
        else {
            _paceUpdateAccumulator = 1f;
            Log.Me(() => "Pace timer reset after required objective completion.");
        }

        _requiredObjectiveCompletionTimes.Add(LevelElapsedTime);
    }

    public static void UpdatePerformanceMetrics(double delta) {
        List<StandardCharacter> units = [.. Commander.GetAllUnits().Where(u => u.IsAlive)];
        if (units.Count == 0) return;

        float dt = (float)delta;
        UpdateHealthMetric(units, dt);
        UpdatePaceMetric(dt);
        UpdateRangeMetric(dt);
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
        if (!_hasCompletedFirstRequiredObjective) {
            PaceRatio = 1f;
            return;
        }

        _paceUpdateAccumulator += delta;
        if (_paceUpdateAccumulator < 1f) return;
        _paceUpdateAccumulator = 0f;

        float now = LevelElapsedTime;
        float window = Instance.PaceWindowSecondsInstance;
        for (int i = _requiredObjectiveCompletionTimes.Count - 1; i >= 0; i--) {
            if (now - _requiredObjectiveCompletionTimes[i] > window) {
                _requiredObjectiveCompletionTimes.RemoveAt(i);
            }
        }

        int countLastMinute = _requiredObjectiveCompletionTimes.Count;
        float opm = countLastMinute;
        float minOpm = Instance.PaceMinOpmInstance;
        float maxOpm = Instance.PaceMaxOpmInstance;
        float targetPace;

        if (opm <= minOpm) targetPace = 0f;
        else if (opm >= maxOpm) targetPace = 2f;
        else {
            float alpha = (opm - minOpm) / (maxOpm - minOpm);
            targetPace = alpha * 2f;
        }

        PaceRatio = targetPace;
    }

    private static void UpdateRangeMetric(float delta) {
        if (_killDistances.Count < KillDistanceWindow) {
            RangeRatio = Mathf.Lerp(RangeRatio, 0.5f, delta * 2f);
            return;
        }

        float sum = 0f;
        foreach (float d in _killDistances) sum += d;
        float avg = sum / Mathf.Max(1, _killDistances.Count);

        float minDist = Instance.RangeMinDistanceInstance;
        float maxDist = Instance.RangeMaxDistanceInstance;
        float targetRange;

        if (avg <= minDist) targetRange = 0f;
        else if (avg >= maxDist) targetRange = 2f;
        else {
            float t = (avg - minDist) / (maxDist - minDist);
            targetRange = t * 2f;
        }

        RangeRatio = Mathf.Lerp(RangeRatio, targetRange, delta * 2f);
    }

    private static void FinalizeOptionalRatioFromPreviousLevel() {
        if (OptionalObjectivesTotal <= 0) {
            OptionalRatio = 1f;
            return;
        }

        float completion = Mathf.Clamp((float)OptionalObjectivesCompleted / OptionalObjectivesTotal, 0f, 1f);
        OptionalRatio = Mathf.Clamp(completion * 2f, 0f, 2f);
    }

    public static void SetOptionalObjectiveProgress(int completed, int total) {
        OptionalObjectivesTotal = Mathf.Max(0, total);
        OptionalObjectivesCompleted = Mathf.Clamp(completed, 0, OptionalObjectivesTotal);
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
            EntityManager.RemoveEntity(enemy);
        }

        Enemies.Clear();
    }


    public static void PurgeDeadEnemies() {
        List<StandardCharacter> deadEnemies = [.. Enemies.Where(e => !e.IsAlive)];

        foreach (StandardCharacter enemy in deadEnemies) {
            enemy.QueueFree();
            EntityManager.RemoveEntity(enemy);
            Enemies.Remove(enemy);
        }
    }

    #endregion

    public static void UpdateDynamicSpawning(double delta) {
        if (!AllowSpawning) return;
        if (CurrentLevel == null) return;

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

    private static double _spendTimer;
    private static int _tokensLeftThisWave;

    private static void RecalculateBudget() {
        if (CurrentLevel == null) return;
        float scaled = CurrentLevel.BaseBudget;
        float difficulty = Mathf.Clamp(
            (Instance.HealthRatioInstance + Instance.PaceRatioInstance + Instance.OptionalRatioInstance + Instance.RangeRatioInstance) * 0.25f,
            0.5f, 2f
        );
        int computed = Mathf.RoundToInt(scaled * difficulty);
        if (_hasStartedAtLeastOneWave && _lastWaveBudgetComputed > 0) {
            int maxIncrease = Mathf.RoundToInt(_lastWaveBudgetComputed * Mathf.Clamp(PerWaveMaxIncrease, 0f, 1f));
            int cappedUpper = _lastWaveBudgetComputed + maxIncrease;
            if (computed > cappedUpper) computed = cappedUpper;
        }
        float activeTokens = GetCurrentEnemyTokenValue();
        int maxAllowed = Mathf.Max(0, CurrentLevel.MaxTotalBudget - Mathf.RoundToInt(activeTokens));
        computed = Mathf.Min(computed, maxAllowed);
        computed = Mathf.Max(computed, CurrentLevel.MinSpendBudget);
        _tokensLeftThisWave = computed;
        _lastWaveBudgetComputed = computed;
    }

    private static void SpendBudget(float delta) {
        if (_tokensLeftThisWave <= 0) return;
        if (_spendTimer > 0d) {
            _spendTimer -= delta;
            return;
        }
        RebuildSpawnWeights();
        if (_spawnWeights.Count == 0) {
            Log.Warn(() => "Dynamic spawn weights empty; skipping spawn.");
            _spendTimer = Instance.SpendCooldownInstance;
            return;
        }
        StandardEnemy? chosen = ChooseSpawnType();
        if (chosen == null) {
            Log.Warn(() => "No enemy type chosen for dynamic spawning.");
            _spendTimer = Instance.SpendCooldownInstance;
            return;
        }
        int cost = Mathf.RoundToInt(chosen.DynamicSpawnCost);
        bool wasSpawned = SpawnEnemy(chosen);
        bool hasBudget = cost <= _tokensLeftThisWave;
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
                _spendTimer = Instance.SpendCooldownInstance;
                return;
            }
            return;
        }
        if (!wasSpawned) chosen.QueueFree();
        _tokensLeftThisWave -= cost;
        _spendTimer = Instance.SpendCooldownInstance;
    }
}
