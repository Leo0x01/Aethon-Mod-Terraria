using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

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
        //  EL DIBUJO (todo por capas de las texturas glow de la casa)
        // ================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.gameMenu) return false;
            SpriteBatch sb = Main.spriteBatch;
            Color c1 = Tinta, c2 = Tinta2;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            int est = Estilo;
            int vivido;

            switch (est)
            {
                case ESTILO_HAZ:
                {
                    Player dueno = Main.player[Projectile.owner];
                    if (dueno == null || !dueno.active) break;
                    float amp = 0.35f + Variante * 0.10f;
                    float ang = (float)Math.Atan2(_ancla.Y - Projectile.Center.Y,
                        _ancla.X - Projectile.Center.X) + MathF.Sin(_fase * 2.2f) * amp;
                    float largo = 640f;
                    Texture2D rayo = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/GlowRay").Value;
                    Texture2D estrella = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/FlareEstrella4Puntas").Value;
                    Vector2 origen = dueno.MountedCenter - Main.screenPosition;
                    float frac = Math.Min(1f, (90 - Projectile.timeLeft) / 12f);
                    float largoVivo = largo * frac;
                    // LAS TRES CAPAS: núcleo blanco, cuerpo tintado, halo tenue.
                    DibujarRayo(sb, rayo, origen, ang, largoVivo, 54f, c2 * 0.55f);
                    DibujarRayo(sb, rayo, origen, ang, largoVivo, 30f, c1 * 0.85f);
                    DibujarRayo(sb, rayo, origen, ang, largoVivo, 12f, Color.White);
                    sb.Draw(estrella, origen, null, c2, _fase * 3f,
                        estrella.Size() * 0.5f, 0.85f, SpriteEffects.None, 0f);
                    break;
                }
                case ESTILO_CORONA:
                {
                    Player dueno = Main.player[Projectile.owner];
                    if (dueno == null || !dueno.active) break;
                    Texture2D orbe = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/GlowOrb").Value;
                    int n = 3 + Variante;
                    float radio = 92f + Variante * 9f;
                    Vector2 basePos = dueno.MountedCenter - Main.screenPosition;
                    for (int i = 0; i < n; i++)
                    {
                        float a = _fase * 1.9f + i * MathHelper.TwoPi / n;
                        Vector2 p = basePos + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radio;
                        sb.Draw(orbe, p, null, c2 * 0.45f, a,
                            orbe.Size() * 0.5f, 1.1f, SpriteEffects.None, 0f);
                        sb.Draw(orbe, p, null, c1 * 0.9f, a,
                            orbe.Size() * 0.5f, 0.62f, SpriteEffects.None, 0f);
                        sb.Draw(orbe, p, null, Color.White * 0.9f, a,
                            orbe.Size() * 0.5f, 0.26f, SpriteEffects.None, 0f);
                    }
                    break;
                }
                case ESTILO_LLUVIA:
                {
                    Texture2D circulo = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/GlowCircle" + Sufijo(c1)).Value;
                    Vector2 suelo = new Vector2(pos.X, _ancla.Y - Main.screenPosition.Y);
                    sb.Draw(circulo, suelo, null, c1 * 0.16f, 0f,
                        circulo.Size() * 0.5f, new Vector2(2.3f, 0.55f),
                        SpriteEffects.None, 0f);
                    break;
                }
                case ESTILO_NOVA:
                {
                    vivido = 70 - Projectile.timeLeft;
                    Texture2D circulo = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/GlowCircle" + Sufijo(c1)).Value;
                    Texture2D orbe = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/GlowOrb").Value;
                    if (vivido < 20)
                    {
                        float carga = vivido / 20f;
                        sb.Draw(orbe, pos, null, c1 * (0.35f + carga * 0.5f), _fase * 4f,
                            orbe.Size() * 0.5f, 0.4f + carga * 0.8f, SpriteEffects.None, 0f);
                    }
                    else
                    {
                        float radio = (vivido - 20) * 17f;
                        float vida = 1f - (vivido - 20) / 50f;
                        if (vida > 0f)
                        {
                            sb.Draw(circulo, pos, null, c1 * (0.75f * vida), 0f,
                                circulo.Size() * 0.5f, new Vector2(radio / 64f, radio / 64f),
                                SpriteEffects.None, 0f);
                            sb.Draw(circulo, pos, null, Color.White * (0.55f * vida), 0f,
                                circulo.Size() * 0.5f, new Vector2(radio * 0.8f / 64f, radio * 0.8f / 64f),
                                SpriteEffects.None, 0f);
                        }
                    }
                    break;
                }
                case ESTILO_VORTICE:
                {
                    Texture2D orbe = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/GlowOrb").Value;
                    Texture2D circulo = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/GlowCircle" + Sufijo(c2)).Value;
                    sb.Draw(circulo, pos, null, c2 * 0.30f, _fase,
                        circulo.Size() * 0.5f, 3.6f, SpriteEffects.None, 0f);
                    // LOS BRAZOS DE LA ESPIRAL (tres, girando).
                    for (int brazo = 0; brazo < 3; brazo++)
                    {
                        for (int paso = 1; paso <= 7; paso++)
                        {
                            float a = _fase * 2.4f + brazo * MathHelper.TwoPi / 3f +
                                paso * 0.55f;
                            float r = paso * 26f;
                            Vector2 p = pos + new Vector2(MathF.Cos(a), MathF.Sin(a)) * r;
                            float esc = 0.55f - paso * 0.06f;
                            sb.Draw(orbe, p, null, c1 * (0.65f - paso * 0.07f), a,
                                orbe.Size() * 0.5f, esc, SpriteEffects.None, 0f);
                        }
                    }
                    // EL CORAZÓN NEGRO.
                    sb.Draw(orbe, pos, null, new Color(12, 8, 20) * 0.85f, -_fase * 3f,
                        orbe.Size() * 0.5f, 1.15f, SpriteEffects.None, 0f);
                    break;
                }
                case ESTILO_ENJAMBRE:
                {
                    Texture2D circulo = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/GlowCircle" + Sufijo(c1)).Value;
                    sb.Draw(circulo, pos, null, c1 * 0.12f, _fase,
                        circulo.Size() * 0.5f, new Vector2(1.7f, 1.7f),
                        SpriteEffects.None, 0f);
                    break;
                }
                case ESTILO_ESTELA:
                {
                    Texture2D rayo = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/GlowRay").Value;
                    // LA ESTELA (las posiciones viejas, deshaciéndose).
                    for (int i = _estela.Length - 1; i > 0; i--)
                    {
                        float t = 1f - i / (float)_estela.Length;
                        Vector2 p = _estela[i] - Main.screenPosition;
                        sb.Draw(rayo, p, null, c1 * (0.30f * t), Projectile.rotation,
                            rayo.Size() * 0.5f, new Vector2(0.7f, 0.28f + t * 0.2f),
                            SpriteEffects.None, 0f);
                    }
                    // LA HOJA (tres capas al ras).
                    DibujarRayo(sb, rayo, pos, Projectile.rotation, 88f, 40f, c2 * 0.5f);
                    DibujarRayo(sb, rayo, pos, Projectile.rotation, 88f, 20f, c1 * 0.95f);
                    DibujarRayo(sb, rayo, pos, Projectile.rotation, 88f, 8f, Color.White);
                    break;
                }
                case ESTILO_ZONA:
                {
                    Texture2D circulo = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/GlowCircle" + Sufijo(c1)).Value;
                    float radio = 120f + Variante * 16f;
                    float late = 0.85f + 0.15f * MathF.Sin(_fase * 3f);
                    // EL CHARCO (achatado: es un suelo).
                    sb.Draw(circulo, pos, null, c1 * (0.30f * late), 0f,
                        circulo.Size() * 0.5f, new Vector2(radio / 64f, radio * 0.45f / 64f),
                        SpriteEffects.None, 0f);
                    sb.Draw(circulo, pos, null, c2 * (0.22f * late), _fase,
                        circulo.Size() * 0.5f, new Vector2(radio * 0.7f / 64f, radio * 0.34f / 64f),
                        SpriteEffects.None, 0f);
                    break;
                }
                case ESTILO_HOJA:
                {
                    Texture2D rayo = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/GlowRay").Value;
                    DibujarRayo(sb, rayo, pos, Projectile.rotation, 62f, 26f, c2 * 0.5f);
                    DibujarRayo(sb, rayo, pos, Projectile.rotation, 62f, 12f, c1);
                    break;
                }
                case ESTILO_LUCERO:
                {
                    Texture2D orbe = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/GlowOrb" + Sufijo(c1)).Value;
                    sb.Draw(orbe, pos, null, c1 * 0.5f, Projectile.rotation,
                        orbe.Size() * 0.5f, 1.1f, SpriteEffects.None, 0f);
                    sb.Draw(orbe, pos, null, Color.White * 0.85f, Projectile.rotation,
                        orbe.Size() * 0.5f, 0.42f, SpriteEffects.None, 0f);
                    break;
                }
            }
            return false;   // TODO lo dibuja la casa (sin el vanilla)
        }

        /// <summary>UN rayo: la textura estirada del origen hacia el
        /// ángulo (una capa — un draw call).</summary>
        private static void DibujarRayo(SpriteBatch sb, Texture2D tex, Vector2 origen,
            float angulo, float largo, float grosor, Color color)
        {
            if (largo <= 1f || color.A == 0) return;
            Vector2 escala = new Vector2(largo / tex.Width, grosor / tex.Height);
            sb.Draw(tex, origen, null, color, angulo,
                new Vector2(0f, tex.Height * 0.5f), escala, SpriteEffects.None, 0f);
        }

        /// <summary>El SUFIJO de la textura tintada más cercana (la casa
        /// tiene glows por color: Gold/Cyan/Purple/Magenta/Green/White).</summary>
        private static string Sufijo(Color c)
        {
            if (c.R > c.B + 40 && c.R > c.G + 20) return "Gold";
            if (c.G > c.R + 20 && c.B > 150) return "Cyan";
            if (c.R > 120 && c.B > c.G + 40) return "Purple";
            if (c.R > 200 && c.B > 200 && c.G < 140) return "Magenta";
            if (c.G > c.R + 40 && c.G > c.B + 40) return "Green";
            return "White";
        }
    }
}
