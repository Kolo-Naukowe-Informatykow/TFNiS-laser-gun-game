using Godot;
using System;

namespace SpaceShooter.Enemies
{
	public enum EnemyFlightPattern
	{
		Forward = 0,
		Slalom = 1,
		EaseInOut = 2,
		PauseBurst = 3
	}

	public enum EnemySpawnOrigin
	{
		TopLeft = 0,
		TopCenter = 1,
		TopRight = 2
	}

	public enum EnemyTargetLane
	{
		BottomLeft = 0,
		BottomCenter = 1,
		BottomRight = 2
	}

	public partial class Enemy : Node2D
	{
		[Signal] public delegate void EscapedEventHandler(int damageToPlayer);
		[Signal] public delegate void RecycleRequestedEventHandler(Enemy enemy, int damageToPlayer);

		private readonly RandomNumberGenerator _rng = new();
		private Tween _damageFlashTween;
		private Tween _hitShakeTween;
		private Sprite2D _sprite;
		private HealthComponent _healthComponent;
		[Export] private ParticlesComponent _particlesComponent;
		private Color _baseSpriteColor = Colors.White;
		private Vector2 _baseSpriteLocalPosition = Vector2.Zero;
		private bool _isDying;
		private bool _isActive;

		[Export] private float _minScale = 0.06f;
		[Export] private float _maxScale = 1.0f;
		[Export] private Curve _scaleCurve;
		[Export] private float _depthSpeed = 0.1f;
		[Export] private float _horizontalExpansion = 2.4f;
		[Export] private int _damageOnEscape = 10;
		[Export] private float _endYMargin = 120f;
		[Export] private float _slalomFrequency = 2.3f;
		[Export] private float _slalomAmplitude = 0.22f;
		[Export] private float _pauseAtProgress = 0.35f;
		[Export] private float _pauseDuration = 0.45f;
		[Export] private float _burstMultiplier = 1.8f;
		[Export] private Color _damageFlashColor = new Color(1f, 0.22f, 0.22f, 1f);
		[Export] private float _damageFlashInDuration = 0.06f;
		[Export] private float _damageFlashOutDuration = 0.16f;
		[Export] private float _hitShakeDuration = 0.16f;
		[Export] private float _hitShakeStrength = 16.0f;
		[Export] private int _hitShakeSteps = 12;

		[Export] private EnemyFlightPattern _flightPattern = EnemyFlightPattern.Forward;
		private Vector2 _startPoint;
		private Vector2 _endPoint;
		private float _pausePointX;
		private float _pausePointProgress;
		private EnemySpawnOrigin _spawnOrigin = EnemySpawnOrigin.TopCenter;
		private EnemyTargetLane _targetLane = EnemyTargetLane.BottomCenter;
		private float _depthProgress;
		private bool _pauseConsumed;
		private float _pauseTimeLeft;

		public override void _Ready()
		{
			if (GlobalGameManager.Instance != null)
			{
				GlobalGameManager.Instance.SeedRngUnique(_rng, nameof(Enemy));
			}
			else
			{
				_rng.Randomize();
			}

			_sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
			if (_sprite != null)
			{
				_baseSpriteColor = _sprite.Modulate;
				_baseSpriteLocalPosition = _sprite.Position;
			}

			_healthComponent = GetNodeOrNull<HealthComponent>("HealthComponent");
			if (_healthComponent != null)
			{
				_healthComponent.HealthChanged += OnHealthChanged;
			}

			_particlesComponent ??= GetNodeOrNull<ParticlesComponent>("ParticlesComponent");

			Scale = Vector2.One * Mathf.Max(0.01f, _minScale);
		}

		public override void _ExitTree()
		{
			if (_healthComponent != null)
			{
				_healthComponent.HealthChanged -= OnHealthChanged;
			}
		}

		public override void _Process(double delta)
		{
			if (!_isActive)
			{
				return;
			}

			if (_depthProgress >= 1f)
			{
				return;
			}

			float deltaF = (float)delta;
			float step = GetPatternDepthStep(deltaF);
			if (step > 0f)
			{
				_depthProgress = Mathf.Min(1f, _depthProgress + step);
			}

			Vector2 viewportSize = GetViewportRect().Size;
			float endY = viewportSize.Y + _endYMargin;
			float currentY = Mathf.Lerp(_startPoint.Y, endY, _depthProgress);
			float currentX = GetPatternX(viewportSize, _depthProgress);

			GlobalPosition = new Vector2(currentX, currentY);
			float currentScale = Mathf.Lerp(_minScale, _maxScale, EvaluateScaleProgress(_depthProgress));
			Scale = Vector2.One * Mathf.Max(0.01f, currentScale);

			if (_depthProgress >= 1f)
			{
				RequestRecycle(_damageOnEscape);
			}
		}

		public void ConfigureSpawn(float startY, EnemySpawnOrigin spawnOrigin, EnemyTargetLane targetLane)
		{
			_isActive = true;
			_isDying = false;
			Visible = true;
			SetProcess(true);
			SetHitboxesEnabled(true);
			_damageFlashTween?.Kill();
			_hitShakeTween?.Kill();

			if (_sprite != null)
			{
				_sprite.Modulate = _baseSpriteColor;
				_sprite.Position = _baseSpriteLocalPosition;
			}

			_healthComponent?.ResetHealth();

			_depthProgress = 0f;
			_pauseConsumed = false;
			_pauseTimeLeft = 0f;
			_spawnOrigin = spawnOrigin;
			_targetLane = targetLane;
			_pausePointProgress = Mathf.Clamp(_pauseAtProgress + _rng.RandfRange(-0.12f, 0.12f), 0.35f, 0.65f);

			Vector2 viewportSize = GetViewportRect().Size;
			_startPoint = new Vector2(GetSpawnOriginX(viewportSize), startY);
			_endPoint = new Vector2(GetTargetLaneX(viewportSize), viewportSize.Y + _endYMargin);
			_pausePointX = GetPausePointX(viewportSize);
			GlobalPosition = _startPoint;
			float initialScale = Mathf.Lerp(_minScale, _maxScale, EvaluateScaleProgress(0f));
			Scale = Vector2.One * Mathf.Max(0.01f, initialScale);
		}

		public void HandleDeath()
		{
			if (_isDying || !_isActive)
			{
				return;
			}

			_isDying = true;
			Node particleParent = GetParent() ?? GetTree()?.CurrentScene;
			_particlesComponent?.EmitAt(GlobalPosition, particleParent);
			RequestRecycle(0);
		}

		public void DeactivateForPool()
		{
			_isActive = false;
			_isDying = false;
			SetProcess(false);
			Visible = false;
			SetHitboxesEnabled(false);
			_damageFlashTween?.Kill();
			_hitShakeTween?.Kill();

			if (_sprite != null)
			{
				_sprite.Modulate = _baseSpriteColor;
				_sprite.Position = _baseSpriteLocalPosition;
			}
		}

		private void OnHealthChanged(int currentHp, int maxHp)
		{
			if (_sprite == null || _isDying)
			{
				return;
			}

			_damageFlashTween?.Kill();
			_damageFlashTween = CreateTween();
			_damageFlashTween.TweenProperty(_sprite, "modulate", _damageFlashColor, Mathf.Max(0.01f, _damageFlashInDuration));
			_damageFlashTween.TweenProperty(_sprite, "modulate", _baseSpriteColor, Mathf.Max(0.01f, _damageFlashOutDuration));

			if (currentHp > 0)
			{
				PlayHitShake();
			}
		}


		private void PlayHitShake()
		{
			if (_sprite == null)
			{
				return;
			}

			_hitShakeTween?.Kill();
			_sprite.Position = _baseSpriteLocalPosition;

			int steps = Math.Max(1, _hitShakeSteps);
			float totalDuration = Mathf.Max(0.01f, _hitShakeDuration);
			float stepDuration = totalDuration / (steps + 1f);

			_hitShakeTween = CreateTween();
			for (int i = 0; i < steps; i++)
			{
				Vector2 offset = new Vector2(
					_rng.RandfRange(-_hitShakeStrength, _hitShakeStrength),
					_rng.RandfRange(-_hitShakeStrength, _hitShakeStrength)
				);

				_hitShakeTween.TweenProperty(_sprite, "position", _baseSpriteLocalPosition + offset, stepDuration);
			}

			_hitShakeTween.TweenProperty(_sprite, "position", _baseSpriteLocalPosition, stepDuration);
		}

		private float EvaluateScaleProgress(float progress)
		{
			float clamped = Mathf.Clamp(progress, 0f, 1f);
			if (_scaleCurve == null)
			{
				return clamped;
			}

			return Mathf.Clamp(_scaleCurve.SampleBaked(clamped), 0f, 1f);
		}

		private float GetPatternDepthStep(float delta)
		{
			float baseSpeed = Mathf.Max(0.01f, _depthSpeed);

			switch (_flightPattern)
			{
				case EnemyFlightPattern.EaseInOut:
					return delta * baseSpeed;
				case EnemyFlightPattern.PauseBurst:
				{
					if (!_pauseConsumed && _depthProgress >= _pausePointProgress)
					{
						_pauseConsumed = true;
						_pauseTimeLeft = Mathf.Max(0f, _pauseDuration);
					}

					if (_pauseTimeLeft > 0f)
					{
						_pauseTimeLeft = Mathf.Max(0f, _pauseTimeLeft - delta);
						return 0f;
					}

					return delta * baseSpeed * Mathf.Max(1f, _burstMultiplier);
				}
				default:
					return delta * baseSpeed;
			}
		}

		private float GetPatternX(Vector2 viewportSize, float progress)
		{
			float startX = _startPoint.X;
			float endX = _endPoint.X;
			float pathProgress = _flightPattern == EnemyFlightPattern.EaseInOut
				? Mathf.SmoothStep(0f, 1f, progress)
				: progress;

			float pathX = Mathf.Lerp(startX, endX, pathProgress);

			switch (_flightPattern)
			{
				case EnemyFlightPattern.Slalom:
				{
					float baseX = pathX;
					float wave = Mathf.Sin(progress * Mathf.Pi * 2f * Mathf.Max(0.1f, _slalomFrequency));
					return baseX + (wave * viewportSize.X * Mathf.Max(0f, _slalomAmplitude));
				}
				case EnemyFlightPattern.PauseBurst:
				{
					if (progress <= _pausePointProgress)
					{
						float segmentProgress = Mathf.InverseLerp(0f, _pausePointProgress, progress);
						return Mathf.Lerp(startX, _pausePointX, segmentProgress);
					}

					float resumeProgress = Mathf.InverseLerp(_pausePointProgress, 1f, progress);
					return Mathf.Lerp(_pausePointX, endX, resumeProgress);
				}
				case EnemyFlightPattern.EaseInOut:
				case EnemyFlightPattern.Forward:
				default:
				{
					return pathX;
				}
			}
		}

		private float GetSpawnOriginX(Vector2 viewportSize)
		{
			float sideMargin = viewportSize.X * 0.1f;
			float leftX = sideMargin;
			float centerX = viewportSize.X * 0.5f;
			float rightX = viewportSize.X - sideMargin;

			switch (_spawnOrigin)
			{
				case EnemySpawnOrigin.TopLeft:
					return leftX;
				case EnemySpawnOrigin.TopRight:
					return rightX;
				default:
					return centerX;
			}
		}

		private float GetTargetLaneX(Vector2 viewportSize)
		{
			float sideMargin = viewportSize.X * 0.1f;
			float leftX = sideMargin;
			float centerX = viewportSize.X * 0.5f;
			float rightX = viewportSize.X - sideMargin;

			switch (_targetLane)
			{
				case EnemyTargetLane.BottomLeft:
					return leftX;
				case EnemyTargetLane.BottomRight:
					return rightX;
				default:
					return centerX;
			}
		}

		private float GetPausePointX(Vector2 viewportSize)
		{
			int laneIndex = _rng.RandiRange(0, 2);
			return laneIndex switch
			{
				0 => GetTargetLaneX(viewportSize, EnemyTargetLane.BottomLeft),
				1 => GetTargetLaneX(viewportSize, EnemyTargetLane.BottomCenter),
				_ => GetTargetLaneX(viewportSize, EnemyTargetLane.BottomRight),
			};
		}

		private float GetTargetLaneX(Vector2 viewportSize, EnemyTargetLane lane)
		{
			float sideMargin = viewportSize.X * 0.1f;
			float leftX = sideMargin;
			float centerX = viewportSize.X * 0.5f;
			float rightX = viewportSize.X - sideMargin;

			switch (lane)
			{
				case EnemyTargetLane.BottomLeft:
					return leftX;
				case EnemyTargetLane.BottomRight:
					return rightX;
				default:
					return centerX;
			}
		}

		private void RequestRecycle(int damageToPlayer)
		{
			if (!_isActive)
			{
				return;
			}

			if (damageToPlayer > 0)
			{
				EmitSignal(SignalName.Escaped, damageToPlayer);
			}

			EmitSignal(SignalName.RecycleRequested, this, damageToPlayer);
		}

		private void SetHitboxesEnabled(bool enabled)
		{
			SetHitboxesEnabledRecursive(this, enabled);
		}

		private void SetHitboxesEnabledRecursive(Node node, bool enabled)
		{
			foreach (Node child in node.GetChildren())
			{
				if (child is Area2D area)
				{
					area.Monitoring = enabled;
					area.Monitorable = enabled;
				}

				if (child is CollisionShape2D collisionShape)
				{
					collisionShape.Disabled = !enabled;
				}

				SetHitboxesEnabledRecursive(child, enabled);
			}
		}
	}
}
