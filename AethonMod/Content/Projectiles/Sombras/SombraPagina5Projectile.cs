using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Globals;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Sombras
{
    // ======================================================================
    //  SombraPagina5Projectile — v6.101 → v6.104 → v6.105 — EL QUINTO
    //  TOMO: LA COPIA DE LA SOMBRA DE LA PÁGINA CON LA MIRADA DE ESTRELLA.
    //
    //  LA LETRA DEL USUARIO (v6.105): «copia la sombra de la pagina y
    //  usala para la sombra de la pagina 5 pero usando la nueva textura
    //  de ojabismo con pupila de estrella y el ColmilloAbismo.png».
    //
    //  EL QUINTO TOMO ES LA SOMBRA DE LA PÁGINA (el tomo I) — la copia
    //  FIEL del ritual y del vestido: el charco bajo el portador, LA
    //  MASA PROCEDURAL de siempre (la carne horneada de la casa — la
    //  masa clásica, NO la piel forjada), la bruma negra en DOS CAPAS,
    //  LA CABEZA DE BRUMA en la punta (cabeza y tentáculo son EL MISMO
    //  CUERPO) y EL DEVORADOR al morder (la masa de bruma que cubre al
    //  jefe entero, las almas volviendo por el cuerpo). Con DOS PIEZAS
    //  DE LA FORJA — las únicas que la letra pide:
    //
    //   · LOS OJOS DE ESTRELLA — OjoAbismo.png v6.105 (el ojo AMARILLO
    //     con la PUPILA ESTRELLA DE 5 PUNTAS): el patrón de los OJOS DE
    //     MASA de la original (siete ojos, reparto áureo, apertura
    //     escalonada, el latido) vestido con la textura nueva — TODOS
    //     girados hacia la presa, la página entera TE MIRA con el sello
    //     del grimorio.
    //   · LA BOCA DE COLMILLOS — ColmilloAbismo.png: dos mandíbulas
    //     PEQUEÑAS de cuatro colmillos cada una DENTRO de la cabeza de
    //     bruma (jadean al cazar, se ABREN al llegar a la presa y
    //     TRITURAN al morder) — la boca vive en la bruma de la punta,
    //     discreta, como un gesto.
    //
    //  LO QUE MUERE (la v6.104 se deshace): la piel lisa forjada (la
    //  masa vuelve a ser LA DE LA ORIGINAL), la corona de tres ojos
    //  gigantes, el anillo de sangre, la garganta roja latiendo y las
    //  mandíbulas dobles de cinco colmillos — el quinto tomo ES la
    //  Sombra de la Página; solo cambian SUS OJOS y SU BOCA.
    // ======================================================================
    public class SombraPagina5Projectile : ModProjectile
    {
        /// <summary>FASE: el brote emergiendo del portador.</summary>
        private const byte FASE_EMERGER = 0;

        /// <summary>FASE: la caza de la presa.</summary>
        private const byte FASE_CAZA = 1;

        /// <summary>FASE: el mordisco drenando.</summary>
        private const byte FASE_MORDISCO = 2;

        /// <summary>FASE: la disipación.</summary>
        private const byte FASE_DISIPAR = 3;

        /// <summary>LA PRESA (ai[0] = whoAmI+1; 0 = libre).</summary>
        private NPC Presa
        {
            get
            {
                if (Projectile.ai[0] <= 0f)
                    return null;
                return Main.npc[(int)Projectile.ai[0] - 1];
            }
        }

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

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
            Projectile.extraUpdates = 2;          // la caza corre al triple
        }

        /// <summary>LA RAÍZ de la sombra: el pecho del portador (la
        /// regla del tomo I: el tentáculo SALE DEL JUGADOR).</summary>
        private Vector2 RaizDeSombra(Player dueño) => dueño.MountedCenter;

        /// <summary>LA SOMBRA PROYECTADA en el suelo (para el charco):
        /// baja hasta el primer sólido (34 tiles de caída).</summary>
        private Vector2 SombraProyectada(Player dueño)
        {
            Vector2 desde = dueño.MountedCenter + new Vector2(0f, dueño.height * 0.45f);
            int tx = (int)(desde.X / 16f);
            int ty = (int)(desde.Y / 16f);
            int i;
            for (i = 0; i < 34; i++)
            {
                if (WorldGen.SolidTile(tx, ty, false))
                    break;
                ty++;
            }
            if (i >= 34)
                return desde;                     // no hay suelo: sin charco
            return new Vector2(tx * 16f + 8f, ty * 16f - 4f);
        }

        public override void AI()
        {
            Projectile.ai[2] += 1f;
            float t = Projectile.ai[2];
            NPC presa = Presa;
            Player dueño = Main.player[Projectile.owner];
            byte fase = (byte)Projectile.ai[1];

            // EL ALIENTO ROJO del abismo (solo cliente).
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                Lighting.AddLight(Projectile.Center, new Vector3(0.62f, 0.22f, 0.55f) * 0.85f);

            bool viva = presa != null && presa.active && presa.life > 0;
            if (fase != FASE_DISIPAR && !viva)
            {
                // LA PRESA MURIÓ (o nunca hubo): la sombra se disipa.
                Projectile.ai[1] = FASE_DISIPAR;
                Projectile.ai[2] = 0f;
                fase = FASE_DISIPAR;
                t = 0f;
            }

            switch (fase)
            {
                // ========================================================
                //  FASE 0 — EL EMERGER: el brote sale del pecho del
                //  portador hacia la presa (la curva DeGolpe de la casa).
                // ========================================================
                case FASE_EMERGER:
                {
                    Vector2 raiz = RaizDeSombra(dueño);
                    Vector2 rumbo = presa != null
                        ? Utils.SafeNormalize(presa.Center - raiz, -Vector2.UnitY)
                        : -Vector2.UnitY;
                    float brote = SombrasLib.DeGolpe(t / 26f);
                    Projectile.Center = raiz + rumbo * (30f + 170f * brote);
                    Projectile.rotation = rumbo.ToRotation();
                    if (t == 3f)
                        Sonar(SoundID.Item122.WithPitchOffset(-0.55f).WithVolumeScale(0.75f),
                            Projectile.Center);
                    if (t >= 26f)
                    {
                        Projectile.ai[1] = FASE_CAZA;
                        Projectile.ai[2] = 0f;
                    }
                    break;
                }

                // ========================================================
                //  FASE 1 — LA CAZA: gira hacia la presa SIEMPRE (máx
                //  0.26 rad por tick — SIEMPRE llega, la regla del tomo).
                // ========================================================
                case FASE_CAZA:
                    if (presa != null && viva)
                    {
                        Vector2 hacia = Utils.SafeNormalize(presa.Center - Projectile.Center, Vector2.UnitX);
                        float rapidez = MathHelper.Clamp(
                            Vector2.Distance(presa.Center, Projectile.Center) * 0.05f, 20f, 52f);
                        float rumboActual = Projectile.velocity.ToRotation();
                        float rumboPresa = hacia.ToRotation();
                        float giro = MathHelper.WrapAngle(rumboPresa - rumboActual);
                        Projectile.velocity = (rumboActual + MathHelper.Clamp(giro, -0.26f, 0.26f))
                            .ToRotationVector2() * rapidez;
                        Projectile.rotation = rumboPresa;

                        // LA MARCA del festín cada 15 t (si es jefe).
                        if (t % 15f == 0f && FaucesGlobalNPC.EsJefe(presa))
                            FaucesGlobalNPC.Marcar(presa, 3);

                        // EL CONTACTO: el mordisco.
                        if (Projectile.Hitbox.Intersects(presa.Hitbox))
                        {
                            Projectile.ai[1] = FASE_MORDISCO;
                            Projectile.ai[2] = 0f;
                            Sonar(SoundID.NPCHit9.WithPitchOffset(-0.5f).WithVolumeScale(0.95f),
                                Projectile.Center);
                        }
                    }
                    if (t > 600f)
                    {
                        Projectile.ai[1] = FASE_DISIPAR;
                        Projectile.ai[2] = 0f;
                    }
                    break;

                // ========================================================
                //  FASE 2 — EL MORDISCO: las fauces TRITURAN clavadas a
                //  la presa, DRENA 3% cada 6 t — y al 1 de vida, LA
                //  DEVORACIÓN.
                // ========================================================
                case FASE_MORDISCO:
                    if (presa != null && presa.active && presa.life > 0)
                    {
                        if (FaucesGlobalNPC.EnFauces(presa))
                        {
                            // Otro tentáculo llegó primero: se disipa.
                            Projectile.ai[1] = FASE_DISIPAR;
                            Projectile.ai[2] = 0f;
                            break;
                        }
                        Projectile.Center = presa.Center;
                        Projectile.rotation = (presa.Center - RaizDeSombra(dueño)).ToRotation();
                        if (t % 5f == 0f && FaucesGlobalNPC.EsJefe(presa))
                            FaucesGlobalNPC.Marcar(presa, 3);

                        // EL DREN (solo servidor/SP): 3% de la vida actual
                        // (mínimo 25) cada 6 t — al 1 de vida, EL FESTÍN.
                        if (Main.netMode == NetmodeID.MultiplayerClient || t <= 18f || t % 6f != 0f)
                            break;
                        NPC cabeza = FaucesGlobalNPC.DueñoDelPool(presa);
                        if (cabeza == null || !cabeza.active || cabeza.life <= 1)
                            break;
                        float quitar = MathF.Max(cabeza.life * 0.03f, 25f);
                        if (cabeza.life - quitar <= 1f)
                        {
                            FaucesGlobalNPC.Iniciar(cabeza, 3);   // LA DEVORACIÓN
                            Projectile.ai[1] = FASE_DISIPAR;
                            Projectile.ai[2] = 0f;
                            break;
                        }
                        cabeza.life -= (int)quitar;
                        cabeza.netUpdate = true;
                        if (t % 36f == 0f)
                            Sonar(SoundID.NPCHit9.WithPitchOffset(-0.2f).WithVolumeScale(0.5f),
                                Projectile.Center);
                    }
                    else
                    {
                        Projectile.ai[1] = FASE_DISIPAR;
                        Projectile.ai[2] = 0f;
                    }
                    break;

                // ========================================================
                //  FASE 3 — LA DISIPACIÓN: se deshace en bruma.
                // ========================================================
                case FASE_DISIPAR:
                {
                    if (t == 1f)
                        Sonar(SoundID.Item122.WithPitchOffset(0.2f).WithVolumeScale(0.3f),
                            Projectile.Center);
                    Projectile.velocity *= 0.92f;
                    if (t >= 44f)
                        Projectile.Kill();
                    break;
                }
            }

            // EL RASTRO de sombra (la caída de tinta del cuerpo).
            if (Main.netMode != NetmodeID.Server && Projectile.ai[1] <= 1f && Main.rand.NextBool(6))
            {
                int polvo = Dust.NewDust(Projectile.Center - new Vector2(24f, 24f),
                    48, 48, 27, 0f, -0.3f, 128, default, 0.55f);
                Main.dust[polvo].noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // EN LA CAZA el cuerpo es la COLUMNA ENTERA (la curva viva
            // del tentáculo desde la raíz hasta la cabeza), no la caja.
            if (Projectile.ai[1] != FASE_CAZA)
                return base.Colliding(projHitbox, targetHitbox);
            Player dueño = Main.player[Projectile.owner];
            Vector2[] columna = SombrasLib.Columna(RaizDeSombra(dueño), Projectile.Center,
                Main.GlobalTimeWrappedHourly, Projectile.whoAmI * 17 + 5, 10, 0.24f);
            float punto = 0f;
            for (int i = 1; i < columna.Length; i++)
            {
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                        columna[i - 1], columna[i], 8f, ref punto))
                    return true;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server)
                return false;
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                DrawTodo();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        // ----------------------------------------------------------------
        //  EL VESTIDO — LA COPIA DE LA SOMBRA DE LA PÁGINA (el tomo I)
        //  con los OJOS DE ESTRELLA y LA BOCA DE COLMILLOS de la forja.
        // ----------------------------------------------------------------
        private void DrawTodo()
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 17 + 5;
            Player dueño = Main.player[Projectile.owner];
            NPC presa = Presa;
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];
            Vector2 raiz = RaizDeSombra(dueño);

            // === EL CHARCO — la sombra que el portador proyecta al suelo
            //     (la original: sutil, viva, pero decorado) ===
            SombrasLib.Charco(SombraProyectada(dueño), 44f, 0.55f, tiempo, semilla);

            // === LA COLUMNA y LA MASA — LA ORIGINAL DE VERDAD: la carne
            //     horneada de la casa (la masa clásica del tomo I, el
            //     PERFIL FUSIFORME: fina en la base, panza al centro,
            //     fina en la punta) — la letra manda: «copia la sombra
            //     de la pagina y usala para la sombra de la pagina 5» ===
            Vector2[] col = SombrasLib.ColumnaViva(raiz, Projectile.Center, tiempo, semilla, 20, 0.24f, gancho: 0.5f);
            float disipa = fase == FASE_DISIPAR ? MathHelper.Clamp(1f - t / 44f, 0f, 1f) : 1f;
            SombrasLib.Masa(col, 9f, 13f, 0.95f * disipa, semilla, tiempo, grosorCentro: 44f);

            // === LA BRUMA NEGRA DEL CUERPO (la original): DOS CAPAS de
            //     puffs vivos exhalando a lo largo de toda la columna ===
            SombrasLib.BrumaColumna(col, 46f, 0.62f * disipa, tiempo, semilla, 16);
            SombrasLib.BrumaColumna(col, 30f, 0.46f * disipa, tiempo, semilla + 97, 12);

            // === LOS OJOS DE ESTRELLA — el patrón de la ORIGINAL (los
            //     OJOS DE MASA: siete, reparto áureo, apertura
            //     escalonada, el latido) vestido con LA TEXTURA NUEVA
            //     (OjoAbismo.png v6.105: el ojo AMARILLO con la PUPILA
            //     ESTRELLA DE 5 PUNTAS) — la página entera TE MIRA con
            //     el sello del grimorio ===
            Vector2 objetivo = presa != null && presa.active
                ? presa.Center
                : dueño.MountedCenter + Projectile.velocity * 30f;
            float presencia = fase == FASE_EMERGER ? SombrasLib.DeGolpe(t / 26f) : 1f;
            OjosDeEstrella(col, objetivo, presencia * disipa, semilla, tiempo);

            // === LA CABEZA ES BRUMA (la original): TODO lo que era boca
            //     vive EN LA PUNTA REAL de la columna — cabeza y
            //     tentáculo son EL MISMO CUERPO ===
            Vector2 punta = col[col.Length - 1];
            Vector2 rumbo = (col[col.Length - 1] - col[Math.Max(col.Length - 4, 0)])
                .SafeNormalize(Projectile.rotation.ToRotationVector2());

            if (fase == FASE_MORDISCO && presa != null && presa.active)
            {
                // EL DEVORADOR — la masa de bruma negra y espesa que CUBRE
                // LA TOTALIDAD del jefe, NACE del tentáculo y TRAGA con
                // pulso (las almas vuelven por el cuerpo) — LA ORIGINAL.
                float hambre = MathHelper.Clamp(t / 26f, 0f, 1f);
                SombrasLib.Devorador(punta, presa, hambre, disipa, tiempo, semilla, col);

                // LA BOCA TRITURANDO — cerrada y masticando rápido dentro
                // de la bruma (los colmillos de la forja aprietan).
                BocaDeColmillos(punta, rumbo, 0.10f + 0.07f * MathF.Abs(MathF.Sin(tiempo * 16f)),
                    disipa, semilla, tiempo);

                // la HERIDA: el punto donde el tentáculo ENTERRÓ la punta
                // brilla rojo DENTRO de la bruma (la digestión).
                VFXCore.Begin();
                VFXCore.Quad(punta, SombrasLib.Alfa(SombrasLib.Rojo, 0.42f * disipa),
                    new Vector2(110f, 110f), rumbo.ToRotation());
                VFXCore.FlushAdditive();
            }
            else
            {
                // LA CABEZA DE BRUMA — la punta disuelta en humo que respira
                // hacia la presa (el capullo al emerger, la nariz al cazar).
                float vigor = fase switch
                {
                    FASE_EMERGER => 0.35f + 0.55f * SombrasLib.DeGolpe(t / 26f),
                    FASE_CAZA => 0.85f + 0.15f * MathF.Sin(tiempo * 8f),
                    _ => 0.5f * disipa,
                };
                SombrasLib.CabezaDeBruma(punta, rumbo, vigor * disipa, disipa, tiempo, semilla);

                // LA BOCA DE COLMILLOS — las dos mandíbulas PEQUEÑAS de la
                // forja (ColmilloAbismo.png) DENTRO de la cabeza de bruma:
                // jadean al cazar (más abiertas cuanta más hambre — cerca
                // de la presa se abren ENORME) — el gesto del quinto tomo.
                float apertura = fase == FASE_CAZA ? AperturaDeCaza(presa, punta, tiempo)
                    : fase == FASE_EMERGER ? 0.15f + 0.45f * SombrasLib.DeGolpe(t / 26f)
                    : 0.30f;
                BocaDeColmillos(punta, rumbo, apertura, disipa, semilla, tiempo);

                // MÁS BRUMA JUSTO EN LA PUNTA (la original): la punta que
                // toca al enemigo exhala SU PROPIA nube — el punto de
                // contacto HUMEA: es donde la página MUERDE.
                SombrasLib.Bruma(punta, 40f, 0.5f * disipa, tiempo, semilla + 31,
                    new Vector2(MathF.Sin(tiempo * 0.9f) * 10f, -14f));
            }
        }

        // ----------------------------------------------------------------
        //  LOS OJOS DE ESTRELLA — el patrón OjosDeMasa de la ORIGINAL
        //  (reparto áureo, apertura escalonada, el latido) vestido con
        //  la textura OjoAbismo.png v6.105 (amarillo, pupila estrella).
        // ----------------------------------------------------------------
        private static void OjosDeEstrella(Vector2[] col, Vector2 presa, float presencia,
            int semilla, float tiempo)
        {
            if (presencia <= 0.02f || col == null || col.Length < 4)
                return;

            var textura = SombrasLib.OjoAbismo;
            VFXCore.Begin();
            int n = col.Length;
            const int CANTIDAD = 7;   // los SIETE de la original (la página es TODA ojos)
            for (int k = 0; k < CANTIDAD; k++)
            {
                float t01 = 0.24f + 0.66f * ((k * 0.618034f + 0.13f) % 1f);   // áureo: reparto orgánico
                int idx = (int)(t01 * (n - 1));
                Vector2 pos = col[idx];
                // apertura escalonada + el latido del ojo (la original)
                float local = presencia * CANTIDAD * 0.9f - k * 0.9f;
                float abierto = MathHelper.Clamp(local, 0f, 1f);
                abierto *= 0.85f + 0.15f * MathF.Sin(tiempo * 3.3f + k * 2.1f + Azar(semilla));
                if (abierto <= 0.05f)
                    continue;
                float tamaño = 16f + 9f * Azar(semilla + k * 13);
                // EL OJO DE LA FORJA — el amarillo con la PUPILA ESTRELLA
                // (null-safe: sin textura cae al ojo rasgado de siempre —
                // la sombra JAMÁS se queda ciega).
                SombrasLib.OjoTexturizado(pos, tamaño, presa - pos, abierto, textura);
            }
            VFXCore.FlushAdditive();
        }

        // ----------------------------------------------------------------
        //  LA APERTURA de la boca al cazar: jadea (más abierta cuanta
        //  más hambre) y cerca de la presa se abre ENORME.
        // ----------------------------------------------------------------
        private static float AperturaDeCaza(NPC presa, Vector2 punta, float tiempo)
        {
            float hambre = 0.42f + 0.22f * MathF.Sin(tiempo * 7f);
            if (presa != null && presa.active)
            {
                float cerca = MathHelper.Clamp(
                    1f - Vector2.Distance(presa.Center, punta) / 420f, 0f, 1f);
                hambre = MathHelper.Lerp(hambre, 0.95f + 0.08f * MathF.Sin(tiempo * 15f),
                    cerca * cerca);
            }
            return hambre;
        }

        // ----------------------------------------------------------------
        //  LA BOCA DE COLMILLOS — las dos mandíbulas PEQUEÑAS de la
        //  forja (ColmilloAbismo.png: la aguja curva de marfil con su
        //  encía violeta) DENTRO de la cabeza de bruma: CUATRO colmillos
        //  por lado en abanico, jadeando según <paramref name="apertura"/>
        //  (0..1) — un GESTO dentro del humo, no una fauce de teatro.
        // ----------------------------------------------------------------
        private static void BocaDeColmillos(Vector2 punta, Vector2 rumbo, float apertura,
            float vive, int semilla, float tiempo)
        {
            var colmilloForja = SombrasLib.ColmilloAbismo;
            if (vive <= 0.03f || (colmilloForja == null && VFXCore.Colmillo == null))
                return;
            float ang = rumbo.ToRotation();
            Vector2 perp = new(-rumbo.Y, rumbo.X);
            float abre = 0.08f + 0.50f * MathHelper.Clamp(apertura, 0f, 1f);   // rad por lado

            VFXCore.Begin();
            for (int lado = -1; lado <= 1; lado += 2)
            {
                for (int k = 0; k < 4; k++)
                {
                    float f = k / 3f;                          // 0 = comisura, 1 = delante
                    float angCol = ang + lado * (0.08f + abre * (0.30f + 0.70f * f));
                    Vector2 nace = punta - rumbo * (11f * (1f - f)) + perp * lado * 5f;
                    float largo = (17f + 12f * (1f - MathF.Abs(f - 0.45f) * 2f))
                        * (0.9f + 0.2f * Azar(semilla + lado * 5 + k));
                    Vector2 rumboCol = angCol.ToRotationVector2();
                    Vector2 posCol = nace + rumboCol * largo * 0.5f;
                    // EL COLMILLO DE LA FORJA (null-safe: sin textura cae
                    // al horneado de la casa — la boca JAMÁS se queda sin dientes)
                    SombrasLib.ColmilloTexturizado(posCol, largo, angCol, 0.94f * vive, colmilloForja);
                }
            }
            VFXCore.FlushAdditive();
        }

        /// <summary>El azar determinista de la casa.</summary>
        private static float Azar(int i)
        {
            float v = MathF.Sin(i * 12.9898f + 78.233f) * 43758.547f;
            return v - MathF.Floor(v);
        }

        /// <summary>El sonido blindado (la lección de las sombras: en
        /// AI gated != Server ya suena en SP y clientes).</summary>
        private static void Sonar(SoundStyle estilo, Vector2 pos)
        {
            try
            {
                SoundEngine.PlaySound(in estilo, pos);
            }
            catch { /* el sonido jamás tumba el ritual */ }
        }
    }
}
