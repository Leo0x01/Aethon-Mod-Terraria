using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Weapons;
using AethonMod.Content.Weapons.V20;
using AethonMod.Content.Items.Cosmetics;

namespace AethonMod.Content.Items
{
    /// <summary>
    /// ArsenalBag — v6.27 — LA BOLSA DEL ARSENAL PRIMORDIAL.
    ///
    /// Petición del usuario: "todo lo que le vas a dar al jugador ponlo en
    /// una bolsa o cofre y dale solo la bolsa con todos los objetos dentro".
    ///
    /// El kit de pruebas dejó de INUNDAR el inventario (40+ ítems por
    /// entrada al mundo): ahora `TestingPlayer` entrega SOLO ESTA BOLSA
    /// (1 ranura, garantizada) y el jugador la abre cuando quiera con
    /// CLIC DERECHO — el arsenal completo se despliega con semántica de
    /// "garantía" (solo entrega lo que FALTE, así se puede reabrir para
    /// recuperar armas perdidas sin duplicar el resto).
    ///
    /// La bolsa es PERMANENTE (no se consume): es la herramienta de
    /// pruebas del mod, no un loot puntual. El contenido es EL MISMO
    /// lista­do que vivía en `TestingPlayer.OnEnterWorld` (kit base +
    /// garantía individual), ahora en UN solo lugar de la verdad.
    /// </summary>
    public class ArsenalBag : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.maxStack = 1;
            Item.consumable = false;        // PERMANENTE: se reabre cuantas veces haga falta
            Item.rare = ItemRarityID.Red;   // el rango del arsenal supremo
            Item.value = Item.buyPrice(1, 0, 0, 0);
            Item.expert = false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "B",
                "[c/B08CFF:LA BOLSA DEL ARSENAL PRIMORDIAL]"));
            tooltips.Add(new TooltipLine(Mod, "B2",
                "[c/E6B0FF:Clic derecho para desplegar TODO el arsenal del mod]"));
            tooltips.Add(new TooltipLine(Mod, "B3",
                "[c/FFD66B:Solo entrega lo que te falte — reábrela cuando pierdas un arma]"));
            tooltips.Add(new TooltipLine(Mod, "B4",
                "[c/78788C:El kit completo de pruebas de Aethon en una sola ranura]"));
        }

        /// <summary>El arma se abre con CLIC DERECHO (patrón bolsa de
        /// recompensa, pero sin consumo — la bolsa vive para siempre).</summary>
        public override bool CanRightClick() => true;

        public override void RightClick(Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            int entregados = 0;
            foreach ((int tipo, int pila) in Contenido())
            {
                if (!Tiene(player, tipo))
                {
                    Dar(player, tipo, pila);
                    entregados++;
                }
            }

            // === LA APERTURA (el momento de la casa: FX + texto) ===
            if (Main.netMode != NetmodeID.Server)
            {
                // 30 chispas doradas + violetas alrededor del jugador.
                for (int i = 0; i < 30; i++)
                {
                    float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    Dust d = Dust.NewDustPerfect(player.Center,
                        i % 3 == 0 ? DustID.PurpleTorch : DustID.GoldFlame,
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                            Main.rand.NextFloat(1.5f, 4.5f),
                        180, default, 0.8f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
                // El latido de luz de la apertura.
                Lighting.AddLight(player.Center, 0.9f, 0.7f, 1.2f);
            }

            Terraria.Audio.SoundEngine.PlaySound(
                SoundID.Item4.WithPitchOffset(-0.25f), player.Center);

            if (player.whoAmI == Main.myPlayer)
            {
                Main.NewText(entregados > 0
                    ? $"El Arsenal Primordial se despliega: {entregados} objetos nuevos."
                    : "El Arsenal Primordial ya está completo contigo.",
                    new Color(230, 196, 255));
            }
        }

        // ==================================================================
        //  EL CONTENIDO — la lista única de la verdad del kit de pruebas
        // ==================================================================

        /// <summary>
        /// TODO lo que el mod entrega al jugador (antes eran 40+ EnsureItem
        /// individuales en TestingPlayer). Llamado en contexto de mundo
        /// cargado (RightClick) — ModContent.ItemType es seguro aquí.
        /// </summary>
        private static List<(int tipo, int pila)> Contenido()
        {
            var l = new List<(int, int)>();

            // --- EL KIT BASE (v5.77, una entrega) ---
            l.Add((ModContent.ItemType<GenesisShard>(), 1));
            l.Add((ItemID.GoldBar, 100));
            l.Add((ModContent.ItemType<LevelUpTester>(), 1));
            l.Add((ModContent.ItemType<BossSummonBag>(), 1));
            l.Add((ModContent.ItemType<Items.SeerOrb>(), 1));
            l.Add((ModContent.ItemType<Weapons.TestMagicRing>(), 1));
            l.Add((ModContent.ItemType<Weapons.TestSparkle>(), 1));
            l.Add((ModContent.ItemType<Weapons.ProjBeam>(), 1));
            l.Add((ModContent.ItemType<Weapons.TestMagicRingV2>(), 1));
            l.Add((ModContent.ItemType<Weapons.V20.SupernovaStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.V20.PlasmaStormStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.V20.PhoenixNovaStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.V20.QuantumSplitStaff>(), 1));

            // --- LAS ARMAS CÓSMICAS CLÁSICAS ---
            l.Add((ModContent.ItemType<Weapons.Cosmic.SunStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.MedusaNebularStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.LivingCometStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.LivingPulsarStaff>(), 1));

            // --- LOS AGUJEROS NEGROS DEFINITIVOS ---
            l.Add((ModContent.ItemType<Weapons.Cosmic.OlvidoBlackHoleStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.CosmicBlackHoleStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.UmbralBlackHoleStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.BrumaBlackHoleStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SupremoBlackHoleStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SupremoAuroraBlackHoleStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.UmbralAscendidoBlackHoleStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.BrumaAscendidoBlackHoleStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.CosmicAscendidoBlackHoleStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.OlvidoAscendidoBlackHoleStaff>(), 1));

            // --- LA FAMILIA DE LOS SOLES RÚNICOS (1..5 + LA VEINTE) ---
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico1Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico2Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico3Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico4Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico5Staff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.SolRunico20Staff>(), 1));

            // --- EL ECLIPSE + EL TRUENO ---
            l.Add((ModContent.ItemType<Weapons.Cosmic.EclipsePrimordialStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.StormRuneStaff>(), 1));

            // --- LOS COSMÉTICOS ---
            l.Add((ModContent.ItemType<Cosmetics.VoidCrownItem>(), 1));
            l.Add((ModContent.ItemType<Cosmetics.RuneCrownItem>(), 1));

            // --- LAS ARMAS DE LAS LIBRERÍAS (v6.24) ---
            l.Add((ModContent.ItemType<Weapons.Cosmic.SinfoniaPrimordialStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.TormentaNebularStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.LanzaAlbaStaff>(), 1));

            // --- EL DESGARRO + LAS SEIS ESTRELLAS REALES + EL CICLO (v6.26) ---
            l.Add((ModContent.ItemType<Weapons.Cosmic.DesgarroRealityStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.NeutronStarStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.PulsarStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.WhiteDwarfStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.DeadStarStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.RedSupergiantStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.MagnetarStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.CicloEstelarStaff>(), 1));

            // --- LOS CINCO BASTONES CREATIVOS (v6.26) ---
            l.Add((ModContent.ItemType<Weapons.Cosmic.RelojArenaStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.MareaGravitatoriaStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.EnjambrePrismaticoStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.PenduloJuicioStaff>(), 1));
            l.Add((ModContent.ItemType<Weapons.Cosmic.CoroEspectralStaff>(), 1));

            // --- v6.27: EL ARMA SUPREMA DEL GAUGE ---
            l.Add((ModContent.ItemType<Weapons.Cosmic.OcasoAethonStaff>(), 1));

            return l;
        }

        // ==================================================================
        //  LOS HELPERS (la semántica de "garantía" de la casa)
        // ==================================================================

        /// <summary>¿El jugador tiene este ítem en el inventario (58 slots)?</summary>
        private static bool Tiene(Player player, int itemType)
        {
            for (int i = 0; i < 58; i++)
            {
                if (player.inventory[i] != null &&
                    player.inventory[i].type == itemType)
                    return true;
            }
            return false;
        }

        /// <summary>Entrega el ítem en la primera ranura libre (o al suelo).</summary>
        private static void Dar(Player player, int itemType, int stack)
        {
            for (int i = 0; i < 58; i++)
            {
                if (player.inventory[i] == null ||
                    player.inventory[i].type == ItemID.None)
                {
                    player.inventory[i].SetDefaults(itemType);
                    player.inventory[i].stack = stack;
                    return;
                }
            }
            int drop = Item.NewItem(player.GetSource_GiftOrReward(),
                player.Center, itemType, stack);
            if (drop >= 0 && drop < Main.item.Length)
                Main.item[drop].noGrabDelay = 0;
        }
    }
}
