using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// RealityTearProjectile — v6.31 — EL DESGARRO EN LA REALIDAD, CUARTA
    /// GENERACIÓN: LA LÍNEA CONTINUA Y PAREJA (research/v631,
    /// INFORME_TAJOS_CORTE_REALIDAD.md §D — la escuela de la línea única de punta a punta).
    ///
    /// v6.31 — las órdenes del usuario: la línea se veía DISCONTINUA y
    /// DESPAREJA, y el ESPEJO ROTO (el ramillete Lichtenberg de v6.30) fuera.
    /// LA RAÍZ (medida): el huso horneado en las RiftTaper* (100% SOLO al
    /// centro, 0.54 en u=0.10) + la respiración ±15% a 4 ciclos del ancho.
    /// EL FIX: texturas de MESETA (ancho 100% en el 80% central + tapas
    /// redondas — gen_rift_meseta_v631.py), el ancho NUNCA respira (la vida
    /// la pone la ALPHA), SIN ramas, SIN shards, SIN arcos — y LA FRACTURA ES
    /// UN EVENTO DE LUZ, NO DE GEOMETRÍA: la línea PERMANECE RECTA.
    ///
    /// LA LÍNEA DE TIEMPO (193 ticks — el guion del filo):
    ///   · TELEGRAFO (12 ticks): la estrella de ruptura crece, el anillo
    ///     implosiona, el mundo se apaga (0→0.22). CERO daño.
    ///   · APERTURA (3 ticks): EL PRIMER GOLPE — la línea nace YA a 620 px
    ///     (UN QUAD continuo): RiftLib.TearImpacto + daño ×1.0 a TODO lo que
    ///     toque la línea. ATRAVIESA PAREDES.
    ///   · RECTO (52 ticks): LA LÍNEA VIVA — pareja, labios continuos,
    ///     estrellas fluyendo dentro del vacío oclusivo, α respira ±8% a
    ///     2.2 Hz. DoT ×0.07 cada 3 ticks (EL DESGARRO EN SÍ).
    ///   · VIBRACIÓN (16 ticks): LA TENSIÓN — onda estacionaria creciendo
    ///     0→3.5 px a ~10 Hz (RiftLib.CaminoVibracion con la cadena GEMELA
    ///     del quad y ANCHO PLANO), shimmer rápido, un retumbo grave.
    ///   · FRACTURA (2 ticks): EL CLÍMAX ÓPTICO — flash 0.30 + ancho ×1.35 +
    ///     intensidad 1.25 + kick ×1.3: daño ×2.2 UNA VEZ (la herida se
    ///     PROFUNDIZA). La línea SIGUE RECTA — nada se rompe en pedazos.
    ///   · CORTE VIVO (98 ticks): la MISMA línea recta, MÁS INTENSA (la
    ///     herida abierta: α 0.90±10% a 4.4 Hz). DoT ×0.10 cada 3 ticks
    ///     (cesa 8 ticks antes del final visual).
    ///   · CIERRE (10 ticks): LA REALIDAD SANA COMIÉNDOSE EL CORTE DESDE LOS
    ///     EXTREMOS (la erosión direccional de la casa): la línea se ACORTA
    ///     hacia el centro SIN menguar el ancho — nunca un fundido plano.
    ///
    /// Determinismo MP: la dirección se toma de la velocity SINCRONIZADA al
    /// nacer; la semilla es Projectile.identity (la misma en todas las
    /// máquinas); el daño por v6.50 — GolpeMotor (el cauce del motor:
    /// crítica real, varianza, on-hit y sync MP del propio motor); el
    /// visual solo cliente (PreDraw).
    /// </summary>
    public class RealityTearProjectile : ModProjectile
    {
        // === LA LÍNEA DE TIEMPO (ticks) ===
        private const int TelegrafoTicks = 12;
        private const int AperturaTicks = 3;
        private const int RectoTicks = 52;
        private const int VibracionTicks = 16;
        private const int FracturaTicks = 2;
        private const int CorteVivoTicks = 98;
        private const int CierreTicks = 10;
        private const int TotalTicks = TelegrafoTicks + AperturaTicks + RectoTicks +
                                       VibracionTicks + FracturaTicks + CorteVivoTicks + CierreTicks;

        /// <summary>El largo del desgarro (≈ media pantalla: es un ARMA, no el corte de un jefe).</summary>
        private const float TearLength = 620f;

        /// <summary>Ancho MÁXIMO de la herida abierta (el rango 8..24 del contrato v2).</summary>
        private const float MaxWidth = 14f;

        /// <summary>El multiplicador de daño del CLÍMAX (la herida se profundiza).</summary>
        private const float DañoFractura = 2.2f;

        /// <summary>El multiplicador de ancho del pulso del clímax (CyberRift boost 1.30 + margen).</summary>
        private const float AnchoFractura = 1.35f;

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
        private bool _impactoHecho;   // el paquete de apertura
        private bool _golpeApertura;  // el daño ×1.0 de la apertura
        private bool _fracturaHecha;  // EL CLÍMAX (daño ×2.2 + paquete)
        private bool _suspiroHecho;   // el sonido final del cierre

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
                if (_age <= TelegrafoTicks + AperturaTicks + RectoTicks) return RiftFase.Sostenido;
                if (_age <= TelegrafoTicks + AperturaTicks + RectoTicks + VibracionTicks) return RiftFase.Vibracion;
                if (_age <= TelegrafoTicks + AperturaTicks + RectoTicks + VibracionTicks +
                             FracturaTicks + CorteVivoTicks)
                    return RiftFase.Fractura;   // el CORTE VIVO (el clímax + la herida abierta)
                return RiftFase.Cierre;
            }
        }

        /// <summary>La edad dentro de la fase actual.</summary>
        private float EdadFase(float ticksFaseAnteriores)
            => _age - ticksFaseAnteriores;

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
            RiftFase fase = Fase;

            switch (fase)
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
                    // El mundo se oscurece rampando (la luz "se cae" en la herida).
                    RiftLib.Oscurecer(0.08f + 0.14f * charge);
                    // El corazón del telégrafo late (luz).
                    Lighting.AddLight(_origin, 0.45f * charge, 0.2f * charge, 0.55f * charge);
                    break;
                }

                // ============================================================
                //  APERTURA — EL PRIMER GOLPE: el espacio TRUENA
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
                    }
                    RiftLib.Oscurecer(0.30f);
                    break;
                }

                // ============================================================
                //  RECTO — LA LÍNEA VIVA: el golpe de apertura + DoT suave
                // ============================================================
                case RiftFase.Sostenido:
                {
                    float ageR = EdadFase(TelegrafoTicks + AperturaTicks);

                    // EL GOLPE DE APERTURA: el corte a PLENA anchura pega UNA vez.
                    if (!_golpeApertura)
                    {
                        _golpeApertura = true;
                        GolpearLinea(1.0f, 0f);
                    }

                    // EL DoT: cada 3 ticks, la línea muerde (×0.07 — el desgarro en sí).
                    if (ageR > 0f && ageR % 3f == 1f)
                        GolpearLinea(0.07f, 0.30f);

                    // LA LUZ a lo largo de la herida (muestreada).
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

                    RiftLib.Oscurecer(0.32f);
                    break;
                }

                // ============================================================
                //  VIBRACIÓN — LA TENSIÓN: la onda estacionaria crece
                // ============================================================
                case RiftFase.Vibracion:
                {
                    float ageV = EdadFase(TelegrafoTicks + AperturaTicks + RectoTicks);
                    float t = MathHelper.Clamp(ageV / VibracionTicks, 0f, 1f);

                    // EL RETUMBO de la tensión (una sola vez, grave y corto).
                    if (ageV <= 1f && Main.netMode != NetmodeID.Server)
                    {
                        try { Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9.WithPitchOffset(-0.8f), _origin); }
                        catch { }
                    }

                    // EL TEMBLOR de cámara creciendo (2→4 px, sutil pero VIVO).
                    if (ageV % 4f < 1f)
                        OndaLib.Kick(2f + 2f * t, 3, (VFXCore.Hash01(seed, (int)_age, 555) - 0.5f) * MathHelper.TwoPi);

                    // DoT: la línea tensa sigue mordiendo.
                    if (ageV > 0f && ageV % 3f == 1f)
                        GolpearLinea(0.07f, 0.30f);

                    RiftLib.Oscurecer(0.33f);
                    break;
                }

                // ============================================================
                //  FRACTURA / CORTE VIVO — EL CLÍMAX y la herida que queda
                // ============================================================
                case RiftFase.Fractura:
                {
                    float preFrac = TelegrafoTicks + AperturaTicks + RectoTicks + VibracionTicks;
                    bool enCorteVivo = _age > preFrac + FracturaTicks;   // LA HERIDA ABIERTA

                    // === EL CLÍMAX (1-2 ticks): la herida se PROFUNDIZA ===
                    if (!_fracturaHecha)
                    {
                        _fracturaHecha = true;

                        // EL GOLPE DEL CLÍMAX: daño ×2.2 por la MISMA línea (la
                        // herida se abre más honda — nada de ramas: TODO ES LA LÍNEA).
                        GolpearLinea(DañoFractura, 0.6f);

                        // EL PAQUETE DEL CLÍMAX: kick ×1.3, flash 0.30 — la línea
                        // PERMANECE RECTA (la fractura es un evento de LUZ).
                        RiftLib.TearImpacto(_origin, _dir, TearLength, Paleta, seed, 1.3f);
                    }

                    if (enCorteVivo)
                    {
                        float ageG = _age - (preFrac + FracturaTicks);

                        // EL DoT DEL CORTE VIVO: cada 3 ticks, ×0.10 (la herida
                        // abierta duele MÁS) — y CESA 8 ticks antes del final.
                        if (ageG > 0f && ageG % 3f == 1f && ageG <= CorteVivoTicks - 8f)
                            GolpearLinea(0.10f, 0.30f);

                        // LA LUZ de la herida (muestreada a lo largo de la línea).
                        if (_age % 3f == 0f)
                            LumenLib.LightAlong(_origin, _origin + _dir * TearLength,
                                new Color(255, 60, 130), 0.50f, 80f);

                        // EL VACÍO EXHALA de cuando en cuando.
                        if (Main.netMode != NetmodeID.Server && _age % 24f == 0f)
                        {
                            float f = VFXCore.Hash01(seed, (int)(_age / 24f), 313);
                            Vector2 p = _origin + _dir * (TearLength * f);
                            RiftLib.ChispasAnomalia(p, 4, Paleta, seed + (int)_age, out ParticleData[] motas);
                            if (motas != null)
                                for (int i = 0; i < motas.Length; i++)
                                    ParticleManager.Spawn(motas[i]);
                        }

                        // El mundo al máximo mientras la herida vive, sanando al final.
                        float vidaG = 1f - MathHelper.Clamp(ageG / CorteVivoTicks, 0f, 1f);
                        RiftLib.Oscurecer(0.35f * vidaG);
                    }
                    else
                        RiftLib.Oscurecer(0.35f);
                    break;
                }

                // ============================================================
                //  CIERRE — el daño YA cesó: la realidad sana la herida
                // ============================================================
                default:
                {
                    float ageC = EdadFase(TelegrafoTicks + AperturaTicks + RectoTicks +
                                          VibracionTicks + FracturaTicks + CorteVivoTicks);

                    // El mundo recupera la luz mientras la herida se cierra.
                    RiftLib.Oscurecer(0.30f * (1f - ageC / CierreTicks));

                    // EL SUSPIRO FINAL (una vez): la realidad sana.
                    if (ageC >= CierreTicks - 1f && !_suspiroHecho)
                    {
                        _suspiroHecho = true;
                        if (Main.netMode != NetmodeID.Server)
                        {
                            try { Terraria.Audio.SoundEngine.PlaySound(SoundID.Item12.WithPitchOffset(0.3f), _origin); }
                            catch { }
                            RiftLib.ChispasAnomalia(_origin, 8, Paleta, seed + 99, out ParticleData[] motas);
                            if (motas != null)
                                for (int i = 0; i < motas.Length; i++)
                                    ParticleManager.Spawn(motas[i]);
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// EL DAÑO DE LA LÍNEA: todo NPC cuyo hitbox toque la cápsula
        /// origin→punta recibe factor·damage. v6.50 — GolpeMotor (el
        /// cauce del motor: crítica real, varianza, on-hit y sync MP del
        /// propio motor); el visual es solo cliente.
        /// </summary>
        private void GolpearLinea(float factor, float knockback)
        {
            int dmg = Math.Max(1, (int)(Projectile.damage * factor));
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                if (!RiftLib.LineaToca(_origin, _dir, TearLength, MaxWidth + 8f, npc.Hitbox)) continue;

                Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, knockback, true);
                // La herida electrifica lo que toca (la anomalía del desgarro) — server/SP.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    try { npc.AddBuff(BuffID.Electrified, 90); } catch { }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // ============================================================
            //  CONTRATO DE BATCH v6.10 (a prueba de balas): en PreDraw el
            //  batch de tML está ABIERTO — cerrarlo antes del pase propio.
            // ============================================================
            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try { DrawDesgarro(); }
            catch { VFXCore.CerrarLoteSiAbierto(); }

            VFXCore.ReabrirLoteVanilla(); // v6.50.11 — curación (lote siempre abierto y vanilla)
            return false;
        }

        /// <summary>Restaura el SpriteBatch con los parámetros EXACTOS del pase
        /// de proyectiles de vanilla (Main.DrawProjectiles).</summary>
        private static void RestauraBatch()
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

            // ============================================================
            //  EL ESTADO DE LA LÍNEA (progress/intensidad/ancho/erosión):
            //  la curva Apertura() del contrato (0→0.08 en 3 ticks ·
            //  plena →0.85 · la Muerte es por EROSIÓN, no por anchura).
            // ============================================================
            float preRecto = TelegrafoTicks + AperturaTicks;
            float preVib = preRecto + RectoTicks;
            float preFrac = preVib + VibracionTicks;
            float preGrieta = preFrac + FracturaTicks;

            float progress;
            float intensity;
            float anchoMul = 1f;
            float erosion = 0f;      // el CIERRE se come el corte desde los extremos

            if (fase == RiftFase.Apertura)
            {
                float ageA = _age - TelegrafoTicks;
                progress = 0.08f * ageA / AperturaTicks;
                intensity = 1f;
            }
            else if (fase == RiftFase.Sostenido)
            {
                float ageR = _age - preRecto;
                progress = 0.08f + 0.77f * MathHelper.Clamp(ageR / RectoTicks, 0f, 1f);
                // La α respira ±8% a 2.2 Hz (el ancho JAMÁS).
                intensity = 0.92f + 0.08f * MathF.Sin(time * MathHelper.TwoPi * 2.2f + seed);
            }
            else if (fase == RiftFase.Vibracion)
            {
                // La vibración mantiene la línea PLENA — la TENSIÓN es lateral.
                progress = 0.85f;
                // EL SHIMMER rápido: la línea parpadea nerviosa (10 Hz).
                intensity = 0.95f + 0.15f * MathF.Sin(time * MathHelper.TwoPi * 10f + seed);
            }
            else if (fase == RiftFase.Fractura)
            {
                if (_age <= preFrac + FracturaTicks)
                {
                    // EL FRAME DEL CLÍMAX: la herida se PROFUNDIZA — flash de
                    // intensidad y UNA inhalación de ancho (×1.35, 2 ticks).
                    progress = 0.85f;
                    intensity = 1.25f;
                    anchoMul = AnchoFractura;
                }
                else
                {
                    // EL CORTE VIVO: la MISMA línea recta, más intensa (herida
                    // abierta) — α 0.90±10% a 4.4 Hz, el ancho SIEMPRE pleno.
                    progress = 0.85f;
                    intensity = 0.90f + 0.10f * MathF.Sin(time * MathHelper.TwoPi * 4.4f + seed);
                }
            }
            else
            {
                // EL CIERRE: LA REALIDAD SANA COMIÉNDOSE EL CORTE DESDE LOS
                // EXTREMOS — la línea se ACORTA hacia el centro, el ancho
                // NO mengua (la erosión direccional de la casa, nunca un fundido plano).
                float ageC = _age - preGrieta - CorteVivoTicks;
                float t = MathHelper.Clamp(ageC / CierreTicks, 0f, 1f);
                progress = 0.85f;
                intensity = 0.92f * (1f - t * 0.5f);
                erosion = t;
            }

            // ============================================================
            //  LA LÍNEA (todas las fases restantes): LA LÍNEA RASGADA de
            //  v6.39 (Tear/TearVacio ya dibujan POR EL CAMINO dentado —
            //  "si la realidad se desgarra no sería una fea línea recta") —
            //  excepto la VIBRACIÓN, que la dobla con la ONDA ESTACIONARIA
            //  SOBRE LOS MISMOS DIENTES (misma semilla, mismo jag: la
            //  herida vibra CON sus dientes, no los cambia).
            // ============================================================
            float ageV = MathF.Max(0f, _age - preVib);
            float amplitud = fase == RiftFase.Vibracion
                ? 3.5f * MathHelper.Clamp(ageV / VibracionTicks, 0f, 1f)
                : 0f;

            if (amplitud > 0.6f)
            {
                // === LA LÍNEA VIBRANDO (la cadena gemela, sin huecos) ===
                Vector2[] camino = RiftLib.CaminoVibracion(origin, dir, TearLength, amplitud, time,
                    2, seed, RiftLib.Rasgado(MaxWidth));
                DibujarCamino(camino, intensity, seed, time);
            }
            else
            {
                // === LA LÍNEA RASGADA (con la EROSIÓN del
                //     cierre acortándola desde los dos extremos) ===
                Vector2 subOrigin = origin + dir * (TearLength * erosion * 0.5f);
                float subLen = TearLength * (1f - erosion);

                // PASO 1 — EL VACÍO OCLUSIVO (lote NO-premultiplicado, PRIMERO).
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                RiftLib.TearVacio(Main.spriteBatch, subOrigin, dir, subLen, progress, MaxWidth, seed, time, anchoMul);
                Main.spriteBatch.End();

                // PASO 2 — LA LUZ (lote aditivo): los TRES quads + estrellas.
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                RiftLib.Tear(Main.spriteBatch, subOrigin, dir, subLen, progress, MaxWidth,
                    Paleta, intensity, seed, time, anchoMul);
            }

            // ============================================================
            //  LA ESTRELLA DEL PUNTO DE RUPTURA: plena al abrir, decae mientras
            //  el desgarro vive, REVIVE en el clímax y muere con el cierre.
            // ============================================================
            float starCharge;
            if (fase == RiftFase.Apertura) starCharge = 1f;
            else if (fase == RiftFase.Sostenido)
                starCharge = MathF.Max(0.28f, 1f - (_age - preRecto) / 40f);
            else if (fase == RiftFase.Vibracion)
                starCharge = 0.35f + 0.25f * (_age - preVib) / VibracionTicks;   // se aviva
            else if (fase == RiftFase.Fractura && _age <= preFrac + FracturaTicks)
                starCharge = 1f;                                                  // EL CLÍMAX
            else if (fase == RiftFase.Fractura)
                starCharge = MathF.Max(0.25f, 0.8f - (_age - preGrieta) / 50f);
            else
                starCharge = MathF.Max(0f, 0.25f * (1f - (_age - preGrieta - CorteVivoTicks) / CierreTicks));

            RiftLib.Star(Main.spriteBatch, origin, starCharge, Paleta,
                MathHelper.Clamp(intensity, 0f, 1f) * 0.9f, seed, time);

            Main.spriteBatch.End();
        }

        /// <summary>Dibuja la LÍNEA VIBRANTE con los dos lotes del contrato — la
        /// cadena GEMELA del quad (mismas texturas Taper recortadas, ANCHO
        /// PLANO: la onda estacionaria sin perder un ápice de parejo).</summary>
        private void DibujarCamino(Vector2[] camino, float intensity, int seed, float time)
        {
            // El progress de la cadena: vida ≈ 0.90 (la herida vibra PLENA —
            // v6.30 la dibujaba a 0.22 de vida: flaca y apagada, otro "corte").
            const float progressCadena = 0.12f;

            // PASO 1 — EL VACÍO OCLUSIVO (lote NO-premultiplicado).
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            RiftLib.GrietaVacio(Main.spriteBatch, camino, progressCadena, MaxWidth, seed, time, plano: true);
            Main.spriteBatch.End();

            // PASO 2 — LA LUZ (lote aditivo): la cadena SIN huecos + estrellas.
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            RiftLib.Grieta(Main.spriteBatch, camino, progressCadena, MaxWidth, Paleta,
                intensity, seed, time, plano: true);
        }
    }
}
