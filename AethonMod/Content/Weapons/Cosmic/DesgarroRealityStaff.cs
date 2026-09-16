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
    /// DesgarroRealityStaff — v6.31 — EL BASTÓN DEL DESGARRO EN LA REALIDAD.
    ///
    /// Petición del usuario: "un bastón que su proyectil sea un desgarro en la
    /// realidad... y dañar con eso". EL PROYECTIL NO ES UN OBJETO: ES LA HERIDA.
    /// Al soltar el casteo, la realidad se ABRE en una línea CONTINUA y PAREJA
    /// de 620 px que atraviesa paredes (un desgarro del ESPACIO no conoce la
    /// geometría):
    ///
    ///   · TELÉGRAFO (12 ticks): la estrella de ruptura crece y el anillo
    ///     implosiona — el mundo se oscurece.
    ///   · APERTURA (3 ticks): EL PRIMER GOLPE — la línea nace YA completa (UN
    ///     SOLO QUAD continuo — cero juntas, cero interrupciones).
    ///   · LA LÍNEA VIVA (~52 ticks): daño de apertura + mordidas suaves.
    ///   · VIBRACIÓN (16 ticks): LA TENSIÓN — onda estacionaria creciendo
    ///     0→3.5 px a ~10 Hz: la línea está a punto de FALLAR.
    ///   · EL CLÍMAX (2 ticks): la herida se PROFUNDIZA — flash + ancho ×1.35
    ///     + daño ×2.2. La línea SIGUE RECTA: nada se rompe en pedazos.
    ///   · EL CORTE VIVO (~1.6 s): la MISMA línea, más intensa, doliendo.
    ///     El cierre se COME el corte desde los extremos. NADA de proyectiles
    ///     extra — TODO ES LA LÍNEA.
    ///
    /// Todo el visual lo pinta RiftLib v3 (las RiftTaper* de MESETA — el
    /// desgarro continuo y parejo); el daño es la escuela A (línea). v6.28 —
    /// sin maná (regla del usuario: todos los bastones del mod son de prueba).
    /// </summary>
    public class DesgarroRealityStaff : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 250;
            Item.DamageType = DamageClass.Magic;
            Item.width = 28; Item.height = 30;
            Item.useTime = 50; Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RealityTearProjectile>();
            Item.shootSpeed = 2f;
            Item.mana = 0; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.UseSound = SoundID.Item8.WithPitchOffset(-0.2f);
            Item.value = 20000;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // EL TITULAR del arma (el nombre vive arriba; esto es el subtítulo).
            tooltips.Add(new TooltipLine(Mod, "D",
                "[c/8A2BE2:EL DESGARRO EN LA REALIDAD]"));
            tooltips.Add(new TooltipLine(Mod, "D2",
                "[c/E6B0FF:Abre una grieta CONTINUA de 620 px que ATRAVIESA PAREDES y corta a todo el que la toque]"));
            tooltips.Add(new TooltipLine(Mod, "D3",
                "[c/FF9EC4:Golpe de apertura · la línea VIBRA... y al PROFUNDIZARSE pega ×2.2 · el corte vivo sigue doliendo]"));
            tooltips.Add(new TooltipLine(Mod, "D4",
                "[c/78788C:El interior es un vacío con estrellas · labios violeta/carmesí · el mundo se apaga · sin maná]"));
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // El desgarro nace EN LA PUNTA del bastón, hacia donde apunta el
            // jugador — la velocidad solo lleva la DIRECCIÓN (la herida no viaja:
            // el espacio se abre donde lo rasgas).
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback,
                player.whoAmI, 0f, 0f, 0f);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
