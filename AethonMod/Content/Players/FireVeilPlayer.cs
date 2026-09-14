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
    /// FireVeilPlayer — v6.23 — EL PORTADOR DE LA ENVOLTURA DE FUEGO.
    ///
    /// Mantiene el CAMPO DE INTENSIDADES del fuego (11×11 celdas — el
    /// abrazo pegado a la silueta, v6.23) y lo hace VIVIR con las
    /// acciones del jugador:
    ///   - Escanea los huecos de accesorio (funcionales + vanidad) buscando
    ///     la Envoltura de Fuego Primordial — un cosmético es un cosmético
    ///     viva donde lo pongas.
    ///   - Cada tick AVANZA la simulación con el VIENTO y el AVIVO de la
    ///     velocidad (FireVeilRenderer.Step).
    ///   - Las CHISPAS escapan de la silueta (más al correr) y el HUMO
    ///     sube de las puntas más altas.
    ///   - La LUZ cálida del fuego ilumina la noche alrededor.
    /// </summary>
    public class FireVeilPlayer : ModPlayer
    {
        /// <summary>¿Lleva la Envoltura de Fuego puesta?</summary>
        public bool FireVeil;

        /// <summary>EL CAMPO: W×H intensidades 0..36 (el estado del fuego).</summary>
        private int[] _grid;

        /// <summary>El brillo global actual (fade-in al equipar).</summary>
        private float _warmup;

        public override void ResetEffects()
        {
            FireVeil = false;
        }

        public override void PostUpdate()
        {
            // === ESCANEO DE HUECOS: accesorios funcionales (3..9) + vanidad
            //     (13..19) — en cualquier lado cuenta (patrón CosmeticPlayer).
            int fireType = ModContent.ItemType<FireVeilItem>();
            for (int i = 3; i <= 19; i++)
            {
                if (i >= 10 && i <= 12) continue;   // vanidad de armadura
                Item item = Player.armor[i];
                if (item == null || item.IsAir) continue;
                if (item.type == fireType) { FireVeil = true; break; }
            }

            if (Main.netMode == NetmodeID.Server) return;

            if (!FireVeil || Player.dead)
            {
                // El campo se enfría suavemente al desequipar (no desaparece
                // de golpe: las últimas brasas se apagan solas).
                if (_grid != null)
                {
                    bool any = false;
                    for (int i = 0; i < _grid.Length; i++)
                    {
                        if (_grid[i] > 0) { _grid[i] = (int)MathHelper.Clamp(_grid[i] - 3, 0, 36); any = true; }
                    }
                    if (!any) _grid = null;
                }
                _warmup = MathHelper.Clamp(_warmup - 0.05f, 0f, 1f);
                return;
            }

            // === EL CAMPO NACE con el jugador ===
            _grid ??= new int[FireVeilRenderer.W * FireVeilRenderer.H];
            _warmup = MathHelper.Clamp(_warmup + 0.06f, 0f, 1f);

            // === LOS ESTADOS DE MOVIMIENTO (la interacción) ===
            bool flying = Player.controlJump && Player.wingTime > 0f &&
                          Player.jump == 0 && Player.velocity.Y != 0f;
            bool falling = Player.velocity.Y > 5.5f && !Player.gravDir.HasNaNOrZero() &&
                           !Player.mount.Active;
            float speed = Player.velocity.Length();
            float fanning = MathHelper.Clamp(speed / 7f, 0f, 1f);

            // === UN TICK DEL FUEGO (la simulación con el viento) ===
            // (a 60 Hz el campo ya hierve; multiplicamos elSteps solo con
            // el extra de volar/caer — dentro de Step).
            FireVeilRenderer.Step(_grid, Player.width, Player.velocity, flying, falling);

            // === LA LUZ cálida del fuego (respirando, avivada al correr) ===
            float pulse = 0.85f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7.3f);
            float li = (0.75f + 0.45f * fanning) * _warmup;
            Lighting.AddLight(Player.Center, 0.95f * li * pulse, 0.48f * li * pulse, 0.14f * li * pulse);
            Lighting.AddLight(Player.Center - new Vector2(0f, Player.height * 0.6f * Player.gravDir),
                0.55f * li, 0.28f * li, 0.08f * li);

            // === LAS CHISPAS que escapan (más al correr — el fuego suelta
            //     brasas con el viento de tu carrera, EN CONTRA de tu marcha) ===
            int emberRate = (int)MathHelper.Lerp(26f, 7f, fanning);   // cada N ticks
            if (Main.rand.NextBool(Math.Max(emberRate, 4)))
            {
                float h = Main.rand.NextFloat(0.1f, 0.95f);
                Vector2 pos = Player.Center + new Vector2(
                    Main.rand.NextFloat(-0.6f, 0.6f) * Player.width,
                    (h - 0.5f) * Player.height * Player.gravDir);
                Vector2 vel = new Vector2(
                    -Player.velocity.X * 0.22f + Main.rand.NextFloat(-0.5f, 0.5f),
                    -Main.rand.NextFloat(0.8f, 1.8f) * Player.gravDir);
                Dust d = Dust.NewDustPerfect(pos, DustID.Torch, vel, 190,
                    FireVeilRenderer.ColorAt(Main.rand.Next(20, 37)), 0.9f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            // === EL HUMO de las puntas (v6.23: justo sobre la coronilla —
            // el licking de 3-4 px respira humo, nada más arriba) ===
            if (Main.rand.NextBool(40))
            {
                Vector2 tip = Player.Center - new Vector2(
                    Main.rand.NextFloat(-8f, 8f),
                    Player.height * (0.92f + Main.rand.NextFloat(0f, 0.12f)) * Player.gravDir);
                Dust d = Dust.NewDustPerfect(tip, DustID.Smoke,
                    new Vector2(Main.rand.NextFloat(-0.4f, 0.4f),
                                -Main.rand.NextFloat(0.5f, 1.1f) * Player.gravDir),
                    90, new Color(60, 50, 55), 1.2f);
                d.noGravity = false;
                d.fadeIn = 0.6f;
            }
        }

        /// <summary>El campo actual (para la capa de dibujado). Null si no arde.</summary>
        public int[] Grid => _grid;

        /// <summary>El brillo global (fade-in) del fuego.</summary>
        public float Warmup => _warmup;
    }

    /// <summary>Utilidad interna: NaN-check barato para gravDir.</summary>
    internal static class GravUtil
    {
        public static bool HasNaNOrZero(this float f) => float.IsNaN(f) || f == 0f;
    }
}
