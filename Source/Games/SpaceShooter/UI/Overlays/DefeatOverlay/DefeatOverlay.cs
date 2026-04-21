using Godot;
using System;

public partial class DefeatOverlay : Control
{
	[Export] private Button _retryButton;
	[Export] private Button _mainMenuButton;
	[Export(PropertyHint.File, "*.tscn")] private string _mainMenuScenePath = "res://Source/Screens/MainMenu/MainMenu.tscn";

	public override void _Ready()
	{
		Visible = false;

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
		if (_retryButton != null)
		{
			_retryButton.Pressed -= OnRetryPressed;
		}

		if (_mainMenuButton != null)
		{
			_mainMenuButton.Pressed -= OnMainMenuPressed;
		}
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
