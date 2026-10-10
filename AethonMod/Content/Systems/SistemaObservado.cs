using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// SISTEMA OBSERVADO — v6.50.98 — QUIEN CUENTA LOS 5 GOLPES.
    /// El sistema global de stacks del Grimorio Sellado, tal como lo
    /// escribió el usuario (namespace de la casa, no TuMod) y con sus
    /// dos errores de fondo corregidos:
    ///
    /// · GoldFlare → GoldFlame (no existe — lección .96 otra vez).
    /// · La limpieza usaba una List que admitía duplicados (timer
    ///   expirado + NPC muerto añadía DOS veces la misma key): ahora es
    ///   HashSet — Remove duplicado era inofensivo, pero el HashSet lo
    ///   dice de una vez.
    ///
    /// EL FLUJO (la letra del usuario): FragmentoDeAethon.OnHitNPC →
    /// AddStack → a 5 stacks el NPC es «Conocido» → ModifyHitNPC del
    /// fragmento le regala CRÍTICO GARANTIZADO + 50% daño. Los stacks
    /// viven 5 segundos (300 ticks) y se limpian solos: timer expirado,
    /// NPC muerto o mundo cerrado (ClearWorld).
    ///
    /// ALCANCE (arma de PRUEBA): el estado vive en la sesión del mundo y
    /// por índice whoAmI — pensado para el juego del probador (SP). En
    /// MP cada lado lleva su cuenta como cualquier ModSystem sin red.
    /// </summary>
    public class SistemaObservado : ModSystem
    {
        // whoAmI del NPC → stacks y su reloj
        private readonly Dictionary<int, int> stacks = new Dictionary<int, int>();
        private readonly Dictionary<int, int> timers = new Dictionary<int, int>();

        /// <summary>Añadir un stack al NPC (lo llama OnHitNPC del fragmento).</summary>
        public void AddStack(int npcIndex)
        {
            if (npcIndex < 0 || npcIndex >= Main.npc.Length || !Main.npc[npcIndex].active)
                return;                       // la mira solo cae sobre los vivos

            if (!stacks.ContainsKey(npcIndex))
                stacks[npcIndex] = 0;
            stacks[npcIndex]++;
            timers[npcIndex] = 300;           // 5 segundos

            // si llega a 5, el NPC queda «Conocido»: el destello dorado
            if (stacks[npcIndex] >= 5)
            {
                for (int i = 0; i < 12; i++)
                {
                    Dust dust = Dust.NewDustPerfect(
                        Main.npc[npcIndex].Center,
                        DustID.GoldFlame,     // GoldFlare NO existe — la casa
                        Vector2.UnitX.RotateRandom(MathHelper.TwoPi) * 3f,
                        220, new Color(255, 218, 94), 1.6f
                    );
                    dust.noGravity = true;
                }
            }
        }

        /// <summary>Obtener cuántos stacks tiene un NPC (0 si no tiene).</summary>
        public int GetStacks(int npcIndex)
        {
            return stacks.ContainsKey(npcIndex) ? stacks[npcIndex] : 0;
        }

        /// <summary>Limpiar los stacks de un NPC (expira o muere).</summary>
        public void ClearStacks(int npcIndex)
        {
            stacks.Remove(npcIndex);
            timers.Remove(npcIndex);
        }

        /// <summary>El reloj de cada tick: los 5 segundos corren, los
        /// muertos se limpian y nadie mira para siempre.</summary>
        public override void PostUpdateWorld()
        {
            if (timers.Count == 0)
                return;

            HashSet<int> toRemove = new HashSet<int>();
            foreach (KeyValuePair<int, int> kvp in timers)
            {
                timers[kvp.Key]--;
                if (timers[kvp.Key] <= 0)
                    toRemove.Add(kvp.Key);    // el reloj se acabó

                if (kvp.Key < Main.npc.Length && !Main.npc[kvp.Key].active)
                    toRemove.Add(kvp.Key);    // el NPC ya no existe (el
                                               // HashSet come el duplicado)
            }

            if (toRemove.Count > 0)
            {
                foreach (int key in toRemove)
                {
                    stacks.Remove(key);
                    timers.Remove(key);
                }
            }
        }

        /// <summary>Al cerrar/cambiar el mundo: la mirada no cruza
        /// mundos — todo se limpia.</summary>
        public override void ClearWorld()
        {
            stacks.Clear();
            timers.Clear();
        }
    }
}
