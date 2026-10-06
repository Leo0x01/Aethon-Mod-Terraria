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
    /// NIDOPAGINAPROJECTILE — v6.50.66 — EL NIDO DE LA PÁGINA.
    ///
    /// La tercera hermana de La Sombra de la Página (v6.50.66 — el
    /// usuario jubiló las 3 armas viejas y pidió 3 COPIAS de la
    /// Sombra con «más personalidad, más efectos, más sabor, más
    /// alma, más de todo»). Esta ocupa el SLOT del arma 4 (La Página
    /// Final) — PERO SIN EL SPRITE DEL LIBRO: el usuario lo vetó
    /// («no uses el sprite del libro»); aquí TODO es código vivo.
    ///
    /// EL NIDO ES LA MADRE DE LAS SOMBRAS: la misma sombra del suelo,
    /// pero preñada — el charco LLENO DE HUEVOS (óvalos negros con el
    /// núcleo ROJO latiendo despacio… que al morder SE ECLOSIONAN:
    /// mini-fauces que abren la boca y sueltan un ALMA cada una), la
    /// cabeza coronada por SEIS GARRAS plegadas como capullo que al
    /// morder SE ABIEN Y CIERRAN EN JAULA sobre la presa (apretando
    /// al compás de la mordida), y CUATRO ALMAS orbitando la cabeza
    /// en todo momento (las que ya viven en la página — giran más
    /// rápido cuando la madre come). El dren más hondo de las tres
    /// hermanas (3,2%/8t): la madre come por sus crías. La bruma más
    /// DENSA: 15 puffs + la nube del nido en el charco. Festín
    /// ESTILO 7 (la jaula de garras del motor del festín).
    /// </summary>
    public class NidoPaginaProjectile : ModProjectile
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
                    // EL NIDO DESPIERTA ATRÁS: los huevos LATE primero
                    // (la masa sube cuando las crías ya piden comida)
                    Vector2 raiz = RaizDeSombra(dueño);
                    Vector2 rumbo = presa != null
                        ? (presa.Center - raiz).SafeNormalize(-Vector2.UnitY)
                        : -Vector2.UnitY;
                    float sube = SombrasLib.DeGolpe(MathHelper.Clamp((t - 9f) / 25f, 0f, 1f));
                    Projectile.Center = raiz + rumbo * (26f + 172f * sube);
                    Projectile.rotation = rumbo.ToRotation();
                    if (t == 4) Sonar(SoundID.Item122.WithPitchOffset(-0.65f).WithVolumeScale(0.85f), Projectile.Center);
                    if (t >= 34) { Projectile.ai[1] = FASE_CAZA; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_CAZA:
                {
                    if (presa != null && presaValida)
                    {
                        // LA MADRE CAZA CON PESO: tranquila, segura —
                        // con la jaula de garras plegada en capullo
                        Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                        float dist = Vector2.Distance(presa.Center, Projectile.Center);
                        float vel = MathHelper.Clamp(dist * 0.045f, 18f, 46f);
                        float actual = Projectile.velocity.ToRotation();
                        float deseado = hacia.ToRotation();
                        float giro = MathHelper.WrapAngle(deseado - actual);
                        Projectile.velocity = (actual + MathHelper.Clamp(giro, -0.22f, 0.22f)).ToRotationVector2() * vel;
                        Projectile.rotation = deseado;

                        if (Projectile.Hitbox.Intersects(presa.Hitbox))
                        {
                            Projectile.ai[1] = FASE_MORDISCO;
                            Projectile.ai[2] = 0;
                            Sonar(SoundID.NPCHit9.WithPitchOffset(-0.45f).WithVolumeScale(0.95f), Projectile.Center);
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

                        // LA MADRE COME POR SUS CRÍAS: el dren más hondo
                        // de las tres hermanas (3,2%/8t)
                        if (Main.netMode != NetmodeID.MultiplayerClient && t > 22 && t % 8 == 0)
                        {
                            NPC dueñoPool = FaucesGlobalNPC.DueñoDelPool(presa);
                            if (dueñoPool != null && dueñoPool.active && dueñoPool.life > 1)
                            {
                                float quitar = MathF.Max(dueñoPool.life * 0.032f, 32f);
                                if (dueñoPool.life - quitar <= 1f)
                                {
                                    FaucesGlobalNPC.Iniciar(dueñoPool, 7);
                                    Projectile.ai[1] = FASE_DISIPAR;
                                    Projectile.ai[2] = 0;
                                    break;
                                }
                                dueñoPool.life -= (int)quitar;
                                dueñoPool.netUpdate = true;
                                if (t % 48 == 0)
                                    Sonar(SoundID.NPCHit9.WithPitchOffset(-0.25f).WithVolumeScale(0.6f), Projectile.Center);
                            }
                        }
                    }
                    else { Projectile.ai[1] = FASE_DISIPAR; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_DISIPAR:
                {
                    if (t == 1) Sonar(SoundID.Item122.WithPitchOffset(0.1f).WithVolumeScale(0.3f), Projectile.Center);
                    Projectile.velocity *= 0.92f;
                    if (t >= 44) Projectile.Kill();
                    break;
                }
            }

            if (Main.netMode != NetmodeID.Server && Projectile.ai[1] <= FASE_CAZA && Main.rand.NextBool(5))
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(26, 26), 52, 52,
                    DustID.Shadowflame, 0f, -0.3f, 128, default, 0.6f);
                Main.dust[d].noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.ai[1] != FASE_CAZA) return base.Colliding(projHitbox, targetHitbox);
            Player dueño = Main.player[Projectile.owner];
            Vector2 raiz = RaizDeSombra(dueño);
            Vector2[] col = SombrasLib.Columna(raiz, Projectile.Center,
                Main.GlobalTimeWrappedHourly, Projectile.whoAmI * 31 + 13, 10, 0.25f);
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
            int semilla = Projectile.whoAmI * 31 + 13;
            Player dueño = Main.player[Projectile.owner];
            NPC presa = Presa;
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];

            Vector2 raiz = RaizDeSombra(dueño);
            float disipa = fase == FASE_DISIPAR ? MathHelper.Clamp(1f - t / 44f, 0f, 1f) : 1f;
            float presencia = fase == FASE_EMERGER
                ? SombrasLib.DeGolpe(MathHelper.Clamp(t / 9f, 0f, 1f)) : 1f;
            bool mordiendo = fase == FASE_MORDISCO;

            // === EL CHARCO NIDO + LOS HUEVOS: cinco óvalos negros con
            // el NÚCLEO ROJO latiendo (despacio siempre… rápido al
            // morder: las crías sienten la comida) ===
            SombrasLib.Charco(raiz, 58f * presencia, 0.82f * disipa, tiempo, semilla);

            Vector2[] huevos = new Vector2[5];
            VFXCore.Begin();
            for (int j = 0; j < 5; j++)
            {
                float f0 = SombrasLib.Frac(SombrasLib.semille(semilla) * 0.53f + j * 0.618034f);
                float ang = MathHelper.Pi + (f0 - 0.5f) * 2.6f;   // semicírculo hacia el suelo
                float dist = 26f + 46f * SombrasLib.Frac(f0 * 7.7f);
                huevos[j] = raiz + new Vector2(MathF.Cos(ang) * dist, -10f - 6f * f0);
                // el óvalo del huevo
                VFXCore.Quad(huevos[j], SombrasLib.Alfa(SombrasLib.Negro, 0.88f * presencia * disipa),
                    new Vector2(17f, 23f), 0f, VFXCore.GlowOrb);
            }
            VFXCore.FlushAlpha();
            // los núcleos laten (aditivo — la vida dentro)
            VFXCore.Begin();
            float latido = mordiendo ? 4.2f : 1.9f;
            for (int j = 0; j < 5; j++)
            {
                float f0 = SombrasLib.Frac(SombrasLib.semille(semilla) * 0.53f + j * 0.618034f);
                float pulso = 0.45f + 0.55f * MathF.Max(0f, MathF.Sin(tiempo * latido + j * 1.9f));
                float aNucleo = (mordiendo ? 0.75f : 0.4f) * pulso * presencia * disipa;
                VFXCore.Quad(huevos[j] + new Vector2(0f, -2f), SombrasLib.Alfa(SombrasLib.Rojo, aNucleo),
                    new Vector2(7f, 8f));
            }
            VFXCore.FlushAdditive();

            // === LA COLUMNA (la madre) ===
            Vector2[] col = SombrasLib.Columna(raiz, Projectile.Center, tiempo, semilla, 20, 0.25f);
            SombrasLib.Masa(col, 48f * presencia, 14f, 0.95f * disipa, semilla, tiempo);

            // === LA BRUMA — la más densa de las tres hermanas: 15
            // puffs de columna + LA NUBE DEL NIDO en el charco (el
            // vapor de la incubación) (v6.50.66: «más bruma») ===
            SombrasLib.BrumaColumna(col, 44f, 0.52f * disipa, tiempo, semilla, 15);
            SombrasLib.Bruma(raiz + new Vector2(0f, -30f), 68f, 0.5f * presencia * disipa, tiempo, semilla + 19);

            // === LOS OJOS DE LA MADRE (seis: los que cuidan) ===
            Vector2 objetivo = presa != null && presa.active
                ? presa.Center
                : dueño.MountedCenter + Projectile.velocity * 30f;
            SombrasLib.OjosDeMasa(col, objetivo, presencia * disipa, semilla, tiempo, 6);

            // === LA CABEZA: FAUCES + GARGANTA + ALIENTO ===
            float apertura = fase switch
            {
                FASE_EMERGER => 0.35f + 0.4f * SombrasLib.DeGolpe(MathHelper.Clamp((t - 9f) / 25f, 0f, 1f)),
                FASE_CAZA => 0.85f + 0.15f * MathF.Sin(tiempo * 8f),
                FASE_MORDISCO => CicloMordida(t),
                _ => 0.5f * disipa,
            };
            Vector2 rumbo = Projectile.rotation.ToRotationVector2();
            SombrasLib.Fauces(Projectile.Center, rumbo, apertura * disipa, 78f, semilla);
            SombrasLib.BrumaBoca(Projectile.Center, rumbo, apertura * disipa, 0.5f * disipa, tiempo, semilla + 5, 7);

            if (mordiendo)
            {
                VFXCore.Begin();
                VFXCore.Quad(Projectile.Center, SombrasLib.Alfa(SombrasLib.Rojo, 0.55f * disipa),
                    new Vector2(128f, 128f), Projectile.rotation);
                VFXCore.FlushAdditive();
            }

            // === LA CORONA DE GARRAS — SEIS, plegadas en capullo en la
            // caza (atrás, apuntando al revés)… y al morder SE ABIEN Y
            // CIERRAN EN JAULA sobre la presa: las bases saltan a un
            // ANILLO alrededor del jefe y aprietan al compás de la
            // mordida (el apretón late con el ciclo de mordida) ===
            float jaula = mordiendo ? SombrasLib.DeGolpe(MathHelper.Clamp(t / 14f, 0f, 1f)) : 0f;
            float apriete = mordiendo ? 0.55f + 0.45f * MathF.Sin((t % 22f) * MathHelper.Pi / 22f) : 0f;
            float radioJaula = 0f;
            if (presa != null && presa.active)
                radioJaula = MathHelper.Clamp(presa.Size.Length() * 0.62f, 44f, 130f);

            for (int k = 0; k < 6; k++)
            {
                float angK = k / 6f * MathHelper.TwoPi + tiempo * (mordiendo ? 0.24f : 0.42f);
                // POSICIÓN PLEGADA: anillo pequeño alrededor de la cabeza
                Vector2 posPlegada = Projectile.Center + new Vector2(MathF.Cos(angK), MathF.Sin(angK)) * 24f;
                Vector2 haciaPlegada = (-rumbo + new Vector2(MathF.Cos(angK), MathF.Sin(angK)) * 0.5f)
                    .SafeNormalize(-rumbo);
                // POSICIÓN JAULA: anillo alrededor de la PRESA, adentro
                Vector2 dirK = new(MathF.Cos(angK), MathF.Sin(angK) * 0.82f);
                Vector2 posJaula = objetivo + dirK * radioJaula;
                Vector2 haciaJaula = -dirK;

                Vector2 pos = Vector2.Lerp(posPlegada, posJaula, jaula);
                Vector2 hacia = Vector2.Lerp(haciaPlegada, haciaJaula, jaula).SafeNormalize(haciaJaula);
                float largo = MathHelper.Lerp(46f, radioJaula * (0.62f + 0.2f * apriete), jaula);
                float gancho = 0.35f * (k % 2 == 0 ? 1f : -1f) + 0.3f * jaula * apriete;

                SombrasLib.Garra(pos, hacia, largo * disipa * presencia, 0.92f * disipa, gancho);
            }

            // === LAS ALMAS QUE YA VIVEN EN LA PÁGINA: cuatro, orbitando
            // la cabeza en todo momento — giran MÁS RÁPIDO cuando la
            // madre come (las crías se acercan a la mesa) ===
            {
                float velocidad = mordiendo ? 2.6f : 1.3f;
                float radioAlmas = mordiendo ? 66f : 48f;
                for (int k = 0; k < 4; k++)
                {
                    float angA = tiempo * velocidad + k * MathHelper.PiOver2;
                    Vector2 pos = Projectile.Center + new Vector2(MathF.Cos(angA), MathF.Sin(angA) * 0.72f) * radioAlmas;
                    SombrasLib.Alma(pos, 9f + 3f * MathF.Sin(tiempo * 3f + k * 1.6f), 0.72f * disipa);
                }
            }

            // === LA ECLOSIÓN: al morder, cada huevo ABRE una mini-fauce
            // y suelta su ALMA (una por ciclo de mordida — las crías
            // prueban la comida por primera vez) ===
            if (mordiendo && disipa > 0.5f)
            {
                float m = t % 22f;
                for (int j = 0; j < 5; j++)
                {
                    float desfase = (m + j * 4.4f) % 22f;
                    float abreHuevo = desfase < 5f
                        ? MathHelper.Lerp(0.9f, 0.15f, SombrasLib.DeGolpe(desfase / 5f))
                        : desfase < 12f ? MathHelper.Lerp(0.15f, 0.95f, (desfase - 5f) / 7f)
                        : 0.85f;
                    SombrasLib.Fauces(huevos[j] + new Vector2(0f, -14f), -Vector2.UnitY,
                        abreHuevo * 0.9f * disipa, 26f, semilla + j * 7);

                    // el alma del huevo sube a la cabeza (una vez por ciclo)
                    float u = SombrasLib.Frac(desfase / 22f + j * 0.37f);
                    if (u < 0.5f)
                    {
                        Vector2 a = huevos[j];
                        Vector2 b = Projectile.Center;
                        Vector2 ctrl = (a + b) * 0.5f + new Vector2(MathF.Sin(j * 2.4f) * 60f, -70f);
                        float uu = 1f - u * 2f;
                        Vector2 pos = uu * uu * a + 2f * uu * (u * 2f) * ctrl + (u * 2f) * (u * 2f) * b;
                        SombrasLib.Alma(pos, 7f, 0.65f * disipa * (1f - u * 2f));
                    }
                }
            }

            // la disipación: las crías se apagan con la madre
            if (fase == FASE_DISIPAR && t < 18f)
                SombrasLib.Bruma(raiz, 60f, 0.45f * (1f - t / 18f), tiempo, semilla + 71);
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
