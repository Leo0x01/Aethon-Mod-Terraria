using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmetic
{
    /// <summary>
    /// MiniEstallidoPet — v6.50.55 — LA MINI-EXPLOSIÓN DE LA MASCOTA (la
    /// letra del usuario: «puede hacer la misma explosion de luz que el
    /// jefe original, solo que esta explocion es muy pequeña y solo
    /// ilumina, y hace muy poco daño a los enemigos, tambien cura un 1%
    /// de salud al jugador si golpea a un enemigo»).
    ///
    /// ES EL ESTALLIDO RADIANTE DEL JEFE (la .53) EN MINIATURA, a ~0.13×:
    /// LAS MISMAS SIETE PIEZAS — los RAYOS (14 de 34-130 px: línea núcleo
    /// blanca + halo oro→ámbar→brasa, cada rayo su vida propia), EL ANILLO
    /// SEGMENTADO (7 emisores 20→105 px con flicker), LA CRUZ ANAMÓRFICA,
    /// EL NÚCLEO que inunda (Bloom hasta ~130 px), LA ESTRELLA de 8 rayos
    /// (DestelloFinal), LA ONDA expansiva y LAS BOKEH que titilan — más
    /// LA LUZ DE MUNDO pequeña y el estampido chiquito (Item122 agudo y
    /// bajito + kick de 2 px).
    ///
    /// LA RECOGIDA (45 t): la regla Fargo de la casa — el telegrafo es
    /// TRANSLÚCIDO e inofensivo (EL LÍMITE: el aro fino de 135 px pulsando
    /// — la MISMA cifra de la hitbox —, el núcleo que se llena y LAS 6
    /// AGUJAS convergiendo). EL ESTALLIDO: daño 15 (MUY poco — es
    /// decorativo) en la hitbox HONESTA de radio 135 SOLO los primeros
    /// 8 t de onda, y LA GRATITUD: cada enemigo tocado cura el 1% de la
    /// vida MÁXIMA del dueño (tope 5% por estallido — la sanación de la
    /// casa: solo el cliente del dueño escribe su vida).
    ///
    /// TODO determinista: la semilla viaja en ai[0] y cada pantalla dibuja
    /// EL MISMO estallido (Hash01 — el patrón de la .53). Nace del
    /// AethonMenorPet (su IA la siembra solo en el cliente del dueño) y
    /// la semilla la lee también el dibujado del pet para ENGORDAR su
    /// brillo mientras carga (ver EdadPublica).
    /// </summary>
    public class MiniEstallidoPet : ModProjectile
    {
        // === LA PALETA DEL ESTALLIDO DEL JEFE (sus mismos colores) ===
        private static readonly Color AmbarEstelar = new(255, 178, 96);
        private static readonly Color EmberPortador = new(255, 120, 60);
        private static readonly Color OroGrimorio = new(245, 196, 81);
        private static readonly Color BlancoCaliente = new(255, 240, 190);

        /// <summary>La edad en ticks (determinista en TODAS las pantallas:
        /// velocidad 0 y cero azar en la IA — cada cliente cuenta igual).</summary>
        public float EdadPublica => _edad;

        private float _edad;    // el compás del mini-estallido
        private int _curas;     // el tope de gratitud (5 × 1%)

        public override string Texture => "AethonMod/Content/NPCs/AethonBoss";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;             // toca a los enemigos (muy poco)
            Projectile.hostile = false;
            Projectile.damage = 15;                 // MUY poco — es un adorno que golpea
            Projectile.knockBack = 1.5f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 165;              // 45 de recogida + 120 de espectáculo
            Projectile.tileCollide = false;         // la luz atraviesa el mundo
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;         // tML la sincroniza
            // cada enemigo UNA sola vez en toda la vida del destello
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            _edad++;

            if (_edad <= 45f)
            {
                // === LA RECOGIDA: la luz crece y las motas caen al centro ===
                float c = _edad / 45f;
                Lighting.AddLight(Projectile.Center,
                    0.40f + 0.75f * c, 0.34f + 0.64f * c, 0.22f + 0.42f * c);

                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    float r = 58f + Main.rand.NextFloat(44f);
                    Vector2 pos = Projectile.Center +
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * r;
                    Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame,
                        -pos.DirectionTo(Projectile.Center) * (r / 14f),
                        160, new Color(255, 244, 200), 0.65f);
                    d.noGravity = true;
                }
            }
            else
            {
                // === EL ESTALLIDO: la ventana de la onda y el espectáculo ===
                float tB = _edad - 45f;

                // el daño VIVE solo en la ventana de la onda (8 t) — después
                // el cuerpo es puro espectáculo (la regla de la .53).
                if (tB > 8f)
                    Projectile.friendly = false;

                // LA LUZ QUE INUNDA (pequeña — la mascota alumbra, no ciega).
                float auge = tB < 45f ? 1f : Math.Max(0f, 1f - (tB - 45f) / 70f);
                Lighting.AddLight(Projectile.Center,
                    1.25f * auge + 0.30f, 1.08f * auge + 0.26f, 0.70f * auge + 0.18f);

                // las chispas que vuelan (los primeros 20 t).
                if (!Main.dedServ && tB < 20f && Main.rand.NextBool(2))
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                        Main.rand.NextFloat(1.4f, 3.4f),
                        170, new Color(255, 244, 200), 0.85f);
                    d.noGravity = true;
                }

                // EL ESTAMPIDO CHIQUITO (UNA vez — el mismo del jefe, agudo
                // y bajito: es una miniatura, no un cañón) + su kick de 2 px.
                if (tB <= 1f && !Main.dedServ && Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item122.WithVolumeScale(0.35f).WithPitchOffset(0.42f),
                        Projectile.Center);
                    OndaLib.Kick(2f, 5);
                }
            }
        }

        /// <summary>
        /// LA HONESTIDAD DE LA CASA (la regla de la .53): golpea lo que SE
        /// VE que golpea — los rayos llegan a ~130 px y el aro telegrafiado
        /// marcó 135: la onda es la hitbox de 270×270, SOLO en su ventana.
        /// </summary>
        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            if (_edad > 45f && _edad <= 53f)
            {
                const int inflar = 250; // 20 → 270 px de onda (radio 135)
                hitbox.X -= inflar / 2;
                hitbox.Y -= inflar / 2;
                hitbox.Width += inflar;
                hitbox.Height += inflar;
            }
        }

        /// <summary>
        /// LA GRATITUD: si la luz toca a un enemigo, el dueño sana el 1% de
        /// su vida MÁXIMA (tope 5 curas — 5% — por estallido: es un adorno,
        /// no una fuente de vida). La sanación de la casa: solo el cliente
        /// del dueño escribe su vida (HealEffect + statLife, el patrón de
        /// WeaponScaling).
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.dedServ) return;
            if (_curas >= 5) return;
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead) return;
            if (duenio.whoAmI != Main.myPlayer) return;

            int cura = Math.Max(1, (int)(duenio.statLifeMax2 * 0.01f));
            _curas++;
            duenio.HealEffect(cura);
            duenio.statLife += cura;
            if (duenio.statLife > duenio.statLifeMax2)
                duenio.statLife = duenio.statLifeMax2;
        }

        // ==================================================================
        //  EL DIBUJADO — EL ESTALLIDO DEL JEFE EN MINIATURA. El contrato de
        //  la casa (la .50/.53): FASE 1 los quads de mundo (búfer aditivo),
        //  FlushAdditive, FASE 2 el lote de las librerías en pantalla, End,
        //  y el finally devuelve SIEMPRE el lote vanilla ABIERTO.
        // ==================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                int seed = (int)Projectile.ai[0];
                Vector2 pos = Projectile.Center - Main.screenPosition;

                // === FASE 1 — LOS QUADS (coords de MUNDO) ===
                if (_edad <= 45f)
                {
                    // LA RECOGIDA: el núcleo que se llena y LAS 6 AGUJAS
                    // convergiendo (la .53 a escala de bolsillo).
                    float c = _edad / 45f;
                    VFXCore.Quad(Projectile.Center, OroGrimorio * (0.20f + 0.25f * c),
                        new Vector2(30f + 34f * c, 30f + 34f * c));
                    for (int i = 0; i < 6; i++)
                    {
                        float angA = i * MathHelper.TwoPi / 6f + t * 0.35f;
                        float rA = MathHelper.Lerp(120f, 26f, c);
                        Vector2 punta = Projectile.Center +
                            new Vector2(MathF.Cos(angA), MathF.Sin(angA)) * rA;
                        VFXCore.Quad(punta, BlancoCaliente * (0.35f + 0.30f * c),
                            new Vector2(14f, 5f), angA);
                    }
                }
                else
                {
                    float tB = _edad - 45f;
                    // EL TEMPO del jefe: crece (12 t), ARDE (50) y se disuelve.
                    float crec = Math.Min(1f, tB / 12f);
                    float fade = tB < 50f ? 1f : Math.Max(0f, 1f - (tB - 50f) / 70f);
                    float giro = tB * 0.0045f;   // la rotación LENTA de la imagen

                    // === (a) LOS RAYOS (14 — el corazón de la imagen) ===
                    for (int i = 0; i < 14; i++)
                    {
                        float h0 = VFXCore.Hash01(seed, i, 10);
                        float h1 = VFXCore.Hash01(seed, i, 11);
                        float h2 = VFXCore.Hash01(seed, i, 12);
                        float h3 = VFXCore.Hash01(seed, i, 13);
                        float ang = giro + i * MathHelper.TwoPi / 14f +
                            (h0 - 0.5f) * (MathHelper.TwoPi / 14f) * 1.15f;
                        // el largo: de 34 (el rayo corto) a 130 px — la
                        // VARIANZA de la imagen, en miniatura honesta.
                        float largo = (34f + 96f * h1) * crec;
                        // la vida propia: cada rayo muere a SU tiempo.
                        float vidaR = 60f + 40f * h2;
                        float alfaR = fade * (tB < vidaR ? 1f :
                            Math.Max(0f, 1f - (tB - vidaR) / 20f));
                        if (alfaR <= 0.02f || largo < 4f) continue;
                        Vector2 dirR = new Vector2(MathF.Cos(ang), MathF.Sin(ang));
                        // LA LÍNEA NÚCLEO (blanca — la doble línea de la imagen).
                        VFXCore.Quad(Projectile.Center + dirR * (7f + largo * 0.5f),
                            BlancoCaliente * (0.85f * alfaR),
                            new Vector2(largo, 1.8f + 1.4f * h3), ang);
                        // EL HALO (más corto, más ancho — y CALIENTE:
                        // el degradé blanco→oro→ámbar→brasa del jefe).
                        Color cHalo = Color.Lerp(OroGrimorio,
                            Color.Lerp(AmbarEstelar, EmberPortador, h3 * 0.7f),
                            0.35f + 0.45f * h3);
                        VFXCore.Quad(Projectile.Center + dirR * (5f + largo * 0.34f),
                            cHalo * (0.40f * alfaR),
                            new Vector2(largo * 0.68f, 6.5f + 4.5f * h0), ang);
                    }

                    // === (b) EL ANILLO SEGMENTADO (7 EMISORES creciendo
                    //     hacia afuera con su flicker — 20 → 105 px) ===
                    float rAnillo = MathHelper.Lerp(20f, 105f,
                        Math.Min(1f, tB / 50f));
                    float alfaA = tB < 65f ? 1f :
                        Math.Max(0f, 1f - (tB - 65f) / 40f);
                    if (alfaA > 0.02f)
                    {
                        int beatE = (int)(tB / 6f);   // el flicker segmentado
                        for (int i = 0; i < 7; i++)
                        {
                            float angE = giro * 1.6f + i * MathHelper.TwoPi / 7f;
                            float hE = VFXCore.Hash01(seed, i, 60 + (beatE % 5));
                            Vector2 e = Projectile.Center +
                                new Vector2(MathF.Cos(angE), MathF.Sin(angE)) * rAnillo;
                            VFXCore.Quad(e,
                                BlancoCaliente * (0.80f * alfaA * (0.7f + 0.3f * hE)),
                                new Vector2(10f + 4f * hE, 10f + 4f * hE));
                            VFXCore.Quad(e, OroGrimorio * (0.45f * alfaA),
                                new Vector2(24f, 24f));
                        }
                    }

                    // === (c) LA CRUZ ANAMÓRFICA (el bloom estirado en H y V
                    //     — vive solo en el AUGE, muere a los 50 t) ===
                    float alfaC = Math.Max(0f, 1f - tB / 50f) * 0.55f;
                    if (alfaC > 0.02f)
                    {
                        VFXCore.Quad(Projectile.Center, BlancoCaliente * alfaC,
                            new Vector2(155f * crec, 3f));
                        VFXCore.Quad(Projectile.Center, BlancoCaliente * alfaC,
                            new Vector2(3f, 155f * crec));
                        VFXCore.Quad(Projectile.Center, OroGrimorio * (alfaC * 0.6f),
                            new Vector2(100f * crec, 7f));
                        VFXCore.Quad(Projectile.Center, OroGrimorio * (alfaC * 0.6f),
                            new Vector2(7f, 100f * crec));
                    }
                }

                VFXCore.FlushAdditive(null, false);

                // === FASE 2 — EL LOTE ADITIVO DE LAS LIBRERÍAS (PANTALLA) ===
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                if (_edad <= 45f)
                {
                    // LA RECOGIDA: EL LÍMITE (el aro fino de 135 px pulsando
                    // — la MISMA cifra de la hitbox — translúcido, la regla
                    // Fargo) y el núcleo que se llena.
                    float c = _edad / 45f;
                    OrbitaLib.AnilloFino(pos, 135f * (0.92f + 0.08f * MathF.Sin(t * 7f)),
                        0f, OrbitaLib.Tint(OroGrimorio, 0.35f + 0.20f * c));
                    LumenLib.Bloom(Main.spriteBatch, pos, 22f + 26f * c,
                        OroGrimorio, 0.35f + 0.35f * c, 3);
                }
                else
                {
                    float tB = _edad - 45f;
                    float crecS = Math.Min(1f, tB / 14f);
                    float fadeS = tB < 50f ? 1f : Math.Max(0f, 1f - (tB - 50f) / 70f);
                    float respira = 0.95f + 0.05f * MathF.Sin(t * 4.2f);

                    // EL NÚCLEO INUNDANDO (el blanco que crece hasta ~130 px
                    // RESPIRANDO — el corazón que se queda viendo).
                    LumenLib.Bloom(Main.spriteBatch, pos,
                        (26f + 104f * crecS) * respira, BlancoCaliente,
                        0.95f * fadeS, 4);
                    LumenLib.BloomPulse(Main.spriteBatch, pos,
                        22f + 26f * crecS, OroGrimorio, 0.55f * fadeS, t, 2.6f);

                    // LA ESTRELLA DE DESTELLO (el flare de 8 rayos girando
                    // LENTO — el lens-flare del cine, a escala de bolsillo).
                    Texture2D dest = VFXCore.DestelloFinal;
                    if (dest != null)
                    {
                        float esc = (210f * crecS) / dest.Width *
                            (0.92f + 0.08f * MathF.Sin(t * 3.4f));
                        Main.spriteBatch.Draw(dest, pos, null,
                            OrbitaLib.Tint(BlancoCaliente, 0.60f * fadeS),
                            t * 0.06f,
                            new Vector2(dest.Width, dest.Height) * 0.5f, esc,
                            SpriteEffects.None, 0f);
                    }

                    // LA ONDA EXPANSIVA (el frente que barre — una sola,
                    // clara: el golpe ya pasó, esto es la firma).
                    if (tB < 70f)
                        OndaLib.Pulse(Main.spriteBatch, pos, tB / 70f, 105f,
                            OroGrimorio, 0.50f * (1f - tB / 70f), seed);

                    // LAS BOKEH (los puntos de luz dispersos de la imagen:
                    // 8 chispas fijas que giran despacito y titilan).
                    for (int i = 0; i < 8; i++)
                    {
                        float h0 = VFXCore.Hash01(seed, i, 30);
                        float h1 = VFXCore.Hash01(seed, i, 31);
                        float tw = 0.35f + 0.65f *
                            (0.5f + 0.5f * MathF.Sin(t * 2.6f + i * 1.9f));
                        float rB = (12f + 88f * h1) * crecS;
                        float angB = h0 * MathHelper.TwoPi + t * 0.05f;
                        Vector2 b = pos + new Vector2(MathF.Cos(angB), MathF.Sin(angB)) * rB;
                        LumenLib.Bloom(Main.spriteBatch, b, 4f + 6f * h1,
                            (i % 2 == 0) ? OroGrimorio : AmbarEstelar,
                            0.45f * fadeS * tw, 2);
                    }
                }

                Main.spriteBatch.End();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            finally
            {
                // el contrato de la casa: salir SIEMPRE con el lote vanilla
                // ABIERTO (la lección de la .50).
                VFXCore.ReabrirLoteVanilla();
            }
            return false;
        }
    }
}
