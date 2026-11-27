using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.NPCs
{
    // ======================================================================
    //  AETHONSELLADO — v6.180 — EL DIOS SELLADO PEQUEÑO (la letra, sesión 33:
    //  «en el mundo interior en la invocacion de Aethon (Sellado) este es
    //  muy grande y lento asi que borralo completo, y crea uno nuevo que
    //  sea mas pequeño que sea del tamaño del grimorio normal, asegurate
    //  de animar correctamente su ojo y sus once ojos […] no toques nada
    //  de los sprite creado, solo anima a este jefe con la misma esencia
    //  que esta animado el grimorio del eterno y el grimorio menor
    //  ademas que sus once ojos esten rodeandolos bien animados con su
    //  parpadeo iris y demas y que esten a nivel maximo, y que saque 1
    //  sombra de la pagina para iniciar la batalla, a medida que
    //  trasncurre la pelea y pierda vida el grimorio en su modo jefe
    //  sacara mas sombra de la pagina hasta 10, al llegar a 5 sombra de
    //  la pagina entra en fase 2 y hace el ataque de Ira de un ser
    //  exterior persiguiendo lentamente al jugador, al llegar a fase 3
    //  con 10 sombras de la pagina, lanzara un segundo ira del ser
    //  exterior persiguiendo al jugador, ademas debes preparar una
    //  coreografia para rellenar el resto de la pelea»).
    //
    //  EL CUERPO: el sprite del GRIMORIO NORMAL (GrimorioHambriento —
    //  el del arma, 36×49) a escala del arma: el dios es un LIBRO de
    //  biblioteca — pequeño, rápido y vivo. Su ojo ES el del arma: el
    //  iris que MIRA y el iris rojo de la furia, con el párpado que
    //  parpadea (la esencia del Grimorio del Eterno y del Menor: el
    //  compás de siempre, el vaivén, el cabeceo).
    //
    //  LA CORONA DE LOS ONCE OJOS: once ojos LO RODEAN (una órbita
    //  viva, cada uno con SU fase de parpadeo, su iris clavado en el
    //  jugador y su giro) — los sprites del escuadrón (OjoEterno), a
    //  NIVEL MÁXIMO: la corona gira, respira y ACECHA.
    //
    //  LA BATALLA (las sombras mandan):
    //   · NACE → suelta LA PRIMERA SOMBRA DE LA PÁGINA (la letra: «que
    //     saque 1 sombra de la pagina para iniciar la batalla»).
    //   · Cada 5 % de vida perdida +1 sombra, HASTA 10 (1+⌊furia×10⌋).
    //   · 5 SOMBRAS (furia 40 %) → FASE 2: LA IRA DE UN SER EXTERIOR —
    //     un ojo colosal que la persigue LENTAMENTE al jugador.
    //   · 10 SOMBRAS (furia 90 %) → FASE 3: LA SEGUNDA IRA.
    //   · LA COREOGRAFÍA (el resto de la pelea): TRES actos que se
    //     turnan — EL VUELO DE DOS TIEMPOS (el libro mira a dónde va y
    //     luego va, escupiendo la pareja dorada/violeta), EL CORO DE
    //     LOS ONCE OJOS (los ojos se hinchan uno a uno y disparan el
    //     arsenal del Recuerdo) y EL TELETRANSPORTE EN BRUMA (el libro
    //     se exhala, desaparece y reaparece en otro flanco).
    // ======================================================================
    public class AethonSellado : ModNPC
    {
        // ═══ LOS ESTADOS ═══
        private const byte EST_NACER = 0;    // la materialización en bruma
        private const byte EST_PELEA = 1;   // la batalla de las sombras
        private const byte EST_MUERTE = 2;  // la última página

        // ═══ LA CORONA ═══
        private const int OJOS = 11;        // los once ojos (la letra)
        // v6.182 — EXACTAMENTE LOS MINIONS A NIVEL MÁXIMO (la letra:
        // «solo debes usar exactamente los minions a nivel maximo que
        // tiene el jugador con sus anillos runicos de soles y agujeros
        // negros»): la hinchazón «nivel 4» de la v6.180 MUERE — cada ojo
        // de la corona es UN MINION DEL ESCUADRÓN tal como lo ve el
        // portador: la MISMA ESCALA del Ojo Eterno (0,375 → 21×20 px) con
        // SU SELLO COMPLETO de las 4 mejoras — el círculo mágico de los
        // agujeros negros + los 2 anillos rúnicos de agujero negro
        // (dorado + violeta) + los 2 anillos rúnicos de sol (oro + azul
        // estelar) — el atuendo EXACTO de OjoEternoMinion.DrawElSello
        // con MejorasMax (la fuente de la verdad).
        private const float ESCALA_MINION = 0.375f;   // == OjoEternoMinion.ESCALA

        // ── LA PALETA DEL SELLO (medida del Umbral — la misma del minion) ──
        private static readonly Color SelloOro = new(240, 124, 65);
        private static readonly Color SelloPunta = new(255, 205, 140);

        // ═══ LA ESCALA DEL CUERPO (el tamaño del grimorio NORMAL) ═══
        private const float ESCALA = 1.30f;   // 36×49 → ~47×64 px

        // ═══ EL ARSENAL DEL CORO (el repertorio del Recuerdo) ═══
        private static readonly int[] ARSENAL = new int[]
        {
            Projectiles.Jefes.AtaqueJefeProjectile.EstiloPuaSagrario,
            Projectiles.Jefes.AtaqueJefeProjectile.EstiloCoroCristal,
            Projectiles.Jefes.AtaqueJefeProjectile.EstiloViroteVacio,
            Projectiles.Jefes.AtaqueJefeProjectile.EstiloFlechaEstelar,
            Projectiles.Jefes.AtaqueJefeProjectile.EstiloTajoPortador,
            Projectiles.Jefes.AtaqueJefeProjectile.EstiloEstallidoRadiante,
        };

        // ═══ EL ESTADO (ai) ═══
        private byte Estado => (byte)(NPC.ai[0] % 3);
        private float T => NPC.ai[1];
        private float Furia => NPC.lifeMax <= 0 ? 0f
            : MathHelper.Clamp(1f - (float)NPC.life / NPC.lifeMax, 0f, 1f);
        private int Fase => SombrasMeta >= 10 ? 3 : SombrasMeta >= 5 ? 2 : 1;

        /// <summary>Las sombras que la furia EXIGE (1 al nacer → 10 al
        /// 90 % de furia — la letra: «hasta 10»).</summary>
        private int SombrasMeta => Math.Min(10, 1 + (int)(Furia * 10f));

        // ═══ LA COREOGRAFÍA (campos vivos — viajan en la instancia) ═══
        private Vector2 _rumboElegido;        // el vuelo de dos tiempos
        private float _rumboTiempo;
        private int _cooldownPar;             // la pareja dorada/violeta
        private int _cooldownOjo;             // el coro de los once
        private int _cooldownTp;              // el teletransporte en bruma
        private byte _acto = 1;               // 0 vuelo · 1 coro · 2 bruma
        private int _actoTick;
        private int _ojoHablante;             // el ojo que dispara ahora
        private int _sombrasVivas;            // el rebaño de la página
        private int _tickRebaño;
        private bool _iraUno;                  // ¿ya lanzó la primera Ira?
        private bool _iraDos;                 // ¿y la segunda?
        private int _ojoParpadeo;             // el compás del propio ojo

        // ═══ v6.183 — LOS ANILLOS SOLARES DE LA FASE (la letra: «que
        //     por cada fase obtenga un anillo runico solar y que ataque
        //     con este anillo»): UN anillo por FASE — 1: LA RUEDA DE
        //     RAYOS · 2: LA ENERGÍA CONCENTRADA · 3: la concentrada +
        //     EL DIOS PERSIGUE a la presa unos segundos ═══
        private int _anilloCooldown = 420;       // el compás del ataque
        private byte _anilloEstado;              // 0 quieto · 1 EL ARRANQUE (girando) · 2 la servida
        private int _anilloT;                    // el reloj del ataque
        private float _anilloGiro;               // el ángulo compartido — se ACelera girando
        private int _anilloPersecucion;          // F3: los ticks de la cacería

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            // EL DIOS PEQUEÑO: el cuerpo del grimorio normal — un libro.
            NPC.width = 44;
            NPC.height = 58;
            NPC.lifeMax = 70_000;
            NPC.damage = 95;
            NPC.defense = 42;
            NPC.knockBackResist = 0f;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.aiStyle = -1;
            NPC.npcSlots = 12f;
            NPC.value = Item.buyPrice(platinum: 1);
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            if (!Main.dedServ)
                NPC.BossBar = ModContent.GetInstance<BarraAethonSellado>();
            Music = MusicID.LunarBoss;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry entry)
        {
            entry.Info.AddRange(new IBestiaryInfoElement[] {
                // v6.183 — EL GUARDIÁN DE LAS LLAVES (la letra: «hay
                // texto en crudo»): la clave pedida era «Bestario» y la
                // viva es «Bestiario» — un TIPÓGRAFO falló y el
                // bestiario pintaba la clave entera. La casa ya lo
                // vigila: tools/audit_claves_codigo_v6183.py.
                new FlavorTextBestiaryInfoElement("Mods.AethonMod.NPCs.AethonSellado.Bestiario"),
            });
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
            => Estado == EST_MUERTE ? false : base.CanBeHitByProjectile(projectile);
        public override bool? CanBeHitByItem(Player player, Item item)
            => Estado == EST_MUERTE ? false : base.CanBeHitByItem(player, item);

        // ════════════════════════════════════════════════════════════
        //  LA INVOCACIÓN (el pilar la llama — solo el interior)
        // ════════════════════════════════════════════════════════════
        public static bool Invocar(Player player)
        {
            // SOLO EL INTERIOR (la letra: «solo aparece en el mundo
            // interior a travez del pilar»)
            if (!Systems.SubmundoGrimorio.Activo) return false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n != null && n.active && n.type == ModContent.NPCType<AethonSellado>())
                {
                    Main.NewText(Language.GetTextValue("Mods.AethonMod.PilarInterior.YaDespierto"),
                        new Color(185, 110, 250));
                    return false;
                }
            }
            int idx = NPC.NewNPC(player.GetSource_FromThis(),
                (int)player.Center.X, (int)player.Center.Y - 300,
                ModContent.NPCType<AethonSellado>(), 0, 0f, 0f, 0f, player.whoAmI);
            if (idx >= 0 && idx < Main.maxNPCs)
            {
                Main.npc[idx].netUpdate = true;
                return true;
            }
            return false;
        }

        // ════════════════════════════════════════════════════════════
        //  LA VIDA DEL DIOS
        // ════════════════════════════════════════════════════════════
        public override void AI()
        {
            try
            {
                NPC.ai[1]++;
                if (Estado == EST_MUERTE) { Muerte(); return; }
                NPC.TargetClosest(true);
                Player presa = Main.player[NPC.target];
                if (presa == null || !presa.active || presa.dead)
                {
                    // sin presa: el sello se cierra y el libro se disuelve
                    NPC.ai[0] = EST_MUERTE; NPC.ai[1] = 0;
                    return;
                }

                if (Estado == EST_NACER) { Nacer(presa); return; }

                // ════════════════════════════════════════════════════
                //  EST_PELEA — LA BATALLA DE LAS SOMBRAS
                // ═══════════════════════════════════════════════════
                Pelear(presa);
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AethonSellado", exRegistro); }
        }

        // ── EL NACIMIENTO: la bruma exhala al libro y la PRIMERA sombra ──
        private void Nacer(Player presa)
        {
            NPC.velocity *= 0.90f;
            NPC.dontTakeDamage = T < 40f;
            if (T == 1f && !Main.dedServ)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122.WithPitchOffset(-0.55f), NPC.Center);
                for (int k = 0; k < 26; k++)
                    SombrasLib.Bruma(NPC.Center + new Vector2(
                        Main.rand.NextFloat(-60f, 60f), Main.rand.NextFloat(-70f, 40f)),
                        42f, 0.75f, Main.GlobalTimeWrappedHourly, k);
            }
            if (T == 70f)
            {
                // LA PRIMERA SOMBRA (la letra: «que saque 1 sombra de la
                // pagina para iniciar la batalla»)
                SoltarSombra(presa);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122.WithPitchOffset(-0.30f), NPC.Center);
                    NPC.netUpdate = true;
                }
                NPC.ai[0] = EST_PELEA; NPC.ai[1] = 0;
                NPC.dontTakeDamage = false;
            }
        }

        // ── LA PELEA: los tres actos + el rebaño + las fases ──
        private void Pelear(Player presa)
        {
            float rapidez = 7.4f + Fase * 1.4f;   // el libro PEQUEÑO vuela RÁPIDO

            // ── EL REBAÑO DE SOMBRAS (la furia manda) ──
            if (++_tickRebaño >= 60)
            {
                _tickRebaño = 0;
                ContarRebaño();
                if (_sombrasVivas < SombrasMeta && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    SoltarSombra(presa);
                    ContarRebaño();
                }
            }

            // ── LAS FASES (5 sombras → la Ira · 10 → la segunda) ──
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (!_iraUno && SombrasMeta >= 5)
                {
                    _iraUno = true;
                    LanzarLaIra(presa, 0);
                }
                if (!_iraDos && SombrasMeta >= 10)
                {
                    _iraDos = true;
                    LanzarLaIra(presa, 1);
                }
            }

            // ── v6.183 — EL ATAQUE DE LOS ANILLOS SOLARES (la letra):
            //     el compás cuenta, el anillo SE ARRANCA girando y sirve
            //     SU animación según la fase — pisa los actos cuando vive
            //     (la cacería de los TRES anillos persigue de verdad) ──
            MoverElAnillo(presa);

            // ── LOS TRES ACTOS (la coreografía) ──
            if (++_actoTick > (Fase >= 2 ? 300 : 380)) { _actoTick = 0; _acto = (byte)((_acto + 1) % 3); }

            switch (_acto)
            {
                // ══ ACTO 1 — EL VUELO DE DOS TIEMPOS (mira a dónde va
                //     y LUEGO va — la esencia del grimorio volador) ══
                case 0:
                {
                    _rumboTiempo--;
                    if (_rumboTiempo <= 0)
                    {
                        // PRIMERO MIRA: elige un flanco nuevo de la presa
                        float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                        _rumboElegido = presa.Center + ang.ToRotationVector2() * Main.rand.NextFloat(210f, 330f);
                        _rumboTiempo = 50 + Main.rand.Next(30);
                    }
                    // DESPUÉS VA (con el vaivén del flotante)
                    Vector2 dir = _rumboElegido - NPC.Center;
                    if (dir.LengthSquared() > 40f * 40f)
                        NPC.velocity = Vector2.Lerp(NPC.velocity, dir.SafeNormalize(Vector2.UnitX) * rapidez, 0.08f);
                    else
                        NPC.velocity *= 0.92f;
                    // el ESCUPIR del libro: la pareja dorada/violeta ×3
                    if (--_cooldownPar <= 0 && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        _cooldownPar = Fase >= 2 ? 46 : 66;
                        DispararLaPareja(presa);
                    }
                    break;
                }

                // ══ ACTO 2 — EL CORO DE LOS ONCE OJOS (el libro FRENA,
                //     un ojo se HINCHA y dispara el arsenal del Recuerdo) ══
                case 1:
                {
                    NPC.velocity *= 0.90f;
                    if (--_cooldownOjo <= 0 && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        _cooldownOjo = Fase >= 2 ? 42 : 60;
                        // en fase 2+ el coro canta a TRES voces
                        int voces = Fase >= 3 ? 3 : Fase >= 2 ? 2 : 1;
                        for (int v = 0; v < voces; v++)
                            DispararUnOjo(presa);
                    }
                    break;
                }

                // ══ ACTO 3 — EL TELETRANSPORTE EN BRUMA (el libro se
                //     exhala y reaparece en otro flanco) ══
                default:
                {
                    if (--_cooldownTp <= 0 && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        _cooldownTp = 90;
                        TeletransporteEnBruma(presa);
                    }
                    // mientras se reúne: deriva lenta hacia la presa
                    Vector2 acecho = presa.Center - NPC.Center;
                    if (acecho.LengthSquared() > 260f * 260f)
                        NPC.velocity = Vector2.Lerp(NPC.velocity,
                            acecho.SafeNormalize(Vector2.UnitX) * (rapidez * 0.5f), 0.05f);
                    else NPC.velocity *= 0.94f;
                    break;
                }
            }

            // EL CABECEO Y LA MIRA (la esencia del grimorio): el libro
            // se inclina hacia donde va y su ojo SIGUE a la presa.
            NPC.rotation = NPC.velocity.X * 0.010f;
            NPC.netUpdate = _actoTick % 30 == 0;
        }

        // ── LA PAREJA DORADA/VIOLETA ×3 (el básico del grimorio) ──
        private void DispararLaPareja(Player presa)
        {
            try
            {
                Vector2 rumbo = (presa.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
                int dano = (int)(NPC.damage * 0.40f);
                for (int k = -1; k <= 1; k++)
                {
                    Vector2 vel = rumbo.RotatedBy(0.26f * k) * 12.5f;
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center,
                        vel.RotatedBy(-0.26f), 931, dano, 2f, NPC.target, -1f, 0.64f, 9201f);
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center,
                        vel.RotatedBy(0.26f), 931, dano, 2f, NPC.target, -1f, 0.28f, 9202f);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122.WithPitchOffset(0.12f), NPC.Center);
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AethonSellado", exRegistro); }
        }

        // ── EL CORO: un ojo de la corona dispara el arsenal ──
        private void DispararUnOjo(Player presa)
        {
            try
            {
                _ojoHablante = (_ojoHablante + 1) % OJOS;
                Vector2 cuna = PosicionDelOjo(_ojoHablante, Main.GlobalTimeWrappedHourly);
                Vector2 rumbo = (presa.Center - cuna).SafeNormalize(Vector2.UnitX);
                int estilo = ARSENAL[Main.rand.Next(ARSENAL.Length)];
                float vel = estilo == Projectiles.Jefes.AtaqueJefeProjectile.EstiloEstallidoRadiante
                    ? 0f : 9.5f;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), cuna, rumbo * vel,
                    ModContent.ProjectileType<Projectiles.Jefes.AtaqueJefeProjectile>(),
                    (int)(NPC.damage * 0.50f), 2f, NPC.target, estilo,
                    NPC.whoAmI * 1.7f, NPC.whoAmI);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122.WithPitchOffset(0.30f), cuna);
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AethonSellado", exRegistro); }
        }

        // ── EL TELETRANSPORTE EN BRUMA (la llegada del interior) ──
        private void TeletransporteEnBruma(Player presa)
        {
            try
            {
                if (!Main.dedServ)
                {
                    // el libro SE EXHALA donde estaba…
                    for (int k = 0; k < 18; k++)
                        SombrasLib.Bruma(NPC.Center + new Vector2(
                            Main.rand.NextFloat(-30f, 30f), Main.rand.NextFloat(-40f, 30f)),
                            36f, 0.8f, Main.GlobalTimeWrappedHourly, k * 3);
                }
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 destino = presa.Center + ang.ToRotationVector2() * Main.rand.NextFloat(320f, 480f);
                destino.X = MathHelper.Clamp(destino.X, 160f, (Main.maxTilesX - 10) * 16f);
                destino.Y = MathHelper.Clamp(destino.Y, 160f, (Main.maxTilesY - 10) * 16f);
                NPC.Center = destino;
                NPC.velocity = Vector2.Zero;
                NPC.netUpdate = true;
                if (!Main.dedServ)
                {
                    // …y REAPARECE envuelto en otra nube
                    for (int k = 0; k < 18; k++)
                        SombrasLib.Bruma(NPC.Center + new Vector2(
                            Main.rand.NextFloat(-30f, 30f), Main.rand.NextFloat(-40f, 30f)),
                            36f, 0.8f, Main.GlobalTimeWrappedHourly + 0.5f, k * 5);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122.WithPitchOffset(-0.42f), NPC.Center);
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AethonSellado", exRegistro); }
        }

        // ── LAS SOMBRAS DE LA PÁGINA (el rebaño hostil) ──
        private void SoltarSombra(Player presa)
        {
            try
            {
                int idxS = Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType
                        <global::AethonMod.Content.Projectiles.Sombras.SombraPaginaProjectile>(),
                    (int)(NPC.damage * 0.55f), 3f, NPC.target, presa.whoAmI + 1);
                if (idxS >= 0)
                {
                    // el dios manda: la sombra persigue a SU presa
                    Main.projectile[idxS].friendly = false;
                    Main.projectile[idxS].hostile = true;
                }
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AethonSellado", exRegistro); }
        }

        private void ContarRebaño()
        {
            try
            {
                int tipo = ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.Sombras.SombraPaginaProjectile>();
                int vivas = 0;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p != null && p.active && p.type == tipo && p.hostile)
                        vivas++;
                }
                _sombrasVivas = vivas;
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AethonSellado", exRegistro); }
        }

        // ── LAS IRAS DE UN SER EXTERIOR (las fases 2 y 3) ──
        private void LanzarLaIra(Player presa, int numero)
        {
            try
            {
                int idx = Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<IraSelladoProjectile>(),
                    (int)(NPC.damage * 0.85f), 0f, presa.whoAmI,
                    NPC.whoAmI, numero);
                if (idx >= 0) Main.projectile[idx].timeLeft = 900;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122.WithPitchOffset(-0.7f), NPC.Center);
                Main.NewText(Language.GetTextValue(
                    "Mods.AethonMod.PilarInterior.IraSellado" + (numero + 1)),
                    new Color(226, 96, 88));
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AethonSellado", exRegistro); }
        }

        // ── v6.183 — EL ATAQUE DE LOS ANILLOS SOLARES: el compás, EL
        //    ARRANQUE (el anillo comieza a girar y se acelera — la
        //    letra) y la servida por fase ──
        private void MoverElAnillo(Player presa)
        {
            try
            {
                // LA CACERÍA DE LOS TRES ANILLOS (la letra: «al mismo
                // tiempo perseguira al jugador unos segundos»): mientras
                // vive, el dios CAZA — pisa los actos, los haces encima.
                if (_anilloPersecucion > 0)
                {
                    _anilloPersecucion--;
                    Vector2 caza = presa.Center - NPC.Center;
                    if (caza.LengthSquared() > 70f * 70f)
                        NPC.velocity = Vector2.Lerp(NPC.velocity,
                            caza.SafeNormalize(Vector2.UnitX) * 5.6f, 0.10f);
                    else NPC.velocity *= 0.93f;
                    if (!Main.dedServ && _anilloPersecucion % 9 == 0)
                    {
                        var d = Dust.NewDustPerfect(NPC.Center + new Vector2(
                            Main.rand.NextFloat(-26f, 26f), Main.rand.NextFloat(-30f, 26f)),
                            DustID.GoldFlame, new Vector2(0f, -0.4f), 150,
                            new Color(255, 196, 90), 0.9f);
                        d.noGravity = true;
                    }
                }
                switch (_anilloEstado)
                {
                    case 0:   // EL COMPÁS: cuenta y arranca
                        if (--_anilloCooldown > 0) return;
                        _anilloEstado = 1; _anilloT = 0;
                        if (!Main.dedServ)
                            Terraria.Audio.SoundEngine.PlaySound(
                                SoundID.Item122.WithPitchOffset(-0.40f), NPC.Center);
                        return;

                    case 1:   // EL ARRANQUE: el anillo comieza a girar —
                              // y SE ACelera (la animación de la letra)
                        _anilloT++;
                        _anilloGiro += 0.030f + _anilloT * _anilloT * 0.000042f;
                        NPC.velocity *= 0.92f;
                        if (_anilloT == 60)
                        {
                            ServirElAnillo(presa);
                            _anilloEstado = 2; _anilloT = 0;
                        }
                        return;

                    default:  // LA SERVIDA: los anillos siguen ardiendo
                        _anilloT++;
                        _anilloGiro += 0.085f;
                        if (_anilloT > 42)
                        {
                            _anilloEstado = 0;
                            _anilloCooldown = Fase >= 3 ? 560 : Fase >= 2 ? 640 : 720;
                        }
                        return;
                }
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AethonSellado.Anillo", exRegistro); }
        }

        /// <summary>v6.183 — LA SERVIDA DEL ANILLO por fase: F1 LA RUEDA
        /// DE RAYOS · F2/F3 LA ENERGÍA CONCENTRADA (un haz POR anillo) ·
        /// F3 además LA CACERÍA del dios.</summary>
        private void ServirElAnillo(Player presa)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            try
            {
                if (Fase >= 2)
                {
                    // LA ENERGÍA CONCENTRADA — un haz POR anillo (la
                    // letra: «cuando son 2 anillos lanzaran energia
                    // concentrada»): las TRES FUNDAS en ORO solar,
                    // ancladas al punto vivo de cada anillo.
                    for (int a = 0; a < Fase; a++)
                    {
                        int idx = Projectile.NewProjectile(NPC.GetSource_FromAI(),
                            NPC.Center, Vector2.Zero,
                            ModContent.ProjectileType<HazSolarSelladoProjectile>(),
                            (int)(NPC.damage * 0.55f), 0f, NPC.target, NPC.whoAmI, a);
                        if (idx >= 0) Main.projectile[idx].timeLeft = 200;   // R23: la vida contada
                    }
                    if (Fase >= 3)
                    {
                        // LA CACERÍA: el dios PERSIGUE unos segundos (la
                        // letra: «al mismo tiempo perseguira al jugador»)
                        _anilloPersecucion = 240;
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Roar.WithPitchOffset(-0.18f), NPC.Center);
                    }
                    else
                        Terraria.Audio.SoundEngine.PlaySound(
                            SoundID.Item122.WithPitchOffset(-0.55f), NPC.Center);
                }
                else
                {
                    // LA RUEDA DE RAYOS: el anillo gira y lanza RAYOS DE
                    // ENERGÍA (la letra) — diez filamentos radiales que
                    // cabalgan el giro vivo del anillo.
                    int rayos = 10;
                    for (int r = 0; r < rayos; r++)
                    {
                        float ang = _anilloGiro * 3f + r * (MathHelper.TwoPi / rayos);
                        int idx = Projectile.NewProjectile(NPC.GetSource_FromAI(),
                            NPC.Center, ang.ToRotationVector2() * 11.5f,
                            ModContent.ProjectileType<RayoSolarSelladoProjectile>(),
                            (int)(NPC.damage * 0.42f), 2f, NPC.target, ang);
                        if (idx >= 0) Main.projectile[idx].timeLeft = 170;   // R23
                    }
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item122.WithPitchOffset(0.25f), NPC.Center);
                }
                NPC.netUpdate = true;
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AethonSellado.Anillo", exRegistro); }
        }

        // ── LA MUERTE (la última página: los ojos se cierran y el
        //    libro se disuelve en bruma) ──
        private void Muerte()
        {
            NPC.velocity *= 0.90f;
            NPC.dontTakeDamage = true;
            NPC.damage = 0;
            if (!Main.dedServ && T % 6f == 0f)
                SombrasLib.Bruma(NPC.Center + new Vector2(
                    Main.rand.NextFloat(-40f, 40f), Main.rand.NextFloat(-50f, 30f)),
                    34f, 0.9f - T / 90f, Main.GlobalTimeWrappedHourly, (int)T);
            if (T == 8f && !Main.dedServ)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122.WithPitchOffset(-0.8f), NPC.Center);
            if (T >= 74f)
            {
                // LA IRA SE APAGA CON SU DIOS (nada queda persiguiendo)
                int tipoIra = ModContent.ProjectileType<IraSelladoProjectile>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p != null && p.active && p.type == tipoIra) p.Kill();
                }
                NPC.life = 0;
                NPC.checkDead();
            }
        }

        public override bool CheckDead()
        {
            // el EST_MUERTE es el CINE: la vida cae a 0 solo al final
            if (Estado != EST_MUERTE && NPC.life <= 0)
            {
                NPC.life = 1;
                NPC.ai[0] = EST_MUERTE;
                NPC.ai[1] = 0;
                NPC.netUpdate = true;
                return false;   // SU muerte original NUNCA arranca: primero el cine
            }
            if (Estado == EST_MUERTE) return base.CheckDead();
            return base.CheckDead();
        }

        public override bool CheckActive() => false;   // el sello lo sostiene

        // ════════════════════════════════════════════════════════════
        //  LA GEOMETRÍA DE LA CORONA (los once ojos alrededor)
        // ════════════════════════════════════════════════════════════
        /// <summary>La posición del ojo `i` de la corona (órbita viva:
        /// el anillo gira, respira y cada ojo tiene su vaivén).</summary>
        private Vector2 PosicionDelOjo(int i, float t)
        {
            float ang = t * 0.85f + i * (MathHelper.TwoPi / OJOS);
            float radio = 92f + 7f * MathF.Sin(t * 1.6f + i * 1.3f);
            return NPC.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.78f) * radio
                + new Vector2(0f, MathF.Sin(t * 1.2f + i) * 5f);
        }

        // ════════════════════════════════════════════════════════════
        //  EL DIBUJO DEL DIOS — v6.182: LA PROPORCIÓN DEL ARMA Y LOS
        //  ANILLOS RÚNICOS. (1) EL IRIS a LA ESCALA DEL ARMA
        //  (GrimorioHambriento.IRIS_ESC — el disco nacía a escala
        //  completa: 42 px sobre un libro de 47×64; el arma dibuja el
        //  SUYO a 0,2773 — AHORA el dios también). (2) EL HALO PLANO
        //  MUERE (la regla de la casa: nada de círculos planos — era un
        //  SoftGlow redondo estirado a 2,4×ESCALA): su lugar lo ocupa
        //  EL ANILLO RÚNICO SOLAR del propio dios (la letra: «y darle
        //  tambien un anillo runico solar al grimorio jefe»).
        //  (3) LA CORONA son EXACTAMENTE los minions a nivel máximo.
        // ════════════════════════════════════════════════════════════
        public override bool PreDraw(SpriteBatch sb, Vector2 screenPos, Color drawColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                Player presa = Main.player[NPC.target];

                // ══ EL ANILLO RÚNICO SOLAR DEL DIOS (la letra: «y darle
                //    también un anillo rúnico solar al grimorio jefe»):
                //    la elipse inclinada de los soles (SigiloLib — el
                //    MISMO emisor del anillo de sol de la mejora 3 del
                //    escuadrón), al tamaño del dios, girando alrededor
                //    del libro — EL REEMPLAZO DEL HALO PLANO ══
                VFXCore.Begin();
                try
                {
                    // v6.183 — LOS ANILLOS SOLARES DE LA FASE (la letra:
                    // «que por cada fase obtenga un anillo runico solar»):
                    // UNO por FASE, cada uno a SU compás (el tercero es el
                    // azul estelar de los soles de la mejora 4) — y el
                    // GIRO VIVO del ataque: en el arranque se aceleran y
                    // ARDEN (la animación de la letra: «el hara que el
                    // anillo comience a girar»).
                    for (int i = 0; i < Fase; i++)
                    {
                        float rx = 58f + i * 16f;
                        bool arde = _anilloEstado > 0 || _anilloPersecucion > 0;
                        float latido = arde ? 1f + 0.22f * MathF.Sin(t * 11f + i * 2f) : 1f;
                        bool azul = i == 2;
                        SigiloLib.AnilloRunico(NPC.Center, rx * latido, rx * 0.36f * latido,
                            -0.55f + i * 0.22f,
                            t * (0.24f + i * 0.06f)
                                + _anilloGiro * (i % 2 == 0 ? 1f : -0.72f),
                            8 - i, Math.Max(rx / 52f, 0.25f) * 1.55f, t, i * 3,
                            azul ? SigiloLib.CuerpoAzul : SigiloLib.CuerpoOro,
                            azul ? SigiloLib.PuntaAzul : SigiloLib.PuntaOro,
                            (arde ? 1f : 0.92f) * NPC.Opacity);
                    }
                    VFXCore.FlushAdditive();
                }
                catch (System.Exception exAnillo)
                { VFXCore.CerrarLoteSiAbierto(); global::AethonMod.Content.Systems.RegistroSistema.Fallo("AethonSellado.Anillo", exAnillo); }

                DibujarLaCorona(sb, t, presa, screenPos);
                DibujarElCuerpo(sb, t, presa, screenPos);
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AethonSellado", exRegistro); }
            VFXCore.ReabrirLoteVanilla();
            return false;   // el dibujo es TODO nuestro
        }

        // ── LA CORONA DE LOS ONCE OJOS: los minions a nivel máximo ──
        private void DibujarLaCorona(SpriteBatch sb, float t, Player presa, Vector2 screenPos)
        {
            // ── LOS ANILLOS DE AGUJERO NEGRO (el lote aditivo del sello
            //    del minion — SU MISMO dibujado): el círculo mágico
            //    erosionado + el dorado de 8 girando + el violeta de 6
            //    contrarrotando ──
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                for (int i = 0; i < OJOS; i++)
                {
                    bool habla = Estado == EST_PELEA && i == _ojoHablante && _acto == 1;
                    Vector2 po = PosicionDelOjo(i, t) - screenPos;
                    float arde = habla ? 1f + 0.25f * MathF.Sin(t * 9f) : 0f;   // el hablante ARDE
                    float escOjo = EscalaDelOjo(i, t) * (habla ? 1.35f : 1f);
                    int seed = NPC.whoAmI * 13 + i * 7 + 3;
                    float r = ESCALA_MINION * 16f * (habla ? 1.25f : 1f);
                    float glyph = Math.Max(r / 52f, 0.25f) * 1.75f;
                    OrbitaLib.SigiloErosionado(po, r, t, seed,
                        2.55f, 8, 0.06f, glyph, SelloOro, SelloPunta,
                        offset: 0, hashOff: 0, spikeAngle: -0.55f, spikeWindow: 0.45f);
                    float g1 = Math.Max(15f / 52f, 0.25f) * 1.15f;
                    OrbitaLib.CirculoRunico(po, 15f * (1f + arde * 0.2f), t, 1f, 8, 0.10f,
                        OrbitaLib.RunaDorada, OrbitaLib.RunaDoradaTip, g1,
                        null, 0, 1, 0.95f, 0.30f);
                    float g2 = Math.Max(20f / 52f, 0.25f) * 1.15f;
                    OrbitaLib.CirculoRunico(po, 20f * (1f + arde * 0.2f), t, 1f, 6, -0.075f,
                        OrbitaLib.RunaVioleta, OrbitaLib.RunaVioletaTip, g2,
                        null, 3, 1, 0.95f, 0.26f);
                }
            }
            finally { Main.spriteBatch.End(); }

            // ── LOS ANILLOS RÚNICOS DE SOL (el lote VFX de coords de
            //    mundo — las elipses inclinadas de la mejora 3 y 4) ──
            VFXCore.Begin();
            try
            {
                for (int i = 0; i < OJOS; i++)
                {
                    bool habla = Estado == EST_PELEA && i == _ojoHablante && _acto == 1;
                    Vector2 c = PosicionDelOjo(i, t);
                    float mult = habla ? 1.2f : 1f;
                    SigiloLib.AnilloRunico(c, 25f * mult, 25f * 0.34f * mult, -0.55f,
                        t * 0.26f, 8, Math.Max(25f / 52f, 0.25f) * 1.45f,
                        t, 0, SigiloLib.CuerpoOro, SigiloLib.PuntaOro, 0.95f);
                    SigiloLib.AnilloRunico(c, 30f * mult, 30f * 0.41f * mult, 0.35f,
                        -t * 0.31f, 6, Math.Max(30f / 52f, 0.25f) * 1.45f,
                        t, 2, SigiloLib.CuerpoAzul, SigiloLib.PuntaAzul, 0.95f);
                }
                VFXCore.FlushAdditive();
            }
            catch (System.Exception exAnilloSol)
            { VFXCore.CerrarLoteSiAbierto(); global::AethonMod.Content.Systems.RegistroSistema.Fallo("AethonSellado.AnilloSol", exAnilloSol); }

            // ── LOS OJOS (el lote vanilla — reabierto): el sprite a la
            //    ESCALA DEL MINION, su parpadeo propio y SU IRIS clavado
            //    en la presa (la firma del escuadrón) ──
            VFXCore.ReabrirLoteVanilla();
            Texture2D ojoA = ModContent.Request<Texture2D>(
                "AethonMod/Content/Projectiles/OjoEterno_Abierto").Value;
            Texture2D ojoM = ModContent.Request<Texture2D>(
                "AethonMod/Content/Projectiles/OjoEterno_Medio").Value;
            Texture2D ojoC = ModContent.Request<Texture2D>(
                "AethonMod/Content/Projectiles/OjoEterno_Cerrado").Value;
            Texture2D iris = ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
            for (int i = 0; i < OJOS; i++)
            {
                Vector2 po = PosicionDelOjo(i, t) - screenPos;
                bool habla = Estado == EST_PELEA && i == _ojoHablante && _acto == 1;
                float escOjo = EscalaDelOjo(i, t) * (habla ? 1.35f : 1f);

                // SU PARPADEO (cada ojo a su compás — la corona respira)
                int fase = ((int)(t * 60f) + i * 37) % 170;
                Texture2D texOjo = ojoA;
                if (fase > 156) texOjo = ojoC;
                else if (fase > 146) texOjo = ojoM;

                sb.Draw(texOjo, po, null,
                    Color.White * NPC.Opacity, 0f,
                    texOjo.Size() * 0.5f, escOjo, SpriteEffects.None, 0f);

                // EL IRIS DEL ESCUADRÓN: clavado en la presa — el viaje
                // por la almendra (±7,5 · ±2,5) a la ESCALA del minion
                if (texOjo == ojoA && presa != null && presa.active)
                {
                    float ang = (presa.Center - screenPos - po).ToRotation();
                    Vector2 mirada = new(MathF.Cos(ang) * 7.5f, MathF.Sin(ang) * 2.5f);
                    Vector2 posIris = po + (new Vector2(0.7f, 2.3f) + mirada) * escOjo;
                    sb.Draw(iris, posIris, null,
                        Color.White * NPC.Opacity, 0f,
                        iris.Size() * 0.5f, 0.41f * escOjo, SpriteEffects.None, 0f);
                }
            }
        }

        /// <summary>La escala viva del ojo `i`: la del MINION (0,375)
        /// con su micro-respiración — la hinchazón de la v6.180 murió.</summary>
        private float EscalaDelOjo(int i, float t)
            => ESCALA_MINION * (1f + 0.06f * MathF.Sin(t * 3.1f + i * 2.2f));

        // ── EL CUERPO: el grimorio normal + su ojo a la ESCALA DEL ARMA ──
        private void DibujarElCuerpo(SpriteBatch sb, float t, Player presa, Vector2 screenPos)
        {
            Vector2 pos = NPC.Center - screenPos;

            // el vaivén del flotante (la firma del grimorio) + el
            // cabeceo hacia donde va
            float bob = MathF.Sin(t * 1.35f + NPC.whoAmI * 0.4f) * 5.5f;
            float tilt = NPC.rotation * 0.6f + 0.05f * MathF.Sin(t * 0.9f);
            Texture2D cuerpo = ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambriento").Value;
            Vector2 destino = pos + new Vector2(0f, bob);

            // LA RESPIRACIÓN (el libro vivo — la esencia del Menor)
            float escVivo = ESCALA * (1f + 0.035f * MathF.Sin(t * 2.1f));
            sb.Draw(cuerpo, destino, null,
                Color.White * NPC.Opacity, tilt,
                cuerpo.Size() * 0.5f, escVivo, SpriteEffects.None, 0f);

            // ══ SU OJO (el del arma: el iris que MIRA + el párpado
            //    que parpadea + el iris rojo de la furia) ══
            // LA CASA DEL OJO en el sprite (OJO = 19.09, 22.45 de 36×49)
            Vector2 casaOjo = destino + new Vector2((19.09f - 18f), (22.45f - 24.5f)) * escVivo;
            // v6.182 — LA PROPORCIÓN DEL ARMA (la letra: «el iris es
            // demasiado grande»): el disco de 32 px nacía a ESCALA
            // COMPLETA (42 px de disco sobre un libro de 47×64); el ARMA
            // dibuja su iris a IRIS_ESC = 0,2773 (la caja de 174 px →
            // 8,87 px de juego en el sprite 36×49). AHORA el dios usa
            // LA MISMA proporción: 32 × 0,2773 × 1,30 ≈ 11,6 px — el
            // ojo del arma en el cuerpo del dios.
            // LA MIRADA: el iris se desliza hacia la presa (el
            // desplazamiento del arma — LA CAJA DEL SOCKET, sin inflar)
            Vector2 mira = Vector2.Zero;
            if (presa != null && presa.active)
            {
                mira = presa.Center - screenPos - casaOjo;
                if (mira.LengthSquared() > 1f) mira = Vector2.Normalize(mira) * 2.6f * escVivo;
            }
            // EL PARPADEO (el compás del Menor: late cada ~2 s)
            float alpha = 1f;
            _ojoParpadeo = (_ojoParpadeo + 1) % 130;
            if (_ojoParpadeo > 118) alpha = Math.Abs(_ojoParpadeo - 124) / 6f;

            Texture2D iris = ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
            float escIris = escVivo * Weapons.GrimorioHambriento.IRIS_ESC;
            sb.Draw(iris, casaOjo + mira, null,
                Color.White * (0.92f * alpha * NPC.Opacity), 0f,
                iris.Size() * 0.5f, escIris, SpriteEffects.None, 0f);
            // EL IRIS ROJO (la furia lo enciende — la marca del dios)
            if (Furia > 0.35f)
                sb.Draw(ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambriento_IrisRojo").Value,
                    casaOjo + mira, null,
                    Color.White * (MathHelper.Clamp((Furia - 0.35f) * 1.4f, 0f, 0.85f) * alpha * NPC.Opacity),
                    0f, iris.Size() * 0.5f, escIris, SpriteEffects.None, 0f);
            // EL PÁRPADO (medio/cerrado en el parpadeo — la casa)
            if (alpha < 0.99f)
            {
                Texture2D tapa = ModContent.Request<Texture2D>(
                    alpha < 0.45f ? "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado"
                                  : "AethonMod/Content/Weapons/GrimorioHambriento_Medio").Value;
                sb.Draw(tapa, destino, null,
                    Color.White * NPC.Opacity, tilt,
                    tapa.Size() * 0.5f, escVivo, SpriteEffects.None, 0f);
            }
        }
    }

    // ======================================================================
    //  IRASELLADOPROJECTILE — v6.180 — LA IRA DE UN SER EXTERIOR (la
    //  letra: «al llegar a 5 sombra de la pagina entra en fase 2 y hace
    //  el ataque de Ira de un ser exterior persiguiendo lentamente al
    //  jugador» + el segundo en la fase 3).
    //
    //  EL CINE: (1) LA CEREMONIA (~2,5 s): el ojo COLOSAL nace sobre el
    //  dios — se hincha, los once ojos de su corona convergen hacia él
    //  y el círculo se cierra mientras el cielo tiembla. (2) LA
    //  PERSIGUIÓN LENTA (~10 s): el ojo se suelta y PERSIGUE al jugador
    //  DESPACIO (velocidad de sonámbulo — se esquiva, pero no se
    //  abandona), escupiendo EL HAZ de su pupila: un rayo colosal que
    //  gira hacia el jugador con una lentitud de sentencia. (3) LA
    //  DISOLUCIÓN: el ser se cansa del mundo y se va.
    //
    //  ai[0] = whoAmI del dios que la lanzó · ai[1] = número (0/1) ·
    //  ai[2] = edad · localAI[0] = el ángulo del haz.
    // ======================================================================
    public class IraSelladoProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private const int T_CEREMONIA = 150;
        private const int T_PERSECUCION = 600;

        /// <summary>El ojo del ser exterior (radio en px).</summary>
        private const float RADIO = 54f;
        /// <summary>La velocidad de la persecución LENTA (la letra).</summary>
        private const float LENTITUD = 2.3f;
        /// <summary>El largo del haz.</summary>
        private const float LARGO_HAZ = 860f;

        private NPC Dios => Projectile.ai[0] >= 0 && Projectile.ai[0] < Main.npc.Length
            ? Main.npc[(int)Projectile.ai[0]] : null;

        private float Edad => Projectile.ai[2];

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = (int)(RADIO * 2f);
            Projectile.height = (int)(RADIO * 2f);
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = T_CEREMONIA + T_PERSECUCION + 40;
            Projectile.netImportant = true;
            Projectile.Opacity = 0f;
        }

        public override void AI()
        {
            try
            {
                Projectile.ai[2]++;
                float t = Edad;
                Player presa = Main.player[Projectile.owner < Main.maxPlayers
                    ? Projectile.owner : 0];
                if (presa == null || !presa.active || presa.dead) { Projectile.Kill(); return; }

                // ══ LA CEREMONIA: nace sobre el dios y SE HINCHA ══
                if (t < T_CEREMONIA)
                {
                    NPC dios = Dios;
                    if (dios != null && dios.active)
                        Projectile.Center = Vector2.Lerp(Projectile.Center,
                            dios.Center + new Vector2(0f, -90f), 0.22f);
                    else Projectile.velocity *= 0.9f;
                    Projectile.Opacity = Math.Min(1f, t / 50f);
                    if (t % 30f == 0f && !Main.dedServ)
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122.WithPitchOffset(-0.6f), Projectile.Center);
                    return;
                }

                // ══ LA PERSECUCIÓN LENTA ══
                Projectile.Opacity = 1f;
                if (t == T_CEREMONIA && !Main.dedServ)
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item122.WithPitchOffset(-0.9f).WithVolumeScale(0.9f), Projectile.Center);

                // EL OJO PERSIGUE DESPACIO (se esquiva, no se abandona)
                Vector2 hacia = presa.Center - Projectile.Center;
                float dist = hacia.Length();
                if (dist > 26f)
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity,
                        hacia.SafeNormalize(Vector2.UnitY) * LENTITUD, 0.018f);
                else Projectile.velocity *= 0.94f;

                // EL HAZ gira hacia la presa CON LENTITUD DE SENTENCIA
                float angActual = Projectile.localAI[0] == 0f
                    ? (presa.Center - Projectile.Center).ToRotation() : Projectile.localAI[0];
                float angMeta = (presa.Center - Projectile.Center).ToRotation();
                float delta = MathHelper.WrapAngle(angMeta - angActual);
                float giroMax = 0.016f;   // radianes por tick — LENTO
                angActual += MathHelper.Clamp(delta, -giroMax, giroMax);
                Projectile.localAI[0] = angActual;

                // EL DAÑO DEL HAZ (la línea bajo la pupila)
                if (Main.netMode != NetmodeID.MultiplayerClient && t > T_CEREMONIA + 30f)
                {
                    Vector2 dir = angActual.ToRotationVector2();
                    Vector2 rel = presa.Center - Projectile.Center;
                    float proyeccion = Vector2.Dot(rel, dir);
                    if (proyeccion > RADIO * 0.6f && proyeccion < LARGO_HAZ)
                    {
                        Vector2 punto = Projectile.Center + dir * proyeccion;
                        if (presa.Center.DistanceSQ(punto) < 30f * 30f)
                            presa.Hurt(Terraria.DataStructures.PlayerDeathReason.ByProjectile(
                                presa.whoAmI, Projectile.whoAmI),
                                (int)(Projectile.damage * 0.35f), 0, false, false, -1, false);
                    }
                }

                // LAS CHISPAS DEL HAZ (la firma de la casa — deterministas)
                if (!Main.dedServ && Main.rand.NextBool(3))
                {
                    Vector2 dir = angActual.ToRotationVector2();
                    Vector2 chispa = Projectile.Center + dir * Main.rand.NextFloat(80f, LARGO_HAZ);
                    var d = Dust.NewDustPerfect(chispa, DustID.Shadowflame,
                        new Vector2(0f, -0.8f), 130, new Color(150, 60, 210), 0.9f);
                    d.noGravity = true;
                }

                // ══ LA DISOLUCIÓN (el ser se cansa del mundo) ══
                if (t > T_CEREMONIA + T_PERSECUCION) Projectile.Kill();
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("IraSellado", exRegistro); }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                float t = Edad;
                Vector2 pos = Projectile.Center - Main.screenPosition;
                float nacer = Math.Min(1f, t / 50f);
                float hinchazon = t < T_CEREMONIA
                    ? MathHelper.Lerp(0.35f, 1f, t / T_CEREMONIA)
                    : 1f;

                Texture2D ojoA = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Projectiles/OjoEterno_Abierto").Value;
                Texture2D ojoM = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Projectiles/OjoEterno_Medio").Value;
                var glow = VFXCore.SoftGlow;

                // ══ v6.182 — LA CORONA DE CONJURO DEL SER (el halo plano
                //    MURIÓ — la regla de la casa: nada de círculos planos;
                //    era un SoftGlow redondo estirado a 3,6×): la TRIPLE
                //    CORONA de los agujeros negros de la casa (OrbitaLib.
                //    CoronaConjuro — el círculo dorado de 8 + el violeta
                //    de 6 + el blanco íntimo de 6, las runas DE PIE con
                //    sus perlas) girando alrededor del ojo colosal ══
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    OrbitaLib.CoronaConjuro(pos, RADIO * 1.5f * hinchazon, t,
                        0.95f * nacer * Projectile.Opacity, 1.30f);
                }
                finally { Main.spriteBatch.End(); }

                // ══ EL HAZ (después de la ceremonia) ══
                VFXCore.ReabrirLoteVanilla();
                if (t >= T_CEREMONIA)
                {
                    float vivo = Math.Min(1f, (t - T_CEREMONIA) / 30f);
                    float morir = Math.Min(1f, (T_CEREMONIA + T_PERSECUCION - t) / 40f);
                    float alfaH = vivo * morir * Projectile.Opacity;
                    Vector2 dir = Projectile.localAI[0].ToRotationVector2();
                    float largo = LARGO_HAZ;
                    // EL CUERPO DEL HAZ: tres fundas (la ancha, la media y el núcleo)
                    for (int capa = 0; capa < 3; capa++)
                    {
                        float ancho = capa == 0 ? 46f : capa == 1 ? 26f : 12f;
                        Color c = capa == 0 ? new Color(120, 46, 190)
                            : capa == 1 ? new Color(190, 96, 235)
                            : new Color(255, 240, 250);
                        if (glow != null)
                        {
                            // EL CONDUCTO: el glow estirado a lo largo del haz
                            Vector2 a = pos + dir * (RADIO * 0.6f);
                            Vector2 b = pos + dir * largo;
                            float ang = Projectile.localAI[0];
                            Vector2 centro = (a + b) * 0.5f;
                            float largoPx = Vector2.Distance(a, b);
                            Main.spriteBatch.Draw(glow, centro, null, c * (0.30f * alfaH),
                                ang, glow.Size() * 0.5f,
                                new Vector2(largoPx / glow.Width, ancho / glow.Height),
                                SpriteEffects.None, 0f);
                        }
                    }
                    // LA BOCA DEL HAZ (la estrella que muerde el aire)
                    VFXCore.CerrarLoteSiAbierto();
                    Vector2 boca = pos + dir * (RADIO * 0.6f + 26f);
                    VFXCore.Quad(boca, new Color(255, 240, 250) * (0.75f * alfaH),
                        new Vector2(34f, 34f), Projectile.localAI[0] + Main.GlobalTimeWrappedHourly);
                    VFXCore.FlushAdditive();
                    VFXCore.ReabrirLoteVanilla();
                }

                // ══ EL OJO COLOSAL ══
                float escOjo = (RADIO * 2f * hinchazon) / ojoA.Width;
                // EL PARPADEO LENTO DEL SER (casi humano — más aterrador)
                int fase = (int)t % 240;
                Texture2D texOjo = fase > 228 ? ojoM : ojoA;
                Main.spriteBatch.Draw(texOjo, pos, null,
                    Color.White * (nacer * Projectile.Opacity), 0f,
                    texOjo.Size() * 0.5f, escOjo, SpriteEffects.None, 0f);

                // ══ LOS ONCE OJOS CONVERGENTES (la ceremonia: la corona
                //    del dios se reza hacia el coloso) ══
                if (t < T_CEREMONIA + 40f)
                {
                    float converge = MathHelper.Clamp(t / T_CEREMONIA, 0f, 1f);
                    float radio = MathHelper.Lerp(230f, 78f, converge);
                    for (int i = 0; i < 11; i++)
                    {
                        float ang = Main.GlobalTimeWrappedHourly * 1.4f + i * (MathHelper.TwoPi / 11f);
                        Vector2 pc = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.8f) * radio;
                        Main.spriteBatch.Draw(ojoA, pc, null,
                            Color.White * (0.65f * nacer * (1f - converge * 0.35f)), 0f,
                            ojoA.Size() * 0.5f, 0.55f, SpriteEffects.None, 0f);
                    }
                }
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("IraSellado", exRegistro); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }

    // ======================================================================
    //  v6.183 — RAYO SOLAR DEL SELLO (la letra: «el hara que el anillo
    //  comience a girar y lanzara rayos de energia»): el filamento de
    //  vértices de la casa (R1: CERO sprites de rayo) — una estocada
    //  radial de la rueda de la FASE 1, cola de filamento oro y núcleo
    //  blanco, chispa de GoldFlame a la cabeza.
    // ======================================================================
    public class RayoSolarSelladoProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private static readonly Color Halo = new(255, 186, 84);
        private static readonly Color Nucleo = new(255, 246, 214);

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 170;   // R23 — LA VIDA CONTADA
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            try
            {
                // EL VUELO RECTO de la rueda (la estocada radial) con su
                // chispa de oro (determinista en el draw — R22 en el filo)
                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    var d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                        new Vector2(0f, -0.3f), 140, new Color(255, 196, 90), 0.75f);
                    d.noGravity = true;
                }
                // los bordes del mundo se lo tragan
                if (Projectile.Center.X < 80f || Projectile.Center.X > (Main.maxTilesX - 5) * 16f ||
                    Projectile.Center.Y < 80f || Projectile.Center.Y > (Main.maxTilesY - 5) * 16f)
                    Projectile.Kill();
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("RayoSolarSellado", exRegistro); }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                // EL FILAMENTO: la cola del rayo (vértices, la casa) —
                // tres tramos rectos con la textura de ruido viajando
                float ang = Projectile.velocity.ToRotation();
                Vector2 screen = Main.screenPosition;
                Vector2 cabeza = Projectile.Center - screen;
                Vector2 cola = cabeza - Projectile.velocity.SafeNormalize(Vector2.UnitX)
                    * MathHelper.Clamp(Projectile.velocity.Length() * 3.2f, 42f, 96f);
                Vector2 medio = (cabeza + cola) * 0.5f
                    + new Vector2(0f, 3f * MathF.Sin(Main.GlobalTimeWrappedHourly * 14f + Projectile.whoAmI));
                Vector2[] pts = { cola, medio, cabeza };
                int seed = Projectile.whoAmI * 31;
                int flick = (int)(Main.GlobalTimeWrappedHourly * 60f) >> 1;
                RayoStrip.Filamento(pts, 5.5f, Halo, Nucleo,
                    0.95f, StormTaper.Center, seed, flick,
                    suavizar: false, crackle: true);
                VFXCore.CerrarLoteSiAbierto();

                // LA CABEZA: la chispa que muerde el aire
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    var glow = VFXCore.SoftGlow;
                    if (glow != null)
                        Main.spriteBatch.Draw(glow, cabeza, null,
                            new Color(255, 214, 120) * 0.85f, ang,
                            glow.Size() * 0.5f, new Vector2(1.05f, 0.62f),
                            SpriteEffects.None, 0f);
                }
                finally { Main.spriteBatch.End(); }
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("RayoSolarSellado.Draw", exRegistro); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }

    // ======================================================================
    //  v6.183 — EL HAZ SOLAR CONCENTRADO (la letra: «cuando son 2
    //  anillos lanzaran energia concentrada… al ser 3 anillos lanzaran
    //  energia concentrada y al mismo tiempo perseguira al jugador»):
    //  UN HAZ POR ANILLO — anclado al PUNTO VIVO de su anillo (gira con
    //  él alrededor del dios), las TRES FUNDAS del Ira en ORO solar y
    //  el rastro lento de sentencia hacia la presa. En la fase 3 el
    //  dios CAZA mientras sus tres haces arden.
    // ======================================================================
    public class HazSolarSelladoProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private const float LARGO_HAZ = 780f;
        private const float ANCHO_LINEA = 26f;

        private NPC Dios => Projectile.ai[0] >= 0 && Projectile.ai[0] < Main.npc.Length
            ? Main.npc[(int)Projectile.ai[0]] : null;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 200;   // R23 — LA VIDA CONTADA
            Projectile.netImportant = true;
            Projectile.Opacity = 0f;
        }

        public override void AI()
        {
            try
            {
                Projectile.ai[2]++;
                NPC dios = Dios;
                if (dios == null || !dios.active) { Projectile.Kill(); return; }
                Player presa = Main.player[Projectile.owner < Main.maxPlayers
                    ? Projectile.owner : 0];
                if (presa == null || !presa.active || presa.dead) { Projectile.Kill(); return; }

                // EL NACER DEL HAZ (el prendido del anillo)
                Projectile.Opacity = Math.Min(1f, Projectile.ai[2] / 25f);

                // LA ANCLA VIVA: el PUNTO del anillo girando alrededor
                // del dios — cada anillo a SU compás, el haz nace de él
                int indice = (int)Projectile.ai[1] % 3;
                float giro = Main.GlobalTimeWrappedHourly * (0.55f + indice * 0.14f)
                    + indice * 2.1f;
                float radio = 58f + indice * 16f;
                Projectile.Center = dios.Center
                    + new Vector2(MathF.Cos(giro), MathF.Sin(giro) * 0.40f) * radio;

                // EL RASTRO LENTO: el haz sigue a la presa con lentitud
                // de sentencia (0,013 rad/t — la del Ira, la de siempre)
                float angActual = Projectile.localAI[0] == 0f
                    ? (presa.Center - Projectile.Center).ToRotation() : Projectile.localAI[0];
                float delta = MathHelper.WrapAngle(
                    (presa.Center - Projectile.Center).ToRotation() - angActual);
                angActual += MathHelper.Clamp(delta, -0.013f, 0.013f);
                Projectile.localAI[0] = angActual;

                // EL DAÑO DE LA LÍNEA (la línea bajo la pupila — el patrón
                // del Ira): proyección + perpendicular
                if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.ai[2] > 25f)
                {
                    Vector2 dir = angActual.ToRotationVector2();
                    Vector2 rel = presa.Center - Projectile.Center;
                    float proyeccion = Vector2.Dot(rel, dir);
                    if (proyeccion > 30f && proyeccion < LARGO_HAZ)
                    {
                        Vector2 punto = Projectile.Center + dir * proyeccion;
                        if (presa.Center.DistanceSQ(punto) < ANCHO_LINEA * ANCHO_LINEA)
                            presa.Hurt(Terraria.DataStructures.PlayerDeathReason.ByProjectile(
                                presa.whoAmI, Projectile.whoAmI),
                                (int)(Projectile.damage * 0.30f), 0, false, false, -1, false);
                    }
                }

                // LAS CHISPAS DEL CONDUCTO (la firma de la casa)
                if (!Main.dedServ && Main.rand.NextBool(3))
                {
                    Vector2 dir = angActual.ToRotationVector2();
                    Vector2 chispa = Projectile.Center + dir * Main.rand.NextFloat(70f, LARGO_HAZ);
                    var d = Dust.NewDustPerfect(chispa, DustID.GoldFlame,
                        new Vector2(0f, -0.8f), 130, new Color(255, 196, 90), 0.9f);
                    d.noGravity = true;
                }

                // EL MORIR: los últimos 20 ticks se apaga
                if (Projectile.timeLeft < 20)
                    Projectile.Opacity = MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f);
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("HazSolarSellado", exRegistro); }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                Vector2 pos = Projectile.Center - Main.screenPosition;
                float alfaH = Projectile.Opacity;
                Vector2 dir = Projectile.localAI[0].ToRotationVector2();
                var glow = VFXCore.SoftGlow;

                // LAS TRES FUNDAS (la técnica del Ira — EN ORO SOLAR):
                // la ancha, la media y el núcleo blanco
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    for (int capa = 0; capa < 3; capa++)
                    {
                        float ancho = capa == 0 ? 42f : capa == 1 ? 24f : 11f;
                        Color c = capa == 0 ? new Color(214, 116, 36)
                            : capa == 1 ? new Color(255, 178, 78)
                            : new Color(255, 248, 220);
                        if (glow != null)
                        {
                            Vector2 a = pos + dir * 30f;
                            Vector2 b = pos + dir * LARGO_HAZ;
                            Vector2 centro = (a + b) * 0.5f;
                            float largoPx = Vector2.Distance(a, b);
                            Main.spriteBatch.Draw(glow, centro, null, c * (0.30f * alfaH),
                                Projectile.localAI[0], glow.Size() * 0.5f,
                                new Vector2(largoPx / glow.Width, ancho / glow.Height),
                                SpriteEffects.None, 0f);
                        }
                    }
                }
                finally { Main.spriteBatch.End(); }

                // LA BOCA DEL HAZ (la estrella que muerde el aire)
                VFXCore.CerrarLoteSiAbierto();
                Vector2 boca = pos + dir * 54f;
                VFXCore.Quad(boca, new Color(255, 240, 200) * (0.75f * alfaH),
                    new Vector2(32f, 32f), Projectile.localAI[0] + Main.GlobalTimeWrappedHourly);
                VFXCore.FlushAdditive();
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("HazSolarSellado.Draw", exRegistro); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }

    // ======================================================================
    //  LA BARRA DEL DIOS SELLADO (la de siempre — el dios cambió de
    //  tamaño, no de barra)
    // ======================================================================
    public class BarraAethonSellado : ModBossBar
    {
        public override bool? ModifyInfo(ref BigProgressBarInfo info,
            ref float life, ref float lifeMax, ref float scale, ref float dps)
        {
            if (info.npcIndexToAimAt < 0 || info.npcIndexToAimAt >= Main.maxNPCs)
                return false;
            NPC n = Main.npc[info.npcIndexToAimAt];
            return n != null && n.active && n.type == ModContent.NPCType<AethonSellado>()
                ? (bool?)null : false;
        }
    }
}
