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
    /// LagrimasSolMoribundoStaff — v6.31 — LAS LÁGRIMAS DEL SOL MORIBUNDO.
    ///
    /// Petición del usuario (research/v631, INFORME_STAR_TOMB_DRAGON_WORD.md
    /// ficha 3 + §6.3): un bastón que derrama 3 LÁGRIMAS de un sol
    /// moribundo — una ESCOPETA MÁGICA de gotas de metal fundido:
    ///
    ///   · EL ABANICO: 3 lágrimas por casteo con ángulos ligeramente
    ///     distintos desde el arma (±0.16 rad + jitter), naciendo a
    ///     22..38 px del jugador.
    ///   · LA ÓRBITA: cada lágrima gira 2,5 s SIN DAÑAR alrededor de su
    ///     punto de nacimiento (radio y ω PROPIOS viajan en ai[]) mientras
    ///     se enciende de chamuscado a oro.
    ///   · LA CACERÍA: tras la órbita persigue al enemigo más cercano —
    ///     curva suave → mordisco fijo — con hasta 18 golpes (i-frames de
    ///     5 ticks por objetivo) y quemadura cósmica de 7 s.
    ///   · EL FINAL: al desvanecerse EXPLOTA en área ×1.5 (radio 90).
    ///
    /// v6.28 — sin maná (regla del usuario: todos los bastones del mod son
    /// de prueba). Nada de otros mods: todo son nuestras librerías.
    /// </summary>
    public class LagrimasSolMoribundoStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 160;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 60; Item.useAnimation = 60;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<LagrimaSolarProjectile>();
            Item.shootSpeed = 12f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8.WithPitchOffset(0.35f);
            Item.value = 25000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // EL TITULAR del arma (el nombre vive arriba; esto es el subtítulo).
            tooltips.Add(new TooltipLine(Mod, "S",
                "[c/FFB340:LAS LÁGRIMAS DEL SOL MORIBUNDO]"));
            tooltips.Add(new TooltipLine(Mod, "S2",
                "[c/FFD98A:Derrama 3 lágrimas que orbitan 2,5 s encendiéndose de chamuscado a oro]"));
            tooltips.Add(new TooltipLine(Mod, "S3",
                "[c/FF7A4A:Luego cazan sin descanso: hasta 18 mordiscos con quemadura cósmica · estallido final ×1.5]"));
            tooltips.Add(new TooltipLine(Mod, "S4",
                "[c/78788C:Gotas de metal fundido que se estiran con la velocidad · sin maná]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA ESCOPETA MÁGICA: el abanico gira sobre la dirección de tiro.
            float angBase = velocity.LengthSquared() > 0.01f
                ? velocity.ToRotation()
                : (Main.MouseWorld - position).ToRotation();

            for (int i = 0; i < 3; i++)
            {
                // 3 ángulos ligeramente distintos: ±0.16 rad + jitter del tiro.
                float spread = (i - 1) * 0.16f + Main.rand.NextFloat(-0.06f, 0.06f);
                Vector2 dir = angBase.ToRotationVector2().RotatedBy(spread);

                // Nacen a 22..38 px del jugador (la ficha: fuera del cuerpo).
                Vector2 pos = position + dir * (22f + 8f * i);

                // CADA LÁGRIMA SU ÓRBITA: radio y ω (con signo) viajan en
                // ai[] → el mismo giro en servidor y clientes.
                float radio = 28f + 18f * Main.rand.NextFloat();
                float omega = (0.07f + 0.05f * Main.rand.NextFloat())
                    * (Main.rand.NextBool() ? 1f : -1f);

                Projectile.NewProjectile(source, pos, dir * 3f, type, damage, knockback,
                    player.whoAmI, radio, omega);
            }
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
