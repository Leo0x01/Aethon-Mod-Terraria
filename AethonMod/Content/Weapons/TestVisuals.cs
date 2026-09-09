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
    //  ARMAS DE PRUEBA VISUAL — v5.35
    //  Solo TestAura y TestRays (mejorados con additive blending)
    // ================================================================

    // === AURA CÓSMICA AL SOSTENER ===
    // Brillo tenue + partículas doradas + glow circle con additive blending.
    public class TestAura : ModItem
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
            // v5.35: Brillo MUY tenue + partículas
            // NOTA: No se puede usar additive blending en HoldItem porque el
            // spriteBatch no está activo (solo se puede en PreDraw/PostDraw).
            float pulse = 0.3f + 0.2f * (float)System.Math.Sin(Main.GameUpdateCount * 0.05f);
            Lighting.AddLight(player.Center, new Vector3(0.3f * pulse, 0.25f * pulse, 0.1f * pulse));

            // Partículas doradas (1/4)
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
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Brillo tenue + partículas + glow additive]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }

    // === RELÁMPAGOS CÓSMICOS AL SOSTENER ===
    // Relámpagos cian que caen cerca del jugador + glow additive.
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
            // v5.35: Relámpagos cian
            // NOTA: No se puede usar additive blending en HoldItem (solo en PreDraw)
            if (Main.rand.NextBool(5))
            {
                float x = player.Center.X + Main.rand.NextFloat(-80, 80);
                float startY = player.Center.Y - Main.rand.NextFloat(100, 180);
                float endY = player.Center.Y + Main.rand.NextFloat(20, 60);

                Vector2 currentPos = new Vector2(x, startY);
                float segmentLength = 12f;
                int numSegments = (int)((endY - startY) / segmentLength);

                for (int i = 0; i < numSegments; i++)
                {
                    float zigzag = Main.rand.NextFloat(-8f, 8f);
                    Vector2 nextPos = new Vector2(currentPos.X + zigzag, currentPos.Y + segmentLength);

                    for (int j = 0; j < 3; j++)
                    {
                        float t = j / 3f;
                        Vector2 dustPos = Vector2.Lerp(currentPos, nextPos, t);
                        Dust d = Dust.NewDustPerfect(dustPos, DustID.BlueTorch,
                            Vector2.Zero, 200, new Color(0, 255, 255), 0.6f);
                        d.noGravity = true; d.fadeIn = 0f;
                    }

                    if (Main.rand.NextBool(4) && i > 1 && i < numSegments - 2)
                    {
                        Vector2 branchEnd = currentPos + new Vector2(Main.rand.NextFloat(-30, 30), Main.rand.NextFloat(15, 30));
                        for (int j = 0; j < 3; j++)
                        {
                            float t = j / 3f;
                            Vector2 dustPos = Vector2.Lerp(currentPos, branchEnd, t);
                            Dust d = Dust.NewDustPerfect(dustPos, DustID.BlueTorch,
                                Vector2.Zero, 180, new Color(100, 200, 255), 0.4f);
                            d.noGravity = true; d.fadeIn = 0f;
                        }
                    }
                    currentPos = nextPos;
                }
                Lighting.AddLight(currentPos, new Vector3(0.2f, 0.8f, 1.0f));
                if (Main.rand.NextBool(3))
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Thunder, currentPos);
            }

            // Brillo tenue del jugador
            Lighting.AddLight(player.Center, new Vector3(0.1f, 0.1f, 0.2f));
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "T", "[c/00FFFF:═══ RELÁMPAGOS CÓSMICOS ═══]"));
            tooltips.Add(new TooltipLine(Mod, "D", "[c/B388FF:Relámpagos cian + glow additive]"));
        }
        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
