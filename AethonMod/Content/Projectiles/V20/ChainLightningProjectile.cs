using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace AethonMod.Content.Projectiles.V20
{
    /// <summary>
    /// ChainLightningProjectile — rayo que salta de enemigo en enemigo.
    ///
    /// Visuales:
    ///   - BeamCyan estirado entre posición actual y objetivo (o dirección de vuelo)
    ///   - Color amarillo-blanco brillante (yellow-white)
    ///   - Glow aditivo
    ///
    /// Físicas:
    ///   - ai[0] = contador de saltos (chain count). Máx = 5
    ///   - Al impactar NPC, busca el NPC más cercano dentro de 300px y
    ///     genera un nuevo ChainLightningProjectile con chain count +1
    ///   - timeLeft = 60 (el bolt individual es corto)
    /// </summary>
    public class ChainLightningProjectile : ModProjectile
    {
        private const float ChainRange = 300f;
        private const int MaxChain = 5;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 60;
            Projectile.light = 0f;
            Projectile.alpha = 0;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            try
            {
                Projectile.ai[0] += 0f; // ai[0] ya es el chain count, no incrementar
                Projectile.rotation = Projectile.velocity.ToRotation();

                // Dust eléctrico amarillo-blanco
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Electric,
                        -Projectile.velocity * 0.05f + new Vector2(
                            Main.rand.NextFloat(-0.8f, 0.8f),
                            Main.rand.NextFloat(-0.8f, 0.8f)),
                        150, new Color(255, 250, 200), 0.7f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // Iluminación amarilla brillante
                Lighting.AddLight(Projectile.Center, new Vector3(1f, 1f, 0.5f));
            }
            catch { }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            try
            {
                int currentChain = (int)Projectile.ai[0];
                // Ya no puede saltar más
                if (currentChain >= MaxChain) return;

                // Buscar el NPC más cercano dentro de 300px (excluyendo el actual target)
                NPC nextTarget = FindNearestNPC(ChainRange, target);
                if (nextTarget == null) return;

                // Vector hacia el siguiente target
                Vector2 dir = (nextTarget.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                Vector2 newVel = dir * Projectile.velocity.Length();

                // Generar nuevo ChainLightningProjectile con chain count + 1
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    newVel,
                    Projectile.type,
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    ai0: (float)(currentChain + 1),
                    ai1: nextTarget.whoAmI);

                // Sparks eléctricos en el punto de impacto
                SpawnChainSparks(target.Center);
            }
            catch { }
        }

        private NPC FindNearestNPC(float range, NPC exclude)
        {
            NPC best = null;
            float bestDist = range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active) continue;
                if (npc.friendly || npc.townNPC) continue;
                if (npc.dontTakeDamage) continue;
                if (!npc.CanBeChasedBy()) continue;
                if (npc.whoAmI == exclude.whoAmI) continue;
                // Saltar NPCs ya golpeados por este chain
                if (Projectile.localNPCImmunity != null
                    && npc.whoAmI >= 0 && npc.whoAmI < Projectile.localNPCImmunity.Length
                    && Projectile.localNPCImmunity[npc.whoAmI] > 0) continue;

                float dist = Vector2.Distance(npc.Center, Projectile.Center);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = npc;
                }
            }
            return best;
        }

        private void SpawnChainSparks(Vector2 center)
        {
            for (int i = 0; i < 6; i++)
            {
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                float speed = Main.rand.NextFloat(2f, 5f);
                Vector2 vel = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed);
                Dust d = Dust.NewDustPerfect(center, DustID.Electric, vel, 200,
                    new Color(255, 250, 180), 1.0f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                Texture2D beamCyan = ModContent.Request<Texture2D>("AethonMod/Content/Effects/BeamCyan").Value;
                Texture2D softGlow = ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value;

                if (beamCyan == null || softGlow == null)
                    return false;

                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                float rotation = Projectile.velocity.ToRotation();

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);

                // === Trail del BeamCyan a lo largo de oldPos (estela eléctrica) ===
                int trailLen = ProjectileID.Sets.TrailCacheLength[Type];
                for (int i = 0; i < trailLen - 1; i++)
                {
                    Vector2 p1 = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                    Vector2 p2 = Projectile.oldPos[i + 1] + Projectile.Size / 2f - Main.screenPosition;
                    if (p1 == p2) continue;
                    float segLen = Vector2.Distance(p1, p2);
                    if (segLen < 0.1f) continue;
                    Vector2 segDir = (p2 - p1).SafeNormalize(Vector2.Zero);
                    float segRot = segDir.ToRotation();
                    float alpha = (1f - (float)i / trailLen) * 0.7f;
                    float scale = (1f - (float)i / trailLen) * 0.9f;

                    Main.spriteBatch.Draw(beamCyan, p1, null,
                        new Color(255, 250, 180, (byte)(alpha * 255f)),
                        segRot,
                        new Vector2(0f, beamCyan.Height / 2f),
                        new Vector2(segLen / (float)beamCyan.Width, scale * 0.6f),
                        SpriteEffects.None, 0f);
                }

                // === Núcleo BeamCyan rotado en la dirección de vuelo ===
                Vector2 beamOrigin = new Vector2(0f, beamCyan.Height / 2f);
                Main.spriteBatch.Draw(beamCyan, drawPos, null,
                    new Color(255, 250, 200, 230),
                    rotation, beamOrigin,
                    new Vector2(2.0f, 0.6f), SpriteEffects.None, 0f);

                // === Glow central amarillo-blanco ===
                Vector2 glowOrigin = new Vector2(softGlow.Width / 2f, softGlow.Height / 2f);
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(255, 250, 180, 200),
                    0f, glowOrigin, 0.7f, SpriteEffects.None, 0f);
                // Núcleo blanco más pequeño
                Main.spriteBatch.Draw(softGlow, drawPos, null,
                    new Color(255, 255, 240, 230),
                    0f, glowOrigin, 0.35f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            }
            catch { }
            return false;
        }
    }
}
