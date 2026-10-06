using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Projectiles.Sombras
{
    /// <summary>
    /// FAUCEDEVORADORPROJECTILE — v6.50.62 — LA ANIMACIÓN DE MUERTE.
    ///
    /// «En el momento en que el jefe llega a 1 punto de vida, su
    /// animación original de muerte SE DETIENE y no avanza; en su lugar
    /// se activa la nueva: la sombra sale y LO DEVORA.»
    ///
    /// v6.50.68 — LOS CONCEPTOS NUEVOS: el usuario jubiló a las TRES
    /// HERMANAS («cambia los otros bastones por conceptos diferentes
    /// pero deja tal y como está La Sombra de la Página») — la MAREA
    /// (5), la MIRADA (6) y el NIDO (7) MUEREN con sus armas. Los
    /// estilos vivos ahora: 3 = La Sombra de la Página (la base,
    /// INTACTA) · 8 = LA PLUMA (la lluvia de tinta) · 9 = LA HOJA (el
    /// molino de filos) · 10 = EL SELLO (el sello del juicio). Además,
    /// LA BOCA YA NO SE DESPEGA: las fauces del festín se dibujan
    /// ANCLADAS A LA PUNTA REAL del tentáculo (la letra del usuario:
    /// «la boca y el tentáculo deben ser uno solo»).
    ///
    /// Lo spawnea FaucesGlobalNPC al interceptar la muerte (o al clavar
    /// la vida en 1 con el drain). El jefe queda POSADO (PreAI false —
    /// ni IA ni animación) mientras ESTE proyectil ejecuta el festín:
    ///
    ///   0-30   MANIFESTACIÓN — la sombra ERUPCIONA del portador
    ///          (el tentáculo GIGANTE se alza del charco).
    ///   30-60  ENVOLVER — el tentáculo SERPENTEA hasta el jefe y lo
    ///          enrosca.
    ///   60-160 EL FESTÍN — las fauces MASTICAN (3 mordidas sonoras),
    ///          la OSCURIDAD crece sobre el jefe hasta taparlo, las
    ///          ALMAS vuelan al portador… y cada arma añade su firma
    ///          (8: la lluvia de tinta · 9: el molino · 10: el sello).
    ///   160-210 LA DISIPACIÓN — bruma negra y polvo: el jefe se
    ///          desintegra. A los 210 el motor mata de verdad (el loot
    ///          cae DENTRO de la bruma: el festín lo digiere todo).
    ///
    /// ai[0] = whoAmI del jefe · ai[1] = estilo (3/8/9/10) · ai[2] = tick.
    /// </summary>
    public class FauceDevoradorProjectile : ModProjectile
    {
        private NPC Jefe => Projectile.ai[0] < 0 || Projectile.ai[0] >= Main.npc.Length
            ? null : Main.npc[(int)Projectile.ai[0]];

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 320;
            Projectile.netImportant = true;
        }

        /// <summary>El portador: el jugador más cercano al jefe (SP: el usuario).</summary>
        private Player Portador(NPC jefe)
        {
            Player mejor = Main.player[Projectile.owner];
            float d = float.MaxValue;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p == null || !p.active) continue;
                float dd = Vector2.DistanceSquared(p.Center, jefe.Center);
                if (dd < d) { d = dd; mejor = p; }
            }
            return mejor;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            NPC jefe = Jefe;

            // el jefe se esfumó (no debería: CheckActive lo clava) → fuera
            if (jefe == null || !jefe.active)
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = jefe.Center;

            if (Main.netMode == NetmodeID.Server) return;   // el server no dibuja

            // === LOS SONIDOS DEL FESTÍN DE LAS ARMAS NUEVAS (v6.50.68 —
            // estilos 8/9/10: la PLUMA, la HOJA y el SELLO; suenan en SP
            // y en los clientes — el server no tiene oídos. El estilo 3
            // (La Sombra de la Página) queda EXACTAMENTE como era) ===
            byte estiloSonido = (byte)Projectile.ai[1];
            if (estiloSonido >= 8)
            {
                float tono = estiloSonido == 8 ? -0.40f : estiloSonido == 9 ? -0.52f : -0.26f;
                if (t == 28) Sonar(SoundID.Item122.WithPitchOffset(tono).WithVolumeScale(0.8f), Projectile.Center);    // la sombra se alza
                if (t == 62) Sonar(SoundID.Item74.WithPitchOffset(tono).WithVolumeScale(0.75f), Projectile.Center);    // el abrazo cae
                if (t == 96 || t == 136) Sonar(SoundID.NPCHit9.WithPitchOffset(tono * 0.5f).WithVolumeScale(0.7f), Projectile.Center); // mastica
                if (t == 168) Sonar(SoundID.Item122.WithPitchOffset(0.2f).WithVolumeScale(0.7f), Projectile.Center);   // se disuelve
            }

            // === POLVO Y AMBIENTE (el cliente lo genera local, determinista basta) ===
            // durante el festín: motas de sombra cayendo del jefe
            if (t > 60 && t < 205 && Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(jefe.Center + new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)) * jefe.Size.Length() * 0.4f,
                    DustID.Shadowflame, new Vector2(0, 1.2f), 128, default, 0.9f);
                d.noGravity = false;
                d.fadeIn = 0.4f;
            }
            // la disipación final: POLVO de sombra que estalla
            if (t >= 160 && t < 205)
            {
                int n = 3;
                for (int k = 0; k < n; k++)
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    Dust d = Dust.NewDustPerfect(jefe.Center,
                        DustID.Smoke,
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Main.rand.NextFloat(2f, 7f),
                        100, new Color(12, 6, 10), 1.5f);
                    d.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            NPC jefe = Jefe;
            if (jefe == null || !jefe.active) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                byte estilo = (byte)Projectile.ai[1];
                DrawTentaculo(jefe);
                DrawAdorno(jefe, estilo);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        // ==================================================================
        //  EL TENTÁCULO GIGANTE (100% código — el estilo de sprite murió
        //  con su arma en la v6.50.66)
        // ==================================================================

        private void DrawTentaculo(NPC jefe)
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 31 + jefe.whoAmI;
            float t = Projectile.ai[2];
            Player portador = Portador(jefe);
            Vector2 raiz = portador.MountedCenter + new Vector2(0, portador.height * 0.42f);

            // fases del festín
            float manifiesta = SombrasLib.DeGolpe(MathHelper.Clamp(t / 30f, 0f, 1f));
            float envuelve = MathHelper.Clamp((t - 30f) / 30f, 0f, 1f);
            float festin = MathHelper.Clamp((t - 60f) / 100f, 0f, 1f);
            float disipa = MathHelper.Clamp((t - 160f) / 45f, 0f, 1f);
            float vivo = 1f - disipa;

            // === EL CHARCO DEL PORTADOR (más grande que nunca) ===
            SombrasLib.Charco(raiz, 64f * manifiesta, 0.85f * vivo, tiempo, semilla);

            // === EL CUERPO GIGANTE: raíz → jefe, en dos tramos ===
            // (a media caza la cabeza VIAJA; ya envuelto, MUERDE en el sitio)
            float alcance = manifiesta * 0.35f + envuelve * 0.65f;
            Vector2 destino = Vector2.Lerp(raiz + new Vector2(0, -160f), jefe.Center, alcance);
            // v6.50.67 — LA COLUMNA VIVA del festín: el tentáculo GIGANTE
            // también cobra inercia de látigo + el gancho del depredador
            Vector2[] col = SombrasLib.ColumnaViva(raiz, destino, tiempo, semilla, 22, 0.20f, gancho: 0.45f);
            SombrasLib.Masa(col, 58f, 20f, 0.96f * vivo, semilla, tiempo);

            // === LA BRUMA DEL CUERPO GIGANTE (v6.50.64): el tentáculo del
            // festín también EXHALA bruma negra a lo largo de todo el cuerpo ===
            SombrasLib.BrumaColumna(col, 54f, 0.45f * vivo, tiempo, semilla, 12);

            // === LOS OJOS: TODOS abiertos, TODOS mirando al jefe ===
            SombrasLib.OjosDeMasa(col, jefe.Center, (0.4f + 0.6f * envuelve) * vivo, semilla, tiempo, 7);

            // === LA OSCURIDAD SOBRE EL JEFE (crece hasta TAPARLO) ===
            float radio = MathHelper.Clamp(jefe.Size.Length() * 0.7f, 90f, 300f);
            float tapa = MathHelper.Clamp(envuelve * 0.5f + festin * 0.5f, 0f, 1f);
            if (tapa > 0.02f)
            {
                VFXCore.Begin();
                VFXCore.Quad(jefe.Center, SombrasLib.Alfa(SombrasLib.Negro, 0.92f * tapa * vivo),
                    new Vector2(radio * 2.15f * (0.55f + 0.45f * tapa), radio * 2.15f * (0.55f + 0.45f * tapa)), 0f, VFXCore.GlowOrb);
                VFXCore.FlushAlpha();
            }

            // === LA CABEZA-BOCA (100% código — la pinza de sprite murió
            // con su arma en la v6.50.66) ===
            {
                // v6.50.68 — ANCLADA A LA PUNTA REAL de la columna (la
                // letra del usuario: «la boca y el tentáculo deben ser
                // uno solo»): la boca VIAJA con la punta física del
                // látigo — jamás vuelve a despegarse del cuerpo
                Vector2 punta = col[col.Length - 1];
                Vector2 rumbo = (jefe.Center - punta).SafeNormalize(Vector2.UnitX);
                float apertura = t < 60f
                    ? 0.9f * manifiesta
                    : CicloMasticar(t);
                SombrasLib.Fauces(punta, rumbo, apertura * vivo, 120f, semilla);

                // v6.50.64 — EL ALIENTO: las fauces del festín respiran bruma
                SombrasLib.BrumaBoca(punta, rumbo, apertura * vivo, 0.5f * vivo, tiempo, semilla + 5, 5);
            }
            // === LAS ALMAS: la vida del jefe vuela al portador ===
            if (t > 55f)
            {
                VFXCore.Begin();
                int nAlmas = 6;
                for (int k = 0; k < nAlmas; k++)
                {
                    float prog = SombrasLib.Frac(t * 0.016f + k * (1f / nAlmas));
                    Vector2 a = jefe.Center + new Vector2(MathF.Cos(k * 2.7f + tiempo * 0.7f), MathF.Sin(k * 3.1f + tiempo)) * radio * 0.4f;
                    Vector2 b = portador.MountedCenter;
                    Vector2 ctrl = (a + b) * 0.5f + new Vector2(MathF.Sin(k * 2.3f) * 120f, -170f);
                    float u = 1f - prog;
                    Vector2 pos = u * u * a + 2f * u * prog * ctrl + prog * prog * b;
                    SombrasLib.Ojo(pos, 7f, b - pos, 1f);        // el alma ES un ojo que mira a casa
                }
                VFXCore.FlushAdditive();
            }

            // === LA DISIPACIÓN: bruma y polvo final ===
            if (disipa > 0.02f)
            {
                SombrasLib.Bruma(jefe.Center, radio * 1.4f, 0.9f * disipa, tiempo, semilla,
                    new Vector2(MathF.Sin(tiempo * 0.7f) * 30f, -18f * disipa));
            }
        }

        // ==================================================================
        // ==================================================================
        //  v6.50.68 — LOS ADORNOS DE LAS ARMAS NUEVAS (estilos 8/9/10):
        //  el festín de sombra (el tentáculo común de arriba) + LA FIRMA
        //  de cada arma nueva. El estilo 3 (La Sombra de la Página, la
        //  base que el usuario pidió dejar INTACTA) no lleva adorno.
        // ==================================================================

        /// <summary>
        /// LA FIRMA DE CADA ARMA NUEVA sobre el festín común de
        /// tentáculo: estilo 8 (LA PLUMA) — LA LLUVIA DE TINTA: agujas
        /// de hueso caen del cielo sobre el jefe en diagonal, clavándose
        /// escalonadas con destellos violetas y gotas de tinta (la pluma
        /// que ESCRIBE el final). Estilo 9 (LA HOJA) — EL MOLINO DE
        /// FILOS: dos anillos de cuatro crescentes contrarrotantes que
        /// giran alrededor del jefe y SE CIÑEN conforme avanza el
        /// festín. Estilo 10 (EL SELLO) — EL SELLO DEL JUICIO: el círculo
        /// rúnico doble se dibuja alrededor del jefe, gira en sentidos
        /// contrarios, SE CIÑE y exhala bruma del suelo.
        /// </summary>
        private void DrawAdorno(NPC jefe, byte estilo)
        {
            if (estilo < 8) return;
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 31 + jefe.whoAmI;
            float t = Projectile.ai[2];

            float envuelve = MathHelper.Clamp((t - 30f) / 30f, 0f, 1f);
            float festin = MathHelper.Clamp((t - 60f) / 100f, 0f, 1f);
            float disipa = MathHelper.Clamp((t - 160f) / 45f, 0f, 1f);
            float vivo = 1f - disipa;
            float radio = MathHelper.Clamp(jefe.Size.Length() * 0.7f, 90f, 300f);

            switch (estilo)
            {
                case 8: // LA PLUMA — la lluvia de tinta
                {
                    if (envuelve <= 0.05f) break;
                    VFXCore.Begin();
                    int nPlumas = 7;
                    for (int k = 0; k < nPlumas; k++)
                    {
                        // cada aguja cae en su momento (reparto áureo) y a
                        // su x sobre el cuerpo del jefe — todo determinista
                        float f0 = SombrasLib.Frac(SombrasLib.semille(semilla + k * 3) * 0.77f + k * 0.618034f);
                        float caida = (t - 46f - f0 * 70f) / 26f;
                        if (caida <= 0f) continue;

                        float x = jefe.Center.X + (f0 - 0.5f) * radio * 1.8f;
                        float yTope = jefe.Center.Y - jefe.Size.Y * 0.75f - 100f;
                        float yPiso = jefe.Center.Y + (SombrasLib.Frac(f0 * 9.3f) - 0.5f) * jefe.Size.Y * 0.7f;

                        if (caida < 1f)
                        {
                            // CAYENDO — la aguja de hueso + su estela violeta
                            float y = MathHelper.Lerp(yTope, yPiso, caida);
                            VFXCore.Quad(new Vector2(x, y), SombrasLib.Alfa(SombrasLib.Blanco, 0.95f * vivo),
                                new Vector2(9f, 44f), MathHelper.Pi, VFXCore.Colmillo);
                            VFXCore.Line(new Vector2(x, y - 26f), new Vector2(x, y - 78f),
                                SombrasLib.Alfa(SombrasLib.Violeta, 0.40f * vivo * (1f - caida)), 5f);
                        }
                        else
                        {
                            // CLAVADA — la aguja se queda hundida, brilla y
                            // escurre tinta (gota que baja — los dos quads
                            // del Alma, INLINE: aquí dentro no se puede
                            // llamar a SombrasLib.Alma, abre su propio lote)
                            float ancla = MathHelper.Clamp((caida - 1f) / 2.2f, 0f, 1f);
                            VFXCore.Quad(new Vector2(x, yPiso - 10f), SombrasLib.Alfa(SombrasLib.Blanco, 0.75f * (1f - ancla * 0.7f) * vivo),
                                new Vector2(8f, 34f), MathHelper.Pi, VFXCore.Colmillo);
                            float gotea = SombrasLib.Frac(tiempo * 0.9f + f0 * 5f);
                            Vector2 gota = new Vector2(x, yPiso + 6f + gotea * 34f);
                            VFXCore.Quad(gota, SombrasLib.Alfa(SombrasLib.Blanco, 0.38f * (1f - gotea) * vivo), new Vector2(7f, 7f));
                            VFXCore.Quad(gota, SombrasLib.Alfa(SombrasLib.Blanco, 0.50f * (1f - gotea) * vivo), new Vector2(3f, 3f));
                            if (caida < 1.35f)
                                VFXCore.Quad(new Vector2(x, yPiso), SombrasLib.Alfa(SombrasLib.Violeta, 0.55f * vivo * (1f - (caida - 1f) / 0.35f)),
                                    VFXCore.RingQuadSize(26f + 20f * (caida - 1f) / 0.35f), 0f, VFXCore.Ring);
                        }
                    }
                    VFXCore.FlushAdditive(VFXCore.Pixel);
                    break;
                }
                case 9: // LA HOJA — el molino de filos
                {
                    if (envuelve <= 0.05f) break;
                    // el molino SE CIÑE conforme devora (el círculo se cierra)
                    float ciñe = 1.22f - 0.55f * festin;
                    for (int k = 0; k < 8; k++)
                    {
                        bool ext = k < 4;
                        float velAng = ext ? 0.55f : -0.40f;             // anillos contrarrotantes
                        float ang = k * MathHelper.PiOver2 + tiempo * velAng + (ext ? 0f : MathHelper.PiOver4);
                        float r = radio * ciñe * (ext ? 1.02f : 0.68f);
                        Vector2 pos = jefe.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.85f) * r;
                        // la hoja corta TANGENCIAL al girar (el filo arrastra)
                        Vector2 tang = new Vector2(-MathF.Sin(ang), MathF.Cos(ang) * 0.85f) * (ext ? 1f : -1f);
                        SombrasLib.Garra(pos - tang * 24f, tang,
                            radio * (ext ? 0.52f : 0.40f), 0.92f * vivo,
                            0.5f * (k % 2 == 0 ? 1f : -1f));
                    }
                    break;
                }
                case 10: // EL SELLO — el sello del juicio
                {
                    if (envuelve <= 0.02f) break;
                    float ciñe = 1f - 0.35f * festin;                    // el sello SE CIÑE sobre el reo
                    float r = radio * ciñe;
                    float abre = SombrasLib.DeGolpe(envuelve) * vivo;

                    // LOS ANILLOS DOBLE — giran en sentidos contrarios
                    VFXCore.Begin();
                    VFXCore.Quad(jefe.Center, SombrasLib.Alfa(SombrasLib.Violeta, 0.50f * abre),
                        VFXCore.RingQuadSize(r * 1.16f), tiempo * 0.5f, VFXCore.Ring);
                    VFXCore.Quad(jefe.Center, SombrasLib.Alfa(SombrasLib.Violeta, 0.34f * abre),
                        VFXCore.RingQuadSize(r * 0.78f), -tiempo * 0.7f, VFXCore.Ring);
                    VFXCore.FlushAdditive();

                    // LAS MARCAS del juicio — ocho rayitas radiales parpadeando
                    VFXCore.Begin();
                    for (int k = 0; k < 8; k++)
                    {
                        float ang = k * MathHelper.PiOver4 + tiempo * 0.35f;
                        Vector2 dir = new Vector2(MathF.Cos(ang), MathF.Sin(ang));
                        float parp = 0.55f + 0.45f * MathF.Sin(tiempo * 3.5f + k * 2.1f);
                        VFXCore.Line(jefe.Center + dir * r * 0.86f, jefe.Center + dir * r * 1.04f,
                            SombrasLib.Alfa(SombrasLib.Blanco, 0.60f * abre * parp), 4f);
                    }
                    VFXCore.FlushAdditive(VFXCore.Pixel);

                    // LA BRUMA DEL CÍRCULO — el sello exhalando del suelo
                    if (festin > 0.05f)
                        SombrasLib.Bruma(jefe.Center + new Vector2(0f, jefe.Size.Y * 0.4f),
                            radio * 0.85f, 0.42f * festin * vivo, tiempo, semilla + 19,
                            new Vector2(0f, -22f * disipa));
                    break;
                }
            }
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }

        /// <summary>El masticar del festín: abre… ¡CIERRA de golpe! …mastica… abre.</summary>
        private static float CicloMasticar(float t)
        {
            float m = (t - 60f) % 32f;
            if (m < 10f) return MathHelper.Lerp(0.2f, 1f, m / 10f);                          // abre
            if (m < 16f) return MathHelper.Lerp(1f, 0.03f, SombrasLib.DeGolpe((m - 10f) / 6f)); // ¡CIERRA!
            if (m < 26f) return 0.15f + 0.1f * MathF.Sin(m * 1.4f);                          // mastica
            return MathHelper.Lerp(0.15f, 0.85f, (m - 26f) / 6f);                            // reabre
        }
    }
}
