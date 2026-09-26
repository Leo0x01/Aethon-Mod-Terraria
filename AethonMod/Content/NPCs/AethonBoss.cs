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
    /// </summary>
    public class AethonBoss : ModNPC
    {
        // === LOS ESTADOS DE LA SIERPE ===
        private const int EST_NACIENDO = 0;     // la presentación
        private const int EST_BAJO_TIERRA = 1;  // la caza subterránea
        private const int EST_EMERGIENDO = 2;   // EL LUNGE
        private const int EST_SUPERFICIE = 3;   // el arco sobre la presa
        private const int EST_HUNDIENDO = 4;    // el clavado
        private const int EST_MURIENDO = 99;    // el cine final

        private int _estado = EST_NACIENDO;
        private int _tickEstado = 0;
        private bool _cadenaCreada = false;
        private bool _bajoTierra = true;        // el detector de superficie
        private float _aberturaMandibula = 0.06f; // las fauces cinéticas

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
            NPC.width = 92;      // el hitbox del cráneo
            NPC.height = 92;
            NPC.damage = 80;     // EL MORDISCO
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
            }

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
            float vel = 13f + Phase * 1.5f;
            Vector2 deseado = new Vector2(
                target.Center.X + target.velocity.X * 12f,
                target.Center.Y + 720f);
            Vector2 hacia = (deseado - NPC.Center).SafeNormalize(Vector2.UnitX) * vel;
            NPC.velocity = Vector2.Lerp(NPC.velocity, hacia, 0.045f);
            _aberturaMandibula = MathHelper.Lerp(_aberturaMandibula, 0.05f, 0.08f);

            // EL RUMBO de la emboscada: cerca del eje Y de la presa (X) y
            // con la paciencia contada → EL LUNGE.
            bool alineada = MathF.Abs(NPC.Center.X - target.Center.X) < 300f;
            if (alineada && _tickEstado > 110)
            {
                _estado = EST_EMERGIENDO;
                _tickEstado = 0;
                Vector2 pred = target.Center + target.velocity * 22f;
                Vector2 dir = (pred - NPC.Center).SafeNormalize(Vector2.UnitY);
                NPC.velocity = dir * (24f + Phase * 2.2f);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                OndaLib.Kick(6f, 12);
            }
        }

        /// <summary>EL EMERGER: el lunge vertical con la boca ABIERTA.</summary>
        private void EstadoEmergiendo(Player target)
        {
            _aberturaMandibula = MathHelper.Lerp(_aberturaMandibula, 0.36f, 0.20f);
            // corrección suave hacia la presa (el lunge es honesto, no teledo).
            Vector2 pred = target.Center + target.velocity * 8f;
            Vector2 hacia = (pred - NPC.Center).SafeNormalize(Vector2.UnitY) * NPC.velocity.Length();
            NPC.velocity = Vector2.Lerp(NPC.velocity, hacia, 0.015f);

            // LA BOCA DISPARA AL EMERGER (la firma de cada fase).
            switch (Phase)
            {
                case 1: VolleadaEspiral(); break;
                case 2: VolleadaNebulosa(target); break;
                case 3: VolleadaDoble(target); break;
                case 5: ElRecordar(target); break;
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
            _aberturaMandibula = MathHelper.Lerp(_aberturaMandibula, 0.10f, 0.08f);

            // media vuelta (o el respiro de 300 t) → A HUNDIRSE.
            if (_recorridoArco >= MathHelper.Pi || _tickEstado >= 300)
            {
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
            return NPC.Center + adelante * 58f;  // más allá de los colmillos
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

            // LA DESARTICULACIÓN: cada 5 ticks muere UN hueso (de la cola
            // hacia la cabeza — el esqueleto se deshace por detrás).
            if ((_tickMuerte % 5u) == 0u && _tickMuerte < 170)
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
            if (_tickMuerte >= 190)
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
        //  EL ARTE — EL CRÁNEO, LAS MANDÍBULAS CINÉTICAS Y LA LUZ
        // ==================================================================
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Main.dedServ) return false;

            // v6.50.11 — sonda de lote (la casa).
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                Texture2D texCraneo = ModContent.Request<Texture2D>(
                    "AethonMod/Content/NPCs/AethonSierpeCabeza").Value;
                Texture2D texMandibula = ModContent.Request<Texture2D>(
                    "AethonMod/Content/NPCs/AethonSierpeMandibula").Value;

                float visibilidad = 1f - (NPC.alpha / 255f);
                if (_muriendo) visibilidad *= 0.5f + 0.5f * (1f - Math.Min(1f, _tickMuerte / 190f));
                Color luz = Color.White * visibilidad;

                // === EL CRÁNEO + LAS DOS HEMIMANDÍBULAS (lote alfa) ===
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    Vector2 pos = NPC.Center - Main.screenPosition;
                    Vector2 origen = new Vector2(texCraneo.Width, texCraneo.Height) * 0.5f;
                    spriteBatch.Draw(texCraneo, pos, null, luz,
                        NPC.rotation, origen, 1f, SpriteEffects.None, 0f);

                    // LAS FAUCES: pivotes en las APÓFISIS DEL CUADRADO
                    // del cráneo (±34, +52 del centro — donde cuelgan) —
                    // giran ABRIRSE (±abertura).
                    Vector2 pivDer = NPC.Center + new Vector2(34f, 52f).RotatedBy(NPC.rotation);
                    Vector2 pivIzq = NPC.Center + new Vector2(-34f, 52f).RotatedBy(NPC.rotation);
                    Vector2 origenM = new Vector2(30f, 101f); // la bola articular
                    float ab = _aberturaMandibula;
                    spriteBatch.Draw(texMandibula, pivDer - Main.screenPosition, null, luz,
                        NPC.rotation + ab, origenM, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(texMandibula, pivIzq - Main.screenPosition, null, luz,
                        NPC.rotation - ab, origenM, 1f, SpriteEffects.FlipHorizontally, 0f);
                }
                finally { spriteBatch.End(); }

                // === LA LUZ DE LAS CUENCAS Y LA CORONA (búfer de quads) ===
                float t = Main.GlobalTimeWrappedHourly;
                float latido = 0.85f + 0.15f * MathF.Sin(t * (2.2f + Phase * 0.6f));
                float alphaLuz = _muriendo ? (0.4f * (1f - Math.Min(1f, _tickMuerte / 190f))) : 1f;

                // las CUENCAS (el oro que MIRA — sprite (±22, +6) del centro).
                for (int sx = -1; sx <= 1; sx += 2)
                {
                    Vector2 cuenca = NPC.Center + new Vector2(sx * 22f, 6f).RotatedBy(NPC.rotation);
                    VFXCore.Quad(cuenca, OroLuz * (0.65f * latido * alphaLuz * visibilidad),
                        new Vector2(16f, 16f));
                    VFXCore.Quad(cuenca, NucleoBlanco * (0.45f * latido * alphaLuz * visibilidad),
                        new Vector2(8f, 8f));
                }
                // LA GARGANTA brilla cuando la boca está ABIERTA.
                if (_aberturaMandibula > 0.15f)
                {
                    VFXCore.Quad(BocaPos(), OroLuz * (0.30f * _aberturaMandibula * 2f * alphaLuz),
                        new Vector2(26f, 30f), NPC.rotation);
                }
                VFXCore.FlushAdditive(null, false);

                // === LA CORONA DE ANILLOS (la firma de la Luz — fina) ===
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    Vector2 posC = NPC.Center - Main.screenPosition;

                    // EL CINE DE MUERTE: LA LUZ SE ESCAPA POR LA BOCA
                    // (crece con el timer — el alma abandona el hueso).
                    if (_muriendo)
                    {
                        float tm = Math.Min(1f, _tickMuerte / 190f);
                        LumenLib.Bloom(spriteBatch, posC, 40f * (1f + tm * 3f),
                            NucleoBlanco, 0.7f * (1f - tm) + 0.08f, 2);
                        LumenLib.Bloom(spriteBatch, posC, 72f * (1f + tm * 2.5f),
                            OroLuz, 0.38f * (1f - tm * 0.7f), 2);
                    }

                    Color colorA = Phase switch
                    {
                        2 => NebulosaVioleta,
                        3 => VioletaLuz,
                        4 => new Color(120, 70, 200),
                        _ => OroLuz,
                    };
                    OrbitaLib.AnilloFino(posC, 64f, t * 0.8f,
                        OrbitaLib.Tint(colorA, 0.38f * alphaLuz));
                    OrbitaLib.AnilloFino(posC, 92f, -t * 0.5f,
                        OrbitaLib.Tint(VioletaLuz, 0.25f * alphaLuz));

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
            return false; // el cráneo se dibuja a sí mismo
        }
    }
}
