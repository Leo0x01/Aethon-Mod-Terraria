using Terraria;
using Terraria.ModLoader;
using Terraria.GameContent;
using Terraria.ID;
using Microsoft.Xna.Framework;
using System;

namespace AethonMod.Content.Buffs
{
    /// <summary>
    /// IraSerExteriorRecargaBuff — v6.145 — EL CONTADOR TIPO BUFF de la
    /// Ira de un Ser Exterior (la letra: «debe estar solo disponible
    /// cada 10 minutos con un contador tipo buff para el haz»): mientras
    /// el reloj de los 10 minutos corre, el portador lleva ESTE buff —
    /// su descripción ES el contador vivo (mm:ss hasta que la Ira vuelva
    /// a estar lista). Cuando el reloj llega a cero, el buff se retira.
    /// </summary>
    public class IraSerExteriorRecargaBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;          // no es castigo: es un reloj
            Main.buffNoTimeDisplay[Type] = true; // el tiempo lo dibuja la desc
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip,
            ref int rare)
        {
            try
            {
                // v6.149 — la letra: «la informacion en el buff de la ira
                // del ser exterior dice que use la combinacion de teclas,
                // eso no deberia decirlo, solo debe decir algo como, ataque
                // de ira masivo que barre con todo». SIN teclas — el
                // disparador (izq+der) se descubre jugando.
                tip = "Ataque de ira masivo que barre con todo.";
            }
            catch { }
        }

        // ================================================================
        //  v6.149 — EL CONTADOR DEBAJO DEL ICONO (la letra: «mostrar un
        //  pequeño contador justo debajo del icono del buff de la ira del
        //  ser exterior»). El reloj de los 10 minutos se dibuja como TEXTO
        //  VIVO pegado a nuestro icono en la rejilla de buffs de la esquina
        //  superior izquierda (la MISMA rejilla de vanilla — medimos la
        //  celda del icono y escribimos el reloj mm:ss justo debajo).
        //  Lo llama el postfix de DrawPlayerHealth (el HUD, cada frame).
        // ================================================================
        public static void DibujarContador()
        {
            try
            {
                Player p = Main.LocalPlayer;
                if (p == null || !p.active || Main.gameMenu ||
                    Main.netMode == NetmodeID.Server) return;
                int tipo = ModContent.BuffType<IraSerExteriorRecargaBuff>();
                int fila = -1, col = -1;
                for (int i = 0; i < 22; i++)          // vanilla dibuja máx 22 buffs
                {
                    if (p.buffType[i] == tipo) { fila = i / 11; col = i % 11; break; }
                }
                if (col < 0) return;

                ulong resta = Systems.AtaquesCombinadosSistema.IraRestante(p);
                if (resta <= 0ul) return;             // listo: ya no hay reloj

                int s = (int)(resta / 60ul);
                string texto = $"{s / 60:D2}:{s % 60:D2}";

                // LA REJILLA DE VANILLA (decompile de Main.DrawBuffs): la
                // esquina de la lista vive en (50, 28)·escala, cada celda
                // mide 48×46 (icono 32 + aire), 11 por fila.
                float esc = Main.inventoryScale;
                float x = (50f + col * 48f + 16f) * esc;
                float y = (28f + fila * 46f + 42f) * esc;   // 32 del icono + 10

                var fuente = Terraria.GameContent.FontAssets.MouseText.Value;
                var dim = fuente.MeasureString(texto);
                Vector2 pos = new Vector2(x - dim.X * 0.5f * 0.85f, y);

                // la sombra oscura + el texto dorado vivo (el reloj del Eterno)
                Utils.DrawBorderString(Main.spriteBatch, texto, pos,
                    new Color(255, 224, 150), 0.85f, 0f, 0f);
            }
            catch { }
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // el buff vive mientras el reloj corra (lo administra el
            // propio sistema en su tick)
        }
    }
}
