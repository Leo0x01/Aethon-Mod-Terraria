using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.Projectiles.V20;

namespace AethonMod.Content.Weapons.V20
{
    /// <summary>
    /// BlackHoleStaff — bastón que invoca un agujero negro realista.
    ///
    /// Especificaciones:
    ///   - damage = 80 (DamageType.Magic)
    ///   - useTime = useAnimation = 40
    ///   - shootSpeed = 8f (lento — el agujero "deriva")
    ///   - mana = 0
    ///   - useStyle = HoldUp, autoReuse = true, noMelee = true
    ///
    /// Dispara BlackHoleProjectile (multi-capa: event horizon, photon ring,
    /// accretion disk, chromatic aberration, lensing, infalling dust + pull
    /// gravitacional sobre NPCs).
    /// </summary>
    public class BlackHoleStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 80;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 40;
            Item.useAnimation = 40;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;
            Item.knockBack = 4f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.shoot = ModContent.ProjectileType<BlackHoleProjectile>();
            Item.shootSpeed = 8f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Spawn ligeramente por encima del jugador (efecto "invocación")
            Vector2 spawnPos = position + new Vector2(0f, -16f);
            Projectile.NewProjectile(source, spawnPos, velocity, type, damage, knockback, player.whoAmI);
            return false; // ya creamos el proyectil
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "BH_Title",
                "[c/FF6400:═══ AGUJERO NEGRO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "BH_Desc",
                "[c/B388FF:Distorsión del espacio-tiempo con disco de acreción y horizonte de sucesos]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
