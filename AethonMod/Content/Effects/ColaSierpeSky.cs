using System;
using System.Collections.Generic;
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

            // v6.50.20 — red de seguridad PURA (el flujo normal ya no
            // lanza: ver PintarCola). v6.50.19 tenía el bug de lote: el
            // Begin propio contra el lote del fondo ABIERTO de vanilla
            // (InvalidOperationException) — este catch lo tragaba y
            // apagaba el cielo, y PostUpdateWorld lo re-encendía: bucle
            // de "Excepción silenciosa" por frame y cola INVISIBLE.
            try { PintarCola(spriteBatch); }
            catch
            {
                // El cielo jamás puede romper el render del juego (la casa).
                _alpha = 0f;
                _activo = false;
            }
        }

        // ==================================================================
        //  LA COLA — v6.50.27 — EL LEVIATÁN DE CÓDIGO (el arte nuevo)
        //
        //  EL REPORTE: «el arte del jefe se ve horrible, deberías
        //  cambiarlo por completo, algo al estilo de la sierpe en el arma
        //  La Sierpe Estelar». EL FONDO TAMBIÉN VISTE EL ARTE NUEVO: los
        //  sprites de vértebra/cola/cráneo mueren — la silueta del
        //  horizonte son CÁPSULAS y ORBES (el mismo ADN del cuerpo
        //  visible, proyectado entre las capas del paisaje) + el rim
        //  dorado de siempre + las APOFISIS estelares latiendo.
        //
        //  v6.50.20 — EL CONTRATO DEL LOTE (la lección del client.log):
        //  CustomSky.Draw corre DENTRO del lote del fondo de vanilla
        //  (Main.Draw: Begin(Deferred·Alpha·LinearClamp·None·Rasterizer·
        //  transformaciónDelFondo) → DrawBG() → DrawSurfaceBG() →
        //  SkyManager.DrawToDepth → AQUÍ, lote ABIERTO → ... → End).
        //  El patrón v6.50.19 (Begin propio sin más) lanzaba
        //  InvalidOperationException (Begin sobre Begin) en CADA frame.
        //  EL PATRÓN DEL DoGSky de Calamity (la referencia de
        //  producción): End del lote de vanilla → lotes propios con LA
        //  MISMA MATRIZ del fondo → devolver el lote del fondo ABIERTO
        //  para que el End de vanilla lo cierre con naturalidad. La
        //  sonda de la casa blindan cada cierre (cero first-chance si
        //  un mod ajeno dejó el lote en un estado raro).
        // ==================================================================
        private void PintarCola(SpriteBatch sb)
        {
            if (Main.gameMenu) return;

            // === v6.50.26 — LA LLEGADA (el reporte: «no aparece una
            //     sección de él en el fondo cuando está llegando»):
            //     mientras el CRÁNEO se materializa (alpha alto — el
            //     nacimiento), la SILUETA GIGANTE de la sierpe cruza el
            //     cielo del fondo — un leviatán de luz nadando ENTRE las
            //     capas del paisaje, con los OJOS DE ORO avanzando.
            //     Cuando la cabeza termina de nacer, la silueta se
            //     disuelve (~1.5 s de recuerdo). ===
            float llegada = 0f;
            int tipoCabeza = ModContent.NPCType<AethonBoss>();
            try
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n != null && n.active && n.type == tipoCabeza &&
                        !n.dontTakeDamage) // el cine de muerte no invoca
                        llegada = Math.Max(llegada, n.alpha / 255f);
                }
            }
            catch { }
            if (llegada > 0.01f) _llegadaVista = llegada;
            else _llegadaVista *= 0.965f; // el fade de salida del leviatán

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
            if (huesos.Count == 0 && llegada <= 0.01f) return;
            huesos.Sort((a, b) => a.idx.CompareTo(b.idx)); // de la 26 a la punta

            // === EL CENTRO DE LA PANTALLA (el ancla del parallax) ===
            Vector2 centroPantalla = Main.screenPosition +
                new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
            Vector2 f = new Vector2(1f / Profundidad, 0.9f / Profundidad);

            Texture2D glow = VFXCore.SoftGlow;          // el pincel de las cápsulas
            Texture2D estrella = AethonSierpeArte.EstrellaDelFondo();
            Texture2D orbe = VFXCore.GlowOrb;            // respaldo del cráneo
            // v6.50.33 — LOS SPRITES DEL DRAGÓN para el leviatán: la
            // llegada del fondo ya tiene la cara del jefe de verdad.
            Texture2D cabezaSlifer = null;
            Texture2D mandibulaSlifer = null;
            Texture2D colaSlifer = null;
            try
            {
                cabezaSlifer = AethonSierpeArte.Cabeza();
                mandibulaSlifer = AethonSierpeArte.Mandibula();
                colaSlifer = AethonSierpeArte.Cola();
            }
            catch { }
            Vector2 origenGlow = new Vector2(glow.Width, glow.Height) * 0.5f;

            float t = Main.GlobalTimeWrappedHourly;

            // === LA MATRIZ DEL PAISAJE — la reconstrucción EXACTA del lote
            // del fondo (la misma corrección que Main.Draw le hace a
            // BackgroundViewMatrix; literal del DoGSky/Calamity): los
            // huesos "pertenecen" al fondo y heredan su espacio. ===
            Matrix m = Main.BackgroundViewMatrix.TransformationMatrix;
            m.Translation -= Main.BackgroundViewMatrix.ZoomMatrix.Translation *
                new Vector3(1f,
                    Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically) ? -1f : 1f,
                    1f);

            // === 0. CERRAR el lote del fondo de vanilla (llega ABIERTO —
            // el contrato; por sonda: si un mod ajeno lo dejó cerrado,
            // aquí NO lanza) ===
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                // === 1) LA SILUETA ATMOSFÉRICA (lote alfa — la niebla del
                //     horizonte): CÁPSULAS de vacío + la punta estelar) ===
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    Main.Rasterizer, null, m);
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
                        float rumbo = rot - MathHelper.PiOver2; // la dirección del hueso

                        // El TINTE del horizonte: hueso granate-gris (v6.50.32 — el
                        // Dragón del Cielo tiñe de escarlata su propia lejanía), translúcido,
                        // más tenue cuanto más atrás (atmósfera de verdad).
                        float desvanecer = (1f - prof / 26f) * 0.28f + 0.42f;
                        Color silueta = new Color(104, 60, 54) * (_alpha * desvanecer);

                        if (esCola && colaSlifer != null)
                        {
                            // v6.50.33 — LA COLA DE VERDAD: la pala espatulada del
                            // set, silueteada a granate (la punta del sprite mira
                            // ATRÁS: el rumbo del hueso la deja arrastrando).
                            sb.Draw(colaSlifer, pos, null, silueta, rumbo,
                                new Vector2(colaSlifer.Width, colaSlifer.Height) * 0.5f,
                                esc * 0.78f, SpriteEffects.None, 0f);
                        }
                        else
                        {
                            // LA CÁPSULA del hueso (el cuerpo de vacío del fondo).
                            float largo = (esCola ? 46f : 64f) * esc;
                            float ancho = (esCola ? 14f : 30f) * esc;
                            sb.Draw(glow, pos, null, silueta, rumbo, origenGlow,
                                new Vector2((largo + ancho) / glow.Width, ancho * 1.9f / glow.Height),
                                SpriteEffects.None, 0f);
                        }
                    }

                    // === v6.50.27 — EL LEVIATÁN DE LA LLEGADA (la
                    //     silueta GIGANTE cruzando el cielo del fondo:
                    //     16 vértebras cápsula ×2.6 + el CRÁNEO orbe al
                    //     frente y sus DOS HOJAS de mandíbula — el
                    //     arribo del final, tapado por los ÁRBOLES y
                    //     tapando las MONTAÑAS: enredado de verdad) ===
                    if (_llegadaVista > 0.02f)
                    {
                        const int NB = 16;
                        float cx = Main.screenWidth * 0.5f;
                        float anchoCielo = Main.screenWidth * 1.05f;
                        float baseY = Main.screenHeight * 0.34f;

                        for (int b = NB - 1; b >= 0; b--) // de la COLA a la CABEZA
                        {
                            float u = (b - (NB - 1) * 0.5f) / ((NB - 1) * 0.5f); // -1..1
                            // EL MEANDRO del leviatán: una ola lenta que
                            // respira (nada, no está parado).
                            float y = baseY
                                - MathF.Sin(u * MathF.PI * 0.9f + 0.4f) * 90f
                                + MathF.Sin(t * 0.9f + u * 2.6f) * 16f;
                            float x = cx + u * anchoCielo * 0.5f
                                + MathF.Sin(t * 0.35f) * 60f;
                            // la tangente local (la rotación del hueso)
                            float yNext = baseY
                                - MathF.Sin((u + 0.06f) * MathF.PI * 0.9f + 0.4f) * 90f
                                + MathF.Sin(t * 0.9f + (u + 0.06f) * 2.6f) * 16f;
                            float rotL = MathF.Atan2(yNext - y, anchoCielo * 0.5f * 0.06f)
                                + MathHelper.PiOver2;
                            float rumboL = rotL - MathHelper.PiOver2;

                            float escL = 2.6f - MathF.Abs(u) * 0.55f; // encoge a la cola
                            Color sil = new Color(96, 54, 48) *
                                (_alpha * _llegadaVista * 0.42f);

                            if (b == 0)
                            {
                                // v6.50.33 — EL CRÁNEO DEL DRAGÓN (u = +1: la
                                // cabeza guía el nado): el SPRITE REAL de la
                                // cabeza silueteado a granate (la máscara de
                                // acero y los colmillos se adivinan en la
                                // lejanía) + LA MANDÍBULA ABIERTA girada —
                                // el leviatán llega RUGIENDO. Respaldo: el
                                // orbe de siempre si el asset no vive.
                                float uH = 1f;
                                float yH = baseY
                                    - MathF.Sin(uH * MathF.PI * 0.9f + 0.4f) * 90f
                                    + MathF.Sin(t * 0.9f + uH * 2.6f) * 16f;
                                float xC = cx + anchoCielo * 0.5f + MathF.Sin(t * 0.35f) * 60f;
                                if (cabezaSlifer != null)
                                {
                                    float tamH = 96f * 2.3f;
                                    float escH = tamH / cabezaSlifer.Width;
                                    Vector2 origH = new Vector2(cabezaSlifer.Width, cabezaSlifer.Height) * 0.5f;
                                    sb.Draw(cabezaSlifer, new Vector2(xC, yH), null,
                                        new Color(108, 62, 56) * (_alpha * _llegadaVista * 0.46f),
                                        rumboL, origH, escH, SpriteEffects.None, 0f);
                                    if (mandibulaSlifer != null)
                                    {
                                        // la bisagra del cráneo (local 100,94) manda
                                        Vector2 bis = new Vector2(xC, yH) +
                                            new Vector2(100f - origH.X, 94f - origH.Y)
                                                .RotatedBy(rumboL) * escH;
                                        sb.Draw(mandibulaSlifer, bis, null,
                                            new Color(96, 54, 48) * (_alpha * _llegadaVista * 0.42f),
                                            rumboL + 0.35f, new Vector2(12f, 50f),
                                            escH, SpriteEffects.None, 0f);
                                    }
                                }
                                else
                                {
                                    float tam = 96f * 2.3f;
                                    sb.Draw(orbe, new Vector2(xC, yH), null,
                                        new Color(108, 62, 56) * (_alpha * _llegadaVista * 0.46f),
                                        rumboL, new Vector2(orbe.Width, orbe.Height) * 0.5f,
                                        tam / orbe.Width, SpriteEffects.None, 0f);
                                }
                            }
                            else
                            {
                                // LA VÉRTEBRA CÁPSULA del leviatán.
                                float largoL = 64f * escL;
                                float anchoL = 30f * escL * (1f - MathF.Abs(u) * 0.3f);
                                sb.Draw(glow, new Vector2(x, y), null, sil, rumboL,
                                    origenGlow,
                                    new Vector2((largoL + anchoL) / glow.Width, anchoL * 1.9f / glow.Height),
                                    SpriteEffects.None, 0f);
                            }
                        }
                    }
                }
                finally { VFXCore.CerrarLoteSiAbierto(); } // el propio, sin first-chance

                // === 2) EL RIM DORADO (lote aditivo — la Luz brilla incluso lejos) ===
                sb.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    Main.Rasterizer, null, m);
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
                        float esc = (esCola ? 0.58f : 0.66f - 0.14f * prof / 13f) * 1.5f;

                        float latido = 0.6f + 0.4f * MathF.Sin(t * 3.1f + k * 0.9f);
                        float brillo = 0.20f * _alpha * (1f - prof / 30f) * latido;
                        sb.Draw(glow, pos, null, new Color(255, 226, 140) * brillo,
                            rot, new Vector2(glow.Width, glow.Height) * 0.5f,
                            new Vector2((esCola ? 0.24f : 0.34f)),
                            SpriteEffects.None, 0f);

                        // v6.50.27 — LA APOFISIS ESTELAR (cada 2 huesos: la
                        // estrella de 4 puntas latiendo en el centrum).
                        float tw = 0.5f + 0.5f * MathF.Sin(t * 2.8f + k * 1.7f);
                        float tamS = 16f * esc;
                        sb.Draw(estrella, pos, null,
                            new Color(255, 240, 190) * (0.22f * _alpha * tw),
                            rot + t * 0.5f, new Vector2(estrella.Width, estrella.Height) * 0.5f,
                            tamS / estrella.Width, SpriteEffects.None, 0f);
                    }

                    // v6.50.27 — LA PUNTA DE LUZ (la cola muere en
                    // ESTRELLA — el final del leviatán).
                    if (huesos.Count > 0)
                    {
                        var (cC, rotC, _) = huesos[huesos.Count - 1];
                        Vector2 proyC = (cC - centroPantalla) * f + centroPantalla;
                        Vector2 posC2 = proyC - Main.screenPosition;
                        sb.Draw(estrella, posC2, null,
                            new Color(255, 226, 140) * (0.30f * _alpha * (0.6f + 0.4f * MathF.Sin(t * 2.2f))),
                            t * 1.2f, new Vector2(estrella.Width, estrella.Height) * 0.5f,
                            22f / estrella.Width, SpriteEffects.None, 0f);
                    }

                    // === v6.50.26 — LOS OJOS DEL LEVIATÁN (la llegada
                    //     tiene MIRADA: dos brasas de ORO en el cráneo del
                    //     fondo + el latido tenue de cada 4ª vértebra) ===
                    if (_llegadaVista > 0.02f)
                    {
                        const int NB = 16;
                        float cx = Main.screenWidth * 0.5f;
                        float anchoCielo = Main.screenWidth * 1.05f;
                        float baseY = Main.screenHeight * 0.34f;

                        // EL CRÁNEO al frente (la misma fórmula de la silueta)
                        float xC = cx + anchoCielo * 0.5f + MathF.Sin(t * 0.35f) * 60f;
                        float yC = baseY
                            - MathF.Sin(1f * MathF.PI * 0.9f + 0.4f) * 90f
                            + MathF.Sin(t * 0.9f + 1f * 2.6f) * 16f;
                        float pulso = 0.55f + 0.45f * MathF.Sin(t * 2.6f);
                        for (int e = 0; e < 2; e++)
                        {
                            Vector2 ojo = new Vector2(xC - 34f + e * 24f, yC - 12f);
                            sb.Draw(glow, ojo, null,
                                new Color(255, 240, 190) * (0.60f * _llegadaVista * pulso),
                                0f, new Vector2(glow.Width, glow.Height) * 0.5f,
                                new Vector2(0.20f, 0.20f), SpriteEffects.None, 0f);
                        }

                        // el latido de la columna (cada 4 vértebras, tenue)
                        for (int b = 4; b < NB; b += 4)
                        {
                            float u = (b - (NB - 1) * 0.5f) / ((NB - 1) * 0.5f);
                            float y = baseY
                                - MathF.Sin(u * MathF.PI * 0.9f + 0.4f) * 90f
                                + MathF.Sin(t * 0.9f + u * 2.6f) * 16f;
                            float x = cx + u * anchoCielo * 0.5f + MathF.Sin(t * 0.35f) * 60f;
                            sb.Draw(glow, new Vector2(x, y), null,
                                new Color(255, 226, 140) * (0.10f * _llegadaVista),
                                0f, new Vector2(glow.Width, glow.Height) * 0.5f,
                                new Vector2(0.20f, 0.20f), SpriteEffects.None, 0f);
                        }
                    }
                }
                finally { VFXCore.CerrarLoteSiAbierto(); } // el propio, sin first-chance
            }
            finally
            {
                // === 3. DEVOLVER EL LOTE DEL FONDO ABIERTO (el End de
                // vanilla tras DrawBG lo cerrará con naturalidad — el
                // patrón del DoGSky). Los parámetros EXACTOS del lote
                // del fondo de vanilla: Deferred · AlphaBlend ·
                // LinearClamp · None · Main.Rasterizer · la matriz m. 
                // Por sonda: no se pisa un Begin vivo (si llegó abierto
                // de un mod ajeno, se respeta y se CURA solo). ===
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

        // v6.50.26 — LA LLEGADA: la intensidad del leviatán del fondo
        // (vive mientras el cráneo se materializa; se disuelve después).
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
