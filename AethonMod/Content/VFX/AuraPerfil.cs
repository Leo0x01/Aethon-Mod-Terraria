// ======================================================================
    //  AuraPerfil — v6.177 — LAS RECETAS DEL AURA (la letra de la
    //  sesión 30: partir los gigantes por responsabilidades SIN tocar
    //  comportamiento). Aquí viven ParticulasAura y AuraPerfil con sus
    //  fábricas (OleadaGrimorio · Las Formas Ascendidas · HaloArcoiris ·
    //  BrasaDelEclipse · HambreDelGrimorio) — mudadas INTACTAS byte a
    //  byte de AuraLib.cs por tools/parte_archivos_v6177.py. El MOTOR
    //  (Actualizar, emisores) vive en AuraLib.cs; los DIBUJOS en
    //  AuraLibDibujo.cs.
    // =====================================================================
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace AethonMod.Content.VFX
{
    // ======================================================================
    //  LA CONFIGURACIÓN DE PARTÍCULAS
    // ======================================================================

    /// <summary>
    /// Las partículas del aura: cenizas, chispas o fragmentos que se
    /// desprenden del borde y ascienden. TODO modificable por código
    /// (fluido): forma, color, transparencia (o sólidas), velocidad,
    /// ascenso, tamaño, vida y tasa de emisión.
    /// </summary>
    public sealed class ParticulasAura
    {
        /// <summary>Slots VIVOS máximos por aura (el techo del emisor).</summary>
        public int Cantidad = 8;
        /// <summary>La forma de cada partícula.</summary>
        public FormaParticula Forma = FormaParticula.Orbe;
        /// <summary>El color de la partícula.</summary>
        public Color Color = new Color(235, 235, 240);
        /// <summary>Alfa de la partícula (la transparencia: 0.35 = velada).</summary>
        public float Alfa = 0.35f;
        /// <summary>¿SÓLIDAS? true = alfa pleno (opacas), false = veladas.</summary>
        public bool Solidas = false;
        /// <summary>Emisiones por segundo.</summary>
        public float Tasa = 0.5f;
        /// <summary>Velocidad radial hacia fuera (px/s).</summary>
        public float Velocidad = 26f;
        /// <summary>Ascenso vertical (px/s — el humo sube).</summary>
        public float Ascenso = 30f;
        /// <summary>Tamaño en px.</summary>
        public float Tamano = 8f;
        /// <summary>Vida en segundos.</summary>
        public float Vida = 1.1f;

        /// <summary>La forma de las partículas.</summary>
        public ParticulasAura ConForma(FormaParticula v) { Forma = v; return this; }
        /// <summary>El color de las partículas.</summary>
        public ParticulasAura ConColor(Color v) { Color = v; return this; }
        /// <summary>La transparencia (0..1) de las partículas.</summary>
        public ParticulasAura ConAlfa(float v) { Alfa = v; return this; }
    }

    // ======================================================================
    //  EL PERFIL — el aura entera, descrita en código
    // ======================================================================

    /// <summary>
    /// AuraPerfil — UN AURA ENTERA descrita como datos. Todo configurables
    /// por código puro con métodos fluidos (la API de la biblioteca):
    ///
    ///   var aura = new AuraPerfil
    ///       { Radio = 70f, Patron = PatronAura.Poligono, Lados = 6 }
    ///       .ConTrasera(centro, medio, borde)
    ///       .ConFrontal(centro, medio, borde)
    ///       .ConDistorsion(1f).ConBlur(1f).ConGlow(1.3f)
    ///       .ConParticulas(new ParticulasAura().ConForma(FormaParticula.Chispa)…);
    ///   (v6.80 — la purga de muertos retiró ConRadio/ConPatron/ConLados/
    ///   ConHumo: cero llamadores; la casa configura por inicializador.)
    ///
    /// CONTRATO DE LAS ZONAS DE COLOR: cada capa (trasera/frontal) pinta
    /// tres zonas — CENTRO, INTERMEDIA y BORDE — y el radio de cada gajo
    /// decide la mezcla. El BORDE además tiñe el halo de glow y (en el
    /// patrón Polígono) las aristas de la jaula: "colorear bordes, centro
    /// y zonas intermedias" es literal aquí.
    ///
    /// v6.50.12 — LA CAPA VIVA (la investigación de auras, destilada de
    /// los tutoriales de AE + el canon VFX de juego): Borde (la cáscara
    /// de energía — rim hacia blanco en la frontera), Parpadeo (el
    /// flicker orgánico de dos senos inconmensurables), Hervor (la
    /// ebullición: flipbook de las 4 variantes de ruido con crossfade por
    /// el pase de blur), Rayos (las agujas radiales de la corona) y
    /// Latido (el doble golpe de 84 bpm que reemplaza la respiración).
    /// </summary>
    public sealed class AuraPerfil
    {
        // === GEOMETRÍA ===
        /// <summary>Radio del aura en px (desde el centro de la criatura).</summary>
        public float Radio = 64f;
        /// <summary>Anillos de gajos (2–4 recomendado).</summary>
        public int Anillos = 3;
        /// <summary>Gajos por anillo (10–16 recomendado).</summary>
        public int Gajos = 12;
        /// <summary>El patrón de la energía.</summary>
        public PatronAura Patron = PatronAura.Perlin;
        /// <summary>Lados del N-gono (patrón Poligono).</summary>
        public int Lados = 6;
        /// <summary>Velocidad de giro del polígono (rad/s).</summary>
        public float Giro = 0.25f;

        // === COLORES POR ZONA — CAPA TRASERA (detrás de la criatura) ===
        /// <summary>Color de la zona CENTRO de la capa trasera.</summary>
        public Color TCentro = new Color(200, 200, 205);
        /// <summary>Color de la zona INTERMEDIA de la capa trasera.</summary>
        public Color TMedio = new Color(225, 228, 232);
        /// <summary>Color de la zona BORDE de la capa trasera.</summary>
        public Color TBorde = new Color(240, 244, 248);
        /// <summary>Alfa de la capa trasera (multiplicador global).</summary>
        public float AlfaTrasera = 0.26f;

        // === COLORES POR ZONA — CAPA FRONTAL (el velo, sobre la criatura) ===
        /// <summary>Color de la zona CENTRO del velo frontal.</summary>
        public Color FCentro = new Color(210, 210, 214);
        /// <summary>Color de la zona INTERMEDIA del velo frontal.</summary>
        public Color FMedio = new Color(228, 230, 234);
        /// <summary>Color de la zona BORDE del velo frontal.</summary>
        public Color FBorde = new Color(240, 242, 246);
        /// <summary>¿Dibujar el velo frontal? (la capa que PISA a la criatura).</summary>
        public bool VeloFrontal = true;
        /// <summary>
        /// Alfa del velo frontal: 0.05–0.10 = la transparencia del 90–95%
        /// que pide el look clásico del anime — la criatura "emite luz desde
        /// dentro" sin perder contraste.
        /// </summary>
        public float AlfaFrontal = 0.06f;

        // === HUMO / MOVIMIENTO ===
        /// <summary>Velocidad del churn: los anillos contrarrotan (rad/s).</summary>
        public float Fluir = 0.4f;
        /// <summary>Ciclos de deriva radial por segundo (el humo hacia fuera).</summary>
        public float Deriva = 0.14f;
        /// <summary>Ascenso del humo (px/s — el fuego sube).</summary>
        public float Ascenso = 12f;
        /// <summary>Amplitud de la DISTORSIÓN (wobble de posición y ángulo).</summary>
        public float Distorsion = 1f;
        /// <summary>0 = sin desenfoque; 1 = el pase doble barato (offset+alfa).</summary>
        public float Blur = 1f;
        /// <summary>Multiplicador del halo de glow exterior.</summary>
        public float Glow = 1.3f;

        // === v6.50.12 — LA CAPA VIVA (la investigación de auras) ===
        // Las cinco técnicas que faltaban, destiladas de los tutoriales
        // de aura de AE (composición por capas: glow + turbulencia +
        // gradient + flicker + agujas) y del canon VFX de juego (Keyser /
        // Schreibt: forma dura sobre masa blanda, ruido que HIERVE):

        /// <summary>
        /// EL BORDE CALIENTE (0..1): la CÁSCARA de energía — los gajos de
        /// la zona exterior tienden a blanco al llegar a la frontera (el
        /// "rim glow": la silueta del aura se LEE contra el fondo). 0 = el
        /// look plano de v6.50.11.
        /// </summary>
        public float Borde = 0.25f;

        /// <summary>
        /// EL PARPADEO ORGÁNICO (0..0.15 recomendado): un vaivén de brillo
        /// de dos senos inconmensurables (nunca se repite) — el aura VIVA.
        /// Sutil a propósito: energía respirando, no estrobo. 0 = quieto.
        /// </summary>
        public float Parpadeo = 0.06f;

        /// <summary>
        /// LA EBULLICIÓN (variaciones de ruido por segundo): el índice de
        /// la textura de ruido AVANZA (flipbook de las 4 variantes del
        /// generador) con fase propia por gajo, y el pase de blur dibuja
        /// la variante SIGUIENTE — crossfade gratis entre fases. 0 = el
        /// ruido estático de siempre.
        /// </summary>
        public float Hervor = 5f;

        /// <summary>
        /// LOS RAYOS RADIALES (agujas de luz del contorno; 0 = off): finas
        /// líneas del cuerpo al borde que laten desfasadas — la corona
        /// radiante de los tutoriales (cuesta exactamente Rayos quads).
        /// </summary>
        public int Rayos = 0;

        /// <summary>
        /// EL LATIDO DEL CORAZÓN: el doble golpe (~84 bpm: golpe + eco a
        /// 0.18 del ciclo) reemplaza la respiración suave — el aura que
        /// ACECHA (el Hambre, el Juicio) late; la que acompaña, respira.
        /// </summary>
        public bool Latido = false;

        // === v6.50.23 — EL PATRÓN BRUMA (la Brasa del Eclipse) ===

        /// <summary>
        /// EL ALFA DE LA BRUMA OSCURA (0..1): la densidad del humo
        /// negro-violeta que OSCURECE el mundo en los bordes (la única
        /// capa NO aditiva de la casa — en el lote aditivo negro = suma
        /// 0 = invisible, la lección #1 de la investigación R62-a). Solo
        /// el patrón Bruma la lee.
        /// </summary>
        public float AlfaBruma = 0.58f;

        /// <summary>
        /// LA ESCALERA DE BLOOM (0..3): las pasadas apiladas del cuerpo
        /// dorado — escala creciente, alfa decreciente (1.00/1.26/1.55 ·
        /// 1.0/0.45/0.20): la gaussiana final ES la SUMA de las pasadas
        /// (la misma receta de la pila 6+1 de los rayos v6.50.22 — y la
        /// regla AAA de la investigación R62-a: bloom = blur + capa
        /// encima). Solo el patrón Bruma la lee.
        /// </summary>
        public int Bloom = 3;

        // === v6.102 — LA CAPA DE TURBULENCIA (la Forma Ascendida 5) ===

        /// <summary>
        /// LA TURBULENCIA SUAVE (0..1): la capa que SOLO viste el aura —
        /// EL VIENTO DEL ESPÍRITU. HEBRAS de vapor advectadas por el
        /// flujo: unas orbitan a favor y otras en contra (EL
        /// CIZALLAMIENTO: la mitad de la banda cruza sobre la otra
        /// mitad), cada una respirando su radio (la cresta y el valle
        /// de su propia ola) y estiradas TANGENCIALMENTE (la veta del
        /// flujo, no la aguja). SUAVE a propósito: alfa bajo, deriva
        /// lenta — energía que respira, no vendaval. SOLO entre 0.68R
        /// y 0.96R: el cuerpo del portador, su contorno y TODO lo demás
        /// quedan intactos — la capa es del AURA y de nadie más.
        /// 0 = sin turbulencia (los perfiles de siempre, intactos).
        /// </summary>
        public float Turbulencia = 0f;

        /// <summary>
        /// LAS HEBRAS de la turbulencia (6–16 recomendado; la mitad a
        /// favor y la mitad en contra — el cizallamiento nace de la
        /// interferencia de las dos corrientes). Solo la lee la capa
        /// de turbulencia (Turbulencia > 0).
        /// </summary>
        public int Hebras = 12;

        // === SEMILLA Y PARTÍCULAS ===
        /// <summary>Semilla determinista: la MISMA apariencia siempre.</summary>
        public int Semilla = 0;
        /// <summary>La configuración de partículas (null = sin partículas).</summary>
        public ParticulasAura Particulas = null;

        // --- LA API FLUIDA (puro código) ---

        /// <summary>Los TRES colores de la capa trasera: centro, medio, borde.</summary>
        public AuraPerfil ConTrasera(Color centro, Color medio, Color borde)
        { TCentro = centro; TMedio = medio; TBorde = borde; return this; }
        /// <summary>Los TRES colores del velo frontal: centro, medio, borde.</summary>
        public AuraPerfil ConFrontal(Color centro, Color medio, Color borde)
        { FCentro = centro; FMedio = medio; FBorde = borde; VeloFrontal = true; return this; }
        /// <summary>Amplitud de la distorsión (wobble).</summary>
        public AuraPerfil ConDistorsion(float v) { Distorsion = v; return this; }
        /// <summary>Fuerza del desenfoque barato (0 = off).</summary>
        public AuraPerfil ConBlur(float v) { Blur = v; return this; }
        /// <summary>Multiplicador del halo de glow.</summary>
        public AuraPerfil ConGlow(float v) { Glow = v; return this; }
        /// <summary>La configuración de partículas (null = sin partículas).</summary>
        public AuraPerfil ConParticulas(ParticulasAura v) { Particulas = v; return this; }

        // --- LOS PRESETS DE LA CASA ---

        /// <summary>
        /// LA DE LAS OLEADAS DEL GRIMORIO: gris-blanca por defecto; en la
        /// oleada 10 el aura se pudre — cuerpo gris-negro con BORDES ROJOS
        /// oscuros (lo que pide la letra: la comida del libro harta de
        /// esperar). Jefes y monstruos de las oleadas visten ESTA.
        /// v6.48 — LA OLEADA ESPECIAL (11): EL JUICIO — el aura más
        /// vistosa de la casa: negra, bordes rojo INTENSO con coronas de
        /// chispas carmesí y ORO (el festín final paga en dos metales).
        /// </summary>
        public static AuraPerfil OleadaGrimorio(int oleada)
        {
            var p = new AuraPerfil
            {
                Radio = 58f,
                Anillos = 3,
                Gajos = 12,
                Patron = PatronAura.Perlin,
                Fluir = 0.45f,
                Deriva = 0.15f,
                Ascenso = 14f,
                Distorsion = 1.1f,
                Blur = 1f,
                Glow = 1.35f,
                Semilla = 770 + oleada * 13,
                AlfaTrasera = 0.26f,
                AlfaFrontal = 0.06f,
                VeloFrontal = true,
            };

            if (oleada >= 11)
            {
                // LA OLEADA ESPECIAL — EL JUICIO: negra con bordes rojo
                // intenso y chispas DOBLES (carmesí + oro), el doble de
                // vivas. Todos los jefes juntos visten ESTA.
                p.ConTrasera(new Color(20, 16, 20), new Color(44, 38, 44), new Color(196, 22, 32));
                p.ConFrontal(new Color(22, 18, 22), new Color(46, 40, 46), new Color(170, 18, 28));
                p.ConParticulas(new ParticulasAura
                {
                    Cantidad = 14,
                    Forma = FormaParticula.Chispa,
                    Color = new Color(210, 30, 42),
                    Alfa = 0.65f,
                    Solidas = false,
                    Tasa = 1.4f,
                    Velocidad = 42f,
                    Ascenso = 52f,
                    Tamano = 12f,
                    Vida = 0.8f,
                });
                p.Glow = 1.9f;
                p.Distorsion = 1.35f;
                // v6.50.12 — EL JUICIO viste la CORONA RADIANTE: agujas de
                // luz alrededor, ebullición furiosa y el borde al máximo —
                // el clímax se lee por DENSIDAD de técnicas, no por tamaño.
                p.Rayos = 10;
                p.Borde = 0.45f;
                p.Parpadeo = 0.10f;
                p.Hervor = 7f;
                p.Latido = true;
            }
            else if (oleada >= 10)
            {
                // LA OLEADA FINAL: gris-negra con bordes rojo oscuro.
                p.ConTrasera(new Color(28, 24, 28), new Color(48, 44, 50), new Color(142, 16, 24));
                p.ConFrontal(new Color(30, 26, 30), new Color(50, 46, 52), new Color(120, 14, 22));
                p.ConParticulas(new ParticulasAura
                {
                    Cantidad = 10,
                    Forma = FormaParticula.Chispa,
                    Color = new Color(178, 26, 38),
                    Alfa = 0.55f,
                    Solidas = false,
                    Tasa = 0.9f,
                    Velocidad = 34f,
                    Ascenso = 40f,
                    Tamano = 10f,
                    Vida = 0.9f,
                });
                p.Glow = 1.6f;
                // v6.50.12 — la oleada podrida LATE (84 bpm): el corazón
                // del hambre se oye con los ojos.
                p.Latido = true;
                p.Borde = 0.35f;     // la costra roja del borde, más presente
                p.Parpadeo = 0.08f;  // la carne vibra
            }
            else
            {
                // EL HAMBRE COMÚN: humo gris-blanco, cenizas pálidas.
                p.ConTrasera(new Color(198, 200, 206), new Color(224, 227, 232), new Color(240, 244, 248));
                p.ConFrontal(new Color(210, 212, 216), new Color(230, 232, 236), new Color(242, 245, 248));
                p.ConParticulas(new ParticulasAura
                {
                    Cantidad = 8,
                    Forma = FormaParticula.Orbe,
                    Color = new Color(232, 232, 236),
                    Alfa = 0.32f,
                    Solidas = false,
                    Tasa = 0.55f,
                    Velocidad = 26f,
                    Ascenso = 32f,
                    Tamano = 8f,
                    Vida = 1.1f,
                });
            }
            return p;
        }

        /// <summary>
        /// v6.48 — LA FORMA ASCENDIDA: el aura de la Luz Primordial que
        /// Aethon deja caer al reconocerte como un par (su drop prometido
        /// desde v5, cumplido). Luz dorada-violeta respirando alrededor
        /// del portador — la corona del que ya no necesita invocarla.
        ///
        /// v6.50.45 — EL PATRÓN DIVINO (la reescritura TOTAL: «se sigue
        /// viendo simple, no es nada divina, además tiene 2 círculos de
        /// color plano semitransparentes»): Patron = Divino — el halo
        /// dorado sobre la cabeza, el círculo rúnico bajo los pies, los
        /// rayos divinos, los ecos y las chispas estelares (todo en
        /// EmitirDivino) — y Glow = 0 y Rayos = 0: LOS DOS CÍRCULOS
        /// PLANOS (el halo de glow de quads sin textura) y las agujas
        /// genéricas MUEREN — este perfil NUNCA pasa por ese camino.
        /// v6.110 — LAS ALAS MURIERON por la letra del usuario: «a la
        /// forma ascendida 1 quitale las alas».
        /// v6.111 — EL HAZ MURIÓ por la letra del usuario: «quítale el
        /// haz de luz» — la columna del cielo y su corte de rayos
        /// CERRARON: la primera luz es el AURA PURA (halo, escrituras,
        /// plumas cayendo): sin anatomía y sin puerta del cielo.
        /// </summary>
        public static AuraPerfil FormaAscendida()
        {
            var p = new AuraPerfil
            {
                Radio = 88f,          // v6.50.47 — LA APOTEOSIS: el dios OCUPA MÁS cielo (74 → 88)
                Anillos = 0,
                Gajos = 0,
                Patron = PatronAura.Divino,   // v6.50.45 — SU propio camino
                Fluir = 0.6f,
                Deriva = 0.12f,
                Ascenso = 16f,        // v6.50.47 — los orbes suben con MÁS fervor (12 → 16)
                Distorsion = 0.9f,
                Blur = 1f,
                Glow = 0f,            // v6.50.45 — CERO DISCOS PLANOS (el reclamo, muerto)
                Semilla = 150,
                AlfaTrasera = 0.34f,  // v6.50.47 — la santidad ARDE un poco más (0.28 → 0.34)
                AlfaFrontal = 0.06f,
                VeloFrontal = false,  // v6.50.45 — el divino es TODO trasero aditivo
                Rayos = 0,            // v6.50.45 — las agujas genéricas mueren (el halo reina)
                Borde = 0.30f,
                Parpadeo = 0.04f,
            };
            p.ConTrasera(new Color(255, 236, 170), new Color(196, 150, 255), new Color(255, 251, 230));
            p.ConFrontal(new Color(255, 240, 180), new Color(200, 156, 255), new Color(255, 253, 240));
            p.ConParticulas(new ParticulasAura
            {
                Cantidad = 22,        // v6.50.47 — la apoteosis: MÁS orbes al cielo (14 → 22)
                Forma = FormaParticula.Orbe,
                Color = new Color(255, 240, 190),
                Alfa = 0.5f,
                Solidas = false,
                Tasa = 1.2f,
                Velocidad = 20f,
                Ascenso = 34f,
                Tamano = 9f,
                Vida = 1.6f,
            });
            return p;
        }

        /// <summary>
        /// v6.50.48 — LA FORMA ASCENDIDA 2: LA APOTEOSIS ABSOLUTA (la
        /// letra del usuario: «crear un nuevo item de la forma ascendida
        /// 2 donde lo hagas mas divino y sagrado y al mismo tiempo
        /// implementes los cambios como las alas con mas plumas y todos
        /// esos efectos del anterior»). Un ítem NUEVO junto al original
        /// (la Forma 1 queda como estaba); esta es su perfil: el doble
        /// de grande (Radio 88 → 116), el doble de orbes (22 → 30) y
        /// la paleta PLATINO: blanco sagrado al centro, oro rosa al
        /// medio, celeste al borde — el lenguaje del arte sacro.
        /// </summary>
        public static AuraPerfil FormaAscendidaDos()
        {
            var p = new AuraPerfil
            {
                Radio = 116f,         // el dios de la apoteosis absoluta OCUPA el cielo
                Anillos = 0,
                Gajos = 0,
                Patron = PatronAura.Divino2,   // SU propio camino (EmitirDivino2)
                Fluir = 0.7f,
                Deriva = 0.14f,
                Ascenso = 22f,        // los orbes suben con TODO el fervor
                Distorsion = 1.0f,
                Blur = 1f,
                Glow = 0f,            // CERO discos planos (el contrato de la .45)
                Semilla = 648,
                AlfaTrasera = 0.40f,  // la santidad ARDE al doble
                AlfaFrontal = 0.06f,
                VeloFrontal = false,
                Rayos = 0,
                Borde = 0.32f,
                Parpadeo = 0.04f,
            };
            // LA PALETA PLATINO: blanco sagrado · oro rosa · celeste.
            p.ConTrasera(new Color(255, 253, 246), new Color(255, 224, 196), new Color(206, 236, 255));
            p.ConFrontal(new Color(255, 250, 240), new Color(255, 228, 205), new Color(220, 242, 255));
            p.ConParticulas(new ParticulasAura
            {
                Cantidad = 30,        // el doble de orbes que la .47
                Forma = FormaParticula.Orbe,
                Color = new Color(255, 246, 210),
                Alfa = 0.55f,
                Solidas = false,
                Tasa = 1.4f,
                Velocidad = 24f,
                Ascenso = 42f,
                Tamano = 10f,
                Vida = 1.8f,
            });
            return p;
        }

        /// <summary>
        /// v6.50.49 — LA FORMA ASCENDIDA 3: EL TRONO (la letra del
        /// usuario: «agrega un 3 forma ascendida que se vea aun mas
        /// divino, investiga mods y busca en internet como es verse con
        /// divinidad celestial»). LA INVESTIGACIÓN: la iconografía del
        /// trono celestial — el lenguaje visual CANÓNICO de la
        /// divinidad (Ap 4 / Ez 1 / la Maiestas Domini del arte sacro):
        /// el ARCOÍRIS alrededor del trono (Ap 4:3), el MAR DE VIDRIO
        /// (Ap 4:6), LAS SIETE LÁMPARAS DE FUEGO (Ap 4:5), LAS RUEDAS
        /// DE OFANIM llenas de ojos (Ez 1) y LA CRUZ DE LUZ. El perfil:
        /// más grande que la apoteosis (Radio 116 → 132), más orbes
        /// (30 → 36) y la paleta PRISMA: blanco que contiene TODOS los
        /// colores al centro, oro blanco al medio y celeste al borde.
        /// </summary>
        public static AuraPerfil FormaAscendidaTres()
        {
            var p = new AuraPerfil
            {
                Radio = 132f,         // EL TRONO OCUPA el cielo entero
                Anillos = 0,
                Gajos = 0,
                Patron = PatronAura.Divino3,   // SU propio camino (EmitirDivino3)
                Fluir = 0.8f,
                Deriva = 0.16f,
                Ascenso = 26f,        // los orbes suben con el fervor del trono
                Distorsion = 1.0f,
                Blur = 1f,
                Glow = 0f,            // CERO discos planos (el contrato de la .45)
                Semilla = 949,
                AlfaTrasera = 0.44f,  // el trono ARDE más que la apoteosis
                AlfaFrontal = 0.06f,
                VeloFrontal = false,
                Rayos = 0,
                Borde = 0.34f,
                Parpadeo = 0.04f,
            };
            // LA PALETA PRISMA: blanco total al centro · oro blanco al
            // medio · celeste al borde (los colores VIVOS llegan por las
            // perlas del arcoíris y las plumas prismáticas).
            p.ConTrasera(new Color(255, 255, 255), new Color(255, 244, 214), new Color(198, 238, 255));
            p.ConFrontal(new Color(255, 252, 245), new Color(255, 246, 220), new Color(225, 244, 255));
            p.ConParticulas(new ParticulasAura
            {
                Cantidad = 36,        // la corte del trono: MÁS orbes que nunca
                Forma = FormaParticula.Orbe,
                Color = new Color(255, 250, 226),
                Alfa = 0.55f,
                Solidas = false,
                Tasa = 1.5f,
                Velocidad = 26f,
                Ascenso = 46f,
                Tamano = 10f,
                Vida = 1.9f,
            });
            return p;
        }

        /// <summary>
        /// v6.50.53 — LA FORMA ASCENDIDA 4 (EL SERAFÍN — la cuarta luz:
        /// «de hecho crea una 4 forma ascendida, asegurate de que sea
        /// algo divino, alas, halo, corona, aura celestial, luz, bruma,
        /// mas luz y destello, mejor iluminacion»). LA INVESTIGACIÓN
        /// (pedida): el serafín de Isaías 6 — EL QUE ARDE («saraph» =
        /// quemar): seis alas, cuerpo de fuego, halo triple y el templo
        /// lleno de humo. El perfil: el MÁS GRANDE de todos (Radio 140)
        /// y la paleta DEL FUEGO: blanco total al centro, oro blanco al
        /// medio y oro vivo al borde.
        /// </summary>
        public static AuraPerfil FormaAscendidaCuatro()
        {
            var p = new AuraPerfil
            {
                Radio = 140f,        // el serafín ocupa MÁS cielo que el trono
                Anillos = 0,
                Gajos = 0,
                Patron = PatronAura.Divino4,   // SU propio camino (EmitirDivino4)
                Fluir = 0.8f,
                Deriva = 0.16f,
                Ascenso = 28f,       // la corte del serafín sube con el fuego
                Distorsion = 1.0f,
                Blur = 1f,
                Glow = 0f,           // CERO discos planos (el contrato de la .45)
                Semilla = 1153,
                AlfaTrasera = 0.46f, // el serafín ARDE aún más que el trono
                AlfaFrontal = 0.06f,
                VeloFrontal = false,
                Rayos = 0,
                Borde = 0.34f,
                Parpadeo = 0.04f,
            };
            // LA PALETA DEL FUEGO: blanco total · oro blanco · oro vivo
            // (el serafín NO es prisma: es FUEGO BLANCO Y ORO — el arcoíris
            // vive en el trono de la Forma 3, donde el usuario lo puso).
            p.ConTrasera(new Color(255, 255, 255), new Color(255, 248, 224), new Color(255, 228, 154));
            p.ConFrontal(new Color(255, 253, 246), new Color(255, 250, 228), new Color(255, 238, 190));
            p.ConParticulas(new ParticulasAura
            {
                Cantidad = 40,       // la corte del serafín: la mayor de todas
                Forma = FormaParticula.Orbe,
                Color = new Color(255, 250, 224),
                Alfa = 0.58f,
                Solidas = false,
                Tasa = 1.6f,
                Velocidad = 28f,
                Ascenso = 50f,
                Tamano = 10f,
                Vida = 2.0f,
            });
            return p;
        }

        // ==================================================================
        //  v6.112 — LAS FÁBRICAS DE LAS LUCES 5, 6, 7, 9 Y 10 MURIERON por
        //  la letra del usuario («las otras formas 5, 6, 7, 9, 10 pueden
        //  ser borradas»): EL RÍO DE PLUMAS, EL ESCRIBA DE LA LUZ, EL
        //  OJO DE LA GALAXIA, EL CÁNTICO DE LAS LÁMPARAS y EL TRONO se
        //  disolvieron con sus perfiles. VIVEN la 1, la 2, la 3, la 4 y
        //  la 8 (LAS OFANIM — la octava luz que el usuario conservó).
        // ==================================================================
        /// <summary>
        /// v6.111 — LA FORMA ASCENDIDA 8 (LAS OFANIM — la octava luz,
        /// RE-CREADA por la letra: «regenera y vuelve a crear la forma
        /// ascendida 8"). LA ESPADA MURIÓ DISUELTA EN RUEDAS: LAS RUEDAS
        /// DENTRO DE RUEDAS de la visión de Ezequiel — TRES AROS girando
        /// cada uno en su plano (la ecuatorial, la vertical en contra, la
        /// interior aprisa), con OJOS DE LUZ sobre las llantas que
        /// PARPADEAN y MIRAN hacia donde va el portador, el fuego de
        /// fricción lamiendo los aros y el pie del trono bajo los pies.
        /// PALETA DE LAS RUEDAS: blanco fuego al centro, oro vivo al
        /// medio, BRASA VIOLETA al borde (la forja hecha rueda).
        /// </summary>
        public static AuraPerfil FormaAscendidaOcho()
        {
            var p = new AuraPerfil
            {
                Radio = 134f,              // el arcángel ocupa el cielo del serafín
                Anillos = 0,
                Gajos = 0,
                Patron = PatronAura.Divino8,   // SU propio camino (EmitirDivino8)
                Fluir = 0.55f,
                Deriva = 0.11f,
                Ascenso = 24f,             // las plumas de acero caen... y SUBEN las brasas
                Distorsion = 0.8f,
                Blur = 1f,
                Glow = 0f,                 // CERO discos planos (el contrato de las divinas)
                Semilla = 8110,
                AlfaTrasera = 0.40f,       // el acero BRILLA frío
                AlfaFrontal = 0.055f,
                VeloFrontal = false,       // TODO-trasero por diseño (la familia de las luces)
                Rayos = 0,
                Borde = 0.30f,
                Parpadeo = 0.04f,
            };
            // LA PALETA DE LAS OFANIM: blanco fuego -> oro vivo -> brasa violeta
            p.ConTrasera(new Color(255, 252, 240), new Color(255, 224, 130),
                         new Color(150, 84, 235));
            p.ConFrontal(new Color(255, 255, 250), new Color(255, 242, 200),
                         new Color(205, 165, 255));
            p.ConParticulas(new ParticulasAura
            {
                Cantidad = 22,       // las brasas del acero forjándose
                Forma = FormaParticula.Chispa,   // CHISPAS: las virutas del yunque celestial
                Color = new Color(232, 240, 255),
                Alfa = 0.55f,
                Solidas = false,
                Tasa = 1.3f,
                Velocidad = 26f,
                Ascenso = 30f,
                Tamano = 8f,
                Vida = 1.5f,
            });
            return p;
        }

        /// <summary>
        /// v6.50.54 — EL HALO ARCOÍRIS (la petición: «toma el arcoiris de la
        /// forma ascendida 3 y crea un nuevo item solo con ese arcoiris, que
        /// sea pequeño y rodee la cabeza del jugador»): el perfil MÍNIMO —
        /// la banda de siete franjas de la Forma 3 (VFXCore.Arcoiris) en un
        /// aro pequeño (rx 42 px) alrededor de la cabeza, con SUS perlas
        /// zodiacales de color y el rim blanco. Radio pequeño (46 — es un
        /// adorno, no un trono) y la paleta del prisma.
        /// </summary>
        public static AuraPerfil HaloArcoiris()
        {
            var p = new AuraPerfil
            {
                Radio = 46f,          // pequeño — rodea la CABEZA, no el cielo
                Anillos = 0,
                Gajos = 0,
                Patron = PatronAura.HaloArcoiris,   // SU propio camino (EmitirHaloArcoiris)
                Fluir = 0.8f,
                Deriva = 0.0f,
                Ascenso = 0f,
                Distorsion = 0f,
                Blur = 0f,
                Glow = 0f,
                Semilla = 3313,
                AlfaTrasera = 0.0f,   // NADA de Emitir genérico: TODO lo dibuja el camino propio
                AlfaFrontal = 0.0f,
                VeloFrontal = false,
                Rayos = 0,
                Borde = 0f,
                Parpadeo = 0f,
            };
            p.ConTrasera(new Color(255, 255, 255), new Color(255, 255, 255), new Color(255, 255, 255));
            p.ConFrontal(new Color(255, 255, 255), new Color(255, 255, 255), new Color(255, 255, 255));
            return p;
        }

        /// <summary>
        /// v6.50.23 — LA BRASA DEL ECLIPSE: el CUARTO tipo de aura de la
        /// casa (la petición con nombre y apellidos: «un accesorio con
        /// algún tipo nuevo de aura usando luz, bruma, desenfoque,
        /// distorsión, bloom, glow, ruido perlin, negra en los bordes,
        /// dorada en el medio y roja en el centro, todo por código»).
        ///
        /// LA PILA (la receta destilada de la investigación R62-a — la
        /// separación humo/emisivo de Battlefront II y el negro-violeta
        /// ~RGB(14,11,18) que "ensucia" en vez de apagar):
        ///   · LA BRUMA (alfa-blend, NO aditiva): fBm de DOBLE warp de
        ///     dominio con ascenso horneado y máscara ANULAR — humo
        ///     negro-violeta OSCURECIENDO el mundo en los BORDES, el
        ///     corazón abierto. Dos capas parallax (escala/contrarrota).
        ///   · LA LUZ: el halo de glow — derrame dorado 1.5R + calor
        ///     rojo 0.85R (el billboard-blob de GTA V).
        ///   · EL NÚCLEO ROJO: las llamas finas (fBm de picos) latiendo
        ///     RÁPIDO con el Hervor — el corazón casi blanco.
        ///   · EL CUERPO DORADO: el fBm warpeado con la ESCALERA DE
        ///     BLOOM (3 pasadas: 1.00/1.26/1.55 × 1.0/0.45/0.20).
        ///   · LOS FANTASMAS: 2 pasadas desplazadas en dirección de
        ///     ruido — la DISTORSIÓN heat-haze sin shaders.
        ///   · EL DESENFOQUE: blur box horneado en cada lámina.
        ///   · LA LUZ DE MUNDO: el portador la enciende (Lighting).
        /// ZONAS: TCentro = el ROJO del núcleo, TMedio = el ORO del
        /// medio, TBorde = el NEGRO-VIOLETA de la bruma — el contrato
        /// de siempre: «negra en los bordes, dorada en el medio y roja
        /// en el centro», LITERAL en la API de las tres zonas.
        /// </summary>
        public static AuraPerfil BrasaDelEclipse()
        {
            var p = new AuraPerfil
            {
                Radio = 62f,
                Patron = PatronAura.Bruma,
                Fluir = 0.55f,      // el churn de la bruma (contrarrotación del núcleo)
                Deriva = 0.18f,     // el humo se despega hacia fuera
                Ascenso = 24f,      // las brasas suben (partículas)
                Distorsion = 1.25f, // LOS FANTASMAS del heat haze + el wobble
                Blur = 1f,          // el desenfoque horneado de las láminas
                Glow = 1.65f,       // el halo de luz (la capa "luz")
                Semilla = 65023,    // "65023": la versión que nació
                AlfaTrasera = 0.30f, // la pila brillante aditiva
                AlfaBruma = 0.66f,  // LA BRUMA oscura (alfa-blend — el valor del mock VLM 9/10)
                AlfaFrontal = 0.05f,
                VeloFrontal = true,
                Bloom = 3,          // la escalera del cuerpo dorado
                Parpadeo = 0.07f,   // el vaivén de brillo orgánico
                Hervor = 6f,        // el núcleo late RÁPIDO
                Latido = true,      // la brasa ACECHA (84 bpm)
            };
            // LAS TRES ZONAS DE LA LETRA: rojo al centro, oro en el
            // medio, negro-violeta en los bordes (la bruma tiñe con TBorde).
            p.ConTrasera(new Color(255, 56, 38), new Color(255, 186, 64), new Color(14, 11, 18));
            p.ConFrontal(new Color(120, 18, 12), new Color(140, 84, 20), new Color(20, 14, 24));
            // LAS BRASAS: chispas del núcleo que escapan del humo.
            p.ConParticulas(new ParticulasAura
            {
                Cantidad = 12,
                Forma = FormaParticula.Chispa,
                Color = new Color(255, 96, 28),
                Alfa = 0.60f,
                Solidas = false,
                Tasa = 1.1f,
                Velocidad = 30f,
                Ascenso = 55f,
                Tamano = 11f,
                Vida = 0.9f,
            });
            return p;
        }

        /// <summary>
        /// LA DEL HAMBRE DEL PROPIO JUGADOR: la ceniza grisácea que el
        /// grimorio hambriento exhala sobre su portador — crece con la
        /// intensidad (los momentos de hambre). Sutil a propósito: es el
        /// AVISO, no el castigo.
        /// </summary>
        public static AuraPerfil HambreDelGrimorio(int intensidad)
        {
            var p = new AuraPerfil
            {
                Radio = 40f + 2.5f * Math.Max(0, intensidad),
                Anillos = 2,
                Gajos = 10,
                Patron = PatronAura.Perlin,
                Fluir = 0.3f,
                Deriva = 0.12f,
                Ascenso = 16f,
                Distorsion = 0.8f,
                Blur = 0.7f,
                Glow = 0.7f,
                Semilla = 1301,
                AlfaTrasera = 0.15f,
                AlfaFrontal = 0.05f,
                VeloFrontal = true,
                // v6.50.12 — EL AVISO LATE: el hambre del portador pulsa a
                // 84 bpm (se nota, pero sutil — el recordatorio, no el
                // castigo: los valores mínimos de la casa).
                Latido = true,
                Borde = 0.15f,
                Parpadeo = 0.05f,
                Hervor = 4f,
            };
            p.ConTrasera(new Color(118, 114, 110), new Color(138, 136, 130), new Color(158, 158, 156));
            p.ConFrontal(new Color(120, 116, 112), new Color(140, 138, 132), new Color(160, 160, 158));
            p.ConParticulas(new ParticulasAura
            {
                Cantidad = 6,
                Forma = FormaParticula.Orbe,
                Color = new Color(112, 108, 104),
                Alfa = 0.3f,
                Solidas = false,
                Tasa = 0.3f,
                Velocidad = 14f,
                Ascenso = 22f,
                Tamano = 6f,
                Vida = 1.4f,
            });
            return p;
        }
    }
}
