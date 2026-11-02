#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.88 — CorruptoGlobalItem: la etiqueta (Corrupto) VA EN EL NOMBRE y el
nombre se vuelve NEGRO; el arma se cubre de un brillo morado-negro EN EL
INVENTARIO (el velo tipo shader — PostDrawInInventory)."""
import io

R = "AethonMod/Content/Globals/CorruptoGlobalItem.cs"
t = io.open(R, encoding="utf-8").read()

# ---------- (1) la etiqueta al nombre + nombre negro ----------
viejo = """                // LA ETIQUETA: pegada al nombre (el puesto del «Superior»
                // que ya jamás volverá a ocupar).
                int idxNombre = -1;
                for (int i = 0; i < tooltips.Count; i++)
                {
                    if (tooltips[i].Name == "ItemName") { idxNombre = i; break; }
                }
                var violeta = new Color(186, 96, 236);
                var tenue = new Color(158, 108, 200);
                var bruma = new Color(122, 216, 182);
                var etiqueta = new TooltipLine(Mod, "CorruptoEtiqueta",
                    Language.GetTextValue("Mods.AethonMod.Items.ArmaCorruptaTag"))
                { OverrideColor = violeta };
                if (idxNombre >= 0) tooltips.Insert(idxNombre + 1, etiqueta);
                else tooltips.Add(etiqueta);
"""
nuevo = """                // v6.88 — LA ETIQUETA EN EL NOMBRE (la letra: «las armas
                // corruptas tendran la etiqueta de (Corrupto) en su
                // nombre, y su nombre se vuelve negro»): la v6.86 ponía la
                // etiqueta en una LÍNEA separada debajo del nombre — ahora
                // el nombre MISMO la lleva: «Espada corta de cobre
                // (Corrupto)» en NEGRO (el negro de la tinta de la casa:
                // legible sobre el tooltip, imposible de confundir con el
                // violeta de las líneas de lore).
                int idxNombre = -1;
                for (int i = 0; i < tooltips.Count; i++)
                {
                    if (tooltips[i].Name == "ItemName") { idxNombre = i; break; }
                }
                var tenue = new Color(158, 108, 200);
                var bruma = new Color(122, 216, 182);
                if (idxNombre >= 0)
                {
                    tooltips[idxNombre].Text =
                        tooltips[idxNombre].Text + " " +
                        Language.GetTextValue("Mods.AethonMod.Items.ArmaCorruptaTag");
                    tooltips[idxNombre].OverrideColor = new Color(30, 30, 40);
                }
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

# ---------- (2) el velo morado-negro en el inventario ----------
viejo = """        // === LA AURA DEL ARMA CAÍDA (se VE maldita en el suelo) ===
"""
nuevo = """        // === v6.88 — EL VELO DEL INVENTARIO (la letra: «se cubren con
        //     un brillo morado negro, este brillo podria ser un shader
        //     que cubra el item»): el \"shader casero\" — el sprite del
        //     item se vuelve a dibujar ENCIMA de sí mismo con el velo
        //     morado-negro pulsante (idéntico frame/origen/escala: CUBRE
        //     el item como una capa de tinta) + el glow que respira por
        //     detrás. En el suelo ya vivía su aura (abajo); el inventario
        //     la pedía a gritos. ===
        public override void PostDrawInInventory(Item item, SpriteBatch spriteBatch,
            Vector2 position, Rectangle frame, Color drawColor, Color itemColor,
            Vector2 origin, float scale)
        {
            if (!Corrompido || Main.gameMenu) return;
            try
            {
                Texture2D tex = Terraria.GameContent.TextureAssets.Item[item.type].Value;
                if (tex == null) return;
                // EL VELO: el sprite encima de sí mismo, teñido morado-negro
                // (pulso 2,4 Hz — la misma respiración del aura del suelo).
                float pulso = 0.42f + 0.16f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.4f);
                spriteBatch.Draw(tex, position, frame,
                    new Color(70, 24, 112, (int)(255f * pulso)) * (pulso * 0.55f),
                    0f, origin, scale, SpriteEffects.None, 0f);
                // EL GLOW: la bruma que respira por detrás del slot.
                Texture2D glow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                if (glow != null)
                    spriteBatch.Draw(glow, position, null,
                        new Color(96, 40, 150) * (0.16f + 0.07f * pulso), 0f,
                        glow.Size() * 0.5f, new Vector2(34f, 34f) / glow.Size(),
                        SpriteEffects.None, 0f);
            }
            catch { }
        }

        // === LA AURA DEL ARMA CAÍDA (se VE maldita en el suelo) ===
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)

io.open(R, "w", encoding="utf-8").write(t)
print("OK — CorruptoGlobalItem: nombre negro (Corrupto) + velo de inventario")
