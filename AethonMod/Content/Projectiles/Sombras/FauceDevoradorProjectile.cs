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
    /// ai[0] = whoAmI del jefe · ai[1] = estilo (3 = La Sombra de la Página) · ai[2] = tick.
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
                DrawTentaculo(jefe, vivo);
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
