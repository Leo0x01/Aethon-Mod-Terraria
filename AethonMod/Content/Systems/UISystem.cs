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
    /// UISystem — sigue EXACTAMENTE el patrón de AnRPG:
    /// 1. Crear UserInterface + UIState una sola vez en Load()
    /// 2. SIEMPRE SetState (nunca null)
    /// 3. En ModifyInterfaceLayers: si visible, Update() + Draw()
    /// </summary>
    public class UISystem : ModSystem
    {
        public UserInterface customSkillTree;
        public Content.SkillTree.UI.SkillTreeUi SkillTreeUI;
        public UI.MemoryCodexUI? CodexUI;
        public UI.BranchChoiceUI? BranchChoiceUI;

        public override void Load()
        {
            if (Main.dedServ) return;

            // Igual que AnRPG: crear una vez, SetState siempre
            customSkillTree = new UserInterface();
            SkillTreeUI = new Content.SkillTree.UI.SkillTreeUi();
            SkillTreeUI.Activate();
            customSkillTree.SetState(SkillTreeUI);

            CodexUI = new UI.MemoryCodexUI();
            BranchChoiceUI = new UI.BranchChoiceUI();
        }

        public override void Unload()
        {
            SkillTreeUI = null; CodexUI = null; BranchChoiceUI = null; customSkillTree = null;
        }

        public override void PostUpdateInput()
        {
            var config = ModContent.GetInstance<Content.AethonConfig>();
            if (config == null) return;

            // Toggle K (igual que AnRPG: JustPressed -> visible = !visible -> LoadSkillTree)
            if (Main.keyState.IsKeyDown(config.SkillTreeKey) && !Main.oldKeyState.IsKeyDown(config.SkillTreeKey))
            {
                var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
                if (sp != null && sp.IsImprinted && !anyOtherOpen(SkillTreeUI))
                {
                    Content.SkillTree.UI.SkillTreeUi.visible = !Content.SkillTree.UI.SkillTreeUi.visible;
                    if (Content.SkillTree.UI.SkillTreeUi.visible) Content.SkillTree.UI.SkillTreeUi.Instance.LoadSkillTree();
                }
            }

            // Toggle J
            if (Main.keyState.IsKeyDown(config.CodexKey) && !Main.oldKeyState.IsKeyDown(config.CodexKey))
            {
                var sp = Main.LocalPlayer?.GetModPlayer<ShardPlayer>();
                if (sp != null && sp.IsImprinted && sp.CodexUnlocked && !anyOtherOpen(CodexUI))
                {
                    if (CodexUI?.IsVisible == true) CodexUI?.Hide();
                    else CodexUI?.Show();
                }
            }

            // UIs con dibujo directo procesan input aquí
            CodexUI?.Update();
            BranchChoiceUI?.Update();
        }

        private bool anyOtherOpen(object? except)
        {
            if (except != SkillTreeUI && Content.SkillTree.UI.SkillTreeUi.visible) return true;
            if (except != CodexUI && (CodexUI?.IsVisible ?? false)) return true;
            if (except != BranchChoiceUI && (BranchChoiceUI?.IsVisible ?? false)) return true;
            return false;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // AnRPG inserta en "Vanilla: Interface Logic 2"
            int insertIdx = layers.FindIndex(l => l.Name.Equals("Vanilla: Interface Logic 2"));
            if (insertIdx == -1) insertIdx = layers.Count;

            // Skill Tree — igual que AnRPG: Update + Draw cuando visible
            layers.Insert(insertIdx, new LegacyGameInterfaceLayer("AethonMod: Skill Tree",
                () =>
                {
                    try
                    {
                        if (SkillTreeUI != null && Content.SkillTree.UI.SkillTreeUi.visible)
                        {
                            customSkillTree.Update(Main._drawInterfaceGameTime);
                            SkillTreeUI.Draw(Main.spriteBatch);
                        }
                    }
                    catch (System.Exception ex) { ModContent.GetInstance<AethonMod>()?.Logger?.Error("SkillTree error", ex); }
                    return true;
                }, InterfaceScaleType.UI));

            // Codex + BranchChoice (dibujo directo)
            layers.Insert(insertIdx, new LegacyGameInterfaceLayer("AethonMod: Codex + Branch",
                () =>
                {
                    try { CodexUI?.Draw(); BranchChoiceUI?.Draw(); }
                    catch (System.Exception ex) { ModContent.GetInstance<AethonMod>()?.Logger?.Error("Codex error", ex); }
                    return true;
                }, InterfaceScaleType.UI));
        }
    }
}
