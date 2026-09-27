using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// RayoPrimordialProjectile — v6.50.18 — EL IMPACTO DEL RAYO DEL CLIMA.
    ///
    /// El proyectil NACE en el punto de impacto y EN EL TICK 0 lanza al
    /// RayoSistema el canal generado por el RayoParams.Tormenta de RayoLib
    /// (el LightningGenerator de vanilla 1.4.5: cae de 1000 px con ±20° de
    /// deriva, choca con tiles y el líquido, horquilla refleja del quiebre
    /// superior, LA OLA). El daño es INSTANTÁNEO (los rayos de vanilla no
    /// telegrafían): radial 100 px + cadena a 2 + Electrified. El cine
    /// (chispas, trueno, sacudida, un toque de flash) en el mismo tick.
    /// </summary>
    public class RayoPrimordialProjectile : ModProjectile
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
            // El daño se aplica a mano (radial + cadena): sin contacto.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 8;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override void AI()
        {
            if (_done) return;
            _done = true;

            Vector2 impacto = Projectile.Center;

            // === EL CANAL DE VANILLA (solo quien tiene ojos: cliente) ===
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                uint semilla = (uint)Main.rand.Next(1, int.MaxValue);
                RayoSistema.Lanzar(RayoParams.Tormenta, semilla, impacto, null,
                    new Color(120, 230, 235), new Color(245, 255, 255),
                    ancho: 16f, vidaTicks: 45, AnimRayo.Clima,
                    luzEstable: true, jugadorAncla: -1);

                // EL POLVO DEL NACIMIENTO (EmitSpawnDust de vanilla): chispas
                // eléctricas cada ~10 puntos del canal, corriendo por la línea.
                // (el sistema ya emite la luz; el polvo lo suelta el impacto)
                for (int i = 0; i < 22; i++)
                {
                    float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    float spd = Main.rand.NextFloat(3f, 10f);
                    Dust d = Dust.NewDustPerfect(impacto, DustID.Electric,
                        new Vector2((float)System.Math.Cos(ang), (float)System.Math.Sin(ang) * 0.75f) * spd,
                        220, new Color(160, 240, 255), 1.25f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // EL GOLPE DE PANTALLA de la casa (sacudida + flash suave +
                // onda — un toque, que el rayo ES el protagonista).
                Pantalla.PresetImpacto(impacto, 1.1f, new Color(190, 245, 255));

                // TRUENO: el zap grave + el retumbo.
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item12.WithPitchOffset(-0.5f).WithVolumeScale(1.2f), impacto);
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item122.WithPitchOffset(-0.15f).WithVolumeScale(0.8f), impacto);
            }

            // === EL DAÑO (la máquina dueña manda; GolpeMotor en el cauce) ===
            if (Projectile.owner == Main.myPlayer)
            {
                var golpeados = new List<NPC>();
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    if ((npc.Center - impacto).Length() < 100f)
                    {
                        Content.Systems.GolpeMotor.Golpear(Projectile, npc,
                            Projectile.damage, 3f, true);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            try { npc.AddBuff(BuffID.Electrified, 300); } catch { }
                        golpeados.Add(npc);
                    }
                }

                // === LA CADENA: hasta 2 rebotes a los vecinos (como el
                //     ArcSurge de vanilla clava arcos extra a los cercanos) ===
                int cadenas = 0;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (cadenas >= 2) break;
                    if (!VFXCore.EsObjetivo(npc) || golpeados.Contains(npc)) continue;
                    if ((npc.Center - impacto).Length() > 320f) continue;

                    Content.Systems.GolpeMotor.Golpear(Projectile, npc,
                        (int)(Projectile.damage * 0.6f), 2f, true);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        try { npc.AddBuff(BuffID.Electrified, 240); } catch { }

                    // el arco de la cadena TAMBIÉN es un canal de vanilla
                    // (el Arco de ArcSurge, a la mitad del ancho).
                    if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                    {
                        uint semillaC = (uint)Main.rand.Next(1, int.MaxValue);
                        RayoSistema.Lanzar(RayoParams.Arco(impacto, npc.Center), semillaC,
                            npc.Center, impacto - npc.Center,
                            new Color(120, 230, 235), new Color(245, 255, 255),
                            ancho: 9f, vidaTicks: 16, AnimRayo.Arco, luzEstable: false);
                    }
                    cadenas++;
                }
            }

            Projectile.netUpdate = true;
        }

        // INVISIBLE: el canal lo dibuja el RayoSistema (PostDrawTiles).
        public override bool PreDraw(ref Color lightColor) => false;
    }
}
