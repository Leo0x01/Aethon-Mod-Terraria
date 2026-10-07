using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.CodiceVivo
{
    /// <summary>
    /// CODICEVIVOPROJECTILE — v6.50.71 — EL CÓDICE QUE VUELA (la prueba del
    /// sprite-de-código del usuario).
    ///
    /// EL SPRITE: el strip de 8 frames (128×1024 — la matriz de píxeles del
    /// usuario ANIMADA por código: el pulso de energía + el parpadeo del
    /// ojo) — un frame cada 7 t de juego (el frameCounter corre al doble
    /// por el extraUpdates).
    ///
    /// EL VUELO (tres actos): LANZA (sale de las manos del portador al
    /// cursor, frenando) → VELA (flota con su vaivén, el ojo BUSCANDO — a
    /// los 10/40/70 t escupe una CHISPA violeta autoguiada al enemigo más
    /// cercano) → RECOGE (vuelve acelerando al portador como bumerán y se
    /// guarda con un suspiro de bruma). El retroceso visual: DOS ESTELAS
    /// del propio sprite animado (oldPos) + el halo violeta que respira.
    /// </summary>
    public class CodiceVivoProjectile : ModProjectile
    {
        private const byte FASE_LANZA = 0;
        private const byte FASE_VELA = 1;
        private const byte FASE_RECOGE = 2;

        public override void SetStaticDefaults()
        {
            // EL STRIP ANIMADO: 8 frames — la tinta late y el ojo PARPADEA
            Main.projFrames[Type] = 8;
        }

        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 44;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 480;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
            Projectile.extraUpdates = 1;
        }

        /// <summary>El enemigo más cercano en radio (el ojo del códice busca).</summary>
        private static NPC PresaCercana(Vector2 desde, float radio)
        {
            NPC mejor = null;
            float mejorD = radio * radio;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.life <= 0 || n.dontTakeDamage) continue;
                if (n.friendly || n.townNPC) continue;
                float d = Vector2.DistanceSquared(n.Center, desde);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            byte fase = (byte)Projectile.ai[1];
            Player dueño = Main.player[Projectile.owner];

            // === LA ANIMACIÓN DEL SPRITE: un frame cada 7 t de juego (la AI
            // corre ×2 por el extraUpdates — el contador avanza al doble) ===
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 14)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % 8;
            }

            // el vaivén del libro flotante (siempre vivo, incluso volando)
            Projectile.rotation = MathF.Sin(t * 0.045f) * 0.10f;

            switch (fase)
            {
                case FASE_LANZA:
                {
                    Projectile.velocity *= 0.952f;
                    if (t == 1f)
                        Sonar(SoundID.Item74.WithPitchOffset(-0.15f).WithVolumeScale(0.7f), Projectile.Center);
                    if (Projectile.velocity.Length() < 4.5f || t > 52f)
                    {
                        Projectile.ai[1] = FASE_VELA;
                        Projectile.ai[2] = 0f;
                    }
                    break;
                }
                case FASE_VELA:
                {
                    // flota: frena del todo y BOBA (la levitación del tomo)
                    Projectile.velocity *= 0.86f;
                    Projectile.velocity.Y += MathF.Sin(t * 0.11f) * 0.16f;
                    Projectile.velocity.X += MathF.Sin(t * 0.07f) * 0.08f;

                    // LAS TRES VOLEAS de chispas (los ojos del códice disparan)
                    if (Main.myPlayer == Projectile.owner && (t == 10f || t == 40f || t == 70f))
                    {
                        NPC presa = PresaCercana(Projectile.Center, 760f);
                        Vector2 rumbo = presa != null
                            ? (presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX)
                            : -Vector2.UnitY;
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center,
                            rumbo * 12f, ModContent.ProjectileType<CodiceVivoChispa>(),
                            (int)(Projectile.damage * 0.6f), 2f, Projectile.owner,
                            presa != null ? presa.whoAmI + 1 : 0, 0);
                        Sonar(SoundID.Item12.WithPitchOffset(0.35f).WithVolumeScale(0.45f), Projectile.Center);
                    }

                    if (t >= 96f)
                    {
                        Projectile.ai[1] = FASE_RECOGE;
                        Projectile.ai[2] = 0f;
                        Sonar(SoundID.Item74.WithPitchOffset(0.25f).WithVolumeScale(0.5f), Projectile.Center);
                    }
                    break;
                }
                case FASE_RECOGE:
                {
                    // EL BUMERÁN: acelera hacia su dueño y se guarda al tocarlo
                    Vector2 hacia = (dueño.MountedCenter - Projectile.Center).SafeNormalize(-Vector2.UnitY);
                    Projectile.velocity += hacia * 1.05f;
                    float vel = Projectile.velocity.Length();
                    if (vel > 26f) Projectile.velocity *= 26f / vel;

                    if (Vector2.DistanceSquared(Projectile.Center, dueño.MountedCenter) < 42f * 42f)
                    {
                        Sonar(SoundID.Item122.WithPitchOffset(0.3f).WithVolumeScale(0.35f), Projectile.Center);
                        Projectile.Kill();
                        return;
                    }
                    break;
                }
            }

            // polvillo violeta del vuelo
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(9))
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(20, 20), 40, 40,
                    DustID.Shadowflame, 0f, -0.25f, 128, default, 0.45f);
                Main.dust[d].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            // === EL HALO VIOLETA que respira (el aura del tomo vivo) ===
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                VFXCore.Begin();
                VFXCore.Quad(Projectile.Center, SombrasLib.Alfa(SombrasLib.Violeta,
                    (0.14f + 0.05f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.6f))),
                    new Vector2(150f, 150f));
                VFXCore.FlushAdditive();
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();

            // === LAS ESTELAS + EL SPRITE (el strip animado: el frame actual) ===
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int alto = tex.Height / Main.projFrames[Projectile.type];
            Rectangle src = new(0, Projectile.frame * alto, tex.Width, alto);
            Vector2 origen = new(tex.Width * 0.5f, alto * 0.5f);
            Vector2 pantalla = Projectile.Center - Main.screenPosition;
            Color luz = Lighting.GetColor(Projectile.Center.ToTileCoordinates());

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.Transform);
            try
            {
                // DOS ESTELAS del propio sprite (los frames recientes, tenues)
                for (int k = 2; k <= 4; k += 2)
                {
                    if (Projectile.oldPos.Length <= k) break;
                    Vector2 vieja = Projectile.oldPos[k];
                    if (vieja == Vector2.Zero) continue;
                    Vector2 posV = vieja + new Vector2(Projectile.width, Projectile.height) * 0.5f - Main.screenPosition;
                    Main.EntitySpriteDraw(tex, posV, src, luz * (0.30f - k * 0.06f),
                        Projectile.rotation * 0.6f, origen, 1f - k * 0.06f, SpriteEffects.None, 0f);
                }
                // EL CÓDICE
                Main.EntitySpriteDraw(tex, pantalla, src, luz,
                    Projectile.rotation, origen, 1f, SpriteEffects.None, 0f);
            }
            catch { }
            Main.spriteBatch.End();
            return false;
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
