using System;
using System.Numerics;
using Raylib_cs;

namespace OkunGame
{
    /// <summary>
    /// A single fish. Holds its own movement data and current behavior state, and
    /// delegates all Update logic to that state. External systems (like a future
    /// harpoon weapon) never touch state classes directly - they just call Hook().
    /// </summary>
    public class Fish
    {
        public static readonly Random Rng = new Random();

        public Vector3 Position;
        public Vector3 Velocity;

        public float Speed;      // current desired speed, set by whichever state is active
        public float BaseSpeed;  // idle cruising speed
        public float PanicSpeed; // fleeing speed

        public float Size;
        public Color BodyColor;

        // Scratch data used by individual states - kept here so states stay stateless
        // and can be swapped freely without leaking data between instances.
        public Vector3 WanderTarget;
        public float WanderTimer;
        public float StateTimer;

        private FishState currentState;

        public string CurrentStateName => currentState.GetType().Name;
        public bool IsCaptured => currentState is CapturedState;
        public bool IsHookable => !(currentState is ResistingState) && !(currentState is CapturedState);

        public Fish(Vector3 spawnPosition, float baseSpeed, float panicSpeed, float size, Color bodyColor)
        {
            Position = spawnPosition;
            Velocity = Vector3.Zero;
            BaseSpeed = baseSpeed;
            PanicSpeed = panicSpeed;
            Size = size;
            BodyColor = bodyColor;

            currentState = new IdleSwimState();
            currentState.Enter(this);
        }

        public void ChangeState(FishState newState)
        {
            currentState.Exit(this);
            currentState = newState;
            currentState.Enter(this);
        }

        public void Update(in FishWorldContext ctx, float dt)
        {
            currentState.Update(this, ctx, dt);
        }

        /// <summary>
        /// External entry point for anything that catches a fish (currently only the
        /// debug 'H' key in Program.cs; later, a Harpoon weapon). Starts the
        /// resist-then-capture sequence. Safe to call repeatedly - no-ops if the fish
        /// is already being caught.
        /// </summary>
        public void Hook()
        {
            if (IsHookable)
            {
                ChangeState(new ResistingState());
            }
        }

        public Color GetStateDebugColor()
        {
            return currentState switch
            {
                IdleSwimState => Color.SkyBlue,
                PanicState => Color.Orange,
                ResistingState => Color.Yellow,
                CapturedState => Color.Lime,
                _ => Color.White
            };
        }

        public static void ClampToBounds(Fish fish, in FishWorldContext ctx)
        {
            fish.Position = Vector3.Clamp(fish.Position, ctx.BoundsMin, ctx.BoundsMax);
        }
    }
}