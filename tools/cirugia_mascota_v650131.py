#!/usr/bin/env python3
# v6.50.131 — CIRUGÍA DE LA MASCOTA (ParejaCerebro + GrimorioMenorPet):
#  (1) EL EVENTO DE LOS CONEJOS REALES MUERE (la letra: «el nuevo evento
#      del minigrimorio el de los conejos reales, quitalo, eliminar ese
#      evento») — constante, campos, struct, case, métodos, llamadas.
#  (2) EL CORO DE LAS HADAS = EL EVENTO PREDERMINADO (la letra: «deja el
#      evento de las hadas como evento predeterminado y que suceda de vez
#      en cuando») — la misma moneda de siempre (35 %, garantizada a la
#      3ª), SIN muro de carne de por medio.
#  (3) ANIMACIÓN NUEVA: LA PIRUETA DEL ARRANQUE — al emprender misión (o
#      la vuelta a casa) el libro da una vuelta de campana con chispas.
import io

RUTA = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Projectiles/Cosmetic/AethonMenorPareja.cs"
src = io.open(RUTA, encoding="utf-8").read()

def reemplazar(viejo, nuevo, veces=1):
    global src
    n = src.count(viejo)
    assert n == veces, f"MARCADOR {viejo[:70]!r}: esperado {veces}, hallado {n}"
    src = src.replace(viejo, nuevo)

def cortar(desde, hasta, extra=""):
    """Borra [desde, hasta) — ambos marcadores únicos; deja 'extra'."""
    global src
    a = src.index(desde)
    assert src.count(desde) == 1 and src.count(hasta) == 1, f"marcador duplicado: {desde[:50]!r} / {hasta[:50]!r}"
    b = src.index(hasta)
    assert a < b, "orden invertido"
    src = src[:a] + extra + src[b:]

# ------------------------------------------------------------------
# (1a) LA CONSTANTE MODO_CONEJOS MUERE
# ------------------------------------------------------------------
reemplazar(
"""        /// <summary>v6.50.130 — EL VERDADERO EVENTO DE LOS CONEJOS (la
        /// letra: «agreguemos un verdadero evento de conejos usando los
        /// conejos reales de terraria, pero estos serían más pequeños y
        /// estarían saltando siempre acercándose al minigrimorio mascota,
        /// este ataca y los espanta»): los conejos REALES de Terraria
        /// (el sprite de vanilla del Bunny, escala 0,55) saltando SIEMPRE
        /// hacia el libro — y el libro los espanta con sus DOS ATAQUES
        /// de siempre: los MINIMIONES (los tres ojos embisten) y LA
        /// SOMBRA DE LA PÁGINA (la sombrilla que muerde — el ataque
        /// original del Grimorio del Eterno real). Como el coro de las
        /// hadas: se disipa si el dueño se mueve. Puro teatro.</summary>
        public const float MODO_CONEJOS = 6f;
""",
"")

# ------------------------------------------------------------------
# (2a) MODO_PLAGA — doc nuevo: EL EVENTO PREDERMINADO
# ------------------------------------------------------------------
reemplazar(
"""        /// <summary>v6.50.126→.130 — nació como LA GUERRA DE LOS CONEJOS
        /// BLANCOS (Re:Zero); la letra de la .130 la bautizó de VERDAD:
        /// «no parecen conejos, parecen hadas… dejémoslo de esa forma,
        /// sería mejor que el evento sean HADAS DE COLORES y que se
        /// active después del muro de carne cuando las hadas estén
        /// activas». EL CORO DE LAS HADAS: haditas de colores (la MISMA
        /// silueta de siempre — ahora con su color propio) que acosan al
        /// libro quieto, tras el Muro de Carne. Puro teatro.</summary>
        public const float MODO_PLAGA = 4f;     // EL CORO DE LAS HADAS (hardmode)
""",
"""        /// <summary>v6.50.126→.131 — nació como LA GUERRA DE LOS CONEJOS
        /// BLANCOS (Re:Zero); la letra de la .130 la bautizó EL CORO DE
        /// LAS HADAS (hadas de COLORES, la misma silueta de siempre,
        /// cada una con su color); la letra de la .131 lo selló: «deja el
        /// evento de las hadas como evento PREDERMINADO y que suceda DE
        /// VEZ EN CUANDO» — el evento de los conejos reales murió y el
        /// coro reina solo, sin muro de carne de por medio. Hadas de
        /// colores que acosan al libro quieto. Puro teatro.</summary>
        public const float MODO_PLAGA = 4f;     // EL CORO DE LAS HADAS (predeterminado)
""")

# ------------------------------------------------------------------
# (1b) LAS CONSTANTES DE CONEJOS REALES MUEREN
# ------------------------------------------------------------------
cortar(
"        // === v6.50.130 — EL EVENTO DE LOS CONEJOS REALES (la letra: los",
"        // === v6.50.125 — LOS MINIONS DE VERDAD (la letra: «los")

# ------------------------------------------------------------------
# (1c) LOS CAMPOS DE ESTADO DE LA BANDADA MUEREN
# ------------------------------------------------------------------
cortar(
"            // === v6.50.130 — EL EVENTO DE LOS CONEJOS REALES ===",
"        }\n\n        /// <summary>UN CUERPO DE LA HORDA")

# ------------------------------------------------------------------
# (1d) EL STRUCT ConejoReal MUERE
# ------------------------------------------------------------------
cortar(
"        /// <summary>v6.50.130 — UN CONEJO REAL DE TERRARIA (la letra: los",
"        // === v6.50.130 — LA PALETA DEL CORO (hadas de COLORES: la letra")

# ------------------------------------------------------------------
# (1e) TICK — el abort por movimiento ya no conoce conejos
# ------------------------------------------------------------------
reemplazar(
"""                if (e.Modo == MODO_VIAJAR || e.Modo == MODO_MOLESTAR ||
                    e.Modo == MODO_PLAGA || e.Modo == MODO_CURIOSO ||
                    e.Modo == MODO_CONEJOS)
                {
                    if (e.Modo == MODO_PLAGA) DispersarPlaga(e);
                    if (e.Modo == MODO_CONEJOS) DispersarConejos(e);
""",
"""                if (e.Modo == MODO_VIAJAR || e.Modo == MODO_MOLESTAR ||
                    e.Modo == MODO_PLAGA || e.Modo == MODO_CURIOSO)
                {
                    if (e.Modo == MODO_PLAGA) DispersarPlaga(e);
""")

# ------------------------------------------------------------------
# (2b) LA MONEDA — el coro es EL evento (sin reparto)
# ------------------------------------------------------------------
reemplazar(
"""                                e.PlagaTiradasFallidas = 0;
                                // v6.50.130 — EL REPARTO DE LOS DOS TEATROS:
                                // tras el MURO DE CARNE reina EL CORO DE LAS
                                // HADAS (la letra: «se active después del
                                // muro de carne cuando las hadas estén
                                // activas» — 65 % de los eventos); los
                                // CONEJOS REALES son el teatro de SIEMPRE
                                // (el 100 % antes del muro, el 35 % después
                                // — las dos travesuras viven juntas).
                                bool coro = Main.hardMode && VFXCore.Hash01(p.whoAmI,
                                    e.TotalGolpes * 3 + 1, e.Viajes * 5 + 2) < 0.65f;
                                if (coro) ArrancarPlaga(p, e);
                                else ArrancarConejos(p, e);
                                break;
""",
"""                                e.PlagaTiradasFallidas = 0;
                                // v6.50.131 — EL CORO DE LAS HADAS ES EL
                                // EVENTO PREDERMINADO (la letra: «deja el
                                // evento de las hadas como evento
                                // predeterminado y que suceda de vez en
                                // cuando»): el evento de los conejos reales
                                // MURIÓ — la misma moneda de siempre (35 %
                                // y garantizada a la tercera), sin muro de
                                // carne de por medio.
                                ArrancarPlaga(p, e);
                                break;
""")

# ------------------------------------------------------------------
# (1f) EL CASE MODO_CONEJOS DEL TICK MUERE (con su break)
# ------------------------------------------------------------------
a = src.index("                case MODO_CONEJOS:")
mk = "                    break;\n            }\n        }"
b = src.index(mk) + len("                    break;\n")
assert a < b
src = src[:a] + src[b:]

# ------------------------------------------------------------------
# (1g) EL CASE MODO_CONEJOS DE ElegirMeta MUERE (con su break)
# ------------------------------------------------------------------
a = src.index("                case MODO_CONEJOS:")
fin = src.index("                    break;", a)
b = fin + len("                    break;\n")
src = src[:a] + src[b:]

# ------------------------------------------------------------------
# (1h) DibujarExtrasAlfa — la llamada a las sombras muere
# ------------------------------------------------------------------
reemplazar(
"""
            // v6.50.130 — LOS CONEJOS REALES: la sombra del piso de cada
            // uno + LA SOMBRILLA DE PÁGINA del ataque en curso (la sombra
            // que muerde — la FORMA 1 contra la bandada). El SPRITE real
            // del Bunny viaja en el lote vanilla con el libro
            // (DibujarLosConejos — la llama GrimorioMenorPet.PreDraw).
            if (e.Modo == MODO_CONEJOS) DibujarSombrasDeLosConejos(p, e);
""", "\n")

# ------------------------------------------------------------------
# (1i) PosicionOjos — la mirada a la bandada muere
# ------------------------------------------------------------------
reemplazar(
"""
            // v6.50.130 — CON LOS CONEJOS REALES: la mirada va a LA
            // BANDADA — el conejo vivo más cercano (y si hay ataque en
            // curso, AL BLANCO: los ojos del pastor nunca se distraen).
            if (e.Modo == MODO_CONEJOS)
            {
                if (e.ConejoObjetivo >= 0 && e.ConejoObjetivo < e.ConejosReales.Length &&
                    e.ConejosReales[e.ConejoObjetivo].Vivo)
                {
                    objetivo = e.ConejosReales[e.ConejoObjetivo].Pos + new Vector2(0f, -8f);
                }
                else
                {
                    float mejorD = float.MaxValue;
                    for (int c = 0; c < e.ConejosReales.Length; c++)
                    {
                        if (!e.ConejosReales[c].Vivo) continue;
                        float d = Vector2.DistanceSquared(e.ConejosReales[c].Pos, centro);
                        if (d < mejorD) { mejorD = d; objetivo = e.ConejosReales[c].Pos; }
                    }
                }
            }
""", "\n")

# ------------------------------------------------------------------
# (1j) PosicionOjos — la embestida contra conejos reales muere
# ------------------------------------------------------------------
a = src.index("            // v6.50.130 — LA EMBESTIDA CONTRA LOS CONEJOS REALES (la")
mk = "        // ==================================================================\n        //  v6.50.125 — LOS TRES OJOS DE VERDAD"
b = src.index(mk)
assert a < b
src = src[:a] + "        }\n\n" + src[b:]
# (el '}    ' que comimos era el cierre del método PosicionOjos: se repone)

# ------------------------------------------------------------------
# (1k) EL BLOQUE ENTERO DE MÉTODOS CONEJOS MUERE
# ------------------------------------------------------------------
cortar(
"        // ==================================================================\n        //  v6.50.130 — EL EVENTO DE LOS CONEJOS REALES (la bandada de",
"        // ==================================================================\n        //  LAS BÚSQUEDAS (vecinos y hermanos)")

# ------------------------------------------------------------------
# verificación del cerebro
# ------------------------------------------------------------------
for fantasma in ("MODO_CONEJOS", "ConejosReales", "ConejoReal", "ConejoEscala",
                 "DispersarConejos", "ArrancarConejos", "DibujarLosConejos",
                 "CONEJOS_MAX", "ConejosAtaque", "ConejoObjetivo", "ConejosEspantados",
                 "CentroDeLaBandada", "FisicaDelConejo", "DibujarSombrasDeLosConejos"):
    assert fantasma not in src, f"quedó rastro de {fantasma}"

io.open(RUTA, "w", encoding="utf-8").write(src)
print("OK — ParejaCerebro:", len(src.splitlines()), "líneas")

# ==================================================================
# GRIMORIOMENORPET — quitar la llamada muerta + LA PIRUETA NUEVA
# ==================================================================
RUTA2 = "/home/z/my-project/download/Aethon-Mod-Terraria/AethonMod/Content/Projectiles/Cosmetic/GrimorioMenorPet.cs"
s2 = io.open(RUTA2, encoding="utf-8").read()

def reemplazar2(viejo, nuevo, veces=1):
    global s2
    n = s2.count(viejo)
    assert n == veces, f"MARCADOR2 {viejo[:70]!r}: esperado {veces}, hallado {n}"
    s2 = s2.replace(viejo, nuevo)

# la llamada al dibujado de la bandada muere
reemplazar2(
"""
                // === 6. v6.50.130 — LOS CONEJOS REALES del evento (el
                //     sprite de vanilla del Bunny a escala 0,55, saltando
                //     hacia el libro — en el lote vanilla con el cuerpo y
                //     los ojos: la misma familia de capas). ===
                ParejaCerebro.DibujarLosConejos(Projectile, _cerebro);
""", "")

# la mirada curiosa ya no conoce el modo muerto
reemplazar2(
"""            else if (_cerebro.Modo == ParejaCerebro.MODO_CURIOSO ||
                     _cerebro.Modo == ParejaCerebro.MODO_CONEJOS ||
                     _cerebro.Modo == ParejaCerebro.MODO_PLAGA)
""",
"""            else if (_cerebro.Modo == ParejaCerebro.MODO_CURIOSO ||
                     _cerebro.Modo == ParejaCerebro.MODO_PLAGA)
""")

# LA PIRUETA DEL ARRANQUE (v6.50.131): al emprender misión o la vuelta,
# una vuelta de campana completa con chispas — el entusiasmo de las
# páginas al volar (localAI[1] = reloj de la pirueta, localAI[2] = modo
# anterior; ninguno viaja en el cable y no hace falta).
reemplazar2(
"""            // v6.50.130 — EL CINEO DEL VUELO (la letra: «mejora las
""",
"""            // v6.50.131 — LA PIRUETA DEL ARRANQUE: al emprender UNA
            // MISIÓN (o la vuelta a casa) el libro celebra con UNA VUELTA
            // de campana y un chorro de chispas — el entusiasmo de las
            // páginas al volar.
            int modoAhora = (int)_cerebro.Modo;
            if (Projectile.localAI[1] <= 0f &&
                modoAhora != (int)Projectile.localAI[2] &&
                (modoAhora == (int)ParejaCerebro.MODO_VIAJAR ||
                 modoAhora == (int)ParejaCerebro.MODO_VOLVER))
            {
                Projectile.localAI[1] = 34f;
                for (int i = 0; i < 7; i++)
                {
                    Dust chispa = Dust.NewDustPerfect(Projectile.Center,
                        DustID.Shadowflame,
                        Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.2f, 3.4f),
                        140, new Color(196, 150, 255), 0.7f);
                    chispa.noGravity = true;
                }
            }
            Projectile.localAI[2] = modoAhora;
            if (Projectile.localAI[1] > 0f) Projectile.localAI[1] -= 1f;
            float pirueta = Projectile.localAI[1] > 0f
                ? MathHelper.TwoPi * (1f - Projectile.localAI[1] / 34f)
                  * (Projectile.whoAmI % 2 == 0 ? 1f : -1f)
                : 0f;

            // v6.50.130 — EL CINEO DEL VUELO (la letra: «mejora las
""")

reemplazar2(
"""            Projectile.rotation = MathHelper.Clamp(Projectile.velocity.X * 0.020f, -0.24f, 0.24f)
                + MathF.Sin(t * 1.2f) * 0.05f;
""",
"""            Projectile.rotation = MathHelper.Clamp(Projectile.velocity.X * 0.020f, -0.24f, 0.24f)
                + MathF.Sin(t * 1.2f) * 0.05f + pirueta;
""")

assert "DibujarLosConejos" not in s2
assert "MODO_CONEJOS" not in s2
io.open(RUTA2, "w", encoding="utf-8").write(s2)
print("OK — GrimorioMenorPet:", len(s2.splitlines()), "líneas")
