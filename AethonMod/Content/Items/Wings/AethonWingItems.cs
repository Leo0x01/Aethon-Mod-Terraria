using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Wings
{
    /// <summary>
    /// AethonWingsItem — v6.11 — LA BASE DE LAS 8 ALAS DEL MOD.
    ///
    /// Las alas son ahora SPRITES de arte generado y refinado (IA →
    /// simetría → contorno Terraria → animación de 4 frames) que usan el
    /// sistema VANILLA de alas: [AutoloadEquip] reserva el slot de equipo,
    /// el PNG {Nombre}_Wings.png es la tira de frames y tML la corta con
    /// Height()/4 (frame 0 reposo, ciclo 0-1-2 al volar, frame 2 al
    /// planear — así dibuja DrawPlayer_09_Wings para alas moddeadas).
    ///
    /// FIX v6.11 (el reporte del usuario "mal animadas, fondo no
    /// transparente"): v6.10 creyó que vanilla cortaba con Height()/7 —
    /// ¡ese era el caso especial de las alas 22/43/44! El camino POR
    /// DEFECTO usa num13=4 → las tiras de 7 frames se cortaban en cuartos
    /// y las alas salían como TRES BANDAS rotas con huecos. Además el
    /// origen real es (Width/2, Height/8) = CENTRO del frame → la raíz
    /// del ala vive ahí, y el pipeline v6.11 (gen_ai_wings_v611.py)
    /// elimina el fondo gris del arte IA por CONECTIVIDAD (flood-fill
    /// desde los bordes) — el fondo queda 100% transparente.
    ///
    /// Cada subclase conserva SUS estadísticas end-game y tooltips.
    /// </summary>
    public abstract class AethonWingsItem : ModItem
    {
        /// <summary>Ticks de vuelo (Solar = 180).</summary>
        public virtual int FlyTime => 180;

        /// <summary>Velocidad de vuelo (Solar = 9).</summary>
        public virtual float FlySpeed => 9f;

        /// <summary>Multiplicador de aceleración (Solar = 2.5).</summary>
        public virtual float FlyAccel => 2.5f;

        /// <summary>¿Vuelo estacionario (mantener salto para flotar)?</summary>
        public virtual bool HasHover => false;

        /// <summary>Velocidad de flotado.</summary>
        public virtual float HoverSpeed => 8f;

        /// <summary>Aceleración de flotado.</summary>
        public virtual float HoverAccel => 3f;

        public override void SetStaticDefaults()
        {
            // Estadísticas end-game por el camino oficial (+ hover opcional).
            ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = HasHover
                ? new WingStats(FlyTime, FlySpeed, FlyAccel, true, HoverSpeed, HoverAccel)
                : new WingStats(FlyTime, FlySpeed, FlyAccel);
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 24;
            Item.value = Item.buyPrice(gold: 15);
            Item.rare = ItemRarityID.Red;
            Item.accessory = true;
        }

        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling,
            ref float ascentWhenRising, ref float maxCanAscendMultiplier,
            ref float maxAscentMultiplier, ref float constantAscend)
        {
            ascentWhenFalling = 0.85f;
            ascentWhenRising = 0.15f;
            maxCanAscendMultiplier = 1f;
            maxAscentMultiplier = 3f;
            constantAscend = 0.135f;
        }

        public override void AddRecipes()
        {
            // Receta barata de pruebas: que nunca se pierdan (como las coronas).
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // =====================================================================
    //  LAS 8 ALAS — arte IA refinado, animación vanilla de 4 frames
    // =====================================================================

    /// <summary>
    /// ALAS DEL HORIZONTE DE SUCESOS: alas de energía violeta-negra con
    /// remolinos de acreción magenta y anillos de fotones blancos en las
    /// puntas.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class EventHorizonWings : AethonWingsItem
    {
        public override int FlyTime => 200;
        public override float FlySpeed => 9.5f;
        public override float FlyAccel => 3f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/FF1738:═══ ALAS DEL HORIZONTE DE SUCESOS ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/FF66AA:Alas de energía violeta-negra con remolinos de acreción\nmagenta cruzando la membrana y anillos de fotones blancos\nardiendo en las puntas]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/FC0096:Cada pluma de plasma gira alrededor de su propio mini horizonte\n— la luz se dobla al pasar cerca de ti]"));
            tooltips.Add(new TooltipLine(Mod, "W5", "[c/FFD700:Vuelo end-game: 200 ticks · velocidad 9.5 · aceleración ×3]"));
        }
    }

    /// <summary>
    /// ALAS DEL ANILLO DE FOTONES: alas de luz dorada hechas de aros de
    /// órbita concéntricos con fotones corriendo por ellos.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class PhotonRingWings : AethonWingsItem
    {
        public override int FlyTime => 200;
        public override float FlySpeed => 9f;
        public override float FlyAccel => 3.2f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/FFC0CB:═══ ALAS DEL ANILLO DE FOTONES ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/FFD9EC:La luz que escapó del horizonte: alas doradas de aros de órbita\nconcéntricos con fotones corriendo por ellos y sus estelas]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/FF9AD5:Cada aleteo acelera los fotones del aro mayor — de dentro\nhacia afuera, como un destello de sincrotón]"));
            tooltips.Add(new TooltipLine(Mod, "W5", "[c/FFD700:Vuelo end-game: 200 ticks · velocidad 9 · aceleración ×3.2 (récord)]"));
        }
    }

    /// <summary>
    /// ALAS DE MARIPOSA CÓSMICA: la silueta real de una ninfa gigante —
    /// membrana de nebulosa púrpura-azul con campos de estrellas, venas
    /// turquesa y borde festoneado. Con FLOTADO.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class CosmicButterflyWings : AethonWingsItem
    {
        public override int FlyTime => 190;
        public override float FlySpeed => 9.5f;
        public override float FlyAccel => 2.8f;
        public override bool HasHover => true;
        public override float HoverSpeed => 9.5f;
        public override float HoverAccel => 3f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/FF7BAC:═══ ALAS DE MARIPOSA CÓSMICA ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/FF9AC8:Membrana de nebulosa púrpura-azul tachonada de campos de\nestrellas, venas de luz turquesa y borde festoneado con ojo\nde ala de ninfa junto a la punta]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/FFC06A:El golpe de vuelo de una mariposa REAL: bajada rápida y potente,\nsubida lenta — las alas casi se aplauden sobre tu espalda]"));
            tooltips.Add(new TooltipLine(Mod, "W5", "[c/FFD700:Vuelo end-game: 190 ticks · velocidad 9.5 · FLOTADO de mariposa]"));
        }
    }

    /// <summary>
    /// ALAS DE HADA DE POLVO ESTELAR: cuatro lóbulos de membrana dorada
    /// translúcida con el borde ámbar ardiendo y polvo estelar brillando.
    /// Con FLOTADO ágil.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class StardustFairyWings : AethonWingsItem
    {
        public override int FlyTime => 180;
        public override float FlySpeed => 10f;
        public override float FlyAccel => 3f;
        public override bool HasHover => true;
        public override float HoverSpeed => 8f;
        public override float HoverAccel => 3.4f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/FFD66E:═══ ALAS DE HADA DE POLVO ESTELAR ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/FFE3A0:Cuatro lóbulos puntiagudos de membrana dorada translúcida con\nel borde ámbar ardiendo y polvo estelar titilando encima]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/FFF3CE:Aleteo de colibrí: vibraciones casi 10 veces por segundo que\nnunca cesan del todo — ni parada sigues brillando]"));
            tooltips.Add(new TooltipLine(Mod, "W5", "[c/FFD700:Vuelo end-game: 180 ticks · velocidad 10 · flotado ágil ×3.4]"));
        }
    }

    /// <summary>
    /// ALAS DE CORONA SOLAR: alas de fuego solar — plasma blanco
    /// incandescente en la base que se arquea dorado-naranja-rojo con
    /// lazos de prominencia y llamaradas en las puntas.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class SolarCoronaWings : AethonWingsItem
    {
        public override int FlyTime => 200;
        public override float FlySpeed => 9.5f;
        public override float FlyAccel => 2.7f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/FF8C1A:═══ ALAS DE CORONA SOLAR ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/FFB347:Lazos de prominencia anclados a dos manchas solares en tu espalda:\nblanco incandescente en la base → dorado → naranja → rojo braza]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/FFD27A:Las puntas PARPADEAN en llamaradas y los lazos se estiran\ncon cada golpe de empuje (como las prominencias reales)]"));
            tooltips.Add(new TooltipLine(Mod, "W5", "[c/FFD700:Vuelo end-game: 200 ticks · velocidad 9.5 · aceleración ×2.7]"));
        }
    }

    /// <summary>
    /// ALAS DE NEBULOSA VIVA: nubes de gas rosa-magenta-turquesa con
    /// estrellas recién nacidas brillando dentro y filamentos
    /// serpenteando entre los blobs.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class LivingNebulaWings : AethonWingsItem
    {
        public override int FlyTime => 190;
        public override float FlySpeed => 9f;
        public override float FlyAccel => 3f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/AA46C8:═══ ALAS DE NEBULOSA VIVA ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/C86ADF:Nubes de gas rosa-magenta-turquesa en deriva turbulenta con\nfilamentos brillantes serpenteando entre ellas]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/E89BF0:Estrellas recién nacidas titilan dentro del gas, con cruces\nde difracción en las más brillantes]"));
            tooltips.Add(new TooltipLine(Mod, "W5", "[c/FFD700:Vuelo end-game: 190 ticks · velocidad 9 · aceleración ×3]"));
        }
    }

    /// <summary>
    /// ALAS DE ECLIPSE TOTAL: alas negras de plumas sólidas con el anillo
    /// cromosférico blanco-dorado EXACTO en el borde — la luna negra
    /// tapando el sol.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class TotalEclipseWings : AethonWingsItem
    {
        public override int FlyTime => 200;
        public override float FlySpeed => 9f;
        public override float FlyAccel => 2.9f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/E8E0F0:═══ ALAS DE ECLIPSE TOTAL ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/C8BCDD:Plumas negras sólidas con el anillo cromosférico blanco-dorado\nexactamente en el borde: la luna negra tapando el sol]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/FFF0F5:Un halo de corona te sigue allá donde vueles — la penumbra\nde un eclipse total en movimiento]"));
            tooltips.Add(new TooltipLine(Mod, "W5", "[c/FFD700:Vuelo end-game: 200 ticks · velocidad 9 · aceleración ×2.9]"));
        }
    }

    /// <summary>
    /// ALAS DE COMETA CARMESÍ: alas gemelas rojo-negras afiladas con
    /// núcleo blanco incandescente y colas de cometa carmesí
    /// arrastrándose con vetas de plasma y brasas.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class CrimsonCometWings : AethonWingsItem
    {
        public override int FlyTime => 190;
        public override float FlySpeed => 10.5f;
        public override float FlyAccel => 2.6f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/FF5A3C:═══ ALAS DE COMETA CARMESÍ ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/FF8A5E:Dos cometas gemelos en tu espalda: alas afiladas rojo-negras con\nnúcleo blanco incandescente y cola iónica ondeante carmesí]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/FFB08E:Cuanta más prisa llevas, más se BARRE la cola hacia atrás\ny más se estira (la personalidad del cometa)]"));
            tooltips.Add(new TooltipLine(Mod, "W5", "[c/FFD700:Vuelo end-game: 190 ticks · velocidad 10.5 (récord) · aceleración ×2.6]"));
        }
    }
}
