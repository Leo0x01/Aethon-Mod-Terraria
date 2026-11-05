using System;
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
    //  copiado tal cual): el Warbanner original es un ÍTEM ANIMADO —
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
    //     (PreDrawInInventory/World con el marco que Main.itemFrame
    //     trae — como el original, escala 0.62).
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

        /// <summary>EL MARCO VIVO del ítem (el que Main.itemFrame
        /// camina con la animación registrada — con tope de seguridad
        /// si la tira cambiara de largo).</summary>
        private static Rectangle MarcoVivo(int tipo, Texture2D tex, int altoMarco)
        {
            int marco = Main.itemFrame[tipo];
            int max = Math.Max(0, tex.Height / altoMarco - 1);
            if (marco < 0) marco = 0;
            else if (marco > max) marco = max;
            return new Rectangle(0, marco * altoMarco, tex.Width, altoMarco);
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            // EL ÍTEM EN LA MANO: el marco vivo, escala contenida (la
            // tira es alta — 0.62 para que quepa en el slot con su
            // fuego — el mismo ajuste del original).
            Texture2D tex = TextureAssets.Item[Type].Value;
            Rectangle marco = MarcoVivo(Type, tex, 78);
            float esc = scale * 0.62f;
            spriteBatch.Draw(tex, position - new Vector2(0f, 2f * scale),
                marco, drawColor, 0f, Utils.Size(marco) * 0.5f, esc,
                SpriteEffects.None, 0f);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            // EL ÍTEM EN EL SUELO: el marco vivo con la luz del mundo.
            Texture2D tex = TextureAssets.Item[Type].Value;
            Rectangle marco = MarcoVivo(Type, tex, 78);
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
