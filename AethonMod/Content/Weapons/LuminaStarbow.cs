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
        }

        public override bool? CanConsumeAmmo(Player player)
        {
            // El arco de luz estelar no consume munición base (pero requiere flechas equipadas).
            return false;
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(2f, 0f);
        }
    }
}
