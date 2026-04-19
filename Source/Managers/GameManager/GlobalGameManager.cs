using Godot;
using System;

public partial class GlobalGameManager : Node
{
        public static GlobalGameManager Instance => ((SceneTree)Engine.GetMainLoop()).Root.GetNode<GlobalGameManager>("GlobalGameManager");
        [Signal] public delegate void CurrentGameChangedEventHandler(int game);

        public enum Games
        {
            SpaceShooter,
        }

        public Games CurrentGame { get; private set; } = Games.SpaceShooter;

        public bool IsCurrentGame(Games game)
        {
            return CurrentGame == game;
        }

        public void SetCurrentGame(Games game)
        {
            if (CurrentGame == game)
            {
                return;
            }

            CurrentGame = game;
            EmitSignal(SignalName.CurrentGameChanged, (int)CurrentGame);
        }
}
