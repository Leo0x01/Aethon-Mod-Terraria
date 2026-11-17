#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""correccion_oraculo_v6151.py — los dos fixes del oráculo:
   (1) LibroDelEternoUI: el using ReLogic.Graphics (la extensión
       DrawString de DynamicSpriteFont) + la fuente por parámetro.
   (2) GlowUILib: los using que faltan (Terraria.ModLoader para
       ModContent; System para MathF)."""
import os
RAIZ = "/home/z/my-project/mod/AethonMod"
os.chdir(RAIZ)

def leer(r):
    with open(r, encoding="utf-8") as f: return f.read()
def escribir(r, s):
    with open(r, "w", encoding="utf-8") as f: f.write(s)

# --- (1) el using de la extensión en LibroDelEternoUI ---
s = leer("Content/UI/LibroDelEternoUI.cs")
viejo = "using Terraria.UI;"
assert viejo in s, "using Terraria.UI no está"
s = s.replace(viejo, "using Terraria.UI;\nusing ReLogic.Graphics;", 1)
escribir("Content/UI/LibroDelEternoUI.cs", s)
print("  ✔ using ReLogic.Graphics en LibroDelEternoUI")

# --- (2) los using de GlowUILib ---
s = leer("Content/VFX/GlowUILib.cs")
viejo = "using Microsoft.Xna.Framework;\nusing Microsoft.Xna.Framework.Graphics;\nusing Terraria;\nusing Terraria.UI;"
assert viejo in s, "cabecera de GlowUILib no calza"
s = s.replace(viejo,
    "using System;\nusing Microsoft.Xna.Framework;\nusing Microsoft.Xna.Framework.Graphics;\nusing Terraria;\nusing Terraria.ModLoader;\nusing Terraria.UI;", 1)
escribir("Content/VFX/GlowUILib.cs", s)
print("  ✔ using System + ModLoader en GlowUILib")

print("CORRECCIONES OK")
