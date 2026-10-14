using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// GrimorioMenorBuff — v6.50.122 — LA SEGUNDA MASCOTA: EL GRIMORIO
    /// MENOR (la miniatura del LIBRO — no confundir con el Aethon Menor,
    /// la miniatura del JEFE de luz: son DOS mascotas distintas, la
    /// letra del usuario que desenredó los conceptos).
    ///
    /// Sostiene a GrimorioMenorPet: el mini-grimorio con sus 3 mini
    /// minions (los mini ojos), su sombrilla de página que muerde, sus
    /// travesuras contra los vecinos cuando te quedas AFK y su
    /// aburrimiento a los 10 golpes (la máquina entera vive en
    /// ParejaCerebro).
    ///
    /// El patrón de la casa (AethonMenorBuff): el buff vive mientras la
    /// mascota viva — su IA lo sostiene. SIN Main.lightPet: el que
    /// alumbra es el Aethon Menor; el libro solo arde por dentro.
    /// </summary>
    public class GrimorioMenorBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName y Description se cargan desde Localization.
            Main.buffNoTimeDisplay[Type] = true;   // sin timer (como toda mascota)
            Main.buffNoSave[Type] = true;          // no se guarda (se recrea al invocar)
            // SIN Main.lightPet — no es mascota de luz (ese trono es del
            // Aethon Menor: la casa no mezcla conceptos).
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // El buff vive mientras el mini grimorio viva.
            if (player.ownedProjectileCounts[
                ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmetic.GrimorioMenorPet>()] == 0)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                // El timer nunca se agota (la mascota lo sostiene).
                player.buffTime[buffIndex] = 18000;
            }
        }
    }
}
