using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Effects.Bruma;

namespace AethonMod.Content.Projectiles.Cosmic
{
    // ======================================================================
    //  v6.51.4 — LA BRUMA DEL NACIMIENTO (la letra: «con respecto a las
    //  ultimas brumas que estas usando, las de la llegada y las del
    //  nacimiento de soles runicos y agujeros negros runicos, me
    //  pregunto, que brumas estas usando, teniamos ya una buena
    //  libreria de brumas completamente funcional que no ponen esos
    //  circulos planos… esos circulos planos estan mal, solo se debe
    //  cubrir con bruma, nada geometria plana»).
    //
    //  LA RESPUESTA A LA PREGUNTA: los círculos planos NO eran una
    //  librería nueva — eran SOFTGLOW (el pincel de luz radial de
    //  VFXCore) usado como si fuera niebla: discos lisos donde debía
    //  vivir BRUMA. Este proyectil es LA CURA: el nacimiento de un sol
    //  rúnico o de un agujero negro se cubre con LA LIBRERÍA DE LA
    //  CASA (BrumaFX — los puffs de humo fBm del flipbook que se
    //  DESGARRAN de verdad, los que visten al Grimorio Dormido y a las
    //  fauces de la sombra): una nube del COLOR de lo que nace (ORO
    //  para el sol · VIOLETA para el agujero) que EXHALA, crece y se
    //  disuelve — humo de verdad, cero geometría.
    //
    //  ai[0] = 1 SOL (dorado) · 2 AGUJERO (violeta) · ai[1] = la
    //  intensidad (0..1 — el nacimiento del supremo respira más).
    // ======================================================================
    public class BrumaNacimientoFX : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private const int VIDA = 46;

        private bool EsSol => (int)Projectile.ai[0] == 1;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = VIDA;
            Projectile.netImportant = false;   // cosmético: cada cable la suya
        }

        public override void AI()
        {
            // LA EXHALACIÓN SUBE (el humo frío de lo que acaba de nacer).
            Projectile.velocity *= 0.94f;
            Projectile.velocity.Y -= 0.02f;

            // LA LUZ de la nube (tenue, del color que es).
            if (!EsSol && Main.netMode != NetmodeID.Server)
                Lighting.AddLight(Projectile.Center, 0.14f, 0.06f, 0.24f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                float edad = VIDA - Projectile.timeLeft;
                float vida = edad / VIDA;                       // 0 → 1
                // LA ENVOLVENTE: nace GORDA (el estallido se asienta) y se
                // disuelve lenta — la firma de la bruma de la casa.
                float densidad = MathHelper.Clamp(1.15f - vida * 1.05f, 0f, 1f)
                    * MathHelper.Clamp(vida * 6f, 0f, 1f);
                if (densidad <= 0.03f) return false;

                float radio = (46f + 54f * vida) * MathHelper.Clamp(
                    0.7f + 0.3f * Projectile.ai[1], 0.7f, 1.6f);
                int semilla = Projectile.identity % 113 + 5;
                Vector2 centro = Projectile.Center - Main.screenPosition;

                // LA NUBE DEL COLOR QUE NACE (BrumaFX: humo fBm de verdad).
                Color masa = EsSol
                    ? new Color(96, 66, 22)      // el humo DORADO (oscuro: la luz la pone el sol)
                    : new Color(52, 24, 84);     // el humo VIOLETA (la bruma del vacío)

                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    BrumaFX.BeginMass();
                    BrumaFX.Cloud(centro, radio, masa, semilla, t,
                        Math.Max(4, (int)(6 * densidad) + 3), alpha: 0.66f * densidad);
                    BrumaFX.Cloud(centro + new Vector2(0f, -radio * 0.22f), radio * 0.62f,
                        EsSol ? new Color(120, 88, 34) : new Color(72, 38, 112),
                        semilla + 41, t * 0.85f, 4, alpha: 0.40f * densidad);
                }
                catch { }
                finally
                {
                    try { Main.spriteBatch.End(); } catch { }
                    VFXCore.CerrarLoteSiAbierto();
                }

                // EL ALIENTO (la lectura nocturna): un soplo ADITIVO del
                // color — VFXCore, coords de mundo, el patrón de la casa.
                try
                {
                    VFXCore.Begin();
                    Color aliento = EsSol
                        ? new Color(255, 208, 110)
                        : new Color(150, 84, 226);
                    for (int k = 0; k < 3; k++)
                    {
                        float late = 0.6f + 0.4f * MathF.Sin(t * (1.2f + k * 0.31f) + k * 2.1f);
                        VFXCore.Quad(Projectile.Center +
                            new Vector2(MathF.Sin(t * 0.7f + k * 2.4f) * radio * 0.3f,
                                        -radio * 0.18f * k),
                            aliento * (0.10f * densidad * late),
                            new Vector2(radio * (1.15f - k * 0.28f),
                                        radio * (0.9f - k * 0.22f)));
                    }
                    VFXCore.FlushAdditive();
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                VFXCore.ReabrirLoteVanilla();
            }
            catch { VFXCore.ReabrirLoteVanilla(); }
            return false;
        }
    }
}
