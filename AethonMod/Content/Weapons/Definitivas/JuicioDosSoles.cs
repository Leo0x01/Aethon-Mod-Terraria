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
    //  JuicioDosSolesProj — LA CEREMONIA ENTERA (las dos esferas, la
    //  implosión y el destello — un solo proyectil, tres actos).
    // ======================================================================
    public class JuicioDosSolesProj : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/Star";

        private const int T_CONVOCATORIA = 30;
        private const int T_IMPLOSION = 26;
        private const int T_DESTELLO = 44;

        /// <summary>El reloj del acto actual.</summary>
        private float _t;

        /// <summary>0 convocatoria · 1 implosión · 2 destello.</summary>
        private int _acto;

        private Vector2 Punto => new(Projectile.ai[0], Projectile.ai[1]);

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = T_CONVOCATORIA + T_IMPLOSION + T_DESTELLO + 10;
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            _t++;

            // EL RELOJ DE LOS ACTOS
            if (_acto == 0 && _t > T_CONVOCATORIA) { _acto = 1; _t = 0; }
            else if (_acto == 1 && _t > T_IMPLOSION) { _acto = 2; _t = 0; NacerElDestello(); }
            else if (_acto == 2 && _t > T_DESTELLO) { Projectile.Kill(); return; }

            Projectile.Center = Punto;

            // === ACTO 1 — LA CONVOCATORIA: las esferas vuelan al punto ===
            if (_acto == 0)
            {
                float progreso = _t / T_CONVOCATORIA;
                // EL POLVO DE LOS DOS SOLES (la luz siembra oro, la
                // oscuridad siembra vacío invertido)
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
                if (progreso >= 1f) { }      // el acto cambia solo
            }
            // === ACTO 2 — LA IMPLOSIÓN: todo cae al centro ===
            else if (_acto == 1)
            {
                // LA SUCCIÓN: el polvo cercano es TIRADO al punto.
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
                // EL OÍDO DEL JUICIO: el remolino gana peso (el tirón).
                if (_t == T_IMPLOSION && !Main.dedServ)
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item122.WithPitchOffset(0.35f), Punto);
                    OndaLib.Kick(6f, 14);
                }
            }
            // === ACTO 3 — EL DESTELLO: la luz inunda ===
            else
            {
                // LA LUZ DEL MUNDO (2 s de día dentro del juicio)
                if (!Main.dedServ)
                {
                    float vida = Math.Max(0f, 1f - _t / T_DESTELLO);
                    Lighting.AddLight(Punto, new Vector3(2.6f, 2.3f, 1.6f) * vida);
                }
                // LA LLUVIA DORADA del estallido
                if (!Main.dedServ && _t < 14 && Main.rand.NextBool(2))
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
                Texture2D estrella = DefinitivasLib.TexStar;
                Texture2D nova = VFXCore.NovaBurst;
                Texture2D destello = VFXCore.DestelloFinal;

                VFXCore.AbrirAdditive();
                try
                {
                    if (_acto == 0)
                    {
                        // LAS DOS ESFERAS volando al punto (a los lados)
                        float p = MathHelper.Clamp(_t / T_CONVOCATORIA, 0f, 1f);
                        p = p * p * (3f - 2f * p);
                        for (int lado = 0; lado < 2; lado++)
                        {
                            bool luz = lado == 0;
                            Vector2 origen = Punto + (luz ? new Vector2(-300f, 0f) : new Vector2(300f, 0f));
                            Vector2 pos = Vector2.Lerp(origen, Punto, p) +
                                new Vector2(0f, MathF.Sin(gt * 6f + lado * 3.1f) * 26f * (1f - p));
                            float escala = 1.1f + 0.3f * MathF.Sin(gt * 9f + lado);
                            // EL BORDE de la esfera (violeta para la oscuridad)
                            if (!luz)
                                Main.spriteBatch.Draw(estrella, pos, null,
                                    new Color(150, 84, 235) * 0.55f, gt * 2.2f,
                                    estrella.Size() * 0.5f, escala * 1.5f, SpriteEffects.None, 0f);
                            // EL NÚCLEO
                            Main.spriteBatch.Draw(estrella, pos, null,
                                luz ? new Color(255, 246, 220) : new Color(12, 7, 20),
                                gt * (luz ? 1.6f : -1.6f), estrella.Size() * 0.5f,
                                escala, SpriteEffects.None, 0f);
                            // EL HALO
                            Main.spriteBatch.Draw(estrella, pos, null,
                                luz ? new Color(255, 214, 130) * 0.45f : new Color(96, 54, 170) * 0.5f,
                                0f, estrella.Size() * 0.5f, escala * 2.1f, SpriteEffects.None, 0f);
                        }
                    }
                    else if (_acto == 1)
                    {
                        // LA IMPLOSIÓN: las esferas espiralan al centro
                        float p = MathHelper.Clamp(_t / T_IMPLOSION, 0f, 1f);
                        float radio = MathHelper.Lerp(210f, 8f, p);
                        for (int lado = 0; lado < 2; lado++)
                        {
                            bool luz = lado == 0;
                            float ang = gt * 5.5f + lado * MathHelper.Pi + p * 9f;
                            Vector2 pos = cen + ang.ToRotationVector2() * radio;
                            float escala = MathHelper.Lerp(1.3f, 0.4f, p);
                            Main.spriteBatch.Draw(estrella, pos, null,
                                luz ? new Color(255, 246, 220) : new Color(12, 7, 20),
                                ang, estrella.Size() * 0.5f, escala, SpriteEffects.None, 0f);
                            Main.spriteBatch.Draw(estrella, pos, null,
                                luz ? new Color(255, 214, 130) * 0.5f : new Color(150, 84, 235) * 0.6f,
                                0f, estrella.Size() * 0.5f, escala * 1.9f, SpriteEffects.None, 0f);
                        }
                        // EL REMOLINO del tirón (el vacío tragándose la luz)
                        Texture2D remolino = DefinitivasLib.TexRemolino;
                        if (remolino != null)
                            Main.spriteBatch.Draw(remolino, cen, null,
                                new Color(120, 70, 200) * (0.5f * (1f - p * 0.5f)),
                                gt * 3.2f, remolino.Size() * 0.5f,
                                2.6f - 1.6f * p, SpriteEffects.None, 0f);
                        // EL LATIDO NEGRO final (el punto de la nada)
                        float negro = MathHelper.Clamp((p - 0.75f) / 0.25f, 0f, 1f);
                        if (negro > 0f && nova != null)
                            Main.spriteBatch.Draw(nova, cen, null,
                                new Color(10, 5, 18) * negro, 0f, nova.Size() * 0.5f,
                                0.4f + 0.5f * negro, SpriteEffects.None, 0f);
                    }
                    else
                    {
                        // EL DESTELLO: la explosión de luz
                        float p = MathHelper.Clamp(_t / T_DESTELLO, 0f, 1f);
                        float crece = Math.Min(1f, _t / 7f);          // abre RÁPIDO
                        float apaga = 1f - p;
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
                }
                finally { VFXCore.RestaurarLoteVanilla(); }
            }
            catch { }
            return false;
        }

        private static float apagasuave(float apaga)
            => apaga * apaga * (3f - 2f * apaga);
    }
}
