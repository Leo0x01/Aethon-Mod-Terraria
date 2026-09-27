using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.NPCs
{
    // ======================================================================
    //  v6.50.19 — LA SIERPE DE HUESO DE LA LUZ: LOS SEGMENTOS.
    //
    //  El jefe final re-encarnado (petición del usuario: "el jefe final
    //  debe ser una sierpe gigante... debe sobresalir de la tierra y su
    //  ataque deben salir de su cabeza... esquelética con aspecto del
    //  esqueleto de una serpiente"). Investigación R59-a: el patrón del
    //  Devourer of Gods de Calamity —
    //
    //  · La CABEZA (AethonBoss, reescrito) spawnea la cadena en su primer
    //    tick: 26 VÉRTEBRAS de mundo + 12 VÉRTEBRAS DEL FONDO + LA COLA.
    //  · ai[0]=quien me sigue · ai[1]=a quien sigo · ai[2]=la CABEZA
    //    (realLife = vida compartida: golpear cualquier hueso duele a la
    //    sierpe entera) · ai[3]=mi ÍNDICE en la cadena.
    //  · EL FOLLOW SUAVE DEL DoG: la dirección al padre se ROTA un 8% de
    //    la diferencia de rotación — 39 huesos curvan con gracia en vez
    //    de en esquinas (DevourerofGodsBody.cs l.255, literal).
    //  · SOBRESALIR DE LA TIERRA: behindTiles = true — el terreno tapa
    //    lo enterrado, gratis (el truco de vanilla EoW + los 4 worms de
    //    Calamity, verificado en SetDefaults).
    //  · LA COLA EN EL FONDO: los segmentos con índice ≥ UMBRAL_FONDO
    //    llevan NPC.hide = true (nadie los dibuja en el mundo) — los
    //    dibuja ColaSierpeSky proyectados ENTRE LAS CAPAS DEL PAISAJE
    //    (el hallazgo R59-a: SkyManager.DrawToDepth, el mecanismo del
    //    DoGSky de Calamity).
    // ======================================================================

    /// <summary>
    /// EL CUERPO — una vértebra con su par de costillas y la runa de oro
    /// de la Luz Primordial encendida en el centrum.
    ///
    /// v6.50.26 — LA SIERPE MÁS GRANDE (el reporte: «su tamaño no es
    /// suficiente, no es lo bastante grande»): 46 vértebras (34 de mundo
    /// + 12 del fondo), HUECO 64 px y TODO el arte dibujado a ESC 1.4 —
    /// la columna mide ~3.200 px de punta a punta y cada hueso es 40%
    /// más grande. El PreDraw ahora dibuja EL HUESO ÉL MISMO (a escala)
    /// porque tML solo sabe dibujar el sprite a 1:1.
    /// </summary>
    public class AethonSierpeCuerpo : ModNPC
    {
        /// <summary>Índice del PRIMER segmento que vive en el FONDO.</summary>
        public const int UMBRAL_FONDO = 34;

        /// <summary>Vértebras TOTALES de la cadena (34 mundo + 12 fondo).</summary>
        public const int TOTAL_VERTEBRAS = 46;

        /// <summary>Separación entre huesos (px).</summary>
        public const float HUECO = 64f;

        /// <summary>v6.50.26 — LA ESCALA DEL ARTE (la sierpe del final).</summary>
        public const float ESC = 1.40f;

        /// <summary>
        /// v6.50.21 — LA TEXTURA EXPLÍCITA (EL FIX DEL CARGADOR). Sin esta
        /// línea tML resuelve la textura por CONVENCIÓN de nombre:
        /// "Content/NPCs/AethonSierpeCuerpo.rawimg", que NO EXISTE en el
        /// paquete (el sprite de la vértebra vive como AethonSierpeVertebra
        /// y así lo piden ColaSierpeSky y las mandíbulas). Resultado en la
        /// 6.50.19/6.50.20: MissingResourceException en
        /// Mod.TransferAllAssets — EL MOD ENTERO QUEDABA DESACTIVADO AL
        /// CARGAR ("Se ha producido un error al cargar AethonMod...").
        /// El idioma de la casa (13 clases ya lo hacen) apunta a la textura
        /// real y el paquete vuelve a ser cerrado.
        /// </summary>
        public override string Texture => "AethonMod/Content/NPCs/AethonSierpeVertebra";

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            // El bestiario es de la CABEZA — los huesos no pagan entrada.
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
        }

        public override void SetDefaults()
        {
            NPC.width = 78;
            NPC.height = 78;
            NPC.damage = 62;
            NPC.defense = 30;
            NPC.lifeMax = 100;               // la vida REAL vive en la cabeza (realLife)
            NPC.HitSound = SoundID.NPCHit2;  // hueso
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;        // la sierpe nada por la tierra
            NPC.behindTiles = true;          // EL TRUCO: el terreno la tapa
            NPC.netAlways = true;
            NPC.npcSlots = 0.25f;
            NPC.aiStyle = -1;
        }

        public override void AI()
        {
            // === LOS GUARDAS DE LA CADENA (un hueso huérfano se cae) ===
            if (NPC.ai[2] < 0f || NPC.ai[2] >= Main.maxNPCs) { NPC.active = false; return; }
            NPC head = Main.npc[(int)NPC.ai[2]];
            if (!head.active || head.type != ModContent.NPCType<AethonBoss>()) { NPC.active = false; return; }
            if (NPC.ai[1] < 0f || NPC.ai[1] >= Main.maxNPCs) { NPC.active = false; return; }
            NPC parent = Main.npc[(int)NPC.ai[1]];
            if (!parent.active) { NPC.active = false; return; }

            // === LA VIDA COMPARTIDA (Calamity: se re-sincroniza cada tick) ===
            NPC.realLife = (int)NPC.ai[2];
            NPC.life = head.life;
            NPC.lifeMax = head.lifeMax;

            // === EL FOLLOW SUAVE DEL DEVOURER OF GODS ===
            // La dirección al padre, rotada un 8% de la diferencia de
            // rotación: la rigidez que hace que 39 huesos CURVEN.
            Vector2 dir = parent.Center - NPC.Center;
            if (dir == Vector2.Zero) dir = Vector2.UnitY;
            float dRot = MathHelper.WrapAngle(parent.rotation - NPC.rotation);
            dir = dir.RotatedBy(dRot * 0.08f);
            NPC.rotation = dir.ToRotation() + MathHelper.PiOver2;
            NPC.Center = parent.Center - dir.SafeNormalize(Vector2.Zero) * HUECO;
            NPC.velocity = Vector2.Zero;

            // === ¿ESTE HUESO VIVE EN EL FONDO? ===
            // Los últimos 12: invisibles en el mundo (hide), dibujados por
            // ColaSierpeSky enredados en el paisaje (proyección 1/prof).
            bool alFondo = NPC.ai[3] >= UMBRAL_FONDO;
            NPC.hide = alFondo;
            if (alFondo) NPC.damage = 0;     // lejanos: no muerden

            // LA LUZ que la columna deja en el mundo.
            if (!alFondo)
                Lighting.AddLight(NPC.Center, new Vector3(0.35f, 0.26f, 0.10f));
        }

        /// <summary>
        /// v6.50.26 — EL HUESO DIBUJADO A ESCALA (tML solo pinta el sprite
        /// 1:1 — la sierpe del final es 40% más grande): el hueso se dibuja
        /// AQUÍ (lote alfa) y LA RUNA DEL CENTRUM respira encima en el
        /// MISMO baile (lote aditivo) — un solo par Begin/End por vértebra.
        /// </summary>
        public override bool PreDraw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Main.dedServ || NPC.hide) return false; // escondido: nada que pintar
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                float latido = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.6f + NPC.ai[3] * 0.7f);

                // === 1) EL HUESO (lote alfa — el cuerpo de la columna) ===
                spriteBatch.Begin(Microsoft.Xna.Framework.Graphics.SpriteSortMode.Deferred,
                    Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend,
                    Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp,
                    Microsoft.Xna.Framework.Graphics.DepthStencilState.None,
                    Microsoft.Xna.Framework.Graphics.RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    var tex = ModContent.Request<Microsoft.Xna.Framework.Graphics.Texture2D>("AethonMod/Content/NPCs/AethonSierpeVertebra").Value;
                    Vector2 pos = NPC.Center - Main.screenPosition;
                    spriteBatch.Draw(tex, pos, null, drawColor,
                        NPC.rotation, tex.Size() * 0.5f, ESC,
                        Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
                }
                finally { VFXCore.CerrarLoteSiAbierto(); }

                // === 2) LA RUNA DORADA (lote aditivo — la Luz del centrum,
                //     el SoftGlow CACHEADO de la casa) ===
                spriteBatch.Begin(Microsoft.Xna.Framework.Graphics.SpriteSortMode.Deferred,
                    Microsoft.Xna.Framework.Graphics.BlendState.Additive,
                    Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp,
                    Microsoft.Xna.Framework.Graphics.DepthStencilState.None,
                    Microsoft.Xna.Framework.Graphics.RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    var glow = VFXCore.SoftGlow;
                    Vector2 pos = NPC.Center - Main.screenPosition;
                    // el glow pequeño y TENUE — el HUESO es el protagonista
                    // (la runa vive DENTRO).
                    spriteBatch.Draw(glow, pos, null,
                        new Color(255, 226, 140) * (0.38f * latido),
                        NPC.rotation, glow.Size() * 0.5f, new Vector2(0.30f, 0.42f) * ESC,
                        Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
                }
                finally { VFXCore.CerrarLoteSiAbierto(); }
            }
            catch { }
            finally { VFXCore.ReabrirLoteVanilla(); }
            return false; // el hueso ya se dibujó aquí (a escala)
        }

        public override bool? CanBeHitByProjectile(Projectile projectile) => true;
        public override bool? CanBeHitByItem(Player player, Item item) => true;

        /// <summary>El hueso NO suelta nada: el botín es de la cabeza.</summary>
        public override void OnKill()
        {
            for (int d = 0; d < 8; d++)
            {
                int idx = Dust.NewDust(NPC.Center, 40, 40, DustID.Bone,
                    Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-7f, 1f));
                Main.dust[idx].noGravity = true;
            }
        }
    }

    /// <summary>
    /// LA COLA — el remate afilado. Vive SIEMPRE en el fondo (hide): es
    /// la punta que se enreda en las imágenes del paisaje.
    /// </summary>
    public class AethonSierpeCola : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
        }

        public override void SetDefaults()
        {
            NPC.width = 40;
            NPC.height = 40;
            NPC.damage = 0;               // vive en el horizonte: no muerde
            NPC.defense = 30;
            NPC.lifeMax = 100;
            NPC.HitSound = SoundID.NPCHit2;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.netAlways = true;
            NPC.npcSlots = 0.25f;
            NPC.aiStyle = -1;
            NPC.hide = true;              // solo ColaSierpeSky la dibuja
        }

        public override void AI()
        {
            if (NPC.ai[2] < 0f || NPC.ai[2] >= Main.maxNPCs) { NPC.active = false; return; }
            NPC head = Main.npc[(int)NPC.ai[2]];
            if (!head.active || head.type != ModContent.NPCType<AethonBoss>()) { NPC.active = false; return; }
            if (NPC.ai[1] < 0f || NPC.ai[1] >= Main.maxNPCs) { NPC.active = false; return; }
            NPC parent = Main.npc[(int)NPC.ai[1]];
            if (!parent.active) { NPC.active = false; return; }

            NPC.realLife = (int)NPC.ai[2];
            NPC.life = head.life;
            NPC.lifeMax = head.lifeMax;

            Vector2 dir = parent.Center - NPC.Center;
            if (dir == Vector2.Zero) dir = Vector2.UnitY;
            float dRot = MathHelper.WrapAngle(parent.rotation - NPC.rotation);
            dir = dir.RotatedBy(dRot * 0.08f);
            NPC.rotation = dir.ToRotation() + MathHelper.PiOver2;
            NPC.Center = parent.Center - dir.SafeNormalize(Vector2.Zero) * AethonSierpeCuerpo.HUECO;
            NPC.velocity = Vector2.Zero;
        }

        public override void OnKill()
        {
            for (int d = 0; d < 8; d++)
            {
                int idx = Dust.NewDust(NPC.Center, 30, 30, DustID.Bone,
                    Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-7f, 1f));
                Main.dust[idx].noGravity = true;
            }
        }
    }
}
