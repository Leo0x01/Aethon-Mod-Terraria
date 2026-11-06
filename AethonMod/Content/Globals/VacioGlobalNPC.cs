using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Buffs;
using AethonMod.Content.Players;
using AethonMod.Content.Projectiles;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// VacioGlobalNPC — v6.101 → v6.102 — EL SISTEMA DE LA QUEMADURA DEL
    /// VACÍO EXTERIOR (la investigación del estandarte para el grimorio).
    ///
    /// LA LETRA DEL USUARIO: «cada enemigo que entre al radio de efecto
    /// del estandarte se quema y visualmente se nota como cada vez se
    /// quema mas y como varias particulas crean algo asi como una capa
    /// de resistencia igual a la de los meteoritos entrando a la
    /// atmosfera y todo con energia morada y negra».
    ///
    /// ESTE archivo es EL MOTOR COMPLETO del debuff, pensado para que el
    /// grimorio lo herede entero en el futuro (cualquier fuente que
    /// aplique <see cref="QuemaduraVacioExterior"/> obtiene TODO esto):
    ///
    ///  · EL ESTADO por enemigo: la INTENSIDAD (×1→×3 por cercanía, la
    ///    técnica Remap del original del estandarte) y el DUEÑO (quién
    ///    arde por quién — el portador cobra el buff Vacío Exterior).
    ///  · LA LENTITUD (PostAI): cuanto más se ACERCA al portador del
    ///    estandarte, más LENTO camina — la caída de velocidad es
    ///    SUAVE por cercanía (×1 en el borde → ×0.55 pegado al cuerpo).
    ///    En PostAI la IA ya escribió SU velocidad: el freno pisa
    ///    DESPUÉS y afecta a TODOS los enemigos, caminantes y voladores.
    ///  · v6.102 — EL VESTIDO DE LA RECETA (PostDraw): la letra nueva —
    ///    «el efecto sobre el enemigo quedo bien en el banner pero los
    ///    vfx no, crea otro banner con algo que me dio una IA […] adapta
    ///    el codigo y lo que dijo»: LA RECETA DEL CALENTAMIENTO
    ///    AERODINÁMICO, adaptada a la energía morada y negra:
    ///     · CAPA 2 — EL FRENTE DE COMPRESIÓN: la estela DENSA del aire
    ///       comprimido ENTRE el portador y el impío (FrenteVacio.png),
    ///       de VIOLETA (lejos) a BLANCO-VIOLETA (pegado, más caliente),
    ///       con el alfa ∝ intensidad — y la DOBLE PASADA de la casa
    ///       (×1.45/×2.1 de alto: el frente GORDO que pidió EL OJO).
    ///     · CAPA 4 — EL DESTELLO: el tinte + bloom sobre el impío
    ///       (el glow que BRILLA más en la cara que avanza) y su
    ///       RESPALDO negro-violeta (la corona del impío cayendo en la
    ///       atmósfera — la única pieza de la v6.101 que sobrevivió:
    ///       leía bien).
    ///    La capa de resistencia de la v6.101 (manto de 16 partículas,
    ///    cresta de ruido comprimida y estelas dibujadas) MUERE aquí:
    ///    la receta la sustituye (la capa 3 — el gas y la ablación —
    ///    vive en el estandarte, en la IA; la distorsión de calor, en
    ///    CalorLib).
    ///  · TODO determinista en el render (semilla = whoAmI — 0 Main.rand
    ///    en el dibujado, la ley de la casa).
    /// </summary>
    public class VacioGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        /// <summary>LA INTENSIDAD de la quemadura (×1 lejos → ×3 pegado,
        /// más la rampa de tiempo ×1→×1.5). La escribe la fuente (el
        /// estandarte; mañana, el grimorio). 0 = no arde.</summary>
        public float IntensidadQuemadura;

        /// <summary>EL DAÑO POR SEGUNDO base de la quemadura a intensidad
        /// ×1 (escala con el nivel de despertar del dueño).</summary>
        public int DanoVacio;

        /// <summary>EL DUEÑO de la quemadura (player.whoAmI + 1; 0 = no
        /// tiene dueño). Mientras un enemigo arde por él, el portador
        /// cobra el buff Vacío Exterior.</summary>
        public int DuenoVacio;

        /// <summary>LA RAMPA VISUAL local (0→1 en ~2 s ardiendo): cada
        /// vez se quema MÁS — el frente y el destello crecen con ella.
        /// Se enfría sola cuando el debuff se apaga.</summary>
        public float VisualQuemadura;

        /// <summary>EL FRENTE de ablación (unitario): la cara que avanza
        /// hacia el portador — el lado que CHOCA con el vacío.</summary>
        public Vector2 FrenteLocal = -Vector2.UnitY;

        /// <summary>LA CERCANÍA local al portador (0 en el borde del
        /// radio → 1 pegado al cuerpo) — la que frena.</summary>
        public float CercaniaLocal;

        // El caché del ruido procedural (las 20 láminas de la casa).
        private static Texture2D[] _ruidoCache;

        // ================================================================
        //  LA LENTITUD + LA RAMPA VISUAL — cada tick de vida del NPC.
        // ================================================================
        public override void PostAI(NPC npc)
        {
            if (Main.gameMenu || !npc.active)
                return;

            bool arde = TieneQuemadura(npc);
            if (arde)
            {
                // LA RAMPA: cada vez se quema más (0→1 en 120 t).
                VisualQuemadura = Math.Min(1f, VisualQuemadura + 1f / 120f);

                // EL FRENTE: la cara que avanza — su velocidad si corre,
                // el camino hacia el portador si se planta.
                float vel = npc.velocity.Length();
                if (vel > 0.6f)
                    FrenteLocal = Vector2.Normalize(npc.velocity);
                else
                {
                    Vector2 hacia = PortadorMasCercano(npc.Center);
                    if (hacia != Vector2.Zero)
                        FrenteLocal = Vector2.Normalize(hacia);
                }

                // LA CERCANÍA al portador MÁS CERCANO con estandarte (la
                // misma técnica Remap del original: radio→pegado = 0→1).
                CercaniaLocal = CalcularCercania(npc);

                // LA LENTITUD DE VERDAD: en PostAI la IA ya escribió SU
                // velocidad — el freno del vacío pisa DESPUÉS y vale para
                // TODOS (los que caminan a velocidad fija incluidos).
                if (CercaniaLocal > 0f && Main.netMode != NetmodeID.MultiplayerClient)
                    npc.velocity *= 1f - 0.45f * CercaniaLocal;
            }
            else
            {
                // EL ENFRIAMIENTO: fuera del fuego el frente se apaga solo.
                if (VisualQuemadura > 0f)
                    VisualQuemadura = Math.Max(0f, VisualQuemadura - 0.05f);
                if (VisualQuemadura <= 0f)
                {
                    CercaniaLocal = 0f;
                    IntensidadQuemadura = 0f;
                }
            }
        }

        /// <summary>¿El NPC arde con la Quemadura del Vacío Exterior?</summary>
        public static bool TieneQuemadura(NPC npc) =>
            npc != null && npc.active && npc.HasBuff(ModContent.BuffType<QuemaduraVacioExterior>());

        /// <summary>EL VECTOR hacia el portador de estandarte más cercano
        /// (Vector2.Zero si no hay nadie vivo portándolo).</summary>
        private static Vector2 PortadorMasCercano(Vector2 desde)
        {
            Vector2 mejor = Vector2.Zero;
            float min = float.MaxValue;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p == null || !p.active || p.dead)
                    continue;
                if (!p.GetModPlayer<JuicioPlayer>().EstandarteJuicio)
                    continue;
                float d = p.Center.DistanceSQ(desde);
                if (d < min)
                {
                    min = d;
                    mejor = p.Center - desde;
                }
            }
            return mejor;
        }

        /// <summary>LA CERCANÍA (0..1): la distancia al rectángulo del NPC
        /// desde el portador más cercano, remapeada del RADIO del
        /// estandarte (300 px — el borde del vacío) a 40 px (pegado).</summary>
        private static float CalcularCercania(NPC npc)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p == null || !p.active || p.dead)
                    continue;
                if (!p.GetModPlayer<JuicioPlayer>().EstandarteJuicio)
                    continue;
                Vector2 borde = Vector2.Clamp(p.Center, npc.TopLeft, npc.BottomRight);
                float d = Vector2.Distance(p.Center, borde);
                if (d < EstandarteJuicioAuraProjectile.RADIO)
                    return Remap(d, EstandarteJuicioAuraProjectile.RADIO, 40f, 0f, 1f);
            }
            return 0f;
        }

        /// <summary>El Remap acotado del original del estandarte.</summary>
        private static float Remap(float v, float in1, float in2, float out1, float out2)
        {
            float k = (v - in1) / (in2 - in1);
            if (k < 0f) k = 0f;
            else if (k > 1f) k = 1f;
            return out1 + (out2 - out1) * k;
        }

        // ================================================================
        //  EL VESTIDO DE LA RECETA — el frente de compresión + el
        //  destello (el contrato del dibujado de la casa: cerrar el
        //  lote, vestir, devolver el lote vanilla SIEMPRE).
        // ================================================================
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (VisualQuemadura <= 0.02f || Main.gameMenu || Main.netMode == NetmodeID.Server)
                return;
            try
            {
                VFXCore.CerrarLoteSiAbierto();
                DibujarElFrenteYElDestello(npc, screenPos);
            }
            catch
            {
                try { VFXCore.CerrarLoteSiAbierto(); } catch { }
            }
            VFXCore.ReabrirLoteVanilla();
        }

        /// <summary>v6.102 — LA CAPA 2 Y LA CAPA 4 DE LA RECETA:
        /// EL FRENTE DE COMPRESIÓN (la estela del aire comprimido entre
        /// el portador y el impío — denso en el origen, disperso hacia
        /// la presa, más caliente cuanto más cerca) y EL DESTELLO (el
        /// tinte + bloom sobre el impío, brillando más en la cara que
        /// avanza), con su RESPALDO negro-violeta detrás.</summary>
        private void DibujarElFrenteYElDestello(NPC npc, Vector2 screenPos)
        {
            float t = Main.GlobalTimeWrappedHourly;
            int semilla = npc.whoAmI * 7 + 3;
            float vive = VisualQuemadura;
            float late = 0.85f + 0.15f * MathF.Sin(t * 9f + Azar(semilla) * 6.28f);

            Vector2 centro = npc.Center - screenPos;
            float rx = npc.width * 0.5f + 8f;
            float ry = npc.height * 0.5f + 8f;

            Color violeta = new(185, 110, 255);
            Color violetaAlto = new(224, 176, 255);
            Color negroVioleta = new(46, 22, 74);

            SpriteBatch sb = Main.spriteBatch;
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D glow = Tex("GlowCircleWhite");   // EL GLOWSOFT de la receta
            Texture2D frente = Tex("FrenteVacio");

            // === CAPA 2 — EL FRENTE DE COMPRESIÓN: el «cono de aire
            //     caliente» ENTRE el portador y el impío. LA RECETA:
            //     Color.Lerp(frío → caliente, intensidad) con el alfa
            //     ∝ intensidad (150·intensidad), la escala X = la
            //     distancia y la Y crece con la cercanía (×1.8 el
            //     pincel) — y LA DOBLE PASADA de la casa (la segunda
            //     ×1.45 de alto al 45%: el frente GORDO que pidió EL
            //     OJO). El origen en el BORDE IZQUIERDO (la convención
            //     de la receta: la estela nace en el portador) y el
            //     espejo por impío (paridad de la semilla: unos lean
            //     arriba, otros abajo — la turbulencia no repite). ===
            if (frente != null)
            {
                Vector2 haciaPortador = PortadorMasCercano(npc.Center);
                if (haciaPortador != Vector2.Zero)
                {
                    Vector2 posPortador = npc.Center + haciaPortador;   // el mundo
                    float dist = haciaPortador.Length();
                    if (dist > 30f)
                    {
                        Vector2 pantalla = posPortador - screenPos;
                        float ang = (centro - pantalla).ToRotation();   // portador → impío
                        float k = CercaniaLocal;
                        Color colFrente = Color.Lerp(violeta, violetaAlto, k);
                        float alfaFrente = (0.15f + 0.50f * k) * vive * late;
                        var origen = new Vector2(0f, frente.Height * 0.5f);
                        var espejo = (semilla & 1) == 0
                            ? SpriteEffects.None : SpriteEffects.FlipVertically;

                        // la pasada PRINCIPAL (el pincel de la receta ×1.8)
                        sb.Draw(frente, pantalla, null, colFrente * alfaFrente,
                            ang, origen,
                            new Vector2(dist / frente.Width, (0.6f + 0.4f * k) * 1.8f),
                            espejo, 0f);
                        // la DOBLE PASADA (más ALTA y más TENUE — el grosor)
                        sb.Draw(frente, pantalla, null, colFrente * (alfaFrente * 0.45f),
                            ang, origen,
                            new Vector2(dist * 0.92f / frente.Width, (0.6f + 0.4f * k) * 2.6f),
                            espejo, 0f);
                    }
                }
            }

            // === CAPA 4 — EL DESTELLO: el tinte + bloom sobre el impío,
            //     BRILLANDO MÁS en la cara que avanza (la que CHOCA con
            //     el vacío: el «tinte rojizo + bloom» de la receta, en
            //     la energía del estandarte) y su núcleo blanco-violeta. ===
            {
                float k = CercaniaLocal;
                float esc = (MathF.Max(rx, ry) * (1.5f + 0.9f * k)) / 128f;  // la receta: 1.5·intensidad
                Capa(sb, glow, centro, violeta * ((0.22f + 0.5f * k) * vive * late),
                    0f, esc);
                // EL NÚCLEO — el punto más caliente del choque
                Texture2D orbe = Tex("GlowOrb");
                if (orbe != null)
                    Capa(sb, orbe, centro + FrenteLocal * (MathF.Max(rx, ry) * 0.35f),
                        violetaAlto * ((0.30f + 0.35f * k) * vive * late),
                        FrenteLocal.ToRotation(), 0.17f + 0.12f * k);
            }

            // === EL RESPALDO — la única pieza de la v6.101 que SOBREVIVE
            //     (leía bien): el negro-violeta detrás de todo el cuerpo,
            //     la corona del impío cayendo en la atmósfera. ===
            Capa(sb, glow, centro, negroVioleta * (0.34f * vive),
                0f, (MathF.Max(rx, ry) * 2.6f) / 128f);

            sb.End();
        }

        // ----------------------------------------------------------------
        //  LOS HELPERS DE LA CASA
        // ----------------------------------------------------------------

        /// <summary>El azar determinista de la casa.</summary>
        private static float Azar(int i)
        {
            float v = MathF.Sin(i * 12.9898f + 78.233f) * 43758.547f;
            return v - MathF.Floor(v);
        }

        /// <summary>La lámina de ruido viva (el caché de las 20).</summary>
        private static Texture2D Ruido(int semilla, float vel)
        {
            _ruidoCache ??= new Texture2D[20];
            int i = (int)(Main.GlobalTimeWrappedHourly * vel + semilla * 0.37f) % 20;
            if (i < 0) i += 20;
            return _ruidoCache[i] ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/Ruido_" + i,
                AssetRequestMode.ImmediateLoad).Value;
        }

        private static Texture2D Tex(string ruta) =>
            ModContent.Request<Texture2D>("AethonMod/Content/Effects/" + ruta,
                AssetRequestMode.ImmediateLoad).Value;

        private static void Capa(SpriteBatch sb, Texture2D tex, Vector2 pos,
            Color color, float rot, float esc)
        {
            if (color.A != 0 && esc > 0f)
                sb.Draw(tex, pos, null, color, rot, Utils.Size(tex) * 0.5f, esc,
                    SpriteEffects.None, 0f);
        }

        private static void Capa(SpriteBatch sb, Texture2D tex, Vector2 pos,
            Color color, float rot, Vector2 esc)
        {
            if (color.A != 0 && (esc.X > 0f || esc.Y > 0f))
                sb.Draw(tex, pos, null, color, rot, Utils.Size(tex) * 0.5f, esc,
                    SpriteEffects.None, 0f);
        }
    }
}
