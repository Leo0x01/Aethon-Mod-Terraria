using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Definitivas;

namespace AethonMod.Content.Weapons.Definitivas
{
    // ======================================================================
    //  v6.55.0 — EL RAYO CONCENTRADO II (antes «la réplica de investigación»
    //  de la v6.54.8 — LA REFORMA de la letra: «no uses texturas originales
    //  del rayo de fusión y además cambia su nombre a rayo concentrado 2 o
    //  algo así y sobre todo el sonido no puedes usarlo debes recrearlo»).
    //
    //  TODO es de la casa ahora:
    //    · LAS TEXTURAS: las cuatro procedurales del plasma (CarnePlasma ·
    //      SegundoRuido · RampaGlow · BitsChispa — nacidas de los
    //      generadores de tools/, 100 % sintetizadas) y el SoftGlow de la
    //      boca. CERO material ajeno en el paquete.
    //    · EL SHADER: HazConcentradoShader.fx — la técnica re-escrita por
    //      la casa (las cuatro texturas multiplicadas con doble scroll
    //      cruzado, la deformación sinusoidal y las chispas en doble AND)
    //      con LAS DOS CURAS: la escala de ruido compensada (uEscalaV —
    //      los rasgos del plasma miden siempre lo mismo en pantalla,
    //      jamás dientes de sierra) y el núcleo suave (la vena blanca por
    //      smoothstep, no el umbral duro del contorno fractal).
    //    · EL SONIDO: RayoConcentradoLoop.ogg — la síntesis propia de la
    //      casa (el grave que late, el motor de armónicos, el plasma
    //      filtrado y el crujido de chispas — bucle periódico exacto).
    //
    //  LA CONDUCTA NO CAMBIA (la letra solo pedía el nombre y el origen):
    //  300 ticks de carga (la boca de glows apilados creciendo + la aguja
    //  que apunta) y 800 de láser — el cono de 3500 px con su barrido
    //  cosenoidal, su kick de cámara, su música callada al 30 %, su vuelo
    //  infinito y sus polvos cromáticos recorriendo el conducto.
    // ======================================================================
    public class RayoConcentradoDos : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 500;
            Item.DamageType = DamageClass.Generic;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RayoConcentradoDosProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 10;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item74.WithPitchOffset(-0.3f);
        }

        public override bool CanUseItem(Player player)
        {
            // v6.56.1 — UN solo haz a la vez, cruzando TODA la escalera
            // (niveles 1-3 + RC2 + Prisma): EscaleraDelRayo.HazLibre.
            return EscaleraDelRayo.HazLibre(player);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL DUEÑO viaja en ai[2] (el slot del portador — el arma lo
            // busca cada tick para colocar su boca).
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type,
                damage, knockback, player.whoAmI, 0f, 0f, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
