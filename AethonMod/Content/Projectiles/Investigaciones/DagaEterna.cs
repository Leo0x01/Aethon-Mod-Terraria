using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using Marca = AethonMod.Content.Buffs.Investigaciones.MarcaDelPrincipio;
using Mirada = AethonMod.Content.Buffs.Investigaciones.MiradaDelFin;

namespace AethonMod.Content.Projectiles.Investigaciones
{
    // ======================================================================
    //  DAGA ETERNA — LA DAGA DE EL PRINCIPIO Y EL FIN (la copia de
    //  investigación de The Beginning and The End de Calamity Entropy):
    //    · ai[0]: 0 = EL PRINCIPIO (daga de ORO) · 1 = EL FIN (daga AZUL)
    //      — la ficha: izquierda oro, derecha azul.
    //    · ai[1]: 0 = normal · 1 = SIGILOSA (rastreadora — el tiro del
    //      ladrón: DOS gemelas que revientan en almas).
    //    · LA FICHA, PALABRA POR PALABRA: 100 penetración de armadura,
    //      NO atraviesa objetivos (penetración 1), atraviesa MUROS, y
    //      ambas aplican SUPRESIÓN VITAL (10 s, «has visto tu muerte»).
    //    · EL CANJE DE ALMAS: golpear a un impío con LA OTRA marca libera
    //      la marca y cada CAPA suelta su alma (las del oro = ALMAS DE
    //      LUZ INFERIOR que vuelan al jugador; las del azul = ALMAS DE
    //      ECO que vuelan a los enemigos disparando ondas que atraviesan).
    // ======================================================================
    public class DagaEterna : ModProjectile
    {
        // EL Lienzo de la casa: TODO su dibujado es código (HazLib / los
        // arcos de la casa) — la textura es el placeholder de siempre.
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Throwing;
            Projectile.penetrate = 1;               // la ficha: NO atraviesa
            Projectile.timeLeft = 180;
            Projectile.tileCollide = false;         // la ficha: atraviesa MUROS
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;
            Projectile.ArmorPenetration = 100;      // la ficha: 100 penetración
        }

        private bool EsFin => Projectile.ai[0] > 0.5f;
        private bool Sigilosa => Projectile.ai[1] > 0.5f;

        private NPC _presa;

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            // LA DAGA SIGILOSA ES RASTREADORA (la ficha: «dos dagas que
            // rastrean»): gira suave hacia la presa más cercana.
            if (Sigilosa)
            {
                if (_presa == null || !_presa.active || _presa.life <= 0)
                    _presa = BuscarPresa(1200f);
                if (_presa != null)
                {
                    Vector2 hacia = _presa.Center - Projectile.Center;
                    float vel = Projectile.velocity.Length();
                    if (hacia.LengthSquared() > 1f && vel > 0.01f)
                    {
                        float ang = Projectile.velocity.ToRotation();
                        ang = ang.AngleTowards(hares(hacia), 0.12f);
                        Projectile.velocity = ang.ToRotationVector2() * vel;
                    }
                }
            }

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center,
                    EsFin ? DustID.Shadowflame : DustID.Enchanted_Gold,
                    -Projectile.velocity * 0.08f, 150,
                    EsFin ? new Color(120, 170, 255) : new Color(255, 224, 150), 0.7f);
                d.noGravity = true;
            }
        }

        private static float hares(Vector2 v) => v.ToRotation();

        private static NPC BuscarPresa(float max)
        {
            NPC mejor = null;
            float mejorD = max * max;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                float d = n.Center.DistanceSQ(Main.player[Main.myPlayer].Center);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // LA SUPRESIÓN VITAL: ambas dagas la aplican (la ficha: 10 s,
            // «has visto tu muerte»).
            target.AddBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.Investigaciones.SupresionVital>(), 600);

            var g = target.GetGlobalNPC<global::AethonMod.Content.Globals.Investigaciones.InvestigacionesGlobalNPC>();

            if (EsFin)
            {
                // === LA DAGA AZUL (EL FIN): si la presa lleva LA MARCA DEL
                //     PRINCIPIO, la marca se LIBERA — cada CAPA suelta su
                //     ALMA DE LUZ INFERIOR (vuelan al jugador: curan y
                //     regalan SERVIDUMBRE) — y su MIRADA la reemplaza ===
                int capas = g.StacksPrincipio;
                if (capas > 0 && target.HasBuff(ModContent.BuffType<Marca>()))
                {
                    QuitarMarca(target, ModContent.BuffType<Marca>());
                    g.StacksPrincipio = 0;
                    for (int i = 0; i < capas; i++)
                        NacerAlma<AlmaLuzInferior>(target.Center, i, capas);
                    if (!Main.dedServ)
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item90.WithPitchOffset(0.2f), target.Center);
                }
                g.StacksFin = Math.Min(8, g.StacksFin + 1);
                target.AddBuff(ModContent.BuffType<Mirada>(), 720);
            }
            else
            {
                // === LA DAGA DE ORO (EL PRINCIPIO): si la presa lleva LA
                //     MIRADA DEL FIN, la mirada se LIBERA — cada CAPA
                //     suelta su ALMA DE ECO (vuelan a los enemigos
                //     disparando ONDAS que atraviesan) — y su MARCA la
                //     reemplaza ===
                int capas = g.StacksFin;
                if (capas > 0 && target.HasBuff(ModContent.BuffType<Mirada>()))
                {
                    QuitarMarca(target, ModContent.BuffType<Mirada>());
                    g.StacksFin = 0;
                    for (int i = 0; i < capas; i++)
                        NacerAlma<AlmaEco>(target.Center, i, capas);
                    if (!Main.dedServ)
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item90.WithPitchOffset(-0.2f), target.Center);
                }
                g.StacksPrincipio = Math.Min(8, g.StacksPrincipio + 1);
                target.AddBuff(ModContent.BuffType<Marca>(), 720);
            }

            // EL ESTALLIDO SIGILOSO: la daga del ladrón REVIENTA en una
            // nube de AMBAS almas (la ficha: «explota en una gran
            // cantidad de almas de eco y almas de luz inferior»).
            if (Sigilosa)
            {
                for (int i = 0; i < 5; i++)
                    NacerAlma<AlmaEco>(target.Center, i, 5);
                for (int i = 0; i < 5; i++)
                    NacerAlma<AlmaLuzInferior>(target.Center, i, 5);
            }
        }

        /// <summary>Mata el buff de la marca en el NPC (busca su índice y
        /// vence su reloj — la liberación de la ficha).</summary>
        private static void QuitarMarca(NPC npc, int tipo)
        {
            int i = npc.FindBuffIndex(tipo);
            if (i >= 0) npc.buffTime[i] = 1;
        }

        private void NacerAlma<T>(Vector2 centro, int i, int total) where T : ModProjectile
        {
            float ang = -MathHelper.PiOver2 + (i - (total - 1) * 0.5f) * 0.35f;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), centro,
                ang.ToRotationVector2() * Main.rand.NextFloat(4f, 8f),
                ModContent.ProjectileType<T>(), Projectile.damage, 0f, Projectile.owner);
        }

        public override bool PreDraw(ref Microsoft.Xna.Framework.Color lightColor)
        {
            // LA DAGA: el HUSO de la casa (HazLib — el mismo del tajo del
            // vacío, la aguja-haz de la casa): dorada o azul según su
            // color, con el núcleo blanco y el borde del color.
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            try
            {
                Vector2 punta = Projectile.Center - Main.screenPosition;
                Vector2 dir = Vector2.Normalize(Projectile.velocity);
                Vector2 rabo = punta - dir * 44f;

                Color borde = EsFin ? new Color(110, 170, 255) : new Color(255, 214, 120);
                Color nucleo = EsFin ? new Color(225, 240, 255) : new Color(255, 250, 235);

                HazLib.Abrir();
                try
                {
                    // EL VELO — el aura del color.
                    HazLib.PrepararPar(rabo, punta, x => 7f * MathF.Pow(MathF.Sin(MathF.PI * x), 0.6f) * 1.9f, 0f, 20);
                    HazLib.Dibujar(x => borde, null, HazLib.FilasVelo);

                    // EL CUERPO — la hoja: nace fina del rabo, LLena al
                    // centro, PUNTA de aguja.
                    HazLib.PrepararPar(rabo, punta, x => 6f * MathF.Pow(MathF.Sin(MathF.PI * x), 0.55f), 0f, 20);
                    HazLib.Dibujar(x => nucleo, (x, y, t) =>
                        MathF.Abs(y - 0.5f) * 2f > 0.72f ? borde : nucleo, HazLib.FilasNucleo);
                }
                finally { HazLib.Cerrar(); }
            }
            catch { }
            return false;
        }
    }

    // ======================================================================
    //  LAS ALMAS DE EL PRINCIPIO Y EL FIN (la ficha):
    //    · EL ALMA DE ECO (回音之魂): vuela a los enemigos y dispara
    //      ONDAS DE SONIDO que atraviesan a todos (la ofensiva).
    //    · EL ALMA DE LUZ INFERIOR (冥煌之魂): vuela al jugador, lo CURA
    //      y le regala LA SERVIDUMBRE (la defensiva).
    // ======================================================================
    public class AlmaEco : ModProjectile
    {
        // EL Lienzo de la casa: TODO su dibujado es código (HazLib / los
        // arcos de la casa) — la textura es el placeholder de siempre.
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Throwing;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 260;
            Projectile.tileCollide = false;
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        private NPC _presa;
        private int _reloj;

        public override void AI()
        {
            _reloj++;

            // EL ALMA ELIGE su presa y la RONDA (a 130 px, girando).
            if (_presa == null || !_presa.active || _presa.life <= 0)
            {
                _presa = DagaEternaBuscar.Buscar(1100f, Projectile.Center);
            }
            if (_presa != null)
            {
                float ang = _reloj * 0.09f;
                Vector2 destino = _presa.Center + ang.ToRotationVector2() * 130f;
                Projectile.velocity += (destino - Projectile.Center) * 0.014f;
                if (Projectile.velocity.LengthSquared() > 14f * 14f)
                    Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 14f;

                // LA ONDA DE SONIDO: cada 30 t el alma DISPARA su onda —
                // el arco que ATRAVIESA a todos (la ficha).
                if (_reloj % 30 == 0 && _reloj > 20)
                {
                    Vector2 hacia = _presa.Center - Projectile.Center;
                    if (hacia.LengthSquared() > 1f)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                            Projectile.Center, Vector2.Normalize(hacia) * 17f,
                            ModContent.ProjectileType<OndaSonoraEterna>(),
                            (int)(Projectile.damage * 0.5f), 0f, Projectile.owner);
                        if (!Main.dedServ)
                            Terraria.Audio.SoundEngine.PlaySound(
                                SoundID.Item117.WithPitchOffset(0.3f), Projectile.Center);
                    }
                }
            }
            else
            {
                // sin presa: deriva serena hacia arriba.
                Projectile.velocity *= 0.96f;
                Projectile.velocity.Y -= 0.04f;
            }

            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                    Main.rand.NextVector2Circular(0.5f, 0.5f), 150,
                    new Color(120, 170, 255), 0.6f);
                d.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.2f, 0.35f, 0.6f);
        }

        public override bool PreDraw(ref Microsoft.Xna.Framework.Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            try
            {
                Vector2 pos = Projectile.Center - Main.screenPosition;
                var glow = DefinitivasLib.Tex("AethonMod/Content/Effects/Procedural/SoftGlow");
                if (glow != null)
                {
                    VFXCore.AbrirAdditive();
                    try
                    {
                        Main.spriteBatch.Draw(glow, pos, null,
                            new Color(120, 170, 255) * 0.5f, 0f, glow.Size() * 0.5f,
                            0.45f, SpriteEffects.None, 0f);
                    }
                    finally { VFXCore.RestaurarLoteVanilla(); }
                }
            }
            catch { }
            return false;
        }
    }

    /// <summary>LA ONDA DE SONIDO DEL ALMA DE ECO: el arco azul que
    /// ATRAVIESA a todos los que cruza (la ficha: «ondas de sonido que
    /// atraviesan»).</summary>
    public class OndaSonoraEterna : ModProjectile
    {
        // EL Lienzo de la casa: TODO su dibujado es código (HazLib / los
        // arcos de la casa) — la textura es el placeholder de siempre.
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Throwing;
            Projectile.penetrate = -1;             // ATRAVIESA a todos
            Projectile.timeLeft = 36;
            Projectile.tileCollide = false;
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                    -Projectile.velocity * 0.05f, 140, new Color(140, 185, 255), 0.55f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Microsoft.Xna.Framework.Color lightColor)
        {
            // EL ARCO DE LA ONDA: el Slash procedural de la casa, girado a
            // la trayectoria — el arco de sonido que atraviesa.
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            try
            {
                var slash = DefinitivasLib.Tex("AethonMod/Content/Effects/Procedural/Slash");
                if (slash != null)
                {
                    Vector2 pos = Projectile.Center - Main.screenPosition;
                    VFXCore.AbrirAdditive();
                    try
                    {
                        Main.spriteBatch.Draw(slash, pos, null,
                            new Color(150, 190, 255) * 0.75f,
                            Projectile.velocity.ToRotation() + MathHelper.PiOver2,
                            slash.Size() * 0.5f, 0.8f, SpriteEffects.None, 0f);
                    }
                    finally { VFXCore.RestaurarLoteVanilla(); }
                }
            }
            catch { }
            return false;
        }
    }

    /// <summary>EL ALMA DE LUZ INFERIOR (冥煌之魂): vuela al jugador, lo
    /// CURA y le regala LA SERVIDUMBRE (la ficha) — luego se disuelve.</summary>
    public class AlmaLuzInferior : ModProjectile
    {
        // EL Lienzo de la casa: TODO su dibujado es código (HazLib / los
        // arcos de la casa) — la textura es el placeholder de siempre.
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;       // no daña: es un REGALO
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active || dueno.dead)
            {
                Projectile.Kill();
                return;
            }

            // EL VUELO AL PORTADOR: serpentea suave hasta su dueño.
            Vector2 hacia = dueno.Center - Projectile.Center;
            Projectile.velocity += hacia * 0.006f;
            Projectile.velocity += new Vector2(
                MathF.Sin(Projectile.timeLeft * 0.2f) * 0.12f, 0f);
            float tope = MathHelper.Min(11f, 3f + hacia.Length() * 0.02f);
            if (Projectile.velocity.Length() > tope)
                Projectile.velocity = Vector2.Normalize(Projectile.velocity) * tope;

            // EL REGALO: al TOCAR a su dueño — la CURA y la SERVIDUMBRE
            // (la ficha: «intentan curarlo y otorgar la servidumbre»).
            if (Projectile.Hitbox.Intersects(dueno.Hitbox))
            {
                dueno.Heal(3);
                dueno.AddBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.Investigaciones.Servidumbre>(), 180);
                if (!Main.dedServ)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, dueno.Center);
                    for (int i = 0; i < 8; i++)
                    {
                        Dust d = Dust.NewDustPerfect(dueno.Center, DustID.Enchanted_Gold,
                            Main.rand.NextVector2Circular(4f, 4f), 200,
                            new Color(255, 232, 160), 0.9f);
                        d.noGravity = true;
                    }
                }
                Projectile.Kill();
                return;
            }

            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold,
                    -Projectile.velocity * 0.05f, 190, new Color(255, 226, 150), 0.6f);
                d.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.5f, 0.4f, 0.18f);
        }

        public override bool PreDraw(ref Microsoft.Xna.Framework.Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            try
            {
                Vector2 pos = Projectile.Center - Main.screenPosition;
                var glow = DefinitivasLib.Tex("AethonMod/Content/Effects/Procedural/SoftGlow");
                if (glow != null)
                {
                    VFXCore.AbrirAdditive();
                    try
                    {
                        Main.spriteBatch.Draw(glow, pos, null,
                            new Color(255, 224, 140) * 0.55f, 0f, glow.Size() * 0.5f,
                            0.45f, SpriteEffects.None, 0f);
                    }
                    finally { VFXCore.RestaurarLoteVanilla(); }
                }
            }
            catch { }
            return false;
        }
    }

    /// <summary>El buscador compartido de las almas (la presa más
    /// cercana que pueda perseguirse).</summary>
    internal static class DagaEternaBuscar
    {
        public static NPC Buscar(float max, Vector2 desde)
        {
            NPC mejor = null;
            float mejorD = max * max;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                float d = n.Center.DistanceSQ(desde);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }
    }
}
