namespace AethonMod.Content.Players
{
    /// <summary>
    /// Las 3 ramas principales de combate del mod.
    /// El Fragmento Génesis evoluciona según la rama que el jugador desarrolle primero.
    /// </summary>
    public enum BranchType
    {
        /// <summary>Sin rama asignada aún (el fragmento aún no se ha imprpreso).</summary>
        None = 0,

        /// <summary>Ranged: arcos, munición, armas arrojadizas. Arma: Lumina, la Arcoestelar.</summary>
        Distance = 1,

        /// <summary>Melee: espadas, lanzas, yoyos. Arma: Solbrand, Filo del Alba.</summary>
        Melee = 2,

        /// <summary>Magic + Summoner fusionados. Arma: Grimorio del Eterno.</summary>
        Magic = 3,
    }

    /// <summary>
    /// Sub-forma del arma dentro de una rama (ej: en Distancia, puede ser arco/pistola/arrojadiza).
    /// </summary>
    public enum WeaponSubForm
    {
        None = 0,
        // Distance: arco, pistola/escopeta, arrojadiza
        Bow = 10,
        Gun = 11,
        Thrown = 12,
        // Melee: espada, lanza, yoyo
        Sword = 20,
        Spear = 21,
        Yoyo = 22,
        // Magic: grimorio de hechizos, grimorio de invocación
        Spellbook = 30,
        Summonbook = 31,
    }
}
