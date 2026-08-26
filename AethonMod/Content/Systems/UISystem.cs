using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.Players;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Sistema central de UI del mod Aethon.
    /// Maneja:
    /// - Carga/descarga de las UIs (SkillTree, Codex, XPBar, BranchChoice)
    /// - Input de teclas (K = arbol, J = codex)
    /// - Capa de renderizado (ModifyInterfaceLayers)
    /// </summary>
    public class UISystem : ModSystem
    {
        internal UI.SkillTreeUIState? SkillTreeUI;
        internal UI.MemoryCodexUIState? CodexUI;
        internal UI.ShardXPBarUI? XPBarUI;
        internal UI.BranchChoiceUI? BranchChoiceUI;

        public override void Load()
        {
            if (Main.dedServ) return;
            SkillTreeUI = new UI.SkillTreeUIState();
            CodexUI = new UI.MemoryCodexUIState();
            XPBarUI = new UI.ShardXPBarUI();
            BranchChoiceUI = new UI.BranchChoiceUI();
        }

        public override void Unload()
        {
            SkillTreeUI = null;
            CodexUI = null;
            XPBarUI = null;
            BranchChoiceUI = null;
        }

        public override void PostUpdateInput()
        {
            // Input de teclas (K = arbol, J = codex)
            var config = ModContent.GetInstance<Content.AethonConfig>();
            if (config == null) return;

            // Si alguna UI principal esta abierta, no procesar apertura de otra (evita conflictos)
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

            // Toggle del codex de memoria (J)
            if (Main.keyState.IsKeyDown(config.CodexKey) &&
                !Main.oldKeyState.IsKeyDown(config.CodexKey))
            {
                var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
                if (sp != null && sp.IsImprinted && !anyFullscreenUIOpenExcept(CodexUI))
                {
                    if (CodexUI?.IsVisible == true) CodexUI.Hide();
                    else CodexUI?.Show();
                }
                else if (sp != null && !sp.IsImprinted)
                {
                    Main.NewText("El Fragmento Genesis aun no tiene una rama. Derrota enemigos para despertarlo.",
                        new Color(180, 160, 220));
                }
            }

            // Update de la barra de XP (siempre visible)
            XPBarUI?.Update();
        }

        /// <summary>True si hay una UI abierta que NO sea la especificada.</summary>
        private bool anyFullscreenUIOpenExcept(object? except)
        {
            if (except != SkillTreeUI && (SkillTreeUI?.IsVisible ?? false)) return true;
            if (except != CodexUI && (CodexUI?.IsVisible ?? false)) return true;
            if (except != BranchChoiceUI && (BranchChoiceUI?.IsVisible ?? false)) return true;
            return false;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // Insertar despues del cursor del mouse para que se dibuje encima de todo.
            int mouseLayerIdx = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseLayerIdx == -1) mouseLayerIdx = layers.Count;

            layers.Insert(mouseLayerIdx, new LegacyGameInterfaceLayer(
                "AethonMod: UIs",
                delegate
                {
                    try
                    {
                        XPBarUI?.Draw();
                        BranchChoiceUI?.Draw();
                        SkillTreeUI?.Draw();
                        CodexUI?.Draw();
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
