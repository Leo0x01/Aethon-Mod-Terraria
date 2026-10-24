using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// OjoEternoBuff — v6.50.111 — el buff de LOS OJOS DEL ETERNO.
    ///
    /// La letra del usuario: «los minion que use el grimorio será su
    /// propio ojo» — los minions del Grimorio del Eterno ya no son el
    /// Orbe Cósmico: son los OJOS del libro, que flotan en corona sobre
    /// el dueño (u orbitan el libro flotante en anillos concéntricos) y
    /// disparan el rayo Perlin negro de bordes morados.
    ///
    /// El patrón del Orbe Cósmico (v6.50.3: el timeLeft nunca expira) y
    /// v6.50.111 — EL ICONO PROPIO: la letra «el icono que muestra el
    /// buff del minion es demasiado grande» — este buff apuntaba su
    /// textura AL SPRITE DEL PROYECTIL (56×54) y el HUD dibuja la
    /// textura TAL CUAL: salía GIGANTE, montado sobre los vecinos. El
    /// icono ahora es Content/Buffs/OjoEternoBuff.png (32×32 — el
    /// estándar de los otros 10 buffs de la casa): EL OJO COMPUESTO —
    /// la base OjoEterno_Abierto CON EL IRIS en su reposo (en runtime
    /// el iris se dibuja aparte), reducido a 32×32 por
    /// tools/gen_icono_ojo_v650111.py. Si un día se cambia el arte:
    /// REEMPLAZAR ese .png manteniendo 32×32.
    /// </summary>
    public class OjoEternoBuff : ModBuff
    {
        // (v6.50.105-110 apuntaba a Content/Projectiles/OjoEterno_Abierto,
        //  56×54 — el HUD lo dibujaba ENORME. Textura por convención: el
        //  .png junto al .cs, 32×32 como los demás buffs.)

        public override void SetStaticDefaults()
        {
            // DisplayName y Description se cargan desde Localization
            Main.buffNoTimeDisplay[Type] = true; // no muestra el timer (como los minions)
            Main.buffNoSave[Type] = true;        // no se guarda al salir (se recrea al invocar)
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // El buff se mantiene mientras haya ojos activos
            if (player.ownedProjectileCounts[
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.OjoEternoMinion>()] == 0)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                // Resetear el timer del buff (que no se agote)
                player.buffTime[buffIndex] = 18000;
            }
        }

        // ================================================================
        //  v6.51.12 — LA DESPEDIDA LEGÍTIMA (la letra del usuario: «solo
        //  se deben desactivar si el jugador los quita usando su buff de
        //  minion»): el CLIC DERECHO en ESTE icono despide al escuadrón
        //  entero (se disuelven en tinta). Es la ÚNICA despedida que
        //  queda — la barra de buffs llena ya NO los mata (ver
        //  EscuadronUtil: la cura de la desinvocación silenciosa).
        // ================================================================
        public override bool RightClick(int buffIndex)
        {
            try
            {
                global::AethonMod.Content.Systems.EscuadronUtil.Despedir(
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.OjoEternoMinion>());
            }
            catch { }
            return true;   // y el buff se cancela (vanilla lo retira)
        }
    }
}
