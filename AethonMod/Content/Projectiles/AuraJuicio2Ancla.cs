using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Buffs;
using AethonMod.Content.Dusts;
using AethonMod.Content.Effects.Bruma;
using AethonMod.Content.Globals;
using AethonMod.Content.Items.Accessories;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles
{
    // ======================================================================
    //  AuraJuicio2Ancla — v6.105 — EL ANCLA INVISIBLE DEL AURA DE
    //  JUICIO 2 (la copia silenciosa: el efecto del ESTANDARTE, el
    //  teatro SOLO en los enemigos).
    //
    //  LA LETRA DEL USUARIO: «su efecto debe ser el mismo que el de
    //  estandarte del juicio, ademas sobre el jugador no necesita
    //  haber nada, el efecto solo se ve al acercarce los enemigos con
    //  particulas mas pequeñas y con los colores de la casa, ya sabes
    //  principalmente morado y negro, ademas tambien agrega particulas
    //  de bruma en la misma direccion e intensidad variable como las
    //  particulas iniciales y sobre los enemigos agrega un efecto que
    //  sea una textura de perlin circular y que solo sea un cemirculo
    //  con un difuminado y transparencia en forma de cirulo en su
    //  parte plana».
    //
    //  EL ANCLA ES INVISIBLE — NADA se dibuja sobre el portador (ni
    //  bloom, ni luciérnagas, ni luz del campo). Lo que hace:
    //
    //   1) MARCA EL CAMPO — EL EFECTO DEL ESTANDARTE (la copia del
    //      bloque del EstandarteJuicioAuraProjectile): el radio
    //      pequeño de 300 px, LA QUEMADURA DEL VACÍO EXTERIOR con su
    //      timer de 180, LA INTENSIDAD ×1 (borde) → ×3 (pegado) más
    //      LA RAMPA del tiempo ardiendo — el dot, la lentitud y el
    //      pago del buff viven en el motor compartido
    //      (QuemaduraVacioExterior + VacioGlobalNPC).
    //
    //   2) VISTE A LOS IMPIOS (solo cuando se ACERCAN — el efecto
    //      solo existe en el enemigo):
    //      · LAS PARTÍCULAS PEQUEÑAS — chispas menudas del polvo del
    //        vacío invertido (núcleo NEGRO + halo VIOLETA — morado y
    //        negro, la casa), naciendo en el flanco que MIRA al
    //        portador y EXPULSADAS alejándose (LA MISMA DIRECCIÓN de
    //        las partículas originales del aura de juicio — pero
    //        pequeñas).
    //      · LAS PARTÍCULAS DE BRUMA — puffs de humo fBm (BrumaFX)
    //        derivando en LA MISMA DIRECCIÓN que las partículas, con
    //        INTENSIDAD VARIABLE (el alfa y el tamaño crecen con la
    //        cercanía del impío: cuanto más pegado arde, más bruma).
    //      · EL SEMICÍRCULO PERLIN — PerlinSemicirculo.png (la
    //        textura de perlin circular que SOLO es un semicírculo,
    //        con el difuminado y transparencia en forma de círculo en
    //        su parte plana): la media cúpula morada y negra sobre
    //        cada impío, INCLINADA hacia el portador, con el pase alfa
    //        (el negro-violeta que OSCURECE — se lee de día) y el
    //        pase aditivo (la energía violeta).
    // ======================================================================
    public class AuraJuicio2Ancla : ModProjectile
    {
        /// <summary>LA DISTANCIA ÍNTIMA del estandarte (a 40 px del
        /// impío, el fuego pleno).</summary>
        private const float DIST_INTIMA = 40f;

        /// <summary>Ticks que el impío sigue ARDIENDO ya fuera del
        /// campo (el timer 180 del estandarte).</summary>
        private const int ARDE_FUERA = 180;

        /// <summary>La intensidad máxima por cercanía (×3 pegado).</summary>
        private const float INTENSIDAD_CERCA = 3f;

        /// <summary>LOS IMPÍOS vestidos a la vez (el presupuesto de la
        /// casa: los más cercanos primero).</summary>
        private const int MAX_VESTIDOS = 8;

        // EL RECURSO NUEVO — el semicírculo Perlin (null-safe, el
        // patrón de la casa: Asset diferido, IsLoaded, catch).
        private static Asset<Texture2D> _perlinSemi;

        /// <summary>EL SEMICÍRCULO PERLIN (256×256 — premultiplicado).
        /// Null-safe.</summary>
        private static Texture2D PerlinSemi
        {
            get
            {
                try
                {
                    _perlinSemi ??= ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/PerlinSemicirculo");
                    return _perlinSemi.IsLoaded ? _perlinSemi.Value : null;
                }
                catch { return null; }
            }
        }

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = false;          // es un CAMPO, no un arma
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;       // el campo del portador: todos lo ven
            Projectile.aiStyle = -1;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player dueno = Main.player[Projectile.owner];

            // EL VÍNCULO — mientras el dueño viva y PORTÉ el aura 2,
            // el ancla vive; si la suelta o cae, muere.
            if (dueno == null || !dueno.active || dueno.dead ||
                !AuraDeJuicio2Item.LlevaElAura(dueno))
            {
                Projectile.Kill();
                return;
            }

            // EL ANCLA: clavada al pecho del portador, inmortal mientras
            // el vínculo viva (INVISIBLE — nada de luz, nada de dibujo).
            Projectile.Center = dueno.MountedCenter;
            Projectile.timeLeft = 2;

            // ==============================================================
            //  1) LA MARCA DEL CAMPO — EL EFECTO DEL ESTANDARTE (solo el
            //     dueño manda: la quemadura viaja dentro del propio
            //     debuff — la copia del bloque del estandarte).
            // ==============================================================
            if (Projectile.owner == Main.myPlayer && !Main.gameMenu)
            {
                int nivelDespertar = dueno.GetModPlayer<ShardPlayer>().NivelDespertar;
                int danoBase = 20 + 6 * nivelDespertar;   // HP/s a intensidad ×1

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || !n.CanBeChasedBy() || n.dontTakeDamage)
                        continue;

                    VacioGlobalNPC vacio = n.GetGlobalNPC<VacioGlobalNPC>();

                    // FUERA DEL RADIO: si arde por NOSOTROS, el fuego se
                    // enfría poco a poco (el debuff muere solo a los 180).
                    Vector2 borde = Vector2.Clamp(Projectile.Center, n.TopLeft, n.BottomRight);
                    float dist = Vector2.Distance(Projectile.Center, borde);
                    float perdon = Math.Max(n.width, n.height) * 0.5f + 40f;
                    if (dist > EstandarteJuicioAuraProjectile.RADIO + perdon)
                    {
                        if (vacio.DuenoVacio == dueno.whoAmI + 1 && vacio.IntensidadQuemadura > 0f)
                            vacio.IntensidadQuemadura *= 0.995f;
                        continue;
                    }

                    // DENTRO — LA MARCA: la Quemadura del Vacío Exterior
                    // (se refresca mientras permanezca; fuera, sus últimos
                    // 180 ticks de fuego — el timer del estandarte).
                    n.AddBuff(ModContent.BuffType<QuemaduraVacioExterior>(), ARDE_FUERA, false);

                    // LA CUENTA DEL DUEÑO (quién cobra el buff del vacío).
                    vacio.DuenoVacio = dueno.whoAmI + 1;
                    vacio.DanoVacio = danoBase;

                    // LA INTENSIDAD — la técnica Remap del estandarte:
                    // ×1 en el borde → ×3 PEGADO, más LA RAMPA DEL TIEMPO
                    // (cada segundo ardiendo lo enciende, hasta ×1.5).
                    float cercania = Remap(dist, EstandarteJuicioAuraProjectile.RADIO + perdon,
                        DIST_INTIMA, 0f, 1f);
                    float rampaTiempo = 1f + 0.5f * Math.Min(1f, vacio.VisualQuemadura);
                    vacio.IntensidadQuemadura = (1f + (INTENSIDAD_CERCA - 1f) * cercania)
                        * rampaTiempo;
                }
            }

            // ==============================================================
            //  2) LAS PARTÍCULAS PEQUEÑAS + LA BRUMA (solo cliente — el
            //     teatro de los impíos; la MISMA DIRECCIÓN que las
            //     partículas originales del aura: expulsadas del flanco
            //     que MIRA al portador, ALEJÁNDOSE de él).
            // ==============================================================
            if (Main.netMode != NetmodeID.Server && !Main.dedServ && !Main.gameMenu)
                VestirALosImpios(dueno);
        }

        // ================================================================
        //  EL TEATRO DE LOS IMPIOS — LAS PARTÍCULAS PEQUEÑAS Y LA BRUMA
        //  (fase de UPDATE: el spawn de polvos; la bruma y el semicírculo
        //  viven en el render).
        // ================================================================
        private void VestirALosImpios(Player dueno)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active)
                    continue;
                if (!VacioGlobalNPC.TieneQuemadura(n))
                    continue;
                VacioGlobalNPC vacio = n.GetGlobalNPC<VacioGlobalNPC>();
                if (vacio.DuenoVacio != dueno.whoAmI + 1)
                    continue;                              // arde por OTRO campo
                if (vacio.VisualQuemadura <= 0.05f)
                    continue;

                float vive = vacio.VisualQuemadura;
                float k = MathHelper.Clamp(vacio.CercaniaLocal, 0f, 1f);   // 0 borde → 1 pegado
                float intensidad = MathHelper.Clamp(vacio.IntensidadQuemadura, 1f, 4.5f);

                // LA DIRECCIÓN DE LA CASA — portador → impío: las chispas
                // nacen en el flanco que MIRA al portador y salen
                // EXPULSADAS alejándose (la misma dirección de las
                // partículas originales del aura de juicio).
                Vector2 hacia = n.Center - dueno.MountedCenter;
                float largo = hacia.Length();
                if (largo < 1f)
                    continue;
                Vector2 dir = hacia * (1f / largo);

                // ----------------------------------------------------------
                //  LAS PARTÍCULAS PEQUEÑAS — menudas (la letra: «deben ser
                //  mas pequeñas»), morado y negro (el polvo del vacío
                //  invertido: NÚCLEO NEGRO + halo VIOLETA), expulsadas con
                //  velocidad ∝ intensidad. Densidad ∝ cercanía: el impío
                //  pegado HIERVE de chispas menudas.
                // ----------------------------------------------------------
                int ritmo = (int)MathHelper.Clamp(5f - k * 2.5f, 1f, 5f);  // 1 de cada 5 lejos… de cada 2 pegado
                if ((Projectile.ai[0] + i) % ritmo == 0f && n.Center.DistanceSQ(dueno.Center) < 640f * 640f)
                {
                    // nacen en el flanco que MIRA al portador (los
                    // gigantes, a un cuarto de su cuerpo hacia él)
                    float mitad = MathF.Max(n.width, n.height) * 0.25f;
                    Vector2 posChispa = n.Center - dir * MathF.Min(mitad, 60f);

                    // EXPULSADAS ALEJÁNDOSE — el abanico que converge al
                    // rumbo recto (la técnica del aura original).
                    float ajuste = Main.rand.NextFloat(1.2f, 3.2f) * (0.6f + 0.4f * k);
                    Vector2 velFin = dir * ajuste;
                    Vector2 velInicio = velFin.RotatedByRandom(0.5f);

                    // EL COLOR DE LA CASA — el halo violeta: más CLARO
                    // cuanto más cerca arde (la temperatura del vacío).
                    Color halo = Color.Lerp(
                        new Color(150, 84, 235), new Color(224, 176, 255), k);
                    Dust d = Dust.NewDustPerfect(posChispa,
                        ModContent.DustType<PolvoVacioInvertido>(), velInicio,
                        0, halo, Main.rand.NextFloat(0.26f, 0.42f));   // PEQUEÑAS (la letra)
                    d.noGravity = true;
                    d.fadeIn = 0.6f;
                }
            }
            Projectile.ai[0] += 1f;
        }

        // ================================================================
        //  EL RENDER — NADA sobre el jugador: SOLO los impíos. LA BRUMA
        //  (los puffs de BrumaFX en la misma dirección e intensidad
        //  variable) y EL SEMICÍRCULO PERLIN (la media cúpula morada y
        //  negra con el difuminado circular en su parte plana).
        // ================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.gameMenu || Main.netMode == NetmodeID.Server)
                return false;

            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active)
                return false;

            try
            {
                VFXCore.CerrarLoteSiAbierto();
                float t = Main.GlobalTimeWrappedHourly;

                // LA LISTA de los impíos que arden por ESTE campo (los
                // más cercanos primero — el presupuesto de la casa).
                int vestidos = 0;
                for (int i = 0; i < Main.maxNPCs && vestidos < MAX_VESTIDOS; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active)
                        continue;
                    VacioGlobalNPC vacio;
                    try { vacio = n.GetGlobalNPC<VacioGlobalNPC>(); }
                    catch { continue; }
                    if (vacio.DuenoVacio != Projectile.owner + 1)
                        continue;
                    if (!VacioGlobalNPC.TieneQuemadura(n) || vacio.VisualQuemadura <= 0.05f)
                        continue;
                    // FUERA DE PANTALLA: nadie lo ve (el margen del
                    // semicírculo más su inclinación — 340 px de colchón)
                    Vector2 enPantalla = n.Center - Main.screenPosition;
                    if (enPantalla.X < -340f || enPantalla.X > Main.screenWidth + 340f ||
                        enPantalla.Y < -340f || enPantalla.Y > Main.screenHeight + 340f)
                        continue;

                    vestidos++;
                    VestirUnImpio(n, vacio, dueno, t);
                }
            }
            catch
            {
                try { VFXCore.CerrarLoteSiAbierto(); } catch { }
            }
            VFXCore.ReabrirLoteVanilla();
            return false;   // el ancla NUNCA se dibuja
        }

        /// <summary>EL VESTIDO DE UN IMPIO — la bruma (misma dirección,
        /// intensidad variable) y el SEMICÍRCULO PERLIN.</summary>
        private static void VestirUnImpio(NPC n, VacioGlobalNPC vacio, Player dueno, float t)
        {
            float vive = vacio.VisualQuemadura;
            float k = MathHelper.Clamp(vacio.CercaniaLocal, 0f, 1f);
            int semilla = n.whoAmI * 13 + 7;

            Vector2 hacia = n.Center - dueno.MountedCenter;
            float largo = hacia.Length();
            if (largo < 1f)
                return;
            Vector2 dir = hacia * (1f / largo);   // portador → impío

            float mitad = MathF.Max(n.width, n.height) * 0.5f;

            // ==============================================================
            //  LAS PARTÍCULAS DE BRUMA — en la MISMA DIRECCIÓN que las
            //  partículas iniciales (derivan ALEJÁNDOSE del portador,
            //  desde el flanco que lo mira) y con INTENSIDAD VARIABLE
            //  (el alfa y el radio crecen con la cercanía: cuanto más
            //  pegado arde, más bruma respira el impío).
            // ==============================================================
            Vector2 o = Main.screenPosition;
            try
            {
                BrumaFX.BeginMass();
                for (int p = 0; p < 3; p++)
                {
                    float f0 = Frac(semilla * 0.83f + p * 0.618034f);
                    float ritmo = 0.10f + 0.09f * Frac(f0 * 9.7f);
                    float edad = Frac(t * ritmo + f0 * 4.1f + p * 0.233f);   // vida cíclica

                    // NACE en el flanco que MIRA al portador y DERIVA
                    // alejándose (LA MISMA DIRECCIÓN de las chispas)…
                    Vector2 pos = n.Center - dir * (mitad * 0.4f)
                        + dir * (edad * (18f + 30f * f0))
                        + new Vector2(0f, -6f * edad);
                    float radio = (6f + 9f * f0) * (0.7f + 0.8f * edad)
                        * (0.8f + 0.5f * k);                 // …y del TAMAÑO de su cuerpo
                    float a = vive * MathF.Sin(edad * MathF.PI)
                        * (0.16f + 0.34f * k);               // INTENSIDAD VARIABLE (×k)
                    Color c = edad > 0.55f
                        ? Color.Lerp(new Color(30, 16, 50), new Color(96, 54, 150),
                            (edad - 0.55f) / 0.45f)
                        : new Color(30, 16, 50);
                    BrumaFX.Puff(pos - o, radio, c, semilla * 31 + p, t + f0 * 7f,
                        MathHelper.Clamp(a, 0.04f, 0.55f), quality: 0.5f,
                        velocity: dir * (12f + 16f * f0));
                }
            }
            catch { }
            finally
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            // ==============================================================
            //  EL SEMICÍRCULO PERLIN — la textura de perlin circular que
            //  SOLO es un semicírculo (PerlinSemicirculo.png): la media
            //  cúpula ARDE sobre el impío, INCLINADA hacia el portador
            //  (la cara que arde es la que lo mira), con el difuminado y
            //  transparencia en forma de círculo en su parte plana (la
            //  cúpula se deshace sobre el cuerpo del impío).
            // ==============================================================
            Texture2D perlin = PerlinSemi;
            if (perlin != null && !perlin.IsDisposed)
            {
                float late = 0.94f + 0.06f * MathF.Sin(t * 7f + semilla * 1.7f);

                // EL TAMAÑO — la cúpula viste al impío ENTERO y crece con
                // la cercanía (el fuego pegado lo ENVUELVE).
                float ancho = (mitad * 2f + 26f) * (0.85f + 0.45f * k) * late;
                float esc = ancho / 192f;                    // el semicírculo mide 2R=192 en la textura

                // LA INCLINACIÓN — la cúpula se echa HACIA el portador:
                // su "arriba" apunta al campo que la enciende.
                float rot = dir.ToRotation() + MathHelper.PiOver2;

                // LA BASE EN EL CUERPO — el diámetro de la textura vive a
                // 22 px bajo su centro (y=150 del lienzo de 256): el
                // origen del quad (el centro de la textura) sube para que
                // LA PARTE PLANA caiga sobre el corazón del impío.
                Vector2 baseTex = new Vector2(0f, 22f * esc).RotatedBy(rot);
                Vector2 pos = n.Center - baseTex;

                Vector2 tamPx = new Vector2(256f * esc, 256f * esc);

                Color negroVioleta = new(26, 12, 44);
                Color violeta = new(150, 84, 235);

                // (1) EL PASO ALFA — el negro-violeta que OSCURECE: la
                //     cúpula se lee TAMBIÉN DE DÍA (la lección de la
                //     .103: el aditivo se lava con el sol).
                VFXCore.Begin();
                VFXCore.Quad(pos, negroVioleta * ((0.34f + 0.22f * k) * vive),
                    tamPx, rot, perlin);
                VFXCore.FlushAlpha();

                // (2) EL PASO ADITIVO — la energía violeta que SUMA: las
                //     vetas del Perlin prenden en la oscuridad.
                VFXCore.Begin();
                VFXCore.Quad(pos, violeta * ((0.20f + 0.26f * k) * vive),
                    tamPx, rot, perlin);
                VFXCore.FlushAdditive();
            }
        }

        /// <summary>El Remap acotado del estandarte.</summary>
        private static float Remap(float v, float in1, float in2, float out1, float out2)
        {
            float kk = (v - in1) / (in2 - in1);
            if (kk < 0f) kk = 0f;
            else if (kk > 1f) kk = 1f;
            return out1 + (out2 - out1) * kk;
        }

        /// <summary>El Frac determinista de la casa.</summary>
        private static float Frac(float v)
        {
            float f = v - MathF.Floor(v);
            return f < 0f ? f + 1f : f;
        }
    }
}
