using Raylib_cs;

namespace OkunGame
{
    /// <summary>
    /// The one place that turns key presses into a SubmarineInput. To rebind the submarine's controls,
    /// change the keys here - nothing else in the game knows which keys do what.
    ///
    ///   Left Shift  accelerate        Left Ctrl  brake (hold on to reverse)      X  all stop
    ///   W / S       up / down         A / D      roll left / right
    /// </summary>
    public static class SubmarineInputReader
    {
        public static SubmarineInput Read()
        {
            float thrust = 0.0f;
            if (Raylib.IsKeyDown(KeyboardKey.LeftShift)) thrust += 1.0f;
            if (Raylib.IsKeyDown(KeyboardKey.LeftControl)) thrust -= 1.0f;

            float vertical = 0.0f;
            if (Raylib.IsKeyDown(KeyboardKey.W)) vertical += 1.0f;
            if (Raylib.IsKeyDown(KeyboardKey.S)) vertical -= 1.0f;

            float roll = 0.0f;
            if (Raylib.IsKeyDown(KeyboardKey.D)) roll += 1.0f;
            if (Raylib.IsKeyDown(KeyboardKey.A)) roll -= 1.0f;

            return new SubmarineInput
            {
                Thrust = thrust,
                AllStop = Raylib.IsKeyDown(KeyboardKey.X),
                Vertical = vertical,
                Roll = roll
            };
        }
    }
}
