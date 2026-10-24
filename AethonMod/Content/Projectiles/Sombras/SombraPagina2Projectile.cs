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
    /// SOMBRAPAGINA2PROJECTILE — v6.51.12 — LA SOMBRA DE LA PÁGINA II:
    /// EL SEGUNDO TOMO (la letra: «crea la sombra de la pagina 2 y a
    /// esta segunda version quiero que le mejores las textura, usa
    /// nuestras librerias y nuestros iris, tambien hacerlo lo mas
    /// detallado posible»).
    ///
    /// EL MISMO FESTÍN DEL TOMO UNO (emerge del portador → caza al jefe
    /// a cualquier distancia → muerde y drena 3 %/6 t → a 1 HP el motor
    /// de devoración, estilo 3) con EL CUERPO MÁS DETALLADO DE LA CASA:
    ///
    ///   · LA DOBLE HÉLICE: DOS ColumnaViva trenzadas alrededor de una
    ///     ESPINA central (la página doblada en dos — cada hebra con SU
    ///     perfil fusiforme y SU bruma).
    ///   · LOS IRIS REALES («nuestros iris»): LOS SPRITES DEL ESCUADRÓN
    ///     — el Ojo Eterno abierto/medio/cerrado con SU iris viajero —
    ///     repartidos por las dos hebras, cada uno con SU parpadeo
    ///     determinista y SU mirada clavada en la presa. Se dibujan en
    ///     el lote vanilla (el contrato de los sprites).
    ///   · LOS RAYOS PERLIN: descargas reales del Trueno Perlin
    ///     (StormLib.PerlinBolt) chispeando ENTRE las hebras — un arco
    ///     por pareja de ojos, vivo, con ramitas.
    ///   · LAS GARRAS DE LA BOCA: un anillo de 3 GARRAS de 3 segmentos
    ///     (SombrasLib.Garra) que se abre con el masticar y CIERRA DE
    ///     GOLPE al morder.
    ///   · EL MORDISCO MAYOR: EL DEVORADOR (la elipse del jefe entero)
    ///     + LA CORONA DE 9 GARRAS del festín del arma 4 (el borde
    ///     dentado que sube) + LA HERIDA DOBLE + el río de almas.
    ///   · EL CHARCO DOBLE con rim violeta (la lectura nocturna de la
    ///     lección .66) y el ALIENTO aditivo de la bruma.
    /// </summary>
    public class SombraPagina2Projectile : ModProjectile
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

        // ==================================================================
        //  LA VIDA (la misma del tomo uno — el festín no se toca)
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
                    Projectile.Center = raiz + rumbo * (30f + 170f * avance);
                    Projectile.rotation = rumbo.ToRotation();
                    if (t == 3) Sonar(SoundID.Item122.WithPitchOffset(-0.45f).WithVolumeScale(0.75f), Projectile.Center);
                    // LA APERTURA DE LA PÁGINA: el doble nace con un chasquido perlin
                    if (t == 12 && Main.netMode != NetmodeID.Server)
                        Sonar(SoundID.Item122.WithPitchOffset(0.35f).WithVolumeScale(0.35f), Projectile.Center);
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

                        // LA MARCA CONTINUA (la regla de la .69 del tomo uno)
                        if (t % 15f == 0f && FaucesGlobalNPC.EsJefe(presa))
                            FaucesGlobalNPC.Marcar(presa, 3);

                        if (Projectile.Hitbox.Intersects(presa.Hitbox))
                        {
                            Projectile.ai[1] = FASE_MORDISCO;
                            Projectile.ai[2] = 0;
                            Sonar(SoundID.NPCHit9.WithPitchOffset(-0.35f).WithVolumeScale(0.9f), Projectile.Center);
                            Sonar(SoundID.Item122.WithPitchOffset(-0.6f).WithVolumeScale(0.6f), Projectile.Center);
                        }
                    }
                    if (t > 600) { Projectile.ai[1] = FASE_DISIPAR; Projectile.ai[2] = 0; }
                    break;
                }
                case FASE_MORDISCO:
                {
                    if (presa != null && presa.active && presa.life > 0)
                    {
                        // EL FESTÍN DE LAS FAUCES MANDA (la cortesía de la .110)
                        if (FaucesGlobalNPC.EnFauces(presa))
                        {
                            Projectile.ai[1] = FASE_DISIPAR;
                            Projectile.ai[2] = 0;
                            break;
                        }
                        Projectile.Center = presa.Center;
                        Projectile.rotation = (presa.Center - RaizDeSombra(dueño)).ToRotation();

                        if (t % 5f == 0f && FaucesGlobalNPC.EsJefe(presa))
                            FaucesGlobalNPC.Marcar(presa, 3);

                        // EL DRENA MAYOR DEL SEGUNDO TOMO: 3 % cada 6 t
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

        // ==================================================================
        //  EL DIBUJO — EL CUERPO DOBLE Y DETALLADO (el contrato del lote:
        //  cerrar → dibujar → REABRIR vanilla → sprites de ojos en vanilla)
        // ==================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                DrawElCuerpoDoble();
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();

            // LOS IRIS REALES («nuestros iris») van en el LOTE VANILLA —
            // el contrato de los sprites de la casa
            try { DibujarLosIrisReales(); } catch { }
            return false;
        }

        /// <summary>LA ESPINA CENTRAL (la ruta del cuerpo — la hebra
        /// madre de la que cuelgan las dos trenzas).</summary>
        private Vector2[] Espina(float tiempo, int semilla, Vector2 raiz)
            => SombrasLib.ColumnaViva(raiz, Projectile.Center, tiempo, semilla, 22, 0.24f, gancho: 0.5f);

        /// <summary>UNA HEBRA DE LA HÉLICE: la espina desplazada en
        /// espiral (la trenza de la página doblada — el radio respira
        /// con el tiempo y se abre en la punta).</summary>
        private Vector2[] Hebra(Vector2[] espina, float tiempo, int semilla, float faseHelice,
            float radioBase, float abrePunta)
        {
            var hebra = new Vector2[espina.Length];
            for (int i = 0; i < espina.Length; i++)
            {
                float u = i / (float)(espina.Length - 1);         // 0 raíz → 1 punta
                // la dirección local de la espina (perpendicular = el plano de la trenza)
                Vector2 dir = espina[Math.Min(i + 1, espina.Length - 1)] -
                    espina[Math.Max(i - 1, 0)];
                Vector2 perp = new Vector2(-dir.Y, dir.X);
                if (perp.LengthSquared() < 0.001f) perp = Vector2.UnitX;
                perp.Normalize();
                // EL GIRO DE LA TRENZA: la fase avanza con la distancia y
                // el reloj (la hélice VIVA — giran las dos hebras)
                float giro = faseHelice + u * 9.5f + tiempo * 2.2f;
                // el radio: fino en la raíz, respirando, ABRIÉNDOSE en la punta
                float radio = radioBase * (0.35f + 0.65f * u) *
                    (1f + 0.15f * MathF.Sin(tiempo * 2.5f + u * 6f)) + abrePunta * u * u;
                hebra[i] = espina[i] + perp * (MathF.Cos(giro) * radio) +
                    dir.SafeNormalize(Vector2.UnitX) * MathF.Sin(giro) * radio * 0.35f;
            }
            return hebra;
        }

        private void DrawElCuerpoDoble()
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 17 + 5;
            Player dueño = Main.player[Projectile.owner];
            NPC presa = Presa;
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];

            Vector2 raiz = RaizDeSombra(dueño);
            float disipa = fase == FASE_DISIPAR ? MathHelper.Clamp(1f - t / 52f, 0f, 1f) : 1f;

            // === EL CHARCO DOBLE (con RIM violeta — legible hasta de
            //     noche, la lección .66: el negro solo NO se ve) ===
            SombrasLib.Charco(SombraProyectada(dueño), 58f, 0.6f * disipa, tiempo, semilla);
            VFXCore.Begin();
            VFXCore.Quad(SombraProyectada(dueño),
                SombrasLib.Alfa(new Color(140, 80, 220), 0.10f * disipa),
                new Vector2(96f, 42f), 0f);
            VFXCore.FlushAdditive();

            // === LA ESPINA CENTRAL ===
            Vector2[] espina = Espina(tiempo, semilla, raiz);

            // === LA DOBLE HÉLICE — DOS hebras trenzadas (fases opuestas:
            //     la página doblada en dos, girando en sentidos opuestos) ===
            Vector2[] hebraA = Hebra(espina, tiempo, semilla, 0f, 16f, 26f);
            Vector2[] hebraB = Hebra(espina, tiempo, semilla + 53, MathHelper.Pi, 16f, 26f);

            // LAS DOS MASAS (perfil fusiforme fino — la trenza, no el garrote)
            SombrasLib.Masa(hebraA, 6f, 11f, 0.95f * disipa, semilla, tiempo, grosorCentro: 26f);
            SombrasLib.Masa(hebraB, 6f, 11f, 0.95f * disipa, semilla + 53, tiempo, grosorCentro: 26f);

            // Y LA ESPINA ENTRE ELLAS — más fina, el lomo del libro
            SombrasLib.Masa(espina, 3f, 7f, 0.65f * disipa, semilla + 7, tiempo, grosorCentro: 18f);

            // === LA BRUMA DE LAS TRES CUERDAS (tres capas — el doble
            //     exhala el DOBLE de humo: la letra de «más bruma») ===
            SombrasLib.BrumaColumna(espina, 54f, 0.60f * disipa, tiempo, semilla, 16);
            SombrasLib.BrumaColumna(hebraA, 30f, 0.42f * disipa, tiempo, semilla + 97, 12);
            SombrasLib.BrumaColumna(hebraB, 30f, 0.42f * disipa, tiempo, semilla + 151, 12);

            // === LOS OJOS PROCEDURALES ENTRE LAS HEBRAS (la mirada
            //     persistente — los iris REALES van aparte, en vanilla) ===
            Vector2 objetivo = presa != null && presa.active
                ? presa.Center
                : dueño.MountedCenter + Projectile.velocity * 30f;
            float presencia = fase == FASE_EMERGER ? SombrasLib.DeGolpe(t / 26f) : 1f;
            SombrasLib.OjosDeMasa(hebraA, objetivo, presencia * disipa * 0.7f, semilla, tiempo, 5);
            SombrasLib.OjosDeMasa(hebraB, objetivo, presencia * disipa * 0.7f, semilla + 53, tiempo, 5);

            // === LOS RAYOS PERLIN ENTRE LAS HEBRAS (la letra: «usa
            //     nuestras librerias» — StormLib.PerlinBolt de verdad):
            //     una descarga VIVA que salta entre hebra y hebra,
            //     recorriendo el cuerpo — la tinta ELECTRIFICADA del
            //     segundo tomo. ===
            if (fase != FASE_DISIPAR || t < 20f)
            {
                int flick = StormLib.FlickTick(tiempo, 14f);
                // LA POSICIÓN DEL SALTO viaja por el cuerpo (determinista)
                float viaja = SombrasLib.Frac(tiempo * 0.22f);
                int idxSalto = (int)(viaja * (espina.Length - 4)) + 1;
                int seedArco = Projectile.whoAmI * 31 + idxSalto;
                if (StormLib.IsLit(seedArco, flick, 0.55f))
                {
                    Vector2 a = hebraA[idxSalto], b = hebraB[idxSalto];
                    StormLib.PerlinBolt(Main.spriteBatch, a - Main.screenPosition,
                        b - Main.screenPosition, seedArco, flick, 3.2f,
                        new Color(190, 110, 255), new Color(240, 224, 255),
                        0.85f * disipa, 0.22f);
                }
            }

            // === LA CABEZA Y LA BOCA ===
            Vector2 punta = espina[espina.Length - 1];
            Vector2 rumbo = (espina[espina.Length - 1] - espina[Math.Max(espina.Length - 4, 0)])
                .SafeNormalize(Projectile.rotation.ToRotationVector2());
            float muerde = CicloMasticar(t);

            if (fase == FASE_MORDISCO && presa != null && presa.active)
            {
                // === EL MORDISCO MAYOR: EL DEVORADOR + LA CORONA DE 9
                //     GARRAS (el borde dentado del festín del arma 4,
                //     mordiendo el plano que sube) + LA HERIDA DOBLE ===
                float hambre = MathHelper.Clamp(t / 26f, 0f, 1f);
                SombrasLib.Devorador(punta, presa, hambre, disipa, tiempo, semilla, espina);

                // LA CORONA DE GARRAS alrededor del jefe (cierran con el pulso)
                VFXCore.Begin();
                float pulso = 0.5f + 0.5f * MathF.Sin(tiempo * 5.5f);
                float radioCorona = MathHelper.Clamp(presa.Size.Length() * 0.65f, 80f, 260f);
                for (int k = 0; k < 9; k++)
                {
                    float ang = k * MathHelper.TwoPi / 9f + tiempo * 0.4f;
                    Vector2 baseG = presa.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                        (radioCorona * (1.18f - 0.14f * pulso));
                    Vector2 hacia = presa.Center - baseG;
                    SombrasLib.Garra(baseG, hacia, 34f * disipa, 0.8f * disipa,
                        gancho: 0.5f + 0.3f * pulso);
                }
                VFXCore.FlushAdditive();

                // LA HERIDA DOBLE (dos puntas enterradas — la página doblada)
                VFXCore.Begin();
                VFXCore.Quad(punta + rumbo * 12f, SombrasLib.Alfa(SombrasLib.Rojo,
                    (0.46f + 0.22f * MathF.Sin(tiempo * 6f)) * disipa),
                    new Vector2(130f, 130f), rumbo.ToRotation());
                VFXCore.Quad(punta - rumbo * 12f, SombrasLib.Alfa(SombrasLib.Rojo,
                    (0.30f + 0.18f * MathF.Sin(tiempo * 6f + 2f)) * disipa),
                    new Vector2(90f, 90f), rumbo.ToRotation());
                VFXCore.FlushAdditive();

                // EL RÍO DE ALMAS (doble — la letra del segundo tomo)
                VFXCore.Begin();
                for (int k = 0; k < 8; k++)
                {
                    float prog = SombrasLib.Frac(t * 0.02f + k * 0.125f);
                    Vector2 a = presa.Center + new Vector2(MathF.Cos(k * 2.4f + tiempo * 0.8f),
                        MathF.Sin(k * 3.2f + tiempo)) * 70f;
                    Vector2 b = dueño.MountedCenter;
                    Vector2 ctrl = (a + b) * 0.5f + new Vector2(MathF.Sin(k * 2.1f) * 130f, -160f);
                    float u = 1f - prog;
                    Vector2 pos = u * u * a + 2f * u * prog * ctrl + prog * prog * b;
                    SombrasLib.Ojo(pos, 8f, b - pos, 1f);
                }
                VFXCore.FlushAdditive();
            }
            else
            {
                // === LA CABEZA DOBLE: cada hebra termina en SU cabeza de
                //     bruma (las dos narices de la página abierta) ===
                float vigor = fase switch
                {
                    FASE_EMERGER => 0.35f + 0.55f * SombrasLib.DeGolpe(t / 26f),
                    FASE_CAZA => 0.85f + 0.15f * MathF.Sin(tiempo * 8f),
                    _ => 0.5f * disipa,
                };
                Vector2 puntaA = hebraA[hebraA.Length - 1];
                Vector2 puntaB = hebraB[hebraB.Length - 1];
                SombrasLib.CabezaDeBruma(puntaA, (puntaA - hebraA[hebraA.Length - 4])
                    .SafeNormalize(rumbo), vigor * disipa, disipa, tiempo, semilla);
                SombrasLib.CabezaDeBruma(puntaB, (puntaB - hebraB[hebraB.Length - 4])
                    .SafeNormalize(rumbo), vigor * disipa, disipa, tiempo, semilla + 53);

                // === EL ANILLO DE GARRAS DE LA BOCA: 3 garras que se
                //     ABREN con el masticar y CIERRAN DE GOLPE — la boca
                //     del segundo tomo es de garras, no de bruma ===
                VFXCore.Begin();
                float apertura = 0.35f + 0.55f * muerde;
                for (int k = 0; k < 3; k++)
                {
                    float ang = rumbo.ToRotation() + MathHelper.Pi +
                        (k - 1) * apertura * 0.85f;
                    Vector2 baseG = punta + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 20f;
                    SombrasLib.Garra(baseG, new Vector2(MathF.Cos(ang), MathF.Sin(ang)),
                        30f * disipa, 0.85f * disipa, gancho: 0.55f);
                }
                VFXCore.FlushAdditive();

                // Y LA BRUMA DEL PUNTO DE CONTACTO (más densa — donde muerde)
                SombrasLib.Bruma(punta, 46f, 0.55f * disipa, tiempo, semilla + 31,
                    new Vector2(MathF.Sin(tiempo * 0.9f) * 12f, -16f));
            }
        }

        // ==================================================================
        //  LOS IRIS REALES («nuestros iris» — la letra): LOS SPRITES DEL
        //  ESCUADRÓN a lo largo de las DOS hebras — el Ojo Eterno con su
        //  iris que MIRA a la presa y sus párpados de textura. En el
        //  LOTE VANILLA (el contrato de los sprites).
        // ==================================================================

        private void DibujarLosIrisReales()
        {
            NPC presa = Presa;
            Player dueño = Main.player[Projectile.owner];
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 17 + 5;
            float disipa = fase == FASE_DISIPAR ? MathHelper.Clamp(1f - t / 52f, 0f, 1f) : 1f;
            if (disipa <= 0.02f) return;

            Texture2D abierto = ModContent.Request<Texture2D>(
                "AethonMod/Content/Projectiles/OjoEterno_Abierto").Value;
            Texture2D medio = ModContent.Request<Texture2D>(
                "AethonMod/Content/Projectiles/OjoEterno_Medio").Value;
            Texture2D cerrado = ModContent.Request<Texture2D>(
                "AethonMod/Content/Projectiles/OjoEterno_Cerrado").Value;
            Texture2D iris = ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
            if (abierto == null || medio == null || cerrado == null || iris == null) return;

            Vector2 raiz = RaizDeSombra(dueño);
            Vector2[] espina = Espina(tiempo, semilla, raiz);
            Vector2[] hebraA = Hebra(espina, tiempo, semilla, 0f, 16f, 26f);
            Vector2[] hebraB = Hebra(espina, tiempo, semilla + 53, MathHelper.Pi, 16f, 26f);

            Vector2 objetivo = presa != null && presa.active
                ? presa.Center
                : dueño.MountedCenter + Projectile.velocity * 30f;

            // CUATRO OJOS REALES: dos por hebra (en el primer tercio y el
            // último — la mirada repartida por la trenza), cada uno con SU
            // parpadeo determinista (el reloj del mundo + su semilla)
            DibujarOjoReal(hebraA, 4, abierto, medio, cerrado, iris, objetivo, tiempo,
                semilla, disipa * 0.95f);
            DibujarOjoReal(hebraA, hebraA.Length - 5, abierto, medio, cerrado, iris, objetivo, tiempo,
                semilla + 11, disipa);
            DibujarOjoReal(hebraB, 6, abierto, medio, cerrado, iris, objetivo, tiempo,
                semilla + 23, disipa * 0.95f);
            DibujarOjoReal(hebraB, hebraB.Length - 6, abierto, medio, cerrado, iris, objetivo, tiempo,
                semilla + 37, disipa);
        }

        /// <summary>UN OJO REAL DE LA HEBRA: el sprite del escuadrón con
        /// SU mirada (el iris viaja hacia la presa ±4 px) y SU párpado
        /// (abierto → medio → cerrado — el ciclo de tres fases del arma).</summary>
        private void DibujarOjoReal(Vector2[] hebra, int idx,
            Texture2D abierto, Texture2D medio, Texture2D cerrado, Texture2D iris,
            Vector2 objetivo, float tiempo, int semillaOjo, float alfa)
        {
            if (hebra == null || hebra.Length == 0 || alfa <= 0.03f) return;
            idx = Math.Clamp(idx, 0, hebra.Length - 1);
            Vector2 pos = hebra[idx];

            // EL PARPADEO (determinista — el reloj del mundo + SU semilla):
            // abierto casi siempre, el medio un instante, cerrado un latido
            float parp = (tiempo * 1.05f + SombrasLib.semille(semillaOjo) * 6.5f) % 6.8f;
            Texture2D cuerpo;
            if (parp <= 0.22f) cuerpo = cerrado;
            else if (parp <= 0.40f || parp > 6.8f - 0.16f) cuerpo = medio;
            else cuerpo = abierto;

            // LA MIRADA: el iris viaja hacia la presa (±4 px — el
            // recorrido de la almendra del arma, en miniatura)
            Vector2 d = objetivo - pos;
            float len = d.Length();
            Vector2 mirada = len > 4f
                ? new Vector2(d.X / len * 4f, d.Y / len * 2.6f) : Vector2.Zero;

            // LA LUZ con PISO (la lección .66: el ojo NUNCA se apaga del todo)
            Color luz = Color.Lerp(Lighting.GetColor(pos.ToTileCoordinates()),
                new Color(226, 218, 244), 0.45f) * alfa;

            const float esc = 0.38f;   // el escuadrón del arma usa 0,375 — igual: familia
            Vector2 pantalla = pos - Main.screenPosition;
            Main.spriteBatch.Draw(cuerpo, pantalla, null, luz, 0f,
                cuerpo.Size() * 0.5f, esc, SpriteEffects.None, 0f);

            // EL IRIS SOLO CON EL OJO ABIERTO/MEDIO (cerrado: el párpado tapa)
            if (cuerpo != cerrado)
            {
                Main.spriteBatch.Draw(iris, pantalla + mirada * esc, null, luz, 0f,
                    iris.Size() * 0.5f, 0.55f * esc, SpriteEffects.None, 0f);
                // y el HALO aditivo pequeñito (la mirada BRILLA — el rim)
                VFXCore.Begin();
                VFXCore.Quad(pos, SombrasLib.Alfa(new Color(178, 110, 236),
                    0.20f * alfa), new Vector2(26f, 26f), 0f);
                VFXCore.FlushAdditive();
            }
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }

        /// <summary>El masticar de la boca de garras (el ciclo del
        /// festín: abre… ¡CIERRA de golpe! …mastica… reabre).</summary>
        private static float CicloMasticar(float t)
        {
            float m = (t - 18f) % 30f;
            if (m < 10f) return MathHelper.Lerp(0.2f, 1f, m / 10f);                          // abre
            if (m < 15f) return MathHelper.Lerp(1f, 0.03f, SombrasLib.DeGolpe((m - 10f) / 5f)); // ¡CIERRA!
            if (m < 25f) return 0.15f + 0.1f * MathF.Sin(m * 1.5f);                          // mastica
            return MathHelper.Lerp(0.15f, 0.85f, (m - 25f) / 5f);                            // reabre
        }
    }
}
