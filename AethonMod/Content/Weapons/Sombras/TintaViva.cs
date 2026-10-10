using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Sombras;

namespace AethonMod.Content.Weapons.Sombras
{
    /// <summary>
    /// TINTAVIVA — v6.50.95/.96 — EL ARMA DE LA GRIETA DE TINTA.
    /// La letra del usuario: «quiero que con este proyectil seas creativo,
    /// lo animes y crees una nueva arma con el proyectil» — la descarga
    /// prestada (el Nightglow vanilla 931 del Grimorio del Eterno) muere:
    /// el libro YA NO dispara luz de hadas ajena, escupe SU PROPIA tinta.
    ///
    /// v6.50.96 — LA GRIETA (GrietaDeTintaProjectile): el sprite EXACTO
    /// del usuario re-leído como lo que siempre fue — UNA GRIETA ABIERTA
    /// EN LA PÁGINA (no una lágrima: las grietas no lloran, ASPIRAN, MIRAN
    /// y DESGARRAN). Nace con su estrella de ruptura, viaja con estela
    /// ribbon del vacío + fantasmas + aspiración de motas + aberración
    /// cromática + glitch de tajas + el iris de apertura + EL OJO RASGADO
    /// que mira a su presa, y muere colapsando HACIA DENTRO con el corte
    /// que sana y LA MANCHA.
    ///
    /// EL ARMA: la pluma del propio grimorio — cada disparo es una herida
    /// que el libro abre en la realidad. Mismo origen que la familia:
    /// si el NERVIOSO está FUERA cazando, el disparo sale DEL LIBRO
    /// (OrigenDelDisparo — la letra .93 sigue mandando).
    /// </summary>
    public class TintaViva : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 90;
            Item.DamageType = DamageClass.Magic;
            Item.width = 44;
            Item.height = 44;
            Item.useTime = 24;
            Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;   // se APUNTA con la lágrima
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.mana = 9;
            Item.knockBack = 3f;
            Item.crit = 8;
            Item.shoot = ModContent.ProjectileType<GrietaDeTintaProjectile>();   // v6.50.96 — LA GRIETA DE TINTA
            Item.shootSpeed = 15.5f;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item122;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL ORIGEN — coherencia visual total con el libro: si el
            // NERVIOSO está FUERA cazando, la tinta sale DEL LIBRO flotante
            // (la letra .93: «mientras el libro este fuera todos los
            // ataques que pertenecen al libro salen del libro»)
            Vector2 origen = GrimorioHambrientoNervioso.PosicionDelLibro(player.whoAmI)
                is Vector2 libro ? libro : position;

            // LA PRIMERA PRESA: el jefe más cercano a cualquier distancia;
            // sin jefes, el enemigo más cercano en 1800 px (la chusma cena
            // tinta también) — la grieta corrige sola después (homing)
            NPC presa = SombraDeLaPagina.BuscarPresa(player.Center, true)
                ?? SombraDeLaPagina.BuscarPresa(player.Center, false);
            Vector2 rumbo = presa != null
                ? (presa.Center - origen).SafeNormalize(Vector2.UnitX)
                : (velocity.LengthSquared() > 0.1f
                    ? Vector2.Normalize(velocity) : Vector2.UnitX);

            Projectile.NewProjectile(source, origen, rumbo * Item.shootSpeed,
                type, damage, knockback, player.whoAmI,
                presa != null ? presa.whoAmI + 1 : 0f, 0f);
            return false;   // ya la spawneé yo: nada de doble vanilla
        }

        public override void AddRecipes()
        {
            // protocolo de pruebas v6.14.2: la madera, siempre la madera
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
