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
    /// CosmicRayStaff — bastón que dispara un rayo cósmico perforante.
    ///
    /// Especificaciones:
    ///   - damage = 60 (DamageType.Magic)
    ///   - useTime = useAnimation = 22
    ///   - shootSpeed = 14f (rápido)
    ///   - mana = 0
    ///   - useStyle = HoldUp, autoReuse = true, noMelee = true
    ///
    /// Override Shoot para NO usar vanilla shoot: creamos un proyectil largo
    /// y fino que perfora todo (penetrate=-1, extraUpdates=3).
    /// PreDraw del proyectil estira BeamGold (scaleX=5, scaleY=0.3) en la
    /// dirección del movimiento y genera sparkles Star.png a lo largo.
    /// </summary>
    public class CosmicRayStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 60;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 0;
            Item.knockBack = 4f;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.shoot = ModContent.ProjectileType<CosmicRayProjectile>();
            Item.shootSpeed = 14f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Spawn ligeramente elevado para que salga del arma
            Vector2 spawnPos = position + new Vector2(0f, -8f);
            Projectile.NewProjectile(source, spawnPos, velocity, type, damage, knockback, player.whoAmI);
            return false; // ya creamos el proyectil nosotros
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "CR_Title",
                "[c/FFD700:═══ RAYO CÓSMICO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "CR_Desc",
                "[c/B388FF:Rayo dorado perforante que atraviesa todo]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
