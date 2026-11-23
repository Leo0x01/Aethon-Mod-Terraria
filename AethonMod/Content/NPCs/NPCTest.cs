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
            //  v6.168 — LA COPIA EXACTA, VISIBLE SIEMPRE (la letra:
            //  «el NPC test no se parece en nada al jugador […] el NPC
            //  test esta invisible y no se ve»). LA AUTOPSIA de la
            //  v6.159: el clon nacía con SerializedClone() PERO SIN
            //  `active = true` NI `whoAmI` — DrawPlayerInternal pasa
            //  al Player por BoringSetup y las capas del renderer solo
            //  visten a un Player ACTIVO con slot válido: el clon caía
            //  en estados vacíos, el catch MUDO se lo tragaba y el NPC
            //  quedaba INVISIBLE (solo la bruma lo delataba).
            //  LA CURA — EL PATRÓN UICharacter COMPLETO (la preview
            //  del menú de vanilla, verificada contra el tML real):
            //   1. SerializedClone() del jugador local (la copia
            //      EXACTA: cuerpo, pelo, ojos, ropa, accesorios).
            //   2. active = true · whoAmI = 0 (un slot válido) ·
            //      InventorCreator = false (no es la preview del menú).
            //   3. Frames manejados a mano (la banda de andar 6..19 de
            //      la preview — parado: frame 0).
            //   4. CurrentPlayerOverride(copia) + DrawPlayer OFICIAL
            //      con la LUZ DEL MUNDO (no silueta ciega).
            //  SIN catch mudo: si el renderer falla, SE VE el fantasma
            //  (la cabeza del Cronista — la textura fantasma de
            //  siempre): jamás invisible.
            // ============================================================
            bool dibujado = false;
            try
            {
                Player copia = _clon ??= Main.LocalPlayer.SerializedClone();
                if (copia != null && Main.PlayerRenderer != null)
                {
                    // EL CLON ACTIVO — la clave que faltaba: sin
                    // active=true el renderer lo viste de NADA.
                    copia.active = true;
                    copia.whoAmI = 0;               // un slot válido
                    copia.dead = false;
                    copia.direction = NPC.direction;
                    copia.gravDir = 1f;
                    copia.fullRotation = 0f;
                    copia.wingFrame = 0;
                    copia.itemAnimation = 0;
                    copia.velocity = new Vector2(NPC.velocity.X, 0f);

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

                    // LA ILUMINACIÓN DEL MUNDO sobre la copia (como
                    // cualquier jugador — no una silueta ciega).
                    Vector3 luz = Lighting.GetSubLight(NPC.Center);
                    float lum = MathHelper.Clamp((luz.X + luz.Y + luz.Z) / 3f * 1.35f, 0.20f, 1f);

                    Vector2 pos = NPC.position - screenPos;
                    using (new Main.CurrentPlayerOverride(copia))
                    {
                        Main.PlayerRenderer.DrawPlayer(Main.Camera, copia, pos,
                            0f, Vector2.Zero, 0f, lum);
                    }
                    dibujado = true;
                }
            }
            catch { }

            // LA RED DE SEGURIDAD: si el renderer no dibujó, SE VE EL
            // FANTASMA (jamás invisible — la letra: «esta invisible y
            // no se ve»).
            if (!dibujado)
                return true;   // vanilla dibuja la textura fantasma

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
