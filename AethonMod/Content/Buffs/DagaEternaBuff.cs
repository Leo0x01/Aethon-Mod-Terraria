using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// DagaEternaBuff — v6.124 — el buff de LAS CINCO DAGAS DEL ETERNO (la
    /// letra: «vienen las 5 dagas que tambien tendran un ojo en el centro»).
    ///
    /// EL PATRÓN DEL BUFF DE LOS OJOS (v6.50.111/86): el reloj nunca
    /// expira (EscuadronUtil.Sostener lo refresca cada tick), el icono
    /// es PROPIO (32×32, el estándar de la casa) y la ÚNICA despedida
    /// legítima es el CLIC DERECHO en este icono (la letra v6.51.12:
    /// «solo se deben desactivar si el jugador los quita usando su
    /// buff de minion») — despide al escuadrón de dagas ENTERO.
    ///
    /// EL ICONO: la daga violeta del vacío con el OJO DORADO en el
    /// centro de la hoja (la firma de las dagas — el iris que mira),
    /// 32×32 100 % opaco con el marco pequeño de la casa (1 px de
    /// amatista con esquinitas doradas, el estilo v6.123). Si un día
    /// se cambia el arte: REEMPLAZAR el .png manteniendo 32×32.
    /// </summary>
    public class DagaEternaBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName y Description se cargan desde Localization
            Main.buffNoTimeDisplay[Type] = true; // no muestra el timer (como los minions)
            Main.buffNoSave[Type] = true;        // no se guarda al salir (se recrea al invocar)
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // El buff se mantiene mientras haya dagas activas
            if (player.ownedProjectileCounts[
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.DagaEternaMinion>()] == 0)
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

        // LA DESPEDIDA LEGÍTIMA (la letra: «solo se deben desactivar si
        // el jugador los quita usando su buff de minion»): el CLIC
        // DERECHO en ESTE icono despide al escuadrón de dagas entero
        // (se disuelven en motas violetas) — el mismo contrato de los ojos.
        public override bool RightClick(int buffIndex)
        {
            try
            {
                global::AethonMod.Content.Systems.EscuadronUtil.Despedir(
                    ModContent.ProjectileType<global::AethonMod.Content.Projectiles.DagaEternaMinion>());
            }
            catch { }
            return true;   // y el buff se cancela (vanilla lo retira)
        }
    }
}
