using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Sombras
{
    /// <summary>
    /// CAIDADEVORADAPROJECTILE — v6.50.122 — LA PELÍCULA DE LA
    /// DEVORACIÓN DEL DOBLE CAÍDO.
    ///
    /// La letra del usuario: «el grimorio siempre cae, pero si tienes
    /// uno en el inventario, el del inventario hace lo mismo que le
    /// hace a los jefes en la primera muerte, se lo come y lo devora
    /// como si fuera un jefe» — el MISMO LENGUAJE de LA PRIMERA CENA
    /// (PrimeraCenaProjectile, la .115), en corto: los SELLOS RÚNICOS
    /// aprisionan al Grimorio Dormido… y LA SOMBRA DE LA PÁGINA sale
    /// DEL ETERNO DEL INVENTARIO (la mano del portador) y se lo come.
    ///
    /// LA COREOGRAFÍA (240 t ≈ 4 s — la AUTORIDAD cierra a los 200:
    /// GrimorioDormido llama a LaCaidaSistema.DevorarLibroCaido):
    ///   0-30   EL SELLO — la TRIPLE CORONA DE CONJURO (los anillos
    ///          rúnicos de los agujeros negros) y los ANILLOS RÚNICOS
    ///          de los soles se materializan alrededor del libro dormido.
    ///   30-55  LA CONTRACCIÓN — los aros se cierran y lo aprisionan.
    ///   55-90  EL DESPERTAR — el Eterno del inventario SALE de la mano
    ///          del portador y toma su puesto A UN COSTADO del doble
    ///          (crece ×1.55: este bocado es un postre, no un jefe).
    ///   90-170 EL FESTÍN — la sombra de la página brota del libro y
    ///          DEVORA al dormido (ColumnaViva + Masa + bruma + los
    ///          OJOS + la cabeza de bruma + el Devorador; el dormido
    ///          TIEMBLA y se apaga — eso lo dibuja su propia IA).
    ///   170-200 EL TRAGO — los anillos ENTEROS colapsan hacia la boca
    ///          de la sombra y la masa vuelve cargando al doble.
    ///   200    LA AUTORIDAD: la marca del alma + la VOZ del Eterno
    ///          (EcoRed, los PENSAMIENTOS) + el doble se marcha.
    ///   200-250 EL POSO — las últimas volutas y la onda final.
    ///
    /// ai[0] = whoAmI del Grimorio Dormido · ai[1] = whoAmI del
    /// portador · ai[2] = tick (netImportant: el reloj viaja solo).
    /// </summary>
    public class CaidaDevoradaProjectile : ModProjectile
    {
        // === LOS RELOJES (calzan con GrimorioDormido/LaCaidaSistema:
        //     el cierra a los 200 — la película respira con él) ===
        private const float T_CONTRACCION = 30f;
        private const float T_LIBRO = 55f;       // el Eterno despierta en la mano
        private const float T_LIBRO_LISTO = 90f; // y toma su puesto al costado
        private const float T_FESTIN = 90f;      // la boca ya está lista: come desde la llegada
        private const float T_TRAGO = 170f;
        private const float T_FIN = 200f;        // == el cierre de la AUTORIDAD
        private const float T_POSO = 250f;

        private NPC Libro => Projectile.ai[0] < 0f || Projectile.ai[0] >= Main.npc.Length
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
            Projectile.timeLeft = 300;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            NPC libro = Libro;

            // SIN DOBLE (la autoridad ya se lo comió): el poso y a casa.
            if (libro == null || !libro.active)
            {
                if (Projectile.timeLeft > 60) Projectile.timeLeft = 60;
                if (t >= T_POSO) { Projectile.Kill(); return; }
                return;
            }

            Projectile.Center = libro.Center;

            if (Main.netMode == NetmodeID.Server) return;   // el server no dibuja

            // EL POLVO DEL CINE: chispas de la digestión durante el festín
            // (el dormido se deshace en bruma hacia la boca de la sombra)
            if (t > T_FESTIN && t < T_TRAGO && Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(
                    libro.Center + new Vector2(Main.rand.NextFloat(-16f, 16f),
                        Main.rand.NextFloat(-20f, 8f)),
                    DustID.Shadowflame, new Vector2(0f, 1.2f), 128, default, 0.8f);
                d.noGravity = false;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            NPC libro = Libro;
            if (libro == null) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                DrawCine(libro);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        // ==================================================================
        //  LA PELÍCULA
        // ==================================================================

        private void DrawCine(NPC libro)
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = 617 + libro.whoAmI * 31;   // LA SEMILLA DEL FESTÍN (única por doble)
            float t = Projectile.ai[2];
            Player portador = Portador;

            Vector2 centroLibro = libro.Center;
            bool vivo = libro.active;

            // === LA SALA DE CINE: el velo de los bordes (presente desde el
            //     sello, se va con el poso) ===
            float presencia = vivo
                ? MathHelper.Clamp(t / 25f, 0f, 1f)
                : MathHelper.Clamp(1f - (t - T_FIN) / 50f, 0f, 1f);
            SombrasLib.Vignette(0.38f * presencia);

            // === LAS FASES ===
            float manifiesta = SombrasLib.DeGolpe(MathHelper.Clamp(t / T_CONTRACCION, 0f, 1f));
            float ciñe = MathHelper.Clamp((t - T_CONTRACCION) / 25f, 0f, 1f);
            float festin = t < T_FESTIN ? 0f
                : MathHelper.Clamp((t - T_FESTIN) / 70f, 0f, 1f);
            float inhala = MathHelper.Clamp((t - T_TRAGO) / 30f, 0f, 1f);
            float poso = !vivo ? MathHelper.Clamp((t - T_FIN) / 50f, 0f, 1f) : 0f;

            // EL VICE ES ÍNTIMO: el dormido es un libro, no un jefe — los
            // sellos lo agarran a la medida de su cuerpo pequeño.
            float radioLibro = 42f;

            // ================================================================
            //  ACTO 1 — EL CERCO: LA TRIPLE CORONA DE CONJURO (los anillos
            //  rúnicos de los AGUJEROS NEGROS, abrillantados) + LOS ANILLOS
            //  RÚNICOS de los soles, aprisionando al doble caído
            // ================================================================
            if (vivo && presencia > 0.02f)
            {
                // EL LATIDO del vice (más rápido conforme exprime)
                float latido = 1f + 0.06f * MathF.Sin(tiempo * (4f + 4f * festin));

                // el radio del vórtice de la corona: se CIÑE al doble (40-60)
                // y al TRAGO se lo lleva entero
                float rBase = MathHelper.Lerp(1.35f, 0.55f, ciñe);
                rBase = MathHelper.Lerp(rBase, 0.10f, inhala);
                float rCorona = radioLibro * rBase * latido;

                // durante EL TRAGO el cerco ENTERO se desliza a la boca de la
                // sombra (los anillos se lo comen con él)
                Vector2 boca = PosicionVivaDelLibro(portador, centroLibro, tiempo, t, vivo, inhala);
                Vector2 centroAros = Vector2.Lerp(centroLibro, boca, inhala);
                float alfaAros = presencia * (1f - 0.9f * inhala * inhala);
                Vector2 posPantalla = centroAros - Main.screenPosition;

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    // --- 1a. EL ABRILLANTADO (dorado + violeta pulsando
                    //     DETRÁS de la corona — ARDE al comer y al tragar) ---
                    float brillo = 0.15f + 0.09f * MathF.Sin(tiempo * 3.1f)
                        + 0.30f * festin + 0.25f * inhala;
                    float g = rCorona * 3.4f;
                    if (g > 10f)
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

                    // --- 1b. LA TRIPLE CORONA (el círculo LITERAL de los
                    //     vórtices — la rueda CORRE al comer) ---
                    if (rCorona > 3f && alfaAros > 0.04f)
                    {
                        float giro = 1f + 0.9f * festin;
                        OrbitaLib.CoronaConjuro(posPantalla, rCorona, tiempo * giro,
                            alfaAros * 1.10f, 1.6f);
                    }

                    // LA PLATAFORMA — el aro horizontal BAJO el doble (el
                    // disco que lo sostiene, respirando) — se desliza con los aros
                    float anchoDisco = radioLibro * MathHelper.Lerp(2.0f, 1.30f, ciñe) * (1f - 0.85f * festin);
                    if (anchoDisco > 10f)
                    {
                        Vector2 posDisco = posPantalla + new Vector2(0f, libro.height * 0.55f + 10f);
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

                // --- 1c. LOS ANILLOS RÚNICOS DE LOS SOLES (el emisor
                //     compartido de la casa, coords de MUNDO en lote VFX) ---
                float rRunico = radioLibro * MathHelper.Lerp(2.0f,
                    MathHelper.Lerp(1.25f, 0.12f, inhala), ciñe);
                if (rRunico > 3f && alfaAros > 0.04f)
                {
                    float spinUp = 1f + 1.6f * festin;   // la rueda corre al comer
                    VFXCore.Begin();
                    SigiloLib.SistemaAnillos(centroAros, rRunico, tiempo * spinUp, semilla,
                        3, 0f, 0f, alfaAros * (1f - inhala * 0.6f));
                    VFXCore.FlushAdditive(null, false);
                }
            }

            // ================================================================
            //  ACTO 2 — EL ETERNO DEL INVENTARIO: sale de la mano del
            //  portador (esc ×1.55 — este bocado es un POSTRE, no un
            //  jefe), toma su puesto A UN COSTADO del doble, INCLINADO
            //  hacia él y MIRÁNDOLO FIJO mientras come
            // ================================================================
            Vector2 posLibro = PosicionVivaDelLibro(portador, centroLibro, tiempo, t, vivo, inhala);
            if (t >= T_LIBRO)
            {
                DibujarElLibro(portador, centroLibro, posLibro, tiempo, t, vivo, festin, inhala);
            }

            // ================================================================
            //  ACTO 3 — LA SOMBRA DE LA PÁGINA: brota DEL ETERNO y DEVORA
            //  al dormido (el patrón .115, en corto)
            // ================================================================
            if (t >= T_FESTIN - 15f && presencia > 0.02f && t >= T_LIBRO)
            {
                float sale = SombrasLib.DeGolpe(MathHelper.Clamp((t - (T_FESTIN - 15f)) / 25f, 0f, 1f));
                float vivoF = vivo ? 1f - 0.75f * poso : 0.25f * (1f - poso);

                // LA RAÍZ: EL LIBRO del inventario (la sombra brota de sus
                // páginas abiertas)
                Vector2 haciaDoble = centroLibro - posLibro;
                Vector2 rumboRaiz = haciaDoble.LengthSquared() > 4f
                    ? Vector2.Normalize(haciaDoble) : Vector2.UnitY;
                Vector2 raiz = posLibro + rumboRaiz * 12f;

                // EL CHARCO bajo el libro (la sombra que gotea de las páginas)
                SombrasLib.Charco(raiz + new Vector2(0f, 26f), 40f * sale, 0.7f * vivoF, tiempo, semilla);

                // === EL CUERPO VIVO: libro → doble — y durante EL TRAGO la
                //     punta VUELVE al libro (la sombra se lleva la masa A CASA) ===
                float alcance = sale * 0.35f + festin * 0.65f;
                Vector2 destino = Vector2.Lerp(raiz + rumboRaiz * 34f, centroLibro, alcance);
                destino = Vector2.Lerp(destino, raiz + rumboRaiz * 14f, inhala * 0.85f);
                Vector2[] col = SombrasLib.ColumnaViva(raiz, destino, tiempo, semilla, 16, 0.20f, gancho: 0.45f);
                SombrasLib.Masa(col, 9f, 15f, 0.95f * vivoF, semilla, tiempo, grosorCentro: 40f);

                // === LA BRUMA DEL CUERPO (dos capas) ===
                SombrasLib.BrumaColumna(col, 42f, 0.50f * vivoF, tiempo, semilla, 12);
                SombrasLib.BrumaColumna(col, 28f, 0.40f * vivoF, tiempo, semilla + 53, 9);

                // === LOS OJOS: TODOS abiertos, TODOS mirando al doble ===
                SombrasLib.OjosDeMasa(col, centroLibro, (0.4f + 0.6f * festin) * vivoF, semilla, tiempo, 5);

                // === LA CABEZA ES BRUMA (la boca de la casa) ===
                Vector2 punta = col[col.Length - 1];
                Vector2 rumbo = vivo
                    ? (centroLibro - punta).SafeNormalize(Vector2.UnitX)
                    : (raiz - punta).SafeNormalize(-Vector2.UnitY);
                float muerde = vivo && t >= T_FESTIN ? CicloMasticar(t) : 0.4f;
                float vigor = MathHelper.Clamp(0.55f + 0.45f * muerde + 0.25f * inhala, 0f, 1f) * vivoF;
                SombrasLib.CabezaDeBruma(punta, rumbo, vigor, vivoF, tiempo, semilla);

                // === EL DEVORADOR — LA CUBIERTA TOTAL que crece con el
                //     festín y SE INHALA entera durante el trago ===
                if (vivo)
                {
                    float intensidad = MathHelper.Clamp(festin * 0.5f
                        + MathHelper.Clamp((t - T_FESTIN - 60f) / 60f, 0f, 1f) * 0.5f, 0f, 1f);
                    SombrasLib.Devorador(punta, libro, intensidad, vivoF, tiempo, semilla, col, trago: inhala);

                    // LA HERIDA: donde la punta ENTERRÓ brilla rojo (digestión)
                    VFXCore.Begin();
                    VFXCore.Quad(punta, SombrasLib.Alfa(SombrasLib.Rojo, (0.42f + 0.20f * inhala) * vivoF),
                        new Vector2(80f, 80f), rumbo.ToRotation());
                    VFXCore.FlushAdditive();
                }

                // === LA CORRIENTE DEL TRAGO — la bruma vuelve por el CUERPO
                //     (la digestión corre hacia el libro) ===
                if (inhala > 0.05f || !vivo)
                {
                    float flujo = MathHelper.Max(inhala, vivo ? 0f : 1f - poso);
                    for (int k = 0; k < 4; k++)
                    {
                        float prog = SombrasLib.Frac(tiempo * 0.8f + k * 0.25f);
                        float idxF = (1f - prog) * (col.Length - 1);
                        int idx = Math.Min((int)idxF, col.Length - 2);
                        if (idx < 0) idx = 0;
                        Vector2 pos = Vector2.Lerp(col[idx], col[idx + 1], SombrasLib.Frac(idxF))
                            + new Vector2(MathF.Sin(k * 2.1f + tiempo * 3f) * 12f, -7f);
                        SombrasLib.Bruma(pos, 22f + 12f * SombrasLib.Frac(k * 0.73f),
                            0.50f * flujo * vivoF, tiempo, semilla + k * 7,
                            new Vector2(0f, -22f * flujo));
                    }
                }
            }

            // === EL POSO — la última voluta y la onda final ===
            if (!vivo)
            {
                float fade = 1f - poso;
                SombrasLib.Bruma(Projectile.Center, 90f * (0.6f + 0.4f * fade), 0.7f * fade, tiempo, semilla,
                    new Vector2(MathF.Sin(tiempo * 0.7f) * 20f, -12f));
                if (t < T_FIN + 10f)
                    SombrasLib.OndaChoque(Projectile.Center, 50f + 200f * (t - T_FIN + 10f) / 10f, 0.5f * fade);
            }
        }

        // ==================================================================
        //  LA POSICIÓN VIVA DEL LIBRO (compartida): el viaje mano → el
        //  ancla AL COSTADO del doble, el vaivén sereno (y el temblor al
        //  masticar), y EL REGRESO al poso. El dibujo del libro Y la
        //  raíz de la sombra leen ESTA posición.
        // ==================================================================
        private Vector2 PosicionVivaDelLibro(Player portador, Vector2 centroLibro,
            float tiempo, float t, bool vivo, float inhala)
        {
            int lado = portador != null && portador.active && portador.direction != 0
                ? portador.direction : 1;
            // A UN COSTADO del doble, del lado QUE MIRA el portador (lo ve
            // de perfil comiendo — el puesto de la .115)
            Vector2 ancla = centroLibro + new Vector2(-lado * 92f, -10f);
            Vector2 raiz = portador != null && portador.active
                ? portador.MountedCenter + new Vector2(portador.direction * 12f, 4f)
                : centroLibro + new Vector2(-lado * 160f, -40f);

            float viaje = MathHelper.Clamp((t - T_LIBRO) / (T_LIBRO_LISTO - T_LIBRO), 0f, 1f);
            float regreso = vivo ? 0f : MathHelper.Clamp((t - (T_FIN + 5f)) / 40f, 0f, 1f);

            Vector2 pos = Vector2.Lerp(raiz, ancla, MathHelper.SmoothStep(0f, 1f, viaje));
            pos = Vector2.Lerp(pos, raiz, MathHelper.SmoothStep(0f, 1f, regreso));
            // el vaivén sereno del flotante (y un temblor al masticar)
            float muerde = vivo && t >= T_FESTIN ? CicloMasticar(t) : 0f;
            pos += new Vector2(MathF.Sin(tiempo * 1.1f) * 3f,
                MathF.Sin(tiempo * 1.4f) * 2.5f - muerde * 2f);
            return pos;
        }

        // ==================================================================
        //  EL LIBRO — el sprite REAL del Eterno (la familia del ojo),
        //  saliendo de la mano del portador, INCLINADO hacia su comida,
        //  ABIERTO y comiendo — y al final VUELVE a la mano
        // ==================================================================
        private void DibujarElLibro(Player portador, Vector2 centroLibro, Vector2 posMundo,
            float tiempo, float t, bool vivo, float festin, float inhala)
        {
            float viaje = MathHelper.Clamp((t - T_LIBRO) / (T_LIBRO_LISTO - T_LIBRO), 0f, 1f);
            float regreso = vivo ? 0f : MathHelper.Clamp((t - (T_FIN + 5f)) / 40f, 0f, 1f);

            // === LA ESCALA: crece al salir (0.62 → 1.55 — POSTRE, no
            //     jefe), mastica y TRAGA ===
            float muerde = vivo && t >= T_FESTIN ? CicloMasticar(t) : 0f;
            float esc = MathHelper.Lerp(0.62f, 1.55f, MathHelper.SmoothStep(0f, 1f, viaje));
            esc *= 1f + 0.05f * muerde;                       // el bocado
            esc *= 1f + 0.10f * inhala;                       // el trago hincha
            esc = MathHelper.Lerp(esc, 0.62f, regreso);       // y encoge al volver
            float alfaLibro = MathHelper.Clamp(viaje * 2f, 0f, 1f);

            Texture2D tex = Terraria.GameContent.TextureAssets.Item[
                ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>()].Value;
            Rectangle frame = new Rectangle(0, 0, tex.Width, tex.Height);
            Vector2 origen = frame.Size() * 0.5f;
            Vector2 pos = posMundo - Main.screenPosition;
            Color luz = Lighting.GetColor(posMundo.ToTileCoordinates());

            int lado = portador != null && portador.active && portador.direction != 0
                ? portador.direction : 1;
            // EL LIBRO SE INCLINA hacia su comida (está A UN COSTADO y la MIRA)
            float rot = 0.09f * MathF.Sin(tiempo * 1.25f) + lado * 0.30f;

            bool comiendo = vivo && t >= T_FESTIN;

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
                    var medio = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Weapons/GrimorioHambriento_Medio").Value;
                    Main.spriteBatch.Draw(medio, pos, frame, luz * alfaLibro, rot, origen, esc,
                        SpriteEffects.None, 0f);
                }
                else
                {
                    // EL OJO: clavado en el doble (la mirada fija de la .115)
                    Vector2 d = centroLibro - posMundo;
                    Vector2 mirada = d.LengthSquared() > 4f
                        ? Vector2.Normalize(d) * new Vector2(3.5f, 2.8f) * 0.9f
                        : Vector2.Zero;
                    Vector2 alOjo = (global::AethonMod.Content.Weapons.GrimorioHambrientoNervioso.OjoTexel
                        + mirada - origen) * esc;
                    Vector2 posOjo = pos + alOjo.RotatedBy(rot);

                    var iris = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
                    Main.spriteBatch.Draw(iris, posOjo, null, luz * alfaLibro, rot,
                        iris.Size() * 0.5f,
                        esc * global::AethonMod.Content.Weapons.GrimorioHambrientoNervioso.IrisEscala,
                        SpriteEffects.None, 0f);
                }
            }
            finally { Main.spriteBatch.End(); }
        }

        /// <summary>El compás del bocado (el mismo de la casa): abre…
        /// ¡CIERRA!, mastica, reabre — cada 32 t.</summary>
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
