using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using PixelMotor = AethonMod.Content.Weapons.Investigaciones.PixelFxPlayer;

namespace AethonMod.Content.Projectiles.TresCasas
{
    // ======================================================================
    //  EL FUEGO DE LAS TRES CASAS (v6.97) — EL MOTOR DE LAS 20 ARMAS
    //  NUEVAS DE LA FORJA.
    //
    //  LA LETRA DEL USUARIO: «usa tu nueva habilidad de la forja para
    //  crear un nuevo grupo de armas con diferentes efectos y vfx,
    //  intenta que sean vistosos y que sean tipo sagrado, horror
    //  cosmico y oscuridad, crea al menos 20 armas nuevas».
    //
    //  LAS TRES CASAS: SAGRADA (oro y luz blanca) · HORROR CÓSMICO
    //  (teal y violeta) · OSCURIDAD (violeta y negro tinta). Las 20
    //  armas son LOS PLATOS de la forja; ESTE proyectil es EL FUEGO:
    //  UN solo motor con OCHO formas (el haz · la corona · la lluvia ·
    //  la nova · el vórtice · el enjambre · la estela · la zona) que
    //  cada arma viste con SUS colores, SU variante y SUS números.
    //
    //  EL CABLE (viaja TODO en el ai[], el patrón de la casa):
    //   · ai[0] = estilo*100 + variante  (1..8 los platos · 91 la hoja
    //     de la lluvia · 92 el lucero del enjambre — los hijos).
    //   · ai[1] = el color PRIMARIO empaquetado (R<<16|G<<8|B).
    //   · ai[2] = el color SECUNDARIO empaquetado.
    //  EL DAÑO de los hijos hereda del proyectil padre (la arma manda).
    //
    //  EL RENDIMIENTO: cero alocaciones por frame en los bucles calientes
    //  (los arrays de segmentos nacen UNA vez en OnSpawn), los polvos
    //  con cuentagotas y TODO el dibujo por capas de las texturas de
    //  glow de la casa — la lección de HazLib: un draw call por capa.
    // ======================================================================
    public class TresCasasProjectile : ModProjectile
    {
        // === LOS OCHO PLATOS ===
        public const int ESTILO_HAZ = 1;        // el rayo que barre
        public const int ESTILO_CORONA = 2;     // los orbitales del portador
        public const int ESTILO_LLUVIA = 3;     // las hojas que caen del cielo
        public const int ESTILO_NOVA = 4;       // el anillo que estalla
        public const int ESTILO_VORTICE = 5;    // el vórtice que arrastra
        public const int ESTILO_ENJAMBRE = 6;   // los luceros que persiguen
        public const int ESTILO_ESTELA = 7;     // la hoja que va y vuelve
        public const int ESTILO_ZONA = 8;       // el charco que permanece
        public const int ESTILO_HOJA = 91;      // (hijo) la hoja cayendo
        public const int ESTILO_LUCERO = 92;    // (hijo) el lucero cazando

        // === EL ESTADO (todo plano — nada de GC) ===
        private int Estilo => (int)Projectile.ai[0] / 100;
        private int Variante => (int)Projectile.ai[0] % 100;
        private static Color Desempacar(float f)
        {
            int v = (int)f;
            return new Color((v >> 16) & 255, (v >> 8) & 255, v & 255);
        }
        private Color Tinta => Desempacar(Projectile.ai[1]);
        private Color Tinta2 => Desempacar(Projectile.ai[2]);

        /// <summary>LA SEMILLA determinista de la coreografía.</summary>
        private int _semilla;
        private float _fase;
        /// <summary>EL ANCLA (el punto donde nació: el cursor o el jugador).</summary>
        private Vector2 _ancla;
        /// <summary>El reloj de los golpes del anillo de la nova (whoAmI del NPC → el tick que podrá volver a probar).</summary>
        private static readonly Dictionary<int, int> _enfriamiento = new();
        /// <summary>El reloj del spawner (la lluvia y el enjambre sueltan hijos con calma).</summary>
        private int _proximoHijo;
        /// <summary>Los segmentos de la estela (nacen UNA vez — cero GC).</summary>
        private Vector2[] _estela = new Vector2[16];

        /// <summary>EL NÚCLEO DEL MOTOR DE PÍXELES (v6.99): la
        /// herramienta de ANIMACIÓN DE ATAQUES de la superforja — el
        /// mismo motor del Taller de Efectos (el de las armas de la
        /// bolsa de taller), corriendo VIVO dentro del proyectil y
        /// TEÑIDO con los dos colores de su arma.</summary>
        private PixelMotor _motor;

        public override void SetStaticDefaults() { Main.projFrames[Type] = 1; }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void OnSpawn(IEntitySource source)
        {
            _semilla = Main.rand.Next(9973);
            _fase = Main.rand.NextFloat(MathHelper.TwoPi);
            _ancla = Projectile.Center;
            for (int i = 0; i < _estela.Length; i++) _estela[i] = Projectile.Center;

            int est = Estilo;
            switch (est)
            {
                case ESTILO_HAZ:
                    Projectile.timeLeft = 90; Projectile.width = 30; Projectile.height = 30;
                    break;
                case ESTILO_CORONA:
                    Projectile.timeLeft = 480;
                    break;
                case ESTILO_LLUVIA:
                    Projectile.timeLeft = 100;
                    _proximoHijo = 4;
                    break;
                case ESTILO_NOVA:
                    Projectile.timeLeft = 70;
                    break;
                case ESTILO_VORTICE:
                    Projectile.timeLeft = 260;
                    break;
                case ESTILO_ENJAMBRE:
                    Projectile.timeLeft = 120;
                    _proximoHijo = 3;
                    break;
                case ESTILO_ESTELA:
                    Projectile.timeLeft = 130;
                    break;
                case ESTILO_ZONA:
                    Projectile.timeLeft = 300;
                    break;
                case ESTILO_HOJA:
                    Projectile.timeLeft = 220;
                    Projectile.tileCollide = true;
                    Projectile.penetrate = 3;
                    break;
                case ESTILO_LUCERO:
                    Projectile.timeLeft = 300;
                    Projectile.penetrate = 1;
                    break;
            }
            Projectile.netUpdate = true;
        }

        // ================================================================
        //  LA VIDA DE CADA PLATO
        // ================================================================
        public override void AI()
        {
            int est = Estilo;
            _fase += 0.035f;
            // EL NÚCLEO DEL MOTOR DE PÍXELES (v6.99): el efecto del
            // Taller de Efectos corre VIVO — solo el que DIBUJA lo
            // instancia (el servidor ni se entera: el daño es el de
            // siempre) — y se funde con su FADE al morir el proyectil.
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                PixelMotor m = Motor();
                m.Tick();
                if (Projectile.timeLeft <= 12) m.Fx.Stop();
            }
            switch (est)
            {
                case ESTILO_HAZ: AiHaz(); break;
                case ESTILO_CORONA: AiCorona(); break;
                case ESTILO_LLUVIA: AiLluvia(); break;
                case ESTILO_NOVA: AiNova(); break;
                case ESTILO_VORTICE: AiVortice(); break;
                case ESTILO_ENJAMBRE: AiEnjambre(); break;
                case ESTILO_ESTELA: AiEstela(); break;
                case ESTILO_ZONA: AiZona(); break;
                case ESTILO_HOJA: AiHoja(); break;
                case ESTILO_LUCERO: AiLucero(); break;
            }
        }

        // === 1) EL HAZ: nace en el jugador, barre hacia el ancla (el
        //     cursor) y se deshace — la línea muerde cada 6 t. ===
        private void AiHaz()
        {
            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active) { Projectile.Kill(); return; }
            Projectile.Center = dueno.MountedCenter;
            // EL BARRIDO: el ángulo apunta al ancla y oscila (la variante
            //     manda la amplitud: la sagrada barre ancho, el ojo fija).
            float amp = 0.35f + Variante * 0.10f;
            float ang = (float)Math.Atan2(_ancla.Y - Projectile.Center.Y,
                _ancla.X - Projectile.Center.X) +
                MathF.Sin(_fase * 2.2f) * amp;
            float largo = 640f;
            Vector2 fin = Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * largo;

            if (Main.GameUpdateCount % 6ul == 0ul)
                GolpearLinea(Projectile.Center, fin, 26f, Projectile.damage);
            if (Main.rand.NextBool(5))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                        Main.rand.NextFloat(30f, largo * 0.9f),
                    DustID.DungeonSpirit, new Vector2(0f, -0.4f), 160, Tinta, 0.55f);
                d.noGravity = true;
            }
        }

        // === 2) LA CORONA: N orbitales girando alrededor del portador —
        //     cada uno es una hoja que muerde al que toca. ===
        private void AiCorona()
        {
            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active) { Projectile.Kill(); return; }
            Projectile.Center = dueno.MountedCenter;

            int n = 3 + Variante;
            float radio = 92f + Variante * 9f;
            for (int i = 0; i < n; i++)
            {
                float a = _fase * 1.9f + i * MathHelper.TwoPi / n;
                Vector2 pos = Projectile.Center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radio;
                // LA MORDIDA del orbital (bala — sin i-frames cruzados).
                if (Main.GameUpdateCount % 8ul == 0ul)
                    GolpearPunto(pos, 24f, (int)(Projectile.damage * 0.75f));
                if (Main.rand.NextBool(22))
                {
                    Dust d = Dust.NewDustPerfect(pos, DustID.DungeonSpirit,
                        new Vector2(0f, -0.5f), 150, Tinta, 0.5f);
                    d.noGravity = true;
                }
            }
        }

        // === 3) LA LLUVIA: la zona del cursor escupe hojas del cielo —
        //     cada hoja es un hijo que cae y se clava. ===
        private void AiLluvia()
        {
            if (Projectile.timeLeft <= _proximoHijo && _proximoHijo < 90)
            {
                int cadencia = 5 - Math.Min(2, Variante);
                _proximoHijo = Projectile.timeLeft - cadencia;
                float dx = Main.rand.NextFloat(-170f, 170f);
                Vector2 origen = new Vector2(_ancla.X + dx, _ancla.Y - 460f);
                int idx = Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                    origen, new Vector2(Main.rand.NextFloat(-1.2f, 1.2f), 9.5f),
                    Type, (int)(Projectile.damage * 0.8f), Projectile.knockBack,
                    Projectile.owner, ESTILO_HOJA * 100 + Variante,
                    Projectile.ai[1], Projectile.ai[2]);
                if (idx >= 0) Main.projectile[idx].netUpdate = true;
            }
        }

        // === 4) LA NOVA: carga 20 t en el ancla y estalla en UN anillo
        //     que crece — muerde a cada blanco UNA sola vez por anillo. ===
        private void AiNova()
        {
            Projectile.Center = _ancla;
            int vivido = 70 - Projectile.timeLeft;
            if (vivido < 20) return;   // LA CARGA

            float radio = (vivido - 20) * 17f;
            if (radio > 6f && radio < 560f)
                GolpearAnillo(Projectile.Center, radio - 18f, radio + 14f, Projectile.damage);

            // EL CHISPAZO de la explosión.
            if (vivido == 20 && !Main.dedServ)
            {
                for (int k = 0; k < 18; k++)
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center,
                        DustID.DungeonSpirit, new Vector2(
                            Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-6f, 6f)),
                        170, Tinta, 0.8f);
                    d.noGravity = true;
                }
            }
        }

        // === 5) EL VÓRTICE: el corazón negro en el ancla — ATRAE a los
        //     vivos y los muele cada 14 t. ===
        private void AiVortice()
        {
            Projectile.Center = _ancla;
            float radio = 240f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                float dist = n.Center.DistanceSQ(Projectile.Center);
                if (dist > radio * radio) continue;
                // EL TIRÓN (más fuerte cerca del corazón).
                float fuerza = (1f - MathF.Sqrt(dist) / radio) * 0.55f;
                Vector2 hacia = Projectile.Center - n.Center;
                if (hacia.LengthSquared() > 1f)
                    n.velocity += Vector2.Normalize(hacia) * fuerza;
                if (Main.GameUpdateCount % 14ul == 0ul)
                    GolpearPunto(n.Center, 30f, (int)(Projectile.damage * 0.6f));
            }
        }

        // === 6) EL ENJAMBRE: la zona suelta LUCEROS que persiguen al
        //     enemigo más cercano y revientan al tocarlo. ===
        private void AiEnjambre()
        {
            if (Projectile.timeLeft <= _proximoHijo && _proximoHijo < 110)
            {
                _proximoHijo = Projectile.timeLeft - 6;
                Vector2 origen = _ancla + new Vector2(
                    Main.rand.NextFloat(-90f, 90f), Main.rand.NextFloat(-70f, -20f));
                int idx = Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                    origen, new Vector2(Main.rand.NextFloat(-2f, 2f), -3.2f),
                    Type, (int)(Projectile.damage * 0.7f), 1.5f,
                    Projectile.owner, ESTILO_LUCERO * 100 + Variante,
                    Projectile.ai[1], Projectile.ai[2]);
                if (idx >= 0) Main.projectile[idx].netUpdate = true;
            }
        }

        // === 7) LA ESTELA: la hoja vuela al ancla, frena, y VUELVE al
        //     dueño — la curva del bumerán de la casa. ===
        private void AiEstela()
        {
            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active) { Projectile.Kill(); return; }

            int vivido = 130 - Projectile.timeLeft;
            if (vivido < 55)
            {
                // LA IDA: persigue el ancla y gira con vida propia.
                Vector2 hacia = _ancla - Projectile.Center;
                if (hacia.LengthSquared() > 900f)
                    Projectile.velocity += Vector2.Normalize(hacia) * 0.55f;
                Projectile.velocity *= 0.965f;
            }
            else
            {
                // LA VUELTA: regresa al dueño (se recoge al llegar).
                Vector2 hacia = dueno.MountedCenter - Projectile.Center;
                if (hacia.LengthSquared() < 700f) { Projectile.Kill(); return; }
                Projectile.velocity = Projectile.velocity * 0.93f +
                    Vector2.Normalize(hacia) * 1.15f;
            }
            Projectile.rotation += 0.36f + Variante * 0.04f;

            // LA ESTELA (el rastro que se deshace).
            for (int i = _estela.Length - 1; i > 0; i--) _estela[i] = _estela[i - 1];
            _estela[0] = Projectile.Center;
        }

        // === 8) LA ZONA: el charco que permanece en el ancla — muele a
        //     TODO el que pisa, cada 10 t. ===
        private void AiZona()
        {
            Projectile.Center = _ancla;
            float radio = 120f + Variante * 16f;
            if (Main.GameUpdateCount % 10ul == 0ul)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                    if (n.Center.DistanceSQ(Projectile.Center) > radio * radio) continue;
                    GolpearPunto(n.Center, 40f, (int)(Projectile.damage * 0.55f));
                }
            }
            if (Main.rand.NextBool(6))
            {
                float a = Main.rand.NextFloat(MathHelper.TwoPi);
                float r = Main.rand.NextFloat(radio * 0.9f);
                Dust d = Dust.NewDustPerfect(Projectile.Center +
                    new Vector2(MathF.Cos(a) * r, MathF.Sin(a) * r * 0.42f),
                    DustID.DungeonSpirit, new Vector2(0f, -0.7f), 140, Tinta, 0.5f);
                d.noGravity = true;
            }
        }

        // === 91) LA HOJA (hija de la lluvia): cae, gira, se clava. ===
        private void AiHoja()
        {
            Projectile.velocity.Y += 0.085f;
            Projectile.rotation += 0.42f;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Estilo == ESTILO_HOJA && !Main.dedServ)
            {
                for (int k = 0; k < 6; k++)
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.DungeonSpirit,
                        new Vector2(Main.rand.NextFloat(-2.5f, 2.5f),
                            Main.rand.NextFloat(-3f, 0f)), 150, Tinta, 0.6f);
                    d.noGravity = true;
                }
            }
            return true;
        }

        // === 92) EL LUCERO (hijo del enjambre): caza al más cercano. ===
        private void AiLucero()
        {
            NPC blanco = null;
            float mejor = 640f * 640f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                float d = n.Center.DistanceSQ(Projectile.Center);
                if (d < mejor) { mejor = d; blanco = n; }
            }
            if (blanco != null)
            {
                Vector2 hacia = blanco.Center - Projectile.Center;
                if (hacia.LengthSquared() > 1f)
                    Projectile.velocity += Vector2.Normalize(hacia) * 0.42f;
            }
            Projectile.velocity *= 0.972f;
            Projectile.rotation += 0.5f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // EL CHISPAZO del contacto (las hojas y los luceros).
            if (!Main.dedServ && (Estilo == ESTILO_HOJA || Estilo == ESTILO_LUCERO))
            {
                for (int k = 0; k < 8; k++)
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.DungeonSpirit,
                        new Vector2(Main.rand.NextFloat(-3f, 3f),
                            Main.rand.NextFloat(-3.5f, 0.5f)), 160, Tinta, 0.7f);
                    d.noGravity = true;
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            // EL MOTOR se apaga con el proyectil: ni un byte de textura
            // ni de buffers más allá de la muerte (el patrón del Taller).
            _motor?.Dispose();
            _motor = null;
        }

        // ================================================================
        //  EL NÚCLEO DEL MOTOR DE PÍXELES (v6.99) — LA HERRAMIENTA DE
        //  ANIMACIÓN DE ATAQUES DE LA SUPERFORJA (la letra: «entre las
        //  herramientas había una para crear animaciones de ataque — es
        //  la que usaste para crear las armas de la bolsa de taller —
        //  ¿por qué no lo usas para crear proyectiles y ataques
        //  interesantes?»). LOS DIEZ proyectiles (ocho platos + dos
        //  hijos) llevan dentro un efecto del motor del Taller de
        //  Efectos, forjado con la SEMILLA DEL ARMA (todos los
        //  proyectiles de la misma arma comparten coreografía: el arma
        //  TIENE un ataque propio) y TEÑIDO con sus dos colores.
        // ================================================================

        /// <summary>Instancia (UNA vez) el efecto del motor de este
        /// plato: la semilla es del ARMA y la rampatinta la tejen sus
        /// dos colores.</summary>
        private PixelMotor Motor()
        {
            if (_motor == null)
            {
                uint c1 = PxColor.Rgb(Tinta.R, Tinta.G, Tinta.B);
                uint c2 = PxColor.Rgb(Tinta2.R, Tinta2.G, Tinta2.B);
                int semilla = Estilo * 37 + Variante;
                _motor = new PixelMotor(Estilo switch
                {
                    ESTILO_HAZ => PixelFxCasas.EmisorHaz(semilla, c1, c2),
                    ESTILO_CORONA => PixelFxCasas.CorazonCorona(semilla, c1, c2),
                    ESTILO_LLUVIA => PixelFxCasas.VeloLluvia(semilla, c1, c2),
                    ESTILO_NOVA => PixelFxCasas.NovaActo(semilla, c1, c2),
                    ESTILO_VORTICE => PixelFxCasas.GargantaVortice(semilla, c1, c2),
                    ESTILO_ENJAMBRE => PixelFxCasas.NidoColmena(semilla, c1, c2),
                    ESTILO_ESTELA => PixelFxCasas.HojaEstela(semilla, c1, c2),
                    ESTILO_ZONA => PixelFxCasas.CharcoZona(semilla, c1, c2),
                    ESTILO_HOJA => PixelFxCasas.HojaCae(semilla, c1, c2),
                    _ => PixelFxCasas.LuceroCaza(semilla, c1, c2),
                }, 2f);
            }
            return _motor;
        }

        /// <summary>El ANCLA DEL MUNDO donde vive el núcleo: el pecho
        /// del portador para el haz y la corona; el punto del plato
        /// para el resto.</summary>
        private Vector2 AnclaDelMotor()
        {
            if (Estilo == ESTILO_HAZ || Estilo == ESTILO_CORONA)
            {
                Player dueno = Main.player[Projectile.owner];
                if (dueno != null && dueno.active) return dueno.MountedCenter;
            }
            return Projectile.Center;
        }

        // ================================================================
        //  LOS GOLPES DEL MOTOR (el cauce de la casa: GolpeMotor)
        // ================================================================

        /// <summary>Golpea al PRIMER blanco que pise este punto (bala).</summary>
        private void GolpearPunto(Vector2 pos, float radio, int dano)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                if (n.Center.DistanceSQ(pos) > radio * radio) continue;
                global::AethonMod.Content.Systems.GolpeMotor.Golpear(Projectile, n, dano, 2f);
                break;
            }
        }

        /// <summary>Golpea a TODO el que cruce la línea (el haz — cada 6 t).</summary>
        private void GolpearLinea(Vector2 a, Vector2 b, float grosor, int dano)
        {
            Vector2 dir = b - a;
            float largo = dir.Length();
            if (largo < 1f) return;
            dir /= largo;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                Vector2 rel = n.Center - a;
                float t = Vector2.Dot(rel, dir);
                if (t < 0f || t > largo) continue;
                float dist = MathF.Abs(rel.X * -dir.Y + rel.Y * dir.X);
                if (dist > grosor + n.width * 0.4f) continue;
                global::AethonMod.Content.Systems.GolpeMotor.Golpear(Projectile, n, dano, 1.5f);
            }
        }

        /// <summary>Golpea a los que estén DENTRO del anillo que crece
        /// (la nova — cada blanco UNA vez por explosión: el enfriamiento
        /// por whoAmI lo garantiza).</summary>
        private void GolpearAnillo(Vector2 centro, float rMin, float rMax, int dano)
        {
            ulong ahora = Main.GameUpdateCount;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !n.CanBeChasedBy()) continue;
                float r = MathF.Sqrt(n.Center.DistanceSQ(centro));
                if (r < rMin || r > rMax) continue;
                if (_enfriamiento.TryGetValue(n.whoAmI, out int hasta) && hasta > (int)ahora)
                    continue;
                _enfriamiento[n.whoAmI] = (int)ahora + 40;
                global::AethonMod.Content.Systems.GolpeMotor.Golpear(Projectile, n, dano, 5f);
            }
        }

        // ================================================================
        //  EL VESTIDO POR CAPAS (v6.98) — la letra del usuario: «para
        //  los proyectiles usa el poder de todas nuestras herramientas
        //  y librerias combinando todo puede salir algo increible […]
        //  cada proyectil se debe crear por capas de vfx, cada capa
        //  agregando algo nuevo al menos entre 5 a 10 capas de vfx por
        //  proyectil».
        //
        //  LAS CAPAS DE LA CASA (de atrás hacia delante — cada plato
        //  viste entre 5 y 10):
        //   1. EL VELO DE RUIDO — el flipbook procedural Ruido_0..19
        //      (la textura VIVA que late y gira, tintada — el plasma).
        //   2. LOS ANILLOS — GlowCircleWhite/MagicRing/FireRing/Ring:
        //      los aros que giran, respiran y se pulsan.
        //   3. LA FIRMA — la textura que da IDENTIDAD al plato:
        //      PulsarCore (la nova) · Star (el lucero) · Crescent (la
        //      hoja) · Vortex (el vórtice) · CometHead (los brazos) —
        //      NADA de círculos planos.
        //   4. LAS CHISPAS — SparkleStar/FlareEstrella4Puntas en
        //      posiciones DETERMINISTAS (el Azar de la semilla) que
        //      titilan solas — cero GC, cero estado nuevo.
        //   5. EL CORAZÓN BLANCO — el núcleo caliente arriba de todo.
        //  TODO en lotes de la casa: el aditivo BRILLA (un Begin solo
        //  por proyectil), el corazón negro del vórtice va en ALPHA
        //  (lo aditivo no puede oscurecer) — el patrón de la nova
        //  (VFXCore cierra · el lote viste · VFXCore reabre el vanilla).
        // ================================================================

        /// <summary>El FLIPBOOK DEL RUIDO (20 marcos que rotan solos —
        /// cacheados por índice: la cadena solo nace una vez).</summary>
        private static Texture2D[] _ruidoCache;

        private static Texture2D Ruido(int semilla, float vel)
        {
            _ruidoCache ??= new Texture2D[20];
            int n = (int)(Main.GlobalTimeWrappedHourly * vel + semilla * 0.37f) % 20;
            if (n < 0) n += 20;
            return _ruidoCache[n] ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/Ruido_" + n).Value;
        }

        private static Texture2D Tex(string ruta) =>
            ModContent.Request<Texture2D>("AethonMod/Content/Effects/" + ruta).Value;

        /// <summary>EL AZAR DETERMINISTA de la semilla (0..1 — la
        /// coreografía es estable entre frames: nada parpadea al azar).</summary>
        private float Azar(int i)
        {
            float v = MathF.Sin(_semilla * 12.9898f + i * 78.233f) * 43758.5453f;
            return v - MathF.Floor(v);
        }

        /// <summary>EL TITILAR (0..1..0 — el pulso lento de cada chispa).</summary>
        private float Titilar(int i, float vel = 3.1f)
            => 0.5f - 0.5f * MathF.Cos(_fase * vel + Azar(i) * MathHelper.TwoPi);

        /// <summary>UNA capa: la textura centrada, girada y tintada.</summary>
        private static void Capa(SpriteBatch sb, Texture2D tex, Vector2 pos,
            Color color, float rot, float esc)
        {
            if (color.A == 0 || esc <= 0f) return;
            sb.Draw(tex, pos, null, color, rot, tex.Size() * 0.5f, esc,
                SpriteEffects.None, 0f);
        }

        /// <summary>UNA capa de SUELO (la elipse achatada del mundo).</summary>
        private static void CapaSuelo(SpriteBatch sb, Texture2D tex, Vector2 pos,
            Color color, float rot, float anchoPx, float altoPx)
        {
            if (color.A == 0 || anchoPx <= 0f) return;
            sb.Draw(tex, pos, null, color, rot, tex.Size() * 0.5f,
                new Vector2(anchoPx / tex.Width, altoPx / tex.Height),
                SpriteEffects.None, 0f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.gameMenu) return false;
            try
            {
                VFXCore.CerrarLoteSiAbierto();
                int est = Estilo;
                // 0) EL NÚCLEO DEL MOTOR DE PÍXELES (v6.99 — la
                //    herramienta de animación de ataques de la
                //    superforja): el alfa recta del compositor, el
                //    píxel NÍTIDO (PointClamp) — DEBAJO de todo lo que
                //    brilla. Las hojas giratorias rotan el ARTE del
                //    efecto con la rotación del proyectil.
                if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                {
                    PixelMotor m = Motor();
                    if (est == ESTILO_ESTELA || est == ESTILO_HOJA || est == ESTILO_LUCERO)
                        m.Fx.options.Angle = Projectile.rotation;
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                        SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    m.Draw(Main.spriteBatch, AnclaDelMotor() - Main.screenPosition -
                        new Vector2(m.Meta.Ox, m.Meta.Oy) * m.Escala);
                    Main.spriteBatch.End();
                }
                // EL CORAZÓN NEGRO del vórtice va en ALPHA (lo aditivo
                // no puede oscurecer) — debajo de todo lo que brilla.
                if (est == ESTILO_VORTICE)
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    DibujarCorazonNegro(Main.spriteBatch);
                    Main.spriteBatch.End();
                }
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                switch (est)
                {
                    case ESTILO_HAZ: VestirHaz(Main.spriteBatch); break;
                    case ESTILO_CORONA: VestirCorona(Main.spriteBatch); break;
                    case ESTILO_LLUVIA: VestirLluvia(Main.spriteBatch); break;
                    case ESTILO_NOVA: VestirNova(Main.spriteBatch); break;
                    case ESTILO_VORTICE: VestirVortice(Main.spriteBatch); break;
                    case ESTILO_ENJAMBRE: VestirEnjambre(Main.spriteBatch); break;
                    case ESTILO_ESTELA: VestirEstela(Main.spriteBatch); break;
                    case ESTILO_ZONA: VestirZona(Main.spriteBatch); break;
                    case ESTILO_HOJA: VestirHoja(Main.spriteBatch); break;
                    case ESTILO_LUCERO: VestirLucero(Main.spriteBatch); break;
                }
                Main.spriteBatch.End();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            VFXCore.ReabrirLoteVanilla();
            return false;   // TODO lo dibuja la casa (sin el vanilla)
        }

        // === EL HAZ (8 capas): el velo de ruido · el halo · el cuerpo
        //     pulsante · el núcleo blanco · el pulso viajero · la
        //     estrella del origen · el corazón del origen · el destello
        //     del final. ===
        private void VestirHaz(SpriteBatch sb)
        {
            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active) return;
            Color c1 = Tinta, c2 = Tinta2;
            float amp = 0.35f + Variante * 0.10f;
            float ang = (float)Math.Atan2(_ancla.Y - Projectile.Center.Y,
                _ancla.X - Projectile.Center.X) + MathF.Sin(_fase * 2.2f) * amp;
            float frac = Math.Min(1f, (90 - Projectile.timeLeft) / 12f);
            float largo = 640f * frac;
            if (largo < 8f) return;
            Vector2 dir = new(MathF.Cos(ang), MathF.Sin(ang));
            Vector2 origen = dueno.MountedCenter - Main.screenPosition;
            Texture2D rayo = Tex("GlowRay");
            // 1 — EL VELO DE RUIDO a lo largo del haz (el plasma vivo)
            CapaSuelo(sb, Ruido(_semilla, 5.5f), origen + dir * (largo * 0.5f),
                c2 * 0.10f, ang, largo, 110f);
            // 2 — EL HALO ancho · 3 — EL CUERPO que respira · 4 — EL NÚCLEO
            DibujarRayo(sb, rayo, origen, ang, largo, 58f, c2 * 0.35f);
            DibujarRayo(sb, rayo, origen, ang, largo, 30f + 7f * MathF.Sin(_fase * 6f), c1 * 0.80f);
            DibujarRayo(sb, rayo, origen, ang, largo, 12f, Color.White);
            // 5 — EL PULSO VIAJERO (la bola que recorre el haz)
            float t = (_fase * 0.85f) % 1f;
            if (t < 0f) t += 1f;
            Vector2 pulsoPos = origen + dir * (largo * t);
            Capa(sb, Tex("GlowOrb"), pulsoPos, c1 * 0.55f, ang, (46f + 14f * MathF.Sin(_fase * 9f)) / 128f);
            Capa(sb, Tex("GlowOrb"), pulsoPos, Color.White * 0.65f, ang, 16f / 128f);
            // 6 — LA ESTRELLA del origen (girando) · 7 — SU CORAZÓN
            Capa(sb, Tex("FlareEstrella4Puntas"), origen, c2,
                _fase * 3f, (86f + 12f * MathF.Sin(_fase * 5f)) / 256f);
            Capa(sb, Tex("Procedural/PulsarCore"), origen, c1 * 0.85f, -_fase * 2f, 40f / 128f);
            // 8 — EL DESTELLO del final del haz
            Vector2 fin = origen + dir * largo;
            Capa(sb, Tex("GlowOrb"), fin, c1 * 0.40f, ang, 40f / 128f);
            Capa(sb, Tex("SparkleStar"), fin, Color.White * (0.4f + 0.3f * Titilar(1)),
                _fase * 2f, (30f + 12f * Titilar(1)) / 32f);
        }

        // === LA CORONA (8 capas): el anillo del suelo · el aro rúnico ·
        //     y POR CADA ORBE: su velo de ruido · su halo · su núcleo
        //     PulsarCore · su corazón blanco · (+ la chispa que persigue
        //     y el titilar del guía). ===
        private void VestirCorona(SpriteBatch sb)
        {
            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active) return;
            Color c1 = Tinta, c2 = Tinta2;
            int n = 3 + Variante;
            float radio = 92f + Variante * 9f;
            Vector2 basePos = dueno.MountedCenter - Main.screenPosition;
            Vector2 suelo = basePos + new Vector2(0f, 22f);
            // 1 — EL ANILLO DEL SUELO (la órbita dibujada en el piso)
            CapaSuelo(sb, Tex("GlowCircleWhite"), suelo, c1 * 0.10f, 0f, radio * 2.1f, radio * 0.66f);
            // 2 — EL ARO RÚNICO (girando con la órbita misma)
            CapaSuelo(sb, Tex("MagicRing"), suelo, c2 * 0.22f, _fase * 1.9f, radio * 2.05f, radio * 0.62f);
            // 3..6 — LAS CAPAS DE CADA ORBE
            for (int i = 0; i < n; i++)
            {
                float a = _fase * 1.9f + i * MathHelper.TwoPi / n;
                Vector2 p = basePos + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radio;
                Capa(sb, Ruido(_semilla + i, 4.2f), p, c2 * 0.10f, a, 58f / 512f);
                Capa(sb, Tex("GlowOrb"), p, c2 * 0.45f, a, 80f / 128f);
                Capa(sb, Tex("Procedural/PulsarCore"), p, c1 * 0.95f, -a * 2f, 46f / 128f);
                Capa(sb, Tex("GlowOrb"), p, Color.White * 0.55f, a, 20f / 128f);
            }
            // 7 — LA CHISPA QUE PERSIGUE (el rastro del orbe guía)
            float aGuia = _fase * 1.9f;
            Vector2 pRastro = basePos + new Vector2(MathF.Cos(aGuia - 0.42f), MathF.Sin(aGuia - 0.42f)) * (radio * 0.96f);
            Capa(sb, Tex("SparkleStar"), pRastro, c1 * (0.35f + 0.3f * Titilar(0)), aGuia, 22f / 32f);
            // 8 — EL TITILAR DEL GUÍA (encima del primer orbe)
            Vector2 pGuia = basePos + new Vector2(MathF.Cos(aGuia), MathF.Sin(aGuia)) * radio;
            Capa(sb, Tex("SparkleStar"), pGuia, Color.White * (0.30f + 0.35f * Titilar(0, 4.5f)),
                _fase * 4f, (24f + 14f * Titilar(0, 4.5f)) / 32f);
        }

        // === LA LLUVIA (6 capas): el velo del suelo · el charco · el
        //     aro interno girando · LOS VERTICALES que caen · los
        //     destellos del suelo · el aro rúnico. ===
        private void VestirLluvia(SpriteBatch sb)
        {
            Color c1 = Tinta, c2 = Tinta2;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Vector2 suelo = new(pos.X, _ancla.Y - Main.screenPosition.Y);
            float late = 0.8f + 0.2f * MathF.Sin(_fase * 3f);
            // 1 — EL VELO DEL SUELO (el ruido que respira)
            CapaSuelo(sb, Ruido(_semilla, 3.8f), suelo, c1 * (0.10f * late), _fase * 0.22f, 360f, 120f);
            // 2 — EL CHARCO · 3 — EL ARO INTERNO (girando)
            CapaSuelo(sb, Tex("GlowCircleWhite"), suelo, c1 * 0.16f, 0f, 360f, 88f);
            CapaSuelo(sb, Tex("GlowCircleWhite"), suelo, c2 * (0.12f * late), _fase, 240f, 62f);
            // 4 — LOS VERTICALES QUE CAEN (la promesa de las hojas)
            Texture2D rayo = Tex("GlowRay");
            for (int i = 0; i < 3; i++)
            {
                float x = suelo.X + (Azar(i) * 2f - 1f) * 150f;
                float caida = (Main.GlobalTimeWrappedHourly * 430f + Azar(i) * 430f) % 430f;
                Vector2 p = new(x, suelo.Y - 430f + caida);
                DibujarRayo(sb, rayo, p, MathHelper.PiOver2, 46f, 6f, c1 * 0.28f);
            }
            // 5 — LOS DESTELLOS DEL SUELO (donde van a clavarse)
            for (int i = 0; i < 3; i++)
            {
                Vector2 p = new(suelo.X + (Azar(i) * 2f - 1f) * 150f, suelo.Y - 4f);
                Capa(sb, Tex("SparkleStar"), p, c2 * (0.30f + 0.45f * Titilar(i, 4f)),
                    _fase + Azar(i), (16f + 14f * Titilar(i, 4f)) / 32f);
            }
            // 6 — EL ARO RÚNICO del perímetro
            CapaSuelo(sb, Tex("MagicRing"), suelo, c2 * 0.18f, -_fase * 0.8f, 372f, 96f);
        }

        // === LA NOVA (10 capas): la CARGA viste velo de ruido creciente
        //     · chispas que convergen · el PulsarCore que se arma · su
        //     corazón · el aro de carga — y el ESTALLIDO: el FireRing
        //     expansivo · la doble onda de choque · las llamaradas del
        //     anillo · el ruido que se deshace · el fogonazo blanco. ===
        private void VestirNova(SpriteBatch sb)
        {
            Color c1 = Tinta, c2 = Tinta2;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            int vivido = 70 - Projectile.timeLeft;
            if (vivido < 20)
            {
                float carga = vivido / 20f;
                // 1 — EL VELO que crece con la carga
                Capa(sb, Ruido(_semilla, 6f), pos, c1 * (0.10f + 0.10f * carga),
                    _fase * 2f, (50f + 130f * carga) / 512f);
                // 2 — LAS CHISPAS QUE CONVERGEN (el imán del estallido)
                for (int i = 0; i < 3; i++)
                {
                    float a = _fase * 3.4f + i * MathHelper.TwoPi / 3f;
                    float r = 76f * (1f - carga) + 14f;
                    Vector2 p = pos + new Vector2(MathF.Cos(a), MathF.Sin(a)) * r;
                    Capa(sb, Tex("GlowOrb"), p, c1 * 0.6f, a, 20f / 128f);
                }
                // 3 — EL PULSARCORE que se arma · 4 — SU CORAZÓN
                Capa(sb, Tex("Procedural/PulsarCore"), pos, c1 * (0.5f + 0.5f * carga),
                    _fase * 4f, (36f + 46f * carga) / 128f);
                Capa(sb, Tex("GlowOrb"), pos, Color.White * (0.35f + 0.5f * carga),
                    0f, (14f + 22f * carga) / 128f);
                // 5 — EL ARO DE CARGA (el disparo en cámara lenta)
                Capa(sb, Tex("Procedural/Ring"), pos, c2 * (0.15f + 0.25f * carga),
                    -_fase * 2.5f, (40f + 150f * carga) / 256f);
            }
            else
            {
                float radio = (vivido - 20) * 17f;
                float vida = 1f - (vivido - 20) / 50f;
                if (vida <= 0f) return;
                // 6 — EL FIRERING EXPANSIVO (el aro de fuego de verdad)
                Capa(sb, Tex("Procedural/FireRing"), pos, c1 * (0.85f * vida),
                    _fase * 2f, (radio * 2.2f) / 256f);
                // 7 — LA DOBLE ONDA DE CHOQUE
                Capa(sb, Tex("GlowCircleWhite"), pos, c1 * (0.75f * vida), 0f, (radio * 2f) / 64f);
                Capa(sb, Tex("GlowCircleWhite"), pos, Color.White * (0.55f * vida), 0f, (radio * 1.6f) / 64f);
                // 8 — LAS LLAMARADAS DEL ANILLO (las puntas que giran)
                for (int i = 0; i < 4; i++)
                {
                    float a = _fase * 2.2f + i * MathHelper.PiOver2;
                    Vector2 p = pos + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radio;
                    Capa(sb, Tex("FlareEstrella4Puntas"), p, c2 * (0.7f * vida),
                        a, (46f * vida + 18f) / 256f);
                }
                // 9 — EL RUIDO QUE SE DESHACE
                Capa(sb, Ruido(_semilla, 6f), pos, c1 * (0.12f * vida),
                    _fase, (radio * 1.4f) / 512f);
                // 10 — EL FOGONAZO (solo los primeros ticks)
                if (vivido - 20 < 6)
                {
                    float fl = (6 - (vivido - 20)) / 6f;
                    Capa(sb, Tex("GlowOrb"), pos, Color.White * (0.8f * fl), 0f, (150f * fl) / 128f);
                }
            }
        }

        // === EL VÓRTICE (8 capas + el corazón negro en alpha): el velo
        //     ambiental · LA TEXTURA VORTEX girando · el disco exterior
        //     · los brazos con CometHead · los titilares del borde · el
        //     aro del corazón · el corazón negro · su núcleo blanco. ===
        private void DibujarCorazonNegro(SpriteBatch sb)
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Capa(sb, Tex("Procedural/BlackDisk"), pos, Color.White * 0.88f, 0f, 64f / 256f);
        }

        private void VestirVortice(SpriteBatch sb)
        {
            Color c1 = Tinta, c2 = Tinta2;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float disco = 232f;
            // 1 — EL VELO AMBIENTAL (el ruido que lo envuelve todo)
            Capa(sb, Ruido(_semilla, 2.6f), pos, c2 * 0.10f, _fase * 0.6f, disco / 512f);
            // 2 — LA TEXTURA VORTEX (la firma del plato — girando)
            Capa(sb, Tex("Procedural/Vortex"), pos, c1 * 0.40f, -_fase * 2.4f,
                (disco * (1f + 0.06f * MathF.Sin(_fase * 3f))) / 64f);
            // 3 — EL DISCO EXTERIOR
            Capa(sb, Tex("GlowCircleWhite"), pos, c2 * 0.30f, _fase, disco / 64f);
            // 4 — LOS BRAZOS DE LA ESPIRAL (CometHead: cabeza con núcleo)
            Texture2D cometa = Tex("Procedural/CometHead");
            for (int brazo = 0; brazo < 3; brazo++)
            {
                for (int paso = 1; paso <= 7; paso++)
                {
                    float a = _fase * 2.4f + brazo * MathHelper.TwoPi / 3f + paso * 0.55f;
                    float r = paso * 26f;
                    Vector2 p = pos + new Vector2(MathF.Cos(a), MathF.Sin(a)) * r;
                    Capa(sb, cometa, p, c1 * (0.65f - paso * 0.07f), a,
                        (38f - paso * 3f) / 192f);
                }
            }
            // 5 — LOS TITILARES DEL BORDE
            for (int i = 0; i < 3; i++)
            {
                float a = _fase * 1.7f + i * MathHelper.TwoPi / 3f;
                Vector2 p = pos + new Vector2(MathF.Cos(a), MathF.Sin(a)) * (disco * 0.48f);
                Capa(sb, Tex("SparkleStar"), p, c2 * (0.3f + 0.5f * Titilar(i, 4.2f)),
                    a, (14f + 12f * Titilar(i, 4.2f)) / 32f);
            }
            // 6 — EL ARO DEL CORAZÓN (la garganta) · 7 — SU NÚCLEO
            Capa(sb, Tex("Procedural/Ring"), pos, c2 * 0.55f, _fase * 2f, 92f / 256f);
            Capa(sb, Tex("GlowOrb"), pos, Color.White * (0.35f + 0.25f * MathF.Sin(_fase * 5f)),
                0f, 22f / 128f);
        }

        // === EL ENJAMBRE (6 capas): el velo del suelo · el charco · el
        //     aro rúnico · LAS MOTES que orbitan · la estrella del
        //     centro · el pulso del borde. ===
        private void VestirEnjambre(SpriteBatch sb)
        {
            Color c1 = Tinta, c2 = Tinta2;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            // 1 — EL VELO · 2 — EL CHARCO
            CapaSuelo(sb, Ruido(_semilla, 3.4f), pos, c1 * 0.08f, _fase * 0.2f, 240f, 90f);
            CapaSuelo(sb, Tex("GlowCircleWhite"), pos, c1 * 0.12f, 0f, 220f, 110f);
            // 3 — EL ARO RÚNICO
            CapaSuelo(sb, Tex("MagicRing"), pos, c2 * 0.20f, _fase * 1.2f, 226f, 104f);
            // 4 — LAS MOTES QUE ORBITAN (los luceros por nacer)
            for (int i = 0; i < 5; i++)
            {
                float a = _fase * (1.4f + Azar(i) * 1.6f) + Azar(i + 7) * MathHelper.TwoPi;
                float r = 34f + Azar(i + 3) * 42f;
                Vector2 p = pos + new Vector2(MathF.Cos(a), MathF.Sin(a) * 0.62f) * r;
                Color cMot = (i % 2 == 0) ? c1 : c2;
                Capa(sb, Tex("SparkleStar"), p, cMot * (0.25f + 0.55f * Titilar(i, 3.6f)),
                    a, (13f + 10f * Titilar(i, 3.6f)) / 32f);
            }
            // 5 — LA ESTRELLA DEL CENTRO (el ojo del enjambre)
            Capa(sb, Tex("Procedural/Star"), pos, c2 * 0.5f, _fase * 2f, 26f / 16f);
            // 6 — EL PULSO DEL BORDE
            Capa(sb, Tex("GlowCircleWhite"), pos, c1 * (0.10f + 0.06f * MathF.Sin(_fase * 3f)),
                _fase, 190f / 64f);
        }

        // === LA ESTELA (8 capas): las bolas del rastro · el rastro en
        //     rayos · el halo · el cuerpo · el núcleo · la estrella de
        //     la cabeza · LA HOJA CRECIENTE (la firma) · las chispas
        //     que suelta. ===
        private void VestirEstela(SpriteBatch sb)
        {
            Color c1 = Tinta, c2 = Tinta2;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D rayo = Tex("GlowRay");
            // 1 — LAS BOLAS DEL RASTRO (los nodos que se apagan)
            for (int i = 3; i < _estela.Length; i += 4)
            {
                float t = 1f - i / (float)_estela.Length;
                Capa(sb, Tex("GlowOrb"), _estela[i] - Main.screenPosition,
                    c1 * (0.22f * t), Projectile.rotation, (30f * t + 8f) / 128f);
            }
            // 2 — EL RASTRO EN RAYOS (el rastro clásico de la casa)
            for (int i = _estela.Length - 1; i > 0; i--)
            {
                float t = 1f - i / (float)_estela.Length;
                Vector2 p = _estela[i] - Main.screenPosition;
                DibujarRayo(sb, rayo, p, Projectile.rotation, 88f,
                    26f * t + 6f, c1 * (0.30f * t));
            }
            // 3 — EL HALO · 4 — EL CUERPO · 5 — EL NÚCLEO
            DibujarRayo(sb, rayo, pos, Projectile.rotation, 88f, 40f, c2 * 0.5f);
            DibujarRayo(sb, rayo, pos, Projectile.rotation, 88f, 20f, c1 * 0.95f);
            DibujarRayo(sb, rayo, pos, Projectile.rotation, 88f, 8f, Color.White);
            // 6 — LA ESTRELLA DE LA CABEZA
            Capa(sb, Tex("FlareEstrella4Puntas"), pos, c1, Projectile.rotation + _fase * 2f, 34f / 256f);
            // 7 — LA HOJA CRECIENTE (la firma del plato — gira con la hoja)
            Capa(sb, Tex("Procedural/Crescent"), pos, c1 * 0.95f, Projectile.rotation, 30f / 64f);
            // 8 — LAS CHISPAS QUE SUELTA
            Vector2 atras = new(-MathF.Cos(Projectile.rotation), -MathF.Sin(Projectile.rotation));
            for (int i = 0; i < 2; i++)
            {
                Vector2 p = pos + atras * (16f + Azar(i) * 10f) +
                    new Vector2(-atras.Y, atras.X) * ((Azar(i + 5) * 2f - 1f) * 8f);
                Capa(sb, Tex("SparkleStar"), p, c2 * (0.25f + 0.4f * Titilar(i, 4.6f)),
                    Projectile.rotation, (10f + 8f * Titilar(i, 4.6f)) / 32f);
            }
        }

        // === LA ZONA (8 capas): el velo del suelo · el charco · el
        //     interior girando · el aro rúnico · el FireRing pulsante
        //     del borde · los destellos internos · la estrella del
        //     centro · LAS MOTES QUE SUBEN. ===
        private void VestirZona(SpriteBatch sb)
        {
            Color c1 = Tinta, c2 = Tinta2;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float radio = 120f + Variante * 16f;
            float late = 0.85f + 0.15f * MathF.Sin(_fase * 3f);
            // 1 — EL VELO DEL SUELO · 2 — EL CHARCO · 3 — EL INTERIOR
            CapaSuelo(sb, Ruido(_semilla, 2.8f), pos, c1 * (0.12f * late), _fase * 0.18f, radio * 2f, radio * 0.95f);
            CapaSuelo(sb, Tex("GlowCircleWhite"), pos, c1 * (0.30f * late), 0f, radio * 2f, radio * 0.9f);
            CapaSuelo(sb, Tex("GlowCircleWhite"), pos, c2 * (0.22f * late), _fase, radio * 1.4f, radio * 0.68f);
            // 4 — EL ARO RÚNICO del perímetro
            CapaSuelo(sb, Tex("MagicRing"), pos, c2 * 0.28f, _fase * 1.3f, radio * 2.05f, radio * 0.95f);
            // 5 — EL FIRERING PULSANTE (las brasas del borde)
            CapaSuelo(sb, Tex("Procedural/FireRing"), pos, c1 * (0.16f + 0.08f * late),
                -_fase * 0.9f, radio * 2.15f * (1f + 0.05f * late), radio * 1.05f);
            // 6 — LOS DESTELLOS INTERNOS
            for (int i = 0; i < 4; i++)
            {
                Vector2 p = pos + new Vector2((Azar(i) * 2f - 1f) * radio * 0.72f,
                    (Azar(i + 4) * 2f - 1f) * radio * 0.34f);
                Capa(sb, Tex("SparkleStar"), p, c1 * (0.22f + 0.4f * Titilar(i, 3.4f)),
                    Azar(i) * 6f, (12f + 10f * Titilar(i, 3.4f)) / 32f);
            }
            // 7 — LA ESTRELLA DEL CENTRO
            Capa(sb, Tex("Procedural/Star"), pos, c2 * 0.30f, _fase * 2.5f, 24f / 16f);
            // 8 — LAS MOTES QUE SUBEN (el humo del conjuro)
            for (int i = 0; i < 3; i++)
            {
                float sube = (Main.GlobalTimeWrappedHourly * 52f + Azar(i) * 80f) % 80f;
                Vector2 p = new(pos.X + (Azar(i) * 2f - 1f) * radio * 0.75f, pos.Y - sube);
                Capa(sb, Tex("GlowOrb"), p, c1 * (0.30f * (1f - sube / 80f) + 0.06f),
                    0f, 12f / 128f);
            }
        }

        // === LA HOJA (6 capas): el halo · el cuerpo · el núcleo blanco
        //     · LA HOJA CRECIENTE que gira · su rastro · la chispa. ===
        private void VestirHoja(SpriteBatch sb)
        {
            Color c1 = Tinta, c2 = Tinta2;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            // 1 — EL HALO · 2 — EL CUERPO · 3 — EL NÚCLEO
            Capa(sb, Tex("GlowOrb"), pos, c2 * 0.35f, Projectile.rotation, 64f / 128f);
            DibujarRayo(sb, Tex("GlowRay"), pos, Projectile.rotation, 62f, 26f, c2 * 0.5f);
            DibujarRayo(sb, Tex("GlowRay"), pos, Projectile.rotation, 62f, 12f, c1);
            DibujarRayo(sb, Tex("GlowRay"), pos, Projectile.rotation, 62f, 5f, Color.White * 0.9f);
            // 4 — LA HOJA CRECIENTE (la firma — gira al caer)
            Capa(sb, Tex("Procedural/Crescent"), pos, c1 * 0.95f, Projectile.rotation, 30f / 64f);
            // 5 — SU RASTRO (la cinta que deja)
            Vector2 atras = new(-MathF.Cos(Projectile.rotation), -MathF.Sin(Projectile.rotation));
            CapaSuelo(sb, Tex("TrailGlow"), pos + atras * 20f, c2 * 0.40f,
                Projectile.rotation, 56f, 18f);
            // 6 — LA CHISPA de la punta
            Capa(sb, Tex("SparkleStar"), pos - atras * 24f, c2 * (0.3f + 0.4f * Titilar(2, 5f)),
                Projectile.rotation, (12f + 8f * Titilar(2, 5f)) / 32f);
        }

        // === EL LUCERO (7 capas): el velo de ruido · el halo · LA
        //     ESTRELLA que gira (la firma) · el corazón blanco · el
        //     destello · las chispas del rastro. ===
        private void VestirLucero(SpriteBatch sb)
        {
            Color c1 = Tinta, c2 = Tinta2;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            // 1 — EL VELO · 2 — EL HALO
            Capa(sb, Ruido(_semilla, 6.5f), pos, c1 * 0.13f, _fase * 3f, 66f / 512f);
            Capa(sb, Tex("GlowOrb"), pos, c1 * 0.5f, Projectile.rotation, 88f / 128f);
            // 3 — LA ESTRELLA QUE GIRA (la firma del lucero)
            Capa(sb, Tex("Procedural/Star"), pos, c1, _fase * 3.5f, 44f / 16f);
            // 4 — EL CORAZÓN BLANCO
            Capa(sb, Tex("GlowOrb"), pos, Color.White * 0.85f, 0f, 34f / 128f);
            // 5 — EL DESTELLO (la cruz que titila)
            Capa(sb, Tex("FlareEstrella4Puntas"), pos, c2 * (0.4f + 0.35f * Titilar(0, 5.2f)),
                _fase * 2.2f, (40f + 16f * Titilar(0, 5.2f)) / 256f);
            // 6..7 — LAS CHISPAS DEL RASTRO (la polvareda estelar)
            Vector2 atras = -Vector2.Normalize(Projectile.velocity);
            if (float.IsNaN(atras.X)) atras = Vector2.Zero;
            for (int i = 0; i < 2; i++)
            {
                Vector2 p = pos + atras * (14f + i * 12f + Azar(i) * 6f);
                Capa(sb, Tex("SparkleStar"), p, c2 * (0.28f + 0.4f * Titilar(i + 2, 5f)),
                    Projectile.rotation, (11f + 8f * Titilar(i + 2, 5f)) / 32f);
            }
        }

        /// <summary>UN rayo: la textura estirada del origen hacia el
        /// ángulo (una capa — un draw call).</summary>
        private static void DibujarRayo(SpriteBatch sb, Texture2D tex, Vector2 origen,
            float angulo, float largo, float grosor, Color color)
        {
            if (largo <= 1f || color.A == 0) return;
            Vector2 escala = new(largo / tex.Width, grosor / tex.Height);
            sb.Draw(tex, origen, null, color, angulo,
                new Vector2(0f, tex.Height * 0.5f), escala, SpriteEffects.None, 0f);
        }
    }
}
