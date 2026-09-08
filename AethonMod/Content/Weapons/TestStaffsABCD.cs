using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.Globals;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// TestStaffs A/B/C/D — 4 variantes del TestStaff para aislar cuál de los
    /// 4 overrides del Grimorio causa el doble Shoot.
    ///
    /// Cada variante tiene EXACTAMENTE UNO de los overrides:
    /// - TestStaffA: ModifyWeaponDamage (rojo)
    /// - TestStaffB: ModifyWeaponKnockback (verde)
    /// - TestStaffC: ModifyManaCost (azul)
    /// - TestStaffD: UseTimeMultiplier (amarillo) — sospechoso principal
    ///
    /// Todas usan la MISMA lógica anti-doble del TestStaff original que funciona.
    /// Si alguna variante hace doble Shoot, sabremos cuál override es el culpable.
    /// </summary>

    // === TESTSTAFF A: ModifyWeaponDamage ===
    public class TestStaffA : ModItem
    {
        private static uint _lastFireFrame = 0;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 10;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp; // mismo que Grimorio
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.autoReuse = true;
            Item.shoot = 931; // Nightglow (mismo que Grimorio)
            Item.shootSpeed = 12f;
            Item.mana = 2;
            Item.noMelee = true;
            Item.reuseDelay = 10;
        }

        // Override bajo test: ModifyWeaponDamage
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            // Simula el código del Grimorio
            damage *= 1.1f; // +10% daño
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Test", "[c/FF5555:═══ TESTSTAFF A ═══]"));
            tooltips.Add(new TooltipLine(Mod, "Desc", "[c/FF5555:Override: ModifyWeaponDamage]"));
            tooltips.Add(new TooltipLine(Mod, "Info", "[c/78788C:Si hace 2 disparos por click, este es el culpable]"));
            tooltips.Add(new TooltipLine(Mod, "Info2", "[c/78788C:Mensaje debug: [A] en rojo]"));
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2) return player.statMana >= 10;
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            uint currentFrame = Main.GameUpdateCount;
            if (currentFrame == _lastFireFrame) return false;
            _lastFireFrame = currentFrame;

            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (Main.myPlayer == player.whoAmI)
                Main.NewText($"[A] 1 proyectil, frame {currentFrame}", new Color(255, 100, 100));
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 10).Register();
        }
    }

    // === TESTSTAFF B: ModifyWeaponKnockback ===
    public class TestStaffB : ModItem
    {
        private static uint _lastFireFrame = 0;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 10;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.autoReuse = true;
            Item.shoot = 931;
            Item.shootSpeed = 12f;
            Item.mana = 2;
            Item.noMelee = true;
            Item.reuseDelay = 10;
        }

        // Override bajo test: ModifyWeaponKnockback
        public override void ModifyWeaponKnockback(Player player, ref StatModifier knockback)
        {
            knockback *= 1.2f; // +20% knockback
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Test", "[c/55FF55:═══ TESTSTAFF B ═══]"));
            tooltips.Add(new TooltipLine(Mod, "Desc", "[c/55FF55:Override: ModifyWeaponKnockback]"));
            tooltips.Add(new TooltipLine(Mod, "Info", "[c/78788C:Si hace 2 disparos por click, este es el culpable]"));
            tooltips.Add(new TooltipLine(Mod, "Info2", "[c/78788C:Mensaje debug: [B] en verde]"));
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2) return player.statMana >= 10;
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            uint currentFrame = Main.GameUpdateCount;
            if (currentFrame == _lastFireFrame) return false;
            _lastFireFrame = currentFrame;

            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (Main.myPlayer == player.whoAmI)
                Main.NewText($"[B] 1 proyectil, frame {currentFrame}", new Color(100, 255, 100));
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 10).Register();
        }
    }

    // === TESTSTAFF C: ModifyManaCost ===
    public class TestStaffC : ModItem
    {
        private static uint _lastFireFrame = 0;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 10;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.autoReuse = true;
            Item.shoot = 931;
            Item.shootSpeed = 12f;
            Item.mana = 2;
            Item.noMelee = true;
            Item.reuseDelay = 10;
        }

        // Override bajo test: ModifyManaCost
        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            Item.mana = 3; // cambia mana dinámicamente
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Test", "[c/55AAFF:═══ TESTSTAFF C ═══]"));
            tooltips.Add(new TooltipLine(Mod, "Desc", "[c/55AAFF:Override: ModifyManaCost]"));
            tooltips.Add(new TooltipLine(Mod, "Info", "[c/78788C:Si hace 2 disparos por click, este es el culpable]"));
            tooltips.Add(new TooltipLine(Mod, "Info2", "[c/78788C:Mensaje debug: [C] en azul]"));
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2) return player.statMana >= 10;
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            uint currentFrame = Main.GameUpdateCount;
            if (currentFrame == _lastFireFrame) return false;
            _lastFireFrame = currentFrame;

            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (Main.myPlayer == player.whoAmI)
                Main.NewText($"[C] 1 proyectil, frame {currentFrame}", new Color(100, 100, 255));
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 10).Register();
        }
    }

    // === TESTSTAFF D: UseTimeMultiplier (SOSPECHOSO PRINCIPAL) ===
    public class TestStaffD : ModItem
    {
        private static uint _lastFireFrame = 0;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 10;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.autoReuse = true;
            Item.shoot = 931;
            Item.shootSpeed = 12f;
            Item.mana = 2;
            Item.noMelee = true;
            Item.reuseDelay = 10;
        }

        // Override bajo test: UseTimeMultiplier (SOSPECHOSO PRINCIPAL)
        // Simula el código del Grimorio que retorna WeaponScaling.UseSpeedMult(level)
        // En nivel 1: 1 - 0.003 = 0.997 (no entero)
        public override float UseTimeMultiplier(Player player)
        {
            return 0.997f; // simula nivel 1 del Grimorio
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Test", "[c/FFFF55:═══ TESTSTAFF D ═══]"));
            tooltips.Add(new TooltipLine(Mod, "Desc", "[c/FFFF55:Override: UseTimeMultiplier (SOSPECHOSO)]"));
            tooltips.Add(new TooltipLine(Mod, "Info", "[c/78788C:Retorna 0.997 (no entero) — puede causar doble Shoot]"));
            tooltips.Add(new TooltipLine(Mod, "Info2", "[c/78788C:Mensaje debug: [D] en amarillo]"));
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2) return player.statMana >= 10;
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            uint currentFrame = Main.GameUpdateCount;
            if (currentFrame == _lastFireFrame) return false;
            _lastFireFrame = currentFrame;

            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (Main.myPlayer == player.whoAmI)
                Main.NewText($"[D] 1 proyectil, frame {currentFrame}", new Color(255, 255, 100));
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 10).Register();
        }
    }
}
