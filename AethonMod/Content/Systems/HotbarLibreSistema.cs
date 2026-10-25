using System;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  HOTBARLIBRESISTEMA — v6.52.2 — LA CURA DEFINITIVA DEL HOTBAR.
    //
    //  LA LETRA DEL USUARIO (tercera vez — la .51.12 retiró la correa del
    //  reuseDelay y NO bastó): «Bug del hotbar todavia esta ahi, no puedo
    //  seleccionar nada con el cursor en los accesos rapidos del
    //  inventario».
    //
    //  LA CAUSA RAÍZ VERDADERA (verificada al IL con Cecil):
    //  ItemSlot.LeftClick y RightClick REBOTAN todo click mientras
    //  player.itemAnimation ≠ 0 || player.itemTime ≠ 0:
    //
    //      ldloc.0                    // player
    //      ldfld Player::itemAnimation
    //      brtrue  → ret              // ← el rebote
    //      ldloc.0                    // player
    //      ldfld Player::itemTime
    //      brfalse → sigue            // si itemTime == 0 continúa…
    //      ret                        // ← …si no, OTRO rebote
    //
    //  Con el Eterno (autoReuse, useTime 22→7) cada disparo mantiene la
    //  animación viva 7–22 ticks — y MANTENIENDO el disparo, SIEMPRE:
    //  la barra entera queda MUERTA mientras se pelea. El guard es UX
    //  puro de vanilla (evitar mover items a mitad de uso) — y mover el
    //  item en uso ya es posible de facto en vanilla (la RUEDA del
    //  ratón cambia el slot activo EN PLENA animación): esta cura solo
    //  extiende esa misma libertad al CURSOR.
    //
    //  LA TÉCNICA: IL hook (el patrón de la casa — el mismo MonoMod de
    //  los 5 detours existentes) que NEUTRALIZA los dos rebotes en el
    //  prólogo de LeftClick y el de RightClick: el player que ldloc.0
    //  dejó en la pila se consume con un Pop, y el ldfld + branch se
    //  remueven — el flujo cae directo al cuerpo del click. La pila
    //  queda limpia (verificado contra el volcado IL del método).
    // ======================================================================
    public class HotbarLibreSistema : ModSystem
    {
        public override void Load()
        {
            try
            {
                Terraria.UI.IL_ItemSlot.LeftClick_ItemArray_int_int += QuitarReboteIzquierdo;
                Terraria.UI.IL_ItemSlot.RightClick_ItemArray_int_int += QuitarReboteDerecho;
            }
            catch { }
        }

        public override void Unload()
        {
            // La disciplina de la casa: el detach jamás estorba la salida.
            try { Terraria.UI.IL_ItemSlot.LeftClick_ItemArray_int_int -= QuitarReboteIzquierdo; } catch { }
            try { Terraria.UI.IL_ItemSlot.RightClick_ItemArray_int_int -= QuitarReboteDerecho; } catch { }
        }

        /// <summary>LEFTCLICK: neutraliza los DOS rebotes (itemAnimation
        /// + itemTime) del prólogo. Pila verificada: ldloc.0+ldfld+branch
        /// es un trio autocontenido — un Pop + dos Remove la dejan sana.</summary>
        private static void QuitarReboteIzquierdo(ILContext il)
        {
            try
            {
                ILCursor c = new(il);

                // === 1) itemAnimation: [ldloc.0][ldfld][brtrue → ret] ===
                if (c.TryGotoNext(i => i.MatchLdfld<Player>("itemAnimation")))
                {
                    c.Emit(Mono.Cecil.Cil.OpCodes.Pop);   // consume el player de la pila
                    c.Remove();                           // mata el ldfld
                    c.Remove();                           // mata el brtrue (el salto al ret)
                }

                // === 2) itemTime: [ldloc.0][ldfld][brfalse → sigue][ret] ===
                if (c.TryGotoNext(i => i.MatchLdfld<Player>("itemTime")))
                {
                    c.Emit(Mono.Cecil.Cil.OpCodes.Pop);   // consume el player
                    c.Remove();                           // mata el ldfld
                    c.Remove();                           // mata el brfalse
                    // El cursor quedó SOBRE el ret del rebote: saltarlo.
                    if (c.Next != null)
                        c.Emit(Mono.Cecil.Cil.OpCodes.Br, c.Next);
                }
            }
            catch { }
        }

        /// <summary>RIGHTCLICK: neutraliza su único rebote. Patrón IL
        /// (verificado al volcado): [ldloc.0][ldfld itemAnimation]
        /// [ldc.i4.0][ble.s → sigue][ret] — tres Remove y un salto sobre
        /// el ret.</summary>
        private static void QuitarReboteDerecho(ILContext il)
        {
            try
            {
                ILCursor c = new(il);
                if (c.TryGotoNext(i => i.MatchLdfld<Player>("itemAnimation")))
                {
                    c.Emit(Mono.Cecil.Cil.OpCodes.Pop);   // consume el player de la pila
                    c.Remove();                           // mata el ldfld
                    c.Remove();                           // mata el ldc.i4.0
                    c.Remove();                           // mata el ble.s
                    if (c.Next != null)
                        c.Emit(Mono.Cecil.Cil.OpCodes.Br, c.Next);   // salta el ret
                }
            }
            catch { }
        }
    }
}
