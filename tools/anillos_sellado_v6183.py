#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""v6.183 — LA CIRUGÍA DE LOS ANILLOS SOLARES DEL SELLO (la letra:
«el jefe Aethon Sellado se ve bien, que por cada fase obtenga un anillo
runico solar y que ataque con este anillo, crea animaciones de ataques
para 1, 2 y 3 anillos solares runicos del jefe, la forma de ataque sera
la siguiente, el hara que el anillo comience a girar y lanzara rayos
de energia, cuando son 2 anillos lanzaran energia concentrada, al ser 3
anillos lanzaran energia concentrada y al mismo tiempo persegura al
jugador unos segundos»).

EL DISEÑO (las tres ANIMACIONES DE ATAQUE):
  · FASE 1 — LA RUEDA DE RAYOS: el anillo SE ARRANCA girando (el giro
    se acelera a la vista) y dispara DIEZ RAYOS DE ENERGÍA radiales
    (RayoSolarSelladoProjectile — filamentos de vértices, R1: cero
    sprites de rayo).
  · FASE 2 — LA ENERGÍA CONCENTRADA: cada anillo dispara SU haz
    concentrado (HazSolarSelladoProjectile — la técnica de las TRES
    FUNDAS del Ira, en ORO solar, anclada al punto vivo del anillo y
    siguiendo a la presa con lentitud de sentencia).
  · FASE 3 — LA CONCENTRADA + LA CACERÍA: los TRES anillos con sus
    haces… y el DIOS PERSIGUE a la presa ~4 s (la cacería pisa los
    actos mientras dura).
Cirugía con asserts de la casa: si el patrón no está, MUERE.
"""
import io

P = '/home/z/my-project/mod/AethonMod/Content/NPCs/AethonSellado.cs'
s = io.open(P, encoding='utf-8').read()


def parche(viejo, nuevo, nota):
    global s
    assert viejo in s, f'no encuentro «{nota}»'
    assert s.count(viejo) == 1, f'«{nota}» ×{s.count(viejo)}'
    s = s.replace(viejo, nuevo, 1)
    print(f'  ✔ {nota}')


# ── 1. LOS CAMPOS VIVOS DEL ANILLO ────────────────────────────────
parche(
    '''        private int _ojoParpadeo;             // el compás del propio ojo
''',
    '''        private int _ojoParpadeo;             // el compás del propio ojo

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
''',
    'los campos del anillo')

# ── 2. LA LLAMADA EN LA PELEA (antes de los actos) ─────────────────
parche(
    '''            // ── LOS TRES ACTOS (la coreografía) ──
            if (++_actoTick > (Fase >= 2 ? 300 : 380)) { _actoTick = 0; _acto = (byte)((_acto + 1) % 3); }''',
    '''            // ── v6.183 — EL ATAQUE DE LOS ANILLOS SOLARES (la letra):
            //     el compás cuenta, el anillo SE ARRANCA girando y sirve
            //     SU animación según la fase — pisa los actos cuando vive
            //     (la cacería de los TRES anillos persigue de verdad) ──
            MoverElAnillo(presa);

            // ── LOS TRES ACTOS (la coreografía) ──
            if (++_actoTick > (Fase >= 2 ? 300 : 380)) { _actoTick = 0; _acto = (byte)((_acto + 1) % 3); }''',
    'la llamada en la pelea')

# ── 3. EL MOTOR DEL ATAQUE (después de LanzarLaIra) ────────────────
parche(
    '''        // ── LA MUERTE (la última página: los ojos se cierran y el
        //    libro se disuelve en bruma) ──''',
    '''        // ── v6.183 — EL ATAQUE DE LOS ANILLOS SOLARES: el compás, EL
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
        //    libro se disuelve en bruma) ──''',
    'el motor del anillo')

# ── 4. EL DIBUJO: Fase ANILLOS con el GIRO VIVO ────────────────────
parche(
    '''                VFXCore.Begin();
                try
                {
                    SigiloLib.AnilloRunico(NPC.Center, 58f, 58f * 0.36f, -0.55f,
                        t * 0.24f, 8, 1.55f, t, 0,
                        SigiloLib.CuerpoOro, SigiloLib.PuntaOro, 0.92f * NPC.Opacity);
                    VFXCore.FlushAdditive();
                }
                catch (System.Exception exAnillo)
                { VFXCore.CerrarLoteSiAbierto(); global::AethonMod.Content.Systems.RegistroSistema.Fallo("AethonSellado.Anillo", exAnillo); }''',
    '''                VFXCore.Begin();
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
                { VFXCore.CerrarLoteSiAbierto(); global::AethonMod.Content.Systems.RegistroSistema.Fallo("AethonSellado.Anillo", exAnillo); }''',
    'el dibujo de los anillos por fase')

# ── 5. LOS DOS PROYECTILES NUEVOS (antes de la barra) ─────────────
parche(
    '''    // ======================================================================
    //  LA BARRA DEL DIOS SELLADO (la de siempre — el dios cambió de
    //  tamaño, no de barra)
    // ======================================================================''',
    '''    // ======================================================================
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
    // ======================================================================''',
    'los dos proyectiles del anillo')

io.open(P, 'w', encoding='utf-8').write(s)
print('CIRUGÍA DE LOS ANILLOS SOLARES: TODO VIVO')
