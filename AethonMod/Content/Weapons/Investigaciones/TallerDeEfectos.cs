using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using ReLogic.Content;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Weapons.Investigaciones
{
    // ======================================================================
    //  v6.90 — EL TALLER DE EFECTOS (la investigación de los reels:
    //  patch-gg/pixelfx — 244 efectos pixel art «cada uno un trocito de
    //  código», MIT, con su skill de Claude Code que los pule con el
    //  método «borrador, render, mirar, refinar» de Hugo Duprez · y
    //  aldegad/sprite-gen — sprites animados listos para el motor).
    //
    //  LA LETRA: «el segundo es un proyecto de efectos VFX, analizalo
    //  bien y dime como se podria aplicar a nuestro proyecto» + «crea
    //  una bolsa nueva y dale la bolsa al jugador, esta bolsa tendras
    //  una copia de todas las armas que sean creadas con esta
    //  investigacion».
    //
    //  LO QUE LA INVESTIGACIÓN ENSEÑÓ (y estas armas aplican):
    //   · CADA EFECTO ES CÓDIGO, NO PNG (pixelfx: «every one a small
    //     piece of code») — las tres armas dibujan TODO su universo
    //     proceduralmente (la geometría del sigilo, el fBm de las
    //     llamas, la corona del eclipse): cero texturas de ataduras.
    //   · LOS ACTOS (pixelfx: «a hit needs a body behind it… wind-up,
    //     freeze on contact, recovery»): el sigilo TIENE su acto — se
    //     TRAZA (wind-up), DETONA congelándose un instante (freeze) y
    //     se DESMORONA en ascuas (recovery).
    //   · EL BORRADOR-RENDER-MIRAR-REFINAR: cada arma pasó por su
    //     contacto visual (VLM sobre el render de sus constantes).
    //
    //  LAS TRES (las mejores puntuadas del catálogo shadow/arcane de
    //  pixelfx por la inspección visual, adaptadas al cosmos del
    //  Eterno — armas de PRUEBA: 0 maná, receta de 5 maderas):
    //   · EL SIGILO DEL ETERNO (shadow/hex-sigil): la pluma ardiente
    //     traza un sigilo maldito en el aire, sus glifos estallan uno
    //     a uno, un destello blanco y se desmorona en ascuas.
    //   · LAS LLAMAS DE SOMBRA (shadow/shadow-flames): la columna de
    //     fuego sombrío violeta que sube viva del suelo (fBm real).
    //   · EL ECLIPSE VIVO (shadow/eclipse): el disco negro de corona
    //     blanca que se abre como una nova y lo ciega todo.
    // ======================================================================

    // ======================================================================
    //  1) EL SIGILO DEL ETERNO — el trazado, los glifos, el destello
    // ======================================================================
    public class SigiloDelEterno : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.damage = 46;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 0;                       // arma de PRUEBA (la casa)
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.noMelee = true;
            Item.noUseGraphic = false;
            Item.knockBack = 4f;
            Item.shoot = ModContent.ProjectileType<SigiloDelEternoProj>();
            Item.shootSpeed = 12f;
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
            Item.UseSound = SoundID.Item8;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Taller",
                "[c/78788C:El taller de efectos — la pluma del Eterno traza su sigilo y lo detona]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    public class SigiloDelEternoProj : ModProjectile
    {
        // EL ACTO (los tiempos del pixelfx hex-sigil: 4,2 s a 30 fps —
        // aquí 126 ticks: TRAZO 0..80 · GLIFOS 80..104 · DESTELLO 104 · ASCUAS).
        private const int F_TRAZO = 80;
        private const int F_GLIFOS = 104;
        private const int F_DESTELLO = 104;
        private const float RADIO = 78f;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 500;
        }

        public override void SetDefaults()
        {
            Projectile.width = (int)(RADIO * 2);
            Projectile.height = (int)(RADIO * 2);
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;   // UN solo golpe (el destello)
        }

        /// <summary>EL TRAZO DEL SIGILO (la geometría del pixelfx: el
        /// círculo, la estrella de 7 puntas de UNA stroke, el círculo
        /// íntimo y el nudo del centro) — puntos con SU momento (t).</summary>
        private static void ElTrazo(out Vector2[] pts, out float[] ts)
        {
            var puntos = new List<Vector2>();
            var momentos = new List<float>();
            float t = 0.08f;
            // EL CÍRCULO EXTERIOR (rápido).
            const int NC = 46;
            for (int i = 0; i <= NC; i++)
            {
                float a = -MathHelper.PiOver2 + i / (float)NC * MathHelper.TwoPi;
                puntos.Add(new Vector2(MathF.Cos(a), MathF.Sin(a)) * RADIO);
                momentos.Add(t + i / (float)NC * 0.30f);
            }
            t += 0.34f;
            // LA ESTRELLA DE 7 (una sola stroke: {i·3 mod 7} — tres
            // puntas por vuelta: la estrella de la heptagrama {7/3}).
            for (int i = 0; i <= 7; i++)
            {
                float a = -MathHelper.PiOver2 + ((i * 3) % 7) * (MathHelper.TwoPi / 7f);
                puntos.Add(new Vector2(MathF.Cos(a), MathF.Sin(a)) * (RADIO * 0.62f));
                momentos.Add(t + i / 7f * 0.34f);
            }
            t += 0.38f;
            // EL NUDO DEL CENTRO (el rombo del pixelfx).
            Vector2[] rombo = { new(0, -14f), new(11f, 0), new(0, 14f), new(-11f, 0), new(0, -14f) };
            for (int i = 0; i < rombo.Length; i++)
            {
                puntos.Add(rombo[i]);
                momentos.Add(t + i / 4f * 0.08f);
            }
            pts = puntos.ToArray();
            ts = momentos.ToArray();
        }

        public override void AI()
        {
            Projectile.velocity *= 0f;   // el sigilo VIVE en su sitio
            int fase = (int)Projectile.ai[0];
            Projectile.ai[1]++;

            // LA LUZ (la firma de la casa).
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                Vector3 luz = fase >= 1 ? new Vector3(0.75f, 0.45f, 0.95f)
                    : new Vector3(0.45f, 0.22f, 0.65f);
                Lighting.AddLight(Projectile.Center, luz * 0.9f);
            }

            // EL CAMBIO DE FASE.
            float t = Projectile.ai[1];
            if (fase == 0 && t >= F_TRAZO) { Projectile.ai[0] = 1; Sonar(SoundID.Item4, 0.6f); }
            if (fase == 1 && t >= F_GLIFOS) { Projectile.ai[0] = 2; LaDetonacion(); }

            // EL TEATRO POR FASE (los polvos del pixelfx).
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                if (fase == 0 && Projectile.ai[1] % 3f == 0f)
                {
                    // LA PLUMA ARDIENDO: una chispa en la punta del trazo.
                    Vector2[] pts; float[] ts; ElTrazo(out pts, out ts);
                    float prog = Projectile.ai[1] / (float)F_TRAZO;
                    for (int i = 0; i < pts.Length; i++)
                    {
                        if (ts[i] > prog) break;
                        if (i + 1 >= pts.Length || ts[i + 1] > prog)
                        {
                            Dust d = Dust.NewDustPerfect(Projectile.Center + pts[i],
                                DustID.Shadowflame, Vector2.Zero, 120,
                                new Color(196, 120, 255), 0.65f);
                            d.noGravity = true;
                        }
                    }
                }
                if (fase == 1 && Projectile.ai[1] % 4f == 0f)
                {
                    // LOS GLIFOS ESTALLAN: chispas en el anillo.
                    float a = Main.rand.NextFloat(MathHelper.TwoPi);
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + a.ToRotationVector2() * (RADIO * 0.82f),
                        DustID.PortalBoltTrail, a.ToRotationVector2() * 1.4f, 120,
                        new Color(235, 220, 255), 0.8f);
                    d.noGravity = true;
                }
                if (fase == 2)
                {
                    // LAS ASCUAS QUE CAEN (el crumble del pixelfx).
                    for (int k = 0; k < 2; k++)
                    {
                        Dust d = Dust.NewDustPerfect(
                            Projectile.Center + Main.rand.NextVector2Circular(RADIO * 0.8f, RADIO * 0.8f),
                            Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch,
                            new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(0.5f, 2.2f)),
                            130, default, 0.8f);
                        d.noGravity = false;
                    }
                }
            }
        }

        /// <summary>EL DESTELLO: un instante congelado y UN golpe radial
        /// (el freeze-on-contact del acto pixelfx: todo lo que está
        /// DENTRO del sigilo recibe el daño UNA sola vez).</summary>
        private void LaDetonacion()
        {
            Sonar(SoundID.Item74.WithPitchOffset(-0.35f), 1.05f);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                foreach (NPC n in Main.ActiveNPCs)
                {
                    if (n == null || !n.active || !VFXCore.EsObjetivo(n)) continue;
                    if (n.Center.DistanceSQ(Projectile.Center) > RADIO * RADIO) continue;
                    try
                    {
                        n.StrikeNPC(new NPC.HitInfo
                        {
                            Damage = Projectile.damage,
                            SourceDamage = Projectile.damage,
                            DamageType = DamageClass.Magic,
                            Crit = false,
                            HitDirection = Math.Sign(n.Center.X - Projectile.Center.X),
                            Knockback = 5f,
                        }, false, false);
                    }
                    catch { }
                }
            }
        }

        private static void Sonar(SoundStyle s, float vol)
        {
            try { SoundEngine.PlaySound(s.WithVolumeScale(vol), Main.LocalPlayer?.Center ?? Vector2.Zero); }
            catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;
            int fase = (int)Projectile.ai[0];
            float t = Projectile.ai[1];
            Vector2 c = Projectile.Center - Main.screenPosition;

            VFXCore.AbrirAdditive();
            try
            {
                Texture2D glow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow", AssetRequestMode.ImmediateLoad).Value;
                Texture2D estrella = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/FlareEstrella4Puntas", AssetRequestMode.ImmediateLoad).Value;

                // === FASE 0·1 — EL TRAZO (la pluma que dibuja) ===
                if (fase <= 1)
                {
                    float prog = fase == 1 ? 1f : MathHelper.Clamp(t / F_TRAZO, 0f, 1f);
                    Vector2[] pts; float[] ts; ElTrazo(out pts, out ts);
                    // EL CAMINO YA TRAZADO (los segmentos hasta prog).
                    for (int i = 1; i < pts.Length; i++)
                    {
                        if (ts[i] > prog) break;
                        float tramo = MathHelper.Clamp((prog - ts[i - 1]) /
                            MathF.Max(ts[i] - ts[i - 1], 0.0001f), 0f, 1f);
                        Vector2 a = c + Vector2.Lerp(pts[i - 1], pts[i], tramo == 1f ? 1f : 0f);
                        Vector2 b = c + pts[i];
                        if (tramo < 1f) b = c + Vector2.Lerp(pts[i - 1], pts[i], tramo);
                        // EL FUEGO DEL TRAZO: la línea viva de glowcitos.
                        float pasos = MathF.Max(1f, (b - a).Length() / 5f);
                        for (int p = 0; p <= (int)pasos; p++)
                        {
                            Vector2 pos = Vector2.Lerp(a, b, p / pasos);
                            Main.spriteBatch.Draw(glow, pos, null,
                                new Color(196, 110, 255) * 0.35f, 0f,
                                glow.Size() * 0.5f, new Vector2(0.16f), SpriteEffects.None, 0f);
                        }
                    }
                    // LA PUNTA DE LA PLUMA (la chispa que avanza).
                    for (int i = 0; i < pts.Length; i++)
                    {
                        if (ts[i] > prog) break;
                        if (i + 1 >= pts.Length || ts[i + 1] > prog)
                        {
                            Main.spriteBatch.Draw(estrella, c + pts[i], null,
                                new Color(255, 235, 255) * 0.9f, Main.GlobalTimeWrappedHourly * 3f,
                                estrella.Size() * 0.5f, new Vector2(0.45f), SpriteEffects.None, 0f);
                        }
                    }
                }

                // === FASE 1 — LOS GLIFOS (el anillo rúnico de la casa,
                //     7 runas que estallan una a una — el alfabeto propio) ===
                if (fase >= 1 && fase <= 2)
                {
                    float pop = fase == 2 ? 1f
                        : MathHelper.Clamp((t - F_TRAZO) / (F_GLIFOS - F_TRAZO), 0f, 1f);
                    OrbitaLib.CirculoRunico(c, 1.32f, Main.GlobalTimeWrappedHourly * 0.9f,
                        RADIO, 7, 0.9f, new Color(196, 110, 255), new Color(255, 240, 255),
                        1.05f, alphaMul: 0.35f + 0.65f * pop, ringAlpha: 0.20f);
                }

                // === FASE 2 — EL DESTELLO Y EL DESMORONAMIENTO ===
                if (fase == 2)
                {
                    float d = t - F_DESTELLO;                     // 0..46
                    // EL FLASH (el primer instante, cegador).
                    float flash = MathF.Max(0f, 1f - d / 8f);
                    if (flash > 0f)
                    {
                        Main.spriteBatch.Draw(estrella, c, null,
                            Color.White * (flash * 0.95f), 0f, estrella.Size() * 0.5f,
                            new Vector2(2.6f * flash + 0.4f), SpriteEffects.None, 0f);
                        Main.spriteBatch.Draw(glow, c, null,
                            new Color(255, 220, 255) * (flash * 0.55f), 0f, glow.Size() * 0.5f,
                            new Vector2(2.2f * flash), SpriteEffects.None, 0f);
                    }
                    // EL ANILLO QUE SE LLEVA TODO (la onda del destello).
                    float anillo = MathHelper.Clamp(d / 18f, 0f, 1f);
                    float alfaAnillo = MathF.Max(0f, 1f - anillo);
                    if (alfaAnillo > 0f)
                        OrbitaLib.AnilloFino(c, RADIO * (0.4f + anillo * 1.5f),
                            Main.GlobalTimeWrappedHourly * 2f, new Color(230, 190, 255) * (alfaAnillo * 0.8f));
                    // EL SIGILO QUE SE APAGA (el trazo entero, muriendo).
                    float muerte = MathHelper.Clamp(d / 40f, 0f, 1f);
                    Vector2[] pts; float[] ts; ElTrazo(out pts, out ts);
                    for (int i = 1; i < pts.Length; i += 2)
                    {
                        if (Main.rand.NextFloat() < muerte) continue;
                        Vector2 pos = c + pts[i];
                        Main.spriteBatch.Draw(glow, pos, null,
                            new Color(150, 80, 210) * ((1f - muerte) * 0.22f), 0f,
                            glow.Size() * 0.5f, new Vector2(0.13f), SpriteEffects.None, 0f);
                    }
                }
            }
            catch { }
            finally { VFXCore.RestaurarLoteVanilla(); }
            return false;
        }
    }

    // ======================================================================
    //  2) LAS LLAMAS DE SOMBRA — la columna de fuego sombrío (fBm real)
    // ======================================================================
    public class LlamasDeSombra : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.damage = 34;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 0;                       // arma de PRUEBA (la casa)
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 26;
            Item.useAnimation = 26;
            Item.noMelee = true;
            Item.knockBack = 2.5f;
            Item.shoot = ModContent.ProjectileType<LlamasDeSombraProj>();
            Item.shootSpeed = 14f;
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
            Item.UseSound = SoundID.Item20;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Taller",
                "[c/78788C:El taller de efectos — el fuego sombrío del Eterno sube vivo del suelo]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    public class LlamasDeSombraProj : ModProjectile
    {
        private const float ALTO = 230f;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 500;
        }

        public override void SetDefaults()
        {
            Projectile.width = 54;
            Projectile.height = (int)ALTO;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 110;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void AI()
        {
            Projectile.velocity *= 0f;
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, new Vector3(0.42f, 0.18f, 0.62f) * 0.85f);
                // LAS ASCUAS QUE SUBEN (las shadow-flames del pixelfx).
                if (Main.rand.NextBool(2))
                {
                    float crec = MathHelper.Clamp(Projectile.ai[0] / 20f, 0f, 1f);
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + new Vector2(
                            Main.rand.NextFloat(-22f, 22f) * crec,
                            Main.rand.NextFloat(ALTO * 0.5f * crec, ALTO * crec)),
                        DustID.Shadowflame,
                        new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), -Main.rand.NextFloat(1.2f, 2.8f)),
                        130, new Color(150, 70, 220), Main.rand.NextFloat(0.7f, 1.2f));
                    d.noGravity = true;
                }
            }
            Projectile.ai[0]++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;
            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow", AssetRequestMode.ImmediateLoad).Value;

            float crec = MathHelper.Clamp(Projectile.ai[0] / 18f, 0f, 1f);   // nace en ⅓ s
            float muerte = MathHelper.Clamp((Projectile.timeLeft < 20 ? 20 - Projectile.timeLeft : 0) / 20f, 0f, 1f);
            float vida = crec * (1f - muerte);
            float reloj = Main.GlobalTimeWrappedHourly;

            // LA COLUMNA: 22 nodos verticales, el ANCHO respira con el
            // fBm de la casa (WarpedFbm — el fuego sombrío VIVO).
            const int N = 22;
            Vector2[] pts = new Vector2[N];
            float[] semis = new float[N];
            Color[] cols = new Color[N];
            for (int i = 0; i < N; i++)
            {
                float y = i / (float)(N - 1);
                pts[i] = Projectile.Center - Main.screenPosition - new Vector2(0f, ALTO * 0.5f * vida * y) +
                    new Vector2(MathF.Sin(reloj * 2.2f + y * 5f) * 3.5f * y, 0f);
                float n = global::AethonMod.Content.Effects.Bruma.BrumaNoise
                    .WarpedFbm(reloj * 1.6f, y * 3.1f + Projectile.ai[1] * 0.021f, 4242, 3f, 4);
                float anchoBase = 30f * (1f - y * 0.35f);
                semis[i] = MathF.Max(2f, anchoBase * (0.55f + 0.75f * n)) * vida;
                // LA PALETA: violeta profundo abajo → magenta arriba.
                Color c = Color.Lerp(new Color(96, 28, 168), new Color(210, 70, 240), y);
                cols[i] = c * (0.30f + 0.22f * n) * vida;
            }

            VFXCore.AbrirAdditive();
            try
            {
                // EL CUERPO DE LA COLUMNA (la faja con el ruido de la casa).
                RayoStrip.FajaTexturadaEnLote(pts, semis, cols, default,
                    TexRuidoLlamas, 140f, 0.8f, reloj);
                // EL NÚCLEO BLANCO-VIOLETA (la vena caliente).
                for (int i = 0; i < N; i++)
                    semis[i] *= 0.34f;
                for (int i = 0; i < N; i++)
                    cols[i] = Color.Lerp(new Color(255, 240, 255), new Color(190, 120, 255), 0.4f)
                        * (cols[i].A / 255f * 0.9f);
                RayoStrip.FajaTexturadaEnLote(pts, semis, cols, default,
                    ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow",
                        AssetRequestMode.ImmediateLoad).Value, 140f, 0f, reloj);
                // LA BASE: el charco de bruma que respira en el suelo.
                Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition + new Vector2(0f, 4f), null,
                    new Color(110, 40, 180) * (0.30f * vida), 0f, glow.Size() * 0.5f,
                    new Vector2(1.5f * vida, 0.55f * vida), SpriteEffects.None, 0f);
            }
            catch { }
            finally { VFXCore.RestaurarLoteVanilla(); }
            return false;
        }

        /// <summary>EL RUIDO DE LAS LLAMAS (128², el WarpedFbm de la casa
        /// en gris × el perfil V de la rampa: los bordes se funden).</summary>
        private static Texture2D _texRuido;
        internal static Texture2D TexRuidoLlamas
        {
            get
            {
                if (_texRuido != null && !_texRuido.IsDisposed) return _texRuido;
                const int L = 128;
                var data = new Color[L * L];
                for (int y = 0; y < L; y++)
                {
                    float v = y / (float)(L - 1);
                    float dv = v - 0.5f;
                    float perfil = MathF.Exp(-(dv * dv) / (2f * 0.20f * 0.20f));
                    for (int x = 0; x < L; x++)
                    {
                        float n = global::AethonMod.Content.Effects.Bruma.BrumaNoise
                            .WarpedFbm(x * 0.06f, y * 0.06f, 777, 3f, 4);
                        byte g = (byte)(MathHelper.Clamp((0.35f + n * 0.45f) * perfil, 0f, 1f) * 255f);
                        data[y * L + x] = new Color(g, g, g, 255);
                    }
                }
                _texRuido = new Texture2D(Main.graphics.GraphicsDevice, L, L);
                _texRuido.SetData(data);
                return _texRuido;
            }
        }
    }

    // ======================================================================
    //  3) EL ECLIPSE VIVO — la nova del disco negro de corona blanca
    // ======================================================================
    public class EclipseVivo : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.damage = 58;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 0;                       // arma de PRUEBA (la casa)
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 32;
            Item.useAnimation = 32;
            Item.noMelee = true;
            Item.knockBack = 6f;
            Item.shoot = ModContent.ProjectileType<EclipseVivoProj>();
            Item.shootSpeed = 12f;
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
            Item.UseSound = SoundID.Item72;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Taller",
                "[c/78788C:El taller de efectos — el disco negro de corona blanca: una nova que lo ciega todo]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    public class EclipseVivoProj : ModProjectile
    {
        private const float RADIO_FINAL = 300f;
        private const int DURACION = 46;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 500;
        }

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = DURACION + 10;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        /// <summary>EL RADIO ACTUAL (la nova se abre: 30 → 300 en 30
        /// ticks con ease-out, y MUERE suave).</summary>
        private float RadioAhora
        {
            get
            {
                float d = DURACION + 10 - Projectile.timeLeft;
                float abre = MathHelper.Clamp(d / 30f, 0f, 1f);
                float ease = 1f - MathF.Pow(1f - abre, 2.2f);
                float muerte = MathHelper.Clamp((Projectile.timeLeft - 10) / 10f, 0f, 1f);
                return MathHelper.Lerp(26f, RADIO_FINAL, ease) * muerte;
            }
        }

        public override void AI()
        {
            Projectile.velocity *= 0f;
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, new Vector3(0.8f, 0.7f, 1f) * 0.8f);
                // LOS RAYOS QUE SE ESCAPAN (los destellos de la corona).
                if (Main.rand.NextBool(3))
                {
                    float a = Main.rand.NextFloat(MathHelper.TwoPi);
                    float r = RadioAhora;
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + a.ToRotationVector2() * r,
                        Main.rand.NextBool() ? DustID.AmberBolt : DustID.PortalBoltTrail,
                        a.ToRotationVector2() * Main.rand.NextFloat(2f, 5f), 140,
                        new Color(255, 246, 220), 0.75f);
                    d.noGravity = true;
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // EL ANILLO DE LA NOVA (solo el borde: 0,78..1,0 del radio).
            float r = RadioAhora;
            float dist = targetHitbox.Center().DistanceSQ(Projectile.Center);
            float exterior = (r + MathF.Max(targetHitbox.Width, targetHitbox.Height) * 0.5f);
            float interior = r * 0.78f;
            return dist <= exterior * exterior && dist >= interior * interior;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;
            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow", AssetRequestMode.ImmediateLoad).Value;
            Texture2D rayo = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/GlowRay", AssetRequestMode.ImmediateLoad).Value;

            float r = RadioAhora;
            Vector2 c = Projectile.Center - Main.screenPosition;
            float reloj = Main.GlobalTimeWrappedHourly;

            // === EL DISCO NEGRO (primero en el lote NORMAL: el negro NO
            //     vive en el aditivo — el disco se DIBUJA, tapando) ===
            try
            {
                VFXCore.CerrarLoteSiAbierto();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                    null, Main.Transform);
                // EL DISCO (negro-violeta casi opaco) + SU LIMBO.
                Main.spriteBatch.Draw(glow, c, null, new Color(16, 8, 26, 235),
                    0f, glow.Size() * 0.5f,
                    new Vector2(r * 0.86f) / glow.Size(), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(glow, c, null, new Color(30, 14, 44, 210),
                    0f, glow.Size() * 0.5f,
                    new Vector2(r * 0.98f) / glow.Size(), SpriteEffects.None, 0f);
                Main.spriteBatch.End();
            }
            catch { }
            finally { VFXCore.RestaurarLoteVanilla(); }

            // === LA CORONA (el aditivo): los rayos que giran + el limbo
            //     blanco + el halo violeta ===
            VFXCore.AbrirAdditive();
            try
            {
                // 14 RAYOS que giran lento y respiran (la corona solar).
                for (int k = 0; k < 14; k++)
                {
                    float a = k / 14f * MathHelper.TwoPi + reloj * 0.55f;
                    float len = r * (0.55f + 0.45f * MathF.Sin(reloj * 3.1f + k * 2.4f));
                    float ancho = 0.5f + 0.3f * MathF.Sin(reloj * 2.2f + k);
                    Main.spriteBatch.Draw(rayo, c, null,
                        new Color(255, 248, 230) * 0.55f, a - MathHelper.PiOver2,
                        new Vector2(rayo.Width * 0.5f, rayo.Height),
                        new Vector2(ancho, len / rayo.Height), SpriteEffects.None, 0f);
                }
                // EL LIMBO BLANCO (el borde del disco que quema).
                OrbitaLib.AnilloFino(c, r * 0.92f, reloj * 0.4f, new Color(255, 250, 240) * 0.85f);
                OrbitaLib.AnilloFino(c, r * 0.99f, -reloj * 0.3f, new Color(210, 170, 255) * 0.5f);
                // EL HALO (la bruma que lo envuelve).
                Main.spriteBatch.Draw(glow, c, null, new Color(150, 110, 220) * 0.30f,
                    0f, glow.Size() * 0.5f, new Vector2(r * 1.25f) / glow.Size(),
                    SpriteEffects.None, 0f);
                // EL NÚCLEO BLANCO del nacimiento (la nova naciendo).
                float d = DURACION + 10 - Projectile.timeLeft;
                float nace = MathF.Max(0f, 1f - d / 10f);
                if (nace > 0f)
                    Main.spriteBatch.Draw(glow, c, null, Color.White * (nace * 0.8f),
                        0f, glow.Size() * 0.5f, new Vector2(1.4f * nace + 0.2f),
                        SpriteEffects.None, 0f);
            }
            catch { }
            finally { VFXCore.RestaurarLoteVanilla(); }
            return false;
        }
    }
}
