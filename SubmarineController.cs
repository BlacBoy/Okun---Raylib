using System;
using System.Numerics;

namespace OkunGame
{
    /// <summary>
    /// Owns the submarine's pose (position, yaw, pitch, roll) and its speed, and advances them each frame.
    /// No Raylib in here: input arrives as a SubmarineInput and steering comes from the AimCursor, so all of
    /// this can be tested headlessly. Rendering stays in SubmarineModel.
    ///
    /// Speed works like a real vessel: holding accelerate pushes it up at a steady rate, braking pulls it
    /// back down (and, if held, into a slow reverse), and with no input the water bleeds the speed off
    /// slowly - the sub coasts to a stop rather than snapping to one.
    /// </summary>
    public class SubmarineController
    {
        // ---- Speed (units/sec) ----
        public const float MaxForwardSpeed = 8.0f;
        public const float MaxReverseSpeed = 2.0f;     // a magnitude: the slowest speed is -MaxReverseSpeed

        private const float Acceleration = 3.0f;        // units/sec^2 while accelerating (about 2.7 s from rest to full speed)
        private const float BrakeDeceleration = 4.0f;   // units/sec^2 while braking, a bit stronger than accelerating
        private const float AllStopDeceleration = 10.0f; // units/sec^2 for the all-stop key
        private const float CoastDrag = 0.35f;          // 1/sec: with no input, speed decays by e^(-0.35 t) - about 6 s from full speed to 1 u/s
        private const float StopThreshold = 0.05f;      // below this a coasting sub just stops, instead of creeping forever

        // ---- Direct controls (unchanged feel from before the controller existed) ----
        private const float VerticalSpeed = 5.0f;       // units/sec for up/down (ballast), independent of heading
        private const float RollRate = 60.0f;           // degrees/sec

        public Vector3 Position;
        public float Yaw;          // radians; turned toward the aim cursor
        public float Pitch;        // radians; turned toward the aim cursor
        public float RollDegrees;  // degrees; bank, from the player's roll input

        /// <summary>Current speed along the nose, in units/sec. Negative when reversing.</summary>
        public float Speed { get; private set; }

        /// <summary>Unit vector along the submarine's nose.</summary>
        public Vector3 Forward => AimCursor.DirectionFrom(Yaw, Pitch);

        /// <summary>0..1: how fast it's going relative to full speed (forwards or backwards). For effects like the bubble trail.</summary>
        public float SpeedFraction => Math.Clamp(MathF.Abs(Speed) / MaxForwardSpeed, 0.0f, 1.0f);

        /// <summary>Advances the submarine by one frame.</summary>
        public void Update(in SubmarineInput input, AimCursor aim, float dt)
        {
            // Turn toward the aim cursor. This doesn't depend on speed: you can swing round on the spot to
            // line up a fish, which is exactly when you've stopped to hook it.
            aim.SteerShip(ref Yaw, ref Pitch, dt);

            Position.Y += Math.Clamp(input.Vertical, -1.0f, 1.0f) * VerticalSpeed * dt;
            RollDegrees += Math.Clamp(input.Roll, -1.0f, 1.0f) * RollRate * dt;

            float speedBefore = Speed;
            Speed = NextSpeed(Speed, input, dt);

            // Move by the AVERAGE of the speed at the start and end of the frame. For steady acceleration
            // that's the exact distance, so the sub covers the same ground at 30, 60 or 144 fps.
            Position += Forward * ((speedBefore + Speed) * 0.5f * dt);
        }

        private static float NextSpeed(float speed, in SubmarineInput input, float dt)
        {
            float thrust = Math.Clamp(input.Thrust, -1.0f, 1.0f);

            if (input.AllStop)
            {
                return MoveToward(speed, 0.0f, AllStopDeceleration * dt);
            }

            if (thrust > 0.0f)
            {
                // Coming out of a reverse uses the stronger brake rate until it's moving forward again
                float rate = speed < 0.0f ? BrakeDeceleration : Acceleration;
                return MoveToward(speed, MaxForwardSpeed, rate * thrust * dt);
            }

            if (thrust < 0.0f)
            {
                float rate = speed > 0.0f ? BrakeDeceleration : Acceleration;
                return MoveToward(speed, -MaxReverseSpeed, rate * -thrust * dt);
            }

            // No input: coast. Exponential decay is frame-rate independent and can never cross zero.
            speed *= MathF.Exp(-CoastDrag * dt);
            return MathF.Abs(speed) < StopThreshold ? 0.0f : speed;
        }

        /// <summary>Moves 'current' toward 'target' by at most 'maxDelta' and never past it, however big the step.</summary>
        private static float MoveToward(float current, float target, float maxDelta)
        {
            float difference = target - current;
            if (MathF.Abs(difference) <= maxDelta) return target;
            return current + MathF.Sign(difference) * maxDelta;
        }
    }
}
