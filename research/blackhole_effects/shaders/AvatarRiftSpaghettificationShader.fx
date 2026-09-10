sampler screenTexture : register(s0);
sampler noxusRiftTexture : register(s1);

float distortionIntensity;
float zoom;
float distortionRadius;
float2 distortionPosition;
float2 screenSize;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0, float4 position : SV_Position) : COLOR0
{
    float distanceFromDistortion = distance(position.xy, distortionPosition) / zoom;
    float localDistortionIntensity = smoothstep(distortionRadius, 0, distanceFromDistortion);
    
    // Apply a slight modifier to make distortions more intense near the inner center of the rift.
    localDistortionIntensity = exp(pow(localDistortionIntensity, 2)) - 1;
    
    // Apply the distortion.
    float2 distortedCoords = lerp(coords, distortionPosition / screenSize, -distortionIntensity * localDistortionIntensity);
    float4 baseColor = tex2D(screenTexture, coords);
    float4 riftColor = tex2D(noxusRiftTexture, coords);
    float4 distortedColor = tex2D(screenTexture, distortedCoords);
    float4 distortedRiftColor = tex2D(noxusRiftTexture, distortedCoords);
    bool inRift = length(distortedColor - distortedRiftColor) <= 0.02 && length(baseColor - riftColor) <= 0.02;
    
    return lerp(distortedColor, baseColor, inRift);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
