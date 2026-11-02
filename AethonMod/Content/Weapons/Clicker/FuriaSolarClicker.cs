using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Clicker;

namespace AethonMod.Content.Weapons.Clicker
{
    /// <summary>
    /// FuriaSolarClicker — v6.82 — LA FURIA SOLAR (clicker de la casa nº2).
    ///
    /// LA LETRA: «crea un arma nueva o varias con lo que creas correcto
    /// de la clase clicker, dale los efectos que sean necesarios».
    ///
    /// LA CLASE CLICKER DE LA CASA (la investigación del Clicker Class
    /// real): EL CLIC ES EL USO — cada clic manual (autoReuse OFF) deja
    /// caer el golpe DONDE apuntas, dentro del RADIO DE CONTRATO dibujado
    /// alrededor del jugador (360px, oro solar, con 10 perlas cardinales
    /// latiendo en el aro — ClickerSistema las dibuja cada frame).
    ///
    /// EL COMBO (el ritmo con propósito): cada 12 clics LA NOVA detona —
    /// la FLOR DE FUEGO de PyraLib.Estallido sobre el cursor (la rampa
    /// solar blanca→oro→naranja→roja), el anillo de choque de OndaLib y
    /// el flash de pantalla con temblor suave (daño ×3, hitbox ANCHA) —
    /// el sol responde a la cadencia con un estallido entero.
    ///
    /// EL FEEDBACK por clic: el PING del anillo (chispas radiales en
    /// círculo, fuego RGB) + el sonido seco del Menú.
    /// </summary>
    public class FuriaSolarClicker : ModItem
    {
        /// <summary>El radio del contrato (px alrededor del jugador).</summary>
        private const float Radio = 360f;

        /// <summary>Cada cuántos clics detona LA NOVA.</summary>
        private const int CadaN = 12;

        public override void SetStaticDefaults()
        {
            // EL REGISTRO EN LA CLASE (el contrato con perlas solares).
            ClickerCasa.Registrar(Type, Radio, new Color(255, 170, 60), EstiloClicker.Solar);
        }

        public override void SetDefaults()
        {
            Item.damage = 120;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            // EL CLIC ES EL USO: manual y contundente (la furia pesa más
            // que el compás — un pelo más lenta, un pelo más fuerte).
            Item.useTime = 8; Item.useAnimation = 8;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<ClickerGolpeProjectile>();
            Item.shootSpeed = 0f;
            Item.mana = 5; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            // LA VOZ DEL CLIC (la firma sonora del Clicker Class).
            Item.UseSound = SoundID.MenuTick;
        }

        public override bool CanUseItem(Player player)
        {
            // EL CONTRATO DEL RADIO: solo dentro del círculo solar.
            return Vector2.DistanceSquared(Main.MouseWorld, player.Center) <= Radio * Radio;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL CONTEO DEL COMBO — y el golpe nace DONDE apuntas.
            int n = player.GetModPlayer<ClickerJugador>().RegistraClic(Type);
            bool nova = n % CadaN == 0;

            Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero, type,
                nova ? damage * 3 : damage, knockback, player.whoAmI,
                nova ? (float)ClickerGolpeProjectile.EstiloNova
                     : (float)ClickerGolpeProjectile.EstiloClic,
                Main.rand.Next(1000));

            // LA NOVA TIENE SU VOZ (el estruendo solar del combo).
            if (nova)
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item14.WithPitchOffset(0.2f), Main.MouseWorld);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D", "[c/FFAA3C:Clicka dentro del círculo solar — cada clic cae donde apuntas]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/FFAA3C:Cada 12 clics: LA NOVA — la flor de fuego estalla en tu cursor (daño ×3)]"));
        }

        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
