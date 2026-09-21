
using Terraria.ModLoader.Config;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Input;
using System.ComponentModel;

namespace AethonMod.Content
{
    /// <summary>
    /// v6.50.2 — LA CONFIG DE LA AUTORIDAD (split de AethonConfig).
    ///
    /// AethonConfig es ConfigScope.ClientSide — sus valores viven en la
    /// máquina de CADA jugador. Pero DOS de sus decisions las leía el
    /// SERVER (la autoridad): XPMultiplier (ShardLevelSystem.OnKill,
    /// server-side) y EventoHambreGrimorio (ShardPlayer: la furia la
    /// desata el server). En un servidor DEDICADO esas lecturas caían en
    /// el DEFAULT del server y la config del cliente era una ILUSIÓN (el
    /// multiplicador "0.5" del cliente no multiplicaba nada; apagar el
    /// evento en el cliente no apagaba nada).
    ///
    /// Esas dos propiedades viven AHORA aquí, como ConfigScope.ServerSide:
    /// la decisión la toma y la ve la AUTORIDAD (en SP y en Host&Play el
    /// jugador local ES la autoridad — nada cambia; en dedicado, la
    /// config del server manda de verdad y vanilla la negocia con los
    /// clientes al cambiarla).
    ///
    /// Las claves hjson de las entradas (Config.XPMultiplier /
    /// Config.EventoHambreGrimorio si existen) SIGUEN VÁLIDAS: tML resuelve
    /// la localización por NOMBRE de propiedad sin importar la clase.
    /// </summary>
    public class AethonConfigServidor : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [DefaultValue(1f)]
        [Range(0.1f, 10f)]
        public float XPMultiplier = 1f;

        // ------------------------------------------------------------------
        //  v6.47 — LA VOZ DEL HAMBRE Y LA FURIA
        // ------------------------------------------------------------------

        /// <summary>
        /// ¿El grimorio hambriento convoca sus OLEADAS cuando pasa
        /// demasiado tiempo sin comer? ON por defecto (el evento ES
        /// contenido del mod de pruebas). Apágalo para dejar SOLO los
        /// susurros y la barra palidecida (puro sabor, cero castigo).
        /// La Carnada del Grimorio (el ítem de prueba) funciona SIEMPRE,
        /// con la bandera apagada o no.
        /// </summary>
        [DefaultValue(true)]
        public bool EventoHambreGrimorio = true;
    }
}
