using System;
using System.Numerics;

namespace OkunGame
{
    ///<summary>Small camera helpers to help with debugging</summary>
    public static class CameraMath
    {
        /// <summary>
        /// The camera's up vector, banked by 'rollRadians' about the camera's OWN forward axis.
        ///
        /// The old code built the up vector as (sin roll, cos roll, 0), which rolls about the world Z
        /// axis. That only matches the camera's real forward axis while the sub is facing +Z; at +-90
        /// degrees of heading the bank disappeared entirely, and facing -Z it reversed. Rotating the
        /// camera's own up about its own forward gives the same bank at every heading and pitch.
        /// </summary>
        public static Vector3 BankedUp(Vector3 forward, float rollRadians)
        {
            // The canera's un-rolled up: world up, made perpendicular to the view direction.
            Vector3 up = Vector3.UnitY - forward * Vector3.Dot(forward, Vector3.UnitY);

            // Looking (almost) straight up or down leaves no horizon to define "up" from
            if(up.LengthSquared() < 1e-6f)
            {
                up = Vector3.UnitZ - forward * Vector3.Dot(forward, Vector3.UnitZ);
            }

            up = Vector3.Normalize(up);
            Vector3 side = Vector3.Cross(forward, up);

            // Sign chosen so the bank direction matches what the game already does (positive roll is clockwise when looking down the forward axis)
            return up * MathF.Cos(rollRadians) - side * MathF.Sin(rollRadians);
        }
    }
}