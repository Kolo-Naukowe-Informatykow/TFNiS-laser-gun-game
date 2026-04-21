using Godot;
using System;

public partial class GameSelectCard : Control
{
    [Export] private string _gameTitle;
    [Export] private Texture2D _gameIcon;
    [Export(PropertyHint.File, "*.tscn")] private string _sceneToLoad;
    [Export] private Label _titleLabel;
    [Export] private TextureRect _iconSprite;
    [Export] private PanelContainer _panelContainer;
    [Export] private Color _normalPanelModulate = Colors.White;
    [Export] private Color _hoverPanelModulate = new Color(1.12f, 1.12f, 1.12f, 1.0f);

    private bool _isHovered;

    public override void _Ready()
    {
        _titleLabel.Text = _gameTitle;
        _iconSprite.Texture = _gameIcon;

        _panelContainer ??= GetNodeOrNull<PanelContainer>("MarginContainer/PanelContainer");

        MouseDefaultCursorShape = CursorShape.PointingHand;
        MouseFilter = MouseFilterEnum.Stop;
        SetChildrenMouseFilterToIgnore(this);

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;

        UpdateVisualState();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton
            && mouseButton.ButtonIndex == MouseButton.Left
            && mouseButton.Pressed)
        {
            TryLoadScene();
            AcceptEvent();
        }
    }

    private void OnMouseEntered()
    {
        _isHovered = true;
        UpdateVisualState();
    }

    private void OnMouseExited()
    {
        _isHovered = false;
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (_panelContainer == null)
        {
            return;
        }

        _panelContainer.Modulate = _isHovered ? _hoverPanelModulate : _normalPanelModulate;
    }

    private void TryLoadScene()
    {
        if (string.IsNullOrWhiteSpace(_sceneToLoad))
        {
            GD.PushWarning($"GameSelectCard '{Name}' nie ma ustawionej ścieżki sceny (_sceneToLoad).");
            return;
        }

        var error = GetTree().ChangeSceneToFile(_sceneToLoad);
        if (error != Error.Ok)
        {
            GD.PushError($"Nie udało się załadować sceny '{_sceneToLoad}'. Kod błędu: {error}.");
        }
    }

    private static void SetChildrenMouseFilterToIgnore(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is Control control)
            {
                control.MouseFilter = MouseFilterEnum.Ignore;
            }

            SetChildrenMouseFilterToIgnore(child);
        }
    }
}
