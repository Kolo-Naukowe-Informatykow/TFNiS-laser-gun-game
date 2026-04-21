using Godot;
using System;

public partial class DefeatOverlay : Control
{
	private Button _retryButton;

	public override void _Ready()
	{
		Visible = false;
		_retryButton = GetNodeOrNull<Button>("PanelContainer/MarginContainer/VBoxContainer/HBoxContainer3/MarginContainer/HBoxContainer/MarginContainer2/VBoxContainer/PanelContainer/Button");

		if (_retryButton == null)
		{
			GD.PushWarning("DefeatOverlay: nie znaleziono przycisku restartu.");
			return;
		}

		_retryButton.Pressed += OnRetryPressed;
	}

	public override void _ExitTree()
	{
		if (_retryButton != null)
		{
			_retryButton.Pressed -= OnRetryPressed;
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
}
