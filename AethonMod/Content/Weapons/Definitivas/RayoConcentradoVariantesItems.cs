using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Definitivas;

namespace AethonMod.Content.Weapons.Definitivas
{
    // ======================================================================
    //  v6.56.6 — LAS NUEVE VARIANTES DEL RAYO CONCENTRADO 2 (la letra del
    //  usuario: «quiero que hagas otra variantes del rayo concentrado 2,
    //  al menos 9 variantes mas con diferentes efectos y tecnicas vfx»).
    //
    //  LA CONDUCTA ES LA DEL RC2 (carga 300 · láser 800 · el temblor ·
    //  los soles rúnicos de la carga): cada ítem dispara EL MISMO
    //  proyectil con la VARIANTE en ai[3] — la piel cambia (gradiente,
    //  boca, luz y la TÉCNICA extra del haz: ver VariantesRC2.cs), el
    //  alma no. Las nueve técnicas, una por variante:
    //    1 Prisma — aberración cromática (el haz se parte en rojo/cian)
    //    2 Eclipse — bruma negra por el conducto
    //    3 Tormenta — rayos que reptan (StormLib.Bolt)
    //    4 Solar — lenguas de fuego solar (PyraLib.Flame)
    //    5 Vórtice — nebulosas orbitando (NebulaLib.Nube)
    //    6 Espectro — fajas fantasma escoltando (los ecos)
    //    7 Sierpe — el eje ondula como serpiente
    //    8 Cuántico — el conducto se ramifica
    //    9 Nova — anillos de pulso que viajan (OndaLib.Pulse)
    // ======================================================================
    internal static class VariantesRC2Items
    {
        /// <summary>LA EXCLUSIÓN FAMILIAR: un solo haz concentrado a la vez
        /// — el MISMO tipo de proyectil para todas las variantes (la
        /// exclusión del RC2 original nos cubre a todos).</summary>
        public static bool HazLibre(Player player)
        {
            int rc2 = ModContent.ProjectileType<RayoConcentradoDosProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI && p.type == rc2)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool Lanzar(Player player, EntitySource_ItemUse_WithAmmo source,
            int type, int damage, float knockback, int variante)
        {
            // LA VARIANTE VIAJA EN ai[1] (el Modo — Projectile.ai solo
            // tiene 3 slots en este tML y NewProjectile alimenta ai0-ai2:
            // ai0=Time, ai1=variante, ai2=dueño). Sincronizada por tML.
            Projectile.NewProjectile(source, player.MountedCenter.X, player.MountedCenter.Y,
                0f, 0f, type, damage, knockback, player.whoAmI, 0f, variante, player.whoAmI);
            return false;
        }
    }

    public class RayoConcentradoPrisma : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 500;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RayoConcentradoDosProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 10;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item74.WithPitchOffset(-0.3f);
        }

        public override bool CanUseItem(Player player) => VariantesRC2Items.HazLibre(player);

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
            => VariantesRC2Items.Lanzar(player, source, type, damage, knockback, 1);
    }

    public class RayoConcentradoEclipse : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 500;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RayoConcentradoDosProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 10;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item74.WithPitchOffset(-0.35f);
        }

        public override bool CanUseItem(Player player) => VariantesRC2Items.HazLibre(player);

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
            => VariantesRC2Items.Lanzar(player, source, type, damage, knockback, 2);
    }

    public class RayoConcentradoTormenta : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 500;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RayoConcentradoDosProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 10;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item74.WithPitchOffset(-0.25f);
        }

        public override bool CanUseItem(Player player) => VariantesRC2Items.HazLibre(player);

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
            => VariantesRC2Items.Lanzar(player, source, type, damage, knockback, 3);
    }

    public class RayoConcentradoSolar : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 500;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RayoConcentradoDosProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 10;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item74.WithPitchOffset(-0.2f);
        }

        public override bool CanUseItem(Player player) => VariantesRC2Items.HazLibre(player);

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
            => VariantesRC2Items.Lanzar(player, source, type, damage, knockback, 4);
    }

    public class RayoConcentradoVortice : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 500;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RayoConcentradoDosProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 10;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item74.WithPitchOffset(-0.3f);
        }

        public override bool CanUseItem(Player player) => VariantesRC2Items.HazLibre(player);

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
            => VariantesRC2Items.Lanzar(player, source, type, damage, knockback, 5);
    }

    public class RayoConcentradoEspectro : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 500;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RayoConcentradoDosProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 10;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item74.WithPitchOffset(-0.4f);
        }

        public override bool CanUseItem(Player player) => VariantesRC2Items.HazLibre(player);

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
            => VariantesRC2Items.Lanzar(player, source, type, damage, knockback, 6);
    }

    public class RayoConcentradoSierpe : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 500;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RayoConcentradoDosProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 10;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item74.WithPitchOffset(-0.28f);
        }

        public override bool CanUseItem(Player player) => VariantesRC2Items.HazLibre(player);

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
            => VariantesRC2Items.Lanzar(player, source, type, damage, knockback, 7);
    }

    public class RayoConcentradoCuantico : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 500;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RayoConcentradoDosProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 10;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item74.WithPitchOffset(-0.22f);
        }

        public override bool CanUseItem(Player player) => VariantesRC2Items.HazLibre(player);

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
            => VariantesRC2Items.Lanzar(player, source, type, damage, knockback, 8);
    }

    public class RayoConcentradoNova : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 500;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RayoConcentradoDosProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 10;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item74.WithPitchOffset(-0.18f);
        }

        public override bool CanUseItem(Player player) => VariantesRC2Items.HazLibre(player);

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
            => VariantesRC2Items.Lanzar(player, source, type, damage, knockback, 9);
    }
}
