using System.Collections.Generic;
using Raylib_cs;

namespace OkunGame
{
    /// <summary>
    /// Generic pool for short-lived "spawn then die" particles - bubbles and capture
    /// bursts both go through this. Ambient/looping effects (SedimentField) manage
    /// their own particles instead, since their lifecycle (wrap-around, never dying)
    /// doesn't fit this pool's age-based fade-and-remove model.
    /// </summary>
    public class ParticleSystem
    {
        private readonly List<Particle> particles = new List<Particle>();
        private readonly int maxParticles;

        public int Count => particles.Count;

        public ParticleSystem(int maxParticles)
        {
            this.maxParticles = maxParticles;
        }

        /// <summary>
        /// Adds a particle if there's room. Silently drops it if the pool is full
        /// rather than growing unbounded - a missed bubble here and there is invisible;
        /// unbounded growth is not.
        /// </summary>
        public void Emit(Particle particle)
        {
            if (particles.Count >= maxParticles) return;
            particles.Add(particle);
        }

        public void Update(float dt)
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                Particle p = particles[i];
                p.Age += dt;

                if (p.Age >= p.LifeTime)
                {
                    particles.RemoveAt(i);
                    continue;
                }

                p.Velocity += p.Acceleration * dt;
                p.Position += p.Velocity * dt;
                particles[i] = p;
            }
        }

        public void Draw()
        {
            foreach (Particle p in particles)
            {
                float t = p.Age / p.LifeTime; // 0 (just spawned) -> 1 (about to die)
                float size = Lerp(p.StartSize, p.EndSize, t);

                byte alpha = p.FadeOut
                    ? (byte)(p.Color.A * (1.0f - t))
                    : p.Color.A;

                Color drawColor = new Color(p.Color.R, p.Color.G, p.Color.B, alpha);
                Raylib.DrawSphereEx(p.Position, size, 4, 4, drawColor); // low-poly - these are tiny, detail is wasted
            }
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}