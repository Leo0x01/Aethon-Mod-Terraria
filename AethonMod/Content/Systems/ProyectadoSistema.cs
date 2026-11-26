using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  ProyectadoSistema — v6.177 — LA MÁQUINA DEL VISOR DEL PROYECTADO.
    //
    //  LA TECLA (el plano): «Presiona X para proyectarte de verdad» —
    //  el keybind propio del mod (X por defecto).
    //
    //  LA BARRA DE ENERGÍA PROYECTADA: la propia del visor (nada de
    //  maná): se dibuja junto al inventario cuando el portador lleva el
    //  visor (o mientras recarga), con el NÚMERO vivo y la palabra
    //  INFINITA para el que se bajó la luna. La tinta: el rojo del meme.
    //
    //  LA PANTALLA ROJA: durante la proyección grande TODO se tiñe —
    //  «se te encienden los ojos y sueltas un rayo enorme»: el mundo
    //  entero ve la proyección por tus ojos.
    // ======================================================================
    public class ProyectadoSistema : ModSystem
    {
        public static ModKeybind TeclaProyectarse { get; private set; }

        /// <summary>La tinta roja de la proyección (0-1: crece con el rayo grande).</summary>
        public static float TintaRoja;

        /// <summary>El alfa de la barra de energía (0-1).</summary>
        public static float AlfaBarra;

        public override void Load()
        {
            TeclaProyectarse = KeybindLoader.RegisterKeybind(Mod, "RayoProyectado", "X");
        }

        public override void Unload()
        {
            TeclaProyectarse = null;
            TintaRoja = 0f;
            AlfaBarra = 0f;
        }

        // === EL PULSO: la tinta y el alfa de la barra ===
        public override void PostUpdateEverything()
        {
            if (Main.dedServ || Main.gameMenu) { TintaRoja = 0f; AlfaBarra = 0f; return; }
            try
            {
                bool proyectando = false;
                int tipoGrande = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.Investigaciones.RayoProyectado>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p != null && p.active && p.type == tipoGrande &&
                        p.localAI[0] >= 213f && p.localAI[0] < 351f &&
                        p.Center.DistanceSQ(Main.LocalPlayer.Center) < 9000000f)
                    { proyectando = true; break; }
                }
                TintaRoja = TintaRoja < (proyectando ? 1f : 0f)
                    ? MathHelper.Min(proyectando ? 1f : 0f, TintaRoja + (proyectando ? 0.08f : 0.035f))
                    : MathHelper.Max(proyectando ? 1f : 0f, TintaRoja - (proyectando ? -0.08f : 0.035f));

                Player yo = Main.LocalPlayer;
                var pr = yo?.GetModPlayer<global::AethonMod.Content.Players.ProyectadoPlayer>();
                bool quiere = yo != null && yo.active && !yo.dead && (
                    yo.HeldItem.type == ModContent.ItemType
                        <global::AethonMod.Content.Items.Investigaciones.VisorDelProyectado>() ||
                    (pr != null && !pr.Infinito && pr.Energia < pr.EnergiaMax - 0.5f));
                AlfaBarra = MathHelper.Clamp(AlfaBarra + (quiere ? 0.1f : -0.05f), 0f, 1f);
            }
            catch (System.Exception exRegistro) { RegistroSistema.Fallo("ProyectadoSistema", exRegistro); }
        }

        // === LAS CAPAS DE LA INTERFAZ: la barra + la tinta ===
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int idxBarra = layers.FindIndex(l => l.Name == "Vanilla: Inventory");
            if (idxBarra >= 0)
                layers.Insert(idxBarra, new LegacyGameInterfaceLayer("AethonMod: Energía proyectada",
                    () => { DibujarBarra(); return true; }, InterfaceScaleType.UI));
            if (TintaRoja > 0.01f)
                layers.Insert(0, new LegacyGameInterfaceLayer("AethonMod: La proyección lo tiñe todo",
                    () =>
                    {
                        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                            new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                            new Color(255, 25, 15) * (0.32f * TintaRoja));
                        return true;
                    }, (InterfaceScaleType)2));
        }

        private static void DibujarBarra()
        {
            if (AlfaBarra <= 0.01f || Main.gameMenu || Main.playerInventory) return;
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || yo.dead) return;
                var pr = yo.GetModPlayer<global::AethonMod.Content.Players.ProyectadoPlayer>();
                if (pr == null) return;
                var fuente = Terraria.GameContent.FontAssets.MouseText.Value;
                var px = TextureAssets.MagicPixel.Value;

                // LA BARRA: junto a los hechizos (bajo la barra de maná de vanilla)
                float w = 172f, h = 10f;
                float x = Main.screenWidth / 2f - w / 2f;
                float y = Main.screenHeight - 118f;   // la fila de las barritas de buff
                Rectangle marco = new((int)x - 2, (int)y - 2, (int)w + 4, (int)h + 4);
                Main.spriteBatch.Draw(px, marco, Color.Black * (0.62f * AlfaBarra));
                Rectangle fondo = new((int)x, (int)y, (int)w, (int)h);
                Main.spriteBatch.Draw(px, fondo, new Color(40, 12, 16) * (0.85f * AlfaBarra));

                if (pr.Infinito)
                {
                    // INFINITA: la barra llena con el brillo del meme
                    Main.spriteBatch.Draw(px, fondo, new Color(255, 60, 50) * (0.30f * AlfaBarra));
                    string texto = Language.GetTextValue("Mods.AethonMod.Proyectado.HudInfinita");
                    Vector2 med = fuente.MeasureString(texto) * 0.7f;
                    Utils.DrawBorderString(Main.spriteBatch, texto,
                        new Vector2(x + w / 2f, y + h / 2f), Color.White * AlfaBarra, 0.7f, 0.5f, 0.5f);
                    return;
                }

                // EL LLENADO: el rojo de la proyección (más pálido si AGOTADO)
                float fr = MathHelper.Clamp(pr.Energia / pr.EnergiaMax, 0f, 1f);
                Color tinta = pr.Agotado ? new Color(140, 40, 40) : new Color(235, 55, 40);
                Rectangle lleno = new((int)x, (int)y, (int)(w * fr), (int)h);
                Main.spriteBatch.Draw(px, lleno, tinta * (0.9f * AlfaBarra));
                // la línea de la recarga (la marca de lo agotado)
                float marca25 = 25f / pr.EnergiaMax;
                Main.spriteBatch.Draw(px, new Rectangle((int)(x + w * marca25) - 1, (int)y, 2, (int)h),
                    new Color(255, 220, 200) * (0.55f * AlfaBarra));
                // EL NÚMERO vivo (la desesperación es exacta)
                string num = $"{(int)pr.Energia}/{(int)pr.EnergiaMax}";
                Utils.DrawBorderString(Main.spriteBatch, num,
                    new Vector2(x + w / 2f, y + h / 2f - 1f), Color.White * AlfaBarra, 0.72f, 0.5f, 0.5f);
                // EL ENFRIAMIENTO de la X (la pistilla de la proyección)
                if (pr.Enfriamiento > 0)
                {
                    string cd = Language.GetTextValue("Mods.AethonMod.Proyectado.HudEnfriamiento",
                        pr.Enfriamiento / 60 + 1);
                    Utils.DrawBorderString(Main.spriteBatch, cd, new Vector2(x + w + 12f, y + h / 2f),
                        new Color(255, 200, 190) * AlfaBarra, 0.72f, 0f, 0.5f);
                }
            }
            catch (System.Exception exRegistro) { RegistroSistema.Fallo("ProyectadoSistema", exRegistro); }
        }
    }
}
