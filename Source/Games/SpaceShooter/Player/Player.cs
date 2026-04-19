using Godot;
using System;

public partial class Player : Node
{
	[Signal] public delegate void PlayerDiedEventHandler();

	[Export] private HealthComponent _healthComponent;

	public HealthComponent HealthComponent => _healthComponent;

	public override void _Ready()
	{
		_healthComponent ??= GetNodeOrNull<HealthComponent>("HealthComponent");

		if (_healthComponent == null)
		{
			GD.PushWarning("Player: brak HealthComponent.");
			return;
		}

		_healthComponent.Died += OnDied;
	}

	public override void _ExitTree()
	{
		if (_healthComponent != null)
		{
			_healthComponent.Died -= OnDied;
		}
	}

	public void ReceiveDamage(int damage)
	{
		_healthComponent?.ReceiveDamage(damage);
	}

	public int GetCurrentHp()
	{
		return _healthComponent?.CurrentHp ?? 0;
	}

	private void OnDied()
	{
		EmitSignal(SignalName.PlayerDied);
	}
}
