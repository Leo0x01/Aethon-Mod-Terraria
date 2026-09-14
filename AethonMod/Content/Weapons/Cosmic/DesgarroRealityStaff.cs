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
    /// DesgarroRealityStaff — v6.26 — EL BASTÓN DEL DESGARRO EN LA REALIDAD.
    ///
    /// Petición del usuario: "un bastón que su proyectil sea un desgarro en la
    /// realidad... y dañar con eso". EL PROYECTIL NO ES UN OBJETO: ES LA HERIDA.
    /// Al soltar el casteo, la realidad se ABRE en una línea de 620 px que
    /// atraviesa paredes (un desgarro del ESPACIO no conoce la geometría):
    ///
    ///   · TELÉGRAFO (10 ticks): la estrella de ruptura crece en la punta del
    ///     bastón y el anillo implosiona — el mundo se oscurece.
    ///   · APERTURA (4 ticks): EL GOLPE — Kick perpendicular, Flash, chispas
    ///     de anomalía y el corte se abre de golpe (0.333/tick — lección élite).
    ///   · SOSTENIDO (90 ticks): la LÍNEA VIVA daña (golpe de apertura + DoT
    ///     cada 2 ticks) con labios violeta/carmesí, aberración R/B, estrellas
    ///     fluyendo dentro del vacío oclusivo y ecos glitch.
    ///   · CIERRE (8 ticks): los labios se cierran, caen shards de vidrio — y
    ///     en el mismo punto queda LA GRIETA PERSISTENTE (~8 s) respirando y
    ///     doliendo en área.
    ///
    /// Todo el visual lo pinta RiftLib (la librería nueva de la casa); el daño
    /// es la escuela A (línea) del contrato. v6.26 — sin maná (regla del
    /// usuario: todos los bastones del mod son de prueba).
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
                "[c/E6B0FF:Abre una grieta de 620 px que ATRAVIESA PAREDES y corta a todo el que la toque]"));
            tooltips.Add(new TooltipLine(Mod, "D3",
                "[c/FF9EC4:Golpe de apertura + desgarro sostenido cada 2 ticks · deja una grieta persistente ~8 s]"));
            tooltips.Add(new TooltipLine(Mod, "D4",
                "[c/78788C:El interior es un vacío con estrellas · labios violeta/carmesí · eco glitch · sin maná]"));
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
