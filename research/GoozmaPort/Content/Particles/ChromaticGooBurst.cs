// ============================================================================
// PUERTO DE INVESTIGACIÓN — Calamity: Hunt of the Old God
// (github.com/TohruKobayashi/CalamityHunt) · Team Hypnos:
// caligulasAquarium, IbanPlay, THEREDSTONEBRO, AmmyanDocCiesh, Blockaroz
// Licencia original: GPL-3.0 (ver LICENSE-GPL3.txt en esta carpeta).
// Copiado verbatim por petición del usuario para investigar al jefe
// GOOZMA; pendiente su decisión de re-licencia/modificación.
// ============================================================================

using System;
using AethonMod.Content.GoozmaPort.Common.Systems.Particles;
using AethonMod.Content.GoozmaPort.Common.Utilities;
using AethonMod.Content.GoozmaPort.Content.NPCs.Bosses.GoozmaBoss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AethonMod.Content.GoozmaPort.Content.Particles;

public sealed class ChromaticGooBurst : Particle<ChromaticGooBurst>
{
    public ColorOffsetData colorData;

    public int style;

    private int frame;

    private int frameCounter;
    
    public override void FetchFromPool()
    {
        base.FetchFromPool();

        colorData = default(ColorOffsetData);
        style = 0;
        frame = 0;
        frameCounter = 0;
    }

    public override void OnSpawn()
    {
        base.Update();
        
        Scale *= Main.rand.NextFloat(0.9f, 1.1f);
        style = Main.rand.Next(2);
        Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
        Velocity = Vector2.Zero;
    }

    protected override void Update()
    {
        frameCounter++;

        if (frame < 3) {
            frameCounter++;
        }

        if (frameCounter % 4 == 0) {
            frame++;
        }

        if (frame > 7) {
            ShouldBeRemovedFromRenderer = true;
        }

        if (colorData.active) {
            Color = new GradientColor(SlimeUtils.GoozColors, 0.2f, 0.2f).ValueAt(colorData.offset + Math.Max(0, frameCounter - 18) * 3f - 2f);
        }
    }

    protected override void Draw(SpriteBatch spriteBatch)
    {
        Texture2D texture = TextureAsset.Value;
        Rectangle drawFrame = texture.Frame(8, 4, frame, style);
        Rectangle glowFrame = texture.Frame(8, 4, frame, style + 2);

        spriteBatch.Draw(texture, Position - Main.screenPosition, drawFrame, Color.LightGray, Rotation, drawFrame.Size() * new Vector2(0.5f, 1f), Scale, 0, 0);
        spriteBatch.Draw(texture, Position - Main.screenPosition, glowFrame, Color, Rotation, glowFrame.Size() * new Vector2(0.5f, 1f), Scale, 0, 0);
        spriteBatch.Draw(texture, Position - Main.screenPosition, glowFrame, (Color * 0.5f) with { A = 0 }, Rotation, glowFrame.Size() * new Vector2(0.5f, 1f), Scale * 1.01f, 0, 0);
    }
    
    protected override ChromaticGooBurst NewInstance()
    {
        return new ChromaticGooBurst();
    }
}
