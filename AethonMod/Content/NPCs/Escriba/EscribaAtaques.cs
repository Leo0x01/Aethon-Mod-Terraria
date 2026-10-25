using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.Coreografia;
using AethonMod.Content.Projectiles.Escriba;
using AethonMod.Content.VFX;
using EscribaJefe = AethonMod.Content.NPCs.Escriba.EscribaJefe;

namespace AethonMod.Content.NPCs.Escriba
{
    // ======================================================================
    //  v6.52.0 — LOS GUIONES DEL ESCRIBA: los 8 ataques + 2 transiciones
    //  de la coreografía, cada uno con SU telegraph (la letra de la
    //  investigación R3: «el jefe ESCRIBE sus ataques en el aire antes
    //  de que ocurran» — la línea de tinta ES el aviso).
    //
    //  LA TABLA DE TIEMPOS (la gramática calibrada de R3 §1.2):
    //    rápido   tele 20–30 t (El Trazo 28, Plumas 35)
    //    normal   tele 45–60 t (Lluvia 40, Jaula 90, Verso 50)
    //    pesado   tele 90–120 t
    //    EL NUKE  tele 216 t   (El Borrón — el estándar Ares de Calamity)
    //  y la RECUPERACIÓN es la ventana de oportunidad VISIBLE.
    // ======================================================================
    public static class EscribaAtaques
    {
        // Los estados de ataque (viven en NPC.ai[0] — el contrato MP).
        public const int ST_TRAZO = 10;
        public const int ST_LLUVIA = 11;
        public const int ST_PLUMAS = 12;
        public const int ST_BORRON = 13;
        public const int ST_TRAZO_CORREGIDO = 14;
        public const int ST_JAULA = 15;
        public const int ST_VERSO = 16;
        public const int ST_ANOTACION = 17;
        public const int ST_TRANSICION1 = 30;
        public const int ST_TRANSICION2 = 31;

        private static GuionAtaque _trazo, _lluvia, _plumas, _borron, _corregido,
            _jaula, _verso, _anotacion, _trans1, _trans2;
        private static bool _listo;

        /// <summary>Construye (una vez) todos los guiones.</summary>
        public static void Asegurar()
        {
            if (_listo) return;
            _listo = true;
            _trazo = GuionTrazo(false);
            _lluvia = GuionLluvia();
            _plumas = GuionPlumas();
            _borron = GuionBorron();
            _corregido = GuionTrazo(true);
            _jaula = GuionJaula();
            _verso = GuionVerso();
            _anotacion = GuionAnotacion();
            _trans1 = GuionTransicion1();
            _trans2 = GuionTransicion2();
        }

        /// <summary>El guion de un estado (null si no es de ataque).</summary>
        public static GuionAtaque DeEstado(int estado)
        {
            Asegurar();
            return estado switch
            {
                ST_TRAZO => _trazo,
                ST_LLUVIA => _lluvia,
                ST_PLUMAS => _plumas,
                ST_BORRON => _borron,
                ST_TRAZO_CORREGIDO => _corregido,
                ST_JAULA => _jaula,
                ST_VERSO => _verso,
                ST_ANOTACION => _anotacion,
                ST_TRANSICION1 => _trans1,
                ST_TRANSICION2 => _trans2,
                _ => null,
            };
        }

        // ------------------------------------------------------------------
        //  1 + 5. EL TRAZO (y su versión corregida — EL FAKEOUT)
        // ------------------------------------------------------------------
        private static GuionAtaque GuionTrazo(bool corregido)
        {
            if (corregido)
            {
                // EL TRAZO CORREGIDO: la MISMA línea… que SE AGUANTA dos
                // veces antes de soltar (la taxonomía del fakeout: el hold
                // extendido — nunca miente sobre el Lanzamiento, solo sobre
                // el CUÁNDO), y el golpe es DOBLE (dos carreras).
                return new GuionAtaque
                {
                    ClaveNombre = "Mods.AethonMod.Escriba.AtaqueCorregido",
                    Peso = 1,
                    ColorTelegrafo = new Color(190, 90, 235),
                    ColorActivo = new Color(255, 220, 190),
                    SonidoInicio = SoundID.Item27,
                    SonidoGolpe = SoundID.Item70,
                    SacudidaGolpe = 5f,
                    Compas = Compas.Nuevo(28f + 25f + 25f + 26f + 50f)
                        .Con(Movimiento.Nuevo(CoreoGuion.Telegrafo, 0f, 28f))
                        .Con(Movimiento.Nuevo("carga1", 28f, 25f))
                        .Con(Movimiento.Nuevo("carga2", 53f, 25f))
                        .Con(Movimiento.Nuevo(CoreoGuion.Activo, 78f, 26f))
                        .Con(Movimiento.Nuevo(CoreoGuion.Recuperacion, 104f, 50f)),
                    IA = IaTrazo(true),
                    Visual = VisualTrazo(true),
                };
            }
            return CoreoGuion.Triseccion("Mods.AethonMod.Escriba.AtaqueTrazo",
                28f, 22f, 45f, IaTrazo(false), VisualTrazo(false), peso: 3, sacudida: 4f,
                inicio: SoundID.Item27, golpe: SoundID.Item70,
                colorTelegrafo: new Color(150, 110, 230), colorActivo: new Color(255, 235, 205));
        }

        /// <summary>LA LÓGICA DEL TRAZO: la línea se marca al ENTRAR; al
        /// soltar, el Escriba la RECORRE de un tajo (×2 si es corregido).</summary>
        private static Action<NPC, Player, Instante> IaTrazo(bool doble)
        {
            return (npc, presa, inst) =>
            {
                var jefe = npc.ModNPC as EscribaJefe;
                if (jefe == null) return;
                bool autoridad = Main.netMode != NetmodeID.MultiplayerClient;

                if (inst.En(CoreoGuion.Telegrafo))
                {
                    npc.velocity *= 0.9f;
                    if (inst.PrimerTick && autoridad)
                        jefe.MarcarCarril(presa, doble ? 44f : 0f);
                }
                else if (inst.En("carga1") || inst.En("carga2"))
                {
                    // EL HOLD: clavado en el sitio (el fakeout respira).
                    npc.velocity *= 0.85f;
                }
                else if (inst.En(CoreoGuion.Activo))
                {
                    if (inst.PrimerTick && autoridad)
                    {
                        npc.Center = jefe.CarrilOrigen();
                        npc.netUpdate = true;
                    }
                    // LA CARRERA: 47 px/t a lo largo del carril; el
                    // corregido hace DOS pasadas (la segunda desde el
                    // carril paralelo).
                    float t = inst.Fase01;
                    float vuelta = doble ? MathF.Min(t * 2f, 1f) : t;
                    bool segunda = doble && t > 0.5f;
                    Vector2 dir = jefe.CarrilDireccion();
                    float offset = (segunda ? -1f : 1f) * jefe.CarrilSeparacion;
                    npc.Center = jefe.CarrilOrigen() +
                        new Vector2(-dir.Y, dir.X) * (doble ? offset : 0f) +
                        dir * (jefe.CarrilLongitud() * vuelta);
                    npc.velocity = dir * 47f;
                    if (inst.UltimoTick) npc.velocity *= 0.2f;
                }
                else if (inst.En(CoreoGuion.Recuperacion))
                {
                    // LA EXHALACIÓN: frena, se apaga (la ventana SE VE).
                    npc.velocity *= 0.92f;
                }
            };
        }

        /// <summary>EL VISUAL DEL TRAZO: la línea SE DIBUJA antes de existir
        /// (BeamLib.Telegrafo) y la carrera deja ESTELA de tinta.</summary>
        private static Action<NPC, Player, Instante> VisualTrazo(bool doble)
        {
            return (npc, presa, inst) =>
            {
                var jefe = npc.ModNPC as EscribaJefe;
                if (jefe == null) return;
                float tiempo = Main.GlobalTimeWrappedHourly;

                if (inst.En(CoreoGuion.Telegrafo) || inst.En("carga1") || inst.En("carga2"))
                {
                    float fase = inst.En(CoreoGuion.Telegrafo)
                        ? inst.Fase01 * 0.5f
                        : 0.5f + 0.5f * (inst.En("carga2") ? 1f : 0.5f);
                    // LA LÍNEA DOBLE del corregido (dos avisos paralelos).
                    Vector2 dir = jefe.CarrilDireccion();
                    Vector2 perp = new(-dir.Y, dir.X);
                    float sep = jefe.CarrilSeparacion;
                    BeamLib.Telegrafo(jefe.CarrilOrigen() + (doble ? perp * sep : Vector2.Zero),
                        dir, jefe.CarrilLongitud(), jefe.TelegrafoTinte(doble), fase, tiempo, 26f);
                    if (doble)
                        BeamLib.Telegrafo(jefe.CarrilOrigen() - perp * sep,
                            dir, jefe.CarrilLongitud(), jefe.TelegrafoTinte(doble), fase, tiempo, 26f);
                }
                else if (inst.En(CoreoGuion.Activo) && VFXCore.Presupuesto(8))
                {
                    // LA ESTELA DEL TAJO: el rastro de tinta tras la carrera.
                    Color tinta = jefe.TelegrafoTinte(false);
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 pos = npc.Center - npc.velocity * (i * 0.55f);
                        VFXCore.Quad(pos, StormLib.Tint(tinta, 0.35f - i * 0.07f),
                            new Vector2(34f - i * 6f, 34f - i * 6f), VFXCore.SoftGlow);
                    }
                }
            };
        }

        // ------------------------------------------------------------------
        //  2. LA LLUVIA DE VERSOS (páginas que caen con charco de sombra)
        // ------------------------------------------------------------------
        private static GuionAtaque GuionLluvia()
        {
            return CoreoGuion.Triseccion("Mods.AethonMod.Escriba.AtaqueLluvia",
                40f, 70f, 35f,
                (npc, presa, inst) =>
                {
                    bool autoridad = Main.netMode != NetmodeID.MultiplayerClient;
                    // LAS PÁGINAS: una cada 11 t durante telegraph+activo
                    // (8 en total — la lluvia con nombre).
                    if (autoridad && ((int)npc.ai[1]) % 11 == 0 &&
                        (inst.En(CoreoGuion.Telegrafo) || inst.En(CoreoGuion.Activo)))
                    {
                        float x = presa.Center.X + Main.rand.NextFloat(-440f, 440f);
                        float y = presa.Center.Y - 560f;
                        var src = npc.GetSource_FromThis();
                        int p = Projectile.NewProjectile(src, new Vector2(x, y), Vector2.Zero,
                            ModContent.ProjectileType<EscribaPagina>(), 70, 3f, Main.myPlayer);
                        if (p >= 0)
                        {
                            Main.projectile[p].ai[1] = presa.Center.Y + Main.rand.NextFloat(-40f, 90f);
                            Main.projectile[p].netUpdate = true;
                        }
                    }
                    if (inst.En(CoreoGuion.Recuperacion)) npc.velocity *= 0.93f;
                },
                (npc, presa, inst) => { },
                peso: 2, sacudida: 2f,
                inicio: SoundID.Item27, golpe: SoundID.Item30,
                colorTelegrafo: new Color(150, 110, 230), colorActivo: new Color(230, 225, 255));
        }

        // ------------------------------------------------------------------
        //  3. LAS PLUMAS (el anillo orbital que cierra hacia dentro)
        // ------------------------------------------------------------------
        private static GuionAtaque GuionPlumas()
        {
            return CoreoGuion.Triseccion("Mods.AethonMod.Escriba.AtaquePlumas",
                35f, 75f, 35f,
                (npc, presa, inst) =>
                {
                    bool autoridad = Main.netMode != NetmodeID.MultiplayerClient;
                    if (inst.PrimerTick && inst.En(CoreoGuion.Telegrafo) && autoridad)
                    {
                        // EL ANILLO: 12 plumas alrededor de la presa (el
                        // hueco de escape queda DONDE NO HAY PLUMA).
                        var src = npc.GetSource_FromThis();
                        for (int i = 0; i < 12; i++)
                        {
                            int p = Projectile.NewProjectile(src, presa.Center, Vector2.Zero,
                                ModContent.ProjectileType<EscribaPluma>(), 65, 2f, Main.myPlayer,
                                0f, i * MathHelper.TwoPi / 12f, presa.whoAmI);
                            if (p >= 0) Main.projectile[p].netUpdate = true;
                        }
                    }
                    if (inst.En(CoreoGuion.Recuperacion)) npc.velocity *= 0.93f;
                },
                (npc, presa, inst) => { },
                peso: 2, sacudida: 3f,
                inicio: SoundID.Item27, golpe: SoundID.Item17,
                colorTelegrafo: new Color(150, 110, 230), colorActivo: new Color(220, 205, 255));
        }

        // ------------------------------------------------------------------
        //  4. EL BORRÓN — EL NUKE DE 216 t (el estándar de Calamity)
        // ------------------------------------------------------------------
        private static GuionAtaque GuionBorron()
        {
            return CoreoGuion.Triseccion("Mods.AethonMod.Escriba.AtaqueBorron",
                216f, 34f, 60f,
                (npc, presa, inst) =>
                {
                    bool autoridad = Main.netMode != NetmodeID.MultiplayerClient;
                    var jefe = npc.ModNPC as EscribaJefe;

                    if (inst.En(CoreoGuion.Telegrafo))
                    {
                        npc.velocity *= 0.9f;
                        // EL RETUMBE: el sonido de carga que CRECE (uno cada
                        // 30 t con pitch ascendente — el rumble que persigue).
                        if (inst.TickEnMovimiento % 30 == 0 && Main.netMode != NetmodeID.Server)
                            Terraria.Audio.SoundEngine.PlaySound(
                                SoundID.Item67.WithPitchOffset(-0.4f + 0.5f * inst.Fase01),
                                npc.Center);
                    }
                    else if (inst.En(CoreoGuion.Activo))
                    {
                        if (inst.PrimerTick && autoridad)
                        {
                            // LA DETONACIÓN: 26 gotas radiales + el daño de
                            // área para quien se quedó pegado al jefe.
                            var src = npc.GetSource_FromThis();
                            for (int i = 0; i < 26; i++)
                            {
                                Vector2 v = new Vector2(1, 0).RotatedBy(
                                    i * MathHelper.TwoPi / 26f + 0.13f) *
                                    Main.rand.NextFloat(6.5f, 11f);
                                Projectile.NewProjectile(src, npc.Center, v,
                                    ModContent.ProjectileType<EscribaGota>(), 95, 3f, Main.myPlayer);
                            }
                            foreach (Player j in Main.player)
                            {
                                if (j == null || !j.active || j.dead) continue;
                                if (Vector2.Distance(j.Center, npc.Center) < 300f)
                                    j.Hurt(PlayerDeathReason.ByNPC(npc.whoAmI), 130,
                                        Math.Sign(j.Center.X - npc.Center.X));
                            }
                        }
                        npc.velocity *= 0.86f;
                    }
                    else if (inst.En(CoreoGuion.Recuperacion))
                    {
                        npc.velocity *= 0.92f;
                        // EL VACÍO tras el borrón (la luz vuelve LENTA).
                        if (jefe != null) LuzEscenica.Foco(1, npc.Center, new Color(170, 140, 90), 0.5f);
                    }
                },
                (npc, presa, inst) =>
                {
                    // EL VISUAL DEL NUKE: el VÓRTICE que converge (las
                    // motas se CIERRAN sobre la pluma) y la FLORACIÓN.
                    if (inst.En(CoreoGuion.Telegrafo) && VFXCore.Presupuesto(14))
                    {
                        float t = Main.GlobalTimeWrappedHourly;
                        for (int i = 0; i < 12; i++)
                        {
                            float h = VFXCore.Hash01(313, i, 7);
                            float radio = MathHelper.Lerp(420f, 46f, inst.Fase01);
                            float ang = t * (1.2f + h) + i * MathHelper.TwoPi / 12f;
                            Vector2 pos = npc.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio;
                            VFXCore.Quad(pos, StormLib.Tint(new Color(120, 70, 200), 0.5f),
                                new Vector2(13f, 13f), VFXCore.GlowOrb);
                        }
                    }
                    if (inst.En(CoreoGuion.Activo) && VFXCore.Presupuesto(10))
                    {
                        // LA FLORACIÓN NEGRA: anillo + bola que EXPANDEN.
                        float t = inst.Fase01;
                        VFXCore.Quad(npc.Center, StormLib.Tint(new Color(90, 50, 160), 0.6f * (1f - t)),
                            new Vector2(120f + 380f * t, 120f + 380f * t), VFXCore.SoftGlow);
                        VFXCore.Quad(npc.Center, StormLib.Tint(new Color(200, 160, 255), 0.5f * (1f - t)),
                            new Vector2(60f + 320f * t, 60f + 320f * t), VFXCore.Ring);
                    }
                },
                peso: 2, sacudida: 8f,
                inicio: SoundID.Item27, golpe: SoundID.Item74,
                colorTelegrafo: new Color(170, 70, 230), colorActivo: new Color(240, 210, 255));
        }

        // ------------------------------------------------------------------
        //  6. LA JAULA DE TINTA (las paredes que cierran la arena)
        // ------------------------------------------------------------------
        private static GuionAtaque GuionJaula()
        {
            return CoreoGuion.Triseccion("Mods.AethonMod.Escriba.AtaqueJaula",
                90f, 240f, 40f,
                (npc, presa, inst) =>
                {
                    bool autoridad = Main.netMode != NetmodeID.MultiplayerClient;
                    if (inst.PrimerTick && inst.En(CoreoGuion.Telegrafo) && autoridad)
                    {
                        // LAS DOS PAREDES: a ±640 px, cerrando hasta ±150.
                        var src = npc.GetSource_FromThis();
                        int izq = Projectile.NewProjectile(src,
                            new Vector2(presa.Center.X - 640f, presa.Center.Y - 170f), Vector2.Zero,
                            ModContent.ProjectileType<EscribaPared>(), 80, 2f, Main.myPlayer);
                        int der = Projectile.NewProjectile(src,
                            new Vector2(presa.Center.X + 640f, presa.Center.Y - 170f), Vector2.Zero,
                            ModContent.ProjectileType<EscribaPared>(), 80, 2f, Main.myPlayer);
                        if (izq >= 0)
                        {
                            Main.projectile[izq].ai[1] = 62f;
                            // v6.52.2 — LA CURA DE LA JAULA: Projectile.ai
                            // es float[3] (índices 0..2 — Cecil: maxAI = 3);
                            // el destino X vivía en ai[3] → IndexOutOfRange
                            // EN CADA JAULA (client.log .52.1). AHORA el
                            // destino viaja en localAI[0] (la pared lo lee).
                            Main.projectile[izq].localAI[0] = presa.Center.X - 150f;
                            Main.projectile[izq].netUpdate = true;
                        }
                        if (der >= 0)
                        {
                            Main.projectile[der].ai[1] = -62f;
                            Main.projectile[der].localAI[0] = presa.Center.X + 150f;
                            Main.projectile[der].netUpdate = true;
                        }
                    }
                    // MIENTRAS LA JAULA VIVE: llovizna de páginas (una cada 26 t).
                    if (inst.En(CoreoGuion.Activo) && autoridad && ((int)npc.ai[1]) % 26 == 0)
                    {
                        float x = presa.Center.X + Main.rand.NextFloat(-160f, 160f);
                        var src = npc.GetSource_FromThis();
                        int p = Projectile.NewProjectile(src, new Vector2(x, presa.Center.Y - 560f),
                            Vector2.Zero, ModContent.ProjectileType<EscribaPagina>(), 60, 3f, Main.myPlayer);
                        if (p >= 0)
                        {
                            Main.projectile[p].ai[1] = presa.Center.Y + 40f;
                            Main.projectile[p].netUpdate = true;
                        }
                    }
                    if (inst.En(CoreoGuion.Recuperacion)) npc.velocity *= 0.93f;
                },
                (npc, presa, inst) => { },
                peso: 1, sacudida: 4f,
                inicio: SoundID.Item27, golpe: SoundID.Item30,
                colorTelegrafo: new Color(150, 110, 230), colorActivo: new Color(200, 180, 255));
        }

        // ------------------------------------------------------------------
        //  7. EL VERSO — EL RAYO MASIVO DEL JEFE (BeamLib hostil)
        // ------------------------------------------------------------------
        private static GuionAtaque GuionVerso()
        {
            return CoreoGuion.Triseccion("Mods.AethonMod.Escriba.AtaqueVerso",
                50f, 140f, 40f,
                (npc, presa, inst) =>
                {
                    bool autoridad = Main.netMode != NetmodeID.MultiplayerClient;
                    // EL ESCRIBA SE PLANTA (el verso se escribe QUIETO).
                    if (inst.En(CoreoGuion.Telegrafo) || inst.En(CoreoGuion.Activo))
                        npc.velocity *= 0.88f;

                    if (inst.PrimerTick && inst.En(CoreoGuion.Telegrafo) && autoridad)
                    {
                        // EL RAYO NACE: ángulo hacia la presa, barrido ±
                        // alternando por semilla del whoAmI.
                        Vector2 dir = Vector2.Normalize(presa.Center - npc.Center);
                        if (float.IsNaN(dir.X) || dir == Vector2.Zero) dir = -Vector2.UnitY;
                        float ang = dir.ToRotation();
                        float barrido = 0.0072f * ((npc.whoAmI & 1) == 0 ? 1f : -1f);
                        var src = npc.GetSource_FromThis();
                        int p = Projectile.NewProjectile(src, npc.Center, Vector2.Zero,
                            ModContent.ProjectileType<EscribaVerso>(), 110, 4f, Main.myPlayer,
                            0f, ang, barrido);
                        if (p >= 0) Main.projectile[p].netUpdate = true;
                    }
                    if (inst.En(CoreoGuion.Recuperacion)) npc.velocity *= 0.93f;
                },
                (npc, presa, inst) => { },
                peso: 2, sacudida: 5f,
                inicio: SoundID.Item27, golpe: SoundID.Item75,
                colorTelegrafo: new Color(140, 90, 235), colorActivo: new Color(215, 190, 255));
        }

        // ------------------------------------------------------------------
        //  8. LA ANOTACIÓN — EL AGARRÓN (el momento «wow» de la R3)
        // ------------------------------------------------------------------
        private static GuionAtaque GuionAnotacion()
        {
            return new GuionAtaque
            {
                ClaveNombre = "Mods.AethonMod.Escriba.AtaqueAnotacion",
                Peso = 0,                                    // NUNCA al azar: el jefe lo ELIGE
                ColorTelegrafo = new Color(220, 120, 240),
                ColorActivo = new Color(255, 245, 235),
                SonidoInicio = SoundID.Item27,
                SonidoGolpe = SoundID.Item122,
                SacudidaGolpe = 6f,
                Compas = Compas.Nuevo(40f + 70f + 50f)
                    .Con(Movimiento.Nuevo(CoreoGuion.Telegrafo, 0f, 40f))
                    .Con(Movimiento.Nuevo(CoreoGuion.Activo, 40f, 70f))
                    .Con(Movimiento.Nuevo(CoreoGuion.Recuperacion, 110f, 50f)),
                IA = (npc, presa, inst) =>
                {
                    bool autoridad = Main.netMode != NetmodeID.MultiplayerClient;
                    var jefe = npc.ModNPC as EscribaJefe;

                    if (inst.En(CoreoGuion.Telegrafo))
                    {
                        // LA CERCA: el Escriba SE ACERCA lento (la tentación).
                        Vector2 hacia = Vector2.Normalize(presa.Center - npc.Center);
                        if (!float.IsNaN(hacia.X)) npc.velocity = hacia * 4.5f;
                        if (inst.UltimoTick && autoridad && jefe != null)
                            jefe.IntentarAgarre(presa);
                    }
                    else if (inst.En(CoreoGuion.Activo))
                    {
                        if (jefe == null) return;
                        if (jefe.AgarreActivo)
                        {
                            // LA PÁGINA SE CIERRA: la presa sujeta (velocidad
                            // a cero + daño por tick cada 12 t — el SP la
                            // vive entera) mientras la cámara MIRA.
                            npc.velocity *= 0.8f;
                            if (autoridad)
                            {
                                presa.velocity *= 0.05f;
                                if (inst.TickEnMovimiento % 12 == 0)
                                    presa.Hurt(PlayerDeathReason.ByNPC(npc.whoAmI), 32,
                                        Math.Sign(presa.Center.X - npc.Center.X));
                                presa.Center = Vector2.Lerp(presa.Center,
                                    npc.Center + new Vector2(0, -30f), 0.12f);
                            }
                            if (inst.UltimoTick && autoridad)
                            {
                                // EL DESENGANCHE: la página la ESCUPIE.
                                Vector2 fuera = new Vector2(presa.Center.X - npc.Center.X,
                                    -0.45f).SafeNormalize(Vector2.UnitX) * 17f;
                                presa.velocity = fuera;
                                for (int i = 0; i < 6; i++)
                                {
                                    Vector2 v = fuera.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.4f, 0.8f);
                                    Projectile.NewProjectile(npc.GetSource_FromThis(),
                                        presa.Center, v, ModContent.ProjectileType<EscribaGota>(),
                                        60, 3f, Main.myPlayer);
                                }
                            }
                        }
                        else
                        {
                            // EL FALLO: el Escriba pasa LARGO (y el cooldown
                            // se reduce a la mitad — la regla CWR del whiff).
                            npc.velocity *= 0.96f;
                        }
                    }
                    else if (inst.En(CoreoGuion.Recuperacion))
                    {
                        npc.velocity *= 0.9f;
                        if (inst.UltimoTick && jefe != null) jefe.FinAgarre();
                    }
                },
                Visual = (npc, presa, inst) =>
                {
                    var jefe = npc.ModNPC as EscribaJefe;
                    if (jefe == null) return;
                    float tiempo = Main.GlobalTimeWrappedHourly;

                    if (inst.En(CoreoGuion.Telegrafo) && VFXCore.Presupuesto(10))
                    {
                        // LAS PÁGINAS QUE RONDAN a la presa (el aviso del
                        // agarrón: se CIERRAN sobre ella).
                        for (int i = 0; i < 5; i++)
                        {
                            float ang = tiempo * 2.4f + i * MathHelper.TwoPi / 5f;
                            float radio = 150f - 60f * inst.Fase01;
                            Vector2 pos = presa.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio;
                            VFXCore.Quad(pos, StormLib.Tint(new Color(240, 236, 250), 0.5f),
                                new Vector2(14f, 19f), ang, VFXCore.SoftGlow);
                        }
                    }
                    if (jefe.AgarreActivo && inst.En(CoreoGuion.Activo) && VFXCore.Presupuesto(8))
                    {
                        // LA PÁGINA QUE ENVUELVE a la presa.
                        VFXCore.Quad(presa.Center, StormLib.Tint(new Color(245, 242, 255), 0.75f),
                            new Vector2(70f, 96f), MathF.Sin(tiempo * 2f) * 0.12f, VFXCore.SoftGlow);
                        VFXCore.Quad(presa.Center, StormLib.Tint(new Color(150, 100, 220), 0.4f),
                            new Vector2(90f, 116f), 0f, VFXCore.Ring);
                    }
                },
            };
        }

        // ------------------------------------------------------------------
        //  LA TRANSICIÓN 1 — «LA CORRECCIÓN» (el modelo de 6 beats de CWR)
        // ------------------------------------------------------------------
        private static GuionAtaque GuionTransicion1()
        {
            return new GuionAtaque
            {
                ClaveNombre = "Mods.AethonMod.Escriba.Transicion1",
                Anunciar = false,                        // la transición ES el anuncio
                ColorTelegrafo = new Color(120, 80, 210),
                ColorActivo = new Color(255, 240, 215),
                SonidoInicio = SoundID.Item27,
                SonidoGolpe = SoundID.Item74,
                SacudidaGolpe = 8f,
                Compas = Compas.Nuevo(50f + 45f + 35f + 50f + 30f + 30f)
                    .Con(Movimiento.Nuevo("reunion", 0f, 50f))
                    .Con(Movimiento.Nuevo("tinta", 50f, 45f))
                    .Con(Movimiento.Nuevo("silencio", 95f, 35f))
                    .Con(Movimiento.Nuevo(CoreoGuion.Activo, 130f, 50f))     // «firma»
                    .Con(Movimiento.Nuevo("estela", 180f, 30f))
                    .Con(Movimiento.Nuevo("respiro", 210f, 30f)),
                IA = (npc, presa, inst) =>
                {
                    // LA TRANSICIÓN ES UN CINE: intocable, quieto, sin
                    // ataques — SOLO el beat visual (la lección Fargo).
                    npc.velocity *= 0.85f;
                    npc.dontTakeDamage = true;
                    if (inst.En("reunion") && inst.PrimerTick)
                        CamaraCine.Enfocar(npc.Center, 2.6f);
                    if (inst.En("silencio") && inst.PrimerTick)
                    {
                        LuzEscenica.Apagar();
                        if (Main.netMode != NetmodeID.Server)
                        {
                            Pantalla.Vineta(0.8f, 1.2f);
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14.WithPitchOffset(-0.8f), npc.Center);
                        }
                    }
                    if (inst.En("estela") && inst.PrimerTick &&
                        Main.netMode != NetmodeID.Server)
                        Pantalla.Flash(new Color(255, 246, 225), 0.35f, 0.5f);
                    if (inst.En("respiro") && inst.UltimoTick)
                        npc.dontTakeDamage = false;
                },
                Visual = (npc, presa, inst) =>
                {
                    // LA TINTA SUBE: partículas que FLUYEN hacia arriba
                    // hacia la pluma alzada (los «ChargeVisuals» de LV).
                    if ((inst.En("reunion") || inst.En("tinta")) && VFXCore.Presupuesto(12))
                    {
                        float t = Main.GlobalTimeWrappedHourly;
                        for (int i = 0; i < 10; i++)
                        {
                            float h = VFXCore.Hash01(733, i, 3);
                            float subida = ((t * (0.6f + h * 0.5f) + h) % 1f);
                            Vector2 pos = npc.Center + new Vector2(
                                MathF.Sin(subida * 9f + h * 40f) * 60f * (1f - subida),
                                140f - subida * 200f);
                            VFXCore.Quad(pos, StormLib.Tint(new Color(140, 95, 220), 0.55f),
                                new Vector2(11f, 11f), VFXCore.GlowOrb);
                        }
                    }
                    // EL BAÑO DE ARENA en «firma»: tres canales, violeta→oro.
                    if (inst.En(CoreoGuion.Activo))
                        LuzEscenica.Foco(2, npc.Center, new Color(255, 220, 150), 1.6f);
                },
            };
        }

        // ------------------------------------------------------------------
        //  LA TRANSICIÓN 2 — «EL INTERLINEADO» (la curación dramática)
        // ------------------------------------------------------------------
        private static GuionAtaque GuionTransicion2()
        {
            return new GuionAtaque
            {
                ClaveNombre = "Mods.AethonMod.Escriba.Transicion2",
                Anunciar = false,
                ColorTelegrafo = new Color(200, 90, 200),
                ColorActivo = new Color(255, 235, 245),
                SonidoInicio = SoundID.Item27,
                SonidoGolpe = SoundID.Item29,
                SacudidaGolpe = 6f,
                Compas = Compas.Nuevo(45f + 70f + 45f + 40f)
                    .Con(Movimiento.Nuevo("ruptura", 0f, 45f))
                    .Con(Movimiento.Nuevo("esferas", 45f, 70f))
                    .Con(Movimiento.Nuevo(CoreoGuion.Activo, 115f, 45f))     // la re-tinta
                    .Con(Movimiento.Nuevo("respiro", 160f, 40f)),
                IA = (npc, presa, inst) =>
                {
                    npc.velocity *= 0.85f;
                    npc.dontTakeDamage = true;
                    if (inst.En("ruptura") && inst.PrimerTick)
                        CamaraCine.Enfocar(npc.Center, 2.2f);

                    // LAS ESFERAS DE TINTA: al ÚLTIMO tick del tramo, la
                    // re-tinta (+10% de vida — el «heal-back» de Fargo).
                    if (inst.En("esferas"))
                    {
                        if (inst.UltimoTick && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int curita = (int)(npc.lifeMax * 0.10f);
                            npc.life = Math.Min(npc.lifeMax, npc.life + curita);
                            npc.netUpdate = true;
                        }
                    }
                    if (inst.En(CoreoGuion.Activo) && inst.PrimerTick &&
                        Main.netMode != NetmodeID.Server)
                    {
                        // LA LUZ SE INVIERTe: la arena al revés para la
                        // última página (el «negative-wash» del blueprint).
                        Pantalla.Flash(new Color(255, 240, 250), 0.4f, 0.6f);
                        LuzEscenica.Golpe(2, new Color(255, 180, 240), 2.6f);
                    }
                    if (inst.En("respiro") && inst.UltimoTick)
                        npc.dontTakeDamage = false;
                },
                Visual = (npc, presa, inst) =>
                {
                    // LAS 40 ESFERAS que brotan y VUELVEN (la re-tinta se VE).
                    if (inst.En("esferas") && VFXCore.Presupuesto(20))
                    {
                        float t = Main.GlobalTimeWrappedHourly;
                        for (int i = 0; i < 14; i++)
                        {
                            float h = VFXCore.Hash01(919, i, 5);
                            float viaje = (t * 0.8f + h) % 1f;
                            float radio = 240f * MathF.Sin(viaje * MathHelper.Pi);
                            float ang = h * MathHelper.TwoPi + t * 0.5f;
                            Vector2 pos = npc.Center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radio;
                            VFXCore.Quad(pos, StormLib.Tint(new Color(210, 130, 240), 0.6f),
                                new Vector2(12f, 12f), VFXCore.GlowOrb);
                        }
                    }
                },
            };
        }
    }
}
