#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.88 — (a) TruenoPerlin: DrawScreenCheckFluff (el rayo de los minions
   se ve contra objetivos lejanos); (b) AethonMenorItem y GrimorioMenorItem:
   fuera del QuickBuff de la tecla B (quitando Item.buffType — el buff lo
   aplica UseItem, como el Sol en Miniatura)."""
import io

# ---------- (a) TruenoPerlin ----------
R = "AethonMod/Content/Projectiles/Cosmic/TruenoPerlinProjectile.cs"
t = io.open(R, encoding="utf-8").read()
viejo = """        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }
"""
nuevo = """        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            // v6.88 — EL FLUFF DEL RAYO LEJANO (la letra: «siento que a
            // veces el rayo perlin de los minions no se ve cuando
            // atacan a objetivos un poco lejos»): el CENTRO de este
            // proyectil es su ANCLA (la PRESA — hasta 1.500 px del ojo);
            // el culling de vanilla solo dibuja el proyectil si su centro
            // está en pantalla — presa fuera de la vista = PreDraw JAMÁS
            // corría y el rayo NACÍA INVISIBLE (el daño golpeaba igual:
            // el síntoma perfecto). 2.600 px de fluff: cubre TODO el
            // rango de detección (500 + nivel/10·100, tope 1.500) con
            // margen — la casa ya usa el truco (RayosPrueba: 5000,
            // MiradaSolyn: 8000); este rayo era el único largo sin él.
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 2600;
        }
"""
assert t.count(viejo) == 1
t = t.replace(viejo, nuevo)
io.open(R, "w", encoding="utf-8").write(t)
print("OK — TruenoPerlin: DrawScreenCheckFluff 2600")

# ---------- (b) las dos mascotas fuera del QuickBuff ----------
for R, buff, pet in [
    ("AethonMod/Content/Items/Llamados/AethonMenorItem.cs",
     "global::AethonMod.Content.Buffs.AethonMenorBuff",
     "global::AethonMod.Content.Projectiles.Cosmetic.AethonMenorPet"),
    ("AethonMod/Content/Items/Llamados/GrimorioMenorItem.cs",
     "global::AethonMod.Content.Buffs.GrimorioMenorBuff",
     "global::AethonMod.Content.Projectiles.Cosmetic.GrimorioMenorPet"),
]:
    t = io.open(R, encoding="utf-8").read()

    # 1) quitar Item.buffType (lo que el QuickBuff de la tecla B lee)
    viejo = f"            Item.buffType = ModContent.BuffType<{buff}>();\n"
    nuevo = ("            // v6.88 — FUERA DEL QUICKBUFF (la letra: «la mascota\n"
             "            // de luz aethon menor se activa con la tecla B tengas\n"
             "            // o no tengas la mascota equipada»): Item.buffType era\n"
             "            // el Imán de la tecla B — vanilla la encontraba en el\n"
             "            // inventario (aunque NO estuviera equipada) y la USABA\n"
             "            // en cada pulsación. El buff lo aplica UseItem (como el\n"
             "            // Sol en Miniatura): SOLO con el CLIC del ítem.\n")
    assert t.count(viejo) == 1, f"buffType no hallado en {R}"
    t = t.replace(viejo, nuevo)

    # 2) UseItem aplica el buff explícito
    viejo = "                player.AddBuff(Item.buffType, 3600, true);"
    nuevo = (f"                player.AddBuff(ModContent.BuffType<{buff}>(), 3600, true);")
    assert t.count(viejo) == 1, f"AddBuff no hallado en {R}"
    t = t.replace(viejo, nuevo)

    io.open(R, "w", encoding="utf-8").write(t)
    print(f"OK — {R.split('/')[-1]}: sin buffType (QuickBuff limpio), AddBuff explícito")
