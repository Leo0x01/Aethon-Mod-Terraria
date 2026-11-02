#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.88 — (a) el Codice MUERE / el Libro Azul NACE en su sitio (bolsa);
   (b) las bajas del Libro Azul se pagan en OnKill;
   (c) el hjson completo de la v6.88."""
import io, os

# ---------- (a) la bolsa: LibroAzul en el sitio del Codice ----------
R = "AethonMod/Content/Items/Bolsas/BolsasCategorias.cs"
t = io.open(R, encoding="utf-8").read()
viejo = "l.Add((ModContent.ItemType<Items.Investigaciones.CodiceHabilidades>(), 1));"
nuevo = ("// v6.88 — EL CÓDICE MURIÓ (el árbol se muda de dueño): la llave\n"
         "            //     del árbol de habilidades es EL LIBRO AZUL (el arma de\n"
         "            //     Investigaciones que abre la ventana con clic derecho).\n"
         "            l.Add((ModContent.ItemType<Items.Investigaciones.LibroAzul>(), 1));")
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)
io.open(R, "w", encoding="utf-8").write(t)
print("OK — BolsaInvestigaciones: LibroAzul en el sitio del Codice")

# el archivo del Codice muere de raíz
os.remove("AethonMod/Content/Items/Investigaciones/CodiceHabilidades.cs")
png = "AethonMod/Content/Items/Investigaciones/CodiceHabilidades.png"
if os.path.exists(png): os.remove(png)
print("OK — CodiceHabilidades.cs/.png MUERTOS de raíz")

# ---------- (b) las bajas del Libro Azul (CazaJefesGlobalNPC) ----------
R = "AethonMod/Content/Globals/CazaJefesGlobalNPC.cs"
t = io.open(R, encoding="utf-8").read()
viejo = """                if (npc.type == NPCID.WallofFlesh && Main.rand.NextBool(2))"""
nuevo = """                // ============================================================
                //  v6.88 — LAS BAJAS DEL LIBRO AZUL (el arma del árbol de
                //  habilidades sube de nivel con las bajas de quien la
                //  lleva: 8 por nivel, tope 100 — los puntos del árbol
                //  nacen de SU nivel (SkillTreePlayer), no del despertar.
                // ============================================================
                try
                {
                    int k = npc.lastInteraction;
                    if (k >= 0 && k < Main.maxPlayers)
                    {
                        Player pj = Main.player[k];
                        if (pj != null && pj.active)
                        {
                            var st = pj.GetModPlayer<Players.SkillTreePlayer>();
                            if (st != null && st.LlevaLibroAzul())
                                st.RegistrarBajaLibroAzul();
                        }
                    }
                }
                catch { }

                if (npc.type == NPCID.WallofFlesh && Main.rand.NextBool(2))"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)
io.open(R, "w", encoding="utf-8").write(t)
print("OK — las bajas del Libro Azul pagadas en OnKill")
