using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// VerboPrimordialProjectile — v6.42 — EL VERBO PRIMORDIAL.
    ///
    /// LA PALABRA que pronunció la primera luz — el arma que habla con
    /// TODAS las bibliotecas de la casa, una por movimiento, como una
    /// sinfonía en siete tiempos (420 ticks = 7 s exactos):
    ///
    ///   I · EL PULSO      — OndaLib + PulsoLib + AudioLib: anillos que
    ///                        empujan la pantalla al paso de la palabra.
    ///   II · EL SELLO     — SigiloLib: el círculo rúnico del conjuro,
    ///                        contrarrotando alrededor del glifo.
    ///   III · LA CORONA   — OrbitaLib: el anillo de energía con sus
    ///                        fotones y sus ecos de anillo.
    ///   IV · LA TORMENTA  — StormLib: la corona de arcos voltaicos y
    ///                        las cadenas que pican a los cercanos (0,5×).
    ///   V · EL FUEGO      — PyraLib + LumenLib + VFXPalettes: la lengua
    ///                        y la llama solares, el destello de la forja.
    ///   VI · EL DESGARRO  — RiftLib + TajoLib + GravLens: la herida que
    ///                        la palabra va dejando EN el mundo, curvando
    ///                        la luz a su paso; lo que toca, lo tajea.
    ///   VII · LA SINFONÍA — TODAS A LA VEZ: CodigosLib.SolVivo de núcleo,
    ///                        SierpesLib la cría orbitando, NebulaLib el
    ///                        gas, TelaLib la cinta, EcosLib los fantasmas
    ///                        del viaje, EstelaLib la estela, ParticleManager
    ///                        la lluvia, OndaLib el telegraph del final…
    ///                        y al último tick: LA DETONACIÓN.
    ///
    /// EL DAÑO crece con el movimiento (×1,0 → ×1,4: la palabra gana
    /// volumen) y la detonación final pega 2,5× en 240 px con quemadura.
    ///
    /// CONTRATOS DE LA CASA respetados al milímetro: el búfer de VFXCore
    /// (coords de MUNDO) se vuelca ANTES de abrir lote propio; los
    /// compositores que gestionan su lote (CodigosLib, SierpesLib) se
    /// llaman con el lote CERRADO; los de pase directo (OndaLib, Lumen,
    /// Orbita, Storm, Pyra, Rift, Tajo, Estela, Tela) van en el lote
    /// propio con coords de PANTALLA; cero Main.rand en el render.
    /// </summary>
    public class VerboPrimordialProjectile : ModProjectile
    {
        public const int TicksPorMovimiento = 60;
        public const int Movimientos = 7;
        public const int VidaTotal = TicksPorMovimiento * Movimientos;

        // === LA PALETA (la palabra es BLANCA — el color lo pone el movimiento). ===
        private static readonly Color PalabraBlanca = new(255, 250, 240);

        private int _edad;
        private readonly Vector2[] _segs = new Vector2[10];
        private readonly float[] _segsAng = new float[10];
        private EcosLib.Memoria _memoria;
        private readonly Vector2[] _camino = new Vector2[16];
        private readonly List<(Vector2 pos, int edad)> _tajos = new(8);

        private int Seed => Math.Max(1, Projectile.identity + 1667);

        private int Movimiento => Math.Min(Movimientos - 1, _edad / TicksPorMovimiento);
        private int EdadEnMovimiento => _edad - Movimiento * TicksPorMovimiento;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = VidaTotal;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.aiStyle = -1;
            _memoria = EcosLib.Crear();
        }

        public override void AI()
        {
            _edad++;
            int mov = Movimiento;
            int em = EdadEnMovimiento;

            // === EL VUELO: recto y sereno (la palabra no se apresura). ===
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, PalabraBlanca.ToVector3() * 0.9f);
            EcosLib.Registrar(ref _memoria, Projectile.Center, Projectile.rotation);

            // EL CAMINO (el historial para la estela y el desgarro).
            if (_edad % 4 == 0)
            {
                for (int i = _camino.Length - 1; i > 0; i--) _camino[i] = _camino[i - 1];
                _camino[0] = Projectile.Center;
            }

            // LA CADENA de la cría (SierpesLib — el cuerpo sigue a la cabeza).
            _segs[0] = Projectile.Center;
            _segsAng[0] = Projectile.rotation;
            for (int i = 1; i < _segs.Length; i++)
            {
                Vector2 delta = _segs[i - 1] - _segs[i];
                float d = delta.Length();
                if (d > 14f && d > 0.001f)
                    _segs[i] += delta * ((d - 14f) / d);
                _segsAng[i] = i == 1 ? Projectile.rotation :
                    (_segs[i - 1] - _segs[i]).ToRotation();
            }

            // LA CINTA (TelaLib — la cinta de la casa recibiendo su primera arma).
            Cinta cinta = Cinta.Adquirir(Projectile.whoAmI, 24);
            cinta.Empujar(Projectile.Center);

            // === EL CAMBIO DE MOVIMIENTO: cada 60 ticks la palabra cambia
            //     de voz — un pulso, un empujón de pantalla y el sonido. ===
            if (em == 0 && _edad > 1)
            {
                AudioLib.Sonar(FamiliaDe(mov), "apertura", Projectile.Center, 0.9f, 0.1f * mov);
                PulsoLib.EmpujarPantalla(TinteDe(mov), 0.16f, 8);

                // El daño crece con el movimiento (la palabra gana volumen).
                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.damage = Math.Max(1, (int)(Projectile.originalDamage * (1f + 0.07f * mov)));
                    Projectile.netUpdate = true;
                }
            }

            // === LOS EFECTOS LÓGICOS POR MOVIMIENTO. ===
            switch (mov)
            {
                case 3:  // IV — LA TORMENTA: las cadenas pican (autoridad).
                    if (Main.myPlayer == Projectile.owner && _edad % 20 == 0)
                    {
                        int picadas = 0;
                        foreach (NPC npc in Main.ActiveNPCs)
                        {
                            if (picadas >= 2) break;
                            if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                            if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > 420f * 420f) continue;
                            Content.Systems.GolpeMotor.Golpear(Projectile, npc,
                                Math.Max(1, (int)(Projectile.damage * 0.5f)), 3f, true);
                            picadas++;
                        }
                    }
                    break;

                case 5:  // VI — EL DESGARRO: la lente curva la luz a su paso.
                    GravLens.Registrar(Projectile.Center, 96f, 0.30f, 2);
                    break;

                case 6:  // VII — LA SINFONÍA: la lluvia de partículas + la carga.
                    GravLens.Registrar(Projectile.Center, 130f, 0.22f, 2);
                    if (_edad % 5 == 0)
                    {
                        float ang = VFXCore.Hash01(Seed, _edad, 3) * MathHelper.TwoPi;
                        var p = new ParticleData
                        {
                            Position = Projectile.Center,
                            Velocity = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 2.2f,
                            Scale = new Vector2(1.6f, 1.6f),
                            PackedColor = ParticleManager.PackColor(Color.White),
                            PackedStartColor = ParticleManager.PackColor(new Color(255, 245, 210)),
                            PackedEndColor = ParticleManager.PackColor(new Color(120, 180, 255, 0)),
                            TimeLeft = 34,
                            Duration = 34,
                            TextureId = ParticleTex.SparkleStar,
                            BlendMode = 1,
                            LayerPriority = LayerPriorities.BeforeProjectiles,
                        };
                        p.EnableComponent(ComponentFlag.FadeOut);
                        p.EnableComponent(ComponentFlag.ColorShift);
                        ParticleManager.Spawn(p);
                    }
                    break;
            }

            // Los tajos del movimiento VI envejecen y mueren.
            for (int i = _tajos.Count - 1; i >= 0; i--)
                if (_edad - _tajos[i].edad > 12) _tajos.RemoveAt(i);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // VI — EL DESGARRO: lo que la palabra toca durante el desgarro
            // lleva la marca: UN TAJO al descubierto en el punto de mordida.
            if (Movimiento == 5 && _tajos.Count < 8)
                _tajos.Add((target.Center, _edad));
        }

        public override void OnKill(int timeLeft)
        {
            // LA PALABRA termina siempre con su detonación.
            if (Main.netMode != NetmodeID.Server)
            {
                Cinta.Soltar(Projectile.whoAmI);
                AudioLib.Sonar(AudioLib.Familia.Cosmica, "carga", Projectile.Center, 1f, 0.3f);
            }
            if (Main.myPlayer == Projectile.owner || Main.netMode == NetmodeID.SinglePlayer)
            {
                int idx = Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                    Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<VerboDetonacionProjectile>(),
                    Math.Max(1, (int)(Projectile.originalDamage * 2.5f)), 8f, Projectile.owner,
                    Movimiento);
                if (idx >= 0)
                    Main.projectile[idx].ai[0] = Movimiento;
            }
        }

        /// <summary>La voz de cada movimiento (las seis familias de la casa).</summary>
        private static AudioLib.Familia FamiliaDe(int mov) => mov switch
        {
            0 => AudioLib.Familia.Cosmica,
            1 => AudioLib.Familia.Runico,
            2 => AudioLib.Familia.Solar,
            3 => AudioLib.Familia.Electrica,
            4 => AudioLib.Familia.Solar,
            5 => AudioLib.Familia.Desgarro,
            _ => AudioLib.Familia.Cosmica,
        };

        private static Color TinteDe(int mov) => mov switch
        {
            0 => new Color(160, 200, 255),
            1 => new Color(255, 214, 130),
            2 => new Color(255, 160, 90),
            3 => new Color(170, 230, 255),
            4 => new Color(255, 120, 60),
            5 => new Color(220, 80, 160),
            _ => new Color(255, 250, 240),
        };

        // ==================================================================
        //  EL RENDER — el concierto: cada movimiento con su biblioteca.
        // ==================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                int mov = Movimiento;
                int em = EdadEnMovimiento;
                float time = Main.GlobalTimeWrappedHourly;
                Vector2 pos = Projectile.Center - Main.screenPosition;
                float brilloMov = Math.Min(1f, em / 6f);   // el arranque de cada movimiento

                // ==============================================================
                //  FASE 1 — EL BÚFER (coords de MUNDO): SigiloLib, NebulaLib,
                //  EcosLib y los quads sueltos. Se vuelca ANTES de todo lote.
                // ==============================================================
                if (mov == 1)
                {
                    // II — EL SELLO: dos círculos rúnicos contrarrotando.
                    SigiloLib.AnilloRunico(Projectile.Center, 64f, 40f, -0.30f,
                        time * 0.55f, 7, 1.4f, time, 2,
                        new Color(255, 214, 130), new Color(255, 242, 200), 0.9f * brilloMov);
                    SigiloLib.AnilloRunico(Projectile.Center, 96f, 58f, 0.38f,
                        -time * 0.35f, 10, 1.1f, time, 5,
                        new Color(150, 190, 255), new Color(220, 235, 255), 0.65f * brilloMov);
                }
                if (mov == 6)
                {
                    // VII — LA SINFONÍA: el gas de la nebulosa + los fantasmas
                    // del viaje completo + el aura de la palabra.
                    NebulaLib.Nube(Projectile.Center, 52f, time, Seed,
                        new Color(255, 245, 220), new Color(140, 110, 220), 0.11f, 14);
                    EcosLib.ColaHistoria(ref _memoria, 8, 7, new Color(255, 248, 225),
                        new Vector2(30f, 14f), 0.5f, 1.3f);
                    VFXCore.Quad(Projectile.Center, TinteDe(6) * 0.30f, new Vector2(120f, 120f));
                }
                // EL GLIFO-CUERPO (todos los movimientos): la palabra escrita.
                VFXCore.Quad(Projectile.Center, PalabraBlanca * (0.55f * brilloMov),
                    new Vector2(38f, 38f));
                // El anillo del cambio de movimiento (los 12 primeros ticks).
                if (em < 12 && _edad > 1)
                    VFXCore.Quad(Projectile.Center, TinteDe(mov) * (0.5f * (1f - em / 12f)),
                        VFXCore.RingQuadSize(30f + 90f * (em / 12f)), 0f, VFXCore.Ring);

                VFXCore.FlushAdditive(null, false);

                // ==============================================================
                //  FASE 2 — LOS COMPOSITORES CON LOTE PROPIO (coords de MUNDO,
                //  cerrado→cerrado): CodigosLib y SierpesLib.
                // ==============================================================
                if (mov == 6)
                {
                    CodigosLib.SolVivo(Projectile.Center, 30f, time, Seed, 0.95f);
                    SierpesLib.CriaEstelar(_segs, _segsAng, _segs.Length, time, Seed, 0.85f);
                }

                // ==============================================================
                //  FASE 3 — EL LOTE PROPIO (coords de PANTALLA): el resto
                //  del concierto, movimiento a movimiento.
                // ==============================================================
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                switch (mov)
                {
                    case 0:
                    {
                        // I — EL PULSO: anillos al paso de la palabra.
                        float prog = (em % 15) / 15f;
                        OndaLib.Pulse(Main.spriteBatch, pos, prog, 70f,
                            new Color(160, 200, 255), 0.7f, Seed);
                        OndaLib.Pulse(Main.spriteBatch, pos, (prog + 0.5f) % 1f, 70f,
                            new Color(120, 160, 255), 0.4f, Seed + 3);
                        LumenLib.Bloom(Main.spriteBatch, pos, 34f, PalabraBlanca, 0.8f);
                        break;
                    }
                    case 1:
                    {
                        // II — EL SELLO: el corazón del conjuro.
                        LumenLib.Bloom(Main.spriteBatch, pos, 30f, new Color(255, 232, 170), 0.75f);
                        break;
                    }
                    case 2:
                    {
                        // III — LA CORONA: el anillo de energía + fotones + ecos.
                        OrbitaLib.AnilloEnergia(pos, 54f, time, Seed,
                            StormLib.FlickTick(time, 11f), front: false,
                            hot: new Color(255, 200, 120), mid: new Color(255, 120, 50),
                            deep: new Color(200, 40, 20), bright: 0.9f);
                        OrbitaLib.Fotones(pos, 54f, time, 4,
                            new Color(255, 140, 60), new Color(255, 240, 210));
                        OrbitaLib.EcosAnillo(pos, 54f, time, 0.2f,
                            new Color(255, 180, 110), new Color(200, 60, 30));
                        LumenLib.Bloom(Main.spriteBatch, pos, 30f, new Color(255, 190, 120), 0.7f);
                        break;
                    }
                    case 3:
                    {
                        // IV — LA TORMENTA: la corona de arcos voltaicos.
                        StormLib.ArcRing(Main.spriteBatch, pos, 52f, 0f, MathHelper.TwoPi,
                            Seed, StormLib.FlickTick(time, 15f), 3.5f,
                            new Color(150, 200, 255), new Color(230, 245, 255), 0.9f, 12);
                        // Las cadenas hacia los cercanos (solo la cara: la lógica
                        // del daño vive en el AI — aquí es el retrato).
                        int cadenas = 0;
                        foreach (NPC npc in Main.ActiveNPCs)
                        {
                            if (cadenas >= 2) break;
                            if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                            if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > 420f * 420f) continue;
                            StormLib.ChainBolt(Main.spriteBatch, pos,
                                npc.Center - Main.screenPosition, Seed + cadenas,
                                StormLib.FlickTick(time, 15f), 3f,
                                new Color(150, 200, 255), new Color(235, 245, 255), 0.8f);
                            cadenas++;
                        }
                        LumenLib.Bloom(Main.spriteBatch, pos, 30f, new Color(200, 230, 255), 0.75f);
                        break;
                    }
                    case 4:
                    {
                        // V — EL FUEGO: la llama y la lengua solares + el destello.
                        PyraLib.Flame(Main.spriteBatch, pos, 30f, PyraPalettes.SolarFire,
                            Seed, time, 1f);
                        Vector2 dirV = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                        PyraLib.Tongue(Main.spriteBatch, pos, 40f, 14f, PyraPalettes.SolarFire,
                            0.7f, Seed + 5, time, 1f);
                        LumenLib.Flare(Main.spriteBatch, pos, 44f,
                            new Color(255, 200, 120), 0.8f * brilloMov, time * 0.4f);
                        break;
                    }
                    case 5:
                    {
                        // VI — EL DESGARRO: la herida que la palabra deja EN el
                        // mundo (la grieta por DONDE ya pasó) + los tajos.
                        Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                        Vector2 atras = pos - dir * 250f;
                        Vector2[] camino = RiftLib.CaminoDesgarro(atras, dir, 250f,
                            Seed, time, 0.45f);
                        RiftLib.Grieta(Main.spriteBatch, camino, 0.55f, 14f,
                            RiftPaletas.Entropia, 0.9f, Seed, time, plano: true);
                        LumenLib.Bloom(Main.spriteBatch, pos, 30f, new Color(230, 170, 255), 0.75f);

                        // LOS TAJOS de las mordidas (la marca del que tocó).
                        foreach (var (tpos, tedad) in _tajos)
                        {
                            float tprog = (_edad - tedad) / 12f;
                            TajoLib.Tajo(tpos - Main.screenPosition, 44f,
                                -MathHelper.PiOver4 - 0.5f, -MathHelper.PiOver4 + 0.5f,
                                tprog, 1f - tprog * 0.4f, 13f,
                                new Color(220, 90, 255), Color.White, Seed + tpos.GetHashCode() % 97,
                                time);
                        }
                        break;
                    }
                    case 6:
                    {
                        // VII — LA SINFONÍA: la estela (EstelaLib) + la cinta
                        // (TelaLib — su PRIMERA arma) + el telegraph del final.
                        int n = 0;
                        var pts = new Vector2[_camino.Length];
                        for (int i = 0; i < _camino.Length; i++)
                        {
                            if (_camino[i] != Vector2.Zero)
                                pts[n++] = _camino[i] - Main.screenPosition;
                        }
                        if (n >= 3)
                        {
                            var recorte = new Vector2[n];
                            Array.Copy(pts, recorte, n);
                            EstelaLib.Ribbon(Main.spriteBatch, recorte, 20f,
                                EstelaProfile.Comet, new Color(255, 246, 220), 0.8f,
                                Seed, time, false, 120f);
                        }

                        Cinta cinta = Cinta.De(Projectile.whoAmI);
                        cinta?.Dibujar(Main.spriteBatch, new Cinta.CintaSpec
                        {
                            Ancho = 16f,
                            Perfil = Cinta.PerfilAncho.Huso,
                            ColorCuerpo = new Color(255, 230, 170),
                            ColorNúcleo = new Color(255, 250, 240),
                            Intensidad = 0.85f,
                            EscalaFase = 1.2f,
                            ConNúcleo = true,
                            LargoMáximo = 240f,
                        });

                        // EL TELEGRAPH DEL FINAL (los 20 primeros ticks).
                        if (em < 20)
                        {
                            float prog = em / 20f;
                            for (int k = 0; k < 4; k++)
                            {
                                float ang = k * MathHelper.PiOver2 + MathHelper.PiOver4;
                                OndaLib.Telegrafo(Main.spriteBatch, pos,
                                    pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 170f,
                                    prog, new Color(255, 244, 214), 2.5f);
                            }
                        }
                        LumenLib.Bloom(Main.spriteBatch, pos, 40f, PalabraBlanca, 0.9f);
                        break;
                    }
                }

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            if (wasActive)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }

    /// <summary>
    /// VerboDetonacionProjectile — v6.42 — LA DETONACIÓN DEL VERBO.
    ///
    /// El ÚLTIMO ALIENTO de la palabra: la explosión que habla con las
    /// bibliotecas del estruendo — la onda cromática doble (OndaLib),
    /// la flor de fuego (PyraLib.Estallido), las cadenas fractales a los
    /// seis enemigos más cercanos (StormLib.MultiBolt), el impacto del
    /// desgarro (RiftLib.TearImpacto — la cámara tronando de lado), la
    /// lluvia de partículas (ParticleManager + los presets de la casa),
    /// el flash (PulsoLib) y la voz (AudioLib). 2,5× de daño en 240 px
    /// con quemadura — la palabra se despide a gritos.
    /// </summary>
    public class VerboDetonacionProjectile : ModProjectile
    {
        public const int Vida = 36;
        public const float RadioDetonacion = 240f;

        private bool _golpeDado;

        private int Seed => Math.Max(1, Projectile.identity + 1777);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.aiStyle = -1;
            Projectile.netImportant = false;
        }

        public override bool? CanDamage() => false;
        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.95f, 0.85f) * 1.6f);

            if (!_golpeDado)
            {
                _golpeDado = true;

                // === EL GOLPE (la autoridad del dueño + EsObjetivo) — v6.50 —
                //     GolpeMotor: el cauce del motor (crítica real, varianza,
                //     on-hit y sync MP del propio motor). ===
                if (Main.myPlayer == Projectile.owner)
                {
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                        if (Vector2.Distance(npc.Center, Projectile.Center) > RadioDetonacion) continue;
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, Projectile.damage, 8f, true);
                        npc.AddBuff(BuffID.OnFire, 200);
                    }
                }

                // === EL PAQUETE DEL ESTRUENDO. ===
                OndaLib.Kick(9f, 14, -1f);
                PulsoLib.EmpujarPantalla(new Color(255, 246, 220), 0.4f, 14);
                RiftLib.TearImpacto(Projectile.Center, -Vector2.UnitY, 90f,
                    RiftPaletas.Entropia, Seed, 1.2f);
                AudioLib.Sonar(AudioLib.Familia.Cosmica, "impacto", Projectile.Center, 1f, 0f);
                AudioLib.Sonar(AudioLib.Familia.Desgarro, "muerte", Projectile.Center, 0.8f, 0.2f);

                // === LA LLUVIA DE PARTÍCULAS (la librería de partículas). ===
                ParticlePresets.Explosion(Projectile.Center, RadioDetonacion * 0.8f, 46,
                    new Color(255, 250, 220), new Color(255, 120, 40), 42);
                ParticlePresets.RingPulse(Projectile.Center, RadioDetonacion,
                    new Color(255, 244, 214), 30);
                ParticlePresets.RingPulse(Projectile.Center, RadioDetonacion * 0.6f,
                    new Color(160, 200, 255), 24);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                float prog = 1f - Projectile.timeLeft / (float)Vida;
                Vector2 pos = Projectile.Center - Main.screenPosition;
                float time = Main.GlobalTimeWrappedHourly;

                // LAS ONDAS: la doble cromática (la escuela de la casa).
                OndaLib.Shock(Main.spriteBatch, pos, prog, RadioDetonacion,
                    new Color(255, 214, 120), 1f, Seed, 14f,
                    OndaFalloff.Quadratic, true);
                OndaLib.Shock(Main.spriteBatch, pos, prog * 0.8f, RadioDetonacion * 0.75f,
                    Color.White, 0.85f, Seed + 11, 9f);

                // LA FLOR DE FUEGO.
                PyraLib.Estallido(Main.spriteBatch, pos, RadioDetonacion * 0.5f, prog,
                    PyraPalettes.SolarFire, Seed, time, 1f);

                // LAS CADENAS FRACTALES a los seis más cercanos.
                int cadenas = 0;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (cadenas >= 6) break;
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > 500f * 500f) continue;
                    StormLib.MultiBolt(Main.spriteBatch, pos, npc.Center - Main.screenPosition,
                        Seed + cadenas * 7, StormLib.FlickTick(time, 15f), 4f,
                        new Color(255, 200, 120), new Color(160, 200, 255),
                        new Color(240, 248, 255), 0.85f * (1f - prog));
                    cadenas++;
                }

                // EL DESTELLO BLANCO del nacimiento.
                if (prog < 0.3f)
                    LumenLib.Bloom(Main.spriteBatch, pos, 90f * (1f - prog / 0.3f),
                        Color.White, 1f);

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            if (wasActive)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
