using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Projectiles.Sombras
{
    /// <summary>
    /// FAUCEDEVORADORPROJECTILE — v6.50.62 — LA ANIMACIÓN DE MUERTE.
    ///
    /// «En el momento en que el jefe llega a 1 punto de vida, su
    /// animación original de muerte SE DETIENE y no avanza; en su lugar
    /// se activa la nueva: la sombra sale y LO DEVORA.»
    ///
    /// Lo spawnea FaucesGlobalNPC al interceptar la muerte (o al clavar
    /// la vida en 1 con el drain). El jefe queda POSADO (PreAI false —
    /// ni IA ni animación) mientras ESTE proyectil ejecuta el festín:
    ///
    ///   0-30   MANIFESTACIÓN — la sombra ERUPCIONA del portador
    ///          (estilo 1/3: el tentáculo GIGANTE se alza del charco;
    ///           estilo 2: la esfera se cierra alrededor del jefe).
    ///   30-60  ENVOLVER — el tentáculo SERPENTEA hasta el jefe y lo
    ///          enrosca / la esfera aprieta (los ojos se abren TODOS).
    ///   60-160 EL FESTÍN — las fauces MASTICAN (3 mordidas sonoras),
    ///          la OSCURIDAD crece sobre el jefe hasta taparlo, las
    ///          ALMAS vuelan al portador.
    ///   160-210 LA DISIPACIÓN — bruma negra y polvo: el jefe se
    ///          desintegra. A los 210 el motor mata de verdad (el loot
    ///          cae DENTRO de la bruma: el festín lo digiere todo).
    ///
    /// ai[0] = whoAmI del jefe · ai[1] = estilo (1/2/3) · ai[2] = tick.
    /// </summary>
    public class FauceDevoradorProjectile : ModProjectile
    {
        private NPC Jefe => Projectile.ai[0] < 0 || Projectile.ai[0] >= Main.npc.Length
            ? null : Main.npc[(int)Projectile.ai[0]];

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 320;
            Projectile.netImportant = true;
        }

        /// <summary>El portador: el jugador más cercano al jefe (SP: el usuario).</summary>
        private Player Portador(NPC jefe)
        {
            Player mejor = Main.player[Projectile.owner];
            float d = float.MaxValue;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p == null || !p.active) continue;
                float dd = Vector2.DistanceSquared(p.Center, jefe.Center);
                if (dd < d) { d = dd; mejor = p; }
            }
            return mejor;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            NPC jefe = Jefe;

            // el jefe se esfumó (no debería: CheckActive lo clava) → fuera
            if (jefe == null || !jefe.active)
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = jefe.Center;

            if (Main.netMode == NetmodeID.Server) return;   // el server no dibuja

            // === POLVO Y AMBIENTE (el cliente lo genera local, determinista basta) ===
            // durante el festín: motas de sombra cayendo del jefe
            if (t > 60 && t < 205 && Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(jefe.Center + new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1)) * jefe.Size.Length() * 0.4f,
                    DustID.Shadowflame, new Vector2(0, 1.2f), 128, default, 0.9f);
                d.noGravity = false;
                d.fadeIn = 0.4f;
            }
            // la disipación final: POLVO de sombra que estalla
            if (t >= 160 && t < 205)
            {
                int n = 3;
                for (int k = 0; k < n; k++)
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    Dust d = Dust.NewDustPerfect(jefe.Center,
                        DustID.Smoke,
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * Main.rand.NextFloat(2f, 7f),
                        100, new Color(12, 6, 10), 1.5f);
                    d.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            NPC jefe = Jefe;
            if (jefe == null || !jefe.active) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                byte estilo = (byte)Projectile.ai[1];
                if (estilo == 2) DrawEsfera(jefe);
                else DrawTentaculo(jefe, estilo == 3);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        // ==================================================================
        //  ESTILOS 1 y 3 — EL TENTÁCULO GIGANTE (sprite / 100% código)
        // ==================================================================

        private void DrawTentaculo(NPC jefe, bool procedural)
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 31 + jefe.whoAmI;
            float t = Projectile.ai[2];
            Player portador = Portador(jefe);
            Vector2 raiz = portador.MountedCenter + new Vector2(0, portador.height * 0.42f);

            // fases del festín
            float manifiesta = SombrasLib.DeGolpe(MathHelper.Clamp(t / 30f, 0f, 1f));
            float envuelve = MathHelper.Clamp((t - 30f) / 30f, 0f, 1f);
            float festin = MathHelper.Clamp((t - 60f) / 100f, 0f, 1f);
            float disipa = MathHelper.Clamp((t - 160f) / 45f, 0f, 1f);
            float vivo = 1f - disipa;

            // === EL CHARCO DEL PORTADOR (más grande que nunca) ===
            SombrasLib.Charco(raiz, 64f * manifiesta, 0.85f * vivo, tiempo, semilla);

            // === EL CUERPO GIGANTE: raíz → jefe, en dos tramos ===
            // (a media caza la cabeza VIAJA; ya envuelto, MUERDE en el sitio)
            float alcance = manifiesta * 0.35f + envuelve * 0.65f;
            Vector2 destino = Vector2.Lerp(raiz + new Vector2(0, -160f), jefe.Center, alcance);
            Vector2[] col = SombrasLib.Columna(raiz, destino, tiempo, semilla, 22, 0.20f);
            SombrasLib.Masa(col, 58f, 20f, 0.96f * vivo, semilla, tiempo);

            // === LOS OJOS: TODOS abiertos, TODOS mirando al jefe ===
            SombrasLib.OjosDeMasa(col, jefe.Center, (0.4f + 0.6f * envuelve) * vivo, semilla, tiempo, 7);

            // === LA OSCURIDAD SOBRE EL JEFE (crece hasta TAPARLO) ===
            float radio = MathHelper.Clamp(jefe.Size.Length() * 0.7f, 90f, 300f);
            float tapa = MathHelper.Clamp(envuelve * 0.5f + festin * 0.5f, 0f, 1f);
            if (tapa > 0.02f)
            {
                VFXCore.Begin();
                VFXCore.Quad(jefe.Center, SombrasLib.Alfa(SombrasLib.Negro, 0.92f * tapa * vivo),
                    new Vector2(radio * 2.15f * (0.55f + 0.45f * tapa), radio * 2.15f * (0.55f + 0.45f * tapa)), 0f, VFXCore.GlowOrb);
                VFXCore.FlushAlpha();
            }

            // === LA CABEZA-BOCA ===
            if (procedural)
            {
                // estilo 3: FAUCES 100% CÓDIGO — mastica el ciclo del festín
                Vector2 rumbo = (jefe.Center - col[col.Length - 4]).SafeNormalize(Vector2.UnitX);
                float apertura = t < 60f
                    ? 0.9f * manifiesta
                    : CicloMasticar(t);
                SombrasLib.Fauces(destino, rumbo, apertura * vivo, 120f, semilla);
            }
            else
            {
                // estilo 1: EL SPRITE GIGANTE del usuario (a escala ~1.0)
                Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[
                    ModContent.ProjectileType<FaucesTentaculoProjectile>()].Value;
                int frame = FrameFestin(t);
                Rectangle src = new Rectangle((frame % 6) * (tex.Width / 6),
                    (frame / 6) * (tex.Height / 3), tex.Width / 6, tex.Height / 3);
                float escala = 1.0f * manifiesta;
                if (t > 60f) escala *= 1f + 0.05f * MathF.Sin(tiempo * 8f);     // la boca APRIETA
                escala *= 0.85f + 0.15f * vivo;
                Vector2 origen = new Vector2(src.Width * 0.42f, src.Height * 0.94f);
                float rot = (jefe.Center - raiz).ToRotation();
                Color luz = Lighting.GetColor(destino.ToTileCoordinates());
                Color tint = new Color(luz.R, luz.G, luz.B, 255) * vivo;

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                    null, Main.Transform);
                Main.EntitySpriteDraw(tex, destino - Main.screenPosition, src, tint, rot,
                    origen, escala, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
            }

            // === LAS ALMAS: la vida del jefe vuela al portador ===
            if (t > 55f)
            {
                VFXCore.Begin();
                int nAlmas = 6;
                for (int k = 0; k < nAlmas; k++)
                {
                    float prog = SombrasLib.Frac(t * 0.016f + k * (1f / nAlmas));
                    Vector2 a = jefe.Center + new Vector2(MathF.Cos(k * 2.7f + tiempo * 0.7f), MathF.Sin(k * 3.1f + tiempo)) * radio * 0.4f;
                    Vector2 b = portador.MountedCenter;
                    Vector2 ctrl = (a + b) * 0.5f + new Vector2(MathF.Sin(k * 2.3f) * 120f, -170f);
                    float u = 1f - prog;
                    Vector2 pos = u * u * a + 2f * u * prog * ctrl + prog * prog * b;
                    SombrasLib.Ojo(pos, 7f, b - pos, 1f);        // el alma ES un ojo que mira a casa
                }
                VFXCore.FlushAdditive();
            }

            // === LA DISIPACIÓN: bruma y polvo final ===
            if (disipa > 0.02f)
            {
                SombrasLib.Bruma(jefe.Center, radio * 1.4f, 0.9f * disipa, tiempo, semilla,
                    new Vector2(MathF.Sin(tiempo * 0.7f) * 30f, -18f * disipa));
            }
        }

        // ==================================================================
        //  ESTILO 2 — LA ESFERA (la boca cerrada que desintegra)
        // ==================================================================

        private void DrawEsfera(NPC jefe)
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 29 + jefe.whoAmI;
            float t = Projectile.ai[2];
            Player portador = Portador(jefe);

            float manifiesta = SombrasLib.DeGolpe(MathHelper.Clamp(t / 26f, 0f, 1f));
            float festin = MathHelper.Clamp((t - 45f) / 115f, 0f, 1f);
            float disipa = MathHelper.Clamp((t - 160f) / 45f, 0f, 1f);
            float vivo = 1f - disipa;

            float radio = MathHelper.Clamp(jefe.Size.Length() * 0.68f, 95f, 280f) * manifiesta;

            // === EL DISCO NEGRO ===
            VFXCore.Begin();
            VFXCore.Quad(jefe.Center, SombrasLib.Alfa(SombrasLib.Negro, 0.97f * manifiesta * vivo),
                new Vector2(radio * 2.1f, radio * 2.1f), 0f, VFXCore.GlowOrb);
            VFXCore.FlushAlpha();

            // === LOS OJOS: TODOS, MIRANDO AL PORTADOR ===
            VFXCore.Begin();
            for (int k = 0; k < 14; k++)
            {
                float f = SombrasLib.Frac(SombrasLib.semille(semilla) * 0.43f + k * 0.618034f);
                float ang = f * MathHelper.TwoPi;
                float rr = radio * (0.2f + 0.66f * SombrasLib.Frac(f * 8.1f));
                Vector2 pos = jefe.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.85f) * rr;
                float abierto = MathHelper.Clamp(manifiesta * 14f * 0.75f - k * 0.75f, 0f, 1f);
                abierto *= 0.85f + 0.15f * MathF.Sin(tiempo * 3.9f + k * 1.7f);
                if (abierto <= 0.05f) continue;
                SombrasLib.Ojo(pos, 14f + 13f * SombrasLib.Frac(f * 6.2f), portador.Center - pos, abierto);
            }
            VFXCore.FlushAdditive();

            // === LA BOCA: muerde el ciclo y se lo COME entero ===
            Vector2 rumbo = new(MathF.Cos(tiempo * 0.45f), MathF.Sin(tiempo * 0.3f));
            rumbo = rumbo.LengthSquared() < 0.01f ? Vector2.UnitX : Vector2.Normalize(rumbo);
            float apertura = t < 45f ? 0.9f : CicloMasticar(t);
            SombrasLib.Fauces(jefe.Center + rumbo * radio * 0.12f, rumbo, apertura * vivo,
                radio * 0.95f * manifiesta, semilla);

            // === EL INTERIOR: el jefe se APAGA (oscuro creciente dentro) ===
            if (festin > 0.02f)
            {
                VFXCore.Begin();
                VFXCore.Quad(jefe.Center, SombrasLib.Alfa(SombrasLib.Negro, 0.9f * festin * vivo),
                    new Vector2(radio * 1.6f * (0.5f + 0.5f * festin), radio * 1.6f * (0.5f + 0.5f * festin)));
                VFXCore.FlushAlpha();
            }

            // === LAS ALMAS vuelan al portador ===
            if (t > 50f)
            {
                VFXCore.Begin();
                for (int k = 0; k < 6; k++)
                {
                    float prog = SombrasLib.Frac(t * 0.015f + k * 0.1667f);
                    Vector2 a = jefe.Center + new Vector2(MathF.Cos(k * 2.6f + tiempo), MathF.Sin(k * 2.2f)) * radio * 0.5f;
                    Vector2 b = portador.MountedCenter;
                    Vector2 ctrl = (a + b) * 0.5f + new Vector2(0, -150f);
                    float u = 1f - prog;
                    Vector2 pos = u * u * a + 2f * u * prog * ctrl + prog * prog * b;
                    SombrasLib.Alma(pos, 11f, 0.85f);
                }
                VFXCore.FlushAdditive();
            }

            // === LA DISIPACIÓN: bruma y polvo ===
            if (disipa > 0.02f)
                SombrasLib.Bruma(jefe.Center, radio * 1.35f, 0.9f * disipa, tiempo, semilla,
                    new Vector2(MathF.Sin(tiempo * 0.8f) * 26f, -20f * disipa));
        }

        /// <summary>El masticar del festín: abre… ¡CIERRA de golpe! …mastica… abre.</summary>
        private static float CicloMasticar(float t)
        {
            float m = (t - 60f) % 32f;
            if (m < 10f) return MathHelper.Lerp(0.2f, 1f, m / 10f);                          // abre
            if (m < 16f) return MathHelper.Lerp(1f, 0.03f, SombrasLib.DeGolpe((m - 10f) / 6f)); // ¡CIERRA!
            if (m < 26f) return 0.15f + 0.1f * MathF.Sin(m * 1.4f);                          // mastica
            return MathHelper.Lerp(0.15f, 0.85f, (m - 26f) / 6f);                            // reabre
        }

        /// <summary>El frame del sprite gigante según el momento del festín.</summary>
        private static int FrameFestin(float t)
        {
            if (t < 30f) return (int)MathHelper.Clamp(t / 30f * 5f, 0f, 5f);        // erupción (0-5)
            if (t < 60f) return 6 + (int)((t - 30f) / 30f * 4f);                     // el latigazo (6-10)
            if (t < 160f)                                                           // muerde (10↔12)
            {
                float m = (t - 60f) % 24f;
                if (m < 10f) return 10;
                if (m < 20f) return 11;
                return 12;
            }
            return 12 + (int)MathHelper.Clamp((t - 160f) / 45f * 4f, 0f, 4f);        // disipa (12-16)
        }
    }
}
