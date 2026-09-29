using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using AethonMod.Content.NPCs;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Effects
{
    // ======================================================================
    //  v6.50.36 — EL CIELO SE ENCIENDE (la llegada de LA LUZ PRIMORDIAL).
    //
    //  Petición del usuario: «mejor hacerlo una luz brillante, el jefe es
    //  una potente luz que ataca al jugador». La sierpe MURIÓ — su cola
    //  enredada en el paisaje (v6.50.19-35) muere con ella. Cuando
    //  AETHON, LA LUZ PRIMORDIAL desciende, EL CIELO ENTERO SE ENCIENDE:
    //
    //  · LA LLEGADA (mientras el núcleo se materializa): un RESPLANDOR
    //    DORADO creciente que inunda el horizonte + SIETE COLUMNAS DE
    //    LUZ lejanas alzándose del borde del mundo (el eco del Juicio
    //    que viene) + LA VENTANA: el orbe del núcleo creciendo en el
    //    centro del cielo — la puerta por la que entra la luz.
    //  · MIENTRAS VIVE: el resplandor dorado PERMANECE en lo alto (la
    //    atmósfera del sol viviente — y en el ECLIPSE (ai[1]==3) se
    //    APAGA a un violeta moribundo: el cielo también muere un rato).
    //
    //  El mecanismo es EL MISMO DE SIEMPRE (el hallazgo R59-a que la
    //  casa hereda): SkyManager.DrawToDepth pinta ENTRE LAS CAPAS del
    //  paisaje — el resplandor queda DETRÁS de las montañas cercanas y
    //    DELANTE de las nubes lejanas (la luz baña el horizonte).
    // ======================================================================
    public class ColaSierpeSky : CustomSky
    {
        /// <summary>Profundidad falsa de la luz (la banda de montañas).</summary>
        private const float Profundidad = 6.5f;

        /// <summary>El resplandor aparece al presentar la luz (fade 2 s).</summary>
        private float _alpha = 0f;
        private bool _activo = false;
        private bool _pintadoEsteFrame = false;

        /// <summary>Bandera de descarga (la casa: el cielo muerto jamás toca ModContent).</summary>
        public static bool Descargado = false;

        // ==================================================================
        //  EL CICLO DE VIDA (GameEffect 2026: Activate/Deactivate con params)
        // ==================================================================
        public override void Activate(Vector2 position, params object[] args) => _activo = true;

        public override void Deactivate(params object[] args) => _activo = false;

        public override void Reset()
        {
            _activo = false;
            _alpha = 0f;
        }

        public override bool IsActive() => _activo || _alpha > 0.001f;

        public override void Update(GameTime gameTime)
        {
            if (Descargado) { _alpha = 0f; return; }
            // EL FADE DE PRESENTACIÓN: 2 s de entrante, 1.5 s de salida.
            float objetivo = _activo ? 1f : 0f;
            float paso = _activo ? (1f / 120f) : (1f / 90f);
            _alpha = MathHelper.Clamp(_alpha + MathF.Sign(objetivo - _alpha) * paso, 0f, 1f);
            _pintadoEsteFrame = false;
        }

        // ==================================================================
        //  EL DIBUJO — EN LA BANDA DE MONTAÑAS (después de las nubes
        //  lejanas, antes de las capas frontales → el resplandor baña
        //  el horizonte de verdad).
        // ==================================================================
        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (Descargado || _alpha <= 0.01f || _pintadoEsteFrame) return;
            if (!(minDepth < 8.2f && minDepth > 1.5f)) return;
            _pintadoEsteFrame = true;

            // La red de seguridad de siempre (v6.50.20): el cielo jamás
            // puede romper el render del juego.
            try { PintarResplandor(spriteBatch); }
            catch
            {
                _alpha = 0f;
                _activo = false;
            }
        }

        // ==================================================================
        //  EL RESPLANDOR — v6.50.36 — LA LUZ EN EL PAISAJE
        //
        //  EL CONTRATO DEL LOTE (la lección del client.log, v6.50.20):
        //  CustomSky.Draw corre DENTRO del lote del fondo de vanilla
        //  (lote ABIERTO). El patrón del DoGSky: End del lote de vanilla
        //  → lotes propios con LA MISMA MATRIZ del fondo → devolver el
        //  lote del fondo ABIERTO para el End de vanilla. Las sondas de
        //  la casa blindan cada cierre.
        // ==================================================================
        private void PintarResplandor(SpriteBatch sb)
        {
            if (Main.gameMenu) return;

            // === LEER AL JEFE: la llegada (alpha alto = naciendo) y el
            //     eclipse (ai[1]==3 — el cielo también se apaga) ===
            float llegada = 0f;
            bool eclipse = false;
            int tipoJefe = ModContent.NPCType<AethonBoss>();
            try
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n != null && n.active && n.type == tipoJefe)
                    {
                        if (!n.dontTakeDamage) llegada = Math.Max(llegada, n.alpha / 255f);
                        if (n.ai[1] == 3f) eclipse = true;
                    }
                }
            }
            catch { }
            if (llegada > 0.01f) _llegadaVista = llegada;
            else _llegadaVista *= 0.965f; // el fade de salida (el eco de la ventana)

            // === LA MATRIZ DEL PAISAJE (la reconstrucción EXACTA del lote
            //     del fondo — literal del DoGSky/Calamity) ===
            Matrix m = Main.BackgroundViewMatrix.TransformationMatrix;
            m.Translation -= Main.BackgroundViewMatrix.ZoomMatrix.Translation *
                new Vector3(1f,
                    Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically) ? -1f : 1f,
                    1f);

            float t = Main.GlobalTimeWrappedHourly;
            Texture2D glow = VFXCore.SoftGlow;
            Texture2D orbe = VFXCore.GlowOrb;
            Vector2 origen = new Vector2(glow.Width, glow.Height) * 0.5f;
            float w = Main.screenWidth;
            float h = Main.screenHeight;

            // === 0. CERRAR el lote del fondo de vanilla (llega ABIERTO) ===
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                // === 1) EL VELO DEL CIELO (lote aditivo: la luz inunda) ===
                sb.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    Main.Rasterizer, null, m);
                try
                {
                    // EL COLOR del cielo: dorado vivo… o el violeta del eclipse.
                    Color cLuz = eclipse ? new Color(110, 70, 190) : new Color(255, 226, 140);

                    // EL RESPLANDOR PERMANENTE: mientras la luz vive, el
                    // horizonte arde suave (la atmósfera del sol).
                    float pulso = 0.82f + 0.18f * MathF.Sin(t * 0.9f);
                    float baseBrillo = eclipse ? 0.05f : 0.10f;
                    sb.Draw(glow, new Vector2(w * 0.5f, -h * 0.55f), null,
                        cLuz * (_alpha * baseBrillo * pulso), 0f, origen,
                        new Vector2(w * 2.6f / glow.Width, h * 1.6f / glow.Height),
                        SpriteEffects.None, 0f);

                    // === LA LLEGADA: EL CIELO SE ENCIENDE DE VERDAD ===
                    if (_llegadaVista > 0.02f)
                    {
                        float av = _llegadaVista;

                        // EL DILUVIO: el resplandor que CRECE hasta inundar.
                        sb.Draw(glow, new Vector2(w * 0.5f, -h * 0.30f), null,
                            cLuz * (_alpha * av * 0.42f * pulso), 0f, origen,
                            new Vector2(w * 3.4f / glow.Width, h * 2.2f / glow.Height),
                            SpriteEffects.None, 0f);

                        // LAS SIETE COLUMNAS LEJANAS: el eco del Juicio —
                        // pilares de luz alzándose del borde del mundo.
                        for (int i = 0; i < 7; i++)
                        {
                            float u = (i - 3f) / 3.5f;                       // -1..1
                            float x = w * (0.5f + u * 0.46f);
                            float alto = h * (0.34f + 0.16f * MathF.Sin(t * 1.3f + i * 1.7f));
                            float lat = 0.6f + 0.4f * MathF.Sin(t * 2.4f + i * 2.3f);
                            sb.Draw(glow, new Vector2(x, h * 0.42f - alto * 0.5f), null,
                                cLuz * (_alpha * av * 0.16f * lat), 0f, origen,
                                new Vector2(90f / glow.Width, alto / glow.Height),
                                SpriteEffects.None, 0f);
                        }

                        // LA VENTANA: el orbe del núcleo creciendo en el
                        // centro del cielo — la puerta de la luz (donde
                        // va a nacer Aethon).
                        float tam = (30f + 130f * av) * (0.92f + 0.08f * MathF.Sin(t * 3.2f));
                        sb.Draw(orbe, new Vector2(w * 0.5f, h * 0.30f), null,
                            new Color(255, 246, 210) * (_alpha * av * 0.55f),
                            0f, new Vector2(orbe.Width, orbe.Height) * 0.5f,
                            tam / orbe.Width, SpriteEffects.None, 0f);
                        // el halo de la ventana
                        sb.Draw(glow, new Vector2(w * 0.5f, h * 0.30f), null,
                            cLuz * (_alpha * av * 0.20f), 0f, origen,
                            new Vector2(tam * 2.6f / glow.Width, tam * 2.6f / glow.Height),
                            SpriteEffects.None, 0f);
                    }
                }
                finally { VFXCore.CerrarLoteSiAbierto(); } // el propio, sin first-chance
            }
            finally
            {
                // === 2. DEVOLVER EL LOTE DEL FONDO ABIERTO (el patrón del
                //     DoGSky — el End de vanilla lo cerrará con naturalidad) ===
                if (!VFXCore.LoteAbierto)
                {
                    try
                    {
                        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                            SamplerState.LinearClamp, DepthStencilState.None,
                            Main.Rasterizer, null, m);
                    }
                    catch { }
                }
            }
        }

        // accesores internos para el sistema registrador (mismo archivo)
        internal bool ActivoInterno => _activo;
        internal float AlfaInterno => _alpha;

        // LA LLEGADA: la intensidad del encendido (vive mientras el
        // núcleo se materializa; se disuelve después).
        private float _llegadaVista = 0f;
    }

    // ======================================================================
    //  EL REGISTRO (la casa: SkyManager + armadura de descarga)
    // ======================================================================
    public class ColaSierpeSistema : ModSystem
    {
        private static ColaSierpeSky _cielo;

        public override void Load()
        {
            ColaSierpeSky.Descargado = false;
            if (Main.dedServ) return;
            try
            {
                _cielo = new ColaSierpeSky();
                SkyManager.Instance["AethonMod:ColaSierpe"] = _cielo;
            }
            catch { }
        }

        public override void Unload()
        {
            // tML no expone Remove de SkyManager (verificado R59-a):
            // el cielo queda registrado pero MUERTO — IsActive() falso
            // para siempre, cero referencias a ModContent en Draw.
            ColaSierpeSky.Descargado = true;
            try { _cielo?.Reset(); }
            catch { }
            _cielo = null;
        }

        public override void OnWorldUnload()
        {
            try { SkyManager.Instance.Deactivate("AethonMod:ColaSierpe"); }
            catch { }
            try { _cielo?.Reset(); }
            catch { }
        }

        /// <summary>
        /// EL ACTIVADOR: el cielo se enciende cuando LA LUZ se presenta,
        /// y se funde cuando ella muere (el fade la apaga).
        /// </summary>
        public override void PostUpdateWorld()
        {
            if (ColaSierpeSky.Descargado || Main.gameMenu) return;
            bool jefeVivo = false;
            try
            {
                int tipo = ModContent.NPCType<AethonBoss>();
                for (int i = 0; i < Main.maxNPCs && !jefeVivo; i++)
                    if (Main.npc[i].active && Main.npc[i].type == tipo)
                        jefeVivo = true;
            }
            catch { return; }

            try
            {
                if (jefeVivo && (_cielo == null || !_cielo.ActivoInterno))
                    SkyManager.Instance.Activate("AethonMod:ColaSierpe");
                else if (!jefeVivo && _cielo != null && _cielo.ActivoInterno &&
                         _cielo.AlfaInterno <= 0.02f)
                    SkyManager.Instance.Deactivate("AethonMod:ColaSierpe");
            }
            catch { }
        }
    }
}
