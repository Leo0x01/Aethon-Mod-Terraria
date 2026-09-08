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
            Item.damage = 10; Item.DamageType = DamageClass.Generic; // v5.31: Generic para no costar mana
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true; // v5.31: sin mana
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
        }
        public override void HoldItem(Player player)
        {
            // v5.32: Brillo MUY tenue (no ilumina tiles, solo efecto visual cercano)
            float pulse = 0.3f + 0.2f * (float)System.Math.Sin(Main.GameUpdateCount * 0.05f);
            Lighting.AddLight(player.Center, new Vector3(0.3f * pulse, 0.25f * pulse, 0.1f * pulse));

            // v5.32: Más partículas doradas (1/4 en vez de 1/8)
            if (Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(player.Center + new Vector2(
                    Main.rand.NextFloat(-25, 25), Main.rand.NextFloat(-30, 10)),
                    DustID.GoldFlame, new Vector2(0, -0.8f), 100,
                    new Color(255, 217, 61), 0.4f);
                d.noGravity = true; d.fadeIn = 0f;
            }
            // Partícula cian ocasional
            if (Main.rand.NextBool(12))
            {
                Dust d = Dust.NewDustPerfect(player.Center + new Vector2(
                    Main.rand.NextFloat(-20, 20), Main.rand.NextFloat(-30, 10)),
                    DustID.BlueTorch, new Vector2(0, -0.6f), 150,
                    new Color(0, 255, 255), 0.3f);
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
            Item.damage = 10; Item.DamageType = DamageClass.Generic; // v5.31: Generic para no costar mana
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true; // v5.31: sin mana
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
            Item.damage = 10; Item.DamageType = DamageClass.Generic; // v5.31: Generic para no costar mana
            Item.width = 28; Item.height = 30;
            Item.useTime = 25; Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true; // v5.31: sin mana
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
            Item.damage = 10; Item.DamageType = DamageClass.Generic; // v5.31: Generic para no costar mana
            Item.width = 28; Item.height = 30;
            Item.useTime = 25; Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true; // v5.31: sin mana
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
        }
        public override bool AltFunctionUse(Player player) => true;
        public override bool CanUseItem(Player player)
        {
            // Sin requisito de mana (arma de prueba)
            return true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                // Click derecho: invocar minion con halo (sin costo de mana)
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
            Item.damage = 10; Item.DamageType = DamageClass.Generic; // v5.31: Generic para no costar mana
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true; // v5.31: sin mana
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
            Item.damage = 10; Item.DamageType = DamageClass.Generic; // v5.31: Generic para no costar mana
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true; // v5.31: sin mana
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
            Item.damage = 10; Item.DamageType = DamageClass.Generic; // v5.31: Generic para no costar mana
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true; // v5.31: sin mana
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
            Item.damage = 15; Item.DamageType = DamageClass.Generic; // v5.31: Generic para no costar mana
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true; // v5.31: sin mana
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
            Item.damage = 15; Item.DamageType = DamageClass.Generic; // v5.31: Generic para no costar mana
            Item.width = 28; Item.height = 30;
            Item.useTime = 20; Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = 931; Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true; // v5.31: sin mana
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

    // === 8. RAYOS CÓSMICOS DESDE EL SUELO ===
    // Rayos dorados muy pequeños que suben desde el suelo hacia el jugador.
    public class TestRays : ModItem
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
        public override void HoldItem(Player player)
        {
            // v5.32: Rayos dorados que suben desde el suelo hacia el jugador
            // Usamos DustID.GoldFlame con velocidad hacia arriba
            if (Main.rand.NextBool(3))
            {
                // Posición aleatoria en el suelo, cerca del jugador
                Vector2 spawnPos = new Vector2(
                    player.Center.X + Main.rand.NextFloat(-40, 40),
                    player.Center.Y + Main.rand.NextFloat(30, 50)); // abajo del jugador

                // Rayo que sube rápido
                Dust d = Dust.NewDustPerfect(spawnPos,
                    DustID.GoldFlame,
                    new Vector2(0, -2.5f), // sube rápido
                    100, new Color(255, 217, 61), 0.3f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
            // Rayo cian ocasional
            if (Main.rand.NextBool(8))
            {
                Vector2 spawnPos = new Vector2(
                    player.Center.X + Main.rand.NextFloat(-30, 30),
                    player.Center.Y + Main.rand.NextFloat(30, 50));
                Dust d = Dust.NewDustPerfect(spawnPos,
                    DustID.BlueTorch,
                    new Vector2(0, -3f),
                    150, new Color(0, 255, 255), 0.25f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
            // Brillo muy tenue
            Lighting.AddLight(player.Center, new Vector3(0.2f, 0.15f, 0.05f));
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FFD700:═══ RAYOS CÓSMICOS ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Rayos dorados subiendo desde el suelo]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
