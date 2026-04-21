using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using SpaceShooter.Enemies;

public partial class EnemySpawner : Node
{
    [Signal] public delegate void EnemyEscapedEventHandler(int damageToPlayer);

    [Export] private Godot.Collections.Array<PackedScene> _enemyScenes;
    [Export] private Node _spawnParent;
    [Export] private float _spawnIntervalMin = 1.2f;
    [Export] private float _spawnIntervalMax = 2.1f;
    [Export] private float _horizonY = 120f;
    [Export] private int _prewarmPerScene = 6;

    private readonly RandomNumberGenerator _rng = new();
    private readonly Dictionary<PackedScene, Stack<Enemy>> _enemyPoolByScene = new();
    private readonly Dictionary<Enemy, PackedScene> _sceneByEnemy = new();
    private int _enemyNameCounter;
    private Timer _spawnTimer;
    private bool _isSpawningEnabled = true;

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

        CallDeferred(nameof(PrewarmPools));

        ScheduleNextSpawn();
    }

    public override void _ExitTree()
    {
        if (_spawnTimer != null)
        {
            _spawnTimer.Timeout -= OnSpawnTimerTimeout;
            _spawnTimer.Stop();
        }
    }

    private void OnSpawnTimerTimeout()
    {
        if (!_isSpawningEnabled)
        {
            return;
        }

        SpawnEnemy();
        ScheduleNextSpawn();
    }

    private void ScheduleNextSpawn()
    {
        if (!_isSpawningEnabled || _spawnTimer == null)
        {
            return;
        }

        float minInterval = Mathf.Max(0.05f, Math.Min(_spawnIntervalMin, _spawnIntervalMax));
        float maxInterval = Mathf.Max(minInterval, Math.Max(_spawnIntervalMin, _spawnIntervalMax));

        _spawnTimer.WaitTime = _rng.RandfRange(minInterval, maxInterval);
        _spawnTimer.Start();
    }

    public void SetSpawningEnabled(bool isEnabled)
    {
        if (_isSpawningEnabled == isEnabled)
        {
            return;
        }

        _isSpawningEnabled = isEnabled;

        if (!_isSpawningEnabled)
        {
            _spawnTimer?.Stop();
            return;
        }

        if (_spawnTimer != null && _spawnTimer.IsStopped())
        {
            ScheduleNextSpawn();
        }
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

        Node parent = _spawnParent ?? GetParent();
        if (parent == null)
        {
            GD.PushWarning("EnemySpawner: brak parenta dla spawnowanego przeciwnika. Spawn anulowany.");
            return;
        }

        Enemy enemy = AcquireEnemy(enemyScene, parent);
        if (enemy == null)
        {
            return;
        }

        EnemySpawnOrigin spawnOrigin = PickSpawnOrigin();
        EnemyTargetLane targetLane = PickTargetLane(spawnOrigin);
        enemy.ConfigureSpawn(_horizonY, spawnOrigin, targetLane);
    }

    private Enemy AcquireEnemy(PackedScene enemyScene, Node targetParent)
    {
        if (!_enemyPoolByScene.TryGetValue(enemyScene, out Stack<Enemy> scenePool))
        {
            scenePool = new Stack<Enemy>();
            _enemyPoolByScene[enemyScene] = scenePool;
        }

        Enemy enemy = null;
        while (scenePool.Count > 0 && enemy == null)
        {
            Enemy pooled = scenePool.Pop();
            if (GodotObject.IsInstanceValid(pooled))
            {
                enemy = pooled;
            }
        }

        if (enemy == null)
        {
            Node enemyNode = enemyScene.Instantiate();
            if (enemyNode is not Enemy instantiatedEnemy)
            {
                GD.PushWarning("EnemySpawner: scena nie dziedziczy po Enemy.");
                enemyNode.QueueFree();
                return null;
            }

            enemy = instantiatedEnemy;
            enemy.Name = BuildEnemyNodeName(enemyScene);
            enemy.RecycleRequested += OnEnemyRecycleRequested;
            _sceneByEnemy[enemy] = enemyScene;
        }

        if (enemy.GetParent() != targetParent)
        {
            if (enemy.GetParent() == null)
            {
                targetParent.AddChild(enemy);
            }
            else
            {
                enemy.Reparent(targetParent);
            }
        }

        return enemy;
    }

    private string BuildEnemyNodeName(PackedScene scene)
    {
        _enemyNameCounter++;
        string scenePath = scene?.ResourcePath ?? string.Empty;
        string baseName = string.IsNullOrEmpty(scenePath)
            ? nameof(Enemy)
            : Path.GetFileNameWithoutExtension(scenePath);

        if (string.IsNullOrEmpty(baseName))
        {
            baseName = nameof(Enemy);
        }

        return $"PooledEnemy_{baseName}_{_enemyNameCounter}";
    }

    private void ReturnEnemyToPool(Enemy enemy)
    {
        if (enemy == null || !GodotObject.IsInstanceValid(enemy))
        {
            return;
        }

        if (!_sceneByEnemy.TryGetValue(enemy, out PackedScene enemyScene) || enemyScene == null)
        {
            enemy.QueueFree();
            return;
        }

        if (!_enemyPoolByScene.TryGetValue(enemyScene, out Stack<Enemy> scenePool))
        {
            scenePool = new Stack<Enemy>();
            _enemyPoolByScene[enemyScene] = scenePool;
        }

        enemy.DeactivateForPool();
        scenePool.Push(enemy);
    }

    private void OnEnemyRecycleRequested(Enemy enemy, int damageToPlayer)
    {
        if (damageToPlayer > 0)
        {
            OnEnemyEscaped(damageToPlayer);
        }

        ReturnEnemyToPool(enemy);
    }

    private void PrewarmPools()
    {
        if (_enemyScenes == null || _enemyScenes.Count == 0)
        {
            return;
        }

        Node parent = _spawnParent ?? GetParent();
        if (parent == null)
        {
            return;
        }

        int prewarmCount = Math.Max(0, _prewarmPerScene);
        for (int i = 0; i < _enemyScenes.Count; i++)
        {
            PackedScene scene = _enemyScenes[i];
            if (scene == null)
            {
                continue;
            }

            for (int n = 0; n < prewarmCount; n++)
            {
                Enemy enemy = AcquireEnemy(scene, parent);
                if (enemy == null)
                {
                    break;
                }

                ReturnEnemyToPool(enemy);
            }
        }
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
