using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace OkunGame
{
    /// <summary>
    /// Owns the fish population: spawning, per-frame updates, drawing, and collecting
    /// captured fish once they're reeled in close enough to the submarine.
    /// </summary>
    public class FishManager
    {
        private static readonly Color[] Palette =
        {
            Color.Yellow, Color.Orange, Color.Lime, Color.SkyBlue, Color.Pink
        };

        private const float CollectDistance = 1.5f;

        private readonly List<Fish> fish = new List<Fish>();
        private readonly Vector3 boundsMin;
        private readonly Vector3 boundsMax;
        private readonly float panicRadius;

        public int CaughtCount { get; private set; }
        public int Count => fish.Count;

        public FishManager(Vector3 boundsMin, Vector3 boundsMax, float panicRadius, int initialCount)
        {
            this.boundsMin = boundsMin;
            this.boundsMax = boundsMax;
            this.panicRadius = panicRadius;

            for (int i = 0; i < initialCount; i++)
            {
                SpawnFish();
            }
        }

        public void SpawnFish()
        {
            Vector3 pos = new Vector3(
                Lerp(boundsMin.X, boundsMax.X, (float)Fish.Rng.NextDouble()),
                Lerp(boundsMin.Y, boundsMax.Y, (float)Fish.Rng.NextDouble()),
                Lerp(boundsMin.Z, boundsMax.Z, (float)Fish.Rng.NextDouble())
            );

            float baseSpeed = 1.0f + (float)Fish.Rng.NextDouble() * 1.0f;       // 1.0 - 2.0
            float panicMultiplier = 2.5f + (float)Fish.Rng.NextDouble();        // 2.5x - 3.5x
            float size = 0.3f + (float)Fish.Rng.NextDouble() * 0.3f;            // 0.3 - 0.6
            Color color = Palette[Fish.Rng.Next(Palette.Length)];

            fish.Add(new Fish(pos, baseSpeed, baseSpeed * panicMultiplier, size, color));
        }

        public void Update(Vector3 submarinePosition, float dt)
        {
            FishWorldContext ctx = new FishWorldContext
            {
                SubmarinePosition = submarinePosition,
                SubmarinePanicRadius = panicRadius,
                BoundsMin = boundsMin,
                BoundsMax = boundsMax
            };

            for (int i = fish.Count - 1; i >= 0; i--)
            {
                Fish f = fish[i];
                f.Update(ctx, dt);

                if (f.IsCaptured && Vector3.Distance(f.Position, submarinePosition) < CollectDistance)
                {
                    fish.RemoveAt(i);
                    CaughtCount++;
                    SpawnFish(); // keep the population topped up
                }
            }
        }

        public void Draw(bool debugColors)
        {
            foreach (Fish f in fish)
            {
                Color drawColor = debugColors ? f.GetStateDebugColor() : f.BodyColor;
                Raylib.DrawSphere(f.Position, f.Size, drawColor);

                if (debugColors)
                {
                    Raylib.DrawSphereWires(f.Position, f.Size, 6, 6, Color.Black);
                }
            }
        }

        public Fish GetNearestFish(Vector3 position, float maxDistance)
        {
            Fish nearest = null;
            float nearestDist = maxDistance;

            foreach (Fish f in fish)
            {
                if (!f.IsHookable) continue;

                float d = Vector3.Distance(f.Position, position);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearest = f;
                }
            }

            return nearest;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}