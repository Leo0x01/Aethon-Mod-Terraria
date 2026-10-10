using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

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
    ///   fases 1,7/0,6), amplitud 0,2 px saciado → 1,3 px famélico leyendo
    ///   SU PROPIA hambre. El escalofrío continuo del difunto.
    /// · EL GLITCH del Errático — la receta .88 INTACTA (el volumen que el
    ///   usuario aprobó: tiras 2-4 desfasándose hasta ±6 px, ráfagas de
    ///   12-24 ticks que se rehacen cada 2 frames, cada 15 s → 2,1 s con el
    ///   hambre, gate al 12 %) con máquina de estado PROPIA: el Nervioso
    ///   rompe a SU hora, jamás sincronizado con el Errático o el Inestable.
    /// · EL NERVIOSO — lo nuevo de la casa, en DOS capas:
    ///   (1) EL OJO ANSIOSO (EstadoGrimorio.Nervioso): revisa cada 2,4 s →
    ///       0,42 s, sus miradas SALTAN a la meta (LERP 0,22 — aterrizan de
    ///       golpe, sin el resbalón del original), 1 de cada 3 vuelve al
    ///       CENTRO (la revisada ansiosa de la casa) y la mitad de las otras
    ///       se va de DARDO LATERAL (el barrido izquierda-derecha del
    ///       susto). Sin el tic del Errático: el miedo no es caos, es patrón.
    ///   (2) EL SOBRESALTO: cada 6,5 s → 1,8 s con el hambre (±25 %), el
    ///       libro da un BRINCO de 1,2 px → 2,2 px en una dirección al azar
    ///       que se asienta en 7 ticks. Y EL SUSTO ROMPE EL LIBRO: con
    ///       hambre (≥12 %) cada sobresalto trae consigo una ráfaga corta de
    ///       glitch (8-14 ticks) — el libro se asusta, brinca Y se rompe.
    ///
    /// La familia queda con CUATRO TEMPERAMENTOS: el normal SERENO, el
    /// Errático CAÓTICO (ojo desenfrenado + glitch), el Nervioso ASUSTADIZO
    /// (ojo ansioso + sobresaltos + temblor + glitch) y el Inestable POSEÍDO
    /// (ojo sereno en cuerpo que se descompone + aura roja). SIN aura roja —
    /// esa es la firma del Inestable. El ciclo vive en SU PROPIO
    /// EstadoGrimorio (la lección de la .86): los cuatro pasan hambre por
    /// separado.
    /// </summary>
    public class GrimorioHambrientoNervioso : GrimorioHambriento
    {
        // === EL GLITCH — las perillas de la .88 (el volumen aprobado) ===
        // sin glitch por debajo del 12 % de hambre (saciado = libro limpio)
        private const float HAMBRE_MIN_GLITCH = 0.12f;

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
        /// sobresalto (la firma del Nervioso — el susto que rompe el libro).</summary>
        private static void PasoMaquina()
        {
            uint t = Main.GameUpdateCount;
            if (t == _tickMaquina)
                return;                       // 1 avance por tick
            _tickMaquina = t;

            float h = _estadoNervioso.Hambre;

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
                // el reloj del susto: 6,5 s → 1,8 s con el hambre (±25 %)
                _tHastaSobresalto = (int)(60f * MathHelper.Lerp(6.5f, 1.8f, h)
                    * (0.75f + 0.5f * (Hash(t ^ 0x68E31DA4u) & 255u) / 255f));
                _tSobresalto = DURACION_SOBRESALTO;
                _ampSobresalto = MathHelper.Lerp(1.2f, 2.2f, h);
                double ang = Main.rand.NextDouble() * Math.PI * 2.0;
                _dirSobresalto = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang));

                // EL SUSTO ROMPE EL LIBRO: con hambre, cada brinco trae una
                // ráfaga corta (8-14 ticks) — se asusta, salta Y se rompe
                if (h >= HAMBRE_MIN_GLITCH && _tRafaga <= 0)
                {
                    _tRafaga = 8 + (int)(Hash(t ^ 0x2545F491u) % 7u);
                    _semilleRafaga = t * 2654435761u;
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

        private struct Tira { public int Y0, Y1; public float Off; }

        /// <summary>EL LAYOUT de este tick (null = libro entero, sin ráfaga).</summary>
        private static Tira[] Layout(out float offOjo)
        {
            offOjo = 0f;
            if (_tRafaga <= 0)
                return null;

            // las tiras se REGENERAN cada 2 frames — el tearing vivo
            uint fase = Main.GameUpdateCount / 2u;
            uint sd = Hash(_semilleRafaga ^ (fase * 0x9E3779B9u));
            float h = _estadoNervioso.Hambre;

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

        /// <summary>Dibuja la textura ENTERA o en TIRAS — inventario y mundo
        /// (cada franja a su lugar natural + su desfase, con la MISMA
        /// transform del libro; sin ráfaga es UN draw, el libro tal cual).</summary>
        private static void DibujarTiras(SpriteBatch sb, Texture2D tex, Vector2 position, Rectangle frame,
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
        private static Vector2 Temblor()
        {
            float t = Main.GameUpdateCount / 60f;
            float amp = 0.20f + 1.10f * _estadoNervioso.Hambre;   // 0,2 px → 1,3 px
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
        private static Vector2 Corrimiento() => Temblor() + Sobresalto();

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
    }
}
