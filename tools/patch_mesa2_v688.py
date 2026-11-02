#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.88 — parche de la MESA (v2 — el i1 ahora se busca DESDE i0: la
duplicación de la v1 quedó extinta)."""
import io

R = "AethonMod/Content/Systems/DespertarGrimorioSistema.cs"
t = io.open(R, encoding="utf-8").read()

# --- 1) cabecera de la mesa ---
viejo = """                int esenciasNecesarias = DespertarGrimorio.EsenciasPara(siguiente);
                long monedasNecesarias = DespertarGrimorio.MonedasPara(siguiente);
                int mineralNecesario = DespertarGrimorio.CantidadMineralDe(siguiente);
                int tipoMineral = DespertarGrimorio.MineralDe(siguiente);
                int[] armasDelPeldano = DespertarGrimorio.ArmasDelNivel[siguiente];
                bool mordisco = false;
"""
nuevo = """                int esenciasNecesarias = DespertarGrimorio.EsenciasPara(siguiente);
                long monedasNecesarias = DespertarGrimorio.MonedasPara(siguiente);
                // v6.88 — LA MEZCLA MINERAL (la ofrenda variada): grupos
                //     con sus alternativos + la cantidad moderada por cabeza.
                int[][] gruposMineral = DespertarGrimorio.GruposMineralDe(siguiente);
                int cantGrupo = DespertarGrimorio.CantidadPorGrupo(siguiente);
                int[] armasDelPeldano = DespertarGrimorio.ArmasDelNivel[siguiente];
                bool mordisco = false;
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

# --- 2) el canje muere (i1 DESDE i0 — la cura de la v1) ---
i0 = t.index("                // ============================================================\n                //  v6.86 — EL CANJE DEL ALTAR")
i1 = t.index("                for (int d = 0; d < Main.maxItems; d++)", i0)
assert i1 > i0, "i1 debe venir DESPUÉS del canje"
t = t[:i0] + """                // ============================================================
                //  v6.88 — EL CANJE DEL ALTAR MURIÓ (la letra: «la funcion
                //  del altar que implementaste no debe estar ligada al
                //  grimorio, asi que quita ese requisito de ofrendas en el
                //  despertar»): el crédito del 30 % y los puntos de
                //  ofrenda se extinguieron con la v6.86 — el altar hoy
                //  corrompe armas (AltarOfrendasUI) y ya no paga NINGUNA
                //  parte de esta mesa. Las esencias se comen solas.
                // ============================================================

""" + t[i1:]

# --- 3) la falta de esencias sin crédito ---
viejo = """                        int falta = esenciasNecesarias - alma.OfrendaEsencias
                            - alma.CreditoPuntosEsencias;
"""
nuevo = """                        int falta = esenciasNecesarias - alma.OfrendaEsencias;
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

# --- 4) el bloque del mineral → la mezcla por grupos ---
viejo = """                    else if (tipoMineral > 0 && drop.type == tipoMineral)
                    {
                        if (alma.OfrendaArma) continue;
                        int faltaMin = mineralNecesario - alma.OfrendaMineral;
                        if (faltaMin <= 0) continue;

                        int comeMin = System.Math.Min(faltaMin, drop.stack);
                        AnimarElBocado(p, drop, 2f);   // v6.51.12: EL FESTÍN CHICO (antes del trago)
                        alma.OfrendaMineral += comeMin;
                        drop.stack -= comeMin;
                        if (drop.stack <= 0) drop.TurnToAir();
                        SincronizarDrop(d);

                        PolvosDelBocado(p, comeMin);
                        EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Despertar.BocadoMineral",
                            new Color(196, 138, 255), comeMin, alma.OfrendaMineral,
                            mineralNecesario);
                        mordisco = true;
                    }
"""
nuevo = """                    else if (!alma.OfrendaArma &&
                        alma.OfrendaMineralGrupo < gruposMineral.Length)
                    {
                        // v6.88 — LA MEZCLA MINERAL (la letra: «debe ser
                        //     variada, pedir menos de ese mineral, pero
                        //     pedir varios al mismo tiempo»): el peldaño
                        //     pide 2-3 minerales de su era — se pagan
                        //     GRUPO A GRUPO (cada grupo lleva sus
                        //     ALTERNATIVOS: en un mundo de estaño, el
                        //     grupo cobre/estaño se paga con estaño). El
                        //     que está en el slot de AHORA es
                        //     OfrendaMineralGrupo; completarlo avanza al
                        //     siguiente.
                        int g = alma.OfrendaMineralGrupo;
                        int[] grupo = gruposMineral[g];
                        if (grupo != null && System.Array.IndexOf(grupo, drop.type) >= 0)
                        {
                            int faltaMin = cantGrupo - alma.OfrendaMineral;
                            if (faltaMin > 0)
                            {
                                int comeMin = System.Math.Min(faltaMin, drop.stack);
                                AnimarElBocado(p, drop, 2f);   // v6.51.12: EL FESTÍN CHICO
                                alma.OfrendaMineral += comeMin;
                                // GRUPO COMPLETO → el siguiente (con el
                                // último, el índice pasa de length: la
                                // MEZCLA ENTERA queda paga).
                                if (alma.OfrendaMineral >= cantGrupo)
                                {
                                    alma.OfrendaMineralGrupo++;
                                    alma.OfrendaMineral = 0;
                                }
                                drop.stack -= comeMin;
                                if (drop.stack <= 0) drop.TurnToAir();
                                SincronizarDrop(d);

                                PolvosDelBocado(p, comeMin);
                                EcoRed.AnunciarAlPortador(p, "Mods.AethonMod.Despertar.BocadoMineral",
                                    new Color(196, 138, 255), comeMin, cantGrupo - faltaMin + comeMin,
                                    cantGrupo);
                                mordisco = true;
                            }
                        }
                    }
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

# --- 5) LaMesaEstaCompleta ---
viejo = """            if (alma == null) return false;
            // v6.86 — el CRÉDITO DEL ALTAR cuenta como esencias comidas
            // (los puntos del altar pagan hasta el 30 % del peldaño).
            if (alma.OfrendaEsencias + alma.CreditoPuntosEsencias <
                DespertarGrimorio.EsenciasPara(siguiente)) return false;
            if (alma.OfrendaMonedas < DespertarGrimorio.MonedasPara(siguiente)) return false;
            if (alma.OfrendaArma) return true;
            return alma.OfrendaMineral >= DespertarGrimorio.CantidadMineralDe(siguiente);
"""
nuevo = """            if (alma == null) return false;
            if (alma.OfrendaEsencias <
                DespertarGrimorio.EsenciasPara(siguiente)) return false;
            if (alma.OfrendaMonedas < DespertarGrimorio.MonedasPara(siguiente)) return false;
            if (alma.OfrendaArma) return true;
            // v6.88 — LA MEZCLA ENTERA (todos los grupos de la ofrenda
            //     variada — el índice pasa de length al pagar el último).
            return alma.OfrendaMineralGrupo >=
                DespertarGrimorio.GruposMineralDe(siguiente).Length;
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

# --- 6) SubirNivel ---
viejo = """                alma.OfrendaMineral = 0;
                alma.OfrendaArma = false;
                // v6.86 — el CRÉDITO DEL ALTAR muere con el peldaño (el
                // canje se cobra UNA vez por subida — los puntos que
                // sobraron viajan al siguiente).
                alma.CreditoPuntosEsencias = 0;
"""
nuevo = """                alma.OfrendaMineral = 0;
                alma.OfrendaMineralGrupo = 0;   // v6.88 — la mezcla renace
                alma.OfrendaArma = false;
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

io.open(R, "w", encoding="utf-8").write(t)
print("OK — la mesa v2: mezcla por grupos + canje extinto (sin duplicar)")
