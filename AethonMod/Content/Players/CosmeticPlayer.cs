using System;
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

        /// <summary>¿Lleva LA FORMA ASCENDIDA (v6.48 — el aura de la Luz
        /// Primordial, el drop cumplido de Aethon; v6.50.44 — LA CORONA
        /// RÚNICA DE AURA murió fusionada en ella: un solo cosmético
        /// divino)?</summary>
        public bool FormaAscendida;

        /// <summary>¿Lleva LA BRASA DEL ECLIPSE (v6.50.23 — el cuarto tipo
        /// de aura: el patrón Bruma de AuraLib, humo negro en los bordes,
        /// oro en el medio y núcleo rojo, por el portador aditivo+alfa)?</summary>
        public bool BrasaDelEclipse;

        public override void ResetEffects()
        {
            VoidCrown = false;
            RuneCrown = false;
            AnilloDorsal = false;
            FormaAscendida = false;
            BrasaDelEclipse = false;
        }

        public override void PostUpdate()
        {
            // === ESCANEO DE HUECOS: accesorios funcionales (3..9) + de
            // vanidad (13..19) — en cualquier lado cuenta.
            int voidType = ModContent.ItemType<Items.Cosmetics.VoidCrownItem>();
            int runeType = ModContent.ItemType<Items.Cosmetics.RuneCrownItem>();
            int anilloType = ModContent.ItemType<Items.Cosmetics.AnilloRunicoDorsalItem>();
            int ascendidaType = ModContent.ItemType<Items.Cosmetics.FormaAscendidaItem>();
            int brasaType = ModContent.ItemType<Items.Cosmetics.BrasaDelEclipseItem>();

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
                else if (item.type == ascendidaType) FormaAscendida = true;
                else if (item.type == brasaType) BrasaDelEclipse = true;
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
            // === LA FORMA ASCENDIDA (v6.48; v6.50.44 la corona rúnica
            //     murió fusionada en ella): vive por el PORTADOR
            //     (AuraPortadorHalo) — el camino aditivo del
            //     halo-proyectil. El dueño local lo invoca; tML lo
            //     sincroniza.
            if (Player.whoAmI == Main.myPlayer)
            {
                if (FormaAscendida && !EspiarPortador(2))
                {
                    Projectile.NewProjectile(Player.GetSource_Misc("FormaAscendida"),
                        Player.Center, Vector2.Zero,
                        ModContent.ProjectileType<Projectiles.Cosmetic.AuraPortadorHalo>(),
                        0, 0f, Player.whoAmI, 2f);
                }
                if (BrasaDelEclipse && !EspiarPortador(3))
                {
                    Projectile.NewProjectile(Player.GetSource_Misc("BrasaDelEclipse"),
                        Player.Center, Vector2.Zero,
                        ModContent.ProjectileType<Projectiles.Cosmetic.AuraPortadorHalo>(),
                        0, 0f, Player.whoAmI, 3f);
                }
            }

            // === LA FORMA ASCENDIDA — v6.50.44, LA VERSIÓN DIVINA ===
            //     «debe ser un item cosmetico digno de un dios»: el aura
            //     base vive por el PORTADOR (arriba) — y aquí viven sus
            //     CUATRO milagros de polvo y luz.
            if (FormaAscendida)
            {
                Vector2 centro = Player.Center;
                float t = Main.GlobalTimeWrappedHourly;

                // --- LA CORONA DEL ASCENSO: SIETE puntos de luz en arco
                //     sobre la cabeza, respirando (la diadema del dios —
                //     el del centro más alto: el arco lee como corona). ---
                for (int c = 0; c < 7; c++)
                {
                    float f = c / 6f;                                  // 0..1
                    float ang = -MathHelper.PiOver2 + (f - 0.5f) * 2.4f; // arco ±69°
                    float alza = 1f + 0.28f * (1f - MathF.Abs(f - 0.5f) * 2f); // el del centro más alto
                    float r = 26f * scale * alza * (1f + 0.06f * MathF.Sin(t * 2.2f + c));
                    Vector2 punto = head + new Vector2(MathF.Cos(ang) * r, MathF.Sin(ang) * r * Player.gravDir);
                    if (Main.rand.NextBool(10))
                    {
                        Dust d = Dust.NewDustPerfect(punto, DustID.Enchanted_Gold,
                            new Vector2(Main.rand.NextFloat(-0.2f, 0.2f),
                                        -Main.rand.NextFloat(0.2f, 0.5f) * Player.gravDir),
                            200, new Color(255, 238, 170), 0.65f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                }

                // --- EL RAYO DIVINO: polvo de luz CAYENDO DEL CIELO sobre
                //     el portador (la bendición vertical — el dios camina
                //     bajo su propio rayo de sol). ---
                if (Main.rand.NextBool(4))
                {
                    Dust d = Dust.NewDustPerfect(
                        centro + new Vector2(Main.rand.NextFloat(-16f, 16f),
                            -Main.rand.NextFloat(170f, 210f)),
                        DustID.GoldFlame,
                        new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(1.6f, 2.6f)),
                        130, new Color(255, 244, 200), 0.9f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // --- LA HUELLA DE LUZ: al moverse, el suelo queda sembrado
                //     de motas doradas (el rastro del tránsito). ---
                if (Player.velocity.LengthSquared() > 2.2f && Main.rand.NextBool(5))
                {
                    Dust d = Dust.NewDustPerfect(
                        centro + new Vector2(Main.rand.NextFloat(-14f, 14f), Player.height * 0.42f),
                        DustID.GoldFlame,
                        new Vector2(-Player.velocity.X * 0.12f, -Main.rand.NextFloat(0.4f, 1.2f)),
                        150, new Color(255, 236, 160), 0.8f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // --- EL PULSO: cada ~150 t la divinidad RESPIRA — un anillo
                //     de 24 motas expandiéndose desde el portador. ---
                if ((Main.GameUpdateCount % 150u) == 0u)
                {
                    for (int o = 0; o < 24; o++)
                    {
                        float ang = o * MathHelper.TwoPi / 24f;
                        Dust d = Dust.NewDustPerfect(centro, DustID.GoldFlame,
                            new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 3.2f,
                            160, new Color(255, 240, 180), 1.0f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                }

                // --- LA LUZ de la forma ascendida: la luz primordial,
                //     TIBIA y ANCHA (más que nunca — un dios alumbra). ---
                Lighting.AddLight(centro - new Vector2(0f, 10f),
                    new Vector3(0.34f, 0.27f, 0.13f));
            }
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
