using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// v6.167 — EL MUNDO INTERIOR ES UN BIOMA VÁLIDO PARA TODOS LOS
    /// JEFES (la letra: «en el mundo interior asegurate de que todo
    /// jefe pueda ser invocado dentro del mundo interior, eso quiere
    /// decir que el mundo interior se vuelve un bioma valido para
    /// todos los jefes de terraria y mas»).
    ///
    /// LA VÍA: los jefes de vanilla verifican LAS ZONAS DEL JUGADOR
    /// (ZoneJungle para Plantera, ZoneSnow, ZoneCorrupt, ZoneBeach,
    /// ZoneUnderworldHeight…) al decidir si SU invocación procede —
    /// el mundo interior es una caja de tinta SIN bioma, así que todo
    /// jefe con requisito de zona REBOTABA. LA CURA: mientras el
    /// portador vive en el submundo, este ModPlayer marca TODAS las
    /// zonas de vanilla como ACTIVAS — el interior es EL BIOMA DE
    /// TODOS (la página en blanco donde cualquier jefe puede nacer).
    /// PostUpdateZones corre DESPUÉS de que vanilla computa sus
    /// zonas: pisamos el resultado final.
    /// </summary>
    public class SubmundoBiomasPlayer : ModPlayer
    {
        public override void PostUpdate()
        {
            if (!global::AethonMod.Content.Systems.SubmundoGrimorio.Activo)
                return;

            // TODAS las zonas, ACTIVAS: selva, nieve, corrupción,
            // carmesí, desierto, playa, dungeons, templo, cielo,
            // granito, mármol, brillo, cementerio… el mundo interior
            // las contiene TODAS (es la página del Eterno).
            Player.ZoneJungle = true;
            Player.ZoneSnow = true;
            Player.ZoneCorrupt = true;
            Player.ZoneCrimson = true;
            Player.ZoneHallow = true;
            Player.ZoneDesert = true;
            Player.ZoneBeach = true;
            Player.ZoneDungeon = true;
            Player.ZoneMeteor = true;
            Player.ZoneGranite = true;
            Player.ZoneMarble = true;
            Player.ZoneGlowshroom = true;
            Player.ZoneGraveyard = true;
            Player.ZoneTowerSolar = true;
            Player.ZoneTowerVortex = true;
            Player.ZoneTowerNebula = true;
            Player.ZoneTowerStardust = true;
            Player.ZoneUnderworldHeight = true;
            Player.ZoneRockLayerHeight = true;
            Player.ZoneDirtLayerHeight = true;
            Player.ZoneSkyHeight = true;
            Player.ZoneRain = false;      // la tinta no llueve
            Player.ZoneSandstorm = false; // ni hay tormenta
            Player.ZoneOldOneArmy = false;
        }
    }
}
