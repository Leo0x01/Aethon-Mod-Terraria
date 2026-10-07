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
    /// MANOESCRIBAPROJECTILE — v6.50.71 — ARMA NUEVA 1: LA MANO DEL ESCRIBA.
    ///
    /// LA GARRA QUE CAMINA: una mano de sombra COLOSAL (palma con UN OJO
    /// que no parpadea + cuatro dedos fusiformes y un pulgar con GARRA de
    /// hueso) nace del CHARCO del portador atada por la MUÑECA — una
    /// cuerda de sombra que se engorda hacia la palma — y CAMINA sobre sus
    /// dedos como una araña hasta el enemigo (el paso arácnido: cada dedo
    /// avanza desfasado, la física verlet hace el resto). Al alcanzarlo:
    /// LOS DEDOS SE ENCIERRAN alrededor de su elipse (la jaula de cinco),
    /// la palma APRIETA al ritmo del CicloApretar y el CHARCO que se abre
    /// bajo sus pies crece con cada apretón — lo está HUNDIENDO en la
    /// sombra. A 1 HP: LA DEVORACIÓN, estilo 11 (EL PUÑO DEL ESCRIBA).
    ///
    /// 100% código: ni un sprite. Cero dependencias.
    /// </summary>
    public class ManoEscribaProjectile : ModProjectile
    {
        private const byte FASE_BROTA = 0;
        private const byte FASE_CAMINA = 1;
        private const byte FASE_AGARRA = 2;
        private const byte FASE_DISIPA = 3;

        private NPC Presa => Projectile.ai[0] <= 0 ? null : Main.npc[(int)Projectile.ai[0] - 1];

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 84;
            Projectile.height = 66;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.extraUpdates = 1;
        }

        /// <summary>El suelo bajo una posición (raycast hacia abajo — el charco).</summary>
        private static Vector2 SueloBajo(Vector2 c)
        {
            int tx = (int)(c.X / 16f), ty = (int)(c.Y / 16f);
            int pasos = 0;
            while (pasos < 34 && !WorldGen.SolidTile(tx, ty)) { ty++; pasos++; }
            Vector2 suelo = new(tx * 16f + 8f, ty * 16f - 4f);
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
            if (fase != FASE_DISIPA && !presaValida)
            {
                Projectile.ai[1] = FASE_DISIPA;
                Projectile.ai[2] = 0;
                fase = FASE_DISIPA;
                t = 0;
            }

            switch (fase)
            {
                case FASE_BROTA:
                {
                    // LA MANO NACE DEL CHARCO: sube desdoblándose (DeGolpe)
                    Vector2 charco = SueloBajo(dueño.MountedCenter + new Vector2(dueño.direction * 40f, 0f));
                    float sube = SombrasLib.DeGolpe(t / 26f);
                    Projectile.Center = charco + new Vector2(0f, -20f - 110f * sube);
                    Projectile.velocity = Vector2.Zero;
                    if (t == 3f) Sonar(SoundID.Item122.WithPitchOffset(-0.5f).WithVolumeScale(0.8f), Projectile.Center);
                    if (t >= 26f) { Projectile.ai[1] = FASE_CAMINA; Projectile.ai[2] = 0f; }
                    break;
                }
                case FASE_CAMINA:
                {
                    if (presa != null && presaValida)
                    {
                        Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                        float dist = Vector2.Distance(presa.Center, Projectile.Center);
                        float vel = MathHelper.Clamp(dist * 0.045f, 18f, 40f);
                        float actual = Projectile.velocity.ToRotation();
                        float deseado = hacia.ToRotation();
                        float giro = MathHelper.WrapAngle(deseado - actual);
                        Projectile.velocity = (actual + MathHelper.Clamp(giro, -0.22f, 0.22f)).ToRotationVector2() * vel;
                        Projectile.rotation = deseado;

                        // LA MARCA CONTINUA (la cura .69 del «a veces no se activa»)
                        if (t % 15f == 0f && FaucesGlobalNPC.EsJefe(presa))
                            FaucesGlobalNPC.Marcar(presa, 11);

                        if (Projectile.Hitbox.Intersects(presa.Hitbox))
                        {
                            Projectile.ai[1] = FASE_AGARRA;
                            Projectile.ai[2] = 0f;
                            Sonar(SoundID.NPCHit9.WithPitchOffset(-0.35f).WithVolumeScale(0.9f), Projectile.Center);
                        }
                    }
                    if (t > 600f) { Projectile.ai[1] = FASE_DISIPA; Projectile.ai[2] = 0f; }
                    break;
                }
                case FASE_AGARRA:
                {
                    if (presa != null && presa.active && presa.life > 0)
                    {
                        // la palma SE SIENTA sobre la presa (la cara que aprieta)
                        Projectile.Center = presa.Center;
                        Projectile.rotation = (presa.Center - dueño.MountedCenter).ToRotation();

                        // LA MARCA CONTINUA mientras agarra (cada 5 t)
                        if (t % 5f == 0f && FaucesGlobalNPC.EsJefe(presa))
                            FaucesGlobalNPC.Marcar(presa, 11);

                        // EL APRETÓN: cada 16 t, el drain cae JUSTO al cierre
                        if (Main.netMode != NetmodeID.MultiplayerClient && t > 14f && t % 16f == 10f)
                        {
                            NPC dueñoPool = FaucesGlobalNPC.DueñoDelPool(presa);
                            if (dueñoPool != null && dueñoPool.active && dueñoPool.life > 1)
                            {
                                float quitar = MathF.Max(dueñoPool.life * 0.025f, 26f);
                                if (dueñoPool.life - quitar <= 1f)
                                {
                                    // === 1 HP: LA MUERTE SE DETIENE → EL PUÑO DEL ESCRIBA ===
                                    FaucesGlobalNPC.Iniciar(dueñoPool, 11);
                                    Projectile.ai[1] = FASE_DISIPA;
                                    Projectile.ai[2] = 0f;
                                    break;
                                }
                                dueñoPool.life -= (int)quitar;
                                dueñoPool.netUpdate = true;
                                Sonar(SoundID.NPCHit9.WithPitchOffset(-0.2f).WithVolumeScale(0.55f), Projectile.Center);
                            }
                        }
                    }
                    else { Projectile.ai[1] = FASE_DISIPA; Projectile.ai[2] = 0f; }
                    break;
                }
                case FASE_DISIPA:
                {
                    if (t == 1f) Sonar(SoundID.Item122.WithPitchOffset(0.2f).WithVolumeScale(0.3f), Projectile.Center);
                    Projectile.velocity *= 0.92f;
                    if (t >= 40f) Projectile.Kill();
                    break;
                }
            }

            if (Main.netMode != NetmodeID.Server && Projectile.ai[1] <= FASE_CAMINA && Main.rand.NextBool(7))
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(34, 30), 68, 60,
                    DustID.Shadowflame, 0f, -0.3f, 128, default, 0.5f);
                Main.dust[d].noGravity = true;
            }
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

        /// <summary>El apretón de la mano: sostiene… ¡CIERRA de golpe! …aprieta.</summary>
        private static float CicloApretar(float t)
        {
            float m = t % 16f;
            if (m < 6f) return MathHelper.Lerp(0.35f, 1f, m / 6f);                       // abre
            if (m < 9f) return MathHelper.Lerp(1f, 0.02f, SombrasLib.DeGolpe((m - 6f) / 3f)); // ¡CIERRA!
            if (m < 13f) return 0.06f + 0.05f * MathF.Sin(m * 2.2f);                     // aprieta
            return MathHelper.Lerp(0.1f, 0.5f, (m - 13f) / 3f);                          // cede
        }

        private void DrawTodo()
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 19 + 3;
            Player dueño = Main.player[Projectile.owner];
            NPC presa = Presa;
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];

            float disipa = fase == FASE_DISIPA ? MathHelper.Clamp(1f - t / 40f, 0f, 1f) : 1f;
            float vida = disipa;
            float despliega = fase == FASE_BROTA ? SombrasLib.DeGolpe(t / 26f) : 1f;
            float agarre = fase == FASE_AGARRA ? MathHelper.Clamp(t / 10f, 0f, 1f) : 0f;
            float apriete = fase == FASE_AGARRA ? CicloApretar(t) : 1f;

            Vector2 dir = Projectile.rotation.ToRotationVector2();
            if (dir.LengthSquared() < 0.01f) dir = -Vector2.UnitY;
            Vector2 perp = new(-dir.Y, dir.X);
            Vector2 palm = Projectile.Center;

            // === LOS CHARCOS: el del portador (la cuna) y el de la presa
            // (LA TUMBA que se abre bajo sus pies mientras la mano aprieta) ===
            Vector2 charcoDueño = SueloBajo(dueño.MountedCenter + new Vector2(dueño.direction * 40f, 0f));
            SombrasLib.Charco(charcoDueño, 40f * despliega, 0.6f * vida, tiempo, semilla);

            // === LA MUÑECA — la cuerda de sombra: fina en el charco,
            // GORDA hacia la palma (fusiforme invertido — el talón de la mano) ===
            Vector2 anclaMuñeca = palm - dir * (34f * despliega) + new Vector2(0f, 26f * (1f - agarre));
            Vector2[] muñeca = SombrasLib.ColumnaViva(charcoDueño + new Vector2(0f, -6f), anclaMuñeca,
                tiempo, semilla + 400, 10, 0.16f, gancho: 0.2f);
            SombrasLib.Masa(muñeca, 5f, 30f, 0.92f * vida, semilla + 400, tiempo, grosorCentro: 24f);
            SombrasLib.BrumaColumna(muñeca, 26f, 0.4f * vida, tiempo, semilla + 41, 6);

            // === LA PALMA — la masa central con su borde de energía ===
            VFXCore.Begin();
            VFXCore.Quad(palm, SombrasLib.Alfa(SombrasLib.Negro, 0.95f * vida),
                new Vector2(118f * despliega, 84f * despliega), Projectile.rotation + MathHelper.PiOver2, VFXCore.GlowOrb);
            VFXCore.FlushAlpha();
            VFXCore.Begin();
            VFXCore.Quad(palm, SombrasLib.Alfa(SombrasLib.Violeta, (0.10f + 0.06f * MathF.Sin(tiempo * 3.1f)) * vida),
                new Vector2(136f * despliega, 100f * despliega), Projectile.rotation + MathHelper.PiOver2);
            VFXCore.FlushAdditive();

            // === EL OJO DE LA PALMA — rasgado, FIJO en la presa, entrecierra
            // al apretar (el esfuerzo de estrangular) ===
            Vector2 mirada = presa != null && presa.active ? presa.Center - palm : dir * 100f;
            float ojoAbre = despliega * (fase == FASE_AGARRA ? 0.55f + 0.45f * apriete : 1f) * vida;
            VFXCore.Begin();
            SombrasLib.Ojo(palm - dir * 4f, 26f, mirada, ojoAbre, pupila: true, rasgada: true);
            VFXCore.FlushAdditive();

            // === LOS DEDOS — cuatro fusiformes en abanico + el pulgar al
            // costado: en marcha, cada uno avanza DESFASADO (el paso de
            // araña); al agarrar, la JAULA de cinco alrededor de la elipse ===
            float[] largos = { 86f, 98f, 88f, 70f };
            for (int k = 0; k < 5; k++)
            {
                bool pulgar = k == 4;
                float lado = pulgar ? 1.35f : (k - 1.5f) * 0.52f;
                float largo = pulgar ? 62f : largos[k];

                // la raíz del dedo: en el borde delantero de la palma
                Vector2 raiz = palm + dir * (30f * despliega)
                             + perp * (lado * 34f * despliega)
                             - dir * (pulgar ? 26f : 0f);

                // EL DESTINO del dedo:
                Vector2 destino;
                if (agarre > 0.01f && presa != null && presa.active)
                {
                    // LA JAULA: los dedos envuelven la elipse de la presa y
                    // CIÑEN con el apretón (1.02 → 0.78 del radio)
                    float rPresa = presa.Size.Length() * 0.5f;
                    float ang = -MathHelper.PiOver2 + (pulgar ? 1.9f : (k - 1.5f) * 0.62f);
                    // la jaula se orienta CONTRA la mano (viene de dir)
                    float angMundo = Projectile.rotation + ang + MathHelper.PiOver2;
                    Vector2 jaula = presa.Center + new Vector2(MathF.Cos(angMundo), MathF.Sin(angMundo) * 0.86f)
                        * rPresa * (1.04f - 0.26f * apriete);
                    Vector2 paso = raiz + dir * largo + perp * (lado * 10f);
                    destino = Vector2.Lerp(paso, jaula, agarre);
                }
                else
                {
                    // EL PASO DE ARAÑA: cada dedo avanza desfasado — el
                    // caminar de la mano (los dedos CAMINAN, la palma planea)
                    float paso = MathF.Sin(tiempo * 6.4f + k * 1.25f);
                    destino = raiz + dir * (largo * despliega + 18f * paso)
                            + perp * (lado * 12f + 5f * MathF.Sin(tiempo * 3.2f + k));
                }

                Vector2[] dedo = SombrasLib.ColumnaViva(raiz, destino, tiempo, semilla + 101 + k * 13,
                    9, 0.10f, gancho: 0f);
                SombrasLib.Masa(dedo, 4f, 3f, 0.92f * vida, semilla + k * 13, tiempo, grosorCentro: 15f);

                // LA GARRA de hueso en la punta (curvada hacia la presa)
                Vector2 puntaD = dedo[dedo.Length - 1];
                Vector2 haciaD = (destino - raiz).SafeNormalize(dir);
                SombrasLib.Garra(puntaD, haciaD, (pulgar ? 20f : 26f) * despliega, 0.9f * vida,
                    agarre > 0.5f ? 0.65f : 0.25f);
            }

            // === LA BRUMA DE LA MANO — la palma exhala y los intersticios
            // entre los dedos HUMEA al apretar (donde aprieta, quema sombra) ===
            SombrasLib.Bruma(palm, 42f * despliega, 0.38f * vida, tiempo, semilla + 7,
                new Vector2(MathF.Sin(tiempo * 0.8f) * 10f, -12f));
            if (agarre > 0.4f)
            {
                Vector2 posCharcoPresa = SueloBajo(presa != null && presa.active ? presa.Center : palm);
                float crece = MathHelper.Clamp(t / 60f, 0f, 1f);
                SombrasLib.Charco(posCharcoPresa, (46f + 54f * crece) * (0.92f + 0.08f * apriete),
                    0.75f * vida, tiempo, semilla + 61);
                SombrasLib.Bruma(posCharcoPresa + new Vector2(0f, -8f), 40f + 26f * crece,
                    0.42f * vida * (0.6f + 0.4f * (1f - apriete)), tiempo, semilla + 63,
                    new Vector2(0f, -16f));
            }

            // EL TEMBLOR DEL IMPACTO: cada cierre de golpe sacude la mano
            if (fase == FASE_AGARRA && t % 16f >= 9f && t % 16f < 11f)
                SombrasLib.OndaChoque(palm, 46f + 40f * (1f - apriete), 0.4f * vida, roja: true);
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
