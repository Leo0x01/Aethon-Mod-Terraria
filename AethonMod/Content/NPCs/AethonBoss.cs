using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Systems;
using AethonMod.Content.Projectiles.Jefes;

namespace AethonMod.Content.NPCs
{
    /// <summary>
    /// Aethon, la Luz Primordial — LA SIERPE DE HUESO (v6.50.19).
    ///
    /// Petición del usuario: "el jefe final debe ser una sierpe gigante
    /// y cuando se presente su cola debe enredarse en las imágenes de
    /// fondo, la sierpe debe sobresalir de la tierra y su ataque deben
    /// salir de su cabeza... esquelética con aspecto del esqueleto de
    /// una serpiente". Investigación R59-a (Devourer of Gods / Storm
    /// Weaver / Desert Scourge + el hallazgo del SkyManager):
    ///
    /// · LA CABEZA (esta clase — el mismo nombre SIEMPRE: los guardados
    ///   y el El Nombre de Aethon la convocan) spawnea 26 VÉRTEBRAS de
    ///   mundo + 12 del FONDO + LA COLA: 39 huesos, ~2.100 px de sierpe.
    /// · SOBRESALE DE LA TIERRA: ciclo CAZA SUBTERRÁNEA → EMERGER
    ///   (lunge) → ARCO EN SUPERFICIE → HUNDIRSE; behindTiles = true
    ///   hace que el TERRENO tape lo enterrado (gratis).
    /// · LA COLA SE ENREDA EN EL FONDO: los últimos 12 huesos + la cola
    ///   no existen en el plano del mundo (hide) — ColaSierpeSky los
    ///   proyecta ENTRE LAS CAPAS DEL PAISAJE (SkyManager.DrawToDepth).
    /// · SUS ATAQUES SALEN DE LA CABEZA: TODO proyectil nace de
    ///   BocaPos() — las mandíbulas cinéticas ABREN al disparar (el
    ///   patrón del DoG: dos hemimandíbulas espejadas girando).
    /// · ESQUELÉTICA: cráneo + mandíbulas + vértebras con costillas —
    ///   hueso cálido con el ORO de la Luz en cuencas y columnas.
    ///
    /// LAS CINCO FASES (la identidad de la pelea, re-anclada):
    /// 1 · POLVO ESTELAR — la espiral de pernos nace de la BOCA al
    ///     emerger; la sierpe arca serena sobre la presa.
    /// 2 · NEBULOSA — al EMERGER escupe las nubes que queman; el polvo
    ///     de hueso mancha el aire.
    /// 3 · GRAVEDAD — el VOLTEO mientras arca en superficie + pernos
    ///     convergentes de la boca + anillos de aviso.
    /// 4 · AGUJERO NEGRO — AL HUNDIRSE SE LA TRAGA: la singularidad
    ///     nace donde ESTÁS, ella vigila arqueando ANCHA alrededor,
    ///     los jets del disco y LAS RUNAS orbitan su cráneo.
    /// 5 · RECONOCIMIENTO — el arco se CIÑE (el acecho), EL RECORDAR
    ///     sale de las fauces abiertas al emerger, cuatro runas.
    ///
    /// EL DROP CUMPLIDO (v6.48): al morir deja LA FORMA ASCENDIDA. La
    /// muerte es CINE: la sierpe se yergue, la luz se le escapa, y los
    /// huesos se desarticulan UNO A UNO de la cola a la cabeza (el
    /// patrón DoG CheckDead→false + DeathAnimationTimer).
    /// Desbloqueo: el Fragmento Génesis alcanza nivel 150; convócala
    /// con El Nombre de Aethon (de día).
    ///
    /// v6.50.26 — EL JEFE DEL FINAL DE VERDAD (el reporte: «debes mejorar
    /// al jefe final Aethon, su arte, su animación, su IA, sus ataques y
    /// también su tamaño, no es lo suficientemente grande; no aparece
    /// una sección de él en el fondo cuando está llegando; no tiene
    /// barra de vida de jefe como otros jefes»):
    /// · LA BARRA DE VIDA: [AutoloadBossHead] + el icono
    ///   AethonBoss_Head_Boss.png — el índice de cabeza de jefe engancha
    ///   la BARRA COMÚN DE VANILLA (CommonBossBigProgressBar la muestra
    ///   para todo NPC con cabeza de jefe — verificado en el decompile).
    /// · MÁS GRANDE: 46 vértebras (34 de mundo + 12 del fondo) a 64 px de
    ///   HUECO, TODO el arte a ESC 1.4 — ~3.400 px de columna.
    /// · LA LLEGADA: ColaSierpeSky pinta la SILUETA GIGANTE cruzando el
    ///   cielo del fondo mientras el cráneo se materializa.
    /// · LA IA NUEVA: EL RAM HORIZONTAL (telegraph + embestida a la
    ///   altura de la presa) y EL ALIENTO PRIMORDIAL (el arco de rayo
    ///   BocaPos→presa, con lluvia de pernos — los rayos de la casa).
    /// · LA ANIMACIÓN: el mordisco al cerrar el arco, el cabeceo
    ///   serpenteo del cráneo, el cháchara de mandíbula del aliento y
    ///   la fare de ojos al disparar.
    ///
    /// v6.50.34 — LA SEÑORA DEL MUNDO (el reporte: «se ve horrible
    /// jajajajaja, mejor borra a ese jefe y olvidemonos de el — en cambio
    /// crea como jefe a la misma sierpe, pero mas grande y mas largo, y
    /// mejora su IA»): EL DRAGÓN DE SPRITES (v6.50.32/33) MUERE — el
    /// jefe vuelve a ser LA SIERPE ESTELAR de la v6.50.27 (el
    /// cráneo-eclipse de código, las placas de vacío, la espina de oro)
    /// PERO a ESCALA DE DIOSA:
    /// · MÁS GRANDE: ESC 1.4 → 1.85 (cada hueso 32% más grande).
    /// · MÁS LARGA: 46 → 68 vértebras (54 de mundo + 14 del fondo), la
    ///   columna ~5.700 px — tres pantallas y media de 1080p.
    /// · LA IA (la letra del pedido):
    ///   - LA ROTACIÓN: fase 2+, el cierre de cada arco lanza RAM o
    ///     CLAVADO — NUNCA el mismo dos veces: hay que cubrir el eje
    ///     horizontal Y el vertical a la vez.
    ///   - EL CLAVADO AÉREO (nuevo, EST_CLAVADO): telegraph de 20 t
    ///     encima de la presa + caída a través de ella hasta la tierra.
    ///   - LA CADENA: el ram de fase 3+ ENCADENA embestidas (2, y 3 en
    ///     la furia) desde lados opuestos — la vuelta en U del DoG.
    ///   - LA PREDICCIÓN ADAPTATIVA: el lead del lunge/clavado escala
    ///     con la distancia (lejos anticipa, cerca dispara franco).
    ///   - EL ANTI-CAMPING: presa quieta 1,5 s → la paciencia de la
    ///     emboscada se derrite (110→24 t).
    ///   - LA FURIA (P5): nado/lunge/ram más rápidos, telegraphs cortos
    ///     (36→26, encadenados 14) y EL ALIENTO DOBLE por arco.
    /// </summary>
    [AutoloadBossHead]
    public class AethonBoss : ModNPC
    {
        // === LOS ESTADOS DE LA SIERPE ===
        private const int EST_NACIENDO = 0;     // la presentación
        private const int EST_BAJO_TIERRA = 1;  // la caza subterránea
        private const int EST_EMERGIENDO = 2;   // EL LUNGE
        private const int EST_SUPERFICIE = 3;   // el arco sobre la presa
        private const int EST_HUNDIENDO = 4;    // el clavado
        private const int EST_CARGA = 5;        // v6.50.26 — EL RAM HORIZONTAL
        private const int EST_CLAVADO = 6;      // v6.50.34 — EL CLAVADO AÉREO
        private const int EST_MURIENDO = 99;    // el cine final

        private int _estado = EST_NACIENDO;
        private int _tickEstado = 0;
        private bool _cadenaCreada = false;
        private bool _bajoTierra = true;        // el detector de superficie
        private float _aberturaMandibula = 0.06f; // las fauces cinéticas

        // === v6.50.26 — EL RAM Y EL ALIENTO ===
        private int _dirCarga = 1;              // hacia qué lado embiste
        private int _arcos = 0;                 // arcos completados (el ram alterna)
        private int _aliento = 0;               // 0: no · 1: cargando · 2: escupiendo
        private int _tickAliento = 0;           // el tempo del aliento
        private bool _alientoEsteCiclo = false; // una vez por ciclo de superficie

        // === v6.50.34 — LA CAZA INTELIGENTE (el reporte: «mejora su IA») ===
        // · LA ROTACIÓN: la sierpe ALTERNA sus ataques de superficie — si
        //   acababa de RAMear, ahora CLAVA desde el cielo; si clavó, ramea.
        //   Nunca repite el mismo patrón dos veces seguidas (leerla es
        //   IMPOSIBLE: la única defensa es moverse).
        // · LA CADENA: el ram de la fase 3+ ENCADENA embestidas del DoG
        //   (2 en P3/P4, 3 en la furia) — cada una desde el lado opuesto.
        // · LA PREDICCIÓN ADAPTATIVA: el lead del lunge/clavado escala con
        //   la distancia (lejos = mucha anticipación, cerca = tiro franco).
        // · EL ANTI-CAMPING: la presa parada 1,5 s derrite la paciencia
        //   de la emboscada — quedarse quieto es invitación al lunge.
        // · LA FURIA (P5): todo más rápido — nado, lunge, ram y telegraphs.
        private int _ultimoAtaque = 0;          // 0: ninguno · 1: ram · 2: clavado
        private int _cargasEnCadena = 0;        // cuántos ram lleva la cadena
        private int _ticksPresaQuieta = 0;      // el contador anti-camping
        private bool _alientoDoble = false;     // P5: el segundo aliento del ciclo
        private int _telegraphCarga = 36;       // ticks del aviso (viaja en ai[3])

        /// <summary>v6.50.34 — LA FURIA: la fase final es MÁS RÁPIDA en
        /// todo (nado, lunge, ram, telegraphs y el aliento doble).</summary>
        private bool Furia => Phase >= 5;

        // === LA FASE DE SIEMPRE (los umbrales de la casa) ===
        private int Phase = 1;
        private int _tickEspiral = 0;
        private int _tickNube = 0;
        private int _tickGravedad = 0;
        private int _tickPerno = 0;
        private int _cicloAgujero = 0;
        private bool _agujeroOn = false;
        private Vector2 _posAgujero = Vector2.Zero;
        private int _tickJets = 0;
        private int _tickRunas = 0;

        // === EL ARCO DE SUPERFICIE ===
        private float _angArco = -MathHelper.PiOver2;
        private float _recorridoArco = 0f;

        // === EL CINE DE MUERTE ===
        private bool _muriendo = false;
        private int _tickMuerte = 0;
        private bool _yaDropeo = false;

        // === LA PALETA DE LA LUZ PRIMORDIAL (de siempre) ===
        private static readonly Color OroLuz = new(255, 240, 190);
        private static readonly Color VioletaLuz = new(196, 150, 255);
        private static readonly Color NucleoBlanco = new(255, 252, 240);
        private static readonly Color NebulosaVioleta = new(170, 110, 240);

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 168;     // v6.50.34 — el cráneo a ESC 1.85 (la Señora del Mundo)
            NPC.height = 168;
            NPC.damage = 95;     // EL MORDISCO (la boca es más grande)
            NPC.defense = 40;
            NPC.lifeMax = 2_400_000;
            NPC.HitSound = SoundID.NPCHit2;   // hueso
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;   // la sierpe NADA por la tierra
            NPC.behindTiles = true;     // EL TRUCO: el terreno la tapa
            NPC.boss = true;
            NPC.npcSlots = 30f;
            NPC.aiStyle = -1;
            NPC.netAlways = true;
            Music = MusicID.Boss5;
            SceneEffectPriority = SceneEffectPriority.BossHigh;
        }

        // ==================================================================
        //  LA IA — LA SIERPE
        // ==================================================================
        public override void AI()
        {
            // === EL CINE DE MUERTE (CheckDead manda aquí) ===
            if (_muriendo) { CineMuerte(); return; }

            // === LA PRESA (el despawn limpio de la casa) ===
            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                NPC.TargetClosest(false);
                target = Main.player[NPC.target];
                if (!target.active || target.dead)
                {
                    MatarCadena();
                    NPC.life = 0;
                    NPC.active = false;
                    return;
                }
            }

            // v6.50.34 — EL DETECTOR DE PRESA QUIETA (el anti-camping):
            // 1,5 s sin moverse y la paciencia de la emboscada SE DERRITE
            // (el acecho castiga al que planta bandera — el reporte:
            // «mejora su IA»).
            if (target.velocity.LengthSquared() < 0.36f) _ticksPresaQuieta++;
            else _ticksPresaQuieta = 0;

            // === LA PRESENTACIÓN: reposicionar BAJO TIERRA (primer tick) ===
            if (!_cadenaCreada)
            {
                // nace PROFUNDA y LEJOS: la sierpe que llega de debajo del mundo.
                int lado = target.Center.X < NPC.Center.X ? 1 : -1;
                NPC.Center = target.Center + new Vector2(lado * 560f, 900f);
                NPC.velocity = Vector2.Zero;
                NPC.alpha = 255;          // se materializa al subir
                _bajoTierra = true;
                CrearCadena();
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Presentacion", OroLuz);
            }

            // === LA FASE POR VIDA (los umbrales de siempre, histéresis) ===
            float hpPct = (float)NPC.life / NPC.lifeMax;
            int newPhase = 1;
            if (hpPct < 0.8f) newPhase = 2;
            if (hpPct < 0.6f) newPhase = 3;
            if (hpPct < 0.4f) newPhase = 4;
            if (hpPct < 0.2f) newPhase = 5;
            if (newPhase > Phase)
            {
                Phase = newPhase;
                OnPhaseChange();
            }

            // === LA ORIENTACIÓN (el cráneo mira a donde nada) ===
            if (NPC.velocity.LengthSquared() > 0.5f)
                NPC.rotation = NPC.velocity.ToRotation() + MathHelper.PiOver2;

            // === EL FADE DE NACIMIENTO ===
            if (NPC.alpha > 0) NPC.alpha = Math.Max(0, NPC.alpha - 2);

            // === EL DETECTOR DE SUPERFICIE (el chapoteo de la casa) ===
            bool solido = WorldGen.SolidTile((int)(NPC.Center.X / 16f), (int)(NPC.Center.Y / 16f));
            if (_bajoTierra && !solido) Chapoteo(emergiendo: true);
            else if (!_bajoTierra && solido) Chapoteo(emergiendo: false);
            _bajoTierra = solido;

            // === EL MOTOR DE ESTADOS ===
            _tickEstado++;
            switch (_estado)
            {
                case EST_NACIENDO: EstadoNaciendo(target); break;
                case EST_BAJO_TIERRA: EstadoBajoTierra(target); break;
                case EST_EMERGIENDO: EstadoEmergiendo(target); break;
                case EST_SUPERFICIE: EstadoSuperficie(target); break;
                case EST_HUNDIENDO: EstadoHundiendose(target); break;
                case EST_CARGA: EstadoCarga(target); break;
                case EST_CLAVADO: EstadoClavado(target); break;
            }

            // === v6.50.26 — EL ALIENTO PRIMORDIAL (el rayo de la boca) ===
            AlientoTick(target);

            // === LOS ATAQUES DE FASE (nacen TODOS de la BOCA) ===
            switch (Phase)
            {
                case 1: Fase1PolvoEstelar(); break;
                case 2: Fase2Nebulosa(target); break;
                case 3: Fase3Gravedad(target); break;
                case 4: Fase4AgujeroNegro(target); break;
                case 5: Fase5Reconocimiento(target); break;
            }

            // LA LUZ del cráneo (la Luz Primordial vive en las cuencas).
            Lighting.AddLight(NPC.Center, new Vector3(0.6f, 0.4f, 0.8f));

            // v6.50.26 — EL ESTADO VIAJA en ai[0], el ALIENTO en ai[1] y
            // EL TICK DEL ESTADO en ai[2] (los clientes dibujan el telegraph
            // del ram, la estela, el arco del aliento y la silueta de
            // llegada con este estado — la casa: el render lee lo
            // SINCRONIZADO, jamás lógica local sin sincronizar).
            NPC.ai[0] = _estado;
            NPC.ai[1] = _aliento;
            NPC.ai[2] = _tickEstado;
            // v6.50.34 — ai[3] (libre en la cabeza): la DURACIÓN del
            // telegraph del ram (36 el primero de la cadena, 20 los
            // encadenados, −30% en la furia) — el cliente dibuja el anillo
            // de aviso CON el tempo real, no con un 36 clavado.
            NPC.ai[3] = _telegraphCarga;

            // MP: la sierpe respira por el cable cada 12 ticks.
            if ((Main.GameUpdateCount % 12u) == 0u) NPC.netUpdate = true;
        }

        // ==================================================================
        //  LA CADENA — 26 vértebras de mundo + 12 del fondo + la cola
        // ==================================================================
        private void CrearCadena()
        {
            _cadenaCreada = true;
            if (Main.netMode == NetmodeID.MultiplayerClient) return; // solo la autoridad

            int prev = NPC.whoAmI;
            int tipoB = ModContent.NPCType<AethonSierpeCuerpo>();
            for (int i = 0; i < AethonSierpeCuerpo.TOTAL_VERTEBRAS; i++)
            {
                int y = (int)(NPC.Center.Y + AethonSierpeCuerpo.HUECO * (i + 1));
                // v6.50.26 — la cadena es MÁS LARGA (46 × 64 ≈ 2.950 px):
                // clampear al fondo del mundo (una invocación a las puertas
                // del infierno no puede parir huesos en el vacío).
                y = Math.Min(y, (Main.maxTilesY - 60) * 16);
                int idx = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, y, tipoB, NPC.whoAmI);
                NPC s = Main.npc[idx];
                s.realLife = NPC.whoAmI;    // la vida compartida
                s.ai[1] = prev;             // a quién sigo
                s.ai[2] = NPC.whoAmI;       // la cabeza
                s.ai[3] = i;                // mi índice
                Main.npc[prev].ai[0] = idx; // el viejo me conoce
                if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, idx);
                prev = idx;
            }
            int yT = (int)(NPC.Center.Y + AethonSierpeCuerpo.HUECO * (AethonSierpeCuerpo.TOTAL_VERTEBRAS + 1));
            yT = Math.Min(yT, (Main.maxTilesY - 60) * 16);
            int cola = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, yT,
                ModContent.NPCType<AethonSierpeCola>(), NPC.whoAmI);
            Main.npc[cola].realLife = NPC.whoAmI;
            Main.npc[cola].ai[1] = prev;
            Main.npc[cola].ai[2] = NPC.whoAmI;
            Main.npc[cola].ai[3] = AethonSierpeCuerpo.TOTAL_VERTEBRAS;
            Main.npc[prev].ai[0] = cola;
            if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, cola);
            NPC.netUpdate = true;
        }

        /// <summary>Mata TODA la cadena (despawn o final del cine).</summary>
        private void MatarCadena()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            int tipoB = ModContent.NPCType<AethonSierpeCuerpo>();
            int tipoC = ModContent.NPCType<AethonSierpeCola>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n != null && n.active && (n.type == tipoB || n.type == tipoC))
                {
                    n.active = false;
                    if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                }
            }
        }

        // ==================================================================
        //  LOS ESTADOS
        // ==================================================================

        /// <summary>LA PRESENTACIÓN: 3.3 s de nacer — la cola entra al fondo.</summary>
        private void EstadoNaciendo(Player target)
        {
            // asciende LENTO hacia su cueva de caza (el mundo la ve llegar).
            Vector2 punto = target.Center + new Vector2(MathF.Sign(NPC.Center.X - target.Center.X) * 420f, 700f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.012f, 0.10f);
            _aberturaMandibula = 0.08f; // entreabierta: está despertando
            if (_tickEstado >= 200)
            {
                _estado = EST_BAJO_TIERRA;
                _tickEstado = 0;
            }
        }

        /// <summary>LA CAZA SUBTERRÁNEA: nada BAJO la presa esperando el eje.</summary>
        private void EstadoBajoTierra(Player target)
        {
            float vel = 13f + Phase * 1.5f + (Furia ? 2f : 0f);
            Vector2 deseado = new Vector2(
                target.Center.X + target.velocity.X * 12f,
                target.Center.Y + 720f);
            Vector2 hacia = (deseado - NPC.Center).SafeNormalize(Vector2.UnitX) * vel;
            NPC.velocity = Vector2.Lerp(NPC.velocity, hacia, 0.045f);
            _aberturaMandibula = MathHelper.Lerp(_aberturaMandibula, 0.05f, 0.08f);

            // v6.50.34 — LA PACIENCIA DINÁMICA: cada fase acecha MENOS
            // (110 t en P1 → 62 en P5)… y la presa QUIETA la derrite por
            // completo (anti-camping: pararse es regalarle el lunge).
            int paciencia = Math.Max(40, 110 - (Phase - 1) * 12);
            if (_ticksPresaQuieta > 90) paciencia = Math.Min(paciencia, 24);

            // EL RUMBO de la emboscada: cerca del eje Y de la presa (X) y
            // con la paciencia contada → EL LUNGE.
            bool alineada = MathF.Abs(NPC.Center.X - target.Center.X) < 300f;
            if (alineada && _tickEstado > paciencia)
            {
                _estado = EST_EMERGIENDO;
                _tickEstado = 0;
                Vector2 pred = PredPresa(target);
                Vector2 dir = (pred - NPC.Center).SafeNormalize(Vector2.UnitY);
                NPC.velocity = dir * (24f + Phase * 2.2f + (Furia ? 2f : 0f));
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                OndaLib.Kick(6f, 12);
            }
        }

        /// <summary>
        /// v6.50.34 — LA PREDICCIÓN ADAPTATIVA: el lead de la presa ESCALA
        /// con la distancia (lejos = hasta 34 t de anticipación — apunta a
        /// donde VAS a estar; cerca = 10 t — tiro franco). La sierpe vieja
        /// lead-eaba fijo 22 t: el que corría en línea la esquivaba por
        /// pura geometría. La nueva LEE tu carrera.
        /// </summary>
        private Vector2 PredPresa(Player target)
        {
            float dist = Vector2.Distance(NPC.Center, target.Center);
            float lead = MathHelper.Clamp(dist / 35f, 10f, 34f);
            return target.Center + target.velocity * lead;
        }

        /// <summary>EL EMERGER: el lunge vertical con la boca ABIERTA.</summary>
        private void EstadoEmergiendo(Player target)
        {
            _aberturaMandibula = MathHelper.Lerp(_aberturaMandibula, 0.36f, 0.20f);
            // corrección suave hacia la presa (el lunge es honesto, no teledo).
            Vector2 pred = target.Center + target.velocity * 8f;
            Vector2 hacia = (pred - NPC.Center).SafeNormalize(Vector2.UnitY) * NPC.velocity.Length();
            NPC.velocity = Vector2.Lerp(NPC.velocity, hacia, 0.015f);

            // LA BOCA DISPARA AL EMERGER — UNA VEZ (la firma de cada fase).
            // v6.50.30 — FIX (EL ALUVIÓN DEL EMERGER): el switch viejo
            // disparaba CADA TICK del lunge (hasta 100 ticks: 700 pernos
            // en fase 1, 100 tajos+rugidos+anuncios en la 5 — la
            // avalancha de proyectiles y el grito repetido del reporte).
            // AHORA: UNA sola volleada al tick 12 (medio lunge, la boca
            // ya fuera de la tierra). La fase 5 NO dispara aquí — su
            // ElRecordar ya lo suelta Fase5Reconocimiento (1 vez, tick 3).
            if (_tickEstado == 12)
            {
                switch (Phase)
                {
                    case 1: VolleadaEspiral(); break;
                    case 2: VolleadaNebulosa(target); break;
                    case 3: VolleadaDoble(target); break;
                }
            }

            // el ápice: cuando el impulso vertical muere → el ARCO.
            if (NPC.velocity.Y > -2f || _tickEstado > 100)
            {
                _estado = EST_SUPERFICIE;
                _tickEstado = 0;
                Vector2 rel = NPC.Center - target.Center;
                _angArco = MathF.Atan2(rel.Y, rel.X);
                _recorridoArco = 0f;
            }
        }

        /// <summary>EL ARCO EN SUPERFICIE: la sierpe pasea por el aire.</summary>
        private void EstadoSuperficie(Player target)
        {
            // fase 4 con el agujero ABIERTO: vigila ANCHA alrededor del
            // colapso (el borde del festín); si no, arca sobre la presa.
            Vector2 centro = _agujeroOn ? _posAgujero : target.Center;
            float radio = _agujeroOn ? 560f : (Phase >= 5 ? 340f : 470f);
            float alto = _agujeroOn ? 300f : (Phase >= 5 ? 190f : 250f);
            float velAng = (0.024f + Phase * 0.004f) * (_agujeroOn ? 0.8f : 1f);
            // el sentido del arco: del lado que ya venía.
            int sentido = MathF.Sin(_angArco) >= 0f ? 1 : -1;

            _angArco += velAng * sentido;
            _recorridoArco += velAng;
            Vector2 punto = centro + new Vector2(MathF.Cos(_angArco) * radio,
                MathF.Sin(_angArco) * alto - 90f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.11f, 0.16f);

            // v6.50.26 — EL MORDISCO: al ENTRAR al arco las fauces cierran
            // DE UN MORDISCO (lerp rápido los primeros 25 ticks; luego el
            // reposo lento de siempre — la animación de la caza).
            float rate = _tickEstado < 25 ? 0.35f : 0.08f;
            _aberturaMandibula = MathHelper.Lerp(_aberturaMandibula, 0.10f, rate);

            // v6.50.26 — EL ALIENTO: en fase 3+, al poco de entrar al arco
            // (una vez por ciclo de superficie).
            if (Phase >= 3 && !_alientoEsteCiclo && _aliento == 0 && _tickEstado >= 30)
            {
                _alientoEsteCiclo = true;
                _aliento = 1;
                _tickAliento = 0;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item117, NPC.Center);
                NPC.netUpdate = true; // el cliente arranca el arco YA
            }
            // v6.50.34 — LA FURIA (P5): EL ALIENTO DOBLE — a mitad del
            // arco, cuando el primero ya murió, la garganta SE RECARGA y
            // escupe el segundo (la fase final no da respiro).
            else if (Furia && _alientoEsteCiclo && !_alientoDoble && _aliento == 0 && _tickEstado >= 150)
            {
                _alientoDoble = true;
                _aliento = 1;
                _tickAliento = 0;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item117, NPC.Center);
                NPC.netUpdate = true;
            }

            // media vuelta (o el respiro de 300 t) → EL ATAQUE QUE TOCA.
            if (_recorridoArco >= MathHelper.Pi || _tickEstado >= 300)
            {
                _arcos++;

                // v6.50.34 — LA ROTACIÓN (la IA nueva): fase 2+, el cierre
                // del arco SIEMPRE lanza un ataque — y NUNCA el mismo dos
                // veces seguidas: si acabo de ramear → CLAVADO desde el
                // cielo; si acabo de clavarme → RAM horizontal. La presa
                // tiene que cubrir el eje horizontal Y el vertical a la
                // vez: leerla de memoria es imposible. (P1 aprende: solo
                // el hundido de siempre.)
                if (Phase >= 2)
                {
                    if (_ultimoAtaque != 1)
                    {
                        // EL RAM HORIZONTAL (v6.50.26 — el dash del DoG).
                        _ultimoAtaque = 1;
                        _cargasEnCadena = 1;
                        _estado = EST_CARGA;
                        _tickEstado = 0;
                        _dirCarga = NPC.Center.X < target.Center.X ? 1 : -1;
                        NPC.velocity *= 0.3f;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                        EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Carga", OroLuz);
                        OndaLib.Kick(7f, 14);
                        NPC.netUpdate = true;
                        return;
                    }
                    // EL CLAVADO AÉREO (v6.50.34 — la muerte desde el cielo).
                    _ultimoAtaque = 2;
                    _estado = EST_CLAVADO;
                    _tickEstado = 0;
                    NPC.velocity *= 0.25f;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                    OndaLib.Kick(7f, 14);
                    NPC.netUpdate = true;
                    return;
                }

                _estado = EST_HUNDIENDO;
                _tickEstado = 0;
                // el clavado: tangente del arco + peso.
                Vector2 tangente = new Vector2(-MathF.Sin(_angArco), MathF.Cos(_angArco) * (alto / radio)) * sentido;
                NPC.velocity = tangente * 13f + new Vector2(0f, 19f);
            }
        }

        /// <summary>EL CLAVADO: entra a la tierra con el hombro.</summary>
        private void EstadoHundiendose(Player target)
        {
            NPC.velocity.Y += 0.28f;      // el peso del hueso
            NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.velocity.SafeNormalize(Vector2.UnitY) * 22f, 0.05f);
            _aberturaMandibula = MathHelper.Lerp(_aberturaMandibula, 0.05f, 0.10f);

            // v6.50.26 — el aliento se corta al clavarse (el arco muere).
            if (_aliento != 0) { _aliento = 0; _tickAliento = 0; NPC.netUpdate = true; }

            // FASE 4 — SE LA TRAGA: al clavarse, la singularidad nace donde
            // ESTÁS (ella se lo lleva debajo y el mundo se curva).
            if (Phase == 4 && !_agujeroOn && _tickEstado == 2)
            {
                _agujeroOn = true;
                _cicloAgujero = 0;
                _posAgujero = target.Center;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item117, _posAgujero);
                OndaLib.Kick(8f, 16);
                NPC.netUpdate = true;
            }

            if (_bajoTierra && _tickEstado > 20)
            {
                _estado = EST_BAJO_TIERRA;
                _tickEstado = 0;
                _alientoEsteCiclo = false; // el próximo arco puede volver a escupir
                _alientoDoble = false;     // v6.50.34 — y la furia recarga el doble
            }
        }

        // ==================================================================
        //  v6.50.26 — EL RAM HORIZONTAL (la embestida de superficie)
        // ==================================================================

        /// <summary>
        /// LA CARGA DEL GUSANO: telegraph (frena, ruge, la runa frontal se
        /// enciende y las fauces se ABREN al máximo) y EMBESTIDA
        /// horizontal a la altura de la presa — el dash del Devourer of
        /// Gods, la firma de los gusanos grandes. En fase 4+ el ram
        /// SIEMBRA pernos con la boca mientras cruza.
        ///
        /// v6.50.34 — LA CADENA (la firma del DoG de verdad): en fase 3+ el
        /// ram no termina al cruzar — VUELVE desde el lado opuesto (2
        /// embestidas en P3/P4, 3 EN LA FURIA), cada una con telegraph
        /// corto (la vuelta en U ES el aviso). La sierpe vieja golpeaba
        /// una vez y se iba: la nueva es un péndulo.
        /// </summary>
        private void EstadoCarga(Player target)
        {
            // === EL TELEGRAPH (0.._telegraphCarga): la sierpe se yergue y avisa ===
            if (_tickEstado <= _telegraphCarga)
            {
                NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Zero, 0.12f);
                _aberturaMandibula = MathHelper.Lerp(_aberturaMandibula, 0.52f, 0.15f);
                if (_tickEstado == 1)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                    OndaLib.Kick(7f, 14);
                }
                // se ALZA sobre la línea de la presa: el rumbo del ram.
                Vector2 ancla = new Vector2(
                    target.Center.X - _dirCarga * 340f,
                    target.Center.Y - 160f);
                NPC.velocity += (ancla - NPC.Center) * 0.004f;
                return;
            }

            // === EL RAM: horizontal a la altura de la presa ===
            float vel = 26f + Phase * 2.5f + (Furia ? 3f : 0f);
            Vector2 hacia = new Vector2(_dirCarga * vel,
                (target.Center.Y - 60f - NPC.Center.Y) * 0.05f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, hacia, 0.10f);
            _aberturaMandibula = MathHelper.Lerp(_aberturaMandibula, 0.46f, 0.12f);

            // LA ESTELA del hueso viajando (el rastro del ram).
            if (!Main.dedServ && (_tickEstado & 3) == 0)
            {
                for (int d = 0; d < 2; d++)
                {
                    int idx = Dust.NewDust(NPC.Center, 80, 80, DustID.Bone,
                        -_dirCarga * Main.rand.NextFloat(3f, 8f), Main.rand.NextFloat(-3f, 3f));
                    Main.dust[idx].noGravity = true;
                }
            }

            // FASE 4+: la boca SIEMBRA pernos mientras cruza.
            if (Phase >= 4 && (_tickEstado % 22) == 0)
                DispararPernoApuntado(target, 0.45f, Main.rand.NextFloat(-0.25f, 0.25f));

            // el cruce termina: ¿OTRA embestida o tierra?
            bool cruzo = (_dirCarga > 0 && NPC.Center.X > target.Center.X + 1500f) ||
                         (_dirCarga < 0 && NPC.Center.X < target.Center.X - 1500f);
            if (_tickEstado > 160 || cruzo)
            {
                // v6.50.34 — LA CADENA: fase 3+ y quedan embestidas → la
                // VUELTA EN U del DoG: dirección invertida, telegraph
                // corto (la curva es el aviso) y a pasar OTRA VEZ.
                int maxCadena = Furia ? 3 : 2;
                if (Phase >= 3 && _cargasEnCadena < maxCadena && cruzo)
                {
                    _cargasEnCadena++;
                    _dirCarga *= -1;
                    _tickEstado = 0;
                    _telegraphCarga = Furia ? 14 : 20;
                    NPC.velocity = new Vector2(_dirCarga * 6f, -4f); // la curva arranca
                    NPC.netUpdate = true;
                    return;
                }
                _estado = EST_HUNDIENDO;
                _tickEstado = 0;
                _telegraphCarga = Furia ? 26 : 36;   // el próximo ciclo arranca largo
                NPC.velocity = new Vector2(_dirCarga * 9f, 21f);
            }
        }

        // ==================================================================
        //  v6.50.34 — EL CLAVADO AÉREO (la muerte desde el cielo)
        // ==================================================================

        /// <summary>
        /// LA NUEVA FIRMA VERTICAL: la sierpe se YERGE sobre la presa
        /// (telegraph de 20 t — se congela, las fauces se abren al máximo)
        /// y se CLAVA a toda velocidad en la posición PREDICHA — A
        /// TRAVÉS de la presa y directo a la tierra, donde la caza
        /// continúa. El complemento del ram: el ram cubre el eje
        /// horizontal, el clavado el vertical — la ROTACIÓN los alterna
        /// y la presa no puede cubrir ambos de memoria.
        /// En fase 4+ la boca siembra pernos en la caída.
        /// </summary>
        private void EstadoClavado(Player target)
        {
            // === EL TELEGRAPH (0..20): se yerge y APUNTA ===
            if (_tickEstado <= 20)
            {
                NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Zero, 0.14f);
                _aberturaMandibula = MathHelper.Lerp(_aberturaMandibula, 0.55f, 0.18f);
                if (_tickEstado == 1)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                    OndaLib.Kick(7f, 14);
                }
                // se ALZA sobre la presa: el punto de la caída.
                Vector2 ancla = new Vector2(target.Center.X, target.Center.Y - 420f);
                NPC.velocity += (ancla - NPC.Center) * 0.010f;
                return;
            }

            // === LA CAÍDA: el rayo de hueso a la presa PREDICHA ===
            float vel = 30f + Phase * 2.5f + (Furia ? 3f : 0f);
            Vector2 hacia = (PredPresa(target) - NPC.Center).SafeNormalize(Vector2.UnitY);
            NPC.velocity = Vector2.Lerp(NPC.velocity, hacia * vel, 0.25f);
            _aberturaMandibula = MathHelper.Lerp(_aberturaMandibula, 0.50f, 0.12f);

            // LA ESTELA de la caída (el rastro vertical del hueso).
            if (!Main.dedServ && (_tickEstado & 3) == 0)
            {
                for (int d = 0; d < 2; d++)
                {
                    int idx = Dust.NewDust(NPC.Center, 90, 90, DustID.Bone,
                        Main.rand.NextFloat(-3f, 3f), -Main.rand.NextFloat(3f, 9f));
                    Main.dust[idx].noGravity = true;
                }
            }

            // FASE 4+: la boca SIEMBRA pernos mientras cae.
            if (Phase >= 4 && (_tickEstado % 20) == 0)
                DispararPernoApuntado(target, 0.45f, Main.rand.NextFloat(-0.25f, 0.25f));

            // la caída termina: atravesó la tierra → la caza sigue ABAJO.
            if (_bajoTierra && _tickEstado > 24)
            {
                _estado = EST_BAJO_TIERRA;
                _tickEstado = 0;
                _alientoEsteCiclo = false;
                _alientoDoble = false;
            }
            else if (_tickEstado > 130)
            {
                // (seguro de mundo abierto: jamás se queda colgando)
                _estado = EST_HUNDIENDO;
                _tickEstado = 0;
            }
        }

        // ==================================================================
        //  v6.50.26 — EL ALIENTO PRIMORDIAL (el rayo de la boca)
        // ==================================================================

        /// <summary>
        /// EL ALIENTO: 40 ticks de CARGA (las fauces se abren, la luz se
        /// acumula en la garganta — el cliente dibuja el crescendo) y 70
        /// de FUEGO: el ARCO de rayo BocaPos→presa arde en el render y la
        /// boca llueve pernos alrededor de la presa (el daño real — el
        /// arco es CINE en el PreDraw de cada máquina, los pernos son
        /// proyectiles sincronizados).
        /// </summary>
        private void AlientoTick(Player target)
        {
            if (_aliento == 0) return;
            _tickAliento++;

            if (_aliento == 1)
            {
                // LA CARGA: la boca se abre, la garganta se llena.
                _aberturaMandibula = MathHelper.Lerp(_aberturaMandibula, 0.55f, 0.14f);
                if (_tickAliento >= 40)
                {
                    _aliento = 2;
                    _tickAliento = 0;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, NPC.Center);
                    EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Aliento", VioletaLuz);
                    NPC.netUpdate = true; // el arco arranca YA en los clientes
                }
                return;
            }

            // EL FUEGO: la boca cháchara y llueven pernos sobre la presa.
            _aberturaMandibula = 0.46f + 0.10f * MathF.Sin(_tickAliento * 0.55f);
            if ((_tickAliento % 8) == 0 && _tickAliento <= 64)
            {
                Vector2 pos = target.Center + new Vector2(
                    Main.rand.NextFloat(-120f, 120f),
                    Main.rand.NextFloat(-260f, -80f));
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, new Vector2(0f, 7f),
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.42f), 2f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloPernoEstelar, 0f,
                        NPC.whoAmI * 97 + _tickAliento);
                }
            }
            if (_tickAliento >= 70)
            {
                _aliento = 0;
                _tickAliento = 0;
                NPC.netUpdate = true;
            }
        }

        /// <summary>EL CHAPOTEO: cruzar la piel del mundo (polvo + temblor).</summary>
        private void Chapoteo(bool emergiendo)
        {
            int n = emergiendo ? 26 : 14;
            for (int d = 0; d < n; d++)
            {
                int idx = Dust.NewDust(NPC.Center, 60, 60, DustID.Bone,
                    Main.rand.NextFloat(-7f, 7f), Main.rand.NextFloat(-11f, 3f));
                Main.dust[idx].noGravity = true;
                int idx2 = Dust.NewDust(NPC.Center, 60, 60, DustID.Smoke,
                    Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-6f, 1f));
                Main.dust[idx2].noGravity = true;
            }
            if (emergiendo)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74, NPC.Center);
                OndaLib.Kick(7f, 14);
            }
        }

        // ==================================================================
        //  LAS FASES — LOS ATAQUES SALEN DE LA CABEZA (BocaPos)
        // ==================================================================

        /// <summary>La boca abierta: donde nacen TODOS sus ataques.</summary>
        private Vector2 BocaPos()
        {
            Vector2 adelante = NPC.velocity.SafeNormalize(Vector2.UnitY);
            if (NPC.velocity == Vector2.Zero) adelante = -Vector2.UnitY.RotatedBy(NPC.rotation);
            return NPC.Center + adelante * (58f * AethonSierpeCuerpo.ESC);  // más allá de los colmillos (ESC 1.4)
        }

        private void Fase1PolvoEstelar()
        {
            // la espiral de SIEMPRE — pero en SUPERFICIE y desde la boca.
            if (_estado != EST_SUPERFICIE) return;
            _tickEspiral++;
            if (_tickEspiral >= 55)
            {
                _tickEspiral = 0;
                VolleadaEspiral();
            }
        }

        private void Fase2Nebulosa(Player target)
        {
            if (_estado != EST_SUPERFICIE && _estado != EST_EMERGIENDO) return;
            _tickNube++;
            if (_tickNube >= 110)
            {
                _tickNube = 0;
                VolleadaNebulosa(target);
            }
            _tickPerno++;
            if (_tickPerno >= 75)
            {
                _tickPerno = 0;
                DispararPernoApuntado(target, 0.6f);
            }
        }

        private void Fase3Gravedad(Player target)
        {
            if (_estado != EST_SUPERFICIE) return;
            _tickGravedad++;
            if (_tickGravedad >= 480) // 8 s — el volteo de siempre
            {
                _tickGravedad = 0;
                FlipGravity(target);
            }
            _tickPerno++;
            if (_tickPerno >= 55)
            {
                _tickPerno = 0;
                DispararPernoApuntado(target, 0.65f, -0.12f);
                DispararPernoApuntado(target, 0.65f, 0.12f);
            }
        }

        private void Fase4AgujeroNegro(Player target)
        {
            // EL CICLO de siempre: 260 t encendida, 160 de respiro.
            _cicloAgujero++;
            if (_agujeroOn && _cicloAgujero >= 260)
            {
                _agujeroOn = false;
                _cicloAgujero = 0;
                NPC.netUpdate = true;
            }

            if (_agujeroOn)
            {
                // LA ATRACCIÓN de siempre (la fuerza que decae).
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player pl = Main.player[i];
                    if (pl == null || !pl.active || pl.dead) continue;
                    Vector2 aH = _posAgujero - pl.Center;
                    float d = aH.Length();
                    if (d > 1100f || d < 40f) continue;
                    float fuerza = 0.30f * (1f - d / 1100f);
                    pl.velocity += aH.SafeNormalize(Vector2.Zero) * fuerza;
                }

                // LOS JETS DEL DISCO (de siempre).
                _tickJets++;
                if (_tickJets >= 80)
                {
                    _tickJets = 0;
                    float baseAng = Main.GlobalTimeWrappedHourly * 2.1f;
                    for (int i = 0; i < 6; i++)
                    {
                        float ang = baseAng + i * MathHelper.TwoPi / 6f;
                        Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 8f;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromAI(),
                                _posAgujero + vel * 6f, vel,
                                ModContent.ProjectileType<AtaqueJefeProjectile>(),
                                (int)(NPC.damage * 0.5f), 2f, Main.myPlayer,
                                AtaqueJefeProjectile.EstiloPernoEstelar, 0f, NPC.whoAmI * 71 + i);
                        }
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, _posAgujero);
                }
            }

            // LAS RUNAS orbitan SU CRÁNEO (de la fase 4 en adelante).
            _tickRunas++;
            if (_tickRunas >= 240 && ContarRunas() < 2)
            {
                _tickRunas = 0;
                NacerRuna();
            }
        }

        private void Fase5Reconocimiento(Player target)
        {
            // EL REBAÑO: CUATRO runas en el cráneo.
            _tickRunas++;
            if (_tickRunas >= 200 && ContarRunas() < 4)
            {
                _tickRunas = 0;
                NacerRuna();
            }

            // EL RECORDAR: al EMERGER (las fauces se abren y recuerdan).
            if (_estado == EST_EMERGIENDO && _tickEstado == 3)
                ElRecordar(target);

            _tickPerno++;
            if (_tickPerno >= 42)
            {
                _tickPerno = 0;
                DispararPernoApuntado(target, 0.6f);
            }
        }

        // ==================================================================
        //  LAS VOLLEADAS DE LA BOCA
        // ==================================================================

        private void VolleadaEspiral()
        {
            float giro = Main.GlobalTimeWrappedHourly * 1.3f;
            for (int i = 0; i < 7; i++)
            {
                float ang = giro + i * MathHelper.TwoPi / 7f;
                Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 7.5f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        BocaPos(), vel,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.55f), 2f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloPernoEstelar, 0f, NPC.whoAmI * 61 + i);
                }
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, NPC.Center);
        }

        private void VolleadaNebulosa(Player target)
        {
            for (int i = 0; i < 3; i++)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = target.Center + new Vector2(
                    MathF.Cos(ang), MathF.Sin(ang) * 0.6f) * Main.rand.NextFloat(180f, 330f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        pos, Vector2.Zero,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.45f), 2f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloNubeNebulosa, 0f, NPC.whoAmI * 67 + i);
                }
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, NPC.Center);
        }

        private void VolleadaDoble(Player target)
        {
            DispararPernoApuntado(target, 0.65f, -0.12f);
            DispararPernoApuntado(target, 0.65f, 0.12f);
        }

        private void FlipGravity(Player player)
        {
            player.AddBuff(BuffID.Gravitation, 180);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, player.Center);
            OndaLib.Kick(6f, 12);
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Gravedad", VioletaLuz);
        }

        /// <summary>EL RECORDAR (de siempre): siete pernos en abanico DESDE
        /// LA BOCA + EL GRAN TAJO sobre la presa.</summary>
        private void ElRecordar(Player target)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Recordar", OroLuz);

            Vector2 dir = (target.Center - BocaPos()).SafeNormalize(Vector2.UnitY);
            for (int i = -3; i <= 3; i++)
            {
                Vector2 vel = dir.RotatedBy(i * 0.13f) * 12f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        BocaPos(), vel,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.6f), 2f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloPernoEstelar, 0f, NPC.whoAmI * 79 + i);
                }
            }

            float angTajo = dir.ToRotation();
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    target.Center, Vector2.Zero,
                    ModContent.ProjectileType<AtaqueJefeProjectile>(),
                    (int)(NPC.damage * 0.9f), 4f, Main.myPlayer,
                    AtaqueJefeProjectile.EstiloTajoPortador, angTajo, NPC.whoAmI * 83);
            }
        }

        /// <summary>Un perno apuntado con LEAD suave — DESDE LA BOCA.</summary>
        private void DispararPernoApuntado(Player target, float mult, float desvio = 0f)
        {
            Vector2 pred = target.Center + target.velocity * 10f;
            Vector2 dir = (pred - BocaPos()).SafeNormalize(Vector2.UnitY).RotatedBy(desvio);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    BocaPos(), dir * 11f,
                    ModContent.ProjectileType<AtaqueJefeProjectile>(),
                    (int)(NPC.damage * mult), 2f, Main.myPlayer,
                    AtaqueJefeProjectile.EstiloPernoEstelar, 0f, NPC.whoAmI * 89);
            }
            // la boca se ABRE al escupir (la mandíbula cinética del DoG).
            _aberturaMandibula = MathF.Max(_aberturaMandibula, 0.30f);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, NPC.Center);
        }

        // ==================================================================
        //  LOS AUXILIARES DE BATALLA (de siempre)
        // ==================================================================

        private static int ContarRunas()
        {
            int tipo = ModContent.ProjectileType<AtaqueJefeProjectile>();
            int n = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == tipo &&
                    (int)p.ai[0] == AtaqueJefeProjectile.EstiloRunaMemorizada)
                    n++;
            }
            return n;
        }

        private void NacerRuna()
        {
            float fase = Main.rand.NextFloat(MathHelper.TwoPi);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<AtaqueJefeProjectile>(),
                    (int)(NPC.damage * 0.65f), 2f, Main.myPlayer,
                    AtaqueJefeProjectile.EstiloRunaMemorizada, fase, NPC.whoAmI * 97);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, NPC.Center);
        }

        /// <summary>EL CAMBIO DE FASE (el respiro curita de la casa).</summary>
        private void OnPhaseChange()
        {
            NPC.life = Math.Min(NPC.lifeMax, NPC.life + NPC.lifeMax / 20);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            OndaLib.Kick(10f, 20);
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Fase",
                OroLuz, Phase, PhaseName());
            NPC.netUpdate = true;
            _agujeroOn = false;
            _cicloAgujero = 0;
        }

        private string PhaseName() => Phase switch
        {
            1 => Language.GetTextValue("Mods.AethonMod.Jefe.Aethon.Nombre1"),
            2 => Language.GetTextValue("Mods.AethonMod.Jefe.Aethon.Nombre2"),
            3 => Language.GetTextValue("Mods.AethonMod.Jefe.Aethon.Nombre3"),
            4 => Language.GetTextValue("Mods.AethonMod.Jefe.Aethon.Nombre4"),
            5 => Language.GetTextValue("Mods.AethonMod.Jefe.Aethon.Nombre5"),
            _ => "?",
        };

        // ==================================================================
        //  LA MUERTE — EL CINE DE LA DESARTICULACIÓN
        // ==================================================================

        /// <summary>
        /// CheckDead → false: la sierpe NO muere por el camino normal —
        /// sube al CINE (el patrón CheckDead/DeathAnimationTimer del DoG):
        /// se yergue, la luz se le escapa, y los huesos se desarticulan
        /// UNO A UNO de la cola a la cabeza.
        /// </summary>
        public override bool CheckDead()
        {
            if (_muriendo) return false;
            _muriendo = true;
            _tickMuerte = 0;
            NPC.life = 1;
            NPC.dontTakeDamage = true;
            _estado = EST_MURIENDO;
            _aberturaMandibula = 0.42f; // la boca se queda abierta: la luz sale
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Muerte", OroLuz);
            NPC.netUpdate = true;
            return false;
        }

        private void CineMuerte()
        {
            _tickMuerte++;
            // SE YERGA: la última ascensión lenta, mirando al cielo.
            NPC.velocity = Vector2.Lerp(NPC.velocity, new Vector2(0f, -1.6f), 0.05f);
            if (NPC.velocity.LengthSquared() > 0.5f)
                NPC.rotation = NPC.velocity.ToRotation() + MathHelper.PiOver2;

            // (LA LUZ SE ESCAPA por la boca — dibujada en PreDraw:
            //  el búfer de quads de VFXCore solo vive si algo lo vuelca
            //  en el pase de render, y el cine no puede depender de
            //  otros renderizadores.)

            // LA DESARTICULACIÓN: cada 4 ticks muere UN hueso (de la cola
            // hacia la cabeza — el esqueleto se deshace por detrás). La
            // cadena es más larga (v6.50.34: 68 huesos → hasta el tick 276:
            // TODOS se desarticulan uno a uno antes del estallido).
            if ((_tickMuerte % 4u) == 0u && _tickMuerte < 276)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int tipoB = ModContent.NPCType<AethonSierpeCuerpo>();
                    int tipoC = ModContent.NPCType<AethonSierpeCola>();
                    int victima = -1; int mejorIdx = -1;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC n = Main.npc[i];
                        if (n == null || !n.active) continue;
                        if (n.type == tipoC) { victima = i; mejorIdx = 999; continue; }
                        if (n.type == tipoB && (int)n.ai[3] > mejorIdx)
                        { victima = i; mejorIdx = (int)n.ai[3]; }
                    }
                    if (victima >= 0)
                    {
                        Main.npc[victima].active = false;
                        for (int d = 0; d < 10; d++)
                        {
                            int idx = Dust.NewDust(Main.npc[victima].Center, 40, 40, DustID.Bone,
                                Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-8f, 2f));
                            Main.dust[idx].noGravity = true;
                        }
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, victima);
                    }
                }
            }

            // EL FINAL: el estallido + el botín + lo que quede de huesos.
            if (_tickMuerte >= 296)
            {
                OndaLib.Kick(14f, 30);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath2, NPC.Center);
                if (!Main.dedServ)
                {
                    for (int d = 0; d < 80; d++)
                    {
                        int idx = Dust.NewDust(NPC.Center, 120, 120, DustID.Bone,
                            Main.rand.NextFloat(-11f, 11f), Main.rand.NextFloat(-13f, 3f));
                        Main.dust[idx].noGravity = true;
                    }
                }
                DropBotin();
                MatarCadena();
                NPC.active = false;
                NPC.netUpdate = true;
            }
        }

        /// <summary>EL BOTÍN (el método de siempre, llamado por el cine).</summary>
        private void DropBotin()
        {
            if (_yaDropeo) return;
            _yaDropeo = true;
            EsenciasModSistema.SoltarEsencia(NPC);
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                ModContent.ItemType<Items.Cosmetics.FormaAscendidaItem>(), 1);

            int killerWho = NPC.lastInteraction;
            if (killerWho < 0 || killerWho >= Main.player.Length)
            {
                for (int i = 0; i < Main.player.Length; i++)
                {
                    if (Main.player[i] != null && Main.player[i].active && NPC.playerInteraction[i])
                    {
                        killerWho = i;
                        break;
                    }
                }
            }
            if (killerWho >= 0 && killerWho < Main.player.Length)
            {
                Player player = Main.player[killerWho];
                if (player != null && player.active)
                {
                    var sp = player.GetModPlayer<Players.ShardPlayer>();
                    if (sp != null)
                    {
                        sp.ResonanceShards += 250;
                        EcoRed.AnunciarAlPortador(player, "Mods.AethonMod.Jefe.Resonancia",
                            new Color(245, 196, 81), NPC.FullName, 250);
                        EcoRed.SincronizarCronica(player);
                    }
                }
            }
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Reconocimiento", OroLuz);
        }

        /// <summary>Fallback exótico (si algo mata por fuera del cine).</summary>
        public override void OnKill()
        {
            if (_yaDropeo) return;
            DropBotin();
        }

        // ==================================================================
        //  EL ARTE — v6.50.27 — LA SIERPE ESTELAR DEL FINAL
        //
        //  El reporte: «el arte del jefe se ve horrible, deberías
        //  cambiarlo por completo, algo al estilo de la sierpe en el arma
        //  La Sierpe Estelar». EL CRÁNEO SPRITE MUERE: la cabeza es
        //  AHORA el ensamblaje de CÓDIGO de AethonSierpeArte.Cabeza — el
        //  respaldo de vacío (la silueta), el CRÁNEO ORBE con su corazón
        //  blanco, LAS FAUCES EN V (los filos de luz girando con la
        //  abertura, colmillos-estrella en las puntas), LOS DOS OJOS
        //  fareando, la CRESTA dorsal y LAS CINCO CHISPAS orbitantes
        //  (la firma de La Sierpe Estelar). El cabeceo serpenteo del
        //  v6.50.26 vive (solo en el dibujo).
        //  LOS FX DE BATALLA se conservan: la garganta ardiendo en la
        //  carga del aliento, la corona de anillos, las motas, el
        //  telegraph del ram, la estela, el cine de muerte y la
        //  singularidad de la fase 4.
        // ==================================================================
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Main.dedServ) return false;

            // v6.50.11 — sonda de lote (la casa).
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                float esc = AethonSierpeCuerpo.ESC;
                float visibilidad = 1f - (NPC.alpha / 255f);
                if (_muriendo) visibilidad *= 0.5f + 0.5f * (1f - Math.Min(1f, _tickMuerte / 270f));

                // v6.50.26 — EL CABECEO SERPENTE (solo en el dibujo: la
                // física no se toca): el cráneo ondula como la columna.
                float t = Main.GlobalTimeWrappedHourly;
                float cabeceo = MathF.Sin(t * 1.15f + NPC.whoAmI) * 0.045f;

                // === 1. LA CABEZA DE LA SIERPE ESTELAR (el arte de código:
                //     2 pases propios — vacío + luz, la sonda de la casa) ===
                Vector2 pos = NPC.Center - Main.screenPosition;
                AethonSierpeArte.Cabeza(pos, NPC.rotation + cabeceo, _aberturaMandibula,
                    t, Phase, visibilidad, esc);

                // v6.50.31 — FIX (la pareja de «Excepción silenciosa» del
                // client.log: Begin-sobre-Begin de FNA, CADA frame de la
                // pelea): Cabeza() sale con el lote ABIERTO — su CerrarBatch
                // aplica el CONTRATO DE CURACIÓN y reabre vanilla — pero este
                // PreDraw asumía que seguía CERRADO (lo cerró la sonda de
                // arriba, línea ~1090). Con las fauces ABIERTAS reventaba el
                // Begin de FlushAdditive (había quads: sin early-return);
                // con las fauces CERRADAS reventaba el Begin de la corona de
                // abajo (el Flush hacía early-return sin cerrar nada). La
                // sonda de la casa cierra aquí: cero first-chance, los DOS
                // caminos quedan limpios.
                VFXCore.CerrarLoteSiAbierto();

                // === 2. LA GARGANTA ARDIENDO (la carga del aliento — el
                //     búfer de quads de VFXCore, coords de MUNDO) ===
                float alphaLuz = _muriendo ? (0.4f * (1f - Math.Min(1f, _tickMuerte / 270f))) : 1f;
                if (_aberturaMandibula > 0.15f)
                {
                    float carga = _aliento == 1 ? Math.Min(1f, _tickAliento / 40f) : 0f;
                    VFXCore.Quad(BocaPos(),
                        OroLuz * ((0.30f + 0.55f * carga) * _aberturaMandibula * 2f * alphaLuz),
                        new Vector2(26f * esc, 30f * esc) * (1f + carga * 0.5f), NPC.rotation);
                }
                VFXCore.FlushAdditive(null, false);

                // === 3. LA CORONA DE ANILLOS (la firma de la Luz — fina) ===
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    Vector2 posC = NPC.Center - Main.screenPosition;

                    // v6.50.27 — LAS MOTAS ORBITANTES (el enjambre de la
                    // Luz: tres chispas girando el cráneo — oro en fase
                    // baja, violeta en la alta).
                    Color cMota = Phase >= 3 ? VioletaLuz : OroLuz;
                    for (int m = 0; m < 3; m++)
                    {
                        float ang = t * (0.8f + m * 0.35f) + m * 2.1f;
                        float r = 86f * esc + 10f * MathF.Sin(t * 2.2f + m);
                        Vector2 mota = posC + new Vector2(MathF.Cos(ang) * r, MathF.Sin(ang) * r * 0.62f);
                        LumenLib.Bloom(spriteBatch, mota, 13f * esc, cMota,
                            (0.35f + 0.20f * MathF.Sin(t * 3f + m * 1.7f)) * alphaLuz, 2);
                    }

                    // EL CINE DE MUERTE: LA LUZ SE ESCAPA POR LA BOCA
                    // (crece con el timer — el alma abandona el hueso).
                    if (_muriendo)
                    {
                        float tm = Math.Min(1f, _tickMuerte / 270f);
                        LumenLib.Bloom(spriteBatch, posC, 40f * (1f + tm * 3f) * esc,
                            NucleoBlanco, 0.7f * (1f - tm) + 0.08f, 2);
                        LumenLib.Bloom(spriteBatch, posC, 72f * (1f + tm * 2.5f) * esc,
                            OroLuz, 0.38f * (1f - tm * 0.7f), 2);
                    }

                    Color colorA = Phase switch
                    {
                        2 => NebulosaVioleta,
                        3 => VioletaLuz,
                        4 => new Color(120, 70, 200),
                        _ => OroLuz,
                    };
                    OrbitaLib.AnilloFino(posC, 64f * esc, t * 0.8f,
                        OrbitaLib.Tint(colorA, 0.38f * alphaLuz));
                    OrbitaLib.AnilloFino(posC, 92f * esc, -t * 0.5f,
                        OrbitaLib.Tint(VioletaLuz, 0.25f * alphaLuz));

                    // v6.50.26 — EL TELEGRAPH DEL RAM: el anillo de aviso
                    // crece mientras carga (el aviso de la embestida —
                    // ai[2] es el tick DEL ESTADO y ai[3] su DURACIÓN,
                    // ambos sincronizados; v6.50.34: el telegraph de la
                    // CADENA es corto y el anillo lo SABE — nada de 36
                    // clavados).
                    if (NPC.ai[0] == EST_CARGA && !_muriendo && NPC.ai[2] <= NPC.ai[3])
                    {
                        float tTele = MathF.Max(1f, NPC.ai[3]);
                        float prog = NPC.ai[2] / tTele;
                        OndaLib.Pulse(spriteBatch, posC, prog, 200f * esc, OroLuz, 0.55f, NPC.whoAmI);
                    }

                    // v6.50.34 — EL TELEGRAPH DEL CLAVADO: el pulso violeta
                    // de la muerte desde el cielo (la sierpe se congela
                    // encima — la sombra crece antes de la caída).
                    if (NPC.ai[0] == EST_CLAVADO && !_muriendo && NPC.ai[2] <= 20f)
                    {
                        float prog = NPC.ai[2] / 20f;
                        OndaLib.Pulse(spriteBatch, posC, prog, 150f * esc, VioletaLuz, 0.55f, NPC.whoAmI + 7);
                    }

                    // v6.50.26 — LA ESTELA DEL RAM (el rastro de luz del
                    // cráneo viajando — tres fantasmas detrás de la velocidad).
                    if (NPC.ai[0] == EST_CARGA && NPC.ai[2] > NPC.ai[3] && !_muriendo &&
                        NPC.velocity.LengthSquared() > 100f)
                    {
                        Vector2 atras = -Vector2.Normalize(NPC.velocity);
                        for (int g = 1; g <= 3; g++)
                        {
                            Vector2 fantasma = posC + atras * (g * 42f * esc);
                            LumenLib.Bloom(spriteBatch, fantasma, 34f * esc,
                                OroLuz, 0.20f / g * alphaLuz, 2);
                        }
                    }

                    // v6.50.34 — LA ESTELA DEL CLAVADO (la caída vertical:
                    // fantasmas violetas sobre la línea de la muerte).
                    if (NPC.ai[0] == EST_CLAVADO && NPC.ai[2] > 20f && !_muriendo &&
                        NPC.velocity.LengthSquared() > 100f)
                    {
                        Vector2 atrasC = -Vector2.Normalize(NPC.velocity);
                        for (int g = 1; g <= 3; g++)
                        {
                            Vector2 fantasma = posC + atrasC * (g * 46f * esc);
                            LumenLib.Bloom(spriteBatch, fantasma, 30f * esc,
                                VioletaLuz, 0.22f / g * alphaLuz, 2);
                        }
                    }

                    // === FASE 3: EL AVISO PREVOLTEO (de siempre) ===
                    if (Phase >= 3 && _estado == EST_SUPERFICIE)
                    {
                        float prog = (_tickGravedad % 480f) / 480f;
                        if (prog > 0.75f)
                        {
                            OndaLib.Pulse(spriteBatch, posC, (prog - 0.75f) / 0.25f,
                                180f, VioletaLuz, 0.5f, NPC.whoAmI);
                        }
                    }

                    // === FASE 4: LA SINGULARIDAD (el disco de siempre) ===
                    if (_agujeroOn)
                    {
                        Vector2 posH = _posAgujero - Main.screenPosition;
                        for (int i = 0; i < 10; i++)
                        {
                            float ang = t * 3.2f + i * MathHelper.TwoPi / 10f;
                            float r = 58f + 14f * VFXCore.Hash01(NPC.whoAmI, i, 0);
                            Vector2 chispa = posH + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.55f) * r;
                            LumenLib.Bloom(spriteBatch, chispa, 10f, VioletaLuz, 0.75f, 2);
                        }
                        OrbitaLib.AnilloFino(posH, 62f, t * 1.6f, OrbitaLib.Tint(NucleoBlanco, 0.5f));
                        OrbitaLib.AnilloFino(posH, 84f, -t * 1.1f, OrbitaLib.Tint(VioletaLuz, 0.4f));
                        OrbitaLib.AnilloFino(posH, 46f, t * 2.2f, OrbitaLib.Tint(OroLuz, 0.3f));
                    }
                }
                finally { spriteBatch.End(); }

                // v6.50.26 — EL ALIENTO PRIMORDIAL: el ARCO de rayo de la
                // BOCA al pecho de la presa (PerlinBolt — el arco eléctrico
                // serpenteante de la casa; en la CARGA crece fino y tenue,
                // en el FUEGO arde completo). Corre en TODAS las máquinas:
                // lee ai[1] (sincronizado) y la posición LOCAL de la presa.
                // EL TEMPO LOCAL: si ai[1] no cambió desde el frame pasado,
                // el contador sigue contando (el crescendo es suave — los
                // paquetes de sync llegan cada 12 ticks, el arco cada 1).
                if (NPC.ai[1] != _alientoPrevio)
                {
                    _alientoPrevio = NPC.ai[1];
                    _tickAlientoLocal = 0f;
                }
                else if (NPC.ai[1] != 0f)
                    _tickAlientoLocal++;
                if (!_muriendo && (NPC.ai[1] == 1f || NPC.ai[1] == 2f))
                {
                    Player presa = Main.player[NPC.target];
                    if (presa != null && presa.active && !presa.dead)
                    {
                        bool cargando = NPC.ai[1] == 1f;
                        float prog = cargando
                            ? Math.Min(1f, _tickAlientoLocal / 40f)
                            : 1f;
                        Vector2 boca = BocaPos() - Main.screenPosition;
                        Vector2 pecho = presa.Center - Main.screenPosition;
                        Color haloA = Phase >= 3 ? VioletaLuz : OroLuz;
                        float ancho = (0.9f + 2.6f * prog) * (cargando ? 0.6f : 1f);
                        int flick = (int)(Main.GameUpdateCount / 3u); // ~20 Hz de meandro
                        StormLib.PerlinBolt(spriteBatch, boca, pecho,
                            NPC.whoAmI * 71 + 13, flick, ancho, haloA, NucleoBlanco,
                            (cargando ? 0.35f : 0.9f) * visibilidad);
                    }
                }
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            finally
            {
                // v6.50.11 — el lote sale SIEMPRE abierto y vanilla.
                VFXCore.ReabrirLoteVanilla();
            }
            return false; // la cabeza de código se dibuja a sí misma
        }

        // v6.50.26 — el tempo LOCAL del aliento (el cliente cuenta su
        // propio tick para el crescendo suave — ai[1] solo viaja por
        // paquetes cada 12 ticks; el arco no puede esperarlos).
        private float _tickAlientoLocal = 0f;
        private float _alientoPrevio = 0f;
    }
}
