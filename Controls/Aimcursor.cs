using System;
using System.Numerics;

namespace OkunGame
{
    /// <summary>
    /// The movable steering cursor. The mouse doesn't move a screen position directly - it
    /// moves an AIM DIRECTION in the world (a yaw and a pitch). The cursor you see is that
    /// direction projected onto the screen, and the submarine turns toward it at a limited
    /// turn rate, so:
    ///   - move the mouse and the cursor moves with it, ahead of the Sub
    ///   - the Sub swings round to face where the cursor points
    ///   - as it catches up, the cursor settles back onto the nose and the Sub stops turning
    ///
    /// The aim can only lead the Sub by a limited cone so the cursor always stays on screen.
    /// The same direction is what the hook launcher aims along, so "where the cursor points"
    /// is both where the Sub is heading and where it will shoot.
    ///
    /// Angle convention matches the submarine: forward = (sin yaw * cos pitch, sin pitch,
    /// cos yaw * cos pitch), so yaw 0 faces +Z and positive yaw turns toward +X (screen left,
    /// as seen from the chase camera).
    /// </summary>
    public class AimCursor
    {
        private const float DegToRad = MathF.PI / 180.0f;

        // How far the aim may lead the Sub (keeps the cursor inside the camera's view)
        private const float MaxYawOffset = 35.0f * DegToRad;
        private const float MaxPitchOffset = 20.0f * DegToRad;

        // Mouse feel
        private const float MouseSensitivity = 0.0025f; // radians of aim per pixel of mouse travel

        // Ship turning: proportional to how far off-target it is, capped at a max rate
        private const float TurnGain = 3.0f;                  // 1/s - higher = snappier
        private const float MaxYawRate = 100.0f * DegToRad;   // radians/sec
        private const float MaxPitchRate = 70.0f * DegToRad;  // radians/sec
        private const float MaxPitch = 80.0f * DegToRad;      // Sub and aim never go past this

        public float AimYaw { get; private set; }
        public float AimPitch { get; private set; }

        /// <summary>Unit vector in the world pointing where the cursor points.</summary>
        public Vector3 Direction => DirectionFrom(AimYaw, AimPitch);

        /// <summary>Moves the aim by a mouse delta (in pixels), keeping it within the cone ahead of the Sub.</summary>
        public void ApplyMouse(Vector2 mouseDelta, float subYaw, float subPitch)
        {
            // Work in offsets relative to the Sub (always small) rather than absolute angles.
            // Adding a big mouse flick to an absolute angle and then wrapping it can land on the
            // far side of the circle and clamp the wrong way; a relative offset just saturates.
            // Same sign convention the old direct mouse-look used: mouse right turns right.
            float yawOffset = WrapPi(AimYaw - subYaw) - mouseDelta.X * MouseSensitivity;
            float pitchOffset = (AimPitch - subPitch) - mouseDelta.Y * MouseSensitivity;

            yawOffset = Math.Clamp(yawOffset, -MaxYawOffset, MaxYawOffset);
            pitchOffset = Math.Clamp(pitchOffset, -MaxPitchOffset, MaxPitchOffset);

            // Re-anchor on the Sub so the angles never drift unbounded
            AimYaw = subYaw + yawOffset;
            AimPitch = Math.Clamp(subPitch + pitchOffset, -MaxPitch, MaxPitch);
        }

        /// <summary>Turns the Sub toward the aim. Call once per frame.</summary>
        public void SteerShip(ref float subYaw, ref float subPitch, float dt)
        {
            float yawError = WrapPi(AimYaw - subYaw);
            float pitchError = AimPitch - subPitch;

            float yawStep = Math.Clamp(yawError * TurnGain, -MaxYawRate, MaxYawRate) * dt;
            float pitchStep = Math.Clamp(pitchError * TurnGain, -MaxPitchRate, MaxPitchRate) * dt;

            // Never step past the aim, however long the frame was. Without this, one long frame
            // (a hitch, alt-tab, the first frame after loading) swings the Sub past its target,
            // and the overshoot then drags the aim along with it.
            yawStep = Math.Clamp(yawStep, -MathF.Abs(yawError), MathF.Abs(yawError));
            pitchStep = Math.Clamp(pitchStep, -MathF.Abs(pitchError), MathF.Abs(pitchError));

            subYaw += yawStep;
            subPitch = Math.Clamp(subPitch + pitchStep, -MaxPitch, MaxPitch);
        }

        /// <summary>Snaps the aim back onto the Sub's nose (the Sub stops turning).</summary>
        public void Recenter(float subYaw, float subPitch)
        {
            AimYaw = subYaw;
            AimPitch = subPitch;
        }

        public static Vector3 DirectionFrom(float yaw, float pitch)
        {
            return new Vector3(
                MathF.Sin(yaw) * MathF.Cos(pitch),
                MathF.Sin(pitch),
                MathF.Cos(yaw) * MathF.Cos(pitch)
            );
        }

        private static float WrapPi(float angle)
        {
            while (angle > MathF.PI) angle -= 2.0f * MathF.PI;
            while (angle < -MathF.PI) angle += 2.0f * MathF.PI;
            return angle;
        }
    }
}