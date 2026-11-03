using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  v6.89 — MASCOTAS Y LA TECLA B (la letra: «sobre la tecla b, deja
    //  la tecla como estaba, y que tanto la mascota de luz de aethon como
    //  el minigrimorio mascota, se puedan activar con la tecla b si es
    //  que estaban desactivados, pero solo si estan equipados»).
    //
    //  EL PROBLEMA DE LA v6.88: para que la tecla B no usara las mascotas
    //  DESDE EL FONDO DE LA BOLSA, la .88 les QUITÓ el Item.buffType —
    //  pero el buffType es también lo que las mete en la RANURA de
    //  mascota del equipo (ItemSlot caso 19: vanityPet/lightPet lo
    //  exigen): la cura mató al paciente (ya no se podían EQUIPAR).
    //
    //  LA CURA DE VERDAD (la tecla se queda COMO ERA — la de vanilla):
    //  UN DETOUR de Player.QuickBuff que filtra a NUESTRAS dos mascotas
    //  del escaneo de vanilla (vanilla NO distingue mascotas de pociones:
    //  usaría cualquiera que encuentre en la bolsa) y las activa ÉL
    //  MISMO — SOLO si están EQUIPADAS (ranura de mascota o de mascota
    //  de luz), SOLO si su buff está apagado, y SOLO si no las tienes
    //  escondidas con el ojo del equipo (hideMisc: si las escondiste,
    //  tu decisión manda).
    // ======================================================================
    public class MascotasTeclaB : ModSystem
    {
        /// <summary>Las DOS mascotas de la casa: (tipo del ítem, buff).</summary>
        private static int[] _tipos = new int[0];
        private static int[] _buffs = new int[0];

        public override void Load()
        {
            On_Player.QuickBuff += DetourQuickBuff;
        }

        public override void Unload()
        {
            On_Player.QuickBuff -= DetourQuickBuff;
            _tipos = null;
            _buffs = null;
        }

        public override void PostSetupContent()
        {
            _tipos = new int[]
            {
                ModContent.ItemType<global::AethonMod.Content.Items.Llamados.AethonMenorItem>(),
                ModContent.ItemType<global::AethonMod.Content.Items.Llamados.GrimorioMenorItem>(),
            };
            _buffs = new int[]
            {
                ModContent.BuffType<global::AethonMod.Content.Buffs.AethonMenorBuff>(),
                ModContent.BuffType<global::AethonMod.Content.Buffs.GrimorioMenorBuff>(),
            };
        }

        /// <summary>¿Es ESTA una de las mascotas de la casa?</summary>
        private static bool EsMascotaNuestra(int tipo) =>
            _tipos != null && Array.IndexOf(_tipos, tipo) >= 0;

        // ================================================================
        //  EL DETOUR: (1) esconde el buffType de nuestras mascotas del
        //  escaneo de vanilla → (2) deja correr el QuickBuff DE SIEMPRE
        //  (la tecla como estaba: pociones y comida funcionan igual) →
        //  (3) devuelve los buffTypes → (4) activa las mascotas
        //  EQUIPADAS que sigan apagadas. Vanilla corre UNA sola vez.
        // ================================================================
        private static void DetourQuickBuff(On_Player.orig_QuickBuff orig, Player self)
        {
            if (_tipos == null || _tipos.Length == 0 || self == null || Main.gameMenu || self != Main.LocalPlayer)
            {
                orig(self);
                return;
            }

            // (1) LOS BUFFTYPES FUERA DEL ESCANEO: vanilla usa cualquier
            //     item con buffType > 0 (POCIONES Y MASCOTAS por igual):
            //     nuestras dos mascotas viajan con el buffType a 0 durante
            //     el escaneo — la tecla B JAMÁS las toca desde el fondo de
            //     la bolsa.
            List<Item> tocadas = new List<Item>();
            List<int> tiposGuardados = new List<int>();
            try
            {
                for (int k = 0; k < 58; k++)
                {
                    Item it = self.inventory[k];
                    if (it == null || it.IsAir || !EsMascotaNuestra(it.type)) continue;
                    tocadas.Add(it);
                    tiposGuardados.Add(it.buffType);
                    it.buffType = 0;   // invisible para vanilla
                }
            }
            catch { }

            // (2) LA TECLA COMO ERA: pociones, comida, caldo — TODO igual
            //     (nuestras mascotas ya no están en el menú de vanilla).
            orig(self);

            // (3)+(4) LA DEVOLUCIÓN Y LA ACTIVACIÓN.
            try
            {
                // (3) LOS BUFFTYPES DE VUELTA (los ítems de la bolsa
                //     siguen siendo lo que son).
                for (int k = 0; k < tocadas.Count; k++)
                    tocadas[k].buffType = tiposGuardados[k];

                // (4) LAS MASCOTAS: SOLO EQUIPADAS (v6.91 — la letra:
                //     «para las mascotas debe funcionar solo cuando estan
                //     equipadas, ademas la tecla B tiene su propia funcion
                //     asi que eso no se debe tocar, debes mantener su
                //     funcion normal» — la ruta de LA BOLSA de la v6.90
                //     MUERE): SOLO la ranura de mascota (miscEquips[0]) o
                //     la de luz (miscEquips[1]), con el ojo del equipo
                //     manda (hideMisc: tu decisión) y SOLO si su buff
                //     está apagado.
                for (int m = 0; m < _tipos.Length; m++)
                {
                    for (int slot = 0; slot < 2; slot++)
                    {
                        Item eq = self.miscEquips[slot];
                        if (eq == null || eq.IsAir || eq.type != _tipos[m]) continue;
                        if (self.hideMisc[slot]) continue;       // escondida: TU decisión
                        if (self.HasBuff(_buffs[m])) continue;   // ya vive
                        // LA ACTIVACIÓN: el buff de 3600 (la mascota lo
                        // sostiene después — el patrón canónico de la
                        // casa: BuffHandle re-engendra, timeLeft = 2).
                        self.AddBuff(_buffs[m], 3600, true);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, self.position);
                    }
                }
            }
            catch { }
        }
    }
}
