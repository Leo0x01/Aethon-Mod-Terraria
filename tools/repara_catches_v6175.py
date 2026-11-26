#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
repara_catches_v6175.py — EL LOOP DE RETROALIMENTACIÓN, paso 1 (v2 CORREGIDA).

LA LETRA: «crea un loop de retroalimentación para mejorar y depurar
errores». LA AUDITORÍA v6.174: 687 catches vacíos (53,8 % de 1.276) —
los errores morían en silencio (la causa de los botones muertos v6.173).

QUÉ HACE: reemplaza CADA catch vacío (con o sin cuerpo multilínea/comentario)
por un catch que REGISTRA el fallo (RegistroSistema.Fallo — deduplicado:
1ª vez = log + chat; repeticiones = contador). El contexto es el nombre
del archivo: el stack trace de la excepción trae el método.

LA LECCIÓN DE LA v1 (16 errores CS): el regex de los catches multilínea
podía casar una llave de cierre DENTRO DE UN COMENTARIO («try{End}») y
comerse media línea de comentario. LA CURA v2: primero se ELIMINAN los
comentarios (conservando offsets — cada carácter sustituido por espacio,
los saltos intactos), el regex corre sobre el texto LIMPIO (las llaves
de comentario ya no existen) y los reemplazos se aplican por POSICIÓN
sobre el archivo original.

Es IDEMPOTENTE: los catch ya convertidos (los que llaman a RegistroSistema)
no se tocan. Se puede correr mil veces.
"""
import re
import pathlib
import sys

RAIZ = pathlib.Path(__file__).resolve().parent.parent / "AethonMod" / "Content"

# catch vacío sobre texto SIN comentarios: llaves internas permitidas solo
# si están vacías/comentadas (los comentarios ya no existen en la vista limpia)
PAT_LIMPIO = re.compile(
    r'catch\s*(?:\(\s*(?:System\.)?Exception(?:\s+\w+)?\s*\))?\s*\{\s*\}')


def limpiar_comentarios(texto: str) -> str:
    """Devuelve el texto con comentarios sustituidos por espacios, MISMA
    longitud (los offsets calzan 1:1 con el original). No toca strings."""
    out = list(texto)
    i, n = 0, len(texto)
    while i < n:
        c = texto[i]
        if c == '/' and i + 1 < n and texto[i + 1] == '/':
            j = texto.find('\n', i)
            if j < 0:
                j = n
            for k in range(i, j):
                if texto[k] != '\n':
                    out[k] = ' '
            i = j
        elif c == '/' and i + 1 < n and texto[i + 1] == '*':
            j = texto.find('*/', i + 2)
            j = n if j < 0 else j + 2
            for k in range(i, j):
                if texto[k] != '\n':
                    out[k] = ' '
            i = j
        elif c == '"':
            # saltar strings (una llave dentro de un string no es estructural)
            i += 1
            while i < n:
                if texto[i] == '\\':
                    i += 2
                    continue
                if texto[i] == '"':
                    i += 1
                    break
                i += 1
        else:
            i += 1
    return ''.join(out)


def convertir(original: str, contexto: str):
    """Devuelve (texto_original_modificado, nº reemplazos)."""
    limpio = limpiar_comentarios(original)
    mapeo = list(PAT_LIMPIO.finditer(limpio))
    if not mapeo:
        return original, 0
    reemplazo = ('catch (System.Exception exRegistro) { global::AethonMod.'
                 'Content.Systems.RegistroSistema.Fallo("' + contexto + '", exRegistro); }')
    out = []
    pos = 0
    for m in mapeo:
        out.append(original[pos:m.start()])
        out.append(reemplazo)
        pos = m.end()
    out.append(original[pos:])
    return ''.join(out), len(mapeo)


def main():
    total, archivos = 0, 0
    for f in sorted(RAIZ.rglob('*.cs')):
        try:
            texto = f.read_text(encoding='utf-8')
        except Exception as e:
            print(f"LEER FALLÓ {f}: {e}")
            continue
        if not texto or 'class RegistroSistema' in texto:
            continue   # el propio registro: su catch vacío es el único sagrado
        nuevo, n = convertir(texto, f.stem)
        if n > 0:
            f.write_text(nuevo, encoding='utf-8')
            archivos += 1
            total += n
    print(f"CONVERTIDOS: {total} catches vacíos → RegistroSistema.Fallo en {archivos} archivos")

    # VERIFICACIÓN: en la vista LIMPIA no puede quedar ninguno (salvo el propio registro)
    quedan = 0
    for f in RAIZ.rglob('*.cs'):
        t = f.read_text(encoding='utf-8', errors='ignore')
        if 'class RegistroSistema' in t:
            continue
        quedan += len(PAT_LIMPIO.findall(limpiar_comentarios(t)))
    print(f"QUEDAN (vista limpia, sin RegistroSistema.cs): {quedan}")
    return 0 if quedan == 0 else 1


if __name__ == '__main__':
    sys.exit(main())
