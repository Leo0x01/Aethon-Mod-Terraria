using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  EL ALTAR DEVORADOR (v6.50.128) — LA MECÁNICA DE COMER ITEMS.
    //
    // LA LETRA DEL USUARIO: «el lugar de caida quede como un altar para
    // añadir una nueva mecanica que dara mas vida al mod, la mecanica es
    // la de comerse los item y mejorar el grimorio del eterno, para comer
    // un item este debe corromperse primero y para eso se usara el lugar
    // de caida».
    //
    // EL CIRCUITO COMPLETO (el altar es PERMANENTE — vive en el cráter
    // aunque el libro dormido ya se haya ido):
    //   1. EL JUGADOR ARROJA un item sobre el altar (un drop del mundo en
    //      el haz — 3 tiles a la redonda).
    //   2. LA CORRUPCIÓN (solo AUTORIDAD, cada 10 t): 2,5 s pudriéndose
    //      en el haz… y el item se CONVIERTE EN SITIO en esencias
    //      corruptas (ItemCorrupto — tantas como valía: la basura 1, un
    //      tesoro hasta 99). La conversión viaja por el cable con el
    //      SyncItem de vanilla (21 — el MISMO mensaje del shimmer: todas
    //      las pantallas ven al mismo drop CONVERTIRSE).
    //   3. LA COMIDA: si un portador del Grimorio del Eterno anda cerca
    //      (<40 tiles), la BOCA se abre sobre el haz (todas las pantallas
    //      — es cosmética local: las cáscaras son succionadas hacia
    //      arriba en cada máquina)… y al segundo el Eterno SE LAS TRAGA:
    //      el drop muere en todas las pantallas (SyncItem con netID 0),
    //      el poder sube PARA SIEMPRE (ShardPlayer.DevoracionPoder: +daño
    //      y +ojos al Grimorio del Eterno) y la VOZ comenta el sabor.
    //
    // EL HAZ (la referencia del usuario): el tubo de luz VIOLETA que nace
    // del bloque dorado del altar y sube — INTENSO cuando corrompe, y con
    // la boca abierta cuando come. Se dibuja en PostDrawTiles (el lote de
    // vanilla llega CERRADO ahí — descompilado — y vanilla abre el suyo
    // después: lotes propios Begin/End, sin tocar nada de nadie).
    // ======================================================================
    public class AltarDevoradorSistema : ModSystem
    {
        // === EL RITMO DEL RITUAL ===
        /// <summary>Ticks de item pudriéndose en el haz antes de convertirse (2,5 s).</summary>
        private const int TICKS_CORROMPER = 150;
        /// <summary>Ticks de cáscara sobre el altar antes del trago (~1 s).</summary>
        private const int TICKS_TRAGAR = 55;
        /// <summary>El radio del haz (en tiles) que atrapa a los drops.</summary>
        private const int RADIO_TILES = 3;
        /// <summary>El alcance del portador del Eterno para comer (en px).</summary>
        private const float ALCANCE_PORTADOR = 40f * 16f;

        /// <summary>Los items pudriéndose (item.whoAmI → ticks en el haz). Solo AUTORIDAD.</summary>
        private readonly Dictionary<int, int> _corrompiendo = new();
        /// <summary>Las cáscaras esperando su trago (item.whoAmI → ticks). Solo AUTORIDAD.</summary>
        private readonly Dictionary<int, int> _cascaras = new();

        // === EL MAPA LOCAL (v6.50.128 — la cosmética en TODAS las máquinas):
        //     los clientes MP no cargan la lista del mundo (vive con la
        //     AUTORIDAD) — cada pantalla encuentra SUS altares escaneando
        //     las tiles alrededor del jugador local cada segundo (la tile
        //     dorada es rara: 5000 lecturas por minuto es nada). ===
        private class AltarLocal
        {
            public int X;
            public int Y;
        }

        private readonly List<AltarLocal> _altaresLocales = new();
        private ulong _ultimoBarrido;

        // === EL FOCO VISUAL (cosmética local — cada pantalla la suya) ===
        /// <summary>¿Hay algo pudriéndose en el haz AHORA? (por altar — para intensificar el dibujado).</summary>
        private static readonly HashSet<int> _altaresActivos = new();
        /// <summary>¿La boca está abierta? (el altar que está comiendo).</summary>
        private static int _altarComiendo = -1;
        /// <summary>El reloj local de la boca (0..1 apertura).</summary>
        private static float _bocaAbierta;

        // ================================================================
        //  LA LÓGICA (cada 10 ticks — el pulso de la casa)
        // ================================================================

        public override void PreUpdateTime()
        {
            try
            {
                if (Main.gameMenu) return;

                _altaresActivos.Clear();

                // === EL BARRIDO LOCAL (todas las máquinas con pantalla):
                //     cada segundo, los altares de la zona del jugador —
                //     es el mapa que alimenta la cosmética (el haz, la
                //     succión, el polvo). ===
                if (!Main.dedServ && Main.GameUpdateCount - _ultimoBarrido >= 60ul)
                {
                    _ultimoBarrido = Main.GameUpdateCount;
                    BarrerAltaresLocales();
                }

                // === LA COSMÉTICA (TODAS las máquinas): las cáscaras son
                //     succionadas hacia la boca del haz (los drops del
                //     mundo son simulación LOCAL — cada pantalla empuja
                //     las suyas: mismo ritual, misma imagen).
                if (!Main.dedServ && _altaresLocales.Count > 0) CosmeticaDelHaz();

                // === EL RITUAL (solo AUTORIDAD — SU lista del mundo) ===
                if (Main.netMode == NetmodeID.MultiplayerClient) return;
                if ((Main.GameUpdateCount % 10ul) != 0ul) return;

                var sysCaida = ModContent.GetInstance<LaCaidaSistema>();
                if (sysCaida == null || sysCaida.Altares.Count == 0) return;
                var altares = sysCaida.Altares;

                var muertos = new List<int>();

                foreach (var altar in altares)
                {
                    if (altar == null) continue;
                    Vector2 centro = new Vector2(altar.X * 16f + 8f, altar.Y * 16f + 8f);
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
                            // El altar PRESERVA sus ofrendas (el reloj de
                            // despawn de vanilla jamás las alcanza).
                            drop.timeSinceItemSpawned = 0;
                            _cascaras[i] = _cascaras.TryGetValue(i, out int tCascara) ? tCascara + 10 : 10;

                            if (_cascaras[i] >= TICKS_TRAGAR && portador != null)
                            {
                                Tragar(altar, centro, i, portador, muertos);
                            }
                            continue;
                        }

                        // ===== LO QUE NO SE COME: el propio Eterno jamás =====
                        if (EsSagrado(drop)) { muertos.Add(i); continue; }

                        // ===== EL ITEM NORMAL: se pudre =====
                        _corrompiendo[i] = _corrompiendo.TryGetValue(i, out int t) ? t + 10 : 10;
                        if (_corrompiendo[i] >= TICKS_CORROMPER)
                        {
                            Corromper(altar, i, drop, muertos);
                        }
                    }
                }

                // LA LIMPIEZA: los diccionarios no guardan fantasmas.
                foreach (int id in muertos) { _corrompiendo.Remove(id); _cascaras.Remove(id); }
                LimpiezaDeDiccionarios();
            }
            catch { }
        }

        /// <summary>EL BARRIDO LOCAL: las tiles de altar en la zona del
        /// jugador (±60 columnas, ±30 filas — la pantalla y un poco más).
        /// La lista del MUNDO (LaCaidaSistema.Altares) es de la AUTORIDAD;
        /// esta es el mapa de la COSMÉTICA de cada pantalla.</summary>
        private void BarrerAltaresLocales()
        {
            try
            {
                _altaresLocales.Clear();
                Player yo = Main.LocalPlayer;
                if (yo == null || !yo.active) return;

                int tipo = ModContent.TileType<global::AethonMod.Content.Tiles.AltarDeLaCaida>();
                int cx = (int)(yo.Center.X / 16f);
                int cy = (int)(yo.Center.Y / 16f);
                int x0 = Math.Max(10, cx - 60), x1 = Math.Min(Main.maxTilesX - 11, cx + 60);
                int y0 = Math.Max(10, cy - 34), y1 = Math.Min(Main.maxTilesY - 11, cy + 34);

                for (int x = x0; x <= x1; x++)
                    for (int y = y0; y <= y1; y++)
                    {
                        Tile t = Main.tile[x, y];
                        if (t != null && t.HasTile && t.TileType == tipo)
                            _altaresLocales.Add(new AltarLocal { X = x, Y = y });
                    }

                // LA AUTORIDAD también conoce SUS altares de la lista (los
                // que el barrido no ve — le da igual: su lógica usa la otra
                // lista; esto es solo para no dibujar de menos cerca).
            }
            catch { }
        }

        /// <summary>¿Este drop está en la zona del haz? (3 tiles a la redonda, apoyado o flotando bajo).</summary>
        private static bool EnLaZona(Item drop, Vector2 centro)
        {
            float dx = MathF.Abs(drop.Center.X - centro.X);
            float dy = drop.Center.Y - centro.Y;
            return dx <= RADIO_TILES * 16f && dy >= -40f && dy <= RADIO_TILES * 16f * 2.2f;
        }

        /// <summary>LO QUE EL ALTAR NO TOCA: el Grimorio del Eterno (¡el
        /// comensal no es plato!) ni las esencias corruptas (ya son su
        /// comida — las maneja la otra rama).</summary>
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
        //  LA CORRUPCIÓN — el item se convierte en esencias
        // ================================================================

        /// <summary>LA CONVERSIÓN: el item muere como era y NACE como
        /// cáscara — EN SU MISMO SLOT (el mensaje 21 de vanilla, el del
        /// shimmer: todas las pantallas ven la MISMA transformación).</summary>
        private static void Corromper(LaCaidaSistema.AltarDelMundo altar, int id, Item drop, List<int> muertos)
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
                drop.timeSinceItemSpawned = 0;   // el altar lo preserva
                drop.velocity *= 0.3f;

                // LA DIFUSIÓN (todas las pantallas ven nacer la cáscara).
                if (Main.netMode == NetmodeID.Server)
                    Terraria.NetMessage.SendData(MessageID.SyncItem, -1, -1, null, id);
                muertos.Add(id);

                // EL AVISO al que esté cerca (la magia negra se siente).
                Player testigo = BuscarPortador(altar.Posicion()) ??
                    JugadorMasCerca(altar.Posicion(), 35f * 16f);
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
        //  EL TRAGO — el Eterno se come las cáscaras
        // ================================================================

        /// <summary>EL FESTÍN: TODAS las cáscaras del altar suben de una
        /// vez — el poder crece para siempre y la VOZ comenta el sabor.</summary>
        private static void Tragar(LaCaidaSistema.AltarDelMundo altar, Vector2 centro,
            int semilla, Player portador, List<int> muertos)
        {
            try
            {
                int tipoHusos = ModContent.ItemType<global::AethonMod.Content.Items.Altar.ItemCorrupto>();

                // LA BOCA REÚNE la mesa entera (todas las pilas del altar).
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

                // LA CUENTA (el aviso de las estadísticas).
                EcoRed.AnunciarAlPortador(portador, "Mods.AethonMod.Caida.DevoradoCuenta",
                    new Color(150, 60, 220), total,
                    (int)MathF.Round(Players.ShardPlayer.BonusDanoDevorado(portador) * 100f));

                // EL POLVO DEL TRAGO (la máquina con pantalla).
                if (!Main.dedServ)
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item113.WithPitchOffset(-0.45f), centro);
                    for (int k = 0; k < 24; k++)
                    {
                        Dust d = Dust.NewDustPerfect(centro + new Vector2(
                                Main.rand.NextFloat(-14f, 14f), Main.rand.NextFloat(-46f, -6f)),
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

            var muertosB = new List<int>();
            foreach (var par in _cascaras)
                if (par.Key < 0 || par.Key >= Main.maxItems || !Main.item[par.Key].active ||
                    Main.item[par.Key].type != ModContent.ItemType<global::AethonMod.Content.Items.Altar.ItemCorrupto>())
                    muertosB.Add(par.Key);
            foreach (int k in muertosB) _cascaras.Remove(k);
        }

        // ================================================================
        //  LA COSMÉTICA DEL HAZ (todas las máquinas, cada frame)
        // ================================================================

        /// <summary>EL EMPUJE DE LA BOCA: las cáscaras sobre el altar son
        /// succionadas hacia arriba (suavemente — flotan hacia la boca
        /// del haz). Los drops del mundo son simulación LOCAL: cada
        /// pantalla empuja las suyas y todas ven el mismo rito.</summary>
        private static void CosmeticaDelHaz()
        {
            _altarComiendo = -1;
            int tipoHusos = ModContent.ItemType<global::AethonMod.Content.Items.Altar.ItemCorrupto>();

            var sys = ModContent.GetInstance<AltarDevoradorSistema>();
            if (sys == null) return;

            foreach (var altar in sys._altaresLocales)
            {
                if (altar == null) continue;
                Vector2 centro = new Vector2(altar.X * 16f + 8f, altar.Y * 16f + 8f);

                // ¿El altar está en pantalla? (si no: ni polvo ni nada).
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

                    if (drop.type == tipoHusos)
                    {
                        // LA SUCCIÓN hacia la boca (3 tiles sobre el altar).
                        comiendo = true;
                        Vector2 boca = centro + new Vector2(0f, -52f);
                        Vector2 hacia = boca - drop.Center;
                        drop.velocity += hacia * 0.012f;
                        drop.velocity *= 0.90f;
                        if (hacia.Length() < 20f) drop.velocity *= 0.5f;
                    }
                    else if (!EsSagrado(drop))
                    {
                        // EL PUDRIMIENTO: chispas violetas mordiendo al item.
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

                if (activo) _altaresActivos.Add(altar.X * 100000 + altar.Y);
                if (comiendo) _altarComiendo = altar.X * 100000 + altar.Y;
            }

            // LA BOCA RESPIRA (0..1): abre al comer, cierra sola.
            float objetivo = _altarComiendo >= 0 ? 1f : 0f;
            _bocaAbierta += (objetivo - _bocaAbierta) * 0.12f;
        }

        // ================================================================
        //  EL HAZ — el dibujado (PostDrawTiles: el lote de vanilla llega
        //  CERRADO y vanilla abre el suyo después — lotes propios)
        // ================================================================

        public override void PostDrawTiles()
        {
            try
            {
                if (Main.gameMenu || Main.dedServ) return;
                var sys = ModContent.GetInstance<AltarDevoradorSistema>();
                if (sys == null || sys._altaresLocales.Count == 0) return;

                SpriteBatch sb = Main.spriteBatch;
                bool dibujado = false;
                try
                {
                    sb.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None,
                        Main.Rasterizer, null, Main.Transform);
                    dibujado = true;

                    foreach (var altar in sys._altaresLocales)
                    {
                        if (altar == null) continue;
                        Vector2 centro = new Vector2(altar.X * 16f + 8f, altar.Y * 16f + 8f);

                        // ¿En pantalla?
                        if (centro.X < Main.screenPosition.X - 120f ||
                            centro.X > Main.screenPosition.X + Main.screenWidth + 120f ||
                            centro.Y < Main.screenPosition.Y - 360f ||
                            centro.Y > Main.screenPosition.Y + Main.screenHeight + 160f) continue;

                        DibujarElHaz(sb, centro, altar.X, altar.Y);
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

        /// <summary>EL HAZ DE LA REFERENCIA: el tubo de luz VIOLETA que
        /// nace del bloque dorado y sube ~12 tiles — el núcleo brillante,
        /// el velo ancho, las runas que ascienden girando… y LA BOCA DEL
        /// ETERNO (el ojo que se abre cuando el festín sirve). TODO en
        /// coords de PANTALLA (mundo − screenPosition: Main.Transform no
        /// trae la cámara, la restamos nosotros — la convención de la
        /// casa).</summary>
        private static void DibujarElHaz(SpriteBatch sb, Vector2 centro, int tileX, int tileY)
        {
            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            if (glow == null) return;

            // EL CENTRO EN PANTALLA (la cámara se resta UNA sola vez).
            centro -= Main.screenPosition;

            float t = Main.GlobalTimeWrappedHourly;
            int idAltar = tileX * 100000 + tileY;
            bool activo = _altaresActivos.Contains(idAltar);
            float vigor = activo ? 1.55f : 1f;
            float alto = 12f * 16f * (activo ? 1.18f : 1f);

            // === 1. EL VELO (ancho, tenue — la columna entera) ===
            for (int i = 0; i < 5; i++)
            {
                float f = i / 4f;
                float y = centro.Y - alto * f;
                float ancho = MathHelper.Lerp(34f, 10f, f) * vigor;
                float alfa = MathHelper.Lerp(0.20f, 0.05f, f) * vigor;
                sb.Draw(glow, new Vector2(centro.X, y), null,
                    new Color(110, 50, 190) * alfa, 0f, glow.Size() * 0.5f,
                    new Vector2(ancho, alto / 5f + 22f) / glow.Size(),
                    SpriteEffects.None, 0f);
            }

            // === 2. EL NÚCLEO (angosto, brillante — el tubo de la
            //     referencia, con su latido de corazón) ===
            float latido = 0.80f + 0.20f * MathF.Sin(t * 2.1f);
            for (int i = 0; i < 3; i++)
            {
                float f = i / 2f;
                float y = centro.Y - alto * 0.5f * f;
                float ancho = MathHelper.Lerp(13f, 5f, f) * vigor * latido;
                float alfa = MathHelper.Lerp(0.42f, 0.14f, f) * vigor;
                sb.Draw(glow, new Vector2(centro.X, y), null,
                    new Color(178, 110, 236) * alfa, 0f, glow.Size() * 0.5f,
                    new Vector2(ancho, alto * 0.6f) / glow.Size(),
                    SpriteEffects.None, 0f);
            }

            // === 3. LAS RUNAS QUE ASCIENDEN (el pilar giratorio — tres
            //     chispas doradas que suben en espiral, como el haz de la
            //     referencia) ===
            for (int r = 0; r < 3; r++)
            {
                float fase = (t * 0.24f + r / 3f) % 1f;
                float y = centro.Y - 8f - fase * alto;
                float ang = t * 2.4f + r * MathHelper.TwoPi / 3f;
                Vector2 p = new Vector2(centro.X + MathF.Cos(ang) * 17f, y);
                float alfa = MathHelper.Clamp((1f - fase) * 1.2f, 0f, 1f) * 0.55f;
                sb.Draw(glow, p, null, new Color(255, 214, 130) * alfa, 0f,
                    glow.Size() * 0.5f, new Vector2(5.5f, 5.5f) / glow.Size(),
                    SpriteEffects.None, 0f);
            }

            // === 4. LA BOCA DEL ETERNO (cuando come): el OJO se abre
            //     sobre el haz y MIRA la comida — la firma del festín. ===
            if (_bocaAbierta > 0.04f)
            {
                Texture2D ojo = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Projectiles/OjoEterno_Abierto").Value;
                var iris = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
                if (ojo != null)
                {
                    Vector2 posBoca = centro + new Vector2(0f, -52f);
                    float abre = _bocaAbierta * _bocaAbierta * (3f - 2f * _bocaAbierta);
                    // EL HALO de la boca.
                    sb.Draw(glow, posBoca, null, new Color(110, 40, 170) * (0.30f * abre), 0f,
                        glow.Size() * 0.5f, new Vector2(64f, 64f) / glow.Size(),
                        SpriteEffects.None, 0f);
                    sb.Draw(ojo, posBoca, null, Color.White * (0.90f * abre), 0f,
                        ojo.Size() * 0.5f, 1.05f * abre, SpriteEffects.None, 0f);
                    if (iris != null)
                    {
                        // EL IRIS QUE MIRA hacia la mesa (abajo — de donde
                        // sube la comida).
                        float oscila = MathF.Sin(t * 3.1f) * 2.2f;
                        sb.Draw(iris, posBoca + new Vector2(0.7f + oscila, 4.5f) * 1.05f * abre,
                            null, Color.White * abre, 0f, iris.Size() * 0.5f,
                            0.41f * 1.05f * abre, SpriteEffects.None, 0f);
                    }
                }
            }
        }
    }
}
