using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Jefes
{
    /// <summary>
    /// AtaqueFusionProjectile — v6.50.117 — LOS DIENTES DEL RECUERDO
    /// COMPLETO (las armas nuevas de la fusión, la letra del usuario
    /// punto por punto). TRES estilos — y las primitivas MASIVAS de
    /// lluvia (pernos, columnas, novas) se REUSAN de
    /// <see cref="AtaqueJefeProjectile"/>: aquí solo vive lo que
    /// NINGÚN diente del dios sabía hacer.
    ///
    ///   0 · EL SOL MORIBUNDO (b): un sol negro-morado GIGANTE (×1.8 la
    ///     talla del sol del dios) que avanza LENTO (~6 px/t) y ATRAE al
    ///     jugador (<900 px: cada tick velocity += dirección·0.06 — el
    ///     tirón vive en TraccionRecuerdoPlayer, el cauce MP correcto:
    ///     mover al jugador es cosa de SU máquina). Dura ~9 s y se
    ///     disipa en NOVA. «Castiga la calma, no el reflejo.»
    ///
    ///   1 · EL LÁSER DE LA REJILLA (c): la barra bicolor de la REJILLA
    ///     LÁSER — horizontal (ai[1] 0-2) o vertical (ai[1] 3-5), con su
    ///     COLOR fijo por paridad del índice (par = ORO, impar =
    ///     OSCURIDAD). El color ACTIVO alterna cada 70 t por el reloj
    ///     GLOBAL de la máquina (todas las barras y todas las pantallas
    ///     alternan JUNTAS): el activo ARDE y duele, el inactivo se
    ///     dibuja TENUE — el aviso legible de lo que viene.
    ///
    ///   2 · LA PÁGINA DE TINTA (d): el rectángulo girando de la LLUVIA
    ///     DE PÁGINAS — cae con vaivén de papel, estela morada y TRES
    ///     LÍNEAS de escritura; contra el suelo SE ROMPE en tinta.
    ///
    ///   3 · EL PERNO DEL CONCILIO (v6.50.120 — Lunatic Cultist · el
    ///     ritual que CONVERGE): nace en el CÍRCULO alrededor de donde
    ///     estaba el jugador, con el rumbo YA CLAVADO al centro; CARGA
    ///     55 t BRILLANDO gordo y TENUE (la regla de legibilidad — lo
    ///     que aún no duele se ve tenue, como el aviso de la Rejilla) y
    ///     a los 55 t VUELA al punto donde estabas: el dodge legible es
    ///     SALIR del círculo antes del cierre.
    ///
    /// REGLAS DE LA CASA (heredadas del arsenal): daño por COLISIÓN del
    /// motor, cero Main.rand en el render, el contrato de lote
    /// cerrado→cerrado→vanilla, y LA AUTOCURACIÓN de talla/vida en la
    /// IA (el msg 27 no lleva OnSpawn — la lección del sol del dios).
    /// </summary>
    public class AtaqueFusionProjectile : ModProjectile
    {
        // === LOS ESTILOS (ai[0]) ===
        public const int EstiloSolMoribundo = 0;
        public const int EstiloRejillaLaser = 1;
        public const int EstiloPaginaTinta = 2;
        // v6.50.120 — EL PERNO RITUAL CONVERGENTE (Lunatic Cultist · el
        // ritual): nace de AVISO en el círculo, CARGA quieto 55 t y a los
        // 55 t VUELA al centro donde estabas — el dodge legible de
        // siempre: SALIR del círculo antes del cierre.
        public const int EstiloRitualConvergente = 3;

        /// <summary>El estilo del diente (ai[0]).</summary>
        private int Estilo => (int)Projectile.ai[0];
        /// <summary>Parámetro libre (el índice de la rejilla · el daño del sol).</summary>
        private float Par => Projectile.ai[1];
        /// <summary>Semilla determinista (ai[2]).</summary>
        private int Seed => Math.Max(1, (int)Projectile.ai[2] % 9973);

        // === LA PALETA DE LA FUSIÓN (los dos colores que se abrazaron) ===
        private static readonly Color OroLuz = new(255, 220, 120);
        private static readonly Color Oscuridad = new(90, 20, 140);
        private static readonly Color NegroMorado = new(45, 12, 70);
        private static readonly Color NucleoMorado = new(160, 100, 225);

        /// <summary>La edad del diente (cada máquina cuenta la suya — la autocuración de la casa).</summary>
        private float _edad;
        /// <summary>La nova del sol moribundo ya sonó (una sola vez).</summary>
        private bool _novaHecha;
        /// <summary>v6.50.120 — el perno del concilio ya SOLTÓ su vuelo
        /// (la descarga suena UNA sola vez por máquina).</summary>
        private bool _ritualLanzado;

        public override string Texture => "AethonMod/Content/Projectiles/Cosmetic/AnillosSingularesHalo";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;      // el paraguas máximo; cada estilo muere por SU reloj (_edad)
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0f;
            // v6.50.1 — los que entren a media pelea reciben las armas de la fusión.
            Projectile.netImportant = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            // Vidas y tallas propias por estilo (la AUTORIDAD las pone aquí;
            // los remotos las AUTOCURAN en la IA — el msg 27 no lleva este
            // OnSpawn, la lección de la casa).
            switch (Estilo)
            {
                case EstiloSolMoribundo:
                    // GIGANTE DE NACIMIENTO: ×1.8 la talla del sol del dios
                    // (96 → 172) — LENTO y con ~9 s de agonía.
                    Projectile.width = 172;
                    Projectile.height = 172;
                    break;

                case EstiloRejillaLaser:
                    // LA BARRA: nace de AVISO (inofensiva — la regla de
                    // legibilidad: lo que aún no duele se ve tenue).
                    Projectile.hostile = false;
                    if (Par >= 3f) { Projectile.width = 30; Projectile.height = 1300; }
                    else { Projectile.width = 1300; Projectile.height = 30; }
                    break;

                case EstiloPaginaTinta:
                    // LA PÁGINA: cae girando y SE ROMPE contra el terreno.
                    Projectile.width = 28;
                    Projectile.height = 40;
                    Projectile.tileCollide = true;
                    break;

                case EstiloRitualConvergente:
                    // v6.50.120 — EL PERNO DEL CONCILIO: nace de AVISO
                    // (inofensivo — la regla de legibilidad: lo que aún
                    // no duele se ve tenue) y JAMÁS toca el terreno (vuela
                    // del círculo al centro por encima de todo).
                    Projectile.width = 30;
                    Projectile.height = 30;
                    Projectile.hostile = false;
                    Projectile.tileCollide = false;
                    break;
            }
        }

        public override void AI()
        {
            _edad++;

            switch (Estilo)
            {
                case EstiloSolMoribundo: SolTick(); break;
                case EstiloRejillaLaser: RejillaTick(); break;
                case EstiloPaginaTinta: PaginaTick(); break;
                case EstiloRitualConvergente: RitualTick(); break;
            }

            // El cable respira (el patrón del arsenal).
            if ((Main.GameUpdateCount % 15u) == 0u) Projectile.netUpdate = true;
        }

        // ==================================================================
        //  EL SOL MORIBUNDO — la caza lenta que ATRAE
        // ==================================================================
        private void SolTick()
        {
            // LA AUTOCURACIÓN de talla (el msg 27 no lleva OnSpawn).
            if (Projectile.width < 172)
            {
                Vector2 c = Projectile.Center;
                Projectile.Resize(172, 172);
                Projectile.Center = c;
            }

            // LA CAZA LENTA (la letra: «persiga lento»): ~6 px/t hacia la
            // presa, con el vaivén irregular de una estrella que muere.
            Player presa = Presa();
            if (presa != null)
            {
                Vector2 hacia = (presa.Center - Projectile.Center)
                    .SafeNormalize(Vector2.UnitY);
                Vector2 objetivo = hacia * 6f + new Vector2(
                    MathF.Sin(_edad * 0.031f + Seed) * 1.4f, 0f);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, objetivo, 0.035f);
            }
            else Projectile.velocity *= 0.96f;
            Projectile.rotation += 0.012f;

            // LA BRASA MORADA (débil, irregular — el pulso del moribundo).
            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(150f, 150f),
                    DustID.PurpleTorch, -Projectile.velocity * 0.04f, 140, Oscuridad, 1.3f);
                d.noGravity = true;
            }

            // LA LUZ DE LA BRASA (el sol moribundo NO alumbra el mundo:
            // solo la brasa violeta que lo hace legible de noche).
            Lighting.AddLight(Projectile.Center, 0.30f, 0.10f, 0.50f);

            // LA NOVA FINAL (~9 s): el sol se DISIPA en un anillo de pernos
            // oscuros — la estrella muerta escupe su última oscuridad.
            if (_edad >= 540f && !_novaHecha)
            {
                _novaHecha = true;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, Projectile.Center);
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item122.WithPitchOffset(-0.3f), Projectile.Center);
                OndaLib.Kick(12f, 26);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 18; i++)
                    {
                        float ang = i * MathHelper.TwoPi / 18f;
                        Vector2 v = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 8.5f;
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                            Projectile.Center.X, Projectile.Center.Y, v.X, v.Y,
                            ModContent.ProjectileType<AtaqueJefeProjectile>(),
                            Math.Max(1, (int)(Projectile.damage * 0.55f)), 2f, Main.myPlayer,
                            AtaqueJefeProjectile.EstiloPernoEstelar, 2f, i * 17 + Seed);
                    }
                }
                Projectile.Kill();
            }
        }

        // ==================================================================
        //  EL LÁSER DE LA REJILLA — la barra bicolor que se enciende
        // ==================================================================
        private void RejillaTick()
        {
            // LA AUTOCURACIÓN de talla.
            bool vert = Par >= 3f;
            int w = vert ? 30 : 1300;
            int h = vert ? 1300 : 30;
            if (Projectile.width != w || Projectile.height != h)
            {
                Vector2 c = Projectile.Center;
                Projectile.Resize(w, h);
                Projectile.Center = c;
            }
            if (Projectile.timeLeft > 600) Projectile.timeLeft = 600;

            // EL CICLO BICOLOR: 50 t de AVISO (todas tenues) y desde ahí el
            // color ACTIVO alterna cada 70 t — por el RELOJ GLOBAL de la
            // máquina (todas las barras, en todas las pantallas, a la vez).
            bool aviso = _edad <= 50f;
            bool oroActivo = ((Main.GameUpdateCount / 70u) & 1u) == 0u;
            bool miColorEsOro = (Par % 2f) < 0.5f;   // índice PAR = ORO · IMPAR = OSCURIDAD
            bool activa = !aviso && oroActivo == miColorEsOro && _edad < 400f;

            Projectile.hostile = activa;
            Projectile.velocity = Vector2.Zero;

            // LAS CHISPAS de la barra encendida (a lo largo de TODO el filo).
            if (activa && !Main.dedServ && Main.rand.NextBool(4))
            {
                float f = Main.rand.NextFloat(-0.5f, 0.5f);
                Vector2 p = vert
                    ? Projectile.Center + new Vector2(0f, f * 1300f)
                    : Projectile.Center + new Vector2(f * 1300f, 0f);
                Dust d = Dust.NewDustPerfect(p,
                    miColorEsOro ? DustID.GoldFlame : DustID.PurpleTorch,
                    Vector2.Zero, 140, miColorEsOro ? OroLuz : Oscuridad, 1.1f);
                d.noGravity = true;
            }

            if (_edad >= 450f) Projectile.Kill();
        }

        // ==================================================================
        //  LA PÁGINA DE TINTA — el papel que cae girando
        // ==================================================================
        private void PaginaTick()
        {
            // LA CAÍDA: gravedad suave + el vaivén del papel, girando
            // siempre (rectángulos de tinta con estela morada).
            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.035f, 7.5f);
            Projectile.velocity.X = MathF.Sin(_edad * 0.045f + Seed) * 1.6f;
            Projectile.rotation += 0.10f + 0.02f * (Seed % 5);

            // LA ESTELA MORADA (la tinta que va dejando al caer).
            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                    -Projectile.velocity * 0.05f, 120, Oscuridad, 0.9f);
                d.noGravity = true;
            }

            if (_edad >= 460f) Projectile.Kill();
        }

        // ==================================================================
        //  v6.50.120 — EL PERNO RITUAL CONVERGENTE — el concilio que
        //  cierra (Lunatic Cultist · el ritual: el círculo anclado
        //  alrededor del jugador que CONVERGE al punto donde estabas)
        // ==================================================================
        private void RitualTick()
        {
            // LA AUTOCURACIÓN de talla (el msg 27 no lleva OnSpawn — la
            // lección del sol del dios).
            if (Projectile.width != 30 || Projectile.height != 30)
            {
                Vector2 c = Projectile.Center;
                Projectile.Resize(30, 30);
                Projectile.Center = c;
            }

            // LA FASE DE CARGA (<55 t): el perno BRILLA quieto — DESHACE
            // su propio avance (telegrafía: el ritual acumulando su
            // sentencia) mientras el polvo de SU color CONVERGE a él
            // desde alrededor (la paridad del índice: par ORO, impar
            // OSCURIDAD).
            if (_edad < 55f)
            {
                Projectile.position -= Projectile.velocity;

                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    bool oro = (Par % 2f) < 0.5f;
                    Vector2 alrededor = Projectile.Center + Main.rand.NextVector2Circular(64f, 64f);
                    Vector2 hacia = (Projectile.Center - alrededor).SafeNormalize(Vector2.UnitX) *
                        Main.rand.NextFloat(1.5f, 3.5f);
                    Dust d = Dust.NewDustPerfect(alrededor,
                        oro ? DustID.GoldFlame : DustID.PurpleTorch,
                        hacia, 150, oro ? OroLuz : Oscuridad, 1.0f);
                    d.noGravity = true;
                }
            }
            else if (!_ritualLanzado)
            {
                // LA DESCARGA (t=55): el perno VUELA al centro donde
                // ESTABAS — el círculo entero se cierra de golpe (los
                // ocho a la vez: el cierre del concilio).
                _ritualLanzado = true;
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item122.WithPitchOffset(0.4f), Projectile.Center);
                OndaLib.Kick(3f, 8);
                Projectile.netUpdate = true;
            }

            // La hostilidad se RECONSTRUYE del reloj en CADA máquina (el
            // msg 27 no la lleva entera — la lección del pilar): la carga
            // es AVISO, el vuelo es FILO.
            Projectile.hostile = _edad >= 55f;

            // LA PENUMBRA VIOLETA del perno (legible aun dentro de la
            // noche del eclipse — el Recuerdo Completo ya no alumbra el
            // mundo, sus armas tienen que alumbrarse solas).
            Lighting.AddLight(Projectile.Center, 0.4f, 0.25f, 0.6f);

            if (_edad > 240f) Projectile.Kill();
        }

        /// <summary>LA PÁGINA SE ROMPE CONTRA EL SUELO: se deshace en tinta.</summary>
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!Main.dedServ)
            {
                for (int i = 0; i < 7; i++)
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                        new Vector2(Main.rand.NextFloat(-2.5f, 2.5f),
                            Main.rand.NextFloat(-3.5f, -0.5f)),
                        140, new Color(120, 60, 180), 1.0f);
                    d.noGravity = true;
                }
            }
            return true;   // muere contra el terreno
        }

        /// <summary>La presa más cercana (el patrón del arsenal: el diente
        /// caza a quien tiene MÁS CERCA — en MP cualquiera puede serlo).</summary>
        private Player Presa()
        {
            Player mejor = null;
            float mejorD = float.MaxValue;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p == null || !p.active || p.dead) continue;
                float d = Vector2.DistanceSquared(p.Center, Projectile.Center);
                if (d < mejorD) { mejorD = d; mejor = p; }
            }
            return mejor;
        }

        // ==================================================================
        //  EL ARTE — 100% código (la técnica de la casa)
        // ==================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 pos = Projectile.Center - Main.screenPosition;

                // === FASE 1 — EL CUERPO DE TINTA (coords de MUNDO en el
                //     búfer de la casa: el rectángulo girando de la página
                //     con SUS líneas de escritura) ===
                if (Estilo == EstiloPaginaTinta)
                {
                    // v6.51.1 — LA CURA DE LOS CUADRADOS (la letra: «hay
                    // un ataque que parece que lanza algo pero el sprite
                    // está mal y solo se ven cuadrados»): el cuerpo de la
                    // página era Pixel-rects duros (rectángulos LISOS de
                    // 1×1 estirados — LITERALES cuadrados). Ahora es una
                    // página de sombra: cuerpo y rim con SoftGlow (bordes
                    // difusos) y las líneas de escritura como trazos
                    // suaves — cero esquinas.
                    VFXCore.Begin();
                    try
                    {
                        Color cTinta = new(70, 30, 112);
                        Color cBorde = new(140, 80, 205);
                        // EL RIM (el borde de la hoja — tinta clara difusa).
                        VFXCore.Quad(Projectile.Center, cBorde * 0.30f,
                            new Vector2(46f, 58f), Projectile.rotation, VFXCore.SoftGlow);
                        // EL CUERPO (la hoja de tinta).
                        VFXCore.Quad(Projectile.Center, cBorde * 0.40f,
                            new Vector2(40f, 52f), Projectile.rotation, VFXCore.SoftGlow);
                        VFXCore.Quad(Projectile.Center, cTinta * 0.85f,
                            new Vector2(30f, 42f), Projectile.rotation, VFXCore.SoftGlow);
                        // LAS TRES LÍNEAS DE ESCRITURA (trazos suaves).
                        for (int l = 0; l < 3; l++)
                        {
                            Vector2 off = new Vector2(0f, (l - 1) * 9f)
                                .RotatedBy(Projectile.rotation);
                            VFXCore.Quad(Projectile.Center + off, cBorde * 0.42f,
                                new Vector2(18f, 4f), Projectile.rotation, VFXCore.SoftGlow);
                        }
                    }
                    finally { VFXCore.FlushAdditive(); }
                }

                // === FASE 2 — EL LOTE ADITIVO DE PANTALLA ===
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    switch (Estilo)
                    {
                        // === EL SOL MORIBUNDO: el cuerpo negro-morado con
                        //     su pulso agonizante, la rueda de rayos APAGADOS
                        //     y EL ANILLO DE LA ATRACCIÓN (el aro que AVISA
                        //     el radio del tirón — 880 px, la letra de la
                        //     tracción) ===
                        case EstiloSolMoribundo:
                        {
                            float agonía = 0.72f + 0.28f * MathF.Sin(t * 1.7f) *
                                MathF.Sin(t * 0.6f + 1f);   // late DESPACIO e irregular
                            LumenLib.Bloom(Main.spriteBatch, pos, 300f, NegroMorado, 0.9f * agonía, 4);
                            LumenLib.Bloom(Main.spriteBatch, pos, 190f, Oscuridad, 0.95f * agonía, 3);
                            LumenLib.BloomPulse(Main.spriteBatch, pos, 110f, NucleoMorado,
                                0.75f * agonía, t, 1.7f);
                            // LA RUEDA APAGADA (la corona que fue, girando lenta).
                            Vector2 origenS = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height) * 0.5f;
                            for (int i = 0; i < 10; i++)
                            {
                                float ang = Projectile.rotation + i * MathHelper.TwoPi / 10f;
                                float largo = 330f + 90f * MathF.Sin(t * 1.1f + i * 1.3f);
                                Main.spriteBatch.Draw(VFXCore.SoftGlow, pos, null,
                                    Oscuridad * (0.30f * agonía), ang, origenS,
                                    new Vector2(largo / VFXCore.SoftGlow.Width * 2f,
                                                26f / VFXCore.SoftGlow.Height * 2f),
                                    SpriteEffects.None, 0f);
                            }
                            // EL ANILLO DE LA ATRACCIÓN (la marca del tirón).
                            Vector2 ringTam = new Vector2(VFXCore.Ring.Width, VFXCore.Ring.Height);
                            Main.spriteBatch.Draw(VFXCore.Ring, pos, null,
                                Oscuridad * (0.22f + 0.10f * MathF.Sin(t * 2.2f)), 0f,
                                ringTam * 0.5f, VFXCore.RingQuadSize(880f) / ringTam,
                                SpriteEffects.None, 0f);
                            break;
                        }

                        // === EL LÁSER DE LA REJILLA: la barra bicolor — el
                        //     color ACTIVO arde con su núcleo blanco y sus
                        //     broches; el INACTIVO se dibuja TENUE (el aviso
                        //     legible de lo que viene) ===
                        case EstiloRejillaLaser:
                        {
                            bool vert = Par >= 3f;
                            bool oro = (Par % 2f) < 0.5f;
                            bool aviso = _edad <= 50f;
                            bool oroActivo = ((Main.GameUpdateCount / 70u) & 1u) == 0u;
                            bool activa = !aviso && oroActivo == oro && _edad < 400f;
                            float fade = _edad > 400f
                                ? Math.Max(0f, 1f - (_edad - 400f) / 50f) : 1f;
                            float crecer = aviso ? 0.4f + 0.6f * (_edad / 50f) : 1f;
                            Color c = oro ? OroLuz : Oscuridad;
                            float alfa = (activa ? 0.85f : 0.16f) * fade * crecer;
                            float rot = vert ? MathHelper.PiOver2 : 0f;
                            const float len = 1300f;
                            float ancho = (activa ? 30f : 12f) + 6f * MathF.Sin(t * 6f);
                            Vector2 origen = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height) * 0.5f;

                            Main.spriteBatch.Draw(VFXCore.SoftGlow, pos, null, c * alfa, rot, origen,
                                new Vector2(len / VFXCore.SoftGlow.Width,
                                            ancho / VFXCore.SoftGlow.Height),
                                SpriteEffects.None, 0f);
                            // EL NÚCLEO BLANCO de la barra activa (el filo que mata).
                            if (activa)
                                Main.spriteBatch.Draw(VFXCore.SoftGlow, pos, null,
                                    Color.White * (alfa * 0.55f), rot, origen,
                                    new Vector2(len / VFXCore.SoftGlow.Width,
                                                6f / VFXCore.SoftGlow.Height),
                                    SpriteEffects.None, 0f);
                            // LOS BROCHES (las puntas del láser).
                            Vector2 dirL = new Vector2(MathF.Cos(rot), MathF.Sin(rot));
                            for (int e = -1; e <= 1; e += 2)
                                LumenLib.Bloom(Main.spriteBatch, pos + dirL * (len * 0.5f * e),
                                    activa ? 34f : 16f, c, alfa, 2);
                            break;
                        }

                        // === LA PÁGINA: la ESTELA MORADA (la tinta que deja
                        //     al caer — el cuerpo lo pinta la FASE 1) ===
                        case EstiloPaginaTinta:
                        {
                            for (int e = 1; e <= 3; e++)
                            {
                                Vector2 fantasma = pos - Projectile.velocity * (0.9f * e);
                                LumenLib.Bloom(Main.spriteBatch, fantasma, 14f - e * 3f,
                                    new Color(120, 60, 185), 0.35f / e, 2);
                            }
                            break;
                        }

                        // === v6.50.120 — EL PERNO DEL CONCILIO: la carga
                        //     BRILLA gorda y TENUE (el aviso legible de la
                        //     Rejilla — lo que aún no duele se ve tenue,
                        //     respirando) y al dispararse se AFILA en un
                        //     perno PLENO en la dirección del vuelo: oro o
                        //     violeta por paridad del índice, con su
                        //     núcleo blanco al volar (el filo que mata) ===
                        case EstiloRitualConvergente:
                        {
                            bool cargando = _edad < 55f;
                            bool oro = (Par % 2f) < 0.5f;
                            Color c = oro ? OroLuz : Oscuridad;
                            float fade = _edad > 210f
                                ? Math.Max(0f, 1f - (_edad - 210f) / 30f) : 1f;
                            float alfa = cargando
                                ? 0.26f + 0.12f * MathF.Sin(t * 9f)   // late juntando la sentencia
                                : 0.92f;                               // PLENO al volar
                            alfa *= fade;
                            float largo = cargando ? 92f : 74f;
                            float ancho = cargando ? 26f : 13f;
                            float rot = Projectile.velocity.ToRotation();
                            Vector2 origen = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height) * 0.5f;

                            Main.spriteBatch.Draw(VFXCore.SoftGlow, pos, null, c * alfa, rot, origen,
                                new Vector2(largo / VFXCore.SoftGlow.Width,
                                            ancho / VFXCore.SoftGlow.Height),
                                SpriteEffects.None, 0f);
                            // EL NÚCLEO BLANCO del perno disparado (el filo que mata).
                            if (!cargando)
                                Main.spriteBatch.Draw(VFXCore.SoftGlow, pos, null,
                                    Color.White * (0.60f * fade), rot, origen,
                                    new Vector2(34f / VFXCore.SoftGlow.Width,
                                                5f / VFXCore.SoftGlow.Height),
                                    SpriteEffects.None, 0f);
                            // LA CABEZA (el corazón del perno, latiendo).
                            LumenLib.Bloom(Main.spriteBatch, pos, cargando ? 30f : 20f, c,
                                (cargando ? 0.55f : 0.85f) * fade, 2);
                            break;
                        }
                    }
                }
                finally { Main.spriteBatch.End(); }
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            finally
            {
                // El estado de salida de PreDraw es SIEMPRE el lote de
                // vanilla (el contrato de la casa).
                VFXCore.CerrarLoteSiAbierto();
                VFXCore.ReabrirLoteVanilla();
            }
            return false;   // los dientes de la fusión se dibujan a sí mismos
        }
    }
}
