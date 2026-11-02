#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.88 — CieloDespiertoUI: la ofrenda mineral se pinta como MEZCLA
   (grupos con alternativos + estado grupo a grupo) y los comentarios
   «50 peldaños» pasan a la verdad de los CIEN."""
import io

R = "AethonMod/Content/UI/CieloDespiertoUI.cs"
t = io.open(R, encoding="utf-8").read()

# --- la cabecera del bloque (4) ---
viejo = """            int[] armas = Systems.DespertarGrimorio.ArmasDelNivel[n];
            int tipoMineral = Systems.DespertarGrimorio.MineralDe(n);
            int cantMineral = Systems.DespertarGrimorio.CantidadMineralDe(n);
            float ax = panel.Right - 210f;
"""
nuevo = """            int[] armas = Systems.DespertarGrimorio.ArmasDelNivel[n];
            // v6.88 — LA MEZCLA MINERAL (la ofrenda variada: grupos con
            //     alternativos, cantidad moderada por cabeza).
            int[][] gruposMin = Systems.DespertarGrimorio.GruposMineralDe(n);
            int cantMineral = Systems.DespertarGrimorio.CantidadPorGrupo(n);
            int grupoActual = alma != null ? alma.OfrendaMineralGrupo : 0;
            int comidosActual = alma != null ? alma.OfrendaMineral : 0;
            bool mezclaPagada = alma != null && grupoActual >= gruposMin.Length;
            float ax = panel.Right - 210f;
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

# --- la alternativa mineral (el «O») ---
viejo = """                // LA ALTERNATIVA MINERAL (el «O»).
                int enBolsaMin = CuentaEnLaBolsa(yo, tipoMineral);
                bool mineralPagado = alma != null && vivo &&
                    alma.OfrendaMineral >= cantMineral;
                string textoMin = mineralPagado
                    ? "[✓] " + Language.GetTextValue(Clave("CieloLingotesPagados"))
                    : "[•] " + string.Format(Language.GetTextValue(Clave("CieloLingotes")),
                        cantMineral, Lang.GetItemNameValue(tipoMineral)) +
                    (enBolsaMin > 0 && !mineralPagado
                        ? string.Format(" ({0})", enBolsaMin) : "");
                Utils.DrawBorderString(sb, textoMin, new Vector2(ax, fy + 44f),
                    mineralPagado ? TintaVerde : (enBolsaMin >= cantMineral ? TintaDorado : TintaApagado),
                    0.80f, 0f, 0.4f);
"""
nuevo = """                // v6.88 — LA ALTERNATIVA MINERAL ES UNA MEZCLA (varios
                //     grupos a la vez, cantidades moderadas): el estado
                //     del grupo ACTUAL (los anteriores ya están pagos) y
                //     cuántos quedan.
                int gInfo = System.Math.Min(grupoActual, gruposMin.Length - 1);
                int tipoGrupo = gruposMin[gInfo] != null && gruposMin[gInfo].Length > 0
                    ? gruposMin[gInfo][0] : -1;
                int enBolsaMin = tipoGrupo > 0 ? CuentaEnLaBolsa(yo, tipoGrupo) : 0;
                bool mineralPagado = mezclaPagada;
                string textoMin = mineralPagado
                    ? "[✓] " + Language.GetTextValue(Clave("CieloLingotesPagados"))
                    : "[•] " + string.Format(Language.GetTextValue(Clave("CieloLingotes")),
                        cantMineral, tipoGrupo > 0 ? Lang.GetItemNameValue(tipoGrupo) : "?") +
                    (gruposMin.Length > 1 && !mineralPagado
                        ? string.Format(" (+{0} más)", gruposMin.Length - 1 - grupoActual < 0
                            ? 0 : gruposMin.Length - 1 - grupoActual) : "") +
                    (enBolsaMin > 0 && !mineralPagado
                        ? string.Format(" ({0})", enBolsaMin) : "");
                Utils.DrawBorderString(sb, textoMin, new Vector2(ax, fy + 44f),
                    mineralPagado ? TintaVerde : (enBolsaMin >= cantMineral ? TintaDorado : TintaApagado),
                    0.80f, 0f, 0.4f);
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

# --- el peldaño mineral puro (la ofrenda ES la mezcla) ---
viejo = """            else
            {
                // EL PELDAÑO MINERAL (45-50: la ofrenda ES el mineral).
                int enBolsaMin = CuentaEnLaBolsa(yo, tipoMineral);
                bool mineralPagado = alma != null && vivo &&
                    alma.OfrendaMineral >= cantMineral;
                try
                {
                    Texture2D tex = TextureAssets.Item[tipoMineral].Value;
                    if (tex != null)
                        sb.Draw(tex, new Vector2(ax + 9f, fy + 14f), null,
                            Color.White * 0.92f, 0f, tex.Size() * 0.5f,
                            MathHelper.Min(18f / tex.Width, 18f / tex.Height),
                            SpriteEffects.None, 0f);
                }
                catch { }
                string textoMin = mineralPagado
                    ? "[✓] " + Language.GetTextValue(Clave("CieloLingotesPagados"))
                    : string.Format(Language.GetTextValue(Clave("CieloLingotes")),
                        cantMineral, Lang.GetItemNameValue(tipoMineral)) +
                    (enBolsaMin > 0 && !mineralPagado
                        ? string.Format(" ({0})", enBolsaMin) : "");
                Utils.DrawBorderString(sb, textoMin, new Vector2(ax, fy + 30f),
                    mineralPagado ? TintaVerde : (enBolsaMin >= cantMineral ? TintaDorado : TintaApagado),
                    0.80f, 0f, 0.4f);
            }
"""
nuevo = """            else
            {
                // v6.88 — EL PELDAÑO MINERAL ES UNA MEZCLA (la letra:
                //     «debe ser variada, pedir menos de ese mineral,
                //     pero pedir varios al mismo tiempo»): la fila pinta
                //     TODOS los grupos (máx 3, el primero de cada uno —
                //     en un mundo de estaño el grupo se paga con estaño)
                //     y el estado cuenta los grupos pagos + el actual.
                bool mineralPagado = mezclaPagada;
                float sx = ax;
                int pintados = 0;
                for (int gi = 0; gi < gruposMin.Length && pintados < 3; gi++)
                {
                    int tipoG = gruposMin[gi] != null && gruposMin[gi].Length > 0
                        ? gruposMin[gi][0] : -1;
                    if (tipoG <= 0) continue;
                    try
                    {
                        Texture2D tex = TextureAssets.Item[tipoG].Value;
                        if (tex == null) continue;
                        // los grupos ya pagos, en VERDE; el actual, DORADO.
                        Color tinta = gi < grupoActual ? TintaVerde
                            : (gi == grupoActual ? TintaDorado : Color.White * 0.92f);
                        sb.Draw(tex, new Vector2(sx + 9f, fy + 14f), null,
                            tinta, 0f, tex.Size() * 0.5f,
                            MathHelper.Min(18f / tex.Width, 18f / tex.Height),
                            SpriteEffects.None, 0f);
                        sx += 20f;
                        pintados++;
                    }
                    catch { }
                }
                int gVivo = System.Math.Min(grupoActual, gruposMin.Length - 1);
                int tipoVivo = gruposMin[gVivo] != null && gruposMin[gVivo].Length > 0
                    ? gruposMin[gVivo][0] : -1;
                int enBolsaMin = tipoVivo > 0 ? CuentaEnLaBolsa(yo, tipoVivo) : 0;
                string textoMin = mineralPagado
                    ? "[✓] " + Language.GetTextValue(Clave("CieloLingotesPagados"))
                    : string.Format(Language.GetTextValue(Clave("CieloLingotes")),
                        cantMineral, tipoVivo > 0 ? Lang.GetItemNameValue(tipoVivo) : "?") +
                    (gruposMin.Length > 1 && !mineralPagado
                        ? string.Format(" · mezcla {0}/{1}", grupoActual, gruposMin.Length) : "") +
                    (enBolsaMin > 0 && !mineralPagado
                        ? string.Format(" ({0})", enBolsaMin) : "");
                Utils.DrawBorderString(sb, textoMin, new Vector2(ax, fy + 30f),
                    mineralPagado ? TintaVerde : (enBolsaMin >= cantMineral ? TintaDorado : TintaApagado),
                    0.80f, 0f, 0.4f);
            }
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

# --- los comentarios «50 peldaños» → la verdad de los CIEN ---
t = t.replace("//   · LA ESCALERA (derecha, con rueda del ratón): los 50 peldaños,",
              "//   · LA ESCALERA (derecha, con rueda del ratón): los CIEN peldaños,")
t = t.replace("//  EL PANEL DERECHO — LA ESCALERA (los 50 peldaños, con rueda)",
              "//  EL PANEL DERECHO — LA ESCALERA (los CIEN peldaños, con rueda)")
t = t.replace("// === LAS FILAS (los 50 peldaños) ===",
              "// === LAS FILAS (los CIEN peldaños) ===")
t = t.replace("// EL PELDAÑO MINERAL (45-50: la ofrenda ES el mineral).",
              "// EL PELDAÑO MINERAL (la ofrenda ES la mezcla).")

io.open(R, "w", encoding="utf-8").write(t)
print("OK — CieloDespiertoUI: la mezcla en la escalera + CIEN peldaños")
