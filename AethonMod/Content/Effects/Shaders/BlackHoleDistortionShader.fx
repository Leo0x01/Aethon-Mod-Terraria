// =============================================================================
// BlackHoleDistortionShader.fx — AethonMod
// Lente gravitacional a pantalla completa para el FONDO del juego: hasta 5
// fuentes (agujeros negros / ondas de choque) curvan las UVs del frame con
// una rotación cuya fuerza decae exponencialmente con la distancia a cada
// fuente. Lo aplica BlackHoleLensSystem sobre una copia de screenTarget.
//
// NOTA de pipeline: el juego carga la versión compilada (.fxc). Si se
// modifica este archivo, recompilar al perfil fx_2_0 para regenerarla.
// =============================================================================

sampler baseTexture : register(s0);

// --- Parámetros (los establece BlackHoleLensSystem por nombre) --------------
float distortionStrength;          // fuerza global (0..1, ligada a la escala)
float maxLensingAngle;             // ángulo pico de la rotación (radianes)
float sourceRadii[5];              // radio de cada fuente (en UV corregidas)
float2 zoom;                       // escala de la pantalla
float2 sourcePositions[5];         // posición UV de cada fuente
float2 aspectRatioCorrectionFactor;// corrige píxeles no cuadrados

// --- Helpers ------------------------------------------------------------------

// Inverso de un lerp saturado: (x−from)/(to−from) acotado a [0,1].
float LerpInverso(float desde, float hasta, float x)
{
    return saturate((x - desde) / (hasta - desde));
}

// Rotación 2D estándar (seno/coseno expandidos).
float2 Rotar2D(float2 v, float angulo)
{
    float s = sin(angulo);
    float c = cos(angulo);
    return float2(v.x * c - v.y * s, v.x * s + v.y * c);
}

// Ángulo de curvado de una fuente sobre un píxel: la distancia (corregida
// por aspecto y zoom) alimenta un decaimiento exponencial — el efecto no se
// extiende mucho más allá del radio de la propia fuente.
float AnguloLente(float radioFuente, float2 coords, float2 posFuente)
{
    float2 coordsZoom = (coords - 0.5) * aspectRatioCorrectionFactor / zoom + 0.5;
    float2 fuenteCorregida = (posFuente - 0.5) * aspectRatioCorrectionFactor + 0.5;
    float distancia = distance(coordsZoom, fuenteCorregida);

    return distortionStrength * maxLensingAngle * exp(-distancia / radioFuente * 2);
}

// --- Pixel shader ---------------------------------------------------------------

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Las fuentes se aplican EN CASCADA: cada una rota el resultado de la
    // anterior alrededor del centro de la pantalla, así dos agujeros
    // cercanos curvan el mismo píxel de forma compuesta.
    float2 coordsDistorsionadas = coords;
    for (int i = 0; i < 5; i++)
    {
        float angulo = AnguloLente(sourceRadii[i], coords, sourcePositions[i]);
        coordsDistorsionadas = Rotar2D(coordsDistorsionadas - 0.5, angulo) + 0.5;
    }

    return tex2D(baseTexture, coordsDistorsionadas);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
