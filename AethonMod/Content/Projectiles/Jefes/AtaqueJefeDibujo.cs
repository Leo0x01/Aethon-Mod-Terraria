// ======================================================================
    //  AtaqueJefeDibujo — v6.177 — EL PARTIDO DEL ARSENAL DEL JEFE
    //  (la letra de la sesión 30: partir los gigantes por
    //  responsabilidades SIN tocar comportamiento). Aquí viven los
    //  DIBUJOS del proyectil (PreDraw · PostDraw · DrawBehind · los
    //  pases): los mismos métodos, INTACTOS byte a byte, mudados de
    //  AtaqueJefeProjectile.cs por tools/parte_archivos_v6177.py — la
    //  clase es `partial`: mismo namespace, mismo nombre, mismo
    //  comportamiento. La PELEA (SetDefaults, AI, EL ARSENAL, el
    //  AnilloCoro) sigue en AtaqueJefeProjectile.cs.
    // =====================================================================
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AethonMod.Content.VFX;
using AethonMod.Content.Projectiles.Cosmic;
using AethonMod.Content.NPCs; // v6.50.59 — el reloj permanente pregunta la FASE del jefe

namespace AethonMod.Content.Projectiles.Jefes
{
    public partial class AtaqueJefeProjectile : ModProjectile
    {
        /// <summary>
        /// v6.50.47 — LAS ESTRUCTURAS DE CAMPO DEL JEFE DIBUJAN DETRÁS DE
        /// LAS CRIATURAS (el fix de «algunos ataques del jefe hacen que
        /// los NPC sean semitransparentes»). EL RELOJ GIGANTE, EL CORO,
        /// EL TELAR y EL DECRETO son EDIFICIOS y TRAMPAS que viven
        /// SEGUNDOS en pantalla: dibujados en el pase normal de
        /// proyectiles (DESPUÉS de los NPCs), sus velos gigantes —
        /// la MASA alpha-blend del reloj («el polvo que OCLUYE») y las
        /// capas aditivas de arena/anillos/jaula — cubrían a los NPC del
        /// pueblo y los volvían FANTASMAS (vistos a través del ataque).
        /// REGISTRADOS en behindNPCs dibujan en el pase de
        /// DrawCachedProjs(DrawCacheProjsBehindNPCs) — DESPUÉS de los
        /// tiles, ANTES de TODOS los NPCs y del jugador (verificado en el
        /// decompile del tML 2026.07.3.0): las criaturas SIEMPRE sólidas
        /// encima, las estructuras de luz debajo. Los proyectiles RÁPIDOS
        /// (pernos, flechas, tajos) siguen en el pase normal — pasan POR
        /// DELANTE como toda bala de Terraria.
        /// </summary>
        public override void DrawBehind(int index, System.Collections.Generic.List<int> behindNPCsAndTiles,
            System.Collections.Generic.List<int> behindNPCs, System.Collections.Generic.List<int> behindProjectiles,
            System.Collections.Generic.List<int> overPlayers, System.Collections.Generic.List<int> overWiresUI)
        {
            if (Estilo == EstiloRelojGigante || Estilo == EstiloCoroJefe ||
                Estilo == EstiloTelarJefe || Estilo == EstiloDecretoJefe ||
                Estilo == EstiloDanzaSolar)
                behindNPCs.Add(index);
        }
        // ==================================================================
        //  EL RENDER — cada diente con SU librería. EL FLUJO DE LA CASA:
        //  (1) los quads al BÚFER (coords de MUNDO) y su volcado propio;
        //  (2) el lote aditivo NUESTRO para las librerías de coords de
        //  PANTALLA (OndaLib/StormLib/TajoLib/LumenLib/OrbitaLib/RiftLib);
        //  (3) el lote del pase reabierto TAL CUAL (cerrado→cerrado).
        // ==================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            // v6.50.45/54 — EL RELOJ GIGANTE camina por SU camino (el renderer
            //     del BASTÓN escalado ×7.5 — LA CATEDRAL DEL TIEMPO: gestiona
            //     SUS lotes enteros — la masa, el reloj y la onda de la
            //     inversión — y devuelve el lote CERRADO, el contrato v6.10).
            if (Estilo == EstiloRelojGigante)
            {
                RelojArenaRenderer.Draw(Projectile, _edad, Seed, 7.5f);   // v6.50.54 — ×7.5: la edad de arena corre desde el nacimiento (la AI ya no le resta la caída)
                VFXCore.ReabrirLoteVanilla();
                return false;
            }

            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 pos = Projectile.Center - Main.screenPosition;

                // === FASE 1 — EL BÚFER DE QUADS (coords de MUNDO) ===
                // v6.104 — LA PURGA DE LAS TEXTURAS PLANAS (la letra:
                //     «muchos de sus ataques son texuras planas, solo un
                //     rectangulo y estan mal implementadas… asegurate de
                //     no dejar texturas planas, lisas o raras»): los
                //     CUERPOS de los proyectiles pequeños ya NO son
                //     glows estirados (rectángulos de luz) — son
                //     DESTELLOS DE 4 PUNTAS texturizados (la estrella de
                //     la casa, VFXCore.DestelloFinal) con SU núcleo y SU
                //     giro: cada proyectil se lee como lo que ES.
                switch (Estilo)
                {
                    case EstiloPuaSagrario:
                        VFXCore.Quad(Projectile.Center, TealCristal * 0.90f,
                            new Vector2(16f, 38f), Projectile.rotation, VFXCore.DestelloFinal);
                        VFXCore.Quad(Projectile.Center, BlancoCaliente * 0.7f,
                            new Vector2(7f, 15f), Projectile.rotation, VFXCore.DestelloFinal);
                        break;

                    case EstiloCoroCristal:
                        VFXCore.Quad(Projectile.Center, TealCristal * 0.95f,
                            new Vector2(20f, 20f), Projectile.rotation + t * 0.6f,
                            VFXCore.DestelloFinal);
                        break;

                    case EstiloViroteVacio:
                        VFXCore.Quad(Projectile.Center, TealVacio * 0.95f,
                            new Vector2(38f, 13f), Projectile.velocity.ToRotation(),
                            VFXCore.DestelloFinal);
                        VFXCore.Quad(Projectile.Center, VioletaVacio * 0.55f,
                            new Vector2(20f, 12f), Projectile.velocity.ToRotation());
                        break;

                    case EstiloFlechaEstelar:
                        VFXCore.Quad(Projectile.Center, AmbarEstelar * 0.95f,
                            new Vector2(30f, 11f), Projectile.rotation, VFXCore.DestelloFinal);
                        VFXCore.Quad(Projectile.Center, BlancoCaliente * 0.7f,
                            new Vector2(12f, 5f), Projectile.rotation, VFXCore.DestelloFinal);
                        break;

                    case EstiloMinaEstelar:
                    {
                        float pulso = 0.9f + 0.1f * MathF.Sin(t * 8f + Seed);
                        VFXCore.Quad(Projectile.Center, AmbarEstelar * (0.85f * pulso),
                            new Vector2(24f, 24f), t * 0.8f + Seed, VFXCore.DestelloFinal);
                        VFXCore.Quad(Projectile.Center, OroGrimorio * 0.6f,
                            new Vector2(11f, 11f));
                        break;
                    }

                    case EstiloEstrellaFugaz:
                        VFXCore.Quad(Projectile.Center, OroGrimorio * 0.95f,
                            new Vector2(26f, 26f), Projectile.rotation + t * 1.2f,
                            VFXCore.DestelloFinal);
                        VFXCore.Quad(Projectile.Center, BlancoCaliente * 0.85f,
                            new Vector2(12f, 12f), Projectile.rotation, VFXCore.DestelloFinal);
                        break;

                    case EstiloCuchillaOrbit:
                        VFXCore.Quad(Projectile.Center, EmberPortador * 0.95f,
                            new Vector2(28f, 10f), Projectile.rotation, VFXCore.DestelloFinal);
                        VFXCore.Quad(Projectile.Center, BlancoCaliente * 0.65f,
                            new Vector2(11f, 4f), Projectile.rotation, VFXCore.DestelloFinal);
                        break;

                    case EstiloCorteRealidad:
                        // El CUERPO del desgarro lo dibuja RiftLib (fase 2);
                        // aquí solo las chispas del borde.
                        break;

                    case EstiloPernoEstelar:
                    {
                        // v6.104 — EL PERNO ES UNA ESTRELLA (no un cuadro):
                        // el destello de 4 puntas de la casa con su giro
                        // propio y su núcleo — la v6.50.117 — LA PALETA
                        // DEL PERNO (ai[1] ≥ 2 = el gemelo oscuro) sigue
                        // viva.
                        Color cLuzP = Par >= 2f ? new Color(110, 30, 170) : LuzPrimordial;
                        Color cOroP = Par >= 2f ? new Color(48, 14, 76) : OroGrimorio;
                        VFXCore.Quad(Projectile.Center, cLuzP * 0.95f,
                            new Vector2(22f, 22f),
                            Projectile.rotation + t * 0.5f + Seed, VFXCore.DestelloFinal);
                        VFXCore.Quad(Projectile.Center, cOroP * 0.7f,
                            new Vector2(9f, 9f));
                        break;
                    }

                    case EstiloColumnaJuicio:
                    {
                        // v6.113 — EL MURO (la letra: «los 3 pilares no
                        // deben bajar deben ocupar toda la pantalla»): el
                        // cuerpo lo dibuja HazLib en LA FASE 1.5 a lo largo
                        // de TODA la pantalla; aquí queda el CHARCO de la
                        // base con SU pulso por tiempo (telegrafo tenue ·
                        // erupción encendida · disolución apagándose).
                        float t2 = _edad;
                        float caida = Projectile.ai[1] >= 1f
                            ? (Projectile.ai[1] >= 2f
                                ? MathF.Max(0f, 1f - (t2 - 42f) / 30f)
                                : 1f)
                            : 0.30f + 0.70f * (t2 / 26f);
                        float pulso = 0.85f + 0.15f * MathF.Sin(t * 5f + Seed);
                        VFXCore.Quad(Projectile.Center, OroGrimorio * (0.40f * caida * pulso),
                            new Vector2(150f, 52f));
                        VFXCore.Quad(Projectile.Center, BlancoCaliente * (0.50f * caida),
                            new Vector2(64f, 24f));
                        break;
                    }

                    case EstiloPilarAparicion:
                    {
                        // v6.50.44 — EL PILAR DEL CIELO: la columna de luz
                        // que CAE (0-40 t: la punta desciende del cielo a la
                        // base), SE SOSTIENE mientras el jefe baja por ella
                        // y se DISUELVE al final (fade por ai[1]). TRES
                        // velos apilados (el manto dorado ancho, el cuerpo
                        // blanco y el núcleo cegador) + el CHARCO DE LUZ en
                        // la base + la PUNTA brillante mientras cae.
                        float p = Math.Min(1f, _edad / 40f);          // el descenso
                        float disolver = Projectile.ai[1] >= 1f
                            ? Math.Max(0f, 1f - _ticksDisolver / 40f) : 1f;
                        float pulso = 0.85f + 0.15f * MathF.Sin(t * 5f + Seed);
                        const float altura = 900f;

                        // el cuerpo del pilar (anclado arriba, la punta baja).
                        // v6.104 — EL CUERPO YA NO SON QUADS PLANOS: lo
                        //     dibuja HazLib en LA FASE 1.5 (TRES velos con
                        //     SU ruido — el manto dorado ancho, el cuerpo
                        //     blanco y el núcleo cegador); la geometría se
                        //     recalcula allá con las MISMAS leyes.
                        float punta = Projectile.Center.Y - altura * (1f - p);

                        // EL CHARCO DE LUZ en la base (donde la columna se
                        // posa — el escenario de la pelea queda BAÑADO).
                        if (p >= 1f)
                        {
                            VFXCore.Quad(Projectile.Center, OroGrimorio * (0.50f * disolver * pulso),
                                new Vector2(170f, 34f));
                            VFXCore.Quad(Projectile.Center, BlancoCaliente * (0.45f * disolver),
                                new Vector2(80f, 16f));
                        }

                        // LA PUNTA que cae (la cabeza del destello mientras
                        // desciende — lo que se ve venir del cielo).
                        if (p < 1f)
                        {
                            Vector2 tip = new Vector2(Projectile.Center.X, punta);
                            VFXCore.Quad(tip, OroGrimorio * 0.85f, new Vector2(90f, 40f));
                            VFXCore.Quad(tip, new Color(255, 253, 244) * 0.90f,
                                new Vector2(40f, 18f));
                        }
                        break;
                    }

                    case EstiloNubeNebulosa:
                    {
                        // v6.113 — LA NUBE CON VOLUMEN (la letra: «algunos
                        // ataques del jefe se ven planos o con texturas
                        // raras»): la flor de humo plano (UN SoftGlow
                        // violeta) murió — cada uno de los 6 puffs ahora
                        // es TRES CAPAS anidadas (el manto exterior, el
                        // cuerpo medio y el corazón claro) con SU fase de
                        // pulso independiente: la nube respira por dentro
                        // y ya no se lee como un disco plano.
                        for (int i = 0; i < 6; i++)
                        {
                            float h = VFXCore.Hash01(Seed, i, (int)(_edad / 40f));
                            float ang = h * MathHelper.TwoPi + t * 0.4f;
                            float r = 30f + 26f * VFXCore.Hash01(Seed, i + 40, 0);
                            Vector2 off = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * r;
                            float fade = 1f - _edad / 360f;
                            float fase = t * 2f + i * 1.7f;
                            float late = 0.8f + 0.2f * MathF.Sin(fase);
                            // TRES CAPAS: manto (violeta) · cuerpo (lila) ·
                            // corazón (casi blanco) — el volumen de verdad.
                            VFXCore.Quad(Projectile.Center + off,
                                NebulosaNube * fade,
                                new Vector2(40f, 36f) * late);
                            VFXCore.Quad(Projectile.Center + off,
                                new Color(122, 78, 188) * (fade * 0.8f),
                                new Vector2(26f, 22f) * (1.05f - 0.15f * MathF.Sin(fase)));
                            VFXCore.Quad(Projectile.Center + off,
                                new Color(224, 176, 255) * (fade * 0.6f),
                                new Vector2(12f, 10f) * (0.9f + 0.25f * MathF.Sin(fase + 1.3f)));
                        }
                        break;
                    }

                    case EstiloRunaMemorizada:
                    {
                        // EL SIGILO: pentágono de cápsulas doradas + núcleo.
                        for (int i = 0; i < 5; i++)
                        {
                            float a0 = Projectile.rotation + i * MathHelper.TwoPi / 5f;
                            float a1 = Projectile.rotation + (i + 1) * MathHelper.TwoPi / 5f;
                            Vector2 p0 = Projectile.Center + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * 26f;
                            Vector2 p1 = Projectile.Center + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * 26f;
                            VFXCore.Line(p0, p1, OroGrimorio * 0.55f, 3f);
                        }
                        VFXCore.Quad(Projectile.Center, BlancoCaliente * 0.9f,
                            new Vector2(10f, 10f));
                        break;
                    }

                    case EstiloEstallidoMina:
                    {
                        // EL CUERPO DEL ESTALLIDO: la bola de chispas (20 t).
                        float prog = _edad / 20f;
                        float r = 66f * (0.3f + 0.7f * prog);
                        for (int i = 0; i < 8; i++)
                        {
                            float ang = i * MathHelper.TwoPi / 8f + Seed * 0.01f;
                            Vector2 off = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * r;
                            VFXCore.Quad(Projectile.Center + off,
                                AmbarEstelar * (0.8f * (1f - prog)),
                                new Vector2(14f, 14f) * (1f - prog * 0.5f));
                        }
                        break;
                    }

                    // === v6.50.45 — EL TELAR: LOS HILOS de la constelación
                    //     (coords de MUNDO — la figura que se dibuja sola,
                    //     estrella a estrella, cerrándose N → 1) ===
                    case EstiloTelarJefe:
                    {
                        // v6.50.46 — LOS HILOS en ORDEN DE ÍNDICE (la jaula
                        // conecta las puntas en su orden natural, aunque el
                        // jefe las haya clavado por el SALTO de la estrella).
                        int puntasV = Par > 0.5f ? 7 : 5;
                        Vector2[] clavadasV = new Vector2[7];
                        int nV = 0;
                        for (int i = 0; i < puntasV; i++)
                            if (_estrellaClavada[i]) clavadasV[nV++] = _estrellas[i];
                        if (nV >= 2)
                        {
                            float brillo = 0.35f + 0.65f * _telarFlash;
                            Color cHilo = Color.Lerp(new Color(150, 170, 255), new Color(255, 255, 255), _telarFlash);
                            for (int i = 0; i < nV; i++)
                            {
                                int sig = (i + 1) % nV;
                                // el último hilo CIERRA la jaula solo con TODAS
                                if (sig == 0 && nV < puntasV) sig = nV - 1;
                                if (i == sig) continue;
                                VFXCore.Line(clavadasV[i], clavadasV[sig],
                                    cHilo * (0.4f * brillo), 2.4f + 2.6f * _telarFlash);
                            }
                        }
                        break;
                    }

                    // === v6.50.45 — EL DECRETO: LOS GLIFOS rúnicos cabalgando
                    //     el anillo (24 cápsulas que se RE-ESCRIBEN en cada
                    //     ejecución — el contrato del bastón) ===
                    case EstiloDecretoJefe:
                    {
                        // v6.50.46 — el radio se COMPUTA de la edad (ai[0]
                        // es el ESTILO — jamás se escribe).
                        // v6.104 — LOS GLIFOS YA NO SON CÁPSULAS PLANAS:
                        //     cada glifo del decreto es un DESTELLO DE 4
                        //     PUNTAS girando (la runa-estrella de la
                        //     casa) — el anillo se lee ESCRITO, no
                        //     punteado.
                        float radioD = Math.Min(80f + 2f * _edad, 960f);
                        int beatIdx = (int)(_edad / 15f);
                        for (int i = 0; i < 24; i++)
                        {
                            float ang = i * MathHelper.TwoPi / 24f + t * 0.10f;
                            Vector2 g = Projectile.Center +
                                new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.86f) * radioD;
                            float h = VFXCore.Hash01(Seed, i, 91 + (beatIdx % 7));
                            VFXCore.Quad(g, ColorOroEclipse * (0.55f + 0.45f * h),
                                new Vector2(9f + 6f * h, 9f + 6f * h),
                                ang + t * 0.8f + i, VFXCore.DestelloFinal);
                        }
                        break;
                    }

                    // === v6.50.53 — EL ESTALLIDO RADIANTE, FASE MUNDO (LA
                    //     IMAGEN DEL USUARIO hecha quads — TODO determinista:
                    //     Hash01 hace que TODAS las pantallas dibujen EL
                    //     MISMO estallido): LOS 44 RAYOS de largo VARIABLE
                    //     (los hay cortos al 25% y los que CRUZAN la
                    //     pantalla — cada uno con SU línea núcleo BLANCA y
                    //     SU halo que se CALIENTA con la distancia: oro →
                    //     ámbar → brasa, el degradé de la imagen), EL ANILLO
                    //     SEGMENTADO (14
                    //     EMISORES brillantes creciendo hacia afuera — NO un
                    //     aro continuo: la firma de la imagen) y LA CRUZ
                    //     ANAMÓRFICA (el bloom que estira H y V — el
                    //     lens-flare del cine) ===
                    case EstiloEstallidoRadiante:
                    case EstiloMuerteFinal:
                    {
                        // v6.50.80 — LA MUERTE comparte TODO el dibujo del
                        // estallido (los 44 rayos, el anillo segmentado, la
                        // cruz anamórfica) — su escala 1.85 hace el resto.

                        // EL TEMPO: crece (0-14 t), ARDE (14-55) y se
                        // disuelve (55-150) — la rotación LENTA de la
                        // imagen (~0.5 RPM).
                        float crec = Math.Min(1f, _edad / 14f);
                        float fade = _edad < 55f ? 1f : Math.Max(0f, 1f - (_edad - 55f) / 95f);
                        float giro = _edad * 0.0045f;
                        Vector2 cE = Projectile.Center;

                        // === (a) LOS RAYOS (el corazón de la imagen) —
                        //     v6.104 — YA NO SON QUADS PLANOS: cada rayo
                        //     lo dibuja HazLib en LA FASE 1.5 (SU núcleo
                        //     blanco, SU halo CALIENTE y SU filo de borde
                        //     — los rayos HIERVEN como los del arma).
                        //     Aquí solo viven las puntas-estrella.
                        // ===
                        for (int i = 0; i < 44; i++)
                        {
                            float h0 = VFXCore.Hash01(Seed, i, 10);
                            float h1 = VFXCore.Hash01(Seed, i, 11);
                            float h2 = VFXCore.Hash01(Seed, i, 12);
                            float h3 = VFXCore.Hash01(Seed, i, 13);
                            float ang = giro + i * MathHelper.TwoPi / 44f +
                                (h0 - 0.5f) * (MathHelper.TwoPi / 44f) * 1.15f;
                            float largo = (250f + 730f * h1) * crec * EscalaEstallido;
                            float vidaR = 75f + 45f * h2;
                            float alfaR = fade * (_edad < vidaR ? 1f :
                                Math.Max(0f, 1f - (_edad - vidaR) / 22f));
                            if (alfaR <= 0.02f || largo < 8f) continue;
                            Vector2 dirR = new Vector2(MathF.Cos(ang), MathF.Sin(ang));
                            // LA PUNTA-ESTRELLA (la cuenta de luz del borde —
                            // el sello de cada rayo, texturizado).
                            VFXCore.Quad(cE + dirR * (46f + largo),
                                BlancoCaliente * (0.75f * alfaR),
                                new Vector2(16f + 10f * h3, 16f + 10f * h3),
                                ang + t * 1.1f + i, VFXCore.DestelloFinal);
                        }

                        // === (b) EL ANILLO SEGMENTADO (14 EMISORES creciendo
                        //     hacia afuera con su flicker — 150 → 760·ESCALA px) ===
                        float rAnillo = MathHelper.Lerp(150f, 760f,
                            Math.Min(1f, _edad / 55f)) * EscalaEstallido;
                        float alfaA = _edad < 70f ? 1f :
                            Math.Max(0f, 1f - (_edad - 70f) / 45f);
                        if (alfaA > 0.02f)
                        {
                            int beatE = (int)(_edad / 6f);   // el flicker segmentado
                            for (int i = 0; i < 14; i++)
                            {
                                float angE = giro * 1.6f + i * MathHelper.TwoPi / 14f;
                                float hE = VFXCore.Hash01(Seed, i, 60 + (beatE % 5));
                                Vector2 e = cE + new Vector2(MathF.Cos(angE), MathF.Sin(angE)) * rAnillo;
                                VFXCore.Quad(e,
                                    BlancoCaliente * (0.80f * alfaA * (0.7f + 0.3f * hE)),
                                    new Vector2(26f + 10f * hE, 26f + 10f * hE));
                                VFXCore.Quad(e, OroGrimorio * (0.45f * alfaA),
                                    new Vector2(58f, 58f));
                            }
                        }

                        // === (c) LA CRUZ ANAMÓRFICA (el bloom estirado en H
                        //     y V — vive solo en el AUGE, muere a los 55 t) ===
                        float alfaC = Math.Max(0f, 1f - _edad / 55f) * 0.55f;
                        if (alfaC > 0.02f)
                        {
                            VFXCore.Quad(cE, BlancoCaliente * alfaC,
                                new Vector2(1150f * crec * EscalaEstallido, 7f));
                            VFXCore.Quad(cE, BlancoCaliente * alfaC,
                                new Vector2(7f, 1150f * crec * EscalaEstallido));
                            VFXCore.Quad(cE, OroGrimorio * (alfaC * 0.6f),
                                new Vector2(760f * crec * EscalaEstallido, 16f));
                            VFXCore.Quad(cE, OroGrimorio * (alfaC * 0.6f),
                                new Vector2(16f, 760f * crec * EscalaEstallido));
                        }
                        break;
                    }

                    // === v6.50.54 — LA DANZA SOLAR (fase mundo): LA RUEDA —
                    //     seis rayos de 860 px (núcleo blanco + halo dorado —
                    //     las primitivas del Estallido) girando LENTO; cada
                    //     tanda nace TRANSLÚCIDA (alfa 0.35 — la regla de la casa)
                    //     y se ENCIENDE cuando mata ===
                    case EstiloDanzaSolar:
                    {
                        int tandaV = (int)(_edad / 175f);
                        float tEdadV = _edad % 175f;
                        float alfaT = tEdadV <= 25f
                            ? 0.35f
                            : Math.Min(1f, 0.35f + (tEdadV - 25f) / 20f * 0.65f);
                        float fadeV = _edad > 520f
                            ? Math.Max(0f, 1f - (_edad - 520f) / 35f) : 1f;
                        float giroV = _edad * 0.0125f + tandaV * 0.35f;
                        const float largoD = 860f;
                        // v6.104 — LOS RAYOS YA NO SON QUADS PLANOS: cada
                        //     uno de los seis lo dibuja HazLib en LA FASE
                        //     1.5 (SU núcleo blanco, SU halo dorado y SU
                        //     filo — la rueda HIERVE). Aquí viven SOLO las
                        //     puntas-estrella y el cubo.
                        for (int i = 0; i < 6; i++)
                        {
                            float angV = giroV + i * MathHelper.TwoPi / 6f;
                            Vector2 dirV = new Vector2(MathF.Cos(angV), MathF.Sin(angV));
                            // LA PUNTA (la cuenta de luz del borde — la
                            // estrella de 4 puntas de la casa).
                            VFXCore.Quad(Projectile.Center + dirV * (60f + largoD),
                                BlancoCaliente * (0.60f * alfaT * fadeV),
                                new Vector2(20f, 20f), angV + t * 1.4f + i,
                                VFXCore.DestelloFinal);
                        }
                        // EL CUBO de la rueda (el núcleo que la sostiene).
                        VFXCore.Quad(Projectile.Center,
                            BlancoCaliente * (0.55f * alfaT * fadeV),
                            new Vector2(95f, 95f));
                        VFXCore.Quad(Projectile.Center,
                            OroGrimorio * (0.30f * alfaT * fadeV),
                            new Vector2(190f, 190f));
                        break;
                    }

                    // === v6.50.54 — LA LANZA ETERNA (fase mundo): el
                    //     TELEGRAFO (la línea fina translúcida de origen a
                    //     destino + la punta pulsando) y EL VUELO (el filo
                    //     blanco largo + el halo) ===
                    case EstiloLanzaEterna:
                    {
                        Vector2 destinoV = new Vector2(Projectile.ai[1], Projectile.ai[2]);
                        Vector2 segV = destinoV - Projectile.Center;
                        float largoSeg = segV.Length();
                        if (largoSeg > 4f)
                        {
                            float angSeg = segV.ToRotation();
                            if (_edad < 50f)
                            {
                                // EL TELEGRAFO (translúcido — inofensivo).
                                float pulsoL = 0.25f + 0.15f * MathF.Sin(t * 6f + Seed);
                                float llenado = Math.Min(1f, _edad / 50f);
                                Vector2 medioSeg = Projectile.Center + segV * (0.5f * llenado);
                                VFXCore.Quad(medioSeg, OroGrimorio * pulsoL,
                                    new Vector2(largoSeg * llenado, 3f), angSeg);
                                // LA PUNTA (donde va a golpear — la marca).
                                VFXCore.Quad(destinoV, BlancoCaliente * (0.5f + 0.3f * MathF.Sin(t * 8f)),
                                    new Vector2(16f, 16f));
                                VFXCore.Quad(destinoV, OroGrimorio * 0.25f,
                                    new Vector2(34f, 34f));
                            }
                            else
                            {
                                // EL VUELO: v6.104 — el filo YA NO ES UN QUAD
                                //     PLANO: lo dibuja HazLib en LA FASE 1.5
                                //     (el filo blanco + el halo con SU ruido).
                                //     Aquí vive SOLO el corazón-estrella.
                                VFXCore.Quad(Projectile.Center, BlancoCaliente * 0.9f,
                                    new Vector2(22f, 22f), Projectile.rotation + t * 1.2f,
                                    VFXCore.DestelloFinal);
                            }
                        }
                        break;
                    }

                    // === v6.50.54 — LA CORONA ETERNA (fase mundo): el ANILLO
                    //     guía translúcido + LAS CATORCE PLUMAS prismáticas
                    //     (cada una SU color del espectro + núcleo blanco +
                    //     la estela corta de la rotación) ===
                    case EstiloCoronaEterna:
                    {
                        float cicloCV = _edad % 600f;
                        float radioCV = cicloCV < 300f
                            ? MathHelper.Lerp(240f, 640f, cicloCV / 300f)
                            : MathHelper.Lerp(640f, 240f, (cicloCV - 300f) / 300f);
                        float giroCV = _edad * 0.024f;
                        float fadeCV = _edad > 610f
                            ? Math.Max(0f, 1f - (_edad - 610f) / 45f) : 1f;

                        // EL ANILLO GUÍA (la única advertencia — translúcido).
                        VFXCore.Quad(Projectile.Center, OroGrimorio * (0.14f * fadeCV),
                            new Vector2(radioCV * 2.174f, radioCV * 0.85f * 2.174f), 0f,
                            VFXCore.Ring);

                        // LAS PLUMAS.
                        for (int f = 0; f < 14; f++)
                        {
                            float angF = giroCV + f * MathHelper.TwoPi / 14f;
                            Vector2 dirF = new Vector2(MathF.Cos(angF), MathF.Sin(angF) * 0.85f);
                            Vector2 plumaF = Projectile.Center + dirF * radioCV;
                            Color cPrisma = ColorPrismaLocal(f / 14f + _edad * 0.003f);
                            // EL CUERPO de la pluma (SU color + el núcleo blanco).
                            VFXCore.Quad(plumaF, cPrisma * (0.85f * fadeCV),
                                new Vector2(20f, 20f));
                            VFXCore.Quad(plumaF, BlancoCaliente * (0.85f * fadeCV),
                                new Vector2(8f, 8f));
                            // LA ESTELA (la cola corta de la rotación).
                            Vector2 atrasF = new Vector2(dirF.Y, -dirF.X / 0.85f);
                            VFXCore.Quad(plumaF + atrasF * 22f, cPrisma * (0.35f * fadeCV),
                                new Vector2(34f, 7f), atrasF.ToRotation());
                        }
                        break;
                    }

                    // === v6.50.57 — EL SOL DEL DIOS, FASE MUNDO (EL SOL QUE
                    //     EL DIOS SE VUELVE y luego LANZA: el MISMO look del
                    //     sol — rueda de 12 rayos + DOS coronas de perlas +
                    //     núcleo — blanco-dorado en la asunción y el vuelo,
                    //     y ENROJECIENDO al hincharse en LA GIGANTE, hasta
                    //     LA EXPLOSIÓN de luz, bruma y formas) ===
                    case EstiloSolJefe:
                    {
                        // EL ACTO (v6.50.60 — por ESTADO, no por reloj: la
                        // ESPOLOTE DE PROXIMIDAD puede encender la gigante
                        // a los 120 t de vida igual que a los 380 — el
                        // render lee la MISMA máquina de la IA).
                        float escSol = Math.Max(0.30f, Projectile.scale);
                        bool gigante = _solGigante;
                        bool estallo = _solExploto;
                        float g = gigante
                            ? MathHelper.Clamp(_tickGigante / 30f, 0f, 1f)
                            : 0f;
                        float ease = g * g * (3f - 2f * g);

                        // LA PALETA DEL ACTO: blanco-dorado → brasa roja.
                        Color rayoSol = Color.Lerp(BlancoCaliente,
                            new Color(255, 74, 36), ease);
                        Color perlaSol = Color.Lerp(OroGrimorio,
                            new Color(255, 120, 50), ease);
                        Color nucleoSol = Color.Lerp(BlancoCaliente,
                            new Color(255, 150, 90), ease * 0.9f);

                        if (!estallo)
                        {
                            float naceSol = Math.Min(1f, _edad / 40f);
                            float latidoSol = 0.86f + 0.14f *
                                MathF.Sin(t * (gigante ? 3.6f : 1.9f));

                            // LA RUEDA DE RAYOS (12 — el sol del dios; en la
                            // gigante TIEMBLAN: el largo jitterea al borde).
                            float giroSol = t * (gigante ? 0.34f : 0.16f);
                            for (int i = 0; i < 12; i++)
                            {
                                float angSol = giroSol + i * MathHelper.TwoPi / 12f;
                                float jitter = gigante
                                    ? 0.82f + 0.18f * VFXCore.Hash01(Seed, i, (int)(t * 1.5f))
                                    : 0.80f + 0.20f * MathF.Sin(t * 2.2f + i * 1.7f);
                                float largoSol = (120f + 60f * naceSol) * jitter * escSol;
                                VFXCore.Quad(Projectile.Center, rayoSol * (0.34f * naceSol),
                                    new Vector2(largoSol * 2f, 20f * escSol), angSol,
                                    VFXCore.SoftGlow);
                            }

                            // LAS DOS CORONAS DE PERLAS (+ LA TERCERA CORONA
                            // DE BRASA que solo la gigante trae).
                            int coronas = gigante ? 3 : 2;
                            for (int anilloS = 0; anilloS < coronas; anilloS++)
                            {
                                float rxS = (anilloS == 0 ? 118f :
                                    anilloS == 1 ? 158f : 206f) * naceSol * escSol;
                                float ryS = (anilloS == 0 ? 72f :
                                    anilloS == 1 ? 48f : 86f) * naceSol * escSol;
                                float wS = anilloS == 0 ? 0.55f :
                                    anilloS == 1 ? -0.38f : 0.72f;
                                Color cPS = anilloS == 2
                                    ? new Color(255, 110, 50) : perlaSol;
                                int nPerlas = anilloS == 2 ? 14 : 11;
                                for (int i = 0; i < nPerlas; i++)
                                {
                                    float angPS = t * wS + i * MathHelper.TwoPi / nPerlas;
                                    Vector2 perlaS = Projectile.Center + new Vector2(
                                        MathF.Cos(angPS) * rxS, MathF.Sin(angPS) * ryS);
                                    float twS = 0.5f + 0.5f *
                                        MathF.Sin(t * 3f + i * 2.1f + anilloS);
                                    VFXCore.Quad(perlaS,
                                        cPS * (0.50f * naceSol * (0.5f + 0.5f * twS)),
                                        new Vector2(16f, 16f) * escSol, 0f, VFXCore.SoftGlow);
                                }
                            }

                            // EL NÚCLEO (el corazón — blanco-dorado ARDIENDO,
                            // brasa al hincharse).
                            float nucleoS = 84f * latidoSol * naceSol * escSol;
                            VFXCore.Quad(Projectile.Center, nucleoSol * (1.0f * naceSol),
                                new Vector2(nucleoS * 1.5f, nucleoS * 1.5f), 0f,
                                VFXCore.SoftGlow);
                            VFXCore.Quad(Projectile.Center,
                                perlaSol * (0.65f * naceSol),
                                new Vector2(nucleoS * 0.42f * 1.5f, nucleoS * 0.42f * 1.5f),
                                0f, VFXCore.SoftGlow);
                        }
                        else
                        {
                            // === LA EXPLOSIÓN: LUZ + BRUMA + FORMAS ===
                            // v6.50.60 — el derrite lee el compás de la
                            // gigante (la espolote puede encenderla a
                            // CUALQUIER edad — el disco se funde igual).
                            float fp = MathHelper.Clamp(
                                (_tickGigante - 30f) / 50f, 0f, 1f);
                            float derrite = 1f - fp;

                            // LA CRUZ ANAMÓRFICA (la firma del estallido —
                            // BLANCA al principio, BRASA después).
                            VFXCore.Quad(Projectile.Center, nucleoSol * (0.85f * derrite),
                                new Vector2(760f, 60f), t * 0.02f, VFXCore.SoftGlow);
                            VFXCore.Quad(Projectile.Center, nucleoSol * (0.85f * derrite),
                                new Vector2(60f, 500f), t * 0.02f, VFXCore.SoftGlow);

                            // LA BRUMA (nubes de fuego translúcidas derivando
                            // hacia afuera — deterministas por semilla;
                            // v6.50.58: 10 → 18 nubes y alfa 0.16 → 0.20:
                            // «necesitan MUCHA bruma al explotar»).
                            for (int i = 0; i < 18; i++)
                            {
                                float hB = VFXCore.Hash01(Seed + 1, i, 7);
                                float angB = hB * MathHelper.TwoPi;
                                float distB = 80f + 340f * fp * (0.5f + hB);
                                Vector2 brumaQ = Projectile.Center + new Vector2(
                                    MathF.Cos(angB), MathF.Sin(angB) * 0.82f) * distB;
                                float tamB = (180f + 240f * hB) * (0.6f + 0.4f * fp);
                                Color cBruma = Color.Lerp(
                                    new Color(255, 170, 90), new Color(200, 90, 60), hB);
                                VFXCore.Quad(brumaQ, cBruma * (0.20f * derrite),
                                    new Vector2(tamB, tamB * 0.8f), angB + fp * 0.4f,
                                    VFXCore.SoftGlow);
                            }

                            // LAS FORMAS (esquirlas-estrella girando que vuelan
                            // hacia afuera — cada una SU rumbo y SU giro).
                            Texture2D destSol = VFXCore.DestelloFinal;
                            if (destSol != null)
                            {
                                for (int i = 0; i < 12; i++)
                                {
                                    float hF = VFXCore.Hash01(Seed + 2, i, 11);
                                    float angF = hF * MathHelper.TwoPi +
                                        fp * (0.6f + hF * 0.8f) * (i % 2 == 0 ? 1f : -1f);
                                    float distF = 120f + 420f * fp * (0.4f + hF * 0.6f);
                                    Vector2 forma = Projectile.Center + new Vector2(
                                        MathF.Cos(angF), MathF.Sin(angF)) * distF;
                                    float tamF = (40f + 70f * hF) * derrite;
                                    if (tamF < 2f) continue;
                                    Color cF = i % 3 == 0
                                        ? new Color(255, 90, 40)
                                        : i % 3 == 1 ? OroGrimorio : BlancoCaliente;
                                    VFXCore.Quad(forma, cF * (0.55f * derrite),
                                        new Vector2(tamF, tamF), angF + t * 0.06f,
                                        destSol);
                                }
                            }
                        }
                        break;
                    }
                }
                VFXCore.FlushAdditive(null, false);

                // ============================================================
                //  === FASE 1.5 — LOS HACES DE LA CASA (v6.104) ===
                //  LA MUERTE DE LAS TEXTURAS PLANAS (la letra: «muchos de
                //  sus ataques son texuras planas, solo un rectangulo y
                //  estan mal implementadas… asegurate de no dejar
                //  texturas planas, lisas o raras»): los CUERPOS largos
                //  que eran glows estirados (la columna, el pilar, los
                //  rayos de la danza, el filo de la lanza y los 44 rayos
                //  del estallido) los dibuja ahora HazLib — la rejilla de
                //  quads con color por vértice, el grumo de ruido, el
                //  núcleo caliente y EL FILO DE BORDE rgb=0 (los picos
                //  matemáticamente imposibles): TODO hierve como los
                //  haces de las armas.
                // ============================================================
                if (!Main.dedServ &&
                    (Estilo == EstiloColumnaJuicio || Estilo == EstiloPilarAparicion ||
                     Estilo == EstiloDanzaSolar || Estilo == EstiloLanzaEterna ||
                     Estilo == EstiloEstallidoRadiante || Estilo == EstiloMuerteFinal))
                {
                    try
                    {
                        HazLib.Abrir();
                        try
                        {
                            switch (Estilo)
                            {
                                // === EL MURO DEL JUICIO (v6.113): de TECHO a
                                //     SUELO de la pantalla — el velo dorado +
                                //     el núcleo blanco + el corazón, HIRVIENDO ===
                                case EstiloColumnaJuicio:
                                {
                                    // LAS FASES: 0 telégrafo (línea fina) ·
                                    // 1 erupción (la pared encendida) ·
                                    // 2 disolución (se apaga hacia arriba).
                                    float fase = Projectile.ai[1];
                                    float tC = _edad;
                                    if (fase < 1f)
                                    {
                                        // EL TELÉGRAFO: la línea fina que vibra
                                        // (crece con la edad — la advertencia).
                                        float aviso = 0.35f + 0.65f * (tC / 26f);
                                        float vibra = 0.75f + 0.25f * MathF.Sin(t * 21f + Seed);
                                        Vector2 arriba = new Vector2(Projectile.Center.X,
                                            Main.screenPosition.Y - 80f) - Main.screenPosition;
                                        Vector2 abajo = new Vector2(Projectile.Center.X,
                                            Main.screenPosition.Y + Main.screenHeight + 80f) - Main.screenPosition;
                                        _hazAlfa = aviso * vibra;
                                        _hazTinte = OroGrimorio;
                                        HazLib.PrepararPar(arriba, abajo, 9f, 7f, 0f, 18);
                                        HazLib.Dibujar(LeyTinteHazVelo, null, HazLib.FilasVelo);
                                        _hazTinte = BlancoCaliente;
                                        HazLib.PrepararPar(arriba, abajo, 4f, 3f, 0f, 18);
                                        HazLib.Dibujar(LeyTinteHazNucleo, null, HazLib.FilasNucleo);
                                    }
                                    else
                                    {
                                        // LA PARED: la erupción encendida y su
                                        // disolución (el fade del final).
                                        float alfaC = fase < 2f
                                            ? 1f
                                            : MathF.Max(0f, 1f - (tC - 42f) / 30f);
                                        float encoge = fase < 2f ? 1f : alfaC;
                                        if (alfaC > 0.03f)
                                        {
                                            Vector2 arriba = new Vector2(Projectile.Center.X,
                                                Main.screenPosition.Y - 80f) - Main.screenPosition;
                                            Vector2 abajo = new Vector2(Projectile.Center.X,
                                                Main.screenPosition.Y + Main.screenHeight + 80f) - Main.screenPosition;
                                            _hazAlfa = alfaC;
                                            _hazTinte = OroGrimorio;
                                            HazLib.PrepararPar(arriba, abajo, 98f * encoge, 80f * encoge, 0f, 18);
                                            HazLib.Dibujar(LeyTinteHazVelo, null, HazLib.FilasVelo);
                                            _hazTinte = BlancoCaliente;
                                            HazLib.PrepararPar(arriba, abajo, 44f * encoge, 32f * encoge, 0f, 18);
                                            HazLib.Dibujar(LeyTinteHazNucleo, LeyVerticeHazNucleo, HazLib.FilasNucleo);
                                            _hazTinte = new Color(255, 253, 244);
                                            HazLib.PrepararPar(arriba, abajo, 18f * encoge, 12f * encoge, 0f, 18);
                                            HazLib.Dibujar(LeyTinteHazNucleo, null, HazLib.FilasNucleo);
                                        }
                                    }
                                    break;
                                }

                                // === EL PILAR DE LA APARICIÓN: TRES velos
                                //     (manto dorado · cuerpo blanco · núcleo) ===
                                case EstiloPilarAparicion:
                                {
                                    float pP = Math.Min(1f, _edad / 40f);
                                    float disolverP = Projectile.ai[1] >= 1f
                                        ? Math.Max(0f, 1f - _ticksDisolver / 40f) : 1f;
                                    const float alturaP = 900f;
                                    if (pP > 0.02f && disolverP > 0.03f)
                                    {
                                        float puntaP = Projectile.Center.Y - alturaP * (1f - pP);
                                        Vector2 arriba = new Vector2(Projectile.Center.X,
                                            Projectile.Center.Y - alturaP) - Main.screenPosition;
                                        Vector2 abajo = new Vector2(Projectile.Center.X,
                                            puntaP) - Main.screenPosition;
                                        _hazAlfa = disolverP;
                                        _hazTinte = OroGrimorio;
                                        HazLib.PrepararPar(arriba, abajo, 95f, 80f, 0f, 14);
                                        HazLib.Dibujar(LeyTinteHazVelo, null, HazLib.FilasVelo);
                                        _hazTinte = BlancoCaliente;
                                        HazLib.PrepararPar(arriba, abajo, 40f, 32f, 0f, 16);
                                        HazLib.Dibujar(LeyTinteHazNucleo, LeyVerticeHazNucleo, HazLib.FilasNucleo);
                                        _hazTinte = new Color(255, 253, 244);
                                        HazLib.PrepararPar(arriba, abajo, 15f, 11f, 0f, 16);
                                        HazLib.Dibujar(LeyTinteHazNucleo, null, HazLib.FilasNucleo);
                                    }
                                    break;
                                }

                                // === LA DANZA SOLAR: los SEIS rayos de la
                                //     rueda (núcleo blanco + halo dorado) ===
                                case EstiloDanzaSolar:
                                {
                                    int tandaH = (int)(_edad / 175f);
                                    float tEdadH = _edad % 175f;
                                    float alfaTH = tEdadH <= 25f
                                        ? 0.35f
                                        : Math.Min(1f, 0.35f + (tEdadH - 25f) / 20f * 0.65f);
                                    float fadeH = _edad > 520f
                                        ? Math.Max(0f, 1f - (_edad - 520f) / 35f) : 1f;
                                    float giroH = _edad * 0.0125f + tandaH * 0.35f;
                                    const float largoDH = 860f;
                                    _hazAlfa = alfaTH * fadeH;
                                    if (_hazAlfa > 0.03f)
                                    {
                                        Vector2 cH = Projectile.Center - Main.screenPosition;
                                        for (int i = 0; i < 6; i++)
                                        {
                                            float angH = giroH + i * MathHelper.TwoPi / 6f;
                                            Vector2 dirH = angH.ToRotationVector2();
                                            Vector2 aH = cH + dirH * 60f;
                                            Vector2 bH = cH + dirH * (60f + largoDH);
                                            _hazTinte = OroGrimorio;
                                            HazLib.PrepararPar(aH, bH, 30f, 16f, 0f, 14);
                                            HazLib.Dibujar(LeyTinteHazVelo, null, HazLib.FilasVelo);
                                            _hazTinte = BlancoCaliente;
                                            HazLib.PrepararPar(aH, bH, 11f, 5f, 0f, 14);
                                            HazLib.Dibujar(LeyTinteHazNucleo, LeyVerticeHazNucleo, HazLib.FilasNucleo);
                                        }
                                    }
                                    break;
                                }

                                // === LA LANZA ETERNA EN VUELO: el filo blanco
                                //     + el halo dorado (el telegrafo ya era
                                //     fino a propósito — el VUELO no) ===
                                case EstiloLanzaEterna:
                                {
                                    if (_edad >= 50f)
                                    {
                                        Vector2 aL = (Projectile.Center - Projectile.rotation.ToRotationVector2() * 60f)
                                            - Main.screenPosition;
                                        Vector2 bL = (Projectile.Center + Projectile.rotation.ToRotationVector2() * 60f)
                                            - Main.screenPosition;
                                        _hazAlfa = 0.95f;
                                        _hazTinte = OroGrimorio;
                                        HazLib.PrepararPar(aL, bL, 26f, 14f, 0f, 10);
                                        HazLib.Dibujar(LeyTinteHazVelo, null, HazLib.FilasVelo);
                                        _hazTinte = BlancoCaliente;
                                        HazLib.PrepararPar(aL, bL, 10f, 4f, 0f, 10);
                                        HazLib.Dibujar(LeyTinteHazNucleo, LeyVerticeHazNucleo, HazLib.FilasNucleo);
                                    }
                                    break;
                                }

                                // === EL ESTALLIDO RADIANTE / LA MUERTE: LOS
                                //     44 RAYOS (núcleo blanco + halo CALIENTE
                                //     en UNA pasada — el degradé de la imagen
                                //     vive en el VÉRTICE) ===
                                case EstiloEstallidoRadiante:
                                case EstiloMuerteFinal:
                                {
                                    float crecH = Math.Min(1f, _edad / 14f);
                                    float fadeH2 = _edad < 55f ? 1f :
                                        Math.Max(0f, 1f - (_edad - 55f) / 95f);
                                    float giroH2 = _edad * 0.0045f;
                                    Vector2 cE2 = Projectile.Center - Main.screenPosition;
                                    for (int i = 0; i < 44; i++)
                                    {
                                        float h0 = VFXCore.Hash01(Seed, i, 10);
                                        float h1 = VFXCore.Hash01(Seed, i, 11);
                                        float h2 = VFXCore.Hash01(Seed, i, 12);
                                        float h3 = VFXCore.Hash01(Seed, i, 13);
                                        float angR = giroH2 + i * MathHelper.TwoPi / 44f +
                                            (h0 - 0.5f) * (MathHelper.TwoPi / 44f) * 1.15f;
                                        float largoR = (250f + 730f * h1) * crecH * EscalaEstallido;
                                        float vidaR = 75f + 45f * h2;
                                        float alfaR = fadeH2 * (_edad < vidaR ? 1f :
                                            Math.Max(0f, 1f - (_edad - vidaR) / 22f));
                                        if (alfaR <= 0.02f || largoR < 8f) continue;
                                        _hazAlfa = alfaR;
                                        _hazAncho = 3.2f + 2.6f * h3 + 9f * h0;  // núcleo + halo
                                        Vector2 dirR = angR.ToRotationVector2();
                                        Vector2 aR = cE2 + dirR * 46f;
                                        Vector2 bR = cE2 + dirR * (46f + largoR);
                                        HazLib.PrepararPar(aR, bR, _hazAncho, _hazAncho * 0.5f, 0f, 12);
                                        HazLib.Dibujar(LeyTinteHazNucleo, LeyVerticeRayoEstallido, HazLib.FilasNucleo);
                                    }
                                    break;
                                }
                            }
                        }
                        finally { HazLib.Cerrar(); }
                    }
                    catch (System.Exception exRegistro) { global::AethonMod.Content.Systems.RegistroSistema.Fallo("AtaqueJefeProjectile", exRegistro); }
                    // (el Cerrar de HazLib deja SU lote aditivo abierto: se
                    // cierra aquí para que la FASE 2 haga su Begin limpia —
                    // el mismo estado en que la 1.5 no hubiera corrido)
                    VFXCore.CerrarLoteSiAbierto();
                }

                // v6.50.58 — EL DISCO DEL PROYECTIL SOL (encima de todo lo
                // demás del sol del dios: la SUPERFICIE DE PLASMA del arma —
                // la letra: «el proyectil debe ser el mismo que lanza el
                // jefe actualmente mas el proyectil Sol funcionando»;
                // la explosión sigue siendo LA NUESTRA — NO la nova del arma).
                if (Estilo == EstiloSolJefe)
                    DibujarDiscoSolDios();

                // === FASE 2 — EL LOTE ADITIVO DE LAS LIBRERÍAS (PANTALLA) ===
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                switch (Estilo)
                {
                    // === LA PÚA: el halo que respira clavada ===
                    case EstiloPuaSagrario:
                    {
                        LumenLib.Bloom(Main.spriteBatch, pos, 20f, TealCristal,
                            _clavada ? 0.55f : 0.8f);
                        if (_clavada)
                        {
                            float prog = (_edad - 150) / 60f;
                            if (prog > 0f && prog < 1f)
                                OndaLib.Pulse(Main.spriteBatch, pos, prog, 70f,
                                    TealCristal, 0.6f, Seed);
                        }
                        break;
                    }

                    // === EL CORO: la esquirla y su canto ===
                    case EstiloCoroCristal:
                    {
                        LumenLib.Bloom(Main.spriteBatch, pos, 18f, TealCristal,
                            _lanzado ? 0.9f : 0.6f);
                        if (!_lanzado)
                            OrbitaLib.AnilloFino(pos, 26f, t * 2.4f,
                                OrbitaLib.Tint(TealCristal, 0.30f));
                        break;
                    }

                    // === EL VIROTE: el parpadeo entre fases ===
                    case EstiloViroteVacio:
                    {
                        float fase = 0.55f + 0.45f * MathF.Sin(t * 11f + Seed);
                        LumenLib.Bloom(Main.spriteBatch, pos, 22f * fase, TealVacio, 0.75f * fase);
                        Vector2 cola = _posAnterior - Main.screenPosition;
                        StormLib.Bolt(Main.spriteBatch, cola, pos, Seed,
                            StormLib.FlickTick(t, 16f), 2.2f,
                            VioletaVacio, TealVacio, 0.55f * fase);
                        break;
                    }

                    // === LA FLECHA: la ESTELA DE FANTASMAS ===
                    case EstiloFlechaEstelar:
                    {
                        LumenLib.Bloom(Main.spriteBatch, pos, 16f, AmbarEstelar, 0.7f);
                        // La cola historia: 4 fantasmas detrás (deterministas).
                        for (int g = 1; g <= 4; g++)
                        {
                            Vector2 fantasma = pos - Projectile.velocity * (1.1f * g);
                            float alfa = 0.4f * (1f - g / 5f);
                            LumenLib.Bloom(Main.spriteBatch, fantasma, 12f,
                                AmbarEstelar, alfa, 2);
                        }
                        break;
                    }

                    // === LA MINA: el aviso de armado ===
                    case EstiloMinaEstelar:
                    {
                        float progArmado = MathHelper.Clamp(_edad / 40f, 0f, 1f);
                        OndaLib.Pulse(Main.spriteBatch, pos,
                            (_edad % 50f) / 50f, 120f, AmbarEstelar,
                            0.35f + 0.25f * progArmado, Seed);
                        LumenLib.BloomPulse(Main.spriteBatch, pos, 20f, OroGrimorio,
                            0.5f + 0.3f * progArmado, t, 3f);
                        break;
                    }

                    // === LA FUGAZ: la marca de suelo antes del impacto ===
                    case EstiloEstrellaFugaz:
                    {
                        LumenLib.Bloom(Main.spriteBatch, pos, 22f, OroGrimorio, 0.85f);
                        // La marca: un anillo fino en el SUELO bajo la estrella.
                        Vector2 suelo = pos;
                        Point tile = Projectile.Center.ToTileCoordinates();
                        for (int ty = 0; ty < 40; ty++)
                        {
                            Point abajo = new Point(tile.X, tile.Y + ty);
                            if (abajo.X > 5 && abajo.X < Main.maxTilesX - 5 &&
                                abajo.Y > 5 && abajo.Y < Main.maxTilesY - 5)
                            {
                                Tile tl = Main.tile[abajo.X, abajo.Y];
                                if (tl != null && tl.HasTile && Main.tileSolid[tl.TileType])
                                {
                                    suelo = new Vector2(abajo.X * 16f + 8f, abajo.Y * 16f) - Main.screenPosition;
                                    break;
                                }
                            }
                        }
                        // v6.49 — EL MEDIO-ANILLO DE SUELO que CRECE mientras la
                        // estrella cae (OndaLib.GroundVisual — la promesa del
                        // comentario por fin cumplida: la marca "respira" el
                        // impacto que viene, pegada al piso de verdad).
                        float progCaida = MathHelper.Clamp(_edad / 90f, 0f, 1f);
                        OndaLib.GroundVisual(Main.spriteBatch, suelo,
                            MathHelper.Min(progCaida * 1.3f, 1f), 74f,
                            AmbarEstelar, 0.55f);
                        OrbitaLib.AnilloFino(suelo, 46f, 0f,
                            OrbitaLib.Tint(AmbarEstelar, 0.4f));
                        break;
                    }

                    // === EL TAJO DEL PORTADOR: marca, luego el corte ===
                    case EstiloTajoPortador:
                    {
                        if (_edad <= 23)
                        {
                            // LA MARCA que se afila (Telegrafo corto).
                            Vector2 fin = pos + new Vector2(MathF.Cos(Par), MathF.Sin(Par)) * 60f;
                            OndaLib.Telegrafo(Main.spriteBatch, pos, fin,
                                _edad / 24f, EmberPortador, 3f);
                        }
                        else
                        {
                            float prog = MathF.Min(1f, (_edad - 24f) / 8f);
                            TajoLib.Tajo(pos, 56f, Par - 0.6f, Par + 0.6f,
                                prog, 1f - prog * 0.3f, 13f,
                                EmberPortador, BlancoCaliente, Seed, t);
                            LumenLib.Bloom(Main.spriteBatch, pos, 26f, EmberPortador,
                                0.7f * (1f - prog));
                        }
                        break;
                    }

                    // === LA CUCHILLA: el filo giratorio ===
                    case EstiloCuchillaOrbit:
                    {
                        LumenLib.Bloom(Main.spriteBatch, pos, 18f, EmberPortador, 0.75f);
                        if (!_lanzado)
                            OrbitaLib.AnilloFino(_centroOrbita - Main.screenPosition, 104f,
                                t * 1.8f, OrbitaLib.Tint(EmberPortador, 0.22f));
                        break;
                    }

                    // === EL CORTE DE REALIDAD: la PARED de desgarro ===
                    case EstiloCorteRealidad:
                    {
                        Vector2 dir = new Vector2(MathF.Cos(Par), MathF.Sin(Par));
                        float largo = 240f;
                        Vector2 origen = Projectile.Center - dir * (largo * 0.5f);
                        float prog = MathHelper.Clamp(_edad / 330f, 0f, 1f);
                        RiftLib.TearVacio(Main.spriteBatch,
                            origen - Main.screenPosition, dir, largo,
                            MathF.Min(1f, 0.25f + _edad / 60f), 34f, Seed, t);
                        break;
                    }

                    // === EL PERNO: el latido estelar ===
                    case EstiloPernoEstelar:
                    {
                        LumenLib.BloomPulse(Main.spriteBatch, pos, 20f,
                            Par >= 2f ? new Color(110, 30, 170) : LuzPrimordial,   // v6.50.117 — la paleta del gemelo
                            0.85f, t, 5f);
                        break;
                    }

                    // === LA COLUMNA: la punta que cae y el latido del juicio ===
                    case EstiloColumnaJuicio:
                    {
                        float intensidad = Projectile.ai[1] >= 1f ? 1f : 0.35f + 0.65f * (_edad / 45f);
                        // LA PUNTA: la cabeza de la columna (donde nace el manto).
                        LumenLib.BloomPulse(Main.spriteBatch, pos, 34f * intensidad,
                            BlancoCaliente, 0.9f * intensidad, t, 6f);
                        // EL LATIDO del juicio (el pulso que anuncia la caída).
                        if (Projectile.ai[1] < 1f)
                        {
                            OndaLib.Pulse(Main.spriteBatch, pos,
                                (_edad % 45f) / 45f, 120f * intensidad,
                                OroGrimorio, 0.5f, Seed);
                        }
                        break;
                    }

                    // === EL PILAR: el bloom de la base y de la punta viva ===
                    case EstiloPilarAparicion:
                    {
                        float p = Math.Min(1f, _edad / 40f);
                        float disolver = Projectile.ai[1] >= 1f
                            ? Math.Max(0f, 1f - _ticksDisolver / 40f) : 1f;
                        // LA BASE: el corazón del charco de luz.
                        LumenLib.BloomPulse(Main.spriteBatch, pos,
                            60f * disolver, OroGrimorio, 0.85f * disolver, t, 3.2f);
                        // LA PUNTA mientras cae: el cometa que desciende.
                        if (p < 1f)
                        {
                            Vector2 tip = pos - new Vector2(0f, 900f * (1f - p));
                            LumenLib.Bloom(Main.spriteBatch, tip, 44f,
                                BlancoCaliente, 0.95f, 2);
                        }
                        break;
                    }

                    // === LA NUBE: los anillos de la zona que quema ===
                    case EstiloNubeNebulosa:
                    {
                        float fade = 1f - _edad / 360f;
                        OrbitaLib.AnilloFino(pos, 54f, t * 0.8f,
                            OrbitaLib.Tint(LuzPrimordial, 0.30f * fade));
                        OrbitaLib.AnilloFino(pos, 40f, -t * 1.1f,
                            OrbitaLib.Tint(VioletaVacio, 0.22f * fade));
                        LumenLib.Bloom(Main.spriteBatch, pos, 30f, VioletaVacio, 0.35f * fade);
                        break;
                    }

                    // === LA RUNA MEMORIZADA: el sigilo dorado vivo ===
                    case EstiloRunaMemorizada:
                    {
                        LumenLib.BloomPulse(Main.spriteBatch, pos, 24f, OroGrimorio,
                            0.8f, t, 2.2f);
                        OrbitaLib.AnilloFino(pos, 30f, -Projectile.rotation,
                            OrbitaLib.Tint(OroGrimorio, 0.35f));
                        break;
                    }

                    // === EL ESTALLIDO DE MINA: la cruz de luz ===
                    case EstiloEstallidoMina:
                    {
                        float prog = _edad / 20f;
                        StormLib.ImpactFlash(Main.spriteBatch, pos, 40f,
                            AmbarEstelar, 1f - prog, t);
                        OndaLib.Pulse(Main.spriteBatch, pos, prog, 130f,
                            OroGrimorio, 0.7f * (1f - prog), Seed);
                        break;
                    }

                    // === v6.50.45 — EL CORO ESPECTRAL: las notas y sus anillos ===
                    case EstiloCoroJefe:
                    {
                        // LA ESCALA (dorada → ceniza — la del bastón).
                        Color[] escala =
                        {
                            new(255, 214, 110), new(255, 190, 92), new(225, 165, 96),
                            new(198, 140, 82), new(172, 112, 66), new(150, 96, 55),
                        };
                        // LAS NOTAS: orbes de luz con su latido propio.
                        for (int i = 0; i < 6; i++)
                        {
                            Vector2 np = _coroNotas[i] - Main.screenPosition;
                            float tw = 0.6f + 0.4f * MathF.Sin(t * 3f + i * 2.1f);
                            LumenLib.Bloom(Main.spriteBatch, np, 16f * tw,
                                escala[i], 0.8f * tw, 2);
                            LumenLib.Bloom(Main.spriteBatch, np, 6f,
                                new Color(255, 248, 220), 0.9f, 2);
                        }
                        // LOS ANILLOS: frentes de onda expandiéndose (y su ECO
                        // tenue detrás — la despedida del canto).
                        float radioFinalV = Par > 0.5f ? 300f : 260f;
                        for (int a = 0; a < _coroAnillos.Count; a++)
                        {
                            AnilloCoro anillo = _coroAnillos[a];
                            Vector2 rp = anillo.Origen - Main.screenPosition;
                            float prog = anillo.Edad / 44f;
                            OndaLib.Pulse(Main.spriteBatch, rp, prog, radioFinalV,
                                escala[anillo.Nota % 6], 0.65f * (1f - prog * 0.5f),
                                Seed + anillo.Nota * 13);
                            if (prog > 0.25f)
                                OndaLib.Pulse(Main.spriteBatch, rp,
                                    prog - 0.25f, radioFinalV,
                                    escala[anillo.Nota % 6], 0.22f, Seed + anillo.Nota * 7);
                        }
                        break;
                    }

                    // === v6.50.45 — EL TELAR: las ESTRELLAS clavadas (la figura
                    //     la dibujan los hilos de la FASE 1; aquí viven las
                    //     estrellas — destellos de 4 puntas con parpadeo propio) ===
                    case EstiloTelarJefe:
                    {
                        int puntasR = Par > 0.5f ? 7 : 5;
                        for (int i = 0; i < puntasR; i++)
                        {
                            if (!_estrellaClavada[i]) continue;
                            Vector2 sp = _estrellas[i] - Main.screenPosition;
                            float tw = 0.55f + 0.45f * MathF.Sin(t * 2.6f + i * 1.9f);
                            Color cE = LumenLib.Hue(VFXCore.Hash01(Seed, i, 87) * 0.86f,
                                0.62f, 0.95f);
                            LumenLib.Flare(Main.spriteBatch, sp, 26f + 14f * _telarFlash,
                                cE, (0.55f + 0.45f * _telarFlash) * tw, t * 0.7f + i);
                            LumenLib.Bloom(Main.spriteBatch, sp, 10f,
                                new Color(255, 255, 255), 0.85f * tw, 2);
                        }
                        // LA SEÑAL del centro (donde nació la figura — el
                        // compás que la presa debe leer).
                        OndaLib.Pulse(Main.spriteBatch, pos,
                            (_edad % 40f) / 40f, 120f,
                            new Color(150, 170, 255), 0.4f, Seed);
                        break;
                    }

                    // === v6.50.45 — EL DECRETO DEL ECLIPSE: el círculo, la onda
                    //     viajera, LA MARCA DEL OJO y el flash de la ejecución ===
                    case EstiloDecretoJefe:
                    {
                        int faseD = Math.Max(2, (int)Par);
                        float radioMaxD = 600f + (faseD - 2) * 120f;
                        float radio = Math.Min(80f + 2f * _edad, radioMaxD);
                        float cyc = _edad % 15f;
                        float flare = cyc < 5f ? 1f - cyc / 5f : 0f;

                        // EL ANILLO de fuego oscuro (dos aros contrarrotantes
                        // + el borde ámbar DESGARRADO del bastón).
                        OrbitaLib.AnilloFino(pos, radio * 0.99f, t * 0.22f,
                            OrbitaLib.Tint(ColorVioletaEclipse, 0.55f));
                        OrbitaLib.AnilloFino(pos, radio * 1.04f, -t * 0.16f,
                            OrbitaLib.Tint(ColorOroEclipse, 0.4f + 0.3f * flare));
                        LumenLib.Bloom(Main.spriteBatch, pos, radio * 0.5f,
                            ColorVioletaEclipse, 0.10f + 0.10f * flare, 2);

                        // LA ONDA VIAJERA: del centro al borde en los 8 t
                        // previos a la ejecución (el telégrafo del tajo).
                        if (cyc >= 7f)
                        {
                            float prog = (cyc - 7f) / 8f;
                            OndaLib.Pulse(Main.spriteBatch, pos, prog, radio,
                                ColorOroEclipse, 0.55f, Seed);
                        }

                        // LA MARCA DEL OJO sobre la presa más cercana dentro
                        // (esclerótica + iris + PUPILA VERTICAL que se
                        // CONTRAE los 6 t previos al tajo — el ojo del bastón).
                        Player marcado = Presa();
                        if (marcado != null &&
                            Vector2.Distance(marcado.Center, Projectile.Center) <= radio)
                        {
                            Vector2 ep = marcado.Center - Main.screenPosition
                                - new Vector2(0f, 58f);
                            float contraccion = cyc >= 9f ? (cyc - 9f) / 6f : 0f;
                            // esclerótica (aplanada — un ojo que FLOTa).
                            LumenLib.Bloom(Main.spriteBatch, ep, 30f,
                                new Color(255, 244, 214), 0.35f, 2);
                            // iris.
                            LumenLib.Bloom(Main.spriteBatch, ep, 14f,
                                ColorVioletaEclipse, 0.7f, 2);
                            // LA PUPILA VERTICAL: se cierra antes del corte.
                            float anchoP = MathHelper.Lerp(5.5f, 1.2f, contraccion);
                            Main.spriteBatch.Draw(VFXCore.SoftGlow, ep, null,
                                new Color(255, 250, 230) * 0.9f, 0f,
                                new Vector2(VFXCore.SoftGlow.Width, VFXCore.SoftGlow.Height) * 0.5f,
                                new Vector2(anchoP / VFXCore.SoftGlow.Width,
                                            16f / VFXCore.SoftGlow.Height),
                                SpriteEffects.None, 0f);
                        }

                        // EL FLASH de la ejecución (la sobre-exposición).
                        if (flare > 0f)
                            LumenLib.Bloom(Main.spriteBatch, pos, radio * 1.1f,
                                new Color(255, 244, 214), 0.22f * flare, 2);
                        break;
                    }

                    // === v6.50.53 — EL ESTALLIDO RADIANTE, FASE PANTALLA (EL
                    //     PUNTO DE LUZ de la imagen): EL NÚCLEO que crece
                    //     hasta ~950 px y RESPIRA, LA ESTRELLA de 8 rayos
                    //     girando LENTA (el lens-flare del cine), LA ONDA
                    //     expansiva y LAS BOKEH (los puntos de luz dispersos
                    //     de la imagen — deterministas, como todo lo demás) ===
                    case EstiloEstallidoRadiante:
                    case EstiloMuerteFinal:
                    {
                        float crecS = Math.Min(1f, _edad / 16f);
                        float fadeS = _edad < 55f ? 1f :
                            Math.Max(0f, 1f - (_edad - 55f) / 95f);
                        float respira = 0.95f + 0.05f * MathF.Sin(t * 4.2f);
                        float escS = EscalaEstallido;   // v6.50.54 — «un poco más grande»

                        // EL NÚCLEO INUNDANDO (el blanco que crece hasta
                        // ~950·escala px — el corazón que se queda viendo).
                        LumenLib.Bloom(Main.spriteBatch, pos,
                            (170f + 780f * crecS) * respira * escS, BlancoCaliente,
                            0.95f * fadeS, 4);
                        LumenLib.BloomPulse(Main.spriteBatch, pos,
                            (150f + 190f * crecS) * escS, OroGrimorio,
                            0.55f * fadeS, t, 2.6f);

                        // LA ESTRELLA DE DESTELLO (el flare de 8 rayos + núcleo
                        // caliente — la rotación LENTA de la imagen).
                        Texture2D dest = VFXCore.DestelloFinal;
                        if (dest != null)
                        {
                            float esc = (1500f * crecS * escS) / dest.Width *
                                (0.92f + 0.08f * MathF.Sin(t * 3.4f));
                            Main.spriteBatch.Draw(dest, pos, null,
                                OrbitaLib.Tint(BlancoCaliente, 0.60f * fadeS),
                                t * 0.06f,
                                new Vector2(dest.Width, dest.Height) * 0.5f, esc,
                                SpriteEffects.None, 0f);
                        }

                        // LA ONDA EXPANSIVA (el frente que barre — una sola,
                        // clara: el golpe ya pasó, esto es la firma).
                        if (_edad < 80f)
                            OndaLib.Pulse(Main.spriteBatch, pos, _edad / 80f,
                                760f * escS, OroGrimorio, 0.50f * (1f - _edad / 80f), Seed);

                        // LAS BOKEH (los puntos de luz dispersos de la imagen:
                        // 26 chispas fijas que giran despacito y titilan).
                        for (int i = 0; i < 26; i++)
                        {
                            float h0 = VFXCore.Hash01(Seed, i, 30);
                            float h1 = VFXCore.Hash01(Seed, i, 31);
                            float tw = 0.35f + 0.65f *
                                (0.5f + 0.5f * MathF.Sin(t * 2.6f + i * 1.9f));
                            float rB = (90f + 640f * h1) * crecS * escS;
                            float angB = h0 * MathHelper.TwoPi + t * 0.05f;
                            Vector2 b = pos + new Vector2(MathF.Cos(angB), MathF.Sin(angB)) * rB;
                            LumenLib.Bloom(Main.spriteBatch, b, 8f + 10f * h1,
                                (i % 2 == 0) ? OroGrimorio : AmbarEstelar,
                                0.45f * fadeS * tw, 2);
                        }
                        break;
                    }

                    // === v6.50.57 — EL SOL DEL DIOS, FASE PANTALLA (EL LENS-
                    //     FLARE del sol: el bloom que crece con el acto —
                    //     blanco-dorado → brasa — la estrella giratoria y,
                    //     en LA EXPLOSIÓN, la ONDA que se expande) ===
                    case EstiloSolJefe:
                    {
                        // v6.50.60 — por ESTADO (la espolote de la IA),
                        // no por reloj: el flare sigue a la gigante ESTÉ
                        // donde ESTÉ la edad del sol.
                        float escSol2 = Math.Max(0.30f, Projectile.scale);
                        bool gigante2 = _solGigante;
                        bool estallo2 = _solExploto;
                        float g2 = gigante2
                            ? MathHelper.Clamp(_tickGigante / 30f, 0f, 1f) : 0f;
                        float ease2 = g2 * g2 * (3f - 2f * g2);
                        float naceSol2 = Math.Min(1f, _edad / 40f);
                        float fadeSol = estallo2
                            ? Math.Max(0f, 1f - (_tickGigante - 30f) / 50f)
                            : 1f;

                        // LA PALETA DEL ACTO.
                        Color cSol2 = Color.Lerp(BlancoCaliente,
                            new Color(255, 90, 40), ease2);
                        Color cSolOro = Color.Lerp(OroGrimorio,
                            new Color(255, 120, 50), ease2);

                        // EL NÚCLEO (el corazón que crece con el acto: 130 en
                        // el vuelo → ~780 en la gigante — un sol de verdad).
                        float respiraS = 0.95f + 0.05f * MathF.Sin(t * (gigante2 ? 5.2f : 4.2f));
                        LumenLib.Bloom(Main.spriteBatch, pos,
                            (130f + 150f * naceSol2) * escSol2 * respiraS, cSol2,
                            0.95f * naceSol2 * fadeSol, 4);
                        LumenLib.BloomPulse(Main.spriteBatch, pos,
                            (95f + 45f * naceSol2) * escSol2, cSolOro,
                            0.60f * naceSol2 * fadeSol, t, gigante2 ? 4.6f : 2.4f);

                        // LA ESTRELLA DE DESTELLO (el flare de 8 rayos ×2).
                        Texture2D destS2 = VFXCore.DestelloFinal;
                        if (destS2 != null)
                        {
                            float escS2 = (560f * naceSol2 * escSol2) / destS2.Width *
                                (0.92f + 0.08f * MathF.Sin(t * 3.4f));
                            Main.spriteBatch.Draw(destS2, pos, null,
                                OrbitaLib.Tint(cSol2, 0.60f * naceSol2 * fadeSol),
                                t * 0.10f,
                                new Vector2(destS2.Width, destS2.Height) * 0.5f, escS2,
                                SpriteEffects.None, 0f);
                            Main.spriteBatch.Draw(destS2, pos, null,
                                OrbitaLib.Tint(cSolOro, 0.40f * naceSol2 * fadeSol),
                                -t * 0.07f + 0.4f,
                                new Vector2(destS2.Width, destS2.Height) * 0.5f,
                                escS2 * 0.6f, SpriteEffects.None, 0f);
                        }

                        // LA ONDA DE LA EXPLOSIÓN (el anillo que se expande
                        // con el estallido — la promesa del daño ya pagado).
                        if (estallo2)
                        {
                            OndaLib.Pulse(Main.spriteBatch, pos,
                                (_tickGigante - 30f) / 50f, 640f,
                                new Color(255, 130, 60), 0.65f * fadeSol, Seed);
                            OndaLib.Pulse(Main.spriteBatch, pos,
                                Math.Max(0f, (_tickGigante - 38f) / 50f), 460f,
                                OroGrimorio, 0.45f * fadeSol, Seed + 3);
                        }
                        break;
                    }
                }
                Main.spriteBatch.End();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            finally
            {
                // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
                // llegó cerrado por un mod ajeno, se cura — el restore condicional
                // devolvía el veneno y tML mataba al proyectil: active=false).
                VFXCore.ReabrirLoteVanilla();
            }
            return false;
        }
        // ==================================================================
        //  v6.50.58 — EL DISCO DEL PROYECTIL SOL (la superficie de plasma
        //  del ARMA, encima del sol del dios)
        // ==================================================================

        /// <summary>
        /// v6.50.58 — EL SOL DEL DIOS VISTE EL PROYECTIL SOL (la letra de la
        /// ronda: «el proyectil debe ser el mismo que lanza el jefe
        /// actualmente mas el proyectil Sol funcionando, pero que no use la
        /// explocion del proyectil sol, que usa la explocion del brillo
        /// actual»): ENCIMA de los rayos/perlas/núcleo que ya tenía, el sol
        /// gana la SUPERFICIE DE PLASMA del arma — el MISMO pipeline del
        /// SunProjectile (glow coronal → backglow → aura de ruido → el DISCO
        /// del SunShader), recolor BLANCO-DORADO (la paleta pedida desde la
        /// ronda pasada) que ENROJECE al hincharse en la GIGANTE. La
        /// explosión NO cambia: la nuestra de siempre (cruz, ondas, esquirlas
        /// y la BRUMA MASIVA nueva) — la StyleNova del arma NO se toca. El
        /// disco se FUNDIENDO en 50 t al estallar (la estrella DESHACIÉNDOSE
        /// en luz y humo).
        /// Llamado desde el PreDraw tras el vuelco de la FASE MUNDO — con el
        /// lote CERRADO (este método gestiona SUS lotes, balanceados de a
        /// pares Begin→End; el catch de la casa cura cualquier media
        /// emergencia).
        /// </summary>
        private void DibujarDiscoSolDios()
        {
            // El acto (v6.50.60 — la misma máquina de la IA, por ESTADO:
            // la espolote de proximidad enciende la gigante cuando QUIERA
            // — el disco se derrite con SU compás, no con el reloj).
            bool estallo = _solExploto;
            float fadeSol = estallo
                ? Math.Max(0f, 1f - (_tickGigante - 30f) / 50f) : 1f;
            if (fadeSol <= 0f) return;   // el disco ya se deshizo en bruma

            float g = _solGigante
                ? MathHelper.Clamp(_tickGigante / 30f, 0f, 1f) : 0f;
            float ease = g * g * (3f - 2f * g);

            // Los shaders (el cacheo de la casa — una sola Request).
            if (!_solDiscoFallo && _solDiscoShader == null)
            {
                try
                {
                    _solDiscoShader = new Ref<Effect>(ModContent.Request<Effect>(
                        "AethonMod/Content/Effects/Shaders/SunShader",
                        AssetRequestMode.ImmediateLoad).Value);
                }
                catch { _solDiscoFallo = true; }
            }
            if (!_solAuraFallo && _solAuraShader == null)
            {
                try
                {
                    _solAuraShader = new Ref<Effect>(ModContent.Request<Effect>(
                        "AethonMod/Content/Effects/Shaders/RadialShineShader",
                        AssetRequestMode.ImmediateLoad).Value);
                }
                catch { _solAuraFallo = true; }
            }

            try
            {
                Vector2 drawPos = Projectile.Center - Main.screenPosition;
                // EL RADIO VISUAL (el del arma: width×scale×0.75 — aquí el
                // cuerpo del sol nace a 96 y la escala lo hace TODO).
                float starR = 96f * Math.Max(0.30f, Projectile.scale) * 0.75f;
                float lifeT = Math.Min(1f, _edad / 470f);

                // === 1. EL GLOW CORONAL (las dos capas del arma que crecen
                //     con la vida — en la explosión ya no: el flash manda) ===
                if (!estallo)
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    SunProjectile.DrawCoronalGlowSprites(drawPos, starR, lifeT, ease);
                    Main.spriteBatch.End();
                }

                // === 2. EL BACKGLOW (dos discos de resplandor — DORADO en
                //     el vuelo, BRASA en la gigante; escala proporcional al
                //     disco: starR/69 = el ratio del arma base) ===
                Texture2D bloomCircle = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Textures/ResplandorChico").Value;
                float escGlow = starR / 69f;
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                Color glowHot = Color.Lerp(new Color(255, 236, 170),
                    new Color(255, 75, 25), ease);
                glowHot.A = 0;
                Main.spriteBatch.Draw(bloomCircle, drawPos, null,
                    glowHot * (0.7f * fadeSol), 0f,
                    bloomCircle.Size() * 0.5f, escGlow * (0.95f + 0.35f * ease),
                    SpriteEffects.None, 0f);
                Color glowRed = Color.Lerp(new Color(255, 120, 40),
                    new Color(255, 30, 10), ease);
                glowRed.A = 0;
                Main.spriteBatch.Draw(bloomCircle, drawPos, null,
                    glowRed * (0.45f * fadeSol), 0f,
                    bloomCircle.Size() * 0.5f, escGlow * (1.61f + 0.5f * ease),
                    SpriteEffects.None, 0f);
                Main.spriteBatch.End();

                // === 3. EL AURA DE RUIDO (RadialShine — la nebulosa que
                //     RUGE alrededor, girando con el sol) ===
                Texture2D wavyBlotch = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Textures/RuidoManchas").Value;
                if (_solAuraShader != null && _solAuraShader.Value != null)
                {
                    Effect shineShader = _solAuraShader.Value;
                    shineShader.Parameters["globalTime"].SetValue(Main.GlobalTimeWrappedHourly);
                    Vector2 shineScale = Vector2.One * (starR * 2f) * 2.72f / wavyBlotch.Size();

                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                        SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    shineShader.CurrentTechnique.Passes[0].Apply();
                    Color shineColor = Color.Lerp(new Color(252, 226, 150),
                        new Color(255, 95, 45), ease);
                    Main.spriteBatch.Draw(wavyBlotch, drawPos, null,
                        shineColor * (0.24f * fadeSol), Projectile.rotation,
                        wavyBlotch.Size() * 0.5f, shineScale, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                }

                // === 4. EL DISCO (el SunShader — la SUPERFICIE DE PLASMA de
                //     verdad: BLANCO-DORADA que ENROJECE en la gigante) ===
                if (_solDiscoShader != null && _solDiscoShader.Value != null)
                {
                    Effect shader = _solDiscoShader.Value;
                    Texture2D psychedelicWing = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Textures/MapaDistorsion").Value;
                    Texture2D dendritic = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Textures/RuidoDendritico").Value;

                    shader.Parameters["coronaIntensityFactor"].SetValue(0.05f);
                    // LA PALETA DEL DIOS: blanco cálido + dorado profundo →
                    // brasa en la gigante (el arma en blanco puro/naranja).
                    shader.Parameters["mainColor"].SetValue(
                        Color.Lerp(new Color(255, 250, 235), new Color(255, 160, 120), ease).ToVector3());
                    shader.Parameters["darkerColor"].SetValue(
                        Color.Lerp(new Color(212, 150, 70), new Color(150, 28, 12), ease).ToVector3());
                    shader.Parameters["subtractiveAccentFactor"].SetValue(
                        new Color(181, 0, 0).ToVector3());
                    shader.Parameters["sphereSpinTime"].SetValue(
                        Main.GlobalTimeWrappedHourly * 0.9f);
                    shader.Parameters["globalTime"].SetValue(Main.GlobalTimeWrappedHourly);

                    Main.graphics.GraphicsDevice.Textures[1] = wavyBlotch;
                    Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                    Main.graphics.GraphicsDevice.Textures[2] = psychedelicWing;
                    Main.graphics.GraphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;

                    Vector2 drawScale = Vector2.One * (starR * 2f) / dendritic.Size();

                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                        SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                        null, Main.GameViewMatrix.TransformationMatrix);
                    shader.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(dendritic, drawPos, null,
                        Color.White * fadeSol, Projectile.rotation,
                        dendritic.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                }
            }
            catch
            {
                // la cura de la casa: si una capa murió a medias, se cierra
                // lo que quedó vivo (cero first-chance — la sonda).
                VFXCore.CerrarLoteSiAbierto();
            }
        }
    }
}
