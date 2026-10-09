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
    /// v6.50.89 — LA CUARTA COPIA: EL GRIMORIO INESTABLE. La letra del
    /// usuario: «crea un cuarto grimorio donde el efecto glish y el temblor
    /// este unidos, ademas ponle un aura pequeña roja». Es la UNIÓN de las
    /// dos copias de la v6.50.87/.88 con una firma propia:
    ///
    /// · EL GLITCH del Errático — la receta v6.50.88 INTACTA (el volumen
    ///   que el usuario aprobó tras pedir el +70 %): tiras 2-4 desfasándose
    ///   hasta ±6 px, ráfagas de 12-24 ticks que se rehacen cada 2 frames,
    ///   cada 15 s → 2,1 s con el hambre, gate al 12 %. La máquina es SUYA
    ///   (estado estático propio): el Inestable rompe a su hora, no a la
    ///   del Errático.
    /// · EL TEMBLOR del Tembloroso — los dos senos incommensurables por eje
    ///   (9,3/17,3 Hz en X, 11,9/19,1 Hz en Y), 0,2 px saciado → 1,3 px
    ///   famélico, leyendo SU PROPIA hambre. El libro entero tiembla y a la
    ///   vez se rompe: cada tira lleva el temblor del cuerpo y su desfase
    ///   de ráfaga.
    /// · EL AURA ROJA PEQUEÑA — lo nuevo de la casa: un resplandor rojo
    ///   vivo (el SoftGlow de 64×64 de la librería de efectos, teñido
    ///   (255, 64, 48)) respirando DETRÁS del libro en los TRES estados —
    ///   inventario, mundo y mano. PEQUEÑA de verdad: 1,55× el ancho del
    ///   libro (el halo asoma ~10 px por lado), alfa 0,30 saciado → 0,42
    ///   famélico con una respiración lenta de ±12 % (0,45 Hz). En el mundo
    ///   la luz no la apaga del todo: piso de 35 % de brillo — de noche el
    ///   libro sigue ardiendo.
    ///
    /// El OJO queda del ciclo NORMAL (mirada 4 s → 0,35 s, sin el tic
    /// errático): la personalidad de la cuarta copia es un ojo sereno en un
    /// cuerpo que se descompone — poseído por dentro, roto por fuera. El
    /// ciclo vive en SU PROPIO EstadoGrimorio (la lección de la .86): las
    /// cuatro copias pasan hambre por separado.
    /// </summary>
    public class GrimorioHambrientoInestable : GrimorioHambriento
    {
        // === EL GLITCH — las perillas de la .88 (el volumen aprobado) ===
        // sin glitch por debajo del 12 % de hambre (saciado = libro limpio)
        private const float HAMBRE_MIN_GLITCH = 0.12f;

        private static readonly EstadoGrimorio _estadoInestable = new EstadoGrimorio();
        protected override EstadoGrimorio Estado => _estadoInestable;

        // === LA MÁQUINA DE RÁFAGAS (avanza UNA vez por tick — la dibujan
        //     las tres vías: inventario, mundo y mano comparten el glitch) ===
        private static uint _tickGlitch;
        private static int _tHastaRafaga = 120;    // 2 s hasta la primera
        private static int _tRafaga;               // >0: ticks que le quedan a la ráfaga
        private static uint _semilleRafaga;

        /// <summary>El hash determinista del layout (mismo tick = mismo glitch).</summary>
        private static uint Hash(uint x)
        {
            x ^= x >> 16; x *= 0x7FEB352Du;
            x ^= x >> 15; x *= 0x846CA68Bu;
            x ^= x >> 13;
            return x;
        }

        private static void PasoGlitch()
        {
            uint t = Main.GameUpdateCount;
            if (t == _tickGlitch)
                return;                       // 1 avance por tick
            _tickGlitch = t;

            float h = _estadoInestable.Hambre;
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
        }

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
            float h = _estadoInestable.Hambre;

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
        // EL TEMBLOR (la receta del Tembloroso — leyendo SU hambre).
        // =================================================================
        /// <summary>EL TEMBLOR de este tick (px de juego, en unidades del
        /// libro — el llamador lo escala a su contexto). Determinista puro:
        /// una función del tick, sin estado.</summary>
        private static Vector2 Temblor()
        {
            float t = Main.GameUpdateCount / 60f;
            float amp = 0.20f + 1.10f * _estadoInestable.Hambre;   // 0,2 px → 1,3 px
            float x = (float)Math.Sin(t * 9.3f * MathHelper.TwoPi) * 0.80f
                    + (float)Math.Sin(t * 17.3f * MathHelper.TwoPi) * 0.25f;
            float y = (float)Math.Sin(t * 11.9f * MathHelper.TwoPi + 1.7f) * 0.80f
                    + (float)Math.Sin(t * 19.1f * MathHelper.TwoPi + 0.6f) * 0.25f;
            return new Vector2(x, y) * amp;
        }

        // =================================================================
        // EL AURA ROJA PEQUEÑA — un resplandor que RESPIRA detrás del libro
        // en los tres estados. SoftGlow (64×64, blanco puro con caída de
        // alfa) teñido de rojo: la intensidad crece con el apetito.
        // =================================================================
        private const float AURA_ANCHO = 1.55f;   // × el ancho del libro (halo ~10 px por lado)
        private static readonly Color AURA_ROJO = new Color(255, 64, 48);
        private const float AURA_ALFA_MIN = 0.30f; // saciado
        private const float AURA_ALFA_MAX = 0.42f; // famélico
        private const float AURA_HZ = 0.45f;       // la respiración (~2,2 s por ciclo)

        /// <summary>La respiración del aura (±12 %) — lenta, orgánica.</summary>
        private static float PulsoAura() =>
            0.88f + 0.12f * (float)Math.Sin(Main.GameUpdateCount / 60f * AURA_HZ * MathHelper.TwoPi);

        /// <summary>El rojo del aura bajo una luz: en el mundo la luz no la
        /// apaga del todo (piso de 35 % — de noche el libro sigue ardiendo,
        /// que es lo que un aura hace).</summary>
        private static Color ColorAura(Color luz, float alfa)
        {
            float m = Math.Max(luz.R, Math.Max(luz.G, luz.B)) / 255f;
            m = Math.Max(m, 0.35f);
            return new Color((byte)(AURA_ROJO.R * m), (byte)(AURA_ROJO.G * m),
                (byte)(AURA_ROJO.B * m), (byte)(255 * alfa));
        }

        /// <summary>El alfa del aura: 0,30 → 0,42 con el hambre, respirando.</summary>
        private static float AlfaAura() =>
            MathHelper.Lerp(AURA_ALFA_MIN, AURA_ALFA_MAX, _estadoInestable.Hambre) * PulsoAura();

        private static Texture2D TexAura() =>
            ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;

        /// <summary>El aura en SpriteBatch — DETRÁS del libro (se dibuja
        /// primero): centro del libro, escala para que el halo mida
        /// AURA_ANCHO × el ancho renderizado del libro.</summary>
        private static void DibujarAura(SpriteBatch sb, Vector2 centro, float anchoLibro, Color luz)
        {
            var aura = TexAura();
            float esc = anchoLibro * AURA_ANCHO / aura.Width;
            esc *= 1f + 0.04f * ((PulsoAura() - 0.88f) / 0.12f);   // ±4 % de hinchado en la inspiración
            sb.Draw(aura, centro, null, ColorAura(luz, AlfaAura()), 0f,
                aura.Size() * 0.5f, esc, SpriteEffects.None, 0f);
        }

        // =================================================================
        // INVENTARIO — el aura respira detrás, el libro tiembla y se rompe.
        // =================================================================
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
            => false;

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            PasoGlitch();
            Tira[] tiras = Layout(out float offOjo);
            Vector2 temblor = Temblor() * scale;

            // EL AULA ROJA — detrás de todo, respirando
            DibujarAura(spriteBatch, position + temblor, frame.Width * scale, drawColor);

            // el cursor lo sigue el ojo en su CASA (con el temblor del cuerpo)
            Estado.PosOjoPantalla = position + temblor + (OJO - origin) * scale;
            Estado.PosValida = true;

            // EL LIBRO — entero sin ráfaga, en tiras con ella, TEMBLANDO
            DibujarTiras(spriteBatch, ModContent.Request<Texture2D>(Texture).Value,
                position + temblor, frame, drawColor, 0f, origin, scale, tiras);

            if (Estado.Fase > 0)
            {
                // el párpado se ROMPE y TIEMBLA con el libro (el mismo layout)
                bool medio = Estado.Fase > 7 || Estado.Fase < 4;
                DibujarTiras(spriteBatch, Capa(medio ? "Medio" : "Cerrado"),
                    position + temblor, frame, drawColor, 0f, origin, scale, tiras);
                return;
            }

            // EL IRIS — viaja con su tira y tiembla con el libro
            var iris = Capa("Iris");
            var irisRojo = Capa("IrisRojo");
            Vector2 posIris = position + temblor + (OJO + Estado.DespActual - origin) * scale
                + new Vector2(offOjo * scale, 0f);
            spriteBatch.Draw(iris, posIris, null, drawColor, 0f, iris.Size() * 0.5f,
                scale * IRIS_ESC, SpriteEffects.None, 0f);
            float rojo = Estado.NivelRojo();
            if (rojo > 0f)
                spriteBatch.Draw(irisRojo, posIris, null, Alfa(rojo), 0f,
                    irisRojo.Size() * 0.5f, scale * IRIS_ESC, SpriteEffects.None, 0f);
        }

        // =================================================================
        // MUNDO — el aura va en PreDraw (DETRÁS del libro que dibuja Post):
        // la MISMA convención vanilla v6.50.84 (pivote = centro del libro).
        // =================================================================
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                Vector2 pivote = PivoteEnMundo(out var frame, out var _);
                DibujarAura(spriteBatch, pivote, frame.Width * scale, lightColor);
            }
            return false;
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, float rotation, float scale, int whoAmI)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            PasoGlitch();
            Tira[] tiras = Layout(out float offOjo);

            Vector2 pivote = PivoteEnMundo(out var frame, out var origen);
            Vector2 temblor = Temblor() * scale;

            // la CASA del ojo (con el temblor — el ojo tiembla con el libro)
            Vector2 alOjo = (OJO + Estado.DespActual - origen) * scale;
            Vector2 casa = pivote + temblor + alOjo.RotatedBy(rotation);
            Estado.PosOjoPantalla = casa;
            Estado.PosValida = true;

            DibujarTiras(spriteBatch, ModContent.Request<Texture2D>(Texture).Value,
                pivote + temblor, frame, lightColor, rotation, origen, scale, tiras);

            if (Estado.Fase > 0)
            {
                bool medio = Estado.Fase > 7 || Estado.Fase < 4;
                DibujarTiras(spriteBatch, Capa(medio ? "Medio" : "Cerrado"),
                    pivote + temblor, frame, lightColor, rotation, origen, scale, tiras);
                return;
            }

            // el iris gira con el libro, viaja con su tira y tiembla con el cuerpo
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
        // MANO — el temblor se INYECTA en la DrawData, el glitch son tiras y
        // el aura es la PRIMERA DrawData del cache (v6.50.84).
        // =================================================================
        public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData,
            ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
        {
            if (Main.netMode == NetmodeID.Server)
                return base.ModifyItemDraw(ref drawInfo, ref drawData, ref coloredDrawData, ref glowMaskDrawData);

            PasoGlitch();
            Tira[] tiras = Layout(out float offOjo);

            // EL TEMBLOR del cuerpo entra en la posición del libro
            drawData.position += Temblor() * Math.Abs(drawData.scale.X);

            bool espejoX = (drawData.effect & SpriteEffects.FlipHorizontally) != 0;
            bool espejoY = (drawData.effect & SpriteEffects.FlipVertically) != 0;
            Rectangle fr = drawData.sourceRect ?? new Rectangle(0, 0, 36, 49);

            // EL AURA — la primera del cache: detrás del libro, respirando,
            // con el MISMO espejo (el centro no cambia, pero así viaja con él)
            var aura = TexAura();
            Vector2 centro = drawData.position + new Vector2(
                (fr.Width * 0.5f - drawData.origin.X) * drawData.scale.X,
                (fr.Height * 0.5f - drawData.origin.Y) * drawData.scale.Y).RotatedBy(drawData.rotation);
            float escAura = fr.Width * Math.Abs(drawData.scale.X) * AURA_ANCHO / aura.Width;
            escAura *= 1f + 0.04f * ((PulsoAura() - 0.88f) / 0.12f);
            drawInfo.DrawDataCache.Add(new DrawData(aura, centro, null,
                ColorAura(drawData.color, AlfaAura()), drawData.rotation,
                aura.Size() * 0.5f, new Vector2(escAura), drawData.effect));

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
