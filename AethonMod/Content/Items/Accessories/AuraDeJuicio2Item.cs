using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles;

namespace AethonMod.Content.Items.Accessories
{
    // ======================================================================
    //  AuraDeJuicio2Item — v6.105 — EL AURA DE JUICIO 2 (LA COPIA
    //  SILENCIOSA: el efecto del ESTANDARTE, nada sobre el jugador).
    //
    //  LA LETRA DEL USUARIO: «las particulas de aura del juicio estan
    //  muy grandes, deben ser mas pequeñas y su efecto debe ser el
    //  mismo que el de estandarte del juicio, ademas sobre el jugador
    //  no necesita haber nada, el efecto solo se ve al acercarce los
    //  enemigos con particulas mas pequeñas y con los colores de la
    //  casa, ya sabes principalmente morado y negro, ademas tambien
    //  agrega particulas de bruma en la misma direccion e intensidad
    //  variable como las particulas iniciales y sobre los enemigos
    //  agrega un efecto que sea una textura de perlin circular y que
    //  solo sea un cemirculo con un difuminado y transparencia en
    //  forma de cirulo en su parte plana estos cambios hazlo en una
    //  nueva copia del aura del juicio, que sea aura del juicio 2».
    //
    //  LA COPIA SILENCIOSA — lo que HEREDA del Aura de Juicio y lo que
    //  cambia:
    //   · EL EFECTO ES EL DEL ESTANDARTE DEL JUICIO (la letra): el
    //     campo de 300 px marca a cada impío con LA QUEMADURA DEL
    //     VACÍO EXTERIOR (el debuff de llamas negras — el dot, el
    //     pago del buff al portador y las llamas viven en el buff),
    //     con LA INTENSIDAD ×1→×3 por cercanía + LA RAMPA del tiempo
    //     ardiendo y LA LENTITUD de los que se acercan (×0.55 pegado
    //     — vive en VacioGlobalNPC, generalizado para este aura).
    //     NADA del bono de daño del warbanner: el efecto es el del
    //     estandarte, punto.
    //   · SOBRE EL JUGADOR NO HAY NADA (la letra): ni las cinco capas
    //     de bloom, ni las luciérnagas, ni el medidor, ni el
    //     estandarte a la espalda — el ancla (AuraJuicio2Ancla) es
    //     INVISIBLE y lo único que hace es marcar el campo y vestir a
    //     los impíos. El aura SOLO se ve cuando los enemigos se
    //     acercan.
    //   · SOBRE LOS ENEMIGOS (cuando entran al campo):
    //      — LAS PARTÍCULAS PEQUEÑAS de la casa (morado y negro):
    //        chispas menudas del polvo del vacío invertido (núcleo
    //        negro + halo violeta), naciendo en el flanco que MIRA al
    //        portador y EXPULSADAS alejándose — la misma dirección de
    //        las partículas originales del aura de juicio, en pequeño.
    //      — LAS PARTÍCULAS DE BRUMA en la MISMA dirección e
    //        intensidad variable (puffs de humo fBm derivando hacia
    //        fuera, más densos cuanto más cerca arde el impío).
    //      — EL SEMICÍRCULO PERLIN (PerlinSemicirculo.png — la textura
    //        de perlin circular que SOLO es un semicírculo con el
    //        difuminado y transparencia en forma de círculo en su
    //        parte plana): la media cúpula morada y negra que ARDE
    //        sobre cada impío, inclinada hacia el portador, con su
    //        pase alfa (el negro-violeta que OSCURECE — se lee de
    //        día) y su pase aditivo (la energía violeta).
    // ======================================================================
    public class AuraDeJuicio2Item : ModItem
    {
        public override string Texture => "AethonMod/Content/Items/Accessories/AuraDeJuicio2Item";

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;
            Item.rare = ItemRarityID.Purple;   // el aura morada de la casa
            Item.value = Item.buyPrice(0, 40, 0, 0);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // ==========================================================
            //  EL ANCLA INVISIBLE — la única pieza viva del aura: marca
            //  el campo (el efecto del estandarte) y viste a los impíos
            //  (las partículas pequeñas, la bruma y el semicírculo).
            //  NADA se dibuja sobre el portador — ni con hideVisual
            //  cambia nada (aquí no HAY vestido del jugador que
            //  silenciar: el teatro vive en los enemigos).
            // ==========================================================
            int tipoAncla = ModContent.ProjectileType<AuraJuicio2Ancla>();
            if (player.ownedProjectileCounts[tipoAncla] < 1 &&
                !player.dead && !Main.gameMenu)
            {
                Projectile.NewProjectile(player.GetSource_FromThis(),
                    player.MountedCenter, Vector2.Zero, tipoAncla, 0, 0f,
                    player.whoAmI);
            }
        }

        /// <summary>¿El jugador lleva EL AURA DE JUICIO 2 puesta? (los
        /// slots 3..10 de la armadura — la copia del inventario NO
        /// cuenta: hay que PORTARLA).</summary>
        public static bool LlevaElAura(Player player)
        {
            if (player == null || !player.active)
                return false;
            int tipo = ModContent.ItemType<AuraDeJuicio2Item>();
            for (int i = 3; i <= 10 && i < player.armor.Length; i++)
            {
                Item a = player.armor[i];
                if (a != null && !a.IsAir && a.type == tipo)
                    return true;
            }
            return false;
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            // EL SELLO — el icono cuadrado de la casa (30×30), tal cual.
            Texture2D tex = TextureAssets.Item[Type].Value;
            spriteBatch.Draw(tex, position, frame, drawColor, 0f,
                Utils.Size(frame) * 0.5f, scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Main.GetItemDrawFrame(Type, out Texture2D tex, out Rectangle marco);
            spriteBatch.Draw(tex, Item.position - Main.screenPosition,
                marco, lightColor, rotation, Utils.Size(marco) * 0.5f, scale,
                SpriteEffects.None, 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // EL PERGAMINO DEL JUICIO 2 — las claves viven en el hjson
            // (×3 idiomas; sin clave, la línea no se dibuja).
            void Linea(string clave, string respaldo)
            {
                string texto = Language.GetTextValue("Mods.AethonMod.Items.AuraDeJuicio2Item." + clave);
                if (string.IsNullOrEmpty(texto) || texto.StartsWith("Mods."))
                    texto = respaldo;
                if (!string.IsNullOrEmpty(texto))
                    tooltips.Add(new TooltipLine(Mod, clave, texto));
            }
            Linea("Titulo", "[c/B96EEA:═══ EL AURA DE JUICIO II ═══]");
            Linea("Linea1", "");
            Linea("Linea2", "");
            Linea("Linea3", "");
            Linea("Linea4", "");
        }
    }
}
