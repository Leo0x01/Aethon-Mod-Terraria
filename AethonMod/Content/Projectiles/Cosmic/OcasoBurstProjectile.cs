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
    /// OcasoBurstProjectile — v6.27 — LA MUERTE DE ESTRELLA.
    ///
    /// El disparo del MODO OCASO del arma suprema: un mini-eclipse de 90 px
    /// que viaja lento (la muerte de una estrella no tiene prisa), daña ×3
    /// con EJECUCIÓN (+50%) bajo el 50% de vida del objetivo, ENCADENA rayos
    /// violetas a los vecinos y al morir se apaga con un desgarro en la
    /// realidad (RiftLib.Tear) + el anillo de onda (OndaLib.Pulse).
    ///
    /// Todo 100% por código: anillos rúnicos por el EMISOR COMPARTIDO de la
    /// casa (RuneSunRenderer.EmitRingSystem — el mismo trazo de los soles),
    /// rampa de fuego SolarFire (PyraPalettes) para la corona, arcos de
    /// StormLib y estela ribbon dorada.
    /// </summary>
    public class OcasoBurstProjectile : ModProjectile
    {
        private int _age;

        private int Seed => Math.Max(1, (int)Projectile.ai[0]) + Projectile.identity;

        /// <summary>El radio visual del mini-eclipse.</summary>
        private const float R = 45f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 110;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;   // la muerte de una estrella ATRAVIESA
            Projectile.light = 0.9f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (Projectile.ai[0] <= 0f)
                Projectile.ai[0] = (Projectile.identity % 9973 + 1) * 1f;
        }

        public override void AI()
        {
            _age++;

            // === EL VUELO LENTO con búsqueda suave (la muerte va por su presa) ===
            NPC presa = PresaCercana(520f);
            if (presa != null)
            {
                Vector2 hacia = presa.Center - Projectile.Center;
                if (hacia.LengthSquared() > 16f)
                {
                    hacia.Normalize();
                    Projectile.velocity = Vector2.Lerp(
                        Projectile.velocity, hacia * 10f, 0.03f);
                }
            }
            if (Projectile.velocity.Length() > 11f)
                Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 11f;

            // === LA ESTELA (el rastro del duelo) ===
            EstelaLib.Track(Projectile.whoAmI, 14).Push(Projectile.Center);
            if (_age % 120 == 0) EstelaLib.PurgeTracks();

            // === LA LUZ (un eclipse dorado-violeta) ===
            if (Main.netMode != NetmodeID.Server && _age % 2 == 0)
                Lighting.AddLight(Projectile.Center, 0.85f, 0.62f, 0.30f);

            // === LAS BRASAS que va soltando (la estrella pierde materia) ===
            if (Main.netMode != NetmodeID.Server && _age % 5 == 0)
            {
                float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * R * 0.7f,
                    DustID.Torch,
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 0.7f,
                    160, new Color(255, 140, 60), 0.7f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

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
        //  EL DAÑO — ×3 ya aplicado; AQUÍ la EJECUCIÓN y la CADENA
        // ==================================================================

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // LA EJECUCIÓN: bajo el 50% de vida, la muerte de estrella
            // remata con +50% (el "guaranteed crit" de la casa — sin
            // dados: es un execute, no una lotería).
            if (target.life < target.lifeMax * 0.5f)
                modifiers.FinalDamage *= 1.5f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            // === LA CADENA VIOLETA: 2 vecinos comen la descarga ===
            int cadenas = 0;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (cadenas >= 2) break;
                if (npc.whoAmI == target.whoAmI || !npc.CanBeChasedBy()) continue;
                if (npc.immortal) continue;
                float dist = (npc.Center - target.Center).Length();
                if (dist > 220f) continue;

                npc.SimpleStrikeNPC(
                    (int)(Projectile.damage * 0.5f), npc.direction, false,
                    2.5f, DamageClass.Magic);
                cadenas++;
            }

            if (Main.netMode == NetmodeID.Server) return;

            // === LOS FX DEL GOLPE (concentrados en el impacto) ===
            OndaLib.Kick(6f, 12);
            for (int i = 0; i < 10; i++)
            {
                float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(target.Center,
                    i % 2 == 0 ? DustID.GoldFlame : DustID.Torch,
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                        Main.rand.NextFloat(1.5f, 4f),
                    220, default, 0.7f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;

            // === EL APAGÓN: la estrella muere ABRIENDO la realidad ===
            // (un desgarro corto de RiftLib en la dirección del vuelo).
            Vector2 dir = Projectile.velocity.LengthSquared() > 0.1f
                ? Vector2.Normalize(Projectile.velocity)
                : new Vector2(1f, 0f);
            // TearImpacto es el paquete de impacto de la casa (kick + shards).
            // El dibujo persistente vive en el buffer de VFXCore: lo emitimos
            // desde un sistema propio al vuelco del frame (ver OcasoBurstFX).
            OcasoBurstFX.ProgramarDesgarro(Projectile.Center, dir, Seed);

            OndaLib.Kick(9f, 18);
            OndaLib.Flash(new Color(255, 150, 60), 0.16f, 10);

            for (int i = 0; i < 18; i++)
            {
                float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Projectile.Center,
                    i % 3 == 0 ? DustID.PurpleTorch : DustID.GoldFlame,
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                        Main.rand.NextFloat(2f, 6f),
                    230, default, 0.9f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item14.WithPitchOffset(-0.35f), Projectile.Center);
        }

        // ==================================================================
        //  EL DIBUJO — EL MINI-ECLIPSE (contrato de batch v6.10)
        // ==================================================================

        public override bool PreDraw(ref Color lightColor)
        {
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
            float lifeT = 1f - Projectile.timeLeft / 110f;
            Vector2 pos = Projectile.Center - Main.screenPosition;

            // === 1. LA ESTELA ribbon (el rastro del duelo) ===
            Vector2[] camino = EstelaLib.Track(Projectile.whoAmI, 14).Points();
            if (camino.Length >= 2)
            {
                for (int i = 0; i < camino.Length; i++)
                    camino[i] -= Main.screenPosition;
                BeginAdditive();
                EstelaLib.Ribbon(Main.spriteBatch, camino, 16f,
                    EstelaProfile.Comet, new Color(255, 170, 70), 0.40f,
                    Seed + 9, time, head: false);
                Main.spriteBatch.End();
            }

            // === 2. LOS ANILLOS RÚNICOS (el EMISOR COMPARTIDO de la casa:
            //     el MISMO trazo de los soles, tier 3, templete dorado) ===
            VFXCore.Begin();
            RuneSunRenderer.EmitRingSystem(Projectile.Center, R * 0.66f, time,
                Seed, 3, 0.35f, lifeT, 0.75f);
            VFXCore.FlushAdditive(null, false);   // el lote ya está CERRADO

            // === 3. EL CUERPO DEL ECLIPSE (glow dorado + limbo + corazón) ===
            BeginAdditive();
            Texture2D glow = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            Quad(glow, pos, new Vector2(R * 2.4f, R * 2.4f), 0f,
                new Color(255, 150, 60) * 0.34f);
            Quad(glow, pos, new Vector2(R * 1.3f, R * 1.3f), 0f,
                new Color(255, 205, 110) * 0.5f);
            Quad(glow, pos, new Vector2(R * 0.5f, R * 0.5f), 0f,
                new Color(255, 250, 230) * 0.9f);

            // === 4. LA CORONA DE FUEGO (la rampa SolarFire girando) ===
            for (int k = 0; k < 8; k++)
            {
                float ang = k / 8f * MathHelper.TwoPi + time * 0.7f;
                float temp = 0.55f + 0.35f * MathF.Sin(time * 2.2f + k * 1.3f);
                Color c = PyraPalettes.Sample(PyraPalettes.SolarFire, temp);
                Vector2 cp = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * R * 0.75f;
                Quad(glow, cp, new Vector2(16f, 16f), ang, c * 0.42f);
            }

            // === 5. LOS ARCOS DE TORMENTA alrededor (la descarga lista) ===
            int flick = StormLib.FlickTick(time, 15f);
            StormLib.ArcRing(Main.spriteBatch, pos, R * 1.15f,
                time * 0.9f, time * 0.9f + MathHelper.Pi * 0.55f,
                Seed + 21, flick, 3f,
                new Color(190, 120, 255), new Color(255, 240, 220), 0.4f, 8);

            // === 6. EL ANILLO DE ONDA (la presión de la muerte expandiéndose) ===
            OndaLib.Pulse(Main.spriteBatch, pos,
                (lifeT * 2.5f) % 1f, R * 1.9f,
                new Color(255, 190, 90), 0.30f, Seed + 31);

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
