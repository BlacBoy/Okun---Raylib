using System;
using System.Numerics;
using Raylib_cs;

namespace OkunGame
{
    /// <summary>
    /// Static helper that spawns a one-off radial burst of particles into a given
    /// ParticleSystem. Not tied to any specific emitter instance since it's a single
    /// momentary event rather than something with ongoing state.
    /// </summary>
    public static class CaptureBurst
    {
        private const int ParticleCount = 16;
        private const float LifeTime = 0.5f;
        private const float Speed = 2.5f;

        public static void Spawn(ParticleSystem particles, Vector3 position)
        {
            for (int i = 0; i < ParticleCount; i++)
            {
                Vector3 direction = RandomDirection();

                particles.Emit(new Particle
                {
                    Position = position,
                    Velocity = direction * Speed,
                    Acceleration = -direction * (Speed * 1.5f), // quick outward pop that decelerates fast
                    Age = 0.0f,
                    LifeTime = LifeTime,
                    StartSize = 0.12f,
                    EndSize = 0.0f,
                    Color = new Color((byte)255, (byte)240, (byte)180, (byte)255),
                    FadeOut = true
                });
            }
        }

        private static Vector3 RandomDirection()
        {
            // Roughly uniform random direction on a unit sphere
            float theta = (float)(Random.Shared.NextDouble() * Math.PI * 2.0);
            float z = (float)(Random.Shared.NextDouble() * 2.0 - 1.0);
            float r = MathF.Sqrt(1.0f - z * z);

            return new Vector3(r * MathF.Cos(theta), z, r * MathF.Sin(theta));
        }
    }
}