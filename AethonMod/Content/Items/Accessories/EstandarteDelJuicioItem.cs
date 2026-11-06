using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Players;

namespace AethonMod.Content.Items.Accessories
{
    // ======================================================================
    //  EstandarteDelJuicioItem — v6.98 → v6.100 — EL ESTANDARTE DEL
    //  JUICIO, FIEL AL ORIGINAL.
    //
    //  LA LETRA DEL USUARIO: «el estandarte del juicio no tiene nada
    //  que ver con el original y se ve peor, procedamos como siempre,
    //  primero copia exactamente la version original al proyecto como
    //  investigacion».
    //
    //  LA INVESTIGACIÓN (research/README-ESTANDARTE.md — el original
    //  copiado tal cual): el estandarte original es un ÍTEM ANIMADO —
    //  una tira de 5 marcos a 6 ticks (DrawAnimationVertical(6, 5)),
    //  de 32×78 px, con el pennant dorado ardiendo. LA v6.98 nuestra
    //  era un ítem ESTÁTICO de 30×40 — por eso «no tenía nada que
    //  ver». ESTA es la cura completa:
    //
    //   · EL SPRITE: tira 30×390 = 5 marcos de 30×78, animada a 6
    //     ticks EXACTOS — forjada por tools/estandarte_v6100.py con
    //     transparencia nativa (el mástil, el pennant afilado, el SOL
    //     DEL JUICIO pulsando y el fuego que lo envuelve).
    //   · EL INVENTARIO: el marco vivo se dibuja él mismo
    //     (PreDrawInInventory con el marco que tML YA trae en el
    //     parámetro frame — y PreDrawInWorld con el helper oficial
    //     Main.GetItemDrawFrame, el mismo del Grimorio Hambriento).
    //
    //     LA LECCIÓN v6.100.1 (el client.log del usuario: interfaz
    //     rota, cosas que desaparecen, pantalla negra): la v6.100
    //     leía Main.itemFrame[Type] — ese array es VANILLA (tML NUNCA
    //     lo redimensiona para mods; solo Main.itemAnimations se
    //     redimensiona). Con el Type del estandarte fuera de rango:
    //     IndexOutOfRangeException CADA FRAME en cada slot — la
    //     excepción silenciosa mataba las capas restantes de la
    //     interfaz. El marco de un ítem animado NUNCA se lee de
    //     Main.itemFrame: llega en el parámetro frame del hook o por
    //     Main.GetItemDrawFrame.
    //   · LA ESPALDA: EstandarteBannerDrawLayer cuelga la tela tras
    //     el portador (el original usaba AutoloadEquip Balloon —
    //     la casa, su propia capa).
    //   · LAS MECÁNICAS: 720 px de radio · la BENDICIÓN (daño ×1.30
    //     por cercanía) · el ARDOR con intensidad ×1→×3 · las CARGAS
    //     que estallan · la Llama del Alba — todo en
    //     EstandarteJuicioAuraProjectile.
    // ======================================================================
    public class EstandarteDelJuicioItem : ModItem
    {
        public override string Texture => "AethonMod/Content/Items/Accessories/EstandarteDelJuicioItem";

        public override void SetStaticDefaults()
        {
            // LA TIRA ANIMADA — como el original: 5 marcos a 6 ticks
            // (el pennant ondea, el sol late, el fuego respira).
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(6, 5, false));
            ItemID.Sets.AnimatesAsSoul[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 78;
            Item.accessory = true;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.buyPrice(0, 30, 0, 0);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            JuicioPlayer modPlayer = player.GetModPlayer<JuicioPlayer>();
            modPlayer.EstandarteJuicio = true;
            modPlayer.EstandarteJuicioOculto = hideVisual;
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            // EL ÍTEM EN LA MANO: el marco vivo LLEGA EN EL PARÁMETRO
            // frame — ItemSlot.Draw lo computa con Main.itemAnimations
            // (el sistema que tML SÍ redimensiona para mods; el viejo
            // Main.itemFrame es el array vanilla y leerlo con un Type
            // de mod revienta). Escala contenida: la tira es alta —
            // 0.62 para que quepa en el slot con su fuego — el mismo
            // ajuste del original.
            Texture2D tex = TextureAssets.Item[Type].Value;
            float esc = scale * 0.62f;
            spriteBatch.Draw(tex, position - new Vector2(0f, 2f * scale),
                frame, drawColor, 0f, Utils.Size(frame) * 0.5f, esc,
                SpriteEffects.None, 0f);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            // EL ÍTEM EN EL SUELO: EL HELPER OFICIAL DE VANILLA — textura
            // + marco de la animación registrada, SEGURO para mods (el
            // mismo del Grimorio Hambriento; nunca Main.itemFrame).
            Main.GetItemDrawFrame(Type, out Texture2D tex, out Rectangle marco);
            spriteBatch.Draw(tex, Item.position - Main.screenPosition,
                marco, lightColor, rotation, Utils.Size(marco) * 0.5f, scale,
                SpriteEffects.None, 0f);
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // EL PERGAMINO DEL JUICIO — las líneas de la v6.98 (las
            // claves viven en el hjson ×3).
            tooltips.Add(new TooltipLine(Mod, "EJTitulo",
                Language.GetTextValue("Mods.AethonMod.Items.EstandarteDelJuicioItem.Titulo")));
            tooltips.Add(new TooltipLine(Mod, "EJL1",
                Language.GetTextValue("Mods.AethonMod.Items.EstandarteDelJuicioItem.Linea1")));
            tooltips.Add(new TooltipLine(Mod, "EJL2",
                Language.GetTextValue("Mods.AethonMod.Items.EstandarteDelJuicioItem.Linea2")));
            tooltips.Add(new TooltipLine(Mod, "EJL3",
                Language.GetTextValue("Mods.AethonMod.Items.EstandarteDelJuicioItem.Linea3")));
            tooltips.Add(new TooltipLine(Mod, "EJL4",
                Language.GetTextValue("Mods.AethonMod.Items.EstandarteDelJuicioItem.Linea4")));
        }

        public override void AddRecipes()
        {
            // LA FORJA: fragmentos del sol (1225) y placas de la casa
            // (520) sobre el altar del luminato (412) — la receta de
            // la v6.98 intacta.
            CreateRecipe(1)
                .AddIngredient(1225, 20)      // Fragmento solar
                .AddIngredient(520, 20)       // Placa sagrada
                .AddIngredient(3458, 12)      // Éter luminoso
                .AddTile(412)                 // Altar del luminato
                .Register();
        }
    }
}
