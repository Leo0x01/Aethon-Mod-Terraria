using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace AethonMod.Content.VFX
{
    // ====================================================================
    //  GlowUILib — v6.151 — LA LUZ DE LA INTERFAZ (la letra: «creo que
    //  tendras que crear un shader o libreria que permita iluminar,
    //  opacar o desaturar partes de la interfaz o cualquier otra cosa
    //  en terraria»).
    //
    //  EL MOTOR: tres primitivas de UI dibujadas con el lote del propio
    //  sprite batch de la interfaz (SIN abrir lotes propios — la
    //  lección de la casa: quien dibuja UI cabalga el lote del pase):
    //   · MarcoLuz  — un MARCO cuadrado de luz (el borde que SOBRESALE
    //     del rectángulo: 4 barras con degradado monotónico hacia
    //     afuera — R2 de la casa, jamás círculos planos).
    //   · Iluminar  — un velo aditivo SOBRE una región (multiplica la
    //     luz que ya hay debajo: para resaltar iconos LISTOS).
    //   · Opacar    — un velo alfa QUE OSCURECE una región (para
    //     iconos agotados/en recarga).
    //   · Desaturar — un velo GRIS de baja saturación sobre una región
    //     (el icono apagado sin matar su silueta).
    //
    //  TODO determinista (R5: cero Main.rand — solo el reloj global y
    //  Hash01) y SIN texturas nuevas: usa el SoftGlow de la casa
    //  ESTIRADO EN BARRAS (la técnica de la v6.56: el degradado nace
    //  del propio sprite de resplandor, monotónico de verdad).
    // ====================================================================
    public static class GlowUILib
    {
        /// <summary>El blanco de la luz de interfaz de la casa.</summary>
        public static readonly Color LuzUI = new(236, 214, 255);

        /// <summary>El púrpura del vacío de la paleta canónica.</summary>
        public static readonly Color PurpuraUI = new(178, 96, 250);

        private static Texture2D _glow;
        private static Texture2D _pixel;

        private static void Cargar()
        {
            if (_glow == null)
                _glow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            if (_pixel == null)
                _pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
        }

        /// <summary>EL MARCO DE LUZ — el borde cuadrado que SOBRESALE
        /// del rectángulo. Cuatro barras (una por lado) de ancho
        /// <paramref name="salida"/> px, con degradado monotónico hacia
        /// afuera (la intensidad CAE con la distancia: la barra más
        /// cercana al icono arde, la lejana se apaga — nunca un cuadro
        /// plano). <paramref name="pulso"/> 0..1 modula el latido.</summary>
        public static void MarcoLuz(SpriteBatch sb, Rectangle r, Color color,
            float salida, float intensidad, float pulso = 0f)
        {
            if (sb == null || r.Width <= 0 || r.Height <= 0 || salida < 0.5f) return;
            if (intensidad <= 0.01f) return;
            Cargar();

            // el latido de la casa: 0.78 + 0.22·sin — vivo, nunca nervioso
            float late = 0.78f + 0.22f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3.1f);
            float k = intensidad * (1f - pulso * 0.35f + pulso * 0.35f * late);

            // LAS CUATRO BARRAS — cada una con SU degradado: la mitad
            // interior (pegada al icono) al 100 % y la exterior al 0 %
            // (dos tramos por lado, monotónico — el marco SOBRESALE).
            float grueso = salida;
            void Barra(float x, float y, float ancho, float alto, float alfa)
            {
                if (alfa <= 0.01f) return;
                sb.Draw(_glow, new Rectangle((int)x, (int)y, (int)ancho, (int)alto),
                    color * alfa);
            }

            float xi = r.X - grueso, yi = r.Y - grueso;
            float xf = r.Right, yf = r.Bottom;
            // ARRIBA y ABAJO (el degradado nace del estirado del SoftGlow:
            // su centro es lo brillante — estiramos horizontal y la caída
            // vertical queda monotónica de la mitad hacia afuera)
            Barra(xi, yi, r.Width + grueso * 2f, grueso * 2f, k * 0.85f);
            Barra(xi, yf - grueso, r.Width + grueso * 2f, grueso * 2f, k * 0.85f);
            // IZQUIERDA y DERECHA
            Barra(xi, yi, grueso * 2f, r.Height + grueso * 2f, k * 0.85f);
            Barra(xf - grueso, yi, grueso * 2f, r.Height + grueso * 2f, k * 0.85f);
        }

        /// <summary>EL VELO QUE ILUMINA — un rectángulo aditivo SOBRE la
        /// región (la luz SUMA sobre lo que ya se ve: el icono listo
        /// ARDE por dentro). Dibujar DESPUÉS del icono.</summary>
        public static void Iluminar(SpriteBatch sb, Rectangle r, Color color, float intensidad)
        {
            if (sb == null || intensidad <= 0.01f) return;
            Cargar();
            sb.Draw(_glow, r, color * (intensidad * 0.9f));
        }

        /// <summary>EL VELO QUE OPACA — un rectángulo NEGRO alfa SOBRE
        /// la región (el icono en recarga se APAGA por dentro, sin
        /// tocar al vecino). Dibujar DESPUÉS del icono.</summary>
        public static void Opacar(SpriteBatch sb, Rectangle r, float intensidad)
        {
            if (sb == null || intensidad <= 0.01f) return;
            Cargar();
            sb.Draw(_pixel, r, Color.Black * (intensidad * 0.55f));
        }

        /// <summary>EL VELO QUE DESATURA — un rectángulo GRIS frío de
        /// baja saturación (el icono pierde su SANGRE sin perder la
        /// silueta: medio apagado, medio presente). Dibujar DESPUÉS
        /// del icono.</summary>
        public static void Desaturar(SpriteBatch sb, Rectangle r, float intensidad)
        {
            if (sb == null || intensidad <= 0.01f) return;
            Cargar();
            sb.Draw(_pixel, r, new Color(120, 116, 132) * (intensidad * 0.45f));
        }
    }
}
