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
    ///   · CALAMITY: trails primitivos (aquí RayoStrip con perfil
    ///     gaussiano), «Yharim's Crystal» = haces que convergen,
    ///     «Universe Splitter» = el partidor.
    ///   · FARGO'S: el patrón FUSIÓN — EL TESTAMENTO rota las sentencias.
    ///   · STARS ABOVE: «cada arma = UNA arquitectura única».
    ///   · THORIUM: premiar el golpe con proyectiles secundarios (las
    ///     ramas de la ira).
    ///
    /// LO QUE VIVE AQUÍ: la paleta del Juicio (el gradiente animado de la
    /// luz primordial), el buscador de presas, la colisión de CONO propia
    /// (la del FusionRay usa una extensión de Calamity que aquí no existe),
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

        /// <summary>
        /// EL GRADIENTE DEL JUICIO: cicla suave oro → ámbar → blanco →
        /// violeta → cian → oro… (la receta del gradiente Oil de el dios antiguo,
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
        /// LA COLISIÓN DE CONO — la receta del rayo de fusión sin la
        /// extensión de Calamity: el punto del hitbox proyectado sobre el
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
        /// EL GIRO CON TOPE (el AngleTowards de Calamity): gira el ángulo
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
        //  EL ESTADO COMPARTIDO
        // ==================================================================

        /// <summary>EL ÍNDICE DEL TESTAMENTO (la corona): qué sentencia toca (0..3).</summary>
        public static int SentenciaActual;
    }
}
