using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AethonMod.Content.Players
{
    // ======================================================================
    //  SkillTreePlayer — v6.86 — EL ÁRBOL DE HABILIDADES DEL PORTADOR.
    //
    //  LA LETRA: «recuerdas cuando estabamos intentando crear un arbol
    //  de habilidades y no lo logramos, bueno el mod Umbra y el mod
    //  Path of Terraria tienen el arbol de habilidades en una ventana
    //  funcional justo como lo necesitabamos, asi que investigalo y
    //  crea un item que la use».
    //
    //  EL INFORME R6 (r6_arbol_habilidades.md, repos clonados): el
    //  modelo a copiar es Path of Terraria — TODO POR JUGADOR
    //  (PassiveTreePlayer.SaveData: tag[id] = nivel), los efectos en
    //  PostUpdateEquips (BuffPlayer cada tick), y el refund con
    //  conectividad (FullyLinkedWithout — el árbol jamás se parte).
    //
    //  LA ECONOMÍA DE LA CASA: los puntos nacen DEL DESPERTAR —
    //  3 + despertar/5 (el árbol del portador crece con su libro: en
    //  el despertar 100 caben TODOS los 23 puntos del árbol).
    // ======================================================================
    public class SkillTreePlayer : ModPlayer
    {
        /// <summary>LOS NIVELES de los nodos (por índice — el orden de
        /// ArbolHabilidadesUI.LosNodos; el índice 0 es el ANCLA, siempre
        /// 1 y gratis).</summary>
        public int[] Niveles = new int[ArbolUtil.TOTAL_NODOS];

        /// <summary>¿Cargó ya? (para no pisar con defaults a mitad de
        /// sesión).</summary>
        public bool Cargado = false;

        // ================================================================
        //  LA PERSISTENCIA (por JUGADOR — viaja con el .plr a todos los
        //  mundos, el modelo de PoT).
        // ================================================================
        public override void SaveData(TagCompound tag)
        {
            try
            {
                var lista = new List<int>();
                for (int i = 0; i < Niveles.Length && i < ArbolUtil.TOTAL_NODOS; i++)
                    lista.Add(Niveles[i]);
                tag["arbolNiveles"] = lista;
            }
            catch { }
        }

        public override void LoadData(TagCompound tag)
        {
            try
            {
                var lista = tag.GetList<int>("arbolNiveles");
                Niveles = new int[ArbolUtil.TOTAL_NODOS];
                for (int i = 0; i < lista.Count && i < ArbolUtil.TOTAL_NODOS; i++)
                    Niveles[i] = System.Math.Max(0, System.Math.Min(9, lista[i]));
                // EL ANCLA SIEMPRE VIVA (gratis, max 1).
                if (Niveles.Length > 0) Niveles[0] = 1;
                Cargado = true;
            }
            catch { Niveles = new int[ArbolUtil.TOTAL_NODOS]; if (Niveles.Length > 0) Niveles[0] = 1; }
        }

        // ================================================================
        //  LOS PUNTOS (nacen del despertar del LIBRO ACTIVO)
        // ================================================================

        /// <summary>LOS PUNTOS TOTALES del portador: 3 + despertar/5 (en
        /// el 100 caben los 23 puntos del árbol completo).</summary>
        public int PuntosTotales()
        {
            int despertar = Systems.DespertarGrimorio.NivelDe(Player);
            return 3 + despertar / 5;
        }

        /// <summary>LOS PUNTOS GASTADOS (la suma de los niveles — el ancla
        /// es gratis: no cuenta).</summary>
        public int PuntosGastados()
        {
            int gastados = 0;
            for (int i = 1; i < Niveles.Length && i < ArbolUtil.TOTAL_NODOS; i++)
                gastados += Niveles[i];
            return gastados;
        }

        /// <summary>LOS PUNTOS LIBRES (los que el árbol puede cobrar
        /// AHORA).</summary>
        public int PuntosLibres()
        {
            int libres = PuntosTotales() - PuntosGastados();
            return libres < 0 ? 0 : libres;
        }

        // ================================================================
        //  LOS EFECTOS (el modelo PoT: PostUpdateEquips — cada tick, por
        //  jugador, en todas las pantallas).
        // ================================================================
        public override void PostUpdateEquips()
        {
            try
            {
                if (Niveles == null || Niveles.Length < ArbolUtil.TOTAL_NODOS) return;
                // EL ANCLA SIEMPRE VIVA (la carga vieja la asegura, la
                // nueva también).
                if (Niveles[0] < 1) Niveles[0] = 1;

                // B — EL FILO DEL ECO: +8 % de daño genérico por nivel.
                Player.GetDamage(DamageClass.Generic) += 0.08f * Niveles[1];
                // C — EL CUERPO DE TINTA: +20 vida máxima por nivel.
                Player.statLifeMax2 += 20 * Niveles[2];
                // D — LA MENTE DESPIERTA: +40 maná máximo por nivel.
                Player.statManaMax2 += 40 * Niveles[3];
                // E — EL COLMILLO CRÍTICO: +4 % de crítico por nivel.
                Player.GetCritChance(DamageClass.Generic) += 4f * Niveles[4];
                // F — LAS PLUMAS LIGERAS: +8 % de velocidad por nivel.
                Player.moveSpeed += 0.08f * Niveles[5];
                // G — LA PIEL DE TINTA: +4 defensa por nivel.
                Player.statDefense += 4 * Niveles[6];
                // H — LA TINTA BARATA: −10 % de coste de maná por nivel.
                Player.manaCost -= 0.10f * Niveles[7];
                if (Player.manaCost < 0f) Player.manaCost = 0f;
                // I — EL CORAZÓN DE TINTA: +1 vida/s por nivel (lifeRegen
                // 2 ≈ 1 HP/s — el convenio de vanilla).
                Player.lifeRegen += 2 * Niveles[8];
                // J — EL ECO DE AETHON (keystone): +1 espacio de minion.
                Player.maxMinions += Niveles[9];
            }
            catch { }
        }
    }

    /// <summary>EL UTILITARIO DEL ÁRBOL (constantes compartidas entre el
    /// jugador y la ventana — el contrato de la casa).</summary>
    public static class ArbolUtil
    {
        /// <summary>EL TOTAL DE NODOS del árbol (el 0 es el ancla).</summary>
        public const int TOTAL_NODOS = 10;
    }
}
