using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Cosmetic;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// SellosPlayer — v6.35 — EL RASTREADOR DE LOS SIGNOS MÁGICOS.
    ///
    /// El ModPlayer de LOS DOS ACCESORIOS DE LAS LIBRERÍAS: escanea
    /// TANTO los huecos de accesorio funcionales como los de VANIDAD
    /// (un signo es un signo viva donde lo lleves — la misma regla de
    /// las coronas) y respeta el OJITO de ocultar accesorio: las
    /// estadísticas viven en UpdateAccessory, la escritura vive aquí.
    ///
    /// v6.40 — LA ESCRITURA CAMBIÓ DE PUERTA: el Sello del Génesis y los
    /// Anillos del Sol Rúnico YA NO salen por las capas de jugador (el
    /// pase del jugador mezcla con la fórmula PREmultiplicada de
    /// AlphaBlend y los tintes de la casa con RGB intacto se dibujaban
    /// como COLOR PLANO — el reporte del usuario): ahora invocan sus
    /// HALOS proyectiles (SelloGenesisHalo / AnillosSolaresHalo), que
    /// vuelcan por SU lote aditivo — el MISMO camino por el que salen
    /// los anillos de los soles rúnicos reales.
    ///
    /// v6.50.44 — LOS ANILLOS DEL HORIZONTE DE SUCESOS FUERON BORRADOS
    /// por petición del usuario («el item, los anillos del horizonte de
    /// sucesos lo puedes borrar») — su lente, sus ascuas, su succión y
    /// su halo murieron con él (GravLens sigue vivo: lo usan los
    /// agujeros negros de las armas cósmicas).
    ///
    /// Lo que hace VIVO cada accesorio:
    ///   · EL SELLO DEL GÉNESIS — chispas doradas que escapan de las
    ///     runas del aro mayor (en su posición EXACTA del mundo) y luz
    ///     de oro suave.
    ///   · LOS ANILLOS DEL SOL RÚNICO — motas de polvo solar cayendo
    ///     en órbita y luz cálida de estrella.
    /// </summary>
    public class SellosPlayer : ModPlayer
    {
        /// <summary>¿Lleva EL SELLO DEL GÉNESIS (y quiere la escritura)?</summary>
        public bool SelloGenesis;

        /// <summary>¿Lleva LOS ANILLOS DEL SOL RÚNICO?</summary>
        public bool AnillosSol;

        public override void ResetEffects()
        {
            SelloGenesis = false;
            AnillosSol = false;
        }

        public override void PostUpdate()
        {
            // === EL ESCANEO DE HUECOS (la regla de las coronas): 3..9
            //     funcionales (respetando el ojito de ocultar) y
            //     13..19 de vanidad (siempre visibles). ===
            int sello = ModContent.ItemType<Items.Accessories.SelloGenesisItem>();
            int sol = ModContent.ItemType<Items.Accessories.AnillosSolRunicoItem>();

            for (int i = 3; i <= 19; i++)
            {
                // Salto los huecos de vanidad de armadura (10..12).
                if (i >= 10 && i <= 12) continue;

                Item item = Player.armor[i];
                if (item == null || item.IsAir) continue;

                if (item.type == sello) SelloGenesis = true;
                else if (item.type == sol) AnillosSol = true;
            }

            // v6.50.2 — FIX (host sin sus visuales en Host&Play): el viejo
            // `netMode == Server` devolvía también en el LISTEN SERVER
            // (netMode 1 CON pantalla): el host perdía la lente de los
            // Anillos del Horizonte, las ascuas, el halo y las chispas de
            // los otros dos sellos. "Sin pantalla" es Main.dedServ — y este
            // bloque es TODO visual de cada pantalla (polvo/luz/lente +
            // halos ya gateados a whoAmI == Main.myPlayer más abajo).
            if (Main.dedServ) return;
            if (Player.dead) return;

            float time = Main.GlobalTimeWrappedHourly;

            // ==============================================================
            //  EL SELLO DEL GÉNESIS — chispas desde las runas EXACTAS.
            // ==============================================================
            if (SelloGenesis)
            {
                if (Main.rand.NextBool(34))
                {
                    int g = Main.rand.Next(8);
                    Vector2 runa = SigiloLib.RunaSelloWorld(Player.Center,
                        Player.height * 1.05f, time, g);
                    Dust d = Dust.NewDustPerfect(runa, DustID.Enchanted_Gold,
                        new Vector2(Main.rand.NextFloat(-0.4f, 0.4f),
                                    -Main.rand.NextFloat(0.6f, 1.4f) * Player.gravDir),
                        170, new Color(255, 214, 120), 0.75f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // Luz de oro suave (la escritura ilumina).
                Lighting.AddLight(Player.Center, new Vector3(0.26f, 0.20f, 0.07f));

                // v6.40 — EL HALO ADITIVO (el sello dejó de salir por la
                // capa de jugador: ahí se veía COLOR PLANO — el pase es
                // premultiplicado y los tintes de la casa no).
                if (Player.whoAmI == Main.myPlayer && !EspiarHaloSello())
                {
                    Projectile.NewProjectile(Player.GetSource_Misc("SelloGenesis"),
                        Player.Center, Vector2.Zero,
                        ModContent.ProjectileType<SelloGenesisHalo>(),
                        0, 0f, Player.whoAmI);
                }
            }

            // ==============================================================
            //  LOS ANILLOS DEL SOL RÚNICO — el polvo de la constelación.
            // ==============================================================
            if (AnillosSol)
            {
                if (Main.rand.NextBool(40))
                {
                    // Motas cayendo en órbita alrededor del cuerpo.
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    float r = Player.height * (0.55f + Main.rand.NextFloat(0.85f));
                    Vector2 pos = Player.Center + new Vector2(
                        (float)System.Math.Cos(ang) * r,
                        (float)System.Math.Sin(ang) * r * 0.5f);
                    Dust d = Dust.NewDustPerfect(pos, DustID.YellowTorch,
                        new Vector2(-Main.rand.NextFloat(0.3f, 0.8f) * Player.direction,
                                    Main.rand.NextFloat(0.2f, 0.6f)),
                        150, new Color(255, 200, 110), 0.65f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // Luz cálida de estrella viva.
                Lighting.AddLight(Player.Center, new Vector3(0.30f, 0.22f, 0.05f));

                // v6.40 — EL HALO ADITIVO (la constelación dejó de salir
                // por la capa de jugador por el mismo COLOR PLANO).
                if (Player.whoAmI == Main.myPlayer && !EspiarHaloSol())
                {
                    Projectile.NewProjectile(Player.GetSource_Misc("AnillosSolRunico"),
                        Player.Center, Vector2.Zero,
                        ModContent.ProjectileType<AnillosSolaresHalo>(),
                        0, 0f, Player.whoAmI);
                }
            }
        }

        /// <summary>¿Ya vive mi halo del sello del génesis?</summary>
        private bool EspiarHaloSello()
        {
            int tipo = ModContent.ProjectileType<SelloGenesisHalo>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == Player.whoAmI && p.type == tipo)
                    return true;
            }
            return false;
        }

        /// <summary>¿Ya vive mi halo de los anillos del sol?</summary>
        private bool EspiarHaloSol()
        {
            int tipo = ModContent.ProjectileType<AnillosSolaresHalo>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == Player.whoAmI && p.type == tipo)
                    return true;
            }
            return false;
        }
    }
}
