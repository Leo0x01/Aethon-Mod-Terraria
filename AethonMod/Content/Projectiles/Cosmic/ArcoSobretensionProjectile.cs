using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// ArcoSobretensionProjectile — v6.50.18 — EL ARCO DEL ARC SURGE.
    ///
    /// Como el 1122 de vanilla: vive UN tick (el visual vive 16 en el
    /// RayoSistema), nace EN el punto de impacto (la mira del jugador) y
    /// clava el daño ahí. El canal es el preset RayoParams.Arco (el
    /// GetArcSurgeWeaponGenerator de vanilla: mano→blanco, 5 capas al 1.2,
    /// paso 6 px, horquillas al 10-50% del trayecto), ANCLADO a la mano
    /// del jugador (la elasticidad cuártica de vanilla: el arco CUELGA de
    /// la mano que se mueve), carmesí (255, 60, 60) — el color EXACTO de
    /// GetLightningColor(1122).
    /// </summary>
    public class ArcoSobretensionProjectile : ModProjectile
    {
        private bool _done;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;          // el contacto de vanilla (16×16 en el blanco)
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1;             // UN tick (el 1122 de vanilla)
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 10;
        }

        public override void AI()
        {
            if (_done) return;
            _done = true;

            // LA MANO (la ancla elástica de vanilla: RotatedRelativePoint).
            Player dueno = Main.player[Projectile.owner];
            Vector2 mano = Projectile.ai[0] != 0f || Projectile.ai[1] != 0f
                ? new Vector2(Projectile.ai[0], Projectile.ai[1])
                : (dueno != null && dueno.active ? dueno.Center : Projectile.Center);
            Vector2 blanco = Projectile.Center;

            // === EL CANAL (solo quien tiene ojos: cliente) ===
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                uint semilla = (uint)Main.rand.Next(1, int.MaxValue);
                RayoSistema.Lanzar(RayoParams.Arco(mano, blanco), semilla,
                    blanco, mano - blanco,
                    new Color(255, 60, 60), new Color(255, 235, 235),
                    ancho: 12f, vidaTicks: 16, AnimRayo.Arco,
                    luzEstable: true, jugadorAncla: Projectile.owner);

                // EL FLASH DEL EXTREMO (la luz del impacto de vanilla) + el
                // zap agudo del arco (el Item15 con su pitch ±0.2).
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item15.WithPitchOffset(-0.15f).WithVolumeScale(0.6f), mano);

                for (int i = 0; i < 10; i++)
                {
                    float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    Dust d = Dust.NewDustPerfect(blanco, DustID.Electric,
                        new Vector2((float)System.Math.Cos(ang), (float)System.Math.Sin(ang)) *
                            Main.rand.NextFloat(2f, 6f),
                        200, new Color(255, 120, 110), 1.0f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // === LOS DOS ARCOS HERMANOS (el cono de 60° de vanilla: hasta 2
            //     enemigos perseguibles en el rectángulo 1000×800 mirando a
            //     donde apuntas) — los dispara la MÁQUINA DUEÑA. ===
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 aim = Vector2.Normalize(blanco - mano);
                if (!float.IsFinite(aim.X) || aim == Vector2.Zero) aim = Vector2.UnitX;

                int hermanos = 0;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (hermanos >= 2) break;
                    if (!VFXCore.EsObjetivo(npc)) continue;

                    Vector2 hacia = npc.Center - mano;
                    if (hacia.Length() > 620f) continue;
                    // EL CONO: solo los que están a donde apuntas (dot ≥ 0.5
                    // — el 60° de vanilla).
                    if (Vector2.Dot(aim, Vector2.Normalize(hacia)) < 0.5f) continue;

                    Content.Systems.GolpeMotor.Golpear(Projectile, npc,
                        (int)(Projectile.damage * 0.7f), 1.5f, true);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        try { npc.AddBuff(BuffID.Electrified, 300); } catch { }

                    if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                    {
                        uint semillaH = (uint)Main.rand.Next(1, int.MaxValue);
                        RayoSistema.Lanzar(RayoParams.Arco(mano, npc.Center), semillaH,
                            npc.Center, mano - npc.Center,
                            new Color(255, 60, 60), new Color(255, 235, 235),
                            ancho: 10f, vidaTicks: 16, AnimRayo.Arco,
                            luzEstable: false, jugadorAncla: Projectile.owner);
                    }
                    hermanos++;
                }
            }

            Projectile.netUpdate = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // EL ZAPPED de vanilla (buff rojo 4-7 s — aquí Electrified, el
            // eléctrico del arsenal).
            try { target.AddBuff(BuffID.Electrified, 300); } catch { }
        }

        // INVISIBLE: el canal lo dibuja el RayoSistema (PostDrawTiles).
        public override bool PreDraw(ref Color lightColor) => false;
    }
}
