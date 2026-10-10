using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Systems;
using AethonMod.Content.Players;
using AethonMod.Content.Globals;
using AethonMod.Content.Projectiles.Nervioso;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// v6.50.90 — EL TERCER ASIENTO CAMBIA DE DUEÑO: EL GRIMORIO NERVIOSO.
    /// La letra del usuario: «el tercer libro borralo, y crea otro con las
    /// mismas caracteristicas, temblor, glisheado, y nervioso». El
    /// Tembloroso (v6.50.87) quedó BORRADO — su escalofrío se hereda aquí,
    /// montado sobre el glitch del Errático y una capa nueva de NERVIOSISMO:
    ///
    /// · EL TEMBLOR del Tembloroso — la receta .87 INTACTA: los dos senos
    ///   incommensurables por eje (9,3/17,3 Hz en X, 11,9/19,1 Hz en Y,
    ///   fases 1,7/0,6). El escalofrío continuo del difunto.
    /// · EL GLITCH del Errático — la receta .88 INTACTA (el volumen que el
    ///   usuario aprobó: tiras 2-4 desfasándose hasta ±6 px, ráfagas de
    ///   12-24 ticks que se rehacen cada 2 frames, cada 15 s → 2,1 s con el
    ///   hambre) con máquina de estado PROPIA: el Nervioso rompe a SU hora
    ///   (v6.50.92: la familia es DOS libros — el Errático y el Inestable
    ///   quedaron BORRADOS, cada uno con su máquina).
    /// · EL NERVIOSO, en DOS capas: EL OJO ANSIOSO (revisa cada 2,4 s →
    ///   0,42 s, miradas que SALTAN a la meta, 1 de cada 3 al CENTRO y
    ///   dardos laterales) y EL SOBRESALTO (cada 6,5 s → 1,8 s, brinco de
    ///   1,2 → 2,2 px que se asienta en 7 ticks y, con hambre, ROMPE el
    ///   libro en una ráfaga corta).
    ///
    /// v6.50.91 — NO ES MIEDO (la letra del usuario: «no es que este
    /// nervioso por tener miedo, esta mas bien intranquilo, hambriento,
    /// enojado, inquieto, quiero comer y tiene hambre»). El temperamento se
    /// re-lee: el Nervioso es UN APETITO CON CUERPO — intranquilo, con
    /// hambre, enojado. v6.50.92 — LA FAMILIA SE ENCOGE A DOS (la letra:
    /// «borra al grimorio hambriento inestable y al erratico, deja el
    /// original y al nervioso»): quedan el normal SERENO y el Nervioso
    /// HAMBRIENTO (el Errático CAÓTICO y el Inestable POSEÍDO, difuntos).
    /// El ciclo vive en SU PROPIO EstadoGrimorio (la lección de la .86):
    /// los dos libros pasan hambre por separado.
    ///
    /// v6.50.91 — LAS DOS VIDAS DEL NERVIOSO (el umbral es el 50%):
    /// · DEBAJO DEL 50%: EL CAZADOR. Tranquilo y paciente — sin temblor, sin
    ///   glitch, sin sobresaltos (el ojo corre el ciclo SERENO del original,
    ///   NerviosoActivo() apagado). Si el jugador se queda QUIETO 5 s
    ///   (valor de PRUEBA), el libro SALE y flota encima de él MIRÁNDOLO
    ///   (GrimorioNerviosoFlotante), esperando a que algo SE MUEVA cerca
    ///   (≤480 px) — cualquier criatura hostil o no, MENOS los NPC de las
    ///   casas. Cuando pasa, del libro sale LA SOMBRA DE LA PÁGINA
    ///   (SombraPaginaCaza) y muerde: 10% de la vida MÁXIMA por golpe hasta
    ///   matarla y ABSORBERLA (AbsorberPresa: sin loot, sin gore, sin
    ///   crédito del jugador; la XP SÍ cobra) — cada presa baja 1% el
    ///   hambre (50% → 49%) y el libro CAZA HASTA EL 0% («saciado. Por
    ///   ahora.»). Mientras patrulla, EL RELOJ DEL HAMBRE SE CONGELA: sólo
    ///   las presas lo mueven.
    /// · DEL 50% EN ADELANTE: EL IMPACIENTE. «El libro se vuelve inquieto y
    ///   muy hambriento, comienza a decir que tiene hambre y es cuando
    ///   comienzan los efectos visuales a su sprite» — el ojo ansioso
    ///   despierta, el temblor, el glitch y los sobresaltos arrancan DE
    ///   CERO al 50% y escalan al máximo, el libro HABLA (FrasesDeHambre) y
    ///   comienzan las PROBABILIDADES de las OLEADAS DE HAMBRE (4% → 20%
    ///   cada 5 s, GrimorioFuriaSistema.Provocar natural).
    /// · AL 100%: EL FUGITIVO. «Cuando su hambre llegue a 100, hagamos que
    ///   escape del jugador algo así a como se usa El Codice Vivo» — el
    ///   libro se desprende y HUYE flotando (la levitación del difunto
    ///   Códice Vivo), con el ojo ROJO al máximo y el cuerpo roto en tiras,
    ///   hasta que lo alimenten o lo reinicien (clic derecho).
    /// </summary>
    public class GrimorioHambrientoNervioso : GrimorioHambriento
    {
        // === EL GLITCH — las perillas de la .88 (el volumen aprobado) ===
        // v6.50.91: el gate ahora se lee sobre HambreEfectos() — sin glitch
        // por debajo del 12% DE LA FASE IMPACIENTE (= 56% de hambre real)
        private const float HAMBRE_MIN_GLITCH = 0.12f;

        // === v6.50.91 — EL UMBRAL DE LA IMPACIENCIA (la letra del usuario):
        //     debajo del 50% el libro es EL CAZADOR (tranquilo, sale a
        //     cazar su propia comida); del 50% en adelante EL IMPACIENTE
        //     (efectos visuales + voz + probabilidades de oleadas) ===
        internal const float UMBRAL_IMPACIENCIA = 0.5f;
        // LA ESPERA: los segundos de quietud del jugador que sacan al libro
        // a cazar (5 s AHORA QUE ES UNA PRUEBA — la letra lo marcó como
        // valor de prueba; ajustable cuando el sistema esté listo)
        internal const int TICKS_QUIETUD_PRUEBA = 300;
        // LAS OLEADAS: la tirada de dados cada 5 s con hambre ≥ 50%
        private const int TICKS_ENTRE_OLEADAS = 300;

        private static readonly EstadoGrimorio _estadoNervioso = new EstadoGrimorio { Nervioso = true };
        protected override EstadoGrimorio Estado => _estadoNervioso;

        // === LA MÁQUINA (avanza UNA vez por tick — el glitch y el
        //     sobresalto comparten guard: las tres vías de dibujo los leen) ===
        private static uint _tickMaquina;
        private static int _tHastaRafaga = 120;    // 2 s hasta la primera
        private static int _tRafaga;               // >0: ticks que le quedan a la ráfaga
        private static uint _semilleRafaga;

        // === EL SOBRESALTO — el brinco del susto (7 ticks, se asienta) ===
        private static int _tHastaSobresalto = 300; // 5 s hasta el primero
        private static int _tSobresalto;            // >0: ticks que le quedan al salto
        private static Vector2 _dirSobresalto = Vector2.Zero;
        private static float _ampSobresalto;
        private const int DURACION_SOBRESALTO = 7;

        /// <summary>El hash determinista del layout (mismo tick = mismo glitch).</summary>
        private static uint Hash(uint x)
        {
            x ^= x >> 16; x *= 0x7FEB352Du;
            x ^= x >> 15; x *= 0x846CA68Bu;
            x ^= x >> 13;
            return x;
        }

        /// <summary>UN avance por tick: la ráfaga del glitch + el reloj del
        /// sobresalto (la firma del Nervioso — el apetito que rompe el
        /// libro). v6.50.91: los efectos leen HambreEfectos() — el umbral
        /// del 50% de la impaciencia está AQUÍ.</summary>
        internal static void PasoMaquina()
        {
            uint t = Main.GameUpdateCount;
            if (t == _tickMaquina)
                return;                       // 1 avance por tick
            _tickMaquina = t;

            float h = HambreEfectos();       // v6.50.91 — 0 debajo del 50%

            // --- EL GLITCH (la máquina de la .88, intacta) ---
            if (_tRafaga > 0)
            {
                if (--_tRafaga == 0)
                {
                    // la próxima ráfaga: 15 s → 2,1 s con el hambre (±25 %)
                    _tHastaRafaga = (int)(60f * MathHelper.Lerp(15f, 2.1f, h)
                        * (0.75f + 0.5f * (Hash(t) & 255u) / 255f));
                }
            }
            else if (--_tHastaRafaga <= 0)
            {
                if (h >= HAMBRE_MIN_GLITCH)
                {
                    // 12-24 ticks — la ráfaga viva de la .88
                    _tRafaga = 12 + (int)(Hash(t ^ 0x5BD1E995u) % 13u);
                    _semilleRafaga = t * 2654435761u;
                }
                else
                    _tHastaRafaga = 60;       // aún sin hambre: reintentar en 1 s
            }

            // --- EL SOBRESALTO (lo nuevo de la .90) ---
            if (_tSobresalto > 0)
            {
                _tSobresalto--;
            }
            else if (--_tHastaSobresalto <= 0)
            {
                // v6.50.91 — EL CAZADOR NO BRINCA: debajo del umbral de la
                // impaciencia no hay sobresaltos (tranquilo, vigilando)
                if (h <= 0f)
                {
                    _tHastaSobresalto = 60;   // reintentar en 1 s
                }
                else
                {
                    // el reloj del susto: 6,5 s → 1,8 s con el hambre (±25 %)
                    _tHastaSobresalto = (int)(60f * MathHelper.Lerp(6.5f, 1.8f, h)
                        * (0.75f + 0.5f * (Hash(t ^ 0x68E31DA4u) & 255u) / 255f));
                    _tSobresalto = DURACION_SOBRESALTO;
                    _ampSobresalto = MathHelper.Lerp(1.2f, 2.2f, h);
                    double ang = Main.rand.NextDouble() * Math.PI * 2.0;
                    _dirSobresalto = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang));

                    // EL HAMBRE ROMPE EL LIBRO: con hambre, cada brinco trae
                    // una ráfaga corta (8-14 ticks) — brinca Y se rompe
                    if (h >= HAMBRE_MIN_GLITCH && _tRafaga <= 0)
                    {
                        _tRafaga = 8 + (int)(Hash(t ^ 0x2545F491u) % 7u);
                        _semilleRafaga = t * 2654435761u;
                    }
                }
            }
        }

        /// <summary>EL SOBRESALTO de este tick (px de juego, unidades del
        /// libro): el salto entero en el primer frame, asentándose lineal
        /// en 7 ticks — un brinco, no un temblor.</summary>
        private static Vector2 Sobresalto() =>
            _tSobresalto > 0
                ? _dirSobresalto * (_ampSobresalto * (_tSobresalto / (float)DURACION_SOBRESALTO))
                : Vector2.Zero;

        internal struct Tira { public int Y0, Y1; public float Off; }

        /// <summary>EL LAYOUT de este tick (null = libro entero, sin ráfaga).</summary>
        internal static Tira[] Layout(out float offOjo)
        {
            offOjo = 0f;
            if (_tRafaga <= 0)
                return null;

            // las tiras se REGENERAN cada 2 frames — el tearing vivo
            uint fase = Main.GameUpdateCount / 2u;
            uint sd = Hash(_semilleRafaga ^ (fase * 0x9E3779B9u));
            float h = HambreEfectos();   // v6.50.91 — el desfase también nace al 50%

            int n = 2 + (int)(Hash(sd) % 3u);            // 2-4 tiras
            var tiras = new Tira[n];
            int hechos = 0;
            int y = 3 + (int)(Hash(sd ^ 0xA5A5u) % 22u);
            while (hechos < n && y <= 41)
            {
                int alto = 4 + (int)(Hash(sd ^ (uint)(hechos * 97 + 1)) % 9u);      // 4-12 px
                float off = ((int)(Hash(sd ^ (uint)(hechos * 131 + 3)) % 13u) - 6)
                    * (0.55f + 0.45f * h);                                          // ±3,3 → ±6 px
                tiras[hechos++] = new Tira { Y0 = y, Y1 = Math.Min(46, y + alto), Off = off };
                y = tiras[hechos - 1].Y1 + 1 + (int)(Hash(sd ^ (uint)(hechos * 17 + 5)) % 5u);
            }
            if (hechos < n)
                Array.Resize(ref tiras, hechos);

            // el iris VIAJA CON SU TIRA (el ojo nunca se desprende del libro)
            for (int i = 0; i < tiras.Length; i++)
                if (OJO.Y >= tiras[i].Y0 && OJO.Y < tiras[i].Y1)
                { offOjo = tiras[i].Off; break; }
            return tiras;
        }

        /// <summary>Dibuja la textura ENTERA o en TIRAS — inventario, mundo y
        /// el espíritu flotante (cada franja a su lugar natural + su desfase,
        /// con la MISMA transform del libro; sin ráfaga es UN draw).</summary>
        internal static void DibujarTiras(SpriteBatch sb, Texture2D tex, Vector2 position, Rectangle frame,
            Color color, float rotation, Vector2 origin, float scale, Tira[] tiras)
        {
            if (tiras == null || tiras.Length == 0)
            {
                sb.Draw(tex, position, frame, color, rotation, origin, scale, SpriteEffects.None, 0f);
                return;
            }
            int cursor = 0;
            for (int i = 0; i <= tiras.Length; i++)
            {
                int y0 = i < tiras.Length ? tiras[i].Y0 : frame.Height;
                if (y0 > cursor)
                    Franja(sb, tex, position, frame, color, rotation, origin, scale, cursor, y0, 0f);
                if (i < tiras.Length)
                {
                    Franja(sb, tex, position, frame, color, rotation, origin, scale,
                        tiras[i].Y0, tiras[i].Y1, tiras[i].Off);
                    if (tiras[i].Y1 > cursor)
                        cursor = tiras[i].Y1;
                }
            }
        }

        // el texel (0,a) a su lugar natural + el desfase — ROTADO con el libro
        private static void Franja(SpriteBatch sb, Texture2D tex, Vector2 position, Rectangle frame,
            Color color, float rotation, Vector2 origin, float scale, int a, int b, float off)
        {
            if (b <= a)
                return;
            var src = new Rectangle(frame.X, frame.Y + a, frame.Width, b - a);
            Vector2 pos = position + new Vector2(off - origin.X, a - origin.Y) * scale;
            if (rotation != 0f)
                pos = position + (new Vector2(off - origin.X, a - origin.Y) * scale).RotatedBy(rotation);
            sb.Draw(tex, pos, src, color, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        /// <summary>La versión en DrawData de las tiras (la mano) — MISMA
        /// transform del held item. Con la gravedad invertida la franja ancla
        /// por su borde INFERIOR (la lección del espejo de la v6.50.84).</summary>
        private static void AgregarTiras(List<DrawData> cache, Texture2D tex,
            DrawData proto, Rectangle fr, bool espejoY, Tira[] tiras)
        {
            if (tiras == null || tiras.Length == 0)
            {
                cache.Add(new DrawData(tex, proto.position, proto.sourceRect, proto.color,
                    proto.rotation, proto.origin, proto.scale, proto.effect));
                return;
            }
            int cursor = 0;
            for (int i = 0; i <= tiras.Length; i++)
            {
                int y0 = i < tiras.Length ? tiras[i].Y0 : fr.Height;
                if (y0 > cursor)
                    AgregarFranja(cache, tex, proto, fr, espejoY, cursor, y0, 0f);
                if (i < tiras.Length)
                {
                    AgregarFranja(cache, tex, proto, fr, espejoY, tiras[i].Y0, tiras[i].Y1, tiras[i].Off);
                    if (tiras[i].Y1 > cursor)
                        cursor = tiras[i].Y1;
                }
            }
        }

        private static void AgregarFranja(List<DrawData> cache, Texture2D tex,
            DrawData proto, Rectangle fr, bool espejoY, int a, int b, float off)
        {
            if (b <= a)
                return;
            var src = new Rectangle(fr.X, fr.Y + a, fr.Width, b - a);
            float ay = espejoY ? fr.Height - b - proto.origin.Y : a - proto.origin.Y;
            Vector2 pos = proto.position + new Vector2(
                (off - proto.origin.X) * proto.scale.X,
                ay * proto.scale.Y).RotatedBy(proto.rotation);
            cache.Add(new DrawData(tex, pos, src, proto.color, proto.rotation,
                Vector2.Zero, proto.scale, proto.effect));
        }

        // =================================================================
        // EL TEMBLOR (la receta del Tembloroso .87 — leyendo SU hambre).
        // =================================================================
        /// <summary>EL TEMBLOR de este tick (px de juego, en unidades del
        /// libro — el llamador lo escala a su contexto). Determinista puro:
        /// una función del tick, sin estado.</summary>
        internal static Vector2 Temblor()
        {
            float t = Main.GameUpdateCount / 60f;
            // v6.50.91 — EL UMBRAL DEL 50%: el CAZADOR no tiembla; de la
            // impaciencia en adelante la amplitud nace de CERO y llega a
            // los 1,3 px del difunto Tembloroso al 100% de hambre
            float he = HambreEfectos();
            float amp = (0.20f + 1.10f * he) * he;   // 0 px → 1,3 px (desde el 50%)
            float x = (float)Math.Sin(t * 9.3f * MathHelper.TwoPi) * 0.80f
                    + (float)Math.Sin(t * 17.3f * MathHelper.TwoPi) * 0.25f;
            float y = (float)Math.Sin(t * 11.9f * MathHelper.TwoPi + 1.7f) * 0.80f
                    + (float)Math.Sin(t * 19.1f * MathHelper.TwoPi + 0.6f) * 0.25f;
            return new Vector2(x, y) * amp;
        }

        /// <summary>EL CORRIMIENTO DEL CUERPO: temblor + sobresalto (px de
        /// juego, unidades del libro — el escalofrío continuo y, cuando el
        /// susto lo brinca, el salto encima). El ojo y las tiras viajan con
        /// él: nunca desalineados del cuerpo.</summary>
        internal static Vector2 Corrimiento() => Temblor() + Sobresalto();

        // =================================================================
        // INVENTARIO — el libro entero lo dibuja la copia (PreDraw false):
        // vanilla dibujaría el libro fijo; aquí viene corrido, temblando y
        /// brincando, y roto en tiras cuando la ráfaga (o el susto) lo pide.
        // =================================================================
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
            => false;

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            PasoMaquina();
            Tira[] tiras = Layout(out float offOjo);
            Vector2 corr = Corrimiento() * scale;

            // el cursor lo sigue el ojo en su CASA (con el corrimiento del
            // cuerpo — el ojo viaja con el libro, nunca desalineado)
            Estado.PosOjoPantalla = position + corr + (OJO - origin) * scale;
            Estado.PosValida = true;

            // EL LIBRO — entero sin ráfaga, en tiras con ella, TEMBLANDO
            DibujarTiras(spriteBatch, ModContent.Request<Texture2D>(Texture).Value,
                position + corr, frame, drawColor, 0f, origin, scale, tiras);

            if (Estado.Fase > 0)
            {
                // el párpado se ROMPE y TIEMBLA con el libro (el mismo layout)
                bool medio = Estado.Fase > 7 || Estado.Fase < 4;
                DibujarTiras(spriteBatch, Capa(medio ? "Medio" : "Cerrado"),
                    position + corr, frame, drawColor, 0f, origin, scale, tiras);
                return;
            }

            // EL IRIS — viaja con su tira y tiembla/brinca con el libro
            var iris = Capa("Iris");
            var irisRojo = Capa("IrisRojo");
            Vector2 posIris = position + corr + (OJO + Estado.DespActual - origin) * scale
                + new Vector2(offOjo * scale, 0f);
            spriteBatch.Draw(iris, posIris, null, drawColor, 0f, iris.Size() * 0.5f,
                scale * IRIS_ESC, SpriteEffects.None, 0f);
            float rojo = Estado.NivelRojo();
            if (rojo > 0f)
                spriteBatch.Draw(irisRojo, posIris, null, Alfa(rojo), 0f,
                    irisRojo.Size() * 0.5f, scale * IRIS_ESC, SpriteEffects.None, 0f);
        }

        // =================================================================
        // MUNDO — la convención vanilla v6.50.84 (pivote-centro + la
        // rotación de vuelo): el libro en tiras, corrido por temblor y susto.
        // =================================================================
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, ref float rotation, ref float scale, int whoAmI)
            => false;

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, float rotation, float scale, int whoAmI)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            PasoMaquina();
            Tira[] tiras = Layout(out float offOjo);

            Vector2 pivote = PivoteEnMundo(out var frame, out var origen);
            Vector2 corr = Corrimiento() * scale;

            // la CASA del ojo (con el corrimiento — el ojo viaja con el cuerpo)
            Vector2 alOjo = (OJO + Estado.DespActual - origen) * scale;
            Vector2 casa = pivote + corr + alOjo.RotatedBy(rotation);
            Estado.PosOjoPantalla = casa;
            Estado.PosValida = true;

            DibujarTiras(spriteBatch, ModContent.Request<Texture2D>(Texture).Value,
                pivote + corr, frame, lightColor, rotation, origen, scale, tiras);

            if (Estado.Fase > 0)
            {
                bool medio = Estado.Fase > 7 || Estado.Fase < 4;
                DibujarTiras(spriteBatch, Capa(medio ? "Medio" : "Cerrado"),
                    pivote + corr, frame, lightColor, rotation, origen, scale, tiras);
                return;
            }

            // el iris gira con el libro, viaja con su tira y con el cuerpo
            Vector2 posOjo = casa + new Vector2(offOjo * scale, 0f).RotatedBy(rotation);
            var iris = Capa("Iris");
            var irisRojo = Capa("IrisRojo");
            spriteBatch.Draw(iris, posOjo, null, lightColor, rotation, iris.Size() * 0.5f,
                scale * IRIS_ESC, SpriteEffects.None, 0f);
            float rojo = Estado.NivelRojo();
            if (rojo > 0f)
                spriteBatch.Draw(irisRojo, posOjo, null, Alfa(rojo), rotation,
                    irisRojo.Size() * 0.5f, scale * IRIS_ESC, SpriteEffects.None, 0f);
        }

        // =================================================================
        // MANO — el corrimiento (temblor + sobresalto) se INYECTA en la
        // DrawData del libro, el glitch son tiras en el cache (v6.50.84).
        // =================================================================
        public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData,
            ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
        {
            if (Main.netMode == NetmodeID.Server)
                return base.ModifyItemDraw(ref drawInfo, ref drawData, ref coloredDrawData, ref glowMaskDrawData);

            PasoMaquina();
            Tira[] tiras = Layout(out float offOjo);

            // EL CUERPO: temblor + sobresalto entran en la posición del libro
            drawData.position += Corrimiento() * Math.Abs(drawData.scale.X);

            bool espejoX = (drawData.effect & SpriteEffects.FlipHorizontally) != 0;
            bool espejoY = (drawData.effect & SpriteEffects.FlipVertically) != 0;
            Rectangle fr = drawData.sourceRect ?? new Rectangle(0, 0, 36, 49);

            AgregarTiras(drawInfo.DrawDataCache, ModContent.Request<Texture2D>(Texture).Value,
                drawData, fr, espejoY, tiras);
            if (coloredDrawData.HasValue)
                drawInfo.DrawDataCache.Add(coloredDrawData.Value);
            if (glowMaskDrawData.HasValue)
                drawInfo.DrawDataCache.Add(glowMaskDrawData.Value);

            if (Estado.Fase > 0)
            {
                bool medio = Estado.Fase > 7 || Estado.Fase < 4;
                AgregarTiras(drawInfo.DrawDataCache, Capa(medio ? "Medio" : "Cerrado"),
                    drawData, fr, espejoY, tiras);
                return false;
            }

            // el iris: el texel del ojo con el espejo (v6.50.84) + su tira
            float rojo = Estado.NivelRojo();
            Vector2 texel = OJO + new Vector2(
                Estado.DespActual.X * (espejoX ? -1f : 1f),
                Estado.DespActual.Y * (espejoY ? -1f : 1f));
            Vector2 q = new Vector2(
                (espejoX ? fr.Width - texel.X : texel.X) - drawData.origin.X,
                (espejoY ? fr.Height - texel.Y : texel.Y) - drawData.origin.Y);
            Vector2 casa = drawData.position + new Vector2(
                q.X * drawData.scale.X, q.Y * drawData.scale.Y).RotatedBy(drawData.rotation);
            Vector2 posOjo = casa + new Vector2(offOjo * drawData.scale.X, 0f).RotatedBy(drawData.rotation);

            var iris = Capa("Iris");
            var irisRojo = Capa("IrisRojo");
            drawInfo.DrawDataCache.Add(new DrawData(iris, posOjo, null, drawData.color,
                drawData.rotation, iris.Size() * 0.5f, drawData.scale * IRIS_ESC, drawData.effect));
            if (rojo > 0f)
                drawInfo.DrawDataCache.Add(new DrawData(irisRojo, posOjo, null, Alfa(rojo),
                    drawData.rotation, irisRojo.Size() * 0.5f, drawData.scale * IRIS_ESC,
                    drawData.effect));

            if (drawInfo.drawPlayer.whoAmI == Main.myPlayer)
            {
                Estado.PosOjoPantalla = casa;
                Estado.PosValida = true;
            }
            return false;
        }

        // =================================================================
        // v6.50.91 — LAS DOS VIDAS DEL NERVIOSO. El controlador vive aquí
        // (es SU hambre la que manda): la quietud del jugador, la salida a
        // caza, la impaciencia con voz, las oleadas probables y la fuga.
        // La CAZA toca NPCs: corre SOLO donde el jugador local es la
        // AUTORIDAD (SP o anfitrión de un listen server — el mismo contrato
        // del resto de la demo).
        // =================================================================

        /// <summary>EL APETITO DE EFECTOS: 0 debajo del umbral (el CAZADOR
        /// está tranquilo), 0→1 del 50% al 100% (la fase IMPACIENTE).</summary>
        internal static float HambreEfectos() =>
            Math.Max(0f, Math.Min(1f, (_estadoNervioso.Hambre - UMBRAL_IMPACIENCIA) * 2f));

        // LA CAZA: la quietud contada del jugador
        private static int _ticksQuieto;
        // LA VOZ: el frío entre frases del hambre
        private static int _ticksHastaFrase = 420;
        // EL GUARD DEL TICK: un PasoCaza por tick, TODAS las copias juntas
        private static uint _tickPasoCaza;

        /// <summary>El estado compartido del Nervioso (lo leen el grimorio
        /// flotante y su sombra para saber de qué hambre viven).</summary>
        internal static EstadoGrimorio EstadoCompartido => _estadoNervioso;

        /// <summary>La geometría del ojo y la escala del iris (las capas del
        /// libro, para el espíritu flotante — mismo socket, misma familia).</summary>
        internal static Vector2 OjoTexel => OJO;
        internal static float IrisEscala => IRIS_ESC;
        internal static Color AlfaCapa(float a) => Alfa(a);

        /// <summary>¿El espíritu del libro está FUERA? (caza o fuga).</summary>
        internal static bool LibroFuera(Player jugador)
        {
            int tipo = ModContent.ProjectileType<GrimorioNerviosoFlotante>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.type == tipo && p.owner == jugador.whoAmI)
                    return true;
            }
            return false;
        }

        /// <summary>¿Está fuera EN PATRULLA DE CAZA? (modo caza — mientras
        /// esto sea verdad, el reloj del hambre se congela).</summary>
        internal static bool CaceriaActiva()
        {
            int tipo = ModContent.ProjectileType<GrimorioNerviosoFlotante>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.type == tipo && p.owner == Main.myPlayer &&
                    p.ai[0] == GrimorioNerviosoFlotante.MODO_CAZA)
                    return true;
            }
            return false;
        }

        /// <summary>v6.50.92 — ¿DÓNDE ESTÁ LA COMIDA? Mientras la sombra
        /// devora a una presa, éste es el punto que el libro MIRA (la
        /// letra del usuario: «el libro debe mirar lo que esta comiendo»):
        /// la presa viva mientras la muerde, el cuerpo de la sombra
        /// mientras las almas suben al libro. Null = no hay festín.</summary>
        internal static Vector2? FocoDelFestin(int dueño)
        {
            int tipo = ModContent.ProjectileType<SombraPaginaCaza>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile pr = Main.projectile[i];
                if (pr == null || !pr.active || pr.type != tipo || pr.owner != dueño)
                    continue;
                int idx = (int)pr.ai[0] - 1;
                NPC presa = idx >= 0 && idx < Main.maxNPCs ? Main.npc[idx] : null;
                if (presa != null && presa.active && presa.life > 0)
                    return presa.Center;      // la está MORDIENDO: mirarLA
                return pr.Center;             // la está ABSORBIENDO: mirar el festín
            }
            return null;
        }

        /// <summary>EL PASO DE LAS DOS VIDAS — una vez por tick desde
        /// UpdateInventory (el guard del tick vale por TODAS las copias del
        /// Nervioso en el inventario: la quietud se cuenta UNA vez). La VOZ
        /// suena en la pantalla del dueño; la caza, las oleadas y la fuga
        /// sólo donde el jugador local MANDA.</summary>
        private static void PasoCaza(Player player)
        {
            // otros jugadores: la demo del Nervioso es del dueño LOCAL (el
            // libro no caza por proxy — y así el guard del tick jamás se lo
            // roba otra copia ajena)
            bool local = player.whoAmI == Main.myPlayer &&
                (Main.netMode != NetmodeID.Server || !Main.dedServ);
            if (!local)
                return;

            if (Main.GameUpdateCount == _tickPasoCaza)
                return;                       // 1 avance por tick, todas las copias
            _tickPasoCaza = Main.GameUpdateCount;

            float h = _estadoNervioso.Hambre;

            // === LA VOZ DEL HAMBRE — «comienza a decir que tiene hambre»:
            //     suena en la pantalla del dueño, en cualquier máquina ===
            FrasesDeHambre(h);

            // === v6.50.92 — EL FOCO DEL FESTÍN: mientras la sombra devora,
            //     el ojo del libro (hotbar, mano, mundo) se clava en SU
            //     COMIDA — la letra: «el libro debe mirar lo que esta
            //     comiendo». Visual puro: también en el cliente MP (la
            //     sombra y los NPC llegan sincronizados) ===
            _estadoNervioso.FocoPantalla = FocoDelFestin(player.whoAmI) is Vector2 festin
                ? festin - Main.screenPosition
                : (Vector2?)null;

            // === LA AUTORIDAD: SP o anfitrión de un listen server (el
            //     cliente MP sólo oye a su libro — no toca NPCs) ===
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            bool fuera = LibroFuera(player);
            if (fuera)
                _ticksQuieto = 0;

            if (!fuera)
            {
                // LA SALIDA A CAZA: apetito (0–50%) + jugador QUIETO 5 s
                if (h > 0f && h < UMBRAL_IMPACIENCIA)
                {
                    bool quieto = !player.dead && player.velocity.LengthSquared() < 0.02f;
                    _ticksQuieto = quieto ? _ticksQuieto + 1 : 0;
                    if (_ticksQuieto >= TICKS_QUIETUD_PRUEBA)
                    {
                        _ticksQuieto = 0;
                        Projectile.NewProjectile(player.GetSource_Misc("NerviosoCaza"),
                            player.Center - new Vector2(0f, 60f), Vector2.Zero,
                            ModContent.ProjectileType<GrimorioNerviosoFlotante>(),
                            0, 0f, player.whoAmI, GrimorioNerviosoFlotante.MODO_CAZA, 0f, 0f);
                        DecirLocal("SaleACazar", new Color(206, 108, 96));
                        try
                        {
                            Terraria.Audio.SoundEngine.PlaySound(
                                SoundID.Item122.WithPitchOffset(-0.2f).WithVolumeScale(0.6f),
                                player.Center);
                        }
                        catch { }
                    }
                }

                // LA FUGA: hambre al MÁXIMO — «que escape del jugador»
                if (h >= 1f)
                {
                    Projectile.NewProjectile(player.GetSource_Misc("NerviosoFuga"),
                        player.Center + new Vector2(Main.rand.NextFloat(-80f, 80f), -90f), Vector2.Zero,
                        ModContent.ProjectileType<GrimorioNerviosoFlotante>(),
                        0, 0f, player.whoAmI, GrimorioNerviosoFlotante.MODO_FUGA, 0f, 0f);
                    DecirLocal("Escapa", new Color(235, 70, 55));
                }
            }

            // === LAS OLEADAS DE HAMBRE — «es donde comienzan las
            //     probabilidades de que las oleadas de hambre se activen» ===
            if (h >= UMBRAL_IMPACIENCIA &&
                ((Main.GameUpdateCount + (ulong)player.whoAmI * 53ul) % (ulong)TICKS_ENTRE_OLEADAS) == 0ul)
            {
                var config = ModContent.GetInstance<Content.AethonConfigServidor>();
                if ((config == null || config.EventoHambreGrimorio) &&
                    !GrimorioFuriaSistema.Activo && GrimorioFuriaSistema.MundoLibre())
                {
                    // 4% al 50% → 20% al 100%, la tirada cada 5 s
                    float urgencia = (h - UMBRAL_IMPACIENCIA) / (1f - UMBRAL_IMPACIENCIA);
                    if (Main.rand.NextFloat() < 0.04f + 0.16f * urgencia)
                    {
                        int nivel = Math.Max(1, Math.Min(10,
                            player.GetModPlayer<ShardPlayer>().FuriaNivel));
                        GrimorioFuriaSistema.Provocar(player, nivel, natural: true);
                    }
                }
            }
        }

        /// <summary>LA VOZ DEL NERVIOSO — intranquilo, hambriento, enojado
        /// («quiero comer y tiene hambre»): del 50% en adelante el libro
        /// EMPIEZA A DECIRLO, cada vez más seguido y más desesperado.</summary>
        private static void FrasesDeHambre(float h)
        {
            if (h < UMBRAL_IMPACIENCIA)
            {
                _ticksHastaFrase = Math.Min(_ticksHastaFrase, 300);
                return;
            }
            if (--_ticksHastaFrase > 0)
                return;

            float urgencia = (h - UMBRAL_IMPACIENCIA) / (1f - UMBRAL_IMPACIENCIA);
            _ticksHastaFrase = (int)(60f * MathHelper.Lerp(9f, 3.5f, urgencia));

            string clave = urgencia < 0.35f ? "Frase" + (1 + Main.rand.Next(2))
                : urgencia < 0.75f ? "Frase" + (3 + Main.rand.Next(2))
                : "Frase5";
            DecirLocal(clave, new Color(206, 108, 96));
        }

        /// <summary>EL LIBRO HABLA — en la pantalla de SU dueño.</summary>
        internal static void DecirLocal(string clave, Color color)
        {
            if (Main.netMode == NetmodeID.Server && Main.dedServ)
                return;
            try
            {
                Main.NewText("«" + Language.GetTextValue("Mods.AethonMod.Hambre.Nervioso." + clave) + "»",
                    color);
            }
            catch { }
        }

        /// <summary>LA ABSORCIÓN (v6.50.91) — la presa muere COMIDA por el
        /// libro: «esta muerte no cuenta como que la hizo el jugador y la
        /// criatura no deja loot, pero si cuenta como exp ya que el mismo
        /// libro la mato». Sin checkDead: la criatura se APAGA bajo la
        /// sombra (sin loot, sin gore, sin crédito) y el hambre baja 1%
        /// (50% → 49%). La llama la sombra (SombraPaginaCaza) en la
        /// AUTORIDAD con el último mordisco.</summary>
        internal static void AbsorberPresa(NPC presa, Player dueño)
        {
            if (presa == null || !presa.active)
                return;

            // LA CADENA COMPLETA (jefes multi-segmento: el Devorador entero)
            int real = presa.realLife >= 0 && presa.realLife != presa.whoAmI ? presa.realLife : presa.whoAmI;
            NPC principal = Main.npc[real];
            NPC paraXP = principal != null && principal.active ? principal : presa;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active)
                    continue;
                bool deLaCadena = n.whoAmI == real || n.realLife == real
                    || (presa.realLife >= 0 && (n.whoAmI == presa.realLife || n.realLife == presa.realLife));
                if (!deLaCadena)
                    continue;
                n.life = 0;
                n.active = false;
                n.netSkip = -1;
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n.whoAmI);
            }

            // EL APETITO: −1% por presa (la letra: 50% → 49%)
            _estadoNervioso.Hambre = Math.Max(0f, _estadoNervioso.Hambre - 0.01f);

            // v6.50.92 — LA COMIDA MARCA LA HORA (la letra: «el hambre debe
            // subir si es que en 10 segundos no come nada»): este bocado
            // congela el reloj del apetito 10 s — el libro acaba de comer.
            _estadoNervioso.UltimaComida = Main.GameUpdateCount;

            // LA XP — el pipeline de siempre (el Grimorio del Eterno visible
            // cobra: la muerte no es del jugador, pero el libro sí comió)
            CobrarXPLibro(dueño, paraXP);

            // el grito silencioso del apetito satisfecho
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                try
                {
                    CombatText.NewText(presa.Hitbox, new Color(235, 70, 55),
                        Language.GetTextValue("Mods.AethonMod.Hambre.Nervioso.Absorbe"));
                }
                catch { }
            }
        }

        /// <summary>LA XP DE LA PRESA — el mismo cobro del pipeline de la
        /// casa (GlobalNPCXP.OnKill), sin FindKiller: el que come es EL
        /// LIBRO, y cobra al Grimorio del Eterno de la barra rápida.</summary>
        private static void CobrarXPLibro(Player dueño, NPC presa)
        {
            try
            {
                int nivelGrimorio = 0;
                bool libroVisible = false;
                for (int i = 0; i < 10; i++)
                {
                    Item inv = dueño.inventory[i];
                    if (inv == null || inv.type != ModContent.ItemType<GrimoireEternal>())
                        continue;
                    if (!libroVisible)
                    {
                        var slx = inv.GetGlobalItem<ShardLevelItem>();
                        if (slx != null)
                            nivelGrimorio = slx.Level;
                        libroVisible = true;
                    }
                }
                if (!libroVisible)
                    return;

                int xp = ShardLevelSystem.ApplyXPMultiplier(
                    ShardLevelSystem.XPForNPC(presa, nivelGrimorio));
                if (xp <= 0)
                    return;

                for (int i = 0; i < 10; i++)
                {
                    Item inv = dueño.inventory[i];
                    if (inv == null || inv.type != ModContent.ItemType<GrimoireEternal>())
                        continue;
                    var sl = inv.GetGlobalItem<ShardLevelItem>();
                    if (sl != null)
                        sl.GrantXP(inv, xp);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient && dueño.whoAmI == Main.myPlayer)
                    ShardHUDSystem.MarcarGanancia(xp);   // SP: el latido dorado
                if (Main.netMode == NetmodeID.Server)
                    EcoRed.SincronizarLibros(dueño);     // MP: la foto al portador
            }
            catch { }
        }

        // =================================================================
        // v6.50.91 — EL RELOJ Y LAS DOS VIDAS (reemplaza el UpdateInventory
        // de la base: el Nervioso tiene horas propias).
        // =================================================================
        public override void UpdateInventory(Player player)
        {
            bool local = player.whoAmI == Main.myPlayer &&
                (Main.netMode != NetmodeID.Server || !Main.dedServ);

            if (local && Main.GameUpdateCount != Estado.TickMarcado)
            {
                Estado.TickMarcado = Main.GameUpdateCount;
                // LA CAZA CONGELA EL RELOJ: «el libro se mantendrá cazando su
                // propia comida hasta llegar a 0% de hambre» — mientras
                // patrulla, sólo las presas mueven el apetito (el ojo sigue
                // vivo: PasoSinHambre). Y el anfitrión de un listen server
                // también pasa hambre (el guard viejo lo dejaba ciego).
                if (CaceriaActiva())
                    Estado.PasoSinHambre();
                else
                    Estado.Paso();
            }

            PasoCaza(player);
        }

        /// <summary>LA FASE DEL NERVIOSO en el tooltip — sus estados NO son
        /// los del resto de la familia (cazador / impaciente / fugitivo).</summary>
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            float h = Estado.Hambre;
            string clave = CaceriaActiva() ? "Cazando"
                : h >= 1f ? "Fugitivo"
                : h >= UMBRAL_IMPACIENCIA ? "Impaciente"
                : "Tranquilo";
            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].Name == "HambreEstado")
                {
                    tooltips[i].Text = Language.GetTextValue("Mods.AethonMod.Hambre.Nervioso." + clave);
                    break;
                }
            }
        }
    }
}
