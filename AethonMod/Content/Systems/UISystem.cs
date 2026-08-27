using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.Players;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Sistema central de UI del mod Aethon.
    /// Todas las UIs se dibujan directamente con sb.Draw() (sin UserInterface)
    /// para evitar los cuadros blancos. Mismo patron que BranchChoiceUI.
    /// </summary>
    public class UISystem : ModSystem
    {
        public UI.SkillTreeUI? SkillTreeUI;
        public UI.MemoryCodexUI? CodexUI;
        public UI.BranchChoiceUI? BranchChoiceUI;

        public override void Load()
        {
            if (Main.dedServ) return;
            SkillTreeUI = new UI.SkillTreeUI();
            CodexUI = new UI.MemoryCodexUI();
            BranchChoiceUI = new UI.BranchChoiceUI();
        }

        public override void Unload()
        {
            SkillTreeUI = null;
            CodexUI = null;
            BranchChoiceUI = null;
        }

        public override void PostUpdateInput()
        {
            var config = ModContent.GetInstance<Content.AethonConfig>();
            if (config == null) return;

            bool anyFullscreenUIOpen = (SkillTreeUI?.IsVisible ?? false) || (CodexUI?.IsVisible ?? false) || (BranchChoiceUI?.IsVisible ?? false);

            // Toggle del arbol de habilidades (K)
            if (Main.keyState.IsKeyDown(config.SkillTreeKey) &&
                !Main.oldKeyState.IsKeyDown(config.SkillTreeKey))
            {
                var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
                if (sp != null && sp.IsImprinted && !anyFullscreenUIOpenExcept(SkillTreeUI))
                {
                    if (SkillTreeUI?.IsVisible == true) SkillTreeUI.Hide();
                    else SkillTreeUI?.Show();
                }
                else if (sp != null && !sp.IsImprinted)
                {
                    Main.NewText("El Fragmento Genesis aun no tiene una rama. Derrota enemigos para despertarlo.",
                        new Color(180, 160, 220));
                }
            }

            // Toggle del codex (J) — solo si esta desbloqueado
            if (Main.keyState.IsKeyDown(config.CodexKey) &&
                !Main.oldKeyState.IsKeyDown(config.CodexKey))
            {
                var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
                if (sp != null && sp.IsImprinted && sp.CodexUnlocked && !anyFullscreenUIOpenExcept(CodexUI))
                {
                    if (CodexUI?.IsVisible == true) CodexUI.Hide();
                    else CodexUI?.Show();
                }
                else if (sp != null && sp.IsImprinted && !sp.CodexUnlocked)
                {
                    Main.NewText("El Codex de Memoria no esta desbloqueado. Consigue un arma magica o de invocacion.",
                        new Color(180, 160, 220));
                }
                else if (sp != null && !sp.IsImprinted)
                {
                    Main.NewText("El Fragmento Genesis aun no tiene una rama.",
                        new Color(180, 160, 220));
                }
            }

            // Update de las UIs
            SkillTreeUI?.Update();
            CodexUI?.Update();
            BranchChoiceUI?.Update();
        }

        private bool anyFullscreenUIOpenExcept(object? except)
        {
            if (except != SkillTreeUI && (SkillTreeUI?.IsVisible ?? false)) return true;
            if (except != CodexUI && (CodexUI?.IsVisible ?? false)) return true;
            if (except != BranchChoiceUI && (BranchChoiceUI?.IsVisible ?? false)) return true;
            return false;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            bool anyUIOpen = (SkillTreeUI?.IsVisible ?? false) || (CodexUI?.IsVisible ?? false) || (BranchChoiceUI?.IsVisible ?? false);

            // === DESACTIVAR INTERFAZ DEL JUEGO (como el bestiario) ===
            if (anyUIOpen)
            {
                layers.RemoveAll(layer =>
                    layer.Name.Contains("Vanilla: Hotbar") ||
                    layer.Name.Contains("Vanilla: Inventory") ||
                    layer.Name.Contains("Vanilla: Player Buffs") ||
                    layer.Name.Contains("Vanilla: Resource Bars") ||
                    layer.Name.Contains("Vanilla: Tooltip"));
            }

            // Insertar despues del cursor del mouse
            int mouseLayerIdx = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseLayerIdx == -1) mouseLayerIdx = layers.Count;

            layers.Insert(mouseLayerIdx, new LegacyGameInterfaceLayer(
                "AethonMod: UIs",
                delegate
                {
                    try
                    {
                        // Todas las UIs se dibujan directamente con sb.Draw()
                        // (no UserInterface, no UIState — patron directo)
                        SkillTreeUI?.Draw();
                        CodexUI?.Draw();
                        BranchChoiceUI?.Draw();
                    }
                    catch (System.Exception ex)
                    {
                        ModContent.GetInstance<AethonMod>()?.Logger?.Error("UISystem render error", ex);
                    }
                    return true;
                },
                InterfaceScaleType.UI));
        }
    }
}
