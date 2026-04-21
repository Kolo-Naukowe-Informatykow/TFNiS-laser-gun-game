using Godot;
using System;

public partial class TimerLabel : Label
{
	private double _elapsedTime;
	private int _lastDisplayedSecond;
	private bool _isCounting;
	private SpaceShooterGameManager _gameManager;

	public override void _Ready()
	{
		_elapsedTime = 0d;
		_lastDisplayedSecond = -1;
		_isCounting = true;

		_gameManager = SpaceShooterGameManager.GetOrNull(this);
		if (_gameManager != null)
		{
			_gameManager.DefeatStateChanged += OnDefeatStateChanged;
			_gameManager.SpaceShooterActiveChanged += OnSpaceShooterActiveChanged;
			_isCounting = _gameManager.IsSpaceShooterActive && !_gameManager.IsDefeated;
		}

		UpdateLabel();
	}

	public override void _ExitTree()
	{
		if (_gameManager != null)
		{
			_gameManager.DefeatStateChanged -= OnDefeatStateChanged;
			_gameManager.SpaceShooterActiveChanged -= OnSpaceShooterActiveChanged;
		}
	}

	public override void _Process(double delta)
	{
		if (!_isCounting)
		{
			return;
		}

		_elapsedTime += delta;
		UpdateLabel();
	}

	private void OnDefeatStateChanged(bool isDefeated)
	{
		_isCounting = !isDefeated;
	}

	private void OnSpaceShooterActiveChanged(bool isActive)
	{
		_isCounting = isActive && (_gameManager == null || !_gameManager.IsDefeated);
	}

	private void UpdateLabel()
	{
		double flooredSeconds = Math.Floor(_elapsedTime);
		flooredSeconds = Math.Clamp(flooredSeconds, 0d, int.MaxValue);
		int totalSeconds = (int)flooredSeconds;

		if (totalSeconds == _lastDisplayedSecond)
		{
			return;
		}

		_lastDisplayedSecond = totalSeconds;
		int hours = totalSeconds / 3600;
		int minutes = (totalSeconds % 3600) / 60;
		int seconds = totalSeconds % 60;

		if (hours > 0)
		{
			Text = $"{hours}h {minutes}m {seconds}s";
			return;
		}

		if (minutes > 0)
		{
			Text = $"{minutes}m {seconds}s";
			return;
		}

		Text = $"{seconds}s";
	}
}
