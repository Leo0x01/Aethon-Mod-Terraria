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
    /// WhiteDwarfProjectile — v6.26 — LA ENANA BLANCA.
    ///
    /// El rescoldo cristalino: pequeño, denso, paciente.
    ///
    /// FÍSICA:
    ///   · Daño CONSTANTE MODERADO: aura del 45% (el estándar de la
    ///     casa) cada 12 ticks en 90 px — sin picos, sin furia: la
    ///     paciencia de un rescoldo.
    ///   · DERIVA LENTA (freno 0.985, persecución mínima): la enana
    ///     no persigue — CAMINA por el campo.
    ///   · EL ANILLO DE ACRECIÓN: si un enemigo pasa a <150 px, el
    ///     disco se enciende y las MOTAS DE LEECH fluyen del enemigo
    ///     a la estrella (visual determinista del renderer + un +15%
    ///     de daño del aura mientras acrece — "les roba masa").
    ///   · Vida 8 s (la más longeva de la familia compacta).
    ///
    /// Daño MP-seguro: SimpleStrikeNPC con Main.netMode != MultiplayerClient.
    /// Visual solo cliente (Main.netMode == Server → return).
    /// </summary>
    public class WhiteDwarfProjectile : ModProjectile
    {
        /// <summary>Vida total en ticks (8 s).</summary>
        private const int Lifetime = 480;

        /// <summary>Radio del aura de daño (constante y moderada).</summary>
        private const float AuraRadius = 90f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 55;
            Projectile.height = 55;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // ai[1] = semilla determinista del disparo (visual).
            if (Projectile.ai[1] <= 0f)
                Projectile.ai[1] = (Projectile.identity % 9973 + 1) * 1f;
        }

        /// <summary>Edad en ticks (ai[0] — determinista, sincronizado).</summary>
        private float Age => Projectile.ai[0];

        /// <summary>Semilla determinista del disparo.</summary>
        private int Seed => Math.Max(1, (int)Projectile.ai[1]) + Projectile.identity;

        /// <summary>0..1 del ciclo de vida.</summary>
        private float LifeT => MathHelper.Clamp(Age / Lifetime, 0f, 1f);

        public override void AI()
        {
            Projectile.ai[0] += 1f;

            // === EL POP ELÁSTICO DE APARICIÓN (tranquilo: cristaliza) ===
            Projectile.scale = ElasticOut(Utils.GetLerpValue(0f, 55f, Age, true));

            // === LA DERIVA LENTA: la enana CAMINA, no persigue ===
            Projectile.velocity *= 0.985f;
            NPC prey = FindNearestEnemy(420f);
            if (prey != null)
            {
                Vector2 toPrey = prey.Center - Projectile.Center;
                float len = toPrey.Length();
                if (len > 80f && len > 0.01f)
                    Projectile.velocity += toPrey / len * 0.25f;
                if (Projectile.velocity.Length() > 1.5f)
                    Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 1.5f;
            }

            // === ¿ESTÁ ACRECIENDO? (un enemigo a <150 px enciende el disco) ===
            NPC accretionTarget = FindNearestEnemy(WhiteDwarfRenderer.AccretionRange);

            // === EL AURA CONSTANTE MODERADA (45%, cada 12 ticks, 90 px) ===
            if (Main.netMode != NetmodeID.MultiplayerClient &&
                Age > 24f && Age % 12f == 0f && Projectile.scale > 0.25f)
            {
                // Mientras ACRECE, el aura sube +15% (la masa robada alimenta).
                float mult = accretionTarget != null ? 0.52f : 0.45f;
                int auraDamage = Math.Max(1, (int)(Projectile.damage * mult));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    if ((npc.Center - Projectile.Center).Length() > AuraRadius) continue;
                    npc.SimpleStrikeNPC(auraDamage, npc.direction, false, 1f, DamageClass.Magic);
                    try { npc.AddBuff(BuffID.OnFire, 240); } catch { }   // v6.30: TODO SOL QUEMA — el rescoldo cristalino
                }
            }

            // === LA LUZ: fulgor frío ESTABLE (blanco-azul de enana) ===
            float lightR = 0.62f * Projectile.scale;
            Lighting.AddLight(Projectile.Center, 0.75f * lightR, 0.85f * lightR, 1.05f * lightR);

            // === VISUAL SOLO CLIENTE: el polvo de la acreción ===
            if (Main.netMode == NetmodeID.Server) return;
            if (accretionTarget != null && Age % 6f == 0f)
            {
                // MOTAS físicas cayendo del enemigo a la estrella (el leech
                // que también dibuja el renderer — aquí con polvo de mundo).
                Vector2 from = accretionTarget.Center +
                    new Vector2(Main.rand.NextFloat(-10f, 10f), Main.rand.NextFloat(-10f, 10f));
                Vector2 dir = Projectile.Center - from;
                if (dir.Length() > 4f)
                {
                    Dust d = Dust.NewDustPerfect(from, DustID.BlueTorch,
                        Vector2.Normalize(dir) * Main.rand.NextFloat(2.5f, 5f),
                        150, new Color(200, 230, 255), 0.55f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
        }

        private NPC FindNearestEnemy(float maxRange)
        {
            NPC best = null;
            float bestDist = maxRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float dist = (npc.Center - Projectile.Center).Length();
                if (dist < bestDist) { bestDist = dist; best = npc; }
            }
            return best;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // ============================================================
            //  CONTRATO DE BATCH A PRUEBA DE BALAS (v6.10): durante PreDraw
            //  el batch de tML está ABIERTO; cerrarlo antes del pase propio.
            // ============================================================
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                // La presa de la acreción (visual determinista del leech):
                // releerla aquí mantiene el render SIN estado compartido.
                NPC prey = FindNearestEnemy(WhiteDwarfRenderer.AccretionRange);
                Vector2? preyCenter = prey != null ? prey.Center : (Vector2?)null;
                WhiteDwarfRenderer.Draw(Projectile, LifeT, Seed, preyCenter);
            }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
                RestoreSpriteBatch();
            return false;
        }

        /// <summary>Restaura el SpriteBatch con los parámetros EXACTOS del
        /// pase de proyectiles de vanilla (Main.DrawProjectiles).</summary>
        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        public override void OnKill(int timeLeft)
        {
            // === EL DAÑO FINAL (MP-seguro): el cristal se rompe ===
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int burst = Math.Max(1, (int)(Projectile.damage * 0.8f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    if ((npc.Center - Projectile.Center).Length() > 110f) continue;
                    npc.SimpleStrikeNPC(burst, npc.direction, false, 1.5f, DamageClass.Magic);
                    try { npc.AddBuff(BuffID.OnFire, 240); } catch { }   // v6.30: TODO SOL QUEMA — el rescoldo cristalino
                }
            }

            if (Main.netMode == NetmodeID.Server) return;

            // === LA ROTURA CRISTALINA: esquirlas blancas-azules ===
            float scale = Math.Max(Projectile.scale, 0.4f);
            ParticlePresets.RingPulse(Projectile.Center, 80f * scale,
                new Color(210, 235, 255, 220), 20);
            ParticlePresets.Explosion(Projectile.Center, 60f * scale, 18,
                new Color(250, 252, 255), new Color(150, 200, 255), 30);

            for (int i = 0; i < 22; i++)
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    new Vector2(MathF.Cos(angle), MathF.Sin(angle)) *
                    Main.rand.NextFloat(3f, 8f) * scale,
                    210, new Color(220, 240, 255), 0.9f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27.WithPitchOffset(0.6f), Projectile.Center);
        }

        /// <summary>Elastic ease-out (curva elástica de aparición).</summary>
        private static float ElasticOut(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float c = (2f * (float)Math.PI) / 3f;
            return (float)(Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c) + 1);
        }
    }
}
