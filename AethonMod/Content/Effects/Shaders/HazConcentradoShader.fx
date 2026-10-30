// =============================================================================
// HazConcentradoShader.fx — AethonMod (v6.55.0 · v6.55.1 PROBADO · v6.56.8 EL CONJURO)
// EL HAZ CONCENTRADO — el shader de la casa para el arma de jugador.
//
// LA LECCIÓN DE LA v6.56.8 (la cura de «la banda plana»): la v6.56.7 reescribió
// el pixel shader con nueve muestreos y capas nuevas — validó contra el parser
// de MojoShader pero en la GPU del jugador el haz se veía como UNA BANDA PLANA
// sin textura, sin glow, de borde duro. LA CURA: este archivo vuelve AL
// ESQUELETO EXACTO DE LA v6.55.1 — el bytecode que el jugador VIO renderizar
// correcto durante toda la era 607df36 (mismo orden de operaciones, mismos
// constructos: texld·mul·mad·add·frc·sincos·rsq·rcp, SEIS muestreos) — y las
// mejoras nuevas se agregan como DELTAS MÍNIMOS sobre ese esqueleto probado.
// La regla que queda: EL .fx DEL RC2 SE CAMBIA CON CIRUGÁ — cada delta se
// compila, se valida contra MojoShader, se inspecciona el GLSL transpilado y
// se verifica en el simulador 1:1 ANTES de empaquetar. Nada de reescrituras
// completas del pixel shader.
//
// LA TÉCNICA (de la casa, estudiada y re-escrita 100 %): un haz de plasma
// vivo nace de las texturas procedurales propias (CarnePlasma · SegundoRuido
// · RampaGlow · BitsChispa) con doble scroll cruzado, deformación sinusoidal
// del muestreo y las chispas en doble AND.
//
// v6.56.8 — LA PALETA DEL MOD EN EL SHADER (la letra: «los colores del mod
// son principalmente morado, rojo, negro»): el COLOR VIAJA por el vértice
// (input.Color — la paleta morado/rojo que el C# manda por nodo), y el shader
// la Parte en TRES TONOS DEL CONJURO:
//   · EL CUERPO — tonoBase: el color vuelta VIOLETA PROFUNDO (el «negro» del
//     conjuro vive aquí: un violeta casi noche que funde el cuerpo del haz).
//   · LAS VENAS — tonoVena: donde el segundo ruido enciende, el tono salta a
//     ROJO VIVO. El segundo ruido YA NO MULTIPLICA (mataba media textura a
//     negro): ahora SELECCIONA el color — el plasma hierve en rojo sobre el
//     cuerpo violeta. DOS COLORES VIVOS EN UN SOLO PRODUCTO.
//   · EL NÚCLEO — la vena blanca de siempre, ahora BLANCO-VIOLETA y con el
//     umbral respirando (el corazón del conducto late).
//
// v6.56.8 — LAS CAPAS NUEVAS (todas per-pixel, la ley del pixel vs el quad —
// códice §5.4: una curva no puede producir picos):
//   · EL PULSO VIAJERO — bandas de brillo que corren de la boca a la punta al
//     ritmo de la carne (sin((x·5 + uTime·7)·2π)): el conducto EXHALA energía.
//   · EL HALO — la falda geométrica pow³ que muere exactamente en el borde
//     del polígono del strip, texturada con la carne a su propio flujo: el
//     resplandor difuso (el «desenfoque») que la v6.56.6 buscó con quads —
//     aquí es UNA muestra y una curva. La franja del filo se tiñe de ROJO
//     (borde² → tonoVena) sin muestrear tres veces.
//
// PIPELINE (la lección de la v6.55.1): el juego carga SOLO la versión
// compilada (.fxc — tModLoader registra FxcReader para ".fxc"; los .fx NO
// se compilan solos). Este .fxc se construye con tools/fx20_build.py: los
// dos entry points se compilan a SM3 (vkd3d-shader 2.x) y el binario de
// efecto se ensambla con el layout exacto de fxc /T fx_2_0, validado
// contra el parser real de FNA (MojoShader) — y el GLSL transpilado se
// INSPECCIONA a mano (la lección de la .56.8). Si algo fallara en runtime,
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
    // LA ENVOLVENTE TRANSVERSAL (la de siempre, un pelo más ancha que la
    // .55.1: el cuerpo llena más el conducto — «necesita más capas»).
    float envV = smoothstep(0.0, 0.22, input.Coord.y) * smoothstep(1.0, 0.78, input.Coord.y);

    // LA CAÍDA LONGITUDINAL: el arranque se enciende y el final se rinde
    // (más pronto en los bordes que en el corazón — la pluma del original).
    float caida = smoothstep(0.42, 0.0, input.Coord.x - sin(input.Coord.y * 3.14159) * 0.34);

    // v6.56.8 — EL PULSO VIAJERO: bandas de energía que corren de la boca a
    // la punta, al ritmo de la carne que camina (el conducto EXHALA).
    float pulso = 0.80 + 0.28 * sin((input.Coord.x * 5.0 + uTime * 7.0) * 6.2831);

    // LA CARNE — fBm con el scroll doble y la deformación sinusoidal.
    float vCarne = VentanaV(input.Coord.y + sin((uTime * 4 + input.Coord.x) * 6.2831 - 1.5708) * 0.05 * uEscalaV);
    float4 carne = tex2D(texCarne, float2(frac(uTime * 3 + input.Coord.x * 1.5), vCarne));

    // EL SEGUNDO RUIDO — v6.56.8: YA NO MULTIPLICABA (mataba media textura
    // a negro); ahora es EL SELECTOR DE LAS VENAS: donde su máscara enciende,
    // el tono salta de violeta profundo a ROJO VIVO.
    float4 segundo = tex2D(texSegunda, float2(frac(uTime * 4 + input.Coord.x * 1.2), VentanaV(input.Coord.y)));
    segundo.a = 1;
    float vena = saturate(segundo.r * 1.6 - 0.05);

    // LA RAMPA — el perfil de calor transversal.
    float4 rampa = tex2D(texRampa, float2(frac(uTime * 2 + input.Coord.x), VentanaV(input.Coord.y)));

    // EL CUERPO: carne × rampa con la caída y el pulso — la textura ENTERA
    // vive (el segundo ya no la mata: la colorea).
    float4 cuerpo = carne * rampa * caida * pulso;
    float brillo = length(cuerpo.rgb);

    // v6.56.8 — LA PALETA DEL CONJURO (morado · rojo · negro): el color del
    // vértice (la paleta morado/rojo que viaja a lo largo del haz) partido
    // en DOS TONOS: el CUERPO violeta profundo (el «negro» del conjuro) y
    // las VENAS rojas (donde el segundo ruido enciende).
    float3 tonoBase = input.Color.rgb * float3(0.42, 0.30, 0.72);
    float3 tonoVena = input.Color.rgb * float3(1.60, 0.38, 0.75) + float3(0.22, 0.02, 0.04);
    float3 tono = lerp(tonoBase, tonoVena, vena * envV);

    float4 laser = cuerpo * float4(tono, 1.0) * 1.55;
    laser.rgb += pow(brillo, 2) * tonoVena * caida * 0.95;

    // LA FALDA — la funda del núcleo: violeta profundo que RESPIRA ROJO
    // donde las venas corren (el borde del conducto arde con ellas).
    float3 tonoFalda = lerp(tonoBase, tonoVena, vena * envV * 0.55);
    float4 falda = float4(rampa.rgb * tonoFalda * caida * envV * 1.15, 0);

    // v6.56.8 — EL NÚCLEO QUE LATE: el umbral de la vena blanca respira con
    // un seno lento — el corazón del conducto se hincha y afloja.
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
    // v6.56.8 — EL HALO (EL DESENFOQUE PER-PIXEL): la falda geométrica
    // pow³ que muere EXACTAMENTE en el borde del polígono del strip,
    // texturada con la carne a su propio flujo — UNA sola muestra. LA
    // FRANJA DEL FILO: cerca del borde el tono del halo salta al ROJO de
    // las venas (borde²) — el filo del conjuro arde sin muestrear triple.
    // Nada de quads: el borde del halo es una curva por píxel.
    // ================================================================
    float geo = saturate(1.0 - abs(input.Coord.y - 0.5) * 2.0);
    float haloGeo = geo * geo * geo;
    float borde = 1.0 - envV;
    float4 haloTex = tex2D(texCarne, float2(frac(uTime * 1.3 + input.Coord.x * 0.7), VentanaV(input.Coord.y)));
    float3 haloTono = lerp(tonoBase, tonoVena, borde * borde * 0.7);
    float3 halo = haloTex.rgb * haloTono * caida * haloGeo * 1.1;

    // LA COMPOSICIÓN: chispas + cuerpo + falda — el núcleo entero muere en
    // su envolvente; el halo vive FUERA, hasta el borde del strip.
    float4 color = laser + falda + chispa;
    color.rgb = lerp(color.rgb, float3(1.0, 0.97, 1.0), nucleo * envV);
    color.rgb = color.rgb * envV + halo;
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
