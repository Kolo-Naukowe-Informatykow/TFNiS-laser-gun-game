using Godot;
using System;

public partial class HitboxComponent : Area2D
{
	[Export] public HealthComponent HealthComponent { get; set; }

	public override void _Ready()
	{
		CollisionShape2D collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collisionShape == null)
		{
			GD.PushWarning($"{Name}: brak CollisionShape2D. Trafienia nie beda wykrywane.");
		}
		else if (collisionShape.Shape == null)
		{
			CircleShape2D fallbackShape = new CircleShape2D();
			fallbackShape.Radius = EstimateFallbackRadius();
			collisionShape.Shape = fallbackShape;

			GD.PushWarning($"{Name}: CollisionShape2D nie mial Shape. Ustawiono fallback CircleShape2D (radius: {fallbackShape.Radius}).");
		}

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

	private float EstimateFallbackRadius()
	{
		Sprite2D sprite = GetNodeOrNull<Sprite2D>("../Sprite2D");
		if (sprite?.Texture != null)
		{
			Vector2 textureSize = sprite.Texture.GetSize();
			float minDimension = Mathf.Min(textureSize.X, textureSize.Y);
			return Mathf.Max(8f, minDimension * 0.35f);
		}

		return 24f;
	}
}
