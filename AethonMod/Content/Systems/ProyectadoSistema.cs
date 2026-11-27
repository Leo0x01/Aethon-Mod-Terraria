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
    //  ProyectadoSistema — v6.179 — LA MÁQUINA DEL VISOR DEL PROYECTADO.
    //
    //  LA LETRA (sesión 32): «solo necesito el arma visor del proyectado
    //  cualquier otra cosa del mod puedes borrarlo, ademas que el arma
    //  ya este en su maxima capacidad y pueda cambiar entre las
    //  diferentes fases».
    //
    //  LA TECLA X (la de siempre) — y el clic derecho con el visor en la
    //  mano — CAMBIAN LA FASE del arma (ProyectadoPlayer.CambiarFase:
    //  fase 1 el rayo continuo · fase 2 la proyección).
    //
    //  v6.179 — EL HUD DE LA FASE: la barra de energía MURIÓ (el arma
    //  nace a su máxima capacidad — INFINITA: nada que administrar) —
    //  en su lugar, el rótulo de la FASE VIVA junto a los hechizos
    //  mientras el visor está en la mano (la fase se VE sin abrir nada).
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

        /// <summary>El alfa del rótulo de fase (0-1).</summary>
        public static float AlfaFase;

        public override void Load()
        {
            TeclaProyectarse = KeybindLoader.RegisterKeybind(Mod, "RayoProyectado", "X");
        }

        public override void Unload()
        {
            TeclaProyectarse = null;
            TintaRoja = 0f;
            AlfaFase = 0f;
        }

        // === EL PULSO: la tinta y el alfa del rótulo ===
        public override void PostUpdateEverything()
        {
            if (Main.dedServ || Main.gameMenu) { TintaRoja = 0f; AlfaFase = 0f; return; }
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
                bool quiere = yo != null && yo.active && !yo.dead && yo.HeldItem.type == ModContent.ItemType
                    <global::AethonMod.Content.Items.Investigaciones.VisorDelProyectado>();
                AlfaFase = MathHelper.Clamp(AlfaFase + (quiere ? 0.1f : -0.05f), 0f, 1f);
            }
            catch (System.Exception exRegistro) { RegistroSistema.Fallo("ProyectadoSistema", exRegistro); }
        }

        // === LAS CAPAS DE LA INTERFAZ: el rótulo de fase + la tinta ===
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int idxFase = layers.FindIndex(l => l.Name == "Vanilla: Inventory");
            if (idxFase >= 0)
                layers.Insert(idxFase, new LegacyGameInterfaceLayer("AethonMod: Fase proyectada",
                    () => { DibujarFase(); return true; }, InterfaceScaleType.UI));
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

        private static void DibujarFase()
        {
            if (AlfaFase <= 0.01f || Main.gameMenu || Main.playerInventory) return;
            try
            {
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active || yo.dead) return;
                var pr = yo.GetModPlayer<global::AethonMod.Content.Players.ProyectadoPlayer>();
                if (pr == null) return;
                var fuente = Terraria.GameContent.FontAssets.MouseText.Value;
                var px = TextureAssets.MagicPixel.Value;

                // EL RÓTULO DE LA FASE VIVA: junto a los hechizos (donde
                // vivía la barra de energía — su heredero de 12 px)
                string texto = Language.GetTextValue("Mods.AethonMod.Proyectado.FaseViva", pr.Fase,
                    Language.GetTextValue("Mods.AethonMod.Proyectado.Fase" + pr.Fase));
                float w = 172f, h = 12f;
                float x = Main.screenWidth / 2f - w / 2f;
                float y = Main.screenHeight - 122f;
                Rectangle marco = new((int)x - 2, (int)y - 2, (int)w + 4, (int)h + 4);
                Main.spriteBatch.Draw(px, marco, Color.Black * (0.55f * AlfaFase));
                Rectangle fondo = new((int)x, (int)y, (int)w, (int)h);
                Main.spriteBatch.Draw(px, fondo, new Color(40, 12, 16) * (0.85f * AlfaFase));
                // la tinta de la fase viva: la 1 en el rojo del rayo fino, la 2 en el rojo del grande
                Main.spriteBatch.Draw(px, fondo, new Color(255, 60, 50) * (0.25f * AlfaFase));
                Utils.DrawBorderString(Main.spriteBatch, texto,
                    new Vector2(x + w / 2f, y + h / 2f), Color.White * AlfaFase, 0.85f, 0.5f, 0.5f);
            }
            catch (System.Exception exRegistro) { RegistroSistema.Fallo("ProyectadoSistema", exRegistro); }
        }
    }
}
