using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// Lumina, la Arcoestelar — arma de la rama de Distancia.
    /// Arco que dispara flechas de luz estelar (no consume munición base).
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
            // El arco de luz estelar no consume munición base.
            return false;
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            // Proyectiles extra (Cuerda doble, etc.)
            // (tModLoader dispara 1 por defecto; los extras se manejan en Shoot.)
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(2f, 0f);
        }
    }
}
