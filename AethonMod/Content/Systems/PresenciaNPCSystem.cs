using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// PresenciaNPCSystem — v6.47 — EL TESTIGO SIEMPRE ESTÁ.
    ///
    /// Petición del usuario: "haz que aparezcan siempre" — el Testigo
    /// era un townNPC SIN lógica de spawn natural (solo nacía por
    /// comando: la auditoría R44 lo documentó como quirk (g)). El mod
    /// no tiene sistema de casas para NPCs cósmicos, así que la casa
    /// resuelve con PRESENCIA GARANTIZADA: cada 10 segundos, si no hay
    /// NINGÚN Testigo en el mundo, uno APARECE cerca del jugador (busca
    /// suelo y aire, 12 intentos; a falta de hueco, flota donde pueda —
    /// es inmortal y no choca con el mundo más que por educación).
    ///
    /// Corre en el servidor del mundo (SP = el mismo proceso).
    /// </summary>
    public class PresenciaNPCSystem : ModSystem
    {
        private int _ticks = 0;

        public override void PostUpdateWorld()
        {
            _ticks++;
            if (_ticks < 600) return; // cada 10 s
            _ticks = 0;

            try
            {
                // ¿ya hay un Testigo? entonces ya está siempre.
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n != null && n.active &&
                        n.type == ModContent.NPCType<Content.NPCs.TheWitness>())
                        return;
                }

                // el jugador de anclaje: el primero vivo
                Player ancla = null;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p != null && p.active && !p.dead) { ancla = p; break; }
                }
                if (ancla == null) return;

                // 12 intentos de hueco alrededor del ancla
                for (int intento = 0; intento < 12; intento++)
                {
                    float ang = intento * (MathHelper.TwoPi / 12f) + Main.rand.NextFloat(0.5f);
                    float dist = 520f + Main.rand.NextFloat(260f);
                    Vector2 pos = ancla.Center + new Vector2(
                        MathF.Cos(ang), MathF.Sin(ang)) * dist;
                    if (HuecoValido(pos))
                    {
                        int idx = NPC.NewNPC(ancla.GetSource_FromAI(),
                            (int)pos.X, (int)pos.Y,
                            ModContent.NPCType<Content.NPCs.TheWitness>());
                        if (idx >= 0 && idx < Main.maxNPCs)
                        {
                            Main.npc[idx].netUpdate = true;
                            // v6.49 — LOCALIZADO (hallazgo AUD-C: el único
                            // texto del sistema estaba hardcodeado).
                            if (Main.netMode != NetmodeID.Server)
                                Main.NewText(
                                    Terraria.Localization.Language.GetTextValue(
                                        "Mods.AethonMod.Presencia.Llegada", Main.npc[idx].FullName),
                                    new Color(196, 150, 255));
                        }
                        return;
                    }
                }
            }
            catch { }
        }

        /// <summary>¿Hay aire de 2×4 con suelo debajo para el Testigo? (bordes respetados)</summary>
        private static bool HuecoValido(Vector2 pos)
        {
            int x = (int)(pos.X / 16f);
            int y = (int)(pos.Y / 16f);
            if (x < 5 || x >= Main.maxTilesX - 7 || y < 5 || y >= Main.maxTilesY - 7)
                return false; // fuera del mundo: otro intento
            for (int dx = 0; dx < 2; dx++)
            {
                for (int dy = 0; dy < 4; dy++)
                {
                    Tile t = Main.tile[x + dx, y + dy];
                    if (t == null || t.HasTile) return false;
                }
            }
            Tile suelo = Main.tile[x, y + 4];
            return suelo != null && suelo.HasTile && Main.tileSolid[suelo.TileType];
        }

        public override void OnWorldUnload() { _ticks = 0; }
        public override void Unload() { _ticks = 0; }
    }
}
