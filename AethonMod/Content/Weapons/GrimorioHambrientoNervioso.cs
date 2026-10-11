using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.Systems;
using AethonMod.Content.Players;
using AethonMod.Content.Globals;
using AethonMod.Content.Projectiles.Nervioso;

namespace AethonMod.Content.Weapons
{
    /// <summary>
    /// v6.50.90 — EL TERCER ASIENTO CAMBIA DE DUEÑO: EL GRIMORIO NERVIOSO.
    /// La letra del usuario: «el tercer libro borralo, y crea otro con las
    /// mismas caracteristicas, temblor, glisheado, y nervioso». El
    /// Tembloroso (v6.50.87) quedó BORRADO — su escalofrío se hereda aquí,
    /// montado sobre el glitch del Errático y una capa nueva de NERVIOSISMO:
    ///
    /// · EL TEMBLOR del Tembloroso — la receta .87 INTACTA: los dos senos
    ///   incommensurables por eje (9,3/17,3 Hz en X, 11,9/19,1 Hz en Y,
    ///   fases 1,7/0,6). El escalofrío continuo del difunto.
    /// · EL GLITCH del Errático — la receta .88 INTACTA (el volumen que el
    ///   usuario aprobó: tiras 2-4 desfasándose hasta ±6 px, ráfagas de
    ///   12-24 ticks que se rehacen cada 2 frames, cada 15 s → 2,1 s con el
    ///   hambre) con máquina de estado PROPIA: el Nervioso rompe a SU hora
    ///   (v6.50.92: la familia es DOS libros — el Errático y el Inestable
    ///   quedaron BORRADOS, cada uno con su máquina).
    /// · EL NERVIOSO, en DOS capas: EL OJO ANSIOSO (revisa cada 2,4 s →
    ///   0,42 s, miradas que SALTAN a la meta, 1 de cada 3 al CENTRO y
    ///   dardos laterales) y EL SOBRESALTO (cada 6,5 s → 1,8 s, brinco de
    ///   1,2 → 2,2 px que se asienta en 7 ticks y, con hambre, ROMPE el
    ///   libro en una ráfaga corta).
    ///
    /// v6.50.91 — NO ES MIEDO (la letra del usuario: «no es que este
    /// nervioso por tener miedo, esta mas bien intranquilo, hambriento,
    /// enojado, inquieto, quiero comer y tiene hambre»). El temperamento se
    /// re-lee: el Nervioso es UN APETITO CON CUERPO — intranquilo, con
    /// hambre, enojado. v6.50.92 — LA FAMILIA SE ENCOGE A DOS (la letra:
    /// «borra al grimorio hambriento inestable y al erratico, deja el
    /// original y al nervioso»): quedan el normal SERENO y el Nervioso
    /// HAMBRIENTO (el Errático CAÓTICO y el Inestable POSEÍDO, difuntos).
    /// El ciclo vive en SU PROPIO EstadoGrimorio (la lección de la .86):
    /// los dos libros pasan hambre por separado.
    ///
    /// v6.50.91 — LAS DOS VIDAS DEL NERVIOSO (el umbral es el 50%):
    /// · DEBAJO DEL 50%: EL CAZADOR. Tranquilo y paciente — sin temblor, sin
    ///   glitch, sin sobresaltos (el ojo corre el ciclo SERENO del original,
    ///   NerviosoActivo() apagado). Si el jugador se queda QUIETO 5 s
    ///   (valor de PRUEBA), el libro SALE y flota encima de él MIRÁNDOLO
    ///   (GrimorioNerviosoFlotante), esperando a que algo SE MUEVA cerca
    ///   (≤480 px) — cualquier criatura hostil o no, MENOS los NPC de las
    ///   casas. Cuando pasa, del libro sale LA SOMBRA DE LA PÁGINA
    ///   (SombraPaginaCaza) y muerde: 10% de la vida MÁXIMA por golpe hasta
    ///   matarla y ABSORBERLA (AbsorberPresa: sin loot, sin gore, sin
    ///   crédito del jugador; la XP SÍ cobra) — cada presa baja 1% el
    ///   hambre (50% → 49%) y el libro CAZA HASTA EL 0% («saciado. Por
    ///   ahora.»). Mientras patrulla, EL RELOJ DEL HAMBRE SE CONGELA: sólo
    ///   las presas lo mueven.
    /// · DEL 50% EN ADELANTE: EL IMPACIENTE. «El libro se vuelve inquieto y
    ///   muy hambriento, comienza a decir que tiene hambre y es cuando
    ///   comienzan los efectos visuales a su sprite» — el ojo ansioso
    ///   despierta, el temblor, el glitch y los sobresaltos arrancan DE
    ///   CERO al 50% y escalan al máximo, el libro HABLA (FrasesDeHambre) y
    ///   comienzan las PROBABILIDADES de las OLEADAS DE HAMBRE (4% → 20%
    ///   cada 5 s, GrimorioFuriaSistema.Provocar natural).
    /// · AL 100%: YA NO HAY FUGITIVO (v6.50.94 — la letra del usuario:
    ///   «esto de la fuga no tiene mucho sentido, el jugador se quedaria
    ///   sin el libro, la fuga simplemente la quitamos el libro no se
    ///   fuga, solo queda flotando cerca del jugador cazando por si
    ///   mismo»): el libro NUNCA se escapa — al 100% se queda flotando
    ///   cerca del jugador, CAZANDO POR SU CUENTA.
    ///
    /// v6.50.94 — LAS CUATRO REGLAS NUEVAS DE LA LETRA:
    /// · EL RELOJ SIEMPRE: el hambre sube coma o no coma (la gracia de
    ///   los 10 s de la .92 murió — «el hambre se pausa» era ella).
    /// · LA CAZA POR SU CUENTA: sale del 25% en adelante si el jugador
    ///   está en AFK (quieto 5 s) O lleva mucho tiempo sin atacar
    ///   criaturas con el libro (30 s) — y mientras el jugador dispara
    ///   con el libro, el espíritu NO lanza sombras (la caza solitaria
    ///   es del ausente).
    /// · EL FESTÍN EGOÍSTA: el libro se come a la presa ENTERA (botín
    ///   incluido) y la XP es DE EL LIBRO (sube SU nivel) — el jugador
    ///   no recibe NADA (el festín compartido con el Grimorio del Eterno
    ///   murió con la .93).
    ///
    /// v6.50.95 — EL COMPÁS MURIÓ (la letra del usuario: «creo que mejor
    /// es dejarlo atacar cuando quiera»): sin relojes — la sombra sale
    /// cuando hay presa que se mueva cerca. Y LA DESCARGA PRESTADA
    /// también murió: el clic izq escupe LA LÁGRIMA DE TINTA (el disparo
    /// propio del libro — el sprite del usuario, heredado de la base).
    /// </summary>
    public class GrimorioHambrientoNervioso : GrimorioHambriento
    {
        // === EL GLITCH — las perillas de la .88 (el volumen aprobado) ===
        // v6.50.91: el gate ahora se lee sobre HambreEfectos() — sin glitch
        // por debajo del 12% DE LA FASE IMPACIENTE (= 56% de hambre real)
        // v6.50.102: internal — la MÁQUINA vive ahora en EstadoGrimorio
        // (por estado) y lee las constantes de aquí (su casa conceptual).
        internal const float HAMBRE_MIN_GLITCH = 0.12f;

        // === v6.50.91 — EL UMBRAL DE LA IMPACIENCIA (la letra del usuario):
        //     debajo del 50% el libro es EL CAZADOR (tranquilo, sale a
        //     cazar su propia comida); del 50% en adelante EL IMPACIENTE
        //     (efectos visuales + voz + probabilidades de oleadas) ===
        internal const float UMBRAL_IMPACIENCIA = 0.5f;
        // v6.50.93 — EL UMBRAL DE LA CAZA (la letra: «el libro solo sale
        // a cazar de 25% de hambre en adelante»): debajo del 25% el libro
        // NO sale ni con el jugador quieto — el apetito aún no aprieta.
        internal const float UMBRAL_CACERIA = 0.25f;
        // LA ESPERA: los segundos de quietud del jugador que sacan al libro
        // a cazar (5 s AHORA QUE ES UNA PRUEBA — la letra lo marcó como
        // valor de prueba; el Eterno jugable la re-escribe a 10 s vía
        // TicksQuietudSalida)
        internal const int TICKS_QUIETUD_PRUEBA = 300;
        // v6.50.94 — LA CAZA DEL AUSENTE (la letra: «esto de la caza por su
        // cuenta es cuando el jugador esta en afk o simplemente lleva
        // mucho tiempo sin atacar criaturas con el libro»): 30 s sin
        // disparar con el libro también lo sacan a cazar — aunque el
        // jugador se esté moviendo (no es AFK, pero el libro tiene hambre
        // y su dueño no le está dando uso)
        internal const int TICKS_SIN_ATAQUE_LIBRO = 1800;
        // v6.50.94 — EL MARGEN DEL CAZADOR ACTIVO: mientras el jugador
        // disparó con el libro hace menos de 5 s, el espíritu NO lanza
        // sombras por su cuenta (el jugador está cazando ÉL — la caza
        // solitaria es del ausente)
        internal const int TICKS_MARGEN_ATAQUE = 300;
        // LAS OLEADAS: la tirada de dados cada 5 s con hambre ≥ 50%
        private const int TICKS_ENTRE_OLEADAS = 300;

        private static readonly EstadoGrimorio _estadoNervioso = new EstadoGrimorio { Nervioso = true };
        protected override EstadoGrimorio Estado => _estadoNervioso;

        // === v6.50.102 — LA FAMILIA SE AMPLÍA: EL ETERNO ES UN NERVIOSO
        //     JUGABLE. Todo lo que era static de ESTA clase y hablaba del
        //     «único» libro nervioso se volvió POR ESTADO (EstadoGrimorio)
        //     o POR TIPO de ítem (TipoLibro): el Nervioso de prueba corre
        //     su minuto de demo y el Eterno su cuarto de hora jugable, cada
        //     uno con SU ojo, SU glitch, SU caza y SU festín. ===

        // === v6.50.102 — LAS CONSTANTES DE LA MÁQUINA (viven AQUÍ, su casa
        //     conceptual; la máquina misma vive POR ESTADO en EstadoGrimorio) ===
        // EL SOBRESALTO: el brinco del susto (7 ticks, se asienta)
        internal const int DURACION_SOBRESALTO = 7;

        /// <summary>¿QUÉ LIBRO SOY? El tipo de ítem que dispara esta
        /// instancia (el Eterno lo re-escribe): decide a qué libro van la
        /// XP del festín, qué estado lo anima y qué sprite muestra el
        /// espíritu flotante.</summary>
        protected virtual int TipoLibro => ModContent.ItemType<GrimorioHambrientoNervioso>();

        /// <summary>LOS SEGUNDOS DE QUIETUD que sacan al libro a cazar: la
        /// demo usa 5 s (la letra lo marcó como valor de PRUEBA); el Eterno
        /// jugable pide 10 s (menos intrusivo en partida de verdad).</summary>
        protected virtual int TicksQuietudSalida => TICKS_QUIETUD_PRUEBA;

        /// <summary>EL ESTADO DE UN LIBRO por su tipo de ítem — el registro
        /// de la familia nerviosa (lo leen el espíritu flotante y su sombra
        /// para servir al libro que LOS llamó, no al que llegó primero).</summary>
        internal static EstadoGrimorio EstadoDeLibro(int tipoLibro)
        {
            if (tipoLibro == ModContent.ItemType<GrimoireEternal>())
                return GrimoireEternal.EstadoEterno;
            return _estadoNervioso;
        }

        /// <summary>Dibuja la textura ENTERA o en TIRAS — inventario, mundo y
        /// el espíritu flotante (cada franja a su lugar natural + su desfase,
        /// con la MISMA transform del libro; sin ráfaga es UN draw).</summary>
        internal static void DibujarTiras(SpriteBatch sb, Texture2D tex, Vector2 position, Rectangle frame,
            Color color, float rotation, Vector2 origin, float scale, EstadoGrimorio.Tira[] tiras)
        {
            if (tiras == null || tiras.Length == 0)
            {
                sb.Draw(tex, position, frame, color, rotation, origin, scale, SpriteEffects.None, 0f);
                return;
            }
            int cursor = 0;
            for (int i = 0; i <= tiras.Length; i++)
            {
                int y0 = i < tiras.Length ? tiras[i].Y0 : frame.Height;
                if (y0 > cursor)
                    Franja(sb, tex, position, frame, color, rotation, origin, scale, cursor, y0, 0f);
                if (i < tiras.Length)
                {
                    Franja(sb, tex, position, frame, color, rotation, origin, scale,
                        tiras[i].Y0, tiras[i].Y1, tiras[i].Off);
                    if (tiras[i].Y1 > cursor)
                        cursor = tiras[i].Y1;
                }
            }
        }

        // el texel (0,a) a su lugar natural + el desfase — ROTADO con el libro
        private static void Franja(SpriteBatch sb, Texture2D tex, Vector2 position, Rectangle frame,
            Color color, float rotation, Vector2 origin, float scale, int a, int b, float off)
        {
            if (b <= a)
                return;
            var src = new Rectangle(frame.X, frame.Y + a, frame.Width, b - a);
            Vector2 pos = position + new Vector2(off - origin.X, a - origin.Y) * scale;
            if (rotation != 0f)
                pos = position + (new Vector2(off - origin.X, a - origin.Y) * scale).RotatedBy(rotation);
            sb.Draw(tex, pos, src, color, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        /// <summary>La versión en DrawData de las tiras (la mano) — MISMA
        /// transform del held item. Con la gravedad invertida la franja ancla
        /// por su borde INFERIOR (la lección del espejo de la v6.50.84).</summary>
        private static void AgregarTiras(List<DrawData> cache, Texture2D tex,
            DrawData proto, Rectangle fr, bool espejoY, EstadoGrimorio.Tira[] tiras)
        {
            if (tiras == null || tiras.Length == 0)
            {
                cache.Add(new DrawData(tex, proto.position, proto.sourceRect, proto.color,
                    proto.rotation, proto.origin, proto.scale, proto.effect));
                return;
            }
            int cursor = 0;
            for (int i = 0; i <= tiras.Length; i++)
            {
                int y0 = i < tiras.Length ? tiras[i].Y0 : fr.Height;
                if (y0 > cursor)
                    AgregarFranja(cache, tex, proto, fr, espejoY, cursor, y0, 0f);
                if (i < tiras.Length)
                {
                    AgregarFranja(cache, tex, proto, fr, espejoY, tiras[i].Y0, tiras[i].Y1, tiras[i].Off);
                    if (tiras[i].Y1 > cursor)
                        cursor = tiras[i].Y1;
                }
            }
        }

        private static void AgregarFranja(List<DrawData> cache, Texture2D tex,
            DrawData proto, Rectangle fr, bool espejoY, int a, int b, float off)
        {
            if (b <= a)
                return;
            var src = new Rectangle(fr.X, fr.Y + a, fr.Width, b - a);
            float ay = espejoY ? fr.Height - b - proto.origin.Y : a - proto.origin.Y;
            Vector2 pos = proto.position + new Vector2(
                (off - proto.origin.X) * proto.scale.X,
                ay * proto.scale.Y).RotatedBy(proto.rotation);
            cache.Add(new DrawData(tex, pos, src, proto.color, proto.rotation,
                Vector2.Zero, proto.scale, proto.effect));
        }

        // =================================================================
        // v6.50.102 — EL TEMBLOR, EL GLITCH Y EL SOBRESALTO VIVEN AHORA EN
        // EL ESTADO (EstadoGrimorio.Temblor/Layout/Corrimiento/PasoMaquina):
        // cada libro nervioso — el de prueba y el Eterno — tiembla, se rompe
        // y brinca con SU propia hambre. Las tres vías de dibujo de abajo
        // leen TODO por el Estado virtual (cada subclase elige su reloj).
        // =================================================================

        // =================================================================
        // INVENTARIO — el libro entero lo dibuja la copia (PreDraw false):
        // vanilla dibujaría el libro fijo; aquí viene corrido, temblando y
        /// brincando, y roto en tiras cuando la ráfaga (o el susto) lo pide.
        // =================================================================
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
            => false;

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Estado.PasoMaquina();
            EstadoGrimorio.Tira[] tiras = Estado.Layout(out float offOjo);
            Vector2 corr = Estado.Corrimiento() * scale;

            // el cursor lo sigue el ojo en su CASA (con el corrimiento del
            // cuerpo — el ojo viaja con el libro, nunca desalineado)
            Estado.PosOjoPantalla = position + corr + (OJO - origin) * scale;
            Estado.PosValida = true;

            // EL LIBRO — entero sin ráfaga, en tiras con ella, TEMBLANDO
            DibujarTiras(spriteBatch, ModContent.Request<Texture2D>(Texture).Value,
                position + corr, frame, drawColor, 0f, origin, scale, tiras);

            if (Estado.Fase > 0)
            {
                // el párpado se ROMPE y TIEMBLA con el libro (el mismo layout)
                bool medio = Estado.Fase > 7 || Estado.Fase < 4;
                DibujarTiras(spriteBatch, Capa(medio ? "Medio" : "Cerrado"),
                    position + corr, frame, drawColor, 0f, origin, scale, tiras);
                return;
            }

            // EL IRIS — viaja con su tira y tiembla/brinca con el libro
            var iris = Capa("Iris");
            var irisRojo = Capa("IrisRojo");
            Vector2 posIris = position + corr + (OJO + Estado.DespActual - origin) * scale
                + new Vector2(offOjo * scale, 0f);
            spriteBatch.Draw(iris, posIris, null, drawColor, 0f, iris.Size() * 0.5f,
                scale * IRIS_ESC, SpriteEffects.None, 0f);
            float rojo = Estado.NivelRojo();
            if (rojo > 0f)
                spriteBatch.Draw(irisRojo, posIris, null, Alfa(rojo), 0f,
                    irisRojo.Size() * 0.5f, scale * IRIS_ESC, SpriteEffects.None, 0f);
        }

        // =================================================================
        // MUNDO — la convención vanilla v6.50.84 (pivote-centro + la
        // rotación de vuelo): el libro en tiras, corrido por temblor y susto.
        // =================================================================
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, ref float rotation, ref float scale, int whoAmI)
            => false;

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, float rotation, float scale, int whoAmI)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Estado.PasoMaquina();
            EstadoGrimorio.Tira[] tiras = Estado.Layout(out float offOjo);

            Vector2 pivote = PivoteEnMundo(out var frame, out var origen);
            Vector2 corr = Estado.Corrimiento() * scale;

            // la CASA del ojo (con el corrimiento — el ojo viaja con el cuerpo)
            Vector2 alOjo = (OJO + Estado.DespActual - origen) * scale;
            Vector2 casa = pivote + corr + alOjo.RotatedBy(rotation);
            Estado.PosOjoPantalla = casa;
            Estado.PosValida = true;

            DibujarTiras(spriteBatch, ModContent.Request<Texture2D>(Texture).Value,
                pivote + corr, frame, lightColor, rotation, origen, scale, tiras);

            if (Estado.Fase > 0)
            {
                bool medio = Estado.Fase > 7 || Estado.Fase < 4;
                DibujarTiras(spriteBatch, Capa(medio ? "Medio" : "Cerrado"),
                    pivote + corr, frame, lightColor, rotation, origen, scale, tiras);
                return;
            }

            // el iris gira con el libro, viaja con su tira y con el cuerpo
            Vector2 posOjo = casa + new Vector2(offOjo * scale, 0f).RotatedBy(rotation);
            var iris = Capa("Iris");
            var irisRojo = Capa("IrisRojo");
            spriteBatch.Draw(iris, posOjo, null, lightColor, rotation, iris.Size() * 0.5f,
                scale * IRIS_ESC, SpriteEffects.None, 0f);
            float rojo = Estado.NivelRojo();
            if (rojo > 0f)
                spriteBatch.Draw(irisRojo, posOjo, null, Alfa(rojo), rotation,
                    irisRojo.Size() * 0.5f, scale * IRIS_ESC, SpriteEffects.None, 0f);
        }

        // =================================================================
        // MANO — el corrimiento (temblor + sobresalto) se INYECTA en la
        // DrawData del libro, el glitch son tiras en el cache (v6.50.84).
        // =================================================================
        public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData,
            ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
        {
            if (Main.netMode == NetmodeID.Server)
                return base.ModifyItemDraw(ref drawInfo, ref drawData, ref coloredDrawData, ref glowMaskDrawData);

            // v6.50.93 — LA MANO VACÍA (la letra: «si el jugador ataca con
            // el libro este no tendra el sprite del libro en su mano, pero
            // solo cuando el libro esta fuera ya que esta flotando»):
            // mientras el ESPÍRITU está fuera, el libro REAL es el que
            // flota — en la mano no se dibuja NADA (ni cuerpo, ni tiras,
            // ni ojo: la mano ataca vacía). Al volver el espíritu, el
            // libro de siempre (temblor, glitch y capas).
            if (LibroFuera(drawInfo.drawPlayer))
                return false;

            Estado.PasoMaquina();
            EstadoGrimorio.Tira[] tiras = Estado.Layout(out float offOjo);

            // EL CUERPO: temblor + sobresalto entran en la posición del libro
            drawData.position += Estado.Corrimiento() * Math.Abs(drawData.scale.X);

            bool espejoX = (drawData.effect & SpriteEffects.FlipHorizontally) != 0;
            bool espejoY = (drawData.effect & SpriteEffects.FlipVertically) != 0;
            Rectangle fr = drawData.sourceRect ?? new Rectangle(0, 0, 36, 49);

            AgregarTiras(drawInfo.DrawDataCache, ModContent.Request<Texture2D>(Texture).Value,
                drawData, fr, espejoY, tiras);
            if (coloredDrawData.HasValue)
                drawInfo.DrawDataCache.Add(coloredDrawData.Value);
            if (glowMaskDrawData.HasValue)
                drawInfo.DrawDataCache.Add(glowMaskDrawData.Value);

            if (Estado.Fase > 0)
            {
                bool medio = Estado.Fase > 7 || Estado.Fase < 4;
                AgregarTiras(drawInfo.DrawDataCache, Capa(medio ? "Medio" : "Cerrado"),
                    drawData, fr, espejoY, tiras);
                return false;
            }

            // el iris: el texel del ojo con el espejo (v6.50.84) + su tira
            float rojo = Estado.NivelRojo();
            Vector2 texel = OJO + new Vector2(
                Estado.DespActual.X * (espejoX ? -1f : 1f),
                Estado.DespActual.Y * (espejoY ? -1f : 1f));
            Vector2 q = new Vector2(
                (espejoX ? fr.Width - texel.X : texel.X) - drawData.origin.X,
                (espejoY ? fr.Height - texel.Y : texel.Y) - drawData.origin.Y);
            Vector2 casa = drawData.position + new Vector2(
                q.X * drawData.scale.X, q.Y * drawData.scale.Y).RotatedBy(drawData.rotation);
            Vector2 posOjo = casa + new Vector2(offOjo * drawData.scale.X, 0f).RotatedBy(drawData.rotation);

            var iris = Capa("Iris");
            var irisRojo = Capa("IrisRojo");
            drawInfo.DrawDataCache.Add(new DrawData(iris, posOjo, null, drawData.color,
                drawData.rotation, iris.Size() * 0.5f, drawData.scale * IRIS_ESC, drawData.effect));
            if (rojo > 0f)
                drawInfo.DrawDataCache.Add(new DrawData(irisRojo, posOjo, null, Alfa(rojo),
                    drawData.rotation, irisRojo.Size() * 0.5f, drawData.scale * IRIS_ESC,
                    drawData.effect));

            if (drawInfo.drawPlayer.whoAmI == Main.myPlayer)
            {
                Estado.PosOjoPantalla = casa;
                Estado.PosValida = true;
            }
            return false;
        }

        // =================================================================
        // v6.50.91 — LAS DOS VIDAS DEL NERVIOSO. El controlador vive aquí
        // (es SU hambre la que manda): la quietud del jugador, la salida a
        // caza, la impaciencia con voz y las oleadas probables.
        // v6.50.102 — TODO POR ESTADO: la quietud, el reloj del ausente y
        // la voz viven en EstadoGrimorio (cada libro pasa SU hambre por su
        // cuenta); PasoCaza es de INSTANCIA y lee el Estado virtual.
        // La CAZA toca NPCs: corre SOLO donde el jugador local es la
        // AUTORIDAD (SP o anfitrión de un listen server — el mismo contrato
        // del resto de la demo).
        // =================================================================

        /// <summary>El estado compartido del Nervioso de prueba (el
        /// espíritu flotante y su sombra lo leen como FALLBACK cuando su
        /// libro no viajó en ai[2] — el registro real es EstadoDeLibro).</summary>
        internal static EstadoGrimorio EstadoCompartido => _estadoNervioso;

        /// <summary>La geometría del ojo y la escala del iris (las capas del
        /// libro, para el espíritu flotante — mismo socket, misma familia).</summary>
        internal static Vector2 OjoTexel => OJO;
        internal static float IrisEscala => IRIS_ESC;
        internal static Color AlfaCapa(float a) => Alfa(a);

        /// <summary>¿El espíritu del libro está FUERA? (caza o vuelta).</summary>
        internal static bool LibroFuera(Player jugador)
        {
            int tipo = ModContent.ProjectileType<GrimorioNerviosoFlotante>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.type == tipo && p.owner == jugador.whoAmI)
                    return true;
            }
            return false;
        }

        /// <summary>¿Está fuera EN PATRULLA DE CAZA? (modo caza — el
        /// espíritu vela sobre su dueño esperando presas).</summary>
        internal static bool CaceriaActiva()
        {
            int tipo = ModContent.ProjectileType<GrimorioNerviosoFlotante>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.type == tipo && p.owner == Main.myPlayer &&
                    p.ai[0] == GrimorioNerviosoFlotante.MODO_CAZA)
                    return true;
            }
            return false;
        }

        /// <summary>v6.50.92 — ¿DÓNDE ESTÁ LA COMIDA? Mientras la sombra
        /// devora a una presa, éste es el punto que el libro MIRA (la
        /// letra del usuario: «el libro debe mirar lo que esta comiendo»):
        /// la presa viva mientras la muerde, el cuerpo de la sombra
        /// mientras las almas suben al libro. Null = no hay festín.</summary>
        internal static Vector2? FocoDelFestin(int dueño)
        {
            int tipo = ModContent.ProjectileType<SombraPaginaCaza>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile pr = Main.projectile[i];
                if (pr == null || !pr.active || pr.type != tipo || pr.owner != dueño)
                    continue;
                int idx = (int)pr.ai[0] - 1;
                NPC presa = idx >= 0 && idx < Main.maxNPCs ? Main.npc[idx] : null;
                if (presa != null && presa.active && presa.life > 0)
                    return presa.Center;      // la está MORDIENDO: mirarLA
                return pr.Center;             // la está ABSORBIENDO: mirar el festín
            }
            return null;
        }

        /// <summary>v6.50.93 — ¿DÓNDE ESTÁ EL ESPÍRITU? Mientras el libro
        /// está FUERA, éste es el punto del que nacen SUS ataques (la
        /// letra: «si el jugador ataque y el libro esta fuera sus ataques
        /// salen del libro no del jugador»). Null = el libro está en
        /// casa (los ataques salen del jugador, como siempre).</summary>
        internal static Vector2? PosicionDelLibro(int dueño)
        {
            int tipo = ModContent.ProjectileType<GrimorioNerviosoFlotante>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.type == tipo && p.owner == dueño)
                    return p.Center;
            }
            return null;
        }

        /// <summary>v6.50.93 — EL DISPARO NACE DEL LIBRO FLOTANTE (la
        /// letra: «mientras el libro este fuera todos los ataques que
        /// pertenecen al libro salen del libro»): la descarga perseguidora
        /// parte del ESPÍRITU cuando está fuera — el jugador apunta, el
        /// libro dispara.</summary>
        protected override Vector2 OrigenDelDisparo(Player player, Vector2 position) =>
            PosicionDelLibro(player.whoAmI) is Vector2 libro ? libro : position;

        /// <summary>v6.50.94 — EL JUGADOR DISPARÓ CON EL LIBRO: cada ataque
        /// del dueño pone a cero el reloj de la caza del ausente (la letra:
        /// «esto de la caza por su cuenta es cuando el jugador esta en afk
        /// o simplemente lleva mucho tiempo sin atacar criaturas con el
        /// libro» — disparar con el libro ES darle uso; el espíritu además
        /// calla sus sombras mientras el dueño caza él, 5 s por disparo).
        /// v6.50.102 — el reloj es de ESTE libro (su Estado virtual).</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            bool r = base.Shoot(player, source, position, velocity, type, damage, knockback);
            if (player.altFunctionUse != 2)
                Estado.TicksSinAtaqueLibro = 0;
            return r;
        }

        /// <summary>EL PASO DE LAS DOS VIDAS — una vez por tick desde
        /// UpdateInventory (el guard del tick vive en el ESTADO: vale por
        /// todas las copias de ESTE libro en el inventario — la quietud se
        /// cuenta UNA vez POR LIBRO; el Nervioso de prueba y el Eterno
        /// llevan cada uno SU reloj). La VOZ suena en la pantalla del dueño;
        /// la caza y las oleadas sólo donde el jugador local MANDA.</summary>
        protected void PasoCaza(Player player)
        {
            // otros jugadores: la caza es del dueño LOCAL (el libro no caza
            // por proxy — y así el guard del tick jamás se lo roba otra
            // copia ajena)
            bool local = player.whoAmI == Main.myPlayer &&
                (Main.netMode != NetmodeID.Server || !Main.dedServ);
            if (!local)
                return;

            EstadoGrimorio e = Estado;
            if (Main.GameUpdateCount == e.TickPasoCaza)
                return;                       // 1 avance por tick, POR LIBRO
            e.TickPasoCaza = Main.GameUpdateCount;

            float h = e.Hambre;

            // v6.50.94 — EL RELOJ DEL AUSENTE: un tick más sin que el
            // jugador ataque criaturas con el libro (cada disparo lo
            // reinicia — ver Shoot)
            e.TicksSinAtaqueLibro++;

            // === LA VOZ DEL HAMBRE — «comienza a decir que tiene hambre»:
            //     suena en la pantalla del dueño, en cualquier máquina ===
            FrasesDeHambre(e, h);

            // === v6.50.92 — EL FOCO DEL FESTÍN: mientras la sombra devora,
            //     el ojo del libro (hotbar, mano, mundo) se clava en SU
            //     COMIDA — la letra: «el libro debe mirar lo que esta
            //     comiendo». Visual puro: también en el cliente MP (la
            //     sombra y los NPC llegan sincronizados) ===
            e.FocoPantalla = FocoDelFestin(player.whoAmI) is Vector2 festin
                ? festin - Main.screenPosition
                : (Vector2?)null;

            // === LA AUTORIDAD: SP o anfitrión de un listen server (el
            //     cliente MP sólo oye a su libro — no toca NPCs) ===
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            bool fuera = LibroFuera(player);
            if (fuera)
                e.TicksQuieto = 0;

            if (!fuera)
            {
                // v6.50.94 — LA SALIDA A CAZA POR SU CUENTA: del 25% EN
                // ADELANTE (la letra .93: «el libro solo sale a cazar de
                // 25% de hambre en adelante» — el 100% YA NO EXCLUYE:
                // la fuga murió) cuando el jugador está AUSENTE: quieto
                // (AFK — la letra .91; 5 s de demo, 10 s jugable) O 30 s
                // sin atacar criaturas con el libro (la letra .94). AL
                // 100% NO SE ESCAPA — se queda flotando cerca, cazando
                // (la letra: «la fuga simplemente la quitamos el libro no
                // se fuga, solo queda flotando cerca del jugador cazando
                // por si mismo»).
                // v6.50.102 — el espíritu nace con el TIPO de su libro en
                // ai[2]: servirá al libro que lo llamó (SU hambre, SU XP).
                if (h >= UMBRAL_CACERIA)
                {
                    bool quieto = !player.dead && player.velocity.LengthSquared() < 0.02f;
                    e.TicksQuieto = quieto ? e.TicksQuieto + 1 : 0;
                    if (e.TicksQuieto >= TicksQuietudSalida ||
                        e.TicksSinAtaqueLibro >= TICKS_SIN_ATAQUE_LIBRO)
                    {
                        e.TicksQuieto = 0;
                        e.TicksSinAtaqueLibro = 0;
                        Projectile.NewProjectile(player.GetSource_Misc("NerviosoCaza"),
                            player.Center - new Vector2(0f, 60f), Vector2.Zero,
                            ModContent.ProjectileType<GrimorioNerviosoFlotante>(),
                            0, 0f, player.whoAmI, GrimorioNerviosoFlotante.MODO_CAZA, 0f, TipoLibro);
                        DecirLocal("SaleACazar", new Color(206, 108, 96));
                        try
                        {
                            Terraria.Audio.SoundEngine.PlaySound(
                                SoundID.Item122.WithPitchOffset(-0.2f).WithVolumeScale(0.6f),
                                player.Center);
                        }
                        catch { }
                    }
                }
            }

            // === LAS OLEADAS DE HAMBRE — «es donde comienzan las
            //     probabilidades de que las oleadas de hambre se activen» ===
            if (h >= UMBRAL_IMPACIENCIA &&
                ((Main.GameUpdateCount + (ulong)player.whoAmI * 53ul) % (ulong)TICKS_ENTRE_OLEADAS) == 0ul)
            {
                var config = ModContent.GetInstance<Content.AethonConfigServidor>();
                if ((config == null || config.EventoHambreGrimorio) &&
                    !GrimorioFuriaSistema.Activo && GrimorioFuriaSistema.MundoLibre())
                {
                    // 4% al 50% → 20% al 100%, la tirada cada 5 s
                    float urgencia = (h - UMBRAL_IMPACIENCIA) / (1f - UMBRAL_IMPACIENCIA);
                    if (Main.rand.NextFloat() < 0.04f + 0.16f * urgencia)
                    {
                        int nivel = Math.Max(1, Math.Min(10,
                            player.GetModPlayer<ShardPlayer>().FuriaNivel));
                        GrimorioFuriaSistema.Provocar(player, nivel, natural: true);
                    }
                }
            }
        }

        /// <summary>LA VOZ DEL NERVIOSO — intranquilo, hambriento, enojado
        /// («quiero comer y tiene hambre»): del 50% en adelante el libro
        /// EMPIEZA A DECIRLO, cada vez más seguido y más desesperado.
        /// v6.50.102 — el frío entre frases es de ESTE libro (su estado).</summary>
        private static void FrasesDeHambre(EstadoGrimorio e, float h)
        {
            if (h < UMBRAL_IMPACIENCIA)
            {
                e.TicksHastaFrase = Math.Min(e.TicksHastaFrase, 300);
                return;
            }
            if (--e.TicksHastaFrase > 0)
                return;

            float urgencia = (h - UMBRAL_IMPACIENCIA) / (1f - UMBRAL_IMPACIENCIA);
            e.TicksHastaFrase = (int)(60f * MathHelper.Lerp(9f, 3.5f, urgencia));

            string clave = urgencia < 0.35f ? "Frase" + (1 + Main.rand.Next(2))
                : urgencia < 0.75f ? "Frase" + (3 + Main.rand.Next(2))
                : "Frase5";
            DecirLocal(clave, new Color(206, 108, 96));
        }

        /// <summary>EL LIBRO HABLA — en la pantalla de SU dueño.</summary>
        internal static void DecirLocal(string clave, Color color)
        {
            if (Main.netMode == NetmodeID.Server && Main.dedServ)
                return;
            try
            {
                Main.NewText("«" + Language.GetTextValue("Mods.AethonMod.Hambre.Nervioso." + clave) + "»",
                    color);
            }
            catch { }
        }

        /// <summary>v6.50.104 — LA DIETA POR LIBRO: cuánto hambre quita
        /// comerse a ESTA presa. El Eterno come POR RAREZA (la letra:
        /// «el hambre deberia bajar usando la rareza de las criaturas
        /// como fuente… cada criatura de 1 estrella solo quita el 0.1%
        /// de hambre los jefes quitan 10%…» — GrimoireEternal.
        /// HambrePorPresa: 1★=0,1% … 5★=5%, jefes 10%); el Nervioso de
        /// prueba conserva su −1% plano de demo (su minuto no necesita
        /// más finura).</summary>
        internal static float ReduccionHambre(NPC presa, int tipoLibro)
        {
            if (tipoLibro == ModContent.ItemType<GrimoireEternal>())
                return GrimoireEternal.HambrePorPresa(presa);
            return 0.01f;
        }

        /// <summary>LA ABSORCIÓN (v6.50.91) — la presa muere COMIDA por el
        /// libro: «esta muerte no cuenta como que la hizo el jugador y la
        /// criatura no deja loot, pero si cuenta como exp ya que el mismo
        /// libro la mato». Sin checkDead: la criatura se APAGA bajo la
        /// sombra (sin loot, sin gore, sin crédito) y el hambre baja LO
        /// QUE ESA PRESA VALE (v6.50.104 — LA DIETA POR RAREZA: el Eterno
        /// cobra 1★=0,1% … 5★=5% y un jefe 10%; el Nervioso de prueba su
        /// −1% de demo). La llama la sombra (SombraPaginaCaza) en la
        /// AUTORIDAD con el último mordisco.
        /// v6.50.94 — SIN GRACIA Y SIN MARCA DE DIGESTIÓN: el bocado ya no
        /// congela el reloj del apetito (la letra: «el hambre se pausa» —
        /// la gracia de la .92 moría aquí) y el compás de la sombra ya no
        /// nace en la absorción (la .93 lo marcaba A MITAD del ataque).
        /// v6.50.95 — el compás ENTERO murió (la letra: «dejarlo atacar
        /// cuando quiera»): la absorción ya no toca reloj NINGUNO — sólo
        /// come, resta LA DIETA de hambre y cobra la XP.
        /// v6.50.102 — EL FESTÍN ES DEL LIBRO QUE LLAMÓ A LA SOMBRA: el
        /// tipo viaja en el espíritu (ai[2]) y baja el hambre de ESE libro
        /// — el Nervioso de prueba come para su minuto, el Eterno para su
        /// cuarto de hora (y SU nivel).</summary>
        internal static void AbsorberPresa(NPC presa, Player dueño, int tipoLibro)
        {
            if (presa == null || !presa.active)
                return;

            // LA CADENA COMPLETA (jefes multi-segmento: el Devorador entero)
            int real = presa.realLife >= 0 && presa.realLife != presa.whoAmI ? presa.realLife : presa.whoAmI;
            NPC principal = Main.npc[real];
            NPC paraXP = principal != null && principal.active ? principal : presa;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active)
                    continue;
                bool deLaCadena = n.whoAmI == real || n.realLife == real
                    || (presa.realLife >= 0 && (n.whoAmI == presa.realLife || n.realLife == presa.realLife));
                if (!deLaCadena)
                    continue;
                n.life = 0;
                n.active = false;
                n.netSkip = -1;
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n.whoAmI);
            }

            // EL APETITO: LO QUE ESA PRESA VALE (v6.50.104 — la dieta por
            // rareza de la letra: el Eterno cobra 1★=0,1% … 5★=5%, el jefe
            // 10%; el Nervioso de prueba su −1% de demo) — y el reloj
            // SIGUE corriendo (v6.50.94: sin gracia, el hambre sube SIEMPRE)
            EstadoGrimorio festin = EstadoDeLibro(tipoLibro);
            float quita = ReduccionHambre(paraXP, tipoLibro);
            festin.Hambre = Math.Max(0f, festin.Hambre - quita);

            // v6.50.94 — EL FESTÍN EGOÍSTA (la letra: «el festin compartido
            // no tiene sentido, el libro casa por estar hambriento, en este
            // caso el libro debe ser egoista y comer toda la criatura loot
            // incluido por eso el libro recibe la exp y el jugador nada»):
            // la presa se ha comido ENTERA (sin loot — la desactivación de
            // arriba ya lo aseguró) y la XP es DEL LIBRO QUE COME — el
            // jugador no recibe NADA.
            CobrarXPLibro(dueño, paraXP, tipoLibro);

            // el grito silencioso del apetito satisfecho — CON LA CIFRA
            // REAL del bocado (v6.50.104: la dieta por rareza hace que
            // cada presa pese distinto: «Absorbe: −{0}% hambre»)
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                try
                {
                    CombatText.NewText(presa.Hitbox, new Color(235, 70, 55),
                        string.Format(Language.GetTextValue("Mods.AethonMod.Hambre.Nervioso.Absorbe"),
                            (quita * 100f).ToString("0.##")));
                }
                catch { }
            }
        }

        /// <summary>v6.50.94 — LA XP ES DEL LIBRO QUE COME (la letra: «el
        /// libro debe ser egoista y comer toda la criatura loot incluido
        /// por eso el libro recibe la exp y el jugador nada»): cada presa de
        /// LA SOMBRA sube el nivel DEL LIBRO QUE LA COMIÓ (ShardLevelItem
        /// vive en el ítem); el jugador no cobra NI latido.
        /// v6.50.102 — POR TIPO: el Nervioso de prueba cobra su XP y el
        /// Eterno la suya — el festín egoísta alimenta a quien cazó.</summary>
        private static void CobrarXPLibro(Player dueño, NPC presa, int tipoLibro)
        {
            try
            {
                // el nivel DEL LIBRO que está comiendo (la primera copia manda)
                int nivelLibro = 1;
                bool hayLibro = false;
                for (int i = 0; i < 59; i++)
                {
                    Item inv = dueño.inventory[i];
                    if (inv == null || inv.type != tipoLibro)
                        continue;
                    if (!hayLibro)
                    {
                        var slx = inv.GetGlobalItem<ShardLevelItem>();
                        if (slx != null)
                            nivelLibro = slx.Level;
                        hayLibro = true;
                    }
                }
                if (!hayLibro)
                    return;

                int xp = ShardLevelSystem.ApplyXPMultiplier(
                    ShardLevelSystem.XPForNPC(presa, nivelLibro));
                if (xp <= 0)
                    return;

                // TODAS las copias de ese libro cobran (comparten el mismo
                // hambre de su clase — son el mismo apetito)
                for (int i = 0; i < 59; i++)
                {
                    Item inv = dueño.inventory[i];
                    if (inv == null || inv.type != tipoLibro)
                        continue;
                    var sl = inv.GetGlobalItem<ShardLevelItem>();
                    if (sl != null)
                        sl.GrantXP(inv, xp);
                }
            }
            catch { }
        }

        // =================================================================
        // v6.50.91 — EL RELOJ Y LAS DOS VIDAS (reemplaza el UpdateInventory
        // de la base: el Nervioso tiene horas propias).
        // =================================================================
        public override void UpdateInventory(Player player)
        {
            bool local = player.whoAmI == Main.myPlayer &&
                (Main.netMode != NetmodeID.Server || !Main.dedServ);

            if (local && Main.GameUpdateCount != Estado.TickMarcado)
            {
                Estado.TickMarcado = Main.GameUpdateCount;
                // v6.50.94 — EL RELOJ CORRE SIEMPRE, SIN GRACIA (la letra:
                // «el hambre se pausa» — la gracia de los 10 s de la .92
                // congelaba el apetito tras cada bocado): el apetito avanza
                // lo cace o no cace, el libro fuera o dentro, coma o no
                // coma. v6.50.102 — LA TASA ES DE LA FAMILIA: el Nervioso
                // de prueba corre su minuto de demo; el Eterno jugable su
                // hora (.103). Las presas lo bajan (v6.50.104: POR RAREZA
                // en el Eterno — cada baja acreditada también alimenta,
                // GlobalNPCXP) — y el clic derecho lo reinicia SOLO en el
                // libro de prueba.
                Estado.Paso();
            }

            PasoCaza(player);
        }

        /// <summary>LA FASE DEL NERVIOSO en el tooltip — sus estados NO son
        /// los del resto de la familia (cazador / impaciente — el FUGITIVO
        /// murió con la fuga en la .94).</summary>
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            float h = Estado.Hambre;
            string clave = CaceriaActiva() ? "Cazando"
                : h >= UMBRAL_IMPACIENCIA ? "Impaciente"
                : h >= UMBRAL_CACERIA ? "Hambriento"   // v6.50.93 — listo para cazar
                : "Tranquilo";
            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].Name == "HambreEstado")
                {
                    tooltips[i].Text = Language.GetTextValue("Mods.AethonMod.Hambre.Nervioso." + clave);
                    break;
                }
            }

            // v6.50.94 — EL NIVEL DEL LIBRO: la XP de sus presas es SUYA (el
            // festín egoísta — la letra: «el libro recibe la exp y el
            // jugador nada»): el nivel vive en el ítem (ShardLevelItem) y
            // se muestra junto al apetito.
            try
            {
                var sl = Item.GetGlobalItem<ShardLevelItem>();
                if (sl != null)
                    tooltips.Add(new TooltipLine(Mod, "NivelNervioso",
                        Language.GetTextValue("Mods.AethonMod.Hambre.Nervioso.Nivel",
                            sl.Level, sl.XP, sl.XPForNextLevel()))
                        { OverrideColor = new Color(198, 160, 78) });
            }
            catch { }
        }
    }
}
