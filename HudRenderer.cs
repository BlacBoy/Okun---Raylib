using System;
using System.Numerics;
using Raylib_cs;

namespace OkunGame
{
    /// <summary>
    /// Draws all 2D HUD elements: depth gauge, fish-caught counter, and the aiming
    /// reticle. Kept separate from Program.cs so HUD layout/style changes don't touch
    /// gameplay code, and so future HUD elements (oxygen, weapon charge, etc.) have an
    /// obvious place to live. Everything here is vector-drawn (no texture assets yet).
    /// </summary>
    public class HudRenderer
    {
        private readonly int screenWidth;
        private readonly int screenHeight;

        // Depth gauge layout
        private const int GaugeX = 30;
        private const int GaugeY = 100;
        private const int GaugeWidth = 18;
        private const int GaugeHeight = 360;

        // Reticle style
        private const int ReticleSize = 14;
        private const int ReticleBracket = 6;

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
            Raylib.DrawText($"{populationCount} nearby", panelX + 8, panelY + 34, 12, Color.LightGray);
        }

        /// <summary>
        /// Corner-bracket reticle at screen center. Turns green when a hookable fish is
        /// within range, so the HUD gives real feedback rather than being purely
        /// decorative. Note this reflects distance-to-submarine (matching the debug 'H'
        /// hook test), not a true aim raycast through the reticle - that distinction
        /// matters once real aiming/weapons exist.
        /// </summary>
        public void DrawReticle(bool targetLocked)
        {
            int cx = screenWidth / 2;
            int cy = screenHeight / 2;
            Color color = targetLocked ? Color.Lime : new Color(255, 255, 255, 200);

            DrawBracket(cx - ReticleSize, cy - ReticleSize, 1, 1, color);   // top-left
            DrawBracket(cx + ReticleSize, cy - ReticleSize, -1, 1, color);  // top-right
            DrawBracket(cx - ReticleSize, cy + ReticleSize, 1, -1, color);  // bottom-left
            DrawBracket(cx + ReticleSize, cy + ReticleSize, -1, -1, color); // bottom-right

            Raylib.DrawCircle(cx, cy, 2, color);
        }

        private void DrawBracket(int x, int y, int dirX, int dirY, Color color)
        {
            Raylib.DrawLine(x, y, x + dirX * ReticleBracket, y, color);
            Raylib.DrawLine(x, y, x, y + dirY * ReticleBracket, color);
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