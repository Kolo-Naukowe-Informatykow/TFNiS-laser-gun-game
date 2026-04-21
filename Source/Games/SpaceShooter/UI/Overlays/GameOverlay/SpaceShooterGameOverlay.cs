using Godot;
using System;

public partial class SpaceShooterGameOverlay : Control
{
	private const string DefeatOverlayScenePath = "res://Source/Games/SpaceShooter/UI/Overlays/DefeatOverlay/DefeatOverlay.tscn";

	private SpaceShooterGameManager _gameManager;
	private HealthComponent _trackedHealth;
	private int _lastKnownHp = -1;
	private ColorRect _damageFlashRect;
	private Tween _damageFlashTween;
	private Control _defeatOverlay;

	public override void _Ready()
	{
		CreateDamageFlashRect();
		CreateDefeatOverlay();

		_gameManager = SpaceShooterGameManager.GetOrNull(this);
		if (_gameManager == null)
		{
			GD.PushWarning("SpaceShooterGameOverlay: nie znaleziono SpaceShooterGameManager.");
			return;
		}

		_gameManager.PlayerChanged += OnPlayerChanged;
		_gameManager.DefeatStateChanged += OnDefeatStateChanged;
		_gameManager.SpaceShooterActiveChanged += OnSpaceShooterActiveChanged;

		AttachToPlayer(_gameManager.Player);
		ApplyOverlayState(_gameManager.IsDefeated);
	}

	public override void _ExitTree()
	{
		DetachFromPlayer();
		_damageFlashTween?.Kill();

		if (_gameManager != null)
		{
			_gameManager.PlayerChanged -= OnPlayerChanged;
			_gameManager.DefeatStateChanged -= OnDefeatStateChanged;
			_gameManager.SpaceShooterActiveChanged -= OnSpaceShooterActiveChanged;
		}
	}

	private void CreateDamageFlashRect()
	{
		_damageFlashRect = new ColorRect
		{
			Name = "DamageFlash",
			Color = new Color(1f, 0f, 0f, 0f),
			MouseFilter = MouseFilterEnum.Ignore,
			ZIndex = 40,
			Visible = true
		};

		_damageFlashRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(_damageFlashRect);
	}

	private void CreateDefeatOverlay()
	{
		Node parent = GetParent();
		if (parent == null)
		{
			return;
		}

		PackedScene defeatOverlayScene = ResourceLoader.Load<PackedScene>(DefeatOverlayScenePath);
		if (defeatOverlayScene == null)
		{
			GD.PushWarning($"SpaceShooterGameOverlay: nie udalo sie zaladowac {DefeatOverlayScenePath}.");
			return;
		}

		if (defeatOverlayScene.Instantiate() is not Control defeatOverlay)
		{
			GD.PushWarning("SpaceShooterGameOverlay: DefeatOverlay scena nie jest typu Control.");
			return;
		}

		defeatOverlay.Name = "DefeatOverlayRuntime";
		defeatOverlay.ZIndex = 100;
		defeatOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		defeatOverlay.Visible = false;
		parent.CallDeferred(Node.MethodName.AddChild, defeatOverlay);
		_defeatOverlay = defeatOverlay;
	}

	private void OnPlayerChanged(Player player)
	{
		AttachToPlayer(player);
	}

	private void OnDefeatStateChanged(bool isDefeated)
	{
		ApplyOverlayState(isDefeated);
	}

	private void OnSpaceShooterActiveChanged(bool isActive)
	{
		if (isActive)
		{
			return;
		}

		ApplyOverlayState(false);
	}

	private void AttachToPlayer(Player player)
	{
		DetachFromPlayer();
		_trackedHealth = player?.HealthComponent;

		if (_trackedHealth == null)
		{
			_lastKnownHp = -1;
			return;
		}

		_lastKnownHp = _trackedHealth.CurrentHp;
		_trackedHealth.HealthChanged += OnPlayerHealthChanged;
	}

	private void DetachFromPlayer()
	{
		if (_trackedHealth != null)
		{
			_trackedHealth.HealthChanged -= OnPlayerHealthChanged;
			_trackedHealth = null;
		}
	}

	private void OnPlayerHealthChanged(int currentHp, int maxHp)
	{
		if (_lastKnownHp >= 0 && currentHp < _lastKnownHp)
		{
			PlayDamageFlash();
		}

		_lastKnownHp = currentHp;
	}

	private void PlayDamageFlash()
	{
		if (_damageFlashRect == null)
		{
			return;
		}

		if (_gameManager?.IsDefeated ?? false)
		{
			return;
		}

		_damageFlashTween?.Kill();
		_damageFlashRect.Color = new Color(1f, 0f, 0f, 0.42f);

		_damageFlashTween = CreateTween();
		_damageFlashTween.TweenProperty(_damageFlashRect, "color", new Color(1f, 0f, 0f, 0f), 0.28f);
	}

	private void ApplyOverlayState(bool isDefeated)
	{
		Visible = !isDefeated;

		if (_defeatOverlay != null)
		{
			_defeatOverlay.Visible = isDefeated;
		}

		if (isDefeated && _damageFlashRect != null)
		{
			_damageFlashTween?.Kill();
			_damageFlashRect.Color = new Color(1f, 0f, 0f, 0f);
		}
	}
}
