using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.Systems;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// GRIMORIO DEL ETERNO — v6.50.102 — EL NERVIOSO JUGABLE.
    ///
    /// La letra del usuario: «creo que me quedo con el Equinoccio […] usa
    /// el proyectil del Equinoccio para el grimorio del eterno, también es
    /// hora de cambiar el sprite del grimorio del eterno, usaremos el
    /// Grimorio hambriento nervioso y su nueva mecánica para el grimorio
    /// del eterno […] el grimorio del eterno tendrá las mecánicas del
    /// grimorio hambriento nervioso pero esta vez será jugable no de
    /// prueba, así que ese minuto que dura el grimorio hambriento
    /// nervioso…». EL MERGE COMPLETO, en una sola arma:
    ///
    /// · EL CUERPO: el sprite del Grimorio Hambriento Nervioso (la familia
    ///   del ojo — temblor, glitch en tiras, sobresaltos, iris rojo) con
    ///   todas sus capas y su dibujo vivo.
    /// · EL RELOJ JUGABLE: el hambre sube 0→100% en UNA HORA
    ///   (1/216000 por tick — el Nervioso de prueba corre su minuto de
    ///   demo; la letra de la .103: «el reloj de hambre debe durar una
    ///   hora en llegar a 100% si no se alimenta el libro» — los 15 min
    ///   de la .102 se leyeron MUY RÁPIDOS). Debajo del 25% EL CAZADOR
    ///   callado; del 25% sale a cazar
    ///   si el dueño está quieto 10 s (la demo usa 5) o lleva 30 s sin
    ///   darle uso; del 50% EL IMPACIENTE — ojo ansioso, voz, temblor,
    ///   glitch, sobresaltos y las OLEADAS probables (4%→20% cada 5 s).
    ///   Sólo la presa comida lo baja (−1%): el clic derecho ya NO
    ///   reinicia el apetito — el Eterno se alimenta CAZANDO, no de gratis.
    /// · LA CAZA PROPIA: el espíritu (GrimorioNerviosoFlotante) sale con
    ///   el TIPO del Eterno en ai[2] — su Sombra de la Página devora
    ///   presas ENTERAS (sin loot, sin crédito) y el festín egoísta paga
    ///   LA XP DEL ETERNO: el libro que come es el libro que sube de
    ///   nivel (ShardLevelItem).
    /// · EL DISPARO: EL EQUINOCCIO con los parámetros del Eterno — cada
    ///   disparo abre LA FUENTE DIVIDIDA sobre el dueño (o sobre el libro
    ///   flotante, si el espíritu está fuera: la regla del Nervioso):
    ///   EL DORADO con luz de sol y sus ocho rayos + EL NEGRO, disco de
    ///   eclipse con rim violeta (la bruma murió con la .102). Cada
    ///   «bolt» del sistema de niveles es una pareja día/noche completa:
    ///   1 base + 1 extra cada 3 niveles, en abanico.
    /// · LO QUE YA ERA SUYO: los niveles infinitos comiendo XP REAL (la
    ///   rareza del bestiario mide el valor, jefes con fórmula fija), el
    ///   escalado (+2.2% daño mágico, −0.3% use time, +1 slot de minion
    ///   cada 10 niveles, lifesteal del 7 en 7…), el Orbe Cósmico en el
    ///   clic derecho y LOS TRES ESTADOS (sostenido/barra rápida/guardado).
    ///
    /// Sin maná (la regla de la casa — el hambre ES el coste).
    /// </summary>
    public class GrimoireEternal : GrimorioHambrientoNervioso
    {
        // === EL CUERPO DEL NERVIOSO (v6.50.102): mismo sprite que la
        //     familia del ojo — una sola fuente de verdad para el arte ===
        public override string Texture => "AethonMod/Content/Weapons/GrimorioHambrientoNervioso";

        // === EL RELOJ JUGABLE: 0→100% en UNA HORA (216.000 ticks — la
        //     letra de la .103: una hora si no se alimenta el libro).
        //     El Nervioso de prueba corre su minuto de demo (1/3600) — el
        //     Eterno es para PARTIDA DE VERDAD: el hambre vuelve a ser una
        //     presión, no un espectáculo. Hitos del reloj: 25% (EL CAZADOR)
        //     a los 15 min, 50% (LA IMPACIENCIA) a los 30 min, 100% a la
        //     hora exacta. ===
        private static readonly EstadoGrimorio _estadoEterno = new EstadoGrimorio
        {
            Nervioso = true,
            TasaHambre = 1f / 216000f,  // UNA HORA de 0 a 100% (la .102: 15 min — muy rápido)
        };
        protected override EstadoGrimorio Estado => _estadoEterno;
        internal static EstadoGrimorio EstadoEterno => _estadoEterno;

        // === LA IDENTIDAD EN EL REGISTRO DE LA FAMILIA: el espíritu y su
        //     sombra sirven al libro que LOS llamó (ai[2] = este tipo) ===
        protected override int TipoLibro => ModContent.ItemType<GrimoireEternal>();

        // === LA ESPERA JUGABLE: 10 s de quietud sacan al libro a cazar
        //     (la letra marcó los 5 s como valor de PRUEBA del Nervioso) ===
        protected override int TicksQuietudSalida => 600;

        // v5.10/v5.18: el anti-doble de la casa (mismo frame) — heredado
        // del TestStaff que funciona. Previene que Shoot se procese dos
        // veces en el mismo frame.
        private static uint _lastFireFrame = 0;
        private static uint _lastMinionFrame = 0;

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
        //  LOS PARÁMETROS DEL ETERNO (los de siempre — el Equinoccio
        //  hereda EL DAÑO y EL RITMO de aquí, no al revés)
        // ================================================================

        public override void SetDefaults()
        {
            Item.damage = 11;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;               // el cuerpo del Nervioso (textura 36×49)
            Item.height = 42;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.knockBack = 3f;
            Item.value = Item.buyPrice(0, 10, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8;
            Item.autoReuse = true;         // v5.1: mantener click = disparo continuo
            Item.shoot = 931;              // Nightglow vanilla (el número — como el Equinoccio)
            Item.shootSpeed = 12f;
            Item.mana = 0;                 // la regla de la casa — el hambre ES el coste
            Item.noMelee = true;
            Item.reuseDelay = 10;          // v5.16: cooldown del TestStaff que funciona
        }

        public override void AddRecipes()
        {
            // LA ADQUISICIÓN CANÓNICA DEL ETERNO (NO hereda la receta de
            // 5 maderas del Nervioso de prueba): la Bolsa de los
            // Fundacionales lo entrega en cada entrada al mundo y el
            // Génesis lo forja (GenesisShard) — el arma raíz del mod no
            // se clava con madera.
        }

        // ================================================================
        //  ESCALADO DE DAÑO (el sistema de niveles del Eterno, intacto)
        // ================================================================

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var sl = GetShard(Item);
            if (sl == null) return;

            // +2.2% daño mágico por nivel (Infinito) — local a ESTE arma.
            damage *= WeaponScaling.MagicDamageMult(sl.Level);

            // BONUS POR MANA FALTANTE — la mitad mágica es por-golpe (aquí);
            // la mitad de invocación vive en ShardPlayer (v6.45).
            damage *= WeaponScaling.LowManaDamageMult(player.statMana, player.statManaMax2);
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
            // la regla de la casa: SIN MANÁ — el nivel escala daño y
            // velocidad; el RECURSO de esta arma es el hambre.
            Item.mana = 0;
            reduce = 1f;
            mult = 0f;
        }

        public override float UseTimeMultiplier(Player player)
        {
            var sl = GetShard(Item);
            if (sl == null) return 1f;
            // v5.25: base = Item.useTime para que UseSpeedMult redondee el
            // useTime efectivo a entero (evita el doble Shoot).
            return WeaponScaling.UseSpeedMult(sl.Level, Item.useTime);
        }

        /// <summary>
        /// v5.26: reduce useAnimation proporcionalmente al useTime para que
        /// NUNCA sea menor (si lo fuera, tModLoader dispararía Shoot 2 veces
        /// por ciclo de animación).
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

            // Click derecho (minion): permite Mana Flower (fix 48688dd);
            // v6.45: en modo pruebas NO se exige maná (bandera).
            if (player.altFunctionUse == 2)
            {
                if (ManaMinionGratis()) return true;
                int minionCost = WeaponScaling.MinionManaCost(level);
                return player.statMana >= minionCost || player.manaFlower;
            }
            // Click izquierdo (el Equinoccio): siempre
            return true;
        }

        public override bool AltFunctionUse(Player player) => true;

        /// <summary>v6.50.102 — LA DIFERENCIA CON EL NERVIOSO DE PRUEBA: el
        /// clic derecho NO reinicia el apetito — invoca al Orbe Cósmico
        /// (ver Shoot). El Eterno se alimenta CAZANDO (sus presas), no de
        /// gratis: el botellón de la demo murió con el minuto.</summary>
        public override bool? UseItem(Player player) => null;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // === v5.18: LÓGICA ANTI-DOBLE IDÉNTICA AL TESTSTAFF (que funciona) ===
            uint currentFrame = Main.GameUpdateCount;

            var sl = GetShard(Item);
            int level = sl?.Level ?? 1;

            // === CLICK DERECHO: invocar minion (la firma del Eterno) ===
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
                        Main.NewText(Language.GetTextValue("Mods.AethonMod.Armas.SlotsMinionLlenos",
                            currentMinions, maxMinions),
                            new Color(255, 120, 120));
                    return false;
                }

                // v6.45: cobrar mana del minion SOLO con la bandera de coste
                // activa (ManaGratisEnPruebas = OFF).
                if (!ManaMinionGratis())
                {
                    int minionCost = WeaponScaling.MinionManaCost(level);
                    if (player.statMana < minionCost && !player.manaFlower) return false;
                    if (player.statMana >= minionCost)
                    {
                        player.statMana -= minionCost;
                        if (player.statMana < 0) player.statMana = 0;
                    }
                }

                // Invocar minion (EXACTAMENTE una vez) — el nivel viaja en
                // ai[2] desde el spawn (v6.50.2: el paquete 27 sí viaja).
                player.AddBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.CosmicOrbBuff>(), 18000);
                Projectile.NewProjectile(source, position, Vector2.Zero,
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.CosmicOrbMinion>(),
                    damage, knockback, player.whoAmI, 0f, 0f, level);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113);
                return false;
            }

            // === CLICK IZQUIERDO: EL EQUINOCCIO DEL ETERNO (v6.50.102) ===
            // Anti-doble simple (mismo frame check).
            if (currentFrame == _lastFireFrame)
            {
                return false;
            }
            _lastFireFrame = currentFrame;

            // el dueño está cazando ÉL: el reloj del ausente se reinicia
            // (la caza solitaria es del ausente — la letra del Nervioso)
            Estado.TicksSinAtaqueLibro = 0;

            // EL ORIGEN: si el espíritu está FUERA, el disparo nace DEL
            // LIBRO FLOTANTE (la regla del Nervioso: «si el jugador ataca
            // y el libro esta fuera sus ataques salen del libro»)
            Vector2 pos = OrigenDelDisparo(player, position);

            // === LA FUENTE DIVIDIDA (la del Equinoccio, EXACTA): la
            //     velocidad de vanilla (aleatoria, siempre hacia arriba) —
            //     el día y la noche separándose sobre tu cabeza ===
            Vector2 baseV = Main.rand.NextVector2Circular(1f, 1f)
                          + Main.rand.NextVector2CircularEdge(3f, 3f);
            if (baseV.Y > 0f) baseV.Y *= -1f;

            // LA PAREJA PRINCIPAL + LAS EXTRA POR NIVEL: cada «bolt» del
            // sistema del Eterno es UN DÍA Y UNA NOCHE — 1 pareja base y
            // una más cada 3 niveles, en abanico (el homing del 931 hace
            // el resto)
            int extra = WeaponScaling.ExtraProjectiles(level);

            LanzarPareja(source, pos, baseV, damage, knockback, player);

            for (int i = 0; i < extra; i++)
            {
                float angulo = (i + 1) * 0.12f * (i % 2 == 0 ? 1f : -1f);
                LanzarPareja(source, pos, baseV.RotatedBy(angulo), damage, knockback, player);
            }

            return false;   // todos los spawns son nuestros
        }

        /// <summary>EL DÍA Y LA NOCHE — la pareja del Equinoccio con los
        /// parámetros del Eterno (el daño ya viene escalado por nivel):
        /// EL DORADO con luz de sol y ocho rayos (ai[2]=9201) y EL NEGRO,
        /// disco de eclipse con rim violeta (ai[2]=9202 — sin bruma desde
        /// la .102, la letra del usuario).</summary>
        private static void LanzarPareja(EntitySource_ItemUse_WithAmmo source, Vector2 pos,
            Vector2 vel, int damage, float knockback, Player player)
        {
            // EL DORADO — EL DÍA: ai[1] = 0.64 → el render del 931 hace
            // hslToRgb((hue+0.5)%1) → matiz 0.14 = DORADO de verdad
            Projectile.NewProjectile(source, pos, vel.RotatedBy(-0.35f),
                931, damage, knockback, player.whoAmI,
                -1f,      // ai[0]: sin objetivo aún
                0.64f,    // ai[1]: EL MATIZ DORADO
                9201f);   // ai[2]: la marca del FX del sol

            // EL NEGRO — LA NOCHE: ai[1] = 0.28 → matiz 0.78 = VIOLETA
            // (el rim del eclipse que late en la oscuridad)
            Projectile.NewProjectile(source, pos, vel.RotatedBy(0.35f),
                931, damage, knockback, player.whoAmI,
                -1f,      // ai[0]: sin objetivo aún
                0.28f,    // ai[1]: el matiz violeta (el rim)
                9202f);   // ai[2]: la marca del FX del eclipse (SIN bruma)
        }

        // ================================================================
        //  TOOLTIP — LA HERENCIA VIVA: la barra de hambre y el estado del
        //  Nervioso (el Eterno ES el Nervioso jugable) + TODAS las
        //  secciones del sistema de niveles de siempre.
        // ================================================================

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // === LA HERENCIA DEL NERVIOSO: la barra de hambre viva + el
            //     estado (tranquilo/hambriento/impaciente/cazando) ===
            base.ModifyTooltips(tooltips);

            // rescatar la pareja del hambre (va tras el daño) y quitar el
            // nivel del Nervioso (la PROGRESIÓN del Eterno ya muestra
            // nivel+XP con su barra) + las vanilla "Level"
            TooltipLine barraHambre = null, estadoHambre = null;
            for (int i = tooltips.Count - 1; i >= 0; i--)
            {
                string n = tooltips[i].Name ?? "";
                string t = tooltips[i].Text ?? "";
                if (n == "HambreBarra") { barraHambre = tooltips[i]; tooltips.RemoveAt(i); }
                else if (n == "HambreEstado") { estadoHambre = tooltips[i]; tooltips.RemoveAt(i); }
                else if (n == "NivelNervioso" || n == "Level" || n == "ItemLevel" ||
                    t.StartsWith("Level:") || t.StartsWith("Level："))
                    tooltips.RemoveAt(i);
            }

            var sl = GetShard(Item);

            // === v5.14: en vista COMPLETA, ocultar líneas vanilla (básicas) ===
            if (sl != null && Globals.TooltipToggleItem.ShowExtendedTooltip)
            {
                for (int i = tooltips.Count - 1; i >= 0; i--)
                {
                    string name = tooltips[i].Name ?? "";
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

            // === LA PAREJA DEL HAMBRE: pegada al daño (básica) o al tope
            //     (completa) — el apetito es EL estado del arma ===
            int ancla = 0;
            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].Name == "Damage") { ancla = i + 1; break; }
            }
            if (estadoHambre != null)
                tooltips.Insert(System.Math.Min(ancla, tooltips.Count), estadoHambre);
            if (barraHambre != null)
                tooltips.Insert(System.Math.Min(ancla, tooltips.Count), barraHambre);

            if (sl == null) return;

            // === v5.11: SummonDamage SOLO en vista básica (no en completa) ===
            int summonDmg = (int)(Item.damage * (1f + sl.Level * WeaponScaling.SummonDamagePerLevel));

            // === v5.2: TOOLTIP CON 2 MODOS (básico / completo) ===

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

            if (Globals.TooltipToggleItem.ShowExtendedTooltip)
            {
                // === MODO COMPLETO: todas las estadísticas ===
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
                int minionCost = WeaponScaling.MinionManaCost(sl.Level);
                // v6.50.102 — cada «bolt» es una pareja día/noche
                int totalPares = 1 + WeaponScaling.ExtraProjectiles(sl.Level);
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
                tooltips.Add(new TooltipLine(Mod, "Knockback",
                    $"[c/FFAA55:+{kbBonus:F0}% retroceso]"));

                tooltips.Add(new TooltipLine(Mod, "SectionResources", "[c/55AAFF:═══ RECURSOS ═══]"));
                // v6.46: el tope +100 de la barra rápida anotado en la línea misma
                tooltips.Add(new TooltipLine(Mod, "ManaMax",
                    $"[c/55AAFF:+{bonusMana} mana máximo (tope +100 sin sostener)]"));
                tooltips.Add(new TooltipLine(Mod, "LifeMax",
                    $"[c/55AAFF:+{bonusLife} vida máxima (tope +100 sin sostener)]"));
                tooltips.Add(new TooltipLine(Mod, "ManaRegen",
                    $"[c/55AAFF:+{manaRegen} mana/seg regeneración]"));
                tooltips.Add(new TooltipLine(Mod, "LifeRegen",
                    $"[c/FF5566:+{lifeRegen:F1} vida/seg regeneración]"));
                tooltips.Add(new TooltipLine(Mod, "DamageRed",
                    $"[c/FFAA55:-{dmgRed:F0}% daño recibido]"));

                tooltips.Add(new TooltipLine(Mod, "SectionProjectile", "[c/FFD700:═══ PROYECTIL ═══]"));
                tooltips.Add(new TooltipLine(Mod, "Parejas",
                    $"[c/55AAFF:{totalPares} pareja(s) día/noche por disparo ({totalPares * 2} orbes)]"));
                tooltips.Add(new TooltipLine(Mod, "AreaDmg",
                    $"[c/55AAFF:+{areaDmg}px daño en área]"));
                tooltips.Add(new TooltipLine(Mod, "OrigenLibro",
                    "[c/BE78FD:Con el espíritu fuera, dispara el LIBRO]"));

                tooltips.Add(new TooltipLine(Mod, "SectionMinion", "[c/BE78FD:═══ ORBE CÓSMICO ═══]"));
                // v6.45/v6.46: los slots de minion — la escalera de cada 10
                // niveles (tope +10, se sostienen en la barra rápida).
                tooltips.Add(new TooltipLine(Mod, "MinionSlots",
                    $"[c/78FF96:+{bonusSlots} slot(s) de minion (máx +10)]"));
                tooltips.Add(new TooltipLine(Mod, "MinionContactDmg",
                    $"[c/BE78FD:+{contactDmg:F0}% daño de contacto]"));
                tooltips.Add(new TooltipLine(Mod, "MinionSpeed",
                    $"[c/BE78FD:+{minionSpd:F0}% velocidad]"));
                tooltips.Add(new TooltipLine(Mod, "MinionRange",
                    $"[c/BE78FD:{detectRange:F0}px rango de detección]"));
                tooltips.Add(new TooltipLine(Mod, "MinionCd",
                    $"[c/BE78FD:{hitCd} frames de cooldown]"));
                // v6.45: el costo de invocación solo se anuncia cuando está
                // ACTIVO (bandera de pruebas OFF).
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
                // === MODO BÁSICO: SummonDamage + PROGRESIÓN + Próximo hito ===
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
                    $"{summonDmg} daño de invocación"); // sin [c/...] = blanco default
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
