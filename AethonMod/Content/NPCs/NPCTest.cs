// ======================================================================
//  NPCTest — v6.156 — EL NPC DE PRUEBA (la letra: «crea tambien un NPC
//  de prueba NPC TEST que solo diga soy un test, este NPC de prueba
//  sera una copia oscurecida del jugador, estara oscurecido con un
//  shader de sombra y un poco de bruma a sus pies»).
//
//  EL CUERPO: LA COPIA DEL JUGADOR LOCAL — dibujada por EL RENDERER DE
//  VANILLA (Main.PlayerRenderer.DrawPlayer — el mismo sistema que
//  dibuja al portador, verificado contra el decompile) con EL PARAM
//  DE SOMBRA (shadow = 0.65: el cuerpo entero OSCURECIDO por el
//  pipeline de sombras del propio renderer — el «shader de sombra» de
//  la letra, SIN tocar tintes a mano) + UN POCO DE BRUMA a sus pies
//  (el suspiro violeta de la casa, 2 motas suaves cada ~14 ticks).
//  El frame del cuerpo se COPIA del jugador vivo (su andar, su salto:
//  es UNA COPIA de verdad).
//  EL DIÁLOGO: solo «Soy un test».
// ======================================================================
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.NPCs
{
    public class NPCTest : ModNPC
    {
        public override string Texture => "AethonMod/Content/NPCs/ElCronista_Head";   // el fantasma de la casa (el cuerpo es 100% renderer)

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 20;
            NPC.height = 42;
            NPC.aiStyle = NPCAIStyleID.Passive;
            NPC.damage = 0;
            NPC.defense = 0;
            NPC.lifeMax = 250;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.4f;
        }

        public override bool CanTownNPCSpawn(int numTownNPCs)
            => true;   // ES UN TEST: siempre puede llegar

        public override string GetChat()
            => "Soy un test";

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = "";   // no tiene nada que vender
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            // LA COPIA OSCURECIDA: el renderer de vanilla dibuja AL
            // JUGADOR LOCAL (SU cuerpo, SU frame, SU ropa — una copia
            // viva) con EL PARAM DE SOMBRA 0.65 — el oscurecido del
            // propio pipeline (el «shader de sombra» de la letra).
            try
            {
                Player copia = Main.LocalPlayer;
                if (copia != null && Main.PlayerRenderer != null)
                {
                    Vector2 pos = NPC.position - screenPos;
                    Main.PlayerRenderer.DrawPlayer(Main.Camera, copia, pos,
                        0f, Vector2.Zero, 0.65f);
                }
            }
            catch { }

            // UN POCO DE BRUMA A SUS PIES: dos motas suaves cada ~14
            // ticks — el suspiro violeta de la casa (PolvoVacioInvertido
            // MENUDO, disuelto en unos pasos).
            if (!Main.dedServ && (Main.GameUpdateCount + NPC.whoAmI) % 14f == 0f)
            {
                for (int k = 0; k < 2; k++)
                {
                    Vector2 cuna = NPC.BottomLeft + new Vector2(
                        Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(-4f, 2f));
                    Dust d = Dust.NewDustPerfect(cuna,
                        ModContent.DustType<global::AethonMod.Content.Dusts.PolvoVacioInvertido>(),
                        new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -Main.rand.NextFloat(0.1f, 0.35f)),
                        0, new Color(110, 60, 180), Main.rand.NextFloat(0.20f, 0.34f));
                    d.noGravity = true;
                }
            }
            return false;
        }
    }
}
