// =============================================================================
// HazConcentradoShader.fx — AethonMod (v6.55.0 · v6.56.7 LA MEJORA)
// EL HAZ CONCENTRADO — el shader de la casa para el arma de jugador.
//
// La técnica (estudiada y re-escrita 100 % para el mod): un haz de plasma
// vivo nace de la MULTIPLICACIÓN de cuatro texturas procedurales de la casa
// (CarnePlasma · SegundoRuido · RampaGlow · BitsChispa) con doble scroll
// cruzado, deformación sinusoidal del muestreo y las chispas en doble AND.
//
// LAS CURAS DE LA CASA (la 2ª ronda del diagnóstico de los picos):
//   1. uEscalaV — LA ESCALA DE RUIDO COMPENSADA: el arma vive a tamaños
//      menores que el original y el ruido fino se leía como dientes de
//      sierra (texel≈2.7 px). El muestreo transversal se ENCOGE
//      proporcionalmente al tamaño real => los rasgos del plasma miden
//      SIEMPRE lo mismo en pantalla — orgánicos a cualquier escala.
//   2. EL NÚCLEO SUAVE — el umbral duro de saturación (if length > k
//      return 1) dibujaba un CONTORNO FRACTAL: aquí la vena blanca nace
//      de un smoothstep — el mismo latido blanco, el borde respira.
//
// v6.56.7 — LA MEJORA DEL SHADER (la letra: «dale una mejora de shaders
// tambien»). TODO NUEVO VIVE PER-PIXEL — la ley de los picos (códice §5.4):
// un efecto de haz hecho con QUADS CPU (la «pila de capas» de la v6.56.6)
// dibuja bordes POLIGONALES duros y texturas crudas de borde serrado; un
// efecto de haz hecho en el PIXEL funde cada borde con smoothstep y es
// incapaz de producir picos. Las cuatro mejoras:
//   · EL HALO — EL DESENFOQUE PER-PIXEL: una falda geométrica (pow³) que
//     muere EXACTAMENTE en el borde del polígono del strip, texturada con
//     la carne del plasma a su propio flujo — el resplandor difuso que
//     desdibuja el borde SIN sierra posible.
//   · LOS FILAMENTOS — LA AUTO-INTERFERENCIA: la carne multiplicada por
//     una copia de sí misma a MEDIA frecuencia (la técnica del recetario
//     de la investigación) — el plasma gana CORDONES GRANDES que
//     serpentean sobre el hervor fino. Dos escalas de textura, un asset.
//   · EL NÚCLEO QUE LATE — el umbral de la vena blanca RESPIRA con un
//     seno lento (~0.9 s): el corazón del conducto se hincha y afloja.
//   · LA FRANJA PRISMÁTICA — el halo se muestrea TRES veces con un
//     microdesfase por canal cerca del borde (rojo afuera, azul adentro):
//     la aberración cromática de la casa viva en el filo del haz.
//
// PIPELINE (la lección de la v6.55.0): el juego carga SOLO la versión
// compilada (.fxc — tModLoader registra FxcReader para ".fxc"; los .fx NO
// se compilan solos). Este .fxc se construye con tools/fx20_build.py: los
// dos entry points se compilan a SM3 (vkd3d-shader 2.x) y el binario de
// efecto se ensambla con el layout exacto de fxc /T fx_2_0, validado
// contra el parser real de FNA (MojoShader). Si algo fallara en runtime,
// el proyectil tiene el fallback de tiras texturizadas de la casa.
// =============================================================================

matrix uTransformMatrix;
float uTime;
float uEscalaV;

texture uTexturaCarne;
sampler texCarne = sampler_state
{
    texture = <uTexturaCarne>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

texture uTexturaSegunda;
sampler texSegunda = sampler_state
{
    texture = <uTexturaSegunda>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

texture uTexturaRampa;
sampler texRampa = sampler_state
{
    texture = <uTexturaRampa>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

texture uTexturaBits;
sampler texBits = sampler_state
{
    texture = <uTexturaBits>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

struct VertexShaderInput
{
    float2 Coord : TEXCOORD0;
    float4 Position : POSITION0;
    float4 Color : COLOR0;
};

struct VertexShaderOutput
{
    float2 Coord : TEXCOORD0;
    float4 Position : POSITION0;
    float4 Color : COLOR0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    output.Color = input.Color;
    output.Coord = input.Coord;
    output.Position = mul(input.Position, uTransformMatrix);
    return output;
}

// El muestreo transversal ENCIMA: la ventana V se encoge con uEscalaV y
// vive centrada en el corazón de la textura — los rasgos del plasma
// conservan su tamaño en pantalla a cualquier escala del haz.
float VentanaV(float coordY)
{
    return 0.5 + (coordY - 0.5) * uEscalaV;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    // LA ENVOLVENTE TRANSVERSAL DEL NÚCLEO (la de siempre): el cuerpo del
    // haz muere suave entre el 30 % y el 70 % del semiancho del strip.
    float envV = smoothstep(0.0, 0.30, input.Coord.y) * smoothstep(1.0, 0.70, input.Coord.y);

    // LA CAÍDA LONGITUDINAL: el arranque se enciende y el final se rinde
    // (más pronto en los bordes que en el corazón — la pluma del original).
    float caida = smoothstep(0.42, 0.0, input.Coord.x - sin(input.Coord.y * 3.14159) * 0.34);

    // LA CARNE — fBm con el scroll doble y la deformación sinusoidal.
    float vCarne = VentanaV(input.Coord.y + sin((uTime * 4 + input.Coord.x) * 6.2831 - 1.5708) * 0.05 * uEscalaV);
    float4 carne = tex2D(texCarne, float2(frac(uTime * 3 + input.Coord.x * 1.5), vCarne));

    // v6.56.7 — LOS FILAMENTOS (la auto-interferencia): la carne evaluada
    // a MEDIA escala espacial (la banda central estirada) y su propio flujo
    // inverso — los CORDONES grandes que serpentean bajo el hervor fino.
    float4 velas = tex2D(texCarne, float2(frac(uTime * 1.6 - input.Coord.x * 0.6),
        VentanaV(input.Coord.y * 0.5 + 0.25)));

    // EL SEGUNDO RUIDO — el que CRUZA al primero (el hervor del producto).
    float4 segundo = tex2D(texSegunda, float2(frac(uTime * 4 + input.Coord.x * 1.2), VentanaV(input.Coord.y)));
    segundo.a = 1;

    // LA RAMPA — el perfil de calor transversal.
    float4 rampa = tex2D(texRampa, float2(frac(uTime * 2 + input.Coord.x), VentanaV(input.Coord.y)));

    // EL CUERPO: el producto de las capas con la envolvente y la caída —
    // los filamentos MODULAN (lerp 1→velas·1.55 al 60 %): la estructura
    // grande sin matar el detalle chico.
    float4 cuerpo = carne * segundo * rampa * caida;
    cuerpo *= lerp(1.0, velas * 1.55 + 0.15, 0.60);
    float brillo = length(cuerpo.rgb);
    float4 laser = cuerpo * input.Color * 1.35;
    laser.rgb += pow(brillo, 2) * input.Color.rgb * caida * 0.85;

    // EL RESPLANDOR de la rampa sobre el color (la falda cálida del núcleo).
    float4 falda = float4(rampa.rgb * input.Color.rgb * caida * envV, 0);

    // v6.56.7 — EL NÚCLEO QUE LATE: el umbral de la vena blanca respira
    // con un seno lento (~54 ticks de período con el uTime de la casa) —
    // el corazón del conducto se hincha y afloja.
    float latido = 0.5 + 0.5 * sin(uTime * 4.65);
    float umbral = 0.30 + 0.09 * latido;
    float nucleo = smoothstep(umbral, umbral + 0.30, length(laser.rgb) + brillo * 0.22);

    // LAS CHISPAS — el doble AND: la misma máscara muestreada con DOS
    // flujos distintos; solo donde ambas nubes de blobs coinciden, arde.
    float vBitsA = frac(VentanaV(input.Coord.y) + sin((input.Coord.x * 2 + uTime) * 6.2831) * 0.01);
    float vBitsB = frac(VentanaV(input.Coord.y) + sin((input.Coord.x * 0.5 + uTime * 2) * 6.2831) * 0.02);
    float4 bitsA = tex2D(texBits, float2(frac(uTime * 3 + input.Coord.x), vBitsA));
    float4 bitsB = tex2D(texBits, float2(frac(uTime * 5 + input.Coord.x), vBitsB));
    float4 chispa = bitsA * bitsB;
    chispa.rgb *= input.Color.rgb * 0.05 + 0.20;

    // La chispa solo vive donde la carne está VIVA (gate por brillo).
    float gateChispa = smoothstep(0.12, 0.30, brillo) * smoothstep(1.0, 0.62, input.Coord.x);
    chispa *= gateChispa;

    // ================================================================
    // v6.56.7 — EL HALO (EL DESENFOQUE PER-PIXEL): la falda geométrica
    // pow³ que muere EXACTAMENTE en el borde del polígono del strip,
    // texturada con la carne a su propio flujo y muestreada TRES veces
    // con microdesfase por canal en la zona del filo — LA FRANJA
    // PRISMÁTICA (rojo afuera, azul adentro). Nada de quads: el borde
    // del halo es una curva por píxel — PICOS IMPOSIBLES.
    // ================================================================
    float geo = saturate(1.0 - abs(input.Coord.y - 0.5) * 2.0);   // 1 centro → 0 borde
    float haloGeo = geo * geo * geo;                              // la caída difusa
    float borde = 1.0 - envV;                                     // vive en el filo
    float desfase = 0.035 * borde;
    float uHalo = frac(uTime * 1.3 + input.Coord.x * 0.7);
    float3 haloPrisma;
    haloPrisma.r = tex2D(texCarne, float2(uHalo, VentanaV(input.Coord.y + desfase))).r;
    haloPrisma.g = tex2D(texCarne, float2(uHalo, VentanaV(input.Coord.y))).g;
    haloPrisma.b = tex2D(texCarne, float2(uHalo, VentanaV(input.Coord.y - desfase))).b;
    float4 halo = float4(haloPrisma * 2.0 * input.Color.rgb * caida, 0) * haloGeo * 0.45;

    // LA COMPOSICIÓN: chispas + núcleo + cuerpo + falda — el núcleo entero
    // muere en su envolvente; el halo vive FUERA, hasta el borde del strip.
    float4 color = laser + falda + chispa;
    color.rgb = lerp(color.rgb, float3(1, 1, 1), nucleo * envV);
    color.rgb = color.rgb * envV + halo.rgb;
    color.a = saturate(brillo * 0.6 + nucleo * 0.5);
    return color;
}

technique TecnicaHaz
{
    pass PaseHaz
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
        VertexShader = compile vs_3_0 VertexShaderFunction();
    }
}
