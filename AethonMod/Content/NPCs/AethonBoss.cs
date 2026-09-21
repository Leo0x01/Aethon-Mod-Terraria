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
    /// Aethon, la Luz Primordial — el jefe final del mod, rehecho DE
    /// VERDAD en v6.48 (las cinco fases ya no son el mismo
    /// CultistBossLightningOrbArc recolorado).
    ///
    /// LO QUE ERA (v5): cinco fases que disparaban el MISMO orbe de
    /// vanilla con distinto número, unos adds CultistBossClone de
    /// prestado y un "TODO: drop de Forma Ascendida" cargando desde v5.
    /// LO QUE ES: LA LUZ DE LA CASA —
    ///
    /// FASE 1 · POLVO ESTELAR: la ESPIRAL de pernos estelares (abanico
    ///   giratorio con latido — Bloom pulsante) y la deriva serena.
    /// FASE 2 · NEBULOSA: las NUBES QUE QUEMAN (zonas de contacto con
    ///   flores de humo violeta que DERIVAN hacia ti) y los círculos
    ///   anchos — el cielo se mancha.
    /// FASE 3 · GRAVEDAD: el CAMBIO de gravedad cada 8 s (el buff, no la
    ///   mutación) + anillos de onda de choque + pernos apuntados.
    /// FASE 4 · AGUJERO NEGRO: EL COLAPSO — Aethon SE RECOGE (el cuerpo
    ///   se apaga), la SINGULARIDAD nace y TE ATRIBA de verdad; los
    ///   jets de acreción escupen pernos radiales desde el disco. LOS
    ///   ADDS ya no son clones de prestado: LAS RUNAS orbitan al boss.
    /// FASE 5 · RECONOCIMIENTO — LAS RUNAS MEMORIZADAS (la promesa de
    ///   v5 cumplida): Aethon empuña TUS runas contra ti — los sigilos
    ///   dorados orbitan y DISPARAN LOS PROPIOS TRUCOS DE LA CASA (tajos
    ///   diferidos y pernos), mientras la Luz danza en OCHO alrededor
    ///   tuyo y cada 6 s EL RECORDAR: la sinfonía de SIETE pernos en
    ///   abanico + EL GRAN TAJO telegrafiado que cruza la arena.
    ///
    /// EL DROP CUMPLIDO: al morir deja LA FORMA ASCENDIDA (el aura de
    /// la Luz Primordial como cosmético — el TODO de v5, pagado).
    /// Desbloqueo: el Fragmento Génesis alcanza nivel 150; convócalo
    /// con El Nombre de Aethon (de día).
    /// </summary>
    public class AethonBoss : ModNPC
    {
        // === EL ESTADO DE LA LUZ ===
        private int Phase = 1;
        private int _tickEspiral = 0;    // fase 1: el abanico giratorio
        private int _tickNube = 0;       // fase 2: la mancha del cielo
        private int _tickGravedad = 0;   // fase 3: el volteo
        private int _tickPerno = 0;      // el pulso de pernos apuntados
        private int _cicloAgujero = 0;   // fase 4: on/off de la singularidad
        private bool _agujeroOn = false;
        private Vector2 _posAgujero = Vector2.Zero;
        private int _tickJets = 0;       // fase 4: los jets del disco
        private int _tickRunas = 0;      // fase 5: el rebaño de runas
        private int _tickRecordar = 0;   // fase 5: EL RECORDAR
        private EcosLib.Memoria _mem;    // la estela de la danza

        // === LA PALETA DE LA LUZ PRIMORDIAL ===
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
            NPC.width = 120;
            NPC.height = 120;
            NPC.damage = 80;
            NPC.defense = 40;
            NPC.lifeMax = 2_400_000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.npcSlots = 30f;
            NPC.aiStyle = -1;
            Music = MusicID.Boss5;
            SceneEffectPriority = SceneEffectPriority.BossHigh;
        }

        public override void AI()
        {
            if (_mem.Pos == null) _mem = EcosLib.Crear();

            // === LA PRESA (y el despawn limpio de la casa) ===
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

            // === LA FASE POR VIDA (el umbral de la casa) ===
            float hpPct = (float)NPC.life / NPC.lifeMax;
            int newPhase = 1;
            if (hpPct < 0.8f) newPhase = 2;
            if (hpPct < 0.6f) newPhase = 3;
            if (hpPct < 0.4f) newPhase = 4;
            if (hpPct < 0.2f) newPhase = 5;
            // v6.50.1 — FIX (histéresis): SOLO SE AVANZA. OnPhaseChange
            // cura +5% del máximo → al cruzar un umbral hacia abajo la vida
            // re-basaba por encima y la fase VOLVÍA (doble flip: dos rugidos,
            // dos curas — +10% de vida por cruce y la pelea rebotaba entre
            // fases). Con newPhase > Phase la furia nunca retrocede.
            if (newPhase > Phase)
            {
                Phase = newPhase;
                OnPhaseChange();
            }

            // === EL MOVIMIENTO (cada fase baila distinto) ===
            Mover(target);

            switch (Phase)
            {
                case 1: Fase1PolvoEstelar(target); break;
                case 2: Fase2Nebulosa(target); break;
                case 3: Fase3Gravedad(target); break;
                case 4: Fase4AgujeroNegro(target); break;
                case 5: Fase5Reconocimiento(target); break;
            }

            // LA MEMORIA de la danza.
            EcosLib.Registrar(ref _mem, NPC.Center, NPC.velocity.ToRotation());

            // LA LUZ de la Luz.
            Lighting.AddLight(NPC.Center, new Vector3(0.6f, 0.4f, 0.8f));
        }

        // ==================================================================
        //  EL MOVIMIENTO — cada fase tiene SU danza
        // ==================================================================
        private void Mover(Player target)
        {
            switch (Phase)
            {
                case 1:
                {
                    // LA DERIVA SERENA: flota a 380 px sobre la presa.
                    Vector2 punto = target.Center + new Vector2(0f, -380f);
                    NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.02f, 0.06f);
                    break;
                }
                case 2:
                {
                    // LOS CÍRCULOS ANCHOS: orbita rápido a 400 px (el cielo
                    // se mancha mientras ella pasea).
                    float ang = Main.GlobalTimeWrappedHourly * 0.55f;
                    Vector2 punto = target.Center + new Vector2(
                        MathF.Cos(ang), MathF.Sin(ang) * 0.6f) * 400f - new Vector2(0f, 120f);
                    NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.03f, 0.08f);
                    break;
                }
                case 3:
                {
                    // v6.50 — EL ACECHO LENTO, CON CURVA (EcosLib.
                    // CurvaAproximacion): la persecución recta pasa a la
                    // librería — la gravedad sigue haciendo el trabajo, pero
                    // ahora ella ESCONDE el rumbo: aproximación anticipada
                    // + strafe inconmensurable a 300 px (esquivarla exige
                    // leerla, no solo correr).
                    Vector2 deseada = EcosLib.CurvaAproximacion(NPC.Center, target.Center,
                        300f, 5.5f, Main.GlobalTimeWrappedHourly, NPC.whoAmI * 53);
                    NPC.velocity = Vector2.Lerp(NPC.velocity, deseada, 0.05f);
                    break;
                }
                case 4:
                {
                    if (_agujeroOn)
                    {
                        // EL COLAPSO: se queda QUIETA a 520 px (la singularidad
                        // trabaja; ella solo vigila desde el borde).
                        Vector2 aT = _posAgujero - NPC.Center;
                        if (aT.Length() > 520f || aT.Length() < 420f)
                            NPC.velocity = Vector2.Lerp(NPC.velocity,
                                aT.SafeNormalize(Vector2.UnitX) * 5f, 0.05f);
                        else NPC.velocity *= 0.94f;
                    }
                    else
                    {
                        // EL RESPIRO: deriva sobre la presa (el ciclo vuelve).
                        Vector2 punto = target.Center + new Vector2(0f, -340f);
                        NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.02f, 0.06f);
                    }
                    break;
                }
                case 5:
                {
                    // EL OCHO DE LA DANZA FINAL: lemniscata alrededor de la
                    // presa (agresiva, acelerando — el reconocimiento te
                    // mira de frente y no deja de moverse).
                    float t = Main.GlobalTimeWrappedHourly * 1.35f;
                    Vector2 ocho = new Vector2(MathF.Sin(t), MathF.Sin(t) * MathF.Cos(t)) * 330f;
                    Vector2 punto = target.Center + ocho - new Vector2(0f, 90f);
                    NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.05f, 0.12f);
                    break;
                }
            }
        }

        // ====================================================================
        //  FASE 1 — POLVO ESTELAR: la espiral de pernos
        // ====================================================================
        private void Fase1PolvoEstelar(Player target)
        {
            _tickEspiral++;
            if (_tickEspiral >= 55)
            {
                _tickEspiral = 0;
                // LA ESPIRAL: abanico de 7 girando (el polvo que la Luz expele).
                float giro = Main.GlobalTimeWrappedHourly * 1.3f;
                for (int i = 0; i < 7; i++)
                {
                    float ang = giro + i * MathHelper.TwoPi / 7f;
                    Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 7.5f;
                    // v6.50.1 — FIX (MP ×N+1): la IA del NPC corre en server
                    // Y clientes — sin gate cada máquina spawnnea su copia y
                    // NewProjectile la auto-difunde. Solo la autoridad spawnnea.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(),
                            NPC.Center, vel,
                            ModContent.ProjectileType<AtaqueJefeProjectile>(),
                            (int)(NPC.damage * 0.55f), 2f, Main.myPlayer,
                            AtaqueJefeProjectile.EstiloPernoEstelar, 0f, NPC.whoAmI * 61 + i);
                    }
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, NPC.Center);
            }
        }

        // ====================================================================
        //  FASE 2 — NEBULOSA: las nubes que queman
        // ====================================================================
        private void Fase2Nebulosa(Player target)
        {
            _tickNube++;
            if (_tickNube >= 110)
            {
                _tickNube = 0;
                // LA MANCHA: 3 nubes alrededor de la presa (derivan hacia ti
                // solas — el cielo se acerca).
                for (int i = 0; i < 3; i++)
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 pos = target.Center + new Vector2(
                        MathF.Cos(ang), MathF.Sin(ang) * 0.6f) * Main.rand.NextFloat(180f, 330f);
                    // v6.50.1 — FIX (MP ×N+1): solo la autoridad spawnnea
                    // (NewProjectile auto-difunde — sin gate, ×jugadores+1).
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

            // EL POLVO DE FONDO: pernos sueltos apuntados.
            _tickPerno++;
            if (_tickPerno >= 75)
            {
                _tickPerno = 0;
                DispararPernoApuntado(target, 0.6f);
            }
        }

        // ====================================================================
        //  FASE 3 — GRAVEDAD: el volteo y las ondas
        // ====================================================================
        private void Fase3Gravedad(Player target)
        {
            _tickGravedad++;
            if (_tickGravedad >= 480) // 8 segundos
            {
                _tickGravedad = 0;
                FlipGravity(target);
            }

            _tickPerno++;
            if (_tickPerno >= 55)
            {
                _tickPerno = 0;
                // EL DOBLE APUNTADO: dos pernos convergentes.
                DispararPernoApuntado(target, 0.65f, -0.12f);
                DispararPernoApuntado(target, 0.65f, 0.12f);
            }
        }

        private void FlipGravity(Player player)
        {
            // Invertir la gravedad por 3 s con el BUFF (no la mutación
            // permanente — el bug de v5 corregido y conservado).
            player.AddBuff(BuffID.Gravitation, 180);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, player.Center);
            OndaLib.Kick(6f, 12);
            Main.NewText(Language.GetTextValue("Mods.AethonMod.Jefe.Aethon.Gravedad"),
                VioletaLuz);
        }

        // ====================================================================
        //  FASE 4 — AGUJERO NEGRO: EL COLAPSO (la singularidad de verdad)
        // ====================================================================
        private void Fase4AgujeroNegro(Player target)
        {
            // EL CICLO: 260 t encendida, 160 t de respiro.
            _cicloAgujero++;
            if (!_agujeroOn && _cicloAgujero >= 160)
            {
                _agujeroOn = true;
                _cicloAgujero = 0;
                // NACE donde ESTÁS (te atrae desde tu propio sitio).
                _posAgujero = target.Center;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item117, _posAgujero);
                OndaLib.Kick(8f, 16);
                NPC.netUpdate = true;
            }
            else if (_agujeroOn && _cicloAgujero >= 260)
            {
                _agujeroOn = false;
                _cicloAgujero = 0;
            }

            if (_agujeroOn)
            {
                // LA ATRACCIÓN: tira de TODAS las presas vivas cerca (la
                // fuerza decae con la distancia — cerca es fatal, lejos es
                // una brisa que molesta).
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

                // LOS JETS DE ACRECIÓN: pernos radiales desde el disco.
                _tickJets++;
                if (_tickJets >= 80)
                {
                    _tickJets = 0;
                    float baseAng = Main.GlobalTimeWrappedHourly * 2.1f;
                    for (int i = 0; i < 6; i++)
                    {
                        float ang = baseAng + i * MathHelper.TwoPi / 6f;
                        Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 8f;
                        // v6.50.1 — FIX (MP ×N+1): solo la autoridad spawnnea
                        // (NewProjectile auto-difunde — sin gate, ×jugadores+1).
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

            // LAS RUNAS (los adds DE LA CASA — ya no clones de prestado):
            // dos runas orbitan al boss desde la fase 4.
            _tickRunas++;
            if (_tickRunas >= 240 && ContarRunas() < 2)
            {
                _tickRunas = 0;
                NacerRuna();
            }
        }

        // ====================================================================
        //  FASE 5 — RECONOCIMIENTO: LAS RUNAS MEMORIZADAS (la promesa v5)
        // ====================================================================
        private void Fase5Reconocimiento(Player target)
        {
            // EL REBAÑO: mantiene CUATRO runas vivas orbitando a Aethon.
            // Cada runa dispara TUS propios trucos (tajos y pernos de la
            // casa — Aethon las memorizó de ti).
            _tickRunas++;
            if (_tickRunas >= 200 && ContarRunas() < 4)
            {
                _tickRunas = 0;
                NacerRuna();
            }

            // EL RECORDAR (cada 360 t): la sinfonía de SIETE pernos en
            // abanico + EL GRAN TAJO telegrafiado que cruza la arena.
            _tickRecordar++;
            if (_tickRecordar >= 360)
            {
                _tickRecordar = 0;
                ElRecordar(target);
            }

            // EL PULSO CONTINUO: pernos apuntados rápidos (la Luz ya no
            // tiene paciencia).
            _tickPerno++;
            if (_tickPerno >= 42)
            {
                _tickPerno = 0;
                DispararPernoApuntado(target, 0.6f);
            }
        }

        /// <summary>
        /// EL RECORDAR: siete pernos en abanico (los siete movimientos)
        /// + EL GRAN TAJO — un corte telegrafiado de TajoLib que cruza
        /// la arena entera por donde ESTÁS. La Luz recuerda CÓMO PELEAS.
        /// </summary>
        private void ElRecordar(Player target)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            Main.NewText(Language.GetTextValue("Mods.AethonMod.Jefe.Aethon.Recordar"),
                OroLuz);

            // LOS SIETE PERNOS (el abanico de los siete movimientos).
            Vector2 dir = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
            for (int i = -3; i <= 3; i++)
            {
                Vector2 vel = dir.RotatedBy(i * 0.13f) * 12f;
                // v6.50.1 — FIX (MP ×N+1): solo la autoridad spawnnea
                // (NewProjectile auto-difunde — sin gate, ×jugadores+1).
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        NPC.Center, vel,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.6f), 2f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloPernoEstelar, 0f, NPC.whoAmI * 79 + i);
                }
            }

            // EL GRAN TAJO: el corte diferido sobre la presa.
            float angTajo = dir.ToRotation();
            // v6.50.1 — FIX (MP ×N+1): solo la autoridad spawnnea
            // (NewProjectile auto-difunde — sin gate, ×jugadores+1).
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    target.Center, Vector2.Zero,
                    ModContent.ProjectileType<AtaqueJefeProjectile>(),
                    (int)(NPC.damage * 0.9f), 4f, Main.myPlayer,
                    AtaqueJefeProjectile.EstiloTajoPortador, angTajo, NPC.whoAmI * 83);
            }
        }

        // ==================================================================
        //  LOS AUXILIARES DE BATALLA
        // ==================================================================

        /// <summary>Un perno apuntado con LEAD suave (± desvío opcional).</summary>
        private void DispararPernoApuntado(Player target, float mult, float desvio = 0f)
        {
            Vector2 pred = target.Center + target.velocity * 10f;
            Vector2 dir = (pred - NPC.Center).SafeNormalize(Vector2.UnitY).RotatedBy(desvio);
            // v6.50.1 — FIX (MP ×N+1): solo la autoridad spawnnea
            // (NewProjectile auto-difunde — sin gate, ×jugadores+1).
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    NPC.Center, dir * 11f,
                    ModContent.ProjectileType<AtaqueJefeProjectile>(),
                    (int)(NPC.damage * mult), 2f, Main.myPlayer,
                    AtaqueJefeProjectile.EstiloPernoEstelar, 0f, NPC.whoAmI * 89);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, NPC.Center);
        }

        /// <summary>¿Cuántas runas memorizadas viven ahora?</summary>
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

        /// <summary>Nace UNA RUNA memorizada orbitando a Aethon.</summary>
        private void NacerRuna()
        {
            float fase = Main.rand.NextFloat(MathHelper.TwoPi);
            // v6.50.1 — FIX (MP ×N+1): solo la autoridad spawnnea
            // (NewProjectile auto-difunde — sin gate, ×jugadores+1).
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

        /// <summary>EL CAMBIO DE FASE: el respiro curita + el anuncio.</summary>
        private void OnPhaseChange()
        {
            // Curita de pausa (mecánica de respiro de la casa).
            NPC.life = Math.Min(NPC.lifeMax, NPC.life + NPC.lifeMax / 20);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            OndaLib.Kick(10f, 20);
            Main.NewText(Language.GetTextValue("Mods.AethonMod.Jefe.Aethon.Fase",
                Phase, PhaseName()), OroLuz);
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
        //  EL ARTE DE LA LUZ — 100% CÓDIGO (corona de anillos + rayos)
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
                Vector2 c = NPC.Center;

                // EL ALIENTO DE LA LUZ (late — más fuerte cada fase).
                float latido = 0.85f + 0.15f * MathF.Sin(t * (2.2f + Phase * 0.6f));

                // EL COLAPSO: en el agujero el cuerpo se APAga (la
                // singularidad se robó la luz).
                float alphaCuerpo = _agujeroOn ? 0.35f : 1f;

                // === FASE 1 — EL CUERPO (búfer de quads, coords de MUNDO) ===

                // EL NÚCLEO (el corazón de la Luz — crece con la fase).
                float nucleoR = 20f + Phase * 3.5f;
                VFXCore.Quad(c, NucleoBlanco * (0.95f * latido * alphaCuerpo),
                    new Vector2(nucleoR, nucleoR) * latido);
                VFXCore.Quad(c, OroLuz * (0.55f * latido * alphaCuerpo),
                    new Vector2(nucleoR * 1.7f, nucleoR * 1.7f) * latido);

                // LOS PÉTALOS DE LUZ (8 rayos girando — los brazos de la estrella).
                for (int i = 0; i < 8; i++)
                {
                    float ang = t * 0.45f + i * MathHelper.PiOver4;
                    float largo = (46f + Phase * 5f) *
                        (0.8f + 0.2f * MathF.Sin(t * 2.4f + i * 1.3f));
                    Vector2 pétalo = c + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                        (nucleoR + largo * 0.5f);
                    VFXCore.Quad(pétalo,
                        (Phase == 2 ? NebulosaVioleta : OroLuz) * (0.5f * latido * alphaCuerpo),
                        new Vector2(largo, 9f), ang);
                }
                VFXCore.FlushAdditive(null, false);

                // === FASE 2 — LOS ADITIVOS (lote de pantalla) ===
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                Vector2 pos = c - Main.screenPosition;

                // LA CORONA: TRES anillos contra-rotando (la firma de la Luz).
                Color colorA = Phase switch
                {
                    2 => NebulosaVioleta,
                    3 => VioletaLuz,
                    4 => new Color(120, 70, 200),
                    _ => OroLuz,
                };
                OrbitaLib.AnilloFino(pos, 58f, t * 0.8f,
                    OrbitaLib.Tint(colorA, 0.45f * alphaCuerpo));
                OrbitaLib.AnilloFino(pos, 82f, -t * 0.55f,
                    OrbitaLib.Tint(VioletaLuz, 0.32f * alphaCuerpo));
                OrbitaLib.AnilloFino(pos, 108f, t * 0.33f,
                    OrbitaLib.Tint(NucleoBlanco, 0.20f * alphaCuerpo));

                // EL NÚCLEO EN BLOOM (la Luz no se mira directo).
                LumenLib.BloomPulse(Main.spriteBatch, pos, 42f * latido,
                    NucleoBlanco, (0.9f * alphaCuerpo) * latido, t, 1.6f + Phase * 0.35f);

                // === FASE 3: LAS ONDAS DEL VOTEO (anillos expansivos) ===
                if (Phase >= 3)
                {
                    float prog = (_tickGravedad % 480f) / 480f;
                    if (prog > 0.75f)
                    {
                        // El aviso PREVOLTEO (la carga antes del tirón).
                        OndaLib.Pulse(Main.spriteBatch, pos, (prog - 0.75f) / 0.25f,
                            180f, VioletaLuz, 0.5f, NPC.whoAmI);
                    }
                }

                // === FASE 4: LA SINGULARIDAD (el agujero de verdad) ===
                if (_agujeroOn)
                {
                    Vector2 posH = _posAgujero - Main.screenPosition;
                    // EL DISCO DE ACRECIÓN: 10 chispas orbitando (deterministas).
                    for (int i = 0; i < 10; i++)
                    {
                        float ang = t * 3.2f + i * MathHelper.TwoPi / 10f;
                        float r = 58f + 14f * VFXCore.Hash01(NPC.whoAmI, i, 0);
                        Vector2 chispa = posH + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.55f) * r;
                        LumenLib.Bloom(Main.spriteBatch, chispa, 10f,
                            VioletaLuz, 0.75f, 2);
                    }
                    // LOS ANILLOS DEL HORIZONTE (la jaula del agujero).
                    OrbitaLib.AnilloFino(posH, 62f, t * 1.6f,
                        OrbitaLib.Tint(NucleoBlanco, 0.5f));
                    OrbitaLib.AnilloFino(posH, 84f, -t * 1.1f,
                        OrbitaLib.Tint(VioletaLuz, 0.4f));
                    OrbitaLib.AnilloFino(posH, 46f, t * 2.2f,
                        OrbitaLib.Tint(OroLuz, 0.3f));
                    // EL CENTRO VACÍO: NO se dibuja (el hueco negro ES el
                    // fondo del juego viéndose a través de la corona).
                }

                // === FASE 5: LA ESTELA DE LA DANZA (los ecos del ocho) ===
                if (Phase == 5 && EcosLib.Profundidad(in _mem) > 8)
                {
                    for (int g = 1; g <= 4; g++)
                    {
                        Vector2 pasado = EcosLib.Pasado(in _mem, g * 4);
                        if (pasado == Vector2.Zero) continue;
                        Vector2 eco = pasado - Main.screenPosition;
                        float alfa = 0.35f * (1f - g / 5f);
                        LumenLib.Bloom(Main.spriteBatch, eco, 30f, OroLuz, alfa, 2);
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
            return false; // la Luz SE dibuja a sí misma (cero sprite)
        }

        // ==================================================================
        //  LA MUERTE DE LA LUZ — EL DROP CUMPLIDO
        // ==================================================================
        public override void OnKill()
        {
            // EL APAGÓN FINAL: el kick de la despedida.
            OndaLib.Kick(14f, 30);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath1, NPC.Center);
            for (int d = 0; d < 60; d++)
            {
                int idx = Dust.NewDust(NPC.Center, 120, 120, DustID.PurpleTorch,
                    Main.rand.NextFloat(-9f, 9f), Main.rand.NextFloat(-10f, 2f));
                Main.dust[idx].noGravity = true;
            }

            // EL DROP CUMPLIDO (el TODO de v5, PAGADO): LA FORMA
            // ASCENDIDA — la propia forma de la Luz, reconocerte como un par.
            // v6.49 — Y SU ALMA: la Esencia de Aethon (+1 nivel al
            // Grimorio; 10 por mundo — el postre del mismo festín).
            EsenciasModSistema.SoltarEsencia(NPC);
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                ModContent.ItemType<Items.Cosmetics.FormaAscendidaItem>(), 1);

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
            if (killerWho >= 0 && killerWho < Main.player.Length)
            {
                Player player = Main.player[killerWho];
                if (player != null && player.active)
                {
                    var sp = player.GetModPlayer<Players.ShardPlayer>();
                    if (sp != null)
                    {
                        sp.ResonanceShards += 250;
                        // v6.50.1 — FIX (MP invisible): OnKill solo corre en
                        // server/SP → el Main.NewText de la resonancia nadie
                        // lo veía en MP. El aviso viaja por EcoRed AL PORTADOR
                        // (el asesino), como en el resto de jefes.
                        EcoRed.AnunciarAlPortador(player, "Mods.AethonMod.Jefe.Resonancia",
                            new Color(245, 196, 81), NPC.FullName, 250);
                        // v6.50.1 — entrega inmediata del shard (MsgCronica, merge máximo).
                        EcoRed.SincronizarCronica(player);
                    }
                }
            }
            // v6.50.1 — FIX (MP invisible): el reconocimiento de Aethon es
            // un anuncio del MUNDO (ChatHelper lo difunde en MP; en SP
            // NewText local como siempre — EcoRed.AnunciarMundo).
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Reconocimiento", OroLuz);
        }
    }
}
