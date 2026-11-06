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
    //  SombraPagina5Projectile — v6.101 — EL QUINTO TOMO: EL TENTÁCULO
    //  DEL ABISMO MORDAZ (la letra del usuario: «crea la sombra de la
    //  pagina 5, pero lo que tienes que cambiar es la textura del
    //  tentaculo, sus ojos y dientes, intenta que se vea mas increible
    //  y aterrador»).
    //
    //  EL RITUAL ES EL DE LA FAMILIA (el tomo I): el tentáculo EMERGE
    //  del portador, CAZA a la presa NO IMPORTA LA DISTANCIA, Muerde y
    //  DRENA 3% cada 6 t — y a 1 de vida, LA DEVORACIÓN.
    //
    //  LO QUE CAMBIA ES LA PIEL DEL TENTÁCULO (más increíble y
    //  aterrador):
    //   · v6.101 — LOS OJOS DEL ABISMO (once ojos rasgados que
    //     parpadean cada uno a su ritmo), LA SIERRA DOBLE de
    //     colmillos y LA FAUCE que jadea y tritura — TODO vivo.
    //   · v6.102 — LA PIEL FORJADA (la letra: «el tentaculo debe ser
    //     rediseñado, con nuevas texturas, usa la forja o el taller
    //     para crear las texturas del tentaculo»): el cuerpo ya no
    //     viste la carne horneada de siempre — viste LA CARNE FORJADA
    //     (TentaculoAbismo.png 128×512: LA PLUMA IA + EL MOLDE de la
    //     casa + LA SASTRERÍA del Ribbon — máscara U que respira,
    //     espejo periódico C0 en V, premultiplicado del lote alfa):
    //     ridges musculares, DOS HILERAS DE VENTOSAS que brillan y
    //     VENAS CARMESÍ horneadas en la propia piel (las capas de
    //     espinas y ventosas sueltas de la masa clásica MURIERON:
    //     la piel trae las suyas). EL RITUAL ES EL DE LA FAMILIA —
    //     IGUAL QUE LA ORIGINAL: emerge, caza, muerde, drena y
    //     devora; solo cambió el traje.
    //
    //   v6.104 — EL TENTÁCULO LISO (la letra del usuario: «mejora aun
    //     mas las texturas de la sombra de la pagina 5, recuerda que
    //     es un tentaculo LISO, con ojos y colmillos, si lo necesitas
    //     siempre puedes crear los ojos y los colmillos con la forja
    //     o el taller»):
    //   · LA PIEL SE ALISA — LA SIERRA DORSAL MUERE: fuera los
    //     colmillos del lomo, fuera las ventosas, fuera los ridges —
    //     el cuerpo queda LISO DE RAÍZ A PUNTA, vestido con LA PIEL
    //     LISA DE LA FORJA (TentaculoLiso.png 128×512) y su BRILLO
    //     HÚMEDO: la banda especular que RESBALA por el lomo mojado
    //     (SombrasLib.MasaLisa).
    //   · LOS OJOS SON DE LA FORJA — OjoAbismo.png 64×64: la esclerótica
    //     oscura con venas carmesí, el iris radial CARMESÍ→VIOLETA y LA
    //     RENDIJA VERTICAL con su borde incandescente — once ojos
    //     graduados que GIRAN hacia la presa y PARPADEAN cada uno a su
    //     ritmo (SombrasLib.OjoTexturizado).
    //   · LOS COLMILLOS SON DE LA FORJA — ColmilloAbismo.png 48×96: la
    //     aguja curva de marfil con su encía violeta, su degradado y su
    //     punta pálida manchada de rojo (SombrasLib.ColmilloTexturizado).
    //   · LA FAUCE — dos mandíbulas de CINCO colmillos cada una que
    //     JADEAN abiertas mientras cazan, se ABREN ENORME al llegar a
    //     la presa… y TRITURAN mordiendo (cierran y mastican rápido).
    //     Detrás de los colmillos: LA GARGANTA — el vacío negro con el
    //     fondo rojo sangre latiendo.
    //   · LA CORONA — tres ojos de la forja GIGANTES sobre la fauce (el
    //     grande del centro y sus dos sacerdotes), la bendición del
    //     abismo antes de morder.
    //   · EL ANILLO DE SANGRE — la presa mordida queda marcada con el
    //     doble anillo violeta + ROJO (el sello del quinto tomo).
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

        /// <summary>LA RAÍZ de la sombra: el pecho del portador.</summary>
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

        /// <summary>EL VESTIDO COMPLETO (el cuerpo de la casa + la piel
        /// NUEVA del quinto tomo: la sierra, los ojos y la fauce).</summary>
        private void DrawTodo()
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 17 + 5;
            Player dueño = Main.player[Projectile.owner];
            NPC presa = Presa;
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];
            Vector2 raiz = RaizDeSombra(dueño);

            // 1) EL CUERPO DE LA CASA — el charco bajo el portador, la
            //    columna viva MÁS GORDA (la masa del quinto tomo es una
            //    bestia más gruesa) y la bruma en dos capas.
            //    v6.104 — EL TENTÁCULO LISO (la letra: «recuerda que es
            //    un tentaculo LISO»): la masa viste LA PIEL LISA de la
            //    forja (MasaLisa — TentaculoLiso.png + EL BRILLO HÚMEDO
            //    que resbala por el lomo); la bruma sigue siendo de la
            //    casa. LA SIERRA DORSAL MUERE con la letra nueva: el
            //    lomo queda LISO de raíz a punta (los colmillos viven
            //    SOLO en la fauce — donde muerden de verdad).
            SombrasLib.Charco(SombraProyectada(dueño), 46f, 0.55f, tiempo, semilla);
            Vector2[] columna = SombrasLib.ColumnaViva(raiz, Projectile.Center,
                tiempo, semilla, 20, 0.24f, 0.5f);
            float vive = fase == FASE_DISIPAR ? MathHelper.Clamp(1f - t / 44f, 0f, 1f) : 1f;
            SombrasLib.MasaLisa(columna, 11f, 15f, 0.96f * vive, semilla, tiempo, 52f);
            SombrasLib.BrumaColumna(columna, 48f, 0.60f * vive, tiempo, semilla, 16);
            SombrasLib.BrumaColumna(columna, 32f, 0.44f * vive, tiempo, semilla + 97, 12);

            // 3) LOS OJOS DEL ABISMO — once ojos DE LA FORJA graduados
            //    (OjoAbismo.png: la esclerótica oscura, el iris
            //    carmesí-violeta y la rendija vertical), TODOS girados
            //    hacia la presa, CADA UNO parpadeando a su propio ritmo
            //    (el parpadeo seco del que mira demasiado).
            Vector2 mirada = presa != null && presa.active
                ? presa.Center
                : dueño.MountedCenter + Projectile.velocity * 30f;
            float apertura = fase == FASE_EMERGER ? SombrasLib.DeGolpe(t / 26f) : 1f;
            OjosDelAbismo(columna, mirada, apertura * vive, semilla, tiempo);

            // 4) LA CABEZA: LA FAUCE de mandíbulas + LA CORONA de ojos.
            Vector2 cabeza = columna[^1];
            Vector2 rumboCabeza = Utils.SafeNormalize(columna[^1] - columna[Math.Max(columna.Length - 4, 0)],
                Projectile.rotation.ToRotationVector2());
            float hambrienta = AperturaDeFauce(fase, t, tiempo, presa);
            FauceYCorona(cabeza, rumboCabeza, hambrienta, vive, tiempo, semilla, mirada, fase);

            // 5) EL MORDISCO VIVO — la boca devoradora de la casa sobre
            //    la presa + el destello rojo del dren + EL ANILLO DE
            //    SANGRE (el sello del quinto tomo: violeta + rojo).
            if (fase == FASE_MORDISCO && presa != null && presa.active)
            {
                float intensidad = MathHelper.Clamp(t / 26f, 0f, 1f);
                SombrasLib.Devorador(cabeza, presa, intensidad, vive, tiempo, semilla, columna);
                VFXCore.Begin();
                VFXCore.Quad(cabeza, SombrasLib.Alfa(SombrasLib.Rojo, 0.42f * vive),
                    new Vector2(116f, 116f), rumboCabeza.ToRotation());
                VFXCore.FlushAdditive();

                // EL ANILLO DE SANGRE — la presa marcada con el doble
                // anillo (violeta + ROJO) en su propio lote inmediato.
                try
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None,
                        RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    Vector2 posPresa = presa.Center - Main.screenPosition;
                    float radioPresa = MathF.Max(presa.width, presa.height) * 0.9f + 30f;
                    VFXCore.RingQuad(posPresa, radioPresa, tiempo * 1.4f,
                        VFXCore.TintAlfa(SombrasLib.Violeta, 0.42f * vive));
                    VFXCore.RingQuad(posPresa, radioPresa * 0.7f, -tiempo * 1.9f,
                        VFXCore.TintAlfa(SombrasLib.Rojo, 0.34f * vive));
                    Main.spriteBatch.End();
                }
                catch
                {
                    VFXCore.CerrarLoteSiAbierto();
                }
                return;
            }

            // 6) LA CABEZA DE BRUMA (en emerger/caza/disipar — el aliento
            //    de la bestia alrededor de la fauce).
            float brillo = fase switch
            {
                FASE_EMERGER => 0.35f + 0.55f * SombrasLib.DeGolpe(t / 26f),
                FASE_CAZA => 0.85f + 0.15f * MathF.Sin(tiempo * 8f),
                _ => 0.5f * vive,
            };
            SombrasLib.CabezaDeBruma(cabeza, rumboCabeza, brillo * vive, vive, tiempo, semilla);
            SombrasLib.Bruma(cabeza, 42f, 0.5f * vive, tiempo, semilla + 31,
                new Vector2(MathF.Sin(tiempo * 0.9f) * 10f, -14f));
        }

        // ----------------------------------------------------------------
        //  LA PIEL NUEVA DEL QUINTO TOMO (v6.104 — EL TENTÁCULO LISO)
        // ----------------------------------------------------------------

        // (v6.104 — LOS DIENTES DEL LOMO MURIERON con la letra nueva:
        //  «recuerda que es un tentaculo LISO» — la sierra dorsal se
        //  BORRA entera y el lomo queda LISO de raíz a punta; los
        //  colmillos viven SOLO en la fauce, donde muerden de
        //  verdad. El método DientesDelLomo fue eliminado.)

        /// <summary>LOS OJOS DEL ABISMO — once ojos DE LA FORJA
        /// (OjoAbismo.png: la esclerótica oscura con venas carmesí, el
        /// iris radial CARMESÍ→VIOLETA y la RENDIJA VERTICAL con su
        /// filo incandescente) graduados en tamaño por toda la masa,
        /// TODOS girados hacia la presa y CADA UNO parpadeando a SU
        /// propio ritmo — con EL PARPADEO SECO: de vez en cuando un
        /// ojo se cierra del todo un instante mientras los demás siguen
        /// mirando (eso es lo que aterroriza).</summary>
        private static void OjosDelAbismo(Vector2[] col, Vector2 presa, float presencia,
            int semilla, float tiempo)
        {
            if (presencia <= 0.02f || col == null || col.Length < 6) return;
            VFXCore.Begin();
            int n = col.Length;
            const int OJOS = 11;
            for (int k = 0; k < OJOS; k++)
            {
                float t01 = 0.10f + 0.82f * ((k * 0.618034f + 0.13f) % 1f);   // áureo: reparto orgánico
                int idx = (int)(t01 * (n - 1));
                Vector2 perp = idx > 0 && idx < n - 1
                    ? col[idx + 1] - col[idx - 1]
                    : Vector2.UnitY;
                perp = new Vector2(-perp.Y, perp.X);
                if (perp.LengthSquared() < 0.01f) perp = Vector2.UnitY;
                perp = Vector2.Normalize(perp);
                float sesgo = (Azar(semilla + k * 31) * 2f - 1f) * 8f;   // no en línea recta: ESCALONAOS
                Vector2 pos = col[idx] + perp * sesgo;

                // EL TAMAÑO GRADUADO: unos pocos ENORMES, muchos pequeños.
                float grande = Azar(semilla + k * 7);
                float tamaño = 8f + 14f * MathF.Pow(grande, 2.2f);

                // EL PARPADEO PROPIO: cada ojo late a SU velocidad y fase;
                // y el PARPADEO SECO (la función escalón del susto).
                float vel = 1.6f + 2.4f * Azar(semilla + k * 13);
                float fase = Azar(semilla + k * 3) * MathHelper.TwoPi;
                float late = 0.86f + 0.14f * MathF.Sin(tiempo * vel + fase);
                float seco = MathF.Sin(tiempo * vel * 0.53f + fase * 2.1f);
                float abierto = presencia * late;
                if (seco < -0.94f)
                    abierto *= 0.08f;                   // EL CIERRE SECO de un solo ojo
                if (abierto <= 0.05f) continue;

                // EL HALO ROJO debajo de los grandes (la fiebre del ojo).
                if (tamaño > 15f)
                    VFXCore.Quad(pos, SombrasLib.Alfa(SombrasLib.Rojo, 0.16f * abierto),
                        new Vector2(tamaño * 2.6f, tamaño * 1.8f));

                // EL OJO DE LA FORJA (OjoAbismo.png — la rendija
                // vertical con su filo incandescente: todos te MIRAN;
                // sin textura — recarga caliente — cae al rasgado
                // procedural de siempre: la sombra JAMÁS se queda
                // ciega).
                SombrasLib.OjoTexturizado(pos, tamaño, presa - pos, abierto,
                    SombrasLib.OjoAbismo);
            }
            VFXCore.FlushAdditive();
        }

        /// <summary>LA APERTURA de la fauce según la fase: jadea al
        /// cazar (más abierta cuanta más hambre — cerca de la presa se
        /// abre ENORME) y TRITURA al morder (cierra y masca rápido).</summary>
        private static float AperturaDeFauce(byte fase, float t, float tiempo, NPC presa)
        {
            switch (fase)
            {
                case FASE_EMERGER:
                    return 0.15f + 0.45f * SombrasLib.DeGolpe(t / 26f);
                case FASE_CAZA:
                {
                    // LA HAMBRE: cerca de la presa la boca se abre ENORME.
                    float hambre = 0.42f + 0.22f * MathF.Sin(tiempo * 7f);
                    if (presa != null && presa.active)
                    {
                        float cerca = MathHelper.Clamp(
                            1f - Vector2.Distance(presa.Center, Main.LocalPlayer.Center) / 420f, 0f, 1f);
                        hambre = MathHelper.Lerp(hambre, 0.95f + 0.08f * MathF.Sin(tiempo * 15f),
                            cerca * cerca);
                    }
                    return hambre;
                }
                case FASE_MORDISCO:
                    // LA TRITURACIÓN: mandíbulas apretadas masticando rápido.
                    return 0.10f + 0.07f * MathF.Abs(MathF.Sin(tiempo * 16f));
                default:
                    return 0.30f;
            }
        }

        /// <summary>LA FAUCE Y LA CORONA — la cabeza del quinto tomo:
        /// dos mandíbulas de CINCO colmillos DE LA FORJA
        /// (ColmilloAbismo.png — la aguja curva de marfil con su encía
        /// violeta y su punta pálida manchada de rojo) que se abren
        /// según <paramref name="apertura"/> (0..1), la GARGANTA
        /// (vacío negro con fondo rojo sangre latiendo) y LA CORONA de
        /// tres ojos DE LA FORJA gigantes bendiciendo la caza.</summary>
        private static void FauceYCorona(Vector2 cabeza, Vector2 rumbo, float apertura,
            float vive, float tiempo, int semilla, Vector2 presa, byte fase)
        {
            var colmilloForja = SombrasLib.ColmilloAbismo;
            if (vive <= 0.03f || (colmilloForja == null && VFXCore.Colmillo == null)) return;
            float ang = rumbo.ToRotation();
            Vector2 perp = new(-rumbo.Y, rumbo.X);
            float abre = 0.10f + 0.62f * MathHelper.Clamp(apertura, 0f, 1f);   // rad por lado

            VFXCore.Begin();

            // LA GARGANTA — el vacío negro con el fondo ROJO SANGRE
            // latiendo detrás de los colmillos (la boca abierta al mundo).
            float late = 0.8f + 0.2f * MathF.Sin(tiempo * 5.3f);
            VFXCore.Quad(cabeza, SombrasLib.Alfa(SombrasLib.RojoGarganta, 0.55f * vive * late),
                new Vector2(64f, 64f) * late);
            VFXCore.Quad(cabeza, Color.Black * (0.72f * vive), new Vector2(46f, 46f));

            // LAS DOS MANDÍBULAS — cinco colmillos en abanico cada una:
            // la comisura atrás, abriéndose hacia delante; los del medio
            // MÁS LARGOS (la pinza que atrapa).
            for (int lado = -1; lado <= 1; lado += 2)
            {
                for (int k = 0; k < 5; k++)
                {
                    float f = k / 4f;                          // 0 = comisura, 1 = delante
                    float angCol = ang + lado * (0.10f + abre * (0.25f + 0.75f * f));
                    Vector2 nace = cabeza - rumbo * (16f * (1f - f)) + perp * lado * 7f;
                    float largo = (24f + 20f * (1f - MathF.Abs(f - 0.45f) * 2f))
                        * (0.9f + 0.2f * Azar(semilla + lado * 5 + k));
                    Vector2 rumboCol = angCol.ToRotationVector2();
                    Vector2 posCol = nace + rumboCol * largo * 0.5f;
                    // (v6.104 — EL COLMILLO DE LA FORJA: la aguja curva
                    // de marfil DETALLADA — ColmilloTexturizado respeta
                    // la convención de siempre: la punta apunta a
                    // angCol; sin textura cae al horneado de la casa)
                    SombrasLib.ColmilloTexturizado(posCol, largo, angCol,
                        0.96f * vive, colmilloForja);
                    // LA PUNTA ROJA de cada colmillo (el filo fresco).
                    VFXCore.Quad(nace + rumboCol * largo * 0.92f,
                        SombrasLib.Alfa(SombrasLib.Rojo, 0.4f * vive),
                        new Vector2(largo * 0.22f, largo * 0.26f));
                }
            }

            // LA CORONA — los tres ojos DE LA FORJA gigantes SOBRE la
            // fauce (el grande del centro y sus dos sacerdotes): la
            // bendición del abismo antes del mordisco, TODOS girados
            // hacia la presa (v6.104 — la textura OjoAbismo.png).
            float coronaAbre = fase == FASE_MORDISCO ? 1f : 0.7f + 0.3f * MathF.Sin(tiempo * 4f);
            SombrasLib.OjoTexturizado(cabeza - rumbo * 30f, 30f, presa - cabeza,
                coronaAbre * vive, SombrasLib.OjoAbismo);
            SombrasLib.OjoTexturizado(cabeza - rumbo * 22f + perp * 20f, 19f, presa - cabeza,
                coronaAbre * 0.9f * vive, SombrasLib.OjoAbismo);
            SombrasLib.OjoTexturizado(cabeza - rumbo * 22f - perp * 20f, 19f, presa - cabeza,
                coronaAbre * 0.9f * vive, SombrasLib.OjoAbismo);

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
