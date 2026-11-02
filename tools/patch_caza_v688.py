#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.88 — parche 3 de DespertarGrimorio.cs: LA CAZA REPARTIDA 1-100.
   JefeDelCamino gana VECES y CONTADOR; Fishron entra como puerta (37);
   Aethon sale del 59 y custodia la 100 (compuesta: Aethon×1 + ML×10 +
   Emperatriz×5); las repeticiones de ML/EoL reparten la caza por 63-99."""
import io

RUTA = "AethonMod/Content/Systems/DespertarGrimorio.cs"
with io.open(RUTA, "r", encoding="utf-8") as f:
    lineas = f.readlines()

# Rango: struct JefeDelCamino (609-1=608 índice) .. fin de JefeYaCaido (737-1=736 índice)
i0 = 608   # "        /// <summary>UNA PUERTA DE LA CAZA..."
i1 = 737   # línea 738 (vacía tras la llave de JefeYaCaido) — índice 737 exclusive-ish

assert "UNA PUERTA DE LA CAZA" in lineas[i0], lineas[i0]
assert lineas[i1-1].strip() == "}", lineas[i1-1]
assert "¿EL JEFE DE ESTA PUERTA YA CAÍDO?" in "".join(lineas[i0:i1])

bloque = '''        /// <summary>UNA PUERTA DE LA CAZA: el nivel que la exige, la
        /// clave hjson de su nombre, el NPC de la miniatura (la ventana
        /// del Cielo pinta su retrato) — v6.88: LAS VECES que la puerta
        /// exige (1 = la de siempre; 2+ = el jefe caído DOS o más veces:
        /// «se puede pedir matar a un jefe varias veces seguidas») y EL
        /// CONTADOR que la paga: 0 = la lápida del mundo + el registro
        /// del cazador (la de siempre) · 1 = Moon Lord (VecesMoonLord)
        /// · 2 = la Emperatriz (VecesEmperatriz) · 3 = Aethon y SU cima
        /// compuesta (la puerta 100).</summary>
        public readonly struct JefeDelCamino
        {
            public readonly int Nivel;
            public readonly string Clave;
            public readonly int Miniatura;
            public readonly int Veces;
            public readonly int Contador;
            public JefeDelCamino(int nivel, string clave, int miniatura,
                int veces = 1, int contador = 0)
            {
                Nivel = nivel;
                Clave = clave;
                Miniatura = miniatura;
                Veces = Math.Max(1, veces);
                Contador = contador;
            }
        }

        /// <summary>v6.88 — LA CAZA REPARTIDA DE LA UNO A LA CIEN (la
        /// letra: «el orden de los jefes como requisito del Despertar
        /// esta mal, usa la wiki para ver el orden correcto… recuerda
        /// que ahora son 100 niveles, y ubicaste a todos en los primeros
        /// 50 niveles» + «ya que como requisitos pides items del Duque
        /// Fishron entonces incluye a ese jefe entre los requisitos» +
        /// «el jefe Aethon no deberia estar como requisito para el nivel
        /// 59, Aethon deberia estar como requisito para el nivel 100,
        /// junto con 10 señores de la luna y 5 emperatris de la luz»):
        /// la escalera 1-50 queda como la v6.86 (VERIFICADA contra la
        /// progresión CANÓNICA de la wiki — cada puerta en la era de
        /// sus armas), el DUQUE FISHRON entra como puerta en el 37 (sus
        /// armas ya vivían ahí — ahora es puerta Y botín, el mismo
        /// latido), Aethon ABANDONA el 59 y custodia LA CIEN — y las
        /// REPETICIONES (Moon Lord y Emperatriz, la letra: «recuerda
        /// que se puede pedir matar a un jefe varias veces seguidas, asi
        /// que distribuye bien todos los requisistos de 1 a 100»)
        /// reparten la caza por la segunda mitad de la escalera: la
        /// cima de vanilla (Señor de la Luna, 50) sigue en su sitio con
        /// las armas de su drop detrás (51-58, OBTENIBLES en su era — la
        /// ley de la casa), y de la 63 a la 99 el cazador vuelve a
        /// tirar de los dos titanes hasta la cuenta completa: la puerta
        /// 100 — LA CIMA COMPUESTA: Aethon ×1 Y Moon Lord ×10 Y
        /// Emperatriz ×5 — y ahí nace LA IRA (la letra: «en el nivel 100
        /// es donde se consigue la habilidad definitiva del grimorio»).</summary>
        public static readonly JefeDelCamino[] LosJefesDelDespertar =
        {
            new JefeDelCamino(3,  "Mods.AethonMod.Despertar.JefeReyGelatina",     NPCID.KingSlime),
            new JefeDelCamino(5,  "Mods.AethonMod.Despertar.JefeOjoDeCthulhu",    NPCID.EyeofCthulhu),
            new JefeDelCamino(8,  "Mods.AethonMod.Despertar.JefeElMalDelMundo",   NPCID.EaterofWorldsHead),
            new JefeDelCamino(10, "Mods.AethonMod.Despertar.JefeReinaAbeja",      NPCID.QueenBee),
            new JefeDelCamino(13, "Mods.AethonMod.Despertar.JefeSkeletron",       NPCID.SkeletronHead),
            new JefeDelCamino(20, "Mods.AethonMod.Despertar.JefeLaMuralla",       NPCID.WallofFlesh),
            new JefeDelCamino(23, "Mods.AethonMod.Despertar.JefeReinaGelatina",   NPCID.QueenSlimeBoss),
            new JefeDelCamino(25, "Mods.AethonMod.Despertar.JefeLosGemelos",      NPCID.Retinazer),
            new JefeDelCamino(28, "Mods.AethonMod.Despertar.JefeElDevorador",     NPCID.TheDestroyer),
            new JefeDelCamino(30, "Mods.AethonMod.Despertar.JefeSkeletronPrime",  NPCID.SkeletronPrime),
            new JefeDelCamino(32, "Mods.AethonMod.Despertar.JefePlantera",        NPCID.Plantera),
            new JefeDelCamino(35, "Mods.AethonMod.Despertar.JefeGolem",           NPCID.Golem),
            new JefeDelCamino(37, "Mods.AethonMod.Despertar.JefeDuqueFishron",    NPCID.DukeFishron),
            new JefeDelCamino(38, "Mods.AethonMod.Despertar.JefeEmperatrizDeLuz", NPCID.HallowBoss),
            new JefeDelCamino(45, "Mods.AethonMod.Despertar.JefeCultistaLunatico",NPCID.CultistBoss),
            new JefeDelCamino(50, "Mods.AethonMod.Despertar.JefeSenorDeLaLuna",   NPCID.MoonLordCore),
            // v6.88 — LAS REPETICIONES (la caza repartida por la cima):
            new JefeDelCamino(63, "Mods.AethonMod.Despertar.JefeSenorDeLaLuna",   NPCID.MoonLordCore, 2, 1),
            new JefeDelCamino(67, "Mods.AethonMod.Despertar.JefeEmperatrizDeLuz", NPCID.HallowBoss,   2, 2),
            new JefeDelCamino(71, "Mods.AethonMod.Despertar.JefeSenorDeLaLuna",   NPCID.MoonLordCore, 3, 1),
            new JefeDelCamino(75, "Mods.AethonMod.Despertar.JefeEmperatrizDeLuz", NPCID.HallowBoss,   3, 2),
            new JefeDelCamino(79, "Mods.AethonMod.Despertar.JefeSenorDeLaLuna",   NPCID.MoonLordCore, 4, 1),
            new JefeDelCamino(83, "Mods.AethonMod.Despertar.JefeEmperatrizDeLuz", NPCID.HallowBoss,   4, 2),
            new JefeDelCamino(87, "Mods.AethonMod.Despertar.JefeSenorDeLaLuna",   NPCID.MoonLordCore, 5, 1),
            new JefeDelCamino(91, "Mods.AethonMod.Despertar.JefeEmperatrizDeLuz", NPCID.HallowBoss,   5, 2),
            new JefeDelCamino(95, "Mods.AethonMod.Despertar.JefeSenorDeLaLuna",   NPCID.MoonLordCore, 7, 1),
            new JefeDelCamino(99, "Mods.AethonMod.Despertar.JefeSenorDeLaLuna",   NPCID.MoonLordCore, 9, 1),
            // LA CIMA COMPUESTA (la letra): Aethon ×1 + Moon Lord ×10 + Emperatriz ×5
            new JefeDelCamino(100,"Mods.AethonMod.Despertar.JefeAethon",          MiniAethon(),       1, 3),
        };

        /// <summary>v6.86 — LA MINIATURA DE AETHON (la ventana del Cielo
        /// pinta su retrato): ModContent NO está listo durante la
        /// inicialización estática de un array — el helper try/catchea
        /// y cae al Rey Gelatina (la cara es cosmética; la PUERTA la
        /// pone el registro del cazador, no la miniatura).</summary>
        private static int MiniAethon()
        {
            try { return ModContent.NPCType<global::AethonMod.Content.NPCs.AethonBoss>(); }
            catch { return NPCID.KingSlime; }
        }

        /// <summary>¿ESTE nivel exige un jefe? (la puerta de la caza — o
        /// null si el nivel no tiene puerta).</summary>
        public static JefeDelCamino? JefeDeNivel(int nivel)
        {
            for (int i = 0; i < LosJefesDelDespertar.Length; i++)
                if (LosJefesDelDespertar[i].Nivel == nivel)
                    return LosJefesDelDespertar[i];
            return null;
        }

        /// <summary>LA BANDERA DEL MUNDO de una puerta (la lápide de
        /// vanilla: la escribió Terraria al morir el jefe, en este
        /// mundo, por quien fuera — la fuente del crédito retroactivo
        /// de SIEMPRE). La Muralla no tiene bandera propia: su lápida
        /// ES el modo difícil (la única puerta al hardmode).
        /// v6.88 — el DUQUE FISHRON gana SU puerta (37) con su lápida
        /// de mundo; Aethon (100) sigue SIN lápida (es del mod): la paga
        /// el REGISTRO DEL CAZADOR — y las REPETICIONES (63-99) no leen
        /// la lápida (una lápida es UNA sola vez): leen los CONTADORES.</summary>
        public static bool BanderaDelMundo(int nivel)
        {
            return nivel switch
            {
                3  => NPC.downedSlimeKing,
                5  => NPC.downedBoss1,
                8  => NPC.downedBoss2,          // el Devorador O el Cerebro (el mal del mundo)
                10 => NPC.downedQueenBee,
                13 => NPC.downedBoss3,
                20 => Main.hardMode,            // la Muralla de Carne
                23 => NPC.downedQueenSlime,
                25 => NPC.downedMechBoss1,      // Los Gemelos
                28 => NPC.downedMechBoss2,      // El Devorador
                30 => NPC.downedMechBoss3,      // Skeletron Prime
                32 => NPC.downedPlantBoss,      // Plantera
                35 => NPC.downedGolemBoss,
                37 => NPC.downedFishron,        // v6.88 — EL DUQUE (la puerta nueva)
                38 => NPC.downedEmpressOfLight, // la Emperatriz de la Luz
                45 => NPC.downedAncientCultist, // la antesala de la cima
                50 => NPC.downedMoonlord,       // LA CIMA de vanilla: el Señor de la Luna
                // 63-99 — las REPETICIONES: sin lápida (la pagan los contadores)
                // 100 — Aethon: sin bandera de mundo (la paga el cazador)
                _  => false,
            };
        }

        /// <summary>¿EL JEFE DE ESTA PUERTA YA CAÍDO? (el crédito
        /// retroactivo de la letra: la bandera del mundo O el registro
        /// del cazador — mátalo cuando quieras, ANTES o DESPUÉS de
        /// llegar al nivel: cuenta igual). Sin puerta: siempre true.
        /// v6.88 — LAS REPETICIONES: las puertas de Moon Lord (63+) y
        /// la Emperatriz (67+) leen SUS CONTADORES (una lápide es UNA
        /// vez; el contador es para siempre); y LA CIMA COMPUESTA (100):
        /// Aethon ≥ 1 Y Moon Lord ≥ 10 Y Emperatriz ≥ 5 — la caza
        /// completa ANTES de subir a la cima donde nace LA IRA.</summary>
        public static bool JefeYaCaido(Player p, int nivel)
        {
            JefeDelCamino? j = JefeDeNivel(nivel);
            if (j == null) return true;
            if (BanderaDelMundo(nivel) && j.Value.Veces <= 1) return true;
            if (p != null && p.active)
            {
                var cazador = p.GetModPlayer<Players.CazaJefesPlayer>();
                if (cazador != null)
                {
                    switch (j.Value.Contador)
                    {
                        case 1:  // Moon Lord (las repetidas de la 63 a la 99)
                            return cazador.VecesMoonLord >= j.Value.Veces;
                        case 2:  // la Emperatriz (las repetidas de la 67 a la 91)
                            return cazador.VecesEmperatriz >= j.Value.Veces;
                        case 3:  // LA CIMA COMPUESTA (100): Aethon + ML×10 + EoL×5
                            return cazador.VecesAethon >= 1
                                && cazador.VecesMoonLord >= 10
                                && cazador.VecesEmperatriz >= 5;
                        default:
                            if (nivel == 100 && cazador.VecesAethon >= 1) return true;
                            if (cazador.Caido(nivel)) return true;
                            return false;
                    }
                }
            }
            return false;
        }
'''

nuevo = lineas[:i0] + [bloque] + lineas[i1:]
with io.open(RUTA, "w", encoding="utf-8") as f:
    f.writelines(nuevo)
print(f"OK — {RUTA}: caza repartida 1-100 ({len(lineas)} → {len(nuevo)} líneas)")
