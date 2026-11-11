using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Globals.Investigaciones
{
    /// <summary>
    /// InvestigacionesGlobalNPC — v6.124 — LA MEMORIA DE LAS MARCAS de EL
    /// PRINCIPIO Y EL FIN (la copia de investigación de The Beginning and
    /// The End de Calamity Entropy).
    ///
    /// LA FICHA DEL ORIGINAL (wiki oficial): las dagas aplican DOS debuffs
    /// que ACUMULAN hasta 8 CAPAS cada uno — el wiki de Calamity Entropy
    /// los cuenta por su sistema de NPC moddeado; la casa los cuenta AQUÍ,
    /// en el GlobalNPC (una instancia por NPC — la memoria de la marca que
    /// lleva puesta cada impío).
    /// </summary>
    public class InvestigacionesGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        /// <summary>Las capas de la MARCA DEL PRINCIPIO (la daga de ORO — 0..8).</summary>
        public int StacksPrincipio;

        /// <summary>Las capas de la MIRADA DEL FIN (la daga AZUL — 0..8).</summary>
        public int StacksFin;
    }
}
