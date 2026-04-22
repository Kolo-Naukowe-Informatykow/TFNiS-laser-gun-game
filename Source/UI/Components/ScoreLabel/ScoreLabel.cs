using Godot;
using System;

public partial class ScoreLabel : Label
{
	private ScoreManager _scoreManager;

	public override void _Ready()
	{
		_scoreManager = ScoreManager.GetOrNull(this);
		if (_scoreManager != null)
		{
			_scoreManager.ScoreChanged += OnScoreChanged;
			SetScoreText(_scoreManager.CurrentScore);
			return;
		}

		SetScoreText(0);
	}

	public override void _ExitTree()
	{
		if (_scoreManager != null)
		{
			_scoreManager.ScoreChanged -= OnScoreChanged;
		}
	}

	private void OnScoreChanged(int score)
	{
		SetScoreText(score);
	}

	private void SetScoreText(int score)
	{
		Text = $"Wynik: {score}";
	}
}
