using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Weapons.Definitivas
{
    // ======================================================================
    //  v6.124 — EL JUICIO DE LOS DOS SOLES, RECONSTRUIDO CON LOS SOLES
    //  RÚNICOS DE LA CASA (la letra del usuario: «el juicio de los dos
    //  soles es raro, no usan los soles runicos que tenemos como base
    //  para sus soles y todavia hay una textura plana y rectangular en
    //  la explocion lo cual es raro»).
    //
    //  LA AUTOPSIA DE LA v6.122: las «esferas» eran UNA ESTRELLA DE 4
    //  PUNTAS de 16 px (Star.png) estirada — no soles; y el destello
    //  final pintaba NovaBurst y DestelloFinal, DOS texturas casi
    //  CUADRADAS llenas (la fila central de la NovaBurst va de borde a
    //  borde a 255) — «la textura plana y rectangular» de la letra.
    //
    //  LA RECONSTRUCCIÓN — TODO ES EL CÓDIGO DE SOLES DEL PROYECTO:
    //    · LOS DOS SOLES SON SOLES RÚNICOS DE VERDAD: el CUERPO SOLAR
    //      es RuneSunRenderer.DrawSunBody (EL MISMO disco de plasma
    //      SunShader con granulación dendrítica, limbo y manchas que
    //      viste a TODOS los soles del mod) y los ANILLOS son
    //      RuneSunRenderer.EmitRingSystem (EL MISMO emisor de SigiloLib
    //      — los aros elípticos con glifos rúnicos cabalgando la órbita
    //      de la familia de soles rúnicos). El SOL BLANCO lleva la
    //      paleta blanco-oro; el SOL NEGRO, la negro-violeta (la lección
    //      v6.101: el negro solo existe si su borde arde — su backglow
    //      violeta y SUS ANILLOS RÚNICOS lo recortan del mundo).
    //    · LA FUSIÓN: los dos soles espiralan uno dentro del otro y
    //      nace EL SOL FUSIONADO — un sol MÁS GRANDE Y DEVASTADOR (×2,1
    //      el cuerpo, el DOBLE de anillos rúnicos) que se hincha y
    //      enrojece mientras carga la sentencia.
    //    · LA EXPLOSIÓN ES LA NOVA DE LOS SOLES DE LA CASA (el patrón
    //      de RuneSunProjectile.OnKill): TODO redondo y orgánico — el
    //      DOBLE círculo de BRUMA dorada (BrumaNacimientoFX, modo 3), la
    //      CHISPA central (ParticlePresets.Explosion), la LLUVIA de
    //      runas de eco doradas, el ANILLO DE CHOQUE (OndaLib.Shock —
    //      la onda circular de la casa) y el sol muriendo EN la luz (su
    //      cuerpo fundiéndose con alphaMul). CERO texturas planas: ni
    //      Star, ni NovaBurst, ni DestelloFinal — el examen de la letra
    //      queda cerrado.
    // ======================================================================
    public class JuicioDosSoles : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 800;               // el ataque DEFINITIVO (la Mirada: 800)
            Item.DamageType = DamageClass.Magic;
            Item.width = 44; Item.height = 44;
            Item.useTime = 60; Item.useAnimation = 60;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;             // la ceremonia se sostiene con el clic
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<JuicioDosSolesProj>();
            Item.shootSpeed = 1f;
            Item.mana = 20;
            Item.ArmorPenetration = 300;
            Item.rare = ItemRarityID.Purple;
            Item.value = 0;
        }

        public override bool CanUseItem(Player player)
        {
            // UN juicio a la vez: mientras la ceremonia viva, no hay otro.
            int tipo = ModContent.ProjectileType<JuicioDosSolesProj>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI && p.type == tipo)
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL PUNTO DE ENCUENTRO: el cursor con correa de 640 px.
            Vector2 punto = Main.MouseWorld;
            float d = Vector2.Distance(player.Center, punto);
            if (d > 640f) punto = player.Center + Vector2.Normalize(punto - player.Center) * 640f;
            Projectile.NewProjectile(source, player.Center, Vector2.Zero, type,
                damage, knockback, player.whoAmI, punto.X, punto.Y);
            return false;
        }

        public override void AddRecipes()
        {
            // EL PROTOCOLO DE LA CASA: la madera de siempre.
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // ======================================================================
    //  JuicioDosSolesProj — LA CEREMONIA ENTERA (los dos soles rúnicos,
    //  la fusión y la nova — un solo proyectil, cuatro actos).
    // ======================================================================
    public class JuicioDosSolesProj : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private const int T_CONVOCATORIA = 36;   // los dos soles vuelan al punto
        private const int T_FUSION = 30;         // espiralan y EL SOL FUSIONADO nace
        private const int T_CARGA = 22;          // el sol fusionado se hincha (la sentencia)
        private const int T_NOVA = 42;           // LA NOVA (la explosión del sol)

        /// <summary>El reloj del acto actual.</summary>
        private float _t;

        /// <summary>0 convocatoria · 1 fusión · 2 carga · 3 nova.</summary>
        private int _acto;

        private Vector2 Punto => new(Projectile.ai[0], Projectile.ai[1]);

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = T_CONVOCATORIA + T_FUSION + T_CARGA + T_NOVA + 10;
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            _t++;

            // EL RELOJ DE LOS ACTOS
            if (_acto == 0 && _t > T_CONVOCATORIA) { _acto = 1; _t = 0; }
            else if (_acto == 1 && _t > T_FUSION) { _acto = 2; _t = 0; }
            else if (_acto == 2 && _t > T_CARGA) { _acto = 3; _t = 0; NacerLaNova(); }
            else if (_acto == 3 && _t > T_NOVA) { Projectile.Kill(); return; }

            Projectile.Center = Punto;

            if (_acto == 0)
            {
                // === ACTO 1 — LA CONVOCATORIA: los DOS SOLES RÚNICOS
                //     vuelan al punto — la luz siembra ORO, la oscuridad
                //     siembra vacío invertido (la firma de siempre) ===
                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    bool esLuz = Main.rand.NextBool();
                    Vector2 semilla = Punto + (esLuz ? new Vector2(-260f, 0f) : new Vector2(260f, 0f)) +
                        Main.rand.NextVector2Circular(60f, 60f);
                    Dust d = Dust.NewDustPerfect(semilla,
                        esLuz ? DustID.GoldFlame : DustID.Shadowflame,
                        -Vector2.Normalize(Punto - semilla) * Main.rand.NextFloat(2f, 5f),
                        0, esLuz ? new Color(255, 226, 150) : new Color(150, 84, 235), 0.9f);
                    d.noGravity = true;
                }
            }
            else if (_acto == 1)
            {
                // === ACTO 2 — LA FUSIÓN: los soles espiralan y TODO el
                //     polvo cercano CAE al centro (la succión de antes,
                //     intacta — el tirón de la casa) ===
                if (!Main.dedServ && Main.rand.NextBool(3))
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    float lejos = Main.rand.NextFloat(180f, 420f);
                    Vector2 pos = Punto + ang.ToRotationVector2() * lejos;
                    Dust d = Dust.NewDustPerfect(pos,
                        Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.Shadowflame,
                        -ang.ToRotationVector2() * (lejos / 14f), 0,
                        new Color(200, 150, 255), 1.1f);
                    d.noGravity = true;
                }
                if (_t == T_FUSION && !Main.dedServ)
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item122.WithPitchOffset(0.35f), Punto);
                    OndaLib.Kick(6f, 14);
                }
            }
            else if (_acto == 2)
            {
                // === ACTO 3 — LA CARGA: el SOL FUSIONADO se hincha — la
                //     gigante de los soles de la casa (enrojece, tiembla
                //     y su luz inunda) ===
                float rg = MathHelper.Clamp(_t / (float)T_CARGA, 0f, 1f);
                if (!Main.dedServ)
                {
                    Lighting.AddLight(Punto, new Vector3(2.2f, 1.8f, 1.2f) * (0.6f + rg));
                    if (Main.rand.NextBool(3))
                        OndaLib.Kick(1 + (int)(4f * rg));
                }
            }
            else
            {
                // === ACTO 4 — LA NOVA: la luz inunda el mundo (2 s de
                //     día dentro del juicio) + la lluvia dorada ===
                if (!Main.dedServ)
                {
                    float vida = Math.Max(0f, 1f - _t / T_NOVA);
                    Lighting.AddLight(Punto, new Vector3(2.6f, 2.3f, 1.6f) * vida);
                    if (_t < 14 && Main.rand.NextBool(2))
                    {
                        Dust d = Dust.NewDustPerfect(Punto, DustID.GoldFlame,
                            Main.rand.NextVector2Circular(11f, 11f), 0,
                            new Color(255, 244, 204), Main.rand.NextFloat(1.1f, 2f));
                        d.noGravity = true;
                    }
                }
            }
        }

        /// <summary>EL NACIMIENTO DE LA NOVA: la onda golpea (una ventana
        /// de 10 t con la hitbox de 920 px — la sentencia) y TODO el
        /// teatro de la nova de los soles de la casa estalla (el patrón
        /// de RuneSunProjectile.OnKill: bruma, chispa, runas de eco).</summary>
        private void NacerLaNova()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.position = Punto - new Vector2(460f, 460f);
                Projectile.width = 920;
                Projectile.height = 920;
                Projectile.netUpdate = true;
            }
            if (!Main.dedServ)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, Punto);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item88, Punto);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Punto);
                OndaLib.Kick(14f, 26);

                // LA NOVA DE BRUMA — el DOBLE círculo de la casa (el
                // patrón del sol rúnico: el círculo COMPLETO al radio de
                // la nova + el corazón MÁS DENSO que se disipa antes).
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(),
                    Punto, Vector2.Zero,
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.BrumaNacimientoFX>(),
                    0, 0f, Projectile.owner, 3f, 460f / 340f);
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(),
                    Punto, Vector2.Zero,
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmic.BrumaNacimientoFX>(),
                    0, 0f, Projectile.owner, 3f, (460f * 0.55f) / 340f);

                // LA CHISPA de la nova (el fogonazo central de la casa).
                ParticlePresets.Explosion(Punto, 300f, 40,
                    new Color(255, 245, 200), new Color(255, 170, 60), 44);

                // LAS RUNAS DE ECO: la nova escupe glifos de luz (la
                // lluvia rúnica del sol de siempre).
                for (int i = 0; i < 26; i++)
                {
                    float angle = (MathHelper.TwoPi / 26f) * i +
                                  Main.rand.NextFloat(-0.08f, 0.08f);
                    Vector2 outward = new((float)Math.Cos(angle), (float)Math.Sin(angle));
                    Dust d = Dust.NewDustPerfect(Punto, DustID.Enchanted_Gold,
                        outward * Main.rand.NextFloat(5f, 12f),
                        255, new Color(255, 235, 170), 1.5f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // LA SENTENCIA NO SE ESQUIVA: el destello es un hecho consumado.
            modifiers.SourceDamage *= 1f;
        }

        // ================================================================
        //  EL DIBUJO — TODO CON EL CÓDIGO DE SOLES DE LA CASA
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            try
            {
                float gt = Main.GlobalTimeWrappedHourly;
                VFXCore.CerrarLoteSiAbierto();

                if (_acto == 0)
                {
                    // === LOS DOS SOLES RÚNICOS volando al punto ===
                    float p = MathHelper.Clamp(_t / T_CONVOCATORIA, 0f, 1f);
                    p = p * p * (3f - 2f * p);
                    for (int lado = 0; lado < 2; lado++)
                        DibujarSolSencillo(lado == 0, p, gt);
                }
                else if (_acto == 1)
                {
                    // === LA FUSIÓN: los dos soles espiralan uno dentro
                    //     del otro, encogiéndose — y al final EL SOL
                    //     FUSIONADO NACE y CRECE (el sol más grande y
                    //     devastador de la letra) ===
                    float p = MathHelper.Clamp(_t / T_FUSION, 0f, 1f);
                    float radio = MathHelper.Lerp(210f, 26f, p);
                    for (int lado = 0; lado < 2; lado++)
                    {
                        bool luz = lado == 0;
                        float ang = gt * 5.5f + lado * MathHelper.Pi + p * 9f;
                        Vector2 pos = Punto + ang.ToRotationVector2() * radio;
                        float r = MathHelper.Lerp(RuneSunRenderer.BodyPx * 0.92f,
                            RuneSunRenderer.BodyPx * 0.34f, p);
                        DibujarSol(luz, pos, r, gt, 8, rg: 0f, alpha: 1f,
                            seed: luz ? 3 : 7, rot: gt * (luz ? 0.4f : -0.4f));
                    }
                    // EL SOL FUSIONADO: nace en el centro a media fusión y
                    // crece hasta su tamaño de sentencia (×2,1 el cuerpo).
                    if (p > 0.45f)
                    {
                        float n = MathHelper.Clamp((p - 0.45f) / 0.55f, 0f, 1f);
                        float r = RuneSunRenderer.BodyPx * MathHelper.Lerp(0.5f, 1.05f, n);
                        DibujarSolFusionado(Punto, r, gt, 12, rg: 0f, alpha: n);
                    }
                }
                else if (_acto == 2)
                {
                    // === LA CARGA: el sol fusionado se HINCHA y enrojece
                    //     (la gigante final de los soles de la casa) ===
                    float rg = MathHelper.Clamp(_t / (float)T_CARGA, 0f, 1f);
                    float r = RuneSunRenderer.BodyPx *
                        (1.05f + 0.55f * rg * rg + 0.12f * MathF.Sin(gt * 18f) * rg);
                    DibujarSolFusionado(Punto, r, gt, 14, rg, alpha: 1f);
                }
                else
                {
                    // === LA NOVA: el sol muere EN la luz — su cuerpo se
                    //     funde (alphaMul) mientras el ANILLO DE CHOQUE
                    //     redondo de la casa se expande. NADA de texturas
                    //     planas: solo el sol, la onda y la bruma ===
                    float p = MathHelper.Clamp(_t / T_NOVA, 0f, 1f);
                    float apaga = 1f - p;
                    float r = RuneSunRenderer.BodyPx *
                        (1.6f + 0.5f * Math.Min(1f, _t / 6f)) * (0.85f + 0.15f * apaga);
                    DibujarSolFusionado(Punto, r, gt, 16, rg: 1f,
                        alpha: MathF.Min(1f, apaga * 1.8f));

                    VFXCore.AbrirAdditive();
                    try
                    {
                        OndaLib.Shock(Main.spriteBatch, Punto - Main.screenPosition,
                            MathHelper.Clamp(_t / 26f, 0f, 1f),
                            1.5f + 4.5f * Math.Min(1f, _t / 7f),
                            new Color(255, 232, 170), 0.8f * apaga, 7, 14f);
                    }
                    finally { VFXCore.RestaurarLoteVanilla(); }
                }

                VFXCore.ReabrirLoteVanilla();
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            return false;
        }

        /// <summary>EL SOL SENCILLO (el blanco o el negro) en su vuelo a
        /// la cita: cuerpo solar + anillos rúnicos + la corona de polvo
        /// de la casa.</summary>
        private void DibujarSolSencillo(bool luz, float progreso, float gt)
        {
            Vector2 origen = Punto + (luz ? new Vector2(-300f, 0f) : new Vector2(300f, 0f));
            Vector2 pos = Vector2.Lerp(origen, Punto, progreso) +
                new Vector2(0f, MathF.Sin(gt * 6f + (luz ? 0f : 3.1f)) * 26f * (1f - progreso));
            DibujarSol(luz, pos, RuneSunRenderer.BodyPx * 0.92f, gt, 8,
                rg: 0f, alpha: 1f, seed: luz ? 3 : 7, rot: gt * (luz ? 0.4f : -0.4f));
        }

        // ================================================================
        //  LOS CUERPOS SOLARES — LA MISMA TÉCNICA DE LA FAMILIA DE SOLES
        //  (DrawSunBody: el disco de plasma SunShader con su granulación
        //  dendrítica + EmitRingSystem: los aros rúnicos de SigiloLib)
        // ================================================================

        /// <summary>Dibuja un sol (blanco o negro) con SU paleta y SUS
        /// anillos rúnicos — la base de soles rúnicos de la casa.</summary>
        private static void DibujarSol(bool luz, Vector2 centroMundo, float r, float gt,
            int anillos, float rg, float alpha, int seed, float rot)
        {
            Vector2 drawPos = centroMundo - Main.screenPosition;
            if (luz)
            {
                // EL SOL BLANCO — blanco-oro (la esfera de luz).
                RuneSunRenderer.DrawSunBody(drawPos, r, rot, gt,
                    new Color(255, 255, 252), new Color(238, 228, 205),
                    new Color(255, 214, 120),
                    new Color(255, 240, 196), new Color(255, 205, 120),
                    new Color(255, 248, 228), 0.9f, alpha);
            }
            else
            {
                // EL SOL NEGRO — negro-violeta (la esfera de oscuridad:
                // el negro solo existe si su borde arde — su backglow
                // violeta lo recorta del mundo).
                RuneSunRenderer.DrawSunBody(drawPos, r, rot, gt,
                    new Color(20, 11, 32), new Color(7, 4, 12),
                    new Color(96, 54, 170),
                    new Color(130, 76, 215), new Color(76, 44, 150),
                    new Color(150, 84, 235), 0.9f, alpha);
            }
            VFXCore.Begin();
            try { RuneSunRenderer.EmitRingSystem(centroMundo, r, gt, seed, anillos, rg, 0f, alpha); }
            finally { VFXCore.FlushAdditive(null, false); }
        }

        /// <summary>EL SOL FUSIONADO — el sol MÁS GRANDE Y DEVASTADOR:
        /// el corazón blanco-oro de la luz con las vetas violetas de la
        /// oscuridad (la paleta de la fusión) y el DOBLE de anillos.</summary>
        private static void DibujarSolFusionado(Vector2 centroMundo, float r, float gt,
            int anillos, float rg, float alpha)
        {
            Vector2 drawPos = centroMundo - Main.screenPosition;
            // la gigante enrojece la paleta (la ley de los soles de la casa)
            Color main = Color.Lerp(new Color(255, 251, 244), new Color(255, 196, 130), rg * 0.8f);
            Color darker = Color.Lerp(new Color(216, 186, 255), new Color(170, 90, 200), rg * 0.8f);
            RuneSunRenderer.DrawSunBody(drawPos, r, gt * 0.5f, gt,
                main, darker, new Color(120, 70, 200),
                Color.Lerp(new Color(255, 238, 205), new Color(255, 170, 90), rg),
                Color.Lerp(new Color(180, 120, 255), new Color(140, 70, 220), rg),
                Color.Lerp(new Color(255, 250, 240), new Color(255, 210, 150), rg),
                0.6f, alpha);
            VFXCore.Begin();
            try { RuneSunRenderer.EmitRingSystem(centroMundo, r, gt, 11, anillos, rg, 0f, alpha); }
            finally { VFXCore.FlushAdditive(null, false); }
        }
    }
}
