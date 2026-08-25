#!/usr/bin/env python3
"""
Genera todos los sprites pixel-art del mod Aethon.
Estilo Terraria: paleta limitada, contornos oscuros, escala 2x (32x32 final).
Tema cósmico: dorado estelar, violeta arcano, naranja solar, teal vacío.
"""
from PIL import Image, ImageDraw
import os

BASE = "/home/z/my-project/AethonMod/Textures"
os.makedirs(f"{BASE}/Items", exist_ok=True)
os.makedirs(f"{BASE}/Weapons", exist_ok=True)
os.makedirs(f"{BASE}/Projectiles", exist_ok=True)
os.makedirs(f"{BASE}/NPCs", exist_ok=True)
os.makedirs(f"{BASE}/Tiles", exist_ok=True)
os.makedirs(f"{BASE}/UI", exist_ok=True)

# Paleta cósmica
GOLD = (245, 196, 81)
GOLD_DARK = (180, 130, 40)
GOLD_LIGHT = (255, 230, 140)
VIOLET = (179, 136, 255)
VIOLET_DARK = (110, 70, 180)
VIOLET_LIGHT = (210, 180, 255)
ORANGE = (255, 154, 60)
ORANGE_DARK = (180, 90, 20)
ORANGE_LIGHT = (255, 200, 120)
TEAL = (61, 214, 196)
TEAL_DARK = (30, 140, 130)
WHITE = (255, 255, 255)
BLACK = (20, 15, 30)
OUTLINE = (10, 8, 18)
TRANSPARENT = (0, 0, 0, 0)
DARK_BG = (13, 10, 26)

def new_canvas(w, h, bg=TRANSPARENT):
    return Image.new("RGBA", (w, h), bg)

def pixel(draw, x, y, color, scale=1):
    """Dibuja un pixel escalado."""
    draw.rectangle([x*scale, y*scale, x*scale+scale-1, y*scale+scale-1], fill=color)

def outline_sprite(img, scale=2):
    """Añade contorno oscuro a los pixels no transparentes (estilo Terraria)."""
    w, h = img.size
    pixels = img.load()
    outline_img = img.copy()
    op = outline_img.load()
    for y in range(h):
        for x in range(w):
            if pixels[x, y][3] > 0:  # no transparente
                # verificar vecinos transparentes
                for dx, dy in [(-1,0),(1,0),(0,-1),(0,1)]:
                    nx, ny = x+dx, y+dy
                    if 0 <= nx < w and 0 <= ny < h:
                        if pixels[nx, ny][3] == 0:
                            op[nx, ny] = OUTLINE + (255,)
    return outline_img

def save_sprite(img, path, scale_up=1):
    """Guarda el sprite, escalado y con contorno."""
    if scale_up > 1:
        w, h = img.size
        img = img.resize((w*scale_up, h*scale_up), Image.NEAREST)
    img.save(path)
    print(f"  ✓ {path} ({img.size[0]}x{img.size[1]})")

# ====================================================================
# ITEMS
# ====================================================================

def sprite_genesis_shard():
    """Fragmento Génesis — mote de luz brillante (16x16)."""
    img = new_canvas(16, 16)
    d = ImageDraw.Draw(img)
    # Núcleo brillante
    d.rectangle([6,6,9,9], fill=WHITE)
    d.rectangle([7,5,8,10], fill=GOLD_LIGHT)
    d.rectangle([5,7,10,8], fill=GOLD_LIGHT)
    # Halo
    d.point([7,4], fill=GOLD)
    d.point([8,4], fill=GOLD)
    d.point([7,11], fill=GOLD)
    d.point([8,11], fill=GOLD)
    d.point([4,7], fill=GOLD)
    d.point([4,8], fill=GOLD)
    d.point([11,7], fill=GOLD)
    d.point([11,8], fill=GOLD)
    # Esquinas
    d.point([6,6], fill=GOLD)
    d.point([9,6], fill=GOLD)
    d.point([6,9], fill=GOLD)
    d.point([9,9], fill=GOLD)
    img = outline_sprite(img)
    save_sprite(img, f"{BASE}/Items/GenesisShard.png", 2)

def sprite_resonance_shard():
    """Fragmento de Resonancia — cristal dorado facetado (14x14)."""
    img = new_canvas(14, 14)
    d = ImageDraw.Draw(img)
    # Forma de diamante
    d.polygon([(7,2),(12,7),(7,12),(2,7)], fill=GOLD)
    d.polygon([(7,2),(12,7),(7,7)], fill=GOLD_LIGHT)
    d.polygon([(7,2),(2,7),(7,7)], fill=GOLD_DARK)
    d.point([7,4], fill=WHITE)
    d.point([5,6], fill=GOLD_LIGHT)
    img = outline_sprite(img)
    save_sprite(img, f"{BASE}/Items/ResonanceShard.png", 2)

def sprite_memory_rune():
    """Runa de Memoria — piedra rúnica violeta (16x16)."""
    img = new_canvas(16, 16)
    d = ImageDraw.Draw(img)
    # Fondo de piedra
    d.rectangle([3,3,12,12], fill=VIOLET_DARK)
    d.rectangle([4,4,11,11], fill=VIOLET)
    # Símbolo rúnico (estrella)
    d.point([7,5], fill=VIOLET_LIGHT)
    d.point([8,5], fill=VIOLET_LIGHT)
    d.rectangle([7,6,8,7], fill=WHITE)
    d.point([6,7], fill=VIOLET_LIGHT)
    d.point([9,7], fill=VIOLET_LIGHT)
    d.point([7,8], fill=VIOLET_LIGHT)
    d.point([8,8], fill=VIOLET_LIGHT)
    # Brillo
    d.point([5,5], fill=VIOLET_LIGHT)
    img = outline_sprite(img)
    save_sprite(img, f"{BASE}/Items/MemoryRune.png", 2)

def sprite_ancient_altar_item():
    """Item del altar (para inventario, 16x16)."""
    os.makedirs(f"{BASE}/Items/Placeables", exist_ok=True)
    img = new_canvas(16, 16)
    d = ImageDraw.Draw(img)
    # Base de piedra
    d.rectangle([3,8,12,13], fill=(80,60,120))
    d.rectangle([3,8,12,8], fill=(100,80,150))
    d.rectangle([4,9,11,12], fill=(60,40,90))
    # Cristal brillante arriba
    d.polygon([(7,2),(10,5),(7,8),(4,5)], fill=VIOLET)
    d.polygon([(7,2),(10,5),(7,5)], fill=VIOLET_LIGHT)
    d.point([7,3], fill=WHITE)
    img = outline_sprite(img)
    save_sprite(img, f"{BASE}/Items/Placeables/AncientAltarItem.png", 2)

# ====================================================================
# ARMAS (más grandes, 20x20)
# ====================================================================

def sprite_lumina_starbow():
    """Lumina, la Arcoestelar — arco dorado (22x28)."""
    img = new_canvas(22, 28)
    d = ImageDraw.Draw(img)
    # Cuerpo del arco (curva)
    for i, y in enumerate(range(4, 24)):
        offset = int(4 * (1 - abs((i-10)/10.0)))
        d.point([10-offset, y], fill=GOLD)
        d.point([10+offset, y], fill=GOLD)
    # Cuerda
    d.line([10, 4, 10, 23], fill=WHITE)
    # Agarre central
    d.rectangle([9,13,11,15], fill=GOLD_DARK)
    # Brillos
    d.point([6,8], fill=GOLD_LIGHT)
    d.point([14,8], fill=GOLD_LIGHT)
    d.point([6,18], fill=GOLD_LIGHT)
    d.point([14,18], fill=GOLD_LIGHT)
    img = outline_sprite(img)
    save_sprite(img, f"{BASE}/Weapons/LuminaStarbow.png", 2)

def sprite_solbrand_edge():
    """Solbrand, Filo del Alba — espada naranja solar (22x28)."""
    img = new_canvas(22, 28)
    d = ImageDraw.Draw(img)
    # Hoja
    d.polygon([(10,2),(13,4),(13,16),(10,18),(7,16),(7,4)], fill=ORANGE_LIGHT)
    d.polygon([(10,2),(13,4),(13,16),(10,16)], fill=ORANGE)
    d.line([10,2,10,16], fill=WHITE)
    # Guarda
    d.rectangle([5,17,15,19], fill=ORANGE_DARK)
    d.rectangle([6,17,14,18], fill=ORANGE)
    # Mango
    d.rectangle([9,20,11,24], fill=(100,60,20))
    # Pomo
    d.rectangle([8,25,12,27], fill=GOLD)
    img = outline_sprite(img)
    save_sprite(img, f"{BASE}/Weapons/SolbrandEdge.png", 2)

def sprite_grimoire_eternal():
    """Grimorio del Eterno — libro mágico violeta (24x22)."""
    img = new_canvas(24, 22)
    d = ImageDraw.Draw(img)
    # Cuerpo del libro
    d.rectangle([4,4,20,18], fill=VIOLET_DARK)
    d.rectangle([5,5,19,17], fill=VIOLET)
    # Lomo
    d.rectangle([4,4,6,18], fill=VIOLET_DARK)
    # Símbolo cósmico en la portada
    d.point([12,8], fill=WHITE)
    d.point([11,9], fill=VIOLET_LIGHT)
    d.point([13,9], fill=VIOLET_LIGHT)
    d.rectangle([11,10,13,11], fill=VIOLET_LIGHT)
    d.point([12,12], fill=VIOLET_LIGHT)
    # Borde dorado
    d.rectangle([5,5,19,5], fill=GOLD)
    d.rectangle([5,17,19,17], fill=GOLD)
    # Brillos
    d.point([8,7], fill=VIOLET_LIGHT)
    d.point([16,14], fill=VIOLET_LIGHT)
    img = outline_sprite(img)
    save_sprite(img, f"{BASE}/Weapons/GrimoireEternal.png", 2)

# ====================================================================
# PROYECTILES (10x10)
# ====================================================================

def sprite_starlight_arrow():
    img = new_canvas(10, 10)
    d = ImageDraw.Draw(img)
    # Flecha dorada
    d.polygon([(8,5),(5,3),(5,7)], fill=GOLD_LIGHT)
    d.rectangle([3,4,6,6], fill=GOLD)
    d.point([3,5], fill=WHITE)
    img = outline_sprite(img)
    save_sprite(img, f"{BASE}/Projectiles/StarlightArrow.png", 2)

def sprite_dawn_slash():
    img = new_canvas(16, 16)
    d = ImageDraw.Draw(img)
    # Onda de corte solar
    d.arc([2,2,14,14], 200, 340, fill=ORANGE, width=2)
    d.arc([3,3,13,13], 200, 340, fill=ORANGE_LIGHT, width=1)
    d.point([12,5], fill=WHITE)
    img = outline_sprite(img)
    save_sprite(img, f"{BASE}/Projectiles/DawnSlash.png", 2)

def sprite_arcane_bolt():
    img = new_canvas(10, 10)
    d = ImageDraw.Draw(img)
    # Bolt violeta
    d.ellipse([2,2,8,8], fill=VIOLET)
    d.ellipse([3,3,7,7], fill=VIOLET_LIGHT)
    d.point([4,4], fill=WHITE)
    img = outline_sprite(img)
    save_sprite(img, f"{BASE}/Projectiles/ArcaneBolt.png", 2)

# ====================================================================
# NPCs (más grandes, 40-80px)
# ====================================================================

def sprite_aethon_boss():
    """Aethon — entidad cósmica colosal (60x60)."""
    img = new_canvas(60, 60)
    d = ImageDraw.Draw(img)
    # Aura de galaxia
    d.ellipse([5,5,55,55], fill=(40,25,70))
    d.ellipse([10,10,50,50], fill=(60,40,100))
    d.ellipse([15,15,45,45], fill=VIOLET_DARK)
    # Núcleo central
    d.ellipse([22,22,38,38], fill=GOLD)
    d.ellipse([25,25,35,35], fill=GOLD_LIGHT)
    d.ellipse([27,27,33,33], fill=WHITE)
    # Estrellas orbitando
    for x, y in [(12,20),(48,20),(12,40),(48,40),(20,12),(40,12),(20,48),(40,48)]:
        d.point([x,y], fill=WHITE)
        d.point([x-1,y], fill=GOLD_LIGHT)
    # Brazos de galaxia (espiral simplificada)
    d.arc([8,8,52,52], 0, 90, fill=VIOLET_LIGHT, width=2)
    d.arc([12,12,48,48], 180, 270, fill=VIOLET_LIGHT, width=2)
    img = outline_sprite(img)
    save_sprite(img, f"{BASE}/NPCs/AethonBoss.png", 1)

def sprite_hollow_titan():
    """Titán Hueco — coloso de cristal (40x56)."""
    img = new_canvas(40, 56)
    d = ImageDraw.Draw(img)
    # Cuerpo cristalino
    d.polygon([(20,4),(32,16),(32,40),(28,52),(12,52),(8,40),(8,16)], fill=TEAL_DARK)
    d.polygon([(20,4),(32,16),(20,28),(8,16)], fill=TEAL)
    d.polygon([(20,4),(8,16),(20,28)], fill=(120,240,220))
    # Ojos brillantes
    d.rectangle([14,18,16,20], fill=WHITE)
    d.rectangle([24,18,26,20], fill=WHITE)
    d.point([15,19], fill=TEAL)
    d.point([25,19], fill=TEAL)
    # Grietas de cristal
    TEAL_LIGHT = (150, 255, 240)
    d.line([20,28,18,36], fill=TEAL_LIGHT)
    d.line([20,28,24,38], fill=TEAL_LIGHT)
    img = outline_sprite(img)
    save_sprite(img, f"{BASE}/NPCs/HollowTitan.png", 1)

def sprite_rift_keeper():
    """Guardián del Rift — figura teal con grietas (30x44)."""
    img = new_canvas(30, 44)
    d = ImageDraw.Draw(img)
    # Cuerpo
    d.rectangle([10,8,20,36], fill=TEAL_DARK)
    d.rectangle([11,9,19,35], fill=TEAL)
    # Cabeza
    d.rectangle([11,4,19,10], fill=TEAL_DARK)
    d.rectangle([12,5,18,9], fill=TEAL)
    # Ojos vacíos
    TEAL_LIGHT_NPC = (150, 255, 240)
    d.point([13,7], fill=TEAL_LIGHT_NPC)
    d.point([17,7], fill=TEAL_LIGHT_NPC)
    # Grietas de rift (violeta)
    d.line([15,12,13,20], fill=VIOLET_LIGHT)
    d.line([15,12,17,22], fill=VIOLET_LIGHT)
    d.line([15,24,13,30], fill=VIOLET_LIGHT)
    img = outline_sprite(img)
    save_sprite(img, f"{BASE}/NPCs/RiftKeeper.png", 1)

def sprite_echo_blade():
    """Eco del Primer Portador — sombra espectral (26x40)."""
    img = new_canvas(26, 40)
    d = ImageDraw.Draw(img)
    # Silueta oscura
    d.rectangle([9,6,17,34], fill=(40,30,50))
    d.rectangle([10,7,16,33], fill=(60,40,80))
    # Cabeza
    d.ellipse([9,2,17,10], fill=(40,30,50))
    # Espada fantasma
    d.rectangle([20,12,22,28], fill=VIOLET_LIGHT)
    d.point([21,11], fill=WHITE)
    # Ojos brillantes
    d.point([12,6], fill=ORANGE)
    d.point([14,6], fill=ORANGE)
    img = outline_sprite(img)
    save_sprite(img, f"{BASE}/NPCs/EchoBlade.png", 1)

def sprite_echo_archer():
    """Eco de la Arquera Estelar — fantasma con arco (26x40)."""
    img = new_canvas(26, 40)
    d = ImageDraw.Draw(img)
    # Silueta
    d.rectangle([9,6,15,34], fill=(40,35,50))
    d.rectangle([10,7,14,33], fill=(70,60,90))
    d.ellipse([9,2,15,10], fill=(40,35,50))
    # Arco fantasma
    d.arc([16,8,22,32], 270, 90, fill=GOLD, width=2)
    # Ojos
    d.point([11,6], fill=GOLD)
    d.point([13,6], fill=GOLD)
    img = outline_sprite(img)
    save_sprite(img, f"{BASE}/NPCs/EchoArcher.png", 1)

def sprite_witness():
    """El Testigo — entidad flotante con ojo (24x36)."""
    img = new_canvas(24, 36)
    d = ImageDraw.Draw(img)
    # Manto
    d.polygon([(12,2),(20,12),(20,30),(12,34),(4,30),(4,12)], fill=VIOLET_DARK)
    d.polygon([(12,2),(20,12),(12,18),(4,12)], fill=VIOLET)
    # Ojo central
    d.ellipse([8,10,16,18], fill=WHITE)
    d.ellipse([10,12,14,16], fill=GOLD)
    d.point([12,14], fill=BLACK)
    # Brillo
    d.point([12,8], fill=GOLD_LIGHT)
    img = outline_sprite(img)
    save_sprite(img, f"{BASE}/NPCs/TheWitness.png", 1)

# ====================================================================
# TILES
# ====================================================================

def sprite_ancient_altar_tile():
    """Altar Antiguo — tile 3x2 (48x32)."""
    img = new_canvas(48, 32)
    d = ImageDraw.Draw(img)
    # Base de piedra
    d.rectangle([4,12,44,28], fill=(60,45,90))
    d.rectangle([6,14,42,26], fill=(80,60,120))
    d.rectangle([4,12,44,14], fill=(100,75,150))
    # Runas grabadas
    for x in [10,20,30]:
        d.point([x,20], fill=GOLD)
        d.point([x+1,20], fill=GOLD_LIGHT)
    # Cristal central flotante
    d.polygon([(24,2),(30,8),(24,14),(18,8)], fill=VIOLET)
    d.polygon([(24,2),(30,8),(24,8)], fill=VIOLET_LIGHT)
    d.point([24,5], fill=WHITE)
    img = outline_sprite(img)
    save_sprite(img, f"{BASE}/Tiles/AncientAltar.png", 1)

# ====================================================================
# UI
# ====================================================================

def sprite_ui_node_common():
    """Nodo común del árbol (16x16)."""
    img = new_canvas(16, 16)
    d = ImageDraw.Draw(img)
    d.ellipse([4,4,12,12], fill=(180,180,210))
    d.ellipse([5,5,11,11], fill=(210,210,230))
    d.point([7,7], fill=WHITE)
    img.save(f"{BASE}/UI/NodeCommon.png")
    print(f"  ✓ {BASE}/UI/NodeCommon.png")

def sprite_ui_node_rare():
    img = new_canvas(16, 16)
    d = ImageDraw.Draw(img)
    d.ellipse([4,4,12,12], fill=(120,170,255))
    d.ellipse([5,5,11,11], fill=(150,190,255))
    d.point([7,7], fill=WHITE)
    img.save(f"{BASE}/UI/NodeRare.png")
    print(f"  ✓ {BASE}/UI/NodeRare.png")

def sprite_ui_node_legendary():
    img = new_canvas(16, 16)
    d = ImageDraw.Draw(img)
    d.ellipse([3,3,13,13], fill=GOLD)
    d.ellipse([4,4,12,12], fill=GOLD_LIGHT)
    d.ellipse([6,6,10,10], fill=WHITE)
    img.save(f"{BASE}/UI/NodeLegendary.png")
    print(f"  ✓ {BASE}/UI/NodeLegendary.png")

def sprite_icon_mod():
    """Icono del mod 80x80."""
    img = new_canvas(80, 80, DARK_BG + (255,))
    d = ImageDraw.Draw(img)
    # Aura
    for r in range(35, 0, -2):
        alpha = int(30 * (1 - r/35))
        d.ellipse([40-r,40-r,40+r,40+r], outline=(60+r,40+r,100,alpha))
    # Estrella central ✦
    d.text((32,24), "✦", fill=GOLD)
    img.save("/home/z/my-project/AethonMod/icon.png")
    print("  ✓ icon.png")

# ====================================================================
# GENERAR TODO
# ====================================================================

print("Generando sprites pixel-art del mod Aethon...\n")

print("Items:")
sprite_genesis_shard()
sprite_resonance_shard()
sprite_memory_rune()
sprite_ancient_altar_item()

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

print("\nUI:")
sprite_ui_node_common()
sprite_ui_node_rare()
sprite_ui_node_legendary()

print("\nIcono:")
sprite_icon_mod()

print(f"\n✅ Todos los sprites generados en {BASE}/")
