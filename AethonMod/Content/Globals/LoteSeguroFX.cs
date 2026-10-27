using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// LOTESEGUROFX — v6.54.5 — EL BLINDAJE DEL CONTRATO DEL LOTE.
    ///
    /// LA AUTOPSIA DEL CLIENT.LOG DE LA v6.54.4: el usuario agarró LA
    /// PRIMERA arma de la bolsa (EL OJO DEL JUICIO) y el juego SE CERRÓ —
    /// «System.InvalidOperationException: End was called, but Begin has
    /// not yet been called» en Main.DrawProjectiles. CAUSA: los PreDraw
    /// de las Doce Sentencias usan VFXCore.FlushAdditive/FlushAlpha y los
    /// helpers de RayoStrip/OndaLib, que CIERRAN el SpriteBatch del pase
    /// de entidades; al salir del PreDraw sin reabrirlo, el End final del
    /// bucle de proyectiles de Terraria revienta y tML mata el proceso.
    /// (El headless no lo cazó: el servidor dedicado NO ejecuta PreDraw —
    /// este bug solo existe en cliente con render.)
    ///
    /// LA CURA EN DOS CAPAS:
    ///   1. Cada PreDraw de las Sentencias ahora sale con
    ///      VFXCore.ReabrirLoteVanilla() (el contrato v6.50.11 que 131
    ///      archivos viejos ya cumplían y las armas nuevas olvidaron).
    ///   2. ESTE blindaje: después del dibujado de CADA proyectil se
    ///      verifica el lote — si alguien (un camino olvidado, una
    ///      excepción tragada a mitad de dibujado, un arma futura) lo
    ///      dejó cerrado, se CURA aquí, ANTES de que el End de Main
    ///      pueda explotar. Para el proyectil vanilla que jamás toca el
    ///      lote el hook es un no-op de una sonda (lectura de reflexión).
    /// </summary>
    public class LoteSeguroFX : GlobalProjectile
    {
        // Firma medida por reflexión sobre el tModLoader.dll 2026.08.3.0 real
        // (sin el drawType del tML clásico — el oráculo cazó la diferencia).
        public override void PostDraw(Projectile projectile, Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return;
            // Idempotente por sonda: si el lote ya está vivo, no hace NADA.
            VFXCore.ReabrirLoteVanilla();
        }
    }
}
