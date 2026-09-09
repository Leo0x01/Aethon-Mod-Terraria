using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace AethonMod.Content.Weapons
{
    // ================================================================
    //  ARMAS AVANZADAS v5.35 — PreDraw + additive blending + texturas custom
    //  NO tocan el Grimorio.
    // ================================================================

    // === TEST MAGIC RING (original, mantener) ===
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
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (proj >= 0 && proj < Main.maxProjectiles) Main.projectile[proj].ai[1] = 3003;
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/BE78FD:═══ ANILLO MÁGICO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Anillo cósmico girando alrededor del proyectil]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // === TEST SPARKLE (original, mantener) ===
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
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (proj >= 0 && proj < Main.maxProjectiles) Main.projectile[proj].ai[1] = 3004;
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FFD700:═══ SPARKLE STARS ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Estrellas de 4 puntas con textura custom]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // ================================================================
    //  TEST MAGIC RING V2 — versión mejorada y profesional
    //  3 anillos girando + hue shift + sparkles + múltiples glows
    // ================================================================
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
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (proj >= 0 && proj < Main.maxProjectiles) Main.projectile[proj].ai[1] = 4006;
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/FF00FF:═══ MAGIC RING V2 ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:3 anillos + hue shift + sparkles + multi-glow]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // ================================================================
    //  5 AURAS NUEVAS (efectos al sostener)
    // ================================================================

    // === 1. AURA SHIELD — escudo de energía con hexágonos ===
    public class AuraShield : ModItem
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
        public override void HoldItem(Player player)
        {
            Lighting.AddLight(player.Center, new Vector3(0.15f, 0.2f, 0.25f));
            // Partículas eléctricas
            if (Main.rand.NextBool(6))
            {
                float angle = Main.rand.NextFloat(0, System.MathF.PI * 2);
                float dist = 40f;
                Dust d = Dust.NewDustPerfect(player.Center + new Vector2(
                    (float)System.Math.Cos(angle) * dist, (float)System.Math.Sin(angle) * dist),
                    DustID.BlueTorch, Vector2.Zero, 200, new Color(0, 255, 255), 0.5f);
                d.noGravity = true; d.fadeIn = 0f;
            }
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        { Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI); return false; }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        { tooltips.Add(new TooltipLine(Mod, "T", "[c/00FFFF:═══ AURA SHIELD ═══]")); tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Escudo de energía cian con hexágonos]")); }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // === 2. AURA SPHERE — esfera de energía con rayo hacia arriba ===
    public class AuraSphere : ModItem
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
        public override void HoldItem(Player player)
        {
            // Esfera grande
            Lighting.AddLight(player.Center, new Vector3(0.15f, 0.2f, 0.25f));
            // Núcleo blanco
            // Rayo hacia arriba
            // Sparkles cuadrados blancos
            if (Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(player.Center + new Vector2(
                    Main.rand.NextFloat(-50, 50), Main.rand.NextFloat(-50, 50)),
                    DustID.BlueTorch, new Vector2(0, -1f), 200, new Color(255, 255, 255), 0.3f);
                d.noGravity = true; d.fadeIn = 0f;
            }
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        { Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI); return false; }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        { tooltips.Add(new TooltipLine(Mod, "T", "[c/00FFFF:═══ AURA SPHERE ═══]")); tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Esfera de energía cian con rayo vertical]")); }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // === 3. AURA DIVINE — aura divina con rayos de luz ===
    public class AuraDivine : ModItem
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
        public override void HoldItem(Player player)
        {
            // Glow púrpura/magenta
            Lighting.AddLight(player.Center, new Vector3(0.3f, 0.1f, 0.3f));
            // Rayos de luz emanando del jugador
            int numRays = 8;
            for (int i = 0; i < numRays; i++)
            {
                float angle = (System.MathF.PI * 2 / numRays) * i + Main.GameUpdateCount * 0.01f;
                Vector2 rayEnd = player.Center + new Vector2(
                    (float)System.Math.Cos(angle) * 60f,
                    (float)System.Math.Sin(angle) * 60f);
                // Dibujar rayo como serie de dust
                for (int j = 0; j < 5; j++)
                {
                    float t = j / 5f;
                    Vector2 pos = Vector2.Lerp(player.Center, rayEnd, t);
                    if (Main.rand.NextBool(2))
                    {
                        Dust d = Dust.NewDustPerfect(pos, DustID.PurpleTorch,
                            Vector2.Zero, 150, new Color(200, 50, 255), 0.4f);
                        d.noGravity = true; d.fadeIn = 0f;
                    }
                }
            }
            // Stardust
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(player.Center + new Vector2(
                    Main.rand.NextFloat(-60, 60), Main.rand.NextFloat(-60, 60)),
                    DustID.Enchanted_Pink, Vector2.Zero, 200, new Color(255, 100, 255), 0.3f);
                d.noGravity = true; d.fadeIn = 0f;
            }
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        { Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI); return false; }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        { tooltips.Add(new TooltipLine(Mod, "T", "[c/FF00FF:═══ AURA DIVINE ═══]")); tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Aura divina púrpura con rayos de luz]")); }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // === 4. AURA BLOOM — expansión radial pulsante ===
    public class AuraBloom : ModItem
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
        public override void HoldItem(Player player)
        {
            // Glow central verde
            Lighting.AddLight(player.Center, new Vector3(0.15f, 0.25f, 0.1f));
            // Partículas verdes
            if (Main.rand.NextBool(5))
            {
                float angle = Main.rand.NextFloat(0, System.MathF.PI * 2);
                float dist = 30f + Main.rand.NextFloat(0, 30f);
                Dust d = Dust.NewDustPerfect(player.Center + new Vector2(
                    (float)System.Math.Cos(angle) * dist, (float)System.Math.Sin(angle) * dist),
                    DustID.GreenTorch, new Vector2((float)System.Math.Cos(angle), (float)System.Math.Sin(angle)), 200, new Color(50, 255, 100), 0.4f);
                d.noGravity = true; d.fadeIn = 0f;
            }
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        { Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI); return false; }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        { tooltips.Add(new TooltipLine(Mod, "T", "[c/50FF64:═══ AURA BLOOM ═══]")); tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Anillos expandiéndose + glow verde]")); }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // === 5. AURA COSMIC — anillo cósmico con partículas que gotean ===
    public class AuraCosmic : ModItem
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
        public override void HoldItem(Player player)
        {
            // Anillo cósmico girando
            // Partículas que gotean del anillo (hacia abajo)
            if (Main.rand.NextBool(3))
            {
                float angle = Main.rand.NextFloat(0, System.MathF.PI * 2);
                float dist = 50f;
                Vector2 dripPos = player.Center + new Vector2(
                    (float)System.Math.Cos(angle) * dist,
                    (float)System.Math.Sin(angle) * dist);
                Dust d = Dust.NewDustPerfect(dripPos, DustID.GoldFlame,
                    new Vector2(0, 1.5f), 150, new Color(255, 217, 61), 0.3f);
                d.noGravity = false; d.fadeIn = 0f;
            }
            // Glow central
            Lighting.AddLight(player.Center, new Vector3(0.25f, 0.2f, 0.05f));
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        { Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI); return false; }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        { tooltips.Add(new TooltipLine(Mod, "T", "[c/FFD700:═══ AURA COSMIC ═══]")); tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Anillo cósmico + partículas que gotean]")); }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // ================================================================
    //  5 EFECTOS DE PROYECTIL NUEVOS
    // ================================================================

    // === 1. PROJ BEAM — rayo de energía con lens flare ===
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
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (proj >= 0 && proj < Main.maxProjectiles) Main.projectile[proj].ai[1] = 4001;
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        { tooltips.Add(new TooltipLine(Mod, "T", "[c/00FFFF:═══ PROJ BEAM ═══]")); tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Rayo de energía con lens flare y bloom]")); }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // === 2. PROJ ELECTRIC — trail eléctrico jagged ===
    public class ProjElectric : ModItem
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
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (proj >= 0 && proj < Main.maxProjectiles) Main.projectile[proj].ai[1] = 4002;
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        { tooltips.Add(new TooltipLine(Mod, "T", "[c/00FFFF:═══ PROJ ELECTRIC ═══]")); tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Trail eléctrico jagged con sparkles]")); }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // === 3. PROJ IMPACT — explosión multicolor al impactar ===
    public class ProjImpact : ModItem
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
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (proj >= 0 && proj < Main.maxProjectiles) Main.projectile[proj].ai[1] = 4003;
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        { tooltips.Add(new TooltipLine(Mod, "T", "[c/50FF64:═══ PROJ IMPACT ═══]")); tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Explosión multicolor al impactar]")); }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // === 4. PROJ RAINBOW — trail arcoíris ===
    public class ProjRainbow : ModItem
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
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (proj >= 0 && proj < Main.maxProjectiles) Main.projectile[proj].ai[1] = 4004;
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        { tooltips.Add(new TooltipLine(Mod, "T", "[c/FF00FF:═══ PROJ RAINBOW ═══]")); tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Trail arcoíris con hue rotation]")); }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // === 5. PROJ LIGHTNING — relámpago alrededor del proyectil ===
    public class ProjLightning : ModItem
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
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (proj >= 0 && proj < Main.maxProjectiles) Main.projectile[proj].ai[1] = 4005;
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        { tooltips.Add(new TooltipLine(Mod, "T", "[c/00FFFF:═══ PROJ LIGHTNING ═══]")); tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Relámpagos alrededor del proyectil]")); }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
