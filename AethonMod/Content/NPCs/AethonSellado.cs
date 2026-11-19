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
    /// <summary>
    /// AethonSellado — v6.144 — EL DIOS SELLADO (el grimorio mismo).
    ///
    /// LA LETRA DEL USUARIO: «Crea un nuevo jefe que sea Aethon
    /// (Sellado) solo aparece en el mundo interior a travez del pilar,
    /// este jefe sera el mismo grimorio y con animaciones propias,
    /// tendras que animar como vuela, como se desplaza de un lugar a
    /// otro, ya sabes como lo hace, el grimorio mira a donde va a ir y
    /// luego va, tambien tendras que animar como se teletransporta, y
    /// el grimorio lo hace cubriendose de bruma, ya sabes como en la
    /// llegada, tendras que animar una coreografia donde usara a sus
    /// minions, y todos los ataques de los que dispone el grimorio pero
    /// estos ataques en el jefe seran individuales y seran lanzado con
    /// ayuda de sus propios minions los cuales si tendran una barra de
    /// vida, pero cuando uno sea destruido a los 30 segundos
    /// reaparacen, las animaciones estan creadas solo mira como
    /// animaste al grimorio y sus minions».
    ///
    ///  · EL CUERPO: el sprite del Nervioso (GrimorioHambrientoNervioso
    ///    — la misma fuente de arte del Eterno) a ESCALA GIGANTE, con
    ///    el vaivén y el cabeceo del GrimorioFlotante (bob + tilt).
    ///  · EL VUELO (mira a dónde va y luego va): dos tiempos — PRIMERO
    ///    el libro SE ORIENTA (la rotación gira hacia el rumbo elegido,
    ///    los ojos miran), DESPUÉS acelera. El rumbo se elige alrededor
    ///    de la presa (la distancia de respeto del jefe).
    ///  · EL TELETRANSPORTE (cubiéndose de bruma): la llegada del
    ///    interior — el libro EXHALA una nube de SombrasLib.Bruma que
    ///    lo CUBRE entero, desaparece dentro, y REAPARECE envuelto en
    ///    otra nube a 320-560 px de la presa.
    ///  · LOS MINIONS CON BARRA (Ojos Sellados): tres NPCs-ojo (la
    ///    textura del escuadrón OjoEterno con la animación
    ///    Abierto/Medio/Cerrado) que orbitan al libro — CADA UNO con su
    ///    barra de vida; destruidos, REVIVEN a los 30 s (el sello los
    ///    re-escribe).
    ///  · LOS ATAQUES DEL ARSENAL (individuales, por los ojos): cada
    ///    ataque es UN SOLO acto disparado por UN ojo (el libro solo
    ///    dirige): la púa sagraria, el coro de cristal, el virote del
    ///    vacío, la flecha estelar, el tajo portador, el estallido
    ///    radiante — el repertorio de AtaqueJefeProjectile.
    ///  · LA COREOGRAFÍA: fases por el reloj — convocatoria de ojos →
    ///    persecución serena con ataques sueltos → la danza (teleports
    ///    encadenados entre ataque y ataque) → la furia (los tres ojos
    ///    a la vez, cada 2 s).
    /// </summary>
    public class AethonSellado : ModNPC
    {
        // ═══ LOS ESTADOS DEL DIOS ═══
        private const byte EST_NACER = 0;      // la materialización con bruma
        private const byte EST_VOLAR = 1;      // el desplazamiento de dos tiempos
        private const byte EST_TELEPORT = 2;   // cubrirse de bruma y reaparecer
        private const byte EST_MUERTE = 3;     // la última luz

        // ═══ LOS MINIONS: LOS DIECISIETE OJOS SELLADOS ═══
        // v6.145 — LA LETRA: «el jefe AETHON (SELLADO) esta mal
        // implementando, debe tener 17 minions» — la corte completa
        // del dios sellado: diecisiete ojos, la órbita mayor.
        private const int OJOS_MAX = 17;
        private const int REVIVIR_T = 1800;     // 30 segundos (la letra)

        // ═══ LOS ATAQUES DEL ARSENAL (individuales — el libro dirige,
        //     los ojos disparan) ═══
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
        private byte Estado => (byte)(NPC.ai[0] % 4);
        private float T => NPC.ai[1];
        private float Furia => MathHelper.Clamp(1f - (float)NPC.life / NPC.lifeMax, 0f, 1f);

        // el rumbo elegido (el libro MIRA primero, va después)
        private Vector2 _rumboElegido;
        private float _rumboTiempo;
        private int _cooldownAtaque;
        private int _cooldownTeleport;
        private readonly int[] _revivirEn = new int[OJOS_MAX];
        // v6.145 — LAS GENERACIONES de los ojos (cuántas veces renació
        // cada uno — el poder crece con cada resurrección)
        private readonly int[] _generaciones = new int[OJOS_MAX];

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 140;
            NPC.height = 180;
            NPC.lifeMax = 900_000;
            NPC.damage = 120;
            NPC.defense = 60;
            NPC.knockBackResist = 0f;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.aiStyle = -1;
            NPC.npcSlots = 15f;
            NPC.value = Item.buyPrice(platinum: 2);
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            if (!Main.dedServ)
                NPC.BossBar = ModContent.GetInstance<BarraAethonSellado>();
            Music = MusicID.LunarBoss;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry entry)
        {
            entry.Info.AddRange(new IBestiaryInfoElement[] {
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
                (int)player.Center.X, (int)player.Center.Y - 420,
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
            NPC.ai[1]++;
            NPC.TargetClosest(true);
            Player presa = Main.player[NPC.target];
            if (presa == null || !presa.active || presa.dead)
            {
                // sin presa: el sello se cierra y el libro se disuelve
                if (Estado != EST_MUERTE) { NPC.ai[0] = EST_MUERTE; NPC.ai[1] = 0; }
                return;
            }

            switch (Estado)
            {
                case EST_NACER: AINacer(presa); break;
                case EST_VOLAR: AIVolar(presa); break;
                case EST_TELEPORT: AITeleport(presa); break;
                case EST_MUERTE: AIMuerte(presa); break;
            }

            // LOS OJOS SELLADOS: mantener la órbita (y el reloj de la
            // resurrección — 30 s la letra)
            MantenerOjos(presa);
        }

        // ── EL NACIMIENTO: la llegada del interior — bruma que lo cubre ──
        private void AINacer(Player presa)
        {
            if (T == 1f)
                Sonar(SoundID.Item122.WithPitchOffset(-0.5f).WithVolumeScale(1f), NPC.Center);
            // la bruma de la llegada: CUBRE el libro entero mientras
            // la materialización vive (30 ticks)
            if (Main.netMode != NetmodeID.Server && T < 30f && T % 2f == 0f)
                for (int i = 0; i < 3; i++)
                {
                    Vector2 deriva = new Vector2(
                        Main.rand.NextFloat(-1.2f, 1.2f),
                        Main.rand.NextFloat(-1.6f, -0.2f));
                    SombrasLib.Bruma(NPC.Center + new Vector2(
                        Main.rand.NextFloat(-80, 80), Main.rand.NextFloat(-100, 100)),
                        60f + 40f * (float)Main.rand.NextDouble(), 0.7f,
                        Main.GlobalTimeWrappedHourly, NPC.whoAmI * 97 + i, deriva);
                }

            // el descenso sereno hasta la altura de vuelo
            Vector2 destino = new Vector2(presa.Center.X + presa.direction * 320f,
                presa.Center.Y - 300f);
            NPC.velocity = Vector2.Lerp(NPC.velocity,
                (destino - NPC.Center) * 0.03f, 0.08f);
            NPC.rotation = MathHelper.Lerp(NPC.rotation, 0f, 0.06f);

            if (T >= 45f) { NPC.ai[0] = EST_VOLAR; NPC.ai[1] = 0; ElegirRumbo(presa); }
        }

        // ── EL VUELO DE DOS TIEMPOS: mira a dónde va, luego va ──
        private void AIVolar(Player presa)
        {
            // v6.145 — EL VUELO DEL CAZADOR (la letra: «la animacion
            // debe ser tomada del grimorio del eterno cuando sale a
            // cazar» — el patrón EXACTO del GrimorioNerviosoFlotante):
            // aceleración suave hacia el destino + freno 0.88 + el
            // balanceo seno del paseo — el libro SERENO, mirando a
            // dónde va (la rotación gira primero, el cuerpo sigue).
            _rumboTiempo++;
            float giro = MathHelper.WrapAngle(_rumboElegido.ToRotation() - NPC.rotation);
            NPC.rotation += MathHelper.Clamp(giro, -0.05f, 0.05f);

            // EL DESTINO + EL VAIVÉN (el +10/-14 seno del flotante)
            Vector2 destino = _destino + new Vector2(
                MathF.Sin(Main.GlobalTimeWrappedHourly * 0.07f) * 10f,
                MathF.Sin(Main.GlobalTimeWrappedHourly * 0.11f) * 14f);

            // LA FÍSICA DEL LIBRO: velocity += (destino-Center)*0.009;
            // velocity *= 0.88 (la firma del espíritu) — con el TOPE
            // SERENO de la caza (10..20 según la distancia, la Furia
            // lo sube — el dios no pasea cuando arde)
            NPC.velocity += (destino - NPC.Center) * 0.009f;
            NPC.velocity *= 0.88f;
            float tope = MathHelper.Clamp(
                Vector2.Distance(destino, NPC.Center) * 0.01f, 10f, 20f + 12f * Furia);
            float velCaza = NPC.velocity.Length();
            if (velCaza > tope) NPC.velocity *= tope / velCaza;

            // ¿LLEGÓ? (cerca del destino elegido) → nuevo rumbo o teleport
            if (Vector2.Distance(NPC.Center, _destino) < 90f || _rumboTiempo > 210f)
            {
                _cooldownTeleport--;
                if (_cooldownTeleport <= 0 && Main.rand.NextBool(2))
                { NPC.ai[0] = EST_TELEPORT; NPC.ai[1] = 0; _cooldownTeleport = 260; return; }
                ElegirRumbo(presa);
            }

            // EL ARSENAL (individual — cada ataque UN ojo lo dispara)
            _cooldownAtaque -= 1 + (int)(Furia * 1.5f);
            if (_cooldownAtaque <= 0)
            {
                DispararArsenal(presa);
                _cooldownAtaque = Main.rand.Next(110, 170) - (int)(Furia * 55f);
            }
        }

        private Vector2 _destino;
        private void ElegirRumbo(Player presa)
        {
            float ang = Main.rand.NextFloat(MathHelper.TwoPi);
            _destino = presa.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.55f)
                * Main.rand.NextFloat(300f, 460f);
            _rumboElegido = (_destino - NPC.Center).SafeNormalize(Vector2.UnitX);
            _rumboTiempo = 0f;
        }

        // ── EL TELETRANSPORTE: cubrirse de bruma, ir, descubrirse ──
        private void AITeleport(Player presa)
        {
            if (T < 24f)
            {
                // LA CUBIERTA: el libro se ENVUELVE en bruma (la llegada
                // del interior — «ya sabes como en la llegada»)
                if (Main.netMode != NetmodeID.Server && T % 2f == 0f)
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 deriva = new Vector2(
                            Main.rand.NextFloat(-1.5f, 1.5f),
                            Main.rand.NextFloat(-2f, 0.2f));
                        SombrasLib.Bruma(NPC.Center + new Vector2(
                            Main.rand.NextFloat(-90, 90), Main.rand.NextFloat(-110, 110)),
                            55f + 45f * (float)Main.rand.NextDouble(), 0.75f,
                            Main.GlobalTimeWrappedHourly, NPC.whoAmI * 61 + i, deriva);
                    }
                NPC.velocity *= 0.90f;
                NPC.Opacity = MathHelper.Clamp(1f - T / 22f, 0f, 1f);
                if (T == 2f) Sonar(SoundID.Item122.WithPitchOffset(-0.3f), NPC.Center);
            }
            else if (T < 28f)
            {
                // EL TRASLADO (invisible): el punto nuevo alrededor de la presa
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 destino = presa.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.55f)
                    * Main.rand.NextFloat(340f, 560f);
                if (T == 25f) NPC.Center = destino;
                NPC.velocity = Vector2.Zero;
            }
            else
            {
                // EL DESCUBRIRSE: otra nube lo recibe (la misma firma)
                NPC.Opacity = MathHelper.Clamp((T - 28f) / 18f, 0f, 1f);
                if (Main.netMode != NetmodeID.Server && T < 48f && T % 2f == 0f)
                    for (int i = 0; i < 3; i++)
                        SombrasLib.Bruma(NPC.Center + new Vector2(
                            Main.rand.NextFloat(-80, 80), Main.rand.NextFloat(-100, 100)),
                            55f, 0.6f, Main.GlobalTimeWrappedHourly, NPC.whoAmI * 43 + i,
                            new Vector2(0f, -1.2f));
                if (T >= 48f)
                {
                    NPC.ai[0] = EST_VOLAR; NPC.ai[1] = 0;
                    ElegirRumbo(presa);
                    if (Main.netMode != NetmodeID.Server) OndaLib.Kick(8f, 20);
                }
            }
        }

        // ── LA MUERTE: la explosión de luz (la firma del dios) ──
        private void AIMuerte(Player presa)
        {
            NPC.velocity *= 0.92f;
            NPC.dontTakeDamage = true;
            if (T == 1f)
            {
                Sonar(SoundID.Item74, NPC.Center);
                if (Main.netMode != NetmodeID.Server)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<Projectiles.Jefes.AtaqueJefeProjectile>(),
                        0, 0f, NPC.target,
                        Projectiles.Jefes.AtaqueJefeProjectile.EstiloMuerteFinal);
                    OndaLib.Kick(16f, 34);
                }
            }
            if (T >= 60f) NPC.active = false;
        }

        public override bool CheckDead()
        {
            // la última luz (jamas el crujido vanilla)
            if (Estado != EST_MUERTE)
            {
                NPC.life = 1;
                NPC.ai[0] = EST_MUERTE; NPC.ai[1] = 0;
                NPC.netUpdate = true;
                return false;
            }
            return true;
        }

        // ════════════════════════════════════════════════════════════
        //  LOS TRES OJOS SELLADOS (minions CON barra, reviven 30 s)
        // ════════════════════════════════════════════════════════════
        private void MantenerOjos(Player presa)
        {
            int tipoOjo = ModContent.NPCType<AethonSelladoOjo>();
            int vivos = 0;
            int[] idxOjos = new int[OJOS_MAX];
            for (int i = 0; i < Main.maxNPCs && vivos < OJOS_MAX; i++)
            {
                NPC n = Main.npc[i];
                if (n != null && n.active && n.type == tipoOjo &&
                    (n.ai[3] == NPC.whoAmI))
                    idxOjos[vivos++] = i;
            }

            // v6.145 — APUNTA LAS GENERACIONES de los ojos vivos (para
            // que el renacido sea MÁS PODEROSO que el que murió)
            for (int i = 0; i < vivos; i++)
                _generaciones[i] = (int)(Main.npc[idxOjos[i]].ai[1] / 10000f);

            // los muertos esperan su reloj (30 s) — el sello RE-ESCRIBE
            for (int i = vivos; i < OJOS_MAX; i++)
            {
                if (Estado == EST_MUERTE) break;     // el dios muerto no llama
                _revivirEn[i]--;
                if (_revivirEn[i] <= 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 pos = NPC.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 140f;
                    int idx = NPC.NewNPC(NPC.GetSource_FromAI(), (int)pos.X, (int)pos.Y,
                        tipoOjo, 0, NPC.whoAmI, i, Main.rand.NextFloat(MathHelper.TwoPi), NPC.target);
                    if (idx >= 0 && idx < Main.maxNPCs)
                    {
                        NPC ojo = Main.npc[idx];
                        // v6.145 — LA GENERACIÓN SUBE: cada ojo renace
                        // MÁS PODEROSO (+1 gen → +35% vida, +20% daño —
                        // la escala la aplica el ojo en su primera AI)
                        int gen = 1 + (_generaciones != null && i < _generaciones.Length
                            ? _generaciones[i] : 0);
                        ojo.ai[1] = gen * 10000f;
                        ojo.netUpdate = true;
                    }
                    _revivirEn[i] = REVIVIR_T;
                    if (Main.netMode != NetmodeID.Server)
                    {
                        Sonar(SoundID.Item122.WithPitchOffset(0.4f), pos);
                        for (int k = 0; k < 3; k++)
                            SombrasLib.Bruma(pos, 40f, 0.6f,
                                Main.GlobalTimeWrappedHourly, idx * 7 + k);
                    }
                }
            }

            // la ÓRBITA — LA CORONA DE LOS DIECISIETE: dos anillos
            // (11 afuera + 6 adentro girando CONTRARIO — el reloj del
            // sello), con el vaivén del libro
            float t = Main.GlobalTimeWrappedHourly;
            for (int i = 0; i < vivos; i++)
            {
                NPC ojo = Main.npc[idxOjos[i]];
                bool anilloExt = i < 11;
                float ang = t * (anilloExt ? 0.75f : -1.1f) +
                    i * (MathHelper.TwoPi / (anilloExt ? 11f : 6f));
                float radio = anilloExt ? 190f : 100f;
                Vector2 destino = NPC.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.72f)
                    * radio + new Vector2(0f, MathF.Sin(t * 1.3f + i) * 6f);
                ojo.Center = Vector2.Lerp(ojo.Center, destino, 0.14f);
                ojo.velocity = Vector2.Zero;
            }
        }

        // ════════════════════════════════════════════════════════════
        //  EL ARSENAL (cada ataque lo dispara UN ojo — el libro dirige)
        //  v6.156 — SUS PROPIOS ATAQUOS A MAYOR ESCALA (la letra:
        //  «el jefe Aethon sellado que es el grimorio mismo con sus
        //  propios ataques pero a mayor escala»): el arsenal se AMPLÍA
        //  con LOS ATAQUES DEL GRIMORIO MISMO — la PAREJA DORADA/
        //  VIOLETA del disparo básico (los discos día/noche del
        //  Equinoccio, ×3 en abanico y con daño de jefe) y LA SOMBRAS
        //  DE LA PÁGINA (los tentáculos negros del arma —
        //  SombraPaginaProjectile con la marca del festín). El
        //  repertorio viejo (púa, coro, virote, flecha, tajo,
        //  estallido) QUEDA — el dios tiene TODO su arsenal.
        // ════════════════════════════════════════════════════════════
        private void DispararArsenal(Player presa)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            // el ojo que dispara (uno vivo; si no, el libro mismo)
            Vector2 cuna = NPC.Center;
            int tipoOjo = ModContent.NPCType<AethonSelladoOjo>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n != null && n.active && n.type == tipoOjo && n.ai[3] == NPC.whoAmI)
                { cuna = n.Center; break; }
            }

            Vector2 rumbo = (presa.Center - cuna).SafeNormalize(Vector2.UnitX);

            // ============================================================
            //  v6.156 — LA MONEDA DEL ARSENAL AMPLIADO: mitad del tiempo
            //  LOS ATAQUES DEL GRIMORIO (la pareja de discos ×3 en
            //  abanico / la sombra de la página), mitad el repertorio
            //  de siempre (el arsenal de AtaqueJefeProjectile).
            // ============================================================
            bool ataqueDelLibro = Main.rand.NextBool(2);
            if (ataqueDelLibro)
            {
                int dado = Main.rand.Next(3);
                if (dado < 2)
                {
                    // LA PAREJA DORADA/VIOLETA A MAYOR ESCALA: el disparo
                    // básico del grimorio (los discos día/noche 931 con
                    // LAS MARCAS DEL FX — el patrón de LanzarPareja del
                    // arma) pero EN TRIPLICA: tres parejas en abanico
                    // (−0.35, 0, +0.35 rad) — el dios escupe SU básico
                    // por la boca ancha.
                    int dano = (int)(NPC.damage * 0.42f);
                    for (int k = -1; k <= 1; k++)
                    {
                        Vector2 velLibro = rumbo.RotatedBy(0.30f * k) * 13f;
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), cuna,
                            velLibro.RotatedBy(-0.30f), 931, dano, 2f, NPC.target,
                            -1f, 0.64f, 9201f);   // EL DORADO (día)
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), cuna,
                            velLibro.RotatedBy(0.30f), 931, dano, 2f, NPC.target,
                            -1f, 0.28f, 9202f);   // EL VIOLETA (noche)
                    }
                    Sonar(SoundID.Item122.WithPitchOffset(0.10f), cuna);
                }
                else
                {
                    // LA SOMBRA DE LA PÁGINA DEL DIOS: el tentáculo negro
                    // del arma (SombraPaginaProjectile) con la marca del
                    // festín — el dios SUelta su propio tentáculo a por
                    // la presa (muerde 2,5% por golpe: el arma a escala).
                    int idxS = Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        cuna, Vector2.Zero,
                        ModContent.ProjectileType
                            <global::AethonMod.Content.Projectiles.Sombras.SombraPaginaProjectile>(),
                        (int)(NPC.damage * 0.60f), 3f, NPC.target,
                        presa.whoAmI + 1);
                    if (idxS >= 0)
                    {
                        // el jefe manda: la sombra persigue a SU presa
                        Main.projectile[idxS].friendly = false;
                        Main.projectile[idxS].hostile = true;
                    }
                    Sonar(SoundID.Item122.WithPitchOffset(-0.30f), cuna);
                }
                return;
            }

            int estilo = ARSENAL[Main.rand.Next(ARSENAL.Length)];
            float vel = estilo == Projectiles.Jefes.AtaqueJefeProjectile.EstiloEstallidoRadiante
                ? 0f : 10f;
            Projectile.NewProjectile(NPC.GetSource_FromAI(), cuna, rumbo * vel,
                ModContent.ProjectileType<Projectiles.Jefes.AtaqueJefeProjectile>(),
                (int)(NPC.damage * 0.55f), 2f, NPC.target, estilo,
                NPC.whoAmI * 1.7f, NPC.whoAmI);
            Sonar(SoundID.Item122.WithPitchOffset(0.25f), cuna);
        }
        private const int ARSELADO_LEN = 6;

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); }
            catch { }
        }

        // ════════════════════════════════════════════════════════════
        //  EL DIBUJO DEL DIOS (el sprite del Nervioso GIGANTE + ojos)
        // ════════════════════════════════════════════════════════════
        //  EL DIBUJO DEL DIOS — v6.156: EL CUERPO ES EL LIBRO V3 DEL
        //  USUARIO (la letra: «tambien te dare el del grimorio para que
        //  lo uses como el jefe Aethon sellado que es el grimorio mismo
        //  con sus propios ataques pero a mayor escala, ademas debes
        //  darle una animacion rapido, como los minions en la pantalla
        //  de titulo y en el pilar»): LAS TRES TEXTURAS DEL SPRITE
        //  LIBRO V3 (LibroV3_Abierto / LibroV3_Medio / LibroV3_Cerrado
        //  — el arte del usuario decodificado PIXEL A PIXEL, 706×967)
        //  dibujadas a escala de dios (×0.42: el cuerpo del jefe mide
        //  ~296×406 px de pantalla) con EL PARPADEO RÁPIDO de los
        //  minions del título y del pilar: el ciclo ABIERTO → MEDIO →
        //  CERRADO → MEDIO que late cada ~2 segundos y se aprieta en
        //  la furia (a media vida late al doble — el dios nervioso de
        //  verdad).
        // ════════════════════════════════════════════════════════════
        public override bool PreDraw(SpriteBatch sb, Vector2 screenPos, Color drawColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            try
            {
                // LAS TRES CARAS DEL LIBRO V3 (el arte del usuario):
                // la tira del dios — abierto, medio, cerrado.
                var texAbiertoAsset = ModContent.Request<Texture2D>(
                    "AethonMod/Content/NPCs/LibroV3_Abierto",
                    ReLogic.Content.AssetRequestMode.ImmediateLoad);
                var texMedioAsset = ModContent.Request<Texture2D>(
                    "AethonMod/Content/NPCs/LibroV3_Medio",
                    ReLogic.Content.AssetRequestMode.ImmediateLoad);
                var texCerradoAsset = ModContent.Request<Texture2D>(
                    "AethonMod/Content/NPCs/LibroV3_Cerrado",
                    ReLogic.Content.AssetRequestMode.ImmediateLoad);
                Texture2D texAbierto = texAbiertoAsset.Value;
                Texture2D texMedio = texMedioAsset.Value;
                Texture2D texCerrado = texCerradoAsset.Value;

                Vector2 pos = NPC.Center - screenPos;
                float t = Main.GlobalTimeWrappedHourly + NPC.whoAmI * 0.37f;

                // el vaivén del flotante (la firma)
                float bob = MathF.Sin(t * 1.35f) * 8f;
                float tilt = NPC.rotation * 0.35f + 0.07f * MathF.Sin(t * 0.9f);
                Vector2 destino = pos + new Vector2(0f, bob);

                // EL HALO del dios sellado
                var glow = VFXCore.SoftGlow;
                sb.Draw(glow, destino, null,
                    new Color(118, 74, 190) * (0.30f * NPC.Opacity), 0f,
                    glow.Size() * 0.5f, 5.5f, SpriteEffects.None, 0f);

                // ============================================================
                //  EL PARPADEO RÁPIDO DE LOS MINIONS (la referencia de la
                //  letra: la pantalla de título y el pilar): el ciclo
                //  ABIERTO → MEDIO → CERRADO → MEDIO → ABIERTO… con el
                //  compás de la casa — un latido cada T_PARPADEO
                //  segundos, y el párpado baja y sube EN FRENA (las
                //  fases del cierre duran un cuarto del ciclo: RÁPIDO
                //  de verdad). La furia aprieta el compás: a media vida
                //  el dios parpidea AL DOBLE (nervioso como el libro
                //  hambriento).
                // ============================================================
                float compas = Furia > 0.5f ? 0.85f : 1.70f;   // furia = al doble
                float faseParp = (t % compas) / compas;          // 0..1
                Texture2D tex;
                // EL CIERRE EN FRENA: abierto (0-0.55), medio (0.55-0.70),
                // cerrado (0.70-0.80), medio (0.80-0.95), abierto (0.95-1).
                if (faseParp < 0.55f || faseParp >= 0.95f) tex = texAbierto;
                else if (faseParp < 0.80f) tex = (faseParp < 0.70f) ? texMedio : texCerrado;
                else tex = texMedio;

                // EL LIBRO V3 GIGANTE (706×967 → ×0.42 ≈ 296×406 px: EL
                // GRIMORIO MISMO, el arte del usuario a escala de dios)
                float escala = 0.42f;
                sb.Draw(tex, destino, null,
                    Color.White * NPC.Opacity, tilt,
                    tex.Size() * 0.5f, escala,
                    SpriteEffects.None, 0f);

                // LOS OJOS SELLADOS sobre la portada: los sellos vivos
                // del libro (cada uno con su NPC-ojo orbitando — el
                // dibujo del NPC-ojo manda; estos son los sellos de la
                // PORTADA, apagados, el contrato) — SIGUEN el compás
                // del parpadeo del cuerpo (cerrado = sellos dormidos).
                float alfaSello = (tex == texCerrado) ? 0.12f
                    : (tex == texMedio) ? 0.24f : 0.35f;
                var ojoTexAsset = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Projectiles/OjoEterno_Abierto",
                    ReLogic.Content.AssetRequestMode.ImmediateLoad);
                var ojoTex = ojoTexAsset.Value;
                for (int i = 0; i < OJOS_MAX; i++)
                {
                    Vector2 off = new Vector2(
                        (i - 1) * 26f, -18f + (i == 1 ? -12f : 0f));
                    sb.Draw(ojoTex, destino + off, null,
                        new Color(150, 110, 210) * (alfaSello * NPC.Opacity), 0f,
                        ojoTex.Size() * 0.5f, 0.55f, SpriteEffects.None, 0f);
                }
            }
            catch { }
            return false;
        }
    }

    // ==================================================================
    //  AethonSelladoOjo — EL OJO SELLADO (el minion con barra de vida).
    //  La textura del escuadrón (OjoEterno) con SU animación (Abierto →
    //  Medio → Cerrado — el parpadeo del minion), orbita al libro y
    //  revive a los 30 s cuando el sello lo re-escribe.
    // ==================================================================
    public class AethonSelladoOjo : ModNPC
    {
        public override string Texture => "AethonMod/Content/Projectiles/OjoEterno_Abierto";

        private int Amo => (int)NPC.ai[0];
        private float Angulo => NPC.ai[2];
        // v6.145 — LA GENERACIÓN (la letra: «cada ojo debe ser mas
        // poderoso cada vez que se regeneren»): cada resurrección
        // sube la generación del ojo (+35% vida, +20% daño — el sello
        // LO ESCRIBE MÁS FUERTE). La generación vive en ai[1] (alta) —
        // el T del ojo se lleva a localAI.
        private int Generacion => (int)(NPC.ai[1] / 10000f);
        private float T => NPC.localAI[0];

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 44;
            NPC.height = 44;
            NPC.lifeMax = 25_000;
            NPC.damage = 70;
            NPC.defense = 30;
            NPC.knockBackResist = 0.15f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.aiStyle = -1;
            NPC.npcSlots = 1f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
        }

        private bool _primeraAI = true;

        public override void AI()
        {
            NPC.localAI[0]++;
            NPC.TargetClosest();

            // v6.145 — LA ESCALA DE LA GENERACIÓN (la letra: «cada ojo
            // debe ser mas poderoso cada vez que se regeneren»): al
            // nacer (o renacer), el ojo toma SU generación y crece —
            // +35% vida y +20% daño por generación. El sello aprende.
            if (_primeraAI)
            {
                _primeraAI = false;
                int gen = Math.Max(0, Generacion);
                if (gen > 0)
                {
                    float mul = 1f + 0.35f * gen;
                    NPC.lifeMax = (int)(25_000 * mul);
                    NPC.life = NPC.lifeMax;
                    NPC.damage = (int)(70 * (1f + 0.20f * gen));
                }
            }

            // EL AMO murió: el sello se disuelve
            NPC amo = Amo >= 0 && Amo < Main.maxNPCs ? Main.npc[Amo] : null;
            if (amo == null || !amo.active ||
                amo.type != ModContent.NPCType<AethonSellado>())
            {
                NPC.life = 0;
                NPC.HitEffect(0, 10);
                NPC.active = false;
                return;
            }

            // la órbita la lleva el amo (MantenerOjos); el ojo solo
            // respira (el bob del minion) y MIRA a su presa
            NPC.Center += new Vector2(0f, MathF.Sin(Main.GlobalTimeWrappedHourly * 2.2f
                + Angulo) * 0.5f);
            NPC.velocity = Vector2.Zero;
        }

        public override bool PreDraw(SpriteBatch sb, Vector2 screenPos, Color drawColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            try
            {
                // LA ANIMACIÓN DEL MINION (la letra: «las animaciones
                // están creadas solo mira como animaste al grimorio y
                // sus minions»): el parpadeo del escuadrón — Abierto la
                // mayor parte del ciclo, Medio un instante, Cerrado el
                // descanso — el mismo mapa del pilar.
                float t = Main.GlobalTimeWrappedHourly + Angulo * 3.1f;
                float fase = (t * 0.55f) % 1f;
                string estado = fase < 0.55f ? "Abierto"
                    : fase < 0.68f ? "Medio" : "Cerrado";
                var texAsset = ModContent.Request<Texture2D>(
                    $"AethonMod/Content/Projectiles/OjoEterno_{estado}",
                    ReLogic.Content.AssetRequestMode.ImmediateLoad);
                var tex = texAsset.Value;

                // MIRA a su presa (el iris sigue al objetivo)
                Player presa = Main.player[NPC.target];
                Vector2 mirada = presa != null && presa.active
                    ? (presa.Center - NPC.Center).SafeNormalize(Vector2.UnitX)
                    : Vector2.UnitX;
                float rot = mirada.ToRotation();

                var glow = VFXCore.SoftGlow;
                Vector2 pos = NPC.Center - screenPos;
                sb.Draw(glow, pos, null,
                    new Color(150, 90, 220) * 0.4f, 0f,
                    glow.Size() * 0.5f, 1.4f, SpriteEffects.None, 0f);
                // v6.145 — EL TAMAÑO DEL PODER: cada generación lo
                // agranda (+12% por gen — el sello lo escribe más fuerte)
                float escGen = 0.85f * (1f + 0.12f * Math.Max(0, Generacion));
                sb.Draw(tex, pos, null, Color.White, rot,
                    tex.Size() * 0.5f, escGen, SpriteEffects.None, 0f);
            }
            catch { }
            return false;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.netMode != NetmodeID.Server && NPC.life <= 0)
                for (int i = 0; i < 4; i++)
                    SombrasLib.Bruma(NPC.Center, 34f, 0.55f,
                        Main.GlobalTimeWrappedHourly, NPC.whoAmI * 11 + i);
        }

        public override bool CheckActive() => false;   // el sello lo sostiene
        public override void BossHeadRotation(ref float rotation)
            => rotation = NPC.rotation;
    }

    // ==================================================================
    //  LA BARRA DEL JEFE (la propia — el dios sellado tiene la suya)
    // ==================================================================
    public class BarraAethonSellado : ModBossBar
    {
        public override bool? ModifyInfo(ref BigProgressBarInfo info,
            ref float life, ref float lifeMax, ref float scale, ref float dps)
        {
            // el NPC enfocado por el juego ES el dios sellado → null
            // (la lógica básica pinta su vida); si no, NO dibujar
            if (info.npcIndexToAimAt < 0 || info.npcIndexToAimAt >= Main.maxNPCs)
                return false;
            NPC n = Main.npc[info.npcIndexToAimAt];
            return n != null && n.active && n.type == ModContent.NPCType<AethonSellado>()
                ? (bool?)null : false;
        }
    }
}
