using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Players;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// TajoGuadanaProjectile — v6.42 — APUESTA 3: LA GUADAÑA DEL DESGARRO.
    ///
    /// EL CRECIENTE: el tajo de la guadaña (TajoLib — el creciente de la
    /// casa, nacido para los tajos del anime): vuela 220 px en la
    /// dirección del tajo y DONDE muere (impacto, alcance o tile) abre
    /// LA FISURA — la herida vertical que queda en el mundo.
    ///
    /// EL HAMBRE: si el jugador traga daño devorado por una fisura
    /// anterior, el creciente nace TEÑIDO de carmesí profundo y con
    /// cuentas de apetito orbitando la hoja (el daño extra se aplicó
    /// en el disparo — esto es la lectura visual).
    /// </summary>
    public class TajoGuadanaProjectile : ModProjectile
    {
        public const float AlcanceTajo = 220f;

        private float _recorrido;

        private int Seed => Math.Max(1, Projectile.identity + 859);

        private static readonly Color HaloTajo = new(255, 120, 90);
        private static readonly Color NucleoTajo = new(255, 240, 230);
        private static readonly Color HaloHambre = new(200, 20, 40);
        private static readonly Color NucleoHambre = new(255, 140, 120);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.tileCollide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.aiStyle = -1;
        }

        public override void AI()
        {
            _recorrido += Projectile.velocity.Length();
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Vector3(0.9f, 0.3f, 0.25f) * 0.8f);

            if (_recorrido >= AlcanceTajo)
                AbrirFisura();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            AbrirFisura();
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // La fisura nace DONDE mordió el tajo (la causalidad del arma).
            AbrirFisura();
        }

        public override void OnKill(int timeLeft)
        {
            // Muerte de verdad (agotó tiempo sin abrir): también hiere.
            if (_recorrido < AlcanceTajo)
                AbrirFisura();
        }

        private void AbrirFisura()
        {
            if (Projectile.ai[0] > 0.5f) return;    // ya abrió (guard de reentrada)
            Projectile.ai[0] = 1f;

            if (Main.netMode != NetmodeID.Server)
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.4f, Pitch = -0.3f },
                    Projectile.Center);

            if (Main.myPlayer == Projectile.owner || Main.netMode == NetmodeID.SinglePlayer)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(),
                    Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<FisuraDesgarroProjectile>(),
                    Projectile.damage, 0f, Projectile.owner);
            }
            Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // EL CRECIENTE: arco de ±0.55 rad mirando al vuelo.
                float edad = Math.Min(1f, (60 - Projectile.timeLeft) / 8f);   // reveal
                bool hambre = Main.player[Projectile.owner].GetModPlayer<ApuestasPlayer>()
                    .HambreGuadana > 0;
                TajoLib.Tajo(Projectile.Center - Main.screenPosition, 52f,
                    Projectile.rotation - 0.55f, Projectile.rotation + 0.55f,
                    edad, 1f, 15f,
                    hambre ? HaloHambre : HaloTajo,
                    hambre ? NucleoHambre : NucleoTajo,
                    Seed, Main.GlobalTimeWrappedHourly);

                // LAS CUENTAS DE APETITO: el hambre orbita la hoja.
                if (hambre)
                {
                    Texture2D glow = VFXCore.SoftGlow;
                    int cuentas = Math.Min(6, 1 + Main.player[Projectile.owner]
                        .GetModPlayer<ApuestasPlayer>().HambreGuadana / 25);
                    for (int i = 0; i < cuentas; i++)
                    {
                        float ang = Main.GlobalTimeWrappedHourly * 4f + i / (float)cuentas * MathHelper.TwoPi;
                        Vector2 orbita = Projectile.Center - Main.screenPosition +
                            new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.6f) * 58f;
                        Main.spriteBatch.Draw(glow, orbita, null, HaloHambre * 0.8f,
                            ang, glow.Size() * 0.5f, new Vector2(9f, 9f) / glow.Size(),
                            SpriteEffects.None, 0f);
                    }
                }

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            if (wasActive)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }

    /// <summary>
    /// FisuraDesgarroProjectile — v6.42 — LA FISURA VERTICAL DE REALIDAD.
    ///
    /// LA HERIDA que el tajo deja en el mundo: una grieta carmesí de
    /// 16×128 px que vive 120 ticks (3·τ de la relajación de fractura
    /// — el "2 segundos" del diseño son la física hablando) y hace DOS
    /// cosas:
    ///
    ///   · MUERDE: todo lo que cruza su banda (±32 px de la línea)
    ///     recibe 0,3× cada 10 ticks (la zona de daño persistente).
    ///   · TRAGA: los proyectiles ENEMIGOS que la cruzan desaparecen
    ///     — la fisura se los come y su daño SE SUMA al próximo tajo
    ///     del portador (EL HAMBRE, con tope de 2× el daño del arma:
    ///     devorar spam no puede trivializar a los jefes).
    ///
    /// El borde rasgado con dientes congelados (CaminoDesgarro — la
    /// costura coincide diente a diente) y las chispas de anomalía.
    /// </summary>
    public class FisuraDesgarroProjectile : ModProjectile
    {
        public const int Vida = 120;
        public const float LargoFisura = 128f;
        public const float Banda = 32f;

        /// <summary>Cooldowns de mordida por NPC (una por 10 ticks).</summary>
        private readonly Dictionary<int, int> _mordidas = new();

        private int _edad;

        private int Seed => Math.Max(1, Projectile.identity + 1049);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 128;
            Projectile.tileCollide = false;
            Projectile.friendly = false;        // la mordida es manual (SimpleStrikeNPC)
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.aiStyle = -1;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            _edad++;
            Projectile.velocity = Vector2.Zero;
            Lighting.AddLight(Projectile.Center, new Vector3(0.7f, 0.12f, 0.15f) * 0.7f);

            // La banda de la fisura: de la punta superior a la inferior.
            Vector2 arriba = Projectile.Center - Vector2.UnitY * (LargoFisura * 0.5f);
            Vector2 abajo = Projectile.Center + Vector2.UnitY * (LargoFisura * 0.5f);

            // === LA MORDIDA (autoridad + EsObjetivo, 0,3× cada 10 ticks). ===
            if (Main.myPlayer == Projectile.owner && _edad % 10 == 0)
            {
                int dmg = Math.Max(1, (int)(Projectile.damage * 0.3f));
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                    if (DistanciaASegmento(npc.Center, arriba, abajo) < Banda + npc.width * 0.35f)
                        npc.SimpleStrikeNPC(dmg, npc.Center.X < Projectile.Center.X ? -1 : 1,
                            false, 1.5f, DamageClass.Melee);
                }
            }

            // === LA TRAGA: devorar proyectiles hostiles que crucen. ===
            if ((Main.myPlayer == Projectile.owner || Main.netMode == NetmodeID.SinglePlayer) &&
                _edad % 2 == 0)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p == null || !p.active || !p.hostile || p.friendly || p.damage <= 0) continue;
                    if (DistanciaASegmento(p.Center, arriba, abajo) > Banda) continue;

                    // EL HAMBRE crece (tope 2× el daño del arma — el diseño).
                    ApuestasPlayer ap = Main.player[Projectile.owner].GetModPlayer<ApuestasPlayer>();
                    int tope = Projectile.damage * 2;
                    ap.HambreGuadana = Math.Min(tope, ap.HambreGuadana + p.damage);

                    p.Kill();
                    if (Main.netMode != NetmodeID.Server)
                    {
                        SoundEngine.PlaySound(SoundID.Item94 with { Volume = 0.3f, Pitch = 0.5f },
                            Projectile.Center);
                        for (int k = 0; k < 6; k++)
                        {
                            Dust d = Dust.NewDustPerfect(p.Center, DustID.Crimson);
                            d.velocity = (Projectile.Center - p.Center).SafeNormalize(Vector2.Zero) * 2.5f;
                            d.scale = 1f;
                            d.noGravity = true;
                        }
                    }
                }
            }

            // Las chispas de anomalía de la herida (cada 18 ticks, 2).
            if (Main.netMode != NetmodeID.Server && _edad % 18 == 0)
            {
                float t = VFXCore.Hash01(Seed, _edad, 0) * 2f - 1f;
                Vector2 punto = Projectile.Center + Vector2.UnitY * (t * LargoFisura * 0.5f);
                Dust d = Dust.NewDustPerfect(punto, DustID.CrimsonTorch);
                d.velocity = Vector2.UnitX * (t > 0 ? 1 : -1) * 1.2f;
                d.scale = 0.8f;
                d.noGravity = true;
            }
        }

        /// <summary>Distancia punto-segmento (la matemática de la casa).</summary>
        private static float DistanciaASegmento(Vector2 punto, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Vector2.Dot(punto - a, ab) / Math.Max(1f, ab.LengthSquared());
            t = MathHelper.Clamp(t, 0f, 1f);
            return Vector2.Distance(punto, a + ab * t);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                float time = Main.GlobalTimeWrappedHourly;
                Vector2 arriba = Projectile.Center - Main.screenPosition - Vector2.UnitY * (LargoFisura * 0.5f);
                float progress = _edad / (float)Vida;   // 0→1: la grieta nace y se apaga

                // LA HERIDA rasgada (dientes congelados, costura exacta).
                Vector2[] camino = RiftLib.CaminoDesgarro(arriba, Vector2.UnitY, LargoFisura,
                    Seed, time, 0.55f);

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                RiftLib.Grieta(Main.spriteBatch, camino, progress, 16f, RiftPaletas.Carmesi,
                    0.95f, Seed, time, plano: true, chispas: true);

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            if (wasActive)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
