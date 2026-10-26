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
        private const float BoundsMargin = 1.5f; // wander targets stay this far inside the world edge

        public override void Enter(Fish fish)
        {
            fish.Speed = fish.BaseSpeed;

            // Enter() doesn't know the world bounds, so don't pick a target yet: aim at the fish's own
            // position, which counts as "arrived" and makes the first Update() pick a bounds-aware one.
            fish.WanderTarget = fish.Position;
            fish.WanderTimer = 0.0f;
        }

        public override void Update(Fish fish, in FishWorldContext ctx, float dt)
        {
            // Submarine got close -> bail out to Panic immediately
            float distToSub = Vector3.Distance(fish.Position, ctx.SubmarinePosition);
            if (distToSub < ctx.SubmarinePanicRadius)
            {
                fish.ChangeState(new PanicState());
                return;
            }

            fish.WanderTimer -= dt;
            if (fish.WanderTimer <= 0.0f || Vector3.Distance(fish.Position, fish.WanderTarget) < ArrivalThreshold)
            {
                PickNewWanderTarget(fish, ctx);
            }

            Vector3 toTarget = fish.WanderTarget - fish.Position;
            if (toTarget.LengthSquared() > 0.0001f)
            {
                Vector3 desiredDir = Vector3.Normalize(toTarget);
                fish.Velocity = Vector3.Lerp(fish.Velocity, desiredDir * fish.Speed, dt * SteerSharpness);
            }

            fish.Position += fish.Velocity * dt;
            Fish.ClampToBounds(fish, ctx);
        }

        public override void Exit(Fish fish)
        {
        }

        private void PickNewWanderTarget(Fish fish, in FishWorldContext ctx)
        {
            Vector3 offset = new Vector3(
                RandomSigned() * WanderRadius,
                RandomSigned() * (WanderRadius * 0.4f),
                RandomSigned() * WanderRadius
            );

            // Clamp to a box slightly inside the world. Without this a fish near an edge would often pick
            // a target out in the void, press against the wall trying to reach it, and sit there until
            // its wander timer ran out.
            Vector3 min = ctx.BoundsMin + new Vector3(BoundsMargin);
            Vector3 max = ctx.BoundsMax - new Vector3(BoundsMargin);
            fish.WanderTarget = Vector3.Clamp(fish.Position + offset, min, max);
            fish.WanderTimer = MinWanderInterval + (float)Fish.Rng.NextDouble() * (MaxWanderInterval - MinWanderInterval);
        }

        private float RandomSigned() => (float)(Fish.Rng.NextDouble() * 2.0 - 1.0);
    }
}