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
    /// TajosAstralesStaff — v6.40 — EL BASTÓN DE LOS TAJOS ASTRALES.
    ///
    /// LA PETICIÓN LITERAL DEL USUARIO: "crea un bastón que dé tajos
    /// iguales [a los animes]: cuando en los animes cortan algo con una
    /// katana y luego salen cortes brillantes en todas direcciones que en
    /// realidad son líneas curvas de color blancas que aparecen y
    /// desaparecen rápidamente con un crecimiento de izquierda a derecha
    /// y estas aparecen después de efectuado el corte".
    ///
    /// La investigación v6.40 (research/tajos_v640/INFORME_TAJOS.md) le
    /// puso nombre a cada pieza: las LÍNEAS DE ESPADA (los crescentes
    /// blancos) y la CAUSALIDAD RETRASADA ("el golpe ya terminó; los
    /// efectos decidieron esperar" — el Delayed Causality del anime).
    ///
    /// EL RITUAL DEL ARMA:
    ///   · EL DESEMBAINO: un hilo tenue vuela del bastón al punto marcado
    ///     (la katana saliendo — sin ruido).
    ///   · EL MARCAJE (10 ticks): el punto SUSURRA — la marca dorada
    ///     pulsando, la tensión del aire. El corte YA OCURRIÓ.
    ///   · EL FLORECER: SIETE MEDIAS LUNAS BLANCAS nacen en direcciones
    ///     desacopladas, escalonadas en olas de 3 ticks — cada una crece
    ///     DE PUNTA A PUNTA (el crecimiento direccional del anime), con
    ///     su campana de vida (nace, arde, se disuelve) y su POP de
    ///     aparición. El DAÑO cae cuando cada tajo APARECE: la herida
    ///     llega con el corte visible, no con el disparo.
    ///
    /// LA TÉCNICA (TAJOLIB — la librería nueva de la casa): arcos de
    /// cápsulas con PERFIL DE LENTE (grosor máximo al centro, fino en
    /// las puntas — la media luna), 3 capas (eco fantasma + halo que
    /// parpadea + núcleo blanco que NUNCA parpadea), frente de revelado
    /// ardiendo y puntas prendidas. Todo determinista (Hash01 por
    /// identity — cero Main.rand en el render).
    ///
    /// v6.40 — SIN MANA (regla del usuario: todos los bastones del mod
    /// son de prueba).
    /// </summary>
    public class TajosAstralesStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 46;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 24; Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<TajoAstralProjectile>();
            Item.shootSpeed = 15f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item12.WithPitchOffset(-0.25f);
            Item.value = 12000;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL DESTINO ES LA MIRA: el corte florecerá donde apunta el
            // cursor (ai[0..1] viajan con el proyectil — determinista MP).
            Vector2 destino = Main.MouseWorld;
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback,
                player.whoAmI, destino.X, destino.Y);

            // La vaina original no vuela (solo el hilo del desembaino).
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D",
                "[c/FFF6E0:Marca el punto donde apuntas — y diez ticks después EL CORTE FLORECE:]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/FFF2CC:siete medias lunas blancas en todas direcciones, creciendo de punta a punta]"));
            tooltips.Add(new TooltipLine(Mod, "D3",
                "[c/78788C:La herida llega cuando el tajo aparece (causalidad retrasada).\nCada arco corta una vez su banda curva · sin coste de maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
