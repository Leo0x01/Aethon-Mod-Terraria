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
    /// Eco del Primer Portador — la sombra del primer alma en comprometerse
    /// con un fragmento, rehecho de cero en v6.48.
    ///
    /// LO QUE ERA (v5): perseguía flotando y escupía un
    /// SolarWhipSword de vanilla por la boca. LO QUE ES: EL DUELISTA DE
    /// LA CASA —
    /// · ARTE 100% CÓDIGO: la silueta de brasa (capas tenues), los OJOS
    /// de rescoldo, la VAINA que late lista para el parry y LA HOJA: un
    /// creciente TajoLib VIVO en la mano que ARQUEA de verdad al golpear.
    /// · EL ARTE DE LA ESPADA (el ciclo del duelista): ACECHA tejiendo
    /// (aproximación sinusoidal — nunca en línea recta), MARCA (el
    /// telegrafo de la casa sobre tu cabeza), TAJO (la embestida a
    /// través con el corte DIFERIDO que florece DONDE ESTABAS) y
    /// RETROCESO (el paso atrás del que sabe terminar un combo).
    /// · EL PARRY (la firma del Portador): 20% de anular tu golpe con
    ///   flash de tajo — sigue ahí, ahora con el destello de TajoLib.
    /// · LAS CUCHILLAS EN ÓRBITA + EL CORTE DE REALIDAD (&lt;40%): las
    ///   cuchillas giran alrededor del duelista y se DISPARAN al
    ///   entrar a su anillo; la pared de desgarro parte el suelo.
    ///
    /// 180.000 PV, se convoca con la Sombra del Portador (de día).
    /// </summary>
    public class EchoBlade : ModNPC
    {
        // === EL CICLO DEL DUELISTA ===
        private const int FaseAcecho = 0;
        private const int FaseMarca = 1;
        private const int FaseTajo = 2;
        private const int FaseRetroceso = 3;

        private int _fase = FaseAcecho;
        private int _tickFase = 0;
        private int _combos = 0;         // cada DOS combos (furia): la pared
        private int _tickOrbit = 0;      // la guardia de cuchillas
        private EcosLib.Memoria _mem;    // la estela del eco

        private int ParryCooldown = 0;

        // === LA PALETA DE RESCOLDO DEL PORTADOR ===
        private static readonly Color EmberSilueta = new(150, 70, 40);
        private static readonly Color EmberVivo = new(255, 120, 60);
        private static readonly Color FiloBlanco = new(255, 238, 220);
        private static readonly Color Rescoldo = new(255, 170, 90);

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 30;
            NPC.height = 48;
            NPC.damage = 50;
            NPC.defense = 20;
            NPC.lifeMax = 180_000;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.02f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.npcSlots = 12f;
            NPC.aiStyle = -1;
            Music = MusicID.Boss1;
        }

        public override void AI()
        {
            if (_mem.Pos == null) _mem = EcosLib.Crear();

            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                NPC.TargetClosest(false);
                target = Main.player[NPC.target];
                if (!target.active || target.dead)
                {
                    NPC.life = 0;
                    NPC.active = false;
                    return;
                }
            }

            bool furia = (float)NPC.life / NPC.lifeMax < 0.40f;
            if (furia && NPC.localAI[0] < 1f)
            {
                NPC.localAI[0] = 1f;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                Main.NewText(Language.GetTextValue("Mods.AethonMod.Jefe.Portador.Furia"),
                    EmberVivo);
                NPC.netUpdate = true;
            }

            _tickFase++;

            switch (_fase)
            {
                // =========================================================
                //  ACECHO: teje hacia la presa (la curva de la casa —
                //  nunca en línea recta)
                // =========================================================
                case FaseAcecho:
                {
                    Vector2 aT = target.Center - NPC.Center;
                    float dist = aT.Length();
                    // v6.50 — LA CURVA DEL DUELISTA (EcosLib.CurvaAproximacion):
                    // el tejido sinusoidal a mano pasa a la librería — el
                    // vaivén de frecuencias inconmensurables sustituye al
                    // péndulo (el paso del duelo respira, no tictacea) y la
                    // aproximación se anticipa con tangente. Radio 210:
                    // apenas FUERA del filo de la marca (190) — acecha al
                    // borde exacto de tu espada.
                    Vector2 deseada = EcosLib.CurvaAproximacion(NPC.Center, target.Center,
                        210f, furia ? 8.5f : 6.5f, Main.GlobalTimeWrappedHourly,
                        NPC.whoAmI * 31);
                    NPC.velocity = Vector2.Lerp(NPC.velocity, deseada, 0.10f);

                    // ¿CERCA? → LA MARCA.
                    if (dist < 190f && _tickFase > 40)
                    {
                        _fase = FaseMarca;
                        _tickFase = 0;
                    }
                    break;
                }

                // =========================================================
                //  MARCA: 18 t de telegrafo sobre la presa (esquivable)
                // =========================================================
                case FaseMarca:
                {
                    NPC.velocity *= 0.86f; // se planta: el peso pasa a la pierna de atrás
                    if (_tickFase >= 18)
                    {
                        _fase = FaseTajo;
                        _tickFase = 0;
                        // EL TAJO DIFERIDO: florece DONDE ESTÁS (la marca te siguió).
                        float dir = (target.Center - NPC.Center).ToRotation();
                        // v6.50.1 — FIX (proyectiles ×N+1 en MP): la IA del jefe
                        // corre en server y clientes; sin gate cada máquina
                        // spawnea su copia y NewProjectile la auto-difunde →
                        // el tajo se multiplicaba ×(jugadores+1). Solo la
                        // autoridad engendra (patrón vanilla); embestida y
                        // sonido siguen en todas las pantallas.
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromAI(),
                                target.Center, Vector2.Zero,
                                ModContent.ProjectileType<AtaqueJefeProjectile>(),
                                (int)(NPC.damage * 0.85f), 3f, Main.myPlayer,
                                AtaqueJefeProjectile.EstiloTajoPortador, dir, NPC.whoAmI * 41);
                        }
                        // LA EMBESTIDA: el portador CRUZA a través.
                        NPC.velocity = new Vector2(MathF.Cos(dir), MathF.Sin(dir)) * 16f;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1, NPC.Center);
                        NPC.netUpdate = true;
                    }
                    break;
                }

                // =========================================================
                //  TAJO: el cruce (la hoja ya floreció al arrancar)
                // =========================================================
                case FaseTajo:
                {
                    NPC.velocity *= 0.97f;
                    if (_tickFase >= 14)
                    {
                        _fase = FaseRetroceso;
                        _tickFase = 0;
                        _combos++;
                    }
                    break;
                }

                // =========================================================
                //  RETROCESO: el paso atrás del que TERMINA el combo
                // =========================================================
                case FaseRetroceso:
                {
                    Vector2 lejos = (NPC.Center - target.Center).SafeNormalize(Vector2.UnitX);
                    NPC.velocity = Vector2.Lerp(NPC.velocity, lejos * 5.5f, 0.08f);
                    if (_tickFase >= 46)
                    {
                        _fase = FaseAcecho;
                        _tickFase = 0;

                        // CADA DOS COMBOS EN FURIA: la PARED DE DESGARRO.
                        if (furia && _combos % 2 == 0)
                        {
                            float cardinal = _combos % 4 * MathHelper.PiOver2;
                            // v6.50.1 — FIX (proyectiles ×N+1 en MP): gate de
                            // spawn solo en la autoridad — la pared de desgarro
                            // ya no se clona en cada cliente; el rugido sí
                            // suena en todas.
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromAI(),
                                    target.Center + new Vector2(MathF.Cos(cardinal),
                                        MathF.Sin(cardinal)) * 440f, Vector2.Zero,
                                    ModContent.ProjectileType<AtaqueJefeProjectile>(),
                                    (int)(NPC.damage * 0.6f), 3f, Main.myPlayer,
                                    AtaqueJefeProjectile.EstiloCorteRealidad,
                                    cardinal + MathHelper.Pi, NPC.whoAmI * 43);
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103, NPC.Center);
                        }
                    }
                    break;
                }
            }

            // === LAS CUCHILLAS EN ÓRBITA (la guardia del duelista en furia) ===
            if (furia)
            {
                _tickOrbit++;
                if (_tickOrbit >= 300)
                {
                    _tickOrbit = 0;
                    // v6.50.1 — FIX (proyectiles ×N+1 en MP): la guardia de
                    // cuchillas solo se engendra en la autoridad — el bucle
                    // entero queda tras el gate (antes 3 copias ×jugadores).
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            float ang = i * MathHelper.TwoPi / 3f;
                            Vector2 pos = NPC.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 104f;
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, Vector2.Zero,
                                ModContent.ProjectileType<AtaqueJefeProjectile>(),
                                (int)(NPC.damage * 0.65f), 2f, Main.myPlayer,
                                AtaqueJefeProjectile.EstiloCuchillaOrbit, ang, NPC.whoAmI * 47 + i);
                        }
                    }
                }
            }

            // EL CRONÓMETRO del destello de parry (el render solo lee).
            if (_flashParry > 0) _flashParry--;

            // LA MEMORIA (la estela del eco).
            EcosLib.Registrar(ref _mem, NPC.Center, NPC.velocity.ToRotation());

            Lighting.AddLight(NPC.Center, new Vector3(0.5f, 0.3f, 0.1f));
        }

        // ==================================================================
        //  EL PARRY DEL PORTADOR (la firma: 20% con flash de tajo)
        // ==================================================================
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            if (ParryCooldown <= 0 && Main.rand.NextBool(5))
            {
                ParryCooldown = 120;
                // Anular el daño por completo (i-frame) usando SetMaxDamage(0).
                modifiers.SetMaxDamage(0);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item37, NPC.Center);
                // EL DESTELLO DEL PARRY: un mini-tajo blanco en el punto del
                // bloqueo (lo dibuja el render con _flashParry).
                _flashParry = 16;
            }
        }
        private int _flashParry = 0;

        // ==================================================================
        //  EL ARTE DEL DUELISTA — 100% CÓDIGO
        // ==================================================================
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                bool furia = (float)NPC.life / NPC.lifeMax < 0.40f;
                Vector2 c = NPC.Center;
                float aliento = 0.88f + 0.12f * MathF.Sin(t * 2.8f);
                float mira = NPC.velocity.LengthSquared() > 0.5f
                    ? NPC.velocity.ToRotation() : NPC.direction * 0f;

                // === FASE 1 — EL CUERPO (búfer de quads, coords de MUNDO) ===

                // LA FALDA DEL ECO (el portador fue humano: la silueta se
                // deshace de la cintura abajo).
                for (int i = 0; i < 3; i++)
                {
                    float franja = (0.34f - i * 0.10f) * aliento;
                    VFXCore.Quad(c + new Vector2(0f, 14f + i * 9f),
                        EmberSilueta * franja, new Vector2(22f - i * 5f, 13f));
                }

                // EL TORSO (pecho de espadachín: hombros caídos hacia delante).
                VFXCore.Quad(c + new Vector2(0f, -5f), EmberSilueta * (0.70f * aliento),
                    new Vector2(21f, 26f));

                // LA CABEZA (la capucha del primer portador).
                VFXCore.Quad(c + new Vector2(0f, -21f), EmberSilueta * (0.80f * aliento),
                    new Vector2(15f, 14f));

                // LOS OJOS DE RESCOLDO (lo único que quedó encendido).
                VFXCore.Quad(c + new Vector2(-4f, -21f), Rescoldo * 0.95f, new Vector2(4.5f, 2.5f));
                VFXCore.Quad(c + new Vector2(4f, -21f), Rescoldo * 0.95f, new Vector2(4.5f, 2.5f));

                // EL BRAZO DE LA HOJA (extendido en la marca y el tajo).
                Vector2 hombro = c + new Vector2(0f, -9f);
                float brazoLen = _fase == FaseMarca || _fase == FaseTajo ? 22f : 12f;
                Vector2 mano = hombro + new Vector2(MathF.Cos(mira), MathF.Sin(mira)) * brazoLen;
                VFXCore.Line(hombro, mano, EmberVivo * (0.60f * aliento), 4f);

                // LA VAINA EN LA CADERA (late lista para el parry).
                float vainaLista = ParryCooldown <= 0 ? 0.85f : 0.3f;
                VFXCore.Quad(c + new Vector2(-NPC.direction * 13f, 2f),
                    Rescoldo * (0.5f * vainaLista * aliento), new Vector2(6f, 22f),
                    -NPC.direction * 0.35f);
                VFXCore.FlushAdditive(null, false);

                // === FASE 2 — LOS ADITIVOS (lote de pantalla) ===
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                Vector2 pos = c - Main.screenPosition;
                Vector2 posMano = mano - Main.screenPosition;

                // === LA HOJA VIVA (el creciente de TajoLib en la mano —
                //     pequeño en reposo, ARQUEA en el tajo) ===
                {
                    float prog = _fase == FaseTajo ? MathHelper.Clamp(_tickFase / 14f, 0f, 1f) : 0.25f;
                    float radio = 34f + 26f * prog;
                    float baseAng = mira;
                    TajoLib.Tajo(posMano, radio,
                        baseAng - 0.75f, baseAng + 0.75f,
                        prog, 0.85f, 10f,
                        EmberVivo, FiloBlanco, NPC.whoAmI * 9, t);
                }

                // LA MARCA (el telegrafo sobre la presa — la línea que se
                // afila antes del cruce).
                if (_fase == FaseMarca)
                {
                    Player presa = Main.player[NPC.target];
                    if (presa != null && presa.active)
                    {
                        OndaLib.Telegrafo(Main.spriteBatch, pos,
                            presa.Center - Main.screenPosition,
                            _tickFase / 18f, EmberVivo, 3f);
                    }
                }

                // EL DESTELLO DEL PARRY (el mini-tajo blanco del bloqueo —
                // el contador lo lleva la AI: el render solo LEE).
                if (_flashParry > 0)
                {
                    float f = _flashParry / 16f;
                    TajoLib.Tajo(pos, 52f, mira - 0.5f, mira + 0.5f,
                        1f - f * 0.4f, f, 12f, FiloBlanco, BlancoPuro, NPC.whoAmI * 5, t);
                }

                // EL ALIENTO (el pecho de brasa).
                LumenLib.Bloom(Main.spriteBatch, pos, 22f, EmberSilueta,
                    (furia ? 0.5f : 0.32f) * aliento);
                // LA VAINA lista (el punto que AVISA el parry).
                if (ParryCooldown <= 0)
                    LumenLib.BloomPulse(Main.spriteBatch,
                        pos + new Vector2(-NPC.direction * 13f, 2f), 9f, Rescoldo,
                        0.8f, t, 5f);

                // === LA ESTELA DEL ECO (los fantasmas de EcosLib — solo en
                //     la embestida: el portador se REPITE al moverse rápido) ===
                if (EcosLib.Profundidad(in _mem) > 6 && NPC.velocity.LengthSquared() > 60f)
                {
                    for (int g = 1; g <= 3; g++)
                    {
                        Vector2 pasado = EcosLib.Pasado(in _mem, g * 4);
                        if (pasado == Vector2.Zero) continue;
                        Vector2 eco = pasado - Main.screenPosition;
                        float alfa = 0.26f * (1f - g / 4f);
                        LumenLib.Bloom(Main.spriteBatch, eco + new Vector2(0f, -8f), 20f,
                            EmberSilueta, alfa, 2);
                    }
                }

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }
            finally
            {
                if (wasActive)
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
            }
            return false; // el portador SE dibuja a sí mismo (cero sprite)
        }
        private static readonly Color BlancoPuro = new(255, 255, 255);

        // ==================================================================
        //  LA MUERTE DEL PORTADOR
        // ==================================================================
        public override void OnKill()
        {
            // v6.49 — SU ALMA: la Esencia del Primer Portador (+1 nivel
            // al Grimorio; 10 por mundo — EsenciasModSistema).
            EsenciasModSistema.SoltarEsencia(NPC);

            // EL ÚLTIMO ECO: la hoja se suelta sola y cae (un tajo de
            // despedida grande, horizontal, apagándose).
            OndaLib.Kick(8f, 16);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item71, NPC.Center);
            for (int d = 0; d < 34; d++)
            {
                int idx = Dust.NewDust(NPC.Center, 30, 48, DustID.Torch,
                    Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-7f, 1f));
                Main.dust[idx].noGravity = true;
            }

            // Otorgar resonancia al jugador que mató al NPC (no a LocalPlayer — bug en MP).
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
            if (killerWho < 0 || killerWho >= Main.player.Length) return;
            Player player = Main.player[killerWho];
            if (player == null || !player.active) return;

            var sp = player.GetModPlayer<Players.ShardPlayer>();
            if (sp != null)
            {
                sp.ResonanceShards += 120;
                // v6.50.1 — FIX (anuncio invisible en MP): OnKill solo corre
                // en server/SP — el Main.NewText no llegaba a nadie en MP;
                // el anuncio ahora viaja por EcoRed al portador que mató.
                EcoRed.AnunciarAlPortador(player, "Mods.AethonMod.Jefe.Resonancia",
                    new Color(245, 196, 81), NPC.FullName, 120);
                // v6.50.1 — entrega inmediata del shard (MsgCronica, merge máximo).
                EcoRed.SincronizarCronica(player);
            }
        }
    }
}
