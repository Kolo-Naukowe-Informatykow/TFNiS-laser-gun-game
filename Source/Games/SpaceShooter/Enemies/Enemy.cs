using Godot;
using System;

namespace SpaceShooter.Enemies
{
	public enum EnemyFlightPattern
	{
		Forward = 0,
		LeftToRight = 1,
		RightToLeft = 2,
		Slalom = 3,
		EaseInOut = 4,
		PauseBurst = 5
	}

	public partial class Enemy : Node2D
	{
		[Signal] public delegate void EscapedEventHandler(int damageToPlayer);

		[Export] private float _minScale = 0.2f;
		[Export] private float _maxScale = 1.2f;
		[Export] private float _depthSpeed = 0.18f;
		[Export] private float _horizontalExpansion = 2.4f;
		[Export] private int _damageOnEscape = 10;
		[Export] private float _endYMargin = 120f;
		[Export] private float _slalomFrequency = 2.3f;
		[Export] private float _slalomAmplitude = 0.22f;
		[Export] private float _pauseAtProgress = 0.55f;
		[Export] private float _pauseDuration = 0.45f;
		[Export] private float _burstMultiplier = 1.8f;

		[Export] private EnemyFlightPattern _flightPattern = EnemyFlightPattern.Forward;
		private Vector2 _startPoint;
		private float _normalizedHorizontalOffset;
		private float _depthProgress;
		private bool _pauseConsumed;
		private float _pauseTimeLeft;

		public override void _Ready()
		{
			Scale = Vector2.One * Mathf.Max(0.01f, _minScale);
		}

		public override void _Process(double delta)
		{
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
			float currentScale = Mathf.Lerp(_minScale, _maxScale, _depthProgress);
			Scale = Vector2.One * Mathf.Max(0.01f, currentScale);

			if (_depthProgress >= 1f)
			{
				EmitSignal(SignalName.Escaped, _damageOnEscape);
				QueueFree();
			}
		}

		public void ConfigureSpawn(float normalizedHorizontalOffset, float startY)
		{
			_depthProgress = 0f;
			_pauseConsumed = false;
			_pauseTimeLeft = 0f;
			_normalizedHorizontalOffset = Mathf.Clamp(normalizedHorizontalOffset, -1f, 1f);

			Vector2 viewportSize = GetViewportRect().Size;
			_startPoint = new Vector2(viewportSize.X * 0.5f, startY);
			GlobalPosition = _startPoint;
			Scale = Vector2.One * Mathf.Max(0.01f, _minScale);
		}

		private float GetPatternDepthStep(float delta)
		{
			float baseSpeed = Mathf.Max(0.01f, _depthSpeed);

			switch (_flightPattern)
			{
				case EnemyFlightPattern.EaseInOut:
				{
					float curve = Mathf.SmoothStep(0.25f, 1.0f, _depthProgress);
					return delta * baseSpeed * curve;
				}
				case EnemyFlightPattern.PauseBurst:
				{
					if (!_pauseConsumed && _depthProgress >= Mathf.Clamp(_pauseAtProgress, 0.05f, 0.95f))
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
			float centerX = viewportSize.X * 0.5f;
			float spread = viewportSize.X * 0.5f * _horizontalExpansion;
			float sideMargin = viewportSize.X * 0.1f;

			switch (_flightPattern)
			{
				case EnemyFlightPattern.LeftToRight:
				{
					float leftX = -sideMargin;
					float rightX = viewportSize.X + sideMargin;
					return Mathf.Lerp(leftX, rightX, progress);
				}
				case EnemyFlightPattern.RightToLeft:
				{
					float leftX = -sideMargin;
					float rightX = viewportSize.X + sideMargin;
					return Mathf.Lerp(rightX, leftX, progress);
				}
				case EnemyFlightPattern.Slalom:
				{
					float baseX = centerX + (_normalizedHorizontalOffset * spread * progress);
					float wave = Mathf.Sin(progress * Mathf.Pi * 2f * Mathf.Max(0.1f, _slalomFrequency));
					return baseX + (wave * viewportSize.X * Mathf.Max(0f, _slalomAmplitude));
				}
				default:
				{
					float currentXOffset = _normalizedHorizontalOffset * spread * progress;
					return centerX + currentXOffset;
				}
			}
		}
	}
}
