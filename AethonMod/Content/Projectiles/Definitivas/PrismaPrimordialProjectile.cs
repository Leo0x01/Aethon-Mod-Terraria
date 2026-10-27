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
    /// PRISMAPRIMORDIALPROJECTILE — v6.54.6 — ARMA 1 DE LAS CINCO: EL
    /// PRISMA PRIMORDIAL — EL ATAQUE DEFINITIVO DEL MOD.
    ///
    /// LA REFORMA (la letra del usuario: «es solo una línea recta con un
    /// poco de glow… le falta más vida, más efectos; que tenga al menos 7
    /// capas de efectos y ya conoces los efectos que tenía el rayo de
    /// fusión — dáselos al prisma»): el prisma hereda LA CORONA y con
    /// ella LA RECETA COMPLETA del rayo de fusión estudiado en la fuente
    /// (Task 2-b), pieza a pieza, adaptada a los siete colores:
    ///
    ///   CAPA 1 — EL VELO PRISMÁTICO: los cuatro bordes de la pantalla
    ///   respiran teñidos del color que cabecea del gradiente.
    ///   CAPA 2 — EL GLOW SÁNDWICH DEL FOCO: los CUATRO HALOS APILADOS
    ///   (14× · 5× · 4× · 2×) + las PESTAÑAS de estrella girando + EL
    ///   ARO ARCOÍRIS que gira sobre sí + la pupila en anillo.
    ///   CAPA 3 — LOS SIETE HACES DEL ESPECTRO: brotan del círculo
    ///   giratorio y CONVERGEN al foco, cada uno con SU color vivo, con
    ///   MEANDRO suave y su PUNTA DE NACIMIENTO pulsando.
    ///   CAPA 4 — EL HAZ CENTRAL CON ANCHO CRECIENTE: la fórmula EXACTA
    ///   del original (progress^0.8 · cbrt · K · (1.1 + cos·0.08)) —
    ///   nace fino en el foco, ENGORDA hacia el fondo y RESPIRA (±8%).
    ///   CAPA 5 — LAS CUATRO FUNDAS + LA VENA: halo del gradiente,
    ///   media funda, funda interna casi blanca y la VENA blanca pura.
    ///   CAPA 6 — LOS TRES PULSOS QUE VIAJAN: bandas de luz que CORREN
    ///   hacia afuera por el canal, cada una con su color desfasado.
    ///   CAPA 7 — LA LLUVIA CROMÁTICA: 12 polvos/frame a lo largo del
    ///   haz con OFFSET DE COLOR POR DISTANCIA (la receta textual del
    ///   original) + el temblor suave de cámara + la luz que baña el
    ///   canal + EL RESPLENDOR DE LA PUNTA (halos + nova + destello).
    ///
    /// EL BUG DE LA .54.4 CURADO: en MODO SENTENCIA (ai[0]=1, invocado
    /// por El Testamento) el prisma vivía PARA SIEMPRE (timeLeft
    /// refrescado cada tick y `_muriendo` jamás) — el haz eterno
    /// bloqueaba el arma y el juego quedaba «usándola» sin fin. Ahora
    /// la sentencia vive su ciclo completo (convergencia rápida 70 t +
    /// sostener + apagado 12 t) y MUERE — y el failsafe global de vida
    /// máxima (1200 t) protege cualquier futuro olvido.
    ///
    /// ai[1] = tick de vida.
    /// </summary>
    public class PrismaPrimordialProjectile : ModProjectile
    {
        private const int NHaz = 7;
        private const float LargoHaz = 2400f;

        // LA VIDA DEL MODO SENTENCIA (acotada: 70 de arranque + 110 de
        // veredicto sostenido + 12 de apagado — NUNCA más).
        private const int TSentenciaTotal = 192;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private int _semilla;
        private bool _muriendo;       // la muerte suave (arranca y ya no se retracta)
        private int _tMuerte;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Projectile.ai[1]++;
            float t = Projectile.ai[1];
            Player dueño = Main.player[Projectile.owner];
            float tiempo = Main.GlobalTimeWrappedHourly;
            if (_semilla == 0) _semilla = (int)Main.GameUpdateCount;

            // v6.54.6 — EL FAILSAFE GLOBAL: ningún prisma vive más de 1200 t
            // (la lección del haz eterno de la .54.4).
            if (t > 1200f) { Projectile.Kill(); return; }

            // EL CANAL vs LA SENTENCIA:
            //  · canalizado: suelto el clic → apagado suave.
            //  · sentencia (ai[0]=1, lo dicta El Testamento): vive su ciclo
            //    COMPLETO y acotado (TSentenciaTotal) — JAMÁS eterno.
            bool sentencia = Projectile.ai[0] >= 1f;
            if (sentencia)
            {
                if (t > TSentenciaTotal) _muriendo = true;
            }
            else
            {
                bool canalVivo = dueño.active && !dueño.dead && dueño.channel &&
                    dueño.HeldItem != null && dueño.HeldItem.type == ModContent.ItemType<Content.Weapons.Definitivas.PrismaPrimordial>();
                if (!canalVivo) _muriendo = true;
            }

            // LA MUERTE SUAVE: 12 t de apagado
            if (_muriendo)
            {
                _tMuerte++;
                if (_tMuerte > 12f) { Projectile.Kill(); return; }
            }

            // === LA GEOMETRÍA DEL PRISMA (flota al pecho del portador) ===
            Vector2 mira = (Main.MouseWorld - dueño.MountedCenter).SafeNormalize(Vector2.UnitX);
            Projectile.Center = dueño.MountedCenter + mira * 26f + new Vector2(0f, -8f);
            Projectile.rotation = mira.ToRotation();

            // LA CONVERGENCIA: canal 150 t · sentencia 70 t (la palabra ya
            // viene con toda la fe puesta)
            float calor = MathHelper.Clamp(t / (sentencia ? 70f : 150f), 0f, 1f);
            float apagado = _muriendo ? MathHelper.Clamp(1f - _tMuerte / 12f, 0f, 1f) : 1f;
            Projectile.localAI[1] = MathHelper.Lerp(Projectile.localAI[1], calor * apagado, 0.12f);
            float conv = Projectile.localAI[1];

            // EL DAÑO Y EL ANCHO ESCALAN con la convergencia (el veredicto crece)
            Projectile.damage = (int)(Projectile.originalDamage * (0.45f + 0.85f * conv));

            // LOS SONIDOS: el arrancar, el zumbar, y el AIRE que se llena
            if (t == 2) DefinitivasLib.Sonar(SoundID.Item13.WithPitchOffset(0.0f).WithVolumeScale(0.6f), Projectile.Center);
            if (t == 90 && conv > 0.5f) DefinitivasLib.Sonar(SoundID.Item122.WithPitchOffset(0.4f).WithVolumeScale(0.5f), Projectile.Center);
            if (sentencia && t == TSentenciaTotal - 12)
                DefinitivasLib.Sonar(SoundID.Item13.WithPitchOffset(-0.4f).WithVolumeScale(0.7f), Projectile.Center);

            // ==================== CAPA 7 — LA LLUVIA CROMÁTICA ====================
            // (la receta TEXTUAL del original: polvos por todo el canal con su
            // color desfasado por la distancia — el flujo que en el shader
            // original hacen las 4 texturas, aquí es polvo vivo)
            if (!Main.dedServ && Main.netMode != NetmodeID.Server && conv > 0.15f)
            {
                Vector2 dir = Projectile.rotation.ToRotationVector2();
                Vector2 foco = PosFoco(conv);
                int cuantos = conv > 0.6f ? 12 : 6;
                for (int i = 0; i < cuantos; i++)
                {
                    float prog = Main.rand.NextFloat(1f);
                    float dist = LargoHaz * prog;
                    // EL SEMIANCHO del haz en ese punto (la fórmula): el polvo
                    // cae DENTRO del canal, no en cualquier lado
                    float semi = AnchoHazEn(prog, conv) * 0.5f;
                    float perp = Main.rand.NextFloat(-1f, 1f) * semi;
                    Vector2 pos = foco + dir * dist + new Vector2(-dir.Y, dir.X) * perp;
                    Color c = DefinitivasLib.GradienteJuicio(tiempo * 0.10f - prog * 0.16f);
                    Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame,
                        dir.RotatedByRandom(0.4f) * Main.rand.NextFloat(1.5f, 6f) * conv, 0, c, 1.0f);
                    d.noGravity = true;
                }
                // LAS CHISPAS DEL FOCO convergiendo (el cruce de los siete)
                if (conv > 0.35f && Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(foco, DustID.GoldFlame,
                        new Vector2(-dir.Y, dir.X).RotatedByRandom(0.7f) * Main.rand.NextFloat(1f, 5f), 0,
                        DefinitivasLib.GradienteJuicio(Main.rand.NextFloat(1f)), 0.9f);
                    d.noGravity = true;
                }

                // LA LUZ QUE BAÑA EL CANAL (tres puntos del haz)
                Lighting.AddLight(foco, 1.8f * conv, 1.6f * conv, 1.2f * conv);
                Lighting.AddLight(foco + dir * 800f, 1.4f * conv, 1.2f * conv, 1.0f * conv);
                Lighting.AddLight(foco + dir * 1600f, 1.0f * conv, 0.9f * conv, 0.8f * conv);

                // EL TEMBLOR SUAVE (el juice sostenido del original, escala
                // prismática: no mata la mano, la acompaña)
                if (conv > 0.55f && Main.rand.NextBool(8))
                    OndaLib.Kick(3 + (int)(conv * 3f), 8);
            }

            if (!_muriendo)
                Projectile.timeLeft = Math.Max(Projectile.timeLeft, 30);
        }

        /// <summary>EL FOCO: donde convergen los siete haces (más cerca = más convergencia).</summary>
        private Vector2 PosFoco(float conv)
            => Projectile.Center + Projectile.rotation.ToRotationVector2() * MathHelper.Lerp(190f, 46f, conv);

        /// <summary>
        /// EL ANCHO DEL HAZ en la fracción p (0..1) — LA FÓRMULA EXACTA DEL
        /// RAYO DE FUSIÓN (DefinitivasLib.AnchoCono), escalada por la
        /// convergencia: nace fino, engorda hacia el fondo y RESPIRA.
        /// </summary>
        private static float AnchoHazEn(float p, float conv)
        {
            float norma = DefinitivasLib.AnchoCono(p, 0f, 1f);       // sin respiración (va aparte)
            float respira = 1f + 0.08f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3f - p * 2f);
            return MathF.Max(norma, 0.04f) * (34f + 118f * conv) * respira;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float conv = Projectile.localAI[1];
            if (conv <= 0.08f) return false;
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Vector2 foco = PosFoco(conv);
            Vector2 fin = foco + dir * LargoHaz;

            // EL CHEQUEO DE SEGMENTO GORDO con el ancho PROMEDIO del perfil
            // (fino en el foco, gordo al fondo — como el cono del original)
            float anchoMedio = (AnchoHazEn(0.15f, conv) + AnchoHazEn(0.85f, conv)) * 0.5f;
            return SegmentoGordo(targetHitbox, foco, fin, anchoMedio);
        }

        /// <summary>El chequeo de segmento con grosor (la distancia punto-segmento contra el semiancho).</summary>
        internal static bool SegmentoGordo(Rectangle hitbox, Vector2 a, Vector2 b, float anchoTotal)
        {
            Vector2 c = hitbox.Center();
            Vector2 ab = b - a;
            float len2 = ab.LengthSquared();
            float u = len2 < 1f ? 0f : MathHelper.Clamp(Vector2.Dot(c - a, ab) / len2, 0f, 1f);
            Vector2 punto = a + ab * u;
            float dist = Vector2.Distance(c, punto);
            return dist <= anchoTotal * 0.5f + MathF.Max(hitbox.Width, hitbox.Height) * 0.5f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            float t = Projectile.ai[1];
            float tiempo = Main.GlobalTimeWrappedHourly;
            float conv = Projectile.localAI[1];

            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Vector2 perp = new(-dir.Y, dir.X);
            Vector2 foco = PosFoco(conv);

            // ==================== CAPA 1 — EL VELO PRISMÁTICO ====================
            VeloPrismatico(0.10f + 0.22f * conv);

            // ==================== LA JOYA (el corazón que gira) ====================
            VFXCore.Begin();
            for (int i = 0; i < 3; i++)
            {
                float giro = tiempo * (0.7f + i * 0.2f) + i * 2.09f;
                Color cPrisma = DefinitivasLib.GradienteJuicio(tiempo * 0.09f + i * 0.33f);
                VFXCore.Quad(Projectile.Center + perp * MathF.Sin(giro) * 6f,
                    DefinitivasLib.Alfa(cPrisma, 0.30f * conv), new Vector2(64f, 64f), giro);
            }
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(Color.White, 0.55f * conv), new Vector2(38f, 38f));
            VFXCore.FlushAdditive();

            if (conv > 0.02f)
            {
                // ================ CAPA 2 — EL GLOW SÁNDWICH DEL FOCO ================
                // (los CUATRO HALOS APILADOS del rayo de fusión: 14× · 5× · 4× · 2×)
                Color grad = DefinitivasLib.GradienteJuicio(tiempo * 0.08f);
                Color grad2 = DefinitivasLib.GradienteJuicio(tiempo * 0.08f + 0.33f);
                VFXCore.Begin();
                VFXCore.Quad(foco, DefinitivasLib.Alfa(grad, 0.10f * conv), new Vector2(1500f, 1500f) * conv);
                VFXCore.Quad(foco, DefinitivasLib.Alfa(grad2, 0.20f * conv), new Vector2(560f, 560f) * conv);
                VFXCore.Quad(foco, DefinitivasLib.Alfa(grad, 0.42f * conv), new Vector2(430f, 430f) * conv);
                VFXCore.Quad(foco, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.50f * conv), new Vector2(215f, 215f) * conv);
                // LAS PESTAÑAS (la estrella de cuatro puntas que gira lenta)
                if (DefinitivasLib.TexStar != null)
                    VFXCore.Quad(foco, DefinitivasLib.Alfa(Color.White, 0.5f * conv),
                        new Vector2(340f, 340f) * conv, tiempo * 0.4f, DefinitivasLib.TexStar);
                // EL ARO ARCOÍRIS (la corona del foco — el espectro entero girando)
                if (VFXCore.Arcoiris != null)
                    VFXCore.Quad(foco, DefinitivasLib.Alfa(Color.White, 0.55f * conv),
                        new Vector2(300f, 300f) * conv, tiempo * 0.22f, VFXCore.Arcoiris);
                // LA PUPILA EN ANILLO
                if (VFXCore.Ring != null)
                    VFXCore.Quad(foco, DefinitivasLib.Alfa(grad2, 0.85f * conv),
                        new Vector2(120f, 120f) * (0.8f + 0.2f * conv), tiempo * 0.9f, VFXCore.Ring);
                VFXCore.Quad(foco, DefinitivasLib.Alfa(Color.White, 0.95f * conv), new Vector2(52f, 52f) * conv);
                VFXCore.FlushAdditive();
            }

            // ==================== CAPA 3 — LOS SIETE HACES DEL ESPECTRO ====================
            // (nacen en el círculo giratorio, CONVERGEN al foco; cada uno con SU
            // color, su MEANDRO suave y su PUNTA DE NACIMIENTO pulsando)
            for (int k = 0; k < NHaz; k++)
            {
                float angNacimiento = Projectile.rotation + MathHelper.TwoPi * k / NHaz + tiempo * 0.5f;
                float radioN = MathHelper.Lerp(150f, 26f, conv);
                Vector2 nace = Projectile.Center - dir * 10f + new Vector2(MathF.Cos(angNacimiento), MathF.Sin(angNacimiento)) * radioN;

                Color cHaz = ColorDePrisma(k, tiempo);

                // La tira con MEANDRO (la vida: el haz pequeño ondula al viajar)
                const int M = 12;
                var pts = new Vector2[M];
                var anchoK = new float[M];
                for (int i = 0; i < M; i++)
                {
                    float q = i / (float)(M - 1);
                    Vector2 base_ = Vector2.Lerp(nace, foco, q);
                    float ondeo = MathF.Sin(q * MathHelper.Pi) * MathF.Sin(tiempo * 3f + k * 1.7f + q * 9f) * 8f * (1f - conv * 0.5f);
                    pts[i] = base_ + perp * ondeo - Main.screenPosition;
                    anchoK[i] = 0.35f + 0.65f * MathF.Sin(q * MathHelper.Pi);
                }
                RayoStrip.AbrirLote();
                try
                {
                    RayoStrip.FilamentoEnLote(pts, MathHelper.Lerp(7f, 16f, conv), cHaz, DefinitivasLib.BlancoCalido,
                        (0.35f + 0.55f * conv), StormTaper.Linear, _semilla + k, 0, false, false, true, null, anchoK);
                }
                finally { RayoStrip.CerrarLote(); }

                // LA PUNTA DE NACIMIENTO (la boca del color — pulsa)
                VFXCore.Begin();
                float pulsoK = 0.7f + 0.3f * MathF.Sin(tiempo * 5f + k * 0.9f);
                VFXCore.Quad(nace, DefinitivasLib.Alfa(cHaz, 0.7f * conv * pulsoK), new Vector2(26f, 26f) * pulsoK);
                VFXCore.FlushAdditive();
            }

            // ==================== CAPAS 4 + 5 — EL HAZ CENTRAL ====================
            // (el ANCHO CRECIENTE del original con sus CUATRO FUNDAS y la VENA)
            if (conv > 0.12f)
            {
                const int N = 30;
                var pts = new Vector2[N];
                var ancho = new float[N];
                for (int i = 0; i < N; i++)
                {
                    float p = i / (float)(N - 1);
                    pts[i] = foco + dir * (LargoHaz * p) - Main.screenPosition;
                    // LA FÓRMULA: progress^0.8 · cbrt · K · (1.1 + cos·(0.08 − p·0.06)),
                    // normalizada — fino al foco, gordo al fondo, respirando
                    ancho[i] = DefinitivasLib.AnchoCono(p, tiempo * 3f, 1f) /
                               DefinitivasLib.AnchoCono(1f, tiempo * 3f, 1f);
                    if (ancho[i] < 0.05f) ancho[i] = 0.05f;
                }
                Color g0 = DefinitivasLib.GradienteJuicio(tiempo * 0.07f);
                Color g1 = DefinitivasLib.GradienteJuicio(tiempo * 0.07f + 0.25f);
                Color g2 = DefinitivasLib.GradienteJuicio(tiempo * 0.07f + 0.50f);

                RayoStrip.AbrirLote();
                try
                {
                    // FUNDA EXTERNA — el tsunami del gradiente (el halo 3.5×)
                    RayoStrip.FilamentoEnLote(pts, (34f + 118f * conv), g0, g1, 0.32f * conv,
                        StormTaper.Flat, _semilla + 20, 0, false, false, false, null, ancho);
                    // FUNDA MEDIA — el cuerpo del veredicto
                    RayoStrip.FilamentoEnLote(pts, (22f + 74f * conv), g1, g2, 0.55f * conv,
                        StormTaper.Flat, _semilla + 21, 0, false, false, false, null, ancho);
                    // FUNDA INTERNA — casi blanca
                    RayoStrip.FilamentoEnLote(pts, (12f + 38f * conv), g2, DefinitivasLib.BlancoCalido, 0.85f * conv,
                        StormTaper.Flat, _semilla + 22, 0, false, false, false, null, ancho);
                    // LA VENA — el núcleo blanco puro (el «return 1» del shader original)
                    RayoStrip.FilamentoEnLote(pts, (6f + 18f * conv), DefinitivasLib.BlancoCalido, Color.White,
                        0.95f * conv, StormTaper.Flat, _semilla + 23, 0, false, false, true, null, ancho);
                }
                finally { RayoStrip.CerrarLote(); }

                // ==================== CAPA 6 — LOS TRES PULSOS QUE VIAJAN ====================
                // (el flujo cromático: bandas de luz que CORREN hacia afuera)
                for (int k = 0; k < 3; k++)
                {
                    float prog = DefinitivasLib.Frac(tiempo * 0.34f + k / 3f);
                    float centroPx = prog * LargoHaz;
                    const int MP = 10;
                    var banda = new Vector2[MP];
                    var anchoB = new float[MP];
                    for (int i = 0; i < MP; i++)
                    {
                        float q = i / (float)(MP - 1);
                        float px = centroPx + (q - 0.5f) * 300f;
                        banda[i] = foco + dir * MathHelper.Clamp(px, 0f, LargoHaz) - Main.screenPosition;
                        float local = 1f - MathF.Abs(q - 0.5f) * 2f;
                        anchoB[i] = MathF.Pow(local, 0.7f) *
                            (DefinitivasLib.AnchoCono(MathHelper.Clamp(px / LargoHaz, 0f, 1f), tiempo * 3f, 1f) /
                             MathF.Max(DefinitivasLib.AnchoCono(1f, tiempo * 3f, 1f), 0.01f));
                        if (anchoB[i] < 0.02f) anchoB[i] = 0.02f;
                    }
                    Color cPulso = DefinitivasLib.GradienteJuicio(tiempo * 0.07f + 0.62f - prog * 0.12f);
                    RayoStrip.AbrirLote();
                    try
                    {
                        RayoStrip.FilamentoEnLote(banda, (26f + 60f * conv), cPulso, DefinitivasLib.BlancoCalido,
                            0.65f * conv, StormTaper.Flat, _semilla + 30 + k, 0, false, false, true, null, anchoB);
                    }
                    finally { RayoStrip.CerrarLote(); }
                }

                // ==================== CAPA 7b — EL RESPLENDOR DE LA PUNTA ====================
                Vector2 punta = foco + dir * LargoHaz;
                VFXCore.Begin();
                VFXCore.Quad(punta, DefinitivasLib.Alfa(g1, 0.22f * conv), new Vector2(700f, 700f) * conv);
                VFXCore.Quad(punta, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.30f * conv), new Vector2(320f, 320f) * conv);
                if (VFXCore.NovaBurst != null)
                    VFXCore.Quad(punta, DefinitivasLib.Alfa(Color.White, 0.5f * conv),
                        new Vector2(420f, 420f) * conv, tiempo * 0.2f, VFXCore.NovaBurst);
                VFXCore.FlushAdditive();
            }

            // v6.54.5/.6 — EL CONTRATO DEL LOTE: TODO PreDraw sale con el lote
            // del pase de entidades VIVO (los Flush* y RayoStrip lo cierran).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>
        /// CAPA 1 — EL VELO PRISMÁTICO: los cuatro bordes de la pantalla
        /// respiran teñidos con el color que cabecea del gradiente (la
        /// anticipación de cine del rayo de fusión, en clave de espectro).
        /// </summary>
        private static void VeloPrismatico(float alfa)
        {
            if (alfa <= 0.02f) return;
            float w = Main.screenWidth, h = Main.screenHeight;
            Vector2 o = Main.screenPosition;
            float tiempo = Main.GlobalTimeWrappedHourly;
            float pulso = 0.9f + 0.1f * MathF.Sin(tiempo * 2.1f);
            Color c = DefinitivasLib.GradienteJuicio(tiempo * 0.05f);

            VFXCore.Begin();
            Color denso = DefinitivasLib.Alfa(new Color(30, 20, 46), alfa * 0.62f * pulso);
            VFXCore.Quad(new Vector2(o.X - w * 0.34f, o.Y + h * 0.5f), denso, new Vector2(w * 1.1f, h * 2.4f), 0f, VFXCore.SoftGlow);
            VFXCore.Quad(new Vector2(o.X + w * 1.34f, o.Y + h * 0.5f), denso, new Vector2(w * 1.1f, h * 2.4f), 0f, VFXCore.SoftGlow);
            VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y - h * 0.36f), denso, new Vector2(w * 2.4f, h * 1.1f), 0f, VFXCore.SoftGlow);
            VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y + h * 1.36f), denso, new Vector2(w * 2.4f, h * 1.1f), 0f, VFXCore.SoftGlow);
            // EL TINTE del gradiente (muy sutil: la sala sabe qué color toca)
            VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y + h * 0.5f), DefinitivasLib.Alfa(c, alfa * 0.05f * pulso),
                new Vector2(w * 1.6f, h * 1.6f), 0f, VFXCore.SoftGlow);
            VFXCore.FlushAlpha();
        }

        /// <summary>EL COLOR DEL ESPECTRO del haz k (los siete del prisma, vivos).</summary>
        private static Color ColorDePrisma(int k, float tiempo)
        {
            // El espectro de vanilla (Last Prism) con el latido del espectro
            float late = 0.82f + 0.18f * MathF.Sin(tiempo * 4f + k * 0.8f);
            Color c = k switch
            {
                0 => new Color(255, 80, 80),      // rojo
                1 => new Color(255, 160, 60),     // naranja
                2 => new Color(255, 230, 100),    // amarillo
                3 => new Color(110, 255, 130),    // verde
                4 => new Color(110, 220, 255),    // cian
                5 => new Color(150, 130, 255),    // azul
                _ => new Color(220, 120, 255),    // violeta
            };
            return DefinitivasLib.Alfa(c, late);
        }
    }
}
