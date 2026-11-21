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
            if (Main.netMode == NetmodeID.Server) return false;

            // ============================================================
            //  v6.159 — LA COPIA VIVA DEL JUGADOR, DE VERDAD (la letra:
            //  «el NPC test no copia al jugador» + «es invisible, solo
            //  lo encontré por las partículas del aura del vacío»).
            //  LA AUTOPSIA de las dos vías muertas:
            //   · LA v6.156 dibujaba AL JUGADOR LOCAL MISMO con
            //     shadow=0.65 — una SILUETA OSCURECIDA (el param de
            //     sombra multiplica el color a casi negro): contra un
            //     fondo de cueva o de noche, el cuerpo SE PERDÍA — por
            //     eso solo las partículas del aura lo delataban. Y PEOR:
            //     dibujar al jugador vivo con Main.Camera en la posición
            //     del NPC no es «una copia»: es un doble parásito.
            //   · LA v6.158 del Cronista usó `new Player()` forjado —
            //     sin los caches de dibujado que el renderer espera de
            //     un Player REAL.
            //  EL PATRÓN DE VANILLA (UICharacter — la preview del menú,
            //  el decompile): Player.SerializedClone() + LA CESTA
            //  `using Main.CurrentPlayerOverride(copia)` para que
            //  PlayerFrame() y los hooks corran SOBRE EL CLON + el
            //  DrawPlayer oficial. EL CLON ES EL JUGADOR DE VERDAD: su
            //  cuerpo, su ropa, sus ojos, su pelo — SIN el oscurecido
            //     que lo mataba (visible SIEMPRE), animado con EL CICLO
            //  DE ANDAR de la preview (la banda 6..19 de vanilla, el
            //  mismo de la fila del menú).
            // ============================================================
            try
            {
                Player copia = _clon ??= Main.LocalPlayer.SerializedClone();
                if (copia != null && Main.PlayerRenderer != null)
                {
                    copia.dead = false;
                    copia.direction = NPC.direction;
                    copia.gravDir = 1f;
                    copia.fullRotation = 0f;
                    copia.wingFrame = 0;
                    copia.itemAnimation = 0;

                    // EL ANDAR DE LA PREVIEW (UICharacter.UpdateAnim):
                    // parado = frame 0 · en marcha = la banda 6..19 del
                    // ciclo de andar de vanilla (la misma del menú).
                    float vel = Math.Abs(NPC.velocity.X);
                    if (vel > 0.3f)
                    {
                        int f = (int)(Main.GlobalTimeWrappedHourly / 0.07f) % 14 + 6;
                        copia.bodyFrame.Y = copia.legFrame.Y = copia.headFrame.Y = f * 56;
                    }
                    else
                    {
                        copia.bodyFrame.Y = copia.legFrame.Y = copia.headFrame.Y = 0;
                    }

                    // v6.161 — LA AUTOPSIA DEL INVISIBLE (la letra: «el
                    // NPC test sigue invisible, no lo veo»): la firma
                    // de DrawPlayer es (camera, player, position,
                    // rotation, rotationOrigin, SHADOW, SCALE) — y la
                    // v6.159 pasaba la ILUMINACIÓN (0.15..1) en el
                    // parámetro ESCALA: de noche o en cueva el clon se
                    // dibujaba al 15% de TAMAÑO (≈8 píxeles de cuerpo
                    // — invisible). LA CURA: escala 1 SIEMPRE (el
                    // cuerpo de verdad, tamaño de jugador). La
                    // iluminación NO se toca aquí: el renderer lee LA
                    // LUZ DEL MUNDO por sí mismo (como a cualquier
                    // jugador — el patrón de vanilla).
                    Vector2 pos = NPC.position - screenPos;
                    using (new Main.CurrentPlayerOverride(copia))
                    {
                        Main.PlayerRenderer.DrawPlayer(Main.Camera, copia, pos,
                            0f, Vector2.Zero, 0f, 1f);

                        // v6.161 — EL VELO DE SOMBRA (la letra original:
                        // «estara oscurecido con un shader de sombra»):
                        // el MISMO clon re-dibujado con shadow = 0.58 —
                        // el parámetro DE SOMBRA del renderer de vanilla
                        // (multiplica el color del cuerpo: la silueta
                        // oscurecida de la letra, SIN tocar tintes a
                        // mano y SIN matar la visibilidad).
                        Main.PlayerRenderer.DrawPlayer(Main.Camera, copia, pos,
                            0f, Vector2.Zero, 0.58f, 1f);
                    }
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

        /// <summary>EL CLON DEL JUGADOR LOCAL — nace UNA vez (la copia
        /// de la preview del menú) y se anima en cada dibujado.</summary>
        private static Player _clon;
    }
}
