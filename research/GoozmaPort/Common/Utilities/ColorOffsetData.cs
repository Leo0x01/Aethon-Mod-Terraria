// ============================================================================
// PUERTO DE INVESTIGACIÓN — Calamity: Hunt of the Old God
// (github.com/TohruKobayashi/CalamityHunt) · Team Hypnos:
// caligulasAquarium, IbanPlay, THEREDSTONEBRO, AmmyanDocCiesh, Blockaroz
// Licencia original: GPL-3.0 (ver LICENSE-GPL3.txt en esta carpeta).
// Copiado verbatim por petición del usuario para investigar al jefe
// GOOZMA; pendiente su decisión de re-licencia/modificación.
// ============================================================================

namespace AethonMod.Content.GoozmaPort.Common.Utilities;

public struct ColorOffsetData
{
    public ColorOffsetData(bool active = false, float offset = 0f)
    {
        this.active = active;
        this.offset = offset;
    }

    public bool active;
    public float offset;
}
