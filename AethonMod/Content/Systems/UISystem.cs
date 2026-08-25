using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.Players;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Sistema que registra y gestiona las UI del mod (árbol de habilidades + códex).
    /// Maneja la tecla 'K' para abrir/cerrar el árbol.
    /// </summary>
    public class UISystem : ModSystem
    {
        private UserInterface? _skillTreeInterface;
        internal UI.SkillTreeUIState? SkillTreeUI;
        private UserInterface? _codexInterface;
        internal UI.MemoryCodexUIState? CodexUI;

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
        }

        public override void Unload()
        {
            SkillTreeUI = null;
            _skillTreeInterface = null;
            CodexUI = null;
            _codexInterface = null;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (SkillTreeUI?.Visible == true)
                _skillTreeInterface?.Update(gameTime);
            if (CodexUI?.Visible == true)
                _codexInterface?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
            if (inventoryIndex != -1)
            {
                layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer(
                    "AethonMod: SkillTreeUI",
                    delegate
                    {
                        if (SkillTreeUI?.Visible == true)
                            _skillTreeInterface?.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI));
                layers.Insert(inventoryIndex + 2, new LegacyGameInterfaceLayer(
                    "AethonMod: CodexUI",
                    delegate
                    {
                        if (CodexUI?.Visible == true)
                            _codexInterface?.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI));
            }
        }

        public override void PostUpdateInput()
        {
            // Tecla 'K' para abrir/cerrar el árbol de habilidades.
            if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.K) &&
                !Main.oldKeyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.K))
            {
                var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
                if (sp != null && sp.IsImprinted)
                {
                    if (SkillTreeUI?.Visible == true)
                    {
                        SkillTreeUI.Hide();
                    }
                    else
                    {
                        SkillTreeUI?.Show();
                    }
                }
            }
            // Tecla 'J' para el códex de memoria.
            if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.J) &&
                !Main.oldKeyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.J))
            {
                var sp = Main.LocalPlayer.GetModPlayer<ShardPlayer>();
                if (sp != null && sp.IsImprinted)
                {
                    if (CodexUI?.Visible == true)
                        CodexUI.Hide();
                    else
                        CodexUI?.Show();
                }
            }
        }
    }
}
