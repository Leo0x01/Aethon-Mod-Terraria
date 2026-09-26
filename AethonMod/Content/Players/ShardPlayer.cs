using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AethonMod.Content.Systems;
using AethonMod.Content.Globals;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Players
{
    public class ShardPlayer : ModPlayer
    {
        public int ResonanceShards = 0;
        public bool FirstLevelUpTriggered = false;

        // ================================================================
        //  v6.47 — LA VOZ DEL HAMBRE
        // ================================================================
        //
        // Con el libro a nivel alto, cada rato sin matar es un MOMENTO
        // DE HAMBRE: EcoLib susurra y la barra dorada paladece. Al cuarto
        // momento (~5 minutos), el libro pierde la paciencia y GRIMORIO
        // FURIOSO llama a su comida (GrimorioFuriaSistema — las oleadas).
        //
        // La hambre solo cuenta con el libro VISIBLE (barra rápida):
        // guardado, el libro DUERME (la hambre se congela, no se olvida).
        // Una kill del portador lo alimenta: hambre a cero.

        /// <summary>Ticks desde la última kill del portador (con libro visible).</summary>
        public int TicksSinMatar = 0;
        /// <summary>Momentos de hambre acumulados (0..10).</summary>
        public int MomentosHambre = 0;

        // ================================================================
        //  v6.48 — EL LIBRO CELOSO · LA CRÓNICA · EL JUICIO
        // ================================================================

        /// <summary>
        /// EL LIBRO CELOSO: el tipo del último ítem sostenido — si el
        /// portador EMPIÑA OTRA ARMA mientras el libro tiene hambre, el
        /// susurro celoso (“¿Eso también mata?”) según la CLASE del arma
        /// (melé, magia, invocación, arrojadiza, arco o bala). Pura voz:
        /// cero código de gameplay.
        /// </summary>
        private int _ultimaArmaSostenida = -1;
        /// <summary>El frío entre susurros de celos (no repite a cada swap).</summary>
        private int _ticksCelos = 0;

        /// <summary>
        /// LA CRÓNICA DEL TESTIGO: los jefes que EL LIBRO ha devorado con
        /// ESTE portador (los mata GlobalNPCXP al cobrar la kill). El
        /// Testigo lee esta lista para contar SU versión humana de cada
        /// derrota — dos narradores, un mismo hecho.
        /// </summary>
        public System.Collections.Generic.List<int> CronicaJefes = new System.Collections.Generic.List<int>();

        /// <summary>
        /// v6.48 — EL CURSOR DE LA CRÓNICA: cuántas derrotas ya CONTÓ el
        /// Testigo en su versión humana (GetChat consume de una en una:
        /// cada charla nueva revela la siguiente página del cuento).
        /// </summary>
        public int CronicaNarrada = 0;

        /// <summary>
        /// ¿El portador ya derrotó LA OLEADA 10 de la furia? El Testigo
        /// solo vende las ESENCIAS de los jefes (10 de platino) a quien
        /// ha sobrevivido al festín completo.
        /// </summary>
        public bool DerrotaOleada10 = false;

        /// <summary>Segundos por momento de hambre ("si pasas minutos sin matar").</summary>
        public const int SegundosPorMomento = 75;
        /// <summary>Momentos que tarden la furia (~5 minutos sin comer).</summary>
        public const int MomentosParaFuria = 4;
        /// <summary>El libro hambriento habla "a nivel alto" (25 = el segundo peldaño del Testigo).</summary>
        public const int NivelMinimoHambre = 25;
        /// <summary>Tope de momentos de hambre (= tope de oleadas).</summary>
        public const int MomentosMax = 10;

        /// <summary>
        /// v6.50.2 — LA SELECCIÓN DE LA CARNADA DEL REMOTO, vista por la
        /// AUTORIDAD: OleadasPreparadas (CarnadaDelGrimorio) es un estático
        /// POR MÁQUINA — en MP el clic derecho del remoto ciclaba SU
        /// contador local y el server ejecutaba el clic izquierdo (uso
        /// sincronizado) con SU propio default. EcoRed.MsgPrepararOleadas
        /// deposita aquí la selección para que el UseItem del server lea
        /// el número que el portador preparó.
        /// CICLO DE VIDA: vive la SESIÓN completa — NO se resetea en
        /// ResetEffects (se perdería a cada tick) ni al morir; NO se
        /// persiste en el .plr (la selección es un estado de la sesión,
        /// como el propio estático en SP). El default 0 = "nunca preparó"
        /// → el UseItem cae al estático local del server (SP/host).
        /// </summary>
        public int OleadasPreparadasRemoto;

        // ================================================================
        //  v6.46 — LOS TRES ESTADOS DEL LIBRO
        // ================================================================
        //
        // 1. SOSTENIDO: TODO el poder. El combate exige blandirlo.
        // 2. EN LA BARRA RÁPIDA (slots 0–9, sin sostener): el libro sigue
        //    COMIENDO XP (GlobalNPCXP), conserva los SLOTS DE MINION y la
        //    vida/maná extra — limitados a +100 cada uno.
        // 3. GUARDADO (inventario profundo, hucha/caja de seguridad/
        //    vaulta, cofre, suelo): NADA. Los minions YA invocados
        //    permanecen hasta que el jugador los desinvoque o mueran (el
        //    buff del orbe vive en el JUGADOR, no en el libro — vanilla no
        //    despawnea minions al bajar maxMinions), pero el libro no
        //    aporta nada hasta volver a la barra rápida.
        //
        // Las stats salen de la PRIMERA copia (sin stacking); la XP la
        // cobran TODAS las copias visibles. La detección se centraliza en
        // NivelLibro() para que todos los hooks cuenten la misma historia.

        /// <summary>
        /// El nivel del libro que manda para el jugador.
        /// soloSostenido = true → SOLO el sostenido (poder de combate).
        /// soloSostenido = false → sostenido, o la primera copia de la
        /// barra rápida (capacidades que no exigen blandir: slots, vida,
        /// maná topeado, y la propia XP que GlobalNPCXP cobra en 0–9).
        /// Devuelve 0 si el libro no está en ningún sitio que cuente.
        /// </summary>
        private int NivelLibro(bool soloSostenido)
        {
            Item held = Player.HeldItem;
            if (held != null && held.type == ModContent.ItemType<Weapons.GrimoireEternal>())
            {
                try
                {
                    var s = held.GetGlobalItem<ShardLevelItem>();
                    if (s != null) return s.Level;
                }
                catch { }
            }
            if (soloSostenido) return 0;

            for (int i = 0; i < 10; i++)
            {
                Item inv = Player.inventory[i];
                if (inv == null || inv.type != ModContent.ItemType<Weapons.GrimoireEternal>())
                    continue;
                try
                {
                    var s = inv.GetGlobalItem<ShardLevelItem>();
                    if (s != null) return s.Level;
                }
                catch { }
                break; // la PRIMERA copia de la barra rápida manda
            }
            return 0;
        }

        /// <summary>
        /// PostUpdateEquips: los bonuses que persisten mientras el libro
        /// esté SOSTENIDO o en la BARRA RÁPIDA. Corre tras ResetEffects:
        /// las stats aquí escritas cuentan de verdad.
        ///
        /// v6.45: el bonus de invocación, el crítico mágico y la
        /// penetración movidos desde GrimoireEternal.ModifyWeaponDamage
        /// (el sitio EQUIVOCADO: ese hook solo corre al calcular el daño
        /// del propio grimorio, así que los minions atacando en otros
        /// ticks no recibían nada — letra muerta de la clase R44).
        ///
        /// v6.46: la separación de estados — el COMBATE (daño de
        /// invocación, crítico, penetración) exige SOSTENER el libro; la
        /// capacidad (slots de minion) y los recursos (vida/maná, tope
        /// +100) sobreviven en la barra rápida.
        /// </summary>
        public override void PostUpdateEquips()
        {
            int nivel = NivelLibro(false);   // sostenido O barra rápida
            if (nivel <= 0) return;
            bool sostenido = NivelLibro(true) > 0;

            // === CAPACIDAD (sostener O barra rápida) ===

            // Slots de minion: base del juego + los del libro (nivel/10,
            // tope +10 al nivel 100). Se SUMA a lo que el resto del
            // equipamiento dé. Guardado el libro, la capacidad baja —
            // pero los minions YA invocados permanecen (vanilla no los
            // despawnea al bajar maxMinions: se van al desinvocarlos).
            Player.maxMinions += WeaponScaling.BonusMinionSlots(nivel);

            // === RECURSOS: completos al sostener, tope +100 en la barra ===
            int bonusVida = WeaponScaling.BonusLife(nivel);
            int bonusMana = WeaponScaling.BonusMana(nivel);
            if (!sostenido)
            {
                // v6.46: sin blandir el libro, el cuerpo solo tolera un
                // préstamo de +100 vida y +100 maná — el resto exige
                // sostenerlo.
                if (bonusVida > 100) bonusVida = 100;
                if (bonusMana > 100) bonusMana = 100;
            }
            Player.statLifeMax2 += bonusVida;
            Player.statManaMax2 += bonusMana;

            // === PODER DE COMBATE: exige SOSTENER el libro ===
            if (!sostenido) return;

            try
            {
                // +1% daño de invocación por nivel (Infinito)
                Player.GetDamage(DamageClass.Summon) += WeaponScaling.SummonDamageBonus(nivel);
                // +0.2% crítico mágico por nivel (Máximo 100%)
                Player.GetCritChance(DamageClass.Magic) += WeaponScaling.CritBonus(nivel);
                // Armor penetration +2% cada 5 niveles (Máximo 50%)
                Player.GetArmorPenetration(DamageClass.Magic) += WeaponScaling.ArmorPenBonus(nivel);
                // v6.45: la mitad de invocación del bonus por mana
                // faltante también es persistente (la mitad mágica
                // sigue evaluándose por golpe en ModifyWeaponDamage).
                Player.GetDamage(DamageClass.Summon) *=
                    WeaponScaling.LowManaDamageMult(Player.statMana, Player.statManaMax2);
            }
            catch { }
        }

        /// <summary>
        /// PostUpdateBuffs: regeneración de mana y vida.
        /// v6.44 (auditoría R44): EL HOOK CORRECTO. En el binario real,
        /// Player.Update consume lifeRegen/manaRegen en UpdateLifeRegen y
        /// UpdateManaRegen, que corren ANTES de PostUpdate — las escritas
        /// del hook viejo eran letra muerta (ResetEffects las borra al
        /// tick siguiente sin que nadie las lea). PostUpdateBuffs corre
        /// entre ResetEffects y el consumo: aquí sí cuentan.
        /// v6.46: la regeneración es poder del libro — SOLO sostenido.
        /// </summary>
        public override void PostUpdateBuffs()
        {
            int level = NivelLibro(true);
            if (level <= 0) return;

            // Regeneración de mana (por segundo)
            // v6.44 (auditoría R44): Player.manaRegen lo REESCRIBE
            // UpdateManaRegen desde cero cada tick — el campo que
            // ACUMULA aportes externos es manaRegenBonus. Unidades del
            // motor: 120 cuentas = 1 maná → para R maná/seg hay que
            // aportar 2·R cuentas por tick.
            int manaRegen = WeaponScaling.ManaRegen(level);
            if (manaRegen > 0 && Player.statMana < Player.statManaMax2)
            {
                Player.manaRegenBonus += manaRegen * 2;
            }

            // Regeneración de vida (por segundo)
            float lifeRegen = WeaponScaling.LifeRegen(level);
            if (lifeRegen > 0 && Player.statLife < Player.statLifeMax2)
            {
                Player.lifeRegen += (int)(lifeRegen * 2); // lifeRegen es en 1/2 vida/seg
            }
        }

        /// <summary>
        /// Modifica el daño recibido (reducción por nivel del Grimorio).
        /// v6.46: reducción de daño = poder del libro — SOLO sostenido.
        /// </summary>
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            int level = NivelLibro(true);
            if (level <= 0) return;

            float reduction = WeaponScaling.DamageReduction(level);
            if (reduction > 0f)
            {
                modifiers.FinalDamage *= 1f - reduction;
            }
        }

        public override void OnHurt(Player.HurtInfo info) { }

        /// <summary>
        /// v6.47: EL CORAZÓN DE LA VOZ DEL HAMBRE. Corre cada tick: cuenta
        /// el tiempo sin matar (solo con libro visible y nivel alto),
        /// susurra cada momento de hambre y cruza el umbral de la furia.
        ///
        /// v6.49 — EL RÉGIMEN AUTORITATIVO: la AUTORIDAD del hambre es el
        /// servidor (en SP, el propio proceso). El cliente MP NO cuenta
        /// sus propios momentos — recibe el estado por EcoRed
        /// (SincronizarHambre) para la barra palidecida, la ceniza del aura
        /// y los celos del libro. Así "la voz y el hambre son del
        /// portador": las siente ÉL, en SU pantalla, contadas UNA vez.
        /// </summary>
        public override void PostUpdate()
        {
            try
            {
                // v6.50.1 — FIX (EL HOST SORDO): en un listen server el host
                // es netMode Server PERO tiene pantalla y es Main.myPlayer —
                // el guard viejo lo excluía del Libro Celoso y del aura de
                // ceniza. Main.dedServ separa al host del dedicado.
                bool local = Player.whoAmI == Main.myPlayer &&
                    (Main.netMode != Terraria.ID.NetmodeID.Server || !Main.dedServ);
                bool autoridad = Main.netMode != Terraria.ID.NetmodeID.MultiplayerClient; // SP o server
                int nivel = NivelLibro(false);

                // v6.50 — LA RED DE SEGURIDAD: cada 10 s el server re-envía
                // libros y crónica al portador (cubre al que entra a mitad
                // de sesión, los movimientos de slot y cualquier deriva
                // que un paquete perdido hubiera dejado — la foto de
                // entrada se pide en OnEnterWorld; esto la repone).
                // v6.50.1: también el HAMBRE (si un MsgHambre se pierde, la
                // barra/aura quedaban desfasadas hasta el próximo cambio).
                // v6.50.2: MsgHambre SIEMPRE (lleva además el FESTÍN —
                // fase/oleada/total para el diagnóstico de furia del
                // cliente; el estado de un portador sin hambre es 0/0 y
                // no molesta).
                if (Main.netMode == Terraria.ID.NetmodeID.Server && Player.active &&
                    ((Main.GameUpdateCount + (ulong)Player.whoAmI * 37ul) % 600u) == 0ul)
                {
                    EcoRed.SincronizarLibros(Player);
                    EcoRed.SincronizarCronica(Player);
                    EcoRed.SincronizarHambre(Player);
                }

                if (autoridad)
                {
                    // v6.50.10 — FIX: UN MUERTO NO ALIMENTA EL LIBRO.
                    // La hambre crecía con el portador caído (5 min de
                    // respawn bastaban para los 10 momentos) y la furia
                    // PROVOCABA con él muerto — el festín nacía y moría al
                    // primer tick (VenganzaPorMuerte) con el churn de
                    // voces de ira/venganza encima del respawn. Ahora el
                    // hambre se CONGELA mientras yace (como guardado el
                    // libro: DUERME) y reanuda al levantarse.
                    if (!Player.dead && nivel >= NivelMinimoHambre)
                    {
                        TicksSinMatar++;
                        int momentos = Math.Min(TicksSinMatar / (60 * SegundosPorMomento), MomentosMax);

                        if (momentos > MomentosHambre)
                        {
                            MomentosHambre = momentos;

                            // v6.49 — el estado nuevo viaja al portador
                            // (su barra, su aura, sus celos — en SU cliente).
                            EcoRed.SincronizarHambre(Player);

                            // EL SUSURRO: la voz del libro en modo hambre — sin
                            // rugido, escala menuda, gris pálido. v6.49: la
                            // clave la reparte la AUTORIDAD y EcoRed la lleva
                            // SOLO al portador ("¿eso también mata?"… pero
                            // de hambre).
                            // v6.48 — EL SAJOR DEL BIOMA: la primer línea de cada
                            // momento cambia con el bioma ("carne de jungla",
                            // "sal del infierno"…): el libro sabe DÓNDE está
                            // hambriento. El resto del escalado sigue siendo el
                            // de la casa (1→4).
                            if (MomentosHambre == 1)
                            {
                                string claveSabor = EcoSistema.ClaveSusurroDelBioma(Player);
                                if (!string.IsNullOrEmpty(claveSabor))
                                    EcoRed.SusurrarAlPortador(Player, claveSabor,
                                        new Microsoft.Xna.Framework.Color(176, 172, 168));
                            }
                            else
                            {
                                EcoRed.SusurrarAlPortador(Player,
                                    "Mods.AethonMod.Eco.Hambre.Susurro" +
                                    Math.Min(MomentosHambre, 4),
                                    new Microsoft.Xna.Framework.Color(176, 172, 168));
                            }

                            // LA FURIA: al cuarto momento, el evento (si la config
                            // lo permite y el mundo está libre — un jefe o una
                            // invasión lo BLOQUEA y la hambre sigue subiendo hasta
                            // 10: por eso existen las 10 oleadas naturales).
                            if (MomentosHambre >= MomentosParaFuria)
                            {
                                // v6.50.2 — FIX (config ClientSide leída por la
                                // AUTORIDAD): EventoHambreGrimorio es una
                                // decisión del SERVER (el evento lo corre él) —
                                // vive en AethonConfigServidor (ServerSide):
                                // en dedicado manda la config del server, no
                                // la ilusión del cliente.
                                var config = ModContent.GetInstance<Content.AethonConfigServidor>();
                                bool evento = config == null || config.EventoHambreGrimorio;
                                if (evento && !GrimorioFuriaSistema.Activo && GrimorioFuriaSistema.MundoLibre())
                                    GrimorioFuriaSistema.Provocar(Player, MomentosHambre);
                            }
                        }
                    }
                    // guardado el libro: DUERME — la hambre se congela.
                }

                // EL LIBRO CELOSO y EL AURA corren en el CLIENTE del
                // portador (percepción local: lo que ÉL sostiene y viste).
                if (local) ElLibroCeloso(nivel);

                // EL AURA DEL HAMBRE: la ceniza del libro sobre su portador
                // — v6.48 por el PORTADOR (AuraPortadorHalo, el camino
                // aditivo del halo-proyectil: el neón de verdad detrás del
                // cuerpo). Las partículas corren en la AI del portador.
                if (local && MomentosHambre > 0 && !Player.dead)
                {
                    if (!EspiarPortador(0))
                    {
                        Projectile.NewProjectile(Player.GetSource_Misc("AuraHambre"),
                            Player.Center, Vector2.Zero,
                            ModContent.ProjectileType<Projectiles.Cosmetic.AuraPortadorHalo>(),
                            0, 0f, Player.whoAmI, 0f);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// v6.48 — EL LIBRO CELOSO: si el portador cambia a OTRA ARMA
        /// mientras el libro está hambriento, el libro susurra su celos —
        /// una muestra DISTINTA según la CLASE del arma sostenida (melé,
        /// magia, invocación, arrojadiza, y la distancia repartida entre
        /// ARCO y BALA, como pidió el usuario). PURA VOZ: cero mecánica,
        /// cero números — el libro solo opina. Frío de 8 s para no cantar
        /// a cada swap de hotbar.
        /// </summary>
        private void ElLibroCeloso(int nivelLibro)
        {
            if (_ticksCelos > 0) _ticksCelos--;

            Item sostenido = Player.HeldItem;
            int tipo = sostenido != null ? sostenido.type : -1;
            if (tipo == _ultimaArmaSostenida) return; // mismo ítem: nada nuevo
            _ultimaArmaSostenida = tipo;

            // solo con hambre de verdad (la primer boca cuenta) y solo
            // con ARMAS (daño > 0, no el propio libro, no herramientas).
            if (MomentosHambre <= 0 || nivelLibro <= 0) return;
            if (sostenido == null || sostenido.IsAir || sostenido.damage <= 0) return;
            if (tipo == ModContent.ItemType<Weapons.GrimoireEternal>()) return;
            if (_ticksCelos > 0) return;

            // LA CLASE DEL ARMA CELADA (con arco y bala separados).
            string pool;
            var dt = sostenido.DamageType;
            if (dt == DamageClass.Melee || dt == DamageClass.MeleeNoSpeed) pool = "Melee";
            else if (dt == DamageClass.Magic || dt == DamageClass.MagicSummonHybrid) pool = "Magia";
            else if (dt == DamageClass.Summon || dt == DamageClass.SummonMeleeSpeed) pool = "Invocacion";
            else if (dt == DamageClass.Throwing) pool = "Arrojadiza";
            else if (dt == DamageClass.Ranged)
            {
                if (sostenido.useAmmo == Terraria.ID.AmmoID.Arrow) pool = "Arco";
                else if (sostenido.useAmmo == Terraria.ID.AmmoID.Bullet) pool = "Bala";
                else pool = "Arco"; // cerbatanas/dardos: el arco cubre
            }
            else return; // sin clase clara: el libro calla

            _ticksCelos = 480; // 8 s de frío
            string texto = EcoLib.ElegirVariante("Mods.AethonMod.Eco.Celos." + pool, 3);
            if (!string.IsNullOrEmpty(texto))
                EcoLib.Hablar(texto,
                    new Microsoft.Xna.Framework.Color(196, 116, 108),
                    rugido: false, escala: 0.52f);
        }

        /// <summary>¿Ya vive mi portador de aura con este modo?</summary>
        private bool EspiarPortador(int modo)
        {
            int tipo = ModContent.ProjectileType<Projectiles.Cosmetic.AuraPortadorHalo>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == Player.whoAmI && p.type == tipo &&
                    (int)p.ai[0] == modo)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// v6.48 — LA CRÓNICA: apunta que EL LIBRO devoró a este jefe con
        /// este portador (lo llama GlobalNPCXP al cobrar la kill — antes
        /// de la voz). El Testigo contará su versión humana.
        /// v6.50.3 — devuelve TRUE solo si la página es NUEVA (el llamador
        /// sincroniza por EcoRed únicamente entonces: antes el Devorador
        /// disparaba ~80 MsgCronica idénticos por una sola derrota).
        /// </summary>
        public bool CronicaMarcar(int npcType)
        {
            if (npcType <= 0) return false;
            if (CronicaJefes.Contains(npcType)) return false;
            CronicaJefes.Add(npcType);
            return true;
        }

        /// <summary>
        /// El portador mató algo (lo llama GlobalNPCXP cuando el libro de
        /// su barra rápida ha comido): la hambre se perdona — la barra
        /// dorada recupera su color y los susurros callan. v6.49: el
        /// estado nuevo viaja al portador (EcoRed) — en MP la autoridad
        /// la perdona y el cliente del portador la VE perdonada.
        /// v6.50.2 — FIX (bandwidth): SOLO cuando el hambre CAMBIA — el
        /// kill que no toca el estado (hambre ya en 0, la mayoría en
        /// combate) ya no dispara un paquete por kill.
        /// </summary>
        public void RegistrarKill()
        {
            TicksSinMatar = 0;
            bool cambio = MomentosHambre != 0;
            MomentosHambre = 0;
            if (cambio)
                EcoRed.SincronizarHambre(Player); // no-op fuera del servidor
        }

        // === EL PERFIL DEL AURA DE HAMBRE (cacheado: cero GC por frame) ===
        // v6.50.3 — FIX (cache ESTÁTICO en un ModPlayer): los campos static
        // se comparten entre TODAS las instancias — en SP (un jugador) la
        // caché acertaba por casualidad, pero en el SERVER de MP el hook
        // corre por cada portador: dos hambres distintas = MISS por frame
        // por jugador (la promesa "cero GC" moría justo donde hay más
        // jugadores). Campos de INSTANCIA: una caché POR JUGADOR, como
        // siempre debió ser.
        private AuraPerfil _auraHambre;
        private int _auraHambreIntensidad = -1;

        /// <summary>El perfil del hambre a la intensidad actual (creado SOLO cuando cambia).</summary>
        private AuraPerfil AuraHambre()
        {
            if (_auraHambre == null || _auraHambreIntensidad != MomentosHambre)
            {
                _auraHambre = AuraPerfil.HambreDelGrimorio(MomentosHambre);
                _auraHambreIntensidad = MomentosHambre;
            }
            return _auraHambre;
        }

        /// <summary>El perfil cacheado del aura de hambre (las capas de jugador lo dibujan).</summary>
        public AuraPerfil AuraHambrePublica() => AuraHambre();

        /// <summary>
        /// El festín terminó y el libro está saciado: la hambre del
        /// portador se perdona por completo (GrimorioFuriaSistema.Fin).
        /// v6.49: el estado viaja por EcoRed al portador.
        /// </summary>
        public void PerdonarHambre()
        {
            TicksSinMatar = 0;
            MomentosHambre = 0;
            EcoRed.SincronizarHambre(Player); // no-op fuera del servidor
        }

        public override void OnEnterWorld()
        {
            // v6.50 — LA FOTO DE ENTRADA: el cliente MP recién llegado pide
            // SUS libros (nivel/XP), SU crónica y SU hambre — vanilla
            // sincroniza el inventario, pero no los datos de GlobalItem ni
            // del ModPlayer. En SP no hay nada que pedir.
            EcoRed.PedirMisLibros();
        }

        public override void SaveData(TagCompound tag)
        {
            // v6.50.15 — ARMADURA (auditoría R55-c): tML captura las
            // excepciones del SaveData y escribe un tag "error", pero el
            // cuerpo es gratis de blindar — el patrón de la casa.
            try
            {
                tag["resonanceShards"] = ResonanceShards;
                tag["firstLevelUpTriggered"] = FirstLevelUpTriggered;
                // v6.48 — la crónica del Testigo y el derecho a las esencias.
                tag["cronicaJefes"] = CronicaJefes;
                tag["cronicaNarrada"] = CronicaNarrada;
                tag["derrotaOleada10"] = DerrotaOleada10;
            }
            catch { }
        }

        public override void LoadData(TagCompound tag)
        {
            try
            {
                ResonanceShards = tag.GetInt("resonanceShards");
                FirstLevelUpTriggered = tag.GetBool("firstLevelUpTriggered");
                CronicaJefes = new System.Collections.Generic.List<int>(tag.GetList<int>("cronicaJefes"));
                CronicaNarrada = tag.GetInt("cronicaNarrada");
                DerrotaOleada10 = tag.GetBool("derrotaOleada10");
            }
            catch { }
        }
    }
}
