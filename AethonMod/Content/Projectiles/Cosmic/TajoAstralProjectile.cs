using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// TajoAstralProjectile — v6.40 — EL CORTE DIFERIDO (el tajo del anime).
    ///
    /// LA ARMADURA DE LA CAUSALIDAD RETRASADA — el tropo del anime que el
    /// usuario pidió ("los cortes aparecen DESPUÉS de efectuado el corte"):
    /// la investigación v6.40 lo nombra — el golpe YA TERMINÓ; los efectos
    /// decidieron esperar.
    ///
    /// LAS TRES FASES (los tiempos medidos de la investigación):
    ///   1. EL DESEMBAINO (ticks 0..6): el proyectil VUEA al punto marcado
    ///      — un hilo de anticipación tenue (la katana saliendo, sin
    ///      ruido). Llega al destino en el tick 6.
    ///   2. EL MARCAJE (ticks 6..16): el punto de destino SUSURRA — una
    ///      marca dorada pulsando, chispitas convergiendo, la tensión del
    ///      aire antes del corte (10 ticks de retardo — la regla de
    ///      legibilidad de la investigación: ≤ 12).
    ///   3. EL FLORECER (tick 16 en adelante): SIETE TAJOS — medias lunas
    ///      blancas en direcciones DESACOPLADAS (Hash01 de la semilla por
    ///      identity — determinista MP), cada una con su POP escalonado
    ///      (3 ticks entre arcos, "olas de tajos"), su crecimiento
    ///      DIRECCIONAL de punta a punta (TajoLib.Tajo) y su campana de
    ///      vida asimétrica. El DAÑO cae en el tick de aparición de cada
    ///      arco (no en el marcaje): la herida llega cuando el tajo
    ///      existe — la causalidad del anime.
    ///
    /// EL DAÑO — v6.50 — GolpeMotor (el cauce del motor: crítica real,
    ///      varianza, on-hit y sync MP del propio motor): cada arco golpea
    ///      UNA vez, en su banda curva (|dist−radio| &lt; 50 && ángulo dentro
    ///      del arco), EsObjetivo, resuelto en el cliente dueño. El visual es
    ///      solo cliente (PreDraw).
    ///
    /// CERO Main.rand en el render (todo Hash01); SIN hide (la lección
    /// v6.35); el contrato de batch v6.10 a prueba de balas.
    /// </summary>
    public class TajoAstralProjectile : ModProjectile
    {
        // === LA GEOMETRÍA DEL FLORECER (las medidas de la investigación) ===
        private const int DesembainoTicks = 6;    // fase 1: el vuelo
        private const int MarcadoTicks = 10;      // fase 2: la marca (≤12: legible)
        private const int PopInicial = DesembainoTicks + MarcadoTicks;  // 16
        private const int Arcos = 7;              // los tajos del florecer
        private const int Stagger = 3;            // ticks entre arcos (olas)
        private const int RevealTicks = 7;        // el crecimiento de punta a punta
        private const int VidaTajo = 15;          // vida total del arco (10-30 de la investigación)
        private const float AnchoTajo = 4.6f;     // grosor máximo (px, en el centro)

        // === EL ESTADO DEL FLORECER (7 tajos, determinista por semilla) ===
        private readonly Vector2[] _centro = new Vector2[Arcos];
        private readonly float[] _radio = new float[Arcos];
        private readonly float[] _ang0 = new float[Arcos];
        private readonly float[] _ang1 = new float[Arcos];
        private readonly int[] _pop = new int[Arcos];
        private bool _init;

        private float _edad;
        private Vector2 _destino;
        private Vector2 _origen;

        // === LA PALETA (el blanco del anime + el oro de la casa) ===
        private static readonly Color TajoBlanco = new(255, 251, 240);   // el filo
        private static readonly Color TajoOro = new(255, 214, 130);      // el halo
        private static readonly Color MarcaOro = new(255, 226, 150);     // la marca

        /// <summary>Semilla determinista (identity — la regla MP de la casa).</summary>
        private int Seed => Math.Max(1, Projectile.identity + 71);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = false;          // el daño va por GolpeMotor (v6.50)
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = PopInicial + Arcos * Stagger + VidaTajo + 8;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 0;
            // SIN hide (la lección v6.35: tML no dibuja los ocultos).
        }

        public override void OnSpawn(IEntitySource data)
        {
            // EL DESTINO (ai[0..1]: el punto del mundo donde florecerán los
            // tajos — el llamador puso la mira del cursor).
            _destino = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            _origen = Projectile.Center;

            // EL VUELO: llegar en DesembainoTicks (velocidad constante).
            Projectile.velocity = (_destino - _origen) / DesembainoTicks;

            // === LOS SIETE TAJOS (la geometría desacoplada por Hash01) ===
            for (int i = 0; i < Arcos; i++)
            {
                float h1 = VFXCore.Hash01(Seed, 11 + i * 7, 3);
                float h2 = VFXCore.Hash01(Seed, 12 + i * 7, 5);
                float h3 = VFXCore.Hash01(Seed, 13 + i * 7, 7);
                float h4 = VFXCore.Hash01(Seed, 14 + i * 7, 9);

                // El centro del arco: cerca de la marca (±64 px) — los
                // tajos la CRUZAN desde todas direcciones.
                _centro[i] = _destino + new Vector2(
                    (h1 - 0.5f) * 128f, (h2 - 0.5f) * 128f);

                // El radio: 54..100 px (la envolvente del florecer).
                _radio[i] = 54f + h3 * 46f;

                // La abertura del creciente: 1.6..2.4 rad (media luna).
                float span = 1.6f + h4 * 0.8f;

                // LA DIRECCIÓN: desacoplada por arco (todas direcciones) —
                // y el SENTIDO del crecimiento alterna (par horario,
                // impar antihorario: el caos ordenado del anime).
                float base0 = VFXCore.Hash01(Seed, 15 + i * 7, 13) * MathHelper.TwoPi;
                if (i % 2 == 0) { _ang0[i] = base0; _ang1[i] = base0 + span; }
                else { _ang0[i] = base0 + span; _ang1[i] = base0; }

                // EL POP escalonado (olas de tajos: 3 ticks entre arcos).
                _pop[i] = PopInicial + i * Stagger + (int)(h2 * 2.99f);
            }

            _init = true;
        }

        public override void AI()
        {
            _edad += 1f;

            // ==============================================================
            //  FASE 1 — EL DESEMBAINO (el vuelo al destino).
            // ==============================================================
            if (_edad < DesembainoTicks)
            {
                // El hilo tenue de anticipación (la katana saliendo).
                if (_edad % 2f == 0f && Main.netMode != NetmodeID.Server)
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                        -Projectile.velocity * 0.06f, 120, MarcaOro, 0.55f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
                return;
            }

            // Clavado en la marca (el florecer no se mueve).
            Projectile.Center = _destino;
            Projectile.velocity = Vector2.Zero;

            // ==============================================================
            //  FASE 2 — EL MARCAJE (la tensión antes del corte).
            // ==============================================================
            if (_edad < PopInicial)
            {
                // La marca respira (luz dorada creciendo).
                float t = (_edad - DesembainoTicks) / (float)MarcadoTicks;
                Lighting.AddLight(_destino, 0.35f * t, 0.28f * t, 0.10f * t);

                // Chispitas convergiendo (el aire tensándose).
                if (_edad % 3f == 0f && Main.netMode != NetmodeID.Server)
                {
                    float ang = VFXCore.Hash01(Seed, 3 + (int)_edad, 17) * MathHelper.TwoPi;
                    Vector2 pos = _destino + new Vector2(
                        (float)Math.Cos(ang) * 26f, (float)Math.Sin(ang) * 26f);
                    Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame,
                        Vector2.Normalize(_destino - pos) * 1.6f, 140, MarcaOro, 0.6f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
                return;
            }

            // ==============================================================
            //  FASE 3 — EL FLORECER (los tajos nacen, cortan y mueren).
            // ==============================================================
            bool floreciendo = false;
            for (int i = 0; i < Arcos; i++)
            {
                float tArco = _edad - _pop[i];
                if (tArco < 0f || tArco > VidaTajo) continue;
                floreciendo = true;

                // EL POP: el tajo APARECE — este es el tick del daño (la
                // causalidad del anime: la herida llega con el tajo).
                if (tArco == 0f)
                {
                    GolpearArco(i);

                    // El destello de nacimiento + la voz del filo (solo el
                    // primer arco suena — 7 sonidos sería spam).
                    if (i == 0 && Main.netMode != NetmodeID.Server)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item93.WithPitchOffset(0.65f).WithVolumeScale(0.55f),
                            _destino);
                    }
                }

                // La luz del tajo (viaja con su campana).
                float cam = TajoLib.Campana(tArco / VidaTajo);
                Lighting.AddLight(_centro[i], 0.55f * cam, 0.50f * cam, 0.24f * cam);
            }

            // Los últimos destellos del florecer muerto (brasas que caen).
            if (floreciendo && _edad % 4f == 0f && Main.netMode != NetmodeID.Server)
            {
                float ang = VFXCore.Hash01(Seed, 5 + (int)_edad, 19) * MathHelper.TwoPi;
                Vector2 pos = _destino + new Vector2(
                    (float)Math.Cos(ang) * 40f, (float)Math.Sin(ang) * 40f);
                Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame,
                    new Vector2(0f, 0.7f), 130, MarcaOro, 0.6f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        /// <summary>
        /// EL DAÑO DEL ARCO — v6.50 — GolpeMotor (el cauce del motor: crítica
        /// real, varianza, on-hit y sync MP del propio motor, resuelto en el
        /// cliente dueño): todo NPC en la BANDA CURVA del tajo (|dist al
        /// centro − radio| &lt; 50 y ángulo dentro del arco) recibe el filo; el
        /// visual es solo cliente.
        /// </summary>
        private void GolpearArco(int i)
        {
            int dmg = Math.Max(1, (int)(Projectile.damage * 0.75f));
            float aMin = Math.Min(_ang0[i], _ang1[i]);
            float aMax = Math.Max(_ang0[i], _ang1[i]);

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;

                Vector2 v = npc.Center - _centro[i];
                float dist = v.Length();
                if (Math.Abs(dist - _radio[i]) > 50f) continue;

                float ang = (float)Math.Atan2(v.Y, v.X);
                if (ang < aMin - 0.25f || ang > aMax + 0.25f) continue;

                Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 4f, true);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;
            if (!_init) return false;

            // ============================================================
            //  CONTRATO DE BATCH A PRUEBA DE BALAS (v6.10).
            // ============================================================
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                DrawTajos();
            }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
                RestoreSpriteBatch();
            return false;
        }

        /// <summary>Restaura el SpriteBatch con los parámetros EXACTOS del
        /// pase de proyectiles de vanilla (Main.DrawProjectiles).</summary>
        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        // ------------------------------------------------------------------
        //  EL DIBUJO DEL CORTE DIFERIDO
        // ------------------------------------------------------------------

        private void DrawTajos()
        {
            float time = Main.GlobalTimeWrappedHourly;
            Vector2 centro = _destino - Main.screenPosition;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            try
            {
                // === FASE 1: EL HILO DE ANTICIPACIÓN (el vuelo) ===
                if (_edad < DesembainoTicks)
                {
                    float f = _edad / (float)DesembainoTicks;
                    Vector2 desde = _origen - Main.screenPosition;
                    Vector2 hasta = centro;
                    Vector2 mid = (desde + hasta) * 0.5f;
                    float len = (hasta - desde).Length();
                    // El hilo: UNA cápsula tenue de origen a destino (el
                    // trazo del embate — fino, casi un susurro).
                    QuadGlow(mid, new Vector2(len, 1.8f),
                        (float)Math.Atan2(hasta.Y - desde.Y, hasta.X - desde.X),
                        TajoLib.Tint(TajoOro, 0.10f * (1f - f * 0.5f)));
                }

                // === FASE 2: LA MARCA (el punto que susurra) ===
                if (_edad >= DesembainoTicks && _edad < PopInicial)
                {
                    float t = (_edad - DesembainoTicks) / (float)MarcadoTicks;
                    // El punto pulsando (la tensión crece con t).
                    float pulso = 0.5f + 0.5f * (float)Math.Sin(time * 14f);
                    QuadGlow(centro, new Vector2(10f + 16f * t, 10f + 16f * t), 0f,
                        TajoLib.Tint(MarcaOro, (0.25f + 0.55f * t) * (0.6f + 0.4f * pulso)));
                    QuadGlow(centro, new Vector2(4f, 4f), 0f,
                        TajoLib.Tint(TajoBlanco, 0.7f * (0.5f + 0.5f * pulso)));
                }

                // === FASE 3: EL FLORECER (los siete tajos) ===
                for (int i = 0; i < Arcos; i++)
                {
                    float tArco = _edad - _pop[i];
                    if (tArco < 0f || tArco > VidaTajo) continue;

                    // El revelado: de punta a punta en RevealTicks (con
                    // arranque elástico — el pop del anime).
                    float reveal = Elastico(Math.Min(tArco / (float)RevealTicks, 1f));

                    // La campana de vida asimétrica (sube al 55%, cae al 45%).
                    float cam = TajoLib.Campana(tArco / VidaTajo);

                    // El radio CRECE con la vida (×1.0 → ×1.32: el arco se
                    // abre mientras muere — el aliento del corte).
                    float radio = _radio[i] * (1f + 0.32f * EaseOut(tArco / VidaTajo));

                    TajoLib.Tajo(_centro[i] - Main.screenPosition, radio,
                        _ang0[i], _ang1[i], reveal, cam,
                        AnchoTajo, TajoOro, TajoBlanco, Seed + i * 31, time);
                }

                // El ECO del florecer: la marca se disuelve con el último
                // tajo (un destello suave de clausura).
                int ultimo = PopInicial + (Arcos - 1) * Stagger + VidaTajo;
                if (_edad > ultimo - 4f && _edad < ultimo + 2f)
                {
                    float f = (_edad - (ultimo - 4f)) / 6f;
                    QuadGlow(centro, new Vector2(46f * (1f - f), 46f * (1f - f)), 0f,
                        TajoLib.Tint(TajoOro, 0.35f * (1f - f)));
                }
            }
            finally
            {
                Main.spriteBatch.End();
            }
        }

        /// <summary>Quad centrado de SoftGlow al batch aditivo ABIERTO.</summary>
        private static void QuadGlow(Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Main.spriteBatch.Draw(
                ModContent.Request<Texture2D>("AethonMod/Content/Effects/Procedural/SoftGlow").Value,
                pos, null, tint, rot,
                new Vector2(32f, 32f), size / new Vector2(64f, 64f),
                SpriteEffects.None, 0f);
        }

        /// <summary>El easeOutBack de la casa (el POP del anime: sale con
        /// impulso — sobrepasa ~1.07 — y se asienta en 1).</summary>
        private static float Elastico(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c = 1.35f;
            float u = t - 1f;
            return 1f + u * u * ((c + 1f) * u + c);
        }

        /// <summary>El ease-out de la casa (el crecimiento se frena suave).</summary>
        private static float EaseOut(float t)
            => 1f - (1f - MathHelper.Clamp(t, 0f, 1f)) * (1f - MathHelper.Clamp(t, 0f, 1f));
    }
}
