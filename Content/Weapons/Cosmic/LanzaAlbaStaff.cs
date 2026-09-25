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
    /// LanzaAlbaStaff — v6.24 — LA LANZA DEL ALBA RÚNICA.
    ///
    /// LA TERCERA DE LAS TRES ARMAS DE LAS LIBRERÍAS (petición del
    /// usuario: "no olvides crear varias armas nuevas que usen todas
    /// nuestras librerías") — el alba es un filo de las tres:
    ///
    ///   · LUMENLIB (la hoja)  → LA LANZA: la hoja de luz (Lance) con su
    ///     estela de fantasmas (LanceTrail) + el destello de la punta +
    ///     el rayo de sol que la precede (el alba abriendo el camino).
    ///   · BRUMAFX (la niebla) → la VOLUTA de bruma que arrastra al volar
    ///     (Tendril sobre su propia estela) — el rocío del amanecer.
    ///   · STORMLIB (el trueno) → al golpear, CADENAS eléctricas saltan
    ///     de la lanza a los enemigos cercanos.
    ///   · LAS RUNAS           → un glifo rúnico ardiendo en el corazón
    ///     de la hoja (la firma del amanecer).
    ///
    /// Dispara la LANZA DEL ALBA: rápida, perforante (4 enemigos), con
    /// cadena eléctrica en cada golpe.
    ///
    /// v6.24 — SIN MANA (regla del usuario: todos los bastones del mod
    /// son de prueba).
    /// </summary>
    public class LanzaAlbaStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 90;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 16; Item.useAnimation = 16;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<LanzaAlbaProjectile>();
            Item.shootSpeed = 23f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item12.WithPitchOffset(0.4f);
            Item.value = 12000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D",
                "[c/FFE8B0:Dispara la Lanza del Alba: hoja de luz + bruma + cadena eléctrica]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/78788C:Perfora 4 enemigos · cada golpe encadena rayos a 2 cercanos · sin maná]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
