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
    //  v6.127 — LA CEREMONIA MEJORADA + LA LEY DE LOS ANILLOS (la letra del
    //  usuario: «la animación actual es insatisfactoria, mejórala» + «máximo
    //  1 anillo rúnico por sol; al fusionarse deben verse 3 anillos»).
    //
    //  LA LEY DE LOS ANILLOS: cada sol sencillo lleva UN SOLO anillo rúnico
    //  (la corona mínima — antes cargaban OCHO cada uno, un mercado de
    //  aros); EL SOL FUSIONADO es el único que merece TRES (la corona
    //  completa de la sentencia — nace con 3, vive con 3, muere con 3).
    //
    //  LA ANIMACIÓN — LA MUERTE DE UNA BINARIA (el guion nuevo, el drama
    //  de verdad — la v6.126 espiralaba a velocidad CONSTANTE y el sol
    //  fusionado nacía a media fusión, sin contacto, sin destello):
    //    ACTO 1 · LA CONVOCATORIA — los dos soles CRECEN mientras vuelan
    //      (nacen chicos, llegan gordos) y dejan ESTELAS de su fuego.
    //    ACTO 2 · LA ESPIRAL DE MUERTE — los soles orbitan su centro común
    //      y la órbita SE CAE: el giro ACELERA (la ley de Kepler — mientras
    //      el radio se hunde, la velocidad sube) hasta el CONTACTO: el
    //      DESTELLO DE FUSIÓN (la luz blanca) y el sol fusionado NACE DEL
    //      CHOQUE — nada nace antes del contacto.
    //    ACTO 3 · LA CARGA — el gigante se hincha, enrojece, TIEMBLA, sus
    //      TRES anillos GIRAN CADA VEZ MÁS RÁPIDO y el polvo cercano CAE
    //      al interior (la succión de la sentencia).
    //    ACTO 4 · LA NOVA — EL COLAPSO: el sol SE CONTRAE de golpe (el
    //      núcleo plegándose, la luz APAGÁNDOSE)… y al fondo del colapso
    //      LA NOVA: la onda, la bruma, las runas — la sentencia.
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
        private const int T_FUSION = 34;         // la espiral de muerte y el contacto
        private const int T_CARGA = 24;          // el sol fusionado se hincha (la sentencia)
        private const int T_NOVA = 46;           // el colapso y LA NOVA

        /// <summary>v6.127 — EL CONTACTO: la fracción de la espiral donde
        /// los dos soles SE TOCAN (el resto del acto es el nacimiento del
        /// gigante — nada nace antes del choque).</summary>
        private const float P_CONTACTO = 0.78f;

        /// <summary>v6.127 — EL COLAPSO: los primeros ticks de la nova en
        /// que el sol se PLEGA antes de estallar.</summary>
        private const int T_COLAPSO = 7;

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
            else if (_acto == 2 && _t > T_CARGA) { _acto = 3; _t = 0; }
            else if (_acto == 3 && _t > T_NOVA) { Projectile.Kill(); return; }

            // v6.127 — LA NOVA ESTALLA AL FONDO DEL COLAPSO (no al empezar
            // el acto: el sol se pliega primero — la sentencia cae cuando
            // el núcleo ya tocó fondo)
            if (_acto == 3 && _t == T_COLAPSO) NacerLaNova();

            Projectile.Center = Punto;

            if (_acto == 0)
            {
                // === ACTO 1 — LA CONVOCATORIA: los DOS SOLES RÚNICOS
                //     CRECEN mientras vuelan a la cita y dejan SU ESTELA
                //     (la luz siembra ORO, la oscuridad VACÍO INVERTIDO) ===
                float p = MathHelper.Clamp(_t / (float)T_CONVOCATORIA, 0f, 1f);
                float suave = p * p * (3f - 2f * p);
                if (!Main.dedServ)
                {
                    for (int lado = 0; lado < 2; lado++)
                    {
                        bool esLuz = lado == 0;
                        Vector2 pos = PosicionSolSencillo(esLuz, suave, Main.GlobalTimeWrappedHourly);
                        Vector2 prev = PosicionSolSencillo(esLuz, Math.Max(0f, suave - 0.06f), Main.GlobalTimeWrappedHourly);
                        Vector2 hacia = pos - prev;
                        if (Main.rand.NextBool(2) && hacia.LengthSquared() > 0.01f)
                        {
                            Dust d = Dust.NewDustPerfect(pos + Main.rand.NextVector2Circular(14f, 14f),
                                esLuz ? DustID.GoldFlame : DustID.Shadowflame,
                                -Vector2.Normalize(hacia) * Main.rand.NextFloat(1.2f, 3f),
                                0, esLuz ? new Color(255, 226, 150) : new Color(150, 84, 235), 0.9f);
                            d.noGravity = true;
                        }
                    }
                }
            }
            else if (_acto == 1)
            {
                // === ACTO 2 — LA ESPIRAL DE MUERTE: la órbita SE CAE, el
                //     giro ACELERA (Kepler) y TODO el polvo cercano cae al
                //     centro; al CONTACTO: el destello y el terremoto ===
                float p = MathHelper.Clamp(_t / (float)T_FUSION, 0f, 1f);
                if (!Main.dedServ)
                {
                    if (Main.rand.NextBool(3))
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
                    // el RETUMBE crece con la caída de la órbita
                    if (p > 0.3f && _t % 4 == 0)
                        OndaLib.Kick(1 + (int)(5f * p), 10);
                    // EL CONTACTO: el destello de fusión (oro + violeta
                    // cruzados — los dos fuegos abrazándose)
                    if (p >= P_CONTACTO && _t <= P_CONTACTO * T_FUSION + 5)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            bool oro = Main.rand.NextBool();
                            float a = Main.rand.NextFloat(MathHelper.TwoPi);
                            Dust d = Dust.NewDustPerfect(Punto,
                                oro ? DustID.GoldFlame : DustID.Shadowflame,
                                a.ToRotationVector2() * Main.rand.NextFloat(4f, 11f), 0,
                                oro ? new Color(255, 240, 190) : new Color(180, 120, 255), 1.4f);
                            d.noGravity = true;
                        }
                    }
                    if (p >= P_CONTACTO && _t <= P_CONTACTO * T_FUSION + 1)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item122.WithPitchOffset(0.35f), Punto);
                        OndaLib.Kick(8f, 18);
                    }
                }
            }
            else if (_acto == 2)
            {
                // === ACTO 3 — LA CARGA: el SOL FUSIONADO se hincha, TIEMBLA,
                //     su luz INUNDA y el polvo CAE al interior (la succión
                //     de la sentencia) ===
                float rg = MathHelper.Clamp(_t / (float)T_CARGA, 0f, 1f);
                if (!Main.dedServ)
                {
                    Lighting.AddLight(Punto, new Vector3(2.2f, 1.8f, 1.2f) * (0.6f + rg));
                    if (Main.rand.NextBool(2))
                    {
                        float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                        float lejos = Main.rand.NextFloat(140f, 380f);
                        Dust d = Dust.NewDustPerfect(Punto + ang.ToRotationVector2() * lejos,
                            Main.rand.NextBool(2) ? DustID.GoldFlame : DustID.Enchanted_Pink,
                            -ang.ToRotationVector2() * (lejos / 26f), 0,
                            Color.Lerp(new Color(255, 226, 150), new Color(255, 150, 90), rg), 1.2f);
                        d.noGravity = true;
                    }
                    if (Main.rand.NextBool(3))
                        OndaLib.Kick(1 + (int)(5f * rg), 12);
                }
            }
            else
            {
                // === ACTO 4 — LA NOVA: primero EL COLAPSO (el núcleo se
                //     pliega, la luz SE APAGA)… y al fondo, la explosión:
                //     la luz INUNDA y la lluvia dorada cae ===
                if (!Main.dedServ)
                {
                    if (_t < T_COLAPSO)
                    {
                        float plega = _t / (float)T_COLAPSO;
                        Lighting.AddLight(Punto, new Vector3(2.2f, 1.8f, 1.2f) * (0.9f - 0.72f * plega));
                    }
                    else
                    {
                        float vida = Math.Max(0f, 1f - (_t - T_COLAPSO) / (T_NOVA - T_COLAPSO));
                        Lighting.AddLight(Punto, new Vector3(2.6f, 2.3f, 1.6f) * vida);
                        if (_t < T_COLAPSO + 14 && Main.rand.NextBool(2))
                        {
                            Dust d = Dust.NewDustPerfect(Punto, DustID.GoldFlame,
                                Main.rand.NextVector2Circular(11f, 11f), 0,
                                new Color(255, 244, 204), Main.rand.NextFloat(1.1f, 2f));
                            d.noGravity = true;
                        }
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
                    // === LOS DOS SOLES RÚNICOS volando a la cita: CRECEN
                    //     mientras vuelan (nacen chicos, llegan gordos) —
                    //     UN SOLO ANILLO cada uno (la ley de la letra) ===
                    float p = MathHelper.Clamp(_t / T_CONVOCATORIA, 0f, 1f);
                    float suave = p * p * (3f - 2f * p);
                    float r = RuneSunRenderer.BodyPx * MathHelper.Lerp(0.5f, 0.92f, suave);
                    for (int lado = 0; lado < 2; lado++)
                    {
                        bool luz = lado == 0;
                        Vector2 pos = PosicionSolSencillo(luz, suave, gt);
                        DibujarSol(luz, pos, r, gt, 1, rg: 0f, alpha: 1f,
                            seed: luz ? 3 : 7, rot: gt * (luz ? 0.4f : -0.4f));
                    }
                }
                else if (_acto == 1)
                {
                    // === LA ESPIRAL DE MUERTE: la órbita SE CAE (empieza
                    //     ancha y serena, el PLUNGE final la plega de
                    //     golpe) y el giro ACELERA (la ley de Kepler — el
                    //     ángulo come el cuadrado del progreso) hasta el
                    //     CONTACTO: el DESTELLO y el gigante NACE DEL
                    //     CHOQUE. UN ANILLO por sol, hasta tocarse ===
                    float p = MathHelper.Clamp(_t / (float)T_FUSION, 0f, 1f);
                    float pc = MathHelper.Clamp(p / P_CONTACTO, 0f, 1f);   // el tramo espiral
                    if (p < P_CONTACTO)
                    {
                        // LA ÓRBITA QUE SE CAE: lenta arriba, el plunge al final
                        float caida = pc * pc * (3f - 2f * pc);
                        caida = MathF.Pow(caida, 1.45f);
                        float radio = MathHelper.Lerp(190f, 24f, caida);
                        // EL GIRO QUE ACELERA (Kepler: ω ∝ 1/r — el cuadrado
                        // del progreso come vueltas)
                        float ang = gt * 1.6f + lado0(gt) + pc * pc * 10.5f;
                        for (int lado = 0; lado < 2; lado++)
                        {
                            bool luz = lado == 0;
                            Vector2 pos = Punto + (ang + (luz ? 0f : MathHelper.Pi)).ToRotationVector2() * radio;
                            float r = RuneSunRenderer.BodyPx * MathHelper.Lerp(0.92f, 0.55f, caida);
                            DibujarSol(luz, pos, r, gt, 1, rg: 0f, alpha: 1f,
                                seed: luz ? 3 : 7, rot: gt * (luz ? 0.4f : -0.4f));
                        }
                    }
                    else
                    {
                        // EL DESTELLO DE CONTACTO (la luz blanca de fusión
                        // — se apaga en lo que queda del acto) + EL
                        // GIGANTE NACIDO DEL CHOQUE (nace grande, se
                        // asienta al tamaño de la sentencia) — TRES ANILLOS
                        float n = MathHelper.Clamp((p - P_CONTACTO) / (1f - P_CONTACTO), 0f, 1f);
                        float r = RuneSunRenderer.BodyPx * MathHelper.Lerp(0.7f, 1.05f, n * n * (3f - 2f * n));
                        DibujarSolFusionado(Punto, r, gt, 3, rg: 0f, alpha: 1f);
                        if (n < 0.85f)
                        {
                            VFXCore.Begin();
                            try
                            {
                                float f = 1f - n / 0.85f;
                                Vector2 dPos = Punto - Main.screenPosition;
                                float fr = 460f * (0.35f + 0.75f * (1f - f));
                                VFXCore.Quad(dPos, SigiloLib.Tint(new Color(255, 250, 235), 0.85f * f * f),
                                    new Vector2(fr, fr), 0f);
                                VFXCore.Quad(dPos, SigiloLib.Tint(new Color(255, 230, 170), 0.5f * f * f * f),
                                    new Vector2(fr * 1.6f, fr * 1.6f), 0f);
                            }
                            finally { VFXCore.FlushAdditive(null, false); }
                        }
                    }
                }
                else if (_acto == 2)
                {
                    // === LA CARGA: el gigante se HINCHA, enrojece, TIEMBLA
                    //     y sus TRES ANILLOS giran cada vez MÁS RÁPIDO (el
                    //     tiempo del sistema se acelera con la carga) ===
                    float rg = MathHelper.Clamp(_t / (float)T_CARGA, 0f, 1f);
                    float r = RuneSunRenderer.BodyPx *
                        (1.05f + 0.55f * rg * rg + 0.12f * MathF.Sin(gt * 18f) * rg);
                    DibujarSolFusionado(Punto, r, gt * (1f + 1.8f * rg), 3, rg, alpha: 1f);
                }
                else
                {
                    // === LA NOVA — EL COLAPSO Y LA LUZ: primero el sol SE
                    //     PLEGA (se contrae y APAGA — el núcleo tocando
                    //     fondo)… y al fondo del colapso ESTALLA: se
                    //     hincha hasta el doble, se funde en la luz y el
                    //     ANILLO DE CHOQUE redondo de la casa barre el
                    //     mundo. TRES ANILLOS hasta el final ===
                    if (_t < T_COLAPSO)
                    {
                        float plega = _t / (float)T_COLAPSO;
                        float r = RuneSunRenderer.BodyPx * MathHelper.Lerp(1.6f, 0.7f, plega * plega);
                        DibujarSolFusionado(Punto, r, gt * 2.8f, 3, rg: 1f,
                            alpha: 1f - 0.45f * plega);
                    }
                    else
                    {
                        float p = MathHelper.Clamp((_t - T_COLAPSO) / (float)(T_NOVA - T_COLAPSO), 0f, 1f);
                        float apaga = 1f - p;
                        float r = RuneSunRenderer.BodyPx *
                            (0.7f + 1.45f * Math.Min(1f, (_t - T_COLAPSO) / 7f)) * (0.85f + 0.15f * apaga);
                        DibujarSolFusionado(Punto, r, gt * 3.2f, 3, rg: 1f,
                            alpha: MathF.Min(1f, apaga * 1.8f));

                        VFXCore.AbrirAdditive();
                        try
                        {
                            OndaLib.Shock(Main.spriteBatch, Punto - Main.screenPosition,
                                MathHelper.Clamp((_t - T_COLAPSO) / 26f, 0f, 1f),
                                1.5f + 4.5f * Math.Min(1f, (_t - T_COLAPSO) / 7f),
                                new Color(255, 232, 170), 0.8f * apaga, 7, 14f);
                        }
                        finally { VFXCore.RestaurarLoteVanilla(); }
                    }
                }

                VFXCore.ReabrirLoteVanilla();
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            return false;
        }

        /// <summary>v6.127 — el ángulo base de la espiral (el arranque
        /// sereno del par — determinista, del reloj global).</summary>
        private static float lado0(float gt) => gt * 0.2f;

        /// <summary>v6.127 — LA POSICIÓN DEL SOL SENCILLO durante la
        /// convocatoria (el MISMO camino para la IA de las estelas y el
        /// PreDraw — una sola verdad): desde su lado (±300 px) con el
        /// cabeceo sereno de siempre, al punto de la cita.</summary>
        private Vector2 PosicionSolSencillo(bool luz, float suave, float gt)
        {
            Vector2 origen = Punto + (luz ? new Vector2(-300f, 0f) : new Vector2(300f, 0f));
            return Vector2.Lerp(origen, Punto, suave) +
                new Vector2(0f, MathF.Sin(gt * 6f + (luz ? 0f : 3.1f)) * 26f * (1f - suave));
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
        /// oscuridad (la paleta de la fusión) y SUS TRES ANILLOS (la ley
        /// de la letra: solo el gigante merece la corona completa).</summary>
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
