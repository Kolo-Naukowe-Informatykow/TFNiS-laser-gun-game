using Godot;
using System;

public partial class HealthComponent : Node
{
	[Signal] public delegate void HealthChangedEventHandler(int currentHp, int maxHp);

	[Signal] public delegate void DiedEventHandler();

	[Export] public int MaxHp = 100;

	public int CurrentHp { get; private set; }

	private bool _isDead;

	public override void _Ready()
	{
		MaxHp = Math.Max(1, MaxHp);
		ResetHealth();
	}

	public void ResetHealth()
	{
		MaxHp = Math.Max(1, MaxHp);
		CurrentHp = MaxHp;
		_isDead = false;
		EmitSignal(SignalName.HealthChanged, CurrentHp, MaxHp);
	}

	public void ReceiveDamage(int damage)
	{
		if (_isDead || damage <= 0)
		{
			return;
		}

		CurrentHp = Math.Max(0, CurrentHp - damage);
		EmitSignal(SignalName.HealthChanged, CurrentHp, MaxHp);

		if (CurrentHp > 0)
		{
			return;
		}

		_isDead = true;
		EmitSignal(SignalName.Died);
		CallParentHandleDeathIfExists();
	}

	private void CallParentHandleDeathIfExists()
	{
		Node parent = GetParent();
		if (parent == null)
		{
			return;
		}

		if (parent.HasMethod("HandleDeath"))
		{
			parent.CallDeferred("HandleDeath");
		}
	}
}
