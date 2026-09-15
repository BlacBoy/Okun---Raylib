using System;
using System.Numerics;
using Raylib_cs;

namespace OkunGame
{
    /// <summary>
    /// Ambient floating sediment/plankton, always present rather than spawn-and-die.
    /// Deliberately separate from ParticleSystem - these wrap back around at the world
    /// bounds instead of fading out and respawning, so the field always looks evenly
    /// populated rather than pulsing as batches die together.
    /// </summary>
    public class SedimentField
    {
        private struct Speck
        {
            public Vector3 Position;
            public Vector3 Drift;
            public float Size;
            public byte Alpha;
        }

        private readonly Speck[] specks;
        private readonly Vector3 boundsMin;
        private readonly Vector3 boundsMax;

        public SedimentField(Vector3 boundsMin, Vector3 boundsMax, int count)
        {
            this.boundsMin = boundsMin;
            this.boundsMax = boundsMax;

            specks = new Speck[count];
            for (int i = 0; i < count; i++)
            {
                specks[i] = CreateSpeck();
            }
        }

        public void Update(float dt)
        {
            for (int i = 0; i < specks.Length; i++)
            {
                specks[i].Position += specks[i].Drift * dt;
                specks[i].Position = WrapToBounds(specks[i].Position);
            }
        }

        public void Draw()
        {
            foreach (Speck s in specks)
            {
                Raylib.DrawSphereEx(s.Position, s.Size, 4, 4, new Color((byte)210, (byte)225, (byte)235, s.Alpha));
            }
        }

        private Speck CreateSpeck()
        {
            return new Speck
            {
                Position = RandomPointInBounds(),
                Drift = new Vector3(
                    RandomSigned() * 0.15f,
                    RandomSigned() * 0.05f, // mostly horizontal drift, gentle vertical bob
                    RandomSigned() * 0.15f
                ),
                Size = 0.03f + (float)Random.Shared.NextDouble() * 0.03f,
                Alpha = (byte)(60 + Random.Shared.Next(0, 60)) // dim, varies a bit per speck
            };
        }

        private Vector3 RandomPointInBounds()
        {
            return new Vector3(
                Lerp(boundsMin.X, boundsMax.X, (float)Random.Shared.NextDouble()),
                Lerp(boundsMin.Y, boundsMax.Y, (float)Random.Shared.NextDouble()),
                Lerp(boundsMin.Z, boundsMax.Z, (float)Random.Shared.NextDouble())
            );
        }

        private Vector3 WrapToBounds(Vector3 pos)
        {
            pos.X = Wrap(pos.X, boundsMin.X, boundsMax.X);
            pos.Y = Wrap(pos.Y, boundsMin.Y, boundsMax.Y);
            pos.Z = Wrap(pos.Z, boundsMin.Z, boundsMax.Z);
            return pos;
        }

        private static float Wrap(float value, float min, float max)
        {
            float range = max - min;
            if (value < min) return value + range;
            if (value > max) return value - range;
            return value;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
        private static float RandomSigned() => (float)(Random.Shared.NextDouble() * 2.0 - 1.0);
    }
}