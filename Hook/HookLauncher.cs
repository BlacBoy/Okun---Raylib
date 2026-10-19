using System;
using System.Numerics;
using Raylib_cs;

namespace OkunGame
{
    public enum HookPhase
    {
        Stowed,      // not deployed
        Flying,      // travelling out from the sub, paying out rope
        Attached,    // caught a fish; the rope is tied to it
        Retracting   // missed or fish collected; being wound back in
    }

    /// <summary>
    /// The hook: fires from a port on the submarine, flies toward a locked-on fish (or straight
    /// along the aim if nothing is locked), and attaches to the first hookable fish it touches.
    /// A rope runs from the port to the hook the whole time, and its look reflects what the fish
    /// is doing - slack in flight, rattling taut while the fish resists, steady while reeling.
    ///
    /// The only thing it does to a fish is call Fish.Hook(); the fish's own state machine
    /// (resist -> captured -> reeled in) does the rest, so the fish code knows nothing about
    /// hooks. This is the piece that becomes Harpoon : Weapon once the weapon base class exists.
    /// </summary>
    public class HookLauncher
    {
        private const float FlightSpeed = 40.0f;     // units/sec outbound
        private const float RetractSpeed = 60.0f;    // units/sec on the way back
        private const float HitReach = 0.35f;        // extra reach beyond a fish's own radius
        private const float StepSize = 0.3f;         // flight is swept in steps this big so fast hooks can't skip a fish
        private const float StowDistance = 0.6f;     // how close to the port counts as "back home"
        private const float HeadScale = 1.2f;        // overall size of the hook head
        private const int RopeSegments = 14;

        private static readonly Color RopeSlackColor = new Color(205, 205, 215, 255);
        private static readonly Color RopeTenseColor = new Color(255, 110, 50, 255);
        private static readonly Color MetalColor = new Color(195, 202, 215, 255);

        private readonly FishManager fishManager;
        private readonly ParticleSystem particles;

        private Vector3 hookPosition;
        private Vector3 flightDirection = Vector3.UnitZ;
        private float distanceTravelled;
        private float maxRange;

        public HookPhase Phase { get; private set; } = HookPhase.Stowed;

        /// <summary>The fish being homed in on (while flying) or tied to (while attached).</summary>
        public Fish? Target { get; private set; }

        public HookLauncher(FishManager fishManager, ParticleSystem particles)
        {
            this.fishManager = fishManager;
            this.particles = particles;
        }

        /// <summary>
        /// Fires the hook. With a locked target it homes in on it; without one it flies straight
        /// along 'aimDirection' (a dry fire that can still hit something on the way).
        /// Returns false if the hook is already out.
        /// </summary>
        public bool TryFire(Vector3 port, Vector3 aimDirection, Fish? lockedTarget, float range)
        {
            if (Phase != HookPhase.Stowed) return false;

            Phase = HookPhase.Flying;
            Target = lockedTarget;
            hookPosition = port;
            distanceTravelled = 0.0f;
            maxRange = range;

            Vector3 toTarget = lockedTarget != null ? lockedTarget.Position - port : Vector3.Zero;
            flightDirection = toTarget.LengthSquared() > 0.0001f ? Vector3.Normalize(toTarget) : aimDirection;
            return true;
        }

        public void Update(Vector3 port, float dt)
        {
            switch (Phase)
            {
                case HookPhase.Flying: UpdateFlying(dt); break;
                case HookPhase.Attached: UpdateAttached(port); break;
                case HookPhase.Retracting: UpdateRetracting(port, dt); break;
            }
        }

        private void UpdateFlying(float dt)
        {
            EmitTrail(2);

            float remaining = FlightSpeed * dt;

            while (remaining > 0.0f)
            {
                // Keep homing on the target as it swims
                if (Target != null && Target.IsHookable)
                {
                    Vector3 toTarget = Target.Position - hookPosition;
                    if (toTarget.LengthSquared() > 0.0001f) flightDirection = Vector3.Normalize(toTarget);
                }

                float step = MathF.Min(remaining, StepSize);
                hookPosition += flightDirection * step;
                distanceTravelled += step;
                remaining -= step;

                Fish? hit = fishManager.GetHookableFishAt(hookPosition, HitReach);
                if (hit != null)
                {
                    hit.Hook(); // from here the fish's own states take over
                    CaptureBurst.Spawn(particles, hookPosition, 8, new Color(255, 255, 255, 255)); // the hook biting in
                    Target = hit;
                    Phase = HookPhase.Attached;
                    return;
                }

                if (distanceTravelled >= maxRange)
                {
                    Target = null;
                    Phase = HookPhase.Retracting;
                    return;
                }
            }
        }

        private void UpdateAttached(Vector3 port)
        {
            if (Target == null || Target.IsCollected)
            {
                Target = null;
                Phase = HookPhase.Retracting;
                return;
            }

            // Sit on the fish's surface, on the side facing the sub
            Vector3 towardPort = port - Target.Position;
            float length = towardPort.Length();
            Vector3 dir = length > 0.0001f ? towardPort / length : Vector3.UnitZ;

            hookPosition = Target.Position + dir * (Target.Size * 0.7f);
        }

        private void UpdateRetracting(Vector3 port, float dt)
        {
            Vector3 toPort = port - hookPosition;
            float distance = toPort.Length();

            if (distance <= StowDistance)
            {
                Phase = HookPhase.Stowed;
                return;
            }

            hookPosition += toPort / distance * MathF.Min(RetractSpeed * dt, distance);
            EmitTrail(1);
        }

        /// <summary>
        /// A few bubbles shed from the hook each frame. The chase camera sits behind the sub, so the
        /// hook flies almost straight away from the player and is seen end-on - the trail is what
        /// makes its path readable.
        /// </summary>
        private void EmitTrail(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 jitter = new Vector3(RandomSigned(), RandomSigned(), RandomSigned()) * 0.15f;

                particles.Emit(new Particle
                {
                    Position = hookPosition + jitter,
                    Velocity = new Vector3(0.0f, 0.6f, 0.0f),
                    Acceleration = new Vector3(0.0f, 0.3f, 0.0f),
                    Age = 0.0f,
                    LifeTime = 0.9f,
                    StartSize = 0.09f,
                    EndSize = 0.02f,
                    Color = new Color((byte)220, (byte)240, (byte)255, (byte)200),
                    FadeOut = true
                });
            }
        }

        private static float RandomSigned() => (float)(Random.Shared.NextDouble() * 2.0 - 1.0);

        /// <summary>Draw inside the primitive (BeginShaderMode) block so the rope and hook pick up fog and lighting.</summary>
        public void Draw(Vector3 port)
        {
            if (Phase == HookPhase.Stowed) return;

            Vector3 toHook = hookPosition - port;
            float length = toHook.Length();
            if (length < 0.05f) return;

            Vector3 dir = toHook / length; // points away from the sub, toward the hook tip
            float time = (float)Raylib.GetTime();

            // 0 = slack rope, 1 = bar taut
            float tension = Phase switch
            {
                HookPhase.Attached when Target is { IsResisting: true } => 0.8f + 0.2f * MathF.Sin(time * 28.0f),
                HookPhase.Attached => 0.55f,
                HookPhase.Flying => 0.15f,
                _ => 0.1f
            };

            Color ropeColor = LerpColor(RopeSlackColor, RopeTenseColor, tension);
            float radius = 0.06f + 0.03f * tension;
            float sag = length * 0.12f * (1.0f - tension);

            // DrawCylinderEx sets no normals, so set one ourselves or the rope shades from
            // whatever normal was left over by the last shape that was drawn.
            Rlgl.Normal3f(0.0f, 1.0f, 0.0f);

            Vector3 eye = hookPosition - dir * (0.55f * HeadScale);
            DrawRope(port, eye, sag, radius, ropeColor);
            DrawHookHead(dir, eye);
        }

        private void DrawRope(Vector3 start, Vector3 end, float sag, float radius, Color color)
        {
            Vector3 previous = start;

            for (int i = 1; i <= RopeSegments; i++)
            {
                float t = i / (float)RopeSegments;

                // Straight line plus a parabolic droop that's zero at both ends
                Vector3 point = Vector3.Lerp(start, end, t) - Vector3.UnitY * (sag * 4.0f * t * (1.0f - t));
                Raylib.DrawCylinderEx(previous, point, radius, radius, 5, color);
                previous = point;
            }
        }

        private void DrawHookHead(Vector3 dir, Vector3 eye)
        {
            // Shaft, then a barbed cone whose tip leads (and, once attached, sinks into the fish)
            Raylib.DrawCylinderEx(hookPosition - dir * (0.5f * HeadScale), hookPosition - dir * (0.1f * HeadScale),
                                  0.05f * HeadScale, 0.05f * HeadScale, 6, MetalColor);
            Raylib.DrawCylinderEx(hookPosition - dir * (0.1f * HeadScale), hookPosition + dir * (0.5f * HeadScale),
                                  0.14f * HeadScale, 0.0f, 8, MetalColor);
            Raylib.DrawSphereEx(eye, 0.09f * HeadScale, 6, 6, MetalColor);
        }

        private static Color LerpColor(Color a, Color b, float t)
        {
            t = Math.Clamp(t, 0.0f, 1.0f);
            return new Color(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t),
                255);
        }
    }
}
