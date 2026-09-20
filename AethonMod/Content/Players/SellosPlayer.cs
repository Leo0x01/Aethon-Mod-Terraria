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
    /// El ModPlayer de LOS TRES ACCESORIOS DE LAS LIBRERÍAS: escanea
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
    /// los anillos de los soles rúnicos reales. La succión, las ascuas
    /// y la lente de los Anillos del Horizonte siguen aquí (su halo ya
    /// iba por el camino aditivo desde v6.35).
    ///
    /// Lo que hace VIVO cada accesorio:
    ///   · EL SELLO DEL GÉNESIS — chispas doradas que escapan de las
    ///     runas del aro mayor (en su posición EXACTA del mundo) y luz
    ///     de oro suave.
    ///   · LOS ANILLOS DEL SOL RÚNICO — motas de polvo solar cayendo
    ///     en órbita y luz cálida de estrella.
    ///   · LOS ANILLOS DEL HORIZONTE — la LENTE GRAVITACIONAL de
    ///     GravLens curvando el fondo alrededor del portador (re-
    ///     registrada cada tick, el patrón del portal estable), ascuas
    ///     rojas que caen HACIA el jugador (la succión visible) y la
    ///     SUCCIÓN real: los enemigos cercanos se deslizan hacia ti.
    /// </summary>
    public class SellosPlayer : ModPlayer
    {
        /// <summary>¿Lleva EL SELLO DEL GÉNESIS (y quiere la escritura)?</summary>
        public bool SelloGenesis;

        /// <summary>¿Lleva LOS ANILLOS DEL SOL RÚNICO?</summary>
        public bool AnillosSol;

        /// <summary>¿Lleva LOS ANILLOS DEL HORIZONTE DE SUCESOS?</summary>
        public bool AnillosVacio;

        public override void ResetEffects()
        {
            SelloGenesis = false;
            AnillosSol = false;
            AnillosVacio = false;
        }

        public override void PostUpdate()
        {
            // === EL ESCANEO DE HUECOS (la regla de las coronas): 3..9
            //     funcionales (respetando el ojito de ocultar) y
            //     13..19 de vanidad (siempre visibles). ===
            int sello = ModContent.ItemType<Items.Accessories.SelloGenesisItem>();
            int sol = ModContent.ItemType<Items.Accessories.AnillosSolRunicoItem>();
            int vacio = ModContent.ItemType<Items.Accessories.AnillosHorizonteItem>();

            for (int i = 3; i <= 19; i++)
            {
                // Salto los huecos de vanidad de armadura (10..12).
                if (i >= 10 && i <= 12) continue;

                Item item = Player.armor[i];
                if (item == null || item.IsAir) continue;

                if (item.type == sello) SelloGenesis = true;
                else if (item.type == sol) AnillosSol = true;
                else if (item.type == vacio) AnillosVacio = true;
            }

            // v6.50 — LA SUCCIÓN ANTES DEL MURO (hallazgo auditoría MP nº3):
            // los cosméticos son del cliente, pero la FÍSICA (npc.velocity
            // hacia el portador) es de la AUTORIDAD — el viejo `return` del
            // server se la tragaba y el gate del bloque (≠ client) la
            // bloqueaba en el cliente: en MP NADIE la corría (muerta).
            // Ahora: server en MP, y el proceso local en SP — como toda
            // física honesta del juego.
            if (AnillosVacio && Main.netMode != NetmodeID.MultiplayerClient && !Player.dead)
            {
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    if (npc.boss) continue;

                    Vector2 hacia = Player.Center - npc.Center;
                    float dist = hacia.Length();
                    if (dist > 140f || dist < 12f) continue;

                    npc.velocity += Vector2.Normalize(hacia) *
                        0.05f * (1f - dist / 140f + 0.35f);
                }
            }

            if (Main.netMode == NetmodeID.Server) return;
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

            // ==============================================================
            //  LOS ANILLOS DEL HORIZONTE — la lente, las ascuas y la
            //  succión.
            // ==============================================================
            if (AnillosVacio)
            {
                // --- LA LENTE (el signature): el fondo se curva alrededor
                //     del portador — re-registro cada tick con vida corta,
                //     el patrón del portal estable de GravLens. Fuerza
                //     0.30: presente sin devorar la pantalla. ---
                GravLens.Registrar(Player.Center, 78f, 0.30f, 0.10f);

                // --- LAS ASCUAS DE LA SUCCIÓN: chispas rojas que CAEN
                //     HACIA el jugador (la materia cayendo al pozo). ---
                if (Main.rand.NextBool(30))
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 pos = Player.Center + new Vector2(
                        (float)System.Math.Cos(ang) * 92f,
                        (float)System.Math.Sin(ang) * 60f);
                    Vector2 hacia = Player.Center - pos;
                    Dust d = Dust.NewDustPerfect(pos, DustID.Torch,
                        Vector2.Normalize(hacia) * Main.rand.NextFloat(1.2f, 2.4f),
                        160, new Color(255, 120, 60), 0.7f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // --- LA LUZ del disco (rojo-naranja tenue). ---
                Lighting.AddLight(Player.Center, new Vector3(0.22f, 0.07f, 0.02f));

                // --- EL HALO: el proyectil cosmético que dibuja los
                //     anillos (SelloVacio en su propio batch) lo invoca
                //     el dueño local y tML lo sincroniza al resto. ---
                if (Player.whoAmI == Main.myPlayer && !EspiarHalo())
                {
                    Projectile.NewProjectile(Player.GetSource_Misc("AnillosHorizonte"),
                        Player.Center, Vector2.Zero,
                        ModContent.ProjectileType<AnillosSingularesHalo>(),
                        0, 0f, Player.whoAmI);
                }
            }
        }

        /// <summary>¿Ya vive mi halo de anillos del vacío?</summary>
        private bool EspiarHalo()
        {
            int tipo = ModContent.ProjectileType<AnillosSingularesHalo>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == Player.whoAmI && p.type == tipo)
                    return true;
            }
            return false;
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
