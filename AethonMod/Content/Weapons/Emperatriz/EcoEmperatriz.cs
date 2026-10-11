using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace AethonMod.Content.Weapons.Emperatriz
{
    /// <summary>
    /// EL ECO DE LA EMPERATRIZ — OPCIÓN B: EL VANILLA + HUE + FX.
    ///
    /// Dispara el Nightglow DE TERRARIA (el proyectil 931 — la misma IA
    /// AI_171 del ataque de la Emperatriz) exactamente como el Grimorio
    /// del Eterno, pero con EL HUE ARREGLADO y los FX de la casa:
    ///
    ///   · EL HUE (el hallazgo del estudio §12): el 931 renderiza su
    ///     color desde ai[1] — el Grimorio del Eterno NO lo pasa, así
    ///     que todos sus orbes salen CIAN fijos. Aquí va LA FÓRMULA
    ///     LITERAL DEL ARMA ORIGINAL: el matiz avanza con el progreso
    ///     de la animación + el miscCounter del jugador (rotación
    ///     completa del arcoíris cada ~5 s) → cada ráfaga sale con su
    ///     color y la paleta ROTA entre usos, como el Nightglow de
    ///     verdad.
    ///   · LA CADENA EXACTA: useTime 2 + animación 36 con ventana de 8
    ///     ticks → 4 orbes por uso, cada uno con la FUENTE de vanilla
    ///     (aleatoria, siempre hacia arriba).
    ///   · LOS FX GLOBALES (EmperatrizFX, marca ai[2]=9101): luz
    ///     prismática del color del orbe + destellos arcoíris + el
    ///     anillo giratorio de la casa debajo del sprite vanilla.
    ///
    /// La IA es 100% de Terraria — no se toca (y ahí está la gracia de
    /// la opción B: comparar el comportamiento puro contra los clones).
    /// Sin maná (regla de la casa — arsenal de pruebas).
    /// </summary>
    public class EcoEmperatriz : ModItem
    {
        // el anti-doble de la casa (mismo frame)
        private static uint _ultimoFrame = 0;

        public override void SetDefaults()
        {
            Item.damage = 60;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 2;
            Item.useAnimation = 36;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.knockBack = 2.5f;
            Item.value = Item.buyPrice(0, 10, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item82;   // el del Nightglow
            Item.autoReuse = true;
            Item.shoot = 931;   // Nightglow — el vanilla de verdad (el número, como GrimoireEternal)
            Item.shootSpeed = 17f;
            Item.mana = 0;                    // regla de la casa: sin maná
            Item.noMelee = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // === LA VENTANA DEL BURST (el gate del Nightglow: solo los
            // primeros 8 ticks de la animación de 36 → 4 disparos) ===
            if (player.itemAnimation < player.itemAnimationMax - 8) return false;

            // el anti-doble de la casa
            uint frame = Main.GameUpdateCount;
            if (frame == _ultimoFrame) return false;
            _ultimoFrame = frame;

            // === LA FUENTE: velocidad aleatoria SIEMPRE hacia arriba ===
            Vector2 v = Main.rand.NextVector2Circular(1f, 1f)
                      + Main.rand.NextVector2CircularEdge(3f, 3f);
            if (v.Y > 0f) v.Y *= -1f;

            // el punto de nacimiento vanilla: 15px delante, 3px abajo
            Vector2 pos = player.MountedCenter + new Vector2(player.direction * 15f, player.gravDir * 3f);

            // === EL HUE ARREGLADO — la fórmula LITERAL del Nightglow
            // (Player.cs del decompile): progreso de la animación ×0.66
            // + miscCounterNormalizado, módulo 1 ===
            float hue = ((float)player.itemAnimation / (float)player.itemAnimationMax * 0.66f
                + player.miscCounter / 300f) % 1f;

            Projectile.NewProjectile(source, pos, v, 931,
                damage, knockback, player.whoAmI,
                -1f,     // ai[0]: sin objetivo aún (el gate del homing vanilla)
                hue,     // ai[1]: EL HUE — el arcoíris de verdad
                9101f);  // ai[2]: la marca del FX (EmperatrizFX)

            return false;   // el spawn es todo nuestro
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
