using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Dusts;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// TentaculoCosmicoProjectile — v6.41 — EL TENTÁCULO DEL VACÍO.
    ///
    /// RÉPLICA EXACTA (arma de pruebas — la petición literal del usuario)
    /// del proyectil del "tomo de la apatía nula": el cerebro se portó
    /// línea a línea del original (investigación v6.41 en
    /// research/apatia_v641/); las partículas originales (pulsos de vacío
    /// y chispas de cola) se replican con sus curvas EXACTAS (implosión
    /// PolyOut(4), opacidad en seno, decaimiento ×0.95/frame, muerte al
    /// cubo) sobre un búfer propio por instancia; y el polvo invertido
    /// del vacío tiene su calco propio en Content/Dusts.
    ///
    /// EL CICLO VITAL (los números del original, sin tocar):
    ///   1. LA GESTACIÓN (ticks 0..120): el tentáculo frena suavemente
    ///      (vel ×0.96) mientras ESCUPE pulsos de vacío (negro + verde)
    ///      que IMPLOEN — sin dañar a nadie (CanDamage = false).
    ///   2. EL AZOTE (3 curvas): en el tick 120 estalla en polvo y sale
    ///      disparado hacia la MIRA (vel 7 girada 0.2·sentido) — y cada
    ///      AI-tick gira su velocidad hacia el lado (la curva que barre
    ///      el cielo). 90 ticks de movimiento con chispas negras y
    ///      verdes en la cola (¡el hitbox es 90×90!).
    ///   3. EL SALTO: al acabar la curva se TELETRANSPORTA ±100px con un
    ///      estallido de blooms, PAUSA 45 ticks recargándose, re-apunta a
    ///      la mira con el sentido de giro INVERTIDO y azota de nuevo.
    ///   4. La tercera curva muere matando: curvas agotadas → polvo final.
    ///
    /// LA LEY DEL DAÑO (la del original): golpea TODO lo que toca su caja
    /// (penetración infinita, cooldown local de 64) y CADA golpe sucesivo
    /// pesa MENOS (×1.0 → ×0.7 tras 5 impactos — el tentáculo se aburre).
    ///
    /// NOTA MP: el estado de fase vive en campos de instancia SIN
    /// sincronizar (arma de pruebas para un jugador).
    /// NOTA DE RENDER: cero Main.rand en PreDraw (la variación de cada
    /// partícula se fija al nacer, como el original guarda su rotación).
    /// </summary>
    public class TentaculoCosmicoProjectile : ModProjectile
    {
        // La textura FANTASMA de los proyectiles 100%-código (el patrón de
        // AuraPortadorHalo/AtaqueJefeProjectile): tML exige el asset default
        // de la clase aunque el PreDraw (los quads del azote) jamás la dibuje
        // — sin este override, MissingResourceException y TODO el mod se
        // desactiva al cargar (la lección del client.log de la v6.50.3).
        public override string Texture => "AethonMod/Content/Projectiles/Cosmetic/AnillosSingularesHalo";

        // ==================================================================
        //  EL CEREBRO (los campos del original, 1:1)
        // ==================================================================

        private ref float Time => ref Projectile.ai[0];

        private bool _preDamage = true;
        private bool _moving = false;
        private float _scaling = 1f;
        private int _scalingTimer = 0;
        private int _curvaDir = 100;
        private int _curvas = 3;
        private const int ScalingTimerMax = 90;
        private float _damageMult = 1f;

        /// <summary>El color interior del tentáculo (verde de vacío).</summary>
        private static readonly Color InnerColor = Color.LightGreen;

        // ==================================================================
        //  LAS PARTÍCULAS (los pulsos y chispas del original, curvas EXACTAS)
        // ==================================================================

        /// <summary>
        /// Un pulso de vacío: bloom que IMPLOE (nace grande, muere en 0)
        /// con easing PolyOut(4) y opacidad en seno — los negros van en
        /// alpha blend (oscurecen), los de color en aditivo.
        /// </summary>
        private struct Pulso
        {
            public Vector2 Pos;
            public float Rot;
            public Color Color;
            public float Escala0;
            public int Nace;
            public int Vida;
            public bool Aditivo;
        }

        /// <summary>
        /// Una chispa de cola: blob estirado perpendicular al vuelo que
        /// decae ×0.95/frame y muere al cubo (vida fija de 12 frames).
        /// </summary>
        private struct Chispa
        {
            public Vector2 Pos;
            public float Rot;
            public Color Color;
            public float Escala0;
            public Vector2 Estira;
            public int Nace;
            public bool Aditivo;
        }

        private const int MaxPulsos = 96;
        private const int MaxChispas = 256;
        private readonly Pulso[] _pulsos = new Pulso[MaxPulsos];
        private readonly Chispa[] _chispas = new Chispa[MaxChispas];
        private int _nPulsos, _nChispas;

        private void NacerPulso(Vector2 pos, Color color, float escala, int vida, bool aditivo)
        {
            if (_nPulsos >= MaxPulsos) return;
            _pulsos[_nPulsos++] = new Pulso
            {
                Pos = pos,
                Rot = Main.rand.NextFloat(-10f, 10f),
                Color = color,
                Escala0 = escala,
                Nace = (int)Main.GameUpdateCount,
                Vida = vida,
                Aditivo = aditivo,
            };
        }

        private void NacerChispa(Vector2 pos, Vector2 vel, Color color, float escala, Vector2 estira, bool aditivo)
        {
            if (_nChispas >= MaxChispas) return;
            _chispas[_nChispas++] = new Chispa
            {
                Pos = pos,
                Rot = vel.ToRotation() + MathHelper.PiOver2,
                Color = color,
                Escala0 = escala,
                Estira = estira,
                Nace = (int)Main.GameUpdateCount,
                Aditivo = aditivo,
            };
        }

        // ==================================================================
        //  LOS DEFAULTS (los del original, 1:1)
        // ==================================================================

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 90;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 8;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8 * Projectile.extraUpdates;
            // El tentáculo se mata a sí mismo cuando agota sus curvas; el
            // timeLeft es solo un techo de seguridad (más largo que el ciclo).
            Projectile.timeLeft = 3600;
        }

        // ==================================================================
        //  LA IA (el puerto línea a línea del original)
        // ==================================================================

        public override void AI()
        {
            Player duenio = Main.player[Projectile.owner];
            float distDuenio = Vector2.Distance(duenio.Center, Projectile.Center);

            // El rumbo: hacia la MIRA del dueño, girado por el sentido de la
            // curva (el original re-apunta a cada tick al mundo del ratón).
            Vector2 rumbo = ((Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX) * 7f)
                .RotatedBy(0.2f * _curvaDir);

            if (_curvaDir == 100)
                _curvaDir = Main.rand.NextBool() ? 1 : -1;

            if (_preDamage)
            {
                // === LA GESTACIÓN: frenar escupiendo pulsos de vacío. ===
                if (distDuenio < 1400f && Time > 3)
                {
                    float scaler = Utils.GetLerpValue(-60, 120, Time, true);
                    NacerPulso(Projectile.Center, Color.Black, 0.6f * scaler, 4, false);
                    NacerPulso(Projectile.Center, InnerColor * 0.6f, 0.48f * scaler, 4, true);
                }
                Projectile.velocity *= 0.96f;
                if (Time == 3)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        float varianza = Main.rand.NextFloat(-0.4f, 0.4f);
                        Dust d = Dust.NewDustPerfect(Projectile.Center,
                            ModContent.DustType<PolvoVacioInvertido>());
                        d.scale = Main.rand.NextFloat(0.8f, 1.2f) - Math.Abs(varianza);
                        d.velocity = Projectile.velocity.RotatedBy(varianza) *
                            Main.rand.NextFloat(1.2f, 1.5f) * (1 - Math.Abs(varianza));
                        d.noGravity = true;
                        d.color = Color.LightGreen;
                    }
                }
            }
            else
            {
                if (_moving)
                {
                    // === EL AZOTE: la curva de persecución + chispas de cola. ===
                    float curva = Utils.GetLerpValue(ScalingTimerMax, 0, _scalingTimer, true) * 0.04f;
                    Projectile.velocity = Projectile.velocity.RotatedBy(curva * _curvaDir);
                    _scaling = Utils.GetLerpValue(0, ScalingTimerMax, _scalingTimer, true);
                    float sharp = Utils.GetLerpValue(ScalingTimerMax * 0.7f, ScalingTimerMax, _scalingTimer, true);

                    if (distDuenio < 1400f && Projectile.timeLeft % 2 == 0)
                    {
                        // Las chispas negras van en alpha blend (oscurecen la
                        // estela — el vacío); las verdes suman.
                        Vector2 cola = -Projectile.velocity * 0.05f;
                        Vector2 estira = new(1.7f - (1 - sharp), 0.9f + (1 - sharp) * 2);
                        NacerChispa(Projectile.Center, cola, Color.Black * 0.85f,
                            0.07f * _scaling, estira, false);
                        NacerChispa(Projectile.Center, cola, InnerColor * 0.75f,
                            0.07f * _scaling, estira, true);
                    }
                    if (Main.rand.NextBool(6))
                    {
                        Dust d = Dust.NewDustPerfect(Projectile.Center,
                            Main.rand.NextBool(6) ? 278 : 267, -Projectile.velocity);
                        d.scale = d.type == 278 ? Main.rand.NextFloat(0.3f, 0.6f) : Main.rand.NextFloat(0.6f, 1.2f);
                        d.velocity = -Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.1f, 0.7f);
                        d.noGravity = true;
                        d.color = InnerColor;
                    }

                    _scalingTimer--;
                    if (_scalingTimer <= 0)
                    {
                        if (_curvas > 1)
                        {
                            // === EL SALTO: teleport + estallido de blooms. ===
                            Projectile.Center += Main.rand.NextVector2Circular(100, 100);
                            NacerPulso(Projectile.Center, Color.Black, 0.6f, 4, false);
                            for (int i = 0; i < 2; i++)
                                NacerPulso(Projectile.Center, InnerColor, 0.36f, ScalingTimerMax / 2, true);
                            Projectile.velocity = Vector2.Zero;
                        }
                        else
                            Projectile.Kill();
                        _moving = false;
                    }
                }
                else
                {
                    // === LA RECARGA: 45 ticks recobrando el impulso. ===
                    _scalingTimer += 2;
                    if (_scalingTimer >= ScalingTimerMax)
                    {
                        _curvas--;
                        if (_curvas > 0)
                        {
                            for (int i = 0; i <= 6; i++)
                            {
                                Dust d = Dust.NewDustPerfect(Projectile.Center,
                                    Main.rand.NextBool(3) ? 191 : ModContent.DustType<PolvoVacioInvertido>(),
                                    Projectile.velocity);
                                d.scale = Main.rand.NextFloat(0.9f, 1.4f);
                                d.velocity = new Vector2(5, 5).RotatedByRandom(100) * Main.rand.NextFloat(0.2f, 1f);
                                d.noGravity = true;
                                d.color = d.type == ModContent.DustType<PolvoVacioInvertido>()
                                    ? InnerColor : default;
                            }
                            Projectile.velocity = rumbo;
                            _curvaDir = (_curvaDir == 1 ? -1 : 1);
                            _moving = true;
                            Projectile.numHits = 0;
                        }
                        else
                            Projectile.Kill();
                    }
                }
            }

            // === EL DESPERTAR: el tick 120 rompe la gestación. ===
            if (Time == 120)
            {
                for (int i = 0; i <= 12; i++)
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center,
                        Main.rand.NextBool() ? 66 : 263, Projectile.velocity);
                    d.scale = Main.rand.NextFloat(0.5f, 0.8f);
                    d.velocity = new Vector2(7, 7).RotatedByRandom(100) * Main.rand.NextFloat(0.2f, 1f);
                    d.noGravity = true;
                    d.color = Color.LightGreen;
                }
                _preDamage = false;
                _moving = true;
                _scalingTimer = ScalingTimerMax;
                Projectile.velocity = rumbo;
            }
            Time++;
        }

        // ==================================================================
        //  LA LEY DEL DAÑO (la del original)
        // ==================================================================

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.numHits == 0)
            {
                SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.3f, Pitch = 0.9f }, Projectile.Center);
                for (int i = 0; i < 6; i++)
                {
                    float varianza = Main.rand.NextFloat(-0.6f, 0.6f);
                    Dust d = Dust.NewDustPerfect(target.Center,
                        ModContent.DustType<PolvoVacioInvertido>());
                    d.scale = Main.rand.NextFloat(1.2f, 1.6f) - Math.Abs(varianza);
                    d.velocity = (Projectile.velocity * 1.5f).RotatedBy(varianza) *
                        Main.rand.NextFloat(1.2f, 1.5f) * (1 - Math.Abs(varianza));
                    d.noGravity = true;
                    d.color = Color.LightGreen;
                }
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // El tentáculo se aburre: cada impacto sucesivo pesa menos.
            _damageMult = MathHelper.Clamp(Utils.GetLerpValue(5, 1, Projectile.numHits), 0.7f, 1);
            modifiers.SourceDamage *= _damageMult;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i <= 8; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center,
                    Main.rand.NextBool() ? 66 : 263, Projectile.velocity);
                d.scale = Main.rand.NextFloat(0.5f, 0.8f);
                d.velocity = Projectile.velocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.3f, 3.1f);
                d.noGravity = true;
                d.color = InnerColor;
            }
        }

        public override bool? CanDamage() => _preDamage ? false : null;

        // ==================================================================
        //  EL RENDER (los pulsos y chispas con las curvas EXACTAS)
        // ==================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // EL PATRÓN A PRUEBA DE BALAS: el lote del pase se cierra, el
            // tentáculo abre y cierra SUS dos pases, y el lote del pase se
            // reabre TAL CUAL estaba.
            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                DibujarParticulas(BlendState.AlphaBlend);
                DibujarParticulas(BlendState.Additive);
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }

            // Limpieza de las partículas muertas (compactado in-place).
            Compactar();

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();

            // Nunca dibujar sprite (no lo hay: es TODO partículas).
            return false;
        }

        /// <summary>
        /// Un pase de partículas (alpha = el vacío que oscurece; aditivo =
        /// la materia que brilla). Las curvas son las del original: el
        /// pulso IMPLOE con PolyOut(4) y opacidad en seno; la chispa decae
        /// ×0.95/frame y muere al cubo.
        /// </summary>
        private void DibujarParticulas(BlendState blend)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, blend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D glow = VFXCore.SoftGlow;
            Vector2 inv = new(1f / glow.Width, 1f / glow.Height);
            Vector2 ojo = glow.Size() * 0.5f;
            Vector2 pantalla = Main.screenPosition;
            bool aditivo = blend == BlendState.Additive;
            uint ahora = Main.GameUpdateCount;

            for (int i = 0; i < _nPulsos; i++)
            {
                Pulso p = _pulsos[i];
                if (p.Aditivo != aditivo) continue;
                int edad = (int)(ahora - (uint)p.Nace);
                if (edad < 0 || edad >= p.Vida) continue;

                float t = edad / (float)p.Vida;
                // EL EASING POLYOUT(4) del original: implosión que frena.
                float prog = 1f - MathF.Pow(1f - t, 4f);
                float escala = MathHelper.Lerp(p.Escala0 * 220f, 0f, prog);
                // LA OPACIDAD EN SENO del original (nace viva, muere suave).
                float op = MathF.Sin(MathHelper.PiOver2 + t * MathHelper.PiOver2);
                if (op <= 0.01f) continue;

                Main.spriteBatch.Draw(glow, p.Pos - pantalla, null,
                    p.Color * op, p.Rot, ojo, escala * inv, SpriteEffects.None, 0f);
            }

            for (int i = 0; i < _nChispas; i++)
            {
                Chispa c = _chispas[i];
                if (c.Aditivo != aditivo) continue;
                int edad = (int)(ahora - (uint)c.Nace);
                if (edad < 0 || edad >= 12) continue;

                float escala = c.Escala0 * 2048f * MathF.Pow(0.95f, edad);
                float op = 1f - MathF.Pow(edad / 12f, 3f);
                if (op <= 0.01f) continue;

                Vector2 tam = new(escala * c.Estira.X, escala * c.Estira.Y);
                Main.spriteBatch.Draw(glow, c.Pos - pantalla, null,
                    c.Color * op, c.Rot, ojo, tam * inv, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.End();
        }

        /// <summary>Compacta los búfers (retira las partículas muertas).</summary>
        private void Compactar()
        {
            uint ahora = Main.GameUpdateCount;
            int n = 0;
            for (int i = 0; i < _nPulsos; i++)
                if ((int)(ahora - (uint)_pulsos[i].Nace) < _pulsos[i].Vida)
                    _pulsos[n++] = _pulsos[i];
            _nPulsos = n;
            n = 0;
            for (int i = 0; i < _nChispas; i++)
                if ((int)(ahora - (uint)_chispas[i].Nace) < 12)
                    _chispas[n++] = _chispas[i];
            _nChispas = n;
        }
    }
}
