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
    /// Usa UserInterface + UIState para cada UI (patron correcto de tModLoader).
    /// Esto garantiza que el renderizado funcione (no mas pantalla blanca).
    /// </summary>
    public class UISystem : ModSystem
    {
        // UserInterfaces (gestionan el estado y el renderizado)
        private UserInterface? _skillTreeInterface;
        private UserInterface? _codexInterface;

        // Estados (contienen los UIElements)
        internal UI.SkillTreeUIState? SkillTreeUI;
        internal UI.MemoryCodexUIState? CodexUI;
        internal UI.FragmentInfoBoxUI? FragmentInfoBoxUI;
        internal UI.BranchChoiceUI? BranchChoiceUI;

        public override void Load()
        {
            if (Main.dedServ) return;

            // Crear UserInterfaces
            _skillTreeInterface = new UserInterface();
            _codexInterface = new UserInterface();

            // Crear estados y activarlos
            SkillTreeUI = new UI.SkillTreeUIState();
            SkillTreeUI.Activate();

            CodexUI = new UI.MemoryCodexUIState();
            CodexUI.Activate();

            // La caja de info del fragmento (no usa UserInterface, se dibuja directamente)
            FragmentInfoBoxUI = new UI.FragmentInfoBoxUI();
            FragmentInfoBoxUI.Activate();

            // La eleccion de rama (sigue siendo directa, solo aparece una vez)
            BranchChoiceUI = new UI.BranchChoiceUI();
        }

        public override void Unload()
        {
            SkillTreeUI = null;
            CodexUI = null;
            FragmentInfoBoxUI = null;
            BranchChoiceUI = null;
            _skillTreeInterface = null;
            _codexInterface = null;
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
                    else
                    {
                        CodexUI?.Show();
                        if (CodexUI != null) _codexInterface?.SetState(CodexUI);
                    }
                }
                else if (sp != null && sp.IsImprinted && !sp.CodexUnlocked)
                {
                    Main.NewText("El Codex de Memoria no esta desbloqueado. Consigue un arma magica o de invocacion para despertarlo.",
                        new Color(180, 160, 220));
                }
                else if (sp != null && !sp.IsImprinted)
                {
                    Main.NewText("El Fragmento Genesis aun no tiene una rama. Derrota enemigos para despertarlo.",
                        new Color(180, 160, 220));
                }
            }

            // Update de las UserInterfaces (procesa input de la UI)
            if (_skillTreeInterface != null && SkillTreeUI?.IsVisible == true)
                _skillTreeInterface.Update(Main._drawInterfaceGameTime);
            if (_codexInterface != null && CodexUI?.IsVisible == true)
                _codexInterface.Update(Main._drawInterfaceGameTime);

            // === BLOQUEAR SCROLL DEL INVENTARIO MIENTRAS UI ESTA ABIERTA ===
            // Resetear el valor del scroll para que el juego vanilla no lo procese
            // (evita que el hotbar se mueva cuando hacemos zoom en el arbol)
            if ((SkillTreeUI?.IsVisible ?? false) || (CodexUI?.IsVisible ?? false))
            {
                Terraria.GameInput.PlayerInput.ScrollWheelValue = Terraria.GameInput.PlayerInput.ScrollWheelValueOld;
            }

            // Limpiar el estado de la UserInterface cuando la UI se oculta
            if (SkillTreeUI?.IsVisible != true) _skillTreeInterface?.SetState(null);
            if (CodexUI?.IsVisible != true) _codexInterface?.SetState(null);

            // BranchChoice update
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
            // Insertar despues del cursor del mouse para que se dibuje encima de todo
            int mouseLayerIdx = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseLayerIdx == -1) mouseLayerIdx = layers.Count;

            layers.Insert(mouseLayerIdx, new LegacyGameInterfaceLayer(
                "AethonMod: UIs",
                delegate
                {
                    try
                    {
                        // Caja de info del fragmento (siempre visible)
                        if (FragmentInfoBoxUI != null && FragmentInfoBoxUI.IsVisible)
                        {
                            FragmentInfoBoxUI.Draw(Main.spriteBatch);
                        }

                        // Eleccion de rama (modal)
                        BranchChoiceUI?.Draw();

                        // Arbol de habilidades (via UserInterface — patron correcto)
                        if (_skillTreeInterface != null && SkillTreeUI?.IsVisible == true)
                        {
                            var gt = Main._drawInterfaceGameTime;
                            if (gt != null) _skillTreeInterface.Draw(Main.spriteBatch, gt);
                        }

                        // Codex de memoria (via UserInterface)
                        if (_codexInterface != null && CodexUI?.IsVisible == true)
                        {
                            var gt = Main._drawInterfaceGameTime;
                            if (gt != null) _codexInterface.Draw(Main.spriteBatch, gt);
                        }
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
