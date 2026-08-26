using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// Lumina, la Arcoestelar — arma de la rama de Distancia.
    /// Arco que dispara flechas de luz estelar (no consume municion base).
    /// El daño escala con el nivel del fragmento: daño = nivel × 2.4.
    /// </summary>
    public class LuminaStarbow : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName / Tooltip cargados desde Localization.
        }

        public override void SetDefaults()
        {
            Item.damage = 12;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 40;
            Item.height = 60;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 10, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item5;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<Projectiles.StarlightArrow>();
            Item.shootSpeed = 14f;
            Item.useAmmo = AmmoID.Arrow;
            Item.noMelee = true;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp != null && sp.IsImprinted && sp.ActiveBranch == Players.BranchType.Distance)
            {
                // Escalado: daño = nivel × 2.4 (aplicado como multiplicador sobre el daño base).
                damage += sp.ShardLevel * 2.4f;
            }
            // Aplicar efectos de nodos del árbol de Distancia.
            float crit = 0;
            Systems.NodeEffectSystem.ApplyDistanceEffects(player, ref damage, ref crit);
            player.GetCritChance(DamageClass.Ranged) += crit;
        }

        public override float UseTimeMultiplier(Player player)
        {
            return Systems.NodeEffectSystem.GetUseSpeedMultiplier(player);
        }

        public override bool CanConsumeAmmo(Item ammo, Player player)
        {
            // El arco de luz estelar no consume munición base, a menos que el jugador no tenga el keystone de carcaj infinito.
            // Si tiene "quiver-keystone" o "ammo-keystone", no consume munición.
            // Base: no consume munición (es un arco cosmico).
            return false;
        }

        /// <summary>
        /// Dispara flechas extra si el jugador tiene nodos de multi-disparo.
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int extra = Systems.NodeEffectSystem.GetExtraProjectiles(player);
            // La flecha principal la dispara tModLoader automaticamente (retornamos true).
            // Disparar las extra en abanico.
            for (int i = 0; i < extra; i++)
            {
                float angle = (i + 1) * 0.15f * (i % 2 == 0 ? 1f : -1f); // abanico alternado
                Vector2 perturbedVel = velocity.RotatedBy(angle);
                Projectile.NewProjectile(source, position, perturbedVel, type, damage, knockback, player.whoAmI);
            }
            return true; // tModLoader dispara la flecha principal.
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(2f, 0f);
        }
    }
}

