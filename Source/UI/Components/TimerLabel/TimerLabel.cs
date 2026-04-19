using Godot;
using System;

public partial class TimerLabel : Label
{
	private double _elapsedTime;
	private int _lastDisplayedSecond;

	public override void _Ready()
	{
		_elapsedTime = 0d;
		_lastDisplayedSecond = -1;
		UpdateLabel();
	}

	public override void _Process(double delta)
	{
		_elapsedTime += delta;
		UpdateLabel();
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
