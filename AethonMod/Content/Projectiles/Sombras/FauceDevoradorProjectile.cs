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
    /// FAUCEDEVORADORPROJECTILE — v6.50.69 — EL ACTO DE DEVORACIÓN, VERSIÓN
    /// BRUMA (la letra del usuario: «en el acto de devoración solo está
    /// la versión anterior de La Sombra de la Página… esta versión con
    /// la bruma es mucho mejor, así que debes cambiar la boca por la
    /// forma actual y toda esa bruma que se traga al jefe debe animarse
    /// para que sustituya cualquier animación»).
    ///
    /// LA BOCA MURIÓ: el festín ya NO dibuja las Fauces procedurales (la
    /// «versión anterior») — la punta es LA CABEZA DE BRUMA (la forma
    /// actual de La Sombra) y la cobertura es EL DEVORADOR v2: LA
    /// ELIPSE REAL DEL JEFE (su hitbox + margen — el Rey Slime queda
    /// ENTIERO dentro) con la REJILLA de puffs que la rellena.
    ///
    /// LA BRUMA ES LA ANIMACIÓN (sustituye la muerte del jefe):
    ///   0-30   MANIFESTACIÓN — el tentáculo GIGANTE sale DEL JUGADOR.
    ///   30-60  ENVOLVER — la punta llega y la cobertura crece hasta
    ///          ser TOTAL (el sprite del jefe se retira en la fase 2:
    ///          FaucesGlobalNPC.PreDraw).
    ///   60-150 EL FESTÍN — muerde con pulso (4,6 Hz), las ALMAS vuelan
    ///          al portador por el cuerpo y cada arma añade su firma.
    ///   150-210 EL TRAGO — LA INHALA: la elipse entera ENCOGE hacia la
    ///          punta, los puffs MIGRAN al embudo muriendo en volutas y
    ///          la corriente de bruma vuelve POR el tentáculo al
    ///          portador — el jefe entra AL CUERPO de la sombra.
    ///   210    LA MUERTE REAL (2ª pasada de CheckDead): loot, logros y
    ///          flags — y la MUERTE PREDETERMINADA NO SE VE: la PURGA
    ///          desactiva el gore y el polvo vanilla en la zona mientras
    ///          la bruma aún la ocupa («quedando solo su loot»).
    ///   +60    EL POSO — las últimas volutas sobre el botín y el
    ///          tentáculo regresa al portador.
    ///
    /// ai[0] = whoAmI del jefe · ai[1] = estilo (3/11/12/13) · ai[2] = tick.
    /// localAI[0] = whoAmI del portador (capturado en vida del jefe).
    /// </summary>
    public class FauceDevoradorProjectile : ModProjectile
    {
        /// <summary>El tick de la muerte real (= FaucesGlobalNPC.LargoFestin).</summary>
        private const float T_MUERTE = 210f;

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

        /// <summary>El portador: el jugador más cercano al jefe (capturado en vida).</summary>
        private Player Portador(NPC jefe)
        {
            if (Projectile.localAI[0] >= 0f && Projectile.localAI[0] < Main.maxPlayers)
            {
                Player p = Main.player[(int)Projectile.localAI[0]];
                if (p != null && p.active) return p;
            }
            Player mejor = Main.player[Projectile.owner];
            if (jefe != null && jefe.active)
            {
                float d = float.MaxValue;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p == null || !p.active) continue;
                    float dd = Vector2.DistanceSquared(p.Center, jefe.Center);
                    if (dd < d) { d = dd; mejor = p; Projectile.localAI[0] = i; }
                }
            }
            return mejor;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            NPC jefe = Jefe;
            bool vivo = jefe != null && jefe.active;

            // === EL POSO (el jefe ya murió de verdad): nada de Kill por
            // aquí — la bruma de la entrega sigue viva un rato sobre el
            // loot y PURGA la muerte vanilla de la zona ===
            if (!vivo)
            {
                if (Projectile.timeLeft > 70) Projectile.timeLeft = 70;
                if (t >= 268) { Projectile.Kill(); return; }

                // LA PURGA también desde la AI (SP / cliente dueño — en los
                // demás clientes la hace el PreDraw, que corre en todos)
                if (t < 262) SombrasLib.PurgaVisualMuerte(Projectile.Center, 380f);

                // las últimas volutas sobre el botín (finas, muriendo)
                if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(4))
                {
                    float f = MathHelper.Clamp((t - T_MUERTE) / 70f, 0f, 1f);
                    Dust d = Dust.NewDustPerfect(Projectile.Center + new Vector2(Main.rand.NextFloat(-70f, 70f), Main.rand.NextFloat(-50f, 30f)),
                        DustID.Shadowflame, new Vector2(0f, -0.6f), 128, default, 0.6f * (1f - f));
                    d.noGravity = true;
                }
                return;
            }

            Projectile.Center = jefe.Center;

            if (Main.netMode == NetmodeID.Server) return;   // el server no dibuja

            // === LOS SONIDOS DEL FESTÍN (los estilos 11/12/13 traen los
            // suyos; el 3 — La Sombra de la Página — queda como siempre:
            // rugido + los tres golpes del NPC) ===
            byte estiloSonido = (byte)Projectile.ai[1];
            if (estiloSonido >= 11)
            {
                float tono = estiloSonido switch
                {
                    11 => -0.46f,   // LA MANO: el puño que sepulta
                    12 => -0.16f,   // LAS TIJERAS: el filo que arranca
                    _ => -0.34f,    // LA PÁGINA: el papel que se rasga
                };
                if (t == 28) Sonar(SoundID.Item122.WithPitchOffset(tono).WithVolumeScale(0.8f), Projectile.Center);    // la sombra se alza
                if (t == 62) Sonar(SoundID.Item74.WithPitchOffset(tono).WithVolumeScale(0.75f), Projectile.Center);    // el abrazo cae
                if (t == 96 || t == 136) Sonar(SoundID.NPCHit9.WithPitchOffset(tono * 0.5f).WithVolumeScale(0.7f), Projectile.Center); // mastica
                if (t == 168) Sonar(SoundID.Item122.WithPitchOffset(0.2f).WithVolumeScale(0.7f), Projectile.Center);   // se disuelve
            }
            // EL TRAGO FINAL — el golpe que se lo lleva (todos los estilos)
            if (t == 203) Sonar(SoundID.Item122.WithPitchOffset(0.34f).WithVolumeScale(0.6f), Projectile.Center);

            // === POLVO Y AMBIENTE (determinista basta) ===
            if (t > 60 && t < 150 && Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(jefe.Center + new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)) * jefe.Size.Length() * 0.4f,
                    DustID.Shadowflame, new Vector2(0, 1.2f), 128, default, 0.9f);
                d.noGravity = false;
                d.fadeIn = 0.4f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            NPC jefe = Jefe;
            bool vivo = jefe != null && jefe.active;

            // LA PURGA EN TODOS LOS CLIENTES (la AI solo corre en el dueño
            // y el server; el PreDraw corre DONDE SE VE): mientras la bruma
            // del poso ocupa la zona, la muerte vanilla NO SE VE — ni gore
            // ni polvo: «quedando solo su loot»
            if (!vivo && Projectile.ai[2] < 262)
                SombrasLib.PurgaVisualMuerte(Projectile.Center, 380f);
            if (jefe == null) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                byte estilo = (byte)Projectile.ai[1];
                DrawTentaculo(jefe, vivo);
                if (vivo) DrawAdorno(jefe, estilo);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        // ==================================================================
        //  EL TENTÁCULO GIGANTE — LA FORMA ACTUAL DE LA SOMBRA (la cabeza
        //  es bruma, la cobertura es EL DEVORADOR v2 de elipse completa)
        // ==================================================================

        private void DrawTentaculo(NPC jefe, bool vivo)
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 31 + jefe.whoAmI;
            float t = Projectile.ai[2];
            Player portador = Portador(vivo ? jefe : null);
            Vector2 raiz = portador.MountedCenter;      // v6.50.68: SALE DEL JUGADOR

            // fases del festín
            float manifiesta = SombrasLib.DeGolpe(MathHelper.Clamp(t / 30f, 0f, 1f));
            float envuelve = MathHelper.Clamp((t - 30f) / 30f, 0f, 1f);
            float festin = MathHelper.Clamp((t - 60f) / 90f, 0f, 1f);
            float inhala = MathHelper.Clamp((t - 150f) / 55f, 0f, 1f);   // EL TRAGO
            float poso = vivo ? 0f : MathHelper.Clamp((t - T_MUERTE) / 60f, 0f, 1f);
            float vivoF = vivo ? 1f - 0.75f * poso : 0.25f * (1f - poso);

            // === EL CHARCO DEL PORTADOR (más grande que nunca) ===
            SombrasLib.Charco(raiz + new Vector2(0f, portador.height * 0.42f), 64f * manifiesta, 0.85f * vivoF, tiempo, semilla);

            // === EL CUERPO GIGANTE: raíz → jefe — y durante EL TRAGO la
            // punta VUELVE al portador (el tentáculo se lleva la nube) ===
            float alcance = manifiesta * 0.35f + envuelve * 0.65f;
            Vector2 destino = Vector2.Lerp(raiz + new Vector2(0, -160f), jefe.Center, alcance);
            destino = Vector2.Lerp(destino, raiz + new Vector2(0f, -50f), inhala * 0.85f);
            Vector2[] col = SombrasLib.ColumnaViva(raiz, destino, tiempo, semilla, 22, 0.20f, gancho: 0.45f);
            // v6.50.71 — EL PERFIL FUSIFORME (la letra del usuario): fina en
            // el jugador, GORDA al centro, fina en la punta — el tentáculo
            // GIGANTE del festín luce igual que el arma que lo invocó
            SombrasLib.Masa(col, 14f, 22f, 0.96f * vivoF, semilla, tiempo, grosorCentro: 62f);

            // === LA BRUMA DEL CUERPO GIGANTE — v6.50.69: DOS CAPAS (más
            // bruma en todo — la letra del usuario para la familia entera)
            SombrasLib.BrumaColumna(col, 54f, 0.55f * vivoF, tiempo, semilla, 16);
            SombrasLib.BrumaColumna(col, 36f, 0.42f * vivoF, tiempo, semilla + 53, 12);

            // === LOS OJOS: TODOS abiertos, TODOS mirando al jefe ===
            SombrasLib.OjosDeMasa(col, jefe.Center, (0.4f + 0.6f * envuelve) * vivoF, semilla, tiempo, 7);

            // === LA CABEZA ES BRUMA (la boca procedural JUBILADA — la
            // letra: «debes cambiar la boca por la forma actual») ===
            Vector2 punta = col[col.Length - 1];
            Vector2 rumbo = vivo
                ? (jefe.Center - punta).SafeNormalize(Vector2.UnitX)
                : (raiz - punta).SafeNormalize(-Vector2.UnitY);
            float muerde = vivo ? CicloMasticar(t) : 0.4f;
            float vigor = MathHelper.Clamp(0.55f + 0.45f * muerde + 0.25f * inhala, 0f, 1f) * vivoF;
            SombrasLib.CabezaDeBruma(punta, rumbo, vigor, vivoF, tiempo, semilla);

            // === EL DEVORADOR v2 — LA CUBIERTA TOTAL (la elipse REAL del
            // jefe) + EL TRAGO animado (la bruma que sustituye cualquier
            // animación): crece al envolver, muerde con pulso y SE INHALA
            // entera hacia la punta durante el trago ===
            if (vivo)
            {
                float intensidad = MathHelper.Clamp(envuelve * 0.5f + festin * 0.5f, 0f, 1f);
                SombrasLib.Devorador(punta, jefe, intensidad, vivoF, tiempo, semilla, col, trago: inhala);

                // LA HERIDA: el punto donde la punta ENTERRÓ brilla rojo
                // DENTRO de la bruma (la digestión)
                VFXCore.Begin();
                VFXCore.Quad(punta, SombrasLib.Alfa(SombrasLib.Rojo, (0.42f + 0.20f * inhala) * vivoF),
                    new Vector2(120f, 120f), rumbo.ToRotation());
                VFXCore.FlushAdditive();
            }

            // === LA CORRIENTE DEL TRAGO — la bruma vuelve por el CUERPO:
            // puffs que nacen a lo largo de la columna y corren hacia la
            // raíz (la vida del jefe VIAJA al portador por dentro) ===
            if (inhala > 0.05f || !vivo)
            {
                float flujo = MathHelper.Max(inhala, vivo ? 0f : 1f - poso);
                for (int k = 0; k < 5; k++)
                {
                    float prog = SombrasLib.Frac(tiempo * 0.8f + k * 0.2f);
                    float idxF = (1f - prog) * (col.Length - 1);
                    int idx = Math.Min((int)idxF, col.Length - 2);
                    if (idx < 0) idx = 0;
                    Vector2 pos = Vector2.Lerp(col[idx], col[idx + 1], SombrasLib.Frac(idxF))
                        + new Vector2(MathF.Sin(k * 2.1f + tiempo * 3f) * 16f, -10f);
                    SombrasLib.Bruma(pos, 30f + 16f * SombrasLib.Frac(k * 0.73f),
                        0.55f * flujo * vivoF, tiempo, semilla + k * 7,
                        new Vector2(0f, -30f * flujo));
                }
            }

            // === LAS ALMAS: la vida del jefe vuela al portador (durante el
            // trago son UN RÍO — ocho, aceleradas) ===
            if (t > 55f)
            {
                VFXCore.Begin();
                int nAlmas = vivo ? 6 + (int)(3f * inhala) : 4;
                float radio = MathHelper.Clamp(jefe.Size.Length() * 0.7f, 90f, 300f);
                for (int k = 0; k < nAlmas; k++)
                {
                    float prog = SombrasLib.Frac(t * (0.016f + 0.014f * inhala) + k * (1f / nAlmas));
                    Vector2 a = jefe.Center + new Vector2(MathF.Cos(k * 2.7f + tiempo * 0.7f), MathF.Sin(k * 3.1f + tiempo)) * radio * 0.4f;
                    Vector2 b = portador.MountedCenter;
                    Vector2 ctrl = (a + b) * 0.5f + new Vector2(MathF.Sin(k * 2.3f) * 120f, -170f);
                    float u = 1f - prog;
                    Vector2 pos = u * u * a + 2f * u * prog * ctrl + prog * prog * b;
                    SombrasLib.Ojo(pos, 7f, b - pos, 1f);        // el alma ES un ojo que mira a casa
                }
                VFXCore.FlushAdditive();
            }

            // === EL POSO — la entrega: el último velo sobre el loot y el
            // pulso final de la onda (el libro se limpia la boca) ===
            if (!vivo)
            {
                float fade = 1f - poso;
                SombrasLib.Bruma(Projectile.Center, 130f * (0.6f + 0.4f * fade), 0.7f * fade, tiempo, semilla,
                    new Vector2(MathF.Sin(tiempo * 0.7f) * 24f, -14f));
                if (t < T_MUERTE + 10f)
                    SombrasLib.OndaChoque(Projectile.Center, 60f + 220f * (t - T_MUERTE + 10f) / 10f, 0.5f * fade);
            }
            else if (t > 160f)
            {
                // LA DISIPACIÓN que ya empezó: bruma y polvo final
                float disipa = MathHelper.Clamp((t - 160f) / 45f, 0f, 1f);
                float radio = MathHelper.Clamp(jefe.Size.Length() * 0.7f, 90f, 300f);
                SombrasLib.Bruma(jefe.Center, radio * 1.2f, 0.9f * disipa * (1f - inhala * 0.7f), tiempo, semilla,
                    new Vector2(MathF.Sin(tiempo * 0.7f) * 30f, -18f * disipa));
            }
        }

        // ==================================================================
        // ==================================================================
        //  v6.50.71 — LAS FIRMAS DE LAS ARMAS NUEVAS (estilos 11/12/13 —
        //  LOS ÚTILES DEL ESCRIBA: LA MANO, LAS TIJERAS y LA PÁGINA
        //  ARRANCADA) sobre el festín común (tentáculo gigante + DEVORADOR
        //  de cobertura total). El estilo 3 (La Sombra de la Página) no
        //  lleva adorno: es la base.
        // ==================================================================

        /// <summary>
        /// Estilo 11 (LA MANO DEL ESCRIBA) — EL PUÑO DEL ESCRIBA: la garra
        /// COLOSAL baja del techo (el brazo vive fuera de pantalla), su ojo
        /// de palma mira FIJO al reo y sus cinco dedos envuelven la elipse
        /// CIÑENDO al ritmo del masticar; en EL TRAGO el puño se CIERRA y
        /// SUBE con la nube. Estilo 12 (LAS TIJERAS DE LA PÁGINA) — EL
        /// CORTE FINAL: las hojas gigantes tijeretean al reo con cada
        /// mordida (cada cierre, un TAJO BLANCO que lo cruza) y en EL TRAGO
        /// quedan CERRADAS. Estilo 13 (LA PÁGINA ARRANCADA) — EL ARREBATO:
        /// el MARCO de hoja de cuaderno (renglones + margen rojo + sellos
        /// rúnicos) ciñe la elipse, tiembla con cada mordida y en EL TRAGO
        /// SE ARRANCA — vuela al portador dejando el hueco blanco.
        /// </summary>
        private void DrawAdorno(NPC jefe, byte estilo)
        {
            if (estilo < 11) return;
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 31 + jefe.whoAmI;
            float t = Projectile.ai[2];

            float envuelve = MathHelper.Clamp((t - 30f) / 30f, 0f, 1f);
            float festin = MathHelper.Clamp((t - 60f) / 90f, 0f, 1f);
            float inhala = MathHelper.Clamp((t - 150f) / 55f, 0f, 1f);
            float vivo = 1f;
            float radio = MathHelper.Clamp(jefe.Size.Length() * 0.7f, 90f, 300f);
            // LA ELIPSE REAL DEL JEFE (la lección del Rey Slime — como EL DEVORADOR)
            float rx = MathHelper.Clamp(jefe.width * 0.62f, 76f, 330f);
            float ry = MathHelper.Clamp(jefe.height * 0.62f, 76f, 350f);

            switch (estilo)
            {
                case 11: // LA MANO DEL ESCRIBA — el puño que sepulta
                {
                    if (envuelve <= 0.05f) break;

                    // LA PALMA COLOSAL cuelga del techo sobre el reo (el
                    // brazo vive FUERA de pantalla: el escriba ESCRIBE desde arriba)
                    float muerde = CicloMasticar(t);
                    float cuelga = 70f + ry * 0.95f - 30f * inhala;   // en el trago RECOGE
                    Vector2 palma = jefe.Center + new Vector2(0f, -cuelga);
                    Vector2 haciaReo = (jefe.Center - palma).SafeNormalize(Vector2.UnitY);

                    // el brazo que sube al techo (la muñeca colosal)
                    VFXCore.Begin();
                    VFXCore.Line(palma, palma + new Vector2(0f, -260f),
                        SombrasLib.Alfa(SombrasLib.Negro, 0.95f * envuelve * vivo), 58f);
                    VFXCore.FlushAlpha(VFXCore.Pixel);

                    // LA PALMA + su OJO rasgado MIRANDO FIJO al reo
                    VFXCore.Begin();
                    VFXCore.Quad(palma, SombrasLib.Alfa(SombrasLib.Negro, 0.95f * envuelve * vivo),
                        new Vector2(rx * 1.7f, ry * 0.95f), 0f, VFXCore.GlowOrb);
                    VFXCore.FlushAlpha();
                    VFXCore.Begin();
                    SombrasLib.Ojo(palma + haciaReo * 6f, 34f, jefe.Center - palma,
                        envuelve * vivo, pupila: true, rasgada: true);
                    VFXCore.FlushAdditive();

                    // LOS CINCO DEDOS envuelven la elipse — CIÑEN al masticar
                    for (int k = 0; k < 5; k++)
                    {
                        float u = (k - 2f) / 2f;                        // −1..1
                        bool pulgar = k == 4;
                        float lado = pulgar ? 1.45f : u;
                        Vector2 raiz = palma + new Vector2(lado * rx * 0.62f, 12f);

                        // el punto de la jaula: el arco SUPERIOR de la elipse,
                        // ciñe al morder (y en el trago CIERRA el puño)
                        float jj = pulgar ? 1.18f : u * 0.92f;
                        float yy = MathF.Sqrt(MathF.Max(0f, 1f - jj * jj));
                        Vector2 jaula = jefe.Center + new Vector2(jj * rx,
                            -yy * ry * (1.02f - 0.22f * muerde) + 8f);
                        jaula = Vector2.Lerp(jaula, jefe.Center, inhala * 0.8f);

                        Vector2[] dedo = SombrasLib.ColumnaViva(raiz, jaula, tiempo, semilla + 511 + k * 13,
                            9, 0.12f, gancho: 0f);
                        SombrasLib.Masa(dedo, 6f, 4f, 0.92f * envuelve * vivo, semilla + k * 13, tiempo, grosorCentro: 20f);
                        SombrasLib.Garra(dedo[dedo.Length - 1], (jaula - raiz).SafeNormalize(Vector2.UnitY),
                            34f * envuelve, 0.9f * vivo, 0.6f);
                    }

                    // la bruma entre los dedos al apretar
                    if (muerde < 0.25f)
                        SombrasLib.Bruma(jefe.Center + new Vector2(0f, -ry * 0.7f), 46f,
                            0.4f * vivo * (1f - muerde), tiempo, semilla + 88);
                    break;
                }
                case 12: // LAS TIJERAS DE LA PÁGINA — el corte final
                {
                    if (envuelve <= 0.05f) break;

                    // EL GOZNE en la diagonal; las hojas GIGANTES cruzan al reo
                    float muerde = CicloMasticar(t);
                    Vector2 gozne = jefe.Center + new Vector2(-rx * 1.25f, -ry * 1.25f) * (1f - 0.3f * inhala);
                    float bisagra = (jefe.Center - gozne).ToRotation();
                    float largo = Vector2.Distance(gozne, jefe.Center) * 2.35f;
                    // abre con el masticar; en EL TRAGO quedan CERRADAS del todo
                    float ap = inhala > 0.45f ? 0.02f : 0.22f + 0.68f * muerde;

                    for (int lado = -1; lado <= 1; lado += 2)
                    {
                        float ang = bisagra + lado * (0.12f + ap * 0.58f);
                        // LA HOJA CURVA — el arco desde el gozne (la misma
                        // matemática del arma, GIGANTE)
                        Vector2[] hoja = new Vector2[10];
                        for (int i = 0; i < 10; i++)
                        {
                            float f = i / 9f;
                            float r = MathHelper.Lerp(30f, largo, f * f * (3f - 2f * f));
                            float aA = ang - lado * 0.40f * f * f;
                            hoja[i] = gozne + new Vector2(MathF.Cos(aA), MathF.Sin(aA)) * r;
                        }
                        SombrasLib.Masa(hoja, 10f, 5f, 0.94f * envuelve * vivo, semilla + lado * 17, tiempo, grosorCentro: 26f);

                        // EL FILO BLANCO — el canto interior de hueso
                        VFXCore.Begin();
                        Vector2 adentro = new(MathF.Cos(ang - lado * 0.30f), MathF.Sin(ang - lado * 0.30f));
                        for (int i = 2; i < hoja.Length; i++)
                            VFXCore.Line(hoja[i - 1] + adentro * 7f, hoja[i] + adentro * 7f,
                                SombrasLib.Alfa(SombrasLib.Blanco, 0.85f * envuelve * vivo), 4.5f);
                        VFXCore.FlushAdditive(VFXCore.Pixel);
                    }

                    // EL REMACHE violeta del gozne
                    VFXCore.Begin();
                    VFXCore.Quad(gozne, SombrasLib.Alfa(SombrasLib.Violeta, 0.35f * envuelve * vivo),
                        new Vector2(90f, 90f));
                    VFXCore.FlushAdditive();

                    // EL TAJO: al cerrar, la línea blanca CRUZA al reo a lo
                    // largo de las hojas (la línea de corte) + su onda
                    if (muerde < 0.12f)
                    {
                        Vector2 eje = bisagra.ToRotationVector2();
                        float fuerza = 1f - muerde / 0.12f;
                        VFXCore.Begin();
                        VFXCore.Line(jefe.Center - eje * (rx + 40f), jefe.Center + eje * (rx + 40f),
                            SombrasLib.Alfa(SombrasLib.Blanco, 0.9f * fuerza * vivo), 12f * fuerza);
                        VFXCore.FlushAdditive(VFXCore.Pixel);
                        SombrasLib.OndaChoque(jefe.Center, radio * (0.6f + 0.5f * fuerza),
                            0.45f * fuerza * vivo, roja: false);
                    }
                    break;
                }
                case 13: // LA PÁGINA ARRANCADA — el arrebato
                {
                    if (envuelve <= 0.05f) break;

                    // EL MARCO alrededor de la elipse: ciñe conforme come
                    float muerde = CicloMasticar(t);
                    float esc = MathHelper.Lerp(1.55f, 1.04f, festin);
                    Vector2 c = jefe.Center;
                    Vector2 a = c - new Vector2(rx * esc, ry * esc);
                    Vector2 b = c + new Vector2(rx * esc, ry * esc);
                    Vector2[] esq = { new(a.X, a.Y), new(b.X, a.Y), new(b.X, b.Y), new(a.X, b.Y) };

                    // en EL TRAGO la hoja SE ARRANCA: vuela al portador
                    Player portador = Portador(jefe);
                    float vuela = inhala;
                    if (vuela > 0.05f)
                    {
                        Vector2 posHoja = Vector2.Lerp(c, portador.MountedCenter, SombrasLib.DeGolpe(vuela));
                        float encoge = (1f - 0.8f * vuela) * esc;
                        Vector2 nA = posHoja - new Vector2(rx * encoge, ry * encoge);
                        Vector2 nB = posHoja + new Vector2(rx * encoge, ry * encoge);
                        esq[0] = new(nA.X, nA.Y); esq[1] = new(nB.X, nA.Y);
                        esq[2] = new(nB.X, nB.Y); esq[3] = new(nA.X, nB.Y);

                        // EL VOODO — el hueco blanco donde estaba el reo
                        if (vuela < 0.6f)
                        {
                            VFXCore.Begin();
                            VFXCore.Quad(c, SombrasLib.Alfa(SombrasLib.Blanco, 0.5f * (1f - vuela / 0.6f) * vivo),
                                new Vector2(rx * 2f, ry * 2f), 0f, VFXCore.Pixel);
                            VFXCore.FlushAlpha(VFXCore.Pixel);
                        }
                    }

                    // LAS CUATRO BARRAS del marco + su rim
                    VFXCore.Begin();
                    for (int k = 0; k < 4; k++)
                    {
                        VFXCore.Line(esq[k], esq[(k + 1) % 4], SombrasLib.Alfa(SombrasLib.Negro, 0.95f * envuelve * vivo), 22f);
                        VFXCore.Line(esq[k], esq[(k + 1) % 4], SombrasLib.Alfa(SombrasLib.Violeta, 0.16f * vivo), 30f);
                    }
                    VFXCore.FlushAlpha(VFXCore.Pixel);

                    // RENGLONES + MARGEN ROJO + sellos rúnicos (la hoja)
                    VFXCore.Begin();
                    for (int k = 1; k <= 6; k++)
                    {
                        float f = k / 7f;
                        float y = MathHelper.Lerp(esq[0].Y, esq[2].Y, f);
                        VFXCore.Line(new Vector2(esq[0].X + 8f, y), new Vector2(esq[1].X - 8f, y),
                            SombrasLib.Alfa(SombrasLib.Violeta, 0.12f * envuelve * vivo * (1f - vuela)), 2f);
                    }
                    float xM = MathHelper.Lerp(esq[0].X, esq[1].X, 0.24f);
                    VFXCore.Line(new Vector2(xM, esq[0].Y + 6f), new Vector2(xM, esq[3].Y - 6f),
                        SombrasLib.Alfa(SombrasLib.Rojo, 0.16f * envuelve * vivo * (1f - vuela)), 2.5f);
                    for (int k = 0; k < 4; k++)
                        VFXCore.Quad(esq[k], SombrasLib.Alfa(SombrasLib.Blanco, 0.5f * envuelve * vivo),
                            VFXCore.RingQuadSize(16f), 0f, VFXCore.Ring);
                    VFXCore.FlushAdditive();

                    // EL TIEMBLA: zigzag blanco en las esquinas con cada mordida
                    if (muerde < 0.2f && vuela <= 0.05f)
                    {
                        float fuerza = 1f - muerde / 0.2f;
                        VFXCore.Begin();
                        for (int k = 0; k < 4; k++)
                        {
                            Vector2 e = esq[k];
                            Vector2 adentro = (c - e).SafeNormalize(Vector2.UnitX) * 30f;
                            for (int j = -1; j <= 1; j += 2)
                                VFXCore.Line(e + adentro * 0.4f,
                                    e + adentro + new Vector2(-adentro.Y, adentro.X) * (j * 0.8f),
                                    SombrasLib.Alfa(SombrasLib.Blanco, 0.85f * fuerza * vivo), 3.5f);
                        }
                        VFXCore.FlushAdditive(VFXCore.Pixel);
                    }

                    // la bruma del perímetro de la hoja
                    Vector2[] perim = new Vector2[16];
                    for (int k = 0; k < 16; k++)
                    {
                        float f = k / 16f * 4f;
                        perim[k] = Vector2.Lerp(esq[(int)f % 4], esq[((int)f + 1) % 4], SombrasLib.Frac(f));
                    }
                    SombrasLib.BrumaColumna(perim, 24f, 0.36f * envuelve * vivo, tiempo, semilla + 97, 10);
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
