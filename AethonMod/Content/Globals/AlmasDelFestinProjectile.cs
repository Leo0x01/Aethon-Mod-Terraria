using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals
{
    // ======================================================================
    //  ALMAS DEL FESTÍN (v6.50.129) — LOS EFECTOS DE LAS ALMAS ÚNICAS en
    //  los disparos del GRIMORIO DEL ETERNO. La letra: «puedes crear
    //  stats y efectos para las armas en caso de que estas tengan
    //  efectos o sean especiales» — devorar un arma ESPECIAL despierta
    //  su alma en el libro (ShardPlayer.AlmaIgnea/AlmaSalvaje/…), y el
    //  alma VIVE aquí: los disparos del Eterno (el 931) pueden PRENDER
    //  (alma ígnea), ENVENENAR (alma salvaje) y ROBAR VIDA (alma
    //  vampírica) con la probabilidad acumulada.
    //
    //  EL FILTRO DE LA CASA: otras armas también disparan el 931 (la
    //  Emperatriz, el Equinoccio…) — el alma solo despierta si el que
    //  dispara SOSTIENE al Grimorio del Eterno (HeldItem manda).
    // ======================================================================
    public class AlmasDelFestinProjectile : GlobalProjectile
    {
        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            => entity.type == 931;   // Nightglow — el disparo del Eterno

        public override void OnHitNPC(Projectile projectile, NPC target,
            NPC.HitInfo hit, int damageDone)
        {
            try
            {
                // SOLO el Eterno en la mano (el dueño del alma).
                Player dueno = Main.player[projectile.owner];
                if (dueno == null || !dueno.active) return;
                Item mano = dueno.HeldItem;
                if (mano == null || mano.type !=
                    ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>())
                    return;

                var sp = dueno.GetModPlayer<global::AethonMod.Content.Players.ShardPlayer>();
                if (sp == null) return;

                bool tocoAlma = false;

                // EL ALMA ÍGNEA: los disparos PRENDEN (tope 30 %).
                float probFuego = global::AethonMod.Content.Players.ShardPlayer.ProbIgnea(dueno);
                if (probFuego > 0f && !target.immortal && !target.friendly &&
                    Main.rand.NextFloat() < probFuego)
                {
                    target.AddBuff(BuffID.OnFire, 240);
                    tocoAlma = true;
                }

                // EL ALMA SALVAJE: los disparos ENVENENAN (tope 30 %).
                float probVeneno = global::AethonMod.Content.Players.ShardPlayer.ProbSalvaje(dueno);
                if (probVeneno > 0f && !target.immortal && !target.friendly &&
                    Main.rand.NextFloat() < probVeneno)
                {
                    target.AddBuff(BuffID.Poisoned, 240);
                    tocoAlma = true;
                }

                // EL ALMA VAMPIRA: los disparos ROBAN VIDA (tope 8 % —
                // solo en la máquina del dueño: la vida es suya).
                float robo = global::AethonMod.Content.Players.ShardPlayer.RoboVidaAlmas(dueno);
                if (robo > 0f && projectile.owner == Main.myPlayer)
                {
                    int sana = (int)System.Math.Ceiling(hit.Damage * robo);
                    if (sana > 0 && dueno.statLife < dueno.statLifeMax2)
                    {
                        dueno.Heal(sana);
                        tocoAlma = true;
                    }
                }

                // EL TEATRO DEL ALMA: el polvillo del efecto que acaba de
                // despertar (solo pantallas — leerlo a simple vista).
                if (tocoAlma && !Main.dedServ && Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        Dust d = Dust.NewDustPerfect(target.Center +
                            new Vector2(Main.rand.NextFloat(-8f, 8f),
                                Main.rand.NextFloat(-10f, 4f)),
                            DustID.Shadowflame,
                            new Vector2(Main.rand.NextFloat(-1f, 1f),
                                Main.rand.NextFloat(-1.6f, -0.2f)), 150,
                            new Color(140, 60, 210), 0.8f);
                        d.noGravity = true;
                    }
                }
            }
            catch { }
        }
    }
}
