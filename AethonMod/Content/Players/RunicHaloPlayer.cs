using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// RunicHaloPlayer — v6.23 — LA ENERGÍA DE VUELO DE LOS ANILLOS DEL SOL IV.
    ///
    /// Petición original: "cuando el jugador VA A VOLAR, estos anillos
    /// BRILLAN CON INTENSIDAD". Este ModPlayer acumula la ENERGÍA DE
    /// VUELO (0..1): sube rápido mientras el jugador VUELA de verdad y
    /// decae suave al soltar — el sistema orbital se aviva y se aquieta
    /// como un motor.
    ///
    /// v6.23: el brillo vive ACOTADO al lenguaje de los soles (petición
    /// del usuario) — la luz del mundo y las chispas también se
    /// contienen al nivel solar.
    /// </summary>
    public class RunicHaloPlayer : ModPlayer
    {
        /// <summary>LA ENERGÍA DE VUELO (0..1) — el brillo del anillo.</summary>
        public float FlightEnergy;

        public override void ResetEffects() { }

        public override void PostUpdate()
        {
            // === EL ESTADO DE VUELO (el patrón probado de las alas) ===
            bool flying = Player.controlJump && Player.wingTime > 0f &&
                          Player.jump == 0 && Player.velocity.Y != 0f;

            // LA ENERGÍA: sube rápida al volar, decae suave al soltar.
            if (flying)
                FlightEnergy = MathHelper.Clamp(FlightEnergy + 0.09f, 0f, 1f);
            else
                FlightEnergy = MathHelper.Clamp(FlightEnergy - 0.035f, 0f, 1f);

            if (Main.netMode == NetmodeID.Server) return;
            if (Player.dead) return;

            // === ANCLA: la ESPALDA ALTA (omóplatos), con gravedad ===
            Vector2 back = Player.Center + new Vector2(0f, -Player.height * 0.145f * Player.gravDir);
            float scale = Player.height / 42f;

            // === LAS CHISPAS del sistema (a borbotones al volar — menos
            //     spam que v6.22: el sistema es ELEGANTE como el sol) ===
            int rate = flying ? 10 : 40;
            if (Main.rand.NextBool(rate))
            {
                int g = Main.rand.Next(RunicHaloRenderer.Glyphs);
                Vector2 glyph = RunicHaloRenderer.GetGlyphPosition(
                    back, scale, Main.GlobalTimeWrappedHourly, g);
                // La chispa sale TANGENCIALMENTE (gira con el anillo).
                Vector2 tang = (glyph - back).RotatedBy(MathHelper.PiOver2);
                tang.Normalize();
                Dust d = Dust.NewDustPerfect(glyph, DustID.Enchanted_Gold,
                    tang * Main.rand.NextFloat(0.8f, 2.0f) * (0.4f + 1.0f * FlightEnergy) -
                    new Vector2(0f, 0.5f),
                    180, new Color(255, 225, 150), 0.7f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // === LA LUZ del sistema (el motor encendido — contenido al
            //     nivel solar, nada del faro cegador de v6.22) ===
            float li = 0.35f + 0.75f * FlightEnergy;
            Lighting.AddLight(back, 0.75f * li, 0.60f * li, 0.28f * li);
        }
    }
}
