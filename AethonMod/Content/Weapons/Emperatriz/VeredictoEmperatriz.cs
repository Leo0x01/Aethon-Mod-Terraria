using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Terraria.Localization;
using AethonMod.Content.Projectiles.Emperatriz;

namespace AethonMod.Content.Weapons.Emperatriz
{
    /// <summary>
    /// EL VEREDICTO DE LA EMPERATRIZ — OPCIÓN C: EL HÍBRIDO DE LA JEFA.
    ///
    /// El arma que dispara EL TRAZO DE LA EMPERATRIZ: el streak 873 de
    /// la jefa (la AI_171 con PARÁMETROS DE JEFA: persecución desde el
    /// tick 140, giros anchos, ATRAVIESA PAREDES, 40 estelas a escala
    /// 1.4 y explosión radial al morir) en versión amistosa. El
    /// ARCOÍRIS COMPLETO de su ataque — cada trazo nace con el
    /// siguiente matiz del ciclo, como la ráfaga de la pelea.
    ///
    /// EL DISPARO — la salida de la RÁFAGA de la jefa (estudio §8): la
    /// velocidad (0,−6) rotada ±90° al azar — el trazo sale ENFURECIDO
    /// hacia cualquier lado del hemisferio superior y el homing lo
    /// corrige. Un trazo por uso, majestuoso.
    ///
    /// EL DESBORDE — el plato fuerte de la opción C, con el patrón del
    /// Grimorio Sellado del usuario (v6.50.98): el 5º USO consecutivo
    /// lanza EL TRAZO MASIVO (daño ×2.5, escala 2.8, penetra 12,
    /// explosión radial TRIPLE y temblor de pantalla), el portador paga
    /// 5% de su vida máxima (nunca mata) y el libro entra en recarga de
    /// 120 ticks (no dispara).
    ///
    /// Sin maná (regla de la casa — arsenal de pruebas).
    /// </summary>
    public class VeredictoEmperatriz : ModItem
    {
        // ===== SISTEMA DE DESBORDE (estado de CLASE — el patrón del
        // Grimorio Sellado del usuario, v6.50.98) =====
        private static int disparos = 0;
        private static int cooldownDesborde = 0;

        // el anti-doble de la casa (mismo frame)
        private static uint _ultimoFrame = 0;

        public override void SetDefaults()
        {
            Item.damage = 130;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 14;
            Item.useAnimation = 14;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.knockBack = 3f;
            Item.value = Item.buyPrice(0, 10, 0, 0);
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item82;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<TrazoEmperatrizProjectile>();
            Item.shootSpeed = 17f;
            Item.mana = 0;                    // regla de la casa: sin maná
            Item.noMelee = true;
        }

        // ===== SISTEMA DE DESBORDE: en recarga el libro no cede =====
        public override bool CanUseItem(Player player)
        {
            if (cooldownDesborde > 0)
                return false;
            return true;
        }

        public override void UpdateInventory(Player player)
        {
            if (cooldownDesborde > 0)
                cooldownDesborde--;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // el anti-doble de la casa
            uint frame = Main.GameUpdateCount;
            if (frame == _ultimoFrame) return false;
            _ultimoFrame = frame;

            // === EL DESBORDE (el patrón del Grimorio Sellado): cada
            // USO es una unidad de presión; el 5º detona ===
            disparos++;
            bool desborde = false;
            if (disparos >= 5)
            {
                desborde = true;
                disparos = 0;
                cooldownDesborde = 120;   // 2 segundos de recarga

                // El desborde cobra: 5% de la vida MÁX — pero NUNCA
                // mata (el clamp del usuario, v6.50.98)
                int precio = player.statLifeMax2 / 20;
                player.statLife = System.Math.Max(1, player.statLife - precio);
                Terraria.CombatText.NewText(player.getRect(),
                    new Color(255, 90, 90), "-" + precio, dramatic: true);

                // el juice del Grimorio Sellado: 30 llamas doradas
                for (int i = 0; i < 30; i++)
                {
                    Dust dust = Dust.NewDustPerfect(player.Center, DustID.GoldFlame,
                        Vector2.UnitX.RotateRandom(MathHelper.TwoPi) * 8f,
                        220, new Color(255, 218, 94), 1.6f);
                    dust.noGravity = true;
                }

                if (Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item88, player.Center);
                    if (player.whoAmI == Main.myPlayer)
                        Main.NewText(Language.GetTextValue("Mods.AethonMod.Desborde.Aviso"),
                            new Color(255, 200, 0));
                }
            }

            // === LA SALIDA DE LA RÁFAGA DE LA JEFA (NPC.cs AI_120): la
            // velocidad (0,−6) rotada ±90° al azar ===
            Vector2 v = new Vector2(0f, -6f).RotatedByRandom(MathHelper.PiOver2);

            Vector2 pos = player.MountedCenter + new Vector2(player.direction * 15f, player.gravDir * 3f);

            // === EL MATIZ: cada trazo nace con el siguiente color del
            // ciclo (el arcoíris de la ráfaga de la jefa) ===
            float hue = (Main.GameUpdateCount % 300f) / 300f;

            // el daño del desborde: ×2.5 (el masivo de verdad)
            int danio = desborde ? (int)(damage * 2.5f) : damage;

            Projectile.NewProjectile(source, pos, v,
                ModContent.ProjectileType<TrazoEmperatrizProjectile>(),
                danio, knockback, player.whoAmI,
                -1f,                   // ai[0]: sin objetivo aún
                hue,                   // ai[1]: el matiz del arcoíris
                desborde ? 1f : 0f);   // ai[2]: la marca del MASIVO

            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
