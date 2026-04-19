using Godot;
using System;

public partial class ParticlesComponent : Node
{
	[Export] private int _amount = 36;
	[Export] private float _lifetime = 0.9f;
	[Export] private float _initialVelocity = 190f;
	[Export] private float _spread = 180f;
	[Export] private float _scale = 2.4f;
	[Export] private float _fadeOutDuration = 0.65f;
	[Export] private float _lifetimeRandomness = 0.45f;
	[Export] private float _fadeStartRatio = 0.65f;
	[Export] private Color _particleColor = new Color(1f, 0.85f, 0.65f, 1f);
	[Export] private Vector2 _gravity = new Vector2(0f, 220f);

	[Export] private bool _useGeneratedTexture = true;
	[Export] private int _generatedTextureSize = 40;
	[Export] private Texture2D _particleTexture;

	private Texture2D _cachedGeneratedTexture;
	private int _cachedTextureSize;
	private Color _cachedTextureColor;
	private GradientTexture1D _cachedFadeRamp;

	public void EmitAtOwner()
	{
		if (GetParent() is Node2D parent2D)
		{
			EmitAt(parent2D.GlobalPosition);
			return;
		}

		GD.PushWarning($"{Name}: EmitAtOwner wymaga parenta typu Node2D.");
	}

	public void EmitAt(Vector2 worldPosition, Node parentOverride = null)
	{
		Node targetParent = parentOverride ?? GetParent();
		if (targetParent == null)
		{
			return;
		}

		GpuParticles2D particles = new GpuParticles2D();
		particles.GlobalPosition = worldPosition;
		particles.Amount = Math.Max(1, _amount);
		particles.OneShot = true;
		particles.Emitting = false;
		particles.Explosiveness = 1.0f;
		particles.LocalCoords = false;
		particles.Modulate = Colors.White;
		particles.Texture = GetOrCreateTexture();

		ParticleProcessMaterial material = new ParticleProcessMaterial();
		material.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere;
		material.Spread = Mathf.Clamp(_spread, 0f, 360f);
		material.InitialVelocityMin = Mathf.Max(0f, _initialVelocity * 0.6f);
		material.InitialVelocityMax = Mathf.Max(0f, _initialVelocity);
		material.ScaleMin = Mathf.Max(0.05f, _scale * 0.6f);
		material.ScaleMax = Mathf.Max(0.05f, _scale);
		material.Gravity = new Vector3(_gravity.X, _gravity.Y, 0f);
		material.Set("lifetime_randomness", Mathf.Clamp(_lifetimeRandomness, 0f, 1f));
		material.Set("color_ramp", GetOrCreateFadeRamp());
		particles.ProcessMaterial = material;

		particles.Lifetime = Mathf.Max(0.05f, _lifetime);
		targetParent.AddChild(particles);

		particles.Emitting = true;

		float fadeDuration = Mathf.Clamp(_fadeOutDuration, 0.05f, (float)particles.Lifetime);
		Tween fadeTween = particles.CreateTween();
		fadeTween.TweenInterval(Mathf.Max(0.01f, particles.Lifetime - fadeDuration));
		fadeTween.TweenProperty(particles, "modulate", new Color(1f, 1f, 1f, 0f), fadeDuration);

		SceneTreeTimer cleanupTimer = GetTree().CreateTimer(particles.Lifetime + fadeDuration + 0.25f);
		cleanupTimer.Timeout += () =>
		{
			if (GodotObject.IsInstanceValid(particles))
			{
				particles.QueueFree();
			}
		};
	}

	private Texture2D GetOrCreateTexture()
	{
		if (!_useGeneratedTexture)
		{
			return _particleTexture;
		}

		if (_cachedGeneratedTexture != null
			&& _cachedTextureSize == _generatedTextureSize
			&& _cachedTextureColor == _particleColor)
		{
			return _cachedGeneratedTexture;
		}

		_cachedGeneratedTexture = CreateGeneratedTexture();
		_cachedTextureSize = _generatedTextureSize;
		_cachedTextureColor = _particleColor;
		return _cachedGeneratedTexture;
	}

	private Texture2D CreateGeneratedTexture()
	{
		int size = Math.Max(8, _generatedTextureSize);
		float radius = (size - 1) * 0.5f;
		Vector2 center = new Vector2(radius, radius);

		Image image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
		image.Fill(new Color(0f, 0f, 0f, 0f));

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float dist = center.DistanceTo(new Vector2(x, y));
				if (dist > radius)
				{
					continue;
				}

				float normalized = 1f - (dist / radius);
				float alpha = Mathf.Pow(normalized, 1.8f);
				Color c = _particleColor;
				c.A *= alpha;
				image.SetPixel(x, y, c);
			}
		}

		return ImageTexture.CreateFromImage(image);
	}

	private GradientTexture1D GetOrCreateFadeRamp()
	{
		if (_cachedFadeRamp != null)
		{
			return _cachedFadeRamp;
		}

		Gradient gradient = new Gradient();
		float fadeStart = Mathf.Clamp(_fadeStartRatio, 0f, 0.99f);
		gradient.AddPoint(0.0f, new Color(1f, 1f, 1f, 1f));
		gradient.AddPoint(fadeStart, new Color(1f, 1f, 1f, 0.9f));
		gradient.AddPoint(1.0f, new Color(1f, 1f, 1f, 0f));

		_cachedFadeRamp = new GradientTexture1D();
		_cachedFadeRamp.Gradient = gradient;
		return _cachedFadeRamp;
	}
}
