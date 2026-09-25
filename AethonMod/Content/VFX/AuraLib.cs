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
    /// <summary>
    /// Los PATRONES de la biblioteca: cómo se reparte la energía alrededor
    /// de la criatura.
    /// </summary>
    public enum PatronAura
    {
        /// <summary>
        /// El ruido de Perlin clásico: gajos de ruido en anillos
        /// contrarrotantes que respiran y se derivan hacia fuera como humo.
        /// </summary>
        Perlin = 0,
        /// <summary>
        /// EL PATRÓN GEOMÉTRICO ABSTRACTO: el mismo ruido de Perlin pero
        /// ENJAULADO en un N-gono que gira — los gajos se pegotean a los
        /// bordes del polígono y este dibuja sus aristas con el color de
        /// BORDE del perfil (una jaula de energía giratoria).
        /// </summary>
        Poligono = 1,
        /// <summary>
        /// Anillos concéntricos que nacen del cuerpo y se expanden hacia
        /// fuera como pulsos de sonar, coloreados por zona.
        /// </summary>
        Anillos = 2,
    }

    /// <summary>La FORMA de las partículas del aura.</summary>
    public enum FormaParticula
    {
        /// <summary>Punto suave (brillo radial).</summary>
        Orbe = 0,
        /// <summary>Chispa alargada orientada a su velocidad.</summary>
        Chispa = 1,
        /// <summary>Rombo compacto (rotado 45°).</summary>
        Rombo = 2,
    }

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

        /// <summary>Cantidad máxima de partículas vivas.</summary>
        public ParticulasAura ConCantidad(int v) { Cantidad = v; return this; }
        /// <summary>La forma de las partículas.</summary>
        public ParticulasAura ConForma(FormaParticula v) { Forma = v; return this; }
        /// <summary>El color de las partículas.</summary>
        public ParticulasAura ConColor(Color v) { Color = v; return this; }
        /// <summary>La transparencia (0..1) de las partículas.</summary>
        public ParticulasAura ConAlfa(float v) { Alfa = v; return this; }
        /// <summary>¿Partículas SÓLIDAS (opacas) o veladas?</summary>
        public ParticulasAura ConSolidas(bool v) { Solidas = v; return this; }
        /// <summary>Emisiones por segundo.</summary>
        public ParticulasAura ConTasa(float v) { Tasa = v; return this; }
        /// <summary>Velocidad radial (px/s).</summary>
        public ParticulasAura ConVelocidad(float v) { Velocidad = v; return this; }
        /// <summary>Ascenso vertical (px/s).</summary>
        public ParticulasAura ConAscenso(float v) { Ascenso = v; return this; }
        /// <summary>Tamaño en px.</summary>
        public ParticulasAura ConTamano(float v) { Tamano = v; return this; }
        /// <summary>Vida en segundos.</summary>
        public ParticulasAura ConVida(float v) { Vida = v; return this; }
    }

    // ======================================================================
    //  EL PERFIL — el aura entera, descrita en código
    // ======================================================================

    /// <summary>
    /// AuraPerfil — UN AURA ENTERA descrita como datos. Todo configurables
    /// por código puro con métodos fluidos (la API de la biblioteca):
    ///
    ///   var aura = new AuraPerfil()
    ///       .ConRadio(70f).ConPatron(PatronAura.Poligono).ConLados(6)
    ///       .ConTrasera(centro, medio, borde)
    ///       .ConFrontal(centro, medio, borde)
    ///       .ConHumo(fluir: 0.4f, deriva: 0.14f, ascenso: 12f)
    ///       .ConDistorsion(1f).ConBlur(1f).ConGlow(1.3f)
    ///       .ConParticulas(new ParticulasAura().ConForma(FormaParticula.Chispa)…);
    ///
    /// CONTRATO DE LAS ZONAS DE COLOR: cada capa (trasera/frontal) pinta
    /// tres zonas — CENTRO, INTERMEDIA y BORDE — y el radio de cada gajo
    /// decide la mezcla. El BORDE además tiñe el halo de glow y (en el
    /// patrón Polígono) las aristas de la jaula: "colorear bordes, centro
    /// y zonas intermedias" es literal aquí.
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

        // === SEMILLA Y PARTÍCULAS ===
        /// <summary>Semilla determinista: la MISMA apariencia siempre.</summary>
        public int Semilla = 0;
        /// <summary>La configuración de partículas (null = sin partículas).</summary>
        public ParticulasAura Particulas = null;

        // --- LA API FLUIDA (puro código) ---

        /// <summary>Radio del aura en px.</summary>
        public AuraPerfil ConRadio(float v) { Radio = v; return this; }
        /// <summary>Anillos de gajos.</summary>
        public AuraPerfil ConAnillos(int v) { Anillos = Math.Max(1, v); return this; }
        /// <summary>Gajos por anillo.</summary>
        public AuraPerfil ConGajos(int v) { Gajos = Math.Max(4, v); return this; }
        /// <summary>El patrón de la energía (Perlin / Poligono / Anillos).</summary>
        public AuraPerfil ConPatron(PatronAura v) { Patron = v; return this; }
        /// <summary>Lados del N-gono del patrón Poligono.</summary>
        public AuraPerfil ConLados(int v) { Lados = Math.Max(3, v); return this; }
        /// <summary>Velocidad de giro del polígono (rad/s).</summary>
        public AuraPerfil ConGiro(float v) { Giro = v; return this; }
        /// <summary>Los TRES colores de la capa trasera: centro, medio, borde.</summary>
        public AuraPerfil ConTrasera(Color centro, Color medio, Color borde)
        { TCentro = centro; TMedio = medio; TBorde = borde; return this; }
        /// <summary>Los TRES colores del velo frontal: centro, medio, borde.</summary>
        public AuraPerfil ConFrontal(Color centro, Color medio, Color borde)
        { FCentro = centro; FMedio = medio; FBorde = borde; VeloFrontal = true; return this; }
        /// <summary>Alfa global de la capa trasera.</summary>
        public AuraPerfil ConAlfaTrasera(float v) { AlfaTrasera = v; return this; }
        /// <summary>¿Dibujar el velo frontal?</summary>
        public AuraPerfil ConVelo(bool v) { VeloFrontal = v; return this; }
        /// <summary>Alfa del velo frontal (0.05–0.10 = 90–95% de transparencia).</summary>
        public AuraPerfil ConAlfaFrontal(float v) { AlfaFrontal = v; return this; }
        /// <summary>El comportamiento de HUMO: churn, deriva radial y ascenso.</summary>
        public AuraPerfil ConHumo(float fluir, float deriva, float ascenso)
        { Fluir = fluir; Deriva = deriva; Ascenso = ascenso; return this; }
        /// <summary>Amplitud de la distorsión (wobble).</summary>
        public AuraPerfil ConDistorsion(float v) { Distorsion = v; return this; }
        /// <summary>Fuerza del desenfoque barato (0 = off).</summary>
        public AuraPerfil ConBlur(float v) { Blur = v; return this; }
        /// <summary>Multiplicador del halo de glow.</summary>
        public AuraPerfil ConGlow(float v) { Glow = v; return this; }
        /// <summary>Semilla determinista del aura.</summary>
        public AuraPerfil ConSemilla(int v) { Semilla = v; return this; }
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
        /// v6.48 — LA CORONA RÚNICA DE AURA: el patrón POLÍGONO con
        /// ConLados(5) y los tintes de la casa (violeta del Sagrario +
        /// oro del grimorio) hecho COSMÉTICO — un pentágono de runas
        /// girando alrededor del jugador con chispas doradas. Cuesta UN
        /// PRESET (no un arma): la Bolsa de Cosméticos la reparte.
        /// </summary>
        public static AuraPerfil CoronaRunica()
        {
            var p = new AuraPerfil
            {
                Radio = 54f,
                Anillos = 2,
                Gajos = 10,
                Patron = PatronAura.Poligono,
                Lados = 5,
                Giro = 0.35f,
                Fluir = 0.5f,
                Deriva = 0.10f,
                Ascenso = 8f,
                Distorsion = 0.55f,
                Blur = 0.6f,
                Glow = 1.5f,
                Semilla = 5150, // "5150": pentágono rúnico
                AlfaTrasera = 0.30f,
                AlfaFrontal = 0.05f,
                VeloFrontal = true,
            };
            // LOS TINTES DEL MOD: violeta del Sagrario al centro, el oro
            // del grimorio en el BORDE (las aristas de la jaula dorada).
            p.ConTrasera(new Color(122, 66, 200), new Color(158, 96, 232), new Color(255, 214, 130));
            p.ConFrontal(new Color(126, 70, 204), new Color(162, 100, 236), new Color(255, 226, 150));
            p.ConParticulas(new ParticulasAura
            {
                Cantidad = 8,
                Forma = FormaParticula.Chispa,
                Color = new Color(255, 214, 130),
                Alfa = 0.55f,
                Solidas = false,
                Tasa = 0.8f,
                Velocidad = 20f,
                Ascenso = 26f,
                Tamano = 9f,
                Vida = 1.2f,
            });
            return p;
        }

        /// <summary>
        /// v6.48 — LA FORMA ASCENDIDA: el aura de la Luz Primordial que
        /// Aethon deja caer al reconocerte como un par (su drop prometido
        /// desde v5, cumplido). Luz dorada-violeta respirando alrededor
        /// del portador — la corona del que ya no necesita invocarla.
        /// </summary>
        public static AuraPerfil FormaAscendida()
        {
            var p = new AuraPerfil
            {
                Radio = 60f,
                Anillos = 3,
                Gajos = 14,
                Patron = PatronAura.Perlin,
                Fluir = 0.6f,
                Deriva = 0.12f,
                Ascenso = 10f,
                Distorsion = 0.7f,
                Blur = 1f,
                Glow = 1.8f,
                Semilla = 150,
                AlfaTrasera = 0.28f,
                AlfaFrontal = 0.06f,
                VeloFrontal = true,
            };
            p.ConTrasera(new Color(255, 236, 170), new Color(196, 150, 255), new Color(255, 251, 230));
            p.ConFrontal(new Color(255, 240, 180), new Color(200, 156, 255), new Color(255, 253, 240));
            p.ConParticulas(new ParticulasAura
            {
                Cantidad = 10,
                Forma = FormaParticula.Orbe,
                Color = new Color(255, 240, 190),
                Alfa = 0.5f,
                Solidas = false,
                Tasa = 1.0f,
                Velocidad = 18f,
                Ascenso = 30f,
                Tamano = 9f,
                Vida = 1.3f,
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

    // ======================================================================
    //  LA BIBLIOTECA
    // ======================================================================

    /// <summary>
    /// AuraLib — LA LIBRERÍA DEL AURA, la octava hermana de la casa
    /// (PantallaLib, MoldeLib, CompásLib, FormaLib, MediaResLib, CieloLib,
    /// EcoLib… ahora AuraLib).
    ///
    /// EL AURA (investigado contra las referencias del anime — las dos
    /// imágenes del usuario, analizadas con el ojo del VLM): la energía que
    /// una criatura desprende. Se compone de DOS CAPAS:
    ///   · CAPA TRASERA — el cuerpo del humo: ruido de Perlin procedural
    ///     (texturas de ruido GENERADAS EN CÓDIGO, sin assets) dispuesto en
    ///     anillos contrarrotantes de gajos, con distorsión, desenfoque
    ///     barato (pase doble desplazado) y glow aditivo. Se dibuja
    ///     DETRÁS de la criatura.
    ///   · CAPA FRONTAL — el VELO: la misma geometría con una transparencia
    ///     del 90–95% (alfa 0.05–0.10) pisando al cuerpo — la criatura
    ///     "emite desde dentro", la marca del look clásico.
    ///
    /// TODO ES CÓDIGO: las texturas de ruido nacen de un generador de
    /// value-noise envolvente (fBm de 4 octavas, semilla FIJA — cero
    /// Main.rand en toda la biblioteca), los patrones son matemática pura
    /// y las partículas viven en el emisor de la casa.
    ///
    /// CONTRATO:
    /// - Vestir(npc, perfil) / Desvestir(npc): el CONSUMIDOR (p. ej.
    ///   OleadaNPC) guarda el perfil y llama a los dibujos.
    /// - DibujarNPC(npc, perfil, frontal): emite los cuadros al buffer de
    ///   VFXCore y lo vuelca ADITIVO. Se llama desde PreDraw (frontal:
    ///   false — detrás del cuerpo) y PostDraw (frontal: true — el velo).
    ///   Gestiona el lote: cierra el activo, vuelca, y el LLAMADOR reabre
    ///   el lote para el sprite de vanilla.
    /// - DibujarJugador(ref drawInfo, perfil, frontal): el camino DrawData
    ///   (capas de jugador — mismo estado que el resto del jugador).
    /// - Actualizar(npc/player, perfil): emisión y avance de partículas
    ///   (deterministas — Hash01, nunca Main.rand).
    /// - Reiniciar(): OnWorldUnload/Unload — cero estática huérfana.
    ///
    /// REGLAS DE LA CASA: render 100% determinista (el tiempo es
    /// GlobalTimeWrappedHourly, la variedad es Hash01 por semilla/slot);
    /// el presupuesto de cuadros de VFXCore se consulta ANTES de emitir;
    /// cero GC por frame (los búferes se reutilizan).
    /// </summary>
    public static class AuraLib
    {
        // ------------------------------------------------------------------
        //  LAS TEXTURAS DE RUIDO — generadas EN CÓDIGO (cero assets)
        // ------------------------------------------------------------------

        /// <summary>Cuatro variantes de ruido (128², PREMULTIPLICADAS:
        /// RGB = alfa = ruido — v6.50.8, la lección de BrumaBrushes).</summary>
        private static Texture2D[] _ruido;
        private const int LadoRuido = 128;
        private const int VariantesRuido = 4;

        /// <summary>
        /// Genera (una sola vez, perezosamente — el dispositivo gráfico ya
        /// existe cuando se dibuja) las 4 texturas de ruido: fBm de value
        /// noise con retícula ENVOLVENTE (el ruido tilea perfecto) en 4
        /// octavas (celdas 8/16/32/64). La semilla es un System.Random FIJO
        /// — determinista, no Main.rand, y corre una vez en la vida del
        /// proceso: no es render.
        /// </summary>
        private static void AsegurarTexturas()
        {
            // v6.50.3 — FIX (texturas muertas): el guard solo miraba null —
            // una Dispose ajena (descarga parcial, device reseteado) dejaba
            // el array con cadáveres y las auras morían en silencio. NOTA
            // honesta: el Texture2D PLANO de FNA no expone IsContentLost
            // (solo los RenderTarget lo tienen — verificado contra el FNA.dll
            // real); el guard cubre null/IsDisposed, que es lo que esta API
            // permite observar. La primera llamada puede venir del camino de
            // DIBUJO (documentado desde v6.49: System.Random determinista por
            // variante, NO Main.rand) y corre UNA vez por sesión.
            if (_ruido != null)
            {
                try
                {
                    for (int v = 0; v < _ruido.Length; v++)
                        if (_ruido[v] == null || _ruido[v].IsDisposed)
                        {
                            DisposeRuido();
                            break;
                        }
                }
                catch { DisposeRuido(); }
                if (_ruido != null) return;
            }
            try
            {
                var device = Main.graphics?.GraphicsDevice;
                if (device == null) return; // menú/carga: se reintenta al dibujar

                _ruido = new Texture2D[VariantesRuido];
                for (int v = 0; v < VariantesRuido; v++)
                {
                    var rnd = new System.Random(0x41555241 + v * 7919); // "AURA" + variante
                    float[,] oct1 = Reticula(rnd, 8);
                    float[,] oct2 = Reticula(rnd, 16);
                    float[,] oct3 = Reticula(rnd, 32);
                    float[,] oct4 = Reticula(rnd, 64);

                    var data = new Color[LadoRuido * LadoRuido];
                    for (int y = 0; y < LadoRuido; y++)
                    {
                        for (int x = 0; x < LadoRuido; x++)
                        {
                            float fx = x / (float)LadoRuido;
                            float fy = y / (float)LadoRuido;
                            // fBm: las 4 octavas, pesos 8/4/2/1
                            float n = 0f;
                            n += Muestrear(oct1, 8, fx, fy) * 0.5f;
                            n += Muestrear(oct2, 16, fx, fy) * 0.25f;
                            n += Muestrear(oct3, 32, fx, fy) * 0.125f;
                            n += Muestrear(oct4, 64, fx, fy) * 0.0625f;
                            n = MathHelper.Clamp(n, 0f, 1f);
                            // curva suave: contraste medio, sin aplastar
                            n = n * n * (3f - 2f * n);
                            byte a = (byte)(n * 255f);
                            // v6.50.8 — FIX (LAS CAJAS BLANCAS del reporte
                            // del usuario): RGB = 255 CONSTANTE con el perfil
                            // SOLO en el alfa es EL anti-patrón de las
                            // texturas runtime — exactamente "el bug de los
                            // rectángulos" que BrumaBrushes v6.25 documentó
                            // y reparó ("premultiplicar o morir"): el lote
                            // ADITIVO de FNA es (One, One) — el alfa NUNCA
                            // gatea el aporte — y el premultiplicado del
                            // loader de PNG NO aplica a texturas creadas con
                            // SetData. Con RGB blanco plano, cada gajo del
                            // aura dibujaba un RECTÁNGULO BLANCO
                            // semitransparente (el "solo cajas blancas" del
                            // accesorio). Con el RGB premultiplicado (= alfa)
                            // el perfil de ruido vive en el canal que el
                            // aditivo suma de verdad → NUBES de ruido de
                            // verdad en el lote aditivo y velo suave en el
                            // de alfa (la misma receta de BrumaBrushes y de
                            // las bandas Bolt* de StormLib).
                            data[y * LadoRuido + x] = new Color(a, a, a, a);
                        }
                    }
                    var tex = new Texture2D(device, LadoRuido, LadoRuido);
                    tex.SetData(data);
                    _ruido[v] = tex;
                }
            }
            catch
            {
                _ruido = null; // reintento silencioso al próximo frame
            }
        }

        /// <summary>Retícula de valores aleatorios de tamaño N (envolvente).</summary>
        private static float[,] Reticula(System.Random rnd, int n)
        {
            var r = new float[n, n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                    r[i, j] = (float)rnd.NextDouble();
            return r;
        }

        /// <summary>
        /// Muestreo BILINEAL ENVOLVENTE de la retícula (las coordenadas se
        /// envuelven con % 1: el ruido tilea sin costura).
        /// </summary>
        private static float Muestrear(float[,] r, int n, float fx, float fy)
        {
            float x = fx * n;
            float y = fy * n;
            int x0 = (int)x % n; if (x0 < 0) x0 += n;
            int y0 = (int)y % n; if (y0 < 0) y0 += n;
            int x1 = (x0 + 1) % n;
            int y1 = (y0 + 1) % n;
            float tx = x - (int)x;
            float ty = y - (int)y;
            // suavizado hermite en la interpolación (perlin-like)
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            float a = r[x0, y0] * (1 - tx) + r[x1, y0] * tx;
            float b = r[x0, y1] * (1 - tx) + r[x1, y1] * tx;
            return a * (1 - ty) + b * ty;
        }

        /// <summary>El ruido de la variante v (null si aún no hay dispositivo).</summary>
        private static Texture2D Ruido(int v)
        {
            AsegurarTexturas();
            if (_ruido == null) return null;
            return _ruido[v % VariantesRuido];
        }

        // ------------------------------------------------------------------
        //  EL EMISOR DE PARTÍCULAS (estado por entidad, cero GC por frame)
        // ------------------------------------------------------------------

        /// <summary>El estado de las partículas de UNA aura (arrays fijos).</summary>
        private class Emisor
        {
            public readonly float[] Ang = new float[16];
            public readonly float[] Rad = new float[16];
            public readonly float[] Edad = new float[16];
            public readonly float[] Vida = new float[16];
            public readonly float[] Tam = new float[16];
            public readonly float[] SemillaSlot = new float[16];

            // v6.50.2 — FIX: el npc.type DEL DUEÑO al registrarse — un
            // whoAmI reciclado DENTRO de los 240t del barrido heredaba el
            // emisor ajeno (glitch de 1-2 s de partículas a mitad de vida
            // orbitando al nuevo inquilino del índice). Con el tipo
            // guardado, el barrido y Actualizar detectan al usurpador.
            public int Tipo = -1;
        }

        /// <summary>Un emisor por NPC vivo (clave: whoAmI) + el del jugador local.</summary>
        private static readonly Dictionary<int, Emisor> _emisores = new Dictionary<int, Emisor>();
        private static Emisor _emisorJugador;

        // v6.49 — EL BARRIDO PERIÓDICO (hallazgo AUD-C): un NPC muerto ya
        // no corre AI → nadie llama Actualizar(!npc.active) → la entrada
        // quedaba en el diccionario hasta el Reiniciar, y el whoAmI
        // RECYCLADO heredaba las partículas a mitad de vida (glitch
        // visual). Cada 240 ticks se barren las claves cuyo NPC ya no
        // vive (barato: solo si hay emisores).
        private static uint _ultimaBarrida;

        /// <summary>
        /// v6.49 — LA BASURA SE SACA SOLA: borra los emisores de NPCs
        /// muertos/despawneados (el barrido que la muerte nunca hacía).
        /// v6.50.2 — FIX: también expulsa al whoAmI RECICLADO — si el índice
        /// lo ocupa OTRO NPC (type distinto del que registró el emisor),
        /// las partículas no son suyas: se purgan igual que las de un
        /// muerto. Lo llama Actualizar en su camino de lógica (no en render).
        /// </summary>
        private static void BarrerEmisores()
        {
            try
            {
                uint tick = Main.GameUpdateCount;
                if (tick - _ultimaBarrida < 240u) return; // cada ~4 s
                _ultimaBarrida = tick;
                if (_emisores.Count == 0) return;

                _barridoBuffer.Clear();
                foreach (var par in _emisores)
                {
                    int idx = par.Key;
                    // v6.50.2 — FIX: además de "ya no vive", "ya no es ÉL":
                    // un índice reciclado por OTRO tipo de NPC purga el
                    // emisor heredado (mismo tipo = el dueño legítimo sigue
                    // vivo: se queda, que es lo que quiere la vida corta).
                    if (idx < 0 || idx >= Main.maxNPCs || !Main.npc[idx].active ||
                        Main.npc[idx].type != par.Value.Tipo)
                        _barridoBuffer.Add(idx);
                }
                for (int i = 0; i < _barridoBuffer.Count; i++)
                    _emisores.Remove(_barridoBuffer[i]);
            }
            catch { }
        }
        private static readonly List<int> _barridoBuffer = new List<int>(16);

        /// <summary>
        /// Avanza y emite las partículas del aura de un NPC (lo llama el
        /// consumidor desde AI — cada tick, no en el render). DETERMINISTA:
        /// la emisión decide por Hash01(semilla, slot, tick), jamás Main.rand.
        /// </summary>
        public static void Actualizar(NPC npc, AuraPerfil p)
        {
            if (npc == null) return;
            // el NPC murió → su emisor muere con él (el índice se recicla)
            if (!npc.active)
            {
                _emisores.Remove(npc.whoAmI);
                return;
            }
            BarrerEmisores(); // v6.49 — la basura de los que murieron SIN AI
            if (p == null || p.Particulas == null) return;
            if (!_emisores.TryGetValue(npc.whoAmI, out Emisor e) || e.Tipo != npc.type)
            {
                // v6.50.2 — FIX: whoAmI RECICLADO — el emisor hallado no fue
                // registrado por ESTE npc (type distinto): era del inquilino
                // anterior del índice y muere aquí, sin esperar al barrido
                // de 240t (el glitch visual dura 1 tick en vez de ~2 s).
                // También cubre el registro nuevo de toda la vida.
                _emisores.Remove(npc.whoAmI);
                e = new Emisor { Tipo = npc.type };
                _emisores[npc.whoAmI] = e;
            }
            AvanzarEmisor(e, p, npc.Center, p.Radio);
        }

        /// <summary>Lo mismo para el jugador local (la del hambre).</summary>
        public static void ActualizarJugador(Player player, AuraPerfil p)
        {
            if (player == null || p == null || p.Particulas == null) return;
            if (_emisorJugador == null) _emisorJugador = new Emisor();
            AvanzarEmisor(_emisorJugador, p, player.Center, p.Radio);
        }

        /// <summary>Avanza el emisor: envejece, emite y mueve las partículas.</summary>
        private static void AvanzarEmisor(Emisor e, AuraPerfil p, Vector2 centro, float radio)
        {
            var cfg = p.Particulas;
            float dt = 1f / 60f;
            uint tick = Main.GameUpdateCount;
            // la probabilidad POR SLOT y POR TICK que produce la TASA pedida
            // en total (Tasa emisiones/seg repartidas entre los slots vivos).
            // v6.49 — EL PRESUPUESTO ADAPTATIVO DE VERDAD (hallazgo AUD-C:
            // "CalidadFpsSystem alimenta una máquina sin motor"): la TASA
            // respira con VFXCore.FactorCalidad — si los FPS caen, las
            // partículas del AURA adelgazan solas (factor 0.5 = mitad de
            // emisión). Con factor 1 (el 99% del tiempo) es IDÉNTICO.
            float factor = VFXCore.FactorCalidad;
            float prob = cfg.Tasa * dt * factor / Math.Max(1, cfg.Cantidad);

            for (int i = 0; i < e.Ang.Length; i++)
            {
                if (i >= cfg.Cantidad) { e.Edad[i] = -1f; continue; }

                // emisión determinista por slot: Hash01(tick) < prob
                if (e.Edad[i] < 0f)
                {
                    float dado = VFXCore.Hash01(p.Semilla, i, (int)(tick & 0xFFFFF));
                    if (dado < prob)
                    {
                        float fase = VFXCore.Hash01(p.Semilla, 977, i);
                        e.Ang[i] = VFXCore.Hash01(p.Semilla, i, (int)(tick * 0.01f)) * MathHelper.TwoPi;
                        e.Rad[i] = radio * (0.35f + 0.5f * fase);
                        e.Edad[i] = 0f;
                        e.Vida[i] = cfg.Vida * (0.8f + 0.4f * fase);
                        e.Tam[i] = cfg.Tamano * (0.8f + 0.4f * dado);
                        e.SemillaSlot[i] = fase;
                    }
                    continue;
                }

                e.Edad[i] += dt;
                if (e.Edad[i] >= e.Vida[i]) { e.Edad[i] = -1f; continue; }

                // deriva: hacia fuera y hacia arriba (el humo sube)
                e.Rad[i] += cfg.Velocidad * dt;
                e.Ang[i] += (e.SemillaSlot[i] - 0.5f) * 0.5f * dt; // espiral leve
            }
        }

        // ------------------------------------------------------------------
        //  EL RENDER — emisión de cuadros al búfer de VFXCore
        // ------------------------------------------------------------------

        /// <summary>
        /// Dibuja el aura de un NPC: emite TODOS los cuadros al búfer de
        /// VFXCore y lo vuelca ADITIVO. frontal=false → la capa trasera
        /// (llamar desde PreDraw, ANTES del sprite); frontal=true → el velo
        /// (PostDraw). Cierra el lote activo para volcar.
        ///
        /// DEVUELVE true si el lote del llamador fue CERRADO (y hay que
        /// reabrirlo con <see cref="ReabrirLoteVanilla"/> para que vanilla
        /// siga dibujando) — false si el búfer quedó vacío o hubo un error
        /// (en el error el lote ya se dejó ABIERTO aquí mismo).
        /// </summary>
        public static bool DibujarNPC(NPC npc, AuraPerfil p, bool frontal)
        {
            if (npc == null || p == null || Main.netMode == NetmodeID.Server) return false;
            if (frontal && !p.VeloFrontal) return false;

            try
            {
                float factor = npcScale(npc);
                Emitir(npc.Center, p.Radio * factor, p, frontal, npc.whoAmI);
                if (VFXCore.QuadCount > 0)
                {
                    VFXCore.FlushAdditive(null, true); // cierra el lote del llamador
                    return true;
                }
                return false; // búfer vacío (sin textura aún / presupuesto): lote intacto
            }
            catch
            {
                // El contrato de rescate: el lote queda ABIERTO pase lo que
                // pase (si estaba cerrado se reabre; si estaba abierto, el
                // Begin falla sin tocar nada).
                try { ReabrirLoteVanilla(); } catch { }
                return false;
            }
        }

        /// <summary>
        /// Reabre el lote de sprites en el estado del dibujado de ENTIDADES
        /// de vanilla — v6.50.2 — FIX: ahora el patrón EXACTO de la casa
        /// (el de ~40 PreDraw/PostDraw del mod, medido en el decompile:
        /// Deferred · AlphaBlend · <see cref="Main.DefaultSamplerState"/>
        /// (PointClamp al dibujar a RT: pixel-art NÍTIDO) · sin depth ·
        /// <see cref="Main.Rasterizer"/> · <see cref="Main.Transform"/>).
        /// El LinearClamp + CullNone viejo dejaba el resto del pase de
        /// NPCs/proyectiles muestreando BILINEAL — sprites borrosos tras
        /// cualquier aura. Público a propósito:
        /// OleadaNPC (y cualquier consumidor futuro) reabre con esto.
        ///
        /// v6.50.11 — ahora DELEGA en VFXCore.ReabrirLoteVanilla (la
        /// implementación canónica con SONDA: idempotente — no pisa un
        /// Begin vivo — y con el Begin blindado; misma firma, mismos
        /// parámetros, cero first-chance).
        /// </summary>
        public static void ReabrirLoteVanilla()
        {
            // v6.50.2 — FIX (restore del pase de entidades): sampler/rasterizer
            // del pase de entidades de vanilla, no LinearClamp+CullNone.
            // v6.50.11 — delegación en el núcleo (sonda + curación).
            VFXCore.ReabrirLoteVanilla();
        }

        /// <summary>
        /// Dibuja el aura del JUGADOR por el camino DrawData (las capas de
        /// dibujado del jugador): emite los cuadros al búfer y los añade a
        /// la caché de DrawData del drawInfo — el pipeline oficial de tML,
        /// sin tocar el lote del renderer (el mismo contrato de las
        /// coronas de la casa).
        /// </summary>
        public static void DibujarJugador(ref PlayerDrawSet drawInfo, AuraPerfil p, bool frontal)
        {
            if (p == null) return;
            if (frontal && !p.VeloFrontal) return;
            try
            {
                Player pl = drawInfo.drawPlayer;
                if (pl == null || pl.dead) return;
                Emitir(pl.Center, p.Radio, p, frontal, 511);
                VFXCore.AppendToPlayerDraw(ref drawInfo);
            }
            catch { }
        }

        /// <summary>
        /// v6.48 — EL CAMINO ADITIVO DEL JUGADOR (la mejora pedida: el
        /// camino del halo-proyectil). Los NPCs dibujan su aura ADITIVA
        /// (neón); el jugador iba por DrawData (AlphaBlend) y las auras
        /// de color vivos salían planas (la lección v6.40 de las manchas
        /// planas). ESTE método emite el aura del jugador al búfer y lo
        /// VUELCA ADITIVO — lo llama el PORTADOR (AuraPortadorHalo), un
        /// proyectil cosmético pegado al jugador: su PreDraw tiene el
        /// lote BAJO NUESTRO CONTROL y vanilla dibuja los proyectiles
        /// ANTES que los jugadores → la capa queda DETRÁS del cuerpo.
        /// El VELO FRONTAL sigue por DrawData (al 6% no necesita neón).
        /// DEVUELVE true si el lote del llamador fue CERRADO (reabrir
        /// con ReabrirLoteVanilla) — el contrato de la casa.
        /// </summary>
        public static bool DibujarJugadorAditivo(Player pl, AuraPerfil p)
        {
            if (pl == null || p == null || Main.netMode == NetmodeID.Server) return false;
            if (pl.dead) return false;
            try
            {
                Emitir(pl.Center, p.Radio, p, frontal: false, 511);
                if (VFXCore.QuadCount > 0)
                {
                    VFXCore.FlushAdditive(null, true); // cierra el lote del llamador
                    return true;
                }
                return false;
            }
            catch
            {
                try { ReabrirLoteVanilla(); } catch { }
                return false;
            }
        }

        /// <summary>Escala del aura según el tamaño del NPC (los jefes visten más grande).</summary>
        private static float npcScale(NPC npc)
        {
            float s = (npc.width + npc.height) / 64f;
            if (s < 1f) s = 1f;
            if (s > 3f) s = 3f;
            return 0.75f + 0.45f * (s - 1f);
        }

        // --- LA EMISIÓN (la matemática compartida por ambos caminos) ---

        /// <summary>
        /// Emite el aura entera al búfer de VFXCore (coords de MUNDO). El
        /// volcado lo decide el llamador (aditivo para NPCs, DrawData para
        /// el jugador). 100% determinista: t = GlobalTimeWrappedHourly,
        /// variedad por Hash01(semilla, gajo, anillo).
        /// </summary>
        private static void Emitir(Vector2 centro, float radio, AuraPerfil p, bool frontal, int idEntidad)
        {
            Texture2D tex0 = Ruido(0);
            if (tex0 == null) return; // sin dispositivo aún: nada que dibujar

            float t = Main.GlobalTimeWrappedHourly;
            float R = radio * VFXCore.Breathe(t, 1.05f, 0f, 0.05f);
            Color cC = frontal ? p.FCentro : p.TCentro;
            Color cM = frontal ? p.FMedio : p.TMedio;
            Color cB = frontal ? p.FBorde : p.TBorde;
            float alfaCapa = frontal ? p.AlfaFrontal : p.AlfaTrasera;

            // === 1. EL HALO DE GLOW (el aliento exterior, color BORDE) ===
            // Dos cuadros SoftGlow concéntricos: el aura "respira" luz.
            if (p.Glow > 0.05f)
            {
                float haloR = R * 1.55f;
                float haloA = (alfaCapa * 0.55f * p.Glow);
                VFXCore.Quad(centro, cB * haloA, new Vector2(haloR * 2.0f, haloR * 2.0f));
                float corR = R * 1.15f;
                VFXCore.Quad(centro, cM * (alfaCapa * 0.4f * p.Glow), new Vector2(corR * 2.0f, corR * 2.0f));
            }

            // === 2. EL CUERPO: los gajos de ruido (patrón Perlin/Poligono) ===
            if (p.Patron != PatronAura.Anillos)
            {
                int cuadros = p.Anillos * p.Gajos * (p.Blur > 0.5f ? 2 : 1);
                if (VFXCore.Presupuesto(cuadros + 4))
                {
                    for (int r = 0; r < p.Anillos; r++)
                    {
                        float fracR = (r + 1f) / p.Anillos;
                        float dir = (r % 2 == 0) ? 1f : -1f; // contrarrotación
                        Texture2D tex = Ruido(r);            // variante de ruido por anillo

                        for (int i = 0; i < p.Gajos; i++)
                        {
                            float h = VFXCore.Hash01(p.Semilla, i, r);
                            // ciclo de humo del gajo: nace dentro, deriva fuera y muere
                            float cyc = Frac(t * p.Deriva + h * 1.7f + r * 0.33f);

                            float ang = i * (MathHelper.TwoPi / p.Gajos)
                                      + t * p.Fluir * dir
                                      + r * 0.35f
                                      + MathF.Sin(t * 2.3f + i * 2.7f) * 0.05f * p.Distorsion;

                            float rad = R * (0.55f + 0.5f * fracR)
                                      + cyc * R * 0.34f
                                      + MathF.Sin(t * 1.7f + h * 6.28f) * 4f * p.Distorsion;

                            // PATRÓN POLÍGONO: el radio se pega a la función
                            // radial del N-gono que gira — la jaula geométrica.
                            if (p.Patron == PatronAura.Poligono)
                            {
                                float a2 = ang + t * p.Giro;
                                float sector = MathHelper.TwoPi / p.Lados;
                                float local = ((a2 % sector) + sector) % sector - sector * 0.5f;
                                float k = MathF.Cos(MathHelper.Pi / p.Lados) /
                                          MathF.Max(0.35f, MathF.Cos(local));
                                rad *= 0.55f + 0.45f * MathHelper.Clamp(k, 0.5f, 1.25f);
                            }

                            Vector2 pos = centro + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rad;
                            pos.Y -= cyc * p.Ascenso * 0.55f; // el humo sube

                            // COLOR POR ZONA: la fracción radial decide la mezcla
                            float rr = MathHelper.Clamp((rad / R - 0.45f) * 0.85f, 0f, 1f);
                            Color col = Zona(cC, cM, cB, rr);
                            float alfa = alfaCapa
                                       * MathF.Sin(MathHelper.Pi * cyc)  // nace y muere suave
                                       * (0.55f + 0.45f * h);            // variedad por gajo

                            // tamaño tangencial: cubrir la cuerda del arco
                            float w = MathHelper.TwoPi * rad / p.Gajos * 1.5f;
                            float hgt = w * (0.5f + 0.5f * h);
                            float rot = ang + MathHelper.PiOver2
                                      + MathF.Sin(t * 3f + i) * 0.09f * p.Distorsion;

                            VFXCore.Quad(pos, col * alfa, new Vector2(w, hgt), rot, tex);

                            // EL DESENFOQUE BARATO: el mismo gajo, desplazado
                            // por la normal y a media alfa — el doble-pase suave.
                            if (p.Blur > 0.5f)
                            {
                                Vector2 normal = new Vector2(MathF.Cos(ang), MathF.Sin(ang));
                                VFXCore.Quad(pos + normal * (2.5f + 1.5f * p.Blur),
                                    col * (alfa * 0.4f), new Vector2(w * 1.15f, hgt * 1.2f), rot, tex);
                            }
                        }
                    }

                    // Las ARISTAS del polígono (patrón Poligono): la jaula
                    // dibuja sus bordes con el COLOR DE BORDE del perfil.
                    if (p.Patron == PatronAura.Poligono)
                    {
                        float rotP = t * p.Giro;
                        float rB = R * 1.05f;
                        Vector2 prev = VertexPoligono(centro, rB, rotP, p.Lados, p.Lados - 1);
                        for (int i = 0; i < p.Lados; i++)
                        {
                            Vector2 v = VertexPoligono(centro, rB, rotP, p.Lados, i);
                            VFXCore.Line(prev, v, cB * (alfaCapa * 0.9f), 2.2f);
                            prev = v;
                        }
                    }
                }
            }
            // === 2b. PATRÓN ANILLOS: pulsos concéntricos tipo sonar ===
            else
            {
                if (VFXCore.Presupuesto(p.Anillos * 2 + 4))
                {
                    for (int r = 0; r < p.Anillos; r++)
                    {
                        float fase = Frac(t * p.Deriva * 1.3f + r / (float)p.Anillos);
                        float rad = R * (0.45f + 0.75f * fase);
                        float alfa = alfaCapa * MathF.Sin(MathHelper.Pi * fase);
                        Color col = Zona(cC, cM, cB, fase);
                        Vector2 size = VFXCore.RingQuadSize(rad);
                        VFXCore.Quad(centro, col * alfa, size, VFXCore.Ring);
                        // su gemelo de glow (el eco del pulso)
                        if (p.Glow > 0.05f)
                            VFXCore.Quad(centro, cB * (alfa * 0.35f), size * 1.1f, VFXCore.Ring);
                    }
                }
            }

            // === 3. LAS PARTÍCULAS ===
            EmitirParticulas(centro, p, frontal, idEntidad);
        }

        /// <summary>El vértice k del N-gono de radio rB girado rotP.</summary>
        private static Vector2 VertexPoligono(Vector2 centro, float rB, float rotP, int lados, int k)
        {
            float a = rotP + k * (MathHelper.TwoPi / lados);
            return centro + new Vector2(MathF.Cos(a), MathF.Sin(a)) * rB;
        }

        /// <summary>
        /// Emite las partículas del aura (forma, color, transparencia —
        /// todo del perfil). El estado vive en el emisor de la entidad.
        /// </summary>
        private static void EmitirParticulas(Vector2 centro, AuraPerfil p, bool frontal, int idEntidad)
        {
            var cfg = p.Particulas;
            if (cfg == null) return;

            Emisor e = null;
            if (idEntidad == 511) e = _emisorJugador;
            else _emisores.TryGetValue(idEntidad, out e);
            if (e == null) return;

            float alfaP = cfg.Solidas ? 1f : cfg.Alfa;
            for (int i = 0; i < e.Ang.Length && i < cfg.Cantidad; i++)
            {
                if (e.Edad[i] < 0f || e.Vida[i] <= 0f) continue;
                float vida01 = e.Edad[i] / e.Vida[i];
                float alfa = alfaP * MathF.Sin(MathHelper.Pi * vida01);
                if (alfa <= 0.01f) continue;

                Vector2 pos = centro + new Vector2(MathF.Cos(e.Ang[i]), MathF.Sin(e.Ang[i])) * e.Rad[i];
                pos.Y -= vida01 * cfg.Ascenso * 0.6f;
                Color col = cfg.Color * alfa;
                float tam = e.Tam[i] * (1f - 0.35f * vida01); // se encogen al morir

                switch (cfg.Forma)
                {
                    case FormaParticula.Orbe:
                        VFXCore.Quad(pos, col, new Vector2(tam, tam));
                        break;
                    case FormaParticula.Chispa:
                        {
                            // rastro alargado en la dirección de la huida
                            Vector2 dir = new Vector2(MathF.Cos(e.Ang[i]), MathF.Sin(e.Ang[i]));
                            VFXCore.Line(pos, pos + dir * (tam * 1.8f), col, Math.Max(1.5f, tam * 0.32f));
                        }
                        break;
                    case FormaParticula.Rombo:
                        VFXCore.Quad(pos, col, new Vector2(tam, tam), MathHelper.PiOver4, Pixel);
                        break;
                }
            }
        }

        /// <summary>El píxel de vanilla (el rombo de 1px estirado y rotado 45°).</summary>
        private static Texture2D Pixel =>
            Terraria.GameContent.TextureAssets.MagicPixel.Value;

        /// <summary>La mezcla de las tres zonas: centro → medio → borde.</summary>
        private static Color Zona(Color centro, Color medio, Color borde, float x)
        {
            if (x < 0.5f) return Color.Lerp(centro, medio, x * 2f);
            return Color.Lerp(medio, borde, (x - 0.5f) * 2f);
        }

        /// <summary>La parte fraccionaria (siempre positiva).</summary>
        private static float Frac(float x)
        {
            x -= MathF.Floor(x);
            return x;
        }

        // ------------------------------------------------------------------
        //  HIGIENE DE LA CASA
        // ------------------------------------------------------------------

        /// <summary>v6.49 — el número de emisores vivos (lo pinta el overlay F8).</summary>
        public static int EmisoresVivos => _emisores.Count + (_emisorJugador != null ? 1 : 0);

        /// <summary>v6.50.3 — funeral del ruido (compartido por Reiniciar y
        /// la autocomprobación de device-lost de AsegurarTexturas).</summary>
        private static void DisposeRuido()
        {
            try
            {
                if (_ruido != null)
                {
                    for (int i = 0; i < _ruido.Length; i++)
                    {
                        _ruido[i]?.Dispose();
                        _ruido[i] = null;
                    }
                }
            }
            catch { }
            _ruido = null;
        }

        /// <summary>
        /// v6.49 — LA PURGA de emergencia (Unloaded/reinicios): emisores
        /// fuera, reloj a cero, el ruido al funeral.
        /// </summary>
        public static void Reiniciar()
        {
            _emisores.Clear();
            _emisorJugador = null;
            _ultimaBarrida = 0; // v6.49 — el reloj del barrido también
            DisposeRuido();
        }
    }
}
