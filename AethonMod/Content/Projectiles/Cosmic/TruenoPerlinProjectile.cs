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
    /// TruenoPerlinProjectile — v6.50.17 — EL ARCO ELÉCTRICO PERLIN.
    ///
    /// El punto de descarga del Cetro del Trueno Perlin: un ancla viva
    /// sobre el objetivo de la que CUELGA el arco generado con RUIDO
    /// (StormLib.PerlinBolt — fBm 3 octavas con fase resbalante: el canal
    /// meandra y serpentrea en vez de re-germinarse entero).
    ///
    /// LA VIDA DEL ARCO:
    ///   1. NACE en el cursor (clampeado al alcance del cetro) y durante
    ///      10 ticks SIGUE AL CURSOR (easing suave): arrastras la descarga
    ///      con la mirada — el rayo PERSEGUIDOR de los arcos eléctricos.
    ///   2. EL GOLPE llega al instante de nacer (sin telegraph — la firma
    ///      de ESTE arma frente al Cetro del Trueno Rúnico): radio 100 +
    ///      CADENA a 2 enemigos (60%) + Electrified.
    ///   3. MUERTE VIOLENTA: el DeathGrow revienta el meandro mientras el
    ///      alfa cae — el arco se desharbe RETORCIÉNDOSE, no educadamente.
    /// </summary>
    public class TruenoPerlinProjectile : ModProjectile
    {
        /// <summary>Ticks que el ancla sigue al cursor.</summary>
        private const int TicksSiguiendo = 10;

        /// <summary>Vida total del arco.</summary>
        private const int TotalLife = 30;

        /// <summary>Regeneración del canal (Hz).</summary>
        private const float FlickHz = 15f;

        /// <summary>Alcance máximo de la persecución del cursor (px).</summary>
        private const float MaxRange = 520f;

        private Vector2 _muzzle;        // la punta del bastón (coords de MUNDO)
        private bool _anchored;

        // === LAS CADENAS (hasta 2 saltos desde el ancla) ===
        private readonly Vector2[] _chainTo = new Vector2[2];
        private readonly bool[] _chainAlive = new bool[2];

        private float _age;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            // El daño se aplica a mano (golpe en radio + cadena): sin
            // daño de contacto de vanilla.
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalLife;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        /// <summary>Semilla determinista del arco.</summary>
        private int Seed => Math.Max(1, Projectile.identity + 13);

        public override void AI()
        {
            _age += 1f;

            // === EL ANCLAJE: el proyectil ES el punto de descarga ===
            if (!_anchored)
            {
                _anchored = true;
                _muzzle = MuzzleDelDueno();
                Projectile.velocity = Vector2.Zero;
                Strike();

                if (Main.netMode != NetmodeID.Server)
                {
                    // EL ZAP: chasquido eléctrico agudo + trueno corto.
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item12.WithPitchOffset(0.35f).WithVolumeScale(1.05f),
                        Projectile.Center);
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item122.WithPitchOffset(0.3f).WithVolumeScale(0.55f),
                        Projectile.Center);

                    // Chispas eléctricas del punto de descarga.
                    for (int i = 0; i < 16; i++)
                    {
                        float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Electric,
                            new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) *
                            Main.rand.NextFloat(2f, 7f),
                            220, new Color(200, 235, 255), 1.2f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                }
            }

            // === LA PERSECUCIÓN DEL CURSOR (los primeros 10 ticks): el
            //     ancla EASEA hacia la mirada — arrastras el arco. ===
            if (_age < TicksSiguiendo && Main.netMode != NetmodeID.Server)
            {
                Player dueno = Main.player[Projectile.owner];
                if (dueno != null && dueno.active && !dueno.dead)
                {
                    Vector2 deseado = Main.MouseWorld;
                    Vector2 desdeDueno = deseado - dueno.Center;
                    float len = desdeDueno.Length();
                    if (len > MaxRange)
                        deseado = dueno.Center + desdeDueno * (MaxRange / len);
                    // easing 0.35: persigue vivo pero sin pegarse al cursor.
                    Projectile.Center = Vector2.Lerp(Projectile.Center, deseado, 0.35f);
                }
            }

            // LA BOCA DEL BASTÓN sigue viva (el dueño puede moverse).
            _muzzle = MuzzleDelDueno();

            // === LA LUZ (estrangulada: cada 3 ticks, a lo largo del canal) ===
            if (_age % 3f == 0f)
            {
                float amp = 0.12f * StormLib.DeathGrow(VidaT());
                // el camino perlin estimado para la luz (barato: 12 puntos).
                Vector2[] camino = CaminoPerlin(12, amp);
                StormLib.AddLightAlong(camino, 0.55f, 0.75f, 1f, 0.8f, 90f);
            }
        }

        /// <summary>La vida normalizada 0..1.</summary>
        private float VidaT()
            => MathHelper.Clamp(_age / TotalLife, 0f, 1f);

        /// <summary>La punta del bastón del dueño (su Center si algo falla).</summary>
        private Vector2 MuzzleDelDueno()
        {
            Player dueno = Main.player[Projectile.owner];
            if (dueno != null && dueno.active && !dueno.dead)
            {
                Vector2 hacia = Projectile.Center - dueno.Center;
                if (hacia.LengthSquared() < 0.01f) hacia = Vector2.UnitX;
                hacia.Normalize();
                return dueno.itemLocation + hacia * 8f;
            }
            return _muzzle != Vector2.Zero ? _muzzle : Projectile.Center;
        }

        /// <summary>
        /// EL GOLPE: radio 100 en el ancla + cadena a 2 enemigos (60%) +
        /// Electrified. v6.50 — GolpeMotor (el cauce del motor: crítica
        /// real, varianza, on-hit y sync MP — resuelto en el cliente dueño).
        /// </summary>
        private void Strike()
        {
            var golpeados = new System.Collections.Generic.List<NPC>();

            const float BurstR = 100f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                if ((npc.Center - Projectile.Center).Length() > BurstR) continue;

                Content.Systems.GolpeMotor.Golpear(Projectile, npc, Projectile.damage, 2f, true);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    try { npc.AddBuff(BuffID.Electrified, 300); } catch { }
                golpeados.Add(npc);
            }

            // === LA CADENA: hasta 2 saltos (60% del daño) ===
            int chainDamage = Math.Max(1, (int)(Projectile.damage * 0.60f));
            Vector2 origin = Projectile.Center;
            for (int c = 0; c < 2; c++)
            {
                NPC best = null;
                float bestDist = 280f;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc) || golpeados.Contains(npc)) continue;
                    float dist = (npc.Center - origin).Length();
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = npc;
                    }
                }
                if (best == null) { _chainAlive[c] = false; continue; }

                _chainTo[c] = best.Center;
                _chainAlive[c] = true;
                origin = best.Center;

                Content.Systems.GolpeMotor.Golpear(Projectile, best, chainDamage, 1.2f, true);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    try { best.AddBuff(BuffID.Electrified, 300); } catch { }
                golpeados.Add(best);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!_anchored) return false;

            // CONTRATO DE BATCH (el de la casa): durante PreDraw el batch
            // de tML está ABIERTO; cerrarlo, abrir el pase aditivo, y
            // devolverlo ABIERTO al salir.
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                DrawArco();
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }

            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        // ------------------------------------------------------------------
        //  EL DIBUJO DEL ARCO PERLIN
        // ------------------------------------------------------------------

        private static readonly Color ArcoHalo = new(120, 190, 255);     // celeste eléctrico
        private static readonly Color ArcoMedio = new(170, 225, 255);   // celeste claro
        private static readonly Color BlancoCaliente = new(255, 252, 245);

        private void DrawArco()
        {
            try
            {
                float time = Main.GlobalTimeWrappedHourly;
                int seed = Seed;
                int flick = StormLib.FlickTick(time, FlickHz);
                Vector2 screen = Main.screenPosition;
                Vector2 boca = _muzzle - screen;
                Vector2 ancla = Projectile.Center - screen;

                float lifeT = VidaT();
                float fade = 1f - MathHelper.Clamp((lifeT - 0.40f) / 0.60f, 0f, 1f);
                if (fade <= 0.02f) return;

                // EL PARPADEO con apagado (la casa): encendido 7/10.
                bool lit = StormLib.IsLit(seed, flick, 0.70f);
                float alpha = lit ? 1f : 0.15f;

                // LA MUERTE VIOLENTA: el meandro REVIENTA al disolverse
                // (ampFrac crece con DeathGrow).
                float ampFrac = 0.12f * StormLib.DeathGrow(lifeT);

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // === 1. EL ARCO PERLIN (el canal que meandra del bastón al ancla) ===
                StormLib.PerlinBolt(Main.spriteBatch, boca, ancla, seed, flick,
                    7.5f, ArcoHalo, BlancoCaliente, alpha * fade, ampFrac);

                // === 2. LAS CADENAS (arcos perlin cortos hacia los saltos) ===
                for (int c = 0; c < 2; c++)
                {
                    if (!_chainAlive[c]) continue;
                    if (!StormLib.IsLit(seed + 47 + c * 31, flick, 0.60f)) continue;
                    StormLib.PerlinBolt(Main.spriteBatch, ancla, _chainTo[c] - screen,
                        seed + 47 + c * 31, flick, 3.6f, ArcoMedio, BlancoCaliente,
                        0.85f * alpha * fade, 0.10f);
                }

                // === 3. EL ANCLA: el núcleo de descarga en el objetivo ===
                Texture2D texAncla = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Projectiles/Cosmic/TruenoPerlinProjectile").Value;
                float pulso = 0.80f + 0.20f * MathF.Sin(time * 22f);
                Main.spriteBatch.Draw(texAncla, ancla, null,
                    TintFijo(ArcoMedio, 0.9f * alpha * fade * pulso), 0f,
                    texAncla.Size() * 0.5f, 1.5f * pulso, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(texAncla, ancla, null,
                    TintFijo(BlancoCaliente, 0.95f * alpha * fade), 0f,
                    texAncla.Size() * 0.5f, 0.8f, SpriteEffects.None, 0f);

                // === 4. EL ESTALLIDO DE IMPACTO (los primeros ticks) ===
                float flashI = MathHelper.Clamp(1f - _age / 6f, 0f, 1f);
                if (flashI > 0f)
                    StormLib.ImpactFlash(Main.spriteBatch, ancla, 46f * (0.6f + 0.4f * flashI),
                        ArcoMedio, flashI * alpha * fade, time * 1.1f);

                // === 5. LOS ARCOS DE CORONA (crispados alrededor del ancla) ===
                if (StormLib.IsLit(seed + 90, flick, 0.65f))
                {
                    float arcR = 18f + 26f * lifeT;
                    StormLib.ArcRing(Main.spriteBatch, ancla, arcR,
                        time * 2.4f, time * 2.4f + 1.7f, seed + 90, flick, 2.8f,
                        TintFijo(ArcoHalo, 0.6f * fade), TintFijo(BlancoCaliente, 0.9f * fade),
                        1f, 8);
                }

                Main.spriteBatch.End();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
        }

        /// <summary>
        /// EL CAMINO PERLIN estimado (para AddLightAlong — el mismo modelo
        /// del PerlinBolt a baja resolución: meandro + arco con envolvente).
        /// </summary>
        private Vector2[] CaminoPerlin(int n, float ampFrac)
        {
            Vector2 start = _muzzle;
            Vector2 end = Projectile.Center;
            var pts = new Vector2[n + 1];
            Vector2 delta = end - start;
            float len = delta.Length();
            if (len < 1f)
            {
                for (int i = 0; i <= n; i++) pts[i] = start;
                return pts;
            }
            Vector2 dir = delta / len;
            Vector2 perp = new Vector2(-dir.Y, dir.X);
            int flick = StormLib.FlickTick(Main.GlobalTimeWrappedHourly, FlickHz);
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                float m = (StormLib.Fbm1D(t * 2.8f + flick * 0.15f, Seed, 3) - 0.5f) * 2f;
                float arco = (StormLib.Ruido1D(t * 0.5f + Seed * 0.013f, Seed + 55) - 0.5f) * 2f;
                float d = t - 0.5f;
                float d5 = d * d * d * d * d;
                float env = MathF.Exp(-5000f * d5 * d5);
                float off = (m * 0.62f * ampFrac + arco * 0.45f * ampFrac) * len;
                pts[i] = start + dir * (len * t) + perp * (off * env);
            }
            pts[0] = start;
            pts[n] = end;
            return pts;
        }

        private static Color TintFijo(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }

        public override void OnKill(int timeLeft)
        {
            // chispas de despedida en el ancla.
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 8; i++)
            {
                float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Electric,
                    new Vector2((float)Math.Cos(ang) * Main.rand.NextFloat(1.5f, 4.5f),
                                (float)Math.Sin(ang) * Main.rand.NextFloat(1.5f, 4.5f)),
                    200, new Color(190, 230, 255), 1.0f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }
    }
}
