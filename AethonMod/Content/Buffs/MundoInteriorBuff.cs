using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AethonMod.Content.Buffs
{
    // ======================================================================
    //  MUNDOINTERIORBUFF — v6.182 — LA PUERTA VIVA HECHA ICONO.
    //
    //  LA LETRA (la sesión del botón que murió): «siento que el boton de
    //  ir al mundo interior esta ocupando espacio innecesario en la zona
    //  de consagracion de la ventana del altar, creo que lo mejor seria
    //  crear un icono tipo buff que represente al mundo interior, el icono
    //  es simple debe representar un paisaje oscuro y brumoso, el paisaje
    //  seria completamente plano sin nada a la distancia solo bruma y
    //  tentaculos […] sera como un buff que se activa con las 1000
    //  esencias del eterno en el altar y quedara permanentemente fijado en
    //  los buff, el buff se llamara Mundo Interior y no tendra
    //  descripcion solo dira Mundo Interior».
    //
    //  EL ICONO: el paisaje plano y brumoso con sus tentáculos
    //  (MundoInteriorBuff.png, 32×32 — tools/icono_mundo_interior_v6182.py,
    //  las herramientas de la casa).
    //  EL NOMBRE: «Mundo Interior» — SIN descripción (la ventana del buff
    //  SOLO dice el nombre: Description => LocalizedText.Empty — sin la
    //  clave muerta colgando del hjson).
    //  LA VIDA: PERMANENTE — nace con las mil esencias pagadas en el
    //  altar (SubmundoGrimorio.PostUpdateWorld lo refresca cada tick
    //  mientras PuertaAbierta viva; el cerrojo PuertaViva de la v6.179
    //  JAMÁS se apaga) y buffNoTimeDisplay le apaga la cuenta.
    //  EL CLIC DERECHO: EL VIAJE — el mismo trago del botón que murió
    //  (Main.DrawBuffIcon → BuffLoader.RightClick →
    //  AltarOfrendasUI.ViajarAlInterior). Devuelve false: TryRemovingBuff
    //  JAMÁS corre — la puerta NO se apaga con el clic.
    // ======================================================================
    public class MundoInteriorBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;              // es un BUFF (la puerta viva)
            Main.buffNoTimeDisplay[Type] = true;     // permanente: sin cuenta ruidosa
            Main.buffNoSave[Type] = true;            // nace del estado del mundo, no del guardado
        }

        /// <summary>SIN DESCRIPCIÓN (la letra: «no tendra descripcion
        /// solo dira Mundo Interior»): la ventana del buff muestra SOLO el
        /// nombre.</summary>
        public override LocalizedText Description => LocalizedText.Empty;

        /// <summary>EL CLIC DERECHO EN EL ICONO: EL VIAJE al mundo
        /// interior (desde cualquier parte — ya no hace falta el altar).
        /// Devuelve false: el icono NO se apaga con el clic — la puerta
        /// vive con las mil esencias, para siempre.</summary>
        public override bool RightClick(int buffIndex)
        {
            global::AethonMod.Content.UI.AltarOfrendasUI.ViajarAlInterior();
            return false;   // jamás removible: la puerta está viva
        }
    }
}
