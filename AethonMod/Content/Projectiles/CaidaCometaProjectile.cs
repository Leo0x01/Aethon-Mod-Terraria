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

        /// <summary>v6.50.129 — EL CRÁTER SOMERO DEL MINIBIOMA (la
        /// letra nueva: «el crater debe tener menos profundidad, el
        /// centro debe estar vacio solo obsidiana y ademas bruma, que
        /// sea como un pequeño minibioma» + «quita eso amarillo que esta
        /// dentro del crater»). La .128 cavaba una FAUCE PROFUNDA (8
        /// tiles) con escaleras, colmillos de garganta y un ALTAR
        /// DORADO en el fondo — TODO ESO MUERE:
        /// · LA LENTE: la excavación es una elipse ANCHA Y BAJA (semiejes
        ///   9 × 4,6 — el doble de ancha que profunda: «menos
        ///   profundidad»).
        /// · LA CONCHA: el fondo es UNA CONCHA DE OBSIDIANA de 3 tiles
        ///   que sigue la curva del cuenco, con cada escalón en
        ///   PENDIENTE CAMINABLE (la técnica de las colinas de vanilla —
        ///   el paso garantizado POR CONSTRUCCIÓN: el cráter SE CAMINA,
        ///   sin escaleras).
        /// · EL CENTRO VACÍO: ni altar, ni pedestal, ni colmillos — solo
        ///   la concha y la BRUMA que respira encima (la zona de
        ///   corrupción vive REGISTRADA en LaCaidaSistema, sin tile: la
        ///   bruma ES la puerta — AltarDevoradorSistema la dibuja).
        /// · LOS PICOS DEL BORDE siguen (la letra de la .128): LAS DOS
        ///   TORRES DEL UMBRAL (±17, altura 9) — lejos del camino de
        ///   salida (±10..±14 libre), el labio viste su aro de obsidiana.
        /// · LA CASA DEL JUGADOR INTACTA (la lista de siempre).</summary>
        private static void ExcavarCrater(Vector2 centro)
        {
            try
            {
                int cx = (int)(centro.X / 16f);
                int cy = (int)(centro.Y / 16f);
                bool toco = false;
                const int semiX = 9;
                const float semiY = 4.6f;

                // === LA CURVA DEL FONDO (el offset de cada columna,
                //     pasos de ≤1 — la escalera suave del minibioma:
                //     5,5,4,4,4,4,3,3,2,1): el bruto de la elipse
                //     recortado a escalones de UN tile para que TODO el
                //     descenso sea pendiente caminable. ===
                int[] fondo = new int[semiX + 1];
                for (int fx = 0; fx <= semiX; fx++)
                {
                    int bruto = (int)MathF.Round(semiY *
                        MathF.Sqrt(1f - (fx / (float)semiX) * (fx / (float)semiX)));
                    if (fx == 0) fondo[fx] = bruto;
                    else fondo[fx] = (int)MathHelper.Clamp(bruto, fondo[fx - 1] - 1, fondo[fx - 1]);
                }

                for (int dx = -semiX; dx <= semiX; dx++)
                {
                    int x = cx + dx;
                    if (x < 10 || x >= Main.maxTilesX - 10) continue;
                    int prof = fondo[Math.Abs(dx)];

                    // === FASE A — LA CAVIDAD (la lente se abre de arriba
                    //     a la curva; la casa del jugador intacta) ===
                    for (int y = cy - 6; y <= cy + prof; y++)
                    {
                        if (y < 10 || y >= Main.maxTilesY - 10) continue;
                        Tile tile = Main.tile[x, y];
                        if (tile == null) continue;
                        if (tile.HasTile && EsCasaDelJugador(tile.TileType)) continue;
                        if (tile.HasTile)
                        {
                            WorldGen.KillTile(x, y, noItem: true);
                            toco = true;
                        }
                    }

                    // === FASE B — LA CONCHA DE OBSIDIANA (3 tiles de
                    //     grosor siguiendo la curva: «el centro… solo
                    //     obsidiana») con la superficie en PENDIENTE
                    //     donde el siguiente escalón baja — el cráter SE
                    //     CAMINA hacia el centro y hacia afuera. ===
                    int sup = cy + prof;
                    for (int dy = 0; dy < 3; dy++)
                        toco |= PonerObsidiana(x, sup + dy);

                    int haciaCentro = dx < 0 ? dx + 1 : dx - 1;
                    if (haciaCentro != dx && Math.Abs(haciaCentro) <= semiX)
                    {
                        int profVecino = fondo[Math.Abs(haciaCentro)];
                        if (profVecino > prof)
                        {
                            Tile t = Main.tile[x, sup];
                            if (t != null && t.HasTile && !EsCasaDelJugador(t.TileType))
                            {
                                // El mapa de la .124 (verificado con su
                                // simulador): bajar hacia la DERECHA →
                                // SlopeDownLeft; bajar hacia la IZQUIERDA
                                // → SlopeDownRight.
                                t.Slope = dx < 0 ? SlopeType.SlopeDownLeft
                                    : SlopeType.SlopeDownRight;
                                WorldGen.SquareTileFrame(x, sup, true);
                                toco = true;
                            }
                        }
                    }

                    // EL ARO DEL LABIO (|dx| ≥ 8): la primera roca natural
                    // de la salida viste obsidiana — el marco del
                    // minibioma (el camino de salida ±10..±14 queda
                    // LIBRE de dientes: las torres viven en ±15..±18).
                    if (Math.Abs(dx) >= 8)
                    {
                        for (int y = cy - 2; y <= cy + 4; y++)
                        {
                            if (y < 10 || y >= Main.maxTilesY - 10) continue;
                            Tile t = Main.tile[x, y];
                            if (t != null && t.HasTile && EsRocaNatural(t.TileType))
                            {
                                t.TileType = TileID.Obsidian;
                                WorldGen.SquareTileFrame(x, y, true);
                                toco = true;
                                break;
                            }
                        }
                    }
                }

                // === FASE C — LOS PICOS DEL BORDE (la letra de la .128
                //     sigue viva: «los picos sean pronunciados»): LAS DOS
                //     TORRES DEL UMBRAL (±17, altura 9, base 4 — el pico
                //     escalonado de la referencia). Los colmillos de la
                //     garganta (±2, ±3) y el labio dentado (±9) MURIERON:
                //     bloqueaban la concha caminable — el centro y la
                //     salida son del minibioma. ===
                toco |= PonerColmillo(cx + 17, BaseSolida(cx + 17, cy - 6, cy + 8), 9, 4);
                toco |= PonerColmillo(cx - 17, BaseSolida(cx - 17, cy - 6, cy + 8), 9, 4);

                // === FASE D — EL MINIBIOMA QUEDA REGISTRADO (v6.50.129:
                //     el ALTAR DORADO murió — «quita eso amarillo»; sin
                //     pedestal, sin tile: el centro queda VACÍO, solo la
                //     concha de obsidiana y la bruma encima. LA ZONA DE
                //     CORRUPCIÓN vive en LaCaidaSistema: las armas que
                //     caigan a la concha se corrompen — la puerta es la
                //     BRUMA, no un bloque). ===
                LaCaidaSistema.RegistrarAltar(cx, cy + fondo[0] - 1);

                // v6.50.130 — EL CRÁTER SUELTA SU BRUMA (la letra: «en el
                // crater debe estar un item invisible que es la bruma en
                // si de esa forma los jugadores pueden tomarla y colocarla
                // donde quieran»): el item de bruma nace flotando en el
                // corazón del minibioma — la bruma que se ve ES el item.
                LaCaidaSistema.EntregarBruma(cx, cy + fondo[0] - 1);

                if (toco)
                {
                    // LA DIFUSIÓN: UN solo cuadrado que cubre torres +
                    // cuenco (las torres arrancan en la roca viva de ±15:
                    // en ladera su punta llega a cy-15 — la caja la cubre).
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendTileSquare(-1, cx - 20, cy - 16, 41, 30);
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
