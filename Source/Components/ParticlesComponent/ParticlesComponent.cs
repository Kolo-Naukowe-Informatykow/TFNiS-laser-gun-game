using Godot;
using System;
using System.Collections.Generic;

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
	[Export] private int _prewarmCount = 2;

	private Texture2D _cachedGeneratedTexture;
	private int _cachedTextureSize;
	private Color _cachedTextureColor;
	private GradientTexture1D _cachedFadeRamp;
	private ParticleProcessMaterial _cachedProcessMaterial;
	private readonly Queue<GpuParticles2D> _particlePool = new();
	private Node _poolParent;
	private int _particleNameCounter;

	public override void _Ready()
	{
		_poolParent = GetTree()?.CurrentScene ?? GetParent();

		for (int i = 0; i < Math.Max(0, _prewarmCount); i++)
		{
			ReturnToPool(CreateParticleNode());
		}
	}

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

		GpuParticles2D particles = AcquireParticle(targetParent);
		particles.GlobalPosition = worldPosition;
		particles.Amount = Math.Max(1, _amount);
		particles.OneShot = true;
		particles.Emitting = false;
		particles.Explosiveness = 1.0f;
		particles.LocalCoords = false;
		particles.Visible = true;
		particles.Modulate = Colors.White;
		particles.Texture = GetOrCreateTexture();
		particles.ProcessMaterial = GetOrCreateProcessMaterial();

		particles.Lifetime = Mathf.Max(0.05f, _lifetime);

		particles.Emitting = true;

		float fadeDuration = Mathf.Clamp(_fadeOutDuration, 0.05f, (float)particles.Lifetime);
		Tween fadeTween = particles.CreateTween();
		fadeTween.TweenInterval(Mathf.Max(0.01f, particles.Lifetime - fadeDuration));
		fadeTween.TweenProperty(particles, "modulate", new Color(1f, 1f, 1f, 0f), fadeDuration);

		Timer cleanupTimer = particles.GetNode<Timer>("ReturnTimer");
		cleanupTimer.Stop();
		cleanupTimer.WaitTime = particles.Lifetime + fadeDuration + 0.25f;
		cleanupTimer.Start();
	}

	private GpuParticles2D AcquireParticle(Node targetParent)
	{
		GpuParticles2D particles = _particlePool.Count > 0 ? _particlePool.Dequeue() : CreateParticleNode();

		if (particles.GetParent() != targetParent)
		{
			particles.Reparent(targetParent);
		}

		return particles;
	}

	private void ReturnToPool(GpuParticles2D particles)
	{
		if (!GodotObject.IsInstanceValid(particles))
		{
			return;
		}

		Timer cleanupTimer = particles.GetNodeOrNull<Timer>("ReturnTimer");
		cleanupTimer?.Stop();
		particles.Emitting = false;
		particles.Visible = false;
		particles.Modulate = Colors.White;

		Node targetPoolParent = _poolParent ?? GetTree()?.CurrentScene ?? GetParent();
		if (targetPoolParent != null && particles.GetParent() != targetPoolParent)
		{
			particles.Reparent(targetPoolParent);
		}

		_particlePool.Enqueue(particles);
	}

	private GpuParticles2D CreateParticleNode()
	{
		_particleNameCounter++;
		GpuParticles2D particles = new GpuParticles2D
		{
			Name = $"PooledGpuParticles2D_{_particleNameCounter}",
			OneShot = true,
			Emitting = false,
			Explosiveness = 1.0f,
			LocalCoords = false,
			Visible = false
		};

		Timer returnTimer = new Timer
		{
			Name = "ReturnTimer",
			OneShot = true,
			Autostart = false
		};

		particles.AddChild(returnTimer);
		returnTimer.Timeout += () => ReturnToPool(particles);

		Node targetPoolParent = _poolParent ?? GetTree()?.CurrentScene ?? GetParent();
		targetPoolParent?.AddChild(particles);
		return particles;
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

	private ParticleProcessMaterial GetOrCreateProcessMaterial()
	{
		if (_cachedProcessMaterial == null)
		{
			_cachedProcessMaterial = new ParticleProcessMaterial();
		}

		_cachedProcessMaterial.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere;
		_cachedProcessMaterial.Spread = Mathf.Clamp(_spread, 0f, 360f);
		_cachedProcessMaterial.InitialVelocityMin = Mathf.Max(0f, _initialVelocity * 0.6f);
		_cachedProcessMaterial.InitialVelocityMax = Mathf.Max(0f, _initialVelocity);
		_cachedProcessMaterial.ScaleMin = Mathf.Max(0.05f, _scale * 0.6f);
		_cachedProcessMaterial.ScaleMax = Mathf.Max(0.05f, _scale);
		_cachedProcessMaterial.Gravity = new Vector3(_gravity.X, _gravity.Y, 0f);
		_cachedProcessMaterial.Set("lifetime_randomness", Mathf.Clamp(_lifetimeRandomness, 0f, 1f));
		_cachedProcessMaterial.Set("color_ramp", GetOrCreateFadeRamp());

		return _cachedProcessMaterial;
	}
}
