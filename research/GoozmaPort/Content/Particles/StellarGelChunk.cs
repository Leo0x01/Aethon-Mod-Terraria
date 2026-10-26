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

public sealed class StellarGelChunk : BaseGelChunk<StellarGelChunk>
{
    protected override void Draw(SpriteBatch spriteBatch)
    {
        Texture2D texture = TextureAsset.Value;
        Rectangle frame = texture.Frame(4, 2, style, 0);
        Rectangle glowFrame = texture.Frame(4, 2, style, 1);
        Vector2 squish = new Vector2(1f - Velocity.Length() * 0.01f, 1f + Velocity.Length() * 0.01f);
        float grow = (float)Math.Sqrt(Utils.GetLerpValue(-20, 40, time, true));
        if (sticking) {
            grow = 1f;
            frame = texture.Frame(4, 2, style + 2, 0);
            glowFrame = texture.Frame(4, 2, style + 2, 1);
            squish = new Vector2(1f + (float)Math.Sqrt(Utils.GetLerpValue(20, 0, time, true)) * 0.33f, 1f - (float)Math.Sqrt(Utils.GetLerpValue(20, 0, time, true)) * 0.33f);
        }
        spriteBatch.Draw(texture, Position - Main.screenPosition, glowFrame, new Color(10, 30, 110, 0) * 0.4f, Rotation, glowFrame.Size() * new Vector2(0.5f, 0.87f), Scale * grow * squish * new Vector2(1.3f, 1.5f), 0, 0);
        spriteBatch.Draw(texture, Position - Main.screenPosition, frame, Color, Rotation, frame.Size() * new Vector2(0.5f, 0.84f), Scale * grow * squish, 0, 0);
        spriteBatch.Draw(texture, Position - Main.screenPosition, frame, new Color(60, 40, 35, 0).MultiplyRGBA(Color), Rotation, frame.Size() * new Vector2(0.5f, 0.84f), Scale * 1.2f * grow * squish, 0, 0);
    }

    protected override StellarGelChunk NewInstance()
    {
        return new StellarGelChunk();
    }
}
