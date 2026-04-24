using Godot;
using System;

namespace SpaceShooter.Screens
{
    public partial class GameScreen : Node
    {
        private const string ScoreManagerScenePath = "res://Source/Games/SpaceShooter/Managers/ScoreManager/ScoreManager.tscn";

        [Export] private EnemySpawner _enemySpawner;
        [Export] private Player _player;
        [Export] private DifficultyManager _difficultyManager;
        private SpaceShooterGameManager _gameManager;
        private ScoreManager _scoreManager;
        private bool _isDefeated;

        public override void _EnterTree()
        {
            EnsureScoreManager();
            EnsureDifficultyManager();
        }

        public override void _Ready()
        {
            _gameManager = SpaceShooterGameManager.GetOrNull(this);
            _scoreManager = ScoreManager.GetOrNull(this);

            if (_gameManager != null)
            {
                _gameManager.ActivateSpaceShooter(_player);
            }

            _scoreManager?.ResetRunState();
            _difficultyManager?.Configure(_enemySpawner);

            if (_enemySpawner == null)
            {
                return;
            }

			_enemySpawner.EnemyDefeated += OnEnemyDefeated;
            _enemySpawner.EnemyEscaped += OnEnemyEscaped;

            if (_player != null)
            {
                _player.PlayerDied += OnPlayerDied;
            }
        }

        public override void _ExitTree()
        {
            if (_enemySpawner != null)
            {
				_enemySpawner.EnemyDefeated -= OnEnemyDefeated;
                _enemySpawner.EnemyEscaped -= OnEnemyEscaped;
            }

            if (_player != null)
            {
                _player.PlayerDied -= OnPlayerDied;
            }

            _gameManager?.DeactivateSpaceShooter(_player);
        }

        private void OnEnemyEscaped(int damageToPlayer)
        {
            if (_isDefeated)
            {
                return;
            }

            if (_player != null)
            {
                _player.ReceiveDamage(damageToPlayer);
            }
        }

        private void OnEnemyDefeated(int scoreValue)
        {
            if (_isDefeated)
            {
                return;
            }

            _scoreManager?.AddScore(scoreValue);
        }

        private void OnPlayerDied()
        {
            EnterDefeatState();
        }

        private void EnterDefeatState()
        {
            if (_isDefeated)
            {
                return;
            }

            _isDefeated = true;
            _enemySpawner?.SetSpawningEnabled(false);
			_scoreManager?.FinalizeRun();
            _gameManager?.EnterDefeatState();
        }

        private void EnsureScoreManager()
        {
            if (ScoreManager.GetOrNull(this) != null)
            {
                return;
            }

            PackedScene scoreManagerScene = ResourceLoader.Load<PackedScene>(ScoreManagerScenePath);
            if (scoreManagerScene == null)
            {
                GD.PushWarning($"GameScreen: nie udalo sie zaladowac {ScoreManagerScenePath}.");
                return;
            }

            if (scoreManagerScene.Instantiate() is not ScoreManager scoreManager)
            {
                GD.PushWarning("GameScreen: ScoreManager scena nie jest typu ScoreManager.");
                return;
            }

            scoreManager.Name = "ScoreManager";
            AddChild(scoreManager);
        }

        private void EnsureDifficultyManager()
        {
            if (_difficultyManager != null)
            {
                return;
            }

            _difficultyManager = GetNodeOrNull<DifficultyManager>("DifficultyManager");
            if (_difficultyManager != null)
            {
                return;
            }

            _difficultyManager = new DifficultyManager
            {
                Name = "DifficultyManager"
            };

            AddChild(_difficultyManager);
        }
    }
}
