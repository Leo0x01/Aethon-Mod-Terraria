using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    // ======================================================================
    //  v6.52.0 — BEAMLIB: LA BIBLIOTECA DEL RAYO MASIVO DE ENERGÍA.
    //
    //  LA INVESTIGACIÓN (Task 84 / R2 — notas completas en
    //  download/research/notes/R2-beam-techniques.md, extraídas de
    //  Calamity + Fargo's + Stars Above + Calamity Overhaul DECOMPILADOS
    //  y del vanilla decompilado al IL): los rayos masivos de los mods
    //  populares NO son una técnica — son SIETE CAPAS apiladas:
    //    (1) el raycast (Collision.LaserScan) con longitud SUAVIZADA
    //        (Lerp 0.5–0.9 — EL motion-blur del barrido: la punta se
    //        desliza, no salta),
    //    (2) la geometría (aquí: CINTAS de VFXCore sobre una espina
    //        ruidosa — el equivalente casa del triangle-strip de Calamity),
    //    (3) el RUIDO (fBm CPU de StormLib con fases LENTAS y anclas
    //        exp(-1200·d^8): el mismo Fbm1D del PerlinBolt pero con la
    //        amplitud al 3.5% del largo — «vivo, no espástico»),
    //    (4) las CAPAS ADITIVAS (halo ×2.3 → cuerpo → core blanco — el
    //        doble pase del Last Prism de vanilla, elevado a tres),
    //    (5) la LUZ DINÁMICA a lo largo de TODO el rayo (AddLightAlong
    //        cada 64 px — la MITAD del presupuesto vanilla de ~150/tick),
    //    (6) los DIAMANTES VIAJEROS (sin(along/92 − t·9)^7 — los paquetes
    //        de energía que RECORREN el rayo: la firma del GammaRayBeam
    //        de Calamity Overhaul),
    //    (7) las TAPAS (boca + punta + estallido si toca pared).
    //
    //  EL CONTRATO DE LA CASA (la lección v6.51.14 — la cura del crash
    //  de la Sombra II): esta librería NUNCA toca el lote del juego.
    //  Solo ENCOLA quads al búfer de VFXCore; el volcado único
    //  (FlushAdditive → finally ReabrirLoteVanilla) vive en el PreDraw
    //  del LLAMADOR. Cero Begin/End, cero BlendStates nuevos, cero
    //  GraphicsDevice crudo — esta clase NO PUEDE reproducir la clase
    //  de crash de la .51.13.
    //
    //  LA RECETA DE PARÁMETROS (bolt vs beam — la tabla del blueprint):
    //    amplitud:  12% del largo (rayo)  → 3.5% (beam)
    //    meandro:   2.8 ciclos           → 1.4 ciclos (la S larga)
    //    jitter:    ×0.25 del meandro, 6 ciclos
    //    fases:     GlobalTime·0.35 / ·1.9 (tiempo CONTINUO, no por flick)
    //    anclas:    exp(-1200·d^8) (más suave que el exp(-5000·d^10) del rayo)
    // ======================================================================
    public static class BeamLib
    {
        // ------------------------------------------------------------------
        //  LA TEXTURA DEL CUERPO — el ruido de tinta generada a mano
        //  (tools/gen_escriba_v6520.py): bandas horizontales elongadas de
        //  fBm con caída gaussiana vertical — al muestrearla con uvFlow la
        //  carne del rayo AVANZA raíz→punta (el «Perlin moviéndose en una
        //  dirección» de la letra del usuario).
        // ------------------------------------------------------------------
        private static Asset<Texture2D> _ruidoTinta;

        public static Texture2D RuidoTinta
        {
            get
            {
                if (_ruidoTinta == null)
                    _ruidoTinta = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/RuidoTinta");
                return _ruidoTinta.Value;
            }
        }

        /// <summary>El smoothstep de la casa (pendiente nula en los extremos).</summary>
        private static float Suave01(float p)
        {
            p = MathHelper.Clamp(p, 0f, 1f);
            return p * p * (3f - 2f * p);
        }

        // ==================================================================
        //  EL RAYO — la pila completa en UNA llamada (solo ENCOLA)
        // ==================================================================

        /// <summary>
        /// Encola al búfer de VFXCore el rayo masivo COMPLETO (las 7 capas
        /// de la receta). EL LLAMADOR es dueño del volcado:
        /// <code>
        ///   PreDraw:  BeamLib.Rayo(...);          // solo encola
        ///             if (VFXCore.QuadCount > 0) {
        ///                 try { VFXCore.FlushAdditive(); }
        ///                 finally { VFXCore.ReabrirLoteVanilla(); } }
        /// </code>
        /// </summary>
        /// <param name="origen">Boca del rayo (coordenadas de MUNDO).</param>
        /// <param name="dir">Dirección UNITARIA del rayo.</param>
        /// <param name="largo">Longitud ya SUAVIZADA por el llamador (el Lerp 0.75 de la receta).</param>
        /// <param name="ancho">Ancho del cuerpo en px (el halo sale ×2.3, el core ×0.42).</param>
        /// <param name="color">El tinte del cuerpo (el halo y el core lo derivan).</param>
        /// <param name="tiempo">Reloj de render DETERMINISTA (Main.GlobalTimeWrappedHourly).</param>
        /// <param name="semilla">Semilla por-proyectil (hace que dos rayos jamás tiemblen igual).</param>
        /// <param name="choquePared">True si el raycast cortó en tile (taper de pared + estallido).</param>
        /// <param name="flujo">Velocidad del avance de la carne (uvFlow — 0.9 por defecto).</param>
        /// <param name="intensidad">Multiplicador global de alfa (envelopes de vida/carga del llamador).</param>
        public static void Rayo(Vector2 origen, Vector2 dir, float largo, float ancho,
            Color color, float tiempo, int semilla,
            bool choquePared = false, float flujo = 0.9f, float intensidad = 1f)
        {
            if (dir.X == 0f && dir.Y == 0f) return;
            dir = dir.SafeNormalize(Vector2.UnitX);
            if (largo < 16f || ancho < 2f || intensidad <= 0.02f) return;
            largo = Math.Min(largo, 4200f);            // el techo del DrawScreenCheckFluff
            if (!VFXCore.CalidadPermitida(VFXCore.CalidadFX.Bloom)) return;

            // === (0) EL PRESUPUESTO — la calidad que respira de la casa ===
            float calidad = MathHelper.Clamp(VFXCore.FactorCalidad, 0.5f, 1f);
            int n = (int)MathF.Round(MathHelper.Lerp(16f, 34f, (calidad - 0.5f) * 2f));
            bool conHalo = calidad > 0.72f;
            int capas = conHalo ? 3 : 2;
            int diamantes = (int)(largo / 578f) + 1;   // un pico cada 92·2π px
            int estimado = capas * (n - 1) + diamantes + 6;
            if (!VFXCore.Presupuesto(estimado))
            {
                // LA DEGRADACIÓN ELEGANTE (el blueprint B1): sin halo,
                // espina a la mitad — el rayo VIVE aunque el frame esté caro.
                conHalo = false;
                capas = 2;
                n = Math.Max(12, n / 2);
                estimado = capas * (n - 1) + diamantes + 4;
                if (!VFXCore.Presupuesto(estimado)) return;
            }

            Vector2 perp = new Vector2(-dir.Y, dir.X);

            // === (3) LA ESPINA RUIDOSA — fBm con fases lentas y anclas ===
            // El mismo Fbm1D del PerlinBolt, con los parámetros de BEAM:
            // amplitud 3.5% del largo, meandro 1.4 ciclos, jitter ×0.25 a 6,
            // fases en tiempo CONTINUO (no por flick) — la tabla R2 §B3.
            float amp = largo * 0.035f;
            Vector2[] espina = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                float d = t - 0.5f;
                float env = MathF.Exp(-1200f * MathF.Pow(d * d, 4f)); // exp(-1200·d^8)
                float meandro = (StormLib.Fbm1D(t * 1.4f + tiempo * 0.35f, semilla, 3) - 0.5f) * 2f;
                float jitter = (StormLib.Fbm1D(t * 6f + tiempo * 1.9f, semilla + 77, 2) - 0.5f) * 2f;
                espina[i] = origen + dir * (largo * t) + perp * ((meandro + jitter * 0.25f) * amp * env);
            }
            espina = StormLib.Chaikin(espina, 1);      // el suavizado de la casa

            // El desplazamiento en t (para que los diamantes SIGAN la espina).
            float Desplaz(float t)
            {
                float d = t - 0.5f;
                float env = MathF.Exp(-1200f * MathF.Pow(d * d, 4f));
                float meandro = (StormLib.Fbm1D(t * 1.4f + tiempo * 0.35f, semilla, 3) - 0.5f) * 2f;
                float jitter = (StormLib.Fbm1D(t * 6f + tiempo * 1.9f, semilla + 77, 2) - 0.5f) * 2f;
                return (meandro + jitter * 0.25f) * amp * env;
            }

            // === (6) LOS ANCHOS — las envolventes del blueprint ===
            // Boca 0.38→1 (el primer 6%), punta libre 1→0.12 (el último
            // 32%), pared 1→0.35 (el último 12% si cortó en tile), y la
            // RESPIRACIÓN ±6% (Breathe de la casa).
            int m = espina.Length;
            float[] anchos = new float[m];
            float[] anchosHalo = new float[m];
            float[] anchosCore = new float[m];
            for (int i = 0; i < m; i++)
            {
                float t = i / (float)(m - 1);
                float boca = 0.38f + 0.62f * Suave01(t / 0.06f);
                float punta = MathHelper.Lerp(1f, 0.12f, Suave01((t - 0.68f) / 0.32f));
                float pared = choquePared ? MathHelper.Lerp(1f, 0.35f, Suave01((t - 0.88f) / 0.12f)) : 1f;
                float respira = VFXCore.Breathe(tiempo, 2.2f, semilla * 0.13f);
                float baseW = ancho * boca * punta * pared * respira;
                anchos[i] = baseW;
                anchosHalo[i] = baseW * 2.3f;
                anchosCore[i] = baseW * 0.42f;
            }

            float flujoV = (tiempo * flujo) % 1f;
            if (flujoV < 0f) flujoV += 1f;

            // === (4) LAS CAPAS ADITIVAS — halo → cuerpo → core ===
            if (conHalo)
                VFXCore.Ribbon(espina, anchosHalo, StormLib.Tint(color, 0.16f * intensidad),
                    VFXCore.SoftGlow, flujoV * 0.6f);

            // EL CUERPO: la textura de ruido de tinta propia — la carne que
            // AVANZA. Tinte con un pulso lento a lo largo (la receta
            // AngelicBeam: hue gradient + sin(t·5)).
            Texture2D cuerpo = RuidoTinta;
            float pulso = 0.55f + 0.12f * MathF.Sin(tiempo * 5f);
            VFXCore.Ribbon(espina, anchos, StormLib.Tint(color, pulso * intensidad),
                cuerpo, flujoV);

            // EL CORE: el pase blanco del Last Prism (escala /2 del vanilla,
            // aquí 0.42) — la línea caliente que hace leer el rayo como LUZ.
            VFXCore.Ribbon(espina, anchosCore,
                StormLib.Tint(new Color(255, 250, 240), 0.85f * intensidad),
                VFXCore.GlowOrb, flujoV * 1.6f);

            // === (5) LOS DIAMANTES VIAJEROS — sin(along/92 − t·9)^7 ===
            // Los PICOS viajan a 92·9 ≈ 828 px/s raíz→punta (los paquetes
            // de energía del AriaGammaRay de CWR). Matemática exacta de
            // picos: along = 92·(π/2 − faseBase + 2π·k), sin bucle denso.
            float faseBase = -tiempo * 9f;
            float paso = 92f * MathHelper.TwoPi;
            float kMin = (0f - 92f * (MathF.PI / 2f + faseBase)) / paso;
            float kMax = (largo - 92f * (MathF.PI / 2f + faseBase)) / paso;
            for (int k = (int)MathF.Ceiling(kMin); k <= (int)MathF.Floor(kMax); k++)
            {
                float along = 92f * (MathF.PI / 2f + faseBase + MathHelper.TwoPi * k);
                if (along < 8f || along > largo - 8f) continue;
                float t = along / largo;
                Vector2 pos = origen + dir * along + perp * Desplaz(t);
                float w = anchos[Math.Clamp((int)(t * (m - 1)), 0, m - 1)];
                VFXCore.Quad(pos, StormLib.Tint(Color.White, 0.5f * intensidad),
                    new Vector2(w * 0.95f, w * 0.95f), (float)dir.ToRotation(),
                    VFXCore.GlowOrb);
            }

            // === (7) LAS TAPAS — la boca y la punta ===
            // LA BOCA: el tapón brillante de donde nace todo.
            VFXCore.Quad(origen, StormLib.Tint(color, 0.55f * intensidad),
                new Vector2(ancho * 1.7f, ancho * 1.7f), VFXCore.GlowOrb);
            VFXCore.Quad(origen, StormLib.Tint(color, 0.30f * intensidad),
                new Vector2(ancho * 2.6f, ancho * 2.6f), (float)dir.ToRotation(),
                VFXCore.Ring);

            // LA PUNTA: fina… salvo que CHOQUE — entonces el estallido de
            // impacto (bola de calor + anillo pulsante a 12 Hz, la receta
            // CWR: exp(-dEnd²)·2.2 + splash sin(t·12)).
            Vector2 puntaPos = origen + dir * largo;
            if (choquePared)
            {
                float pulsoImp = 0.7f + 0.3f * MathF.Sin(tiempo * 12f);
                VFXCore.Quad(puntaPos, StormLib.Tint(Color.White, 0.55f * intensidad * pulsoImp),
                    new Vector2(ancho * 3.2f, ancho * 3.2f), VFXCore.NovaBurst);
                VFXCore.Quad(puntaPos, StormLib.Tint(color, 0.4f * intensidad),
                    new Vector2(ancho * (3f + 2f * pulsoImp), ancho * (3f + 2f * pulsoImp)),
                    (float)dir.ToRotation(), VFXCore.Ring);
            }
            else
            {
                VFXCore.Quad(puntaPos, StormLib.Tint(color, 0.4f * intensidad),
                    new Vector2(ancho * 0.9f, ancho * 0.9f), VFXCore.GlowOrb);
            }

            // === (2) LA LUZ — a lo largo de TODO el rayo ===
            // Cada 64 px (la MITAD del presupuesto vanilla de ~150 AddLight
            // por rayo — la regla amable de la casa), con el color del cuerpo.
            Vector3 luz = color.ToVector3();
            StormLib.AddLightAlong(espina, luz.X, luz.Y, luz.Z, 0.85f * intensidad, 64f);
        }

        // ==================================================================
        //  EL TELEGRAPH — la línea de aviso antes del disparo (la receta
        //  ArtemisLaser de Calamity: banda tenue que late mientras carga)
        // ==================================================================

        /// <summary>
        /// Encola la LÍNEA DE AVISO de un rayo que se avecina: banda fina
        /// aditiva que LATA (2 Hz) y CRECE con la fase de carga — el
        /// «telegraph» universal de los jefes modernos. Solo encola.
        /// </summary>
        /// <param name="faseCarga">0→1 — cuán cerca está el disparo.</param>
        public static void Telegrafo(Vector2 origen, Vector2 dir, float largo,
            Color color, float faseCarga, float tiempo, float anchoFinal = 22f)
        {
            if (dir.X == 0f && dir.Y == 0f) return;
            dir = dir.SafeNormalize(Vector2.UnitX);
            largo = Math.Min(largo, 4200f);
            if (largo < 16f) return;
            if (!VFXCore.Presupuesto(6)) return;

            // LA LATIDOS: 2 Hz creciendo a 4 Hz al final (la escalada de
            // tensión del blueprint R3 — el jugador SIENTE cuándo llega).
            float hz = MathHelper.Lerp(2f, 4f, MathHelper.Clamp(faseCarga, 0f, 1f));
            float latido = 0.5f + 0.5f * MathF.Sin(tiempo * hz * MathHelper.TwoPi);
            float alfa = (0.10f + 0.22f * faseCarga) * (0.55f + 0.45f * latido);

            // LA BANDA: línea fina que ENGORDA hacia el ancho final —
            // el rayo se ANUNCIA dibujándose antes de existir.
            Vector2 fin = origen + dir * largo;
            VFXCore.Line(origen, fin, StormLib.Tint(color, alfa), 3f + anchoFinal * 0.18f * faseCarga);

            // EL BORDE DE PLASMA: dos líneas pelos a los lados (la firma
            // del telegraph de Artemis: doble dibujado interior+exterior).
            Vector2 perp = new Vector2(-dir.Y, dir.X) * (4f + anchoFinal * 0.22f * faseCarga);
            VFXCore.Line(origen + perp, fin + perp, StormLib.Tint(color, alfa * 0.5f), 1.5f);
            VFXCore.Line(origen - perp, fin - perp, StormLib.Tint(color, alfa * 0.5f), 1.5f);

            // LA BOCA: el punto que SE ESTÁ CARGANDO (crece con la fase).
            VFXCore.Quad(origen, StormLib.Tint(color, 0.25f + 0.35f * faseCarga),
                new Vector2(10f + anchoFinal * 0.5f * faseCarga,
                            10f + anchoFinal * 0.5f * faseCarga),
                VFXCore.GlowOrb);

            // La luz tenue de la línea (una sola pasada, 96 px).
            Vector3 luz = color.ToVector3();
            StormLib.AddLightAlong(new[] { origen, fin }, luz.X, luz.Y, luz.Z,
                0.35f * (0.5f + 0.5f * faseCarga), 96f);
        }
    }
}
