// ============================================================================
// PUERTO DE INVESTIGACIÓN — Calamity: Hunt of the Old God
// (github.com/TohruKobayashi/CalamityHunt) · Team Hypnos:
// caligulasAquarium, IbanPlay, THEREDSTONEBRO, AmmyanDocCiesh, Blockaroz
// Licencia original: GPL-3.0 (ver LICENSE-GPL3.txt en esta carpeta).
// Copiado verbatim por petición del usuario para investigar al jefe
// GOOZMA; pendiente su decisión de re-licencia/modificación.
// ============================================================================

using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.GoozmaPort.Common.Systems.Metaballs;

public abstract class MetaballDrawer : ILoadable
{
    public MetaballRenderTargetContent content;

    public void Load(Mod mod)
    {
        content = new MetaballRenderTargetContent();
        Initialize();
        Main.ContentThatNeedsRenderTargets.Add(content);
    }

    public virtual void Initialize() { }

    public void Unload()
    {
    }
}
