using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Sombras;

namespace AethonMod.Content.Weapons.Sombras
{
    /// <summary>
    /// SELLODELAPAGINA — v6.50.68 — ARMA NUEVA 3: EL SELLO DE LA PÁGINA.
    ///
    /// La letra del usuario: «cambia los otros bastones por conceptos
    /// diferentes». EL SELLO reemplaza a El Nido de la Página — no
    /// persigue ni perfora: MARCA EL TERRENO. El círculo rúnico se
    /// dibuja donde apunta el cursor, ERUPCIONA en seis garras que
    /// brotan del perímetro y deja un poso de bruma que muerde. Su
    /// festín: EL SELLO DEL JUICIO (estilo 10, el círculo que se ciñe
    /// sobre el reo).
    ///
    /// ARMA DE PRUEBA (no toca el grimorio): se entrega en LA BOLSA DE
    /// LAS SOMBRAS del kit de pruebas; la receta de 5 madera queda como
    /// vía alternativa (protocolo v6.14.2).
    /// </summary>
    public class SelloDeLaPagina : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 190;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 58;
            Item.useAnimation = 58;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SelloPaginaProjectile>();
            Item.shootSpeed = 14f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item123;
        }

        public override bool CanUseItem(Player player)
        {
            // UN solo sello vivo por portador (la firma no se repite)
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<SelloPaginaProjectile>())
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA FIRMA: se traza DONDE APUNTA EL CURSOR (máx. 700 px —
            // la firma se estampa cerca del escribano)
            Vector2 destino = Main.MouseWorld;
            Vector2 delta = destino - player.MountedCenter;
            if (delta.Length() > 700f)
                destino = player.MountedCenter + Vector2.Normalize(delta) * 700f;

            Projectile.NewProjectile(source, destino, Vector2.Zero,
                type, damage, knockback, player.whoAmI, 0f, 0f);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
