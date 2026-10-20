#!/usr/bin/env python3
# v6.50.131 — CIRUGÍA DEL VAGABUNDO DEL SELECCIONADOR:
#  (1) UN SOLO ATAQUE: LA ANDANADA DE LOS MINIMIONES (la letra: «deben
#      tener un solo ataque, este es que ataque con los minimions usando
#      proyectiles como el vagabundo») — LA SOMBRA DE LA PÁGINA MUERE en
#      el menú (campos _pag*, DibujarLaPagina, golpe 1).
#  (2) ANIMACIONES NUEVAS: barrel roll orgánico en los rumbos, respiración
#      de escala, ESTELA DE BRUMA al volar rápido, ALETEO de páginas en las
#      pausas vivas, anticipación (inhala al desplegar ojos) y retroceso
#      del libro cuando la andanada dispara.
import io

RUTA = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Systems/SeleccionGrimorioSistema.cs"
src = io.open(RUTA, encoding="utf-8").read()

def reemplazar(viejo, nuevo, veces=1):
    global src
    n = src.count(viejo)
    assert n == veces, f"MARCADOR {viejo[:60]!r}: esperado {veces}, hallado {n}"
    src = src.replace(viejo, nuevo)

# ------------------------------------------------------------------
# (1a) EL HEADER — documentar el cambio de ataque
# ------------------------------------------------------------------
reemplazar(
"""    //  · EL TÍTULO SE ATACA CON LAS ARMAS: golpe 1 = LA SOMBRA DE LA
    //    PÁGINA (una página de tinta lo BARRE en diagonal, dejando sus
    //    manchas); golpes 2-3 = LA ANDANADA DE LOS MINIMIONES (los tres
    //    ojos de la corona se despliegan y DISPARAN virotes violetas que
    //    salpican el logo) — y las manchas de tinta siguen acumulándose.
""",
"""    //  · v6.50.131 — EL TÍTULO SE ATACA CON UN SOLO ATAQUE (la letra:
    //    «los ataques del minigrimorio y del vagabundo deben ser los
    //    mismos… un solo ataque: atacar con los minimions usando
    //    proyectiles como el vagabundo»): LA ANDANADA DE LOS MINIMIONES
    //    (los tres ojos de la corona se despliegan y DISPARAN virotes
    //    violetas que salpican el logo) — LA SOMBRA DE LA PÁGINA MURIÓ
    //    en el menú (vive como arma del Eterno, donde siempre perteneció).
""")

# ------------------------------------------------------------------
# (1b) LOS CAMPOS DE LA PÁGINA MUEREN; nacen los de las animaciones
# ------------------------------------------------------------------
reemplazar(
"""        // === v6.50.128 — LOS PUFFS DE BRUMA (la estela de la Sombra de la Página) ===
""",
"""        // === v6.50.131 — LOS PUFFS DE BRUMA (la estela de niebla del
        // vagabundo al volar rápido: las páginas respiran) ===
""")

reemplazar(
"""        // === v6.50.128 — LA SOMBRA DE LA PÁGINA (el barrido del golpe 1) ===
        private static Vector2 _pagDe, _pagHacia;
        private static float _pagAng;

        // === v6.50.128 — LAS POSICIONES DE LA CORONA durante la andanada ===
""",
"""        // === v6.50.128 — LAS POSICIONES DE LA CORONA durante la andanada ===
""")

reemplazar(
"""        // === v6.50.128 — EL RASTRO (las estelas fantasma del vuelo rápido) ===
        private static readonly Vector2[] _rastro = new Vector2[7];
        private static int _idxRastro;
""",
"""        // === v6.50.128 — EL RASTRO (las estelas fantasma del vuelo rápido) ===
        private static readonly Vector2[] _rastro = new Vector2[7];
        private static int _idxRastro;

        // === v6.50.131 — LAS ANIMACIONES VIVAS (la letra: «debes mejorar
        // las animaciones y movimientos del vagabundo») ===
        /// <summary>El BARREL ROLL: de tanto en cuando el vagabundo da una
        /// vuelta completa de campana en pleno vuelo (un giro extra que
        /// nace de golpe y se apaga solo — la travesura de las páginas).</summary>
        private static float _giroExtra;
        /// <summary>El ALETEO de páginas: una sacudida escamada durante
        /// las pausas vivas (se estira, suelta motas y vuelve a su tamaño).</summary>
        private static int _aleteo;
        /// <summary>El RETROCESO del libro cuando la andanada dispara.</summary>
        private static int _tRecoil;
""")

# ------------------------------------------------------------------
# (1c) REINICIAR — limpiar las animaciones nuevas también
# ------------------------------------------------------------------
reemplazar(
"""            _pausa = 0;
            _vel = Vector2.Zero;
            _posAnterior = _ancla;   // v6.50.129 — sin salto fantasma al renacer
""",
"""            _pausa = 0;
            _vel = Vector2.Zero;
            _giroExtra = 0f;         // v6.50.131 — animaciones a cero
            _aleteo = 0;
            _tRecoil = 0;
            _posAnterior = _ancla;   // v6.50.129 — sin salto fantasma al renacer
""")

# ------------------------------------------------------------------
# (1d) FASE_IR — la andanada es el PRIMER (y único) golpe
# ------------------------------------------------------------------
reemplazar(
"""                        _fase = FASE_ACTUAR;
                        _tFase = 0;
                        _tActo = 0;
                        _golpesLogo = 1;
""",
"""                        _fase = FASE_ACTUAR;
                        _tFase = 0;
                        _tActo = 0;
                        // v6.50.131 — LA ANDANADA ES EL PRIMER GOLPE (la
                        // Sombra de la Página murió en el menú: un solo
                        // ataque, el de los minimiones con proyectiles).
                        _golpesLogo = 2;
""")

# ------------------------------------------------------------------
# (1e) ACTUAR LOGO — borrar el bloque entero del golpe 1
# ------------------------------------------------------------------
i0 = src.index("            if (_golpesLogo == 1)")
i1 = src.index("                // ====== GOLPES 2-3 — LA ANDANADA DE LOS MINIMIONES ======")
src = (src[:i0]
       + """            {
                // ====== LA ANDANADA DE LOS MINIMIONES (v6.50.131 — EL
                // ÚNICO ATAQUE del vagabundo, la letra: «un solo ataque:
                // atacar con los minimions usando proyectiles») ======
"""
       + src[i1:])

# el doc del método
reemplazar(
"""        /// <summary>ATACAR EL TÍTULO CON LAS ARMAS DEL GRIMORIO (v6.50.128
        /// — la letra: «debe atacarlo con la sombra de la pagina o con el
        /// ataque de los minimion»): GOLPE 1 = LA SOMBRA DE LA PÁGINA barre
        /// el logo en diagonal dejando sus manchas; GOLPES 2-3 = los TRES
        /// OJOS de la corona se despliegan y DISPARAN virotes violetas.</summary>
""",
"""        /// <summary>ATACAR EL TÍTULO CON LOS MINIMIONES (v6.50.131 — la
        /// letra: «deben tener un solo ataque, este es que ataque con los
        /// minimions usando proyectiles como el vagabundo»): DOS ANDANADAS
        /// — los TRES OJOS de la corona se despliegan frente al logo y
        /// DISPARAN sus virotes violetas. La Sombra de la Página ya no
        /// vive en el menú (es arma del Eterno, no del vagabundo).</summary>
""")

# el disparo marca el retroceso del libro
reemplazar(
"""                    // EL RETROCESO del ojo que dispara.
                    _ojosActo[ojo] -= rumbo.SafeNormalize(Vector2.UnitX) * 3f;
""",
"""                    // EL RETROCESO del ojo que dispara… y del LIBRO que
                    // lo dirige (v6.50.131: el jadeo de la andanada).
                    _ojosActo[ojo] -= rumbo.SafeNormalize(Vector2.UnitX) * 3f;
                    _tRecoil = 7;
""")

# ------------------------------------------------------------------
# (2a) NUEVO RUMBO — a veces, barrel roll de camino
# ------------------------------------------------------------------
reemplazar(
"""        private static void NuevoRumbo()
        {
            _objetivo = new Vector2(
                Main.rand.NextFloat(50f, Main.screenWidth - 50f),
                Main.rand.NextFloat(60f, Main.screenHeight - 60f));
            _tFase = 0;
        }
""",
"""        private static void NuevoRumbo()
        {
            _objetivo = new Vector2(
                Main.rand.NextFloat(50f, Main.screenWidth - 50f),
                Main.rand.NextFloat(60f, Main.screenHeight - 60f));
            _tFase = 0;
            // v6.50.131 — EL BARREL ROLL: 1 de cada 6 rumbos, el vagabundo
            // viaja dando UNA VUELTA de campana (giro extra que nace
            // entero y se apaga exponencial: ~1,2 s de pirueta).
            if (Main.rand.Next(6) == 0)
                _giroExtra = Main.rand.NextBool() ? MathHelper.TwoPi : -MathHelper.TwoPi;
        }
""")

# ------------------------------------------------------------------
# (2b) LA PAUSA VIVA — a veces, un aleteo de páginas
# ------------------------------------------------------------------
reemplazar(
"""        private static void EmpezarPausa()
        {
            _pausa = Main.rand.Next(42, 108);
            _tMirada = 0;
            _tFase = 0;
        }
""",
"""        private static void EmpezarPausa()
        {
            _pausa = Main.rand.Next(42, 108);
            _tMirada = 0;
            _tFase = 0;
            // v6.50.131 — EL ALETEO: 1 de cada 3 pausas, el libro SACUDE
            // sus páginas (se hincha, suelta motas y se asienta).
            if (Main.rand.Next(3) == 0) _aleteo = 26;
        }
""")

# ------------------------------------------------------------------
# (2c) ACTUALIZAR — animar giroExtra/aleteo/recoil + estela de bruma
# ------------------------------------------------------------------
reemplazar(
"""            // v6.50.129 — LA VELOCIDAD VERDADERA: el desplazamiento REAL
""",
"""            // v6.50.131 — LAS ANIMACIONES VIVAS: el barrel roll se apaga
            // solo (exponencial), el aleteo cuenta sus 26 t y el retroceso
            // de la andanada decae.
            _giroExtra *= 0.94f;
            if (MathF.Abs(_giroExtra) < 0.015f) _giroExtra = 0f;
            if (_tRecoil > 0) _tRecoil--;
            if (_aleteo > 0)
            {
                _aleteo--;
                if (_aleteo == 13) Rafaga(_pos + new Vector2(0f, -8f), 3);
            }

            // v6.50.131 — LA ESTELA DE BRUMA: volando rápido, el vagabundo
            // gotea mechones de niebla de sus páginas (los puffs ya no son
            // de la Sombra de la Página: ahora son SU rastro de vuelo).
            if (_vel.LengthSquared() > 26f && (_tFase & 3) == 0)
                _puffs.Add(new Puff
                {
                    Pos = _pos + Main.rand.NextVector2Unit() * 7f,
                    Vel = -_vel * 0.14f + new Vector2(0f, 0.3f),
                    Vida = Main.rand.NextFloat(0.4f, 0.85f),
                    Tam = Main.rand.NextFloat(11f, 24f)
                });

            // v6.50.129 — LA VELOCIDAD VERDADERA: el desplazamiento REAL
""")

# ------------------------------------------------------------------
# (2d) DIBUJAR EL LIBRO — giro extra + respiración + anticipación
# ------------------------------------------------------------------
reemplazar(
"""            const float esc = 0.52f;
            float bob = MathF.Sin(t * 1.75f + 0.8f) * 2.4f;
            // EL CINEO: la inclinación nace de la VELOCIDAD (se recuesta
            // hacia donde vuela) + su vaivén perezoso de siempre.
            float giro = MathHelper.Clamp(_vel.X * 0.028f, -0.30f, 0.30f)
                + MathF.Sin(t * 1.2f) * 0.05f;
""",
"""            const float esc = 0.52f;
            float bob = MathF.Sin(t * 1.75f + 0.8f) * 2.4f;
            // EL CINEO: la inclinación nace de la VELOCIDAD (se recuesta
            // hacia donde vuela) + su vaivén perezoso de siempre + EL
            // BARREL ROLL (v6.50.131: la pirueta completa de camino).
            float giro = MathHelper.Clamp(_vel.X * 0.028f, -0.30f, 0.30f)
                + MathF.Sin(t * 1.2f) * 0.05f + _giroExtra;

            // v6.50.131 — LA RESPIRACIÓN Y EL TEATRO DE ESCALA: el libro
            // late ±2 %; el ALETEO de las pausas lo hincha en campana; la
            // andanada lo hace INHALAR al desplegar los ojos (anticipación)
            // y lo empuja ATRÁS en cada disparo (retroceso).
            float escViva = esc * (1f + 0.02f * MathF.Sin(t * 2.1f));
            if (_aleteo > 0)
                escViva *= 1f + 0.06f * MathF.Sin((26 - _aleteo) / 26f * MathF.Pi);
            if (_fase == FASE_ACTUAR && _acto == ACTO_LOGO && _golpesLogo >= 2 && _tActo < 30)
                escViva *= 1f + 0.04f * (_tActo / 30f);          // la inhalación
            if (_tRecoil > 0)
                escViva *= 1f - 0.025f * (_tRecoil / 7f);        // el jadeo
""")

# aplicar escViva en los usos de esc del cuerpo/ojo del libro
reemplazar(
"""            sb.Draw(tex, centro, frame, luz, giro, origen, esc, SpriteEffects.None, 0f);

            // EL OJO VIVO (mira hacia el FOCO de la curiosidad).
""",
"""            sb.Draw(tex, centro, frame, luz, giro, origen, escViva, SpriteEffects.None, 0f);

            // EL OJO VIVO (mira hacia el FOCO de la curiosidad).
""")

reemplazar(
"""                sb.Draw(parpado, centro, frame, luz, giro, origen, esc, SpriteEffects.None, 0f);
""",
"""                sb.Draw(parpado, centro, frame, luz, giro, origen, escViva, SpriteEffects.None, 0f);
""")

reemplazar(
"""                Vector2 alOjo = (global::AethonMod.Content.Weapons.GrimorioHambrientoNervioso.OjoTexel
                    + mirada - origen) * esc;
""",
"""                Vector2 alOjo = (global::AethonMod.Content.Weapons.GrimorioHambrientoNervioso.OjoTexel
                    + mirada - origen) * escViva;
""")

# ------------------------------------------------------------------
# (1f) DIBUJAR LA PÁGINA MUERE (llamada + método entero)
# ------------------------------------------------------------------
reemplazar(
"""                DibujarManchas(_lote);
                DibujarElLibro(_lote);
                DibujarLaPagina(_lote);
""",
"""                DibujarManchas(_lote);
                DibujarElLibro(_lote);
""")

i0 = src.index("        /// <summary>LA SOMBRA DE LA PÁGINA (v6.50.128 — el golpe 1 del")
i1 = src.index("        /// <summary>LA BRASA del vagabundo")
src = src[:i0] + src[i1:]

# ------------------------------------------------------------------
# verificaciones finales
# ------------------------------------------------------------------
assert "DibujarLaPagina" not in src, "quedó una referencia a la página muerta"
assert "_pagDe" not in src and "_pagHacia" not in src and "_pagAng" not in src, "quedaron campos de la página"
assert src.count("_golpesLogo = 2;") == 1
assert "_giroExtra" in src and "_aleteo" in src and "_tRecoil" in src

io.open(RUTA, "w", encoding="utf-8").write(src)
print("OK — cirugía del vagabundo aplicada:", len(src.splitlines()), "líneas")
