using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    // ====================================================================
    //  v6.56.9 — IMPACTOLIB — EL MOMENTO DEL GOLPE UNIFICADO (la librería
    //  nueva nº3 de la auditoría §5.3: «UNA llamada que compone lo que hoy
    //  cada proyectil ensambla a mano con números mágicos: flash local +
    //  sacudida escalada + onda + golpe de luz + sonido»).
    //
    //  LA DEUDA QUE PAGA: 88 llamadas a OndaLib.Kick, ~18 a OndaLib.Flash
    //  y 244 AddLight a mano en 118 archivos, cada una con su constelación
    //  de números — el juice del golpe reinventado en cada arma. Aquí viven
    //  LAS RECETAS DE INTENSIDAD de la casa:
    //    · Golpe(pos, i)        — EL IMPACTO estándar (sacudida + flash + luz)
    //  · GolpeDeCine(pos, i)  — EL IMPACTO + hitstop de TiemposLib (el
    //      mundo contiene la respiración — solo un jugador)
    //  Y MAÑANA: la onda de choque, el sonido con prioridad, las chispas —
    //  todo detrás de esta fachada (respeta la Ley Primera §0: no se
    //  inventa nada, se COMPONE lo que ya vive en OndaLib/PantallaLib).
    //
    //  v6.80 — LA LEY DE LAS DOS RECETAS (la cura del bug nº2 del Grupo B
    //  de la auditoría v6.78: la doble receta de impacto): ya NO compiten
    //  — cada una tiene su oficio y su letra:
    //    · ImpactoLib.Golpe/GolpeDeCine — LA RECETA DEL DISPARO (el juice
    //      del golpe de un arma: sacudida + flash local + baño de luz +
    //      hitstop). El consumidor la llama en la IGNICIÓN o el IMPACTO.
    //    · Pantalla.PresetImpacto — LA RECETA CINEMATOGRÁFICA (el golpe
    //      de pantalla del acontecimiento: sacudida + cruz + viñeta +
    //      onda expansiva). El consumidor la llama en el momento ÉPICO.
    //  AMBAS salen por el MISMO motor de sacudida (PantallaLib, el dueño
    //  del pipeline — ver abajo) y los mismos gestos canónicos. Y la
    //  migración por tandas de los 82 Kick/224 AddLight a mano sigue
    //  PENDIENTE (tanda 1 de N: esta entrega converge las RECETAS; los
    //  llamadores históricos no se tocan — el alias delgado los respeta).
    //
    //  Consumidor de nacimiento: LA IGNICIÓN de las copias del RC2 (la
    /// letra: el momento del disparo del rayo concentrado).
    // ====================================================================
    public static class ImpactoLib
    {
        /// <summary>
        /// EL GOLPE DE LUZ — el estallido luminoso de un frame que baña la
        /// zona (la parte AddLight del facade: tres focos escalados por la
        /// intensidad, blanco-violeta de la casa).
        /// </summary>
        private static void LuzDelGolpe(Vector2 pos, float i, Color color)
        {
            if (Main.netMode == NetmodeID.Server) return;
            float k = MathHelper.Clamp(i, 0f, 2.5f);
            Lighting.AddLight(pos, color.R / 255f * 2.2f * k, color.G / 255f * 2.0f * k, color.B / 255f * 2.4f * k);
            Lighting.AddLight(pos + new Vector2(140f * k, 0f), color.R / 255f * 1.1f * k, color.G / 255f * 1.0f * k, color.B / 255f * 1.2f * k);
            Lighting.AddLight(pos - new Vector2(140f * k, 0f), color.R / 255f * 1.1f * k, color.G / 255f * 1.0f * k, color.B / 255f * 1.2f * k);
        }

        /// <summary>
        /// EL IMPACTO ESTÁNDAR — lo que cada proyectil ensamblaba a mano:
        /// kick de cámara escalado + flash local de la paleta + golpe de
        /// luz en tres focos. [intensidad] 1 = el estándar de la casa;
        /// 0.5 sutil; 2 contundente (tope interno 2.5).
        /// [suave] (v6.57) — LA VÍA DULCE: en vez del kick de impulso, la
        /// sacudida determinista de DOBLES SENOS de PantallaLib — unos
        /// pocos píxeles, movimiento suave (la letra del RC2: «un temblor
        /// de unos pocos pixeles y con movimientos suaves» al lanzar).
        /// [destello] (v6.58) — EL GOLPE A CIEGAS: false = NI flash local
        /// NI baño de luz — solo queda la sacudida (y el hitstop en
        /// GolpeDeCine). La letra del RC2: «justo en el primer segundo de
        /// lanzar el rayo hay un pequeño destello blanco… no debería
        /// estar ahí… es solo de iluminación» — hay momentos que se leen
        /// por el MOVIMIENTO, no por la luz.
        /// </summary>
        public static void Golpe(Vector2 pos, float intensidad, Color? color = null, bool suave = false,
            bool destello = true)
        {
            if (Main.netMode == NetmodeID.Server) return;
            float i = MathHelper.Clamp(intensidad, 0.1f, 2.5f);
            Color c = color ?? new Color(255, 248, 235);

            // LA SACUDIDA — v6.80: LA CONVERGENCIA DEL GRUPO B (bug nº2 de
            // la auditoría v6.78): PantallaLib es EL DUEÑO del pipeline de
            // sacudida y las recetas de la casa salen TODAS por su puerta
            // (SacudidaTrauma, un ICameraModifier REAL del motor — el kick
            // de OndaLib mutaba Main.screenPosition por su cuenta y PISABA
            // a los CameraModifiers sin saberlo). CALIBRACIÓN EXACTA del
            // feel viejo: el pico del kick era 7·i px; la escala de la casa
            // es pico = (min(N,10)/10)²·min(N,14) — el mapeo inverso
            // (raíz cúbica por debajo del trauma 1, saturado y capped a 14
            // por encima) devuelve el pico del kick VIEJO al px exacto:
            // 7 px con i=1, 10.15 con i=1.45, 14 (el cap histórico) con
            // i=2.5. La VENTANA también es la del kick ((8+6·i)/60 s); lo
            // que cambia es la CURVA: dobles senos inconmensurables
            // orgánicos en vez del seno seco de 13 Hz — la misma familia
            // visual que la vía dulce, UNA sola sacudida de casa.
            if (suave)
            {
                // LA VÍA DULCE (v6.57, intacta): ~2.6 px por intensidad,
                // medio segundo de decaído, DOBLES SENOS — determinista y
                // suave, nada de sacudidas secas.
                Pantalla.Sacudir(2.6f * i, 0.55f, "ImpactoLib-suave");
            }
            else
            {
                // LA VÍA SECA (v6.80, convergida): el pico y la ventana del
                // kick viejo, por el motor del dueño.
                float picoKickViejo = 7f * i;                       // px de pico a conservar
                float n = picoKickViejo >= 10f
                    ? Math.Min(picoKickViejo, 14f)                  // trauma saturado: pico = N (tope 14, el cap histórico)
                    : (float)Math.Cbrt(100f * picoKickViejo);       // pico = (N/10)²·N  →  N = ∛(100·pico)
                Pantalla.Sacudir(n, (8f + 6f * i) / 60f, "ImpactoLib");
            }

            // EL FLASH LOCAL + LA LUZ — solo si el golpe pide verse
            // (v6.58: destello: false = la ignición del haz, que se lee
            // por el temblor y el hitstop, no por la luz blanca).
            if (destello)
            {
                // EL FLASH LOCAL — el gradiente centrado en el golpe.
                OndaLib.Flash(c, 0.16f * i, 8, pos);

                // LA LUZ — el baño del frame.
                LuzDelGolpe(pos, i, c);
            }
        }

        /// <summary>
        /// EL GOLPE DE CINE — el impacto estándar + EL HITSTOP de
        /// TiemposLib: el mundo CONTIENE LA RESPIRACIÓN [ticksCongelar]
        /// ticks (solo un jugador; en MP el hitstop es no-op y el golpe
        /// queda igual). LA IGNICIÓN de las copias del RC2 nació con esto.
        /// [suave] (v6.57) — el temblor va por LA VÍA DULCE de PantallaLib
        /// (pocos px, dobles senos): el lanzamiento del RC2 lo usa.
        /// [destello] (v6.58) — pasa intacto a Golpe: la ignición del haz
        /// va A CIEGAS (sin flash blanco ni baño de luz — la letra: «ese
        /// pequeño destello blanco de luz no debería estar ahí»).
        /// </summary>
        public static void GolpeDeCine(Vector2 pos, float intensidad, int ticksCongelar = 6,
            Color? color = null, bool suave = false, bool destello = true)
        {
            Golpe(pos, intensidad, color, suave, destello);
            if (ticksCongelar > 0)
                TiemposLib.Congelar(ticksCongelar);
        }
    }
}
