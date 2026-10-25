using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.GameContent.Bestiary;
using AethonMod.Content.Coreografia;
using AethonMod.Content.Systems;
using AethonMod.Content.VFX;
using AethonMod.Content.Projectiles.Escriba;

namespace AethonMod.Content.NPCs.Escriba
{
    // ======================================================================
    //  v6.52.0 — EL ESCRIBA, LA MANO QUE DEVORÓ EL LIBRO: el jefe demo
    //  de la coreografía (la petición del usuario: «para nuestro jefe
    //  siento que le falta una coreografía clara y precisa además de
    //  más efectos visuales, más luces… que se vea mucho más avanzado
    //  en su visual y conceptos» — el video de ejemplo era el Meowzers
    //  Boss Fight de Zenovia, el dev de Lunar Veil).
    //
    //  EL CONCEPTO (R3 §4.5): el Escriba es la MANO QUE ESCRIBIÓ las
    //  páginas que Aethon devoró — una silueta de tinta alta y delgada
    //  con UN SOLO ojo blanco y un collar de páginas flotantes. Sus
    //  ataques SE ESCRIBEN en el aire antes de existir: la línea de
    //  tinta ES el telegraph.
    //
    //  LO QUE DEMUESTRA (todo lo que le falta al jefe actual — la
    //  auditoría R3 §3): gramática telegraph→activo→recuperación con
    //  CoreoGuion, barra de conjuro, luz escénica por beats, cámara
    //  cinemática, transiciones de fase de 6 beats, el fakeout, el
    //  nuke de 216 t, la jaula de arena, el agarrón (La Anotación) y
    //  la muerte-performance con firma caligráfica.
    //
    //  TODO el cuerpo es CÓDIGO (la casa: SombrasLib + VFXCore — ni un
    //  sprite del jefe) y TODO vive bajo el contrato del lote (la
    //  cura v6.51.14: encolar → FlushAdditive → finally Reabrir).
    // ======================================================================
    [AutoloadBossHead]
    public class EscribaJefe : ModNPC
    {
        // === LOS ESTADOS (NPC.ai[0] — el contrato MP de la casa) ===
        public const int ST_PORTADA = 0;
        public const int ST_ELECCION = 1;
        public const int ST_MUERTE = 40;

        // === LOS DATOS SINCRONIZADOS (ModNPC = instancia por entidad) ===
        public bool AgarreActivo;
        public bool OleadaAgresiva;
        public int CooldownAnotacion;
        public int UltimoAtaque = -1, PenultimoAtaque = -1;

        // EL CARRIL DEL TRAZO (el telegraph que se convierte en el tajo).
        public float CarrilOx, CarrilOy, CarrilDx, CarrilDy, CarrilLargo, CarrilSeparacion;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;

            // LA POSE DEL BESTIARIO (la cura v6.51.4: el catálogo dibuja
            // ARTE, no el cuerpo de código).
            NPCID.Sets.NPCBestiaryDrawOffset[NPC.type] =
                new NPCID.Sets.NPCBestiaryDrawModifiers()
                {
                    CustomTexturePath = "AethonMod/Content/NPCs/Escriba/EscribaJefe_PoseBestiario",
                    Scale = 0.42f,
                    PortraitScale = 0.95f,
                };
        }

        public override void SetDefaults()
        {
            NPC.width = 96;
            NPC.height = 210;
            NPC.damage = 90;
            NPC.defense = 70;
            NPC.lifeMax = 340_000;                  // el demo boss de la R3
            NPC.HitSound = SoundID.NPCHit54;
            NPC.DeathSound = SoundID.NPCDeath44;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.npcSlots = 30f;
            NPC.aiStyle = -1;
            NPC.netAlways = true;
            Music = MusicID.LunarBoss;
            SceneEffectPriority = SceneEffectPriority.BossHigh;
        }

        public override void BossHeadSlot(ref int index)
        {
            index = NPCHeadLoader.GetBossHeadSlot("AethonMod/Content/NPCs/Escriba/EscribaJefe_Head_Boss");
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement("Mods.AethonMod.NPCs.EscribaJefe.Bestiary"),
            });
        }

        public override void AI()
        {
            EscribaAtaques.Asegurar();
            NPC.TargetClosest(true);
            Player presa = Main.player[NPC.target];
            if (presa == null || !presa.active || presa.dead)
            {
                // SIN PRESA NO HAY HISTORIA (el jefe se va, como manda la casa).
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.EncourageDespawn(10);
                    CheckActiveDespawn();
                }
                return;
            }

            // === EL CONTROL DE FASES (solo la AUTORIDAD decreta) ===
            if (Main.netMode != NetmodeID.MultiplayerClient)
                PasoFases(presa);

            // === LA MUERTE-PROFORMANCE MANDA SOBRE TODO ===
            if (NPC.ai[0] == ST_MUERTE)
            {
                AIPuntoFinal(presa);
                return;
            }

            // === LA PORTADA (la intro de 200 t: la tinta llueve y el
            //     Escriba se ENSAMBLA — la primera impresión) ===
            if (NPC.ai[0] == ST_PORTADA)
            {
                NPC.ai[1] += 1f;
                NPC.velocity *= 0.9f;
                if (NPC.ai[1] == 1f)
                {
                    if (Main.netMode != NetmodeID.Server)
                    {
                        CamaraCine.Enfocar(NPC.Center, 3f);
                        Pantalla.Vineta(0.5f, 3.4f);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, NPC.Center);
                    }
                    EcoRed.AnunciarMundo("Mods.AethonMod.Escriba.Anuncio",
                        new Color(212, 190, 255));
                }
                if (NPC.ai[1] >= 200f)
                {
                    EntrarEstado(ST_ELECCION);
                }
                return;
            }

            // === LA ELECCIÓN (el compás de respiro entre ataques) ===
            if (NPC.ai[0] == ST_ELECCION)
            {
                NPC.ai[1] += 1f;
                // EL FLOTAR de la casa: deriva suave hacia su puesto (encima
                // de la presa, a 280 px, con el vaivén de siempre).
                Vector2 puesto = presa.Center + new Vector2(MathF.Sin(NPC.ai[1] * 0.02f) * 120f, -280f);
                Vector2 hacia = puesto - NPC.Center;
                if (hacia.LengthSquared() > 16f)
                    NPC.velocity = Vector2.Lerp(NPC.velocity, hacia * 0.045f, 0.08f);
                else NPC.velocity *= 0.9f;

                // EL DESCANSO respira con la fase (P3 = oleada corta).
                float paciencia = OleadaAgresiva ? 26f : (NPC.ai[2] >= 3f ? 34f : 55f);
                if (NPC.ai[1] >= paciencia && Main.netMode != NetmodeID.MultiplayerClient)
                    ElegirAtaque(presa);
                return;
            }

            // === UN ATAQUE CUALQUIERA: el DIRECTOR lleva el compás ===
            GuionAtaque guion = EscribaAtaques.DeEstado((int)NPC.ai[0]);
            if (guion != null)
            {
                CoreoGuion.Tick(NPC, presa, guion, NPC.ai[1]);
                if (CoreoGuion.Termino(guion, NPC.ai[1]))
                {
                    CoreoGuion.Salir(NPC, presa, guion);
                    EntrarEstado(ST_ELECCION);
                }
                else NPC.ai[1] += 1f;
                return;
            }

            // Estado desconocido (defensa): a elección.
            EntrarEstado(ST_ELECCION);
        }

        // ------------------------------------------------------------------
        //  LAS FASES — 66% y 33% (las transiciones SON eventos)
        // ------------------------------------------------------------------
        private void PasoFases(Player presa)
        {
            int estado = (int)NPC.ai[0];
            if (estado == EscribaAtaques.ST_TRANSICION1 || estado == EscribaAtaques.ST_TRANSICION2 ||
                estado == ST_MUERTE || estado == ST_PORTADA) return;

            float vida01 = NPC.life / (float)NPC.lifeMax;
            if (NPC.ai[2] < 2f && vida01 <= 0.66f)
            {
                NPC.ai[2] = 2f;
                EntrarEstado(EscribaAtaques.ST_TRANSICION1);
            }
            else if (NPC.ai[2] < 3f && vida01 <= 0.33f)
            {
                NPC.ai[2] = 3f;
                EntrarEstado(EscribaAtaques.ST_TRANSICION2);
            }
        }

        /// <summary>LA BOLSA PONDERADA con memoria de dos (la casa: sin
        /// repetir los dos últimos — la anti-montonia de Fargo).</summary>
        private void ElegirAtaque(Player presa)
        {
            if (CooldownAnotacion > 0) CooldownAnotacion--;

            int fase = (int)NPC.ai[2];
            var bolsa = new System.Collections.Generic.List<(int estado, int peso)>();
            void Meter(int e, int p)
            {
                if (e != UltimoAtaque && e != PenultimoAtaque) bolsa.Add((e, p));
            }
            Meter(EscribaAtaques.ST_TRAZO, 3);
            Meter(EscribaAtaques.ST_LLUVIA, 2);
            Meter(EscribaAtaques.ST_PLUMAS, 2);
            if (fase >= 2)
            {
                Meter(EscribaAtaques.ST_BORRON, 2);
                Meter(EscribaAtaques.ST_TRAZO_CORREGIDO, 1);
                Meter(EscribaAtaques.ST_JAULA, 1);
                Meter(EscribaAtaques.ST_VERSO, 2);
            }
            // LA ANOTACIÓN: el agarrón se ELIGE (cooldown 1500 t, a la
            // mitad si falló — la regla CWR del whiff).
            bool anotacionLista = CooldownAnotacion <= 0 && fase >= 3 &&
                EscribaAtaques.ST_ANOTACION != UltimoAtaque;
            if (anotacionLista)
            {
                EntrarEstado(EscribaAtaques.ST_ANOTACION);
                return;
            }

            int total = 0;
            foreach (var (_, p) in bolsa) total += p;
            if (total <= 0) { EntrarEstado(EscribaAtaques.ST_TRAZO); return; }
            int dado = Main.rand.Next(total);
            foreach (var (e, p) in bolsa)
            {
                dado -= p;
                if (dado < 0) { EntrarEstado(e); return; }
            }
            EntrarEstado(EscribaAtaques.ST_TRAZO);
        }

        private void EntrarEstado(int estado)
        {
            PenultimoAtaque = UltimoAtaque;
            UltimoAtaque = estado;
            NPC.ai[0] = estado;
            NPC.ai[1] = 0f;
            NPC.netUpdate = true;

            GuionAtaque guion = EscribaAtaques.DeEstado(estado);
            if (guion != null)
            {
                CoreoGuion.Entrar(NPC, Main.player[NPC.target], guion);
                // EL PESO DE LA ANOTACIÓN (cooldown al entrar al agarrón).
                if (estado == EscribaAtaques.ST_ANOTACION)
                    CooldownAnotacion = 1500;
            }
        }

        // ------------------------------------------------------------------
        //  EL CARRIL DEL TRAZO (el telegraph que se vuelve tajo)
        // ------------------------------------------------------------------
        public void MarcarCarril(Player presa, float separacion)
        {
            Vector2 predicha = presa.Center + presa.velocity * 18f;
            Vector2 dir = predicha - NPC.Center;
            if (dir.LengthSquared() < 4f) dir = -Vector2.UnitY;
            dir.Normalize();
            CarrilOx = NPC.Center.X;
            CarrilOy = NPC.Center.Y;
            CarrilDx = dir.X;
            CarrilDy = dir.Y;
            CarrilLargo = 940f;
            CarrilSeparacion = separacion;
            NPC.netUpdate = true;
        }

        public Vector2 CarrilOrigen() => new(CarrilOx, CarrilOy);
        public Vector2 CarrilDireccion() => new(CarrilDx, CarrilDy);
        public float CarrilLongitud() => CarrilLargo;
        public Color TelegrafoTinte(bool corregido) =>
            corregido ? new Color(205, 105, 245) : new Color(150, 110, 230);

        // ------------------------------------------------------------------
        //  EL AGARRE (La Anotación)
        // ------------------------------------------------------------------
        public void IntentarAgarre(Player presa)
        {
            AgarreActivo = Vector2.Distance(presa.Center, NPC.Center) < 230f;
            if (AgarreActivo && Main.netMode != NetmodeID.Server)
                CamaraCine.Enfocar(NPC.Center, 1.4f);
            NPC.netUpdate = true;
        }

        public void FinAgarre()
        {
            // EL WHIFF PERDONA A MEDIAS (la regla CWR): falló = cooldown a la mitad.
            if (!AgarreActivo && CooldownAnotacion > 750) CooldownAnotacion = 750;
            AgarreActivo = false;
            NPC.netUpdate = true;
        }

        // ------------------------------------------------------------------
        //  LA MUERTE — «EL PUNTO FINAL» (240 t de despedida)
        // ------------------------------------------------------------------
        public override bool CheckDead()
        {
            if (NPC.ai[0] != ST_MUERTE)
            {
                // LA PRIMERA VEZ: vida congelada a 1 y a la performance
                // (la cura v6.51.2: la muerte REAL corre AL FINAL, con
                // NPCLoot y bestiario — jamás active=false a mano).
                NPC.life = 1;
                EntrarEstadoMuerte();
                return false;
            }
            return true;   // el Punto Final ya se escribió: muere de verdad
        }

        private void EntrarEstadoMuerte()
        {
            NPC.ai[0] = ST_MUERTE;
            NPC.ai[1] = 0f;
            NPC.dontTakeDamage = true;
            NPC.netUpdate = true;
            if (Main.netMode != NetmodeID.Server)
            {
                CamaraCine.Enfocar(NPC.Center, 3.6f);
                LuzEscenica.Apagar();
                Pantalla.Vineta(0.85f, 2.2f);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath39, NPC.Center);
            }
        }

        private void AIPuntoFinal(Player presa)
        {
            NPC.ai[1] += 1f;
            NPC.velocity *= 0.88f;
            float t = NPC.ai[1];

            if (t == 30f && Main.netMode != NetmodeID.Server)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14.WithPitchOffset(-0.8f), NPC.Center);

            if (t >= 235f)
            {
                // EL CIERRE REAL (la lección v6.51.2): la kill se registra,
                // el botín cae, el bestiario abre la ficha.
                NPC.dontTakeDamage = false;
                NPC.life = 0;
                if (Main.netMode != NetmodeID.Server)
                {
                    Pantalla.Flash(new Color(255, 250, 240), 0.5f, 0.7f);
                    Pantalla.OndaExpansiva(NPC.Center, 900f, 0.8f);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, NPC.Center);
                }
            }
        }

        private void CheckActiveDespawn()
        {
            if (NPC.timeLeft > 10) NPC.timeLeft = 10;
        }

        public override bool CheckActive() => false;   // el jefe decide su salida (sin presa)

        // ------------------------------------------------------------------
        //  MP — TODO el estado extra viaja SIMÉTRICO (v6.50.62). En ModNPC
        //  el par ES (BinaryWriter)/(BinaryReader) — los bits van como bool.
        // ------------------------------------------------------------------
        public override void SendExtraAI(BinaryWriter binaryWriter)
        {
            try
            {
                binaryWriter.Write(AgarreActivo);
                binaryWriter.Write(OleadaAgresiva);
                binaryWriter.Write(CooldownAnotacion);
                binaryWriter.Write(UltimoAtaque);
                binaryWriter.Write(PenultimoAtaque);
                binaryWriter.Write(CarrilOx);
                binaryWriter.Write(CarrilOy);
                binaryWriter.Write(CarrilDx);
                binaryWriter.Write(CarrilDy);
                binaryWriter.Write(CarrilLargo);
                binaryWriter.Write(CarrilSeparacion);
            }
            catch { }
        }

        public override void ReceiveExtraAI(BinaryReader binaryReader)
        {
            try
            {
                AgarreActivo = binaryReader.ReadBoolean();
                OleadaAgresiva = binaryReader.ReadBoolean();
                CooldownAnotacion = binaryReader.ReadInt32();
                UltimoAtaque = binaryReader.ReadInt32();
                PenultimoAtaque = binaryReader.ReadInt32();
                CarrilOx = binaryReader.ReadSingle();
                CarrilOy = binaryReader.ReadSingle();
                CarrilDx = binaryReader.ReadSingle();
                CarrilDy = binaryReader.ReadSingle();
                CarrilLargo = binaryReader.ReadSingle();
                CarrilSeparacion = binaryReader.ReadSingle();
            }
            catch { }
        }

        // ==================================================================
        //  EL CUERPO — 100% CÓDIGO (la casa: SombrasLib + VFXCore)
        // ==================================================================
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            try
            {
                float tiempo = Main.GlobalTimeWrappedHourly;
                Player presa = Main.player[NPC.target];
                Vector2 centro = NPC.Center;
                float muerte01 = NPC.ai[0] == ST_MUERTE
                    ? MathHelper.Clamp(1f - NPC.ai[1] / 200f, 0.05f, 1f) : 1f;

                // === LA COLUMNA DE TINTA (el cuerpo alto) ===
                Vector2 raiz = centro + new Vector2(0, 105f);
                Vector2 cabeza = centro + new Vector2(
                    MathF.Sin(tiempo * 0.9f) * 6f, -128f + MathF.Sin(tiempo * 1.7f) * 5f);
                Vector2[] columna = SombrasLib.ColumnaViva(raiz, cabeza, tiempo, NPC.whoAmI * 17 + 3,
                    22, 0.24f, 2.2f);
                SombrasLib.Masa(columna, 46f * muerte01, 12f * muerte01, 0.85f * muerte01,
                    NPC.whoAmI * 31 + 5, tiempo, 30f * muerte01);
                SombrasLib.BrumaColumna(columna, 20f, 0.35f * muerte01, tiempo,
                    NPC.whoAmI * 11 + 2, 6);

                // === EL OJO ÚNICO (blanco, siguiendo a la presa) ===
                if (presa != null && presa.active)
                {
                    Vector2 mirada = presa.Center - cabeza;
                    SombrasLib.Ojo(cabeza + new Vector2(0, 18f), 22f * muerte01, mirada,
                        0.9f + 0.1f * MathF.Sin(tiempo * 2f), true, false);
                }

                // === EL COLLAR DE PÁGINAS (seis, orbitando el pecho) ===
                if (VFXCore.Presupuesto(8))
                {
                    for (int i = 0; i < 6; i++)
                    {
                        float ang = tiempo * 0.7f + i * MathHelper.TwoPi / 6f;
                        Vector2 pos = centro + new Vector2(MathF.Cos(ang) * 66f,
                            -40f + MathF.Sin(ang * 2f) * 24f);
                        VFXCore.Quad(pos, StormLib.Tint(new Color(238, 234, 248), 0.55f * muerte01),
                            new Vector2(15f, 21f), MathF.Sin(tiempo * 1.3f + i) * 0.5f, VFXCore.SoftGlow);
                    }
                }

                // === LA PLUMA GIGANTE (la mano derecha del Escriba) ===
                // El asta: una línea viva; la barba del plumaje: cinta.
                Vector2 hombro = centro + new Vector2(46f, -60f);
                Vector2 punta = hombro + new Vector2(38f, -74f) * muerte01;
                VFXCore.Line(hombro, punta, StormLib.Tint(new Color(240, 236, 250), 0.8f * muerte01), 4f);
                Vector2[] plumaje = { hombro + new Vector2(0, 8f),
                                      Vector2.Lerp(hombro, punta, 0.5f) + new Vector2(16f, 2f),
                                      punta };
                float[] anchosPluma = { 16f, 26f, 3f };
                VFXCore.Ribbon(plumaje, anchosPluma,
                    StormLib.Tint(new Color(170, 130, 235), 0.5f * muerte01),
                    VFXCore.SoftGlow, tiempo * 0.6f);

                // === LA FIRMA CALIGRÁFICA (la muerte la ESCRIBE) ===
                if (NPC.ai[0] == ST_MUERTE && NPC.ai[1] > 30f && VFXCore.Presupuesto(24))
                {
                    float t = MathHelper.Clamp((NPC.ai[1] - 30f) / 90f, 0f, 1f);
                    // La CURVA DE FIRMA: una S caligráfica grande cruzando el
                    // aire (5 puntos, Catmull-ish por el propio Ribbon).
                    Vector2 c = NPC.Center;
                    Vector2[] firma =
                    {
                        c + new Vector2(-420f, -60f),
                        c + new Vector2(-260f, 80f) ,
                        c + new Vector2(-60f, -110f),
                        c + new Vector2(160f, 90f),
                        c + new Vector2(400f, -40f),
                    };
                    // La firma SE ESCRIBE de izquierda a derecha (el trazo
                    // avanza con t — la revelación del nombre).
                    int hasta = Math.Max(2, (int)(firma.Length * (t * 1.2f)));
                    var visible = new Vector2[Math.Min(hasta, firma.Length)];
                    Array.Copy(firma, visible, visible.Length);
                    if (visible.Length >= 2)
                    {
                        float[] anchosF = new float[visible.Length];
                        for (int i = 0; i < visible.Length; i++)
                            anchosF[i] = 7f + 5f * MathF.Sin(i * 2.4f);
                        VFXCore.Ribbon(visible, anchosF,
                            StormLib.Tint(new Color(250, 246, 255), 0.7f),
                            VFXCore.GlowOrb, tiempo * 0.4f);
                    }
                }

                // === LA LUZ ESCÉNICA DEL CUERPO (el foco canal 0) ===
                LuzEscenica.Foco(0, centro, new Color(150, 110, 230), 1.4f * muerte01);

                // === EL VOLCADO ÚNICO (EL CONTRATO v6.51.14) ===
                if (VFXCore.QuadCount > 0)
                {
                    try { VFXCore.FlushAdditive(); }
                    finally { VFXCore.ReabrirLoteVanilla(); }
                }
            }
            catch { }
            return false;   // el sprite del NPC jamás se dibuja
        }
    }
}
