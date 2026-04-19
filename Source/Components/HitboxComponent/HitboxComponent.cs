using Godot;
using System;

public partial class HitboxComponent : Area2D
{
	[Export] public HealthComponent HealthComponent { get; set; }

	public override void _Ready()
	{
		if (HealthComponent != null)
		{
			return;
		}

		HealthComponent = GetNodeOrNull<HealthComponent>("../HealthComponent");
	}

	public void ReceiveDamage(int damage)
	{
		if (HealthComponent == null)
		{
			GD.PushWarning($"{Name}: HealthComponent is not assigned. Damage ignored.");
			return;
		}

		HealthComponent.ReceiveDamage(damage);
	}
}
