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

            // 2. Setup a 3rd-person camera that orbits just above/behind the sub
            Camera3D camera = new Camera3D();
            camera.Up = new Vector3(0.0f, 1.0f, 0.0f);
            camera.FovY = 60.0f;
            camera.Projection = CameraProjection.Perspective;

            // 3. Submarine state
            Vector3 subPosition = new Vector3(0.0f, 0.0f, 0.0f);
            float subRoll = 0.0f; // degrees, rotation around the sub's forward (Z) axis

            // Camera orbit state (relative to the submarine)
            float cameraYaw = 0.0f;                              // radians, horizontal orbit angle
            float cameraPitch = 25.0f * (MathF.PI / 180.0f);      // radians, starts looking slightly down
            float cameraDistance = 8.0f;                          // how far behind the sub
            float cameraHeight = 3.0f;                            // base height above the sub

            const float mouseSensitivity = 0.003f;
            const float minPitch = -5.0f * (MathF.PI / 180.0f);
            const float maxPitch = 80.0f * (MathF.PI / 180.0f);

            const float verticalSpeed = 5.0f;  // units/sec for W/S up-down
            const float rollSpeed = 60.0f;     // degrees/sec for A/D roll

            // Crosshair / steering reticle (drawn at screen center for now — will drive
            // sub steering once mouse-to-world aiming is wired up)
            const int centerX = screenWidth / 2;
            const int centerY = screenHeight / 2;
            const int crosshairSize = 10;
            Color crosshairColor = new Color(255, 255, 255, 200);


            Raylib.SetTargetFPS(60);

            // 4. Main Game Loop
            while (!Raylib.WindowShouldClose())
            {
                float dt = Raylib.GetFrameTime();

                // ---- Mouse look ----
                Vector2 mouseDelta = Raylib.GetMouseDelta();
                cameraYaw -= mouseDelta.X * mouseSensitivity;
                cameraPitch += mouseDelta.Y * mouseSensitivity;
                cameraPitch = Math.Clamp(cameraPitch, minPitch, maxPitch);

                // ---- W / S: submarine up and down ----
                if (Raylib.IsKeyDown(KeyboardKey.W)) subPosition.Y += verticalSpeed * dt;
                if (Raylib.IsKeyDown(KeyboardKey.S)) subPosition.Y -= verticalSpeed * dt;

                // ---- A / D: submarine roll left and right ----
                if (Raylib.IsKeyDown(KeyboardKey.A)) subRoll -= rollSpeed * dt;
                if (Raylib.IsKeyDown(KeyboardKey.D)) subRoll += rollSpeed * dt;

                // ---- Orbit the camera around the submarine ----
                Vector3 camOffset = new Vector3(
                    cameraDistance * MathF.Cos(cameraPitch) * MathF.Sin(cameraYaw),
                    cameraHeight + cameraDistance * MathF.Sin(cameraPitch),
                    cameraDistance * MathF.Cos(cameraPitch) * MathF.Cos(cameraYaw)
                );

                camera.Position = subPosition + camOffset;
                camera.Target = subPosition;

                // ---- Draw ----
                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(10, 25, 47, 255));

                Raylib.BeginMode3D(camera);

                    // Draw the submarine with its roll applied
                    Rlgl.PushMatrix();
                        Rlgl.Translatef(subPosition.X, subPosition.Y, subPosition.Z);
                        Rlgl.Rotatef(subRoll, 0.0f, 0.0f, 1.0f);
                        Raylib.DrawCube(Vector3.Zero, 2.0f, 1.5f, 4.0f, Color.Red);
                        Raylib.DrawCubeWires(Vector3.Zero, 2.0f, 1.5f, 4.0f, Color.White);
                    Rlgl.PopMatrix();

                    Raylib.DrawGrid(20, 1.0f);
                Raylib.EndMode3D();

                Raylib.DrawText("Welcome to OKUN Development!", 10, 10, 20, Color.White);
                Raylib.DrawText("Mouse: look around | W/S: up/down | A/D: roll left/right", 10, 40, 16, Color.LightGray);
                Raylib.DrawFPS(10, screenHeight - 30);
				
				// --- Draw Crosshairs / reticle at screen center
				Raylib.DrawLine(centerX - crosshairSize, centerY, centerX + crosshairSize, centerY, crosshairColor);
                Raylib.DrawLine(centerX, centerY - crosshairSize, centerX, centerY + crosshairSize, crosshairColor);
                Raylib.DrawCircleLines(centerX, centerY, 3, crosshairColor);

                Raylib.EndDrawing();
            }

            // 5. Cleanup
            Raylib.CloseWindow();
        }
    }
}