using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// EL EQUINOCCIO DE LA APATÍA (v6.51.2) — LA FUSIÓN DE LA LETRA:
    /// «quiero que conviertas el proyectil actual del grimorio eterno
    /// en una fusion con el tomo de la apatia nula, dejando el tema
    /// de luz y oscuridad del grimorio pero con el toque de la apatia
    /// nula».
    ///
    /// EL PROYECTIL VIEJO (los discos del Equinoccio, la pareja
    /// dorada/violeta del 931) MUERE — el Eterno ahora AZOTA con
    /// TENTÁCULOS: cada disparo sigue siendo UN DÍA Y UNA NOCHE (la
    /// pareja de siempre), pero su cuerpo es el del tentáculo cósmico
    /// de la apatía nula —
    ///
    ///   · LA GESTACIÓN (18 t): el tentáculo nace y FRENA escupiendo
    ///     PULSOS QUE IMPLODEN (el día: oro que se recoge · la noche:
    ///     VACÍO que oscurece en pase alfa) sin dañar a nadie.
    ///   · EL AZOTE (3 curvas): se dispara a la presa CURVANDO el
    ///     rumbo a cada tick (la media luna que barre el cielo) con
    ///     su CHISPAS de cola estiradas — y entre curva y curva el
    ///     SALTO: teleport + estallido de blooms + el sentido de giro
    ///     INVERTIDO.
    ///   · EL SEGUIMIENTO (la otra letra de la v6.51.2): simple y
    ///     lento por defecto; a partir del nivel 100 del grimorio
    ///     acelea, hasta MUY agresivo a nivel 1000
    ///     (WeaponScaling.HomingAcel/HomingTope).
    ///
    /// EL DÍA (ai[1]=0.64): cuerpo de sol — oro vivo, luz cálida, las
    /// chispas que BRILLAN. LA NOCHE (ai[1]=0.28): cuerpo de eclipse —
    /// negro-violeta, el vacío que OSCURECE su estela (pase alfa) con
    /// su rim de brasa fría.
    /// </summary>
    public class TentaculoEquinoccioProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Projectiles/Cosmetic/AnillosSingularesHalo";

        private ref float Time => ref Projectile.ai[0];

        /// <summary>¿EL DÍA o LA NOCHE? El matiz de la convención del
        /// Equinoccio (0.64 dorado · 0.28 violeta).</summary>
        private bool EsDia => Projectile.ai[1] >= 0.5f;

        // === LA GESTACIÓN (más corta que la del tomo: el Eterno no
        //     espera a nadie — pero el gesto queda: nace, respira,
        //     AZOTA). ===
        private const int T_GESTA = 18;

        // === EL AZOTE ===
        private const int CurvasMax = 3;
        private const int T_AZOTE = 46;      // ticks de vuelo por curva
        private const int T_PAUSA = 12;      // la recarga entre curvas

        private bool _enGestacion = true;
        private bool _volando = false;
        private int _curvaDir = 0;
        private int _curvasHechas = 0;
        private int _tickFase = 0;
        private float _escala = 1f;

        // === LA MEMORIA DEL CUERPO (la historia de posiciones — el
        //     tentáculo ES su cola: segmentos que se afilan). ===
        private const int HistoriaMax = 18;
        private readonly Vector2[] _historia = new Vector2[HistoriaMax];
        private int _nHistoria = 0;

        // === LAS PARTÍCULAS (los pulsos que imploden y las chispas —
        //     el toque de la apatía, curvas de la casa). ===
        private struct Pulso
        {
            public Vector2 Pos;
            public float Rot;
            public Color Color;
            public float Escala0;
            public int Nace;
            public int Vida;
            public bool Aditivo;
        }
        private struct Chispa
        {
            public Vector2 Pos;
            public float Rot;
            public Color Color;
            public float Escala0;
            public Vector2 Estira;
            public int Nace;
            public bool Aditivo;
        }
        private const int MaxPulsos = 48;
        private const int MaxChispas = 128;
        private readonly Pulso[] _pulsos = new Pulso[MaxPulsos];
        private readonly Chispa[] _chispas = new Chispa[MaxChispas];
        private int _nPulsos, _nChispas;

        // === LA PALETA (día y noche) ===
        private Color ColorNucleo => EsDia ? new Color(255, 238, 178) : new Color(178, 116, 255);
        private Color ColorCuerpo => EsDia ? new Color(255, 176, 66) : new Color(96, 40, 170);
        private Color ColorRim => EsDia ? new Color(255, 246, 205) : new Color(140, 78, 220);
        private Color ColorVacio => new Color(16, 7, 28);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.penetrate = -1;          // golpea TODO lo que toca
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.timeLeft = 600;          // techo de seguridad
            Projectile.netImportant = false;    // lo engendra el Shoot del arma
        }

        public override bool? CanDamage() => !_enGestacion;   // la gestación no daña (la ley del tomo)

        private void NacerPulso(Vector2 pos, Color color, float escala, int vida, bool aditivo)
        {
            if (_nPulsos >= MaxPulsos) return;
            _pulsos[_nPulsos++] = new Pulso
            {
                Pos = pos,
                Rot = Main.rand.NextFloat(-10f, 10f),
                Color = color,
                Escala0 = escala,
                Nace = (int)Main.GameUpdateCount,
                Vida = vida,
                Aditivo = aditivo,
            };
        }

        private void NacerChispa(Vector2 pos, Vector2 vel, Color color, float escala, Vector2 estira, bool aditivo)
        {
            if (_nChispas >= MaxChispas) return;
            _chispas[_nChispas++] = new Chispa
            {
                Pos = pos,
                Rot = vel.ToRotation() + MathHelper.PiOver2,
                Color = color,
                Escala0 = escala,
                Estira = estira,
                Nace = (int)Main.GameUpdateCount,
                Aditivo = aditivo,
            };
        }

        public override void AI()
        {
            Time += 1f;
            _tickFase++;
            if (_curvaDir == 0) _curvaDir = Main.rand.NextBool() ? 1 : -1;

            // LA MEMORIA: el cuerpo se acuerda de dónde estuvo.
            if (_nHistoria < HistoriaMax) _historia[_nHistoria++] = Projectile.Center;
            else
            {
                for (int i = 0; i < HistoriaMax - 1; i++) _historia[i] = _historia[i + 1];
                _historia[HistoriaMax - 1] = Projectile.Center;
            }

            // === EL SEGUIMIENTO QUE CRECE (la letra v6.51.2): lento y
            //     simple por defecto, del nivel 100 al 1000 cada vez
            //     MÁS agresivo. ===
            int nivelLibro = Content.Systems.WeaponScaling.NivelDelDuenio(Projectile);
            float homAcel = Content.Systems.WeaponScaling.HomingAcel(nivelLibro);
            float homTope = Content.Systems.WeaponScaling.HomingTope(nivelLibro);

            NPC presa = PresaMasCercana(900f);

            if (_enGestacion)
            {
                // === LA GESTACIÓN: frena escupiendo pulsos que IMPLODEN. ===
                Projectile.velocity *= 0.94f;
                if (Time > 2 && Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
                {
                    // EL DÍA SUMA (oro aditivo); LA NOCHE OSCURECE (vacío en
                    // pase alfa) + su rim de brasa fría encima.
                    NacerPulso(Projectile.Center, EsDia ? ColorCuerpo : ColorVacio,
                        0.5f, 4, EsDia);
                    if (!EsDia)
                        NacerPulso(Projectile.Center, ColorRim * 0.55f, 0.4f, 4, true);
                }
                if (Time >= T_GESTA)
                {
                    _enGestacion = false;
                    _volando = true;
                    _tickFase = 0;
                    // EL ARRANQUE: hacia la presa (o arriba si el mundo está limpio)
                    Vector2 rumbo = presa != null
                        ? presa.Center - Projectile.Center
                        : new Vector2(Main.rand.NextFloat(-1f, 1f), -1.4f);
                    if (rumbo.LengthSquared() < 1f) rumbo = -Vector2.UnitY;
                    rumbo.Normalize();
                    Projectile.velocity = rumbo.RotatedBy(0.35f * _curvaDir) * 8.5f;
                    if (!Main.dedServ)
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item12.WithPitchOffset(EsDia ? -0.25f : -0.55f),
                            Projectile.Center);
                }
                return;
            }

            if (_volando)
            {
                // === EL AZOTE: la MEDIA LUNA — el rumbo GIRA a cada tick
                //     (la curva que barre el cielo) y ACELEA hacia la
                //     presa con la agresividad del grimorio. ===
                float f = 1f - Math.Min(_tickFase / (float)T_AZOTE, 1f);
                Projectile.velocity = Projectile.velocity.RotatedBy(0.052f * f * _curvaDir);
                _escala = 1f - 0.35f * f;

                if (presa != null)
                {
                    Vector2 hacia = presa.Center - Projectile.Center;
                    float len = hacia.Length();
                    if (len > 40f && len > 0.01f)
                    {
                        hacia /= len;
                        Projectile.velocity += hacia * 0.10f * homAcel;
                    }
                    float spd = Projectile.velocity.Length();
                    float tope = 8.5f * homTope;
                    if (spd > tope) Projectile.velocity *= tope / spd;
                }

                // LAS CHISPAS DE COLA (estiradas — el gesto del tomo):
                // la noche lleva VACÍO en pase alfa (oscurece la estela).
                if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
                {
                    Vector2 cola = -Projectile.velocity * 0.04f;
                    Vector2 estira = new(1.6f, 1.0f + 0.6f * f);
                    if (!EsDia)
                        NacerChispa(Projectile.Center, cola, ColorVacio * 0.8f,
                            0.055f * _escala, estira, false);
                    NacerChispa(Projectile.Center, cola, ColorCuerpo * 0.75f,
                        0.055f * _escala, estira, true);
                }

                if (_tickFase >= T_AZOTE)
                {
                    _curvasHechas++;
                    if (_curvasHechas >= CurvasMax)
                    {
                        // LA TERCERA CURVA MUERE MATANDO: el polvo final.
                        Morir();
                        return;
                    }
                    // === EL SALTO: teleport hacia la presa + blooms + pausa
                    //     + el sentido de giro INVERTIDO. ===
                    _volando = false;
                    _tickFase = 0;
                    Vector2 salto = presa != null
                        ? (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX)
                        : Main.rand.NextVector2Unit();
                    Projectile.Center += salto * Main.rand.NextFloat(60f, 110f);
                    _curvaDir = -_curvaDir;
                    if (!Main.dedServ)
                    {
                        for (int i = 0; i < 6; i++)
                            NacerPulso(Projectile.Center, ColorNucleo * 0.8f,
                                0.55f, 5, true);
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item122.WithPitchOffset(EsDia ? 0.35f : -0.35f)
                                .WithVolumeScale(0.5f), Projectile.Center);
                    }
                }
            }
            else
            {
                // === LA PAUSA (la recarga entre curvas): flota, respira,
                //     los pulsos le DAN hambre. ===
                Projectile.velocity *= 0.90f;
                if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(3))
                    NacerPulso(Projectile.Center, ColorCuerpo * 0.6f, 0.35f, 4, true);
                if (_tickFase >= T_PAUSA)
                {
                    _volando = true;
                    _tickFase = 0;
                    Vector2 rumbo = presa != null
                        ? presa.Center - Projectile.Center
                        : new Vector2(Main.rand.NextFloat(-1f, 1f), -1.2f);
                    if (rumbo.LengthSquared() < 1f) rumbo = -Vector2.UnitY;
                    rumbo.Normalize();
                    Projectile.velocity = rumbo.RotatedBy(0.35f * _curvaDir) * 8.5f;
                }
            }

            // LA LUZ: el día ENCIENDE; la noche apenas susurra violeta.
            if (EsDia)
                Lighting.AddLight(Projectile.Center, 0.55f, 0.42f, 0.16f);
            else
                Lighting.AddLight(Projectile.Center, 0.16f, 0.08f, 0.30f);
        }

        /// <summary>LA PRESA: la más cercana y de verdad (el filtro de la casa).</summary>
        private NPC PresaMasCercana(float rango)
        {
            NPC mejor = null;
            float mejorD = rango;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !VFXCore.EsObjetivo(n)) continue;
                float d = n.Center.DistanceSQ(Projectile.Center);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        private void Morir()
        {
            if (!Main.dedServ)
            {
                for (int i = 0; i < 16; i++)
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center,
                        EsDia ? DustID.GoldFlame : DustID.Shadowflame,
                        Main.rand.NextVector2Circular(5f, 5f), 150,
                        EsDia ? new Color(255, 200, 110) : new Color(140, 70, 210), 1.1f);
                    d.noGravity = true;
                }
            }
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ) return;
            for (int i = 0; i < 10; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center,
                    EsDia ? 278 : 267,
                    Main.rand.NextVector2Circular(4f, 4f));
                d.scale = Main.rand.NextFloat(0.4f, 0.9f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;
            try
            {
                float time = Main.GlobalTimeWrappedHourly;
                int ahora = (int)Main.GameUpdateCount;
                Vector2 centro = Projectile.Center;

                // ============================================================
                //  EL CUERPO — LA CINTA QUE SE AFILA: la historia de
                //  posiciones dibujada como segmentos decrecientes (el
                //  tentáculo ES su cola). El día: oro aditivo; la noche:
                //  VACÍO en pase alfa primero (oscurece) y su rim de
                //  brasa fría encima.
                // ============================================================
                VFXCore.CerrarLoteSiAbierto();

                // EL PASE ALFA DE LA NOCHE (el vacío que oscurece la estela)
                if (!EsDia && _nHistoria >= 2)
                {
                    VFXCore.Begin();
                    try
                    {
                        for (int i = 0; i < _nHistoria - 1; i++)
                        {
                            float f = i / (float)Math.Max(1, _nHistoria - 1);
                            float grueso = (2f + 13f * (1f - f)) * _escala;
                            VFXCore.Line(_historia[i], _historia[i + 1],
                                ColorVacio * 0.72f, grueso * 1.9f);
                        }
                    }
                    catch { VFXCore.CerrarLoteSiAbierto(); }
                    VFXCore.FlushAlpha();
                }

                VFXCore.Begin();
                try
                {
                    // EL CUERPO (aditivo): la cinta con su pulso de vida
                    if (_nHistoria >= 2)
                    {
                        for (int i = 0; i < _nHistoria - 1; i++)
                        {
                            float f = i / (float)Math.Max(1, _nHistoria - 1);
                            float vivo = 0.8f + 0.2f * MathF.Sin(time * 9f + i * 0.9f);
                            float grueso = (2f + 13f * (1f - f)) * _escala;
                            VFXCore.Line(_historia[i], _historia[i + 1],
                                ColorCuerpo * (0.42f * vivo * (1f - f * 0.35f)), grueso);
                            VFXCore.Line(_historia[i], _historia[i + 1],
                                ColorRim * (0.20f * vivo), grueso * 0.42f);
                        }
                    }

                    // LA CABEZA: núcleo + halo + EL OJO de la pupila oscura
                    // (la mirada del Equinoccio: el día la cierra, la noche
                    // la abre — la firma del grimorio en cada tentáculo).
                    float cab = 13f * _escala * (0.9f + 0.1f * MathF.Sin(time * 7f));
                    VFXCore.Quad(centro, ColorCuerpo * 0.55f, new Vector2(cab * 3.2f, cab * 3.2f));
                    VFXCore.Quad(centro, ColorNucleo * 0.85f, new Vector2(cab * 1.7f, cab * 1.7f));
                    VFXCore.Quad(centro, Color.White * 0.30f, new Vector2(cab * 0.8f, cab * 0.8f));

                    // LOS PULSOS QUE IMPLODEN (de grandes a nada — el gesto
                    // del tomo, con su easing PolyOut(4) y su seno).
                    for (int i = 0; i < _nPulsos; i++)
                    {
                        float ed = (ahora - _pulsos[i].Nace) / (float)Math.Max(1, _pulsos[i].Vida);
                        if (ed < 0f || ed > 1f) continue;
                        float ease = 1f - MathF.Pow(1f - ed, 4f);          // PolyOut(4): implosiona
                        float alfa = MathF.Sin(MathHelper.PiOver2 + ed * MathHelper.PiOver2);
                        float esc = _pulsos[i].Escala0 * 64f * (1f - ease);
                        if (esc < 2f) continue;
                        VFXCore.Quad(_pulsos[i].Pos,
                            _pulsos[i].Color * (alfa * 0.6f), new Vector2(esc, esc));
                    }
                    _nPulsos = CompactarPulsos(_pulsos, _nPulsos, ahora);

                    // LAS CHISPAS (estiradas perpendicular al vuelo, decaen
                    // ×0.95 y mueren al cubo).
                    for (int i = 0; i < _nChispas; i++)
                    {
                        float ed = (ahora - _chispas[i].Nace) / 12f;
                        if (ed < 0f || ed > 1f) continue;
                        float esc = _chispas[i].Escala0 * 64f * MathF.Pow(1f - ed, 3f) * 0.95f;
                        if (esc < 1.5f) continue;
                        VFXCore.Quad(_chispas[i].Pos, _chispas[i].Color * (0.7f * (1f - ed)),
                            new Vector2(esc * _chispas[i].Estira.X * 2.2f,
                                esc * _chispas[i].Estira.Y * 1.4f),
                            _chispas[i].Rot);
                    }
                    _nChispas = CompactarChispas(_chispas, _nChispas, ahora);
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.FlushAdditive();

                VFXCore.ReabrirLoteVanilla();
            }
            catch { VFXCore.ReabrirLoteVanilla(); }
            return false;
        }

        /// <summary>Compacta los búferes in-place (las partículas
        /// viven 4-12 frames: las muertas salen, las vivas se pegan al
        /// frente — cero alocaciones, el búfer jamás se llena).</summary>
        private static int CompactarPulsos(Pulso[] buffer, int n, int ahora)
        {
            int v = 0;
            for (int i = 0; i < n; i++)
            {
                if (ahora - buffer[i].Nace <= buffer[i].Vida)
                    buffer[v++] = buffer[i];
            }
            return v;
        }

        private static int CompactarChispas(Chispa[] buffer, int n, int ahora)
        {
            int v = 0;
            for (int i = 0; i < n; i++)
            {
                if (ahora - buffer[i].Nace <= 12)
                    buffer[v++] = buffer[i];
            }
            return v;
        }
    }
}
