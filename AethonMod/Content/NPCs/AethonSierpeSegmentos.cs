using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

            // LA LUZ que la columna deja en el mundo (v6.50.32 — el
            // resplandor GRANATE del Dragón del Cielo: antes dorada).
            if (!alFondo)
                Lighting.AddLight(NPC.Center, new Vector3(0.30f, 0.10f, 0.05f));
        }

        /// <summary>
        /// v6.50.33 — EL ANILLO DEL DRAGÓN (la petición: «podrias crear
        /// sprite segmentados basados en slifer… como Devourer of Gods de
        /// Calamity sprites por segmento»): cada vértebra pinta SU SPRITE
        /// (AethonSierpeVertebra — chevrones de escama, aleta dorsal en
        /// teja, vientre de pizarra, filado frontal) con LA CURVA DE
        /// ESCALA anatómica: CUELLO fino junto al cráneo (0.50 — el
        /// cráneo manda y el ojo queda libre) → TORSO (1.0 del 6º al
        /// 14º) → PUNTA de látigo (0.36 en la 33). Los anillos se
        /// solapan 64 px (HUECO) y el filado claro del borde frontal lee
        /// «anillos encadenados» — el truco del DoG.
        /// </summary>
        public override bool PreDraw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Main.dedServ || NPC.hide) return false; // escondido: nada que pintar
            try
            {
                var tex = AethonSierpeArte.Vertebra();
                if (tex == null) return false;
                int idx = (int)NPC.ai[3];

                // LA CURVA DE ESCALA: cuello → torso → punta.
                float cuello = MathHelper.Lerp(0.50f, 1f, Math.Min(1f, idx / 6f));
                float punta = idx <= 6 ? 1f
                    : MathHelper.Lerp(1f, 0.36f, Math.Clamp((idx - 6) / 27f, 0f, 1f));
                float escala = cuello * punta * ESC;

                // LA DIRECCIÓN del hueso (tML guarda el rumbo + π/2) — el
                // sprite es HORIZONTAL: gira con el rumbo del viaje.
                float rumbo = NPC.rotation - MathHelper.PiOver2;

                // EL SUELO DE LUZ (el mismo del cráneo: la columna nunca
                // es un recorte negro — y el fade de nacimiento/muerte).
                float visibilidad = 1f - (NPC.alpha / 255f);
                Color c = Color.Lerp(drawColor, Color.White, 0.45f) * visibilidad;

                spriteBatch.Draw(tex, NPC.Center - screenPos, null, c, rumbo,
                    new Vector2(tex.Width, tex.Height) * 0.5f, escala,
                    SpriteEffects.None, 0f);
            }
            catch { }
            return false; // el sprite del anillo ya se dibujó aquí
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
