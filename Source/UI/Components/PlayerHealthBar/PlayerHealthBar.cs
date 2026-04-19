using Godot;
using System;

public partial class PlayerHealthBar : Control
{
    [Export] private ProgressBar _healthBar;

    private SpaceShooterGameManager _gameManager;
    private GlobalGameManager _globalGameManager;
    private HealthComponent _trackedHealth;

    public override void _Ready()
    {
        if (_healthBar == null)
        {
            _healthBar = GetNodeOrNull<ProgressBar>("ProgressBar");
        }

        _gameManager = SpaceShooterGameManager.GetOrNull(this);
        if (_gameManager == null)
        {
            GD.PushWarning("PlayerHealthBar: nie znaleziono SpaceShooterGameManager (oczekiwany node: CurrentScene/SpaceShooterGameManager).");
            return;
        }

        _globalGameManager = GlobalGameManager.Instance;
        if (_globalGameManager != null)
        {
            _globalGameManager.CurrentGameChanged += OnCurrentGameChanged;
        }

        _gameManager.PlayerChanged += OnPlayerChanged;
        _gameManager.SpaceShooterActiveChanged += OnSpaceShooterActiveChanged;

        UpdateVisibilityFromGlobalState();
        AttachToPlayer(_gameManager.Player);
    }

    public override void _ExitTree()
    {
        DetachFromHealth();

        if (_gameManager != null)
        {
            _gameManager.PlayerChanged -= OnPlayerChanged;
            _gameManager.SpaceShooterActiveChanged -= OnSpaceShooterActiveChanged;
        }

        if (_globalGameManager != null)
        {
            _globalGameManager.CurrentGameChanged -= OnCurrentGameChanged;
        }
    }

    private void OnPlayerChanged(Player player)
    {
        AttachToPlayer(player);
    }

    private void OnSpaceShooterActiveChanged(bool isActive)
    {
        UpdateVisibilityFromGlobalState();

        if (!isActive)
        {
            AttachToPlayer(null);
        }
    }

    private void OnCurrentGameChanged(int game)
    {
        UpdateVisibilityFromGlobalState();

        if ((GlobalGameManager.Games)game != GlobalGameManager.Games.SpaceShooter)
        {
            AttachToPlayer(null);
        }
    }

    private void AttachToPlayer(Player player)
    {
        DetachFromHealth();

        _trackedHealth = player?.HealthComponent;
        if (_trackedHealth == null)
        {
            ResetBar();
            return;
        }

        _trackedHealth.HealthChanged += OnHealthChanged;
        ApplyHealth(_trackedHealth.CurrentHp, _trackedHealth.MaxHp);
    }

    private void DetachFromHealth()
    {
        if (_trackedHealth != null)
        {
            _trackedHealth.HealthChanged -= OnHealthChanged;
            _trackedHealth = null;
        }
    }

    private void OnHealthChanged(int currentHp, int maxHp)
    {
        ApplyHealth(currentHp, maxHp);
    }

    private void ApplyHealth(int currentHp, int maxHp)
    {
        if (_healthBar == null)
        {
            return;
        }

        _healthBar.MaxValue = Math.Max(1, maxHp);
        _healthBar.Value = Mathf.Clamp(currentHp, 0, maxHp);
    }

    private void ResetBar()
    {
        if (_healthBar == null)
        {
            return;
        }

        _healthBar.MaxValue = 1;
        _healthBar.Value = 0;
    }

    private void UpdateVisibilityFromGlobalState()
    {
        bool isSpaceShooterSelected = _globalGameManager?.IsCurrentGame(GlobalGameManager.Games.SpaceShooter) ?? true;
        bool isSpaceShooterActive = _gameManager?.IsSpaceShooterActive ?? false;
        Visible = isSpaceShooterSelected && isSpaceShooterActive;
    }
}
