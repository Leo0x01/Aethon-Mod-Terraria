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
    /// PRIMERACENAPROJECTILE — v6.50.115 — LA PELÍCULA DEL FESTÍN DEL
    /// PRIMER JEFE: EL LIBRO A UN COSTADO MIRANDO FIJO, LA SOMBRA DE
    /// LA PÁGINA DEVORA.
    ///
    /// La letra del usuario (.115): «La sombra de la página es
    /// necesaria ya que es la forma en la que el grimorio debe
    /// comerse a los jefes… primero quita los 3 anillos de fotones,
    /// eso no es de lo que estaba hablando, yo lo que me refería era
    /// a los anillos de los agujeros rúnicos… más los anillos de los
    /// soles que sí están bien, estos anillos de agujero negro rúnico
    /// deben brillar… la mayor parte de la animación está bien, solo
    /// que no está en el centro de la pantalla, se centra en el
    /// jugador, creo que en este caso es mejor ubicar al jefe encima
    /// del jugador y que el grimorio se ponga a un lado para devorar
    /// al jefe, la misma animación pero con la sombra de la página
    /// saliendo del libro y con el libro mirando fijamente a su
    /// víctima».
    ///
    /// LA COREOGRAFÍA (los relojes calzan con FaucesGlobalNPC, que
    /// mueve al jefe — ENCIMA DEL PORTADOR desde .115 — y suelta la
    /// muerte real a los 430 t):
    ///   0-40   EL SELLO — LA TRIPLE CORONA DE CONJURO (LOS ANILLOS
    ///          RÚNICOS DE LOS AGUJEROS NEGROS: el círculo DORADO de 8
    ///          runas + el VIOLETA de 6 contrarrotando + el BLANCO
    ///          íntimo de 6, las runas DE PIE con sus perlas —
    ///          OrbitaLib.CoronaConjuro, el círculo LITERAL de los
    ///          vórtices) ABRILLANTADA con su resplandor pulsante, y
    ///          los ANILLOS RÚNICOS DE LOS SOLES (los que SÍ estaban
    ///          bien — SigiloLib.SistemaAnillos) se materializan
    ///          alrededor del jefe detenido. LOS TRES ANILLOS DE
    ///          FOTONES RGB DE LA .114 MUEREN (no eran los de la
    ///          letra).
    ///   40-60  LA CONTRACCIÓN — los aros SE CIERRAN y lo aprisionan.
    ///   60-150 LA ELEVACIÓN — los anillos lo levantan (−90 px) y lo
    ///          llevan ENCIMA DEL PORTADOR (el ancla .115 del motor).
    ///   150-195 EL LIBRO DESPIERTA — el Eterno sale de la mano del
    ///          portador, CRECE ×2,6 y toma su puesto A UN COSTADO del
    ///          preso, INCLINADO hacia él, MIRÁNDOLO FIJO.
    ///   175-345 LA SOMBRA DE LA PÁGINA — sale DEL LIBRO (la raíz ya
    ///          NO es la mano del portador): el cuerpo vivo
    ///          (ColumnaViva + Masa + bruma + los OJOS que miran al
    ///          preso + la cabeza de bruma) trepa hacia su víctima y
    ///          la DEVORA (el Devorador v2, la elipse completa).
    ///   195-345 EL FESTÍN — las ALMAS y la bruma corren del jefe a la
    ///          BOCA DEL LIBRO, que mastica (CicloMasticar) con la
    ///          herida roja encendida; los anillos se EXPRIMEN.
    ///   345-430 EL TRAGO — los anillos ENTEROS se deslizan y colapsan
    ///          DENTRO del libro (radio → 0 en la boca), la sombra
    ///          VUELVE cargando la masa; a los 400 EL LIBRO SE CIERRA
    ///          de golpe (el párpado Cerrado + el chasquido + la onda).
    ///   430    LA MUERTE REAL (la suelta FaucesGlobalNPC): loot completo
    ///          — el gore NO se ve (PurgaVisualMuerte lo tapa).
    ///   430-490 EL REGRESO — el libro, saciado, VUELVE a la mano del
    ///          portador encogiendo; las últimas volutas sobre el botín.
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
        private const float T_LIBRO = 150f;     // el libro despierta (mismo t que el ancla)
        private const float T_LIBRO_LISTO = 195f;
        private const float T_FESTIN = 195f;    // la boca ya está lista: come desde la llegada
        private const float T_TRAGO = 345f;
        private const float T_CIERRE = 400f;    // EL LIBRO SE CIERRA de golpe
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
            // el festín: chispas de la digestión — CAEN hacia la boca del libro
            if (t > T_FESTIN && t < T_TRAGO && Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(jefe.Center + new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)) * jefe.Size * 0.4f,
                    DustID.Shadowflame, new Vector2(0f, 1.4f), 128, default, 0.9f);
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

            // === LAS FASES ===
            float manifiesta = SombrasLib.DeGolpe(MathHelper.Clamp(t / T_CONTRACCION, 0f, 1f));
            float ciñe = MathHelper.Clamp((t - T_CONTRACCION) / 20f, 0f, 1f);
            float exprime = t < T_FESTIN ? 0f
                : MathHelper.Clamp((t - T_FESTIN) / (T_CIERRE - T_FESTIN), 0f, 1f);
            float inhala = MathHelper.Clamp((t - T_TRAGO) / (T_MUERTE - T_TRAGO), 0f, 1f);
            float poso = vivo ? 0f : MathHelper.Clamp((t - T_MUERTE) / 62f, 0f, 1f);

            float radioJefe = MathHelper.Clamp(MathF.Max(jefe.width, jefe.height) * 0.62f, 70f, 320f);

            // ================================================================
            //  v6.50.115 — EL PUESTO DEL LIBRO: A UN COSTADO del preso (la
            //  letra: «el grimorio se ponga a un lado para devorar al
            //  jefe»), al lado que mira el portador, a la altura del
            //  pecho del preso — para MIRARLO FIJO de costado
            // ================================================================
            int lado = portador != null && portador.active && portador.direction != 0
                ? portador.direction : 1;
            Vector2 anclaLibro = centroJefe + new Vector2(lado * (radioJefe + 118f), radioJefe * 0.10f);

            // === LA POSICIÓN VIVA DEL LIBRO (compartida: el dibujo del
            //     libro Y la raíz de la sombra respiran igual) ===
            Vector2 posLibro = PosicionVivaDelLibro(portador, anclaLibro, tiempo, t, vivo);

            // ================================================================
            //  ACTO 1 — EL CERCO (0 → la muerte): LA TRIPLE CORONA DE
            //  CONJURO (los ANILLOS RÚNICOS de los AGUJEROS NEGROS,
            //  ABRILLANTADOS) y LOS ANILLOS RÚNICOS de los soles,
            //  aprisionando al jefe
            // ================================================================
            if (t < T_MUERTE && presencia > 0.02f)
            {
                // EL LATIDO del vice (más rápido conforme exprime)
                float latido = 1f + 0.06f * MathF.Sin(tiempo * (4f + 4f * exprime));

                // el radio del vórtice de la corona: se CIÑE al preso (40-60)
                // y al TRAGO se lo lleva entero (345+)
                float rBase = MathHelper.Lerp(1.10f, 0.46f, ciñe);
                rBase = MathHelper.Lerp(rBase, 0.10f, inhala);
                float rCorona = radioJefe * rBase * latido;

                // durante EL TRAGO el cerco ENTERO se desliza a la boca del
                // libro y muere ahí (los anillos se lo comen con él)
                Vector2 centroAros = Vector2.Lerp(centroJefe, anclaLibro, inhala);
                float alfaAros = presencia * (1f - 0.9f * inhala * inhala);
                Vector2 posPantalla = centroAros - Main.screenPosition;

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    // --- 1a. EL ABRILLANTADO (la letra: «estos anillos de
                    //     agujero negro rúnico deben brillar»): el
                    //     resplandor DORADO + VIOLETA pulsando DETRÁS de la
                    //     corona — y ARDE al comer y al tragar ---
                    float brillo = 0.15f + 0.09f * MathF.Sin(tiempo * 3.1f)
                        + 0.30f * exprime + 0.25f * inhala;
                    float g = rCorona * 3.4f;
                    if (g > 12f)
                    {
                        Main.spriteBatch.Draw(VFXCore.SoftGlow, posPantalla, null,
                            new Color(255, 186, 92, 255) * (0.55f * brillo * alfaAros), 0f,
                            VFXCore.SoftGlow.Size() * 0.5f, new Vector2(g / 64f, g / 64f),
                            SpriteEffects.None, 0f);
                        Main.spriteBatch.Draw(VFXCore.SoftGlow, posPantalla, null,
                            new Color(150, 110, 255, 255) * (0.38f * brillo * alfaAros), 0f,
                            VFXCore.SoftGlow.Size() * 0.5f, new Vector2(g * 1.15f / 64f, g * 1.15f / 64f),
                            SpriteEffects.None, 0f);
                    }

                    // --- 1b. LA TRIPLE CORONA DE CONJURO — LOS ANILLOS
                    //     RÚNICOS DE LOS AGUJEROS NEGROS (la letra .115: NO
                    //     los fotones RGB de la .114 — el CÍRCULO DE CONJURO
                    //     LITERAL de los vórtices: el dorado de 8 girando +
                    //     el violeta de 6 contrarrotando + el blanco íntimo
                    //     de 6 rápido, las runas DE PIE con sus perlas; y la
                    //     rueda CORRE al comer) ---
                    if (rCorona > 4f && alfaAros > 0.04f)
                    {
                        float giro = 1f + 0.9f * exprime;    // la rueda corre al comer
                        OrbitaLib.CoronaConjuro(posPantalla, rCorona, tiempo * giro,
                            alfaAros * 1.10f, 2.1f);
                    }

                    // LA PLATAFORMA — el aro horizontal BAJO el jefe (el
                    // disco que lo LEVANTA, respirando) — se desliza con los aros
                    float anchoDisco = radioJefe * MathHelper.Lerp(2.2f, 1.35f, ciñe) * (1f - 0.85f * exprime);
                    if (anchoDisco > 12f)
                    {
                        Vector2 posDisco = posPantalla + new Vector2(0f, jefe.height * 0.55f + 14f);
                        float ySquash = 0.26f + 0.05f * MathF.Sin(tiempo * 2.2f);
                        Main.spriteBatch.Draw(VFXCore.SoftGlow, posDisco, null,
                            new Color(150, 80, 235, 255) * (0.30f * manifiesta * alfaAros), 0f,
                            VFXCore.SoftGlow.Size() * 0.5f, new Vector2(anchoDisco / 64f, anchoDisco * ySquash / 64f),
                            SpriteEffects.None, 0f);
                        Main.spriteBatch.Draw(VFXCore.SoftGlow, posDisco, null,
                            new Color(236, 200, 255, 255) * (0.22f * manifiesta * alfaAros), 0f,
                            VFXCore.SoftGlow.Size() * 0.5f, new Vector2(anchoDisco * 0.8f / 64f, anchoDisco * ySquash * 0.55f / 64f),
                            SpriteEffects.None, 0f);
                    }
                }
                finally { Main.spriteBatch.End(); }

                // --- 1c. LOS ANILLOS RÚNICOS DE LOS SOLES (los que SÍ
                //     estaban bien — la letra .115; el emisor compartido de
                //     la casa, coords de MUNDO en lote VFX): tres aros de
                //     runas (6·8·10 glifos) que giran, se inclinan y al
                //     exprimir CORREN más (la rueda viva) ---
                float rRunico = radioJefe * MathHelper.Lerp(2.45f, MathHelper.Lerp(1.45f, 0.15f, inhala), ciñe);
                if (rRunico > 4f && alfaAros > 0.04f)
                {
                    float spinUp = 1f + 1.6f * exprime;   // la rueda corre al comer
                    VFXCore.Begin();
                    SigiloLib.SistemaAnillos(centroAros, rRunico, tiempo * spinUp, semilla,
                        3, 0f, 0f, alfaAros * (1f - inhala * 0.6f));
                    VFXCore.FlushAdditive(null, false);
                }
            }

            // ================================================================
            //  ACTO 2 — EL LIBRO (T_LIBRO → el regreso): sale de la mano
            //  del portador, CRECE, vuela a su puesto A UN COSTADO del
            //  preso, se INCLINA hacia él y LO MIRA FIJO mientras come
            // ================================================================
            if (t >= T_LIBRO)
            {
                DibujarElLibro(jefe, vivo, portador, centroJefe, anclaLibro, posLibro, lado,
                    tiempo, semilla, t, manifiesta, exprime, inhala, poso, presencia);
            }

            // ================================================================
            //  ACTO 2b — LA SOMBRA DE LA PÁGINA (T_FESTIN−20 → el poso):
            //  v6.50.115 — LA SOMBRA VUELVE y sale DEL LIBRO (la letra:
            //  «con la sombra de la página saliendo del libro» — la raíz
            //  ya NO es la mano del portador): el cuerpo vivo trepa a su
            //  víctima y la DEVORA (el patrón .113, enraizado en el libro)
            // ================================================================
            if (t >= T_FESTIN - 20f && presencia > 0.02f && t >= T_LIBRO)
            {
                float sale = SombrasLib.DeGolpe(MathHelper.Clamp((t - (T_FESTIN - 20f)) / 30f, 0f, 1f));
                float festin = MathHelper.Clamp((t - T_FESTIN) / 85f, 0f, 1f);     // la cobertura crece
                float vivoF = vivo ? 1f - 0.75f * poso : 0.25f * (1f - poso);

                // LA RAÍZ: EL LIBRO (la sombra brota de sus páginas abiertas)
                Vector2 haciaJefe = centroJefe - posLibro;
                Vector2 rumboRaiz = haciaJefe.LengthSquared() > 4f
                    ? Vector2.Normalize(haciaJefe) : Vector2.UnitY;
                Vector2 raiz = posLibro + rumboRaiz * 16f;

                // EL CHARCO bajo el libro (la sombra que gotea de las páginas)
                SombrasLib.Charco(posLibro + new Vector2(0f, 30f), 52f * sale, 0.7f * vivoF, tiempo, semilla);

                // === EL CUERPO GIGANTE: libro → jefe — y durante EL TRAGO la
                //     punta VUELVE al libro (la sombra se lleva la nube A CASA) ===
                float alcance = sale * 0.35f + festin * 0.65f;
                Vector2 destino = Vector2.Lerp(raiz + rumboRaiz * 42f, centroJefe, alcance);
                destino = Vector2.Lerp(destino, raiz + rumboRaiz * 18f, inhala * 0.85f);
                Vector2[] col = SombrasLib.ColumnaViva(raiz, destino, tiempo, semilla, 22, 0.20f, gancho: 0.45f);
                SombrasLib.Masa(col, 12f, 20f, 0.95f * vivoF, semilla, tiempo, grosorCentro: 54f);

                // === LA BRUMA DEL CUERPO GIGANTE (dos capas) ===
                SombrasLib.BrumaColumna(col, 50f, 0.50f * vivoF, tiempo, semilla, 14);
                SombrasLib.BrumaColumna(col, 34f, 0.40f * vivoF, tiempo, semilla + 53, 11);

                // === LOS OJOS: TODOS abiertos, TODOS mirando a la víctima ===
                SombrasLib.OjosDeMasa(col, centroJefe, (0.4f + 0.6f * festin) * vivoF, semilla, tiempo, 7);

                // === LA CABEZA ES BRUMA (la boca de la casa) ===
                Vector2 punta = col[col.Length - 1];
                Vector2 rumbo = vivo
                    ? (centroJefe - punta).SafeNormalize(Vector2.UnitX)
                    : (raiz - punta).SafeNormalize(-Vector2.UnitY);
                float muerde = vivo && t >= T_FESTIN ? CicloMasticar(t) : 0.4f;
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
                        new Vector2(110f, 110f), rumbo.ToRotation());
                    VFXCore.FlushAdditive();
                }

                // === LA CORRIENTE DEL TRAGO — la bruma vuelve por el CUERPO
                //     (la digestión corre hacia el libro) ===
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
                            + new Vector2(MathF.Sin(k * 2.1f + tiempo * 3f) * 14f, -8f);
                        SombrasLib.Bruma(pos, 26f + 14f * SombrasLib.Frac(k * 0.73f),
                            0.50f * flujo * vivoF, tiempo, semilla + k * 7,
                            new Vector2(0f, -26f * flujo));
                    }
                }
            }

            // ================================================================
            //  LA CORRIENTE — la vida del preso baja a la boca del libro
            //  (bruma que corre + LAS ALMAS, ya no al portador: AL LIBRO)
            // ================================================================
            if (t >= T_FESTIN && presencia > 0.02f)
            {
                float flujo = vivo ? 1f : 1f - poso;
                Vector2 boca = posLibro + (centroJefe - posLibro).SafeNormalize(Vector2.UnitX) * 14f;

                // LA BRUMA DE LA CORRIENTE: puffs que nacen alrededor del
                // preso y se deslizan hacia la boca abierta del libro
                for (int k = 0; k < 5; k++)
                {
                    float prog = SombrasLib.Frac(tiempo * 0.55f + k * 0.2f);
                    Vector2 pos = Vector2.Lerp(
                        centroJefe + new Vector2(MathF.Sin(k * 2.4f + tiempo) * radioJefe * 0.5f,
                                                 MathF.Cos(k * 1.7f + tiempo * 1.3f) * radioJefe * 0.25f),
                        boca, prog * prog);
                    SombrasLib.Bruma(pos, 26f + 14f * SombrasLib.Frac(k * 0.73f),
                        0.50f * flujo * presencia, tiempo, semilla + k * 7,
                        new Vector2(0f, 22f * flujo));
                }

                // LAS ALMAS: la vida del jefe vuela a la BOCA DEL LIBRO (en
                // el trago son UN RÍO — ocho, aceleradas)
                VFXCore.Begin();
                int nAlmas = vivo ? 6 + (int)(3f * inhala) : 4;
                float radio = MathHelper.Clamp(jefe.Size.Length() * 0.7f, 90f, 300f);
                for (int k = 0; k < nAlmas; k++)
                {
                    float prog = SombrasLib.Frac(t * (0.016f + 0.014f * inhala) + k * (1f / nAlmas));
                    Vector2 a = centroJefe + new Vector2(MathF.Cos(k * 2.7f + tiempo * 0.7f), MathF.Sin(k * 3.1f + tiempo)) * radio * 0.4f;
                    Vector2 b = boca;
                    Vector2 ctrl = (a + b) * 0.5f + new Vector2(MathF.Sin(k * 2.3f) * 90f, -30f);
                    float u = 1f - prog;
                    Vector2 pos = u * u * a + 2f * u * prog * ctrl + prog * prog * b;
                    SombrasLib.Ojo(pos, 7f, b - pos, 1f);        // el alma ES un ojo que mira a la boca
                }
                VFXCore.FlushAdditive();
            }

            // === EL POSO — la entrega: el último velo sobre el loot y el
            //     pulso final de la onda ===
            if (!vivo)
            {
                float fade = 1f - poso;
                SombrasLib.Bruma(Projectile.Center, 130f * (0.6f + 0.4f * fade), 0.7f * fade, tiempo, semilla,
                    new Vector2(MathF.Sin(tiempo * 0.7f) * 24f, -14f));
                if (t < T_MUERTE + 10f)
                    SombrasLib.OndaChoque(Projectile.Center, 60f + 240f * (t - T_MUERTE + 10f) / 10f, 0.5f * fade);
            }
        }

        // ==================================================================
        //  LA POSICIÓN VIVA DEL LIBRO (v6.50.115 — COMPARTIDA): el viaje
        //  mano → el ancla AL COSTADO del preso, el vaivén sereno del
        //  flotante (y el temblor al masticar), y EL REGRESO al poso.
        //  El dibujo del libro Y la raíz de la sombra leen ESTA posición
        //  — la sombra brota de DONDE ESTÁ el libro, no de un ancla muerta.
        // ==================================================================
        private Vector2 PosicionVivaDelLibro(Player portador, Vector2 anclaLibro,
            float tiempo, float t, bool vivo)
        {
            float viaje = MathHelper.Clamp((t - T_LIBRO) / (T_LIBRO_LISTO - T_LIBRO), 0f, 1f);
            Vector2 raiz = portador != null && portador.active
                ? portador.MountedCenter + new Vector2(portador.direction * 12f, 6f)
                : Projectile.Center;

            // === EL REGRESO (el poso): saciado, vuelve a la mano ===
            float regreso = vivo ? 0f : MathHelper.Clamp((t - (T_MUERTE + 5f)) / 55f, 0f, 1f);

            Vector2 posMundo = Vector2.Lerp(raiz, anclaLibro, MathHelper.SmoothStep(0f, 1f, viaje));
            posMundo = Vector2.Lerp(posMundo, raiz, MathHelper.SmoothStep(0f, 1f, regreso));
            // el vaivén sereno del flotante (y un temblor al masticar)
            float muerde = vivo && t >= T_FESTIN ? CicloMasticar(t) : 0f;
            posMundo += new Vector2(MathF.Sin(tiempo * 1.1f) * 4f,
                MathF.Sin(tiempo * 1.4f) * 3f - muerde * 3f);
            return posMundo;
        }

        // ==================================================================
        //  EL LIBRO — el sprite REAL del Eterno (la familia del ojo:
        //  el mismo cuerpo del Nervioso), GRANDE, volando de la mano del
        //  portador a su puesto AL COSTADO del preso, INCLINADO hacia su
        //  víctima, ABIERTO y comiendo — y al final SE CIERRA de golpe
        //  y VUELVE
        // ==================================================================
        private void DibujarElLibro(NPC jefe, bool vivo, Player portador, Vector2 centroJefe,
            Vector2 anclaLibro, Vector2 posMundo, int lado, float tiempo, int semilla, float t,
            float manifiesta, float exprime, float inhala, float poso, float presencia)
        {
            float viaje = MathHelper.Clamp((t - T_LIBRO) / (T_LIBRO_LISTO - T_LIBRO), 0f, 1f);
            float regreso = vivo ? 0f : MathHelper.Clamp((t - (T_MUERTE + 5f)) / 55f, 0f, 1f);

            // === LA ESCALA: crece al salir (1,0 → 2,6), mastica y TRAGA ===
            float muerde = vivo && t >= T_FESTIN ? CicloMasticar(t) : 0f;
            float esc = MathHelper.Lerp(1.0f, 2.6f, MathHelper.SmoothStep(0f, 1f, viaje));
            esc *= 1f + 0.05f * muerde;                       // el bocado
            esc *= 1f + 0.10f * inhala;                       // el trago hincha
            esc = MathHelper.Lerp(esc, 1.0f, regreso);        // y encoge al volver
            float alfaLibro = MathHelper.Clamp(viaje * 2f, 0f, 1f) * (1f - 0f);

            // === EL CUERPO: el sprite del ítem + el párpado + el iris
            //     (el contrato del GrimorioNerviosoFlotante, a lo GRANDE) ===
            Texture2D tex = Terraria.GameContent.TextureAssets.Item[
                ModContent.ItemType<Weapons.GrimoireEternal>()].Value;
            Rectangle frame = new Rectangle(0, 0, tex.Width, tex.Height);
            Vector2 origen = frame.Size() * 0.5f;
            Vector2 pos = posMundo - Main.screenPosition;
            Color luz = Lighting.GetColor(posMundo.ToTileCoordinates());
            // v6.50.115 — EL LIBRO SE INCLINA hacia su víctima (está A UN
            // COSTADO y la MIRA): el vaivén de siempre + el sesgo del costado
            float rot = 0.09f * MathF.Sin(tiempo * 1.25f) - lado * 0.30f;

            // EL PÁRPADO: ABIERTO comiendo (Medio) — y a los 400 SE CIERRA
            // de golpe (Cerrado): el chasquido del festín
            bool cerrado = t >= T_CIERRE;
            bool comiendo = t >= T_FESTIN && !cerrado;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                Main.spriteBatch.Draw(tex, pos, frame, luz * alfaLibro, rot, origen, esc,
                    SpriteEffects.None, 0f);

                if (comiendo)
                {
                    // la boca: el párpado MEDIO — la página abierta hacia su comida
                    var parpado = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Weapons/GrimorioHambriento_Medio").Value;
                    Main.spriteBatch.Draw(parpado, pos, frame, luz * alfaLibro, rot, origen, esc,
                        SpriteEffects.None, 0f);

                    // LA MIRADA: el iris CLAVADO en su víctima (el preso del costado)
                    Vector2 d = centroJefe - posMundo;
                    Vector2 mirada = d.LengthSquared() > 4f
                        ? Vector2.Normalize(d) * new Vector2(3.5f, 2.8f) * 0.9f
                        : Vector2.Zero;
                    Vector2 alOjo = (Weapons.GrimorioHambrientoNervioso.OjoTexel + mirada - origen) * esc;
                    Vector2 posOjo = pos + alOjo.RotatedBy(rot);
                    var iris = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
                    Main.spriteBatch.Draw(iris, posOjo, null, luz * alfaLibro, rot,
                        iris.Size() * 0.5f, esc * Weapons.GrimorioHambrientoNervioso.IrisEscala,
                        SpriteEffects.None, 0f);
                }
                else if (cerrado)
                {
                    // CERRADO: el libro sellado con el festín DENTRO
                    var parpado = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado").Value;
                    Main.spriteBatch.Draw(parpado, pos, frame, luz * alfaLibro, rot, origen, esc,
                        SpriteEffects.None, 0f);
                }
                else
                {
                    // aún viaja: DESPIERTA — el iris abierto mira a su comida
                    Vector2 d = centroJefe - posMundo;
                    Vector2 mirada = d.LengthSquared() > 4f
                        ? Vector2.Normalize(d) * new Vector2(3.5f, 2.8f) * 0.9f
                        : Vector2.Zero;
                    Vector2 alOjo = (Weapons.GrimorioHambrientoNervioso.OjoTexel + mirada - origen) * esc;
                    Vector2 posOjo = pos + alOjo.RotatedBy(rot);
                    var iris = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
                    Main.spriteBatch.Draw(iris, posOjo, null, luz * alfaLibro, rot,
                        iris.Size() * 0.5f, esc * Weapons.GrimorioHambrientoNervioso.IrisEscala,
                        SpriteEffects.None, 0f);
                }
            }
            finally { Main.spriteBatch.End(); }

            // === EL HALO DEL LIBRO (lote VFX, mundo): la presencia violeta
            //     que se ENCIENDE al comer y ARDE al tragar ===
            VFXCore.Begin();
            try
            {
                float pulso = 0.14f + 0.06f * MathF.Sin(tiempo * 2.6f)
                    + 0.22f * exprime + 0.30f * inhala;
                VFXCore.Quad(posMundo, SombrasLib.Alfa(SombrasLib.Violeta, pulso * presencia),
                    new Vector2(150f * esc, 150f * esc));

                // LA HERIDA: la boca abierta brilla ROJA mientras come (la
                // digestión del preso cayendo adentro) — hacia el PRESO
                // (v6.50.115: el libro está al costado) — y el destello del
                // CHASQUIDO al cerrarse (la onda que lo sella)
                if (comiendo)
                {
                    Vector2 hacia = centroJefe - posMundo;
                    Vector2 dir = hacia.LengthSquared() > 4f ? Vector2.Normalize(hacia) : -Vector2.UnitY;
                    VFXCore.Quad(posMundo + dir * 24f * esc,
                        SombrasLib.Alfa(SombrasLib.Rojo, (0.35f + 0.25f * muerde) * presencia),
                        new Vector2(90f * esc, 60f * esc));
                }
                if (t >= T_CIERRE && t < T_CIERRE + 14f)
                {
                    float snap = 1f - (t - T_CIERRE) / 14f;
                    VFXCore.Quad(posMundo, SombrasLib.Alfa(new Color(255, 240, 200), 0.8f * snap),
                        new Vector2(220f * (1f + 1.2f * (1f - snap)), 220f * (1f + 1.2f * (1f - snap))));
                    if (t < T_CIERRE + 10f)
                        SombrasLib.OndaChoque(posMundo, 40f + 260f * (t - T_CIERRE) / 10f, 0.55f * snap);
                }
            }
            catch { }
            VFXCore.FlushAdditive();
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
