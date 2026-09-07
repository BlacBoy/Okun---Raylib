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

        private float RandomSigned() => (float)(Fish.Rng.NextDouble() * 2.0 - 1.0);
    }
}