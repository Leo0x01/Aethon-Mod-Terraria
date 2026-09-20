using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// AudioLib — v6.34 — LA IDENTIDAD SONORA DE LA CASA: la casa suena —
    /// cada familia tiene SU voz y ningún coro de impactos apila el mismo
    /// golpe.
    ///
    /// Antes de esta librería cada proyectil improvisaba sus PlaySound a
    /// su aire: el mismo Item14 gritaba en treinta archivos con pitches
    /// contradictorios, y una sala llena de proyectiles apilaba el mismo
    /// golpe una vez por enemigo (el "coro"). AudioLib destila la
    /// práctica sonora de la casa en TRES reglas:
    ///
    ///   · LA FAMILIA ES LA VOZ: Solar brilla (+0.10), el Vacío traga
    ///     (−0.30), lo Eléctrico chispea (+0.40), lo Rúnico es puro
    ///     (0.00), lo Cósmico es hondo (−0.10) y el Desgarro raja
    ///     (−0.20). El mismo vocabulario vanilla, seis acentos.
    ///   · EL MOMENTO ES EL PAPEL: "apertura" (nace), "impacto"
    ///     (golpea), "carga" (acumula), "zona" (permanece) y "muerte"
    ///     (termina) — cada uno con su volumen base y su cadencia. Un
    ///     momento desconocido cae en "impacto": el default honesto.
    ///   · NADIE APILA: el mismo (familia, momento) NO suena más de una
    ///     vez cada 12 ticks (30 para zona y carga) — diez agujeros
    ///     negros tragando a la vez suenan como UN agujero negro grande.
    ///
    /// TODO el catálogo es vanilla 1.4 referenciado por SoundID clásico
    /// y TODO ya sonaba en la casa (son los mismos Item que staffs y
    /// proyectiles usan desde v6.2x): cero archivos de audio, cero
    /// rutas de textura frágiles.
    ///
    /// Contrato: pura decoración de cliente — el servidor es sordo y
    /// nada de esto decide daño; el jitter de pitch usa Main.rand pero
    /// jamás toca lógica. <see cref="SilenciarZona"/> reinicia la
    /// memoria al cambiar de arma.
    /// </summary>
    public static class AudioLib
    {
        // ==================================================================
        //  1 — LAS FAMILIAS Y LOS MOMENTOS
        // ==================================================================

        /// <summary>
        /// LAS SEIS VOCES de la casa. El pitch base de cada una es su
        /// carácter (ver <see cref="PitchDe"/>); el catálogo de sonidos
        /// de cada (familia, momento) vive en <see cref="Elegir"/>.
        /// </summary>
        public enum Familia
        {
            Solar,      // la forja brillante
            Vacia,      // el vacío que traga
            Electrica,  // la chispa
            Runico,     // la piedra que canta
            Cosmica,    // lo hondo
            Desgarro    // el filo que raja
        }

        // Los cinco momentos, como índices estables (la clave del
        // anti-spam se compone a mano con ellos — nada de GetHashCode
        // de strings, que cambia entre procesos).
        private const int APERTURA = 0;
        private const int IMPACTO = 1;
        private const int CARGA = 2;
        private const int ZONA = 3;
        private const int MUERTE = 4;

        // ==================================================================
        //  2 — EL ANTI-SPAM (nadie apila el mismo golpe)
        // ==================================================================

        // (clave → tick del último sonido). A lo sumo 6×5 = 30 claves
        // vivas; la limpieza perezosa de PuedeSonar lo mantiene plano.
        private static readonly Dictionary<int, int> _ultimoTick = new Dictionary<int, int>(32);

        /// <summary>
        /// v6.49 — LA HIGIENE (hallazgo AUD-C: las hermanas limpian sus
        /// estáticas; AudioLib no). El barrido perezoso de PuedeSonar
        /// mantiene el diccionario plano DENTRO de un mundo, pero entre
        /// mundos los ticks heredados frenaban los primeros sonidos.
        /// </summary>
        public static void Reiniciar()
        {
            _ultimoTick.Clear();
        }

        /// <summary>
        /// LA VOZ: hace sonar un momento de una familia. Este es el
        /// ÚNICO punto por el que el mod debería reproducir sonido de
        /// combate — aplica el pitch de la familia, el volumen base del
        /// momento, el jitter vivo y el anti-spam. Devuelve nada porque
        /// el sonido nunca decide nada: es puro perfume.
        /// </summary>
        /// <param name="familia">La voz que habla.</param>
        /// <param name="momento">"apertura", "impacto", "carga", "zona"
        /// o "muerte" (desconocido → impacto).</param>
        /// <param name="pos">Dónde nace el sonido.</param>
        /// <param name="volumen">Escala sobre el volumen base (1 = tal cual).</param>
        /// <param name="pitch">Desplazamiento extra sobre el pitch de la familia.</param>
        public static void Sonar(Familia familia, string momento, Vector2 pos, float volumen = 1f, float pitch = 0f)
        {
            // El servidor es sordo: sin sonido no hay sync que valga.
            if (Main.netMode == NetmodeID.Server)
                return;

            int m = Momento(momento);

            // EL ANTI-SPAM primero: si el coro ya cantó este golpe hace
            // poco, este miembro calla (y NO gasta su turno).
            if (!PuedeSonar(((int)familia * 8) + m, Enfriamiento(m)))
                return;

            // EL PITCH: la familia + el llamador + la variación viva
            // (±0.08) — salvo la carga, que sube ESTABLE para que el
            // oído sienta la tensión acumulándose sin traición.
            float p = PitchDe(familia) + pitch;
            if (m != CARGA)
                p += Main.rand.NextFloat(-0.08f, 0.08f);
            p = MathHelper.Clamp(p, -1f, 1f);

            // EL VOLUMEN: base del momento, escalado por el llamador.
            float v = MathHelper.Clamp(VolumenDe(m) * MathHelper.Max(volumen, 0f), 0f, 1f);

            try
            {
                Terraria.Audio.SoundEngine.PlaySound(
                    Elegir(familia, m).WithPitchOffset(p).WithVolumeScale(v), pos);
            }
            catch { }   // a prueba de balas, como toda la casa
        }

        /// <summary>
        /// REINICIA la memoria del anti-spam. Al cambiar de arma (o de
        /// escena) la voz nueva debe sonar YA — no hereda el silencio
        /// que la voz anterior dejó acumulado.
        /// </summary>
        public static void SilenciarZona()
        {
            _ultimoTick.Clear();
        }

        /// <summary>
        /// ¿Puede esta clave sonar ya? Registra el tick actual si sí.
        /// Limpieza perezosa: si el diccionario pasa de 64 entradas se
        /// vacía entero (no es crítico — se rellena en un suspiro).
        /// </summary>
        private static bool PuedeSonar(int clave, int enfriamiento)
        {
            int ahora = (int)Main.GameUpdateCount;

            if (_ultimoTick.Count > 64)
                _ultimoTick.Clear();

            if (_ultimoTick.TryGetValue(clave, out int ultimo) && ahora - ultimo < enfriamiento)
                return false;   // aún dentro del silencio: este golpe NO suena

            _ultimoTick[clave] = ahora;
            return true;
        }

        // ==================================================================
        //  3 — EL CATÁLOGO (familia × momento → sonido vanilla)
        // ==================================================================

        /// <summary>
        /// EL REPERTORIO: cada (familia, momento) mapea a un SoundStyle
        /// VANILLA que la casa ya usaba — por eso cada línea lleva su
        /// porqué: es pedigrí, no capricho.
        /// </summary>
        private static Terraria.Audio.SoundStyle Elegir(Familia f, int m) => (f, m) switch
        {
            // ===== SOLAR — la forja brillante =====
            (Familia.Solar, APERTURA) => SoundID.Item44,    // el encendido de los bastones vivos (cometa/púlsar/medusa)
            (Familia.Solar, IMPACTO)  => SoundID.Item45,    // EL yunque del sol: SunProjectile y los eclipses ya golpean así
            (Familia.Solar, CARGA)    => SoundID.Item4,     // la campanita de bolsas y subida de nivel: el poder que sube
            (Familia.Solar, ZONA)     => SoundID.Item27,    // el ping de alta tensión de la enana blanca: la corona vibrando
            (Familia.Solar, MUERTE)   => SoundID.Item62,    // el lamento de la estrella muerta (DeadStar): el sol apagándose

            // ===== VACIA — el vacío que traga =====
            (Familia.Vacia, APERTURA) => SoundID.Item20,    // el cast con el que TODOS los bastones de agujero negro abren
            (Familia.Vacia, IMPACTO)  => SoundID.Item14,    // la explosión del colapso: los agujeros de la casa mueren así
            (Familia.Vacia, CARGA)    => SoundID.Item88,    // LA succión: el sonido identitario de todo agujero del mod
            (Familia.Vacia, ZONA)     => SoundID.Item122,   // el zumbido grave continuo (tormenta/sinfonía): el área tragándose
            (Familia.Vacia, MUERTE)   => SoundID.Item63,    // el golpe seco del Rencor: el CLAC final de la singularidad

            // ===== ELECTRICA — la chispa =====
            (Familia.Electrica, APERTURA) => SoundID.Item93,    // el zumbido con el que prenden el enjambre y la tormenta
            (Familia.Electrica, IMPACTO)  => SoundID.Item12,    // EL TRUENO del golpe del rayo rúnico (su "zap" grave)
            (Familia.Electrica, CARGA)    => SoundID.Item77,    // la bobina subiendo: el magnetar carga con él (−0.35→+0.30)
            (Familia.Electrica, ZONA)     => SoundID.Item93,    // el patrón del enjambre: zumbido periódico a bajo volumen
            (Familia.Electrica, MUERTE)   => SoundID.Item94,    // el clímax del overclock del magnetar: la última descarga

            // ===== RUNICO — la piedra que canta =====
            (Familia.Runico, APERTURA) => SoundID.Item12,   // en casa YA es el cast rúnico (StormRune/LanzaAlba/Sinfonía)
            (Familia.Runico, IMPACTO)  => SoundID.Item70,   // el clang de PIEDRA de PulsoLib: losas rúnicas chocando
            (Familia.Runico, CARGA)    => SoundID.Item15,   // la voz que sube: el cuásar carga con ella (−0.35→+0.6)
            (Familia.Runico, ZONA)     => SoundID.Item113,  // la invocación del grimorio eterno: el círculo cantando sostenido
            (Familia.Runico, MUERTE)   => SoundID.Item37,   // el eco de la espada espectral (EchoBlade): la runa deshaciéndose

            // ===== COSMICA — lo hondo =====
            (Familia.Cosmica, APERTURA) => SoundID.Item8,       // el cast universal de la casa (35 staffs): aquí grave
            (Familia.Cosmica, IMPACTO)  => SoundID.NPCHit41,    // el golpe del titán hueco: EL impacto cósmico de la casa
            (Familia.Cosmica, CARGA)    => SoundID.Item117,     // el zumbido profundo de la Eminencia: la presencia acumulándose
            (Familia.Cosmica, ZONA)     => SoundID.Item21,      // la marea gravitatoria latiendo: la onda del área
            (Familia.Cosmica, MUERTE)   => SoundID.NPCDeath43,  // la muerte del titán hueco: cuando cae algo cósmico GORDO

            // ===== DESGARRO — el filo que raja =====
            (Familia.Desgarro, APERTURA) => SoundID.Item9,                   // EL sonido del rasgado: RealityTear abre con él
            (Familia.Desgarro, IMPACTO)  => SoundID.Item90,                  // el clang de ACERO de PulsoLib: el filo cortando
            (Familia.Desgarro, CARGA)    => SoundID.Item104,                 // el arcano de la Eminencia: la tensión antes de rajar
            (Familia.Desgarro, ZONA)     => SoundID.DD2_EtherianPortalOpen,  // un portal abierto: el desgarro ES un portal
            (Familia.Desgarro, MUERTE)   => SoundID.NPCDeath6,               // la muerte del guardián del rift: el rift cerrándose

            _ => SoundID.Item14    // (inalcanzable: m siempre es 0..4)
        };

        // ==================================================================
        //  4 — LA CALIBRACIÓN (pitch, volumen y cadencia por papel)
        // ==================================================================

        /// <summary>El CARÁCTER de cada voz: su pitch base.</summary>
        private static float PitchDe(Familia f) => f switch
        {
            Familia.Solar     => 0.10f,   // brillante
            Familia.Vacia     => -0.30f,  // grave
            Familia.Electrica => 0.40f,   // agudo
            Familia.Runico    => 0.00f,   // puro
            Familia.Cosmica   => -0.10f,  // hondo
            _                 => -0.20f   // el desgarro raja
        };

        /// <summary>El VOLUMEN base de cada papel (el llamador escala sobre esto).</summary>
        private static float VolumenDe(int m) => m switch
        {
            APERTURA => 0.90f,   // se anuncia, no grita
            CARGA    => 0.60f,   // la tensión es un rumor
            ZONA     => 0.45f,   // la zona es un latido de fondo
            _        => 1.00f    // impacto y muerte: a plena voz
        };

        /// <summary>
        /// LA CADENCIA del anti-spam: cuántos ticks de silencio se
        /// exige a cada papel. Apertura/impacto/muerte son eventos
        /// (12 ticks); zona y carga son estados (30) — un estado no es
        /// una metralleta.
        /// </summary>
        private static int Enfriamiento(int m) => m switch
        {
            ZONA  => 30,
            CARGA => 30,
            _     => 12
        };

        /// <summary>Traduce el nombre del momento a su índice (desconocido → impacto).</summary>
        private static int Momento(string momento)
        {
            switch (momento)
            {
                case "apertura": return APERTURA;
                case "carga": return CARGA;
                case "zona": return ZONA;
                case "muerte": return MUERTE;
                default: return IMPACTO;   // el default honesto
            }
        }
    }
}
