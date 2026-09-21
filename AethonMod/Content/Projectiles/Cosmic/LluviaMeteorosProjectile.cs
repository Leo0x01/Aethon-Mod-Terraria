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
    /// LluviaMeteorosProjectile — LA LLUVIA DE METEOROS (el director).
    ///
    /// Un proyectil INVISIBLE clavado en el punto del cursor: durante
    /// 30 ticks dibuja EL TELÉGRAFO — un círculo fino ámbar de 220 px
    /// con un anillo interior que se CIERRA y las 8 marcas de impacto
    /// deterministas — y luego ESCALONA los 8 meteoros (uno cada 4
    /// ticks) naciendo 420 px ARRIBA de sus puntos, en diagonal. Él no
    /// daña: solo coordina (los meteoros son hijos sincronizados).
    /// </summary>
    public class LluviaMeteorosProjectile : ModProjectile
    {
        // === LA CRONOLOGÍA (ticks) ===
        private const int TelegraphTicks = 30;   // el aviso en el suelo
        private const int Meteoros = 8;          // piedras por lluvia
        private const int EscalonTicks = 4;      // una piedra cada 4 ticks
        private const float Radio = 220f;        // radio de la zona
        private const float AlturaCaida = 420f;  // nacen esta distancia arriba
        private const float VelY = 15f;          // caída vertical
        private const float VelX = 3.2f;         // deriva diagonal

        // === LA PALETA (ámbar de aviso) ===
        private static readonly Color ColorAmbar = new(255, 196, 96);
        private static readonly Color ColorAmbarClaro = new(255, 236, 180);

        private float _age;
        private bool _nacio;
        private int _spawneados;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            // Daño 100% manual (escuela A): el director no toca a nadie.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TelegraphTicks + Meteoros * EscalonTicks + 8;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
        }

        /// <summary>Semilla determinista (identidad de red).</summary>
        private int Seed => Math.Max(1, Projectile.identity + 59);

        /// <summary>El punto de aterrizaje determinista del meteoro i (dentro del radio).</summary>
        private Vector2 PuntoMeteoro(int i)
        {
            float ang = VFXCore.Hash01(Seed, i * 3 + 1, 11) * MathHelper.TwoPi;
            float d = MathF.Sqrt(VFXCore.Hash01(Seed, i * 3 + 2, 17)) * Radio;
            return Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * d;
        }

        /// <summary>El lado de la diagonal del meteoro i (determinista).</summary>
        private float LadoMeteoro(int i)
            => VFXCore.Hash01(Seed, i * 3 + 3, 23) < 0.5f ? -1f : 1f;

        public override void AI()
        {
            _age += 1f;

            if (!_nacio)
            {
                _nacio = true;
                Projectile.velocity = Vector2.Zero;
            }

            // La luz de aviso ámbar late mientras vive el telégrafo.
            float pulso = 0.7f + 0.3f * MathF.Sin(_age * 0.42f + Seed);
            Lighting.AddLight(Projectile.Center, 0.42f * pulso, 0.28f * pulso, 0.07f * pulso);

            if (_age < TelegraphTicks) return;

            // ============================================================
            //  EL ESCALONADO: 8 meteoros, uno cada 4 ticks (solo el
            //  server/SP spawnea — los hijos viajan sincronizados).
            // ============================================================
            int deberia = Math.Min(Meteoros, (int)(_age - TelegraphTicks) / EscalonTicks + 1);
            while (_spawneados < deberia)
            {
                SpawnMeteoro(_spawneados);
                _spawneados++;
            }

            if (_spawneados >= Meteoros && _age >= TelegraphTicks + Meteoros * EscalonTicks + 4)
                Projectile.Kill();
        }

        /// <summary>EL METEORO i: nace 420 px arriba de su punto, cayendo en diagonal.</summary>
        private void SpawnMeteoro(int i)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            Vector2 aterrizaje = PuntoMeteoro(i);
            Vector2 vel = new Vector2(VelX * LadoMeteoro(i), VelY);
            float tCaida = AlturaCaida / VelY;
            Vector2 origen = aterrizaje - vel * tCaida;

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), origen, vel,
                ModContent.ProjectileType<MeteoroProjectile>(), Projectile.damage, 3f,
                Projectile.owner, i);
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

            try { DrawTelegrafo(); }
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

        /// <summary>EL TELÉGRAFO: círculo fino ámbar + anillo que se cierra + las 8 marcas.</summary>
        private void DrawTelegrafo()
        {
            float time = Main.GlobalTimeWrappedHourly;
            int seed = Seed;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            float t = MathHelper.Clamp(_age / TelegraphTicks, 0f, 1f);
            // El telégrafo se apaga suavemente cuando ya caen las piedras.
            float fade = _age <= TelegraphTicks ? 1f
                : MathHelper.Clamp(1f - (_age - TelegraphTicks) / 12f, 0f, 1f);
            if (fade <= 0.01f) return;

            // La urgencia: late cada vez más rápido cerca de la hora.
            float urgencia = 0.6f + 0.4f * MathF.Sin(time * (4f + 10f * t) + seed);

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            var ringSize = new Vector2(VFXCore.Ring.Width, VFXCore.Ring.Height);
            var glowSize = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height);

            // 1. EL CÍRCULO FINO de la zona (220 px), girando despacio.
            Main.spriteBatch.Draw(VFXCore.Ring, drawPos, null,
                Tint(ColorAmbar, 0.40f * urgencia * fade), time * 0.7f,
                ringSize * 0.5f, VFXCore.RingQuadSize(Radio) / ringSize, SpriteEffects.None, 0f);

            // 2. EL ANILLO QUE SE CIERRA: contrae al centro mientras llega la hora.
            float cierre = Radio * (1f - 0.9f * t);
            if (cierre > 10f)
                Main.spriteBatch.Draw(VFXCore.Ring, drawPos, null,
                    Tint(ColorAmbarClaro, 0.30f * urgencia * fade), -time * 1.6f,
                    ringSize * 0.5f, VFXCore.RingQuadSize(cierre) / ringSize, SpriteEffects.None, 0f);

            // 3. LAS 8 MARCAS de impacto deterministas (donde caerá cada piedra).
            for (int i = 0; i < Meteoros; i++)
            {
                Vector2 p = PuntoMeteoro(i) - Main.screenPosition;
                float tw = 0.5f + 0.5f * MathF.Sin(time * 6f + i * 2.4f + seed);
                Main.spriteBatch.Draw(VFXCore.SoftGlow, p, null,
                    Tint(ColorAmbarClaro, 0.35f * tw * fade), 0f, glowSize * 0.5f,
                    new Vector2(10f, 10f) / glowSize, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(VFXCore.Ring, p, null,
                    Tint(ColorAmbar, 0.22f * fade), time * 0.9f + i,
                    ringSize * 0.5f, VFXCore.RingQuadSize(22f) / ringSize, SpriteEffects.None, 0f);
            }

            // 4. EL CORAZÓN de la zona.
            LumenLib.Bloom(Main.spriteBatch, drawPos, 26f, ColorAmbar, 0.35f * urgencia * fade, 2);

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
