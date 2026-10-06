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
    /// MAREAPAGINAPROJECTILE — v6.50.66 — LA MAREA DE LA PÁGINA.
    ///
    /// La primera de las TRES HERMANAS de La Sombra de la Página
    /// (v6.50.66: el usuario jubiló a las 3 armas viejas — la pinza del
    /// gif, el tajo y el libro — y pidió COPIAR a La Sombra de la
    /// Página y darle a cada copia «más personalidad, más efectos
    /// visuales, más sabor, más alma, más de todo»).
    ///
    /// LA MAREA ES EL OCÉANO DE LA PÁGINA: la misma sombra del suelo,
    /// pero hecha AGUA VIVA — no un tentáculo: TRES CABEZAS-CRESTA
    /// mordiendo en OLEADA (una en el centro y una por flanko, con el
    /// ciclo de mordida DESFAZADO — la marea no mastica: TRAGA por
    /// olas), el charco con ANILLOS DE OLEAJE que se expanden, NIEBLA
    /// RASANTE en el suelo (el banco de bruma de la orilla), PULSOS que
    /// suben por el cuerpo como el vaivén del mar y SPRAY de gotas
    /// negras cuando las fauces CIERRAN. Nada por el aire TEJIÉNDOSE
    /// (serpenteo suave de ola).
    ///
    /// Más bruma que la base (16 puffs de columna + 3 nubes de suelo),
    /// dren más rápido y más suave (2,2%/5t — la marea mordisquea) y
    /// su festín es el ESTILO 5: la OLA QUE SUBE (el adorno de marea
    /// del motor del festín).
    /// </summary>
    public class MareaPaginaProjectile : ModProjectile
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

        /// <summary>La sombra del suelo: raycast hacia abajo desde el portador.</summary>
        private Vector2 RaizDeSombra(Player dueño)
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
                    // LA MAREA NO BROTA: PRIMERO HINCHA (t<10 el charco
                    // crece — la resaca) y LUEGO LAS CABEZAS SUBEN
                    Vector2 raiz = RaizDeSombra(dueño);
                    Vector2 rumbo = presa != null
                        ? (presa.Center - raiz).SafeNormalize(-Vector2.UnitY)
                        : -Vector2.UnitY;
                    float hincha = SombrasLib.DeGolpe(MathHelper.Clamp(t / 10f, 0f, 1f));
                    float sube = SombrasLib.DeGolpe(MathHelper.Clamp((t - 10f) / 24f, 0f, 1f));
                    float avance = hincha * 0.25f + sube * 0.75f;
                    Projectile.Center = raiz + rumbo * (24f + 168f * avance);
                    Projectile.rotation = rumbo.ToRotation();
                    if (t == 6) Sonar(SoundID.Item122.WithPitchOffset(-0.55f).WithVolumeScale(0.8f), Projectile.Center);
                    if (t >= 34) { Projectile.ai[1] = FASE_CAZA; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_CAZA:
                {
                    if (presa != null && presaValida)
                    {
                        Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                        float dist = Vector2.Distance(presa.Center, Projectile.Center);
                        float vel = MathHelper.Clamp(dist * 0.048f, 18f, 48f);
                        float actual = Projectile.velocity.ToRotation();
                        float deseado = hacia.ToRotation();
                        float giro = MathHelper.WrapAngle(deseado - actual);
                        Projectile.velocity = (actual + MathHelper.Clamp(giro, -0.24f, 0.24f)).ToRotationVector2() * vel;
                        Projectile.rotation = deseado;

                        // EL SERPENTEO DE LA OLA: la marea no va recta —
                        // TEJE, como una ola que se derrama por el aire
                        Vector2 perp = new(-hacia.Y, hacia.X);
                        Projectile.position += perp * MathF.Sin(t * 0.26f) * 1.05f;

                        if (Projectile.Hitbox.Intersects(presa.Hitbox))
                        {
                            Projectile.ai[1] = FASE_MORDISCO;
                            Projectile.ai[2] = 0;
                            Sonar(SoundID.NPCHit9.WithPitchOffset(-0.4f).WithVolumeScale(0.9f), Projectile.Center);
                        }
                    }
                    if (t > 600) { Projectile.ai[1] = FASE_DISIPAR; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_MORDISCO:
                {
                    if (presa != null && presa.active && presa.life > 0)
                    {
                        Projectile.Center = presa.Center;
                        Projectile.rotation = (presa.Center - RaizDeSombra(dueño)).ToRotation();

                        // LA MAREA MORDISQUEA MÁS RÁPIDO Y MÁS SUAVE
                        // (2,2%/5t — tragaderas de olita)
                        if (Main.netMode != NetmodeID.MultiplayerClient && t > 16 && t % 5 == 0)
                        {
                            NPC dueñoPool = FaucesGlobalNPC.DueñoDelPool(presa);
                            if (dueñoPool != null && dueñoPool.active && dueñoPool.life > 1)
                            {
                                float quitar = MathF.Max(dueñoPool.life * 0.022f, 22f);
                                if (dueñoPool.life - quitar <= 1f)
                                {
                                    FaucesGlobalNPC.Iniciar(dueñoPool, 5);
                                    Projectile.ai[1] = FASE_DISIPAR;
                                    Projectile.ai[2] = 0;
                                    break;
                                }
                                dueñoPool.life -= (int)quitar;
                                dueñoPool.netUpdate = true;
                                if (t % 30 == 0)
                                    Sonar(SoundID.NPCHit9.WithPitchOffset(-0.15f).WithVolumeScale(0.5f), Projectile.Center);
                            }
                        }
                    }
                    else { Projectile.ai[1] = FASE_DISIPAR; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_DISIPAR:
                {
                    if (t == 1) Sonar(SoundID.Item122.WithPitchOffset(0.15f).WithVolumeScale(0.3f), Projectile.Center);
                    Projectile.velocity *= 0.92f;
                    if (t >= 44) Projectile.Kill();
                    break;
                }
            }

            if (Main.netMode != NetmodeID.Server && Projectile.ai[1] <= FASE_CAZA && Main.rand.NextBool(5))
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(26, 26), 52, 52,
                    DustID.Shadowflame, 0f, -0.25f, 128, default, 0.6f);
                Main.dust[d].noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.ai[1] != FASE_CAZA) return base.Colliding(projHitbox, targetHitbox);
            Player dueño = Main.player[Projectile.owner];
            Vector2 raiz = RaizDeSombra(dueño);
            Vector2[] col = SombrasLib.Columna(raiz, Projectile.Center,
                Main.GlobalTimeWrappedHourly, Projectile.whoAmI * 23 + 7, 10, 0.26f);
            float punto = 0f;
            for (int i = 1; i < col.Length; i++)
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                    col[i - 1], col[i], 8, ref punto))
                    return true;
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            VFXCore.CerrarLoteSiAbierto();
            try { DrawTodo(); }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        private void DrawTodo()
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 23 + 7;
            Player dueño = Main.player[Projectile.owner];
            NPC presa = Presa;
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];

            Vector2 raiz = RaizDeSombra(dueño);
            float disipa = fase == FASE_DISIPAR ? MathHelper.Clamp(1f - t / 44f, 0f, 1f) : 1f;
            float presencia = fase == FASE_EMERGER
                ? SombrasLib.DeGolpe(MathHelper.Clamp(t / 10f, 0f, 1f))
                : 1f;

            // === EL CHARCO OCÉANO (más ancho: la orilla de la página)
            // + LOS ANILLOS DE OLEAJE que se expanden y mueren ===
            SombrasLib.Charco(raiz, 74f * presencia, 0.82f * disipa, tiempo, semilla);
            if (presencia > 0.3f)
            {
                VFXCore.Begin();
                for (int j = 0; j < 3; j++)
                {
                    float ciclo = SombrasLib.Frac(tiempo * 0.13f + j / 3f);
                    float radio = 34f + ciclo * 100f;
                    VFXCore.Quad(raiz, SombrasLib.Alfa(SombrasLib.Negro, 0.42f * (1f - ciclo) * presencia * disipa),
                        VFXCore.RingQuadSize(radio), 0f, VFXCore.Ring);
                }
                VFXCore.FlushAlpha();
            }

            // === LA COLUMNA GORDA (el oleaje entero, no una soga) ===
            // v6.50.67 — VIVA: la ola con inercia de verdad + el gancho
            // que SE ENROLLA (la cresta de la ola que se derrama)
            Vector2[] col = SombrasLib.ColumnaViva(raiz, Projectile.Center, tiempo, semilla, 22, 0.26f, gancho: -0.6f);
            SombrasLib.Masa(col, 56f * presencia, 15f, 0.95f * disipa, semilla, tiempo);

            // === LOS PULSOS DE LA MAREA: dos brillos que SUBEN por el
            // cuerpo como el vaivén del mar (la ola viaja por dentro) ===
            if (fase != FASE_EMERGER)
            {
                VFXCore.Begin();
                for (int j = 0; j < 2; j++)
                {
                    float u = SombrasLib.Frac(tiempo * 0.22f + j * 0.5f);
                    int idx = Math.Min((int)(u * (col.Length - 1)), col.Length - 1);
                    VFXCore.Quad(col[idx], SombrasLib.Alfa(SombrasLib.Violeta, 0.15f * disipa),
                        new Vector2(58f, 50f));
                }
                VFXCore.FlushAdditive();
            }

            // === LA BRUMA — EL BANCO DE NIEBLA DE LA ORILLA (v6.50.66:
            // «se necesita más bruma») — 16 puffs de columna + TRES
            // NUBES RASANTES pegadas al suelo alrededor del charco ===
            SombrasLib.BrumaColumna(col, 46f * presencia, 0.5f * disipa, tiempo, semilla, 16);
            SombrasLib.Bruma(raiz + new Vector2(-78f, -16f), 44f, 0.5f * presencia * disipa, tiempo, semilla + 11);
            SombrasLib.Bruma(raiz + new Vector2(84f, -14f), 40f, 0.45f * presencia * disipa, tiempo, semilla + 29);
            SombrasLib.Bruma(raiz + new Vector2(0f, -26f), 54f, 0.55f * presencia * disipa, tiempo, semilla + 47);

            // === LOS OJOS DEL MAR (pocos: el mar es más boca que mirada) ===
            Vector2 objetivo = presa != null && presa.active
                ? presa.Center
                : dueño.MountedCenter + Projectile.velocity * 30f;
            SombrasLib.OjosDeMasa(col, objetivo, presencia * disipa, semilla, tiempo, 5);

            // === LAS TRES CABEZAS-CRESTA: la del centro manda, las de
            // los flancos mueren DESFAZADAS — la marea TRAGA por olas ===
            Vector2 rumbo = Projectile.rotation.ToRotationVector2();
            Vector2 perp = new(-rumbo.Y, rumbo.X);
            float baseAbierta = fase switch
            {
                FASE_EMERGER => 0.4f + 0.4f * SombrasLib.DeGolpe(MathHelper.Clamp((t - 10f) / 24f, 0f, 1f)),
                FASE_CAZA => 0.85f + 0.15f * MathF.Sin(tiempo * 8f),
                FASE_MORDISCO => CicloMordida(t),
                _ => 0.5f * disipa,
            };
            float escalaCabeza = fase == FASE_EMERGER
                ? SombrasLib.DeGolpe(MathHelper.Clamp((t - 10f) / 24f, 0f, 1f)) : 1f;

            Vector2 headC = Projectile.Center + rumbo * 12f;
            Vector2 headA = Projectile.Center + perp * 36f + rumbo * 4f;
            Vector2 headB = Projectile.Center - perp * 36f + rumbo * 4f;

            float abreC = baseAbierta;
            float abreA = fase == FASE_MORDISCO ? CicloMordida(t - 7.3f) : baseAbierta;
            float abreB = fase == FASE_MORDISCO ? CicloMordida(t - 14.6f) : baseAbierta;

            SombrasLib.Fauces(headC, rumbo, abreC * disipa * escalaCabeza, 76f * escalaCabeza, semilla);
            SombrasLib.Fauces(headA, (rumbo + perp * 0.38f).SafeNormalize(rumbo),
                abreA * disipa * escalaCabeza, 60f * escalaCabeza, semilla + 3);
            SombrasLib.Fauces(headB, (rumbo - perp * 0.38f).SafeNormalize(rumbo),
                abreB * disipa * escalaCabeza, 60f * escalaCabeza, semilla + 9);

            // las tres gargantas laten al morder
            if (fase == FASE_MORDISCO)
            {
                VFXCore.Begin();
                VFXCore.Quad(headC, SombrasLib.Alfa(SombrasLib.Rojo, 0.5f * disipa), new Vector2(110f, 110f), Projectile.rotation);
                VFXCore.Quad(headA, SombrasLib.Alfa(SombrasLib.Rojo, 0.4f * disipa), new Vector2(84f, 84f), Projectile.rotation);
                VFXCore.Quad(headB, SombrasLib.Alfa(SombrasLib.Rojo, 0.4f * disipa), new Vector2(84f, 84f), Projectile.rotation);
                VFXCore.FlushAdditive();
            }

            // el aliento de las tres bocas (más bruma de boca que la base)
            SombrasLib.BrumaBoca(headC, rumbo, abreC * disipa, 0.5f * disipa, tiempo, semilla + 5, 5);
            if (escalaCabeza > 0.5f)
            {
                SombrasLib.BrumaBoca(headA, (rumbo + perp * 0.38f).SafeNormalize(rumbo),
                    abreA * disipa, 0.4f * disipa, tiempo, semilla + 13, 4);
                SombrasLib.BrumaBoca(headB, (rumbo - perp * 0.38f).SafeNormalize(rumbo),
                    abreB * disipa, 0.4f * disipa, tiempo, semilla + 21, 4);
            }

            // === EL SPRAY: gotas negras que saltan cuando las fauces
            // CIERRAN (el chapoteo de la mordida — determinista) ===
            if (fase == FASE_MORDISCO && disipa > 0.5f)
            {
                float m = t % 22f;
                if (m < 8f)
                {
                    VFXCore.Begin();
                    for (int k = 0; k < 8; k++)
                    {
                        float f0 = SombrasLib.Frac(SombrasLib.semille(semilla + k) * 0.71f + k * 0.618034f);
                        float edad = SombrasLib.Frac(tiempo * (1.6f + f0) + f0 * 9f);
                        if (edad > 0.6f) continue;
                        float ang = -MathHelper.PiOver2 + (f0 - 0.5f) * 2.4f;
                        Vector2 dir = new(MathF.Cos(ang), MathF.Sin(ang));
                        Vector2 origen = k % 3 == 0 ? headC : k % 3 == 1 ? headA : headB;
                        Vector2 pos = origen + dir * (12f + 52f * edad)
                            + new Vector2(0f, 70f * edad * edad);
                        float alfa = (1f - edad / 0.6f) * 0.8f;
                        VFXCore.Quad(pos, SombrasLib.Alfa(SombrasLib.Negro, alfa), new Vector2(6f, 6f), 0f, VFXCore.Pixel);
                    }
                    VFXCore.FlushAlpha();
                }
            }

            // la disipación devuelve el agua a la página
            if (fase == FASE_DISIPAR && t < 20f)
                SombrasLib.Bruma(raiz, 62f, 0.5f * (1f - t / 20f), tiempo, semilla + 61);
        }

        private static float CicloMordida(float t)
        {
            if (t < 0f) t += 22f * MathF.Ceiling(-t / 22f);
            float m = t % 22f;
            if (m < 5f) return MathHelper.Lerp(0.9f, 0.12f, SombrasLib.DeGolpe(m / 5f));
            if (m < 12f) return MathHelper.Lerp(0.12f, 0.95f, (m - 5f) / 7f);
            return 0.9f + 0.1f * MathF.Sin(m * 1.1f);
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
