using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Weapons;

namespace AethonMod.Content.Projectiles.Nervioso
{
    /// <summary>
    /// SOMBRAPAGINACAZA — v6.50.91 — LA SOMBRA DE LA PÁGINA DEL NERVIOSO.
    /// La letra del usuario: «del libro sale La Sombra de la Pagina haciendo
    /// un 10% de daño a la criatura hasta que la mata y la absorbe, esta
    /// muerte no cuenta como que la hizo el jugador y la criatura no deja
    /// loot, pero si cuenta como exp ya que el mismo libro la mato».
    ///
    /// NACE DEL LIBRO FLOTANTE (no del jugador): la columna de sombra crece
    /// desde el grimorio que vela sobre el portador, persigue a la presa y
    /// la MUELDE — un 10% de la vida MÁXIMA por golpe (cada 30 t: diez
    /// golpes y la criatura cae). Al último golpe NO hay muerte de vanilla:
    /// el libro ABSORBE a su presa (GrimorioHambrientoNervioso.AbsorberPresa
    /// — sin loot, sin gore, sin crédito del jugador) y su hambre baja 1%.
    ///
    /// v6.50.94 — EL FESTÍN EGOÍSTA Y EL COMPÁS. La letra: «el festin
    /// compartido no tiene sentido […] el libro debe ser egoista y comer
    /// toda la criatura loot incluido por eso el libro recibe la exp y el
    /// jugador nada» — la XP de la presa es DEL NERVIOSO (AbsorberPresa),
    /// no del Grimorio del Eterno.
    ///
    /// v6.50.95 — EL COMPÁS MURIÓ. La letra del usuario: «quitar la
    /// restriccion del compás de la sombra — tus dos reglas a la vez: un
    /// ataque cada 10 s (marcado al nacer) y nunca antes de 2 s de
    /// terminar el anterior (marcado al morir). El re-lanzamiento al
    /// matar murió. — creo que mejor es dejarlo atacar cuando quiera»:
    /// SIN relojes de ninguna clase — la sombra sale CUANDO HAY PRESA
    /// que se mueva cerca (la única cola que queda es la natural: una
    /// presa por sombra — el festín del Nervioso sigue siendo un arte
    /// individual).
    ///
    /// El cuerpo es el lenguaje de la casa (SombrasLib): la columna viva de
    /// verlet, la masa negra, los ojos que miran, la bruma que come la luz
    /// y EL DEVORADOR cubriendo a la presa mientras drena — y en la
    /// absorción, las ALMAS viajando por el cuerpo hacia el libro.
    /// </summary>
    public class SombraPaginaCaza : ModProjectile
    {
        private const byte FASE_EMERGER = 0;
        private const byte FASE_CAZA = 1;
        private const byte FASE_MORDIDA = 2;
        private const byte FASE_ABSORBER = 3;
        private const byte FASE_DISIPAR = 4;

        /// <summary>EL MORDISCO: cada cuántos ticks la sombra drena su 10%.</summary>
        private const int CADENCIA_MORDIDA = 30;

        private NPC Presa => Projectile.ai[0] <= 0 ? null : Main.npc[(int)Projectile.ai[0] - 1];

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.tileCollide = false;
            // LA REGLA DEL LIBRO: el daño ES la sombra drenando su 10% —
            // cero golpes de contacto (la muerte NO es del jugador)
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3600;
            Projectile.netImportant = true;
        }

        /// <summary>LA RAÍZ ES EL LIBRO FLOTANTE: la sombra nace del grimorio
        /// que vela encima del portador (esta sombra sale a cazar POR SU
        /// CUENTA — es el apetito del libro, no el brazo del jugador).</summary>
        private Vector2 RaizDeSombra()
        {
            int tipo = ModContent.ProjectileType<GrimorioNerviosoFlotante>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.type == tipo && p.owner == Projectile.owner)
                    return p.Center;
            }
            Player dueño = Main.player[Projectile.owner];
            return dueño != null && dueño.active ? dueño.MountedCenter : Projectile.Center;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            NPC presa = Presa;
            Player dueño = Main.player[Projectile.owner];
            byte fase = (byte)Projectile.ai[1];
            bool autoridad = Main.netMode != NetmodeID.MultiplayerClient;

            bool presaValida = presa != null && presa.active && presa.life > 0;
            if (fase != FASE_DISIPAR && fase != FASE_ABSORBER && !presaValida)
            {
                Projectile.ai[1] = FASE_DISIPAR;
                Projectile.ai[2] = 0;
                Projectile.netUpdate = true;
                fase = FASE_DISIPAR;
                t = 0;
            }

            switch (fase)
            {
                case FASE_EMERGER:
                {
                    // sale DEL LIBRO hacia la presa (el capullo que despliega)
                    Vector2 raiz = RaizDeSombra();
                    Vector2 rumbo = presa != null && presaValida
                        ? (presa.Center - raiz).SafeNormalize(-Vector2.UnitY)
                        : -Vector2.UnitY;
                    float avance = SombrasLib.DeGolpe(t / 20f);
                    Projectile.Center = raiz + rumbo * (24f + 130f * avance);
                    Projectile.rotation = rumbo.ToRotation();
                    if (t == 3) Sonar(SoundID.Item122.WithPitchOffset(-0.45f).WithVolumeScale(0.6f), Projectile.Center);
                    if (t >= 20) { Projectile.ai[1] = FASE_CAZA; Projectile.ai[2] = 0; Projectile.netUpdate = true; }
                    break;
                }
                case FASE_CAZA:
                {
                    if (presa != null && presaValida)
                    {
                        Vector2 hacia = (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                        float dist = Vector2.Distance(presa.Center, Projectile.Center);
                        float vel = MathHelper.Clamp(dist * 0.05f, 20f, 46f);
                        float actual = Projectile.velocity.ToRotation();
                        float deseado = hacia.ToRotation();
                        float giro = MathHelper.WrapAngle(deseado - actual);
                        Projectile.velocity = (actual + MathHelper.Clamp(giro, -0.26f, 0.26f)).ToRotationVector2() * vel;
                        Projectile.rotation = deseado;

                        // la presa se alejó demasiado del libro: soltarla
                        // (la caza vigila ALREDEDOR del grimorio, no del mapa)
                        if (Vector2.DistanceSquared(presa.Center, RaizDeSombra()) > 1400f * 1400f)
                        {
                            Projectile.ai[1] = FASE_DISIPAR;
                            Projectile.ai[2] = 0;
                            Projectile.netUpdate = true;
                            break;
                        }

                        if (Projectile.Hitbox.Intersects(presa.Hitbox))
                        {
                            Projectile.ai[1] = FASE_MORDIDA;
                            Projectile.ai[2] = 0;
                            Projectile.netUpdate = true;
                            Sonar(SoundID.NPCHit9.WithPitchOffset(-0.35f).WithVolumeScale(0.9f), Projectile.Center);
                        }
                    }
                    break;
                }
                case FASE_MORDIDA:
                {
                    if (presa != null && presa.active && presa.life > 0)
                    {
                        Projectile.Center = presa.Center;
                        Projectile.rotation = (presa.Center - RaizDeSombra()).ToRotation();

                        // EL DRENAJE (sólo la AUTORIDAD): 10% de la vida
                        // MÁXIMA por mordida — la vida jamás llega a 0 por
                        // esta vía: el último golpe es LA ABSORCIÓN
                        if (autoridad && t > 18 && t % CADENCIA_MORDIDA == 0)
                        {
                            int golpe = Math.Max(1, (int)Math.Ceiling(presa.lifeMax * 0.10f));
                            if (presa.life - golpe <= 0)
                            {
                                // LA ABSORCIÓN — el libro se la come entera:
                                // sin loot, sin gore, sin crédito; XP sí; −1%
                                GrimorioHambrientoNervioso.AbsorberPresa(presa, dueño);
                                Projectile.ai[1] = FASE_ABSORBER;
                                Projectile.ai[2] = 0;
                                Projectile.netUpdate = true;
                                Sonar(SoundID.Item122.WithPitchOffset(0.25f).WithVolumeScale(0.5f), Projectile.Center);
                            }
                            else
                            {
                                presa.life -= golpe;
                                presa.netUpdate = true;
                                if (t % 60 == 0)
                                    Sonar(SoundID.NPCHit9.WithPitchOffset(-0.1f).WithVolumeScale(0.45f), Projectile.Center);
                            }
                        }
                    }
                    else
                    {
                        Projectile.ai[1] = FASE_DISIPAR;
                        Projectile.ai[2] = 0;
                        Projectile.netUpdate = true;
                    }
                    break;
                }
                case FASE_ABSORBER:
                {
                    // lo que era la presa VIAJA al libro (las almas del draw);
                    // aquí sólo decaer suave mientras suben
                    Projectile.velocity *= 0.90f;
                    if (t >= 40) { Projectile.ai[1] = FASE_DISIPAR; Projectile.ai[2] = 0; }
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

            // polvillo violeta del vuelo
            if (Main.netMode != NetmodeID.Server && Projectile.ai[1] <= FASE_CAZA && Main.rand.NextBool(6))
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(24, 24), 48, 48,
                    DustID.Shadowflame, 0f, -0.3f, 128, default, 0.55f);
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

        private void DrawTodo()
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 17 + 5;
            Player dueño = Main.player[Projectile.owner];
            NPC presa = Presa;
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];

            Vector2 raiz = RaizDeSombra();

            // === LA COLUMNA VIVA (más delgada que la del arma: esta página
            //     es un hilillo de hambre, no un garrote) ===
            Vector2[] col = SombrasLib.ColumnaViva(raiz, Projectile.Center, tiempo, semilla, 18, 0.24f, gancho: 0.5f);
            float disipa = fase == FASE_DISIPAR ? MathHelper.Clamp(1f - t / 44f, 0f, 1f)
                : fase == FASE_ABSORBER ? MathHelper.Clamp(1f - t / 40f, 0f, 1f) : 1f;
            SombrasLib.Masa(col, 7f, 11f, 0.92f * disipa, semilla, tiempo, grosorCentro: 34f);

            // === LA BRUMA NEGRA del cuerpo ===
            SombrasLib.BrumaColumna(col, 40f, 0.50f * disipa, tiempo, semilla, 12);
            SombrasLib.BrumaColumna(col, 26f, 0.38f * disipa, tiempo, semilla + 97, 8);

            // === LOS OJOS — la página es TODA ojos, y miran a la presa ===
            Vector2 objetivo = presa != null && presa.active
                ? presa.Center
                : dueño.MountedCenter + Projectile.velocity * 30f;
            float presencia = fase == FASE_EMERGER ? SombrasLib.DeGolpe(t / 20f) : 1f;
            SombrasLib.OjosDeMasa(col, objetivo, presencia * disipa, semilla, tiempo, 5);

            // === LA PUNTA ===
            Vector2 punta = col[col.Length - 1];
            Vector2 rumbo = (col[col.Length - 1] - col[Math.Max(col.Length - 4, 0)])
                .SafeNormalize(Projectile.rotation.ToRotationVector2());

            if (fase == FASE_MORDIDA && presa != null && presa.active)
            {
                // EL DEVORADOR — la masa que CUBRE a la presa y TRAGA; la
                // intensidad es lo COMIDO (0→1 con la vida que falta)
                float comido = 1f - MathHelper.Clamp(presa.life / (float)Math.Max(1, presa.lifeMax), 0f, 1f);
                SombrasLib.Devorador(punta, presa, comido, disipa, tiempo, semilla, col);

                // LA HERIDA: donde la página muerde, brilla rojo (la digestión)
                VFXCore.Begin();
                VFXCore.Quad(punta, SombrasLib.Alfa(SombrasLib.Rojo, 0.42f * disipa),
                    new Vector2(110f, 110f), rumbo.ToRotation());
                VFXCore.FlushAdditive();
            }
            else if (fase == FASE_ABSORBER)
            {
                // LA ABSORCIÓN: lo que era la presa SUBE por el cuerpo de la
                // sombra hacia el libro — almas que se desvanecen al llegar
                for (int i = 0; i < 6; i++)
                {
                    float prog = MathHelper.Clamp(t / 40f - i * 0.08f, 0f, 1f);
                    if (prog <= 0f || prog >= 1f) continue;
                    float ease = prog * prog * (3f - 2f * prog);
                    Vector2 pos = Vector2.Lerp(Projectile.Center, raiz, ease)
                        + new Vector2(MathF.Sin(tiempo * 6f + i * 2.4f) * 14f * (1f - ease),
                                      MathF.Cos(tiempo * 5f + i * 1.7f) * 10f * (1f - ease));
                    SombrasLib.Alma(pos, 26f - 10f * ease, (1f - ease) * disipa);
                }
                // el último suspiro de bruma donde estaba la boca
                SombrasLib.Bruma(Projectile.Center, 44f, 0.55f * disipa, tiempo, semilla + 31,
                    new Vector2(0f, -18f));
            }
            else
            {
                // LA CABEZA DE BRUMA — la punta disuelta en humo hacia la presa
                float vigor = fase switch
                {
                    FASE_EMERGER => 0.35f + 0.55f * SombrasLib.DeGolpe(t / 20f),
                    FASE_CAZA => 0.85f + 0.15f * MathF.Sin(tiempo * 8f),
                    _ => 0.5f * disipa,
                };
                SombrasLib.CabezaDeBruma(punta, rumbo, vigor * disipa, disipa, tiempo, semilla);
                SombrasLib.Bruma(punta, 36f, 0.5f * disipa, tiempo, semilla + 31,
                    new Vector2(MathF.Sin(tiempo * 0.9f) * 10f, -14f));
            }
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
