// =============================================================================
// CalorDistorsionShader.fx — AethonMod (v6.78 — LA LIBRERÍA DE CALOR)
// EL AIRE QUE HIERVE — la distorsión de calor de pantalla para el Rayo de
// Prueba C (y mañana, cualquier fuente que CalorLib registre).
//
// LA TÉCNICA (la investigación v6.78 — la síntesis de tres escuelas):
//   · REFRACTIÓN EN ESPACIO DE PANTALLA: el filtro re-muestrea la pantalla
//     (uImage0/texPantalla — el slot s0 que la cadena de filtros de vanilla
//     alimenta) en UVs desplazadas por un ruido de plasma que hace scroll
//     (Wolfire 2006, Rastertek T53, Linden Reid — EL postulado del heat
//     haze 2D).
//   · EL RUIDO QUE HIERVE EN SU SITIO: DOS octavas en DIAGONALES OPUESTAS
//     (una sube ↗, la otra ↖ — el truco de MoonstoneDistortion de Starlight
//     River): los componentes horizontales opuestos se cancelan y el patrón
//     NO «fluye» hacia ningún lado — el aire HIERVE sin desplazarse. La
//     tercera octava (calidad alta) agrega el shimmer fino rápido.
//   · LA CONVECCIÓN: un sesgo NETO hacia arriba (el aire caliente sube —
//     la asimetría física que el ojo lee como «calor»).
//   · EL CAMPO DE CALOR: las fuentes (la cápsula del haz · los anillos de
//     los estallidos) se pintan ADITIVAS en un render target a ¼ de
//     resolución (el patrón del WaterShaderData de vanilla,
//     DISTORTION_BUFFER_SCALE 0.25: 1/16 de los píxeles, el fetch LINEAR
//     suaviza el upscale gratis) — donde DOS fuentes se solapan, el calor
//     SE SUMA (el hervidero de la boca del haz).
//   · LA ABERRACIÓN CROMÁTICA DEL ESTALLIDO: 3 taps radiales alrededor del
//     punto del golpe — R hacia afuera, G al centro, B hacia adentro ×0.9
//     (la asimetría leve de la lente real; 6-16 px en el pico; Unity PPv2
//     y el 90 % de los juegos: el estándar). El centro llega por
//     uTargetPosition — la MISMA matemática zoom-correcta de vanilla:
//     (uTargetPosition − uScreenPosition)/uScreenResolution = UV real de
//     pantalla (la corrección de zoom y de offScreenRange la hacen
//     UnscaledScreenPosition/UnscaledScreenSize en el Apply).
//
// EL LUGAR DEL FRAME: este efecto vive como FILTRO en Filters.Scene
// ("AethonCalor") — la cadena de FilterManager.EndCapture lo aplica
// ENCIMA del mundo capturado y DEBAJO de la UI, componiendo con los demás
// filtros (el Graveyard tiñe el mundo ya distorsionado: la lección de la
// v6.51.3 no se rompe). La ACTIVACIÓN del filtro fuerza la captura de
// pantalla (CanCapture = HasActiveFilter) — sin filtros activos vanilla
// no captura NADA: la distorsión solo puede vivir aquí.
//
// LOS PARÁMETROS DE VANILLA (defensivos): ScreenShaderData.Apply los
// setea TODOS con ?. (los ausentes se saltan solos — el parche de tML);
// los declarados aquí que no se usan quedan como constantes muertas y los
// que SÍ se usan llegan con la matemática oficial: uOpacity (el fade del
// filtro ±1/s), uTime (GlobalTimeWrappedHourly), uIntensity,
// uTargetPosition/uScreenPosition/uScreenResolution (zoom-correctos).
//
// GPU-UNIVERSAL (la ley §0 de la casa): si el .fxc no cargara en una GPU
// rara, CalorLib jamás activa el filtro — el haz conserva su pila de
// SIETE capas de siempre y nada se rompe. Cero excepciones por frame (la
// bandera de fallo de la casa).
//
// PIPELINE: el juego carga SOLO la versión compilada (.fxc — tML registra
// FxcReader para ".fxc"). Este .fxc se construye con la cadena de la
// casa: vkd3d-shader compila el PS a SM3 y tools/fx20_build.py --solo-ps
// ensambla el binario fx_2_0 (layout de los filtros de cadena: UN estado
// de pase PIXELSHADER, un objeto grande, cero vertex shader — SpriteBatch
// conserva su propio VS), validado contra el parser real de FNA
// (MojoShader). EL .fx SE CAMBIA CON CIRUGÁ (la ley del HazConcentrado):
// cada delta se compila, valida y se inspecciona el GLSL transpilado.
// =============================================================================

// --- EL SLOT DE LA CADENA: la pantalla la dibuja FilterManager sobre s0 ---
sampler texPantalla : register(s0);

// --- EL CAMPO DE CALOR (¼ de resolución, CLAMP: nada que repetir) ---------
texture uTexturaCampo;
sampler texCampo = sampler_state
{
    texture = <uTexturaCampo>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = CLAMP;
    AddressV = CLAMP;
};

// --- EL RUIDO DE PLASMA (512², seamless, WRAP: el scroll es infinito) -----
texture uTexturaRuido;
sampler texRuido = sampler_state
{
    texture = <uTexturaRuido>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = WRAP;
    AddressV = WRAP;
};

// --- LOS PARÁMETROS DE VANILLA (los setea ScreenShaderData.Apply) ---------
float2 uImageOffset;         // (defensivo)
float  uOpacity;             // EL FADE del filtro (0→1 al activar, ±1/s)
float  uSaturation;          // (defensivo)
float  uIntensity;           // la fuerza maestra (UseIntensity — por defecto 1)
float3 uSecondaryColor;      // (defensivo)
float  uTime;                // GlobalTimeWrappedHourly (segundos)
float2 uScreenPosition;      // la esquina de pantalla en mundo (zoom-correcta)
float2 uScreenResolution;    // el tamaño de pantalla (zoom-correcto)
float2 uDirection;           // (defensivo)
float3 uColor;               // (defensivo)
float2 uTargetPosition;      // EL CENTRO DEL ESTALLIDO en mundo (UseTargetPosition)
float  uProgress;            // (defensivo)
float2 uZoom;                // (defensivo — la corrección ya vive en los de arriba)
float2 uSceneSize;           // (defensivo, 1.4.5)
float2 uSceneOffset;         // (defensivo, 1.4.5)
float2 uImageSize0;          // (defensivo, 1.4.5)

// --- LOS PARÁMETROS DE LA CASA (los setea CalorLib por nombre) ------------
float uAnchoPx;      // LA AMPLITUD: px de flexión del aire con calor pleno
float uCAFuerza;     // LA ABERRACIÓN: px del split R/B en el pico (ya con su
                     // envolvente temporal calculada en C# — el golpe y su muerte)
float uCARadio;      // el radio del estallido (en ALTURAS de pantalla: 0.34
                     // = un tercio de la pantalla alrededor del golpe)
float uConveccion;   // el sesgo ascendente (0..1: el aire caliente SUBE)
float uCalidad;      // LAS OCTAVAS: 1 = la base · 2 = +la contraria (hervir
                     // in situ) · 3 = +el shimmer fino (la media de FactorCalidad)

// --- EL PIXEL SHADER ---------------------------------------------------------
float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // EL CAMPO: lo que las fuentes pintaron (aditivo: donde se cruzan, SUMA).
    float H = saturate(tex2D(texCampo, coords).r);

    // EL CENTRO DEL ESTALLIDO en UV de pantalla (la matemática zoom-correcta
    // de vanilla — el patrón del shockwave comunitario).
    float2 centroUV = (uTargetPosition - uScreenPosition) / uScreenResolution;

    // LA ABERRACIÓN: la distancia al centro, corregida por aspecto (unidades
    // de ALTURA — los anillos nacen redondos, no ovales) y el perfil radial
    // (nulo en el eje óptico — física de la lente —, pleno al medio, muerto
    // en el borde del estallido).
    float2 dc = coords - centroUV;
    dc.x *= uScreenResolution.x / uScreenResolution.y;
    float r = length(dc);
    float2 dir = dc / max(r, 0.0001);
    float perfil = saturate(1.0 - r / max(uCARadio, 0.0001)) * (r + 0.15);
    float amt = (uCAFuerza / uScreenResolution.y) * perfil * saturate(uOpacity);

    // EL EARLY-OUT (branch coherente — barato): sin calor y sin aberración,
    // copia limpia. El filtro cuesta UNA muestra en el 95 % de la pantalla.
    if (H < 0.004 && amt < 0.00002)
        return tex2D(texPantalla, coords);

    // EL RUIDO DE CONVECCIÓN — texels cuadrados (el aspecto corrige la Y).
    float aspect = uScreenResolution.x / uScreenResolution.y;
    float2 nuv = float2(coords.x, coords.y * aspect);
    float t = uTime;

    // (1) LA BASE ANCHA — la ola portadora (siempre viva, calidad ≥ 1).
    float2 N = (tex2D(texRuido, nuv * 1.00 + float2( t * 0.045,  t * 0.110)).r - 0.5)
               * 2.0 * float2(0.020, 0.026);

    // (2) LA CONTRARIA — diagonal opuesta (calidad ≥ 2): lo que sube por la
    //     derecha baja por la izquierda — el patrón HIERVE EN SU SITIO.
    if (uCalidad >= 1.5)
        N += (tex2D(texRuido, nuv * 1.87 + float2(-t * 0.060,  t * 0.150)).r - 0.5)
             * 2.0 * float2(0.013, 0.018);

    // (3) EL SHIMMER FINO — la octava rápida de los detalles (calidad ≥ 3).
    if (uCalidad >= 2.5)
        N += (tex2D(texRuido, nuv * 3.41 + float2( t * 0.030,  t * 0.280)).r - 0.5)
             * 2.0 * float2(0.008, 0.011);

    // LA CONVECCIÓN: el sesgo NETO ascendente — el aire caliente SUBE (muestrear
    // un pelo más abajo: la imagen se levanta, como detrás de una hoguera).
    N.y -= uConveccion * 0.55 * H;

    // EL OFFSET: la amplitud (px→UV) por el campo, la fuerza y el fade.
    float2 bias = N * H * (uAnchoPx / uScreenResolution) * uIntensity * saturate(uOpacity);
    float2 uvD = clamp(coords + bias, 0.002, 0.998);

    // LA COMPOSICIÓN — 3 taps radiales: R afuera, G al centro, B adentro ×0.9.
    float3 col;
    col.r = tex2D(texPantalla, clamp(uvD + dir * amt, 0.002, 0.998)).r;
    col.g = tex2D(texPantalla, uvD).g;
    col.b = tex2D(texPantalla, clamp(uvD - dir * amt * 0.9, 0.002, 0.998)).b;
    return float4(col, 1.0);
}

technique TecnicaCalor
{
    pass PaseCalor
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
