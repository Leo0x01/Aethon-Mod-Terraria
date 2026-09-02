using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Players;

namespace AethonMod.Content.Weapons.Projectiles
{
    /// <summary>
    /// Solbrand Blade — proyectil que COPIA el arma SolbrandEdge.
    /// Es una replica de la espada que sale volando, girando y persiguiendo
    /// al enemigo mas cercano. Se desbloquea cada 10 niveles de Melee.
    ///
    /// Comportamiento:
    /// - Spawnea en la posicion del jugador con velocidad inicial hacia el cursor
    /// - Gira rapidamente (efecto de espada arrojadiza)
    /// - Persigue al enemigo hostil mas cercano (homing)
    /// - Atraviesa tiles
    /// - Dura ~5 segundos
    /// </summary>
    public class DawnSlash : ModProjectile
    {
        private int _target = -1;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            // El proyectil no usa su propia textura: la sobreescribimos en PreDraw
            // con la textura del SolbrandEdge.
        }

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 3; // atraviesa varios enemigos
            Projectile.timeLeft = 300; // ~5 segundos
            Projectile.aiStyle = -1;   // AI custom
            Projectile.tileCollide = false; // atraviesa paredes
            Projectile.light = 0.8f;
            Projectile.extraUpdates = 1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Explosion solar al impactar.
            for (int i = 0; i < 15; i++)
            {
                Dust.NewDustPerfect(target.Center, DustID.SolarFlare,
                    new Vector2(Main.rand.NextFloat(-5, 5), Main.rand.NextFloat(-5, 5)),
                    100, new Color(255, 154, 60), 1.5f);
            }
        }

        public override void AI()
        {
            // === ROTACION DRAMATICA (espada giratoria) ===
            Projectile.rotation += 0.45f;

            // === BUSCAR BLANCO si no tenemos uno o si murio ===
            if (_target < 0 || _target >= Main.maxNPCs ||
                !Main.npc[_target].active || Main.npc[_target].life <= 0)
            {
                _target = FindNearestHostile();
            }

            // === HOMING: perseguir al blanco ===
            if (_target >= 0 && _target < Main.maxNPCs && Main.npc[_target].active)
            {
                NPC tgt = Main.npc[_target];
                Vector2 toTarget = tgt.Center - Projectile.Center;
                float dist = toTarget.Length();
                if (dist > 0.1f)
                {
                    Vector2 desiredVel = toTarget.SafeNormalize(Vector2.Zero) * 14f;
                    // Curva suave: interpola la velocidad actual hacia la deseada
                    float homingStrength = 0.15f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVel, homingStrength);
                }
            }
            else
            {
                // Sin blanco: desacelera suavemente (flota)
                Projectile.velocity *= 0.98f;
            }

            // === ESTELA SOLAR ===
            if (Main.rand.NextBool(2))
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.SolarFlare, 0f, 0f, 100, new Color(255, 154, 60), 1.1f);
            }
            if (Main.rand.NextBool(4))
            {
                Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                    new Vector2(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-2, 2)),
                    150, default, 0.9f);
            }
        }

        /// <summary>
        /// Encuentra el NPC hostil mas cercano al proyectil (rango 800px).
        /// </summary>
        private int FindNearestHostile()
        {
            int best = -1;
            float bestDist = 800f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active) continue;
                if (npc.friendly || npc.townNPC) continue;
                if (npc.dontTakeDamage) continue;
                if (npc.aiStyle == 7) continue; // critters
                if (npc.catchItem > 0) continue;
                if (npc.immortal) continue;
                if (!npc.CanBeChasedBy()) continue;

                float dist = Vector2.Distance(npc.Center, Projectile.Center);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = i;
                }
            }
            return best;
        }

        /// <summary>
        /// Dibuja la TEXTURA DEL ARMA SolbrandEdge en vez de la textura propia.
        /// Esto hace que el proyectil sea visualmente una copia de la espada volando.
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            // Cargar la textura del item SolbrandEdge
            var tex = ModContent.Request<Texture2D>("AethonMod/Content/Weapons/SolbrandEdge").Value;
            if (tex == null) return true; // fallback: dibuja textura propia

            Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            // Pulso de luz dorada
            float scale = 1f + 0.1f * (float)System.Math.Sin(Projectile.rotation * 2f);

            // Dibujar con la rotacion del proyectil (espada girando)
            Main.spriteBatch.Draw(tex, drawPos, null, lightColor * 0.95f,
                Projectile.rotation, origin, scale, SpriteEffects.None, 0f);

            return false; // NO dibujar la textura original del proyectil
        }
    }
}
