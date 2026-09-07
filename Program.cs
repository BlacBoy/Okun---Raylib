using System;
using System.Numerics;
using Raylib_cs;

namespace OkunGame
{
    class Program
    {
        static void Main(string[] args)
        {
            // 1. Initialize the window
            const int screenWidth = 1280;
            const int screenHeight = 720;
            Raylib.InitWindow(screenWidth, screenHeight, "Okun: Submarine Odyssey (Test Render)");

            // Lock the mouse into the window and hide the cursor so we can read raw deltas
            Raylib.DisableCursor();

            // 2. Camera — chase cam that follows the submarine's own facing
            Camera3D camera = new Camera3D();
            camera.Up = new Vector3(0.0f, 1.0f, 0.0f);
            camera.FovY = 60.0f;
            camera.Projection = CameraProjection.Perspective;

            // 3. Submarine state
            Vector3 subPosition = new Vector3(0.0f, 0.0f, 0.0f);
            float subYaw = 0.0f;    // radians, heading — driven by mouse X (steering)
            float subPitch = 0.0f;  // radians, nose up/down — driven by mouse Y (steering)
            float subRoll = 0.0f;   // degrees, bank left/right — driven by A/D

            const float mouseSensitivity = 0.0025f;
            const float minPitch = -80.0f * (MathF.PI / 180.0f);
            const float maxPitch = 80.0f * (MathF.PI / 180.0f);

            const float verticalSpeed = 5.0f;   // units/sec for W/S up-down
            const float rollSpeed = 60.0f;      // degrees/sec for A/D roll
            const float forwardSpeed = 4.0f;    // units/sec — sub cruises forward so steering has something to steer

            // Chase-camera placement, relative to the submarine's own forward direction
            const float cameraDistance = 8.0f;  // how far behind the sub
            const float cameraHeight = 3.0f;    // how far above the sub

            // Crosshair / steering reticle — sits at screen center, matches the sub's nose
            const int centerX = screenWidth / 2;
            const int centerY = screenHeight / 2;
            const int crosshairSize = 10;
            Color crosshairColor = new Color(255, 255, 255, 200);

            // 4. Fish population
            Vector3 worldBoundsMin = new Vector3(-40.0f, -20.0f, -40.0f);
            Vector3 worldBoundsMax = new Vector3(40.0f, 5.0f, 40.0f);
            const float fishPanicRadius = 6.0f;
            const int initialFishCount = 25;
            FishManager fishManager = new FishManager(worldBoundsMin, worldBoundsMax, fishPanicRadius, initialFishCount);

            // Debug-only, until a real harpoon weapon exists to call Fish.Hook() itself
            const float hookTestRange = 10.0f;
            bool debugFishColors = true;

            Raylib.SetTargetFPS(60);

            // 5. Main Game Loop
            while (!Raylib.WindowShouldClose())
            {
                float dt = Raylib.GetFrameTime();

                // ---- Mouse steers the submarine's heading (yaw) and nose pitch ----
                Vector2 mouseDelta = Raylib.GetMouseDelta();
                subYaw -= mouseDelta.X * mouseSensitivity;
                subPitch -= mouseDelta.Y * mouseSensitivity;
                subPitch = Math.Clamp(subPitch, minPitch, maxPitch);

                // ---- W / S: submarine up and down (ballast, independent of heading) ----
                if (Raylib.IsKeyDown(KeyboardKey.W)) subPosition.Y += verticalSpeed * dt;
                if (Raylib.IsKeyDown(KeyboardKey.S)) subPosition.Y -= verticalSpeed * dt;

                // ---- A / D: submarine roll left and right ----
                if (Raylib.IsKeyDown(KeyboardKey.A)) subRoll -= rollSpeed * dt;
                if (Raylib.IsKeyDown(KeyboardKey.D)) subRoll += rollSpeed * dt;

                // ---- Debug: toggle fish state colors / test-hook the nearest fish ----
                if (Raylib.IsKeyPressed(KeyboardKey.F1)) debugFishColors = !debugFishColors;

                if (Raylib.IsKeyPressed(KeyboardKey.H))
                {
                    Fish target = fishManager.GetNearestFish(subPosition, hookTestRange);
                    target?.Hook();
                }

                // ---- Submarine's forward direction, from yaw & pitch ----
                Vector3 subForward = new Vector3(
                    MathF.Sin(subYaw) * MathF.Cos(subPitch),
                    MathF.Sin(subPitch),
                    MathF.Cos(subYaw) * MathF.Cos(subPitch)
                );

                // Cruise forward along the nose so there's something to actually steer.
                subPosition += subForward * forwardSpeed * dt;

                // ---- Fish population update ----
                fishManager.Update(subPosition, dt);

                // ---- Chase camera: sits behind and above the sub, looking along its nose ----
                camera.Position = subPosition - subForward * cameraDistance + new Vector3(0.0f, cameraHeight, 0.0f);
                camera.Target = subPosition + subForward;

                // Bank the camera's up vector with the sub's roll for a bit of feel
                float rollRad = subRoll * (MathF.PI / 180.0f);
                camera.Up = new Vector3(MathF.Sin(rollRad), MathF.Cos(rollRad), 0.0f);

                // ---- Draw ----
                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(10, 25, 47, 255));

                Raylib.BeginMode3D(camera);

                    // Draw the submarine, oriented to match its yaw / pitch / roll
                    Rlgl.PushMatrix();
                        Rlgl.Translatef(subPosition.X, subPosition.Y, subPosition.Z);
                        Rlgl.Rotatef(subYaw * (180.0f / MathF.PI), 0.0f, 1.0f, 0.0f);
                        Rlgl.Rotatef(-subPitch * (180.0f / MathF.PI), 1.0f, 0.0f, 0.0f);
                        Rlgl.Rotatef(subRoll, 0.0f, 0.0f, 1.0f);
                        Raylib.DrawCube(Vector3.Zero, 2.0f, 1.5f, 4.0f, Color.Red);
                        Raylib.DrawCubeWires(Vector3.Zero, 2.0f, 1.5f, 4.0f, Color.White);
                    Rlgl.PopMatrix();

                    Raylib.DrawGrid(40, 1.0f);

                    fishManager.Draw(debugFishColors);

                Raylib.EndMode3D();

                Raylib.DrawText("Welcome to OKUN Development!", 10, 10, 20, Color.White);
                Raylib.DrawText("Mouse: steer heading | W/S: up/down | A/D: roll left/right", 10, 40, 16, Color.LightGray);
                Raylib.DrawText("H: hook nearest fish (test) | F1: toggle fish debug colors", 10, 60, 16, Color.LightGray);
                Raylib.DrawText($"Fish: {fishManager.Count}   Caught: {fishManager.CaughtCount}", 10, 90, 18, Color.White);
                Raylib.DrawFPS(10, screenHeight - 30);

                // Crosshair / reticle — shows the sub's current heading
                Raylib.DrawLine(centerX - crosshairSize, centerY, centerX + crosshairSize, centerY, crosshairColor);
                Raylib.DrawLine(centerX, centerY - crosshairSize, centerX, centerY + crosshairSize, crosshairColor);
                Raylib.DrawCircleLines(centerX, centerY, 3, crosshairColor);

                Raylib.EndDrawing();
            }

            // 6. Cleanup
            Raylib.CloseWindow();
        }
    }
}