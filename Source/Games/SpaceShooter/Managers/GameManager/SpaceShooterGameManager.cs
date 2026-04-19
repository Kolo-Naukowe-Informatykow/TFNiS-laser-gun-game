using Godot;
using System;

public partial class SpaceShooterGameManager : Node
{
	[Signal] public delegate void PlayerChangedEventHandler(Player player);
	[Signal] public delegate void SpaceShooterActiveChangedEventHandler(bool isActive);

	public static SpaceShooterGameManager Instance { get; private set; }

	public Player Player { get; private set; }
	public bool IsSpaceShooterActive { get; private set; }

	public override void _EnterTree()
	{
		if (Instance != null && Instance != this)
		{
			GD.PushWarning("SpaceShooterGameManager: wykryto druga instancje. Nadpisuje Instance.");
		}

		Instance = this;
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public static SpaceShooterGameManager GetOrNull(Node context)
	{
		if (Instance != null)
		{
			return Instance;
		}

		return context?.GetNodeOrNull<SpaceShooterGameManager>("/root/GameManager");
	}

	public void ActivateSpaceShooter(Player player)
	{
		GlobalGameManager.Instance?.SetCurrentGame(GlobalGameManager.Games.SpaceShooter);

		bool activeChanged = !IsSpaceShooterActive;
		IsSpaceShooterActive = true;

		if (activeChanged)
		{
			EmitSignal(SignalName.SpaceShooterActiveChanged, IsSpaceShooterActive);
		}

		SetPlayer(player);
	}

	public void DeactivateSpaceShooter(Player expectedPlayer = null)
	{
		if (expectedPlayer == null || expectedPlayer == Player)
		{
			SetPlayer(null);
		}

		if (!IsSpaceShooterActive)
		{
			return;
		}

		IsSpaceShooterActive = false;
		EmitSignal(SignalName.SpaceShooterActiveChanged, IsSpaceShooterActive);
	}

	public void SetPlayer(Player player)
	{
		if (Player == player)
		{
			return;
		}

		Player = player;
		EmitSignal(SignalName.PlayerChanged, Player);
	}
}
