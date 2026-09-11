sampler baseTexture : register(s0);
sampler noiseTexture : register(s1);

float globalTime;
float blackHoleRadius;
float accretionDiskRadius;
float aspectRatioCorrectionFactor;
float cameraAngle;
float2 zoom;
float3 cameraRotationAxis;
float3 blackHoleCenter;
float3 accretionDiskColor;
float3 accretionDiskScale;

float QuadraticBump(float x)
{
    return x * (4 - x * 4);
}

float Hash13(float3 p)
{
    return frac(sin(dot(p, float3(12.9898, 78.233, 51.9852))) * 30000);
}

float SignedTorusDistance(float3 p, float2 t)
{
    float2 q = float2(length(p.xz) - t.x, p.y);
    return length(q) - t.y;
}

float2 RotatedBy(float2 v, float theta)
{
    float s = sin(theta);
    float c = cos(theta);
    return float2(v.x * c - v.y * s, v.x * s + v.y * c);
}

float4 Sample(float3 position)
{
    // Calculate the amount of glow for the accretion disk based on the distance relative to a torus surrounding the black hole.
    float3 offsetFromBlackHole = (position - blackHoleCenter) / accretionDiskScale;
    float accretionDiskDistance = -SignedTorusDistance(offsetFromBlackHole, float2(0.75, accretionDiskRadius));
    float accretionDiskGlow = pow(max(0, accretionDiskDistance / accretionDiskRadius), 0.9);
    
    // Apply some noise to the glow calculation to make it feel less artifically halo-y.
    float2 radial = float2(atan2(offsetFromBlackHole.x, offsetFromBlackHole.z) / 6.283 + 0.5, length(offsetFromBlackHole));
    accretionDiskGlow *= tex2D(noiseTexture, radial * float2(3, 3.5) + globalTime * float2(6.3, -2));
    
    // Combine the results with the base accretion disk color.
    float4 accretionDiskColorWithAlpha = float4(pow(saturate(accretionDiskColor), 1.1), 1) * accretionDiskGlow * 0.75;
    
    return lerp(accretionDiskColorWithAlpha, float4(0, 0, 0, 1), smoothstep(0.01, 0, length(offsetFromBlackHole) - blackHoleRadius));
}

// https://en.wikipedia.org/wiki/Rodrigues%27_rotation_formula
float3 RodriguesRotation(float3 v, float3 axis, float angle)
{
    float c = cos(angle);
    float s = sin(angle);
    return v * c + cross(v, axis) * s + axis * dot(axis, v) * (1 - c);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0, float4 position : SV_Position) : COLOR0
{
    float2 baseCoords = coords;
    
    // Rearrage coordinates into a -1 to 1 UV range.
    coords = (coords - 0.5) * float2(aspectRatioCorrectionFactor, 1) + 0.5;
    coords = coords * 2 - 1;
    
    // Initialize positional information.
    float capturedLightInterpolant = 0;
    float3 samplePoint = float3(coords / zoom, -0.9);
    float3 standardLightPositionIncrement = float3(0, 0, 1);
    float distanceFromBlackHole = 0;
    float distanceFromBlackHoleEdge = 0;
    
    float3 startingSamplePoint = float3(samplePoint.xy, 0);
    
    // Apply camera rotation.
    samplePoint = RodriguesRotation(samplePoint - blackHoleCenter, cameraRotationAxis, cameraAngle) + blackHoleCenter;
    standardLightPositionIncrement = RodriguesRotation(standardLightPositionIncrement, cameraRotationAxis, cameraAngle);
    
    // Slightly nudge the starting sample point around a touch to make banding artifacts from the limited step count virtually unnoticeable.
    samplePoint += standardLightPositionIncrement * Hash13(samplePoint * 10 + globalTime) * 0.0175;
    
    float4 result = 0;
    float2 distortionOffset = 0;
    for (float i = 0; i < 75; i++)
    {
        // Calculate the distance from the black hole and its edge.
        distanceFromBlackHole = distance(samplePoint, blackHoleCenter);
        distanceFromBlackHoleEdge = distanceFromBlackHole - blackHoleRadius;
        
        // Determine how much light was captured by the black hole on this update step.
        capturedLightInterpolant = smoothstep(0.01, -0.1, distanceFromBlackHoleEdge);
        
        // Move the sample point forward and towards the black hole based on proximity.
        float step = lerp(0.02, 0.021, 1 - QuadraticBump(i / 75));
        float distortionIntensity = clamp(0.005 / pow(distanceFromBlackHole, 2), 0, 0.1) * blackHoleRadius;
        float3 distortion = normalize(blackHoleCenter - samplePoint) * distortionIntensity;
        
        samplePoint += distortion;
        samplePoint += standardLightPositionIncrement * (1 - capturedLightInterpolant) * step;
        
        // Accumulate total distortion offsets in the lightmarch for later.
        distortionOffset += distortion.xy;
        
        // Additively apply color samples to the result.
        // This determines the base of the accretion disk's color.
        result += Sample(samplePoint);
    }
    
    // Apply glow effects around the black hole.
    float glowAttenuation = smoothstep(5, 0, distanceFromBlackHole / blackHoleRadius);
    float4 accretionDiskColorWithAlpha = float4(accretionDiskColor, 1);
    result += clamp(0.3 / pow(distanceFromBlackHole, 3) * accretionDiskColorWithAlpha, 0, 2) * glowAttenuation * lerp(1, accretionDiskColorWithAlpha * 0.12, capturedLightInterpolant);
    
    float glowDistance = (distance(startingSamplePoint, blackHoleCenter) - blackHoleRadius * 1.7);
    result += 0.015 / abs(glowDistance) * smoothstep(0.2, 0.1, glowDistance);
    
    // Apply gravitational lensing UV effects.
    float angleOffset = length(distortionOffset) * 50 - globalTime * 6;
    float2 blackHolePosition2D = (blackHoleCenter.xy + 1) * 0.5;
    float2 rotatedCoords = RotatedBy(baseCoords - blackHolePosition2D, angleOffset) + blackHolePosition2D;
    float2 interpolatedCoords = lerp(baseCoords, rotatedCoords, smoothstep(0.125, 0.3, length(distortionOffset))) + distortionOffset;
    
    return tex2D(baseTexture, interpolatedCoords) * (1 - capturedLightInterpolant) + result;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}