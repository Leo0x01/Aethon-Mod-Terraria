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
    /// MIRADAPAGINAPROJECTILE — v6.50.66 — LA MIRADA DE LA PÁGINA.
    ///
    /// La segunda hermana de La Sombra de la Página (v6.50.66 — el
    /// usuario jubiló a las 3 armas viejas y pidió 3 COPIAS de la
    /// Sombra con «más personalidad, más efectos, más sabor, más
    /// alma»). Cada copia es un HAMBRE distinto de la página.
    ///
    /// LA MIRADA ES LA PÁGINA QUE TE MIRA: la misma sombra del suelo,
    /// pero TODA OJOS — no cinco: UNA PARED de catorce ojos en dos
    /// filas que parpadean EN OLEADAS (uno tras otro, como luces que
    /// se apagan por un pasillo), todos con la pupila CLAVADA en la
    /// presa… y encima de la cabeza, EL OJO COLOSAL — medio cerrado
    /// mientras caza (te vigila), ABIERTO DE GOLPE al morder (te
    /// juzga), con la pupila que ENGORDA con cada tragada. Durante la
    /// caza, TRES LÍNEAS DE MIRADA se tienden desde los ojos hasta la
    /// presa (la promesa violeta), y al morder la sala SE ENCIENDE en
    /// viñeta (el mundo se hace borde) y de los ojos caen LÁGRIMAS DE
    /// TINTA. Muerde LENTO y HONDO (2,8%/7t): la mirada no devora —
    /// SENTENCIA. Festín ESTILO 6 (el ojo del juicio).
    /// </summary>
    public class MiradaPaginaProjectile : ModProjectile
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
            Projectile.width = 56;
            Projectile.height = 56;
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
                    // LA MIRADA NO BROTA: LOS OJOS SE ABREN PRIMERO (la
                    // página YA estaba mirando) y la masa sube DESPUÉS
                    Vector2 raiz = RaizDeSombra(dueño);
                    Vector2 rumbo = presa != null
                        ? (presa.Center - raiz).SafeNormalize(-Vector2.UnitY)
                        : -Vector2.UnitY;
                    float sube = SombrasLib.DeGolpe(MathHelper.Clamp((t - 8f) / 22f, 0f, 1f));
                    Projectile.Center = raiz + rumbo * (26f + 168f * sube);
                    Projectile.rotation = rumbo.ToRotation();
                    if (t == 2) Sonar(SoundID.Item122.WithPitchOffset(-0.25f).WithVolumeScale(0.7f), Projectile.Center);
                    if (t >= 30) { Projectile.ai[1] = FASE_CAZA; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_CAZA:
                {
                    if (presa != null && presaValida)
                    {
                        // LA MIRADA CAZA DESPACIO Y SIN DESVIARSE: el que
                        // mira ya sabe a dónde vas a estar
                        Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                        float dist = Vector2.Distance(presa.Center, Projectile.Center);
                        float vel = MathHelper.Clamp(dist * 0.042f, 16f, 44f);
                        float actual = Projectile.velocity.ToRotation();
                        float deseado = hacia.ToRotation();
                        float giro = MathHelper.WrapAngle(deseado - actual);
                        Projectile.velocity = (actual + MathHelper.Clamp(giro, -0.2f, 0.2f)).ToRotationVector2() * vel;
                        Projectile.rotation = deseado;

                        if (Projectile.Hitbox.Intersects(presa.Hitbox))
                        {
                            Projectile.ai[1] = FASE_MORDISCO;
                            Projectile.ai[2] = 0;
                            Sonar(SoundID.NPCHit9.WithPitchOffset(-0.3f).WithVolumeScale(0.9f), Projectile.Center);
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

                        // LA MIRADA MORDER LENTO Y HONDO (2,8%/7t): la
                        // sentencia no se apura
                        if (Main.netMode != NetmodeID.MultiplayerClient && t > 20 && t % 7 == 0)
                        {
                            NPC dueñoPool = FaucesGlobalNPC.DueñoDelPool(presa);
                            if (dueñoPool != null && dueñoPool.active && dueñoPool.life > 1)
                            {
                                float quitar = MathF.Max(dueñoPool.life * 0.028f, 28f);
                                if (dueñoPool.life - quitar <= 1f)
                                {
                                    FaucesGlobalNPC.Iniciar(dueñoPool, 6);
                                    Projectile.ai[1] = FASE_DISIPAR;
                                    Projectile.ai[2] = 0;
                                    break;
                                }
                                dueñoPool.life -= (int)quitar;
                                dueñoPool.netUpdate = true;
                                if (t % 42 == 0)
                                    Sonar(SoundID.NPCHit9.WithPitchOffset(-0.05f).WithVolumeScale(0.55f), Projectile.Center);
                            }
                        }
                    }
                    else { Projectile.ai[1] = FASE_DISIPAR; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_DISIPAR:
                {
                    if (t == 1) Sonar(SoundID.Item122.WithPitchOffset(0.25f).WithVolumeScale(0.3f), Projectile.Center);
                    Projectile.velocity *= 0.92f;
                    if (t >= 44) Projectile.Kill();
                    break;
                }
            }

            if (Main.netMode != NetmodeID.Server && Projectile.ai[1] <= FASE_CAZA && Main.rand.NextBool(6))
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(24, 24), 48, 48,
                    DustID.Shadowflame, 0f, -0.3f, 128, default, 0.5f);
                Main.dust[d].noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.ai[1] != FASE_CAZA) return base.Colliding(projHitbox, targetHitbox);
            Player dueño = Main.player[Projectile.owner];
            Vector2 raiz = RaizDeSombra(dueño);
            Vector2[] col = SombrasLib.Columna(raiz, Projectile.Center,
                Main.GlobalTimeWrappedHourly, Projectile.whoAmI * 29 + 11, 10, 0.24f);
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
            int semilla = Projectile.whoAmI * 29 + 11;
            Player dueño = Main.player[Projectile.owner];
            NPC presa = Presa;
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];

            Vector2 raiz = RaizDeSombra(dueño);
            float disipa = fase == FASE_DISIPAR ? MathHelper.Clamp(1f - t / 44f, 0f, 1f) : 1f;
            // LA MIRADA SE APAGA AL REVÉS: los ojos se CIERRAN primero
            // (en la emergencia están ABIERTOS desde el instante cero:
            // la página YA estaba mirando antes de levantarse)
            float presencia = fase == FASE_DISIPAR
                ? 1f - SombrasLib.DeGolpe(MathHelper.Clamp(t / 22f, 0f, 1f))
                : 1f;

            // === EL CHARCO (los ojos del suelo ya miran hacia arriba) ===
            SombrasLib.Charco(raiz, 54f, 0.8f * disipa, tiempo, semilla);

            // === LA COLUMNA ===
            // v6.50.67 — VIVA: la mirada se DESLIZA con inercia (el ojo
            // flota, no teletransporta) + gancho suave
            Vector2[] col = SombrasLib.ColumnaViva(raiz, Projectile.Center, tiempo, semilla, 20, 0.24f, gancho: 0.35f);
            SombrasLib.Masa(col, 42f, 13f, 0.95f * disipa, semilla, tiempo);

            // === LA BRUMA — columna ALTA (14 puffs) + DOS NUBES que
            // envuelven el cuerpo (v6.50.66: «se necesita más bruma») ===
            SombrasLib.BrumaColumna(col, 40f, 0.48f * disipa, tiempo, semilla, 14);
            SombrasLib.Bruma(col[Math.Max(1, col.Length / 3)], 62f, 0.32f * disipa, tiempo, semilla + 13);
            SombrasLib.Bruma(col[Math.Min(col.Length - 2, (col.Length * 2) / 3)], 50f, 0.26f * disipa, tiempo, semilla + 37);

            // === LA PARED DE OJOS: catorce, en dos filas, parpadeando
            // EN OLEADAS, todos clavados en la presa. En la mordida:
            // TODOS ENTRECIERRAN (la furia del que ya no solo mira) ===
            Vector2 objetivo = presa != null && presa.active
                ? presa.Center
                : dueño.MountedCenter + Projectile.velocity * 30f;
            bool mordiendo = fase == FASE_MORDISCO;
            float enojada = mordiendo ? 0.35f : 1f;

            Vector2[] posOjos = new Vector2[14];
            VFXCore.Begin();
            for (int k = 0; k < 14; k++)
            {
                float f0 = SombrasLib.Frac(SombrasLib.semille(semilla) * 0.43f + k * 0.618034f);
                float t01 = 0.16f + 0.68f * f0;
                int idx = (int)(t01 * (col.Length - 1));
                Vector2 seg = col[Math.Min(idx + 1, col.Length - 1)] - col[Math.Max(idx - 1, 0)];
                Vector2 perpL = seg.LengthSquared() < 0.01f ? Vector2.UnitY
                    : new Vector2(-seg.Y, seg.X) * (1f / MathF.Sqrt(seg.LengthSquared()));
                float lado = k % 2 == 0 ? 1f : -1f;
                posOjos[k] = col[idx] + perpL * (lado * 20f);
                // apertura escalonada (nacen de golpe, uno tras otro)…
                float local = presencia * 14f * 0.8f - k * 0.8f;
                float abierto = MathHelper.Clamp(local, 0f, 1f);
                // …el PARPADEO EN OLEADA: cada ojo se cierra un instante
                // en su propio compás (la ola de párpados)…
                float ciclo = SombrasLib.Frac(tiempo * 0.33f + f0 * 5.1f);
                if (ciclo < 0.07f) abierto *= 0.12f;
                // …y la furia entrecierra todos al morder
                abierto *= enojada;
                abierto *= 0.85f + 0.15f * MathF.Sin(tiempo * 3.1f + k * 2.4f);
                if (abierto <= 0.05f) continue;
                // v6.50.67 — UN TERCIO DE LOS OJOS SON RASGADOS (el dragón
                // de la idea central): la pared no repite el mismo ojo —
                // HUMANOS y DRACÓNICOS mirando a la presa
                SombrasLib.Ojo(posOjos[k], 13f + 7f * SombrasLib.Frac(f0 * 6.8f), objetivo - posOjos[k], abierto,
                    rasgada: k % 3 == 0);
            }
            VFXCore.FlushAdditive();

            // === LAS LÍNEAS DE MIRADA: tres promesas violetas que se
            // tienden de los ojos a la presa (el telegraph) ===
            if (presa != null && presa.active && disipa > 0.4f)
            {
                VFXCore.Begin();
                float grosor = mordiendo ? 2.2f : 1.4f;
                float aLinea = (mordiendo ? 0.16f : 0.09f) * disipa;
                for (int k = 0; k < 3; k++)
                {
                    Vector2 de = posOjos[k * 5];
                    VFXCore.Line(de, objetivo, SombrasLib.Alfa(SombrasLib.Violeta, aLinea), grosor);
                }
                VFXCore.FlushAdditive();
            }

            // === LA CABEZA: FAUCES + EL OJO COLOSAL ENCIMA ===
            float apertura = fase switch
            {
                FASE_EMERGER => 0.35f + 0.4f * SombrasLib.DeGolpe(MathHelper.Clamp((t - 8f) / 22f, 0f, 1f)),
                FASE_CAZA => 0.85f + 0.15f * MathF.Sin(tiempo * 8f),
                FASE_MORDISCO => CicloMordida(t),
                _ => 0.5f * disipa,
            };
            Vector2 rumbo = Projectile.rotation.ToRotationVector2();
            SombrasLib.Fauces(Projectile.Center, rumbo, apertura * disipa, 74f, semilla);
            SombrasLib.BrumaBoca(Projectile.Center, rumbo, apertura * disipa, 0.5f * disipa, tiempo, semilla + 5, 5);

            if (fase == FASE_MORDISCO)
            {
                VFXCore.Begin();
                VFXCore.Quad(Projectile.Center, SombrasLib.Alfa(SombrasLib.Rojo, 0.5f * disipa),
                    new Vector2(120f, 120f), Projectile.rotation);
                VFXCore.FlushAdditive();
            }

            // === EL OJO COLOSAL: medio cerrado en la caza (te vigila),
            // ABIERTO DE GOLPE al morder (te juzga) — y su pupila
            // ENGORDA con cada tragada (la sentencia se escribe) ===
            Vector2 posGrande = Projectile.Center - rumbo * 26f + new Vector2(0f, -54f);
            float abreGrande = mordiendo
                ? SombrasLib.DeGolpe(MathHelper.Clamp(t / 18f, 0f, 1f))
                : fase == FASE_CAZA ? 0.5f + 0.08f * MathF.Sin(tiempo * 2.6f)
                : fase == FASE_EMERGER ? 0.3f * SombrasLib.DeGolpe(t / 30f)
                : 0.5f * disipa;
            // al disipar: PARPADEA y se cierra (el último vistazo)
            if (fase == FASE_DISIPAR && t > 22f) abreGrande *= 1f - SombrasLib.DeGolpe((t - 22f) / 22f);
            abreGrande *= disipa;
            SombrasLib.Ojo(posGrande, 66f, objetivo - posGrande, abreGrande);
            if (mordiendo && abreGrande > 0.5f)
            {
                // LA PUPILA QUE ENGORDA: cada mordida la hace crecer
                float tragadas = MathF.Min(t / 7f, 10f) / 10f;
                Vector2 m = (objetivo - posGrande).LengthSquared() < 0.01f ? Vector2.Zero
                    : Vector2.Normalize(objetivo - posGrande) * 12f;
                VFXCore.Begin();
                VFXCore.Quad(posGrande + m, SombrasLib.Alfa(SombrasLib.Rojo, 0.85f * disipa),
                    new Vector2(14f + 20f * tragadas, 16f + 22f * tragadas));
                VFXCore.FlushAdditive();
            }

            // === LAS LÁGRIMAS DE TINTA: al morder, los ojos lloran
            // NEGRO (la tinta de la página que se corre) ===
            if (mordiendo && disipa > 0.5f)
            {
                VFXCore.Begin();
                for (int k = 0; k < 3; k++)
                {
                    Vector2 ojo = posOjos[k * 4 + 1];
                    float edad = SombrasLib.Frac(tiempo * 0.4f + k * 0.33f);
                    if (edad > 0.55f) continue;
                    Vector2 pos = ojo + new Vector2(
                        MathF.Sin(edad * 9f + k * 2f) * 4f, 6f + 92f * edad * edad);
                    VFXCore.Quad(pos, SombrasLib.Alfa(SombrasLib.Negro, 0.8f * (1f - edad / 0.55f)),
                        new Vector2(5f, 8f), 0f, VFXCore.Pixel);
                }
                VFXCore.FlushAlpha();
            }

            // === LA VIÑETA DE LA SENTENCIA: al morder, el mundo se
            // hace BORDE (la sala se apaga alrededor de la mirada) ===
            if (mordiendo)
                SombrasLib.Vignette((0.16f + 0.08f * MathF.Sin(tiempo * 2.2f)) * disipa);
        }

        private static float CicloMordida(float t)
        {
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
