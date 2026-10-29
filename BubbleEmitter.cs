using System;
using System.Numerics;
using Raylib_cs;

namespace OkunGame
{
    /// <summary>
    /// Emits small rising bubbles from just behind the submarine. The rate follows the submarine's speed:
    /// a gentle trickle when it's stopped, a proper stream at full speed. Owns only its spawn accumulator -
    /// actual particle simulation happens in the shared ParticleSystem it's given.
    /// </summary>
    public class BubbleEmitter
    {
        private const float IdleRate = 4.0f;   // bubbles/sec when stopped
        private const float FullRate = 30.0f;  // bubbles/sec at full speed
        private const int MaxPerFrame = 6;     // safety cap so a long frame can't dump a huge burst
        private const float BubbleLifeTime = 2.5f;
        private const float RiseSpeed = 1.2f;
        private const float JitterStrength = 0.3f;
        private const float TrailOffset = 2.2f; // how far behind the sub's center bubbles spawn

        private readonly ParticleSystem particles;
        private float spawnAccumulator;

        public BubbleEmitter(ParticleSystem particles)
        {
            this.particles = particles;
        }

        /// <param name="speedFraction">0..1, how fast the submarine is going relative to full speed.</param>
        public void Update(Vector3 subPosition, Vector3 subForward, float speedFraction, float dt)
        {
            float rate = IdleRate + (FullRate - IdleRate) * Math.Clamp(speedFraction, 0.0f, 1.0f);
            spawnAccumulator += rate * dt;

            // Emit every bubble that's due. Accumulating (instead of "one per update") keeps the rate
            // right at any frame rate, including long frames.
            int spawned = 0;
            while (spawnAccumulator >= 1.0f && spawned < MaxPerFrame)
            {
                spawnAccumulator -= 1.0f;
                SpawnBubble(subPosition, subForward);
                spawned++;
            }

            spawnAccumulator = MathF.Min(spawnAccumulator, 1.0f);
        }

        private void SpawnBubble(Vector3 subPosition, Vector3 subForward)
        {
            Vector3 spawnPos = subPosition - subForward * TrailOffset;

            Vector3 jitter = new Vector3(
                RandomSigned() * JitterStrength,
                RandomSigned() * JitterStrength * 0.5f,
                RandomSigned() * JitterStrength
            );

            float sizeVariance = 0.85f + (float)Random.Shared.NextDouble() * 0.3f;

            particles.Emit(new Particle
            {
                Position = spawnPos + jitter,
                Velocity = new Vector3(0.0f, RiseSpeed, 0.0f),
                Acceleration = new Vector3(0.0f, 0.4f, 0.0f), // bubbles accelerate slightly as they rise
                Age = 0.0f,
                LifeTime = BubbleLifeTime,
                StartSize = 0.06f * sizeVariance,
                EndSize = 0.02f * sizeVariance,
                Color = new Color((byte)200, (byte)230, (byte)255, (byte)180),
                FadeOut = true
            });
        }

        private static float RandomSigned() => (float)(Random.Shared.NextDouble() * 2.0 - 1.0);
    }
}
