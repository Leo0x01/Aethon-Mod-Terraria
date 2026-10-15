using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
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
    /// v6.50.124 — EL PATRÓN CANÓNICO COMPLETO (la cura de la .124 para
    /// su hermano el mini grimorio también le sienta bien): el buff
    /// sostiene el reloj Y engendra a la cría si falta mediante
    /// BuffHandle_SpawnPetIfNeededAndSetTime (verificado al IL de ESTE
    /// tML 2026.08.3.0 — el mismo helper que usa el ExampleLightPet de
    /// esta versión): si algo mata a la cría, NACE OTRA (el contrato de
    /// toda mascota de vanilla). El gemelo oscuro vive como siempre:
    /// mientras la cría de luz viva, el buff también sostiene a su
    /// hermana de sombra (nace junto al dueño si no está; su IA la mata
    /// si este buff muere). Solo la engendra EL CLIENTE DEL DUEÑO.
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
            // EL PATRÓN CANÓNICO (ExampleLightPetBuff del tML 2026.08.3.0):
            // el buff sostiene el reloj a 18000 Y engendra a la cría en el
            // cliente del dueño si falta (y la RE-ENGENDRA si algo la mata).
            bool dummy = false;
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref dummy,
                ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmetic.AethonMenorPet>(), 18000);

            // v6.50.117 — EL GEMELO OSCURO: mientras la cría de luz viva, el
            // buff TAMBIÉN sostiene a su hermano de oscuridad (nace junto
            // al dueño si no está; su IA lo mata si este buff muere — el
            // MISMO contrato del claro, la misma verificación por
            // ownedProjectileCounts). Solo lo engendra EL CLIENTE DEL
            // DUEÑO y jamás el servidor dedicado: como toda mascota
            // netImportant, nace local y el cable lo sincroniza con el
            // resto del mundo.
            if (player.ownedProjectileCounts[
                ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmetic.AethonMenorPet>()] > 0)
            {
                int gemelo = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmetic.AethonMenorOscuroPet>();
                if (player.ownedProjectileCounts[gemelo] == 0 &&
                    player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server)
                {
                    Projectile.NewProjectile(player.GetSource_FromThis(), player.Center, Vector2.Zero,
                        gemelo, 0, 0f, player.whoAmI);
                }
            }
        }
    }
}
