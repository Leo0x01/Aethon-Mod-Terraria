using Terraria;
using Terraria.ModLoader;
using global::AethonMod.Content.Projectiles.Investigaciones;
using global::AethonMod.Content.Weapons.Investigaciones;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// SolEnMiniaturaBuff — v6.85 — EL BUFF DEL SOL EN MINIATURA (R3).
    ///
    /// El patrón del arsenal (LivingPulsarBuff): vive mientras el sol
    /// viva; el clic derecho lo DESPIDE (EscuadronUtil.Despedir) y apaga
    /// el sello de persistencia (la letra: «a no ser que el jugador los
    /// desinvoque usando su buff»).
    /// </summary>
    public class SolEnMiniaturaBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.ownedProjectileCounts[
                    ModContent.ProjectileType<SolEnMiniaturaProj>()] == 0)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                player.buffTime[buffIndex] = 18000;
            }
        }

        public override bool RightClick(int buffIndex)
        {
            try
            {
                global::AethonMod.Content.Systems.EscuadronUtil.Despedir(
                    ModContent.ProjectileType<SolEnMiniaturaProj>());
                // EL SELLO SE APAGA (la despedida legítima — sin esto el
                // sol volvería al entrar al mundo).
                Main.LocalPlayer.GetModPlayer<SolEnMiniaturaPlayer>().Invocado = false;
            }
            catch { }
            return true;
        }
    }
}
