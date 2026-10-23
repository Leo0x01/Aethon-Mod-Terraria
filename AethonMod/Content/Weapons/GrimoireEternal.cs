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

        // ================================================================
        //  v6.51.10 — LA REFORJA VIVA: LA ESCALERA DE LOS MODIFICADORES
        //  MÁGICOS DE TERRARIA (la letra del usuario: «esta parte debes
        //  cambiarla, estos son los modificadores de Terraria y deben
        //  estar con sus modificadores de estadisticas correspondientes,
        //  ademas no debe empezar en (comun), debe empezar en (Ignorante)
        //  ya que es un arma magica y debe cambiar cada 33 niveles el
        //  modificador y tambien sus estadisticas, de esta forma el
        //  grimorio del eterno se reforja por si solo, asi que empieza en
        //  nivel 1 con (Ignorante) a nivel 33 mejora a (Transtornado) y
        //  asi ir mejorando hasta llegar a legendario… el legendario
        //  tendra las mismas estadisticas de Mítico pero con un 3% extra
        //  y luego el legendario supremo tendra las mismas estadisticas
        //  que Mítico pero con un 10% extra»).
        //
        //  LA ESCALERA — los 12 MODIFICADORES MÁGICOS de la wiki, del
        //  PEOR al MEJOR (columnas exactas de la tabla: Daño · Velocidad
        //  · % crítico · Costo de maná · Retroceso · Nivel · Valor), un
        //  peldaño CADA 33 NIVELES y el Supremo a los 100 del Legendario:
        //     1  → (Ignorante)     -10% daño · +20% maná · t-2 · -48,16%
        //    33  → (Transtornado)  -10% daño · -10% retroceso · t-1 · -34,39%
        //    66  → (Inepto)        +10% maná · t-1 · -19,00%
        //    99  → (Intenso)       +10% daño · +15% maná · t-1 · -12,58%
        //   132  → (Furioso)       +15% daño · +20% maná · +15% retro · +11,94%
        //   165  → (Maníaco)       -10% daño · +10% vel · -10% maná · +18,59%
        //   198  → (Prohibido)     +10% vel · +10% maná · +10% retro · +18,59%
        //   231  → (Experto)       -15% maná · t+1 · +32,25%
        //   264  → (Celeste)       +10% daño · -10% vel · -10% maná · +10% retro · +43,50%
        //   297  → (Místico)       +10% daño · -15% maná · t+2 · +60,02%
        //   330  → (Maestro)       +15% daño · -20% maná · +5% retro · +92,83%
        //   363  → (Mítico)        +15% daño · +10% vel · +5% crít · -10% maná · +15% retro · +209,85%
        //   400  → (Legendario)    EL NUESTRO: Mítico +3% en cada stat
        //   500  → (Legendario Supremo) LA EXCLUSIVA DEL MOD: Mítico +10%
        //         en cada stat — 100 niveles tras el Legendario: cuando el
        //         nombre consigue EL BRILLO característico (morado y negro
        //         con bordes dorados, negros y morados —
        //         NombreEternoGlobalItem).
        //
        //  LAS ESTADÍSTICAS SON DE VERDAD (la letra: «asegurate de que
        //  estas estadisticas de los modificadores de la reforja se
        //  apliquen correctamente»): Daño en ModifyWeaponDamage ·
        //  Velocidad en UseTime/UseAnimationMultiplier (positiva = más
        //  rápido) · Crítico en ModifyWeaponCrit · Maná en el coste de
        //  LOS OJOS (la invocación — el único maná que cobra el arma;
        //  positivo = más caro) · Retroceso en ModifyWeaponKnockback ·
        //  Valor en el precio del item (UpdateInventory). TODO suma UNA
        //  vez con las estadísticas de siempre (nivel/despertar/festín).
        // ================================================================

        /// <summary>UN PELDAÑO DE LA REFORJA VIVA: el modificador mágico
        /// de la wiki con SUS estadísticas exactas (las fracciones: +0.15
        /// = +15%; Velocidad positiva = más rápido; Maná positivo = MÁS
        /// caro; Valor = multiplicador del precio base del libro).</summary>
        public sealed class PeldanoReforja
        {
            public readonly string Nombre;
            public readonly int Nivel;         // nivel del libro que lo viste
            public readonly float Dano;        // +0.15 = +15% daño
            public readonly float Velocidad;   // +0.10 = +10% velocidad (− = lento)
            public readonly float Critico;     // +0.05 = +5% crítico
            public readonly float Mana;        // +0.20 = +20% coste de maná (− = rebaja)
            public readonly float Retroceso;   // +0.15 = +15% retroceso
            public readonly int Tier;          // el «Nivel» de la wiki (−2..+2)
            public readonly float Valor;       // +2.0985 = +209,85% de valor
            public PeldanoReforja(string nombre, int nivel, float dano,
                float velocidad, float critico, float mana, float retroceso,
                int tier, float valor)
            {
                Nombre = nombre; Nivel = nivel; Dano = dano;
                Velocidad = velocidad; Critico = critico; Mana = mana;
                Retroceso = retroceso; Tier = tier; Valor = valor;
            }
        }

        /// <summary>LA ESCALERA COMPLETA — los 12 modificadores mágicos de
        /// Terraria (de la tabla de la wiki, del peor al mejor por Valor)
        /// y las DOS cimas del mod: (Legendario) = Mítico con +3% extra
        /// en cada estadística (nivel 400) · (Legendario Supremo) = Mítico
        /// con +10% extra (nivel 500) — la exclusiva del libro.</summary>
        public static readonly PeldanoReforja[] LaReforja =
        {
            new PeldanoReforja("Ignorante",          1,   -0.10f, 0f,     0f,    +0.20f, 0f,     -2, -0.4816f),
            new PeldanoReforja("Transtornado",      33,   -0.10f, 0f,     0f,     0f,    -0.10f, -1, -0.3439f),
            new PeldanoReforja("Inepto",            66,    0f,    0f,     0f,    +0.10f, 0f,     -1, -0.1900f),
            new PeldanoReforja("Intenso",           99,   +0.10f, 0f,     0f,    +0.15f, 0f,     -1, -0.1258f),
            new PeldanoReforja("Furioso",          132,   +0.15f, 0f,     0f,    +0.20f, +0.15f, +1, +0.1194f),
            new PeldanoReforja("Maníaco",           165,  -0.10f, +0.10f, 0f,    -0.10f, 0f,     +1, +0.1859f),
            new PeldanoReforja("Prohibido",         198,   0f,    +0.10f, 0f,    +0.10f, +0.10f, +1, +0.1859f),
            new PeldanoReforja("Experto",           231,   0f,    0f,     0f,    -0.15f, 0f,     +1, +0.3225f),
            new PeldanoReforja("Celeste",           264,  +0.10f, -0.10f, 0f,    -0.10f, +0.10f, +1, +0.4350f),
            new PeldanoReforja("Místico",           297,  +0.10f, 0f,     0f,    -0.15f, 0f,     +2, +0.6002f),
            new PeldanoReforja("Maestro",           330,  +0.15f, 0f,     0f,    -0.20f, +0.05f, +2, +0.9283f),
            new PeldanoReforja("Mítico",            363,  +0.15f, +0.10f, +0.05f, -0.10f, +0.15f, +2, +2.0985f),
            // LAS CIMAS DEL MOD (la letra de la .51.11): Legendario no
            // existe para las armas mágicas de vanilla (el techo es
            // Mítico) — el NUESTRO son las MISMAS estadísticas de Mítico
            // con el extra prometido: nivel 400 y nivel 500.
            new PeldanoReforja("Legendario",        400,  +0.18f, +0.13f, +0.08f, -0.13f, +0.18f, +2, +2.4000f),
            new PeldanoReforja("Legendario Supremo", 500,  +0.25f, +0.20f, +0.15f, -0.20f, +0.25f, +2, +3.1000f),
        };

        /// <summary>El ÍNDICE del peldaño de la reforja por el nivel del
        /// libro: cada 33 niveles sube UN modificador (33 · 66 · 99 · …
        /// · 363)… (Legendario) en el nivel 400 y (Legendario Supremo)
        /// en el 500. Empieza en (Ignorante) — el peor modificador
        /// mágico: así nace el arma mágica.</summary>
        public static int IndiceEtiqueta(int nivel)
        {
            int idx = 0;
            for (int i = 0; i < LaReforja.Length; i++)
                if (nivel >= LaReforja[i].Nivel) idx = i;
            return idx;
        }

        /// <summary>EL PELDAÑO ACTUAL del libro (modificador + estadísticas).
        /// Null-safe: sin nivel, el (Ignorante) del nacimiento.</summary>
        public static PeldanoReforja PeldanoDe(int nivel)
            => LaReforja[IndiceEtiqueta(nivel)];

        /// <summary>EL TEXTO de la etiqueta (el paréntesis que SIEMPRE
        /// va AL FINAL del nombre).</summary>
        public static string NombreEtiqueta(int indice)
            => (indice >= 0 && indice < LaReforja.Length)
                ? LaReforja[indice].Nombre : LaReforja[0].Nombre;

        /// <summary>LA LÍNEA DE ESTADÍSTICAS del peldaño para el tooltip
        /// (los modificadores de la wiki, con sus colores: lo bueno
        /// verde, lo malo rojo — el maná al revés: caro = rojo).</summary>
        public static string StatsEtiqueta(PeldanoReforja p)
        {
            var partes = new System.Collections.Generic.List<string>();
            void Trozo(float valor, string clave, bool invertir)
            {
                if (MathF.Abs(valor) < 0.0005f) return;
                float v = invertir ? -valor : valor;   // el maná: -15% es BUENO
                string color = v > 0f ? "7FE89B" : "FF8C8C";
                string signo = valor > 0f ? "+" : "-";
                partes.Add("[c/" + color + ":" + signo +
                    MathF.Round(MathF.Abs(valor) * 100f) + "% " + clave + "]");
            }
            Trozo(p.Dano, "daño", false);
            Trozo(p.Velocidad, "velocidad", false);
            Trozo(p.Critico, "crítico", false);
            Trozo(p.Mana, "coste de maná", true);
            Trozo(p.Retroceso, "retroceso", false);
            return partes.Count > 0 ? string.Join("[c/78788C: · ]", partes) : "—";
        }

        /// <summary>EL COLOR del nombre por peldaño — la escalera
        /// cromática de la REFORJA (del gris roto del Ignorante al oro
        /// del Legendario, pasando por el violeta cósmico que respira
        /// del Mítico)… y el SUPREMO lo dibuja NombreEternoGlobalItem
        /// (morado y negro con bordes dorados, negros y morados + sus
        /// destellos — este color es solo el respaldo).</summary>
        public static Color ColorEtiqueta(int indice)
        {
            switch (indice)
            {
                case 0: return new Color(198, 198, 198);  // Ignorante — el gris pálido
                case 1: return new Color(176, 148, 158);  // Transtornado — gris roto
                case 2: return new Color(168, 178, 148);  // Inepto — verdoso apagado
                case 3: return new Color(216, 196, 150);  // Intenso — ámbar sucio
                case 4: return new Color(108, 212, 128);  // Furioso — el verde
                case 5: return new Color(140, 220, 120);  // Maníaco — verde vivo
                case 6: return new Color(120, 210, 190);  // Prohibido — turquesa
                case 7: return new Color(110, 190, 235);  // Experto — celeste
                case 8: return new Color(130, 175, 255);  // Celeste — azul cielo
                case 9: return new Color(255, 158, 66);   // Místico — el naranja épico
                case 10:
                    {
                        // el violeta cósmico que respira (LegendarioSupremo.cs)
                        float t = Main.GlobalTimeWrappedHourly;
                        float r = 0.62f + 0.20f * MathF.Sin(t * 0.9f);
                        float g = 0.22f + 0.14f * MathF.Sin(t * 0.53f + 2.1f);
                        float b = 0.95f + 0.05f * MathF.Sin(t * 1.7f + 0.6f);
                        return new Color(r, g, b);
                    }
                case 11: return new Color(255, 205, 92);  // Mítico — el oro clásico
                case 12: return new Color(255, 220, 110); // Legendario — oro brillante
                case 13: return new Color(178, 120, 235); // Supremo — (lo pinta el PreDraw)
                default: return new Color(198, 198, 198);
            }
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
            Item.reuseDelay = 7;           // v6.51.11: LA CORREA FINAL del tope (7 ticks — igual que MinTicksUso: NADA dispara doble)
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
        //  v6.51.9 — EL NOMBRE DEL LIBRO ES SUYO (la letra: «cuando
        //  agarre al grimorio dormido al llegar al inventario decia
        //  Grimorio del Eterno (Agil)»): los PREFIJOS DE VANILLA MURIERON
        //  — QuickSpawnItem pasa Prefix(-1) y el 75 % de las veces el
        //  libro nacía con un prefijo universal (en la localización
        //  española se leen ENTRE PARÉNTESIS tras el nombre: «(Ágil)»).
        //  El paréntesis del Eterno es SU ETIQUETA DE ESCALERA y NADA
        //  MÁS — y el libro no se reforja: su poder es su nivel, su
        //  despertar y su festín (el yunque se le niega en
        //  AlmaEternoGlobalItem.CanReforge).
        // ================================================================

        public override bool? PrefixChance(int pre, Terraria.Utilities.UnifiedRandom rand)
            => false;   // NUNCA un prefijo: ni al nacer (-1) ni reforge (-3)

        /// <summary>EL PARÉNTESIS VIVE EN EL NOMBRE (y LA MIGRACIÓN):
        /// el libro en la bolsa lleva SIEMPRE su etiqueta puesta (el
        /// texto del nombre — se ve en el tooltip, en el chat de
        /// recogida, en todas partes), y los Eternos viejos que ya
        /// cargaban un prefijo de vanilla lo SUELTAN aquí al primer
        /// tick.</summary>
        public override void UpdateInventory(Player player)
        {
            base.UpdateInventory(player);   // EL RELOJ DEL HAMBRE de la familia — INTACTO
            try
            {
                if (Item == null || Item.IsAir) return;
                if (Item.prefix != 0)
                    Item.ResetPrefix();     // la migración: fuera «(Ágil)»
                var sl = GetShard(Item);
                int idxEtiqueta = IndiceEtiqueta(sl?.Level ?? 1);
                string nombreBase = Lang.GetItemNameValue(Item.type);
                Item.SetNameOverride(nombreBase + " (" + NombreEtiqueta(idxEtiqueta) + ")");
                // v6.51.10 — LA REFORJA VIVA TAMBIÉN PAGA EN TIENDA (la
                //     columna «Valor» de la wiki): el precio del libro
                //     escala con SU modificador (Mítico = +209,85% — el
                //     múltiplo exacto de la tabla). Siempre recalculado
                //     del precio base: idempotente, sin acumulación.
                Item.value = (int)(100000L * (1f + LaReforja[idxEtiqueta].Valor));
            }
            catch { }
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

            // v6.51.10 — LA REFORJA VIVA (la letra: «estos son los
            //     modificadores de Terraria y deben estar con sus
            //     modificadores de estadisticas correspondientes»): la
            //     columna DAÑO del peldaño actual (Ignorante -10% …
            //     Mítico +15% … Supremo +25%) — multiplica UNA vez con
            //     todo lo de arriba (nivel/despertar/festín).
            damage *= 1f + PeldanoDe(sl?.Level ?? 1).Dano;

            // v6.51.11 — EL PODER DEVORADO MURIÓ como bonus del libro (la
            //     letra: «las esencias consumidas por el grimorio no
            //     cuentan para el aumento general de daño, solo cuentan
            //     para su despertar»): el daño por esencias vive AHORA en
            //     el PORTADOR que las come (ShardPlayer.ModifyWeaponDamage:
            //     +0,1 % por esencia en TODO su daño, tope +100 % con
            //     1000 — al tope, cada esencia es 1 moneda de oro).
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
            // v6.51.10 — LA REFORJA VIVA: la columna RETROCESO del
            //     peldaño (Transtornado -10% … Mítico +15% … Supremo
            //     +25%).
            knockback *= 1f + PeldanoDe(GetShard(Item)?.Level ?? 1).Retroceso;
        }

        /// <summary>v6.50.129 — LA MIRADA DEL FESTÍN: el crítico de las
        /// armas mágicas comidas (+ el Corazón Estelar de las armas de
        /// estrellas) — la tercera mejora del festín de la bruma.</summary>
        public override void ModifyWeaponCrit(Player player, ref float crit)
        {
            crit += Players.ShardPlayer.BonusCritArmasDevoradas(player) * 100f;
            // v6.51.10 — LA REFORJA VIVA: la columna % ATAQUE CRÍTICO del
            //     peldaño (solo Mítico/Legendario/Supremo la traen: +5 /
            //     +8 / +15).
            crit += PeldanoDe(GetShard(Item)?.Level ?? 1).Critico * 100f;
        }

        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            // la regla de la casa: SIN MANÁ — el nivel escala daño y
            // velocidad; el RECURSO de esta arma es el hambre.
            Item.mana = 0;
            reduce = 1f;
            mult = 0f;
        }

        // ================================================================
        //  v6.51.11 — EL TOPE MÁXIMO DE VELOCIDAD (la letra del usuario:
        //  «a la esta estadisticas de aqui, velocidad: Velocidad → el
        //  tiempo de uso (animación y disparo juntos: nunca dispara
        //  doble), ponle un tope para que nunca dispare el doble, un
        //  tope maximo de velocidad y que de ahi no suba, tambien cambia
        //  todas las estadisticas y sistema que afecten a la velocidad
        //  para que incluso todos juntos siempre se queden en el tope
        //  maximo y nada lo pueda romper»): TODAS las fuentes de
        //  velocidad del Eterno — el NIVEL (WeaponScaling.UseSpeedMult),
        //  el FESTÍN DE ARMAS (LA PRISA + EL ALMA VELOZ) y LA REFORJA
        //  VIVA (la columna Velocidad de la wiki) — se multiplican
        //  JUNTAS en UNA sola función… y el resultado NUNCA baja del
        //  TOPE: con los 22 ticks de base, 7 TICKS POR USO, JAMÁS 6 (ni
        //  el nivel infinito, ni el festín completo, ni la reforja
        //  suprema — TODOS JUNTOS se quedan en el tope). El useTime y
        //  la useAnimation comparten EL MISMO factor (la regla
        //  anti-doble-Shoot de la v5.26): la animación NUNCA queda por
        //  debajo del disparo, y el reuseDelay de 7 es la correa final.
        // ================================================================

        /// <summary>EL TOPE DURO en ticks: el uso del Eterno JAMÁS baja
        /// de aquí (22 → 7 = ×3,14 de velocidad — nada lo rompe).</summary>
        public const int MinTicksUso = 7;

        /// <summary>EL FACTOR TOTAL de velocidad (useTime y useAnimation
        /// JUNTOS — una sola fuente de verdad): nivel × festín × reforja,
        /// topeado en ticks ENTEROS. NADA puede romper el tope.</summary>
        private float FactorVelocidadTotal(Player player, int baseTicks)
        {
            var sl = GetShard(Item);
            if (sl == null || baseTicks <= 0) return 1f;

            // 1) EL NIVEL (−0,3 %/nivel con tope interno del −25 % — y su
            //    redondeo a entero de la v5.25, la cura del doble-Shoot).
            float factor = WeaponScaling.UseSpeedMult(sl.Level, baseTicks);

            // 2) EL FESTÍN DE ARMAS (LA PRISA de las armas a distancia +
            //    EL ALMA VELOZ de las hojas rápidas — tope conjunto +35 %).
            factor *= 1f - Players.ShardPlayer.BonusVelocidadArmasDevoradas(player);

            // 3) LA REFORJA VIVA (la columna Velocidad de la wiki:
            //    Maníaco/Prohibido +10 % … Celeste −10 % … Supremo +20 %).
            factor *= 1f - PeldanoDe(sl.Level).Velocidad;

            // 4) LA CUANTIZACIÓN + EL TOPE: el uso efectivo SIEMPRE en
            //    ticks ENTEROS (animación y disparo JUNTOS — jamás
            //    fracciones que disparen doble)… y NUNCA por debajo del
            //    TOPE DURO: ni todo junto lo rompe (la letra).
            int ticks = (int)System.Math.Floor(baseTicks * factor);
            if (ticks < MinTicksUso) ticks = MinTicksUso;
            if (ticks > baseTicks * 2) ticks = baseTicks * 2;   // la lentitud del Celeste tampoco se desboca
            return (float)ticks / baseTicks;
        }

        public override float UseTimeMultiplier(Player player)
            => FactorVelocidadTotal(player, Item.useTime);

        /// <summary>
        /// v5.26: reduce useAnimation proporcionalmente al useTime para que
        /// NUNCA sea menor (si lo fuera, tModLoader dispararía Shoot 2 veces
        /// por ciclo de animación). v6.51.11 — EL MISMO FACTOR TOTAL del
        /// useTime (FactorVelocidadTotal: la única fuente de verdad):
        /// animación y disparo caminan JUNTOS, topeados JUNTOS.
        /// </summary>
        public override float UseAnimationMultiplier(Player player)
            => FactorVelocidadTotal(player, Item.useAnimation);

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
                // v6.51.10 — LA REFORJA VIVA: la columna COSTO DE MANÁ del
                //     peldaño se cobra en EL OJO — el único maná que cobra
                //     esta arma (la regla de la casa: el clic izquierdo
                //     vive del hambre, no del maná). Ignorante +20% …
                //     Experto/Místico -15% … Supremo -20%.
                minionCost = (int)System.Math.Ceiling(
                    minionCost * (1f + PeldanoDe(level).Mana));
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
                int ojoIdx = Projectile.NewProjectile(source, position, Vector2.Zero,
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.OjoEternoMinion>(),
                    danoOjo, knockback, player.whoAmI, 0f, 0f, level);
                // v6.51.9 — LA AUDITORÍA DEL ×1,25: si el daño nació con el
                // bonus del DESPERTAR 50 HORNEADO (DanoOjosDelEterno lo
                // hornea), se marca — las consumidoras (el rayo del ojo,
                // los ataques combinados) NO vuelven a multiplicarlo.
                if (ojoIdx >= 0 && Main.projectile[ojoIdx].ModProjectile
                        is global::AethonMod.Content.Projectiles.OjoEternoMinion ojoNuevo)
                    ojoNuevo.Bonus50Horneado =
                        DespertarGrimorio.NivelDe(player) >= DespertarGrimorio.NivelMax;
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
        //  completa SE PARTE EN DOS.
        //  v6.51.9 — CUATRO VENTANAS (la letra: «poner en una nueva
        //  ventana de informacion todo el apartado de Progresion, y que
        //  la ventana de progresion sea la segunda ventana, primero la
        //  ventana normal, luego la de progresion, luego la de las
        //  estadisticas del arma y por ultimo la ventana de festin, la
        //  ventana de festin deberia tener otro nombre ya que lo que
        //  muestra es informacion del escuadron y sus ataques
        //  combinados») — el clic derecho cicla (TooltipToggleItem):
        //    · 1 PROGRESIÓN   — EL NIVEL · EL DESPERTAR · SUS REQUISITOS
        //      · EL HAMBRE · LOS HITOS (el apartado entero, en SU ventana:
        //      la básica ya no lo lleva — y el camino solo enseña EL
        //      PRÓXIMO hito, no la escalera completa).
        //    · 2 EL ARMA      — DAÑO · RECURSOS · PROYECTIL · BONUS.
        //    · 3 EL ESCUADRÓN — LOS OJOS DEL ETERNO · LO DEVORADO · LOS
        //      ATAQUES COMBINADOS (antes «EL FESTÍN»: eso es lo que
        //      enseña — el escuadrón del libro, y así se llama).
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
            // v6.51.9 — LAS CUATRO VENTANAS: 0 básica · 1 PROGRESIÓN ·
            // 2 EL ARMA · 3 EL ESCUADRÓN (la vieja ventana del festín se
            // llama EL ESCUADRÓN — eso es lo que enseña: el escuadrón del
            // libro y sus ataques combinados).
            int vista = Globals.TooltipToggleItem.VistaEstadisticas;
            bool completa = sl != null && vista != 0;
            bool ventanaProgresion = sl != null && vista == 1;
            bool ventanaArma = sl != null && vista == 2;
            bool ventanaFestin = sl != null && vista == 3;

            // === v6.51.9 — EL COLOR DE LA ETIQUETA DEL NOMBRE (la letra:
            //     «a medida que el grimorio suba de nivel esa etiqueta ()
            //     debe ir mejorando… hasta llegar a Legendario»): el
            //     paréntesis SIEMPRE AL FINAL del nombre es SU ETIQUETA DE
            //     ESCALERA — Común → Raro → Épico → Mítico → Legendario →
            //     y 50 niveles después LA EXCLUSIVA DEL MOD: Legendario
            //     Supremo (cada 11 niveles un peldaño). El TEXTO vive en el
            //     nombre del item (SetNameOverride en UpdateInventory y al
            //     nacer en AlmaEternoGlobalItem.OnSpawn — se ve en el
            //     tooltip, en el chat de recogida, en todas partes); aquí
            //     se le viste SU COLOR de escalera (el Supremo lo dibuja
            //     entero NombreEternoGlobalItem). ===
            try
            {
                int idxEtiqueta = IndiceEtiqueta(sl?.Level ?? 1);
                string etiquetaTxt = "(" + NombreEtiqueta(idxEtiqueta) + ")";
                for (int i = 0; i < tooltips.Count; i++)
                {
                    if (tooltips[i].Mod != "Terraria" || tooltips[i].Name != "ItemName")
                        continue;
                    // (por si el libro se mira desde un cofre — sin
                    // UpdateInventory que haya puesto el nombre: el
                    // paréntesis se pone aquí, SIN duplicarlo)
                    if (!tooltips[i].Text.EndsWith(etiquetaTxt))
                        tooltips[i].Text = tooltips[i].Text + " " + etiquetaTxt;
                    tooltips[i].OverrideColor = ColorEtiqueta(idxEtiqueta);
                    break;
                }
            }
            catch { }

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

                // === v6.51.11 — LA REFORJA EN LA VENTANA BÁSICA (la letra:
                //     «ahora sobre los modificadores de forja, el aumento
                //     que estos dan deben aparecer en la ventana de
                //     informacion basica»): el modificador que viste el
                //     libro con SUS estadísticas de la wiki — las mismas
                //     líneas coloreadas de la ventana EL ARMA, compactas
                //     aquí, para leerse de un vistazo. ===
                {
                    var rungB = PeldanoDe(sl.Level);
                    tooltips.Add(new TooltipLine(Mod, "ReforjaBasica",
                        $"[c/B26EEC:Reforja ({rungB.Nombre}):] {StatsEtiqueta(rungB)}"));
                }
            }

            // === v6.51.11 — EL FESTÍN DEL PORTADOR (la letra: «sobre las
            //     esencia corrupta estas si las consume el jugador
            //     aumentan su ataque siguen aumentando hasta un maximo
            //     de 100 %… cuando el jugador cumpla con el limite de
            //     esencias consumida y llegue al tope que es 100% de daño
            //     o sea que el jugador consuma 1000 esencias corruptas,
            //     entonces las esencias se convierten en monedas de oro,
            //     1 esencia 1 de oro»): el contador de lo que EL JUGADOR
            //     comió — lo que el LIBRO come solo despierta y NO sube
            //     este número (el viejo poder devorado migró aquí). Solo
            //     si el portador ya probó una (el nuevo calla). ===
            Player portadorTip = Main.LocalPlayer;
            var spDevorado = Item != null && !Item.IsAir
                ? Item.GetGlobalItem<Globals.AlmaEternoGlobalItem>()
                : null;
            var spPortador = portadorTip != null
                ? portadorTip.GetModPlayer<Players.ShardPlayer>() : null;
            int esenciasDelPortador = spPortador?.EsenciasConsumidas ?? 0;
            // v6.51.1 — LO DEVORADO solo en SU ventana (la letra: la
            // ventana del escuadrón es SOLO estadísticas del escuadrón y
            // su festín — nada de progresión ni descripciones; v6.51.9:
            // ahora es la CUARTA ventana, EL ESCUADRÓN).
            if (ventanaFestin && esenciasDelPortador > 0)
            {
                int vistos = System.Math.Min(esenciasDelPortador,
                    Players.ShardPlayer.EsenciasTope);
                string pctDanoTxt = (vistos * 0.1f).ToString("0.#",
                    System.Globalization.CultureInfo.InvariantCulture);
                tooltips.Add(new TooltipLine(Mod, "DevoradoAltar",
                    Language.GetTextValue("Mods.AethonMod.Caida.DevoradoTooltip",
                        vistos, pctDanoTxt)));
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

            // === v6.50.110 — LA VENTANA DEL ARMA (v6.51.9: la TERCERA de
            //     las cuatro — tras la progresión): el poder del libro
            //     (DAÑO / RECURSOS / PROYECTIL / BONUS). El repertorio de
            //     la .106/.107 con las etiquetas HONESTAS del análisis:
            //     el crítico y la penetración son MÁGICOS y viven
            //     SOSTENIENDO el libro (la nota al pie lo aclara) ===
            if (ventanaArma)
            {
                // === v6.51.10 — LA REFORJA VIVA (la letra: «de esta forma
                //     el grimorio del eterno se reforja por si solo»):
                //     el modificador mágico ACTUAL con SUS estadísticas
                //     de la wiki (aplicadas de verdad — ver los hooks) y
                //     el próximo peldaño de la escalera. ===
                {
                    var rungA = PeldanoDe(sl.Level);
                    int idxA = IndiceEtiqueta(sl.Level);
                    tooltips.Add(new TooltipLine(Mod, "SectionReforja",
                        "[c/C49CFF:═══ LA REFORJA ═══]"));
                    tooltips.Add(new TooltipLine(Mod, "ReforjaActual",
                        $"[c/E8D8FF:({rungA.Nombre})]  " +
                        $"[c/78788C:(viste en el nivel {rungA.Nivel})]"));
                    tooltips.Add(new TooltipLine(Mod, "ReforjaStats",
                        StatsEtiqueta(rungA)));
                    if (idxA + 1 < LaReforja.Length)
                    {
                        var prox = LaReforja[idxA + 1];
                        tooltips.Add(new TooltipLine(Mod, "ReforjaProxima",
                            $"[c/78788C:Próxima reforja: ][c/9B6FE8:({prox.Nombre})]" +
                            $"[c/78788C: en el nivel {prox.Nivel}]"));
                    }
                }

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

            // === v6.50.110 — LA VENTANA DEL ESCUADRÓN (v6.51.9 — LA
            //     CUARTA Y ÚLTIMA: antes «EL FESTÍN», pero lo que enseña
            //     es el escuadrón del libro y sus ataques combinados — la
            //     letra le puso SU NOMBRE VERDADERO): LOS OJOS DEL ETERNO
            //     + LO DEVORADO + LOS ATAQUES COMBINADOS ===
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
                    // v6.51.9 — EL DAÑO POR ANILLO (la letra: «con cada
                    //     anillo runico extra del sol runico este aumenta
                    //     su daño tambien»): +50% por anillo de más.
                    tooltips.Add(new TooltipLine(Mod, "WSol",
                        $"[c/E8D8FF:Sol Rúnico: {Systems.DespertarGrimorio.AnillosDelSol(despertar)} anillo(s) · " +
                        $"{Systems.DespertarGrimorio.SolesPorMinuto(despertar)}/min]  " +
                        "[c/78788C:(2+ ojos mejorados, al azar · +50% daño por anillo extra)]"));
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
            //     v6.51.9 — EN SU PROPIA VENTANA, LA SEGUNDA (la letra:
            //     «poner en una nueva ventana de informacion todo el
            //     apartado de Progresion, y que la ventana de progresion
            //     sea la segunda ventana» — la básica ya no la lleva).
            //     v6.50.108 — EL ORDEN DE LA LETRA: la BARRA de hambre
            //     primero y el estado JUSTO DEBAJO. ===
            if (ventanaProgresion)
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

                // === v6.51.10 — LA REFORJA VIVA CAMINA CON EL NIVEL (la
                //     letra: «debe cambiar cada 33 niveles el modificador
                //     y tambien sus estadisticas»): UNA línea — dónde
                //     está el libro y cuándo sube su modificador. ===
                {
                    var rungP = PeldanoDe(sl.Level);
                    int idxP = IndiceEtiqueta(sl.Level);
                    if (idxP + 1 < LaReforja.Length)
                    {
                        var proxP = LaReforja[idxP + 1];
                        tooltips.Add(new TooltipLine(Mod, "ReforjaViva",
                            $"[c/78788C:Reforja: ][c/E8D8FF:({rungP.Nombre})]" +
                            $"[c/78788C: → ][c/9B6FE8:({proxP.Nombre})]" +
                            $"[c/78788C: en el nivel {proxP.Nivel}]"));
                    }
                    else
                    {
                        tooltips.Add(new TooltipLine(Mod, "ReforjaViva",
                            $"[c/78788C:Reforja: ][c/E8D8FF:({rungP.Nombre})]" +
                            "[c/78788C: — la cima del mod]"));
                    }
                }

                // === v6.51.0 — LA BARRA DEL DESPERTAR (la letra: «estos
                //     niveles deben estar ubicados en la ventana de
                //     información DEBAJO de la barra de niveles y ENCIMA
                //     de la barra de hambre») + la LISTA PEQUEÑA de lo
                //     que necesita el próximo nivel («para subirla de
                //     nivel se presentará una lista pequeña de lo que
                //     necesita»). ===
                try
                {
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
                    // dinero · el arma del peldaño) — v6.51.11 LOS
                    // CONTADORES DEL LIBRO (la comida se sirve EN EL
                    // SUELO: la lista cuenta lo que el libro YA COMIÓ de
                    // cada ofrenda — v6.51.5 la lista viva con su ✓
                    // dorado latiendo (DespertarRequisitosGlobalItem) y
                    // los sprites mini del arma del peldaño).
                    if (despertar < Systems.DespertarGrimorio.NivelMax)
                    {
                        int esencias = Systems.DespertarGrimorio.EsenciasPara(siguiente);
                        long monedas = Systems.DespertarGrimorio.MonedasPara(siguiente);

                        // v6.51.11 — LA MESA DEL LIBRO (los contadores
                        // parciales de la ofrenda: lo que YA comió de
                        // este peldaño — persisten y viajan por el cable).
                        var almaLista = Globals.AlmaEternoGlobalItem.De(Item);
                        int comidasEsencias = almaLista?.OfrendaEsencias ?? 0;
                        long comidasMonedas = almaLista?.OfrendaMonedas ?? 0L;
                        int comidosMineral = almaLista?.OfrendaMineral ?? 0;
                        bool devoradaArma = almaLista?.OfrendaArma ?? false;

                        // (v6.51.6: UN SOLO COLOR POR LÍNEA, el ✓ DENTRO
                        // del mismo span, y el arma con SU NOMBRE + SU
                        // SPRITE INLINE [i:id] — el tag de item NATIVO de
                        // tML, a prueba de todo).
                        string colOK = "7FE89B", colNo = "C4A2FF";

                        // === EL REQUISITO 1 — LAS ESENCIAS (las comidas). ===
                        bool esenciasOK = comidasEsencias >= esencias;
                        tooltips.Add(new TooltipLine(Mod, "DespertarReq1",
                            $"[c/78788C:  Para Despertar {siguiente}:]"));
                        tooltips.Add(new TooltipLine(Mod, "DespertarReq2",
                            $"[c/{(esenciasOK ? colOK : colNo)}:  • Esencias Corruptas: {Math.Min(comidasEsencias, esencias)}/{esencias}{(esenciasOK ? "  ✓" : "")}]"));

                        // === EL REQUISITO 2 — EL DINERO (lo comido — y
                        //     lo que falta, para saber cuánto soltar). ===
                        bool monedasOK = comidasMonedas >= monedas;
                        string textoMonedas = Systems.DespertarGrimorioSistema.ItemToText(monedas);
                        if (!monedasOK)
                            textoMonedas += $" — faltan {Systems.DespertarGrimorioSistema.ItemToText(monedas - comidasMonedas)}";
                        tooltips.Add(new TooltipLine(Mod, "DespertarReq3",
                            $"[c/{(monedasOK ? colOK : colNo)}:  • {textoMonedas}{(monedasOK ? "  ✓" : "")}]"));

                        // === EL REQUISITO 3 — EL ARMA DEL PELDAÑO (o
                        //     v6.51.8: LA OFRENDA MINERAL): SU NOMBRE +
                        //     SU SPRITE al lado (cada alternativa de este
                        //     peldaño, [i:id] = el icono del item
                        //     RENDERIZADO POR EL PARSER DE tML). ===
                        if (Systems.DespertarGrimorio.EsNivelMineral(siguiente))
                        {
                            // === v6.51.8 — LA OFRENDA MINERAL (la cima
                            //     45-50: los lingotes del fin del mundo,
                            //     sin corrupción) — el contador de los
                            //     COMIDOS (v6.51.11: parcial — el libro
                            //     come solo los que le faltan). ===
                            int tipoMin = Systems.DespertarGrimorio.MineralDe(siguiente);
                            int cantMin = Systems.DespertarGrimorio.CantidadMineralDe(siguiente);
                            bool mineralOK = comidosMineral >= cantMin;
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
                            trozosMin.Add($"[c/{colMin}:{Math.Min(comidosMineral, cantMin)}/{cantMin} × {nombreMin}{(mineralOK ? "  ✓" : "")}]");
                            tooltips.Add(new TooltipLine(Mod, "DespertarReq4",
                                string.Join(" ", trozosMin)));
                            tooltips.Add(new TooltipLine(Mod, "DespertarReq5",
                                "[c/78788C:  (suelta los lingotes DELANTE del grimorio seleccionado — come solo los que faltan)]"));
                        }
                        else
                        {
                        // v6.51.11 — EL ARMA se devora ENTERA (una sola):
                        // el ✓ es el sello del libro (la comió o no).
                        string colArma = devoradaArma ? colOK : colNo;
                        int[] peldano = Systems.DespertarGrimorio.ArmasDelNivel[siguiente];
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
                        if (devoradaArma) trozosArma.Add($"[c/{colArma}: ✓]");
                        tooltips.Add(new TooltipLine(Mod, "DespertarReq4",
                            string.Join(" ", trozosArma)));
                        tooltips.Add(new TooltipLine(Mod, "DespertarReq5",
                            "[c/78788C:  (corrupta — suéltala DELANTE del grimorio seleccionado y la devorará)]"));
                        }

                        // === v6.51.9 — LA ALTERNATIVA MINERAL EN TODOS LOS
                        //     PELDAÑOS DE ARMAS (si el arma del peldaño no
                        //     llega, LA PILA DE LINGOTES del metal del
                        //     nivel paga lo mismo — siempre más de 50,
                        //     siempre creciente). v6.51.11 — CONTADOR
                        //     PARCIAL: el ejemplo de la letra (necesita 70,
                        //     comes 30, faltan 40). ===
                        if (!Systems.DespertarGrimorio.EsNivelMineral(siguiente))
                        {
                            int tipoAlt = Systems.DespertarGrimorio.MineralDe(siguiente);
                            int cantAlt = Systems.DespertarGrimorio.CantidadMineralDe(siguiente);
                            bool altOK = devoradaArma || comidosMineral >= cantAlt;
                            string colAlt = altOK ? colOK : colNo;
                            string nombreAlt = "—";
                            try
                            {
                                var protoAlt = Terraria.ID.ContentSamples.ItemsByType[tipoAlt];
                                if (protoAlt != null) nombreAlt = protoAlt.Name;
                            }
                            catch { }
                            var trozosAlt = new System.Collections.Generic.List<string>();
                            trozosAlt.Add($"[c/{colAlt}:  • O la ofrenda mineral:]");
                            trozosAlt.Add($"[i:{tipoAlt}]");
                            trozosAlt.Add($"[c/{colAlt}:{Math.Min(comidosMineral, cantAlt)}/{cantAlt} × {nombreAlt}{(altOK ? "  ✓" : "")}]");
                            tooltips.Add(new TooltipLine(Mod, "DespertarReq6",
                                string.Join(" ", trozosAlt)));
                        }

                        // === v6.51.11 — LA NOTA FINAL (la comida se sirve
                        //     EN EL SUELO — el gesto nuevo): con el arma O
                        //     los lingotes basta; suelta la ofrenda
                        //     DELANTE del grimorio SELECCIONADO y come SOLO
                        //     lo que necesita (nada del inventario — el
                        //     error de la mochila murió). ===
                        tooltips.Add(new TooltipLine(Mod, "DespertarReq7",
                            "[c/78788C:  (con el arma O los lingotes basta; suelta la ofrenda DELANTE del grimorio seleccionado — come solo lo que le falta y al completarla despierta)]"));
                    }
                    else
                    {
                        tooltips.Add(new TooltipLine(Mod, "DespertarMax",
                            "[c/C49CFF:  EL GRIMORIO DESPERTÓ DEL TODO (máximo por ahora)]"));
                    }

                    // === v6.51.9 — SOLO EL PRÓXIMO HITO (la letra: «el
                    //     despertar ocupa mucho espacio, solo debe salir
                    //     el proximo hito cuando se completo el anterior»):
                    //     la escalera COMPLETA de la .51.8 murió — vive UNA
                    //     SOLA LÍNEA, el próximo hito del camino; cuando el
                    //     libro lo completa, aparece el que sigue (y en el
                    //     50 ya no queda nada). ===
                    foreach (var paso in Systems.DespertarGrimorio.ElCaminoDelDespertar)
                    {
                        if (paso.Nivel <= despertar) continue;
                        tooltips.Add(new TooltipLine(Mod, "DespertarCamino",
                            "[c/78788C:  Próximo hito del despertar:] " +
                            $"[c/9B6FE8:Nivel {paso.Nivel}:] " +
                            $"[c/C4A2FF:{Language.GetTextValue(paso.Clave)}]"));
                        break;   // SOLO el próximo (la letra)
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

            // === EL CONMUTADOR (.110→.51.9): el clic derecho cicla las
            //     CUATRO ventanas — la línea dice SIEMPRE a cuál vas ===
            tooltips.Add(new TooltipLine(Mod, "ModeIndicator", vista switch
            {
                0 => "[c/78788C:═══ Click der para ver PROGRESIÓN ═══]",
                1 => "[c/78788C:═══ Click der para ver EL ARMA ═══]",
                2 => "[c/78788C:═══ Click der para ver EL ESCUADRÓN ═══]",
                _ => "[c/78788C:═══ Click der para vista básica ═══]",
            }));
        }
    }
}
