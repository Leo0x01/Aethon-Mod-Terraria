using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// EcoLib — LA LIBRERÍA DE LAS VOCES: diálogos dramáticos en pantalla,
    /// con máquina de escribir, pausa y fundido. La séptima hermana de las
    /// librerías de la casa (PantallaLib, MoldeLib, CompásLib, FormaLib,
    /// MediaResLib, CieloLib… ahora EcoLib).
    ///
    /// CONTRATO:
    /// - Hablar(texto YA localizado, tinte): encola la voz. Cola acotada
    ///   (ColaMax): un asalto de jefes no puede crecer sin control.
    /// - Update(): avanza la voz activa (un tick por llamada — la mueve
    ///   EcoSistema.UpdateUI, solo cliente).
    /// - Dibujar(sb): pinta la voz activa (la capa la inserta EcoSistema
    ///   tras "Vanilla: Death Text" — el mismo estado de spriteBatch que
    ///   usa el texto de muerte de vanilla; EcoLib NUNCA abre lotes
    ///   propios).
    /// - Reiniciar(): OnWorldUnload/Unload — cero estática huérfana.
    ///
    /// v6.47 — LOS SUSURROS: Hablar acepta ESCALA (0.52 = la voz menuda
    /// del hambre del grimorio — el mismo tipo dramático, tamaño de
    /// secreto) y rugido opcional ya existía: la Voz del Hambre entra por
    /// aquí SIN rugir, susurrando de verdad.
    ///
    /// v6.48 — LA COLA CON PRIORIDAD Y LAS VARIANTES:
    /// - Hablar(..., prioridad: true): LA VOZ DEL LIBRO SIEMPRE VA
    ///   PRIMERO — se cuela al FRENTE de la cola (la voz activa termina
    ///   su línea y la siguiente en nacer es la del libro; las demás
    ///   esperan detrás: "la otra voz aparece después de la primera",
    ///   exactamente como pidió el usuario). Así la Furia nunca compite
    ///   con las voces de los jefes del propio evento.
    /// - ElegirVariante(clave, n): el REPARTO sin repetición — cada
    ///   situación tiene VARIAS muestras y esta elige una distinta de la
    ///   última dicha para esa clave (matar al Rey Gelatina veinte veces
    ///   no puede escuchar siempre la misma línea).
    ///
    /// v6.49 — LA VOZ CAMINA EN RED (con EcoRed):
    /// - ElegirClave(clave, n): el REPARTO sin repetición devolviendo la
    ///   CLAVE COMPLETA ("claveN") — EcoRed la empaqueta tal cual y el
    ///   portador la resuelve en SU idioma: la variante la elige la
    ///   AUTORIDAD (SP o servidor de MP) y todos los portadores ven la
    ///   misma línea que la autoridad repartió.
    ///
    /// REGLAS DE LA CASA:
    /// - Render 100% DETERMINISTA: cero Main.rand — la animación es pura
    ///   función de la edad del eco (tipeo, pop, deriva y fundido).
    /// - El texto largo se PARTE EN LÍNEAS al encolar (una sola vez, fuera
    ///   del render) y el ancho se AUTOAJUSTA al 75% de la pantalla: la
    ///   voz nunca se sale del marco.
    /// - Fuente: FontAssets.DeathText — la tipografía dramática del
    ///   "Has muerto…" de vanilla, la voz correcta para un libro que
    ///   devora dioses.
    /// </summary>
    public static class EcoLib
    {
        /// <summary>Una voz en pantalla.</summary>
        private class Eco
        {
            public string Texto = "";
            public Color Tinte = Color.White;
            public float Escala = 0.62f;
            public int TicksTipeo = 46;    // revelado (~2 caracteres/tick)
            public int TicksMuestra = 150; // texto completo quieto
            public int TicksFundido = 42;  // desvanecer
            public bool Rugido = true;     // sonido grave al nacer
            public int Edad = 0;

            // v6.49 — EL CACHE DE LA MEDIDA (auditoría AUD-C): medir el
            // texto y el origen UNA vez al encolar; el render jamás
            // vuelve a llamar MeasureString (era 2× por frame) ni recorta
            // la máquina de escribir cada tick (solo cuando cambia el
            // número de caracteres visibles).
            public Vector2 Medida = Vector2.Zero;
            public Vector2 Origen = Vector2.Zero;
            public int UltimosVisibles = -1;
            public string TextoTipeado = null;
        }

        private static readonly Queue<Eco> _cola = new Queue<Eco>();
        private static Eco _activo = null;

        /// <summary>La memoria del reparto: la última variante dicha por clave.</summary>
        private static readonly Dictionary<string, int> _ultimaVariante = new Dictionary<string, int>();

        /// <summary>Tope de la cola pendiente (la activa no cuenta).</summary>
        public const int ColaMax = 8;

        /// <summary>
        /// Encola una voz. El texto llega YA LOCALIZADO (hjson). Las
        /// líneas largas se parten aquí (una vez, fuera del render) en
        /// trozos de ~44 caracteres cortados en espacio.
        /// v6.47: escala opcional (los SUSURROS usan ~0.52).
        /// v6.48: prioridad opcional — la voz del LIBRO se cuela AL FRENTE
        /// de la cola (no corta la voz activa: la línea en curso termina
        /// y la del libro nace justo después; las demás esperan detrás).
        /// </summary>
        public static void Hablar(string texto, Color tinte, bool rugido = true,
            float escala = 0.62f, bool prioridad = false)
        {
            try
            {
                if (string.IsNullOrEmpty(texto)) return;
                if (_cola.Count >= ColaMax) return; // desbordamiento: se descarta, sin crecer

                var eco = new Eco
                {
                    Texto = PartirEnLineas(texto, 44),
                    Tinte = tinte,
                    Rugido = rugido,
                    Escala = MathHelper.Clamp(escala, 0.3f, 1.2f),
                };
                PrepararCache(eco);
                if (prioridad && _cola.Count > 0)
                {
                    // EL CULÓN DE LA COLA: la voz prioritaria pasa delante de
                    // TODAS las pendientes (las prioritarias múltiples se
                    // mantienen en su orden de llegada entre ellas).
                    var pendientes = _cola.ToArray();
                    _cola.Clear();
                    _cola.Enqueue(eco);
                    for (int i = 0; i < pendientes.Length; i++)
                        _cola.Enqueue(pendientes[i]);
                }
                else
                {
                    _cola.Enqueue(eco);
                }
            }
            catch { }
        }

        /// <summary>
        /// v6.49 — LA PREPARACIÓN (una vez, fuera del render): mide el
        /// texto completo, fija el origen y CALIBRA la máquina de
        /// escribir por longitud (los textos largos tipean más rápido:
        /// el "quieto" y el fundido ya no se pisan con líneas de 300
        /// caracteres — hallazgo AUD-C).
        /// </summary>
        private static void PrepararCache(Eco eco)
        {
            try
            {
                var font = Terraria.GameContent.FontAssets.DeathText.Value;
                // Medida VISIBLE (con escala — para el autoajuste de ancho)…
                eco.Medida = font.MeasureString(eco.Texto) * eco.Escala;
                // …y el ORIGEN en espacio de textura SIN escala (DrawString
                // aplica la escala sobre el origen: pre-escalarlo descentraría).
                eco.Origen = font.MeasureString(eco.Texto) * 0.5f;
                // ~2 caracteres/tick con piso y techo (los susurros cortos
                // respiran; las crónicas largas no eternizan el tipeo).
                eco.TicksTipeo = (int)MathHelper.Clamp(eco.Texto.Length * 0.5f, 22f, 90f);
            }
            catch { eco.Medida = Vector2.Zero; eco.Origen = Vector2.Zero; }
        }

        /// <summary>
        /// v6.48 — EL REPARTO SIN REPETICIÓN: elige una de las variantes
        /// "clave1..claveN" DISTINTA de la última dicha para esa situación
        /// (aleatorio lógico — corre en la lógica del juego, nunca en el
        /// render). Devuelve el TEXTO YA LOCALIZADO (y "" si la clave no
        /// existe — el llamador decide si callar).
        /// </summary>
        public static string ElegirVariante(string clave, int variantes)
        {
            string completa = ElegirClave(clave, variantes);
            if (string.IsNullOrEmpty(completa)) return "";
            return Language.GetTextValue(completa);
        }

        /// <summary>
        /// v6.49 — EL REPARTO QUE VIAJA: lo mismo que ElegirVariante pero
        /// devuelve la CLAVE COMPLETA ("…Celos.Melee2") en vez del texto.
        /// La AUTORIDAD la llama (SP o servidor MP); EcoRed empaqueta la
        /// clave y el portador la resuelve en su idioma — la variante se
        /// reparte UNA vez para todos, la memoria anti-repetición vive
        /// con la autoridad.
        /// </summary>
        public static string ElegirClave(string clave, int variantes)
        {
            try
            {
                if (variantes <= 1) return clave + "1";
                int ultima = -1;
                _ultimaVariante.TryGetValue(clave, out ultima);

                int idx;
                if (variantes == 2) idx = ultima == 1 ? 2 : 1;
                else
                {
                    // Sorteo con rechazo de la repetida (n>2: 1..n menos la última)
                    do { idx = 1 + Main.rand.Next(variantes); }
                    while (idx == ultima);
                }
                _ultimaVariante[clave] = idx;
                return clave + idx;
            }
            catch { return clave + "1"; }
        }

        /// <summary>
        /// Parte el texto en líneas de máx 'largo' caracteres cortando en
        /// espacios (sin partir palabras). Determinista, corre UNA vez al
        /// encolar — el render nunca trabaja sobre texto cambiante.
        /// </summary>
        private static string PartirEnLineas(string texto, int largo)
        {
            if (string.IsNullOrEmpty(texto) || texto.Length <= largo) return texto;
            var lineas = new List<string>();
            string resto = texto;
            while (resto.Length > largo)
            {
                int corte = -1;
                for (int i = largo; i > 8; i--) // buscar espacio hacia atrás
                {
                    if (resto[i] == ' ') { corte = i; break; }
                }
                if (corte <= 0) corte = largo; // sin espacio: corte duro
                lineas.Add(resto.Substring(0, corte).TrimEnd());
                resto = resto.Substring(corte).TrimStart();
            }
            if (resto.Length > 0) lineas.Add(resto);
            return string.Join("\n", lineas);
        }

        /// <summary>
        /// Avanza la voz activa un tick (la llama EcoSistema.UpdateUI,
        /// solo cliente). Al nacer una voz suena el rugido.
        /// </summary>
        public static void Update()
        {
            try
            {
                if (_activo == null)
                {
                    if (_cola.Count == 0) return;
                    _activo = _cola.Dequeue();
                    _activo.Edad = 0;
                    if (_activo.Rugido)
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar);
                }
                _activo.Edad++;
                if (_activo.Edad >= _activo.TicksTipeo + _activo.TicksMuestra + _activo.TicksFundido)
                    _activo = null; // fin: la siguiente voz nace el próximo tick
            }
            catch { _activo = null; }
        }

        /// <summary>¿hay voz activa o pendiente? (diagnóstico).</summary>
        public static bool Hablando => _activo != null || _cola.Count > 0;

        /// <summary>v6.49 — cuántas voces viven (1 activa + cola) — lo pinta el overlay F8.</summary>
        public static int ColasPendientes => (_activo != null ? 1 : 0) + _cola.Count;

        /// <summary>
        /// Pinta la voz activa. DETERMINISTA: tipeo por edad, pop de
        /// nacimiento (1.14 → 1.0 en 10 ticks), deriva lenta hacia arriba
        /// (como el texto de muerte de vanilla) y fundido final. La caja
        /// se mide con el texto COMPLETO para que no salte mientras tipea.
        /// El ancho se autoajusta al 75% de la pantalla (piso de escala
        /// 0.3): la voz nunca desborda.
        /// </summary>
        public static void Dibujar(SpriteBatch sb)
        {
            try
            {
                Eco eco = _activo;
                if (eco == null) return;

                int total = eco.TicksTipeo + eco.TicksMuestra + eco.TicksFundido;
                if (eco.Edad >= total) return;

                // === ALFA por fases (tipeo → muestra → fundido) ===
                float alpha = 1f;
                if (eco.Edad > eco.TicksTipeo + eco.TicksMuestra)
                    alpha = 1f - (float)(eco.Edad - eco.TicksTipeo - eco.TicksMuestra) / eco.TicksFundido;
                // nacimiento: primeros 6 ticks
                float nacer = eco.Edad >= 6 ? 1f : eco.Edad / 6f;
                alpha = Microsoft.Xna.Framework.MathHelper.Clamp(alpha * nacer, 0f, 1f);
                if (alpha <= 0f) return;

                // === MÁQUINA DE ESCRIBIR (~2 caracteres/tick) — v6.49: el
                // recorte SOLO cuando cambia el número de visibles (cero
                // strings basura por frame; hallazgo AUD-C) ===
                int visibles = eco.Edad * 2;
                if (visibles > eco.Texto.Length) visibles = eco.Texto.Length;
                if (visibles != eco.UltimosVisibles)
                {
                    eco.UltimosVisibles = visibles;
                    eco.TextoTipeado = visibles >= eco.Texto.Length
                        ? eco.Texto : eco.Texto.Substring(0, visibles);
                }
                string texto = eco.TextoTipeado ?? eco.Texto;

                var font = Terraria.GameContent.FontAssets.DeathText.Value;
                // v6.49 — LA MEDIDA CACHEADA (una vez al encolar): el render
                // nunca vuelve a medir (era 2× por frame).
                Vector2 medidas = eco.Medida;

                // === AUTOAJUSTE de ancho (75% de pantalla, piso 0.3) ===
                float escala = eco.Escala;
                float anchoMax = Main.screenWidth * 0.75f;
                if (medidas.X > anchoMax && medidas.X > 0f)
                {
                    float ajuste = anchoMax / medidas.X;
                    escala = Microsoft.Xna.Framework.MathHelper.Max(escala * ajuste, 0.3f);
                    medidas *= ajuste;
                }

                // === POSICIÓN: centrado, 24% de altura, deriva lenta arriba ===
                Vector2 centro = new Vector2(
                    Main.screenWidth * 0.5f,
                    Main.screenHeight * 0.24f - eco.Edad * 0.12f);

                // === POP de nacimiento: escala 1.14 → 1.0 en 10 ticks ===
                float pop = 1f + 0.14f * (1f - Microsoft.Xna.Framework.MathHelper.Clamp(eco.Edad / 10f, 0f, 1f));
                float escalaFinal = escala * pop;

                Vector2 origen = eco.Origen;

                // Sombra (doble de vanilla: desplazada y negra) + voz
                sb.DrawString(font, texto, centro + new Vector2(3f, 3f),
                    Color.Black * (alpha * 0.75f), 0f, origen, escalaFinal, SpriteEffects.None, 0f);
                sb.DrawString(font, texto, centro,
                    eco.Tinte * alpha, 0f, origen, escalaFinal, SpriteEffects.None, 0f);
            }
            catch { }
        }

        /// <summary>OnWorldUnload/Unload: la cola y la voz activa mueren con el mundo.</summary>
        public static void Reiniciar()
        {
            _cola.Clear();
            _activo = null;
            _ultimaVariante.Clear();
        }
    }
}
