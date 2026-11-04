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
    // ======================================================================
    //  v6.93 — EL RENGLÓN FINAL (la sesión de HAZLIB; la letra del usuario:
    //  «ahora usando lo nuevo aprendido crea una nueva arma definitiva»).
    //  La librería de haces acaba de nacer y esta es SU PRIMERA DEFINITIVA:
    //  el arma entera es un HazLib.PrepararPar con personalidad propia.
    //
    //  LA IDEA: el grimorio devora armas desde la primera página… y al final
    //  de la última queda UNA línea por escribir. EL RENGLÓN FINAL no es un
    //  rayo que sale del jugador: es una BARRA DE TINTA DE 4.800 PX QUE
    //  ATRAVIESA AL ESCRITOR — el eje del haz va de PUNTA A PUNTA y el
    //  jugador vive EN MEDIO, en el charco dorado donde la pluma muerde el
    //  papel. Nada de boca→objetivo (eso es la Mirada) ni de cono de carga
    //  (eso es el RC2): aquí el PAR es el renglón entero, y el Eterno lo
    //  hace GIRAR — UNA VUELTA ENTERA a la página, borrando todo lo que
    //  la línea toca. El ataque ES la página que se escribe sola.
    //
    //  LAS FASES (el guion de la sentencia):
    //    1. LA CITACIÓN (36 ticks, 0,6 s): al mantener el clic nace
    //       RenglonCargaProj — el ritual: el reglón fantasma se traza
    //       entre DOS PLUMAS estelares que giran una vuelta enfrentadas
    //       mientras las chispas caen a la línea. Suena RunaCarga (el
    //       rugido rúnico de la casa). Si el canal se rompe, la cita muere
    //       sin renglón.
    //    2. EL RENGLÓN (480 actualizaciones, 4 s): el trueno y nace
    //       RenglonLineaProj. EL TRAZO (26 act.): la barra se extiende a
    //       ambos lados del escritor hasta 2.400 px por lado, siguiendo al
    //       ratón. LA ESCRITURA (406 act.): la barra gira UNA REVOLUCIÓN
    //       ENTERA a velocidad de manecilla (arranque suave), con daño
    //       sostenido a lo largo de TODA la línea. LAS COPIAS DE TINTA: dos
    //       reglones fantasma (13 y 26 act. atrás) quedan escritos un
    //       instante — la espiral violeta que la página recuerda.
    //    3. EL PUNTO FINAL (48 act.): el renglón se CONTRAE sobre el
    //       escritor mientras crece el período blanco… y estalla: el anillo
    //       de tinta dorada y violeta, el trueno del punto final.
    //
    //  EL RENDER — TODO sobre HazLib (cero picos por fórmula, la ley de la
    //  casa): cada renglón (el vivo + los dos fantasmas) son DOS PARES
    //  apilados en el pozo de UN draw call:
    //    · EL VELO (×1.85 de ancho, FilasVelo): la cúpula pow(4y(1−y), 2.5)·3
    //      — el halo de tinta que respira alrededor del renglón.
    //    · EL CUERPO (FilasNucleo): la tinta con su GRUMO DE DOS OCTAVAS
    //      (RuidoDendritico en RAM, muestreo bilineal con envoltura), su
    //      NÚCLEO DE PLUMA blanco cálido (1/d), su CHARCO DORADO en el
    //      escritor (x≈0,5 vira al oro), su FOGONAZO en las puntas mientras
    //      el trazo se extiende y su PERÍODO blanco que crece al cerrar.
    //  La COMBA del eje ondula ±34 px (sin(t·0,045)) — la tinta viva cede —
    //  y se TENSA al cerrar (la regla se endereza para el punto final).
    //  El filo de borde rgb=0 lo pone SIEMPRE la librería: imposible que
    //  un pico de brillo encienda.
    //
    //  CERO-GC: las leyes son MÉTODOS ESTÁTICOS cacheados en static
    //  readonly (una alocación al cargar el tipo); el estado que leen —
    //  largo, tiempo y el factor de fantasma — vive en tres casillas
    //  estáticas que se refrescan al empezar cada hook. Nada de closures.
    // ======================================================================

    /// <summary>EL ARMA — El Renglón Final: la citación ritual y la última
    /// línea que el grimorio escribe sobre el mundo.</summary>
    public class RenglonFinal : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 600;               // daño de definitiva (el RC2: 500; la Mirada: 800)
            Item.DamageType = DamageClass.Magic;
            Item.width = 40; Item.height = 40;
            Item.useTime = 36; Item.useAnimation = 36;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;             // LA CANALIZACIÓN DEL RITUAL
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<RenglonCargaProj>();
            Item.shootSpeed = 1f;
            Item.mana = 10;                  // el precio de las definitivas
            Item.ArmorPenetration = 500;     // la pluma no pregunta por armaduras
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
        }

        public override bool CanUseItem(Player player)
        {
            // UNA sentencia a la vez: ni carga ni renglón vivo.
            int cita = ModContent.ProjectileType<RenglonCargaProj>();
            int renglon = ModContent.ProjectileType<RenglonLineaProj>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    (p.type == cita || p.type == renglon))
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA CITACIÓN nace sobre el escritor, mirando al cursor.
            Vector2 dir = (Main.MouseWorld - player.Center).SafeNormalize(-Vector2.UnitY);
            Projectile.NewProjectile(source, player.Center, Vector2.Zero, type,
                damage, knockback, player.whoAmI, dir.ToRotation(), 0f);

            // EL RUGIDO DE LA CITACIÓN — RunaCarga, el sonido rúnico de la casa.
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                try
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        new Terraria.Audio.SoundStyle(
                            "AethonMod/Content/Sounds/RunaCarga",
                            Terraria.Audio.SoundType.Sound)
                        {
                            MaxInstances = 1,
                            SoundLimitBehavior = Terraria.Audio.SoundLimitBehavior.ReplaceOldest,
                            Volume = 0.55f,
                            Pitch = -0.10f
                        },
                        player.Center);
                }
                catch { }
            }
            return false;
        }

        public override void AddRecipes()
        {
            // EL PROTOCOLO DE LA CASA: la madera de siempre.
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // ======================================================================
    //  RenglonCargaProj — LA CITACIÓN (36 ticks, 0,6 s): el reglón fantasma
    //  trazado entre dos plumas estelares mientras las chispas caen a la
    //  línea. Si el canal se rompe, la cita muere SIN renglón.
    // ======================================================================
    public class RenglonCargaProj : ModProjectile
    {
        /// <summary>La duración del ritual (en ticks).</summary>
        private const float Ritual = 36f;

        /// <summary>El progreso suavizado de la cita (0→1) — lo leen las leyes.</summary>
        private static float _ritual;

        private static readonly HazLib.LeyNodo LeyTinteRitual = TinteRitual;

        public override string Texture => "AethonMod/Content/Effects/Procedural/Star";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 36;          // 0,6 s de ritual
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (owner == null || !owner.active || owner.dead ||
                !owner.channel)
            {
                // EL CANAL SE ROMPIÓ: la cita muere sin renglón.
                Projectile.Kill();
                return;
            }

            // EL ÁNGULO persigue al ratón (el renglón se alinea antes de la tinta).
            Vector2 dir = (Main.MouseWorld - owner.Center).SafeNormalize(-Vector2.UnitY);
            Projectile.ai[0] = dir.ToRotation();
            Projectile.Center = owner.Center;

            // EL ALCANCE de la regla fantasma (60 → 210 px por lado).
            float r = 1f - Projectile.timeLeft / Ritual;
            float suave = r * r * (3f - 2f * r);
            float alcance = 60f + 150f * suave;

            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                // EL POLVO DE LA CITACIÓN: chispas que caen AL RENGLÓN desde
                // ambos lados — el cielo de tinta que se pliega sobre la línea.
                float lado = Main.rand.NextBool() ? 1f : -1f;
                Vector2 perp = new Vector2(-dir.Y, dir.X);
                Vector2 pos = Projectile.Center
                    + dir * Main.rand.NextFloat(-alcance, alcance)
                    + perp * (lado * Main.rand.NextFloat(40f, 160f));
                Dust d = Dust.NewDustPerfect(pos,
                    Main.rand.NextBool() ? DustID.Electric : DustID.PortalBoltTrail,
                    Vector2.Zero, 0,
                    Main.rand.NextBool() ? new Color(255, 208, 120) : new Color(188, 108, 255),
                    0.9f);
                if (d != null)
                {
                    // LA CAÍDA: cada chispa busca SU punto del renglón (la
                    // proyección sobre el eje) y cae a él a 10 u/s.
                    Vector2 rel = pos - Projectile.Center;
                    Vector2 objetivo = Projectile.Center + dir * Vector2.Dot(rel, dir);
                    Vector2 hacia = objetivo - pos;
                    if (hacia.LengthSquared() > 1f)
                        d.velocity = hacia / hacia.Length() * 10f;
                    d.noGravity = true;
                }
            }

            if (Projectile.timeLeft <= 1)
            {
                // === EL RENGLÓN NACE: el trueno de la casa y la línea viva. ===
                if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                {
                    try
                    {
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item122.WithPitchOffset(-0.30f).WithVolumeScale(1.05f),
                            Projectile.Center);
                    }
                    catch { }
                }

                Projectile renglon = Projectile.NewProjectileDirect(
                    Projectile.GetSource_FromAI(), owner.Center, Vector2.Zero,
                    ModContent.ProjectileType<RenglonLineaProj>(),
                    Projectile.damage, 6f, Projectile.owner,
                    Projectile.ai[0], 0f);
                renglon.netUpdate = true;
            }
        }

        /// <summary>EL TINTE DEL REGLÓN FANTASMA: violeta tenue que prende con
        /// el ritual (0,08 → 0,30) y parpadea a 40 Hz — la regla lápiz.</summary>
        private static Color TinteRitual(float p)
        {
            float k = (0.08f + 0.22f * _ritual)
                * (0.8f + 0.2f * MathF.Sin(Main.GlobalTimeWrappedHourly * 40f));
            return new Color(172, 82, 255)
                * (k * Tinta.Rampa(0f, 0.08f, p) * Tinta.Rampa(1f, 0.92f, p));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;
            try
            {
                if (!RecursosRenglon.AsegurarCargado()) return false;
                Player owner = Main.player[Projectile.owner];
                if (owner == null || !owner.active) return false;

                // EL PROGRESO (suavizado) — lo leen las leyes del ritual.
                float r = 1f - Projectile.timeLeft / Ritual;
                float suave = r * r * (3f - 2f * r);
                _ritual = suave;

                Vector2 dir = Projectile.ai[0].ToRotationVector2();
                Vector2 centro = Projectile.Center - Main.screenPosition;
                float alcance = 60f + 150f * suave;
                float semi = 2f + 12f * suave;

                // EL REGLÓN TRAZADO — la regla fantasma entre las dos plumas
                // (un PAR liso de HazLib: el tinte tal cual, el filo de la casa).
                HazLib.Abrir();
                try
                {
                    HazLib.PrepararPar(centro - dir * alcance, centro + dir * alcance,
                        semi, semi, 0f, 22);
                    HazLib.Dibujar(LeyTinteRitual, null, HazLib.FilasNucleo);
                }
                finally { HazLib.Cerrar(); }

                // LAS PLUMAS DE LA CITACIÓN — dos sellos estelares enfrentados
                // que giran UNA vuelta en sentidos opuestos, y el oro del escritor.
                VFXCore.AbrirAdditive();
                try
                {
                    Texture2D glow = RecursosRenglon.Glow;
                    Texture2D flare = RecursosRenglon.Flare;
                    if (glow == null || flare == null) return false;

                    float t = Main.GlobalTimeWrappedHourly;
                    Color violeta = new Color(196, 128, 255, 0);
                    Color dorado = new Color(255, 208, 120, 0);
                    Vector2 ojoGlow = glow.Size() * 0.5f;
                    Vector2 ojoFlare = flare.Size() * 0.5f;
                    for (int lado = -1; lado <= 1; lado += 2)
                    {
                        Vector2 pos = centro + dir * (alcance * lado);
                        Main.spriteBatch.Draw(glow, pos, null, violeta, 0f,
                            ojoGlow, 0.28f + 0.35f * suave, SpriteEffects.None, 0f);
                        Main.spriteBatch.Draw(flare, pos, null, violeta,
                            lado * suave * MathHelper.TwoPi, ojoFlare,
                            0.30f + 0.35f * suave, SpriteEffects.None, 0f);
                    }
                    Main.spriteBatch.Draw(glow, centro, null, dorado, 0f,
                        ojoGlow, 0.30f + 0.40f * suave, SpriteEffects.None, 0f);
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                finally { VFXCore.RestaurarLoteVanilla(); }
            }
            catch { }
            return false;
        }
    }

    // ======================================================================
    // ======================================================================
    //  RenglonLineaProj — EL RENGLÓN (480 actualizaciones, 4 s): la barra
    //  de tinta de 4.800 px que ATRAVIESA al escritor y da UNA VUELTA
    //  ENTERA a la página. El eje, el filo y el lote los pone HazLib; las
    //  leyes propias (el grumo, el charco, las plumas, el punto final)
    //  viven abajo — CERO PICOS por FÓRMULA.
    // ======================================================================
    public class RenglonLineaProj : ModProjectile
    {
        /// <summary>El alcance del renglón POR LADO (la barra mide el doble).</summary>
        private const float LargoMax = 2400f;

        /// <summary>La vida útil en actualizaciones (extraUpdates 1 → 4 s).</summary>
        private const float VidaUtil = 480f;

        /// <summary>El trazo se extiende hasta aquí (26 act.).</summary>
        private const float TrazoFin = 26f;

        /// <summary>El punto final empieza a contraer aquí (432 act.).</summary>
        private const float CierreIni = 432f;

        /// <summary>El semiancho base de la tinta en el renglón.</summary>
        private const float AnchoBase = 64f;

        /// <summary>Las actualizaciones que dura UNA REVOLUCIÓN de la página.</summary>
        private const float Vuelta = 406f;

        /// <summary>La velocidad angular de la manecilla (rad/actualización).</summary>
        private const float Omega = MathHelper.TwoPi / Vuelta;

        /// <summary>EL TIEMPO del renglón (ai[1]: 0 → 480, una por actualización).</summary>
        private float Tiempo { get => Projectile.ai[1]; set => Projectile.ai[1] = value; }

        /// <summary>El largo por lado (localAI[0] — viaja por el cable).</summary>
        private float Largo { get => Projectile.localAI[0]; set => Projectile.localAI[0] = value; }

        // ================================================================
        //  EL ESTADO DE LAS LEYES — tres casillas estáticas
        //
        //  Las leyes (anchuras, tintes, vértices) son MÉTODOS ESTÁTICOS
        //  cacheados en static readonly (cero-GC). El estado que leen — el
        //  largo, el tiempo y EL FACTOR DE FANTASMA (1 = el renglón vivo;
        //  0,58/0,30 = las copias de tinta que la página recuerda) — se
        //  refresca al empezar cada hook que las necesite.
        // ================================================================
        private static float _leyLargo;
        private static float _leyTiempo;
        private static float _leyFantasma = 1f;

        private void RefrescarLeyes()
        {
            _leyLargo = Largo;
            _leyTiempo = Tiempo;
            _leyFantasma = 1f;   // el presente es el renglón VIVO (los fantasmas lo pisan al dibujarse)
        }

        // ================================================================
        //  LAS LEYES CACHEADAS — los delegados de HazLib
        // ================================================================
        private static readonly HazLib.LeyAncho LeySemianchoCuerpo = Semiancho;
        private static readonly HazLib.LeyAncho LeySemianchoVelo = SemianchoVelo;
        private static readonly HazLib.LeyNodo LeyTinteCuerpo = TinteCuerpo;
        private static readonly HazLib.LeyNodo LeyTinteVelo = TinteVelo;
        private static readonly HazLib.LeyVertice LeyVerticeCuerpo = VerticeCuerpo;
        private static readonly HazLib.LeyVertice LeyVerticeVelo = VerticeVelo;

        public override string Texture => "AethonMod/Content/Effects/Procedural/Star";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 8000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 500;         // la AI mata en 480 (el punto final)
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;   // daño sostenido: ~8 golpes/s
            Projectile.DamageType = DamageClass.Magic;
            Projectile.aiStyle = -1;
            Projectile.hide = true;            // se esconde…
        }

        // …y dibuja SOBRE los jugadores.
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles,
            List<int> behindNPCs, List<int> behindProjectiles,
            List<int> overPlayers, List<int> overWiresUI)
            => overPlayers.Add(index);

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (owner == null || !owner.active)
            {
                Projectile.Kill();
                return;
            }

            float u = Tiempo;

            // === EL ÁNGULO: el trazo sigue al ratón; la escritura gira como
            //     una manecilla (arranque suave — la pluma toma impulso). ===
            if (u < TrazoFin)
            {
                float objetivo = (Main.MouseWorld - owner.Center).ToRotation();
                float delta = MathHelper.WrapAngle(objetivo - Projectile.ai[0]);
                Projectile.ai[0] += delta * 0.25f;
            }
            else
            {
                float acel = Tinta.Rampa(TrazoFin, TrazoFin + 45f, u);
                Projectile.ai[0] += Omega * (0.22f + 0.78f * acel * acel);
            }

            // === EL LARGO: el trazo se extiende (26 act.), la página se
            //     escribe entera, y el punto final lo contrae (50 px/act.). ===
            if (u < TrazoFin) Largo = LargoMax * (u + 1f) / TrazoFin;
            else if (u < CierreIni) Largo = LargoMax;
            else Largo = MathF.Max(0f, Largo - 50f);

            // === LA POSICIÓN: el renglón ATRAVIESA al escritor. ===
            Projectile.Center = owner.Center;
            Projectile.rotation = Projectile.ai[0];

            Tiempo++;
            RefrescarLeyes();    // las leyes leen el presente

            // === LA LUZ (3 puntos: el escritor y las dos plumas) y LAS
            //     CHISPAS: las plumas escupen tinta hacia atrás del barrido
            //     y el filo del renglón suelta polvo. ===
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                Vector2 dir = Projectile.ai[0].ToRotationVector2();
                Vector2 tang = new Vector2(-dir.Y, dir.X);
                Vector2 plumaA = Projectile.Center - dir * Largo;
                Vector2 plumaB = Projectile.Center + dir * Largo;

                Lighting.AddLight(Projectile.Center, new Vector3(0.95f, 0.72f, 1.00f) * 0.95f);
                Lighting.AddLight(plumaA, new Vector3(0.62f, 0.28f, 1.00f) * 0.85f);
                Lighting.AddLight(plumaB, new Vector3(0.62f, 0.28f, 1.00f) * 0.85f);

                // LAS PLUMAS: la tinta del giro queda ATRÁS de cada punta.
                for (int i = 0; i < 2; i++)
                {
                    Vector2 punta = i == 0 ? plumaA : plumaB;
                    float lado = i == 0 ? -1f : 1f;
                    Dust d = Dust.NewDustPerfect(punta,
                        Main.rand.NextBool() ? DustID.Electric : DustID.PortalBoltTrail,
                        -tang * lado * Main.rand.NextFloat(3f, 12f)
                            + dir * Main.rand.NextFloat(-3f, 3f),
                        0,
                        Main.rand.NextBool() ? new Color(255, 208, 120) : new Color(188, 108, 255),
                        Main.rand.NextFloat(0.8f, 1.4f));
                    if (d != null) d.noGravity = true;
                }

                // EL FILO: el borde del renglón escupe polvo a los lados.
                float semi = Semiancho(0.5f);
                for (int i = 0; i < 2; i++)
                {
                    float tramo = i == 0 ? 0.32f : 0.68f;
                    float lado = i == 0 ? 1f : -1f;
                    Dust d = Dust.NewDustPerfect(
                        Vector2.Lerp(plumaA, plumaB, tramo) + tang * (semi * 0.9f * lado),
                        DustID.PortalBoltTrail,
                        tang * lado * Main.rand.NextFloat(2f, 7f), 0,
                        Main.rand.NextBool() ? new Color(150, 90, 255) : new Color(255, 226, 150),
                        Main.rand.NextFloat(0.7f, 1.2f));
                    if (d != null) d.noGravity = true;
                }
            }

            // LA SINCRONÍA: un empujón cada 30 actualizaciones basta (el giro
            // es determinista entre empujones).
            if (((int)Tiempo % 30) == 0) Projectile.netUpdate = true;

            // === EL PUNTO FINAL: la contracción acaba y el período ESTALLA. ===
            if (Tiempo >= VidaUtil)
            {
                if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                {
                    try
                    {
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item14.WithPitchOffset(-0.25f).WithVolumeScale(0.9f),
                            Projectile.Center);
                    }
                    catch { }

                    // EL ANILLO DE TINTA: 18 polvos dorados y violetas.
                    for (int i = 0; i < 18; i++)
                    {
                        Vector2 hacia = new Vector2(1f, 0f).RotatedBy(
                            MathHelper.TwoPi * i / 18f);
                        Dust d = Dust.NewDustPerfect(Projectile.Center,
                            Main.rand.NextBool() ? DustID.Electric : DustID.PortalBoltTrail,
                            hacia * Main.rand.NextFloat(6f, 16f), 0,
                            Main.rand.NextBool() ? new Color(255, 208, 120) : new Color(188, 108, 255),
                            Main.rand.NextFloat(1.0f, 1.6f));
                        if (d != null) d.noGravity = true;
                    }
                }
                Projectile.Kill();
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // LA COLISIÓN: TODA la línea (del 97 % de un lado al otro), ancho
            // de ley en el escritor ×1,7 — el renglón golpea de punta a punta.
            if (Largo < 40f) return false;
            RefrescarLeyes();
            Vector2 dir = Projectile.ai[0].ToRotationVector2();
            Vector2 a = Projectile.Center - dir * (Largo * 0.97f);
            Vector2 b = Projectile.Center + dir * (Largo * 0.97f);
            float punto = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                a, b, Semiancho(0.5f) * 1.7f, ref punto);
        }

        // ================================================================
        //  LAS LEYES DEL ASPECTO — la tinta de la última línea
        // ================================================================

        /// <summary>EL SEMIANCHO del renglón en el nodo p (0 = la pluma A,
        /// 1 = la pluma B; el escritor vive en 0,5): las PUNTAS REDONDAS
        /// (círculo en el último 14 % de cada extremo), el CHARCO del
        /// escritor (embolsamiento ×1,62 en el centro), el LATIDO de la
        /// tinta (11 Hz), el nacimiento fino del trazo y el desinflado del
        /// cierre. Los fantasmas salen algo más finos (×0,55+0,45·f).</summary>
        private static float Semiancho(float p)
        {
            float rel = _leyLargo / LargoMax;

            // LAS PUNTAS REDONDAS — el círculo del extremo.
            float q = MathF.Abs(p * 2f - 1f) / 0.86f;
            float punta = q >= 1f ? 0f : MathF.Sqrt(1.001f - q * q);

            // EL CHARCO — la tinta se embolsa donde la pluma muerde el papel.
            float eje = MathF.Abs(p * 2f - 1f);
            float charco = 1f + 0.62f * (1f - eje) * (1f - eje);

            // EL LATIDO de la tinta y el grosor que nace fino y cuaja.
            float latido = MathF.Cos(Main.GlobalTimeWrappedHourly * 70f) * 4.5f * rel;
            float grosor = MathHelper.Lerp(AnchoBase * 0.55f, AnchoBase, rel * rel);

            float nace = Tinta.Rampa(0f, 4f, _leyTiempo);
            float cierre = Tinta.Rampa(0f, 10f, VidaUtil - _leyTiempo);
            float f = 0.55f + 0.45f * _leyFantasma;
            return (grosor * punta * charco + latido) * nace * cierre * f;
        }

        /// <summary>El velo es el renglón ×1,85 — el halo de tinta que respira.</summary>
        private static float SemianchoVelo(float p) => Semiancho(p) * 1.85f;

        /// <summary>EL TINTE del cuerpo: violeta de tinta del Eterno (172,82,255)
        /// que nunca se corta en seco (las puntas funden en el último 2 %).
        /// El cuadrado del fantasma apaga las copias (1 / 0,34 / 0,09).</summary>
        private static Color TinteCuerpo(float p)
        {
            float k = _leyFantasma * _leyFantasma * Tinta.Rampa(0f, 3f, _leyTiempo);
            return new Color(172, 82, 255)
                * (k * Tinta.Rampa(0f, 0.02f, p) * Tinta.Rampa(1f, 0.98f, p));
        }

        /// <summary>EL TINTE del velo: violeta profundo (120,40,210) al 30 %.</summary>
        private static Color TinteVelo(float p)
        {
            float k = _leyFantasma * _leyFantasma;
            return new Color(120, 40, 210)
                * (0.30f * k * Tinta.Rampa(0.02f, 0.05f, p) * Tinta.Rampa(0.98f, 0.95f, p));
        }

        /// <summary>EL VÉRTICE del cuerpo — el INTERIOR del renglón (el borde
        /// lo pone HazLib): el GRUMO DE DOS OCTAVAS (la gruesa fluye lenta
        /// por el renglón, la fina rasga rápido en contra), el NÚCLEO DE
        /// PLUMA blanco cálido hacia el eje, el CHARCO DORADO del escritor,
        /// el FOGONAZO de las puntas durante el trazo y el PERÍODO blanco
        /// que crece al cerrar. Todos los aditivos se escalan por el factor
        /// de fantasma: las copias son tinta vieja, más tenue.</summary>
        private static Color VerticeCuerpo(float x, float y, Color tinte)
        {
            float t = Main.GlobalTimeWrappedHourly;
            float f = _leyFantasma;

            // EL GRUMO DE TINTA — dos octavas del ruido dendrítico.
            float n = 0.62f * RecursosRenglon.Muestrear(
                          Tinta.Frac(x * 2.4f - t * 1.7f),
                          Tinta.Frac(y * 3.2f + x * 0.6f + t * 0.4f))
                    + 0.38f * RecursosRenglon.Muestrear(
                          Tinta.Frac(x * 5.5f + t * 2.3f),
                          Tinta.Frac(y * 1.6f - t * 0.8f));
            float opacidad = Tinta.Smoothstep(0.30f, 0.46f, n);

            // EL NÚCLEO DE PLUMA — la vena blanca cálida hacia el eje.
            float d = MathF.Abs(y - 0.5f);
            float razon = d <= 0.0001f ? 1f : Tinta.Saturar((0.015f + 0.92f * n) / d);
            float brillo = razon * Tinta.Smoothstep(0.28f, 0.10f, d) * f;

            // EL CHARCO DORADO DEL ESCRITOR — el centro del renglón vira al
            // oro: ahí está la pluma que escribe (el lerp NO extrapola).
            float charco = Tinta.Smoothstep(0.38f, 0.50f, x) * Tinta.Smoothstep(0.62f, 0.50f, x);
            float mezcla = Math.Min(1f, charco * 0.85f + n * 0.10f);
            Vector4 color = Vector4.Lerp(tinte.ToVector4(), RecursosRenglon.Dorado, mezcla);

            // EL FOGONAZO DE LAS PLUMAS — las puntas arden mientras el trazo
            // se extiende (y se apagan cuando la página ya está escrita).
            float plumas = Tinta.Smoothstep(0.09f, 0.01f, Math.Min(x, 1f - x))
                         * Tinta.Rampa(TrazoFin + 40f, TrazoFin, _leyTiempo) * f;

            // EL PERÍODO — el punto blanco que crece en el escritor al cerrar.
            float periodo = Tinta.Smoothstep(0.46f, 0.50f, x) * Tinta.Smoothstep(0.54f, 0.50f, x)
                          * Tinta.Rampa(CierreIni + 18f, VidaUtil - 6f, _leyTiempo) * f;

            // LA SUMA SATURADA — el filo de borde lo pone la librería encima.
            Vector4 suma = color * opacidad
                + new Vector4(brillo, brillo * 0.96f, brillo * 0.86f, 0f)
                + new Vector4(plumas * 0.98f, plumas * 0.88f, plumas * 0.62f, 0f)
                + new Vector4(periodo, periodo, periodo * 0.96f, 0f);
            return Tinta.AColor(Tinta.Saturar4(suma));
        }

        /// <summary>EL VÉRTICE del velo — la CÚPULA: pow(4y(1−y), 2.5)·3.
        /// En y=0/1 la potencia es 0 EXACTO: el halo nace y muere en el
        /// borde sin encender nada (el filo de HazLib encima es invisible).</summary>
        private static Color VerticeVelo(float x, float y, Color tinte)
        {
            float bulto = y * (4f - y * 4f);
            float k = bulto * bulto * MathF.Sqrt(bulto) * 3f;
            return Tinta.AColor(tinte.ToVector4() * k);
        }

        /// <summary>LA COMBA del eje — la tinta viva cede (±34 px a 0,045
        /// rad/act.), nace tensa y SE TENSA al cerrar: la regla se endereza
        /// para escribir el punto final.</summary>
        private static float Comba(float tiempo)
            => MathF.Sin(tiempo * 0.045f) * 34f
             * Tinta.Rampa(0f, 20f, tiempo)
             * Tinta.Rampa(0f, 20f, VidaUtil - tiempo);

        // ================================================================
        //  EL RENDER — los fantasmas + el renglón vivo sobre HazLib, UN
        //  draw call; después los focos (el escritor, las plumas, el período)
        //  en el lote aditivo de la casa.
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;
            try
            {
                if (!RecursosRenglon.AsegurarCargado()) return false;
                Player owner = Main.player[Projectile.owner];
                if (owner == null || !owner.active) return false;
                RefrescarLeyes();

                Vector2 centro = Projectile.Center - Main.screenPosition;

                HazLib.Abrir();
                try
                {
                    // === LAS COPIAS DE TINTA — los reglones ya escritos que
                    //     la página recuerda un instante (13 y 26 act. atrás):
                    //     la espiral violeta del barrido. Debajo del vivo. ===
                    if (_leyTiempo > TrazoFin + 8f)
                    {
                        for (int g = 2; g >= 1; g--)
                        {
                            float retraso = 13f * g;
                            _leyFantasma = g == 2 ? 0.30f : 0.58f;
                            Vector2 gdir = (Projectile.ai[0] - Omega * retraso).ToRotationVector2();
                            Vector2 ga = centro - gdir * Largo;
                            Vector2 gb = centro + gdir * Largo;
                            float gc = Comba(_leyTiempo - retraso);

                            HazLib.PrepararPar(ga, gb, LeySemianchoVelo, gc, 40);
                            HazLib.Dibujar(LeyTinteVelo, LeyVerticeVelo, HazLib.FilasVelo);
                            HazLib.PrepararPar(ga, gb, LeySemianchoCuerpo, gc, 56);
                            HazLib.Dibujar(LeyTinteCuerpo, LeyVerticeCuerpo, HazLib.FilasNucleo);
                        }
                    }

                    // === EL RENGLÓN VIVO — velo debajo, cuerpo encima. ===
                    _leyFantasma = 1f;
                    Vector2 dir = Projectile.ai[0].ToRotationVector2();
                    Vector2 a = centro - dir * Largo;
                    Vector2 b = centro + dir * Largo;
                    float comba = Comba(_leyTiempo);

                    HazLib.PrepararPar(a, b, LeySemianchoVelo, comba, 48);
                    HazLib.Dibujar(LeyTinteVelo, LeyVerticeVelo, HazLib.FilasVelo);
                    HazLib.PrepararPar(a, b, LeySemianchoCuerpo, comba, 84);
                    HazLib.Dibujar(LeyTinteCuerpo, LeyVerticeCuerpo, HazLib.FilasNucleo);
                }
                finally { HazLib.Cerrar(); }

                // === LOS FOCOS — el charco del escritor, las plumas de las
                //     puntas y el período blanco que crece al cerrar. ===
                VFXCore.AbrirAdditive();
                try
                {
                    Texture2D glow = RecursosRenglon.Glow;
                    Texture2D flare = RecursosRenglon.Flare;
                    if (glow == null || flare == null) return false;

                    float t = Main.GlobalTimeWrappedHourly;
                    float latido = 0.5f + 0.5f * MathF.Cos(t * 80f);
                    float brillo = Tinta.Rampa(0f, 10f, _leyTiempo)     // el encendido
                                 * Tinta.Rampa(0f, 60f, Largo)          // el cierre contrae
                                 * MathHelper.Lerp(1f, 1.25f, latido)
                                 * 0.45f;
                    Color dorado = new Color(255, 208, 120, 0);
                    Color violeta = new Color(216, 150, 255, 0);
                    Vector2 ojoGlow = glow.Size() * 0.5f;
                    Vector2 ojoFlare = flare.Size() * 0.5f;

                    // EL ESCRITOR — el charco dorado donde la pluma muerde.
                    Main.spriteBatch.Draw(glow, centro, null, dorado, 0f,
                        ojoGlow, brillo * 2.2f, SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(glow, centro, null, dorado, 0f,
                        ojoGlow, brillo * 1.6f, SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(flare, centro, null, dorado, t * 1.3f,
                        ojoFlare, brillo * 1.5f, SpriteEffects.None, 0f);

                    // EL PERÍODO — el punto blanco final que crece girando.
                    float periodo = Tinta.Rampa(CierreIni + 18f, VidaUtil - 6f, _leyTiempo);
                    if (periodo > 0f)
                        Main.spriteBatch.Draw(flare, centro, null,
                            new Color(255, 255, 248, 0), -t * 6f, ojoFlare,
                            0.6f + periodo * 2.8f, SpriteEffects.None, 0f);

                    // LAS PLUMAS — un sello estelar en cada punta.
                    Vector2 dir = Projectile.ai[0].ToRotationVector2();
                    for (int lado = -1; lado <= 1; lado += 2)
                    {
                        Vector2 pos = centro + dir * (Largo * lado);
                        Main.spriteBatch.Draw(glow, pos, null, violeta, 0f,
                            ojoGlow, brillo * 1.1f, SpriteEffects.None, 0f);
                        Main.spriteBatch.Draw(flare, pos, null,
                            new Color(235, 190, 255, 0), -t * 4f * lado, ojoFlare,
                            brillo * 0.8f, SpriteEffects.None, 0f);
                    }
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                finally { VFXCore.RestaurarLoteVanilla(); }
            }
            catch { }
            return false;
        }
    }

    // ======================================================================
    //  Tinta — LOS AUXILIARES DE LA LEY: la matemática propia del renglón
    //  (rampas que soportan bordes invertidos, smoothstep, frac, saturar).
    // ======================================================================
    internal static class Tinta
    {
        /// <summary>Rampa inversa: v=a → 0, v=b → 1 (soporta rangos
        /// descendentes — los apagados viven de esto).</summary>
        public static float Rampa(float a, float b, float v)
            => MathHelper.Clamp((v - a) / (b - a), 0f, 1f);

        /// <summary>saturate de HLSL, componente a componente.</summary>
        public static float Saturar(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

        public static Vector4 Saturar4(Vector4 v)
            => new Vector4(Saturar(v.X), Saturar(v.Y), Saturar(v.Z), Saturar(v.W));

        /// <summary>El smoothstep de HLSL (soporta bordes invertidos).</summary>
        public static float Smoothstep(float a, float b, float x)
        {
            float t = Saturar((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        /// <summary>frac de HLSL (negativos incluidos: x−floor(x) ∈ [0,1)).</summary>
        public static float Frac(float x) => x - MathF.Floor(x);

        /// <summary>El Vector4 de la ley → el Color byte del vértice (el
        /// alfa no pinta: el lote aditivo de la casa suma el rgb).</summary>
        public static Color AColor(Vector4 v)
            => new Color(Byte255(v.X), Byte255(v.Y), Byte255(v.Z), 255);

        public static byte Byte255(float f)
            => (byte)Math.Clamp((int)(f * 255f + 0.5f), 0, 255);
    }

    // ======================================================================
    //  RecursosRenglon — LAS TEXTURAS DE LA CASA y LA CACHÉ CPU DEL RUIDO:
    //  el ruido dendrítico se vuelca a RAM UNA VEZ (GetData) y se muestrea
    //  bilineal con envoltura — la réplica del sampler en CPU. Tres assets
    //  de la casa: SoftGlow (los focos), FlareEstrella4Puntas (los sellos
    //  estelares) y RuidoDendritico (el grumo de la tinta).
    // ======================================================================
    internal static class RecursosRenglon
    {
        private static Asset<Texture2D> _glow;     // SoftGlow — los focos
        private static Asset<Texture2D> _flare;    // FlareEstrella4Puntas — los sellos
        private static Asset<Texture2D> _ruido;    // RuidoDendritico — el grumo
        private static Color[] _ruidoRam;
        private static int _ancho;
        private static int _alto;

        /// <summary>El dorado del charco del escritor.</summary>
        public static readonly Vector4 Dorado = new Color(255, 206, 96).ToVector4();

        public static Texture2D Glow => _glow?.Value;
        public static Texture2D Flare => _flare?.Value;

        /// <summary>Carga las tres texturas (una vez) y vuelca el ruido a
        /// RAM. False solo si el asset falla (no debería: van con el mod).</summary>
        public static bool AsegurarCargado()
        {
            if (_glow == null)
            {
                try
                {
                    _glow = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/SoftGlow",
                        AssetRequestMode.ImmediateLoad);
                    _flare = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/FlareEstrella4Puntas",
                        AssetRequestMode.ImmediateLoad);
                    _ruido = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/RuidoDendritico",
                        AssetRequestMode.ImmediateLoad);
                }
                catch { return false; }
            }
            if (_ruidoRam == null)
            {
                Texture2D tex = _ruido?.Value;
                if (tex == null || tex.IsDisposed) return false;
                _ancho = tex.Width;
                _alto = tex.Height;
                _ruidoRam = new Color[_ancho * _alto];
                try { tex.GetData(_ruidoRam); }
                catch
                {
                    // EL PARACAÍDAS: gris plano — el renglón vive aunque el
                    // grumo no (jamás debería disparar).
                    for (int i = 0; i < _ruidoRam.Length; i++)
                        _ruidoRam[i] = new Color(128, 128, 128, 255);
                }
            }
            return _glow.Value != null && _flare.Value != null;
        }

        /// <summary>EL SAMPLER del grumo: bilineal con envoltura (u,v ya con
        /// frac aplicado). Lee el canal R de la caché en RAM y da 0..1.</summary>
        public static float Muestrear(float u, float v)
        {
            int w = _ancho, h = _alto;
            float fx = u * w - 0.5f;
            float fy = v * h - 0.5f;
            int x0 = (int)MathF.Floor(fx);
            int y0 = (int)MathF.Floor(fy);
            int xa = Envolver(x0, w);
            int ya = Envolver(y0, h);
            int xb = (xa + 1) % w;
            int yb = (ya + 1) % h;
            float tx = fx - x0;
            float ty = fy - y0;

            Color s00 = _ruidoRam[ya * w + xa];
            Color s10 = _ruidoRam[ya * w + xb];
            Color s01 = _ruidoRam[yb * w + xa];
            Color s11 = _ruidoRam[yb * w + xb];
            float arriba = (s00.R + (s10.R - s00.R) * tx) * (1f / 255f);
            float abajo = (s01.R + (s11.R - s01.R) * tx) * (1f / 255f);
            return arriba + (abajo - arriba) * ty;
        }

        /// <summary>El wrap del sampler (negativos incluidos).</summary>
        private static int Envolver(int i, int n) => ((i % n) + n) % n;
    }
}
