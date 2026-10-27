using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Definitivas
{
    /// <summary>
    /// PRISMAPRIMORDIALPROJECTILE — v6.54.7 — ARMA 1 DE LAS CINCO: EL
    /// PRISMA PRIMORDIAL — EL ATAQUE DEFINITIVO DEL MOD.
    ///
    /// LA TRANSCENDENCIA (la letra del usuario: «siento que a las armas
    /// les faltan más capas de efectos visuales… el prisma primordial
    /// deja mucho que desear, no le llega ni a los pies a los ataques
    /// como el rayo de fusión… tienes que mejorar aún más los efectos
    /// visuales, hacerlos más impresionantes… una investigación a los
    /// rayos destructores de otros mods y aprende nuevas técnicas»):
    /// el shader del rayo de fusión original fue DISECCIONADO en su
    /// fuente (sus 4 texturas multiplicadas: cuerpo fBm · segundo ruido
    /// · glow · bits de chispas, con el fade de lente, el umbral de
    /// núcleo blanco y el tambaleo transversal) y RECONSTRUIDO en CPU
    /// con la pila de seis pasadas texturizadas — MÁS las técnicas
    /// nuevas de la investigación de los rayos destructores de los mods
    /// populares (Task 78-a: 15 técnicas catalogadas, las 10 que faltan
    /// aplicadas aquí):
    ///
    ///   CAPA  1 — EL VELO PRISMÁTICO: los bordes de la pantalla
    ///   respiran teñidos (más denso en el encendido).
    ///   CAPA  2 — LAS POSIMÁGENES DEL BARRIDO (nueva, técnica nº2):
    ///   las últimas TRES direcciones del haz redibujadas como ecos
    ///   tenues — el barrido deja ESTELA, se lee VELOZ y masivo.
    ///   CAPA  3 — EL TELÉGRAFO DE LA CARGA (nueva, técnica nº15): la
    ///   aguja uniforme que LATA acelerando + los corredores que
    ///   recorren el canal antes del encendido.
    ///   CAPA  4 — EL SÁNDWICH DEL FOCO + EL IRIS (nueva, técnica nº10):
    ///   los CUATRO HALOS APILADOS + las pestañas + el aro arcoíris + la
    ///   pupila + DOS AROS GIRATORIOS EN CONTRA que SE ABREN con la
    ///   carga — la máquina que se abre antes de disparar.
    ///   CAPA  5 — LOS SIETE HACES DEL ESPECTRO: ahora TEXTURIZADOS (dos
    ///   pasadas cada uno: aura de SU color + núcleo en banda) — la
    ///   carne fluye dentro de cada haz de color.
    ///   CAPA  6 — LA ESPINA VIVA (nueva, técnica nº3 — verlet): el haz
    ///   central YA NO es una línea recta: 16 nodos con resorte que se
    ///   REZAGAN y LATIGUEAN al barrer el ratón — vivo, no rígido.
    ///   CAPA  7 — EL HAZ DE FUSIÓN TEXTURIZADO: LA PILA DE SEIS
    ///   PASADAS (DefinitivasLib.HazFusionEspina) — aura lenta · cuerpo
    ///   con EL COLOR QUE VIAJA y el FADE DE LENTE · remolino de segundo
    ///   ruido a 1.9× (la carne que HIERVE) · núcleo en banda · la vena
    ///   blanca · la ABERRACIÓN CROMÁTICA de bordes (nueva, técnica nº8).
    ///   CAPA  8 — EL SOBREPASO DEL ENCENDIDO (nueva, técnica nº9): al
    ///   cruzar la convergencia el haz SE PASA (×1.14) y vuelve con el
    ///   muelle — el «chomp» elástico que vende el disparo.
    ///   CAPA  9 — EL FLICKER INCONMENSURABLE (nueva, técnica nº11): dos
    ///   senos de 8.7 y 23.3 Hz que jamás se repiten — nunca late igual.
    ///   CAPA 10 — LOS ARCOS QUE TREPAN (nueva, técnica nº7): relámpagos
    ///   finos que nacen en puntos del canal y SE ESCAPAN por la
    ///   perpendicular — la energía que no cabe.
    ///   CAPA 11 — LOS TRES PULSOS QUE VIAJAN: bandas de luz CORRIENDO
    ///   por el canal (ahora texturizadas, más vivas).
    ///   CAPA 12 — LA LLUVIA CROMÁTICA: 12 polvos/frame con el offset de
    ///   color por distancia (la receta textual del original) + LAS
    ///   CHISPAS DE BORDE (nueva — los «bits» del shader: motas que
    ///   arden en el filo del canal) + el temblor CADA 4 TICKS (el
    ///   PunchCamera del original) + la luz que baña el canal.
    ///   CAPA 13 — LA ZONA DE IMPACTO (nueva, técnica nº5): la punta del
    ///   haz ya no se apaga — ARDE: nova pulsando a 12 Hz, aro de
    ///   choque, chispas con gravedad, luz y los PINCHOS DE DIFRACCIÓN
    ///   (nueva, técnica nº6 — el destello anamórfico alineado al haz).
    ///   CAPA 14 — EL RESPLENDOR DE LA PUNTA: los halos heredados de la
    ///   .54.6 fundidos con la zona de impacto.
    ///
    /// EL BUG DE LA .54.4 CURADO (v6.54.6): la sentencia vive su ciclo
    /// acotado (70 + 110 + 12 t) y MUERE — y el failsafe global (1200 t)
    /// protege cualquier futuro olvido. ai[1] = tick de vida.
    /// </summary>
    public class PrismaPrimordialProjectile : ModProjectile
    {
        private const int NHaz = 7;
        private const float LargoHaz = 2400f;

        // LA VIDA DEL MODO SENTENCIA (acotada: 70 de arranque + 110 de
        // veredicto sostenido + 12 de apagado — NUNCA más).
        private const int TSentenciaTotal = 192;

        // LA ESPINA VIVA: 16 nodos verlet (el haz que se rezaga y latiguea).
        private const int NEspina = 16;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private int _semilla;
        private bool _muriendo;       // la muerte suave (arranca y ya no se retracta)
        private int _tMuerte;

        // ==================== EL ESTADO NUEVO (v6.54.7) ====================
        private Vector2[] _espina = new Vector2[NEspina];       // LA ESPINA VIVA (mundo)
        private Vector2[] _espinaPrev = new Vector2[NEspina];   // el frame anterior (verlet)
        private bool _espinaLista;

        private readonly float[] _ecoRot = new float[4];        // LAS POSIMÁGENES: últimas direcciones
        private readonly float[] _ecoConv = new float[4];
        private int _tEco;

        private int _tEncendido = -1;   // EL SOBREPASO (el chomp elástico del arranque)
        private float _convPrev;         // el detector del cruce de encendido
        private int _tArco;              // LOS ARCOS QUE TREPAN
        private int _tChispaBorde;       // LAS CHISPAS DE BORDE (los «bits» del shader)

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Projectile.ai[1]++;
            float t = Projectile.ai[1];
            Player dueño = Main.player[Projectile.owner];
            float tiempo = Main.GlobalTimeWrappedHourly;
            if (_semilla == 0) _semilla = (int)Main.GameUpdateCount;

            // v6.54.6 — EL FAILSAFE GLOBAL: ningún prisma vive más de 1200 t
            // (la lección del haz eterno de la .54.4).
            if (t > 1200f) { Projectile.Kill(); return; }

            // EL CANAL vs LA SENTENCIA:
            //  · canalizado: suelto el clic → apagado suave.
            //  · sentencia (ai[0]=1, lo dicta El Testamento): vive su ciclo
            //    COMPLETO y acotado (TSentenciaTotal) — JAMÁS eterno.
            bool sentencia = Projectile.ai[0] >= 1f;
            if (sentencia)
            {
                if (t > TSentenciaTotal) _muriendo = true;
            }
            else
            {
                bool canalVivo = dueño.active && !dueño.dead && dueño.channel &&
                    dueño.HeldItem != null && dueño.HeldItem.type == ModContent.ItemType<Content.Weapons.Definitivas.PrismaPrimordial>();
                if (!canalVivo) _muriendo = true;
            }

            // LA MUERTE SUAVE: 12 t de apagado
            if (_muriendo)
            {
                _tMuerte++;
                if (_tMuerte > 12f) { Projectile.Kill(); return; }
            }

            // === LA GEOMETRÍA DEL PRISMA (flota al pecho del portador) ===
            Vector2 mira = (Main.MouseWorld - dueño.MountedCenter).SafeNormalize(Vector2.UnitX);
            Projectile.Center = dueño.MountedCenter + mira * 26f + new Vector2(0f, -8f);
            Projectile.rotation = mira.ToRotation();

            // LA CONVERGENCIA: canal 150 t · sentencia 70 t (la palabra ya
            // viene con toda la fe puesta)
            float calor = MathHelper.Clamp(t / (sentencia ? 70f : 150f), 0f, 1f);
            float apagado = _muriendo ? MathHelper.Clamp(1f - _tMuerte / 12f, 0f, 1f) : 1f;
            Projectile.localAI[1] = MathHelper.Lerp(Projectile.localAI[1], calor * apagado, 0.12f);
            float conv = Projectile.localAI[1];

            // EL DAÑO Y EL ANCHO ESCALAN con la convergencia (el veredicto crece)
            Projectile.damage = (int)(Projectile.originalDamage * (0.45f + 0.85f * conv));

            // ==================== CAPA 8 — EL SOBREPASO DEL ENCENDIDO ====================
            // (el chomp elástico: al cruzar la media carga el haz SE PASA y
            // vuelve muelleando — la técnica nº9 de la investigación)
            if (_convPrev < 0.45f && conv >= 0.45f && !_muriendo)
            {
                _tEncendido = 0;
                DefinitivasLib.Sonar(SoundID.Item122.WithPitchOffset(0.5f).WithVolumeScale(0.9f), Projectile.Center);
                OndaLib.Flash(new Color(255, 248, 235), 0.22f, 10, Projectile.Center);
                OndaLib.Kick(10, 14);
            }
            _convPrev = conv;
            if (_tEncendido >= 0) { _tEncendido++; if (_tEncendido > 12) _tEncendido = -1; }

            // ==================== CAPA 6 — LA ESPINA VIVA (verlet) ====================
            // (técnica nº3: 16 nodos con resorte hacia su reposo en la recta;
            // el nodo 0 PINZADO al foco — al barrer el ratón el haz SE REZAGA
            // y LATIGUEA: la diferencia entre un láser muerto y un látigo vivo)
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Vector2 foco = PosFoco(conv);
            if (!Main.dedServ && Main.netMode != NetmodeID.Server)
            {
                if (!_espinaLista || conv < 0.05f)
                {
                    for (int i = 0; i < NEspina; i++)
                    {
                        _espina[i] = foco + dir * (LargoHaz * i / (NEspina - 1f));
                        _espinaPrev[i] = _espina[i];
                    }
                    _espinaLista = true;
                }
                else
                {
                    for (int i = 0; i < NEspina; i++)
                    {
                        Vector2 reposo = foco + dir * (LargoHaz * i / (NEspina - 1f));
                        if (i == 0) { _espina[0] = reposo; _espinaPrev[0] = reposo; continue; }
                        Vector2 vel = (_espina[i] - _espinaPrev[i]) * 0.86f;              // damping
                        _espinaPrev[i] = _espina[i];
                        _espina[i] += vel;
                        _espina[i] += (reposo - _espina[i]) * (0.085f + 0.065f * conv);   // stiffness
                    }
                }

                // LAS POSIMÁGENES: cada 3 t la dirección actual entra al anillo
                // (las 3 anteriores se dibujan como ecos del barrido).
                if (++_tEco >= 3)
                {
                    _tEco = 0;
                    for (int i = 3; i > 0; i--) { _ecoRot[i] = _ecoRot[i - 1]; _ecoConv[i] = _ecoConv[i - 1]; }
                    _ecoRot[0] = Projectile.rotation;
                    _ecoConv[0] = conv;
                }

                // ==================== CAPA 10 — LOS ARCOS QUE TREPAN ====================
                // (técnica nº7: relámpagos finos que nacen EN el canal y se
                // escapan por la perpendicular — la energía que no cabe dentro)
                if (conv > 0.55f && ++_tArco > 9)
                {
                    _tArco = 0;
                    float prog = Main.rand.NextFloat(0.15f, 0.85f);
                    Vector2 p = foco + dir * (LargoHaz * prog);
                    float lado = Main.rand.NextBool() ? 1f : -1f;
                    Vector2 destino = p + new Vector2(-dir.Y, dir.X) * lado *
                        Main.rand.NextFloat(34f, 95f) * (0.5f + prog);
                    RayoSistema.Lanzar(RayoParams.Arco(p, destino),
                        (uint)Main.rand.Next(1, int.MaxValue), destino, p - destino,
                        DefinitivasLib.GradienteJuicio(Main.rand.NextFloat(1f)),
                        DefinitivasLib.BlancoCalido, Main.rand.NextFloat(4f, 7f),
                        Main.rand.Next(5, 9), AnimRayo.Arco, false);
                }
            }

            // LOS SONIDOS: el arrancar, el zumbar, y el AIRE que se llena
            if (t == 2) DefinitivasLib.Sonar(SoundID.Item13.WithPitchOffset(0.0f).WithVolumeScale(0.6f), Projectile.Center);
            if (t == 90 && conv > 0.5f) DefinitivasLib.Sonar(SoundID.Item122.WithPitchOffset(0.4f).WithVolumeScale(0.5f), Projectile.Center);
            if (sentencia && t == TSentenciaTotal - 12)
                DefinitivasLib.Sonar(SoundID.Item13.WithPitchOffset(-0.4f).WithVolumeScale(0.7f), Projectile.Center);

            // ==================== CAPA 12 — LA LLUVIA CROMÁTICA ====================
            // (la receta TEXTUAL del original: polvos por todo el canal con su
            // color desfasado por la distancia — el flujo que en el shader
            // original hacen las 4 texturas, aquí es polvo vivo)
            if (!Main.dedServ && Main.netMode != NetmodeID.Server && conv > 0.15f)
            {
                int cuantos = conv > 0.6f ? 12 : 6;
                for (int i = 0; i < cuantos; i++)
                {
                    float prog = Main.rand.NextFloat(1f);
                    float dist = LargoHaz * prog;
                    // EL SEMIANCHO del haz en ese punto (la fórmula): el polvo
                    // cae DENTRO del canal, no en cualquier lado
                    float semi = AnchoHazEn(prog, conv) * 0.5f;
                    float perp = Main.rand.NextFloat(-1f, 1f) * semi;
                    Vector2 pos = foco + dir * dist + new Vector2(-dir.Y, dir.X) * perp;
                    Color c = DefinitivasLib.GradienteJuicio(tiempo * 0.10f - prog * 0.16f);
                    Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame,
                        dir.RotatedByRandom(0.4f) * Main.rand.NextFloat(1.5f, 6f) * conv, 0, c, 1.0f);
                    d.noGravity = true;
                }

                // LAS CHISPAS DEL FOCO convergiendo (el cruce de los siete)
                if (conv > 0.35f && Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(foco, DustID.GoldFlame,
                        new Vector2(-dir.Y, dir.X).RotatedByRandom(0.7f) * Main.rand.NextFloat(1f, 5f), 0,
                        DefinitivasLib.GradienteJuicio(Main.rand.NextFloat(1f)), 0.9f);
                    d.noGravity = true;
                }

                // LAS CHISPAS DE BORDE — los «bits» del shader original
                // (técnica heredada del fuente: las motas que arden en el
                // FILO del canal, donde el fade de lente las deja vivir)
                if (conv > 0.5f && ++_tChispaBorde > 2)
                {
                    _tChispaBorde = 0;
                    float prog = Main.rand.NextFloat(0.1f, 0.9f);
                    float semi = AnchoHazEn(prog, conv) * 0.5f;
                    float lado = Main.rand.NextBool() ? 1f : -1f;
                    Vector2 posB = foco + dir * (LargoHaz * prog) + new Vector2(-dir.Y, dir.X) * lado * semi * 0.92f;
                    Dust d = Dust.NewDustPerfect(posB, DustID.GoldFlame,
                        new Vector2(-dir.Y, dir.X) * lado * Main.rand.NextFloat(1f, 3f) + dir * Main.rand.NextFloat(2f), 0,
                        DefinitivasLib.BlancoCalido, 0.7f);
                    d.noGravity = true;
                }

                // LA LUZ QUE BAÑA EL CANAL (tres puntos del haz + LA PUNTA)
                Lighting.AddLight(foco, 1.8f * conv, 1.6f * conv, 1.2f * conv);
                Lighting.AddLight(foco + dir * 800f, 1.4f * conv, 1.2f * conv, 1.0f * conv);
                Lighting.AddLight(foco + dir * 1600f, 1.0f * conv, 0.9f * conv, 0.8f * conv);

                // ==================== EL TEMBLOR CADA 4 TICKS ====================
                // (el PunchCamera del original: shakeStrength·8 cada 4 t —
                // aquí el Kick de la casa con la misma cadencia)
                if (conv > 0.7f && t % 4f == 0f)
                    OndaLib.Kick(4 + (int)(conv * 4f), 8);

                // ==================== CAPA 13 — LA ZONA DE IMPACTO ====================
                // (técnica nº5: la punta ARDE — chispas con gravedad que
                // rebotan en la perpendicular, como el estallido continuo del
                // GammaRay de los jefes de Calamity)
                Vector2 punta = _espinaLista ? _espina[NEspina - 1] : foco + dir * LargoHaz;
                int chispas = conv > 0.6f ? 3 : 1;
                for (int i = 0; i < chispas; i++)
                {
                    Dust d = Dust.NewDustPerfect(punta + new Vector2(Main.rand.NextFloat(-36f, 36f), Main.rand.NextFloat(-26f, 26f)),
                        DustID.FireworkFountain_Yellow,
                        new Vector2(dir.X * Main.rand.NextFloat(2f, 8f) + Main.rand.NextFloat(-4f, 4f),
                                    dir.Y * Main.rand.NextFloat(2f, 8f) - 2f), 0,
                        DefinitivasLib.Ambar, 1.1f);
                    d.noGravity = false;          // la gravedad de verdad las trae de vuelta
                }
                Lighting.AddLight(punta, 1.5f * conv, 1.3f * conv, 1.0f * conv);
            }

            if (!_muriendo)
                Projectile.timeLeft = Math.Max(Projectile.timeLeft, 30);
        }

        /// <summary>EL FOCO: donde convergen los siete haces (más cerca = más convergencia).</summary>
        private Vector2 PosFoco(float conv)
            => Projectile.Center + Projectile.rotation.ToRotationVector2() * MathHelper.Lerp(190f, 46f, conv);

        /// <summary>
        /// EL ANCHO DEL HAZ en la fracción p (0..1) — LA FÓRMULA EXACTA DEL
        /// RAYO DE FUSIÓN (DefinitivasLib.AnchoCono), escalada por la
        /// convergencia + LA RESPIRACIÓN ±8 % + EL FLICKER INCONMENSURABLE
        /// (v6.54.7: dos senos de 8.7/23.3 Hz que jamás se repiten) + EL
        /// SOBREPASO DEL ENCENDIDO (el chomp elástico de la técnica nº9).
        /// </summary>
        private float AnchoHazEn(float p, float conv)
        {
            float norma = DefinitivasLib.AnchoCono(p, 0f, 1f);       // sin respiración (va aparte)
            float respira = 1f + 0.08f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3f - p * 2f);
            float flicker = DefinitivasLib.Flicker(Main.GlobalTimeWrappedHourly);
            float sobrepaso = 1f;
            if (_tEncendido >= 0)
            {
                float tau = _tEncendido / 12f;
                sobrepaso = 1f + 0.14f * MathF.Sin(tau * MathHelper.Pi) * (1f - tau);
            }
            return MathF.Max(norma, 0.04f) * (34f + 118f * conv) * respira * flicker * sobrepaso;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float conv = Projectile.localAI[1];
            if (conv <= 0.08f) return false;
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Vector2 foco = PosFoco(conv);
            Vector2 fin = foco + dir * LargoHaz;

            // EL CHEQUEO DE SEGMENTO GORDO con el ancho PROMEDIO del perfil
            // (fino en el foco, gordo al fondo — como el cono del original)
            float anchoMedio = (AnchoHazEn(0.15f, conv) + AnchoHazEn(0.85f, conv)) * 0.5f;
            return SegmentoGordo(targetHitbox, foco, fin, anchoMedio);
        }

        /// <summary>El chequeo de segmento con grosor (la distancia punto-segmento contra el semiancho).</summary>
        internal static bool SegmentoGordo(Rectangle hitbox, Vector2 a, Vector2 b, float anchoTotal)
        {
            Vector2 c = hitbox.Center();
            Vector2 ab = b - a;
            float len2 = ab.LengthSquared();
            float u = len2 < 1f ? 0f : MathHelper.Clamp(Vector2.Dot(c - a, ab) / len2, 0f, 1f);
            Vector2 punto = a + ab * u;
            float dist = Vector2.Distance(c, punto);
            return dist <= anchoTotal * 0.5f + MathF.Max(hitbox.Width, hitbox.Height) * 0.5f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            float t = Projectile.ai[1];
            float tiempo = Main.GlobalTimeWrappedHourly;
            float conv = Projectile.localAI[1];

            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Vector2 perp = new(-dir.Y, dir.X);
            Vector2 foco = PosFoco(conv);
            float ang = Projectile.rotation;

            // ==================== CAPA 1 — EL VELO PRISMÁTICO ====================
            VeloPrismatico(0.10f + 0.22f * conv + (_tEncendido >= 0 ? 0.10f : 0f));

            // ==================== CAPA 2 — LAS POSIMÁGENES DEL BARRIDO ====================
            // (técnica nº2: las últimas direcciones redibujadas a 22/14/6 % —
            // el motion blur del barrido; se dibujan DETRÁS del haz de verdad)
            for (int e = 1; e < 4; e++)
            {
                if (_ecoConv[e] < 0.25f) continue;
                float alfaE = 0.24f * (1f - e * 0.34f) * conv;
                DibujarEco(_ecoRot[e], _ecoConv[e], alfaE, tiempo);
            }

            // ==================== CAPA 3 — EL TELÉGRAFO DE LA CARGA ====================
            if (conv > 0.03f && conv < 0.40f) TelegrafoDeCarga(foco, dir, conv, tiempo);

            // ==================== LA JOYA (el corazón que gira) ====================
            VFXCore.Begin();
            for (int i = 0; i < 3; i++)
            {
                float giro = tiempo * (0.7f + i * 0.2f) + i * 2.09f;
                Color cPrisma = DefinitivasLib.GradienteJuicio(tiempo * 0.09f + i * 0.33f);
                VFXCore.Quad(Projectile.Center + perp * MathF.Sin(giro) * 6f,
                    DefinitivasLib.Alfa(cPrisma, 0.30f * conv), new Vector2(64f, 64f), giro);
            }
            VFXCore.Quad(Projectile.Center, DefinitivasLib.Alfa(Color.White, 0.55f * conv), new Vector2(38f, 38f));
            VFXCore.FlushAdditive();

            if (conv > 0.02f)
            {
                // ============ CAPA 4 — EL SÁNDWICH DEL FOCO + EL IRIS ============
                // (los CUATRO HALOS APILADOS del rayo de fusión: 14× · 5× · 4× · 2×)
                Color grad = DefinitivasLib.GradienteJuicio(tiempo * 0.08f);
                Color grad2 = DefinitivasLib.GradienteJuicio(tiempo * 0.08f + 0.33f);
                VFXCore.Begin();
                VFXCore.Quad(foco, DefinitivasLib.Alfa(grad, 0.10f * conv), new Vector2(1500f, 1500f) * conv);
                VFXCore.Quad(foco, DefinitivasLib.Alfa(grad2, 0.20f * conv), new Vector2(560f, 560f) * conv);
                VFXCore.Quad(foco, DefinitivasLib.Alfa(grad, 0.42f * conv), new Vector2(430f, 430f) * conv);
                VFXCore.Quad(foco, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.50f * conv), new Vector2(215f, 215f) * conv);
                // LAS PESTAÑAS (la estrella de cuatro puntas que gira lenta)
                if (DefinitivasLib.TexStar != null)
                    VFXCore.Quad(foco, DefinitivasLib.Alfa(Color.White, 0.5f * conv),
                        new Vector2(340f, 340f) * conv, tiempo * 0.4f, DefinitivasLib.TexStar);
                // EL ARO ARCOÍRIS (la corona del foco — el espectro entero girando)
                if (VFXCore.Arcoiris != null)
                    VFXCore.Quad(foco, DefinitivasLib.Alfa(Color.White, 0.55f * conv),
                        new Vector2(300f, 300f) * conv, tiempo * 0.22f, VFXCore.Arcoiris);
                // LA PUPILA EN ANILLO
                if (VFXCore.Ring != null)
                    VFXCore.Quad(foco, DefinitivasLib.Alfa(grad2, 0.85f * conv),
                        new Vector2(120f, 120f) * (0.8f + 0.2f * conv), tiempo * 0.9f, VFXCore.Ring);

                // EL IRIS — LA MÁQUINA QUE SE ABRE (técnica nº10): dos aros
                // girando EN CONTRA que SE DILATAN con la carga (la apertura
                // de la boca del cañón: escala crece, alfa baja — al completar
                // la carga están ABIERTAS y solo queda el fuego)
                if (VFXCore.Ring != null)
                {
                    float abre = MathHelper.Clamp((conv - 0.15f) / 0.85f, 0f, 1f);
                    VFXCore.Quad(foco, DefinitivasLib.Alfa(grad2, 0.55f * conv * (1f - abre * 0.5f)),
                        new Vector2(150f, 150f) * (0.55f + 1.1f * abre), -tiempo * 0.9f, VFXCore.Ring);
                    VFXCore.Quad(foco, DefinitivasLib.Alfa(grad, 0.45f * conv * (1f - abre * 0.5f)),
                        new Vector2(150f, 150f) * (0.4f + 0.85f * abre), tiempo * 1.4f, VFXCore.Ring);
                }
                VFXCore.Quad(foco, DefinitivasLib.Alfa(Color.White, 0.95f * conv), new Vector2(52f, 52f) * conv);
                VFXCore.FlushAdditive();
            }

            // ==================== CAPA 5 — LOS SIETE HACES DEL ESPECTRO ====================
            // (TEXTURIZADOS: dos pasadas por haz — el aura de SU color con la
            // carne fluyendo + el núcleo en banda; el meandro y la punta de
            // nacimiento pulsando heredados de la .54.6)
            if (conv > 0.05f)
            {
                Texture2D carne = DefinitivasLib.TexRuido;
                for (int k = 0; k < NHaz; k++)
                {
                    float angNacimiento = ang + MathHelper.TwoPi * k / NHaz + tiempo * 0.5f;
                    float radioN = MathHelper.Lerp(150f, 26f, conv);
                    Vector2 nace = Projectile.Center - dir * 10f + new Vector2(MathF.Cos(angNacimiento), MathF.Sin(angNacimiento)) * radioN;

                    Color cHaz = ColorDePrisma(k, tiempo);

                    // La espina con MEANDRO (la vida: el haz pequeño ondula al viajar)
                    const int M = 12;
                    var pts = new Vector2[M];
                    var anchoK = new float[M];
                    for (int i = 0; i < M; i++)
                    {
                        float q = i / (float)(M - 1);
                        Vector2 base_ = Vector2.Lerp(nace, foco, q);
                        float ondeo = MathF.Sin(q * MathHelper.Pi) * MathF.Sin(tiempo * 3f + k * 1.7f + q * 9f) * 8f * (1f - conv * 0.5f);
                        pts[i] = base_ + perp * ondeo - Main.screenPosition;
                        anchoK[i] = 0.35f + 0.65f * MathF.Sin(q * MathHelper.Pi);
                    }
                    if (carne != null)
                    {
                        RayoStrip.AbrirLote();
                        try
                        {
                            float semiK = MathHelper.Lerp(7f, 16f, conv) * 0.5f;
                            // EL AURA DEL COLOR — la carne fluye en SU color
                            RayoStrip.FajaTexturadaEnLote(pts, ArraySemis(anchoK, semiK),
                                DefinitivasLib.Alfa(cHaz, 0.40f * conv), carne,
                                130f, 1.6f, tiempo, 0f, 1f);
                            // EL NÚCLEO EN BANDA — la vena caliente
                            RayoStrip.FajaTexturadaEnLote(pts, ArraySemis(anchoK, semiK * 0.45f),
                                DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.70f * conv), carne,
                                90f, 2.2f, tiempo, 0.36f, 0.64f);
                        }
                        finally { RayoStrip.CerrarLote(); }
                    }

                    // LA PUNTA DE NACIMIENTO (la boca del color — pulsa)
                    VFXCore.Begin();
                    float pulsoK = 0.7f + 0.3f * MathF.Sin(tiempo * 5f + k * 0.9f);
                    VFXCore.Quad(nace, DefinitivasLib.Alfa(cHaz, 0.7f * conv * pulsoK), new Vector2(26f, 26f) * pulsoK);
                    VFXCore.FlushAdditive();
                }
            }

            // ==================== CAPAS 6+7 — EL HAZ CENTRAL: LA ESPINA VIVA + LA PILA ====================
            // (el ANCHO CRECIENTE del original sobre LA ESPINA VERLET — la
            // pila de seis pasadas texturizadas de DefinitivasLib)
            if (conv > 0.12f)
            {
                // LA ESPINA: la viva si existe; si no, la recta de siempre
                Vector2[] espina;
                if (_espinaLista)
                {
                    espina = _espina;
                }
                else
                {
                    espina = new Vector2[NEspina];
                    for (int i = 0; i < NEspina; i++)
                        espina[i] = foco + dir * (LargoHaz * i / (NEspina - 1f));
                }

                // EL PERFIL por nodo: la fórmula EXACTA (progress^0.8 · cbrt ·
                // K · (1.1 + cos·0.08)) con respiración + flicker + sobrepaso
                var perfil = new float[NEspina];
                for (int i = 0; i < NEspina; i++)
                {
                    float p = i / (float)(NEspina - 1);
                    perfil[i] = MathF.Max(AnchoHazEn(p, conv) * 0.5f, 0.5f);
                }

                DefinitivasLib.HazFusionEspina(espina, perfil,
                    DefinitivasLib.GradienteJuicio(tiempo * 0.07f), conv, tiempo, _semilla + 20,
                    true, 1f, true);

                // ==================== CAPA 11 — LOS TRES PULSOS QUE VIAJAN ====================
                // (el flujo cromático: bandas de luz que CORREN hacia afuera,
                // ahora TEXTURIZADAS — la carne del pulso también fluye)
                Texture2D carne = DefinitivasLib.TexRuido;
                if (carne != null)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        float prog = DefinitivasLib.Frac(tiempo * 0.34f + k / 3f);
                        float centroPx = prog * LargoHaz;
                        const int MP = 10;
                        var banda = new Vector2[MP];
                        var anchoB = new float[MP];
                        for (int i = 0; i < MP; i++)
                        {
                            float q = i / (float)(MP - 1);
                            float px = centroPx + (q - 0.5f) * 300f;
                            banda[i] = foco + dir * MathHelper.Clamp(px, 0f, LargoHaz) - Main.screenPosition;
                            float local = 1f - MathF.Abs(q - 0.5f) * 2f;
                            anchoB[i] = MathF.Pow(local, 0.7f) *
                                (DefinitivasLib.AnchoCono(MathHelper.Clamp(px / LargoHaz, 0f, 1f), tiempo * 3f, 1f) /
                                 MathF.Max(DefinitivasLib.AnchoCono(1f, tiempo * 3f, 1f), 0.01f));
                            if (anchoB[i] < 0.02f) anchoB[i] = 0.02f;
                        }
                        Color cPulso = DefinitivasLib.GradienteJuicio(tiempo * 0.07f + 0.62f - prog * 0.12f);
                        RayoStrip.AbrirLote();
                        try
                        {
                            RayoStrip.FajaTexturadaEnLote(banda, ArraySemis(anchoB, (13f + 30f * conv)),
                                DefinitivasLib.Alfa(cPulso, 0.70f * conv), carne,
                                150f, 3.2f, tiempo, 0.30f, 0.70f);
                        }
                        finally { RayoStrip.CerrarLote(); }
                    }
                }

                // ==================== CAPAS 13+14 — LA ZONA DE IMPACTO ====================
                // (la punta ARDE: nova a 12 Hz + el aro pulsante + los pinchos
                // de difracción alineados al haz + el resplandor heredado)
                Vector2 punta = espina[NEspina - 1];
                Color g1 = DefinitivasLib.GradienteJuicio(tiempo * 0.07f + 0.25f);
                float pulsoImp = 0.7f + 0.3f * MathF.Sin(tiempo * 12f);
                VFXCore.Begin();
                VFXCore.Quad(punta, DefinitivasLib.Alfa(g1, 0.22f * conv), new Vector2(700f, 700f) * conv);
                VFXCore.Quad(punta, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.30f * conv), new Vector2(320f, 320f) * conv);
                if (VFXCore.NovaBurst != null)
                {
                    VFXCore.Quad(punta, DefinitivasLib.Alfa(Color.White, 0.55f * conv),
                        new Vector2(420f, 420f) * conv * pulsoImp, tiempo * 0.2f, VFXCore.NovaBurst);
                    VFXCore.Quad(punta, DefinitivasLib.Alfa(g1, 0.35f * conv * pulsoImp),
                        new Vector2(260f, 260f) * conv, tiempo * 1.1f, VFXCore.NovaBurst);
                }
                if (VFXCore.Ring != null)
                    VFXCore.Quad(punta, DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.40f * conv * pulsoImp),
                        new Vector2(300f, 300f) * conv * pulsoImp, ang + MathHelper.PiOver2, VFXCore.Ring);
                // LOS PINCHOS DE DIFRACCIÓN en foco y punta (técnica nº6)
                DefinitivasLib.PinchosDifraccion(foco, ang, 380f * conv, Color.White, 0.45f * conv, tiempo);
                DefinitivasLib.PinchosDifraccion(punta, ang, 520f * conv, g1, 0.40f * conv, tiempo);
                VFXCore.FlushAdditive();
            }

            // v6.54.5/.6 — EL CONTRATO DEL LOTE: TODO PreDraw sale con el lote
            // del pase de entidades VIVO (los Flush* y RayoStrip lo cierran).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>
        /// CAPA 2 — UNA POSIMAGEN del barrido: el haz fantasma en su
        /// dirección de hace e·3 ticks, a dos pasadas baratas (aura + filo).
        /// </summary>
        private void DibujarEco(float rotEco, float convEco, float alfaE, float tiempo)
        {
            if (alfaE <= 0.02f) return;
            Vector2 dirE = rotEco.ToRotationVector2();
            Vector2 focoE = PosFoco(convEco);
            Texture2D carne = DefinitivasLib.TexRuido;
            if (carne == null) return;

            const int NE = 12;
            var pts = new Vector2[NE];
            var semi = new float[NE];
            for (int i = 0; i < NE; i++)
            {
                float p = i / (float)(NE - 1);
                pts[i] = focoE + dirE * (LargoHaz * p) - Main.screenPosition;
                float norma = DefinitivasLib.AnchoCono(p, 0f, 1f);
                semi[i] = MathF.Max(norma, 0.05f) * (28f + 90f * convEco) * 0.5f;
            }
            Color cE = DefinitivasLib.GradienteJuicio(tiempo * 0.07f - 0.08f);
            RayoStrip.AbrirLote();
            try
            {
                RayoStrip.FajaTexturadaEnLote(pts, semi, DefinitivasLib.Alfa(cE, 0.30f * alfaE),
                    carne, 480f, 1.1f, tiempo, 0f, 1f);
                RayoStrip.FajaTexturadaEnLote(pts, ArraySemis(semi, 0.28f),
                    DefinitivasLib.Alfa(DefinitivasLib.BlancoCalido, 0.55f * alfaE),
                    carne, 360f, 2.0f, tiempo, 0.36f, 0.64f);
            }
            finally { RayoStrip.CerrarLote(); }
        }

        /// <summary>
        /// CAPA 3 — EL TELÉGRAFO DE LA CARGA: la AGUJA uniforme que LATA
        /// (2→4 Hz, la técnica EmpressLance de la investigación) + LOS
        /// CORREDORES que aceleran con la carga (técnica nº15: la promesa
        /// del trayecto completa ANTES del encendido).
        /// </summary>
        private void TelegrafoDeCarga(Vector2 foco, Vector2 dir, float conv, float tiempo)
        {
            float hz = MathHelper.Lerp(2f, 4f, conv);
            float latido = 0.5f + 0.5f * MathF.Sin(tiempo * hz * MathHelper.TwoPi);
            float w = MathHelper.Lerp(3f, 11f, conv);
            Color tinte = Color.Lerp(DefinitivasLib.BlancoCalido, DefinitivasLib.GradienteJuicio(tiempo * 0.1f), 0.5f);
            float alfa = (0.10f + 0.26f * conv) * (0.55f + 0.45f * latido);

            Texture2D carne = DefinitivasLib.TexRuido;
            if (carne != null)
            {
                var pts = new Vector2[2];
                pts[0] = foco - Main.screenPosition;
                pts[1] = foco + dir * LargoHaz - Main.screenPosition;
                RayoStrip.AbrirLote();
                try
                {
                    RayoStrip.FajaTexturadaEnLote(pts, new float[] { w * 0.5f, w * 0.5f },
                        DefinitivasLib.Alfa(tinte, alfa), carne,
                        480f, 0.6f + conv * 2.2f, tiempo, 0.40f, 0.60f);
                }
                finally { RayoStrip.CerrarLote(); }
            }

            // LOS CORREDORES (los paquetes de luz que recorren la aguja)
            VFXCore.Begin();
            for (int i = 0; i < 3; i++)
            {
                float prog = DefinitivasLib.Frac(tiempo * (0.5f + conv * 1.6f) + i / 3f);
                VFXCore.Quad(foco + dir * (LargoHaz * prog),
                    DefinitivasLib.Alfa(tinte, 0.35f * (1f - prog * 0.4f)),
                    new Vector2(14f, 14f));
            }
            VFXCore.FlushAdditive();
        }

        /// <summary>LOS SEMIANCHOS de un perfil unitario por un semiancho base.</summary>
        private static float[] ArraySemis(float[] perfilUnit, float semi)
        {
            var r = new float[perfilUnit.Length];
            for (int i = 0; i < perfilUnit.Length; i++) r[i] = perfilUnit[i] * semi;
            return r;
        }

        /// <summary>
        /// CAPA 1 — EL VELO PRISMÁTICO: los cuatro bordes de la pantalla
        /// respiran teñidos con el color que cabecea del gradiente (la
        /// anticipación de cine del rayo de fusión, en clave de espectro).
        /// </summary>
        private static void VeloPrismatico(float alfa)
        {
            if (alfa <= 0.02f) return;
            float w = Main.screenWidth, h = Main.screenHeight;
            Vector2 o = Main.screenPosition;
            float tiempo = Main.GlobalTimeWrappedHourly;
            float pulso = 0.9f + 0.1f * MathF.Sin(tiempo * 2.1f);
            Color c = DefinitivasLib.GradienteJuicio(tiempo * 0.05f);

            VFXCore.Begin();
            Color denso = DefinitivasLib.Alfa(new Color(30, 20, 46), alfa * 0.62f * pulso);
            VFXCore.Quad(new Vector2(o.X - w * 0.34f, o.Y + h * 0.5f), denso, new Vector2(w * 1.1f, h * 2.4f), 0f, VFXCore.SoftGlow);
            VFXCore.Quad(new Vector2(o.X + w * 1.34f, o.Y + h * 0.5f), denso, new Vector2(w * 1.1f, h * 2.4f), 0f, VFXCore.SoftGlow);
            VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y - h * 0.36f), denso, new Vector2(w * 2.4f, h * 1.1f), 0f, VFXCore.SoftGlow);
            VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y + h * 1.36f), denso, new Vector2(w * 2.4f, h * 1.1f), 0f, VFXCore.SoftGlow);
            // EL TINTE del gradiente (muy sutil: la sala sabe qué color toca)
            VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y + h * 0.5f), DefinitivasLib.Alfa(c, alfa * 0.05f * pulso),
                new Vector2(w * 1.6f, h * 1.6f), 0f, VFXCore.SoftGlow);
            VFXCore.FlushAlpha();
        }

        /// <summary>EL COLOR DEL ESPECTRO del haz k (los siete del prisma, vivos).</summary>
        private static Color ColorDePrisma(int k, float tiempo)
        {
            // El espectro de vanilla (Last Prism) con el latido del espectro
            float late = 0.82f + 0.18f * MathF.Sin(tiempo * 4f + k * 0.8f);
            Color c = k switch
            {
                0 => new Color(255, 80, 80),      // rojo
                1 => new Color(255, 160, 60),     // naranja
                2 => new Color(255, 230, 100),    // amarillo
                3 => new Color(110, 255, 130),    // verde
                4 => new Color(110, 220, 255),    // cian
                5 => new Color(150, 130, 255),    // azul
                _ => new Color(220, 120, 255),    // violeta
            };
            return DefinitivasLib.Alfa(c, late);
        }
    }
}
