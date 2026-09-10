// =============================================================================
// Shockwave.fx - AethonMod
// Crea una onda expansiva visual desde un punto. Distorsiona la imagen en un
// anillo que se expande. Efectivo para explosiones, slams de boss, eventos.
//
// Basado en: "Librería de Partículas para Terraria - Referencia para IA"
// Sección 30: Shader de Shockwave
// =============================================================================

sampler ScreenTexture : register(s0);
float2 ShockwaveCenter = float2(0.5, 0.5);
float ShockwaveRadius = 0.3;
float ShockwaveWidth = 0.05;
float ShockwaveStrength = 0.02;

struct VertexShaderOutput
{
    float4 Position : POSITION;
    float2 TexCoords : TEXCOORD0;
    float4 Color : COLOR0;
};

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TexCoords;

    // Distancia al centro del shockwave
    float dist = distance(uv, ShockwaveCenter);

    // Calcular offset si estamos en el anillo del shockwave
    float diff = abs(dist - ShockwaveRadius);
    if (diff < ShockwaveWidth)
    {
        // Direccion radial
        float2 dir = normalize(uv - ShockwaveCenter);

        // Intensidad maxima en el centro del anillo, decae hacia los bordes
        float intensity = 1.0 - (diff / ShockwaveWidth);
        intensity = smoothstep(0.0, 1.0, intensity);

        // Aplicar offset
        uv += dir * ShockwaveStrength * intensity;
    }

    return tex2D(ScreenTexture, uv);
}

technique DefaultTechnique
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 PixelShaderFunction();
    }
};
