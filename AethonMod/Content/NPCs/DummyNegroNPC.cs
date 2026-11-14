using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.NPCs
{
    // ======================================================================
    //  DummyNegroNPC — v6.105 — EL MUÑECO DE PRUEBAS NEGRO DEL GRIMORIO.
    //
    //  LA LETRA DEL USUARIO: «ademas revisa si tocaste el codigo de los
    //  dummy en el pasado, pueden ser en commits muy viejos, recuerdo
    //  que lo tocamos, en caso de haberlos tocado intenta ponerlos
    //  justo como deben estar en el terraria normal, y crea un nuevo
    //  dummy con el mismo sprite que los de terraria normal pero
    //  teñido de negro y que estos dummy negros si puedan recibir
    //  todos los ataques del grimorio».
    //
    //  LA AUTOPSIA (los commits viejos): la v6.30 metió al Target
    //  Dummy en VFXCore.EsObjetivo a petición de la letra de aquella
    //  era («haz que todas las armas puedan dañar a los Dummy») — HOY
    //  la letra devuelve los dummies normales a TERRARIA NORMAL
    //  (EsObjetivo y BuscarPresa vuelven a CanBeChasedBy: el dummy
    //  NO es objetivo, como en el juego base).
    //
    //  Y NACE EL DUMMY NEGRO — EL BANCO DE PRUEBAS DEL GRIMORIO:
    //   · EL SPRITE DE LA CASA (v6.136 — la letra: «Genera las
    //     texturas con SForja»): el muñeco de paja NEGRO-VIOLETA nació
    //     en LA FORJA — cero texturas del juego base (la regla de la
    //     casa: todo el arte es 100% nuestro).
    //   · RECIBE TODOS LOS ATAQUES DEL GRIMORIO:
    //     NPCID.Sets.CanBeChasedBy[Type] = true — LA MISMA PUERTA que
    //     excluye a los dummies normales lo INCLUYE a él: las sombras
    //     lo CAZAN (BuscarPresa), los homing lo persiguen
    //     (EsObjetivo), las auras lo marcan, el tentáculo lo MUERDE y
    //     DRENA.
    //   · COMO EL DUMMY VANILLA: immortal (jamás muere — el banco de
    //     pruebas es eterno), muestra los números de daño, no se
    //     mueve, no ataca, no cuenta en el radar de enemigos.
    //   · LA VIDA ETERNA DEL BANCO: al 25% de vida se RESTAURA entera
    //     (con su puff violeta) — la devoración del festín JAMÁS se
    //     dispara sobre la herramienta: el dren de las sombras baja su
    //     vida… y el muñeco renace para la siguiente prueba.
    //   · EL BAMBOLEO: al recibir un golpe se inclina y vuelve — el
    //     gesto vivo del muñeco de entrenamiento.
    // ======================================================================
    public class DummyNegroNPC : ModNPC
    {
        /// <summary>EL SPRITE DE LA CASA — el muñeco negro de la
        /// forja (DummyNegroNPC.png junto a esta clase).</summary>

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;   // quieto: UN solo marco (el de la forja)
            // EL BESTIARIO NO (no es un monstruo — es una herramienta de
            // pruebas: como el dummy vanilla, no aparece en las cifras).
            NPCID.Sets.DontDoHardmodeScaling[Type] = true;
            // (LA AUTOPSIA DE LA v6.105 contra el tML 2026.09.3.1 REAL:
            // NPC.CanBeChasedBy() — 58 bytes de IL descompilados — lee el
            // campo de instancia NPC.chaseable Y EXCLUYE a los immortal:
            // chaseable=false + immortal=true es por lo que el Target
            // Dummy de vanilla JAMÁS es perseguido. NO EXISTE ningún
            // NPCID.Sets.CanBeChasedBy — el campo de instancia ES la
            // puerta. La llave del dummy negro vive en SetDefaults.)
        }

        public override void SetDefaults()
        {
            // COMO EL DUMMY VANILLA: quieto, neutral, eterno.
            NPC.width = 20;
            NPC.height = 44;
            NPC.damage = 0;
            NPC.defense = 0;            // números limpios: el daño tal cual
            NPC.lifeMax = 5000;         // banco de pruebas generoso
            NPC.life = 5000;
            // ==============================================================
            //  LA LLAVE DE LA LETRA — «que estos dummy negros si puedan
            //  recibir todos los ataques del grimorio»: NPC.chaseable =
            //  true (el campo que lee CanBeChasedBy — medido contra el
            //  IL del tML real). POR ESO el immortal MUERE aquí: el
            //  chequeo del motor EXCLUYE a los immortal… y este muñeco
            //  necesita ser perseguido. La eternidad la garantiza
            //  CheckDead (abajo): NUNCA muere de verdad.
            //  TODO el grimorio lo ve: las sombras lo cazan (BuscarPresa),
            //  los homing lo persiguen (EsObjetivo), las auras lo marcan.
            // ==============================================================
            NPC.chaseable = true;
            NPC.immortal = false;
            NPC.dontTakeDamage = false; // SÍ muestra los números (y SÍ recibe)
            NPC.friendly = false;       // NEUTRAL (como el vanilla: los proyectiles le pegan)
            NPC.townNPC = false;
            NPC.npcSlots = 0f;          // no ocupa cupo de spawn
            NPC.dontCountMe = true;     // no cuenta en el radar de enemigos
            NPC.knockBackResist = 0f;   // clavado: el muñeco no se deja mover
            NPC.aiStyle = -1;           // sin IA vanilla (yo me encargo)
            NPC.netAlways = true;       // que todos lo vean igual
            NPC.HitSound = SoundID.NPCHit1;
        }

        public override bool CheckDead()
        {
            // ==============================================================
            //  LA ETERNIDAD DEL BANCO — el golpe MÁS masivo del grimorio
            //  puede tumbarlo… y el muñeco RENACE: la vida vuelve al
            //  pleno (con su puff violeta) y CheckDead devuelve FALSE —
            //  el motor NO lo mata (sin gore, sin drops, sin nada: el
            //  banco de pruebas es ETERNO, como el dummy vanilla).
            // ==============================================================
            NPC.life = NPC.lifeMax;
            NPC.netUpdate = true;
            if (Main.netMode != NetmodeID.Server && !Main.dedServ && !Main.gameMenu)
            {
                for (int i = 0; i < 16; i++)
                {
                    Dust d = Dust.NewDustPerfect(
                        NPC.Center + new Vector2(
                            Main.rand.NextFloat(-NPC.width, NPC.width) * 0.8f,
                            Main.rand.NextFloat(-NPC.height, NPC.height) * 0.7f),
                        Terraria.ModLoader.ModContent.DustType
                            <global::AethonMod.Content.Dusts.PolvoVacioInvertido>(),
                        new Vector2(Main.rand.NextFloat(-0.8f, 0.8f),
                            -Main.rand.NextFloat(0.5f, 1.5f)),
                        0, new Color(224, 176, 255), Main.rand.NextFloat(0.6f, 1.1f));
                    d.noGravity = true;
                }
            }
            return false;   // NUNCA muere de verdad
        }

        public override void AI()
        {
            // EL BAMBOLEO decae (nace en HitEffect: 24 → 0).
            if (NPC.ai[0] > 0f)
                NPC.ai[0]--;

            // LA VIDA DEL BANCO — al 25% se RESTAURA entera (con su puff
            // violeta): el dren directo de las sombras jamás verá la vida
            // llegar a 1, el usuario prueba TODO el arsenal una y otra
            // vez, y la devoración jamás se le dispara (además del guard
            // de FaucesGlobalNPC: la herramienta NO es comida).
            if (NPC.life < NPC.lifeMax / 4)
            {
                NPC.life = NPC.lifeMax;
                NPC.netUpdate = true;
                if (Main.netMode != NetmodeID.Server && !Main.dedServ && !Main.gameMenu)
                {
                    for (int i = 0; i < 12; i++)
                    {
                        Dust d = Dust.NewDustPerfect(
                            NPC.Center + new Vector2(
                                Main.rand.NextFloat(-NPC.width, NPC.width) * 0.7f,
                                Main.rand.NextFloat(-NPC.height, NPC.height) * 0.6f),
                            Terraria.ModLoader.ModContent.DustType
                                <global::AethonMod.Content.Dusts.PolvoVacioInvertido>(),
                            new Vector2(Main.rand.NextFloat(-0.6f, 0.6f),
                                -Main.rand.NextFloat(0.4f, 1.2f)),
                            0, new Color(150, 84, 235), Main.rand.NextFloat(0.5f, 0.9f));
                        d.noGravity = true;
                    }
                }
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            // EL BAMBOLEO NACE — el muñeco se inclina hacia el golpe y
            // vuelve (la dirección: de dónde vino el empujón).
            NPC.ai[0] = 24f;
            NPC.ai[1] = hit.Knockback >= 0f ? 1f : -1f;
            if (hit.Damage > 0 && Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                int polvos = Math.Min(10, 2 + (int)(hit.Damage / 40f));
                for (int i = 0; i < polvos; i++)
                {
                    Dust d = Dust.NewDustPerfect(
                        NPC.Center + new Vector2(
                            Main.rand.NextFloat(-NPC.width, NPC.width) * 0.6f,
                            Main.rand.NextFloat(-NPC.height, NPC.height) * 0.5f),
                        Terraria.ModLoader.ModContent.DustType
                            <global::AethonMod.Content.Dusts.PolvoVacioInvertido>(),
                        new Vector2(Main.rand.NextFloat(-1.2f, 1.2f),
                            -Main.rand.NextFloat(0.5f, 1.6f)),
                        0, new Color(224, 176, 255), Main.rand.NextFloat(0.4f, 0.8f));
                    d.noGravity = true;
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // ==============================================================
            //  EL MISMO SPRITE DE TERRARIA NORMAL, TEÑIDO DE NEGRO — el
            //  frame 0 del Target Dummy vanilla dibujado a mano con el
            //  negro-violeta de la casa (morado y negro), con su
            //  resplandor morado detrás (la lectura nocturna) y el
            //  BAMBOLEO del golpe (se inclina desde la base y vuelve).
            // ==============================================================
            try
            {
                Texture2D tex = TextureAssets.Npc[Type].Value;
                if (tex == null || tex.IsDisposed)
                    return false;

                // EL SPRITE DE LA CASA — un solo marco: el muñeco
                // negro de la forja (ya nació negro-violeta).
                var marco = new Rectangle(0, 0, tex.Width, tex.Height);

                // EL BAMBOLEO — la inclinación que nace al golpe y muere
                // sola (oscilación amortiguada desde la base).
                float balanceo = 0f;
                if (NPC.ai[0] > 0f)
                {
                    float k = NPC.ai[0] / 24f;
                    balanceo = (float)Math.Sin((1f - k) * Math.PI * 1.6f) * 0.17f * k
                        * (NPC.ai[1] < 0f ? -1f : 1f);
                }

                // LA POSICIÓN — la base del muñeco clavada al suelo.
                Vector2 pos = NPC.Bottom - screenPos;
                Vector2 origen = new Vector2(marco.Width * 0.5f, marco.Height);

                // (1) EL RESPLANDOR MORADO — la lectura nocturna del
                //     negro de la casa (un halo suave bajo el lote alfa
                //     del propio NPC).
                Texture2D glow = VFXCore.SoftGlow;
                if (glow != null && !glow.IsDisposed)
                {
                    spriteBatch.Draw(glow, pos - new Vector2(0f, marco.Height * 0.5f),
                        null, new Color(66, 32, 108) * 0.42f, 0f,
                        glow.Size() * 0.5f, new Vector2(marco.Width * 2.6f / glow.Width,
                            marco.Height * 2.2f / glow.Height), SpriteEffects.None, 0f);
                }

                // (2) EL MUÑECO — el sprite de la casa tal cual (ya
                //     nació negro-violeta en la forja), bañado por la
                //     luz del mundo (drawColor).
                spriteBatch.Draw(tex, pos, marco, drawColor, balanceo, origen,
                    1f, SpriteEffects.None, 0f);

                // (3) LA VENA VIOLETA — un segundo pase ADITIVO muy
                //     tenue con el mismo sprite: las luces del original
                //     (los ojos del muñeco, los bordes) PRENDEN en
                //     violeta sobre el negro — el guiño del grimorio.
                try
                {
                    VFXCore.CerrarLoteSiAbierto();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None,
                        RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    Main.spriteBatch.Draw(tex, pos, marco,
                        new Color(120, 70, 190) * 0.20f, balanceo, origen,
                        1f, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                }
                catch
                {
                    try { VFXCore.CerrarLoteSiAbierto(); } catch { }
                }
            }
            catch
            {
                // el dibujo JAMÁS tumba el frame — si algo falla, el
                // sprite crudo sale por el return true.
                return true;
            }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
