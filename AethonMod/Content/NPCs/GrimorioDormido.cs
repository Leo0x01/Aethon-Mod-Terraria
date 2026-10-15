using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Systems;
using AethonMod.Content.VFX;

namespace AethonMod.Content.NPCs
{
    // ======================================================================
    //  EL GRIMORIO DORMIDO (v6.50.117 — LA CAÍDA).
    //
    // LA LETRA DEL USUARIO: «el grimorio cae y queda FLOTANDO DORMIDO con
    // emoji de sueño (como los NPC de Terraria dormidos — dibuja "Z z z"
    // ascendiendo)», «al TOMARLO sale una VENTANA DE DIÁLOGO como la de
    // los NPC donde el grimorio habla por primera vez y deja claro que
    // hablará con los PENSAMIENTOS del jugador (esos serán los diálogos
    // flotantes que ya tiene el mod — EcoLib)».
    //
    // EL TRUCO TÉCNICO: townNPC = true (la ventana vanilla de diálogo lo
    // exige) PERO CanTownNPCSpawn => false (vanilla JAMÁS lo auto-spawnea
    // — solo lo engendra LaCaidaSistema en el cráter de su cometa) y
    // aiStyle = -1 (IA propia: FLOTA con bobbing, no camina, no pide
    // casa). Intocable: su cita es con UNA mano, no con espadas.
    //
    // EL DESTINATARIO viaja en npc.ai[1] (el índice — se refresca al
    // reconectar) y en npc.GivenName (el nombre — el 56 lo viste en los
    // clientes; su primer tick de IA lo reconstruye de ai[1] si el cable
    // llegó tarde).
    //
    // v6.50.122 — LA DEVORACIÓN DEL DOBLE (la letra: «el grimorio siempre
    // cae, pero si tienes uno en el inventario, el del inventario hace
    // lo mismo que le hace a los jefes en la primera muerte, se lo come
    // y lo devora como si fuera un jefe»): si el destinatario ya carga
    // un Grimorio del Eterno y SE ACERCA (~420 px), el libro del
    // inventario despierta — SELLOS RÚNICOS + LA SOMBRA DE LA PÁGINA se
    // lo comen (CaidaDevoradaProjectile, el cine). El reloj del festín
    // viaja en ai[2]: todas las pantallas ven temblar al devorado; la
    // AUTORIDAD cierra a los 200 t (LaCaidaSistema.DevorarLibroCaido:
    // marca el alma, apaga la pendiente y el Eterno dice su línea por
    // los PENSAMIENTOS — EcoRed).
    // ======================================================================
    public class GrimorioDormido : ModNPC
    {
        /// <summary>El color del sueño (las ZZZ).</summary>
        private static readonly Color TinteSueño = new(200, 170, 255);

        /// <summary>La altura de la "Z" de la fuente (cacheada UNA vez — la casa no mide en render).</summary>
        private static float _altoZ = -1f;

        // v6.50.124 — LA LETRA: «el grimorio de la caída debe tener el
        // ojo cerrado»: el Nervioso (el libro con el ojo ABIERTO) se
        // cambia por su hermano _Cerrado — el mismo cuerpo, el párpado
        // bajado: DORMIDO de verdad (las Zzz y la luz violeta del sueño
        // siguen intactas — el libro sueña a ojos cerrados).
        public override string Texture => "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado";

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            // El DisplayName vive en la localización (NPCs.GrimorioDormido).
        }

        public override void SetDefaults()
        {
            NPC.width = 30;                 // el cuerpo del Nervioso (la textura del Eterno)
            NPC.height = 42;
            NPC.lifeMax = 5000;
            NPC.damage = 0;                 // no hace daño: está DORMIDO
            NPC.defense = 0;
            NPC.dontTakeDamage = true;      // intocable — ni el héroe más tonto
            NPC.knockBackResist = 0f;
            NPC.townNPC = true;             // LA VENTANA DE DIÁLOGO VANILLA
            NPC.aiStyle = -1;               // IA propia (flota, no camina)
            NPC.noGravity = true;
            NPC.noTileCollide = true;       // flota sobre su cráter
            NPC.npcSlots = 0.01f;           // no ocupa la población del mundo
            NPC.netAlways = true;           // su cita es de todos los cables
            NPC.value = 0;                  // no es una presa
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;

            // El libro NO busca casa: su cráter es su hogar (fuera del
            // sistema de housing de vanilla — TownNPCStayingHomeless).
            TownNPCStayingHomeless = true;
        }

        /// <summary>Vanilla JAMÁS lo engendra — solo LaCaidaSistema (y su lista persistente).</summary>
        public override bool CanTownNPCSpawn(int numTownNPCs) => false;

        /// <summary>El libro ESPERA (CheckActive false: ni noche ni distancia lo despide).</summary>
        public override bool CheckActive() => false;

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            // ENTRADA MÍNIMA (el patrón de la casa): una sola línea de
            // sabor — el bestiario no necesita más de un libro dormido.
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement("Mods.AethonMod.NPCs.GrimorioDormido.Bestiario"),
            });
        }

        // ================================================================
        //  LA IA — flota dormido (bobbing ±6 px, rotación sutil, cero pasos)
        // ================================================================
        public override void AI()
        {
            // === EL PRIMER TICK: las anclas del bobbing ===
            if (NPC.localAI[0] == 0f)
            {
                NPC.localAI[0] = 1f;
                NPC.localAI[1] = NPC.position.Y; // la altura base (donde cayó)
                NPC.localAI[2] = NPC.position.X; // v6.50.122 — la X base (el temblor del devorado)
                NPC.ai[0] = 0f;                  // el reloj compartido del bobbing
                NPC.netUpdate = true;
            }

            // === EL NOMBRE RECONSTRUIDO (MP): GivenName no viaja en el
            //     cable — ai[1] (el destinatario) SÍ: cada máquina lo
            //     traduce a nombre cada segundo PERO SOLO si aún no lo
            //     conoce (nunca pisa el nombre bueno con el de un índice
            //     reciclado — el libro sabe a quién espera). Así el cliente
            //     que llegó tarde aprende el nombre (y el índice NUEVO de
            //     una reconexión lo refresca el reconciliador del server).
            if (NPC.ai[0] % 60f == 0f && string.IsNullOrEmpty(NPC.GivenName))
            {
                int idx = (int)NPC.ai[1];
                if (idx >= 0 && idx < Main.maxPlayers &&
                    Main.player[idx] != null && Main.player[idx].active &&
                    !string.IsNullOrEmpty(Main.player[idx].name))
                    NPC.GivenName = Main.player[idx].name;
            }

            // === v6.50.122 — LA DEVORACIÓN DEL DOBLE: cuando su
            //     DESTINATARIO con un Grimorio del Eterno encima se acerca
            //     (~420 px — justo antes del alcance de la charla), el
            //     libro del inventario despierta y se lo COME. La
            //     AUTORIDAD decide (SP/host/server); el reloj del festín
            //     (ai[2]) viaja en el SyncNPC para que todas las pantallas
            //     vean temblar al devorado. ===
            if (NPC.ai[2] <= 0f && Main.netMode != NetmodeID.MultiplayerClient &&
                NPC.ai[0] % 30f == 0f)
            {
                Player hambriento = DestinatarioVivo();
                if (hambriento != null && !hambriento.dead &&
                    Vector2.DistanceSquared(hambriento.Center, NPC.Center) < 420f * 420f &&
                    LaCaidaSistema.TieneGrimorio(hambriento))
                {
                    NPC.ai[2] = 1f;              // el reloj del festín ARRANCA
                    NPC.netUpdate = true;
                    // LA PELÍCULA (netImportant: viaja sola a todas las pantallas)
                    if (Main.projectile.Length > 0)
                    {
                        int pr = Projectile.NewProjectile(NPC.GetSource_FromAI(),
                            NPC.Center, Vector2.Zero,
                            ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Sombras.CaidaDevoradaProjectile>(),
                            0, 0f, Main.myPlayer, NPC.whoAmI, hambriento.whoAmI);
                        if (pr >= 0 && pr < Main.projectile.Length)
                            Main.projectile[pr].timeLeft = 260;
                    }
                }
            }

            // === EL RELOJ DEL SUEÑO (ai[0]: compartido con el cable) ===
            NPC.ai[0] += 1f;

            NPC.velocity = Vector2.Zero;

            if (NPC.ai[2] <= 0f)
            {
                // === EL BOBBING: Y ±6 px (sin(t·0.03)) — flota, no camina ===
                NPC.position.Y = NPC.localAI[1] + MathF.Sin(NPC.ai[0] * 0.03f) * 6f;

                // === LA ROTACIÓN SUTIL: se mece como un péndulo perezoso ===
                NPC.rotation = MathF.Sin(NPC.ai[0] * 0.021f) * 0.06f;

                // === LA LUZ VIOLETA DEL SUEÑO (se ve en la noche del cráter) ===
                Lighting.AddLight(NPC.Center, 0.32f, 0.24f, 0.50f);

                // === LA RESPIRACIÓN DEL CABLE (posición por el cable, suave) ===
                if (NPC.ai[0] % 45f == 0f) NPC.netUpdate = true;
            }
            else
            {
                // === v6.50.122 — EL DEVORADO: el reloj del festín corre en
                //     TODAS las máquinas y el libro TIEMBLA cada vez más
                //     desesperado mientras se lo comen — determinista
                //     (senos del reloj, jamás Main.rand: todas las
                //     pantallas ven EL MISMO espasmo). ===
                NPC.ai[2] += 1f;
                float f = MathHelper.Clamp(NPC.ai[2] / 200f, 0f, 1f);
                float temblor = 0.8f + 2.6f * f;
                NPC.position.X = NPC.localAI[2]
                    + MathF.Sin(NPC.ai[2] * 1.7f) * temblor
                    + MathF.Sin(NPC.ai[2] * 3.1f) * temblor * 0.4f;
                NPC.position.Y = NPC.localAI[1] + MathF.Sin(NPC.ai[2] * 2.3f) * temblor * 0.5f;
                NPC.rotation = MathF.Sin(NPC.ai[2] * 0.35f) * 0.10f * f;

                // LA LUZ SE LE MUERE mientras lo comen (la vida se le va
                // por la boca de la sombra).
                Lighting.AddLight(NPC.Center, 0.32f * (1f - f), 0.24f * (1f - f), 0.50f * (1f - f * 0.7f));

                // LA AUTORIDAD CIERRA EL CUENTO a los 200 t (la película
                // llega a su TRAGO y el Eterno se traga al doble).
                if (Main.netMode != NetmodeID.MultiplayerClient && NPC.ai[2] >= 200f)
                    LaCaidaSistema.DevorarLibroCaido(NPC, DestinatarioVivo());

                // LA RESPIRACIÓN DEL CABLE (el temblor también viaja).
                if (NPC.ai[0] % 15f == 0f) NPC.netUpdate = true;
            }
        }

        // ================================================================
        //  EL DIÁLOGO — la ventana vanilla habla
        // ================================================================

        /// <summary>¿El jugador local es el destinatario de ESTE libro?</summary>
        private bool EsDestinatario(Player p)
            => p != null && !string.IsNullOrEmpty(NPC.GivenName) && p.name == NPC.GivenName;

        /// <summary>v6.50.122 — El destinatario VIVO de este libro (por
        /// NOMBRE — el cable puede traer índices reciclados: el nombre es
        /// la identidad del pacto).</summary>
        private Player DestinatarioVivo()
        {
            if (string.IsNullOrEmpty(NPC.GivenName)) return null;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p != null && p.active && p.name == NPC.GivenName) return p;
            }
            return null;
        }

        public override string GetChat()
        {
            Player yo = Main.LocalPlayer;

            // OTRO JUGADOR: el libro ni despierta del todo.
            if (!EsDestinatario(yo))
                return Language.GetTextValue("Mods.AethonMod.Caida.OtroJugador");

            // v6.50.122 — LO ESTÁN DEVORANDO: el Eterno del inventario se
            // lo está comiendo — ya no puede hablar (el festín corre).
            if (NPC.ai[2] > 0f)
                return Language.GetTextValue("Mods.AethonMod.Caida.Devorandose");

            // DESTINATARIO CON GRIMORIO YA: una sola alma, un solo libro.
            // (El caso raro: charló ANTES del pulso de los 30 t del
            // gatillo — al siguiente tick el Eterno lo devora igual.)
            if (LaCaidaSistema.TieneGrimorio(yo))
                return Language.GetTextValue("Mods.AethonMod.Caida.YaTienes");

            // LA PRESENTACIÓN: el libro DESPIERTA al sentir la mano (4-6
            // frases — habla por primera vez y promete hablar a los
            // PENSAMIENTOS: los diálogos flotantes de EcoLib).
            return Language.GetTextValue("Mods.AethonMod.Caida.Presentacion");
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = "";
            button2 = "";
            Player yo = Main.LocalPlayer;

            // EL BOTÓN SOLO PARA LA MANO CORRECTA (y sin libro ya — y sin
            // festín en curso: mientras lo devoran no hay nada que tomar).
            if (EsDestinatario(yo) && NPC.ai[2] <= 0f && !LaCaidaSistema.TieneGrimorio(yo))
                button = Language.GetTextValue("Mods.AethonMod.Caida.BotonTomar");
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (!firstButton) return;
            Player yo = Main.LocalPlayer;
            if (yo == null) return;

            // LA REVALIDACIÓN DEL CLIC (destinatario + sin libro — el
            // resto lo revalida la AUTORIDAD en TomarElLibro).
            if (!EsDestinatario(yo) || LaCaidaSistema.TieneGrimorio(yo)) return;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // MP: la mano se EXTIENDE por el cable — el libro, el ítem
                // y la marca nacen del server (la dádiva regresa al clic).
                EcoRed.PedirTomarCaida();
                CerrarVentana();
                return;
            }

            // SP / HOST: la AUTORIDAD está aquí mismo.
            LaCaidaSistema.TomarElLibro(NPC, yo);
            CerrarVentana();
        }

        /// <summary>Cierra la ventana de diálogo (el libro ya no está).</summary>
        private static void CerrarVentana()
        {
            try
            {
                Main.npcChatText = "";
                Main.player[Main.myPlayer].SetTalkNPC(-1, true);
            }
            catch { }
        }

        // ================================================================
        //  EL DIBUJO — bruma morada + ZZZ + el libro violeta
        // ================================================================
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos,
            Color drawColor)
        {
            if (Main.dedServ) return false;

            // === 1. LA BRUMA MORADA (BrumaFX — la misma llamada de los
            //     jefes: la niebla fría del cráter lo abraza) ===
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                SombrasLib.Bruma(NPC.Center, 46f, 0.25f,
                    Main.GlobalTimeWrappedHourly, NPC.whoAmI * 31 + 7);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();

            // === 2. LAS "Z z z" (el sueño de los NPC de vanilla — tres Z
            //     ascendentes, crecientes, con alpha pulsante). v6.50.122 —
            //     SIN ZZZ MIENTRAS LO DEVORAN: al comérselo se le muere el
            //     sueño (solo le queda el espasmo del cuerpo). ===
            if (NPC.ai[2] <= 0f)
            {
                try { DibujarZzz(spriteBatch, screenPos); }
                catch { }
            }

            // === 3. EL LIBRO: se dibuja AQUÍ (violeta SIEMPRE — el aura
            //     del sueño no se apaga con la luz del mundo) ===
            try
            {
                Texture2D tex = Terraria.GameContent.TextureAssets.Npc[Type].Value;
                Color color = Color.Lerp(drawColor, new Color(205, 175, 255), 0.5f);
                // v6.50.122 — EL DEVORADO SE APAGA: mientras se lo comen,
                // el doble pierde cuerpo (la vida se le va por la boca).
                float fDev = MathHelper.Clamp(NPC.ai[2] / 200f, 0f, 1f);
                color *= 1f - 0.45f * fDev;
                spriteBatch.Draw(tex, NPC.Center - screenPos, null, color,
                    NPC.rotation, tex.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            }
            catch { }

            return false; // el libro SE dibuja solo (con su aura)
        }

        /// <summary>Las tres Z del sueño: tamaños 12/16/20, offsets (18,−30)/(26,−46)/(34,−62), flotando con el bobbing.</summary>
        private void DibujarZzz(SpriteBatch sb, Vector2 screenPos)
        {
            // La fuente del sueño (la de los rótulos de la UI de vanilla).
            DynamicSpriteFont fuente = Terraria.GameContent.FontAssets.MouseText.Value;
            if (fuente == null) return;

            // LA MEDIDA, UNA SOLA VEZ (la regla de la casa: cero
            // MeasureString por frame).
            if (_altoZ <= 0f)
            {
                _altoZ = fuente.MeasureString("Z").Y;
                if (_altoZ <= 0f) return;
            }

            float t = Main.GlobalTimeWrappedHourly;
            Vector2 basePos = NPC.Top - screenPos + new Vector2(6f, -4f);

            for (int i = 0; i < 3; i++)
            {
                // EL CICLO ASCENDENTE (cada Z sube y se disuelve, corrida
                // de la anterior — la procesión del sueño).
                float ciclo = (t * 0.42f + i * 0.334f) % 1f;

                float tamaño = 12f + i * 4f;                        // 12 / 16 / 20
                Vector2 off = new Vector2(18f + i * 8f, -30f - i * 16f) // (18,−30) (26,−46) (34,−62)
                    + new Vector2(MathF.Sin(t * 1.3f + i) * 2.5f, -16f * ciclo);
                float pulso = 0.72f + 0.28f * MathF.Sin(t * 2.6f + i * 2.1f);
                byte alfa = (byte)(235f * (1f - ciclo) * pulso);

                sb.DrawString(fuente, "Z", basePos + off,
                    new Color(TinteSueño.R, TinteSueño.G, TinteSueño.B, alfa),
                    0f, Vector2.Zero, tamaño / _altoZ, SpriteEffects.None, 0f);
            }
        }
    }
}
