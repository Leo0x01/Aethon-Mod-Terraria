using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Items.Wings
{
    /// <summary>
    /// VFXWingsItem — v6.08 — LA BASE DE LAS 8 ALAS DE LUZ.
    ///
    /// TODAS las alas del mod son ahora de luz 100% procedural (la técnica
    /// de las coronas, como pidió el usuario): la textura de equipo es un
    /// PNG EN BLANCO (Calamity-style) y TODO el dibujado lo hace la
    /// biblioteca VFX desde VFXWingsDrawLayer, con la animación por
    /// muelles/personalidad de WingAnimPlayer.
    ///
    /// Cada subclase define SUS estadísticas end-game (nivel Solar Wings
    /// 180/9/2.5 o superior, con hover en las de hada y mariposa), su
    /// estilo de render (WingStyles) y sus tooltips de color.
    /// </summary>
    public abstract class VFXWingsItem : ModItem
    {
        /// <summary>El estilo de render/animação de esta ala (WingStyles).</summary>
        public abstract int Style { get; }

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
    //  LAS 8 ALAS DE LUZ — todas end-game, todas técnica coronas
    // =====================================================================

    /// <summary>
    /// ALAS DEL HORIZONTE DE SUCESOS (v6.08 REDISEÑADAS): dos mini agujeros
    /// negros en los omóplatos con RASTROS DE ACRECIÓN alargados barriendo
    /// el aire (la nueva silueta v6.08: núcleo compacto + anillo de fotones
    /// abrazándolo + cinta fina de plasma con Doppler δ³ y cuentas de
    /// materia orbitando).
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class EventHorizonWings : VFXWingsItem
    {
        public override int Style => WingStyles.EventHorizon;
        public override int FlyTime => 200;
        public override float FlySpeed => 9.5f;
        public override float FlyAccel => 3f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/FF1738:═══ ALAS DEL HORIZONTE DE SUCESOS ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/FF66AA:Rastros de acreción ALARGADOS que barren el aire desde dos mini\nagujeros negros en tu espalda: núcleo compacto, anillo de fotones\nabrazándolo y plasma con Doppler relativista (δ³)]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/FC0096:Cuentas de materia orbitando el horizonte y ecos de anillo de Einstein\nen cada punta]"));
            tooltips.Add(new TooltipLine(Mod, "W4", "[c/78788C:Ala de luz 100% procedural (técnica de las coronas):\nse pliega en reposo · cinta orientada por tangente · sweep aerodinámico]"));
            tooltips.Add(new TooltipLine(Mod, "W5", "[c/FFD700:Vuelo end-game: 200 ticks · velocidad 9.5 · aceleración ×3]"));
        }
    }

    /// <summary>
    /// ALAS DEL ANILLO DE FOTONES (v6.08 REDISEÑADAS): la luz que ESCAPA —
    /// tres aros de órbita elípticos por lado en abanico con fotones
    /// corriendo por ellos y estelas, y un pulso que recorre el aro mayor
    /// con cada golpe de aleteo.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class PhotonRingWings : VFXWingsItem
    {
        public override int Style => WingStyles.PhotonRing;
        public override int FlyTime => 200;
        public override float FlySpeed => 9f;
        public override float FlyAccel => 3.2f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/FFC0CB:═══ ALAS DEL ANILLO DE FOTONES ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/FFD9EC:La luz que escapa del horizonte: tres aros de órbita en abanico\ncon fotones corriendo por ellos y sus estelas]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/FF9AD5:Cada golpe de aleteo lanza un PULSO que recorre el aro mayor\nde dentro afuera]"));
            tooltips.Add(new TooltipLine(Mod, "W4", "[c/78788C:Ala de luz 100% procedural (técnica de las coronas):\nórbitas que respiran · aceleración de fotones al volar]"));
            tooltips.Add(new TooltipLine(Mod, "W5", "[c/FFD700:Vuelo end-game: 200 ticks · velocidad 9 · aceleración ×3.2 (récord)]"));
        }
    }

    /// <summary>
    /// ALAS DE MARIPOSA CÓSMICA (v6.08 NUEVAS): la silueta real de una
    /// mariposa — lobo superior con su ojo de ala y lobo inferior caído,
    /// cada uno con su fase de aleteo distinta — pintada con membrana
    /// translúcida, venas de luz, borde dorado festoneado y golpe de vuelo
    /// ASIMÉTRICO de mariposa de verdad (bajada rápida, subida lenta).
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class CosmicButterflyWings : VFXWingsItem
    {
        public override int Style => WingStyles.Butterfly;
        public override int FlyTime => 190;
        public override float FlySpeed => 9.5f;
        public override float FlyAccel => 2.8f;
        public override bool HasHover => true;
        public override float HoverSpeed => 9.5f;
        public override float HoverAccel => 3f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/FF7BAC:═══ ALAS DE MARIPOSA CÓSMICA ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/FF9AC8:Membrana de magenta translúcido con venas de luz, borde dorado\nfestoneado y el ojo de ala de una nymphálida junto a la punta]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/FFC06A:El golpe de vuelo de una mariposa REAL: bajada rápida y potente,\nsubida lenta — las alas casi se aplauden sobre tu espalda]"));
            tooltips.Add(new TooltipLine(Mod, "W4", "[c/78788C:Ala de luz 100% procedural (técnica de las coronas):\nlobos con fase independiente · vuelo estacionario de mariposa]"));
            tooltips.Add(new TooltipLine(Mod, "W5", "[c/FFD700:Vuelo end-game: 190 ticks · velocidad 9.5 · FLOTADO de mariposa]"));
        }
    }

    /// <summary>
    /// ALAS DE HADA DE POLVO ESTELAR (v6.08 NUEVAS): cuatro lóbulos
    /// alargados y puntiagudos de membrana dorada con el borde ámbar
    /// ardiendo, 7 chispas de polvo estelar titilando sobre ellas y el
    /// aleteo de COLIBRÍ: vibración casi 10 veces por segundo que NUNCA
    /// cesa del todo.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class StardustFairyWings : VFXWingsItem
    {
        public override int Style => WingStyles.Fairy;
        public override int FlyTime => 180;
        public override float FlySpeed => 10f;
        public override float FlyAccel => 3f;
        public override bool HasHover => true;
        public override float HoverSpeed => 8f;
        public override float HoverAccel => 3.4f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/FFD66E:═══ ALAS DE HADA DE POLVO ESTELAR ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/FFE3A0:Cuatro lóbulos puntiagudos de membrana dorada con el borde ámbar\nardiendo y polvo estelar titilando sobre las alas]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/FFF3CE:Aleteo de colibrí: ~10 vibraciones por segundo que nunca cesan\ndel todo — ni parada sigues brillando]"));
            tooltips.Add(new TooltipLine(Mod, "W4", "[c/78788C:Ala de luz 100% procedural (técnica de las coronas):\nvibración de colibrí · FLOTADO de hada nerviosa]"));
            tooltips.Add(new TooltipLine(Mod, "W5", "[c/FFD700:Vuelo end-game: 180 ticks · velocidad 10 · flotado ágil ×3.4]"));
        }
    }

    /// <summary>
    /// ALAS DE CORONA SOLAR (v6.08 NUEVAS): lazos de PROMINENCIA — plasma
    /// que nace blanco incandescente en la fotosfera, se arquea hacia
    /// fuera enfriándose a dorado/naranja/rojo y vuelve a caer, con las
    /// puntas parpadeando en llamaradas y estirándose al empujar.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class SolarCoronaWings : VFXWingsItem
    {
        public override int Style => WingStyles.SolarCorona;
        public override int FlyTime => 200;
        public override float FlySpeed => 9.5f;
        public override float FlyAccel => 2.7f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/FF8C1A:═══ ALAS DE CORONA SOLAR ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/FFB347:Lazos de prominencia anclados a dos manchas solares en tu espalda:\nblanco incandescente en la base → dorado → naranja → rojo braza]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/FFD27A:Las puntas PARPADEAN en llamaradas y los lazos se estiran\ncon cada golpe de empuje (como las prominencias reales)]"));
            tooltips.Add(new TooltipLine(Mod, "W4", "[c/78788C:Ala de luz 100% procedural (técnica de las coronas):\nlazos que ondean al viento solar · neblina de corona]"));
            tooltips.Add(new TooltipLine(Mod, "W5", "[c/FFD700:Vuelo end-game: 200 ticks · velocidad 9.5 · aceleración ×2.7]"));
        }
    }

    /// <summary>
    /// ALAS DE NEBULOSA VIVA (v6.08 NUEVAS): seis blobs de gas MAGENTA que
    /// derivan cada uno con su propia turbulencia, filamentos brillantes
    /// serpenteando entre ellos y estrellas recién nacidas titilando
    /// dentro — el ala entera RESPIRA en lugar de aletear.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class LivingNebulaWings : VFXWingsItem
    {
        public override int Style => WingStyles.Nebula;
        public override int FlyTime => 190;
        public override float FlySpeed => 9f;
        public override float FlyAccel => 3f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/AA46C8:═══ ALAS DE NEBULOSA VIVA ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/C86ADF:Seis blobs de gas magenta en deriva turbulenta con filamentos\nbrillantes serpenteando entre ellos]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/E89BF0:Estrellas recién nacidas titilan dentro del gas, con cruces\nde difracción en las más brillantes]"));
            tooltips.Add(new TooltipLine(Mod, "W4", "[c/78788C:Ala de luz 100% procedural (técnica de las coronas):\nla nube RESPIRA en lugar de aletear · nunca del todo quieta]"));
            tooltips.Add(new TooltipLine(Mod, "W5", "[c/FFD700:Vuelo end-game: 190 ticks · velocidad 9 · aceleración ×3]"));
        }
    }

    /// <summary>
    /// ALAS DE ECLIPSE TOTAL (v6.08 NUEVAS): la luna negra tapando el sol —
    /// discos negros sólidos con el anillo cromosférico EXACTO en el borde,
    /// rayos de corona de longitudes desiguales ondeando al viento solar y
    /// una llamarada rosa asomando por el limbo. Vuelo majestuoso y lento.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class TotalEclipseWings : VFXWingsItem
    {
        public override int Style => WingStyles.Eclipse;
        public override int FlyTime => 200;
        public override float FlySpeed => 9f;
        public override float FlyAccel => 2.9f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/E8E0F0:═══ ALAS DE ECLIPSE TOTAL ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/C8BCDD:La luna negra tapando el sol: discos sólidos con el anillo\ncromosférico exactamente en el borde]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/FFF0F5:Rayos de corona DESIGUALES ondeando al viento solar y una\nllamarada rosa asomando por el limbo]"));
            tooltips.Add(new TooltipLine(Mod, "W4", "[c/78788C:Ala de luz 100% procedural (técnica de las coronas):\nvuelo majestuoso · la penumbra te sigue]"));
            tooltips.Add(new TooltipLine(Mod, "W5", "[c/FFD700:Vuelo end-game: 200 ticks · velocidad 9 · aceleración ×2.9]"));
        }
    }

    /// <summary>
    /// ALAS DE COMETA CARMESÍ (v6.08 NUEVAS): dos cometas gemelos anclados
    /// en los omóplatos con su núcleo blanco-dorado y la COLA IÓNICA
    /// arrastrándose atrás — larga, cónica, SIEMPRE ondeando, con vetas de
    /// plasma y motas de polvo. Cuanto más rápido vuelas, más se BARRRE la
    /// cola hacia atrás y se estira.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class CrimsonCometWings : VFXWingsItem
    {
        public override int Style => WingStyles.Comet;
        public override int FlyTime => 190;
        public override float FlySpeed => 10.5f;
        public override float FlyAccel => 2.6f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/FF5A3C:═══ ALAS DE COMETA CARMESÍ ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/FF8A5E:Dos cometas gemelos en tu espalda: núcleo blanco incandescente\ny cola iónica ondeante blanco → dorado → carmesí → braza]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/FFB08E:Cuanta más prisa llevas, más se BARRE la cola hacia atrás\ny más se estira (la personalidad del cometa)]"));
            tooltips.Add(new TooltipLine(Mod, "W4", "[c/78788C:Ala de luz 100% procedural (técnica de las coronas):\nonda viajera por la cola · vetas de plasma y motas de polvo]"));
            tooltips.Add(new TooltipLine(Mod, "W5", "[c/FFD700:Vuelo end-game: 190 ticks · velocidad 10.5 (récord) · aceleración ×2.6]"));
        }
    }
}
