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
    /// v6.50.68 — LA LETRA DEL USUARIO, DOS CIRUGÍAS (el resto INTACTO):
    /// (1) «la base del tentáculo nace en el suelo bajo el jugador y no
    /// sobre el jugador como debe ser» → LA RAÍZ ES EL CUERPO DEL
    /// PORTADOR (MountedCenter): el tentáculo SALE DEL JUGADOR esté donde
    /// esté — el charco queda como su SOMBRA proyectada en el suelo
    /// (mero decorado, ya no es el origen). (2) «la boca deja mucho que
    /// desear… la boca y el tentáculo deben ser uno solo… en vez de boca
    /// una masa de bruma negra cubre la totalidad de un jefe y nace del
    /// tentáculo hacia el jefe» → LAS FAUCES PROCEDURALES FUERON
    /// RETIRADAS: la punta se disuelve en LA CABEZA DE BRUMA (caza) y al
    /// morder EL DEVORADOR — la masa de bruma negra y espesa que cubre
    /// TODO el jefe, nace de la punta REAL de la columna y traga con
    /// pulso mientras las almas vuelven al portador por el cuerpo.
    ///
    /// «La misma arma, pero SOLO POR CÓDIGO»: cero sprites — ni uno.
    /// El tentáculo COMPLETO nace, caza, muerde y disipa dibujado con
    /// los pinceles del motor (Carne + Colmillo + Ventosa + BrumaFX vía
    /// la SombrasLib): la masa negra muscular, los ojos que se abren DE
    /// GOLPE y miran, el borde de energía que fluye y la bruma que come
    /// la luz.
    ///
    /// Mismo comportamiento que el ARMA 1 (emerge del jugador → caza al
    /// jefe a cualquier distancia → muerde y drena 2.5%/6t → a 1 HP el
    /// motor del festín, estilo 3).
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

        /// <summary>
        /// v6.50.68 — LA RAÍZ ES EL JUGADOR (la letra del usuario: «la base
        /// del tentáculo nace en el suelo bajo el jugador y no sobre el
        /// jugador como debe ser»). El tentáculo SALE DEL CUERPO del
        /// portador — vuela, salta o camine: la base SIEMPRE está en él.
        /// </summary>
        private Vector2 RaizDeSombra(Player dueño)
        {
            return dueño.MountedCenter;
        }

        /// <summary>
        /// v6.50.68 — LA SOMBRA PROYECTADA del portador en el suelo (solo
        /// decorado: el charco de siempre, pero YA NO ES el origen del
        /// tentáculo — es la sombra que el portador echa al volar).
        /// </summary>
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

                        // v6.50.69 — LA MARCA CONTINUA (la letra: «a veces La
                        // Sombra de la Página no se activa y no hace el acto
                        // de devoración con los jefes»): mientras el tentáculo
                        // TIENE UN JEFE como presa, la marca se REFRESCA — si
                        // OTRA cosa (esbirro, otro jugador, un DoT) le inflige
                        // el golpe mortal, la muerte SIGE SIENDO NUESTRA y el
                        // festín arranca igual. Antes la marca solo vivía en el
                        // golpe mortal NUESTRO: si otro lo mataba, el jefe se
                        // moría «normal» y el usuario veía que NO se activaba
                        if (t % 15f == 0f && FaucesGlobalNPC.EsJefe(presa))
                            FaucesGlobalNPC.Marcar(presa, 3);

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

                        // v6.50.69 — LA MARCA CONTINUA mientras MUERDE (cada
                        // 5 t): el golpe mortal de CUALQUIER fuente durante la
                        // mordida también dispara la devoración
                        if (t % 5f == 0f && FaucesGlobalNPC.EsJefe(presa))
                            FaucesGlobalNPC.Marcar(presa, 3);

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

            // === EL CHARCO — v6.50.68: ya NO es la raíz (el tentáculo nace
            // DEL JUGADOR): es la SOMBRA que el portador proyecta al suelo —
            // sutil, viva, pero decorado ===
            SombrasLib.Charco(SombraProyectada(dueño), 44f, 0.55f, tiempo, semilla);

            // === LA COLUMNA y LA MASA ===
            // v6.50.67 — LA COLUMNA VIVA: física de verlet con inercia y
            // ONDA VIAJERA (la letra del GIF del usuario) + el GANCHO de
            // la punta — la base también se hace FLUIDA
            Vector2[] col = SombrasLib.ColumnaViva(raiz, Projectile.Center, tiempo, semilla, 20, 0.24f, gancho: 0.5f);
            float disipa = fase == FASE_DISIPAR ? MathHelper.Clamp(1f - t / 44f, 0f, 1f) : 1f;
            SombrasLib.Masa(col, 40f, 12f, 0.95f * disipa, semilla, tiempo);

            // === LA BRUMA NEGRA DEL CUERPO (v6.50.64): el tentáculo
            // 100% código también EXHALA — puﬀs vivos a lo largo ===
            // v6.50.69 — MÁS BRUMA (la letra: «la cantidad de bruma en el
            // tentáculo no es suficiente, necesita más»): DOS CAPAS (16
            // puffs gruesos + 12 finos desfasados — antes una sola de 10)
            // y cada puff nace más gordo y crece más (BrumaColumna v2:
            // ×1.6 de crecimiento, 7 halos de aliento)
            SombrasLib.BrumaColumna(col, 46f, 0.62f * disipa, tiempo, semilla, 16);
            SombrasLib.BrumaColumna(col, 30f, 0.46f * disipa, tiempo, semilla + 97, 12);

            // === LOS OJOS (más ojos que el arma 1: la página es TODA ojos) ===
            Vector2 objetivo = presa != null && presa.active
                ? presa.Center
                : dueño.MountedCenter + Projectile.velocity * 30f;
            float presencia = fase == FASE_EMERGER ? SombrasLib.DeGolpe(t / 26f) : 1f;
            SombrasLib.OjosDeMasa(col, objetivo, presencia * disipa, semilla, tiempo, 7);

            // === v6.50.68 — LA CABEZA ES BRUMA (la boca procedural RETIRADA
            // por la letra del usuario): TODO lo que era boca ahora vive EN LA
            // PUNTA REAL de la columna — cabeza y tentáculo son EL MISMO
            // CUERPO por construcción, jamás vuelve a haber separación ===
            Vector2 punta = col[col.Length - 1];
            Vector2 rumbo = (col[col.Length - 1] - col[Math.Max(col.Length - 4, 0)])
                .SafeNormalize(Projectile.rotation.ToRotationVector2());

            if (fase == FASE_MORDISCO && presa != null && presa.active)
            {
                // EL DEVORADOR — la masa de bruma negra y espesa que CUBRE
                // LA TOTALIDAD del jefe, NACE del tentáculo y TRAGA con
                // pulso (las almas vuelven por el cuerpo)
                float hambre = MathHelper.Clamp(t / 26f, 0f, 1f);
                SombrasLib.Devorador(punta, presa, hambre, disipa, tiempo, semilla, col);

                // la HERIDA: el punto donde el tentáculo ENTERRÓ la punta
                // brilla rojo DENTRO de la bruma (la digestión)
                VFXCore.Begin();
                VFXCore.Quad(punta, SombrasLib.Alfa(SombrasLib.Rojo, 0.42f * disipa),
                    new Vector2(110f, 110f), rumbo.ToRotation());
                VFXCore.FlushAdditive();
            }
            else
            {
                // LA CABEZA DE BRUMA — la punta disuelta en humo que respira
                // hacia la presa (el capullo al emerger, la nariz al cazar)
                float vigor = fase switch
                {
                    FASE_EMERGER => 0.35f + 0.55f * SombrasLib.DeGolpe(t / 26f),
                    FASE_CAZA => 0.85f + 0.15f * MathF.Sin(tiempo * 8f),
                    _ => 0.5f * disipa,
                };
                SombrasLib.CabezaDeBruma(punta, rumbo, vigor * disipa, disipa, tiempo, semilla);
            }
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
