// =============================================================================
// RadialShineShader.fx — AethonMod
// Aura radial con ruido animado: muestrea la textura s0 en coordenadas
// POLARES (ángulo/radio) a dos escalas distintas desplazándose a velocidades
// diferentes, y las combina (raíz del producto) para un brillo estelar que
// respira y rota alrededor del centro. El glow del centro (1/r) remata el
// núcleo.
//
// NOTA de pipeline: el juego carga la versión compilada (.fxc). Si se
// modifica este archivo, recompilar al perfil fx_2_0 para regenerarla.
// =============================================================================

sampler baseTexture : register(s0);

float globalTime;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Coordenadas polales del píxel: u = ángulo (0..1 con wrap), v = radio.
    float2 polar = float2(atan2(coords.y - 0.5, coords.x - 0.5) / 6.283 + 0.5,
                           distance(coords, 0.5));

    // Dos pasadas de ruido con distinta escala angular/radial y velocidad:
    // franjas gruesas girando lento + filamentos finos girando más rápido.
    float ruidoA = tex2D(baseTexture, polar * float2(2, 0.02) + float2(0, globalTime * -0.11));
    float ruidoB = tex2D(baseTexture, polar * float2(3, 0.04) + float2(0, globalTime * -0.08));

    // El brillo exterior se apaga hacia el borde con umbral ruidoso (el
    // alcance del aura NO es un círculo perfecto: respira con el ruido).
    float fundidoBorde = smoothstep(0.5 - ruidoB * 0.3, 0, polar.y);

    // Raíz del producto: equilibra las dos escalas (ninguna domina).
    float brilloBase = sqrt(ruidoA * ruidoB) * fundidoBorde * 3;

    // Núcleo: glow 1/r apagado suavemente (el corazón del aura).
    float glowCentro = smoothstep(0.18, 0, polar.y) * 0.4 / polar.y;

    // Realce de contraste (potencia 2.4 → solo los picos más brillantes) y
    // composición aditiva con el color del vértice.
    float4 resultado = smoothstep(0, 0.85, pow(brilloBase, 2.4)) * sampleColor + glowCentro * sampleColor.a;
    resultado.a = 0;

    return resultado;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
