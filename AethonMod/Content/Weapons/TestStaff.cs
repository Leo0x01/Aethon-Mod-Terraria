using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// Báculo de Prueba v2 — arma de TESTEO con lógica anti-doble DIFERENTE.
    ///
    /// v5.11: Enfoque nuevo para resolver el doble disparo:
    /// - Item.shoot = ProjectileID.None (sin proyectil default de tModLoader)
    /// - Item.reuseDelay = 5 (forza 5 frames de cooldown entre usos)
    /// - Shoot retorna false SIEMPRE (tModLoader nunca crea proyectil default)
    /// - Todos los proyectiles se crean manualmente con Projectile.NewProjectile
    /// - Verificación con player.itemAnimation para evitar re-disparo en la misma animación
    /// </summary>
    public class TestStaff : ModItem
    {
        // Track del último frame en que se disparó (uint = Main.GameUpdateCount)
        private static uint _lastFireFrame = 0;
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
            Item.autoReuse = true;
            // v5.11 CLAVE: shoot = None para que tModLoader NO cree proyectil default
            Item.shoot = ProjectileID.None;
            Item.shootSpeed = 12f;
            Item.mana = 2;
            Item.noMelee = true;
            // v5.11: reuseDelay fuerza un cooldown entre usos
            Item.reuseDelay = 5;
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
            // === v5.11: LÓGICA ANTI-DOBLE NUEVA ===
            // Como Item.shoot = None, el parámetro 'type' será -1 o 0.
            // tModLoader NO crea ningún proyectil default.
            // Nosotros creamos TODO aquí y retornamos false.

            uint currentFrame = Main.GameUpdateCount;

            // === CLICK DERECHO: invocar minion ===
            if (player.altFunctionUse == 2)
            {
                // Anti-doble: verificar si ya invocamos un minion en este frame
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
                return false;
            }

            // === CLICK IZQUIERDO: dispara EXACTAMENTE 1 proyectil ===
            // v5.11: Anti-doble usando frame count
            // Solo permitimos 1 disparo por frame (incluso si Shoot se llama 2 veces)
            if (currentFrame == _lastFireFrame)
            {
                return false;
            }
            _lastFireFrame = currentFrame;

            // Crear EXACTAMENTE 1 proyectil (Nightglow = 931, mismo que Grimorio)
            int projType = 931; // Nightglow
            Projectile.NewProjectile(source, position, velocity, projType, damage, knockback, player.whoAmI);

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

            tooltips.Add(new TooltipLine(Mod, "TestInfo", "[c/78FF96:═══ ARMA DE PRUEBA v2 ═══]"));
            tooltips.Add(new TooltipLine(Mod, "Desc1",
                "[c/B388FF:Arma para testear lógica anti-doble (v2)]"));
            tooltips.Add(new TooltipLine(Mod, "Desc2",
                "[c/B388FF:Click izq: 1 Nightglow (shoot=None + return false)]"));
            tooltips.Add(new TooltipLine(Mod, "Desc3",
                "[c/B388FF:Click der: 1 minion (anti-doble con frame count)]"));
            tooltips.Add(new TooltipLine(Mod, "Desc4",
                "[c/78788C:reuseDelay=5, shoot=None]"));
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
