using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// EcoSistema — EL ANFITRIÓN DE ECOLIB: registra la capa de interfaz
    /// de las voces (justo después de "Vanilla: Death Text", la zona del
    /// texto dramático de vanilla), actualiza la cola cada frame de UI y
    /// conoce LA TABLA DE JEFES: qué voz y qué COLOR tiene cada derrota.
    ///
    /// Los TEXTOS viven en el hjson (Eco.VozJefe.*): nada de diálogos
    /// hardcodeados — la casa localiza.
    /// El FORMATO (una línea personal por jefe, con su color propio) es
    /// el clásico de los mods de mensajes de estado: la voz del GRIMORIO
    /// devora la esencia del jefe caído y lo dice a pantalla completa.
    ///
    /// DISPARO: GlobalNPCXP.OnKill → AnunciarJefeMuerto, SOLO para el
    /// jugador local (SP-first; en MP la kill la resuelve el servidor y
    /// la voz de cada cliente es TODO pendiente como el resto del sync).
    /// Una derrota, UNA voz: las partes en cascada no hablan
    /// (ShardLevelSystem.EsParteDeJefe) y Los Gemelos (dos cuerpos, un
    /// jefe) hablan cuando cae el ÚLTIMO.
    /// </summary>
    public class EcoSistema : ModSystem
    {
        public override void UpdateUI(GameTime gameTime)
        {
            // Solo cliente: las voces viven en la pantalla del que mató.
            if (Main.gameMenu || Main.dedServ) return;
            EcoLib.Update();
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int idx = layers.FindIndex(l => l.Name == "Vanilla: Death Text");
            if (idx != -1)
            {
                layers.Insert(idx + 1, new LegacyGameInterfaceLayer(
                    "AethonMod: Voces del Grimorio",
                    () => { EcoLib.Dibujar(Main.spriteBatch); return true; },
                    InterfaceScaleType.UI));
            }
        }

        public override void OnWorldUnload()
        {
            EcoLib.Reiniciar();
            _ultimaClave = null;
            _tickUltimaClave = -10000;
        }

        public override void Unload()
        {
            EcoLib.Reiniciar();
            _ultimaClave = null;
            _tickUltimaClave = -10000;
        }

        // ================================================================
        //  LA TABLA DE JEFES — derrota → (clave hjson, color de la voz)
        // ================================================================

        /// <summary>Anti-duplicado: la última clave hablada y cuándo.</summary>
        private static string _ultimaClave = null;
        private static long _tickUltimaClave = -10000;

        /// <summary>
        /// Anuncia la derrota de un jefe con la voz del Grimorio. Llamado
        /// desde GlobalNPCXP.OnKill cuando el killer es el jugador local.
        /// v6.47: también los JEFES DEL MOD tienen su voz (Aethon, el
        /// Titán Hueco, el Guardián del Rift y Los Ecos — que hablan al
        /// caer el ÚLTIMO, como Los Gemelos).
        /// </summary>
        public static void AnunciarJefeMuerto(NPC npc)
        {
            try
            {
                if (npc == null || !npc.boss) return;

                // Los Gemelos son UN jefe con dos cuerpos: la voz suena
                // cuando cae el ÚLTIMO gemelo, no con cada uno.
                if (npc.type == NPCID.Retinazer || npc.type == NPCID.Spazmatism)
                {
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC otro = Main.npc[i];
                        if (otro != null && otro.active && otro.whoAmI != npc.whoAmI &&
                            (otro.type == NPCID.Retinazer || otro.type == NPCID.Spazmatism))
                            return; // aún vive un gemelo: la derrota no es completa
                    }
                }
                // v6.47 — LOS ECOS DEL MOD son lo mismo: la Arquera y el
                // Primer Portador son DOS cuerpos de la MISMA historia
                // (los portadores anteriores); la voz suena al caer el
                // último de los dos.
                else if (npc.type == ModContent.NPCType<Content.NPCs.EchoArcher>() ||
                         npc.type == ModContent.NPCType<Content.NPCs.EchoBlade>())
                {
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC otro = Main.npc[i];
                        if (otro != null && otro.active && otro.whoAmI != npc.whoAmI &&
                            (otro.type == ModContent.NPCType<Content.NPCs.EchoArcher>() ||
                             otro.type == ModContent.NPCType<Content.NPCs.EchoBlade>()))
                            return; // aún vive un eco: la derrota no es completa
                    }
                }
                // Partes en cascada (segmentos, ojos, manos, cabezas): no hablan.
                else if (ShardLevelSystem.EsParteDeJefe(npc))
                {
                    return;
                }

                string clave = ClaveDeVoz(npc.type);

                // Anti-duplicado: si ambos gemelos caen en el MISMO tick,
                // OnKill dispara dos veces y los dos ven "no queda gemelo"
                // — la misma clave no repite dentro de 300 ticks.
                if (clave == _ultimaClave && Main.GameUpdateCount - _tickUltimaClave < 300)
                    return;
                _ultimaClave = clave;
                _tickUltimaClave = Main.GameUpdateCount;

                string texto = Language.GetTextValue("Mods.AethonMod.Eco.VozJefe." + clave);
                if (string.IsNullOrEmpty(texto) || texto.StartsWith("Mods.AethonMod"))
                    texto = Language.GetTextValue("Mods.AethonMod.Eco.VozJefe.Desconocido");

                EcoLib.Hablar(texto, ColorDeVoz(npc.type));
            }
            catch { }
        }

        /// <summary>
        /// v6.48 — EL SABOR DEL BIOMA: la primer línea del hambre sabe a
        /// DÓNDE está el libro — cada bioma tiene SUS propias muestras de
        /// "carne de jungla", "sal del infierno"… (3 por bioma, repartidas
        /// por ElegirVariante sin repetir). La llama ShardPlayer en el
        /// primer momento de hambre; el color y la escala los pone el
        /// llamador. Devuelve "" si algo falla (el llamador calla).
        /// </summary>
        public static string SusurroDelBioma(Player p)
        {
            try
            {
                if (p == null || !p.active) return "";
                string pool;
                if (p.ZoneUnderworldHeight) pool = "Infierno";
                else if (p.ZoneDungeon) pool = "Mazmorra";
                else if (p.ZoneCorrupt) pool = "Corrupcion";
                else if (p.ZoneCrimson) pool = "Carmesi";
                else if (p.ZoneSnow) pool = "Nieve";
                else if (p.ZoneJungle) pool = "Jungla";
                else if (p.ZoneDesert) pool = "Desierto";
                else if (p.ZoneBeach) pool = "Playa";
                else if (p.ZoneHallow) pool = "Sagrado";
                else if (p.ZoneRockLayerHeight || p.ZoneDirtLayerHeight) pool = "Subsuelo";
                else if (p.ZoneSkyHeight) pool = "Cielo";
                else pool = "Superficie";
                return EcoLib.ElegirVariante("Mods.AethonMod.Eco.Bioma." + pool, 3);
            }
            catch { return ""; }
        }

        /// <summary>La clave hjson de la voz de cada derrota (pública: el
        /// Testigo cronista reutiliza la MISMA tabla para su versión
        /// humana de la misma derrota — dos narradores, un hecho).</summary>
        public static string ClaveDeVoz(int type)
        {
            if (type == NPCID.KingSlime) return "KingSlime";
            if (type == NPCID.EyeofCthulhu) return "EyeofCthulhu";
            if (type == NPCID.Deerclops) return "Deerclops";
            if (type == NPCID.EaterofWorldsHead) return "EaterofWorlds";
            if (type == NPCID.BrainofCthulhu) return "BrainofCthulhu";
            if (type == NPCID.QueenBee) return "QueenBee";
            if (type == NPCID.SkeletronHead) return "Skeletron";
            if (type == NPCID.WallofFlesh) return "WallofFlesh";
            if (type == NPCID.QueenSlimeBoss) return "QueenSlime";
            if (type == NPCID.Retinazer || type == NPCID.Spazmatism) return "Twins";
            if (type == NPCID.TheDestroyer) return "Destroyer";
            if (type == NPCID.SkeletronPrime) return "SkeletronPrime";
            if (type == NPCID.Plantera) return "Plantera";
            if (type == NPCID.Golem) return "Golem";
            if (type == NPCID.DukeFishron) return "DukeFishron";
            if (type == NPCID.HallowBoss) return "EmpressOfLight";
            if (type == NPCID.CultistBoss) return "LunaticCultist";
            if (type == NPCID.MoonLordCore) return "MoonLord";
            // v6.47 — LOS JEFES DEL MOD, ahora que se pueden convocar:
            try
            {
                if (type == ModContent.NPCType<Content.NPCs.AethonBoss>()) return "Aethon";
                if (type == ModContent.NPCType<Content.NPCs.HollowTitan>()) return "HollowTitan";
                if (type == ModContent.NPCType<Content.NPCs.RiftKeeper>()) return "RiftKeeper";
                if (type == ModContent.NPCType<Content.NPCs.EchoArcher>() ||
                    type == ModContent.NPCType<Content.NPCs.EchoBlade>()) return "LosEcos";
            }
            catch { }
            return "Desconocido"; // jefes del mod desconocidos: la voz genérica
        }

        /// <summary>
        /// El color de la voz — uno por jefe, su temática. La paleta de
        /// la casa (sin azules planos de interfaz): tintes vivos sobre el
        /// fondo del juego, siempre legibles con la sombra del EcoLib.
        /// </summary>
        private static Color ColorDeVoz(int type)
        {
            if (type == NPCID.KingSlime) return new Color(62, 219, 191);     // gelatina turquesa
            if (type == NPCID.EyeofCthulhu) return new Color(226, 64, 64);    // carmesí
            if (type == NPCID.Deerclops) return new Color(168, 220, 242);     // escarcha
            if (type == NPCID.EaterofWorldsHead) return new Color(170, 102, 235); // corruptela
            if (type == NPCID.BrainofCthulhu) return new Color(255, 102, 140);// carne carmesí
            if (type == NPCID.QueenBee) return new Color(255, 192, 55);       // miel
            if (type == NPCID.SkeletronHead) return new Color(216, 208, 180); // hueso pálido
            if (type == NPCID.WallofFlesh) return new Color(255, 120, 60);    // infierno
            if (type == NPCID.QueenSlimeBoss) return new Color(255, 140, 190);// azúcar rosa
            if (type == NPCID.Retinazer || type == NPCID.Spazmatism)
                return new Color(255, 105, 90);                              // fuego doble
            if (type == NPCID.TheDestroyer) return new Color(250, 130, 200);  // metal magenta
            if (type == NPCID.SkeletronPrime) return new Color(255, 170, 70); // óxido
            if (type == NPCID.Plantera) return new Color(110, 230, 90);       // clorofuria
            if (type == NPCID.Golem) return new Color(230, 160, 70);          // núcleo solar
            if (type == NPCID.DukeFishron) return new Color(150, 220, 255);   // sal y tormenta
            if (type == NPCID.HallowBoss) return new Color(255, 230, 120);    // destello prisma
            if (type == NPCID.CultistBoss) return new Color(100, 240, 255);   // teletransporte
            if (type == NPCID.MoonLordCore) return new Color(200, 140, 255);  // luz lunar
            // v6.47 — los colores de los jefes del mod
            try
            {
                if (type == ModContent.NPCType<Content.NPCs.AethonBoss>())
                    return new Color(196, 150, 255);   // la luz primordial
                if (type == ModContent.NPCType<Content.NPCs.HollowTitan>())
                    return new Color(168, 232, 255);   // cristal del Sagrario
                if (type == ModContent.NPCType<Content.NPCs.RiftKeeper>())
                    return new Color(96, 224, 220);    // teal del entre-mundos
                if (type == ModContent.NPCType<Content.NPCs.EchoArcher>() ||
                    type == ModContent.NPCType<Content.NPCs.EchoBlade>())
                    return new Color(255, 178, 96);    // ámbar de los portadores
            }
            catch { }
            return new Color(245, 196, 81);                                   // el dorado del grimorio
        }

        /// <summary>
        /// v6.47 — LA PRIMERA 5★: la primera criatura 5 estrellas que el
        /// libro se come merece su LÍNEA propia. La llama GlobalNPCXP al
        /// detectarla (una vez por libro).
        /// </summary>
        public static void SusurrarCincoEstrellas()
        {
            try
            {
                EcoLib.Hablar(
                    Language.GetTextValue("Mods.AethonMod.Eco.VozCincoEstrellas"),
                    new Color(255, 122, 218),  // magenta raro
                    rugido: false, escala: 0.62f);
            }
            catch { }
        }
    }
}
