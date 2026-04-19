using Godot;
using System;

public partial class TimerLabel : Label
{
	private double _elapsedTime;

	public override void _Ready()
	{
		_elapsedTime = 0d;
		UpdateLabel();
	}

	public override void _Process(double delta)
	{
		_elapsedTime += delta;
		UpdateLabel();
	}

	private void UpdateLabel()
	{
		int totalSeconds = Mathf.FloorToInt((float)_elapsedTime);
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
