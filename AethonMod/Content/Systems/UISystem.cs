using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// UISystem — gestiona la UI del mod.
    /// BranchChoiceUI fue removido. Las armas se craftean directamente.
    /// </summary>
    public class UISystem : ModSystem
    {
        public override void Load()
        {
            if (Main.dedServ) return;
        }

        public override void Unload() { }
        public override void PostUpdateInput() { }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) { }
    }
}
