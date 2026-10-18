using System;
using System.Numerics;
using Raylib_cs;

namespace OkunGame
{
    /// <summary>
    /// The underwater fog + lighting shader comes in two variants that share one fragment
    /// shader (fog.fs):
    ///   - ForPrimitives (fog.vs):   raylib immediate-mode shapes - fish, particles, sediment
    ///   - ForModels     (model.vs): DrawModel meshes - the submarine
    /// They need different vertex shaders because raylib feeds them transforms differently
    /// (see the comments at the top of model.vs). This class keeps both variants'
    /// uniforms in sync so Program.cs can treat them as a single "scene shader".
    /// </summary>
    public class SceneShaders
    {
        private struct Locations
        {
            public int ViewPos;
            public int FogColor;
            public int FogStart;
            public int FogEnd;
            public int LightDirection;
            public int LightColor;
            public int AmbientColor;
        }

        private readonly Locations primitiveLocs;
        private readonly Locations modelLocs;

        public Shader ForPrimitives { get; }
        public Shader ForModels { get; }

        public SceneShaders(string shaderFolder)
        {
            string fragment = $"{shaderFolder}/fog.fs";

            ForPrimitives = LoadChecked($"{shaderFolder}/fog.vs", fragment);
            ForModels = LoadChecked($"{shaderFolder}/model.vs", fragment);

            primitiveLocs = ReadLocations(ForPrimitives);
            modelLocs = ReadLocations(ForModels);
        }

        /// <summary>
        /// Sets the values that don't change frame to frame (fog + lighting) on both variants.
        /// </summary>
        public void SetEnvironment(
            Vector3 fogColor, float fogStart, float fogEnd,
            Vector3 lightDirection, Vector4 lightColor, Vector4 ambientColor)
        {
            ApplyEnvironment(ForPrimitives, primitiveLocs, fogColor, fogStart, fogEnd, lightDirection, lightColor, ambientColor);
            ApplyEnvironment(ForModels, modelLocs, fogColor, fogStart, fogEnd, lightDirection, lightColor, ambientColor);
        }

        /// <summary>
        /// Call once per frame with the camera's world position (used for fog distance
        /// and specular highlights).
        /// </summary>
        public void SetViewPosition(Vector3 cameraPosition)
        {
            Raylib.SetShaderValue(ForPrimitives, primitiveLocs.ViewPos, cameraPosition, ShaderUniformDataType.Vec3);
            Raylib.SetShaderValue(ForModels, modelLocs.ViewPos, cameraPosition, ShaderUniformDataType.Vec3);
        }

        /// <summary>
        /// Unload AFTER SubmarineModel.Unload(): raylib's UnloadModel also unloads whatever
        /// shader is attached to the model's materials, and SubmarineModel detaches ours
        /// first so it isn't freed twice.
        /// </summary>
        public void Unload()
        {
            Raylib.UnloadShader(ForPrimitives);
            Raylib.UnloadShader(ForModels);
        }

        private static Shader LoadChecked(string vertexPath, string fragmentPath)
        {
            Shader shader = Raylib.LoadShader(vertexPath, fragmentPath);

            // A shader that fails to compile/link comes back invalid, and drawing with it
            // makes everything vanish with no error. Fail loudly instead.
            if (!Raylib.IsShaderValid(shader))
            {
                throw new InvalidOperationException(
                    $"Shader failed to load or compile ({vertexPath} + {fragmentPath}). " +
                    "Check the raylib log above for the GLSL error, and that the files exist " +
                    "relative to the working directory.");
            }

            return shader;
        }

        private static Locations ReadLocations(Shader shader)
        {
            return new Locations
            {
                ViewPos = Raylib.GetShaderLocation(shader, "viewPos"),
                FogColor = Raylib.GetShaderLocation(shader, "fogColor"),
                FogStart = Raylib.GetShaderLocation(shader, "fogStart"),
                FogEnd = Raylib.GetShaderLocation(shader, "fogEnd"),
                LightDirection = Raylib.GetShaderLocation(shader, "lightDirection"),
                LightColor = Raylib.GetShaderLocation(shader, "lightColor"),
                AmbientColor = Raylib.GetShaderLocation(shader, "ambientColor")
            };
        }

        private static void ApplyEnvironment(
            Shader shader, Locations locs,
            Vector3 fogColor, float fogStart, float fogEnd,
            Vector3 lightDirection, Vector4 lightColor, Vector4 ambientColor)
        {
            Raylib.SetShaderValue(shader, locs.FogColor, fogColor, ShaderUniformDataType.Vec3);
            Raylib.SetShaderValue(shader, locs.FogStart, fogStart, ShaderUniformDataType.Float);
            Raylib.SetShaderValue(shader, locs.FogEnd, fogEnd, ShaderUniformDataType.Float);
            Raylib.SetShaderValue(shader, locs.LightDirection, lightDirection, ShaderUniformDataType.Vec3);
            Raylib.SetShaderValue(shader, locs.LightColor, lightColor, ShaderUniformDataType.Vec4);
            Raylib.SetShaderValue(shader, locs.AmbientColor, ambientColor, ShaderUniformDataType.Vec4);
        }
    }
}