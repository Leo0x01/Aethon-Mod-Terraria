using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Sombras
{
    /// <summary>
    /// v6.144 — LA SOMBRA DE LA PÁGINA II: EL PROYECTIL REBOTÓN.
    ///
    /// LA LETRA: «fusiona la nueva arma de la sombra de la pagina con
    /// una el ataque de Muñeca Maldita de Llama Sombría, pero
    /// modificalo para que tome el tono de la casa… ademas de que
    /// mejora su animacion para que parezca que se mueven despues de
    /// ser lanzados».
    ///
    ///  · EL VUELO DE LA MUÑECA MALDITA (el Shadowflame hex doll):
    ///    el proyectil REBOTA en las paredes (hasta 5 veces, cada
    ///    rebote lo ACELERA ×1.12) y atraviesa enemigos mientras
    ///    rebota — la firma del arma de vanilla.
    ///  · EL TONO DE LA CASA: NO es una llama púrpura de vanilla: es
    ///    LA MASA LISA de la sombra de la página (MasaLisa — negro
    ///    con la vena violeta de la casa) que SE ESTIRA con la
    ///    velocidad y GOTEA bruma.
    ///  · LA ANIMACIÓN POST-LANZAMIENTO (la letra: «parezca que se
    ///    mueven despues de ser lanzados»): la masa NO es un sprite
    ///    quieto — ONDEA mientras vuela: la punta respira
    ///    (CabezaDeBruma), el cuerpo serpentea (ColumnaViva con el
    ///    gancho de inercia) y suelta un rastro de puffs que se
    ///    quedan VIVOS detrás.
    /// </summary>
    public class SombraPaginaIIProjectile : ModProjectile
    {
        private const int REBOTES_MAX = 5;

        // TODO el cuerpo es LA MASA LISA de la casa (el dibujado es
        // 100% código — como el SombraPagina original: la textura solo
        // cumple el contrato de tML; nunca se dibuja).
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            ProjectileID.Sets.CultistIsResistantTo[Type] = false;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;          // atraviesa mientras rebota (la muñeca)
            Projectile.timeLeft = 600;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Projectile.ai[0]++;                  // la edad
            float t = Projectile.ai[0];

            // LA MASA VIVA: mientras vuela, el rumbo del cuerpo sigue la
            // velocidad con INERCIA (la animación post-lanzamiento: la
            // sombra «nada» tras el disparo, no es un clavo recto)
            if (t > 6f)
            {
                float actual = Projectile.velocity.ToRotation();
                float deseado = Projectile.velocity.ToRotation();
                // el cabeceo: la punta ondula ±0.2 rad con el tiempo —
                // el aleteo de la página cayendo
                deseado += MathF.Sin(Main.GlobalTimeWrappedHourly * 9f +
                    Projectile.whoAmI * 2.1f) * 0.20f;
                Projectile.rotation = deseado;
                // la luz: la vena violeta de la casa
                Lighting.AddLight(Projectile.Center,
                    new Vector3(0.42f, 0.24f, 0.62f) * 0.7f);
            }

            // LA ESTELA DE BRUMA (el rastro que se queda vivo — cada
            // puff nace detrás y flota: la sombra SANGRA humo)
            if (Main.netMode != NetmodeID.Server &&
                Main.rand.NextBool(3))
            {
                Vector2 deriva = -Projectile.velocity * 0.12f +
                    new Vector2(MathF.Sin(t * 0.3f + Projectile.whoAmI) * 0.6f, -0.5f);
                SombrasLib.Bruma(Projectile.Center - Projectile.velocity * 0.35f,
                    16f + 8f * MathF.Sin(t * 0.21f), 0.40f,
                    Main.GlobalTimeWrappedHourly, Projectile.whoAmI * 31 + 7, deriva);
            }

            // LA MUERTE SUAVE: al agotar los rebotes se disipa (no
            // desaparece en seco)
            if (Projectile.ai[1] >= REBOTES_MAX && t > 540f) Projectile.Kill();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // EL REBOTE DE LA MUÑECA MALDITA: invierte el eje golpeado y
            // ACELERA — cada rebote lo hace más rápido (×1.12), hasta 5
            if (Projectile.ai[1] >= REBOTES_MAX)
                return true;                     // el último: muere

            Projectile.ai[1]++;
            if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
                Projectile.velocity.X = -oldVelocity.X;
            if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
                Projectile.velocity.Y = -oldVelocity.Y;
            Projectile.velocity *= 1.12f;        // la aceleración del rebote
            Projectile.netUpdate = true;

            // el golpe: bruma del impacto + el eco de la casa
            if (Main.netMode != NetmodeID.Server)
            {
                SombrasLib.Bruma(Projectile.Center, 34f, 0.66f,
                    Main.GlobalTimeWrappedHourly, Projectile.whoAmI * 13 + 3);
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item122.WithPitchOffset(0.18f).WithVolumeScale(0.35f),
                    Projectile.Center);
            }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // LA SOMBRA NO MUERDE (sin dientes): solo el roce frío —
            // una pulsación de bruma en el punto de contacto
            if (Main.netMode != NetmodeID.Server)
                SombrasLib.Bruma(target.Center, 26f, 0.5f,
                    Main.GlobalTimeWrappedHourly, Projectile.whoAmI * 7 + 11);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                float tiempo = Main.GlobalTimeWrappedHourly;
                int semilla = Projectile.whoAmI * 17 + 5;

                // EL CUERPO: LA MASA LISA DE LA CASA — la columna viva
                // del disparo a la punta (corta: es un proyectil, no un
                // tentáculo anclado) con el perfil fusiforme SERENO
                Vector2 raiz = Projectile.Center - Projectile.velocity * 1.15f;
                Vector2 punta = Projectile.Center + Projectile.velocity * 0.30f;
                Vector2[] col = SombrasLib.ColumnaViva(raiz, punta, tiempo, semilla,
                    9, 0.20f, gancho: 0.42f);

                // SIN OJOS, SIN DIENTES (la letra): solo la masa lisa
                SombrasLib.MasaLisa(col, 7f, 13f, 0.96f, semilla, tiempo);

                // LA PUNTA RESPIRA (la animación que sigue viva tras el
                // lanzamiento): la cabeza de bruma late con la velocidad
                float vigor = 0.7f + 0.3f * MathF.Sin(tiempo * 10f + semilla);
                Vector2 rumbo = (punta - col[Math.Max(col.Length - 3, 0)])
                    .SafeNormalize(Projectile.rotation.ToRotationVector2());
                SombrasLib.CabezaDeBruma(punta, rumbo, vigor, 0.9f, tiempo, semilla);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
