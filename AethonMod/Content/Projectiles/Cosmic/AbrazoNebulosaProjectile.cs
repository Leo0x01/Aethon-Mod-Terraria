using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Particles;
using AethonMod.Content.Effects.Bruma;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// AbrazoNebulosaProjectile — EL ABRAZO DE LA NEBULOSA.
    ///
    /// Vuela rápido al punto del cursor y SE INSTALA ahí (vida 300
    /// ticks): una nube de BrumaFX en TRES colores mezclados (magenta,
    /// cian, violeta — deterministas) que DERIVA lentamente mientras
    /// su radio crece de 40 a 170 px en 40 ticks.
    ///
    /// LOS ENEMIGOS DENTRO: DoT ×0.06 cada 3 ticks + LENTITUD
    /// (velocity ×0.94 cada 4 ticks, solo server/SP).
    ///
    /// LAS ESTRELLITAS: cada 40 ticks nace una estrella bebé (destello
    /// que carga 30 ticks) y REVIENTA en un mini-pop ×0.8 radio 60 con
    /// luz. Al morir, la nebulosa se disipa en polvo de estrellas.
    /// </summary>
    public class AbrazoNebulosaProjectile : ModProjectile
    {
        // === LA CRONOLOGÍA (ticks) ===
        private const int VidaTicks = 300;      // vida instalada
        private const int CrecerTicks = 40;     // 40→170 px
        private const int DoTCada = 3;          // el mordisco constante
        private const int SlowCada = 4;         // la lentitud
        private const int BebeCada = 40;        // nace una estrellita
        private const int BebeTicks = 30;       // ...y a los 30 REVIENTA
        private const int MaxBebes = 8;
        private const float PopRadio = 60f;     // radio del mini-pop

        // === EL RADIO ===
        private const float RadioIni = 40f;
        private const float RadioFin = 170f;

        // === LA PALETA (magenta-cian-violeta) ===
        private static readonly Color ColorMagenta = new(255, 92, 198);
        private static readonly Color ColorCian = new(96, 222, 255);
        private static readonly Color ColorVioleta = new(168, 96, 255);
        private static readonly Color[] PaletaEstrellas =
        {
            new Color(255, 130, 220), new Color(130, 235, 255), new Color(200, 140, 255)
        };

        /// <summary>Una estrella bebé: cuándo nació, dónde (ángulo+dist) y si ya reventó.</summary>
        private struct Bebe
        {
            public int Nace;
            public float Ang;
            public float Dist;
            public bool Pop;
        }

        private float _age;
        private bool _nacio;
        private bool _instalada;
        private int _edadInst;
        private Vector2 _objetivo;
        private readonly Bebe[] _bebes = new Bebe[MaxBebes];
        private int _nBebes;

        /// <summary>Los últimos 3 mini-pops (anillo circular de destellos).</summary>
        private readonly Vector2[] _popPos = new Vector2[3];
        private readonly float[] _popAge = new float[3] { 999f, 999f, 999f };
        private int _popN;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            // Daño 100% manual (escuela A): sin contacto de vanilla.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 70;   // el vuelo; al instalarse se redefine
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        /// <summary>Semilla determinista (identidad de red).</summary>
        private int Seed => Math.Max(1, Projectile.identity + 67);

        /// <summary>El radio actual (40→170 en 40 ticks tras instalarse).</summary>
        private float RadioActual()
            => MathHelper.Lerp(RadioIni, RadioFin, MathHelper.Clamp(_edadInst / (float)CrecerTicks, 0f, 1f));

        public override void AI()
        {
            _age += 1f;

            if (!_nacio)
            {
                _nacio = true;
                _objetivo = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            }

            // Los destellos de los pops envejecen siempre.
            for (int i = 0; i < 3; i++)
                _popAge[i] += 1f;

            if (!_instalada)
            {
                // ========================================================
                //  EL VUELO: rápido hacia el punto del cursor.
                // ========================================================
                Vector2 hacia = _objetivo - Projectile.Center;
                if (hacia.LengthSquared() <= 28f * 28f)
                {
                    _instalada = true;
                    Projectile.Center = _objetivo;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.timeLeft = VidaTicks + 4;
                    _edadInst = 0;
                    if (Main.netMode != NetmodeID.Server)
                    {
                        try
                        {
                            Terraria.Audio.SoundEngine.PlaySound(
                                SoundID.Item88.WithPitchOffset(-0.2f), Projectile.Center);
                        }
                        catch { }
                    }
                }
                else
                {
                    Projectile.velocity = Vector2.Normalize(hacia) * 26f;
                }

                LuzNebulosa(RadioIni * 0.5f);
                return;
            }

            // ============================================================
            //  LA NEBULOSA INSTALADA: deriva, abraza, y pare hijos.
            // ============================================================
            _edadInst++;
            float radio = RadioActual();

            // LA DERIVA LENTA: dos senos inconmensurables (determinista).
            Projectile.velocity = new Vector2(
                MathF.Sin(_age * 0.017f + Seed) * 0.35f,
                MathF.Cos(_age * 0.013f + Seed * 1.7f) * 0.28f);

            LuzNebulosa(radio);

            // EL ABRAZO: DoT + lentitud (escuela A: solo server/SP).
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (_edadInst % DoTCada == 0) Morder(radio);
                if (_edadInst % SlowCada == 0) Frenar(radio);
            }

            // LAS ESTRELLITAS: una nueva cada 40 ticks...
            if (_edadInst >= BebeCada && _edadInst % BebeCada == 0 && _nBebes < MaxBebes)
                NacerBebe();

            // ...y cada una REVIENTA 30 ticks después.
            for (int k = 0; k < _nBebes; k++)
            {
                if (_bebes[k].Pop) continue;
                if (_edadInst - _bebes[k].Nace >= BebeTicks)
                    Reventar(k);
            }
        }

        /// <summary>EL DoT DEL ABRAZO: ×0.06 a todos los de dentro.</summary>
        private void Morder(float radio)
        {
            int dmg = Math.Max(1, (int)(Projectile.damage * 0.06f));
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float alcance = radio * 0.92f + Math.Max(npc.width, npc.height) * 0.5f;
                if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > alcance * alcance) continue;
                npc.SimpleStrikeNPC(dmg, npc.direction, false, 0f, DamageClass.Magic);
            }
        }

        /// <summary>LA LENTITUD: los de dentro se arrastran (×0.94 cada 4 ticks).</summary>
        private void Frenar(float radio)
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > radio * radio) continue;
                npc.velocity *= 0.94f;
            }
        }

        /// <summary>NACE una estrellita en un punto determinista de la nube.</summary>
        private void NacerBebe()
        {
            float ang = VFXCore.Hash01(Seed, _nBebes * 5 + 2, 31) * MathHelper.TwoPi;
            float dist = (0.25f + 0.55f * VFXCore.Hash01(Seed, _nBebes * 5 + 3, 37)) * RadioActual();
            _bebes[_nBebes] = new Bebe { Nace = _edadInst, Ang = ang, Dist = dist, Pop = false };
            _nBebes++;
        }

        /// <summary>La posición viva de la estrellita k (deriva con la nube).</summary>
        private Vector2 PosBebe(int k)
        {
            float giro = _edadInst * 0.004f;
            return Projectile.Center + new Vector2(
                MathF.Cos(_bebes[k].Ang + giro), MathF.Sin(_bebes[k].Ang + giro)) * _bebes[k].Dist;
        }

        /// <summary>EL MINI-POP: ×0.8 radio 60 con luz + destello + chispas.</summary>
        private void Reventar(int k)
        {
            _bebes[k].Pop = true;
            Vector2 pos = PosBebe(k);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dmg = Math.Max(1, (int)(Projectile.damage * 0.8f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    float alcance = PopRadio + Math.Max(npc.width, npc.height) * 0.5f;
                    if (Vector2.DistanceSquared(npc.Center, pos) > alcance * alcance) continue;
                    npc.SimpleStrikeNPC(dmg, npc.direction, false, 1.5f, DamageClass.Magic);
                }
            }

            if (Main.netMode != NetmodeID.Server)
            {
                _popPos[_popN] = pos;
                _popAge[_popN] = 0f;
                _popN = (_popN + 1) % 3;
                Lighting.AddLight(pos, 0.7f, 0.55f, 0.9f);
                try
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item93.WithPitchOffset(0.35f), pos);
                }
                catch { }
                RiftLib.ChispasAnomalia(pos, 6, PaletaEstrellas, Seed + k * 13, out ParticleData[] motas);
                if (motas != null)
                    for (int i = 0; i < motas.Length; i++)
                        ParticleManager.Spawn(motas[i]);
            }
        }

        private void LuzNebulosa(float radio)
        {
            float k = MathHelper.Clamp(radio / RadioFin, 0.2f, 1f);
            float latido = 0.8f + 0.2f * MathF.Sin(_age * 0.12f + Seed);
            Lighting.AddLight(Projectile.Center, 0.30f * k * latido, 0.22f * k * latido, 0.40f * k * latido);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // LA DISIPACIÓN: la nebulosa muere en polvo de estrellas.
            RiftLib.ChispasAnomalia(Projectile.Center, 16, PaletaEstrellas,
                Seed + 9, out ParticleData[] motas);
            if (motas != null)
                for (int i = 0; i < motas.Length; i++)
                    ParticleManager.Spawn(motas[i]);
            OndaLib.Flash(ColorVioleta, 0.10f, 8);
            OndaLib.Kick(1.2f, 8);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            // ============================================================
            //  CONTRATO DE BATCH v6.10: cerrar, dibujar, restaurar.
            // ============================================================
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try { DrawNebulosa(); }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
                RestauraBatch();
            return false;
        }

        private static void RestauraBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        private void DrawNebulosa()
        {
            float time = Main.GlobalTimeWrappedHourly;
            int seed = Seed;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float radio = _instalada ? RadioActual() : RadioIni * 0.45f;
            float fade = MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f);

            // ============================================================
            //  1. LA MASA — tres nubes de BrumaFX (los colores mezclados).
            // ============================================================
            BrumaFX.BeginMass();
            BrumaFX.Cloud(drawPos, radio * 0.85f, ColorMagenta, seed + 11, time, 5, 0.40f * fade);
            BrumaFX.Cloud(drawPos, radio * 0.85f, ColorCian, seed + 211, time, 5, 0.34f * fade);
            BrumaFX.Cloud(drawPos, radio * 0.85f, ColorVioleta, seed + 411, time, 5, 0.40f * fade);
            Main.spriteBatch.End();

            // ============================================================
            //  2. EL BRILLO — el corazón, las motas y las estrellitas.
            // ============================================================
            BrumaFX.BeginGlow();

            var glowSize = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height);

            // El corazón tenue de la nube.
            LumenLib.Bloom(Main.spriteBatch, drawPos, radio * 0.35f, ColorVioleta, 0.20f * fade, 3);

            // Las motas de estrellas deterministas dentro de la nube.
            for (int i = 0; i < 7; i++)
            {
                float ang = VFXCore.Hash01(seed, 700 + i, 41) * MathHelper.TwoPi + time * 0.05f;
                float d = (0.2f + 0.7f * VFXCore.Hash01(seed, 710 + i, 43)) * radio * 0.8f;
                Vector2 p = drawPos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * d;
                float tw = 0.35f + 0.65f * MathF.Abs(MathF.Sin(time * 2.6f + i * 2.1f + seed));
                Main.spriteBatch.Draw(VFXCore.SoftGlow, p, null,
                    Tint(PaletaEstrellas[i % 3], 0.30f * tw * fade), 0f, glowSize * 0.5f,
                    new Vector2(7f, 7f) / glowSize, SpriteEffects.None, 0f);
            }

            if (_instalada)
            {
                // LAS ESTRELLITAS: cargan 30 ticks, cada vez más blancas.
                for (int k = 0; k < _nBebes; k++)
                {
                    if (_bebes[k].Pop) continue;
                    float t = MathHelper.Clamp((_edadInst - _bebes[k].Nace) / (float)BebeTicks, 0f, 1f);
                    Vector2 p = PosBebe(k) - Main.screenPosition;
                    float tam = 5f + 9f * t;
                    float pulso = 0.55f + 0.45f * MathF.Sin(time * 8f + k * 2.7f + t * 20f);
                    Color c = PaletaEstrellas[k % 3];

                    // la cruz de 4 puntas
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, p, null,
                        Tint(c, 0.55f * pulso * fade), 0f, glowSize * 0.5f,
                        new Vector2(tam * 3.2f, tam * 0.8f) / glowSize, SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, p, null,
                        Tint(c, 0.55f * pulso * fade), MathHelper.PiOver2, glowSize * 0.5f,
                        new Vector2(tam * 3.2f, tam * 0.8f) / glowSize, SpriteEffects.None, 0f);
                    // el corazón que se enciende
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, p, null,
                        Tint(Color.White, 0.75f * t * fade), 0f, glowSize * 0.5f,
                        new Vector2(tam, tam) / glowSize, SpriteEffects.None, 0f);
                }

                // LOS MINI-POPS: anillos que se abren y mueren.
                for (int i = 0; i < 3; i++)
                {
                    if (_popAge[i] >= 10f) continue;
                    float pr = _popAge[i] / 10f;
                    OndaLib.Pulse(Main.spriteBatch, _popPos[i] - Main.screenPosition, pr, 46f,
                        PaletaEstrellas[i % 3], 0.55f * (1f - pr), seed + i * 17);
                }
            }

            Main.spriteBatch.End();
        }

        /// <summary>Tinte premultiplicado de la casa (v6.25).</summary>
        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(
                (byte)(int)(c.R * f), (byte)(int)(c.G * f), (byte)(int)(c.B * f),
                (byte)(int)(255f * f));
        }
    }
}
