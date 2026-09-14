using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Effects.Bruma;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// RealityTearProjectile — v6.26 — EL PROYECTIL QUE ES UN DESGARRO EN LA REALIDAD.
    ///
    /// Petición del usuario: "un bastón que su proyectil sea un desgarro en la
    /// realidad... y dañar con eso". ESTE NO VIAJA: es la herida misma. Al
    /// dispararse, el espacio se ABRE en una línea de 620 px en la dirección
    /// del disparo (ATRAVIESA PAREDES — un desgarro del espacio no conoce la
    /// geometría) y todo el daño pasa por la LÍNEA (escuela A del contrato
    /// RiftLib: Collision.CheckAABBvLineCollision, el patrón élite SCCut).
    ///
    /// LA LÍNEA DE TIEMPO (el contrato de fases):
    ///   · TELEGRAFO (10 ticks): la estrella de ruptura crece y el anillo
    ///     implosiona (RiftLib.Star con charge 0→1). CERO daño.
    ///   · APERTURA (4 ticks, ~0.333/tick — élite): EL GOLPE — RiftLib.TearImpacto
    ///     (Kick perpendicular 7 px, Flash 0.22, sonido pitch −0.65, 12 chispas
    ///     de anomalía) y los labios saltan de 1 px a 14 px.
    ///   · SOSTENIDO (90 ticks): el GOLPE DE APERTURA (daño ×1.0 a toda la línea)
    ///     + DoT cada 2 ticks (×0.08 — ticks alternos) mientras RiftLib.Tear
    ///     pinta labios violeta/carmesí con aberración R/B, estrellas fluyendo
    ///     dentro del VACÍO OCLUSIVO (BlackDisk no-premultiplicado) y ecos glitch.
    ///   · CIERRE (8 ticks): el daño CESÓ (lección élite/élite: 8 ticks antes del
    ///     final visual), los labios se cierran y caen SHARDS de vidrio; al
    ///     terminar nace LA GRIETA PERSISTENTE (RealityTearZoneProjectile).
    ///
    /// Determinismo MP: la dirección se toma de la velocity SINCRONIZADA al
    /// nacer; la semilla es Projectile.identity (la misma en todas las
    /// máquinas); el daño solo en `Main.netMode != MultiplayerClient` con
    /// `SimpleStrikeNPC`; el visual solo cliente (PreDraw).
    /// </summary>
    public class RealityTearProjectile : ModProjectile
    {
        // === LA LÍNEA DE TIEMPO (ticks) ===
        private const int TelegrafoTicks = 10;
        private const int AperturaTicks = 4;
        private const int SostenidoTicks = 90;
        private const int CierreTicks = 8;
        private const int TotalTicks = TelegrafoTicks + AperturaTicks + SostenidoTicks + CierreTicks;

        /// <summary>El largo del desgarro (≈ media pantalla: es un ARMA, no el corte de un jefe).</summary>
        private const float TearLength = 620f;

        /// <summary>Ancho MÁXIMO de la herida abierta (el rango 8..16 del contrato).</summary>
        private const float MaxWidth = 14f;

        // === LA PALETA DEL ARMA: labios violeta/carmesí (realidad herida) ===
        private static readonly Color[] Paleta =
        {
            new(150, 80, 255),   // velo VIOLETA
            new(255, 60, 130),   // cuerpo CARMESÍ
            new(255, 170, 220),  // acento rosado
            new(235, 245, 255),  // núcleo blanco-cian (y las estrellas del vacío)
        };

        private float _age;
        private Vector2 _origin;      // dónde se rasgó el espacio
        private Vector2 _dir;         // hacia dónde (unitaria)
        private int _ecoTicks;        // ticks restantes de ECO GLITCH (visual)
        private bool _impactoHecho;   // el paquete de apertura
        private bool _golpeHecho;     // el golpe de apertura (daño ×1)
        private bool _shardsHechos;   // los vidrios del cierre
        private bool _zonaNacida;     // la grieta persistente

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            // Daño 100% manual por LÍNEA (la escuela A): sin contacto de vanilla.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalTicks + 2;
            Projectile.ignoreWater = true;
            // EL DESGARRO ATRAVIESA PAREDES: es una herida del espacio, no un objeto.
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        /// <summary>Semilla determinista (identidad de red: la misma en todas las máquinas).</summary>
        private int Seed => Math.Max(1, Projectile.identity + 13);

        /// <summary>La fase vital actual del desgarro.</summary>
        private RiftFase Fase
        {
            get
            {
                if (_age <= TelegrafoTicks) return RiftFase.Telegrafo;
                if (_age <= TelegrafoTicks + AperturaTicks) return RiftFase.Apertura;
                if (_age <= TelegrafoTicks + AperturaTicks + SostenidoTicks) return RiftFase.Sostenido;
                return RiftFase.Cierre;
            }
        }

        public override void AI()
        {
            _age += 1f;

            // === EL NACIMIENTO: clavar la herida en el espacio ===
            if (_age <= 1.5f)
            {
                _origin = Projectile.Center;
                _dir = Projectile.velocity.LengthSquared() > 0.0001f
                    ? Vector2.Normalize(Projectile.velocity)
                    : new Vector2(1f, 0f);
            }
            // La herida NO viaja: vive donde la rasgaste.
            Projectile.velocity = Vector2.Zero;
            Projectile.position = _origin - Projectile.Size * 0.5f;

            int seed = Seed;

            switch (Fase)
            {
                // ============================================================
                //  TELÉGRAFO — la estrella crece, el mundo contiene el aliento
                // ============================================================
                case RiftFase.Telegrafo:
                {
                    float charge = _age / TelegrafoTicks;
                    // 2-3 chispitas por par de ticks (client): la tensión de la ruptura.
                    if (Main.netMode != NetmodeID.Server && _age % 2f == 0f)
                    {
                        RiftLib.ChispasAnomalia(_origin + _dir * 12f, 2, Paleta, seed + (int)_age, out ParticleData[] motas);
                        if (motas != null)
                            for (int i = 0; i < motas.Length; i++)
                                ParticleManager.Spawn(motas[i]);
                    }
                    // El mundo se oscurece rampando (T11 — la luz "se cae" en la herida).
                    RiftLib.Oscurecer(0.08f + 0.14f * charge);
                    // El corazón del telégrafo late (luz).
                    Lighting.AddLight(_origin, 0.45f * charge, 0.2f * charge, 0.55f * charge);
                    break;
                }

                // ============================================================
                //  APERTURA — EL GOLPE: el espacio TRUENA al lado de la línea
                // ============================================================
                case RiftFase.Apertura:
                {
                    if (!_impactoHecho)
                    {
                        _impactoHecho = true;
                        // EL PAQUETE DE IMPACTO (RiftLib): Kick PERPENDICULAR (la
                        // cámara se desplaza AL LADO — la realidad tronó), Flash,
                        // sonido grave y la ráfaga de chispas.
                        RiftLib.TearImpacto(_origin, _dir, TearLength, Paleta, seed);
                        _ecoTicks = 3;   // el glitch del golpe
                    }
                    RiftLib.Oscurecer(0.30f);
                    break;
                }

                // ============================================================
                //  SOSTENIDO — LA LÍNEA VIVA: golpe de apertura + DoT alterno
                // ============================================================
                case RiftFase.Sostenido:
                {
                    float ageS = _age - (TelegrafoTicks + AperturaTicks);

                    // EL GOLPE DE APERTURA: el corte a PLENA anchura pega fuerte UNA vez.
                    if (!_golpeHecho)
                    {
                        _golpeHecho = true;
                        GolpearLinea(1.0f, 0f);
                    }

                    // EL DoT DE TICKS ALTERNOS: cada 2 ticks, la línea muerde (×0.08).
                    // (Desde el tick 1 del sostenido — el golpe de apertura
                    // ya pega SU bite en el tick 0.)
                    if (ageS > 0f && ageS % 2f == 1f)
                        GolpearLinea(0.08f, 0.35f);

                    // LA LUZ a lo largo de la herida (muestreada — LumenLib).
                    if (_age % 3f == 0f)
                        LumenLib.LightAlong(_origin, _origin + _dir * TearLength,
                            new Color(150, 80, 255), 0.55f, 80f);

                    // EL VACÍO EXHALA: motas de anomalía de cuando en cuando.
                    if (Main.netMode != NetmodeID.Server && _age % 18f == 0f)
                    {
                        float f = VFXCore.Hash01(seed, (int)(_age / 18f), 171);
                        Vector2 p = _origin + _dir * (TearLength * f);
                        RiftLib.ChispasAnomalia(p, 5, Paleta, seed + (int)_age, out ParticleData[] motas);
                        if (motas != null)
                            for (int i = 0; i < motas.Length; i++)
                                ParticleManager.Spawn(motas[i]);
                    }

                    // EL ECO GLITCH ocasional (~12% de los ticks): la herida "salta".
                    if (_ecoTicks > 0) _ecoTicks--;
                    else if (VFXCore.Hash01(seed, (int)_age, 999) < 0.12f)
                        _ecoTicks = 2;

                    // El mundo al MÁXIMO de oscuridad del arma (0.35 — el tope del contrato).
                    RiftLib.Oscurecer(0.35f);
                    break;
                }

                // ============================================================
                //  CIERRE — el daño YA cesó: solo la herida muriendo
                // ============================================================
                default:
                {
                    float ageC = _age - (TelegrafoTicks + AperturaTicks + SostenidoTicks);

                    // LOS SHARDS DE VIDRIO al 30% del cierre (los números del contrato).
                    if (ageC >= 2f && !_shardsHechos)
                    {
                        _shardsHechos = true;
                        if (Main.netMode != NetmodeID.Server)
                        {
                            RiftLib.Shards(_origin, _dir, Paleta, seed, out ParticleData[] shards, TearLength);
                            if (shards != null)
                                for (int i = 0; i < shards.Length; i++)
                                    ParticleManager.Spawn(shards[i]);
                        }
                        _ecoTicks = 4;   // el eco glitch final ×1
                    }

                    // El mundo recupera la luz mientras la herida se cierra.
                    RiftLib.Oscurecer(0.35f * (1f - ageC / CierreTicks));

                    if (_ecoTicks > 0) _ecoTicks--;

                    // EL FINAL: la herida se convierte en LA GRIETA PERSISTENTE.
                    if (_age >= TotalTicks && !_zonaNacida)
                    {
                        _zonaNacida = true;
                        NacerGrieta();
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// EL DAÑO DE LA LÍNEA (escuela A): todo NPC cuyo hitbox toque la cápsula
        /// origin→punta recibe <paramref name="factor"/>·damage. Determinismo MP:
        /// SOLO server/singleplayer (SimpleStrikeNPC); el visual es solo cliente.
        /// </summary>
        private void GolpearLinea(float factor, float knockback)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            int dmg = Math.Max(1, (int)(Projectile.damage * factor));
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || !npc.CanBeChasedBy()) continue;
                if (!RiftLib.LineaToca(_origin, _dir, TearLength, MaxWidth + 8f, npc.Hitbox)) continue;

                npc.SimpleStrikeNPC(dmg, npc.direction, false, knockback, DamageClass.Magic);
                // La herida electrifica lo que toca (la anomalía del desgarro).
                try { npc.AddBuff(BuffID.Electrified, 90); } catch { }
            }
        }

        /// <summary>Al terminar el cierre, EL DESGARRO DEJA CICATRIZ: nace la grieta persistente.</summary>
        private void NacerGrieta()
        {
            // En multijugador el servidor es el que crea la grieta (se sincroniza
            // sola); el cliente deja que su copia muera por timeLeft.
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            // LA DIRECCIÓN de la cicatriz: la del corte, torcida por la anomalía
            // (determinista por semilla — misma grieta en todas las máquinas).
            float ang = MathF.Atan2(_dir.Y, _dir.X)
                        + (VFXCore.Hash01(Seed, 777, 13) - 0.5f) * 0.9f;

            // EL CAP DE LA CASA: máx 4 grietas simultáneas — la más vieja cierra.
            int tipo = ModContent.ProjectileType<RealityTearZoneProjectile>();
            int oldest = -1, count = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || p.type != tipo) continue;
                count++;
                if (oldest < 0 || p.timeLeft < Main.projectile[oldest].timeLeft) oldest = i;
            }
            if (count >= RealityTearZoneProjectile.CapGrietas && oldest >= 0)
                Main.projectile[oldest].Kill();

            Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                _origin, Vector2.Zero, tipo,
                Math.Max(1, Projectile.damage), 0f, Main.myPlayer,
                Seed, ang);

            if (Main.netMode != NetmodeID.Server)
            {
                try { Terraria.Audio.SoundEngine.PlaySound(SoundID.Item12.WithPitchOffset(-0.4f), _origin); }
                catch { }
            }
            Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            // ============================================================
            //  CONTRATO DE BATCH v6.10 (a prueba de balas): en PreDraw el
            //  batch de tML está ABIERTO — cerrarlo antes del pase propio.
            // ============================================================
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try { DrawDesgarro(); }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
                RestauraBatch();
            return false;
        }

        /// <summary>Restaura el SpriteBatch con los parámetros EXACTOS del pase
        /// de proyectiles de vanilla (Main.DrawProjectiles) — lo usan también
        /// las zonas de grieta del archivo.</summary>
        internal static void RestauraBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        /// <summary>EL DESENLACE VISUAL del desgarro (los DOS lotes del contrato RiftLib).</summary>
        private void DrawDesgarro()
        {
            // Guard a prueba de balas: sin dirección no hay herida (un frame
            // de carrera antes del primer AI no debe dibujar NaNs).
            if (_dir.LengthSquared() < 0.0001f) return;

            float time = Main.GlobalTimeWrappedHourly;
            int seed = Seed;
            Vector2 origin = _origin - Main.screenPosition;
            Vector2 dir = _dir;
            RiftFase fase = Fase;

            // ============================================================
            //  EL TELÉGRAFO: solo la estrella creciendo + el anillo que
            //  implosiona (charge 0→1 con ease-out — la tensión de la ruptura).
            // ============================================================
            if (fase == RiftFase.Telegrafo)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                float charge = 1f - MathF.Pow(1f - _age / TelegrafoTicks, 3f);
                RiftLib.Star(Main.spriteBatch, origin, charge, Paleta, 0.9f, seed, time);
                Main.spriteBatch.End();
                return;
            }

            // EL PROGRESS del corte (la curva Apertura() del contrato):
            // apertura 0→0.08 en 4 ticks · sostenido →0.85 · cierre →1.
            float progress;
            float intensity;
            float ageS = _age - (TelegrafoTicks + AperturaTicks);
            float ageC = _age - (TelegrafoTicks + AperturaTicks + SostenidoTicks);
            if (fase == RiftFase.Apertura)
            {
                progress = 0.08f * (ageS + AperturaTicks) / AperturaTicks;   // ageS es negativo aquí
                intensity = 1f;
            }
            else if (fase == RiftFase.Sostenido)
            {
                progress = 0.08f + 0.77f * MathHelper.Clamp(ageS / SostenidoTicks, 0f, 1f);
                intensity = 0.92f + 0.08f * MathF.Sin(time * 5.1f + seed);
            }
            else
            {
                progress = 0.85f + 0.15f * MathHelper.Clamp(ageC / CierreTicks, 0f, 1f);
                intensity = 0.92f * (1f - MathHelper.Clamp(ageC / CierreTicks, 0f, 1f) * 0.5f);
            }

            // ============================================================
            //  PASO 1 — EL VACÍO OCLUSIVO (lote NO-premultiplicado, PRIMERO):
            //  el interior del desgarro es un agujero NEGRO en la escena (la
            //  banda BlackDisk 0.62·w respira) — pinta ANTES que la luz.
            // ============================================================
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            RiftLib.TearVacio(Main.spriteBatch, origin, dir, TearLength, progress, MaxWidth, seed, time);
            Main.spriteBatch.End();

            // ============================================================
            //  PASO 2 — LA LUZ (lote aditivo): los labios ×3 capas con la
            //  aberración R/B, las estrellas fluyendo y la estrella de ruptura.
            // ============================================================
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            RiftLib.Tear(Main.spriteBatch, origin, dir, TearLength, progress, MaxWidth,
                Paleta, intensity, seed, time);

            // LA ESTRELLA DEL PUNTO DE RUPTURA: plena al abrir, decae mientras
            // el desgarro vive (la herida ya no necesita gritar) y muere con el cierre.
            float starCharge;
            if (fase == RiftFase.Apertura) starCharge = 1f;
            else if (fase == RiftFase.Sostenido)
                starCharge = MathF.Max(0.28f, 1f - ageS / 40f);
            else starCharge = MathF.Max(0f, 0.28f * (1f - ageC / CierreTicks));
            RiftLib.Star(Main.spriteBatch, origin, starCharge, Paleta,
                MathHelper.Clamp(intensity, 0f, 1f) * 0.9f, seed, time);

            // ============================================================
            //  EL ECO GLITCH (lección élite glur SIN render targets): re-dibujo
            //  de la geometría con offsets RGB de canal puro en los frames marcados.
            // ============================================================
            if (_ecoTicks > 0)
            {
                RiftLib.EcoGlitch(seed, time, out Vector2[] ecoOff, out Color[] ecoTint);
                for (int e = 0; e < ecoOff.Length; e++)
                    RiftLib.Tear(Main.spriteBatch, origin, dir, TearLength, progress, MaxWidth,
                        Paleta, intensity * 0.6f, seed, time, ecoOff[e], ecoTint[e]);
            }

            Main.spriteBatch.End();
        }
    }

    /// <summary>
    /// RealityTearZoneProjectile — v6.26 — LA GRIETA PERSISTENTE (la cicatriz del desgarro).
    ///
    /// Lo que queda cuando el Bastón del Desgarro cierra: una herida flotante
    /// de ~8 s (480 ticks) sobre un CAMINO FRACTAL Lichtenberg (24 puntos,
    /// curvatura acumulada, micro-fallas 1/9 — RiftLib.CaminoGrieta) que
    /// RESPIRA (±8%), NO scrollea (es una herida, no una boca tragando) y hace
    /// DoT EN ÁREA (escuela B: cada 15 ticks a quien toque la cápsula del
    /// camino, ×0.25 del daño). El interior sigue siendo VACÍO OCLUSIVO con
    /// estrellas fijas; alrededor chasquean ARCOS DE ANOMALÍA (StormLib) y
    /// exhalan motas de bruma (BrumaFX). Cap de la casa: 4 grietas a la vez.
    ///
    /// Determinismo MP: el camino nace de ai[0] (semilla) + ai[1] (ángulo),
    /// ambos SINCRONIZADOS al spawn — la MISMA grieta en todas las máquinas.
    /// </summary>
    public class RealityTearZoneProjectile : ModProjectile
    {
        /// <summary>La vida de la cicatriz (~8 s).</summary>
        private const int VidaTicks = 480;

        /// <summary>Ancho máximo de la grieta en la RAÍZ (taper hacia la punta).</summary>
        private const float MaxWidth = 13f;

        /// <summary>El cap de la casa: máximo de grietas simultáneas (lo aplica el desgarro al nacer la cicatriz).</summary>
        internal const int CapGrietas = 4;

        private Vector2[] _camino;
        private float _age;
        private int _ecoTicks;

        /// <summary>La textura es la del desgarro (la sombra de disco de la casa).</summary>
        public override string Texture => "AethonMod/Content/Projectiles/Cosmic/RealityTearProjectile";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = VidaTicks;
            Projectile.ignoreWater = true;
            // La cicatriz flota en el espacio rasgado: paredes no existen.
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            _age += 1f;

            // === EL NACIMIENTO: el camino Lichtenberg (determinista por ai[]) ===
            if (_camino == null)
            {
                int seed = (int)Projectile.ai[0];
                float ang = Projectile.ai[1];
                _camino = RiftLib.CaminoGrieta(Projectile.Center,
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)),
                    seed, 24, 25f, 50f, 6f);
            }
            Projectile.velocity = Vector2.Zero;

            float vida = 1f - MathHelper.Clamp(_age / VidaTicks, 0f, 1f);

            // === EL DoT EN ÁREA (escuela B): cada 15 ticks, la cápsula del camino ===
            if (_age % 15f == 1f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = Math.Max(1, (int)(Projectile.damage * 0.25f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !npc.CanBeChasedBy()) continue;
                    if (!TocaCamino(npc.Hitbox)) continue;
                    npc.SimpleStrikeNPC(dmg, npc.direction, false, 0.25f, DamageClass.Magic);
                }
            }

            // === LA LUZ de la herida (muestreada a lo largo del camino — StormLib) ===
            if (_age % 3f == 0f && _camino != null)
                StormLib.AddLightAlong(_camino, 0.45f * vida, 0.20f * vida, 0.70f * vida,
                    0.8f * vida, 56f);

            // === EL VACÍO EXHALA: motas de anomalía de cuando en cuando ===
            if (Main.netMode != NetmodeID.Server && _age % 24f == 0f && _camino != null)
            {
                int idx = 1 + (int)(VFXCore.Hash01((int)Projectile.ai[0], (int)(_age / 24f), 313) * (_camino.Length - 2));
                RiftLib.ChispasAnomalia(_camino[Math.Clamp(idx, 1, _camino.Length - 2)], 4,
                    RiftPaletas.Vacio, (int)Projectile.ai[0] + (int)_age, out ParticleData[] motas);
                if (motas != null)
                    for (int i = 0; i < motas.Length; i++)
                        ParticleManager.Spawn(motas[i]);
            }

            // === EL ECO GLITCH ocasional (la cicatriz "recuerda") ===
            if (_ecoTicks > 0) _ecoTicks--;
            else if (VFXCore.Hash01((int)Projectile.ai[0], (int)_age, 977) < 0.06f)
                _ecoTicks = 2;

            // === LA OSCURIDAD residual (la cicatriz también apaga el mundo) ===
            RiftLib.Oscurecer(0.18f * vida);

            // LA MUERTE: un suspiro y la realidad sana.
            if (_age >= VidaTicks)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    try { Terraria.Audio.SoundEngine.PlaySound(SoundID.Item12.WithPitchOffset(0.3f), Projectile.Center); }
                    catch { }
                }
                Projectile.Kill();
            }
        }

        /// <summary>¿El hitbox toca la CÁPSULA del camino? (taper local + 6 px de franja de gracia).</summary>
        private bool TocaCamino(Rectangle hitbox)
        {
            if (_camino == null || _camino.Length < 2) return false;
            float total = 0f;
            for (int i = 1; i < _camino.Length; i++)
                total += Vector2.Distance(_camino[i - 1], _camino[i]);

            float arc = 0f;
            for (int i = 0; i < _camino.Length - 1; i++)
            {
                Vector2 a = _camino[i];
                Vector2 b = _camino[i + 1];
                float len = Vector2.Distance(a, b);
                if (len < 0.35f) { arc += len; continue; }
                float tMid = (arc + len * 0.5f) / total;
                arc += len;

                float wseg = MaxWidth * MathF.Pow(1f - tMid, 0.9f) + 6f;
                Vector2 d = (b - a) / len;
                if (RiftLib.LineaToca(a, d, len, wseg, hitbox))
                    return true;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            if (_camino == null) return false;

            // ============================================================
            //  CONTRATO DE BATCH v6.10 (cerrar el de tML antes del propio).
            // ============================================================
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try { DrawGrieta(); }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
                RealityTearProjectile.RestauraBatch();
            return false;
        }

        /// <summary>EL DESENLACE VISUAL de la cicatriz (vacío → luz, los DOS lotes).</summary>
        private void DrawGrieta()
        {
            float time = Main.GlobalTimeWrappedHourly;
            int seed = (int)Projectile.ai[0];
            float progress = MathHelper.Clamp(_age / VidaTicks, 0f, 1f);
            float vida = 1f - progress;

            // LA FLOTACIÓN de la herida (solo visual: la cicatriz se balancea).
            Vector2 sway = new Vector2(0f, MathF.Sin(time * 0.9f + seed) * 2.5f);
            var camino = new Vector2[_camino.Length];
            for (int i = 0; i < _camino.Length; i++)
                camino[i] = _camino[i] - Main.screenPosition + sway;

            // ============================================================
            //  PASO 1 — EL VACÍO OCLUSIVO + EL ALIENTO DE BRUMA (no-premult).
            // ============================================================
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            RiftLib.GrietaVacio(Main.spriteBatch, camino, progress, MaxWidth, seed, time);

            // LA BRUMA que exhala la herida (sutil, violeta frío — la casa).
            int tick = (int)(time * 60f);
            if (tick % 45 < 12 && vida > 0.2f)
            {
                int slot = tick / 45;
                for (int b = 0; b < 2; b++)
                {
                    int idx = 1 + (int)(VFXCore.Hash01(seed, slot, 401 + b) * (camino.Length - 2));
                    Vector2 p = camino[Math.Clamp(idx, 1, camino.Length - 2)];
                    BrumaFX.Puff(p, 12f + 8f * VFXCore.Hash01(seed, slot, 411 + b),
                        new Color(90, 70, 160), seed + 200 + b * 37 + slot,
                        time, alpha: 0.09f * vida, quality: 0.4f);
                }
            }
            Main.spriteBatch.End();

            // ============================================================
            //  PASO 2 — LA LUZ (aditivo): labios que respiran + estrellas fijas
            //  + chispas de anomalía + los ARCOS que chasquean (StormLib).
            // ============================================================
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            RiftLib.Grieta(Main.spriteBatch, camino, progress, MaxWidth, RiftPaletas.Vacio,
                0.9f, seed, time, chispas: true);

            // LA ESTRELLA DE LA RAÍZ: el "ojo" pequeño y constante de la cicatriz.
            RiftLib.Star(Main.spriteBatch, camino[0], 0.35f * vida + 0.1f, RiftPaletas.Vacio,
                0.55f * vida, seed, time);

            // LOS ARCOS DE ANOMALÍA (StormLib): cada ~7 ticks un chasquido entre
            // dos puntos del camino — la grieta sigue "viva" bajo la costra.
            int aSlot = tick / 7;
            if (vida > 0.15f && StormLib.IsLit(seed + aSlot, StormLib.FlickTick(time, 12f), 0.45f))
            {
                int iA = 1 + (int)(VFXCore.Hash01(seed, aSlot, 501) * (camino.Length - 3));
                int iB = Math.Clamp(iA + 2 + (int)(VFXCore.Hash01(seed, aSlot, 503) * 4f), 2, camino.Length - 1);
                StormLib.ChainBolt(Main.spriteBatch, camino[iA], camino[iB],
                    seed + aSlot, StormLib.FlickTick(time, 12f), 2.6f,
                    new Color(120, 90, 220) * vida, new Color(235, 245, 255) * vida,
                    0.8f * vida, 5, 8f);
            }

            // EL ECO GLITCH ocasional: la cicatriz "recuerda" el corte.
            if (_ecoTicks > 0)
            {
                RiftLib.EcoGlitch(seed, time, out Vector2[] ecoOff, out Color[] ecoTint);
                for (int e = 0; e < ecoOff.Length; e++)
                    RiftLib.Grieta(Main.spriteBatch, camino, progress, MaxWidth, RiftPaletas.Vacio,
                        0.55f * vida, seed, time, chispas: false, ecoOff[e], ecoTint[e]);
            }

            Main.spriteBatch.End();
        }
    }
}
