using Terraria;
using Terraria.ModLoader;
using AethonMod.Content.Items;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// TestingPlayer — el kit de pruebas del arsenal.
    ///
    /// v6.18 — LA SEGUNDA GRAN LIMPIEZA (petición del usuario):
    /// · TODAS LAS ALAS BORRADAS (8 items + VFX + draw layer + anim player).
    /// · El AGUJERO NEGRO BASE queda OCULTO (no borrado): ya no se entrega
    ///   ni se garantiza — el código sigue en el mod por si acaso.
    /// · AGUJERO DEL VACÍO (Crimson) y FUSIÓN (base+vacío) ELIMINADOS.
    /// · SE QUEDAN los 4 agujeros definitivos: UMBRAL, BRUMA, CÓSMICO y
    ///   OLVIDO — más los nuevos de la tanda v6.18 (Supremo + Ascendidos).
    ///
    /// v6.01 — LA GRAN LIMPIEZA: el usuario seleccionó qué se queda.
    /// FUERA: las 4 armas de COLOR, 15 de las 20 V20 y las 2 cósmicas
    /// nuevas (Quásar y Galaxia Viviente). SE QUEDAN 14: los 4 tests
    /// clásicos, el Grimorio, 4 V20 y las 5 cósmicas.
    ///
    /// v5.98 — FIX DEL KIT "CONGELADO": el kit base se entrega UNA sola vez
    /// (gate por GenesisShard), pero las ARMAS CÓSMICAS EN DESARROLLO se
    /// garantizan INDIVIDUALMENTE en cada entrada al mundo.
    /// </summary>
    public class TestingPlayer : ModPlayer
    {
        public override void OnEnterWorld()
        {
            if (Main.netMode != Terraria.ID.NetmodeID.SinglePlayer) return;
            if (Player.whoAmI != Main.myPlayer) return;

            // === KIT BASE (una sola vez) ===
            if (!HasItem(ModContent.ItemType<GenesisShard>()))
            {
                GiveItem(ModContent.ItemType<GenesisShard>(), 1);
                GiveItem(Terraria.ID.ItemID.GoldBar, 100);
                GiveItem(ModContent.ItemType<LevelUpTester>(), 1);
                GiveItem(ModContent.ItemType<BossSummonBag>(), 1);
                GiveItem(ModContent.ItemType<Items.SeerOrb>(), 1);
                GiveItem(ModContent.ItemType<Weapons.TestMagicRing>(), 1);
                GiveItem(ModContent.ItemType<Weapons.TestSparkle>(), 1);
                GiveItem(ModContent.ItemType<Weapons.ProjBeam>(), 1);
                GiveItem(ModContent.ItemType<Weapons.TestMagicRingV2>(), 1);
                // v5.77: arsenal creativo V20 — v6.01: solo los 4 ELEGIDOS
                GiveItem(ModContent.ItemType<Weapons.V20.SupernovaStaff>(), 1);
                GiveItem(ModContent.ItemType<Weapons.V20.PlasmaStormStaff>(), 1);
                GiveItem(ModContent.ItemType<Weapons.V20.PhoenixNovaStaff>(), 1);
                GiveItem(ModContent.ItemType<Weapons.V20.QuantumSplitStaff>(), 1);
                // v5.80+: armas cósmicas basadas en shaders de lensing
                // (v6.18: el AGUJERO NEGRO BASE ya NO se entrega — OCULTO,
                // no borrado, petición del usuario)
                GiveItem(ModContent.ItemType<Weapons.Cosmic.SunStaff>(), 1);
                // v5.97: LA MEDUSA NEBULAR (invocador de minion cósmico)
                GiveItem(ModContent.ItemType<Weapons.Cosmic.MedusaNebularStaff>(), 1);
            }

            // === GARANTÍA INDIVIDUAL (v5.98) — las armas cósmicas en
            // desarrollo SIEMPRE están en el inventario, venga de la
            // versión que venga el guardado del jugador ===
            // (v6.01: QUÁSAR y GALAXIA VIVIENTE ELIMINADOS — "se ven
            // horrible y son muy simples, no vale la pena que continúen";
            // quien aún los tenga guardados los conserva, pero ya no se
            // garantizan. El OJO DEL VACÍO corrió la misma suerte en v6.00.)
            // (v6.18: el AGUJERO NEGRO BASE está OCULTO — no se entrega.)
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.SunStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.MedusaNebularStaff>());
            // v5.99: EL COMETA ESTELAR y EL PÚLSAR VIVO (invocadores)
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.LivingCometStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.LivingPulsarStaff>());
            // (v6.18: el AGUJERO DEL VACÍO y la FUSIÓN fueron ELIMINADOS —
            // petición del usuario; quien los tenga guardados los conserva.)
            // v6.14: EL OLVIDO (100% creado por código, v6.15)
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.OlvidoBlackHoleStaff>());
            // v6.16: EL AGUJERO CÓSMICO y EL AGUJERO DEL UMBRAL
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.CosmicBlackHoleStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.UmbralBlackHoleStaff>());
            // v6.17: EL AGUJERO DE LA BRUMA — la demostración de la
            // LIBRERÍA de humo/niebla/bruma procedural del proyecto
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.BrumaBlackHoleStaff>());
            // v6.18: LOS CINCO NUEVOS — el SUPREMO (la fusión de los 4,
            // mejorado y potenciado) + los 4 ASCENDIDOS (copias mejoradas
            // de Umbral/Bruma/Cósmico/Olvido con la librería de rayos)
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.SupremoBlackHoleStaff>());
            // v6.20: el SUPREMO AURORA — el gradiente negro→morado→azul→dorado
            // (el Supremo original dorado queda INTACTO)
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.SupremoAuroraBlackHoleStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.UmbralAscendidoBlackHoleStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.BrumaAscendidoBlackHoleStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.CosmicAscendidoBlackHoleStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.OlvidoAscendidoBlackHoleStaff>());
            // v6.19: LA FAMILIA DE LOS SOLES RÚNICOS — 10 copias del Sol
            // (el Sol original queda INTACTO), N anillos rúnicos por copia
            // con giros alternos y mejoras progresivas (la X las tiene todas)
            // (v6.26: las copias 6..19 quedan OCULTAS — petición del usuario:
            // ya no se entregan ni se garantizan; el código sigue en el mod.
            // La VEINTE recibe LA MEJORA MAYOR y la corona de la familia.)
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.SolRunico1Staff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.SolRunico2Staff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.SolRunico3Staff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.SolRunico4Staff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.SolRunico5Staff>());
            // (v6.26: SolRunico6..SolRunico19 OCULTOS — no se entregan.)
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.SolRunico20Staff>());
            // v6.22: EL ECLIPSE PRIMORDIAL — el Sol de los 20 Anillos fundido
            // con TODOS los agujeros negros (luz + bruma + humo + rayos)
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.EclipsePrimordialStaff>());
            // v6.19: EL CETRO DEL TRUENO — el arma de rayos de la librería
            // StormLib (daño en línea, cadena, arcos, electrificación)
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.StormRuneStaff>());
            // v6.03: LOS COSMÉTICOS DE LAS DOS CORONAS (la del agujero,
            // detrás de la cabeza, y la rúnica nueva — v6.24: LA AUREOLA
            // DEL SOL I ringiendo la cabeza)
            EnsureItem(ModContent.ItemType<Items.Cosmetics.VoidCrownItem>());
            EnsureItem(ModContent.ItemType<Items.Cosmetics.RuneCrownItem>());
            // (v6.24: la ENVOLTURA DE FUEGO y la ENVOLTURA DE RAYOS fueron
            // BORRADAS — petición del usuario. La CORONA DE ANILLOS RÚNICOS
            // (el anillo del Sol I sobre el cuerpo) y el ANILLO RÚNICO
            // ESTELAR (los círculos de los agujeros en la espalda) quedan.)
            EnsureItem(ModContent.ItemType<Items.Cosmetics.RuneRingCrownItem>());
            EnsureItem(ModContent.ItemType<Items.Wings.RunicHaloWings>());
            // (v6.18: las 8 alas viejas FUERON BORRADAS — petición del
            // usuario. v6.22 vuelve UNA sola, LA BUENA: el anillo rúnico.)
            // v6.24: LAS TRES ARMAS DE LAS LIBRERÍAS — cada una usa TODAS
            // (StormLib rayos + BrumaFX humo + LumenLib luz + runas)
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.SinfoniaPrimordialStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.TormentaNebularStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.LanzaAlbaStaff>());
            // v6.26: EL DESGARRO EN LA REALIDAD — RiftLib (la librería nueva
            // de las grietas espacio-temporales) + el proyectil que ATRAVIESA
            // paredes con su línea de daño
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.DesgarroRealityStaff>());
            // v6.26: LAS SEIS ESTRELLAS REALES — estrellas de neutrones,
            // púlsar, enana blanca, estrella muerta, supergigante roja y
            // magnetar (astrofísica estilizada de la casa)
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.NeutronStarStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.PulsarStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.WhiteDwarfStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.DeadStarStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.RedSupergiantStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.MagnetarStaff>());
            // v6.26: EL CICLO ESTELAR — la estrella que VIVE: nebulosa →
            // secuencia principal → gigante roja → supernova → remanente
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.CicloEstelarStaff>());
            // v6.26: LOS CINCO BASTONES CREATIVOS — el reloj de arena cósmico,
            // la marea gravitatoria, el enjambre prismático, el péndulo del
            // juicio y el coro espectral
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.RelojArenaStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.MareaGravitatoriaStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.EnjambrePrismaticoStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.PenduloJuicioStaff>());
            EnsureItem(ModContent.ItemType<Weapons.Cosmic.CoroEspectralStaff>());
        }

        /// <summary>¿El jugador tiene este ítem en el inventario (58 slots)?</summary>
        private bool HasItem(int itemType)
        {
            for (int i = 0; i < 58; i++)
            {
                if (Player.inventory[i] != null &&
                    Player.inventory[i].type == itemType)
                    return true;
            }
            return false;
        }

        /// <summary>Entrega el ítem SOLO si no lo tiene (garantía por arma).</summary>
        private void EnsureItem(int itemType)
        {
            if (HasItem(itemType)) return;
            GiveItem(itemType, 1);
        }

        private void GiveItem(int itemType, int stack)
        {
            for (int i = 0; i < 58; i++)
            {
                if (Player.inventory[i] == null ||
                    Player.inventory[i].type == Terraria.ID.ItemID.None)
                {
                    Player.inventory[i].SetDefaults(itemType);
                    Player.inventory[i].stack = stack;
                    return;
                }
            }
            int drop = Item.NewItem(Player.GetSource_GiftOrReward(), Player.Center, itemType, stack);
            if (drop >= 0 && drop < Main.item.Length)
                Main.item[drop].noGrabDelay = 0;
        }
    }
}
