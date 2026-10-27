using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Definitivas;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Weapons.Definitivas
{
    // ======================================================================
    //  v6.54.6 — LA BOLSA DE LAS ARMAS DEFINITIVAS, LA REFORMA. Este
    //  archivo: LA CORONA.
    // ======================================================================

    /// <summary>
    /// ARMA 5 — EL TESTAMENTO DE AETHON: LA CORONA DE LA BOLSA (el
    /// patrón FUSIÓN de Fargo's — el «Soul of Eternity»): el libro abre
    /// sus páginas con SU CINE DE SIETE CAPAS (el velo del dictado
    /// teñido con el color de la sentencia, los halos apilados, las
    /// páginas volando, el anillo de runas, LA PALABRA — la onda
    /// expansiva del dictado —, el sello y EL VÍNCULO, el filamento
    /// vivo que une el libro a su sentencia)… y DICTA la sentencia que
    /// toca: CADA USO dispara UNA DE LAS CUATRO SENTENCIAS con su poder
    /// acrecentado (×1.3), rotando para siempre. Cuatro arsenales en un
    /// solo objeto — y NINGUNA sentencia se queda viva para siempre
    /// (la cura del haz eterno de la .54.4).
    /// </summary>
    public class TestamentoAethon : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 300;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 50;
            Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<TestamentoAethonProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 50;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item74.WithPitchOffset(-0.3f);
        }

        public override bool CanUseItem(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<TestamentoAethonProjectile>())
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
