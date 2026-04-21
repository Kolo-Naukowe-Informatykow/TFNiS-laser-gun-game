using Godot;
using System;

namespace SpaceShooter.Screens
{
    public partial class GameScreen : Node
    {
        [Export] private EnemySpawner _enemySpawner;
        [Export] private Player _player;
        private SpaceShooterGameManager _gameManager;
        private bool _isDefeated;

        public override void _Ready()
        {
            _gameManager = SpaceShooterGameManager.GetOrNull(this);

            if (_gameManager != null)
            {
                _gameManager.ActivateSpaceShooter(_player);
            }

            if (_enemySpawner == null)
            {
                return;
            }

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
            _gameManager?.EnterDefeatState();
        }
    }
}
