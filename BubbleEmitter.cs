using System;
using System.Numerics;
using Raylib_cs;

namespace OkunGame
{
    /// <summary>
    /// Continuously emits small rising bubbles from just behind the submarine.
    /// Owns only its spawn timer - actual particle simulation happens in the shared
    /// ParticleSystem it's given.
    /// </summary>
    public class BubbleEmitter
    {
        private const float SpawnInterval = 0.05f; // ~20 bubbles/sec
        private const float BubbleLifeTime = 2.5f;
        private const float RiseSpeed = 1.2f;
        private const float JitterStrength = 0.3f;
        private const float TrailOffset = 2.2f; // how far behind the sub's center bubbles spawn

        private readonly ParticleSystem particles;
        private float spawnTimer;

        public BubbleEmitter(ParticleSystem particles)
        {
            this.particles = particles;
        }

        public void Update(Vector3 subPosition, Vector3 subForward, float dt)
        {
            spawnTimer -= dt;
            if (spawnTimer <= 0.0f)
            {
                spawnTimer = SpawnInterval;
                SpawnBubble(subPosition, subForward);
            }
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