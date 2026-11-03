using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// VastagoDelEternoBuff — v6.91 — LA MASCOTA NUEVA: EL VÁSTAGO DEL
    /// ETERNO, nacida del pipeline de la investigación sprite-gen
    /// (aldegad, Apache-2.0 — «una sola imagen de personaje → sprites
    /// animados listos para el motor»: la imagen base se generó con
    /// identidad única y fondo croma, la transparencia real salió por
    /// flood-fill desde los bordes, y los ciclos idle/walk — respiración
    /// con squash&amp;stretch + parpadeo, y el vuelo con balanceo — son
    /// el atlas de 8 frames del projectile).
    ///
    /// El patrón canónico de la casa (GrimorioMenorBuff, el ExamplePet
    /// del tML): el buff sostiene el reloj Y engendra al vástago si
    /// falta (y lo RE-ENGENDRA si algo lo mata).
    /// </summary>
    public class VastagoDelEternoBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName y Description se cargan desde Localization.
            Main.buffNoTimeDisplay[Type] = true;   // sin timer (como toda mascota)
            Main.buffNoSave[Type] = true;          // no se guarda (se recrea al invocar)
            // SIN Main.lightPet — no es mascota de luz (ese trono es del
            // Aethon Menor)…
            Main.vanityPet[Type] = true;           // …PERO ES MASCOTA DE VANILLA:
                                                    // la ranura de mascota del equipo
                                                    // (ItemSlot caso 19) SOLO acepta
                                                    // items cuyo buff enciende ESTE
                                                    // flag — el patrón .124.
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // EL PATRÓN CANÓNICO: sostiene el reloj a 18000 Y engendra al
            // vástago en el cliente del dueño si falta.
            bool dummy = false;
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref dummy,
                ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmetic.VastagoDelEternoPet>(), 18000);
        }
    }
}
