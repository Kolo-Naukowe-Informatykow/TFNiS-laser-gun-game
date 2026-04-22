using Godot;
using System;

public partial class HighScoreTableEntry : HBoxContainer
{
    [Export] private Label _numberLabel;
    [Export] private Label _nameLabel;
    [Export] private Label _scoreLabel;
    [Export] private Label _dateLabel;

    public void SetEntryData(int place, string name, int score, DateTime date)
    {
        _numberLabel.Text = $"{place}.";
        _nameLabel.Text = name;
        _scoreLabel.Text = score.ToString();
        _dateLabel.Text = date.ToString("dd.MM.yyyy HH:mm");
    }
}
