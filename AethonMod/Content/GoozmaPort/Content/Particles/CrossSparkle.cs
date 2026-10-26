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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AethonMod.Content.GoozmaPort.Content.Particles;

public sealed class CrossSparkle : Particle<CrossSparkle>
{
    private int time;
    public Func<Vector2> anchor;
    
    public override void FetchFromPool()
    {
        base.FetchFromPool();

        time = 0;
        anchor = null;
    }

    public override void OnSpawn()
    {
        Rotation = Velocity.ToRotation() + Main.rand.NextFloat(-0.05f, 0.05f);
        Velocity = Vector2.Zero;
    }

    protected override void Update()
    {
        base.Update();
        
        time++;
        if (time > 15) {
            ShouldBeRemovedFromRenderer = true;
        }

        if (anchor != null) {
            Position += anchor.Invoke();
        }
    }

    protected override void Draw(SpriteBatch spriteBatch)
    {
        Texture2D texture = TextureAsset.Value;

        Vector2 drawScale = Scale * MathF.Pow(Utils.GetLerpValue(15, 6, time, true), 2.5f) * Utils.GetLerpValue(0, 5, time, true);
        Color drawColor = Color with { A = 0 };
        spriteBatch.Draw(texture, Position - Main.screenPosition, texture.Frame(), Color * 0.2f, Rotation - MathHelper.PiOver4, texture.Size() * 0.5f, drawScale * new Vector2(0.3f, 0.5f), 0, 0);
        spriteBatch.Draw(texture, Position - Main.screenPosition, texture.Frame(), Color * 0.2f, Rotation + MathHelper.PiOver4, texture.Size() * 0.5f, drawScale * new Vector2(0.3f, 0.5f), 0, 0);
        spriteBatch.Draw(texture, Position - Main.screenPosition, texture.Frame(), drawColor, Rotation - MathHelper.PiOver4, texture.Size() * 0.5f, drawScale * new Vector2(0.6f, 1f), 0, 0);
        spriteBatch.Draw(texture, Position - Main.screenPosition, texture.Frame(), drawColor, Rotation + MathHelper.PiOver4, texture.Size() * 0.5f, drawScale * new Vector2(0.6f, 1f), 0, 0);
        spriteBatch.Draw(texture, Position - Main.screenPosition, texture.Frame(), drawColor * 0.2f, Rotation - MathHelper.PiOver4, texture.Size() * 0.5f, drawScale * new Vector2(0.4f, 2f), 0, 0);
        spriteBatch.Draw(texture, Position - Main.screenPosition, texture.Frame(), drawColor * 0.2f, Rotation + MathHelper.PiOver4, texture.Size() * 0.5f, drawScale * new Vector2(0.4f, 2f), 0, 0);
        spriteBatch.Draw(texture, Position - Main.screenPosition, texture.Frame(), new Color(255, 255, 255, 0), Rotation - MathHelper.PiOver4, texture.Size() * 0.5f, drawScale * 0.5f, 0, 0);
        spriteBatch.Draw(texture, Position - Main.screenPosition, texture.Frame(), new Color(255, 255, 255, 0), Rotation + MathHelper.PiOver4, texture.Size() * 0.5f, drawScale * 0.5f, 0, 0);
    }
    
    protected override CrossSparkle NewInstance()
    {
        return new CrossSparkle();
    }
}
