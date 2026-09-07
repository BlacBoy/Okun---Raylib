using System.Numerics;

namespace OkunGame
{
    /// <summary>
    /// Default state: fish wanders toward a loosely random nearby target, picking a new
    /// one periodically (or once it arrives). Transitions to Panic if the submarine gets
    /// too close.
    /// </summary>
    public class IdleSwimState : FishState
    {
        private const float WanderRadius = 6.0f;
        private const float MinWanderInterval = 2.0f;
        private const float MaxWanderInterval = 5.0f;
        private const float ArrivalThreshold = 0.5f;
        private const float SteerSharpness = 2.0f;

        public override void Enter(Fish fish)
        {
            fish.Speed = fish.BaseSpeed;
            PickNewWanderTarget(fish);
        }

        public override void Update(Fish fish, in FishWorldContext ctx, float dt)
        {
            // Submarine gets close -- initiate Panic mode
            float distToSub = Vector3.Distance(fish.Position, ctx.SubmarinePosition);
            if (distToSub < ctx.SubmarinePanicRadius)
            {
                fish.ChangeState(new PanicState());
                return;
            }

            fish.WanderTimer -= dt;
            if (fish.WanderTimer <= 0.0f || Vector3.Distance(fish.Position, fish.WanderTarget) < ArrivalThreshold)
            {
                PickNewWanderTarget(fish);
            }

            Vector3 toTarget = fish.WanderTarget - fish.Position;
            if (toTarget.LengthSquared() > 0.0001f)
            {
                Vector3 desiredDir = Vector3.Normalize(toTarget);
                fish.Velocity = Vector3.Lerp(fish.Velocity, desiredDir * fish.Speed, SteerSharpness * dt);
            }

            fish.Position += fish.Velocity * dt;
            Fish.ClampToBounds(fish, ctx);
        }

        public override void Exit(Fish fish)
        {
            // Nothing to do here
        }

        private void PickNewWanderTarget(Fish fish)
        {
            Vector3 offset = new Vector3(
                RandomSigned() * WanderRadius,
                RandomSigned() * (WanderRadius * 0.4f),
                RandomSigned() * WanderRadius
            );

            fish.WanderTarget = fish.Position + offset;
            fish.WanderTimer = MinWanderInterval + (float)Fish.Rng.NextDouble() * (MaxWanderInterval - MinWanderInterval);
        }

        private float RandomSigned() => (float)(Fish.Rng.NextDouble() * 2.0 - 1.0);
    }
}