using System;
using System.Numerics;
using Raylib_cs;

namespace OkunGame
{
    /// <summary>
    /// The submarine's visual: a real model replacing the placeholder cube. Owns loading,
    /// shader binding, and drawing with the sub's yaw / pitch / roll, so Program.cs only
    /// has to say where the sub is and which way it's facing.
    ///
    /// Model conventions (Resources/Models/Sub_DD.glb): +Z is forward, +Y is up, origin at
    /// the center of the hull - matching how the game already steers the sub.
    /// </summary>
    public unsafe class SubmarineModel
    {
        private Model model;
        private readonly float scale;

        /// <summary>Bounding-box size after scaling: (width X, height Y, length Z).</summary>
        public Vector3 Size { get; }

        public SubmarineModel(string modelPath, Shader modelShader, float scale)
        {
            this.scale = scale;

            model = Raylib.LoadModel(modelPath);
            if (!Raylib.IsModelValid(model))
            {
                throw new InvalidOperationException(
                    $"Could not load model '{modelPath}'. Check the path is correct relative to the working directory.");
            }

            // DrawModel uses each material's own shader (BeginShaderMode does not apply to it)
            for (int i = 0; i < model.MaterialCount; i++)
            {
                model.Materials[i].Shader = modelShader;
            }

            BoundingBox bounds = Raylib.GetModelBoundingBox(model);
            Size = (bounds.Max - bounds.Min) * scale;

            Console.WriteLine(
                $"[SubmarineModel] {model.MeshCount} meshes, {model.MaterialCount} materials, " +
                $"raw bounds {bounds.Min} .. {bounds.Max}, scaled size {Size}");
        }

        /// <summary>
        /// Where the hook launches from, in the model's own (unscaled) coordinates: just in
        /// front of the hull, low on the nose, below the headlights.
        /// </summary>
        public static readonly Vector3 HookPortLocal = new Vector3(0.0f, -1.5f, 3.7f);

        /// <summary>
        /// Converts a point in the model's local space to world space for a given pose, using the
        /// same rotation order the model is drawn with (scale, roll Z, pitch X, yaw Y, translate),
        /// so attachment points stay glued to the hull however the sub is tilted.
        /// </summary>
        public Vector3 LocalToWorld(Vector3 local, Vector3 position, float yawRadians, float pitchRadians, float rollDegrees)
        {
            float roll = rollDegrees * (MathF.PI / 180.0f);
            Vector3 p = local * scale;

            // Roll about Z
            p = new Vector3(p.X * MathF.Cos(roll) - p.Y * MathF.Sin(roll), p.X * MathF.Sin(roll) + p.Y * MathF.Cos(roll), p.Z);

            // Pitch about X (negated, matching how the model itself is drawn)
            float pitch = -pitchRadians;
            p = new Vector3(p.X, p.Y * MathF.Cos(pitch) - p.Z * MathF.Sin(pitch), p.Y * MathF.Sin(pitch) + p.Z * MathF.Cos(pitch));

            // Yaw about Y
            p = new Vector3(p.X * MathF.Cos(yawRadians) + p.Z * MathF.Sin(yawRadians), p.Y, -p.X * MathF.Sin(yawRadians) + p.Z * MathF.Cos(yawRadians));

            return p + position;
        }

        public void Draw(Vector3 position, float yawRadians, float pitchRadians, float rollDegrees)
        {
            // Built with Raymath rather than System.Numerics.Matrix4x4.Create*: raylib and
            // .NET lay matrices out transposed relative to each other, so mixing them
            // silently misplaces the model. Raymath matrices are raylib-native.
            //
            // MatrixMultiply(a, b) applies a first, then b - the same order the old
            // Rlgl calls used: scale, roll (Z), pitch (X), yaw (Y), then translate.
            float rollRadians = rollDegrees * (MathF.PI / 180.0f);

            Matrix4x4 transform = Raymath.MatrixScale(scale, scale, scale);
            transform = Raymath.MatrixMultiply(transform, Raymath.MatrixRotateZ(rollRadians));
            transform = Raymath.MatrixMultiply(transform, Raymath.MatrixRotateX(-pitchRadians));
            transform = Raymath.MatrixMultiply(transform, Raymath.MatrixRotateY(yawRadians));
            transform = Raymath.MatrixMultiply(transform, Raymath.MatrixTranslate(position.X, position.Y, position.Z));

            model.Transform = transform;

            // The source materials are double-sided (windows, guard rails), so draw the
            // sub without backface culling to match.
            Rlgl.DisableBackfaceCulling();
            Raylib.DrawModel(model, Vector3.Zero, 1.0f, Color.White);
            Rlgl.EnableBackfaceCulling();
        }

        public void Unload()
        {
            // UnloadModel unloads every material's shader unless it's raylib's default.
            // All 10 of our materials share one shader that SceneShaders owns, so point
            // them back at the default first - otherwise it would be freed 10 times.
            Shader defaultShader = new Shader
            {
                Id = Rlgl.GetShaderIdDefault(),
                Locs = Rlgl.GetShaderLocsDefault()
            };

            for (int i = 0; i < model.MaterialCount; i++)
            {
                model.Materials[i].Shader = defaultShader;
            }

            Raylib.UnloadModel(model);
        }
    }
}
