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
    //  v6.88 — EL ÁRBOL CAMBIA DE DUEÑO (la letra: «el arbol de
    //  habilidades que no sea para el grimorio del eterno, crea un arma
    //  nueva para que use el arbol de habilidades, esta nueva arma no
    //  tiene nada que ver con el grimorio, asi que crea el arma libro
    //  azul y enlazalo al arbol de habilidades como prueba»): CERO
    //  VÍNCULO CON EL DESPERTAR — los puntos nacen del NIVEL DEL LIBRO
    //  AZUL (el arma de Investigaciones que ABRE el árbol con clic
    //  derecho), y los efectos SOLO viven mientras el portador LLEVA
    //  el Libro Azul. El Libro Azul sube de nivel con las BAJAS de su
    //  portador (8 por nivel, tope 100 — como el despertar: 3 +
    //  nivel/5, los mismos 23 puntos de la cima).
    // ======================================================================
    public class SkillTreePlayer : ModPlayer
    {
        /// <summary>LOS NIVELES de los nodos (por índice — el 0 es el
        /// ANCLA, siempre 1 y gratis).</summary>
        public int[] Niveles = new int[ArbolUtil.TOTAL_NODOS];

        /// <summary>¿Cargó ya? (para no pisar con defaults a mitad de
        /// sesión).</summary>
        public bool Cargado = false;

        // ================================================================
        //  v6.88 — EL LIBRO AZUL (la nueva fuente de puntos del árbol):
        //  su NIVEL (1-100) y sus BAJAS acumuladas (8 por nivel).
        // ================================================================

        /// <summary>EL NIVEL DEL LIBRO AZUL de este portador (1-100).
        /// Sube con las bajas: 1 + bajas/8.</summary>
        public int NivelLibroAzul = 1;

        /// <summary>LAS BAJAS acumuladas con el Libro Azul puesto (8
        /// por nivel — el tope de 100 son 792 bajas).</summary>
        public int BajasLibroAzul = 0;

        /// <summary>¿LLEVA el Libro Azul? (inventario o mano — la guardia
        /// de los efectos del árbol: sin el arma, el árbol duerme).</summary>
        public bool LlevaLibroAzul()
        {
            try
            {
                int tipo = ModContent.ItemType
                    <global::AethonMod.Content.Weapons.Investigaciones.LibroAzul>();
                if (Player.HeldItem != null && !Player.HeldItem.IsAir &&
                    Player.HeldItem.type == tipo) return true;
                for (int i = 0; i < 58; i++)
                    if (Player.inventory[i] != null && !Player.inventory[i].IsAir
                        && Player.inventory[i].type == tipo)
                        return true;
                return false;
            }
            catch { return false; }
        }

        /// <summary>LA BAJA DEL LIBRO AZUL (la paga CazaJefesGlobalNPC.OnKill
        /// con el último golpe): 8 bajas por nivel — el arma crece con
        /// quien la lleva.</summary>
        public void RegistrarBajaLibroAzul()
        {
            try
            {
                BajasLibroAzul++;
                int nivel = 1 + BajasLibroAzul / 8;
                if (nivel > 100) nivel = 100;
                if (nivel > NivelLibroAzul)
                {
                    NivelLibroAzul = nivel;
                    // EL ANUNCIO DEL PELDAÑO (solo donde hay oídos).
                    if (Player.whoAmI == Main.myPlayer && !Main.dedServ)
                    {
                        Main.NewText($"[c/9BD4FF:El Libro Azul sube al nivel {NivelLibroAzul}]"
                            + (PuntosLibres() > 0
                                ? $"[c/78FF96: — {PuntosLibres()} punto(s) del árbol de habilidades]"
                                : ""), 255, 255, 255);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4
                            .WithPitchOffset(0.25f), Player.Center);
                    }
                }
            }
            catch { }
        }

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
                // v6.88 — EL LIBRO AZUL (la fuente de puntos).
                tag["arbolNivelLibroAzul"] = NivelLibroAzul;
                tag["arbolBajasLibroAzul"] = BajasLibroAzul;
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
                // v6.88 — el Libro Azul (o el recomputado si la clave falta).
                NivelLibroAzul = System.Math.Max(1, System.Math.Min(100,
                    tag.GetInt("arbolNivelLibroAzul")));
                BajasLibroAzul = System.Math.Max(0, tag.GetInt("arbolBajasLibroAzul"));
                if (NivelLibroAzul <= 1 && BajasLibroAzul > 0)
                    NivelLibroAzul = System.Math.Min(100, 1 + BajasLibroAzul / 8);
                Cargado = true;
            }
            catch { Niveles = new int[ArbolUtil.TOTAL_NODOS]; if (Niveles.Length > 0) Niveles[0] = 1; }
        }

        // ================================================================
        //  LOS PUNTOS (v6.88: nacen del NIVEL DEL LIBRO AZUL — nada del
        //  despertar: el árbol ya no es del grimorio)
        // ================================================================

        /// <summary>LOS PUNTOS TOTALES del portador: 3 + nivel del Libro
        /// Azul / 5 (en el 100 caben los 23 puntos del árbol completo —
        /// los mismos techos de siempre).</summary>
        public int PuntosTotales()
        {
            return 3 + NivelLibroAzul / 5;
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
        //  jugador) — v6.88: SOLO mientras el portador LLEVA EL LIBRO
        //  AZUL (la letra: el árbol es del ARMA; sin el arma puesta, el
        //  árbol duerme y sus nodos no dan nada).
        // ================================================================
        public override void PostUpdateEquips()
        {
            try
            {
                if (Niveles == null || Niveles.Length < ArbolUtil.TOTAL_NODOS) return;
                // EL ANCLA SIEMPRE VIVA (la carga vieja la asegura, la
                // nueva también).
                if (Niveles[0] < 1) Niveles[0] = 1;

                // v6.88 — LA GUARDIA DEL ARMA: sin el Libro Azul puesto,
                //     el árbol duerme (cero efectos: el árbol no es del
                //     grimorio ni del personaje — es del ARMA).
                if (!LlevaLibroAzul()) return;

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
