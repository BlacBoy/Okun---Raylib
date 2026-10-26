using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace OkunGame
{
    /// <summary>What the cursor is currently telling the player. Add a mode here, then give it a row in CursorRenderer.Styles.</summary>
    public enum CursorMode
    {
        Navigate,       // just steering - nothing targeted
        TargetLocked,   // a hookable fish is under the cursor
        HookInFlight,   // hook has been fired and is travelling
        HookRetracting, // hook is winding back in; it can't be fired again until it's home
        FishResisting,  // hooked fish is fighting the line
        Reeling         // fish has given in and is being reeled to the sub
    }

    public enum CursorShape { Brackets, Crosshair, Diamond, Ring }

    /// <summary>Everything that makes one cursor mode look the way it does.</summary>
    public readonly record struct CursorStyle(
        CursorShape Shape,
        Color Color,
        float Size,        // half-extent in pixels
        float SpinSpeed,   // radians/sec the shape rotates (0 = static)
        float PulseSpeed); // how fast the size throbs (0 = steady)

    /// <summary>
    /// Draws the aim cursor, the ship-nose marker, and the target marker. The look of each
    /// mode lives entirely in the Styles table, so retuning a mode (or adding one) never
    /// touches the drawing code or the game logic that picks the mode.
    /// </summary>
    public class CursorRenderer
    {
        private const float LineThickness = 2.0f;

        private static readonly Dictionary<CursorMode, CursorStyle> Styles = new()
        {
            [CursorMode.Navigate]      = new CursorStyle(CursorShape.Brackets,  new Color(255, 255, 255, 210), 16.0f, 0.0f, 0.0f),
            [CursorMode.TargetLocked]  = new CursorStyle(CursorShape.Diamond,   Color.Lime,                    18.0f, 0.0f, 5.0f),
            [CursorMode.HookInFlight]  = new CursorStyle(CursorShape.Ring,      Color.Orange,                  18.0f, 7.0f, 0.0f),
            [CursorMode.HookRetracting] = new CursorStyle(CursorShape.Ring,      new Color(150, 160, 175, 230), 18.0f, -7.0f, 0.0f), // grey, spinning backwards
            [CursorMode.FishResisting] = new CursorStyle(CursorShape.Crosshair, Color.Red,                     17.0f, 0.0f, 16.0f),
            [CursorMode.Reeling]       = new CursorStyle(CursorShape.Diamond,   Color.SkyBlue,                 15.0f, 3.0f, 0.0f),
        };

        public static CursorStyle StyleFor(CursorMode mode) => Styles[mode];

        /// <summary>The aim cursor itself, drawn at the projected aim point.</summary>
        public void Draw(Vector2 position, CursorMode mode, double time)
        {
            CursorStyle style = Styles[mode];
            float size = Pulsed(style, time);
            float spin = (float)time * style.SpinSpeed;

            switch (style.Shape)
            {
                case CursorShape.Brackets:  DrawBrackets(position, size, style.Color); break;
                case CursorShape.Crosshair: DrawCrosshair(position, size, style.Color); break;
                case CursorShape.Diamond:   DrawDiamond(position, size, spin, style.Color); break;
                case CursorShape.Ring:      DrawRing(position, size, spin, style.Color); break;
            }

            Raylib.DrawCircleV(position, 2.0f, style.Color); // center dot, common to every mode
        }

        /// <summary>
        /// Small hollow marker showing where the ship's nose is actually pointing, so the
        /// player can see the cursor leading the ship and the ship catching up to it.
        /// </summary>
        public void DrawNoseMarker(Vector2 position)
        {
            Raylib.DrawCircleLines((int)position.X, (int)position.Y, 5.0f, new Color(255, 255, 255, 110));
        }

        /// <summary>
        /// Marker drawn over the targeted / hooked fish, in the current mode's color. Deliberately a
        /// different shape (open square brackets) from the cursor's own diamond, so the two never
        /// read as duplicates when the cursor is sitting right next to the fish.
        /// </summary>
        public void DrawTargetMarker(Vector2 position, CursorMode mode, double time)
        {
            CursorStyle style = Styles[mode];
            float size = 22.0f * (1.0f + 0.08f * MathF.Sin((float)time * 6.0f));
            DrawBrackets(position, size, new Color(style.Color.R, style.Color.G, style.Color.B, (byte)210));
        }

        private static float Pulsed(CursorStyle style, double time)
        {
            if (style.PulseSpeed <= 0.0f) return style.Size;
            return style.Size * (1.0f + 0.12f * MathF.Sin((float)time * style.PulseSpeed));
        }

        private static void DrawBrackets(Vector2 center, float size, Color color)
        {
            float arm = size * 0.45f;

            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    Vector2 corner = center + new Vector2(sx * size, sy * size);
                    Raylib.DrawLineEx(corner, corner + new Vector2(-sx * arm, 0.0f), LineThickness, color);
                    Raylib.DrawLineEx(corner, corner + new Vector2(0.0f, -sy * arm), LineThickness, color);
                }
            }
        }

        private static void DrawCrosshair(Vector2 center, float size, Color color)
        {
            float gap = size * 0.35f;

            Raylib.DrawLineEx(center + new Vector2(gap, 0.0f), center + new Vector2(size, 0.0f), LineThickness, color);
            Raylib.DrawLineEx(center - new Vector2(gap, 0.0f), center - new Vector2(size, 0.0f), LineThickness, color);
            Raylib.DrawLineEx(center + new Vector2(0.0f, gap), center + new Vector2(0.0f, size), LineThickness, color);
            Raylib.DrawLineEx(center - new Vector2(0.0f, gap), center - new Vector2(0.0f, size), LineThickness, color);

            Raylib.DrawCircleLines((int)center.X, (int)center.Y, size * 0.55f, color);
        }

        private static void DrawDiamond(Vector2 center, float size, float spin, Color color)
        {
            Vector2 top = center + Rotate(new Vector2(0.0f, -size), spin);
            Vector2 right = center + Rotate(new Vector2(size, 0.0f), spin);
            Vector2 bottom = center + Rotate(new Vector2(0.0f, size), spin);
            Vector2 left = center + Rotate(new Vector2(-size, 0.0f), spin);

            Raylib.DrawLineEx(top, right, LineThickness, color);
            Raylib.DrawLineEx(right, bottom, LineThickness, color);
            Raylib.DrawLineEx(bottom, left, LineThickness, color);
            Raylib.DrawLineEx(left, top, LineThickness, color);
        }

        private static void DrawRing(Vector2 center, float size, float spin, Color color)
        {
            Raylib.DrawRing(center, size - 2.0f, size, 0.0f, 360.0f, 36, color);

            // Four ticks that rotate with the spin so the ring reads as "busy"
            for (int i = 0; i < 4; i++)
            {
                float angle = spin + i * (MathF.PI / 2.0f);
                Vector2 dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                Raylib.DrawLineEx(center + dir * (size + 3.0f), center + dir * (size + 10.0f), LineThickness, color);
            }
        }

        private static Vector2 Rotate(Vector2 v, float angle)
        {
            float cos = MathF.Cos(angle);
            float sin = MathF.Sin(angle);
            return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
        }
    }
}