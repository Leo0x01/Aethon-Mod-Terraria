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
    ///
    /// PATRÓN CORRECTO (descubierto tras investigación):
    /// 1. UIScrollBlockPlayer.PreUpdate() setea Player.mouseInterface = true
    ///    — Esto hace que Terraria NO procese clicks del mouse como input del juego
    ///    — El juego no ataca, no coloca bloques, no usa items
    /// 2. PostUpdateInput() — las UIs leen Main.mouseLeft/mouseRight directamente
    ///    — Como mouseInterface = true, el juego ya no consumió el click
    ///    — Las UIs pueden usar Main.mouseLeft normalmente
    /// 3. ModifyInterfaceLayers — solo DIBUJA
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

            // Toggle del codex (J)
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
            }

            // === LAS UIs PROCESAN EL INPUT AQUI ===
            // Como Player.mouseInterface = true fue seteado en PreUpdate,
            // el juego NO consumió Main.mouseLeft/mouseRight.
            // Las UIs pueden leerlos directamente.
            SkillTreeUI?.Update();
            CodexUI?.Update();
            BranchChoiceUI?.Update();

            // === RESETEAR SCROLL DESPUES DE QUE LAS UIs LO LEAN ===
            // Esto evita que el scroll del hotbar se mueva
            if (anyFullscreenUIOpen)
            {
                Terraria.GameInput.PlayerInput.ScrollWheelValue = Terraria.GameInput.PlayerInput.ScrollWheelValueOld;
            }
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

            // Desactivar capas vanilla cuando UI abierta
            if (anyUIOpen)
            {
                layers.RemoveAll(layer =>
                    layer.Name.Contains("Vanilla: Hotbar") ||
                    layer.Name.Contains("Vanilla: Inventory") ||
                    layer.Name.Contains("Vanilla: Player Buffs") ||
                    layer.Name.Contains("Vanilla: Resource Bars") ||
                    layer.Name.Contains("Vanilla: Tooltip"));
            }

            int mouseLayerIdx = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseLayerIdx == -1) mouseLayerIdx = layers.Count;

            layers.Insert(mouseLayerIdx, new LegacyGameInterfaceLayer(
                "AethonMod: UIs",
                delegate
                {
                    try
                    {
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
