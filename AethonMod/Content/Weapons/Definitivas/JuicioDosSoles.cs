using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Weapons.Definitivas
{
    // ======================================================================
    //  v6.122 — EL JUICIO DE LOS DOS SOLES (la letra del usuario: «usa la
    //  superforja para crear un arma con ataque definitivo de luz y
    //  oscuridad, quiero dos esferas de luz y oscuridad colisionen y
    //  creen una implosion que luego explota en un destello de luz»).
    //
    //  EL ATAQUE DEFINITIVO DE LUZ Y OSCURIDAD — el guion en TRES actos:
    //    1. LA CONVOCATORIA (30 t): DOS ESFERAS nacen a los lados del
    //       portador — la ESFERA DE LUZ (blanco-dorado, chispas de oro)
    //       y la ESFERA DE OSCURIDAD (núcleo negro con el borde ardiendo
    //       violeta — la lección v6.101: el negro solo existe si su
    //       borde arde) — y vuelan al PUNTO DE ENCUENTRO (el cursor, con
    //       correa de 640 px).
    //    2. LA IMPLOSIÓN (26 t): las dos esferas ESPIRALAN una dentro de
    //       la otra, encogiéndose — el remolino de la casa gira y TIRA
    //       del polvo hacia adentro (la luz se ATRAGANTA), un latido
    //       negro al final: el punto donde la luz y la oscuridad se
    //       comen mutuamente.
    //    3. EL DESTELLO (44 t): LA EXPLOSIÓN DE LUZ — la NovaBurst de la
    //       casa naciendo de CERO a TAMAÑO DIOS, el anillo de choque de
    //       OndaLib expandiéndose, los rayos del DestelloFinal, la lluvia
    //       de chispas doradas, la luz que inunda el mundo y la sacudida
    //       de cámara. La onda golpea UNA VEZ (ventana de 10 t, radio
    //       460): la sentencia de los dos soles.
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
    //  JuicioDosSolesProj — v6.124 — LA CEREMONIA CON SOLES DE VERDAD (la
    //  letra: «ya que son soles hubieras usado nuestro codigo de soles
    //  para crear esos 2 soles, creando uno blanco y otro negro y asi
    //  fucionarlos en una explosion creando uno aun mas grande y
    //  debastador y luego explotando» + «todo proyectil debe ser creado
    //  con la superforja, en este caso con la nueva que sera creada
    //  SForja»).
    //
    //  LOS SOLES SON SPRITES DE LA SFORJA (la autopsia de los «soles
    //  transparentes»: la v6.122 dibujaba la textura procedural Star
    //  con colores multiplicados — al fallar la carga el dibujo moría
    //  en silencio y SOLO quedaba el destello; y aún cargando, la
    //  estrella NO ES un sol). AHORA: EL SOL DE LUZ y EL SOL DE
    //  OSCURIDAD nacen del CÓDIGO DE SOLES DE LA CASA
    //  (gen_sol_aethon_v65036.py — el generador del sol del jefe,
    //  parametrizado en tools/sforja.py con HD cuadrado ×8): el
    //  blanco-dorado de Aethon y el negro con el borde ardiendo
    //  violeta (la lección v6.101) — y LA FUSIÓN: el sol del juicio
    //  con el corazón negro y la doble corona.
    //
    //  EL GUION EN CUATRO ACTOS (la letra, con la fusión que faltaba):
    //    1. LA CONVOCATORIA (30 t): LOS DOS SOLES nacen a los lados y
    //       vuelan al PUNTO DE ENCUENTRO — el sol de LUZ con su corona
    //       dorada girando, el sol de OSCURIDAD con su borde ardiendo.
    //    2. LA IMPLOSIÓN (26 t): los dos soles ESPIRALAN uno dentro
    //       del otro — el remolino TIRA del polvo, el latido negro al
    //       final: donde la luz y la oscuridad se comen mutuamente.
    //    3. LA FUSIÓN (30 t): NACE EL SOL DEL JUICIO — uno AÚN MÁS
    //       GRANDE Y DEVASTADOR que crece temblando hasta el tamaño
    //       dios, la luz inunda el mundo: la fusión de la letra, que
    //       la v6.122 se saltaba por completo.
    //    4. EL DESTELLO (40 t): EL SOL EXPLOTA — la NovaBurst de la
    //       casa, el anillo de choque, la lluvia dorada y la onda que
    //       golpea (ventana de 10 t, radio 460): la sentencia.
    // ======================================================================
    public class JuicioDosSolesProj : ModProjectile
    {
        // LA TEXTURA DE IDENTIDAD: EL SOL DEL JUICIO (la fusión — el
        // corazón de la ceremonia; los dos soles y el destello viven
        // abajo, cada uno con SU sprite de la SForja).
        public override string Texture => "AethonMod/Content/Effects/Procedural/SolFusion";

        // === LOS SOLES DE LA SFORJA (el código de soles de la casa) ===
        private static Asset<Texture2D> _solLuz;
        private static Asset<Texture2D> _solOscuro;
        private static Asset<Texture2D> _solFusion;

        private static Texture2D SolLuz
        {
            get
            {
                try
                {
                    _solLuz ??= ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/SolLuz");
                    return _solLuz.IsLoaded ? _solLuz.Value : null;
                }
                catch { return null; }
            }
        }

        private static Texture2D SolOscuro
        {
            get
            {
                try
                {
                    _solOscuro ??= ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/SolOscuro");
                    return _solOscuro.IsLoaded ? _solOscuro.Value : null;
                }
                catch { return null; }
            }
        }

        private static Texture2D SolFusion
        {
            get
            {
                try
                {
                    _solFusion ??= ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/SolFusion");
                    return _solFusion.IsLoaded ? _solFusion.Value : null;
                }
                catch { return null; }
            }
        }

        private const int T_CONVOCATORIA = 30;
        private const int T_IMPLOSION = 26;
        private const int T_FUSION = 30;
        private const int T_DESTELLO = 40;

        /// <summary>El reloj del acto actual.</summary>
        private float _t;

        /// <summary>0 convocatoria · 1 implosión · 2 fusión · 3 destello.</summary>
        private int _acto;

        private Vector2 Punto => new(Projectile.ai[0], Projectile.ai[1]);

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = T_CONVOCATORIA + T_IMPLOSION + T_FUSION + T_DESTELLO + 10;
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            _t++;

            // EL RELOJ DE LOS ACTOS
            if (_acto == 0 && _t > T_CONVOCATORIA) { _acto = 1; _t = 0; }
            else if (_acto == 1 && _t > T_IMPLOSION) { _acto = 2; _t = 0; NacerElSolFusion(); }
            else if (_acto == 2 && _t > T_FUSION) { _acto = 3; _t = 0; NacerElDestello(); }
            else if (_acto == 3 && _t > T_DESTELLO) { Projectile.Kill(); return; }

            Projectile.Center = Punto;

            // === ACTO 1 — LA CONVOCATORIA: los soles vuelan al punto ===
            if (_acto == 0)
            {
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
            // === ACTO 2 — LA IMPLOSIÓN: todo cae al centro ===
            else if (_acto == 1)
            {
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
                if (_t == T_IMPLOSION && !Main.dedServ)
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item122.WithPitchOffset(0.35f), Punto);
                    OndaLib.Kick(6f, 14);
                }
            }
            // === ACTO 3 — LA FUSIÓN: el sol del juicio CRECE temblando ===
            else if (_acto == 2)
            {
                if (!Main.dedServ)
                {
                    float crece = Math.Min(1f, _t / T_FUSION);
                    Lighting.AddLight(Punto, new Vector3(2.8f, 2.4f, 1.8f) * (0.4f + 0.6f * crece));
                    // EL TEMBLOR DE LA FUSIÓN (el dios apretando el puño)
                    if (Main.rand.NextBool(4))
                        OndaLib.Kick(2 + (int)(6f * crece), 6);
                    // LAS CHISPAS DE LA FORJA (los dos soles deshaciéndose)
                    if (Main.rand.NextBool(2))
                    {
                        bool esLuz = Main.rand.NextBool();
                        Dust d = Dust.NewDustPerfect(Punto + Main.rand.NextVector2Circular(60f, 60f) * (1f - crece * 0.6f),
                            esLuz ? DustID.GoldFlame : DustID.Shadowflame,
                            -Vector2.Normalize(Punto - Projectile.Center - Main.rand.NextVector2Circular(80f, 80f)) * 3f,
                            0, esLuz ? new Color(255, 236, 170) : new Color(170, 98, 245), 1.2f);
                        d.noGravity = true;
                    }
                }
            }
            // === ACTO 4 — EL DESTELLO: el sol explota en luz ===
            else
            {
                if (!Main.dedServ)
                {
                    float vida = Math.Max(0f, 1f - _t / T_DESTELLO);
                    Lighting.AddLight(Punto, new Vector3(2.6f, 2.3f, 1.6f) * vida);
                }
                if (!Main.dedServ && _t < 14 && Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Punto, DustID.GoldFlame,
                        Main.rand.NextVector2Circular(11f, 11f), 0,
                        new Color(255, 244, 204), Main.rand.NextFloat(1.1f, 2f));
                    d.noGravity = true;
                }
            }
        }

        /// <summary>EL NACIMIENTO DEL SOL DEL JUICIO (la fusión de la
        /// letra: «fucionarlos en una explosion creando uno aun mas
        /// grande y debastador»): el trueno de la forja y el tirón.</summary>
        private void NacerElSolFusion()
        {
            if (!Main.dedServ)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122.WithPitchOffset(-0.25f), Punto);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item15, Punto);
                OndaLib.Kick(8f, 18);
            }
        }

        /// <summary>EL NACIMIENTO DEL DESTELLO: la onda golpea (una ventana
        /// de 10 t con la hitbox de 920 px — la sentencia) y el teatro
        /// estalla (el trueno, la sacudida, el anillo).</summary>
        private void NacerElDestello()
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
                OndaLib.Kick(14f, 26);
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // LA SENTENCIA NO SE ESQUIVA: el destello es un hecho consumado.
            modifiers.SourceDamage *= 1f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            try
            {
                Vector2 cen = Punto - Main.screenPosition;
                float gt = Main.GlobalTimeWrappedHourly;
                Texture2D luz = SolLuz;
                Texture2D oscuridad = SolOscuro;
                Texture2D fusion = SolFusion;
                Texture2D nova = VFXCore.NovaBurst;
                Texture2D destello = VFXCore.DestelloFinal;

                // ============================================================
                //  LOS SOLES (núcleos en lote ALFA — el negro del sol de
                //  oscuridad TIENE que ser negro de verdad, el aditivo lo
                //  lavaría; los HALOS en aditivo aparte, como la casa manda)
                // ============================================================
                void DibujarSol(Texture2D sol, Vector2 pos, float escala, float rot, float alfa)
                {
                    if (sol == null) return;
                    Main.spriteBatch.Draw(sol, pos, null, Color.White * alfa, rot,
                        sol.Size() * 0.5f, escala, SpriteEffects.None, 0f);
                }
                void DibujarHalo(Texture2D sol, Vector2 pos, float escala, Color tinte, float alfa)
                {
                    if (sol == null) return;
                    Main.spriteBatch.Draw(sol, pos, null, tinte * alfa, 0f,
                        sol.Size() * 0.5f, escala, SpriteEffects.None, 0f);
                }

                if (_acto == 0)
                {
                    // ACTO 1 — LAS DOS SOLES volando al encuentro
                    float p = MathHelper.Clamp(_t / T_CONVOCATORIA, 0f, 1f);
                    p = p * p * (3f - 2f * p);
                    for (int lado = 0; lado < 2; lado++)
                    {
                        bool esLuz = lado == 0;
                        Texture2D sol = esLuz ? luz : oscuridad;
                        if (sol == null) continue;
                        Vector2 origen = Punto + (esLuz ? new Vector2(-300f, 0f) : new Vector2(300f, 0f));
                        Vector2 pos = Vector2.Lerp(origen, Punto, p) - Main.screenPosition +
                            new Vector2(0f, MathF.Sin(gt * 6f + lado * 3.1f) * 26f * (1f - p));
                        float escala = 0.9f + 0.25f * MathF.Sin(gt * 5f + lado);
                        float rot = gt * (esLuz ? 0.55f : -0.55f);
                        // EL NÚCLEO (alfa puro — el negro es negro)
                        DibujarSol(sol, pos, escala, rot, 1f);
                        // EL HALO (aditivo — la corona ardiendo alrededor)
                        VFXCore.AbrirAdditive();
                        try
                        {
                            DibujarHalo(sol, pos, escala * 2.2f,
                                esLuz ? new Color(255, 214, 130) : new Color(150, 84, 235), 0.34f);
                            DibujarHalo(sol, pos, escala * 1.4f,
                                esLuz ? new Color(255, 240, 190) : new Color(96, 50, 170), 0.5f);
                        }
                        finally { VFXCore.RestaurarLoteVanilla(); }
                    }
                }
                else if (_acto == 1)
                {
                    // ACTO 2 — LA IMPLOSIÓN: los soles espiralan al centro
                    float p = MathHelper.Clamp(_t / T_IMPLOSION, 0f, 1f);
                    float radio = MathHelper.Lerp(210f, 8f, p);
                    for (int lado = 0; lado < 2; lado++)
                    {
                        bool esLuz = lado == 0;
                        Texture2D sol = esLuz ? luz : oscuridad;
                        if (sol == null) continue;
                        float ang = gt * 5.5f + lado * MathHelper.Pi + p * 9f;
                        Vector2 pos = cen + ang.ToRotationVector2() * radio;
                        float escala = MathHelper.Lerp(1.0f, 0.34f, p);
                        DibujarSol(sol, pos, escala, ang * 2f, 1f);
                        VFXCore.AbrirAdditive();
                        try
                        {
                            DibujarHalo(sol, pos, escala * 1.9f,
                                esLuz ? new Color(255, 214, 130) : new Color(150, 84, 235), 0.5f);
                        }
                        finally { VFXCore.RestaurarLoteVanilla(); }
                    }
                    // EL REMOLINO del tirón (el vacío tragándose la luz)
                    Texture2D remolino = DefinitivasLib.TexRemolino;
                    if (remolino != null)
                    {
                        VFXCore.AbrirAdditive();
                        try
                        {
                            Main.spriteBatch.Draw(remolino, cen, null,
                                new Color(120, 70, 200) * (0.5f * (1f - p * 0.5f)),
                                gt * 3.2f, remolino.Size() * 0.5f,
                                2.6f - 1.6f * p, SpriteEffects.None, 0f);
                        }
                        finally { VFXCore.RestaurarLoteVanilla(); }
                    }
                    // EL LATIDO NEGRO final (el punto de la nada)
                    float negro = MathHelper.Clamp((p - 0.75f) / 0.25f, 0f, 1f);
                    if (negro > 0f && nova != null)
                    {
                        VFXCore.AbrirAdditive();
                        try
                        {
                            Main.spriteBatch.Draw(nova, cen, null,
                                new Color(10, 5, 18) * negro, 0f, nova.Size() * 0.5f,
                                0.4f + 0.5f * negro, SpriteEffects.None, 0f);
                        }
                        finally { VFXCore.RestaurarLoteVanilla(); }
                    }
                }
                else if (_acto == 2)
                {
                    // ACTO 3 — LA FUSIÓN: EL SOL DEL JUICIO nace y crece
                    // hasta el tamaño dios, TEMBLANDO de poder
                    float p = MathHelper.Clamp(_t / T_FUSION, 0f, 1f);
                    if (fusion != null)
                    {
                        float crece = 0.35f + 4.4f * (p * p * (3f - 2f * p));
                        // EL TEMBLOR DE LA FUSIÓN (la masa peleando por nacer)
                        Vector2 tiro = p < 0.95f
                            ? new Vector2(MathF.Sin(gt * 46f), MathF.Cos(gt * 38f)) * 7f * (1f - p * 0.5f)
                            : Vector2.Zero;
                        DibujarSol(fusion, cen + tiro, crece, gt * 0.5f, Math.Min(1f, 0.4f + p));
                        VFXCore.AbrirAdditive();
                        try
                        {
                            DibujarHalo(fusion, cen + tiro, crece * 1.55f,
                                new Color(255, 214, 130), 0.42f * (0.5f + 0.5f * p));
                            DibujarHalo(fusion, cen + tiro, crece * 2.6f,
                                new Color(150, 84, 235), 0.30f * p);
                        }
                        finally { VFXCore.RestaurarLoteVanilla(); }
                        // LOS DOS SOLES disolviéndose DENTRO de la fusión
                        float disuelven = Math.Max(0f, 1f - p * 2.6f);
                        if (disuelven > 0.02f)
                        {
                            for (int lado = 0; lado < 2; lado++)
                            {
                                bool esLuz = lado == 0;
                                Texture2D sol = esLuz ? luz : oscuridad;
                                if (sol == null) continue;
                                float ang = lado * MathHelper.Pi + p * 7f;
                                Vector2 pos = cen + ang.ToRotationVector2() * MathHelper.Lerp(80f, 0f, p);
                                DibujarSol(sol, pos, 0.8f * disuelven + 0.2f, ang, disuelven);
                            }
                        }
                    }
                }
                else
                {
                    // ACTO 4 — EL DESTELLO: el sol del juicio EXPLOTA en luz
                    float p = MathHelper.Clamp(_t / T_DESTELLO, 0f, 1f);
                    float crece = Math.Min(1f, _t / 7f);          // abre RÁPIDO
                    float apaga = 1f - p;
                    VFXCore.AbrirAdditive();
                    try
                    {
                        // LOS RAYOS del destello
                        if (destello != null)
                            Main.spriteBatch.Draw(destello, cen, null,
                                new Color(255, 244, 210) * (0.85f * apaga),
                                gt * 0.9f, destello.Size() * 0.5f,
                                (1.4f + 5.6f * crece) * (0.8f + 0.2f * MathF.Sin(gt * 14f)),
                                SpriteEffects.None, 0f);
                        // LA NOVA (el corazón de la explosión)
                        if (nova != null)
                            Main.spriteBatch.Draw(nova, cen, null,
                                new Color(255, 250, 235) * (0.95f * apaga * apaga * (3f - 2f * apaga)),
                                0f, nova.Size() * 0.5f,
                                (0.6f + 6.4f * crece) * (0.9f + 0.1f * MathF.Sin(gt * 12f)),
                                SpriteEffects.None, 0f);
                        // EL SOL FUSIONADO deshaciéndose en la explosión
                        if (fusion != null && _t < 18)
                            Main.spriteBatch.Draw(fusion, cen, null,
                                new Color(255, 244, 210) * (0.5f * (1f - _t / 18f)),
                                gt * 1.4f, fusion.Size() * 0.5f,
                                4.6f * (1f + 0.3f * MathF.Sin(gt * 20f)) * (1f - _t / 40f),
                                SpriteEffects.None, 0f);
                        // EL ANILLO DE CHOQUE (la onda de OndaLib)
                        OndaLib.Shock(Main.spriteBatch, cen, MathHelper.Clamp(_t / 26f, 0f, 1f),
                            1.5f + 4.5f * crece, new Color(255, 232, 170), 0.8f, 7, 14f);
                    }
                    finally { VFXCore.RestaurarLoteVanilla(); }
                }
            }
            catch { }
            return false;
        }
    }
}
