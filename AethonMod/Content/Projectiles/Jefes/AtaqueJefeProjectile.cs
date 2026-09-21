using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Jefes
{
    /// <summary>
    /// AtaqueJefeProjectile — v6.48 — LOS DIENTES DE LOS CINCO.
    ///
    /// EL ARSENAL COMPLETO de los jefes del MOD (Aethon · el Titán Hueco
    /// · el Guardián del Rift · la Arquera · el Primer Portador): cada
    /// jefe v6.48 pelea con LAS LIBRERÍAS DE LA CASA — nada de
    /// proyectiles vanilla de prestado (los CultistBossLightningOrbArc
    /// recoloreados y las "minas" que eran ProjectileID.Bullet estáticos
    /// murieron aquí). Un diente por estilo, TODOS telegrafiados o
    /// esquivables, TODOS con la física del motor:
    ///
    ///   0  PÚA DEL SAGRARIO (Titán): espina de cristal en arco que SE
    ///      CLAVA, respira y DETONA en esquirlas (OndaLib.Pulse).
    ///   1  CORO DE CRISTAL (Titán, furia): esquirla en órbita alrededor
    ///      del coloso que SE LANZA a la presa cuando el coro canta.
    ///   2  VIROTE DE VACÍO (Rift): lanza entre-mundos que PARPADEA entre
    ///      fases (medio dentro del desgarro, medio fuera).
    ///   3  FLECHA ESTELAR (Arquera): flecha con corrección de rumbo y
    ///      ESTELA DE FANTASMAS (EspectroLib — la cola historia).
    ///   4  MINA ESTELAR (Arquera): LA MINA DE VERDAD — se ARMA con
    ///      pulso de aviso (OndaLib) y DETONA en un ARCO VOLTAICO a la
    ///      presa (StormLib.ChainBolt + ImpactFlash) — ya no una bala
    ///      quieta.
    ///   5  ESTRELLA FUGAZ (Arquera, furia): estrella que cae con MARCA
    ///      de suelo telegrafiada antes del impacto (OndaLib.Ground).
    ///   6  TAJO DEL PORTADOR (Portador): el corte DIFERIDO de la casa —
    ///      marca que respira (Telegrafo) y FLORECE en arco TajoLib.
    ///   7  CUCHILLA EN ÓRBITA (Portador): cuchilla girando en anillo
    ///      alrededor del duelista que se DISPARA al cierre.
    ///   8  CORTE DE REALIDAD (Portador, furia): la PARED DE DESGARRO
    ///      que AVanza lenta (RiftLib.Tear con CaminoDesgarro) — el
    ///      campo que parte el suelo en dos.
    ///   9  PERNO ESTELAR (Aethon): el perno de polvo estelar con halo
    ///      y latido (Bloom pulsante).
    ///   10 NUBE DE NEBULOSA (Aethon): zona que QUEMA por contacto —
    ///      flores de humo violeta girando (quads SoftGlow + anillos).
    ///   11 RUNA MEMORIZADA (Aethon, fase 5): LA RUNA QUE TE RECUERDA —
    ///      el sigilo dorado orbita a Aethon y DISPARA TUS PROPIOS
    ///      movimientos (tajos y pernos de la casa, aprendidos de ti).
    ///   12 ESTALLIDO DE MINA (Arquera): el cuerpo del estallido (20 t
    ///      de radio honesto — daño por colisión del motor).
    ///
    /// REGLAS DE LA CASA: daño por COLISIÓN del motor (cero daño
    /// manual), cero Main.rand en el render (Hash01 y semillas por
    /// ai[2]), SIN hide (la lección v6.35), el contrato de lote
    /// cerrado→cerrado (VFXCore búfer → lote aditivo de pantalla →
    /// lote del pase reabierto TAL CUAL).
    /// </summary>
    public class AtaqueJefeProjectile : ModProjectile
    {
        // === LOS ESTILOS (ai[0]) ===
        public const int EstiloPuaSagrario = 0;
        public const int EstiloCoroCristal = 1;
        public const int EstiloViroteVacio = 2;
        public const int EstiloFlechaEstelar = 3;
        public const int EstiloMinaEstelar = 4;
        public const int EstiloEstrellaFugaz = 5;
        public const int EstiloTajoPortador = 6;
        public const int EstiloCuchillaOrbit = 7;
        public const int EstiloCorteRealidad = 8;
        public const int EstiloPernoEstelar = 9;
        public const int EstiloNubeNebulosa = 10;
        public const int EstiloRunaMemorizada = 11;
        public const int EstiloEstallidoMina = 12;

        /// <summary>El estilo del diente (ai[0]).</summary>
        private int Estilo => (int)Projectile.ai[0];
        /// <summary>Parámetro libre (ángulo base / fase / modo).</summary>
        private float Par => Projectile.ai[1];
        /// <summary>Semilla determinista (ai[2]).</summary>
        private int Seed => Math.Max(1, (int)Projectile.ai[2] % 9973);

        // === EL ESTADO (por estilo) ===
        private float _edad;
        private Vector2 _centroOrbita;   // el anillo del coro / la runa
        private bool _lanzado;           // esquirla/cuchilla ya disparada
        private bool _clavada;           // la púa ya se hundió en el suelo
        private Vector2 _posAnterior;    // la cola del virote

        // === LAS PALETAS DE LOS CINCO ===
        private static readonly Color TealCristal = new(168, 232, 255);
        private static readonly Color TealVacio = new(96, 224, 220);
        private static readonly Color VioletaVacio = new(140, 60, 220);
        private static readonly Color AmbarEstelar = new(255, 178, 96);
        private static readonly Color EmberPortador = new(255, 120, 60);
        private static readonly Color LuzPrimordial = new(196, 150, 255);
        private static readonly Color OroGrimorio = new(245, 196, 81);
        private static readonly Color BlancoCaliente = new(255, 240, 190);
        private static readonly Color NebulosaNube = new(55, 42, 71); // violeta oscuro premezclado (XNA Color no define +)

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
            Projectile.timeLeft = 420;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0.5f;
            // v6.50.1 — los que entren a media pelea reciben las paredes/minas
            // (vanilla lo hace con los proyectiles de jefe).
            Projectile.netImportant = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            _posAnterior = Projectile.Center;
            _centroOrbita = Projectile.Center;

            // La púa es la única que RESPETA el terreno (se clava).
            Projectile.tileCollide = Estilo == EstiloPuaSagrario ||
                                     Estilo == EstiloEstrellaFugaz;

            // Vidas propias por estilo.
            switch (Estilo)
            {
                case EstiloPuaSagrario: Projectile.timeLeft = 300; break;
                case EstiloCoroCristal: Projectile.timeLeft = 330; break;
                case EstiloViroteVacio: Projectile.timeLeft = 200; break;
                case EstiloFlechaEstelar: Projectile.timeLeft = 240; break;
                case EstiloMinaEstelar: Projectile.timeLeft = 480; break;
                case EstiloEstrellaFugaz: Projectile.timeLeft = 300; break;
                case EstiloTajoPortador: Projectile.timeLeft = 90; break;
                case EstiloCuchillaOrbit: Projectile.timeLeft = 360; break;
                case EstiloCorteRealidad: Projectile.timeLeft = 330; break;
                case EstiloPernoEstelar: Projectile.timeLeft = 220; break;
                case EstiloNubeNebulosa: Projectile.timeLeft = 360; break;
                case EstiloRunaMemorizada: Projectile.timeLeft = 600; break;
                case EstiloEstallidoMina: Projectile.timeLeft = 20; break;
            }
        }

        /// <summary>
        /// v6.50.2 — FIX (LA PÚA MORÍA AL CLAVARSE): el motor mata al
        /// proyectil en la PRIMERA colisión de tile (OnTileCollide default
        /// → true → Kill del else final del cauce) — la IA corría ANTES de
        /// la colisión y jamás veía velocity≈0: _clavada nunca se ponía, las
        /// 90 t de "respiración" y DetonarPua eran código muerto (en SP y
        /// server igual). AHORA la colisión ES el clavado: velocidad a cero,
        /// sin rebote y SIN muerte (return false — la IA detona a su tiempo).
        /// La estrella fugaz recibía el mismo disparo del motor cuando su
        /// chequeo manual de suelo perdía la carrera contra un tick rápido.
        /// Corre en TODAS las máquinas: la simulación local de cada pantalla
        /// queda idéntica (la autoridad detona; el visual acompaña).
        /// </summary>
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Estilo == EstiloPuaSagrario || Estilo == EstiloEstrellaFugaz)
            {
                _clavada = true;
                Projectile.velocity = Vector2.Zero;
                return false; // ni muerte ni rebote: clavada
            }
            return true;
        }

        /// <summary>
        /// v6.50.2 — FIX (hitbox honesta del estallido): el cuerpo de la
        /// mina dibuja ~130 px pero su caja de colisión era la de
        /// SetDefaults (14×14) — solo dañaba al pisar el centro exacto.
        /// Inflada a la talla del visual (golpea lo que SE VE que golpea).
        /// </summary>
        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            if (Estilo == EstiloEstallidoMina)
            {
                const int inflar = 96; // 14 → 110 px de cuerpo
                hitbox.X -= inflar / 2;
                hitbox.Y -= inflar / 2;
                hitbox.Width += inflar;
                hitbox.Height += inflar;
            }
        }

        /// <summary>La presa más cercana.</summary>
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

            // v6.50.2 — FIX (MP: el estado que OnSpawn no lleva): OnSpawn
            // NO corre en los clientes que reciben el proyectil por red
            // (msg 27 — payload sin tileCollide/timeLeft) → la púa
            // ATRAVESABA el suelo en las pantallas remotas y el
            // _centroOrbita quedaba (0,0) (coro/cuchilla/runa teleportados
            // a la esquina del mundo — sus ataques jamás amenazaban a los
            // remotos). Autocuración al inicio de cada IA: el estado se
            // reconstruye desde lo que SÍ viaja (posición + ai[]).
            if (_centroOrbita == Vector2.Zero) _centroOrbita = Projectile.Center;
            Projectile.tileCollide = Estilo == EstiloPuaSagrario ||
                                     Estilo == EstiloEstrellaFugaz;

            Player presa = Presa();

            switch (Estilo)
            {
                // =============================================================
                //  LA PÚA DEL SAGRARIO — arco, clavado, detona en esquirlas
                // =============================================================
                case EstiloPuaSagrario:
                {
                    if (!_clavada)
                    {
                        // EL ARCO: gravedad del coloso.
                        Projectile.velocity.Y += 0.30f;
                        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

                        // ¿CLAVADA? (suelo o techo — el terreno la detiene)
                        if (Projectile.velocity.LengthSquared() < 0.01f) _clavada = true;
                    }
                    else
                    {
                        Projectile.velocity = Vector2.Zero;
                        // LA DETONACIÓN: respira 90 t y estalla.
                        if (_edad > 210)
                        {
                            DetonarPua();
                            Projectile.Kill();
                        }
                    }
                    break;
                }

                // =============================================================
                //  EL CORO DE CRISTAL — órbita y lanza al canto
                // =============================================================
                case EstiloCoroCristal:
                {
                    if (!_lanzado)
                    {
                        // LA ÓRBITA: el anillo del coro (el centro a la deriva
                        // lenta hacia la presa — el coro se acerca cantando).
                        if (presa != null && _edad % 10 == 0)
                        {
                            Vector2 dir = (presa.Center - _centroOrbita).SafeNormalize(Vector2.Zero);
                            _centroOrbita += dir * 2.2f;
                        }
                        float ang = Par + _edad * 0.050f;
                        Projectile.Center = _centroOrbita +
                            new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 118f;
                        Projectile.rotation = ang + MathHelper.PiOver2;

                        // EL CANTO: a los 150 t (o 60 en el coro rápido de la
                        // furia — Par marca la cadencia) TODAS se lanzan.
                        if (_edad >= (Par < 0f ? 60 : 150))
                        {
                            _lanzado = true;
                            if (presa != null)
                                Projectile.velocity = (presa.Center - Projectile.Center)
                                    .SafeNormalize(Vector2.UnitY) * 13f;
                        }
                    }
                    else if (_edad > 330) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  EL VIROTE DE VACÍO — recto, parpadeando entre fases
                // =============================================================
                case EstiloViroteVacio:
                {
                    _posAnterior = Projectile.Center - Projectile.velocity * 1.6f;
                    if (_edad > 200) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  LA FLECHA ESTELAR — corrección de rumbo y estela
                // =============================================================
                case EstiloFlechaEstelar:
                {
                    if (presa != null && _edad < 46)
                    {
                        Vector2 deseada = (presa.Center - Projectile.Center)
                            .SafeNormalize(Vector2.UnitY) * 14f;
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, deseada, 0.055f);
                    }
                    Projectile.rotation = Projectile.velocity.ToRotation();
                    if (_edad > 240) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  LA MINA ESTELAR — se arma, avisa, DETONA en arco
                // =============================================================
                case EstiloMinaEstelar:
                {
                    Projectile.velocity *= 0.90f; // se asienta donde cae
                    float distPresa = presa != null
                        ? Vector2.Distance(presa.Center, Projectile.Center) : 9999f;

                    // LA DETONACIÓN: presa en el radio del arco (160 px) o
                    // vida agotada (el suelo queda sembrado un rato).
                    if ((distPresa < 160f && _edad > 40) || _edad > 440)
                    {
                        DetonarMina(presa);
                        Projectile.Kill();
                    }
                    break;
                }

                // =============================================================
                //  LA ESTRELLA FUGAZ — cae sobre la marca
                // =============================================================
                case EstiloEstrellaFugaz:
                {
                    if (!_clavada)
                    {
                        Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.42f, 15f);
                        Projectile.rotation += 0.22f;

                        // ¿IMPACTO? (terreno — tileCollide activo para esta)
                        Point tile = Projectile.Center.ToTileCoordinates();
                        if (tile.X > 5 && tile.X < Main.maxTilesX - 5 &&
                            tile.Y > 5 && tile.Y < Main.maxTilesY - 5)
                        {
                            Tile tl = Main.tile[tile.X, tile.Y + 1];
                            if (tl != null && tl.HasTile && Main.tileSolid[tl.TileType])
                            {
                                _clavada = true;
                                DetonarFugaz();
                            }
                        }
                    }
                    else { DetonarFugaz(); }
                    if (_edad > 300) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  EL TAJO DEL PORTADOR — marca que respira, corte que florece
                // =============================================================
                case EstiloTajoPortador:
                {
                    if (_edad < 24)
                    {
                        // LA MARCA: el proyectil ya voló a su sitio (la fija
                        // el duelista) y la tensión crece.
                        Projectile.velocity *= 0.82f;
                    }
                    else if (_edad == 24)
                    {
                        // EL FLORECIMIENTO: arranque violento en la dirección
                        // de la marca (Par = ángulo del corte).
                        Projectile.velocity = new Vector2(MathF.Cos(Par), MathF.Sin(Par)) * 15f;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item71, Projectile.Center);
                    }
                    if (_edad > 72) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  LA CUCHILLA EN ÓRBITA — gira y se dispara al cierre
                // =============================================================
                case EstiloCuchillaOrbit:
                {
                    if (!_lanzado)
                    {
                        float ang = Par + _edad * 0.065f;
                        Projectile.Center = _centroOrbita +
                            new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 104f;
                        Projectile.rotation = ang + MathHelper.PiOver2;

                        // LA PRESA ENTRA AL ANILLO → disparo.
                        if (presa != null && _edad > 30)
                        {
                            float d = Vector2.Distance(presa.Center, _centroOrbita);
                            if (d < 170f)
                            {
                                _lanzado = true;
                                Projectile.velocity = (presa.Center - Projectile.Center)
                                    .SafeNormalize(Vector2.UnitY) * 15f;
                            }
                        }
                    }
                    else if (_edad > 360) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  EL CORTE DE REALIDAD — la pared que avanza
                // =============================================================
                case EstiloCorteRealidad:
                {
                    // AVANZA LENTA (la pared que parte el suelo: no persigue,
                    // OBLIGA a moverse — el campo del duelista).
                    Projectile.velocity = new Vector2(MathF.Cos(Par), MathF.Sin(Par)) * 3.4f;
                    if (_edad > 330) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  EL PERNO ESTELAR — recto con latido
                // =============================================================
                case EstiloPernoEstelar:
                {
                    Projectile.rotation = Projectile.velocity.ToRotation();
                    if (_edad > 220) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  LA NUBE DE NEBULOSA — la zona que quema (por contacto)
                // =============================================================
                case EstiloNubeNebulosa:
                {
                    // Deriva lenta hacia la presa (la nube persigue sin prisa).
                    if (presa != null)
                    {
                        Vector2 dir = (presa.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, dir * 1.4f, 0.01f);
                    }
                    if (_edad > 360) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  LA RUNA MEMORIZADA — orbita a Aethon y dispara TUS trucos
                // =============================================================
                case EstiloRunaMemorizada:
                {
                    // LA ÓRBITA a Aethon (ai[1] = fase; el centro a la deriva
                    // hacia la presa lento — la memoria se acerca).
                    if (presa != null && _edad % 12 == 0)
                    {
                        Vector2 dir = (presa.Center - _centroOrbita).SafeNormalize(Vector2.Zero);
                        _centroOrbita += dir * 2.6f;
                    }
                    float ang = Par + _edad * 0.038f;
                    Projectile.Center = _centroOrbita +
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 150f;
                    Projectile.rotation = ang;

                    // EL DISPARO: cada 110 t la runa RECUERDA uno de TUS
                    // movimientos y lo repite — un tajo diferido o un perno.
                    if (_edad % 110 == 0 && _edad > 0 && presa != null)
                    {
                        bool tajo = (Seed + _edad / 110) % 2 == 0;
                        if (tajo)
                        {
                            float dirCorte = (presa.Center - Projectile.Center).ToRotation();
                            // v6.50.1 — FIX (MP ×N+1): la IA del proyectil
                            // corre en server Y clientes (el dueño puede ser
                            // 255 — spawn del server) — sin gate cada máquina
                            // spawnnea su copia y NewProjectile la difunde.
                            // Solo la autoridad spawnnea (los clientes no).
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                                    presa.Center, Vector2.Zero,
                                    ModContent.ProjectileType<AtaqueJefeProjectile>(),
                                    (int)(Projectile.damage * 0.8f), 3f, Main.myPlayer,
                                    EstiloTajoPortador, dirCorte, Seed + 17);
                            }
                        }
                        else
                        {
                            Vector2 vel = (presa.Center - Projectile.Center)
                                .SafeNormalize(Vector2.UnitY) * 11f;
                            // v6.50.1 — FIX (MP ×N+1): solo la autoridad spawnnea.
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                                    Projectile.Center, vel,
                                    ModContent.ProjectileType<AtaqueJefeProjectile>(),
                                    (int)(Projectile.damage * 0.7f), 2f, Main.myPlayer,
                                    EstiloPernoEstelar, 0f, Seed + 29);
                            }
                        }
                    }
                    if (_edad > 600) Projectile.Kill();
                    break;
                }

                // =============================================================
                //  EL ESTALLIDO DE MINA — el cuerpo del boom (20 t honestos)
                // =============================================================
                case EstiloEstallidoMina:
                {
                    Projectile.velocity = Vector2.Zero;
                    // v6.50.2 — FIX (el fantasma de 7 s en clientes MP): el
                    // timeLeft=20 se fija en OnSpawn, que NO corre al recibir
                    // el msg 27 → las pantallas remotas mantenían la zona 420 t
                    // (colisión hostil evaluada EN cada cliente local). La edad
                    // SÍ corre en todas las máquinas: muerte por edad,
                    // idempotente con el timeLeft de la autoridad.
                    if (_edad > 20) Projectile.Kill();
                    break;
                }
            }

            // La luz del diente (el color de su dueño).
            Vector3 luz = Estilo switch
            {
                EstiloPuaSagrario or EstiloCoroCristal => new Vector3(0.20f, 0.42f, 0.55f),
                EstiloViroteVacio => new Vector3(0.14f, 0.30f, 0.32f),
                EstiloFlechaEstelar or EstiloMinaEstelar or EstiloEstrellaFugaz
                    or EstiloEstallidoMina => new Vector3(0.48f, 0.30f, 0.10f),
                EstiloTajoPortador or EstiloCuchillaOrbit or EstiloCorteRealidad
                    => new Vector3(0.45f, 0.16f, 0.07f),
                EstiloNubeNebulosa => new Vector3(0.20f, 0.10f, 0.34f),
                EstiloRunaMemorizada or EstiloPernoEstelar => new Vector3(0.38f, 0.28f, 0.10f),
                _ => new Vector3(0.3f, 0.3f, 0.3f),
            };
            Lighting.AddLight(Projectile.Center, luz);
        }

        // ==================================================================
        //  LAS DETONACIONES (lógica del juego — polvos y cuerpos del motor)
        // ==================================================================

        /// <summary>La púa estalla en ESQUIRLAS radiales (el coloso las escupe).</summary>
        private void DetonarPua()
        {
            for (int i = 0; i < 5; i++)
            {
                float ang = i * MathHelper.TwoPi / 5f + Seed * 0.01f;
                Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 7f;
                // v6.50.1 — FIX (MP ×N+1): solo la autoridad spawnnea
                // (los clientes corren esta IA — la esquirla se difunde sola).
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                        Projectile.Center, vel,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(Projectile.damage * 0.55f), 2f, Main.myPlayer,
                        EstiloPernoEstelar, 0f, Seed + i * 7);
                }
            }
            for (int d = 0; d < 8; d++)
            {
                int idx = Dust.NewDust(Projectile.Center, 12, 12, DustID.IceTorch,
                    -Main.rand.NextFloat(2f, 5f), -Main.rand.NextFloat(1f, 4f));
                Main.dust[idx].noGravity = true;
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item30, Projectile.Center);
        }

        /// <summary>
        /// LA MINA DETONA: el ARCO VOLTAICO a la presa (visual de
        /// StormLib) + el CUERPO del estallido (proyectil con hitbox
        /// honesta — el daño lo pone el motor por colisión, no a mano).
        /// </summary>
        private void DetonarMina(Player presa)
        {
            // EL CUERPO: 20 t de radio honesto.
            // v6.50.1 — FIX (MP ×N+1): solo la autoridad spawnnea
            // (los clientes corren esta IA — el cuerpo se difunde solo).
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                    Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<AtaqueJefeProjectile>(),
                    Projectile.damage, 4f, Main.myPlayer,
                    EstiloEstallidoMina, 0f, Seed);
            }

            for (int d = 0; d < 10; d++)
            {
                int idx = Dust.NewDust(Projectile.Center, 14, 14, DustID.YellowStarDust,
                    Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 1f));
                Main.dust[idx].noGravity = true;
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item94, Projectile.Center);
        }

        /// <summary>La estrella fugaz revienta en polvo estelar.</summary>
        private void DetonarFugaz()
        {
            for (int d = 0; d < 8; d++)
            {
                int idx = Dust.NewDust(Projectile.Center, 12, 12, DustID.YellowStarDust,
                    Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-3f, 1f));
                Main.dust[idx].noGravity = true;
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            Projectile.Kill();
        }

        // ==================================================================
        //  EL RENDER — cada diente con SU librería. EL FLUJO DE LA CASA:
        //  (1) los quads al BÚFER (coords de MUNDO) y su volcado propio;
        //  (2) el lote aditivo NUESTRO para las librerías de coords de
        //  PANTALLA (OndaLib/StormLib/TajoLib/LumenLib/OrbitaLib/RiftLib);
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
                    case EstiloPuaSagrario:
                        VFXCore.Quad(Projectile.Center, TealCristal * 0.85f,
                            new Vector2(10f, 26f), Projectile.rotation);
                        VFXCore.Quad(Projectile.Center, BlancoCaliente * 0.7f,
                            new Vector2(5f, 12f), Projectile.rotation);
                        break;

                    case EstiloCoroCristal:
                        VFXCore.Quad(Projectile.Center, TealCristal * 0.9f,
                            new Vector2(16f, 7f), Projectile.rotation);
                        break;

                    case EstiloViroteVacio:
                        VFXCore.Quad(Projectile.Center, TealVacio * 0.9f,
                            new Vector2(30f, 6f), Projectile.velocity.ToRotation());
                        VFXCore.Quad(Projectile.Center, VioletaVacio * 0.5f,
                            new Vector2(18f, 10f), Projectile.velocity.ToRotation());
                        break;

                    case EstiloFlechaEstelar:
                        VFXCore.Quad(Projectile.Center, AmbarEstelar * 0.9f,
                            new Vector2(24f, 5f), Projectile.rotation);
                        VFXCore.Quad(Projectile.Center, BlancoCaliente * 0.65f,
                            new Vector2(10f, 3f), Projectile.rotation);
                        break;

                    case EstiloMinaEstelar:
                    {
                        float pulso = 0.9f + 0.1f * MathF.Sin(t * 8f + Seed);
                        VFXCore.Quad(Projectile.Center, AmbarEstelar * (0.75f * pulso),
                            new Vector2(18f, 18f));
                        VFXCore.Quad(Projectile.Center, OroGrimorio * 0.6f,
                            new Vector2(9f, 9f));
                        break;
                    }

                    case EstiloEstrellaFugaz:
                        VFXCore.Quad(Projectile.Center, OroGrimorio * 0.95f,
                            new Vector2(20f, 20f), Projectile.rotation);
                        VFXCore.Quad(Projectile.Center, BlancoCaliente * 0.8f,
                            new Vector2(10f, 10f), Projectile.rotation);
                        break;

                    case EstiloCuchillaOrbit:
                        VFXCore.Quad(Projectile.Center, EmberPortador * 0.9f,
                            new Vector2(22f, 6f), Projectile.rotation);
                        VFXCore.Quad(Projectile.Center, BlancoCaliente * 0.6f,
                            new Vector2(9f, 3f), Projectile.rotation);
                        break;

                    case EstiloCorteRealidad:
                        // El CUERPO del desgarro lo dibuja RiftLib (fase 2);
                        // aquí solo las chispas del borde.
                        break;

                    case EstiloPernoEstelar:
                        VFXCore.Quad(Projectile.Center, LuzPrimordial * 0.9f,
                            new Vector2(16f, 16f), Projectile.rotation);
                        VFXCore.Quad(Projectile.Center, OroGrimorio * 0.6f,
                            new Vector2(8f, 8f), Projectile.rotation);
                        break;

                    case EstiloNubeNebulosa:
                    {
                        // LAS FLORES DE HUMO: 6 puffs orbitando con Hash01.
                        for (int i = 0; i < 6; i++)
                        {
                            float h = VFXCore.Hash01(Seed, i, (int)(_edad / 40f));
                            float ang = h * MathHelper.TwoPi + t * 0.4f;
                            float r = 30f + 26f * VFXCore.Hash01(Seed, i + 40, 0);
                            Vector2 off = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * r;
                            float fade = 1f - _edad / 360f;
                            VFXCore.Quad(Projectile.Center + off,
                                NebulosaNube * fade,
                                new Vector2(34f, 30f) * (0.8f + 0.2f * MathF.Sin(t * 2f + i)));
                        }
                        break;
                    }

                    case EstiloRunaMemorizada:
                    {
                        // EL SIGILO: pentágono de cápsulas doradas + núcleo.
                        for (int i = 0; i < 5; i++)
                        {
                            float a0 = Projectile.rotation + i * MathHelper.TwoPi / 5f;
                            float a1 = Projectile.rotation + (i + 1) * MathHelper.TwoPi / 5f;
                            Vector2 p0 = Projectile.Center + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * 26f;
                            Vector2 p1 = Projectile.Center + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * 26f;
                            VFXCore.Line(p0, p1, OroGrimorio * 0.55f, 3f);
                        }
                        VFXCore.Quad(Projectile.Center, BlancoCaliente * 0.9f,
                            new Vector2(10f, 10f));
                        break;
                    }

                    case EstiloEstallidoMina:
                    {
                        // EL CUERPO DEL ESTALLIDO: la bola de chispas (20 t).
                        float prog = _edad / 20f;
                        float r = 66f * (0.3f + 0.7f * prog);
                        for (int i = 0; i < 8; i++)
                        {
                            float ang = i * MathHelper.TwoPi / 8f + Seed * 0.01f;
                            Vector2 off = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * r;
                            VFXCore.Quad(Projectile.Center + off,
                                AmbarEstelar * (0.8f * (1f - prog)),
                                new Vector2(14f, 14f) * (1f - prog * 0.5f));
                        }
                        break;
                    }
                }
                VFXCore.FlushAdditive(null, false);

                // === FASE 2 — EL LOTE ADITIVO DE LAS LIBRERÍAS (PANTALLA) ===
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                switch (Estilo)
                {
                    // === LA PÚA: el halo que respira clavada ===
                    case EstiloPuaSagrario:
                    {
                        LumenLib.Bloom(Main.spriteBatch, pos, 20f, TealCristal,
                            _clavada ? 0.55f : 0.8f);
                        if (_clavada)
                        {
                            float prog = (_edad - 150) / 60f;
                            if (prog > 0f && prog < 1f)
                                OndaLib.Pulse(Main.spriteBatch, pos, prog, 70f,
                                    TealCristal, 0.6f, Seed);
                        }
                        break;
                    }

                    // === EL CORO: la esquirla y su canto ===
                    case EstiloCoroCristal:
                    {
                        LumenLib.Bloom(Main.spriteBatch, pos, 18f, TealCristal,
                            _lanzado ? 0.9f : 0.6f);
                        if (!_lanzado)
                            OrbitaLib.AnilloFino(pos, 26f, t * 2.4f,
                                OrbitaLib.Tint(TealCristal, 0.30f));
                        break;
                    }

                    // === EL VIROTE: el parpadeo entre fases ===
                    case EstiloViroteVacio:
                    {
                        float fase = 0.55f + 0.45f * MathF.Sin(t * 11f + Seed);
                        LumenLib.Bloom(Main.spriteBatch, pos, 22f * fase, TealVacio, 0.75f * fase);
                        Vector2 cola = _posAnterior - Main.screenPosition;
                        StormLib.Bolt(Main.spriteBatch, cola, pos, Seed,
                            StormLib.FlickTick(t, 16f), 2.2f,
                            VioletaVacio, TealVacio, 0.55f * fase);
                        break;
                    }

                    // === LA FLECHA: la ESTELA DE FANTASMAS ===
                    case EstiloFlechaEstelar:
                    {
                        LumenLib.Bloom(Main.spriteBatch, pos, 16f, AmbarEstelar, 0.7f);
                        // La cola historia: 4 fantasmas detrás (deterministas).
                        for (int g = 1; g <= 4; g++)
                        {
                            Vector2 fantasma = pos - Projectile.velocity * (1.1f * g);
                            float alfa = 0.4f * (1f - g / 5f);
                            LumenLib.Bloom(Main.spriteBatch, fantasma, 12f,
                                AmbarEstelar, alfa, 2);
                        }
                        break;
                    }

                    // === LA MINA: el aviso de armado ===
                    case EstiloMinaEstelar:
                    {
                        float progArmado = MathHelper.Clamp(_edad / 40f, 0f, 1f);
                        OndaLib.Pulse(Main.spriteBatch, pos,
                            (_edad % 50f) / 50f, 120f, AmbarEstelar,
                            0.35f + 0.25f * progArmado, Seed);
                        LumenLib.BloomPulse(Main.spriteBatch, pos, 20f, OroGrimorio,
                            0.5f + 0.3f * progArmado, t, 3f);
                        break;
                    }

                    // === LA FUGAZ: la marca de suelo antes del impacto ===
                    case EstiloEstrellaFugaz:
                    {
                        LumenLib.Bloom(Main.spriteBatch, pos, 22f, OroGrimorio, 0.85f);
                        // La marca: un anillo fino en el SUELO bajo la estrella.
                        Vector2 suelo = pos;
                        Point tile = Projectile.Center.ToTileCoordinates();
                        for (int ty = 0; ty < 40; ty++)
                        {
                            Point abajo = new Point(tile.X, tile.Y + ty);
                            if (abajo.X > 5 && abajo.X < Main.maxTilesX - 5 &&
                                abajo.Y > 5 && abajo.Y < Main.maxTilesY - 5)
                            {
                                Tile tl = Main.tile[abajo.X, abajo.Y];
                                if (tl != null && tl.HasTile && Main.tileSolid[tl.TileType])
                                {
                                    suelo = new Vector2(abajo.X * 16f + 8f, abajo.Y * 16f) - Main.screenPosition;
                                    break;
                                }
                            }
                        }
                        // v6.49 — EL MEDIO-ANILLO DE SUELO que CRECE mientras la
                        // estrella cae (OndaLib.GroundVisual — la promesa del
                        // comentario por fin cumplida: la marca "respira" el
                        // impacto que viene, pegada al piso de verdad).
                        float progCaida = MathHelper.Clamp(_edad / 90f, 0f, 1f);
                        OndaLib.GroundVisual(Main.spriteBatch, suelo,
                            MathHelper.Min(progCaida * 1.3f, 1f), 74f,
                            AmbarEstelar, 0.55f);
                        OrbitaLib.AnilloFino(suelo, 46f, 0f,
                            OrbitaLib.Tint(AmbarEstelar, 0.4f));
                        break;
                    }

                    // === EL TAJO DEL PORTADOR: marca, luego el corte ===
                    case EstiloTajoPortador:
                    {
                        if (_edad <= 23)
                        {
                            // LA MARCA que se afila (Telegrafo corto).
                            Vector2 fin = pos + new Vector2(MathF.Cos(Par), MathF.Sin(Par)) * 60f;
                            OndaLib.Telegrafo(Main.spriteBatch, pos, fin,
                                _edad / 24f, EmberPortador, 3f);
                        }
                        else
                        {
                            float prog = MathF.Min(1f, (_edad - 24f) / 8f);
                            TajoLib.Tajo(pos, 56f, Par - 0.6f, Par + 0.6f,
                                prog, 1f - prog * 0.3f, 13f,
                                EmberPortador, BlancoCaliente, Seed, t);
                            LumenLib.Bloom(Main.spriteBatch, pos, 26f, EmberPortador,
                                0.7f * (1f - prog));
                        }
                        break;
                    }

                    // === LA CUCHILLA: el filo giratorio ===
                    case EstiloCuchillaOrbit:
                    {
                        LumenLib.Bloom(Main.spriteBatch, pos, 18f, EmberPortador, 0.75f);
                        if (!_lanzado)
                            OrbitaLib.AnilloFino(_centroOrbita - Main.screenPosition, 104f,
                                t * 1.8f, OrbitaLib.Tint(EmberPortador, 0.22f));
                        break;
                    }

                    // === EL CORTE DE REALIDAD: la PARED de desgarro ===
                    case EstiloCorteRealidad:
                    {
                        Vector2 dir = new Vector2(MathF.Cos(Par), MathF.Sin(Par));
                        float largo = 240f;
                        Vector2 origen = Projectile.Center - dir * (largo * 0.5f);
                        float prog = MathHelper.Clamp(_edad / 330f, 0f, 1f);
                        RiftLib.TearVacio(Main.spriteBatch,
                            origen - Main.screenPosition, dir, largo,
                            MathF.Min(1f, 0.25f + _edad / 60f), 34f, Seed, t);
                        break;
                    }

                    // === EL PERNO: el latido estelar ===
                    case EstiloPernoEstelar:
                    {
                        LumenLib.BloomPulse(Main.spriteBatch, pos, 20f, LuzPrimordial,
                            0.85f, t, 5f);
                        break;
                    }

                    // === LA NUBE: los anillos de la zona que quema ===
                    case EstiloNubeNebulosa:
                    {
                        float fade = 1f - _edad / 360f;
                        OrbitaLib.AnilloFino(pos, 54f, t * 0.8f,
                            OrbitaLib.Tint(LuzPrimordial, 0.30f * fade));
                        OrbitaLib.AnilloFino(pos, 40f, -t * 1.1f,
                            OrbitaLib.Tint(VioletaVacio, 0.22f * fade));
                        LumenLib.Bloom(Main.spriteBatch, pos, 30f, VioletaVacio, 0.35f * fade);
                        break;
                    }

                    // === LA RUNA MEMORIZADA: el sigilo dorado vivo ===
                    case EstiloRunaMemorizada:
                    {
                        LumenLib.BloomPulse(Main.spriteBatch, pos, 24f, OroGrimorio,
                            0.8f, t, 2.2f);
                        OrbitaLib.AnilloFino(pos, 30f, -Projectile.rotation,
                            OrbitaLib.Tint(OroGrimorio, 0.35f));
                        break;
                    }

                    // === EL ESTALLIDO DE MINA: la cruz de luz ===
                    case EstiloEstallidoMina:
                    {
                        float prog = _edad / 20f;
                        StormLib.ImpactFlash(Main.spriteBatch, pos, 40f,
                            AmbarEstelar, 1f - prog, t);
                        OndaLib.Pulse(Main.spriteBatch, pos, prog, 130f,
                            OroGrimorio, 0.7f * (1f - prog), Seed);
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
