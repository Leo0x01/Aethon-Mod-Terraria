using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Sistema de eventos cósmicos — dispara eventos ambientales según el nivel
    /// del fragmento de cada jugador.
    ///
    /// Niveles de evento:
    /// - Lv 25: Lluvia de Luz Estelar (meteoros ambientales, mineral raro).
    /// - Lv 50: El Sagrario Hueco se extiende.
    /// - Lv 75: Rifts dimensionales (mini-mazmorras).
    /// - Lv 100: El agitar de Aethon (Ecos aparecen).
    /// - Lv 150: El despertar (jefe final disponible).
    /// </summary>
    public class CosmicEventSystem : ModSystem
    {
        // Trackea qué hitos ya se han disparado por jugador (simplificado: 1 flag global).
        private static bool _milestone25Triggered = false;
        private static bool _milestone50Triggered = false;
        private static bool _milestone75Triggered = false;
        private static bool _milestone100Triggered = false;
        private static bool _milestone150Triggered = false;

        private int _meteorTimer = 0;

        public override void PostUpdateWorld()
        {
            // Iterar todos los jugadores activos.
            foreach (Player player in Main.ActivePlayers)
            {
                if (player == null || !player.active || player.dead) continue;

                // Leer nivel del Grimorio sostenido directamente del item.
                Item held = player.HeldItem;
                if (held == null || held.type != ModContent.ItemType<Weapons.GrimoireEternal>()) continue;

                int level = 1;
                try
                {
                    var sl = held.GetGlobalItem<Globals.ShardLevelItem>();
                    if (sl != null) level = sl.Level;
                }
                catch { continue; }

                // --- Hitos de un solo disparo (por jugador, en SP) ---
                // Nota: estos flags son globales; en MP deberian ser por-jugador.
                if (level >= 25 && !_milestone25Triggered)
                {
                    _milestone25Triggered = true;
                    AnnounceMilestone("Lluvia de Luz Estelar");
                }
                if (level >= 50 && !_milestone50Triggered)
                {
                    _milestone50Triggered = true;
                    AnnounceMilestone("El Sagrario Hueco se extiende");
                }
                if (level >= 75 && !_milestone75Triggered)
                {
                    _milestone75Triggered = true;
                    AnnounceMilestone("Rifts Dimensionales");
                }
                if (level >= 100 && !_milestone100Triggered)
                {
                    _milestone100Triggered = true;
                    AnnounceMilestone("Aethon se agita — Ecos despiertan");
                }
                if (level >= 150 && !_milestone150Triggered)
                {
                    _milestone150Triggered = true;
                    AnnounceMilestone("El Despertar — Aethon disponible");
                }

                var config = ModContent.GetInstance<Content.AethonConfig>();
                bool eventsEnabled = config?.EnableCosmicEvents ?? true;
                bool starlightEnabled = config?.EnableStarlightRain ?? true;
                bool riftsEnabled = config?.EnableDimensionalRifts ?? true;

                // --- Evento continuo: Lluvia de Luz Estelar (Lv 25+) ---
                if (eventsEnabled && starlightEnabled && level >= 25)
                {
                    UpdateStarlightRain(player);
                }

                // --- Evento continuo: Rifts dimensionales (Lv 75+) ---
                // Solo el servidor spawnea NPCs para evitar duplicados en MP.
                if (eventsEnabled && riftsEnabled && level >= 75 && Main.netMode != Terraria.ID.NetmodeID.MultiplayerClient)
                {
                    UpdateDimensionalRifts(player);
                }
            }
        }

        /// <summary>Lluvia de Luz Estelar: meteoros dorados ambientales periódicos.</summary>
        private void UpdateStarlightRain(Player player)
        {
            _meteorTimer++;
            if (_meteorTimer >= 600) // cada ~10 segundos
            {
                _meteorTimer = 0;
                // Spawn un meteoro dorado cerca del jugador.
                Vector2 pos = player.Center + new Vector2(
                    Main.rand.NextFloat(-600, 600),
                    -600f);
                Vector2 vel = new Vector2(0, 8f);
                Projectile.NewProjectile(
                    player.GetSource_FromThis(),
                    pos, vel,
                    ProjectileID.StarCannonStar, // visual, no daña
                    0, 0f,
                    Main.myPlayer);
            }
        }

        /// <summary>Rifts dimensionales: chance de spawn de mini-estructuras (simplificado).</summary>
        private void UpdateDimensionalRifts(Player player)
        {
            // Simplificado: chance baja de spawn un Guardián del Rift si no hay jefe activo.
            if (Main.rand.NextBool(36000) && !NPC.AnyNPCs(ModContent.NPCType<NPCs.RiftKeeper>()))
            {
                // Solo si el jugador está bajo tierra.
                if (player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight)
                {
                    NPC.NewNPC(
                        player.GetSource_FromThis(),
                        (int)player.Center.X + Main.rand.Next(-300, 300),
                        (int)player.Center.Y - 200,
                        ModContent.NPCType<NPCs.RiftKeeper>());
                    Main.NewText("¡Un Rift dimensional se ha abierto!", new Color(61, 214, 196));
                }
            }
        }

        private void AnnounceMilestone(string name)
        {
            Main.NewText($"✦ Hitos cósmico: {name}", new Color(245, 196, 81));
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, Main.LocalPlayer.Center);
        }

        /// <summary>Resetea los flags de hitos (para testeo o nueva partida).</summary>
        public static void ResetMilestones()
        {
            _milestone25Triggered = false;
            _milestone50Triggered = false;
            _milestone75Triggered = false;
            _milestone100Triggered = false;
            _milestone150Triggered = false;
        }

        public override void OnWorldLoad()
        {
            // Resetear hitos al cargar un mundo nuevo (evita que se queden "ya triggered").
            ResetMilestones();
        }

        public override void OnWorldUnload()
        {
            ResetMilestones();
        }
    }
}
