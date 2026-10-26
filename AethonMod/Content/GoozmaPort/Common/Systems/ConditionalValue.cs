// ============================================================================
// PUERTO DE INVESTIGACIÓN — Calamity: Hunt of the Old God
// (github.com/TohruKobayashi/CalamityHunt) · Team Hypnos:
// caligulasAquarium, IbanPlay, THEREDSTONEBRO, AmmyanDocCiesh, Blockaroz
// Licencia original: GPL-3.0 (ver LICENSE-GPL3.txt en esta carpeta).
// Copiado verbatim por petición del usuario para investigar al jefe
// GOOZMA; pendiente su decisión de re-licencia/modificación.
// ============================================================================

using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.GoozmaPort.Common.Systems
{
    public class ConditionalValue : ModSystem
    {
        public static bool ExpertMode = false;
        public static bool MasterMode = false;
        public static bool RevengeanceMode = false;
        public static bool DeathMode = false;
        public static bool BossRush = false;

        public override void PreUpdateNPCs()
        {
            if (ModLoader.HasMod(HUtils.CalamityMod)) {
                Mod Calamity = ModLoader.GetMod(HUtils.CalamityMod);
                BossRush = ((bool)Calamity.Call("GetDifficultyActive", "bossrush")) ? true : false;
                DeathMode = ((bool)Calamity.Call("GetDifficultyActive", "death") || (bool)Calamity.Call("GetDifficultyActive", "bossrush")) ? true : false;
                RevengeanceMode = ((bool)Calamity.Call("GetDifficultyActive", "revengeance") || (bool)Calamity.Call("GetDifficultyActive", "bossrush")) ? true : false;
                ExpertMode = (Main.expertMode || (bool)Calamity.Call("GetDifficultyActive", "bossrush")) ? true : false;
                MasterMode = (Main.masterMode || (bool)Calamity.Call("GetDifficultyActive", "bossrush")) ? true : false;
            }
            else {
                RevengeanceMode = Main.expertMode;
                ExpertMode = Main.expertMode;
            }
        }

        public static int BalanceToggleValue(int vanilla, int? nonVanilla) => (int)BalanceToggleValue(vanilla, nonVanilla);

        public static float DifficultyBasedValue(float? normal = null, float? expert = null, float? revengeance = null, float? death = null, float? ftw = null, float? gfb = null)
        {
            if (Main.zenithWorld && gfb != null) {
                return (float)gfb;
            }
            else if (Main.getGoodWorld && ftw != null) {
                return (float)ftw;
            }
            else if ((DeathMode || (HUtils.InfernumActive() && Config.Instance.infernum)) && death != null) {
                return (float)death;
            }
            else if (RevengeanceMode && revengeance != null) {
                return (float)revengeance;
            }
            else if (ExpertMode && expert != null) {
                return (float)expert;
            }
            else {
                return (float)normal;
            }
        }
    }
}
