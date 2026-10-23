using System;
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
    ///   CADA BAJA LO ALIMENTA POR RAREZA (v6.50.104, la letra: «el
    ///   hambre deberia bajar usando la rareza de las criaturas como
    ///   fuente… cada criatura de 1 estrella solo quita el 0.1% de
    ///   hambre los jefes quitan 10%… y el hambre se reduce de 0.1% a
    ///   5% dependiendo de la rareza del monstruo»): 1★ quita 0,1% …
    ///   5★ quita 5% (lineal) y un jefe entero 10%. El clic derecho ya
    ///   NO reinicia el apetito — se alimenta MATANDO (y su sombra
    ///   devora presas enteras: mismo valor, mismo bocado).
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
    ///   cada 10 niveles, lifesteal del 7 en 7…), LOS OJOS DEL ETERNO en
    ///   el clic derecho (v6.50.105: los minions son los OJOS del libro —
    ///   la letra: «el minion que use el grimorio será su propio ojo»:
    ///   v6.51.7 EL ARCO DEL BÁCULO SANGUINARIO: el abanico quieto
    ///   detrás del dueño (como los murciélagos del Sanguine Staff) que
    ///   GIRA alrededor del libro flotante cuando sale a cazar, el rayo
    ///   Perlin NEGRO de bordes morados
    ///   con bruma en la punta, el círculo mágico de los agujeros
    ///   negros, el retroceso al disparar y el parpadeo independiente)
    ///   y LOS TRES ESTADOS (sostenido/barra rápida/guardado).
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

        // === v6.50.104 — LA DIETA POR RAREZA (la letra: «el hambre
        //     deberia bajar usando la rareza de las criaturas como
        //     fuente… y el hambre se reduce de 0.1% a 5% dependiendo de
        //     la rareza del monstruo»): la MISMA fuente que la XP — las
        //     ESTRELLAS del bestiario (ShardLevelSystem.EstrellasDe).
        //     Escalera LINEAL 1★→5★: 0,1% · 1,3% · 2,6% · 3,8% · 5%;
        //     el JEFE vale 10% entero. La come TODO lo que alimenta al
        //     libro: la baja acreditada (GlobalNPCXP.OnKill) y la presa
        //     que devora su sombra (AbsorberPresa). Con el reloj de la
        //     hora (.103): 5★ compra 3 min de apetito, un jefe 6. ===
        internal static float HambrePorPresa(NPC presa)
        {
            if (presa == null || !presa.active)
                return 0f;
            // v6.50.116 — EL SELLO VERDADERO: los jefes de oleada mueren
            //     AISLADOS de vanilla (tipo/netID en 0 — OleadaNPC.
            //     AislarJefeDeOleada) pero para el LIBRO siguen siendo
            //     jefes: el festín de la oleada alimenta al Eterno igual
            //     (la letra: los jefes de oleada «solo afectan al
            //     grimorio… y al hambre»).
            if (presa.boss || Globals.OleadaNPC.JefeVerdadero(presa))
                return 0.10f;                        // EL JEFE: 10% entero
            int estrellas = ShardLevelSystem.EstrellasDe(presa);
            if (estrellas < 1) estrellas = 1;        // la chusma más común: el piso de la 1★
            if (estrellas > 5) estrellas = 5;
            return 0.001f + (estrellas - 1) * (0.05f - 0.001f) / 4f;   // 0,1% → 5% lineal
        }

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
            // v6.51.1 — LA RAREZA CÓSMICA (la letra: «una rareza más que
            // legendaria, más que mítica, más que única, una rareza cósmica
            // además con la etiqueta de legendario supremo»).
            Item.rare = ModContent.RarityType
                <global::AethonMod.Content.Rarities.LegendarioSupremo>();
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
            // LA ADQUISICIÓN CANÓNICA DEL ETERNO (v6.50.117, LA CAÍDA):
            // una noche avanzada, un cometa de tinta negro-morado cae a
            // ~30 pantallas del spawn por cada jugador sin libro, excava
            // su cráter y deja al GRIMORIO DORMIDO flotando — el
            // destinatario extiende la mano y el libro habla por primera
            // vez (LaCaidaSistema + GrimorioDormido). El Eterno ya NO se
            // forja: nace. (La Bolsa de los Fundacionales sigue siendo la
            // vía de pruebas del kit; esta receta sigue vacía a propósito
            // — el arma raíz del mod no se clava con madera.)
        }

        // ================================================================
        //  ESCALADO DE DAÑO (el sistema de niveles del Eterno, intacto)
        // ================================================================

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            var sl = GetShard(Item);

            // +2.2% daño mágico por nivel (Infinito) — local a ESTE arma.
            if (sl != null)
                damage *= WeaponScaling.MagicDamageMult(sl.Level);

            // BONUS POR MANA FALTANTE — la mitad mágica es por-golpe (aquí);
            // la mitad de invocación vive en ShardPlayer (v6.45).
            damage *= WeaponScaling.LowManaDamageMult(player.statMana, player.statManaMax2);

            // v6.50.128 — LO DEVORADO EN EL ALTAR: cada esencia corrupta que
            // el Eterno tragó en el ALTAR DE LA CAÍDA es +0,25 % de daño
            // PERMANENTE (tope +75 % — la letra: «la mecanica es la de
            // comerse los item y mejorar el grimorio del eterno»).
            damage *= 1f + Players.ShardPlayer.BonusDanoDevorado(player);

            // v6.50.129 — EL FESTÍN DE LAS ARMAS (la letra: «cualquier arma
            // corrompida se vuelve exp para el grimorio… cada arma debe
            // subir algun stats del grimorio»): la Fuerza (armas melee
            // comidas) + el EXP acumulado (la rareza de todo lo devorado).
            damage *= 1f + Players.ShardPlayer.BonusDanoArmasDevoradas(player);

            // v6.51.0 — EL DESPERTAR (la letra: «consumir la esencia
            // corrupta sigue aumentando un poquito el daño en %… ese
            // daño aumentado es de TODOS los tipos de daño que tiene el
            // grimorio, o sea mágico y de invocación»): +1 % por nivel
            // de despertar.
            damage *= 1f + Systems.DespertarGrimorio.BonusDano(
                Systems.DespertarGrimorio.NivelDe(player));
        }

        public override void ModifyWeaponKnockback(Player player, ref StatModifier knockback)
        {
            var sl = GetShard(Item);
            if (sl != null)
            {
                // +10% knockback cada 10 niveles (tope +100%)
                knockback *= WeaponScaling.KnockbackMult(sl.Level);
            }
            // v6.50.129 — LA MORDIDA Y EL PUÑO TITÁN del festín de armas
            // (las lanzables y los pesos comidos: +empuje permanente).
            knockback *= 1f + Players.ShardPlayer.BonusEmpujeArmasDevoradas(player);
        }

        /// <summary>v6.50.129 — LA MIRADA DEL FESTÍN: el crítico de las
        /// armas mágicas comidas (+ el Corazón Estelar de las armas de
        /// estrellas) — la tercera mejora del festín de la bruma.</summary>
        public override void ModifyWeaponCrit(Player player, ref float crit)
        {
            crit += Players.ShardPlayer.BonusCritArmasDevoradas(player) * 100f;
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
            float factor = WeaponScaling.UseSpeedMult(sl.Level, Item.useTime);
            // v6.50.129 — LA PRISA Y EL ALMA VELOZ del festín de armas (las
            // armas a distancia comidas + las hojas rápidas): el uso se
            // ACELERA por lo devorado (tope conjunto +35 % — con el factor
            // del nivel, multiplicado: nunca por debajo del ciclo mínimo).
            factor *= 1f - Players.ShardPlayer.BonusVelocidadArmasDevoradas(player);
            return MathHelper.Clamp(factor, 0.35f, 1f);
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
            float factor = WeaponScaling.UseSpeedMult(sl.Level, Item.useAnimation);
            // v6.50.129 — la misma PRISA del festín, para que la animación
            // NUNCA quede por debajo del useTime (la regla anti-doble-Shoot).
            factor *= 1f - Players.ShardPlayer.BonusVelocidadArmasDevoradas(player);
            return MathHelper.Clamp(factor, 0.35f, 1f);
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
        /// clic derecho NO reinicia el apetito — invoca a LOS OJOS DEL
        /// ETERNO (ver Shoot). El Eterno se alimenta CAZANDO (sus presas),
        /// no de gratis: el botellón de la demo murió con el minuto.</summary>
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

                // v6.50.116 — EL ESCUADRÓN DE 11 Y EL FORTALECIMIENTO (la
                // letra: «en vez de 10 minion que sean 11, así se ve
                // mejor en la animación de cuando está el libro fuera»):
                // el tope son 11 OJOS DEL ETERNO (TechoOjosDe — PLANO,
                // ya sin equipo ni nivel; con 11 las órbitas del libro
                // cierran EXACTO: anillo interior de 5 + exterior de 6)
                // y SOLO se cuentan LOS OJOS (los minions vanilla no
                // ocupan el escuadrón del libro); con el escuadrón
                // LLENO, la invocación que sería la 12.ª NO invoca:
                // FORTALECE a un ojo vivo (+50% daño y un anillo por
                // mejora, 4 mejoras máximas — OjoEternoMinion).
                int maxMinions = Players.ShardPlayer.TechoOjosDe(player, level);
                int tipoOjo = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.OjoEternoMinion>();

                // v6.51.0 — LA ESCALERA DE LA LETRA: «en nivel 5 puede
                // invocar su PRIMER minion» — antes del nivel 5 el
                // escuadrón está cerrado (cada ojo nuevo se ANUNCIA al
                // subir de nivel — ShardLevelItem.OnLevelUp).
                int despertar = Systems.DespertarGrimorio.NivelDe(player);
                int mejorasDesbloqueadas = Systems.DespertarGrimorio.MejorasDesbloqueadas(despertar);
                if (maxMinions <= 0)
                {
                    if (Main.myPlayer == player.whoAmI)
                        Main.NewText(Terraria.Localization.Language.GetTextValue(
                            "Mods.AethonMod.Despertar.OjosCerrados", 5),
                            new Color(255, 130, 130));
                    return false;
                }
                int currentMinions = 0;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p != null && p.active && p.owner == player.whoAmI &&
                        p.type == tipoOjo)
                        currentMinions++;
                }

                if (currentMinions >= maxMinions)
                {
                    // === LA 12.ª INVOCACIÓN FORTALECE: se elige el ojo con
                    //     MENOS mejoras de la banda (el reparto parejo hace
                    // que TODO el escuadrón despierte sus anillos) ===
                    Projectile objetivo = null;
                    int mejorMenor = int.MaxValue;
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Projectile p = Main.projectile[i];
                        if (p == null || !p.active || p.owner != player.whoAmI ||
                            p.type != tipoOjo) continue;
                        int m = global::AethonMod.Content.Projectiles.OjoEternoMinion.MejorasDe(p);
                        if (m >= global::AethonMod.Content.Projectiles.OjoEternoMinion.MejorasMax)
                            continue;                       // ya lleva sus 4 mejoras
                        // v6.51.0 — LAS MEJORAS SON DEL DESPERTAR (la
                        // letra: «con la nueva mecánica las mejoras están
                        // sujetas a esa mecánica de corrupción» — nivel 5
                        // → la 1 · 15 → la 2 · 30 → la 3 · 40 → la 4).
                        if (m >= mejorasDesbloqueadas)
                            continue;                       // el despertar aún no las presta
                        if (m < mejorMenor) { mejorMenor = m; objetivo = p; }
                    }

                    if (objetivo == null)
                    {
                        // v6.51.0 — DOS CIERRES DISTINTOS: o todo el
                        // escuadrón llevó sus 4 mejoras (el final de
                        // verdad)… o el DESPERTAR aún no presta más.
                        if (Main.myPlayer == player.whoAmI)
                        {
                            if (mejorasDesbloqueadas >= global::AethonMod.Content.Projectiles.OjoEternoMinion.MejorasMax)
                                Main.NewText(Terraria.Localization.Language.GetTextValue(
                                    "Mods.AethonMod.Armas.OjosAlMaximo"),
                                    new Color(255, 120, 120));
                            else
                                Main.NewText(Terraria.Localization.Language.GetTextValue(
                                    "Mods.AethonMod.Despertar.MejorasSelladas", despertar),
                                    new Color(255, 130, 130));
                        }
                        return false;
                    }

                    // el coste del conjuro (fortalecer TAMBIÉN cuesta —
                    // es una invocación más)
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

                    global::AethonMod.Content.Projectiles.OjoEternoMinion.Fortalecer(objetivo);
                    if (Main.myPlayer == player.whoAmI)
                        Main.NewText(Language.GetTextValue("Mods.AethonMod.Armas.OjoFortalecido",
                            global::AethonMod.Content.Projectiles.OjoEternoMinion.MejorasDe(objetivo) * 50),
                            new Color(196, 138, 255));
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item113);
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

                // Invocar el OJO (EXACTAMENTE una vez) — el nivel viaja en
                // ai[2] desde el spawn (v6.50.2: el paquete 27 sí viaja).
                // v6.51.0 — EL DAÑO ES DEL GRIMORIO SOLAMENTE (la letra:
                // «sin importar la armadura o accesorios»): NO el damage
                // del disparo (que ya lleva las stats del jugador) sino
                // DanoOjosDelEterno — su base, su nivel, su despertar.
                int danoOjo = WeaponScaling.DanoOjosDelEterno(Item.damage, level, player);
                player.AddBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.OjoEternoBuff>(), 18000);
                Projectile.NewProjectile(source, position, Vector2.Zero,
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.OjoEternoMinion>(),
                    danoOjo, knockback, player.whoAmI, 0f, 0f, level);
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

        /// <summary>EL DÍA Y LA NOCHE — LOS DISCOS DEL EQUINOCCIO DE
        /// SIEMPRE. v6.51.3 — LA VUELTA A CASA (la letra: «ahora los
        /// proyectiles del grimorio son horribles, necesitamos crear un
        /// proyectil digno para el grimorio, de momento deja el que ya
        /// teniamos antes»): la fusión con el Tentáculo del Equinoccio de
        /// la v6.51.2 MUERE como disparo básico — vuelve EL PROYECTIL DE
        /// ANTES (el 931 del Equinoccio, los discos día/noche de la
        /// v6.51.1, con sus marcas 9201/9202 del FX). El tentáculo vive
        /// en OTROS rincones del Eterno; el disparo básico del libro
        /// vuelve a ser lo que era. (El proyectil DIGNO queda como el
        /// pendiente de diseño — las ideas van en el mensaje de la
        /// versión.)</summary>
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
        //  TOOLTIP — v6.50.108 — LA CAJA COMPACTA + LA SEGUNDA VENTANA.
        //
        //  La letra de la .107 (INTACTA): la caja básica es COMPACTA —
        //  daño mágico, daño de invocación, datos y buff propios del
        //  juego, el lore de 2 líneas y PROGRESIÓN (EXP + hambre). El
        //  DAÑO MÁGICO y los datos/buff los pone EL JUEGO (vanilla, sin
        //  tocarlos nunca); el LORE (2 líneas) vive en el hjson.
        //
        //  La letra de la .108: «ese texto [del estado del hambre] debe
        //  ir justo debajo del hambre, no encima» → la BARRA primero y
        //  el estado DEBAJO; «el grimorio necesita una segunda ventana
        //  de info para visualizar sus estadísticas» → la vista
        //  completa por el conmutador de siempre (clic der sobre el
        //  libro: TooltipToggleItem); «en progresión quitaste las
        //  estadísticas del próximo nivel, vuelve a ponerlas» → el
        //  PRÓXIMO HITO y su escalera de mejoras, dentro de PROGRESIÓN.
        //
        //  La letra de la .110: «la segunda ventana de info del grimorio
        //  tiene demasiada informacion, crea una nueva ventana de
        //  informacion para repartir mejor la informacion» → la vista
        //  completa SE PARTE EN DOS y el clic derecho cicla TRES ventanas
        //  (TooltipToggleItem.VistaEstadisticas):
        //    · 1 EL ARMA   — DAÑO · RECURSOS · PROYECTIL · BONUS.
        //    · 2 EL FESTÍN — LOS OJOS DEL ETERNO · PROGRESIÓN · LA
        //      PRIMERA CENA (el nuevo festín del primer jefe).
        //  Y el análisis de la .110 («analisa si las estadisticas del
        //  grimorio eterno aplican realmente») dejó las etiquetas
        //  HONESTAS: el crítico y la penetración son MÁGICOS y exigen
        //  SOSTENER el libro (la nota al pie de EL ARMA lo dice);
        //  los ojos leen el nivel con el que fueron INVOCADOS (la nota
        //  al pie de EL FESTÍN también).
        // ================================================================

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // === v6.51.3 — LA ETIQUETA SE MUDÓ AL BESTIARIO (la letra:
            //     «creo que la etiqueta ✦ LEGENDARIO SUPREMO ✦ deberia
            //     estar mejor en el bestiario en la info del grimorio, por
            //     lo tanto quita ✦ LEGENDARIO SUPREMO ✦ de la ventana de
            //     info del grimorio»): MUERE la primera línea del tooltip —
            //     la rareza suprema ahora vive en la FICHA DEL GRIMORIO
            //     DEL BESTIARIO (EtiquetaSupremaInfoElement: el rótulo
            //     NEGRO con los destellos de 4 puntas de los soles rúnicos
            //     de 3 anillos, justo encima de las diez estrellas negras).
            //     El COLOR del nombre del arma sigue respirando violeta
            //     (LegendarioSupremo — la rareza de siempre). ===

            // === LA HERENCIA DEL NERVIOSO: la barra de hambre viva + el
            //     estado — rescatados y REUBICADOS al final, dentro de
            //     PROGRESIÓN (la letra: el hambre sale en la progresión,
            //     junto a la de exp) ===
            base.ModifyTooltips(tooltips);

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
            // v6.50.110 — LAS TRES VENTANAS: 0 básica · 1 EL ARMA · 2 EL FESTÍN
            int vista = Globals.TooltipToggleItem.VistaEstadisticas;
            bool completa = sl != null && vista != 0;
            bool ventanaArma = sl != null && vista == 1;
            bool ventanaFestin = sl != null && vista == 2;

            // === v6.50.108 — LA SEGUNDA VENTANA: en vista COMPLETA se
            //     ocultan las líneas vanilla básicas (el hueco lo llenan
            //     las secciones de estadísticas) ===
            if (completa)
            {
                for (int i = tooltips.Count - 1; i >= 0; i--)
                {
                    string name = tooltips[i].Name ?? "";
                    if (name == "Damage" || name == "CritChance" || name == "Speed" ||
                        name == "Knockback" || name == "UseMana" || name == "ManaCost" ||
                        name == "BestiaryNotes" || name == "ItemLevel" ||
                        name.StartsWith("Tooltip") ||     // Tooltip0, Tooltip1… (el lore vive en la básica)
                        name.StartsWith("Prefix"))
                    {
                        tooltips.RemoveAt(i);
                    }
                }
            }

            if (sl == null) return;

            // === EL DAÑO DE INVOCACIÓN — pegado al daño mágico del juego
            //     (la línea vanilla "Damage" ya dice «XX daño mágico»).
            //     En la COMPLETA no: allí va por la sección DAÑO ===
            if (!completa)
            {
                int summonDmg = (int)(Item.damage *
                    (1f + sl.Level * WeaponScaling.SummonDamagePerLevel));
                int ancla = -1;
                for (int i = 0; i < tooltips.Count; i++)
                {
                    if (tooltips[i].Name == "Damage") { ancla = i + 1; break; }
                }
                var summonLine = new TooltipLine(Mod, "SummonDamage",
                    $"[c/BE78FD:{summonDmg} daño de invocación]");
                if (ancla >= 0 && ancla < tooltips.Count)
                    tooltips.Insert(ancla, summonLine);
                else
                    tooltips.Add(summonLine);
            }

            // === v6.50.128 — LO DEVORADO EN EL ALTAR (la mejora permanente
            //     de la mecánica nueva: esencias comidas → +daño, +ojos).
            //     Solo si el portador ya comió ALGO (el libro nuevo calla). ===
            Player portadorTip = Main.LocalPlayer;
            // v6.51.6 — LO DEVORADO ES DE ESTE LIBRO (la letra: «las
            // estadísticas… son caracteristicas exclusivas del grimorio
            // del eterno»): la ventana lee el ALMA DEL ITEM señalizado —
            // cada Eterno cuenta SU propio festín.
            var spDevorado = Item != null && !Item.IsAir
                ? Item.GetGlobalItem<Globals.AlmaEternoGlobalItem>()
                : null;
            // v6.51.1 — LO DEVORADO solo en SU ventana (la letra: la
            // tercera ventana es SOLO estadísticas del escuadrón y su
            // festín — nada de progresión ni descripciones).
            if (ventanaFestin && spDevorado != null && spDevorado.DevoracionPoder > 0)
            {
                // El porcentaje como TEXTO a 2 decimales: 1 esencia = +0,25
                // (un entero lo escondería — la misma cura del aviso).
                string pctDanoTxt =
                    (Globals.AlmaEternoGlobalItem.BonusDanoDevorado(Item) * 100f).ToString("0.##");
                int ojosExtra = Globals.AlmaEternoGlobalItem.OjosExtraDevorados(Item);
                string ojos = ojosExtra > 0
                    ? Language.GetTextValue("Mods.AethonMod.Caida.DevoradoTooltipOjos", ojosExtra)
                    : "";
                tooltips.Add(new TooltipLine(Mod, "DevoradoAltar",
                    Language.GetTextValue("Mods.AethonMod.Caida.DevoradoTooltip",
                        spDevorado.DevoracionPoder, pctDanoTxt, ojos)));
            }

            // === v6.50.129 — EL FESTÍN DE LAS ARMAS (la letra: «cualquier
            //     arma corrompida se vuelve exp para el grimorio a mayor la
            //     rareza del arma mayor los exp. ademas cada arma debe
            //     subir algun stats del grimorio»): el contador del EXP +
            //     los stats por clase + las almas únicas despiertas —
            //     solo si el libro ya probó un arma (el nuevo calla). ===
            if (ventanaFestin && spDevorado != null && (spDevorado.DevoracionExp > 0 ||
                spDevorado.DevoracionFuerza > 0 || spDevorado.DevoracionPrisa > 0 ||
                spDevorado.DevoracionMirada > 0 || spDevorado.DevoracionCorona > 0 ||
                spDevorado.DevoracionMordida > 0 || spDevorado.AlmaIgnea > 0 ||
                spDevorado.AlmaSalvaje > 0 || spDevorado.AlmaVampira > 0 ||
                spDevorado.CorazonEstelar > 0 || spDevorado.AlmaVeloz > 0 ||
                spDevorado.PunoTitan > 0))
            {
                string pctArmas = (Globals.AlmaEternoGlobalItem.BonusDanoArmasDevoradas(Item) * 100f)
                    .ToString("0.##");
                tooltips.Add(new TooltipLine(Mod, "FestinArmas",
                    $"[c/B26EEC:El festín de las armas:] " +
                    $"[c/E8D8FF:{spDevorado.DevoracionExp} EXP] " +
                    $"[c/B26EEC:(+{pctArmas}% daño)]"));

                // LOS STATS POR CLASE (solo los que existen).
                var partes = new System.Collections.Generic.List<string>();
                if (spDevorado.DevoracionFuerza > 0)
                    partes.Add($"[c/E8D8FF:Fuerza +{(spDevorado.DevoracionFuerza * 0.35f):0.##}%]");
                if (spDevorado.DevoracionPrisa > 0 || spDevorado.AlmaVeloz > 0)
                    partes.Add($"[c/E8D8FF:Prisa +{(Globals.AlmaEternoGlobalItem.BonusVelocidadArmasDevoradas(Item) * 100f):0.##}%]");
                if (spDevorado.DevoracionMirada > 0 || spDevorado.CorazonEstelar > 0)
                    partes.Add($"[c/E8D8FF:Mirada +{(Globals.AlmaEternoGlobalItem.BonusCritArmasDevoradas(Item) * 100f):0.##}%]");
                int ojosCorona = Globals.AlmaEternoGlobalItem.OjosCoronaDevorados(Item);
                if (ojosCorona > 0)
                    partes.Add($"[c/E8D8FF:Corona +{ojosCorona} ojos]");
                if (spDevorado.DevoracionMordida > 0 || spDevorado.PunoTitan > 0)
                    partes.Add($"[c/E8D8FF:Mordida +{(Globals.AlmaEternoGlobalItem.BonusEmpujeArmasDevoradas(Item) * 100f):0.##}%]");
                if (partes.Count > 0)
                    tooltips.Add(new TooltipLine(Mod, "FestinArmasStats",
                        string.Join("[c/9B6EC8: · ]", partes)));

                // LAS ALMAS ÚNICAS DESPIERTAS (las armas especiales).
                var almas = new System.Collections.Generic.List<string>();
                if (spDevorado.AlmaIgnea > 0)
                    almas.Add($"[c/FF9A4D:Alma ígnea ×{spDevorado.AlmaIgnea}]");
                if (spDevorado.AlmaSalvaje > 0)
                    almas.Add($"[c/A8E063:Alma salvaje ×{spDevorado.AlmaSalvaje}]");
                if (spDevorado.AlmaVampira > 0)
                    almas.Add($"[c/E8638D:Alma vampírica ×{spDevorado.AlmaVampira}]");
                if (spDevorado.CorazonEstelar > 0)
                    almas.Add($"[c/F7D774:Corazón estelar ×{spDevorado.CorazonEstelar}]");
                if (spDevorado.AlmaVeloz > 0)
                    almas.Add($"[c/9BD4FF:Alma veloz ×{spDevorado.AlmaVeloz}]");
                if (spDevorado.PunoTitan > 0)
                    almas.Add($"[c/D9A066:Puño titán ×{spDevorado.PunoTitan}]");
                if (almas.Count > 0)
                    tooltips.Add(new TooltipLine(Mod, "FestinAlmas",
                        string.Join(" ", almas)));
            }

            // === v6.50.110 — LA VENTANA 1 · EL ARMA: el poder del libro
            //     (DAÑO / RECURSOS / PROYECTIL / BONUS). El repertorio de
            //     la .106/.107 con las etiquetas HONESTAS del análisis:
            //     el crítico y la penetración son MÁGICOS y viven
            //     SOSTENIENDO el libro (la nota al pie lo aclara) ===
            if (ventanaArma)
            {
                int magicDmgPct = (int)(sl.Level * WeaponScaling.MagicDamagePerLevel * 100);
                int summonDmgPct = (int)(sl.Level * WeaponScaling.SummonDamagePerLevel * 100);
                float critPct = WeaponScaling.CritBonus(sl.Level);
                float armorPenPct = WeaponScaling.ArmorPenBonus(sl.Level);
                int bonusMana = WeaponScaling.BonusMana(sl.Level);
                int bonusLife = WeaponScaling.BonusLife(sl.Level);
                int manaRegen = WeaponScaling.ManaRegen(sl.Level);
                float lifeRegen = WeaponScaling.LifeRegen(sl.Level);
                float dmgRed = WeaponScaling.DamageReduction(sl.Level) * 100f;
                float kbBonus = (WeaponScaling.KnockbackMult(sl.Level) - 1f) * 100f;
                int totalPares = 1 + WeaponScaling.ExtraProjectiles(sl.Level);
                int areaDmg = WeaponScaling.BoltAreaDamage(sl.Level);
                float lowManaBonus = (WeaponScaling.LowManaDamageMult(Main.LocalPlayer.statMana, Main.LocalPlayer.statManaMax2) - 1f) * 100f;

                tooltips.Add(new TooltipLine(Mod, "SectionDamage", "[c/FFD700:═══ DAÑO ═══]"));
                tooltips.Add(new TooltipLine(Mod, "MagicDamage",
                    $"[c/FF5555:+{magicDmgPct}% daño mágico (este libro)]"));
                tooltips.Add(new TooltipLine(Mod, "SummonDamageScale",
                    $"[c/BE78FD:+{summonDmgPct}% daño de invocación]"));
                tooltips.Add(new TooltipLine(Mod, "Crit",
                    $"[c/FFAA55:+{critPct:F1}% probabilidad de crítico mágico]"));
                tooltips.Add(new TooltipLine(Mod, "ArmorPen",
                    $"[c/55FFFF:+{armorPenPct:F0}% penetración de armadura mágica]"));
                tooltips.Add(new TooltipLine(Mod, "Knockback",
                    $"[c/FFAA55:+{kbBonus:F0}% retroceso]"));

                tooltips.Add(new TooltipLine(Mod, "SectionResources", "[c/55AAFF:═══ RECURSOS ═══]"));
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

                // LA NOTA AL PIE DEL ANÁLISIS (la .110): el poder de
                // combate del libro tiene PRECIO DE POSTURA — sostenerlo
                tooltips.Add(new TooltipLine(Mod, "NotaPostura",
                    "[c/78788C:Sostenido: crítico · penetración · regeneración · reducción]"));
                tooltips.Add(new TooltipLine(Mod, "NotaPostura2",
                    "[c/78788C:Barra rápida: vida/maná (tope +100) · slots de minion]"));
            }

            // === v6.50.110 — LA VENTANA 2 · EL FESTÍN: LOS OJOS DEL
            //     ETERNO + LA PRIMERA CENA (los sirvientes del libro y
            //     su festín del primer jefe) ===
            if (ventanaFestin)
            {
                int minionCost = WeaponScaling.MinionManaCost(sl.Level);
                float contactDmg = (WeaponScaling.MinionContactDamageMult(sl.Level) - 1f) * 100f;
                float minionSpd = (WeaponScaling.MinionSpeedMult(sl.Level) - 1f) * 100f;
                float detectRange = WeaponScaling.MinionDetectionRange(sl.Level);
                float ritmoOjos = (1f + 0.5f * System.Math.Min(sl.Level - 1, 99) / 99f - 1f) * 100f;
                int cadenaOjos = WeaponScaling.MinionChainCount(sl.Level);

                // v6.51.1 — LOS NÚMEROS REALES DEL PORTADOR (la letra:
                // «solo estadísticas de los minions… además de las
                // estadísticas que ya están»).
                // v6.51.6 — el despertar de ESTE libro (el item señalado).
                int despertar = Systems.DespertarGrimorio.NivelDe(Item);
                int techoOjos = Players.ShardPlayer.TechoOjosDe(portadorTip, sl.Level);
                int ojosVivos = 0;
                int tipoOjoTip = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.OjoEternoMinion>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile pr = Main.projectile[i];
                    if (pr != null && pr.active && pr.type == tipoOjoTip &&
                        pr.owner == portadorTip.whoAmI) ojosVivos++;
                }
                int mejorasDesbloqueadas = Systems.DespertarGrimorio.MejorasDesbloqueadas(despertar);
                float multMejoras = 1f + 0.5f * mejorasDesbloqueadas;

                tooltips.Add(new TooltipLine(Mod, "SectionMinion", "[c/BE78FD:═══ EL ESCUADRÓN ═══]"));
                tooltips.Add(new TooltipLine(Mod, "MinionSlots",
                    $"[c/E8D8FF:Ojos: {ojosVivos}/{techoOjos}]  " +
                    $"[c/78788C:(+1 ojo cada 10 niveles del grimorio)]"));
                tooltips.Add(new TooltipLine(Mod, "MinionMejoras",
                    $"[c/E8D8FF:Mejoras desbloqueadas: {mejorasDesbloqueadas}/4]  " +
                    $"[c/BE78FD:(+50% daño por mejora · ×{multMejoras:0.##} de daño con las mejoras — +200% con las 4)]"));
                tooltips.Add(new TooltipLine(Mod, "MinionContactDmg",
                    $"[c/BE78FD:+{contactDmg:F0}% daño del rayo]"));
                tooltips.Add(new TooltipLine(Mod, "MinionRitmo",
                    $"[c/BE78FD:+{ritmoOjos:F0}% velocidad de ataque (tope +50% en nivel 100)]"));
                tooltips.Add(new TooltipLine(Mod, "MinionCadena",
                    $"[c/BE78FD:Rayo en cadena: {cadenaOjos} enemigos (base 2, +1 cada 20 niveles)]"));
                tooltips.Add(new TooltipLine(Mod, "MinionSpeed",
                    $"[c/BE78FD:+{minionSpd:F0}% velocidad]"));
                tooltips.Add(new TooltipLine(Mod, "MinionRange",
                    $"[c/BE78FD:{detectRange:F0}px rango de detección]"));
                if (!ManaMinionGratis())
                {
                    tooltips.Add(new TooltipLine(Mod, "ManaMinion",
                        $"[c/55AAFF:Costo: {minionCost} mana por invocación]"));
                }

                // === LOS ATAQUES COMBINADOS (la letra: «si son capaces de
                //     lanzar soles runicos y agujeros negros runicos,
                //     cuanto pueden lanzar y eso») ===
                tooltips.Add(new TooltipLine(Mod, "SectionCombinados",
                    "[c/C49CFF:═══ ATAQUES COMBINADOS ═══]"));
                if (Systems.DespertarGrimorio.SolDisponible(despertar))
                {
                    tooltips.Add(new TooltipLine(Mod, "WSol",
                        $"[c/E8D8FF:Sol Rúnico: {Systems.DespertarGrimorio.AnillosDelSol(despertar)} anillo(s) · " +
                        $"{Systems.DespertarGrimorio.SolesPorMinuto(despertar)}/min]  " +
                        "[c/78788C:(2+ ojos mejorados, al azar)]"));
                }
                else
                {
                    tooltips.Add(new TooltipLine(Mod, "WSol",
                        "[c/78788C:Sol Rúnico: bloqueado — Despertar 7]"));
                }
                if (Systems.DespertarGrimorio.AgujerosDisponibles(despertar))
                {
                    string asc = Systems.DespertarGrimorio.AgujerosAscendidos(despertar)
                        ? " · ASCENDIDOS" : "";
                    tooltips.Add(new TooltipLine(Mod, "WAgujeros",
                        $"[c/E8D8FF:Agujeros Negros Rúnicos: 4 al azar{asc} · " +
                        $"{Systems.DespertarGrimorio.AgujerosPorMinuto(despertar)}/min]  " +
                        "[c/78788C:(5+ ojos mejorados)]"));
                }
                else
                {
                    tooltips.Add(new TooltipLine(Mod, "WAgujeros",
                        "[c/78788C:Agujeros Negros Rúnicos: bloqueado — Despertar 10]"));
                }
                if (Systems.DespertarGrimorio.SupremoDisponible(despertar))
                {
                    tooltips.Add(new TooltipLine(Mod, "WSupremo",
                        "[c/E8D8FF:Supremo / Supremo Aurora: 1/min]  " +
                        "[c/78788C:(8+ ojos con la mejora 4)]"));
                }
                else
                {
                    tooltips.Add(new TooltipLine(Mod, "WSupremo",
                        "[c/78788C:Supremo / Supremo Aurora: bloqueado — Despertar 45]"));
                }
                if (despertar >= Systems.DespertarGrimorio.NivelMax)
                {
                    tooltips.Add(new TooltipLine(Mod, "WMaximo",
                        "[c/C49CFF:Despertar 50: TODO el ataque del escuadrón ×1,25 y más rápido]"));
                }
            }

            // === LA PROGRESIÓN: la barra de EXP y la pareja del HAMBRE.
            //     v6.51.1 — SOLO EN LA VENTANA BÁSICA (la letra: la
            //     tercera ventana es solo estadísticas — la progresión no
            //     debe estar ahí).
            //     v6.50.108 — EL ORDEN DE LA LETRA: la BARRA de hambre
            //     primero y el estado JUSTO DEBAJO. ===
            if (vista == 0)
            {
                int xpNeeded = sl.XPForNextLevel();
                float pct = xpNeeded > 0 ? (float)sl.XP / xpNeeded : 0f;
                pct = System.Math.Clamp(pct, 0f, 1f);
                int barLen = 20;
                int filled = (int)(barLen * pct);
                string bar = "";
                for (int i = 0; i < barLen; i++)
                    bar += i < filled ? "█" : "░";

                tooltips.Add(new TooltipLine(Mod, "SectionProgress",
                    "[c/78FF96:═══ PROGRESIÓN ═══]"));
                tooltips.Add(new TooltipLine(Mod, "Level",
                    $"[c/FFD700:Nivel {sl.Level}]  [c/B388FF:{bar} {sl.XP}/{xpNeeded} XP]"));

                // === v6.51.0 — LA BARRA DEL DESPERTAR (la letra: «estos
                //     niveles deben estar ubicados en la ventana de
                //     información DEBAJO de la barra de niveles y ENCIMA
                //     de la barra de hambre») + la LISTA PEQUEÑA de lo
                //     que necesita el próximo nivel («para subirla de
                //     nivel se presentará una lista pequeña de lo que
                //     necesita»). ===
                try
                {
                    Player portador = Main.LocalPlayer;
                    // v6.51.6 — LA ESCALERA ES DE ESTE LIBRO (la letra: el
                    // despertar es individual de cada grimorio): se lee el
                    // ALMA DEL ITEM señalizado — dos Eternos en la misma
                    // bolsa muestran DOS escaleras distintas.
                    int despertar = Systems.DespertarGrimorio.NivelDe(Item);
                    int siguiente = Systems.DespertarGrimorio.NivelDespertarSiguiente(Item);

                    // LA BARRA (el nivel de despertar es la propia barra:
                    // 50 celdas, el nivel las llena).
                    int celdas = 25;
                    int llenas = (int)(celdas * despertar / (float)Systems.DespertarGrimorio.NivelMax);
                    string barDesp = "";
                    for (int i = 0; i < celdas; i++)
                        barDesp += i < llenas ? "▓" : "·";

                    tooltips.Add(new TooltipLine(Mod, "DespertarBarra",
                        $"[c/C49CFF:Despertar {despertar}/50]  [c/9B6FE8:{barDesp}]"));
                    tooltips.Add(new TooltipLine(Mod, "DespertarDano",
                        $"[c/9B6FE8:  +{(despertar)}% de daño a TODO el grimorio]"));

                    // LA LISTA PEQUEÑA DEL PRÓXIMO NIVEL (esencias ·
                    // dinero · el arma del peldaño) — v6.51.5 LA LISTA
                    // VIVA (la letra: «los requisitos que aparecen en el
                    // despertar para subir su nivel, deberia mostrar
                    // cuando cumples uno de ellos, ademas deberia
                    // mostrar el sprite del arma muy pequeño para saber
                    // que arma se necesita»): cada requisito se pinta
                    // VERDE al cumplirlo con su conteo vivo (3/5) y su
                    // ✓ dorado latiendo (DespertarRequisitosGlobalItem),
                    // y el arma del peldaño enseña sus sprites mini (las
                    // corruptas que ya llevas ARDEN con halo dorado).
                    if (despertar < Systems.DespertarGrimorio.NivelMax)
                    {
                        int esencias = Systems.DespertarGrimorio.EsenciasPara(siguiente);
                        long monedas = Systems.DespertarGrimorio.MonedasPara(siguiente);

                        // === v6.51.6 — LA LISTA DEL DESPERTAR, RE-DISEÑADA
                        //     (la letra: «lo que debe decir es el nombre
                        //     del arma y justo al lado un pequeño sprite de
                        //     como es el arma» + el check que se veía como
                        //     texto crudo [c/FFD678:✓] — el tag ANIDADO
                        //     dentro de otro tag muere: tML lo imprime
                        //     literal. AHORA: UN SOLO COLOR POR LÍNEA, el ✓
                        //     DENTRO del mismo span, y el arma con SU
                        //     NOMBRE + SU SPRITE INLINE [i:id] — el tag de
                        //     item NATIVO de tML, a prueba de todo). ===
                        string colOK = "7FE89B", colNo = "C4A2FF";

                        // === EL REQUISITO 1 — LAS ESENCIAS (conteo vivo). ===
                        int llevasEsencias = Systems.DespertarGrimorio.EsenciasEnManos(portador);
                        bool esenciasOK = llevasEsencias >= esencias;
                        tooltips.Add(new TooltipLine(Mod, "DespertarReq1",
                            $"[c/78788C:  Para Despertar {siguiente}:]"));
                        tooltips.Add(new TooltipLine(Mod, "DespertarReq2",
                            $"[c/{(esenciasOK ? colOK : colNo)}:  • Esencias Corruptas: {Math.Min(llevasEsencias, esencias)}/{esencias}{(esenciasOK ? "  ✓" : "")}]"));

                        // === EL REQUISITO 2 — EL DINERO (lo que llevas). ===
                        long llevasMonedas = Systems.DespertarGrimorioSistema.ContarMonedas(portador);
                        bool monedasOK = llevasMonedas >= monedas;
                        string textoMonedas = Systems.DespertarGrimorioSistema.ItemToText(monedas);
                        if (!monedasOK && llevasMonedas > 0L)
                            textoMonedas += $" — llevas {Systems.DespertarGrimorioSistema.ItemToText(llevasMonedas)}";
                        tooltips.Add(new TooltipLine(Mod, "DespertarReq3",
                            $"[c/{(monedasOK ? colOK : colNo)}:  • {textoMonedas}{(monedasOK ? "  ✓" : "")}]"));

                        // === EL REQUISITO 3 — EL ARMA DEL PELDAÑO (o
                        //     v6.51.8: LA OFRENDA MINERAL): SU NOMBRE +
                        //     SU SPRITE al lado (cada alternativa de este
                        //     peldaño, [i:id] = el icono del item
                        //     RENDERIZADO POR EL PARSER DE tML — se puede
                        //     hasta pasar el cursor por encima). ===
                        if (Systems.DespertarGrimorio.EsNivelMineral(siguiente))
                        {
                            // === v6.51.8 — LA OFRENDA MINERAL (la letra:
                            //     «en caso de no haber mas arma usar cierta
                            //     cantidad de lingotes de algun mineral… al
                            //     ser minerales no necesitan estar
                            //     corruptos»): el lingote del peldaño con
                            //     SU conteo vivo — tags SEPARADOS, nada de
                            //     anidados (la lección de la .51.6). ===
                            int tipoMin = Systems.DespertarGrimorio.MineralDe(siguiente);
                            int cantMin = Systems.DespertarGrimorio.CantidadMineralDe(siguiente);
                            int llevasMin = Systems.DespertarGrimorio.MineralesEnManos(portador, siguiente);
                            bool mineralOK = llevasMin >= cantMin;
                            string colMin = mineralOK ? colOK : colNo;
                            string nombreMin = "—";
                            try
                            {
                                var protoMin = Terraria.ID.ContentSamples.ItemsByType[tipoMin];
                                if (protoMin != null) nombreMin = protoMin.Name;
                            }
                            catch { }
                            var trozosMin = new System.Collections.Generic.List<string>();
                            trozosMin.Add($"[c/{colMin}:  • Ofrenda mineral:]");
                            trozosMin.Add($"[i:{tipoMin}]");
                            trozosMin.Add($"[c/{colMin}:{Math.Min(llevasMin, cantMin)}/{cantMin} × {nombreMin}{(mineralOK ? "  ✓" : "")}]");
                            tooltips.Add(new TooltipLine(Mod, "DespertarReq4",
                                string.Join(" ", trozosMin)));
                            tooltips.Add(new TooltipLine(Mod, "DespertarReq5",
                                "[c/78788C:  (suéltalos con el grimorio en la mano — sin corrupción)]"));
                        }
                        else
                        {
                        bool armaOK = false;
                        int[] peldano = Systems.DespertarGrimorio.ArmasDelNivel[siguiente];
                        for (int pa = 0; pa < peldano.Length; pa++)
                            if (Systems.DespertarGrimorio.LlevaCorrupta(portador, peldano[pa]))
                            { armaOK = true; break; }
                        string colArma = armaOK ? colOK : colNo;
                        var trozosArma = new System.Collections.Generic.List<string>();
                        trozosArma.Add($"[c/{colArma}:  • Devorar 1 CORRUPTA:]");
                        for (int pa = 0; pa < peldano.Length && pa < 6; pa++)
                        {
                            if (pa > 0) trozosArma.Add($"[c/78788C:·]");
                            trozosArma.Add($"[i:{peldano[pa]}]");
                            string nombreArma = "—";
                            try
                            {
                                var proto = Terraria.ID.ContentSamples.ItemsByType[peldano[pa]];
                                if (proto != null) nombreArma = proto.Name;
                            }
                            catch { }
                            trozosArma.Add($"[c/{colArma}:{nombreArma}]");
                        }
                        if (armaOK) trozosArma.Add($"[c/{colArma}: ✓]");
                        tooltips.Add(new TooltipLine(Mod, "DespertarReq4",
                            string.Join(" ", trozosArma)));
                        tooltips.Add(new TooltipLine(Mod, "DespertarReq5",
                            "[c/78788C:  (suéltala con el grimorio en la mano)]"));
                        }
                    }
                    else
                    {
                        tooltips.Add(new TooltipLine(Mod, "DespertarMax",
                            "[c/C49CFF:  EL GRIMORIO DESPERTÓ DEL TODO (máximo por ahora)]"));
                    }

                    // === v6.51.8 — EL CAMINO DEL DESPERTAR (la letra:
                    //     «en cada nivel del despertar solo dice % de
                    //     aumento de daño, pero no dicen nada mas,
                    //     deberian decir cuando es que aparecen las
                    //     mejoras en sus respectivos niveles y no solo
                    //     aumento de daño»): la escalera de hitos QUE
                    //     FALTA — cada mejora con SU nivel (la escalera
                    //     se va acortando a medida que el libro sube; en
                    //     el 50 ya no queda nada — solo el máximo).
                    bool caminoPintado = false;
                    foreach (var paso in Systems.DespertarGrimorio.ElCaminoDelDespertar)
                    {
                        if (paso.Nivel <= despertar) continue;
                        if (!caminoPintado)
                        {
                            tooltips.Add(new TooltipLine(Mod, "DespertarCamino",
                                "[c/78788C:  El camino del despertar:]"));
                            caminoPintado = true;
                        }
                        tooltips.Add(new TooltipLine(Mod, "DespertarCamino_" + paso.Nivel,
                            $"[c/9B6FE8:  • Nivel {paso.Nivel}:] [c/C4A2FF:{Language.GetTextValue(paso.Clave)}]"));
                    }
                }
                catch { }

                if (barraHambre != null) tooltips.Add(barraHambre);
                if (estadoHambre != null) tooltips.Add(estadoHambre);

                int nextMilestoneLevel = ((sl.Level / 5) + 1) * 5;
                int nextMilestoneNum = nextMilestoneLevel / 5;
                var nextRewards = WeaponScaling.MilestoneRewards(nextMilestoneNum);
                tooltips.Add(new TooltipLine(Mod, "NextMilestone",
                    $"[c/78788C:Próximo hito: Nivel {nextMilestoneLevel}]"));
                foreach (var reward in nextRewards)
                {
                    tooltips.Add(new TooltipLine(Mod, "MS_" + reward.GetHashCode(),
                        $"[c/78FF96:  • {reward}]"));
                }
            }

            // === EL CONMUTADOR (.110): el clic derecho cicla las TRES
            //     ventanas — la línea dice SIEMPRE a cuál vas ===
            tooltips.Add(new TooltipLine(Mod, "ModeIndicator", vista switch
            {
                1 => "[c/78788C:═══ Click der para ver EL FESTÍN ═══]",
                2 => "[c/78788C:═══ Click der para vista básica ═══]",
                _ => "[c/78788C:═══ Click der para ver EL ARMA ═══]",
            }));
        }
    }
}
