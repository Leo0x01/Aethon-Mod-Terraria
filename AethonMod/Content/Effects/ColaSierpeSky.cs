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
    //  v6.50.37 — EL CIELO DE LA LLEGADA DEFINITIVA (EL MEDIO DÍA DE LA
    //  OSCURIDAD).
    //
    //  Petición del usuario: «cuando Aethon aparece destellos de luz
    //  aparecen en el cielo, el sol se vuelve negro y este queda
    //  centrado justo en el centro del cielo… antes de que Aethon
    //  aparezca, cuando el sol esta centrado en el cielo, el sol brilla
    //  con intensidad y de ahi aparece Aethon».
    //
    //  LA SECUENCIA (leída del espejo AethonBoss.SubLlegada — vive en
    //  TODAS las máquinas):
    //  · SUB 10 (EL MUNDO TIEMBLA): LOS DESTELLOS — chispas de luz
    //    naciendo y muriendo por todo el cielo + las SIETE COLUMNAS
    //    lejanas alzándose (el eco del Juicio) + LA VENTANA pequeña
    //    abriéndose DONDE ESTÁ EL SOL.
    //  · SUB 11 (EL TIEMPO CORRE): el sol ATRAVIESA el cielo — LA
    //    VENTANA LO SIGUE (la puerta de la luz persiguiendo a su dueño).
    //  · SUB 12 (EL SOL BRILLA CON INTENSIDAD): la ventana se vuelve
    //    un HOYO CEGADOR en el cielo — el blanco que crece hasta que
    //    el VELO de VeloLib lo reemplaza con EL FLASH de pantalla completa.
    //  · SUB 13+ (LA OSCURIDAD): el cielo se APAGA — el resplandor
    //    dorado permanente muere a un violeta de ultratumba (la luz del
    //    mundo ya no está aquí: está en ÉL).
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

        // === LOS DESTELLOS DEL CIELO (las chispas de la llegada) ===
        private struct Destello
        {
            public Vector2 Pos;
            public int Edad;
            public int Vida;
            public float Tam;
        }
        private readonly Destello[] _destellos = new Destello[18];
        private int _cursor = 0;

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

            // === LOS DESTELLOS NACEN (subs 10-12: el cielo se llena de
            //     chispas que nacen y mueren — cada cliente los ve suyos) ===
            int sub = AethonBoss.SubLlegada;
            if (!Main.gameMenu && sub >= 10 && sub <= 12 && Main.rand.NextBool(5))
            {
                ref Destello d = ref _destellos[_cursor];
                d.Pos = new Vector2(
                    Main.screenWidth * Main.rand.NextFloat(0.04f, 0.96f),
                    Main.screenHeight * Main.rand.NextFloat(0.04f, 0.42f));
                d.Edad = 0;
                d.Vida = Main.rand.Next(26, 52);
                d.Tam = Main.rand.NextFloat(22f, 58f);
                _cursor = (_cursor + 1) % _destellos.Length;
            }
            // todos envejecen
            for (int i = 0; i < _destellos.Length; i++)
                if (_destellos[i].Edad < _destellos[i].Vida) _destellos[i].Edad++;
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
        //  EL RESPLANDOR — v6.50.37 — EL CIELO DE LA LLEGADA
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

            // === LEER EL ESTADO DEL MUNDO (el espejo de la casa — vive
            //     en todas las máquinas leyendo ai[] del jefe) ===
            int sub = AethonBoss.SubLlegada;
            bool eclipse = false;
            int tipoJefe = ModContent.NPCType<AethonBoss>();
            try
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n != null && n.active && n.type == tipoJefe && n.ai[1] == 3f)
                    {
                        eclipse = true;
                        break;
                    }
                }
            }
            catch { }

            // LA LLEGADA: intensidad 1 mientras los subs 10-12; al llegar
            // la oscuridad (13) la luz YA NO ESTÁ en el cielo — se apaga.
            float objetivoLlegada = (sub >= 10 && sub <= 12) ? 1f : 0f;
            float pasoLlegada = objetivoLlegada > _llegadaVista ? (1f / 25f) : (1f / 22f);
            _llegadaVista = MathHelper.Clamp(
                _llegadaVista + MathF.Sign(objetivoLlegada - _llegadaVista) * pasoLlegada,
                0f, 1f);

            bool oscuro = AethonBoss.OscuridadObjetivo || AethonBoss.TiempoCongelado;

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
                    // EL COLOR del cielo: dorado vivo… violeta del eclipse…
                    // o el VIOLETA MUERTO de LA OSCURIDAD (la luz ya no
                    // está aquí — está TODA en un solo lugar).
                    Color cLuz = eclipse ? new Color(110, 70, 190)
                        : (oscuro ? new Color(70, 46, 130) : new Color(255, 226, 140));

                    // EL RESPLANDOR PERMANENTE: mientras la luz vive, el
                    // horizonte arde suave… salvo en la oscuridad, donde
                    // solo queda un fantasma violeta.
                    float pulso = 0.82f + 0.18f * MathF.Sin(t * 0.9f);
                    float baseBrillo = eclipse ? 0.05f : (oscuro ? 0.028f : 0.10f);
                    sb.Draw(glow, new Vector2(w * 0.5f, -h * 0.55f), null,
                        cLuz * (_alpha * baseBrillo * pulso), 0f, origen,
                        new Vector2(w * 2.6f / glow.Width, h * 1.6f / glow.Height),
                        SpriteEffects.None, 0f);

                    // === LA LLEGADA ===
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

                        // === LA VENTANA — DONDE ESTÁ EL SOL (la puerta de
                        //     la luz): durante la carrera SIGUE al sol en su
                        //     carrera; en el climax se vuelve CEGADORA. La
                        //     posición es la EXACTA de vanilla (el decompile
                        //     de DrawSunAndMoon — la casa la replica).
                        //     v6.50.39: de NOCHE no hay sol en el cielo (la
                        //     luna barre y el alba llega sola) — la puerta de
                        //     la luz no abre donde no hay sol: la ventana
                        //     espera al alba de LA CARRERA. ===
                        if (Main.dayTime)
                        {
                            Vector2 posSol = AethonBoss.PosicionSolEnCielo();
                            float crescendo = sub == 12 ? Math.Min(1f, ContarTickClimax() / 90f) : 0f;
                            float tam = (34f + 66f * av + 390f * crescendo) *
                                (0.92f + 0.08f * MathF.Sin(t * (3.2f + 6f * crescendo)));

                            // EL NÚCLEO CEGADOR (blanco puro al climax)
                            sb.Draw(orbe, posSol, null,
                                new Color(255, 250, 224) * (_alpha * (av * 0.55f + 0.45f * crescendo)),
                                0f, new Vector2(orbe.Width, orbe.Height) * 0.5f,
                                tam / orbe.Width, SpriteEffects.None, 0f);
                            // el halo de la ventana
                            sb.Draw(glow, posSol, null,
                                cLuz * (_alpha * (av * 0.20f + 0.25f * crescendo)), 0f, origen,
                                new Vector2(tam * 2.6f / glow.Width, tam * 2.6f / glow.Height),
                                SpriteEffects.None, 0f);

                            // LOS RAYOS DEL CLIMAX (cuando el sol BRILLA con
                            // intensidad, sus rayos se alargan por el cielo)
                            if (crescendo > 0.05f)
                            {
                                Vector2 origenR = new Vector2(glow.Width, glow.Height) * 0.5f;
                                for (int i = 0; i < 8; i++)
                                {
                                    float ang = i * MathHelper.PiOver4 + t * 0.3f;
                                    float largo = tam * (1.4f + 1.2f * crescendo);
                                    sb.Draw(glow, posSol + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                                        (tam * 0.8f + largo * 0.5f), null,
                                        new Color(255, 244, 200) * (_alpha * 0.30f * crescendo),
                                        ang, origenR,
                                        new Vector2(largo / glow.Width, (6f + 10f * crescendo) / glow.Height),
                                        SpriteEffects.None, 0f);
                                }
                            }
                        }
                    }

                    // === LOS DESTELLOS DEL CIELO (las chispas de la
                    //     llegada: nacen, arden y mueren — el cielo anuncia
                    //     que la luz está LLEGANDO) ===
                    for (int i = 0; i < _destellos.Length; i++)
                    {
                        Destello d = _destellos[i];
                        if (d.Edad >= d.Vida) continue;
                        float progreso = d.Edad / (float)d.Vida;
                        float brillo = MathF.Sin(progreso * MathHelper.Pi);   // nace y muere
                        if (brillo <= 0f) continue;
                        // EL CRUZADO: la chispa con estrella (la firma de
                        // la casa para los destellos con FORMA)
                        Texture2D cruz = VFXCore.DestelloFinal;
                        if (cruz != null)
                        {
                            sb.Draw(cruz, d.Pos, null,
                                new Color(255, 248, 220) * (_alpha * 0.30f * brillo),
                                d.Edad * 0.06f,
                                new Vector2(cruz.Width, cruz.Height) * 0.5f,
                                d.Tam * (0.8f + 0.4f * brillo) / cruz.Width,
                                SpriteEffects.None, 0f);
                        }
                        else
                        {
                            sb.Draw(glow, d.Pos, null,
                                new Color(255, 248, 220) * (_alpha * 0.30f * brillo),
                                0f, origen,
                                new Vector2(d.Tam / glow.Width, d.Tam / glow.Height),
                                SpriteEffects.None, 0f);
                        }
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

        /// <summary>El tick del climax leído del jefe (para el crescendo de la ventana).</summary>
        private static float ContarTickClimax()
        {
            try
            {
                int tipo = ModContent.NPCType<AethonBoss>();
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n != null && n.active && n.type == tipo && n.ai[0] == 0f)
                        return n.ai[2];
                }
            }
            catch { }
            return 0f;
        }

        // accesores internos para el sistema registrador (mismo archivo)
        internal bool ActivoInterno => _activo;
        internal float AlfaInterno => _alpha;

        // LA LLEGADA: la intensidad del encendido (vive mientras la luz
        // llega; se disuelve cuando la oscuridad toma el control).
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
