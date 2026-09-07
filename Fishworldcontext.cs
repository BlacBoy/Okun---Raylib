using System.Numerics;

namespace OkunGame
{
    /// <summary>
    /// Read-only snapshot of the world, passed into fish states each frame so they can
    /// react to the submarine's position and stay within the play area, without needing
    /// a reference back to Program or the submarine itself.
    /// </summary>
    public struct FishWorldContext
    {
        public Vector3 SubmarinePosition;
        public float SubmarinePanicRadius;
        public Vector3 BoundsMin;
        public Vector3 BoundsMax;
    }
}