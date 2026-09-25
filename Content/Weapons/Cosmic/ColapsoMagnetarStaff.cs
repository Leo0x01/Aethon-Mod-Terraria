using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Cosmic;

namespace AethonMod.Content.Weapons.Cosmic
{
    /// <summary>
    /// ColapsoMagnetarStaff — v6.31 — "EL COLAPSO DEL MAGNETAR"
    /// (research/v631, INFORME_STAR_TOMB_DRAGON_WORD.md ficha 2 + §6.2).
    ///
    /// El freno magnético y el starquake de la forma 2, destilados a un
    /// arma INDEPENDIENTE: cada uso dispara una estrella magnetar que
    /// hace EL CICLO COMPLETO ella sola —
    ///
    ///   · NACE Y VIVE: vuela al cursor frenando en seco (v₀ =
    ///     distancia/7.375, ×0.885/tick — aterriza EXACTO) y gira con
    ///     DOS HAZES CORTOS opuestos.
    ///   · EL FRENO (55 ticks): el spin cae ×0.15, los haces ×0.55, la
    ///     estrella TIEMBLA, la jaula se enreda y un anillo telegráfico
    ///     se contrae mientras la CARGA sube 0→1.
    ///   · EL STARQUAKE: anillo expansivo de hasta 580 px que SOLO
    ///     golpea con el FRENTE — daño 1.4×..2.5× (308..550) y
    ///     Quemadura Cósmica 20 s.
    ///   · EL OVERCLOCK: la superviviente gira ×4.7 con haces ×1.55 y
    ///     daño ×1.6, y muere en un destello.
    ///
    /// Solo UNA estrella a la vez: el nuevo disparo se come a la previa.
    /// v6.28 — sin maná (regla de la casa: bastones de prueba). Nada de
    /// otros mods: todo son nuestras librerías.
    /// </summary>
    public class ColapsoMagnetarStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 220;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            // Un ciclo completo por click: SIN autoReuse — la estrella
            // merece su drama.
            Item.useTime = 30; Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<ColapsoMagnetarProjectile>();
            Item.shootSpeed = 15f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8.WithPitchOffset(-0.45f);
            Item.value = 25000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // EL TITULAR del arma (el nombre vive arriba; esto es el subtítulo).
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/8A2BE2:EL COLAPSO DEL MAGNETAR]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/E6B0FF:Dispara una estrella que frena hasta casi detenerse, tiembla y ESTALLA]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/FF9EC4:El STARQUAKE solo golpea con el FRENTE de su anillo: 308..550 según la carga]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:La superviviente entra en OVERCLOCK y arde ×1.6 · una estrella a la vez · sin maná]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL HANDBRAKE DE LA CASA: solo UNA estrella en campo — el nuevo
            // disparo devora a la previa (el ciclo nunca se solapa).
            MatarEstrellaPrevia(player);

            // EL PUNTO OBJETIVO: donde estaba el cursor al disparar. La
            // velocidad inicial resuelve la serie geométrica (0.885¹..0.885²⁶
            // = 7.375·v₀) para que la estrella ATERRICE EXACTO — con el
            // clamp, el objetivo se recalcula al aterrizaje real.
            Vector2 target = Main.MouseWorld;
            Vector2 delta = target - position;
            Vector2 v0 = delta / ColapsoMagnetarProjectile.TravelFactor;

            float speed = v0.Length();
            if (speed > 64f) v0 *= 64f / speed;
            else if (speed < 8f)
                v0 = speed > 0.001f ? v0 * (8f / speed) : velocity.SafeNormalize(Vector2.UnitX) * 8f;

            // El punto de aterrizaje EXACTO (tras el clamp).
            Vector2 aterrizaje = position + v0 * ColapsoMagnetarProjectile.TravelFactor;

            // La fase del eje magnético al nacer (el desalineado del barrido)
            // — viaja en ai[2]: la misma en servidor y clientes.
            float faseMagnetica = Main.rand.NextFloat(MathHelper.TwoPi);

            Projectile.NewProjectile(source, position, v0, type, damage, knockback,
                player.whoAmI, aterrizaje.X, aterrizaje.Y, faseMagnetica);
            return false;
        }

        /// <summary>EL TOPE DE 1: busca en Main.projectile la estrella del
        /// colapso de este owner y la mata — el ciclo nuevo empieza limpio.</summary>
        private static void MatarEstrellaPrevia(Player player)
        {
            int tipo = ModContent.ProjectileType<ColapsoMagnetarProjectile>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.type != tipo || p.owner != player.whoAmI)
                    continue;
                p.Kill();
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
