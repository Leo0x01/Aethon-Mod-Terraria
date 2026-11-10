using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Systems;
using AethonMod.Content.VFX;
using AethonMod.Content.Effects.Bruma;

namespace AethonMod.Content.Effects
{
    // ======================================================================
    //  CieloInteriorEterno — v6.118 — EL CIELO DEL CUERPO DEL DIOS.
    //
    //  LA LETRA (el rework del mundo interior): «el fondo del mundo debe
    //  ser un mundo de tentaculos por capas y oscuridad por capas, ademas
    //  de brumas y particulas» + el cuestionario: 3 CAPAS (lejos
    //  siluetas casi negras · medio con ojos que te siguen · cerca
    //  verlet enormes), piel NUEVA («Piel del Eterno» — la de la Forja,
    //  violeta-negro con vetas DORADAS), sol eclipse «EN CAPAS» (el
    //  disco negro vive arriba, abrazado por las capas y las brumas), el
    //  OJO COLOSAL FUERA (los ojos de los tentáculos lo reemplazan —
    //  TODOS te miran), brumas «TODAS» (paralaje + volutas) y polvo
    //  estelar denso en las capas lejanas.
    //
    //  EL VEHÍCULO: el CustomSky de la casa (el hallazgo R59-a de
    //  ColaSierpeSky — SkyManager.DrawToDepth pinta ENTRE LAS CAPAS del
    //  paisaje: el tentáculo queda DETRÁS de las montañas del propio
    //  interior, DELANTE del cielo). El contrato del lote del cielo de
    //  jefe: End del lote de vanilla → lotes propios con LA MATRIZ del
    //  fondo → devolver el lote del fondo ABIERTO (sonda de la casa en
    //  cada cierre). LA BRUMA DEL ASTRO vive en el postfix de
    //  DrawSunAndMoon (la posición EXACTA de vanilla — la misma
    //  matemática de AmbienteOleadaSistema) para abrazar el disco del
    //  eclipse.
    //
    //  LA VIDA: la Piel del Eterno FLUYE por las tiras (bandas de V con
    //  uvFlujo — la textura es periódica), los cercanos son VERLET
    //  (SombrasLib.ColumnaViva — inercia y overshoot de verdad), TODOS
    //  los ojos siguen al jugador (OjoAbismo, giro ±0.35 rad, parpadeo
    //  por escala Y) y TODO el cielo respira con EL LATIDO DEL CORAZÓN
    //  de amatista (SubmundoGrimorio.PulsoCorazon — el balanceo y los
    //  brillos se hinchan a cada golpe, ~5 s).
    //
    //  v6.120 — LAS CAPAS EN LOOP (la letra: «el fondo deben ser capas
    //  de tentaculos en loop y al menos 3 capas»): las TRES capas de
    //  tentáculos son ahora BANDAS PERIÓDICAS — cada capa tiene su
    //  PERIODO propio en píxeos (lejos 900 · medio 760 · cerca 640) y
    //  SUS tentáculos por periodo (4 · 3 · 2), dibujados en
    //  REPETICIONES que envuelven la pantalla por MÓDULO del paralaje:
    //  camines donde camines por el llano de obsidiana, el cuerpo del
    //  dios SE REPITE idéntico sin costura — el fondo es un LOOP de
    //  verdad (el mundo interior de una entidad cósmica NO TIENE BORDE:
    //  su interior te envuelve entero, siempre).
    // ======================================================================
    public class CieloInteriorEterno : CustomSky
    {
        /// <summary>La banda de profundidad del paisaje (la de las montañas).</summary>
        private const float Profundidad = 6.5f;

        private float _alpha = 0f;
        private bool _activo = false;
        private bool _pintadoEsteFrame = false;

        /// <summary>Bandera de descarga (la casa: el cielo muerto jamás toca ModContent).</summary>
        public static bool Descargado = false;

        // === LAS TEXTURAS (lazy, null-safe ante recargas calientes) ===
        private static Texture2D _piel;          // PielDelEterno (la Forja v6.118)
        private static Texture2D _ojo;           // OjoAbismo (la casa)
        private static Texture2D _chispa;        // SparkleStar (el polvo estelar)
        private static bool _texturasProbadas;

        // === EL POZO DE ANCLAS (determinista — la composición del dios es
        //     SIEMPRE LA MISMA: el interior se regenera idéntico) ===
        // LEJOS: 7 siluetas · MEDIO: 4 brazos con ojos · CERCA: 3 titanes verlet
        private static readonly int[] SemillaLejos = { 7101, 7109, 7117, 7127, 7139, 7149, 7161 };
        private static readonly int[] SemillaMedio = { 7203, 7217, 7233, 7249 };
        private static readonly int[] SemillaCerca = { 7307, 7331, 7353 };

        // ==================================================================
        //  EL CICLO DE VIDA (GameEffect 2026: Activate/Deactivate)
        // ==================================================================
        public override void Activate(Vector2 position, params object[] args) => _activo = true;

        public override void Deactivate(params object[] args) => _activo = false;

        public override void Reset()
        {
            _activo = false;
            _alpha = 0f;
        }

        public override bool IsActive() => _activo || _alpha > 0.001f;

        // accesores internos para el sistema registrador (mismo archivo)
        internal bool ActivoInterno => _activo;
        internal float AlfaInterno => _alpha;

        /// <summary>v6.121 — NACE PLENO: el cuerpo del dios NO hace
        /// fade-in dentro del interior (la letra: el fondo debe estar
        /// LLENO desde el primer frame — el velo de la revelación ya
        /// cubre la llegada; el fade de 2,4 s de la v6.120 dejaba los
        /// primeros segundos en PANTALLA NEGRA, y eso era justo lo que
        /// el jugador veía al entrar). El fundido queda para la SALIDA
        /// (Deactivate), donde sí es de buen tono.</summary>
        internal void NacerPleno()
        {
            if (_activo && _alpha < 1f) _alpha = 1f;
        }

        public override void Update(GameTime gameTime)
        {
            if (Descargado) { _alpha = 0f; return; }
            // EL FADE DE PRESENTACIÓN: 2,4 s de entrante, 1,5 s de salida.
            float objetivo = _activo ? 1f : 0f;
            float paso = _activo ? (1f / 145f) : (1f / 90f);
            _alpha = MathHelper.Clamp(_alpha + MathF.Sign(objetivo - _alpha) * paso, 0f, 1f);
            _pintadoEsteFrame = false;
        }

        // ==================================================================
        //  EL DIBUJO — v6.121 — EN TODAS LAS BANDAS DEL CIELO (la letra:
        //  «el fondo es una pantalla negra y se corta a unos píxeles del
        //  borde — debería ser un fondo lejano lleno de tentáculos»):
        //  la vieja banda de montañas (1.5 < minDepth < 8.2) dejaba el
        //  cielo de vanilla — NEGRO bajo el eclipse — asomando por todo lo
        //  que no fuera esa banda. El cuerpo del dios se pinta EN TODAS
        //  las bandas (la guard _pintadoEsteFrame lo hace UNA vez por
        //  frame, en la primera banda que pregunte: queda detrás del
        //  disco del eclipse y de las brumas — el fondo LEJANO de verdad,
        //  sin cortes ni costuras, de borde a borde).
        // ==================================================================
        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (Descargado || _alpha <= 0.01f || _pintadoEsteFrame) return;
            // EL CIELO DEL CUERPO VIVE EN LA SUPERFICIE: bajo la roca, el
            // interior ES la carne (las paredes de la profundidad mandan) —
            // el tentáculo no se cuela en la cueva del dios.
            try
            {
                Player p = Main.LocalPlayer;
                if (p != null && p.active && !p.dead &&
                    p.Center.Y / 16f > Main.rockLayer + 30f) return;
            }
            catch { }
            _pintadoEsteFrame = true;

            // La red de seguridad de siempre: el cielo jamás rompe el render.
            // v6.122 — PERO UN SOLO FALLO YA NO MATA EL CIELO PARA SIEMPRE:
            // el catch viejo ponía _activo=false a la PRIMERA excepción y
            // el mundo interior quedaba negro el resto de la partida (el
            // fondo negro reportado). Ahora aguanta 60 fallos consecutivos
            // (un segundo entero de frames malos) antes de rendirse; cualquier
            // frame bueno rearma el contador.
            try { PintarElCuerpo(spriteBatch); _fallosSeguidos = 0; }
            catch
            {
                _fallosSeguidos++;
                if (_fallosSeguidos > 60)
                {
                    _alpha = 0f;
                    _activo = false;
                }
            }
        }

        /// <summary>Contador de fallos consecutivos del pintado (v6.122).</summary>
        private int _fallosSeguidos;

        // ==================================================================
        //  EL CUERPO — tres capas de tentáculos + polvo estelar + brumas
        //  (EL CONTRATO DEL LOTE del cielo de jefe, byte a byte con
        //  ColaSierpeSky: End del fondo → propios con SU matriz → devolver
        //  el fondo ABIERTO)
        // ==================================================================
        private void PintarElCuerpo(SpriteBatch sb)
        {
            if (Main.gameMenu) return;
            if (!AsegurarTexturas()) return;

            float t = Main.GlobalTimeWrappedHourly;
            float w = Main.screenWidth;
            float h = Main.screenHeight;
            // EL LATIDO DEL CORAZÓN (vive en SubmundoGrimorio): el cielo
            // entero se hincha a cada golpe del dios.
            float latido = SubmundoGrimorio.PulsoCorazon;
            float hincha = 1f + 0.30f * latido;
            float a = _alpha;

            // LA MATRIZ DEL PAISAJE (la reconstrucción EXACTA del lote del
            // fondo — la investigación de la casa, byte a byte)
            Matrix m = Main.BackgroundViewMatrix.TransformationMatrix;
            m.Translation -= Main.BackgroundViewMatrix.ZoomMatrix.Translation *
                new Vector3(1f,
                    Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically) ? -1f : 1f,
                    1f);

            // el paralaje propio de cada capa (la cámara separa las
            // profundidades: el lejano apenas se mueve, el cercano vive)
            float camX = Main.screenPosition.X - w * 0.5f;
            float camY = Main.screenPosition.Y - h * 0.5f;

            // === 0. CERRAR el lote del fondo de vanilla (llega ABIERTO) ===
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                // ============================================================
                //  v6.121 — (0) LA PÁGINA DEL CUERPO: LA BASE QUE LLENA LA
                //      PANTALLA ENTERA (la letra: «el fondo es una pantalla
                //      negra… debería ser un fondo lejano lleno de
                //      tentáculos»). Antes el cuerpo vivía SOLO en la banda
                //      de montañas y el resto era el cielo de vanilla, NEGRO
                //      bajo el eclipse — el interior parecía una pantalla
                //      negra CORTADA. Ahora la CARNE DEL VACÍO (el gradiente
                //      profundo de la paleta de la casa) cubre TODA la
                //      pantalla DE BORDE A BORDE con un sobregiro de 120 px
                //      por lado que mata el corte en el borde aunque la
                //      matriz desplace unos píxeles — y encima nace el
                //      cuerpo entero de tentáculos.
                // ============================================================
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    Main.Rasterizer, null, m);
                try
                {
                    int franjas = 10;
                    float y0 = -120f, y1 = h + 120f;
                    for (int f = 0; f < franjas; f++)
                    {
                        float u0 = (float)f / franjas;
                        float u1 = (float)(f + 1) / franjas;
                        // la carne del vacío: 34,20,58 arriba → 12,6,22 abajo
                        Color franja = Color.Lerp(new Color(34, 20, 58), new Color(12, 6, 22), 0.5f + 0.5f * (u0 + u1) * 0.5f);
                        franja *= (0.88f + 0.10f * latido) * a;
                        float ya = y0 + (y1 - y0) * u0;
                        float yb = y0 + (y1 - y0) * u1 + 2f;
                        sb.Draw(Terraria.GameContent.TextureAssets.MagicPixel.Value,
                            new Rectangle(-120, (int)ya, (int)w + 240, (int)(yb - ya) + 2),
                            franja);
                    }
                }
                finally { VFXCore.CerrarLoteSiAbierto(); }

                // ============================================================
                //  (1) LA CAPA LEJANA — v6.121 — EL FONDO LLENO DE
                //      TENTÁCULOS: SEIS siluetas por PERIODO (900 px) —
                //      el doble que la v6.120 — con raíces MUY por encima
                //      de la pantalla (hasta -910) y cabezas que llegan
                //      HONDO (hasta el 82 % de la altura): la bóveda del
                //      dios CUBIERTA de cuerpo, sin huecos negros. El
                //      overscan de DOS periodos por lado mata el corte en
                //      el borde para siempre. El tinte SUBE (16,9,30 →
                //      46,28,84): lo lejano ahora SE LEE contra la carne —
                //      la vieja silueta casi-negra era INVISIBLE y el fondo
                //      parecía vacío.
                // ============================================================
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    Main.Rasterizer, null, m);
                try
                {
                    Color tinteLejos = new Color(46, 28, 84);
                    float perLejos = 900f;                     // el PERIODO del loop
                    float offLejos = Mod(camX * 0.012f, perLejos);
                    int repsLejos = (int)(w / perLejos) + 4;   // dos periodos de sobregiro por lado
                    for (int rep = -2; rep <= repsLejos - 2; rep++)
                    {
                        float x0 = rep * perLejos - offLejos;
                        for (int k = 0; k < SemillaLejos.Length && k < 6; k++)
                        {
                            int sem = SemillaLejos[k];
                            float fx = 0.06f + 0.155f * k + 0.05f * VFXCore.Hash01(sem, 1, 911);
                            Vector2 raiz = new Vector2(x0 + perLejos * fx, -720f - 190f * VFXCore.Hash01(sem, 2, 913));
                            Vector2 cabeza = raiz + new Vector2(
                                (VFXCore.Hash01(sem, 3, 917) - 0.5f) * 320f + MathF.Sin(t * 0.11f + k) * 46f,
                                h * (0.42f + 0.40f * VFXCore.Hash01(sem, 4, 919)) + 720f);
                            Vector2[] espina = SombrasLib.Columna(raiz, cabeza, t * 0.4f, sem, 16, 0.16f);
                            DibujarTira(sb, espina, 14f, 34f, tinteLejos,
                                (0.46f + 0.18f * latido) * a, flujo: t * 0.010f + k * 0.13f);
                        }
                    }

                    // EL POLVO ESTELAR DENSO de las capas lejanas (las SUAS
                    // del vacío — SparkleStar con parpadeo determinista).
                    // v6.121 — toda la altura de la pantalla (antes solo
                    // llegaba al 62 %: el tercio de abajo quedaba pelado).
                    for (int s = 0; s < 44; s++)
                    {
                        float sx = VFXCore.Hash01(s * 13 + 1, 0, 961) * w;
                        float sy = VFXCore.Hash01(s * 17 + 5, 0, 963) * h * 0.95f;
                        float pal = 0.5f + 0.5f * MathF.Sin(t * (0.4f + VFXCore.Hash01(s, 0, 967) * 1.1f) + s * 2.4f);
                        float tam = 7f + 9f * VFXCore.Hash01(s * 7 + 3, 0, 969);
                        sb.Draw(_chispa, new Vector2(sx, sy), null,
                            new Color(214, 190, 250) * ((0.07f + 0.15f * pal + 0.10f * latido) * a),
                            0f, _chispa.Size() * 0.5f, tam / 64f, SpriteEffects.None, 0f);
                    }
                }
                finally { VFXCore.CerrarLoteSiAbierto(); }

                // ============================================================
                //  (2) LAS BRUMAS DE PARALAJE (BrumaFX.MistBand ×3 — la
                //      receta textual de la librería: capas 0/1/2 con
                //      vientos distintos = PROFUNDIDAD) — entre lo lejano y
                //      lo medio, la niebla del cuerpo del dios.
                //
                //      v6.122 — LA CURA DEL FONDO NEGRO: el contrato de
                //      BrumaFX es «dibuja en el lote ABIERTO del llamador»
                //      y estas brumas corrían con el lote CERRADO (entre
                //      el finally de la capa lejana y el Begin de la capa
                //      media) → la primera QuadFrame lanzaba «Draw was
                //      called, but Begin has not yet been called» (el
                //      crash silencioso del client.log al ENTRAR al
                //      interior), el catch de Draw apagaba el cielo para
                //      siempre (_activo = false) y el mundo interior
                //      quedaba NEGRO de por vida. SU PROPIO LOTE con la
                //      matriz del paisaje — la lección v6.119 de RayoLib:
                //      el que dibuja, abre su lote.
                // ============================================================
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    Main.Rasterizer, null, m);
                try
                {
                    BrumaFX.MistBand(new Rectangle(-260, -160, (int)w + 520, (int)h + 320),
                        new Color(64, 42, 96), 5501, t, wind: 11f, layer: 0, alpha: 0.13f * a);
                    BrumaFX.MistBand(new Rectangle(-260, -160, (int)w + 520, (int)h + 320),
                        new Color(52, 32, 84), 5507, t, wind: 19f, layer: 1, alpha: 0.11f * a);
                }
                finally { VFXCore.CerrarLoteSiAbierto(); }

                // ============================================================
                //  (3) LA CAPA MEDIA — v6.120 — LA BANDA EN LOOP: TRES brazos
                //      con Piel del Eterno por PERIODO (760 px), repetidos sin
                //      costura — y sus OJOS QUE TE SIGUEN: el loop pone MÁS
                //      ojos mirándote (todos los del cuerpo, a la vez)
                // ============================================================
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    Main.Rasterizer, null, m);
                try
                {
                    Vector2 miradaJugador = Main.LocalPlayer != null
                        ? Main.LocalPlayer.Center - Main.screenPosition : new Vector2(w * 0.5f, h * 0.7f);
                    float perMedio = 760f;                      // el PERIODO del loop
                    float offMedio = Mod(camX * 0.035f, perMedio);
                    // v6.121 — CUATRO brazos por periodo (antes 3) y el
                    // overscan de dos periodos: el cuerpo medio también
                    // llena los bordes.
                    int repsMedio = (int)(w / perMedio) + 4;
                    for (int rep = -2; rep <= repsMedio - 2; rep++)
                    {
                        float x0 = rep * perMedio - offMedio;
                        for (int k = 0; k < SemillaMedio.Length && k < 4; k++)
                        {
                            int sem = SemillaMedio[k];
                            float fx = 0.10f + 0.24f * k + 0.05f * VFXCore.Hash01(sem, 1, 931);
                            Vector2 raiz = new Vector2(x0 + perMedio * fx, -520f - 120f * VFXCore.Hash01(sem, 2, 933));
                            Vector2 cabeza = raiz + new Vector2(
                                (VFXCore.Hash01(sem, 3, 937) - 0.5f) * 260f + MathF.Sin(t * 0.16f + k * 1.7f) * 60f * hincha,
                                h * (0.46f + 0.42f * VFXCore.Hash01(sem, 4, 939)) + 520f);
                            Vector2[] espina = SombrasLib.Columna(raiz, cabeza, t * 0.5f, sem, 18, 0.19f);
                            DibujarTira(sb, espina, 22f, 52f, new Color(150, 96, 210),
                                (0.50f + 0.16f * latido) * a, flujo: t * 0.020f + k * 0.21f);

                            // LOS OJOS DEL BRAZO (2 por brazo — reparto áureo):
                            // TODOS MIRAN al portador
                            for (int e = 0; e < 2; e++)
                            {
                                float ue = 0.30f + 0.40f * e;
                                int idx = (int)(ue * (espina.Length - 1));
                                DibujarOjo(sb, espina[idx], 16f + 7f * VFXCore.Hash01(sem, 5 + e, 941),
                                    miradaJugador, sem * 3 + e, (0.5f + 0.3f * latido) * a);
                            }
                        }
                    }
                }
                finally { VFXCore.CerrarLoteSiAbierto(); }

                // ============================================================
                //  (4) EL SOL DEL ECLIPSE «EN CAPAS»: el disco negro de
                //      vanilla (Sun3 prestado, clavado en el cenit) BRILLA
                //      entre las capas — aquí su ARO DE BRUMA (el abrazo);
                //      las capas cercanas pasarán por encima.
                //      v6.122 — SU PROPIO LOTE (mismo contrato de BrumaFX:
                //      el catch local tragaba el disparo del lote cerrado,
                //      pero el halo JAMÁS se dibujaba — ni crash ni sol).
                // ============================================================
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    Main.Rasterizer, null, m);
                try
                {
                    Vector2 sol = new Vector2(w * 0.5f, h * 0.16f);  // el cenit (time 27000)
                    float radio = 92f;
                    BrumaFX.Puff(sol, radio * 1.05f, new Color(96, 62, 158), 4711, t,
                        alpha: 0.34f * a, quality: 0.7f);
                    BrumaFX.Puff(sol + new Vector2(radio * 0.32f, -radio * 0.2f),
                        radio * 0.8f, new Color(20, 10, 34), 8231, t + 37f,
                        alpha: 0.30f * a, quality: 0.6f);
                    for (int i = 0; i < 5; i++)
                    {
                        float ang = t * 0.09f + i * MathHelper.TwoPi / 5f;
                        float r = radio * (1.12f + 0.30f * MathF.Sin(t * 0.4f + i * 2.1f));
                        Vector2 pos = sol + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.75f) * r;
                        BrumaFX.Puff(pos, radio * 0.55f, (i & 1) == 0
                            ? new Color(96, 62, 158) : new Color(20, 10, 34),
                            9100 + i * 13, t + i * 2.3f, alpha: 0.24f * a, quality: 0.5f);
                    }
                }
                catch { }
                finally { VFXCore.CerrarLoteSiAbierto(); }

                // ============================================================
                //  (5) LA CAPA CERCANA — v6.120 — LA BANDA EN LOOP: DOS
                //      TITANES verlet por PERIODO (640 px), repetidos sin
                //      costura (ColumnaViva — inercia y latigazo de verdad)
                //      con la Piel del Eterno a TAMAÑO DIOS, su BRILLO
                //      HÚMEDO aditivo (las vetas doradas ARDEN), ojos por
                //      todo el cuerpo y bruma en la cabeza. El loop los
                //      clona a lo ancho del cielo: EL DIOS ENTERO, en bucle.
                // ============================================================
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    Main.Rasterizer, null, m);
                try
                {
                    Vector2 miradaJugador = Main.LocalPlayer != null
                        ? Main.LocalPlayer.Center - Main.screenPosition : new Vector2(w * 0.5f, h * 0.7f);
                    float perCerca = 640f;                     // el PERIODO del loop
                    float offCerca = Mod(camX * 0.06f, perCerca);
                    // v6.121 — el overscan de dos periodos también en los
                    // titanes: ningun borde se queda sin dios.
                    int repsCerca = (int)(w / perCerca) + 4;
                    for (int rep = -2; rep <= repsCerca - 2; rep++)
                    {
                        float x0 = rep * perCerca - offCerca;
                        for (int k = 0; k < SemillaCerca.Length && k < 2; k++)
                        {
                            int sem = SemillaCerca[k];
                            float fx = 0.22f + 0.48f * k + 0.06f * VFXCore.Hash01(sem, 1, 951);
                            Vector2 raiz = new Vector2(x0 + perCerca * fx, -520f - 120f * VFXCore.Hash01(sem, 2, 953));
                            // LA CABEZA VIVA (Lissajous lenta — el verlet hace el resto)
                            Vector2 cabeza = raiz + new Vector2(
                                MathF.Sin(t * 0.14f + k * 2.2f) * 150f * hincha,
                                h * (0.38f + 0.24f * VFXCore.Hash01(sem, 4, 957)) +
                                MathF.Cos(t * 0.10f + k * 1.3f) * 70f * hincha);
                            Vector2[] espina = SombrasLib.ColumnaViva(raiz, cabeza, t, sem, 22, 0.24f, gancho: 0.42f);

                            // EL CUERPO (fusiforme: raíz 34 → vientre 104 → cabeza 66)
                            DibujarTira(sb, espina, 34f, 104f, new Color(168, 112, 224),
                                (0.78f + 0.14f * latido) * a, flujo: t * 0.030f + k * 0.29f,
                                grosorCentroAlt: 66f);

                            // LOS OJOS DEL TITÁN (4 por cuerpo — TODOS te miran,
                            // apertura escalonada, el latido los hincha)
                            for (int e = 0; e < 4; e++)
                            {
                                float ue = 0.22f + 0.20f * e;
                                int idx = (int)(ue * (espina.Length - 1));
                                DibujarOjo(sb, espina[idx], 24f + 9f * VFXCore.Hash01(sem, 6 + e, 959),
                                    miradaJugador, sem * 7 + e, (0.62f + 0.30f * latido) * a);
                            }
                        }
                    }
                }
                finally { VFXCore.CerrarLoteSiAbierto(); }

                // EL BRILLO HÚMEDO de los titanes (lote aditivo: las VETAS
                // DORADAS de la piel ARDEN — la misma tira ×1.35 de ancho,
                // EN EL MISMO LOOP de la capa)
                sb.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    Main.Rasterizer, null, m);
                try
                {
                    // el MISMO módulo del loop de la capa (mismo frame, mismo
                    // camX: las vetas caen EXACTAS sobre sus cuerpos) — con
                    // el MISMO overscan de la v6.121
                    float offBrillo = Mod(camX * 0.06f, 640f);
                    for (int rep = -2; rep <= (int)(w / 640f) + 2; rep++)
                    {
                        float x0 = rep * 640f - offBrillo;
                        for (int k = 0; k < SemillaCerca.Length && k < 2; k++)
                        {
                            int sem = SemillaCerca[k];
                            float fx = 0.22f + 0.48f * k + 0.06f * VFXCore.Hash01(sem, 1, 951);
                            Vector2 raiz = new Vector2(x0 + 640f * fx, -520f - 120f * VFXCore.Hash01(sem, 2, 953));
                            Vector2 cabeza = raiz + new Vector2(
                                MathF.Sin(t * 0.14f + k * 2.2f) * 150f * hincha,
                                h * (0.38f + 0.24f * VFXCore.Hash01(sem, 4, 957)) +
                                MathF.Cos(t * 0.10f + k * 1.3f) * 70f * hincha);
                            Vector2[] espina = SombrasLib.ColumnaViva(raiz, cabeza, t, sem, 22, 0.24f, gancho: 0.42f);
                            DibujarTira(sb, espina, 46f, 140f, new Color(150, 84, 235),
                                (0.10f + 0.10f * latido) * a, flujo: t * 0.030f + k * 0.29f,
                                grosorCentroAlt: 88f);
                        }
                    }
                    // LAS VOLUTAS entre titanes (BrumaFX.Tendril — la niebla
                    // serpentea entre los cuerpos del dios)
                    for (int k = 0; k < 2; k++)
                    {
                        float bx = w * (0.30f + 0.38f * k) - camX * 0.05f;
                        Vector2[] camino = new Vector2[5];
                        for (int p = 0; p < 5; p++)
                        {
                            camino[p] = new Vector2(
                                bx + MathF.Sin(t * 0.21f + p * 1.9f + k * 3.1f) * (90f + 40f * p),
                                h * (0.22f + 0.13f * p) + MathF.Cos(t * 0.16f + p * 1.3f + k) * 34f);
                        }
                        BrumaFX.Tendril(camino, 46f, new Color(84, 54, 140), 9700 + k * 7,
                            t, alpha: 0.20f * a);
                    }
                }
                finally { VFXCore.CerrarLoteSiAbierto(); }
            }
            finally
            {
                // === DEVOLVER EL LOTE DEL FONDO ABIERTO (el patrón del cielo
                //     de jefe — el End de vanilla lo cierra con naturalidad) ===
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

        // ==================================================================
        //  LA TIRA DE PIEL — el cuerpo del tentáculo del cielo: segmentos
        //  rotados solapados muestreando BANDAS DE V de la piel (la carne
        //  FLUYE: la textura es periódica y las bandas avanzan con el
        //  flujo) con perfil fusiforme (raíz → vientre → cabeza)
        // ==================================================================
        private static void DibujarTira(SpriteBatch sb, Vector2[] espina, float grosorRaiz,
            float grosorCentro, Color tinte, float alfa, float flujo, float grosorCentroAlt = -1f)
        {
            if (espina == null || espina.Length < 3 || alfa <= 0.01f) return;
            int n = espina.Length;
            for (int i = 0; i < n - 1; i++)
            {
                float u = i / (float)(n - 1);
                float u1 = (i + 1) / (float)(n - 1);
                Vector2 a = espina[i];
                Vector2 b = espina[i + 1];
                Vector2 mid = (a + b) * 0.5f;
                float len = Vector2.Distance(a, b) + 2.5f;      // el solape mata las juntas
                if (len < 2f) continue;
                float rot = (b - a).ToRotation() + MathHelper.PiOver2;

                // EL PERFIL FUSIFORME (fina → gorda → fina, como Masa)
                float wU = PerfilFusiforme(u);
                float wU1 = PerfilFusiforme(u1);
                float grosor = MathHelper.Lerp(grosorRaiz, grosorCentro, wU);
                float grosor1 = MathHelper.Lerp(grosorRaiz, grosorCentro, wU1);
                if (grosorCentroAlt > 0f)
                {
                    // el grosor del CENTRO declarado aparte (el vientre del titán)
                    grosor = MathHelper.Lerp(grosor, grosorCentroAlt, PerfilFusiforme(u) * 0.6f);
                    grosor1 = MathHelper.Lerp(grosor1, grosorCentroAlt, PerfilFusiforme(u1) * 0.6f);
                }

                // LAS BANDAS DE V (la carne fluye raíz→cabeza)
                float v0 = Frac01(u * 2.6f + flujo);
                float v1 = Frac01(u1 * 2.6f + flujo);
                if (v1 <= v0) v1 = v0 + 0.02f;                  // nunca hacia atrás
                int y0 = (int)(v0 * 510f);
                int alto = Math.Max(3, (int)((v1 - v0) * 510f));
                if (y0 + alto > 512) { y0 = 512 - alto; }
                var src = new Rectangle(0, y0, 128, alto);

                sb.Draw(_piel, mid, src, tinte * alfa, rot,
                    new Vector2(64f, alto * 0.5f),
                    new Vector2(grosor / 128f, len / alto),
                    SpriteEffects.None, 0f);
            }
        }

        /// <summary>El perfil fusiforme suave (0 en los extremos, 1 al centro).</summary>
        private static float PerfilFusiforme(float u)
        {
            float f = MathF.Sin(u * MathHelper.Pi);
            return f * f * (0.55f + 0.45f * f);
        }

        private static float Frac01(float x)
        {
            float f = x - MathF.Floor(x);
            return f < 0f ? 0f : (f > 0.999f ? 0.999f : f);
        }

        /// <summary>v6.120 — EL MÓDULO DEL LOOP: el resto positivo de
        /// x/m (el corazón del «tentaculos en loop» — el patrón del
        /// cuerpo VUELVE A EMPEZAR idéntico tras cada periodo, camines
        /// donde camines: sin costura, sin fin).</summary>
        private static float Mod(float x, float m)
        {
            float f = x % m;
            return f < 0f ? f + m : f;
        }

        // ==================================================================
        //  EL OJO DEL CUERPO — OjoAbismo girando hacia su presa (el
        //  parpadeo aplasta la escala Y; la apertura vive en la semilla)
        // ==================================================================
        private static void DibujarOjo(SpriteBatch sb, Vector2 pos, float tam, Vector2 presa, int semilla, float alfa)
        {
            if (alfa <= 0.02f || _ojo == null) return;
            // EL PARPADEO: cada ~7 s el ojo se cierra un instante
            float ciclo = Frac01(Main.GlobalTimeWrappedHourly / 7f + VFXCore.Hash01(semilla, 1, 971));
            float apertura = 1f;
            if (ciclo > 0.93f)
            {
                float f = (ciclo - 0.93f) / 0.07f;
                apertura = 1f - 0.97f * MathF.Sin(MathHelper.Pi * f);
            }
            if (apertura < 0.05f) return;

            // LA MIRADA: el ojo GIRA hacia el portador (±0.35 rad)
            Vector2 dir = presa - pos;
            float rot = dir.LengthSquared() > 4f
                ? MathF.Atan2(dir.Y, dir.X) * 0.22f
                : 0f;
            float late = 0.8f + 0.2f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.1f + semilla);

            sb.Draw(_ojo, pos, null, new Color(226, 200, 255) * (alfa * apertura * late),
                rot, _ojo.Size() * 0.5f, new Vector2(tam / 64f, (tam / 64f) * apertura),
                SpriteEffects.None, 0f);
            // el halo rojo-violeta del ojo (la lectura de noche)
            sb.Draw(_ojo, pos, null, new Color(150, 60, 200) * (alfa * apertura * 0.24f),
                rot, _ojo.Size() * 0.5f, new Vector2(tam * 1.7f / 64f, (tam * 1.7f / 64f) * apertura),
                SpriteEffects.None, 0f);
        }

        // ==================================================================
        //  LAS TEXTURAS (lazy + null-safe — la armadura de la casa)
        // ==================================================================
        private static bool AsegurarTexturas()
        {
            if (_texturasProbadas && _piel == null) return false;
            if (_piel != null && _ojo != null && _chispa != null) return true;
            try
            {
                if (_piel == null)
                    _piel = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/PielDelEterno", (AssetRequestMode)1).Value;
                if (_ojo == null)
                    _ojo = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/OjoAbismo", (AssetRequestMode)1).Value;
                if (_chispa == null)
                    _chispa = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/SparkleStar", (AssetRequestMode)1).Value;
                return _piel != null && _ojo != null && _chispa != null;
            }
            catch
            {
                _texturasProbadas = true;
                return false;
            }
        }
    }

    // ======================================================================
    //  EL REGISTRO + LA BRUMA DEL ASTRO (el abrazo del disco del eclipse)
    // ======================================================================
    public class CieloInteriorSistema : ModSystem
    {
        private static CieloInteriorEterno _cielo;

        public override void Load()
        {
            CieloInteriorEterno.Descargado = false;
            if (Main.dedServ) return;
            try
            {
                _cielo = new CieloInteriorEterno();
                SkyManager.Instance["AethonMod:CieloInteriorEterno"] = _cielo;
            }
            catch { }
            // LA BRUMA DEL ASTRO: el postfix de DrawSunAndMoon (la posición
            // EXACTA de vanilla — el mismo contrato de
            // AmbienteOleadaSistema.TintarAstros) abrazca el disco del
            // eclipse del interior con la niebla del grimorio.
            Terraria.On_Main.DrawSunAndMoon += BrumaDelSolInterior;
        }

        public override void Unload()
        {
            CieloInteriorEterno.Descargado = true;
            Terraria.On_Main.DrawSunAndMoon -= BrumaDelSolInterior;
            try { _cielo?.Reset(); }
            catch { }
            _cielo = null;
        }

        public override void OnWorldUnload()
        {
            if (Main.dedServ) return;
            try { SkyManager.Instance.Deactivate("AethonMod:CieloInteriorEterno"); }
            catch { }
            try { _cielo?.Reset(); }
            catch { }
        }

        /// <summary>
        /// EL ACTIVADOR: el cielo del cuerpo vive mientras el interior viva.
        /// </summary>
        public override void PostUpdateWorld()
        {
            if (Main.dedServ || CieloInteriorEterno.Descargado || Main.gameMenu) return;
            try
            {
                bool dentro = SubmundoGrimorio.Activo;
                if (dentro && (_cielo == null || !_cielo.ActivoInterno))
                {
                    SkyManager.Instance.Activate("AethonMod:CieloInteriorEterno");
                    // v6.121 — NACE PLENO: dentro del cuerpo del dios el
                    // fondo existe DESDE EL PRIMER FRAME (el fade de 2,4 s
                    // era la pantalla negra de la que se quejó la letra).
                    _cielo?.NacerPleno();
                }
                else if (!dentro && _cielo != null && _cielo.ActivoInterno &&
                         _cielo.AlfaInterno <= 0.02f)
                    SkyManager.Instance.Deactivate("AethonMod:CieloInteriorEterno");
            }
            catch { }
        }

        /// <summary>
        /// LA BRUMA DEL SOL DEL ECLIPSE: en el interior, el disco negro
        /// (Sun3 prestado, clavado en el cenit) vive ABIERTO por un aro de
        /// niebla violeta y negra que respira — el ABRAZO de las capas (el
        /// calco del DibujarBrumaDelAstro de AmbienteOleadaSistema, con la
        /// posición EXACTA del decompile).
        /// </summary>
        private static void BrumaDelSolInterior(Terraria.On_Main.orig_DrawSunAndMoon orig, Terraria.Main self,
            Main.SceneArea sceneArea, Color moonColor, Color sunColor, float tempMushroomInfluence)
        {
            orig(self, sceneArea, moonColor, sunColor, tempMushroomInfluence);
            if (!SubmundoGrimorio.Activo || Main.gameMenu || Main.netMode == NetmodeID.Server) return;
            try
            {
                // LA POSICIÓN del sol (decompile 81905-81926, EXACTO — el
                // interior clava time=27000: el cenit)
                Texture2D sol = Terraria.GameContent.TextureAssets.Sun.Value;
                int w = sol.Width;
                int num3 = (int)(Main.time / 54000.0 * (sceneArea.totalWidth + w * 2f)) - w;
                double parab = Math.Pow((Main.time / 54000.0 - 0.5) * 2.0, 2.0);   // time 27000 → 0
                int num4 = (int)(sceneArea.bgTopY + parab * 250.0 + 180.0);
                float escala = (float)(1.2 - parab * 0.4) * Main.ForcedMinimumZoom * 1.1f;
                Vector2 pos = new Vector2(num3, num4 + Main.sunModY) + sceneArea.SceneLocalScreenPositionOffset;

                float t = Main.GlobalTimeWrappedHourly;
                float radio = sol.Width * 0.5f * escala;

                // EL NÚCLEO morado que CUBRE el disco + EL VELO negro algo
                // descentrado + EL ANILLO de satélites que orbitan
                BrumaFX.Puff(pos, radio * 1.15f, new Color(96, 62, 158), 4711, t,
                    alpha: 0.50f, quality: 0.8f);
                BrumaFX.Puff(pos + new Vector2(radio * 0.3f, -radio * 0.18f),
                    radio * 0.9f, new Color(20, 10, 34), 8231, t + 37f,
                    alpha: 0.44f, quality: 0.7f);
                for (int i = 0; i < 5; i++)
                {
                    float ang = t * 0.10f + i * MathHelper.TwoPi / 5f;
                    float r = radio * (1.1f + 0.3f * MathF.Sin(t * 0.4f + i * 2.1f));
                    Vector2 p = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.75f) * r;
                    BrumaFX.Puff(p, radio * 0.6f, (i & 1) == 0
                        ? new Color(96, 62, 158) : new Color(20, 10, 34),
                        4700 + i * 11, t + i * 1.7f, alpha: 0.34f, quality: 0.5f);
                }
            }
            catch
            {
                // la armadura de siempre: el dibujo del cielo JAMÁS tumba el frame
            }
        }
    }
}
