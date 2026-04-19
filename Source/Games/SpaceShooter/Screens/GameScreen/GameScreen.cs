using Godot;
using System;

namespace SpaceShooter.Screens
{
    public partial class GameScreen : Node
    {
        [Export] private EnemySpawner _enemySpawner;
        [Export] private Player _player;
        private HealthComponent _playerHealth;

        public override void _Ready()
        {
            if (_enemySpawner == null)
            {
                return;
            }
            if (_player != null)
            {
                _playerHealth = _player.HealthComponent;
            }
            _enemySpawner.EnemyEscaped += OnEnemyEscaped;
        }

        private void OnEnemyEscaped(int damageToPlayer)
        {
            if (_player != null)
            {
                _player.ReceiveDamage(damageToPlayer);
                return;
            }

            _playerHealth?.ReceiveDamage(damageToPlayer);
        }
    }
}
