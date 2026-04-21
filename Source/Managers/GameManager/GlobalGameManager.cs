using Godot;
using System;

public partial class GlobalGameManager : Node
{
        public static GlobalGameManager Instance
        {
            get
            {
                SceneTree tree = Engine.GetMainLoop() as SceneTree;
                return tree?.Root?.GetNodeOrNull<GlobalGameManager>("GlobalGameManager");
            }
        }
        [Signal] public delegate void CurrentGameChangedEventHandler(int game);

        [Export] private ulong _seed = 0;

        public enum Games
        {
            SpaceShooter,
        }

        public Games CurrentGame { get; private set; } = Games.SpaceShooter;
        public ulong RuntimeSeed { get; private set; }

        private ulong _rngSequence = 1;

        public override void _Ready()
        {
            ProcessMode = ProcessModeEnum.Always;
            RuntimeSeed = _seed != 0 ? _seed : GenerateRandomSeed();
            _rngSequence = 1;
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event.IsActionPressed("ExitGame"))
            {
                GetTree().Quit();
            }
        }

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

        public void SeedRngUnique(RandomNumberGenerator rng, string streamName)
        {
            if (rng == null)
            {
                return;
            }

            ulong streamSeed = HashStreamName(streamName);
            ulong mixed = Mix64(RuntimeSeed ^ streamSeed ^ _rngSequence);
            _rngSequence++;
            rng.Seed = mixed;
        }

        private static ulong GenerateRandomSeed()
        {
            ulong ticks = (ulong)DateTime.UtcNow.Ticks;
            ulong processTime = (ulong)Time.GetTicksUsec();
            return Mix64(ticks ^ processTime);
        }

        private static ulong HashStreamName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 1469598103934665603UL;
            }

            ulong hash = 1469598103934665603UL;
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= 1099511628211UL;
            }

            return hash;
        }

        private static ulong Mix64(ulong z)
        {
            z += 0x9e3779b97f4a7c15UL;
            z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9UL;
            z = (z ^ (z >> 27)) * 0x94d049bb133111ebUL;
            return z ^ (z >> 31);
        }
}
