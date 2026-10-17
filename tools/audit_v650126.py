#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
audit_v650126.py — LA AUDITORÍA DE LA v6.50.126 (todo debe dar ✓).

Verifica (marcadores de código EXACTOS, estilo de la casa):
  1. build.txt = 6.50.126
  2. LA CAÍDA: failsafe del primer vecino (HayVecinoQueNoEsElGuia +
     llegoPrimerNpc persistido + CometaEnVuelo en los TRES caminos +
     ForzarCaida)
  3. LA TINTA DEL COMETA: ítem + icono + bolsa + EcoRed Msg 15 +
     localización ×3
  4. EL GRIMORIO DORMIDO: intimidad MP (CanChat + PreDraw filtrado)
  5. LOS MINIMINIONS MÁS JUNTOS: órbita 44 → 27
  6. LA PLAGA DEL CONEJO (Re:Zero): MODO_PLAGA + horda + división +
     ojos rojos + determinismo (Hash01, sin Main.rand en el cerebro)
  7. EL SELECCIONADOR: vista previa isAPreviewDummy + el vagabundo
     (On_Main.PostDrawMenu + sol/luna/logo + manchas)
"""
import io
import os
import sys

BASE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
MOD = os.path.normpath(os.path.join(BASE, "AethonMod"))
CONT = os.path.join(MOD, "Content")

ok = 0
fallos = []


def check(nombre, cond):
    global ok
    if cond:
        ok += 1
        print(f"  ✓ {nombre}")
    else:
        fallos.append(nombre)
        print(f"  ✗ {nombre}")


def leer(ruta):
    with io.open(ruta, encoding="utf-8") as f:
        return f.read()


print("AUDIT v6.50.126:")

# ===== 1. LA VERSIÓN =====
build = leer(os.path.join(MOD, "build.txt"))
check("build.txt = 6.50.126", "version = 6.50.126" in build)

# ===== 2. LA CAÍDA — EL FAILSAFE + LA TINTA =====
caida = leer(os.path.join(CONT, "Systems", "LaCaidaSistema.cs"))
check("failsafe: HayVecinoQueNoEsElGuia (detector)", "HayVecinoQueNoEsElGuia()" in caida)
check("failsafe: NPCID.Guide excluido", "NPCID.Guide" in caida)
check("failsafe: _llegoPrimerNpc persistido (llegoPrimerNpc)", 'tag["llegoPrimerNpc"]' in caida
      and 'tag.GetBool("llegoPrimerNpc")' in caida)
check("failsafe: el bloque dispara cometas", "if (!_llegoPrimerNpc && HayVecinoQueNoEsElGuia())" in caida)
check("guarda-choques: CometaEnVuelo (def + 3 usos)",
      caida.count("CometaEnVuelo(") >= 4)
check("la tinta: ForzarCaida (SP/host + MP)", "public static int ForzarCaida(Player p)" in caida)
check("la caída por jugador/mundo intacta (.125)",
      "_cayoEnEsteMundo" in caida and "cayosDelMundo" in caida)

# ===== 3. LA TINTA DEL COMETA =====
tinta = leer(os.path.join(CONT, "Items", "Llamados", "TintaDelCometa.cs"))
check("ítem: TintaDelCometa existe", "public class TintaDelCometa : ModItem" in tinta)
check("ítem: usa ForzarCaida en SP/host", "LaCaidaSistema.ForzarCaida(player)" in tinta)
check("ítem: pide el cielo por cable en MP", "EcoRed.PedirForzarCaida()" in tinta)
check("ítem: los avisos de la tinta", "Caida.TintaYaEspera" in tinta and "Caida.TintaEnVuelo" in tinta)
check("ítem: SIN receta (es de pruebas)", "SIN RECETA" in tinta)
check("icono: TintaDelCometa.png existe",
      os.path.isfile(os.path.join(CONT, "Items", "Llamados", "TintaDelCometa.png")))

bolsa = leer(os.path.join(CONT, "Items", "Llamados", "BolsaDeLaCria.cs"))
check("bolsa: entrega la tinta", "ItemType<TintaDelCometa>()" in bolsa)

eco = leer(os.path.join(CONT, "Systems", "EcoRed.cs"))
check("EcoRed: MsgForzarCaida = 15", "public const byte MsgForzarCaida = 15;" in eco)
check("EcoRed: el server atiende el pedido", "TintaDelCometa.AtenderPedido(solicitante)" in eco)
check("EcoRed: el cliente lo pide", "public static void PedirForzarCaida()" in eco)

for idioma, nombre_item in [("es-MX", "Tinta del Cometa"), ("es-ES", "Tinta del Cometa"),
                            ("en-US", "Comet Ink")]:
    hjson = leer(os.path.join(MOD, "Localization",
                              f"{idioma}_Mods.AethonMod.hjson"))
    check(f"hjson {idioma}: ítem + 2 avisos",
          "TintaDelCometa: {" in hjson and f"DisplayName: {nombre_item}" in hjson
          and "TintaYaEspera:" in hjson and "TintaEnVuelo:" in hjson)

# ===== 4. EL GRIMORIO DORMIDO — LA INTIMIDAD MP =====
dormido = leer(os.path.join(CONT, "NPCs", "GrimorioDormido.cs"))
check("dormido: CanChat filtrado en MP",
      "public override bool CanChat()" in dormido and "EsDestinatario(yo)" in dormido)
check("dormido: PreDraw invisible para otros en MP",
      "MultiplayerClient && !EsDestinatario(Main.LocalPlayer)" in dormido)

# ===== 5. LOS MINIMINIONS MÁS JUNTOS =====
pareja = leer(os.path.join(CONT, "Projectiles", "Cosmetic", "AethonMenorPareja.cs"))
check("órbita: 44 → 27 (más juntos, la letra)", "float radio = (27f + MathF.Sin(t * 0.9f) * 2f) * brotar;" in pareja)
check("órbita: la 44 vieja MUERTA", "44f + MathF.Sin" not in pareja)

# ===== 6. LA PLAGA DEL CONEJO (Re:Zero) =====
check("plaga: MODO_PLAGA", "public const float MODO_PLAGA = 4f;" in pareja)
check("plaga: la horda (12 cuerpos)", "public ConejoPlaga[] Conejos" in pareja)
check("plaga: la moneda del easteregg (30%)", "PLAGA_PROBABILIDAD = 0.30f" in pareja)
check("plaga: se DIVIDEN (el gimmick Re:Zero)", "seDivide" in pareja and "k.Gen + 1" in pareja)
check("plaga: embestida de ojos al final de la ronda",
      "e.PlagaRonda >= PLAGA_RONDA - 15" in pareja)
check("plaga: los OJOS ROJOS mirando al maná", "255, 46, 46" in pareja)
check("plaga: el motor es Hash01 (determinista)", "VFXCore.Hash01(e.PlagaSemilla," in pareja)
check("plaga: el aborto al moverse el dueño",
      "e.Modo == MODO_VIAJAR || e.Modo == MODO_MOLESTAR || e.Modo == MODO_PLAGA" in pareja)
check("plaga: el libro guarda la frontera (ElegirMeta)",
      "case MODO_PLAGA:" in pareja)

# ===== 7. EL SELECCIONADOR DE PERSONAJE =====
pet = leer(os.path.join(CONT, "Projectiles", "Cosmetic", "GrimorioMenorPet.cs"))
check("pet: la vista previa propia (isAPreviewDummy)",
      "Projectile.isAPreviewDummy" in pet and "PreDrawVistaPrevista()" in pet)
check("pet: los ojos de la vista previa en corona compacta",
      "const float escOjo = 0.22f;" in pet)

sel = leer(os.path.join(CONT, "Systems", "SeleccionGrimorioSistema.cs"))
check("menú: el hook On_Main.PostDrawMenu", "On_Main.PostDrawMenu +=" in sel and
      "On_Main.PostDrawMenu -=" in sel)
check("menú: el umbral de los ~5 s", "UMBRAL_ESCAPE = 300" in sel)
check("menú: ATACA EL TÍTULO (manchas de tinta)", "_manchas" in sel and "ACTO_LOGO" in sel)
check("menú: CAMBIA EL SOL (el ojo eterno + el empujón)",
      "Main.sunModY = (short)" in sel and "DibujarOjoDelSol" in sel)
check("menú: CAMBIA LA LUNA (la fase gira)",
      "Main.moonPhase = (Main.moonPhase + 1) % 8;" in sel)
check("menú: las fórmulas de vanilla (sol/luna)",
      "PosicionDelSol()" in sel and "PosicionDeLaLuna()" in sel)
check("menú: el lote se restaura EXACTO",
      "SamplerState.LinearClamp" in sel and "Main.Rasterizer" in sel)

# ===== RESUMEN =====
print(f"\n{ok} ✓ · {len(fallos)} ✗")
if fallos:
    print("FALLOS:")
    for f in fallos:
        print("  -", f)
    sys.exit(1)
print("AUDIT v6.50.126 TODO OK")
