using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// VelaSolarProjectile — v6.42 — APUESTA 2: LA VELA SOLAR.
    ///
    /// LA VELA que se despliega al canalizar el arma: una membrana
    /// curvada de fotones que captura el viento de la luz y dispara
    /// MÁS RÁPIDO cuanto más caliente está:
    ///
    ///   · CALENTAMIENTO (mantener pulsado): el calor crece con la
    ///     curva exponencial de un capacitor (τ = 132 ticks ≈ 2,2 s —
    ///     progreso visible en el primer segundo, saturación suave);
    ///     la cadencia sube de 12/s a 30/s y el daño escala ×1→×1,6.
    ///   · SOLTAR: el calor decae con la ley de enfriamiento de Newton
    ///     (×0,988/tick) — soltar a tiempo conserva ~el 70%.
    ///   · FUSIÓN (calor = 1): 90 ticks de PLASMA azul-blanco (daño
    ///     ×2, velocidad ×1,4, cadencia 30/s fijas) — la recompensa
    ///     visible: la vela arde como una estrella.
    ///   · FUSIÓN DEL CAÑÓN (tras la fusión): 180 ticks de vela caída
    ///     humeante (no dispara) y calor residual 0,3 al reiniciar.
    ///
    /// La física de la vela real: la presión de radiación empuja
    /// proporcional al área expuesta — por eso la membrana SE DESPLIEGA
    /// (más calor = más área capturada = más cadencia).
    ///
    /// CONVENCIONES DE LA CASA: el calor vive en ai[0] (se sincroniza
    /// para que los demás clientes vean la membrana crecer), el estado
    /// en ai[1]; los disparos salen SOLO de la autoridad (el dueño).
    /// </summary>
    public class VelaSolarProjectile : ModProjectile
    {
        // === EL CALENTAMIENTO ===
        public const float Tau = 132f;              // ticks de la constante de tiempo
        public const float EnfriamientoNewton = 0.988f;
        public const int FusionTicks = 90;
        public const int CanionMuertoTicks = 180;
        public const int Varillas = 7;              // las costillas del arco de la vela

        // === LA PALETA ===
        private static readonly Color MembranaBase = new(255, 196, 110);
        private static readonly Color MembranaFusion = new(150, 210, 255);
        private static readonly Color NucleoFusion = new(235, 245, 255);

        private int _fusRestante;
        private int _canionRestante;
        private int _cooldownDisparo;

        /// <summary>Semilla determinista por identidad.</summary>
        private int Seed => Math.Max(1, Projectile.identity + 733);

        private float Calor
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = MathHelper.Clamp(value, 0f, 1f);
        }

        /// <summary>0 = calentando · 1 = fusión · 2 = cañón muerto.</summary>
        private float Estado
        {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.tileCollide = false;
            Projectile.friendly = false;      // la vela no golpea: disparan sus ráfagas
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90;
            Projectile.aiStyle = -1;
            Projectile.netImportant = false;
        }

        public override bool? CanDamage() => false;
        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead ||
                duenio.HeldItem == null ||
                duenio.HeldItem.type != ModContent.ItemType<Weapons.Cosmic.VelaSolar>())
            {
                // El arma se guardó: la vela se pliega con un suspiro de brasas.
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 15);
                return;
            }

            Projectile.timeLeft = 90;
            _cooldownDisparo--;

            // === LA POSICIÓN: a la espalda del hombro director, tendida al viento. ===
            int lado = duenio.direction > 0 ? 1 : -1;
            Vector2 ancla = duenio.Center + new Vector2(-52f * lado, -58f);
            Projectile.Center = Vector2.Lerp(Projectile.Center, ancla, 0.15f);
            Projectile.velocity = Vector2.Zero;

            bool canalizando = duenio.channel && duenio.itemAnimation > 0;

            if (Estado == 0f)
            {
                // === CALENTANDO (o conservando el calor al soltar). ===
                if (canalizando)
                    Calor += (1f / Tau) * (1f - Calor);       // la curva del capacitor
                else
                    Calor *= EnfriamientoNewton;               // la ley de Newton

                if (Calor >= 0.999f && _fusRestante == 0 && canalizando)
                {
                    Estado = 1f;
                    _fusRestante = FusionTicks;
                    if (Main.netMode != NetmodeID.Server)
                    {
                        SoundEngine.PlaySound(SoundID.Item68 with { Volume = 0.5f, Pitch = 0.55f },
                            Projectile.Center);
                        PulsoLib.EmpujarPantalla(new Color(170, 215, 255), 0.3f, 12);
                    }
                }
            }
            else if (Estado == 1f)
            {
                // === LA FUSIÓN: 90 ticks de plasma. ===
                _fusRestante--;
                Calor = 1f;
                if (_fusRestante <= 0)
                {
                    Estado = 2f;
                    _canionRestante = CanionMuertoTicks;
                    if (Main.netMode != NetmodeID.Server)
                        SoundEngine.PlaySound(SoundID.Item89 with { Volume = 0.4f, Pitch = -0.4f },
                            Projectile.Center);
                }
            }
            else
            {
                // === EL CAÑÓN MUERTO: 180 ticks humeantes. ===
                _canionRestante--;
                Calor *= 0.995f;
                if (_canionRestante <= 0)
                {
                    Estado = 0f;
                    Calor = 0.3f;   // el calor residual de la casa
                }
                if (_edadPar() % 6 == 0 && Main.netMode != NetmodeID.Server)
                {
                    Dust humo = Dust.NewDustPerfect(Projectile.Center + new Vector2(0f, -14f),
                        DustID.Smoke);
                    humo.velocity = new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -0.9f);
                    humo.scale = 1.1f;
                    humo.alpha = 130;
                }
            }

            // === LA LUZ (el color del estado lo cuenta todo). ===
            Color luz = Estado == 1f
                ? new Color(0.5f, 0.75f, 1f)
                : new Color(1f, 0.72f, 0.35f) * (0.4f + 0.8f * Calor);
            Lighting.AddLight(Projectile.Center, luz.ToVector3());

            // === LOS DISPAROS (solo la autoridad — el dueño de la vela). ===
            if (Main.myPlayer == Projectile.owner && _cooldownDisparo <= 0)
            {
                bool fusion = Estado == 1f;
                if (fusion || (Estado == 0f && canalizando && Calor > 0.05f))
                {
                    int intervalo = fusion ? 2 : Math.Max(2, 5 - (int)(Calor * 3f));
                    _cooldownDisparo = intervalo;

                    Vector2 mira = duenio.Center + Vector2.Normalize(
                        new Vector2(Main.mouseX + Main.screenPosition.X - duenio.Center.X,
                            Main.mouseY + Main.screenPosition.Y - duenio.Center.Y)) * 10f;
                    Vector2 dir = (mira - Projectile.Center).SafeNormalize(Vector2.UnitX);
                    Vector2 origen = Projectile.Center + dir * 30f;
                    float vel = fusion ? 15.4f : 11f;
                    int dmg = Math.Max(1, (int)(Projectile.damage * (fusion ? 2f : 1f + 0.6f * Calor)));

                    int idx = Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                        origen, dir * vel, ModContent.ProjectileType<RafagaSolarProjectile>(),
                        dmg, 2f, Projectile.owner,
                        fusion ? 1f : 0f, 0f, 0f);
                    if (idx >= 0 && fusion)
                        Main.projectile[idx].CritChance = Math.Max(Main.projectile[idx].CritChance, 25);

                    if (Main.netMode != NetmodeID.Server && _edadPar() % 10 == 0)
                        SoundEngine.PlaySound(SoundID.Item12 with
                        {
                            Volume = 0.15f,
                            Pitch = fusion ? 0.6f : -0.2f + 0.5f * Calor
                        }, Projectile.Center);
                }
            }
        }

        private int _edadContador;
        private int _edadPar() => _edadContador++;

        // === LA GEOMETRÍA DE LA VELA (v6.43): el arco se calcula UNA vez
        // por frame y ambos pases leen del MISMO array — cero GC, y el
        // volumétrico y la estructura no pueden divergir. ===
        private readonly Vector2[] _puntas = new Vector2[Varillas + 1];
        private Vector2 _baseP;
        private Vector2 _bordeP;
        private Color _rampa;
        private float _semiA;
        private float _semiB;
        private float _inclinacion;
        private float _caida;
        private int _lado;
        private float _time;

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            CalcularVela();

            // === EL PASE DE MEDIA RESOLUCIÓN (v6.43 — MediaResLib): el
            // VOLUMÉTRICO DIFUSO de la vela — el tejido de la membrana,
            // el borde llameante y las alas de fusión — corre a media
            // resolución y se compone ×2: fill-rate ÷4. Son manchas de
            // luz, no trazos definidos: con el sampler lineal el
            // resultado es indistinguible. ===
            MediaResLib.Empezar(pixelado: false);
            if (MediaResLib.EnPase)
            {
                try { DrawVolumetrico(Main.spriteBatch); }
                finally { MediaResLib.Terminar(); }
            }
            else
            {
                // Respaldo nativo (RT indisponible): el mismo dibujado sin pase.
                try
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    DrawVolumetrico(Main.spriteBatch);
                    Main.spriteBatch.End();
                }
                catch
                {
                    try { Main.spriteBatch.End(); } catch { }
                }
            }

            // === LA ESTRUCTURA FINA (resolución NATIVA): las costillas y el
            // corazón son trazos DEFINIDOS del arte — la media resolución
            // se los comería. ===
            try
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                DrawEstructura(Main.spriteBatch);
                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            if (wasActive)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                    null, Main.Transform);
            return false;
        }

        /// <summary>
        /// LA GEOMETRÍA — el arco de la vela resuelto una sola vez: 7
        /// varillas sobre un cuarto de elipse. El calor despliega el arco
        /// (más área = más presión de radiación capturada); el cañón
        /// muerto la deja CAÍDA (rotación vencida); la rampa del color
        /// pinta el tejido según el estado (fuego solar → plasma frío →
        /// gris vencido).
        /// </summary>
        private void CalcularVela()
        {
            _baseP = Projectile.Center - Main.screenPosition;
            _lado = Main.player[Projectile.owner].direction > 0 ? 1 : -1;
            _time = Main.GlobalTimeWrappedHourly;

            // La caída del cañón muerto: la vela se rinde.
            _caida = Estado == 2f
                ? MathHelper.Lerp(0f, 0.9f, 1f - _canionRestante / (float)CanionMuertoTicks)
                : 0f;

            // El despliegue: 55% del arco frío → 100% al calor pleno.
            float despliegue = 0.55f + 0.45f * Calor;
            _semiA = 66f * despliegue;
            _semiB = 54f * despliegue;
            _inclinacion = -0.35f * _lado + _caida * 1.1f;

            _rampa = Estado == 1f
                ? PyraPalettes.Sample(PyraPalettes.ColdFire, 0.8f)
                : Estado == 2f
                    ? new Color(120, 110, 100) * 0.5f
                    : PyraPalettes.Sample(PyraPalettes.SolarFire, 0.25f + 0.6f * Calor);

            for (int i = 0; i <= Varillas; i++)
            {
                float t = i / (float)Varillas;           // 0..1 a lo largo del arco
                float ang = MathHelper.PiOver2 - t * MathHelper.PiOver2;   // vertical → horizontal
                // El arco de la vela (elipse en cuartos, espejada por lado).
                Vector2 local = new Vector2(-MathF.Cos(ang) * _semiA * _lado, -MathF.Sin(ang) * _semiB)
                    .RotatedBy(_inclinacion * (0.4f + 0.6f * t));
                _puntas[i] = _baseP + local + new Vector2(0f, MathF.Sin(_time * 1.8f + t * 3f) * 2.5f);
            }

            // El extremo libre del arco: donde vive el borde llameante.
            _bordeP = _baseP + new Vector2(-_semiA * _lado, -_semiB).RotatedBy(_inclinacion * 0.4f);
        }

        /// <summary>
        /// EL VOLUMÉTRICO (va al pase de MEDIA RESOLUCIÓN): el tejido
        /// entre varillas, el borde llameante (las lenguas del mástil) y
        /// las alas de fusión — las manchas de luz DIFUSAS, las que no
        /// necesitan resolución nativa. El batch que recibe es el del
        /// pase (matriz escalada ×0,5): las posiciones son las de siempre.
        /// </summary>
        private void DrawVolumetrico(SpriteBatch batch)
        {
            Texture2D glow = VFXCore.SoftGlow;

            // EL TEJIDO: el quad estirado entre varillas.
            for (int i = 1; i <= Varillas; i++)
            {
                Vector2 delta = _puntas[i] - _puntas[i - 1];
                float len = delta.Length();
                if (len > 0.5f)
                {
                    float opacidad = (0.16f + 0.3f * Calor) * (1f - _caida * 0.6f);
                    Color tejido = _rampa * opacidad;
                    batch.Draw(glow, _puntas[i - 1] + delta * 0.5f, null, tejido,
                        delta.ToRotation(), glow.Size() * 0.5f,
                        new Vector2(len * 1.05f, 10f + 8f * Calor) / glow.Size(),
                        SpriteEffects.None, 0f);
                }
            }

            // EL BORDE LLAMEANTE (solo con calor — las lenguas del mástil).
            if (Calor > 0.15f && Estado != 2f)
            {
                PyraLib.Tongue(batch, _bordeP, 26f + 30f * Calor, 12f + 10f * Calor,
                    Estado == 1f ? PyraPalettes.ColdFire : PyraPalettes.SolarFire,
                    Calor, Seed, _time, 0.9f * Calor);
            }

            // EL ALAS DE FUSIÓN: la estrella despierta — arco voltaico.
            if (Estado == 1f)
                StormLib.ArcRing(batch, _baseP, 40f, -MathHelper.Pi, 0f, Seed,
                    StormLib.FlickTick(_time, 15f), 3f, MembranaFusion * 0.7f, NucleoFusion, 0.8f, 6);
        }

        /// <summary>
        /// LA ESTRUCTURA (resolución NATIVA): las costillas luminosas del
        /// tejido y el corazón de la vela — los trazos DEFINIDOS del arte.
        /// </summary>
        private void DrawEstructura(SpriteBatch batch)
        {
            Texture2D glow = VFXCore.SoftGlow;

            for (int i = 0; i <= Varillas; i++)
            {
                float t = i / (float)Varillas;
                float ang = MathHelper.PiOver2 - t * MathHelper.PiOver2;

                // LA VARILLA: la costilla luminosa del tejido.
                batch.Draw(glow, _puntas[i], null, _rampa * (0.5f + 0.25f * Calor),
                    ang + _inclinacion, glow.Size() * 0.5f,
                    new Vector2(6f, 26f) / glow.Size(), SpriteEffects.None, 0f);
            }

            // EL CORAZÓN de la vela (el mástil).
            batch.Draw(glow, _baseP, null, Estado == 1f ? NucleoFusion * 0.8f : _rampa * 0.55f,
                0f, glow.Size() * 0.5f, new Vector2(14f, 20f) / glow.Size(),
                SpriteEffects.None, 0f);
        }
    }

    /// <summary>
    /// RafagaSolarProjectile — v6.42 — LA RÁFAGA DE LA VELA.
    ///
    /// La astilla de sol que la vela escupe: cometa de fuego solar
    /// (o de PLASMA azul-blanco si nació de la fusión — ai[0] lo
    /// recuerda) con su cola de fantasmas de EspectroLib y su pequeña
    /// flor de fuego al morir (PyraLib.Estallido — el idioma de la
    /// casa para las muertes ardientes).
    /// </summary>
    public class RafagaSolarProjectile : ModProjectile
    {
        private EspectroLib.Memoria _memoria;

        private int Seed => Math.Max(1, Projectile.identity + 977);

        public static bool EsFusion(Projectile p) => p.ai[0] > 0.5f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = true;
            Projectile.aiStyle = -1;
            _memoria = EspectroLib.Crear();
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            bool fusion = EsFusion(Projectile);
            Color luz = fusion ? new Color(0.6f, 0.8f, 1f) : new Color(1f, 0.6f, 0.25f);
            Lighting.AddLight(Projectile.Center, luz.ToVector3());
            EspectroLib.Registrar(ref _memoria, Projectile.Center, Projectile.rotation);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 5; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center,
                    EsFusion(Projectile) ? DustID.BlueCrystalShard : DustID.Torch);
                float ang = i / 5f * MathHelper.TwoPi;
                d.velocity = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 1.6f;
                d.scale = 0.9f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                bool fusion = EsFusion(Projectile);
                Color tinte = fusion ? new Color(150, 210, 255) : new Color(255, 170, 80);

                EspectroLib.ColaHistoria(ref _memoria, 2, 5, tinte,
                    new Vector2(24f, 10f), 0.5f, 1.4f);
                VFXCore.FlushAdditive(null, false);

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                Texture2D glow = VFXCore.SoftGlow;
                Vector2 pos = Projectile.Center - Main.screenPosition;
                Main.spriteBatch.Draw(glow, pos, null, tinte * 0.55f,
                    Projectile.rotation, glow.Size() * 0.5f,
                    new Vector2(34f, 12f) / glow.Size(), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(glow, pos, null,
                    (fusion ? NucleoFusionDib : Color.White) * 0.9f,
                    Projectile.rotation, glow.Size() * 0.5f,
                    new Vector2(16f, 6f) / glow.Size(), SpriteEffects.None, 0f);

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            if (wasActive)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                    null, Main.Transform);
            return false;
        }

        private static readonly Color NucleoFusionDib = new(235, 245, 255);
    }
}
