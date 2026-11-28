using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items
{
    // ======================================================================
    //  EstrellaFugazConsagrada — v6.185 — LA ESTRELLA FUGAZ CONSAGRADA (la
    //  letra del ataque básico del grimorio: «para desbloquear el nivel del
    //  despertar que mejora el proyectil base del grimorio los requisitos
    //  de ese nivel de despertar deben incluir cierta cantidad de Estrellas
    //  Fugaces, estas estrellas se pueden comprar a El Cronista por 10
    //  esencias del eterno cada estrella ademas la cantidad de estrellas
    //  fugaces que seran requerida iran de 1 a 1000, siendo 1000 las
    //  necesarias para el nivel 120 del despertar, el grimorio solo comera
    //  las estrellas fugaces consagradas, y estas estrellas consagradas no
    //  desaparecen por el dia y su sprite sera el mismo que el de las
    //  estrellas fugaces de terraria normal, solo que con una capa de
    //  brillo solar por encima»).
    //
    //  · SE COMPRA en El Cronista (ElCronista.ModifyActiveShop: 10
    //    esencias del Eterno — la moneda de la casa).
    //  · ES UNA OFRENDA, NO UN CONSUMIBLE: no se usa — se ARROJA al suelo
    //    delante del grimorio y el libro se come SOLO LAS QUE FALTAN del
    //    peldaño vivo (DespertarGrimorioSistema.ServirLaMesa — la regla de
    //    los 30 de 50). El Eterno NUNCA prueba la estrella fugaz de
    //    vanilla (FallenStar, 75): solo ESTA.
    //  · NO DESAPARECE POR EL DÍA: vanilla mata al FallenStar al alba por
    //    TIPO (Item.UpdateItem: type == 75) — esta es OTRO tipo de item y
    //    nadie la toca. Nace consagrada: no pasa por el altar.
    //  · EL SPRITE: la estrella fugaz de Terraria con la capa de brillo
    //    solar (tools/t7_sprite_estrella_v6185.py).
    // ======================================================================
    public class EstrellaFugazConsagrada : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 9999;
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
        }

        // LA TINTA SOLAR en el suelo: la estrella brilla de noche como las
        // de vanilla (la luz de la estrella fugaz) — v6.185.
        public override void PostUpdate()
        {
            if (Main.netMode == NetmodeID.Server) return;
            Lighting.AddLight(Item.position, 0.55f, 0.42f, 0.14f);
        }
    }
}
