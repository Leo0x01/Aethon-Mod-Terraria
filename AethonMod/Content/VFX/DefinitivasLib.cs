using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// DEFINITIVASLIB — v6.50.66 — LA LIBRERÍA DE LA BOLSA DE LAS ARMAS
    /// DEFINITIVAS («El Veredicto de Aethon»).
    ///
    /// Nace de LA INVESTIGACIÓN PROFUNDA que pidió el usuario: «investiga
    /// a todos los jefes y armas finales de los mods más populares y de los
    /// visualmente más hermosos… sus técnicas y texturas usadas». Las
    /// conclusiones aplicadas aquí (Task 2-a/2-b del worklog):
    ///
    ///   · EL RAYO DE FUSIÓN DE EL DIOS ANTIGUO (FusionRay.cs, leído en su fuente):
    ///     cono de 3500 px con ANCHO CRECIENTE (progress^0.8 · cbrt · K ·
    ///     (1.1 + cos(Time)·0.08)), BARRIDO SINUSOIDAL (cos(t/T·2π)·2.5 rad),
    ///     núcleo blanco, gradiente ANIMADO con offset por distancia, shake
    ///     constante y 15 partículas/frame a lo largo del cono.
    ///   · CALAMITY: trails primitivos de 60 vértices (aquí RayoStrip con
    ///     perfil gaussiano), glowmask por capa, sprites grandes y VIVOS,
    ///     «Yharim's Crystal» = 6 haces que convergen, «Dance of Light» =
    ///     pares de espadas que se buscan, «Galaxia» = 4 attunements.
    ///   · FARGO'S: el patrón FUSIÓN (todas las armas de una familia en
    ///     una final — aquí: EL TESTAMENTO, que rota las once sentencias).
    ///   · STARS ABOVE: «cada arma = UNA arquitectura única» (medidores,
    ///     combos, teletransportes) — cada definitiva tiene SU mecánica.
    ///   · THORIUM: premiar el golpe con proyectiles secundarios.
    ///
    /// LO QUE VIVE AQUÍ: la paleta del Juicio (el gradiente animado de la
    /// luz primordial), el buscador de presas, la colisión de CONO propia
    /// (la del FusionRay usa una extensión de Calamity que aquí no existe),
    /// el giro de ángulo con tope, los accesos null-safe a las texturas
    /// procedurales y el estado compartido de las dos armas con memoria
    /// (la constelación y el atavío del filo).
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
        /// LA COLISIÓN DE CONO — la receta del FusionRay del dios antiguo sin la
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
        /// EL ANCHO CRECIENTE DEL CONO — la fórmula EXACTA del FusionRay:
        /// progress^0.8 · cbrt(lerp(1.01, 0.7, progress)) · K ·
        /// (1.1 + cos(Time)·(0.08 − progress·0.06)). Nace estrecho en el
        /// ojo, engorda hacia el fondo y RESPIRA (la senoide de ±8%).
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

        private static Texture2D _star, _lumenBlade, _crescent, _vortex, _cometHead, _fireRing, _slash;

        /// <summary>LA ESTRELLA de 4 puntas (núcleo caliente) — las semillas del arma 8.</summary>
        public static Texture2D TexStar => Pedir(ref _star, "AethonMod/Content/Effects/Procedural/Star");

        /// <summary>LA HOJA DE LUZ — las hojas gemelas del arma 3 y los arcos del filo.</summary>
        public static Texture2D TexLumenBlade => Pedir(ref _lumenBlade, "AethonMod/Content/Effects/Procedural/LumenBlade");

        /// <summary>LA MEDIA LUNA — los tajos de la guadaña y las lunas del sistema.</summary>
        public static Texture2D TexCrescent => Pedir(ref _crescent, "AethonMod/Content/Effects/Procedural/Crescent");

        /// <summary>EL VÓRTICE — la espiral del devorador.</summary>
        public static Texture2D TexVortex => Pedir(ref _vortex, "AethonMod/Content/Effects/Procedural/Vortex");

        /// <summary>LA CABEZA DE COMETA — los cometas del sistema de Aethon.</summary>
        public static Texture2D TexCometHead => Pedir(ref _cometHead, "AethonMod/Content/Effects/Procedural/CometHead");

        /// <summary>EL ANILLO DE FUEGO — las detonaciones solares.</summary>
        public static Texture2D TexFireRing => Pedir(ref _fireRing, "AethonMod/Content/Effects/Procedural/FireRing");

        /// <summary>EL TAJO — los arcos de espada.</summary>
        public static Texture2D TexSlash => Pedir(ref _slash, "AethonMod/Content/Effects/Procedural/Slash");

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
        //  EL ESTADO COMPARTIDO (armas con memoria de sesión)
        // ==================================================================

        /// <summary>
        /// LA CONSTELACIÓN DEL JUICIO (arma 8): las estrellas sembradas
        /// viven aquí (la casa ya tiene precedentes de estado estático —
        /// el pozo de RayoSistema). Máx 7 estrellas; expira sola.
        /// </summary>
        public static class Constelacion
        {
            public const int Max = 7;
            /// <summary>Las posiciones (mundo) de las estrellas sembradas.</summary>
            public static readonly Vector2[] Estrellas = new Vector2[Max];
            /// <summary>El número de estrellas vivas (0..Max).</summary>
            public static int Contador;
            /// <summary>El tick de siembra de la última estrella (para expirar).</summary>
            public static int TickUltima;

            public static void Sembrar(Vector2 pos)
            {
                if (Contador >= Max) return;
                Estrellas[Contador++] = pos;
                TickUltima = (int)Main.GameUpdateCount;
            }

            public static void Reiniciar()
            {
                Contador = 0;
                TickUltima = (int)Main.GameUpdateCount;
            }
        }

        /// <summary>
        /// EL ATAVÍO DEL FILO CONSTELADO (arma 4): la constelación activa
        /// de cada jugador (0 = EL SOL, 1 = LA ESTRELLA POLAR,
        /// 2 = ANDRÓMEDA, 3 = ARIES). Memoria de sesión (la casa: armas de
        /// prueba, la bolsa las repone).
        /// </summary>
        public static byte[] AtavioFilo = new byte[Main.maxPlayers];

        /// <summary>EL ÍNDICE DEL TESTAMENTO (arma 12): qué sentencia toca.</summary>
        public static int SentenciaActual;
    }
}
