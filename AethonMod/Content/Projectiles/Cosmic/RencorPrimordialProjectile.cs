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
    /// RencorPrimordialProjectile — v6.30 — EL RENCOR PRIMORDIAL.
    ///
    /// El ritual completo del exhumado — con los colores MEDIDOS
    /// de los sprites reales (research/v630): el haz = NÚCLEO BLANCO PURO +
    /// bordes ROSA-MAGENTA (204,77,112); el círculo = PLATA/BLANCO-gris; los
    /// brazos = SILUETAS NEGRAS con borde rojo oscuro (pase alfa — v6.29 los
    /// hizo de hueso blanco y no se parecían en nada):
    ///
    ///   FASE CARGA (180 ticks — los 3 s EXACTOS del ritual):
    ///     EL CÍRCULO DE TRANSMUTACIÓN — 10 runas doradas CW + 6 violetas
    ///     CCW encendiéndose UNA A UNA + LA ESTRELLA de 5 puntas (el
    ///     homenaje FMA) + la bruma espiralando HACIA dentro + las ascuas
    ///     orbitando + el Telegraph carmesí del haz que va a venir.
    ///
    ///   FASE HAZ (90 ticks — "The Angy Beam"):
    ///     RiftLib.Tear con paleta carmesí-ámbar: UN SOLO QUAD continuo de
    ///     880 px × 44 que perfora TODO. Daño manual por línea (la escuela
    ///     A): golpe de apertura ×1.0 + DoT ×0.30 cada 5 ticks.
    ///     AL TOCAR TILE: LOS BRAZOS ESPECTRALES (×0.66, 4 brazos de
    ///     cápsulas que brotan de la superficie) + LAS ASCUAS (×0.33, área
    ///     + PyraLib.Sparks de rampa SolarFire) + la fog violeta + LA LAVA
    ///     (PyraLib.Flame lamiendo el tile + luz ámbar).
    ///     Los enemigos que mueren bajo el haz SE DESINTEGRAN EN CENIZA.
    ///
    ///   FASE CIERRE (14 ticks): el haz se cierra, los brazos se retractan,
    ///     la estrella implota y el círculo exhala su última runa.
    /// </summary>
    public class RencorPrimordialProjectile : ModProjectile
    {
        // === LOS TIEMPOS DEL RITUAL (el ritual: 3 s de carga exactos) ===
        private const int CargaTicks = 180;
        private const int HazTicks = 90;
        private const int CierreTicks = 14;
        private const int TotalTicks = CargaTicks + HazTicks + CierreTicks;

        // === EL HAZ ===
        private const float HazLongitud = 880f;
        private const float HazAncho = 44f;

        // === LOS DAÑOS PORCENTUALES DEL RITUAL ===
        private const float DañoBrazo = 0.66f;    // 66.67% — los brazos espectrales
        private const float DañoAscuas = 0.33f;   // 33.33% — las ascuas

        // === LOS BRAZOS (4, nacen escalonados durante el haz) ===
        private const int Brazos = 4;
        private readonly bool[] _brazoNacido = new bool[Brazos];
        private readonly float[] _brazoEdad = new float[Brazos];
        private readonly bool[] _brazoGolpe1 = new bool[Brazos];
        private readonly bool[] _brazoGolpe2 = new bool[Brazos];

        private float _age;
        private Vector2 _origin;       // el centro del círculo
        private Vector2 _dir;          // la dirección del haz (unitaria)
        private bool _impactoHecho;    // el paquete del disparo
        private bool _golpeApertura;   // el ×1.0 del nacer del haz
        private bool _tileHallado;
        private Vector2 _puntoTile;    // dónde toca el haz la superficie

        // === LA PALETA DEL RENCOR (v6.30 — MEDIDA del sprite real: "The Angy
        //     Beam" = NÚCLEO BLANCO PURO + bordes ROSA-MAGENTA (204,77,112)) ===
        private static readonly Color[] PaletaRencor =
        {
            new(140, 20, 60), new(204, 77, 112), new(255, 180, 200), new(255, 255, 255),
        };

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            // Daño por LÍNEA/ÁREA — v6.50 — GolpeMotor (el cauce del motor:
            // crítica real, varianza, on-hit y sync MP del propio motor).
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalTicks + 2;
            Projectile.ignoreWater = true;
            // EL HAZ PERFORA TODO: paredes incluidas (un desgarro de luz, no un objeto).
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        /// <summary>Semilla determinista de red.</summary>
        private int Seed => Math.Max(1, Projectile.identity + 17);

        /// <summary>¿Estamos en la fase del haz?</summary>
        private bool EnHaz => _age > CargaTicks && _age <= CargaTicks + HazTicks;

        /// <summary>La edad DENTRO del haz.</summary>
        private float EdadHaz => _age - CargaTicks;

        public override void AI()
        {
            _age += 1f;

            // === EL NACIMIENTO: clavar el círculo en el espacio ===
            if (_age <= 1.5f)
            {
                _origin = Projectile.Center;
                _dir = Projectile.velocity.LengthSquared() > 0.0001f
                    ? Vector2.Normalize(Projectile.velocity)
                    : new Vector2(1f, 0f);
            }
            Projectile.velocity = Vector2.Zero;
            Projectile.position = _origin - Projectile.Size * 0.5f;

            int seed = Seed;
            float time = Main.GlobalTimeWrappedHourly;

            // ============================================================
            //  FASE CARGA — el círculo respira y el mundo contiene el aliento
            // ============================================================
            if (_age <= CargaTicks)
            {
                float charge = MathHelper.Clamp(_age / CargaTicks, 0f, 1f);

                // EL LATIDO del ritual (subiendo de tono — la carga del ritual).
                if (Main.netMode != NetmodeID.Server && _age % 30f == 0f)
                {
                    try
                    {
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item4.WithPitchOffset(-0.5f + 0.7f * charge).WithVolumeScale(0.6f),
                            _origin);
                    }
                    catch { }
                }

                // LAS ASCUAS orbitando el círculo (client — las motas visuales).
                if (Main.netMode != NetmodeID.Server && _age % 10f == 0f)
                {
                    PyraLib.Sparks(_origin + _dir * 20f, -_dir, 2,
                        PyraPalettes.SolarFire, seed + (int)_age, out ParticleData[] motas);
                    if (motas != null)
                        for (int i = 0; i < motas.Length; i++)
                            ParticleManager.Spawn(motas[i]);
                }

                // EL MUNDO SE OSCURECE rampando (el ritual drena la luz).
                RiftLib.Oscurecer(0.04f + 0.12f * charge);

                // La luz del círculo creciendo (carmesí).
                if (_age % 4f == 0f)
                    Lighting.AddLight(_origin, 0.65f * charge, 0.18f * charge, 0.22f * charge);
            }
            // ============================================================
            //  FASE HAZ — EL DESGARRO DE LUZ CONTINUO
            // ============================================================
            else if (EnHaz)
            {
                float hazAge = EdadHaz;

                // === EL DISPARO: TODO sucede YA ===
                if (!_impactoHecho)
                {
                    _impactoHecho = true;

                    // EL PAQUETE DE IMPACTO (kick perpendicular + flash + chispas).
                    RiftLib.TearImpacto(_origin, _dir, HazLongitud, PaletaRencor, seed, 1.0f);

                    // EL TRUENO DEL HAZ (el disparo del láser).
                    if (Main.netMode != NetmodeID.Server)
                    {
                        try
                        {
                            Terraria.Audio.SoundEngine.PlaySound(
                                SoundID.Item12.WithPitchOffset(-0.30f), _origin);
                            Terraria.Audio.SoundEngine.PlaySound(
                                SoundID.Item93.WithPitchOffset(0.30f).WithVolumeScale(0.7f), _origin);
                        }
                        catch { }
                    }

                    // EL TILE DONDE MUERE EL HAZ (el hogar de brazos y ascuas).
                    HallarTile();
                }

                // === EL GOLPE DE APERTURA: el haz a PLENA anchura pega UNA vez ===
                if (!_golpeApertura)
                {
                    _golpeApertura = true;
                    GolpearLinea(1.0f);
                }

                // === EL DoT DEL HAZ: cada 5 ticks la carne quema (×0.30) ===
                if (hazAge > 0f && hazAge % 5f == 1f)
                    GolpearLinea(0.30f);

                // === EL LOOP DEL LÁSER (el lazo del láser) ===
                if (Main.netMode != NetmodeID.Server && hazAge % 15f == 0f)
                {
                    try
                    {
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item9.WithPitchOffset(0.25f).WithVolumeScale(0.30f), _origin);
                    }
                    catch { }
                }

                // === LOS BRAZOS NACEN ESCALONADOS de la superficie ===
                for (int b = 0; b < Brazos; b++)
                {
                    // SOLO si el haz halló un tile (sin pared no hay brazos —
                    // la regla del ritual: nacen DE la superficie).
                    if (!_tileHallado) break;
                    if (!_brazoNacido[b] && hazAge >= 6f + b * 14f)
                    {
                        _brazoNacido[b] = true;
                        _brazoEdad[b] = 0f;
                        // EL CRUJIDO del brazo al brotar del tile.
                        if (Main.netMode != NetmodeID.Server)
                        {
                            try
                            {
                                Terraria.Audio.SoundEngine.PlaySound(
                                    SoundID.Item63.WithPitchOffset(-0.45f).WithVolumeScale(0.45f),
                                    _puntoTile);
                            }
                            catch { }
                        }
                    }
                    if (!_brazoNacido[b]) continue;
                    _brazoEdad[b] += 1f;

                    // LOS DOS GOLPES DEL BRAZO (al extenderse ×0.66).
                    GeomBrazo(b, out _, out _, out Vector2 mano);
                    if (!_brazoGolpe1[b] && _brazoEdad[b] >= 12f)
                    {
                        _brazoGolpe1[b] = true;
                        GolpearCirculo(mano, 30f, DañoBrazo);
                    }
                    if (!_brazoGolpe2[b] && _brazoEdad[b] >= 38f)
                    {
                        _brazoGolpe2[b] = true;
                        GolpearCirculo(mano, 30f, DañoBrazo);
                    }
                }

                // === LAS ASCUAS: cada 8 ticks, ×0.33 en área + sparks ===
                if (_tileHallado && hazAge % 8f == 3f)
                {
                    GolpearCirculo(_puntoTile, 62f, DañoAscuas);
                    if (Main.netMode != NetmodeID.Server)
                    {
                        PyraLib.Sparks(_puntoTile, -_dir, 7,
                            PyraPalettes.SolarFire, seed + (int)_age, out ParticleData[] ascuas);
                        if (ascuas != null)
                            for (int i = 0; i < ascuas.Length; i++)
                                ParticleManager.Spawn(ascuas[i]);
                    }
                }

                // === LA LUZ a lo largo del haz + LA LAVA ámbar del tile ===
                if (_age % 4f == 0f)
                {
                    LumenLib.LightAlong(_origin, _origin + _dir * HazLongitud,
                        PaletaRencor[1], 0.85f, 110f);
                    if (_tileHallado)
                        Lighting.AddLight(_puntoTile, 1.1f, 0.45f, 0.12f);
                }

                RiftLib.Oscurecer(0.20f);
            }
            // ============================================================
            //  FASE CIERRE — la estrella implota y el círculo exhala
            // ============================================================
            else
            {
                float cierreAge = _age - (CargaTicks + HazTicks);

                // EL SUSPIRO final del ritual.
                if (cierreAge <= 1f && Main.netMode != NetmodeID.Server)
                {
                    try
                    {
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item4.WithPitchOffset(-0.8f).WithVolumeScale(0.5f), _origin);
                    }
                    catch { }
                    // La última exhalación de bruma.
                    for (int i = 0; i < 10; i++)
                    {
                        float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                        Dust d = Dust.NewDustPerfect(_origin, DustID.Smoke,
                            new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * 1.6f,
                            120, new Color(90, 40, 60), 1.4f);
                        d.noGravity = true;
                    }
                }

                // Los brazos viven su retracción.
                for (int b = 0; b < Brazos; b++)
                    if (_brazoNacido[b]) _brazoEdad[b] += 1f;
            }
        }

        // ==================================================================
        //  LA GEOMETRÍA DE LOS BRAZOS (determinista — el daño y el dibujo
        //  ven EL MISMO brazo en servidor y cliente)
        // ==================================================================

        /// <summary>
        /// El brazo b: base (en el tile), codo y MANO (la punta que agarra).
        /// Nace de la superficie apuntando CONTRA el haz (sale del muro
        /// hacia la boca del láser) con abanico ±82° y va creciendo con
        /// smoothstep durante 16 ticks.
        /// </summary>
        private void GeomBrazo(int b, out Vector2 basePos, out Vector2 codo, out Vector2 mano)
        {
            float baseAngle = MathF.Atan2(-_dir.Y, -_dir.X) + (b - 1.5f) * 0.55f;
            float sway = MathF.Sin(_brazoEdad[b] * 0.13f + b * 2.1f) * 0.22f;
            float grow = Smooth(_brazoEdad[b] / 16f);
            float largo = 48f * grow;

            basePos = _puntoTile;
            Vector2 dirA = new((float)Math.Cos(baseAngle + sway * 0.5f),
                               (float)Math.Sin(baseAngle + sway * 0.5f));
            Vector2 dirB = new((float)Math.Cos(baseAngle + sway),
                               (float)Math.Sin(baseAngle + sway));
            codo = basePos + dirA * (largo * 0.45f);
            mano = codo + dirB * (largo * 0.55f);
        }

        private static float Smooth(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        /// <summary>EL TILE donde muere el haz: marcha de 16 px hasta la primera celda sólida.</summary>
        private void HallarTile()
        {
            _tileHallado = false;
            for (float d = 24f; d <= HazLongitud; d += 16f)
            {
                Vector2 p = _origin + _dir * d;
                int tx = (int)(p.X / 16f), ty = (int)(p.Y / 16f);
                if (tx < 8 || tx > Main.maxTilesX - 8 || ty < 8 || ty > Main.maxTilesY - 8)
                    return; // fuera del mundo: el haz se pierde en el infinito
                if (WorldGen.SolidTile(tx, ty))
                {
                    _tileHallado = true;
                    _puntoTile = p - _dir * 8f; // ligeramente fuera del sólido
                    return;
                }
            }
        }

        // ==================================================================
        //  EL DAÑO — v6.50 — GolpeMotor (el cauce del motor: crítica real,
        //  varianza, on-hit y sync MP del propio motor)
        // ==================================================================

        /// <summary>El haz golpea TODA la línea (con la muerte de ceniza).</summary>
        private void GolpearLinea(float mult)
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                if (!RiftLib.LineaToca(_origin, _dir, HazLongitud, HazAncho * 1.5f, npc.Hitbox))
                    continue;
                int dmg = (int)(Projectile.damage * mult);
                Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 3f, true);
                if (npc.life <= 0) MuerteCeniza(npc);
            }
        }

        /// <summary>Un golpe de área (brazos/ascuas) con la muerte de ceniza.</summary>
        private void GolpearCirculo(Vector2 centro, float radio, float mult)
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                if ((npc.Center - centro).Length() > radio + npc.width * 0.5f) continue;
                int dmg = (int)(Projectile.damage * mult);
                Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 2f, true);
                if (npc.life <= 0) MuerteCeniza(npc);
            }
        }

        /// <summary>
        /// LA MUERTE CON FIRMA: los enemigos del rencor NO caen — SE
        /// DESINTEGRAN EN CENIZA (la ráfaga cenicienta-carmesí del exhumado).
        /// </summary>
        private static void MuerteCeniza(NPC npc)
        {
            if (Main.netMode == NetmodeID.Server) return;
            Vector2 c = npc.Center;
            for (int i = 0; i < 16; i++)
            {
                float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float vel = Main.rand.NextFloat(0.8f, 3.2f);
                Dust d = Dust.NewDustPerfect(c,
                    i % 3 == 0 ? DustID.Crimson : DustID.Ash,
                    new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * vel
                        + new Vector2(0f, -1.2f),
                    140, new Color(150, 90, 100), 1.2f);
                d.noGravity = i % 4 == 0;
                d.fadeIn = 0f;
            }
            Lighting.AddLight(c, 0.7f, 0.25f, 0.3f);
        }

        // ==================================================================
        //  EL DIBUJO — el círculo, el haz, los brazos, la lava
        // ==================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            // CONTRATO DE BATCH A PRUEBA DE BALAS (v6.10).
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                DrawRencor();
            }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
                RestoreSpriteBatch();
            return false;
        }

        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        // --- LA PALETA DEL DIBUJO (v6.30 — MEDIDA de los sprites de referencia:
        //     el círculo es ESCALA DE GRISES blanca-plata (Rancor_Magic_Circle:
        //     40% (224) + 33% (192) + 12% (160)); los brazos son SILUETAS
        //     NEGRAS con borde rojo oscuro (Rancor_Arms: 31% negro + 22%
        //     (32,0,0) + 17% (64,32,32)) — NO hueso blanco) ---
        private static readonly Color PlataHalo = new(208, 214, 224);
        private static readonly Color PlataViva = new(242, 245, 250);
        private static readonly Color PlataTenue = new(168, 174, 186);
        private static readonly Color SombraBrazo = new(10, 4, 8);      // el CUERPO negro del brazo
        private static readonly Color RojoBrazo = new(96, 16, 24);      // el BORDE rojo oscuro
        private static readonly Color BrumaVioleta = new(96, 60, 110);

        private void DrawRencor()
        {
            try
            {
                float time = Main.GlobalTimeWrappedHourly;
                int seed = Seed;
                Vector2 center = _origin - Main.screenPosition;

                float charge = MathHelper.Clamp(_age / CargaTicks, 0f, 1f);
                bool enCarga = _age <= CargaTicks;
                bool enHaz = EnHaz;
                float hazAge = EdadHaz;
                // El desvanecido del cierre (los últimos 14 ticks).
                float fadeFinal = MathHelper.Clamp(
                    (TotalTicks - _age) / CierreTicks, 0f, 1f);
                float vida = enCarga ? 1f : fadeFinal;

                // ============ 1. EL PASE ALFA — la masa que ocluye ============
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                if (enCarga)
                {
                    // LA BRUMA REUNIÉNDOSE: la nube del ritual creciendo con la carga.
                    BrumaFX.Cloud(center, 34f + 54f * charge, BrumaVioleta, seed + 3, time,
                        puffs: 6, alpha: 0.26f * (0.4f + 0.6f * charge), worldLit: true);
                }
                else if (enHaz && _tileHallado)
                {
                    // LA FOG DEL IMPACTO (la niebla del rencor).
                    BrumaFX.Puff(_puntoTile - Main.screenPosition, 58f, BrumaVioleta,
                        seed + (int)(hazAge * 0.25f), time,
                        alpha: 0.22f * vida, quality: 0.6f, worldLit: true);
                    BrumaFX.Puff(_puntoTile - Main.screenPosition + new Vector2(26f, -14f), 40f,
                        BrumaVioleta, seed + 91 + (int)(hazAge * 0.25f), time,
                        alpha: 0.16f * vida, quality: 0.5f, worldLit: true);
                }

                // v6.30 — LOS BRAZOS ESPECTRALES EN EL PASE ALFA: son SILUETAS
                // NEGRAS (medido: 31% negro + rojo oscuro en el borde — el
                // NEGRO aditivo es INVISIBLE, la lección v6.29 del pase alfa).
                if (enHaz && _tileHallado)
                    DibujarBrazos(time, vida);

                // ============ 2..7: LO BRILLANTE (pase aditivo) ============
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                if (enCarga)
                {
                    // --- 2. EL TELEGRAPH DEL HAZ (la línea que anuncia el corte) ---
                    LumenLib.Telegraph(Main.spriteBatch, center, _dir, HazLongitud,
                        PaletaRencor[1], charge);

                    // --- 3. LA ESPIRAL DE BRUMA hacia el centro (se reúne) ---
                    DibujarEspiralBruma(center, charge, time, seed);
                }

                if (enHaz)
                {
                    // --- 4. EL HAZ CONTINUO (UN SOLO QUAD — la lección v6.28) ---
                    float progreso = MathHelper.Clamp(hazAge / HazTicks, 0f, 1f);
                    // OJO: RiftLib.Tear toma coords de PANTALLA (el contrato
                    // de RealityTear — el lote usa GameViewMatrix).
                    RiftLib.Tear(Main.spriteBatch, _origin - Main.screenPosition, _dir,
                        HazLongitud, progreso, HazAncho, PaletaRencor, 1f * vida, seed, time);

                    // --- 5. EL AURA ÍGNEA del haz (el ardor ígneo del haz —
                    //     v6.30: ROSA como el borde medido del haz).
                    LumenLib.Ray(Main.spriteBatch, center, _dir, HazLongitud, HazAncho * 1.6f,
                        new Color(232, 120, 160), 0.28f * vida, 0.5f + 0.5f * (float)Math.Sin(time * 9f));

                    // LA BOCA DEL HAZ cegadora (los primeros ticks).
                    float boca = MathHelper.Clamp(1f - hazAge / 10f, 0f, 1f);
                    if (boca > 0f)
                        StormLib.ImpactFlash(Main.spriteBatch, center,
                            58f * (0.6f + 0.4f * boca), PaletaRencor[1], boca * vida, time);

                    // --- 6. LA LAVA (PyraLib.Flame lamiendo la superficie) ---
                    if (_tileHallado)
                    {
                        PyraLib.Flame(Main.spriteBatch, _puntoTile - Main.screenPosition,
                            20f, PyraPalettes.SolarFire, seed + 5, time,
                            (0.65f + 0.35f * (float)Math.Sin(time * 7f)) * vida);

                        // v6.30 — EL RESPLANDOR DEL BROTE de los brazos (aditivo:
                        // la ÚNICA luz de las sombras — la herida del muro ARDE).
                        DibujarAurasBrote(time, vida);
                    }
                }

                if (_age > CargaTicks + HazTicks)
                {
                    // --- EL CIERRE: la última runa exhala + el anillo que se va ---
                    float cierreAge = _age - (CargaTicks + HazTicks);
                    Vector2 anillo = center;
                    Quad(VFXCore.Ring, anillo,
                        VFXCore.RingQuadSize(30f + 70f * (cierreAge / CierreTicks)),
                        cierreAge * 0.15f, Tint(PaletaRencor[1], 0.35f * fadeFinal));
                }

                // --- 7. EL CÍRCULO DE TRANSMUTACIÓN (vive TODO el ritual) ---
                if (vida > 0.02f)
                    DibujarCirculo(center, time, seed, charge, enCarga, vida);

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        /// <summary>LA ESPIRAL DE BRUMA que se reúne hacia el centro durante la carga.</summary>
        private static void DibujarEspiralBruma(Vector2 center, float charge, float time, int seed)
        {
            if (charge < 0.15f) return;
            // El camino: de FUERA hacia DENTRO (la bruma es aspirada).
            var camino = new Vector2[14];
            for (int k = 0; k < camino.Length; k++)
            {
                float t = k / (float)(camino.Length - 1);
                float ang = time * 1.8f + t * 4.2f;
                float radio = (92f - 74f * t) * (0.6f + 0.4f * charge);
                camino[k] = center + new Vector2(
                    (float)Math.Cos(ang) * radio, (float)Math.Sin(ang) * radio * 0.8f);
            }
            BrumaFX.Tendril(camino, 14f, BrumaVioleta, seed + 7, time,
                0.35f * charge, 0.8f);
        }

        /// <summary>EL CÍRCULO DE TRANSMUTACIÓN (el homenaje FMA de la casa).</summary>
        private void DibujarCirculo(Vector2 center, float time, int seed,
            float charge, bool enCarga, float vida)
        {
            Vector2[][] glifos = GlifosRencor;

            // === EL ANILLO EXTERIOR de plata (10 runas CW — el blanco-gris
            //     MEDIDO del Rancor_Magic_Circle: 224/192/160) ===
            float spin = enCarga ? time * 0.12f : time * 0.45f;
            int litOro = enCarga
                ? (int)Math.Ceiling(10f * charge) : 10;
            for (int g = 0; g < 10; g++)
                DibujarRuna(center, 96f, spin, g, 10, glifos[(g + 2) % glifos.Length],
                    PlataViva, 0.80f, vida, litOro > g ? 1f : 0.15f);

            // === EL ANILLO INTERIOR de plata tenue (6 runas CCW) ===
            int litVioleta = enCarga
                ? (int)Math.Ceiling(6f * charge) : 6;
            for (int g = 0; g < 6; g++)
                DibujarRuna(center, 64f, -time * 0.10f, g, 6, glifos[g % glifos.Length],
                    PlataTenue, 0.72f, vida, litVioleta > g ? 1f : 0.15f);

            // === LOS DOS ANILLOS (el cuerpo del círculo — plata) ===
            Quad(VFXCore.Ring, center, VFXCore.RingQuadSize(190f), time * 0.05f,
                Tint(PlataHalo, 0.30f * vida));
            Quad(VFXCore.Ring, center, VFXCore.RingQuadSize(128f), -time * 0.04f,
                Tint(PlataTenue, 0.26f * vida));

            // === LA ESTRELLA DE 5 PUNTAS (el sello de la transmutación) ===
            if (charge > 0.35f || !enCarga)
            {
                float starAlpha = enCarga
                    ? MathHelper.Clamp((charge - 0.35f) / 0.4f, 0f, 1f)
                    : vida;
                float rot = time * 0.30f;
                float R = 56f;
                var puntos = new Vector2[5];
                for (int p = 0; p < 5; p++)
                {
                    float a = rot + p / 5f * MathHelper.TwoPi - MathHelper.PiOver2;
                    puntos[p] = center + new Vector2((float)Math.Cos(a) * R, (float)Math.Sin(a) * R);
                }
                // EL PENTAGRAMA: 1→3→5→2→4→1 (el trazo continuo de cinco saltos).
                int[] orden = { 0, 2, 4, 1, 3, 0 };
                for (int s = 0; s < orden.Length - 1; s++)
                {
                    Vector2 a = puntos[orden[s]], b = puntos[orden[s + 1]];
                    Vector2 mid = (a + b) * 0.5f;
                    Vector2 d = b - a;
                    float len = d.Length();
                    if (len < 0.01f) continue;
                    Capsule(mid, len, 3.4f, (float)Math.Atan2(d.Y, d.X),
                        Tint(PlataViva, 0.55f * starAlpha * vida));
                    Capsule(mid, len, 7f, (float)Math.Atan2(d.Y, d.X),
                        Tint(PaletaRencor[1], 0.20f * starAlpha * vida));
                }
            }

            // === EL CORAZÓN DEL RITUAL (bloom creciendo → cegador en el haz) ===
            if (enCarga)
                LumenLib.BloomPulse(Main.spriteBatch, center, 16f + 30f * charge,
                    PaletaRencor[1], 0.30f + 0.30f * charge, time, 1.4f, 2);
            else
                LumenLib.BloomPulse(Main.spriteBatch, center, 46f * vida,
                    PaletaRencor[2], 0.55f * vida, time, 2.2f, 2);
        }

        /// <summary>Una runa del círculo (la tecnología de la casa — TormentaNebular).</summary>
        private void DibujarRuna(Vector2 center, float radius, float orbit, int g,
            int count, Vector2[] strokes, Color body, float gScale, float vida, float lit)
        {
            float ang = g / (float)count * MathHelper.TwoPi + orbit;
            float floatR = radius + 2.4f * gScale * (float)Math.Sin(
                Main.GlobalTimeWrappedHourly * 1.35f + g * 0.9f);
            Vector2 pos = center + new Vector2(
                (float)Math.Cos(ang) * floatR,
                (float)Math.Sin(ang) * floatR * 0.82f);
            float pulse = 0.75f + 0.25f * (float)Math.Sin(
                Main.GlobalTimeWrappedHourly * 2.4f + g * 1.3f);

            Quad(VFXCore.SoftGlow, pos, new Vector2(30f * gScale, 30f * gScale), 0f,
                Tint(body, 0.20f * pulse * vida * lit));

            for (int s = 0; s < strokes.Length; s += 2)
            {
                Vector2 a = pos + strokes[s] * gScale;
                Vector2 b = pos + strokes[s + 1] * gScale;
                Vector2 mid = (a + b) * 0.5f;
                Vector2 d = b - a;
                float len = d.Length();
                if (len < 0.01f) continue;
                Capsule(mid, len, 3.2f * gScale, (float)Math.Atan2(d.Y, d.X),
                    Tint(body, 0.85f * pulse * vida * lit));
            }
        }

        /// <summary>LOS BRAZOS ESPECTRALES (v6.30 — PASE ALFA): SILUETAS
        /// NEGRAS con borde rojo oscuro — MEDIDO del sprite Rancor_Arms de
        /// la referencia (31% negro puro + 22% (32,0,0) + 17% (64,32,32)): manos-
        /// garra de SOMBRAS que brotan del muro herido, NO huesos blancos.</summary>
        private void DibujarBrazos(float time, float vida)
        {
            for (int b = 0; b < Brazos; b++)
            {
                if (!_brazoNacido[b]) continue;
                float edad = _brazoEdad[b];
                // La retracción del cierre: los últimos 12 ticks se encogen.
                float retract = MathHelper.Clamp((TotalTicks - _age) / 12f, 0f, 1f);
                float pulsar = 0.8f + 0.2f * (float)Math.Sin(time * 6f + b * 1.7f);

                GeomBrazo(b, out Vector2 basePos, out Vector2 codo, out Vector2 mano);
                basePos -= Main.screenPosition;
                codo -= Main.screenPosition;
                mano -= Main.screenPosition;

                // EL BRAZO AL NACER: emerge del muro en 8 ticks (el brote).
                float brote = MathHelper.Clamp(edad / 8f, 0f, 1f);

                // EL SEGMENTO SUPERIOR (sombra negra + borde rojo oscuro).
                DibujarSegmentoBrazo(basePos, Vector2.Lerp(basePos, codo, brote), 9f, pulsar * vida * retract);
                // EL ANTEBRAZO.
                DibujarSegmentoBrazo(codo, Vector2.Lerp(codo, mano, brote), 7f, pulsar * vida * retract);

                // LA MANO-GARRA: la masa negra de la muñeca + los 4 DEDOS.
                Quad(VFXCore.SoftGlow, mano, new Vector2(13f, 13f), 0f,
                    Tint(RojoBrazo, 0.60f * vida * retract));
                Quad(VFXCore.SoftGlow, mano, new Vector2(9f, 9f), 0f,
                    Tint(SombraBrazo, 0.88f * vida * retract));
                float angMano = MathF.Atan2(mano.Y - codo.Y, mano.X - codo.X);
                for (int f = 0; f < 4; f++)
                {
                    float fa = angMano + (f - 1.5f) * 0.38f +
                        0.10f * MathF.Sin(time * 5f + f * 2.0f + b);
                    float flargo = (13f + 4f * (f % 2)) * retract * brote;
                    Vector2 punta = mano + new Vector2((float)Math.Cos(fa), (float)Math.Sin(fa)) * flargo;
                    DibujarSegmentoBrazo(mano, punta, 3.2f, 0.9f * pulsar * vida * retract);
                }
            }
        }

        /// <summary>EL RESPLANDOR DEL BROTE (aditivo — v6.30): la herida roja
        /// del muro donde nace cada brazo (LA ÚNICA luz de los brazos: el
        /// cuerpo es sombra, el brote ARDE).</summary>
        private void DibujarAurasBrote(float time, float vida)
        {
            for (int b = 0; b < Brazos; b++)
            {
                if (!_brazoNacido[b]) continue;
                float retract = MathHelper.Clamp((TotalTicks - _age) / 12f, 0f, 1f);
                GeomBrazo(b, out Vector2 basePos, out _, out _);
                basePos -= Main.screenPosition;
                float pulso = 0.7f + 0.3f * (float)Math.Sin(time * 7f + b * 2.1f);
                Quad(VFXCore.SoftGlow, basePos, new Vector2(38f, 38f), 0f,
                    Tint(PaletaRencor[1], (0.20f + 0.10f * pulso) * vida * retract));
                Quad(VFXCore.SoftGlow, basePos, new Vector2(14f, 14f), 0f,
                    Tint(new Color(255, 120, 140), 0.30f * vida * retract * pulso));
            }
        }

        /// <summary>Un segmento del brazo: borde ROJO OSCURO ancho + cuerpo
        /// NEGRO (la silueta medida — dibujado en el PASE ALFA).</summary>
        private void DibujarSegmentoBrazo(Vector2 a, Vector2 b, float ancho, float alpha)
        {
            if (alpha <= 0.02f) return;
            Vector2 mid = (a + b) * 0.5f;
            Vector2 d = b - a;
            float len = d.Length();
            if (len < 0.5f) return;
            float rot = (float)Math.Atan2(d.Y, d.X);
            // El borde rojo oscuro (más ancho, tenue — el rim de la sombra).
            Capsule(mid, len, ancho * 2.0f, rot, Tint(RojoBrazo, 0.50f * alpha));
            // EL CUERPO NEGRO (la silueta espectral medida).
            Capsule(mid, len, ancho, rot, Tint(SombraBrazo, 0.88f * alpha));
        }

        /// <summary>Los glifos del círculo del rencor. v6.50.3 — FIX (cero GC): era PROPIEDAD —
        /// el jagged array se alocaba CADA FRAME desde el PreDraw (3 glifos ×
        /// N proyectiles × 60 fps); ahora static readonly: UNA vez.</summary>
        private static readonly Vector2[][] GlifosRencor = new Vector2[][]
        {
            // EL COLMILLO
            new Vector2[] { new(-3.5f, 6f), new(0f, -6.5f), new(0f, -6.5f), new(3.5f, 6f), new(-2f, 2f), new(2f, 2f) },
            // EL OJO LLORANDO
            new Vector2[] { new(-4f, -2f), new(0f, -5f), new(0f, -5f), new(4f, -2f), new(4f, -2f), new(0f, 3f), new(0f, 3f), new(-4f, -2f), new(0f, 3f), new(0f, 7f) },
            // LA LLAMA ATADA
            new Vector2[] { new(0f, 6f), new(0f, -6f), new(-3.5f, 2f), new(3.5f, 2f), new(-2.5f, -4f), new(0f, -6f), new(2.5f, -4f), new(0f, -6f) },
        };

        // ------------------------------------------------------------------
        //  HELPERS DE DIBUJO (los de la casa)
        // ------------------------------------------------------------------

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
