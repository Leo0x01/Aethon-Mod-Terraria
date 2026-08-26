using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.Players;

namespace AethonMod.Content.Systems
{
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
            SkillTreeUI = null; CodexUI = null; XPBarUI = null; BranchChoiceUI = null;
        }

        public override void PostUpdateInput()
        {
            // Input de teclas (K, J)
            var config = ModContent.GetInstance<Content.AethonConfig>();
            if (config == null) return;

            if (Main.keyState.IsKeyDown(config.SkillTreeKey) &&
                !Main.oldKeyState.IsKeyDown(config.SkillTreeKey))
            {
                var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
                if (sp != null && sp.IsImprinted)
                {
                    if (SkillTreeUI?.IsVisible == true) SkillTreeUI.Hide();
                    else SkillTreeUI?.Show();
                }
            }
            if (Main.keyState.IsKeyDown(config.CodexKey) &&
                !Main.oldKeyState.IsKeyDown(config.CodexKey))
            {
                var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
                if (sp != null && sp.IsImprinted)
                {
                    if (CodexUI?.IsVisible == true) CodexUI.Hide();
                    else CodexUI?.Show();
                }
            }

            // Update de UIs
            XPBarUI?.Update();
            // SkillTreeUI no necesita Update separado (se actualiza en Draw)
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // Insertar en la capa mas alta posible para que se dibuje encima de todo
            layers.Add(new LegacyGameInterfaceLayer(
                "AethonMod: AllUI",
                delegate
                {
                    XPBarUI?.Draw();
                    BranchChoiceUI?.Draw();
                    SkillTreeUI?.Draw();
                    CodexUI?.Draw();
                    return true;
                },
                InterfaceScaleType.UI));
        }
    }
}
