using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles
{
    /// <summary>
    /// PRIMERTRAZOHELD — v6.52.0 — EL CONTROLADOR DEL TRAZO (la receta
    /// vanilla del Last Prism 633, casa-ificada):
    ///
    ///   · Vive en la mano (Center = MountedCenter), intocable.
    ///   · EL APUNTE CON INERCIA: velocity = Normalize(Lerp(mira, velocity,
    ///     0.92)) — el rayo BARRER con peso (el «motion-blur» del barrido:
    ///     la punta se desliza, jamás salta).
    ///   · LA CONVERGENCIA: al primer tick nacen CINCO fajos sobre un anillo
    ///     giratorio de radio 20→4 px; a plena carga (180 t) el abanico se
    ///     CIERRA y los cinco se funden en UN rayo (daño ×3, el momento
    ///     «lock» del Prism).
    ///   · EL ZUMBIDO: SoundID.Item15 cada 20 t (el loop del Prism).
    ///
    /// Dibuja LA PLUMA (el sprite del ítem) en la mano apuntando al ratón
    /// + el halo de carga; los fajos dibujan el rayo ellos mismos.
    /// </summary>
    public class PrimerTrazoHeld : ModProjectile
    {
        // v6.52.1 — LA CURA DE LAS 9 TEXTURAS: el placeholder de la
        // casa (dibuja el sprite del ÍTEM, no su propio asset).
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        // LA CARGA: 0→1 en 180 ticks (3 s — el número del Prism vanilla).
        public float Carga => MathHelper.Clamp((Projectile.ai[0] - 1f) / 179f, 0f, 1f);

        // ¿Ya soltó el clic? (ai[1] = 1 — el estado de release, visto por
        // TODAS las máquinas para que los fajos se despidan en sincronía).
        public bool Soltado => Projectile.ai[1] >= 1f;

        public override void SetStaticDefaults()
        {
            // DisplayName: Items.PrimerTrazo (el arma); este no se anuncia.
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = false;          // el daño lo llevan los FAJOS
            Projectile.penetrate = -1;
            Projectile.timeLeft = 6;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player dueño = Main.player[Projectile.owner];
            if (dueño == null || dueño.dead || !dueño.active)
            {
                Projectile.Kill();
                return;
            }

            // LA MANO: clavado en el MountedCenter, mirando al frente.
            Projectile.Center = dueño.MountedCenter;
            Projectile.timeLeft = 2;
            dueño.heldProj = Projectile.whoAmI;

            // === EL APUNTE CON INERCIA (la receta R2 §6.1) ===
            if (Main.myPlayer == Projectile.owner && !Soltado)
            {
                Vector2 mira = Main.MouseWorld - dueño.MountedCenter;
                if (mira.LengthSquared() < 0.001f) mira = -Vector2.UnitY;
                mira.Normalize();
                if (dueño.gravDir == -1f)
                    mira.Y = -mira.Y;
                Vector2 nueva = Vector2.Lerp(mira, Projectile.velocity.SafeNormalize(-Vector2.UnitY), 0.92f);
                if (nueva.LengthSquared() < 0.001f) nueva = mira;
                nueva.Normalize();
                if (nueva != Projectile.velocity)
                    Projectile.netUpdate = true;
                Projectile.velocity = nueva;
            }

            // === LA LIBERACIÓN (el release de la receta: 12 t de fade con
            // el widthMul → 0 del haz — el rayo EXPIRA, no muere) ===
            if (Soltado)
            {
                Projectile.ai[2] += 1f;
                if (Projectile.ai[2] > 14f)
                    Projectile.Kill();
            }
            else if (Main.myPlayer == Projectile.owner)
            {
                // El canal vivo de vanilla: canal roto o jugador fuera de
                // combate → release (NO Kill directo: el fade es de la casa).
                if (!dueño.channel || dueño.noItems || dueño.CCed || dueño.dead)
                {
                    Projectile.ai[1] = 1f;
                    Projectile.netUpdate = true;
                }
            }

            // === EL ZUMBIDO DEL PRISM (Item15 cada 20 t) ===
            if (!Soltado && Projectile.soundDelay <= 0)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item15, Projectile.Center);
                Projectile.soundDelay = 20;
            }

            // === EL NACIMIENTO DE LOS FAJOS (tick 1, solo el dueño) ===
            if (Projectile.ai[0] == 1f && Main.myPlayer == Projectile.owner)
            {
                for (int i = 0; i < 5; i++)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                        Projectile.Center, Projectile.velocity,
                        ModContent.ProjectileType<PrimerTrazoFajo>(),
                        Projectile.damage, Projectile.knockBack,
                        Projectile.owner, i, Projectile.whoAmI);
                }
            }
            Projectile.ai[0] += 1f;

            // === EL MOMENTO «LOCK» (carga 1: el flash de la fusión) ===
            if (Projectile.ai[0] == 180f)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item75.WithPitchOffset(0.35f),
                    Projectile.Center);
                if (Main.netMode != NetmodeID.Server)
                    Pantalla.Flash(new Color(255, 250, 235), 0.18f, 0.35f);
            }

            // === LA LUZ DE LA BOCA (el canal encendido) ===
            if (Main.netMode != NetmodeID.Server)
                Lighting.AddLight(Projectile.Center, 0.55f * (0.4f + 0.6f * Carga),
                    0.5f * (0.4f + 0.6f * Carga), 0.75f * (0.4f + 0.6f * Carga));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            // === LA CARGA VISUAL (solo ENCOLA — el contrato de la casa) ===
            float carga = Carga;
            float tiempo = Main.GlobalTimeWrappedHourly;
            if (carga < 1f && !Soltado && VFXCore.Presupuesto(10))
            {
                // LAS MOTAS QUE CONVERGEN: 6 chispas orbitando en espiral
                // que SE CIERRAN hacia la boca a medida que carga (el
                // «converging charge» de la receta — deterministas por
                // Hash01, jamás Main.rand en render).
                Vector2 boca = Projectile.Center + Projectile.velocity * 18f;
                for (int i = 0; i < 6; i++)
                {
                    float h = VFXCore.Hash01(991, i, 3);
                    float radio = MathHelper.Lerp(52f, 10f, carga);
                    float ang = tiempo * (2.2f + h * 1.6f) + i * MathHelper.TwoPi / 6f
                                + h * MathHelper.TwoPi;
                    Vector2 pos = boca + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio
                                  * (1f - 0.25f * MathF.Sin(tiempo * 3f + h * 9f));
                    Color chispa = Main.hslToRgb((i / 6f + tiempo * 0.12f) % 1f, 1f, 0.72f);
                    VFXCore.Quad(pos, StormLib.Tint(chispa, 0.65f),
                        new Vector2(9f, 9f), VFXCore.GlowOrb);
                }
            }

            // EL HALO DE LA BOCA: crece con la carga (el «muzzle» carga).
            Color halo = new Color(235, 225, 255);
            VFXCore.Quad(Projectile.Center + Projectile.velocity * 16f,
                StormLib.Tint(halo, (0.25f + 0.55f * carga) * (Soltado ? 0.3f : 1f)),
                new Vector2(26f + 40f * carga, 26f + 40f * carga), VFXCore.SoftGlow);

            // === EL VOLCADO ÚNICO (la letra del contrato v6.51.14) ===
            if (VFXCore.QuadCount > 0)
            {
                try { VFXCore.FlushAdditive(); }
                finally { VFXCore.ReabrirLoteVanilla(); }
            }

            // === LA PLUMA EN LA MANO (lote vanilla REABIERTO — el sprite
            // del ítem girado al apunte, el flip vertical cuando apunta a
            // la izquierda: el dibujado de arma sostenida de siempre) ===
            Texture2D pluma = Terraria.GameContent.TextureAssets.Item[
                ModContent.ItemType<Content.Weapons.PrimerTrazo>()].Value;
            float rot = Projectile.velocity.ToRotation();
            Vector2 posPantalla = Projectile.Center - Main.screenPosition + Projectile.velocity * 10f;
            SpriteEffects volteo = Projectile.velocity.X < 0f
                ? SpriteEffects.FlipVertically : SpriteEffects.None;
            Main.spriteBatch.Draw(pluma, posPantalla, null, Color.White,
                rot + (Projectile.velocity.X < 0f ? 0f : MathHelper.PiOver2),
                new Vector2(pluma.Width * 0.5f, pluma.Height * 0.85f),
                1f, volteo, 0f);

            return false;   // el sprite del proyectil NUNCA se dibuja
        }
    }
}
