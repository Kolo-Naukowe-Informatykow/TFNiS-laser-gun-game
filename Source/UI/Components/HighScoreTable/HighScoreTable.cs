using Godot;
using System;
using System.Collections.Generic;

public partial class HighScoreTable : PanelContainer
{
    private const string RuntimeEntryPrefix = "RuntimeHighScoreEntry_";

    [Export] private int _maxEntries = 8;
    [Export] private VBoxContainer _entriesContainer;
    [Export(PropertyHint.File, "*.tscn")] private string _entryScenePath;
    [Export] private HBoxContainer _currentEntryContainer;
    [Export] private Label _currentPlaceLabel;
    [Export] private LineEdit _currentNameLineEdit;
    [Export] private Label _currentScoreLabel;
    [Export] private Label _currentDateLabel;

    private PackedScene _entryScene;
    private bool _callbacksConnected;
    private Control _currentEntryWrapper;

    public override void _Ready()
    {
        _maxEntries = Math.Max(1, _maxEntries);
        EnsureCurrentStatsNodes();

        if (!string.IsNullOrWhiteSpace(_entryScenePath))
        {
            _entryScene = ResourceLoader.Load<PackedScene>(_entryScenePath);
        }

        if (_currentNameLineEdit != null && !_callbacksConnected)
        {
            _currentNameLineEdit.TextSubmitted += OnCurrentNameSubmitted;
            _currentNameLineEdit.FocusExited += OnCurrentNameFocusExited;
            _callbacksConnected = true;
        }

        if (_currentPlaceLabel != null)
        {
            _currentPlaceLabel.Text = "-";
        }

        if (_currentScoreLabel != null)
        {
            _currentScoreLabel.Text = "0";
        }

        if (_currentDateLabel != null)
        {
            _currentDateLabel.Text = "-";
        }
    }

    public override void _ExitTree()
    {
        if (_currentNameLineEdit != null && _callbacksConnected)
        {
            _currentNameLineEdit.TextSubmitted -= OnCurrentNameSubmitted;
            _currentNameLineEdit.FocusExited -= OnCurrentNameFocusExited;
        }
    }

    public void RefreshFromScoreManager(ScoreManager scoreManager)
    {
        EnsureCurrentStatsNodes();

        if (scoreManager == null)
        {
            SetCurrentEntryVisible(false);
            ClearTableEntries();
            return;
        }

        List<HighScoreEntry> gameEntries = BuildEntriesForCurrentGame(scoreManager);
        HighScoreEntry currentEntry = scoreManager.LastFinalizedEntry;
        bool currentInTop = currentEntry != null && gameEntries.Contains(currentEntry);

        int currentPlace = GetCurrentPlace(gameEntries, currentEntry, scoreManager.CurrentScore);
        UpdateCurrentStats(scoreManager, currentEntry, currentPlace, currentInTop);
        RenderTable(gameEntries, currentEntry, currentInTop);
    }

    private List<HighScoreEntry> BuildEntriesForCurrentGame(ScoreManager scoreManager)
    {
        var result = new List<HighScoreEntry>();
        if (scoreManager?.HighScores == null)
        {
            return result;
        }

        for (int i = 0; i < scoreManager.HighScores.Count; i++)
        {
            HighScoreEntry entry = scoreManager.HighScores[i];
            if (entry == null)
            {
                continue;
            }

            if (entry.Game != GlobalGameManager.Games.SpaceShooter)
            {
                continue;
            }

            result.Add(entry);
        }

        return result;
    }

    private void UpdateCurrentStats(ScoreManager scoreManager, HighScoreEntry currentEntry, int currentPlace, bool inTop)
    {
        string displayName = currentEntry?.PlayerName;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = scoreManager.PlayerName;
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = "Player";
        }

        int score = currentEntry?.Score ?? scoreManager.CurrentScore;
        DateTime achievedAt = currentEntry?.AchievedAtUtc ?? DateTime.UtcNow;

        if (_currentPlaceLabel != null)
        {
            _currentPlaceLabel.Text = inTop ? currentPlace.ToString() : $">{_maxEntries}";
        }

        if (_currentNameLineEdit != null)
        {
            _currentNameLineEdit.Text = displayName;
            _currentNameLineEdit.Editable = currentEntry != null;
        }

        if (_currentScoreLabel != null)
        {
            _currentScoreLabel.Text = score.ToString();
        }

        if (_currentDateLabel != null)
        {
            _currentDateLabel.Text = achievedAt.ToLocalTime().ToString("dd/MM HH:mm");
        }
    }

    private int GetCurrentPlace(List<HighScoreEntry> gameEntries, HighScoreEntry currentEntry, int fallbackScore)
    {
        if (currentEntry == null)
        {
            return 0;
        }

        int index = gameEntries.IndexOf(currentEntry);
        if (index >= 0)
        {
            return index + 1;
        }

        int place = 1;
        for (int i = 0; i < gameEntries.Count; i++)
        {
            HighScoreEntry entry = gameEntries[i];
            if (entry == null)
            {
                continue;
            }

            if (entry.Score > fallbackScore)
            {
                place++;
            }
        }

        return place;
    }

    private void RenderTable(List<HighScoreEntry> gameEntries, HighScoreEntry currentEntry, bool currentInTop)
    {
        ClearTableEntries();

        if (_entriesContainer == null || _entryScene == null)
        {
            return;
        }

        if (currentEntry == null)
        {
            SetCurrentEntryVisible(false);

            int topCount = Math.Min(_maxEntries, gameEntries.Count);
            for (int i = 0; i < topCount; i++)
            {
                HighScoreTableEntry row = CreateEntryRow(i + 1, gameEntries[i]);
                if (row != null)
                {
                    _entriesContainer.AddChild(row);
                    _entriesContainer.MoveChild(row, i);
                }
            }

            return;
        }

        SetCurrentEntryVisible(true);
        EnsureCurrentEntryWrapperParent();

        var regularEntries = new List<HighScoreEntry>();
        int currentDisplayIndex = 0;

        if (currentInTop)
        {
            int added = 0;
            for (int i = 0; i < gameEntries.Count && added < _maxEntries; i++)
            {
                HighScoreEntry entry = gameEntries[i];
                if (entry == currentEntry)
                {
                    currentDisplayIndex = added;
                    added++;
                    continue;
                }

                regularEntries.Add(entry);
                added++;
            }
        }
        else
        {
            int maxRegular = Math.Max(0, _maxEntries - 1);
            for (int i = 0; i < gameEntries.Count && regularEntries.Count < maxRegular; i++)
            {
                regularEntries.Add(gameEntries[i]);
            }

            currentDisplayIndex = regularEntries.Count;
        }

        int regularIndex = 0;
        int totalSlots = Math.Min(_maxEntries, regularEntries.Count + 1);
        for (int slot = 0; slot < totalSlots; slot++)
        {
            if (slot == currentDisplayIndex)
            {
                if (_currentEntryWrapper != null && _currentEntryWrapper.GetParent() == _entriesContainer)
                {
                    _entriesContainer.MoveChild(_currentEntryWrapper, slot);
                }

                continue;
            }

            if (regularIndex >= regularEntries.Count)
            {
                continue;
            }

            HighScoreTableEntry row = CreateEntryRow(slot + 1, regularEntries[regularIndex]);
            regularIndex++;
            if (row == null)
            {
                continue;
            }

            _entriesContainer.AddChild(row);
            _entriesContainer.MoveChild(row, slot);
        }
    }

    private HighScoreTableEntry CreateEntryRow(int place, HighScoreEntry entry)
    {
        if (_entriesContainer == null || _entryScene == null || entry == null)
        {
            return null;
        }

        if (_entryScene.Instantiate() is not HighScoreTableEntry row)
        {
            return null;
        }

        row.Name = $"{RuntimeEntryPrefix}{place}";

        row.SetEntryData(
            place,
            string.IsNullOrWhiteSpace(entry.PlayerName) ? "Player" : entry.PlayerName,
            entry.Score,
            entry.AchievedAtUtc.ToLocalTime()
        );

        return row;
    }

    private void ClearTableEntries()
    {
        if (_entriesContainer == null)
        {
            return;
        }

        for (int i = _entriesContainer.GetChildCount() - 1; i >= 0; i--)
        {
            Node child = _entriesContainer.GetChild(i);
            if (child is HighScoreTableEntry)
            {
                child.QueueFree();
                continue;
            }

            if (child.Name != null && child.Name.ToString().StartsWith(RuntimeEntryPrefix, StringComparison.Ordinal))
            {
                child.QueueFree();
            }
        }
    }

    private void EnsureCurrentStatsNodes()
    {
        _entriesContainer ??= GetNodeOrNull<VBoxContainer>("VBoxContainer2/EntryContainer");

        if (_currentEntryContainer == null || !GodotObject.IsInstanceValid(_currentEntryContainer))
        {
            _currentEntryContainer = GetNodeOrNull<HBoxContainer>("VBoxContainer2/EntryContainer/PanelContainer/CurrentScoreTableEntry");
        }

        if (_currentEntryContainer != null)
        {
            _currentEntryWrapper = _currentEntryContainer.GetParent<Control>();
        }

        if (_currentPlaceLabel == null || !GodotObject.IsInstanceValid(_currentPlaceLabel))
        {
            _currentPlaceLabel = GetNodeOrNull<Label>("VBoxContainer2/EntryContainer/PanelContainer/CurrentScoreTableEntry/PlaceLabel");
        }

        if (_currentNameLineEdit == null || !GodotObject.IsInstanceValid(_currentNameLineEdit))
        {
            _currentNameLineEdit = GetNodeOrNull<LineEdit>("VBoxContainer2/EntryContainer/PanelContainer/CurrentScoreTableEntry/NameLabel");
        }

        if (_currentScoreLabel == null || !GodotObject.IsInstanceValid(_currentScoreLabel))
        {
            _currentScoreLabel = GetNodeOrNull<Label>("VBoxContainer2/EntryContainer/PanelContainer/CurrentScoreTableEntry/ScoreLabel");
        }

        if (_currentDateLabel == null || !GodotObject.IsInstanceValid(_currentDateLabel))
        {
            _currentDateLabel = GetNodeOrNull<Label>("VBoxContainer2/EntryContainer/PanelContainer/CurrentScoreTableEntry/DateLabel");
        }
    }

    private void EnsureCurrentEntryWrapperParent()
    {
        if (_entriesContainer == null || _currentEntryWrapper == null)
        {
            return;
        }

        if (_currentEntryWrapper.GetParent() == _entriesContainer)
        {
            return;
        }

        _currentEntryWrapper.Reparent(_entriesContainer);
    }

    private void SetCurrentEntryVisible(bool visible)
    {
        if (_currentEntryWrapper == null)
        {
            return;
        }

        _currentEntryWrapper.Visible = visible;
    }

    private void OnCurrentNameSubmitted(string newText)
    {
        ApplyCurrentPlayerName(newText);
    }

    private void OnCurrentNameFocusExited()
    {
        if (_currentNameLineEdit == null)
        {
            return;
        }

        ApplyCurrentPlayerName(_currentNameLineEdit.Text);
    }

    private void ApplyCurrentPlayerName(string value)
    {
        ScoreManager scoreManager = ScoreManager.GetOrNull(this);
        if (scoreManager == null)
        {
            return;
        }

        string normalized = string.IsNullOrWhiteSpace(value) ? "Player" : value.Trim();
        scoreManager.UpdateLastFinalizedPlayerName(normalized);
        RefreshFromScoreManager(scoreManager);
    }
}
