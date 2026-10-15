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
    /// v6.50.124 — EL FIX DEL SLOT Y DEL NACIMIENTO (la letra del
    /// usuario: «ninguna de las dos mascotas se pueden poner en donde
    /// van las mascotas en el equipamiento» + «el pequeño grimorio no
    /// tiene sus minion»): la .122 declaraba «SIN Main.lightPet» PERO
    /// JAMÁS encendió Main.vanityPet — y el flag es EL REQUISITO de
    /// vanilla para las DOS cosas (verificado al IL de ESTE tML
    /// 2026.08.3.0): ItemSlot.cs caso 19 (la ranura de mascota) solo
    /// acepta items con «vanityPet[buffType] && !lightPet[buffType]»,
    /// y la vida entera de la mascota (engendre + sostén + re-engendre
    /// si algo la mata) vive en el patrón canónico del ExamplePet de
    /// esta MISMA versión: BuffHandle_SpawnPetIfNeededAndSetTime.
    /// Sin el flag, el ítem no entraba en la ranura Y el buff moría
    /// solo a los 3600 ticks del uso (el par de la luz se sostenía a
    /// 18000; este libro no tenía quien lo sostuviera entero).
    ///
    /// El patrón de la casa (AethonMenorBuff, ahora canónico en ambas):
    /// el buff sostiene el reloj Y engendra al libro si falta — la
    /// mascota lo mata su IA si el buff se apaga (clic al icono del
    /// buff para despedirlo, como toda mascota de vanilla).
    /// </summary>
    public class GrimorioMenorBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName y Description se cargan desde Localization.
            Main.buffNoTimeDisplay[Type] = true;   // sin timer (como toda mascota)
            Main.buffNoSave[Type] = true;          // no se guarda (se recrea al invocar)
            // SIN Main.lightPet — no es mascota de luz (ese trono es del
            // Aethon Menor: la casa no mezcla conceptos)…
            Main.vanityPet[Type] = true;           // …PERO ES MASCOTA DE VANILLA:
                                                    // la ranura de mascota del equipo
                                                    // (ItemSlot caso 19) SOLO acepta
                                                    // items cuyo buff encienda ESTE
                                                    // flag — v6.50.124, la cura.
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // EL PATRÓN CANÓNICO (ExamplePetBuff del tML 2026.08.3.0 —
            // verificado al IL: BuffHandle_SpawnPetIfNeededAndSetTime
            // sostiene el reloj a 18000 Y engendra al libro en el cliente
            // del dueño si falta — y lo RE-ENGENDRA si algo lo mata, el
            // contrato de toda mascota de vanilla). La .122 no engendraba
            // NADIE: el disparo del ítem creaba al libro y el buff solo
            // miraba — si el libro moría por otra vía, el buff se apagaba
            // en el mismo tick y no había vuelta atrás.
            bool dummy = false;
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref dummy,
                ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmetic.GrimorioMenorPet>(), 18000);
        }
    }
}
