// =============================================================================
// ChromaticAberration.fx - AethonMod
// Simula el defecto óptico de lentes donde los canales R, G, B no convergen
// en el mismo punto. Crea un halo RGB en bordes de objetos brillantes.
//
// Basado en: "Librería de Partículas para Terraria - Referencia para IA"
// Sección 28: Shader de Chromatic Aberration
// =============================================================================

sampler ScreenTexture : register(s0);
float AberrationAmount = 0.005;
float2 Center = float2(0.5, 0.5);

struct VertexShaderOutput
{
    float4 Position : POSITION;
    float2 TexCoords : TEXCOORD0;
    float4 Color : COLOR0;
};

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    // Vector del centro a este pixel
    float2 dir = input.TexCoords - Center;
    float dist = length(dir);
    dir = normalize(dir);

    // Cantidad de aberration basada en distancia al centro
    float amount = AberrationAmount * dist;

    // Samplear cada canal con offset diferente
    float r = tex2D(ScreenTexture, input.TexCoords + dir * amount).r;
    float g = tex2D(ScreenTexture, input.TexCoords).g;
    float b = tex2D(ScreenTexture, input.TexCoords - dir * amount).b;
    float a = tex2D(ScreenTexture, input.TexCoords).a;

    return float4(r, g, b, a);
}

technique DefaultTechnique
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 PixelShaderFunction();
    }
};
