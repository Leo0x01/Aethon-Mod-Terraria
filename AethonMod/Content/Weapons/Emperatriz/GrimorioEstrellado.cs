using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.Projectiles.Emperatriz;

namespace AethonMod.Content.Weapons.Emperatriz
{
    /// <summary>
    /// EL GRIMORIO ESTRELLADO — OPCIÓN A (la recomendada): EL CLON.
    ///
    /// El hermano evolucionado del Grimorio Sellado (v6.50.98 — el arma
    /// del usuario): el sello SE ROMPIÓ y la Emperatriz de la Luz prestó
    /// su streak. Dispara LA ESTELA DE AETHON: un ModProjectile propio
    /// que replica al 100% la IA del streak de la Emperatriz (la AI_171
    /// del Nightglow 931 — ver ESTUDIO_PROYECTIL_NIGHTGLOW.md §11) pero
    /// con la PALETA CÓSMICA del grimorio (oro/cian/magenta/índigo) y
    /// los ganchos del sistema de la casa.
    ///
    /// EL DISPARO — la cadencia EXACTA del Nightglow de verdad:
    ///   · useTime 2 + useAnimation 36, pero SOLO dispara durante los
    ///     primeros 8 ticks de la animación → 4 estelas por uso, una
    ///     cada 2 ticks (el burst ×4 del arma original),
    ///   · cada estela sale con la VELOCIDAD FUENTE de vanilla
    ///     (aleatoria, SIEMPRE hacia arriba — el homing la corrige),
    ///   · cada estela lleva un color distinto de la paleta cósmica.
    ///
    /// EL SISTEMA (el del Grimorio Sellado del usuario, v6.50.98):
    ///   · OBSERVADO — cada golpe marca a la presa (SistemaObservado);
    ///     a 5 marcas, crítico garantizado (+50%).
    ///   · DESBORDE — el 5º USO consecutivo detona: la primera estela
    ///     del uso sale MASIVA (daño ×2.5, escala ×2, anillo radial al
    ///     morir), el portador paga 5% de su vida máxima (nunca mata) y
    ///     el libro entra en recarga de 120 ticks (no dispara).
    ///
    /// Sin maná (regla de la casa — arsenal de pruebas).
    /// </summary>
    public class GrimorioEstrellado : ModItem
    {
        // ===== SISTEMA DE DESBORDE (estado de CLASE — el patrón del
        // Grimorio Sellado del usuario, v6.50.98) =====
        private static int disparos = 0;
        private static int cooldownDesborde = 0;

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
            Item.UseSound = SoundID.Item82;   // el del Nightglow de verdad
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<EstelaAethonProjectile>();
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
            // === LA VENTANA DEL BURST (el gate del Nightglow: solo los
            // primeros 8 ticks de la animación de 36 → 4 disparos) ===
            if (player.itemAnimation < player.itemAnimationMax - 8) return false;

            // el anti-doble de la casa
            uint frame = Main.GameUpdateCount;
            if (frame == _ultimoFrame) return false;
            _ultimoFrame = frame;

            // === EL ÍNDICE DE LA ESTELA dentro del burst (0..3) ===
            int orbe = (int)System.Math.Clamp(
                (player.itemAnimationMax - player.itemAnimation) / 2, 0, 3);

            // === EL DESBORDE se cuenta (y detona) en la PRIMERA estela
            // del uso: un USO = una unidad de presión (como el Sellado) ===
            bool desborde = false;
            if (orbe == 0)
            {
                disparos++;
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

                    // Aethon desborda del propio portador (el juice del
                    // Grimorio Sellado: 30 llamas doradas)
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
            }

            // === LA FUENTE: velocidad aleatoria SIEMPRE hacia arriba ===
            // (la fórmula literal del Nightglow)
            Vector2 v = Main.rand.NextVector2Circular(1f, 1f)
                      + Main.rand.NextVector2CircularEdge(3f, 3f);
            if (v.Y > 0f) v.Y *= -1f;

            Vector2 pos = player.MountedCenter + new Vector2(player.direction * 15f, player.gravDir * 3f);

            // === LA PALETA: cada estela del burst lleva un color cósmico
            // distinto, rotando entre usos ===
            float indicePaleta = (Main.GameUpdateCount / 2f) % 4f;

            // el daño del desborde: ×2.5 (la estela MASIVA de verdad)
            int danio = desborde ? (int)(damage * 2.5f) : damage;

            Projectile.NewProjectile(source, pos, v,
                ModContent.ProjectileType<EstelaAethonProjectile>(),
                danio, knockback, player.whoAmI,
                -1f,                   // ai[0]: sin objetivo aún
                indicePaleta,          // ai[1]: el color de la paleta
                desborde ? 1f : 0f);   // ai[2]: la marca del MASIVO

            return false;   // el spawn es todo nuestro
        }

        public override void AddRecipes()
        {
            // el protocolo v6.14.2 de la casa: receta de 5 madera como
            // vía alternativa (el kit de entrega es la vía principal)
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
