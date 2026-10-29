namespace OkunGame
{
    /// <summary>
    /// What the player is asking the submarine to do this frame, as plain numbers. Keeping this separate
    /// from the keyboard means SubmarineController never touches Raylib, so it can be tested without a
    /// window, and changing a key binding only ever touches SubmarineInputReader.
    /// </summary>
    public struct SubmarineInput
    {
        /// <summary>-1..1. +1 accelerate, -1 brake (and then reverse), 0 = no input, so the sub coasts.</summary>
        public float Thrust;

        /// <summary>Held: brake hard to a standstill (never into reverse).</summary>
        public bool AllStop;

        /// <summary>-1..1. +1 = up, -1 = down.</summary>
        public float Vertical;

        /// <summary>-1..1. +1 = roll right, -1 = roll left.</summary>
        public float Roll;
    }
}
