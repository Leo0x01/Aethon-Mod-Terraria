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
    ///
    /// v6.50.34 — LA SEÑORA DEL MUNDO (el reporte: «en cambio crea como
    /// jefe a la misma sierpe, pero mas grande y mas largo»): la cadena
    /// crece a 68 VÉRTEBRAS (54 de mundo + 14 del fondo), HUECO 84 px y
    /// TODO el arte a ESC 1.85 — la columna mide ~5.700 px de punta a
    /// punta (tres pantallas y media de 1080p) y cada hueso es 32%
    /// más grande que la v6.50.33. Los UMBRALES del taper y las ALETAS
    /// escalan con UMBRAL_FONDO — el cuerpo largo es MÚSCULO: grueso
    /// junto al cráneo, látigo en la punta.
    /// </summary>
    public class AethonSierpeCuerpo : ModNPC
    {
        /// <summary>Índice del PRIMER segmento que vive en el FONDO
        /// (v6.50.34: 54 — el cuerpo de mundo es MÁS LARGO).</summary>
        public const int UMBRAL_FONDO = 54;

        /// <summary>Vértebras TOTALES de la cadena
        /// (v6.50.34: 54 mundo + 14 fondo = 68).</summary>
        public const int TOTAL_VERTEBRAS = 68;

        /// <summary>Separación entre huesos (px) — v6.50.34: escala con
        /// ESC 1.85 (los anillos se SOLAPAN ~30 px: la cuenta de
        /// La Sierpe Estelar, huesos encadenados, no un tubo).</summary>
        public const float HUECO = 84f;

        /// <summary>v6.50.26 — LA ESCALA DEL ARTE (v6.50.34 — la Señora
        /// del Mundo: 1.4 → 1.85, cada hueso 32% más grande).</summary>
        public const float ESC = 1.85f;

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
            NPC.width = 100;    // v6.50.34 — el hueso a ESC 1.85
            NPC.height = 100;
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
        /// v6.50.27 — EL ARTE NUEVO DE LA SIERPE ESTELAR (el reporte:
        /// «el arte del jefe se ve horrible, deberías cambiarlo por
        /// completo, algo al estilo de la sierpe en el arma La Sierpe
        /// Estelar»): el hueso SPRITE muere — cada vértebra es AHORA
        /// CRIATURA DE CÓDIGO (AethonSierpeArte.Vertebra): el cuerpo de
        /// vacío del pase alfa (la silueta con contraste) + la Luz de la
        /// columna (velo, espina, apófisis estelar, centrum ardiendo) +
        /// LAS ALETAS de varillas cada 8 huesos (alternando lado — el
        /// rasgo de pez de La Sierpe Estelar). El TAPER es muscular:
        /// 33 px junto al cráneo → 10 en la punta de mundo.
        ///
        /// v6.50.34 — LA SEÑORA DEL MUNDO: con 54 huesos de mundo las
        /// ALETAS recorren toda la columna (6, 14, 22, 30, 38 y 46 —
        /// el abanico cada 8, el lado alterna) y el TAPER se afina
        /// (33→10): el cuerpo LARGO lee músculo de acecho, no tubo.
        /// </summary>
        public override bool PreDraw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Main.dedServ || NPC.hide) return false; // escondido: nada que pintar
            try
            {
                int idx = (int)NPC.ai[3];
                float visibilidad = 1f - (NPC.alpha / 255f);
                // EL TAPER: el radio del hueso (graso junto al cráneo,
                // afilado hacia el fondo). v6.50.34 — 33→10: el cuerpo
                // largo se afila MÁS (el látigo de la Señora del Mundo).
                float radio = MathHelper.Lerp(33f, 10f, Math.Clamp(idx / (float)(UMBRAL_FONDO - 1), 0f, 1f)) * ESC;
                // LA DIRECCIÓN del hueso (tML guarda el rumbo + π/2).
                float rumbo = NPC.rotation - MathHelper.PiOver2;
                // v6.50.34 — LAS ALETAS: cada 8 huesos desde el 6º, el
                // lado alterna (6,14,22,30,38,46 — el abanico recorre
                // TODA la columna larga).
                float ladoAleta = 0f;
                int nAleta = (idx - 6) / 8;
                if (idx >= 6 && (idx - 6) % 8 == 0 && nAleta < 6)
                    ladoAleta = (nAleta & 1) == 0 ? 1f : -1f;

                AethonSierpeArte.Vertebra(NPC.Center - Main.screenPosition, rumbo,
                    radio, Main.GlobalTimeWrappedHourly, idx, visibilidad, ladoAleta);
            }
            catch { }
            return false; // el arte de código ya se dibujó aquí
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
            NPC.width = 52;     // v6.50.34 — la cola a ESC 1.85
            NPC.height = 52;
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
