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

            // Lock the mouse into the window and hide the OS cursor. We read raw mouse movement
            // and draw our own cursor (see AimCursor / CursorRenderer).
            Raylib.DisableCursor();

            // 2. Camera — chase cam that follows the submarine's own facing
            Camera3D camera = new Camera3D();
            camera.Up = new Vector3(0.0f, 1.0f, 0.0f);
            camera.FovY = 60.0f;
            camera.Projection = CameraProjection.Perspective;

            // Underwater fog + lighting. SceneShaders holds two variants of one shader: a
            // primitive variant for raylib's immediate-mode shapes (fish, particles, sediment,
            // hook) and a model variant for the submarine. The grid and debug wireframes are
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

            // Submarine model. The scale tunes how big it looks next to the fish and the chase
            // camera; the raw model is about 4.3 x 4.7 x 7.5 units.
            const float subModelScale = 0.6f;
            SubmarineModel submarine = new SubmarineModel("Resources/Models/Sub_DD.glb", sceneShaders.ForModels, subModelScale);
            bool showSubBounds = false; // F2 toggles the debug bounding box

            // 3. Submarine state
            Vector3 subPosition = new Vector3(0.0f, 0.0f, 0.0f);
            float subYaw = 0.0f;    // radians, heading — turned toward the aim cursor
            float subPitch = 0.0f;  // radians, nose up/down — turned toward the aim cursor
            float subRoll = 0.0f;   // degrees, bank left/right — driven by A/D

            const float verticalSpeed = 5.0f;   // units/sec for W/S up-down
            const float rollSpeed = 60.0f;      // degrees/sec for A/D roll
            const float forwardSpeed = 4.0f;    // units/sec — sub cruises forward so steering has something to steer

            // Chase-camera placement, relative to the submarine's own forward direction.
            // Looking a little ahead of the sub (rather than at it) puts the sub low in the frame
            // and leaves room above it for the cursor to lead the ship.
            const float cameraDistance = 8.0f;   // how far behind the sub
            const float cameraHeight = 3.0f;     // how far above the sub
            const float cameraLookAhead = 12.0f; // how far in front of the sub the camera aims

            // 4. Aim cursor + hook
            AimCursor aim = new AimCursor();
            CursorRenderer cursorRenderer = new CursorRenderer();

            const float hookRange = 30.0f;          // how far the hook can reach / lock on
            const float lockOnAngleDegrees = 5.0f;  // how close to the cursor line a fish must be to lock
            const float lockOnAngle = lockOnAngleDegrees * (MathF.PI / 180.0f);
            const float aimDistance = 60.0f;        // how far out the cursor's world point is projected for drawing

            // 5. Fish population
            Vector3 worldBoundsMin = new Vector3(-40.0f, -20.0f, -40.0f);
            Vector3 worldBoundsMax = new Vector3(40.0f, 5.0f, 40.0f);
            const float fishPanicRadius = 6.0f;
            const int initialFishCount = 25;
            FishManager fishManager = new FishManager(worldBoundsMin, worldBoundsMax, fishPanicRadius, initialFishCount);
            bool debugFishColors = true;

            // Particles: bubbles, capture bursts and the hook's trail share one pool (all spawn-then-die);
            // ambient sediment manages itself since it wraps around instead of dying.
            ParticleSystem particleSystem = new ParticleSystem(400);
            HookLauncher hook = new HookLauncher(fishManager, particleSystem);

            BubbleEmitter bubbleEmitter = new BubbleEmitter(particleSystem);
            SedimentField sedimentField = new SedimentField(worldBoundsMin, worldBoundsMax, 180);

            fishManager.FishCaptured += capturedPosition => CaptureBurst.Spawn(particleSystem, capturedPosition);

            // 6. HUD
            HudRenderer hud = new HudRenderer(screenWidth, screenHeight);
            float maxDepthRange = worldBoundsMax.Y - worldBoundsMin.Y;

            Raylib.SetTargetFPS(60);

            // 7. Main Game Loop
            while (!Raylib.WindowShouldClose())
            {
                // Cap the frame time: a long hitch (alt-tab, window drag, the first frame after
                // loading) would otherwise be simulated as one huge step. The game just runs
                // slightly slow for that frame instead.
                float dt = MathF.Min(Raylib.GetFrameTime(), 0.1f);

                // ---- Mouse moves the aim cursor; the ship turns to face it ----
                Vector2 mouseDelta = Raylib.GetMouseDelta();
                aim.ApplyMouse(mouseDelta, subYaw, subPitch);
                if (Raylib.IsKeyPressed(KeyboardKey.C)) aim.Recenter(subYaw, subPitch);
                aim.SteerShip(ref subYaw, ref subPitch, dt);

                // ---- W / S: submarine up and down (ballast, independent of heading) ----
                if (Raylib.IsKeyDown(KeyboardKey.W)) subPosition.Y += verticalSpeed * dt;
                if (Raylib.IsKeyDown(KeyboardKey.S)) subPosition.Y -= verticalSpeed * dt;

                // ---- A / D: submarine roll left and right ----
                if (Raylib.IsKeyDown(KeyboardKey.A)) subRoll -= rollSpeed * dt;
                if (Raylib.IsKeyDown(KeyboardKey.D)) subRoll += rollSpeed * dt;

                // ---- Debug toggles ----
                if (Raylib.IsKeyPressed(KeyboardKey.F1)) debugFishColors = !debugFishColors;
                if (Raylib.IsKeyPressed(KeyboardKey.F2)) showSubBounds = !showSubBounds;

                // ---- Submarine's forward direction, from yaw & pitch ----
                Vector3 subForward = AimCursor.DirectionFrom(subYaw, subPitch);

                // Cruise forward along the nose so there's something to actually steer.
                subPosition += subForward * forwardSpeed * dt;

                // ---- Hook: lock-on along the cursor, fire on click ----
                Vector3 hookPort = submarine.LocalToWorld(SubmarineModel.HookPortLocal, subPosition, subYaw, subPitch, subRoll);
                Vector3 aimDirection = aim.Direction;

                Fish? lockedFish = fishManager.GetFishAlongRay(hookPort, aimDirection, hookRange, lockOnAngle);

                if (Raylib.IsMouseButtonPressed(MouseButton.Left) || Raylib.IsKeyPressed(KeyboardKey.H))
                {
                    hook.TryFire(hookPort, aimDirection, lockedFish, hookRange);
                }

                // ---- Fish population + hook update ----
                fishManager.Update(subPosition, dt);
                hook.Update(hookPort, dt);

                // ---- Particles ----
                bubbleEmitter.Update(subPosition, subForward, dt);
                sedimentField.Update(dt);
                particleSystem.Update(dt);

                // ---- Chase camera: sits behind and above the sub, looking ahead along its nose ----
                camera.Position = subPosition - subForward * cameraDistance + new Vector3(0.0f, cameraHeight, 0.0f);
                camera.Target = subPosition + subForward * cameraLookAhead;

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

                    // Fish, sediment, particles and the hook are immediate-mode shapes, which need
                    // the primitive variant of the shader applied via BeginShaderMode.
                    Raylib.BeginShaderMode(sceneShaders.ForPrimitives);
                        fishManager.Draw(debugFishColors);
                        sedimentField.Draw();
                        particleSystem.Draw();
                        hook.Draw(hookPort);
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

                // ---- Cursor: where the aim points, where the nose points, and what's targeted ----
                double time = Raylib.GetTime();
                CursorMode cursorMode = ResolveCursorMode(hook, lockedFish);

                Vector2 cursorScreen = ProjectToScreen(hookPort + aimDirection * aimDistance, camera, screenWidth, screenHeight);
                Vector2 noseScreen = ProjectToScreen(hookPort + subForward * aimDistance, camera, screenWidth, screenHeight);

                Fish? markedFish = hook.Phase == HookPhase.Attached ? hook.Target
                                 : cursorMode == CursorMode.TargetLocked ? lockedFish
                                 : null;
                if (markedFish != null && IsInFrontOfCamera(markedFish.Position, camera))
                {
                    cursorRenderer.DrawTargetMarker(Raylib.GetWorldToScreen(markedFish.Position, camera), cursorMode, time);
                }

                cursorRenderer.DrawNoseMarker(noseScreen);
                cursorRenderer.Draw(cursorScreen, cursorMode, time);

                Raylib.DrawFPS(10, screenHeight - 30);

                // Small, unobtrusive controls hint - still a dev build for now
                Raylib.DrawText(
                    "Mouse: aim & steer | Click/H: fire hook | C: recenter | W/S: up-down | A/D: roll | F1: fish colors | F2: sub bounds",
                    10, screenHeight - 55, 14, new Color(255, 255, 255, 140)
                );

                Raylib.EndDrawing();
            }

            // 8. Cleanup - the submarine goes before the shaders (see SceneShaders.Unload)
            submarine.Unload();
            sceneShaders.Unload();
            Raylib.CloseWindow();
        }

        /// <summary>
        /// Decides which cursor look to show. All the game-state -> cursor-mode logic lives
        /// here; how each mode actually looks is defined in CursorRenderer.Styles.
        /// </summary>
        static CursorMode ResolveCursorMode(HookLauncher hook, Fish? lockedFish)
        {
            switch (hook.Phase)
            {
                case HookPhase.Flying:
                    return CursorMode.HookInFlight;

                case HookPhase.Attached:
                    return hook.Target is { IsResisting: true } ? CursorMode.FishResisting : CursorMode.Reeling;

                default:
                    return lockedFish != null ? CursorMode.TargetLocked : CursorMode.Navigate;
            }
        }

        /// <summary>Screen position of a world point, kept inside the window so the cursor never leaves the screen.</summary>
        static Vector2 ProjectToScreen(Vector3 worldPoint, Camera3D camera, int screenWidth, int screenHeight)
        {
            const float margin = 40.0f;
            Vector2 screen = Raylib.GetWorldToScreen(worldPoint, camera);

            return new Vector2(
                Math.Clamp(screen.X, margin, screenWidth - margin),
                Math.Clamp(screen.Y, margin, screenHeight - margin)
            );
        }

        static bool IsInFrontOfCamera(Vector3 worldPoint, Camera3D camera)
        {
            Vector3 view = Vector3.Normalize(camera.Target - camera.Position);
            return Vector3.Dot(worldPoint - camera.Position, view) > 0.0f;
        }
    }
}