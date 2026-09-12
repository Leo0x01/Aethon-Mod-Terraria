using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// CosmeticPlayer — v6.03 — EL RASTREADOR DE COSMÉTICOS DEL JUGADOR.
    ///
    /// Mantiene las banderas de qué coronas lleva puestas el jugador
    /// (escaneando TANTO los huecos de accesorio funcionales como los de
    /// VANIDAD — un cosmético es un cosmético viva donde lo pongas) y hace
    /// que vivan en el mundo:
    ///   - La CORONA DE ARCOS suelta ascuas rosas sobre sus ápices.
    ///   - La CORONA RÚNICA emite chispas ascendentes desde las perlas.
    ///   - Ambas iluminan suavemente la noche con su color.
    /// </summary>
    public class CosmeticPlayer : ModPlayer
    {
        /// <summary>¿Lleva la Corona de la Reina del Vacío (arcos de neón)?</summary>
        public bool VoidCrown;

        /// <summary>¿Lleva la Corona Rúnica Estelar (glifos flotantes)?</summary>
        public bool RuneCrown;

        public override void ResetEffects()
        {
            VoidCrown = false;
            RuneCrown = false;
        }

        public override void PostUpdate()
        {
            // === ESCANEO DE HUECOS: accesorios funcionales (3..9) + de
            // vanidad (13..19) — en cualquier lado cuenta.
            int voidType = ModContent.ItemType<Items.Cosmetics.VoidCrownItem>();
            int runeType = ModContent.ItemType<Items.Cosmetics.RuneCrownItem>();

            for (int i = 3; i <= 19; i++)
            {
                // Salto los huecos de armadura/vanidad de armadura (9..12 no
                // existen como tales: 0-2 armadura, 3-9 accesorios,
                // 10-12 vanidad de armadura, 13-19 vanidad de accesorios).
                if (i >= 10 && i <= 12) continue;

                Item item = Player.armor[i];
                if (item == null || item.IsAir) continue;
                if (item.type == voidType) VoidCrown = true;
                else if (item.type == runeType) RuneCrown = true;
            }

            if (Main.netMode == NetmodeID.Server) return;
            if (Player.dead) return;

            // Centro de la cabeza (respeta la gravedad invertida).
            Vector2 head = Player.Center - new Vector2(0f, Player.height * 0.22f * Player.gravDir);
            float scale = Player.height / 42f;

            // === LA CORONA DE ARCOS VIVE: ascuas rosas que se alzan sobre
            // los ápices (la corona es energía, no un adorno estático).
            if (VoidCrown)
            {
                float horizonPx = Player.width * 0.55f;
                if (Main.rand.NextBool(18))
                {
                    Vector2 emberPos = head + new Vector2(
                        Main.rand.NextFloat(-1.3f, 1.3f) * horizonPx,
                        -horizonPx * (1.1f + Main.rand.NextFloat(0.8f, 2.2f)) * Player.gravDir);
                    Dust d = Dust.NewDustPerfect(emberPos, DustID.Enchanted_Pink,
                        new Vector2(Main.rand.NextFloat(-0.5f, 0.5f),
                                    -Main.rand.NextFloat(0.7f, 1.5f) * Player.gravDir),
                        180, new Color(255, 175, 215), 0.8f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // Luz carmesí suave de la corona.
                Lighting.AddLight(head, new Vector3(0.30f, 0.06f, 0.12f));
            }

            // === LA CORONA RÚNICA RESPIRA LUZ: chispas ascendentes desde
            // las perlas de los glifos.
            if (RuneCrown)
            {
                if (Main.rand.NextBool(28))
                {
                    int g = Main.rand.Next(RuneCrownRenderer.GlyphCount);
                    Vector2 pearl = RuneCrownRenderer.GetPearlPosition(
                        head, scale, Main.GlobalTimeWrappedHourly, g);
                    Dust d = Dust.NewDustPerfect(pearl, DustID.Enchanted_Pink,
                        new Vector2(Main.rand.NextFloat(-0.3f, 0.3f),
                                    -Main.rand.NextFloat(0.5f, 1.1f) * Player.gravDir),
                        160, new Color(255, 200, 220), 0.7f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }

                // Luz rosa tenue del arco rúnico.
                Lighting.AddLight(head - new Vector2(0f, 18f * Player.gravDir),
                    new Vector3(0.22f, 0.04f, 0.14f));
            }
        }
    }
}
