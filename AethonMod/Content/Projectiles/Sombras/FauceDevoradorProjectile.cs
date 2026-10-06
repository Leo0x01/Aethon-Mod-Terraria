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
    /// v6.50.66 — LAS TRES HERMANAS: el usuario jubiló a las 3 armas
    /// viejas (la pinza del gif, la esfera del tajo y el libro de la
    /// página final) y pidió copiar a La Sombra de la Página con más
    /// personalidad. LOS ESTILOS MUERTOS (1-sprite, 2-esfera, 4-libro)
    /// se BORRARON con sus armas — el sprite del libro quedó VETADO por
    /// el usuario («no uses el sprite del libro»). Los estilos vivos:
    /// 3 = La Sombra de la Página (la base, INTACTA), 5 = LA MAREA
    /// (ola que sube + cresta de fauces), 6 = LA MIRADA (el ojo del
    /// juicio + viñeta), 7 = EL NIDO (jaula de garras + almas). Cada
    /// hermana añade su FIRMA sobre el festín común (DrawAdorno).
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
    ///          ALMAS vuelan al portador… y cada hermana añade su firma
    ///          (5: la ola que sube · 6: el ojo del juicio · 7: la jaula).
    ///   160-210 LA DISIPACIÓN — bruma negra y polvo: el jefe se
    ///          desintegra. A los 210 el motor mata de verdad (el loot
    ///          cae DENTRO de la bruma: el festín lo digiere todo).
    ///
    /// ai[0] = whoAmI del jefe · ai[1] = estilo (3/5/6/7) · ai[2] = tick.
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

            // === LOS SONIDOS DEL FESTÍN DE LAS TRES HERMANAS (v6.50.66 —
            // estilos 5/6/7: la MAREA, la MIRADA y el NIDO; suenan en SP
            // y en los clientes — el server no tiene oídos. El estilo 3
            // (La Sombra de la Página) queda EXACTAMENTE como era) ===
            byte estiloSonido = (byte)Projectile.ai[1];
            if (estiloSonido >= 5)
            {
                float tono = estiloSonido == 5 ? -0.50f : estiloSonido == 6 ? -0.28f : -0.62f;
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
            Vector2[] col = SombrasLib.Columna(raiz, destino, tiempo, semilla, 22, 0.20f);
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
                // FAUCES PROCEDURALES — mastica el ciclo del festín
                Vector2 rumbo = (jefe.Center - col[col.Length - 4]).SafeNormalize(Vector2.UnitX);
                float apertura = t < 60f
                    ? 0.9f * manifiesta
                    : CicloMasticar(t);
                SombrasLib.Fauces(destino, rumbo, apertura * vivo, 120f, semilla);

                // v6.50.64 — EL ALIENTO: las fauces del festín respiran bruma
                SombrasLib.BrumaBoca(destino, rumbo, apertura * vivo, 0.5f * vivo, tiempo, semilla + 5, 5);
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
        //  v6.50.66 — LOS ADORNOS DE LAS TRES HERMANAS (estilos 5/6/7):
        //  el festín de sombra (el tentáculo común de arriba) + LA FIRMA
        //  de cada arma nueva. El estilo 3 (La Sombra de la Página, la
        //  base que el usuario pidió dejar INTACTA) no lleva adorno.
        // ==================================================================

        /// <summary>
        /// LA FIRMA DE CADA HERMANA sobre el festín común de tentáculo:
        /// estilo 5 (LA MAREA) — la OLA QUE SUBE: un plano negro ancho
        /// que TREPA por el cuerpo del jefe con una CRESTA de cinco
        /// mini-fauces masticando a lo ancho, más la humareda de la
        /// resaca. Estilo 6 (LA MIRADA) — EL OJO DEL JUICIO: un ojo
        /// colosal se abre sobre el jefe (parpadea con cada mordida, la
        /// pupila ENGORDA con el festín) y la sala se hace borde.
        /// Estilo 7 (EL NIDO) — LA JAULA DE GARRAS: ocho zarpos se
        /// cierran sobre el jefe apretando al compás, con las almas de
        /// las crías orbitando la comida.
        /// </summary>
        private void DrawAdorno(NPC jefe, byte estilo)
        {
            if (estilo < 5) return;
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
                case 5: // LA MAREA — la ola que sube con cresta de fauces
                {
                    float sube = MathHelper.Clamp((t - 40f) / 90f, 0f, 0.94f);
                    if (sube > 0.02f)
                    {
                        Vector2 abajo = jefe.Center + new Vector2(0f, jefe.Size.Y * 0.55f);
                        VFXCore.Begin();
                        for (int capa = 0; capa < 3; capa++)
                        {
                            float y = abajo.Y - jefe.Size.Y * 1.1f * sube * (1f - capa * 0.18f);
                            float ancho = radio * (2.4f - capa * 0.5f) * (0.7f + 0.3f * sube);
                            VFXCore.Quad(new Vector2(abajo.X, y),
                                SombrasLib.Alfa(SombrasLib.Negro, (0.85f - capa * 0.18f) * vivo),
                                new Vector2(ancho, radio * (0.9f - capa * 0.2f)), 0f, VFXCore.GlowOrb);
                        }
                        VFXCore.FlushAlpha();

                        // LA CRESTA: cinco mini-fauces masticando a lo ancho
                        float abreCresta = CicloMasticar(t);
                        for (int k = 0; k < 5; k++)
                        {
                            float x = jefe.Center.X + (k - 2f) / 2f * radio * 1.5f;
                            Vector2 pos = new Vector2(x, abajo.Y - jefe.Size.Y * 1.1f * sube);
                            SombrasLib.Fauces(pos, -Vector2.UnitY, abreCresta * vivo, 42f, semilla + k * 5);
                        }
                    }
                    // la humareda de la resaca (más bruma en el festín)
                    if (festin > 0.1f)
                        SombrasLib.Bruma(jefe.Center + new Vector2(-radio * 0.8f, 0f),
                            radio * 0.7f, 0.5f * festin * vivo, tiempo, semilla + 23);
                    break;
                }
                case 6: // LA MIRADA — el ojo del juicio
                {
                    if (envuelve > 0.05f)
                    {
                        Vector2 pos = jefe.Center + new Vector2(0f, -radio * 0.95f);
                        float abre = SombrasLib.DeGolpe(envuelve) * (0.8f + 0.2f * MathF.Sin(tiempo * 2.4f));
                        // el parpadeo del juicio: se cierra con cada mordida
                        if (t > 60f)
                        {
                            float m = (t - 60f) % 32f;
                            if (m < 4f) abre *= 0.15f;
                        }
                        SombrasLib.Ojo(pos, radio * 0.5f, jefe.Center - pos, abre * vivo);

                        // la pupila del juicio ENGORDA conforme devora
                        if (festin > 0.1f)
                        {
                            Vector2 dirP = Vector2.Normalize(jefe.Center - pos) * radio * 0.09f;
                            VFXCore.Begin();
                            VFXCore.Quad(pos + dirP, SombrasLib.Alfa(SombrasLib.Rojo, 0.85f * vivo),
                                new Vector2(radio * (0.10f + 0.14f * festin),
                                            radio * (0.11f + 0.15f * festin)));
                            VFXCore.FlushAdditive();
                        }
                    }
                    // la sala se hace borde alrededor del juicio
                    SombrasLib.Vignette(0.22f * festin * vivo);
                    break;
                }
                case 7: // EL NIDO — la jaula de garras + las almas de las crías
                {
                    if (envuelve > 0.05f)
                    {
                        float apriete = 0.6f + 0.4f * MathF.Sin(t * 0.22f);
                        for (int k = 0; k < 8; k++)
                        {
                            float ang = k / 8f * MathHelper.TwoPi + tiempo * 0.2f;
                            Vector2 dir = new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.82f);
                            Vector2 pos = jefe.Center + dir * radio * 1.02f;
                            SombrasLib.Garra(pos, -dir,
                                radio * (0.6f + 0.25f * apriete) * vivo, 0.9f * vivo,
                                0.4f * (k % 2 == 0 ? 1f : -1f));
                        }
                        // las almas de las crías orbitando la comida
                        for (int k = 0; k < 4; k++)
                        {
                            float angA = tiempo * 2.2f + k * MathHelper.PiOver2;
                            Vector2 pos = jefe.Center + new Vector2(MathF.Cos(angA), MathF.Sin(angA) * 0.72f) * radio * 0.72f;
                            SombrasLib.Alma(pos, 10f, 0.75f * vivo);
                        }
                    }
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
