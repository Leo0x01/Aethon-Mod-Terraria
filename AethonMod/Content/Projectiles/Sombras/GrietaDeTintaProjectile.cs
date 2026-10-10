using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.Sombras
{
    /// <summary>
    /// GRIETADETINTA — v6.50.96 — LA GRIETA (el disparo propio del libro).
    /// La letra del usuario: «el nuevo proyectil lo usaste tal cual, no le
    /// diste efectos visuales, digamos que es un grieta como debe ser,
    /// entonces dale muchos efectos visuales investiga que tipo de efectos
    /// se le pueden poner al nuevo proyectil cuanto mas efectos mejor».
    ///
    /// v6.50.95 lo bautizó «Lágrima de Tinta» — v6.50.96 lo RE-LEE como lo
    /// que siempre fue: UNA GRIETA. El sprite EXACTO del usuario (607×1344,
    /// RLE «sin pérdida») no es una lágrima: es el FRAGMENTO NEGRO de una
    /// página DESGARRADA, con el borde violeta de la herida y un corazón de
    /// marfil — UNA GRIETA ABIERTA EN LA REALIDAD, y las grietas NO lloran:
    /// ASPIRAN, MIRAN y DESGARRAN. La .96 le monta encima EL STACK COMPLETO
    /// de efectos de grieta (la investigación: aberración cromática del
    /// umbral, partículas aspiradas por portales, el glitch de tajas, la
    /// estrella de ruptura, el ojo rasgado, el corte que sana):
    ///
    /// EL STACK (lo que vive en cada disparo):
    /// · 1 — EL NACIMIENTO: al abrirse, la ESTRELLA DE RUPTURA (RiftLib.Star
    ///   — la espiga de 4 puntas creciendo con el ANILLO QUE IMPLOSIONA)
    ///   + RÁFAGA de chispas de anomalía (RiftPaletas.Vacio) + el sonido
    ///   grave del desgarro. La grieta NACE rasgando.
    /// · 2 — EL RIBBON DEL VACÍO: estela-cometa violeta (EstelaLib) — el
    ///   rastro de NADA que deja lo abierto.
    /// · 3 — LAS ESTELAS FANTASMA: 3 ecos del sprite por oldPos (la página
    ///   que se deshace detrás).
    /// · 4 — LA ASPIRACIÓN: el mundo ENTRA a la grieta — motas de polvo
    ///   violeta que nacen en anillo y CONVERGEN EN ESPIRAL hacia la boca
    ///   + motas ORBITALES (mini disco de acreción de tinta).
    /// · 5 — LA ABERRACIÓN CROMÁTICA: el sprite tiene DOS ECOS partidos
    ///   (rojo a un lado, cian al otro — la lección UmbralRoto) cuya
    ///   separación LATE con el corazón.
    /// · 6 — EL GLITCH DE TAJOS: cada ~0.5 s una ráfaga corta donde el
    ///   sprite se PARTE en 3 bandas horizontales desplazadas (EcoGlitch)
    ///   — la realidad pierde frames alrededor de la herida.
    /// · 7 — EL HALO DOBLE: violeta de sombra + marfil que late (el .95).
    /// · 8 — EL IRIS: el anillo ELÍPTICO de apertura alrededor de la grieta
    ///   (el diafragma del desgarro, respirando).
    /// · 9 — EL OJO RASGADO: el corazón de marfil ES UN OJO (SombrasLib.Ojo
    ///   rasgado) que SOLO abre cuando hay presa — y MIRA a su presa.
    /// · 10 — LAS ESTRELLAS FUGITIVAS: chispas del OTRO LADO que se escapan
    ///   de la herida cada 18 t (RiftLib.ChispasAnomalia).
    /// · 11 — EL GOTEO: la tinta que gotea de una herida abierta (polvo
    ///   flotante + GOTAS que caen — el .95).
    /// · 12 — LA LUZ PARPADEANTE: luz doble violeta/marfil con FLICKER de
    ///   conexión rota (la electricidad de la realidad herida).
    /// · 13 — EL VAIVÉN + LA RESPIRACIÓN: el mecerse perpendicular (solo
    ///   draw) y los 4 frames del strip (el .95).
    /// · 14 — LA MORDIDA (OnHit): salpicadura en anillo + chispas de
    ///   anomalía donde mordió.
    /// · 15 — EL COLAPSO (OnKill): la grieta se muere HACIA DENTRO —
    ///   IMPLOSIÓN de motas convergentes + estallido + LA MANCHA y
    ///   EL CORTE QUE SANA (la línea con aberración que se cierra —
    ///   «la realidad sana comiéndose el corte», la escuela RiftLib).
    ///
    /// Coherencia total con el libro: atraviesa muros (tileCollide false:
    /// las grietas no conocen puertas), caza como el Nervioso caza, y mira
    /// a su presa como el grimorio mira al jugador.
    /// </summary>
    public class GrietaDeTintaProjectile : ModProjectile
    {
        // LA CACERÍA (el .95 intacto)
        private const float RADIO_BUSCA = 900f;    // a quién busca
        private const int TICKS_REBUSCA = 8;       // cada cuánto re-busca
        private const float VEL_INICIAL = 12f;     // arranca lenta…
        private const float VEL_MAXIMA = 19f;      // …y acelera al caer
        private const float GIRO_MAX = 0.11f;      // rad/t de rumbo

        // LA RESPIRACIÓN
        private const int TICKS_POR_FRAME = 5;

        // EL GOTEO
        private const int TICKS_GOTA = 7;

        // EL GLITCH DE TAJOS — ráfaga de 7 t cada 34 t
        private const float GLITCH_CADA = 34f;
        private const float GLITCH_DURA = 7f;

        // LA SEMILLA determinista de la grieta (glitch, chispas, iris)
        private int Semilla => Projectile.whoAmI * 31 + 7;

        private NPC Presa => Projectile.ai[0] <= 0 ? null : Main.npc[(int)Projectile.ai[0] - 1];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;   // la respiración del strip
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 44;
            Projectile.tileCollide = false;         // las grietas no conocen puertas
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;               // tres presas por grieta
            Projectile.timeLeft = 300;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            float edad = Projectile.ai[1]++;

            // === LA RESPIRACIÓN: el corazón late cada 5 t ===
            if (++Projectile.frameCounter >= TICKS_POR_FRAME)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) & 3;
            }

            // === LA CACERÍA (el .95) ===
            if (edad % TICKS_REBUSCA == 0f)
            {
                NPC presa = Presa;
                if (presa == null || !presa.active || presa.life <= 0)
                    presa = BuscarPresa();
                Projectile.ai[0] = presa != null ? presa.whoAmI + 1 : 0f;
            }

            NPC objetivo = Presa;
            if (objetivo != null && objetivo.active && objetivo.life > 0)
            {
                float vel = MathHelper.Clamp(Projectile.velocity.Length() + 0.08f,
                    VEL_INICIAL, VEL_MAXIMA);
                Vector2 hacia = (objetivo.Center - Projectile.Center)
                    .SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX));
                float deseado = hacia.ToRotation();
                float actual = Projectile.velocity.ToRotation();
                float giro = MathHelper.Clamp(MathHelper.WrapAngle(deseado - actual),
                    -GIRO_MAX, GIRO_MAX);
                Projectile.velocity = (actual + giro).ToRotationVector2() * vel;
            }
            else if (Projectile.velocity.Length() < VEL_INICIAL)
            {
                Projectile.velocity = Vector2.Normalize(Projectile.velocity) * VEL_INICIAL;
            }

            // LA ROTACIÓN: alineada al vuelo con el temblor de la tinta
            Projectile.rotation = Projectile.velocity.ToRotation()
                + MathF.Sin(edad * 0.31f + Projectile.whoAmI) * 0.07f;

            // === 2 — EL RIBBON: el track de la espina (se dibuja en PreDraw) ===
            if (Main.netMode != NetmodeID.Server)
                EstelaLib.Track(Projectile.whoAmI, 16).Push(Projectile.Center);

            if (Main.netMode != NetmodeID.Server)
            {
                // === 1 — EL NACIMIENTO: chispas de anomalía + el desgarro ===
                if (edad == 0f)
                {
                    RiftLib.ChispasAnomalia(Projectile.Center, 7, RiftPaletas.Vacio,
                        Semilla + 13, out ParticleData[] burst);
                    Spawn(burst);
                    Sonar(SoundID.Item122.WithPitchOffset(-0.42f).WithVolumeScale(0.5f),
                        Projectile.Center);
                }

                // === 10 — LAS ESTRELLAS FUGITIVAS: cada 18 t, 2 motas del
                //     otro lado que se escapan de la herida ===
                if (edad % 18f == 0f)
                {
                    RiftLib.ChispasAnomalia(Projectile.Center, 2, RiftPaletas.Vacio,
                        Semilla + (int)edad, out ParticleData[] fugitivas);
                    Spawn(fugitivas);
                }

                // === 4 — LA ASPIRACIÓN: el mundo ENTRA a la grieta ===
                // Motas que nacen en anillo (34-66 px) y CONVERGEN en
                // espiral hacia la boca — la grieta ASPIRA su entorno.
                if (Main.rand.NextBool(2))
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    float r = Main.rand.NextFloat(34f, 66f);
                    Vector2 haciaDentro = -ang.ToRotationVector2();
                    Vector2 tangencial = new Vector2(-haciaDentro.Y, haciaDentro.X) * 0.45f;
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + ang.ToRotationVector2() * r,
                        DustID.Shadowflame,
                        haciaDentro * Main.rand.NextFloat(2.2f, 3.4f) + tangencial,
                        128, default, Main.rand.NextFloat(0.45f, 0.7f));
                    d.noGravity = true;
                }
                // Las ORBITALES: motas que GIROSAN pegadas a la herida —
                // el mini disco de acreción de la tinta.
                if (edad % 7f == 0f)
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 orbital = new Vector2(-MathF.Sin(ang), MathF.Cos(ang)) * 1.7f;
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + ang.ToRotationVector2() * Main.rand.NextFloat(18f, 28f),
                        DustID.PurpleTorch, orbital, 128, default, 0.55f);
                    d.noGravity = true;
                }

                // === 11 — EL GOTEO: la tinta que gotea de la herida abierta ===
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * 0.5f,
                        DustID.Shadowflame,
                        -Projectile.velocity * 0.04f + new Vector2(
                            Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(-0.6f, 0.2f)),
                        128, default, 0.65f);
                    d.noGravity = true;
                }
                if (edad % TICKS_GOTA == 0f)
                {
                    Dust g = Dust.NewDustPerfect(
                        Projectile.Center + new Vector2(Main.rand.NextFloat(-3f, 3f), 8f),
                        DustID.PurpleTorch,
                        new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(0.3f, 0.9f)),
                        128, default, 0.5f);
                    g.noGravity = false;
                }

                // === 12 — LA LUZ PARPADEANTE: violeta + marfil con el
                //     flicker de una conexión rota (la realidad herida) ===
                float pulso = PulsoDelFrame();
                float latido = (0.55f + 0.25f * pulso)
                    * (0.80f + 0.20f * VFXCore.Hash01(Projectile.whoAmI, (int)(edad / 3f), 55));
                Lighting.AddLight(Projectile.Center, 0.30f * latido, 0.20f * latido, 0.48f * latido);
                Lighting.AddLight(Projectile.Center + Projectile.rotation.ToRotationVector2() * 10f,
                    0.45f * latido, 0.42f * latido, 0.34f * latido);
            }
        }

        /// <summary>EL LATIDO del frame (0.2..1.0 — el corazón manda en el
        /// frame 2, como el .95).</summary>
        private float PulsoDelFrame() => Projectile.frame == 2 ? 1f
            : Projectile.frame == 1 ? 0.55f
            : Projectile.frame == 3 ? 0.2f : 0.4f;

        /// <summary>LA PRESA: el enemigo válido más cercano en RADIO_BUSCA
        /// (los vecinos de las casas, jamás — esta grieta no come amigos).</summary>
        private NPC BuscarPresa()
        {
            NPC mejor = null;
            float mejorD = RADIO_BUSCA * RADIO_BUSCA;
            foreach (NPC n in Main.ActiveNPCs)
            {
                if (n == null || !n.active || n.life <= 0 || n.dontTakeDamage) continue;
                if (n.townNPC || n.friendly || NPCID.Sets.ActsLikeTownNPC[n.type]) continue;
                float d = Vector2.DistanceSquared(n.Center, Projectile.Center);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        /// <summary>14 — LA MORDIDA: donde mordió, salpicadura en anillo +
        /// chispas de anomalía de la herida fresca.</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 10; i++)
                {
                    float ang = (float)(i / 10.0 * Math.PI * 2.0);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                        ang.ToRotationVector2() * Main.rand.NextFloat(1.5f, 3.5f), 128,
                        default, 0.8f);
                    d.noGravity = true;
                }
                RiftLib.ChispasAnomalia(Projectile.Center, 4, RiftPaletas.Vacio,
                    Semilla + 77, out ParticleData[] mordida);
                Spawn(mordida);
            }
            Sonar(SoundID.NPCHit9.WithPitchOffset(-0.25f).WithVolumeScale(0.7f), Projectile.Center);
        }

        /// <summary>15 — EL COLAPSO: la grieta muere HACIA DENTRO.</summary>
        public override void OnKill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                // LA IMPLOSIÓN: 14 motas en anillo que CONVERGEN al punto —
                // la grieta se traga su última luz antes de romperse.
                for (int i = 0; i < 14; i++)
                {
                    float ang = (float)(i / 14.0 * Math.PI * 2.0) + Main.rand.NextFloat(-0.2f, 0.2f);
                    Vector2 haciaDentro = -ang.ToRotationVector2();
                    Dust d = Dust.NewDustPerfect(
                        Projectile.Center + ang.ToRotationVector2() * Main.rand.NextFloat(22f, 30f),
                        DustID.Shadowflame,
                        haciaDentro * Main.rand.NextFloat(2.4f, 3.4f), 128, default, 0.75f);
                    d.noGravity = true;
                }
                // EL ESTALLIDO: la salpicadura grande (el .95)
                for (int i = 0; i < 16; i++)
                {
                    float ang = (float)(i / 16.0 * Math.PI * 2.0);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                        ang.ToRotationVector2() * Main.rand.NextFloat(2f, 5f), 128,
                        default, 1.0f);
                    d.noGravity = Main.rand.NextBool(2);
                }
                RiftLib.ChispasAnomalia(Projectile.Center, 10, RiftPaletas.Vacio,
                    Semilla + 99, out ParticleData[] colapso);
                Spawn(colapso);
                // LA MANCHA + EL CORTE QUE SANA (el recuerdo del disparo)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center,
                    Vector2.Zero, ModContent.ProjectileType<ManchaDeTinta>(),
                    0, 0f, Projectile.owner, 0f, 0f);
            }
            Sonar(SoundID.Item122.WithPitchOffset(0.3f).WithVolumeScale(0.6f), Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            VFXCore.CerrarLoteSiAbierto();
            try { DrawTodo(lightColor); }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        private void DrawTodo(Color luz)
        {
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int alto = tex.Height / Main.projFrames[Projectile.type];
            Rectangle frame = new Rectangle(0, Projectile.frame * alto, tex.Width, alto);
            Vector2 origen = new Vector2(tex.Width * 0.5f, alto * 0.5f);
            float edad = Projectile.ai[1];
            float time = Main.GlobalTimeWrappedHourly;
            float pulso = PulsoDelFrame();

            // EL VAIVÉN: la grieta se mece PERPENDICULAR al vuelo — SOLO
            // en el draw (el hitbox viaja honesto)
            Vector2 rumbo = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 perp = new Vector2(-rumbo.Y, rumbo.X);
            Vector2 vaiven = perp * MathF.Sin(edad * 0.23f + Projectile.whoAmI * 1.7f) * 2.4f;

            // === 2 — EL RIBBON DEL VACÍO + 1 — LA ESTRELLA DE NACIMIENTO ===
            //     (lote aditivo propio: el rastro de NADA y la ruptura)
            Vector2[] camino = EstelaLib.Track(Projectile.whoAmI, 16).Points();
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                if (camino.Length >= 2)
                {
                    for (int i = 0; i < camino.Length; i++)
                        camino[i] -= Main.screenPosition;
                    EstelaLib.Ribbon(Main.spriteBatch, camino, 13f,
                        EstelaProfile.Comet, SombrasLib.Violeta, 0.42f,
                        Semilla + 9, time, head: false);
                }
                // LA ESTRELLA DE RUPTURA: crece (charge 0→1 en 9 t — con su
                // anillo implosionando) y se apaga en 22 t — la grieta NACE.
                if (edad < 22f)
                {
                    float charge = MathHelper.Clamp(edad / 9f, 0f, 1f);
                    float fade = 1f - edad / 22f;
                    RiftLib.Star(Main.spriteBatch,
                        Projectile.Center + vaiven - Main.screenPosition,
                        charge, RiftPaletas.Vacio, 0.85f * fade, Semilla, time);
                }
            }
            catch { }
            Main.spriteBatch.End();
            VFXCore.ReabrirLoteVanilla();

            // === 3 — LAS ESTELAS FANTASMA: 3 ecos de la página que se
            //     deshace detrás (lote alfa vanilla) ===
            for (int i = 3; i >= 1; i--)
            {
                if (i >= Projectile.oldPos.Length) continue;
                Vector2 vieja = Projectile.oldPos[i];
                if (vieja == Vector2.Zero) continue;
                float f = 1f - i / 4f;                          // 0.25..0.75
                Color fantasma = SombrasLib.Alfa(SombrasLib.Violeta, 0.30f * f);
                float rotVieja = i < Projectile.oldRot.Length
                    ? Projectile.oldRot[i] : Projectile.rotation;
                Main.spriteBatch.Draw(tex, vieja + Projectile.Size * 0.5f - Main.screenPosition,
                    frame, fantasma, rotVieja, origen, 0.92f * f, SpriteEffects.None, 0f);
            }

            Vector2 posMundo = Projectile.Center + vaiven;

            // === 7 — EL HALO DOBLE + 8 — EL IRIS + 5 — LA ABERRACIÓN
            //     CROMÁTICA (lote aditivo VFXCore, DEBAJO del sprite:
            //     los ecos R/C asoman por los bordes — aberración de verdad) ===
            VFXCore.Begin();
            float latido = 0.55f + 0.45f * MathF.Sin(edad * 0.11f);
            // EL HALO (el .95): violeta fuera, marfil dentro
            VFXCore.Quad(posMundo, SombrasLib.Alfa(SombrasLib.Violeta,
                (0.16f + 0.10f * pulso) * latido + 0.06f), new Vector2(72f, 72f) * (0.8f + 0.3f * pulso));
            VFXCore.Quad(posMundo, SombrasLib.Alfa(SombrasLib.Blanco,
                0.22f * pulso * latido), new Vector2(34f, 46f), Projectile.rotation);
            // EL IRIS: el anillo ELÍPTICO de apertura (el diafragma del
            // desgarro — alineado al vuelo, respirando ±12%)
            float respira = 1f + 0.12f * MathF.Sin(time * 2.6f + Semilla);
            Vector2 iris = VFXCore.RingQuadSize(1f) * new Vector2(46f, 30f) * respira;
            VFXCore.Quad(posMundo, SombrasLib.Alfa(SombrasLib.Violeta, 0.20f + 0.08f * pulso),
                iris, Projectile.rotation, VFXCore.Ring);
            VFXCore.Quad(posMundo, SombrasLib.Alfa(SombrasLib.Blanco, 0.10f * pulso),
                iris * 0.62f, Projectile.rotation + MathF.Sin(time * 1.7f) * 0.2f, VFXCore.Ring);
            // LA ABERRACIÓN CROMÁTICA: los DOS ECOS del sprite (rojo/cian)
            // con la separación LATIENDO con el corazón
            float sep = 1.2f + 1.0f * pulso;
            VFXCore.QuadSrc(posMundo + perp * sep, new Color(255, 40, 60, 105),
                new Vector2(tex.Width, alto), Projectile.rotation, tex, frame);
            VFXCore.QuadSrc(posMundo - perp * sep, new Color(60, 160, 255, 105),
                new Vector2(tex.Width, alto), Projectile.rotation, tex, frame);
            VFXCore.FlushAdditive();
            VFXCore.ReabrirLoteVanilla();

            // === EL SPRITE — el fragmento del usuario (lote alfa vanilla) ===
            bool glitch = edad % GLITCH_CADA < GLITCH_DURA;
            if (glitch)
            {
                // === 6 — EL GLITCH DE TAJOS: 3 bandas horizontales del frame
                //     desplazadas (EcoGlitch) — la realidad pierde frames ===
                RiftLib.EcoGlitch(Semilla, time, out Vector2[] offs, out _);
                const int bandas = 3;
                for (int b = 0; b < bandas; b++)
                {
                    int y0 = alto * b / bandas;
                    int y1 = alto * (b + 1) / bandas;
                    if (y1 > alto) y1 = alto;
                    if (y1 - y0 <= 0) continue;
                    var src = new Rectangle(0, Projectile.frame * alto + y0, tex.Width, y1 - y0);
                    var orgBanda = new Vector2(tex.Width * 0.5f, (y0 + y1) * 0.5f);
                    Vector2 rel = new Vector2(0f, (y0 + y1) * 0.5f - alto * 0.5f)
                        .RotatedBy(Projectile.rotation);
                    Main.spriteBatch.Draw(tex, posMundo - Main.screenPosition + rel + offs[b % 3],
                        src, luz, Projectile.rotation, orgBanda, 1f, SpriteEffects.None, 0f);
                }
            }
            else
            {
                Main.spriteBatch.Draw(tex, posMundo - Main.screenPosition, frame, luz,
                    Projectile.rotation, origen, 1f, SpriteEffects.None, 0f);
            }

            // === 9 — EL OJO RASGADO: el corazón de marfil ES un ojo — SOLO
            //     abre cuando hay presa, y MIRA a su presa (lote aditivo
            //     ENCIMA del sprite: el ojo brilla SOBRE la herida) ===
            NPC presa = Presa;
            if (presa != null && presa.active && presa.life > 0)
            {
                VFXCore.Begin();
                Vector2 eje = Projectile.rotation.ToRotationVector2();
                Vector2 posOjo = posMundo + eje * 6f;
                SombrasLib.Ojo(posOjo, 11f, presa.Center - posOjo,
                    0.30f + 0.70f * pulso, pupila: true, rasgada: true);
                VFXCore.FlushAdditive();
                VFXCore.ReabrirLoteVanilla();
            }
        }

        private static void Spawn(ParticleData[] motas)
        {
            if (motas == null) return;
            for (int i = 0; i < motas.Length; i++)
                ParticleManager.Spawn(motas[i]);
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }

    /// <summary>
    /// MANCHADETINTA — v6.50.95/.96 — LO QUE DEJA LA GRIETA AL CERRARSE.
    /// La salpicadura de tinta: un charco violeta que se desvanece donde la
    /// grieta colapsó — y v6.50.96 le añade EL CORTE QUE SANA: la línea de
    /// la herida final con aberración cromática (cian arriba, magenta
    /// abajo, marfil en el centro — la escuela UmbralRoto) que se CIERRA
    /// desde los extremos hacia el centro — «la realidad sana comiéndose
    /// el corte». Puro draw, sin daño, sin colisión, sin red.
    /// </summary>
    public class ManchaDeTinta : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private const float VIDA = 46f;          // el charco vive 46 t
        private const float DURA_CORTE = 26f;    // el corte sana en 26 t

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = (int)VIDA;
        }

        public override void AI()
        {
            // LAS GOTAS DEL FINAL: dos últimas gotas que caen al nacer
            if (Projectile.ai[1] == 0f && Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 3; i++)
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                        new Vector2(Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-2.5f, -0.5f)),
                        128, default, 0.55f);
                    d.noGravity = false;
                }
            }
            Projectile.ai[1]++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            float t = Projectile.ai[1];
            float desvanece = MathHelper.Clamp(1f - t / VIDA, 0f, 1f);
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                // EL CHARCO (el .95)
                SombrasLib.Charco(Projectile.Center, 26f + 14f * (1f - desvanece),
                    0.55f * desvanece, Main.GlobalTimeWrappedHourly, Projectile.whoAmI * 13 + 7);
                // la última luz de la tinta
                if (desvanece > 0.4f)
                {
                    VFXCore.Begin();
                    VFXCore.Quad(Projectile.Center, SombrasLib.Alfa(SombrasLib.Violeta,
                        0.14f * desvanece), new Vector2(40f, 40f));
                    VFXCore.FlushAdditive();
                }

                // === EL CORTE QUE SANA: la línea de la herida final con
                //     ABERRACIÓN CROMÁTICA que se acorta de extremo a
                //     extremo — la realidad cerrando la grieta ===
                if (t < DURA_CORTE)
                {
                    float cierre = 1f - t / DURA_CORTE;        // 1→0
                    float len = 46f * cierre;
                    Vector2 a = Projectile.Center - Vector2.UnitX * len * 0.5f;
                    Vector2 b = Projectile.Center + Vector2.UnitX * len * 0.5f;
                    Vector2 off = new Vector2(0f, 1.6f);
                    VFXCore.Begin();
                    VFXCore.Line(a + off, b + off,
                        SombrasLib.Alfa(new Color(0, 255, 255), 0.35f * cierre), 2.6f);
                    VFXCore.Line(a - off, b - off,
                        SombrasLib.Alfa(new Color(255, 0, 249), 0.35f * cierre), 2.6f);
                    VFXCore.Line(a, b,
                        SombrasLib.Alfa(SombrasLib.Blanco, 0.65f * cierre), 2.2f);
                    VFXCore.FlushAdditive();
                }
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
