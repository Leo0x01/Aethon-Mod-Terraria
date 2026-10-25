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
    /// SOMBRAPAGINA4PROJECTILE — v6.52.2 — LA SOMBRA DE LA PÁGINA IV:
    /// EL IRIS COLOSAL (la letra: «crea un sombra de la pagina 3 y 4
    /// pero para estos solo usa el iris no el ojo completo»).
    ///
    /// EL CUARTO TOMO — la página que prescindió del cuerpo entero: lo que
    /// caza es UN SOLO IRIS GIGANTE (nuestro GrimorioHambriento_IrisRojo
    /// ×4-6), colgado del portador por DOS NERVIOS ÓPTICOS (dos cuerdas
    /// finas de sombra que se trenzan suavemente). NO hay ojo — el iris
    /// ES la boca:
    ///
    ///   · LA PUPILA: un disco negro (SoftGlow tintado, en el lote
    ///     vanilla ALFA — el único sitio donde el negro de verdad oscurece)
    ///     que SE DILATA con el masticar y en el drenaje se abre EN
    ///     BOCIO — el vacío que traga.
    ///   · EL DESTRALLO: durante el drenaje, filamentos de tinta
    ///     (StormLib.PerlinBolt) corren DESDE la presa HACIA la pupila —
    ///     la esencia viaja por dentro del iris.
    ///   · EL MORDISCO: el iris colosal se PRENSA sobre el jefe, la
    ///     pupila al máximo (el jefe visto A TRAVÉS del iris) + la corona
    ///     de garras del festín — y el drenaje de siempre (3%/6t) con la
    ///     devoración a 1 HP (estilo 3, el motor intacto).
    /// </summary>
    public class SombraPagina4Projectile : ModProjectile
    {
        private const byte FASE_EMERGER = 0;
        private const byte FASE_CAZA = 1;
        private const byte FASE_MORDISCO = 2;
        private const byte FASE_DISIPAR = 3;

        private NPC Presa => Projectile.ai[0] <= 0 ? null : Main.npc[(int)Projectile.ai[0] - 1];

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 25;
            Projectile.extraUpdates = 2;
        }

        // ==================================================================
        //  LA VIDA (el motor del festín — intacto)
        // ==================================================================

        private Vector2 RaizDeSombra(Player dueño) => dueño.MountedCenter;

        private Vector2 SombraProyectada(Player dueño)
        {
            Vector2 c = dueño.MountedCenter + new Vector2(0, dueño.height * 0.45f);
            int tx = (int)(c.X / 16f), ty = (int)(c.Y / 16f);
            int pasos = 0;
            while (pasos < 34 && !WorldGen.SolidTile(tx, ty)) { ty++; pasos++; }
            Vector2 suelo = new Vector2(tx * 16f + 8f, ty * 16f - 4f);
            return pasos < 34 ? suelo : c;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            NPC presa = Presa;
            Player dueño = Main.player[Projectile.owner];
            byte fase = (byte)Projectile.ai[1];

            bool presaValida = presa != null && presa.active && presa.life > 0;
            if (fase != FASE_DISIPAR && !presaValida)
            {
                Projectile.ai[1] = FASE_DISIPAR;
                Projectile.ai[2] = 0;
                fase = FASE_DISIPAR;
                t = 0;
            }

            switch (fase)
            {
                case FASE_EMERGER:
                {
                    Vector2 raiz = RaizDeSombra(dueño);
                    Vector2 rumbo = presa != null
                        ? (presa.Center - raiz).SafeNormalize(-Vector2.UnitY)
                        : -Vector2.UnitY;
                    float avance = SombrasLib.DeGolpe(t / 26f);
                    Projectile.Center = raiz + rumbo * (40f + 150f * avance);
                    Projectile.rotation = rumbo.ToRotation();
                    if (t == 3) Sonar(SoundID.Item122.WithPitchOffset(-0.55f).WithVolumeScale(0.75f), Projectile.Center);
                    // EL IRIS SE CENTRA: el destello de enfoque (la pupila clava la vista)
                    if (t == 14 && Main.netMode != NetmodeID.Server)
                        Sonar(SoundID.Item4.WithPitchOffset(-0.3f).WithVolumeScale(0.5f), Projectile.Center);
                    if (t >= 26) { Projectile.ai[1] = FASE_CAZA; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_CAZA:
                {
                    if (presa != null && presaValida)
                    {
                        Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                        float dist = Vector2.Distance(presa.Center, Projectile.Center);
                        float vel = MathHelper.Clamp(dist * 0.05f, 18f, 46f);
                        float actual = Projectile.velocity.ToRotation();
                        float deseado = hacia.ToRotation();
                        float giro = MathHelper.WrapAngle(deseado - actual);
                        Projectile.velocity = (actual + MathHelper.Clamp(giro, -0.22f, 0.22f)).ToRotationVector2() * vel;
                        Projectile.rotation = deseado;

                        if (t % 15f == 0f && FaucesGlobalNPC.EsJefe(presa))
                            FaucesGlobalNPC.Marcar(presa, 3);

                        if (Projectile.Hitbox.Intersects(presa.Hitbox))
                        {
                            Projectile.ai[1] = FASE_MORDISCO;
                            Projectile.ai[2] = 0;
                            Sonar(SoundID.NPCHit9.WithPitchOffset(-0.4f).WithVolumeScale(0.9f), Projectile.Center);
                            Sonar(SoundID.Item122.WithPitchOffset(-0.65f).WithVolumeScale(0.6f), Projectile.Center);
                        }
                    }
                    if (t > 600) { Projectile.ai[1] = FASE_DISIPAR; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_MORDISCO:
                {
                    if (presa != null && presa.active && presa.life > 0)
                    {
                        if (FaucesGlobalNPC.EnFauces(presa))
                        {
                            Projectile.ai[1] = FASE_DISIPAR;
                            Projectile.ai[2] = 0;
                            break;
                        }
                        // EL IRIS SE PRENSA sobre el jefe (lo MIRA de cerca)
                        Projectile.Center = presa.Center;
                        Projectile.rotation = (presa.Center - RaizDeSombra(dueño)).ToRotation();

                        if (t % 5f == 0f && FaucesGlobalNPC.EsJefe(presa))
                            FaucesGlobalNPC.Marcar(presa, 3);

                        // EL DRENAJE DEL CUARTO TOMO: 3% cada 6 t (la pupila bebe)
                        if (Main.netMode != NetmodeID.MultiplayerClient && t > 18 && t % 6 == 0)
                        {
                            NPC dueñoPool = FaucesGlobalNPC.DueñoDelPool(presa);
                            if (dueñoPool != null && dueñoPool.active && dueñoPool.life > 1)
                            {
                                float quitar = MathF.Max(dueñoPool.life * 0.03f, 30f);
                                if (dueñoPool.life - quitar <= 1f)
                                {
                                    FaucesGlobalNPC.Iniciar(dueñoPool, 3);
                                    Projectile.ai[1] = FASE_DISIPAR;
                                    Projectile.ai[2] = 0;
                                    break;
                                }
                                dueñoPool.life -= (int)quitar;
                                dueñoPool.netUpdate = true;
                                if (t % 36 == 0)
                                    Sonar(SoundID.NPCHit9.WithPitchOffset(-0.1f).WithVolumeScale(0.5f), Projectile.Center);
                            }
                        }
                    }
                    else { Projectile.ai[1] = FASE_DISIPAR; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_DISIPAR:
                {
                    if (t == 1) Sonar(SoundID.Item122.WithPitchOffset(0.2f).WithVolumeScale(0.3f), Projectile.Center);
                    Projectile.velocity *= 0.92f;
                    if (t >= 52) Projectile.Kill();
                    break;
                }
            }

            if (Main.netMode != NetmodeID.Server && Projectile.ai[1] <= FASE_CAZA && Main.rand.NextBool(5))
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(28, 28), 56, 56,
                    DustID.Shadowflame, 0f, -0.3f, 128, default, 0.55f);
                Main.dust[d].noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.ai[1] != FASE_CAZA) return base.Colliding(projHitbox, targetHitbox);
            Player dueño = Main.player[Projectile.owner];
            Vector2 raiz = RaizDeSombra(dueño);
            Vector2[] col = SombrasLib.Columna(raiz, Projectile.Center,
                Main.GlobalTimeWrappedHourly, Projectile.whoAmI * 17 + 5, 10, 0.24f);
            float punto = 0f;
            for (int i = 1; i < col.Length; i++)
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                    col[i - 1], col[i], 10, ref punto))
                    return true;
            return false;
        }

        // ==================================================================
        //  EL DIBUJO — los nervios ópticos + EL IRIS COLOSAL
        // ==================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                DrawLosNervios();
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();

            // EL IRIS COLOSAL + SU PUPILA van en el LOTE VANILLA (el
            // contrato de los sprites, blindado: SIEMPRE lote vivo al salir).
            try { DibujarElIrisColosal(); }
            catch { VFXCore.ReabrirLoteVanilla(); }
            return false;
        }

        /// <summary>LOS DOS NERVIOS ÓPTICOS (cuerdas finas trenzadas — la
        /// espina doble del iris colgante).</summary>
        private Vector2[] Nervio(float tiempo, int semilla, Vector2 raiz, float faseTrenza)
        {
            Vector2[] col = SombrasLib.ColumnaViva(raiz, Projectile.Center, tiempo, semilla, 16, 0.2f, gancho: 0.42f);
            var nervio = new Vector2[col.Length];
            for (int i = 0; i < col.Length; i++)
            {
                float u = i / (float)(col.Length - 1);
                Vector2 dir = col[Math.Min(i + 1, col.Length - 1)] - col[Math.Max(i - 1, 0)];
                Vector2 perp = new Vector2(-dir.Y, dir.X);
                if (perp.LengthSquared() < 0.001f) perp = Vector2.UnitX;
                perp.Normalize();
                float giro = faseTrenza + u * 6.5f + tiempo * 1.6f;
                float radio = 7f * (0.3f + 0.7f * u) * (1f + 0.18f * MathF.Sin(tiempo * 2.1f + u * 5f));
                nervio[i] = col[i] + perp * (MathF.Cos(giro) * radio);
            }
            return nervio;
        }

        private void DrawLosNervios()
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 17 + 5;
            Player dueño = Main.player[Projectile.owner];
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];

            Vector2 raiz = RaizDeSombra(dueño);
            float disipa = fase == FASE_DISIPAR ? MathHelper.Clamp(1f - t / 52f, 0f, 1f) : 1f;
            float presencia = fase == FASE_EMERGER ? SombrasLib.DeGolpe(t / 26f) : 1f;

            SombrasLib.Charco(SombraProyectada(dueño), 50f, 0.5f * disipa, tiempo, semilla);

            // LOS DOS NERVIOS — finos (el iris cuelga de ellos)
            Vector2[] nA = Nervio(tiempo, semilla, raiz, 0f);
            Vector2[] nB = Nervio(tiempo, semilla + 31, raiz, MathHelper.Pi);
            SombrasLib.Masa(nA, 3.5f, 7f, 0.85f * disipa * presencia, semilla, tiempo, grosorCentro: 16f);
            SombrasLib.Masa(nB, 3.5f, 7f, 0.85f * disipa * presencia, semilla + 31, tiempo, grosorCentro: 16f);
            SombrasLib.BrumaColumna(nA, 34f, 0.38f * disipa * presencia, tiempo, semilla, 10);
            SombrasLib.BrumaColumna(nB, 34f, 0.38f * disipa * presencia, tiempo, semilla + 31, 10);

            // EL MORDISCO: LA CORONA DE GARRAS alrededor del jefe (las
            // pestañas del iris — el borde dentado del festín, en garra)
            NPC presa = Presa;
            if (fase == FASE_MORDISCO && presa != null && presa.active)
            {
                VFXCore.Begin();
                float pulso = 0.5f + 0.5f * MathF.Sin(tiempo * 5.5f);
                float radioCorona = MathHelper.Clamp(presa.Size.Length() * 0.7f, 90f, 280f);
                for (int k = 0; k < 9; k++)
                {
                    float ang = k * MathHelper.TwoPi / 9f + tiempo * 0.35f;
                    Vector2 baseG = presa.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                        (radioCorona * (1.16f - 0.12f * pulso));
                    Vector2 hacia = presa.Center - baseG;
                    SombrasLib.Garra(baseG, hacia, 30f * disipa, 0.75f * disipa,
                        gancho: 0.5f + 0.3f * pulso);
                }
                VFXCore.FlushAdditive();
            }
        }

        /// <summary>
        /// EL IRIS COLOSAL: el sprite del iris rojo ×(4..6) con LA PUPILA
        /// (disco negro en el lote vanilla ALFA — el único negro que
        /// oscurece de verdad) que se DILATA con el masticar y en el
        /// drenaje se abre EN BOCIO; los FILAMENTOS de tinta corren de la
        /// presa a la pupila.
        /// </summary>
        private void DibujarElIrisColosal()
        {
            NPC presa = Presa;
            Player dueño = Main.player[Projectile.owner];
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 17 + 5;
            float disipa = fase == FASE_DISIPAR ? MathHelper.Clamp(1f - t / 52f, 0f, 1f) : 1f;
            if (disipa <= 0.02f) return;

            Texture2D iris = ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambriento_IrisRojo").Value;
            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            if (iris == null || glow == null) return;

            float presencia = fase == FASE_EMERGER ? SombrasLib.DeGolpe(t / 26f) : 1f;

            // EL MASTICAR (el ciclo del festín — la pupila respira con él)
            float masticar = CicloMasticar(t);
            // LA DILATACIÓN: base + masticar; en MORDISCO la pupila SE ABRE
            float dilata = fase == FASE_MORDISCO
                ? MathHelper.Lerp(0.55f + 0.20f * masticar, 1.05f, MathHelper.Clamp(t / 40f, 0f, 1f))
                : 0.5f + 0.18f * masticar;

            // LA ESCALA DEL IRIS: grande y respirando (el coloso)
            float escIris = (4.2f + 0.5f * masticar) * presencia *
                (1f + 0.04f * MathF.Sin(tiempo * 2.6f));

            Vector2 pos = Projectile.Center;
            Vector2 pantalla = pos - Main.screenPosition;
            Color luzIris = Color.Lerp(Lighting.GetColor(pos.ToTileCoordinates()),
                new Color(232, 224, 244), 0.5f) * (disipa * presencia);

            // 1) EL HALO del coloso (aditivo, encolado — volcado único al final)
            VFXCore.Quad(pos, SombrasLib.Alfa(new Color(255, 120, 90), 0.16f * disipa * presencia),
                new Vector2(190f * escIris * 0.28f, 190f * escIris * 0.28f), 0f, VFXCore.SoftGlow);

            // 2) EL IRIS (vanilla — el sprite rojo gigante)
            Main.spriteBatch.Draw(iris, pantalla, null, luzIris, 0f,
                iris.Size() * 0.5f, escIris, SpriteEffects.None, 0f);

            // 3) LA PUPILA — el disco negro EN ALFA (vanilla): el vacío.
            //    SoftGlow tintado NEGRO en el lote alfa = el oscurecido
            //    real (esto el aditivo NO puede hacerlo).
            float escPupila = escIris * dilata;
            Main.spriteBatch.Draw(glow, pantalla, null,
                Microsoft.Xna.Framework.Color.Black * (0.92f * disipa * presencia), 0f,
                glow.Size() * 0.5f, escPupila * 0.55f, SpriteEffects.None, 0f);

            // 4) EL BRILLO de la mirada (la chispa del iris — vivo siempre)
            Vector2 destello = pantalla + new Vector2(
                -6f * escIris, -7f * escIris) * (1f - dilata * 0.4f);
            Main.spriteBatch.Draw(glow, destello, null,
                new Color(255, 240, 230, 200) * (0.75f * disipa * presencia), 0f,
                glow.Size() * 0.5f, escIris * 0.16f, SpriteEffects.None, 0f);

            // 5) LOS FILAMENTOS DEL DRENAJE: tinta que corre de la presa a
            //    la pupila (PerlinBolt de verdad — la esencia viaja)
            if (fase == FASE_MORDISCO && presa != null && presa.active && t > 18)
            {
                int flick = StormLib.FlickTick(tiempo, 13f);
                int hilos = 3;
                for (int k = 0; k < hilos; k++)
                {
                    int seedH = Projectile.whoAmI * 41 + k;
                    if (!StormLib.IsLit(seedH, flick, 0.65f)) continue;
                    Vector2 desde = presa.Center + new Vector2(
                        MathF.Cos(k * MathHelper.TwoPi / hilos + tiempo) * 18f,
                        MathF.Sin(k * MathHelper.TwoPi / hilos + tiempo) * 18f);
                    StormLib.PerlinBolt(Main.spriteBatch, desde - Main.screenPosition,
                        pantalla, seedH, flick, 2.6f,
                        new Color(255, 130, 100), new Color(255, 224, 214),
                        0.75f * disipa, 0.20f);
                }
            }

            // EL VOLCADO ÚNICO del halo (el contrato blindado)
            if (VFXCore.QuadCount > 0)
            {
                try { VFXCore.FlushAdditive(); }
                finally { VFXCore.ReabrirLoteVanilla(); }
            }
        }

        /// <summary>El masticar del iris (el ciclo del festín — la dilatación).</summary>
        private static float CicloMasticar(float t)
        {
            float m = (t - 18f) % 18f;
            if (m < 6f) return MathHelper.Lerp(0.25f, 1f, m / 6f);
            if (m < 10f) return MathHelper.Lerp(1f, 0.1f, SombrasLib.DeGolpe((m - 6f) / 4f));
            return 0.2f + 0.12f * MathF.Sin(m * 1.7f);
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
