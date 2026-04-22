using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class ScoreManager : Node
{
	[Signal] public delegate void ScoreChangedEventHandler(int score);
	[Signal] public delegate void HighScoresChangedEventHandler();

	private const int MaxHighScores = 10;
	private const string HighScoresFilePath = "user://space_shooter_high_scores.json";

	public static ScoreManager Instance { get; private set; }

	public int CurrentScore { get; private set; }
	public string PlayerName { get; set; } = "Player";
	public IReadOnlyList<HighScoreEntry> HighScores => _highScores;
	public HighScoreEntry LastFinalizedEntry { get; private set; }

	private readonly List<HighScoreEntry> _highScores = new();
	private bool _runFinalized;

	public override void _EnterTree()
	{
		if (Instance != null && Instance != this)
		{
			GD.PushWarning("ScoreManager: wykryto druga instancje. Nadpisuje Instance.");
		}

		Instance = this;
		LoadHighScores();
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public static ScoreManager GetOrNull(Node context)
	{
		if (Instance != null)
		{
			return Instance;
		}

		if (context == null)
		{
			return null;
		}

		Node currentScene = context.GetTree()?.CurrentScene;
		if (currentScene == null)
		{
			return null;
		}

		return currentScene.GetNodeOrNull<ScoreManager>("ScoreManager");
	}

	public void ResetRunState()
	{
		CurrentScore = 0;
		_runFinalized = false;
		LastFinalizedEntry = null;
		EmitSignal(SignalName.ScoreChanged, CurrentScore);
	}

	public void AddScore(int score)
	{
		if (score <= 0)
		{
			return;
		}

		CurrentScore += score;
		EmitSignal(SignalName.ScoreChanged, CurrentScore);
	}

	public HighScoreEntry FinalizeRun()
	{
		if (_runFinalized)
		{
			return null;
		}

		if (CurrentScore <= 0)
		{
			return null;
		}

		_runFinalized = true;

		string normalizedPlayerName = string.IsNullOrWhiteSpace(PlayerName) ? "Player" : PlayerName.Trim();
		HighScoreEntry entry = new HighScoreEntry
		{
			Game = GlobalGameManager.Games.SpaceShooter,
			PlayerName = normalizedPlayerName,
			Score = CurrentScore,
			AchievedAtUtc = DateTime.UtcNow
		};

		_highScores.Add(entry);
		_highScores.Sort(CompareHighScoreEntries);
		if (_highScores.Count > MaxHighScores)
		{
			_highScores.RemoveRange(MaxHighScores, _highScores.Count - MaxHighScores);
		}

		bool entryPersisted = _highScores.Contains(entry);
		LastFinalizedEntry = entryPersisted ? entry : null;
		SaveHighScores();
		EmitSignal(SignalName.HighScoresChanged);
		return LastFinalizedEntry;
	}

	public HighScoreEntry GetBestHighScore()
	{
		return _highScores.Count > 0 ? _highScores[0] : null;
	}

	public void UpdateLastFinalizedPlayerName(string playerName)
	{
		if (LastFinalizedEntry == null || !_highScores.Contains(LastFinalizedEntry))
		{
			LastFinalizedEntry = null;
			return;
		}

		string normalized = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim();
		PlayerName = normalized;
		LastFinalizedEntry.PlayerName = normalized;
		SaveHighScores();
		EmitSignal(SignalName.HighScoresChanged);
	}

	private static int CompareHighScoreEntries(HighScoreEntry left, HighScoreEntry right)
	{
		if (left == null && right == null)
		{
			return 0;
		}

		if (left == null)
		{
			return 1;
		}

		if (right == null)
		{
			return -1;
		}

		int scoreComparison = right.Score.CompareTo(left.Score);
		if (scoreComparison != 0)
		{
			return scoreComparison;
		}

		return left.AchievedAtUtc.CompareTo(right.AchievedAtUtc);
	}

	private void LoadHighScores()
	{
		_highScores.Clear();

		if (!FileAccess.FileExists(HighScoresFilePath))
		{
			return;
		}

		using FileAccess file = FileAccess.Open(HighScoresFilePath, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PushWarning($"ScoreManager: nie udalo sie otworzyc {HighScoresFilePath}.");
			return;
		}

		string json = file.GetAsText();
		if (string.IsNullOrWhiteSpace(json))
		{
			return;
		}

		try
		{
			List<HighScoreEntry> loadedScores = JsonSerializer.Deserialize<List<HighScoreEntry>>(json);
			if (loadedScores == null)
			{
				return;
			}

			_highScores.AddRange(loadedScores);
			_highScores.Sort(CompareHighScoreEntries);
			if (_highScores.Count > MaxHighScores)
			{
				_highScores.RemoveRange(MaxHighScores, _highScores.Count - MaxHighScores);
			}
		}
		catch (Exception exception)
		{
			GD.PushWarning($"ScoreManager: nie udalo sie wczytac high score: {exception.Message}");
		}
	}

	private void SaveHighScores()
	{
		try
		{
			string json = JsonSerializer.Serialize(_highScores, new JsonSerializerOptions
			{
				WriteIndented = true
			});

			using FileAccess file = FileAccess.Open(HighScoresFilePath, FileAccess.ModeFlags.Write);
			if (file == null)
			{
				GD.PushWarning($"ScoreManager: nie udalo sie zapisac {HighScoresFilePath}.");
				return;
			}

			file.StoreString(json);
		}
		catch (Exception exception)
		{
			GD.PushWarning($"ScoreManager: nie udalo sie zapisac high score: {exception.Message}");
		}
	}
}

[Serializable]
public sealed class HighScoreEntry
{
	public GlobalGameManager.Games Game { get; set; } = GlobalGameManager.Games.SpaceShooter;
	public string PlayerName { get; set; } = "Player";
	public int Score { get; set; }
	public DateTime AchievedAtUtc { get; set; }
}
