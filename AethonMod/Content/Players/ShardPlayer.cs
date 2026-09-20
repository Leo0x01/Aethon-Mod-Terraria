using System;
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

        /// <summary>Segundos por momento de hambre ("si pasas minutos sin matar").</summary>
        public const int SegundosPorMomento = 75;
        /// <summary>Momentos que tarden la furia (~5 minutos sin comer).</summary>
        public const int MomentosParaFuria = 4;
        /// <summary>El libro hambriento habla "a nivel alto" (25 = el segundo peldaño del Testigo).</summary>
        public const int NivelMinimoHambre = 25;
        /// <summary>Tope de momentos de hambre (= tope de oleadas).</summary>
        public const int MomentosMax = 10;

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
        /// </summary>
        public override void PostUpdate()
        {
            try
            {
                bool local = Player.whoAmI == Main.myPlayer && Main.netMode != Terraria.ID.NetmodeID.Server;
                int nivel = NivelLibro(false);

                if (nivel >= NivelMinimoHambre)
                {
                    TicksSinMatar++;
                    int momentos = Math.Min(TicksSinMatar / (60 * SegundosPorMomento), MomentosMax);

                    if (momentos > MomentosHambre)
                    {
                        MomentosHambre = momentos;

                        // EL SUSURRO: la voz del libro en modo hambre — sin
                        // rugido, escala menuda, gris pálido. Solo la oye el
                        // portador (SP-first).
                        if (local)
                        {
                            string clave = "Mods.AethonMod.Eco.Hambre.Susurro" +
                                           Math.Min(MomentosHambre, 4);
                            EcoLib.Hablar(
                                Terraria.Localization.Language.GetTextValue(clave),
                                new Microsoft.Xna.Framework.Color(176, 172, 168),
                                rugido: false, escala: 0.52f);
                        }

                        // LA FURIA: al cuarto momento, el evento (si la config
                        // lo permite y el mundo está libre — un jefe o una
                        // invasión lo BLOQUEA y la hambre sigue subiendo hasta
                        // 10: por eso existen las 10 oleadas naturales).
                        if (MomentosHambre >= MomentosParaFuria &&
                            Main.netMode != Terraria.ID.NetmodeID.MultiplayerClient)
                        {
                            var config = ModContent.GetInstance<Content.AethonConfig>();
                            bool evento = config == null || config.EventoHambreGrimorio;
                            if (evento && !GrimorioFuriaSistema.Activo && GrimorioFuriaSistema.MundoLibre())
                                GrimorioFuriaSistema.Provocar(Player, MomentosHambre);
                        }
                    }
                }
                // guardado el libro: DUERME — la hambre se congela.

                // EL AURA DEL HAMBRE: la ceniza del libro sobre su portador
                // (AuraLib por las capas de jugador — solo el local la viste).
                if (local && MomentosHambre > 0 && !Player.dead)
                    AuraLib.ActualizarJugador(Player, AuraHambre());
            }
            catch { }
        }

        /// <summary>
        /// El portador mató algo (lo llama GlobalNPCXP cuando el libro de
        /// su barra rápida ha comido): la hambre se perdona — la barra
        /// dorada recupera su color y los susurros callan.
        /// </summary>
        public void RegistrarKill()
        {
            TicksSinMatar = 0;
            MomentosHambre = 0;
        }

        // === EL PERFIL DEL AURA DE HAMBRE (cacheado: cero GC por frame) ===
        private static AuraPerfil _auraHambre;
        private static int _auraHambreIntensidad = -1;

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
        /// </summary>
        public void PerdonarHambre()
        {
            TicksSinMatar = 0;
            MomentosHambre = 0;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["resonanceShards"] = ResonanceShards;
            tag["firstLevelUpTriggered"] = FirstLevelUpTriggered;
        }

        public override void LoadData(TagCompound tag)
        {
            try
            {
                ResonanceShards = tag.GetInt("resonanceShards");
                FirstLevelUpTriggered = tag.GetBool("firstLevelUpTriggered");
            }
            catch { }
        }
    }
}
