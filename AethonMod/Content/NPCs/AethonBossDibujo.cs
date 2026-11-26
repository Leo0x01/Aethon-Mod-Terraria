// ======================================================================
    //  AethonBossDibujo — v6.177 — EL PARTIDO DEL JEFE (la letra de la
    //  sesión 30: partir los gigantes por responsabilidades SIN tocar
    //  comportamiento). Aquí viven los DIBUJOS del jefe (PreDraw ·
    //  PostDraw · FindFrame): los mismos métodos, INTACTOS byte a
    //  byte, mudados de AethonBoss.cs por tools/parte_archivos_v6177.py
    //  — la clase es `partial`: mismo namespace, mismo nombre, mismo
    //  comportamiento. La PELEA (SetDefaults, AI, los ataques) sigue
    //  en AethonBoss.cs.
    // =====================================================================
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.GameContent.Bestiary;
using AethonMod.Content.VFX;
using AethonMod.Content.Systems;
using AethonMod.Content.Projectiles.Jefes;

namespace AethonMod.Content.NPCs
{
    public partial class AethonBoss : ModNPC
    {
        // ==================================================================
        //  EL ARTE — EL SOL DE CÓDIGO (100% píxeles de luz, cero sprites)
        //
        //  LA LECCIÓN DE LAS 3 MUERTES (dragón de código «no se parece
        //  en nada», dragón de sprites «se ve horrible», sierpe «se ve
        //  feo»): NO MÁS ANATOMÍA. La luz no tiene forma que salir mal
        //  — tiene BRILLO: núcleo, halo, rayos, coronas y chispas. Todo
        //  aditivo (la luz no proyecta silueta: IRRADIA), todo
        //  sincronizado (ai[0]/ai[1]/ai[2]/ai[3] — el contrato de la
        //  casa), todo el contrato de lote cerrado→cerrado→vanilla.
        // ==================================================================
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Main.dedServ) return false;

            // v6.51.4 — EL BESTIARIO NO corre la danza de lotes (la pose
            // vive en su CustomTexturePath; esto es el cinturón de
            // seguridad si algún otro camino dibujara al dummy).
            if (NPC.IsABestiaryIconDummy) return false;

            VFXCore.CerrarLoteSiAbierto();
            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 posC = NPC.Center - Main.screenPosition;
                float visibilidad = 1f - (NPC.alpha / 255f);
                // v6.50.56 — EL ECLIPSE MURIÓ DEL TODO (la letra: «al final
                // de la muerte del jefe, este se oscurece, NO»): el disco
                // oscuro y el apagón del cine de muerte FUERA — el dios
                // muerto JAMÁS se apaga; su luz CRECE (brilloMuerte) hasta
                // LA GRAN EXPLOSIÓN DE LUZ (v6.50.81: el estallido ES el
                // adiós — ya no hay bola final que la suceda).
                float faseInt = 0.55f + 0.45f * (Phase - 1) / 4f;   // la furia brilla más

                // === LA MUERTE: LA CARGA (todo se recoge al punto que
                //     ARDE — el sol se hace punta de lanza, no ceniza) ===
                float colapso = 1f;
                float brilloMuerte = 1f;
                if (_muriendo)
                {
                    float cargaM = Math.Min(1f, _tickMuerte / 110f);
                    colapso = MathHelper.Lerp(1f, 0.55f, cargaM);   // se recoge…
                    brilloMuerte = 1f + 1.4f * cargaM;               // …ARDIENDO: el brillo SUBE (×2.4 al final)
                }

                // === EL SOL (lote aditivo — TODO lo que brilla) ===
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    float latido = 0.84f + 0.16f * MathF.Sin(t * 1.6f);
                    // v6.50.56 — LA AGONÍA ARDE: brilloMuerte SUBE mientras
                    // muere (antes: eclipse → 0.10 — el apagón de la letra).
                    float brillo = faseInt * brilloMuerte;

                    // === 1. EL VELO (el aura violeta de la profundidad) —
                    //     v6.50.119 — EL CUERPO GIGANTE: TODO el sol de
                    //     código crece ×EscalaCuerpo (2.5 — un 150% MÁS
                    //     grande, la letra: «hacer que sean mas grande,
                    //     al menos un 150% mas grande») ===
                    LumenLib.Bloom(spriteBatch, posC, 540f * EscalaCuerpo, PaletaVioleta,
                        0.10f * brillo * visibilidad, 2);

                    // === 2. EL HALO DORADO (la corona del sol) ===
                    LumenLib.BloomPulse(spriteBatch, posC, 295f * EscalaCuerpo * latido, PaletaOro,
                        0.50f * brillo * visibilidad, t, 1.6f);

                    // === 3. LOS RAYOS RADIALES (la rueda de luz girando —
                    //     y en EL VÓRTICE la rueda ACELERA ×4: el sol que
                    //     enrosca su propia luz antes de soltar la galaxia.
                    //     v6.50.56 — también ACELERA al morir: la carga del
                    //     último aliento gira la rueda más y más rápido) ===
                    {
                        int nRayos = 8 + Phase * 2;                       // 10 en P1 → 18 en P5
                        float giro = t * ((_muriendo || NPC.ai[0] == EST_VORTICE || NPC.ai[0] == EST_DANZA) ? 0.42f : 0.10f);
                        Vector2 origen = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height) * 0.5f;
                        for (int i = 0; i < nRayos; i++)
                        {
                            float ang = giro + i * MathHelper.TwoPi / nRayos;
                            float largo = (255f + 165f * faseInt) * EscalaCuerpo *
                                (0.78f + 0.22f * MathF.Sin(t * 2.1f + i * 1.9f)) * colapso;
                            float ancho = (34f - 12f * faseInt) * EscalaCuerpo;
                            Color cR = (Phase >= 4) ?
                                Color.Lerp(PaletaOro, PaletaVioleta, 0.45f) : PaletaOro;
                            spriteBatch.Draw(VFXCore.SoftGlow, posC, null,
                                cR * (0.30f * brillo * visibilidad), ang, origen,
                                new Vector2(largo / VFXCore.SoftGlow.Width * 2f,
                                            ancho / VFXCore.SoftGlow.Height * 2f),
                                SpriteEffects.None, 0f);
                        }
                    }

                    // === 4. LAS DOS CORONAS DE PERLAS (los anillos de
                    //     Saturno: órbitas elípticas de blooms girando
                    //     en sentidos opuestos — el efecto 3D) ===
                    for (int anillo = 0; anillo < 2; anillo++)
                    {
                        float rx = (anillo == 0 ? 218f : 300f) * EscalaCuerpo * colapso;
                        float ry = (anillo == 0 ? 134f : 90f) * EscalaCuerpo * colapso;
                        float w = anillo == 0 ? 0.55f : -0.38f;          // sentidos opuestos
                        Color cP = anillo == 0 ? PaletaOro : PaletaVioleta;
                        int nPerlas = 13;
                        for (int i = 0; i < nPerlas; i++)
                        {
                            float ang = t * w + i * MathHelper.TwoPi / nPerlas;
                            Vector2 perla = posC + new Vector2(MathF.Cos(ang) * rx,
                                MathF.Sin(ang) * ry);
                            float tw = 0.5f + 0.5f * MathF.Sin(t * 3f + i * 2.1f + anillo);
                            LumenLib.Bloom(spriteBatch, perla, 12f * EscalaCuerpo * colapso, cP,
                                0.42f * (0.5f + 0.5f * tw) *
                                brillo * visibilidad, 2);
                        }
                    }

                    // === 4.5 EL ANILLO ARCOÍRIS MURIÓ (v6.50.52:
                    //     «el jefe no necesita tener un arcoiris» — la banda
                    //     vive en la Forma 3 y en la mascota; el dios de la
                    //     luz es ORO Y NÚCLEO BLANCO, nada de espectro) ===

                    // === 5. LAS CHISPAS ORBITANTES (el enjambre cercano) ===
                    int nChispas = 5 + Phase;
                    Color cMota = Phase >= 4 ? PaletaVioleta : PaletaOro;
                    for (int m = 0; m < nChispas; m++)
                    {
                        float ang = t * (1.1f + m * 0.17f) + m * 2.1f;
                        float r = 176f * EscalaCuerpo * colapso + 16f * MathF.Sin(t * 2.4f + m);
                        Vector2 mota = posC + new Vector2(MathF.Cos(ang) * r,
                            MathF.Sin(ang) * r * 0.72f);
                        LumenLib.Bloom(spriteBatch, mota, 20f * EscalaCuerpo * colapso, cMota,
                            0.40f * brillo * visibilidad, 2);
                    }

                    // === 6. EL NÚCLEO (el corazón blanco de Aethon —
                    //     v6.50.56: NUNCA más el rim del eclipse: el corazón
                    //     ARDE entero hasta el último tick) ===
                    float tamNucleo = 130f * EscalaCuerpo * latido * colapso;
                    {
                        LumenLib.Bloom(spriteBatch, posC, tamNucleo, PaletaNucleo,
                            1.0f * brillo * visibilidad, 3);
                        LumenLib.Bloom(spriteBatch, posC, tamNucleo * 0.42f, PaletaOro,
                            0.65f * brillo * visibilidad, 2);
                    }

                    // === 7. LA CARGA DEL RAYO (el crescendo del núcleo) ===
                    if (NPC.ai[1] == 1f && !_muriendo)
                    {
                        float prog = Math.Min(1f, _tickRayoLocal / 40f);
                        LumenLib.Bloom(spriteBatch, posC, (88f + 132f * prog) * EscalaCuerpo,
                            PaletaNucleo, 0.45f * prog * visibilidad, 3);
                    }

                    // === 8. LOS TELEGRAPHS (la casa: todo ataque se
                    //     anuncia — y con la oscuridad MUERTA (v6.50.41)
                    //     SIEMPRE se ven, en el propio pase del mundo) ===
                    DibujarTelegrafos(spriteBatch, NPC, posC, Phase);

                    // === 9. LA ESTELA DEL DESTELLO (los fantasmas del cruce) ===
                    if (NPC.ai[0] == EST_DESTELLO && NPC.ai[2] > 20f && !_muriendo &&
                        NPC.velocity.LengthSquared() > 400f)
                    {
                        Vector2 atras = -Vector2.Normalize(NPC.velocity);
                        for (int g = 1; g <= 4; g++)
                        {
                            Vector2 fantasma = posC + atras * (g * 74f * EscalaCuerpo);
                            LumenLib.Bloom(spriteBatch, fantasma, (62f - g * 9f) * EscalaCuerpo,
                                PaletaOro, 0.26f / g * visibilidad, 2);
                        }
                    }

                    // === 9.5 v6.50.117 — EL HILO DE BRUMA DEL ÓRBITA: la
                    //     hebra de blooms temblorosos que une al fragmento
                    //     con su hermano (la memoria de que fueron uno) ===
                    if (_estado == EST_ORBITANDO)
                    {
                        NPC hO = BuscarHermano();
                        if (hO != null)
                        {
                            Vector2 bP = hO.Center - Main.screenPosition;
                            Vector2 dH = bP - posC;
                            if (dH.LengthSquared() > 1600f)
                            {
                                const int nH = 12;
                                for (int iH = 0; iH <= nH; iH++)
                                {
                                    float f = iH / (float)nH;
                                    Vector2 pH = posC + dH * f +
                                        new Vector2(0f, MathF.Sin(t * 3f + f * 6f) * 9f);
                                    LumenLib.Bloom(spriteBatch, pH,
                                        15f + 9f * MathF.Sin(f * MathHelper.Pi),
                                        PaletaVioleta,
                                        0.30f * (0.6f + 0.4f * MathF.Sin(t * 2f + f * 5f)) * visibilidad, 2);
                                }
                            }
                        }
                    }

                    // === 9.6 v6.50.117 — LA CINEMÁTICA DE FUSIÓN: ANILLOS
                    //     DE ONDA alternando LUZ/OSCURIDAD (los pinta UNA
                    //     sola vez el de MENOR whoAmI, en el punto medio
                    //     del abrazo) y el FLASH que crece hasta el final ===
                    if (_estado == EST_FUSIONANDO)
                    {
                        NPC hF = BuscarHermano();
                        if (hF != null && NPC.whoAmI < hF.whoAmI)
                        {
                            Vector2 midP = (NPC.Center + hF.Center) * 0.5f - Main.screenPosition;
                            int fT = Math.Min(200, _tickEstado);
                            for (int r = 0; r < 5; r++)
                            {
                                int t0 = 20 + r * 40;
                                if (fT >= t0 && fT <= t0 + 60)
                                {
                                    float progR = (fT - t0) / 60f;
                                    Color cR2 = (r & 1) == 0
                                        ? new Color(255, 220, 120)   // la LUZ
                                        : new Color(90, 20, 140);    // la OSCURIDAD
                                    OndaLib.Pulse(spriteBatch, midP, progR,
                                        260f + 560f * progR, cR2, 0.75f, r * 7);
                                }
                            }
                            // EL FLASH CRECIENTE (la luz del abrazo).
                            float cargaF = fT / 200f;
                            LumenLib.Bloom(spriteBatch, midP,
                                120f + 640f * cargaF, PaletaNucleo,
                                0.35f + 0.55f * cargaF, 4);
                        }
                    }

                    // === 10. LA MUERTE: EL ESTALLIDO FINAL (el flash) ===
                    // v6.50.57 — LA INUNDACIÓN MÁS LARGA Y MÁS GRANDE (la
                    // letra: «explotar en brillo intenso»): 50 t de destello
                    // que ENGORDA — el estallido final del dios detenido,
                    // ARDIENDO en su sitio.
                    if (_muriendo && _tickMuerte >= 110 && _tickMuerte <= 160)
                    {
                        float fp = (_tickMuerte - 110) / 50f;   // 0→1: la inundación
                        float engorda = 1f + 0.35f * MathF.Sin(fp * MathHelper.Pi);
                        LumenLib.Bloom(spriteBatch, posC,
                            (1000f + 3800f * fp) * engorda, PaletaNucleo,
                            0.95f * (1f - fp * 0.55f), 4);
                        LumenLib.Bloom(spriteBatch, posC,
                            (680f + 3000f * fp) * engorda, PaletaOro,
                            0.65f * (1f - fp * 0.45f), 3);
                    }
                }
                finally { spriteBatch.End(); }

                // === EL RAYO PRIMORDIAL (PerlinBolt — el arco de la casa;
                //     corre en TODAS las máquinas leyendo ai[1]) ===
                if (NPC.ai[1] != _rayoPrevio)
                {
                    _rayoPrevio = NPC.ai[1];
                    _tickRayoLocal = 0f;
                }
                else if (NPC.ai[1] != 0f) _tickRayoLocal++;

                if (!_muriendo && (NPC.ai[1] == 1f || NPC.ai[1] == 2f))
                {
                    Player presa = Main.player[NPC.target];
                    if (presa != null && presa.active && !presa.dead)
                    {
                        bool cargando = NPC.ai[1] == 1f;
                        float prog = cargando ? Math.Min(1f, _tickRayoLocal / 40f) : 1f;
                        Vector2 nucleo = NPC.Center - Main.screenPosition;
                        Vector2 pecho = presa.Center - Main.screenPosition;
                        float ancho = (1.2f + 3.4f * prog) * (cargando ? 0.6f : 1f);
                        int flick = (int)(Main.GameUpdateCount / 3u);
                        StormLib.PerlinBolt(spriteBatch, nucleo, pecho,
                            NPC.whoAmI * 71 + 13, flick, ancho, PaletaOro, PaletaNucleo,
                            (cargando ? 0.35f : 0.95f) * visibilidad);
                    }
                }
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            finally
            {
                // v6.50.53 — EL FANTASMA DE LOS NPCS, MUERTO: el PerlinBolt
                // del RAYO (la carga y el fuego, ai[1] 1-2) deja el lote
                // ABIERTO EN ADITIVO — RayoStrip.CerrarLote reabre con
                // BlendState.Additive para los gorros del rayo, y el
                // ReabrirLoteVanilla de abajo era un NO-OP («ya hay Begin
                // vivo: no lo pisamos») → TODO NPC dibujado después del
                // jefe salía ADITIVO: el FANTASMA TRANSPARENTE del usuario
                // («hay algun ataque del jefe que al hacerlo los npc se
                // vuelven transparente, creo que es cuando lanza rayo»).
                // LA CURA: si quedó un lote abierto que NO es el de
                // vanilla, se CIERRA y se reabre el de vanilla — el estado
                // de salida de PreDraw es SIEMPRE el lote NPC de vanilla
                // (AlphaBlend + Main.Transform), con rayo o sin él.
                VFXCore.CerrarLoteSiAbierto();
                VFXCore.ReabrirLoteVanilla();
            }
            return false; // el sol de código se dibuja a sí mismo
        }
        /// <summary>
        /// LOS TELEGRAPHS PÚBLICOS (v6.50.37): la línea guía del destello
        /// y el pulso de la nova — dibujables desde CUALQUIER lote
        /// aditivo (el PreDraw del mundo los pide en su propio pase).
        /// </summary>
        internal static void DibujarTelegrafos(SpriteBatch sb, NPC npc, Vector2 posC, int faseTelegrafo)
        {
            // EL DESTELLO: la LÍNEA GUÍA — el rayo de puntería creciendo
            // en el rumbo del cruce (ai[3] = el ángulo).
            if (npc.ai[0] == EST_DESTELLO && npc.ai[2] <= 20f)
            {
                float prog = npc.ai[2] / 20f;
                float angD = npc.ai[3];
                Vector2 dirD = new Vector2(MathF.Cos(angD), MathF.Sin(angD));
                Vector2 origenL = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height) * 0.5f;
                float largoL = 1900f * prog;
                sb.Draw(VFXCore.SoftGlow, posC + dirD * (largoL * 0.5f), null,
                    OroLuz * (0.30f + 0.25f * prog), angD, origenL,
                    new Vector2(largoL / VFXCore.SoftGlow.Width,
                                (7f + 9f * prog) / VFXCore.SoftGlow.Height),
                    SpriteEffects.None, 0f);
                // EL ANILLO que se cierra (el compás del cruce).
                OndaLib.Pulse(sb, posC, prog, 220f, OroLuz, 0.55f, npc.whoAmI);
            }

            // LA NOVA: la contracción (el pulso que se APRIETA).
            if (npc.ai[0] == EST_NOVA && npc.ai[2] <= 45f)
            {
                float prog = npc.ai[2] / 45f;
                OndaLib.Pulse(sb, posC, 1f - prog,
                    420f - 300f * prog, OroLuz, 0.60f, npc.whoAmI + 3);
            }

            // v6.50.53/54 — EL ESTALLIDO RADIANTE: LA RECOGIDA — 60 t de
            //     telegrafo antes de CADA detonación (ai[3] lleva el tick de
            //     la PRÓXIMA — el multi-estallido re-dibuja su aro SIEMPRE)
            //     con CUATRO señales: EL LÍMITE (el aro del radio de peligro
            //     PULSANDO y creciendo con la fase — 600·ESCALA: la MISMA
            //     cifra de la hitbox), LAS CATORCE BRASAS (el anillo
            //     segmentado CAYÉNDOSE en espiral), EL NÚCLEO que SE LLENA
            //     y LAS AGUJAS (la luz CORRIENDO hacia el punto).
            if (npc.ai[0] == EST_ESTALLIDO && npc.ai[3] > 0f &&
                npc.ai[2] >= npc.ai[3] - 60f && npc.ai[2] < npc.ai[3])
            {
                float proxima = npc.ai[3];
                float prog = (npc.ai[2] - (proxima - 60f)) / 60f;
                float escalaR = EscalaEstallido(faseTelegrafo);   // el aro telegrafiado = la hitbox
                float tg = Main.GlobalTimeWrappedHourly;

                // (a) EL LÍMITE: el aro fino del radio de peligro (600·escala
                //     px — LA MISMA cifra de la hitbox de la onda) pulsando y
                //     ENCENDIÉNDOSE a medida que el estallido llega.
                float alfaL = 0.22f + 0.30f * prog + 0.10f * MathF.Sin(tg * 9f);
                Vector2 ringTam = new Vector2(VFXCore.Ring.Width, VFXCore.Ring.Height);
                sb.Draw(VFXCore.Ring, posC, null, OroLuz * alfaL, 0f,
                    ringTam * 0.5f, VFXCore.RingQuadSize(600f * escalaR) / ringTam,
                    SpriteEffects.None, 0f);

                // (b) LAS CATORCE BRASAS: el anillo segmentado CAYENDO al
                //     centro (760·escala → 150 — las brasas de la imagen,
                //     pero en reversa: se RECOGEN antes de explotar).
                float rB = MathHelper.Lerp(760f * escalaR, 150f, prog);
                for (int i = 0; i < 14; i++)
                {
                    float angB = i * MathHelper.TwoPi / 14f + prog * 2.4f + tg * 0.25f;
                    Vector2 e = posC + new Vector2(MathF.Cos(angB), MathF.Sin(angB)) * rB;
                    LumenLib.Bloom(sb, e, 22f + 8f * prog, OroLuz,
                        0.5f + 0.3f * prog, 2);
                }

                // (c) EL NÚCLEO que se llena (el punto de luz creciendo —
                //     el crescendo del rayo, pero GRANDE: este es EL estallido).
                LumenLib.Bloom(sb, posC, 90f + 140f * prog, NucleoBlanco,
                    0.35f + 0.50f * prog, 3);

                // (d) LAS AGUJAS que convergen (la luz CORRIENDO hacia el
                //     punto — deterministas: Hash01 con el flick del rayo).
                Vector2 origenS = new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height) * 0.5f;
                int flick = (int)(Main.GameUpdateCount / 3u);
                for (int i = 0; i < 10; i++)
                {
                    float angA = VFXCore.Hash01(npc.whoAmI, i, 50) * MathHelper.TwoPi + tg * 0.5f;
                    float rA = 620f * escalaR * (0.30f + 0.70f * VFXCore.Hash01(npc.whoAmI, i, 51 + (flick % 3)));
                    float len = 120f + 80f * prog;
                    Vector2 dirA = new Vector2(MathF.Cos(angA), MathF.Sin(angA));
                    Vector2 centroA = posC + dirA * (rA - len * 0.5f);
                    sb.Draw(VFXCore.SoftGlow, centroA, null,
                        OroLuz * (0.30f + 0.20f * prog), angA, origenS,
                        new Vector2(len / VFXCore.SoftGlow.Width, 4f / VFXCore.SoftGlow.Height),
                        SpriteEffects.None, 0f);
                }
            }
        }
    }
}
