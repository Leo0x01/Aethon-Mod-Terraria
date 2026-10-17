using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Tiles
{
    // ======================================================================
    //  EL ALTAR DE LA CAÍDA (v6.50.128) — la letra del usuario: «el lugar
    //  de caida quede como un altar para añadir una nueva mecanica que
    //  dara mas vida al mod, la mecanica es la de comerse los item y
    //  mejorar el grimorio del eterno, para comer un item este debe
    //  corromperse primero y para eso se usara el lugar de caida».
    //
    // LA REFERENCIA (la imagen del usuario): el bloque DORADO con la
    // ESPIRAL negra en la base del pico central, del que emana un HAZ DE
    // LUZ VIOLETA vertical — el receptor del altar de invocación. Aquí es
    // la puerta de LA CORRUPCIÓN: lo que un jugador arroje sobre el altar
    // cae en el haz, se PUDRE en esencias corruptas… y el Grimorio del
    // Eterno se lo come para crecer (AltarDevoradorSistema).
    //
    // TÉCNICA: la PRIMERA ModTile del mod — 1×1 al estilo del altar
    // demoníaco de vanilla (NO sólida: se camina a través, los items
    // reposan en el pedestal de obsidiana de abajo), tileFrameImportant
    // (su marco es SUYO, no se funde con vecinos), luz propia que LATE
    // (NearbyEffects + Lighting.AddLight — el patrón de siempre; ESTE tML
    // 2026.08 no trae ModifyLight en ModTile), mapa dorado… e INTOCABLE:
    // CanKillTile le niega el pico (y las bombas, que pasan por KillTile)
    // y CanDrop no suelta NADA jamás.
    // ======================================================================
    public class AltarDeLaCaida : ModTile
    {
        public override string Texture => "AethonMod/Content/Tiles/AltarDeLaCaida";

        public override void SetStaticDefaults()
        {
            // EL CUERPO: como el altar demoníaco — un bloque suelto que NO
            // estorba el paso (los items caen y reposan en el pedestal).
            Main.tileSolid[Type] = false;
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileBlockLight[Type] = false;
            TileID.Sets.PreventsSandfall[Type] = true;

            // EL MAPA: la chispa dorada de la referencia.
            LocalizedText nombre = CreateMapEntryName();
            AddMapEntry(new Color(255, 214, 130), nombre);
        }

        /// <summary>La LUZ DEL ALTAR: dorada con un latido violeta — el
        /// corazón del haz que corrompe (late como el grimorio dormido).
        /// NearbyEffects corre en cada tile viva cerca del jugador: el
        /// hogar natural de las luces de las tiles del mod.</summary>
        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (!closer) return;
            float latido = 0.80f + 0.20f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.1f + i * 0.7f);
            Lighting.AddLight(i * 16 + 8, j * 16 + 8,
                0.52f * latido, 0.40f * latido, 0.62f * latido);
        }

        /// <summary>EL PICO REBOTADO (y las bombas también: las explosiones
        /// terminan en KillTile — el altar es del cráter, no del jugador).</summary>
        public override bool CanKillTile(int i, int j, ref bool blockDamaged)
        {
            blockDamaged = false;
            return false;
        }

        /// <summary>Nada que soltar: jamás muere.</summary>
        public override bool CanDrop(int i, int j) => false;
    }
}
