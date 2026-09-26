using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using AethonMod.Content.NPCs;

namespace AethonMod.Content.Effects
{
    // ======================================================================
    //  v6.50.19 — LA COLA ENREDA EN LAS IMÁGENES DE FONDO.
    //
    //  Petición del usuario (literal): "cuando se presenta su cola debe
    //  enredarse en las imágenes de fondo". El hallazgo de la investi-
    //  gación R59-a: vanilla TIENE un mecanismo oficial para dibujar
    //  entre capas de parallax — SkyManager.DrawToDepth: el juego llama
    //  a cada capa de fondo con su profundidad (1/parallax) y el Custom-
    //  Sky pinta en la banda que toca. Calamity hace EXACTO esto con el
    //  DoGSky (SkyEffectLoaderSystem.Load: SkyManager.Instance["Calamity
    //  Mod:DevourerofGodsHead"] = new DoGSky()).
    //
    //  LA PROYECCIÓN (la fórmula del DoGSky, literal): el punto del mundo
    //  se proyecta al espacio del paisaje con (p − centro) · (1/prof,
    //  0.9/prof) + centro — la cola se mueve CASI como el fondo y queda
    //  ENREDADA entre montañas y nubes, con la oclusión correcta: los
    //  árboles frontales la tapan, ella tapa las montañas.
    //
    //  QUIÉNES se dibujan: los segmentos con NPC.hide (índice ≥ UMBRAL_
    //  FONDO + la cola) — huesos que NO existen en el plano del mundo.
    // ======================================================================
    public class ColaSierpeSky : CustomSky
    {
        /// <summary>Profundidad falsa de la cola (más alta = más lejos).</summary>
        private const float Profundidad = 6.5f;

        /// <summary>La cola aparece al presentar la sierpe (fade 2 s).</summary>
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
        //  EL DIBUJO — EN LA BANDA DE MONTAÑAS (la primera llamada de
        //  profundidad media: después de las nubes lejanas, antes de las
        //  capas frontales → los árboles del bioma la TAPAN: enredada).
        // ==================================================================
        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (Descargado || _alpha <= 0.01f || _pintadoEsteFrame) return;
            // La banda de la profundidad de montañas (DrawToDepth pasa
            // ~11/8 para nubes; ~5-6 para montañas; menor para capas
            // frontales): la PRIMERA llamada de esa zona pinta UNA vez.
            if (!(minDepth < 8.2f && minDepth > 1.5f)) return;
            _pintadoEsteFrame = true;

            try { PintarCola(spriteBatch); }
            catch
            {
                // El cielo jamás puede romper el render del juego (la casa).
                _alpha = 0f;
                _activo = false;
            }
        }

        // ==================================================================
        //  LA COLA — los huesos proyectados al paisaje
        // ==================================================================
        private void PintarCola(SpriteBatch sb)
        {
            if (Main.gameMenu) return;

            // === RECOLECTAR los huesos del fondo (índices altos + cola) ===
            int tipoCuerpo = ModContent.NPCType<AethonSierpeCuerpo>();
            int tipoCola = ModContent.NPCType<AethonSierpeCola>();
            var huesos = new List<(Vector2 c, float rot, int idx)>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active) continue;
                if (n.type == tipoCola)
                    huesos.Add((n.Center, n.rotation, 999));
                else if (n.type == tipoCuerpo && n.ai[3] >= AethonSierpeCuerpo.UMBRAL_FONDO)
                    huesos.Add((n.Center, n.rotation, (int)n.ai[3]));
            }
            if (huesos.Count == 0) return;
            huesos.Sort((a, b) => a.idx.CompareTo(b.idx)); // de la 26 a la punta

            // === EL CENTRO DE LA PANTALLA (el ancla del parallax) ===
            Vector2 centroPantalla = Main.screenPosition +
                new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
            Vector2 f = new Vector2(1f / Profundidad, 0.9f / Profundidad);

            Texture2D texVertebra = ModContent.Request<Texture2D>(
                "AethonMod/Content/NPCs/AethonSierpeVertebra").Value;
            Texture2D texCola = ModContent.Request<Texture2D>(
                "AethonMod/Content/NPCs/AethonSierpeCola").Value;
            Vector2 origenV = new Vector2(texVertebra.Width, texVertebra.Height) * 0.5f;
            Vector2 origenC = new Vector2(texCola.Width, texCola.Height) * 0.5f;

            float t = Main.GlobalTimeWrappedHourly;

            // === 1) LA SILUETA ATMOSFÉRICA (lote alfa — la niebla del horizonte) ===
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Matrix.Identity);
            try
            {
                for (int k = 0; k < huesos.Count; k++)
                {
                    var (c, rot, idx) = huesos[k];
                    bool esCola = idx >= 999;
                    int prof = Math.Min(huesos.Count, Math.Max(1, idx - AethonSierpeCuerpo.UMBRAL_FONDO + 1));

                    // LA PROYECCIÓN DEL DoG: el mundo visto desde lejos.
                    Vector2 proy = (c - centroPantalla) * f + centroPantalla;
                    // EL VAIVÉN (la cola viva en el horizonte — se ENREDA).
                    proy.Y += MathF.Sin(t * 1.8f + k * 0.55f) * 9f;
                    proy.X += MathF.Sin(t * 0.7f + k * 0.4f) * 5f;
                    Vector2 pos = proy - Main.screenPosition;

                    // La escala: se encoge hacia la punta (perspectiva lejana).
                    float esc = (esCola ? 0.58f : 0.66f - 0.14f * prof / 13f) * 1.5f;

                    // El TINTE del horizonte: hueso azul-violeta, translúcido,
                    // más tenue cuanto más atrás (atmósfera de verdad).
                    float desvanecer = (1f - prof / 26f) * 0.28f + 0.42f;
                    Color silueta = new Color(96, 88, 126) * (_alpha * desvanecer);

                    sb.Draw(esCola ? texCola : texVertebra, pos, null, silueta,
                        rot, esCola ? origenC : origenV, esc, SpriteEffects.None, 0f);
                }
            }
            finally { sb.End(); }

            // === 2) EL RIM DORADO (lote aditivo — la Luz brilla incluso lejos) ===
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Matrix.Identity);
            try
            {
                for (int k = 0; k < huesos.Count; k += 2) // cada 2 huesos: puntual y barato
                {
                    var (c, rot, idx) = huesos[k];
                    bool esCola = idx >= 999;
                    int prof = Math.Min(huesos.Count, Math.Max(1, idx - AethonSierpeCuerpo.UMBRAL_FONDO + 1));
                    Vector2 proy = (c - centroPantalla) * f + centroPantalla;
                    proy.Y += MathF.Sin(t * 1.8f + k * 0.55f) * 9f;
                    proy.X += MathF.Sin(t * 0.7f + k * 0.4f) * 5f;
                    Vector2 pos = proy - Main.screenPosition;

                    float latido = 0.6f + 0.4f * MathF.Sin(t * 3.1f + k * 0.9f);
                    float brillo = 0.20f * _alpha * (1f - prof / 30f) * latido;
                    Texture2D glow = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/SoftGlow").Value;
                    sb.Draw(glow, pos, null, new Color(255, 226, 140) * brillo,
                        rot, glow.Size() * 0.5f, (esCola ? 0.30f : 0.38f),
                        SpriteEffects.None, 0f);
                }
            }
            finally { sb.End(); }
        }

        // accesores internos para el sistema registrador (mismo archivo)
        internal bool ActivoInterno => _activo;
        internal float AlfaInterno => _alpha;
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
        /// EL ACTIVADOR: la cola entra en el paisaje cuando la sierpe
        /// se presenta, y se funde cuando muere (el fade la apaga).
        /// </summary>
        public override void PostUpdateWorld()
        {
            if (ColaSierpeSky.Descargado || Main.gameMenu) return;
            bool sierpeViva = false;
            try
            {
                int tipo = ModContent.NPCType<AethonBoss>();
                for (int i = 0; i < Main.maxNPCs && !sierpeViva; i++)
                    if (Main.npc[i].active && Main.npc[i].type == tipo)
                        sierpeViva = true;
            }
            catch { return; }

            try
            {
                if (sierpeViva && (_cielo == null || !_cielo.ActivoInterno))
                    SkyManager.Instance.Activate("AethonMod:ColaSierpe");
                else if (!sierpeViva && _cielo != null && _cielo.ActivoInterno &&
                         _cielo.AlfaInterno <= 0.02f)
                    SkyManager.Instance.Deactivate("AethonMod:ColaSierpe");
            }
            catch { }
        }
    }
}
