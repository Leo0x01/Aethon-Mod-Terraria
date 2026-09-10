sampler baseTexture : register(s0);

float globalTime;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(baseTexture, coords);
    float brightness = dot(color.rgb, 0.333);
    return float4(0, 0, 0, 1) * sampleColor * color.a * smoothstep(0.3, 0.1, brightness);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}