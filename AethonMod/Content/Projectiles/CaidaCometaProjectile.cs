using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Systems;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles
{
    // ======================================================================
    //  EL COMETA DE TINTA (v6.50.117 — LA CAÍDA).
    //
    // LA LETRA: «reusar la estética del meteorito de fondo de Terraria
    // pero en NEGRO y MORADO»: una estrella negra con estela morada que
    // cruza la PARTE ALTA del cielo (fase 0), pica hacia su punto de
    // impacto (fase 1) y EXCAVA SU CRÁTER (fase 2) dejando al Grimorio
    // Dormido flotando en el centro.
    //
    // EL CABLE: netImportant (la IA corre en todos, como el coro del
    // AtaqueJefe) — pero SOLO LA AUTORIDAD cambia el mundo (cráter,
    // libro, registro de la caída): el resto mira el espectáculo.
    //
    // EL DESTINO viaja en ai[1]/ai[2] (X/1000f, Y/1000f) y el
    // destinatario es Projectile.owner (su cometa, su libro).
    // ======================================================================
    public class CaidaCometaProjectile : ModProjectile
    {
        /// <summary>FASE 0 «el cielo»: ticks que dura el cruce lateral (~4 s).</summary>
        private const float TicksCielo = 240f;

        /// <summary>La velocidad TOPE del picado (px/tick).</summary>
        private const float VelPicado = 38f;

        /// <summary>El núcleo de la estrella negra (casi negro, tinta).</summary>
        private static readonly Color NucleoTinta = new(18, 6, 26);

        /// <summary>La estela morada del cometa.</summary>
        private static readonly Color EstelaMorada = new(120, 40, 190);

        /// <summary>El aviso del impacto (morado, para el destinatario).</summary>
        private static readonly Color TinteCaida = new(150, 60, 220);

        /// <summary>
        /// LA CASA DEL CRÁTER (IDs reales de ESTE tML): cofres, mesas,
        /// puertas, pinturas, cajas de música y vitrinas — la estrella
        /// rompe PIEDRA, no la casa del jugador.
        /// </summary>
        private static readonly ushort[] CasaDelJugador =
        {
            TileID.Containers, TileID.Containers2,            // 21 · 467 cofres
            TileID.FakeContainers, TileID.FakeContainers2,    // 441 · 468
            TileID.Tables, TileID.Tables2,                    // 14 · 469 mesas
            TileID.BewitchingTable, TileID.AlchemyTable,      // 354 · 355
            TileID.PicnicTable, TileID.WarTable,              // 487 · 464
            TileID.ClosedDoor, TileID.OpenDoor,               // 10 · 11 puertas
            TileID.TallGateClosed, TileID.TallGateOpen,       // 388 · 389
            TileID.Painting3X3, TileID.Painting4X3,           // 240 · 241 pinturas
            TileID.Painting6X4, TileID.Painting2X3,           // 242 · 245
            TileID.Painting3X2,                               // 246
            TileID.MusicBoxes, TileID.ItemFrame,             // 139 · 395
            TileID.DisplayDoll, TileID.Banners,               // 470 · 91
        };

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.damage = 0;            // escénico: no toca a nadie
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;   // atraviesa la roca: su cita es un PUNTO
            Projectile.ignoreWater = true;
            Projectile.aiStyle = 0;           // la IA es 100% nuestra
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1800;       // generoso: cielo + picado + colchón
            Projectile.netImportant = true;   // todos lo ven caer
        }

        // ================================================================
        //  LA IA — tres fases (ai[0])
        // ================================================================
        public override void AI()
        {
            // localAI[0]: la edad; localAI[1]: el sonido de arranque (una vez).
            Projectile.localAI[0] += 1f;
            Vector2 destino = new Vector2(Projectile.ai[1] * 1000f, Projectile.ai[2] * 1000f);

            // EL ARRANQUE: un trueno grave muy atrás del cielo (solo la
            // autoridad lo emite — vanilla lo difunde; los clientes lo
            // reciben, no lo duplican).
            if (Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Sonar(SoundID.Item88.WithPitchOffset(-0.4f).WithVolumeScale(0.9f), Projectile.Center);
            }

            if (Projectile.ai[0] == 0f)
            {
                // ======== FASE 0 — EL CIELO ========
                // Viaja en DIAGONAL por la parte alta (la velocidad la puso
                // el disparo: ~14 px/t laterales + una leve caída).
                if (Projectile.localAI[0] >= TicksCielo)
                    Projectile.ai[0] = 1f; // el cielo termina: gira el picado

                // El polvo morado de la estela (su posición REAL — la
                // estela física del cielo).
                if (Projectile.localAI[0] % 4f == 0f)
                    PolvoEstela(Projectile.Center, 1);
            }
            else if (Projectile.ai[0] == 1f)
            {
                // ======== FASE 1 — EL PICADO ========
                Vector2 hacia = destino - Projectile.Center;
                float dist = hacia.Length();
                if (dist <= 40f)
                {
                    Impactar(destino);
                    return;
                }
                hacia /= dist;

                // Acelera hasta 38 px/t (la gravedad de la cita).
                float vel = System.Math.Min(VelPicado, Projectile.velocity.Length() + 0.38f);
                Projectile.velocity = hacia * vel;
                Projectile.rotation = Projectile.velocity.ToRotation();

                // La estela se alarga en el picado (doble de polvo).
                if (Projectile.localAI[0] % 2f == 0f)
                    PolvoEstela(Projectile.Center, 2);
            }
        }

        // ================================================================
        //  FASE 2 — EL IMPACTO
        // ================================================================
        private void Impactar(Vector2 destino)
        {
            Projectile.ai[0] = 2f;
            Projectile.position = destino - Projectile.Size * 0.5f;
            Projectile.velocity = Vector2.Zero;

            // === VISUALES (todas las máquinas — el polvo no viaja por cable) ===
            if (!Main.dedServ)
            {
                // La explosión de tinta: ~140 partículas moradas y negras.
                for (int i = 0; i < 140; i++)
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    float rap = Main.rand.NextFloat(1f, 9f);
                    bool morado = (i & 1) == 0;
                    Dust d = Dust.NewDustPerfect(destino, DustID.PurpleTorch,
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.8f) * rap, 200,
                        morado ? new Color(120, 40, 190) : new Color(18, 6, 26),
                        Main.rand.NextFloat(0.9f, 1.6f));
                    d.noGravity = i % 3 != 0;
                }
                // LA ONDA DE CHOQUE (el anillo de la casa — blanco frío).
                SombrasLib.OndaChoque(destino, 80f, 0.55f, roja: false);
                SombrasLib.OndaChoque(destino, 150f, 0.30f, roja: false);

                // LA SACUDIDA: solo quien la SIENTE (a <1600 px del impacto).
                Player yo = Main.LocalPlayer;
                if (yo != null && yo.active && yo.DistanceSQ(destino) < 1600f * 1600f)
                    PulsoLib.GolpearCamara(Vector2.UnitY, 12f, 26);
            }

            // EL TRUENO DEL ATERIZAJE (la autoridad lo difunde).
            if (Main.netMode != NetmodeID.MultiplayerClient)
                Sonar(SoundID.Item14.WithPitchOffset(-0.6f).WithVolumeScale(1.1f), destino);

            // === EL MUNDO CAMBIA — SOLO LA AUTORIDAD ===
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                ExcavarCrater(destino);
                LaCaidaSistema.RegistrarImpacto(destino, Projectile.owner);

                // El aviso del cráter — PRIVADO: solo el destinatario sabe
                // que su grimorio ya lo espera.
                if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
                    EcoRed.AnunciarAlPortador(Main.player[Projectile.owner],
                        "Mods.AethonMod.Caida.Impacto", TinteCaida);
            }

            // El cometa muere sin más polvo (el cráter ya habló).
            Projectile.Kill();
        }

        /// <summary>v6.50.124 — LA FAUCE DE OBSIDIANA (la letra del
        /// usuario: «cambiar el bloque, es mejor que sea obsidiana y que
        /// además que siempre haya un lugar por donde el jugador pueda
        /// pasar, en vez de una grieta en círculo de la caída, lo mejor
        /// es que sean picos de obsidiana como la caída de IT en
        /// pennywise, con picos hacia arriba y puntiagudos»).
        ///
        /// LA INVESTIGACIÓN (la pidió la letra): la caída de IT — el
        /// Pennywise vencido CAYENDO al hoyo negro de la cisterna bajo
        /// la casa de Neibolt (IT 2017) y la caída PRIMORDIAL que formó
        /// Derry (el lore: IT llegó del cielo en un impacto) — el
        /// lenguaje visual: un hoyo negro rodeado de FAUCES, dientes
        /// hacia arriba. Y la técnica Terraria: los picos PUNTIAGUDOS
        /// de verdad son PARES de medias-tiles en pendiente
        /// (SlopeType.SlopeDownLeft + SlopeDownRight = el pico /\).
        ///
        /// EL DISEÑO (el cráter circular MUERE — nace LA FAUCE):
        /// · EL CUENCO: la excavación de siempre (radio 11, la casa del
        ///   jugador intacta).
        /// · LAS DOS ESCALERAS — EL PASO GARANTIZADO: rampas de obsidiana
        ///   a 45° desde cada borde hasta el fondo, construidas con
        ///   TILES EN PENDIENTE (la técnica de las colinas de vanilla:
        ///   se CAMINA, no se salta) — se baja, se toma el libro y se
        ///   sube, siempre, por los dos lados.
        /// · LA GARGANTA: los colmillos que apuntan al libro — LOS DOS
        ///   GRANDES (±3, altura 6, la cornisa de la fauce) y el lecho
        ///   central (±2, altura 3); el fondo exacto queda abierto (ahí
        ///   cae la luz del sueño) y NINGÚN diente toca los
        ///   aterrizajes de las escaleras (viven en ±4).
        /// · TODO OBSIDIANA (la tinta quemada de la .118 se vestía de
        ///   ébano al 80%: ahora es obsidiana del 100% — la letra).
        /// · LA LECCIÓN 2D: en vista lateral TODO colmillo alto puesto
        ///   sobre la línea de marcha BLOQUEA el paso — por eso los
        ///   grandes viven en la GARGANTA (bajo el libro) y las rampas
        ///   son el camino: «siempre hay un lugar por donde el jugador
        ///   pueda pasar», de verdad y por construcción.</summary>
        private static void ExcavarCrater(Vector2 centro)
        {
            try
            {
                int cx = (int)(centro.X / 16f);
                int cy = (int)(centro.Y / 16f);
                const int radio = 11;
                bool toco = false;

                // === FASE A — EL CUENCO (como la .118: el corazón se
                //     abre, el borde a medias, la casa intacta) ===
                for (int x = cx - radio; x <= cx + radio; x++)
                {
                    for (int y = cy - radio; y <= cy + radio; y++)
                    {
                        if (x < 10 || y < 10 || x >= Main.maxTilesX - 10 || y >= Main.maxTilesY - 10) continue;
                        float d = MathF.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                        if (d > radio) continue;

                        Tile tile = Main.tile[x, y];
                        if (tile == null) continue;
                        if (tile.HasTile && EsCasaDelJugador(tile.TileType)) continue;

                        if (tile.HasTile && (d <= 8f || Main.rand.NextBool(2)))
                        {
                            WorldGen.KillTile(x, y, noItem: true);
                            toco = true;
                        }

                        // === FASE B — LA TINTA QUEMADA (TODO obsidiana — la
                        //     letra: «es mejor que sea obsidiana»): esparcida
                        //     SOLO dentro del cuenco (nada de escombros
                        //     flotando en el cielo — la garganta de colmillos
                        //     es la que cuenta la caída ahora) y el aro
                        //     tintado del borde, también obsidiana. LA
                        //     COLUMNA DEL LIBRO (±1) queda LIMPIA: ahí cae
                        //     la luz violeta del sueño. ===
                        if (!tile.HasTile && d >= 2.5f && y > cy + 2 && Math.Abs(x - cx) > 1)
                        {
                            float dentro = 1f - d / (float)radio;  // 1 centro → 0 borde
                            if (Main.rand.NextFloat() < 0.26f + 0.34f * dentro)
                                toco |= PonerObsidiana(x, y);
                        }
                        else if (tile.HasTile && y >= cy && d > 8f &&
                                 EsRocaNatural(tile.TileType) && Main.rand.NextFloat() < 0.35f)
                        {
                            tile.TileType = TileID.Obsidian;   // EL ARO ROTO: obsidiana (antes ébano)
                            WorldGen.SquareTileFrame(x, y, true);
                            toco = true;
                        }
                    }
                }

                // === FASE C — LAS DOS ESCALERAS (EL PASO GARANTIZADO): una
                //     rampa de obsidiana a 45° desde cada borde del cuenco
                //     hasta el fondo — la superficie de cada escalón es una
                //     TILE EN PENDIENTE (la técnica de las colinas de
                //     vanilla: SlopeDownLeft/SlopeDownRight se CAMINAN, no
                //     se saltan) con dos filas de relleno debajo y tres de
                //     aire limpio encima. El aterrizaje del fondo (±4) es
                //     el balcón de la fauce: desde ahí se habla con el
                //     libro. ===
                for (int lado = -1; lado <= 1; lado += 2)
                {
                    for (int i = 0; i <= 8; i++)
                    {
                        int x = cx + lado * (12 - i);
                        int ys = cy + i;    // la superficie del escalón (45° exactos)
                        if (x < 10 || x >= Main.maxTilesX - 10) break;

                        // LA SUPERFICIE CAMINABLE: la pendiente que baja
                        // hacia el centro (lado −: hacia la derecha →
                        // SlopeDownLeft; lado +: hacia la izquierda →
                        // SlopeDownRight).
                        toco |= Pender(x, ys,
                            lado < 0 ? SlopeType.SlopeDownLeft : SlopeType.SlopeDownRight);
                        toco |= PonerObsidiana(x, ys + 1);
                        toco |= PonerObsidiana(x, ys + 2);

                        // EL AIRE: tres filas de cabeza limpias sobre el escalón.
                        for (int y = ys - 3; y <= ys - 1; y++)
                        {
                            Tile t = Main.tile[x, y];
                            if (t != null && t.HasTile && !EsCasaDelJugador(t.TileType))
                            {
                                WorldGen.KillTile(x, y, noItem: true);
                                toco = true;
                            }
                        }
                    }
                }

                // === FASE D — LA FAUCE DE PICOS PRONUNCIADOS (v6.50.128 —
                //     la letra: «sobre la caida, seria mejor que los picos
                //     sean pronunciados y que el lugar de caida quede como
                //     un altar» + la referencia: TRES torres escalonadas
                //     de piedra oscura, la central la mayor — en 2D la
                //     columna central es del ALTAR y del haz, así que las
                //     DOS TORRES DEL UMBRAL (±17, altura 9, base 4 — el
                //     pico escalonado de la referencia, cada peldaño un
                //     bloque más estrecho, remate en /\) flanquean la
                //     entrada del cráter A DISTANCIA SEGURA: sus columnas
                //     (±15..±18) JAMÁS tocan las escaleras (±4..±12) ni
                //     el corredor de salida (±13..±14 libre — el PASO
                //     GARANTIZADO de la .124 sigue intacto), los colmillos
                //     de la garganta CRECEN (±3 → altura 7) y el labio del
                //     rim gana sus dientes (±9) ===
                toco |= PonerColmillo(cx + 3, BaseSolida(cx + 3, cy + 3, cy + radio), 7, 2);
                toco |= PonerColmillo(cx - 3, BaseSolida(cx - 3, cy + 3, cy + radio), 7, 2);
                toco |= PonerColmillo(cx + 2, BaseSolida(cx + 2, cy + 3, cy + radio), 4, 2);
                toco |= PonerColmillo(cx - 2, BaseSolida(cx - 2, cy + 3, cy + radio), 4, 2);
                toco |= PonerColmillo(cx + 9, BaseSolida(cx + 9, cy - 4, cy + radio), 3, 2);
                toco |= PonerColmillo(cx - 9, BaseSolida(cx - 9, cy - 4, cy + radio), 3, 2);
                toco |= PonerColmillo(cx + 17, BaseSolida(cx + 17, cy - 6, cy + radio), 9, 4);
                toco |= PonerColmillo(cx - 17, BaseSolida(cx - 17, cy - 6, cy + radio), 9, 4);

                // === FASE E — EL ALTAR DE LA CAÍDA (v6.50.128 — la letra:
                //     «el lugar de caida quede como un altar para añadir
                //     una nueva mecanica… la de comerse los item y mejorar
                //     el grimorio del eterno, para comer un item este debe
                //     corromperse primero y para eso se usara el lugar de
                //     caida» + la referencia: el bloque DORADO de espiral
                //     negro con su haz de luz violeta): el pedestal de
                //     obsidiana (3×2) en el fondo exacto… y la tile del
                //     ALTAR levitando sobre él. La columna de encima queda
                //     LIMPIA — ahí vive el haz que corrompe.) ===
                int pisoAltar = BaseSolida(cx, cy + 4, cy + radio);
                if (pisoAltar < 10 || pisoAltar >= Main.maxTilesY - 10) pisoAltar = cy + 8;
                for (int dx = -1; dx <= 1; dx++)
                {
                    toco |= PonerObsidiana(cx + dx, pisoAltar);
                    toco |= PonerObsidiana(cx + dx, pisoAltar + 1);
                }
                toco |= PonerAltar(cx, pisoAltar - 1);
                LaCaidaSistema.RegistrarAltar(cx, pisoAltar - 1);

                if (toco)
                {
                    // LA DIFUSIÓN: UN solo cuadrado que cubre la fauce
                    // entera (las torres del umbral llegan a ±18 — la caja
                    // creció con los picos pronunciados).
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendTileSquare(-1, cx - 20, cy - 12, 41, 26);
                    }
                }
            }
            catch { }
        }

        /// <summary>La primera tile de AIRE sobre la roca viva de la
        /// columna (busca hacia ABAJO desde <paramref name="yDesde"/>
        /// hasta <paramref name="yHasta"/>): dónde arraiga un colmillo.
        /// −1 si la columna no tiene roca que lo sostenga.</summary>
        private static int BaseSolida(int x, int yDesde, int yHasta)
        {
            if (x < 10 || x >= Main.maxTilesX - 10) return -1;
            if (yDesde < 10) yDesde = 10;
            if (yHasta >= Main.maxTilesY - 10) yHasta = Main.maxTilesY - 11;
            for (int y = yDesde; y <= yHasta; y++)
            {
                Tile t = Main.tile[x, y];
                if (t == null) continue;
                if (t.HasTile && Main.tileSolid[t.TileType] && !EsCasaDelJugador(t.TileType))
                    return y - 1;   // la tile de aire justo encima de la roca
            }
            return -1;
        }

        /// <summary>EL COLMILLO: columna de obsidiana que se AFILA al
        /// subir (ancho <paramref name="anchoBase"/> en la base → 2 en
        /// la punta) y REMATA en el PAR PUNTIAGUDO: la tile izquierda
        /// con la esquina superior derecha cortada (SlopeDownLeft) y la
        /// derecha con la superior izquierda (SlopeDownRight) — el pico
        /// /\ de la letra, de verdad. <paramref name="topeY"/> corta el
        /// crecimiento. IGNORA la casa del jugador (los dientes no
        /// muerden muebles).</summary>
        private static bool PonerColmillo(int bx, int yAire, int altura, int anchoBase, int? topeY = null)
        {
            if (yAire < 10 || yAire >= Main.maxTilesY - 10) return false;
            if (bx < 10 || bx >= Main.maxTilesX - 10) return false;

            bool toco = false;
            int yPunta = yAire;
            for (int h = 0; h < altura; h++)
            {
                int y = yAire - h;
                if (y < 10) break;
                if (topeY.HasValue && y <= topeY.Value) break;

                float f = (h + 1f) / altura;                     // 0 base … 1 punta
                int ancho = Math.Max(2, (int)MathF.Ceiling(anchoBase * (1f - f)));
                int x0 = bx - ancho / 2;    // base 2 → bx-1..bx (la columna bx+1 queda libre)
                for (int dx = 0; dx < ancho; dx++)
                    toco |= PonerObsidiana(x0 + dx, y);

                yPunta = y;
            }

            // LA PUNTA /\: las dos tiles del remate en pendiente — el
            // colmillo TERMINA EN PICO, no en ladrillo.
            for (int dx = 0; dx < 2; dx++)
            {
                int x = bx - 1 + dx;
                if (x < 10 || x >= Main.maxTilesX - 10) continue;
                Tile t = Main.tile[x, yPunta];
                if (t == null || !t.HasTile) continue;
                if (EsCasaDelJugador(t.TileType)) continue;
                t.Slope = dx == 0
                    ? SlopeType.SlopeDownLeft     // queda la mitad inferior-izquierda: /
                    : SlopeType.SlopeDownRight;   // queda la mitad inferior-derecha: \
                WorldGen.SquareTileFrame(x, yPunta, true);
                toco = true;
            }
            return toco;
        }

        /// <summary>UNA TILE EN PENDIENTE de obsidiana — la superficie
        /// CAMINABLE de las escaleras (la técnica de las colinas de
        /// vanilla: se sube y se baja CAMINANDO).</summary>
        private static bool Pender(int x, int y, SlopeType pendiente)
        {
            if (!PonerObsidiana(x, y)) return false;
            Tile t = Main.tile[x, y];
            if (t == null) return false;
            t.Slope = pendiente;
            WorldGen.SquareTileFrame(x, y, true);
            return true;
        }

        /// <summary>v6.50.128 — LA TILE DEL ALTAR: el bloque dorado de la
        /// espiral (la referencia) plantado en el fondo del cráter. Es la
        /// puerta de LA CORRUPCIÓN DE ITEMS — intocable (la protege un
        /// GlobalTile) y con luz propia (el altar LATE).</summary>
        private static bool PonerAltar(int x, int y)
        {
            if (x < 10 || y < 10 || x >= Main.maxTilesX - 10 || y >= Main.maxTilesY - 10) return false;
            Tile t = Main.tile[x, y];
            if (t == null) return false;
            if (t.HasTile && EsCasaDelJugador(t.TileType)) return false;
            t.HasTile = true;
            t.TileType = (ushort)ModContent.TileType<global::AethonMod.Content.Tiles.AltarDeLaCaida>();
            t.Slope = 0;
            t.IsHalfBlock = false;
            t.TileFrameX = 0;
            t.TileFrameY = 0;
            WorldGen.SquareTileFrame(x, y, true);
            return true;
        }

        /// <summary>Una tile de obsidiana limpia (respeta la casa del jugador).</summary>
        private static bool PonerObsidiana(int x, int y)
        {
            if (x < 10 || y < 10 || x >= Main.maxTilesX - 10 || y >= Main.maxTilesY - 10) return false;
            Tile t = Main.tile[x, y];
            if (t == null) return false;
            if (t.HasTile && EsCasaDelJugador(t.TileType)) return false;
            t.HasTile = true;
            t.TileType = TileID.Obsidian;
            t.Slope = 0;
            t.IsHalfBlock = false;
            t.TileFrameX = 0;
            t.TileFrameY = 0;
            WorldGen.SquareTileFrame(x, y, true);
            return true;
        }

        /// <summary>¿Es la roca común del mundo? (piedra, tierra o pasto —
        /// el aro se tintará SOLO sobre roca natural: ni minerales, ni
        /// plantas, ni nada que el jugador haya puesto con intención).</summary>
        private static bool EsRocaNatural(int tipo)
        {
            return tipo == TileID.Stone || tipo == TileID.Dirt ||
                   tipo == TileID.Grass || tipo == TileID.Ebonstone;
        }

        /// <summary>¿Es la casa del jugador? (cofres, mesas, puertas, pinturas — la casa no se rompe).</summary>
        private static bool EsCasaDelJugador(int tipo)
        {
            for (int i = 0; i < CasaDelJugador.Length; i++)
                if (CasaDelJugador[i] == tipo) return true;
            return false;
        }

        /// <summary>La estela física del cometa (polvo morado, sin gravedad).</summary>
        private static void PolvoEstela(Vector2 pos, int cantidad)
        {
            if (Main.dedServ) return;
            for (int i = 0; i < cantidad; i++)
            {
                Dust d = Dust.NewDustPerfect(pos, DustID.PurpleTorch,
                    new Vector2(Main.rand.NextFloat(-1.4f, 1.4f), Main.rand.NextFloat(-1f, 1f)),
                    170, new Color(120, 40, 190), Main.rand.NextFloat(0.7f, 1.2f));
                d.noGravity = true;
            }
        }

        /// <summary>Un sonido en el mundo (la autoridad lo difunde; el dedicado no lo oye).</summary>
        private static void Sonar(Terraria.Audio.SoundStyle style, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(style, pos); }
            catch { }
        }

        // ================================================================
        //  EL DIBUJO — la estrella negra de fondo
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            VFXCore.CerrarLoteSiAbierto();
            try { DibujarCometa(); }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false; // el cometa SE dibuja solo (100% procedural)
        }

        private void DibujarCometa()
        {
            float time = Main.GlobalTimeWrappedHourly;
            bool enCielo = Projectile.ai[0] == 0f;

            // === LA POSICIÓN DE FONDO ===
            // FASE 0: la estrella cuelga de la PARTE ALTA de la pantalla
            // (mientras su cuerpo REAL vive por encima de la cámara, el
            // dibujo la clava al cielo visible) — el momento en que el
            // picado la BAJA a la pantalla, el cuerpo real toma el mando
            // y la transición es continua (el clamp solo empuja HACIA
            // ARRIBA, nunca de lado).
            Vector2 pos = Projectile.Center - Main.screenPosition;
            if (enCielo)
                pos.Y = System.Math.Max(pos.Y, 70f + MathF.Sin(time * 0.8f) * 12f);

            // === LA ESCALA: chica en el cielo (0.45 — lo lejos), real en
            //     el picado (crece al acercarse a su cita) ===
            float dist = Vector2.Distance(Projectile.Center,
                new Vector2(Projectile.ai[1] * 1000f, Projectile.ai[2] * 1000f));
            float cercania = MathHelper.Clamp(1f - dist / 2600f, 0f, 1f);
            float escala = enCielo ? 0.45f : MathHelper.Lerp(0.45f, 1.15f, cercania);

            Vector2 rumbo = Projectile.velocity;
            if (rumbo.LengthSquared() < 0.01f) rumbo = Vector2.UnitY; // jamás debería: siempre vuela
            rumbo = Vector2.Normalize(rumbo);

            // ====== PASO 1 — LA ESTELA (aditivo): 14 segmentos decrecientes
            // ====== hacia atrás con alpha decreciente — la firma del cometa.
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                int segmentos = enCielo ? 10 : 14; // el picado la alarga
                float largo = enCielo ? 34f : 52f;
                for (int i = 0; i < segmentos; i++)
                {
                    float f = i / (float)segmentos;                       // 0=cabeza … 1=cola
                    float tam = (26f - 16f * f) * escala;                // decrece hacia atrás
                    float alfa = (0.34f - 0.30f * f) * escala;          // se apaga hacia atrás
                    // Cada segmento REZA un poco alrededor del eje (la
                    // tinta no vuela en línea recta).
                    float rezo = MathF.Sin(time * 2.1f + i * 0.9f) * (3f + 5f * f) * escala;
                    Vector2 segPos = pos - rumbo * (largo * f * escala) +
                        new Vector2(-rumbo.Y, rumbo.X) * rezo;
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, segPos, null,
                        SombrasLib.Alfa(EstelaMorada, alfa), rumbo.ToRotation() + MathHelper.PiOver2,
                        VFXCore.SoftGlow.Size() * 0.5f, tam / 64f, SpriteEffects.None, 0f);
                }
            }
            catch { }
            Main.spriteBatch.End();

            // ====== PASO 2 — EL NÚCLEO (alfa): la elipse de TINTA que
            // ====== OSCURECE — negro sobre lo que sea, SIN lighting
            // ====== (la estrella de fondo no conoce el sol del mundo).
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                // La corona morada (aditiva no puede: la tinta OSCURECE —
                // esto es la esfera violeta que asoma en el borde).
                Main.spriteBatch.Draw(VFXCore.SoftGlow, pos, null,
                    SombrasLib.Alfa(EstelaMorada, 0.30f * escala),
                    0f, VFXCore.SoftGlow.Size() * 0.5f, escala, SpriteEffects.None, 0f);
                // EL NÚCLEO: casi negro — la estrella misma.
                Main.spriteBatch.Draw(VFXCore.SoftGlow, pos, null,
                    SombrasLib.Alfa(NucleoTinta, 0.94f),
                    0f, VFXCore.SoftGlow.Size() * 0.5f, 0.68f * escala, SpriteEffects.None, 0f);
                // EL LATIDO: una chispa morada en el corazón (tinta viva).
                float latido = 0.5f + 0.5f * MathF.Sin(time * 3.2f);
                Main.spriteBatch.Draw(VFXCore.SoftGlow, pos, null,
                    SombrasLib.Alfa(EstelaMorada, 0.22f * escala * latido),
                    0f, VFXCore.SoftGlow.Size() * 0.5f, 0.32f * escala, SpriteEffects.None, 0f);
            }
            catch { }
            Main.spriteBatch.End();
        }
    }
}
