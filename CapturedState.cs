using System.Numerics;

namespace OkunGame
{
    /// <summary>
    /// Fish has stopped resisting and is reeled straight toward the submarine.
    /// FishManager is responsible for noticing when a captured fish gets close enough
    /// to the sub and removing/collecting it - this state just handles the movement.
    /// </summary>
    public class CapturedState : FishState
    {
        private const float ReelSpeed = 6.0f;

        public override void Enter(Fish fish)
        {
            fish.Speed = ReelSpeed;
        }

        public override void Update(Fish fish, in FishWorldContext ctx, float dt)
        {
            Vector3 toSub = ctx.SubmarinePosition - fish.Position;
            if (toSub.LengthSquared() > 0.0001f)
            {
                Vector3 dir = Vector3.Normalize(toSub);
                fish.Velocity = dir * fish.Speed;
                fish.Position += fish.Velocity * dt;
            }
        }

        public override void Exit(Fish fish)
        {
        }
    }
}