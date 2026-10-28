// LAS 3 ARMAS DEL GRIMORIO (v6.56.0) — Índice · Pluma · Ojo (bolsa 6→12).
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
    public class IndiceDelJuicio : ModItem
    {
        public override void SetDefaults()
        {
            //IL_00c9: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ce: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d6: Unknown result type (might be due to invalid IL or missing references)
            ((ModItem)this).Item.damage = 120;
            ((ModItem)this).Item.DamageType = DamageClass.Generic;
            ((Entity)((ModItem)this).Item).width = 40;
            ((Entity)((ModItem)this).Item).height = 40;
            ((ModItem)this).Item.useTime = 30;
            ((ModItem)this).Item.useAnimation = 30;
            ((ModItem)this).Item.useStyle = 4;
            ((ModItem)this).Item.noMelee = true;
            ((ModItem)this).Item.autoReuse = true;
            ((ModItem)this).Item.shoot = ModContent.ProjectileType<IndiceDelJuicioProjectile>();
            ((ModItem)this).Item.shootSpeed = 1f;
            ((ModItem)this).Item.mana = 60;
            ((ModItem)this).Item.rare = 11;
            ((ModItem)this).Item.value = Item.buyPrice(0, 0, 0, 0);
            Item item = ((ModItem)this).Item;
            SoundStyle item2 = SoundID.Item74;
            item.UseSound = (item2).WithPitchOffset(-0.35f);
        }

        public override bool CanUseItem(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile val = Main.projectile[i];
                if (val != null && ((Entity)val).active && val.owner == ((Entity)player).whoAmI && val.type == ModContent.ProjectileType<IndiceDelJuicioProjectile>())
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            //IL_0002: Unknown result type (might be due to invalid IL or missing references)
            //IL_0007: Unknown result type (might be due to invalid IL or missing references)
            Projectile.NewProjectile((IEntitySource)(object)source, player.MountedCenter, Vector2.Zero, type, damage, knockback, ((Entity)player).whoAmI, 0f, 0f, (float)((Entity)player).whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            ((ModItem)this).CreateRecipe(1).AddIngredient(9, 5).Register();
        }
    }

    public class OjoDelJuicio : ModItem
    {
        public override void SetDefaults()
        {
            //IL_00cc: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d1: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d9: Unknown result type (might be due to invalid IL or missing references)
            ((ModItem)this).Item.damage = 900;
            ((ModItem)this).Item.DamageType = DamageClass.Generic;
            ((Entity)((ModItem)this).Item).width = 40;
            ((Entity)((ModItem)this).Item).height = 40;
            ((ModItem)this).Item.useTime = 40;
            ((ModItem)this).Item.useAnimation = 40;
            ((ModItem)this).Item.useStyle = 4;
            ((ModItem)this).Item.noMelee = true;
            ((ModItem)this).Item.autoReuse = true;
            ((ModItem)this).Item.shoot = ModContent.ProjectileType<OjoDelJuicioProjectile>();
            ((ModItem)this).Item.shootSpeed = 1f;
            ((ModItem)this).Item.mana = 100;
            ((ModItem)this).Item.rare = 11;
            ((ModItem)this).Item.value = Item.buyPrice(0, 0, 0, 0);
            Item item = ((ModItem)this).Item;
            SoundStyle item2 = SoundID.Item74;
            item.UseSound = (item2).WithPitchOffset(-0.6f);
        }

        public override bool CanUseItem(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile val = Main.projectile[i];
                if (val != null && ((Entity)val).active && val.owner == ((Entity)player).whoAmI && val.type == ModContent.ProjectileType<OjoDelJuicioProjectile>())
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            //IL_0002: Unknown result type (might be due to invalid IL or missing references)
            //IL_0007: Unknown result type (might be due to invalid IL or missing references)
            Projectile.NewProjectile((IEntitySource)(object)source, player.MountedCenter, Vector2.Zero, type, damage, knockback, ((Entity)player).whoAmI, 0f, 0f, (float)((Entity)player).whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            ((ModItem)this).CreateRecipe(1).AddIngredient(9, 5).Register();
        }
    }

    public class PlumaDelVeredicto : ModItem
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
            ((ModItem)this).Item.useTime = 36;
            ((ModItem)this).Item.useAnimation = 36;
            ((ModItem)this).Item.useStyle = 4;
            ((ModItem)this).Item.noMelee = true;
            ((ModItem)this).Item.autoReuse = true;
            ((ModItem)this).Item.shoot = ModContent.ProjectileType<PlumaDelVeredictoProjectile>();
            ((ModItem)this).Item.shootSpeed = 1f;
            ((ModItem)this).Item.mana = 70;
            ((ModItem)this).Item.rare = 11;
            ((ModItem)this).Item.value = Item.buyPrice(0, 0, 0, 0);
            Item item = ((ModItem)this).Item;
            SoundStyle item2 = SoundID.Item74;
            item.UseSound = (item2).WithPitchOffset(-0.45f);
        }

        public override bool CanUseItem(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile val = Main.projectile[i];
                if (val != null && ((Entity)val).active && val.owner == ((Entity)player).whoAmI && val.type == ModContent.ProjectileType<PlumaDelVeredictoProjectile>())
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            //IL_0002: Unknown result type (might be due to invalid IL or missing references)
            //IL_0007: Unknown result type (might be due to invalid IL or missing references)
            Projectile.NewProjectile((IEntitySource)(object)source, player.MountedCenter, Vector2.Zero, type, damage, knockback, ((Entity)player).whoAmI, 0f, 0f, (float)((Entity)player).whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            ((ModItem)this).CreateRecipe(1).AddIngredient(9, 5).Register();
        }
    }
}
