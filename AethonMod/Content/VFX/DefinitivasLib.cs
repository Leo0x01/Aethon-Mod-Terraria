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

        // ==================================================================
        //  v6.56.4 — HAZFUSIONESPINA, RETIRADA. La vía CPU de seis pasadas
        //  murió con 0 consumidores: la letra del usuario («solo debes
        //  adaptar el código del rayo concentrado 2 a las características
        //  de las armas con pico») mandó los conductos al CAMINO GPU del
        //  RC2 (el strip del proyectil + HazConcentradoShader, el borde
        //  fundido POR PÍXEL). Sus fórmulas viven en los llamadores
        //  (el perfil AnchoCono y el COLOR QUE VIAJA con el FADE DE
        //  LENTE, por nodo). La LECCIÓN completa de por qué la vía CPU
        //  no podía curar los picos, en RayoStrip.cs (la reversión
        //  educada de la v6.56.3) y en el códice §5.4. (v6.56.9: la
        //  aberración por-tira que vivía en HazConcentradoLib murió con
        //  la purga de la parte muerta de esa librería — auditoría §3.)
        // ==================================================================


        // ==================================================================
        //  v6.56.3 — EL ESTADO COMPARTIDO, RETIRADO: SentenciaActual solo lo
        //  avanzaba el Testamento (borrado por la letra del usuario) y nadie
        //  más lo leía — el dictador muerto no deja huérfanos. Si el tercer
        //  acto (HOJA-DE-RUTA Task 99) necesita sentencias compartidas, nace
        //  DERIVADA del ExtraAI ya sincronizado («computar, no sincronizar»).
        // ==================================================================
    }
}
