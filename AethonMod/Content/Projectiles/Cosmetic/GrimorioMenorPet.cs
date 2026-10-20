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
    /// GrimorioMenorPet — v6.50.122 — LA SEGUNDA MASCOTA: EL GRIMORIO
    /// DEL ETERNO EN MINIATURA (la letra del usuario que desenredó los
    /// conceptos: «la mascota normal es una pequeña representación del
    /// grimorio del eterno… este sí debe tener los mini minions, toda
    /// la mecánica de molestar npc y etc»).
    ///
    /// NO ES la criatura de luz: el Aethon Menor es el JEFE en
    /// miniatura y su repertorio es ALUMBRAR, ESTALLAR y FUSIONARSE con
    /// su gemela oscura. ESTE es el LIBRO en miniatura — y su repertorio
    /// es el del tomo hambriento: vigila, se aburre, viaja al pueblo y
    /// MOLESTA a los vecinos con sus 3 MINI OJOS (v6.50.125: LOS
    /// MISMOS OJOS DEL GRIMORIO NORMAL EN MINIATURA — el sprite real del
    /// Ojo Eterno del arma a escala 0,27, con su iris que mira y sus
    /// párpados de textura; la letra: «los mismos minion que el grimorio
    /// normal, o sea ojos, pero mas pequeños») y su SOMBRILLA DE PÁGINA
    /// que muerde (1 de daño sin acreditación, tope 10 golpes, emotes,
    /// aburrimiento, el AFK del dueño). TODA esa máquina vive en
    /// ParejaCerebro — el cerebro que nació en la .117 montado en la
    /// cría de luz y que la .122 devolvió a su dueño verdadero: el libro
    /// (véase AethonMenorPareja.cs).
    ///
    /// EL CUERPO: el sprite REAL del Eterno (la familia del ojo: el
    /// mismo cuerpo del Nervioso) a escala 0.52, con su OJO VIVO — mira
    /// a su dueño, parpadea a su compás y durante las misiones CLAVA la
    /// mirada en su víctima (el contrato del GrimorioNerviosoFlotante,
    /// en miniatura) — más su ANILLO RÚNICO OSCURO violeta y el
    /// polvillo de sombra. NO es mascota de luz: su brillo es la brasa
    /// tenue de sus páginas, no una lámpara.
    ///
    /// LA VIDA: el patrón vanilla de toda mascota (la lección .121):
    /// timeLeft = 2 A CADA TICK — la muerte la decide SOLO el buff
    /// (GrimorioMenorBuff), jamás el reloj.
    /// </summary>
    public class GrimorioMenorPet : ModProjectile
    {
        // === EL CEREBRO (la máquina de travesuras — ParejaCerebro) ===
        private readonly ParejaCerebro.Estado _cerebro = new ParejaCerebro.Estado();

        public override string Texture => "AethonMod/Content/Weapons/GrimorioHambrientoNervioso";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            Main.projPet[Type] = true;                    // mascota: persiste
            ProjectileID.Sets.NeedsUUID[Type] = true;     // la instancia única del dueño
            // SIN ProjectileID.Sets.LightPet — NO es mascota de luz (ese
            // trono es del Aethon Menor): el libro es la sombra que
            // acompaña, no la lámpara que guía.
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 28;
            Projectile.friendly = true;         // las mascotas son friendly (no daña: damage 0)
            Projectile.damage = 0;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;     // la sombra atraviesa el mundo
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;     // tML la sincroniza
        }

        public override void AI()
        {
            // === LA VIDA DE LA MASCOTA (el patrón vanilla): el buff manda ===
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead ||
                !duenio.HasBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.GrimorioMenorBuff>()))
            {
                Projectile.Kill();
                return;
            }

            // LA CURA DE LA .121 (el patrón vanilla de TODA mascota: Wisp,
            // Corazón Carmesí, Fuego Fatuo): el reloj se refresca A CADA
            // TICK — la muerte la decide SOLO el buff, jamás el timeLeft.
            Projectile.timeLeft = 2;

            // v6.50.124 — LA EDAD LOCAL (el teatro del DESPERTAR: los 3
            // mini minions BROTAN de las páginas en los primeros 36 t —
            // la leen PosicionOjos/DibujarExtras; localAI no viaja en el
            // cable y no hace falta: cada pantalla florece la suya).
            Projectile.localAI[0] += 1f;

            float t = Main.GlobalTimeWrappedHourly;

            // === EL CEREBRO: el AFK del dueño, el viaje al pueblo, LAS DOS
            //     FORMAS de molestar y el tope de 10 golpes (ParejaCerebro
            //     — TODO determinista, el contrato de la casa: el servidor
            //     y todos los clientes evolucionan IGUAL). ===
            ParejaCerebro.Tick(Projectile, duenio, _cerebro);

            // === LA PERSECUCIÓN SUAVE: el hombro del dueño — el LADO
            //     ESPEJO del Aethon Menor (si llevas las dos mascotas, la
            //     cría de luz pasea un hombro y el libro el otro: cada
            //     quien su sitio). ===
            float lado = MathF.Sin(t * 0.35f + MathHelper.Pi) * 40f + duenio.direction * 22f;
            float bob = MathF.Sin(t * 1.75f + 0.8f) * 8f;
            Vector2 meta = duenio.Center + new Vector2(lado, -38f + bob);
            // Si está de misión (viajar / molestar / volver), la meta la
            // decide EL CEREBRO: el vecino, su órbita, o el lado del dueño.
            ParejaCerebro.ElegirMeta(Projectile, duenio, _cerebro, ref meta);

            // La búsqueda serena… y LA MISIÓN VUELA: en viaje/molestia/
            // vuelta la persecución es DIRECTA — crucero ~12 px/t (el
            // mismo muelle dual de la .117). v6.50.129 — LA CURIOSIDAD
            // (MODO_CURIOSO) vuela con la SUAVIDAD del hombro: es un
            // paseo, no una misión (la figura de Lissajous pide muelle
            // gentil — el mismo de la vida serena).
            Vector2 hacia = meta - Projectile.Center;
            float dist = hacia.Length();
            if (_cerebro.Modo == ParejaCerebro.MODO_NORMAL ||
                _cerebro.Modo == ParejaCerebro.MODO_CURIOSO)
            {
                if (dist > 900f)            // el dueño se fue muy lejos: le alcanza volando
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, hacia * 0.05f, 0.20f);
                else
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, hacia * 0.045f, 0.10f);

                // El freno al llegar (flota, no vibra encima del punto).
                if (dist < 24f)
                    Projectile.velocity *= 0.80f;
            }
            else
            {
                Vector2 dir = dist > 0.001f ? hacia / dist : Vector2.Zero;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, dir * 12f, 0.10f);
                if (dist < 30f)
                    Projectile.velocity *= 0.85f;
            }

            // v6.50.131 — LA PIRUETA DEL ARRANQUE: al emprender UNA
            // MISIÓN (o la vuelta a casa) el libro celebra con UNA VUELTA
            // de campana y un chorro de chispas — el entusiasmo de las
            // páginas al volar.
            int modoAhora = (int)_cerebro.Modo;
            if (Projectile.localAI[1] <= 0f &&
                modoAhora != (int)Projectile.localAI[2] &&
                (modoAhora == (int)ParejaCerebro.MODO_VIAJAR ||
                 modoAhora == (int)ParejaCerebro.MODO_VOLVER))
            {
                Projectile.localAI[1] = 34f;
                for (int i = 0; i < 7; i++)
                {
                    Dust chispa = Dust.NewDustPerfect(Projectile.Center,
                        DustID.Shadowflame,
                        Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.2f, 3.4f),
                        140, new Color(196, 150, 255), 0.7f);
                    chispa.noGravity = true;
                }
            }
            Projectile.localAI[2] = modoAhora;
            if (Projectile.localAI[1] > 0f) Projectile.localAI[1] -= 1f;
            float pirueta = Projectile.localAI[1] > 0f
                ? MathHelper.TwoPi * (1f - Projectile.localAI[1] / 34f)
                  * (Projectile.whoAmI % 2 == 0 ? 1f : -1f)
                : 0f;

            // v6.50.130 — EL CINEO DEL VUELO (la letra: «mejora las
            // animaciones del pequeño minigrimorio mascota»): el libro ya
            // no se mece como un péndulo ajeno al mundo — se RECUESTA
            // hacia donde vuela (la inclinación nace de SU velocidad:
            // aceleró a la derecha, se inclinó a la derecha) y encima
            // conserva el vaivén perezoso de su familia (el Dormido).
            Projectile.rotation = MathHelper.Clamp(Projectile.velocity.X * 0.020f, -0.24f, 0.24f)
                + MathF.Sin(t * 1.2f) * 0.05f + pirueta;

            // === EL RESPLANDOR DE LAS PÁGINAS: una brasa violeta, TENUE
            //     (0.24/0.17/0.40 — menos de la quinta parte de la cría de
            //     luz): el libro NO es una lámpara; es el fulgor inquieto
            //     de sus páginas hambrientas. ===
            Lighting.AddLight(Projectile.Center, 0.24f, 0.17f, 0.40f);

            // === EL POLVILLO DE SOMBRA (el goteo del tomo — más ralo que
            //     el de la gemela oscura: el mini es pequeño).
            //     v6.50.130 — EL ALETEO DE PÁGINAS: cuando vuela RÁPIDO
            //     (misión, curiosidad) el goteo se vuelve CHISPA — motas
            //     claras que se quedan atrás (las páginas pasando solas). ===
            bool vuelaRapido = Projectile.velocity.Length() > 5.5f;
            if (!Main.dedServ && Main.rand.NextBool(vuelaRapido ? 9 : 26))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + new Vector2(Main.rand.NextFloat(-7f, 7f),
                        Main.rand.NextFloat(-7f, 7f)),
                    DustID.Shadowflame,
                    new Vector2(Main.rand.NextFloat(-0.3f, 0.3f),
                        Main.rand.NextFloat(-0.7f, -0.2f)) - Projectile.velocity * 0.12f,
                    128, new Color(110, 40, 170), 0.55f);
                d.noGravity = true;

                // LA PÁGINA SUELTA: una mota CLARA detrás del libro cuando
                // vuela de verdad (el rastro de hojas del viaje).
                if (vuelaRapido && Main.rand.NextBool(3))
                {
                    Dust hoja = Dust.NewDustPerfect(
                        Projectile.Center - Projectile.velocity * 1.6f,
                        DustID.Shadowflame,
                        new Vector2(Main.rand.NextFloat(-0.5f, 0.5f),
                            Main.rand.NextFloat(-0.8f, -0.2f)),
                        140, new Color(214, 198, 240), 0.65f);
                    hoja.noGravity = true;
                }
            }
        }

        // ==================================================================
        //  EL DIBUJADO — el cuerpo del Eterno en miniatura + el ojo vivo
        //  + los extras del cerebro (sombrilla y mini ojos, lote alfa;
        //  anillo rúnico oscuro e iris, lote aditivo). El contrato del
        //  bool de la casa: salir SIEMPRE con el lote vanilla ABIERTO.
        // ==================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // === v6.50.126 — EL SELECCIONADOR DE PERSONAJE (la letra: «el
            //     grimorio menor se sale del recuadro del personaje y no
            //     se ubica como las otras mascotas»): la vista previa de
            //     vanilla (UICharacter.DrawPets) crea un DUMMY de este
            //     proyectil (isAPreviewDummy) y lo dibuja con
            //     DrawProjDirect — PERO el dummy NUNCA corre la IA: su
            //     localAI[0] queda en 0 para siempre (el brote eterno: el
            //     anillo de nacimiento pegado al libro + los ojos en
            //     radio 0 apilados y cerrados) y la órbita de siempre se
            //     salía del recuadro de 59×58. LA VISTA PREVIA ES SU
            //     PROPIA ESCENA: el libro en su sitio (abajo a la derecha
            //     del personaje, como TODA mascota) + los tres ojos en
            //     corona compacta DESPIERTOS — y a los ~5 s… EL LIBRO SE
            //     ESCAPA DEL RECUADRO (el easteregg de la letra: sale a
            //     DEAMBULAR por toda la pantalla — lo dibuja
            //     SeleccionGrimorioSistema, sobre el logo y el cielo). ===
            if (Projectile.isAPreviewDummy)
                return PreDrawVistaPrevista();

            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 c = Projectile.Center;

                // === 1. LOS EXTRAS DEL CEREBRO, LOTE ALFA: la sombrilla de
                //     página bajo el vecino (FORMA 1) y los discos NEGROS de
                //     los 3 mini ojos — lo negro NO vive en el aditivo
                //     (negro = suma 0 = invisible, la lección de la .66). ===
                ParejaCerebro.DibujarExtrasAlfa(Projectile, _cerebro);

                // === 2. EL HALO VIOLETA (la brasa de las páginas — el pulso
                //     del flotante grande, a escala mini) ===
                float pulso = 0.16f + 0.07f * MathF.Sin(t * 2.6f);
                VFXCore.Quad(c, new Color(110, 40, 170, 255) * pulso, new Vector2(64f, 64f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(c, new Color(150, 90, 225, 255) * (pulso * 0.8f), new Vector2(30f, 30f), 0f, VFXCore.SoftGlow);

                // === 3. LOS EXTRAS DEL CEREBRO, LOTE ADITIVO: el anillo
                //     rúnico oscuro y el iris violeta de los mini ojos — se
                //     SUMAN al lote del cuerpo (un solo volcado). ===
                ParejaCerebro.DibujarExtrasAditivos(Projectile, _cerebro);

                if (VFXCore.QuadCount > 0)
                {
                    // El contrato de la casa (v6.50.50): volar el lote y
                    // REABRIR el vanilla — jamás salir con el lote cerrado.
                    VFXCore.FlushAdditive(null, true);
                    VFXCore.ReabrirLoteVanilla();
                }
                else
                {
                    VFXCore.ReabrirLoteVanilla();
                }

                // === 4. EL CUERPO: el sprite REAL del Eterno (la familia
                //     del ojo: el mismo cuerpo del Nervioso) a escala 0.52 —
                //     con su OJO VIVO y su párpado (el contrato del
                //     GrimorioNerviosoFlotante, en miniatura). ===
                DibujarElLibro(t);

                // === 5. v6.50.125 — LOS TRES OJOS DE VERDAD: los mismos
                //     minions del grimorio normal (los Ojos del Eterno — el
                //     sprite real 56×54 del arma) en MINIATURA (escala 0,27;
                //     el arma los usa a 0,375), con su iris que MIRA y sus
                //     párpados de textura — la letra: «los mismos minion
                //     que el grimorio normal, o sea ojos, pero mas
                //     pequeños». ===
                ParejaCerebro.DibujarLosTresOjos(Projectile, _cerebro);

                return false;   // el cuerpo SE dibuja aquí (con su ojo)
            }
            catch
            {
                try { VFXCore.ReabrirLoteVanilla(); } catch { }
                return false;
            }
        }

        /// <summary>EL LIBRO EN MINIATURA: el cuerpo del Eterno a 0.52, el
        /// párpado que parpadea a su compás (determinista — el reloj del
        /// mundo) y la mirada: a su dueño normalmente, CLAVADA en la
        /// víctima durante las misiones (la vigilancia es la mirada).</summary>
        private void DibujarElLibro(float t)
        {
            Texture2D tex = Terraria.GameContent.TextureAssets.Item[
                ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>()].Value;
            if (tex == null) return;
            Rectangle frame = new Rectangle(0, 0, tex.Width, tex.Height);
            Vector2 origen = frame.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Color luz = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
            // LA RESPIRACIÓN (v6.50.130): el libro está VIVO — su cuerpo
            // se hincha y deshincha suave (3,5 % con compás lento).
            const float esc = 0.52f;
            float escVivo = esc * (1f + 0.035f * MathF.Sin(t * 2.1f));

            // LA MIRADA: su dueño… salvo de misión — entonces CLAVADA en
            // la víctima (el mini grimorio acecha antes de morder).
            // v6.50.130 — Y EN CURIOSIDAD, EL PAISAJE: el ojo deambula
            // (figuras suaves alrededor: el libro MIRA el mundo, no solo
            // a su dueño — la letra de «mejora las animaciones»).
            Vector2 objetivo = Main.player[Projectile.owner].MountedCenter;
            if (_cerebro.NpcObjetivo >= 0 && _cerebro.NpcObjetivo < Main.maxNPCs)
            {
                NPC victima = Main.npc[_cerebro.NpcObjetivo];
                if (victima != null && victima.active) objetivo = victima.Center;
            }
            else if (_cerebro.Modo == ParejaCerebro.MODO_CURIOSO ||
                     _cerebro.Modo == ParejaCerebro.MODO_PLAGA)
            {
                objetivo = Projectile.Center + new Vector2(
                    MathF.Sin(t * 0.7f) * 120f,
                    MathF.Cos(t * 0.53f) * 70f - 30f);
            }
            Vector2 d = objetivo - Projectile.Center;
            Vector2 mirada = d.LengthSquared() > 4f
                ? Vector2.Normalize(d) * new Vector2(3.5f, 2.8f) * 0.9f
                : Vector2.Zero;

            Main.spriteBatch.Draw(tex, pos, frame, luz, Projectile.rotation, origen, escVivo,
                SpriteEffects.None, 0f);

            // EL PARPADEO (determinista — el reloj del mundo): cada ~5 s un
            // párpado de un cuarto de segundo (el libro sueña despierto).
            float parp = (t * 0.9f) % 5.2f;
            if (parp > 4.90f)
            {
                var parpado = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado").Value;
                Main.spriteBatch.Draw(parpado, pos, frame, luz, Projectile.rotation, origen, escVivo,
                    SpriteEffects.None, 0f);
            }
            else
            {
                // el texel del ojo, ROTADO con el libro (la convención .84)
                Vector2 alOjo = (global::AethonMod.Content.Weapons.GrimorioHambrientoNervioso.OjoTexel
                    + mirada - origen) * escVivo;
                Vector2 posOjo = pos + alOjo.RotatedBy(Projectile.rotation);

                var iris = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
                Main.spriteBatch.Draw(iris, posOjo, null, luz, Projectile.rotation,
                    iris.Size() * 0.5f, escVivo * global::AethonMod.Content.Weapons.GrimorioHambrientoNervioso.IrisEscala,
                    SpriteEffects.None, 0f);
            }
        }
        // ==================================================================
        //  v6.50.126 — LA VISTA PREVIA DEL SELECCIONADOR (el dummy de
        //  vanilla): el libro POSEA en su recuadro como cualquier mascota
        //  (bobbing del reloj del menú, ojo vivo que mira y parpadea) con
        //  sus tres ojos en CORONA COMPACTA (18 px — pegados al tomo, la
        //  letra de la órbita) DESPIERTOS desde el primer frame. NADA de
        //  lotes VFXCore ni anillos de nacimiento: el menú no es lugar
        //  para abrir lotes ajenos. A los ~5 s el libro SE VA (el sistema
        //  del easteregg toma el control — TickPreview devuelve false).
        // ==================================================================
        private bool PreDrawVistaPrevista()
        {
            try
            {
                float t = Main.GlobalTimeWrappedHourly;

                // EL LUGAR DE UI donde vanilla puso al dummy (position ya
                // viene sumándole screenPosition — restando vuelve al
                // espacio de pantalla del menú).
                Vector2 pos = Projectile.Center - Main.screenPosition;

                // EL RELOJ DEL EASTEREGG: < 5 s = posa en el recuadro;
                // >= 5 s = el libro DESAPARECE DE AQUÍ (se fue a vagar por
                // TODA la pantalla — SeleccionGrimorioSistema).
                if (!Systems.SeleccionGrimorioSistema.TickVistaPrevista(pos))
                    return false;

                // EL LIBRO EN MINIATURA (la misma receta del mundo, en el
                // lote corriente del menú — la luz es SUYA: en el menú no
                // hay mundo que ilumine).
                Texture2D tex = Terraria.GameContent.TextureAssets.Item[
                    ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>()].Value;
                if (tex != null)
                {
                    const float esc = 0.52f;
                    float bob = MathF.Sin(t * 1.75f + 0.8f) * 2.2f;
                    float giro = MathF.Sin(t * 1.2f) * 0.06f;
                    Vector2 centro = pos + new Vector2(0f, bob);
                    Rectangle frame = new Rectangle(0, 0, tex.Width, tex.Height);
                    Vector2 origen = frame.Size() * 0.5f;
                    Color luz = new Color(226, 220, 244);

                    Main.spriteBatch.Draw(tex, centro, frame, luz, giro, origen, esc,
                        SpriteEffects.None, 0f);

                    // EL OJO VIVO: mira de lado a lado + parpadea (el
                    // parpadeo con el overlay _Cerrado, como el mundo).
                    float parp = (t * 0.9f) % 5.2f;
                    if (parp > 4.90f)
                    {
                        var parpado = ModContent.Request<Texture2D>(
                            "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado").Value;
                        Main.spriteBatch.Draw(parpado, centro, frame, luz, giro, origen, esc,
                            SpriteEffects.None, 0f);
                    }
                    else
                    {
                        Vector2 mirada = new Vector2(MathF.Sin(t * 0.55f) * 2.6f,
                            MathF.Sin(t * 0.41f + 0.6f) * 1.6f);
                        Vector2 alOjo = (global::AethonMod.Content.Weapons.GrimorioHambrientoNervioso.OjoTexel
                            + mirada - origen) * esc;
                        Vector2 posOjo = centro + alOjo.RotatedBy(giro);
                        var iris = ModContent.Request<Texture2D>(
                            "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
                        Main.spriteBatch.Draw(iris, posOjo, null, luz, giro,
                            iris.Size() * 0.5f, esc * global::AethonMod.Content.Weapons.GrimorioHambrientoNervioso.IrisEscala,
                            SpriteEffects.None, 0f);
                    }
                }

                // LOS TRES OJOS EN CORONA COMPACTA (18 px — pegados al
                // libro, la misma familia del arma, DESPIERTOS): la
                // mascota se presenta ENTERA en su recuadro.
                Texture2D ojoAbierto = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Projectiles/OjoEterno_Abierto").Value;
                Texture2D ojoCerrado = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Projectiles/OjoEterno_Cerrado").Value;
                Texture2D irisOjo = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
                if (ojoAbierto != null && ojoCerrado != null && irisOjo != null)
                {
                    const float escOjo = 0.22f;
                    for (int i = 0; i < 3; i++)
                    {
                        float ang = t * 0.75f + i * MathHelper.TwoPi / 3f;
                        float radio = 17f + MathF.Sin(t * 0.9f) * 1.5f;
                        Vector2 p = pos + new Vector2(MathF.Cos(ang) * radio,
                            MathF.Sin(ang) * radio * 0.82f - 4f
                            + MathF.Sin(t * 1.6f + i * 2.4f) * 1.5f);

                        // el parpadeo propio de cada hermano (abierto casi
                        // siempre, cerrado un instante).
                        float parpOjo = (t * 1.15f + i * 2.61f) % 6.5f;
                        bool cerrado = parpOjo <= 0.26f;
                        Texture2D cuerpo = cerrado ? ojoCerrado : ojoAbierto;
                        Color luz = new Color(232, 226, 246);
                        Main.spriteBatch.Draw(cuerpo, p, null, luz, 0f,
                            cuerpo.Size() * 0.5f, escOjo, SpriteEffects.None, 0f);
                        if (!cerrado)
                        {
                            Main.spriteBatch.Draw(irisOjo,
                                p + new Vector2(0.7f, 2.3f) * escOjo + new Vector2(
                                    MathF.Sin(t * 0.5f + i) * 1.2f, 0.4f), null, luz, 0f,
                                irisOjo.Size() * 0.5f, 0.41f * escOjo,
                                SpriteEffects.None, 0f);
                        }
                    }
                }

                return false;   // el dibujado de vanilla del dummy NO corre
            }
            catch { return false; }
        }
    }
}
