using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using AethonMod.Content.NPCs;

namespace AethonMod.Content.Items.Llamados
{
    // ======================================================================
    //  v6.47 — LOS LLAMADOS: los invocadores de los JEFES DEL MOD.
    //
    //  v6.50.61 — LA PURGA DE NPC (la letra del usuario: «borra a todos
    //  los NPC del mod, solo deja al jefe principal, Aethon, borra otros
    //  jefes y al testigo»): los CUATRO llamados de los guardianes
    //  murieron con sus jefes (Cristal del Titán Hueco, Sello del Rift,
    //  Pluma de la Arquera, Sombra del Portador). SOLO queda EL NOMBRE
    //  DE AETHON — el llamado del FINAL.
    //
    //  Contrato de la casa para todos: reutilizables (no consumibles —
    //  el mod entero es de pruebas), rugido al nacer, aviso de "ya vive
    //  uno" si intentas doblar, y el mensaje localizado de noche
    //  (AETHON lo rompe: su llegada no conoce hora).
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
    /// El Nombre de Aethon — convoca AETHON, EL GRIMORIO ETERNO.
    /// "El libro que cargas tiene un nombre propio: pronúncialo y se
    /// abrirá para probarte." El llamado del final: no exige nivel 150
    /// aquí porque el mod es de PRUEBAS — el lore queda en el tooltip.
    ///
    /// v6.50.43 — A CUALQUIER HORA: la llegada corre el tiempo hasta el
    /// próximo mediodía (de noche: la noche entera + el amanecer, en un
    /// timelapse visible) — no importa la hora del llamado.
    /// v6.50.49 — LA ENTRADA DE LA EMPERATRIZ, EXACTA (la letra: «para
    /// la entrada del Aethon original, modifiquemosla y que sea
    /// exactamente como la emperatris de la luz, para ello revisa como
    /// terraria maneja su entrada y copiala»): LA CARRERA AL MEDIODÍA
    /// MURIÓ (con su temblor, su pilar y su descenso). Ahora es
    /// LITERAL el case 661 del decompile (la muerte de la luciérnaga
    /// prisma): nace 200 px ENCIMA del portador + jitter circular de 50
    /// + NPC.SpawnBoss (que trae el «ha despertado», el target fijado y
    /// el timeLeft ×20) — y una presentación breve flotando ahí
    /// (EST_NACIENDO con SUB_PRESENTA) antes de la pelea. La hora da
    /// igual: la Emperatriz nace de noche y su cielo no se toca.
    /// </summary>
    public class NombreDeAethon : LlamadoDeJefe
    {
        protected override int NpcConvocado => ModContent.NPCType<AethonBoss>();

        /// <summary>La luz primordial contesta a CUALQUIER hora (como la Emperatriz: el reloj no la toca).</summary>
        protected override bool ConvocableDeNoche => true;

        /// <summary>
        /// EL USO: la entrada EXACTA de la Emperatriz (LITERAL del
        /// decompile, case 661 — la muerte de la luciérnaga prisma,
        /// palabra por palabra):
        ///   Vector2 pos = Center + (0, -200) + NextVector2Circular(50, 50);
        ///   SpawnBoss(x, y, 636, target);
        /// Ni el offset lateral del común ni su NewNPC: el SpawnBoss de
        /// vanilla con la fórmula de la luciérnaga. El server manda
        /// (MP: el servidor convoca).
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
