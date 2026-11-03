using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Cosmetic
{
    /// <summary>
    /// VastagoDelEternoPet — v6.91 — LA MASCOTA NUEVA: EL VÁSTAGO DEL
    /// ETERNO — el librito alado de ojos grandes que nació del pipeline
    /// sprite-gen de la investigación de los reels (una sola imagen →
    /// transparencia real por flood-fill → LOS CICLOS ANIMADOS: idle con
    /// respiración squash&amp;stretch y parpadeo · vuelo con balanceo).
    ///
    /// EL ATLAS (Content/Projectiles/Cosmetic/VastagoDelEternoPet.png,
    /// 40×384): 8 frames de 40×48 — 0..3 IDLE (respiración + parpadeo)
    /// y 4..7 VUELO (el bob y la inclinación de las zancadas del aire).
    /// El FindFrame elige el ciclo por la velocidad del dueño.
    ///
    /// LA VIDA: el patrón vanilla de toda mascota (la lección .121):
    /// timeLeft = 2 A CADA TICK — la muerte la decide SOLO el buff.
    /// </summary>
    public class VastagoDelEternoPet : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 8;                  // el atlas: 4 idle + 4 vuelo
            Main.projPet[Type] = true;                  // mascota: persiste
            ProjectileID.Sets.NeedsUUID[Type] = true;   // la instancia única del dueño
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 30;
            Projectile.friendly = true;     // las mascotas son friendly (no daña: damage 0)
            Projectile.damage = 0;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false; // flota: atraviesa el mundo
            Projectile.ignoreWater = true;
            Projectile.netImportant = true; // tML la sincroniza
        }

        public override void AI()
        {
            // === LA VIDA DE LA MASCOTA (el patrón vanilla): el buff manda ===
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead ||
                !duenio.HasBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.VastagoDelEternoBuff>()))
            {
                Projectile.Kill();
                return;
            }
            Projectile.timeLeft = 2;

            float t = Main.GlobalTimeWrappedHourly;

            // === LA PERSECUCIÓN SUAVE: el lado libre del dueño (el libro
            //     viejo pasea el otro hombro — cada quien su sitio). ===
            float lado = MathF.Sin(t * 0.35f) * 34f + duenio.direction * -26f;
            float bob = MathF.Sin(t * 1.9f) * 9f;
            Vector2 meta = duenio.Center + new Vector2(lado, -46f + bob);

            Vector2 hacia = meta - Projectile.Center;
            float dist = hacia.Length();
            if (dist > 900f)
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, hacia * 0.05f, 0.20f);
            else
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, hacia * 0.045f, 0.10f);
            if (dist < 24f)
                Projectile.velocity *= 0.80f;

            // EL CINEO DEL VUELO: se recuesta hacia donde vuela.
            Projectile.rotation = MathHelper.Clamp(Projectile.velocity.X * 0.016f, -0.20f, 0.20f);

            // EL POLVILLO ESTELAR: una chispa violeta MUY de vez en cuando
            // (la estela del cosmos que lo parió).
            if (Main.rand.NextBool(48) && !Main.dedServ)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    DustID.Shadowflame, -Projectile.velocity * 0.15f, 140,
                    new Color(196, 150, 255), 0.55f);
                d.noGravity = true;
            }

            // === EL FRAME (el atlas sprite-gen): si el dueño CORRE, el
            //     ciclo de VUELO (4..7); quieto, la respiración del IDLE
            //     (0..3) — el parpadeo llega solo en el frame 3. ===
            float velDuenio = MathF.Abs(duenio.velocity.X) + MathF.Abs(duenio.velocity.Y);
            bool volando = velDuenio > 1.6f || dist > 120f;
            int primero = volando ? 4 : 0;
            int cuenta = 4;
            float paso = volando ? 7f : 9f;   // el vuelo aletea más rápido
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= (int)paso)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame < primero || Projectile.frame >= primero + cuenta)
                    Projectile.frame = primero;
            }
            Projectile.spriteDirection = 1;   // el atlas mira al frente
        }
    }
}
