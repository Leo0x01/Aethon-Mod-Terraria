using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Items.Cosmetics;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// StormVeilPlayer — v6.23 — EL PORTADOR DE LA ENVOLTURA DE RAYOS.
    ///
    /// Mantiene viva la ENVOLTURA DE RAYOS sobre el jugador:
    ///   - Escanea los huecos de accesorio (funcionales + vanidad)
    ///     buscando la Envoltura de Rayos Primordial — un cosmético es un
    ///     cosmético viva donde lo pongas (patrón FireVeilPlayer).
    ///   - Acumula la ENERGÍA DE LA TORMENTA (0..1): la velocidad la
    ///     SUBE rápido y la calma la baja suave — quieto es una brisa
    ///     eléctrica, corriendo es una tormenta encendida.
    ///   - Las CHISPAS eléctricas saltan del contorno de la silueta (más
    ///     frecuentes con la energía).
    ///   - La LUZ fría-oro del rayo parpadea con el flick de descarga.
    /// </summary>
    public class StormVeilPlayer : ModPlayer
    {
        /// <summary>¿Lleva la Envoltura de Rayos puesta?</summary>
        public bool StormVeil;

        /// <summary>LA ENERGÍA de la tormenta (0..1) — la velocidad acumulada.</summary>
        public float Energy;

        /// <summary>El brillo global actual (fade-in al equipar).</summary>
        private float _warmup;

        public override void ResetEffects()
        {
            StormVeil = false;
        }

        public override void PostUpdate()
        {
            // === ESCANEO DE HUECOS: accesorios funcionales (3..9) + vanidad
            //     (13..19) — en cualquier lado cuenta (patrón FireVeilPlayer).
            int stormType = ModContent.ItemType<StormVeilItem>();
            for (int i = 3; i <= 19; i++)
            {
                if (i >= 10 && i <= 12) continue;   // vanidad de armadura
                Item item = Player.armor[i];
                if (item == null || item.IsAir) continue;
                if (item.type == stormType) { StormVeil = true; break; }
            }

            if (Main.netMode == NetmodeID.Server) return;

            if (!StormVeil || Player.dead)
            {
                // La tormenta se APAGA suave al desequipar (la última
                // chispa salta sola, no desaparece de golpe).
                Energy = MathHelper.Clamp(Energy - 0.04f, 0f, 1f);
                _warmup = MathHelper.Clamp(_warmup - 0.05f, 0f, 1f);
                return;
            }

            _warmup = MathHelper.Clamp(_warmup + 0.06f, 0f, 1f);

            // === LA ENERGÍA DE LA TORMENTA (la interacción con el
            //     movimiento): la velocidad la sube RÁPIDO, la calma la
            //     baja suave — correr ENCIENDE la envoltura. ===
            float speed = Player.velocity.Length();
            float target = MathHelper.Clamp(speed / 7f, 0f, 1f);
            if (target > Energy)
                Energy = MathHelper.Clamp(Energy + 0.08f, 0f, target);
            else
                Energy = MathHelper.Clamp(Energy - 0.035f, target, 1f);

            // === LA LUZ fría-oro del rayo — parpadea con el flick de
            //     descarga (el stutter eléctrico de verdad) ===
            float pulse = 0.80f + 0.20f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 9.3f);
            float li = (0.45f + 0.65f * Energy) * _warmup;
            Lighting.AddLight(Player.Center,
                0.80f * li * pulse, 0.78f * li * pulse, 0.62f * li * pulse);

            // === LAS CHISPAS eléctricas del contorno (más frecuentes con
            //     la energía — la silueta SUELTA carga al moverse) ===
            int rate = (int)MathHelper.Lerp(34f, 9f, Energy);   // cada N ticks
            if (Main.rand.NextBool(Math.Max(rate, 4)))
            {
                // Un punto al AZAR del carril (la elipse de la silueta).
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                float rx = Math.Max(Player.width * 0.75f, 13f) + 3f;
                float ry = Player.height * 0.55f;
                Vector2 pos = Player.Center + new Vector2(
                    (float)Math.Cos(ang) * rx,
                    (float)Math.Sin(ang) * ry);
                // La chispa sale RADIAL, con el viento en contra de la marcha.
                Vector2 vel = new Vector2(
                    (float)Math.Cos(ang) * Main.rand.NextFloat(0.6f, 1.8f) - Player.velocity.X * 0.15f,
                    (float)Math.Sin(ang) * Main.rand.NextFloat(0.6f, 1.8f) - 0.4f);
                Dust d = Dust.NewDustPerfect(pos, DustID.Electric, vel, 170,
                    Main.rand.NextBool(3) ? new Color(190, 210, 255) : new Color(255, 235, 170),
                    0.65f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        /// <summary>La energía de la tormenta (para la capa de dibujado).</summary>
        public float StormEnergy => Energy;

        /// <summary>El brillo global (fade-in) de la envoltura.</summary>
        public float Warmup => _warmup;
    }
}
