using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.Players;

namespace AethonMod.Content.Systems
{
    public class UISystem : ModSystem
    {
        private UserInterface? _skillTreeInterface;
        internal UI.SkillTreeUIState? SkillTreeUI;
        private UserInterface? _codexInterface;
        internal UI.MemoryCodexUIState? CodexUI;
        private UserInterface? _xpBarInterface;
        internal UI.ShardXPBarUI? XPBarUI;
        private UserInterface? _branchChoiceInterface;
        internal UI.BranchChoiceUI? BranchChoiceUI;

        public override void Load()
        {
            if (Main.dedServ) return;
            _skillTreeInterface = new UserInterface();
            SkillTreeUI = new UI.SkillTreeUIState();
            SkillTreeUI.Activate();
            _skillTreeInterface.SetState(SkillTreeUI);

            _codexInterface = new UserInterface();
            CodexUI = new UI.MemoryCodexUIState();
            CodexUI.Activate();
            _codexInterface.SetState(CodexUI);

            _xpBarInterface = new UserInterface();
            XPBarUI = new UI.ShardXPBarUI();
            XPBarUI.Activate();
            _xpBarInterface.SetState(XPBarUI);

            _branchChoiceInterface = new UserInterface();
            BranchChoiceUI = new UI.BranchChoiceUI();
            BranchChoiceUI.Activate();
            _branchChoiceInterface.SetState(BranchChoiceUI);
        }

        public override void Unload()
        {
            SkillTreeUI = null;
            _skillTreeInterface = null;
            CodexUI = null;
            _codexInterface = null;
            XPBarUI = null;
            _xpBarInterface = null;
            BranchChoiceUI = null;
            _branchChoiceInterface = null;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            // La barra de XP siempre se actualiza (ella misma decide si es visible).
            _xpBarInterface?.Update(gameTime);

            if (SkillTreeUI?.IsVisible == true)
                _skillTreeInterface?.Update(gameTime);
            if (CodexUI?.IsVisible == true)
                _codexInterface?.Update(gameTime);
            if (BranchChoiceUI?.IsVisible == true)
                _branchChoiceInterface?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
            if (inventoryIndex != -1)
            {
                // Barra de XP (siempre visible cuando el fragmento está imprintado)
                layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer(
                    "AethonMod: XPBar",
                    delegate
                    {
                        if (XPBarUI?.IsVisible == true)
                            _xpBarInterface?.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI));

                // Elección de rama (tarjetas flotantes)
                layers.Insert(inventoryIndex + 2, new LegacyGameInterfaceLayer(
                    "AethonMod: BranchChoice",
                    delegate
                    {
                        if (BranchChoiceUI?.IsVisible == true)
                            _branchChoiceInterface?.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI));

                // Árbol de habilidades
                layers.Insert(inventoryIndex + 3, new LegacyGameInterfaceLayer(
                    "AethonMod: SkillTreeUI",
                    delegate
                    {
                        if (SkillTreeUI?.IsVisible == true)
                            _skillTreeInterface?.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI));

                // Códex de memoria
                layers.Insert(inventoryIndex + 4, new LegacyGameInterfaceLayer(
                    "AethonMod: CodexUI",
                    delegate
                    {
                        if (CodexUI?.IsVisible == true)
                            _codexInterface?.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI));
            }
        }

        public override void PostUpdateInput()
        {
            var config = ModContent.GetInstance<Content.AethonConfig>();
            if (config == null) return;

            if (Main.keyState.IsKeyDown(config.SkillTreeKey) &&
                !Main.oldKeyState.IsKeyDown(config.SkillTreeKey))
            {
                var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
                if (sp != null && sp.IsImprinted)
                {
                    if (SkillTreeUI?.IsVisible == true)
                        SkillTreeUI.Hide();
                    else
                        SkillTreeUI?.Show();
                }
            }
            if (Main.keyState.IsKeyDown(config.CodexKey) &&
                !Main.oldKeyState.IsKeyDown(config.CodexKey))
            {
                var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
                if (sp != null && sp.IsImprinted)
                {
                    if (CodexUI?.IsVisible == true)
                        CodexUI.Hide();
                    else
                        CodexUI?.Show();
                }
            }
        }
    }
}
