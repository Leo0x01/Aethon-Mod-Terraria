using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Projectiles.Sombras
{
    /// <summary>
    /// PRIMERACENAPROJECTILE — v6.50.110 — LA PELÍCULA DEL FESTÍN DEL
    /// PRIMER JEFE.
    ///
    /// La letra del usuario: «cuando se mata un jefe su animacion de
    /// muerte se detiene y sea deborado por el grimorio usando la sombra
    /// de la pagina, el jugador recibe loot ya que cuenta como una muerte
    /// normal, lo que sucede es que el jugador ataca al jefe y cuando
    /// llegue a 100 de vida el jefe se detiene circulos magicos como los
    /// de los agujeros negros aprisionan al jefe, lo levantan y llevan
    /// al centro de la pantalla y luego del grimorio sale la sombra de
    /// la pagina y devora por completo al jefe».
    ///
    /// LA COREOGRAFÍA (los relojes calzan con FaucesGlobalNPC, que mueve
    /// al jefe y suelta la muerte real a los 430 t):
    ///   0-40   EL SELLO — TRES círculos mágicos (los erosionados de los
    ///          agujeros negros — OrbitaLib.SigiloErosionado) se
    ///          materializan alrededor del jefe detenido.
    ///   40-60  LA CONTRACCIÓN — los aros SE CIERRAN y lo aprisionan.
    ///   60-150 LA ELEVACIÓN — los sellos lo levantan (−90 px) y lo
    ///          llevan al CENTRO DE LA PANTALLA (el jefe viaja: lo mueve
    ///          FaucesGlobalNPC en la autoridad; aquí sólo lo dibujamos
    ///          colgado de sus aros).
    ///   150-175 LA PRESENTACIÓN — el sello íntimo se cierra del todo.
    ///   175-345 EL FESTÍN — DEL GRIMORIO (la mano del portador) sale LA
    ///          SOMBRA DE LA PÁGINA (el cuerpo gigante de la casa:
    ///          ColumnaViva + Masa fusiforme + bruma + ojos + cabeza de
    ///          bruma) y lo DEVORA (el Devorador v2 de elipse completa,
    ///          las almas volando al portador) mientras los sellos SE
    ///          ENCIENEN en él (radio → 0: el vice lo exprime hacia
    ///          adentro, como el horizonte de un agujero negro).
    ///   345-430 EL TRAGO — la elipse entera SE INHALA hacia la punta de
    ///          la sombra; el sprite del jefe ya se retiró (t ≥ 260).
    ///   430    LA MUERTE REAL (la suelta FaucesGlobalNPC): loot completo
    ///          — el gore NO se ve (PurgaVisualMuerte lo tapa).
    ///   +60    EL POSO — las últimas volutas sobre el botín.
    ///
    /// ai[0] = whoAmI del jefe · ai[1] = whoAmI del portador · ai[2] = tick.
    /// </summary>
    public class PrimeraCenaProjectile : ModProjectile
    {
        // === LOS RELOJES (idénticos a FaucesGlobalNPC — la película y el
        //     motor respiran al mismo tiempo) ===
        private const float T_CONTRACCION = 40f;
        private const float T_ELEVACION = 60f;
        private const float T_CENTRO = 150f;
        private const float T_FESTIN = 175f;
        private const float T_TRAGO = 345f;
        private const float T_MUERTE = 430f;   // == FaucesGlobalNPC.LargoPrimeraCena

        private NPC Jefe => Projectile.ai[0] < 0 || Projectile.ai[0] >= Main.npc.Length
            ? null : Main.npc[(int)Projectile.ai[0]];

        private Player Portador
        {
            get
            {
                int i = (int)Projectile.ai[1];
                if (i < 0 || i >= Main.maxPlayers) return null;
                Player p = Main.player[i];
                return p != null && p.active ? p : null;
            }
        }

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 560;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            NPC jefe = Jefe;
            bool vivo = jefe != null && jefe.active;

            // === EL POSO (el jefe ya murió de verdad): la bruma de la
            //     entrega sigue viva un rato sobre el loot y PURGA la
            //     muerte vanilla de la zona ===
            if (!vivo)
            {
                if (Projectile.timeLeft > 80) Projectile.timeLeft = 80;
                if (t >= T_MUERTE + 62) { Projectile.Kill(); return; }

                // LA PURGA también desde la AI (SP / cliente dueño — en los
                // demás clientes la hace el PreDraw, que corre en todos)
                if (t < T_MUERTE + 56) SombrasLib.PurgaVisualMuerte(Projectile.Center, 420f);

                // las últimas volutas sobre el botín
                if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(4))
                {
                    float f = MathHelper.Clamp((t - T_MUERTE) / 62f, 0f, 1f);
                    Dust d = Dust.NewDustPerfect(Projectile.Center + new Vector2(Main.rand.NextFloat(-80f, 80f), Main.rand.NextFloat(-50f, 30f)),
                        DustID.Shadowflame, new Vector2(0f, -0.6f), 128, default, 0.6f * (1f - f));
                    d.noGravity = true;
                }
                return;
            }

            Projectile.Center = jefe.Center;

            if (Main.netMode == NetmodeID.Server) return;   // el server no dibuja

            // === POLVO DEL CINE (determinista basta) ===
            // la elevación: el polvo del jefe CAE mientras sube
            if (t > T_ELEVACION && t < T_CENTRO && Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(jefe.Center + new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-0.3f, 1)) * jefe.Size * 0.4f,
                    DustID.Shadowflame, new Vector2(0f, 1.6f), 128, default, 0.8f);
                d.noGravity = false;
            }
            // el festín: chispas de la digestión
            if (t > T_FESTIN && t < T_TRAGO && Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(jefe.Center + new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)) * jefe.Size * 0.4f,
                    DustID.Shadowflame, new Vector2(0f, 1.1f), 128, default, 0.9f);
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
            // y el server; el PreDraw corre DONDE SE VE)
            if (!vivo && Projectile.ai[2] < T_MUERTE + 56)
                SombrasLib.PurgaVisualMuerte(Projectile.Center, 420f);
            if (jefe == null) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                DrawCena(jefe, vivo);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        // ==================================================================
        //  LA PELÍCULA
        // ==================================================================

        private void DrawCena(NPC jefe, bool vivo)
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = 911 + jefe.whoAmI * 13;   // LA SEMILLA DE LA CENA (única por festín)
            float t = Projectile.ai[2];
            Player portador = Portador ?? Main.player[Projectile.owner];
            Vector2 centroJefe = vivo ? jefe.Center : Projectile.Center;

            // === LA SALA DE CINE: el velo de los bordes (presente desde el
            //     sello, se va con el poso) ===
            float presencia = vivo
                ? MathHelper.Clamp(t / 30f, 0f, 1f)
                : 1f - MathHelper.Clamp((t - T_MUERTE) / 62f, 0f, 1f);
            SombrasLib.Vignette(0.45f * presencia);

            // ================================================================
            //  ACTO 1 — LOS SELLOS QUE APRISIONAN (0 → T_TRAGO)
            //  TRES círculos mágicos erosionados (los de los agujeros
            //  negros): el GRANDE que lo rodea, el MEDIO que lo ciñe y el
            //  ÍNTIMO que lo estrangula. Se materializan (0-40), se
            //  CONTRAEN (40-60), lo llevan (60-150), lo presentan
            //  (150-175) y durante el festín SE ENCIENEN en él (radio → 0).
            // ================================================================
            if (t < T_TRAGO)
            {
                float radioJefe = MathHelper.Clamp(MathF.Max(jefe.width, jefe.height) * 0.62f, 70f, 320f);
                // la aparición del sello (de nada a todo en la fase 0)
                float manifiesta = SombrasLib.DeGolpe(MathHelper.Clamp(t / T_CONTRACCION, 0f, 1f));
                // la contracción (40-60): de la rueda ancha al abrazo
                float ciñe = MathHelper.Clamp((t - T_CONTRACCION) / 20f, 0f, 1f);
                // el festín exprime los sellos hacia DENTRO (175-345 → 0)
                float exprime = t < T_FESTIN ? 0f : MathHelper.Clamp((t - T_FESTIN) / (T_TRAGO - T_FESTIN), 0f, 1f);

                // los tres radios (×radioJefe): ANCHO → MEDIO → ÍNTIMO
                float rGrande = MathHelper.Lerp(3.1f, 1.30f, ciñe) * (1f - 0.92f * exprime);
                float rMedio = MathHelper.Lerp(2.5f, 1.05f, ciñe) * (1f - 0.95f * exprime);
                float rIntimo = MathHelper.Lerp(1.8f, 0.80f, ciñe) * (1f - 0.97f * exprime);

                // el latido del vice (más rápido conforme cierra)
                float latido = 1f + 0.06f * MathF.Sin(tiempo * (4f + 4f * exprime));

                Vector2 posPantalla = centroJefe - Main.screenPosition;
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    // EL GRAN SELLO — el aro exterior: lento, solemne
                    OrbitaLib.SigiloErosionado(posPantalla, rGrande * latido, tiempo, semilla,
                        1.45f, 10, 0.05f, MathF.Max(rGrande / 90f, 0.3f) * 1.4f,
                        new Color(196, 108, 255), new Color(236, 200, 255),
                        offset: 0, hashOff: 0, spikeAngle: -0.55f, spikeWindow: 0.45f);
                    // EL SELLO MEDIO — contrarrotando
                    OrbitaLib.SigiloErosionado(posPantalla, rMedio * latido, tiempo, semilla + 37,
                        1.9f, 8, -0.08f, MathF.Max(rMedio / 80f, 0.3f) * 1.4f,
                        new Color(150, 80, 235), new Color(210, 170, 255),
                        offset: 3, hashOff: 5, spikeAngle: 0.8f, spikeWindow: 0.5f);
                    // EL SELLO ÍNTIMO — el vice: rápido, apretado
                    OrbitaLib.SigiloErosionado(posPantalla, rIntimo * latido, tiempo, semilla + 91,
                        2.4f, 6, 0.14f, MathF.Max(rIntimo / 70f, 0.28f) * 1.5f,
                        new Color(240, 124, 65), new Color(255, 205, 140),
                        offset: 5, hashOff: 9, spikeAngle: -1.2f, spikeWindow: 0.6f);

                    // LA PLATAFORMA — el aro horizontal BAJO el jefe (el
                    // disco que lo LEVANTA: la elipse chata de los sellos
                    // de invocación, respirando)
                    float anchoDisco = radioJefe * MathHelper.Lerp(2.2f, 1.35f, ciñe) * (1f - 0.85f * exprime);
                    if (anchoDisco > 12f)
                    {
                        Vector2 posDisco = posPantalla + new Vector2(0f, jefe.height * 0.55f + 14f);
                        float ySquash = 0.26f + 0.05f * MathF.Sin(tiempo * 2.2f);
                        Main.spriteBatch.Draw(VFXCore.SoftGlow, posDisco, null,
                            new Color(150, 80, 235, 255) * (0.30f * manifiesta), 0f,
                            VFXCore.SoftGlow.Size() * 0.5f, new Vector2(anchoDisco / 64f, anchoDisco * ySquash / 64f),
                            SpriteEffects.None, 0f);
                        Main.spriteBatch.Draw(VFXCore.SoftGlow, posDisco, null,
                            new Color(236, 200, 255, 255) * (0.22f * manifiesta), 0f,
                            VFXCore.SoftGlow.Size() * 0.5f, new Vector2(anchoDisco * 0.8f / 64f, anchoDisco * ySquash * 0.55f / 64f),
                            SpriteEffects.None, 0f);
                    }
                }
                finally { Main.spriteBatch.End(); }
            }

            // ================================================================
            //  ACTO 2 — LA SOMBRA DE LA PÁGINA (T_FESTIN → el poso)
            //  Del GRIMORIO (la mano que lo sostiene) sale el cuerpo
            //  gigante de la casa y DEVORA al preso del centro.
            // ================================================================
            if (t >= T_FESTIN - 20f)
            {
                float sale = SombrasLib.DeGolpe(MathHelper.Clamp((t - (T_FESTIN - 20f)) / 30f, 0f, 1f));
                float festin = MathHelper.Clamp((t - T_FESTIN) / 85f, 0f, 1f);     // la cobertura crece
                float inhala = MathHelper.Clamp((t - T_TRAGO) / 60f, 0f, 1f);      // EL TRAGO
                float poso = vivo ? 0f : MathHelper.Clamp((t - T_MUERTE) / 62f, 0f, 1f);
                float vivoF = vivo ? 1f - 0.75f * poso : 0.25f * (1f - poso);

                // LA RAÍZ: la MANO del portador (el grimorio EN ELLA)
                Vector2 raiz = portador.MountedCenter + new Vector2(portador.direction * 12f, 6f);

                // === EL CHARCO DEL PORTADOR ===
                SombrasLib.Charco(raiz + new Vector2(0f, portador.height * 0.42f), 64f * sale, 0.85f * vivoF, tiempo, semilla);

                // === EL CUERPO GIGANTE: mano → jefe — y durante EL TRAGO la
                //     punta VUELVE al portador (la sombra se lleva la nube) ===
                float alcance = sale * 0.35f + festin * 0.65f;
                Vector2 destino = Vector2.Lerp(raiz + new Vector2(0, -160f), centroJefe, alcance);
                destino = Vector2.Lerp(destino, raiz + new Vector2(0f, -50f), inhala * 0.85f);
                Vector2[] col = SombrasLib.ColumnaViva(raiz, destino, tiempo, semilla, 22, 0.20f, gancho: 0.45f);
                SombrasLib.Masa(col, 14f, 22f, 0.96f * vivoF, semilla, tiempo, grosorCentro: 62f);

                // === LA BRUMA DEL CUERPO GIGANTE (dos capas) ===
                SombrasLib.BrumaColumna(col, 54f, 0.55f * vivoF, tiempo, semilla, 16);
                SombrasLib.BrumaColumna(col, 36f, 0.42f * vivoF, tiempo, semilla + 53, 12);

                // === LOS OJOS: TODOS abiertos, TODOS mirando al jefe ===
                SombrasLib.OjosDeMasa(col, centroJefe, (0.4f + 0.6f * festin) * vivoF, semilla, tiempo, 7);

                // === LA CABEZA ES BRUMA (la boca de la casa) ===
                Vector2 punta = col[col.Length - 1];
                Vector2 rumbo = vivo
                    ? (centroJefe - punta).SafeNormalize(Vector2.UnitX)
                    : (raiz - punta).SafeNormalize(-Vector2.UnitY);
                float muerde = vivo ? CicloMasticar(t) : 0.4f;
                float vigor = MathHelper.Clamp(0.55f + 0.45f * muerde + 0.25f * inhala, 0f, 1f) * vivoF;
                SombrasLib.CabezaDeBruma(punta, rumbo, vigor, vivoF, tiempo, semilla);

                // === EL DEVORADOR v2 — LA CUBIERTA TOTAL (la elipse REAL
                //     del preso) que crece con el festín y SE INHALA entera
                //     hacia la punta durante el trago ===
                if (vivo)
                {
                    float intensidad = MathHelper.Clamp(festin * 0.5f + MathHelper.Clamp((t - T_FESTIN - 85f) / 85f, 0f, 1f) * 0.5f, 0f, 1f);
                    SombrasLib.Devorador(punta, jefe, intensidad, vivoF, tiempo, semilla, col, trago: inhala);

                    // LA HERIDA: donde la punta ENTERRÓ brilla rojo (digestión)
                    VFXCore.Begin();
                    VFXCore.Quad(punta, SombrasLib.Alfa(SombrasLib.Rojo, (0.42f + 0.20f * inhala) * vivoF),
                        new Vector2(120f, 120f), rumbo.ToRotation());
                    VFXCore.FlushAdditive();
                }

                // === LA CORRIENTE DEL TRAGO — la bruma vuelve por el CUERPO ===
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

                // === LAS ALMAS: la vida del jefe vuela al portador (en el
                //     trago son UN RÍO — ocho, aceleradas) ===
                if (t > T_FESTIN - 15f)
                {
                    VFXCore.Begin();
                    int nAlmas = vivo ? 6 + (int)(3f * inhala) : 4;
                    float radio = MathHelper.Clamp(jefe.Size.Length() * 0.7f, 90f, 300f);
                    for (int k = 0; k < nAlmas; k++)
                    {
                        float prog = SombrasLib.Frac(t * (0.016f + 0.014f * inhala) + k * (1f / nAlmas));
                        Vector2 a = centroJefe + new Vector2(MathF.Cos(k * 2.7f + tiempo * 0.7f), MathF.Sin(k * 3.1f + tiempo)) * radio * 0.4f;
                        Vector2 b = portador.MountedCenter;
                        Vector2 ctrl = (a + b) * 0.5f + new Vector2(MathF.Sin(k * 2.3f) * 120f, -170f);
                        float u = 1f - prog;
                        Vector2 pos = u * u * a + 2f * u * prog * ctrl + prog * prog * b;
                        SombrasLib.Ojo(pos, 7f, b - pos, 1f);        // el alma ES un ojo que mira a casa
                    }
                    VFXCore.FlushAdditive();
                }
            }

            // === EL POSO — la entrega: el último velo sobre el loot y el
            //     pulso final de la onda (el libro se limpia la boca) ===
            if (!vivo)
            {
                float poso = MathHelper.Clamp((t - T_MUERTE) / 62f, 0f, 1f);
                float fade = 1f - poso;
                SombrasLib.Bruma(Projectile.Center, 130f * (0.6f + 0.4f * fade), 0.7f * fade, tiempo, semilla,
                    new Vector2(MathF.Sin(tiempo * 0.7f) * 24f, -14f));
                if (t < T_MUERTE + 10f)
                    SombrasLib.OndaChoque(Projectile.Center, 60f + 240f * (t - T_MUERTE + 10f) / 10f, 0.5f * fade);
            }
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }

        /// <summary>El masticar del festín: abre… ¡CIERRA de golpe! …mastica… abre.</summary>
        private static float CicloMasticar(float t)
        {
            float m = (t - T_FESTIN) % 32f;
            if (m < 10f) return MathHelper.Lerp(0.2f, 1f, m / 10f);                          // abre
            if (m < 16f) return MathHelper.Lerp(1f, 0.03f, SombrasLib.DeGolpe((m - 10f) / 6f)); // ¡CIERRA!
            if (m < 26f) return 0.15f + 0.1f * MathF.Sin(m * 1.4f);                          // mastica
            return MathHelper.Lerp(0.15f, 0.85f, (m - 26f) / 6f);                            // reabre
        }
    }
}
