using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Buffs;
using AethonMod.Content.Projectiles.Cosmetic;
using AethonMod.Content.Weapons;

namespace AethonMod.Content.Globals
{
    /// <summary>
    /// PuebloChismesNPC — v6.50.117 — LOS CHISMES DEL PUEBLO.
    ///
    /// EL ESPÍRITU (la letra del usuario): «MUCHOS diálogos únicos y
    /// divertidos para cada NPC». Cuando el jugador pasea por el
    /// pueblo con la MASCOTA puesta (el Aethon Menor y su gemelo), los
    /// vecinos comentan sus TRAVESURAS con gracia — «ayer vi los
    /// pequeños ojos de tu mascota mirándome a través de mi ventana».
    /// Cuando carga el GRIMORIO, los vecinos sueltan comentarios
    /// OSCUROS, de terror, PERTURBADORES: susurros, páginas que se
    /// voltean solas, cosas que se movieron de noche, SOMBRAS con
    /// ojos... al pueblo LE TIENE MIEDO al libro, y ya no lo disimula.
    ///
    /// Además, EL RECLAMO GARANTIZADO: la pareja de mascotas registra
    /// cada golpe de sus travesuras en AethonMenorPet.TicksMolestado
    /// (npcIndex → tick del último mordisco). El vecino molestado le
    /// reclama al jugador en su próxima charla, con cara de pocos
    /// amigos — UNA vez por pleito, sin repetir.
    /// </summary>
    public class PuebloChismesNPC : GlobalNPC
    {
        // === EL MAPA DEL PUEBLO: npc.type → la clave del vecino en el
        //     hjson (Pueblo.Chismes.<clave>.<Mascota|Grimorio><1-4> y
        //     Pueblo.Reclamos.<clave>). Los 25 vecinos vanilla con
        //     nombre y oficio; el town NPC que no esté en el mapa
        //     (otros mods) habla del pool «Genericos». Todos los
        //     nombres verificados contra el NPCID de este tML 2026.08
        //     (el Tavernkeep es DD2Bartender, la Zoólogo BestiaryGirl).
        private static readonly Dictionary<int, string> _claves = new()
        {
            { NPCID.Guide, "Guide" },
            { NPCID.Merchant, "Merchant" },
            { NPCID.Nurse, "Nurse" },
            { NPCID.ArmsDealer, "ArmsDealer" },
            { NPCID.Dryad, "Dryad" },
            { NPCID.Demolitionist, "Demolitionist" },
            { NPCID.Clothier, "Clothier" },
            { NPCID.GoblinTinkerer, "Goblin" },
            { NPCID.Wizard, "Wizard" },
            { NPCID.Mechanic, "Mechanic" },
            { NPCID.SantaClaus, "Santa" },
            { NPCID.Truffle, "Truffle" },
            { NPCID.Steampunker, "Steampunker" },
            { NPCID.DyeTrader, "DyeTrader" },
            { NPCID.PartyGirl, "PartyGirl" },
            { NPCID.Cyborg, "Cyborg" },
            { NPCID.Painter, "Painter" },
            { NPCID.WitchDoctor, "WitchDoctor" },
            { NPCID.Pirate, "Pirate" },
            { NPCID.Stylist, "Stylist" },
            { NPCID.Angler, "Angler" },
            { NPCID.DD2Bartender, "Tavernkeep" },
            { NPCID.Golfer, "Golfer" },
            { NPCID.BestiaryGirl, "Zoologist" },
            { NPCID.Princess, "Princess" },
        };

        // === LOS PLEITOS YA COBRADOS: vecinos que YA reclamaron su
        //     mordisco (un reclamo por pleito — el vecino molesto no
        //     repite la queja toda la noche). El perdón llega solo:
        //     si pasan 3600 ticks sin un solo golpe nuevo, el pueblo
        //     olvida y la próxima ronda de travesuras vuelve a tener
        //     derecho a reclamo.
        private static readonly HashSet<int> _reclamados = new();

        public override void GetChat(NPC npc, ref string chat)
        {
            // Solo los vecinos del pueblo chismean aquí
            if (!npc.townNPC)
                return;

            // === 1. EL RECLAMO GARANTIZADO: si ESTE vecino acaba de ser
            //     molestado por la pareja (menos de 5400 ticks = minuto
            //     y medio de rencor), le suelta su reclamo al jugador.
            //     _reclamados.Add devuelve true solo la PRIMERA vez de
            //     este pleito — después, el vecino se guarda la queja.
            //     (Antes de mirar nada: la limpieza del perdón.)
            LimpiarReclamosSiHayPerdon();
            if (AethonMenorPet.TicksMolestado.TryGetValue(npc.whoAmI, out int tickGolpe))
            {
                long edad = (long)Main.GameUpdateCount - tickGolpe;
                if (edad >= 0L && edad < 5400L && _reclamados.Add(npc.whoAmI))
                {
                    chat = Language.GetTextValue($"Mods.AethonMod.Pueblo.Reclamos.{ClaveNPC(npc)}");
                    return;
                }
            }

            // === 2. LOS CHISMES: si el jugador lleva la mascota o el
            //     grimorio, 45% de que el vecino suelte un chisme en
            //     vez de su línea vanilla. De la MASCOTA: travesuras
            //     con gracia. Del GRIMORIO: terror puro. Si lleva
            //     ambos, la moneda al aire entre los dos temas.
            bool mascota = TieneMascota();
            bool grimorio = TieneGrimorio();
            if (!mascota && !grimorio)
                return;
            if (Main.rand.Next(100) >= 45)
                return;

            bool deMascota = mascota && (!grimorio || Main.rand.Next(2) == 0);
            string prefijo = deMascota ? "Mascota" : "Grimorio";
            chat = Language.GetTextValue($"Mods.AethonMod.Pueblo.Chismes.{ClaveNPC(npc)}.{prefijo}{Main.rand.Next(1, 5)}");
        }

        /// <summary>La clave de chismes del vecino — o «Genericos» si
        /// no tiene bloque propio en el hjson (otros mods, el viejo
        /// del cementerio, quien sea).</summary>
        private static string ClaveNPC(NPC npc)
        {
            return _claves.TryGetValue(npc.type, out string clave) ? clave : "Genericos";
        }

        /// <summary>¿El jugador local lleva puesta la MASCOTA? El buff
        /// del Aethon Menor es quien sostiene a la pareja (el gemelo
        /// oscuro nace y vive de ese mismo buff).</summary>
        private static bool TieneMascota()
        {
            return Main.LocalPlayer.HasBuff(ModContent.BuffType<AethonMenorBuff>());
        }

        /// <summary>¿El jugador local carga el GRIMORIO? Con que esté
        /// en cualquier slot 0..58 del inventario basta — llevarlo
        /// ENCIMA asusta al pueblo igual que blandirlo.</summary>
        private static bool TieneGrimorio()
        {
            int eterno = ModContent.ItemType<GrimoireEternal>();
            Item[] inv = Main.LocalPlayer.inventory;
            for (int i = 0; i < 59 && i < inv.Length; i++)
            {
                Item slot = inv[i];
                if (slot != null && slot.type == eterno)
                    return true;
            }
            return false;
        }

        /// <summary>PostUpdate del mundo entero no existe para un
        /// GlobalNPC de charla — la limpieza del perdón se hace aquí,
        /// perezosa y barata: si llevamos 3600 ticks (un minuto) sin
        /// un solo golpe nuevo de la pareja, se borran TODOS los
        /// pleitos cobrados. La mascota se portó bien un rato largo:
        /// el pueblo olvida.</summary>
        private static void LimpiarReclamosSiHayPerdon()
        {
            if (_reclamados.Count == 0)
                return;
            uint golpeMasReciente = 0u;
            foreach (int t in AethonMenorPet.TicksMolestado.Values)
            {
                if ((uint)t > golpeMasReciente)
                    golpeMasReciente = (uint)t;
            }
            if (golpeMasReciente == 0u || (long)Main.GameUpdateCount - golpeMasReciente >= 3600L)
                _reclamados.Clear();
        }
    }
}
