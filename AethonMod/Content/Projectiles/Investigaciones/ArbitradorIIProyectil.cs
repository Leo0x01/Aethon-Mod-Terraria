using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Investigaciones
{
    // ======================================================================
    //  ARBITRADOR II PROYECTIL — LA JABALINA DEL ORIGINAL (Calamity
    //  Entropy — la ficha del wiki):
    //    · EL VUELO: atraviesa MUROS (tileCollide = false), gira como
    //      jabalina lanzada.
    //    · EL GOLPE NORMAL: ESTALLA al tocar a un objetivo — daño a TODO
    //      lo que viva en el área de la explosión (radio 130).
    //    · EL GOLPE SIGILOSO: SE CLAVA en el objetivo (lo cabalga), y a
    //      los 60 ticks (1 s) libera la EXPLOSIÓN EN X — CUATRO BRAZOS
    //      diagonales (la X) de mayor alcance (280 px) que barren a todo
    //      lo que cruzan.
    // ======================================================================
    public class ArbitradorIIProyectil : ModProjectile
    {
        // EL Lienzo de la casa: TODO su dibujado es código (HazLib / los
        // arcos de la casa) — la textura es el placeholder de siempre.
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Throwing;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = false;       // la ficha: ATRAVIESA MUROS
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;
        }

        /// <summary>0 = normal · 1 = sigilosa (clavada + X).</summary>
        private float Modo => Projectile.ai[0];

        // LA CLAVADA (el modo sigiloso): la presa clavada y el reloj de la X.
        private int _clavadaEn = -1;
        private int _relojX;
        private bool _explotada;

        public override void AI()
        {
            // EL VUELO DE LA JABALINA: gira sobre su trayectoria.
            if (_clavadaEn < 0)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            else
            {
                // LA CLAVADA: cabalga a su presa (la jabalina ENTERRADA).
                NPC presa = Main.npc[_clavadaEn];
                if (presa != null && presa.active && presa.life > 0)
                {
                    Projectile.Center = presa.Center;
                    Projectile.velocity = Vector2.Zero;
                    if (!Main.dedServ && Main.rand.NextBool(4))
                    {
                        Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(14f, 14f),
                            DustID.Enchanted_Pink, Vector2.Zero, 160,
                            new Color(190, 140, 255), 0.7f);
                        d.noGravity = true;
                    }
                }
                else
                {
                    // la presa murió antes de la X: la jabalina cae suelta.
                    _clavadaEn = -1;
                    Projectile.velocity = new Vector2(0f, 4f);
                }

                // EL RELOJ DE LA X (1 segundo clavada — la ficha).
                if (!_explotada)
                {
                    _relojX++;
                    if (_relojX >= 60)
                    {
                        _explotada = true;
                        ExplotarLaX();
                        Projectile.Kill();
                        return;
                    }
                }
            }

            if (!Main.dedServ && _clavadaEn < 0 && Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                    -Projectile.velocity * 0.1f, 140, new Color(160, 110, 240), 0.7f);
                d.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Modo > 0.5f)
            {
                // EL SIGILOSO: SE CLAVA (la ficha: la jabalina se queda
                // DENTRO y a los 1 s libera la X).
                _clavadaEn = target.whoAmI;
                _relojX = 0;
                Projectile.netUpdate = true;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
                return;
            }

            // EL NORMAL: ESTALLA al golpear — daño en ÁREA (la ficha).
            ExplotarNormal();
            Projectile.Kill();
        }

        /// <summary>LA EXPLOSIÓN NORMAL: todo lo que viva en el área (el
        /// cauce MP-seguro de la casa: GolpeMotor).</summary>
        private void ExplotarNormal()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy() || npc.friendly) continue;
                    if (npc.Center.DistanceSQ(Projectile.Center) < 130f * 130f)
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc, Projectile.damage, 2f, true);
                }
            }
            if (!Main.dedServ)
            {
                for (int i = 0; i < 18; i++)
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Pink,
                        ang.ToRotationVector2() * Main.rand.NextFloat(3f, 9f), 180,
                        new Color(200, 150, 255), Main.rand.NextFloat(0.9f, 1.6f));
                    d.noGravity = true;
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62, Projectile.Center);
                OndaLib.Kick(4f, 10);
            }
        }

        /// <summary>LA EXPLOSIÓN EN X DEL SIGILOSO: CUATRO BRAZOS diagonales
        /// (la X de la ficha) que barren a 280 px — la versión de MAYOR
        /// ALCANCE.</summary>
        private void ExplotarLaX()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // LOS CUATRO BRAZOS DE LA X: los brazos diagonales (la X).
                for (int brazo = 0; brazo < 4; brazo++)
                {
                    float ang = MathHelper.PiOver4 + brazo * MathHelper.PiOver2;   // ↗ ↘ ↙ ↖
                    Vector2 dir = ang.ToRotationVector2();
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.CanBeChasedBy() || npc.friendly) continue;
                        Vector2 v = npc.Center - Projectile.Center;
                        float along = Vector2.Dot(v, dir);
                        if (along < -npc.width || along > 280f) continue;
                        float perp = MathF.Abs(v.X * -dir.Y + v.Y * dir.X);
                        if (perp < 34f + npc.width * 0.5f)
                            Content.Systems.GolpeMotor.Golpear(Projectile, npc, Projectile.damage, 3f, true);
                    }
                }
            }
            if (!Main.dedServ)
            {
                for (int brazo = 0; brazo < 4; brazo++)
                {
                    float ang = MathHelper.PiOver4 + brazo * MathHelper.PiOver2;
                    for (int i = 0; i < 26; i++)
                    {
                        float lejos = i / 25f * 280f;
                        Dust d = Dust.NewDustPerfect(Projectile.Center + ang.ToRotationVector2() * lejos,
                            i % 3 == 0 ? DustID.Enchanted_Pink : DustID.Shadowflame,
                            ang.ToRotationVector2() * Main.rand.NextFloat(2f, 6f), 190,
                            i % 3 == 0 ? new Color(220, 170, 255) : new Color(150, 84, 235),
                            Main.rand.NextFloat(0.9f, 1.7f));
                        d.noGravity = true;
                    }
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62.WithPitchOffset(-0.2f), Projectile.Center);
                OndaLib.Kick(9f, 16);
            }
        }

        public override bool PreDraw(ref Microsoft.Xna.Framework.Color lightColor)
        {
            // LA JABALINA: el huso del arbitraje — un PEQUEÑO HAZ de la
            // casa (HazLib) por el cuerpo (morado en los bordes, negro al
            // centro — la paleta de la Entropía) + la punta de estrella.
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            try
            {
                Vector2 punta = Projectile.Center - Main.screenPosition;
                Vector2 dir = _clavadaEn >= 0
                    ? Projectile.rotation.ToRotationVector2()
                    : Vector2.Normalize(Projectile.velocity);
                Vector2 rabo = punta - dir * 56f;

                HazLib.Abrir();
                try
                {
                    HazLib.IntensidadGlobal = 1f;
                    HazLib.PrepararPar(rabo, punta, x => 10f * MathF.Pow(MathF.Sin(MathF.PI * x), 0.7f), 0f, 24);
                    HazLib.Dibujar(x => new Color(150, 84, 235, 255),
                        (x, y, tinte) => MathF.Abs(y - 0.5f) * 2f < 0.5f
                            ? new Color(6, 3, 10, 255)
                            : tinte,
                        HazLib.FilasNucleo);
                }
                finally { HazLib.Cerrar(); }
            }
            catch { }
            return false;
        }
    }
}
