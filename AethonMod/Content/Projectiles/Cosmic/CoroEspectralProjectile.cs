using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Particles;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// CoroEspectralProjectile — v6.26 — EL CORO ESPECTRAL.
    ///
    /// EL PROYECTIL ES UN CORO: 6 NOTAS DE LUZ fantasmales (sub-entidades
    /// LÓGICAS dentro del proyectil — el patrón de la casa: NADA de
    /// proyectiles extra) que orbitan lentamente el punto de lanzamiento
    /// (el ANCLA, en ai[0]/ai[1]) en órbitas elípticas propias (radio,
    /// velocidad y sentido distintos por nota — el coro nunca se alinea).
    ///
    /// EL CANTO (la mecánica): cada CICLO de 48 ticks (0.8 s) la NOTA
    /// SIGUIENTE (round-robin) EMITE su anillo:
    ///   · VISUAL: anillo de onda sonora OndaLib.Pulse expandiéndose desde
    ///     la posición de la nota + ECO tenue (un segundo anillo retrasado
    ///     y desvanecido que el renderer dibuja tras el primero).
    ///   · SONIDO: campana de cristal — SoundID.Item70 con PITCH distinto
    ///     por nota (la escala: −0.35 a +0.45).
    ///   · DAÑO: el FRENTE del anillo golpea a cada enemigo que ATRAVIESA
    ///     (|dist − radio| &lt; 14 px → 12% del arma, UNA vez por anillo por
    ///     enemigo — HashSet propio del anillo): daño bajo pero FRECUENTE
    ///     en un área de 260 px (los anillos van y vienen del coro).
    ///
    /// LA DESPEDIDA: a los ~9 s el canto termina y las notas se APAGAN UNA
    /// A UNA (cada una con su último anillo pequeño); la última nota
    /// entrega el ACORDE final (anillo doble). Vida 720 ticks = 12 s.
    ///
    /// Daño MP-seguro: SimpleStrikeNPC bajo `Main.netMode != NetmodeID.
    /// MultiplayerClient`. Determinismo por semilla de identity.
    /// </summary>
    public class CoroEspectralProjectile : ModProjectile
    {
        /// <summary>La vida del concierto: 720 ticks = 12 s.</summary>
        public const int TotalTicks = 720;

        /// <summary>El CICLO del coro: cada 48 ticks canta la nota siguiente.</summary>
        public const int CicloTicks = 48;

        /// <summary>Las NOTAS del coro.</summary>
        public const int Notas = 6;

        /// <summary>El radio FINAL de los anillos (px).</summary>
        public const float RadioAnillo = 260f;

        /// <summary>Duración del vuelo de un anillo (ticks).</summary>
        public const int AnilloTicks = 44;

        /// <summary>Tick donde TERMINA el canto (empieza la despedida).</summary>
        public const int FinCanto = 528;

        // === EL ANILLO ACTIVO (sub-entidad lógica — visible al renderer) ===
        internal sealed class Anillo
        {
            public Vector2 Origen;
            public float Edad;
            public Color Color;
            public int Semilla;
            public HashSet<int> Golpeados = new();
            public bool Eco;          // true = el anillo tenue de despedida
        }

        // === LAS NOTAS (sub-entidades lógicas) ===
        private readonly float[] _fase = new float[Notas];
        private readonly float[] _radio = new float[Notas];
        private readonly float[] _velAng = new float[Notas];
        private readonly float[] _altura = new float[Notas];
        /// <summary>La nota viva hasta su tick de apagado (0 = apagada).</summary>
        private readonly float[] _brillo = new float[Notas];

        /// <summary>Los anillos activos (máx 7 — uno por ciclo + ecos).</summary>
        private readonly List<Anillo> _anillos = new(8);

        private float _age;

        /// <summary>El ANCLA del coro (ai[0]=X, ai[1]=Y — sincronizadas).</summary>
        private Vector2 Ancla => new(Projectile.ai[0], Projectile.ai[1]);

        /// <summary>Semilla determinista (ai[2]).</summary>
        private int Seed => Math.Max(1, (int)Projectile.ai[2]) + Projectile.identity;

        /// <summary>El daño base del arma. v6.27 FIX: el array `ai`
        /// de tModLoader SOLO tiene 3 ranuras — el espejo vive en `localAI[2]`
        /// (no sincronizada, pero `Projectile.damage` —que SÍ viaja— es el
        /// fallback, y el daño manual corre en servidor/SP).</summary>
        private float BaseDamage => Projectile.localAI[2] > 0f ? Projectile.localAI[2] : Projectile.damage;

        // === LOS COLORES DE LA ESCALA (dorada → ceniza) ===
        private static readonly Color[] Escala = new Color[]
        {
            new(255, 214, 110),   // dorada
            new(255, 190, 92),    // ámbar
            new(225, 165, 96),    // bronce
            new(198, 140, 82),    // cobre
            new(172, 112, 66),    // óxido
            new(150, 96, 55),     // ceniza dorada
        };

        /// <summary>El color de la nota i (para el renderer).</summary>
        internal static Color ColorDe(int i) => Escala[i % Escala.Length];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            // La caja es el CORO entero (el dibujo es 100% código).
            Projectile.width = 20;
            Projectile.height = 20;
            // Daño 100% manual (el patrón de la casa): los anillos.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalTicks;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // EL ANCLA: el coro se reúne en el punto de emisión (el cursor).
            Projectile.ai[0] = Projectile.Center.X;
            Projectile.ai[1] = Projectile.Center.Y;
            if (Projectile.ai[2] <= 0f)
                Projectile.ai[2] = (Projectile.identity % 9973 + 1) * 1f;
            // v6.27 FIX: `ai[3]` NO EXISTE (IndexOutOfRangeException en el
            // log del usuario) — el espejo del daño va a localAI[2].
            Projectile.localAI[2] = Projectile.damage;
            _age = 0f;
            Projectile.netUpdate = true;

            // LAS ÓRBITAS de las notas (deterministas por semilla: radios
            // 56-88, velocidades 0.010-0.018, sentidos alternos).
            for (int i = 0; i < Notas; i++)
            {
                float h1 = VFXCore.Hash01(Seed, i, 13);
                float h2 = VFXCore.Hash01(Seed, i, 17);
                _fase[i] = i * MathHelper.TwoPi / Notas + h1 * 0.9f;
                _radio[i] = 56f + 32f * h2;
                _velAng[i] = (0.010f + 0.008f * h1) * ((i & 1) == 0 ? 1f : -1f);
                _altura[i] = (h2 - 0.5f) * 26f;   // la elipse es INCLINADA
                _brillo[i] = 1f;
            }
        }

        // ==================================================================
        //  EL CONCIERTO — órbitas, canto y despedida
        // ==================================================================

        public override void AI()
        {
            _age += 1f;

            // === EL PROYECTIL ES EL ANCLA (flotando con un vaivén de
            //     director: ±3 px, 0.4 Hz — cosmético). ===
            Projectile.Center = Ancla + new Vector2(
                MathF.Sin(_age * 0.026f) * 3f, MathF.Cos(_age * 0.021f) * 3f);

            // === LAS ÓRBITAS (cada nota vive su elipse) ===
            for (int i = 0; i < Notas; i++)
            {
                _fase[i] += _velAng[i];
                // EL APAGADO progresivo de la despedida: la nota i se apaga
                // en su tick (FinCanto + 24·i .. +48 más de fade).
                float apagaEn = FinCanto + 24f * i;
                if (_age > apagaEn)
                {
                    float fade = MathHelper.Clamp(1f - (_age - apagaEn) / 48f, 0f, 1f);
                    if (_brillo[i] > 0f && fade <= 0f)
                    {
                        // EL ÚLTIMO ANILLO de esta nota (pequeño — el eco de
                        // despedida) al momento exacto del apagado.
                        EmitirAnillo(i, eco: true);
                    }
                    _brillo[i] = fade;
                }
            }

            // === EL CANTO (cada 48 ticks, la nota siguiente) ===
            if (_age % CicloTicks == 0f && _age <= FinCanto)
            {
                int nota = (int)(_age / CicloTicks) % Notas;
                EmitirAnillo(nota, eco: false);
            }

            // === LOS ANILLOS: vuelan, golpean y mueren ===
            for (int a = _anillos.Count - 1; a >= 0; a--)
            {
                Anillo anillo = _anillos[a];
                anillo.Edad += 1f;
                if (anillo.Edad >= AnilloTicks)
                {
                    _anillos.RemoveAt(a);
                    continue;
                }

                // EL DAÑO del frente (MP-seguro — el anillo ATRAVIESA).
                if (Main.netMode != NetmodeID.MultiplayerClient && !anillo.Eco)
                {
                    float r = RadioAnillo * OndaLib.Expansion(
                        anillo.Edad / AnilloTicks) * (anillo.Eco ? 0.5f : 1f);
                    int dmg = Math.Max(1, (int)(BaseDamage * 0.12f));
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.CanBeChasedBy()) continue;
                        if (anillo.Golpeados.Contains(npc.whoAmI)) continue;
                        float dist = (npc.Center - anillo.Origen).Length();
                        if (MathF.Abs(dist - r) > 14f) continue;
                        anillo.Golpeados.Add(npc.whoAmI);
                        npc.SimpleStrikeNPC(dmg, npc.direction, false, 1f,
                            DamageClass.Magic);
                    }
                }
            }

            // === LA LUZ del coro (tenue, dorada — un coro de fantasmas) ===
            if (Main.netMode != NetmodeID.Server && _age % 4f == 0f)
                Lighting.AddLight(Projectile.Center, 0.34f, 0.27f, 0.14f);
        }

        /// <summary>UNA NOTA CANTA: el anillo + la campana + el registro.</summary>
        private void EmitirAnillo(int nota, bool eco)
        {
            Vector2 pos = PosNota(nota);

            Anillo anillo = new()
            {
                Origen = pos,
                Edad = 0f,
                Color = ColorDe(nota),
                Semilla = Seed + nota * 31,
                Eco = eco,
            };
            _anillos.Add(anillo);

            if (Main.netMode == NetmodeID.Server) return;

            // === LA CAMPANA DE CRISTAL (tono por nota: la escala entera) ===
            // (los ecos suenan al doble de pitch y más flojos — el fantasma
            //  del tono que fue.)
            float pitch = eco
                ? 0.45f + nota * 0.06f
                : -0.35f + nota * 0.16f;
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item70.WithPitchOffset(pitch).WithVolumeScale(eco ? 0.25f : 0.45f),
                pos);

            // === LA CHISPA del canto (en la nota que canta) ===
            if (!eco)
            {
                Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame,
                    -Vector2.UnitY * 0.8f, 160, anillo.Color, 0.5f);
                d.noGravity = true;
            }
        }

        /// <summary>La posición ACTUAL de la nota i (su elipse).</summary>
        private Vector2 PosNota(int i)
        {
            float c = MathF.Cos(_fase[i]);
            float s = MathF.Sin(_fase[i]);
            // La elipse INCLINADA: aplastada en Y y con su altura propia.
            return Ancla + new Vector2(c * _radio[i],
                s * _radio[i] * 0.55f + _altura[i] * (0.4f + 0.6f * MathF.Abs(c)));
        }

        // ==================================================================
        //  EL ACCESO DEL RENDERER (el coro para el pintado)
        // ==================================================================

        /// <summary>La posición de la nota i.</summary>
        internal Vector2 NotaPos(int i) => PosNota(i);

        /// <summary>El brillo de la nota i (la despedida lo apaga).</summary>
        internal float NotaBrillo(int i) => _brillo[i];

        /// <summary>La fase orbital de la nota i (para las banderas).</summary>
        internal float NotaFase(int i) => _fase[i];

        /// <summary>El radio de la órbita de la nota i.</summary>
        internal float NotaRadio(int i) => _radio[i];

        /// <summary>La altura de la órbita de la nota i.</summary>
        internal float NotaAltura(int i) => _altura[i];

        /// <summary>Los anillos activos (para el renderer).</summary>
        internal List<Anillo> AnillosLista => _anillos;

        // ==================================================================
        //  EL DIBUJO (contrato de batch v6.10)
        // ==================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                CoroEspectralRenderer.Draw(this, _age, Seed);
            }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
                RestoreSpriteBatch();
            return false;
        }

        /// <summary>Restaura el SpriteBatch con los parámetros EXACTOS del
        /// pase de proyectiles de vanilla.</summary>
        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        public override void OnKill(int timeLeft)
        {
            // EL ACORDE FINAL: cuando muere el coro, el último suspiro —
            // un anillo doble tenue (visual cliente).
            if (Main.netMode == NetmodeID.Server) return;
            ParticlePresets.RingPulse(Projectile.Center, 150f,
                new Color(255, 214, 130, 180), 36);
            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item70.WithPitchOffset(-0.45f), Projectile.Center);
        }
    }
}
