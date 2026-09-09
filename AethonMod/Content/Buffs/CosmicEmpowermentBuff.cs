using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// Empoderamiento Cósmico — buff ceremonial aplicado al llevar
    /// equipado el Sello de Aethon.
    /// - +10% daño (todas las clases)
    /// - +5% probabilidad de crítico
    /// - 1% de lifesteal en cualquier ataque
    /// El buff se refresca cada frame mientras el accesorio esté equipado.
    /// </summary>
    public class CosmicEmpowermentBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // No mostrar timer porque es mantenido por el accesorio
            Main.buffNoTimeDisplay[Type] = true;
            // No se guarda al salir (se recrea al equipar el sello)
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // +10% daño a todas las clases (Generic afecta todas)
            player.GetDamage(DamageClass.Generic) += 0.10f;

            // +5% probabilidad de crítico a todas las clases
            player.GetCritChance(DamageClass.Generic) += 5f;

            // +5% velocidad de uso (ataques más rápidos) — bonus sutil
            player.GetAttackSpeed(DamageClass.Generic) += 0.05f;

            // Lifesteal 1%: se aplica via el ModPlayer (ShardPlayer)
            // porque ModPlayer tiene acceso a OnHitNPC para inyectar el heal.
            // Aquí solo marcamos el flag.
            var sp = player.GetModPlayer<global::AethonMod.Content.Players.ShardPlayer>();
            if (sp != null)
            {
                sp.HasCosmicEmpowerment = true;
            }

            // Resetear timer para que no se agote (lo refresca el UpdateAccessory)
            player.buffTime[buffIndex] = 60;
        }
    }
}
