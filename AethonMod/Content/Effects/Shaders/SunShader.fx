// =============================================================================
// SunShader.fx — AethonMod
// Superficie estelar procedural: coords esféricas (pellizco radial para que
// el ruido "viaje" por una esfera y no por una hoja plana), doble muestreo
// de ruido de fuego auto-desplazado, manchas oscuras restando color de
// acento, ríos de lava brillantes con el mapa de offsets UV y corona con
// brillo en 1/|d| sobre el limbo.
//
// s0 = ruido de fuego (superficie), s1 = ruido de acento (manchas/lava),
// s2 = mapa de offsets (ondulación de los ríos).
//
// NOTA de pipeline: el juego carga la versión compilada (.fxc). Si se
// modifica este archivo, recompilar al perfil fx_2_0 para regenerarla.
// =============================================================================

sampler fireNoiseTexture : register(s0);
sampler accentNoiseTexture : register(s1);
sampler uvOffsetNoiseTexture : register(s2);

// --- Parámetros (los establece SunProjectile por nombre) ---------------------
float globalTime;                // tiempo envuelto del juego
float coronaIntensityFactor;     // fuerza de la corona
float sphereSpinTime;            // giro de la esfera (animación de superficie)
float3 mainColor;                // color principal (blanco cálido)
float3 darkerColor;              // color de las zonas oscuras (naranja profundo)
float3 subtractiveAccentFactor;  // cuánto resta el acento (manchas)

// --- Helpers --------------------------------------------------------------------

// (x−desde)/(hasta−desde) saturado.
float LerpInverso(float desde, float hasta, float x)
{
    return saturate((x - desde) / (hasta - desde));
}

// --- Pixel shader ------------------------------------------------------------------

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Distancia al centro (² y magnificada ×2 para las fórmulas siguientes).
    float2 coordsCentrados = coords * 2 - 1;
    float distSqr = dot(coordsCentrados, coordsCentrados) * 2;

    // Opacidad del disco estelar: borde suave hacia fuera.
    float opacidad = LerpInverso(0.5, 0.42, distSqr);

    // Pellizco esférico: remapea el cuadrado a coordenadas de ESFERA — el
    // ruido fluye como si la textura viviera sobre la bola (y no sobre el
    // plano). El +0.045 evita la singularidad del centro.
    float pellizco = (1 - sqrt(abs(1 - distSqr))) / distSqr + 0.045;
    float2 coordsEsfera = coords * pellizco + float2(sphereSpinTime, 0);

    // Doble muestreo de ruido: el PRIMER muestreo desplaza las UVs del
    // SEGUNDO (auto-distorsión) → la superficie se auto-pliega como plasma.
    float desplazamiento = tex2D(fireNoiseTexture, coordsEsfera).r * 0.41 + globalTime * 0.3;
    float2 coordsBrillo = coordsEsfera + float2(desplazamiento, 0);
    float3 texturaBrillo = tex2D(fireNoiseTexture, coordsBrillo);

    // Glow central: máximo en el núcleo, se apaga al 91% del radio.
    float glowNucleo = saturate(1 - distSqr * 0.91);

    // Base: pellizco × color principal + glow × color oscuro + textura.
    float3 resultado = pellizco * mainColor * 0.777 + glowNucleo * darkerColor + texturaBrillo;

    // Manchas: donde el brillo es BAJO se mezcla hacia el color oscuro, y el
    // ruido de acento RESTA (resta suavizada por el factor de acento).
    resultado = lerp(resultado, darkerColor, saturate(1 - texturaBrillo.r) * 0.8);
    resultado -= (1 - subtractiveAccentFactor) * tex2D(accentNoiseTexture, coordsEsfera * 2).r * 1.1;

    // Ríos de lava: el mapa de offsets ondula las UVs del acento; el cuadrado
    // del ruido deja solo los filamentos MÁS brillantes (finos y calientes).
    float2 offsetUV = tex2D(uvOffsetNoiseTexture, coords + float2(0, globalTime * 0.4));
    resultado += pow(tex2D(accentNoiseTexture, coordsEsfera * 1.2 + offsetUV * 0.04).r, 2) * 2.1;

    // Corona: solo el ANILLO del limbo (nada sobre la estrella, decae fuera).
    // El 1/|dist−0.5| afila el borde; el offset UV le da borde irregular.
    float fundidoCorona = LerpInverso(0.2, 0.5, distSqr) * LerpInverso(1.91, 0.98, distSqr) * coronaIntensityFactor;
    float brilloCorona = fundidoCorona / abs(distSqr - 0.5 + offsetUV.y * 0.04 + 0.04);

    // Estrella (con su opacidad) + corona encima, ambos por el color del vértice.
    return (opacidad * float4(resultado, 1) + float4(mainColor, 1) * brilloCorona) * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
