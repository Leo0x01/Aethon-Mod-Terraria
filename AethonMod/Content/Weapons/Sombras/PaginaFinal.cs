using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Sombras;

namespace AethonMod.Content.Weapons.Sombras
{
    /// <summary>
    /// PAGINAFINAL — v6.50.64 — ARMA 4: LA PÁGINA FINAL (la evolución AAA
    /// del arma 3, La Sombra de la Página — «mejorada mucho mucho mucho»).
    ///
    /// «La última página del grimorio: la que se escribe con cada jefe
    /// devorado.» El ritual en cuatro actos (la investigación AAA —
    /// Calamity/Stars Above + los 12 principios de la animación — hecha
    /// arma): LA APERTURA (el libro despliega su ojo sobre el portador y
    /// la sala se apaga) → LOS TRES VERSOS (tres zarpos convergen en el
    /// jefe desde ángulos distintos) → LA JAULA DE TINTA (el anillo de
    /// púas que se cierra a mordidas — el drain más fuerte de las cuatro:
    /// 4% cada 7 t) → EL FESTÍN (a 1 HP el libro GIGANTE se abre en el
    /// cielo, agarra al jefe con sus zarpos, el plano negro SUBE
    /// comiéndoselo y al final se sella con una onda de choque).
    ///
    /// 100% CÓDIGO como su madre el arma 3: ni un sprite.
    ///
    /// ARMA DE PRUEBA (no toca el grimorio): se entrega en LA BOLSA DE
    /// LAS SOMBRAS del kit de pruebas (la lección v6.14.2/.63 — cada arma
    /// nueva se registra en DOS sitios: aquí y en la bolsa); la receta de
    /// 5 madera queda como vía alternativa (protocolo v6.14.2).
    /// </summary>
    public class PaginaFinal : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 220;
            Item.DamageType = DamageClass.Generic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 58;
            Item.useAnimation = 58;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<PaginaFinalProjectile>();
            Item.shootSpeed = 14f;
            Item.mana = 0;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item74;
        }

        public override bool CanUseItem(Player player)
        {
            // UN solo ritual vivo por portador (el festín es un arte individual)
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    p.type == ModContent.ProjectileType<PaginaFinalProjectile>())
                    return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA PRESA: el jefe más cercano (a CUALQUIER distancia); sin
            // jefes, el enemigo más cercano en 1800 px (la chusma también cena)
            NPC presa = BuscarPresa(player.Center, true) ?? BuscarPresa(player.Center, false);
            Vector2 rumbo = presa != null
                ? (presa.Center - player.MountedCenter).SafeNormalize(velocity.LengthSquared() > 0.1f ? velocity : Vector2.UnitX)
                : velocity.LengthSquared() > 0.1f ? velocity.SafeNormalize(Vector2.UnitX) : Vector2.UnitX;

            Projectile.NewProjectile(source, player.MountedCenter, rumbo * 14f, type, damage, knockback,
                player.whoAmI, presa != null ? presa.whoAmI + 1 : 0, 0);
            return false;
        }

        /// <summary>El buscador de presas compartido por las tres armas.</summary>
        internal static NPC BuscarPresa(Vector2 desde, bool soloJefes)
        {
            NPC mejor = null;
            float mejorD = soloJefes ? float.MaxValue : 1800f * 1800f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.life <= 0 || n.dontTakeDamage) continue;
                if (n.friendly || n.townNPC) continue;
                if (soloJefes && !(n.boss || NPCID.Sets.ShouldBeCountedAsBoss[n.type])) continue;
                if (!soloJefes && (n.boss || NPCID.Sets.ShouldBeCountedAsBoss[n.type])) continue;
                float d = Vector2.DistanceSquared(n.Center, desde);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
