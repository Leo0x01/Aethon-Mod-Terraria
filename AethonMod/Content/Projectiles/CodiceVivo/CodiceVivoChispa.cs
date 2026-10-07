using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.CodiceVivo
{
    /// <summary>
    /// CODICEVIVOCHISPA — v6.50.72 — LA CHISPA DEL CÓDICE (la prueba del
    /// sprite-de-código del usuario).
    ///
    /// La mirada hecha bala: una chispa VIOLETA autoguiada que el Códice
    /// Vivo escupe desde su ojo (en VELA, en ANDANADAS de tres) al enemigo
    /// más cercano — 100% dibujada por código (SoftGlow + VFXCore: el
    /// núcleo blanco, el halo violeta y la estela), persigue con giro suave
    /// y muere en un microdestello.
    /// </summary>
    public class CodiceVivoChispa : ModProjectile
    {
        private NPC Presa => Projectile.ai[0] <= 0 ? null : Main.npc[(int)Projectile.ai[0] - 1];

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 170;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];

            // LA PRESA: la que trae (o la más cercana si la original murió)
            NPC presa = Presa;
            if (presa == null || !presa.active || presa.life <= 0)
            {
                presa = null;
                float mejorD = 560f * 560f;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || n.life <= 0 || n.dontTakeDamage) continue;
                    if (n.friendly || n.townNPC) continue;
                    float d = Vector2.DistanceSquared(n.Center, Projectile.Center);
                    if (d < mejorD) { mejorD = d; presa = n; }
                }
                if (presa != null) Projectile.ai[0] = presa.whoAmI + 1;
            }

            // EL GIRO SUAVE: la chispa CURVA hacia su presa (persigue, no teletransporta)
            if (presa != null)
            {
                Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                float deseado = hacia.ToRotation();
                float actual = Projectile.velocity.ToRotation();
                float giro = MathHelper.WrapAngle(deseado - actual);
                float vel = MathF.Max(Projectile.velocity.Length(), 9f);
                Projectile.velocity = (actual + MathHelper.Clamp(giro, -0.09f, 0.09f)).ToRotationVector2() * vel;
            }
            else
            {
                // sin presa: serpentea muriendo (la chispa que no encontró a nadie)
                Projectile.velocity.Y += MathF.Sin(t * 0.2f) * 0.12f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                // LA ESTELA — la línea violeta que deja
                VFXCore.Begin();
                Vector2 cola = Projectile.Center - Projectile.velocity * 2.4f;
                VFXCore.Line(cola, Projectile.Center, SombrasLib.Alfa(SombrasLib.Violeta, 0.6f), 9f);
                VFXCore.FlushAdditive();

                // EL NÚCLEO — blanco con su halo violeta
                VFXCore.Begin();
                VFXCore.Quad(Projectile.Center, SombrasLib.Alfa(SombrasLib.Violeta, 0.55f), new Vector2(46f, 46f));
                VFXCore.Quad(Projectile.Center, SombrasLib.Alfa(SombrasLib.Blanco, 0.95f), new Vector2(16f, 16f));
                VFXCore.FlushAdditive();
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;
            Sonar(SoundID.Item12.WithPitchOffset(0.55f).WithVolumeScale(0.3f), Projectile.Center);
            for (int k = 0; k < 6; k++)
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(8, 8), 16, 16,
                    DustID.Shadowflame, Main.rand.NextFloat(-1.6f, 1.6f), Main.rand.NextFloat(-1.6f, 0.4f),
                    128, default, 0.7f);
                Main.dust[d].noGravity = true;
            }
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
