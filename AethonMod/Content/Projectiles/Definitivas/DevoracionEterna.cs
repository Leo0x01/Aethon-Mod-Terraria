using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Systems;
using AethonMod.Content.VFX;
using static Terraria.ModLoader.ModContent;

namespace AethonMod.Content.Projectiles.Definitivas
{
    // ======================================================================
    //  DevoracionEterna — v6.118 — EL FESTÍN DEL PORTADOR.
    //
    //  LA LETRA DEL USUARIO: «La animación de la devoración esta mal
    //  hecha, la animacion correcta seria la misma que usa el grimorio
    //  para jefe, asi que borra esta animacion donde el libro se parte,
    //  esta mal, lo que debes hacer es usar la misma animacion que el
    //  grimorio usa para devorar a los jefes y solo enviar al jugador al
    //  nuevo mundo cuando el jugador entre a la boca del grimorio».
    //
    //  LA CURA v6.118 — EL CALCO DEL CINE DE LOS JEFES: LAS DOS TAPAS DE
    //  LA v6.117 MURIERON (el libro partido por el lomo, el disco del
    //  vacío con su aro dorado — todo el andamiaje borrado). La
    //  devoración del portador ES AHORA LA MISMA PELÍCULA DEL FESTÍN DE
    //  `PrimeraCenaProjectile` — la letra exacta del calco:
    //   · EL LIBRO DE VERDAD (DibujarElLibro del festín): el sprite REAL
    //     del Grimorio del Eterno que CRECE ×2,6, se inclina hacia su
    //     presa, abre EL PÁRPADO MEDIO mientras come — y SU IRIS DORADO
    //     MIRA FIJO AL PORTADOR con el recorte que respira al masticar
    //     (CicloMasticar) — al tragárselo, EL LIBRO SE CIERRA DE UN
    //     GOLPE (el párpado Cerrado + el chasquido + la onda).
    //   · LA SOMBRA DE LA PÁGINA (la cadena SombrasLib del festín):
    //     el charco bajo el libro, EL CUERPO VIVO (ColumnaViva + Masa
    //     fusiforme), DOS CAPAS de bruma de columna, LOS OJOS que se
    //     abren a lo largo del cuerpo y MIRAN a la presa, LA CABEZA DE
    //     BRUMA (la boca moderna) y EL DEVORADOR — la cobertura total
    //     (la elipse del CUERPO DEL JUGADOR, la nueva sobrecarga Player
    //     de SombrasLib) que lo envuelve y SE LO INHALA.
    //   · EL CERCO: LA TRIPLE CORONA DE CONJURO (OrbitaLib) aprisiona al
    //     portador + LOS ANILLOS RÚNICOS DE LOS SOLES (SigiloLib) — y al
    //     trago ENTEROS se deslizan DENTRO de la boca del libro.
    //   · LA CORRIENTE Y LAS ALMAS: la bruma corre del preso a la boca y
    //     las almas (ojos blancos) vuelan al libro.
    //
    //  LA PUERTA VIVE EN LA BOCA: el viaje YA NO lo dispara un
    //  temporizador — SOLO la COLISIÓN del jugador con el RECTÁNGULO DE
    //  LA BOCA del grimorio (el patrón Hitbox.Intersects de las sombras
    //  de página de la casa). El portador ES ARRASTRADO (controles
    //  congelados + el lerp espiral de siempre, hacia la BOCA) y al
    //  ENTRAR: el estallido de páginas, el rugido, EL MUNDO SE LLENA DE
    //  BRUMA (el faro de OnPostDraw tapa también la carga) y la puerta
    //  gira detrás de la inundación llena.
    //
    //  LO QUE SE CONSERVA (si funciona, no se toca): la red de
    //  seguridad del OnKill (nadie invisible ni atrapado), Tragado →
    //  hideEntirePlayer (ShardPlayer.ModifyDrawInfo), el faro
    //  (SubmundoGrimorio.BrumaFaro) y su retirada de 0,7 s al llegar.
    // ======================================================================
    public class DevoracionEterna : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        // === LOS TIEMPOS DE LA CASA (el festín del portador, ~4 s) ===
        private const int TNace = 26;        // el libro despierta y CRECE
        private const int TSello = 30;       // el cerco cierra sobre la presa
        private const int TSombra = 40;      // la sombra de la página brota
        private const int TFestin = 60;      // la boca mastica desde la llegada
        private const int TDesiste = 300;    // la red: sin trago, se disuelve
        private const int TTrasTragoPuerta = 16;  // tras ENTRAR: la puerta gira
        private const int TTrasTragoMuerte = 74;   // tras ENTRAR: el poso muere
        // v6.124 — LA CACERÍA INVERSA (la letra: «acabo de ver que para que
        // el jugador entre al mundo interior debe tocar el grimorio mientras
        // es deborado, bueno muchas veces el jugador no es atraido
        // correctamente al grimorio, en este caso para que no hayan errores,
        // si por 2 segundos el jugador no es atraido, el grimorio volara
        // hacia el jugador y con esto se soluciona el problema de que a
        // veces no entra al mundo interior»): DOS SEGUNDOS de paciencia —
        // pasados 120 ticks de arrastre SIN trago, el libro DEJA SU ANCLA
        // Y VUELA hacia su presa: la boca llega hasta el jugador y el
        // trago NUNCA falla.
        private const int TVueloLibro = 120; // 2 s de arrastre sin trago → el libro VUELA

        /// <summary>
        /// ¿El portador ya está DENTRO de la boca? (la casa lo consulta en
        /// ShardPlayer.ModifyDrawInfo para ocultarlo del dibujado: la presa
        /// tragada no flota sobre el libro cerrado).
        /// </summary>
        public static bool Tragado;

        // === LAS TEXTURAS DEL FESTÍN (la familia del ojo — las mismas del
        //     cine de jefes, NADA de libros partidos) ===
        private static Texture2D _texLibro;     // el cuerpo: el ítem Grimorio del Eterno
        private static Texture2D _texMedio;     // el párpado MEDIO (comiendo)
        private static Texture2D _texCerrado;   // el párpado CERRADO (el portazo)
        private static Texture2D _texIris;      // el iris dorado (la mirada)
        private static bool _texturasProbadas;

        private bool _entregado;
        private bool _estallo;          // EL TRAGO ya ocurrió
        private float _tTrago;          // el tick exacto del trago
        private float _disuelve;        // la red de despedida (sin trago)

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 120;
            Projectile.height = 200;
            Projectile.friendly = false;      // SIN DAÑO — la letra
            Projectile.damage = 0;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TDesiste + TTrasTragoMuerte + 10;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        /// <summary>EL LIBRO: anclado sobre el punto del uso, meciéndose.</summary>
        private Vector2 PosLibro => Projectile.Center + new Vector2(0f, -140f) +
            new Vector2(0f, MathF.Sin(Main.GlobalTimeWrappedHourly * 1.6f) * 5f);

        /// <summary>
        /// LA BOCA DEL GRIMORIO (la fórmula del festín de jefes): el punto
        /// a 14 px del libro, ENFRENTE de la presa — ahí vuelan las almas
        /// y ahí espera EL RECTÁNGULO que traga.
        /// </summary>
        private Vector2 BocaDelLibro(Vector2 presa)
        {
            Vector2 d = presa - PosLibro;
            return PosLibro + (d.LengthSquared() > 4f ? Vector2.Normalize(d) : Vector2.UnitY) * 14f;
        }

        /// <summary>EL RECTÁNGULO DE LA BOCA (el gatillo del viaje): generoso
        /// a la escala del libro crecido (~×2,2) — la presa ENTRA, no roza.</summary>
        private Rectangle RectBoca(Vector2 boca, float esc)
        {
            float lado = MathHelper.Clamp(64f * (esc / 2.2f), 52f, 84f);
            return new Rectangle((int)(boca.X - lado * 0.5f), (int)(boca.Y - lado * 0.5f),
                (int)lado, (int)lado);
        }

        public override void AI()
        {
            float t = Projectile.ai[0];
            Projectile.ai[0] = t + 1f;
            Projectile.velocity = Vector2.Zero;

            Player p = Main.player[Projectile.owner];
            bool dueñoVivo = p != null && p.active && !p.dead;
            Vector2 libro = PosLibro;

            // === LA RED DEL DESISTIMIENTO: sin trago a los TDesiste, el
            //     grimorio SE DISUELVE (nadie viaja sin ENTRAR a la boca —
            //     la letra) y el OnKill devuelve controles y limpieza ===
            if (_disuelve > 0f)
            {
                _disuelve += 1f / 30f;
                if (_disuelve >= 1f)
                {
                    Projectile.Kill();
                    return;
                }
            }
            else if (t >= TDesiste)
            {
                _disuelve = 0.01f;
            }

            // === EL PORTADOR ES PRESA (controles fuera + EL ARRASTRE) ===
            if (dueñoVivo && p.whoAmI == Main.myPlayer && _disuelve <= 0f)
            {
                // best effort: el lerp de POSICIÓN es el que manda
                p.controlLeft = false;
                p.controlRight = false;
                p.controlUp = false;
                p.controlDown = false;
                p.controlJump = false;
                p.controlUseItem = false;
                p.controlUseTile = false;

                if (Projectile.ai[0] >= TSombra && !_estallo)
                {
                    // EL ARRASTRE: cada vez más decidido hacia LA BOCA (no
                    // hacia el libro: la presa entra por la boca), con el
                    // vaivén lateral que se apaga al llegar (la espiral de
                    // siempre — la presa cae vencida, no teletransportada).
                    float f = MathHelper.Clamp((Projectile.ai[0] - TSombra) / 110f, 0f, 1f);
                    float k = 0.045f + 0.075f * f * f;
                    Vector2 boca = BocaDelLibro(p.Center);
                    Vector2 vaivén = new Vector2(
                        MathF.Sin(Projectile.ai[0] * 0.22f) * 34f * (1f - f * 0.7f), 0f);
                    try
                    {
                        p.Center = Vector2.Lerp(p.Center, boca + vaivén, k);
                        p.velocity = Vector2.Zero;
                        p.gravDir = 1f;
                        p.maxFallSpeed = 0f;

                        // EL GIRO DE LA PRESA (Player.fullRotation de
                        // vanilla): nace suave y ACELERA con la cercanía.
                        p.fullRotationOrigin = p.Center - p.position; // el eje: el centro del cuerpo
                        p.fullRotation += (0.055f + 0.13f * f * f) * (f > 0.55f ? 1.6f : 1f);
                    }
                    catch { }

                    // ==========================================================
                    //  v6.124 — LA CACERÍA INVERSA (la letra: «si por 2
                    //  segundos el jugador no es atraido, el grimorio
                    //  volara hacia el jugador»): pasados 120 ticks de
                    //  arrastre SIN trago, el libro ABANDONA su ancla y
                    //  VUELA hasta su presa — la boca cae ENCIMA del
                    //  jugador y el rectángulo del trago NUNCA falla (la
                    //  cura de la entrada intermitente al mundo interior).
                    // ==========================================================
                    if (Projectile.ai[0] >= TSombra + TVueloLibro)
                    {
                        Vector2 posLibro = PosLibro;
                        Vector2 hacia = p.Center - posLibro;
                        float dist = hacia.Length();
                        Vector2 dirLibro = dist > 0.01f ? hacia / dist : Vector2.UnitY;

                        // EL OBJETIVO DEL CENTRO: la BOCA del libro PUESTA
                        // EN el jugador (la boca vive a 14 px del libro,
                        // hacia la presa — y PosLibro es Center+(0,−140)).
                        Vector2 centroObjetivo = p.Center - dirLibro * 14f + new Vector2(0f, 140f);

                        // EL VUELO: arranca suave y se decide (0.05 → 0.22
                        // en ~45 t — el libro cae sobre su presa).
                        float v = MathHelper.Clamp(
                            (Projectile.ai[0] - (TSombra + TVueloLibro)) / 45f, 0f, 1f);
                        float kv = 0.05f + 0.17f * v * v;
                        Projectile.Center = Vector2.Lerp(Projectile.Center, centroObjetivo, kv);

                        // EL DESPEGUE: el aleteo del libro que rompe su
                        // ancla (UNA sola vez) + la estela de páginas.
                        if (Projectile.ai[0] == TSombra + TVueloLibro && !Main.dedServ)
                        {
                            Terraria.Audio.SoundEngine.PlaySound(
                                Terraria.ID.SoundID.Item113.WithPitchOffset(-0.4f), posLibro);
                            OndaLib.Kick(3f, 10);
                        }
                        if (!Main.dedServ && Main.netMode != 2 && Main.rand.NextBool(2))
                        {
                            Dust d = Dust.NewDustPerfect(posLibro + Main.rand.NextVector2Circular(30f, 22f),
                                Terraria.ID.DustID.Enchanted_Gold,
                                -dirLibro * Main.rand.NextFloat(1.5f, 4f) +
                                new Vector2(0f, Main.rand.NextFloat(-0.6f, 0.6f)),
                                180, new Color(255, 226, 150), Main.rand.NextFloat(0.7f, 1.2f));
                            d.noGravity = true;
                            d.fadeIn = 0f;
                        }
                    }

                    // === LA PUERTA VIVE EN LA BOCA: SOLO la colisión del
                    //     jugador con el rectángulo de la boca dispara el
                    //     trago (la letra: «solo enviar al jugador al nuevo
                    //     mundo cuando el jugador entre a la boca del
                    //     grimorio») — el patrón Hitbox.Intersects de las
                    //     sombras de página de la casa ===
                    float esc = EscalaDelLibro(Projectile.ai[0], 0f);
                    Rectangle rectBoca = RectBoca(boca, esc);
                    if (rectBoca.Intersects(p.Hitbox) && !_estallo)
                    {
                        _estallo = true;
                        _tTrago = Projectile.ai[0];
                        try
                        {
                            p.Center = boca;        // el último bocado: al centro de la boca
                            p.velocity = Vector2.Zero;
                            p.fullRotation = 0f;
                        }
                        catch { }
                        Tragado = true;            // la presa desaparece DENTRO
                        if (Main.netMode != 2)
                        {
                            for (int i = 0; i < 36; i++)
                            {
                                float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                                Dust d = Dust.NewDustPerfect(boca,
                                    i % 3 == 0 ? DustID.Enchanted_Pink : DustID.GoldFlame,
                                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Main.rand.NextFloat(2.0f, 6.5f),
                                    180, i % 3 == 0 ? new Color(190, 150, 240) : new Color(255, 230, 160), 1.1f);
                                d.noGravity = true;
                                d.fadeIn = 0f;
                            }
                            OndaLib.Kick(9);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, boca);
                        }
                    }
                }
            }

            // === EL POLVO DE ORO DEL SUELO (el descenso del libro) ===
            if (Projectile.ai[0] < TNace + 12 && !Main.dedServ && Main.netMode != 2)
            {
                if (Main.rand.NextBool(2))
                {
                    Vector2 pos = Projectile.Center + new Vector2(
                        Main.rand.NextFloat(-44f, 44f), Main.rand.NextFloat(-4f, 10f));
                    Dust d = Dust.NewDustPerfect(pos, DustID.Enchanted_Gold,
                        new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -Main.rand.NextFloat(1.4f, 3.2f)),
                        160, new Color(255, 214, 130), 0.9f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
            }

            // === LA ESPIRAL DE LA DEVORACIÓN: páginas y esencias SUBEN de
            //     la presa a la boca (la corriente del festín) ===
            if (Projectile.ai[0] >= TSombra && !_estallo && !Main.dedServ && Main.netMode != 2 &&
                dueñoVivo && _disuelve <= 0f)
            {
                if (Main.rand.NextBool(2))
                {
                    Vector2 boca = BocaDelLibro(p.Center);
                    Vector2 desde = p.Center + Main.rand.NextVector2Circular(26f, 34f);
                    Vector2 tang = new Vector2(-(boca - desde).Y, (boca - desde).X);
                    Vector2 vel = (boca - desde) * 0.045f + tang * 0.016f;
                    Dust d = Dust.NewDustPerfect(desde,
                        Main.rand.NextBool(3) ? DustID.Enchanted_Pink : DustID.GoldFlame,
                        vel, 170,
                        Main.rand.NextBool(3) ? new Color(190, 150, 240) : new Color(255, 230, 160),
                        Main.rand.NextFloat(0.8f, 1.3f));
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
                // el temblor del conjuro crece con la cercanía.
                if (Main.rand.NextBool(5))
                {
                    float f = MathHelper.Clamp((Projectile.ai[0] - TSombra) / 110f, 0f, 1f);
                    OndaLib.Kick(2 + (int)(5f * f));
                }
            }

            // === LA BRUMA INUNDA (solo en la animación — la letra): el
            //     faro sube CON LA CERCANÍA de la boca y, tras el trago,
            //     termina de llenarse PARA la puerta (la pantalla de carga
            //     vive ESCONDIDA detrás — el contrato de PintarFaroDevoracion) ===
            if (_estallo)
            {
                float f = MathHelper.Clamp((Projectile.ai[0] - _tTrago) / (float)TTrasTragoPuerta, 0f, 1f);
                SubmundoGrimorio.BrumaFaro = Math.Max(SubmundoGrimorio.BrumaFaro, f * f * (3f - 2f * f));
            }
            else if (dueñoVivo && Projectile.ai[0] >= TSombra)
            {
                // la inundación se anuncia mientras la presa se acerca
                Vector2 boca = BocaDelLibro(p.Center);
                float dist = Vector2.Distance(p.Center, boca);
                float cercania = MathHelper.Clamp(1f - dist / 260f, 0f, 1f);
                SubmundoGrimorio.BrumaFaro = Math.Max(SubmundoGrimorio.BrumaFaro, cercania * 0.42f);
            }

            // === LA PUERTA GIRA (detrás de la bruma llena) — SOLO tras el
            //     trago (la colisión con la boca ya ocurrió) ===
            if (_estallo && !_entregado && Projectile.ai[0] >= _tTrago + TTrasTragoPuerta)
            {
                _entregado = true;
                if (dueñoVivo)
                    SubmundoGrimorio.Entrar(p);
            }

            if (Projectile.ai[0] >= _tTrago + TTrasTragoMuerte && _estallo)
                Projectile.Kill();
        }

        /// <summary>
        /// v6.117 — LA RED DE SEGURIDAD DEL TRAGO: si el proyectil muere
        /// SIN entregar la puerta (otro mod lo mató, el mundo se cayó,
        /// lo que sea), la inundación y la presa-oculta se liberan —
        /// nadie queda invisible ni atrapado en la bruma.
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            if (!_entregado)
            {
                Tragado = false;
                SubmundoGrimorio.BrumaFaro = 0f;
                try
                {
                    if (Main.myPlayer >= 0 && Main.player[Main.myPlayer] != null)
                        Main.player[Main.myPlayer].fullRotation = 0f;
                }
                catch { }
            }
        }

        // ==================================================================
        //  EL DIBUJO — LA PELÍCULA DEL FESTÍN (el calco de PrimeraCena)
        // ==================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == 2) return false;
            try
            {
                if (!AsegurarTexturas()) return false;
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    DrawFestin();
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.ReabrirLoteVanilla();
            }
            catch { }
            return false;
        }

        /// <summary>La escala del libro (el crecimiento del festín: 1,0 → 2,6).</summary>
        private static float EscalaDelLibro(float t, float muerde)
        {
            float crece = MathHelper.Clamp((t - TNace * 0.5f) / 55f, 0f, 1f);
            float esc = MathHelper.Lerp(1.1f, 2.6f, MathHelper.SmoothStep(0f, 1f, crece));
            esc *= 1f + 0.05f * muerde;       // el bocado
            return esc;
        }

        private void DrawFestin()
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            float t = Projectile.ai[0];
            Player p = Main.player[Projectile.owner];
            bool vivo = p != null && p.active && !p.dead;
            Vector2 centroPresa = vivo ? p.Center : Projectile.Center;
            int semilla = 911 + Projectile.owner * 13 + Projectile.whoAmI * 7;  // única por festín

            // === LA DESPEDIDA (la red): el disolverse sin trago ===
            float vivoF = _disuelve > 0f ? 1f - _disuelve : 1f;
            float presencia = MathHelper.Clamp(t / 24f, 0f, 1f) * vivoF;

            // === LA SALA DE CINE: el velo de los bordes ===
            SombrasLib.Vignette(0.42f * presencia);

            // === LAS FASES (el calendario del festín) ===
            float manifiesta = SombrasLib.DeGolpe(MathHelper.Clamp(t / TSello, 0f, 1f)) * presencia;
            float ciñe = MathHelper.Clamp((t - TSello) / 22f, 0f, 1f);
            float muerde = t >= TFestin ? CicloMasticar(t) : 0.35f;
            float inhala = _estallo
                ? MathHelper.Clamp((t - _tTrago) / 26f, 0f, 1f)
                : 0f;
            float poso = _estallo
                ? MathHelper.Clamp((t - _tTrago - 26f) / 48f, 0f, 1f)
                : 0f;
            vivoF = _estallo ? (1f - 0.7f * poso) * presencia : vivoF;

            float radioPresa = 64f;   // la corona del hombre: menuda como él

            // ================================================================
            //  ACTO 1 — EL CERCO: LA TRIPLE CORONA DE CONJURO + LOS ANILLOS
            //  DE LOS SOLES, aprisionando al PORTADOR (la presa del festín)
            // ================================================================
            if (presencia > 0.02f && t < _tTrago + 20f + 1f)
            {
                float latido = 1f + 0.06f * MathF.Sin(tiempo * (4f + 4f * ciñe));
                float rBase = MathHelper.Lerp(1.35f, 0.52f, ciñe);
                rBase = MathHelper.Lerp(rBase, 0.10f, inhala);
                float rCorona = radioPresa * rBase * latido;

                // durante EL TRAGO el cerco ENTERO se desliza a la boca y muere ahí
                Vector2 bocaCer = BocaDelLibro(centroPresa);
                Vector2 centroAros = Vector2.Lerp(centroPresa, bocaCer, inhala);
                float alfaAros = presencia * (1f - 0.9f * inhala * inhala);
                Vector2 posPantalla = centroAros - Main.screenPosition;

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    // EL ABRILLANTADO: el resplandor DORADO + VIOLETA pulsando
                    // DETRÁS de la corona — ARDE al comer y al tragar
                    float brillo = 0.15f + 0.09f * MathF.Sin(tiempo * 3.1f) + 0.30f * ciñe + 0.25f * inhala;
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

                    // LA TRIPLE CORONA DE CONJURO (los anillos rúnicos de los
                    // agujeros negros: dorado de 8 + violeta de 6 + blanco de 6)
                    if (rCorona > 4f && alfaAros > 0.04f)
                    {
                        float giro = 1f + 0.9f * ciñe;    // la rueda corre al comer
                        OrbitaLib.CoronaConjuro(posPantalla, rCorona, tiempo * giro,
                            alfaAros * 1.10f, 2.1f);
                    }

                    // LA PLATAFORMA — el aro horizontal BAJO la presa
                    float anchoDisco = radioPresa * MathHelper.Lerp(2.0f, 1.30f, ciñe) * (1f - 0.85f * ciñe);
                    if (anchoDisco > 12f)
                    {
                        Vector2 posDisco = posPantalla + new Vector2(0f, p.height * 0.55f + 12f);
                        float ySquash = 0.26f + 0.05f * MathF.Sin(tiempo * 2.2f);
                        Main.spriteBatch.Draw(VFXCore.SoftGlow, posDisco, null,
                            new Color(150, 80, 235, 255) * (0.30f * manifiesta * alfaAros), 0f,
                            VFXCore.SoftGlow.Size() * 0.5f, new Vector2(anchoDisco / 64f, anchoDisco * ySquash / 64f),
                            SpriteEffects.None, 0f);
                    }
                }
                finally { Main.spriteBatch.End(); }

                // LOS ANILLOS RÚNICOS DE LOS SOLES (el emisor compartido)
                float rRunico = radioPresa * MathHelper.Lerp(2.3f, MathHelper.Lerp(1.4f, 0.15f, inhala), ciñe);
                if (rRunico > 4f && alfaAros > 0.04f)
                {
                    float spinUp = 1f + 1.6f * ciñe;
                    VFXCore.Begin();
                    SigiloLib.SistemaAnillos(centroAros, rRunico, tiempo * spinUp, semilla,
                        3, 0f, 0f, alfaAros * (1f - inhala * 0.6f));
                    VFXCore.FlushAdditive(null, false);
                }
            }

            // ================================================================
            //  ACTO 2 — EL LIBRO DE VERDAD (el DibujarElLibro del festín):
            //  crece, se inclina hacia su presa, MIRA FIJO con su iris
            //  recortado, mastica — y al trago SE CIERRA DE UN GOLPE
            // ================================================================
            DibujarElLibro(centroPresa, tiempo, t, muerde, inhala, poso, presencia, vivoF);

            // ================================================================
            //  ACTO 2b — LA SOMBRA DE LA PÁGINA: LA CADENA SombrasLib DEL
            //  FESTÍN, brotando DEL LIBRO hacia el portador
            // ================================================================
            if (t >= TSombra - 12f && presencia > 0.02f)
            {
                float sale = SombrasLib.DeGolpe(MathHelper.Clamp((t - (TSombra - 12f)) / 30f, 0f, 1f));
                float festin = MathHelper.Clamp((t - TSombra) / 80f, 0f, 1f);   // la cobertura crece

                // LA RAÍZ: EL LIBRO (la sombra brota de sus páginas)
                Vector2 haciaPresa = centroPresa - PosLibro;
                Vector2 rumboRaiz = haciaPresa.LengthSquared() > 4f
                    ? Vector2.Normalize(haciaPresa) : Vector2.UnitY;
                Vector2 raiz = PosLibro + rumboRaiz * 16f;

                // EL CHARCO bajo el libro
                SombrasLib.Charco(PosLibro + new Vector2(0f, 30f), 46f * sale, 0.7f * vivoF, tiempo, semilla);

                // EL CUERPO GIGANTE: libro → presa (y al trago, VUELVE cargando)
                float alcance = sale * 0.35f + festin * 0.65f;
                Vector2 destino = Vector2.Lerp(raiz + rumboRaiz * 34f, centroPresa, alcance);
                destino = Vector2.Lerp(destino, raiz + rumboRaiz * 16f, inhala * 0.85f);
                Vector2[] col = SombrasLib.ColumnaViva(raiz, destino, tiempo, semilla, 22, 0.20f, gancho: 0.45f);
                SombrasLib.Masa(col, 11f, 18f, 0.95f * vivoF, semilla, tiempo, grosorCentro: 48f);

                // LA BRUMA DEL CUERPO (dos capas — la firma del festín)
                SombrasLib.BrumaColumna(col, 44f, 0.50f * vivoF, tiempo, semilla, 13);
                SombrasLib.BrumaColumna(col, 30f, 0.40f * vivoF, tiempo, semilla + 53, 10);

                // LOS OJOS: TODOS abiertos, TODOS mirando a la presa
                SombrasLib.OjosDeMasa(col, centroPresa, (0.4f + 0.6f * festin) * vivoF, semilla, tiempo, 7);

                // LA CABEZA ES BRUMA (la boca de la casa)
                Vector2 punta = col[col.Length - 1];
                Vector2 rumbo = (centroPresa - punta).SafeNormalize(Vector2.UnitX);
                float vigor = MathHelper.Clamp(0.55f + 0.45f * muerde + 0.25f * inhala, 0f, 1f) * vivoF;
                SombrasLib.CabezaDeBruma(punta, rumbo, vigor, vivoF, tiempo, semilla);

                // EL DEVORADOR — LA CUBIERTA TOTAL DEL JUGADOR (la nueva
                // sobrecarga Player: la elipse del CUERPO del portador)
                if (vivo)
                {
                    float intensidad = MathHelper.Clamp(festin * 0.5f + MathHelper.Clamp((t - TSombra - 80f) / 80f, 0f, 1f) * 0.5f, 0f, 1f);
                    SombrasLib.Devorador(punta, p, intensidad, vivoF, tiempo, semilla, col, trago: inhala);

                    // LA HERIDA: donde la punta ENTERRÓ brilla rojo (digestión)
                    VFXCore.Begin();
                    VFXCore.Quad(punta, SombrasLib.Alfa(SombrasLib.Rojo, (0.42f + 0.20f * inhala) * vivoF),
                        new Vector2(100f, 100f), rumbo.ToRotation());
                    VFXCore.FlushAdditive();
                }

                // LA CORRIENTE DEL TRAGO — la bruma vuelve por el CUERPO
                if (inhala > 0.05f)
                {
                    for (int k = 0; k < 5; k++)
                    {
                        float prog = SombrasLib.Frac(tiempo * 0.8f + k * 0.2f);
                        float idxF = (1f - prog) * (col.Length - 1);
                        int idx = Math.Min((int)idxF, col.Length - 2);
                        if (idx < 0) idx = 0;
                        Vector2 pos = Vector2.Lerp(col[idx], col[idx + 1], SombrasLib.Frac(idxF))
                            + new Vector2(MathF.Sin(k * 2.1f + tiempo * 3f) * 14f, -8f);
                        SombrasLib.Bruma(pos, 24f + 14f * SombrasLib.Frac(k * 0.73f),
                            0.50f * inhala * vivoF, tiempo, semilla + k * 7,
                            new Vector2(0f, -26f * inhala));
                    }
                }
            }

            // ================================================================
            //  LA CORRIENTE Y LAS ALMAS: la vida del preso baja a la BOCA
            //  del libro (las almas SON ojos que miran a la boca)
            // ================================================================
            if (t >= TFestin && presencia > 0.02f)
            {
                float flujo = _estallo ? 1f - 0.6f * poso : 1f;
                Vector2 boca = BocaDelLibro(centroPresa);

                // LA BRUMA DE LA CORRIENTE: puffs que nacen alrededor del
                // preso y se deslizan hacia la boca abierta
                for (int k = 0; k < 5; k++)
                {
                    float prog = SombrasLib.Frac(tiempo * 0.55f + k * 0.2f);
                    Vector2 pos = Vector2.Lerp(
                        centroPresa + new Vector2(MathF.Sin(k * 2.4f + tiempo) * 30f,
                                                 MathF.Cos(k * 1.7f + tiempo * 1.3f) * 16f),
                        boca, prog * prog);
                    SombrasLib.Bruma(pos, 24f + 12f * SombrasLib.Frac(k * 0.73f),
                        0.50f * flujo * presencia, tiempo, semilla + k * 7,
                        new Vector2(0f, 20f * flujo));
                }

                // LAS ALMAS: el río que entra por la boca
                VFXCore.Begin();
                int nAlmas = _estallo ? 9 : 6;
                float radio = 96f;
                for (int k = 0; k < nAlmas; k++)
                {
                    float prog = SombrasLib.Frac(t * (0.016f + 0.014f * inhala) + k * (1f / nAlmas));
                    Vector2 a = centroPresa + new Vector2(MathF.Cos(k * 2.7f + tiempo * 0.7f), MathF.Sin(k * 3.1f + tiempo)) * radio * 0.4f;
                    Vector2 b = boca;
                    Vector2 ctrl = (a + b) * 0.5f + new Vector2(MathF.Sin(k * 2.3f) * 80f, -28f);
                    float u = 1f - prog;
                    Vector2 pos = u * u * a + 2f * u * prog * ctrl + prog * prog * b;
                    SombrasLib.Ojo(pos, 7f, b - pos, 1f);  // el alma mira a la boca
                }
                VFXCore.FlushAdditive();
            }

            // === EL POSO: el último velo y el pulso final de la onda ===
            if (_estallo && poso < 1f)
            {
                float fade = 1f - poso;
                SombrasLib.Bruma(PosLibro, 110f * (0.6f + 0.4f * fade), 0.6f * fade, tiempo, semilla,
                    new Vector2(MathF.Sin(tiempo * 0.7f) * 22f, -12f));
            }
        }

        // ==================================================================
        //  EL LIBRO DE VERDAD — el calco de DibujarElLibro del festín de
        //  jefes: el sprite REAL del Eterno, GRANDE, INCLINADO hacia su
        //  presa, ABIERTO comiendo (el párpado MEDIO + el iris recortado
        //  MIRÁNDOLO FIJO) — y al trago SE CIERRA DE UN GOLPE (Cerrado)
        // ==================================================================
        private void DibujarElLibro(Vector2 centroPresa, float tiempo, float t,
            float muerde, float inhala, float poso, float presencia, float vivoF)
        {
            // LA ESCALA: crece al nacer (1,1 → 2,6), mastica y TRAGA
            float esc = EscalaDelLibro(t, muerde);
            esc *= 1f + 0.10f * inhala;                       // el trago hincha
            esc *= 1f - 0.25f * poso;                         // y exhala al poso
            float alfaLibro = MathHelper.Clamp(t / 14f, 0f, 1f) * presencia;

            Rectangle frame = new Rectangle(0, 0, _texLibro.Width, _texLibro.Height);
            Vector2 origen = frame.Size() * 0.5f;
            Vector2 pos = PosLibro - Main.screenPosition;
            Color luz = Lighting.GetColor(PosLibro.ToTileCoordinates());
            // EL LIBRO SE INCLINA hacia su presa (está ARRIBA y la MIRA):
            // el vaivén de siempre + el sesgo de la rendición
            float rot = 0.09f * MathF.Sin(tiempo * 1.25f) - 0.18f * inhala;

            // EL PÁRPADO: ABIERTO comiendo (Medio) — y al trago SE CIERRA
            // de golpe (Cerrado): el chasquido del festín
            bool cerrado = _estallo;
            bool comiendo = t >= TFestin && !cerrado;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                Main.spriteBatch.Draw(_texLibro, pos, frame, luz * alfaLibro, rot, origen, esc,
                    SpriteEffects.None, 0f);

                if (comiendo)
                {
                    // la boca: el párpado MEDIO — la página abierta hacia su comida
                    Main.spriteBatch.Draw(_texMedio, pos, frame, luz * alfaLibro, rot, origen, esc,
                        SpriteEffects.None, 0f);

                    // LA MIRADA: el iris CLAVADO en el portador, con el
                    // RECORTES que respira al masticar (la cura v6.50.116:
                    // el iris nunca asoma por encima del párpado medio)
                    Vector2 d = centroPresa - PosLibro;
                    Vector2 mirada = d.LengthSquared() > 4f
                        ? Vector2.Normalize(d) * new Vector2(3.5f, 2.8f) * 0.9f
                        : Vector2.Zero;
                    Vector2 alOjo = (Weapons.GrimorioHambrientoNervioso.OjoTexel + mirada - origen) * esc;
                    Vector2 posOjo = pos + alOjo.RotatedBy(rot);

                    float apertura = 0.45f + 0.50f * muerde;   // 0,45 mordida → 0,95 bocado
                    int corte = (int)(_texIris.Height * (1f - apertura));
                    var recorte = new Rectangle(0, corte, _texIris.Width, _texIris.Height - corte);
                    var origenIris = new Vector2(_texIris.Width * 0.5f,
                        _texIris.Height * 0.5f - corte);
                    Main.spriteBatch.Draw(_texIris, posOjo, recorte, luz * alfaLibro, rot,
                        origenIris, esc * Weapons.GrimorioHambrientoNervioso.IrisEscala,
                        SpriteEffects.None, 0f);
                }
                else if (cerrado)
                {
                    // CERRADO: el libro sellado con el festín DENTRO
                    Main.spriteBatch.Draw(_texCerrado, pos, frame, luz * alfaLibro, rot, origen, esc,
                        SpriteEffects.None, 0f);
                }
                else
                {
                    // aún despierta: el iris abierto mira a su comida
                    Vector2 d = centroPresa - PosLibro;
                    Vector2 mirada = d.LengthSquared() > 4f
                        ? Vector2.Normalize(d) * new Vector2(3.5f, 2.8f) * 0.9f
                        : Vector2.Zero;
                    Vector2 alOjo = (Weapons.GrimorioHambrientoNervioso.OjoTexel + mirada - origen) * esc;
                    Vector2 posOjo = pos + alOjo.RotatedBy(rot);
                    Main.spriteBatch.Draw(_texIris, posOjo, null, luz * alfaLibro, rot,
                        _texIris.Size() * 0.5f, esc * Weapons.GrimorioHambrientoNervioso.IrisEscala,
                        SpriteEffects.None, 0f);
                }
            }
            finally { Main.spriteBatch.End(); }

            // === EL HALO DEL LIBRO (lote VFX, mundo): la presencia violeta
            //     que se ENCIENDE al comer y ARDE al tragar + EL CHASQUIDO
            //     del cierre (el destello + la onda que lo sella) ===
            VFXCore.Begin();
            try
            {
                float pulso = 0.14f + 0.06f * MathF.Sin(tiempo * 2.6f) + 0.22f * ciñeLibro(t) + 0.30f * inhala;
                VFXCore.Quad(PosLibro, SombrasLib.Alfa(SombrasLib.Violeta, pulso * presencia),
                    new Vector2(150f * esc, 150f * esc));

                // LA HERIDA: la boca abierta brilla ROJA mientras come
                if (comiendo)
                {
                    Vector2 hacia = centroPresa - PosLibro;
                    Vector2 dir = hacia.LengthSquared() > 4f ? Vector2.Normalize(hacia) : -Vector2.UnitY;
                    VFXCore.Quad(PosLibro + dir * 24f * esc,
                        SombrasLib.Alfa(SombrasLib.Rojo, (0.35f + 0.25f * muerde) * presencia),
                        new Vector2(90f * esc, 60f * esc));
                }
                // EL CHASQUIDO al cerrarse (la onda que lo sella)
                if (_estallo && t < _tTrago + 14f)
                {
                    float snap = 1f - (t - _tTrago) / 14f;
                    VFXCore.Quad(PosLibro, SombrasLib.Alfa(new Color(255, 240, 200), 0.8f * snap),
                        new Vector2(220f * (1f + 1.2f * (1f - snap)), 220f * (1f + 1.2f * (1f - snap))));
                    if (t < _tTrago + 10f)
                        SombrasLib.OndaChoque(PosLibro, 40f + 260f * (t - _tTrago) / 10f, 0.55f * snap);
                }
            }
            catch { }
            VFXCore.FlushAdditive();
        }

        /// <summary>El apretón del cerco leído en el halo (la fase del festín).</summary>
        private static float ciñeLibro(float t)
        {
            return MathHelper.Clamp((t - TSello) / 22f, 0f, 1f);
        }

        /// <summary>El masticar del festín: abre… ¡CIERRA de golpe! …mastica… abre.</summary>
        private static float CicloMasticar(float t)
        {
            float m = (t - TFestin) % 32f;
            if (m < 10f) return MathHelper.Lerp(0.2f, 1f, m / 10f);                          // abre
            if (m < 16f) return MathHelper.Lerp(1f, 0.03f, SombrasLib.DeGolpe((m - 10f) / 6f)); // ¡CIERRA!
            if (m < 26f) return 0.15f + 0.1f * MathF.Sin(m * 1.4f);                          // mastica
            return MathHelper.Lerp(0.15f, 0.85f, (m - 26f) / 6f);                            // reabre
        }

        // ==================================================================
        //  LAS TEXTURAS DEL FESTÍN (la familia del ojo — nula el ala de las
        //  dos tapas: el libro YA NO SE PARTE)
        // ==================================================================
        private static bool AsegurarTexturas()
        {
            if (_texturasProbadas && _texLibro == null) return false;
            if (_texLibro != null && _texMedio != null && _texCerrado != null && _texIris != null) return true;
            try
            {
                _texLibro = Terraria.GameContent.TextureAssets.Item[
                    ModContent.ItemType<Weapons.GrimoireEternal>()].Value;
                _texMedio = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambriento_Medio", (AssetRequestMode)1).Value;
                _texCerrado = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado", (AssetRequestMode)1).Value;
                _texIris = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambriento_Iris", (AssetRequestMode)1).Value;
                return _texLibro != null && _texMedio != null && _texCerrado != null && _texIris != null;
            }
            catch
            {
                _texturasProbadas = true;
                return false;
            }
        }
    }
}
