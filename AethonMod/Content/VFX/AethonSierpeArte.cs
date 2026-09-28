using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// AethonSierpeArte — v6.50.27 — EL ARTE NUEVO DE LA SIERPE DEL FINAL.
    ///
    /// EL REPORTE (literal): «el arte del jefe se ve horrible, deberías
    /// cambiarlo por completo, algo al estilo de la sierpe en el arma La
    /// Sierpe Estelar». LA SIERPE ESTELAR (CodigosLib.SierpeEstelar) NO
    /// usa sprites: es CRIATURA DE CÓDIGO — el velo de SoftGlow que suda
    /// el cuerpo, la ESPINA de cápsulas, las APOFISIS de estrellas de 4
    /// puntas, las ALETAS de varillas, el CRÁNEO orbe con la MANDÍBULA
    /// en V, los DOS OJOS fríos y las chispas orbitando la cabeza. ESE
    /// es el ADN; este archivo lo viste de jefe final con LA PALETA DE
    /// LA LUZ PRIMORDIAL (oro + violeta + blanco cálido) y a ESCALA de
    /// pesadilla (ESC 1.4, 46 vértebras).
    ///
    /// LA LECCIÓN DEL ARTE VIEJO: los sprites de hueso (cráneo 120×168,
    /// mandíbula 60×110, vértebra 64×88) eran IMÁGENES pegadas al mundo:
    /// sin profundidad, sin rim, sin vida — a 1.4× se veían borrosos y
    /// planos. El arte de código respira: el velo late, las apófisis
    /// giran, el centrum arde, las aletas se abren — TODO es luz sobre
    /// un cuerpo de VACÍO (el pase alfa: cápsulas oscuras que dan
    /// CONTRASTE con cualquier fondo — cielo, cueva, infierno).
    ///
    /// EL CONTRATO: TODAS las posiciones en COORDENADAS DE PANTALLA
    /// (mundo − Main.screenPosition, como toda la casa); los bailes de
    /// lote los lleva cada método (pase alfa + pase aditivo, la sonda
    /// de la casa y el lote vanilla devuelto ABIERTO — CerrarBatch).
    /// </summary>
    public static class AethonSierpeArte
    {
        // ==================================================================
        //  LA PALETA — la Luz Primordial
        // ==================================================================

        /// <summary>El ORO de la Luz (ojos, filos, apófisis).</summary>
        public static readonly Color Oro = new(255, 240, 190);

        /// <summary>El VIOLETA de las fases altas (fase 3+).</summary>
        public static readonly Color Violeta = new(196, 150, 255);

        /// <summary>El BLANCO cálido del núcleo.</summary>
        public static readonly Color Blanco = new(255, 252, 240);

        private static readonly Color Velo = new(255, 214, 120);     // el sudor de luz del cuerpo
        private static readonly Color EspinaDorada = new(255, 244, 206); // la columna ardiendo (oro cálido)

        // EL CUERPO DE VACÍO (el contraste del pase alfa): la sierpe de
        // la Luz CABALGA un cuerpo de noche — las cápsulas oscuras.
        private static readonly Color VoidOscuro = new(24, 16, 38);
        private static readonly Color VoidMedio = new(52, 36, 88);

        private static Asset<Texture2D> _star;

        /// <summary>La estrella de 4 puntas (la apófisis de la casa).</summary>
        private static Texture2D Star =>
            (_star ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/Star")).Value;

        /// <summary>
        /// LA ESTRELLA DEL FONDO (el leviatán del cielo la pide para sus
        /// apófisis y su punta — la misma textura, cacheada y null-segura).
        /// </summary>
        public static Texture2D EstrellaDelFondo()
        {
            try { return Star; }
            catch { return VFXCore.SoftGlow; }
        }

        /// <summary>El tinte de intensidad de la casa (delega en OrbitaLib).</summary>
        private static Color T(Color c, float f) => OrbitaLib.Tint(c, f);

        // ==================================================================
        //  LA VÉRTEBRA — el hueso de la sierpe estelar del final
        // ==================================================================

        /// <summary>
        /// UNA VÉRTEBRA: el cuerpo de vacío (pase alfa: dos cápsulas
        /// oscuras — la silueta con profundidad) + LA LUZ DE LA COLUMNA
        /// (pase aditivo: el velo que suda, la espina de luz, la APOFISIS
        /// estelar girando, el CENTRUM ardiendo en el corazón del hueso).
        ///
        /// EL TAPER lo lleva el llamador (radio por índice — la sierpe se
        /// afina de 30 px junto al cráneo a ~9 en la punta).
        /// </summary>
        /// <param name="pos">Centro del hueso (PANTALLA).</param>
        /// <param name="rumbo">La DIRECCIÓN del hueso (rad — hacia la cabeza).</param>
        /// <param name="radio">El radio del hueso (px, ya escalado).</param>
        /// <param name="t">El tiempo animado.</param>
        /// <param name="idx">El índice en la cadena (la fase de los latidos).</param>
        /// <param name="alpha">La visibilidad global 0..1.</param>
        /// <param name="ladoAleta">0 = sin aleta; ±1 = el lado de la ALETA (las varillas).</param>
        public static void Vertebra(Vector2 pos, float rumbo, float radio,
            float t, int idx, float alpha, float ladoAleta = 0f)
        {
            if (alpha <= 0.02f) return;

            // === 1) EL PASE ALFA: el cuerpo de vacío (la silueta) ===
            OrbitaLib.AbrirAlpha();
            try
            {
                // LA PLACA del vacío (2.9·radio — ARTICULADA: cada hueso
                // se dibuja [placa → luz] y la placa del SIGUIENTE tapa la
                // cola de luz del anterior → segmentos de luz entre placas
                // de noche, el look de CUENTAS de La Sierpe Estelar — no
                // un tubo, una CRIATURA de segmentos)…
                OrbitaLib.Capsule(pos, radio * 2.95f, radio * 1.70f, rumbo,
                    T(VoidOscuro, 0.84f * alpha));
                // …y el canal medio (la espina dorsal del vacío — profundidad).
                OrbitaLib.Capsule(pos, radio * 2.9f, radio * 0.68f, rumbo,
                    T(VoidMedio, 0.85f * alpha));
            }
            finally { OrbitaLib.CerrarBatch(); }

            // === 2) EL PASE ADITIVO: la Luz de la columna ===
            OrbitaLib.AbrirAdditive();
            try
            {
                // EL VELO: el cuerpo entero sudando luz tenue (la Sierpe
                // Estelar mide 4.4× el radio — aquí igual, la familia).
                OrbitaLib.Quad(VFXCore.SoftGlow, pos,
                    new Vector2(radio * 3.4f, radio * 3.4f), 0f, T(Velo, 0.06f * alpha));

                // LA ESPINA DE LUZ: la cápsula caliente a lo largo del
                // hueso — ORO CÁLIDO (la paleta de la Luz Primordial: el
                // blanco se reserva para ojos y filos — el cuerpo ARDE
                // dorado como el Sol Rúnico).
                OrbitaLib.Capsule(pos, radio * 2.55f, radio * 0.62f, rumbo,
                    T(EspinaDorada, 0.65f * alpha));

                // LA APOFISIS ESTELAR: la estrella de 4 puntas girando
                // lento en el centrum (cada hueso con SU fase — la columna
                // entera respira en cascada).
                float tw = 0.7f + 0.3f * MathF.Sin(t * 2.6f + idx * 1.9f);
                OrbitaLib.Quad(Star, pos,
                    new Vector2(radio * 1.7f, radio * 1.7f), rumbo + t * 0.6f,
                    T(Oro, 0.38f * tw * alpha));

                // EL CENTRUM: el corazón de luz de cada hueso (el orbe
                // pequeño pulsando — la runa dorada del arte viejo, viva).
                float lat = 0.6f + 0.4f * MathF.Sin(t * 3.0f + idx * 0.7f);
                OrbitaLib.Quad(VFXCore.GlowOrb, pos,
                    new Vector2(radio * 0.50f, radio * 0.50f), 0f,
                    T(Oro, 0.42f * lat * alpha));

                // LAS ALETAS: las varillas de la Sierpe Estelar (3 por
                // abanico, perpendiculares al cuerpo — el lado alterna).
                if (ladoAleta != 0f)
                    AletaEnLote(pos, rumbo, ladoAleta, radio, t, alpha);
            }
            finally { OrbitaLib.CerrarBatch(); }
        }

        /// <summary>
        /// LAS ALETAS (dentro del lote aditivo ABIERTO): el abanico de
        /// TRES varillas frías perpendicular al cuerpo — el rasgo de pez
        /// abisal de La Sierpe Estelar (sus aletas en las i==8 e i==14).
        /// El latido del abanico respira con el tiempo.
        /// </summary>
        private static void AletaEnLote(Vector2 pos, float rumbo, float lado,
            float radio, float t, float alpha)
        {
            float perp = rumbo + MathHelper.PiOver2 * lado;
            float latido = 0.75f + 0.25f * MathF.Sin(t * 3.4f + lado * 1.4f + radio);
            for (int v = -1; v <= 1; v++)
            {
                float dirA = perp + v * 0.40f;
                float largo = (radio * 1.6f + radio * 0.55f * (1 - Math.Abs(v))) * latido;
                Vector2 punta = pos + new Vector2(MathF.Cos(dirA), MathF.Sin(dirA)) * largo;
                Vector2 mid = (pos + punta) * 0.5f;
                OrbitaLib.Capsule(mid, largo, radio * 0.22f, dirA,
                    T(Violeta, 0.45f * alpha));
                OrbitaLib.Quad(Star, punta,
                    new Vector2(radio * 0.5f, radio * 0.5f), dirA,
                    T(Violeta, 0.65f * alpha));
            }
        }

        // ==================================================================
        //  LA CABEZA — el cráneo orbe, las fauces en V, los ojos, la cresta
        // ==================================================================

        /// <summary>
        /// LA CABEZA DE LA LUZ PRIMORDIAL — el ensamblaje completo:
        ///   · EL RESPALDO DE VACÍO (pase alfa): una cápsula oscura tras el
        ///     cráneo (el cuello) y las DOS HOJAS de las mandíbulas — la
        ///     silueta lee contra cualquier fondo;
        ///   · EL CRÁNEO ORBE (pase aditivo): el halo de SoftGlow + el orbe
        ///     de núcleo sólido (GlowOrb) con el CORAZÓN blanco dentro;
        ///   · LAS FAUCES EN V: dos filos de luz (cápsulas doradas) que
        ///     giran ABRIÉNDOSE con <paramref name="abertura"/> — las
        ///     mandíbulas cinéticas, cada punta con su COLMILLO estrella;
        ///   · LOS DOS OJOS: orbes de oro (violeta en fase alta) con núcleo
        ///     blanco, FAREANDO con la abertura de la boca;
        ///   · LA CRESTA DORSAL: la varilla del lomo con su estrella;
        ///   · LAS CINCO CHISPAS ORBITANTES (la firma de La Sierpe Estelar).
        /// </summary>
        /// <param name="pos">El centro del cráneo (PANTALLA).</param>
        /// <param name="rot">LA ROTACIÓN DEL NPC (rumbo + π/2 — como tML).</param>
        /// <param name="abertura">La abertura de las fauces 0..0.6.</param>
        /// <param name="t">El tiempo animado.</param>
        /// <param name="fase">La fase de la pelea (1..5 — el color de ojos/chispas).</param>
        /// <param name="alpha">La visibilidad global 0..1.</param>
        /// <param name="esc">La ESCALA del final (AethonSierpeCuerpo.ESC).</param>
        public static void Cabeza(Vector2 pos, float rot, float abertura,
            float t, int fase, float alpha, float esc)
        {
            if (alpha <= 0.02f) return;
            float rumbo = rot - MathHelper.PiOver2;              // el frente de la sierpe
            Vector2 frente = new Vector2(MathF.Cos(rumbo), MathF.Sin(rumbo));
            Vector2 perp = new Vector2(-frente.Y, frente.X);
            Color cOjo = fase >= 3 ? Violeta : Oro;

            // === 1) EL RESPALDO DE VACÍO (pase alfa — la silueta) ===
            OrbitaLib.AbrirAlpha();
            try
            {
                // EL CUELLO: la primera vértebra engarzada al cráneo.
                OrbitaLib.Capsule(pos - frente * (34f * esc), 44f * esc, 52f * esc,
                    rumbo, T(VoidOscuro, 0.75f * alpha));
                // EL CRÁNEO DE VACÍO (la ESFERA SÓLIDA — el eclipse): el
                // GlowOrb sombreado pintado de NOCHE al 92% — una cabeza
                // con CUERPO, no un pila de brillos (la lección del mock:
                // la acumulación aditiva clipeaba a BOLA; la estructura
                // la da la materia oscura, la vida los ACENTOS de luz).
                OrbitaLib.Quad(VFXCore.GlowOrb, pos,
                    new Vector2(104f * esc, 104f * esc), rumbo, T(VoidOscuro, 0.92f * alpha));
                // LAS HOJAS DE LAS MANDÍBULAS (la sombra bajo el filo —
                // ALARGADAS: las fauces ABRAZAN el frente del cráneo).
                for (int m = -1; m <= 1; m += 2)
                {
                    float dirM = rumbo + m * (0.22f + abertura * 0.92f);
                    Vector2 piv = pos + perp * (m * 16f * esc) + frente * (22f * esc);
                    Vector2 fin = piv + new Vector2(MathF.Cos(dirM), MathF.Sin(dirM)) * (76f * esc);
                    OrbitaLib.Capsule((piv + fin) * 0.5f, 76f * esc, 34f * esc, dirM,
                        T(VoidOscuro, 0.90f * alpha));
                }
            }
            finally { OrbitaLib.CerrarBatch(); }

            // === 2) LA LUZ DEL CRÁNEO (pase aditivo) ===
            OrbitaLib.AbrirAdditive();
            try
            {
                // EL HALO (el sudor de luz de la cabeza — TENUE: el
                // eclipse + el anillo son los protagonistas).
                OrbitaLib.Quad(VFXCore.SoftGlow, pos,
                    new Vector2(170f * esc, 170f * esc), 0f, T(Velo, 0.08f * alpha));
                OrbitaLib.Quad(VFXCore.SoftGlow, pos,
                    new Vector2(110f * esc, 110f * esc), 0f, T(Violeta, 0.06f * alpha));

                // EL ANILLO DE ORO (EL ECLIPSE DE LA LUZ PRIMORDIAL — la
                // firma): el aro fino alrededor de la esfera de noche…
                OrbitaLib.AnilloFino(pos, 52f * esc, rumbo, T(Oro, 0.60f * alpha));
                // …y el CORAZÓN: el punto de luz que MIRA desde el centro
                // del eclipse (pequeño, caliente, único).
                OrbitaLib.Quad(VFXCore.GlowOrb, pos,
                    new Vector2(26f * esc, 26f * esc), rumbo, T(Blanco, 0.85f * alpha));

                // LAS FAUCES EN V: LOS FILOS DE LUZ — lo más BRILLANTE
                // de la cabeza (la mordida es el arma del jefe): cápsulas
                // doradas LARGAS que giran con la abertura, cada punta
                // con su COLMILLO estrella.
                for (int m = -1; m <= 1; m += 2)
                {
                    float dirM = rumbo + m * (0.22f + abertura * 0.92f);
                    Vector2 piv = pos + perp * (m * 16f * esc) + frente * (22f * esc);
                    Vector2 fin = piv + new Vector2(MathF.Cos(dirM), MathF.Sin(dirM)) * (70f * esc);
                    Vector2 mid = (piv + fin) * 0.5f;
                    // EL FILO: la hoja dorada de la mandíbula (ANCHA — la
                    // mordida con PESO).
                    OrbitaLib.Capsule(mid, 66f * esc, 17f * esc, dirM,
                        T(Oro, 0.75f * alpha));
                    // EL FILO BLANCO (el corte de la hoja — caliente).
                    OrbitaLib.Capsule(mid, 62f * esc, 6f * esc, dirM,
                        T(Blanco, 0.58f * alpha));
                    // EL COLMILLO: la estrella de la punta (la presa ve las
                    // puntas ANTES que el cráneo — el aviso de la mordida).
                    OrbitaLib.Quad(Star, fin,
                        new Vector2(30f * esc, 30f * esc), dirM, T(Blanco, 0.85f * alpha));
                }

                // LOS DOS OJOS (fríos, fijos al rumbo — FAREAN con la boca).
                float fare = 1f + abertura * 0.85f;
                for (int o = -1; o <= 1; o += 2)
                {
                    Vector2 ojo = pos + perp * (o * 24f * esc) + frente * (10f * esc);
                    OrbitaLib.Quad(VFXCore.GlowOrb, ojo,
                        new Vector2(20f * esc * fare, 20f * esc * fare), 0f,
                        T(cOjo, 0.78f * alpha));
                    OrbitaLib.Quad(VFXCore.GlowOrb, ojo,
                        new Vector2(10f * esc * fare, 10f * esc * fare), 0f,
                        T(Blanco, 0.85f * alpha));
                }

                // LA CRESTA DORSAL (la vela del lomo — DORSAL de verdad:
                // atrás y arriba, no lateral: el mock la leía como un
                // tercer aspa; ahora es corta y pegada al cráneo).
                Vector2 baseCresta = pos - frente * (14f * esc);
                Vector2 finCresta = baseCresta - perp * (10f * esc) - frente * (34f * esc);
                float dirC = MathF.Atan2(finCresta.Y - baseCresta.Y, finCresta.X - baseCresta.X);
                Vector2 midC = (baseCresta + finCresta) * 0.5f;
                OrbitaLib.Capsule(midC, 44f * esc, 9f * esc, dirC, T(Oro, 0.62f * alpha));
                OrbitaLib.Quad(Star, finCresta, new Vector2(22f * esc, 22f * esc),
                    dirC, T(Oro, 0.75f * alpha));

                // LAS CINCO CHISPAS ORBITANTES (la firma de La Sierpe
                // Estelar: su cabeza SEMBRA estrellas alrededor — aquí son
                // los granos de la Luz Primordial).
                int seed = 900; // la semilla fija de las chispas (determinista)
                for (int k = 0; k < 5; k++)
                {
                    float h = VFXCore.Hash01(seed, 800 + k, 97);
                    float angS = h * MathHelper.TwoPi + t * (1.1f + h);
                    Vector2 p = pos + new Vector2(MathF.Cos(angS), MathF.Sin(angS)) * (56f + 22f * h) * esc;
                    float tw = 0.4f + 0.6f * MathF.Sin(t * 3.5f + k * 2.2f);
                    OrbitaLib.Quad(Star, p, new Vector2(15f * esc, 15f * esc), angS,
                        T(fase >= 3 ? Violeta : Oro, 0.5f * tw * alpha));
                }
            }
            finally { OrbitaLib.CerrarBatch(); }
        }

        // ==================================================================
        //  LA COLA — la punta del látigo (para el leviatán del fondo)
        // ==================================================================

        /// <summary>
        /// LA PUNTA DE LA COLA (dentro de un lote YA ABIERTO — la usa el
        /// leviatán del cielo): la cápsula que se afina a estrella — la
        /// sierpe muere en PUNTA DE LUZ, no en hueso plano.
        /// </summary>
        public static void PuntaEnLote(Vector2 pos, float rumbo, float radio,
            float t, float alpha)
        {
            OrbitaLib.Capsule(pos, radio * 2.6f, radio * 0.85f, rumbo,
                T(Velo, 0.30f * alpha));
            OrbitaLib.Quad(Star, pos + new Vector2(MathF.Cos(rumbo), MathF.Sin(rumbo)) * radio * 0.9f,
                new Vector2(radio * 1.1f, radio * 1.1f), rumbo + t * 1.2f,
                T(Oro, 0.55f * alpha));
        }
    }
}
