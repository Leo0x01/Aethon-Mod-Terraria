using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// AethonMenorBuff — v6.50.49 — LA MASCOTA DE LUZ.
    ///
    /// La letra del usuario: «ademas crea una pequeña mascota de luz que
    /// sea Aethon original pero mas pequeño». EL AETHON MENOR: una
    /// chispa viva de la Luz Primordial — su núcleo, sus perlas y su
    /// arcoíris en miniatura, flotando a tu lado y ALUMBRANDO tu
    /// camino (MASCOTA DE LUZ de vanilla: Main.lightPet — la familia
    /// de la antorcha espectral y del fuego fatuo).
    ///
    /// El patrón de la casa (CriaEstelarBuff): el buff vive mientras la
    /// mascota viva — su IA lo sostiene.
    /// </summary>
    public class AethonMenorBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName y Description se cargan desde Localization.
            Main.buffNoTimeDisplay[Type] = true;   // sin timer (como toda mascota)
            Main.buffNoSave[Type] = true;          // no se guarda (se recrea al invocar)
            Main.lightPet[Type] = true;            // v6.50.49 — MASCOTA DE LUZ de vanilla
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // El buff vive mientras el Aethon menor viva.
            if (player.ownedProjectileCounts[
                ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmetic.AethonMenorPet>()] == 0)
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
