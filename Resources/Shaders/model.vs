#version 330

// Vertex shader for meshes drawn with DrawModel / DrawMesh (the submarine).
//
// Counterpart to fog.vs, which handles raylib's immediate-mode shapes (DrawSphere etc).
// The two differ on purpose:
//   - immediate-mode shapes arrive with transforms already baked into vertexPosition,
//     and raylib never fills in matModel / matNormal for them (fog.vs)
//   - DrawModel sends LOCAL-space vertices plus real matModel / matNormal uniforms,
//     so we transform position and normal ourselves (this file)
// Both feed the same fog.fs fragment shader.

in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec4 vertexColor;

uniform mat4 mvp;
uniform mat4 matModel;
uniform mat4 matNormal;   // transpose(inverse(matModel)), supplied by raylib

out vec2 fragTexCoord;
out vec4 fragColor;
out vec3 fragPosition;
out vec3 fragNormal;

void main()
{
    fragTexCoord = vertexTexCoord;
    fragColor = vertexColor;

    // World-space position (needed for fog distance + specular)
    fragPosition = vec3(matModel * vec4(vertexPosition, 1.0));

    // World-space normal - this is what finally makes lighting follow the sub's
    // yaw / pitch / roll, which the primitive-cube version could not do.
    fragNormal = normalize(mat3(matNormal) * vertexNormal);

    gl_Position = mvp * vec4(vertexPosition, 1.0);
}
