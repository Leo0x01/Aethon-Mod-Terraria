// ============================================================================
// PUERTO DE INVESTIGACIÓN — Calamity: Hunt of the Old God
// (github.com/TohruKobayashi/CalamityHunt) · Team Hypnos:
// caligulasAquarium, IbanPlay, THEREDSTONEBRO, AmmyanDocCiesh, Blockaroz
// Licencia original: GPL-3.0 (ver LICENSE-GPL3.txt en esta carpeta).
// Copiado verbatim por petición del usuario para investigar al jefe
// GOOZMA; pendiente su decisión de re-licencia/modificación.
// ============================================================================

using System.Collections.Generic;
using System.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AethonMod.Content.GoozmaPort.Common.Systems;

public sealed class BossDownedSystem : ModSystem
{
    public const string KeyPrefix = "downedBoss";
    public const string GoozmaKey = "Goozma";
    public const string PluriKey = "Pluripotent";

    public static BossDownedSystem Instance => ModContent.GetInstance<BossDownedSystem>();

    public bool GoozmaDowned
    {
        get => downedBoss[GoozmaKey];
        set => downedBoss[GoozmaKey] = value;
    }
    public bool PluripotentDowned
    {
        get => downedBoss[PluriKey];
        set => downedBoss[PluriKey] = value;
    }

    private readonly Dictionary<string, bool> downedBoss = new()
    {
        { GoozmaKey, false },
        { PluriKey, false },
    };

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(GoozmaDowned);
        writer.Write(PluripotentDowned);
        base.NetSend(writer);
    }

    public override void NetReceive(BinaryReader reader)
    {
        GoozmaDowned = reader.ReadBoolean();
        PluripotentDowned = reader.ReadBoolean();
        base.NetReceive(reader);
    }

    public override void SaveWorldData(TagCompound tag)
    {
        foreach (string entry in downedBoss.Keys) {
            tag[KeyPrefix + entry] = downedBoss[entry];
        }
    }

    public override void LoadWorldData(TagCompound tag)
    {
        foreach (string entry in downedBoss.Keys) {
            downedBoss[entry] = tag.GetBool(KeyPrefix + entry);
        }
    }
}
