using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.ID;

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
        }

        private static readonly Queue<Eco> _cola = new Queue<Eco>();
        private static Eco _activo = null;

        /// <summary>Tope de la cola pendiente (la activa no cuenta).</summary>
        public const int ColaMax = 8;

        /// <summary>
        /// Encola una voz. El texto llega YA LOCALIZADO (hjson). Las
        /// líneas largas se parten aquí (una vez, fuera del render) en
        /// trozos de ~44 caracteres cortados en espacio.
        /// </summary>
        public static void Hablar(string texto, Color tinte, bool rugido = true)
        {
            try
            {
                if (string.IsNullOrEmpty(texto)) return;
                if (_cola.Count >= ColaMax) return; // desbordamiento: se descarta, sin crecer

                var eco = new Eco { Texto = PartirEnLineas(texto, 44), Tinte = tinte, Rugido = rugido };
                _cola.Enqueue(eco);
            }
            catch { }
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

                // === MÁQUINA DE ESCRIBIR (~2 caracteres/tick) ===
                string texto = eco.Texto;
                int visibles = eco.Edad * 2;
                if (visibles < texto.Length)
                    texto = texto.Substring(0, visibles);

                var font = Terraria.GameContent.FontAssets.DeathText.Value;
                Vector2 medidas = font.MeasureString(eco.Texto) * eco.Escala;

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

                Vector2 origen = font.MeasureString(eco.Texto) * 0.5f;

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
        }
    }
}
