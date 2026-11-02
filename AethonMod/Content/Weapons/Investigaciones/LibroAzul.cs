using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Players;
using AethonMod.Content.Projectiles.Investigaciones;

namespace AethonMod.Content.Weapons.Investigaciones
{
    // ======================================================================
    //  v6.88 — EL LIBRO AZUL: EL ARMA DEL ÁRBOL DE HABILIDADES.
    //
    //  LA LETRA: «el arbol de habilidades que no sea para el grimorio
    //  del eterno, crea un arma nueva para que use el arbol de
    //  habilidades, esta nueva arma no tiene nada que ver con el
    //  grimorio, asi que crea el arma libro azul y enlazalo al arbol
    //  de habilidades como prueba».
    //
    //  EL DISEÑO (la prueba completa):
    //   · CLIC DERECHO abre EL ÁRBOL DE HABILIDADES (la ventana
    //     funcional de siempre — ArbolHabilidadesUI.Alternar()).
    //   · CLIC IZQUIERDO dispara LA DESCARGA DEL LIBRO: un perno azul
    //     que crece con el nivel del arma (daño 12 + nivel·2 — el Libro
    //     Azul sube con las bajas de su portador: 8 por nivel, tope
    //     100; el árbol cobra sus puntos del nivel del ARMA: 3 +
    //     nivel/5).
    //   · MANÁ 0 (la regla de pruebas de la casa: las armas de
    //     investigación no cobran maná).
    //   · CERO VÍNCULO CON EL GRIMORIO: nada del despertar, nada del
    //     Eterno — el árbol ES del Libro Azul (SkillTreePlayer).
    // ======================================================================
    public class LibroAzul : ModItem
    {
        public override string Texture => "AethonMod/Content/Weapons/Investigaciones/LibroAzul";

        public override void SetDefaults()
        {
            // EL SPRITE de la casa (30×30, tools/gen_libro_azul_v688.py).
            Item.width = 30;
            Item.height = 30;
            Item.maxStack = 1;
            Item.damage = 12;                    // la base (Shoot escala con el nivel)
            Item.DamageType = DamageClass.Magic;
            Item.knockBack = 2.5f;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.mana = 0;                       // la regla de pruebas: maná CERO
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<LibroAzulDescarga>();
            Item.shootSpeed = 13f;
            Item.UseSound = SoundID.Item9.WithPitchOffset(0.35f);
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
        }

        // EL CLIC DERECHO ABRE EL ÁRBOL (el patrón del Códice, la
        // llave que se muda de dueño).
        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                // LA PUERTA DEL ÁRBOL: solo con el arma EN LA MANO (la
                // letra: el arma usa el árbol — la llave es el arma).
                UI.ArbolHabilidadesUI.Alternar();
                Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuOpen.WithPitchOffset(0.2f));
                return false;
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA DESCARGA ESCALA CON EL NIVEL DEL ARMA (12 + nivel·2):
            // el Libro Azul pega más cuanto más lo usas.
            var st = player.GetModPlayer<SkillTreePlayer>();
            int nivel = st?.NivelLibroAzul ?? 1;
            int dano = 12 + nivel * 2;
            Projectile.NewProjectile(source, position, velocity, type,
                dano, knockback, player.whoAmI, nivel);
            return false;   // el perno nace AQUÍ (con su nivel dentro)
        }

        // LA REFORJA PROHIBIDA (la regla de las armas de Investigaciones:
        // sin prefijos — el nivel del arma es SU reforge).
        public override bool CanReforge() => false;

        public override void AddRecipes()
        {
            // LA VÍA ALTERNATIVA de la casa (la entrega principal es la
            // bolsa: solo lo que falte).
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
