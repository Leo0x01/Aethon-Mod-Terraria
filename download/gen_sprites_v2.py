#!/usr/bin/env python3
"""
Genera sprites pixel-art de alta calidad para el mod Aethon.
Estilo Terraria: paleta limitada, contornos oscuros, 2x scale.
Tema cósmico: dorado estelar, violeta arcano, naranja solar, teal vacío.
"""
from PIL import Image, ImageDraw
import os

BASE = "/home/z/my-project/AethonMod/Content"

# Paleta cósmica
GOLD = (245, 196, 81)
GOLD_DARK = (180, 130, 40)
GOLD_LIGHT = (255, 230, 140)
GOLD_BRIGHT = (255, 250, 200)
VIOLET = (179, 136, 255)
VIOLET_DARK = (110, 70, 180)
VIOLET_LIGHT = (210, 180, 255)
ORANGE = (255, 154, 60)
ORANGE_DARK = (180, 90, 20)
ORANGE_LIGHT = (255, 200, 120)
TEAL = (61, 214, 196)
TEAL_DARK = (30, 140, 130)
TEAL_LIGHT = (150, 240, 220)
WHITE = (255, 255, 255)
BLACK = (20, 15, 30)
OUTLINE = (10, 8, 18)
TRANSPARENT = (0, 0, 0, 0)

def new_canvas(w, h):
    return Image.new("RGBA", (w, h), TRANSPARENT)

def px(img, x, y, color):
    """Dibuja un pixel."""
    img.putpixel((x, y), color)

def rect(img, x, y, w, h, color):
    """Dibuja un rectángulo relleno."""
    for dy in range(h):
        for dx in range(w):
            if 0 <= x+dx < img.width and 0 <= y+dy < img.height:
                img.putpixel((x+dx, y+dy), color)

def outline_rect(img, x, y, w, h, color):
    """Dibuja el borde de un rectángulo."""
    for dx in range(w):
        img.putpixel((x+dx, y), color)
        img.putpixel((x+dx, y+h-1), color)
    for dy in range(h):
        img.putpixel((x, y+dy), color)
        img.putpixel((x+w-1, y+dy), color)

def circle(img, cx, cy, r, color, filled=True):
    """Dibuja un círculo pixel-art."""
    for y in range(-r, r+1):
        for x in range(-r, r+1):
            d = (x*x + y*y) ** 0.5
            if d <= r:
                if 0 <= cx+x < img.width and 0 <= cy+y < img.height:
                    img.putpixel((cx+x, cy+y), color)
            elif d <= r + 0.5 and not filled:
                if 0 <= cx+x < img.width and 0 <= cy+y < img.height:
                    img.putpixel((cx+x, cy+y), color)

def outline_circle(img, cx, cy, r, color):
    """Dibuja el borde de un círculo."""
    for y in range(-r, r+1):
        for x in range(-r, r+1):
            d = (x*x + y*y) ** 0.5
            if r - 0.5 <= d <= r + 0.5:
                if 0 <= cx+x < img.width and 0 <= cy+y < img.height:
                    img.putpixel((cx+x, cy+y), color)

def outline_sprite(img):
    """Añade contorno oscuro a los pixels no transparentes."""
    w, h = img.size
    pixels = img.load()
    result = img.copy()
    rp = result.load()
    for y in range(h):
        for x in range(w):
            if pixels[x, y][3] > 0:
                for dx, dy in [(-1,0),(1,0),(0,-1),(0,1),(-1,-1),(1,-1),(-1,1),(1,1)]:
                    nx, ny = x+dx, y+dy
                    if 0 <= nx < w and 0 <= ny < h:
                        if pixels[nx, ny][3] == 0:
                            rp[nx, ny] = OUTLINE + (255,)
    return result

def scale2x(img):
    """Escala 2x con nearest neighbor."""
    w, h = img.size
    return img.resize((w*2, h*2), Image.NEAREST)

def save(img, path):
    img.save(path)
    print(f"  ✓ {path} ({img.size[0]}x{img.size[1]})")

# ====================================================================
# ITEMS (32x32 finales, 16x16 base × 2)
# ====================================================================

def sprite_genesis_shard():
    """Fragmento Génesis — orbe de luz brillante con halo estelar."""
    img = new_canvas(24, 24)
    # Núcleo brillante blanco
    circle(img, 12, 12, 3, WHITE)
    circle(img, 12, 12, 2, GOLD_BRIGHT)
    # Halo dorado
    circle(img, 12, 12, 5, GOLD)
    circle(img, 12, 12, 4, GOLD_LIGHT)
    circle(img, 12, 12, 3, WHITE)
    # Estrellas alrededor (4 puntas)
    for angle in [0, 90, 180, 270]:
        import math
        rad = math.radians(angle)
        x = 12 + int(math.cos(rad) * 7)
        y = 12 + int(math.sin(rad) * 7)
        if 0 <= x < 24 and 0 <= y < 24:
            img.putpixel((x, y), GOLD)
            img.putpixel((x+1, y), GOLD_LIGHT)
            img.putpixel((x, y+1), GOLD_LIGHT)
    # Destellos diagonales
    img.putpixel((7, 7), GOLD_LIGHT)
    img.putpixel((16, 7), GOLD_LIGHT)
    img.putpixel((7, 16), GOLD_LIGHT)
    img.putpixel((16, 16), GOLD_LIGHT)
    img = outline_sprite(img)
    save(img, f"{BASE}/Items/GenesisShard.png")

def sprite_resonance_shard():
    """Fragmento de Resonancia — cristal dorado facetado."""
    img = new_canvas(20, 20)
    # Forma de diamante
    for y in range(20):
        for x in range(20):
            dx = abs(x - 10)
            dy = abs(y - 10)
            if dx + dy <= 7:
                if dx + dy <= 3:
                    img.putpixel((x, y), GOLD_BRIGHT)
                elif dx + dy <= 5:
                    img.putpixel((x, y), GOLD_LIGHT)
                else:
                    img.putpixel((x, y), GOLD)
    # Brillo superior izquierdo
    img.putpixel((8, 7), WHITE)
    img.putpixel((9, 8), WHITE)
    img = outline_sprite(img)
    save(img, f"{BASE}/Items/ResonanceShard.png")

def sprite_memory_rune():
    """Runa de Memoria — piedra rúnica violeta con estrella."""
    img = new_canvas(24, 24)
    # Fondo de piedra
    rect(img, 4, 4, 16, 16, VIOLET_DARK)
    rect(img, 5, 5, 14, 14, VIOLET)
    # Borde dorado
    outline_rect(img, 4, 4, 16, 16, GOLD)
    # Estrella central (4 puntas)
    img.putpixel((11, 10), WHITE)
    img.putpixel((12, 10), WHITE)
    img.putpixel((10, 11), VIOLET_LIGHT)
    img.putpixel((11, 11), WHITE)
    img.putpixel((12, 11), WHITE)
    img.putpixel((13, 11), VIOLET_LIGHT)
    img.putpixel((11, 12), WHITE)
    img.putpixel((12, 12), WHITE)
    img.putpixel((10, 12), VIOLET_LIGHT)
    img.putpixel((13, 12), VIOLET_LIGHT)
    img.putpixel((11, 13), VIOLET_LIGHT)
    img.putpixel((12, 13), VIOLET_LIGHT)
    # Runas en las esquinas
    img.putpixel((6, 6), GOLD_LIGHT)
    img.putpixel((17, 6), GOLD_LIGHT)
    img.putpixel((6, 17), GOLD_LIGHT)
    img.putpixel((17, 17), GOLD_LIGHT)
    img = outline_sprite(img)
    save(img, f"{BASE}/Items/MemoryRune.png")

def sprite_altar_item():
    """Item del altar — mini altar con cristal."""
    img = new_canvas(24, 24)
    # Base de piedra
    rect(img, 4, 14, 16, 6, (60, 45, 90))
    rect(img, 5, 15, 14, 4, (80, 60, 120))
    rect(img, 4, 14, 16, 1, (100, 80, 150))
    # Cristal flotante
    for y in range(8):
        for x in range(8):
            dx = abs(x - 4)
            dy = abs(y - 4)
            if dx + dy <= 4:
                if dx + dy <= 1:
                    img.putpixel((8+x, 2+y), WHITE)
                elif dx + dy <= 2:
                    img.putpixel((8+x, 2+y), VIOLET_LIGHT)
                else:
                    img.putpixel((8+x, 2+y), VIOLET)
    # Brillo del cristal
    img.putpixel((10, 4), WHITE)
    img = outline_sprite(img)
    save(img, f"{BASE}/Items/Placeables/AncientAltarItem.png")

# ====================================================================
# ARMAS (48x48 finales, 24x24 base × 2)
# ====================================================================

def sprite_lumina_starbow():
    """Lumina — arco cósmico dorado con estrella central."""
    img = new_canvas(28, 36)
    # Cuerpo del arco (curva dorada)
    for y in range(4, 32):
        t = (y - 4) / 28.0
        offset = int(5 * (1 - abs(t * 2 - 1)))
        # Lado izquierdo
        px(img, 10 - offset, y, GOLD)
        px(img, 11 - offset, y, GOLD_LIGHT if t < 0.5 else GOLD_DARK)
        # Lado derecho
        px(img, 16 + offset, y, GOLD)
        px(img, 17 + offset, y, GOLD_LIGHT if t < 0.5 else GOLD_DARK)
    # Cuerda
    for y in range(5, 31):
        px(img, 13, y, WHITE)
    # Agarre central
    rect(img, 12, 17, 4, 4, GOLD_DARK)
    rect(img, 13, 18, 2, 2, GOLD)
    # Estrella en el centro
    px(img, 14, 19, WHITE)
    # Brillos en las puntas
    px(img, 6, 6, GOLD_BRIGHT)
    px(img, 21, 6, GOLD_BRIGHT)
    px(img, 6, 29, GOLD_BRIGHT)
    px(img, 21, 29, GOLD_BRIGHT)
    img = outline_sprite(img)
    save(img, f"{BASE}/Weapons/LuminaStarbow.png")

def sprite_solbrand_edge():
    """Solbrand — espada solar naranja con hoja de luz."""
    img = new_canvas(28, 36)
    # Hoja (triangular)
    for y in range(2, 22):
        w = max(1, 5 - (y - 2) // 5)
        cx = 14
        for x in range(-w, w+1):
            if 0 <= cx+x < 28:
                if y < 8:
                    px(img, cx+x, y, ORANGE_LIGHT)
                elif y < 16:
                    px(img, cx+x, y, ORANGE)
                else:
                    px(img, cx+x, y, ORANGE_DARK)
        # Línea central blanca
        px(img, cx, y, WHITE)
    # Guarda
    rect(img, 9, 22, 11, 3, ORANGE_DARK)
    rect(img, 10, 22, 9, 1, ORANGE)
    # Mango
    rect(img, 12, 25, 5, 7, (100, 60, 20))
    rect(img, 13, 26, 3, 5, (120, 70, 30))
    # Pomo dorado
    rect(img, 11, 32, 7, 3, GOLD)
    rect(img, 12, 33, 5, 1, GOLD_LIGHT)
    # Brillo en la punta
    px(img, 14, 2, WHITE)
    px(img, 13, 3, GOLD_BRIGHT)
    px(img, 15, 3, GOLD_BRIGHT)
    img = outline_sprite(img)
    save(img, f"{BASE}/Weapons/SolbrandEdge.png")

def sprite_grimoire_eternal():
    """Grimorio — libro mágico violeta con estrella cósmica."""
    img = new_canvas(30, 28)
    # Cuerpo del libro
    rect(img, 5, 5, 20, 18, VIOLET_DARK)
    rect(img, 6, 6, 18, 16, VIOLET)
    # Lomo
    rect(img, 5, 5, 3, 18, VIOLET_DARK)
    rect(img, 6, 6, 1, 16, (90, 50, 150))
    # Borde dorado
    outline_rect(img, 5, 5, 20, 18, GOLD)
    rect(img, 5, 5, 20, 1, GOLD)
    rect(img, 5, 22, 20, 1, GOLD)
    # Símbolo cósmico central (estrella)
    cx, cy = 16, 13
    px(img, cx, cy-2, WHITE)
    px(img, cx-1, cy-1, VIOLET_LIGHT)
    px(img, cx, cy-1, WHITE)
    px(img, cx+1, cy-1, VIOLET_LIGHT)
    rect(img, cx-1, cy, 3, 2, VIOLET_LIGHT)
    px(img, cx, cy+2, VIOLET_LIGHT)
    # Destellos
    px(img, 9, 9, VIOLET_LIGHT)
    px(img, 22, 9, VIOLET_LIGHT)
    px(img, 9, 19, VIOLET_LIGHT)
    px(img, 22, 19, VIOLET_LIGHT)
    # Runa dorada en la portada
    px(img, 8, 7, GOLD_LIGHT)
    px(img, 23, 7, GOLD_LIGHT)
    img = outline_sprite(img)
    save(img, f"{BASE}/Weapons/GrimoireEternal.png")

# ====================================================================
# PROYECTILES (20x20 finales, 10x10 base × 2)
# ====================================================================

def sprite_starlight_arrow():
    """Flecha de luz estelar dorada."""
    img = new_canvas(14, 14)
    # Punta
    px(img, 10, 6, GOLD_LIGHT)
    px(img, 10, 7, GOLD_LIGHT)
    px(img, 11, 6, WHITE)
    px(img, 11, 7, WHITE)
    # Cuerpo
    rect(img, 5, 6, 5, 2, GOLD)
    # Plumaje
    px(img, 3, 5, GOLD_LIGHT)
    px(img, 3, 8, GOLD_LIGHT)
    px(img, 4, 6, GOLD)
    px(img, 4, 7, GOLD)
    # Brillo
    px(img, 6, 6, WHITE)
    img = outline_sprite(img)
    save(img, f"{BASE}/Weapons/Projectiles/StarlightArrow.png")

def sprite_dawn_slash():
    """Onda de corte solar naranja."""
    img = new_canvas(20, 20)
    # Arco cósmico
    for angle_deg in range(200, 340, 5):
        import math
        rad = math.radians(angle_deg)
        x = 10 + int(math.cos(rad) * 7)
        y = 10 + int(math.sin(rad) * 7)
        if 0 <= x < 20 and 0 <= y < 20:
            px(img, x, y, ORANGE)
    for angle_deg in range(210, 330, 5):
        rad = math.radians(angle_deg)
        x = 10 + int(math.cos(rad) * 5)
        y = 10 + int(math.sin(rad) * 5)
        if 0 <= x < 20 and 0 <= y < 20:
            px(img, x, y, ORANGE_LIGHT)
    for angle_deg in range(220, 320, 5):
        rad = math.radians(angle_deg)
        x = 10 + int(math.cos(rad) * 3)
        y = 10 + int(math.sin(rad) * 3)
        if 0 <= x < 20 and 0 <= y < 20:
            px(img, x, y, WHITE)
    # Destello
    px(img, 15, 5, GOLD_BRIGHT)
    img = outline_sprite(img)
    save(img, f"{BASE}/Weapons/Projectiles/DawnSlash.png")

def sprite_arcane_bolt():
    """Bolt arcano violeta con núcleo blanco."""
    img = new_canvas(14, 14)
    # Halo
    circle(img, 7, 7, 5, VIOLET_DARK)
    # Cuerpo
    circle(img, 7, 7, 4, VIOLET)
    circle(img, 7, 7, 3, VIOLET_LIGHT)
    # Núcleo
    circle(img, 7, 7, 2, WHITE)
    # Destellos
    px(img, 3, 7, VIOLET_LIGHT)
    px(img, 11, 7, VIOLET_LIGHT)
    px(img, 7, 3, VIOLET_LIGHT)
    px(img, 7, 11, VIOLET_LIGHT)
    img = outline_sprite(img)
    save(img, f"{BASE}/Weapons/Projectiles/ArcaneBolt.png")

# ====================================================================
# NPCs (48-80px, más grandes)
# ====================================================================

def sprite_aethon_boss():
    """Aethon — galaxia con núcleo dorado y estrellas orbitando."""
    img = new_canvas(48, 48)
    cx, cy = 24, 24
    # Aura exterior
    circle(img, cx, cy, 22, (30, 20, 50))
    circle(img, cx, cy, 18, (50, 30, 80))
    circle(img, cx, cy, 14, VIOLET_DARK)
    circle(img, cx, cy, 10, VIOLET)
    # Núcleo dorado
    circle(img, cx, cy, 6, GOLD)
    circle(img, cx, cy, 4, GOLD_LIGHT)
    circle(img, cx, cy, 2, WHITE)
    # Estrellas orbitando
    import math
    for i in range(8):
        angle = i * math.pi / 4
        x = cx + int(math.cos(angle) * 16)
        y = cy + int(math.sin(angle) * 16)
        if 0 <= x < 48 and 0 <= y < 48:
            px(img, x, y, WHITE)
            px(img, x+1, y, GOLD_LIGHT)
            px(img, x, y+1, GOLD_LIGHT)
    # Brazos espirales
    for i in range(12):
        angle = i * math.pi / 6
        r = 8 + i
        x = cx + int(math.cos(angle) * r)
        y = cy + int(math.sin(angle) * r)
        if 0 <= x < 48 and 0 <= y < 48:
            px(img, x, y, VIOLET_LIGHT)
    img = outline_sprite(img)
    save(img, f"{BASE}/NPCs/AethonBoss.png")

def sprite_hollow_titan():
    """Titán Hueco — coloso de cristal teal."""
    img = new_canvas(40, 56)
    # Cuerpo cristalino
    for y in range(4, 52):
        for x in range(8, 32):
            dx = abs(x - 20)
            # Forma de cuerpo
            if y < 16:
                w = 4 + (y - 4) // 3
            elif y < 40:
                w = 8
            else:
                w = max(2, 8 - (y - 40) // 3)
            if dx <= w:
                if dx <= 2:
                    px(img, x, y, TEAL_LIGHT)
                elif dx <= 5:
                    px(img, x, y, TEAL)
                else:
                    px(img, x, y, TEAL_DARK)
    # Ojos brillantes
    rect(img, 15, 18, 3, 3, WHITE)
    rect(img, 22, 18, 3, 3, WHITE)
    px(img, 16, 19, TEAL)
    px(img, 23, 19, TEAL)
    # Grietas de cristal
    px(img, 18, 28, TEAL_LIGHT)
    px(img, 19, 29, TEAL_LIGHT)
    px(img, 20, 30, TEAL_LIGHT)
    px(img, 21, 31, TEAL_LIGHT)
    img = outline_sprite(img)
    save(img, f"{BASE}/NPCs/HollowTitan.png")

def sprite_rift_keeper():
    """Guardián del Rift — figura teal con grietas violeta."""
    img = new_canvas(30, 44)
    # Cuerpo
    rect(img, 10, 8, 12, 32, TEAL_DARK)
    rect(img, 11, 9, 10, 30, TEAL)
    # Cabeza
    rect(img, 11, 4, 10, 6, TEAL_DARK)
    rect(img, 12, 5, 8, 4, TEAL)
    # Ojos
    px(img, 13, 7, TEAL_LIGHT)
    px(img, 17, 7, TEAL_LIGHT)
    # Grietas de rift (violeta)
    px(img, 15, 12, VIOLET_LIGHT)
    px(img, 14, 14, VIOLET_LIGHT)
    px(img, 16, 16, VIOLET_LIGHT)
    px(img, 15, 20, VIOLET_LIGHT)
    px(img, 14, 24, VIOLET_LIGHT)
    px(img, 16, 28, VIOLET_LIGHT)
    img = outline_sprite(img)
    save(img, f"{BASE}/NPCs/RiftKeeper.png")

def sprite_echo_blade():
    """Eco del Primer Portador — sombra espectral con espada."""
    img = new_canvas(26, 40)
    # Silueta oscura
    rect(img, 9, 6, 8, 28, (40, 30, 50))
    rect(img, 10, 7, 6, 26, (60, 40, 80))
    # Cabeza
    circle(img, 13, 5, 4, (40, 30, 50))
    circle(img, 13, 5, 3, (60, 40, 80))
    # Espada fantasma
    rect(img, 19, 12, 2, 16, VIOLET_LIGHT)
    px(img, 20, 11, WHITE)
    # Ojos brillantes
    px(img, 12, 4, ORANGE)
    px(img, 14, 4, ORANGE)
    # Aura fantasma
    px(img, 8, 10, VIOLET)
    px(img, 8, 20, VIOLET)
    px(img, 18, 30, VIOLET)
    img = outline_sprite(img)
    save(img, f"{BASE}/NPCs/EchoBlade.png")

def sprite_echo_archer():
    """Eco de la Arquera — fantasma con arco."""
    img = new_canvas(26, 40)
    # Silueta
    rect(img, 9, 6, 6, 26, (40, 35, 50))
    rect(img, 10, 7, 4, 24, (70, 60, 90))
    circle(img, 12, 5, 3, (40, 35, 50))
    # Arco fantasma
    for y in range(8, 30):
        t = (y - 8) / 22.0
        offset = int(3 * (1 - abs(t * 2 - 1)))
        px(img, 17 - offset, y, GOLD)
        px(img, 18 + offset, y, GOLD)
    # Ojos
    px(img, 11, 4, GOLD)
    px(img, 13, 4, GOLD)
    img = outline_sprite(img)
    save(img, f"{BASE}/NPCs/EchoArcher.png")

def sprite_witness():
    """El Testigo — entidad flotante con ojo cósmico."""
    img = new_canvas(24, 36)
    # Manto
    for y in range(2, 34):
        for x in range(4, 20):
            dx = abs(x - 12)
            if y < 12:
                w = 4 + (y - 2) // 3
            elif y < 28:
                w = 8
            else:
                w = max(2, 8 - (y - 28) // 2)
            if dx <= w:
                if dx <= 2:
                    px(img, x, y, VIOLET)
                else:
                    px(img, x, y, VIOLET_DARK)
    # Ojo central
    circle(img, 12, 12, 4, WHITE)
    circle(img, 12, 12, 3, GOLD)
    circle(img, 12, 12, 2, GOLD_DARK)
    px(img, 12, 12, BLACK)
    # Brillo
    px(img, 12, 8, GOLD_LIGHT)
    px(img, 8, 12, VIOLET_LIGHT)
    px(img, 16, 12, VIOLET_LIGHT)
    img = outline_sprite(img)
    save(img, f"{BASE}/NPCs/TheWitness.png")

# ====================================================================
# TILES
# ====================================================================

def sprite_ancient_altar_tile():
    """Altar Antiguo — tile 3x2 con cristal brillante."""
    img = new_canvas(54, 36)
    # Base de piedra
    rect(img, 4, 16, 46, 16, (60, 45, 90))
    rect(img, 6, 18, 42, 12, (80, 60, 120))
    rect(img, 4, 16, 46, 2, (100, 75, 150))
    # Runas grabadas
    for x in [10, 18, 26, 34, 42]:
        px(img, x, 24, GOLD)
        px(img, x+1, 24, GOLD_LIGHT)
    # Cristal central flotante
    for y in range(8):
        for x in range(8):
            dx = abs(x - 4)
            dy = abs(y - 4)
            if dx + dy <= 4:
                if dx + dy <= 1:
                    px(img, 23+x, 2+y, WHITE)
                elif dx + dy <= 2:
                    px(img, 23+x, 2+y, VIOLET_LIGHT)
                else:
                    px(img, 23+x, 2+y, VIOLET)
    # Brillo del cristal
    px(img, 26, 5, WHITE)
    img = outline_sprite(img)
    save(img, f"{BASE}/Tiles/AncientAltar.png")

# ====================================================================
# GENERAR TODO
# ====================================================================

print("Generando sprites pixel-art de alta calidad...\n")

print("Items:")
sprite_genesis_shard()
sprite_resonance_shard()
sprite_memory_rune()
sprite_altar_item()

print("\nArmas:")
sprite_lumina_starbow()
sprite_solbrand_edge()
sprite_grimoire_eternal()

print("\nProyectiles:")
sprite_starlight_arrow()
sprite_dawn_slash()
sprite_arcane_bolt()

print("\nNPCs:")
sprite_aethon_boss()
sprite_hollow_titan()
sprite_rift_keeper()
sprite_echo_blade()
sprite_echo_archer()
sprite_witness()

print("\nTiles:")
sprite_ancient_altar_tile()

print("\nIcono del mod:")
# Icono 80x80
img = new_canvas(80, 80)
# Fondo
rect(img, 0, 0, 80, 80, (13, 10, 26))
# Aura
circle(img, 40, 40, 30, (30, 20, 50))
circle(img, 40, 40, 22, (50, 30, 80))
circle(img, 40, 40, 15, VIOLET_DARK)
# Núcleo
circle(img, 40, 40, 8, GOLD)
circle(img, 40, 40, 5, GOLD_LIGHT)
circle(img, 40, 40, 2, WHITE)
# Estrellas
import math
for i in range(12):
    angle = i * math.pi / 6
    x = 40 + int(math.cos(angle) * 25)
    y = 40 + int(math.sin(angle) * 25)
    if 0 <= x < 80 and 0 <= y < 80:
        px(img, x, y, WHITE)
        px(img, x+1, y, GOLD_LIGHT)
img = outline_sprite(img)
save(img, "/home/z/my-project/AethonMod/icon.png")

print(f"\n✅ Todos los sprites generados en {BASE}/")
