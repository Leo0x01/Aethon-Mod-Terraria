using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Items.Accessories;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// AuraJuicioMetroSistema — v6.104 — EL MEDIDOR «FUERZA DEL
    /// ESTANDARTE» (la barrita del original, hecha casa).
    ///
    /// LA LETRA DEL USUARIO: «copia exactamente el codigo de Warbanner
    /// of the Righteous y sus textura, efectos vfx tecnica todo».
    ///
    /// QUÉ MIDE: la CORRIENTE DE PODER del Aura de Juicio — el
    /// multiplicador NETO que el campo está aplicando AHORA a TODO el
    /// daño del portador (−15% sin enemigos cerca … +15% pegado al
    /// impío más cercano). El original la dibuja como una barrita con
    /// el porcentaje; la casa la viste de icono de estandarte:
    ///
    ///   [⚑] ├───────────┤ +12%
    ///
    ///  · EL ICONO: el mini estandarte 14×18 (AuraJuicioMetro.png).
    ///  · LA BARRA: 90×10 — fondo nocturno, marco dorado y relleno
    ///    SlateGray→Gold según la fuerza (0% = gris plomo: el campo
    ///    vacío CASTIGA; 100% = oro pleno: el juicio pegado al cuerpo).
    ///  · EL TEXTO: «+X%» o «−X%» (caché de la casa: solo se
    ///    reconstruye cuando cambia el porcentaje redondeado).
    ///
    /// DÓNDE: abajo-izquierda, DEBAJO de la última fila de buffs (la
    /// geometría adaptativa del ShardHUDSystem: 11 buffs por fila,
    /// +50 px por fila extra — jamás pisa los iconos) y bajo la línea
    /// de texto de la barra de XP del grimorio.
    /// </summary>
    public class AuraJuicioMetroSistema : ModSystem
    {
        // === LA GEOMETRÍA (la de la barra dorada del grimorio) ===
        private const float X = 20f;            // el borde izquierdo del hotbar
        private const int AnchoBarra = 90;
        private const int AltoBarra = 10;

        // === EL CACHÉ DE TEXTOS (la ley AUD-C: cero strings por frame) ===
        private static int _cachePorcentaje = int.MinValue;
        private static string _cacheTexto = "+0%";
        private static Vector2 _cacheMedida = Vector2.Zero;
        private static string _cacheTitulo;

        // === LA TEXTURA DEL ICONO (null-segura, recarga caliente) ===
        private static Asset<Texture2D> _icono;

        private static Texture2D Icono
        {
            get
            {
                try
                {
                    _icono ??= ModContent.Request<Texture2D>(
                        "AethonMod/Content/Systems/AuraJuicioMetro");
                    return _icono.IsLoaded ? _icono.Value : null;
                }
                catch { return null; }
            }
        }

        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            if (Main.gameMenu || Main.netMode == NetmodeID.Server || Main.dedServ)
                return;

            Player p = Main.LocalPlayer;
            if (p == null || !p.active)
                return;

            // EL GEMELO DE LA VERDAD: el static lo escribe la
            // UpdateAccessory, pero la ROPA se re-verifica contra los
            // slots de armadura (desequipar el aura apaga el medidor
            // aunque el static quede caliente un frame).
            if (!AuraDeJuicioItem.Activo || !AuraDeJuicioItem.LlevaElAura(p))
                return;

            try
            {
                DibujarElMedidor(spriteBatch, p);
            }
            catch { }
        }

        public override void OnWorldUnload()
        {
            AuraDeJuicioItem.Activo = false;
            AuraDeJuicioItem.FuerzaViva = 0f;
            _cachePorcentaje = int.MinValue;
            _cacheTitulo = null;
        }

        public override void Unload()
        {
            _icono = null;
            OnWorldUnload();
        }

        // ================================================================
        //  EL MEDIDOR — el icono + la barra + el veredicto.
        // ================================================================
        private static void DibujarElMedidor(SpriteBatch sb, Player p)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            if (pixel == null)
                return;
            DynamicSpriteFont fuente = FontAssets.MouseText.Value;
            if (fuente == null)
                return;

            // === LA FUERZA VIVA (−0.15..+0.15 → 0..1 para la barra) ===
            float fuerza = MathHelper.Clamp(AuraDeJuicioItem.FuerzaViva, -0.15f, 0.15f);
            float fraccion = (fuerza + 0.15f) / 0.30f;

            // === LA GEOMETRÍA ADAPTATIVA (la de la barra del grimorio:
            //     filas de 11 buffs, primera fila y=76, +50 por fila) ===
            int buffs = 0;
            for (int b = 0; b < p.buffType.Length; b++)
                if (p.buffType[b] > 0) buffs++;
            int filas = buffs > 0 ? (buffs + 10) / 11 : 0;
            float y = filas > 0 ? 76f + filas * 50f + 24f : 108f;

            float latido = 0.85f + 0.15f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3f);

            // === EL ICONO — el mini estandarte (14×18) a la izquierda ===
            float xIcono = X;
            Texture2D icono = Icono;
            if (icono != null)
            {
                sb.Draw(icono, new Vector2(xIcono, y - 4f), null,
                    Color.White * latido, 0f, Vector2.Zero, 1f,
                    SpriteEffects.None, 0f);
            }

            // === LA BARRA (x tras el icono + 4 de aire) ===
            float x = xIcono + (icono != null ? 18f : 0f);

            // EL FONDO NOCTURNO + EL MARCO DORADO
            sb.Draw(pixel, new Rectangle((int)(x - 2), (int)(y - 2), AnchoBarra + 4, AltoBarra + 4),
                new Color(12, 10, 20) * 0.68f);
            Color marco = new Color(255, 214, 110) * (0.55f + 0.25f * latido);
            sb.Draw(pixel, new Rectangle((int)(x - 2), (int)(y - 2), AnchoBarra + 4, 1), marco);
            sb.Draw(pixel, new Rectangle((int)(x - 2), (int)(y + AltoBarra + 1), AnchoBarra + 4, 1), marco);
            sb.Draw(pixel, new Rectangle((int)(x - 2), (int)(y - 2), 1, AltoBarra + 4), marco);
            sb.Draw(pixel, new Rectangle((int)(x + AnchoBarra + 1), (int)(y - 2), 1, AltoBarra + 4), marco);

            // EL RELLENO — SlateGray→Gold: gris plomo el campo vacío
            // (¡CASTIGA con −15%!), oro pleno el juicio pegado al cuerpo.
            int lleno = (int)(AnchoBarra * fraccion);
            if (lleno > 0)
            {
                Color cuerpo = Color.Lerp(Color.SlateGray, Color.Gold, fraccion);
                Color brillo = Color.Lerp(new Color(140, 146, 158), new Color(255, 236, 160), fraccion);
                sb.Draw(pixel, new Rectangle((int)x, (int)y, lleno, AltoBarra), cuerpo);
                sb.Draw(pixel, new Rectangle((int)x, (int)y, lleno, AltoBarra / 2), brillo * 0.85f);
            }

            // === EL TEXTO — «+X%» o «−X%» a la derecha de la barra ===
            int porcentaje = (int)MathF.Round(fuerza * 100f);
            if (porcentaje != _cachePorcentaje)
            {
                _cachePorcentaje = porcentaje;
                _cacheTexto = (porcentaje >= 0 ? "+" : "−") + Math.Abs(porcentaje) + "%";
                _cacheMedida = fuente.MeasureString(_cacheTexto) * 0.85f;
            }
            Color tinte = porcentaje >= 0
                ? new Color(255, 214, 110)
                : new Color(168, 172, 182);
            sb.DrawString(fuente, _cacheTexto,
                new Vector2(x + AnchoBarra + 6f, y - 1f), tinte,
                0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

            // === EL TÍTULO — «Fuerza del Estandarte» bajo la barra
            //     (localizado; fallback honesto si la clave falta). ===
            if (_cacheTitulo == null)
            {
                string t = Language.GetTextValue("Mods.AethonMod.HUD.FuerzaEstandarte");
                _cacheTitulo = string.IsNullOrEmpty(t) || t.StartsWith("Mods.")
                    ? "Fuerza del Estandarte" : t;
            }
            sb.DrawString(fuente, _cacheTitulo,
                new Vector2(x, y + AltoBarra + 3f),
                new Color(210, 190, 150) * 0.9f,
                0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }
    }
}
