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
    [Export] private float _horizontalSpawnRange = 0.7f;

    private readonly RandomNumberGenerator _rng = new();
    private Timer _spawnTimer;

    public override void _Ready()
    {
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
        parent?.AddChild(enemy);

        enemy.Escaped += OnEnemyEscaped;
        float horizontalOffset = _rng.RandfRange(-_horizontalSpawnRange, _horizontalSpawnRange);
        enemy.ConfigureSpawn(horizontalOffset, _horizonY);
    }

    private void OnEnemyEscaped(int damageToPlayer)
    {
        EmitSignal(SignalName.EnemyEscaped, damageToPlayer);
    }
}
