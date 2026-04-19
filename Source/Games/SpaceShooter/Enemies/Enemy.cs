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

		private readonly RandomNumberGenerator _rng = new();

		[Export] private float _minScale = 0.2f;
		[Export] private float _maxScale = 1.2f;
		[Export] private float _depthSpeed = 0.1f;
		[Export] private float _horizontalExpansion = 2.4f;
		[Export] private int _damageOnEscape = 10;
		[Export] private float _endYMargin = 120f;
		[Export] private float _slalomFrequency = 2.3f;
		[Export] private float _slalomAmplitude = 0.22f;
		[Export] private float _pauseAtProgress = 0.35f;
		[Export] private float _pauseDuration = 0.45f;
		[Export] private float _burstMultiplier = 1.8f;

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

		public void ConfigureSpawn(float startY, EnemySpawnOrigin spawnOrigin, EnemyTargetLane targetLane)
		{
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
			Scale = Vector2.One * Mathf.Max(0.01f, _minScale);
		}

		public void HandleDeath()
		{
			QueueFree();
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
			float spread = viewportSize.X * 0.5f * _horizontalExpansion;

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
	}
}
