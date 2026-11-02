#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.88 — parche 2 de DespertarGrimorio.cs:
   (1) IraDisponible(p) simplificada (la puerta 100 ya cobró la caza) +
       IraContadoresTexto ML10/EoL5/Aethon1 + MultEscuadron/PctEscuadron.
   (2) La API del altar (puntos/relleno) MUERE — el altar se desliga del
       despertar (la letra: quita ese requisito de ofrendas)."""
import io, sys

RUTA = "AethonMod/Content/Systems/DespertarGrimorio.cs"
with io.open(RUTA, "r", encoding="utf-8") as f:
    lineas = f.readlines()

# --- (1) líneas 471..494 (índices 470..493): IraDisponible(p) + IraContadores ---
bloque_ira = '''        /// <summary>v6.88 — LA IRA VIVE CON EL DESPERTAR 100 (la letra
        /// nueva de la cima: «Aethon debería estar como requisito para el
        /// nivel 100, junto con 10 señores de la luna y 5 emperatris de la
        /// luz, ya que en el nivel 100 es donde se consigue la habilidad
        /// definitiva del grimorio, o sea su ataque definitivo, obtiene
        /// Ira de un Ser Exterior»): la PUERTA 100 (JefeYaCaido) exige
        /// Aethon ≥ 1 Y Moon Lord ≥ 10 Y Emperatriz ≥ 5 ANTES DE SUBIR —
        /// llegar al 100 ya ES haber pagado la caza completa. La cura del
        /// bug «al llegar al nivel máximo del despertar el haz no se
        /// lanza»: la v6.86 volvía a pedir los contadores DESPUÉS de
        /// subir (si no los tenía, el haz NUNCA nacía); la v6.88 los
        /// cobra en la puerta y la Ira nace con el despertar 100. El
        /// escuadrón de 11 TODOS al máximo sigue siendo requisito del
        /// DISPARO (la letra v6.60, la pone
        /// AtaquesCombinadosSistema.Tick).</summary>
        public static bool IraDisponible(Player p)
        {
            if (p == null || !p.active) return false;
            return NivelDe(p) >= NivelMax;
        }

        /// <summary>Los CONTADORES DE LA CIMA en texto (para tooltips y
        /// la ventana del Cielo) — v6.88: «Moon Lord n/10 · Emperatriz
        /// n/5 · Aethon n/1» (la letra nueva de la cima).</summary>
        public static string IraContadoresTexto(Player p)
        {
            var cazador = p?.GetModPlayer<Players.CazaJefesPlayer>();
            int ml = cazador?.VecesMoonLord ?? 0;
            int eo = cazador?.VecesEmperatriz ?? 0;
            int ae = cazador?.VecesAethon ?? 0;
            return $"Moon Lord {Math.Min(ml, 10)}/10 · Emperatriz {Math.Min(eo, 5)}/5 · Aethon {Math.Min(ae, 1)}/1";
        }

        // ================================================================
        //  v6.88 — LOS HITOS DEL ESCUADRÓN (la letra: «hay una parte en
        //  las ventana de info que dice Despertar 50 todo el ataque del
        //  escuadron 1.25 y mas rapido, creo que deberia decirlo en
        //  porcentaje y deberia de haber un aumento cada 20 niveles
        //  ademas de que debe decirlo como hito»): EL HITO DEL ESCUADRÓN
        //  nace CADA 20 NIVELES, en PORCENTAJE, como HITO de la escalera
        //  (ClaveHito + ElCaminoDelDespertar + el tooltip del arma):
        ///    20 → TODO el ataque del escuadrón +10 %
        ///    40 → +15 %   · 60 → +20 %   · 80 → +25 %   · 100 → +30 %
        //  La v6.51.9 lo cobraba UNA sola vez (×1,25 con el despertar 50,
        //  horneado al nacer); la v6.88 lo cobra VIVO por hitos — el
        //  escuadrón entero se fortalece con el despertar de su dueño.
        // ================================================================

        /// <summary>EL MULTIPLICADOR DE ATAQUE DEL ESCUADRÓN por los
        /// hitos del despertar (cada 20 niveles, en porcentaje): lo leen
        /// BonusHitoVivo (el daño del rayo de cada ojo — el reemplazo
        /// del ×1,25 del 50) y los conjuros combinados (sol, agujeros,
        /// supremo — AtaquesCombinadosSistema).</summary>
        public static float MultEscuadron(int despertar)
        {
            if (despertar >= 100) return 1.30f;
            if (despertar >= 80) return 1.25f;
            if (despertar >= 60) return 1.20f;
            if (despertar >= 40) return 1.15f;
            if (despertar >= 20) return 1.10f;
            return 1.00f;
        }

        /// <summary>El PORCENTAJE del hito en texto (para tooltips:
        /// «+25 %»).</summary>
        public static int PctEscuadron(int despertar)
            => (int)Math.Round((MultEscuadron(despertar) - 1f) * 100f);

'''

# --- (2) líneas 496..602 (índices 495..601): la API del altar MUERE ---
bloque_altar = '''        // ================================================================
        //  v6.88 — EL ALTAR SE DESLIGA DEL DESPERTAR (la letra: «la
        //  funcion del altar que implementaste no debe estar ligada al
        //  grimorio, asi que quita ese requisito de ofrendas en el
        //  despertar, el altar sirve para corromper las armas»): LA
        //  ECONOMÍA DE PUNTOS (PuntosPorEsencia, ValorMinimoRelleno,
        //  AceptaRelleno, ValorEnPuntos, RegistrarOfrenda) MUERE de
        //  raíz — el canje del 30 % de esencias (ServirLaMesa) muere con
        //  ella y los campos del alma (ProgresoOfrendas /
        //  CreditoPuntosEsencias) quedan huérfanos y se extinguen. El
        //  altar hoy es EL CORRUPTOR DE ARMAS (AltarOfrendasUI) y nada
        //  más. La vía de recetas de corrupción (RecetasCorrupcionSistema)
        //  SIGUE como alternativa de siempre.
        // ================================================================

'''

# Verificaciones de anclaje (línea-1 == índice 0)
assert lineas[470].lstrip().startswith("/// <summary>v6.86 — LA IRA COMPLETA"), lineas[470]
assert "IraContadoresTexto" in lineas[487] or "IraContadoresTexto" in lineas[488]
assert lineas[495].lstrip().startswith("// ======"), lineas[495]
assert "PuntosPorEsencia = 25" in "".join(lineas[505:515])
assert "RegistrarOfrenda" in lineas[585]

nuevo = lineas[:470] + [bloque_ira] + lineas[494:495] + [bloque_altar] + lineas[602:]

with io.open(RUTA, "w", encoding="utf-8") as f:
    f.writelines(nuevo)
print(f"OK — {RUTA}: Ira simplificada + MultEscuadron + API del altar extinguida "
      f"({len(lineas)} → {sum(1 for _ in nuevo)} líneas)")
