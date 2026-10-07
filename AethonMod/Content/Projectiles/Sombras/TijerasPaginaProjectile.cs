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
    /// TIJERASPAGINAPROJECTILE — v6.50.71 — ARMA NUEVA 2: LAS TIJERAS DE LA PÁGINA.
    ///
    /// EL TIJERETAZO: dos HOJAS CURVAS de sombra —cuñas fusiformes con el
    /// FILO BLANCO de hueso en el canto interior— vuelan ABIERTAS al
    /// enemigo (remache violeta en el gozne, homing que siempre llega) y
    /// al alcanzarlo TIJERETEAN: abren… ¡CIERRAN DE GOLPE! …y el cierre
    /// es un TAJO BLANCO que cruza al reo — el pedazo cortado (una rebanada
    /// oscura) se separa, deriva y se disuelve en bruma. Cada tijeretada
    /// se lleva un bocado GORDO de vida. A 1 HP: LA DEVORACIÓN, estilo 12
    /// (EL CORTE FINAL).
    ///
    /// 100% código: ni un sprite. Cero dependencias.
    /// </summary>
    public class TijerasPaginaProjectile : ModProjectile
    {
        private const byte FASE_VUELO = 0;
        private const byte FASE_TAJO = 1;
        private const byte FASE_DISIPA = 2;

        private NPC Presa => Projectile.ai[0] <= 0 ? null : Main.npc[(int)Projectile.ai[0] - 1];

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 70;
            Projectile.height = 70;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 22;
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
            if (fase != FASE_DISIPA && !presaValida)
            {
                Projectile.ai[1] = FASE_DISIPA;
                Projectile.ai[2] = 0;
                fase = FASE_DISIPA;
                t = 0;
            }

            switch (fase)
            {
                case FASE_VUELO:
                {
                    if (presa != null && presaValida)
                    {
                        Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                        float dist = Vector2.Distance(presa.Center, Projectile.Center);
                        float vel = MathHelper.Clamp(dist * 0.055f, 22f, 52f);
                        float actual = Projectile.velocity.ToRotation();
                        float deseado = hacia.ToRotation();
                        float giro = MathHelper.WrapAngle(deseado - actual);
                        Projectile.velocity = (actual + MathHelper.Clamp(giro, -0.28f, 0.28f)).ToRotationVector2() * vel;
                        Projectile.rotation = deseado;

                        // LA MARCA CONTINUA (la cura .69)
                        if (t % 15f == 0f && FaucesGlobalNPC.EsJefe(presa))
                            FaucesGlobalNPC.Marcar(presa, 12);

                        // llegó: el gozne se clava y arranca el ciclo
                        if (dist < MathF.Max(presa.width, presa.height) * 0.6f + 26f)
                        {
                            Projectile.ai[1] = FASE_TAJO;
                            Projectile.ai[2] = 0f;
                            Projectile.Center = presa.Center;
                            Sonar(SoundID.Item122.WithPitchOffset(-0.2f).WithVolumeScale(0.8f), Projectile.Center);
                        }
                    }
                    if (t > 600f) { Projectile.ai[1] = FASE_DISIPA; Projectile.ai[2] = 0f; }
                    break;
                }
                case FASE_TAJO:
                {
                    if (presa != null && presa.active && presa.life > 0)
                    {
                        Projectile.Center = presa.Center;
                        // el corte SIEMPRE en la línea jugador→presa
                        Projectile.rotation = (presa.Center - dueño.MountedCenter).ToRotation();

                        // LA MARCA CONTINUA mientras corta (cada 5 t)
                        if (t % 5f == 0f && FaucesGlobalNPC.EsJefe(presa))
                            FaucesGlobalNPC.Marcar(presa, 12);

                        // EL TIJERETAZO: el drain cae JUSTO al cierre del ciclo
                        if (Main.netMode != NetmodeID.MultiplayerClient && t > 13f && t % 26f == 14f)
                        {
                            NPC dueñoPool = FaucesGlobalNPC.DueñoDelPool(presa);
                            if (dueñoPool != null && dueñoPool.active && dueñoPool.life > 1)
                            {
                                float quitar = MathF.Max(dueñoPool.life * 0.06f, 60f);
                                if (dueñoPool.life - quitar <= 1f)
                                {
                                    // === 1 HP: LA MUERTE SE DETIENE → EL CORTE FINAL ===
                                    FaucesGlobalNPC.Iniciar(dueñoPool, 12);
                                    Projectile.ai[1] = FASE_DISIPA;
                                    Projectile.ai[2] = 0f;
                                    break;
                                }
                                dueñoPool.life -= (int)quitar;
                                dueñoPool.netUpdate = true;
                                Sonar(SoundID.Item74.WithPitchOffset(0.42f).WithVolumeScale(0.75f), Projectile.Center);
                                Sonar(SoundID.NPCHit9.WithPitchOffset(-0.3f).WithVolumeScale(0.6f), Projectile.Center);
                            }
                        }
                        if (t > 400f) { Projectile.ai[1] = FASE_DISIPA; Projectile.ai[2] = 0f; }
                    }
                    else { Projectile.ai[1] = FASE_DISIPA; Projectile.ai[2] = 0f; }
                    break;
                }
                case FASE_DISIPA:
                {
                    if (t == 1f) Sonar(SoundID.Item122.WithPitchOffset(0.25f).WithVolumeScale(0.3f), Projectile.Center);
                    Projectile.velocity *= 0.92f;
                    if (t >= 36f) Projectile.Kill();
                    break;
                }
            }

            // el destello del tijeretazo decae (lo dibuja el PreDraw)
            if (Projectile.localAI[0] > 0f) Projectile.localAI[0]--;

            if (Main.netMode != NetmodeID.Server && Projectile.ai[1] == FASE_VUELO && Main.rand.NextBool(6))
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(28, 28), 56, 56,
                    DustID.Shadowflame, -Projectile.velocity.X * 0.1f, -Projectile.velocity.Y * 0.1f, 128, default, 0.55f);
                Main.dust[d].noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // LAS HOJAS SON EL ARMA: colisión de línea contra las dos cuñas
            // (determinista — la misma matemática del dibujo)
            float tiempo = Main.GlobalTimeWrappedHourly;
            byte fase = (byte)Projectile.ai[1];
            float ap = Apertura(fase, Projectile.ai[2], tiempo);
            float punto = 0f;
            for (int lado = -1; lado <= 1; lado += 2)
            {
                Vector2[] hoja = HojaCurva(Projectile.Center,
                    Projectile.rotation + lado * (0.14f + ap * 0.62f), lado * -0.42f, 118f, 9);
                for (int i = 1; i < hoja.Length; i++)
                    if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                        hoja[i - 1], hoja[i], 7, ref punto))
                        return true;
            }
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

        /// <summary>La apertura de las hojas: en vuelo respira bien abierta;
        /// en el tajo, el ciclo que CIERRA DE GOLPE cada 26 t.</summary>
        private static float Apertura(byte fase, float t, float tiempo)
        {
            if (fase == FASE_VUELO) return 0.72f + 0.14f * MathF.Sin(tiempo * 6f);
            if (fase == FASE_TAJO)
            {
                float m = t % 26f;
                if (m < 10f) return MathHelper.Lerp(0.30f, 1f, m / 10f);                          // abre
                if (m < 13f) return MathHelper.Lerp(1f, 0.02f, SombrasLib.DeGolpe((m - 10f) / 3f)); // ¡CIERRA!
                if (m < 16f) return 0.02f;                                                        // corta
                return MathHelper.Lerp(0.02f, 0.55f, (m - 16f) / 10f);                            // reabre
            }
            return 0.4f;
        }

        /// <summary>
        /// UNA HOJA CURVA (la cuña de la tijera): arco desde el gozne — el
        /// radio crece smoothstep y el ángulo se curva hacia el filo (la
        /// cuña que cierra). Determinista.
        /// </summary>
        private static Vector2[] HojaCurva(Vector2 gozne, float ang, float curva, float largo, int puntos)
        {
            puntos = Math.Max(puntos, 3);
            Vector2[] col = new Vector2[puntos];
            for (int i = 0; i < puntos; i++)
            {
                float f = i / (float)(puntos - 1);
                float r = MathHelper.Lerp(22f, largo, f * f * (3f - 2f * f));
                float a = ang + curva * f * f;
                col[i] = gozne + new Vector2(MathF.Cos(a), MathF.Sin(a)) * r;
            }
            return col;
        }

        private void DrawTodo()
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 23 + 5;
            Player dueño = Main.player[Projectile.owner];
            NPC presa = Presa;
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];

            float disipa = fase == FASE_DISIPA ? MathHelper.Clamp(1f - t / 36f, 0f, 1f) : 1f;
            float ap = Apertura(fase, t, tiempo);
            Vector2 gozne = Projectile.Center;

            // === EL REMACHE (el gozne violeta que late) ===
            VFXCore.Begin();
            VFXCore.Quad(gozne, SombrasLib.Alfa(SombrasLib.Violeta, (0.30f + 0.14f * MathF.Sin(tiempo * 5f)) * disipa),
                new Vector2(64f, 64f));
            VFXCore.Quad(gozne, SombrasLib.Alfa(SombrasLib.Blanco, 0.5f * disipa), new Vector2(14f, 14f));
            VFXCore.FlushAdditive();

            // === LAS DOS HOJAS — cuñas fusiformes con FILO DE HUESO ===
            for (int lado = -1; lado <= 1; lado += 2)
            {
                float ang = Projectile.rotation + lado * (0.14f + ap * 0.62f);
                Vector2[] hoja = HojaCurva(gozne, ang, lado * -0.42f, 118f, 10);

                // la masa (fina en el gozne, GORDA al centro, fina en la punta)
                SombrasLib.Masa(hoja, 7f, 4f, 0.94f * disipa, semilla + lado * 17, tiempo, grosorCentro: 21f);

                // EL FILO BLANCO — el canto interior (donde corta): línea
                // fina de hueso apenas DENTRO de la cuña
                VFXCore.Begin();
                Vector2 adentro = new(MathF.Cos(ang - lado * 0.30f), MathF.Sin(ang - lado * 0.30f));
                for (int i = 2; i < hoja.Length; i++)
                    VFXCore.Line(hoja[i - 1] + adentro * 5f, hoja[i] + adentro * 5f,
                        SombrasLib.Alfa(SombrasLib.Blanco, 0.85f * disipa), 3.5f);
                VFXCore.FlushAdditive(VFXCore.Pixel);

                // el rastro de bruma de la hoja
                SombrasLib.BrumaColumna(hoja, 18f, 0.36f * disipa, tiempo, semilla + lado * 31, 5);
            }

            // === EL TAJO — el destelle blanco del cierre (cruza al reo a lo
            // LARGO de las hojas: la línea de corte) ===
            if (Projectile.localAI[0] > 0f)
            {
                float f = 1f - Projectile.localAI[0] / 10f;
                float medioTajo = presa != null && presa.active
                    ? MathF.Max(presa.width, presa.height) * 0.85f + 30f
                    : 110f;
                Vector2 dirTajo = Projectile.rotation.ToRotationVector2();
                Vector2 a = gozne - dirTajo * medioTajo;
                Vector2 b = gozne + dirTajo * medioTajo;

                VFXCore.Begin();
                VFXCore.Line(a, b, SombrasLib.Alfa(SombrasLib.Blanco, 0.9f * (1f - f) * disipa), 10f * (1f - f * 0.6f));
                VFXCore.FlushAdditive(VFXCore.Pixel);
                SombrasLib.OndaChoque(gozne, 30f + 90f * f, 0.5f * (1f - f) * disipa, roja: false);

                // LA REBANADA — el pedazo cortado: una tabla oscura que se
                // SEPARA del cuerpo, deriva y se disuelve en bruma
                Vector2 normal = new Vector2(-dirTajo.Y, dirTajo.X) * (14f + 46f * f);
                float ancho = medioTajo * 1.1f;
                VFXCore.Begin();
                VFXCore.Quad(gozne + normal + dirTajo * (8f * f), SombrasLib.Alfa(SombrasLib.Negro, 0.8f * (1f - f) * disipa),
                    new Vector2(ancho, 12f + 6f * (1f - f)), Projectile.rotation, VFXCore.Pixel);
                VFXCore.FlushAlpha(VFXCore.Pixel);
                SombrasLib.Bruma(gozne + normal + dirTajo * (16f * f), 34f,
                    0.5f * (1f - f * 0.5f) * disipa, tiempo, semilla + 77,
                    normal * 0.4f);
            }

            // EL CRUJE del cierre (para el siguiente tijeretazo)
            if (fase == FASE_TAJO && t % 26f == 13f)
                Projectile.localAI[0] = 10f;
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
