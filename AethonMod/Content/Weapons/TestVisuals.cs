using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.Globals;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Weapons
{
    // ================================================================
    //  ARMAS DE PRUEBA VISUAL — v5.28
    //  Cada arma prueba un efecto visual diferente sin tocar el Grimorio.
    // ================================================================

    // === 1. AURA CÓSMICA AL SOSTENER ===
    // Brillo dorado pulsante que ilumina alrededor del jugador.
    public class TestAura : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 10; Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 2; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
        }
        public override void HoldItem(Player player)
        {
            // AURA: brillo dorado pulsante que ilumina 5 tiles a la redonda
            float pulse = 0.7f + 0.3f * (float)System.Math.Sin(Main.GameUpdateCount * 0.05f);
            Lighting.AddLight(player.Center, new Vector3(1.0f * pulse, 0.8f * pulse, 0.3f * pulse));
            // Partículas doradas sutiles alrededor del jugador
            if (Main.rand.NextBool(8))
            {
                Dust d = Dust.NewDustPerfect(player.Center + new Vector2(
                    Main.rand.NextFloat(-30, 30), Main.rand.NextFloat(-40, 0)),
                    DustID.GoldFlame, new Vector2(0, -0.5f), 100,
                    new Color(255, 217, 61), 0.5f);
                d.noGravity = true; d.fadeIn = 0f;
            }
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FFD700:═══ AURA CÓSMICA ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Brillo dorado pulsante al sostener]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // === 2. TRAIL DE ESTRELLAS ===
    // El proyectil deja un rastro de pequeñas estrellas doradas/cian.
    public class TestStarTrail : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 10; Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 2; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Crear proyectil normal
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            // Marcar el proyectil para que el GlobalProjectile sepa que tiene trail de estrellas
            if (proj >= 0 && proj < Main.maxProjectiles)
            {
                var p = Main.projectile[proj];
                p.ai[1] = 9999; // flag mágico para indicar trail de estrellas
            }
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/55AAFF:═══ TRAIL DE ESTRELLAS ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:El proyectil deja rastro de estrellas]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // === 3. PORTAL AL DISPARAR ===
    // Aparece un mini-portal cósmico temporal frente al jugador al disparar.
    public class TestPortal : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 10; Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 25; Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 2; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Crear mini-portal: 15 partículas en espiral frente al jugador
            Vector2 portalCenter = position + velocity.SafeNormalize(Vector2.Zero) * 30f;
            for (int i = 0; i < 15; i++)
            {
                float angle = (System.MathF.PI * 2 / 15) * i + Main.GameUpdateCount * 0.1f;
                float dist = 20f;
                Vector2 offset = new Vector2(
                    (float)System.Math.Cos(angle) * dist,
                    (float)System.Math.Sin(angle) * dist);
                Dust d = Dust.NewDustPerfect(portalCenter + offset,
                    DustID.GoldFlame, Vector2.Zero, 150,
                    new Color(255, 217, 61), 0.8f);
                d.noGravity = true; d.fadeIn = 0f;
            }
            // Centro del portal: destello cian
            for (int i = 0; i < 5; i++)
            {
                Dust d = Dust.NewDustPerfect(portalCenter,
                    DustID.BlueTorch,
                    new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)),
                    200, new Color(0, 255, 255), 0.6f);
                d.noGravity = true; d.fadeIn = 0f;
            }
            // Sonido del portal
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, portalCenter);
            // Crear el proyectil
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/BE78FD:═══ PORTAL CÓSMICO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Portal dorado/cian al disparar]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // === 4. MINION CON HALO ===
    // Invoca un minion con un anillo dorado girando alrededor.
    public class TestHaloMinion : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 10; Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 25; Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 5; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
        }
        public override bool AltFunctionUse(Player player) => true;
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2) return player.statMana >= 10;
            return true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                // Click derecho: invocar minion con halo
                player.statMana -= 10;
                player.AddBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.CosmicOrbBuff>(), 18000);
                int proj = Projectile.NewProjectile(source, position, Vector2.Zero,
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.CosmicOrbMinion>(),
                    damage, knockback, player.whoAmI);
                // Marcar el minion para que tenga halo (usando ai[0] = 1)
                if (proj >= 0 && proj < Main.maxProjectiles)
                    Main.projectile[proj].ai[0] = 1; // flag de halo
                return false;
            }
            // Click izquierdo: disparo normal
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/78FF96:═══ MINION CON HALO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Click der: invoca minion con anillo dorado]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // === 5a. COLOR DEL PROYECTIL — DORADO (bajo nivel) ===
    // Re-tinte el Nightglow con un tinte dorado.
    public class TestColorGold : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 10; Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 2; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (proj >= 0 && proj < Main.maxProjectiles)
            {
                Main.projectile[proj].ai[1] = 1001; // flag color dorado
            }
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FFD700:═══ COLOR: DORADO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Proyectil con tinte dorado]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // === 5b. COLOR DEL PROYECTIL — CIAN (nivel medio) ===
    public class TestColorCyan : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 10; Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 2; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (proj >= 0 && proj < Main.maxProjectiles)
            {
                Main.projectile[proj].ai[1] = 1002; // flag color cian
            }
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/00FFFF:═══ COLOR: CIAN ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Proyectil con tinte cian]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // === 5c. COLOR DEL PROYECTIL — MAGENTA (nivel alto) ===
    public class TestColorMagenta : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 10; Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 2; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (proj >= 0 && proj < Main.maxProjectiles)
            {
                Main.projectile[proj].ai[1] = 1003; // flag color magenta
            }
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FF00FF:═══ COLOR: MAGENTA ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Proyectil con tinte magenta]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // === 6. DAÑO EN ÁREA — RADIO 20 (estándar del Grimorio) ===
    // Dispara Nightglow con daño en área de 20px (radio estándar del Grimorio).
    public class TestArea20 : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 15; Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 3; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (proj >= 0 && proj < Main.maxProjectiles)
            {
                Main.projectile[proj].ai[1] = 2001; // flag: daño en área radio 20
            }
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FFAA55:═══ ÁREA: 20px ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Nightglow con daño en área 20px]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Radio estándar del Grimorio (tope actual)]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // === 7. DAÑO EN ÁREA — RADIO 60 (ampliado) ===
    // Dispara Nightglow con daño en área de 60px (casi 4 tiles).
    public class TestArea60 : ModItem
    {
        public override void SetStaticDefaults() { }
        public override void SetDefaults()
        {
            Item.damage = 15; Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 3; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (proj >= 0 && proj < Main.maxProjectiles)
            {
                Main.projectile[proj].ai[1] = 2002; // flag: daño en área radio 60
            }
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FF5555:═══ ÁREA: 60px ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Nightglow con daño en área 60px]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78788C:Radio ampliado (casi 4 tiles)]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
