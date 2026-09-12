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
    /// v5.99 — EL COMETA ESTELAR (minion invocador — el proyectil ES la
    /// invocación). Petición del usuario: "usando la segunda imagen como
    /// referencia, crea un minion cosmico con efectos, ya sabes de la misma
    /// forma a como creaste la medusa" (la referencia: una criatura-estrella
    /// de 8 puntas con núcleo cálido y cuerpo frío, envuelta en partículas —
    /// el CosmicOrbMinion existente usa ESA imagen; este es una criatura
    /// ORIGINAL hermana: un cometa VIVO).
    ///
    /// LA CRIATURA: un cometa vivo — núcleo de plasma blanco-oro con
    /// granulación (CometHead) envuelto en una CORONA DE 8 PUNTAS lanceoladas
    /// cian→violeta (CometCrown — el homenaje a la estrella de la referencia)
    /// que gira lentísima alrededor del núcleo, con COLA de polvo estelar
    /// que ondea a su estela (historial de posiciones, cálida cerca del
    /// núcleo → fría en la punta) y CHISPAS orbitando (el campo de
    /// partículas de la referencia).
    ///
    /// LOCOMOCIÓN — UNA ÓRBITA DE VERDAD: en reposo no flota ni persigue:
    /// ORBITA al jugador en una elipse excéntrica (apoapsis lejos, periapsis
    /// cerca — como un cometa de verdad). Para atacar hace lo que hace un
    /// cometa en periapsis: CAE EN PICADO sobre la víctima (aceleración
    /// continua, cola ardiendo) y al rozarla ESTALLA en una pequeña nova
    /// (AoE + quemadura OnFire — materia estelar CALIENTE, la firma opuesta
    /// a la medusa) y vuelve a subir a su órbita. Un ciclo: órbita → picado
    /// → nova → órbita.
    ///
    /// EL ESPACIOTIEMPO: fuente sutil del pase B del BlackHoleLensSystem —
    /// curva MÁS cuando cae en picado (velocidad = momento = curvatura).
    ///
    /// Ciclo de vida del sirviente: patrón CosmicOrb (buff StellarCometBuff
    /// sostenido por el propio cometa; muere sin él).
    /// Campos: ai[0]=edad · ai[1]=estado (0=órbita, 1=picado) ·
    /// ai[2]=enfriamiento del picado. Todo determinista de la edad.
    /// </summary>
    public class StellarCometMinion : ModProjectile
    {
        // === ÓRBITA ===
        private const float OrbitA = 118f;      // semi-eje mayor (apoapsis)
        private const float OrbitB = 54f;       // semi-eje menor (periapsis)
        private const float OrbitOmega = 0.016f; // velocidad angular (rad/t)

        // === EL PICADO ===
        private const float DiveAccel = 0.46f;
        private const float DiveMaxSpeed = 16.5f;
        private const float DiveTriggerRange = 520f;  // dispara si la víctima está a < esto
        private const float NovaRadius = 92f;         // radio de la nova de impacto
        private const int DiveCooldown = 70;

        // --- Estado visual local (solo cliente) ---
        private Vector2[] _trail;
        private Vector2 _novaPos;
        private int _novaTimer;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18000;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
            Projectile.aiStyle = -1;
            Projectile.light = 0.7f;
        }

        public override bool? CanCutTiles() => false;
        public override bool MinionContactDamage() => true;

        // ================================================================
        //  HELPERS (lente + dibujado compartido)
        // ================================================================

        /// <summary>Radio visual del núcleo (px).</summary>
        internal static float GetCometVisualRadius(Projectile p)
        {
            return p.width * p.scale * 0.24f;
        }

        /// <summary>
        /// Fuerza de lente (pase B): 0.05 en órbita, hasta ~0.17 en picado
        /// (la velocidad ES momento: el cometa curva más cuando cae).
        /// </summary>
        internal static float GetCometLensStrength(Projectile p)
        {
            float spd = p.velocity.Length();
            return 0.05f + MathHelper.Clamp(spd / DiveMaxSpeed, 0f, 1f) * 0.12f;
        }

        // ================================================================
        //  AI — órbita → picado → nova → órbita
        // ================================================================

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (owner == null || !owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }
            CheckMinionBuff(owner);

            float age = Projectile.ai[0];
            Projectile.ai[0] = age + 1f;
            if (Projectile.ai[2] > 0f) Projectile.ai[2] -= 1f;

            NPC target = FindTarget(owner);
            bool diving = Projectile.ai[1] == 1f;

            if (!diving)
            {
                // === LA ÓRBITA (elipse excéntrica alrededor del jugador) ===
                float phase = age * OrbitOmega + Projectile.minionPos * 1.9f;
                Vector2 orbitPoint = owner.Center + new Vector2(
                    (float)Math.Cos(phase) * OrbitA,
                    (float)Math.Sin(phase) * OrbitB - 40f);
                // giro suave hacia el punto orbital (resorte amortiguado)
                Vector2 toPoint = orbitPoint - Projectile.Center;
                Projectile.velocity += toPoint * 0.018f;
                Projectile.velocity *= 0.93f;

                // === DISPARO DEL PICADO: víctima en rango y frío a cero ===
                if (target != null && Projectile.ai[2] <= 0f &&
                    Vector2.Distance(target.Center, Projectile.Center) < DiveTriggerRange)
                {
                    Projectile.ai[1] = 1f;
                }
            }
            else
            {
                // === EL PICADO: cae sobre la víctima acelerando sin freno ===
                if (target == null)
                {
                    // la víctima murió a mitad del picado → volver a la órbita
                    Projectile.ai[1] = 0f;
                    Projectile.ai[2] = 24f;
                }
                else
                {
                    Vector2 to = target.Center - Projectile.Center;
                    float dist = to.Length();
                    to /= Math.Max(dist, 0.001f);
                    Projectile.velocity += to * DiveAccel;
                    Projectile.velocity *= 0.995f;
                    float spd = Projectile.velocity.Length();
                    if (spd > DiveMaxSpeed)
                        Projectile.velocity *= DiveMaxSpeed / spd;

                    // === LA NOVA DEL PERIAPSIS: rozando la víctima ===
                    if (dist < 48f)
                    {
                        CometNova(target.Center);
                        Projectile.ai[1] = 0f;
                        Projectile.ai[2] = DiveCooldown;
                        // rebote: la nova lo impulsa de vuelta a la órbita
                        Projectile.velocity = -to * 9f;
                    }
                }
            }

            // === NO PERDERSE ===
            if (Vector2.Distance(Projectile.Center, owner.Center) > 1300f)
            {
                Projectile.Center = owner.Center - Vector2.UnitY * 60f;
                Projectile.velocity *= 0.2f;
                _trail = null;
            }

            // === SIMULACIÓN VISUAL (solo cliente) ===
            if (Main.netMode != NetmodeID.Server)
            {
                UpdateTrail();
                if (_novaTimer > 0) _novaTimer--;

                // chispas orbitando (el campo de partículas de la referencia)
                if (Main.rand.NextBool(4))
                {
                    float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float rr = GetCometVisualRadius(Projectile) * 2.4f;
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * rr,
                        DustID.BlueTorch,
                        -Projectile.velocity * 0.08f + new Vector2(
                            (float)Math.Cos(ang + MathHelper.PiOver2),
                            (float)Math.Sin(ang + MathHelper.PiOver2)) * 0.7f,
                        140, Main.rand.NextBool(2)
                            ? new Color(255, 214, 140)   // oro
                            : new Color(190, 140, 255),   // violeta
                        0.45f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // === LUZ (cálida, más viva en el picado) ===
            float diveGlow = diving ? 1f : 0f;
            Lighting.AddLight(Projectile.Center,
                new Vector3(0.85f, 0.72f, 0.5f) * (0.7f + 0.5f * diveGlow));
        }

        /// <summary>LA NOVA DEL PERIAPSIS: pequeño estallido + AoE + OnFire.</summary>
        private void CometNova(Vector2 at)
        {
            _novaPos = at;
            _novaTimer = 22;

            // AoE: materia estelar caliente (SimpleStrikeNPC, patrón del sol)
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int novaDamage = Math.Max(1, (int)(Projectile.damage * 0.6f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy()) continue;
                    float dist = (npc.Center - at).Length();
                    if (dist > NovaRadius) continue;
                    npc.SimpleStrikeNPC(novaDamage, npc.direction, false, 3f, DamageClass.Summon);
                    npc.AddBuff(BuffID.OnFire, 240);
                }
            }

            if (Main.netMode != NetmodeID.Server)
            {
                try { Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, at); } catch { }
                for (int i = 0; i < 18; i++)
                {
                    float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    Dust d = Dust.NewDustPerfect(at, DustID.Torch,
                        new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) *
                            Main.rand.NextFloat(1.5f, 4.5f),
                        220, Main.rand.NextBool(2)
                            ? new Color(255, 210, 130)
                            : new Color(170, 190, 255), 0.7f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
        }

        // ================================================================
        //  COLA — historial de posiciones
        // ================================================================

        private void UpdateTrail()
        {
            const int TrailLen = 18;
            if (_trail == null)
            {
                _trail = new Vector2[TrailLen];
                for (int i = 0; i < TrailLen; i++)
                    _trail[i] = Projectile.Center;
                return;
            }
            for (int i = TrailLen - 1; i > 0; i--)
                _trail[i] = _trail[i - 1];
            _trail[0] = Projectile.Center;
        }

        // ================================================================
        //  OBJETIVO + BUFF (patrón del arsenal/CosmicOrb)
        // ================================================================

        private NPC FindTarget(Player owner)
        {
            if (owner.HasMinionAttackTargetNPC)
            {
                NPC marked = Main.npc[owner.MinionAttackTargetNPC];
                if (marked != null && marked.active && marked.CanBeChasedBy())
                    return marked;
            }
            NPC closest = null;
            float closestDist = 700f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.friendly || npc.townNPC) continue;
                if (npc.dontTakeDamage || npc.immortal) continue;
                if (!npc.CanBeChasedBy()) continue;
                float dist = Vector2.Distance(npc.Center, Projectile.Center);
                if (dist < closestDist) { closestDist = dist; closest = npc; }
            }
            return closest;
        }

        private void CheckMinionBuff(Player owner)
        {
            int buffType = ModContent.BuffType<global::AethonMod.Content.Buffs.StellarCometBuff>();
            bool hasBuff = false;
            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                if (owner.buffType[i] == buffType && owner.buffTime[i] > 0)
                { hasBuff = true; break; }
            }
            if (!hasBuff) Projectile.Kill();
            else
            {
                owner.AddBuff(buffType, 18000);
                Projectile.timeLeft = 2;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // materia estelar CALIENTE (la firma opuesta a la medusa)
            try { target.AddBuff(BuffID.OnFire, 200); } catch { }
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 4; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                    new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f)),
                    200, new Color(255, 200, 120), 0.5f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        public override void OnKill(int timeLeft)
        {
            // se disuelve en polvo de estrellas doradas y frías
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 22; i++)
            {
                float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * Main.rand.NextFloat(4f, 26f),
                    Main.rand.NextBool(2) ? DustID.Torch : DustID.BlueTorch,
                    new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * Main.rand.NextFloat(0.6f, 2.4f),
                    200, Main.rand.NextBool(2)
                        ? new Color(255, 210, 140)
                        : new Color(160, 200, 255), 0.75f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        // ================================================================
        //  RENDER — el cometa entero (reutilizable: mundo + lente)
        // ================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            if (BlackHoleLensSystem.LensActive)
                return false;

            if (DrawCometVisuals(Projectile, true))
                RestoreSpriteBatch();
            return false;
        }

        /// <summary>
        /// Dibuja el cometa completo (protocolo del arsenal: devuelve true si
        /// tomó el spriteBatch y lo dejó CERRADO). Capas:
        ///   1. COLA (historial de posiciones: cálida cerca → fría lejos)
        ///   2. CORONA de 8 puntas GIRANDO (más rápido en picado)
        ///   3. NÚCLEO de plasma (pulso sutil)
        ///   4. CHISPAS orbitando (campo de partículas de la referencia)
        ///   5. NOVA del periapsis (flash + anillo si _novaTimer > 0)
        /// </summary>
        internal static bool DrawCometVisuals(Projectile p, bool endActiveBatch)
        {
            try
            {
                if (p == null || !p.active) return false;
                var mp = p.ModProjectile as StellarCometMinion;
                if (mp == null) return false;

                float age = p.ai[0];
                bool diving = p.ai[1] == 1f;
                float nucleusR = GetCometVisualRadius(p);
                float crownR = nucleusR * 2.55f;
                Vector2 drawPos = p.Center - Main.screenPosition;

                Texture2D headTex = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/CometHead").Value;
                Texture2D crownTex = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/CometCrown").Value;
                Texture2D softGlow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                Texture2D star = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/Star").Value;

                if (endActiveBatch)
                    Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // === 1. LA COLA (polvo estelar a la estela) ===
                if (mp._trail != null)
                {
                    for (int i = mp._trail.Length - 1; i >= 1; i--)
                    {
                        float t = i / (float)(mp._trail.Length - 1); // 0=nuevo,1=viejo
                        Vector2 tp = mp._trail[i] - Main.screenPosition;
                        float size = MathHelper.Lerp(nucleusR * 0.85f, nucleusR * 0.12f, t);
                        // cálida cerca del núcleo → fría en la punta
                        Color col = Color.Lerp(
                            new Color(255, 214, 140), new Color(120, 150, 255), t);
                        Main.spriteBatch.Draw(softGlow, tp, null,
                            new Color(col.R, col.G, col.B, (byte)(int)(150 * (1f - t * 0.75f))),
                            0f, softGlow.Size() * 0.5f,
                            size / (softGlow.Width * 0.5f), SpriteEffects.None, 0f);
                    }
                }

                // === 2. LA CORONA de 8 puntas (gira; en picado GIRA MÁS) ===
                float spin = age * (0.02f + (diving ? 0.045f : 0f));
                float crownScale = crownR / (crownTex.Width * 0.5f);
                Main.spriteBatch.Draw(crownTex, drawPos, null,
                    Color.White * (diving ? 1f : 0.88f), spin,
                    crownTex.Size() * 0.5f, crownScale, SpriteEffects.None, 0f);

                Main.spriteBatch.End();

                // === 3. EL NÚCLEO (AlphaBlend: cuerpo sólido de plasma) ===
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                float pulse = 1f + 0.05f * (float)Math.Sin(age * 0.11f);
                float headScale = nucleusR * pulse / (headTex.Width * 0.5f);
                Main.spriteBatch.Draw(headTex, drawPos, null, Color.White, 0f,
                    headTex.Size() * 0.5f, headScale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();

                // === 4. CHISPAS orbitando (aditivo) ===
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                for (int i = 0; i < 5; i++)
                {
                    float a = age * 0.07f + i * MathHelper.TwoPi / 5f;
                    float rr = crownR * (1.02f + 0.1f * (float)Math.Sin(age * 0.05f + i * 2f));
                    Vector2 sp = drawPos + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * rr;
                    Color sc = i % 2 == 0 ? new Color(255, 220, 150) : new Color(190, 150, 255);
                    Main.spriteBatch.Draw(star, sp, null,
                        new Color(sc.R, sc.G, sc.B, 170), a,
                        star.Size() * 0.5f, (nucleusR * 0.34f) / (star.Width * 0.5f),
                        SpriteEffects.None, 0f);
                }

                // === 5. LA NOVA DEL PERIAPSIS (flash + anillo expandiéndose) ===
                if (mp._novaTimer > 0)
                {
                    float nt = 1f - mp._novaTimer / 22f;   // 0→1
                    float flash = (1f - nt) * (1f - nt);   // fuerte al inicio
                    Vector2 np = mp._novaPos - Main.screenPosition;
                    Main.spriteBatch.Draw(softGlow, np, null,
                        new Color(255, 235, 190, (byte)(int)(190 * flash)), 0f,
                        softGlow.Size() * 0.5f,
                        (NovaRadius * (0.3f + nt) * flash + 8f) / (softGlow.Width * 0.5f),
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
    }
}
