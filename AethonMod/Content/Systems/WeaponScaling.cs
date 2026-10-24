using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Sistema de escalado del Grimorio del Eterno por nivel (INFINITO).
    /// Todas las mejoras tienen máximo salvo donde se indique "Infinito".
    /// </summary>
    public static class WeaponScaling
    {
        // === STATS BASE POR NIVEL ===
        public const float MagicDamagePerLevel = 0.022f;
        public const float SummonDamagePerLevel = 0.01f;
        public const float CritPerLevel = 0.002f;
        public const float UseSpeedPerLevel = 0.003f;

        // === MANA ===
        // v6.45: ManaBase/ManaPer20Levels/ManaMax y ManaCost() ELIMINADOS —
        // el bolt es sin maná desde v6.42 (Item.mana = 0) y la única
        // referencia que quedaba era la línea del tooltip que mentía.
        // El coste del minion (MinionManaCost) vive: se activa apagando la
        // bandera ManaGratisEnPruebas de la config.

        // ================================================================
        //  STATS BASE
        // ================================================================

        /// <summary>Multiplicador de daño mágico (Infinito).</summary>
        public static float MagicDamageMult(int level) => 1f + level * MagicDamagePerLevel;

        /// <summary>Bonus de daño de invocación (Infinito).</summary>
        public static float SummonDamageBonus(int level) => level * SummonDamagePerLevel;

        /// <summary>Bonus de crítico (Máximo 100%).</summary>
        public static float CritBonus(int level)
        {
            float crit = level * CritPerLevel * 100f;
            if (crit > 100f) crit = 100f;
            return crit;
        }

        /// <summary>Bonus de armor penetration cada 5 niveles (Máximo 50%).</summary>
        public static float ArmorPenBonus(int level)
        {
            float pen = (level / 5) * 2f;
            if (pen > 50f) pen = 50f;
            return pen;
        }

        /// <summary>
        /// Multiplicador de use time (Máximo -25%).
        /// v5.25: Redondea el useTime efectivo a ENTERO para evitar doble Shoot.
        /// Causa raíz: si useTime × multiplier no es entero, tModLoader llama
        /// Shoot 2 veces por ciclo de animación. Redondear a entero lo arregla.
        /// El multiplier se calcula para que useTimeBase × multiplier sea entero.
        /// </summary>
        public static float UseSpeedMult(int level, int useTimeBase = 22)
        {
            // v5.78: guard contra division by zero
            if (useTimeBase <= 0) return 1f;
            float reduction = level * UseSpeedPerLevel;
            if (reduction > 0.25f) reduction = 0.25f;
            // Calcular useTime efectivo y redondear a entero
            int effectiveUseTime = (int)(useTimeBase * (1f - reduction));
            if (effectiveUseTime < 1) effectiveUseTime = 1;
            // Retornar el multiplier que produce ese useTime entero
            return (float)effectiveUseTime / useTimeBase;
        }

        // ================================================================
        //  MANA
        // ================================================================

        /// <summary>Costo de mana del minion: 15 + nivel, tope 100 (solo con ManaGratisEnPruebas = OFF).</summary>
        public static int MinionManaCost(int level)
        {
            int cost = 15 + level;
            if (cost > 100) cost = 100;
            return cost;
        }

        // ================================================================
        //  BONUS POR MANA FALTANTE
        // ================================================================

        /// <summary>+0.5% daño por 1% mana faltante (tope +50%).</summary>
        public static float LowManaDamageMult(int currentMana, int maxMana)
        {
            if (maxMana <= 0) return 1f;
            float missing = (float)(maxMana - currentMana) / maxMana;
            if (missing < 0f) missing = 0f;
            if (missing > 1f) missing = 1f;
            return 1f + missing * 0.5f;
        }

        // ================================================================
        //  STATS DEL JUGADOR (persisten al cambiar de arma)
        // ================================================================

        /// <summary>+1 mana max cada 4 niveles (Infinito).</summary>
        public static int BonusMana(int level) => level / 4;

        /// <summary>+2 vida max cada 20 niveles (Infinito).</summary>
        public static int BonusLife(int level) => (level / 20) * 2;

        // (v6.50.115 — la escalera de +1 slot de minion cada 10 niveles
        // de la v6.46–.114 MUERE: solo servía para inflar el escuadrón
        // de los ojos, y el escuadrón de la .115 es LLANO: ni equipo
        // ni nivel — ver EscuadronEterno abajo. Los minions VANILLA
        // nunca la leyeron: siguen a maxMinions de siempre.)

        /// <summary>EL ESCUADRÓN DE LOS OJOS DEL ETERNO (la letra .116:
        /// «en vez de 10 minion que sean 11, así se ve mejor en la
        /// animación de cuando está el libro fuera»): ONCE — con la
        /// banda COMPLETA las órbitas del libro cierran EXACTO (el
        /// anillo interior lleva 5 y el exterior 6: DOS RUEDAS llenas,
        /// sin huecos ni remiendos) — y la invocación que sería la 12.ª
        /// YA NO INVOCA: FORTALECE a un ojo vivo (+50% de daño por
        /// mejora, 4 mejoras máximas — OjoEternoMinion.MejorasDe/
        /// Fortalecer).</summary>
        public const int EscuadronEterno = 11;

        /// <summary>
        /// v6.50.115 — EL TECHO PLANO DE LOS OJOS DEL ETERNO (la letra:
        /// «en el caso de los minions del grimorio, 30 son demasiado…
        /// cuando llegue a 11 minions del grimorio en la siguiente
        /// invocación no invoca minion, lo que hace es fortalecer a uno
        /// que ya existe con un 50% de daño» — .116: ONCE ojos, la 12.ª
        /// fortalece): el 20+equipo+nivel de la .114 (que con todo
        /// puesto llegaba a 30) MUERE — EL ESCUADRÓN ES LLANO: ni el
        /// equipo de invocador ni el nivel del libro lo inflan (los
        /// minions VANILLA siguen con su maxMinions de siempre, sin
        /// tocar). La compuerta del clic derecho lee ESTE número y, con
        /// el escuadrón lleno, cada invocación FORTALECE a un ojo
        /// existente. (La firma conserva los parámetros de la .114 por
        /// los call sites — ya no cuentan: el tope es un PISO de
        /// maxMinions y NADA más.)
        /// </summary>
        public static int TechoOjosEterno(int equipoInvocador, int nivel)
        {
            // v6.51.0 — LA ESCALERA DE LA LETRA: «la cantidad de minions
            // están sujetos a la subida normal de nivel del grimorio:
            // cada 10 niveles 1 minion nuevo, en nivel 5 puede invocar
            // su primer minion, en nivel 10 su segundo minion y a partir
            // de aquí cada 10 niveles 1 nuevo» — nivel 5 → 1 · nivel 10
            // → 2 · nivel 20 → 3 · … nivel 100 → 11 (EscuadronEterno, el
            // tope de las órbitas que cierran exacto).
            if (nivel < 5) return 0;
            if (nivel < 10) return 1;
            int ojos = 2 + (nivel - 10) / 10;
            return System.Math.Min(EscuadronEterno, ojos);
        }

        /// <summary>v6.51.0 — EL DAÑO DE LOS OJOS SIN ARMADURA (la
        /// letra: «sin importar la armadura o accesorios que lleve el
        /// jugador, los minions del grimorio estarán sujetos a las
        /// características del grimorios»): el daño de nacimiento de un
        /// ojo nace SOLO del grimorio — su base, su nivel, su despertar
        /// y lo devorado (los banquetes de la .128-.129)… y el NIVEL 50
        /// DEL DESPERTAR lo multiplica (la letra: «mejora TODO el ataque
        /// de los minions y sus proyectiles»).
        /// v6.51.12 — LA PARIDAD DE LA LETRA NUEVA («el daño de
        /// invocacion es muy inferior al daño magico, parece que los
        /// calculos no se aplican igual, el calculo para el daño de
        /// invocacion debe ser el mismo que se usa para el daño magico
        /// […] y para que todos los stats del grimorio tambien sumen al
        /// daño de invocacion»): el cálculo es AHORA EL MISMO del clic
        /// izquierdo — el NIVEL escala con MagicDamageMult (+2,2 %/nivel
        /// — antes +1 %: LA BRECHA), LA REFORJA VIVA aporta SU columna
        /// de daño (Ignorante −10 % … Mítico +15 % … Supremo +25 %) y EL
        /// FESTÍN DEL PORTADOR suma (vestido por la presencia del libro,
        /// FactorDelLibro). Lo que queda EXCLUSIVO de cada clase vive en
        /// el pipeline de vanilla (GetTotalDamage: el accesorio que dice
        /// «daño mágico» SOLO al mágico; el genérico y el de invocación
        /// — a los ojos, MinionOriginalDamageGlobal).</summary>
        public static int DanoOjosDelEterno(int danoBase, int nivel, Player p)
        {
            // 1) EL NIVEL — EL MISMO del mágico (la brecha moría aquí:
            //    +1 %/nivel contra +2,2 %/nivel del clic izquierdo).
            float d = danoBase * MagicDamageMult(System.Math.Max(0, nivel));

            // 2) EL DESPERTAR (+1 % por nivel — como el mágico).
            int despertar = Content.Systems.DespertarGrimorio.NivelDe(p);
            d *= 1f + Content.Systems.DespertarGrimorio.BonusDano(despertar);

            // 3) LO DEVORADO (el festín de armas — como el mágico).
            d *= 1f + Players.ShardPlayer.BonusDanoDevorado(p);
            d *= 1f + Players.ShardPlayer.BonusDanoArmasDevoradas(p);

            // 4) v6.51.12 — LA REFORJA VIVA (la columna DAÑO del peldaño
            //    — el mismo multiplicador del clic izquierdo).
            d *= 1f + Content.Weapons.GrimoireEternal.PeldanoDe(nivel).Dano;

            // 5) v6.51.12 — EL FESTÍN DEL PORTADOR (las esencias que ÉL
            //    comió — vestidas por la presencia del libro: 100 % en
            //    la mano · 25 % en la bolsa · 0 guardado).
            d *= 1f + Players.ShardPlayer.BonusDanoEsencias(p) *
                Content.Globals.AlmaEternoGlobalItem.FactorDelLibro(p);

            // 6) EL NIVEL 50 DEL DESPERTAR: TODO el ataque de los ojos.
            if (despertar >= Content.Systems.DespertarGrimorio.NivelMax)
                d *= 1.25f;
            return System.Math.Max(1, (int)d);
        }

        // ================================================================
        //  KNOCKBACK (cada 10 niveles, máximo +100%)
        // ================================================================

        /// <summary>+10% knockback cada 10 niveles (tope +100%).</summary>
        public static float KnockbackMult(int level)
        {
            float bonus = (level / 10) * 0.1f;
            if (bonus > 1f) bonus = 1f;
            return 1f + bonus;
        }

        // ================================================================
        //  REGENERACIÓN (cada 20 niveles)
        // ================================================================

        /// <summary>+1 mana/seg cada 20 niveles (tope 10/seg).</summary>
        public static int ManaRegen(int level)
        {
            int regen = level / 20;
            if (regen > 10) regen = 10;
            return regen;
        }

        /// <summary>+0.5 vida/seg cada 20 niveles (tope 5/seg).</summary>
        public static float LifeRegen(int level)
        {
            float regen = (level / 20) * 0.5f;
            if (regen > 5f) regen = 5f;
            return regen;
        }

        // ================================================================
        //  REDUCCIÓN DE DAÑO (cada 100 niveles, máximo 10%)
        // ================================================================

        /// <summary>-1% daño recibido cada 100 niveles (tope 10%).</summary>
        public static float DamageReduction(int level)
        {
            float reduction = (level / 100) * 0.01f;
            if (reduction > 0.10f) reduction = 0.10f;
            return reduction;
        }

        // ================================================================
        //  LIFESTEAL (cada 7 niveles, empieza nivel 7)
        // ================================================================

        public static float LifestealPercent(int level)
        {
            if (level < 7) return 0f;
            return (level / 7) * 0.001f;
        }

        public static bool HasLifesteal(int level) => level >= 7;

        public static void ApplyLifesteal(Player player, int damageDone, int level)
        {
            // v6.50 — LA VIDA ES DEL DUEÑO (hallazgo auditoría MP nº2b): la
            // HP del jugador la manda SU cliente (vanilla) — curar la
            // réplica del server o de otros clientes era vida fantasma que
            // la sincronización borra. Solo el cliente del portador sana.
            // v6.50.2 — FIX (lifesteal muerto en Host&Play): el viejo gate
            // `netMode == Server` devolvía también en el LISTEN SERVER
            // (netMode 1 CON pantalla — los golpes del host se resuelven EN
            // el proceso server) → el host jamás sanaba. "Sin pantalla" es
            // Main.dedServ, no netMode. La línea de abajo (whoAmI !=
            // Main.myPlayer) ya protege las réplicas ajenas.
            if (Main.dedServ) return;
            if (player == null || player.whoAmI != Main.myPlayer) return;

            float pct = LifestealPercent(level);
            if (pct <= 0f || damageDone <= 0) return;
            int heal = (int)System.Math.Max(1, damageDone * pct);
            player.HealEffect(heal);
            player.statLife += heal;
            if (player.statLife > player.statLifeMax2)
                player.statLife = player.statLifeMax2;
        }

        // ================================================================
        //  PROYECTILES EXTRA (cada 3 niveles)
        // ================================================================

        public static int ExtraProjectiles(int level)
        {
            if (level < 3) return 0;
            return level / 3;
        }

        // ================================================================
        //  DAÑO EN ÁREA DEL BOLT (+1px por nivel, tope 100px)
        // ================================================================

        /// <summary>+1px radio de daño en área por nivel (tope 100px).</summary>
        public static int BoltAreaDamage(int level)
        {
            int area = level;
            if (area > 100) area = 100;
            return area;
        }

        // ================================================================
        //  STATS DEL MINION
        // ================================================================

        /// <summary>Hit cooldown: 15 - 1 cada 10 niveles (min 1).</summary>
        public static int MinionHitCooldown(int level) => System.Math.Max(1, 15 - level / 10);

        /// <summary>+50% daño de contacto cada 15 niveles (tope +500%).</summary>
        public static float MinionContactDamageMult(int level)
        {
            float bonus = (level / 15) * 0.5f;
            if (bonus > 5f) bonus = 5f;
            return 1f + bonus;
        }

        /// <summary>+50% velocidad del minion cada 30 niveles (tope +200%).</summary>
        public static float MinionSpeedMult(int level)
        {
            float bonus = (level / 30) * 0.5f;
            if (bonus > 2f) bonus = 2f;
            return 1f + bonus;
        }

        /// <summary>+100px rango de detección cada 10 niveles (tope 1500px, base 500).</summary>
        public static float MinionDetectionRange(int level)
        {
            float range = 500f + (level / 10) * 100f;
            if (range > 1500f) range = 1500f;
            return range;
        }

        /// <summary>v6.50.109 — LA IRA EN CADENA (la letra: «añadir una
        /// estadística para el grimorio eterno que mejore a los minion»):
        /// los rayos de los ojos saltan a +1 enemigo extra cada 20 niveles
        /// — base 2 saltos en nivel 1, tope 7 saltos en nivel 100. El
        /// cetro propio (TruenoPerlinStaff) NO lee esto: su cadena sigue
        /// fija en 2 — la estadística es del GRIMORIO y sólo mejoran LOS
        /// OJOS.</summary>
        public static int MinionChainCount(int level)
        {
            int extra = level / 20;
            if (extra > 5) extra = 5;
            return 2 + extra;
        }

        // ================================================================
        //  EL SEGUIMIENTO QUE CRECE CON EL NIVEL (v6.51.2)
        // ================================================================

        /// <summary>EL NIVEL DEL GRIMORIO del dueño de un proyectil
        /// (0 si no se puede leer — los proyectiles del mod que no
        /// pertenecen a un portador del libro siguen al ritmo base).</summary>
        public static int NivelDelDuenio(Projectile p)
        {
            if (p == null || p.owner < 0 || p.owner >= Main.maxPlayers) return 0;
            Player pl = Main.player[p.owner];
            if (pl == null || !pl.active) return 0;
            var sp = pl.GetModPlayer<Players.ShardPlayer>();
            return sp == null ? 0 : sp.NivelGrimorioPublico;
        }

        /// <summary>v6.51.2 — LA LETRA: «el seguimiento del agujero
        /// negro rúnico y de los soles rúnicos debe ser un poco más
        /// agresivo a medida que el nivel del grimorio aumente, por
        /// defecto un seguimiento simple y lento después de ser
        /// lanzado por los minions, y a partir de nivel 100 del
        /// grimorio el seguimiento aumenta un poquito más hasta tener
        /// un seguimiento muy agresivo a nivel 1000 del grimorio».
        /// EL ACELERADOR: ×1 hasta nivel 100 (simple y lento), luego
        /// crece SUAVE (raíz de la proporción) hasta ×9 en el 1000 —
        /// el multiplicador de la ACELERACIÓN de la persecución.</summary>
        public static float HomingAcel(int nivel)
        {
            if (nivel <= 100) return 1f;
            double t = (nivel - 100) / 900.0;
            if (t > 1.0) t = 1.0;
            return 1f + 8f * (float)System.Math.Sqrt(t);
        }

        /// <summary>El TOPE DE VELOCIDAD de la persecución: ×1 hasta
        /// nivel 100, hasta ×3,5 en el 1000 (el acelerón sin control
        /// haría al agujero cruzarse de largo: el tope crece
        /// acompasado).</summary>
        public static float HomingTope(int nivel)
        {
            if (nivel <= 100) return 1f;
            double t = (nivel - 100) / 900.0;
            if (t > 1.0) t = 1.0;
            return 1f + 2.5f * (float)System.Math.Sqrt(t);
        }

        // ================================================================
        //  HITOS (cada 5 niveles, con múltiples mejoras por hito)
        // ================================================================

        // v6.46: MilestonesForLevel() y NextMilestoneSummary() ELIMINADOS —
        // cero usos en todo el mod (verificados por grep): helpers muertos
        // de la era v5.x. El tooltip usa MilestoneRewards directamente.

        /// <summary>
        /// Devuelve TODAS las mejoras que se otorgan en un hito específico.
        /// Un hito puede tener múltiples mejoras simultáneas.
        /// </summary>
        public static List<string> MilestoneRewards(int milestone)
        {
            var rewards = new List<string>();
            int level = milestone * 5;

            // +1 slot de minion cada 10 niveles (v6.46: tope +10 al nivel 100,
            // sumado a la base del juego — antes decía "cada 5" y mentía)
            if (milestone % 2 == 0 && level <= 100) // nivel 10, 20, … 100
                rewards.Add("+1 slot de minion");

            // +1 bolt extra cada 3 niveles (en hitos múltiplos de 3)
            if (milestone % 3 == 0) // nivel 15, 30, 45...
                rewards.Add("+1 bolt extra");

            // v6.45: "+5% prob disparo doble" ELIMINADO de los hitos —
            // DoubleShotChance() nunca se llamaba desde ningún sitio: era
            // una promesa rota en el texto. Los bolts extra reales son los
            // de arriba (ExtraProjectiles, usado en Shoot).

            // Daño en área +1px cada 5 niveles
            rewards.Add("+1px daño en área");

            // Armor penetration +2% cada 5 niveles (cada hito)
            float pen = ArmorPenBonus(level);
            if (pen < 50f)
                rewards.Add("+2% armor penetration");

            // Knockback +10% cada 10 niveles
            if (milestone % 2 == 0) // nivel 10, 20, 30...
            {
                float kb = KnockbackMult(level);
                if (kb < 2f)
                    rewards.Add("+10% knockback");
            }

            // Regeneración mana +1/seg cada 20 niveles
            if (milestone % 4 == 0) // nivel 20, 40, 60...
            {
                int regen = ManaRegen(level);
                if (regen < 10)
                    rewards.Add("+1 mana/seg regen");
            }

            // Regeneración vida +0.5/seg cada 20 niveles
            if (milestone % 4 == 0)
            {
                float regen = LifeRegen(level);
                if (regen < 5f)
                    rewards.Add("+0.5 vida/seg regen");
            }

            // Vida max +2 cada 20 niveles
            if (milestone % 4 == 0)
                rewards.Add("+2 vida max");

            // Mana max +1 cada 4 niveles (en hitos múltiplos de 4)
            // Como los hitos son cada 5, agregamos mana en niveles que son múltiplos de 4
            // Esto se maneja en PostUpdateEquips, no en hitos.

            // Daño de contacto minion +50% cada 15 niveles
            if (milestone % 3 == 0) // nivel 15, 30, 45...
            {
                float dmg = MinionContactDamageMult(level);
                if (dmg < 6f)
                    rewards.Add("+50% daño contacto minion");
            }

            // Velocidad minion +50% cada 30 niveles
            if (milestone % 6 == 0) // nivel 30, 60, 90...
            {
                float spd = MinionSpeedMult(level);
                if (spd < 3f)
                    rewards.Add("+50% velocidad minion");
            }

            // Rango detección minion +100px cada 10 niveles
            if (milestone % 2 == 0)
            {
                float range = MinionDetectionRange(level);
                if (range < 1500f)
                    rewards.Add("+100px rango detección");
            }

            // v6.50.109 — LA IRA EN CADENA: +1 salto de cadena del rayo de
            // los ojos cada 20 niveles (nivel 20, 40, 60, 80, 100 — de 2 a 7)
            if (milestone % 4 == 0
                && MinionChainCount(level - 5) < MinionChainCount(level))
                rewards.Add("+1 salto de cadena del rayo");

            // Lifesteal +0.1% cada 7 niveles (en hitos cercanos)
            // Nivel 7, 14, 21, 28, 35... = hitos 1.4, 2.8, 4.2, 5.6, 7...
            // Redondeamos: hito 1 (nivel 5) no, hito 2 (nivel 10) cerca de 7, hito 3 (15) cerca de 14
            int lsTier = level / 7;
            int prevLsTier = (level - 5) / 7;
            if (lsTier > prevLsTier && level >= 7)
                rewards.Add("+0.1% lifesteal");

            // Reducción de daño -1% cada 100 niveles
            if (milestone % 20 == 0) // nivel 100, 200...
            {
                float dr = DamageReduction(level);
                if (dr < 0.10f)
                    rewards.Add("-1% daño recibido");
            }

            // Crítico mágico (cada hito da algo de crit, se muestra solo si no ha llegado al tope)
            float crit = CritBonus(level);
            if (crit < 100f)
                rewards.Add("+1% crítico mágico");

            return rewards;
        }
    }
}
