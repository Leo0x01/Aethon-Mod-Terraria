using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// Báculo de Prueba — arma de TESTEO para experimentar con lógica de
    /// lanzamiento de proyectiles e invocación de minions SIN tocar el Grimorio.
    ///
    /// v5.8: Creada para resolver 2 problemas reportados por el usuario:
    /// 1. El Grimorio lanza 2 proyectiles por click (debería ser 1)
    /// 2. Los minions en niveles altos se invocan doble (gastando el doble de mana)
    ///
    /// Enfoque del TestStaff:
    /// - Click izquierdo: dispara EXACTAMENTE 1 proyectil (return false + crea 1)
    ///   en vez de return true (que deja que tModLoader dispare +1)
    /// - Click derecho: invoca minion con verificación estricta de no-duplicación
    ///   usando un flag estático para evitar que Shoot se procese dos veces
    /// - Tooltip muestra contadores en tiempo real para debug
    /// </summary>
    public class TestStaff : ModItem
    {
        // Flag estático para evitar que Shoot se procese dos veces en el mismo click
        // (puede pasar con autoReuse=true o con certain use styles)
        // Usamos uint porque Main.GameUpdateCount retorna uint (no int)
        private static uint _lastShootFrame = 0;
        private static uint _lastMinionFrame = 0;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 10;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.autoReuse = true; // permite mantener click
            Item.shoot = ProjectileID.WoodenArrowFriendly; // proyectil vanilla simple
            Item.shootSpeed = 12f;
            Item.mana = 2;
            Item.noMelee = true;
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
            // === ANTI-DOBLE: verificar si ya procesamos un Shoot en este frame ===
            // Esto previene que el proyectil se lance 2 veces por click
            uint currentFrame = Main.GameUpdateCount;
            if (currentFrame == _lastShootFrame)
            {
                // Ya procesamos un Shoot en este frame → ignorar (evita doble)
                return false;
            }
            _lastShootFrame = currentFrame;

            // === CLICK DERECHO: invocar minion ===
            if (player.altFunctionUse == 2)
            {
                // Anti-doble para minions
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

                // Cobrar mana EXACTAMENTE UNA vez
                int minionCost = 10;
                if (player.statMana < minionCost && !player.manaFlower) return false;
                if (player.statMana >= minionCost)
                {
                    player.statMana -= minionCost;
                    if (player.statMana < 0) player.statMana = 0;
                }

                // Invocar minion EXACTAMENTE UNA vez
                player.AddBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.CosmicOrbBuff>(), 18000);
                Projectile.NewProjectile(source, position, Vector2.Zero,
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.CosmicOrbMinion>(),
                    damage, knockback, player.whoAmI);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113);

                if (Main.myPlayer == player.whoAmI)
                    Main.NewText($"Minion invocado ({currentMinions + 1}/{player.maxMinions})",
                        new Color(245, 196, 81));
                return false; // return false: tModLoader NO dispara proyectil extra
            }

            // === CLICK IZQUIERDO: dispara EXACTAMENTE 1 proyectil ===
            // Enfoque diferente al Grimorio:
            // - Creamos el proyectil nosotros con Projectile.NewProjectile
            // - Retornamos FALSE para que tModLoader NO dispare el proyectil default
            // - Así garantizamos exactamente 1 proyectil por click
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);

            if (Main.myPlayer == player.whoAmI)
                Main.NewText($"Proyectil lanzado (1)",
                    new Color(245, 196, 81));
            return false; // return false: tModLoader NO dispara proyectil extra
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // Tooltip de debug — muestra contadores en tiempo real
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

            tooltips.Add(new TooltipLine(Mod, "TestInfo", "[c/78FF96:═══ ARMA DE PRUEBA ═══]"));
            tooltips.Add(new TooltipLine(Mod, "Desc1",
                "[c/B388FF:Arma para testear lógica de proyectiles y minions.]"));
            tooltips.Add(new TooltipLine(Mod, "Desc2",
                "[c/B388FF:Click izq: 1 proyectil exacto (return false)"));
            tooltips.Add(new TooltipLine(Mod, "Desc3",
                "[c/B388FF:Click der: 1 minion exacto (anti-doble)]"));
            tooltips.Add(new TooltipLine(Mod, "Status",
                $"[c/FFD700:Minions activos: {minionCount}/{(Main.LocalPlayer != null ? Main.LocalPlayer.maxMinions : 0)}]"));
            tooltips.Add(new TooltipLine(Mod, "Cost",
                "[c/55AAFF:Costo: 2 mana bolt | 10 mana minion]"));
        }

        public override void AddRecipes()
        {
            // Crafteable con madera para fácil acceso en testing
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 10)
                .Register();
        }
    }
}
