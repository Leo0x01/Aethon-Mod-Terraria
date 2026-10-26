// ============================================================================
// PUERTO DE INVESTIGACIÓN — Calamity: Hunt of the Old God
// (github.com/TohruKobayashi/CalamityHunt) · Team Hypnos:
// caligulasAquarium, IbanPlay, THEREDSTONEBRO, AmmyanDocCiesh, Blockaroz
// Licencia original: GPL-3.0 (ver LICENSE-GPL3.txt en esta carpeta).
// Copiado verbatim por petición del usuario para investigar al jefe
// GOOZMA; pendiente su decisión de re-licencia/modificación.
// ============================================================================

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent.Bestiary;
using Terraria.UI;

namespace AethonMod.Content.GoozmaPort.Common.Graphics
{
    public class SlimeMonsoonPortraitBackground : IBestiaryInfoElement, IBestiaryBackgroundImagePathAndColorProvider
    {
        public Asset<Texture2D> GetBackgroundImage() => AssetDirectory.Textures.SlimeMonsoon.MapBG;
        public Color? GetBackgroundColor() => Color.Purple;
        public UIElement ProvideUIElement(BestiaryUICollectionInfo info) => null;
    }
}
