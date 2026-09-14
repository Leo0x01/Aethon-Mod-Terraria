using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// OcasoShardProjectile — v6.27 — EL FRAGMENTO DEL OCASO.
    ///
    /// El disparo de CARGA del arma suprema: fragmentos dorado-violeta
    /// con leve búsqueda de enemigo, cada IMPACTO llena el aro medidor
    /// (+3 — la trinidad gauge del informe). Visual: el corazón de luz
    /// + la cruz de destello (la forma de estrella de 4 puntas de la
    /// casa) + la estela ribbon — dibujado 100% por código.
    /// </summary>
    public class OcasoShardProjectile : ModProjectile
    {
        private int _age;

        /// <summary>Semilla determinista (ai[0] — las 3 ranuras de tML).</summary>
        private int Seed => Math.Max(1, (int)Projectile.ai[0]) + Projectile.identity;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.light = 0.4f;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (Projectile.ai[0] <= 0f)
                Projectile.ai[0] = (Projectile.identity % 9973 + 1) * 1f;
        }

        public override void AI()
        {
            _age++;

            // === LA BÚSQUEDA SUAVE (el fragmento huele la luz que apaga) ===
            NPC presa = PresaCercana(420f);
            if (presa != null)
            {
                Vector2 hacia = presa.Center - Projectile.Center;
                if (hacia.LengthSquared() > 16f)
                {
                    hacia.Normalize();
                    float giro = 0.055f;
                    Projectile.velocity = Vector2.Lerp(
                        Projectile.velocity, hacia * Projectile.velocity.Length(), giro);
                }
            }
            if (Projectile.velocity.Length() > 16f)
                Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 16f;

            Projectile.rotation = _age * 0.22f;

            // === LA ESTELA (el rastro del fragmento) ===
            EstelaLib.Track(Projectile.whoAmI, 10).Push(Projectile.Center);
            if (_age % 120 == 0) EstelaLib.PurgeTracks();

            // === LA LUZ (dorada con un toque violeta) ===
            if (Main.netMode != NetmodeID.Server && _age % 2 == 0)
                Lighting.AddLight(Projectile.Center, 0.55f, 0.40f, 0.18f);
        }

        /// <summary>La presa más cercana en rango (el patrón de la casa).</summary>
        private NPC PresaCercana(float maxRange)
        {
            NPC best = null;
            float bestDist = maxRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                float dist = (npc.Center - Projectile.Center).Length();
                if (dist < bestDist) { bestDist = dist; best = npc; }
            }
            return best;
        }

        // ==================================================================
        //  EL IMPACTO — CADA GOLPE CARGA EL ARO (la trinidad)
        // ==================================================================

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // EL GAUGE: +3 por impacto (34 impactos llenan el aro).
            Player dueño = Main.player[Projectile.owner];
            dueño?.GetModPlayer<OcasoPlayer>().AñadirGauge(OcasoPlayer.GaugePorImpacto);

            if (Main.netMode == NetmodeID.Server) return;

            // LAS ASCUAS del impacto (la rampa SolarFire de PyraPalettes).
            for (int i = 0; i < 5; i++)
            {
                float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(target.Center, DustID.GoldFlame,
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                        Main.rand.NextFloat(0.8f, 2.4f),
                    200, default, 0.55f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 4; i++)
            {
                float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 1.2f,
                    150, default, 0.4f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
            // (el track del ring-buffer se pudre SOLO en 2 ticks — sin
            // limpiar nada global: ClearTracks borraría el mundo entero)
        }

        // ==================================================================
        //  EL DIBUJO — 100% por código (contrato de batch v6.10)
        // ==================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            // El contrato de la casa: cerrar el lote de tML, el pase propio,
            // y restaurar los parámetros EXACTOS del pase de proyectiles.
            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                Dibujar();
            }
            catch { try { Main.spriteBatch.End(); } catch { } }

            if (wasActive)
                RestoreSpriteBatch();
            return false;
        }

        private static void RestoreSpriteBatch()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
        }

        private static void BeginAdditive()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        private void Dibujar()
        {
            float time = Main.GlobalTimeWrappedHourly;
            Vector2 pos = Projectile.Center - Main.screenPosition;

            // === 1. LA ESTELA ribbon (dorado → violeta) ===
            Vector2[] camino = EstelaLib.Track(Projectile.whoAmI, 10).Points();
            if (camino.Length >= 2)
            {
                for (int i = 0; i < camino.Length; i++)
                    camino[i] -= Main.screenPosition;
                BeginAdditive();
                EstelaLib.Ribbon(Main.spriteBatch, camino, 7f,
                    EstelaProfile.Comet, new Color(255, 200, 110), 0.45f,
                    Seed + 3, time, head: false);
                Main.spriteBatch.End();
            }

            // === 2. EL CUERPO (glow dorado + corazón blanco) ===
            BeginAdditive();
            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            Quad(glow, pos, new Vector2(26f, 26f), 0f, new Color(255, 190, 90) * 0.55f);
            Quad(glow, pos, new Vector2(13f, 13f), 0f, new Color(255, 246, 215) * 0.9f);

            // === 3. LA CRUZ DE DESTELLO (la estrella de 4 puntas) ===
            float rot = Projectile.rotation;
            Quad(glow, pos, new Vector2(34f, 4.5f), rot, new Color(255, 214, 110) * 0.8f);
            Quad(glow, pos, new Vector2(34f, 4.5f), rot + MathHelper.PiOver2,
                new Color(200, 130, 255) * 0.8f);
            // La cruz menor girada 45° (el brillo interior de la casa).
            Quad(glow, pos, new Vector2(16f, 3f), rot + MathHelper.PiOver4,
                new Color(255, 240, 200) * 0.6f);

            // === 4. EL TITILEO (la vida del fragmento: 2 puntas orbitando) ===
            for (int k = 0; k < 2; k++)
            {
                float ang = time * 5.5f * ((k & 1) == 0 ? 1f : -1f) + k * MathHelper.Pi;
                Vector2 sp = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 11f;
                Quad(glow, sp, new Vector2(5f, 5f), 0f, new Color(230, 170, 255) * 0.7f);
            }

            Main.spriteBatch.End();
        }

        /// <summary>Quad centrado (tamaño total = size px) sobre el lote ABIERTO.</summary>
        private static void Quad(Texture2D tex, Vector2 pos, Vector2 size, float rot, Color tint)
        {
            if (tex == null || tint.A == 0) return;
            Main.spriteBatch.Draw(tex, pos, null, tint, rot,
                new Vector2(tex.Width, tex.Height) * 0.5f,
                size / new Vector2(tex.Width, tex.Height),
                SpriteEffects.None, 0f);
        }
    }
}
