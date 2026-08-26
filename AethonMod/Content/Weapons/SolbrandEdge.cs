using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// Solbrand, Filo del Alba — arma de la rama de Cuerpo a Cuerpo.
    ///
    /// PROYECTILES: Base NO dispara proyectiles (melee puro).
    /// Los proyectiles se desbloquean con nodos Notable/Keystone del árbol.
    /// Ej: nodo "blade-1" (Corte de rayo) desbloquea el proyectil DawnSlash.
    /// </summary>
    public class SolbrandEdge : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.damage = 14;
            Item.DamageType = DamageClass.Melee;
            Item.width = 50;
            Item.height = 50;
            Item.useTime = 18;
            Item.useAnimation = 18;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6f;
            Item.value = Item.buyPrice(0, 10, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            // Base: NO dispara proyectiles (melee puro).
            Item.shoot = ProjectileID.None;
            Item.shootSpeed = 12f;
            // Permitir daño melee directo (no solo proyectil).
            Item.noMelee = false;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp != null && sp.IsImprinted && sp.ActiveBranch == Players.BranchType.Melee)
            {
                damage += sp.ShardLevel * 3.1f;
            }
            float crit = 0;
            Systems.NodeEffectSystem.ApplyMeleeEffects(player, ref damage, ref crit);
            player.GetCritChance(DamageClass.Melee) += crit;
        }

        public override void ModifyWeaponKnockback(Player player, ref StatModifier knockback)
        {
            knockback *= Systems.NodeEffectSystem.GetMeleeKnockbackMult(player);
        }

        public override bool CanShoot(Player player)
        {
            // Solo disparar proyectiles si el jugador tiene nodos que los desbloquean.
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp == null) return false;

            // Nodo "blade-1-notable" (Corte de rayo) desbloquea el proyectil.
            bool hasBladeNotable = sp.AllocatedNodes.Contains("blade-notable");
            if (hasBladeNotable)
            {
                Item.shoot = ModContent.ProjectileType<Projectiles.DawnSlash>();
                return true;
            }

            // Nodo "celestial-notable" desbloquea onda solar.
            bool hasCelestialNotable = sp.AllocatedNodes.Contains("celestial-notable");
            if (hasCelestialNotable)
            {
                Item.shoot = ModContent.ProjectileType<Projectiles.DawnSlash>();
                return true;
            }

            // Sin nodos: no disparar.
            Item.shoot = ProjectileID.None;
            return false;
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-2f, 0f);
        }
    }
}
