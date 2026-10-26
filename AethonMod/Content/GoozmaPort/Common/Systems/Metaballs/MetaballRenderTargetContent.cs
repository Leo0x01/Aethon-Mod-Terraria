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
using Terraria.GameContent;

namespace AethonMod.Content.GoozmaPort.Common.Systems.Metaballs;

public class MetaballRenderTargetContent : ARenderTargetContentByRequest
{
    public Action<SpriteBatch> draw;
    public int width;
    public int height;

    protected override void HandleUseReqest(GraphicsDevice device, SpriteBatch spriteBatch)
    {
        device.SetRenderTarget(_target);
        device.Clear(Color.Transparent);

        PrepareARenderTarget_AndListenToEvents(ref _target, device, width, height, RenderTargetUsage.PreserveContents);

        draw?.Invoke(spriteBatch);

        device.SetRenderTarget(null);
        _wasPrepared = true;
    }
}
