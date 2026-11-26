#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
cirugia_v6176_entrega.py — v6.176 — EL ARMA DEL CÍCLOPE, ENTREGADA (la
letra: «si creaste algo de esto Cíclope Proyectado, debes darselo al
jugador directamente, ademas lo que se tenia que crear era un arma»).

CUATRO piezas:
  1. LA LOCALIZACIÓN: el ítem (DisplayName+Tooltip) y el aviso de
     entrega (Ira.CiclopeEntregado) en es-ES (la fuente única) y su
     espejo en en-US (la paridad R7 — las claves nacen en ambos).
  2. LA ENTREGA DIRECTA: TestingPlayer.OnEnterWorld — el mismo patrón
     del Faro (una por jugador, siempre que no la lleve).
  3. LA BOLSA (R8): el arma vive en la Bolsa de las Investigaciones.
  4. EL SYNC: es-MX se regenera byte a byte (sync_es_mx_v6564.py).
"""
import pathlib
import subprocess
import sys

RAIZ = pathlib.Path(__file__).resolve().parent.parent
F_ES = RAIZ / "AethonMod" / "Localization" / "es-ES_Mods.AethonMod.hjson"
F_EN = RAIZ / "AethonMod" / "Localization" / "en-US_Mods.AethonMod.hjson"
F_TEST = RAIZ / "AethonMod" / "Content" / "Players" / "TestingPlayer.cs"
F_BOLSAS = RAIZ / "AethonMod" / "Content" / "Items" / "Bolsas" / "BolsasCategorias.cs"

# ---------- (1a) es-ES: el ítem ----------
V1 = """Items: {
	FaroDelEclipse: {
		DisplayName: Faro del Eclipse
		Tooltip: Mantén apretado el botón de uso — medio segundo de carga y el conducto sigue al cursor hasta 900 px
	}
"""

N1 = """Items: {
	FaroDelEclipse: {
		DisplayName: Faro del Eclipse
		Tooltip: Mantén apretado el botón de uso — medio segundo de carga y el conducto sigue al cursor hasta 900 px
	}

	// v6.176 — EL ARMA DEL CÍCLOPE PROYECTADO (la letra: «lo que se
	// tenia que crear era un arma» — la proyección del ojo del vídeo).
	ProyeccionDelCiclope: {
		DisplayName: Proyección del Cíclope
		Tooltip: El ojo del Cíclope se proyecta sobre ti: doce latidos de grito y el rayo magenta sale — penetración infinita
	}
"""

# ---------- (1b) es-ES: el aviso de entrega ----------
V2 = """	FaroEntregado: El Faro del Eclipse brilla en tu inventario — el Eterno lo entrega para que lo pruebes.
}"""

N2 = """	FaroEntregado: El Faro del Eclipse brilla en tu inventario — el Eterno lo entrega para que lo pruebes.
	CiclopeEntregado: La Proyección del Cíclope abre su ojo en tu inventario — el arma del vídeo, entregada para que la pruebes.
}"""

# ---------- (1c) en-US: el ítem (paridad R7) ----------
V3 = """	FaroDelEclipse: {
		DisplayName: Eclipse Lighthouse
		Tooltip: Hold the use button — half a second of charge, then the beam follows the cursor up to 900 px
	}
"""

N3 = """	FaroDelEclipse: {
		DisplayName: Eclipse Lighthouse
		Tooltip: Hold the use button — half a second of charge, then the beam follows the cursor up to 900 px
	}

	// v6.176 — THE CYCLOPS' WEAPON (the letter: what had to be built
	// was a weapon — the eye projection of the video).
	ProyeccionDelCiclope: {
		DisplayName: Cyclops Projection
		Tooltip: The Cyclops' eye projects over you: twelve beats of screaming, then the magenta ray fires — infinite pierce
	}
"""

# ---------- (1d) en-US: el aviso ----------
V4 = """	FaroEntregado: The Lighthouse of the Eclipse shines in your inventory — the Eternal hands it over for you to test it."""

N4 = """	FaroEntregado: The Lighthouse of the Eclipse shines in your inventory — the Eternal hands it over for you to test it.
	CiclopeEntregado: The Cyclops Projection opens its eye in your inventory — the video's weapon, delivered for you to test."""

# ---------- (2) TestingPlayer: la entrega directa ----------
V5 = """            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("TestingPlayer", exRegistro); }

            // v6.50.10 — ARMADURA: PlayerLoader.OnEnterWorld NO envuelve el"""

N5 = """            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("TestingPlayer", exRegistro); }

            // ==============================================================
            //  v6.176 — EL ARMA DEL CÍCLOPE PROYECTADO, DIRECTO A LA MANO
            //  (la letra: «si creaste algo de esto Cíclope Proyectado,
            //  debes darselo al jugador directamente, ademas lo que se
            //  tenia que crear era un arma»): la Proyección del Cíclope
            //  nace en el inventario del portador al entrar al mundo —
            //  SIEMPRE que no la lleve (una por jugador).
            // ==============================================================
            try
            {
                int tipoCiclope = ModContent.ItemType
                    <global::AethonMod.Content.Weapons.Investigaciones.ProyeccionDelCiclope>();
                bool llevaCiclope = false;
                for (int k = 0; k < 58; k++)
                {
                    Item it = Player.inventory[k];
                    if (it != null && !it.IsAir && it.type == tipoCiclope) { llevaCiclope = true; break; }
                }
                if (!llevaCiclope)
                {
                    Player.QuickSpawnItem(Player.GetSource_FromThis(), tipoCiclope, 1);
                    if (Player.whoAmI == Main.myPlayer && !Main.dedServ)
                        Main.NewText(Terraria.Localization.Language.GetTextValue(
                                "Mods.AethonMod.Ira.CiclopeEntregado"),
                            new Microsoft.Xna.Framework.Color(255, 140, 220));
                }
            }
            catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("TestingPlayer", exRegistro); }

            // v6.50.10 — ARMADURA: PlayerLoader.OnEnterWorld NO envuelve el"""

# ---------- (3) la bolsa (R8) ----------
V6 = """            // v6.175 — EL ECO PROYECTADO (la llamada del Cíclope Proyectado
            // — la recreación del vídeo «Me Proyecte» de Chopilas).
            l.Add((ModContent.ItemType<Items.NPCs.EcoProyectado>(), 3));"""

N6 = """            // v6.175 — EL ECO PROYECTADO (la llamada del Cíclope Proyectado
            // — la recreación del vídeo «Me Proyecte» de Chopilas).
            l.Add((ModContent.ItemType<Items.NPCs.EcoProyectado>(), 3));
            // v6.176 — EL ARMA DEL CÍCLOPE (la letra: «lo que se tenia
            // que crear era un arma» — la proyección del ojo).
            l.Add((ModContent.ItemType<Weapons.Investigaciones.ProyeccionDelCiclope>(), 1));"""


def main() -> int:
    operaciones = [
        ("es-ES: el ítem", F_ES, V1, N1),
        ("es-ES: el aviso", F_ES, V2, N2),
        ("en-US: el ítem", F_EN, V3, N3),
        ("en-US: el aviso", F_EN, V4, N4),
        ("TestingPlayer: la entrega", F_TEST, V5, N5),
        ("la bolsa de investigaciones", F_BOLSAS, V6, N6),
    ]
    for etiqueta, archivo, viejo, nuevo in operaciones:
        t = archivo.read_text(encoding="utf-8")
        if nuevo in t:
            print(f"YA: {etiqueta}")
            continue
        assert viejo in t, f"PATRÓN NO ENCONTRADO: {etiqueta}"
        archivo.write_text(t.replace(viejo, nuevo, 1), encoding="utf-8")
        print(f"✔ {etiqueta}")

    # (4) EL SYNC: es-MX espejo byte a byte
    r = subprocess.run([sys.executable, str(RAIZ / "tools" / "sync_es_mx_v6564.py")],
                       capture_output=True, text=True, cwd=str(RAIZ))
    print(r.stdout.strip()[-400:] if r.stdout else r.stderr.strip()[-400:])
    return 0


if __name__ == "__main__":
    sys.exit(main())
