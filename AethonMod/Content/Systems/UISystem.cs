using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.Players;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// UISystem — sigue el patrón exacto de AnRPG:
    /// 1. Crear UserInterface + UIState una sola vez en Load()
    /// 2. SIEMPRE SetState (nunca null)
    /// 3. En ModifyInterfaceLayers: si visible, Update() + Draw()
    /// 4. mouseInterface = true en DrawSelf del UIState (no en ModPlayer)
    /// </summary>
    public class UISystem : ModSystem
    {
        private UserInterface _skillTreeInterface = new();
        private UserInterface _codexInterface = new();

        public UI.SkillTreeUIState? SkillTreeUI;
        public UI.MemoryCodexUI? CodexUI;
        public UI.BranchChoiceUI? BranchChoiceUI;

        public override void Load()
        {
            if (Main.dedServ) return;
            SkillTreeUI = new UI.SkillTreeUIState();
            SkillTreeUI.Activate();
            _skillTreeInterface.SetState(SkillTreeUI);

            CodexUI = new UI.MemoryCodexUI();
            BranchChoiceUI = new UI.BranchChoiceUI();
        }

        public override void Unload()
        {
            SkillTreeUI = null; CodexUI = null; BranchChoiceUI = null;
        }

        public override void PostUpdateInput()
        {
            var config = ModContent.GetInstance<Content.AethonConfig>();
            if (config == null) return;

            // Toggle K
            if (Main.keyState.IsKeyDown(config.SkillTreeKey) && !Main.oldKeyState.IsKeyDown(config.SkillTreeKey))
            {
                var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
                if (sp != null && sp.IsImprinted && !anyOtherOpen(SkillTreeUI))
                {
                    if (SkillTreeUI?.IsVisible == true) SkillTreeUI.Hide();
                    else SkillTreeUI?.Show();
                }
            }
            // Toggle J
            if (Main.keyState.IsKeyDown(config.CodexKey) && !Main.oldKeyState.IsKeyDown(config.CodexKey))
            {
                var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
                if (sp != null && sp.IsImprinted && sp.CodexUnlocked && !anyOtherOpen(CodexUI))
                {
                    if (CodexUI?.IsVisible == true) CodexUI.Hide();
                    else CodexUI?.Show();
                }
            }

            // Las UIs que usan dibujo directo (Codex, BranchChoice) procesan input aquí
            CodexUI?.Update();
            BranchChoiceUI?.Update();
        }

        private bool anyOtherOpen(object? except)
        {
            if (except != SkillTreeUI && (SkillTreeUI?.IsVisible ?? false)) return true;
            if (except != CodexUI && (CodexUI?.IsVisible ?? false)) return true;
            if (except != BranchChoiceUI && (BranchChoiceUI?.IsVisible ?? false)) return true;
            return false;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // AnRPG inserta en "Vanilla: Interface Logic 2" — hacemos lo mismo
            int insertIdx = layers.FindIndex(l => l.Name.Equals("Vanilla: Interface Logic 2"));
            if (insertIdx == -1) insertIdx = layers.Count;

            layers.Insert(insertIdx, new LegacyGameInterfaceLayer("AethonMod: Skill Tree",
                () =>
                {
                    try
                    {
                        if (SkillTreeUI?.IsVisible == true)
                        {
                            // AnRPG patrón: Update + Draw
                            _skillTreeInterface.Update(Main._drawInterfaceGameTime);
                            SkillTreeUI.Draw(Main.spriteBatch);
                        }
                    }
                    catch (System.Exception ex) { ModContent.GetInstance<AethonMod>()?.Logger?.Error("SkillTree render error", ex); }
                    return true;
                }, InterfaceScaleType.UI));

            layers.Insert(insertIdx, new LegacyGameInterfaceLayer("AethonMod: Codex + Branch",
                () =>
                {
                    try
                    {
                        CodexUI?.Draw();
                        BranchChoiceUI?.Draw();
                    }
                    catch (System.Exception ex) { ModContent.GetInstance<AethonMod>()?.Logger?.Error("Codex render error", ex); }
                    return true;
                }, InterfaceScaleType.UI));
        }
    }
}
