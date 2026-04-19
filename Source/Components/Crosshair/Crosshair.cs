using Godot;

public partial class Crosshair : CanvasLayer
{
    [Export] private bool _enableLightGunForceFeedback = true;
    [Export] private string _lightGunVid = "F143";
    [Export] private int _lightGunBaudRate = 9600;
    [Export] private string _lightGunStartCommand = "S";
    [Export] private string _lightGunShotCommand = "F\\x02\\x01";
    [Export] private string _lightGunExitCommand = "E";

    [Export] private TextureRect _crosshairTextureRect;

    [Export] private TextureRect _glowTextureRect;

    private Tween _pulseTween;
    private Vector2 _baseScale = Vector2.One;
    private Vector2 _glowBaseScale = Vector2.One;
    private Color _baseColor = Colors.White;
    private Color _glowBaseColor = new Color(1f, 1f, 1f, 0.16f);
    private LightGunSerialPort _lightGunPort;
    private SpaceShooterGameManager _gameManager;
    private Player _trackedPlayer;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Hidden;
        ProcessMode = ProcessModeEnum.Always;

        if (_enableLightGunForceFeedback)
        {
            _lightGunPort = new LightGunSerialPort(_lightGunVid, _lightGunBaudRate);
            _lightGunPort.SendAscii(_lightGunStartCommand);
        }

        CallDeferred(nameof(RefreshPivotOffsets));

        if (_crosshairTextureRect != null)
        {
            _baseScale = _crosshairTextureRect.Scale;
            _baseColor = _crosshairTextureRect.Modulate;
        }

        if (_glowTextureRect != null)
        {
            _glowBaseScale = _glowTextureRect.Scale;
            _glowBaseColor = _glowTextureRect.Modulate;
        }

		_gameManager = SpaceShooterGameManager.GetOrNull(this);
        if (_gameManager == null)
        {
            _gameManager = GetTree()?.CurrentScene?.GetNodeOrNull<SpaceShooterGameManager>("SpaceShooterGameManager");
        }

		if (_gameManager != null)
		{
			_gameManager.PlayerChanged += OnPlayerChanged;
			AttachToPlayer(_gameManager.Player);
        }
        else
        {
            GD.PushWarning("Crosshair: nie znaleziono SpaceShooterGameManager. Strzelanie gracza moze nie dzialac.");
		}
    }

    public override void _Process(double delta)
    {
        if (_crosshairTextureRect == null)
        {
            return;
        }

        var mousePosition = GetViewport().GetMousePosition();
        _crosshairTextureRect.GlobalPosition = mousePosition - (_crosshairTextureRect.Size * 0.5f);

        if (_glowTextureRect != null)
        {
            _glowTextureRect.GlobalPosition = mousePosition - (_glowTextureRect.Size * 0.5f);
        }

    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("Shoot"))
        {
			_trackedPlayer?.RequestShot(GetViewport().GetMousePosition());
        }
    }

    public override void _ExitTree()
    {
        if (_enableLightGunForceFeedback)
        {
            _lightGunPort?.SendAscii(_lightGunExitCommand);
            _lightGunPort?.Dispose();
            _lightGunPort = null;
        }

        DetachFromPlayer();

        if (_gameManager != null)
        {
            _gameManager.PlayerChanged -= OnPlayerChanged;
        }

        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    private void OnPlayerChanged(Player player)
    {
        AttachToPlayer(player);
    }

    private void AttachToPlayer(Player player)
    {
        DetachFromPlayer();

        _trackedPlayer = player;
        if (_trackedPlayer == null)
        {
            return;
        }

        _trackedPlayer.ShotFired += OnPlayerShotFired;
    }

    private void DetachFromPlayer()
    {
        if (_trackedPlayer != null)
        {
            _trackedPlayer.ShotFired -= OnPlayerShotFired;
            _trackedPlayer = null;
        }
    }

    private void OnPlayerShotFired(Vector2 screenPosition)
    {
        if (_enableLightGunForceFeedback)
        {
            _lightGunPort?.SendNotation(_lightGunShotCommand);
        }

        PlayShotPulse();
    }

    private void PlayShotPulse()
    {
        if (_crosshairTextureRect == null)
        {
            return;
        }

        RefreshPivotOffsets();
        _pulseTween?.Kill();

        var hitColor = new Color(1f, 0.15f, 0.15f, 1f);
        var glowColor = new Color(1f, 0.25f, 0.25f, 0.42f);

        _pulseTween = CreateTween();
        _pulseTween.SetParallel(true);
        _pulseTween.TweenProperty(_crosshairTextureRect, "scale", _baseScale * 1.18f, 0.045f);
        _pulseTween.TweenProperty(_crosshairTextureRect, "modulate", hitColor, 0.045f);

        if (_glowTextureRect != null)
        {
            _pulseTween.TweenProperty(_glowTextureRect, "scale", _glowBaseScale * 1.35f, 0.045f);
            _pulseTween.TweenProperty(_glowTextureRect, "modulate", glowColor, 0.045f);
        }

        _pulseTween.SetParallel(false);
        _pulseTween.TweenInterval(0.035f);

        _pulseTween.SetParallel(true);
        _pulseTween.TweenProperty(_crosshairTextureRect, "scale", _baseScale, 0.08f);
        _pulseTween.TweenProperty(_crosshairTextureRect, "modulate", _baseColor, 0.08f);

        if (_glowTextureRect != null)
        {
            _pulseTween.TweenProperty(_glowTextureRect, "scale", _glowBaseScale, 0.08f);
            _pulseTween.TweenProperty(_glowTextureRect, "modulate", _glowBaseColor, 0.08f);
        }
    }

    private void RefreshPivotOffsets()
    {
        if (_crosshairTextureRect != null)
        {
            _crosshairTextureRect.PivotOffset = _crosshairTextureRect.Size * 0.5f;
        }

        if (_glowTextureRect != null)
        {
            _glowTextureRect.PivotOffset = _glowTextureRect.Size * 0.5f;
        }
    }
}
