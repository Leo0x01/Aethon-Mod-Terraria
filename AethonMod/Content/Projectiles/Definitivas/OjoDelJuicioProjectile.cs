using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Definitivas
{
    /// <summary>
    /// OJODELJUICIOPROJECTILE — v6.50.66 — ARMA 1: EL RAYO CONCENTRADO.
    ///
    /// LA ADAPTACIÓN DEL FUSIONRAY DEL DIOS ANTIGUO (leído y diseccionado en su
    /// fuente — Task 2-b) AL LADO DE LA LUZ: donde el dios antiguo vomita su
    /// arcoíris de aceite, EL GRIMORIO CONCENTRA la luz primordial y la
    /// DEVUELVA como veredicto. La receta, pieza a pieza:
    ///
    ///   · LA CARGA (80 t): el OJO se abre sobre el portador con sus
    ///     CUATRO HALOS APILADOS (escala 14× — el «glow sándwich» de
    ///     el dios antiguo), las CHISPAS DE ENERGÍA CONVERGEN (polvo vivo que cae
    ///     hacia el ojo) y el pitido sube de tono. La sala respira con el
    ///     velo dorado de los bordes.
    ///   · EL RAYO (320 t): un CONO de 2400 px con SEMIÁNGULO 0.26 y
    ///     ANCHO CRECIENTE — la fórmula EXACTA del original
    ///     (progress^0.8 · cbrt · K · (1.1 + cos·0.08)) — que BARRE el
    ///     campo en UNA ONDA COMPLETA (cos(t/T·2π)·2.2 rad) mientras
    ///     RASTREA lento a la presa (AnguloHacia 0.05→0.015). Núcleo
    ///     blanco puro, TRES fundas del gradiente del Juicio y DOS
    ///     PULSOS QUE VIAJAN por el rayo (el flujo cromático).
    ///   · EL JUICE: Kick cada 4 t TODO el rayo, Flash al abrir,
    ///     12 chispas/frame a lo largo del cono, vuelo infinito al
    ///     portador (la honestidad del original: el barrido no se esquiv a
    ///     caminando).
    ///   · EL COLAPSO (36 t): el cono se pliega, el sizzle y el destello
    ///     final de cierre.
    ///
    /// ai[1] = fase (0 carga · 1 rayo · 2 colapso) · ai[2] = tick.
    /// </summary>
    public class OjoDelJuicioProjectile : ModProjectile
    {
        private const byte FASE_CARGA = 0;
        private const byte FASE_RAYO = 1;
        private const byte FASE_COLAPSO = 2;

        private const int CargaTicks = 80;
        private const int RayoTicks = 320;
        private const int LargoCono = 2400;
        private const float Semiangulo = 0.26f;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private float _rumbo;          // el ángulo base (persigue a la presa)
        private float _tamanoReal;     // el tamaño suavizado del rayo (0..1)
        private int _direccion = 1;    // el lado del barrido
        private int _semilla;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = CargaTicks + RayoTicks + 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
            Projectile.netImportant = true;
        }

        private NPC Presa()
        {
            NPC mejor = null;
            float mejorD = 2600f * 2600f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.life <= 0 || n.dontTakeDamage) continue;
                if (n.friendly || n.townNPC) continue;
                float d = Vector2.DistanceSquared(n.Center, Projectile.Center);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            byte fase = (byte)Projectile.ai[1];
            Player dueño = Main.player[Projectile.owner];
            float tiempo = Main.GlobalTimeWrappedHourly;

            if (_semilla == 0) _semilla = (int)Projectile.position.X ^ (int)Main.GameUpdateCount;
            if (t < 2f)
            {
                _direccion = Main.rand.NextBool() ? 1 : -1;
                _rumbo = (Main.MouseWorld - dueño.MountedCenter).ToRotation();
            }

            // EL CANAL: soltar el clic durante la carga corta el ritual
            // (ai[0] = 1 → MODO SENTENCIA: invocado por El Testamento —
            // vive entero sin canal, el libro dicta y el ojo obedece)
            if (fase == FASE_CARGA && Projectile.ai[0] != 1f && !dueño.channel)
            {
                Projectile.ai[1] = FASE_COLAPSO;
                Projectile.ai[2] = 0;
                fase = FASE_COLAPSO;
                t = 0;
            }
            // el rayo ya disparado vive su vida completo (el veredicto no se retracta)

            float objetivoTam = 0f;

            switch (fase)
            {
                case FASE_CARGA:
                {
                    Projectile.Center = PosOjo(dueño);
                    _rumbo = DefinitivasLib.AnguloHacia(_rumbo,
                        (Main.MouseWorld - Projectile.Center).ToRotation(), 0.06f);

                    // LA MÚSICA DE LA CARGA: tres pitidos que suben
                    if (t == 4) DefinitivasLib.Sonar(SoundID.Item13.WithPitchOffset(-0.5f).WithVolumeScale(0.5f), Projectile.Center);
                    if (t == 40) DefinitivasLib.Sonar(SoundID.Item13.WithPitchOffset(-0.1f).WithVolumeScale(0.7f), Projectile.Center);
                    if (t == 68) DefinitivasLib.Sonar(SoundID.Item13.WithPitchOffset(0.4f).WithVolumeScale(0.9f), Projectile.Center);

                    // LAS CHISPAS CONVERGENTES (el «ChromaticEnergyDust» del original)
                    if (!Main.dedServ && Main.netMode != NetmodeID.Server && t > 8)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                            float radio = Main.rand.NextFloat(180f, 460f);
                            Vector2 pos = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio;
                            Vector2 vel = (Projectile.Center - pos) * 0.045f;
                            Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, vel, 0,
                                DefinitivasLib.GradienteJuicio(tiempo * 0.11f + i * 0.2f), 0.9f);
                            d.noGravity = true;
                        }
                        Lighting.AddLight(Projectile.Center, 1.4f, 1.1f, 0.6f);
                    }

                    if (t >= CargaTicks)
                    {
                        Projectile.ai[1] = FASE_RAYO;
                        Projectile.ai[2] = 0;
                        DefinitivasLib.Sonar(SoundID.Item122.WithPitchOffset(-0.15f).WithVolumeScale(1.1f), Projectile.Center);
                        OndaLib.Flash(new Color(255, 240, 200), 0.30f, 12, Projectile.Center);
                        OndaLib.Kick(12, 18);
                    }
                    break;
                }

                case FASE_RAYO:
                {
                    // EL RASTREO LENTO (0.05 → 0.015: la presa puede huir, no salvarse)
                    NPC presa = Presa();
                    float hacia = presa != null
                        ? (presa.Center - Projectile.Center).ToRotation()
                        : (Main.MouseWorld - Projectile.Center).ToRotation();
                    _rumbo = DefinitivasLib.AnguloHacia(_rumbo, hacia,
                        MathHelper.Lerp(0.05f, 0.015f, MathHelper.Clamp(t / RayoTicks, 0f, 1f)));

                    // EL BARRIDO SINUSOIDAL — UNA onda completa de lado a lado
                    float entrada = MathHelper.Clamp(t / 30f, 0f, 1f);
                    float salida = MathHelper.Clamp((RayoTicks - t) / 40f, 0f, 1f);
                    float ondulación = MathF.Cos(t / RayoTicks * MathHelper.TwoPi) * 2.2f * entrada * salida;
                    Projectile.rotation = _rumbo + ondulación * _direccion;

                    // EL TAMAÑO: nace con golpe, sostiene, muere plegándose
                    objetivoTam = MathF.Pow(
                        MathHelper.Clamp((t + 14f) / 50f, 0f, 1f) *
                        MathHelper.Clamp((RayoTicks + 40f - t) / 60f, 0f, 1f), 3f);

                    // EL JUICE DEL ORIGINAL: temblor TODO el rayo
                    if (!Main.dedServ && Main.netMode != NetmodeID.Server && t % 4 == 0 && t < RayoTicks - 4)
                        OndaLib.Kick(7 + (int)(_tamanoReal * 4f), 12);

                    // EL ALIENTO DE LA SALA: el ojo mira, la pantalla tiembla, el aire brilla
                    if (!Main.dedServ && Main.netMode != NetmodeID.Server)
                    {
                        Vector2 dir = Projectile.rotation.ToRotationVector2();
                        for (int i = 0; i < 12; i++)
                        {
                            float prog = Main.rand.NextFloat(1f);
                            float dist = LargoCono * (0.5f + _tamanoReal * 0.5f) * prog;
                            float perp = Main.rand.NextFloat(-90f, 90f) * prog * _tamanoReal;
                            Vector2 pos = Projectile.Center + dir * dist + new Vector2(-dir.Y, dir.X) * perp;
                            Color c = DefinitivasLib.GradienteJuicio(tiempo * 0.10f - prog * 0.16f);
                            Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame,
                                dir.RotatedByRandom(0.5f) * Main.rand.NextFloat(2f, 9f) * _tamanoReal, 0, c, 1.1f);
                            d.noGravity = true;
                        }
                        Lighting.AddLight(Projectile.Center + dir * 300f, 1.8f, 1.5f, 0.9f);
                        Lighting.AddLight(Projectile.Center + dir * 1000f, 1.5f, 1.2f, 0.8f);
                    }

                    // EL VUELO INFINITO DEL PORTADOR (la honestidad de el dios antiguo:
                    // el cono no se esquiva caminando — el veredicto te sostiene)
                    dueño.wingTime = dueño.wingTimeMax;
                    if (dueño.velocity.Y > 2f) dueño.velocity.Y *= 0.82f;

                    if (t >= RayoTicks)
                    {
                        Projectile.ai[1] = FASE_COLAPSO;
                        Projectile.ai[2] = 0;
                        DefinitivasLib.Sonar(SoundID.Item13.WithPitchOffset(-0.6f).WithVolumeScale(0.8f), Projectile.Center);
                    }
                    break;
                }

                case FASE_COLAPSO:
                {
                    if (t == 1)
                    {
                        // EL SIZZLE de cierre + el estallido de chispas
                        DefinitivasLib.Sonar(SoundID.Item74.WithPitchOffset(-0.5f), Projectile.Center);
                        if (!Main.dedServ && Main.netMode != NetmodeID.Server)
                        {
                            OndaLib.Flash(new Color(255, 214, 120), 0.22f, 10, Projectile.Center);
                            for (int i = 0; i < 40; i++)
                            {
                                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Main.rand.NextFloat(3f, 12f),
                                    0, DefinitivasLib.GradienteJuicio(Main.rand.NextFloat(1f)), 1.2f);
                                d.noGravity = true;
                            }
                        }
                    }
                    if (t > 34) Projectile.Kill();
                    break;
                }
            }

            _tamanoReal = MathHelper.Lerp(_tamanoReal, objetivoTam, 0.1f);
            if (fase == FASE_CARGA) Projectile.Center = PosOjo(dueño);
            Projectile.timeLeft = Math.Max(Projectile.timeLeft, 30);
        }

        /// <summary>EL OJO DEL PORTADOR: flota sobre su cabeza, mirando siempre a la presa.</summary>
        private static Vector2 PosOjo(Player dueño)
            => dueño.MountedCenter + new Vector2(0f, -96f);

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.ai[1] == FASE_RAYO && _tamanoReal > 0.12f)
                return DefinitivasLib.Cono(targetHitbox, Projectile.Center,
                    LargoCono * (0.5f + _tamanoReal * 0.5f), Projectile.rotation, Semiangulo * _tamanoReal);
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];
            float tiempo = Main.GlobalTimeWrappedHourly;
            Player dueño = Main.player[Projectile.owner];

            // ==================== EL VELO DORADO DE LA SALA ====================
            if (fase != FASE_COLAPSO)
            {
                float velo = fase == FASE_CARGA
                    ? 0.18f + 0.30f * MathHelper.Clamp(t / CargaTicks, 0f, 1f)
                    : 0.46f * MathHelper.Clamp((RayoTicks - t) / 50f, 0f, 1f);
                VeloDorado(velo);
            }

            // ==================== EL OJO DEL JUICIO ====================
            DibujarOjo(Projectile.Center, fase, t, tiempo, dueño);

            // ==================== EL CONO ====================
            if (fase == FASE_RAYO || (fase == FASE_COLAPSO && _tamanoReal > 0.05f))
                DibujarCono(Projectile.Center, Projectile.rotation, _tamanoReal, tiempo, fase, t);

            return false;
        }

        /// <summary>
        /// EL VELO DORADO — la anticipación de cine (la receta del Vignette
        /// de la casa, teñida de luz): los cuatro bordes respiran en oro.
        /// </summary>
        private static void VeloDorado(float alfa)
        {
            if (alfa <= 0.02f) return;
            float w = Main.screenWidth, h = Main.screenHeight;
            Vector2 o = Main.screenPosition;
            float pulso = 0.9f + 0.1f * MathF.Sin(tiempo());

            VFXCore.Begin();
            Color c = DefinitivasLib.Alfa(new Color(48, 26, 8), alfa * 0.62f * pulso);
            VFXCore.Quad(new Vector2(o.X - w * 0.34f, o.Y + h * 0.5f), c, new Vector2(w * 1.1f, h * 2.4f), 0f, VFXCore.SoftGlow);
            VFXCore.Quad(new Vector2(o.X + w * 1.34f, o.Y + h * 0.5f), c, new Vector2(w * 1.1f, h * 2.4f), 0f, VFXCore.SoftGlow);
            VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y - h * 0.36f), c, new Vector2(w * 2.4f, h * 1.1f), 0f, VFXCore.SoftGlow);
            VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y + h * 1.36f), c, new Vector2(w * 2.4f, h * 1.1f), 0f, VFXCore.SoftGlow);
            VFXCore.FlushAlpha();
        }

        private static float tiempo() => Main.GlobalTimeWrappedHourly;

        /// <summary>
        /// EL OJO — el sello del grimorio abierto sobre el portador: el
        /// «glow sándwich» del original (cuatro halos apilados de escala
        /// 14× a 2×) con la PUPILA DORADA en el centro y sus RAYOS de
        /// pestañas girando despacio.
        /// </summary>
        private static void DibujarOjo(Vector2 pos, byte fase, float t, float tiempo, Player dueño)
        {
            float carga = fase == FASE_CARGA ? MathHelper.Clamp(t / CargaTicks, 0f, 1f)
                : fase == FASE_RAYO ? 1f
                : MathHelper.Clamp(1f - t / 34f, 0f, 1f);
            if (carga <= 0.01f) return;

            Color grad = DefinitivasLib.GradienteJuicio(tiempo * 0.08f);
            Color grad2 = DefinitivasLib.GradienteJuicio(tiempo * 0.08f + 0.33f);

            VFXCore.Begin();

            // LOS CUATRO HALOS APILADOS (la receta del FusionRay: 14× → 5× → 4× → 2×)
            VFXCore.Quad(pos, DefinitivasLib.Alfa(grad, 0.10f * carga), new Vector2(1500f, 1500f) * carga);
            VFXCore.Quad(pos, DefinitivasLib.Alfa(grad2, 0.20f * carga), new Vector2(560f, 560f) * carga);
            VFXCore.Quad(pos, DefinitivasLib.Alfa(grad, 0.42f * carga), new Vector2(430f, 430f) * carga);
            VFXCore.Quad(pos, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.50f * carga), new Vector2(215f, 215f) * carga);

            // EL DESTELLO DE RAYOS (las pestañas del ojo — el lens-flare de la casa)
            if (DefinitivasLib.TexStar != null)
                DefinitivasLib.Blit(Main.spriteBatch, DefinitivasLib.TexStar, pos - Main.screenPosition,
                    DefinitivasLib.Alfa(Color.White, 0.5f * carga), new Vector2(340f, 340f) * carga,
                    tiempo * 0.4f);

            // LA PUPILA: anillo iris + núcleo
            if (VFXCore.Ring != null)
                DefinitivasLib.Blit(Main.spriteBatch, VFXCore.Ring, pos - Main.screenPosition,
                    DefinitivasLib.Alfa(grad2, 0.85f * carga), new Vector2(120f, 120f) * (0.8f + 0.2f * carga),
                    tiempo * 0.9f);
            VFXCore.Quad(pos, DefinitivasLib.Alfa(Color.White, 0.95f * carga), new Vector2(52f, 52f) * carga);

            VFXCore.FlushAdditive();
        }

        /// <summary>
        /// EL CONO — EL CORAZÓN DEL ARMA. TRES fundas de primitivas (la
        /// receta RayoStrip de la casa: tira continua con perfil
        /// gaussiano y ancho por nodo) con el ANCHO CRECIENTE del
        /// original + la VENA blanca + DOS PULSOS QUE VIAJAN por el canal
        /// (el flujo cromático que en el dios antiguo hace el shader con 4
        /// texturas — aquí, bandas de luz que corren hacia afuera) + el
        /// resplandor del extremo.
        /// </summary>
        private void DibujarCono(Vector2 origen, float angulo, float tam, float tiempo, byte fase, float t)
        {
            float activo = tam;
            if (activo <= 0.03f) return;

            float largo = LargoCono * (0.5f + activo * 0.5f);
            Vector2 dir = angulo.ToRotationVector2();
            Vector2 perp = new(-dir.Y, dir.X);

            // === EL CAMINO DEL CANAL (36 nodos, coords PANTALLA) ===
            const int N = 36;
            var pts = new Vector2[N];
            var ancho = new float[N];
            for (int i = 0; i < N; i++)
            {
                float p = i / (float)(N - 1);
                pts[i] = origen + dir * (largo * p) - Main.screenPosition;
                ancho[i] = DefinitivasLib.AnchoCono(p, tiempo * 3f, 1f) / DefinitivasLib.AnchoCono(1f, tiempo * 3f, 1f);
                if (ancho[i] < 0.02f) ancho[i] = 0.02f;
            }

            // === LAS TRES FUNDAS + LA VENA (tira por tira, del halo al núcleo) ===
            Color g0 = DefinitivasLib.GradienteJuicio(tiempo * 0.07f);          // el color que ABRE el ciclo
            Color g1 = DefinitivasLib.GradienteJuicio(tiempo * 0.07f + 0.25f);
            Color g2 = DefinitivasLib.GradienteJuicio(tiempo * 0.07f + 0.50f);

            RayoStrip.AbrirLote();
            try
            {
                // FUNDA EXTERNA — el tsunami (halo 3.1× de 300 px de ancho base)
                RayoStrip.FilamentoEnLote(pts, 300f * activo, g0, g1, 0.30f * activo,
                    StormTaper.Flat, _semilla, 0, false, false, false, null, ancho);
                // FUNDA MEDIA — el cuerpo del veredicto
                RayoStrip.FilamentoEnLote(pts, 170f * activo, g1, g2, 0.55f * activo,
                    StormTaper.Flat, _semilla + 1, 0, false, false, false, null, ancho);
                // FUNDA INTERNA — casi blanca
                RayoStrip.FilamentoEnLote(pts, 80f * activo, g2, DefinitivasLib.BlancoCalido, 0.85f * activo,
                    StormTaper.Flat, _semilla + 2, 0, false, false, false, null, ancho);
                // LA VENA — el núcleo blanco puro (el «return 1» del shader original)
                RayoStrip.FilamentoEnLote(pts, 30f * activo, DefinitivasLib.BlancoCalido, Color.White,
                    0.95f * activo, StormTaper.Flat, _semilla + 3, 0, false, false, true, null, ancho);
            }
            finally { RayoStrip.CerrarLote(); }

            // === LOS DOS PULSOS QUE VIAJAN (el flujo cromático sin shader) ===
            for (int k = 0; k < 2; k++)
            {
                float prog = DefinitivasLib.Frac(tiempo * 0.34f + k * 0.5f);      // corren hacia afuera
                float centroPx = prog * largo;
                const int M = 10;
                var banda = new Vector2[M];
                var anchoB = new float[M];
                for (int i = 0; i < M; i++)
                {
                    float q = i / (float)(M - 1);
                    float px = centroPx + (q - 0.5f) * 260f;
                    banda[i] = origen + dir * MathHelper.Clamp(px, 0f, largo) - Main.screenPosition;
                    // el ancho del pulso: campana suave a lo largo de su propia longitud
                    float local = 1f - MathF.Abs(q - 0.5f) * 2f;
                    anchoB[i] = MathF.Pow(local, 0.7f) *
                        (DefinitivasLib.AnchoCono(MathHelper.Clamp(px / largo, 0f, 1f), tiempo * 3f, 1f) /
                         MathF.Max(DefinitivasLib.AnchoCono(1f, tiempo * 3f, 1f), 0.01f));
                    if (anchoB[i] < 0.02f) anchoB[i] = 0.02f;
                }
                Color cPulso = DefinitivasLib.GradienteJuicio(tiempo * 0.07f + 0.62f - prog * 0.12f);
                RayoStrip.AbrirLote();
                try
                {
                    RayoStrip.FilamentoEnLote(banda, 90f * activo, cPulso, DefinitivasLib.BlancoCalido,
                        0.65f * activo, StormTaper.Flat, _semilla + 10 + k, 0, false, false, true, null, anchoB);
                }
                finally { RayoStrip.CerrarLote(); }
            }

            // === EL RESPLENDOR DEL EXTREMO (el cono MUERE brillando) ===
            Vector2 punta = origen + dir * largo;
            VFXCore.Begin();
            VFXCore.Quad(punta, DefinitivasLib.Alfa(g1, 0.22f * activo), new Vector2(700f, 700f) * activo);
            VFXCore.Quad(punta, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.30f * activo), new Vector2(320f, 320f) * activo);
            if (VFXCore.NovaBurst != null)
                DefinitivasLib.Blit(Main.spriteBatch, VFXCore.NovaBurst, punta - Main.screenPosition,
                    DefinitivasLib.Alfa(Color.White, 0.5f * activo), new Vector2(420f, 420f) * activo, tiempo * 0.2f);
            VFXCore.FlushAdditive();
        }
    }
}
