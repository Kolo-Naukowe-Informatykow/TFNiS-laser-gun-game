using Godot;
using System;

public partial class DefeatOverlay : Control
{
	[Export] private Button _retryButton;
	[Export] private Button _mainMenuButton;
	[Export] private HighScoreTable _highScoreTable;
	[Export(PropertyHint.File, "*.tscn")] private string _mainMenuScenePath = "res://Source/Screens/MainMenu/MainMenu.tscn";

	private ScoreManager _scoreManager;
	private SpaceShooterGameManager _gameManager;

	public override void _Ready()
	{
		Visible = false;
		_scoreManager = ScoreManager.GetOrNull(this);
		_gameManager = SpaceShooterGameManager.GetOrNull(this);
		_highScoreTable ??= GetNodeOrNull<HighScoreTable>("PanelContainer/MarginContainer/VBoxContainer/HBoxContainer/MarginContainer2/HighScoreTable");

		if (_scoreManager != null)
		{
			_scoreManager.HighScoresChanged += OnHighScoresChanged;
		}

		if (_gameManager != null)
		{
			_gameManager.DefeatStateChanged += OnDefeatStateChanged;
		}

		RefreshHighScoreTable();

		if (_retryButton == null)
		{
			GD.PushWarning("DefeatOverlay: nie znaleziono przycisku restartu.");
		}
		else
		{
			_retryButton.Pressed += OnRetryPressed;
		}

		if (_mainMenuButton == null)
		{
			GD.PushWarning("DefeatOverlay: nie znaleziono przycisku Main Menu.");
		}
		else
		{
			_mainMenuButton.Pressed += OnMainMenuPressed;
		}
	}

	public override void _ExitTree()
	{
		if (_scoreManager != null)
		{
			_scoreManager.HighScoresChanged -= OnHighScoresChanged;
		}

		if (_gameManager != null)
		{
			_gameManager.DefeatStateChanged -= OnDefeatStateChanged;
		}

		if (_retryButton != null)
		{
			_retryButton.Pressed -= OnRetryPressed;
		}

		if (_mainMenuButton != null)
		{
			_mainMenuButton.Pressed -= OnMainMenuPressed;
		}
	}

	private void OnDefeatStateChanged(bool isDefeated)
	{
		if (!isDefeated)
		{
			return;
		}

		RefreshHighScoreTable();
	}

	private void OnHighScoresChanged()
	{
		RefreshHighScoreTable();
	}

	private void RefreshHighScoreTable()
	{
		if (_highScoreTable == null)
		{
			return;
		}

		_scoreManager ??= ScoreManager.GetOrNull(this);
		_highScoreTable.RefreshFromScoreManager(_scoreManager);
	}

	private void OnRetryPressed()
	{
		SceneTree tree = GetTree();
		if (tree == null)
		{
			return;
		}

		string currentScenePath = tree.CurrentScene?.SceneFilePath;
		if (string.IsNullOrEmpty(currentScenePath))
		{
			tree.ReloadCurrentScene();
			return;
		}

		tree.ChangeSceneToFile(currentScenePath);
	}

	private void OnMainMenuPressed()
	{
		if (string.IsNullOrWhiteSpace(_mainMenuScenePath))
		{
			GD.PushWarning("DefeatOverlay: brak sciezki do Main Menu.");
			return;
		}

		if (!ResourceLoader.Exists(_mainMenuScenePath))
		{
			GD.PushWarning($"DefeatOverlay: scena Main Menu nie istnieje: {_mainMenuScenePath}");
			return;
		}

		GetTree()?.ChangeSceneToFile(_mainMenuScenePath);
	}
}
