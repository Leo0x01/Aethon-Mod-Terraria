using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Weapons.Investigaciones
{
    // ======================================================================
    //  EL PRINCIPIO Y EL FIN — v6.124 — LA COPIA DE INVESTIGACIÓN de THE
    //  BEGINNING AND THE END de CALAMITY ENTROPY (la letra: «copia el
    //  arma the beginning and the end de CALAMITY ENTROPY y crea una
    //  version con su codigo, sprite y textura al 100% como
    //  investigacion, luego daselas al jugador»).
    //
    //  LA FICHA DEL ORIGINAL (el wiki oficial de Calamity Entropy —
    //  calentropy.miraheze.org):
    //    · TIPO: daga ARROJADIZA ratera, post-Señor de la Luna, crafteable
    //      en el Altar del Abismo (12× Colmillo de Dragón Fantasma +
    //      sus materiales).
    //    · NÚMEROS: 2500 daño · empuje 1 · tiempo de uso 18 · 4% crítico ·
    //      mejor modificador: Godly.
    //    · CLIC IZQUIERDO: la daga de ORO «El Principio» — aplica LA
    //      MARCA DEL PRINCIPIO (hasta 8 capas).
    //    · CLIC DERECHO: la daga AZUL «El Fin» — aplica LA MIRADA DEL FIN
    //      (hasta 8 capas).
    //    · LAS DAGAS: 100 penetración de armadura, NO atraviesan
    //      objetivos, atraviesan MUROS, y ambas aplican SUPRESIÓN VITAL
    //      («has visto tu muerte»).
    //    · EL CANJE DE ALMAS: al golpear a un impío que lleva UNA de las
    //      marcas, la daga DEL OTRO COLOR la LIBERA: la marca muere, la
    //      suya la reemplaza, y cada CAPA liberada nace un ALMA —
    //      la marca liberada suelta sus ALMAS DE LUZ INFERIOR (vuelan al
    //      jugador: curan y regalan SERVIDUMBRE) y la mirada liberada
    //      suelta sus ALMAS DE ECO (vuelan a los enemigos y disparan
    //      ondas de sonido que atraviesan TODO).
    //    · EL ATAQUE SIGILOSO: DOS dagas rastreadoras que al golpear
    //      REVIENTAN en una nube de ambas almas.
    //
    //  LAS ADAPTACIONES DE LA CASA (documentadas — Calamity Entropy es
    //  código CERRADO: esta copia reproduce la ficha COMPLETA del wiki
    //  oficial con las herramientas de la casa):
    //    · La clase RATERA de Calamity → DamageClass.Throwing.
    //    · El STEALTH de Calamity → el SIGILO RATERO de la casa (2,5 s
    //      quieto cargan el tiro sigiloso — SigiloRatero).
    //    · Las capas de los debuffs → InvestigacionesGlobalNPC (la
    //      memoria por NPC de la casa).
    //    · Las dagas y las almas se dibujan con los HAZES de la casa
    //      (HazLib — el mismo huso del tajo del vacío).
    // ======================================================================
    public class ElPrincipioYElFin : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 44;
            Item.damage = 2500;                      // la ficha: 2500
            Item.DamageType = DamageClass.Throwing;  // (ratero → arrojadiza)
            Item.useTime = 18;                       // la ficha: 18
            Item.useAnimation = 18;
            Item.knockBack = 1f;                     // la ficha: 1 (muy débil)
            Item.crit = 4;                           // la ficha: 4%
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;                // las dagas vuelan (las dibuja el proyectil)
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Investigaciones.DagaEterna>();
            Item.shootSpeed = 16f;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.buyPrice(platinum: 1); // (la ficha: vende 64 oro)
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL COLOR DEL USO: izquierda = EL PRINCIPIO (oro), derecha =
            // EL FIN (azul) — la ficha.
            bool esFin = player.altFunctionUse == 2;

            // EL SIGILO DEL LADRÓN: 2,5 s quieto cargan EL ATAQUE
            // SIGILOSO (las DOS dagas rastreadoras que revientan en
            // almas — la ficha).
            var sigilo = player.GetModPlayer<global::AethonMod.Content.Players.Investigaciones.SigiloRatero>();
            bool sigiloso = sigilo.Gastar();

            if (sigiloso)
            {
                // LAS DOS DAGAS GEMELAS rastreadoras (una de cada color).
                for (int i = 0; i < 2; i++)
                {
                    Vector2 vel = velocity.RotatedBy((i - 0.5f) * 0.22f);
                    Projectile.NewProjectile(source, position, vel, type,
                        damage, knockback, player.whoAmI, i == 1 ? 1f : 0f, 1f);
                }
                if (!Main.dedServ)
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item60.WithPitchOffset(-0.35f), player.Center);
            }
            else
            {
                Projectile.NewProjectile(source, position, velocity, type,
                    damage, knockback, player.whoAmI, esFin ? 1f : 0f, 0f);
            }
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            void Linea(string clave)
            {
                string texto = Language.GetTextValue("Mods.AethonMod.Items.ElPrincipioYElFin." + clave);
                if (string.IsNullOrEmpty(texto) || texto.StartsWith("Mods."))
                    return;
                tooltips.Add(new TooltipLine(Mod, clave, texto));
            }
            Linea("Titulo");
            Linea("Linea1");
            Linea("Linea2");
            Linea("Linea3");
            Linea("Linea4");
            Linea("Linea5");
            Linea("Receta");
        }

        public override void AddRecipes()
        {
            // EL PROTOCOLO DE LA CASA para las armas de investigación:
            // la madera de siempre (la vía de prueba). La receta
            // VERDADERA del original viaja en el tooltip.
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
