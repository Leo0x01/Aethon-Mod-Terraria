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
    /// CompasRunico — v6.82 — EL COMPÁS RÚNICO (clicker de la casa nº1).
    ///
    /// LA LETRA: «crea un arma nueva o varias con lo que creas correcto
    /// de la clase clicker, dale los efectos que sean necesarios».
    ///
    /// LA CLASE CLICKER DE LA CASA (la investigación del Clicker Class
    /// real): EL CLIC ES EL USO — cada clic manual (autoReuse OFF) deja
    /// caer el golpe DONDE apuntas, dentro del RADIO DE CONTRATO dibujado
    /// alrededor del jugador (300px, cian arcano, con 8 runas orbitando
    /// el aro — ClickerSistema lo dibuja con la librería de anillos
    /// rúnicos de la casa).
    ///
    /// EL COMBO (el ritmo con propósito): cada 8 clics LA CONSTELACIÓN
    /// detona — cinco estrellas vecinas apuntan al cursor y SUS CINCO
    /// RAYOS de StormLib convergen en el punto (daño ×2, hitbox ancha,
    /// flash cian) — el cielo entero responde a la cadencia.
    ///
    /// EL FEEDBACK por clic: el PING del anillo (la firma del Clicker
    /// Class: chispas radiales en círculo) + el sonido seco del Menú
    /// (MenuTick, LA voz del clic).
    /// </summary>
    public class CompasRunico : ModItem
    {
        /// <summary>El radio del contrato (px alrededor del jugador).</summary>
        private const float Radio = 300f;

        /// <summary>Cada cuántos clics detona LA CONSTELACIÓN.</summary>
        private const int CadaN = 8;

        public override void SetStaticDefaults()
        {
            // EL REGISTRO EN LA CLASE (el contrato dibujado por ClickerSistema).
            ClickerCasa.Registrar(Type, Radio, new Color(120, 220, 255), EstiloClicker.Runico);
        }

        public override void SetDefaults()
        {
            Item.damage = 85;
            Item.DamageType = DamageClass.Generic;
            Item.width = 28; Item.height = 30;
            // EL CLIC ES EL USO: corto y MANUAL (autoReuse OFF — mantener
            // apretado no dispara: cada clic cuenta — la esencia de la clase).
            Item.useTime = 7; Item.useAnimation = 7;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<ClickerGolpeProjectile>();
            Item.shootSpeed = 0f;
            Item.mana = 4; Item.noMelee = true;
            Item.rare = ItemRarityID.Quest;
            // LA VOZ DEL CLIC (la firma sonora del Clicker Class).
            Item.UseSound = SoundID.MenuTick;
        }

        public override bool CanUseItem(Player player)
        {
            // EL CONTRATO DEL RADIO: solo se puede clickear DENTRO del
            // círculo (la regla visible — el aro brilla cuando el cursor
            // está dentro; fuera, el arma calla).
            return Vector2.DistanceSquared(Main.MouseWorld, player.Center) <= Radio * Radio;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL CONTEO DEL COMBO (vive en el ModPlayer; cambiar de arma
            // lo reinicia) — y el golpe nace DONDE apuntas.
            int n = player.GetModPlayer<ClickerJugador>().RegistraClic(Type);
            bool constelacion = n % CadaN == 0;

            Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero, type,
                constelacion ? damage * 2 : damage, knockback, player.whoAmI,
                constelacion ? (float)ClickerGolpeProjectile.EstiloConstelacion
                             : (float)ClickerGolpeProjectile.EstiloClic,
                Main.rand.Next(1000));

            // LA CONSTELACIÓN TIENE SU VOZ (el clic normal ya sonó con el
            // MenuTick del ítem — el especial estrena la suya).
            if (constelacion)
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item4.WithPitchOffset(-0.15f), Main.MouseWorld);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "D", "[c/78DCFF:Clicka dentro del círculo rúnico — cada clic cae donde apuntas]"));
            tooltips.Add(new TooltipLine(Mod, "D2", "[c/78DCFF:Cada 8 clics: LA CONSTELACIÓN — 5 rayos convergen en tu cursor (daño ×2)]"));
        }

        public override void AddRecipes() { CreateRecipe().AddIngredient(ItemID.Wood, 5).Register(); }
    }
}
