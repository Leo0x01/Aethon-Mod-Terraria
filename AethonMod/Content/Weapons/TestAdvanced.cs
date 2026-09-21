using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace AethonMod.Content.Weapons
{
    // ================================================================
    //  ARMAS DE PRUEBA v5.46
    //  Solo lo que funciona. v6.01 — las 4 armas de COLOR
    //  (Rainbow/Red/Yellow/Green) fueron ELIMINADAS por petición del
    //  usuario (limpieza del arsenal de pruebas).
    // ================================================================

    // === TEST MAGIC RING (funciona) ===
    public class TestMagicRing : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 10; Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // v6.50.2 — FIX (ai[] post-spawn no viaja): el paquete 27 sale
            // DENTRO de NewProjectile → la escritura de ai[1] DESPUÉS del
            // spawn solo existía en la copia local (el server y los demás
            // clientes veían el default del tipo 931 y el FX de TestAdvancedFX
            // no se dibujaba). El flag viaja ahora como argumento ai1 (se
            // aplica y sincroniza cuando Owner == Main.myPlayer — Shoot
            // corre solo en el cliente dueño).
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback,
                player.whoAmI, 0f, 3003f);
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/BE78FD:═══ ANILLO MÁGICO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Anillo cósmico girando alrededor del proyectil]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // === TEST SPARKLE (funciona) ===
    public class TestSparkle : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 10; Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // v6.50.2 — FIX (ai[] post-spawn no viaja): como TestMagicRing —
            // el flag 3004 viaja como ai1 del spawn.
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback,
                player.whoAmI, 0f, 3004f);
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FFD700:═══ SPARKLE STARS ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Estrellas de 4 puntas con textura custom]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // === PROJ BEAM (funciona) ===
    public class ProjBeam : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 10; Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true; Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest; Item.UseSound = SoundID.Item8;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // v6.50.2 — FIX (ai[] post-spawn no viaja): como TestMagicRing —
            // el flag 4001 viaja como ai1 del spawn.
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback,
                player.whoAmI, 0f, 4001f);
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/00FFFF:═══ PROJ BEAM ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Rayo de energía con lens flare y bloom]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // === TEST MAGIC RING V2 (funciona) ===
    public class TestMagicRingV2 : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 10; Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // v6.50.2 — FIX (ai[] post-spawn no viaja): como TestMagicRing —
            // el flag 4006 viaja como ai1 del spawn.
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback,
                player.whoAmI, 0f, 4006f);
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FF00FF:═══ MAGIC RING V2 ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:3 anillos + hue shift + sparkles + multi-glow]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
