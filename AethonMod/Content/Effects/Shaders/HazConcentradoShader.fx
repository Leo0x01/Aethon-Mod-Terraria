// =============================================================================
// HazConcentradoShader.fx — AethonMod (v6.55.0)
// EL HAZ CONCENTRADO — el shader de la casa para el arma de jugador.
//
// La técnica (estudiada y re-escrita 100 % para el mod): un haz de plasma
// vivo nace de la MULTIPLICACIÓN de cuatro texturas procedurales de la casa
// (CarnePlasma · SegundoRuido · RampaGlow · BitsChispa) con doble scroll
// cruzado, deformación sinusoidal del muestreo y las chispas en doble AND.
//
// LAS DOS CURAS DE LA CASA (la 2ª ronda del diagnóstico de los picos):
//   1. uEscalaV — LA ESCALA DE RUIDO COMPENSADA: el arma vive a tamaños
//      menores que el original y el ruido fino se leía como dientes de
//      sierra (texel≈2.7 px). El muestreo transversal se ENCOGE
//      proporcionalmente al tamaño real => los rasgos del plasma miden
//      SIEMPRE lo mismo en pantalla — orgánicos a cualquier escala.
//   2. EL NÚCLEO SUAVE — el umbral duro de saturación (if length > k
//      return 1) dibujaba un CONTORNO FRACTAL: aquí la vena blanca nace
//      de un smoothstep — el mismo latido blanco, el borde respira.
//
// El .fx viaja como fuente: tML lo compila al cargar (FxcReader) y si algo
// fallara, el proyectil tiene el fallback de tiras texturizadas de la casa.
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
    // LA ENVOLVENTE TRANSVERSAL (propia de la casa): el borde del haz
    // muere suave SIEMPRE — viva donde viva la textura muestreada.
    float envV = smoothstep(0.0, 0.30, input.Coord.y) * smoothstep(1.0, 0.70, input.Coord.y);

    // LA CAÍDA LONGITUDINAL: el arranque se enciende y el final se rinde
    // (más pronto en los bordes que en el corazón — la pluma del original).
    float caida = smoothstep(0.42, 0.0, input.Coord.x - sin(input.Coord.y * 3.14159) * 0.34);

    // LA CARNE — fBm con el scroll doble y la deformación sinusoidal.
    float vCarne = VentanaV(input.Coord.y + sin((uTime * 4 + input.Coord.x) * 6.2831 - 1.5708) * 0.05 * uEscalaV);
    float4 carne = tex2D(texCarne, float2(frac(uTime * 3 + input.Coord.x * 1.5), vCarne));

    // EL SEGUNDO RUIDO — el que CRUZA al primero (el hervor del producto).
    float4 segundo = tex2D(texSegunda, float2(frac(uTime * 4 + input.Coord.x * 1.2), VentanaV(input.Coord.y)));
    segundo.a = 1;

    // LA RAMPA — el perfil de calor transversal.
    float4 rampa = tex2D(texRampa, float2(frac(uTime * 2 + input.Coord.x), VentanaV(input.Coord.y)));

    // EL CUERPO: el producto de las capas con la envolvente y la caída.
    float4 cuerpo = carne * segundo * rampa * caida;
    float brillo = length(cuerpo.rgb);
    float4 laser = cuerpo * input.Color * 1.35;
    laser.rgb += pow(brillo, 2) * input.Color.rgb * caida * 0.85;

    // EL RESPLANDOR de la rampa sobre el color (la falda cálida).
    float4 falda = float4(rampa.rgb * input.Color.rgb * caida * envV, 0);

    // EL NÚCLEO SUAVE — LA CURA: la vena blanca por saturación nace de un
    // smoothstep (el mismo latido blanco del original, sin contorno
    // fractal — el borde del núcleo RESPIRA con el plasma).
    float nucleo = smoothstep(0.34, 0.62, length(laser.rgb) + brillo * 0.22);

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

    // LA COMPOSICIÓN: chispas + núcleo + cuerpo + falda — todo aditivo.
    float4 color = laser + falda + chispa;
    color.rgb = lerp(color.rgb, float3(1, 1, 1), nucleo * envV);
    color.rgb *= envV;
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
