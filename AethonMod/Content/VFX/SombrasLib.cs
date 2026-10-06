using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using AethonMod.Content.Effects.Bruma;

namespace AethonMod.Content.VFX
{
    /// <summary>
    /// SOMBRASLIB — v6.50.62 — LA LIBRERÍA DE LAS SOMBRAS DEVORADORAS.
    ///
    /// El lenguaje visual de Pride (Selim Bradley, FMA:B) traducido a
    /// primitivas de la casa: masa NEGRA que TAPA (lote alfa — el negro
    /// aditivo es invisible, para oscurecer hay que salir del aditivo),
    /// OJOS blancos que se abren de golpe y MIRAN (lote aditivo), DIENTES
    /// blancos triangulares, CHARCOS de sombra en el suelo y BRUMA que
    /// se come la luz. Todo determinista (semillas por whoAmI) y sin
    /// sprites: Pixel + SoftGlow + GlowOrb, los tres pinceles del motor.
    ///
    /// CONTRATO DE LOTES (la lección de la .58): cada método de dibujo
    /// abre y CIERRA sus propios lotes vía VFXCore.Flush* — el llamador
    /// (un PreDraw de la casa) hace CerrarLoteSiAbierto() antes y
    /// ReabrirLoteVanilla() al final. Aquí dentro jamás queda un Begin
    /// vivo: los vuelcos son atómicos por diseño.
    /// </summary>
    public static class SombrasLib
    {
        // ==================================================================
        //  LA PALETA DE PRIDE — negro absoluto, blanco de hueso, rojo
        //  visceral (la luz que MATA la sombra es la única con color).
        // ==================================================================

        /// <summary>El negro de la masa (casi absoluto: 8/4/7, un pelo de violeta para que se lea sobre fondo negro).</summary>
        public static readonly Color Negro = new(8, 4, 7);

        /// <summary>El blanco de los ojos y los dientes (hueso cálido, no clínico).</summary>
        public static readonly Color Blanco = new(250, 248, 244);

        /// <summary>El rojo de las pupilas y las fauces abiertas.</summary>
        public static readonly Color Rojo = new(198, 18, 24);

        /// <summary>Rojo profundo para el interior de las bocas (garganta).</summary>
        public static readonly Color RojoGarganta = new(120, 8, 12);

        // ==================================================================
        //  EL CUERPO — la polilínea orgánica (la columna del tentáculo)
        // ==================================================================

        /// <summary>
        /// LA COLUMNA VERTEBRAL DEL TENTÁCULO: N puntos de la RAÍZ a la
        /// CABEZA siguiendo una Bézier cuadrática whose control point
        /// ondula con el tiempo — el flujo líquido de Pride: la masa NUNCA
        /// va recta, SIEMPRE serpentea (y serpentea MÁS cuanto más larga,
        /// la física del látigo). Determinista por semilla.
        /// </summary>
        /// <param name="raiz">Nacimiento (el jugador / su sombra).</param>
        /// <param name="cabeza">La boca (el jefe).</param>
        /// <param name="tiempo">GlobalTimeWrappedHourly.</param>
        /// <param name="semilla">Semilla determinista (whoAmI·k).</param>
        /// <param name="puntos">Cantidad de puntos (18 por defecto).</param>
        /// <param name="amplitud">0..1 — cuánto serpentrea (0.22 = vivo).</param>
        public static Vector2[] Columna(Vector2 raiz, Vector2 cabeza, float tiempo, int semilla,
            int puntos = 18, float amplitud = 0.22f)
        {
            puntos = Math.Max(puntos, 2);
            Vector2[] col = new Vector2[puntos];

            float dist = Vector2.Distance(raiz, cabeza);
            // el látigo largo ondea MÁS (la amplitud vive con la longitud)
            float escala = Math.Min(dist / 420f, 2.2f);
            Vector2 medio = (raiz + cabeza) * 0.5f;
            Vector2 perp = new Vector2(-(cabeza - raiz).Y, (cabeza - raiz).X);
            perp = perp.LengthSquared() < 1f ? Vector2.UnitY : Vector2.Normalize(perp);

            // DOS ondas superpuestas (lenta + viva): el vaivén orgánico
            float onda = MathF.Sin(tiempo * 1.35f + semilla * 0.71f) * 0.62f
                       + MathF.Sin(tiempo * 3.1f + semilla * 1.37f) * 0.38f;
            // la fase lenta DESPLAZA el punto de control, la viva lo tiembla
            float vivo = 1f + 0.18f * MathF.Sin(tiempo * 5.3f + semilla);
            Vector2 ctrl = medio + perp * (onda * dist * amplitud * escala * vivo * 0.5f);

            for (int i = 0; i < puntos; i++)
            {
                float t = i / (float)(puntos - 1);
                // Bézier cuadrática raíz→ctrl→cabeza
                float u = 1f - t;
                col[i] = u * u * raiz + 2f * u * t * ctrl + t * t * cabeza;
            }
            return col;
        }

        // ==================================================================
        //  v6.50.67 — LA COLUMNA VIVA: la física del GIF del usuario
        //  (emerge → golpea con OVERSHOOT → ondula al retraer).
        // ==================================================================

        /// <summary>
        /// EL ESTADO FÍSICO de un tentáculo (verlet): posición y posición
        /// previa por junta — la inercia VIVE aquí (la v6.50.66 era una
        /// Bézier recalculada: cero memoria, cero látigo).
        /// </summary>
        private class EspinaViva
        {
            public Vector2[] Pos;
            public Vector2[] Prev;
            public uint UltimoFrame;
            public uint UltimoUso;
        }

        /// <summary>Las espinas vivas cacheadas por semilla (poda por edad).</summary>
        private static readonly System.Collections.Generic.Dictionary<int, EspinaViva> _espinas = new();

        /// <summary>
        /// v6.50.67 — LA COLUMNA VIVA: la misma firma que
        /// <see cref="Columna"/> pero con FÍSICA DE VERLET — la letra del
        /// GIF de referencia del usuario: «onda viajera base→punta, desenrollado
        /// explosivo, OVERSHOOT de látigo, retractación con ondulación
        /// secundaria, inercia de follow-through (la punta sigue moviéndose
        /// 2-3 frames después de que la base paró)».
        ///
        /// LA RECETA: cada junta recuerda su posición anterior (VERLET —
        /// la velocidad es la resta, gratis), la RAÍZ va anclada dura, la
        /// CABEZA persigue su objetivo con resorte SUBAMORTIGUADO (el
        /// overshoot sale SOLO), la ONDA VIAJERA empuja en perpendicular
        /// con fase que viaja raíz→punta, el GANCHO curva el último
        /// cuarto y la cuerda (constraint de distancia) reparte el resto.
        /// Dos pasos jamás por frame (compuerta por GameUpdateCount) y
        /// estados huérfanos podidos por edad.
        ///
        /// SOLO PARA DIBUJAR: el COLLIDING se queda con la Columna
        /// determinista (misma forma en cliente y servidor — la física
        /// visual no viaja por la red y no debe afectar gameplay).
        /// </summary>
        /// <param name="gancho">−1..1 — cuánto y hacia qué lado se curva la punta (el gancho del GIF).</param>
        public static Vector2[] ColumnaViva(Vector2 raiz, Vector2 cabeza, float tiempo, int semilla,
            int puntos = 18, float amplitud = 0.22f, float gancho = 0f)
        {
            puntos = Math.Max(puntos, 4);
            if (Main.netMode == NetmodeID.Server) return Columna(raiz, cabeza, tiempo, semilla, puntos, amplitud);

            // === el estado (crece si cambia el tamaño pedido) ===
            if (!_espinas.TryGetValue(semilla, out EspinaViva esp) || esp.Pos.Length != puntos)
            {
                // nace sobre la Bézier estática: arranque orgánico, sin salto
                Vector2[] inicial = Columna(raiz, cabeza, tiempo, semilla, puntos, amplitud);
                esp = new EspinaViva
                {
                    Pos = (Vector2[])inicial.Clone(),
                    Prev = (Vector2[])inicial.Clone(),
                    UltimoFrame = 0,
                    UltimoUso = Main.GameUpdateCount,
                };
                _espinas[semilla] = esp;

                // LA PODA (barata, solo al insertar): fuera estados sin
                // uso hace 600 frames + tope duro de 128 espinas vivas
                if (_espinas.Count > 128)
                {
                    var muertas = new System.Collections.Generic.List<int>();
                    foreach (var kv in _espinas)
                        if (Main.GameUpdateCount - kv.Value.UltimoUso > 600) muertas.Add(kv.Key);
                    for (int i = 0; i < muertas.Count; i++) _espinas.Remove(muertas[i]);
                    if (_espinas.Count > 128) _espinas.Clear();   // red de seguridad
                }
            }
            esp.UltimoUso = Main.GameUpdateCount;

            // === LA COMPUERTA: un solo paso de física por frame de juego ===
            if (esp.UltimoFrame == Main.GameUpdateCount)
                return esp.Pos;
            esp.UltimoFrame = Main.GameUpdateCount;

            int n = puntos;
            Vector2[] pos = esp.Pos, prev = esp.Prev;

            // === 1 · LA INERCIA (verlet): la velocidad es la resta ===
            float dist = Vector2.Distance(raiz, cabeza);
            Vector2 eje = dist < 1f ? -Vector2.UnitY : (cabeza - raiz) * (1f / dist);
            Vector2 perp = new(-eje.Y, eje.X);
            float escala = Math.Min(dist / 420f, 2.2f);           // el látigo largo ondea MÁS
            float ladoGancho = semille(semilla) > 0.5f ? 1f : -1f;

            for (int i = 1; i < n; i++)
            {
                float f = i / (float)(n - 1);
                Vector2 vel = pos[i] - prev[i];

                // LA ONDA VIAJERA — la fase RESTA hacia la punta: la cresta
                // VIAJA raíz→punta con el tiempo (la letra del GIF)
                float onda = MathF.Sin(tiempo * 2.6f - f * 5.5f + semille(semilla) * 6.28f) * 0.62f
                           + MathF.Sin(tiempo * 4.9f - f * 9.2f + semille(semilla + 7) * 6.28f) * 0.38f;

                // EL GANCHO — el último cuarto se curva hacia su lado
                float ganchoF = gancho != 0f
                    ? MathF.Max(0f, f - 0.72f) / 0.28f * gancho * ladoGancho
                    : 0f;

                Vector2 acel = perp * (onda * dist * amplitud * escala * 0.020f + ganchoF * 1.6f)
                             + new Vector2(0f, 0.42f);            // el peso (cae un pelo)

                prev[i] = pos[i];
                pos[i] += vel * 0.88f + acel;                     // damping 0.88: vivo sin ser eterno
            }

            // === 2 · LAS ANCLAS: la raíz DURA, la cabeza con resorte ===
            prev[0] = pos[0] = raiz;
            // el resorte del cabeza: SUBAMORTIGUADO (k 0.30) — el overshoot del látigo
            pos[n - 1] += (cabeza - pos[n - 1]) * 0.30f;

            // === 3 · LA CUERDA (constraint de distancia ×3 pasadas) ===
            float segLen = MathF.Max(dist, 110f) / (n - 1);        // holgura mínima: cerca, la sombra SE ENROSCA
            for (int pasada = 0; pasada < 3; pasada++)
            {
                for (int i = 1; i < n; i++)
                {
                    Vector2 d = pos[i] - pos[i - 1];
                    float len = d.Length();
                    if (len < 0.001f) continue;
                    float corr = (len - segLen) / len;

                    if (i == 1) pos[i] -= d * corr;                // la raíz no se mueve
                    else if (i == n - 1)
                    {
                        // la punta cede la mitad (su resorte también tira)
                        pos[i] -= d * corr * 0.5f;
                        pos[i - 1] += d * corr * 0.5f;
                    }
                    else
                    {
                        pos[i] -= d * corr * 0.5f;
                        pos[i - 1] += d * corr * 0.5f;
                    }
                }
            }

            return pos;
        }

        /// <summary>
        /// v6.50.67 — DIBUJA LA MASA DE VERDAD: la v6.50.66 era una CADENA
        /// DE LÍNEAS GORDAS («solo son líneas geométricas» — la letra del
        /// usuario). AHORA es un CUERPO: la CINTA de carne texturizada
        /// (fibras musculares + vetas carmesí que FLUYEN raíz→punta, la
        /// textura horneada Carne de VFXCore muestreada por bandas), con
        /// PERFIL MUSCULAR (bulbo en la base, S-taper a la punta — no una
        /// recta), el VELO que respira, EL BORDE DE ENERGÍA (cintas
        /// aditivas finas a cada lado: violeta en la base → ROJO SANGRE
        /// en la punta, con un PULSO que viaja — la lectura garantizada
        /// sobre cualquier fondo), ESPINAS DE HUESO curvas alternando
        /// lados (colmillos de verdad, no púas-barra) y la FILA DE
        /// VENTOSAS del lomo (la referencia del spritesheet del usuario).
        /// </summary>
        public static void Masa(Vector2[] col, float grosorRaiz, float grosorPunta, float alfa, int semilla, float tiempo)
        {
            if (col == null || col.Length < 2 || alfa <= 0.02f) return;

            var carne = VFXCore.Carne;
            int n = col.Length;

            // === EL PERFIL MUSCULAR: bulbo + S-taper + la respiración ===
            var anchos = new float[n];
            for (int i = 0; i < n; i++)
            {
                float f = i / (float)(n - 1);
                // el S-taper (smoothstep: la masa NO se afila en línea recta)
                float g = MathHelper.Lerp(grosorRaiz, grosorPunta, f * f * (3f - 2f * f));
                // EL BULBO — el bíceps del tentáculo a un quinto del nacimiento
                g *= 1f + 0.16f * MathF.Exp(-((f - 0.18f) / 0.16f) * ((f - 0.18f) / 0.16f));
                // la respiración a lo largo (la ondulación de energía viva)
                g *= 0.9f + 0.1f * MathF.Sin(tiempo * 6.2f + f * 7f + semille(semilla));
                anchos[i] = g;
            }

            // === CAPA 1 · LA CARNE (la cinta que TAPA — lote alfa) ===
            if (carne != null)
            {
                VFXCore.Begin();
                VFXCore.Ribbon(col, anchos, Alfa(Blanco, alfa), carne,
                    uvFlow: tiempo * 0.05f);
                VFXCore.FlushAlpha();
            }
            else
            {
                // fallback (sin dispositivo): la cadena clásica
                VFXCore.Begin();
                for (int i = 1; i < n; i++)
                    VFXCore.Line(col[i - 1], col[i], Alfa(Negro, alfa), anchos[i]);
                VFXCore.FlushAlpha(VFXCore.Pixel);
            }

            // === CAPA 2 · EL VELO (el borde que respira — ×1.7, tenue) ===
            VFXCore.Begin();
            VFXCore.Ribbon(col, anchos, Alfa(Negro, alfa * 0.35f), carne,
                uvFlow: tiempo * 0.05f, edgeOutset: 0.35f);
            VFXCore.FlushAlpha();

            // === CAPA 3 · EL BORDE DE ENERGÍA (aditivo, por segmento):
            // violeta en la raíz → ROJO SANGRE en la punta + el PULSO que
            // VIAJA raíz→punta — el filo del tajo.png: «luminous red edge» ===
            var bordes = new Color[n];
            for (int i = 0; i < n; i++)
            {
                float f = i / (float)(n - 1);
                Color c = Color.Lerp(Violeta, Rojo, MathF.Pow(f, 1.3f));
                // EL PULSO — dos crestas que suben por el cuerpo (fase viajera)
                float pulso = 0.55f + 0.45f * MathF.Sin(tiempo * 4.2f - f * 9f + semille(semilla) * 6.28f);
                bordes[i] = Alfa(c, (0.10f + 0.16f * f) * alfa * pulso);
            }
            VFXCore.Begin();
            VFXCore.RibbonTinted(col, anchos, bordes, carne,
                uvFlow: tiempo * 0.05f, edgeInset: 0.40f);
            VFXCore.FlushAdditive();

            // === CAPA 4 · LAS ESPINAS DE HUESO (colmillos curvos
            // alternando lado — la sierra afilada de la v6.50.66 ahora
            // con dientes DE VERDAD) ===
            if (VFXCore.Colmillo != null)
            {
                VFXCore.Begin();
                for (int i = 2; i < n - 2; i++)
                {
                    float f = i / (float)(n - 1);
                    if (f < 0.42f) continue;
                    float fase = MathF.Sin(i * 2.399f + Frac(semille(semilla) * 5.9f) * MathHelper.TwoPi);
                    if (fase < 0.35f) continue;

                    Vector2 dir = col[Math.Min(i + 1, n - 1)] - col[Math.Max(i - 1, 0)];
                    if (dir.LengthSquared() < 0.01f) continue;
                    dir = Vector2.Normalize(dir);
                    Vector2 perp = new(-dir.Y, dir.X);
                    float lado = fase > 0 ? 1f : -1f;

                    // el colmillo apunta hacia FUERA y se curva hacia atrás
                    float tamaño = anchos[i] * (0.85f + 0.3f * Frac(semille(semilla + i) * 3.1f));
                    Vector2 punta = col[i] + perp * lado * (anchos[i] * 0.5f + tamaño * 0.4f);
                    Vector2 rumbo = perp * lado - dir * 0.45f;      // el curvado hacia atrás
                    // (la textura Colmillo apunta ARRIBA: rot = angulo(rumbo) + π/2)
                    VFXCore.Quad(col[i] + perp * lado * anchos[i] * 0.42f,
                        Alfa(Blanco, 0.9f * alfa),
                        new Vector2(tamaño * 0.55f, tamaño),
                        rumbo.ToRotation() + MathHelper.PiOver2, VFXCore.Colmillo);
                }
                VFXCore.FlushAdditive();
            }

            // === CAPA 5 · LAS VENTOSAS DEL LOMO (la fila blanca del
            // spritesheet de referencia — «white circular suckers along
            // its length»: la panza del tentáculo, el lado que SUJETA) ===
            if (VFXCore.Ventosa != null)
            {
                VFXCore.Begin();
                int cada = Math.Max(2, n / 8);
                for (int i = 3; i < n - 3; i += cada)
                {
                    float f = i / (float)(n - 1);
                    Vector2 dir = col[Math.Min(i + 1, n - 1)] - col[Math.Max(i - 1, 0)];
                    if (dir.LengthSquared() < 0.01f) continue;
                    dir = Vector2.Normalize(dir);
                    Vector2 perp = new(-dir.Y, dir.X);
                    // el lado del lomo: el que MIRA ABAJO (la ventosa sujeta)
                    if (perp.Y > 0f) perp = -perp;
                    float tamaño = anchos[i] * 0.34f;
                    VFXCore.Quad(col[i] + perp * anchos[i] * 0.30f,
                        Alfa(Blanco, 0.55f * alfa),
                        new Vector2(tamaño, tamaño), 0f, VFXCore.Ventosa);
                }
                VFXCore.FlushAdditive();
            }
        }

        /// <summary>El charco de sombra del que nace todo (el ancla en el suelo).</summary>
        public static void Charco(Vector2 pos, float radio, float alfa, float tiempo, int semilla)
        {
            if (alfa <= 0.02f) return;
            VFXCore.Begin();
            // tres elipses solapadas ondulando: el charco VIVO
            for (int k = 0; k < 3; k++)
            {
                float w = radio * (1.05f + 0.12f * MathF.Sin(tiempo * 1.7f + k * 2.4f + semille(semilla)));
                float h = w * (0.34f + 0.05f * MathF.Sin(tiempo * 2.3f + k));
                VFXCore.Quad(pos + new Vector2((k - 1) * radio * 0.22f, 0f), Alfa(Negro, alfa * (k == 1 ? 1f : 0.7f)),
                    new Vector2(w, h), VFXCore.GlowOrb);
            }
            VFXCore.FlushAlpha();
        }

        // ==================================================================
        //  LOS OJOS — blancos, se abren DE GOLPE y MIRAN
        // ==================================================================

        /// <summary>El violeta del RIM de la masa y las venas de bruma: la
        /// sombra mágica se LEE sobre cualquier fondo (v6.50.65).</summary>
        public static readonly Color Violeta = new(118, 74, 190);

        /// <summary>El humo base de la bruma negra: negro con un fulgor
        /// violáceo (v6.50.65 — el negro puro era invisible).</summary>
        public static readonly Color HumoNegro = new(18, 10, 28);

        /// <summary>El color al que SE ENFRÍA la bruma al disolverse: el
        /// borde violáceo-gris que hace visible cada puff (v6.50.65).</summary>
        public static readonly Color HumoVioleta = new(64, 42, 96);

        /// <summary>
        /// UN OJO: esclerótica blanca elíptica (aditivo), pupila roja
        /// desplazada hacia <paramref name="mirada"/>. El tamaño late
        /// con <paramref name="abierto"/> (0 = cerrado, 1 = bien abierto):
        /// los ojos de Pride NO parpadean suaves — SE ABREN de golpe, y
        /// la apertura se anima con un ease-out violento en el llamador.
        /// v6.50.65 — EL HALO: un resplandor blanco suave detrás de la
        /// esclerótica para que el ojo PRENDA en cualquier fondo.
        /// v6.50.67 — EL OJO RASGADO: con <paramref name="rasgada"/> el
        /// ojo es el DRAGÓN de la idea central del usuario — esclerótica
        /// cálida + iris carmesí + LA RENDIJA VERTICAL (la textura
        /// horneada OjoRasgado: la rendija va con alpha 0 y en el lote
        /// aditivo añade CERO — se lee NEGRA sobre cualquier fondo).
        /// </summary>
        public static void Ojo(Vector2 pos, float tamaño, Vector2 mirada, float abierto, bool pupila = true, bool rasgada = false)
        {
            if (abierto <= 0.03f || tamaño <= 0.5f) return;
            float a = MathHelper.Clamp(abierto, 0f, 1f);

            // EL HALO (v6.50.65): el ojo BRILLA antes de existir
            VFXCore.Quad(pos, Alfa(Blanco, 0.20f * a), new Vector2(tamaño * 2.1f, tamaño * 1.5f));

            if (rasgada && VFXCore.OjoRasgado != null)
            {
                // EL OJO DEL DRAGÓN — la rendija vertical, inclinada un
                // pelo hacia donde MIRA (la textura apunta con la rendija
                // VERTICAL: la inclinación es el gesto)
                float inclinacion = mirada.LengthSquared() < 0.01f ? 0f
                    : MathHelper.Clamp(mirada.X * 0.0006f, -0.35f, 0.35f);
                Vector2 m = mirada.LengthSquared() < 0.01f ? Vector2.Zero
                    : Vector2.Normalize(mirada) * tamaño * 0.10f;
                VFXCore.Quad(pos + m, Alfa(Blanco, 0.95f * a),
                    new Vector2(tamaño, tamaño * 0.62f), inclinacion, VFXCore.OjoRasgado);
                return;
            }

            VFXCore.Quad(pos, Alfa(Blanco, 0.92f * a), new Vector2(tamaño, tamaño * 0.62f));
            if (pupila)
            {
                Vector2 m = mirada.LengthSquared() < 0.01f ? Vector2.Zero : Vector2.Normalize(mirada) * tamaño * 0.18f;
                VFXCore.Quad(pos + m, Alfa(Rojo, 0.95f * a), new Vector2(tamaño * 0.34f, tamaño * 0.40f));
            }
        }

        /// <summary>
        /// LOS OJOS DE LA MASA: repartidos por la columna con semilla
        /// fija, mirando TODOS a la misma presa (la firma de Pride: la
        /// masa entera TE MIRA). Cada ojo abre con retraso escalonado
        /// (aparecen de golpe, uno tras otro) y late.
        /// </summary>
        /// <param name="presencia">0..1 — cuántos ojos hay abiertos ya.</param>
        public static void OjosDeMasa(Vector2[] col, Vector2 presa, float presencia, int semilla, float tiempo, int cantidad = 5)
        {
            if (presencia <= 0.02f || col == null || col.Length < 4) return;
            VFXCore.Begin();
            int n = col.Length;
            for (int k = 0; k < cantidad; k++)
            {
                float t01 = 0.24f + 0.66f * ((k * 0.618034f + 0.13f) % 1f);   // áureo: reparto orgánico
                int idx = (int)(t01 * (n - 1));
                Vector2 pos = col[idx];
                // apertura escalonada + el latido del ojo
                float local = presencia * cantidad * 0.9f - k * 0.9f;
                float abierto = MathHelper.Clamp(local, 0f, 1f);
                abierto *= 0.85f + 0.15f * MathF.Sin(tiempo * 3.3f + k * 2.1f + semille(semilla));
                if (abierto <= 0.05f) continue;
                float tamaño = 16f + 9f * Frac(semille(semilla) * 0.173f + k * 0.37f);
                Ojo(pos, tamaño, presa - pos, abierto);
            }
            VFXCore.FlushAdditive();
        }

        // ==================================================================
        //  v6.50.69 — LAS FAUCES PROCEDURALES JUBILADAS: la letra del
        //  usuario («en el acto de devoración solo está la versión
        //  anterior de La Sombra… debes cambiar la boca por la forma
        //  actual») — TODO lo que era boca es ahora CABEZA DE BRUMA +
        //  EL DEVORADOR (abajo). El método murió con su último
        //  llamador (el festín de la .68).
        // ==================================================================

        // ==================================================================
        //  LA BRUMA Y LAS ALMAS
        //  v6.50.65 — LA BRUMA DE VERDAD: la bruma de la .64 (quads
        //  SoftGlow casi negros al 30%) era INVISIBLE — negro sobre
        //  negro, puffs minúsculos, cero textura. AHORA usa BrumaFX (la
        //  librería de humo de la casa: flipbooks de ruido fBm horneados,
        //  sub-blobs que respiran, invariancia de escala) en el lote de
        //  MASA (AlphaBlend — humo que OCLUYE) con la RAMPA DE
        //  ENFRIAMIENTO violácea: cada puff NACE negro, se DESGARRA y se
        //  disuelve en gris-violeta — el borde SIEMPRE se lee — más una
        //  segunda pasada aditiva de ALIENTO VIOLETA entre los puffs
        //  (la magia de la sombra PRENDE en cualquier fondo).
        // ==================================================================

        /// <summary>
        /// EL PATRÓN COMPARTIDO: abre el lote de masa de BrumaFX, dibuja
        /// los puffs que el callback pida (en coords de PANTALLA) y lo
        /// cierra — blindado con try/finally para no dejar jamás un Begin
        /// vivo (la lección de la .58).
        /// </summary>
        private static void LoteDeBruma(Action dibujar)
        {
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                BrumaFX.BeginMass();
                dibujar();
            }
            catch { }
            finally
            {
                try { Main.spriteBatch.End(); } catch { }
                VFXCore.CerrarLoteSiAbierto();
            }
        }

        /// <summary>
        /// EL Aliento violeta (la segunda capa): puffs aditivos violáceos
        /// MUY tenues entre la bruma — la lectura garantizada de noche y
        /// en cueva. Va por VFXCore (lote aditivo de la casa).
        /// </summary>
        private static void AlientoVioleta(Vector2 pos, float radio, float alfa, float tiempo, int semilla, int k)
        {
            float f0 = Frac(semille(semilla) * 0.61f + k * 0.618034f);
            float late = 0.7f + 0.3f * MathF.Sin(tiempo * (1.1f + f0) + k * 2.4f);
            VFXCore.Quad(pos + new Vector2(MathF.Sin(tiempo * 0.8f + k * 2.1f) * radio * 0.5f,
                                           -radio * 0.35f * f0),
                Alfa(Violeta, alfa * 0.11f * late), new Vector2(radio * 1.9f, radio * 1.5f));
        }

        /// <summary>
        /// LA NUBE DE BRUMA que desintegra (la muerte del festín): racimo
        /// BrumaFX.Cloud (masa que ocluye + rampa violácea) + aliento
        /// violeta. Coordenadas de MUNDO.
        /// </summary>
        public static void Bruma(Vector2 pos, float radio, float alfa, float tiempo, int semilla, Vector2 deriva = default)
        {
            if (alfa <= 0.02f || radio <= 1f) return;
            if (Main.netMode == NetmodeID.Server) return;

            Vector2 o = Main.screenPosition;
            LoteDeBruma(() =>
            {
                BrumaFX.Cloud(pos - o, radio, HumoNegro, semilla,
                    tiempo, Math.Max(3, (int)(radio / 30f)), alpha: 0.62f * alfa);
                BrumaFX.Cloud(pos - o + deriva * 0.5f, radio * 0.66f, HumoVioleta, semilla + 31,
                    tiempo * 0.85f, 3, alpha: 0.30f * alfa);
            });

            // la lectura nocturna: el aliento violeta
            VFXCore.Begin();
            for (int k = 0; k < 4; k++)
                AlientoVioleta(pos, radio * 0.55f, alfa, tiempo, semilla + 5, k);
            VFXCore.FlushAdditive();
        }

        // ==================================================================
        //  v6.50.65 — LA BRUMA NEGRA CONTINUA (BrumaFX) + LAS GARRAS
        // ==================================================================

        /// <summary>
        /// LA BRUMA VIVA DEL CUERPO: puﬀs de humo fBm (flipbook que se
        /// DESGARRA de verdad) que nacen PEGADOS a la masa del tentáculo,
        /// crecen ×1.5, derivan hacia fuera y hacia arriba y se enfrían
        /// de negro a violeta-gris al morir — la exhalación continua de
        /// la sombra. Cada puﬀ ancla a un punto de la columna (reparto
        /// áureo), su vida es CÍCLICA y todo determinista por semilla.
        /// Funciona con CUALQUIER polilínea — tentáculo, rastro o anillo.
        /// </summary>
        /// <param name="col">La polilínea que respira (columna, rastro, anillo).</param>
        /// <param name="grosor">Grosor de la masa que la exhala (escala los puﬀs).</param>
        /// <param name="alfa">0..1 — cuánta bruma hay.</param>
        public static void BrumaColumna(Vector2[] col, float grosor, float alfa, float tiempo, int semilla, int cantidad = 9)
        {
            if (alfa <= 0.02f || col == null || col.Length < 3) return;
            if (Main.netMode == NetmodeID.Server) return;
            int n = col.Length;
            Vector2 o = Main.screenPosition;

            LoteDeBruma(() =>
            {
                for (int k = 0; k < cantidad; k++)
                {
                    float f0 = Frac(semille(semilla) * 0.83f + k * 0.618034f);  // el ancla en la columna
                    float ritmo = 0.10f + 0.09f * Frac(f0 * 9.7f);              // cada puﬀ respira a su ritmo
                    float edad = Frac(tiempo * ritmo + f0 * 4.1f + k * 0.233f); // vida cíclica 0→1
                    int idx = (int)(f0 * (n - 1));
                    // la dirección local de la columna (para derivar DE LADO)
                    Vector2 seg = col[Math.Min(idx + 1, n - 1)] - col[Math.Max(idx - 1, 0)];
                    Vector2 perp = seg.LengthSquared() < 0.01f ? Vector2.UnitY
                        : new Vector2(-seg.Y, seg.X) * (1f / MathF.Sqrt(seg.LengthSquared()));
                    float lado = Frac(f0 * 13.7f) > 0.5f ? 1f : -1f;
                    float deriva = edad * (30f + 44f * Frac(f0 * 5.9f));
                    Vector2 pos = col[idx]
                        + perp * (lado * (grosor * 0.32f + deriva * 0.5f))
                        + new Vector2(0f, -20f * edad);                          // sube, como humo frío
                    // v6.50.69 — MÁS BRUMA (la letra: «la cantidad de bruma en
                    // el tentáculo no es suficiente, necesita más»): el puff
                    // nace más gordo y CRECE hasta ×1.6 (era ×1.35)
                    float r = grosor * (0.58f + 1.02f * edad);                    // nace chico, crece ×1.6
                    // LA ENVOLVENTE y LA RAMPA (a mano — BrumaFX.Puff expone quality):
                    float a = alfa * MathF.Sin(edad * MathF.PI) * (0.82f + 0.18f * Frac(f0 * 7.3f));
                    Color c = edad > 0.55f
                        ? Color.Lerp(HumoNegro, HumoVioleta, (edad - 0.55f) / 0.45f)
                        : HumoNegro;
                    BrumaFX.Puff(pos - o, r, c, semilla * 31 + k, tiempo + f0 * 7f,
                        MathHelper.Clamp(a, 0.04f, 0.88f), quality: 0.55f,
                        velocity: perp * (lado * deriva * 0.4f));
                }
            });

            // el aliento violeta entre puffs (lectura nocturna) — v6.50.69:
            // hasta 7 halos (eran 5: MÁS BRUMA)
            VFXCore.Begin();
            for (int k = 0; k < Math.Min(cantidad + 2, 7); k++)
            {
                float f0 = Frac(semille(semilla) * 0.83f + k * 0.618034f);
                int idx = (int)(f0 * (n - 1));
                AlientoVioleta(col[idx], grosor * 0.9f, alfa, tiempo, semilla + k * 3, k);
            }
            VFXCore.FlushAdditive();
        }

        /// <summary>
        /// EL ALIENTO DE LA BOCA: la bruma que EXHALA una fauce abierta —
        /// sale de la garganta hacia <paramref name="dir"/> en abanico,
        /// crece y se enfría violáceo al disolverse. Gateada por
        /// <paramref name="apertura"/>: la boca cerrada no respira.
        /// </summary>
        public static void BrumaBoca(Vector2 garganta, Vector2 dir, float apertura, float alfa, float tiempo, int semilla, int puffs = 5)
        {
            if (alfa <= 0.02f || apertura <= 0.1f) return;
            if (Main.netMode == NetmodeID.Server) return;
            Vector2 d = dir.LengthSquared() < 0.01f ? Vector2.UnitX : Vector2.Normalize(dir);
            Vector2 perp = new(-d.Y, d.X);
            Vector2 o = Main.screenPosition;

            LoteDeBruma(() =>
            {
                for (int k = 0; k < puffs; k++)
                {
                    float f0 = Frac(semille(semilla) * 0.47f + k * 0.618034f);
                    float edad = Frac(tiempo * (0.16f + 0.10f * f0) + f0 * 5.3f);
                    float alcance = (18f + 56f * f0) * (0.35f + 0.65f * apertura);
                    float lado = (Frac(f0 * 11.3f) > 0.5f ? 1f : -1f) * (0.3f + 0.7f * edad);
                    Vector2 pos = garganta + d * (alcance * (0.3f + 0.7f * edad))
                                + perp * (lado * alcance * 0.4f)
                                + new Vector2(0f, -10f * edad);
                    float r = (18f + 26f * f0) * (0.55f + 0.8f * edad) * (0.4f + 0.6f * apertura);
                    float a = alfa * apertura * MathF.Sin(edad * MathF.PI) * 0.85f;
                    Color c = edad > 0.55f
                        ? Color.Lerp(HumoNegro, HumoVioleta, (edad - 0.55f) / 0.45f)
                        : HumoNegro;
                    BrumaFX.Puff(pos - o, r, c, semilla * 17 + k * 3, tiempo + f0 * 9f,
                        MathHelper.Clamp(a, 0.04f, 0.8f), quality: 0.5f,
                        velocity: d * (alcance * 0.25f));
                }
            });

            // la lectura nocturna del aliento
            VFXCore.Begin();
            for (int k = 0; k < 3; k++)
                AlientoVioleta(garganta + d * (30f + k * 26f) * apertura, 26f * apertura,
                    alfa * apertura, tiempo, semilla + k * 5, k);
            VFXCore.FlushAdditive();
        }

        // ==================================================================
        //  v6.50.68 — LA CABEZA DE BRUMA + EL DEVORADOR (la letra del
        //  usuario: «la boca deja mucho que desear… la boca y el
        //  tentáculo deben ser uno solo… en vez de boca una masa de
        //  bruma negra cubre la totalidad de un jefe y esta bruma negra
        //  y espesa nace del tentáculo hacia el jefe»).
        // ==================================================================

        /// <summary>
        /// LA CABEZA DE BRUMA — la boca procedural RETIRADA: la punta del
        /// tentáculo NO termina en mandíbulas dibujadas aparte (que se
        /// despegaban del cuerpo): termina DISOLVIÉNDOSE en una cabeza de
        /// humo que respira hacia <paramref name="dir"/>. Como se dibuja
        /// EN la punta real de la columna (la pasa el llamador), cabeza y
        /// tentáculo son EL MISMO CUERPO por construcción — jamás puede
        /// haber separación. Mientras caza: una masa mediana que abre el
        /// rumbo; al emerger: el capullo que estalla.
        /// </summary>
        /// <param name="punta">LA PUNTA REAL de la columna (col[n−1]).</param>
        /// <param name="dir">Rumbo de caza (la tangente final de la columna).</param>
        /// <param name="vigor">0..1 — cuánta cabeza hay (crece al emerger, late al cazar).</param>
        public static void CabezaDeBruma(Vector2 punta, Vector2 dir, float vigor, float alfa, float tiempo, int semilla)
        {
            if (alfa <= 0.02f || vigor <= 0.03f) return;
            if (Main.netMode == NetmodeID.Server) return;
            vigor = MathHelper.Clamp(vigor, 0f, 1f);

            Vector2 o = Main.screenPosition;
            // LA MASA DE LA CABEZA: un racimo denso EN la punta — el
            // humo que REEMPLAZA la boca: nace pegado, crece y respira.
            // v6.50.69 — MÁS BRUMA: el racimo principal CRECE y gana DOS
            // PUFFS ORBITANDO la punta (la cabeza es una NUBE, no una bola)
            LoteDeBruma(() =>
            {
                float late = 0.85f + 0.15f * MathF.Sin(tiempo * 3.1f + semille(semilla));
                float r = (36f + 28f * vigor) * late;
                BrumaFX.Puff(punta - o, r, HumoNegro, semilla * 13 + 1, tiempo,
                    MathHelper.Clamp(0.60f * alfa * vigor, 0.05f, 0.88f), quality: 0.6f);
                BrumaFX.Puff(punta - o + new Vector2(MathF.Sin(tiempo * 1.7f + semille(semilla + 3)) * 10f,
                    -6f - 4f * MathF.Sin(tiempo * 2.2f)), r * 0.7f, HumoVioleta,
                    semilla * 13 + 2, tiempo * 0.9f,
                    MathHelper.Clamp(0.32f * alfa * vigor, 0.04f, 0.5f), quality: 0.5f);
                // LOS ORBITANTES: dos puffs menores girando alrededor de la
                // punta (la cabeza VIVA — nace, gira y se deshace)
                for (int k = 0; k < 2; k++)
                {
                    float ang = tiempo * (1.35f + 0.4f * k) + k * MathHelper.Pi + semille(semilla + 7) * 6.28f;
                    float rad = r * (0.62f + 0.14f * MathF.Sin(tiempo * 2.4f + k));
                    BrumaFX.Puff(punta - o + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.82f) * rad,
                        r * 0.52f, HumoNegro, semilla * 13 + 3 + k, tiempo * 1.1f + k,
                        MathHelper.Clamp(0.34f * alfa * vigor, 0.04f, 0.55f), quality: 0.45f);
                }
            });

            // el aliento hacia adelante (la nariz del cazador)
            BrumaBoca(punta, dir, vigor, alfa, tiempo, semilla + 5, 4);
        }

        /// <summary>
        /// EL DEVORADOR v6.50.69 — LA COBERTURA TOTAL + EL TRAGO.
        ///
        /// La letra del usuario (.68): «en vez de boca, una masa de bruma
        /// negra que CUBRE LA TOTALIDAD de un jefe, y esa bruma negra y
        /// espesa NACE del tentáculo hacia el jefe». Y la de la .69: «la
        /// bruma que se traga al jefe no cubre realmente al jefe, solo
        /// cubre una parte… la bruma debe cubrir a TODO el jefe, por
        /// ejemplo al salir el rey slime la bruma no lo cubre completo».
        ///
        /// LA CAUSA de la cobertura parcial: la .68 usaba un RADIO CIRCULAR
        /// (jefe.Size.Length()*0.62, tope 260) — un círculo sobre un jefe
        /// ALTO (King Slime ~200×250) deja la corona y los pies AL AIRE.
        /// LA CURA: LA ELIPSE REAL DEL JEFE — rx/ry de su hitbox + 24% de
        /// margen, y encima una REJILLA DE PUFFS que la rellena entera
        /// (2-7 columnas × 2-8 filas según tamaño, jitter determinista:
        /// humo, jamás una parrilla) sobre EL VELO (un GlowOrb ESTIRADO a
        /// la elipse — la base casi opaca que NADA atraviesa).
        ///
        /// EL TRAGO (<paramref name="trago"/>): 0..1 — el festín pasa el
        /// parámetro conforme INHALA: la elipse entera SE ENCOGE hacia la
        /// punta del tentáculo, los puffs MIGRAN hacia ella muriendo en
        /// volutas (el jefe siendo succionado por el cuerpo) y el velo se
        /// disuelve. Es LA ANIMACIÓN QUE REEMPLAZA la muerte del jefe (la
        /// letra de la .69: «toda esa bruma que se traga al jefe debe
        /// animarse para que sustituya cualquier animación»).
        ///
        /// Capas: (0) EL VELO elíptico · (A) EL PUENTE punta→jefe ·
        /// (B) LA REJILLA que cubre TODO · (C) EL TRAGADO rítmico (pulso
        /// 4,6 Hz) + LAS ALMAS volviendo al portador POR la columna ·
        /// (D) EL BORDE violeta/rojo — la lectura nocturna.
        /// </summary>
        /// <param name="punta">LA PUNTA REAL de la columna (col[n−1]).</param>
        /// <param name="jefe">La presa mordida (su hitbox manda en la elipse).</param>
        /// <param name="intensidad">0..1 — cuánta masa hay (crece al morder).</param>
        /// <param name="col">La columna del tentáculo (por donde vuelan las almas). Opcional.</param>
        /// <param name="trago">0..1 — el avance del tragado (0 = cubierto, 1 = inhalado).</param>
        public static void Devorador(Vector2 punta, NPC jefe, float intensidad, float alfa, float tiempo, int semilla, Vector2[] col = null, float trago = 0f)
        {
            if (alfa <= 0.02f || intensidad <= 0.02f || jefe == null || !jefe.active) return;
            if (Main.netMode == NetmodeID.Server) return;
            intensidad = MathHelper.Clamp(intensidad, 0f, 1f);
            trago = MathHelper.Clamp(trago, 0f, 1f);

            Vector2 centro = jefe.Center;
            // === LA ELIPSE REAL DEL JEFE (la cura del Rey Slime a medias):
            // semianchos del hitbox + 24% de margen — CUBRE LA CORONA y
            // los pies, sea alto, ancho o cuadrado ===
            float rx = MathHelper.Clamp(jefe.width * 0.62f, 76f, 330f);
            float ry = MathHelper.Clamp(jefe.height * 0.62f, 76f, 350f);

            // (C) EL TRAGADO: la masa se CIÑE con cada bocanada (4,6 Hz) y
            // TODO el conjunto ENCOGE hacia la punta conforme trago→1
            float mordisco = 1f - 0.07f * (0.5f + 0.5f * MathF.Sin(tiempo * 4.6f + semille(semilla))) * intensidad;
            float ciñe = mordisco * (1f - 0.72f * trago);
            Vector2 o = Main.screenPosition;
            Vector2 sorbe = Vector2.Lerp(centro, punta, trago);   // el embudo del tragado

            // === (0) EL VELO — la base que NADA atraviesa: un GlowOrb
            // ESTIRADO a la ELIPSE completa (la .68 era un círculo: la
            // corona del Rey Slime quedaba fuera) ===
            VFXCore.Begin();
            VFXCore.Quad(sorbe, Alfa(Negro, 0.88f * intensidad * (1f - 0.55f * trago)),
                new Vector2(rx * 2.28f * ciñe, ry * 2.28f * ciñe), 0f, VFXCore.GlowOrb);
            VFXCore.FlushAlpha();

            LoteDeBruma(() =>
            {
                // (A) EL PUENTE — del tentáculo al jefe: la bruma NACE de
                // la punta (puffs alineados, radios hasta la elipse)
                Vector2 eje = centro - punta;
                float len = eje.Length();
                if (len > 24f)
                {
                    Vector2 dir = eje * (1f / len);
                    int pasos = Math.Clamp((int)(len / 42f), 2, 6);
                    for (int k = 0; k <= pasos; k++)
                    {
                        float f = k / (float)pasos;
                        float r = MathHelper.Lerp(24f, MathF.Min(rx, ry) * 0.85f, f) * mordisco;
                        BrumaFX.Puff((punta + dir * (len * f)) - o, r, HumoNegro,
                            semilla * 7 + k, tiempo + f * 3f,
                            MathHelper.Clamp(0.52f * alfa * intensidad, 0.05f, 0.8f), quality: 0.5f,
                            velocity: dir * 12f);
                    }
                }

                // (B) LA REJILLA DE LA CUBIERTA TOTAL — puffs que rellenan
                // LA ELIPSE ENTERA del jefe: nace del tamaño REAL (más
                // rejilla = jefes más grandes), jitter determinista para
                // que sea humo y no una parrilla, y TODO ciñe/migra con el
                // tragado (los del borde mueren primero: la nube se cierra
                // sobre la boca)
                float rPuff = MathHelper.Clamp(MathF.Min(rx, ry) * 0.66f, 48f, 130f);
                int nx = Math.Clamp((int)MathF.Ceiling(2f * rx / (rPuff * 1.04f)), 2, 7);
                int ny = Math.Clamp((int)MathF.Ceiling(2f * ry / (rPuff * 1.04f)), 2, 8);
                for (int gy = 0; gy < ny; gy++)
                {
                    for (int gx = 0; gx < nx; gx++)
                    {
                        float fx = nx == 1 ? 0.5f : gx / (float)(nx - 1);
                        float fy = ny == 1 ? 0.5f : gy / (float)(ny - 1);
                        // el jitter de humo (determinista por semilla+celda)
                        float jx = (Frac(semille(semilla + gx * 31 + gy * 7) * 3.7f) - 0.5f) * rPuff * 0.6f;
                        float jy = (Frac(semille(semilla + gy * 13 + gx * 3) * 5.9f) - 0.5f) * rPuff * 0.6f;
                        Vector2 local = new((fx - 0.5f) * 2f * rx + jx, (fy - 0.5f) * 2f * ry + jy);
                        // fuera de la elipse (+8%): se RECOGE hacia dentro
                        float ex = local.X / (rx * 1.08f), ey = local.Y / (ry * 1.08f);
                        float dE = ex * ex + ey * ey;
                        if (dE > 1f) local *= 0.80f;

                        // la respiración de la masa (cada celda a su ritmo)
                        float respira = 1f + 0.06f * MathF.Sin(tiempo * 3.2f + gx * 1.7f + gy * 2.3f);
                        Vector2 pos = centro + local * respira * ciñe;
                        // LA MIGRACIÓN DEL TRAGO: cada puff corre hacia el
                        // embudo (los del borde PRIMERO — la nube se cierra)
                        pos = Vector2.Lerp(centro + local * respira * ciñe, sorbe, trago * (0.45f + 0.55f * MathHelper.Clamp(dE, 0.1f, 1f)));

                        float r = rPuff * (0.86f + 0.30f * Frac(semille(semilla + gx * 5 + gy * 11) * 2.3f))
                                * (1f - 0.42f * trago);
                        // el alfa: nace con la intensidad, muere con el trago
                        float a = alfa * intensidad
                                * MathHelper.Clamp(1.15f - 0.25f * dE, 0.55f, 1f)
                                * (1f - trago * (0.35f + 0.55f * MathHelper.Clamp(dE, 0f, 1f)));
                        // LA RAMPA de enfriamiento (la firma de la casa)
                        Color c = dE > 0.55f || trago > 0.5f
                            ? Color.Lerp(HumoNegro, HumoVioleta, MathHelper.Clamp((dE - 0.55f) / 0.45f + trago * 0.4f, 0f, 1f))
                            : HumoNegro;
                        BrumaFX.Puff(pos - o, r, c, semilla * 17 + gx * 3 + gy * 5, tiempo + fx * 4f + fy * 2f,
                            MathHelper.Clamp(a, 0.04f, 0.88f), quality: 0.5f,
                            velocity: (sorbe - pos) * 0.045f);
                    }
                }

                // EL NÚCLEO VIOLETA — el centro aún RESPIRA (la digestión)
                if (trago < 0.75f)
                    BrumaFX.Cloud(sorbe - o, MathF.Min(rx, ry) * 0.5f * ciñe, HumoVioleta, semilla * 17 + 6, tiempo * 0.9f,
                        4, alpha: MathHelper.Clamp(0.34f * alfa * intensidad * (1f - trago), 0.04f, 0.5f));
            });

            // (D) EL BORDE violeta/rojo — la masa PRENDE en la noche (detrás,
            // en aditivo tenue: no tapa, HACE LEER) — ahora ELÍPTICO
            VFXCore.Begin();
            float late = 0.75f + 0.25f * MathF.Sin(tiempo * 3.8f + semille(semilla));
            VFXCore.Quad(sorbe, Alfa(Violeta, 0.10f * alfa * intensidad * late),
                new Vector2(rx * 2.5f * ciñe, ry * 2.5f * ciñe), 0f);
            VFXCore.Quad(sorbe, Alfa(Rojo, 0.07f * alfa * intensidad * late),
                new Vector2(rx * 1.5f * ciñe, ry * 1.5f * ciñe), 0f);
            VFXCore.FlushAdditive();

            // (C) LAS ALMAS — la vida del jefe vuelve al portador POR el
            // cuerpo del tentáculo (la columna es el camino); durante el
            // trago son un RÍO (el festín se lleva todo)
            if (col != null && col.Length >= 3)
            {
                VFXCore.Begin();
                int nAlmas = 4 + (int)(4f * trago);
                for (int k = 0; k < nAlmas; k++)
                {
                    float prog = Frac(tiempo * (0.42f + 0.30f * trago) + k * (1f / nAlmas) + semille(semilla + k) * 0.2f);
                    float idxF = (1f - prog) * (col.Length - 1);
                    int idx = (int)idxF;
                    float resto = Frac(idxF);
                    if (idx >= col.Length - 1) { idx = col.Length - 2; resto = 1f; }
                    if (idx < 0) idx = 0;
                    Vector2 pos = Vector2.Lerp(col[idx], col[idx + 1], resto)
                        + new Vector2(MathF.Sin(prog * 9f + k * 2.4f) * 14f,
                                      MathF.Cos(prog * 7f + k * 1.7f) * 10f);
                    Alma(pos, 12f + 5f * MathF.Sin(tiempo * 5f + k), (0.8f + 0.2f * trago) * alfa * intensidad);
                }
                VFXCore.FlushAdditive();
            }
        }

        /// <summary>
        /// v6.50.69 — LA PURGA DE LA MUERTE VANILLA (la letra del usuario:
        /// «no se activa la opción de muerte predeterminada del jefe…
        /// lo que sucede es el arma devora al jefe, quedando solo su
        /// loot»). Cuando el motor del festín ejecuta la muerte REAL (la
        /// 2ª pasada de CheckDead — la que suelta el loot, los logros y
        /// las flags), vanilla también escupe su GORE y su polvo: esta
        /// purga los DESACTIVA en la zona del tragado mientras la bruma
        /// aún la ocupa — el jefe se lo lleva el libro ENTERO, sin un
        /// hueso fuera de la nube. Los ITEMS no se tocan (el loot cae
        /// dentro de la bruma, como manda la letra). Solo cliente.
        /// </summary>
        /// <param name="centro">El último centro conocido del jefe.</param>
        /// <param name="radio">El radio de la purga (gore completo; el polvo solo en el núcleo).</param>
        public static void PurgaVisualMuerte(Vector2 centro, float radio)
        {
            if (Main.netMode == NetmodeID.Server) return;
            float r2 = radio * radio;
            for (int i = 0; i < Main.gore.Length; i++)
            {
                Gore g = Main.gore[i];
                if (g == null || !g.active) continue;
                if (Vector2.DistanceSquared(g.position, centro) < r2) g.active = false;
            }
            float rNucleo = radio * 0.55f;
            float r2n = rNucleo * rNucleo;
            for (int i = 0; i < Main.dust.Length; i++)
            {
                Dust d = Main.dust[i];
                if (d == null || !d.active) continue;
                if (d.type == DustID.Shadowflame || d.type == DustID.Smoke) continue;  // los nuestros viven
                if (Vector2.DistanceSquared(d.position, centro) < r2n) d.active = false;
            }
        }

        /// <summary>
        /// EL VELO DE LA SALA (la anticipación de cine): oscurece los
        /// CUATRO BORDES con doble velo (uno ancho y denso + otro corto y
        /// profundo) que RESPIRA lento — la penumbra de Pride hecha sala
        /// de proyección. v6.50.65: ×2 capas + pulso — antes era un velo
        /// tan tenue que nadie lo veía.
        /// </summary>
        public static void Vignette(float alfa)
        {
            if (alfa <= 0.02f) return;
            if (Main.netMode == NetmodeID.Server) return;
            float w = Main.screenWidth, h = Main.screenHeight;
            Vector2 o = Main.screenPosition;
            float pulso = 0.90f + 0.10f * MathF.Sin(Main.GlobalTimeWrappedHourly * 1.7f);

            // CAPA 1 — el velo ancho (cubre media pantalla hacia adentro)
            Color c1 = Alfa(Negro, alfa * 0.62f * pulso);
            VFXCore.Begin();
            VFXCore.Quad(new Vector2(o.X - w * 0.34f, o.Y + h * 0.5f), c1, new Vector2(w * 1.1f, h * 2.4f), 0f, VFXCore.SoftGlow);
            VFXCore.Quad(new Vector2(o.X + w * 1.34f, o.Y + h * 0.5f), c1, new Vector2(w * 1.1f, h * 2.4f), 0f, VFXCore.SoftGlow);
            VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y - h * 0.36f), c1, new Vector2(w * 2.4f, h * 1.1f), 0f, VFXCore.SoftGlow);
            VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y + h * 1.36f), c1, new Vector2(w * 2.4f, h * 1.1f), 0f, VFXCore.SoftGlow);
            VFXCore.FlushAlpha();

            // CAPA 2 — el velo profundo de la esquina (más corto, más negro)
            Color c2 = Alfa(Negro, alfa * 0.80f * pulso);
            VFXCore.Begin();
            VFXCore.Quad(new Vector2(o.X - w * 0.22f, o.Y + h * 0.5f), c2, new Vector2(w * 0.8f, h * 1.9f), 0f, VFXCore.SoftGlow);
            VFXCore.Quad(new Vector2(o.X + w * 1.22f, o.Y + h * 0.5f), c2, new Vector2(w * 0.8f, h * 1.9f), 0f, VFXCore.SoftGlow);
            VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y - h * 0.24f), c2, new Vector2(w * 1.9f, h * 0.8f), 0f, VFXCore.SoftGlow);
            VFXCore.Quad(new Vector2(o.X + w * 0.5f, o.Y + h * 1.24f), c2, new Vector2(w * 1.9f, h * 0.8f), 0f, VFXCore.SoftGlow);
            VFXCore.FlushAlpha();
        }

        /// <summary>
        /// v6.50.67 — UN ZARPO DE VERDAD: la v6.50.66 eran «tres barras
        /// decrecientes + una línea blanca» — la «púa rectangular» que el
        /// usuario enterró dos versiones atrás, vuelta. AHORA: un TALÓN
        /// curvo (la mini-cinta de carne que nace gruesa y se afila en
        /// S, curvándose hacia <paramref name="gancho"/> — el crescente
        /// del zarpo), LA PUNTA DE HUESO (la aguja Colmillo alineada con
        /// la tangente final — blanca, afilada, brillando) y EL FILO
        /// rojo tenue (el borde que corta).
        /// </summary>
        /// <param name="basePos">La base gruesa de la garra (la más alejada del jefe).</param>
        /// <param name="hacia">Dirección de la punta (hacia el jefe).</param>
        /// <param name="largo">Largo total en px.</param>
        /// <param name="alfa">0..1.</param>
        /// <param name="gancho">−1..1 — cuánto y hacia dónde se curva.</param>
        public static void Garra(Vector2 basePos, Vector2 hacia, float largo, float alfa, float gancho = 0f)
        {
            if (largo <= 3f || alfa <= 0.02f) return;
            if (hacia.LengthSquared() < 0.01f) hacia = -Vector2.UnitY;
            Vector2 dir = Vector2.Normalize(hacia);
            Vector2 perp = new(-dir.Y, dir.X);
            var carne = VFXCore.Carne;

            // === LA CURVA DEL TALÓN: nace recta y se ENROLLA hacia el
            // gancho (el zarpo del cangrejo — S-taper, no tres barras) ===
            var pts = new Vector2[5];
            var anchos = new float[5];
            pts[0] = basePos;
            for (int k = 1; k < 5; k++)
            {
                float f = k / 4f;
                float desvio = gancho * (0.18f * f + 0.55f * f * f);   // el enrollado acelera
                Vector2 paso = dir + perp * desvio;
                paso.Normalize();
                pts[k] = pts[k - 1] + paso * (largo * 0.26f);
                anchos[k - 1] = largo * (0.24f * (1f - f * 0.72f));    // afila raíz→punta
            }
            anchos[4] = anchos[3] * 0.5f;

            if (carne != null)
            {
                // LA CARNE del talón (lote alfa)
                VFXCore.Begin();
                VFXCore.Ribbon(pts, anchos, Alfa(Blanco, alfa), carne, uvFlow: 0.6f);
                VFXCore.FlushAlpha();

                // EL FILO rojo tenue (aditivo — la lectura nocturna)
                var filo = new Color[5];
                for (int k = 0; k < 5; k++)
                    filo[k] = Alfa(k > 2 ? Rojo : Violeta, 0.13f * alfa);
                VFXCore.Begin();
                VFXCore.RibbonTinted(pts, anchos, filo, carne, uvFlow: 0.6f, edgeInset: 0.36f);
                VFXCore.FlushAdditive();
            }
            else
            {
                VFXCore.Begin();
                for (int k = 1; k < 5; k++)
                    VFXCore.Line(pts[k - 1], pts[k], Alfa(Negro, alfa), anchos[k - 1]);
                VFXCore.FlushAlpha(VFXCore.Pixel);
            }

            // === LA PUNTA DE HUESO — la aguja alineada con la tangente
            // final del talón (la parte que MATA es blanca) ===
            if (VFXCore.Colmillo != null)
            {
                Vector2 tang = pts[4] - pts[3];
                if (tang.LengthSquared() > 0.01f)
                {
                    tang.Normalize();
                    VFXCore.Begin();
                    VFXCore.Quad(pts[4] - tang * largo * 0.02f,
                        Alfa(Blanco, 0.88f * alfa),
                        new Vector2(largo * 0.085f, largo * 0.34f),
                        tang.ToRotation() + MathHelper.PiOver2, VFXCore.Colmillo);
                    VFXCore.FlushAdditive();
                }
            }
        }

        /// <summary>
        /// LA ONDA DE CHOQUE (v6.50.64 — el «juice» del impacto): anillo
        /// doble que se expande y muere — el CRUJIDO del golpe hecho
        /// imagen. En rojo visceral (la luz que mata la sombra) o en
        /// blanco de hueso.
        /// </summary>
        public static void OndaChoque(Vector2 pos, float radio, float alfa, bool roja = true)
        {
            if (alfa <= 0.02f || radio < 4f) return;
            VFXCore.Begin();
            Color c = roja ? Rojo : Blanco;
            VFXCore.Quad(pos, Alfa(c, alfa), VFXCore.RingQuadSize(radio), 0f, VFXCore.Ring);
            VFXCore.Quad(pos, Alfa(c, alfa * 0.45f), VFXCore.RingQuadSize(radio * 0.74f), 0f, VFXCore.Ring);
            VFXCore.FlushAdditive();
        }

        /// <summary>Un alma (partícula blanca pequeña, aditivo): lo que vuela de la presa al portador.</summary>
        public static void Alma(Vector2 pos, float tamaño, float alfa)
        {
            if (alfa <= 0.02f) return;
            VFXCore.Begin();
            VFXCore.Quad(pos, Alfa(Blanco, alfa * 0.75f), new Vector2(tamaño, tamaño));
            VFXCore.Quad(pos, Alfa(Blanco, alfa), new Vector2(tamaño * 0.4f, tamaño * 0.4f));
            VFXCore.FlushAdditive();
        }

        // ==================================================================
        //  PEQUEÑOS AJUSTADORES
        // ==================================================================

        /// <summary>Color con alpha escalada (Color·f de la casa).</summary>
        public static Color Alfa(Color c, float f)
        {
            f = MathHelper.Clamp(f, 0f, 1f);
            return new Color((byte)(c.R * f), (byte)(c.G * f), (byte)(c.B * f), (byte)(255f * f));
        }

        /// <summary>Hash determinista [0,1).</summary>
        public static float Frac(float x) => x - MathF.Floor(x);

        /// <summary>La semilla sana (entero → [0,1) determinista, nunca negativa).</summary>
        public static float semille(int s) => Frac(MathF.Sin(s * 12.9898f) * 43758.5453f);

        /// <summary>
        /// EL ACELERADOR DE APERTURA de los ojos/bocas de Pride: nada,
        /// nada… y DE GOLPE. Ease-out explosivo (k=4): 0→1 con casi todo
        /// el salto en el primer 25% del tiempo.
        /// </summary>
        public static float DeGolpe(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return 1f - MathF.Pow(1f - t, 4f);
        }
    }
}
