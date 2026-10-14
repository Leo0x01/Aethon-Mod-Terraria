using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Systems;
using AethonMod.Content.Projectiles.Jefes;

namespace AethonMod.Content.NPCs
{
    // ======================================================================
    //  v6.50.117 — EL RECUERDO COMPLETO (la letra del usuario): «cuando
    //  ambos lleguen a 1000, los dos jefes se fusionan en uno solo, ese
    //  sería más fuerte con ataques más vistosos».
    //
    //  Nace SOLO de LA FUSIÓN (la cinemática de la base lo trae al mundo
    //  con la vida entera, donde los dos fragmentos se abrazaron). Su
    //  cuerpo es UN ECLIPSE: el sol dorado de la base (heredado intacto)
    //  con LA MITAD OSCURA deslizándose encima — mitad oro, mitad
    //  negro-morado, el dios completo que fue.
    //
    //  EL MOVESET NUEVO (la letra, punto por punto — reusa las
    //  primitivas del dios: pernos, novas y columnas de siempre, con
    //  lógica nueva):
    //    a · EL ECLIPSE GEMELO — la media pantalla ACTIVA alterna: la
    //        lluvia cae del lado que ARDE y el color inactivo se dibuja
    //        tenue (corre al lado que no duele).
    //    b · EL SOL MORIBUNDO — el sol negro GIGANTE (×1.8) LENTO que
    //        ATRAE al jugador (<900 px): castiga la calma, no el reflejo.
    //    c · LA REJILLA LÁSER BICOLOR — barras horizontales y verticales
    //        alternando el color activo (uno duele, el otro es aviso).
    //    d · LA LLUVIA DE PÁGINAS — páginas de tinta girando en oleadas,
    //        con rayos dorados cosiendo entre ellas.
    //    e · LOS ANILLOS DE FUSIÓN — la nova del dios, duplicada: anillos
    //        expandiéndose que ALTERNAN luz y oscuridad.
    //    f · EL ÚLTIMO ALIENTO — a vida ≤1% deja de atacar: se separa
    //        3 s en las DOS siluetas (ambas MIRAN al jugador — el
    //        núcleo-ojo apunta), se disuelven en bruma… y el CINE DE
    //        MUERTE de siempre (con el botín COMPLETO + 1 esencia extra
    //        que compensa los fragmentos que nunca dropearon).
    //
    //  LAS REGLAS HEREDADAS: máximo UN ataque grande a la vez (ya es
    //  así por estados), el batón de turnos NO le aplica (no tiene
    //  hermano) y el mediodía eterno lo sostiene AethonLlegadaSistema
    //  (que ya cuenta al fusionado entre los vivos).
    // ======================================================================
    public class AethonBossFusionado : AethonBoss
    {
        // === LOS ESTADOS PROPIOS DEL RECUERDO COMPLETO (los que la base
        //     no conoce — su switch se los pasa a EstadoFusionado) ===
        private const int F_ECLIPSE = 30;    // a — EL ECLIPSE GEMELO
        private const int F_SOLMOR = 31;     // b — EL SOL MORIBUNDO
        private const int F_REJILLA = 32;    // c — LA REJILLA LÁSER BICOLOR
        private const int F_PAGINAS = 33;    // d — LA LLUVIA DE PÁGINAS
        private const int F_ANILLOS = 34;    // e — LOS ANILLOS DE FUSIÓN
        private const int F_ALIENTO = 35;    // f — EL ÚLTIMO ALIENTO (≤1%)

        /// <summary>EL GATILLO DEL 1% vive UNA sola vez (la despedida no se repite).</summary>
        private bool _alientoDado = false;

        // El cuerpo es 100% código: se reusa la textura del jefe de luz
        // como ancla del motor (jamás se ve — el PreDraw manda).
        public override string Texture => "AethonMod/Content/NPCs/AethonBoss";

        // La cabeza de la barra: EL SOL del recuerdo (el eclipse entero).
        public override string BossHeadTexture => "AethonMod/Content/NPCs/AethonBoss_Head_Boss";

        // === LOS ANCLAJES DE LA BASE (los tres que lo cambian todo) ===

        /// <summary>ES EL RECUERDO COMPLETO: sin hermano, sin órbita, sin fusión.</summary>
        protected override bool EsFusion => true;

        /// <summary>Nace del abrazo de la fusión: NUNCA corre la llegada
        /// (nace YA en la órbita de pelea, dentro del flash).</summary>
        protected override bool HaceLlegada => false;

        /// <summary>Su hermano no existe (0 = la IA de fusión lo salta).</summary>
        protected override int TipoHermano => 0;

        /// <summary>En el ÚLTIMO ALIENTO todo calla (la despedida no dispara).</summary>
        protected override bool SilencioFinal => _estado == F_ALIENTO;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            // El DisplayName vive en la localización (NPCs.AethonBossFusionado).
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            // LA FUERZA DE AMBOS (la letra: «más fuerte»): la vida entera
            // del dios original — pero con el moveset nuevo completo.
            NPC.lifeMax = 2_400_000;
        }

        protected override void PasoFusionPrevia()
        {
            base.PasoFusionPrevia();   // (la base vuelve YA: EsFusion no busca hermano)

            // EL ÚLTIMO ALIENTO (la letra): a vida ≤1% el recuerdo DEJA de
            // atacar y se DESPIDE — la separación en las dos siluetas la
            // dibuja el PreDraw, la disolución la corre el estado.
            if (!_alientoDado && !_muriendo &&
                _estado != F_ALIENTO && _estado != EST_MURIENDO &&
                NPC.life * 100f <= NPC.lifeMax)
            {
                _alientoDado = true;
                _coreoLargo = 0;
                _estado = F_ALIENTO;
                _tickEstado = 0;
                NPC.dontTakeDamage = true;   // 3 s de gracia: la despedida no se interrumpe a golpes
                NPC.netUpdate = true;        // MP: el cambio de estado viaja YA
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item117.WithPitchOffset(-0.5f), NPC.Center);
                OndaLib.Kick(9f, 20);
            }
        }

        /// <summary>
        /// EL MOVESET DEL RECUERDO COMPLETO: las CINCO armas de la fusión,
        /// rotando SIN repetir (ni el último plato ni el de atrás — la
        /// memoria doble de la casa). Máximo UN ataque grande a la vez:
        /// ya es así por estados.
        /// </summary>
        protected override void ElegirAtaque(Player target)
        {
            int[] menu = { F_ECLIPSE, F_SOLMOR, F_REJILLA, F_PAGINAS, F_ANILLOS };
            int elegido = menu[Main.rand.Next(menu.Length)];
            int intentos = 0;
            while ((elegido == _ultimoAtaque || elegido == _anteultimoAtaque) &&
                intentos++ < menu.Length)
                elegido = menu[(Array.IndexOf(menu, elegido) + 1) % menu.Length];
            _anteultimoAtaque = _ultimoAtaque;
            _ultimoAtaque = elegido;
            _estado = elegido;
            _tickEstado = 0;
            NPC.netUpdate = true;
        }

        /// <summary>
        /// LOS FONDOS DE FASE DEL RECUERDO COMPLETO: sin decreto y sin
        /// especial de fase (el moveset es SUYO) — pero la furia de
        /// siempre sigue: la cura de fase, el rugido y el anuncio.
        /// </summary>
        protected override void OnPhaseChange()
        {
            NPC.life = Math.Min(NPC.lifeMax, NPC.life + NPC.lifeMax / 20);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            OndaLib.Kick(10f, 20);
            EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Fase",
                PaletaOro, Phase, PhaseName());
            NPC.netUpdate = true;
        }

        /// <summary>EL DESPACHO de los estados propios (los ≥30).</summary>
        protected override bool EstadoFusionado(int estado, Player target)
        {
            switch (estado)
            {
                case F_ECLIPSE: EclipseGemelo(target); return true;
                case F_SOLMOR: SolMoribundo(target); return true;
                case F_REJILLA: RejillaLaser(target); return true;
                case F_PAGINAS: LluviaPaginas(target); return true;
                case F_ANILLOS: AnillosFusion(target); return true;
                case F_ALIENTO: UltimoAliento(target); return true;
            }
            return false;
        }

        // ==================================================================
        //  (a) EL ECLIPSE GEMELO — la media pantalla que ARDE alterna
        // ==================================================================
        private void EclipseGemelo(Player target)
        {
            // EL EJE DEL ECLIPSE: el dios se alza sobre la arena (la
            // izquierda es ORO, la derecha OSCURIDAD — los velos los
            // pinta el PreDraw en cada pantalla).
            Vector2 punto = target.Center + new Vector2(0f, -460f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.06f, 0.12f);

            if (_tickEstado == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item117.WithPitchOffset(-0.35f), NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Decreto", PaletaOro);
                OndaLib.Kick(8f, 18);
                NPC.netUpdate = true;
            }

            // TRES RONDAS de 100 t: ronda 0 arde la MITAD DORADA
            // (izquierda), ronda 1 la OSCURA (derecha), ronda 2 la dorada
            // de nuevo — la lluvia cae SOLO del lado que arde.
            int ronda = Math.Min(2, _tickEstado / 100);
            bool ardeIzq = (ronda & 1) == 0;

            if (_tickEstado < 300 && (_tickEstado % 5) == 0 && (_tickEstado % 100) >= 15 &&
                Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int k = 0; k < 2; k++)
                {
                    float lado = ardeIzq ? -1f : 1f;
                    float x = NPC.Center.X + lado * Main.rand.NextFloat(80f, 620f);
                    float y = target.Center.Y - Main.rand.NextFloat(760f, 900f);
                    Vector2 v = new Vector2(Main.rand.NextFloat(-1.2f, 1.2f),
                        Main.rand.NextFloat(8.5f, 11f));
                    // LA PALETA DEL LADO: izquierda = pernos de LUZ (ai 0),
                    // derecha = pernos de OSCURIDAD (ai 2) — el color que
                    // arde es el color que duele.
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        x, y, v.X, v.Y,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.55f), 2f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloPernoEstelar, ardeIzq ? 0f : 2f,
                        NPC.whoAmI * 61 + _tickEstado * 3 + k);
                }
            }

            if (_tickEstado >= 340) CerrarEstado();
        }

        // ==================================================================
        //  (b) EL SOL MORIBUNDO — el gigante lento que ATRAE
        // ==================================================================
        private void SolMoribundo(Player target)
        {
            // EL DIOS OBSERVA desde arriba: el sol negro caza solo.
            Vector2 punto = target.Center + new Vector2(0f, -500f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.05f, 0.10f);

            if (_tickEstado == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item117.WithPitchOffset(-0.25f), NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Sol", PaletaOro);
                OndaLib.Kick(8f, 16);
                NPC.netUpdate = true;
            }

            // EL LANZAMIENTO (t=20): el retroceso del cañonazo y el sol
            // negro nace rumbo a la presa — LENTO (6 px/t), GIGANTE (×1.8
            // la talla del sol del dios) y con ~9 s de agonía. La
            // ATRACCIÓN la aplica TraccionRecuerdoPlayer en cada jugador.
            if (_tickEstado == 20 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int dano = Math.Max(1, (int)(NPC.damage * 1.05f));
                Vector2 rumbo = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitY) * 6f;
                Projectile.NewProjectile(NPC.GetSource_FromAI(),
                    NPC.Center.X, NPC.Center.Y, rumbo.X, rumbo.Y,
                    ModContent.ProjectileType<AtaqueFusionProjectile>(),
                    dano, 3f, Main.myPlayer,
                    AtaqueFusionProjectile.EstiloSolMoribundo, dano, NPC.whoAmI);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, NPC.Center);
                NPC.netUpdate = true;
            }
            else if (_tickEstado <= 20 && _tickEstado > 8)
            {
                // EL ARMA CARGA: retrocede un pelo antes del cañonazo.
                Vector2 atras = (NPC.Center - target.Center)
                    .SafeNormalize(Vector2.UnitY) * 1.2f;
                NPC.velocity = Vector2.Lerp(NPC.velocity, atras, 0.08f);
            }

            // El estado suelta ANTES: el sol vive SOLO sus 9 s (el dios
            // sigue peleando mientras el moribundo caza — el hazardo
            // persistente de la casa, como el reloj de la fase 2).
            if (_tickEstado >= 120) CerrarEstado();
        }

        // ==================================================================
        //  (c) LA REJILLA LÁSER BICOLOR — el tablero que alterna
        // ==================================================================
        private void RejillaLaser(Player target)
        {
            // EL DIOS SE CLAVA sobre la presa: la rejilla se teje alrededor.
            Vector2 punto = target.Center + new Vector2(0f, -520f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.05f, 0.10f);

            if (_tickEstado == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item117.WithPitchOffset(0.15f), NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Danza", PaletaOro);
                OndaLib.Kick(7f, 16);
                NPC.netUpdate = true;
            }

            // LA SIEMBRA (t=30): SEIS barras alrededor de la presa — tres
            // HORIZONTALES (ai[1] 0-2) y tres VERTICALES (ai[1] 3-5), con
            // el COLOR por paridad del índice (par ORO, impar OSCURIDAD):
            // el tablero queda tejido y el ciclo alterna quién arde.
            if (_tickEstado == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 6; i++)
                {
                    bool vert = i >= 3;
                    Vector2 pos = vert
                        ? new Vector2(target.Center.X + (i - 4) * 320f, target.Center.Y)
                        : new Vector2(target.Center.X, target.Center.Y + (i - 1) * 200f);
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        pos.X, pos.Y, 0f, 0f,
                        ModContent.ProjectileType<AtaqueFusionProjectile>(),
                        (int)(NPC.damage * 0.60f), 0f, Main.myPlayer,
                        AtaqueFusionProjectile.EstiloRejillaLaser, i, NPC.whoAmI * 53 + i);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122.WithPitchOffset(0.3f), NPC.Center);
                NPC.netUpdate = true;
            }

            // La rejilla vive SOLA sus ~7 s (el proyectil se apaga a los
            // 450 t) — el dios la vigila desde arriba TODO ese tiempo.
            if (_tickEstado >= 470) CerrarEstado();
        }

        // ==================================================================
        //  (d) LA LLUVIA DE PÁGINAS — la biblioteca se vierte
        // ==================================================================
        private void LluviaPaginas(Player target)
        {
            // EL DIOS SE ALZA y arranca las páginas de tinta.
            Vector2 punto = target.Center + new Vector2(0f, -480f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (punto - NPC.Center) * 0.06f, 0.12f);

            if (_tickEstado == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70.WithPitchOffset(-0.3f), NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Coro", PaletaOro);
                OndaLib.Kick(6f, 14);
                NPC.netUpdate = true;
            }

            // LAS TRES OLEADAS (t=30/90/150): diez páginas de tinta cayendo
            // en abanico sobre la presa — girando, con estela morada,
            // rompiéndose contra el suelo.
            for (int o = 0; o < 3; o++)
            {
                if (_tickEstado == 30 + o * 60 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int p = 0; p < 10; p++)
                    {
                        float x = target.Center.X + (p - 4.5f) * 110f +
                            Main.rand.NextFloat(-40f, 40f);
                        float y = target.Center.Y - 780f - Main.rand.NextFloat(0f, 70f);
                        Vector2 v = new Vector2(Main.rand.NextFloat(-1f, 1f),
                            Main.rand.NextFloat(3f, 5f));
                        Projectile.NewProjectile(NPC.GetSource_FromAI(),
                            x, y, v.X, v.Y,
                            ModContent.ProjectileType<AtaqueFusionProjectile>(),
                            (int)(NPC.damage * 0.50f), 2f, Main.myPlayer,
                            AtaqueFusionProjectile.EstiloPaginaTinta, 0f,
                            p + o * 16 + NPC.whoAmI * 7);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item9.WithPitchOffset(-0.2f), NPC.Center);
                    NPC.netUpdate = true;
                }
            }

            // LOS RAYOS DORADOS ENTRE OLEADAS (t=60/120/180): tres pernos
            // de LUZ cosiendo el hueco entre las páginas — la mitad dorada
            // del recuerdo reclama su turno.
            if ((_tickEstado == 60 || _tickEstado == 120 || _tickEstado == 180) &&
                Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 pred = PredPresa(target);
                for (int r = 0; r < 3; r++)
                {
                    Vector2 pos = NPC.Center + new Vector2(
                        Main.rand.NextFloat(-160f, 160f), Main.rand.NextFloat(-20f, 60f));
                    float ang = (pred - pos).ToRotation() + (r - 1) * 0.13f;
                    Vector2 v = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 12f;
                    Projectile.NewProjectile(NPC.GetSource_FromAI(),
                        pos.X, pos.Y, v.X, v.Y,
                        ModContent.ProjectileType<AtaqueJefeProjectile>(),
                        (int)(NPC.damage * 0.45f), 2f, Main.myPlayer,
                        AtaqueJefeProjectile.EstiloPernoEstelar, 0f,
                        NPC.whoAmI * 97 + _tickEstado + r);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, NPC.Center);
            }

            if (_tickEstado >= 240) CerrarEstado();
        }

        // ==================================================================
        //  (e) LOS ANILLOS DE FUSIÓN — la nova doble que alterna
        // ==================================================================
        private void AnillosFusion(Player target)
        {
            // LA CONTRACCIÓN: se detiene y se llena (la nova de siempre).
            NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Zero, 0.15f);

            if (_tickEstado == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item117, NPC.Center);
                EcoRed.AnunciarMundo("Mods.AethonMod.Jefe.Aethon.Nova", PaletaOro);
                OndaLib.Kick(6f, 12);
                NPC.netUpdate = true;
            }

            // TRES ANILLOS (t=45/70/95) que ALTERNAN luz y oscuridad —
            // cada uno MÁS RÁPIDO que el anterior (la expansión que se
            // come la arena), con los cuatro huecos de la nova de la casa.
            int[] ticksDet = { 45, 70, 95 };
            for (int a = 0; a < 3; a++)
            {
                if (_tickEstado == ticksDet[a] && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float vel = 6.5f + a * 2.4f;
                    float desfase = a * MathHelper.Pi / 9f;
                    float hueco = 0.20f;
                    float paleta = (a & 1) == 0 ? 0f : 2f;   // anillo 0 y 2: LUZ · 1: OSCURIDAD
                    for (int i = 0; i < 18; i++)
                    {
                        float ang = i * MathHelper.TwoPi / 18f + desfase;
                        float grad = Math.Abs(MathHelper.WrapAngle(ang - desfase));
                        if (grad < hueco || Math.Abs(grad - MathHelper.PiOver2) < hueco ||
                            Math.Abs(grad - MathHelper.Pi) < hueco ||
                            Math.Abs(grad - MathHelper.Pi * 1.5f) < hueco) continue;
                        Vector2 dir = new Vector2(MathF.Cos(ang), MathF.Sin(ang));
                        Vector2 tan = new Vector2(-dir.Y, dir.X) * (1.1f + a * 0.55f);
                        Vector2 v = dir * vel + tan;
                        Projectile.NewProjectile(NPC.GetSource_FromAI(),
                            NPC.Center.X, NPC.Center.Y, v.X, v.Y,
                            ModContent.ProjectileType<AtaqueJefeProjectile>(),
                            (int)(NPC.damage * 0.60f), 2f, Main.myPlayer,
                            AtaqueJefeProjectile.EstiloPernoEstelar, paleta,
                            NPC.whoAmI * 71 + a * 19 + i);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, NPC.Center);
                    OndaLib.Kick(9f, 18);
                    NPC.netUpdate = true;
                }
            }

            if (_tickEstado >= 140) CerrarEstado();
        }

        // ==================================================================
        //  (f) EL ÚLTIMO ALIENTO — la despedida a vida ≤1%
        // ==================================================================
        private void UltimoAliento(Player target)
        {
            // SE DETIENE: ya no ataca, ya no esquiva — solo mira.
            NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Zero, 0.18f);
            NPC.dontTakeDamage = true;

            // LA BRUMA DE LA DISOLUCIÓN: las dos mitades se deshacen (la
            // separación y los OJOS que te miran los dibuja el PreDraw).
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                int lado = Main.rand.NextBool() ? 1 : -1;
                Vector2 silueta = NPC.Center + new Vector2(lado * 120f, 0f);
                Dust d = Dust.NewDustPerfect(
                    silueta + Main.rand.NextVector2Circular(70f, 70f),
                    DustID.PurpleTorch,
                    new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -Main.rand.NextFloat(0.5f, 1.6f)),
                    150, lado > 0 ? new Color(90, 20, 140) : PaletaOro, 1.2f);
                d.noGravity = true;
            }

            // 3 s (180 t) de despedida… y EL CINE DE MUERTE de siempre
            // (con el botín COMPLETO + la esencia extra).
            if (_tickEstado >= 180)
            {
                IniciarCineMuerte();
            }
        }

        /// <summary>
        /// EL BOTÍN DEL RECUERDO COMPLETO: TODO el de siempre (esencia,
        /// la Forma Ascendida, el Aethon Menor, los 250 fragmentos)…
        /// y UNA ESENCIA DE AETHON EXTRA — la compensación por los dos
        /// fragmentos que se fusionaron sin dropear NADA. v6.50.118 —
        /// LA CRÍA GEMELA GARANTIZADA (la letra: «asegúrate de que el
        /// jugador pueda conseguir las mascotas»): el Aethon Menor ya
        /// NO es un 20% de azar ni un regalo de pruebas — ESTA es su
        /// vía canónica: el recuerdo recompuesto suelta a su cría, y el
        /// mismo ítem engendra a la PAREJA (la de luz y su gemela
        /// oscura — un solo ítem, las dos mascotas).
        /// </summary>
        protected override void DropBotin()
        {
            base.DropBotin();
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                ModContent.ItemType<Items.Esencias.EsenciaDeAethon>(), 1);
            // LA CRÍA GEMELA — SIEMPRE (la vía canónica de la mascota).
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                ModContent.ItemType<Items.Llamados.AethonMenorItem>(), 1);
        }

        // ==================================================================
        //  EL ARTE — EL ECLIPSE (mitad dorada, mitad negro-morado)
        // ==================================================================
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // EL SOL DORADO DE LA BASE (intacto — la mitad de luz del
            // recuerdo; su propio PreDraw abre y cierra sus lotes).
            base.PreDraw(spriteBatch, screenPos, drawColor);

            if (Main.dedServ) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 posC = NPC.Center - Main.screenPosition;
                float visibilidad = 1f - (NPC.alpha / 255f);

                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    // === 1. EL DISCO DE LA OSCURIDAD: la mitad oscura del
                    //     recuerdo TAPA el sol dorado — deslizándose lenta
                    //     alrededor del núcleo: UN ECLIPSE VIVO ===
                    if (!_muriendo)
                    {
                        Vector2 offD = new Vector2(MathF.Cos(t * 0.35f), MathF.Sin(t * 0.35f)) * 46f;
                        LumenLib.Bloom(spriteBatch, posC + offD, 150f,
                            new Color(45, 12, 70), 0.85f * visibilidad, 3);
                        LumenLib.Bloom(spriteBatch, posC + offD, 92f,
                            new Color(90, 20, 140), 0.90f * visibilidad, 2);
                        // EL RIM DEL ECLIPSE (el borde morado que lo recorta).
                        Vector2 ringTam = new Vector2(VFXCore.Ring.Width, VFXCore.Ring.Height);
                        spriteBatch.Draw(VFXCore.Ring, posC + offD, null,
                            new Color(140, 60, 210) * (0.35f * visibilidad), 0f,
                            ringTam * 0.5f, VFXCore.RingQuadSize(96f) / ringTam,
                            SpriteEffects.None, 0f);
                    }

                    // === 2. EL VELO DEL ECLIPSE GEMELO: la media pantalla
                    //     ACTIVA arde, la inactiva se dibuja TENUE (el
                    //     aviso legible — el lado tranquilo es donde se
                    //     vive) ===
                    if (_estado == F_ECLIPSE)
                    {
                        int ronda = Math.Min(2, _tickEstado / 100);
                        bool ardeIzq = (ronda & 1) == 0;
                        float eje = posC.X;
                        float anchoV = Math.Max(560f, Main.screenWidth * 0.55f);
                        float altoV = Math.Max(700f, Main.screenHeight * 1.05f);
                        Vector2 origenV = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height) * 0.5f;

                        // EL LADO DORADO (izquierda).
                        float aOro = (ardeIzq ? 0.26f : 0.09f) +
                            (ardeIzq ? 0.05f : 0f) * MathF.Sin(t * 5f);
                        spriteBatch.Draw(VFXCore.SoftGlow,
                            new Vector2(eje - anchoV * 0.5f, Main.screenHeight * 0.5f), null,
                            new Color(255, 220, 120) * (aOro * visibilidad), 0f, origenV,
                            new Vector2(anchoV / VFXCore.SoftGlow.Width,
                                        altoV / VFXCore.SoftGlow.Height),
                            SpriteEffects.None, 0f);

                        // EL LADO OSCURO (derecha).
                        float aOsc = (!ardeIzq ? 0.26f : 0.09f) +
                            (!ardeIzq ? 0.05f : 0f) * MathF.Sin(t * 5f);
                        spriteBatch.Draw(VFXCore.SoftGlow,
                            new Vector2(eje + anchoV * 0.5f, Main.screenHeight * 0.5f), null,
                            new Color(90, 20, 140) * (aOsc * visibilidad), 0f, origenV,
                            new Vector2(anchoV / VFXCore.SoftGlow.Width,
                                        altoV / VFXCore.SoftGlow.Height),
                            SpriteEffects.None, 0f);

                        // EL EJE (la línea que parte la pantalla en dos —
                        // vertical, fina, blanca-caliente: la frontera).
                        Vector2 origenE = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height) * 0.5f;
                        float altoE = Math.Max(1200f, Main.screenHeight * 1.6f);
                        spriteBatch.Draw(VFXCore.SoftGlow,
                            new Vector2(eje, Main.screenHeight * 0.5f), null,
                            new Color(255, 240, 210) * (0.5f * visibilidad), 0f, origenE,
                            new Vector2(7f / VFXCore.SoftGlow.Width,
                                        altoE / VFXCore.SoftGlow.Height),
                            SpriteEffects.None, 0f);
                    }

                    // === 3. EL ÚLTIMO ALIENTO: las DOS SILUETAS — la de
                    //     luz y la de oscuridad, a ±120 px, AMBAS MIRANDO
                    //     al jugador (el núcleo-ojo apunta) — deshaciéndose
                    //     en bruma a medida que los 3 s se consumen ===
                    if (_estado == F_ALIENTO)
                    {
                        Player presa = Main.player[NPC.target];
                        Vector2 mira = presa != null && presa.active && !presa.dead
                            ? presa.Center : NPC.Center + Vector2.UnitX * 300f;
                        float disol = Math.Min(1f, _tickEstado / 180f);
                        float alfa = (1f - disol) * visibilidad;

                        for (int lado = -1; lado <= 1; lado += 2)
                        {
                            Vector2 silueta = posC + new Vector2(lado * 120f, 0f);
                            Color cSil = lado < 0 ? PaletaOro : new Color(90, 20, 140);

                            // EL CUERPO TENUE del fragmento.
                            LumenLib.Bloom(spriteBatch, silueta, 84f, cSil, 0.40f * alfa, 3);
                            LumenLib.Bloom(spriteBatch, silueta, 42f, cSil, 0.55f * alfa, 2);

                            // EL NÚCLEO-OJO QUE APUNTA: la pupila desplazada
                            // hacia la presa — las dos mitades MIRANDO al
                            // que las está matando.
                            Vector2 hacia = (mira - (silueta + Main.screenPosition))
                                .SafeNormalize(Vector2.UnitY) * 13f;
                            LumenLib.Bloom(spriteBatch, silueta + hacia, 20f,
                                new Color(255, 250, 240), 0.85f * alfa, 3);

                            // LA BRUMA QUE SE DESHACE (las volutas subiendo).
                            for (int v = 0; v < 3; v++)
                            {
                                float fV = (t * 0.8f + v * 0.33f + (lado + 1) * 0.5f) % 1f;
                                Vector2 voluta = silueta + new Vector2(
                                    MathF.Sin(fV * 6f + v * 2f) * 26f, -20f - fV * 90f);
                                LumenLib.Bloom(spriteBatch, voluta, 26f * (0.4f + fV),
                                    cSil, 0.25f * alfa * (1f - fV), 2);
                            }
                        }
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
                // El estado de salida de PreDraw es SIEMPRE el lote de
                // vanilla (el contrato de la casa).
                VFXCore.CerrarLoteSiAbierto();
                VFXCore.ReabrirLoteVanilla();
            }
            return false;   // el eclipse se dibuja a sí mismo
        }
    }
}
