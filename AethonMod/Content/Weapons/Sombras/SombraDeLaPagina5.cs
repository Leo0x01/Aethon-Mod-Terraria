using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Sombras;

namespace AethonMod.Content.Weapons.Sombras
{
    /// <summary>
    /// SOMBRADELAPAGINA5 — v6.101 — ARMA: LA SOMBRA DE LA PÁGINA V —
    /// EL TENTÁCULO DEL ABISMO MORDAZ (la letra del usuario: «crea la
    /// sombra de la pagina 5, pero lo que tienes que cambiar es la
    /// textura del tentaculo sus ojos y dientes, intenta que se vea mas
    /// increible y aterrador»).
    ///
    /// El quinto tomo: el ritual de la familia (caza al jefe a cualquier
    /// distancia, muerde y drena 3%/6t, a 1 HP la devoración) con la
    /// piel NUEVA: ONCE OJOS RASGADOS que parpadean cada uno a su ritmo,
    /// LA SIERRA DOBLE de colmillos por el lomo y LA FAUCE — mandíbulas
    /// de cinco colmillos que jadean, se abren ENORME sobre la presa y
    /// TRITURAN mordiendo, con la garganta roja latiendo detrás.
    ///
    /// ARMA DE PRUEBA (la familia de la Bolsa de las Sombras — y la
    /// entrega directa de la casa, como todo lo nuevo).
    /// </summary>
    public class SombraDeLaPagina5 : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 200;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 48;
            Item.useAnimation = 48;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SombraPagina5Projectile>();
            Item.shootSpeed = 13f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item122;
        }

        public override bool CanUseItem(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<SombraPagina5Projectile>())
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            NPC presa = SombraDeLaPagina.BuscarPresa(player.Center, true) ??
                        SombraDeLaPagina.BuscarPresa(player.Center, false);
            Vector2 rumbo = presa != null
                ? (presa.Center - player.MountedCenter).SafeNormalize(velocity.LengthSquared() > 0.1f ? velocity : Vector2.UnitX)
                : velocity.LengthSquared() > 0.1f ? velocity.SafeNormalize(Vector2.UnitX) : Vector2.UnitX;

            Projectile.NewProjectile(source, player.MountedCenter, rumbo * 12f, type, damage, knockback,
                player.whoAmI, presa != null ? presa.whoAmI + 1 : 0, 0);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
