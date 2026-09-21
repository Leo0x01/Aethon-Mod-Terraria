using System;
using System.Collections.Generic;
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
    /// SembradorPulsarProjectile — v6.31 — EL PÚLSAR ANCLA del Sembrador del
    /// Cementerio Estelar (research/v631, INFORME_STAR_TOMB_DRAGON_WORD.md
    /// ficha 1 — la siembra del púlsar ancla, destilada a la casa).
    ///
    /// LA CRONOLOGÍA DE LA ESTRELLA (600 ticks):
    ///   · EL VUELO (26 ticks): sale RÁPIDA hacia el cursor y FRENA
    ///     geométricamente — velocity ×0.885 por tick. La serie
    ///     0.885¹..0.885²⁶ suma 7.375·v₀: el ítem resuelve v₀ =
    ///     distancia/7.375 para que aterrice EXACTO, y el snap final clava
    ///     el centro en el punto objetivo (ai[0..1], viaja en el paquete
    ///     de spawn).
    ///   · EL ANCLAJE (tick 26): velocity = 0 PARA SIEMPRE; golpe de
    ///     anclaje ×1.0 en radio 110, kick de cámara + flash + retumbo
    ///     grave, y chispas de aterrizaje.
    ///   · LA ESTRELLA ANCLADA: GIRA acelerando — spin lerp(0.03, 0.135,
    ///     t²) en 120 ticks — y barre con DOS HAZES-FARO OPUESTOS de 500 px
    ///     por el eje magnético DESALINEADO (ai[2]: el eje gira con el spin
    ///     → faro). Cada enemigo tocado por un haz recibe ×0.25 con
    ///     i-frames propios de 10 ticks por objetivo.
    ///
    /// Determinismo MP: el punto objetivo y la fase magnética viajan en
    /// ai[] (paquete de spawn); la semilla visual es Projectile.identity;
    /// el daño por GolpeMotor — v6.50 (el cauce del motor: crítica real,
    /// varianza, on-hit y sync MP del propio motor, resuelto en el
    /// cliente dueño); el visual solo cliente (PreDraw).
    /// </summary>
    public class SembradorPulsarProjectile : ModProjectile
    {
        // === LA CRONOLOGÍA (ticks) ===
        private const int FrenadoTicks = 26;
        private const int VidaTicks = 600;
        private const int SpinUpTicks = 120;

        /// <summary>El freno geométrico por tick (la estrella pierde 11.5% de su velocidad).</summary>
        private const float FrenoK = 0.885f;

        /// <summary>La serie 0.885¹..0.885²⁶: lo que viaja la estrella en unidades de v₀.</summary>
        public const float TravelFactor = 7.375f;

        /// <summary>El largo de cada haz-faro (el alcance del barrido).</summary>
        private const float BeamLength = 500f;

        /// <summary>Ancho de colisión del haz (radio 17 — la cápsula polar).</summary>
        private const float BeamColision = 34f;

        /// <summary>i-frames PROPIOS por objetivo (un golpe de haz cada 10 ticks).</summary>
        private const int IframesHaz = 10;

        /// <summary>El daño de cada_TICK de haz (el anclaje pega ×1.0).</summary>
        private const float DañoHaz = 0.25f;

        /// <summary>El radio del golpe de anclaje (la onda al clavarse).</summary>
        private const float RadioAnclaje = 110f;

        // === LA PALETA (los colores medidos del púlsar original) ===
        private static readonly Color ColorVioleta = new(138, 79, 255);   // violeta neutrón
        private static readonly Color ColorAzul = new(120, 181, 255);     // azul magnetosfera
        private static readonly Color ColorBlanco = new(199, 214, 255);   // blanco-azulado caliente
        private static readonly Color ColorProfundo = new(31, 26, 128);   // base profunda de la corteza

        private static readonly Color[] Paleta = { ColorVioleta, ColorAzul, ColorBlanco, ColorProfundo };

        private float _age;
        private bool _nacio;
        private bool _anchored;
        private float _anchorAge;
        private float _spinPhase;
        private float _magPhase;
        private Vector2 _target;

        /// <summary>i-frames por objetivo: whoAmI del NPC → tick del último golpe de haz.</summary>
        private readonly Dictionary<int, int> _ultimoGolpe = new();

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            // Daño 100% sub-ataques por GolpeMotor (v6.50 — el cauce del
            // motor): sin contacto de vanilla.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = VidaTicks + 2;
            Projectile.ignoreWater = true;
            // La estrella atraviesa paredes: es un cadáver estelar, no un objeto.
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        /// <summary>Semilla determinista (identidad de red: la misma en todas las máquinas).</summary>
        private int Seed => Math.Max(1, Projectile.identity + 29);

        /// <summary>LA ENVOLVENTE DEL FARO: |cos(spin)|⁵ — pico dos veces por vuelta (los dos polos).</summary>
        private float Faro()
        {
            if (!_anchored) return 0.5f;
            float c = MathF.Abs(MathF.Cos(_spinPhase));
            return c * c * c * c * c;
        }

        /// <summary>El eje magnético: DESALINEADO del eje de spin (ai[2]) — por eso el haz BARRE como un faro.</summary>
        private Vector2 Eje()
            => new Vector2(MathF.Cos(_spinPhase + _magPhase), MathF.Sin(_spinPhase + _magPhase));

        public override void AI()
        {
            _age += 1f;

            // === EL NACIMIENTO: clavar el punto objetivo (viaja en ai[]) ===
            if (!_nacio)
            {
                _nacio = true;
                _target = new Vector2(Projectile.ai[0], Projectile.ai[1]);
                _magPhase = Projectile.ai[2];
                if (_target == Vector2.Zero) _target = Projectile.Center;
            }

            if (!_anchored)
            {
                // ============================================================
                //  EL VUELO — el freno geométrico: la estrella pierde 11.5%
                //  de su velocidad por tick y muere parada en el cursor.
                // ============================================================
                Projectile.velocity *= FrenoK;

                // La luz del vuelo (violeta frío).
                Lighting.AddLight(Projectile.Center, 0.30f, 0.26f, 0.55f);

                // La estela de chispas (cliente, determinista — semilla + edad).
                if (Main.netMode != NetmodeID.Server && _age % 2f == 0f)
                {
                    RiftLib.ChispasAnomalia(Projectile.Center, 2, Paleta, Seed + (int)_age, out ParticleData[] motas);
                    if (motas != null)
                        for (int i = 0; i < motas.Length; i++)
                            ParticleManager.Spawn(motas[i]);
                }

                if (_age >= FrenadoTicks)
                {
                    // ========================================================
                    //  EL ANCLAJE — la estrella se CLAVA donde estaba el cursor
                    // ========================================================
                    _anchored = true;
                    _anchorAge = 0f;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.Center = _target;   // aterrizaje EXACTO (corrige el redondeo de la serie)

                    // EL GOLPE DE ANCLAJE ×1.0 (la onda al clavarse).
                    GolpearAnclaje();

                    if (Main.netMode != NetmodeID.Server)
                    {
                        // Kick de cámara + flash + RETUMBO grave + chispas de aterrizaje.
                        OndaLib.Kick(3.2f, 10);
                        OndaLib.Flash(ColorVioleta, 0.22f, 8);
                        try { Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9.WithPitchOffset(-0.7f), Projectile.Center); }
                        catch { }
                        RiftLib.ChispasAnomalia(Projectile.Center, 12, Paleta, Seed + 77, out ParticleData[] motas);
                        if (motas != null)
                            for (int i = 0; i < motas.Length; i++)
                                ParticleManager.Spawn(motas[i]);
                    }
                }
            }
            else
            {
                // ============================================================
                //  LA ESTRELLA ANCLADA — el spin-up y el faro
                // ============================================================
                _anchorAge += 1f;

                // EL SPIN-UP: el giro se acelera con el tiempo² (0.03→0.135 rad/tick en 120 ticks).
                float t = MathHelper.Clamp(_anchorAge / SpinUpTicks, 0f, 1f);
                _spinPhase += MathHelper.Lerp(0.03f, 0.135f, t * t);

                // El fadeIn de los haces (8 ticks): no dañan antes de 0.2.
                float beamFade = MathHelper.Clamp(_anchorAge / 8f, 0f, 1f);
                if (beamFade >= 0.2f)
                    GolpearHaces();

                // LA LUZ VIOLETA LATE CON EL FARO.
                float faro = Faro();
                Lighting.AddLight(Projectile.Center,
                    0.32f + 0.26f * faro, 0.24f + 0.20f * faro, 0.58f + 0.30f * faro);

                // EL FLUJO POLAR: chispas a lo largo del eje cada 3 ticks (cliente).
                if (Main.netMode != NetmodeID.Server && _age % 3f == 0f)
                {
                    Vector2 dir = Eje();
                    RiftLib.ChispasAnomalia(Projectile.Center + dir * 30f, 1, Paleta, Seed + (int)_age, out ParticleData[] motas);
                    if (motas != null)
                        for (int i = 0; i < motas.Length; i++)
                            ParticleManager.Spawn(motas[i]);
                }
            }
        }

        /// <summary>
        /// EL GOLPE DE ANCLAJE (×1.0): todo NPC en radio 110 del punto de
        /// aterrizaje recibe el daño pleno. v6.50 — GolpeMotor (el cauce
        /// del motor: crítica real, varianza, on-hit y sync MP del propio
        /// motor); el debuff sigue siendo autoridad.
        /// </summary>
        private void GolpearAnclaje()
        {
            int dmg = Math.Max(1, Projectile.damage);
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;

                float alcance = RadioAnclaje + Math.Max(npc.width, npc.height) * 0.5f;
                if (Vector2.Distance(npc.Center, _target) > alcance) continue;

                Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 6f, true);
                // La estrella muerta quema hacia dentro (20 s — el debuff de la casa).
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    try { npc.AddBuff(ModContent.BuffType<Content.Buffs.QuemaduraCosmica>(), 1200); } catch { }
                // Los haces respetan SUS i-frames también tras el anclaje.
                _ultimoGolpe[npc.whoAmI] = (int)_age;
            }
        }

        /// <summary>
        /// LOS HAZES-FARO (×0.25 por impacto): las DOS líneas-polares opuestas
        /// (500 px × radio 17) barriendo por el eje magnético. i-frames
        /// PROPIOS de 10 ticks por objetivo. v6.50 — GolpeMotor (el cauce
        /// del motor: crítica real, varianza, on-hit y sync MP del propio
        /// motor); el debuff sigue siendo autoridad.
        /// </summary>
        private void GolpearHaces()
        {
            Vector2 eje = Eje();
            int dmg = Math.Max(1, (int)(Projectile.damage * DañoHaz));

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;

                bool tocado = RiftLib.LineaToca(Projectile.Center, eje, BeamLength, BeamColision, npc.Hitbox)
                           || RiftLib.LineaToca(Projectile.Center, -eje, BeamLength, BeamColision, npc.Hitbox);
                if (!tocado) continue;

                // i-frames PROPIOS por objetivo: un golpe de haz cada 10 ticks.
                if (_ultimoGolpe.TryGetValue(npc.whoAmI, out int ultimo) && _age - ultimo < IframesHaz)
                    continue;
                _ultimoGolpe[npc.whoAmI] = (int)_age;

                Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 1.5f, true);
                // El plasma del haz reaviva la quemadura.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    try { npc.AddBuff(ModContent.BuffType<Content.Buffs.QuemaduraCosmica>(), 240); } catch { }
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // La corteza EVAPORA: la estrella se despide (también cuando el
            // Sembrador se la come por el tope de 3).
            try { Terraria.Audio.SoundEngine.PlaySound(SoundID.Item12.WithPitchOffset(0.2f), Projectile.Center); }
            catch { }
            RiftLib.ChispasAnomalia(Projectile.Center, 10, Paleta, Seed + 99, out ParticleData[] motas);
            if (motas != null)
                for (int i = 0; i < motas.Length; i++)
                    ParticleManager.Spawn(motas[i]);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // ============================================================
            //  CONTRATO DE BATCH v6.10 (a prueba de balas): en PreDraw el
            //  batch de tML está ABIERTO — cerrarlo antes del pase propio.
            // ============================================================
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try { DrawPulsar(); }
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

        /// <summary>EL DESENLACE VISUAL del púlsar: el lote aditivo (bloom, anillo,
        /// haces, barridos) y DESPUÉS el cuerpo — la técnica del sol de la casa.</summary>
        private void DrawPulsar()
        {
            float time = Main.GlobalTimeWrappedHourly;
            int seed = Seed;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            float faro = Faro();
            float beamFade = _anchored ? MathHelper.Clamp(_anchorAge / 8f, 0f, 1f) : 0f;
            // El final de la vida se apaga suave (los últimos 40 ticks).
            float fade = MathHelper.Clamp(Projectile.timeLeft / 40f, 0f, 1f);
            // El cuerpo nace pequeño y se asienta en 6 ticks; late con el faro.
            float R = 26f * (1f + 0.06f * faro) * (0.75f + 0.25f * MathHelper.Clamp(_age / 6f, 0f, 1f));

            // ============================================================
            //  EL LOTE ADITIVO — la magnetosfera y el faro
            // ============================================================
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            // 1. EL BLOOM LATIENDO CON EL FARO (el resplandor de la estrella muerta).
            LumenLib.Bloom(Main.spriteBatch, drawPos, R * (2.0f + 0.8f * faro),
                ColorVioleta, (0.40f + 0.25f * faro) * fade, 3);

            // 2. LA ESTELA DE VUELO: el resplandor estirado CONTRA la velocidad
            //     (mientras frena, la estrella deja su rastro de plasma).
            if (!_anchored && Projectile.velocity.LengthSquared() > 1f)
            {
                Vector2 vdir = Vector2.Normalize(Projectile.velocity);
                float v = Projectile.velocity.Length();
                LumenLib.Ray(Main.spriteBatch, drawPos, -vdir, 30f + v * 2.2f, R * 1.1f,
                    ColorAzul, 0.35f * fade, 0.6f);
            }

            // 3. EL ANILLO FINO GIRATORIO (el ecuador de la magnetosfera).
            float ringRot = _anchored
                ? _spinPhase + _magPhase + MathHelper.PiOver2
                : time * 3f;
            DrawAnillo(Main.spriteBatch, drawPos, R, ringRot, time, seed, fade);

            // 4. LOS DOS HAZES-FARO OPUESTOS (tras anclar, creciendo con el fadeIn).
            if (_anchored && beamFade > 0.01f)
            {
                DrawHazes(Main.spriteBatch, drawPos, R, faro, beamFade, fade);
                // Los destellos del barrido (visual cliente puro).
                DrawBarridos(Main.spriteBatch, R, time, beamFade * fade);
            }

            Main.spriteBatch.End();

            // ============================================================
            //  5. EL CUERPO — LA TÉCNICA DEL SOL DE LA CASA (DrawSunBody
            //     gestiona SUS lotes → DESPUÉS del aditivo): el disco
            //     azul-blanco gira SOLDADO AL EJE con spinMul ALTO (la
            //     estrella ES el giro), paleta azul-blanca de púlsar.
            // ============================================================
            float eje = _anchored
                ? _spinPhase + _magPhase
                : (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X);
            RuneSunRenderer.DrawSunBody(drawPos, R, eje, time,
                new Color(235, 242, 255),   // main — blanco azulado
                new Color(120, 155, 235),   // darker — azul acero
                new Color(70, 90, 220),     // accent — azul profundo
                new Color(150, 190, 255),   // backHot — halo azul
                new Color(90, 110, 255),    // backRed — halo violeta frío
                new Color(200, 220, 255),   // shine — destello
                3.2f, fade * (0.82f + 0.18f * faro));
        }

        /// <summary>EL ANILLO DE EMISIÓN: el aro fino perpendicular al giro + las
        /// perlas orbitando (el material cayendo al disco) — deterministas.</summary>
        private void DrawAnillo(SpriteBatch batch, Vector2 drawPos, float R,
            float rot, float time, int seed, float alpha)
        {
            // El aro fino (girado solidario al eje).
            Vector2 size = VFXCore.RingQuadSize(R * 2.3f);
            batch.Draw(VFXCore.Ring, drawPos, null, Tint(ColorAzul, 0.38f * alpha), rot,
                VFXCore.Ring.Size() * 0.5f, size / VFXCore.Ring.Size(), SpriteEffects.None, 0f);

            // Las perlas corriendo por el anillo (contra-rotando).
            const int Perlas = 4;
            for (int i = 0; i < Perlas; i++)
            {
                float t = i / (float)Perlas * MathHelper.TwoPi - time * 1.3f;
                Vector2 local = new Vector2(MathF.Cos(t), MathF.Sin(t) * 0.32f) * (R * 2.3f);
                Vector2 pearl = drawPos + local.RotatedBy(rot);
                float tw = 0.55f + 0.45f * MathF.Sin(time * 5f + i * 2.4f + seed);
                Vector2 scale = new Vector2(R * 0.30f, R * 0.30f) / VFXCore.SoftGlow.Size();
                batch.Draw(VFXCore.SoftGlow, pearl, null, Tint(ColorBlanco, 0.50f * tw * alpha), 0f,
                    VFXCore.SoftGlow.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
        }

        /// <summary>LOS DOS HAZES-FARO: quads rotando con el eje magnético — el
        /// haz principal (Ray de 500 px latiendo con el faro) + el abanico
        /// tenue que el barrido deja atrás + la boca de descarga polar.</summary>
        private void DrawHazes(SpriteBatch batch, Vector2 drawPos, float R,
            float faro, float beamFade, float fade)
        {
            float axisAngle = _spinPhase + _magPhase;
            for (int side = 0; side < 2; side++)
            {
                Vector2 dir = new Vector2(
                    MathF.Cos(axisAngle + side * MathHelper.Pi),
                    MathF.Sin(axisAngle + side * MathHelper.Pi));

                // a) EL ABANICO DEL FARO: 3 rayos tenues tras el haz (el rastro
                //    del barrido — la memoria del plasma).
                for (int i = 0; i < 3; i++)
                {
                    float spread = (i / 2f - 0.5f) * 0.30f;   // ±8.6°
                    Vector2 coneDir = dir.RotatedBy(spread);
                    LumenLib.Ray(batch, drawPos + coneDir * (R * 0.7f), coneDir,
                        BeamLength * 0.38f * beamFade, R * 0.85f,
                        ColorAzul, 0.13f * beamFade * fade, 0.4f + 0.3f * faro);
                }

                // b) EL HAZ PRINCIPAL: 500 px creciendo con el fadeIn, el grosor
                //    respirando al latido del faro.
                LumenLib.Ray(batch, drawPos + dir * (R * 0.55f), dir,
                    BeamLength * (0.35f + 0.65f * beamFade), R * (1.45f + 0.55f * faro),
                    ColorAzul, (0.50f + 0.28f * faro) * beamFade * fade, faro);

                // c) LA BOCA POLAR: el gorro de descarga del casquete.
                LumenLib.Bloom(batch, drawPos + dir * (R * 0.55f), R * 1.3f,
                    ColorBlanco, 0.50f * beamFade * fade, 2);
            }
        }

        /// <summary>LOS DESTELLOS DEL BARRIDO (visual CLIENTE puro): releer los
        /// enemigos y chispear donde el haz los está tocando — se VE el faro golpear.</summary>
        private void DrawBarridos(SpriteBatch batch, float R, float time, float alpha)
        {
            float axisAngle = _spinPhase + _magPhase;
            for (int side = 0; side < 2; side++)
            {
                Vector2 dir = new Vector2(
                    MathF.Cos(axisAngle + side * MathHelper.Pi),
                    MathF.Sin(axisAngle + side * MathHelper.Pi));
                Vector2 a = Projectile.Center + dir * (R * 0.5f);
                Vector2 b = Projectile.Center + dir * BeamLength;

                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    if (DistToSegment(npc.Center, a, b) > R * 2.2f) continue;

                    Vector2 hit = npc.Center - Main.screenPosition;
                    // Más brillante cuando el haz apunta DIRECTO al enemigo.
                    float aim = Vector2.Dot(Vector2.Normalize(npc.Center - Projectile.Center), dir);
                    float strength = MathHelper.Clamp((aim - 0.70f) / 0.30f, 0f, 1f);
                    if (strength <= 0.05f) continue;

                    StormLib.ImpactFlash(batch, hit, R * 1.1f,
                        ColorAzul, 0.50f * strength * alpha, time * 3f + side);
                }
            }
        }

        /// <summary>Distancia punto → segmento (el test del barrido del haz).</summary>
        private static float DistToSegment(Vector2 pt, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.LengthSquared();
            if (len2 < 0.001f) return (pt - a).Length();
            float t = MathHelper.Clamp(Vector2.Dot(pt - a, ab) / len2, 0f, 1f);
            return (pt - a - ab * t).Length();
        }

        /// <summary>Tinte de intensidad LINEAL de la casa (v6.50.3 — el Additive
        /// de FNA es (SourceAlpha, One): el alfa GATEA; RGB intacto, alfa=f).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            // v6.50.3 — FIX (sonda IL contra el FNA real): BlendState.Additive
            // de FNA es (SourceAlpha, One) — el alfa GATEA el aporte. El Tint
            // premultiplicado v6.25 atenuaba DOS VECES (intensidad real f²:
            // el halo 0.30 salía a 0.09). RGB intacto, alfa=f: LINEAL.
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }
    }
}
