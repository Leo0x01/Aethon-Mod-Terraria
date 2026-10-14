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

        /// <summary>El cráter suave con su RASTRO OSCURO (v6.50.118): radio
        /// 11 tiles, contenedores/mesas/puertas/pinturas intactos, bordes
        /// irregulares — y la tinta que salpicó queda en la roca: PIEDRA DE
        /// ÉBANO y OBSIDIANA (bloques EXISTENTES de vanilla, la letra: «no
        /// necesita bloques nuevos se pueden reutilizar los bloques
        /// existentes»).</summary>
        private static void ExcavarCrater(Vector2 centro)
        {
            try
            {
                int cx = (int)(centro.X / 16f);
                int cy = (int)(centro.Y / 16f);
                const int radio = 11;
                bool toco = false;

                for (int x = cx - radio; x <= cx + radio; x++)
                {
                    for (int y = cy - radio; y <= cy + radio; y++)
                    {
                        if (x < 10 || y < 10 || x >= Main.maxTilesX - 10 || y >= Main.maxTilesY - 10) continue;
                        float d = MathF.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                        if (d > radio) continue;

                        Tile tile = Main.tile[x, y];
                        if (tile == null) continue;

                        // LA CASA DEL JUGADOR: intacta y SIN rastro encima.
                        if (tile.HasTile && EsCasaDelJugador(tile.TileType)) continue;

                        // EL BORDE IRREGULAR: el corazón (≤8 tiles) se
                        // abre siempre; los 3 externos solo a medias.
                        if (tile.HasTile && (d <= 8f || Main.rand.NextBool(2)))
                        {
                            WorldGen.KillTile(x, y, noItem: true);
                            toco = true;
                        }

                        // v6.50.118 — EL RASTRO DEL COMETA (la letra: «el
                        // cráter debe dejar algún rastro de algún bloque
                        // oscuro… se pueden reutilizar los bloques
                        // existentes»): donde la tinta golpeó queda PIEDRA
                        // DE ÉBANO (el bloque oscuro de la Corrupción, con
                        // sus vetas moradas — negro-morado como el cometa)
                        // y algo de OBSIDIANA (la tinta quemada por la
                        // caída). Esparcida y orgánica: más densa hacia el
                        // fondo, jamás en el corazón exacto (el libro
                        // duerme ahí, flotando).
                        if (!tile.HasTile && d >= 2.5f)
                        {
                            float dentro = 1f - d / (float)radio;  // 1 centro → 0 borde
                            if (Main.rand.NextFloat() < 0.26f + 0.34f * dentro)
                            {
                                tile.HasTile = true;
                                tile.TileType = Main.rand.NextFloat() < 0.80f
                                    ? TileID.Ebonstone
                                    : TileID.Obsidian;
                                tile.Slope = 0;
                                tile.IsHalfBlock = false;
                                tile.TileFrameX = 0;
                                tile.TileFrameY = 0;
                                WorldGen.SquareTileFrame(x, y, true);
                                toco = true;
                            }
                        }
                        else if (tile.HasTile && d > 8f &&
                                 EsRocaNatural(tile.TileType) && Main.rand.NextFloat() < 0.35f)
                        {
                            // EL ARO TINTADO: el borde que no se abrió
                            // amanece ÉBANO — desde lejos, el cráter
                            // entero lee «aquí cayó algo oscuro».
                            tile.TileType = TileID.Ebonstone;
                            WorldGen.SquareTileFrame(x, y, true);
                            toco = true;
                        }
                    }
                }

                if (toco)
                {
                    // LA DIFUSIÓN: UN solo cuadrado que cubre el cráter
                    // entero (más barato y más atómico que zona por zona).
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendTileSquare(-1, cx - radio - 1, cy - radio - 1,
                            (radio + 1) * 2 + 1, (radio + 1) * 2 + 1);
                    }
                }
            }
            catch { }
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
