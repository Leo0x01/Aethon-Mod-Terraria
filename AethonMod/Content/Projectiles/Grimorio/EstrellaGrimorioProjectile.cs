using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Effects.Bruma;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Projectiles.Grimorio
{
    /// <summary>
    /// EstrellaGrimorioProjectile — v6.185 — EL ATAQUE BÁSICO DEL GRIMORIO
    /// POR NIVEL DE DESPERTAR (0-120). La letra entera vive en
    /// DespertarGrimorio (las tablas puras) y AQUÍ se viste.
    ///
    /// LA BASE: el proyectil del BASTÓN DE SINFONÍA PRIMORDIAL
    /// (SinfoniaPrimordialProjectile — v6.24) portado pieza a pieza:
    ///   · LA ESTRELLITA — el corazón de luz (LumenLib.BloomPulse + Flare).
    ///   · LOS HACES EN CADA PUNTA — los cinco rayos de sol (LumenLib.Ray).
    ///   · EL ANILLO RÚNICO DEL AGUJERO NEGRO — las seis runas orbitando
    ///     (DrawOrbitingRunes: la misma tabla de glifos).
    ///   · LOS ARCOS ENTRE RUNAS — StormLib.ArcRing.
    ///   · LA EXPLOSIÓN — DrawFinale: la nube de bruma, la onda de choque,
    ///     el destello, la aurora, los seis rayos radiales, las runas
    ///     volando y las ondas — INMUTABLE (a los 35 viva EXACTA; a los 30
    ///     la MISMA a escala ×0,55).
    ///
    /// EL DAÑO: 100 % manual (el patrón del original: sin contacto de
    /// vanilla — GolpeMotor resuelve en el cliente dueño, el debuff en la
    /// autoridad). El ataque base del despertar ya vive en el daño del
    /// disparo (GrimoireEternal.ModifyWeaponDamage: Flat).
    ///
    /// EL ESTADO: ai[0] = el nivel de despertar (viaja desde el disparo — el
    /// paquete 27 sí lo lleva) · ai[1] = la semilla · ai[2] = libre. Todo lo
    /// demás se deriva de ai[0] en OnSpawn — cada pantalla viste lo mismo.
    /// </summary>
    public class EstrellaGrimorioProjectile : ModProjectile
    {
        /// <summary>Ticks de vuelo antes de morir sola (los 120 del original).</summary>
        private const int FlyLife = 120;

        /// <summary>Duración de la fase de detonación (LA SINFONÍA).</summary>
        private const int FinaleTicks = 26;

        /// <summary>Regeneración de los rayos (Hz) — el original.</summary>
        private const float FlickHz = 12f;

        /// <summary>El giro máximo por tick al que se aplica el seguimiento.</summary>
        private const float GiroMax = 0.08f;

        /// <summary>El radio de caza de la presa del seguimiento.</summary>
        private const float RadioPresaN = 600f;

        /// <summary>El radio del estallido del original (la explosión inmutable).</summary>
        private const float BurstR = 140f;

        // === EL ESTADO DERIVADO (de ai[0] — en OnSpawn) ===
        private bool _multicolor, _anillo, _anilloColor, _haces, _hacesColor;
        private bool _explota, _dosAnillos, _contra, _arcosAnillo, _tormenta;
        private bool _arcsAttack, _quemadura;
        private float _seguimiento;
        private int _saltos;
        private float _escalaExplosion = 1f;

        private float _age;
        private bool _dying;
        private float _dyingAge;

        // === LAS CADENAS de los arcos (hasta 6 saltos) ===
        private readonly Vector2[] _chainFrom = new Vector2[6];
        private readonly Vector2[] _chainTo = new Vector2[6];
        private readonly float[] _chainLife = new float[6];

        public int Despertar => (int)Projectile.ai[0];

        private int Seed => Math.Max(1, (int)Projectile.ai[1]);

        /// <summary>LA TEXTURA: 100 % código (el corazón, los rayos, las
        /// runas y la explosión viven en el PreDraw) — la casa presta el
        /// SoftGlow (el contrato del Guardián: ninguna clase sin textura —
        /// el mismo préstamo de MiniEstallidoPet y SombraPaginaProjectile).</summary>
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = false;            // daño 100 % manual (el patrón del original)
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = FlyLife + FinaleTicks;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;          // la estrella MUERE en las paredes (hasta el 100)
            Projectile.extraUpdates = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            int d = (int)Projectile.ai[0];
            _multicolor = d >= 5;
            _anillo = d >= 10;
            _anilloColor = d >= 15;
            _haces = d >= 20;
            _hacesColor = d >= 25;
            _explota = d >= 30;
            _escalaExplosion = d >= 35 ? 1f : 0.55f;
            _dosAnillos = d >= 55;
            _contra = d >= 75;
            _arcosAnillo = d >= 95;
            _tormenta = Systems.DespertarGrimorio.TormentaAlExplotar(d);
            _arcsAttack = d >= 105;
            _quemadura = Systems.DespertarGrimorio.QuemaduraDelVacio(d);
            _seguimiento = Systems.DespertarGrimorio.SeguimientoEstrella(d);
            _saltos = Systems.DespertarGrimorio.SaltosDeLosArcos(d);
            // v6.185 — EL 100 ATRAVIESA PAREDES (la letra: «adquiere la
            //     capacidad de atravezar paredes la cual era una capacidad
            //     que no tenia hasta ahora»)
            if (d >= 100) Projectile.tileCollide = false;
        }

        // ================================================================
        //  LA IA — el vuelo, el seguimiento, los arcos y la muerte
        // ================================================================
        public override void AI()
        {
            _age += 1f;

            // === LA FASE DE DETONACIÓN: la sinfonía suena y se apaga ===
            if (_dying)
            {
                _dyingAge += 1f;
                Projectile.velocity *= 0.80f;
                if (Main.netMode != NetmodeID.Server && _dyingAge < 14f && _dyingAge % 2f == 0f)
                {
                    float ang = Main.rand.NextFloat(0, MathHelper.TwoPi);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowRod,
                        new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) *
                        Main.rand.NextFloat(1.5f, 4.5f),
                        200, new Color(255, 235, 180), 0.9f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
                if (_age % 3f == 0f)
                {
                    float f = 1f - _dyingAge / FinaleTicks;
                    Lighting.AddLight(Projectile.Center, 0.75f * f, 0.55f * f, 0.30f * f);
                }
                return;
            }

            // === EL VUELO: deceleración suave (el original) ===
            Projectile.velocity *= 0.988f;

            // === EL SEGUIMIENTO (el 2: «algo muy leve quizás un 10 %»): la
            //     estrella corrige SU rumbo hacia la presa más cercana — la
            //     fracción del giro máximo que el nivel presta ===
            if (_seguimiento > 0f)
            {
                NPC presa = PresaCercana(RadioPresaN);
                if (presa != null)
                {
                    Vector2 rumbo = presa.Center - Projectile.Center;
                    if (rumbo.LengthSquared() > 1f)
                    {
                        float giro = MathHelper.WrapAngle(rumbo.ToRotation() - Projectile.velocity.ToRotation());
                        float paso = MathHelper.Clamp(giro, -GiroMax * _seguimiento, GiroMax * _seguimiento);
                        Projectile.velocity = Projectile.velocity.RotatedBy(paso);
                    }
                }
            }

            // === LA ESTELA (EstelaLib — el ring-buffer por identidad) ===
            if (_multicolor)
            {
                EstelaLib.Track(Projectile.whoAmI, 24).Push(Projectile.Center);
                if (_age % 120f == 0f) EstelaLib.PurgeTracks();
            }

            // === LOS ARCOS QUE ATACAN (105+ — la letra: «sus arcos electricos
            //     ataque a enemigos que esten cercas de el y por cada 3
            //     niveles de despertar a partir del nivel 105 el pequeño arco
            //     salta y se ramifica a un enemigo nuevo cercano») ===
            if (_arcsAttack && _age > 4f && _age % 9f == 0f)
                LanzarArcos();

            // La vida de las cadenas decae (para el dibujo).
            for (int c = 0; c < 6; c++)
                if (_chainLife[c] > 0f) _chainLife[c] -= 1f;

            // === LA MUERTE NATURAL: al borde del tiempo — LA SINFONÍA si
            //     explota; si no, la estrella se esfuma a los 120 t ===
            if (Projectile.timeLeft <= FinaleTicks + 2)
            {
                if (_explota) Detonate();
                return;
            }

            // === EL CONTACTO: enemigo tocado → el golpe y la muerte ===
            if (_age > 4f)
            {
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    if ((npc.Center - Projectile.Center).Length() >
                        26f + Math.Max(npc.width, npc.height) * 0.35f) continue;

                    if (_explota) { Detonate(); break; }

                    // v6.185 — EL NIVEL 0-29: «simplemente se desaparece al
                    //     chocar con cualquier pared o enemigo» — el golpe
                    //     del contacto (una vez) y la estrella se va.
                    GolpeDeLaEstrella(npc, Projectile.damage, 3.5f);
                    Projectile.Kill();
                    return;
                }
            }

            // === LA LUZ de la estrella (muestreada) ===
            if (_age % 3f == 0f)
            {
                Color c = ColorDeLaEstrella();
                Lighting.AddLight(Projectile.Center, c.R / 255f * 0.9f,
                    c.G / 255f * 0.9f, c.B / 255f * 0.9f);
            }
        }

        /// <summary>LA PRESA más cercana a la estrella en un radio (el
        /// objetivo válido de la casa).</summary>
        private NPC PresaCercana(float radio)
        {
            NPC mejor = null;
            float mejorD = radio * radio;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float d = (npc.Center - Projectile.Center).LengthSquared();
                if (d < mejorD) { mejorD = d; mejor = npc; }
            }
            return mejor;
        }

        /// <summary>EL GOLPE de la estrella (GolpeMotor — el cauce del motor)
        /// + la QUEMADURA DEL VACÍO (118+: la letra: «adquiere la capacidad
        /// de aplicar el Debuff de Quemadura del Vacio a los enemigos»). El
        /// debuff solo lo aplica (el daño de la quemadura vive en SU buff —
        /// que solo funciona con La Forma Eterna).</summary>
        private bool GolpeDeLaEstrella(NPC npc, int dano, float kb)
        {
            bool dio = Content.Systems.GolpeMotor.Golpear(Projectile, npc, dano, kb, true);
            if (dio) AplicarQuemadura(npc);
            return dio;
        }

        private void AplicarQuemadura(NPC npc)
        {
            if (!_quemadura || npc == null || !npc.active) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            try
            {
                int quemadura = ModContent.BuffType<global::AethonMod.Content.Buffs.QuemaduraVacioExterior>();
                if (quemadura > 0 && !npc.HasBuff(quemadura)) npc.AddBuff(quemadura, 600);
            }
            catch (System.Exception exRegistro) { RegistroSistema.Fallo("EstrellaGrimorio.Quemadura", exRegistro); }
        }

        /// <summary>LOS ARCOS DEL 105: el primero salta al enemigo cercano y
        /// se RAMIFICA a `saltos` enemigos nuevos (cada uno una vez). El
        /// daño del arco: 35 % del disparo (la cadena del original era
        /// 55 % — pero el arco es un extra del básico). El visual de la
        /// cadena vive en los buffers (el dibujo los pinta).</summary>
        private void LanzarArcos()
        {
            if (_saltos <= 0) return;
            int golpe = Math.Max(1, (int)(Projectile.damage * 0.35f));
            Vector2 desde = Projectile.Center;
            NPC anterior = null;
            int slot = 0;
            for (int s = 0; s < _saltos && slot < 6; s++)
            {
                NPC presa = ObjetivoDelArco(desde, 260f, anterior);
                if (presa == null) break;
                GolpeDeLaEstrella(presa, golpe, 1.5f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    _chainFrom[slot] = desde;
                    _chainTo[slot] = presa.Center;
                    _chainLife[slot] = 9f;
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    try { presa.AddBuff(BuffID.Electrified, 180); } catch (System.Exception exRegistro) { RegistroSistema.Fallo("EstrellaGrimorio.Arcos", exRegistro); }
                anterior = presa;
                desde = presa.Center;
                slot++;
            }
        }

        /// <summary>El enemigo válido más cercano a un punto, sin contar al
        /// anterior de la cadena (la ramificación busca uno NUEVO).</summary>
        private static NPC ObjetivoDelArco(Vector2 desde, float radio, NPC excluir)
        {
            NPC mejor = null;
            float mejorD = radio * radio;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                if (npc == excluir) continue;
                float d = (npc.Center - desde).LengthSquared();
                if (d < mejorD) { mejorD = d; mejor = npc; }
            }
            return mejor;
        }

        // ================================================================
        //  LA SINFONÍA — el estallido (INMUTABLE: la copia del original con
        //  la escala como único parámetro)
        // ================================================================
        private void Detonate()
        {
            if (_dying) return;
            _dying = true;
            _dyingAge = 0f;
            Projectile.timeLeft = (int)(FinaleTicks + 2);
            Projectile.velocity = Vector2.Zero;
            float esc = _escalaExplosion;

            if (Main.netMode != NetmodeID.Server)
            {
                // EL ACORDE: trueno grave + campana prismática.
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item12.WithPitchOffset(-0.35f).WithVolumeScale(1.1f), Projectile.Center);
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item122.WithPitchOffset(0.30f).WithVolumeScale(0.7f), Projectile.Center);

                // LAS CHISPAS del estallido (prismáticas + doradas).
                for (int i = 0; i < 30; i++)
                {
                    float ang = Main.rand.NextFloat(0, MathHelper.TwoPi);
                    float spd = Main.rand.NextFloat(3f, 12f);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowRod,
                        new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang) * 0.8f) * spd,
                        220, new Color(255, 240, 190), 1.2f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // EL PAQUETE DE IMPACTO CENTRALIZADO (OndaLib) — la sacudida
                // del acumulador ÚNICO del frame (sin flash de pantalla).
                OndaLib.Kick(9f * esc, 16);
            }

            // === EL DAÑO EN ÁREA (radio 140 × escala) — GolpeMotor ===
            float radio = BurstR * esc;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                if ((npc.Center - Projectile.Center).Length() > radio) continue;
                Content.Systems.GolpeMotor.Golpear(Projectile, npc, Projectile.damage, 3.5f, true);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    try { npc.AddBuff(BuffID.Electrified, 240); } catch (System.Exception exRegistro) { RegistroSistema.Fallo("EstrellaGrimorio.Detonate", exRegistro); }
                AplicarQuemadura(npc);
            }

            // === v6.185 — LA TORMENTA NEBULAR (100+): al explotar la estrella
            //     SE TRANSFORMA en el proyectil del Cetro de Tormenta
            //     Nebular durante 2 s (daña lo que toca) — nace en la
            //     AUTORIDAD y viaja solo (un proyectil normal sincronizado).
            //     ai[2] = los saltos de ramificación que la Tormenta hereda
            //     (110+): 0 = no ramifica. ===
            if (_tormenta && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int saltosTormenta = Systems.DespertarGrimorio.TormentaRamifica(Despertar) ? _saltos : 0;
                int idx = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.TormentaNebularProjectile>(),
                    Projectile.damage, 0f, Projectile.owner, 0f, 0f, saltosTormenta);
                if (idx >= 0)
                {
                    Main.projectile[idx].timeLeft = 120;                     // ~2 s
                    Main.projectile[idx].netUpdate = true;
                    // la Tormenta hereda el despertar (para la implosión del 115+)
                    if (Main.projectile[idx].ModProjectile is global::AethonMod.Content.Projectiles.Cosmic.TormentaNebularProjectile t)
                        t.DespertarHeredado = Despertar;
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            // murió sin sonar (pared, tiempo): la sinfonía igual suena —
            // pero SOLO si el nivel la tiene (el 0-29 se esfuma).
            if (!_dying && _explota) Detonate();
            // el rastro se purga
            try { EstelaLib.ClearTracks(); }
            catch (System.Exception exRegistro) { RegistroSistema.Fallo("EstrellaGrimorio.Purga", exRegistro); }
        }

        // ================================================================
        //  EL DIBUJO — LA ESTRELLA VIVIDA POR EL NIVEL
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                if (_dying) DrawFinale();
                else DrawStar();
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }

            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        // ------------------------------------------------------------------
        //  LA PALETA DE LA ESTRELLA (la letra: «su color va de negro, dorado,
        //  morado y rojo» — tonos oscuros)
        // ------------------------------------------------------------------
        private static readonly Color[] Paleta =
        {
            new(30, 16, 42),      // NEGRO (violeta de tiniebla)
            new(214, 154, 54),   // DORADO
            new(142, 72, 212),   // MORADO
            new(204, 52, 72),    // ROJO
        };
        private static readonly Color GoldHalo = new(255, 195, 85);
        private static readonly Color GoldWarm = new(255, 225, 140);
        private static readonly Color StarBlue = new(150, 180, 255);
        private static readonly Color WhiteIncan = new(255, 250, 235);
        private static readonly Color BrumaVioleta = new(120, 88, 168);
        private static readonly Color BrumaDorada = new(170, 128, 62);
        private static readonly Color NegroVivo = new(26, 14, 36);

        /// <summary>El color ACTUAL de la estrella: negro fijo hasta el 4;
        /// del 5 en adelante el ciclo de la paleta (deriva determinista).</summary>
        private Color ColorDeLaEstrella()
        {
            if (!_multicolor) return NegroVivo;
            return ColorPaleta(Main.GlobalTimeWrappedHourly, 0);
        }

        /// <summary>El color de la paleta en la fase `orden` (0 = la
        /// estrella · 2 = el anillo · 1 = los haces — la letra: «el mismo
        /// patrón de colores pero en orden diferente»): el índice camina
        /// con el tiempo y se SUAVIZA entre pasos contiguos.</summary>
        private Color ColorPaleta(float time, int orden)
        {
            float paso = 2.2f;                           // segundos por color
            float t = time / paso + Seed * 0.37f + orden * 1.0f;
            int i0 = ((int)MathF.Floor(t)) % 4; if (i0 < 0) i0 += 4;
            int i1 = (i0 + 1) % 4;
            float f = t - MathF.Floor(t);
            f = MathHelper.SmoothStep(0f, 1f, f);
            return Color.Lerp(Paleta[i0], Paleta[i1], f);
        }

        /// <summary>El color VIVO de la paleta para lo ADITIVO: el negro de
        /// la paleta es opaco (no suma nada) — para la luz se usa un
        /// violeta oscuro en su lugar (la estrella «negra» se lee por su
        /// cuerpo oscuro en el pase alfa, no por su brillo).</summary>
        private Color ColorLuz(Color c)
        {
            if (c.R < 60 && c.G < 40) return new Color(92, 48, 140);
            return c;
        }

        // ------------------------------------------------------------------
        //  AL VUELO — LA ESTRELLITA VESTIDA
        // ------------------------------------------------------------------
        private void DrawStar()
        {
            try
            {
                float time = Main.GlobalTimeWrappedHourly;
                int seed = Seed;
                int flick = StormLib.FlickTick(time, FlickHz);
                Vector2 center = Projectile.Center - Main.screenPosition;
                Color color = ColorDeLaEstrella();
                Color luz = ColorLuz(color);

                // === 0. EL CUERPO OSCURO (pase ALFA — la estrella NEGRA tiene
                //     cuerpo: un disco de tiniebla que se lee sobre cualquier
                //     fondo) ===
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                Quad(VFXCore.SoftGlow, center, new Vector2(22f, 22f), 0f, new Color(14, 8, 20, 230));
                Quad(VFXCore.SoftGlow, center, new Vector2(12f, 12f), 0f, new Color(40, 22, 54, 255));
                Main.spriteBatch.End();

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // === 1. LA ESTELA DE BRUMA (desde el 5 — el aliento multicolor) ===
                if (_multicolor)
                {
                    Vector2 vel = Projectile.velocity;
                    for (int i = 1; i <= 3; i++)
                    {
                        Vector2 world = Projectile.Center - vel * (0.55f * i);
                        Color bc = ColorPaleta(time - 0.12f * i, 0);
                        BrumaFX.Puff(world - Main.screenPosition, 14f + 5f * i, ColorLuz(bc),
                            seed + 300 + i * 37, time - 0.12f * i,
                            alpha: 0.22f - 0.05f * i, quality: 0.5f);
                    }

                    // LA ESTELA DE RIBBON (EstelaLib — el camino real)
                    Vector2[] camino = EstelaLib.Track(Projectile.whoAmI, 24).Points();
                    if (camino.Length > 2)
                    {
                        for (int i = 0; i < camino.Length; i++)
                            camino[i] -= Main.screenPosition;
                        EstelaLib.Ribbon(Main.spriteBatch, camino, 15f,
                            EstelaProfile.Comet, luz, 0.55f, seed + 7, time);
                    }
                }

                // === 2. EL CORAZÓN DE LUZ (LumenLib — el original: bloom + destello) ===
                LumenLib.BloomPulse(Main.spriteBatch, center, 30f, luz, 0.55f, time, 1.6f, 3);
                LumenLib.Flare(Main.spriteBatch, center, 26f, luz, 0.35f, time * 0.5f);

                // === 3. EL AURORA (la sinfonía cromática girando — desde el 5) ===
                if (_multicolor)
                {
                    float drift = LumenLib.Drift(time, seed, 0.22f);
                    LumenLib.Aurora(Main.spriteBatch, center, 34f, time, drift, 0.26f, 9);
                }

                // === 4. LOS HACES EN CADA PUNTA (los cinco rayos de sol — 20+):
                //     20-24 con el color de la estrella; 25+ MULTICOLOR (otro
                //     orden) ===
                if (_haces)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        float ang = i / 5f * MathHelper.TwoPi + time * 0.8f;
                        Vector2 dir = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang));
                        Color c = _hacesColor ? ColorLuz(ColorPaleta(time + i * 0.25f, 1)) : luz;
                        float pulse = 0.5f + 0.5f * (float)Math.Sin(time * 3.2f + i * 1.26f);
                        LumenLib.Ray(Main.spriteBatch, center + dir * 6f, dir,
                            20f + 14f * pulse, 7f, c, 0.30f + 0.25f * pulse, pulse);
                    }
                }

                // === 5. EL ANILLO RÚNICO DEL AGUJERO NEGRO (10+): las seis
                //     runas orbitando (la misma tabla de glifos del original).
                //     15+: la paleta del anillo (orden 2). 55+: el SEGUNDO anillo.
                //     75+: los dos giran cada uno en un sentido (el de afuera
                //     invierte). ===
                if (_anillo)
                {
                    Color cAnillo = _anilloColor ? ColorLuz(ColorPaleta(time, 2)) : GoldWarm;
                    float giro1 = 0.30f;                       // el original: +0,30 rad/s
                    DrawOrbitingRunes(center, 24f, time, seed, 1f, cAnillo, giro1);
                    if (_dosAnillos)
                    {
                        float giro2 = _contra ? -0.30f : 0.30f;   // 75+: sentidos opuestos
                        DrawOrbitingRunes(center, 40f, time, seed + 3, 1f, cAnillo, giro2);
                    }
                }

                // === 6. LOS ARCOS ENTRE RUNAS (StormLib — los dos del original
                //     alrededor del corazón, desde el 10) y — 95+ — EL ARCO DE
                //     CADA ANILLO recorriéndolo en el sentido CONTRARIO al
                //     giro de su anillo ===
                if (_anillo)
                {
                    for (int a = 0; a < 2; a++)
                    {
                        if (!StormLib.IsLit(seed + 40 + a * 17, flick, 0.65f)) continue;
                        float span = 1.0f + 0.6f * VFXCore.Hash01(seed, 941 + a, flick);
                        float baseA = time * (1.1f + 0.4f * a) + a * 2.6f;
                        StormLib.ArcRing(Main.spriteBatch, center, 20f + 4f * a,
                            baseA, baseA + span, seed + 500 + a * 13, flick,
                            3.2f, Tint(GoldWarm, 0.55f), Tint(WhiteIncan, 0.9f), 1f, 9);
                    }
                }
                if (_arcosAnillo)
                {
                    // el arco del anillo 1: gira al −0,30 (contrario a su +0,30)
                    float a1 = -time * 0.30f * 2.2f + seed * 0.1f;
                    StormLib.ArcRing(Main.spriteBatch, center, 24f, a1, a1 + 1.2f, seed + 701, flick,
                        3.0f, Tint(StarBlue, 0.55f), Tint(WhiteIncan, 0.9f), 1f, 10);
                    if (_dosAnillos)
                    {
                        // el arco del anillo 2: contrario a SU giro (+0,30 → −; −0,30 → +)
                        float dir2 = _contra ? 1f : -1f;
                        float a2 = dir2 * time * 0.30f * 2.2f + 2.1f;
                        StormLib.ArcRing(Main.spriteBatch, center, 40f, a2, a2 + 1.2f, seed + 709, flick,
                            3.0f, Tint(StarBlue, 0.55f), Tint(WhiteIncan, 0.9f), 1f, 12);
                    }
                }
                StormLib.EndCap(Main.spriteBatch, center, 9f,
                    Tint(luz, 0.55f), Tint(WhiteIncan, 0.85f), 1f);

                // === 7. LAS CADENAS de los arcos del 105 (el dibujo) ===
                for (int c = 0; c < 6; c++)
                {
                    if (_chainLife[c] <= 0f) continue;
                    if (!StormLib.IsLit(seed + 61 + c * 37, flick, 0.60f)) continue;
                    float a = MathHelper.Clamp(_chainLife[c] / 9f, 0f, 1f);
                    StormLib.ChainBolt(Main.spriteBatch,
                        _chainFrom[c] - Main.screenPosition, _chainTo[c] - Main.screenPosition,
                        seed + 61 + c * 37, flick, 4.2f,
                        Tint(StarBlue, 0.75f * a), Tint(new Color(230, 240, 255), 0.9f * a),
                        1f, 7, 10f);
                }

                Main.spriteBatch.End();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
        }

        /// <summary>Las runas orbitando (la tabla de glifos del original —
        /// la forma de los agujeros negros), con SU color y SU giro.</summary>
        private void DrawOrbitingRunes(Vector2 center, float radius, float time,
            int seed, float fade, Color color, float giro)
        {
            Vector2[][] glyphs = RuneGlyphs;
            const int Count = 6;
            float gScale = 0.62f;
            Color halo = Tint(color, 0.20f);
            Color trazo = Tint(color, 0.85f);
            for (int g = 0; g < Count; g++)
            {
                float ang = g / (float)Count * MathHelper.TwoPi + time * giro;
                float floatR = radius + 2.4f * gScale * (float)Math.Sin(time * 1.35f + g * 0.9f);
                float bobY = 2.0f * gScale * (float)Math.Sin(time * 0.85f + g * 1.7f);
                Vector2 pos = center + new Vector2(
                    (float)Math.Cos(ang) * floatR,
                    (float)Math.Sin(ang) * floatR + bobY);
                float pulse = 0.75f + 0.25f * (float)Math.Sin(time * 2.4f + g * 1.3f);

                Quad(VFXCore.SoftGlow, pos, new Vector2(26f * gScale, 26f * gScale), 0f,
                    new Color(halo.R, halo.G, halo.B, (byte)(halo.A * pulse * fade)));

                Vector2[] strokes = glyphs[(g + 1) % glyphs.Length];
                for (int s = 0; s < strokes.Length; s += 2)
                {
                    Vector2 a = pos + strokes[s] * gScale;
                    Vector2 b = pos + strokes[s + 1] * gScale;
                    Vector2 mid = (a + b) * 0.5f;
                    Vector2 d = b - a;
                    float len = d.Length();
                    if (len < 0.01f) continue;
                    Capsule(mid, len, 3.2f * gScale, (float)Math.Atan2(d.Y, d.X),
                        new Color(trazo.R, trazo.G, trazo.B, (byte)(trazo.A * pulse * fade)));
                }

                Vector2 pearl = pos - new Vector2(0f, 9.5f * gScale);
                Quad(VFXCore.SoftGlow, pearl, new Vector2(6.4f * gScale, 6.4f * gScale), 0f,
                    new Color(halo.R, halo.G, halo.B, (byte)(halo.A * 3.1f * pulse * fade)));
                Quad(VFXCore.SoftGlow, pearl, new Vector2(3.0f * gScale, 3.0f * gScale), 0f,
                    Tint(WhiteIncan, 0.9f * pulse * fade));
            }
        }

        // ------------------------------------------------------------------
        //  AL MORIR — LA SINFONÍA (el estallido del original, INMUTABLE,
        //  con la escala como único parámetro: ×1 = idéntico)
        // ------------------------------------------------------------------
        private void DrawFinale()
        {
            try
            {
                float esc = _escalaExplosion;
                float time = Main.GlobalTimeWrappedHourly;
                int seed = Seed;
                int flick = StormLib.FlickTick(time, FlickHz);
                Vector2 center = Projectile.Center - Main.screenPosition;
                float phase = MathHelper.Clamp(_dyingAge / FinaleTicks, 0f, 1f);
                float fade = 1f - phase;
                float burst = 1f - phase;
                float drift = LumenLib.Drift(time, seed, 0.30f);
                Color prism = LumenLib.Hue(drift, 0.6f, 1f);

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // === 1. LA NUBE DE BRUMA de la detonación (en pase ALFA) ===
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                float cloudR = (34f + 64f * phase) * esc;
                BrumaFX.Cloud(Projectile.Center - Main.screenPosition, cloudR,
                    BrumaVioleta, seed + 71, time, puffs: 6,
                    alpha: 0.36f * fade + 0.06f);
                BrumaFX.Cloud(Projectile.Center - Main.screenPosition, cloudR * 0.62f,
                    BrumaDorada, seed + 137, time * 0.8f, puffs: 4,
                    alpha: 0.28f * fade + 0.04f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // === 2. EL ESTALLIDO DE LUZ + LA ONDA EXPANSIVA (OndaLib) ===
                OndaLib.Shock(Main.spriteBatch, center, phase, 215f * esc, prism,
                    MathHelper.Clamp(0.85f * fade + 0.15f, 0f, 1f), seed, 10f,
                    OndaFalloff.Quadratic, chromatic: true);

                float flashI = MathHelper.Clamp(1f - _dyingAge / 10f, 0f, 1f);
                if (flashI > 0f)
                {
                    StormLib.ImpactFlash(Main.spriteBatch, center,
                        110f * esc * (0.6f + 0.4f * flashI), prism, flashI, time * 0.9f);
                    if (_dyingAge < 3f)
                        StormLib.ImpactFlash(Main.spriteBatch, center, 72f * esc,
                            GoldWarm, flashI * 0.7f, -time * 1.2f);
                }
                LumenLib.Flare(Main.spriteBatch, center,
                    (70f + 90f * phase) * esc * (0.5f + 0.5f * fade), prism,
                    0.60f * fade + 0.30f * flashI, time * 0.4f);

                // EL AURORA COMPLETA (la sinfonía cromática de despedida).
                LumenLib.Aurora(Main.spriteBatch, center,
                    (50f + 70f * phase) * esc, time, drift, 0.45f * fade, 15);

                // === 3. LOS SEIS RAYOS RADIALES (StormLib — reventando afuera) ===
                for (int i = 0; i < 6; i++)
                {
                    if (!StormLib.IsLit(seed + 130 + i * 29, flick, 0.55f)) continue;
                    float ang = i / 6f * MathHelper.TwoPi + seed % 7 * 0.13f;
                    Vector2 dir = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang));
                    float inner = (26f + 85f * phase) * esc;
                    float outer = inner + 42f * esc * burst;
                    StormLib.MultiBolt(Main.spriteBatch,
                        center + dir * inner, center + dir * outer,
                        seed + 130 + i * 29, flick,
                        Math.Max(3f, (7f * burst + 2f) * esc),
                        Tint(GoldWarm, 0.60f * fade), Tint(prism, 0.60f * fade),
                        Tint(WhiteIncan, 0.92f * fade),
                        0.9f * fade + 0.1f, 16f * esc, 9);
                }

                // === 4. LAS RUNAS VOLANDO (la firma que revienta) ===
                DrawOrbitingRunes(center, (26f + 78f * phase) * esc, time, seed, fade, GoldWarm, 0.30f);

                // === 5. LA ONDA DE CHOQUE expandiéndose ===
                float waveR = (30f + 150f * phase) * esc;
                Quad(VFXCore.Ring, center, VFXCore.RingQuadSize(waveR),
                    phase * 2.6f, Tint(GoldWarm, 0.40f * fade * fade));
                Quad(VFXCore.Ring, center, VFXCore.RingQuadSize(waveR * 0.66f),
                    -phase * 3.1f, Tint(prism, 0.30f * fade * fade));

                Main.spriteBatch.End();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
        }

        // ------------------------------------------------------------------
        //  HELPERS (los del original — la misma tabla de glifos)
        // ------------------------------------------------------------------
        private static readonly Vector2[][] RuneGlyphs = new Vector2[][]
        {
            // EL SOL ROTO
            new Vector2[] { new(0f, -7f), new(0f, 7f), new(-3.5f, -3f), new(0f, -6.5f), new(3.5f, -3f), new(0f, -6.5f), new(-3.5f, 3.5f), new(3.5f, 3.5f), new(-2f, 5.5f), new(2f, 5.5f) },
            // EL CETRO
            new Vector2[] { new(0f, 7f), new(0f, -4f), new(0f, -4f), new(-3f, -7f), new(0f, -4f), new(3f, -7f), new(-2.5f, 0f), new(2.5f, 0f), new(-2.5f, 3f), new(2.5f, 3f) },
            // LA ESTRELLA DOBLE
            new Vector2[] { new(0f, 7f), new(0f, -7f), new(-4f, 0f), new(4f, 0f), new(-2.5f, -4.5f), new(2.5f, 4.5f), new(2.5f, -4.5f), new(-2.5f, 4.5f) },
        };

        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        private static void Capsule(Vector2 mid, float len, float width, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Quad(VFXCore.SoftGlow, mid, new Vector2(len + width, width * 1.9f), rot, tint);
        }

        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }
    }
}
