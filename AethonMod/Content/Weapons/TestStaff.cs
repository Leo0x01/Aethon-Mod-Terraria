using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// Báculo de Prueba v3 — arma de TESTEO con lógica anti-doble DIFERENTE.
    ///
    /// v5.12: Enfoque completamente nuevo:
    /// - Item.shoot = 931 (Nightglow, mismo que Grimorio)
    /// - Shoot retorna false SIEMPRE (tModLoader NO crea proyectil default)
    /// - Creamos TODOS los proyectiles nosotros con Projectile.NewProjectile
    /// - Item.useTime = Item.useAnimation (evita timing issues)
    /// - Item.reuseDelay = 10 (cooldown forzado entre usos)
    /// - Item.useStyle = Swing (más estable que HoldUp para testing)
    /// - No autoReuse (para ver mejor si hay doble)
    /// </summary>
    public class TestStaff : ModItem
    {
        // Track del último frame en que se disparó
        private static uint _lastFireFrame = 0;
        private static uint _lastMinionFrame = 0;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 10;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.autoReuse = true; // v5.13: permite mantener click para disparar continuo
            Item.shoot = 931; // Nightglow (mismo que Grimorio)
            Item.shootSpeed = 12f;
            Item.mana = 2;
            Item.noMelee = true;
            Item.reuseDelay = 10; // v5.12: cooldown forzado entre usos
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            // Click derecho: invocar minion (requiere mana)
            if (player.altFunctionUse == 2)
            {
                return player.statMana >= 10 || player.manaFlower;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // === v5.12: LÓGICA ANTI-DOBLE v3 ===
            // Enfoque: return false SIEMPRE. Creamos TODO nosotros.
            // El anti-doble se basa en frame count + reuseDelay.

            uint currentFrame = Main.GameUpdateCount;

            // === CLICK DERECHO: invocar minion ===
            if (player.altFunctionUse == 2)
            {
                // Anti-doble: si ya invocamos minion en este frame, ignorar
                if (currentFrame == _lastMinionFrame)
                {
                    return false;
                }
                _lastMinionFrame = currentFrame;

                // Contar minions actuales
                int currentMinions = 0;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active &&
                        Main.projectile[i].owner == player.whoAmI &&
                        Main.projectile[i].minion)
                        currentMinions++;
                }

                if (currentMinions >= player.maxMinions)
                {
                    if (Main.myPlayer == player.whoAmI)
                        Main.NewText($"Slots llenos: {currentMinions}/{player.maxMinions}",
                            new Color(255, 120, 120));
                    return false;
                }

                // Cobrar mana UNA vez
                int minionCost = 10;
                if (player.statMana < minionCost && !player.manaFlower) return false;
                if (player.statMana >= minionCost)
                {
                    player.statMana -= minionCost;
                    if (player.statMana < 0) player.statMana = 0;
                }

                // Invocar minion UNA vez
                player.AddBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.CosmicOrbBuff>(), 18000);
                Projectile.NewProjectile(source, position, Vector2.Zero,
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.CosmicOrbMinion>(),
                    damage, knockback, player.whoAmI);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113);

                if (Main.myPlayer == player.whoAmI)
                    Main.NewText($"Minion invocado ({currentMinions + 1}/{player.maxMinions})",
                        new Color(245, 196, 81));
                return false; // NO dejar que tModLoader cree proyectil
            }

            // === CLICK IZQUIERDO: dispara EXACTAMENTE 1 proyectil ===
            // Anti-doble: si ya disparamos en este frame, ignorar
            if (currentFrame == _lastFireFrame)
            {
                return false;
            }
            _lastFireFrame = currentFrame;

            // Crear EXACTAMENTE 1 proyectil (Nightglow = 931, mismo que Grimorio)
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);

            if (Main.myPlayer == player.whoAmI)
                Main.NewText($"Proyectil lanzado (1) frame {currentFrame}",
                    new Color(245, 196, 81));
            return false; // return false SIEMPRE: tModLoader NO crea proyectil default
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int minionCount = 0;
            if (Main.LocalPlayer != null)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active &&
                        Main.projectile[i].owner == Main.LocalPlayer.whoAmI &&
                        Main.projectile[i].minion)
                        minionCount++;
                }
            }

            tooltips.Add(new TooltipLine(Mod, "TestInfo", "[c/78FF96:═══ ARMA DE PRUEBA v3 ═══]"));
            tooltips.Add(new TooltipLine(Mod, "Desc1",
                "[c/B388FF:Arma para testear lógica anti-doble (v3)]"));
            tooltips.Add(new TooltipLine(Mod, "Desc2",
                "[c/B388FF:Click izq: 1 Nightglow (return false + creamos 1)]"));
            tooltips.Add(new TooltipLine(Mod, "Desc3",
                "[c/B388FF:Click der: 1 minion (anti-doble con frame count)]"));
            tooltips.Add(new TooltipLine(Mod, "Desc4",
                "[c/78788C:useStyle=Swing, reuseDelay=10, autoReuse=true]"));
            tooltips.Add(new TooltipLine(Mod, "Status",
                $"[c/FFD700:Minions activos: {minionCount}/{(Main.LocalPlayer != null ? Main.LocalPlayer.maxMinions : 0)}]"));
            tooltips.Add(new TooltipLine(Mod, "Cost",
                "[c/55AAFF:Costo: 2 mana bolt | 10 mana minion]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 10)
                .Register();
        }
    }
}
