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

            // Underwater fog + lighting. SceneShaders holds two variants of one shader: a
            // primitive variant for raylib's immediate-mode shapes (fish, particles, sediment)
            // and a model variant for the submarine. The grid and debug wireframes are
            // deliberately drawn outside any shader (see DEBUG GEOMETRY below) so they stay
            // visible regardless of shader state.
            SceneShaders sceneShaders = new SceneShaders("Resources/Shaders");

            Vector3 fogColor = new Vector3(0.04f, 0.12f, 0.20f); // matches the deep-ocean clear color
            const float fogStart = 10.0f;  // fog begins fading things out past this distance
            const float fogEnd = 45.0f;    // fully fogged out (invisible into the murk) past this distance

            // ---------------------------------------------------------
            // Basic underwater lighting
            // ---------------------------------------------------------
            Vector3 lightDirection = Vector3.Normalize(new Vector3(-0.35f, -1.0f, -0.25f));
            Vector4 lightColor = new Vector4(0.65f, 0.85f, 1.0f, 1.0f);
            Vector4 ambientColor = new Vector4(0.08f, 0.14f, 0.18f, 1.0f);

            sceneShaders.SetEnvironment(fogColor, fogStart, fogEnd, lightDirection, lightColor, ambientColor);

            // Submarine model (replaces the placeholder cube). The scale tunes how big it looks
            // next to the fish and the chase camera; the raw model is about 4.3 x 4.7 x 7.5 units.
            const float subModelScale = 0.6f;
            SubmarineModel submarine = new SubmarineModel("Resources/Models/Sub_DD.glb", sceneShaders.ForModels, subModelScale);
            bool showSubBounds = false; // F2 toggles the debug bounding box

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

            // 4. Fish population
            Vector3 worldBoundsMin = new Vector3(-40.0f, -20.0f, -40.0f);
            Vector3 worldBoundsMax = new Vector3(40.0f, 5.0f, 40.0f);
            const float fishPanicRadius = 6.0f;
            const int initialFishCount = 25;
            FishManager fishManager = new FishManager(worldBoundsMin, worldBoundsMax, fishPanicRadius, initialFishCount);

            // Debug-only, until a real harpoon weapon exists to call Fish.Hook() itself
            const float hookTestRange = 10.0f;
            bool debugFishColors = true;

            // Particles: bubbles + capture bursts share one pool (both spawn-then-die);
            // ambient sediment manages itself since it wraps around instead of dying.
            ParticleSystem particleSystem = new ParticleSystem(400);
            BubbleEmitter bubbleEmitter = new BubbleEmitter(particleSystem);
            SedimentField sedimentField = new SedimentField(worldBoundsMin, worldBoundsMax, 180);

            fishManager.FishCaptured += capturedPosition => CaptureBurst.Spawn(particleSystem, capturedPosition);

            // 5. HUD
            HudRenderer hud = new HudRenderer(screenWidth, screenHeight);
            float maxDepthRange = worldBoundsMax.Y - worldBoundsMin.Y;

            Raylib.SetTargetFPS(60);

            // 6. Main Game Loop
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

                // ---- Debug: toggle fish state colors ----
                if (Raylib.IsKeyPressed(KeyboardKey.F1)) debugFishColors = !debugFishColors;
                if (Raylib.IsKeyPressed(KeyboardKey.F2)) showSubBounds = !showSubBounds;

                // Nearest hookable fish - reused for both the debug hook-test key and
                // the reticle's target-lock feedback, so we only search once per frame.
                Fish? nearestHookable = fishManager.GetNearestFish(subPosition, hookTestRange);

                if (Raylib.IsKeyPressed(KeyboardKey.H))
                {
                    nearestHookable?.Hook();
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

                // ---- Particles ----
                bubbleEmitter.Update(subPosition, subForward, dt);
                sedimentField.Update(dt);
                particleSystem.Update(dt);

                // ---- Chase camera: sits behind and above the sub, looking along its nose ----
                camera.Position = subPosition - subForward * cameraDistance + new Vector3(0.0f, cameraHeight, 0.0f);
                camera.Target = subPosition + subForward;

                // Bank the camera's up vector with the sub's roll for a bit of feel
                float rollRad = subRoll * (MathF.PI / 180.0f);
                camera.Up = new Vector3(MathF.Sin(rollRad), MathF.Cos(rollRad), 0.0f);

                // Keep both shader variants' camera-position uniform current every frame
                sceneShaders.SetViewPosition(camera.Position);

                // ---- Draw ----
                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(10, 25, 47, 255));

                Raylib.BeginMode3D(camera);

                    // The submarine is a real model. DrawModel uses each material's own shader
                    // (the model variant, bound in SubmarineModel), so no BeginShaderMode here.
                    submarine.Draw(subPosition, subYaw, subPitch, subRoll);

                    // Fish, sediment, and particles are immediate-mode shapes, which need the
                    // primitive variant of the shader applied via BeginShaderMode.
                    Raylib.BeginShaderMode(sceneShaders.ForPrimitives);
                        fishManager.Draw(debugFishColors);
                        sedimentField.Draw();
                        particleSystem.Draw();
                    Raylib.EndShaderMode();

                // =========================================================
                // DEBUG GEOMETRY (unlit - wireframes/grid stay outside the shader
                // so they're always visible regardless of shader state)
                // =========================================================
                if (showSubBounds)
                {
                    Rlgl.PushMatrix();
                        Rlgl.Translatef(subPosition.X, subPosition.Y, subPosition.Z);
                        Rlgl.Rotatef(subYaw * (180.0f / MathF.PI), 0.0f, 1.0f, 0.0f);
                        Rlgl.Rotatef(-subPitch * (180.0f / MathF.PI), 1.0f, 0.0f, 0.0f);
                        Rlgl.Rotatef(subRoll, 0.0f, 0.0f, 1.0f);
                        Raylib.DrawCubeWires(Vector3.Zero, submarine.Size.X, submarine.Size.Y, submarine.Size.Z, Color.White);
                    Rlgl.PopMatrix();
                }

                Raylib.DrawGrid(40, 1.0f);

                Raylib.EndMode3D();

                // ---- HUD ----
                float currentDepth = worldBoundsMax.Y - subPosition.Y;
                hud.DrawDepthGauge(currentDepth, maxDepthRange);
                hud.DrawFishCounter(fishManager.CaughtCount, fishManager.Count);
                hud.DrawReticle(nearestHookable != null);
                Raylib.DrawFPS(10, screenHeight - 30);

                // Small, unobtrusive controls hint - still a dev build for now
                Raylib.DrawText(
                    "Mouse: steer | W/S: up-down | A/D: roll | H: hook (test) | F1: fish colors | F2: sub bounds",
                    10, screenHeight - 55, 14, new Color(255, 255, 255, 140)
                );

                Raylib.EndDrawing();
            }

            // 7. Cleanup - the submarine goes before the shaders (see SceneShaders.Unload)
            submarine.Unload();
            sceneShaders.Unload();
            Raylib.CloseWindow();
        }
    }
}