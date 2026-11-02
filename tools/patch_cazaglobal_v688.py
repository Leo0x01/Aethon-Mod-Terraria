#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.88 — CazaJefesGlobalNPC: paga VecesEmperatriz (HallowBoss),
   Aethon paga la 100 (no la 59), Fishron entra en PuertaDe (37)."""
import io

RUTA = "AethonMod/Content/Globals/CazaJefesGlobalNPC.cs"
with io.open(RUTA, "r", encoding="utf-8") as f:
    t = f.read()

# --- 1) el contador de la Emperatriz junto al de Moon Lord ---
viejo = """                if (npc.type == ModContent.NPCType<NPCs.AethonBoss>() ||"""
nuevo = """                // v6.88 — EL CONTADOR DE LA EMPERATRIZ (la letra de la
                //     cima: «5 emperatris de la luz»): lo pagan las
                //     pantallas vivas, como el de Moon Lord.
                if (npc.type == NPCID.HallowBoss)
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player p = Main.player[i];
                        if (p == null || !p.active || p.dead) continue;
                        var cazador = p.GetModPlayer<Players.CazaJefesPlayer>();
                        if (cazador != null) cazador.VecesEmperatriz++;
                    }
                }
                if (npc.type == ModContent.NPCType<NPCs.AethonBoss>() ||"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

# --- 2) Aethon paga la 100 (la cima compuesta) ---
viejo = "                        cazador.Pagar(59);   // LA PUERTA DEL RECUERDO LEJANO"
nuevo = "                        cazador.Pagar(100);  // v6.88 — LA CIMA COMPUESTA (era la 59: Aethon sube a la CIEN)"
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

# --- 3) Fishron gana su puerta en PuertaDe ---
viejo = """                // v6.86 — Deerclops y el Duque Fishron MURIERON como puerta
                // (la letra: «el jefe Deerclop no lo incluyas en la
                // progresion» + «el jefe duke fishron no necesitas
                // incluirlo») — sus líneas ya no existen.
"""
nuevo = """                // v6.88 — EL DUQUE FISHRON VUELVE COMO PUERTA (la letra
                // nueva: «ya que como requisitos pides items del Duque
                // Fishron entonces incluye a ese jefe entre los
                // requisitos») — la 37, con sus armas en el MISMO
                // peldaño (puerta Y botín). Deerclops sigue FUERA (la
                // letra v6.86 lo excluyó y nadie lo reclamó).
                NPCID.DukeFishron          => 37,
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

with io.open(RUTA, "w", encoding="utf-8") as f:
    f.write(t)
print("OK — CazaJefesGlobalNPC: EoL++ / Aethon→100 / Fishron→37")
