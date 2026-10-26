using System;
using System.Numerics;

namespace OkunGame
{
    /// <summary>
    /// Fish flees directly away from the submarine at higher speed, with some jitter so
    /// it doesn't look robotic. Calms back down to Idle once it's been clear of the
    /// submarine's panic radius for a short while.
    /// </summary>
    public class PanicState : FishState
    {
        private const float CalmDuration = 2.5f;
        private const float ClearMultiplier = 1.5f; // must be this far outside panic radius to start calming
        private const float JitterStrength = 2.0f;
        private const float SteerSharpness = 4.0f;
        private const float WallTouchDistance = 0.75f; // within this of a wall counts as being against it

        private float calmTimer;

        public override void Enter(Fish fish)
        {
            fish.Speed = fish.PanicSpeed;
            calmTimer = 0.0f;
        }

        public override void Update(Fish fish, in FishWorldContext ctx, float dt)
        {
            Vector3 away = fish.Position - ctx.SubmarinePosition;
            float distToSub = away.Length();

            Vector3 fleeDir = distToSub > 0.0001f
                ? away / distToSub
                : new Vector3(1.0f, 0.0f, 0.0f); // fallback if fish is exactly on top of the sub

            fleeDir = SlideAlongWalls(fleeDir, away, fish.Position, ctx);

            Vector3 jitter = new Vector3(
                RandomSigned(),
                RandomSigned() * 0.3f,
                RandomSigned()
            ) * JitterStrength;

            Vector3 desiredVel = (fleeDir * fish.Speed) + jitter;
            fish.Velocity = Vector3.Lerp(fish.Velocity, desiredVel, dt * SteerSharpness);
            fish.Position += fish.Velocity * dt;
            Fish.ClampToBounds(fish, ctx);

            if (distToSub > ctx.SubmarinePanicRadius * ClearMultiplier)
            {
                calmTimer += dt;
                if (calmTimer >= CalmDuration)
                {
                    fish.ChangeState(new IdleSwimState());
                }
            }
            else
            {
                calmTimer = 0.0f;
            }
        }

        public override void Exit(Fish fish)
        {
        }

        /// <summary>
        /// A fish fleeing straight into a wall can't go any further, but its desired velocity still
        /// points into the wall every frame, so it just sits there pinned while the submarine closes
        /// in. Drop the part of the flee direction that points into a wall so it slides along it
        /// instead; if that leaves (almost) nothing - it's fleeing dead into the wall or a corner -
        /// slide along the free axis it was being pushed along the most.
        /// </summary>
        private static Vector3 SlideAlongWalls(Vector3 fleeDir, Vector3 away, Vector3 position, in FishWorldContext ctx)
        {
            Vector3 slid = fleeDir;
            bool blockedX = false, blockedY = false, blockedZ = false;

            if (position.X <= ctx.BoundsMin.X + WallTouchDistance && slid.X < 0.0f) { slid.X = 0.0f; blockedX = true; }
            if (position.X >= ctx.BoundsMax.X - WallTouchDistance && slid.X > 0.0f) { slid.X = 0.0f; blockedX = true; }
            if (position.Y <= ctx.BoundsMin.Y + WallTouchDistance && slid.Y < 0.0f) { slid.Y = 0.0f; blockedY = true; }
            if (position.Y >= ctx.BoundsMax.Y - WallTouchDistance && slid.Y > 0.0f) { slid.Y = 0.0f; blockedY = true; }
            if (position.Z <= ctx.BoundsMin.Z + WallTouchDistance && slid.Z < 0.0f) { slid.Z = 0.0f; blockedZ = true; }
            if (position.Z >= ctx.BoundsMax.Z - WallTouchDistance && slid.Z > 0.0f) { slid.Z = 0.0f; blockedZ = true; }

            if (!blockedX && !blockedY && !blockedZ) return fleeDir; // nowhere near a wall: unchanged

            if (slid.LengthSquared() >= 0.04f) return Vector3.Normalize(slid);

            // Nothing left to slide along: pick the free axis the fish was being pushed along the most
            float awayX = blockedX ? 0.0f : MathF.Abs(away.X);
            float awayY = blockedY ? 0.0f : MathF.Abs(away.Y);
            float awayZ = blockedZ ? 0.0f : MathF.Abs(away.Z);

            if (blockedX && blockedY && blockedZ) return fleeDir; // boxed into a corner of a tiny world; nothing sensible to do

            if (!blockedY && awayY >= awayX && awayY >= awayZ) return new Vector3(0.0f, away.Y >= 0.0f ? 1.0f : -1.0f, 0.0f);
            if (!blockedZ && awayZ >= awayX) return new Vector3(0.0f, 0.0f, away.Z >= 0.0f ? 1.0f : -1.0f);
            if (!blockedX) return new Vector3(away.X >= 0.0f ? 1.0f : -1.0f, 0.0f, 0.0f);
            return new Vector3(0.0f, away.Y >= 0.0f ? 1.0f : -1.0f, 0.0f);
        }

        private float RandomSigned() => (float)(Fish.Rng.NextDouble() * 2.0 - 1.0);
    }
}