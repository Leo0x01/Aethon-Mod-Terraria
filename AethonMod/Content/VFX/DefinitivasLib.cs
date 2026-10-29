using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// DEFINITIVASLIB — v6.54.6 — LA LIBRERÍA DE LA BOLSA DE LAS ARMAS
    /// DEFINITIVAS, LA REFORMA («El Veredicto de Aethon»: LAS CINCO
    /// SENTENCIAS que eligió el usuario, cada una con sus SIETE CAPAS
    /// de efectos).
    ///
    /// Nace de LA INVESTIGACIÓN PROFUNDA que pidió el usuario: «investiga
    /// a todos los jefes y armas finales de los mods más populares y de los
    /// visualmente más hermosos… sus técnicas y texturas usadas». Las
    /// conclusiones aplicadas aquí (Task 2-a/2-b del worklog):
    ///
    ///   · EL RAYO DE FUSIÓN estudiado en su fuente (Task 2-b): cono de
    ///     3500 px con ANCHO CRECIENTE (progress^0.8 · cbrt · K ·
    ///     (1.1 + cos(Time)·0.08)), núcleo blanco, gradiente ANIMADO con
    ///     offset por distancia, los CUATRO HALOS APILADOS (14×·5×·4×·2×),
    ///     15 partículas/frame a lo largo del canal y shake constante —
    ///     ahora ES EL PRISMA PRIMORDIAL (el ataque definitivo).
    ///   · Los trails primitivos del género (aquí RayoStrip con perfil
    ///     gaussiano), los haces que convergen,
    ///     el partidor.
    ///   · El patrón de rotación de sentencias (retirado con el Testamento, v6.56.3).
    ///   · Las armas de arquitectura única.
    ///   · Premiar el golpe con proyectiles secundarios (las
    ///     ramas de la ira).
    ///
    /// LO QUE VIVE AQUÍ: la paleta del Juicio (el gradiente animado de la
    /// luz primordial), el buscador de presas, la colisión de CONO propia
    /// (la del haz de referencia usa una extensión que aquí no existe),
    /// el giro de ángulo con tope, los accesos null-safe a las texturas
    /// procedurales y el estado compartido de la corona (la sentencia
    /// que toca).
    /// </summary>
    public static class DefinitivasLib
    {
        // ==================================================================
        //  LA PALETA DEL JUICIO — el gradiente animado de la luz primordial
        // ==================================================================

        /// <summary>El ORO del alba — el color base del grimorio.</summary>
        public static readonly Color Oro = new(255, 214, 120);

        /// <summary>El ÁMBAR del fuego primordial.</summary>
        public static readonly Color Ambar = new(255, 158, 64);

        /// <summary>El BLANCO CÁLIDO del núcleo (nunca blanco puro salvo venas).</summary>
        public static readonly Color BlancoCalido = new(255, 248, 235);

        /// <summary>El VIOLETA de la tinta viva del libro.</summary>
        public static readonly Color Violeta = new(176, 118, 255);

        /// <summary>El CIAN del firmamento.</summary>
        public static readonly Color Cian = new(120, 220, 255);

        private static readonly Color[] _paradasJuicio =
        {
            new Color(255, 214, 120),   // oro
            new Color(255, 158, 64),    // ámbar
            new Color(255, 248, 235),   // blanco cálido
            new Color(176, 118, 255),   // violeta
            new Color(120, 220, 255),   // cian
        };

        // v6.56.3 — LAS PALETAS DE LA ESCALERA: la letra del usuario: «el
        // rayo del juicio, rayo del veredicto y rayo del alba son exactamente
        // iguales» — compartían renderizador Y paleta (el GradienteJuicio
        // hardcodeado). Cada PELDAÑO recupera SU alma: el Alba es la primera
        // luz del amanecer (oro pálido → durazno → blanco), el Veredicto es
        // la tarde que se vuelve noche (ámbar → magenta → violeta) y el
        // Juicio conserva el arcoíris completo de la casa.
        private static readonly Color[] _paradasAlba =
        {
            new Color(255, 236, 179),   // oro pálido
            new Color(255, 213, 150),   // durazno suave
            new Color(255, 248, 235),   // blanco cálido
            new Color(255, 222, 173),   // luz de miel
            new Color(255, 240, 200),   // crema del alba
        };

        private static readonly Color[] _paradasVeredicto =
        {
            new Color(255, 190, 92),    // ámbar de tarde
            new Color(255, 140, 96),    // brasa
            new Color(255, 244, 224),   // blanco cálido
            new Color(214, 96, 214),    // magenta
            new Color(150, 96, 240),    // violeta de noche
        };

        /// <summary>
        /// EL GRADIENTE DEL JUICIO: cicla suave oro → ámbar → blanco →
        /// violeta → cian → oro… (la receta del gradiente Oil de la referencia,
        /// con la identidad cromática del grimorio). <paramref name="t"/>
        /// en vueltas (1.0 = un ciclo completo).
        /// </summary>
        public static Color GradienteJuicio(float t)
        {
            t = t - MathF.Floor(t);
            float x = t * _paradasJuicio.Length;
            int i = (int)x;
            int j = (i + 1) % _paradasJuicio.Length;
            return Color.Lerp(_paradasJuicio[i], _paradasJuicio[j], x - i);
        }

        /// <summary>v6.56.3 — EL GRADIENTE DEL ALBA: la primera luz (oro
        /// pálido → durazno → blanco → miel → crema). EL ALMA DEL PELDAÑO 1.</summary>
        public static Color GradienteAlba(float t)
        {
            t = t - MathF.Floor(t);
            float x = t * _paradasAlba.Length;
            int i = (int)x;
            int j = (i + 1) % _paradasAlba.Length;
            return Color.Lerp(_paradasAlba[i], _paradasAlba[j], x - i);
        }

        /// <summary>v6.56.3 — EL GRADIENTE DEL VEREDICTO: la tarde que se
        /// vuelve noche (ámbar → brasa → blanco → magenta → violeta). EL
        /// ALMA DEL PELDAÑO 2.</summary>
        public static Color GradienteVeredicto(float t)
        {
            t = t - MathF.Floor(t);
            float x = t * _paradasVeredicto.Length;
            int i = (int)x;
            int j = (i + 1) % _paradasVeredicto.Length;
            return Color.Lerp(_paradasVeredicto[i], _paradasVeredicto[j], x - i);
        }

        /// <summary>v6.56.3 — EL GRADIENTE DE LA ESCALERA: la paleta del
        /// peldaño (1=Alba · 2=Veredicto · 3=Juicio).</summary>
        public static Color GradienteEscalera(int nivel, float t)
        {
            return nivel switch
            {
                1 => GradienteAlba(t),
                2 => GradienteVeredicto(t),
                _ => GradienteJuicio(t),
            };
        }

        /// <summary>La versión OSCURECIDA del gradiente (para halos).</summary>
        public static Color GradienteJuicioOscuro(float t, float f = 0.55f)
        {
            Color c = GradienteJuicio(t);
            return new Color((byte)(c.R * f), (byte)(c.G * f), (byte)(c.B * f));
        }

        /// <summary>El contrato de alfa de la casa (rgb premultiplicado).</summary>
        public static Color Alfa(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color((byte)(int)(c.R * f + 0.5f), (byte)(int)(c.G * f + 0.5f),
                (byte)(int)(c.B * f + 0.5f), (byte)(int)(255 * f + 0.5f));
        }

        /// <summary>La fracción de siempre (0..1).</summary>
        public static float Frac(float x) => x - MathF.Floor(x);

        // ==================================================================
        //  LOS HELPERS DE JUEGO
        // ==================================================================

        /// <summary>
        /// EL BUSCADOR DE PRESAS (el contrato de las fauces, ahora de las
        /// sentencias): jefes a CUALQUIER distancia; sin jefes, la chusma
        /// más cercana en 1800 px.
        /// </summary>
        public static NPC BuscarPresa(Vector2 desde, bool soloJefes)
        {
            NPC mejor = null;
            float mejorD = soloJefes ? float.MaxValue : 1800f * 1800f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.life <= 0 || n.dontTakeDamage) continue;
                if (n.friendly || n.townNPC) continue;
                if (soloJefes && !(n.boss || NPCID.Sets.ShouldBeCountedAsBoss[n.type])) continue;
                if (!soloJefes && (n.boss || NPCID.Sets.ShouldBeCountedAsBoss[n.type])) continue;
                float d = Vector2.DistanceSquared(n.Center, desde);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        /// <summary>
        /// LA COLISIÓN DE CONO — la receta del rayo de fusión sin
        /// extensiones ajenas: el punto del hitbox proyectado sobre el
        /// eje del cono debe caer dentro del largo, y su distancia
        /// perpendicular debe caber en el semiancho creciente
        /// (dist·tan(semiángulo) + el radio del propio hitbox).
        /// </summary>
        public static bool Cono(Rectangle hitbox, Vector2 origen, float largo, float angulo, float semiangulo)
        {
            Vector2 dir = new(MathF.Cos(angulo), MathF.Sin(angulo));
            Vector2 c = hitbox.Center();
            Vector2 v = c - origen;
            float along = v.X * dir.X + v.Y * dir.Y;
            float radioH = MathF.Max(hitbox.Width, hitbox.Height) * 0.5f;
            if (along < -radioH || along > largo + radioH) return false;
            float perp = MathF.Abs(v.X * -dir.Y + v.Y * dir.X);
            float semi = along > 0f ? along * MathF.Tan(semiangulo) : semiangulo * 60f;
            return perp <= semi + radioH;
        }

        /// <summary>
        /// EL GIRO CON TOPE (el AngleTowards del género): gira el ángulo
        /// actual hacia el objetivo SIN pasarse, envolviendo la diferencia
        /// a (−π, π].
        /// </summary>
        public static float AnguloHacia(float actual, float objetivo, float pasoMax)
        {
            float d = objetivo - actual;
            while (d > MathHelper.Pi) d -= MathHelper.TwoPi;
            while (d <= -MathHelper.Pi) d += MathHelper.TwoPi;
            return actual + MathHelper.Clamp(d, -pasoMax, pasoMax);
        }

        /// <summary>El Sonar blindado de la casa (jamás tumba el frame).</summary>
        public static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }

        /// <summary>DeGolpe de la casa (aceleración brusca de la animación).</summary>
        public static float DeGolpe(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            float u = 1f - t;
            return 1f - u * u * u * u;
        }

        /// <summary>
        /// EL ANCHO CRECIENTE DEL CONO — la fórmula EXACTA del rayo de
        /// fusión: progress^0.8 · cbrt(lerp(1.01, 0.7, progress)) · K ·
        /// (1.1 + cos(Time)·(0.08 − progress·0.06)). Nace estrecho en el
        /// origen, engorda hacia el fondo y RESPIRA (la senoide de ±8%).
        /// </summary>
        public static float AnchoCono(float progress, float time, float k)
        {
            float inicio = MathF.Pow(progress, 0.8f);
            float tope = MathF.Cbrt(MathHelper.Clamp(MathHelper.Lerp(1.01f, 0.7f, progress), 0f, 1.01f));
            return inicio * tope * k * (1.1f + MathF.Cos(time) * (0.08f - progress * 0.06f));
        }

        // ==================================================================
        //  LAS TEXTURAS PROCEDURALES (accesos null-safe, el patrón VFXCore)
        // ==================================================================

        private static Texture2D _star;

        /// <summary>LA ESTRELLA de 4 puntas (núcleo caliente) — las notas del réquiem, las runas y sellos del testamento, las pestañas del prisma.</summary>
        public static Texture2D TexStar => Pedir(ref _star, "AethonMod/Content/Effects/Procedural/Star");

        private static Texture2D Pedir(ref Texture2D campo, string ruta)
        {
            try
            {
                if (campo == null || campo.IsDisposed)
                    campo = ModContent.Request<Texture2D>(ruta, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                return campo;
            }
            catch { return null; }
        }

        // ==================================================================
        //  v6.54.7 — EL ACCESO CACHEADO A LAS TEXTURAS DE RUIDO + EL HAZ
        //  DE FUSIÓN TEXTURIZADO (LA PILA DE SEIS PASADAS).
        //
        //  LA LETRA: «siento que a las armas les faltan más capas de
        //  efectos visuales… el prisma primordial deja mucho que desear,
        //  no le llega ni a los pies a los ataques como el rayo de fusión
        //  o otros jefes con rayos destructores… vuelve a darle una
        //  revisada a las armas y una investigación a los rayos
        //  destructores de otros mods y aprende nuevas técnicas».
        //
        //  LA AUTOPSIA: las fundas de la .54.6 eran FILAMENTOS DE COLOR
        //  SÓLIDO (RayoStrip.FilamentoEnLote) — el perfil gaussiano sin
        //  textura. El rayo de fusión DE REFERENCIA multiplica CUATRO texturas
        //  en el shader (cuerpo fBm · segundo ruido · glow · bits de
        //  chispas, estudiado en su fuente): el «hervir» vive en ESA
        //  multiplicación, no en el color. LA CURA: la pila de seis
        //  pasadas texturizadas (RayoStrip.FajaTexturadaEnLote — el ruido
        //  AVANZA por las UVs a velocidades DISTINTAS por capa: dos ruidos
        //  cruzándose = la carne que hierve), más las técnicas NUEVAS de
        //  la investigación (Task 78-a): el flicker inconmensurable, la
        //  aberración cromática de bordes, los pinchos de difracción
        //  anamórficos y el fade de lente por nodo.
        // ==================================================================

        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> _cacheTex = new();

        /// <summary>Acceso cacheado y null-safe a CUALQUIER textura del mod por ruta (viva entre frames, muerta al dispose).</summary>
        public static Texture2D Tex(string ruta)
        {
            if (_cacheTex.TryGetValue(ruta, out Texture2D t) && t != null && !t.IsDisposed) return t;
            try { t = ModContent.Request<Texture2D>(ruta, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value; }
            catch { t = null; }
            _cacheTex[ruta] = t;
            return t;
        }

        // ================================================================
        //  v6.54.8 — LA CURA DE LOS PICOS (Task 79-a + la letra del
        //  usuario: «todo tiene esos picos... esa fea textura de picos
        //  no ayuda en nada... el rayo de fusión tiene varias capas,
        //  como distorsión, desenfoque, ruido perlin entre otras
        //  tecnicas»). La autopsia: RuidoTinta era «bandas de fBm en U»
        //  — GEOMETRÍA ANGULAR que al fluir por el haz se veía como
        //  PICOS APILADOS (el VLM lo confirmó sobre el collage); y
        //  RayoConcentrado_2 era una gaussiana casi sin detalle (0
        //  transiciones) — el «segundo ruido» de la referencia tiene 487
        //  colores y el 22 % saturado. LA CURA: las cuatro texturas de
        //  PLASMA de la receta del informe 79-a (fBm Perlin tileable con
        //  domain warping ±6px, envolvente suave en V que SIEMPRE manda
        //  sobre el ruido, blur direccional 0.5px, TODO EN GRISES — el
        //  color es 100 % runtime). Test anti-picos en tools/
        //  gen_plasma_v6548.py: peine=0, pendiente −1.5 — CERO picos
        //  por construcción.
        // ================================================================

        /// <summary>
        /// LA CARNE del haz — CarnePlasma (v6.54.8): plasma fBm orgánico
        /// con el núcleo saturado a lo largo de TODO el canal (la fila
        /// central de la referencia está a 0.81..1.00) y los tonos medios
        /// VIVIENDO dentro de la banda. Gris — el color va en runtime.
        /// </summary>
        public static Texture2D TexRuido => Tex("AethonMod/Content/Effects/Procedural/CarnePlasma");

        /// <summary>
        /// EL SEGUNDO RUIDO — SegundoRuido (v6.54.8): más binario que la
        /// carne (52 % negro + 22 % saturado, ~64 niveles), como la
        /// segunda capa del haz de referencia. Con flujo más rápido las dos capas
        /// se CRUZAN y el haz HIERVE (el producto tex0·tex1).
        /// </summary>
        public static Texture2D TexRemolino => Tex("AethonMod/Content/Effects/Procedural/SegundoRuido");

        /// <summary>
        /// LOS BITS — BitsChispa (v6.54.8): la máscara blanca de blobs
        /// duros de la referencia (uBits): ~16 blobs elongados 2-3:1 a lo
        /// largo, cobertura 12 %, bordes 2-4px. NUNCA se dibuja sola: se
        /// muestrea DOS veces con flujos distintos (el AND doble del
        /// shader) y se cruza con la carne — chispas dispersas.
        /// (v6.55.0 — la máscara regenerada: los blobs viven SOLO en el
        /// corazón V 0.32..0.68 — jamás asoman al borde de la tira.)
        /// </summary>
        public static Texture2D TexBits => Tex("AethonMod/Content/Effects/Procedural/BitsChispa");

        /// <summary>
        /// LA RAMPA — RampaGlow (v6.54.8): el perfil de calor transversal
        /// (la funda que envuelve el canal). La usa el shader del haz
        /// concentrado como cuarta textura multiplicada.
        /// </summary>
        public static Texture2D TexRampa => Tex("AethonMod/Content/Effects/Procedural/RampaGlow");

        /// <summary>
        /// EL FLICKER INCONMENSURABLE (técnica nº11 de la investigación):
        /// dos senos de frecuencias 8.7 Hz y 23.3 Hz que JAMÁS se repiten
        /// en fase — el haz vivo nunca late igual dos veces. Se multiplica
        /// sobre la respiración ±8 % de la fórmula del ancho.
        /// </summary>
        public static float Flicker(float t)
            => 1f + 0.05f * MathF.Sin(t * 8.7f * MathHelper.TwoPi) + 0.035f * MathF.Sin(t * 23.3f * MathHelper.Pi + 1.7f);

        /// <summary>
        /// v6.54.7 — EL HAZ DE FUSIÓN TEXTURIZADO: LA PILA DE SEIS PASADAS
        /// que reemplaza a las fundas de color sólido. Es el shader del
        /// rayo original resuelto en CPU, pasada a pasada, sobre la espina
        /// que el llamador traiga (recta, vertical o LA ESPINA VIVA verlet):
        ///
        ///   PASADA 1 — EL AURA: RuidoTinta a 2.4× con flujo lento — la
        ///   envoltura que respira y se deshace lejos del eje.
        ///   PASADA 2 — EL CUERPO: RuidoTinta a 1× con el COLOR QUE VIAJA
        ///   a lo largo (el ValueAt(p) de la referencia, por vértice) y EL
        ///   FADE DE LENTE (el centro del haz sobrevive más que los
        ///   extremos — el smoothstep del shader, horneado por nodo).
        ///   PASADA 3 — EL REMOLINO: el SEGUNDO ruido a 0.78× con periodo
        ///   FINO y flujo 1.9× — dos carnes cruzándose a velocidades
        ///   distintas: el HERVIR del producto tex0·tex1.
        ///   PASADA 4 — EL NÚCLEO EN BANDA: solo la vena central de la
        ///   textura (V 0.36..0.64) a 0.45× con flujo 2.6× — el canal
        ///   blanco-caliente que corre más rápido que su carne.
        ///   PASADA 5 — LOS BITS (v6.54.8): la máscara de chispas duras
        ///   muestreada DOS veces con flujos distintos (1.3× y 2.1× — el
        ///   AND doble del shader: bitsTex(t·3+x) × bitsTex(t·5+x)): dos
        ///   nubes de blobs cruzándose = chispas dispersas que NUNCA se
        ///   alinean (la regla de oro nº5: los elementos duros jamás se
        ///   dibujan solos ni periódicos).
        ///   PASADA 6 — LA VENA: el filamento blanco puro (el «return 1»
        ///   del umbral del shader original).
        ///   PASADA 7 — LA ABERRACIÓN CROMÁTICA: dos tiras finísimas
        ///   rojo/cian desplazadas en la perpendicular (técnica nº8: el
        ///   borde prismático = energía extrema).
        /// </summary>
        /// <param name="espina">El camino del haz en coordenadas de MUNDO.</param>
        /// <param name="perfil">EL SEMIANCHO por nodo en px (ya con toda la fórmula del llamador dentro).</param>
        /// <param name="color">El color base (el cuerpo viaja en arcoíris del Juicio; el aura lo usa fijo).</param>
        /// <param name="intensidad">La envolvente global (conv / vida / potencia).</param>
        /// <param name="tiempo">El reloj determinista de la casa.</param>
        /// <param name="semilla">Semilla por-proyectil (la vena tiembla distinta en cada haz).</param>
        /// <param name="arcoiris">True = el color VIAJA a lo largo (la firma de la referencia); false = color fijo.</param>
        /// <param name="flujo">Multiplicador del flujo (negativo = la carne hierve HACIA la raíz — las columnas hierven hacia arriba).</param>
        /// <param name="conAberracion">False para las columnas finas (las ramas no necesitan borde prismático).</param>
        public static void HazFusionEspina(Vector2[] espina, float[] perfil, Color color, float intensidad,
            float tiempo, int semilla, bool arcoiris = true, float flujo = 1f, bool conAberracion = true,
            Func<float, Color> gradiente = null)
        {
            if (espina == null || espina.Length < 2 || perfil == null || perfil.Length < espina.Length) return;
            if (intensidad <= 0.02f) return;
            int n = espina.Length;

            // LAS COORDENADAS DE PANTALLA (el contrato de RayoStrip).
            var pts = new Vector2[n];
            float maxPerfil = 0.001f;
            for (int i = 0; i < n; i++)
            {
                pts[i] = espina[i] - Main.screenPosition;
                if (perfil[i] > maxPerfil) maxPerfil = perfil[i];
            }

            // LOS SEMIANCHOS por capa (el aura 2.4× · el cuerpo 1× · el remolino 0.78× · el núcleo 0.45×).
            var semiA = new float[n]; var semiB = new float[n]; var semiC = new float[n]; var semiD = new float[n];
            for (int i = 0; i < n; i++)
            {
                semiA[i] = perfil[i] * 2.4f;
                semiB[i] = perfil[i];
                semiC[i] = perfil[i] * 0.78f;
                semiD[i] = perfil[i] * 0.45f;
            }

            // EL COLOR QUE VIAJA + EL FADE DE LENTE (por nodo — el smoothstep
            // del shader: el centro del haz brilla más que los extremos).
            // v6.56.3 — EL GRADIENTE ES PARÁMETRO: la escalera pasa SU paleta
            // (Alba/Veredicto/Juicio); null = el Juicio de toda la vida.
            Color Grad(float t) => gradiente != null ? gradiente(t) : GradienteJuicio(t);
            Color[] porNodo = null;
            if (arcoiris)
            {
                porNodo = new Color[n];
                for (int i = 0; i < n; i++)
                {
                    float q = i / (float)(n - 1);
                    float lente = 0.85f + 0.30f * MathF.Sin(q * MathHelper.Pi);
                    porNodo[i] = Alfa(Grad(tiempo * 0.07f - q * 0.16f), 0.55f * intensidad * lente);
                }
            }

            Texture2D carne = TexRuido;
            Texture2D remolino = TexRemolino;

            RayoStrip.AbrirLote();
            try
            {
                if (carne != null)
                {
                    // PASADA 1 — EL AURA (la envoltura lenta que respira).
                    RayoStrip.FajaTexturadaEnLote(pts, semiA, Alfa(color, 0.16f * intensidad),
                        carne, 560f, 0.5f * flujo, tiempo, 0f, 1f);
                    // PASADA 2 — EL CUERPO (el color que viaja + el fade de lente).
                    RayoStrip.FajaTexturadaEnLote(pts, semiB, porNodo, Alfa(color, 0.55f * intensidad),
                        carne, 420f, 1.0f * flujo, tiempo, 0f, 1f);
                }
                if (remolino != null)
                {
                    // PASADA 3 — EL REMOLINO (la carne que hierve: dos ruidos cruzados).
                    RayoStrip.FajaTexturadaEnLote(pts, semiC, Alfa(color, 0.42f * intensidad),
                        remolino, 240f, 1.9f * flujo, tiempo, 0f, 1f);
                }

                // PASADA 3.5 — LOS BITS (v6.54.8): el AND doble del shader —
                // la misma máscara con DOS flujos que se cruzan: donde los
                // blobs de ambas nubes coinciden, CHISPEA (y donde no, nada:
                // la cobertura del 12 % de cada una se cruza en ~1.5 % de
                // puntos brillantes — las chispas dispersas de la referencia).
                // v6.55.0 — LA CURA DE LOS PICOS (2ª ronda): la pasada vivía
                // a 1.15× el semiancho del CUERPO con la máscara llegando
                // hasta V=0.09 — los blobs duros ASOMABAN en la funda tenue
                // y el ojo los leía como PÚAS pegadas al borde (simulado:
                // zigzag cada ~17 px). Ahora la máscara nueva vive SOLO en
                // el corazón (V 0.32..0.68, gen_bits_v6550.py) y la pasada
                // se dibuja DENTRO del cuerpo (0.98× y V 0.08..0.92 — por
                // si una textura vieja anda suelta): las chispas son
                // TEXTURA del canal, jamás silueta.
                Texture2D bits = TexBits;
                // v6.56.3 — LA PUERTA DE LOS BITS: en haces angostos (perfil
                // medio ≤ 40 px) los blobs del doble-AND son púas puras a
                // 0.11-0.33 px/texel — no aportan nada a 46 px de ancho
                // (la autopsia de la 3ª ronda de picos). El Alba los suelta;
                // el Veredicto y el Juicio los conservan.
                float perfilMedio = 0f;
                for (int i = 0; i < n; i++) perfilMedio += perfil[i];
                perfilMedio /= n;
                if (bits != null && intensidad > 0.35f && perfilMedio > 40f)
                {
                    var semiBits = new float[n];
                    for (int i = 0; i < n; i++) semiBits[i] = perfil[i] * 0.98f;
                    Color cBits = arcoiris
                        ? Grad(tiempo * 0.07f - 0.30f)
                        : color;
                    RayoStrip.FajaTexturadaEnLote(pts, semiBits, Alfa(cBits, 0.30f * intensidad),
                        bits, 210f, 1.3f * flujo, tiempo, 0.08f, 0.92f);
                    RayoStrip.FajaTexturadaEnLote(pts, semiBits, Alfa(BlancoCalido, 0.22f * intensidad),
                        bits, 150f, 2.1f * flujo, tiempo + 3.7f, 0.08f, 0.92f);
                }

                if (carne != null)
                {
                    // PASADA 4 — EL NÚCLEO EN BANDA (la vena caliente de la textura).
                    RayoStrip.FajaTexturadaEnLote(pts, semiD, Alfa(BlancoCalido, 0.85f * intensidad),
                        carne, 380f, 2.6f * flujo, tiempo, 0.36f, 0.64f);
                }

                // PASADA 5 — LA VENA BLANCA (el umbral «return 1» del shader).
                var perfilUnit = new float[n];
                for (int i = 0; i < n; i++) perfilUnit[i] = perfil[i] / maxPerfil;
                RayoStrip.FilamentoEnLote(pts, maxPerfil * 0.32f, BlancoCalido, Color.White,
                    0.95f * intensidad, StormTaper.Flat, semilla, 0, false, false, true, null, perfilUnit);

                // PASADA 6 — LA ABERRACIÓN CROMÁTICA (el borde prismático).
                if (conAberracion && intensidad > 0.5f && carne != null)
                {
                    var ptsR = new Vector2[n]; var ptsC = new Vector2[n];
                    var semiE = new float[n];
                    for (int i = 0; i < n; i++)
                    {
                        Vector2 d = i < n - 1 ? pts[i + 1] - pts[i] : pts[i] - pts[i - 1];
                        Vector2 perp = new(-d.Y, d.X);
                        if (perp.LengthSquared() < 0.0001f) perp = Vector2.UnitY; else perp = Vector2.Normalize(perp);
                        float desplaz = 2f + perfil[i] * 0.10f;
                        ptsR[i] = pts[i] + perp * desplaz;
                        ptsC[i] = pts[i] - perp * desplaz;
                        semiE[i] = perfil[i] * 0.30f;
                    }
                    RayoStrip.FajaTexturadaEnLote(ptsR, semiE, Alfa(new Color(255, 70, 70), 0.30f * intensidad),
                        carne, 300f, 1.4f * flujo, tiempo, 0.40f, 0.60f);
                    RayoStrip.FajaTexturadaEnLote(ptsC, semiE, Alfa(new Color(70, 210, 255), 0.30f * intensidad),
                        carne, 300f, 1.4f * flujo, tiempo, 0.40f, 0.60f);
                }
            }
            finally { RayoStrip.CerrarLote(); }
        }

        /// <summary>
        /// v6.54.7 — LOS PINCHOS DE DIFRACCIÓN (técnica nº6 de la
        /// investigación): el destello anamórfico de lente ALINEADO al
        /// ángulo del haz — el largo ×8 del ancho en el eje, el corto
        /// perpendicular, respirando EN FASE con el conjunto. Va DENTRO de
        /// un VFXCore.Begin() abierto por el llamador.
        /// </summary>
        public static void PinchosDifraccion(Vector2 pos, float angulo, float largo, Color color, float alfa, float tiempo)
        {
            if (TexStar == null || alfa <= 0.02f) return;
            float respira = 0.9f + 0.1f * MathF.Sin(tiempo * 3f);
            VFXCore.Quad(pos, Alfa(color, alfa), new Vector2(largo, largo * 0.13f) * respira, angulo, TexStar);
            VFXCore.Quad(pos, Alfa(color, alfa * 0.65f), new Vector2(largo * 0.34f, largo * 0.34f) * respira,
                angulo + MathHelper.PiOver2, TexStar);
        }

        /// <summary>Dibuja una textura de la librería centrada en pantalla-coordenadas (blindado).</summary>
        public static void Blit(SpriteBatch batch, Texture2D tex, Vector2 posPantalla, Color color,
            Vector2 escalaPx, float rot = 0f)
        {
            if (tex == null || batch == null) return;
            // v6.54.5 — EL GUARD DEL LOTE MUERTO (la autopsia del client.log de
            // la .54.4): el ojo del arma 1 dibujaba aquí con el SpriteBatch ya
            // CERRADO por un Flush anterior — un Draw sobre un lote muerto que
            // envenenaba el render. Si la sonda de la casa está disponible y
            // dice que no hay Begin vivo, NO se dibuja (un adorno perdido jamás
            // tirará el juego; un End sin Begin sí lo hace).
            if (VFXCore.SondaActiva && !VFXCore.LoteAbierto) return;
            batch.Draw(tex, posPantalla, null, color, rot, tex.Size() * 0.5f,
                new Vector2(escalaPx.X / tex.Width, escalaPx.Y / tex.Height), SpriteEffects.None, 0f);
        }

        // ==================================================================
        //  v6.56.3 — EL ESTADO COMPARTIDO, RETIRADO: SentenciaActual solo lo
        //  avanzaba el Testamento (borrado por la letra del usuario) y nadie
        //  más lo leía — el dictador muerto no deja huérfanos. Si el tercer
        //  acto (HOJA-DE-RUTA Task 99) necesita sentencias compartidas, nace
        //  DERIVADA del ExtraAI ya sincronizado («computar, no sincronizar»).
        // ==================================================================
    }
}
