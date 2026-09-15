using System.Numerics;
using Raylib_cs;

namespace OkunGame
{
    /// <summary>
    /// A single short-lived particle (bubble, capture-burst spark, etc). Owned and
    /// updated by ParticleSystem - nothing else should need to touch these fields
    /// directly once emitted.
    /// </summary>
    public struct Particle
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 Acceleration; // constant force applied every frame (buoyancy, drag, etc.)

        public float Age;
        public float LifeTime;

        public float StartSize;
        public float EndSize;

        public Color Color;
        public bool FadeOut; // whether alpha shrinks to 0 over the particle's lifetime
    }
}