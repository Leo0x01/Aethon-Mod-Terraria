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
    /// v6.50.87 — LA COPIA ERRÁTICA DEL GRIMORIO HAMBRIENTO (nació Rúnico en
    /// la .86; el usuario: «deja solo el grimorio hambriento normal y
    /// modifica las dos copias con otros efectos, borra los anteriores, esta
    /// vez que sean mas suave los efectos… has que una de las copias se
    /// vuelva erratica al tener hambre, cuanta mas hambre mas erratica y que
    /// tenga un efecto glish»). Las runas y el aura quedaron BORRADAS — el
    /// usuario las vio «bastante mal y exageradas».
    ///
    /// DOS COSAS VIVEN AQUÍ y las dos crecen con el apetito:
    ///
    /// · LA MIRADA ERRÁTICA (EstadoGrimorio.Erratico): el ojo deja de
    ///   vigilar tranquilo — sus cambios de mirada pasan de 3 s a 0,16 s, el
    ///   resbalón se vuelve brusco (LERP 0,10 → 0,36) y cada meta lleva un
    ///   tic que ninguna repetición comparte. Saciado es apenas más
    ///   inquieto que el original; famélico es un ojo que no se queda quieto.
    ///
    /// · EL GLITCH — RÁFAGAS cortas y suaves, nada de ruido por frame: el
    ///   96 % del tiempo el libro está LIMPIO (la lección de la .86: los
    ///   efectos permanentes gritan; los breves susurran). Cada ráfaga dura
    ///   7-14 ticks (0,12-0,23 s); entre ráfagas pasan 22 s → 3,2 s con el
    ///   hambre y abajo del 12 % de hambre NO HAY GLITCH (saciado, el libro
    ///   está entero). Dentro de la ráfaga el libro se rompe en 1-2 TIRAS
    ///   horizontales desfasadas ±1,3 → ±3 px que se regeneran cada 3
    ///   frames: el «tearing» de señal perdida. El layout es DETERMINISTA
    ///   por tick (hash de la semilla de la ráfaga): el hotbar y la mano
    ///   muestran el MISMO glitch, y el iris VIAJA CON SU TIRA — el ojo
    ///   nunca se desprende del libro roto.
    ///
    /// Las tiras se dibujan como el libro mismo: la MISMA transform vanilla
    /// en inventario, mundo (con la rotación de vuelo) y mano (espejo y
    /// gravedad incluidos) — v6.50.84. El ciclo del original queda INTACTO
    /// (mismo ojo, mismo parpadeo PURO de código, misma descarga, mismo clic
    /// derecho) con SU PROPIO EstadoGrimorio.
    /// </summary>
    public class GrimorioHambrientoErratico : GrimorioHambriento
    {
        // sin glitch por debajo del 12 % de hambre (saciado = libro limpio)
        private const float HAMBRE_MIN_GLITCH = 0.12f;

        private static readonly EstadoGrimorio _estadoErratico = new EstadoGrimorio { Erratico = true };
        protected override EstadoGrimorio Estado => _estadoErratico;

        // === LA MÁQUINA DE RÁFAGAS (avanza UNA vez por tick — lo dibujan
        //     las tres vías: inventario, mundo y mano comparten el glitch) ===
        private static uint _tickGlitch;
        private static int _tHastaRafaga = 240;    // 4 s hasta la primera
        private static int _tRafaga;               // >0: ticks que le quedan a la ráfaga
        private static uint _semillaRafaga;

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

            float h = _estadoErratico.Hambre;
            if (_tRafaga > 0)
            {
                if (--_tRafaga == 0)
                {
                    // la próxima ráfaga: 22 s → 3,2 s con el hambre (±25 %)
                    _tHastaRafaga = (int)(60f * MathHelper.Lerp(22f, 3.2f, h)
                        * (0.75f + 0.5f * (Hash(t) & 255u) / 255f));
                }
            }
            else if (--_tHastaRafaga <= 0)
            {
                if (h >= HAMBRE_MIN_GLITCH)
                {
                    _tRafaga = 7 + (int)(Hash(t ^ 0x5BD1E995u) & 7u);   // 7-14 ticks
                    _semillaRafaga = t * 2654435761u;
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

            // las tiras se REGENERAN cada 3 frames — el tearing vivo
            uint fase = Main.GameUpdateCount / 3u;
            uint sd = Hash(_semillaRafaga ^ (fase * 0x9E3779B9u));
            float h = _estadoErratico.Hambre;

            int n = 1 + (int)(Hash(sd) % 2u);            // 1-2 tiras (SUAVE)
            var tiras = new Tira[n];
            int hechos = 0;
            int y = 3 + (int)(Hash(sd ^ 0xA5A5u) % 34u); // la primera nace en y=3..36
            while (hechos < n && y <= 41)
            {
                int alto = 4 + (int)(Hash(sd ^ (uint)(hechos * 97 + 1)) % 8u);      // 4-11 px
                float off = ((int)(Hash(sd ^ (uint)(hechos * 131 + 3)) % 7u) - 3)
                    * (0.45f + 0.55f * h);                                          // ±1,3 → ±3 px
                tiras[hechos++] = new Tira { Y0 = y, Y1 = Math.Min(46, y + alto), Off = off };
                y = tiras[hechos - 1].Y1 + 2 + (int)(Hash(sd ^ (uint)(hechos * 17 + 5)) % 6u);
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
        /// por su borde INFERIOR (FlipVertically la voltea dentro de su propio
        /// quad — la misma lección del espejo de la v6.50.84).</summary>
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
        // INVENTARIO — el libro entero lo dibuja la copia (PreDraw false):
        // vanilla dibujaría el libro fijo; aquí puede venir ROTO en tiras.
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

            // el cursor lo sigue el ojo en su CASA (sin el ruido de la ráfaga)
            // — como el original: también durante el parpadeo
            Estado.PosOjoPantalla = position + (OJO - origin) * scale;
            Estado.PosValida = true;

            // EL LIBRO — entero sin ráfaga, en tiras con ella
            DibujarTiras(spriteBatch, ModContent.Request<Texture2D>(Texture).Value,
                position, frame, drawColor, 0f, origin, scale, tiras);

            if (Estado.Fase > 0)
            {
                // el párpado se ROMPE con el libro (el mismo layout)
                bool medio = Estado.Fase > 7 || Estado.Fase < 4;
                DibujarTiras(spriteBatch, Capa(medio ? "Medio" : "Cerrado"),
                    position, frame, drawColor, 0f, origin, scale, tiras);
                return;
            }

            // EL IRIS — viaja con su tira
            var iris = Capa("Iris");
            var irisRojo = Capa("IrisRojo");
            Vector2 posIris = position + (OJO + Estado.DespActual - origin) * scale
                + new Vector2(offOjo * scale, 0f);
            spriteBatch.Draw(iris, posIris, null, drawColor, 0f, iris.Size() * 0.5f,
                scale * IRIS_ESC, SpriteEffects.None, 0f);
            float rojo = Estado.NivelRojo();
            if (rojo > 0f)
                spriteBatch.Draw(irisRojo, posIris, null, Alfa(rojo), 0f,
                    irisRojo.Size() * 0.5f, scale * IRIS_ESC, SpriteEffects.None, 0f);
        }

        // =================================================================
        // MUNDO — la convención vanilla v6.50.84 (pivote + rotación de vuelo).
        // =================================================================
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, ref float rotation, ref float scale, int whoAmI)
            => false;

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, float rotation, float scale, int whoAmI)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            PasoGlitch();
            Tira[] tiras = Layout(out float offOjo);

            Vector2 pivote = PivoteEnMundo(out var frame, out var origen);

            // la CASA del ojo (también durante el parpadeo — como el original)
            Vector2 alOjo = (OJO + Estado.DespActual - origen) * scale;
            Vector2 casa = pivote + alOjo.RotatedBy(rotation);
            Estado.PosOjoPantalla = casa;
            Estado.PosValida = true;

            DibujarTiras(spriteBatch, ModContent.Request<Texture2D>(Texture).Value,
                pivote, frame, lightColor, rotation, origen, scale, tiras);

            if (Estado.Fase > 0)
            {
                bool medio = Estado.Fase > 7 || Estado.Fase < 4;
                DibujarTiras(spriteBatch, Capa(medio ? "Medio" : "Cerrado"),
                    pivote, frame, lightColor, rotation, origen, scale, tiras);
                return;
            }

            // el iris gira con el libro y viaja con su tira
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
        // MANO — ModifyItemDraw agrega las franjas como DrawData (v6.50.84).
        // =================================================================
        public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData,
            ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
        {
            if (Main.netMode == NetmodeID.Server)
                return base.ModifyItemDraw(ref drawInfo, ref drawData, ref coloredDrawData, ref glowMaskDrawData);

            PasoGlitch();
            Tira[] tiras = Layout(out float offOjo);

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
