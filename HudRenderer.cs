using System;
using System.Numerics;
using Raylib_cs;

namespace OkunGame
{
    /// <summary>
    /// Draws the static 2D HUD elements: depth gauge, speed gauge and fish-caught counter. The aim
    /// cursor lives in CursorRenderer. Kept separate from Program.cs so HUD layout/style changes don't touch
    /// gameplay code, and so future HUD elements (oxygen, weapon charge, etc.) have an
    /// obvious place to live. Everything here is vector-drawn (no texture assets yet).
    /// </summary>
    public class HudRenderer
    {
        private readonly int screenWidth;
        private readonly int screenHeight;

        // Speed gauge sits on the right edge, mirroring the depth gauge on the left
        private const int SpeedGaugeRightMargin = 30;

        // Depth gauge layout
        private const int GaugeX = 30;
        private const int GaugeY = 100;
        private const int GaugeWidth = 18;
        private const int GaugeHeight = 360;


        public HudRenderer(int screenWidth, int screenHeight)
        {
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
        }

        /// <summary>
        /// Vertical depth gauge on the left edge of the screen. currentDepth and
        /// maxDepth should both be in the same units; 0 = surface, growing downward.
        /// </summary>
        public void DrawDepthGauge(float currentDepth, float maxDepth)
        {
            float fraction = Math.Clamp(currentDepth / maxDepth, 0.0f, 1.0f);

            // Track background
            Raylib.DrawRectangle(GaugeX, GaugeY, GaugeWidth, GaugeHeight, new Color(0, 0, 0, 120));
            Raylib.DrawRectangleLines(GaugeX, GaugeY, GaugeWidth, GaugeHeight, new Color(255, 255, 255, 160));

            // Filled portion, growing downward from the top (surface) as depth increases
            int fillHeight = (int)(GaugeHeight * fraction);
            Raylib.DrawRectangle(GaugeX, GaugeY, GaugeWidth, fillHeight, Color.SkyBlue);

            // Current-depth marker
            int markerY = GaugeY + fillHeight;
            Raylib.DrawTriangle(
                new Vector2(GaugeX + GaugeWidth + 4, markerY),
                new Vector2(GaugeX + GaugeWidth + 12, markerY - 5),
                new Vector2(GaugeX + GaugeWidth + 12, markerY + 5),
                Color.White
            );

            Raylib.DrawText("DEPTH", GaugeX - 4, GaugeY - 24, 14, Color.LightGray);
            Raylib.DrawText($"{currentDepth:0}m", GaugeX + GaugeWidth + 16, markerY - 8, 16, Color.White);
        }

        /// <summary>
        /// Vertical speed gauge on the right edge. The zero line is marked; forward speed fills upward from it
        /// (blue) and reverse fills downward (orange).
        /// </summary>
        public void DrawSpeedGauge(float speed, float minSpeed, float maxSpeed)
        {
            int x = screenWidth - SpeedGaugeRightMargin - GaugeWidth;
            int bottom = GaugeY + GaugeHeight;
            float range = maxSpeed - minSpeed;

            float clamped = Math.Clamp(speed, minSpeed, maxSpeed);
            int zeroY = bottom - (int)(GaugeHeight * (0.0f - minSpeed) / range);
            int speedY = bottom - (int)(GaugeHeight * (clamped - minSpeed) / range);

            Raylib.DrawRectangle(x, GaugeY, GaugeWidth, GaugeHeight, new Color(0, 0, 0, 120));

            if (speedY < zeroY) Raylib.DrawRectangle(x, speedY, GaugeWidth, zeroY - speedY, Color.SkyBlue);
            else if (speedY > zeroY) Raylib.DrawRectangle(x, zeroY, GaugeWidth, speedY - zeroY, Color.Orange);

            Raylib.DrawRectangleLines(x, GaugeY, GaugeWidth, GaugeHeight, new Color(255, 255, 255, 160));
            Raylib.DrawLine(x - 6, zeroY, x + GaugeWidth + 6, zeroY, new Color(255, 255, 255, 200)); // zero line

            // Current-speed marker, pointing at the gauge from the left
            Raylib.DrawTriangle(
                new Vector2(x - 4, speedY),
                new Vector2(x - 12, speedY + 5),
                new Vector2(x - 12, speedY - 5),
                Color.White
            );

            string label = $"{speed:0.0} m/s";
            Raylib.DrawText(label, x - 18 - Raylib.MeasureText(label, 16), speedY - 8, 16, Color.White);
            Raylib.DrawText("SPEED", x - 6, GaugeY - 24, 14, Color.LightGray);
        }

        /// <summary>
        /// Fish-caught counter, top-right, with a small vector fish icon instead of a
        /// texture asset.
        /// </summary>
        public void DrawFishCounter(int caughtCount, int populationCount)
        {
            const int panelWidth = 150;
            const int panelHeight = 50;
            int panelX = screenWidth - panelWidth - 20;
            int panelY = 20;

            Raylib.DrawRectangle(panelX, panelY, panelWidth, panelHeight, new Color(0, 0, 0, 120));
            Raylib.DrawRectangleLines(panelX, panelY, panelWidth, panelHeight, new Color(255, 255, 255, 160));

            DrawFishIcon(panelX + 20, panelY + 25, 10, Color.SkyBlue);

            Raylib.DrawText($"x {caughtCount}", panelX + 42, panelY + 12, 22, Color.White);
            Raylib.DrawText($"{populationCount} in world", panelX + 8, panelY + 34, 12, Color.LightGray);
        }

        private void DrawFishIcon(int x, int y, float size, Color color)
        {
            // Simple side-profile fish: triangular body + tail, no texture needed
            Raylib.DrawTriangle(
                new Vector2(x - size, y),
                new Vector2(x + size * 0.6f, y - size * 0.6f),
                new Vector2(x + size * 0.6f, y + size * 0.6f),
                color
            );
            Raylib.DrawTriangle(
                new Vector2(x + size * 0.6f, y - size * 0.6f),
                new Vector2(x + size * 1.4f, y),
                new Vector2(x + size * 0.6f, y + size * 0.6f),
                color
            );
        }
    }
}
