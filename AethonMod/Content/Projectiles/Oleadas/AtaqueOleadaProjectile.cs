using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Oleadas
{
    /// <summary>
    /// AtaqueOleadaProjectile — v6.48 — LOS DIENTES DE LOS GUARDIANES.
    ///
    /// LOS ATAQUES NUEVOS de los jefes de las oleadas: cada guardián
    /// convocado por la furia del grimorio tiene un ARMA PROPIA hecha con
    /// las librerías de la casa (OndaLib, TajoLib, StormLib, LumenLib y
    /// VFXCore) — corresponden con su tema, su apariencia y su bioma:
    ///
    ///   Estilo 0 — REY GELATINA, "El Sello del Trono": las cuentas de la
    ///     corona — orbes turquesa en anillo giratorio que se abren hacia
    ///     fuera (OndaLib.Pulse al paso + la voz del rey).
    ///   Estilo 1 — OJO DE CTHULHU, "Los Tajos del Vigía": marcas carmesí
    ///     telegrafiadas (OndaLib.Pulse contraído) y CORTES DIFERIDOS
    ///     (TajoLib) — el arco aparece donde estabas, no donde estás.
    ///   Estilo 2 — DEERCLOPS, "Látigos de Escarcha": espinas de hielo
    ///     que caen del cielo en zigzag (StormLib.ChainBolt) y estallan
    ///     al clavarse (polvo de hielo + sonido).
    ///   Estilo 3 — ABEJA REINA, "El Abanico de Aguijones": cinco
    ///     aguijones dorados en abanico con corrección de rumbo — la
    ///     reina no perdona el errar.
    ///   Estilo 4 — DEVORADOR, "Las Fauces Corruptas": relámpagos
    ///     corruptos que CURVAN hacia la presa (FractalBolt morado con
    ///     búsqueda — la boca que persigue).
    ///   Estilo 5 — CEREBRO, "El Estallido Carmesí": ráfaga radial de
    ///     reflejos — el estallido psíquico, corto y violento.
    ///   Estilo 6 — SKELETRON, "Las Calaveras en Órbita": calaveras
    ///     girando alrededor de un centro a la deriva que SE LANZAN al
    ///     portador cuando el hambre lo ordena.
    ///
    /// DAÑO por colisión del MOTOR (hostil, hitbox honesta — nada de
    /// daño manual: la casa prefiere la física del juego). El daño y la
    /// cadencia los fija OleadaNPC al convocarlos (escalan con la oleada:
    /// ×(k+1); la ESPECIAL ×15). Cero Main.rand en el render; SIN hide
    /// (la lección v6.35); el contrato de lote cerrado→cerrado.
    /// </summary>
    public class AtaqueOleadaProjectile : ModProjectile
    {
        // === LOS ESTILOS (ai[0]): un diente por guardián ===
        public const int EstiloReyGelatina = 0;
        public const int EstiloOjo = 1;
        public const int EstiloDeerclops = 2;
        public const int EstiloAbeja = 3;
        public const int EstiloDevorador = 4;
        public const int EstiloCerebro = 5;
        public const int EstiloSkeletron = 6;

        /// <summary>El estilo del diente (ai[0]).</summary>
        private int Estilo => (int)Projectile.ai[0];
        /// <summary>Semilla determinista (ai[2]).</summary>
        private int Seed => Math.Max(1, (int)Projectile.ai[2] % 9973);

        // === EL ESTADO (por estilo) ===
        private float _edad;
        private Vector2 _centroOrbita; // el anillo de las cuentas/calaveras
        private bool _lanzada;         // la calavera ya se lanzó
        private Vector2 _posAnterior;  // para el relámpago del devorador

        public override string Texture => "AethonMod/Content/Projectiles/Cosmetic/AnillosSingularesHalo";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0.6f;
            // v6.50.2 — quien entre a media oleada recibe los dientes
            // (vanilla lo hace con los proyectiles de jefe — la misma
            // regla que su hermano AtaqueJefeProjectile).
            Projectile.netImportant = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            _posAnterior = Projectile.Center;
            _centroOrbita = Projectile.Center;
        }

        /// <summary>La presa más cercana (el portador del libro).</summary>
        private Player Presa()
        {
            Player mejor = null;
            float mejorD = float.MaxValue;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p == null || !p.active || p.dead) continue;
                float d = Vector2.DistanceSquared(p.Center, Projectile.Center);
                if (d < mejorD) { mejorD = d; mejor = p; }
            }
            return mejor;
        }

        public override void AI()
        {
            _edad++;

            // v6.50.2 — FIX (MP: _centroOrbita no viaja): OnSpawn NO corre
            // en los clientes que reciben el proyectil por red (msg 27) →
            // el anillo de las cuentas/calaveras quedaba centrado en (0,0)
            // (esquina del mundo — teletransporte visual y cero amenaza en
            // las pantallas remotas). Autocuración: el centro se reconstruye
            // desde la posición recibida.
            if (_centroOrbita == Vector2.Zero) _centroOrbita = Projectile.Center;

            Player presa = Presa();

            switch (Estilo)
            {
                // =============================================================
                //  EL SELLO DEL TRONO — las cuentas de la corona del Rey
                // =============================================================
                case EstiloReyGelatina:
                {
                    // El anillo gira y SE ABRE hacia fuera (la corona
                    // estallando en cuentas). ai[1] = ángulo base.
                    float ang = Projectile.ai[1] + _edad * 0.030f;
                    float radio = 70f + _edad * 1.35f; // se abre
                    Projectile.Center = _centroOrbita +
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio;
                    if (_edad >= 150) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  LOS TAJOS DEL VIGÍA — marca telegrafiada + corte diferido
                // =============================================================
                case EstiloOjo:
                {
                    if (_edad < 26)
                    {
                        // LA MARCA: el proyectil YA voló a la marca (la fija
                        // el convocador cerca de la presa) y ESPERA — la
                        // tensión antes del corte.
                        Projectile.velocity *= 0.80f;
                    }
                    else if (_edad == 26)
                    {
                        // EL CORTE: arranque violento — el tajo aparece de
                        // golpe en la dirección que trae (ai[1]).
                        float a = Projectile.ai[1];
                        Projectile.velocity = new Vector2(MathF.Cos(a), MathF.Sin(a)) * 13f;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item71, Projectile.Center);
                    }
                    else
                    {
                        if (_edad > 70) Projectile.Kill();
                    }
                    break;
                }

                // =============================================================
                //  LOS LÁTIGOS DE ESCARCHA — caída en zigzag + estallido
                // =============================================================
                case EstiloDeerclops:
                {
                    // Cae con vaivén (el látigo del invierno).
                    Projectile.velocity.X = MathF.Sin(_edad * 0.35f + Seed) * 2.6f;
                    Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.28f, 11f);

                    // ¿CLAVADO? (suelo debajo o edad límite)
                    Point tile = Projectile.Center.ToTileCoordinates();
                    bool clavado = false;
                    if (tile.X > 5 && tile.X < Main.maxTilesX - 5 &&
                        tile.Y > 5 && tile.Y < Main.maxTilesY - 5)
                    {
                        Tile tl = Main.tile[tile.X, tile.Y + 1];
                        clavado = tl != null && tl.HasTile && Main.tileSolid[tl.TileType];
                    }
                    if (clavado || _edad > 200)
                    {
                        // EL ESTALLIDO: el polvo del impacto + fin del látigo.
                        for (int d = 0; d < 6; d++)
                        {
                            int idx = Dust.NewDust(Projectile.Center, 10, 10,
                                DustID.IceTorch,
                                -Main.rand.NextFloat(3f, 6f) * MathF.Cos(d * MathHelper.TwoPi / 6f),
                                -Main.rand.NextFloat(2f, 5f));
                            Main.dust[idx].noGravity = true;
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item30, Projectile.Center);
                        Projectile.Kill();
                    }
                    break;
                }

                // =============================================================
                //  EL ABANICO DE AGUIJONES — corrección de rumbo real
                // =============================================================
                case EstiloAbeja:
                {
                    if (presa != null && _edad < 40)
                    {
                        Vector2 deseada = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 11f;
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, deseada, 0.06f);
                    }
                    if (_edad > 90) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  LAS FAUCES CORRUPTAS — el relámpago que CURVA a la presa
                // =============================================================
                case EstiloDevorador:
                {
                    if (presa != null)
                    {
                        Vector2 lado = new Vector2(-Projectile.velocity.Y, Projectile.velocity.X)
                            .SafeNormalize(Vector2.Zero);
                        // La curva: aceleración lateral hacia la presa (la
                        // boca que persigue, no la bala que va recta).
                        float signo = Vector2.Dot(lado, presa.Center - Projectile.Center) >= 0 ? 1f : -1f;
                        Projectile.velocity += lado * (0.16f * signo);
                        float vel = Projectile.velocity.Length();
                        if (vel > 9f) Projectile.velocity *= 9f / vel;
                    }
                    _posAnterior = Projectile.Center - Projectile.velocity * 2.2f;
                    if (_edad > 110) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  EL ESTALLIDO CARMESÍ — ráfaga radial corta y violenta
                // =============================================================
                case EstiloCerebro:
                {
                    if (_edad > 46) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  LAS CALAVERAS EN ÓRBITA — giran, a la deriva, y SE LANZAN
                // =============================================================
                case EstiloSkeletron:
                {
                    // El centro a la deriva hacia la presa (lento).
                    if (presa != null && _edad % 8 == 0)
                    {
                        Vector2 dir = (presa.Center - _centroOrbita).SafeNormalize(Vector2.Zero);
                        _centroOrbita += dir * 3.2f;
                    }

                    if (!_lanzada && _edad < 120)
                    {
                        // LA ÓRBITA: la calavera gira en su anillo.
                        float ang = Projectile.ai[1] + _edad * 0.045f;
                        Projectile.Center = _centroOrbita +
                            new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 92f;
                        Projectile.velocity = Vector2.Zero;
                    }
                    else if (!_lanzada)
                    {
                        // EL HAMBRE LO ORDENA: la calavera se lanza a la presa.
                        _lanzada = true;
                        if (presa != null)
                        {
                            Projectile.velocity = (presa.Center - Projectile.Center)
                                .SafeNormalize(Vector2.UnitY) * 14f;
                        }
                    }
                    if (_edad > 210) Projectile.Kill();
                    break;
                }
            }

            // La luz del diente (el color propio de su furia).
            if (Estilo == EstiloOjo || Estilo == EstiloCerebro)
                Lighting.AddLight(Projectile.Center, new Vector3(0.45f, 0.08f, 0.10f));
            else if (Estilo == EstiloDevorador)
                Lighting.AddLight(Projectile.Center, new Vector3(0.24f, 0.08f, 0.42f));
            else if (Estilo == EstiloSkeletron)
                Lighting.AddLight(Projectile.Center, new Vector3(0.30f, 0.28f, 0.22f));
        }

        // ==================================================================
        //  EL RENDER — cada diente con SU librería. EL FLUJO DE LA CASA:
        //  (1) los quads al BÚFER (coords de MUNDO) y su volcado propio;
        //  (2) el lote aditivo NUESTRO para las librerías de coords de
        //  PANTALLA (OndaLib/StormLib/TajoLib/LumenLib/OrbitaLib);
        //  (3) el lote del pase reabierto TAL CUAL (cerrado→cerrado).
        // ==================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 pos = Projectile.Center - Main.screenPosition;

                // === FASE 1 — EL BÚFER DE QUADS (coords de MUNDO) ===
                switch (Estilo)
                {
                    case EstiloReyGelatina:
                        VFXCore.Quad(Projectile.Center, new Color(62, 219, 191) * 0.75f,
                            new Vector2(22f, 22f));
                        VFXCore.Quad(Projectile.Center, new Color(190, 255, 240) * 0.6f,
                            new Vector2(10f, 10f));
                        break;

                    case EstiloDeerclops:
                        VFXCore.Quad(Projectile.Center, new Color(230, 245, 255) * 0.8f,
                            new Vector2(12f, 18f), Projectile.velocity.ToRotation() + MathHelper.PiOver2);
                        break;

                    case EstiloAbeja:
                        VFXCore.Quad(Projectile.Center, new Color(255, 192, 55) * 0.85f,
                            new Vector2(18f, 7f), Projectile.velocity.ToRotation());
                        VFXCore.Line(Projectile.Center - Projectile.velocity * 2f,
                            Projectile.Center, new Color(255, 220, 130) * 0.4f, 3f);
                        break;

                    case EstiloCerebro:
                        VFXCore.Quad(Projectile.Center,
                            new Color(255, 102, 140) * (0.85f * (1f - _edad / 46f)),
                            new Vector2(16f, 16f));
                        break;

                    case EstiloSkeletron:
                        VFXCore.Quad(Projectile.Center, new Color(216, 208, 180) * 0.9f,
                            new Vector2(26f, 24f));
                        VFXCore.Quad(Projectile.Center + new Vector2(-6f, -3f),
                            new Color(20, 16, 20) * 0.8f, new Vector2(7f, 7f));
                        VFXCore.Quad(Projectile.Center + new Vector2(6f, -3f),
                            new Color(20, 16, 20) * 0.8f, new Vector2(7f, 7f));
                        if (_lanzada)
                            VFXCore.Quad(Projectile.Center + new Vector2(0f, 3f),
                                new Color(216, 60, 50) * 0.9f, new Vector2(8f, 6f));
                        break;
                }
                VFXCore.FlushAdditive(null, false);

                // === FASE 2 — EL LOTE ADITIVO DE LAS LIBRERÍAS (PANTALLA) ===
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                switch (Estilo)
                {
                    // === EL SELLO DEL TRONO: la cuenta + su pulso ===
                    case EstiloReyGelatina:
                    {
                        float prog = (_edad % 30f) / 30f;
                        OndaLib.Pulse(Main.spriteBatch, pos, prog, 44f,
                            new Color(62, 219, 191), 0.55f, Seed);
                        LumenLib.Bloom(Main.spriteBatch, pos, 18f,
                            new Color(62, 219, 191), 0.6f);
                        break;
                    }

                    // === LOS TAJOS DEL VIGÍA: la marca, luego el corte ===
                    case EstiloOjo:
                    {
                        if (_edad <= 25)
                        {
                            float prog = _edad / 25f;
                            OndaLib.Pulse(Main.spriteBatch, pos, 1f - prog, 54f,
                                new Color(226, 64, 64), 0.8f, Seed);
                            LumenLib.Bloom(Main.spriteBatch, pos, 26f,
                                new Color(226, 64, 64), 0.5f + 0.4f * prog);
                        }
                        else
                        {
                            float prog = MathF.Min(1f, (_edad - 26f) / 8f);
                            float ang = Projectile.velocity.ToRotation();
                            TajoLib.Tajo(pos, 46f,
                                ang - 0.55f, ang + 0.55f,
                                prog, 1f - prog * 0.3f, 11f,
                                new Color(226, 64, 64), new Color(255, 238, 230),
                                Seed, t);
                            LumenLib.Bloom(Main.spriteBatch, pos, 24f,
                                new Color(226, 64, 64), 0.7f * (1f - prog));
                        }
                        break;
                    }

                    // === LOS LÁTIGOS DE ESCARCHA: el zigzag helado ===
                    case EstiloDeerclops:
                    {
                        Vector2 cola = pos - Projectile.velocity * 2.4f;
                        StormLib.ChainBolt(Main.spriteBatch, cola, pos, Seed,
                            StormLib.FlickTick(t, 12f), 2.6f,
                            new Color(120, 190, 235), new Color(230, 245, 255), 0.85f);
                        LumenLib.Bloom(Main.spriteBatch, pos, 22f,
                            new Color(168, 220, 242), 0.8f);
                        break;
                    }

                    // === EL ABANICO DE AGUIJONES: el aguijón dorado ===
                    case EstiloAbeja:
                    {
                        LumenLib.Bloom(Main.spriteBatch, pos, 18f,
                            new Color(255, 192, 55), 0.55f);
                        break;
                    }

                    // === LAS FAUCES CORRUPTAS: el relámpago morado curvado ===
                    case EstiloDevorador:
                    {
                        Vector2 cola = _posAnterior - Main.screenPosition;
                        StormLib.FractalBolt(Main.spriteBatch, cola, pos, Seed,
                            StormLib.FlickTick(t, 14f), 2.8f,
                            new Color(140, 60, 220), new Color(230, 190, 255), 0.9f);
                        LumenLib.Bloom(Main.spriteBatch, pos, 24f,
                            new Color(170, 102, 235), 0.75f);
                        break;
                    }

                    // === EL ESTALLIDO CARMESÍ: el reflejo que se apaga ===
                    case EstiloCerebro:
                    {
                        if (_edad < 10)
                        {
                            float prog = _edad / 10f;
                            TajoLib.Tajo(pos, 38f,
                                Projectile.velocity.ToRotation() - 0.4f,
                                Projectile.velocity.ToRotation() + 0.4f,
                                prog, 1f - _edad / 46f, 8f,
                                new Color(255, 102, 140), new Color(255, 236, 230),
                                Seed, t);
                        }
                        LumenLib.Bloom(Main.spriteBatch, pos, 20f,
                            new Color(255, 102, 140), 0.6f * (1f - _edad / 46f));
                        break;
                    }

                    // === LAS CALAVERAS EN ÓRBITA: la calavera de luz ===
                    case EstiloSkeletron:
                    {
                        OrbitaLib.AnilloFino(pos, 22f, t * 1.2f,
                            OrbitaLib.Tint(new Color(216, 208, 180), 0.35f));
                        LumenLib.Bloom(Main.spriteBatch, pos, 24f,
                            new Color(216, 208, 180), 0.5f);
                        break;
                    }
                }
                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
            finally
            {
                if (wasActive)
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                        Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                        null, Main.Transform);
            }
            return false;
        }
    }
}
