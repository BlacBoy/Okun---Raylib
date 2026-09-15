#version 330

// Input vertex attributes
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec4 vertexColor;

// Raylib matrices
uniform mat4 mvp;

// Outputs to fragment shader
out vec2 fragTexCoord;
out vec4 fragColor;
out vec3 fragPosition;
out vec3 fragNormal;

void main()
{
    fragTexCoord = vertexTexCoord;
    fragColor = vertexColor;

    // Raylib's immediate-mode batch renderer (DrawCube/DrawSphere/DrawGrid) bakes each
    // shape's model transform into vertexPosition on the CPU before it ever reaches this
    // shader - vertexPosition here is already world-space. matModel/matNormal are only
    // populated for DrawModel/DrawMesh-based rendering, NOT for these primitive draw
    // calls - reading them here would pull back all-zero matrices and break everything.
    //
    // NOTE: normals are NOT baked/rotated the same way - rlNormal3f stores them as-is,
    // untouched by any rotation on the matrix stack (Rlgl.Rotatef etc). Fine for shapes
    // that are never rotated (DrawSphere, DrawGrid), but the submarine cube (drawn
    // inside a Rlgl.PushMatrix/Rotatef block) will shade using its *unrotated* normals
    // until it's replaced with a real model.
    fragPosition = vertexPosition;
    fragNormal = vertexNormal;

    gl_Position = mvp * vec4(vertexPosition, 1.0);
}