#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
cirugia_v6176_ui.py — v6.176 — LA VENTANA DEL PILAR Y EL ALTAR (la
letra: «los textos en la ventana del pilar y el altar se desbordan,
especialmente en los botones, ademas el marcador del checklis que dice
que se usan las 3 armas esta mal, deberia ser igual a los otros
checklis donde el cuadrito se pinta de morado, ademas el color de los
textos es demasiado claro en la ventana del pilar y el altar»).

CUATRO CURAS:
  1. LAS TIJERAS DEL LIBRO: el pase del cuerpo corre con recorte de
     tijeras sobre las DOS páginas (transformadas por la matriz de UI)
     — LO QUE NO CABE EN EL PAPEL NO SE DIBUJA. El desborde es
     matemáticamente imposible (cualquiera que sea su causa: fuente
     dinámica, texto largo, argumentos crecidos).
  2. EL BOTÓN JAMÁS REBOSA: re-medida AL MOMENTO DEL TRAZO (la fuente
     MouseText de vanilla es DINÁMICA — puede crecer entre la medida y
     el dibujo; medir pegado al trazo cierra la ventana).
  3. EL COLOR: TintaApagado OSCURECE (118,96,70 → 82,60,40 — leía
     claro sobre el papel) y el halo blanco del texto se vuelve MANSO
     (grosor que escala con el texto + un punto de alfa menos: el
     borde separa la tinta del papel, no la lava).
  4. EL CHECKLIST UNIFORME: la casilla «LAS TRES A LA VEZ» pierde su
     palomita — vive EXACTAMENTE como las del albedrío: el cuadradito
     se RELLENA de morado.
"""
import pathlib
import sys

RAIZ = pathlib.Path(__file__).resolve().parent.parent
F = RAIZ / "AethonMod" / "Content" / "UI" / "LibroDelEternoUI.cs"

# ---------- (1) LAS TIJERAS (rasterizador compartido) ----------
V1 = """    /// <summary>El marco cosido del libro (doble línea — el borde de
    /// las páginas vivas de las pestañas nuevas) y la palomita de la casa.</summary>
    internal static class LibroMarco
    {"""

N1 = """    /// <summary>El marco cosido del libro (doble línea — el borde de
    /// las páginas vivas de las pestañas nuevas) y la palomita de la casa.</summary>
    internal static class LibroMarco
    {
        /// <summary>v6.176 — LAS TIJERAS del libro: el rasterizador con
        /// recorte para el pase del cuerpo — el papel manda (nada
        /// desborda la página; la letra: «los textos en la ventana del
        /// pilar y el altar se desbordan, especialmente en los
        /// botones»).</summary>
        internal static readonly Microsoft.Xna.Framework.Graphics.RasterizerState Tijeras =
            new Microsoft.Xna.Framework.Graphics.RasterizerState
            { CullMode = Microsoft.Xna.Framework.Graphics.CullMode.None, ScissorTestEnable = true };
"""

# ---------- (2) EL PASE DEL CUERPO CON TIJERAS ----------
V2 = """                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Main.UIScaleMatrix);"""

N2 = """                // v6.176 — EL PASE CON TIJERAS: el cuerpo del libro corre
                // recortado a las DOS PÁGINAS — lo que no cabe en el
                // papel NO SE DIBUJA (la letra: «los textos… se
                // desbordan, especialmente en los botones»). El recorte
                // se mide en píxeles de pantalla (la matriz de UI
                // aplicada a las esquinas de las páginas).
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    LibroMarco.Tijeras, null, Main.UIScaleMatrix);
                try
                {
                    Vector2 cTL = Vector2.Transform(new Vector2(
                        Math.Min(pagIzq.X, pagDer.X), Math.Min(pagIzq.Y, pagDer.Y)), Main.UIScaleMatrix);
                    Vector2 cBR = Vector2.Transform(new Vector2(
                        Math.Max(pagIzq.Right, pagDer.Right), Math.Max(pagIzq.Bottom, pagDer.Bottom)), Main.UIScaleMatrix);
                    var recortePag = new Rectangle((int)cTL.X, (int)cTL.Y,
                        (int)(cBR.X - cTL.X) + 2, (int)(cBR.Y - cTL.Y) + 2);
                    var vpPag = sb.GraphicsDevice.Viewport.Bounds;
                    if (recortePag.X < vpPag.X) { recortePag.Width -= vpPag.X - recortePag.X; recortePag.X = vpPag.X; }
                    if (recortePag.Y < vpPag.Y) { recortePag.Height -= vpPag.Y - recortePag.Y; recortePag.Y = vpPag.Y; }
                    if (recortePag.Right > vpPag.Right) recortePag.Width = vpPag.Right - recortePag.X;
                    if (recortePag.Bottom > vpPag.Bottom) recortePag.Height = vpPag.Bottom - recortePag.Y;
                    if (recortePag.Width < 0) recortePag.Width = 0;
                    if (recortePag.Height < 0) recortePag.Height = 0;
                    if (recortePag.Width > 0 && recortePag.Height > 0)
                        sb.GraphicsDevice.ScissorRectangle = recortePag;
                }
                catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("LibroDelEternoUI", exRegistro); }"""

# ---------- (3) LAS TIJERAS SE GUARDAN (antes de la X) ----------
V3 = """                var rectX = new Rectangle(x + w - 38, y + 6, 30, 30);"""

N3 = """                // v6.176 — LAS TIJERAS SE GUARDAN: la X de la tapa y el
                // selector flotante viven FUERA de las páginas (la tapa
                // y el aire) — el recorte vuelve a la pantalla entera.
                try { sb.GraphicsDevice.ScissorRectangle = sb.GraphicsDevice.Viewport.Bounds; }
                catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("LibroDelEternoUI", exRegistro); }

                var rectX = new Rectangle(x + w - 38, y + 6, 30, 30);"""

# ---------- (4) EL HALO MANSO ----------
V4 = """            Color halo = new Color(255, 250, 236);
            const float o = 1.1f;"""

N4 = """            // v6.176 — EL HALO MANSO (la letra: «el color de los textos
            // es demasiado claro en la ventana del pilar y el altar»):
            // el contorno blanco era GRUESO constante — a las escalas
            // menudas de los controles (0.45-0.88) el borde COMÍA el
            // glifo y la tinta leía clara. Ahora el grosor ESCALA con
            // el texto y el blanco pierde un punto de alfa: separa, no
            // lava.
            Color halo = new Color(255, 250, 236) * 0.82f;
            float o = Math.Clamp(esc * 1.15f, 0.55f, 1.1f);"""

# ---------- (5) EL CHECKLIST UNIFORME (MarcaTres) ----------
V5 = """            sb.Draw(TextureAssets.MagicPixel.Value, marca, marcada ? TintaVioleta * 0.30f : PapelSombra * 0.9f);
            LibroMarco.Marco(sb, marca, marcada || hovM ? TintaVioleta : TintaApagado * 0.6f);
            if (marcada) LibroMarco.Palomita(sb, marca, TintaVioleta);"""

N5 = """            // v6.176 — EL CHECKLIST UNIFORME (la letra: «el marcador del
            // checklis que dice que se usan las 3 armas esta mal,
            // deberia ser igual a los otros checklis donde el cuadrito
            // se pinta de morado»): la palomita MUERE — la casilla vive
            // EXACTAMENTE como las del albedrío: el cuadradito se
            // RELLENA de morado, sin símbolo encima.
            sb.Draw(TextureAssets.MagicPixel.Value, marca, marcada ? TintaVioleta * 0.85f : PapelSombra * 0.9f);
            LibroMarco.Marco(sb, marca, marcada || hovM ? TintaVioleta : TintaApagado * 0.6f);"""

# ---------- (6) EL BOTÓN DEL PILAR: re-medida al trazo ----------
V6 = """            LibroLineas.Texto(sb, txt, new Vector2(b.Center.X, b.Center.Y),
                apagado ? TintaApagado : Tinta, esc, 0.5f, 0.5f);
            if (hov && clicNuevo) alClic?.Invoke();
            return cy + 48f;
        }

        /// <summary>LA PÁGINA IZQUIERDA: la salida y las invocaciones.</summary>"""

N6 = """            // v6.176 — EL BOTÓN JAMÁS REBOSA: re-medida AL MOMENTO DEL
            // TRAZO (la fuente MouseText de vanilla es dinámica — puede
            // crecer entre la medida y el dibujo; medir pegado al trazo
            // cierra la ventana y las tijeras del pase lo blindan).
            esc = LibroLineas.EscalaQueCabe(txt, esc, 206f);
            LibroLineas.Texto(sb, txt, new Vector2(b.Center.X, b.Center.Y),
                apagado ? TintaApagado : Tinta, esc, 0.5f, 0.5f);
            if (hov && clicNuevo) alClic?.Invoke();
            return cy + 48f;
        }

        /// <summary>LA PÁGINA IZQUIERDA: la salida y las invocaciones.</summary>"""

# ---------- (7) EL BOTÓN DEL ALTAR: re-medida al trazo ----------
V7 = """            LibroLineas.Texto(sb, t ?? "", new Vector2(b.Center.X, b.Center.Y),
                apagado ? TintaApagado : Tinta, esc, 0.5f, 0.5f);
            if (hov && clicNuevo) alClic?.Invoke();
            return cy + 48f;
        }

        /// <summary>LA PÁGINA IZQUIERDA: la ofrenda y su mundo.</summary>"""

N7 = """            // v6.176 — EL BOTÓN JAMÁS REBOSA (re-medida al momento del
            // trazo — la ventana de la fuente dinámica se cierra aquí)
            esc = LibroLineas.EscalaQueCabe(t ?? "", esc, 206f);
            LibroLineas.Texto(sb, t ?? "", new Vector2(b.Center.X, b.Center.Y),
                apagado ? TintaApagado : Tinta, esc, 0.5f, 0.5f);
            if (hov && clicNuevo) alClic?.Invoke();
            return cy + 48f;
        }

        /// <summary>LA PÁGINA IZQUIERDA: la ofrenda y su mundo.</summary>"""

# ---------- (8) EL COLOR OSCURO: PILAR ----------
V8 = """        private static readonly Color TintaApagado = new(118, 96, 70);
        private static readonly Color TintaRojo = new(140, 40, 40);
        private static readonly Color PapelSombra = new(216, 200, 166);"""

N8 = """        private static readonly Color TintaApagado = new(82, 60, 40);   // v6.176: oscurecido (leía claro sobre el papel)
        private static readonly Color TintaRojo = new(140, 40, 40);
        private static readonly Color PapelSombra = new(216, 200, 166);"""

# ---------- (9) EL COLOR OSCURO: ALTAR ----------
V9 = """        private static readonly Color TintaApagado = new(118, 96, 70);
        private static readonly Color TintaRojo = new(140, 40, 40);
        private static readonly Color PapelSombra = new(190, 172, 134);"""

N9 = """        private static readonly Color TintaApagado = new(82, 60, 40);   // v6.176: oscurecido (leía claro sobre el papel)
        private static readonly Color TintaRojo = new(140, 40, 40);
        private static readonly Color PapelSombra = new(190, 172, 134);"""


def main() -> int:
    t = F.read_text(encoding="utf-8")
    ya = sum(1 for v in (N1, N2, N3, N4, N5, N6, N7, N8, N9) if v in t)
    if ya:
        print(f"YA APLICADO parcialmente ({ya}/9) — se aborta")
        return 1 if ya < 9 else 0
    for etiqueta, viejo, nuevo in [
        ("las tijeras (rasterizador)", V1, N1),
        ("el pase del cuerpo con recorte", V2, N2),
        ("tijeras se guardan (la X)", V3, N3),
        ("el halo manso", V4, N4),
        ("checklist uniforme", V5, N5),
        ("botón del pilar", V6, N6),
        ("botón del altar", V7, N7),
        ("color oscuro pilar", V8, N8),
        ("color oscuro altar", V9, N9),
    ]:
        assert viejo in t, f"PATRÓN NO ENCONTRADO: {etiqueta}"
        t = t.replace(viejo, nuevo, 1)
        print(f"✔ {etiqueta}")
    F.write_text(t, encoding="utf-8")
    print("✔ LibroDelEternoUI: tijeras + botones a prueba de rebose + colores + checklist")
    return 0


if __name__ == "__main__":
    sys.exit(main())
