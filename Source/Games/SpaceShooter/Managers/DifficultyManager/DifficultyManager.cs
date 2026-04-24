using Godot;
using System;

public partial class DifficultyManager : Node
{
    [Export] private EnemySpawner _enemySpawner;
    [Export] private float _targetHardTimeSeconds = 240f;
    [Export] private float _maxSpawnRateMultiplier = 1.5f;
    [Export] private float _maxSpawnCountMultiplier = 1.3f;
    [Export] private float _maxEnemySpeedMultiplier = 1.3f;
    [Export] private float _maxEnemyHealthMultiplier = 1.4f;
    [Export] private float _maxEnemyScoreMultiplier = 1.4f;

    private SpaceShooterGameManager _gameManager;
    private float _elapsedSeconds;
    private bool _isConfigured;
    private float _updateAccumulator;
    private const float UpdateIntervalSeconds = 0.25f;

    public override void _Ready()
    {
        _gameManager = SpaceShooterGameManager.GetOrNull(this);
        if (_gameManager != null)
        {
            _gameManager.SpaceShooterActiveChanged += OnSpaceShooterActiveChanged;
            _gameManager.DefeatStateChanged += OnDefeatStateChanged;
        }

        _isConfigured = false;
        _elapsedSeconds = 0f;
        _updateAccumulator = 0f;
    }

    public override void _ExitTree()
    {
        if (_gameManager != null)
        {
            _gameManager.SpaceShooterActiveChanged -= OnSpaceShooterActiveChanged;
            _gameManager.DefeatStateChanged -= OnDefeatStateChanged;
        }
    }

    public override void _Process(double delta)
    {
        if (!CanProgressDifficulty())
        {
            return;
        }

        float deltaF = Mathf.Max(0f, (float)delta);
        _elapsedSeconds += deltaF;
        _updateAccumulator += deltaF;

        if (_updateAccumulator < UpdateIntervalSeconds)
        {
            return;
        }

        _updateAccumulator = 0f;
        ApplyCurrentDifficulty();
    }

    public void Configure(EnemySpawner enemySpawner)
    {
        _enemySpawner = enemySpawner;
        _isConfigured = true;
        ResetDifficultyProgress();
    }

    private bool CanProgressDifficulty()
    {
        if (!_isConfigured || _enemySpawner == null)
        {
            return false;
        }

        if (_gameManager == null)
        {
            return true;
        }

        return _gameManager.IsSpaceShooterActive && !_gameManager.IsDefeated;
    }

    private void OnSpaceShooterActiveChanged(bool isActive)
    {
        if (isActive)
        {
            ResetDifficultyProgress();
        }
    }

    private void OnDefeatStateChanged(bool isDefeated)
    {
        if (!isDefeated)
        {
            ResetDifficultyProgress();
        }
    }

    private void ResetDifficultyProgress()
    {
        _elapsedSeconds = 0f;
        _updateAccumulator = 0f;
        ApplyCurrentDifficulty();
    }

    private void ApplyCurrentDifficulty()
    {
        if (_enemySpawner == null)
        {
            return;
        }

        float hardTime = Mathf.Max(60f, _targetHardTimeSeconds);
        float progress = Mathf.Clamp(_elapsedSeconds / hardTime, 0f, 1f);
        float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
        float lateGamePressure = Mathf.Pow(progress, 1.5f);
        float pressure = Mathf.Lerp(smoothProgress, lateGamePressure, 0.4f);

        float spawnRateMultiplier = BuildMultiplier(_maxSpawnRateMultiplier, pressure);
        float spawnCountMultiplier = BuildMultiplier(_maxSpawnCountMultiplier, pressure);
        float enemySpeedMultiplier = BuildMultiplier(_maxEnemySpeedMultiplier, pressure);
        float enemyHealthMultiplier = BuildMultiplier(_maxEnemyHealthMultiplier, pressure);
        float enemyScoreMultiplier = BuildMultiplier(_maxEnemyScoreMultiplier, pressure);

        _enemySpawner.SetDifficultyMultipliers(
            spawnRateMultiplier,
            spawnCountMultiplier,
            enemySpeedMultiplier,
            enemyHealthMultiplier,
            enemyScoreMultiplier);
    }

    private static float BuildMultiplier(float maxMultiplier, float pressure)
    {
        float safeMax = Mathf.Max(1f, maxMultiplier);
        float clampedPressure = Mathf.Clamp(pressure, 0f, 1f);
        return Mathf.Lerp(1f, safeMax, clampedPressure);
    }
}
