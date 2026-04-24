using Godot;
using System;

public partial class Player : Node
{
    [Signal] public delegate void ShotFiredEventHandler(Vector2 screenPosition);
    [Signal] public delegate void PlayerDiedEventHandler();

    [Export] public int ShotDamage = 80;
    [Export] private float _shotAssistRadius = 20f;
    [Export] private AudioStreamPlayer _shotSfx;
    [Export] private AudioStreamPlayer _hurtSfx;
    [Export] private HealthComponent _healthComponent;
    [Export] private float _shotCooldown = 0.16f;

    public HealthComponent HealthComponent => _healthComponent;

    private double _shotCooldownRemaining;
    private SpaceShooterGameManager _gameManager;

    public override void _Ready()
    {
        SetProcess(true);
        _gameManager = SpaceShooterGameManager.GetOrNull(this);
        if (_gameManager != null)
        {
            _gameManager.DefeatStateChanged += OnDefeatStateChanged;
        }

        _healthComponent ??= GetNodeOrNull<HealthComponent>("HealthComponent");

        if (_healthComponent == null)
        {
            GD.PushWarning("Player: brak HealthComponent.");
            return;
        }

        _healthComponent.Died += OnDied;
    }

    public override void _ExitTree()
    {
        if (_gameManager != null)
        {
            _gameManager.DefeatStateChanged -= OnDefeatStateChanged;
        }

        if (_healthComponent != null)
        {
            _healthComponent.Died -= OnDied;
        }
    }

    public override void _Process(double delta)
    {
        if (_shotCooldownRemaining > 0d)
        {
            _shotCooldownRemaining = Math.Max(0d, _shotCooldownRemaining - delta);
        }
    }

    public void ReceiveDamage(int damage)
    {
        _healthComponent?.ReceiveDamage(damage);

        // This is probably not the right place, but i can't find a better one right now
        _hurtSfx?.Play();
    }

    public void RequestShot(Vector2 screenPosition)
    {
        if (_gameManager?.IsDefeated ?? false)
        {
            return;
        }

        if (_shotCooldownRemaining > 0d)
        {
            return;
        }

        ExecuteShot(screenPosition);
    }

    private void ExecuteShot(Vector2 screenPosition)
    {
        _shotCooldownRemaining = Mathf.Max(0.01f, _shotCooldown);
        TryDamageEnemyAtScreenPosition(screenPosition);

        EmitSignal(SignalName.ShotFired, screenPosition);

        _shotSfx?.Play();
    }

    private bool TryDamageEnemyAtScreenPosition(Vector2 screenPosition)
    {
        if (_gameManager?.IsDefeated ?? false)
        {
            return false;
        }

        Vector2 worldPosition = ScreenToWorldPosition(screenPosition);
        Godot.Collections.Array<Godot.Collections.Dictionary> results = QueryHitboxesAtWorldPosition(worldPosition);

        foreach (Godot.Collections.Dictionary result in results)
        {
            GodotObject collider = ExtractCollider(result);
            if (collider is not HitboxComponent hitbox)
            {
                continue;
            }

            hitbox.ReceiveDamage(ShotDamage);
            return true;
        }

        return false;
    }

    private Godot.Collections.Array<Godot.Collections.Dictionary> QueryHitboxesAtWorldPosition(Vector2 worldPosition)
    {
        var spaceState = GetViewport().GetWorld2D().DirectSpaceState;

        PhysicsPointQueryParameters2D pointQuery = new PhysicsPointQueryParameters2D
        {
            Position = worldPosition,
            CollideWithAreas = true,
            CollideWithBodies = false,
            CollisionMask = uint.MaxValue
        };

        Godot.Collections.Array<Godot.Collections.Dictionary> pointResults = spaceState.IntersectPoint(pointQuery);
        if (pointResults.Count > 0 || _shotAssistRadius <= 0f)
        {
            return pointResults;
        }

        CircleShape2D assistShape = new CircleShape2D
        {
            Radius = Mathf.Max(1f, _shotAssistRadius)
        };

        PhysicsShapeQueryParameters2D shapeQuery = new PhysicsShapeQueryParameters2D
        {
            Shape = assistShape,
            Transform = new Transform2D(0f, worldPosition),
            CollideWithAreas = true,
            CollideWithBodies = false,
            CollisionMask = uint.MaxValue
        };

        return spaceState.IntersectShape(shapeQuery);
    }

    private Vector2 ScreenToWorldPosition(Vector2 screenPosition)
    {
        Transform2D canvasTransform = GetViewport().GetCanvasTransform();
        return canvasTransform.AffineInverse() * screenPosition;
    }

    private GodotObject ExtractCollider(Godot.Collections.Dictionary result)
    {
        if (!result.ContainsKey("collider"))
        {
            return null;
        }

        Variant raw = (Variant)result["collider"];
        if (raw.VariantType == Variant.Type.Object)
        {
            return raw.AsGodotObject();
        }

        return null;
    }

    private void OnDied()
    {
        EmitSignal(SignalName.PlayerDied);
    }

    private void OnDefeatStateChanged(bool isDefeated)
    {
        _ = isDefeated;
    }
}