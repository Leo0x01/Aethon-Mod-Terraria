using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Investigacion;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// INVESTIGACIONCOSMETICOSPLAYER — v6.54.0 — LOS DOS ADORNOS DE LA
    /// INVESTIGACIÓN (el patrón AlasAuroraPlayer de la casa: los ítems
    /// marcan las banderas cada frame, el ModPlayer engendra los halos
    /// y la vanidad es solo el visual).
    /// </summary>
    public class InvestigacionCosmeticosPlayer : ModPlayer
    {
        public bool EstelaCosmos;
        public bool EstelaCosmosOculta;
        public bool EstelaCosmosVanity;

        public bool TintaViva;
        public bool TintaVivaOculta;
        public bool TintaVivaVanity;

        public override void ResetEffects()
        {
            EstelaCosmos = false;
            EstelaCosmosOculta = false;
            EstelaCosmosVanity = false;
            TintaViva = false;
            TintaVivaOculta = false;
            TintaVivaVanity = false;
        }

        public override void PostUpdateEquips()
        {
            // EL ANILLO (EstelaDelCosmos).
            bool anilloVivo = EstelaCosmos && !EstelaCosmosOculta;
            if (anilloVivo && Player.whoAmI == Main.myPlayer && !Espiar<EstelaCosmosHalo>())
                Projectile.NewProjectile(Player.GetSource_Misc("EstelaDelCosmos"),
                    Player.Center, Microsoft.Xna.Framework.Vector2.Zero,
                    ModContent.ProjectileType<EstelaCosmosHalo>(), 0, 0f, Player.whoAmI);

            // LA COLA (TintaViva).
            bool colaViva = TintaViva && !TintaVivaOculta;
            if (colaViva && Player.whoAmI == Main.myPlayer && !Espiar<TintaVivaHalo>())
                Projectile.NewProjectile(Player.GetSource_Misc("TintaViva"),
                    Player.Center, Microsoft.Xna.Framework.Vector2.Zero,
                    ModContent.ProjectileType<TintaVivaHalo>(), 0, 0f, Player.whoAmI);
        }

        /// <summary>¿Ya vive mi halo? (el patrón Espiar de la casa).</summary>
        private bool Espiar<T>() where T : ModProjectile
        {
            int tipo = ModContent.ProjectileType<T>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == Player.whoAmI && p.type == tipo)
                    return true;
            }
            return false;
        }
    }
}
