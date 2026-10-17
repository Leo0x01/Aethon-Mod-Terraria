using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Effects.Bruma;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  LA BRUMA DEVORADORA (v6.50.128 · v6.50.129) — LA MECÁNICA DE COMER
    //  ITEMS Y ARMAS.
    //
    //  LA LETRA DEL USUARIO (v6.50.129): «la mecanica para que el
    //  grimorio devore un item en este caso un arma de cualquier tipo:
    //  el crater debe tener menos profundidad, el centro debe estar
    //  vacio solo obsidiana y ademas bruma, que sea como un pequeño
    //  minibioma, entonces cualquier arma que caiga ahi o directamente
    //  desde el menu de creacion, se pueden corromper con la estadistica
    //  corrupto, al ser corrompida ya no puede tener etiquetas como
    //  superior, mitica, rota y cualquier otra etiqueta y el duende debe
    //  decir algo como esta arma esta corrompida no puedo hacer nada con
    //  ella. cuualquier arma corrompida se vuelve exp para el grimorio a
    //  mayor la rareza del arma mayor los exp. ademas cada arma debe
    //  subir algun stats del grimorio ya sea un stats unico o no».
    //
    //  EL CIRCUITO (la zona es PERMANENTE — vive en el cráter aunque el
    //  libro dormido ya se haya ido; LA PUERTA ES LA BRUMA, no un
    //  bloque — el altar dorado de la .128 murió por la letra):
    //   1. EL ARMA CAE A LA CONCHA (o nace ahí desde el menú de creación
    //      de Journey — la zona es un LUGAR: no le importa de dónde
    //      vino el item): 1 s en la bruma y queda CORRUPTA (la
    //      estadística: el prefijo PELADO con Prefix(-3) — «ya no puede
    //      tener etiquetas» — el sello CorruptoGlobalItem que viaja con
    //      ella para siempre, el yunque cerrado y el duende confesando).
    //   2. LA ESPERA EN LA BRUMA: 4 s más pudriéndose… y si un portador
    //      del Eterno anda cerca (<40 tiles), LA BRUMA SE LA TRAGA:
    //      el arma muere en todas las pantallas y el Eterno crece —
    //      EXP POR RAREZA (la tabla) + UN STAT POR CLASE (melee→daño ·
    //      ranged→velocidad · magic→crítico · summon→ojos · otra→empuje)
    //      + EL ALMA ÚNICA si era especial (fuego/veneno/vampiro/
    //      estrellas/velocidad/peso: ShardPlayer).
    //   3. LOS ITEMS NORMALES siguen el camino de la .128: 2,5 s
    //      pudriéndose → ESENCIAS CORRUPTAS (tantas como valían) → el
    //      trago del portador → +daño permanente (el poder devorado).
    //
    //  LA BRUMA (la letra: «solo obsidiana y ademas bruma, que sea como
    //  un pequeño minibioma»): columnas de humo fBn (BrumaFX — el humo
    //  REAL de la casa, negro→violeta) naciendo de la concha y
    //  respirando, el resplandor violeta del piso para leerla de noche…
    //  y cuando come, EL VÓRTICE: la bruma gira y se traga la mesa. Sin
    //  haz dorado, sin runas de oro, sin ojo ni boca: SOLO BRUMA.
    // ======================================================================
    public class AltarDevoradorSistema : ModSystem
    {
        // === EL RITMO DEL RITUAL ===
        /// <summary>Ticks de item normal pudriéndose antes de ser esencia (2,5 s).</summary>
        private const int TICKS_CORROMPER = 150;
        /// <summary>Ticks de ARMA en la bruma antes del sello corrupto (1 s).</summary>
        private const int TICKS_CORROMPER_ARMA = 60;
        /// <summary>Ticks de arma corrupta esperando el trago (4 s — da tiempo a rescatarla).</summary>
        private const int TICKS_DEVORAR_ARMA = 240;
        /// <summary>Ticks de cáscara antes del trago (~1 s).</summary>
        private const int TICKS_TRAGAR = 55;
        /// <summary>El radio de la zona (en tiles) — el cuenco abierto del minibioma.</summary>
        private const int RADIO_TILES = 5;
        /// <summary>El alcance del portador del Eterno para comer (en px).</summary>
        private const float ALCANCE_PORTADOR = 40f * 16f;

        /// <summary>Los items pudriéndose (item.whoAmI → ticks). Solo AUTORIDAD.</summary>
        private readonly Dictionary<int, int> _corrompiendo = new();
        /// <summary>Las armas ganándose el sello corrupto. Solo AUTORIDAD.</summary>
        private readonly Dictionary<int, int> _corrompiendoArmas = new();
        /// <summary>Las armas corruptas esperando el trago. Solo AUTORIDAD.</summary>
        private readonly Dictionary<int, int> _armasEsperando = new();
        /// <summary>Las cáscaras esperando su trago. Solo AUTORIDAD.</summary>
        private readonly Dictionary<int, int> _cascaras = new();

        // === EL MAPA LOCAL (la cosmética en TODAS las máquinas):
        //     SP/servidor = la lista del mundo (LaCaidaSistema, copiada
        //     cada segundo — la autoridad la acaba de escribir); cliente
        //     MP = la lista SINCRONIZADA (EcoRed.MsgAltarZonas: la foto
        //     de entrada + el difundido de cada caída nueva). La .128
        //     escaneaba la TILE DORADA — la tile murió con el altar. ===
        private class ZonaLocal
        {
            public int X;
            public int Y;
        }

        private readonly List<ZonaLocal> _zonasLocales = new();
        private readonly List<ZonaLocal> _zonasSincronizadas = new();
        private ulong _ultimoRefresco;

        // === EL FOCO VISUAL (cosmética local — cada pantalla la suya) ===
        /// <summary>¿Hay algo corrompiéndose en la bruma AHORA? (por zona).</summary>
        private static readonly HashSet<int> _zonasActivas = new();
        /// <summary>La zona que está comiendo (el vórtice).</summary>
        private static int _zonaComiendo = -1;
        /// <summary>El reloj local del vórtice (0..1 intensidad).</summary>
        private static float _vortice;

        // ================================================================
        //  LA LÓGICA (cada 10 ticks — el pulso de la casa)
        // ================================================================

        public override void PreUpdateTime()
        {
            try
            {
                if (Main.gameMenu) return;

                _zonasActivas.Clear();

                // === EL REFRESCO DEL MAPA LOCAL ===
                if (Main.GameUpdateCount - _ultimoRefresco >= 60ul)
                {
                    _ultimoRefresco = Main.GameUpdateCount;
                    RefrescarZonasLocales();
                }

                // === LA COSMÉTICA (TODAS las máquinas con pantalla): la
                //     succión del vórtice y el polvo de la corrupción. ===
                if (!Main.dedServ && _zonasLocales.Count > 0) CosmeticaDeLaBruma();

                // === EL RITUAL (solo AUTORIDAD — SU lista del mundo) ===
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                if ((Main.GameUpdateCount % 10ul) != 0ul) return;

                var sysCaida = ModContent.GetInstance<LaCaidaSistema>();
                if (sysCaida == null || sysCaida.Altares.Count == 0) return;
                var zonas = sysCaida.Altares;

                var muertos = new List<int>();

                foreach (var zona in zonas)
                {
                    if (zona == null) continue;
                    Vector2 centro = new Vector2(zona.X * 16f + 8f, zona.Y * 16f + 8f);
                    Player portador = BuscarPortador(centro);

                    for (int i = 0; i < Main.maxItems; i++)
                    {
                        Item drop = Main.item[i];
                        if (drop == null || !drop.active || drop.IsAir || drop.stack <= 0) continue;
                        if (!EnLaZona(drop, centro)) continue;

                        int tipoHusos = ModContent.ItemType<global::AethonMod.Content.Items.Altar.ItemCorrupto>();

                        // ===== LA CÁSCARA: cuenta para el trago =====
                        if (drop.type == tipoHusos)
                        {
                            // La bruma PRESERVA sus ofrendas (el reloj de
                            // despawn de vanilla jamás las alcanza).
                            drop.timeSinceItemSpawned = 0;
                            _cascaras[i] = _cascaras.TryGetValue(i, out int tCascara) ? tCascara + 10 : 10;

                            if (_cascaras[i] >= TICKS_TRAGAR && portador != null)
                            {
                                Tragar(zona, centro, i, portador, muertos);
                            }
                            continue;
                        }

                        // ===== LO QUE NO SE COME: el propio Eterno jamás =====
                        if (EsSagrado(drop)) { muertos.Add(i); continue; }

                        // ===== v6.50.129 — LAS ARMAS: EL CAMINO CORRUPTO =====
                        if (EsArma(drop))
                        {
                            var sello = drop.GetGlobalItem<global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                            if (sello != null && !sello.Corrompido)
                            {
                                // FASE 1 — EL SELLO: 1 s en la bruma y queda
                                // CORRUPTA (sin etiquetas, con el aura).
                                _corrompiendoArmas[i] = _corrompiendoArmas.TryGetValue(i, out int tA)
                                    ? tA + 10 : 10;
                                drop.timeSinceItemSpawned = 0;   // la bruma la preserva
                                if (_corrompiendoArmas[i] >= TICKS_CORROMPER_ARMA)
                                {
                                    if (CorromperArma(zona, i, drop, muertos))
                                        _armasEsperando[i] = 0;   // pasa a la mesa de espera
                                }
                            }
                            else
                            {
                                // FASE 2 — LA ESPERA: corrupta y pudriéndose…
                                // el trago llega con el portador cerca (la
                                // ventana de 4 s da tiempo a RESCATARLA: el
                                // arma corrupta es un arma — la letra del
                                // duende existe porque se PUEDE llevar).
                                drop.timeSinceItemSpawned = 0;
                                _armasEsperando[i] = _armasEsperando.TryGetValue(i, out int tE)
                                    ? tE + 10 : 10;
                                if (_armasEsperando[i] >= TICKS_DEVORAR_ARMA && portador != null)
                                {
                                    DevorarArma(zona, centro, i, drop, portador, muertos);
                                }
                            }
                            continue;
                        }

                        // ===== EL ITEM NORMAL: se pudre en esencias =====
                        _corrompiendo[i] = _corrompiendo.TryGetValue(i, out int t) ? t + 10 : 10;
                        drop.timeSinceItemSpawned = 0;
                        if (_corrompiendo[i] >= TICKS_CORROMPER)
                        {
                            Corromper(zona, i, drop, muertos);
                        }
                    }
                }

                // LA LIMPIEZA: los diccionarios no guardan fantasmas.
                foreach (int id in muertos)
                {
                    _corrompiendo.Remove(id);
                    _corrompiendoArmas.Remove(id);
                    _armasEsperando.Remove(id);
                    _cascaras.Remove(id);
                }
                LimpiezaDeDiccionarios();
            }
            catch { }
        }

        /// <summary>EL MAPA LOCAL: SP/servidor copia la lista del mundo (la
        /// autoridad la acaba de escribir); el cliente MP vive de la foto
        /// sincronizada (EcoRed.MsgAltarZonas — la .128 escaneaba la tile
        /// dorada: murió con el altar).</summary>
        private void RefrescarZonasLocales()
        {
            try
            {
                _zonasLocales.Clear();
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    // EL CLIENTE: la lista sincronizada (vació = sin zonas).
                    foreach (var z in _zonasSincronizadas)
                        if (z != null) _zonasLocales.Add(z);
                    return;
                }
                // SP / SERVIDOR: la lista del mundo, tal cual.
                var sys = ModContent.GetInstance<LaCaidaSistema>();
                if (sys == null) return;
                foreach (var a in sys.Altares)
                {
                    if (a == null) continue;
                    _zonasLocales.Add(new ZonaLocal { X = a.X, Y = a.Y });
                }
            }
            catch { }
        }

        /// <summary>v6.50.129 — LA FOTO SINCRONIZADA (EcoRed.MsgAltarZonas,
        /// solo clientes MP): las zonas del mundo según la autoridad.</summary>
        public static void RecibirZonas(List<int[]> pares)
        {
            try
            {
                var sys = ModContent.GetInstance<AltarDevoradorSistema>();
                if (sys == null || pares == null) return;
                sys._zonasSincronizadas.Clear();
                foreach (var par in pares)
                {
                    if (par == null || par.Length < 2) continue;
                    sys._zonasSincronizadas.Add(new ZonaLocal { X = par[0], Y = par[1] });
                }
                sys._ultimoRefresco = 0;   // al próximo tick el mapa local se viste
            }
            catch { }
        }

        /// <summary>¿Este drop está en la bruma del minibioma? (el cuenco
        /// abierto: ±5 tiles de la concha, desde el borde hasta el piso).</summary>
        private static bool EnLaZona(Item drop, Vector2 centro)
        {
            float dx = MathF.Abs(drop.Center.X - centro.X);
            float dy = drop.Center.Y - centro.Y;
            return dx <= RADIO_TILES * 16f && dy >= -170f && dy <= 40f;
        }

        /// <summary>v6.50.129 — ¿Es un ARMA? («un arma de cualquier tipo»):
        /// daño > 0, sin pico/hacha/martillo (las herramientas no son
        /// armas), sin accesorios ni munición pura — melee, ranged,
        /// magic, summon y lanzables.</summary>
        private static bool EsArma(Item it)
        {
            return it != null && !it.IsAir && it.damage > 0 && !it.accessory &&
                !it.vanity && it.pick == 0 && it.axe == 0 && it.hammer == 0 &&
                it.ammo == 0 && it.type !=
                ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>();
        }

        /// <summary>LO QUE LA BRUMA NO TOCA: el Grimorio del Eterno (¡el
        /// comensal no es plato!).</summary>
        private static bool EsSagrado(Item drop)
        {
            int eterno = ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>();
            return drop.type == eterno;
        }

        /// <summary>EL PORTADOR MÁS CERCANO con un Grimorio del Eterno en el inventario.</summary>
        private static Player BuscarPortador(Vector2 centro)
        {
            Player mejor = null;
            float mejorD = ALCANCE_PORTADOR * ALCANCE_PORTADOR;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p == null || !p.active || p.dead) continue;
                float d = p.Center.DistanceSQ(centro);
                if (d > mejorD) continue;
                if (!LaCaidaSistema.TieneGrimorio(p)) continue;
                mejorD = d;
                mejor = p;
            }
            return mejor;
        }

        // ================================================================
        //  v6.50.129 — EL SELLO CORRUPTO (las armas)
        // ================================================================

        /// <summary>EL SELLO: el arma cae en la bruma y sale CORRUPTA — la
        /// estadística de la letra: el prefijo PELADO (Prefix(-3): «ya no
        /// puede tener etiquetas como superior, mitica, rota y cualquier
        /// otra»), el sello que viaja con ella para siempre (CorruptoGlobal
        /// Item) y el aviso. El arma SIGUE siendo un arma (sus stats
        /// intactos): solo está MARCADA — y marcada es comida.</summary>
        private static bool CorromperArma(LaCaidaSistema.AltarDelMundo zona, int id,
            Item drop, List<int> muertos)
        {
            try
            {
                var sello = drop.GetGlobalItem<global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                if (sello != null) sello.Corrompido = true;

                // SIN ETIQUETAS (la letra): el prefijo muere AHORA — jamás
                // vuelve (el yunque le está cerrado: CanReforge).
                // (ResetPrefix = pelado LIMPIO: Prefix(-3) RIFARÍA un
                // prefijo nuevo — verificado contra el Item descompilado.)
                drop.ResetPrefix();
                drop.noGrabDelay = 30;
                drop.velocity *= 0.3f;

                // LA DIFUSIÓN (todas las pantallas ven el sello caer).
                if (Main.netMode == NetmodeID.Server)
                    Terraria.NetMessage.SendData(MessageID.SyncItem, -1, -1, null, id);
                muertos.Add(id);

                // EL AVISO al que esté cerca (la bruma acaba de morder).
                Player testigo = BuscarPortador(zona.Posicion()) ??
                    JugadorMasCerca(zona.Posicion(), 35f * 16f);
                if (testigo != null)
                {
                    EcoRed.AnunciarAlPortador(testigo, "Mods.AethonMod.Caida.ArmaCorrompidaAviso",
                        new Color(150, 60, 220), drop.Name);
                }

                // EL POLVO DEL SELLO (la máquina con pantalla).
                if (!Main.dedServ)
                {
                    for (int k = 0; k < 14; k++)
                    {
                        Dust d = Dust.NewDustPerfect(drop.Center, DustID.Shadowflame,
                            new Vector2(Main.rand.NextFloat(-1.6f, 1.6f),
                                Main.rand.NextFloat(-2.4f, -0.2f)), 150,
                            new Color(120, 40, 180), 0.9f);
                        d.noGravity = true;
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103.WithPitchOffset(-0.15f),
                        drop.Center);
                }
                return true;
            }
            catch { return false; }
        }

        // ================================================================
        //  v6.50.129 — LA TABLA DEL EXP (la letra: «a mayor la rareza del
        //  arma mayor los exp») Y LOS STATS POR CLASE / LAS ALMAS ÚNICAS
        // ================================================================

        /// <summary>EL EXP DE UN ARMA por su rareza (la letra: «a mayor la
        /// rareza del arma mayor los exp») — la escala de la casa: la
        /// basura alimenta poco, la mítica es un banquete.</summary>
        private static long ExpPorRareza(int rareza)
        {
            switch (rareza)
            {
                case -1: return 5;        // gris: la pobreza también alimenta
                case 0: return 10;        // blanca
                case 1: return 20;        // azul
                case 2: return 45;        // verde
                case 3: return 100;       // naranja
                case 4: return 250;       // rosa
                case 5: return 600;       // rojo claro
                case 6: return 1500;      // rojo (pre-hardmode alto)
                case 7: return 4000;      // rosa fuerte / experto
                case 8: return 8000;      // exclusivo experto
                case 9: return 8000;      // exclusivo maestro
                default: return rareza >= 10 ? 15000 : 10;   // quest/mística
            }
        }

        /// <summary>EL STAT QUE ESTA ARMA SUBE («cada arma debe subir algun
        /// stats del grimorio ya sea un stats unico o no»): por CLASE de
        /// daño — melee→Fuerza (daño) · ranged→Prisa (velocidad) ·
        /// magic→Mirada (crítico) · summon→Corona (ojos) · otra→Mordida
        /// (empuje). Devuelve la CLAVE del nombre para el aviso.</summary>
        private static string SubirStatPorClase(Players.ShardPlayer sp, Item arma)
        {
            if (arma.DamageType == DamageClass.Melee) { sp.DevoracionFuerza++; return "StatFuerza"; }
            if (arma.DamageType == DamageClass.Ranged) { sp.DevoracionPrisa++; return "StatPrisa"; }
            if (arma.DamageType == DamageClass.Magic) { sp.DevoracionMirada++; return "StatMirada"; }
            if (arma.DamageType == DamageClass.Summon) { sp.DevoracionCorona++; return "StatCorona"; }
            sp.DevoracionMordida++;
            return "StatMordida";
        }

        /// <summary>EL ALMA ÚNICA («puedes crear stats y efectos para las
        /// armas en caso de que estas tengan efectos o sean especiales»):
        /// las armas CON EFECTO despiertan SU alma en el libro — fuego,
        /// veneno, vampiro, estrellas, velocidad, peso. Devuelve la clave
        /// del alma (o null si era un arma común).</summary>
        private static string DespertarAlma(Players.ShardPlayer sp, Item arma)
        {
            int t = arma.type;
            // ALMA ÍGNEA — las armas de fuego.
            if (t == ItemID.FieryGreatsword || t == ItemID.Sunfury ||
                t == ItemID.Flamelash || t == ItemID.InfernoFork ||
                t == ItemID.Flamethrower || t == ItemID.VolcanoSmall ||
                t == ItemID.VolcanoLarge)
            { sp.AlmaIgnea++; return "AlmaIgnea"; }
            // ALMA SALVAJE — las armas de veneno.
            if (t == ItemID.BladeofGrass || t == ItemID.ThornChakram ||
                t == ItemID.BeeGun || t == ItemID.Beenade || t == ItemID.PoisonStaff)
            { sp.AlmaSalvaje++; return "AlmaSalvaje"; }
            // ALMA VELOZ — las hojas rápidas.
            if (t == ItemID.Muramasa || t == ItemID.Katana || t == ItemID.FetidBaghnakhs)
            { sp.AlmaVeloz++; return "AlmaVeloz"; }
            // CORAZÓN ESTELAR — las armas de estrellas.
            if (t == ItemID.Starfury || t == ItemID.StarWrath || t == ItemID.StarCannon)
            { sp.CorazonEstelar++; return "CorazonEstelar"; }
            // ALMA VAMPIRA — el robo de vida.
            if (t == ItemID.VampireKnives)
            { sp.AlmaVampira++; return "AlmaVampira"; }
            // PUÑO TITÁN — el peso.
            if (t == ItemID.PaladinsHammer || t == ItemID.GolemFist)
            { sp.PunoTitan++; return "PunoTitan"; }
            return null;
        }

        /// <summary>EL TRAGO DE LA BRUMA: el arma corrupta muere en todas
        /// las pantallas y el Eterno crece PARA SIEMPRE — EXP por rareza +
        /// un stat por clase + el alma única si era especial. La VOZ
        /// cuenta la mejora y el aviso desglosa el plato.</summary>
        private static void DevorarArma(LaCaidaSistema.AltarDelMundo zona, Vector2 centro,
            int id, Item drop, Player portador, List<int> muertos)
        {
            try
            {
                var sp = portador.GetModPlayer<Players.ShardPlayer>();
                if (sp == null) return;

                // EL PLATO: EXP (por rareza, por unidad del stack) + stat + alma.
                long exp = ExpPorRareza(drop.rare) * Math.Max(1, drop.stack);
                sp.DevoracionExp += exp;
                string statKey = SubirStatPorClase(sp, drop);
                string almaKey = DespertarAlma(sp, drop);

                // EL ARMA MUERE en todas las pantallas (netID 0 — el truco
                // del shimmer: el mensaje 21 apaga el slot).
                drop.active = false;
                drop.stack = 0;
                if (Main.netMode == NetmodeID.Server)
                    Terraria.NetMessage.SendData(MessageID.SyncItem, -1, -1, null, id);
                muertos.Add(id);

                // EL NÚMERO CAMINA YA (el daño del Eterno se calcula en la
                // máquina del portador — el mismo cauce del poder).
                sp.SyncAhoraDevoracion();

                // LA VOZ DEL ETERNO — el sabor del arma (por rareza).
                string claveSabor = exp <= 20 ? "Mods.AethonMod.Caida.DevoradoSabor1"
                    : exp <= 100 ? "Mods.AethonMod.Caida.DevoradoSabor2"
                    : exp <= 1000 ? "Mods.AethonMod.Caida.DevoradoSabor3"
                    : "Mods.AethonMod.Caida.DevoradoSabor4";
                EcoRed.HablarAlPortador(portador, claveSabor,
                    new Color(178, 110, 236), rugido: true, escala: 0.72f, prioridad: true);

                // LA CUENTA: «El Eterno devora a {arma}: +{exp} EXP · +1 {stat}».
                EcoRed.AnunciarAlPortador(portador, "Mods.AethonMod.Caida.ArmaDevorada",
                    new Color(150, 60, 220), drop.Name, exp,
                    NetworkText.FromKey("Mods.AethonMod.Caida." + statKey));

                // EL ALMA DESPIERTA (solo las especiales): «¡El alma de
                // {arma} despierta en el libro: {alma}!».
                if (!string.IsNullOrEmpty(almaKey))
                {
                    EcoRed.AnunciarAlPortador(portador, "Mods.AethonMod.Caida.AlmaDespertada",
                        new Color(122, 216, 182), drop.Name,
                        NetworkText.FromKey("Mods.AethonMod.Caida." + almaKey));
                }

                // EL POLVO DEL TRAGO (la máquina con pantalla).
                if (!Main.dedServ)
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item113.WithPitchOffset(-0.45f), centro);
                    for (int k = 0; k < 26; k++)
                    {
                        Dust d = Dust.NewDustPerfect(centro + new Vector2(
                                Main.rand.NextFloat(-30f, 30f), Main.rand.NextFloat(-40f, 6f)),
                            DustID.Shadowflame,
                            new Vector2(Main.rand.NextFloat(-2.6f, 2.6f),
                                Main.rand.NextFloat(-3.8f, -0.2f)), 160,
                            new Color(110, 40, 170), 1.1f);
                        d.noGravity = true;
                    }
                }
            }
            catch { }
        }

        // ================================================================
        //  LA CORRUPCIÓN DE ITEMS NORMALES — el camino de la .128
        // ================================================================

        /// <summary>LA CONVERSIÓN: el item muere como era y NACE como
        /// cáscara — EN SU MISMO SLOT (el mensaje 21 de vanilla, el del
        /// shimmer: todas las pantallas ven la MISMA transformación).</summary>
        private static void Corromper(LaCaidaSistema.AltarDelMundo zona, int id, Item drop, List<int> muertos)
        {
            try
            {
                // CUÁNTAS ESENCIAS: lo que VALÍA (valor total del stack;
                // la basura da 1, un tesoro hasta 99).
                long valor = Math.Max(0L, (long)drop.value) * drop.stack;
                int n = (int)MathHelper.Clamp(1 + valor / 2500L, 1, 99);

                int tipoHusos = ModContent.ItemType<global::AethonMod.Content.Items.Altar.ItemCorrupto>();
                drop.netDefaults(tipoHusos);
                drop.stack = n;
                drop.noGrabDelay = 45;
                drop.timeSinceItemSpawned = 0;   // la bruma lo preserva
                drop.velocity *= 0.3f;

                // LA DIFUSIÓN (todas las pantallas ven nacer la cáscara).
                if (Main.netMode == NetmodeID.Server)
                    Terraria.NetMessage.SendData(MessageID.SyncItem, -1, -1, null, id);
                muertos.Add(id);

                // EL AVISO al que esté cerca (la magia negra se siente).
                Player testigo = BuscarPortador(zona.Posicion()) ??
                    JugadorMasCerca(zona.Posicion(), 35f * 16f);
                if (testigo != null)
                {
                    EcoRed.AnunciarAlPortador(testigo, "Mods.AethonMod.Caida.ItemCorrompido",
                        new Color(150, 60, 220), n);
                }

                // EL POLVO DEL CAMBIO (la máquina con pantalla).
                if (!Main.dedServ)
                {
                    for (int k = 0; k < 16; k++)
                    {
                        Dust d = Dust.NewDustPerfect(drop.Center, DustID.Shadowflame,
                            new Vector2(Main.rand.NextFloat(-1.8f, 1.8f),
                                Main.rand.NextFloat(-3.2f, -0.4f)), 150,
                            new Color(120, 40, 180), 1f);
                        d.noGravity = true;
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103.WithPitchOffset(-0.3f),
                        drop.Center);
                }
            }
            catch { }
        }

        // ================================================================
        //  EL TRAGO — el Eterno se come las cáscaras (el camino de la .128)
        // ================================================================

        /// <summary>EL FESTÍN: TODAS las cáscaras de la zona suben de una
        /// vez — el poder crece para siempre y la VOZ comenta el sabor.</summary>
        private static void Tragar(LaCaidaSistema.AltarDelMundo zona, Vector2 centro,
            int semilla, Player portador, List<int> muertos)
        {
            try
            {
                int tipoHusos = ModContent.ItemType<global::AethonMod.Content.Items.Altar.ItemCorrupto>();

                // EL VÓRTICE REÚNE la mesa entera (todas las pilas de la zona).
                int total = 0;
                for (int i = 0; i < Main.maxItems; i++)
                {
                    Item drop = Main.item[i];
                    if (drop == null || !drop.active || drop.type != tipoHusos || drop.stack <= 0) continue;
                    if (!EnLaZona(drop, centro)) continue;
                    total += drop.stack;

                    // EL PLATO MUERE en todas las pantallas (netID 0 = el
                    // mensaje 21 apaga el slot — el truco del shimmer).
                    drop.active = false;
                    drop.stack = 0;
                    if (Main.netMode == NetmodeID.Server)
                        Terraria.NetMessage.SendData(MessageID.SyncItem, -1, -1, null, i);
                    muertos.Add(i);
                }
                if (total <= 0) return;

                // EL PODER CRECE para siempre (el .plr del portador lo
                // guardará — y el SyncPlayer lo lleva a su cliente).
                var sp = portador.GetModPlayer<Players.ShardPlayer>();
                if (sp == null) return;
                sp.DevoracionPoder += total;
                sp.SyncAhoraDevoracion();

                // LA VOZ DEL ETERNO — el sabor de lo tragado (por tamaño
                // del bocado: 1-2 casi nada · 3-8 pasable · 9-20 sabroso ·
                // 21+ un manjar).
                string clave = total <= 2 ? "Mods.AethonMod.Caida.DevoradoSabor1"
                    : total <= 8 ? "Mods.AethonMod.Caida.DevoradoSabor2"
                    : total <= 20 ? "Mods.AethonMod.Caida.DevoradoSabor3"
                    : "Mods.AethonMod.Caida.DevoradoSabor4";
                EcoRed.HablarAlPortador(portador, clave,
                    new Color(178, 110, 236), rugido: true, escala: 0.72f, prioridad: true);

                // LA CUENTA (el aviso de las estadísticas). El porcentaje
                // viaja COMO TEXTO ya redondeado a 2 decimales: un bocado
                // de 1 esencia es +0,25 % y el número ENTERO lo escondería
                // («+0 %» leería como roto — el análisis v6.50.128).
                EcoRed.AnunciarAlPortador(portador, "Mods.AethonMod.Caida.DevoradoCuenta",
                    new Color(150, 60, 220), total,
                    (Players.ShardPlayer.BonusDanoDevorado(portador) * 100f).ToString("0.##"));

                // EL POLVO DEL TRAGO (la máquina con pantalla).
                if (!Main.dedServ)
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item113.WithPitchOffset(-0.45f), centro);
                    for (int k = 0; k < 24; k++)
                    {
                        Dust d = Dust.NewDustPerfect(centro + new Vector2(
                                Main.rand.NextFloat(-14f, 14f), Main.rand.NextFloat(-40f, -6f)),
                            DustID.Shadowflame,
                            new Vector2(Main.rand.NextFloat(-2.4f, 2.4f),
                                Main.rand.NextFloat(-3.6f, -0.2f)), 160,
                            new Color(110, 40, 170), 1f);
                        d.noGravity = true;
                    }
                }
            }
            catch { }
        }

        /// <summary>El jugador vivo más cercano a un punto (sin filtro — el testigo del aviso).</summary>
        private static Player JugadorMasCerca(Vector2 centro, float alcance)
        {
            Player mejor = null;
            float mejorD = alcance * alcance;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p == null || !p.active || p.dead) continue;
                float d = p.Center.DistanceSQ(centro);
                if (d < mejorD) { mejorD = d; mejor = p; }
            }
            return mejor;
        }

        /// <summary>Los diccionarios olvidan a los muertos (drops recogidos, evaporados…).</summary>
        private void LimpiezaDeDiccionarios()
        {
            var muertosC = new List<int>();
            foreach (var par in _corrompiendo)
                if (par.Key < 0 || par.Key >= Main.maxItems || !Main.item[par.Key].active)
                    muertosC.Add(par.Key);
            foreach (int k in muertosC) _corrompiendo.Remove(k);

            var muertosA = new List<int>();
            foreach (var par in _corrompiendoArmas)
                if (par.Key < 0 || par.Key >= Main.maxItems || !Main.item[par.Key].active ||
                    !EsArma(Main.item[par.Key]) ||
                    Main.item[par.Key].GetGlobalItem<global::AethonMod.Content.Globals.CorruptoGlobalItem>()?.Corrompido == true)
                    muertosA.Add(par.Key);
            foreach (int k in muertosA) _corrompiendoArmas.Remove(k);

            var muertosE = new List<int>();
            foreach (var par in _armasEsperando)
                if (par.Key < 0 || par.Key >= Main.maxItems || !Main.item[par.Key].active ||
                    !EsArma(Main.item[par.Key]) ||
                    Main.item[par.Key].GetGlobalItem<global::AethonMod.Content.Globals.CorruptoGlobalItem>()?.Corrompido != true)
                    muertosE.Add(par.Key);
            foreach (int k in muertosE) _armasEsperando.Remove(k);

            var muertosB = new List<int>();
            foreach (var par in _cascaras)
                if (par.Key < 0 || par.Key >= Main.maxItems || !Main.item[par.Key].active ||
                    Main.item[par.Key].type != ModContent.ItemType<global::AethonMod.Content.Items.Altar.ItemCorrupto>())
                    muertosB.Add(par.Key);
            foreach (int k in muertosB) _cascaras.Remove(k);
        }

        // ================================================================
        //  LA COSMÉTICA DE LA BRUMA (todas las máquinas, cada frame)
        // ================================================================

        /// <summary>EL VÓRTICE SUAVE: las cáscaras y las armas corruptas
        /// en la zona son atraídas al corazón de la bruma (flotan hacia
        /// el remolino — la mesa se sirve sola). Los drops del mundo son
        /// simulación LOCAL: cada pantalla empuja las suyas y todas ven
        /// el mismo rito.</summary>
        private static void CosmeticaDeLaBruma()
        {
            _zonaComiendo = -1;
            int tipoHusos = ModContent.ItemType<global::AethonMod.Content.Items.Altar.ItemCorrupto>();

            var sys = ModContent.GetInstance<AltarDevoradorSistema>();
            if (sys == null) return;

            foreach (var zona in sys._zonasLocales)
            {
                if (zona == null) continue;
                Vector2 centro = new Vector2(zona.X * 16f + 8f, zona.Y * 16f + 8f);

                // ¿La zona está en pantalla? (si no: ni polvo ni nada).
                if (centro.X < Main.screenPosition.X - 120f ||
                    centro.X > Main.screenPosition.X + Main.screenWidth + 120f ||
                    centro.Y < Main.screenPosition.Y - 240f ||
                    centro.Y > Main.screenPosition.Y + Main.screenHeight + 160f) continue;

                bool activo = false, comiendo = false;

                for (int i = 0; i < Main.maxItems; i++)
                {
                    Item drop = Main.item[i];
                    if (drop == null || !drop.active || drop.stack <= 0) continue;
                    if (!EnLaZona(drop, centro)) continue;

                    var sello = drop.GetGlobalItem<global::AethonMod.Content.Globals.CorruptoGlobalItem>();
                    bool armaCorrupta = sello != null && sello.Corrompido;

                    if (drop.type == tipoHusos || armaCorrupta)
                    {
                        // LA SUCCIÓN hacia el corazón de la bruma (el
                        // remolino que se los traga — ítem a ítem).
                        comiendo = true;
                        Vector2 corazon = centro + new Vector2(0f, -24f);
                        Vector2 hacia = corazon - drop.Center;
                        drop.velocity += hacia * 0.012f;
                        drop.velocity *= 0.90f;
                        if (hacia.Length() < 20f) drop.velocity *= 0.5f;
                    }
                    else if (!EsSagrado(drop))
                    {
                        // EL PUDRIMIENTO: chispas violetas mordiendo al item
                        // (el arma ganándose el sello — la bruma la rodea).
                        activo = true;
                        if (Main.rand.NextBool(5))
                        {
                            Dust d = Dust.NewDustPerfect(drop.Center + Main.rand.NextVector2Unit() * 8f,
                                DustID.Shadowflame, new Vector2(0f, -0.8f), 140,
                                new Color(130, 50, 190), 0.7f);
                            d.noGravity = true;
                        }
                    }
                }

                int idZona = zona.X * 100000 + zona.Y;
                if (activo) _zonasActivas.Add(idZona);
                if (comiendo) _zonaComiendo = idZona;
            }

            // EL VÓRTICE RESPIRA (0..1): gira al comer, se aquieta solo.
            float objetivo = _zonaComiendo >= 0 ? 1f : 0f;
            _vortice += (objetivo - _vortice) * 0.10f;
        }

        // ================================================================
        //  LA BRUMA — el dibujado (PostDrawTiles: el lote de vanilla llega
        //  CERRADO y vanilla abre el suyo después — lotes propios)
        // ================================================================

        public override void PostDrawTiles()
        {
            try
            {
                if (Main.gameMenu || Main.dedServ) return;
                var sys = ModContent.GetInstance<AltarDevoradorSistema>();
                if (sys == null || sys._zonasLocales.Count == 0) return;

                // === 1. LA BRUMA DE MASA (el humo fBn que OCLUYE — el
                //     minibioma de la letra: «solo obsidiana y ademas
                //     bruma»). Lote propio BeginMass/End, coords de
                //     PANTALLA (mundo − screenPosition). ===
                bool loteMasa = false;
                try
                {
                    BrumaFX.BeginMass();
                    loteMasa = true;
                    foreach (var zona in sys._zonasLocales)
                    {
                        if (zona == null) continue;
                        Vector2 centro = new Vector2(zona.X * 16f + 8f, zona.Y * 16f + 8f);
                        if (centro.X < Main.screenPosition.X - 160f ||
                            centro.X > Main.screenPosition.X + Main.screenWidth + 160f ||
                            centro.Y < Main.screenPosition.Y - 320f ||
                            centro.Y > Main.screenPosition.Y + Main.screenHeight + 160f) continue;
                        DibujarLaBruma(centro, zona.X, zona.Y);
                    }
                }
                catch { }
                finally
                {
                    if (loteMasa) { try { Main.spriteBatch.End(); } catch { } }
                }

                // === 2. EL ALIENTO (aditivo): el resplandor del piso (la
                //     lectura nocturna del minibioma) + EL VÓRTICE cuando
                //     come — la bruma que gira y se traga la mesa. ===
                SpriteBatch sb = Main.spriteBatch;
                bool dibujado = false;
                try
                {
                    sb.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None,
                        Main.Rasterizer, null, Main.Transform);
                    dibujado = true;

                    foreach (var zona in sys._zonasLocales)
                    {
                        if (zona == null) continue;
                        Vector2 centro = new Vector2(zona.X * 16f + 8f, zona.Y * 16f + 8f);
                        if (centro.X < Main.screenPosition.X - 160f ||
                            centro.X > Main.screenPosition.X + Main.screenWidth + 160f ||
                            centro.Y < Main.screenPosition.Y - 320f ||
                            centro.Y > Main.screenPosition.Y + Main.screenHeight + 160f) continue;
                        DibujarElAliento(sb, centro, zona.X, zona.Y);
                    }
                }
                catch { }
                finally
                {
                    if (dibujado) { try { sb.End(); } catch { } }
                }
            }
            catch { }
        }

        /// <summary>LA BRUMA DEL MINIBIOMA (el humo de verdad — BrumaFX
        /// fBn): tres columnas que nacen de la concha y respiran — negra
        /// al centro (la masa que ocluye), violácea a los costados (la
        /// lectura), más GORDA cuando algo se está corrompiendo. Coordenadas
        /// de PANTALLA (el lote de masa usa GameViewMatrix: la cámara se
        /// resta aquí — la convención de SombrasLib.Bruma).</summary>
        private static void DibujarLaBruma(Vector2 centro, int tileX, int tileY)
        {
            float t = Main.GlobalTimeWrappedHourly;
            int idZona = tileX * 100000 + tileY;
            bool activo = _zonasActivas.Contains(idZona);
            float vigor = activo ? 1.45f : 1f;

            Vector2 baseP = centro - Main.screenPosition;
            int semilla = tileX * 31 + tileY * 7 + 911;

            // LA COLUMNA NEGRA (la masa central — sube ~6 tiles).
            BrumaFX.Column(baseP, 92f * vigor, 40f, SombrasLib.HumoNegro,
                semilla, t, 0.60f * vigor, 0.30f);

            // LAS COLUMNAS VIOLETAS (los costados que respiran — la
            // lectura nocturna de la bruma).
            BrumaFX.Column(baseP + new Vector2(-34f, 2f), 70f * vigor, 30f,
                SombrasLib.HumoVioleta, semilla + 37, t * 1.07f, 0.42f * vigor, 0.26f);
            BrumaFX.Column(baseP + new Vector2(34f, 2f), 64f * vigor, 27f,
                SombrasLib.HumoVioleta, semilla + 71, t * 0.93f, 0.38f * vigor, 0.34f);
        }

        /// <summary>EL ALIENTO DE LA BRUMA (aditivo): el RESPLANDOR DEL
        /// PISO — el corazón violeta del minibioma latiendo sobre la
        /// concha (la lectura de noche) — y EL VÓRTICE cuando come: la
        /// espiral de motas que gira y se cierra sobre la mesa (SIN ojo,
        /// SIN boca, SIN oro: solo la bruma trabajando).</summary>
        private static void DibujarElAliento(SpriteBatch sb, Vector2 centro, int tileX, int tileY)
        {
            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            if (glow == null) return;

            float t = Main.GlobalTimeWrappedHourly;
            int idZona = tileX * 100000 + tileY;
            bool activo = _zonasActivas.Contains(idZona);
            Vector2 pos = centro - Main.screenPosition;

            // EL RESPLANDOR DEL PISO (el latido del minibioma).
            float latido = 0.16f + 0.06f * MathF.Sin(t * 1.7f + tileX * 0.3f);
            if (activo) latido *= 1.6f;
            sb.Draw(glow, pos, null, new Color(96, 44, 160) * latido, 0f,
                glow.Size() * 0.5f, new Vector2(150f, 52f) / glow.Size(),
                SpriteEffects.None, 0f);
            sb.Draw(glow, pos, null, new Color(150, 80, 220) * (latido * 0.7f), 0f,
                glow.Size() * 0.5f, new Vector2(70f, 26f) / glow.Size(),
                SpriteEffects.None, 0f);

            // EL VÓRTICE (cuando come): la espiral de motas que gira y
            // se cierra — la bruma tragándose la mesa.
            if (_vortice > 0.04f && _zonaComiendo == idZona)
            {
                for (int m = 0; m < 10; m++)
                {
                    float f = m / 10f;
                    float gira = t * (2.2f - f * 0.8f) + m * MathHelper.TwoPi / 10f;
                    float radio = (52f - 34f * f) * (1f + 0.10f * MathF.Sin(t * 3.1f + m));
                    Vector2 p = pos + new Vector2(MathF.Cos(gira) * radio,
                        MathF.Sin(gira) * radio * 0.62f - 18f);
                    float alfa = _vortice * (0.55f - 0.32f * f);
                    sb.Draw(glow, p, null, new Color(150, 70, 220) * alfa, gira,
                        glow.Size() * 0.5f, new Vector2(11f, 5.5f) / glow.Size(),
                        SpriteEffects.None, 0f);
                }
                // EL CORAZÓN del vórtice (la boca de la bruma — un
                // embudo de luz, jamás un ojo).
                sb.Draw(glow, pos + new Vector2(0f, -22f), null,
                    new Color(190, 120, 240) * (0.30f * _vortice), 0f,
                    glow.Size() * 0.5f, new Vector2(34f, 30f) / glow.Size(),
                    SpriteEffects.None, 0f);
            }
        }
    }
}
