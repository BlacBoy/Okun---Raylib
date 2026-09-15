#version 330

in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragPosition;
in vec3 fragNormal;

uniform sampler2D texture0;
uniform vec4 colDiffuse;

// Camera
uniform vec3 viewPos;                // camera world position, set from C# each frame

// Underwater fog
uniform vec3 fogColor;
uniform float fogStart;              // distance at which fog begins to take effect
uniform float fogEnd;                // distance at which fog is fully opaque

// Lighting
uniform vec3 lightDirection;
uniform vec4 lightColor;
uniform vec4 ambientColor;

out vec4 finalColor;

void main()
{
    // ---------------------------------------------------------
    // Base material color
    // ---------------------------------------------------------
    vec4 texelColor = texture(texture0, fragTexCoord);
    vec4 baseColor = texelColor * colDiffuse * fragColor;

    // ---------------------------------------------------------
    // Surface normal
    // ---------------------------------------------------------
    vec3 normal = normalize(fragNormal);

    // ---------------------------------------------------------
    // Directional light (Lambert diffuse)
    // ---------------------------------------------------------
    // lightDirection is the direction the light TRAVELS (e.g. downward), so we negate
    // it to get the surface-to-light direction needed for the dot product.
    vec3 lightDir = normalize(-lightDirection);
    float diffuse = max(dot(normal, lightDir), 0.0);

    // ---------------------------------------------------------
    // Simple specular highlight
    // ---------------------------------------------------------
    vec3 viewDir = normalize(viewPos - fragPosition);
    vec3 reflectDir = reflect(-lightDir, normal); // reflect() wants the incident (light-travel) direction
    float specular = 0.0;
    if (diffuse > 0.0)
    {
        specular = pow(max(dot(viewDir, reflectDir), 0.0), 32.0);
    }
    vec3 specularColor = lightColor.rgb * specular * 0.25; // kept subtle underwater

    // ---------------------------------------------------------
    // Combine ambient + diffuse + specular into the lit color
    // ---------------------------------------------------------
    vec3 litColor = baseColor.rgb * (ambientColor.rgb + lightColor.rgb * diffuse) + specularColor;

    // ---------------------------------------------------------
    // Underwater fog
    // ---------------------------------------------------------
    float dist = length(viewPos - fragPosition);
    float fogFactor = clamp((fogEnd - dist) / (fogEnd - fogStart), 0.0, 1.0);

    vec3 finalRgb = mix(fogColor, litColor, fogFactor);
    finalColor = vec4(finalRgb, baseColor.a);
}
