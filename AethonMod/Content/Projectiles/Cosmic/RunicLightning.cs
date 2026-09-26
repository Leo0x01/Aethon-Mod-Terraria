using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// RunicLightning — v6.21 — EL RAYO DEL CIELO DEL CETRO DEL TRUENO.
    ///
    /// RECONSTRUCCIÓN total del arma de rayos con StormLib (la librería de
    /// la investigación v6.21). La v6.19 disparaba una línea horizontal de
    /// cápsulas borrosas — "eso no son rayos de verdad". Ahora es lo que
    /// el ojo lee como RELÁMPAGO:
    ///
    ///   1. TELEGRAPH (10 ticks): línea fina de aviso del cielo al objetivo
    ///      + anillo pulsante en el punto de impacto. El daño llega SOLO
    ///      al terminar el telegraph.
    ///   2. EL RAYO: MULTI-FILAMENTO que CAE DEL CIELO (ancla ~700-980 px
    ///      por encima, con deriva lateral) — tronco dorado + filamentos
    ///      azul-estelar acompañantes + RAMAS con auto-corrección —
    ///      texturas de FILAMENTO de verdad (núcleo blanco serpenteante
    ///      con grietas), re-generado a 15 Hz con APAGADO intermitente.
    ///   3. DAÑO EN LA COLUMNA: la LÍNEA RECTA cielo→suelo (el jitter es
    ///      cosmético — determinismo MP gratis) + estallido radial en el
    ///      impacto + CADENA a 3 enemigos (60%) + Electrified.
    ///   4. EL IMPACTO: cruz de luz (4 draws) + destello radial girando +
    ///      onda de choque + arcos eléctricos + chispas + trueno grave.
    ///   5. MUERTE VIOLENTA: el jitter REVIENTA (DeathGrow) mientras el
    ///      rayo se disuelve — nunca se desvanece educadamente.
    /// </summary>
    public class RunicLightning : ModProjectile
    {
        /// <summary>Ticks de telegraph antes del golpe.</summary>
        private const int TelegraphTicks = 10;

        /// <summary>Vida total (telegraph + descarga).</summary>
        private const int TotalLife = 34;

        /// <summary>Regeneración del rayo (Hz).</summary>
        private const float FlickHz = 15f;

        // === LA GEOMETRÍA DE LA DESCARGA (coords de MUNDO) ===
        private Vector2 _strike;         // el punto de impacto
        private Vector2 _sky;            // el ancla del cielo
        private bool _anchored;
        private bool _hasStruck;

        // === LAS CADENAS (hasta 3 saltos desde el impacto) ===
        private readonly Vector2[] _chainFrom = new Vector2[3];
        private readonly Vector2[] _chainTo = new Vector2[3];
        private readonly bool[] _chainAlive = new bool[3];

        private float _age;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            // El daño lo aplicamos a mano (columna + estallido + cadena):
            // sin daño de contacto de vanilla.
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

        /// <summary>Semilla determinista de la descarga.</summary>
        private int Seed => Math.Max(1, Projectile.identity + 7);

        public override void AI()
        {
            _age += 1f;

            // === EL ANCLAJE: el proyectil ES el punto de impacto ===
            if (!_anchored)
            {
                _anchored = true;
                _strike = Projectile.Center;
                // El ANCLA DEL CIELO: por encima y a la deriva lateral —
                // el rayo llega ESQUIVADO como las descargas de verdad.
                int seed = Seed;
                float slant = (VFXCore.Hash01(seed, 5, 9001) - 0.5f) * 260f;
                float height = 720f + VFXCore.Hash01(seed, 6, 9002) * 260f;
                _sky = _strike + new Vector2(slant, -height);
                Projectile.velocity = Vector2.Zero;
            }

            // === EL GOLPE: al terminar el telegraph, TODO sucede YA ===
            if (!_hasStruck && _age >= TelegraphTicks)
            {
                _hasStruck = true;
                Strike();

                if (Main.netMode != NetmodeID.Server)
                {
                    // TRUENO: zap grave (pitch −0.45) + zaps agudos en capa.
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item12.WithPitchOffset(-0.45f).WithVolumeScale(1.15f), _strike);
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item93.WithPitchOffset(0.25f).WithVolumeScale(0.65f), _strike);
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item122.WithPitchOffset(-0.10f).WithVolumeScale(0.75f), _strike);

                    // EL ESTALLIDO DE CHISPAS del impacto.
                    for (int i = 0; i < 26; i++)
                    {
                        float ang = Main.rand.NextFloat(0, MathHelper.TwoPi);
                        float spd = Main.rand.NextFloat(3f, 11f);
                        Dust d = Dust.NewDustPerfect(_strike, DustID.Electric,
                            new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang) * 0.7f) * spd,
                            220, new Color(255, 240, 190), 1.3f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                    for (int i = 0; i < 14; i++)
                    {
                        float ang = Main.rand.NextFloat(0, MathHelper.TwoPi);
                        Dust d = Dust.NewDustPerfect(_strike, DustID.YellowTorch,
                            new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) *
                            Main.rand.NextFloat(2f, 7f),
                            200, new Color(255, 210, 120), 1.1f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }

                    // EL PUÑETAZO DE CÁMARA (vertical — la caída del rayo).
                    try
                    {
                        Main.instance.CameraModifiers.Add(
                            new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
                                _strike, new Vector2(0f, 1f), 7f, 10, 16, 0.4f,
                                "AethonSkyBolt"));
                    }
                    catch { }
                }
            }

            // === LA LUZ (estrangulada: cada 3 ticks, a lo largo del camino) ===
            if (_age % 3f == 0f)
            {
                if (_hasStruck)
                {
                    Vector2[] path = StormLib.ZigPath(_sky, _strike, Seed,
                        StormLib.FlickTick(Main.GlobalTimeWrappedHourly, FlickHz), 8, 24f);
                    StormLib.AddLightAlong(path, 0.85f, 0.78f, 0.45f, 0.9f, 90f);
                }
                else
                {
                    // Durante el telegraph: el objetivo "carga" con luz.
                    Lighting.AddLight(_strike, 0.35f, 0.32f, 0.18f);
                }
            }
        }

        /// <summary>EL GOLPE: columna cielo→suelo + estallido + cadena.
        /// v6.50 — GolpeMotor (el cauce del motor: crítica real, varianza,
        /// on-hit y sync MP del propio motor — resuelto en el cliente
        /// dueño; el debuff sigue siendo autoridad).</summary>
        private void Strike()
        {
            var hit = new List<NPC>();

            // === 1. LA COLUMNA: la LÍNEA RECTA (el jitter es cosmético) ===
            const float Margin = 16f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc)) continue;
                Vector2 c1 = npc.position - new Vector2(Margin, Margin);
                Vector2 c2 = npc.Size + new Vector2(Margin * 2f, Margin * 2f);
                if (!Collision.CheckAABBvLineCollision(c1, c2, _sky, _strike)) continue;

                Content.Systems.GolpeMotor.Golpear(Projectile, npc, Projectile.damage, 2.5f, true);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    try { npc.AddBuff(BuffID.Electrified, 240); } catch { }
                hit.Add(npc);
            }

            // === 2. EL ESTALLIDO RADIAL en el impacto (radio 110) ===
            const float BurstR = 110f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!VFXCore.EsObjetivo(npc) || hit.Contains(npc)) continue;
                if ((npc.Center - _strike).Length() > BurstR) continue;

                Content.Systems.GolpeMotor.Golpear(Projectile, npc, Projectile.damage, 3f, true);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    try { npc.AddBuff(BuffID.Electrified, 240); } catch { }
                hit.Add(npc);
            }

            // === 3. LA CADENA: hasta 3 saltos desde el impacto (60%) ===
            int chainDamage = Math.Max(1, (int)(Projectile.damage * 0.60f));
            Vector2 origin = _strike;
            for (int c = 0; c < 3; c++)
            {
                NPC best = null;
                float bestDist = 320f;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!VFXCore.EsObjetivo(npc) || hit.Contains(npc)) continue;
                    float dist = (npc.Center - origin).Length();
                    if (dist < bestDist)
                    {
                        bool already = false;
                        for (int p = 0; p < c; p++)
                            if (_chainAlive[p] && (_chainTo[p] - npc.Center).Length() < 8f)
                                already = true;
                        if (already) continue;
                        bestDist = dist;
                        best = npc;
                    }
                }
                if (best == null) { _chainAlive[c] = false; continue; }

                _chainFrom[c] = origin;
                _chainTo[c] = best.Center;
                _chainAlive[c] = true;
                origin = best.Center;             // la cadena sigue desde el último.

                Content.Systems.GolpeMotor.Golpear(Projectile, best, chainDamage, 1.5f, true);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    try { best.AddBuff(BuffID.Electrified, 240); } catch { }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Guard: hasta anclarse no hay descarga que dibujar.
            if (!_anchored) return false;

            // ============================================================
            //  CONTRATO DE BATCH A PRUEBA DE BALAS (v6.10 — la lección de
            //  los agujeros, confirmada por la investigación v6.21 en TODOS
            //  los mods grandes): durante PreDraw el batch de tML está
            //  ABIERTO; hay que CERRARLO antes de abrir el pase aditivo.
            // ============================================================
            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                DrawStrike();
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
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
        //  EL DIBUJO DE LA DESCARGA
        // ------------------------------------------------------------------

        private static readonly Color GoldHalo = new(255, 195, 85);     // oro solar
        private static readonly Color GoldWarm = new(255, 225, 140);    // oro cálido
        private static readonly Color StarBlue = new(150, 180, 255);    // azul estelar
        private static readonly Color WhiteIncan = new(255, 250, 235);  // blanco incandescente

        private void DrawStrike()
        {
            try
            {
                float time = Main.GlobalTimeWrappedHourly;
                int seed = Seed;
                int flick = StormLib.FlickTick(time, FlickHz);
                Vector2 screen = Main.screenPosition;
                Vector2 sky = _sky - screen;
                Vector2 strike = _strike - screen;

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                if (!_hasStruck)
                {
                    DrawTelegraph(sky, strike, time, seed, flick);
                }
                else
                {
                    DrawBolt(sky, strike, time, seed, flick);
                }

                Main.spriteBatch.End();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
        }

        /// <summary>LA LÍNEA DE AVISO del telegraph + el anillo objetivo.</summary>
        private void DrawTelegraph(Vector2 sky, Vector2 strike, float time, int seed, int flick)
        {
            // La línea fina: una tira de halo de 3 px del cielo al objetivo
            // con el FILAMENTO tenue encendido a ráfagas (la carga).
            Vector2[] path = StormLib.ZigPath(sky, strike, seed, flick, 10, 14f);
            float charge = MathHelper.Clamp(_age / TelegraphTicks, 0f, 1f);
            float pulse = 0.55f + 0.45f * (float)Math.Sin(time * 9f);

            for (int i = 0; i < path.Length - 1; i++)
            {
                Vector2 seg = path[i + 1] - path[i];
                float len = seg.Length();
                if (len < 0.5f) continue;
                float rot = (float)Math.Atan2(seg.Y, seg.X);
                Vector2 mid = (path[i] + path[i + 1]) * 0.5f;
                // v6.50.22 — EL PIXEL del motor (la banda BoltHalo se
                // retira del consumo: suelo de alfa en los bordes).
                Quad(VFXCore.Pixel, mid, new Vector2(len + 6f, 3.5f + 2.5f * pulse * charge),
                    rot, Tint(GoldWarm, 0.35f * charge));
            }

            // El filamento de carga: encendido a ráfagas cada vez más rápido.
            if (StormLib.IsLit(seed, flick, 0.30f + 0.45f * charge))
                StormLib.Bolt(Main.spriteBatch, sky, strike, seed, flick,
                    2.6f, Tint(GoldHalo, 0.55f * charge), Tint(WhiteIncan, 0.85f * charge),
                    1f, 8, 12f);

            // El ANILLO OBJETIVO pulsante (el punto de impacto anunciado).
            float ringR = 30f + 9f * (float)Math.Sin(time * 9f);
            Quad(StormLib_Ring(), strike, VFXCore.RingQuadSize(ringR), time * 2.2f,
                Tint(GoldWarm, 0.45f * charge));
            Quad(StormLib_Glow(), strike, new Vector2(22f, 22f), 0f,
                Tint(GoldWarm, 0.35f + 0.25f * pulse * charge));
        }

        /// <summary>EL RAYO DE VERDAD cayendo del cielo + su impacto.</summary>
        private void DrawBolt(Vector2 sky, Vector2 strike, float time, int seed, int flick)
        {
            float strikeAge = _age - TelegraphTicks;              // 0..24
            float lifeT = MathHelper.Clamp(strikeAge / (TotalLife - TelegraphTicks), 0f, 1f);
            float fade = 1f - MathHelper.Clamp((lifeT - 0.45f) / 0.55f, 0f, 1f);
            if (fade <= 0.02f) return;

            // EL PARPADEO con apagado (la lección #1): encendido 7 de cada
            // 10 regeneraciones; apagado queda un FANTASMA tenue.
            bool lit = StormLib.IsLit(seed, flick, 0.70f);
            float alpha = lit ? 1f : 0.16f;

            // LA MUERTE VIOLENTA: el jitter REVIENTA al disolverse.
            float amp = 26f * StormLib.DeathGrow(lifeT);

            // === 1. EL RAYO MULTI-FILAMENTO (la descarga) ===
            StormLib.MultiBolt(Main.spriteBatch, sky, strike, seed, flick,
                11f, GoldHalo, StarBlue, WhiteIncan, alpha * fade, amp, 14);

            // === 2. LAS CADENAS (eslabones azul-estelar, tenues) ===
            for (int c = 0; c < 3; c++)
            {
                if (!_chainAlive[c]) continue;
                if (!StormLib.IsLit(seed + 61 + c * 37, flick, 0.55f)) continue;
                StormLib.ChainBolt(Main.spriteBatch,
                    _chainFrom[c] - Main.screenPosition, _chainTo[c] - Main.screenPosition,
                    seed + 61 + c * 37, flick, 4.4f,
                    Tint(StarBlue, 0.75f * fade), Tint(new Color(230, 240, 255), 0.9f * fade),
                    1f, 7, 9f);
            }

            // === 3. EL FLASH DE IMPACTO (los primeros ticks: apilado) ===
            float flashI = MathHelper.Clamp(1f - strikeAge / 7f, 0f, 1f);
            if (flashI > 0f)
            {
                // El destello se pinta DOBLE los 3 primeros ticks (multi-draw).
                float size = 62f * (0.55f + 0.45f * flashI);
                StormLib.ImpactFlash(Main.spriteBatch, strike, size, GoldWarm,
                    flashI * alpha, time * 0.9f);
                if (strikeAge < 3f)
                    StormLib.ImpactFlash(Main.spriteBatch, strike, size * 0.7f, GoldWarm,
                        flashI * 0.6f, -time * 1.3f);
            }

            // === 4. LOS ARCOS DE IMPACTO (coronas crispadas) ===
            if (StormLib.IsLit(seed + 40, flick, 0.75f))
            {
                float arcR = 26f + 30f * lifeT;
                StormLib.ArcRing(Main.spriteBatch, strike, arcR,
                    time * 2.1f, time * 2.1f + 1.9f, seed + 40, flick, 3.2f,
                    Tint(GoldHalo, 0.60f * fade), Tint(WhiteIncan, 0.9f * fade), 1f, 9);
                StormLib.ArcRing(Main.spriteBatch, strike, arcR * 0.66f,
                    -time * 2.7f + 2.5f, -time * 2.7f + 4.1f, seed + 41, flick, 2.6f,
                    Tint(StarBlue, 0.55f * fade), Tint(new Color(230, 240, 255), 0.85f * fade),
                    1f, 8);
            }

            // === 5. LA ONDA DE CHOQUE expandiéndose ===
            float wavePhase = MathHelper.Clamp(strikeAge / 14f, 0f, 1f);
            if (wavePhase < 1f)
            {
                float waveR = 24f + 120f * wavePhase;
                float waveFade = (1f - wavePhase) * (1f - wavePhase) * fade;
                Quad(StormLib_Ring(), strike,
                    VFXCore.RingQuadSize(waveR), wavePhase * 2.5f,
                    Tint(new Color(255, 230, 160), 0.45f * waveFade));
            }
        }

        // ------------------------------------------------------------------
        //  HELPERS
        // ------------------------------------------------------------------

        private static Texture2D StormLib_Glow()
            => VFXCore.SoftGlow;

        private static Texture2D StormLib_Ring()
            => VFXCore.Ring;

        private static void Quad(Texture2D tex, Vector2 pos,
            Vector2 size, float rot, Color tint)
        {
            if (tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }

        private static Color Tint(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color(c.R, c.G, c.B, (byte)(int)(255f * f));
        }

        public override void OnKill(int timeLeft)
        {
            // Chispas de despedida en el punto de impacto.
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 10; i++)
            {
                float ang = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(_strike, DustID.Electric,
                    new Vector2((float)Math.Cos(ang) * Main.rand.NextFloat(2f, 6f),
                                (float)Math.Sin(ang) * Main.rand.NextFloat(2f, 6f)),
                    220, new Color(255, 235, 170), 1.1f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }
    }
}
