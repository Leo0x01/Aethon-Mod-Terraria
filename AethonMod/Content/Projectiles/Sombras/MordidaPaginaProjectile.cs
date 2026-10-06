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
    /// MORDIDAPAGINAPROJECTILE — v6.50.69 — ARMA NUEVA 2: LA MORDIDA DE LA PÁGINA.
    ///
    /// La letra del usuario: «esas tres armas nuevas se ven y funcionan
    /// horrible» — la familia vuelve al ADN de La Sombra (tentáculo de
    /// carne + bruma) con un OFICIO distinto por hermana.
    ///
    /// LA MORDIDA ES EL GLOTÓN: un tentáculo CORTO Y GORDO (el doble de
    /// grueso que La Sombra) con una CABEZA DE BRUMA ENORME coronada de
    /// TRES GARRAS DE HUESO que se CIERRAN de golpe. Sale DEL JUGADOR,
    /// caza lento y pesado (no hay quién le gane el bocado) y al alcanzar
    /// a la presa SE CUELGA: cada 16 ticks UNA MORDIDA de verdad (golpe
    /// real con su número) — mastica, traga y vuelve por más. A un jefe
    /// le arranca pedazos enteros… y cuando le queda UNA mordida de vida:
    /// LA DEVORACIÓN — el festín estilo 9, LA MASTICACIÓN GIGANTE.
    ///
    /// 100% código: ni un sprite del arma. Cero dependencias.
    /// </summary>
    public class MordidaPaginaProjectile : ModProjectile
    {
        private const byte FASE_EMERGER = 0;
        private const byte FASE_CAZA = 1;
        private const byte FASE_MASTICA = 2;
        private const byte FASE_DISIPAR = 3;

        private NPC Presa => Projectile.ai[0] <= 0 ? null : Main.npc[(int)Projectile.ai[0] - 1];

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 56;
            Projectile.height = 56;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;    // CADA MORDICA es un golpe real
            Projectile.extraUpdates = 1;
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
                    Vector2 raiz = dueño.MountedCenter;
                    Vector2 rumbo = presa != null
                        ? (presa.Center - raiz).SafeNormalize(-Vector2.UnitY)
                        : -Vector2.UnitY;
                    float avance = SombrasLib.DeGolpe(t / 24f);
                    Projectile.Center = raiz + rumbo * (26f + 120f * avance);
                    Projectile.rotation = rumbo.ToRotation();
                    if (t == 3) Sonar(SoundID.Item122.WithPitchOffset(-0.55f).WithVolumeScale(0.8f), Projectile.Center);
                    if (t >= 24) { Projectile.ai[1] = FASE_CAZA; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_CAZA:
                {
                    if (presa != null && presaValida)
                    {
                        Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                        float dist = Vector2.Distance(presa.Center, Projectile.Center);
                        // LENTO Y PESADO (la mitad de rápida que La Sombra):
                        // el glotón no corre — REMA
                        float vel = MathHelper.Clamp(dist * 0.04f, 13f, 30f);
                        float actual = Projectile.velocity.ToRotation();
                        float deseado = hacia.ToRotation();
                        float giro = MathHelper.WrapAngle(deseado - actual);
                        Projectile.velocity = (actual + MathHelper.Clamp(giro, -0.16f, 0.16f)).ToRotationVector2() * vel;
                        Projectile.rotation = deseado;

                        // v6.50.69 — LA MARCA CONTINUA (la cura del «a veces
                        // no se activa»: la muerte del jefe por CUALQUIER
                        // fuente durante la caza es NUESTRA)
                        if (t % 15f == 0f && FaucesGlobalNPC.EsJefe(presa))
                            FaucesGlobalNPC.Marcar(presa, 9);

                        if (Projectile.Hitbox.Intersects(presa.Hitbox))
                        {
                            Projectile.ai[1] = FASE_MASTICA;
                            Projectile.ai[2] = 0;
                            Sonar(SoundID.NPCHit9.WithPitchOffset(-0.5f).WithVolumeScale(1f), Projectile.Center);
                        }
                    }
                    if (t > 600) { Projectile.ai[1] = FASE_DISIPAR; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_MASTICA:
                {
                    if (presa != null && presa.active && presa.life > 0)
                    {
                        Projectile.Center = presa.Center;
                        Projectile.rotation = (presa.Center - dueño.MountedCenter).ToRotation();

                        // v6.50.69 — LA MARCA CONTINUA mientras mastica
                        if (t % 5f == 0f && FaucesGlobalNPC.EsJefe(presa))
                            FaucesGlobalNPC.Marcar(presa, 9);

                        // EL TINTERO DE LA MORDIDA: cuando al jefe le queda
                        // UNA mordida de vida, NO se la damos — arranca LA
                        // DEVORACIÓN (el festín estilo 9): la letra del
                        // usuario — interceptarlo con 1 de vida y comérselo
                        if (Main.netMode != NetmodeID.MultiplayerClient && t > 10)
                        {
                            NPC dueñoPool = FaucesGlobalNPC.DueñoDelPool(presa);
                            if (dueñoPool != null && dueñoPool.active &&
                                FaucesGlobalNPC.EsJefe(dueñoPool) &&
                                dueñoPool.life > 1 && dueñoPool.life <= Projectile.damage + 2)
                            {
                                FaucesGlobalNPC.Iniciar(dueñoPool, 9);
                                Projectile.ai[1] = FASE_DISIPAR;
                                Projectile.ai[2] = 0;
                                break;
                            }
                        }

                        // el crujido de cada mordida (el golpe lo pone el
                        // motor con localNPCHitCooldown)
                        if (t % 16f == 0f)
                            Sonar(SoundID.NPCHit9.WithPitchOffset(-0.25f + 0.06f * ((t / 16f) % 3f)).WithVolumeScale(0.55f), Projectile.Center);
                    }
                    else { Projectile.ai[1] = FASE_DISIPAR; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_DISIPAR:
                {
                    if (t == 1) Sonar(SoundID.Item122.WithPitchOffset(0.25f).WithVolumeScale(0.3f), Projectile.Center);
                    Projectile.velocity *= 0.90f;
                    if (t >= 48) Projectile.Kill();
                    break;
                }
            }

            if (Main.netMode != NetmodeID.Server && Projectile.ai[1] <= FASE_CAZA && Main.rand.NextBool(5))
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(28, 28), 56, 56,
                    DustID.Shadowflame, 0f, -0.35f, 128, default, 0.6f);
                Main.dust[d].noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // cada MORDIDA marca: si ESTE bocado mata al jefe, es NUESTRO
            if (target.life <= 0 && FaucesGlobalNPC.EsJefe(target))
                FaucesGlobalNPC.Marcar(target, 9);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.ai[1] != FASE_CAZA && Projectile.ai[1] != FASE_MASTICA)
                return base.Colliding(projHitbox, targetHitbox);
            Player dueño = Main.player[Projectile.owner];
            Vector2 raiz = dueño.MountedCenter;
            Vector2[] col = SombrasLib.Columna(raiz, Projectile.Center,
                Main.GlobalTimeWrappedHourly, Projectile.whoAmI * 19 + 3, 10, 0.20f);
            float punto = 0f;
            for (int i = 1; i < col.Length; i++)
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                    col[i - 1], col[i], 12, ref punto))
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
            int semilla = Projectile.whoAmI * 19 + 3;
            Player dueño = Main.player[Projectile.owner];
            NPC presa = Presa;
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];

            Vector2 raiz = dueño.MountedCenter;
            float disipa = fase == FASE_DISIPAR ? MathHelper.Clamp(1f - t / 48f, 0f, 1f) : 1f;

            // === EL CUERPO GORDO: CORTO, MUSCULOSO, sin gancho (el glotón
            // va DERECHO a la boca — el drama lo pone la cabeza)
            Vector2[] col = SombrasLib.ColumnaViva(raiz, Projectile.Center, tiempo, semilla, 12, 0.16f, gancho: 0f);
            SombrasLib.Masa(col, 62f, 28f, 0.96f * disipa, semilla, tiempo);

            // === LA BRUMA DEL GLOTÓN — DOS CAPAS GORDAS (más bruma aún que
            // La Sombra: este EXHALA al masticar)
            SombrasLib.BrumaColumna(col, 54f, 0.62f * disipa, tiempo, semilla, 13);
            SombrasLib.BrumaColumna(col, 36f, 0.46f * disipa, tiempo, semilla + 71, 9);

            // === LA CABEZA GIGANTE — bruma ENORME (la forma actual, al
            // doble de vigor) + LAS TRES GARRAS QUE CIERRAN
            Vector2 punta = col[col.Length - 1];
            Vector2 rumbo = (col[col.Length - 1] - col[Math.Max(col.Length - 3, 0)])
                .SafeNormalize(Projectile.rotation.ToRotationVector2());
            float muerde = fase == FASE_MASTICA ? CicloMorder(t) : 0.7f;
            SombrasLib.CabezaDeBruma(punta, rumbo, (0.9f + 0.3f * muerde) * disipa, disipa, tiempo, semilla);

            // LAS GARRAS DE HUESO — tres, en abanico frontal, que se CIERRAN
            // de golpe con cada mordida (el aplastador)
            float abre = 1f - muerde;      // 1 = abiertas · 0 = CERRADAS
            for (int k = -1; k <= 1; k++)
                SombrasLib.Garra(punta + rumbo * 8f - rumbo.RotatedBy(k * 0.5f) * 10f,
                    rumbo.RotatedBy(k * (0.55f + 0.5f * abre)),
                    52f + 22f * (1f - abre), 0.9f * disipa,
                    k * 0.6f);

            // === AL MASTICAR: la HERIDA late dentro de la boca y saltan
            // MIGAS de hueso (el festín cotidiano del glotón)
            if (fase == FASE_MASTICA && presa != null && presa.active)
            {
                VFXCore.Begin();
                VFXCore.Quad(punta, SombrasLib.Alfa(SombrasLib.Rojo, (0.35f + 0.25f * muerde) * disipa),
                    new Vector2(90f, 90f), rumbo.ToRotation());
                VFXCore.FlushAdditive();

                if (presa.life <= presa.lifeMax * 0.5f)
                    SombrasLib.Devorador(punta, presa, 0.45f, disipa * 0.8f, tiempo, semilla, col, trago: 0f);
            }

            // === LOS OJOS — pocos y GRANDES (el glotón solo mira lo que come)
            Vector2 objetivo = presa != null && presa.active ? presa.Center
                : dueño.MountedCenter + Projectile.velocity * 30f;
            SombrasLib.OjosDeMasa(col, objetivo, disipa, semilla, tiempo, 3);
        }

        /// <summary>El ciclo de la mordida del glotón: abre… ¡CIERRA! …masca.</summary>
        private static float CicloMorder(float t)
        {
            float m = t % 16f;
            if (m < 5f) return MathHelper.Lerp(0.25f, 1f, m / 5f);                          // abre
            if (m < 8f) return MathHelper.Lerp(1f, 0.05f, SombrasLib.DeGolpe((m - 5f) / 3f)); // ¡CIERRA!
            return 0.2f + 0.12f * MathF.Sin(m * 1.8f);                                       // masca
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
