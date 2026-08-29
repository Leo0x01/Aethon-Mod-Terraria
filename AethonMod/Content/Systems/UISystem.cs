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
    /// Usa UserInterface + UIState (patrón nativo de tModLoader).
    /// Player.mouseInterface = true bloquea el input del juego.
    /// </summary>
    public class UISystem : ModSystem
    {
        private UserInterface? _skillTreeInterface;
        public UI.SkillTreeUIState? SkillTreeUI;
        public UI.MemoryCodexUI? CodexUI;
        public UI.BranchChoiceUI? BranchChoiceUI;

        public override void Load()
        {
            if (Main.dedServ) return;
            _skillTreeInterface = new UserInterface();
            SkillTreeUI = new UI.SkillTreeUIState();
            SkillTreeUI.Activate();
            CodexUI = new UI.MemoryCodexUI();
            BranchChoiceUI = new UI.BranchChoiceUI();
        }

        public override void Unload()
        {
            SkillTreeUI = null;
            CodexUI = null;
            BranchChoiceUI = null;
            _skillTreeInterface = null;
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
                    else
                    {
                        SkillTreeUI?.Show();
                        if (SkillTreeUI != null) _skillTreeInterface?.SetState(SkillTreeUI);
                    }
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
            }

            // === LAS UIs PROCESAN EL INPUT ===
            // Como Player.mouseInterface = true, el juego no consumió los clicks.
            // UserInterface.Update procesa OnClick, OnMouseOver, etc. nativamente.
            if (_skillTreeInterface != null && SkillTreeUI?.IsVisible == true)
                _skillTreeInterface.Update(Main._drawInterfaceGameTime);
            if (SkillTreeUI?.IsVisible != true) _skillTreeInterface?.SetState(null);

            CodexUI?.Update();
            BranchChoiceUI?.Update();

            // === RESETEAR SCROLL DESPUES DE QUE LAS UIs LO LEAN ===
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
                        // Árbol via UserInterface (procesa clicks nativamente)
                        if (_skillTreeInterface != null && SkillTreeUI?.IsVisible == true)
                        {
                            var gt = Main._drawInterfaceGameTime;
                            if (gt != null) _skillTreeInterface.Draw(Main.spriteBatch, gt);
                        }
                        // Codex y BranchChoice via dibujo directo
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
