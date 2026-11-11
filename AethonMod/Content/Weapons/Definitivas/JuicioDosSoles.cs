using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    //  v6.124 — LA PELÍCULA NUEVA (la letra: «la animacion de Juicio de
    //  los Dos Soles no me gusta para nada, ademas que no tengan anillos
    //  runicos los soles, a lo mucho que solo tengan 1 y cuando se unan
    //  que tengan 3, ademas debes mejorar la animacion en todos los
    //  sentidos»). LA AUTOPSIA: los soles eran TRES texturas de estrella
    //  apiladas girando (el núcleo, el halo ×2,1 y el borde girando con
    //  el reloj — LEÍDO como anillos rúnicos) y la coreografía era DOS
    //  lerp rectos y un remolino plano. LA OBRA NUEVA — cuatro actos,
    //  el lenguaje de las estrellas binarias de verdad:
    //    1. LA CONVOCATORIA (34 t): los DOS SOLES nacen a los lados del
    //       portador con su destello de nacimiento y CRECEN volando al
    //       punto de encuentro por ARCOS CURVOS (el de luz por arriba,
    //       el de oscuridad por abajo — el yin y el yang) sembrando su
    //       rastro (oro y brasa), mientras el punto respira UN ANILLO
    //       fino de telégrafo.
    //    2. LA DANZA (46 t): LA ÓRBITA BINARIA — los dos soles giran
    //       el uno alrededor del otro, el radio CAE (84 → 12 px) y la
    //       vuelta ACELERA (el inspiral de verdad: ω 0,09 → 0,30 rad/t)
    //       tejiendo la espiral con sus estelas; el polvo cercano cae al
    //       centro (la gravedad del par) y el temblor crece con la
    //       vuelta. Al contacto: EL COMPÁS — un golpe de oscuridad.
    //    3. LA FUSIÓN (24 t): los dos son UNO — EL ECLIPSE de la casa
    //       (el disco NEGRO de borde ardiendo — el negro solo existe si
    //       su borde arde) con SUS TRES ANILLOS (la letra: «cuando se
    //       unan que tengan 3» — oro, blanco y violeta, cada uno girando
    //       a su aire). El cuerpo SE CONTRAE (la tensión) tragándose el
    //       polvo que acelera hacia adentro.
    //    4. EL DESTELLO (44 t): LA EXPLOSIÓN DE LUZ — la corona ERUPCIONA
    //       (el anillo de fuego expandiéndose), la NovaBurst nace de
    //       cero a tamaño dios, los rayos del DestelloFinal, la lluvia
    //       dorada, la luz que inunda el mundo y la sacudida. La onda
    //       golpea UNA VEZ (la ventana de 10 t y el radio de 460 — las
    //       estadísticas de la v6.122, INTACTAS).
    //
    //  LOS ANILLOS DE LOS SOLES (la letra, EXACTA): UNO solo por sol en
    //  los actos 1-2 (la corona fina que respira) y TRES en la fusión —
    //  las texturas de estrella girando MURIERON (eran «los anillos
    //  rúnicos»).
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
    //  JuicioDosSolesProj — LA CEREMONIA ENTERA (los dos soles, la danza,
    //  la fusión y el destello — un solo proyectil, cuatro actos).
    // ======================================================================
    public class JuicioDosSolesProj : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        // === LOS TIEMPOS DE LA PELÍCULA NUEVA (v6.124) ===
        private const int T_CONVOCATORIA = 34;
        private const int T_DANZA = 46;
        private const int T_FUSION = 24;
        private const int T_DESTELLO = 44;

        /// <summary>El reloj del acto actual.</summary>
        private float _t;

        /// <summary>0 convocatoria · 1 danza · 2 fusión · 3 destello.</summary>
        private int _acto;

        /// <summary>Las estelas de la danza (la espiral tejida — 8 muestras por sol).</summary>
        private readonly Vector2[] _estelaLuz = new Vector2[8];
        private readonly Vector2[] _estelaSombra = new Vector2[8];

        private Vector2 Punto => new(Projectile.ai[0], Projectile.ai[1]);

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = T_CONVOCATORIA + T_DANZA + T_FUSION + T_DESTELLO + 10;
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;
        }

        public override bool ShouldUpdatePosition() => false;

        // ================================================================
        //  LAS POSICIONES DE LA DANZA — la coreografía de los dos soles
        // ================================================================

        /// <summary>EL ARCO DE LA CONVOCATORIA (Bezier cuadrática): el sol
        /// de luz llega POR ARRIBA y el de oscuridad POR ABAJO — los arcos
        /// curvos que reemplazan los lerps rectos de la v6.122.</summary>
        private static Vector2 ArcoConvocatoria(Vector2 punto, bool luz, float p)
        {
            float e = p * p * (3f - 2f * p);
            Vector2 origen = punto + (luz ? new Vector2(-330f, -50f) : new Vector2(330f, 50f));
            Vector2 control = punto + (luz ? new Vector2(-90f, -190f) : new Vector2(90f, 190f));
            Vector2 a = Vector2.Lerp(origen, control, e);
            Vector2 b = Vector2.Lerp(control, punto, e);
            return Vector2.Lerp(a, b, e);
        }

        /// <summary>LA ÓRBITA BINARIA DE LA DANZA: el radio CAE (el
        /// inspiral) y la vuelta ACELERA — cada sol en su lado opuesto del
        /// par (θ y θ+π: DOS soles girando el uno alrededor del otro).</summary>
        private static void PosicionesDanza(Vector2 punto, float t, out Vector2 luz, out Vector2 sombra, out float radio)
        {
            float p = MathHelper.Clamp(t / T_DANZA, 0f, 1f);
            // EL INSPIRAL: el radio muere suave al final (la caída acelera)
            radio = MathHelper.Lerp(84f, 12f, p * p * (3f - 2f * p) * 0.75f + p * 0.25f * p);
            // LA VUELTA QUE ACELERA: ω crece de 0,09 a 0,30 rad/t
            float omega = MathHelper.Lerp(0.09f, 0.30f, p * p);
            float ang = 1.1f + omega * t * (1f + p * 0.55f);
            luz = punto + ang.ToRotationVector2() * radio;
            sombra = punto - ang.ToRotationVector2() * radio;
        }

        public override void AI()
        {
            _t++;

            // EL RELOJ DE LOS ACTOS
            if (_acto == 0 && _t > T_CONVOCATORIA) { _acto = 1; _t = 0; }
            else if (_acto == 1 && _t > T_DANZA) { _acto = 2; _t = 0; }
            else if (_acto == 2 && _t > T_FUSION) { _acto = 3; _t = 0; NacerElDestello(); }
            else if (_acto == 3 && _t > T_DESTELLO) { Projectile.Kill(); return; }

            Projectile.Center = Punto;

            // === ACTO 1 — LA CONVOCATORIA: los soles crecen volando ===
            if (_acto == 0)
            {
                float p = MathHelper.Clamp(_t / T_CONVOCATORIA, 0f, 1f);
                Vector2 pl = ArcoConvocatoria(Punto, true, p);
                Vector2 ps = ArcoConvocatoria(Punto, false, p);
                // EL DESTELLO DE NACIMIENTO (una vez, a medio camino)
                if (!Main.dedServ && (int)_t == 8)
                {
                    OndaLib.Flash(new Color(255, 240, 210), 0.10f, 8);
                }
                // LOS RASTROS: el oro de la luz, la brasa de la oscuridad
                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    bool esLuz = Main.rand.NextBool();
                    Vector2 pos = esLuz ? pl : ps;
                    Dust d = Dust.NewDustPerfect(pos,
                        esLuz ? DustID.GoldFlame : DustID.Shadowflame,
                        -Vector2.Normalize(Punto - pos) * Main.rand.NextFloat(0.6f, 2.2f) +
                            Main.rand.NextVector2Circular(0.8f, 0.8f),
                        0, esLuz ? new Color(255, 226, 150) : new Color(150, 84, 235), 0.9f);
                    d.noGravity = true;
                }
            }
            // === ACTO 2 — LA DANZA: la órbita binaria y la gravedad del par ===
            else if (_acto == 1)
            {
                PosicionesDanza(Punto, _t, out Vector2 pl, out Vector2 ps, out float radio);
                // LAS ESTELAS (la espiral tejida: cada sol guarda su rastro)
                if ((int)_t % 3 == 0)
                {
                    for (int i = 7; i > 0; i--) { _estelaLuz[i] = _estelaLuz[i - 1]; _estelaSombra[i] = _estelaSombra[i - 1]; }
                    _estelaLuz[0] = pl; _estelaSombra[0] = ps;
                }
                // LA GRAVEDAD DEL PAR: el polvo cercano CAE al centro (y
                // acelera con la vuelta — la succión de la v6.122, hecha danza)
                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    float p = MathHelper.Clamp(_t / T_DANZA, 0f, 1f);
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    float lejos = Main.rand.NextFloat(150f, 420f);
                    Vector2 pos = Punto + ang.ToRotationVector2() * lejos;
                    Dust d = Dust.NewDustPerfect(pos,
                        Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.Shadowflame,
                        -ang.ToRotationVector2() * (lejos / (16f - 7f * p)), 0,
                        new Color(210, 160, 255), 1.0f);
                    d.noGravity = true;
                }
                // EL TEMBLOR QUE CRECE con la vuelta (el rugido del inspiral)
                float prog = MathHelper.Clamp(_t / T_DANZA, 0f, 1f);
                if (!Main.dedServ && Main.rand.NextFloat() < 0.02f + 0.10f * prog * prog)
                    OndaLib.Kick(1.5f + 3.5f * prog, 10);
                // EL COMPÁS DEL CONTACTO (el golpe de oscuridad antes del silencio)
                if ((int)_t == T_DANZA && !Main.dedServ)
                {
                    OndaLib.Flash(new Color(6, 3, 14), 0.45f, 10);
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item122.WithPitchOffset(-0.35f), Punto);
                }
            }
            // === ACTO 3 — LA FUSIÓN: la contracción y la lluvia al centro ===
            else if (_acto == 2)
            {
                float p = MathHelper.Clamp(_t / T_FUSION, 0f, 1f);
                // LA GRAVEDAD FINAL: TODO lo cercano acelera hacia adentro
                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    float lejos = Main.rand.NextFloat(110f, 380f);
                    Vector2 pos = Punto + ang.ToRotationVector2() * lejos;
                    Dust d = Dust.NewDustPerfect(pos,
                        Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.Shadowflame,
                        -ang.ToRotationVector2() * (lejos / (9f - 5f * p)), 0,
                        new Color(220, 170, 255), 1.1f);
                    d.noGravity = true;
                }
                if (!Main.dedServ && Main.rand.NextFloat() < 0.05f + 0.15f * p)
                    OndaLib.Kick(2f + 4f * p, 8);
                // LA TENSIÓN FINAL: el silencio carga (el golpe queda para el destello)
                if ((int)_t == T_FUSION && !Main.dedServ)
                {
                    OndaLib.Flash(new Color(8, 4, 16), 0.35f, 8);
                }
            }
            // === ACTO 4 — EL DESTELLO: la luz inunda ===
            else
            {
                // LA LUZ DEL MUNDO (2 s de día dentro del juicio)
                if (!Main.dedServ)
                {
                    float vida = Math.Max(0f, 1f - _t / T_DESTELLO);
                    Lighting.AddLight(Punto, new Vector3(3.1f, 2.7f, 1.9f) * vida);
                }
                // LA LLUVIA DORADA del estallido
                if (!Main.dedServ && _t < 16 && Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Punto, DustID.GoldFlame,
                        Main.rand.NextVector2Circular(11f, 11f), 0,
                        new Color(255, 244, 204), Main.rand.NextFloat(1.1f, 2f));
                    d.noGravity = true;
                }
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
                OndaLib.Flash(new Color(255, 250, 235), 0.55f, 12);
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
                Texture2D glow = VFXCore.SoftGlow;
                Texture2D nova = VFXCore.NovaBurst;
                Texture2D destello = VFXCore.DestelloFinal;

                // ============================================================
                //  ACTO 1 — LA CONVOCATORIA: dos soles creciendo por sus arcos
                // ============================================================
                if (_acto == 0)
                {
                    float p = MathHelper.Clamp(_t / T_CONVOCATORIA, 0f, 1f);
                    float crece = 0.35f + 0.65f * (p * p * (3f - 2f * p));
                    Vector2 pl = ArcoConvocatoria(Punto, true, p) - Main.screenPosition;
                    Vector2 ps = ArcoConvocatoria(Punto, false, p) - Main.screenPosition;

                    // EL TELEGRAFO: UN anillo fino respirando en el punto
                    float resp = 1f + 0.08f * MathF.Sin(gt * 4.2f);
                    OrbitaLib.AbrirAdditive();
                    try
                    {
                        OrbitaLib.AnilloFino(cen, 44f * resp, 0f,
                            OrbitaLib.Tint(new Color(230, 205, 255), 0.16f + 0.10f * p));
                        DibujarSolDeLuz(pl, crece, 1f);
                        DibujarSolDeSombra(ps, crece, 1f);
                    }
                    finally { OrbitaLib.CerrarBatch(); }
                    DibujarDiscoNegro(ps, 13f * crece);
                }
                // ============================================================
                //  ACTO 2 — LA DANZA: la órbita binaria con sus estelas
                // ============================================================
                else if (_acto == 1)
                {
                    PosicionesDanza(Punto, _t, out Vector2 pl, out Vector2 ps, out float radio);
                    Vector2 plS = pl - Main.screenPosition, psS = ps - Main.screenPosition;
                    float p = MathHelper.Clamp(_t / T_DANZA, 0f, 1f);

                    // LAS ESTELAS: la espiral tejida (los fantasmas de la danza)
                    OrbitaLib.AbrirAdditive();
                    try
                    {
                        for (int i = 6; i >= 1; i--)
                        {
                            float f = (7 - i) / 7f;
                            if (_estelaLuz[i] != Vector2.Zero)
                            {
                                Vector2 e = _estelaLuz[i] - Main.screenPosition;
                                if (glow != null)
                                    Main.spriteBatch.Draw(glow, e, null,
                                        new Color(255, 226, 150) * (0.16f * f),
                                        0f, glow.Size() * 0.5f, (0.24f + 0.06f * i) * (1f - 0.5f * p),
                                        SpriteEffects.None, 0f);
                            }
                            if (_estelaSombra[i] != Vector2.Zero)
                            {
                                Vector2 e = _estelaSombra[i] - Main.screenPosition;
                                if (glow != null)
                                    Main.spriteBatch.Draw(glow, e, null,
                                        new Color(150, 84, 235) * (0.20f * f),
                                        0f, glow.Size() * 0.5f, (0.26f + 0.07f * i) * (1f - 0.5f * p),
                                        SpriteEffects.None, 0f);
                            }
                        }
                        DibujarSolDeLuz(plS, 1f, 1f);
                        DibujarSolDeSombra(psS, 1f, 1f);
                        // LA ÓRBITA que se lee (el círculo de la danza, tenue)
                        OrbitaLib.AnilloFino(cen, radio, gt * 0.8f,
                            OrbitaLib.Tint(new Color(190, 150, 255), 0.10f));
                    }
                    finally { OrbitaLib.CerrarBatch(); }
                    DibujarDiscoNegro(psS, 13f * (1f - 0.35f * p));
                }
                // ============================================================
                //  ACTO 3 — LA FUSIÓN: el eclipse con SUS TRES ANILLOS
                // ============================================================
                else if (_acto == 2)
                {
                    float p = MathHelper.Clamp(_t / T_FUSION, 0f, 1f);
                    float contrae = MathHelper.Lerp(1f, 0.34f, p * p);
                    float tension = p * p;

                    OrbitaLib.AbrirAdditive();
                    try
                    {
                        // EL BORDE ARDIENDO del eclipse (la ley del negro:
                        // el disco negro vive en el pase alfa de abajo —
                        // su borde arde AQUÍ)
                        if (glow != null)
                        {
                            Main.spriteBatch.Draw(glow, cen, null,
                                new Color(255, 214, 130) * (0.55f + 0.35f * tension),
                                0f, glow.Size() * 0.5f, (0.85f + 0.5f * tension) * contrae,
                                SpriteEffects.None, 0f);
                        }
                        // === LOS TRES ANILLOS DE LA FUSIÓN (la letra: «cuando
                        //     se unan que tengan 3») — cada uno SU color, SU
                        //     velocidad y SU dirección; se aprietan con la
                        //     contracción y arden con la tensión ===
                        float brillo = 0.55f + 0.45f * tension;
                        OrbitaLib.AnilloFino(cen, 30f * contrae, gt * 1.4f,
                            OrbitaLib.Tint(new Color(255, 214, 130), brillo));        // el ORO
                        OrbitaLib.AnilloFino(cen, 39f * contrae, -gt * 1.0f,
                            OrbitaLib.Tint(new Color(255, 246, 225), brillo * 0.85f)); // el BLANCO
                        OrbitaLib.AnilloFino(cen, 49f * contrae, gt * 0.7f,
                            OrbitaLib.Tint(new Color(150, 84, 235), brillo * 0.75f));  // el VIOLETA
                        // EL ÚLTIMO SUSPIRO del sol de luz (la corona que se
                        // apaga devorada — el residuo del acto anterior)
                        if (glow != null && tension < 0.5f)
                            Main.spriteBatch.Draw(glow, cen, null,
                                new Color(255, 240, 205) * (0.35f * (1f - tension * 2f)),
                                0f, glow.Size() * 0.5f, 0.55f * contrae,
                                SpriteEffects.None, 0f);
                    }
                    finally { OrbitaLib.CerrarBatch(); }
                    // EL DISCO NEGRO (el eclipse de la casa)
                    DibujarDiscoNegro(cen, 21f * contrae);
                }
                // ============================================================
                //  ACTO 4 — EL DESTELLO: la erupción de la corona
                // ============================================================
                else
                {
                    float p = MathHelper.Clamp(_t / T_DESTELLO, 0f, 1f);
                    float crece = Math.Min(1f, _t / 7f);          // abre RÁPIDO
                    float apaga = 1f - p;
                    OrbitaLib.AbrirAdditive();
                    try
                    {
                        // LA CORONA ERUPCIONA (el anillo de fuego que se
                        // expande — la erupción solar de la fusión)
                        float anilloR = 40f + 520f * crece;
                        float anilloBrillo = MathF.Max(0f, 0.85f - crece * 0.55f) * (0.7f + 0.3f * MathF.Sin(gt * 18f));
                        OrbitaLib.AnilloFino(cen, anilloR, gt * 0.5f,
                            OrbitaLib.Tint(new Color(255, 232, 170), anilloBrillo));
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
                                new Color(255, 250, 235) * (0.95f * apagasuave(apaga)),
                                0f, nova.Size() * 0.5f,
                                (0.6f + 6.4f * crece) * (0.9f + 0.1f * MathF.Sin(gt * 12f)),
                                SpriteEffects.None, 0f);
                        // EL ANILLO DE CHOQUE (la onda de OndaLib)
                        OndaLib.Shock(Main.spriteBatch, cen, MathHelper.Clamp(_t / 26f, 0f, 1f),
                            1.5f + 4.5f * crece, new Color(255, 232, 170), 0.8f, 7, 14f);
                    }
                    finally { OrbitaLib.CerrarBatch(); }
                }
            }
            catch { }
            return false;
        }

        /// <summary>EL SOL DE LUZ (la letra de la v6.124: SIN anillos
        /// rúnicos — A LO MUCHO UNO): el disco blanco-dorado caliente con
        /// su corona tibia y UN ANILLO FINO que respira — nada de
        /// texturas de estrella girando.</summary>
        private static void DibujarSolDeLuz(Vector2 pos, float escala, float alfa)
        {
            Texture2D glow = VFXCore.SoftGlow;
            if (glow == null) return;
            float respira = 1f + 0.06f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5.1f);
            // LA CORONA tibia (el halo que la envuelve)
            Main.spriteBatch.Draw(glow, pos, null,
                new Color(255, 214, 130) * (0.40f * alfa), 0f, glow.Size() * 0.5f,
                1.05f * escala * respira, SpriteEffects.None, 0f);
            // EL DISCO (el corazón blanco-dorado)
            Main.spriteBatch.Draw(glow, pos, null,
                new Color(255, 246, 220) * (0.95f * alfa), 0f, glow.Size() * 0.5f,
                0.50f * escala, SpriteEffects.None, 0f);
            // EL ÚNICO ANILLO (la corona fina — uno solo, la letra)
            OrbitaLib.AnilloFino(pos, 16f * escala * respira, 0f,
                OrbitaLib.Tint(new Color(255, 232, 170), 0.42f * alfa));
        }

        /// <summary>EL SOL DE OSCURIDAD (la ley v6.101: el negro solo
        /// existe si su borde arde): el disco negro vive en el pase alfa
        /// (DibujarDiscoNegro) y AQUÍ arden su borde violeta y SU UNICO
        /// ANILLO fino.</summary>
        private static void DibujarSolDeSombra(Vector2 pos, float escala, float alfa)
        {
            Texture2D glow = VFXCore.SoftGlow;
            if (glow == null) return;
            float respira = 1f + 0.06f * MathF.Sin(Main.GlobalTimeWrappedHourly * 4.6f + 1.9f);
            // EL BORDE ARDIENDO (el aro violeta que hace existir al negro)
            Main.spriteBatch.Draw(glow, pos, null,
                new Color(150, 84, 235) * (0.50f * alfa), 0f, glow.Size() * 0.5f,
                0.62f * escala * respira, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glow, pos, null,
                new Color(96, 54, 170) * (0.55f * alfa), 0f, glow.Size() * 0.5f,
                0.45f * escala, SpriteEffects.None, 0f);
            // EL ÚNICO ANILLO (uno solo, la letra — violeta hondo)
            OrbitaLib.AnilloFino(pos, 17f * escala * respira, 0f,
                OrbitaLib.Tint(new Color(122, 78, 188), 0.42f * alfa));
        }

        /// <summary>EL DISCO NEGRO (el pase de lo oscuro): el negro de
        /// verdad NO puede ser aditivo — vive en el pase ALFA encima de
        /// todo lo brillante (la ley de la casa para TODO lo negro).</summary>
        private static void DibujarDiscoNegro(Vector2 pos, float radio)
        {
            try
            {
                VFXCore.AbrirAlpha();
                try
                {
                    Texture2D glow = VFXCore.SoftGlow;
                    if (glow != null)
                        Main.spriteBatch.Draw(glow, pos, null,
                            new Color(8, 4, 16, 255), 0f, glow.Size() * 0.5f,
                            radio * 2f / glow.Width, SpriteEffects.None, 0f);
                }
                finally { OrbitaLib.CerrarBatch(); }
            }
            catch { }
        }

        private static float apagasuave(float apaga)
            => apaga * apaga * (3f - 2f * apaga);
    }
}
