using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Buffs;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// DeadStarProjectile — v6.26 — LA ESTRELLA MUERTA (LA ENANA NEGRA).
    ///
    /// Un sol que agotó hasta su último fotón: mata LENTO.
    ///
    /// FÍSICA:
    ///   · DAÑO DE ENTROPÍA: 8 p/s constantes en 120 px — SimpleStrikeNPC
    ///     con daño BAJO y FRECUENTE (cada 15 ticks, ~2 por golpe: la
    ///     entropía deshace, no revienta). Sin debuffs rápidos: la
    ///     muerte por enfriamiento no se apura.
    ///   · DERIVA errática lenta (el vagar de un cadáver: wander
    ///     determinista por senos inconmensurables).
    ///   · LA LUZ: naranja MUY tenue (0.08/0.03/0.01 — el rescoldo);
    ///     el oscurecimiento real del entorno lo hace el aura oscura
    ///     VISUAL del renderer (masa negra en AlphaBlend).
    ///   · Vida 10 s (la más longeva: la muerte es paciente).
    ///
    /// Daño MP-seguro: SimpleStrikeNPC con Main.netMode != MultiplayerClient.
    /// Visual solo cliente (Main.netMode == Server → return).
    /// </summary>
    public class DeadStarProjectile : ModProjectile
    {
        /// <summary>Vida total en ticks (10 s — la más paciente).</summary>
        private const int Lifetime = 600;

        /// <summary>Radio del aura de entropía.</summary>
        private const float EntropyRadius = 120f;

        /// <summary>Periodo del tick de entropía (15 ticks → 4 golpes/s).</summary>
        private const int EntropyPeriod = 15;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 49;
            Projectile.height = 49;
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

            // === LA APARICIÓN: sin pop elástico — la muerte EMERGE lenta
            //     (un fade-in de 40 ticks: nadie nace muerto de golpe) ===
            Projectile.scale = MathHelper.Clamp(Age / 40f, 0f, 1f) *
                               (1f + 0.02f * MathF.Sin(Age * 0.05f));

            // === EL VAGAR DEL CADÁVER: deriva errática lenta (senos
            //     inconmensurables — determinista, sin estado) ===
            Projectile.velocity *= 0.98f;
            float wobX = MathF.Sin(Age * 0.021f + Seed * 0.13f);
            float wobY = MathF.Sin(Age * 0.017f + Seed * 0.29f + 2.1f);
            Projectile.velocity += new Vector2(wobX, wobY) * 0.05f;
            if (Projectile.velocity.Length() > 0.8f)
                Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 0.8f;

            // === EL DAÑO DE ENTROPÍA: 8 p/s — daño BAJO y FRECUENTE
            //     (SimpleStrikeNPC cada 15 ticks; ~2 por golpe) ===
            if (Main.netMode != NetmodeID.MultiplayerClient &&
                Age > 30f && Age % EntropyPeriod == 0f && Projectile.scale > 0.25f)
            {
                // 4 golpes/s × ~2 = los 8 p/s de la entropía (escala con el
                // arma por un 1% — la muerte no se compra con daño).
                int dot = Math.Max(2, (int)(Projectile.damage * 0.01f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    if ((npc.Center - Projectile.Center).Length() > EntropyRadius) continue;
                    npc.SimpleStrikeNPC(dot, 0, false, 0f, DamageClass.Magic);
                        try { npc.AddBuff(ModContent.BuffType<Content.Buffs.QuemaduraCosmica>(), 300); } catch { }   // v6.30: el fuego de una estrella MUERTA es NEGRO
                }
            }

            // === LA LUZ: el rescoldo (naranja MUY tenue — casi nada) ===
            float ember = 0.5f + 0.5f * MathF.Sin(Age * 0.021f * MathHelper.TwoPi);
            Lighting.AddLight(Projectile.Center,
                0.08f * Projectile.scale * (0.7f + 0.3f * ember),
                0.03f * Projectile.scale,
                0.01f * Projectile.scale);

            // === VISUAL SOLO CLIENTE: las brasas frías cayendo (polvo) ===
            if (Main.netMode == NetmodeID.Server) return;
            if (Age % 11f == 0f && Projectile.scale > 0.3f)
            {
                float ang = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Vector2 rim = Projectile.Center +
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (DeadStarRenderer.BodyPx * Projectile.scale);
                Dust d = Dust.NewDustPerfect(rim, DustID.BlueTorch,
                    new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(0.3f, 1.1f)),
                    90, new Color(120, 160, 220), 0.5f);
                d.noGravity = false;   // las brasas CAEN (frías y muertas)
                d.fadeIn = 0f;
            }
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
                DeadStarRenderer.Draw(Projectile, LifeT, Seed);
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
            // === EL DAÑO FINAL (MP-seguro): el último suspiro (pequeño) ===
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int burst = Math.Max(1, (int)(Projectile.damage * 0.5f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    if ((npc.Center - Projectile.Center).Length() > 100f) continue;
                    npc.SimpleStrikeNPC(burst, npc.direction, false, 0.5f, DamageClass.Magic);
                        try { npc.AddBuff(ModContent.BuffType<Content.Buffs.QuemaduraCosmica>(), 300); } catch { }   // v6.30: el fuego de una estrella MUERTA es NEGRO
                }
            }

            if (Main.netMode == NetmodeID.Server) return;

            // === POLVO ERES: humo oscuro + brasas apagándose ===
            float scale = Math.Max(Projectile.scale, 0.4f);
            ParticlePresets.RingPulse(Projectile.Center, 70f * scale,
                new Color(120, 70, 45, 140), 26);

            for (int i = 0; i < 16; i++)
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke,
                    new Vector2(MathF.Cos(angle), MathF.Sin(angle)) *
                    Main.rand.NextFloat(1f, 3f) * scale,
                    90, new Color(40, 35, 45), 1.3f);
                d.noGravity = false;
                d.fadeIn = 0f;
            }
            for (int i = 0; i < 10; i++)
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch,
                    new Vector2(MathF.Cos(angle), MathF.Sin(angle)) *
                    Main.rand.NextFloat(1.5f, 4f) * scale,
                    110, new Color(140, 120, 180), 0.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62.WithPitchOffset(-0.4f), Projectile.Center);
        }
    }
}
