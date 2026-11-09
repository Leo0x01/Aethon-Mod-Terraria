using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Items.Llamados
{
    // ======================================================================
    //  MundoInteriorEternoItem — v6.115 → v6.119 — LA PUERTA AL MUNDO
    //  INTERIOR (Y LA PUERTA PORTÁTIL DE LA BOLSA).
    //
    //  LA LETRA DEL USUARIO: «para que el altar nos lleve a otro mundo
    //  que es el mundo crea un nuevo item que se fabrica con esencia
    //  del eterno en el altar, cuando introduces 1000 esencias se
    //  fabrica el item Mundo Interior del Eterno, al usar Mundo
    //  Interior del Eterno se hace una animacion donde el grimorio del
    //  eterno devora al jugador de la misma forma que devora a los
    //  jefes, pero sin hacerle daño, en el momento en que es devorado
    //  el mundo se llena de bruma solo en la animacion y el jugador es
    //  transportado el nuevo mundo, al aparecer en el nuevo mundo el
    //  mundo esta lleno de bruma y esta se disipa revelando como es
    //  este mundo nuevo».
    //
    //  v6.119 — LA LETRA NUEVA: «intenta que el item que lleva al mundo
    //  interior no sea consumible y se pueda seguir usando siempre» +
    //  «al consagrar 1000 esencias el altar se pueda ir al mundo
    //  interior». LA PUERTA NO SE GASTA (de jamás): consumible = false
    //  desde su nacimiento y AQUÍ BLINDADA — cada uso deja el ítem
    //  DONDE ESTABA (ni stack--, ni TurnToAir, ni consume en NINGÚN
    //  camino: el UseItem SOLO nace el proyectil y devuelve true), y
    //  el guard nuevo de CanUseItem espera a que la DEVORACIÓN viva
    //  termine para aceptar el siguiente uso: la puerta se puede usar
    //  SIEMPRE — al volver del interior, de nuevo y de nuevo. (Y la
    //  segunda puerta nació en el ALTAR: consagradas las MIL esencias
    //  en el altar, SU ventana traga igual que el ítem — ver
    //  AltarOfrendasUI y SubmundoGrimorio.EsenciasConsagradas.)
    //
    //  EL ARTE DE LA PUERTA: 1000× ESENCIA DEL ETERNO en el ALTAR DE LA
    //  CONSAGRACIÓN (el mismo número sagrado de la carta cósmica: las
    //  mil esencias que la espalda del jugador ya conoce). Al USARLO no
    //  se consume — una puerta no se gasta: nace LA DEVORACIÓN (el
    //  proyectil DevoracionEterna: el grimorio desciende, se abre,
    //  traga al portador SIN DAÑO, el mundo se llena de bruma y el
    //  interior lo recibe con su revelación de niebla).
    //
    //  (La puerta vieja — el grimorio en la mano sobre el altar — murió
    //  por esta letra: el camino AHORA es el ítem, el arte y el trago.)
    // ======================================================================
    public class MundoInteriorEternoItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.maxStack = 1;
            Item.consumable = false;          // una puerta no se gasta (v6.119: JAMÁS)
            Item.rare = ItemRarityID.Purple;  // el morado del vacío de la casa
            Item.value = Item.buyPrice(platinum: 1);
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item74;
        }

        public override bool CanUseItem(Player player)
        {
            // DENTRO ya no hay nada que devorar: el libro no se traga a
            // quien ya está en su barriga.
            if (SubmundoGrimorio.Activo || SubmundoGrimorio.Transicion)
                return false;
            // v6.119 — UN FESTÍN A LA VEZ: si UNA devoración ya vive para
            // este portador, el ítem espera — el libro que ya está
            // tragando no abre una segunda boca. El ítem NO se pierde ni
            // se gasta (el uso simplemente no dispara hasta que el
            // trago termina) y tras el viaje queda DONDE ESTABA: la
            // puerta se puede usar SIEMPRE, una y otra vez.
            int tipoFestin = ModContent.ProjectileType
                <Projectiles.Definitivas.DevoracionEterna>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile pr = Main.projectile[i];
                if (pr != null && pr.active && pr.type == tipoFestin &&
                    pr.owner == player.whoAmI)
                    return false;
            }
            return true;
        }

        public override bool? UseItem(Player player)
        {
            if (Main.myPlayer != player.whoAmI) return null;

            string razon;
            if (!SubmundoGrimorio.PuedeEntrar(player, out razon))
            {
                if (razon != null)
                    Main.NewText(razon, new Color(190, 160, 220));
                return true;
            }

            // LA DEVORACIÓN NACE: el grimorio baja, se abre y traga al
            // portador (sin daño — el trago lleva al interior, no a la
            // boca de la nada).
            Projectile.NewProjectile(player.GetSource_ItemUse(Item),
                player.Center, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.Definitivas.DevoracionEterna>(),
                0, 0f, player.whoAmI);
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // EL PERGAMINO DE LA PUERTA — las claves viven en el hjson
            // (×3 idiomas; sin clave, la línea no se dibuja).
            void Linea(string clave, string respaldo)
            {
                string texto = Language.GetTextValue("Mods.AethonMod.Items.MundoInteriorEternoItem." + clave);
                if (string.IsNullOrEmpty(texto) || texto.StartsWith("Mods."))
                    texto = respaldo;
                if (!string.IsNullOrEmpty(texto))
                    tooltips.Add(new TooltipLine(Mod, clave, texto));
            }
            Linea("Titulo", "[c/B96EEA:═══ EL MUNDO INTERIOR DEL ETERNO ═══]");
            Linea("Linea1", "");
            Linea("Linea2", "");
            Linea("Linea3", "");
            // v6.119 — LA LETRA: «que se pueda seguir usando siempre» —
            // la línea del contrato: la puerta NO se gasta.
            Linea("Linea4", "");
        }

        public override void AddRecipes()
        {
            // EL ARTE DE LA PUERTA (la letra): 1000 esencias del Eterno
            // entregadas al ALTAR DE LA CONSAGRACIÓN — las mil esencias
            // que el libro reconoce como UNA voluntad entera.
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<Altar.ItemCorrupto>(), 1000)
                .AddTile(ModContent.TileType<Tiles.AltarOfrendasTile>())
                .Register();
        }
    }
}
