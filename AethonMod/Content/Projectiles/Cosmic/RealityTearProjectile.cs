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
    /// RealityTearProjectile — v6.30 — EL DESGARRO EN LA REALIDAD, TERCERA
    /// GENERACIÓN: LA LÍNEA QUE PARTE EL ESPEJO.
    ///
    /// v6.30 — las órdenes del usuario: (1) la segunda fase SIGUE sin ser
    /// continua (tenía cortes) — RiftLib v3 endurece la cadena: el VACÍO
    /// solapa IGUAL que la luz (len+w — cubre giros ≤~126°), LA PERLA vive
    /// en TODOS los vértices (raíz incluida) con diámetro max(w), el taper
    /// es SUAVE (exp 0.45 + suelo 25% — el vidrio real mantiene el 40-70%
    /// de su anchura) y NINGÚN segmento se omite jamás; (2) el proyectil
    /// que CAÍA (los shards) BORRADO — la realidad se parte COMO UN ESPEJO
    /// ROTO: el canal madre + RAMIFICACIONES COMO UN RAYO (la figura de
    /// Lichtenberg: ramas a 25°-55° alternando lados, ×0.6 de ancho, con
    /// sub-ramas) + los ARCOS TELARAÑA concéntricos del impacto — TODO ES
    /// LA GRIETA, nada se suelta; (3) el daño de la fractura pega TODO el
    /// patrón: ×2.2 el canal madre, ×1.6 las ramas/arcos.
    ///
    /// LA LÍNEA DE TIEMPO (el guion de física del vidrio):
    ///   · TELEGRAFO (12 ticks): la estrella de ruptura crece, el anillo
    ///     implosiona, el mundo se apaga (0→0.22). CERO daño.
    ///   · APERTURA (3 ticks): EL PRIMER GOLPE — la línea recta (620 px, UN
    ///     QUAD continuo) se abre de golpe: RiftLib.TearImpacto + daño ×1.0
    ///     a TODO lo que toque la línea. ATRAVIESA PAREDES.
    ///   · RECTO (52 ticks): la línea viva — labios continuos, estrellas
    ///     fluyendo dentro del vacío oclusivo, respiración nebulosa. DoT
    ///     ×0.07 cada 3 ticks (EL DESGARRO EN SÍ — nada de proyectiles).
    ///   · VIBRACIÓN (16 ticks): LA TENSIÓN — onda estacionaria creciendo
    ///     0→3.5 px a ~10 Hz (RiftLib.CaminoVibracion con la cadena v6.30),
    ///     shimmer rápido, un retumbo grave. La línea está a punto de FALLAR.
    ///   · FRACTURA (2 ticks): EL CLÍMAX — EL ESPEJO SE PARTE: el canal
    ///     Lichtenberg moderado + LAS RAMAS tipo rayo + los arcos telaraña
    ///     (CaminoEspejoRoto): daño ×2.2 canal / ×1.6 ramas + kick ×1.3 +
    ///     flash 0.30 + el mundo al MÁXIMO de oscuridad. SIN shards.
    ///   · ESPEJO VIVO (98 ticks): la herida RAMIFICADA persiste respirando
    ///     (±8%) con estrellas fijas y chispas. DoT ×0.10 canal / ×0.07
    ///     ramas cada 3 ticks. La vida NUNCA baja de ~0.52 (se LEE continua).
    ///   · CIERRE (10 ticks): el daño CESÓ 8 ticks antes del final visual;
    ///     TODAS las grietas se cierran juntas y la realidad sana.
    ///
    /// Determinismo MP: la dirección se toma de la velocity SINCRONIZADA al
    /// nacer; la semilla es Projectile.identity (la misma en todas las
    /// máquinas); el daño solo en `Main.netMode != MultiplayerClient` con
    /// `SimpleStrikeNPC`; el visual solo cliente (PreDraw).
    /// </summary>
    public class RealityTearProjectile : ModProjectile
    {
        // === LA LÍNEA DE TIEMPO (ticks) ===
        private const int TelegrafoTicks = 12;
        private const int AperturaTicks = 3;
        private const int RectoTicks = 52;
        private const int VibracionTicks = 16;
        private const int FracturaTicks = 2;
        private const int GrietaTicks = 98;
        private const int CierreTicks = 10;
        private const int TotalTicks = TelegrafoTicks + AperturaTicks + RectoTicks +
                                       VibracionTicks + FracturaTicks + GrietaTicks + CierreTicks;

        /// <summary>El largo del desgarro (≈ media pantalla: es un ARMA, no el corte de un jefe).</summary>
        private const float TearLength = 620f;

        /// <summary>Ancho MÁXIMO de la herida abierta (el rango 8..24 del contrato v2).</summary>
        private const float MaxWidth = 14f;

        /// <summary>El multiplicador de daño de LA FRACTURA (la petición v6.28: "aún más daño cuando se fractura").</summary>
        private const float DañoFractura = 2.2f;

        /// <summary>El multiplicador de daño de LAS RAMAS y arcos del espejo roto (v6.30).</summary>
        private const float DañoRamas = 1.6f;

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
        private Vector2[] _caminoMundo;     // el canal del espejo en coords de MUNDO (alias del Principal — luz/chispas)
        private RiftLib.RiftRamillete _espejoMundo;     // EL ESPEJO ROTO (mundo — el daño de TODAS las grietas)
        private RiftLib.RiftRamillete _espejoPantalla;  // EL ESPEJO ROTO (pantalla — el dibujo)

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            // Daño 100% manual por LÍNEA/CAMINO (la escuela A): sin contacto de vanilla.
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
                if (_age <= TelegrafoTicks + AperturaTicks + RectoTicks + VibracionTicks + FracturaTicks)
                    return RiftFase.Fractura;
                if (_age <= TelegrafoTicks + AperturaTicks + RectoTicks + VibracionTicks +
                             FracturaTicks + GrietaTicks)
                    return RiftFase.Fractura;   // la GRIETA VIVA (mismo estado, progreso distinto)
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
            float time = Main.GlobalTimeWrappedHourly;

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
                    // El mundo se oscurece rampando (T11 — la luz "se cae" en la herida).
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
                //  FRACTURA / GRIETA VIVA — EL CLÍMAX y la herida que queda
                // ============================================================
                case RiftFase.Fractura:
                {
                    float preFrac = TelegrafoTicks + AperturaTicks + RectoTicks + VibracionTicks;
                    bool enGrieta = _age > preFrac + FracturaTicks;   // la GRIETA VIVA

                    // === EL CLÍMAX (1-2 ticks): EL ESPEJO SE PARTE ===
                    if (!_fracturaHecha)
                    {
                        _fracturaHecha = true;

                        // LA REALIDAD SE PARTE COMO UN ESPEJO ROTO: el canal
                        // Lichtenberg + LAS RAMIFICACIONES tipo rayo + los arcos
                        // telaraña del impacto (v6.30 — la petición del usuario:
                        // ramificaciones como un rayo para simular el espejo).
                        _espejoMundo = RiftLib.CaminoEspejoRoto(_origin, _dir, seed, TearLength);
                        _caminoMundo = _espejoMundo.Principal;

                        // EL GOLPE DEL ESPEJO: daño ×2.2 por el CANAL MADRE y
                        // ×1.6 por RAMAS/ARCOS (la fractura pega TODO el patrón).
                        GolpearRamillete(DañoFractura, DañoRamas, 0.6f);

                        // EL PAQUETE DEL CLÍMAX: kick ×1.3, flash 0.30 — SIN
                        // shards que caen (v6.30: nada suelto; TODO ES LA GRIETA).
                        RiftLib.TearImpacto(_origin, _dir, TearLength, Paleta, seed, 1.3f);
                    }

                    if (enGrieta)
                    {
                        float ageG = _age - (preFrac + FracturaTicks);

                        // EL DoT DEL ESPEJO VIVO: cada 3 ticks, ×0.10 el canal
                        // y ×0.07 ramas/arcos (la herida rota duele en TODOS sus
                        // filos — tiene más bordes que una línea).
                        if (ageG > 0f && ageG % 3f == 1f)
                            GolpearRamillete(0.10f, 0.07f, 0.30f);

                        // LA LUZ de la herida (muestreada a lo largo del canal madre).
                        if (_age % 3f == 0f && _caminoMundo != null)
                            StormLib.AddLightAlong(_caminoMundo, 0.40f, 0.18f, 0.55f, 0.7f, 56f);

                        // EL VACÍO EXHALA de cuando en cuando.
                        if (Main.netMode != NetmodeID.Server && _age % 24f == 0f && _caminoMundo != null)
                        {
                            int idx = 1 + (int)(VFXCore.Hash01(seed, (int)(_age / 24f), 313) * (_caminoMundo.Length - 2));
                            RiftLib.ChispasAnomalia(_caminoMundo[Math.Clamp(idx, 1, _caminoMundo.Length - 2)], 4,
                                Paleta, seed + (int)_age, out ParticleData[] motas);
                            if (motas != null)
                                for (int i = 0; i < motas.Length; i++)
                                    ParticleManager.Spawn(motas[i]);
                        }

                        // El mundo al máximo mientras la herida vive, sanando al final.
                        float vidaG = 1f - MathHelper.Clamp(ageG / GrietaTicks, 0f, 1f);
                        RiftLib.Oscurecer(0.35f * vidaG);
                    }
                    else
                        RiftLib.Oscurecer(0.35f);
                    break;
                }

                // ============================================================
                //  CIERRE — el daño YA cesó: solo la herida muriendo
                // ============================================================
                default:
                {
                    float ageC = EdadFase(TelegrafoTicks + AperturaTicks + RectoTicks +
                                          VibracionTicks + FracturaTicks + GrietaTicks);

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
        /// EL DAÑO DE LA LÍNEA (escuela A): todo NPC cuyo hitbox toque la cápsula
        /// origin→punta recibe factor·damage. Determinismo MP: SOLO
        /// server/singleplayer (SimpleStrikeNPC); el visual es solo cliente.
        /// </summary>
        private void GolpearLinea(float factor, float knockback)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            int dmg = Math.Max(1, (int)(Projectile.damage * factor));
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                if (!RiftLib.LineaToca(_origin, _dir, TearLength, MaxWidth + 8f, npc.Hitbox)) continue;

                npc.SimpleStrikeNPC(dmg, npc.direction, false, knockback, DamageClass.Magic);
                // La herida electrifica lo que toca (la anomalía del desgarro).
                try { npc.AddBuff(BuffID.Electrified, 90); } catch { }
            }
        }

        /// <summary>
        /// EL DAÑO DEL ESPEJO ROTO (escuela A): el canal madre a
        /// <paramref name="factorPrincipal"/> y las ramas/arcos a
        /// <paramref name="factorRamas"/> — LA FRACTURA PEGA TODO EL PATRÓN.
        /// Determinismo MP: SOLO server/singleplayer (SimpleStrikeNPC).
        /// </summary>
        private void GolpearRamillete(float factorPrincipal, float factorRamas, float knockback)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || _espejoMundo == null) return;

            int dmgP = Math.Max(1, (int)(Projectile.damage * factorPrincipal));
            int dmgR = Math.Max(1, (int)(Projectile.damage * factorRamas));
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;

                // El canal madre (el golpe mayor).
                if (RiftLib.CaminoToca(_espejoMundo.Principal, MaxWidth, npc.Hitbox, 8f))
                {
                    npc.SimpleStrikeNPC(dmgP, npc.direction, false, knockback, DamageClass.Magic);
                    try { npc.AddBuff(BuffID.Electrified, 120); } catch { }
                    continue;
                }
                // Las ramas y los arcos (los filos secundarios del espejo).
                if (RiftLib.RamilleteToca(_espejoMundo, MaxWidth, npc.Hitbox, 8f))
                {
                    npc.SimpleStrikeNPC(dmgR, npc.direction, false, knockback, DamageClass.Magic);
                    try { npc.AddBuff(BuffID.Electrified, 120); } catch { }
                }
            }
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
            //  FASES CON LÍNEA/CAMINO: el progress del corte (la curva
            //  Apertura() del contrato v2 — apertura 0→0.08 en 3 ticks ·
            //  sostenido →0.85 · cierre →1).
            // ============================================================
            float preRecto = TelegrafoTicks + AperturaTicks;
            float preVib = preRecto + RectoTicks;
            float preFrac = preVib + VibracionTicks;
            float preGrieta = preFrac + FracturaTicks;

            float progress;
            float intensity;
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
                intensity = 0.92f + 0.08f * MathF.Sin(time * 5.1f + seed);
            }
            else if (fase == RiftFase.Vibracion)
            {
                // La vibración mantiene la línea PLENA (progress fijo al final
                // del sostenido) — la anchura no cambia: la TENSIÓN es lateral.
                progress = 0.85f;
                // EL SHIMMER rápido: la línea parpadea nerviosa (10 Hz).
                intensity = 0.95f + 0.15f * MathF.Sin(time * MathHelper.TwoPi * 10f + seed);
            }
            else if (fase == RiftFase.Fractura)
            {
                if (_age <= preFrac + FracturaTicks)
                {
                    // EL FRAME DEL ESPEJO PARTIÉNDOSE: la herida NACE al
                    // MÁXIMO (el flash de la rotura — vida plena) y la
                    // intensidad EXPLOTA.
                    progress = 0.05f;
                    intensity = 1.25f;
                }
                else
                {
                    // EL ESPEJO VIVO: progreso 0→0.55 de su propia vida
                    // (v6.30: la vida JAMÁS baja de 0.45^0.8≈0.52 — la
                    // grieta se LEA continua hasta que cierra de verdad).
                    float ageG = _age - preGrieta;
                    progress = 0.10f + 0.45f * MathHelper.Clamp(ageG / GrietaTicks, 0f, 1f);
                    intensity = 0.90f + 0.10f * MathF.Sin(time * 4.4f + seed);
                }
            }
            else
            {
                // EL CIERRE DEL ESPEJO: 0.55→1.0 — el patrón entero sana.
                float ageC = _age - preGrieta - GrietaTicks;
                progress = 0.55f + 0.45f * MathHelper.Clamp(ageC / CierreTicks, 0f, 1f);
                intensity = 0.92f * (1f - MathHelper.Clamp(ageC / CierreTicks, 0f, 1f) * 0.5f);
            }

            // ============================================================
            //  FASE 1 — LA LÍNEA RECTA (Telegrafo→Vibración): EL DESGARRO
            //  EN UN SOLO QUAD (v6.28 — CERO juntas, CERO interrupciones).
            //  La vibración dobla el quad con la ONDA ESTACIONARIA (CaminoVibracion
            //  dibujado con la cadena sin huecos cuando la amplitud se LEA).
            // ============================================================
            if (fase <= RiftFase.Vibracion)
            {
                float ageV = MathF.Max(0f, _age - preVib);
                float amplitud = fase == RiftFase.Vibracion
                    ? 3.5f * MathHelper.Clamp(ageV / VibracionTicks, 0f, 1f)
                    : 0f;

                if (amplitud > 0.6f)
                {
                    // === LA LÍNEA VIBRANDO (la cadena sin huecos) ===
                    Vector2[] camino = RiftLib.CaminoVibracion(origin, dir, TearLength, amplitud, time);
                    DibujarCamino(camino, progress, intensity, seed, time);
                }
                else
                {
                    // === LA LÍNEA RECTA PURA: UN SOLO QUAD ===
                    // PASO 1 — EL VACÍO OCLUSIVO (lote NO-premultiplicado, PRIMERO).
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    RiftLib.TearVacio(Main.spriteBatch, origin, dir, TearLength, progress, MaxWidth, seed, time);
                    Main.spriteBatch.End();

                    // PASO 2 — LA LUZ (lote aditivo): los TRES quads + estrellas.
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    RiftLib.Tear(Main.spriteBatch, origin, dir, TearLength, progress, MaxWidth,
                        Paleta, intensity, seed, time);
                }
            }
            // ============================================================
            //  FASE 2 — EL ESPEJO ROTO (Fractura/Grieta/Cierre): el canal
            //  madre + LAS RAMIFICACIONES tipo rayo + los arcos telaraña,
            //  TODO con la cadena continua v6.30 (sin cortes — el usuario).
            // ============================================================
            else
            {
                // El espejo se regenera en CLIENTE también (determinista: la
                // MISMA semilla → la MISMA herida en todas las máquinas).
                if (_espejoPantalla == null)
                    _espejoPantalla = RiftLib.CaminoEspejoRoto(origin, dir, seed, TearLength);
                DibujarRamillete(_espejoPantalla, progress, intensity, seed, time);
            }

            // ============================================================
            //  LA ESTRELLA DEL PUNTO DE RUPTURA: plena al abrir, decae mientras
            //  el desgarro vive, REVIVE en la fractura y muere con el cierre.
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
                starCharge = MathF.Max(0f, 0.25f * (1f - (_age - preGrieta - GrietaTicks) / CierreTicks));

            RiftLib.Star(Main.spriteBatch, origin, starCharge, Paleta,
                MathHelper.Clamp(intensity, 0f, 1f) * 0.9f, seed, time);

            Main.spriteBatch.End();
        }

        /// <summary>Dibuja una herida CAMINO (vibrante) con los dos lotes del contrato.</summary>
        private void DibujarCamino(Vector2[] camino, float progress, float intensity, int seed, float time)
        {
            // PASO 1 — EL VACÍO OCLUSIVO (lote NO-premultiplicado).
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            RiftLib.GrietaVacio(Main.spriteBatch, camino, progress, MaxWidth, seed, time);
            Main.spriteBatch.End();

            // PASO 2 — LA LUZ (lote aditivo): la cadena SIN huecos + estrellas.
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            RiftLib.Grieta(Main.spriteBatch, camino, progress, MaxWidth, Paleta,
                intensity, seed, time);
        }

        /// <summary>Dibuja EL ESPEJO ROTO completo (canal + ramas + arcos) con
        /// los dos lotes del contrato (v6.30).</summary>
        private void DibujarRamillete(RiftLib.RiftRamillete espejo, float progress,
            float intensity, int seed, float time)
        {
            // PASO 1 — EL VACÍO OCLUSIVO de TODAS las grietas (juntas).
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            RiftLib.RamilleteVacio(Main.spriteBatch, espejo, progress, MaxWidth, seed, time);
            Main.spriteBatch.End();

            // PASO 2 — LA LUZ de TODAS las grietas (juntas).
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            RiftLib.Ramillete(Main.spriteBatch, espejo, progress, MaxWidth, Paleta,
                intensity, seed, time);
        }
    }
}
