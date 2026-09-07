using System.Numerics;

namespace OkunGame
{
    /// <summary>
    /// Fish has been hooked (by a future weapon calling Fish.Hook()) and thrashes in
    /// place rather than swimming anywhere. After a struggle duration it gives in and
    /// moves to Captured. Later this is where per-weapon "break free" chances or
    /// stamina/tug-of-war mechanics would plug in.
    /// </summary>
    public class ResistingState : FishState
    {
        private const float StruggleDuration = 3.0f;
        private const float ThrashStrength = 3.0f;
        private const float ThrashDisplacementScale = 0.15f;

        public override void Enter(Fish fish)
        {
            fish.StateTimer = 0.0f;
            fish.Speed = 0.0f;
        }

        public override void Update(Fish fish, in FishWorldContext ctx, float dt)
        {
            fish.StateTimer += dt;

            Vector3 jitter = new Vector3(
                RandomSigned(),
                RandomSigned(),
                RandomSigned()
            ) * ThrashStrength;

            fish.Velocity = jitter;
            fish.Position += fish.Velocity * dt * ThrashDisplacementScale;
            Fish.ClampToBounds(fish, ctx);

            if (fish.StateTimer >= StruggleDuration)
            {
                fish.ChangeState(new CapturedState());
            }
        }

        public override void Exit(Fish fish)
        {
        }

        private float RandomSigned() => (float)(Fish.Rng.NextDouble() * 2.0 - 1.0);
    }
}