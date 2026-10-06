using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Sombras;

namespace AethonMod.Content.Weapons.Sombras
{
    /// <summary>
    /// CRIADELAPAGINA — v6.50.69 — ARMA NUEVA 3: LA CRÍA DE LA PÁGINA.
    ///
    /// El Sello murió («se ven y funcionan horrible»). LA CRÍA es la
    /// guardia de la familia: TRES tentáculos pequeños que VIVEN en el
    /// portador (orbitan colgados de él) y CAZAN SOLOS — cada uno con su
    /// ojo rasgado MIRANDO a la presa. No se apunta: se invoca y la
    /// camada protege (una cría por uso, hasta TRES). Si su mordisco
    /// mata a un jefe: LA DEVORACIÓN (festín estilo 10, LA MENADA).
    /// Arma de prueba: LA BOLSA DE LAS SOMBRAS (receta de 5 madera —
    /// protocolo v6.14.2). 100% código.
    /// </summary>
    public class CriaDeLaPagina : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 75;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 26;
            Item.useAnimation = 26;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<CriaPaginaProjectile>();
            Item.shootSpeed = 0f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item122;
        }

        public override bool CanUseItem(Player player)
        {
            // LA CAMADA COMPLETA son tres — se puede invocar mientras falten
            int vivas = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<CriaPaginaProjectile>())
                    vivas++;
            }
            return vivas < 3;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL PUESTO LIBRE: el primer ángulo (0°/120°/240°) sin cría viva
            bool[] ocupado = new bool[3];
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<CriaPaginaProjectile>() &&
                    p.ai[0] >= 0f && p.ai[0] < 3f)
                    ocupado[(int)p.ai[0]] = true;
            }
            int slot = -1;
            for (int s = 0; s < 3; s++)
                if (!ocupado[s]) { slot = s; break; }
            if (slot < 0) return false;

            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type,
                damage, knockback, player.whoAmI, slot, 0f);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
