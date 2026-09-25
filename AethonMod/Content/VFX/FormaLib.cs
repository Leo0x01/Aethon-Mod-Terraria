using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// FormaLib — v6.43 — LAS FORMAS DE LA SECCIÓN EFICAZ.
    ///
    /// Idea central: hasta ahora cada arma improvisaba su detección de
    /// contacto con AABBs cuadrados ("¿está el centro del enemigo a menos
    /// de N píxeles de la línea?") — una sección eficaz cuadrada que no
    /// se parece en nada a la forma del golpe. Esta librería unifica la
    /// detección contra FORMAS — segmento, banda alrededor de un segmento,
    /// cápsula, anillo, sector — con una API común: la mordida de una
    /// fisura, el frente de una onda expansiva o el abanico de un tajo
    /// preguntan por SU forma real, no por un rectángulo de compromiso.
    ///
    /// Contrato de la casa:
    ///   · PURA: no hay estado estático mutable salvo
    ///     <see cref="Depuracion"/> — cualquier proyectil la usa sin
    ///     inicializar nada y en cualquier orden.
    ///   · CERO GC: toda la consulta es matemática en pila; el único
    ///     delegado (el de <see cref="RecorrerNPCs"/>) lo aporta el
    ///     llamador — el bucle de la librería no aloca nada.
    ///   · La capa de GEOMETRÍA (Distancia/Punto) es pura Vector2, sin
    ///     ninguna dependencia de Terraria; la capa de ENTIDADES
    ///     (NPC/Jugador/Proyectil) la reutiliza. El filtro de BANDO —
    ///     a quién se puede golpear — sigue siendo
    ///     <see cref="VFXCore.EsObjetivo"/> (la puerta de la casa):
    ///     lo aplica el llamador, o <see cref="RecorrerNPCs"/> por él.
    ///   · Todas las posiciones son COORDENADAS DE MUNDO, las mismas
    ///     que usa el resto del mod.
    /// </summary>
    public static class FormaLib
    {
        // ==================================================================
        //  0. DEPURACIÓN — VER LA SECCIÓN EFICAZ
        // ==================================================================

        /// <summary>
        /// ¿Dibujar las formas que consultan los proyectiles? (false por
        /// defecto — es el ÚNICO estado mutable de la librería). Enciéndelo
        /// desde un llave de pruebas o una consola y cada golpe pintará su
        /// forma real: la mordida de la fisura, la banda de la traga, el
        /// frente de la onda... la sección eficaz hecha visible.
        /// </summary>
        public static bool Depuracion;

        /// <summary>Color de la banda/segmento en depuración (cian).</summary>
        private static readonly Color ColorDebugBanda = new(0, 255, 255, 200);

        /// <summary>Color de las guías (bordes de la banda) en depuración.</summary>
        private static readonly Color ColorDebugGuia = new(0, 255, 255, 80);

        /// <summary>Color del anillo en depuración (amarillo).</summary>
        private static readonly Color ColorDebugAnillo = new(255, 255, 0, 200);

        /// <summary>Color del sector en depuración (magenta).</summary>
        private static readonly Color ColorDebugSector = new(255, 80, 255, 200);

        // ==================================================================
        //  1. GEOMETRÍA PURA — SOLO Vector2, SIN DEPENDENCIAS
        // ==================================================================

        /// <summary>
        /// Debajo de esta longitud al cuadrado el segmento se trata como un
        /// PUNTO (caso degenerado: a y b prácticamente coinciden).
        /// </summary>
        private const float EpsilonDegenerado = 1E-05f;

        /// <summary>
        /// Distancia de un punto al SEGMENTO a→b (no a la recta infinita):
        /// proyección escalar del punto sobre el segmento,
        /// t = dot(p−a, b−a) / |b−a|², CLAMPEADA a [0, 1] — más allá de los
        /// extremos mide contra el extremo más cercano. Si el segmento es
        /// degenerado (|b−a| &lt; épsilon) devuelve la distancia a a.
        /// </summary>
        public static float DistanciaASegmento(Vector2 punto, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float largo2 = ab.LengthSquared();
            if (largo2 < EpsilonDegenerado)
                return Vector2.Distance(punto, a);

            float t = Vector2.Dot(punto - a, ab) / largo2;
            t = MathHelper.Clamp(t, 0f, 1f);
            return Vector2.Distance(punto, a + ab * t);
        }

        /// <summary>
        /// El punto del segmento a→b más cercano a <paramref name="punto"/>
        /// (la proyección clampeada a [0, 1]). Para knockbacks DIRECCIONALES:
        /// el empuje nace donde el golpe rozó de verdad, no en el centro de
        /// la forma.
        /// </summary>
        public static Vector2 PuntoMasCercanoEnSegmento(Vector2 punto, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float largo2 = ab.LengthSquared();
            if (largo2 < EpsilonDegenerado)
                return a;

            float t = Vector2.Dot(punto - a, ab) / largo2;
            t = MathHelper.Clamp(t, 0f, 1f);
            return a + ab * t;
        }

        /// <summary>
        /// ¿Está el punto en la BANDA de <paramref name="ancho"/> px (ANCHO
        /// TOTAL) alrededor del segmento a→b — la "calle" que recorre la
        /// línea? |distancia al segmento| ≤ ancho/2, con la distancia
        /// CLAMPEADA a los extremos: la calle corta donde corta el segmento
        /// (más allá de las puntas no hay banda).
        /// </summary>
        public static bool PuntoEnBanda(Vector2 punto, Vector2 a, Vector2 b, float ancho)
            => DistanciaASegmento(punto, a, b) <= ancho * 0.5f;

        /// <summary>
        /// ¿Está el punto en la CÁPSULA de radio <paramref name="radio"/>
        /// alrededor del segmento a→b? Mismo test que
        /// <see cref="PuntoEnBanda"/> con ancho = 2·radio — existen las dos
        /// porque el llamador piensa "banda de 64" o "cápsula de 32" según
        /// el arma; el convenio del parámetro es la única diferencia.
        /// </summary>
        public static bool PuntoEnCapsula(Vector2 punto, Vector2 a, Vector2 b, float radio)
            => DistanciaASegmento(punto, a, b) <= radio;

        /// <summary>
        /// ¿Está el punto en el ANILLO [radioInterno, radioExterno] alrededor
        /// del centro? dist ∈ [ri, re] — la forma de las ondas expansivas que
        /// golpean por el FRENTE (ni el cráter interior ni más allá del
        /// frente). Si ri &gt; re el anillo es vacío y devuelve false.
        /// </summary>
        public static bool PuntoEnAnillo(Vector2 punto, Vector2 centro,
            float radioInterno, float radioExterno)
        {
            float d = Vector2.Distance(punto, centro);
            return d >= radioInterno && d <= radioExterno;
        }

        /// <summary>
        /// Diferencia de ángulos a−b NORMALIZADA a [−π, π] (el camino corto:
        /// ángulos que dan la vuelta se pliegan). El helper público de los
        /// sectores — cualquier comparación de ángulos de la casa puede
        /// usarlo.
        /// </summary>
        public static float DeltaAngle(float a, float b)
        {
            float d = (a - b) % MathHelper.TwoPi;
            if (d > MathF.PI)
                d -= MathHelper.TwoPi;
            else if (d < -MathF.PI)
                d += MathHelper.TwoPi;
            return d;
        }

        /// <summary>
        /// ¿Está el punto en el SECTOR (arco/abanico) de vértice
        /// <paramref name="centro"/>? Radio: dist ≤
        /// <paramref name="radio"/>. Ángulo: |DeltaAngle(atan2(punto−centro),
        /// anguloCentro)| ≤ <paramref name="apertura"/>/2, con el wrap
        /// correcto de <see cref="DeltaAngle"/> (un abanico que cruza el
        /// ±π funciona igual que uno que no).
        /// </summary>
        public static bool PuntoEnSector(Vector2 punto, Vector2 centro, float radio,
            float anguloCentro, float apertura)
        {
            if (Vector2.DistanceSquared(punto, centro) > radio * radio)
                return false;
            return Math.Abs(DeltaAngle((punto - centro).ToRotation(), anguloCentro)) <= apertura * 0.5f;
        }

        /// <summary>
        /// ¿Cruza el segmento a→b el AABB <paramref name="rect"/>?
        /// Algoritmo: LIANG-BARSKY (recorte paramétrico por planos): el
        /// parámetro t ∈ [0, 1] del segmento se recorta contra los 4 planos
        /// del rectángulo; si queda un intervalo no vacío, hay cruce.
        /// Elegido sobre Cohen-Sutherland por ser TODO ramas y divisiones
        /// — sin tablas de códigos, sin intersecciones calculadas que no se
        /// usan, cero alocaiones. Tocar el borde CUENTA como cruce (los
        /// planos son inclusivos). Un segmento degenerado (punto) se
        /// resuelve con los mismos planos: dentro → true.
        /// </summary>
        public static bool SegmentoIntersecaAABB(Vector2 a, Vector2 b, Rectangle rect)
        {
            float t0 = 0f, t1 = 1f;
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;

            // Los 4 planos del AABB en pares (p, q) de Liang-Barsky:
            // x ≥ Left, x ≤ Right, y ≥ Top, y ≤ Bottom.
            if (!PlanoLiangBarsky(-dx, a.X - rect.Left, ref t0, ref t1)) return false;
            if (!PlanoLiangBarsky(dx, rect.Right - a.X, ref t0, ref t1)) return false;
            if (!PlanoLiangBarsky(-dy, a.Y - rect.Top, ref t0, ref t1)) return false;
            if (!PlanoLiangBarsky(dy, rect.Bottom - a.Y, ref t0, ref t1)) return false;

            return t0 <= t1;
        }

        /// <summary>
        /// El recorte de Liang-Barsky contra UN plano: (p, q) describe el
        /// semiespacio del AABB y ajusta el intervalo vivo [t0, t1] del
        /// segmento. p == 0 (paralelo al plano): fuera si q &lt; 0. Si no, la
        /// entrada/salida es r = q/p: p &lt; 0 entra (sube t0), p &gt; 0 sale
        /// (baja t1). False = el segmento quedó fuera.
        /// </summary>
        private static bool PlanoLiangBarsky(float p, float q, ref float t0, ref float t1)
        {
            if (p == 0f)
                return q >= 0f;

            float r = q / p;
            if (p < 0f)
            {
                if (r > t1) return false;
                if (r > t0) t0 = r;
            }
            else
            {
                if (r < t0) return false;
                if (r < t1) t1 = r;
            }
            return true;
        }

        // ==================================================================
        //  2. CONSULTAS CONTRA ENTIDADES — NPC / JUGADOR / PROYECTIL
        // ==================================================================

        /// <summary>
        /// El RADIO MEDIO de un NPC: (ancho + alto) / 4 — la media de sus
        /// dos semiejes. La aproximación documentada de la casa para las
        /// consultas de banda/anillo/sector: más honesta que "solo el
        /// ancho" para hitboxes alargadas, más barata que girar el AABB
        /// (eso lo hace <see cref="NPCEnSegmento"/>). 0 si el NPC es nulo.
        /// </summary>
        public static float RadioMedio(NPC npc)
            => npc == null ? 0f : (npc.width + npc.height) * 0.25f;

        /// <summary>
        /// ¿Toca el NPC el SEGMENTO a→b con grosor
        /// <paramref name="grosor"/> (ANCHO TOTAL de la línea)? La consulta
        /// fina de la casa contra el MOTOR: llama a
        /// Collision.CheckAABBvLineCollision(position, size, a, b, grosor,
        /// ref scratch) — la MISMA llamada del beam del fragmento (el
        /// parámetro ref es una variable float de rayado que el motor
        /// pide pasar; se declara local y ya). El motor trata la línea
        /// como franja de grosor px: primero rechaza por la envolvente del
        /// segmento inflada ±grosor/2, luego refina con el AABB del NPC
        /// GIRADO al marco de la línea — la sección eficaz de la FORMA,
        /// no de la caja.
        ///
        /// Antes del motor va el BROAD-PHASE de la librería: la misma
        /// envolvente (min/max de los extremos, inflada ±grosor/2) contra
        /// el AABB del NPC con 4 comparaciones — matemáticamente la misma
        /// puerta que abre el motor, así que no puede rechazar a nadie que
        /// el motor aceptaría, y cuesta cuatro comparaciones.
        ///
        /// NOTA: es GEOMETRÍA pura — no filtra bando. La puerta de daño
        /// (VFXCore.EsObjetivo) la aplica el llamador, como en toda la casa.
        /// </summary>
        public static bool NPCEnSegmento(NPC npc, Vector2 a, Vector2 b, float grosor)
        {
            if (npc == null || !npc.active)
                return false;

            // BROAD-PHASE: envolvente del segmento inflada ±grosor/2.
            float medio = grosor * 0.5f;
            Vector2 min = Vector2.Min(a, b) - new Vector2(medio, medio);
            Vector2 max = Vector2.Max(a, b) + new Vector2(medio, medio);
            if (npc.position.X > max.X || npc.position.Y > max.Y)
                return false;
            if (npc.position.X + npc.width < min.X || npc.position.Y + npc.height < min.Y)
                return false;

            // NARROW-PHASE: el chequeo de línea del motor (ref float scratch).
            float rayado = 0f;
            return Collision.CheckAABBvLineCollision(npc.position, npc.Size, a, b, grosor, ref rayado);
        }

        /// <summary>
        /// ¿Está el NPC en la BANDA de <paramref name="ancho"/> px (ancho
        /// total) alrededor del segmento a→b? Distancia del CENTRO del NPC
        /// al segmento ≤ ancho/2 + <see cref="RadioMedio"/> — la
        /// aproximación conservadora para hitboxes alargadas: el cuerpo
        /// entero cuenta, no solo el centro, sin pagar el giro del AABB.
        /// </summary>
        public static bool NPCEnBanda(NPC npc, Vector2 a, Vector2 b, float ancho)
        {
            if (npc == null || !npc.active)
                return false;
            return DistanciaASegmento(npc.Center, a, b) <= ancho * 0.5f + RadioMedio(npc);
        }

        /// <summary>
        /// ¿Está el NPC en el ANILLO [radioInterno, radioExterno] con su
        /// cuerpo? Distancia del centro del NPC ∈ [ri − radioMedio, re +
        /// radioMedio] — el FRENTE de la onda golpea a quien ASOMA al
        /// anillo, no solo a quien el centro dentro.
        /// </summary>
        public static bool NPCEnAnillo(NPC npc, Vector2 centro, float radioInterno, float radioExterno)
        {
            if (npc == null || !npc.active)
                return false;
            float d = Vector2.Distance(npc.Center, centro);
            return d >= radioInterno - RadioMedio(npc) && d <= radioExterno + RadioMedio(npc);
        }

        /// <summary>
        /// ¿Está el NPC en el SECTOR (abanico) con su cuerpo? Combina el
        /// radio (dist ≤ radio + radioMedio) con el ángulo
        /// (|DeltaAngle| ≤ apertura/2 sobre el centro→NPC) — la forma de
        /// los barridos con abanico.
        /// </summary>
        public static bool NPCEnSector(NPC npc, Vector2 centro, float radio,
            float anguloCentro, float apertura)
        {
            if (npc == null || !npc.active)
                return false;
            float alcance = radio + RadioMedio(npc);
            if (Vector2.DistanceSquared(npc.Center, centro) > alcance * alcance)
                return false;
            return Math.Abs(DeltaAngle((npc.Center - centro).ToRotation(), anguloCentro)) <= apertura * 0.5f;
        }

        /// <summary>
        /// ¿Está el JUGADOR en la banda de <paramref name="ancho"/> px
        /// alrededor del segmento? La gemela de <see cref="NPCEnBanda"/>
        /// para los jefes futuros: la media de los semiejes del jugador +
        /// la distancia del centro al segmento ≤ ancho/2.
        /// </summary>
        public static bool JugadorEnBanda(Player jugador, Vector2 a, Vector2 b, float ancho)
        {
            if (jugador == null || !jugador.active)
                return false;
            float radioMedio = (jugador.width + jugador.height) * 0.25f;
            return DistanciaASegmento(jugador.Center, a, b) <= ancho * 0.5f + radioMedio;
        }

        /// <summary>
        /// ¿Es este un proyectil HOSTIL comestible que cruza la banda? La
        /// regla de la casa para los "tragadores": pr.active &amp;&amp;
        /// pr.hostile &amp;&amp; pr.damage &gt; 0 — los telegraphs con
        /// daño 0 quedan fuera POR DISEÑO (devorar avisos no alimentaría a
        /// nadie) — más la geometría: el CENTRO del proyectil dentro de la
        /// banda de <paramref name="ancho"/> px alrededor del segmento.
        /// </summary>
        public static bool ProyectilHostilEnBanda(Projectile pr, Vector2 a, Vector2 b, float ancho)
        {
            if (pr == null || !pr.active || !pr.hostile || pr.damage <= 0)
                return false;
            return PuntoEnBanda(pr.Center, a, b, ancho);
        }

        /// <summary>
        /// Recorre los NPCs VIVOS y cercanos y llama a la acción por cada
        /// uno — el iterador de la casa con culling barato: patrón
        /// npc.active &amp;&amp; VFXCore.EsObjetivo (la puerta de bando ya
        /// aplicada) y distancia centro→npc.Center ≤ radioMax +
        /// npc.Size.Length()/2 (el cuerpo entero cuenta para el culling,
        /// no solo el centro). El bucle NO aloca: la delegación la aporta
        /// el llamador (pasa una lambda que capture lo suyo o un delegado
        /// cachado); las 200 vueltas son dos comparaciones para el muerto
        /// lejano.
        /// </summary>
        /// <param name="centro">Centro de la búsqueda (coords de mundo).</param>
        /// <param name="radioMax">Radio de culling en px.</param>
        /// <param name="accion">Qué hacer con cada NPC vivo y cercano.</param>
        public static void RecorrerNPCs(Vector2 centro, float radioMax, Action<NPC> accion)
        {
            if (accion == null)
                return;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc == null || !npc.active || !VFXCore.EsObjetivo(npc))
                    continue;

                float alcance = radioMax + npc.Size.Length() * 0.5f;
                if (Vector2.DistanceSquared(centro, npc.Center) > alcance * alcance)
                    continue;

                accion(npc);
            }
        }

        // ==================================================================
        //  3. DIBUJO DE DEPURACIÓN — LA SECCIÓN EFICAZ HECHA VISIBLE
        //     (solo cuando Depuracion; lote propio: End defensivo →
        //     Immediate+Additive → End en finally → reapertura estándar)
        // ==================================================================

        /// <summary>
        /// Dibuja la BANDA (o el segmento si ancho es 0): línea central
        /// gruesa + bordes a ±ancho/2 + tapas en los extremos (las tapas
        /// marcan el CLAMP: más allá de las puntas no hay golpe). Coordenadas
        /// de MUNDO. Solo dibuja si <see cref="Depuracion"/> está encendido;
        /// el dibujo de depuración JAMÁS rompe al llamador.
        /// </summary>
        public static void DepurarDibujar(Vector2 a, Vector2 b, float ancho, Color? color = null)
        {
            if (!Depuracion || Main.netMode == NetmodeID.Server || Main.gameMenu)
                return;

            bool habiaLote;
            try { habiaLote = AbrirLoteDebug(); }
            catch { return; }

            try
            {
                SpriteBatch sb = Main.spriteBatch;
                Vector2 pantalla = Main.screenPosition;
                Color nucleo = color ?? ColorDebugBanda;
                Color guia = new Color(nucleo.R, nucleo.G, nucleo.B, nucleo.A / 3);

                LineaDebug(sb, a - pantalla, b - pantalla, nucleo, 2f);

                if (ancho > 0.5f)
                {
                    Vector2 delta = b - a;
                    float largo = delta.Length();
                    Vector2 perp = new Vector2(-delta.Y, delta.X) / Math.Max(largo, 0.001f)
                        * (ancho * 0.5f);
                    LineaDebug(sb, a + perp - pantalla, b + perp - pantalla, guia, 1f);
                    LineaDebug(sb, a - perp - pantalla, b - perp - pantalla, guia, 1f);
                    // Las tapas: la banda corta en los extremos del segmento.
                    LineaDebug(sb, a + perp - pantalla, a - perp - pantalla, guia, 1f);
                    LineaDebug(sb, b + perp - pantalla, b - perp - pantalla, guia, 1f);
                }
            }
            catch { }
            finally { CerrarLoteDebug(habiaLote); }
        }

        /// <summary>
        /// Dibuja el ANILLO: las dos circunferencias (interna y externa) +
        /// marcas radiales cada 45° entre ambas. Coordenadas de MUNDO.
        /// Solo dibuja si <see cref="Depuracion"/> está encendido.
        /// </summary>
        public static void DepurarDibujar(Vector2 centro, float radioInterno,
            float radioExterno, Color? color = null)
        {
            if (!Depuracion || Main.netMode == NetmodeID.Server || Main.gameMenu)
                return;

            bool habiaLote;
            try { habiaLote = AbrirLoteDebug(); }
            catch { return; }

            try
            {
                SpriteBatch sb = Main.spriteBatch;
                Vector2 c = centro - Main.screenPosition;
                Color nucleo = color ?? ColorDebugAnillo;

                CirculoDebug(sb, c, radioInterno, nucleo);
                CirculoDebug(sb, c, radioExterno, nucleo);

                // Marcas radiales: el GROSOR del anillo, visible.
                Color guia = new Color(nucleo.R, nucleo.G, nucleo.B, nucleo.A / 2);
                for (int i = 0; i < 8; i++)
                {
                    float ang = i * MathHelper.PiOver4;
                    Vector2 dir = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang));
                    LineaDebug(sb, c + dir * radioInterno, c + dir * radioExterno, guia, 1f);
                }
            }
            catch { }
            finally { CerrarLoteDebug(habiaLote); }
        }

        /// <summary>
        /// Dibuja el SECTOR: los dos radios límite + el arco del frente a
        /// <paramref name="radio"/>. Coordenadas de MUNDO. Solo dibuja si
        /// <see cref="Depuracion"/> está encendido.
        /// </summary>
        public static void DepurarDibujar(Vector2 centro, float radio, float anguloCentro,
            float apertura, Color? color = null)
        {
            if (!Depuracion || Main.netMode == NetmodeID.Server || Main.gameMenu)
                return;

            bool habiaLote;
            try { habiaLote = AbrirLoteDebug(); }
            catch { return; }

            try
            {
                SpriteBatch sb = Main.spriteBatch;
                Vector2 c = centro - Main.screenPosition;
                Color nucleo = color ?? ColorDebugSector;
                Color guia = new Color(nucleo.R, nucleo.G, nucleo.B, nucleo.A / 2);

                float inicio = anguloCentro - apertura * 0.5f;

                Vector2 Punto(float ang)
                    => c + new Vector2((float)Math.Cos(ang) * radio, (float)Math.Sin(ang) * radio);

                // Los radios límite del abanico.
                LineaDebug(sb, c, Punto(inicio), guia, 1f);
                LineaDebug(sb, c, Punto(inicio + apertura), guia, 1f);

                // El arco del frente.
                const int Segmentos = 24;
                Vector2 anterior = Punto(inicio);
                for (int i = 1; i <= Segmentos; i++)
                {
                    Vector2 actual = Punto(inicio + apertura * (i / (float)Segmentos));
                    LineaDebug(sb, anterior, actual, nucleo, 1f);
                    anterior = actual;
                }
            }
            catch { }
            finally { CerrarLoteDebug(habiaLote); }
        }

        /// <summary>
        /// Un trazo fino del dibujo de depuración: el MagicPixel rotado y
        /// estirado de p a q (grosor en px). Cero Main.rand — trazos
        /// perfectamente deterministas.
        /// </summary>
        private static void LineaDebug(SpriteBatch sb, Vector2 p, Vector2 q, Color c, float grosor)
        {
            Vector2 delta = q - p;
            float largo = delta.Length();
            if (largo < 0.05f || c.A == 0)
                return;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            sb.Draw(pixel, p, null, c, delta.ToRotation(),
                new Vector2(0f, pixel.Height * 0.5f),
                new Vector2(largo / pixel.Width, grosor / pixel.Height),
                SpriteEffects.None, 0f);
        }

        /// <summary>Una circunferencia de depuración (polilínea cerrada).</summary>
        private static void CirculoDebug(SpriteBatch sb, Vector2 centro, float radio, Color c)
        {
            const int Segmentos = 40;
            Vector2 anterior = centro + new Vector2(radio, 0f);
            for (int i = 1; i <= Segmentos; i++)
            {
                float ang = i / (float)Segmentos * MathHelper.TwoPi;
                Vector2 actual = centro + new Vector2((float)Math.Cos(ang) * radio,
                    (float)Math.Sin(ang) * radio);
                LineaDebug(sb, anterior, actual, c, 1f);
                anterior = actual;
            }
        }

        /// <summary>
        /// Abre el lote de depuración de la casa: v6.50.11 — SONDA (el End
        /// solo si hay un Begin vivo — cero first-chance) y Begin
        /// Inmediato+Aditivo con la GameViewMatrix — el MISMO contrato que
        /// el resto de los lotes propios del mod.
        /// </summary>
        private static bool AbrirLoteDebug()
        {
            // v6.50.11 — el rastreo del lote ajeno por sonda (exacto).
            bool habiaLote = VFXCore.LoteAbierto;
            if (habiaLote) Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            return habiaLote;
        }

        /// <summary>
        /// Cierra el lote de depuración y devuelve el lote SIEMPRE ABIERTO
        /// y vanilla (v6.50.11 — el contrato de curación; antes solo
        /// reabría si había lote al llegar y devolvía el veneno si no lo
        /// había).
        /// </summary>
        private static void CerrarLoteDebug(bool habiaLote)
        {
            VFXCore.CerrarLoteSiAbierto();

            // v6.50.11 — curación incondicional (los parámetros vanilla
            // exactos; idempotente por sonda).
            VFXCore.ReabrirLoteVanilla();
        }
    }
}
