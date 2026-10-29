// LA ESCALERA DEL RAYO (v6.56.0) — Alba · Veredicto · Juicio (niveles 1-3 del RC2).
// clase reconstruida del BINARIO verificado tras el wipe del sandbox:
// la fuente original se perdió; este código viene del .tmod (md5 3b45a5fe…) y
// se verificó idéntico al recompilar (ver CHANGES.md v6.56.0 y worklog Task 69).
// Los comentarios de la fuente original no viajan dentro del .dll.

using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Definitivas;

namespace AethonMod.Content.Weapons.Definitivas
{
    internal static class EscaleraDelRayo
    {
        public static bool HazLibre(Player player)
        {
            // v6.56.1 — LA EXCLUSIÓN CRUZADA DE LA ESCALERA: UN solo haz
            // definitivo a la vez, CRUZANDO clases (niveles 1-3 + RC2 +
            // Prisma). Antes cada familia solo se excluía a sí misma y
            // cabían hasta 3 haces simultáneos apilando daño.
            int nivel = ModContent.ProjectileType<RayoConcentradoNivelProjectile>();
            int rc2 = ModContent.ProjectileType<RayoConcentradoDosProjectile>();
            int prisma = ModContent.ProjectileType<PrismaPrimordialProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile val = Main.projectile[i];
                if (val != null && ((Entity)val).active && val.owner == ((Entity)player).whoAmI
                    && (val.type == nivel || val.type == rc2 || val.type == prisma))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public class RayoDelAlba : ModItem
    {
        public override void SetDefaults()
        {
            //IL_00cb: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d0: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d8: Unknown result type (might be due to invalid IL or missing references)
            ((ModItem)this).Item.damage = 150;
            ((ModItem)this).Item.DamageType = DamageClass.Generic;
            ((Entity)((ModItem)this).Item).width = 40;
            ((Entity)((ModItem)this).Item).height = 40;
            ((ModItem)this).Item.useTime = 30;
            ((ModItem)this).Item.useAnimation = 30;
            ((ModItem)this).Item.useStyle = 4;
            ((ModItem)this).Item.noMelee = true;
            ((ModItem)this).Item.autoReuse = true;
            ((ModItem)this).Item.shoot = ModContent.ProjectileType<RayoConcentradoNivelProjectile>();
            ((ModItem)this).Item.shootSpeed = 1f;
            ((ModItem)this).Item.mana = 40;
            ((ModItem)this).Item.rare = 5;
            ((ModItem)this).Item.value = Item.buyPrice(0, 0, 0, 0);
            Item item = ((ModItem)this).Item;
            SoundStyle item2 = SoundID.Item74;
            item.UseSound = (item2).WithPitchOffset(-0.5f);
        }

        public override bool CanUseItem(Player player)
        {
            return EscaleraDelRayo.HazLibre(player);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            //IL_0002: Unknown result type (might be due to invalid IL or missing references)
            //IL_0007: Unknown result type (might be due to invalid IL or missing references)
            Projectile.NewProjectile((IEntitySource)(object)source, player.MountedCenter, Vector2.Zero, type, damage, knockback, ((Entity)player).whoAmI, 0f, 1f, (float)((Entity)player).whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            ((ModItem)this).CreateRecipe(1).AddIngredient(9, 5).Register();
        }
    }

    public class RayoDelVeredicto : ModItem
    {
        public override void SetDefaults()
        {
            //IL_00cb: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d0: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d8: Unknown result type (might be due to invalid IL or missing references)
            ((ModItem)this).Item.damage = 250;
            ((ModItem)this).Item.DamageType = DamageClass.Generic;
            ((Entity)((ModItem)this).Item).width = 40;
            ((Entity)((ModItem)this).Item).height = 40;
            ((ModItem)this).Item.useTime = 30;
            ((ModItem)this).Item.useAnimation = 30;
            ((ModItem)this).Item.useStyle = 4;
            ((ModItem)this).Item.noMelee = true;
            ((ModItem)this).Item.autoReuse = true;
            ((ModItem)this).Item.shoot = ModContent.ProjectileType<RayoConcentradoNivelProjectile>();
            ((ModItem)this).Item.shootSpeed = 1f;
            ((ModItem)this).Item.mana = 60;
            ((ModItem)this).Item.rare = 5;
            ((ModItem)this).Item.value = Item.buyPrice(0, 0, 0, 0);
            Item item = ((ModItem)this).Item;
            SoundStyle item2 = SoundID.Item74;
            item.UseSound = (item2).WithPitchOffset(-0.4f);
        }

        public override bool CanUseItem(Player player)
        {
            return EscaleraDelRayo.HazLibre(player);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            //IL_0002: Unknown result type (might be due to invalid IL or missing references)
            //IL_0007: Unknown result type (might be due to invalid IL or missing references)
            Projectile.NewProjectile((IEntitySource)(object)source, player.MountedCenter, Vector2.Zero, type, damage, knockback, ((Entity)player).whoAmI, 0f, 2f, (float)((Entity)player).whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            ((ModItem)this).CreateRecipe(1).AddIngredient(9, 5).Register();
        }
    }

    public class RayoDelJuicio : ModItem
    {
        public override void SetDefaults()
        {
            //IL_00cc: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d1: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d9: Unknown result type (might be due to invalid IL or missing references)
            ((ModItem)this).Item.damage = 380;
            ((ModItem)this).Item.DamageType = DamageClass.Generic;
            ((Entity)((ModItem)this).Item).width = 40;
            ((Entity)((ModItem)this).Item).height = 40;
            ((ModItem)this).Item.useTime = 30;
            ((ModItem)this).Item.useAnimation = 30;
            ((ModItem)this).Item.useStyle = 4;
            ((ModItem)this).Item.noMelee = true;
            ((ModItem)this).Item.autoReuse = true;
            ((ModItem)this).Item.shoot = ModContent.ProjectileType<RayoConcentradoNivelProjectile>();
            ((ModItem)this).Item.shootSpeed = 1f;
            ((ModItem)this).Item.mana = 80;
            ((ModItem)this).Item.rare = 11;
            ((ModItem)this).Item.value = Item.buyPrice(0, 0, 0, 0);
            Item item = ((ModItem)this).Item;
            SoundStyle item2 = SoundID.Item74;
            item.UseSound = (item2).WithPitchOffset(-0.3f);
        }

        public override bool CanUseItem(Player player)
        {
            return EscaleraDelRayo.HazLibre(player);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            //IL_0002: Unknown result type (might be due to invalid IL or missing references)
            //IL_0007: Unknown result type (might be due to invalid IL or missing references)
            Projectile.NewProjectile((IEntitySource)(object)source, player.MountedCenter, Vector2.Zero, type, damage, knockback, ((Entity)player).whoAmI, 0f, 3f, (float)((Entity)player).whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            ((ModItem)this).CreateRecipe(1).AddIngredient(9, 5).Register();
        }
    }
}
