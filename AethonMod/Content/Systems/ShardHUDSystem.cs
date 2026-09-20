using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.Globals;
using AethonMod.Content.Weapons;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// ShardHUDSystem — LA BARRA DORADA: el nivel y la XP del Grimorio al
    /// lado del hotbar, visible siempre que el libro esté en la BARRA
    /// RÁPIDA (los slots 0–9, el inventario visible de las teclas de
    /// número — sostenerlo también cuenta). Con el inventario abierto no
    /// se dibuja: el tooltip del libro ya cuenta la XP entera.
    ///
    /// GEOMETRÍA (leída del IL de GUIHotbarDrawInner, el dibujo real del
    /// hotbar de vanilla): primer slot en (20, 20), slots de 56px con
    /// stride de +4 — nueve slots a escala 0.75 (42px) + el seleccionado
    /// a escala 1 (56px) ≈ 470px de fila, fondo ≈ y=76. La barra vive
    /// justo debajo: x=20, y=80.
    ///
    /// EL PULSO: al cobrar XP (MarcarGanancia, llamado desde
    /// GlobalNPCXP.OnKill del jugador local) la barra LATE 90 ticks —
    /// brillo senoidal sobre el relleno dorado — y un "+XP" flotante se
    /// eleva y se funde. DETERMINISTA: cero Main.rand, todo es función
    /// de la edad del estado; la fase del latido es el tiempo vivo del
    /// pulso, no azar.
    /// Chip "HM ×2" mientras Main.hardMode: el indicador de que las
    /// CRIATURAS pagan doble (los jefes son XP fija — el tooltip del
    /// libro lo aclara).
    /// </summary>
    public class ShardHUDSystem : ModSystem
    {
        // === ESTADO DEL HUD (cliente) ===
        private static int _pulso = 0;           // ticks restantes de latido
        private static int _edadGanancia = -1;   // edad del "+XP" flotante (-1 = inactivo)
        private static int _ultimaGanancia = 0;

        public const int AnchoBarra = 470;       // la fila del hotbar entera
        public const int AltoBarra = 10;
        public const float XBarra = 20f;         // el borde izquierdo del hotbar
        public const float YBarra = 80f;         // justo bajo la fila de slots
        public const float YTexto = 94f;         // la línea de texto bajo la barra
        public const int TicksPulso = 90;
        public const int TicksGanancia = 70;

        /// <summary>
        /// La XP cobrada por una kill: enciende el pulso y suelta el
        /// "+XP" flotante. La llama GlobalNPCXP.OnKill cuando el killer
        /// es el jugador local y el libro está en su barra rápida.
        /// </summary>
        public static void MarcarGanancia(int xp)
        {
            _pulso = TicksPulso;
            _edadGanancia = 0;
            _ultimaGanancia = xp;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (Main.gameMenu) return;
            if (_pulso > 0) _pulso--;
            if (_edadGanancia >= 0)
            {
                _edadGanancia++;
                if (_edadGanancia > TicksGanancia) _edadGanancia = -1;
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // Justo DESPUÉS del hotbar: la barra es el vecino de abajo.
            int idx = layers.FindIndex(l => l.Name == "Vanilla: Hotbar");
            if (idx != -1)
            {
                layers.Insert(idx + 1, new LegacyGameInterfaceLayer(
                    "AethonMod: Barra XP Grimorio",
                    () => { DibujarBarra(); return true; },
                    InterfaceScaleType.UI));
            }
        }

        private static void DibujarBarra()
        {
            try
            {
                // Con el inventario abierto la fila 2 del inventario pisa
                // esta zona (DrawInventory: fila j=1 empieza en y≈68) y el
                // tooltip del libro ya muestra la XP: no se dibuja.
                if (Main.playerInventory || Main.gameMenu) return;
                Player p = Main.LocalPlayer;
                if (p == null || !p.active) return;

                // El libro debe estar en la BARRA RÁPIDA (0–9).
                ShardLevelItem sl = null;
                for (int i = 0; i < 10; i++)
                {
                    Item inv = p.inventory[i];
                    if (inv == null || inv.type != ModContent.ItemType<GrimoireEternal>())
                        continue;
                    sl = inv.GetGlobalItem<ShardLevelItem>();
                    break; // la primera copia es la que manda
                }
                if (sl == null) return;

                int xpNecesaria = sl.XPForNextLevel();
                float frac = xpNecesaria > 0 ? (float)sl.XP / xpNecesaria : 0f;
                frac = MathHelper.Clamp(frac, 0f, 1f);

                SpriteBatch sb = Main.spriteBatch;
                Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
                var font = Terraria.GameContent.FontAssets.MouseText.Value;

                int ancho = (int)MathHelper.Min(AnchoBarra, Main.screenWidth - 60);
                float x = XBarra;
                float y = YBarra;

                // === EL LATIDO: brillo senoidal mientras vive el pulso ===
                float fase = (TicksPulso - _pulso) * 0.22f;
                float latido = _pulso > 0
                    ? 0.5f + 0.5f * (float)System.Math.Sin(fase)
                    : 0f;

                // === FONDO y MARCO ===
                sb.Draw(pixel, new Rectangle((int)(x - 3), (int)(y - 3), ancho + 6, AltoBarra + 6),
                    new Color(12, 10, 20) * 0.62f);
                Color marco = new Color(120 + (int)(125 * latido), 96 + (int)(60 * latido), 40, 230);
                sb.Draw(pixel, new Rectangle((int)(x - 2), (int)(y - 2), ancho + 4, 1), marco);
                sb.Draw(pixel, new Rectangle((int)(x - 2), (int)(y + AltoBarra + 1), ancho + 4, 1), marco);
                sb.Draw(pixel, new Rectangle((int)(x - 2), (int)(y - 2), 1, AltoBarra + 4), marco);
                sb.Draw(pixel, new Rectangle((int)(x + ancho + 1), (int)(y - 2), 1, AltoBarra + 4), marco);

                // === RELLENO DORADO (dos tonos: cuerpo + brillo superior) ===
                int lleno = (int)(ancho * frac);
                if (lleno > 0)
                {
                    Color cuerpo = new Color(245, 196, 81);
                    Color brillo = new Color(255, 235, 150);
                    if (latido > 0f)
                    {
                        // el pulso empuja el dorado hacia el blanco-oro
                        cuerpo = Color.Lerp(cuerpo, new Color(255, 244, 190), latido * 0.6f);
                        brillo = Color.Lerp(brillo, Color.White, latido * 0.5f);
                    }
                    sb.Draw(pixel, new Rectangle((int)x, (int)y, lleno, AltoBarra), cuerpo);
                    sb.Draw(pixel, new Rectangle((int)x, (int)y, lleno, AltoBarra / 2), brillo * 0.85f);
                }

                // === TEXTO: nivel a la izquierda, XP a la derecha ===
                string txtNivel = $"Grimorio · Nv {sl.Level}";
                sb.DrawString(font, txtNivel, new Vector2(x + 2f, YTexto),
                    new Color(245, 196, 81) * 0.95f, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

                string txtXP = $"{sl.XP} / {xpNecesaria} XP";
                if (Main.hardMode) txtXP += "   HM ×2";
                Vector2 medXP = font.MeasureString(txtXP) * 0.85f;
                sb.DrawString(font, txtXP, new Vector2(x + ancho - medXP.X, YTexto),
                    (Main.hardMode ? new Color(255, 160, 90) : new Color(220, 220, 220)) * 0.9f,
                    0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

                // === EL "+XP" FLOTANTE: nace sobre la barra, sube y se funde ===
                if (_edadGanancia >= 0 && _edadGanancia <= TicksGanancia)
                {
                    float alfaG = 1f - (float)_edadGanancia / TicksGanancia;
                    string txtGan = $"+{_ultimaGanancia}";
                    Vector2 posG = new Vector2(
                        x + ancho - 70f,
                        y - 6f - _edadGanancia * 0.45f);
                    sb.DrawString(font, txtGan, posG + new Vector2(1.5f, 1.5f),
                        Color.Black * (alfaG * 0.7f), 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
                    sb.DrawString(font, txtGan, posG,
                        new Color(255, 222, 120) * alfaG, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
                }
            }
            catch { }
        }

        public override void OnWorldUnload() { _pulso = 0; _edadGanancia = -1; }
        public override void Unload() { _pulso = 0; _edadGanancia = -1; }
    }
}
