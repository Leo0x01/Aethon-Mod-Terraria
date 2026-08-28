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
    /// ORDEN DE EJECUCIÓN CRÍTICO:
    /// 1. PreUpdatePlayers (UIScrollBlockPlayer) — bloquea input del juego
    /// 2. Juego procesa input (ya bloqueado, no hace nada)
    /// 3. PostUpdateInput (UISystem) — toggle de teclas + UIs procesan input
    /// 4. ModifyInterfaceLayers — solo DIBUJA, no procesa input
    ///
    /// El truco: las UIs procesan su input en PostUpdateInput (paso 3),
    /// que corre DESPUÉS de que UIScrollBlockPlayer bloqueó el input del juego.
    /// Pero las UIs leen el estado ORIGINAL del mouse que el juego guardó
    /// antes de que UIScrollBlockPlayer lo reseteara.
    ///
    /// Para que esto funcione, UIScrollBlockPlayer debe guardar una COPIA
    /// del estado del mouse/teclado antes de resetearlo.
    /// </summary>
    public class UISystem : ModSystem
    {
        public UI.SkillTreeUI? SkillTreeUI;
        public UI.MemoryCodexUI? CodexUI;
        public UI.BranchChoiceUI? BranchChoiceUI;

        // Copia del estado del mouse ANTES de que UIScrollBlockPlayer lo resetee
        public static bool MouseLeft;
        public static bool MouseRight;
        public static bool MouseLeftRelease;
        public static int ScrollDelta;
        public static int MouseX;
        public static int MouseY;

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

        /// <summary>
        /// Se llama en PreUpdate (antes de que el juego procese input).
        /// Guardamos el estado del mouse para que las UIs lo puedan usar
        /// después, y luego bloqueamos todo para que el juego no responda.
        /// </summary>
        public static void CaptureAndBlockInput()
        {
            // Guardar estado del mouse ANTES de que el juego lo procese
            MouseLeft = Main.mouseLeft;
            MouseRight = Main.mouseRight;
            MouseLeftRelease = Main.mouseLeftRelease;
            ScrollDelta = Terraria.GameInput.PlayerInput.ScrollWheelValue - Terraria.GameInput.PlayerInput.ScrollWheelValueOld;
            MouseX = Main.mouseX;
            MouseY = Main.mouseY;

            // NO resetear Main.mouseLeft/Right aqui — el flag Main.playerInventory=true
            // se encarga de que el juego no procese clicks del mundo.
            // Las UIs leen UISystem.MouseLeft/Right que es la copia guardada.
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
            }

            // === LAS UIs PROCESAN EL INPUT AQUI ===
            // Usan UISystem.MouseLeft, MouseRight, etc. (copia guardada en PreUpdate)
            SkillTreeUI?.Update();
            CodexUI?.Update();
            BranchChoiceUI?.Update();

            // === RESETEAR SCROLL DELTA DESPUES DE QUE LAS UIs LO LEAN ===
            // Esto evita que el scroll se procese multiples veces
            ScrollDelta = 0;

            // === BLOQUEAR SCROLL DEL JUEGO ===
            // Resetear aqui (despues de que las UIs ya leyeron ScrollDelta)
            // para que el hotbar no se mueva
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
                        // SOLO DIBUJAR — el input se procesa en PostUpdateInput
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
