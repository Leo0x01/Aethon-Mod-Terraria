#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.88 — parche de la MESA: la ofrenda mineral pasa a MEZCLAS por grupos
   (varios minerales a la vez, cantidades moderadas) y el CANJE DEL ALTAR
   muere (el altar ya no está ligado al despertar). Alma: nace
   OfrendaMineralGrupo, mueren ProgresoOfrendas/CreditoPuntosEsencias."""
import io

# ================= AlmaEternoGlobalItem =================
R = "AethonMod/Content/Globals/AlmaEternoGlobalItem.cs"
t = io.open(R, encoding="utf-8").read()

# --- 1) los campos de puntos mueren; nace OfrendaMineralGrupo ---
i0 = t.index("        /// <summary>Los PUNTOS DE OFRENDA acumulados por el altar")
i1 = t.index("public int CreditoPuntosEsencias = 0;") + len("public int CreditoPuntosEsencias = 0;")
while t[i1] != "\n": i1 += 1
i1 += 1
nuevo = '''        // ================================================================
        //  v6.88 — LA ECONOMÍA DE PUNTOS MUERE (la letra: «la funcion del
        //  altar que implementaste no debe estar ligada al grimorio, asi
        //  que quita ese requisito de ofrendas en el despertar»):
        //  ProgresoOfrendas y CreditoPuntosEsencias se extinguen con la
        //  v6.86. NACE el índice de la MEZCLA mineral (la ofrenda
        //  variada de la v6.88: «pedir varios al mismo tiempo»).
        // ================================================================

        /// <summary>v6.88 — EL GRUPO ACTUAL de la mezcla mineral del
        /// peldaño vivo (0 = el primero de GruposMineralDe): el índice
        /// del mineral que la mesa está comiendo AHORA. Completo el
        /// grupo, avanza al siguiente; length = mezcla entera paga.</summary>
        public int OfrendaMineralGrupo = 0;

'''
t = t[:i0] + nuevo + t[i1:]

# --- 2) SaveData ---
viejo = """                if (OfrendaMineral > 0) tag["almaOfrendaMineral"] = OfrendaMineral;
"""
nuevo = """                if (OfrendaMineral > 0) tag["almaOfrendaMineral"] = OfrendaMineral;
                if (OfrendaMineralGrupo > 0) tag["almaOfrendaMineralGrupo"] = OfrendaMineralGrupo;
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)
viejo = """                if (ProgresoOfrendas > 0) tag["almaPuntos"] = ProgresoOfrendas;
                if (CreditoPuntosEsencias > 0) tag["almaCredito"] = CreditoPuntosEsencias;
"""
assert t.count(viejo) == 1
t = t.replace(viejo, "")

# --- 3) LoadData ---
viejo = """                OfrendaMineral = System.Math.Max(0, System.Math.Min(9999, tag.GetInt("almaOfrendaMineral")));
"""
nuevo = """                OfrendaMineral = System.Math.Max(0, System.Math.Min(9999, tag.GetInt("almaOfrendaMineral")));
                OfrendaMineralGrupo = System.Math.Max(0, System.Math.Min(9, tag.GetInt("almaOfrendaMineralGrupo")));
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)
viejo = """                ProgresoOfrendas = System.Math.Max(0, System.Math.Min(999999, tag.GetInt("almaPuntos")));
                CreditoPuntosEsencias = System.Math.Max(0, System.Math.Min(9999, tag.GetInt("almaCredito")));
"""
assert t.count(viejo) == 1
t = t.replace(viejo, "")

# --- 4) el catch del LoadData ---
viejo = """                ProgresoOfrendas = 0;
                CreditoPuntosEsencias = 0;
"""
assert t.count(viejo) == 1
t = t.replace(viejo, "")

# --- 5) NetSend: ver 4, sin puntos, con grupo ---
viejo = "                writer.Write((byte)3);                    // versión del alma (v6.86: libro + altar)"
nuevo = "                writer.Write((byte)4);                    // versión del alma (v6.88: libro + grupo mineral)"
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)
viejo = """                writer.Write(LibroCorruptoConsumido);
                writer.Write(ProgresoOfrendas);
                writer.Write(CreditoPuntosEsencias);
"""
nuevo = """                writer.Write(LibroCorruptoConsumido);
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)
viejo = """                writer.Write(PunoTitan);
"""
nuevo = """                writer.Write(PunoTitan);
                if (ver >= 4) writer.Write((byte)OfrendaMineralGrupo);   // v6.88: la mezcla
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

# --- 6) NetReceive ---
viejo = """                    LibroCorruptoConsumido = reader.ReadBoolean();
                    ProgresoOfrendas = System.Math.Max(0, reader.ReadInt32());
                    CreditoPuntosEsencias = System.Math.Max(0, reader.ReadInt32());
"""
nuevo = """                    LibroCorruptoConsumido = reader.ReadBoolean();
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)
viejo = """                PunoTitan = System.Math.Max(0, reader.ReadInt32());
"""
nuevo = """                PunoTitan = System.Math.Max(0, reader.ReadInt32());
                if (ver >= 4) OfrendaMineralGrupo = reader.ReadByte();   // v6.88: la mezcla
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

io.open(R, "w", encoding="utf-8").write(t)
print("OK — AlmaEternoGlobalItem: grupo mineral + puntos extintos")

# ================= DespertarGrimorioSistema =================
R = "AethonMod/Content/Systems/DespertarGrimorioSistema.cs"
t = io.open(R, encoding="utf-8").read()

# --- 1) cabecera de la mesa: grupos en vez de tipo único ---
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

# --- 2) el canje muere ---
i0 = t.index("                // ============================================================\n                //  v6.86 — EL CANJE DEL ALTAR")
i1 = t.index("                for (int d = 0; d < Main.maxItems; d++)")
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
                                    new Color(196, 138, 255), comeMin,
                                    System.Math.Min(alma.OfrendaMineral + (alma.OfrendaMineralGrupo > g ? cantGrupo : 0), cantGrupo),
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
print("OK — DespertarGrimorioSistema: mezcla por grupos + canje extinto")
