using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Aethon;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// EL GRIMORIO SELLADO — v6.50.98 — EL ARMA NUEVA DE PRUEBA.
    /// La letra del usuario: «te dare el codigo para un arma nueva de
    /// prueba, recuerda darcela al jugador, este sera el proyectil y su
    /// efecto» — el arma dispara EL FRAGMENTO DE AETHON y su efecto es
    /// EL SISTEMA DE DESBORDE + EL SISTEMA DE STACKS. Los ERRORES del
    /// código del usuario, todos muertos aquí:
    ///
    /// · (1) SINTAXIS: el disparo del desborde traía
    ///   «(int)(damage * 2.5f,» — un paréntesis sin cerrar que no
    ///   compilaba NADA del archivo.
    /// · (2) ItemID.Spellbook no existe: es ItemID.SpellTome (verificado
    ///   contra la tML 2026.8.3.0 — la receta del borrador apuntaba a la
    ///   nada).
    /// · (3) Tooltip.SetDefault / DisplayName: viven en el hjson (la
    ///   casa) — en esta tML son obsoletos y el -build los auto-genera.
    /// · (4) EL DAÑO AL JUGADOR: «daña 5% de vida» nunca MATA — clamp a
    ///   1 HP (el borrador podía matarte con la vida baja).
    /// · (5) Los CONTADORES son static: los campos de instancia de un
    ///   ModItem mueren con cada clonación del ítem (recogerlo, recargar
    ///   el mundo); el static de CLASE sobrevive — y el arma de prueba
    ///   es UNA copia por definición (viene en la Bolsa del Probador).
    /// </summary>
    public class GrimorioSellado : ModItem
    {
        // ===== SISTEMA DE DESBORDE (estado de CLASE — ver (5) arriba) =====
        private static int disparos = 0;
        private static int cooldownDesborde = 0;

        public override void SetStaticDefaults()
        {
            // (El DisplayName y el tooltip viven en el hjson — la casa.)
        }

        public override void SetDefaults()
        {
            Item.damage = 45;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 12;
            Item.width = 28;                  // == el sprite (28×30)
            Item.height = 30;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 4f;
            Item.value = Item.buyPrice(gold: 10);
            Item.rare = ItemRarityID.Cyan;
            Item.shoot = ModContent.ProjectileType<FragmentoAethon>();
            Item.shootSpeed = 14f;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item122;  // el 122 grave de la familia
        }

        // ===== SISTEMA DE DESBORDE =====
        public override bool CanUseItem(Player player)
        {
            // Si está en cooldown, el sello no cede: no puede disparar
            if (cooldownDesborde > 0)
                return false;
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            disparos++;

            // ===== DESBORDE al 5º disparo =====
            if (disparos >= 5)
            {
                disparos = 0;
                cooldownDesborde = 120;       // 2 segundos de cooldown

                // EL RAYO MASIVO (el desborde de Aethon) — aquí estaba el
                // ERROR (1): el paréntesis sin cerrar. Va lento (×0.5)
                // y pega al 250% con knockback 8.
                Projectile.NewProjectile(
                    source,
                    position,
                    velocity * 0.5f,
                    type,
                    (int)(damage * 2.5f),
                    8f,
                    player.whoAmI
                );

                // El desborde también daña al jugador: 5% de la vida MÁX
                // — pero NUNCA mata (ERROR (4): clamp a 1 HP)
                player.statLife = System.Math.Max(1,
                    player.statLife - player.statLifeMax2 / 20);

                // Efecto visual: Aethon desborda del propio jugador
                for (int i = 0; i < 30; i++)
                {
                    Dust dust = Dust.NewDustPerfect(
                        player.Center,
                        DustID.GoldFlame,    // GoldFlare NO existe — la casa
                        Vector2.UnitX.RotateRandom(MathHelper.TwoPi) * 8f,
                        220, new Color(255, 218, 94), 1.6f
                    );
                    dust.noGravity = true;
                }

                // Sonido + mensaje
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item88, player.position);
                Main.NewText("¡Aethon se desborda!", 255, 200, 0);
            }

            // EL DISPARO NORMAL — spawneado a mano para poder encender el
            // homing con ai[1] (0 = sin homing, 1 = con homing: la mejora
            // futura del arma solo cambia este número)
            Projectile proj = Projectile.NewProjectileDirect(
                source, position, velocity, type, damage, knockback, player.whoAmI
            );
            proj.ai[1] = 0;

            return false;                     // ya lo hicimos manual
        }

        public override void UpdateInventory(Player player)
        {
            // decrementar cooldown (mientras el libro está en el inventario)
            if (cooldownDesborde > 0)
                cooldownDesborde--;
        }

        public override void AddRecipes()
        {
            // LA RECETA DEL USUARIO (corregida: SpellTome, no Spellbook)
            CreateRecipe()
                .AddIngredient(ItemID.SpellTome, 1)
                .AddIngredient(ItemID.GoldBar, 5)
                .AddIngredient(ItemID.Amethyst, 20)
                .AddTile(TileID.Bookcases)
                .Register();

            // EL PROTOCOLO DE PRUEBAS (v6.14.2): la madera, siempre la madera
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 5)
                .Register();
        }
    }
}
