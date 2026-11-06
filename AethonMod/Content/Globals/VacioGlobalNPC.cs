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
    /// VacioGlobalNPC — v6.101 — EL SISTEMA DE LA QUEMADURA DEL VACÍO
    /// EXTERIOR (la investigación del estandarte para el grimorio).
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
    ///  · LA CAPA DE RESISTENCIA (PostDraw): el frente de ablación de
    ///    los meteoritos entrando a la atmósfera — un MANTO de partículas
    ///    alrededor del cuerpo MÁS BRILLANTE en la cara que avanza, la
    ///    CRESTA DE CHOQUE delante (dos láminas de ruido comprimidas) y
    ///    LAS ESTELAS que se desprenden hacia atrás como chatarra
    ///    ardiente — TODO con energía MORADA y NEGRA, y cada vez MÁS
    ///    INTENSO cuanto más tiempo lleva ardiendo (la rampa visual
    ///    sube en 2 segundos de quemado).
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
        /// tiene dueño). Mientras un enemigo arda por él, el portador
        /// cobra el buff Vacío Exterior.</summary>
        public int DuenoVacio;

        /// <summary>LA RAMPA VISUAL local (0→1 en ~2 s ardiendo): cada
        /// vez se quema MÁS — el manto, la cresta y las estelas crecen
        /// con ella. Se enfría sola cuando el debuff se apaga.</summary>
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
                // EL ENFRIAMIENTO: fuera del fuego el manto se apaga solo.
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
        //  LA CAPA DE RESISTENCIA — el frente de ablación del meteoro.
        //  (El contrato del dibujado de la casa: cerrar el lote, vestir,
        //  devolver el lote vanilla SIEMPRE.)
        // ================================================================
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (VisualQuemadura <= 0.02f || Main.gameMenu || Main.netMode == NetmodeID.Server)
                return;
            try
            {
                VFXCore.CerrarLoteSiAbierto();
                DibujarCapaDeAblacion(npc, screenPos);
            }
            catch
            {
                try { VFXCore.CerrarLoteSiAbierto(); } catch { }
            }
            VFXCore.ReabrirLoteVanilla();
        }

        /// <summary>LA CAPA DE RESISTENCIA COMPLETA: el manto de
        /// partículas alrededor del cuerpo (más brillante en la cara que
        /// avanza), la CRESTA DE CHOQUE comprimida delante y LAS ESTELAS
        /// de ablación que se desprenden hacia atrás — morado y negro,
        /// creciendo con la rampa visual.</summary>
        private void DibujarCapaDeAblacion(NPC npc, Vector2 screenPos)
        {
            float t = Main.GlobalTimeWrappedHourly;
            int semilla = npc.whoAmI * 7 + 3;
            float vive = VisualQuemadura;
            float late = 0.85f + 0.15f * MathF.Sin(t * 9f + semille(semilla) * 6.28f);

            Vector2 centro = npc.Center - screenPos;
            float rx = npc.width * 0.5f + 8f;
            float ry = npc.height * 0.5f + 8f;
            float angFrente = FrenteLocal.ToRotation();

            Color violeta = new(185, 110, 255);
            Color violetaAlto = new(224, 176, 255);
            Color negroVioleta = new(46, 22, 74);

            SpriteBatch sb = Main.spriteBatch;
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D destello = Tex("SparkleStar");
            Texture2D orbe = Tex("GlowOrb");

            // 1) EL MANTO DE RESISTENCIA — 16 partículas en elipse alrededor
            //    del cuerpo, girando despacio, BRILLANDO MÁS en la cara que
            //    avanza (cos(ángulo−frente)^2: el frente del meteoro).
            for (int k = 0; k < 16; k++)
            {
                float ang = k * MathHelper.TwoPi / 16f + t * 0.45f;
                Vector2 pos = centro + new Vector2(MathF.Cos(ang) * rx, MathF.Sin(ang) * ry);
                float choque = MathF.Max(0f, MathF.Cos(ang - angFrente));
                float brillo = (0.22f + 0.78f * choque * choque) * vive * late;
                float esc = (3.2f + 2.6f * choque + 1.8f * Azar(semilla + k)) / 32f;
                Capa(sb, destello, pos, Color.Lerp(violeta, violetaAlto, choque) * brillo,
                    ang + t * 2f, esc);
            }

            // 2) LA CRESTA DE CHOQUE — las dos láminas de ruido COMPRIMIDAS
            //    en la cara que avanza (el plasma del meteoro entrando).
            Vector2 posCresta = centro + FrenteLocal * (MathF.Max(rx, ry) * 0.72f + 7f);
            Texture2D ruido = Ruido(semilla, 6.2f);
            float ancho = MathF.Max(rx, ry) * 2.1f * (0.9f + 0.2f * vive);
            Capa(sb, ruido, posCresta, violeta * (0.30f * vive * late),
                angFrente, new Vector2(ancho / 512f, (MathF.Max(rx, ry) * 1.05f) / 512f));
            Capa(sb, ruido, posCresta + FrenteLocal * 5f,
                Color.White * (0.10f * vive),
                angFrente, new Vector2(ancho * 0.7f / 512f, (MathF.Max(rx, ry) * 0.55f) / 512f));
            // EL NÚCLEO DE LA CRESTA: el punto más caliente del choque.
            Capa(sb, orbe, posCresta, violetaAlto * (0.4f * vive * late),
                angFrente, 0.16f);

            // 3) LAS ESTELAS DE ABLACIÓN — la chatarra que se desprende
            //    hacia ATRÁS del avance: 8 colas que nacen en el hemisferio
            //    delantero y se estiran contracorriente, finas y largas.
            for (int k = 0; k < 8; k++)
            {
                float azar = Azar(semilla + k * 13);
                float ang = angFrente + (azar * 2f - 1f) * 1.9f;
                Vector2 nace = centro + new Vector2(MathF.Cos(ang) * rx, MathF.Sin(ang) * ry);
                float largo = (16f + 36f * azar) * vive;
                Vector2 cola = nace - FrenteLocal * largo;
                Vector2 medio = (nace + cola) * 0.5f;
                // LA ESTELA VIOLETA (el rastro caliente)…
                sb.Draw(orbe, medio, null, violeta * (0.5f * vive * (1f - azar * 0.4f)),
                    (cola - nace).ToRotation(), Utils.Size(orbe) * 0.5f,
                    new Vector2(largo / 128f, 3.6f / 128f), SpriteEffects.None, 0f);
                // …Y SU SOMBRA NEGRA (el humo del vacío que deja).
                sb.Draw(orbe, medio + FrenteLocal * largo * 0.25f, null,
                    Color.Black * (0.35f * vive),
                    (cola - nace).ToRotation(), Utils.Size(orbe) * 0.5f,
                    new Vector2(largo * 1.25f / 128f, 6.5f / 128f), SpriteEffects.None, 0f);
            }

            // 4) EL HALO DEL VACÍO — el resplandor morado-oscuro detrás de
            //    todo el cuerpo (la corona del enemigo cayendo en la atmósfera).
            Capa(sb, Tex("GlowCircleWhite"), centro, negroVioleta * (0.34f * vive),
                0f, (MathF.Max(rx, ry) * 2.6f) / 128f);
            Capa(sb, orbe, centro, violeta * (0.12f * vive * late),
                0f, (MathF.Max(rx, ry) * 1.9f) / 128f);

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

        private static float semille(int s) => Azar(s);

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
