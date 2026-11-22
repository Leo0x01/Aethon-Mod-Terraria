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
using System;
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
        // v6.165 — LA TIRA PROPIA (la letra: «todos los mods usan el
        // spritesheet de Terraria para sus NPCs»): hoja de 25 marcos
        // layout vanilla (idle/parpadeo/caminar/uso), túnica gris de
        // espécimen — el framer de vanilla hace el resto.
        public override string Texture => "AethonMod/Content/NPCs/NPCTest";

        public override void SetStaticDefaults()
        {
            // EL LAYOUT VANILLA DE LA CASA (Guide/AlchemistNPC): 25
            // marcos + los sets del framer de aldeano.
            Main.npcFrameCount[Type] = 25;
            NPCID.Sets.ExtraFramesCount[Type] = 5;
            NPCID.Sets.AttackFrameCount[Type] = 4;
            NPCID.Sets.DangerDetectRange[Type] = 500;
            NPCID.Sets.AttackType[Type] = 0;
            NPCID.Sets.AttackTime[Type] = 90;
            NPCID.Sets.AttackAverageChance[Type] = 30;
            NPCID.Sets.NPCFramingGroup[Type] = 0;
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
            AnimationType = NPCID.Guide;   // el framer vanilla de aldeano
        }

        public override bool CanTownNPCSpawn(int numTownNPCs)
            => false;   // v6.157: NACE JUNTO AL CRONISTA (el spawner de
                        // CronistaSistema lo invoca — la letra: «el NPC
                        // test debe aparecer junto con el cronista»);
                        // el camino vanilla de casas NUNCA lo trae (por
                        // eso «no lo veo» — sin casa no hay town NPC)

        public override string GetChat()
            => "Soy un test";

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = "";   // no tiene nada que vender
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // v6.165 — EL SPRITE VANILLA (la letra: «todos los mods
            // usan el spritesheet de Terraria para sus NPCs»): el
            // clon del jugador MURIÓ — el NPC de prueba es UN NPC de
            // TERRARIA con tira propia de 25 marcos (NPCTest.png) y
            // el framer de vanilla lo anima (AnimationType Guide).
            // La bruma de la casa a sus pies queda (el suspiro
            // violeta de siempre).
            if (!Main.dedServ && (Main.GameUpdateCount + NPC.whoAmI) % 14f == 0f)
            {
                try
                {
                    for (int k = 0; k < 2; k++)
                    {
                        Vector2 cuna = NPC.BottomLeft + new Vector2(
                            Main.rand.NextFloat(NPC.width), 0f);
                        Dust d = Dust.NewDustPerfect(cuna,
                            ModContent.DustType<global::AethonMod.Content.Dusts.PolvoVacioInvertido>(),
                            new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), -0.15f),
                            0, new Color(110, 60, 180), Main.rand.NextFloat(0.15f, 0.25f));
                        d.noGravity = true;
                    }
                }
                catch { }
            }
            return true;    // LA TIRA PROPIA — vanilla dibuja y anima
        }

    }
}
