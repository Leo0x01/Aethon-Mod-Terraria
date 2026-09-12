using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Effects;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// LivingGalaxyProjectile — LA GALAXIA VIVIENTE (arma nueva v6.00).
    ///
    /// Petición del usuario: "borra el ojo, se ve feo, mejor crea un arma
    /// nueva con un proyectil cosmico, este debe ser una galaxia, investiga
    /// galaxias en internet". EL OJO DEL VACÍO fue ELIMINADO — su hueco lo
    /// ocupa ESTO: una GALAXIA ESPIRAL DE DISEÑO PERFECTO (grand-design,
    /// como M51/M101 según la investigación: bulbo AMARILLO de estrellas
    /// viejas + brazos AZULES de estrellas jóvenes + nudos ROSAS HII +
    /// carriles de POLVO oscuro — SpiralGalaxy.png 512 PIL).
    ///
    /// EL PROYECTIL ES LA GALAXIA (~9 s de vida):
    ///   - NACE con pop elástico, VUELA donde la lanzas y ahí SE ESTACIONA
    ///     (deceleración), orbitando LENTAMENTE su ancla
    ///   - El DISCO GIRA (0.02 rad/t) y CABECEA EN 3D: la escala Y oscila
    ///     0.55→1.0 — la moneda espacial girando de canto a cara
    ///   - GRAVEDAD DE DISCO: arrastra suavemente a los enemigos (radio 300)
    ///   - AURA ESTELAR: daño de área cada 10 ticks (45%) dentro del disco
    ///   - SEMBRADO ESTELAR: cada 24 ticks los BRAZOS sueltan 2 estrellas
    ///     (GalaxyStarProjectile) tangencialmente — la galaxia SIEMBRA
    ///     estrellas al girar, como un rociador cósmico
    ///   - ACECHA: el ancla deriva hacia el enemigo más cercano (0.7 px/t)
    ///   - LA EXPLOSIÓN ESTELLAR (OnKill): 14 estrellas radiales + AoE del
    ///     90% + destello — la galaxia se dispara en polvo de estrellas
    ///   - LENTE: el fondo se curva sutilmente a su alrededor (pase B,
    ///     fuerza respirando con el giro — masa de cien mil millones de soles)
    ///
    /// Dibujada ENCIMA de la lente (protocolo del arsenal: PreDraw devuelve
    /// false cuando LensActive y el BlackHoleLensSystem llama a
    /// DrawGalaxyVisuals). Campos: ai[0] = edad · ai[1] = semilla ·
    /// ai[2] = fase (0 vuelo / 1 estacionada) · localAI[0]/[1] = ancla.
    /// </summary>
    public class LivingGalaxyProjectile : ModProjectile
    {
        private const int GalaxyLifetime = 540;    // ~9 s
        private const float DiskVisualRadius = 118f; // radio del disco a escala 1
        private const float GravityRadius = 300f;
        private const float GravityStrength = 0.4f;
        private const int StarSeedInterval = 24;   // cada 24 ticks: 2 estrellas
        private const int AuraInterval = 10;       // cada 10 ticks: daño de área

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 140;
            Projectile.height = 140;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = GalaxyLifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.light = 1.0f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            float age = Projectile.ai[0];
            Projectile.ai[0] = age + 1f;
            if (Projectile.ai[1] == 0f)
                Projectile.ai[1] = Main.rand.Next(1, 999999);
            int seed = (int)Projectile.ai[1];

            // === POP ELÁSTICO DE NACIMIENTO ===
            Projectile.scale = ElasticOut(Utils.GetLerpValue(0f, 40f, age, true));

            // === EL GIRO DEL DISCO (los brazos barren) ===
            Projectile.rotation += 0.02f;

            bool parked = Projectile.ai[2] > 0f;

            if (!parked)
            {
                // === FASE VUELO: hacia donde la lanzaron, frenando ===
                Projectile.velocity *= 0.94f;
                if (Projectile.velocity.LengthSquared() < 6.5f || age > 60f)
                {
                    // SE ESTACIONA: ancla clavada donde cayó
                    Projectile.ai[2] = 1f;
                    Projectile.localAI[0] = Projectile.Center.X;
                    Projectile.localAI[1] = Projectile.Center.Y;
                    Projectile.velocity = Vector2.Zero;
                }
            }
            else
            {
                // === FASE ESTACIONADA: orbita su ancla LENTAMENTE ===
                Vector2 anchor = new Vector2(Projectile.localAI[0], Projectile.localAI[1]);

                // ACECHA: el ancla deriva hacia el enemigo más cercano — la
                // galaxia flota SILENCIOSAMENTE hacia donde está la materia.
                NPC prey = FindNearestEnemy(560f);
                if (prey != null)
                {
                    Vector2 toPrey = prey.Center - anchor;
                    if (toPrey.LengthSquared() > 100f)
                    {
                        toPrey.Normalize();
                        anchor += toPrey * 0.7f;
                        Projectile.localAI[0] = anchor.X;
                        Projectile.localAI[1] = anchor.Y;
                    }
                }

                float orbitAngle = age * 0.02f + seed * 0.001f;
                Vector2 orbit = new Vector2(
                    (float)Math.Cos(orbitAngle), (float)Math.Sin(orbitAngle) * 0.6f) * 22f;
                Vector2 target = anchor + orbit;
                // persecución SUAVE del punto de órbita (nunca teletransporta)
                Vector2 toTarget = target - Projectile.Center;
                if (toTarget.LengthSquared() > 0.25f)
                    Projectile.Center += toTarget * 0.08f;
            }

            // === GRAVEDAD DEL DISCO: arrastra a los enemigos ===
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                Vector2 toCenter = Projectile.Center - npc.Center;
                float dist = toCenter.Length();
                if (dist > GravityRadius || dist < 5f) continue;
                float strength = (1f - dist / GravityRadius) * GravityStrength;
                if (toCenter.LengthSquared() > 0.01f)
                {
                    toCenter.Normalize();
                    npc.velocity += toCenter * strength;
                }
            }

            // === AURA ESTELAR: daño de área dentro del disco ===
            if (Main.netMode != NetmodeID.MultiplayerClient &&
                age > 12f && age % AuraInterval == 0f)
            {
                float auraRadius = GetGalaxyVisualRadius(Projectile) * 0.95f;
                int auraDamage = Math.Max(1, (int)(Projectile.damage * 0.45f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist > auraRadius) continue;
                    npc.SimpleStrikeNPC(auraDamage, npc.direction, false, 1.5f, DamageClass.Magic);
                }
            }

            // === SEMBRADO ESTELLAR: los brazos sueltan estrellas ===
            if (parked && Projectile.owner == Main.myPlayer &&
                age > 30f && age % StarSeedInterval == 0f)
            {
                float diskR = GetGalaxyVisualRadius(Projectile);
                int starDamage = Math.Max(1, (int)(Projectile.damage * 0.6f));
                for (int k = 0; k < 2; k++)
                {
                    // la punta de cada brazo (los brazos van opuestos)
                    float armAngle = Projectile.rotation + k * MathHelper.Pi;
                    Vector2 dir = new Vector2((float)Math.Cos(armAngle), (float)Math.Sin(armAngle));
                    Vector2 spawnPos = Projectile.Center + dir * (diskR * 0.55f);
                    // TANGENCIAL: la estrella hereda el giro del disco
                    Vector2 tangent = new Vector2(-dir.Y, dir.X);
                    Vector2 vel = tangent * 9.5f + dir * 2.5f;
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(), spawnPos, vel,
                        ModContent.ProjectileType<GalaxyStarProjectile>(),
                        starDamage, 1.2f, Projectile.owner,
                        (age % 48f < 24f) ? k : k + 2);   // ai[0]: índice de color
                }
            }

            // === POLVO ESTELAR AMBIENTAL (la galaxia pierde materia) ===
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(6))
            {
                float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float rr = GetGalaxyVisualRadius(Projectile) * Main.rand.NextFloat(0.35f, 0.95f);
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + new Vector2(
                        (float)Math.Cos(ang), (float)Math.Sin(ang)) * rr,
                    DustID.BlueTorch,
                    new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(-0.5f, -0.1f)),
                    150, Main.rand.Next(4) switch
                    {
                        0 => new Color(255, 230, 170),   // oro del bulbo
                        1 => new Color(255, 170, 215),   // HII rosa
                        _ => new Color(170, 215, 255),   // azul de los brazos
                    }, 0.45f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // === LUZ (el núcleo ilumina la escena) ===
            Lighting.AddLight(Projectile.Center, new Vector3(1.05f, 1.0f, 1.25f));
        }

        public override void OnKill(int timeLeft)
        {
            // === LA EXPLOSIÓN ESTELLAR ===
            // La galaxia se dispara en polvo de estrellas: 14 estrellas
            // radiales + AoE del 90% + destello. (Nada de ondas de choque:
            // el sol tiene SU nova y el agujero SU anillo — la galaxia
            // estalla en SEMILLAS, su firma propia.)
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                float diskR = GetGalaxyVisualRadius(Projectile);
                int aoeDamage = Math.Max(1, (int)(Projectile.damage * 0.9f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float dist = (npc.Center - Projectile.Center).Length();
                    if (dist > diskR * 1.1f) continue;
                    npc.SimpleStrikeNPC(aoeDamage, npc.direction, false, 3f, DamageClass.Magic);
                }
            }

            if (Projectile.owner == Main.myPlayer)
            {
                int starDamage = Math.Max(1, (int)(Projectile.damage * 0.65f));
                for (int i = 0; i < 14; i++)
                {
                    float ang = i * MathHelper.TwoPi / 14f +
                                Main.rand.NextFloat(-0.12f, 0.12f);
                    float spd = Main.rand.NextFloat(9f, 14f);
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * 20f,
                        new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * spd,
                        ModContent.ProjectileType<GalaxyStarProjectile>(),
                        starDamage, 1.5f, Projectile.owner,
                        i % 3);                            // ai[0]: índice de color
                }
            }

            if (Main.netMode == NetmodeID.Server) return;

            try
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item92, Projectile.Center);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            }
            catch { }

            for (int i = 0; i < 34; i++)
            {
                float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float spd = Main.rand.NextFloat(2f, 7f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * spd,
                    230, Main.rand.Next(3) switch
                    {
                        0 => new Color(255, 230, 170),
                        1 => new Color(255, 170, 215),
                        _ => new Color(180, 220, 255),
                    }, 0.8f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        // ================================================================
        //  RENDER — el disco espiral girando y cabeceando en 3D
        // ================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            if (BlackHoleLensSystem.LensActive)
                return false;

            if (DrawGalaxyVisuals(Projectile, true))
                RestoreSpriteBatch();
            return false;
        }

        /// <summary>
        /// Dibuja la galaxia completa (protocolo del arsenal: devuelve true
        /// si tomó el spriteBatch y lo dejó CERRADO). Capas:
        ///   1. HALO aditivo (el disco difuso que envuelve — respira)
        ///   2. EL DISCO ESPIRAL (AlphaBlend: el polvo oscuro NO puede ser
        ///      aditivo) — girando + CABECEO 3D (escala Y 0.55→1.0)
        ///   3. EL BULBO (glow dorado pulsante + núcleo blanco)
        ///   4. CHISPAS DE LOS BRAZOS (estrellas brillantes rotando + titileo)
        /// </summary>
        internal static bool DrawGalaxyVisuals(Projectile p, bool endActiveBatch)
        {
            try
            {
                if (p == null || !p.active) return false;

                float age = p.ai[0];
                int seed = (int)p.ai[1];
                float diskR = GetGalaxyVisualRadius(p);
                float spin = p.rotation;
                Vector2 drawPos = p.Center - Main.screenPosition;

                Texture2D diskTex = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SpiralGalaxy").Value;
                Texture2D softGlow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                Texture2D star = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/Star").Value;

                if (endActiveBatch)
                    Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // === 1. EL HALO (respira lentamente) ===
                float breath = 0.85f + 0.15f * (float)Math.Sin(age * 0.035f);
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(90, 105, 175, (byte)(70 * breath)), 0f,
                    softGlow.Size() * 0.5f,
                    (diskR * 2.15f * breath) / (softGlow.Width * 0.5f),
                    SpriteEffects.None, 0f);

                // === 2. EL DISCO (AlphaBlend — el polvo oscuro lo exige) ===
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // CABECEO 3D: la escala Y oscila 0.55→1.0 (la moneda espacial
                // girando de canto a cara) — con la rotación del disco encima
                // el eje menor también gira: la galaxia TROTA en el espacio.
                float squash = 0.55f + 0.45f *
                    (0.5f + 0.5f * (float)Math.Sin(age * 0.011f + seed * 0.01f));
                float diskScale = (diskR * 2f) / diskTex.Width;
                Main.spriteBatch.Draw(diskTex, drawPos, null,
                    Color.White * 0.97f, spin,
                    diskTex.Size() * 0.5f,
                    new Vector2(diskScale, diskScale * squash),
                    SpriteEffects.None, 0f);

                // eco tenue desplazado 180° (la "imagen secundaria" de la lente
                // de la propia galaxia — profundo, apenas ahí)
                Main.spriteBatch.Draw(diskTex, drawPos, null,
                    new Color(120, 140, 220, 40), spin + MathHelper.Pi,
                    diskTex.Size() * 0.5f,
                    new Vector2(diskScale * 0.45f, diskScale * 0.45f * squash),
                    SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // === 3. EL BULBO (glow dorado pulsante + núcleo blanco) ===
                float corePulse = 0.9f + 0.1f * (float)Math.Sin(age * 0.09f);
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(255, 232, 185, (byte)(185 * corePulse)), 0f,
                    softGlow.Size() * 0.5f,
                    (diskR * 0.42f * corePulse) / (softGlow.Width * 0.5f),
                    SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(255, 250, 240, (byte)(235 * corePulse)), 0f,
                    softGlow.Size() * 0.5f,
                    (diskR * 0.16f * corePulse) / (softGlow.Width * 0.5f),
                    SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(star, drawPos, null,
                    new Color(255, 255, 252, 235), spin * 1.5f,
                    star.Size() * 0.5f,
                    (diskR * 0.14f) / (star.Width * 0.5f),
                    SpriteEffects.None, 0f);

                // === 4. CHISPAS DE LOS BRAZOS (titileo determinista) ===
                for (int k = 0; k < 5; k++)
                {
                    float armAngle = spin + (k / 5f) * MathHelper.TwoPi;
                    float rr = diskR * (0.42f + 0.45f * ((k * 37 + seed) % 100) / 100f);
                    Vector2 sp = drawPos + new Vector2(
                        (float)Math.Cos(armAngle), (float)Math.Sin(armAngle) * squash) * rr;
                    // titileo: hash de (semilla, brazo, fase lenta)
                    int phase = (int)(age / 16f) + k;
                    float tw = Hash01(seed, k, phase);
                    float bright = 0.35f + 0.65f * tw;
                    Color sc = (k % 3) switch
                    {
                        0 => new Color(185, 225, 255),   // azul
                        1 => new Color(255, 235, 185),   // oro
                        _ => new Color(255, 190, 225),   // HII rosa
                    };
                    Main.spriteBatch.Draw(star, sp, null,
                        new Color(sc.R, sc.G, sc.B, (byte)(int)(200 * bright)),
                        armAngle + (float)Math.Sin(age * 0.05f + k) * 0.4f,
                        star.Size() * 0.5f,
                        ((6f + 4f * tw) * bright + 3f) / (star.Width * 0.5f),
                        SpriteEffects.None, 0f);
                }

                Main.spriteBatch.End();
                return true; // batch tomado y dejado CERRADO
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
                return true;
            }
        }

        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        // ================================================================
        //  HELPERS (protocolo del arsenal — los usa el LensSystem)
        // ================================================================

        /// <summary>Radio visual del disco (px).</summary>
        internal static float GetGalaxyVisualRadius(Projectile p)
        {
            return p.scale * DiskVisualRadius;
        }

        /// <summary>
        /// Fuerza de la lente (pase B): RESPIRA con el giro del disco —
        /// una masa de cientos de miles de soles girando: donde flota, el
        /// fondo se dobla apenas (0.07→0.12).
        /// </summary>
        internal static float GetGalaxyLensStrength(Projectile p)
        {
            float spinPhase = 0.5f + 0.5f * (float)Math.Sin(p.rotation * 3f);
            return 0.07f + 0.05f * spinPhase;
        }

        private NPC FindNearestEnemy(float maxDist)
        {
            NPC best = null;
            float bestDist = maxDist * maxDist;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                float d2 = (npc.Center - Projectile.Center).LengthSquared();
                if (d2 < bestDist)
                {
                    bestDist = d2;
                    best = npc;
                }
            }
            return best;
        }

        private static float ElasticOut(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float c = (2f * MathHelper.Pi) / 3f;
            return (float)(Math.Pow(2f, -9f * t) * Math.Sin((t * 10f - 0.75f) * c) + 1.0);
        }

        /// <summary>Hash determinista [0,1) — el MISMO titileo en todas las máquinas.</summary>
        private static float Hash01(int seed, int k, int phase)
        {
            float h = (float)Math.Abs(Math.Sin(
                seed * 12.9898f + k * 78.233f + phase * 37.719f) * 43758.5453f);
            return h - (float)Math.Floor(h);
        }
    }
}
