using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Players;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// OcasoSystem — v6.27 — EL ARO MEDIDOR DEL OCASO.
    ///
    /// La UI del gauge del Bastón del Ocaso de Aethon, 100% por código
    /// (la lección de UI del Cosmic Destroyer: indicadores discretos +
    /// fade-out de visibilidad −0.05/tick — nada de sprites fijos):
    ///
    ///   · CARGA: un arco de 20 segmentos tipo AUREOLA sobre la cabeza,
    ///     violeta → dorado conforme se llena (el color es el progreso).
    ///   · LISTO: el arco entero PULSA dorado con 3 puntas orbitando y el
    ///     rótulo "EL OCASO" — el aviso de que el clic derecho ya manda.
    ///   · ACTIVO: el arco DRENA violeta→rojo con chispas parpadeando
    ///     (StormLib.IsLit) — los 8 s de la lluvia de muertes de estrellas.
    ///   · SOBRECALENTADO: el arco rojo se APAGA segmento a segmento al
    ///     ritmo del lockout, con parpadeo de castigo a 8 Hz.
    ///
    /// Se dibuja en PostDrawInterface CON EL LOTE DE LA INTERFAZ TAL CUAL
    /// (el patrón aprobado de OndaSystem: cero manipulación de estado en
    /// espacio de UI — la clase de bug que v6.27 acaba de enterrar).
    /// </summary>
    public class OcasoSystem : ModSystem
    {
        private const int Segmentos = 20;

        // El arco: una aureola abierta por debajo (de −155° a −25°).
        private const float Ang0 = -MathHelper.Pi * 0.861f;
        private const float Ang1 = -MathHelper.Pi * 0.139f;

        private static Asset<Texture2D> _glow;

        private static Texture2D GlowTex =>
            (_glow ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow")).Value;

        public override void Unload()
        {
            _glow = null;
            OcasoBurstFX.Unload();
        }

        public override void PreUpdateEntities()
        {
            if (Main.netMode == NetmodeID.Server) return;
            OcasoBurstFX.Tick();   // el envejecimiento por TICK de las heridas
        }

        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // === PRIMERO LAS HERIDAS de las muertes de estrella (el
            //     desgarro del apagón vive sobre TODO, en coords de mundo) ===
            OcasoBurstFX.Dibujar(spriteBatch);

            Player p = Main.LocalPlayer;
            if (p == null || !p.active) return;

            var op = p.GetModPlayer<OcasoPlayer>();

            // === VISIBILIDAD: siempre con el arma en la mano; con fade si
            //     se disparó hace poco (el fade vive en OcasoPlayer) ===
            bool sosteniendo = p.HeldItem != null && p.HeldItem.type ==
                ModContent.ItemType<global::AethonMod.Content.Weapons.Cosmic.OcasoAethonStaff>();
            float vis = sosteniendo ? 1f : MathHelper.Clamp(op.UiFade, 0f, 1f);
            if (vis <= 0.02f) return;

            float time = Main.GlobalTimeWrappedHourly;
            Vector2 centro = p.Center - Main.screenPosition + new Vector2(0f, -46f);
            float radio = 34f;

            // === EL HALO DE FONDO (el aura del medidor) ===
            Color fondo = op.OcasoActivo
                ? new Color(255, 120, 60)
                : op.Sobrecalentado ? new Color(255, 60, 40)
                : new Color(150, 90, 255);
            Quad(spriteBatch, centro, fondo, radio * 2.6f, 0.06f * vis);

            // === EL ESTADO DEL ARO ===
            if (op.OcasoActivo) DibujarOcaso(spriteBatch, op, centro, radio, time, vis);
            else if (op.Sobrecalentado) DibujarLockout(spriteBatch, op, centro, radio, time, vis);
            else DibujarCarga(spriteBatch, op, centro, radio, time, vis);
        }

        // ==================================================================
        //  ESTADO 1 · LA CARGA (violeta → dorado)
        // ==================================================================

        private static void DibujarCarga(SpriteBatch sb, OcasoPlayer op,
            Vector2 centro, float radio, float time, float vis)
        {
            bool listo = op.Gauge >= OcasoPlayer.GaugeMax;

            // EL PULSO DE LISTO: 6 Hz suaves — el arma YA puede.
            float pulso = listo ? 0.65f + 0.35f * MathF.Sin(time * MathHelper.TwoPi * 3f) : 1f;

            for (int k = 0; k < Segmentos; k++)
            {
                float t = k / (float)(Segmentos - 1);
                float ang = MathHelper.Lerp(Ang0, Ang1, t);
                bool lleno = op.Gauge >= (k + 1) * (OcasoPlayer.GaugeMax / Segmentos);

                // El COLOR es el progreso: violeta profundo → oro del ocaso.
                Color c = Color.Lerp(
                    new Color(140, 70, 255), new Color(255, 214, 110), t);

                float a = lleno ? 0.75f * pulso : 0.16f;
                Segmento(sb, centro, ang, radio, c, a * vis, time, k);
            }

            if (!listo) return;

            // === LAS TRES PUNTAS ORBITANDO (el aviso vivo del TSA) ===
            for (int k = 0; k < 3; k++)
            {
                float ang = time * 1.9f * ((k & 1) == 0 ? 1f : -1f)
                            + k * MathHelper.TwoPi / 3f;
                Vector2 pp = centro + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (radio + 9f);
                Quad(sb, pp, new Color(255, 236, 170), 9f, 0.55f * vis * pulso);
                Quad(sb, pp, new Color(255, 180, 80), 4.5f, 0.75f * vis);
            }

            // === EL RÓTULO (el clic derecho ya manda) ===
            // v6.49 — EL RÓTULO LOCALIZADO Y MEDIDO UNA VEZ (hallazgo
            // AUD-C: "EL OCASO" estaba hardcodeado FUERA del hjson y se
            // medía con MeasureString cada frame).
            var fuente = Terraria.GameContent.FontAssets.ItemStack.Value;
            if (_rotulo == null)
            {
                _rotulo = Terraria.Localization.Language.GetTextValue("Mods.AethonMod.Ocaso.Rotulo");
                _rotuloMedida = fuente.MeasureString(_rotulo);
            }
            sb.DrawString(fuente, _rotulo,
                centro + new Vector2(0f, -radio - 20f) - _rotuloMedida * (0.7f * 0.5f),
                new Color(255, 220, 140) * (0.85f * vis * pulso), 0f, Vector2.Zero,
                0.7f, SpriteEffects.None, 0f);
        }

        // v6.49 — el cache del rótulo (texto localizado + medida).
        private static string _rotulo;
        private static Vector2 _rotuloMedida;

        // ==================================================================
        //  ESTADO 2 · EL OCASO ACTIVO (el drenaje de la lluvia)
        // ==================================================================

        private static void DibujarOcaso(SpriteBatch sb, OcasoPlayer op,
            Vector2 centro, float radio, float time, float vis)
        {
            // La FRACCIÓN restante del modo (1 → 0 durante los 8 s).
            float resto = 1f - op.OcasoT;

            for (int k = 0; k < Segmentos; k++)
            {
                float t = k / (float)(Segmentos - 1);
                float ang = MathHelper.Lerp(Ang0, Ang1, t);
                bool lleno = resto >= (k + 1) * (1f / Segmentos);

                // El drenaje: ORO al inicio → rojo del recalentamiento al final.
                Color c = Color.Lerp(
                    new Color(255, 190, 90), new Color(255, 70, 40), op.OcasoT);

                float a = lleno ? 0.85f : 0.10f;
                // LAS CHISPAS del arco: el parpadeo de la casa.
                int flick = StormLib.FlickTick(time, 11f);
                if (lleno && StormLib.IsLit(k * 37 + 5, flick, 0.25f)) a = 1f;
                Segmento(sb, centro, ang, radio, c, a * vis, time, k);
            }

            // EL CORAZÓN del ocaso: el mini-eclipse latiendo en el centro.
            float latido = 0.8f + 0.2f * MathF.Sin(time * MathHelper.TwoPi * 1.4f);
            Quad(sb, centro, new Color(255, 160, 60), 30f * latido, 0.22f * vis);
            Quad(sb, centro, new Color(255, 240, 210), 12f * latido, 0.5f * vis);
        }

        // ==================================================================
        //  ESTADO 3 · EL LOCKOUT (la sobrecalentada apagándose)
        // ==================================================================

        private static void DibujarLockout(SpriteBatch sb, OcasoPlayer op,
            Vector2 centro, float radio, float time, float vis)
        {
            // La fracción RESTANTE del castigo (se APAGA al recuperarse).
            float resto = op.OverheatTime / (float)OcasoPlayer.OverheatTicks;
            // EL PARPADEO DE CASTIGO: 8 Hz alternando dureza.
            float castigo = MathF.Sin(time * MathHelper.TwoPi * 8f) > 0f ? 1f : 0.45f;

            for (int k = 0; k < Segmentos; k++)
            {
                float t = k / (float)(Segmentos - 1);
                float ang = MathHelper.Lerp(Ang0, Ang1, t);
                bool vivo = resto >= (k + 1) * (1f / Segmentos);
                float a = vivo ? 0.55f * castigo : 0.06f;
                Segmento(sb, centro, ang, radio, new Color(255, 60, 40), a * vis, time, k);
            }

            // EL VAPOR del recalentamiento: dos volutas tenues subiendo.
            for (int k = 0; k < 2; k++)
            {
                float fase = time * 0.7f + k * 0.5f;
                Vector2 pp = centro + new Vector2(
                    MathF.Sin(fase * 3.1f) * 6f, -radio - 12f - (fase % 1f) * 16f);
                Quad(sb, pp, new Color(200, 90, 70), 14f * (1f - fase % 1f), 0.14f * vis);
            }
        }

        // ==================================================================
        //  LOS PINCELES (con el lote de la interfaz TAL CUAL)
        // ==================================================================

        /// <summary>Un segmento del arco: glifo alargado TANGENTE al aro
        /// (la muesca rúnica de la casa) + núcleo brillante.</summary>
        private static void Segmento(SpriteBatch sb, Vector2 centro, float ang,
            float radio, Color c, float alpha, float time, int k)
        {
            if (alpha <= 0.01f) return;
            Vector2 pos = centro + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio;
            float largo = radio * (Ang1 - Ang0) / Segmentos * 1.55f;
            float ancho = 4.2f;

            // El glifo: el quad alargado ROTADO a la tangente del arco.
            Texture2D tex = GlowTex;
            sb.Draw(tex, pos, null, c * alpha, ang + MathHelper.PiOver2,
                tex.Size() * 0.5f,
                new Vector2(largo / tex.Width, ancho / tex.Height) * 2f,
                SpriteEffects.None, 0f);

            // El NÚCLEO del segmento (más pequeño, más brillante).
            sb.Draw(tex, pos, null, Color.Lerp(c, Color.White, 0.45f) * alpha * 0.8f,
                ang + MathHelper.PiOver2, tex.Size() * 0.5f,
                new Vector2(largo * 0.45f / tex.Width, 2.6f / tex.Height) * 2f,
                SpriteEffects.None, 0f);
        }

        /// <summary>Un quad de luz suave centrado (el pincel SoftGlow).</summary>
        private static void Quad(SpriteBatch sb, Vector2 pos, Color c, float tam, float alpha)
        {
            if (alpha <= 0.01f) return;
            Texture2D tex = GlowTex;
            sb.Draw(tex, pos, null, c * alpha, 0f, tex.Size() * 0.5f,
                new Vector2(tam / tex.Width, tam / tex.Height) * 2f,
                SpriteEffects.None, 0f);
        }
    }
}
