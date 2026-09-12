using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Particles;
using AethonMod.Content.Effects;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// VoidEyeProjectile — EL OJO DEL VACÍO (arma nueva de terror cósmico,
    /// v5.96 — petición del usuario: "crea otra arma nueva de prueba con la
    /// que has aprendido y esta nueva arma debe tener un proyectil lo más
    /// cósmico y de terror cósmico que se te ocurra, lo dejo a tu imaginación").
    /// (v5.97: el arma ya se le entrega al jugador en TestingPlayer — faltaba.)
    ///
    /// UNA ESTRELLA MUERTA CON UN OJO VIVO. Todo lo aprendido en el arsenal
    /// cósmico, condensado en un solo cuerpo celeste que te observa:
    ///   - CUERPO: una estrella MUERTA — el SunShader con paleta invertida
    ///     (carbón oscuro con vetas carmesí: el gemelo maligno del sol).
    ///   - OJO: esclerótica marfil enfermizo con VENAS generadas (EyeSclera),
    ///     iris ÁMBAR que ROTA lentamente (los iris no deberían rotar) y
    ///     MIRA al enemigo más cercano — el offset del iris SIGUE al objetivo.
    ///     Si no hay enemigos... te mira A TI (al jugador).
    ///   - PUPILA: un MICRO AGUJERO NEGRO (RealBlackHoleShader, 75 pasos de
    ///     lensing con su propio disco de acreción carmesí). La pupila SE
    ///     DILATA con el terror — y con ella, el AURA DE DAÑO y la LENTE.
    ///   - PÁRPADOS: carne muerta (EyeLid) que se ABREN LENTO al despertar,
    ///     PARPADEAN cada ~3.3 s (el ojo cerrado DAÑA EL DOBLE: en la oscuridad
    ///     es cuando alimenta) y se ABREN DE PAR EN PAR en la fase final.
    ///   - LENTE: el BlackHoleLensSystem lo recoge como fuente SUTIL (pase B,
    ///     como la gigante roja del sol): el espacio se curva alrededor del
    ///     ojo mientras la pupila se dilata.
    ///
    /// CICLO DE VIDA (12 s = 720 ticks):
    ///   - t=0-0.8s   LA GRIETA: la estrella muerta emerge LENTA (sin pop
    ///                elástico — un peso siniestro), el ojo CERRADO. Sonido
    ///                grave (MoonLord). Materia oscura converge.
    ///   - t=0.8-3s   EL DESPERTAR: los párpados se abren LENTO (smoothstep).
    ///                El iris comienza a rotar. Primer quejido (ZombieMoan).
    ///   - t=3-10s    LA OBSERVACIÓN: el ojo SE ARRASTRA hacia su objetivo;
    ///                AURA DE TERROR (daño de área CRECIENTE con la dilatación
    ///                + ShadowFlame + ralentización por pavor ×0.92); PARPADEO
    ///                cada ~3.3 s (daño ×2 y gravedad ×2.5 mientras cerrado);
    ///                quejidos susurrados cada ~2.8 s; lágrimas de sangre.
    ///   - t=10-12s   EL TERROR: párpados retraídos (ojo DE PAR EN PAR), iris
    ///                ÁMBAR→SANGRE, pupila a máxima dilatación, la estrella se
    ///                hincha ×1.4, la lente se dispara, gravedad ×4 (arrastre).
    ///                Aura cada 0.1 s al 75% del daño. Gemido creciente.
    ///   - t=12s      EL GRITO (OnKill): chillido (ScaryScream) + implosión de
    ///                materia oscura + AoE del núcleo (380 px, ×1.6, ShadowFlame
    ///                8 s + Weak) + UN ÚNICO ANILLO DE EINSTEIN — EL DESGARRO —
    ///                con el daño del grito COMPLETO y su flash de liberación
    ///                (v5.97: una sola explosión, como el sol y el agujero) +
    ///                temblor fuerte.
    ///
    /// El daño ES daño de área (filosofía v5.96): el hitbox de contacto solo
    /// cubre el cuerpo de la estrella; el AURA que se extiende POR FUERA crece
    /// con la dilatación de la pupila y culmina en la explosión.
    ///
    /// Campos AI: ai[0]=edad visual · ai[1]=libre · ai[2]=daño base.
    /// localAI: nada persistente — TODO (dilatación, apertura, terror) se
    /// deriva DETERMINISTA de la edad → MP coherente sin sincronizar nada.
    /// </summary>
    public class VoidEyeProjectile : ModProjectile
    {
        /// <summary>Duración total: 12 segundos.</summary>
        internal const int EyeLifetime = 720;

        /// <summary>Fin de LA GRIETA (emergencia de la estrella muerta).</summary>
        internal const int RiftEnd = 48;

        /// <summary>Fin de EL DESPERTAR (párpados abiertos).</summary>
        internal const int AwakeEnd = 180;

        /// <summary>Inicio de EL TERROR.</summary>
        internal const int TerrorStart = 600;

        /// <summary>Ciclo de parpadeo durante la observación.</summary>
        private const float BlinkCycle = 200f;

        // Shaders estáticos (compartidos entre instancias, como el sol)
        private static Ref<Effect> _sunShader;
        private static bool _sunShaderFailed;

        private float Age => Projectile.ai[0];

        // ================================================================
        //  FASES (deterministas — derivadas de la edad)
        // ================================================================

        /// <summary>
        /// Dilatación de la pupila: 0.55 al despertar → 1.05 al final de la
        /// observación → 1.35 en pleno terror. Es el MOTOR del arma: el aura
        /// de daño, la lente y el tamaño de la pupila TODO crece con ella.
        /// </summary>
        internal static float GetDilation(Projectile p)
        {
            float age = p.ai[0];
            float dil = 0.55f + 0.5f * MathHelper.Clamp((age - RiftEnd) / 500f, 0f, 1f);
            if (age > TerrorStart)
            {
                float tt = MathHelper.Clamp((age - TerrorStart) / (float)(EyeLifetime - TerrorStart), 0f, 1f);
                dil += 0.3f * (tt * tt * (3f - 2f * tt));
            }
            return dil;
        }

        /// <summary>
        /// Apertura de los párpados (0 = cerrado, 1 = abierto, >1 = retraído
        /// de par en par en el terror). Determinista: el parpadeo, el daño
        /// en la oscuridad y el dibujado coinciden en TODAS las máquinas.
        /// </summary>
        internal static float GetOpenAmount(float age)
        {
            if (age < RiftEnd) return 0f;                    // la grieta: cerrado
            if (age < AwakeEnd)                              // el despertar: LENTO
            {
                float t = (age - RiftEnd) / (float)(AwakeEnd - RiftEnd);
                return t * t * (3f - 2f * t);                // smoothstep
            }
            if (age < TerrorStart)                           // la observación
            {
                float c = (age - AwakeEnd) % BlinkCycle;
                if (c >= 95f && c < 100f) return 1f - (c - 95f) / 5f;   // cierra
                if (c >= 100f && c < 108f) return 0f;                    // CERRADO
                if (c >= 108f && c < 118f) return (c - 108f) / 10f;      // reabre
                return 1f;
            }
            // el terror: ojos de par en par (más allá de 1 — retraídos)
            float tt = MathHelper.Clamp((age - TerrorStart) / (float)(EyeLifetime - TerrorStart), 0f, 1f);
            return 1f + 0.3f * (tt * tt * (3f - 2f * tt));
        }

        /// <summary>
        /// Radio visual de la estrella muerta (px) — misma convención que el
        /// sol: width×scale×0.75. Crece con la hinchazón del terror.
        /// </summary>
        internal static float GetEyeVisualRadius(Projectile p)
        {
            return p.width * p.scale * 0.75f;
        }

        /// <summary>Progreso del terror (0..1 en los últimos 2 s).</summary>
        private static float GetTerrorProgress(Projectile p)
        {
            return p.ai[0] <= TerrorStart ? 0f
                : MathHelper.Clamp((p.ai[0] - TerrorStart) / (float)(EyeLifetime - TerrorStart), 0f, 1f);
        }

        // ================================================================
        //  DEFAULTS
        // ================================================================

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 130;
            Projectile.height = 130;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = EyeLifetime;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        // ================================================================
        //  AI
        // ================================================================

        public override void AI()
        {
            // === EMERGENCIA SIN POP: peso siniestro ===
            // Sin ElasticOut: la estrella muerta CRECE LENTO (smoothstep en
            // 48 ticks) — lo contrario del rebote alegre del sol. Luego asciende
            // a su tamaño pleno en 120 ticks, y en el TERROR se hincha ×1.4.
            float rise = MathHelper.Clamp(Projectile.ai[0] / (float)RiftEnd, 0f, 1f);
            float baseScale;
            if (Projectile.ai[0] <= RiftEnd)
            {
                baseScale = rise * rise * (3f - 2f * rise) * 0.55f;
            }
            else if (Projectile.ai[0] <= RiftEnd + 120)
            {
                float settle = (Projectile.ai[0] - RiftEnd) / 120f;
                baseScale = MathHelper.Lerp(0.55f, 1f, settle * settle);
            }
            else
            {
                baseScale = 1f;
            }
            // === LA HINCHAZÓN DEL TERROR (últimos 2 s): ×1.4 ===
            float terror = GetTerrorProgress(Projectile);
            float swell = terror > 0f ? terror * terror * (3f - 2f * terror) : 0f;
            Projectile.scale = baseScale * (1f + 0.4f * swell);
            Projectile.ai[0] += 1f;

            float age = Projectile.ai[0];
            float dil = GetDilation(Projectile);
            float open = GetOpenAmount(age);
            bool terrorPhase = age > TerrorStart;

            // === SONIDOS DEL HORROR (cliente) ===
            if (Main.netMode != NetmodeID.Server)
            {
                if ((int)age == 2) SoundEngine.PlaySound(SoundID.MoonLord, Projectile.Center);
                if ((int)age == AwakeEnd) SoundEngine.PlaySound(SoundID.ZombieMoan, Projectile.Center);
                if ((int)age == TerrorStart) SoundEngine.PlaySound(SoundID.MoonLord, Projectile.Center);
                // quejidos susurrados durante la observación
                if (age > AwakeEnd && age < TerrorStart && (int)age % 170 == 0)
                    SoundEngine.PlaySound(SoundID.ZombieMoan, Projectile.Center);
                // el parpadeo susurra al cerrar
                if (age > AwakeEnd && age < TerrorStart)
                {
                    float c = (age - AwakeEnd) % BlinkCycle;
                    if ((int)c == 100) SoundEngine.PlaySound(SoundID.ZombieMoan, Projectile.Center);
                }
            }

            // === LA MIRADA ARRASTRA AL OJO HACIA SU OBJETIVO ===
            // (el ojo SE DESLIZA hacia lo que observa — un creep lento)
            if (Main.netMode != NetmodeID.MultiplayerClient && age > AwakeEnd)
            {
                NPC target = FindGazeTarget();
                if (target != null)
                {
                    Vector2 toTarget = target.Center - Projectile.Center;
                    if (toTarget.LengthSquared() > 100f)
                    {
                        toTarget.Normalize();
                        Projectile.velocity += toTarget * (terrorPhase ? 0.10f : 0.05f);
                    }
                }
                // frenado (flota, se resiste a moverse)
                Projectile.velocity *= 0.96f;
                float maxSpeed = terrorPhase ? 3.2f : 2.0f;
                if (Projectile.velocity.Length() > maxSpeed)
                    Projectile.velocity = Vector2.Normalize(Projectile.velocity) * maxSpeed;
            }
            else
            {
                Projectile.velocity *= 0.96f;
            }

            // === GRAVEDAD DEL PAVOR: arrastra a las víctimas ===
            float gravityRadius = terrorPhase ? 520f : 420f;
            float gravityStrength = 1.8f * (terrorPhase ? 4f : 1f);
            if (open < 0.25f) gravityStrength *= 2.5f; // cerrado: tira el DOBLE
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                Vector2 toCenter = Projectile.Center - npc.Center;
                float dist = toCenter.Length();
                if (dist > gravityRadius || dist < 5f) continue;
                float strength = (1f - dist / gravityRadius) * gravityStrength * 0.35f;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    npc.velocity += toCenter * strength;
                }
            }

            // === v5.96 — AURA DE DAÑO DE ÁREA CRECIENTE (el pavor quema) ===
            // Filosofía del arma: TODO el daño es de área, se extiende POR
            // FUERA del cuerpo y CRECE con la dilatación de la pupila. En la
            // oscuridad (párpados cerrados) daña EL DOBLE — es cuando alimenta.
            if (Main.netMode != NetmodeID.MultiplayerClient && age > AwakeEnd)
            {
                int interval = terrorPhase ? 6 : 12; // 0.1 s en terror, 0.2 s normal
                if ((int)age % interval == 0)
                {
                    float dilNorm = MathHelper.Clamp((dil - 0.55f) / 0.8f, 0f, 1f);
                    float auraRadius = terrorPhase ? 380f : 140f + 160f * dilNorm;
                    float dmgFactor = terrorPhase ? 0.75f : 0.28f + 0.34f * dilNorm;
                    if (open < 0.25f) dmgFactor *= 2f; // la oscuridad duele más
                    int auraDamage = Math.Max(1, (int)(Projectile.damage * dmgFactor));
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.CanBeChasedBy()) continue;
                        float dist = (npc.Center - Projectile.Center).Length();
                        if (dist > auraRadius) continue;
                        // ralentización por pavor: las piernas no responden
                        npc.velocity *= 0.92f;
                        int dir = npc.Center.X < Projectile.Center.X ? -1 : 1;
                        npc.SimpleStrikeNPC(auraDamage, dir, false, 0f, DamageClass.Magic);
                        // la mirada quema el alma, no la carne
                        npc.AddBuff(BuffID.ShadowFlame, terrorPhase ? 360 : 240);
                    }
                }
            }

            // === PARTÍCULAS Y DUSTS (cliente) ===
            if (Main.netMode != NetmodeID.Server)
            {
                if (age < RiftEnd) SpawnRiftIntake();
                else if (terrorPhase) SpawnTerrorDusts(dil);
                else SpawnWatchingDusts(dil, open);
            }

            // === ILUMINACIÓN: carmesí enfermo, crece con la dilatación ===
            float lightPulse = terrorPhase ? 1.6f : (0.6f + (dil - 0.55f) * 0.8f);
            Lighting.AddLight(Projectile.Center,
                new Vector3(0.34f, 0.05f, 0.08f) * lightPulse);

            // === TEMBLOR CRECIENTE en el terror ===
            if (terrorPhase && (int)age % 20 == 0)
            {
                try
                {
                    Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                        Projectile.Center, new Vector2(1f, 0f), 2f + terror * 5f, 8, 12, 0.3f,
                        "AethonVoidEyeDread"));
                }
                catch { }
            }
        }

        /// <summary>El objetivo de la mirada (compartido por daño y creep).</summary>
        private NPC FindGazeTarget()
        {
            NPC best = null;
            float minDist = 900f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                float d = (npc.Center - Projectile.Center).Length();
                if (d < minDist) { minDist = d; best = npc; }
            }
            return best;
        }

        // ------------------------------------------------------------------
        //  PARTÍCULAS / DUSTS
        // ------------------------------------------------------------------

        /// <summary>LA GRIETA: materia oscura convergiendo al nacimiento.</summary>
        private void SpawnRiftIntake()
        {
            for (int i = 0; i < 2; i++)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(120f, 210f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.Shadowflame,
                    (Projectile.Center - spawnPos) * 0.045f, 150,
                    new Color(120, 30, 60), 1.1f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        /// <summary>LA OBSERVACIÓN: lágrimas de sangre + zarcillos orbitando.</summary>
        private void SpawnWatchingDusts(float dil, float open)
        {
            // lágrimas de sangre desde el párpado inferior (solo con el ojo abierto)
            if (open > 0.5f && Main.rand.NextBool(5))
            {
                Vector2 tearPos = Projectile.Center + new Vector2(
                    Main.rand.NextFloat(-30f, 30f), 34f * Math.Max(Projectile.scale, 0.3f));
                Dust d = Dust.NewDustPerfect(tearPos, DustID.Blood,
                    new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(1f, 2.6f)),
                    120, new Color(140, 10, 25), 0.9f);
                d.noGravity = false;
                d.fadeIn = 0f;
            }

            // zarcillos de materia oscura orbitando la estrella (librería)
            if (Main.rand.NextBool(3))
            {
                float radius = Main.rand.NextFloat(70f, 110f) * Math.Max(Projectile.scale, 0.4f);
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float angVel = Main.rand.NextBool(2)
                    ? Main.rand.NextFloat(0.03f, 0.055f)
                    : -Main.rand.NextFloat(0.03f, 0.055f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius);
                var p = new ParticleData
                {
                    Position = spawnPos,
                    Velocity = Vector2.Zero,
                    Scale = new Vector2(1.3f, 0.38f),
                    Rotation = angle,
                    PackedColor = ParticleManager.PackColor(new Color(150, 30, 55, 150)),
                    PackedStartColor = ParticleManager.PackColor(new Color(150, 30, 55, 150)),
                    PackedEndColor = ParticleManager.PackColor(new Color(60, 6, 18, 20)),
                    TimeLeft = 60,
                    Duration = 60,
                    TextureId = ParticleTex.TrailGlow,
                    BlendMode = 1,
                    LayerPriority = LayerPriorities.BeforeProjectiles,
                };
                p.UserData0 = Projectile.Center.X;
                p.UserData1 = Projectile.Center.Y;
                p.UserData2 = angVel;
                p.UserData3 = radius;
                p.EnableComponent(ComponentFlag.Orbit);
                p.EnableComponent(ComponentFlag.ColorShift);
                p.EnableComponent(ComponentFlag.FadeOut);
                ParticleManager.Spawn(p);
            }

            // brasa corrupta ocasional escapando del cuerpo
            if (Main.rand.NextBool(6))
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * 60f * Math.Max(Projectile.scale, 0.3f),
                    (float)Math.Sin(angle) * 60f * Math.Max(Projectile.scale, 0.3f));
                Dust d = Dust.NewDustPerfect(pos, DustID.Corruption,
                    new Vector2((float)Math.Cos(angle) * 0.8f, (float)Math.Sin(angle) * 0.8f - 0.4f),
                    100, new Color(90, 20, 40), 0.8f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        /// <summary>EL TERROR: sangre y llama sombría a borbotones.</summary>
        private void SpawnTerrorDusts(float dil)
        {
            for (int i = 0; i < 2; i++)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(60f, 130f) * Math.Max(Projectile.scale, 0.4f);
                Vector2 pos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(pos, DustID.Shadowflame,
                        new Vector2((float)Math.Cos(angle) * 1.6f, (float)Math.Sin(angle) * 1.6f - 0.8f),
                        160, new Color(160, 40, 70), 1.2f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
                else
                {
                    Dust d = Dust.NewDustPerfect(pos, DustID.Blood,
                        new Vector2(0f, Main.rand.NextFloat(2f, 4f)), 130,
                        new Color(150, 8, 24), 1.0f);
                    d.noGravity = false;
                    d.fadeIn = 0f;
                }
            }
        }

        // ================================================================
        //  IMPACTO
        // ================================================================

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;
            // el toque de la estrella muerta quema el alma
            try { target.AddBuff(BuffID.ShadowFlame, 300); } catch { }
            SoundEngine.PlaySound(SoundID.Item14, target.Center);
        }

        // ================================================================
        //  EL GRITO — MUERTE
        // ================================================================

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                // EL GRITO: chillido + estruendo + temblor fuerte
                SoundEngine.PlaySound(SoundID.ScaryScream, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
                try
                {
                    Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                        Projectile.Center, new Vector2(1f, 0f), 9f, 13, 22, 0.5f,
                        "AethonVoidEyeScream"));
                }
                catch { }
            }

            // === ONDA (autoridad: el dueño del proyectil) ===
            if (Projectile.owner == Main.myPlayer)
            {
                // v5.97 — EL GRITO ES UNA SOLA ONDA (petición del usuario:
                // "solo deben tener una explosión donde suceda todo"): ya no
                // son la cromática inversa del colapso + el anillo retardado —
                // es EL DESGARRO: un único ANILLO DE EINSTEIN que rasga la
                // realidad desde el punto donde el ojo murió, con el daño del
                // grito COMPLETO (antes 0.6 + 0.5 repartidos). Su FLASH DE
                // LIBERACIÓN inicial (la luz del ojo escapando) + la banda fina
                // con ShadowFlame + el fondo curvándose a su paso: TODO en
                // esta única onda. Los dusts de implosión/sangre del OnKill
                // ocurren en el MISMO instante — una sola explosión.
                int tearDmg = Math.Max(1, Projectile.damage);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center.X, Projectile.Center.Y, 0f, 0f,
                    ModContent.ProjectileType<CosmicShockwaveProjectile>(),
                    tearDmg, 0f, Projectile.owner,
                    0f,                                        // edad: sin retardo — TODO sucede YA
                    CosmicShockwaveProjectile.StyleEinstein,
                    460f);                                     // radio máximo
            }

            // === AoE DEL NÚCLEO (la mirada final) ===
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int screamDamage = Math.Max(1, (int)(Projectile.damage * 1.6f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist < 380f)
                    {
                        npc.SimpleStrikeNPC(screamDamage, npc.direction,
                            false, 4f, DamageClass.Magic);
                        npc.AddBuff(BuffID.ShadowFlame, 480); // 8 s de llama sombría
                        npc.AddBuff(BuffID.Weak, 300);        // 5 s de debilidad
                    }
                }
            }

            if (Main.netMode == NetmodeID.Server) return;

            // === IMPLOSIÓN de materia oscura + EXPLOSIÓN de sangre sombría ===
            for (int i = 0; i < 46; i++)
            {
                float angle = (MathHelper.TwoPi / 46) * i;
                float dist = Main.rand.NextFloat(120f, 200f);
                Vector2 spawnPos = Projectile.Center + new Vector2(
                    (float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
                Vector2 toCenter = Projectile.Center - spawnPos;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.Shadowflame,
                        toCenter * 9f, 210, new Color(150, 30, 70), 1.5f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
            for (int i = 0; i < 40; i++)
            {
                float angle = (MathHelper.TwoPi / 40) * i;
                Vector2 dir = new Vector2(
                    (float)Math.Cos(angle) * Main.rand.NextFloat(5f, 12f),
                    (float)Math.Sin(angle) * Main.rand.NextFloat(5f, 12f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Blood,
                    dir, 200, new Color(160, 15, 35), 1.4f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // Presets de la librería (paleta del horror)
            ParticlePresets.Implosion(Projectile.Center, 160f, 36,
                new Color(170, 40, 80), 24);
            ParticlePresets.Explosion(Projectile.Center, 120f, 24,
                new Color(200, 60, 110), new Color(60, 5, 20), 36);
        }

        // ================================================================
        //  RENDER
        // ================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            // La lente va DETRÁS del ojo (igual que el sol y el agujero):
            // con la lente activa el BlackHoleLensSystem lo pinta ENCIMA.
            if (BlackHoleLensSystem.LensActive)
                return false;

            DrawEyeVisuals(Projectile, true);

            RestoreSpriteBatch();
            return false;
        }

        /// <summary>
        /// Dibuja el OJO COMPLETO (estrella muerta + ojo vivo). Compartido
        /// entre el pase del mundo (PreDraw, endActiveBatch=true) y el pase
        /// posterior a la lente (BlackHoleLensSystem, endActiveBatch=false).
        ///
        /// v5.99 — REDISEÑO TOTAL del render (petición: "el ojo no se ve
        /// nada bien, intenta mejorarlo"): texturas v2 (esclerótica esférica
        /// con venas audaces + iris turbulento per-pixel + párpados-armadura)
        /// y la PUPILA estilo GARGANTUA (Interstellar): esfera negra + anillo
        /// de fotones fino + banda de acreción cruzando DELANTE + arcos
        /// lenteados curvándose SOBRE y BAJO la esfera — nada de shader
        /// mini (a esta escala era papilla ilegible, "puerta de madera").
        ///
        /// CAPAS (de atrás a delante):
        ///   0. backglow carmesí + aro del aura de daño (aditivo).
        ///   1. la ESTRELLA MUERTA (SunShader invertido, carbón + vetas).
        ///   2. VIGNETTA del receptáculo (asienta el ojo en la estrella —
        ///      antes un anillo duro y "suelto"; ahora una cavidad suave).
        ///   3. la ESCLERÓTICA v2 (esfera de marfil, venas, especular).
        ///   4. el IRIS v2 (ámbar turbulento en órbita) rotando + MIRANDO.
        ///   5. FALLOFF oscuro iris→pupila (la pupila se HUNDE en el iris).
        ///   6. la PUPILA GARGANTUA (ver DrawPupilGargantua).
        ///   7. CATCHLIGHT unificado (media luna húmeda sobre todo el ojo).
        ///   8. los PÁRPADOS-armadura (se abren, parpadean, se retraen).
        /// </summary>
        internal static void DrawEyeVisuals(Projectile p, bool endActiveBatch)
        {
            if (!_sunShaderFailed && _sunShader == null)
            {
                try
                {
                    _sunShader = new Ref<Effect>(ModContent.Request<Effect>(
                        "AethonMod/Content/Effects/Shaders/SunShader",
                        AssetRequestMode.ImmediateLoad).Value);
                }
                catch { _sunShaderFailed = true; }
            }

            try
            {
                float age = p.ai[0];
                float dil = GetDilation(p);
                float open = GetOpenAmount(age);
                float terror = GetTerrorProgress(p);
                Vector2 drawPos = p.Center - Main.screenPosition;
                float eyeR = GetEyeVisualRadius(p);
                float scleraR = eyeR * 0.82f;

                Texture2D softGlow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                Texture2D scleraTex = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/EyeSclera").Value;
                Texture2D irisTex = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/EyeIris").Value;
                Texture2D lidTex = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/EyeLid").Value;
                Texture2D ringTex = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/Ring").Value;
                Texture2D fireRing = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/FireRing").Value;

                // === 0. BACKGLOW CARMESÍ + ARO DEL AURA (aditivo) ===
                if (endActiveBatch)
                    Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // el halo RESPIRA (lento, sutil — vida, no parpadeo)
                float breathe = 0.5f + 0.5f * (float)Math.Sin(age * 0.021f);
                float haloScale = eyeR * (2.2f + 0.9f * terror + 0.18f * breathe) /
                    (softGlow.Width * 0.5f);
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(96, 12, 28, 40 + (int)(14f * breathe)) * p.scale, 0f,
                    softGlow.Size() * 0.5f, haloScale, SpriteEffects.None, 0f);

                if (age > AwakeEnd)
                {
                    float dilNorm = MathHelper.Clamp((dil - 0.55f) / 0.8f, 0f, 1f);
                    float auraR = terror > 0f ? 380f : 140f + 160f * dilNorm;
                    float auraScale = auraR / (ringTex.Width * 0.5f * 0.92f);
                    Main.spriteBatch.Draw(ringTex, drawPos, null,
                        new Color(130, 16, 34, 22), 0f,
                        ringTex.Size() * 0.5f, auraScale, SpriteEffects.None, 0f);
                }
                Main.spriteBatch.End();

                // === 1. LA ESTRELLA MUERTA (SunShader invertido) ===
                if (_sunShader != null && _sunShader.Value != null)
                {
                    Texture2D dendritic = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Textures/DendriticNoiseZoomedOut").Value;
                    Texture2D wavyBlotch = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Textures/WavyBlotchNoise").Value;
                    Texture2D psychedelicWing = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Textures/PsychedelicWingTextureOffsetMap").Value;

                    Effect shader = _sunShader.Value;
                    shader.Parameters["coronaIntensityFactor"].SetValue(0.05f + 0.07f * terror);
                    shader.Parameters["mainColor"].SetValue(
                        Color.Lerp(new Color(52, 34, 38), new Color(70, 26, 34), terror).ToVector3());
                    shader.Parameters["darkerColor"].SetValue(
                        Color.Lerp(new Color(125, 12, 26), new Color(190, 20, 40), terror).ToVector3());
                    shader.Parameters["subtractiveAccentFactor"].SetValue(new Color(120, 0, 10).ToVector3());
                    shader.Parameters["sphereSpinTime"].SetValue(Main.GlobalTimeWrappedHourly * 0.45f);
                    shader.Parameters["globalTime"].SetValue(Main.GlobalTimeWrappedHourly);

                    Main.graphics.GraphicsDevice.Textures[1] = wavyBlotch;
                    Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                    Main.graphics.GraphicsDevice.Textures[2] = psychedelicWing;
                    Main.graphics.GraphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;

                    Vector2 drawScale = Vector2.One * p.width * p.scale * 1.5f / dendritic.Size();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                        SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    shader.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(dendritic, drawPos, null, Color.White, 0f,
                        dendritic.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                }

                // === 2. VIGNETTA DEL RECEPTÁCULO (cavidad suave — el ojo
                //     ASIENTA en la estrella, sin anillos duros sueltos) ===
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                float cavityScale = scleraR * 1.38f / (softGlow.Width * 0.5f);
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(16, 5, 8, 165), 0f, softGlow.Size() * 0.5f,
                    cavityScale, SpriteEffects.None, 0f);

                // === 3. LA ESCLERÓTICA v2 (esfera de marfil, venas audaces) ===
                float scleraScale = scleraR / (scleraTex.Width * 0.5f);
                Color scleraTint = Color.Lerp(Color.White, new Color(255, 148, 148), terror * 0.5f);
                Main.spriteBatch.Draw(scleraTex, drawPos, null, scleraTint, 0f,
                    scleraTex.Size() * 0.5f, scleraScale, SpriteEffects.None, 0f);

                // === 4. EL IRIS v2 (ámbar turbulento, MIRA al objetivo) ===
                float irisR = scleraR * 0.58f;
                Vector2 gaze = GetGazeDirection(p);
                Vector2 irisOffset = gaze * scleraR * 0.18f;
                float irisRot = age * 0.008f;
                Color irisTint = Color.Lerp(Color.White, new Color(255, 105, 95), terror);
                float irisScale = irisR / (irisTex.Width * 0.5f);
                Main.spriteBatch.Draw(irisTex, drawPos + irisOffset, null, irisTint, irisRot,
                    irisTex.Size() * 0.5f, irisScale, SpriteEffects.None, 0f);

                // === 5. FALLOFF iris→pupila (la pupila se HUNDE: gradiente
                //     oscuro entre el borde ardiente del iris y el vacío) ===
                float pupilR = irisR * 0.5f * dil;
                float fallScale = (pupilR * 2.05f) / (softGlow.Width * 0.5f);
                Main.spriteBatch.Draw(softGlow, drawPos + irisOffset, null,
                    new Color(20, 8, 14, 150), 0f, softGlow.Size() * 0.5f,
                    fallScale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();

                // === 6. LA PUPILA GARGANTUA (aditivo + alpha) ===
                DrawPupilGargantua(drawPos + irisOffset, pupilR, softGlow, ringTex, fireRing,
                    terror, age);

                // === 7. CATCHLIGHT unificado + 8. PÁRPADOS ===
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // media luna húmeda arriba-izquierda SOBRE iris+pupila (el
                // brillo unificado del ojo mojado — coordina toda la luz)
                float clOff = scleraR * 0.44f;
                float clScale = scleraR * 0.66f / (softGlow.Width * 0.5f);
                Main.spriteBatch.Draw(softGlow,
                    drawPos + new Vector2(-clOff, -clOff) + irisOffset * 0.4f, null,
                    new Color(255, 255, 248, 64), -0.6f,
                    new Vector2(softGlow.Width * 0.5f, softGlow.Height * 0.72f),
                    new Vector2(clScale, clScale * 0.42f), SpriteEffects.None, 0f);
                Main.spriteBatch.End();

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // párpados-armadura: se deslizan con `open`
                float lidSlide = scleraR * 1.28f * Math.Min(open, 1f);
                float closeOverlap = scleraR * 0.35f * (1f - Math.Min(open, 1f));
                float terrorPull = Math.Max(0f, open - 1f) * scleraR * 0.9f;
                float lidScale = scleraR * 2.45f / lidTex.Width;

                Vector2 upperPos = drawPos + new Vector2(0f, -lidSlide - terrorPull + closeOverlap);
                Main.spriteBatch.Draw(lidTex, upperPos, null, Color.White, 0f,
                    lidTex.Size() * 0.5f, lidScale, SpriteEffects.None, 0f);
                Vector2 lowerPos = drawPos + new Vector2(0f, lidSlide + terrorPull - closeOverlap);
                Main.spriteBatch.Draw(lidTex, lowerPos, null, Color.White, 0f,
                    lidTex.Size() * 0.5f, lidScale, SpriteEffects.FlipVertically, 0f);
                Main.spriteBatch.End();

                // === 8b. RIM GLOW de los párpados (aditivo): la luz del ojo
                //     BAÑA el filo de la armadura — ya no son placas planas ===
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                byte lidGlowA = (byte)(int)(52 + 60 * terror);
                Color lidGlow = new Color(255, 120, 70, lidGlowA);
                Main.spriteBatch.Draw(lidTex, upperPos, null, lidGlow, 0f,
                    lidTex.Size() * 0.5f, lidScale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(lidTex, lowerPos, null, lidGlow, 0f,
                    lidTex.Size() * 0.5f, lidScale, SpriteEffects.FlipVertically, 0f);
                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        /// <summary>
        /// Dirección de la MIRADA (cliente): hacia el enemigo más cercano
        /// dentro de 900 px… y si no hay NADIE, hacia el JUGADOR — cuando no
        /// hay nada que vigilar, el ojo te vigia A TI. Pura cosmofobia.
        /// </summary>
        private static Vector2 GetGazeDirection(Projectile p)
        {
            try
            {
                Vector2 best = Vector2.Zero;
                float bestDist = 900f;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc == null || !npc.active || !npc.CanBeChasedBy()) continue;
                    Vector2 to = npc.Center - p.Center;
                    float d = to.Length();
                    if (d < bestDist && d > 1f) { bestDist = d; best = to; }
                }
                if (best == Vector2.Zero)
                {
                    // nadie a quien mirar → EL JUGADOR
                    best = Main.LocalPlayer.Center - p.Center;
                }
                if (best.LengthSquared() > 1f)
                    return Vector2.Normalize(best);
            }
            catch { }
            return Vector2.Zero;
        }

        /// <summary>
        /// LA PUPILA GARGANTUA (v5.99): un agujero negro legible a CUALQUIER
        /// escala — el look de Interstellar dibujado con sprites:
        ///   1. BANDA VERTICAL lenteada DETRÁS: el disco de acreción curvado
        ///      por la gravedad aparece como ARCOS sobre y bajo la esfera
        ///      (la imagen lenteada del disco que hay detrás).
        ///   2. ESFERA NEGRA: el horizonte de sucesos (disco negro puro con
        ///      borde suave, no un cuadrado).
        ///   3. ANILLO DE FOTONES: aro fino blanco-caliente ABRAZANDO la
        ///      esfera (la luz atrapada orbitando) — con un micro-pulso.
        ///   4. BANDA HORIZONTAL DELANTE: el disco de acreción CRUZANDO por
        ///      delante de la esfera (oro-carmesí, aditivo).
        /// En el terror TODO se aviva y enrojece. Se DILATA con el horror:
        /// la pupila del terror no se encoge, se TRAGA la luz.
        /// Requiere el batch del mundo CERRADO (lo abre y cierra él mismo).
        /// </summary>
        private static void DrawPupilGargantua(Vector2 pupilPos, float pupilR,
            Texture2D softGlow, Texture2D ringTex, Texture2D fireRing,
            float terror, float age)
        {
            if (pupilR < 2.5f)
                return;
            try
            {
                // tonos: oro-carmesí en calma → ARDIENTE en el terror
                Color diskCol = Color.Lerp(new Color(232, 150, 70), new Color(255, 92, 60), terror);
                Color photonCol = Color.Lerp(new Color(255, 244, 208), new Color(255, 214, 170), terror);

                // === 1. BANDA VERTICAL DETRÁS (los ARCOS lenteados) ===
                // FireRing estirado VERTICALMENTE: lo que quede por encima
                // y por debajo de la esfera negra son los arcos de Gargantua.
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                Vector2 frOrigin = fireRing.Size() * 0.5f;
                float arcW = pupilR * 2.1f / fireRing.Width;          // ancho ceñido
                float arcH = (pupilR * 3.6f) / fireRing.Height;       // alto generoso
                byte arcA = (byte)(int)(120 + 90 * terror);
                Main.spriteBatch.Draw(fireRing, pupilPos, null,
                    new Color(diskCol.R, diskCol.G, diskCol.B, arcA), MathHelper.PiOver2,
                    frOrigin, new Vector2(arcW, arcH), SpriteEffects.None, 0f);

                // === 4-primera-mitad. BANDA HORIZONTAL DELANTE (la mitad
                //     trasera del disco, apagada, para densidad) ===
                float bandW = (pupilR * 3.4f) / fireRing.Width;
                float bandH = (pupilR * 1.15f) / fireRing.Height;
                Main.spriteBatch.Draw(fireRing, pupilPos, null,
                    new Color(diskCol.R, diskCol.G, diskCol.B, (byte)(int)(70 + 70 * terror)), 0f,
                    frOrigin, new Vector2(bandW, bandH), SpriteEffects.None, 0f);
                Main.spriteBatch.End();

                // === 2. LA ESFERA NEGRA (horizonte de sucesos) ===
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                // núcleo duro + falloff del borde (esfera SUAVE, no recorte)
                float coreScale = (pupilR * 2.02f) / softGlow.Width;
                Main.spriteBatch.Draw(softGlow, pupilPos, null,
                    new Color(4, 1, 3, 250), 0f, softGlow.Size() * 0.5f,
                    coreScale, SpriteEffects.None, 0f);
                // halo de absorción JUSTO fuera del horizonte (la luz se
                // atenúa al caer — un anillo oscuro fino alrededor)
                float absorbScale = (pupilR * 2.6f) / softGlow.Width;
                Main.spriteBatch.Draw(softGlow, pupilPos, null,
                    new Color(8, 2, 6, 120), 0f, softGlow.Size() * 0.5f,
                    absorbScale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();

                // === 3+4-final. ANILLO DE FOTONES + BANDA FRONTAL (aditivo) ===
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // anillo de fotones: fino, blanco-caliente, MICRO-pulso lento
                float phPulse = 0.92f + 0.08f * (float)Math.Sin(age * 0.13f);
                float phScale = (pupilR * 1.16f * phPulse) / (ringTex.Width * 0.5f * 0.95f);
                Main.spriteBatch.Draw(ringTex, pupilPos, null,
                    new Color(photonCol.R, photonCol.G, photonCol.B, 235), 0f,
                    ringTex.Size() * 0.5f, phScale, SpriteEffects.None, 0f);

                // la banda FRONTAL del disco cruzando la esfera (encima del
                // negro: el disco de acreción pasa POR DELANTE del horizonte)
                Main.spriteBatch.Draw(fireRing, pupilPos, null,
                    new Color(diskCol.R, diskCol.G, diskCol.B, (byte)(int)(150 + 80 * terror)), 0f,
                    frOrigin, new Vector2(bandW, bandH * 0.72f), SpriteEffects.None, 0f);

                // chispa del punto más caliente (donde el disco roza el
                // anillo de fotones — el "beaming" relativista)
                float sparkScale = (pupilR * 1.5f) / (softGlow.Width * 0.5f);
                Main.spriteBatch.Draw(softGlow,
                    pupilPos + new Vector2(pupilR * 0.55f, -pupilR * 0.28f), null,
                    new Color(255, 236, 190, (byte)(int)(60 + 60 * terror)), 0f,
                    softGlow.Size() * 0.5f, sparkScale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
        }

        /// <summary>Restaura el SpriteBatch al estado que tML espera tras PreDraw.</summary>
        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
