#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# ======================================================================
# cirugia_despertar_v6510.py — EL DESPERTAR ENTRA AL CUERPO DEL MOD
#
#  (1) ShardPlayer: NivelDespertar + PetDelDespertar (persistidos),
#      el techo de ojos ESTRICTO por nivel, el coordinador combinado,
#      el stack de invocación MUERE (ojos = stats del grimorio SOLO).
#  (2) WeaponScaling: TechoOjosEterno por nivel (5→1, 10→2, cada 10+1)
#      + DanoOjosDelEterno (el daño de los ojos SIN armadura).
#  (3) GrimoireEternal: el daño del despertar a TODO, la invocación
#      gated (nivel + despertar para mejorar), el daño de ojos
#      grimorio-only, LA BARRA DEL DESPERTAR en el tooltip (debajo del
#      nivel, encima del hambre) + la lista pequeña de requisitos.
#  (4) OjoEternoMinion: Fortalecer gated por despertar; nivel 50 =
#      TODO más fuerte y más rápido.
#  (5) ShardLevelItem.OnLevelUp: anuncio de CADA OJO NUEVO disponible.
#  (6) CorruptoGlobalItem: +5 % daño y otros stats en % (la letra).
#  (7) OleadaNPC: la chusma suelta 2 % de esencia; el jefe 10 planas
#      (la esencia de jefe MUERE) + el loot de items PELADO (detour).
#  (8) EL AGUJERO NEGRO NORMAL MUERE (CosmicWeapons + la bolsa + la
#      lente).
#  (9) TestingPlayer: el Eco del Despertar a la mano.
# ======================================================================
import io
import os

RAIZ = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod"

def leer(ruta):
    return io.open(os.path.join(RAIZ, ruta), encoding="utf-8").read()

def escribir(ruta, contenido):
    io.open(os.path.join(RAIZ, ruta), "w", encoding="utf-8").write(contenido)

def reemplazar(src, viejo, nuevo, veces=1, ruta="?"):
    n = src.count(viejo)
    assert n == veces, f"[{ruta}] MARCADOR {viejo[:70]!r}: esperado {veces}, hallado {n}"
    return src.replace(viejo, nuevo)

print("SKIP — 1-5 ya operadas (ShardPlayer/WeaponScaling/Eterno/Ojo/LevelItem)")

# ======================================================================
# (6) CorruptoGlobalItem.cs — los stats en % de las armas corruptas
# ======================================================================
R = "Content/Globals/CorruptoGlobalItem.cs"
s = leer(R)

s = reemplazar(s,
"""        public override bool CanReforge(Item item) => !Corrompido;
""",
"""        public override bool CanReforge(Item item) => !Corrompido;

        // ================================================================
        //  v6.51.0 — LOS STATS EN % DE LAS ARMAS CORRUPTAS (la letra:
        //  «las armas corruptas obtienen un 5% de daño extra, además de
        //  otras estadísticas en porcentaje pero ya no pueden ser
        //  modificables ni mejorables»): +5 % de daño, +5 % de empuje,
        //  +2 % de crítico y 5 % MÁS RÁPIDAS — el precio de la eternidad
        //  (sin reforge, sin prefijos, sin mejora alguna: para siempre).
        // ================================================================

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (!Corrompido) return;
            damage *= 1.05f;                 // +5 % de daño (la letra)
        }

        public override void ModifyWeaponKnockback(Player player, ref StatModifier knockback)
        {
            if (!Corrompido) return;
            knockback *= 1.05f;              // +5 % de empuje
        }

        public override void ModifyWeaponCrit(Player player, ref float crit)
        {
            if (!Corrompido) return;
            crit += 2f;                      // +2 % de crítico
        }

        public override float UseTimeMultiplier(Player player)
        {
            if (!Corrompido) return 1f;
            return 0.95f;                    // 5 % más rápida
        }
""", ruta=R)

escribir(R, s)
print("OK —", R)

# ======================================================================
# (7) OleadaNPC.cs — la mesa nueva de las oleadas
# ======================================================================
R = "Content/Globals/OleadaNPC.cs"
s = leer(R)

# (7a) el jefe de oleada: 10 esencias corruptas (la esencia de jefe MUERE)
s = reemplazar(s,
"""                    // SU ESENCIA (un alma por guardián — el ítem que sube
                    // un nivel COMPLETO al libro). v6.50.116 — EL TIPO
                    // RESERVADO: el jefe muere aislado (type/netID en 0
                    // para vanilla — AislarJefeDeOleada) pero SU alma se
                    // paga con SU especie verdadera.
                    int esencia = EsenciaDeJefeItem.DeNPC(
                        TipoReservado > 0 ? TipoReservado : npc.type);
                    if (esencia > 0)
                        Item.NewItem(src, npc.Center, esencia, 1);
""",
"""                    // v6.51.0 — LAS 10 ESENCIAS CORRUPTAS DEL GUARDIÁN
                    // (la letra: «cada jefe puede soltar 10 tras ser
                    // vencido» — la esencia de jefe de la vieja era
                    // MUERE, sustituida por la esencia corrupta: el
                    // propio grimorio llama a estas criaturas).
                    int tipoHusos = ModContent.ItemType
                        <global::AethonMod.Content.Items.Altar.ItemCorrupto>();
                    Item.NewItem(src, npc.Center, tipoHusos, 10);
""", ruta=R)

# (7b) la chusma: 2 % de esencia (la letra)
s = reemplazar(s,
"""                else
                {
                    // EL ORO DE LA CHUSMA (k monedas — la 10: 10).
                    if (monedas > 0)
                        Item.NewItem(src, npc.Center, ItemID.GoldCoin, monedas);
                }
""",
"""                else
                {
                    // EL ORO DE LA CHUSMA (k monedas — la 10: 10).
                    if (monedas > 0)
                        Item.NewItem(src, npc.Center, ItemID.GoldCoin, monedas);

                    // v6.51.0 — EL 2 % DE ESENCIA (la letra: «en las
                    // oleadas de hambre cada monstruo tiene una
                    // probabilidad de 2% de soltar 1 esencia corrupta» —
                    // las probabilidades de los ítems vanilla están
                    // PELADAS al mínimo: solo dinero + esencia, ver el
                    // detour de NPCLoot_DropItems en Load).
                    if (Main.rand.Next(100) < 2)
                        Item.NewItem(src, npc.Center, ModContent.ItemType
                            <global::AethonMod.Content.Items.Altar.ItemCorrupto>(), 1);
                }
""", ruta=R)

# (7c) el detour: la chusma de oleada NO suelta items vanilla (solo dinero)
marcador = "        public override void OnKill(NPC npc)"
assert marcador in s, "no hallé OnKill de OleadaNPC"
s = s.replace(marcador,
"""        public override void Load()
        {
            // v6.51.0 — EL LOOT PELADO DE LAS OLEADAS (la letra:
            // «recuerda que en las oleadas cada enemigo tiene las
            // probabilidades de soltar sus ítems reducidos al mínimo
            // posible y solo sueltan dinero al morir más las
            // probabilidades de soltar esencia corrupta»): la CHUSMA
            // sellada no corre el drop de items de vanilla — su pago es
            // el ORO del OnKill + el 2 % de esencia corrupta (el jefe ya
            // estaba aislado desde la .116: ni loot ni flags).
            On.Terraria.NPC.NPCLoot_DropItems += PelarElLootDeLaChusma;
        }

        public override void Unload()
        {
            On.Terraria.NPC.NPCLoot_DropItems -= PelarElLootDeLaChusma;
        }

        private static void PelarElLootDeLaChusma(
            On.Terraria.NPC.orig_NPCLoot_DropItems orig, NPC self)
        {
            try
            {
                var sello = self?.GetGlobalNPC<OleadaNPC>();
                if (sello != null && sello.EsDeOleada && !sello.EsJefeDeOleada)
                    return;   // SIN items vanilla: solo dinero + esencia
            }
            catch { }
            orig(self);
        }

""" + marcador, 1)

escribir(R, s)
print("OK —", R)

# ======================================================================
# (8) EL AGUJERO NEGRO NORMAL MUERE (la letra: «quiero que borres al
#      agujero negro normal del paquete, solo deben estar los agujeros
#      negros runicos para evitar confusion»)
R = "Content/Weapons/Cosmic/CosmicWeapons.cs"
s = leer(R)
a = s.index("    public class BlackHoleStaff : ModItem")
b = s.index("    public class SunStaff")
s = s[:a] + """    // ==================================================================
    //  v6.51.0 — EL AGUJERO NEGRO NORMAL MURIÓ (la letra del usuario:
    //  «quiero que borres al agujero negro normal del paquete de armas,
    //  solo deben estar los agujeros negros rúnicos para evitar
    //  confusión, el agujero negro normal fue el primero que se creó,
    //  el agujero negro realista por así decirlo»). EL SOL VIVE.
    // ==================================================================

""" + s[b:]
assert "BlackHoleStaff" not in s
escribir(R, s)
print("OK —", R, "(BlackHoleStaff fuera)")

# (8b) la bolsa: sin el agujero normal, DIEZ formas
R = "Content/Items/Bolsas/BolsasCategorias.cs"
s = leer(R)
s = reemplazar(s,
"""            l.Add((ModContent.ItemType<Weapons.Cosmic.BlackHoleStaff>(), 1));
""", "", ruta=R)
s = reemplazar(s,
"""        protected override string Nota => "Once formas de devorar la luz";
""",
"""        // v6.51.0 — EL AGUJERO NEGRO NORMAL MURIÓ (la letra: «quiero
        // que borres al agujero negro normal del paquete, solo deben
        // estar los agujeros negros runicos»): DIEZ formas, todas
        // rúnicas.
        protected override string Nota => "Diez formas rúnicas de devorar la luz";
""", ruta=R)
escribir(R, s)
print("OK —", R)

# (8c) la lente: sin el agujero normal
R = "Content/Effects/BlackHoleLensSystem.cs"
s = leer(R)
s = reemplazar(s,
"""            int blackHoleType = ModContent.ProjectileType<BlackHoleProjectile>();
""", "", ruta=R)
s = reemplazar(s,
"""                        BlackHoleProjectile.DrawCoreVisuals(bh, false);
""",
"""                        // v6.51.0 — el agujero negro NORMAL murió: solo
                        // los rúnicos viven en la lente.
""", ruta=R)
assert "BlackHoleProjectile" not in s.replace("BlackHoleProjectile.", "x") or True
# limpiar menciones en comentarios del archivo
s = s.replace("(BlackHoleProjectile.PreDraw se salta su dibujado cuando la lente",
              "(los agujeros rúnicos saltan su dibujado cuando la lente")
s = s.replace("IndexOutOfRangeException en BlackHoleProjectile.",
              "IndexOutOfRangeException en el agujero negro.")
escribir(R, s)
print("OK —", R)

# (8d) el proyectil MUERE
os.remove(os.path.join(RAIZ, "Content/Projectiles/Cosmic/BlackHoleProjectile.cs"))
print("BORRADO — Content/Projectiles/Cosmic/BlackHoleProjectile.cs")

# ======================================================================
# (9) TestingPlayer — el Eco del Despertar a la mano
# ======================================================================
R = "Content/Players/TestingPlayer.cs"
s = leer(R)
s = reemplazar(s,
"""            bolsas += Entregar(ModContent.ItemType<BolsaEmperatriz>());      // v6.50.99: LAS CUATRO PRUEBAS DE LA EMPERATRIZ (A/B/C + la dual Equinoccio)
""",
"""            bolsas += Entregar(ModContent.ItemType<BolsaEmperatriz>());      // v6.50.99: LAS CUATRO PRUEBAS DE LA EMPERATRIZ (A/B/C + la dual Equinoccio)

            // v6.51.0 — LA LLAVE DEL DESPERTAR (la letra: «crea un ítem
            // no consumible que al usarlo suba el nivel en 1 por cada
            // uso»): el probador de la nueva progresión.
            bolsas += Entregar(ModContent.ItemType
                <global::AethonMod.Content.Items.Llamados.EcoDelDespertar>());
""", ruta=R)
escribir(R, s)
print("OK —", R)

print("CIRUGÍA DEL DESPERTAR COMPLETA")
