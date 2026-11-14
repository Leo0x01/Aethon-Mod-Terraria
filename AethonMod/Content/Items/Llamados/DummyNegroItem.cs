using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.NPCs;

namespace AethonMod.Content.Items.Llamados
{
    // ======================================================================
    //  DummyNegroItem — v6.105 — EL MUÑECO DE PRUEBAS NEGRO (el ítem
    //  que lo coloca, como el Target Dummy de vanilla pero negro).
    //
    //  LA LETRA DEL USUARIO: «crea un nuevo dummy con el mismo sprite
    //  que los de terraria normal pero teñido de negro y que estos
    //  dummy negros si puedan recibir todos los ataques del
    //  grimorio».
    //
    //  EL ÍTEM: clic y el muñeco nace en el tile del cursor. El icono
    //  ES el sprite del muñeco de la casa (v6.136 — la letra:
    //  «Genera las texturas con SForja»: nació en la forja; el ítem lo
    //  toma prestado del NPC con el override de la casa).
    // ======================================================================
    public class DummyNegroItem : ModItem
    {
        /// <summary>EL SPRITE DE LA CASA — prestado del NPC por el
        /// override de la casa (el muñeco negro de la forja).</summary>
        public override string Texture => "AethonMod/Content/NPCs/DummyNegroNPC";

        public override void SetStaticDefaults()
        {
            // El DisplayName/Tooltip viven en el hjson (Items.DummyNegroItem).
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 30;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTurn = true;
            Item.consumable = true;
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.autoReuse = false;
        }

        public override bool CanUseItem(Player player)
        {
            // Solo donde haya SITIO (que no haya ya un NPC en el tile del
            // cursor — la cortesía del dummy vanilla).
            Vector2 pos = PosicionDeColocacion();
            Rectangle zona = new Rectangle((int)pos.X - 12, (int)pos.Y - 44, 24, 44);
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n != null && n.active && n.Hitbox.Intersects(zona))
                    return false;
            }
            return true;
        }

        public override bool? UseItem(Player player)
        {
            // LA COLOCACIÓN — el muñeco nace de pie sobre el tile del
            // cursor (la base en el suelo del tile, como el vanilla).
            Vector2 pos = PosicionDeColocacion();
            int quien = NPC.NewNPC(player.GetSource_FromThis(),
                (int)pos.X, (int)pos.Y, ModContent.NPCType<DummyNegroNPC>());
            if (Main.npc[quien].active)
                Main.npc[quien].Bottom = pos;
            return true;   // consume el ítem
        }

        /// <summary>EL SITIO — el centro del tile del cursor, la base en
        /// su suelo.</summary>
        private static Vector2 PosicionDeColocacion()
        {
            int tx = (int)(Main.MouseWorld.X / 16f);
            int ty = (int)(Main.MouseWorld.Y / 16f);
            return new Vector2(tx * 16f + 8f, ty * 16f + 16f);
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            // EL ICONO — el sprite de la casa (el muñeco negro de la
            // forja), escalado al slot.
            Texture2D tex = TextureAssets.Item[Type].Value;
            if (tex == null || tex.IsDisposed)
                return true;
            var marco = new Rectangle(0, 0, tex.Width, tex.Height);

            float esc = scale * 28f / System.MathF.Max(marco.Width, marco.Height);
            spriteBatch.Draw(tex, position, marco, drawColor, 0f,
                marco.Size() * 0.5f, esc, SpriteEffects.None, 0f);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D tex = TextureAssets.Item[Type].Value;
            if (tex == null || tex.IsDisposed)
                return true;
            var marco = new Rectangle(0, 0, tex.Width, tex.Height);

            // EL ÍTEM EN EL SUELO — el muñeco negro tirado.
            spriteBatch.Draw(tex, Item.position - Main.screenPosition, marco,
                lightColor, rotation, marco.Size() * 0.5f, scale,
                SpriteEffects.None, 0f);
            return false;
        }

        public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
        {
            string tip = Language.GetTextValue("Mods.AethonMod.Items.DummyNegroItem.Tip");
            if (!string.IsNullOrEmpty(tip) && !tip.StartsWith("Mods."))
                tooltips.Add(new TooltipLine(Mod, "Tip", tip));
        }
    }
}
