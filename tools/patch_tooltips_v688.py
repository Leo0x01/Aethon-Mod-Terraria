#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.88 — GrimoireEternal (tooltips): la ventana de info dice la VERDAD.
   (1) bloqueos con sus niveles reales (15/30/90/100 — la v6.86 los dejó
       en 7/10/45/50 y el usuario veía «50 de 50»);
   (2) el hito del escuadrón en PORCENTAJE (cada 20 niveles) + línea viva;
   (3) la cima de la Ira: Moon Lord 10 + Emperatriz 5 + Aethon 1;
   (4) los PUNTOS DEL ALTAR mueren (ya no existen);
   (5) la alternativa mineral muestra la MEZCLA completa (grupo a grupo);
   (6) el tope dice 100 de 100."""
import io

R = "AethonMod/Content/Weapons/GrimoireEternal.cs"
t = io.open(R, encoding="utf-8").read()

# --- (1) los bloqueos con sus niveles REALES ---
viejo = '"[c/78788C:Sol Rúnico: bloqueado — Despertar 7]"'
nuevo = '"[c/78788C:Sol Rúnico: bloqueado — Despertar 15]"'
assert t.count(viejo) == 1; t = t.replace(viejo, nuevo)

viejo = '"[c/78788C:Agujeros Negros Rúnicos: bloqueado — Despertar 10]"'
nuevo = '"[c/78788C:Agujeros Negros Rúnicos: bloqueado — Despertar 30]"'
assert t.count(viejo) == 1; t = t.replace(viejo, nuevo)

viejo = '"[c/78788C:Supremo / Supremo Aurora: bloqueado — Despertar 45]"'
nuevo = '"[c/78788C:Supremo / Supremo Aurora: bloqueado — Despertar 90]"'
assert t.count(viejo) == 1; t = t.replace(viejo, nuevo)

viejo = '"[c/78788C:Ira de un Ser Exterior: bloqueado — Despertar 50]"'
nuevo = '"[c/78788C:Ira de un Ser Exterior: bloqueado — Despertar 100]"'
assert t.count(viejo) == 1; t = t.replace(viejo, nuevo)

# --- (2) el hito del escuadrón en porcentaje + línea viva ---
viejo = """                if (despertar >= Systems.DespertarGrimorio.NivelMax)
                {
                    tooltips.Add(new TooltipLine(Mod, "WMaximo",
                        "[c/C49CFF:Despertar 50: TODO el ataque del escuadrón ×1,25 y más rápido]"));
                }
"""
nuevo = """                // v6.88 — EL HITO DEL ESCUADRÓN EN PORCENTAJE (la letra:
                //     «dice Despertar 50 todo el ataque del escuadron
                //     1.25 y mas rapido, creo que deberia decirlo en
                //     porcentaje y deberia de haber un aumento cada 20
                //     niveles ademas de que debe decirlo como hito»):
                //     línea VIVA con el multiplicador actual (y el
                //     próximo hito en la misma línea).
                {
                    int pct = Systems.DespertarGrimorio.PctEscuadron(despertar);
                    int proximoHito = 20;
                    while (proximoHito <= despertar && proximoHito < 100) proximoHito += 20;
                    string viva = pct > 0
                        ? $"[c/C49CFF:DESPERTAR {despertar}: TODO el ataque del escuadrón +{pct} %]"
                        : "[c/78788C:el escuadrón aún no cobra su hito (Despertar 20: +10 %)]";
                    if (pct > 0 && proximoHito <= Systems.DespertarGrimorio.NivelMax)
                        viva += $"[c/78788C:  →  hito {proximoHito}: +{Systems.DespertarGrimorio.PctEscuadron(proximoHito)} %]";
                    else if (pct > 0)
                        viva += "[c/C49CFF:  — la cima del escuadrón]";
                    tooltips.Add(new TooltipLine(Mod, "WHitoEscuadron", viva));
                }
"""
assert t.count(viejo) == 1; t = t.replace(viejo, nuevo)

# --- (3) la cima de la Ira: ML 10 + EoL 5 + Aethon 1 ---
viejo = """                        if (siguiente == Systems.DespertarGrimorio.NivelMax)
                        {
                            var cazador = Main.LocalPlayer.GetModPlayer
                                <global::AethonMod.Content.Players.CazaJefesPlayer>();
                            int mlVeces = cazador?.VecesMoonLord ?? 0;
                            int aeVeces = cazador?.VecesAethon ?? 0;
                            bool iraPaga = mlVeces >= 5 && aeVeces >= 1;
                            string colIra = iraPaga ? colOK : colNo;
                            tooltips.Add(new TooltipLine(Mod, "DespertarReqIra",
                                $"[c/{colIra}:  • LA IRA (El Haz): Moon Lord {Math.Min(mlVeces, 5)}/5 · Aethon {Math.Min(aeVeces, 1)}/1{(iraPaga ? "  ✓" : "")}]"));
                        }
"""
nuevo = """                        if (siguiente == Systems.DespertarGrimorio.NivelMax)
                        {
                            // v6.88 — LA CIMA COMPUESTA (la letra: «Aethon
                            //     debería estar como requisito para el nivel
                            //     100, junto con 10 señores de la luna y 5
                            //     emperatris de la luz, ya que en el nivel
                            //     100 es donde se consigue la habilidad
                            //     definitiva del grimorio, o sea su ataque
                            //     definitivo, obtiene Ira de un Ser Exterior»).
                            var cazador = Main.LocalPlayer.GetModPlayer
                                <global::AethonMod.Content.Players.CazaJefesPlayer>();
                            int mlVeces = cazador?.VecesMoonLord ?? 0;
                            int eoVeces = cazador?.VecesEmperatriz ?? 0;
                            int aeVeces = cazador?.VecesAethon ?? 0;
                            bool iraPaga = mlVeces >= 10 && eoVeces >= 5 && aeVeces >= 1;
                            string colIra = iraPaga ? colOK : colNo;
                            tooltips.Add(new TooltipLine(Mod, "DespertarReqIra",
                                $"[c/{colIra}:  • LA IRA (El Haz): Moon Lord {Math.Min(mlVeces, 10)}/10 · Emperatriz {Math.Min(eoVeces, 5)}/5 · Aethon {Math.Min(aeVeces, 1)}/1{(iraPaga ? "  ✓" : "")}]"));
                        }
"""
assert t.count(viejo) == 1; t = t.replace(viejo, nuevo)

# --- (4) los PUNTOS DEL ALTAR mueren ---
viejo = """                        // === v6.86 — LOS PUNTOS DEL ALTAR (la letra del
                        //     altar: «para que el grimorio pueda consumir
                        //     cosas que no sean las que necesita y no
                        //     esten corruptas»): lo que el altar devoró ya
                        //     vive en el alma del libro — y paga hasta el
                        //     30 % de las esencias del peldaño. ===
                        int puntos = almaLista?.ProgresoOfrendas ?? 0;
                        int credito = almaLista?.CreditoPuntosEsencias ?? 0;
                        tooltips.Add(new TooltipLine(Mod, "DespertarReqPuntos",
                            $"[c/78EDBE:  • Puntos de Ofrenda del altar: {puntos} (pagan hasta el 30 % de las esencias)]" +
                            (credito > 0 ? $"[c/78EDBE: — {credito} esencias ya pagas]" : "")));
"""
nuevo = """                        // v6.88 — LOS PUNTOS DEL ALTAR MURIERON (la letra:
                        //     «quita ese requisito de ofrendas en el
                        //     despertar»): ya no hay línea de puntos — el
                        //     altar corrompe armas y esta mesa no lo lee.
"""
assert t.count(viejo) == 1; t = t.replace(viejo, nuevo)

# --- (5) la alternativa mineral → la MEZCLA (grupo a grupo) ---
viejo = """                        if (!Systems.DespertarGrimorio.EsNivelMineral(siguiente))
                        {
                            int tipoAlt = Systems.DespertarGrimorio.MineralDe(siguiente);
                            int cantAlt = Systems.DespertarGrimorio.CantidadMineralDe(siguiente);
                            bool altOK = devoradaArma || comidosMineral >= cantAlt;
                            string colAlt = altOK ? colOK : colNo;
                            string nombreAlt = "—";
                            try
                            {
                                var protoAlt = Terraria.ID.ContentSamples.ItemsByType[tipoAlt];
                                if (protoAlt != null) nombreAlt = protoAlt.Name;
                            }
                            catch { }
                            var trozosAlt = new System.Collections.Generic.List<string>();
                            trozosAlt.Add($"[c/{colAlt}:  • O la ofrenda mineral:]");
                            trozosAlt.Add($"[i:{tipoAlt}]");
                            trozosAlt.Add($"[c/{colAlt}:{Math.Min(comidosMineral, cantAlt)}/{cantAlt} × {nombreAlt}{(altOK ? "  ✓" : "")}]");
                            tooltips.Add(new TooltipLine(Mod, "DespertarReq6",
                                string.Join(" ", trozosAlt)));
                        }
"""
nuevo = """                        // v6.88 — LA ALTERNATIVA MINERAL ES UNA MEZCLA
                        //     (la letra: «debe ser variada, pedir menos de
                        //     ese mineral, pero pedir varios al mismo
                        //     tiempo»): UNA LÍNEA POR GRUPO de la mezcla
                        //     del peldaño — con su estado (✓ / n·cANT) y
                        //     los ALTERNATIVOS del grupo como iconos.
                        if (!Systems.DespertarGrimorio.EsNivelMineral(siguiente))
                        {
                            int[][] gruposAlt = Systems.DespertarGrimorio.GruposMineralDe(siguiente);
                            int cantAlt = Systems.DespertarGrimorio.CantidadPorGrupo(siguiente);
                            for (int gi = 0; gi < gruposAlt.Length && gi < 3; gi++)
                            {
                                int[] grupo = gruposAlt[gi];
                                if (grupo == null || grupo.Length == 0) continue;
                                // el grupo se completa con el índice: los
                                // pagos ya hechos (OfrendaMineralGrupo)
                                // cubren los grupos anteriores.
                                bool grupoPago = devoradaArma ||
                                    almaLista != null && gi < almaLista.OfrendaMineralGrupo;
                                bool grupoActual = almaLista != null && !devoradaArma &&
                                    gi == almaLista.OfrendaMineralGrupo;
                                int comidos = grupoActual ? Math.Min(almaLista.OfrendaMineral, cantAlt) : 0;
                                bool altOK = grupoPago;
                                string colAlt = altOK ? colOK : (grupoActual ? "E8D8FF" : colNo);
                                var trozosAlt = new System.Collections.Generic.List<string>();
                                trozosAlt.Add($"[c/{colAlt}:  • {'a' + gi}) la ofrenda mineral:]");
                                for (int ti = 0; ti < grupo.Length && ti < 3; ti++)
                                    trozosAlt.Add($"[i:{grupo[ti]}]");
                                string nombreAlt = "—";
                                try
                                {
                                    var protoAlt = Terraria.ID.ContentSamples.ItemsByType[grupo[0]];
                                    if (protoAlt != null) nombreAlt = protoAlt.Name;
                                }
                                catch { }
                                trozosAlt.Add($"[c/{colAlt}:{(altOK ? cantAlt : comidos)}/{cantAlt} × {nombreAlt}{(altOK ? "  ✓" : "")}]");
                                tooltips.Add(new TooltipLine(Mod, "DespertarReq6_" + gi,
                                    string.Join(" ", trozosAlt)));
                            }
                        }
"""
assert t.count(viejo) == 1; t = t.replace(viejo, nuevo)

# --- (6) el tope dice la verdad ---
viejo = '"[c/C49CFF:  EL GRIMORIO DESPERTÓ DEL TODO (máximo por ahora)]"'
nuevo = '"[c/C49CFF:  EL GRIMORIO DESPERTÓ DEL TODO — 100 de 100]"'
assert t.count(viejo) == 1; t = t.replace(viejo, nuevo)

io.open(R, "w", encoding="utf-8").write(t)
print("OK — GrimoireEternal: tooltips con la verdad (15/30/90/100, hito %, cima ML10+EoL5+Ae1, sin puntos, mezcla)")
