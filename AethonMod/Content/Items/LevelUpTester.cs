using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using AethonMod.Content.Globals;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Items
{
    /// <summary>
    /// Verdugo de Niveles (TEMPORAL) — item de testing.
    /// Da +10 niveles al primer Grimorio del Eterno encontrado en el inventario.
    /// No requiere sostenerlo: busca en todos los slots.
    ///
    /// v6.50.43 — YA NO ES CONSUMIBLE (petición del usuario: «el item
    /// que sube de nivel el grimorio es un consumible, has que no sea
    /// consumible que cada vez que lo active suba 10 niveles sin
    /// consumirse»): cada activación suma +10 niveles y el ítem QUEDA
    /// en la mano — como toda herramienta de pruebas de la casa (el
    /// patrón de la Carnada: reutilizable, pila 1).
    /// </summary>
    public class LevelUpTester : ModItem
    {
        public const int LevelsPerUse = 10;

        // v6.146 — EL SELECTOR DE POTENCIA (la letra: el click derecho
        // cambia 10 ↔ 100; el izquierdo aplica)
        public override bool CanRightClick() => Main.mouseRight && !Main.LocalPlayer.mouseInterface;

        public override void RightClick(Player player)
        {
            var sp = player.GetModPlayer<global::AethonMod.Content.Players.ShardPlayer>();
            if (sp == null) return;
            sp.VerdugoPasoCien = !sp.VerdugoPasoCien;
            Main.NewText(Language.GetTextValue(
                "Mods.AethonMod.LevelUpTester.Paso",
                sp.VerdugoPasoCien ? 100 : LevelsPerUse), new Microsoft.Xna.Framework.Color(245, 196, 81));
            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
        }

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.maxStack = 1;
            // v6.50.43 — NO CONSUMIBLE: cada uso sube +10 niveles y el
            // ítem NO se gasta (el patrón de la Carnada: herramienta de
            // pruebas reutilizable).
            Item.consumable = false;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.UseSound = SoundID.Item4;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
        }

        public override bool? UseItem(Player player)
        {
            // v6.50 — el doble gate muerto arreglado: el server corre este
            // UseItem por el uso sincronizado del jugador remoto (y él es
            // quien debe subir SU copia — EcoRed.SincronizarLibros lleva el
            // nivel nuevo al portador). El texto es local.
            // (El tester salta OnLevelUp a propósito: es una HERRAMIENTA de
            // pruebas — el flujo real (GrantXP/SubirNivelDirecto) sí celebra.)

            // Buscar el primer Grimorio del Eterno en todo el inventario.
            for (int i = 0; i < 59; i++)
            {
                Item inv = player.inventory[i];
                if (inv == null || inv.type != ModContent.ItemType<Weapons.GrimoireEternal>()) continue;

                var sl = inv.GetGlobalItem<ShardLevelItem>();
                if (sl == null) continue;

                // v6.146 — EL PASO DEL SELECTOR (la letra nueva): el
                // click derecho eligió la potencia (10 o 100); el click
                // izquierdo la APLICA — como las oleadas del hambre.
                var spVer = player.GetModPlayer<global::AethonMod.Content.Players.ShardPlayer>();
                int paso = spVer != null && spVer.VerdugoPasoCien ? 100 : LevelsPerUse;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    sl.Level += paso;

                if (player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server)
                {
                    // v6.50.2 — FIX (strings hardcodeados → hjson, regla de
                    // la casa): los dos avisos del probador viajan por clave.
                    Main.NewText(Language.GetTextValue("Mods.AethonMod.LevelUpTester.Subida",
                            inv.Name, paso, sl.Level),
                        new Microsoft.Xna.Framework.Color(245, 196, 81));

                    // Efectos visuales de subida de nivel.
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4);
                    for (int d = 0; d < 30; d++)
                    {
                        Dust.NewDustPerfect(player.Center, DustID.GoldFlame,
                            new Microsoft.Xna.Framework.Vector2(Main.rand.NextFloat(-6, 6), Main.rand.NextFloat(-6, 6)),
                            100, new Microsoft.Xna.Framework.Color(245, 196, 81), 1.5f);
                    }
                }

                EcoRed.SincronizarLibros(player); // no-op fuera del server
                return true;
            }

            if (player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server)
                Main.NewText(Language.GetTextValue("Mods.AethonMod.LevelUpTester.SinGrimorio"),
                    new Microsoft.Xna.Framework.Color(255, 120, 120));
            return false;
        }
    }
}
