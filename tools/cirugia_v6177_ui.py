#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""cirugia_v6177_ui.py — LA INTERFAZ QUE SE LEE.

v6.177 (la letra):
  · «el texto en los botones del altar se desborda un poco»
  · «en el slot de ofrendas deberia estar el slot de ofrenda sin
    textos, actualmente el slot de ofrendas tiene un texto que dice
    ofrendas y se desborda, ese texto es innecesario»
  · «en el altar al tocar un slot para las armas del trio eterno, se
    abre la ventana flotante, esta tiene un texto muy pequeño y no se
    puede leer»
  · «el texto que tiene la ventana flotante de el arsenal elige un
    arma es innecesario, con que diga elige un arma es suficiente»
  · «por cierto todos los textos del altar son muy pequeños»
  · «en el pilar los textos de los botones se desbordan»
  · «no veo que hayas modificado del todo a UI del altar y el pilar,
    sigue igual y con textos desbordado […] los textos siguen sin ser
    arreglados, ya que se desbordan y son demasiado claros en cuanto a
    color»

LA CURA COMPLETA:
  1. EL SLOT DE LA OFRENDA queda LIMPIO (el texto «ofrenda» de dentro
     MUERE — la letra: innecesario y desbordaba).
  2. LA VENTANA FLOTANTE: título GRANDE (escala 1.0 — se lee desde el
     otro lado de la sala) y su título del trío ahora dice solo
     «ELIGE UN ARMA».
  3. TODAS las escalas del altar y el pilar SUBEN (~+0.10 cada una:
     los cuerpos 0.72-0.82 → 0.84-0.94).
  4. LOS BOTONES: escala base 0.88 → 1.05 y MÁS ALTOS (40 → 46 px) —
     el texto se mide al trazo con margen de halo: NADA desborda.
  5. EL HALO se calma un pelo (0.82 → 0.75 de alfa): separa sin lavar
     la tinta (la letra: «demasiado claros en cuanto a color»).
"""
import re

R = 'AethonMod/Content/UI/LibroDelEternoUI.cs'
s = open(R, encoding='utf-8').read()
orig = len(s)

# ── 1. EL SLOT DE LA OFRENDA SIN TEXTO ────────────────────────────
viejo = """            else
                LibroLineas.Texto(sb, At("SlotVacio"), slot.Center.ToVector2(), TintaApagado, 0.7f, 0.5f, 0.5f);"""
nuevo = """            // v6.177 — EL SLOT LIMPIO (la letra: «en el slot de ofrendas
            // deberia estar el slot de ofrenda sin textos… ese texto es
            // innecesario»): el hueco vacío queda VACÍO de verdad."""
assert viejo in s, 'el texto del slot no se encontró'
s = s.replace(viejo, nuevo, 1)

# ── 2. EL HALO MANSO ───────────────────────────────────────────────
viejo = """            Color halo = new Color(255, 250, 236) * 0.82f;"""
nuevo = """            Color halo = new Color(255, 250, 236) * 0.75f;   // v6.177: un punto más manso (la letra: «demasiado claros»)"""
assert viejo in s, 'el halo no se encontró'
s = s.replace(viejo, nuevo, 1)

# ── 3. LOS DOS BOTONES DEL LIBRO: grandes y medidos ───────────────
# (el del altar y el del pilar tienen la misma receta — parche por
#  coincidencia de patrón en cada método Boton)
viejo = """            var fuente = FontAssets.MouseText.Value;
            float esc = LibroLineas.EscalaQueCabe(t ?? "", 0.88f, 208f);
            Rectangle b = new Rectangle((int)(pag.X + pag.Width / 2f - 110f), (int)cy, 220, 40);
            bool vivo = Cabe(cy, 40f, rec);"""
nuevo = """            var fuente = FontAssets.MouseText.Value;
            // v6.177 — EL BOTÓN GRANDE (la letra: «todos los textos del
            // altar son muy pequeños» + «los textos de los botones se
            // desbordan»): escala base 1.05 (antes 0.88) y el texto se
            // mide CON margen de halo (el contorno vive FUERA del área
            // medida) — nada desborda jamás y el botón se lee.
            float esc = LibroLineas.EscalaQueCabe(t ?? "", 1.05f, 202f);
            Rectangle b = new Rectangle((int)(pag.X + pag.Width / 2f - 113f), (int)cy, 226, 46);
            bool vivo = Cabe(cy, 46f, rec);"""
assert viejo in s, 'el botón del altar no se encontró'
s = s.replace(viejo, nuevo, 1)

viejo = """            string txt = t ?? "";
            float esc = LibroLineas.EscalaQueCabe(txt, 0.88f, 208f);
            Rectangle b = new Rectangle((int)(pag.X + pag.Width / 2f - 110f), (int)cy, 220, 40);
            bool vivo = cy + 40f <= rec.Bottom;"""
nuevo = """            string txt = t ?? "";
            // v6.177 — EL BOTÓN GRANDE (el pilar con la misma cura del
            // altar: escala 1.05 · medido con margen de halo · 46 de alto)
            float esc = LibroLineas.EscalaQueCabe(txt, 1.05f, 202f);
            Rectangle b = new Rectangle((int)(pag.X + pag.Width / 2f - 113f), (int)cy, 226, 46);
            bool vivo = cy + 46f <= rec.Bottom;"""
assert viejo in s, 'el botón del pilar no se encontró'
s = s.replace(viejo, nuevo, 1)

# las alturas de retorno de los botones (48 → 54) y las re-medidas
s = s.replace("""            if (hov && clicNuevo) alClic?.Invoke();
            return cy + 48f;""",
"""            if (hov && clicNuevo) alClic?.Invoke();
            return cy + 54f;""", 1)
s = s.replace("""            if (hov && clicNuevo) alClic?.Invoke();
            return cy + 48f;
        }""",
"""            if (hov && clicNuevo) alClic?.Invoke();
            return cy + 54f;
        }""")
# la re-medida del altar (esc, 206f → esc, 202f) y el alto del texto
s = s.replace("esc = LibroLineas.EscalaQueCabe(t ?? \"\", esc, 206f);",
              "esc = LibroLineas.EscalaQueCabe(t ?? \"\", esc, 202f);", 1)
s = s.replace("esc = LibroLineas.EscalaQueCabe(txt, esc, 206f);",
              "esc = LibroLineas.EscalaQueCabe(txt, esc, 202f);", 1)

# ── 4. TODAS LAS ESCALAS DEL ALTAR Y EL PILAR SUBEN ────────────────
# (la letra: «todos los textos del altar son muy pequeños»): las
#  llamadas Linea(..., 0.72-0.82, ...) suben +0.10; las Texto también
SUBE = {
    'At("EtiquetaArma"), cy, 0.82f': 'At("EtiquetaArma"), cy, 0.92f',
    'At("FiltroArma"), cy, 0.72f': 'At("FiltroArma"), cy, 0.84f',
    'At("OfrendaPrestada"), cy, 0.72f': 'At("OfrendaPrestada"), cy, 0.84f',
    '"DevoraLinea", arma.stack,': '"DevoraLinea", arma.stack,',
    'At("TrioSinTrio"), cy, 0.82f': 'At("TrioSinTrio"), cy, 0.92f',
    'At("TrioPista"), cy, 0.76f': 'At("TrioPista"), cy, 0.88f',
    'At("ComoCriaturas"), cy, 0.74f': 'At("ComoCriaturas"), cy, 0.86f',
    'At("ComoJefes"), cy, 0.74f': 'At("ComoJefes"), cy, 0.86f',
    'At("ComoOleadas"), cy, 0.74f': 'At("ComoOleadas"), cy, 0.86f',
    'At("TrioAlbedrioPista"), cy, 0.72f': 'At("TrioAlbedrioPista"), cy, 0.84f',
    'At("SelectorTrioTitulo"), abrirPara, 3': 'At("SelectorTrioTitulo"), abrirPara, 3',
}
for v, n in SUBE.items():
    if v in s:
        s = s.replace(v, n, 1)

# las líneas con Language.GetTextValue a escala fija (suben también)
s = s.replace('cy, 0.78f, Tinta);', 'cy, 0.88f, Tinta);')          # DevoraLinea / MundoProgreso
s = s.replace('cy, 0.78f, TintaApagado);', 'cy, 0.88f, TintaApagado);')  # SelloSinOfrenda
s = s.replace('cy, 0.8f, Tinta);', 'cy, 0.9f, Tinta);')            # SelloCuesta
s = s.replace('cy, 0.78f, TintaRojo);', 'cy, 0.88f, TintaRojo);')  # SelloFalta
s = s.replace('cy, 0.76f, Tinta);', 'cy, 0.88f, Tinta);')          # Gana*
s = s.replace('cy, 0.76f, TintaApagado);', 'cy, 0.88f, TintaApagado);')  # pistas del pilar

# el selector de nivel del pilar y la vitrina
s = s.replace('LibroLineas.EscalaQueCabe(nivelTxt, 0.92f, 180f);',
              'LibroLineas.EscalaQueCabe(nivelTxt, 1.02f, 172f);', 1)
s = s.replace('LibroLineas.EscalaQueCabe(poder, 0.76f, 92f);',
              'LibroLineas.EscalaQueCabe(poder, 0.86f, 96f);', 1)
s = s.replace('LibroLineas.EscalaQueCabe(nombreJefe, 0.86f, anchoNom);',
              'LibroLineas.EscalaQueCabe(nombreJefe, 0.96f, anchoNom);', 1)

# el trío: etiquetas y casillas
s = s.replace('TintaApagado, 0.76f, 0f, 0.5f);', 'TintaApagado, 0.86f, 0f, 0.5f);')  # etiqueta ranura
s = s.replace('marcada ? TintaVioleta : hovTxt ? Tinta : TintaApagado, 0.82f, 0f, 0.5f);',
              'marcada ? TintaVioleta : hovTxt ? Tinta : TintaApagado, 0.92f, 0f, 0.5f);')
s = s.replace('hovTodo ? Tinta : TintaApagado, 0.82f, 0.5f, 0.5f);',
              'hovTodo ? Tinta : TintaApagado, 0.92f, 0.5f, 0.5f);')
s = s.replace('float escT = LibroLineas.EscalaQueCabe(txt, 0.76f, 92f);',
              'float escT = LibroLineas.EscalaQueCabe(txt, 0.86f, 88f);', 1)
s = s.replace('!llena ? TintaApagado : viva ? TintaVioleta : Tinta, escT, 0f, 0.5f);',
              '!llena ? TintaApagado : viva ? TintaVioleta : Tinta, escT, 0f, 0.5f);')

# el número de las ranuras vacías del trío
s = s.replace('TintaApagado, 0.9f, 0.5f, 0.5f);', 'TintaApagado, 1.0f, 0.5f, 0.5f);', 1)

open(R, 'w', encoding='utf-8', newline='\n').write(s)
print(f'OK LibroDelEternoUI ({orig} → {len(s)} bytes)')

# ══ 5. LA VENTANA FLOTANTE: título grande + textos más grandes ════
R2 = 'AethonMod/Content/UI/SelectorFlotante.cs'
s = open(R2, encoding='utf-8').read()
orig = len(s)

s = s.replace("""        private const int AltoTitulo = 34;""",
"""        private const int AltoTitulo = 44;   // v6.177: el título GRANDE (la letra: «un texto muy pequeño y no se puede leer»)""", 1)
s = s.replace("""                float escT = LibroLineas.EscalaQueCabe(_titulo, 0.8f, r.Width - 24f);""",
"""                // v6.177 — EL TÍTULO SE LEE (la letra: «esta tiene un
                // texto muy pequeño y no se puede leer»): escala base 1.0
                float escT = LibroLineas.EscalaQueCabe(_titulo, 1.0f, r.Width - 24f);""", 1)
s = s.replace("""                    LibroLineas.Texto(sb, it.stack.ToString(),
                            new Vector2(celda.Right - 3f, celda.Bottom - 5f), Tinta, 0.68f, 1f, 0.85f);""",
"""                    LibroLineas.Texto(sb, it.stack.ToString(),
                            new Vector2(celda.Right - 3f, celda.Bottom - 5f), Tinta, 0.78f, 1f, 0.85f);""", 1)
s = s.replace("""                    float escN = LibroLineas.EscalaQueCabe(nombre, 0.8f, 260f);""",
"""                    float escN = LibroLineas.EscalaQueCabe(nombre, 0.92f, 260f);""", 1)
s = s.replace("""                    float escV = LibroLineas.EscalaQueCabe(vacio, 0.78f, r.Width - 30f);""",
"""                    float escV = LibroLineas.EscalaQueCabe(vacio, 0.9f, r.Width - 30f);""", 1)
s = s.replace("""                    float escP = LibroLineas.EscalaQueCabe(pista, 0.66f, r.Width - 24f);""",
"""                    float escP = LibroLineas.EscalaQueCabe(pista, 0.78f, r.Width - 24f);""", 1)

open(R2, 'w', encoding='utf-8', newline='\n').write(s)
print(f'OK SelectorFlotante ({orig} → {len(s)} bytes)')

# ══ 6. EL TÍTULO DEL TRÍO: «ELIGE UN ARMA» (la letra: lo otro sobra) ═
for idioma in ('es-ES', 'es-MX', 'en-US'):
    R3 = f'AethonMod/Localization/{idioma}_Mods.AethonMod.hjson'
    s = open(R3, encoding='utf-8').read()
    if idioma == 'en-US':
        viejo, nuevo = "SelectorTrioTitulo: THE ARSENAL — CHOOSE A WEAPON", "SelectorTrioTitulo: CHOOSE A WEAPON"
    else:
        viejo, nuevo = "SelectorTrioTitulo: EL ARSENAL — ELIGE UN ARMA", "SelectorTrioTitulo: ELIGE UN ARMA"
    assert viejo in s, f'el título del trío no está en {idioma}'
    s = s.replace(viejo, nuevo, 1)
    open(R3, 'w', encoding='utf-8', newline='\n').write(s)
    print(f'OK {idioma}: ELIGE UN ARMA')
