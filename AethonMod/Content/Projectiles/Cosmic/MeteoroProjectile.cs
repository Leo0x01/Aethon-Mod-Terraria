using System;
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
    /// MeteoroProjectile — LA PIEDRA QUE CAE (hija de la Lluvia).
    ///
    /// Nace 420 px ARRIBA del punto determinista que le marcó el director
    /// y cae en DIAGONAL constante. Lleva una LENGUA DE FUEGO (PyraLib)
    /// anclada en la cabeza apuntando CONTRA la caída + ascuas que va
    /// soltando. Al tocar suelo o enemigo EXPLOTA: onda pequeña + chispas,
    /// daño ×1.0 radio 70 + Quemadura Cósmica 3 s.
    /// </summary>
    public class MeteoroProjectile : ModProjectile
    {
        /// <summary>Radio del impacto.</summary>
        private const float RadioExplosion = 70f;

        /// <summary>Ticks de la animación de la explosión.</summary>
        private const int BoomTicks = 16;

        /// <summary>Vida de vuelo (failsafe si nunca toca nada).</summary>
        private const int VidaTicks = 80;

        // === LA PALETA (fuego solar) ===
        private static readonly Color ColorAmbar = new(255, 190, 90);
        private static readonly Color ColorBlanco = new(255, 250, 230);

        private float _age;
        private bool _nacio;
        private int _idx;
        private bool _explotada;
        private float _explAge;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            // Daño 100% manual (escuela A): sin contacto de vanilla.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = VidaTicks;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        /// <summary>Semilla determinista (identidad de red).</summary>
        private int Seed => Math.Max(1, Projectile.identity + 61);

        public override void AI()
        {
            _age += 1f;

            if (!_nacio)
            {
                _nacio = true;
                _idx = (int)Projectile.ai[0];
            }

            if (!_explotada)
            {
                Volar();
            }
            else
            {
                // ========================================================
                //  EL IMPACTO: la onda crece y muere (24→16 ticks).
                // ========================================================
                _explAge += 1f;
                if (_explAge >= BoomTicks)
                    Projectile.Kill();
            }
        }

        /// <summary>LA CAÍDA: diagonal constante hasta tocar suelo o enemigo.</summary>
        private void Volar()
        {
            // La luz cálida del trozo de sol que cae.
            PyraLib.Light(Projectile.Center, 110f, PyraPalettes.SolarFire, 0.9f);

            // Las ascuas que va soltando (deterministas, cliente).
            if (Main.netMode != NetmodeID.Server && (int)_age % 5 == 0)
            {
                PyraLib.Sparks(Projectile.Center, -Projectile.velocity, 2, PyraPalettes.SolarFire,
                    Seed + (int)_age, out ParticleData[] ascuas);
                if (ascuas != null)
                    for (int i = 0; i < ascuas.Length; i++)
                        ParticleManager.Spawn(ascuas[i]);
            }

            bool tocaPared = Collision.SolidCollision(Projectile.Center - Vector2.One * 6f, 12, 12);
            bool tocaEnemigo = false;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                float alcance = 16f + Math.Max(npc.width, npc.height) * 0.5f;
                if (Vector2.DistanceSquared(npc.Center, Projectile.Center) < alcance * alcance)
                {
                    tocaEnemigo = true;
                    break;
                }
            }

            if (tocaPared || tocaEnemigo || _age >= VidaTicks - 2f)
                Explotar();
        }

        /// <summary>EL IMPACTO: ×1.0 radio 70 + Quemadura Cósmica 3 s + la onda pequeña.</summary>
        private void Explotar()
        {
            _explotada = true;
            _explAge = 0f;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = BoomTicks + 2;

            // v6.50 — GolpeMotor (el cauce del motor: crítica real, varianza,
            // on-hit y sync MP del propio motor); la quemadura, server/SP.
            {
                int dmg = Math.Max(1, Projectile.damage);
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc)) continue;
                    float alcance = RadioExplosion + Math.Max(npc.width, npc.height) * 0.5f;
                    if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > alcance * alcance) continue;
                    Content.Systems.GolpeMotor.Golpear(Projectile, npc, dmg, 3f, true);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        try { npc.AddBuff(ModContent.BuffType<Content.Buffs.QuemaduraCosmica>(), 180); } catch { }
                }
            }

            if (Main.netMode != NetmodeID.Server)
            {
                OndaLib.Kick(1.4f, 8);
                OndaLib.Flash(ColorAmbar, 0.10f, 6);
                try
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item14.WithPitchOffset(-0.25f + _idx * 0.06f), Projectile.Center);
                }
                catch { }
                PyraLib.Sparks(Projectile.Center, Vector2.Zero, 14, PyraPalettes.SolarFire,
                    Seed + 5, out ParticleData[] ascuas);
                if (ascuas != null)
                    for (int i = 0; i < ascuas.Length; i++)
                        ParticleManager.Spawn(ascuas[i]);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // ============================================================
            //  CONTRATO DE BATCH v6.10: cerrar, dibujar, restaurar.
            // ============================================================
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try { DrawMeteoro(); }
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

        private void DrawMeteoro()
        {
            float time = Main.GlobalTimeWrappedHourly;
            int seed = Seed;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            if (!_explotada)
            {
                // ========================================================
                //  LA PIEDRA: lengua de fuego CONTRA la caída + cabeza
                //  blanca-caliente con halo ámbar.
                // ========================================================
                if (Projectile.velocity.LengthSquared() > 0.5f)
                {
                    Vector2 atras = -Vector2.Normalize(Projectile.velocity);
                    PyraLib.Tongue(Main.spriteBatch, drawPos, 54f, 15f, PyraPalettes.SolarFire,
                        0.92f, seed, time, 0.9f, 0f, 1f, atras.ToRotation() + MathHelper.PiOver2);
                }

                LumenLib.Bloom(Main.spriteBatch, drawPos, 30f, ColorAmbar, 0.75f, 3);

                var orbSize = new Vector2(VFXCore.GlowOrb.Width, VFXCore.GlowOrb.Height);
                Main.spriteBatch.Draw(VFXCore.GlowOrb, drawPos, null,
                    Tint(ColorBlanco, 0.92f), 0f, orbSize * 0.5f,
                    new Vector2(15f, 15f) / orbSize, SpriteEffects.None, 0f);
            }
            else
            {
                // ========================================================
                //  EL IMPACTO: onda de choque pequeña ámbar-blanca.
                // ========================================================
                float progreso = MathHelper.Clamp(_explAge / BoomTicks, 0f, 1f);
                float caida = 1f - progreso * progreso;

                OndaLib.Shock(Main.spriteBatch, drawPos, progreso, RadioExplosion,
                    ColorAmbar, 0.85f * caida, seed, 9f);
                OndaLib.Shock(Main.spriteBatch, drawPos, MathHelper.Clamp(progreso * 1.15f, 0f, 1f),
                    RadioExplosion * 0.66f, ColorBlanco, 0.55f * caida, seed + 7, 5f);

                LumenLib.Bloom(Main.spriteBatch, drawPos,
                    RadioExplosion * (0.35f + 0.65f * caida), ColorAmbar, 0.40f * caida, 3);
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
