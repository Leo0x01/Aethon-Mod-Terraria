using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using AethonMod.Content.NPCs;

namespace AethonMod.Content.Items.Llamados
{
    // ======================================================================
    //  v6.47 — LOS LLAMADOS: los invocadores de los JEFES DEL MOD
    //
    //  Petición del usuario: "para cada jefe un ítem invocador y que se
    //  invoque de día". Los cinco jefes del mod eran FANTASMAS de código
    //  (ningún spawn natural, ningún invocador: no había forma de verlos
    //  en el juego). Ahora cada uno responde a su llamado — DE DÍA los
    //  cuatro guardianes (la noche es del Ojo y de los muertos)…
    //  Y A CUALQUIER HORA EL NOMBRE DE AETHON (v6.50.43): «no tiene
    //  sentido eso ya que al invocar el jefe el tiempo pasa hasta que
    //  el sol está en el centro del cielo, así que no importa la hora
    //  de invocarlo» — su llegada trae el mediodía consigo.
    //
    //  Contrato de la casa para todos: reutilizables (no consumibles —
    //  el mod entero es de pruebas), rugido al nacer, aviso de "ya vive
    //  uno" si intentas doblar, y el mensaje localizado de noche.
    // ======================================================================

    /// <summary>EL LLAMADO COMÚN: la maquinaria de los invocadores.</summary>
    public abstract class LlamadoDeJefe : ModItem
    {
        /// <summary>El NPC que convoca (el tipo del mod).</summary>
        protected abstract int NpcConvocado { get; }

        /// <summary>
        /// ¿Se puede llamar de NOCHE? (v6.50.43) — false por defecto: los
        /// guardianes son criaturas del día. EL NOMBRE DE AETHON lo rompe:
        /// su llegada CORRE EL TIEMPO hasta el próximo mediodía — la hora
        /// del llamado no importa (la noche solo hace el viaje más largo).
        /// </summary>
        protected virtual bool ConvocableDeNoche => false;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 1;
            Item.consumable = false;          // reutilizable: es de pruebas
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item4;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
        }

        /// <summary>¿El jefe ya vive? No se dobla el llamado.</summary>
        protected bool YaVive()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n != null && n.active && n.type == NpcConvocado) return true;
            }
            return false;
        }

        public override bool CanUseItem(Player player)
        {
            // SOLO DE DÍA — salvo los CONVOCABLES DE NOCHE (v6.50.43: para
            // Aethon el gate no tenía sentido — su llegada CORRE el reloj
            // hasta el mediodía; de noche, la noche entera pasa visiblemente
            // y el sol SE POSA en el centro del cielo).
            if (!ConvocableDeNoche && !Main.dayTime)
            {
                if (player.whoAmI == Main.myPlayer)
                    Main.NewText(Language.GetTextValue("Mods.AethonMod.Llamado.SoloDia"),
                        new Color(170, 160, 150));
                return false;
            }
            if (YaVive())
            {
                if (player.whoAmI == Main.myPlayer)
                    Main.NewText(Language.GetTextValue("Mods.AethonMod.Llamado.YaVive"),
                        new Color(170, 160, 150));
                return false;
            }
            return true;
        }

        public override bool? UseItem(Player player)
        {
            // v6.50 — EL DOBLE GATE MUERTO (hallazgo auditoría MP nº5): el
            // guard myPlayer bloqueaba al SERVER (que corre este UseItem por
            // el uso SINCRONIZADO del jugador remoto — "Called on local,
            // server, and remote clients", doc oficial) y el guard de
            // netMode bloqueaba al cliente — en MP NADIE convocaba al jefe.
            // El servidor convoca y netUpdate lo difunde a todos.
            if (Main.netMode == NetmodeID.MultiplayerClient) return null; // el servidor manda

            // Nace a la vista, frente al portador y en alto.
            Vector2 pos = player.Center + new Vector2(player.direction * -420f, -180f);
            int idx = NPC.NewNPC(player.GetSource_ItemUse(Item), (int)pos.X, (int)pos.Y, NpcConvocado);
            if (idx >= 0 && idx < Main.maxNPCs)
            {
                Main.npc[idx].netUpdate = true;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, Main.npc[idx].Center);
            }
            return true;
        }
    }

    /// <summary>
    /// El Cristal del Titán Hueco — convoca al guardián del Sagrario.
    /// Fragmento del coloso cristalino que despertó bajo tierra.
    /// </summary>
    public class CristalDelTitanHueco : LlamadoDeJefe
    {
        protected override int NpcConvocado => ModContent.NPCType<HollowTitan>();
    }

    /// <summary>
    /// El Sello del Rift — convoca al Guardián del Rift.
    /// La llave del que existe mitad aquí, mitad entre mundos.
    /// </summary>
    public class SelloDelRift : LlamadoDeJefe
    {
        protected override int NpcConvocado => ModContent.NPCType<RiftKeeper>();
    }

    /// <summary>
    /// La Pluma de la Arquera — convoca al Eco de la Arquera Estelar.
    /// Cayó del cielo la vez que ella intentó derribar a Aethon.
    /// </summary>
    public class PlumaDeLaArquera : LlamadoDeJefe
    {
        protected override int NpcConvocado => ModContent.NPCType<EchoArcher>();
    }

    /// <summary>
    /// La Sombra del Portador — convoca al Eco del Primer Portador.
    /// La silueta del primer alma que se ató a un fragmento.
    /// </summary>
    public class SombraDelPortador : LlamadoDeJefe
    {
        protected override int NpcConvocado => ModContent.NPCType<EchoBlade>();
    }

    /// <summary>
    /// El Nombre de Aethon — convoca AETHON, LA LUZ PRIMORDIAL.
    /// "Ve al Sagrario Hueco y llama su nombre." El llamado del final:
    /// no exige nivel 150 aquí porque el mod es de PRUEBAS — el lore
    /// queda en el tooltip y el Testigo lo cuenta.
    ///
    /// v6.50.43 — A CUALQUIER HORA: la llegada corre el tiempo hasta el
    /// próximo mediodía (de noche: la noche entera + el amanecer, en un
    /// timelapse visible) — no importa la hora del llamado.
    /// </summary>
    public class NombreDeAethon : LlamadoDeJefe
    {
        protected override int NpcConvocado => ModContent.NPCType<AethonBoss>();

        /// <summary>La luz primordial contesta a CUALQUIER hora: su llegada trae el mediodía consigo.</summary>
        protected override bool ConvocableDeNoche => true;
    }

    /// <summary>
    /// v6.50.48 — EL NOMBRE DEL SEGUNDO AETHON — EL INVOCADOR NUMERO 2
    /// (la letra del usuario: «ahora crea un nuevo jefe Aethon con un
    /// nuevo invocador, que sera el invocador numero 2, este jefe
    /// tambien es una luz, pero dale la entrada exacta que tiene la
    /// emperatriz de la luz»).
    ///
    /// LA ENTRADA ES LA DE LA EMPERATRIZ, LITERAL DEL DECOMPILE (case
    /// 661 — la muerte de la luciérnaga prisma):
    ///   Vector2 pos = Center + (0, -200) + NextVector2Circular(50, 50);
    ///   SpawnBoss(x, y, 636, target);
    /// La segunda luz nace 200 px ENCIMA del portador con el mismo
    /// jitter circular de 50 y el mismo SpawnBoss de vanilla (que trae
    /// el «ha despertado», el target fijado y el timeLeft x20).
    /// </summary>
    public class NombreDeAethonSegundo : LlamadoDeJefe
    {
        protected override int NpcConvocado => ModContent.NPCType<AethonSegundo>();

        /// <summary>La segunda luz tampoco consulta el reloj: contesta a CUALQUIER hora.</summary>
        protected override bool ConvocableDeNoche => true;

        /// <summary>
        /// EL USO: la entrada EXACTA de la Emperatriz (el override total
        /// del UseItem de la casa: ni el offset lateral del común ni su
        /// NewNPC — el SpawnBoss de vanilla con la fórmula de la
        /// luciérnaga). El server manda (MP: el servidor convoca).
        /// </summary>
        public override bool? UseItem(Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return null; // el servidor manda

            // LA FÓRMULA DE LA LUCIÉRNAGA (case 661, palabra por palabra):
            // 200 px encima + jitter circular de 50 + SpawnBoss.
            Vector2 pos = player.Center + new Vector2(0f, -200f) + Main.rand.NextVector2Circular(50f, 50f);
            NPC.SpawnBoss((int)pos.X, (int)pos.Y, NpcConvocado, player.whoAmI);
            return true;
        }
    }
}
