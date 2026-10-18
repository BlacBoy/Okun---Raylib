using System;
using System.Numerics;
using Raylib_cs;

namespace OkunGame
{
    /// <summary>
    /// The submarine's visual: a real model replacing teh placeholder cube.
    /// Owns loading,shader binding, and drawing with the sub's yaw/pitch/roll,
    /// so Program.cs only has to say where the sub is and which way it's facing.
    /// 
    /// Model conventions (Resources/Models/Sub_DD.glb): +Z is forward, +Y is up, +X is right. The model's origin is at the center of the sub's hull.
    /// </summary>
    public unsafe class SubmarineModel
    {
        private Model model;
        private readonly float scale;

        /// <summary>Bounding-box size after scaling: (width X, height Y, length Z).</summary>
        public Vector3 Size{get; }

        public SubmarineModel(string modelPath, Shader modelShader, float scale)
        {
            this.scale = scale;

            model = Raylib.LoadModel(modelPath);
            if(!Raylib.IsModelValid(model))
            {
                throw new InvalidOperationException(
                    $"Could not load model '{modelPath}'. Check the path is correct relative to the working directory.");
            }

            // DrawModel uses each material's own shader (BeginShaderMode does not appy to it)
            for(int i = 0; i < model.MaterialCount; i++)
            {
                model.Materials[i].Shader = modelShader;
            }

            BoundingBox bounds = Raylib.GetModelBoundingBox(model);
            Size = (bounds.Max - bounds.Min) * scale;

            Console.WriteLine(
                $"[SubmarineModel]{model.MeshCount} meshes, {model.MaterialCount} materials, " +
                $"raw bounds {bounds.Min} .. {bounds.Max}, scaled size {Size}");
        }

        public void Draw(Vector3 position, float yawRadians, float pitchRadians, float rollDegrees)
        {
            // Built with Raymath rather than System.Numerics.Matrix4x4.Create*: raylib and
            // .NET lay matrices out transposed relatives to each other, so mixing them
            // silently misplaces the model. Raymath matrices are raylib-native.
            //
            // MatrixMultiple(a, b) applies a first, then b - the same order the old
            // Rlgl calls used: scale, roll (Z), pitch (X), yaw (Y), then translate.
            float rollRadians = rollDegrees * (MathF.PI / 180.0f);

            Matrix4x4 transform = Raymath.MatrixScale(scale, scale, scale);
            transform = Raymath.MatrixMultiply(transform, Raymath.MatrixRotateZ(rollRadians));
            transform = Raymath.MatrixMultiply(transform, Raymath.MatrixRotateX(-pitchRadians));
            transform = Raymath.MatrixMultiply(transform, Raymath.MatrixRotateY(yawRadians));
            transform = Raymath.MatrixMultiply(transform, Raymath.MatrixTranslate(position.X, position.Y, position.Z));

            model.Transform = transform;

            // The source materials are double sided (windows, guard rails), so draw the sub
            // without backface culling.
            Rlgl.DisableBackfaceCulling();
            Raylib.DrawModel(model, Vector3.Zero, 1.0f, Color.White);
            Rlgl.EnableBackfaceCulling();
        }

        public void Unload()
        {
            // UnloadModel unloads every material's shader unless it is raylib default.
            // All 10 of materials share one shader that SceneShader owns,
            // so point them back at the default shader first to avoid freeing 10 times.
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