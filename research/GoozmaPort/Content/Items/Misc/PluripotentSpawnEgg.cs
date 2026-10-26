// ============================================================================
// PUERTO DE INVESTIGACIÓN — Calamity: Hunt of the Old God
// (github.com/TohruKobayashi/CalamityHunt) · Team Hypnos:
// caligulasAquarium, IbanPlay, THEREDSTONEBRO, AmmyanDocCiesh, Blockaroz
// Licencia original: GPL-3.0 (ver LICENSE-GPL3.txt en esta carpeta).
// Copiado verbatim por petición del usuario para investigar al jefe
// GOOZMA; pendiente su decisión de re-licencia/modificación.
// ============================================================================

using AethonMod.Content.GoozmaPort.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.GoozmaPort.Content.Items.Misc
{
    [LegacyName("SludgeFocus")]
    public class PluripotentSpawnEgg : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 48;
            Item.knockBack = 6;
            Item.value = 10000;
            Item.rare = ItemRarityID.Purple; // PORT NOTE: their custom rarity not ported
            if (ModLoader.HasMod(HUtils.CalamityMod)) {
                ModRarity r;
                Mod calamity = ModLoader.GetMod(HUtils.CalamityMod);
                calamity.TryFind<ModRarity>("BurnishedAuric", out r);
                Item.rare = r.Type;
            }
            Item.channel = true;

            // PORT NOTE (desviación documentada): el flujo original usa el
            // HUEVO sobre la ESTATUA SlimeNinjaStatue (tile no portado). Para
            // que el convocado de la bolsa FUNICIONE, el huevo ahora invoca
            // directamente al PluripotentSpawn — el MISMO NPC.NewNPCDirect
            // que hace la estatua (el huevo absorbe slimes y eclosiona en
            // GOOZMA exactamente igual).
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.autoReuse = false;
            Item.consumable = false;
        }

        public override bool? UseItem(Player player)
        {
            if (Main.myPlayer == player.whoAmI &&
                !NPC.AnyNPCs(ModContent.NPCType<Content.NPCs.PluripotentSpawn>()) &&
                !NPC.AnyNPCs(ModContent.NPCType<Content.NPCs.Bosses.GoozmaBoss.Goozma>()))
            {
                NPC.NewNPCDirect(player.GetSource_FromThis(),
                    player.Center - new Microsoft.Xna.Framework.Vector2(0f, 96f),
                    ModContent.NPCType<Content.NPCs.PluripotentSpawn>());
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.Center);
            }
            return true;
        }

        public override void AddRecipes()
        {
            // PORT NOTE (deviation): original recipe was 5 ChromaticMass +
            // 100 Gel + GelatinousCatalyst at the SlimeNinjaStatue tile —
            // those items/tiles belong to AethonMod.Content.GoozmaPort's wider content.
            CreateRecipe()
                .AddIngredient(ItemID.Gel, 100)
                .AddTile(TileID.Anvils)
                .Register();
        }

        public static Asset<Texture2D> glowTexture;

        public override void Load()
        {
            glowTexture = AssetUtilities.RequestImmediate<Texture2D>(Texture + "Glow");
        }

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            spriteBatch.Draw(glowTexture.Value, position, glowTexture.Value.Frame(), Color.White, 0, glowTexture.Value.Size() * 0.5f, scale, 0, 0);
            spriteBatch.Draw(glowTexture.Value, position, glowTexture.Value.Frame(), new Color(50, 50, 50, 0), 0, glowTexture.Value.Size() * 0.5f, scale, 0, 0);
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            spriteBatch.Draw(glowTexture.Value, Item.Center - Main.screenPosition, glowTexture.Value.Frame(), Color.White, rotation, glowTexture.Value.Size() * 0.5f, scale, 0, 0);
            spriteBatch.Draw(glowTexture.Value, Item.Center - Main.screenPosition, glowTexture.Value.Frame(), new Color(50, 50, 50, 0), rotation, glowTexture.Value.Size() * 0.5f, scale, 0, 0);
        }
    }
}
