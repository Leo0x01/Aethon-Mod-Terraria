using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.Players;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// UISystem — Sistema de UI simplificado.
    /// Solo gestiona BranchChoiceUI (la selección de rama/arma del Fragmento Genesis).
    /// El árbol de habilidades (K) y el Codex de Memoria (J) fueron eliminados.
    /// </summary>
    public class UISystem : ModSystem
    {
        public UI.BranchChoiceUI? BranchChoiceUI;

        public override void Load()
        {
            if (Main.dedServ) return;
            BranchChoiceUI = new UI.BranchChoiceUI();
        }

        public override void Unload()
        {
            BranchChoiceUI = null;
        }

        public override void PostUpdateInput()
        {
            // BranchChoiceUI procesa input en su propio Draw (edge detection de click).
            // Aqui solo actualizamos estado ligero si fuera necesario.
            BranchChoiceUI?.Update();
        }

        private bool anyOtherOpen()
        {
            return BranchChoiceUI?.IsVisible ?? false;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int insertIdx = layers.FindIndex(l => l.Name.Equals("Vanilla: Interface Logic 2"));
            if (insertIdx == -1) insertIdx = layers.Count;

            // BranchChoiceUI (dibujo directo — selección de arma del Fragmento Genesis)
            layers.Insert(insertIdx, new LegacyGameInterfaceLayer("AethonMod: Branch Choice",
                () =>
                {
                    try { BranchChoiceUI?.Draw(); }
                    catch (System.Exception ex) { ModContent.GetInstance<AethonMod>()?.Logger?.Error("BranchChoiceUI error", ex); }
                    return true;
                }, InterfaceScaleType.UI));
        }
    }
}
