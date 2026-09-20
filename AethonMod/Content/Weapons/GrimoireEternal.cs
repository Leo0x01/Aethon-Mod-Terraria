using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.Systems;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// Grimorio del Eterno — arma mágica híbrida (magia + invocación).
    ///
    /// SISTEMA DE NIVELES:
    /// - Sube de nivel al matar enemigos (XP guardada en ShardLevelItem).
    /// - v6.45: XP REAL por rareza del bestiario (0–5★), jefes a tope y
    ///   hardmode ×2; el arma gana XP desde TODO el inventario.
    /// - Niveles infinitos. Sin dependencias de BranchType ni IsImprinted.
    ///
    /// CLICK IZQUIERDO: dispara ArcaneBolt (homing). SIN MANÁ (v6.42, arsenal de pruebas).
    /// CLICK DERECHO: invoca CosmicOrbMinion. SIN MANÁ en pruebas
    /// (v6.45, bandera ManaGratisEnPruebas — apagarla restaura el coste).
    ///
    /// ESCALADO POR NIVEL:
    /// - +2.2% daño mágico, +1% daño summon, +0.2% crit, +0.4% armor pen
    /// - -0.3% use time (tope -25%)
    /// - +1 slot de minion cada 5 niveles
    /// - +1 bolt extra cada 5 niveles
    /// - Lifesteal nivel 7+: +0.1% cada 7 niveles
    /// - Bonus por mana faltante: +0.5% daño por 1% mana faltante (tope +50%)
    /// </summary>
    public class GrimoireEternal : ModItem
    {
        // v5.10: Anti-doble (migrado del TestStaff que funciona).
        // Previene que Shoot se procese dos veces en el mismo frame,
        // lo que causaba que se lanzaran 2 proyectiles y se invocaran
        // 2 minions por click.
        // v5.18: Renombrado _lastShootFrame → _lastFireFrame para coincidir
        // con TestStaff, y movido el check DESPUÉS del bloque del minion
        // (igual que TestStaff que funciona).
        // v5.25: Cooldown ELIMINADO — ya no es necesario porque UseSpeedMult
        // ahora redondea el useTime a entero, evitando el doble Shoot.
        private static uint _lastFireFrame = 0;
        private static uint _lastMinionFrame = 0;

        // OnCraft eliminado: el evento cinematográfico (LevelUpEventSystem) fue
        // removido por request del usuario. El crafteo del Grimorio ya no produce
        // temblor de pantalla ni texto de lore.

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 11;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 38;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.HoldUp; // v5.18: restaurado (la animación no era el problema)
            Item.knockBack = 3f;
            Item.value = Item.buyPrice(0, 10, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.autoReuse = true; // v5.1: permite mantener click para disparar continuo
            Item.shoot = 931; // Nightglow (fix 48688dd) — proyectil vanilla con homing
            Item.shootSpeed = 12f;
            Item.mana = 0;    // v6.42: SIN MANÁ (la regla de la casa — todas son armas de pruebas)
            Item.noMelee = true;
            Item.reuseDelay = 10; // v5.16: cooldown forzado (migrado del TestStaff que funciona)
        }

        /// <summary>
        /// Obtiene el ShardLevelItem del arma. Defensivo: try/catch.
        /// </summary>
        private ShardLevelItem GetShard(Item item)
        {
            try { return item.GetGlobalItem<ShardLevelItem>(); }
            catch { return null; }
        }

        /// <summary>
        /// v6.45: ¿el minion NO cuesta maná? Bandera de pruebas en config
        /// (ManaGratisEnPruebas, ON por defecto). Null-safe: sin config
        /// cargada se asume modo pruebas (el mod entero lo es).
        /// </summary>
        private static bool ManaMinionGratis()
        {
            var config = ModContent.GetInstance<Content.AethonConfig>();
            return config == null || config.ManaGratisEnPruebas;
        }

        // ================================================================
        //  ESCALADO DE DAÑO
        // ================================================================

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var sl = GetShard(Item);
            if (sl == null) return;

            // +2.2% daño mágico por nivel (Infinito) — local a ESTE arma.
            damage *= WeaponScaling.MagicDamageMult(sl.Level);

            // BONUS POR MANA FALTANTE — la mitad mágica es por-golpe (aquí);
            // la mitad de invocación vive en ShardPlayer (v6.45, ver abajo).
            damage *= WeaponScaling.LowManaDamageMult(player.statMana, player.statManaMax2);

            // v6.45: EL BONUS DE SUMMON MOVIDO AL SITIO CORRECTO. Antes las
            // tres líneas siguientes mutaban stats GLOBALES del jugador
            // (daño de invocación, crítico mágico y penetración) desde este
            // hook, que solo corre al calcular el daño de ESTE arma: los
            // minions atacando en otros ticks no recibían nada (la misma
            // clase de letra muerta que la regeneración de la auditoría R44).
            // Ahora ShardPlayer.PostUpdateEquips las aplica persistentemente
            // mientras el Grimorio esté en el inventario.
            //   player.GetDamage(DamageClass.Summon)      += SummonDamageBonus
            //   player.GetCritChance(DamageClass.Magic)   += CritBonus
            //   player.GetArmorPenetration(DamageClass.Magic) += ArmorPenBonus
        }

        public override void ModifyWeaponKnockback(Player player, ref StatModifier knockback)
        {
            var sl = GetShard(Item);
            if (sl == null) return;
            // +10% knockback cada 10 niveles (tope +100%)
            knockback *= WeaponScaling.KnockbackMult(sl.Level);
        }

        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            // v6.42: SIN MANÁ (la regla de la casa — todas son armas de
            // pruebas): el coste escalado se retira; el nivel sigue
            // escalando daño/velocidad, ya no el maná.
            Item.mana = 0;
            reduce = 1f;
            mult = 0f;
        }

        public override float UseTimeMultiplier(Player player)
        {
            var sl = GetShard(Item);
            if (sl == null) return 1f;
            // v5.25: Pasar Item.useTime como base para que UseSpeedMult
            // redondee el useTime efectivo a entero (evita doble Shoot)
            return WeaponScaling.UseSpeedMult(sl.Level, Item.useTime);
        }

        /// <summary>
        /// v5.26: UseAnimationMultiplier — reduce useAnimation proporcionalmente
        /// al useTime para que NUNCA sea menor. Si useAnimation < useTime efectivo,
        /// tModLoader dispara Shoot 2 veces por ciclo de animación.
        /// Aplicar el mismo multiplier a useAnimation lo arregla.
        /// </summary>
        public override float UseAnimationMultiplier(Player player)
        {
            var sl = GetShard(Item);
            if (sl == null) return 1f;
            return WeaponScaling.UseSpeedMult(sl.Level, Item.useAnimation);
        }

        // ================================================================
        //  USO DEL ARMA
        // ================================================================

        public override bool CanUseItem(Player player)
        {
            var sl = GetShard(Item);
            int level = sl?.Level ?? 1;

            // Click derecho (minion): permite Mana Flower (fix 48688dd)
            // v6.45: en modo pruebas NO se exige maná (bandera); con la
            // bandera OFF vuelve el coste escalado + Mana Flower.
            if (player.altFunctionUse == 2)
            {
                if (ManaMinionGratis()) return true;
                int minionCost = WeaponScaling.MinionManaCost(level);
                return player.statMana >= minionCost || player.manaFlower;
            }
            // Click izquierdo (bolt): return true — Terraria maneja mana + Mana Flower (fix 48688dd)
            return true;
        }

        public override bool AltFunctionUse(Player player) => true;

        // v5.22: Eliminado UseItem override (el TestStaff no lo tiene y funciona)
        // v5.4: Eliminado CanRightClick() y RightClick() porque CanRightClick=true
        // hacia que tModLoader interpretara el click derecho como 'consumir item'
        // (como una poción), haciendo que el Grimorio desapareciera del inventario.
        // La alternancia del tooltip ahora se maneja via ItemSlot.LeftClick en
        // el GlobalItem TooltipToggleItem (ver Content/Globals/TooltipToggleItem.cs).

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // === v5.18: LÓGICA ANTI-DOBLE IDÉNTICA AL TESTSTAFF (que funciona) ===
            // El anti-doble del fire está DESPUÉS del bloque del minion (igual que TestStaff)
            uint currentFrame = Main.GameUpdateCount;

            var sl = GetShard(Item);
            int level = sl?.Level ?? 1;

            // === CLICK DERECHO: invocar minion ===
            if (player.altFunctionUse == 2)
            {
                // Anti-doble específico para minions
                if (currentFrame == _lastMinionFrame)
                {
                    return false;
                }
                _lastMinionFrame = currentFrame;

                int maxMinions = player.maxMinions;
                int currentMinions = 0;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active && Main.projectile[i].owner == player.whoAmI &&
                        Main.projectile[i].minion)
                        currentMinions++;
                }

                if (currentMinions >= maxMinions)
                {
                    if (Main.myPlayer == player.whoAmI)
                        Main.NewText($"Slots de minion llenos: {currentMinions}/{maxMinions}.",
                            new Color(255, 120, 120));
                    return false;
                }

                // v6.45: cobrar mana del minion SOLO con la bandera de coste
                // activa ( ManaGratisEnPruebas = OFF ). En modo pruebas la
                // invocación es gratis como el resto del arsenal (v6.42).
                if (!ManaMinionGratis())
                {
                    // Cobrar mana del minion (fix 48688dd: maneja Mana Flower)
                    int minionCost = WeaponScaling.MinionManaCost(level);
                    if (player.statMana < minionCost && !player.manaFlower) return false;
                    if (player.statMana >= minionCost)
                    {
                        player.statMana -= minionCost;
                        if (player.statMana < 0) player.statMana = 0;
                    }
                }

                // Invocar minion (EXACTAMENTE una vez)
                player.AddBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.CosmicOrbBuff>(), 18000);
                int minionProj = Projectile.NewProjectile(source, position, Vector2.Zero,
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.CosmicOrbMinion>(),
                    damage, knockback, player.whoAmI);
                // v5.29: Cachear el nivel del Grimorio en ai[2] para que el minion
                // mantenga sus stats (velocidad, cooldown, rango, daño) aunque
                // el jugador cambie de arma después de invocarlo.
                if (minionProj >= 0 && minionProj < Main.maxProjectiles)
                {
                    Main.projectile[minionProj].ai[2] = level;
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113);
                return false;
            }

            // === CLICK IZQUIERDO: Nightglow (1 base + extras por nivel) ===
            // Anti-doble simple (mismo frame check).
            if (currentFrame == _lastFireFrame)
            {
                return false;
            }
            _lastFireFrame = currentFrame;

            int extra = WeaponScaling.ExtraProjectiles(level);

            // Proyectil principal (1 exacto)
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);

            // Bolts extra en abanico
            for (int i = 0; i < extra; i++)
            {
                float angle = (i + 1) * 0.12f * (i % 2 == 0 ? 1f : -1f);
                Vector2 perturbedVel = velocity.RotatedBy(angle);
                Projectile.NewProjectile(source, position, perturbedVel, type, damage, knockback, player.whoAmI);
            }

            return false; // return false → tModLoader NO dispara proyectil extra
        }

        // ================================================================
        //  TOOLTIP
        // ================================================================

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // Ocultar línea vanilla "Level: X"
            for (int i = tooltips.Count - 1; i >= 0; i--)
            {
                string t = tooltips[i].Text ?? "";
                if (tooltips[i].Name == "Level" || tooltips[i].Name == "ItemLevel" ||
                    t.StartsWith("Level:") || t.StartsWith("Level："))
                    tooltips.RemoveAt(i);
            }

            var sl = GetShard(Item);
            if (sl == null) return;

            // === v5.14: En vista COMPLETA, ocultar líneas vanilla (básicas) ===
            // Las líneas vanilla (Damage, CritChance, Knockback, UseMana, Tooltip, etc.)
            // son info básica que ya no se necesita en la vista completa porque
            // tenemos secciones detalladas (DAÑO, RECURSOS, etc.)
            if (Globals.TooltipToggleItem.ShowExtendedTooltip)
            {
                for (int i = tooltips.Count - 1; i >= 0; i--)
                {
                    string name = tooltips[i].Name ?? "";
                    // Ocultar TODAS las líneas vanilla y de modifier
                    // Líneas vanilla: Damage, CritChance, Speed, Knockback, UseMana, ManaCost, ItemLevel
                    // Tooltip: "Tooltip0", "Tooltip1", etc. (usar StartsWith)
                    // Modifier: "PrefixDamage", "PrefixSpeed", "PrefixCrit", etc. (usar StartsWith)
                    if (name == "Damage" || name == "CritChance" || name == "Speed" ||
                        name == "Knockback" || name == "UseMana" || name == "ManaCost" ||
                        name == "BestiaryNotes" || name == "ItemLevel" ||
                        name.StartsWith("Tooltip") ||     // Tooltip0, Tooltip1, etc.
                        name.StartsWith("Prefix"))        // PrefixDamage, PrefixSpeed, etc.
                    {
                        tooltips.RemoveAt(i);
                    }
                }
            }

            // === v5.11: SummonDamage SOLO en vista básica (no en completa) ===
            int summonDmg = (int)(Item.damage * (1f + sl.Level * WeaponScaling.SummonDamagePerLevel));

            // === v5.2: TOOLTIP CON 2 MODOS ===
            // Modo básico (default): solo nivel + XP + próximo hito (ventana pequeña)
            // Modo completo (click derecho): todas las estadísticas

            // Calcular todos los valores
            int xpNeeded = sl.XPForNextLevel();
            float pct = xpNeeded > 0 ? (float)sl.XP / xpNeeded : 0f;
            pct = System.Math.Clamp(pct, 0f, 1f);
            int barLen = 20;
            int filled = (int)(barLen * pct);
            string bar = "";
            for (int i = 0; i < barLen; i++)
                bar += i < filled ? "█" : "░";

            int nextMilestoneLevel = ((sl.Level / 5) + 1) * 5;
            int nextMilestoneNum = nextMilestoneLevel / 5;
            var nextRewards = WeaponScaling.MilestoneRewards(nextMilestoneNum);

            // Indicador de modo (básico/completo)
            // v5.7: Usa flag estático de TooltipToggleItem (mismo patrón que SeerOrb)
            // v5.8: 'Próximo hito' solo en vista básica (no duplicar en completa)
            // v5.9: Sección PROGRESIÓN también solo en vista básica
            if (Globals.TooltipToggleItem.ShowExtendedTooltip)
            {
                // === MODO COMPLETO: mostrar todas las estadísticas (sin Próximo hito) ===
                int magicDmgPct = (int)(sl.Level * WeaponScaling.MagicDamagePerLevel * 100);
                int summonDmgPct = (int)(sl.Level * WeaponScaling.SummonDamagePerLevel * 100);
                float critPct = WeaponScaling.CritBonus(sl.Level);
                float armorPenPct = WeaponScaling.ArmorPenBonus(sl.Level);
                int bonusSlots = WeaponScaling.BonusMinionSlots(sl.Level);
                int bonusMana = WeaponScaling.BonusMana(sl.Level);
                int bonusLife = WeaponScaling.BonusLife(sl.Level);
                int manaRegen = WeaponScaling.ManaRegen(sl.Level);
                float lifeRegen = WeaponScaling.LifeRegen(sl.Level);
                float dmgRed = WeaponScaling.DamageReduction(sl.Level) * 100f;
                float kbBonus = (WeaponScaling.KnockbackMult(sl.Level) - 1f) * 100f;
                // v6.45: ManaCost eliminado — el bolt es SIN MANÁ desde v6.42
                // (Item.mana = 0): la línea "Costo: X mana por disparo" era
                // una promesa rota en el tooltip.
                int minionCost = WeaponScaling.MinionManaCost(sl.Level);
                int totalBolts = 1 + WeaponScaling.ExtraProjectiles(sl.Level);
                int areaDmg = WeaponScaling.BoltAreaDamage(sl.Level);
                float lowManaBonus = (WeaponScaling.LowManaDamageMult(Main.LocalPlayer.statMana, Main.LocalPlayer.statManaMax2) - 1f) * 100f;
                int hitCd = WeaponScaling.MinionHitCooldown(sl.Level);
                float contactDmg = (WeaponScaling.MinionContactDamageMult(sl.Level) - 1f) * 100f;
                float minionSpd = (WeaponScaling.MinionSpeedMult(sl.Level) - 1f) * 100f;
                float detectRange = WeaponScaling.MinionDetectionRange(sl.Level);

                tooltips.Add(new TooltipLine(Mod, "SectionDamage", "[c/FFD700:═══ DAÑO ═══]"));
                tooltips.Add(new TooltipLine(Mod, "MagicDamage",
                    $"[c/FF5555:+{magicDmgPct}% daño mágico]"));
                tooltips.Add(new TooltipLine(Mod, "SummonDamageScale",
                    $"[c/BE78FD:+{summonDmgPct}% daño de invocación]"));
                tooltips.Add(new TooltipLine(Mod, "Crit",
                    $"[c/FFAA55:+{critPct:F1}% probabilidad crítica]"));
                tooltips.Add(new TooltipLine(Mod, "ArmorPen",
                    $"[c/55FFFF:+{armorPenPct:F0}% penetración de armadura]"));
                // v6.45: MinionSlots movido a la sección ORBE CÓSMICO — el
                // bonus de summon en su sitio correcto, junto al resto de
                // stats del minion (antes flotaba en la sección de DAÑO).
                tooltips.Add(new TooltipLine(Mod, "Knockback",
                    $"[c/FFAA55:+{kbBonus:F0}% retroceso]"));

                tooltips.Add(new TooltipLine(Mod, "SectionResources", "[c/55AAFF:═══ RECURSOS ═══]"));
                tooltips.Add(new TooltipLine(Mod, "ManaMax",
                    $"[c/55AAFF:+{bonusMana} mana máximo]"));
                tooltips.Add(new TooltipLine(Mod, "LifeMax",
                    $"[c/55AAFF:+{bonusLife} vida máxima]"));
                tooltips.Add(new TooltipLine(Mod, "ManaRegen",
                    $"[c/55AAFF:+{manaRegen} mana/seg regeneración]"));
                tooltips.Add(new TooltipLine(Mod, "LifeRegen",
                    $"[c/FF5566:+{lifeRegen:F1} vida/seg regeneración]"));
                tooltips.Add(new TooltipLine(Mod, "DamageRed",
                    $"[c/FFAA55:-{dmgRed:F0}% daño recibido]"));

                tooltips.Add(new TooltipLine(Mod, "SectionProjectile", "[c/FFD700:═══ PROYECTIL ═══]"));
                tooltips.Add(new TooltipLine(Mod, "Bolts",
                    $"[c/55AAFF:{totalBolts} proyectil(es) por disparo]"));
                tooltips.Add(new TooltipLine(Mod, "AreaDmg",
                    $"[c/55AAFF:+{areaDmg}px daño en área]"));
                // v6.45: la línea "Costo: X mana por disparo" eliminada — el
                // bolt no cuesta maná desde v6.42 (promesa rota).

                tooltips.Add(new TooltipLine(Mod, "SectionMinion", "[c/BE78FD:═══ ORBE CÓSMICO ═══]"));
                // v6.45: los slots de minion viven aquí — junto al resto del
                // summon (antes en la sección DAÑO).
                tooltips.Add(new TooltipLine(Mod, "MinionSlots",
                    $"[c/78FF96:+{bonusSlots} slot(s) de minion]"));
                tooltips.Add(new TooltipLine(Mod, "MinionContactDmg",
                    $"[c/BE78FD:+{contactDmg:F0}% daño de contacto]"));
                tooltips.Add(new TooltipLine(Mod, "MinionSpeed",
                    $"[c/BE78FD:+{minionSpd:F0}% velocidad]"));
                tooltips.Add(new TooltipLine(Mod, "MinionRange",
                    $"[c/BE78FD:{detectRange:F0}px rango de detección]"));
                tooltips.Add(new TooltipLine(Mod, "MinionCd",
                    $"[c/BE78FD:{hitCd} frames de cooldown]"));
                // v6.45: el costo de invocación solo se anuncia cuando está
                // ACTIVO (bandera de pruebas OFF) — en modo gratis mostrar
                // un costo sería otra promesa rota.
                if (!ManaMinionGratis())
                {
                    tooltips.Add(new TooltipLine(Mod, "ManaMinion",
                        $"[c/55AAFF:Costo: {minionCost} mana por invocación]"));
                }

                tooltips.Add(new TooltipLine(Mod, "SectionBonus", "[c/FF5566:═══ BONUS ═══]"));
                tooltips.Add(new TooltipLine(Mod, "LowMana",
                    $"[c/FF5555:★ Mana bajo: +{lowManaBonus:F0}% daño (máximo +50%)]"));

                if (WeaponScaling.HasLifesteal(sl.Level))
                {
                    float lsPct = WeaponScaling.LifestealPercent(sl.Level) * 100f;
                    int nextLs = ((sl.Level / 7) + 1) * 7;
                    tooltips.Add(new TooltipLine(Mod, "Lifesteal",
                        $"[c/FF5566:♥ Robo de vida: +{lsPct:F2}% (próximo nivel {nextLs})]"));
                }
                else
                {
                    tooltips.Add(new TooltipLine(Mod, "LifestealLocked",
                        $"[c/78788C:♥ Robo de vida se desbloquea en nivel 7]"));
                }

                tooltips.Add(new TooltipLine(Mod, "ModeIndicator",
                    $"[c/78788C:═══ Click der para vista básica ═══]"));
            }
            else
            {
                // === MODO BÁSICO: SummonDamage + PROGRESIÓN + Próximo hito + indicador ===
                // v5.11: SummonDamage (daño de invocación) solo aquí (no en vista completa)
                // v5.62: SummonDamage en blanco (antes morado BE78FD) y justo después del
                //        daño mágico vanilla (línea "Damage"), no al final del tooltip.
                int summonDmgIndex = -1;
                for (int i = 0; i < tooltips.Count; i++)
                {
                    if (tooltips[i].Name == "Damage")
                    {
                        summonDmgIndex = i + 1; // insertar después del daño mágico
                        break;
                    }
                }
                var summonLine = new TooltipLine(Mod, "SummonDamage",
                    $"{summonDmg} daño de invocación"); // sin [c/...] = color blanco default
                if (summonDmgIndex >= 0 && summonDmgIndex < tooltips.Count)
                    tooltips.Insert(summonDmgIndex, summonLine);
                else
                    tooltips.Add(summonLine);

                tooltips.Add(new TooltipLine(Mod, "SectionProgress", "[c/78FF96:═══ PROGRESIÓN ═══]"));
                tooltips.Add(new TooltipLine(Mod, "Level",
                    $"[c/FFD700:Nivel {sl.Level}]  [c/B388FF:{bar} {sl.XP}/{xpNeeded} XP]"));
                tooltips.Add(new TooltipLine(Mod, "NextMilestone",
                    $"[c/78788C:Próximo hito: Nivel {nextMilestoneLevel}]"));
                foreach (var reward in nextRewards)
                {
                    tooltips.Add(new TooltipLine(Mod, "MS_" + reward.GetHashCode(),
                        $"[c/78FF96:  • {reward}]"));
                }
                tooltips.Add(new TooltipLine(Mod, "ModeIndicator",
                    $"[c/78788C:═══ Click der para vista completa ═══]"));
            }
        }
    }
}
