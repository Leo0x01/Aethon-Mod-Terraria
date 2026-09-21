using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// CosmeticPlayer — v6.03 — EL RASTREADOR DE COSMÉTICOS DEL JUGADOR.
    ///
    /// Mantiene las banderas de qué coronas lleva puestas el jugador
    /// (escaneando TANTO los huecos de accesorio funcionales como los de
    /// VANIDAD — un cosmético es un cosmético viva donde lo pongas) y hace
    /// que vivan en el mundo:
    ///   - La CORONA DE ARCOS suelta ascuas rosas sobre sus ápices.
    ///   - La CORONA RÚNICA emite chispas ascendentes desde las perlas.
    ///   - EL ANILLO RÚNICO DORSAL (v6.37): invoca su halo proyectil (la
    ///     TRIPLE CORONA DE CONJURO de los agujeros negros, dibujada por
    ///     AnilloDorsalRenderer con OrbitaLib — runas de pie y perlas
    ///     detrás del cuerpo) y suelta chispas DORADAS desde las runas
    ///     exactas del círculo de oro.
    ///   - Ambas coronas iluminan suavemente la noche con su color.
    ///
    /// (v6.28: la CORONA DE ANILLOS RÚNICOS y el ANILLO RÚNICO ESTELAR
    /// fueron BORRADOS por petición del usuario — quedan las dos coronas.)
    /// </summary>
    public class CosmeticPlayer : ModPlayer
    {
        /// <summary>¿Lleva la Corona de la Reina del Vacío (arcos de neón)?</summary>
        public bool VoidCrown;

        /// <summary>¿Lleva la Corona Rúnica Estelar (glifos flotantes)?</summary>
        public bool RuneCrown;

        /// <summary>¿Lleva EL ANILLO RÚNICO DORSAL (v6.37 — la triple
        /// corona de conjuro del vacío colgada de la espalda)?</summary>
        public bool AnilloDorsal;

        /// <summary>¿Lleva LA CORONA RÚNICA DE AURA (v6.48 — el pentágono
        /// Polígono(5) violeta-oro de AuraLib, por el portador aditivo)?</summary>
        public bool CoronaRunicaAura;

        /// <summary>¿Lleva LA FORMA ASCENDIDA (v6.48 — el aura de la Luz
        /// Primordial, el drop cumplido de Aethon)?</summary>
        public bool FormaAscendida;

        public override void ResetEffects()
        {
            VoidCrown = false;
            RuneCrown = false;
            AnilloDorsal = false;
            CoronaRunicaAura = false;
            FormaAscendida = false;
        }

        public override void PostUpdate()
        {
            // === ESCANEO DE HUECOS: accesorios funcionales (3..9) + de
            // vanidad (13..19) — en cualquier lado cuenta.
            int voidType = ModContent.ItemType<Items.Cosmetics.VoidCrownItem>();
            int runeType = ModContent.ItemType<Items.Cosmetics.RuneCrownItem>();
            int anilloType = ModContent.ItemType<Items.Cosmetics.AnilloRunicoDorsalItem>();
            int coronaAuraType = ModContent.ItemType<Items.Cosmetics.CoronaRunicoAuraItem>();
            int ascendidaType = ModContent.ItemType<Items.Cosmetics.FormaAscendidaItem>();

            for (int i = 3; i <= 19; i++)
            {
                // Salto los huecos de armadura/vanidad de armadura (9..12 no
                // existen como tales: 0-2 armadura, 3-9 accesorios,
                // 10-12 vanidad de armadura, 13-19 vanidad de accesorios).
                if (i >= 10 && i <= 12) continue;

                Item item = Player.armor[i];
                if (item == null || item.IsAir) continue;
                if (item.type == voidType) VoidCrown = true;
                else if (item.type == runeType) RuneCrown = true;
                else if (item.type == anilloType) AnilloDorsal = true;
                else if (item.type == coronaAuraType) CoronaRunicaAura = true;
                else if (item.type == ascendidaType) FormaAscendida = true;
            }

            // v6.50.2 — FIX (host sin sus visuales en Host&Play): mismo error
            // raíz que SellosPlayer — `netMode == Server` también devolvía en
            // el LISTEN SERVER (netMode 1 CON pantalla): el host perdía las
            // chispas/luz de las coronas. "Sin pantalla" es Main.dedServ. El
            // resto del bloque es polvo/luz por pantalla y los halos ya están
            // gateados a whoAmI == Main.myPlayer.
            if (Main.dedServ) return;
            if (Player.dead) return;

            // Centro de la cabeza (respeta la gravedad invertida).
            Vector2 head = Player.Center - new Vector2(0f, Player.height * 0.22f * Player.gravDir);
            float scale = Player.height / 42f;

            // === LA CORONA DE ARCOS VIVE: ascuas rosas que se alzan sobre
            // los ápices (la corona es energía, no un adorno estático).
            if (VoidCrown)
            {
                float horizonPx = Player.width * 0.55f;
                if (Main.rand.NextBool(18))
                {
                    Vector2 emberPos = head + new Vector2(
                        Main.rand.NextFloat(-1.3f, 1.3f) * horizonPx,
                        -horizonPx * (1.1f + Main.rand.NextFloat(0.8f, 2.2f)) * Player.gravDir);
                    Dust d = Dust.NewDustPerfect(emberPos, DustID.Enchanted_Pink,
                        new Vector2(Main.rand.NextFloat(-0.5f, 0.5f),
                                    -Main.rand.NextFloat(0.7f, 1.5f) * Player.gravDir),
                        180, new Color(255, 175, 215), 0.8f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // Luz carmesí suave de la corona.
                Lighting.AddLight(head, new Vector3(0.30f, 0.06f, 0.12f));
            }

            // === LA CORONA RÚNICA ESTELAR (revertida a v6.23 — la que
            //     estaba bien) respira luz: chispas ascendentes desde
            //     las perlas de los glifos.
            if (RuneCrown)
            {
                if (Main.rand.NextBool(28))
                {
                    int g = Main.rand.Next(RuneCrownRenderer.GlyphCount);
                    Vector2 pearl = RuneCrownRenderer.GetPearlPosition(
                        head, scale, Main.GlobalTimeWrappedHourly, g);
                    Dust d = Dust.NewDustPerfect(pearl, DustID.Enchanted_Pink,
                        new Vector2(Main.rand.NextFloat(-0.3f, 0.3f),
                                    -Main.rand.NextFloat(0.5f, 1.1f) * Player.gravDir),
                        160, new Color(255, 200, 220), 0.7f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // Luz rosa tenue del arco rúnico.
                Lighting.AddLight(head - new Vector2(0f, 18f * Player.gravDir),
                    new Vector3(0.22f, 0.04f, 0.14f));
            }

            // === EL ANILLO RÚNICO DORSAL (v6.37): LA CORONA DE CONJURO
            //     de los agujeros negros DETRÁS del cuerpo — el halo
            //     proyectil la dibuja (el dueño local lo invoca; tML lo
            //     sincroniza) y la escritura DORADA respira: chispas que
            //     escapan de las runas EXACTAS del círculo de oro.
            if (AnilloDorsal)
            {
                // EL HALO (la triple corona + el anillo de fotones).
                if (Player.whoAmI == Main.myPlayer && !EspiarHaloDorsal())
                {
                    Projectile.NewProjectile(Player.GetSource_Misc("AnilloRunicoDorsal"),
                        Player.Center, Vector2.Zero,
                        ModContent.ProjectileType<Projectiles.Cosmetic.AnilloRunicoDorsalHalo>(),
                        0, 0f, Player.whoAmI);
                }

                // Chispas doradas desde las runas del círculo de oro (la
                // misma ancla y la misma geometría del halo).
                if (Main.rand.NextBool(36))
                {
                    Vector2 espalda = Player.Center -
                        new Vector2(0f, Player.height * 0.04f * Player.gravDir);
                    int g = Main.rand.Next(OrbitaLib.RunasMedias);
                    Vector2 runa = AnilloDorsalRenderer.RunaWorld(espalda,
                        Player.height, Main.GlobalTimeWrappedHourly, g);
                    Dust d = Dust.NewDustPerfect(runa, DustID.Enchanted_Pink,
                        new Vector2(Main.rand.NextFloat(-0.4f, 0.4f),
                                    -Main.rand.NextFloat(0.4f, 1.0f) * Player.gravDir),
                        160, new Color(255, 214, 140), 0.7f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
            // === LA CORONA RÚNICA DE AURA (v6.48) y LA FORMA ASCENDIDA:
            //     ambas viven por el PORTADOR (AuraPortadorHalo) — el
            //     camino aditivo del halo-proyectil. El dueño local lo
            //     invoca; tML lo sincroniza.
            if (Player.whoAmI == Main.myPlayer)
            {
                if (CoronaRunicaAura && !EspiarPortador(1))
                {
                    Projectile.NewProjectile(Player.GetSource_Misc("CoronaRunicaAura"),
                        Player.Center, Vector2.Zero,
                        ModContent.ProjectileType<Projectiles.Cosmetic.AuraPortadorHalo>(),
                        0, 0f, Player.whoAmI, 1f);
                }
                if (FormaAscendida && !EspiarPortador(2))
                {
                    Projectile.NewProjectile(Player.GetSource_Misc("FormaAscendida"),
                        Player.Center, Vector2.Zero,
                        ModContent.ProjectileType<Projectiles.Cosmetic.AuraPortadorHalo>(),
                        0, 0f, Player.whoAmI, 2f);
                }
            }

            // LA LUZ de la forma ascendida (la luz primordial tibia).
            if (FormaAscendida)
                Lighting.AddLight(Player.Center - new Vector2(0f, 10f),
                    new Vector3(0.22f, 0.17f, 0.08f));
        }

        /// <summary>¿Ya vive mi portador de aura con este modo?</summary>
        private bool EspiarPortador(int modo)
        {
            int tipo = ModContent.ProjectileType<Projectiles.Cosmetic.AuraPortadorHalo>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == Player.whoAmI && p.type == tipo &&
                    (int)p.ai[0] == modo)
                    return true;
            }
            return false;
        }

        /// <summary>¿Ya vive mi halo de la corona rúnica dorsal?</summary>
        private bool EspiarHaloDorsal()
        {
            int tipo = ModContent.ProjectileType<Projectiles.Cosmetic.AnilloRunicoDorsalHalo>();
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
