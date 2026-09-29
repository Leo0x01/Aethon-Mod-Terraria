using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.NPCs;
using AethonMod.Content.Players;
using AethonMod.Content.Projectiles.Jefes;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Systems
{
    // ======================================================================
    //  LA LLEGADA DE LA LUZ (v6.50.39) — EL RELOJ Y LA OSCURIDAD DE AETHON.
    //
    //  Este sistema reemplaza a OscuridadSistema (retirado de raíz por
    //  petición: «solo hace que todo este negro y no es el oscurecer que
    //  quiero») y hereda sus DOS trabajos:
    //
    //  1. EL ESPEJO + EL RELOJ: reconstruye el estado de LA LLEGADA en
    //     TODAS las máquinas leyendo ai[] del jefe (el servidor manda,
    //     cada cliente padece) y maneja el tiempo — CON LA LECCIÓN DE
    //     ESTA VERSIÓN: «el sol no debe solo saltar a su posición, si el
    //     sol no está en la posición solo debe correr el tiempo hasta
    //     llegar a su posición de forma natural, no dar un salto como si
    //     se teletransportara al centro». EL SOL NUNCA MÁS SE TELE-
    //     TRANSPORTA: si era de noche, la NOCHE ENTERA corre (la luna
    //     barre el cielo, el alba llega SOLA); y al acercarse al mediodía
    //     el reloj DESESPERA — la carrera desacelera en aproximación y
    //     ATERRIZA en 27000 exacto, sin snap, sin salto: el sol se POSA
    //     en el centro como un avión, no como un teletransporte.
    //
    //  2. LA OSCURIDAD: ahora es VeloLib (la técnica de Wrath of the
    //     Gods — TotalScreenOverlaySystem): EL VELO sobre el frame
    //     terminado + LAS LUCES dibujadas después (las excepciones de
    //     la petición original: Aethon brilla dorado porque ES luz pura;
    //     el jugador solo un pequeño círculo — SOLO si su Grimorio es
    //     nivel 50 o superior; las balas de la luz, para ver venir el
    //     castigo) + EL SOL NEGRO y los telegraphs, encima de todo.
    // ======================================================================
    public class AethonLlegadaSistema : ModSystem
    {
        // === EL ESTADO CLIENTE (los fades respiran en cada máquina) ===
        private static float _solNegro = 0f;
        private static bool _avisoGrimorioHecho = false;

        /// <summary>El nivel del Grimorio del jugador local (0 si no lo lleva).</summary>
        private static int NivelGrimorio()
        {
            try
            {
                Player p = Main.LocalPlayer;
                if (p == null || !p.active) return 0;
                var sp = p.GetModPlayer<ShardPlayer>();
                return sp != null ? sp.NivelGrimorioPublico : 0;
            }
            catch { return 0; }
        }

        public override void OnModLoad()
        {
            // EL PINTOR: lo que vive SOBRE la oscuridad (el sol negro, los
            // destellos lejanos, los telegraphs) — registrado UNA vez.
            if (Main.netMode != NetmodeID.Server)
                Velo.SobreElVelo(PintarSobreElVelo);
        }

        public override void OnWorldUnload()
        {
            Entierro();
        }

        public override void Unload()
        {
            try { Velo.QuitarPintor(PintarSobreElVelo); } catch { }
            Entierro();
        }

        private static void Entierro()
        {
            _solNegro = 0f;
            _avisoGrimorioHecho = false;
            AethonBoss.SubLlegada = 0;
            AethonBoss.TiempoCorriendo = false;
            AethonBoss.TiempoCongelado = false;
            AethonBoss.OscuridadObjetivo = false;
        }

        // ==================================================================
        //  EL ESPEJO — PRE UPDATE TIME (corre en TODAS las máquinas,
        //  DESPUÉS de la IA del jefe y ANTES de UpdateTime: el reloj
        //  obedece en el MISMO tick en que la IA decide)
        // ==================================================================
        public override void PreUpdateTime()
        {
            if (Main.gameMenu)
            {
                Entierro();
                Velo.Apagar(1f / 30f);
                return;
            }

            NPC jefe = BuscarJefe();

            if (jefe == null)
            {
                // la luz murió o se fue: el mundo vuelve a ser del mundo.
                // La oscuridad se disuelve DURANTE la contracción (el
                // estallido final del jefe CEGA desde el mundo) y el sol
                // negro se despide LENTO (1/130: más lento que la noche).
                EntierroLocal();
                return;
            }

            bool enLlegada = jefe.ai[0] == 0f;                 // EST_NACIENDO
            int sub = enLlegada ? (int)jefe.ai[1] : 0;
            AethonBoss.SubLlegada = sub;
            bool enPelea = jefe.ai[0] >= 1f && jefe.ai[0] <= 7f;
            bool muriendo = jefe.ai[0] == 99f;
            bool laOscuridad = sub >= 13 || enPelea;           // LA OSCURIDAD
            AethonBoss.OscuridadObjetivo = laOscuridad;

            // === EL RELOJ — LA LECCIÓN DE ESTA VERSIÓN ====================
            // «no dar un salto como si se teletransportara al centro»:
            // cada máquina corre SU propio reloj hacia el mediodía y el
            // aterrizaje es CONVERGENTE — nadie teletransporta nada.
            bool enCentro = Main.dayTime && Main.time >= 26999.0;

            // corre mientras: la carrera oficial (sub 11)… o lo que falte
            // para llegar (un cliente que se enteró tarde del climax
            // TERMINA su carrera: el sol llega al centro, no salta).
            AethonBoss.TiempoCorriendo =
                (sub == 11 || ((sub >= 12 || enPelea) && !enCentro)) && !muriendo;

            // congela: el climax en adelante — pero SOLO si ya llegó (un
            // reloj a medio camino sigue corriendo hasta clavarse solo).
            AethonBoss.TiempoCongelado = (sub >= 12 || enPelea) && enCentro;

            // el aterrizaje exacto: si el propio reloj pasó de 26999 (la
            // desaceleración aterriza a 1 unidad por tick), clavarlo — es
            // MENOS de un tick de sol: nadie lo ve, no es un salto.
            if (AethonBoss.TiempoCorriendo && Main.dayTime &&
                Main.time >= 26999.0 && Main.time < 27000.0)
                Main.time = 27000.0;

            // === EL VELO (la oscuridad de WotG) ============================
            if (sub == 12 && jefe.ai[2] >= 90f)
            {
                // EL FLASH DEL CLIMAX: la luz inunda TODO — y en su pico
                // Aethon se materializa; cuando llega la oscuridad, el
                // blanco CEGA y se apaga en un crossfade de medio segundo.
                Velo.Ver(Color.White, 1f, 0.10f);
            }
            else if (laOscuridad)
            {
                // LA OSCURIDAD PRIMORDIAL: casi opaca, con la temperatura
                // violeta de la casa — las siluetas quedan al 7%.
                Velo.Ver(new Color(10, 7, 22), 0.93f, 1f / 55f);
            }
            else if (muriendo)
            {
                // la contracción final: la oscuridad se disuelve en 45 t
                // DURANTE la contracción — el estallido CEGA desde el mundo.
                Velo.Apagar(1f / 45f);
            }

            // === EL SOL NEGRO (entra con la oscuridad, se despide LENTO) ===
            float pasoSol = laOscuridad ? 1f / 40f : 1f / 130f;
            _solNegro = laOscuridad
                ? MathF.Min(1f, _solNegro + pasoSol)
                : MathF.Max(0f, _solNegro - pasoSol);

            // la despedida del sol negro puede vivir más que el velo:
            // mientras él arda, el pintor sigue encendido.
            Velo.PintoresSiempre(_solNegro > 0.002f);

            // === EL AVISO DEL GRIMORIO (una vez por llegada, cliente local) ==
            if (sub == 13 && !_avisoGrimorioHecho &&
                Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                _avisoGrimorioHecho = true;
                int nivel = NivelGrimorio();
                string clave = nivel >= 50
                    ? "Mods.AethonMod.Jefe.Aethon.OscuridadGrimorio"
                    : "Mods.AethonMod.Jefe.Aethon.OscuridadSinGrimorio";
                Main.NewText(Language.GetTextValue(clave, nivel),
                    new Color(196, 150, 255));
            }

            // === EL TEMBLOR DE LA LLEGADA (los kicks de la casa — sub 10
            //     crece hasta 13 px, sub 11 se sostiene suave mientras el
            //     tiempo corre) ==============================================
            if (Main.netMode != NetmodeID.Server && !Main.dedServ &&
                sub >= 10 && sub <= 11 && (Main.GameUpdateCount % 13u) == 0u)
            {
                float f = sub == 10 ? Math.Min(1f, jefe.ai[2] / 150f) : 0.55f;
                OndaLib.Kick(4f + 9f * f, 13);
            }

            // === LAS LUCES DEL FRAME (las excepciones de la oscuridad) ===
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                RegistrarLuces(jefe);
        }

        /// <summary>La luz murió: los fades exhalaron, el reloj vuelve al mundo.</summary>
        private static void EntierroLocal()
        {
            AethonBoss.SubLlegada = 0;
            AethonBoss.TiempoCorriendo = false;
            AethonBoss.TiempoCongelado = false;   // el sol recupera su curso
            AethonBoss.OscuridadObjetivo = false;
            _avisoGrimorioHecho = false;
            Velo.Apagar(1f / 45f);               // la oscuridad se disuelve (45 t)
        }

        // ==================================================================
        //  EL RELOJ — MODIFY TIME RATE (corre en TODAS las máquinas).
        //
        //  LA CARRERA NATURAL: mientras es de NOCHE el reloj corre a 300×
        //  (la luna barre el cielo en ~1.8 s — el alba llega SOLA, sin
        //  corte); al hacerse de día, la carrera DESESPERA: lejos del
        //  mediodía vuela (300×) y al acercarse desacelera en aproximación
        //  (rate = distancia × 0.08, piso 1) — el sol se POSA en el centro
        //  exacto como un avión que aterriza, NUNCA como un teletransporte.
        // ==================================================================
        public override void ModifyTimeRate(ref double timeRate,
            ref double tileUpdateRate, ref double eventUpdateRate)
        {
            if (AethonBoss.TiempoCorriendo)
            {
                if (!Main.dayTime)
                {
                    // la noche entera: la luna cruza y el alba llega sola
                    timeRate = 300.0;
                }
                else
                {
                    double restante = 27000.0 - Main.time;   // hasta el mediodía
                    if (restante > 6000.0) timeRate = 300.0; // lejos: vuela
                    else timeRate = Math.Max(1.0, Math.Min(300.0, restante * 0.08));
                }
                tileUpdateRate = 1.0;      // el mundo físico sigue normal
                eventUpdateRate = 0.0;     // cero tiradas de eventos en el barrido
            }
            else if (AethonBoss.TiempoCongelado)
            {
                // EL MEDIO DÍA ETERNO: el sol clavado en el centro.
                timeRate = 0.0;
            }
        }

        // ==================================================================
        //  LAS LUCES DEL FRAME — las excepciones de la oscuridad:
        //  · AETHON brilla dorado (ES luz pura); en su eclipse hasta su
        //    luz muere: se encoge y se vuelve violeta.
        //  · EL JUGADOR: solo un pequeño círculo — SOLO con Grimorio ≥ 50.
        //  · LAS BALAS de la luz (pernos, columnas, runas): se ve venir
        //    el castigo, no el terreno.
        // ==================================================================
        private static void RegistrarLuces(NPC jefe)
        {
            float t = Main.GlobalTimeWrappedHourly;
            float respira = 1f + 0.03f * MathF.Sin(t * 0.7f);   // la luz respira

            // === EL AGUJERO… LA LUZ DE AETHON (ES la luz pura) ===
            if (jefe != null && jefe.active && jefe.alpha < 200)
            {
                bool eclipse = jefe.ai[1] == 3f && jefe.ai[0] != 0f;
                // en el ECLIPSE hasta SU luz muere: se encoge y se vuelve
                // violeta — la oscuridad le cierra la mano encima
                float radio = eclipse ? 210f : 430f;
                Color tinte = eclipse
                    ? new Color(150, 105, 220)
                    : new Color(255, 240, 200);
                Velo.Luz(jefe.Center, radio * respira, tinte, true);
            }

            // === EL CÍRCULO DEL JUGADOR (solo Grimorio ≥ 50) ===
            if (NivelGrimorio() >= 50)
            {
                Player p = Main.LocalPlayer;
                if (p != null && p.active && !p.dead)
                    Velo.Luz(p.Center, 235f * respira, new Color(225, 232, 255), false);
            }

            // === LAS BALAS DE LA LUZ (la única luz que se MUEVE por el mundo) ===
            int tipo = ModContent.ProjectileType<AtaqueJefeProjectile>();
            int huecos = 0;
            for (int i = 0; i < Main.maxProjectiles && huecos < 26; i++)
            {
                Projectile pr = Main.projectile[i];
                if (pr == null || !pr.active || pr.type != tipo) continue;
                int estilo = (int)pr.ai[0];
                if (estilo != AtaqueJefeProjectile.EstiloPernoEstelar &&
                    estilo != AtaqueJefeProjectile.EstiloColumnaJuicio &&
                    estilo != AtaqueJefeProjectile.EstiloRunaMemorizada) continue;
                float rBala = estilo == AtaqueJefeProjectile.EstiloColumnaJuicio
                    ? 130f : 88f;
                Velo.Luz(pr.Center, rBala, new Color(255, 244, 214), false);
                huecos++;
            }
        }

        // ==================================================================
        //  EL PINTOR — LO QUE VIVE SOBRE LA OSCURIDAD (el contrato de la
        //  casa: el lote llega ABIERTO en aditivo·identidad y se devuelve
        //  ABIERTO en aditivo·identidad):
        //  1. EL SOL NEGRO (el eclipse de la concentración — el disco que
        //     CUBRE al sol real, el rim dorado latiendo, la corona).
        //  2. LOS DESTELLOS LEJANOS (la oscuridad está VIVA).
        //  3. LOS TELEGRAPHS (el aviso de la casa SIEMPRE se ve).
        // ==================================================================
        private static void PintarSobreElVelo(SpriteBatch sb)
        {
            if (Main.gameMenu || Main.dedServ) return;
            try
            {
                NPC jefe = BuscarJefe();
                float t = Main.GlobalTimeWrappedHourly;
                Matrix mVista = Main.GameViewMatrix.ZoomMatrix;

                // el lote llega abierto en aditivo·identidad: cerrarlo
                VFXCore.CerrarLoteSiAbierto();
                try
                {
                    // ========= 1. EL SOL NEGRO =========
                    if (_solNegro > 0.002f)
                        DibujarSolNegro(sb, t);

                    // ========= 2. LOS DESTELLOS LEJANOS =========
                    // (la oscuridad está VIVA: el cielo recuerda la luz)
                    if (Velo.Intensidad > 0.5f)
                    {
                        sb.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                            SamplerState.LinearClamp, DepthStencilState.None,
                            RasterizerState.CullNone, null, Matrix.Identity);
                        try
                        {
                            Texture2D g3 = VFXCore.SoftGlow;
                            Vector2 origenG3 = new Vector2(g3.Width, g3.Height) * 0.5f;
                            int wDev = Main.screenWidth;
                            int hDev = Main.screenHeight;
                            for (int i = 0; i < 7; i++)
                            {
                                // hash determinista de la casa: el MISMO cielo
                                // en todas las máquinas, sin sincronizar nada
                                float hx = VFXCore.Hash01(977, i, (int)(t * 0.35f));
                                float hy = VFXCore.Hash01(131, i, (int)(t * 0.35f));
                                float fase = VFXCore.Hash01(571, i, (int)(t * 0.35f));
                                float brillo = MathF.Max(0f,
                                    MathF.Sin((t * 0.35f % 1f) * 6f + fase * 40f));
                                if (brillo <= 0f) continue;
                                Vector2 dp = new Vector2(hx * wDev, hy * hDev * 0.45f);
                                sb.Draw(g3, dp, null,
                                    new Color(140, 110, 210) * (0.06f * brillo * Velo.Intensidad),
                                    0f, origenG3,
                                    new Vector2(70f / g3.Width, 70f / g3.Height),
                                    SpriteEffects.None, 0f);
                            }
                        }
                        finally { sb.End(); }
                    }

                    // ========= 3. LOS TELEGRAPHS SOBRE LA OSCURIDAD =========
                    // (la línea guía del destello y el pulso de la nova —
                    //  el aviso de la casa SIEMPRE se ve; viven en el espacio
                    //  de VISTA, como el resto del mundo)
                    if (jefe != null && Velo.Intensidad >= 0.3f)
                    {
                        sb.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                            SamplerState.LinearClamp, DepthStencilState.None,
                            RasterizerState.CullNone, null, mVista);
                        try
                        {
                            AethonBoss.DibujarTelegrafos(sb, jefe,
                                jefe.Center - Main.screenPosition);
                        }
                        finally { sb.End(); }
                    }
                }
                finally
                {
                    // DEVOLVER EL LOTE DEL CONTRATO (aditivo·identidad, abierto)
                    try
                    {
                        sb.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                            SamplerState.LinearClamp, DepthStencilState.None,
                            RasterizerState.CullNone, null, Matrix.Identity);
                    }
                    catch { }
                }
            }
            catch (Exception e)
            {
                try
                {
                    Terraria.ModLoader.Logging.PublicLogger.Error(
                        "[AethonMod] AethonLlegadaSistema: el pintor del velo falló", e);
                }
                catch { }
            }
        }

        // ==================================================================
        //  EL SOL NEGRO — EL ECLIPSE DE LA CONCENTRACIÓN.
        //
        //  Posición LITERAL del sol de vanilla (DrawSunAndMoon, la fórmula
        //  completa de la casa): transformada a píxeles con
        //  BackgroundViewMatrix.EffectMatrix. El disco negro absoluto CUBRE
        //  al sol real + el rim dorado latiendo + la corona de filamentos.
        // ==================================================================
        private static void DibujarSolNegro(SpriteBatch sb, float t)
        {
            Texture2D disco = ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/BlackDisk").Value;
            if (disco == null || disco.IsDisposed) return;

            Vector2 posCielo = AethonBoss.PosicionSolEnCielo();
            Vector2 pos = Vector2.Transform(posCielo,
                Main.BackgroundViewMatrix.EffectMatrix);

            // el tamaño del sol de vanilla + el margen del eclipse
            float solW = 80f;
            try { solW = Terraria.GameContent.TextureAssets.Sun.Value.Width; }
            catch { }
            float radioDisco = solW * 1.32f * 0.5f + 44f;
            Vector2 origenD = new Vector2(disco.Width, disco.Height) * 0.5f;

            // === EL DISCO NEGRO (alfa: cubre al sol real) ===
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Matrix.Identity);
            try
            {
                // BlackDisk: núcleo sólido hasta el 70% del radio
                float lado = (radioDisco / 0.70f) * 2f;
                sb.Draw(disco, pos, null, Color.White * _solNegro, 0f, origenD,
                    lado / disco.Width, SpriteEffects.None, 0f);

                // el aura oscura que se COME el cielo alrededor
                Texture2D glow = VFXCore.SoftGlow;
                sb.Draw(glow, pos, null,
                    new Color(8, 4, 18) * (0.55f * _solNegro), 0f,
                    new Vector2(glow.Width, glow.Height) * 0.5f,
                    new Vector2(radioDisco * 4.2f / glow.Width,
                                radioDisco * 4.2f / glow.Height),
                    SpriteEffects.None, 0f);
            }
            finally { sb.End(); }

            // === EL RIM DORADO Y LA CORONA (aditivo: el eclipse VIVE) ===
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Matrix.Identity);
            try
            {
                // EL RIM: el anillo fino justo en el borde del disco
                Texture2D ring = VFXCore.Ring;
                float ladoRing = radioDisco / 0.46f * 2f;   // la receta de VFXCore
                float latido = 0.86f + 0.14f * MathF.Sin(t * 1.3f);
                sb.Draw(ring, pos, null,
                    new Color(255, 214, 120) * (0.85f * _solNegro * latido), 0f,
                    new Vector2(ring.Width, ring.Height) * 0.5f,
                    ladoRing / ring.Width, SpriteEffects.None, 0f);
                // el segundo rim interior (la corona doble)
                sb.Draw(ring, pos, null,
                    new Color(180, 130, 255) * (0.35f * _solNegro * latido), 0f,
                    new Vector2(ring.Width, ring.Height) * 0.5f,
                    ladoRing * 0.82f / ring.Width, SpriteEffects.None, 0f);

                // LA CORONA DE RAYOS: doce filamentos girando LENTO
                Texture2D g2 = VFXCore.SoftGlow;
                Vector2 origenG = new Vector2(g2.Width, g2.Height) * 0.5f;
                float giro = t * 0.05f;
                for (int i = 0; i < 12; i++)
                {
                    float ang = giro + i * MathHelper.Pi / 6f;
                    float largo = radioDisco * (1.5f + 0.7f * MathF.Sin(t * 0.9f + i * 1.7f));
                    Color cR = (i % 3 == 0) ? new Color(190, 140, 255) : new Color(255, 214, 130);
                    sb.Draw(g2, pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                        (radioDisco + largo * 0.5f), null,
                        cR * (0.10f * _solNegro), ang, origenG,
                        new Vector2(largo / g2.Width, 6f / g2.Height),
                        SpriteEffects.None, 0f);
                }
            }
            finally { sb.End(); }
        }

        /// <summary>El jefe de la luz (o null si no está).</summary>
        private static NPC BuscarJefe()
        {
            try
            {
                int tipo = ModContent.NPCType<AethonBoss>();
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n != null && n.active && n.type == tipo) return n;
                }
            }
            catch { }
            return null;
        }
    }
}
