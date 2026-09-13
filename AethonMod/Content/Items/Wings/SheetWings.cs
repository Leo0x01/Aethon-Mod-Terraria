using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Wings
{
    /// <summary>
    /// SolarNovaWings — v6.06 — ALAS DE NOVA SOLAR.
    ///
    /// Lenguadas de plasma de núcleo blanco-incandescente → dorado →
    /// naranja → rojo profundo (la paleta de las ondas de fuego del
    /// SunStaff). Spritesheet vanilla de 4 frames.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class SolarNovaWings : AethonWings
    {
        public override int FlyTime => 190;
        public override float FlySpeed => 9.5f;
        public override float FlyAccel => 2.6f;
        public override int FlightDust => DustID.GoldFlame;
        public override float FlightDustScale => 1.3f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/FF6600:═══ ALAS DE NOVA SOLAR ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/FFD700:Lenguadas de plasma vivo: núcleo blanco incandescente,\ndorado fundido, naranja y rojo profundo en las puntas]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/78788C:La misma paleta de las ondas de fuego del Báculo del Sol]"));
            tooltips.Add(new TooltipLine(Mod, "W4", "[c/FFD700:Vuelo end-game: 190 ticks · velocidad 9.5]"));
        }
    }

    /// <summary>
    /// QuantumPlasmaWings — v6.06 — ALAS DE PLASMA CUÁNTICO.
    ///
    /// Shards angulares cian facetados con destellos internos y bordes
    /// de brillo aditivo: tecnología alienígena hecha gema.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class QuantumPlasmaWings : AethonWings
    {
        public override int FlyTime => 180;
        public override float FlySpeed => 10f;     // la más rápida del set
        public override float FlyAccel => 2.4f;
        public override int FlightDust => DustID.BlueTorch;
        public override Color FlightDustColor => new Color(120, 235, 255);
        public override float FlightDustScale => 1.15f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/46C8F0:═══ ALAS DE PLASMA CUÁNTICO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/96F3FF:Shards de cristal cian facetado con plasma eléctrico dentro,\nfilos de brillo aditivo y destellos que recorren las facetas]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/78788C:Tecnología de un imperio que ya no existe]"));
            tooltips.Add(new TooltipLine(Mod, "W4", "[c/FFD700:Vuelo end-game: 180 ticks · velocidad 10 (récord del set)]"));
        }
    }

    /// <summary>
    /// EtherealVoidWings — v6.06 — ALAS DEL VACÍO ETÉREO.
    ///
    /// Membrana demoníaca púrpura-negra translúcida con dedos óseos,
    /// VENAS FUCSIA vivas (la sangre del vacío de la Reina) y estrellas
    /// internas.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class EtherealVoidWings : AethonWings
    {
        public override int FlyTime => 180;
        public override float FlySpeed => 9f;
        public override float FlyAccel => 2.5f;
        public override int FlightDust => DustID.PurpleTorch;
        public override float FlightDustScale => 1.25f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/FC0096:═══ ALAS DEL VACÍO ETÉREO ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/B080D0:Membrana púrpura translúcida sobre dedos de hueso antiguo,\ncruzada por VENAS FUCSIA vivas — la sangre del vacío de la Reina]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/78788C:Hay estrellas dentro de la membrana. No preguntes cómo]"));
            tooltips.Add(new TooltipLine(Mod, "W4", "[c/FFD700:Vuelo end-game: 180 ticks · velocidad 9]"));
        }
    }

    /// <summary>
    /// GlacialEtherWings — v6.06 — ALAS DE ÉTER GLACIAL.
    ///
    /// Plumas etéreas celestes con puntas de carámbano y brillo interior
    /// frío; las plumas altas casi se desvanecen en el aire.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class GlacialEtherWings : AethonWings
    {
        public override int FlyTime => 180;
        public override float FlySpeed => 8.5f;
        public override float FlyAccel => 2.8f;
        public override int FlightDust => DustID.BlueTorch;
        public override Color FlightDustColor => new Color(195, 235, 255);
        public override float FlightDustScale => 1.1f;

        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling,
            ref float ascentWhenRising, ref float maxCanAscendMultiplier,
            ref float maxAscentMultiplier, ref float constantAscend)
        {
            ascentWhenFalling = 0.70f;   // planeo largo y lento
            ascentWhenRising = 0.15f;
            maxCanAscendMultiplier = 1f;
            maxAscentMultiplier = 3f;
            constantAscend = 0.135f;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/7FD4F7:═══ ALAS DE ÉTER GLACIAL ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/C9EEFF:Plumas de éter celeste con puntas de carámbano\ny un brillo frío que late dentro de cada pluma]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/78788C:Cae tan despacio que el invierno te sigue]"));
            tooltips.Add(new TooltipLine(Mod, "W4", "[c/FFD700:Vuelo end-game: 180 ticks · planeo extra lento]"));
        }
    }

    /// <summary>
    /// GenesisFossilWings — v6.06 — ALAS FÓSILES DEL GÉNESIS.
    ///
    /// Hueso antiguo dorado con bandas de sedimento, vetas de ámbar y un
    /// shard cristalizado en el hombro: las alas de algo que voló cuando
    /// el mundo era nuevo.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class GenesisFossilWings : AethonWings
    {
        public override int FlyTime => 180;
        public override float FlySpeed => 9f;
        public override float FlyAccel => 2.5f;
        public override int FlightDust => DustID.GoldFlame;
        public override Color FlightDustColor => new Color(220, 185, 120);
        public override float FlightDustScale => 1.2f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/C9A86A:═══ ALAS FÓSILES DEL GÉNESIS ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/E8D9A8:Plumas de hueso fosilizado con vetas de ámbar y bandas de\nsedimento — el vuelo petrificado del primer amanecer del mundo]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/78788C:El Fragmento Génesis las reconoció. Eso debería preocuparte]"));
            tooltips.Add(new TooltipLine(Mod, "W4", "[c/FFD700:Vuelo end-game: 180 ticks · velocidad 9]"));
        }
    }

    /// <summary>
    /// NebulaPillarWings — v6.06 — ALAS DEL PILAR DE NEBULOSA.
    ///
    /// Cuerpo magenta translúcido con BORDE CIAN brillante y lóbulos
    /// redondeados: el look exacto del campo de fuerza del agujero negro
    /// (la burbuja estilo Nebula Pillar que el usuario validó en v5.93).
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class NebulaPillarWings : AethonWings
    {
        public override int FlyTime => 185;
        public override float FlySpeed => 9f;
        public override float FlyAccel => 2.6f;
        public override int FlightDust => DustID.PurpleTorch;
        public override Color FlightDustColor => new Color(230, 130, 255);
        public override float FlightDustScale => 1.2f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/B03BE0:═══ ALAS DEL PILAR DE NEBULOSA ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/7CF6FF:Cuerpo de nebulosa magenta translúcida con borde cian brillante\ny lóbulos que respiran — la burbuja del agujero hecha alas]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/78788C:La aberración vive en el borde, como debe ser]"));
            tooltips.Add(new TooltipLine(Mod, "W4", "[c/FFD700:Vuelo end-game: 185 ticks · velocidad 9]"));
        }
    }

    /// <summary>
    /// EclipseWings — v6.06 — ALAS DEL ECLIPSE.
    ///
    /// Dos discos de ECLIPSE negro con filo dorado en la espalda y rayos
    /// de corona blanco-oro en abanico que forman la silueta del ala, con
    /// su arco de limbo abrazando el conjunto.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class EclipseWings : AethonWings
    {
        public override int FlyTime => 190;
        public override float FlySpeed => 9.2f;
        public override float FlyAccel => 2.7f;
        public override int FlightDust => DustID.GoldFlame;
        public override Color FlightDustColor => new Color(255, 240, 200);
        public override float FlightDustScale => 1.15f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/FFE066:═══ ALAS DEL ECLIPSE ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/FFF3C4:Dos eclipses portátiles en tu espalda: discos negros de filo dorado\nrodeados de rayos de corona blanco-oro en abanico]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/78788C:El sol del Báculo te presta su corona. De día, nadie la nota]"));
            tooltips.Add(new TooltipLine(Mod, "W4", "[c/FFD700:Vuelo end-game: 190 ticks · velocidad 9.2]"));
        }
    }

    /// <summary>
    /// SupernovaWings — v6.06 — ALAS DE SUPERNOVA.
    ///
    /// Plumas de choque blancas-rosadas-violeta con ANILLOS DE ONDA
    /// expansiva cruzando cada ala: una supernova atada a tu espalda.
    /// </summary>
    [AutoloadEquip(EquipType.Wings)]
    public class SupernovaWings : AethonWings
    {
        public override int FlyTime => 185;
        public override float FlySpeed => 9.4f;
        public override float FlyAccel => 2.5f;
        public override int FlightDust => DustID.RainbowRod;
        public override Color FlightDustColor => new Color(255, 170, 230);
        public override float FlightDustScale => 1.25f;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "W1", "[c/FF6EB0:═══ ALAS DE SUPERNOVA ═══]"));
            tooltips.Add(new TooltipLine(Mod, "W2", "[c/B4F0FF:Plumas de choque de blanco-calor a violeta con anillos de onda\nexpansiva cruzando cada ala — una supernova atada a tu espalda]"));
            tooltips.Add(new TooltipLine(Mod, "W3", "[c/78788C:El frente de choque late. La estrella ya no está; las alas sí]"));
            tooltips.Add(new TooltipLine(Mod, "W4", "[c/FFD700:Vuelo end-game: 185 ticks · velocidad 9.4]"));
        }
    }
}
