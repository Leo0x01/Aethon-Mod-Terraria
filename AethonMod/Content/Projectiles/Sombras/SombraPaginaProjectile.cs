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
    /// SOMBrapaginaPROJECTILE — v6.50.62 — ARMA 3: LA SOMBRA DE LA PÁGINA.
    /// v6.50.64 — LA BRUMA NEGRA: todo el cuerpo y la boca exhalan puﬀs
    /// vivos (BrumaColumna + BrumaBoca) — la petición del usuario.
    ///
    /// «La misma arma, pero SOLO POR CÓDIGO»: cero sprites — ni uno.
    /// El tentáculo COMPLETO nace, caza, muerde y disipa dibujado con
    /// los pinceles del motor (Pixel + GlowOrb + SoftGlow vía la
    /// SombrasLib): la masa negra con borde de sierra, los ojos que se
    /// abren DE GOLPE y miran, las FAUCES procedurales (mandíbulas en
    /// cuña con colmillos blancos) y la garganta roja.
    ///
    /// Mismo comportamiento que el ARMA 1 (emerge del jugador → caza al
    /// jefe a cualquier distancia → muerde y drena 2.5%/6t → a 1 HP el
    /// motor del festín, estilo 3) — pero nace de la SOMBRA DEL SUELO
    /// del portador (raycast hacia abajo: donde hay sombra, la página
    /// mira), no de su pecho.
    /// </summary>
    public class SombraPaginaProjectile : ModProjectile
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
                    Vector2 raiz = RaizDeSombra(dueño);
                    Vector2 rumbo = presa != null
                        ? (presa.Center - raiz).SafeNormalize(-Vector2.UnitY)
                        : -Vector2.UnitY;
                    float avance = SombrasLib.DeGolpe(t / 26f);
                    Projectile.Center = raiz + rumbo * (30f + 170f * avance);
                    Projectile.rotation = rumbo.ToRotation();
                    if (t == 3) Sonar(SoundID.Item122.WithPitchOffset(-0.45f).WithVolumeScale(0.75f), Projectile.Center);
                    if (t >= 26) { Projectile.ai[1] = FASE_CAZA; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_CAZA:
                {
                    if (presa != null && presaValida)
                    {
                        Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                        float dist = Vector2.Distance(presa.Center, Projectile.Center);
                        float vel = MathHelper.Clamp(dist * 0.05f, 20f, 52f);
                        float actual = Projectile.velocity.ToRotation();
                        float deseado = hacia.ToRotation();
                        float giro = MathHelper.WrapAngle(deseado - actual);
                        Projectile.velocity = (actual + MathHelper.Clamp(giro, -0.26f, 0.26f)).ToRotationVector2() * vel;
                        Projectile.rotation = deseado;

                        if (Projectile.Hitbox.Intersects(presa.Hitbox))
                        {
                            Projectile.ai[1] = FASE_MORDISCO;
                            Projectile.ai[2] = 0;
                            Sonar(SoundID.NPCHit9.WithPitchOffset(-0.35f).WithVolumeScale(0.9f), Projectile.Center);
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

                        if (Main.netMode != NetmodeID.MultiplayerClient && t > 18 && t % 6 == 0)
                        {
                            NPC dueñoPool = FaucesGlobalNPC.DueñoDelPool(presa);
                            if (dueñoPool != null && dueñoPool.active && dueñoPool.life > 1)
                            {
                                float quitar = MathF.Max(dueñoPool.life * 0.025f, 25f);
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
                    if (t >= 44) Projectile.Kill();
                    break;
                }
            }

            if (Main.netMode != NetmodeID.Server && Projectile.ai[1] <= FASE_CAZA && Main.rand.NextBool(6))
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(24, 24), 48, 48,
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
            int semilla = Projectile.whoAmI * 17 + 5;
            Player dueño = Main.player[Projectile.owner];
            NPC presa = Presa;
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];

            Vector2 raiz = RaizDeSombra(dueño);

            // === EL CHARCO RAÍZ (la sombra del suelo, más grande que el arma 1) ===
            SombrasLib.Charco(raiz, 52f, 0.8f, tiempo, semilla);

            // === LA COLUMNA y LA MASA ===
            // v6.50.67 — LA COLUMNA VIVA: física de verlet con inercia y
            // ONDA VIAJERA (la letra del GIF del usuario) + el GANCHO de
            // la punta — la base también se hace FLUIDA
            Vector2[] col = SombrasLib.ColumnaViva(raiz, Projectile.Center, tiempo, semilla, 20, 0.24f, gancho: 0.5f);
            float disipa = fase == FASE_DISIPAR ? MathHelper.Clamp(1f - t / 44f, 0f, 1f) : 1f;
            SombrasLib.Masa(col, 40f, 12f, 0.95f * disipa, semilla, tiempo);

            // === LA BRUMA NEGRA DEL CUERPO (v6.50.64): el tentáculo
            // 100% código también EXHALA — puﬀs vivos a lo largo ===
            SombrasLib.BrumaColumna(col, 38f, 0.45f * disipa, tiempo, semilla, 10);

            // === LOS OJOS (más ojos que el arma 1: la página es TODA ojos) ===
            Vector2 objetivo = presa != null && presa.active
                ? presa.Center
                : dueño.MountedCenter + Projectile.velocity * 30f;
            float presencia = fase == FASE_EMERGER ? SombrasLib.DeGolpe(t / 26f) : 1f;
            SombrasLib.OjosDeMasa(col, objetivo, presencia * disipa, semilla, tiempo, 7);

            // === LA CABEZA: FAUCES PROCEDURALES (cero sprites) ===
            float apertura = fase switch
            {
                FASE_EMERGER => 0.35f + 0.4f * SombrasLib.DeGolpe(t / 26f),
                FASE_CAZA => 0.85f + 0.15f * MathF.Sin(tiempo * 8f),           // bien abierta, viva
                FASE_MORDISCO => CicloMordida(t),
                _ => 0.5f * disipa,
            };
            Vector2 rumbo = Projectile.rotation.ToRotationVector2();
            SombrasLib.Fauces(Projectile.Center, rumbo, apertura * disipa, 74f, semilla);

            // === EL ALIENTO DE LA BOCA (v6.50.64): las fauces
            // procedurales respiran bruma negra al abrirse ===
            SombrasLib.BrumaBoca(Projectile.Center, rumbo, apertura * disipa,
                0.5f * disipa, tiempo, semilla + 5, 5);

            // el brillo rojo de la garganta cuando muerde
            if (fase == FASE_MORDISCO)
            {
                VFXCore.Begin();
                VFXCore.Quad(Projectile.Center, SombrasLib.Alfa(SombrasLib.Rojo, 0.5f * disipa),
                    new Vector2(120f, 120f), Projectile.rotation);
                VFXCore.FlushAdditive();
            }
        }

        private static float CicloMordida(float t)
        {
            float m = t % 22f;
            if (m < 5f) return MathHelper.Lerp(0.9f, 0.12f, SombrasLib.DeGolpe(m / 5f));  // ¡CIERRA!
            if (m < 12f) return MathHelper.Lerp(0.12f, 0.95f, (m - 5f) / 7f);             // reabre
            return 0.9f + 0.1f * MathF.Sin(m * 1.1f);                                     // sostiene
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
