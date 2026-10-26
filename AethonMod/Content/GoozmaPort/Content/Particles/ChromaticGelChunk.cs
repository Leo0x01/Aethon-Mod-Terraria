// ============================================================================
// PUERTO DE INVESTIGACIÓN — Calamity: Hunt of the Old God
// (github.com/TohruKobayashi/CalamityHunt) · Team Hypnos:
// caligulasAquarium, IbanPlay, THEREDSTONEBRO, AmmyanDocCiesh, Blockaroz
// Licencia original: GPL-3.0 (ver LICENSE-GPL3.txt en esta carpeta).
// Copiado verbatim por petición del usuario para investigar al jefe
// GOOZMA; pendiente su decisión de re-licencia/modificación.
// ============================================================================

using System;

using AethonMod.Content.GoozmaPort.Content.NPCs.Bosses.GoozmaBoss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AethonMod.Content.GoozmaPort.Content.Particles;

public sealed class ChromaticGelChunk : BaseGelChunk<ChromaticGelChunk>
{
    public override bool RequiresImmediateMode => true;
    
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

        Goozma.GetGradientMapValues(out float[] brightnesses, out Vector3[] colors);
        Effect effect = AssetDirectory.Effects.HolographicGel.Value;
        effect.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly % 1f);
        effect.Parameters["colors"].SetValue(colors);
        effect.Parameters["brightnesses"].SetValue(brightnesses);
        effect.Parameters["baseToScreenPercent"].SetValue(sticking ? 0f : Utils.GetLerpValue(70, 30, time, true));
        effect.Parameters["baseToMapPercent"].SetValue(0f);
        effect.CurrentTechnique.Passes[0].Apply();

        spriteBatch.Draw(texture, Position - Main.screenPosition, frame, Color, Rotation, frame.Size() * new Vector2(0.5f, 0.84f), Scale * grow * squish, 0, 0);

        Main.pixelShader.CurrentTechnique.Passes[0].Apply();
    }
    
    protected override ChromaticGelChunk NewInstance()
    {
        return new ChromaticGelChunk();
    }
}
