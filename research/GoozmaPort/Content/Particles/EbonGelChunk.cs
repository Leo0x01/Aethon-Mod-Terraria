// ============================================================================
// PUERTO DE INVESTIGACIÓN — Calamity: Hunt of the Old God
// (github.com/TohruKobayashi/CalamityHunt) · Team Hypnos:
// caligulasAquarium, IbanPlay, THEREDSTONEBRO, AmmyanDocCiesh, Blockaroz
// Licencia original: GPL-3.0 (ver LICENSE-GPL3.txt en esta carpeta).
// Copiado verbatim por petición del usuario para investigar al jefe
// GOOZMA; pendiente su decisión de re-licencia/modificación.
// ============================================================================

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AethonMod.Content.GoozmaPort.Content.Particles;

public sealed class EbonGelChunk : BaseGelChunk<EbonGelChunk>
{
    protected override void Draw(SpriteBatch spriteBatch)
    {
        Texture2D texture = TextureAsset.Value;
        Rectangle frame = texture.Frame(4, 1, style, 0);
        Vector2 squish = new Vector2(1f - Velocity.Length() * 0.01f, 1f + Velocity.Length() * 0.01f);
        float grow = (float)Math.Sqrt(Utils.GetLerpValue(-20, 40, time, true));
        if (sticking) {
            grow = 1f;
            frame = texture.Frame(4, 1, style + 2, 0);
            squish = new Vector2(1f + (float)Math.Sqrt(Utils.GetLerpValue(20, 0, time, true)) * 0.33f, 1f - (float)Math.Sqrt(Utils.GetLerpValue(20, 0, time, true)) * 0.33f);
        }

        Color lightColor = Lighting.GetColor(Position.ToTileCoordinates());
        spriteBatch.Draw(texture, Position - Main.screenPosition, frame, Color.Lerp(Color, Color.MultiplyRGBA(lightColor), sticking ? 1f : Utils.GetLerpValue(10, 50, time, true)), Rotation, frame.Size() * new Vector2(0.5f, 0.84f), Scale * grow * squish, 0, 0);
    }
    
    protected override EbonGelChunk NewInstance()
    {
        return new EbonGelChunk();
    }
}
