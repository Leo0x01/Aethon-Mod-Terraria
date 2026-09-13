// =============================================================================
// RealBlackHoleShader.fx — AethonMod
// Núcleo del agujero negro: raymarching de 75 pasos con desviación
// gravitacional aproximada, disco de acreción toroidal con ruido, anillo de
// fotones y horizonte de sucesos que absorbe la luz capturada.
//
// El fondo del juego se muestrea con las coordenadas desviadas por la marcha
// (lente sobre la textura s0); el ruido del disco llega por s1.
//
// NOTA de pipeline: el juego carga la versión compilada (.fxc). Si se
// modifica este archivo, recompilar al perfil fx_2_0 para regenerarla.
// =============================================================================

sampler baseTexture : register(s0);
sampler noiseTexture : register(s1);

// --- Parámetros (los establece BlackHoleProjectile por nombre) -------------
float globalTime;                    // tiempo envuelto del juego
float blackHoleRadius;               // radio del horizonte (en unidades shader)
float accretionDiskRadius;           // grosor del toro del disco
float aspectRatioCorrectionFactor;   // corrige píxeles no cuadrados
float cameraAngle;                   // inclinación de la cámara
float2 zoom;                         // escala del canvas
float3 cameraRotationAxis;           // eje de rotación de la cámara
float3 blackHoleCenter;              // centro del agujero (espacio shader)
float3 accretionDiskColor;           // color base del disco
float3 accretionDiskScale;           // achatado del toro (perspectiva)

// --- Helpers ----------------------------------------------------------------

// Jitter determinista por píxel: rompe el banding de la marcha discreta.
float HashPunto(float3 p)
{
    return frac(sin(dot(p, float3(12.9898, 78.233, 51.9852))) * 30000);
}

// Distancia con signo a un toro de eje Y (el anillo del disco).
float DistanciaToro(float3 p, float2 t)
{
    float2 q = float2(length(p.xz) - t.x, p.y);
    return length(q) - t.y;
}

// Rotación 2D estándar (matriz de seno/coseno expandida).
float2 Rotar2D(float2 v, float angulo)
{
    float s = sin(angulo);
    float c = cos(angulo);
    return float2(v.x * c - v.y * s, v.x * s + v.y * c);
}

// Rotación de un vector sobre un eje arbitrario (fórmula de rotación de ejes:
// v·cos + (e×v)·sin + e·(e·v)·(1−cos)).
float3 RotarSobreEje(float3 v, float3 eje, float angulo)
{
    float c = cos(angulo);
    float s = sin(angulo);
    return v * c + cross(v, eje) * s + eje * dot(eje, v) * (1 - c);
}

// Brillo del disco de acreción en un punto del espacio: toro difuso + ruido
// polar (angular/radial) desplazándose con el tiempo + negro dentro del
// horizonte.
float4 MuestrearDisco(float3 posicion)
{
    float3 offset = (posicion - blackHoleCenter) / accretionDiskScale;

    // Proximidad difusa al toro (0.75 = radio mayor fijo del anillo).
    float distanciaToro = -DistanciaToro(offset, float2(0.75, accretionDiskRadius));
    float brillo = pow(max(0, distanciaToro / accretionDiskRadius), 0.9);

    // Ruido en coordenadas polares: el disco "hierve" al girar.
    float2 polar = float2(atan2(offset.x, offset.z) / 6.283 + 0.5, length(offset));
    brillo *= tex2D(noiseTexture, polar * float2(3, 3.5) + globalTime * float2(6.3, -2));

    float4 colorDisco = float4(pow(saturate(accretionDiskColor), 1.1), 1) * brillo * 0.75;

    // Dentro del horizonte no hay disco: negro absoluto.
    float dentroHorizonte = smoothstep(0.01, 0, length(offset) - blackHoleRadius);
    return lerp(colorDisco, float4(0, 0, 0, 1), dentroHorizonte);
}

// --- Pixel shader ------------------------------------------------------------

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0, float4 position : SV_Position) : COLOR0
{
    float2 coordsBase = coords;

    // Llevar las coordenadas al rango [-1, 1] (con corrección de aspecto).
    coords = (coords - 0.5) * float2(aspectRatioCorrectionFactor, 1) + 0.5;
    coords = coords * 2 - 1;

    // Estado de la marcha de la luz.
    float luzCapturada = 0;
    float3 punto = float3(coords / zoom, -0.9);
    float3 pasoLuz = float3(0, 0, 1);
    float distanciaAlCentro = 0;
    float distanciaAlBorde = 0;

    // Punto de partida (para el halo exterior al final).
    float3 puntoInicial = float3(punto.xy, 0);

    // Girar cámara y dirección de la luz con la misma rotación.
    punto = RotarSobreEje(punto - blackHoleCenter, cameraRotationAxis, cameraAngle) + blackHoleCenter;
    pasoLuz = RotarSobreEje(pasoLuz, cameraRotationAxis, cameraAngle);

    // Dithering sub-paso: desplaza el inicio una fracción aleatoria del paso
    // para que el banding de los 75 pasos sea invisible.
    punto += pasoLuz * HashPunto(punto * 10 + globalTime) * 0.0175;

    float4 resultado = 0;
    float2 offsetDistorsion = 0;

    // === MARCHA DE LA LUZ (75 pasos) ===
    // Cada paso avanza hacia el observador Y se curva hacia el agujero con
    // una intensidad ~ 1/r² (aproximación newtoniana del lente).
    for (float i = 0; i < 75; i++)
    {
        distanciaAlCentro = distance(punto, blackHoleCenter);
        distanciaAlBorde = distanciaAlCentro - blackHoleRadius;

        // ¿Cuánta luz se tragó el horizonte en este tramo? (1 = absorbida)
        luzCapturada = smoothstep(0.01, -0.1, distanciaAlBorde);

        // Paso del frente de luz: mínimo en el centro de la marcha, algo
        // mayor en los extremos (curva "rebote" 4·t·(1−t) invertida).
        float avance = 4 * (i / 75) * (1 - i / 75);
        float paso = lerp(0.02, 0.021, 1 - avance);

        // Curvatura hacia el agujero, limitada para no explotar cerca de r=0.
        float intensidadCurva = clamp(0.005 / pow(distanciaAlCentro, 2), 0, 0.1) * blackHoleRadius;
        float3 curva = normalize(blackHoleCenter - punto) * intensidadCurva;

        punto += curva;
        punto += pasoLuz * (1 - luzCapturada) * paso;

        offsetDistorsion += curva.xy;

        // El disco brilla ADDITIVAMENTE a lo largo de la marcha (materia
        // atravesada en cada tramo).
        resultado += MuestrearDisco(punto);
    }

    // === HALOS POST-MARCHA ===
    // Halo cálido alrededor del agujero (1/r³), apagándose al alejarse y
    // teñido del color del disco cuando la luz fue absorbida.
    float atenuacionHalo = smoothstep(5, 0, distanciaAlCentro / blackHoleRadius);
    float4 colorDisco = float4(accretionDiskColor, 1);
    resultado += clamp(0.3 / pow(distanciaAlCentro, 3) * colorDisco, 0, 2)
                 * atenuacionHalo
                 * lerp(1, colorDisco * 0.12, luzCapturada);

    // Anillo de fotones: brillo fino justo fuera del horizonte (1/|d|).
    float distGlow = (distance(puntoInicial, blackHoleCenter) - blackHoleRadius * 1.7);
    resultado += 0.015 / abs(distGlow) * smoothstep(0.2, 0.1, distGlow);

    // === LENTE SOBRE EL FONDO ===
    // El fondo (s0) se muestrea en las coordenadas base ROTADAS por la
    // distorsión acumulada + el desplazamiento neto de la marcha. La rotación
    // total gira con el tiempo (arrastre de marco) alrededor del agujero.
    float anguloLente = length(offsetDistorsion) * 50 - globalTime * 6;
    float2 centroPantalla = (blackHoleCenter.xy + 1) * 0.5;
    float2 coordsRotadas = Rotar2D(coordsBase - centroPantalla, anguloLente) + centroPantalla;
    float mezclaLente = smoothstep(0.125, 0.3, length(offsetDistorsion));
    float2 coordsFinales = lerp(coordsBase, coordsRotadas, mezclaLente) + offsetDistorsion;

    // Fondo lenteado (invisible dentro del horizonte) + brillo acumulado.
    return tex2D(baseTexture, coordsFinales) * (1 - luzCapturada) + resultado;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
