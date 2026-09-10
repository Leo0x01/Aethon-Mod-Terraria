// =============================================================================
// Bloom.fx - AethonMod
// Hace que las áreas brillantes "sangren" luz hacia las áreas oscuras adyacentes.
// Requiere: extraer bright → blur horizontal → blur vertical + combine.
//
// Basado en: "Librería de Partículas para Terraria - Referencia para IA"
// Sección 27: Shader de Bloom
// =============================================================================

sampler ScreenTexture : register(s0);
float BloomThreshold = 0.8;
float BloomIntensity = 1.0;
float2 TexelSize;

struct VertexShaderOutput
{
    float4 Position : POSITION;
    float2 TexCoords : TEXCOORD0;
    float4 Color : COLOR0;
};

// Paso 1: extraer areas brillantes
float4 ExtractBright(VertexShaderOutput input) : COLOR0
{
    float4 color = tex2D(ScreenTexture, input.TexCoords);
    float brightness = dot(color.rgb, float3(0.299, 0.587, 0.114)); // Luma
    return brightness > BloomThreshold ? color * BloomIntensity : float4(0, 0, 0, 0);
}

// Paso 2: blur horizontal
float4 BlurHorizontal(VertexShaderOutput input) : COLOR0
{
    float4 color = float4(0, 0, 0, 0);
    float weights[5] = {0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216};
    color += tex2D(ScreenTexture, input.TexCoords) * weights[0];
    for (int i = 1; i < 5; i++)
    {
        color += tex2D(ScreenTexture, input.TexCoords + float2(TexelSize.x * i, 0)) * weights[i];
        color += tex2D(ScreenTexture, input.TexCoords - float2(TexelSize.x * i, 0)) * weights[i];
    }
    return color;
}

// Paso 3: blur vertical + combinar con original
float4 CombineBloom(VertexShaderOutput input) : COLOR0
{
    float4 original = tex2D(ScreenTexture, input.TexCoords);
    float4 blurred = float4(0, 0, 0, 0);
    float weights[5] = {0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216};
    blurred += tex2D(ScreenTexture, input.TexCoords) * weights[0];
    for (int i = 1; i < 5; i++)
    {
        blurred += tex2D(ScreenTexture, input.TexCoords + float2(0, TexelSize.y * i)) * weights[i];
        blurred += tex2D(ScreenTexture, input.TexCoords - float2(0, TexelSize.y * i)) * weights[i];
    }
    return original + blurred * BloomIntensity;
}

technique Bloom
{
    pass Extract { PixelShader = compile ps_4_0_level_9_1 ExtractBright(); }
    pass BlurH { PixelShader = compile ps_4_0_level_9_1 BlurHorizontal(); }
    pass Combine { PixelShader = compile ps_4_0_level_9_1 CombineBloom(); }
};
