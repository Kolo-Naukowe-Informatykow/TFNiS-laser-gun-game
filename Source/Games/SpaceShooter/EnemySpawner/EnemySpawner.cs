using Godot;
using System;
using SpaceShooter.Enemies;

public partial class EnemySpawner : Node
{
    [Signal] public delegate void EnemyEscapedEventHandler(int damageToPlayer);

    [Export] private Godot.Collections.Array<PackedScene> _enemyScenes;
    [Export] private Node _spawnParent;
    [Export] private float _spawnIntervalMin = 1.2f;
    [Export] private float _spawnIntervalMax = 2.1f;
    [Export] private float _horizonY = 120f;

    private readonly RandomNumberGenerator _rng = new();
    private Timer _spawnTimer;

    public override void _Ready()
    {
        if (GlobalGameManager.Instance != null)
        {
            GlobalGameManager.Instance.SeedRngUnique(_rng, nameof(EnemySpawner));
        }
        else
        {
            _rng.Randomize();
        }

        _spawnTimer = new Timer
        {
            OneShot = true,
            Autostart = false
        };

        AddChild(_spawnTimer);
        _spawnTimer.Timeout += OnSpawnTimerTimeout;

        ScheduleNextSpawn();
    }

    private void OnSpawnTimerTimeout()
    {
        SpawnEnemy();
        ScheduleNextSpawn();
    }

    private void ScheduleNextSpawn()
    {
        float minInterval = Mathf.Max(0.05f, Math.Min(_spawnIntervalMin, _spawnIntervalMax));
        float maxInterval = Mathf.Max(minInterval, Math.Max(_spawnIntervalMin, _spawnIntervalMax));

        _spawnTimer.WaitTime = _rng.RandfRange(minInterval, maxInterval);
        _spawnTimer.Start();
    }

    private void SpawnEnemy()
    {
        if (_enemyScenes == null || _enemyScenes.Count == 0)
        {
            GD.PushWarning("EnemySpawner: brak przypisanych scen przeciwnikow.");
            return;
        }

        int randomIndex = _rng.RandiRange(0, _enemyScenes.Count - 1);
        PackedScene enemyScene = _enemyScenes[randomIndex];
        if (enemyScene == null)
        {
            GD.PushWarning("EnemySpawner: wykryto pusta scene przeciwnika na liscie.");
            return;
        }

        Node enemyNode = enemyScene.Instantiate();
        if (enemyNode is not Enemy enemy)
        {
            GD.PushWarning("EnemySpawner: scena nie dziedziczy po Enemy.");
            enemyNode.QueueFree();
            return;
        }

        Node parent = _spawnParent ?? GetParent();
        if (parent == null)
        {
            GD.PushWarning("EnemySpawner: brak parenta dla spawnowanego przeciwnika. Spawn anulowany.");
            enemy.QueueFree();
            return;
        }

        parent.AddChild(enemy);

        EnemySpawnOrigin spawnOrigin = PickSpawnOrigin();
        EnemyTargetLane targetLane = PickTargetLane(spawnOrigin);

        enemy.Escaped += OnEnemyEscaped;
        enemy.ConfigureSpawn(_horizonY, spawnOrigin, targetLane);
    }

    private void OnEnemyEscaped(int damageToPlayer)
    {
        EmitSignal(SignalName.EnemyEscaped, damageToPlayer);
    }

    private EnemySpawnOrigin PickSpawnOrigin()
    {
        int index = _rng.RandiRange(0, 2);
        return (EnemySpawnOrigin)index;
    }

    private EnemyTargetLane PickTargetLane(EnemySpawnOrigin spawnOrigin)
    {
        switch (spawnOrigin)
        {
            case EnemySpawnOrigin.TopLeft:
                return _rng.Randf() < 0.5f ? EnemyTargetLane.BottomCenter : EnemyTargetLane.BottomRight;
            case EnemySpawnOrigin.TopRight:
                return _rng.Randf() < 0.5f ? EnemyTargetLane.BottomCenter : EnemyTargetLane.BottomLeft;
            default:
                return (EnemyTargetLane)_rng.RandiRange(0, 2);
        }
    }
}
