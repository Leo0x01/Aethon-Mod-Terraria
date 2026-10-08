using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using AethonMod.Content.Systems;

namespace AethonMod.Content.Items
{
    /// <summary>
    /// EL RELOJ DE ARENA DEL ESCRIBA (ÍTEM DE PRUEBA) — v6.50.75.
    ///
    /// La letra del usuario: «deberías incluir algo para cambiar el día y
    /// la noche, un clic y es de día, otro clic y es de noche».
    ///
    /// · UN CLIC: si es de día, CAE LA NOCHE (7:30 PM); si es de noche,
    ///   AMANECE (4:30 AM) — la hora cero de cada mitad del reloj de
    ///   Terraria (Main.time = 0 con Main.dayTime girado).
    ///
    /// El reloj del escriba: la arena es TINTA — con ella escribió el
    /// alba y el ocaso del mundo, y girarla reescribe la página del
    /// cielo. Reutilizable (el patrón de la Carnada: pila 1, nunca se
    /// consume).
    ///
    /// FUNCIONA A MEDIA OLEADA: el cielo del festín es dinámico (el
    /// reloj manda — AmbienteOleadaSistema cruza suave hacia el nuevo
    /// ambiente, como en un atardecer real; los jefes prestados NO se
    /// enteran: el préstamo ambiental de OleadaNPC viste el mundo SOLO
    /// durante el AI de cada guardián y lo devuelve intacto).
    ///
    /// MP: el reloj es del MUNDO — el uso sincronizado corre este
    /// UseItem en el server (la autoridad), que gira la hora y hace
    /// BROADCAST a todos (EcoRed.MsgCambiarHorario). SP: giro directo.
    /// </summary>
    public class RelojDeArenaDelEscriba : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.maxStack = 1;
            Item.consumable = false;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item4;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(0, 0, 0, 0);
        }

        public override bool? UseItem(Player player)
        {
            // EL DESTINO DEL GIRO (calculado ANTES de tocar nada: en el
            // cliente remoto el cielo aún no gira — el server lo girará
            // por el uso sincronizado y el paquete traerá este mismo
            // destino).
            bool haciaElDia = !Main.dayTime;

            // === LA AUTORIDAD DEL RELOJ (SP y server — el uso
            // sincronizado del remoto corre AQUÍ en el server) ===
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Main.dayTime = haciaElDia;
                Main.time = 0.0; // 4:30 AM si amanece · 7:30 PM si anochece

                // MP: el giro es del mundo — viaja a TODOS los clientes.
                if (Main.netMode == NetmodeID.Server)
                    EcoRed.DifundirGiroReloj(haciaElDia, 0.0, (byte)player.whoAmI);
            }

            // EL AVISO en las máquinas con pantalla propia: SP directo;
            // el HOST (listen server) también lo ve aquí (mismo proceso
            // — su propio paquete no le llega por red). Los clientes
            // remotos lo reciben por EcoRed.MsgCambiarHorario.
            if (Main.netMode == NetmodeID.SinglePlayer ||
                (Main.netMode == NetmodeID.Server && !Main.dedServ))
                GiroLocal(haciaElDia, (byte)player.whoAmI);

            return true;
        }

        /// <summary>
        /// Lo que siente CADA máquina al girar el reloj: el aviso del
        /// escriba y la lluvia de tinta alrededor de quien lo giró.
        /// (En SP la llama el propio uso; en MP, la recepción del
        /// broadcast — una sola voz por máquina, sin ecos dobles.)
        /// </summary>
        public static void GiroLocal(bool dia, byte quien)
        {
            // EL AVISO — la tinta reescribe la página del cielo.
            Main.NewText(Language.GetTextValue(dia
                    ? "Mods.AethonMod.RelojDeArena.AlAmanecer"
                    : "Mods.AethonMod.RelojDeArena.AlAnochecer"),
                dia ? new Color(255, 214, 130) : new Color(196, 150, 255));

            // LA LLUVIA DE TINTA alrededor del que giró el reloj:
            // al amanecer, dorada (GoldFlame — el oro del alba de la
            // casa); al anochecer, sombra violeta (Shadowflame — la
            // misma tinta del Códice Vivo).
            Player autor = quien < Main.maxPlayers ? Main.player[quien] : null;
            if (autor != null && autor.active)
            {
                Vector2 c = autor.Center;
                int tipo = dia ? DustID.GoldFlame : DustID.Shadowflame;
                Color tinte = dia ? new Color(255, 214, 130) : new Color(196, 150, 255);
                for (int d = 0; d < 36; d++)
                {
                    Vector2 vel = new Vector2(Main.rand.NextFloat(-5.5f, 5.5f),
                                              Main.rand.NextFloat(-7.5f, 2.5f));
                    Dust.NewDustPerfect(c, tipo, vel, 100, tinte,
                        Main.rand.NextFloat(1.0f, 1.7f));
                }
            }
        }
    }
}
