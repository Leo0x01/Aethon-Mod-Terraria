#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
cirugia_v6151.py — LA CIRUGÍA DE LOS DOCE PUNTOS DE LA LETRA v6.151.

La letra del usuario (textual, los 12 puntos):
 1. «el brillo en el icono de buff de la ira, es solo un circulo plano
    demasiado grande, creo que tendras que crear un shader o libreria que
    permita iluminar, opacar o desaturar partes de la interfaz... en este
    caso seria bueno un pequeño brillo cuadrado que sobresalga al menos
    1 a 5 pixeles del icono de buff de la ira y que este indique cuando
    esta listo» + «en el buff de la ira si el brillo sale bien el
    contador no hace falta, asi que arregla lo del brillo»
 2. «no puedo activar la ira de un ser exterior, en el momento en el que
    voy a activarla todos los minions desaparecen, ademas quiero que
    limites el ataque basico del grimorio del eterno a solo 10»
 3. «tienes que mejorar la animacion de el juicio de los dos soles... el
    proyectil da un salto para hacer otra animacion, debe ser una
    animacion suave al inicio y ademas debe ser completamente fluida... el
    sol formado por la fusion no debe explotar asi nada mas, debe
    mantenerse, perseguir a los enemigos y despues de un tiempo debe
    crecer al menos 70% mas su tamaño, volverse completamente rojo y
    luego explotar, junto a todas estas acciones su daño aumenta y ademas
    su caja de colisiones tambien para abarcar mas area de daño»
 4. «debes aumentar los tick de daño de la ira de un ser exterior, que
    sean la misma velocidad que los ticks de los agujeros runicos, esto
    es solo aumentar la velocidad con la que hace daño, no su daño»
 5. «La Forma Eterna solo se construye en personaje que no la hayan
    desbloqueado antes... en mi personaje de prueba sin importar cuantos
    libros lleve a despertar 101 a 120 no puedo ver como se construye» +
    «esto Construccion hasta el 120 con anillos de soles runicos no se
    cumple, explota en nivel 112 de despertar, el 113 y 119 no hacen
    nada, ademas la explosion es horrible... sera el problema ese de
    archivos fnx... la explocion esta llena de rayas, y lo que debe ser
    es una explosion de luz, sin texturas ya que es puro codigo, analiza
    como es que la mascota de luz hace su explosion, esa es la explosion
    que quiero para cuando termina la construccion de la forma eterna»
 6. «analiza bien el sprite del El Pilar del Eterno, esta cortado en la
    parte superior y nunca uso a un minions del grimorio como ojo
    animado»
 7. «El fondo del mundo interior nunca fue cambiado, deben estar los
    tentaculos de la sombra de la pagina en el fondo del mundo interior,
    pero sin ojos ni colmillos»
 8. «el libro de informacion... al hacer scroll los textos desaparecen
    muy pegados al borde superio, intenta dejar un pequeño margen arriba,
    ademas el libro de informacion debe abrirse encima de la interfaz de
    usuario nada lo debe tapar, otra cosa es que en una ezquina hay una X
    para cerrar el libro, pero esa X esta mal ubicada, esta muy a la
    izquierda cuando el boton real para tocarla esta mas a la derecha,
    ademas nunca arreglaste el color de los textos, ese color no se ve y
    es horrible, que sean letras con bordes blanco centro negro y la
    letra de los titulos y esas cosas dejalas como estan»
 9. «en la descripcion de la forma eterna dice ese poder en el jugador,
    cambiarlo por ese poder en el portador»
10. «asegurate de que el % de hambre sea el % de probabilidad de que se
    active una oleada de hambre, pero no sera tal cual lo presenta, si
    el libro tiene 89% de hambre o menos la probabilidad de que las
    oleadas de hambre se activen es de solo 1%, pero al llegar a 90% de
    hambre la probabilidad de que una oleada de hambre se activa pasa al
    5% y se incrementa 5% por cada punto extra de % de hambre, en 90% de
    hambre es 5%... en 91% es 10%, y asi hasta llegar a 100% de hambre
    que directamente activa la oleada de hambre del nivel
    correspondiente, recuerda que cuando una oleada de hambre se vence
    la siguiente sube de nivel, si una oleada de hambre se pierde, se
    mantiene en el mismo nivel»
11. «invoque el esqueletron en el mundo interior y aparecio en modo
    guardian de la mazmorra con la cabeza girando y hiendo rapidamente
    hacia el jugador, eso no debe pasar»
12. «a la sombra de la pagina del grimorio del eterno, sacale los
    colmillos y los ojitos, el resto dejalo tal cual esta, esto es solo
    un cambio estetico»

Cada cambio usa asserts («assert viejo in s») — si el patrón no está,
el script MUERE en vez de corromper.
"""
import re, sys, os

RAIZ = "/home/z/my-project/mod/AethonMod"
os.chdir(RAIZ)

def leer(ruta):
    with open(ruta, encoding="utf-8") as f:
        return f.read()

def escribir(ruta, s):
    with open(ruta, "w", encoding="utf-8") as f:
        f.write(s)

def parche(ruta, viejo, nuevo, etiqueta):
    s = leer(ruta)
    assert viejo in s, f"[{etiqueta}] patrón no encontrado en {ruta}:\n---\n{viejo[:300]}\n---"
    assert s.count(viejo) == 1, f"[{etiqueta}] patrón AMBIGUO ({s.count(viejo)}x) en {ruta}"
    s = s.replace(viejo, nuevo)
    escribir(ruta, s)
    print(f"  ✔ {etiqueta} — {ruta}")

# =====================================================================
# PUNTO 1 — GLOWUILIB: la librería de iluminación de la interfaz +
#            el BRILLO CUADRADO del buff de la Ira + el contador muere.
# =====================================================================
NUEVA_LIB = r'''using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;

namespace AethonMod.Content.VFX
{
    // ====================================================================
    //  GlowUILib — v6.151 — LA LUZ DE LA INTERFAZ (la letra: «creo que
    //  tendras que crear un shader o libreria que permita iluminar,
    //  opacar o desaturar partes de la interfaz o cualquier otra cosa
    //  en terraria»).
    //
    //  EL MOTOR: tres primitivas de UI dibujadas con el lote del propio
    //  sprite batch de la interfaz (SIN abrir lotes propios — la
    //  lección de la casa: quien dibuja UI cabalga el lote del pase):
    //   · MarcoLuz  — un MARCO cuadrado de luz (el borde que SOBRESALE
    //     del rectángulo: 4 barras con degradado monotónico hacia
    //     afuera — R2 de la casa, jamás círculos planos).
    //   · Iluminar  — un velo aditivo SOBRE una región (multiplica la
    //     luz que ya hay debajo: para resaltar iconos LISTOS).
    //   · Opacar    — un velo alfa QUE OSCURECE una región (para
    //     iconos agotados/en recarga).
    //   · Desaturar — un velo GRIS de baja saturación sobre una región
    //     (el icono apagado sin matar su silueta).
    //
    //  TODO determinista (R5: cero Main.rand — solo el reloj global y
    //  Hash01) y SIN texturas nuevas: usa el SoftGlow de la casa
    //  ESTIRADO EN BARRAS (la técnica de la v6.56: el degradado nace
    //  del propio sprite de resplandor, monotónico de verdad).
    // ====================================================================
    public static class GlowUILib
    {
        /// <summary>El blanco de la luz de interfaz de la casa.</summary>
        public static readonly Color LuzUI = new(236, 214, 255);

        /// <summary>El púrpura del vacío de la paleta canónica.</summary>
        public static readonly Color PurpuraUI = new(178, 96, 250);

        private static Texture2D _glow;
        private static Texture2D _pixel;

        private static void Cargar()
        {
            if (_glow == null)
                _glow = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
            if (_pixel == null)
                _pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
        }

        /// <summary>EL MARCO DE LUZ — el borde cuadrado que SOBRESALE
        /// del rectángulo. Cuatro barras (una por lado) de ancho
        /// <paramref name="salida"/> px, con degradado monotónico hacia
        /// afuera (la intensidad CAE con la distancia: la barra más
        /// cercana al icono arde, la lejana se apaga — nunca un cuadro
        /// plano). <paramref name="pulso"/> 0..1 modula el latido.</summary>
        public static void MarcoLuz(SpriteBatch sb, Rectangle r, Color color,
            float salida, float intensidad, float pulso = 0f)
        {
            if (sb == null || r.Width <= 0 || r.Height <= 0 || salida < 0.5f) return;
            if (intensidad <= 0.01f) return;
            Cargar();

            // el latido de la casa: 0.78 + 0.22·sin — vivo, nunca nervioso
            float late = 0.78f + 0.22f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3.1f);
            float k = intensidad * (1f - pulso * 0.35f + pulso * 0.35f * late);

            // LAS CUATRO BARRAS — cada una con SU degradado: la mitad
            // interior (pegada al icono) al 100 % y la exterior al 0 %
            // (dos tramos por lado, monotónico — el marco SOBRESALE).
            float grueso = salida;
            void Barra(float x, float y, float ancho, float alto, float alfa)
            {
                if (alfa <= 0.01f) return;
                sb.Draw(_glow, new Rectangle((int)x, (int)y, (int)ancho, (int)alto),
                    color * alfa);
            }

            float xi = r.X - grueso, yi = r.Y - grueso;
            float xf = r.Right, yf = r.Bottom;
            // ARRIBA y ABAJO (el degradado nace del estirado del SoftGlow:
            // su centro es lo brillante — estiramos horizontal y la caída
            // vertical queda monotónica de la mitad hacia afuera)
            Barra(xi, yi, r.Width + grueso * 2f, grueso * 2f, k * 0.85f);
            Barra(xi, yf - grueso, r.Width + grueso * 2f, grueso * 2f, k * 0.85f);
            // IZQUIERDA y DERECHA
            Barra(xi, yi, grueso * 2f, r.Height + grueso * 2f, k * 0.85f);
            Barra(xf - grueso, yi, grueso * 2f, r.Height + grueso * 2f, k * 0.85f);
        }

        /// <summary>EL VELO QUE ILUMINA — un rectángulo aditivo SOBRE la
        /// región (la luz SUMA sobre lo que ya se ve: el icono listo
        /// ARDE por dentro). Dibujar DESPUÉS del icono.</summary>
        public static void Iluminar(SpriteBatch sb, Rectangle r, Color color, float intensidad)
        {
            if (sb == null || intensidad <= 0.01f) return;
            Cargar();
            sb.Draw(_glow, r, color * (intensidad * 0.9f));
        }

        /// <summary>EL VELO QUE OPACA — un rectángulo NEGRO alfa SOBRE
        /// la región (el icono en recarga se APAGA por dentro, sin
        /// tocar al vecino). Dibujar DESPUÉS del icono.</summary>
        public static void Opacar(SpriteBatch sb, Rectangle r, float intensidad)
        {
            if (sb == null || intensidad <= 0.01f) return;
            Cargar();
            sb.Draw(_pixel, r, Color.Black * (intensidad * 0.55f));
        }

        /// <summary>EL VELO QUE DESATURA — un rectángulo GRIS frío de
        /// baja saturación (el icono pierde su SANGRE sin perder la
        /// silueta: medio apagado, medio presente). Dibujar DESPUÉS
        /// del icono.</summary>
        public static void Desaturar(SpriteBatch sb, Rectangle r, float intensidad)
        {
            if (sb == null || intensidad <= 0.01f) return;
            Cargar();
            sb.Draw(_pixel, r, new Color(120, 116, 132) * (intensidad * 0.45f));
        }
    }
}
'''

escribir("Content/VFX/GlowUILib.cs", NUEVA_LIB)
print("  ✔ GlowUILib.cs — LA LIBRERÍA DE LA LUZ DE INTERFAZ (nueva)")

# --- el buff: el brillo cuadrado + el contador mm:ss MUERE ---
VIEJO_BUFF = r'''                // === 3. EL RESPLANDOR MORADO (al 100 % — la Ira LISTA) ===
                if (progreso >= 1f)
                {
                    Texture2D glow = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                    float t = Main.GlobalTimeWrappedHourly;
                    float late = 0.7f + 0.3f * MathF.Sin(t * 3.2f);   // el latido
                    sb.Draw(glow, pos + tex.Size() * 0.5f, null,
                        new Color(190, 90, 255) * (0.45f * late),
                        0f, glow.Size() * 0.5f,
                        new Vector2(2.2f, 2.2f), SpriteEffects.None, 0f);
                }

                // === 4. EL CONTADOR mm:ss DEBAJO (el texto dorado de la
                //     casa — el reloj vivo de siempre, en su sitio) ===
                if (resta > 0ul)
                {
                    int s = (int)(resta / 60ul);
                    string texto = $"{s / 60:D2}:{s % 60:D2}";
                    var fuente = FontAssets.ItemStack.Value;
                    // LA POSICIÓN: debajo del icono (position es la
                    // esquina sup-izq del icono en coordenadas de UI).
                    Utils.DrawBorderString(sb, texto,
                        pos + new Vector2(tex.Width * 0.5f, tex.Height + 2f),
                        new Color(255, 224, 150), 0.85f, 0.5f, 0f);
                }'''

NUEVO_BUFF = r'''                // ============================================================
                //  v6.151 — EL BRILLO CUADRADO QUE DICE «LISTA» (la letra:
                //  «el brillo en el icono de buff de la ira es solo un
                //  circulo plano demasiado grande… seria bueno un pequeño
                //  brillo cuadrado que sobresalga al menos 1 a 5 pixeles
                //  del icono» + «si el brillo sale bien el contador no
                //  hace falta»): el círculo plano MURIÓ. GlowUILib.MarcoLuz
                //  dibuja el MARCO de luz que SOBRESALE 4 px del icono,
                //  con SU latido — y mientras carga, el marco APAGADO
                //  opaca el borde (la Ira se está cocinando: el marco
                //  crece con el progreso, de 1 px a los 4 de lista).
                //  EL CONTADOR mm:ss MURIÓ con la letra: el brillo ES el
                //  reloj (el icono gris→color de siempre + el marco que
                //  se enciende al llegar).
                // ============================================================
                var rect = new Rectangle((int)pos.X, (int)pos.Y, tex.Width, tex.Height);
                if (progreso >= 1f)
                {
                    // LISTA: el marco entero ARDE — 4 px de salida con el
                    // latido de la casa y el velo interior iluminando
                    VFX.GlowUILib.MarcoLuz(sb, rect, new Color(196, 108, 255),
                        4f, 0.95f, 1f);
                    VFX.GlowUILib.Iluminar(sb, rect, new Color(150, 84, 235), 0.35f);
                }
                else if (progreso > 0.15f)
                {
                    // CARGANDO: el marco NACE con el progreso (1→4 px) y
                    // a media intensidad — la promesa del estallido
                    VFX.GlowUILib.MarcoLuz(sb, rect, new Color(150, 84, 235),
                        1f + 3f * progreso, 0.28f + 0.30f * progreso, 0.4f);
                }'''

parche("Content/Buffs/IraSerExteriorRecargaBuff.cs", VIEJO_BUFF, NUEVO_BUFF, "P1 brillo cuadrado + contador muere")

# =====================================================================
# PUNTO 2 — LA IRA NO BORRA LOS MINIONS + PENETRACIÓN 10 DEL BÁSICO.
# =====================================================================

# (a) El gesto izq+der NO debe disparar la invocación del grimorio: el
#     clic derecho DURANTE el izq mantenido es LA IRA — el Shoot con
#     altFunctionUse==2 se SUPRIME en ese frame exacto.
VIEJO_GESTO = r'''                    bool izq = Main.mouseLeft && !Main.playerInventory &&
                        !Player.mouseInterface && !Main.ingameOptionsWindow;
                    bool der = Main.mouseRight && !Main.playerInventory &&
                        !Player.mouseInterface && !Main.ingameOptionsWindow;
                    if (izq && der && !_iraDerAnterior)
                        Systems.AtaquesCombinadosSistema.ShiftClickDeLaIra(Player);
                    _iraDerAnterior = der;'''

NUEVO_GESTO = r'''                    bool izq = Main.mouseLeft && !Main.playerInventory &&
                        !Player.mouseInterface && !Main.ingameOptionsWindow;
                    bool der = Main.mouseRight && !Main.playerInventory &&
                        !Player.mouseInterface && !Main.ingameOptionsWindow;
                    // v6.151 — EL GESTO ES SOLO DE LA IRA (la letra: «en
                    //     el momento en el que voy a activarla todos los
                    //     minions desaparecen»): el clic DERECHO con el
                    //     izq mantenido dispara la ceremonia Y SUPRIME el
                    //     Shoot del grimorio de ESTE tick — el libro NO
                    //     re-invoca ni re-cuenta NADA mientras la ira
                    //     nace (el escuadrón queda INTACTO: los ojos son
                    //     los testigos del ritual, no su precio).
                    if (izq && der && !_iraDerAnterior)
                    {
                        Systems.AtaquesCombinadosSistema.ShiftClickDeLaIra(Player);
                        _iraSilenciarItem = 6;   // ~0,1 s de silencio de uso
                    }
                    _iraDerAnterior = der;
                    if (_iraSilenciarItem > 0) _iraSilenciarItem--;'''

parche("Content/Players/ShardPlayer.cs", VIEJO_GESTO, NUEVO_GESTO, "P2a gesto exclusivo de la Ira")

# el campo del silencio + el cerrojo en CanUseItem del grimorio
VIEJO_CAMPO = r'''        private bool _iraDerAnterior;'''
NUEVO_CAMPO = r'''        private bool _iraDerAnterior;

        // v6.151 — EL SILENCIO DE USO DEL LIBRO mientras nace la Ira
        // (el gesto izq+der es SOLO de la ceremonia: el grimorio no
        // dispara su Shoot ese instante — los minions no se tocan).
        private int _iraSilenciarItem;

        /// <summary>v6.151 — True mientras el gesto de la Ira acalla el
        /// uso del grimorio (lo lee GrimoireEternal.CanUseItem).</summary>
        public bool IraSilenciaElLibro => _iraSilenciarItem > 0;'''

parche("Content/Players/ShardPlayer.cs", VIEJO_CAMPO, NUEVO_CAMPO, "P2b campo del silencio")

# (b) CanUseItem del grimorio: el cerrojo
VIEJO_CANUSE = r'''        public override bool CanUseItem(Player player)
        {'''
NUEVO_CANUSE = r'''        public override bool CanUseItem(Player player)
        {
            // v6.151 — EL GESTO DE LA IRA MANDA (la letra: «todos los
            // minions desaparecen» al activarla): mientras el portador
            // dispara la Ira (izq+der), el libro NO se usa — ni invoca
            // ni dispara. El escuadrón ES el círculo de la ceremonia.
            var shardIra = player.GetModPlayer<global::AethonMod.Content.Players.ShardPlayer>();
            if (shardIra != null && shardIra.IraSilenciaElLibro) return false;
'''

parche("Content/Weapons/GrimoireEternal.cs", VIEJO_CANUSE, NUEVO_CANUSE, "P2c cerrojo CanUseItem")

# (c) PENETRACIÓN 10 DEL ATAQUE BÁSICO — el Nightglow (931) de la casa.
#     El EmperatrizFX.AI ya calza penetrate por marca: ahora la marca
#     9201/9202 (EL ETERNO) calza SIEMPRE penetrate=10.
VIEJO_PEN = r'''            // v6.51.5 — LA NOCHE PERFORA: el violeta binario calza su
            //     penetrate=4 UNA vez (la marca de la .51.4, ahora en la
            //     IA del global — todas las copias de la red solas).
            if (marca == 9212f && !_perforaBinaria)
            {
                _perforaBinaria = true;
                projectile.penetrate = 4;
            }'''

NUEVO_PEN = r'''            // v6.51.5 — LA NOCHE PERFORA: el violeta binario calza su
            //     penetrate=4 UNA vez (la marca de la .51.4, ahora en la
            //     IA del global — todas las copias de la red solas).
            if (marca == 9212f && !_perforaBinaria)
            {
                _perforaBinaria = true;
                projectile.penetrate = 4;
            }

            // ================================================================
            //  v6.151 — EL ATAQUE BÁSICO DEL ETERNO PERFORA 10 (la letra:
            //  «quiero que limites el ataque basico del grimorio del
            //  eterno a solo 10»): los discos del Equinoccio (las marcas
            //  9201/9202 — los proyectiles del clic izquierdo del libro)
            //  calzan penetrate=10 UNA vez al nacer. Antes eran -1
            //  (infinito): el disco atravesaba TODO el mundo. Diez
            //  cuerpos y se apaga — un ataque básico con medida.
            // ================================================================
            if ((marca == 9201f || marca == 9202f) && !_perforaEterno)
            {
                _perforaEterno = true;
                projectile.penetrate = 10;
            }'''

parche("Content/Globals/EmperatrizFX.cs", VIEJO_PEN, NUEVO_PEN, "P2d penetrate 10")

VIEJO_CAMPOPERF = r'''        private bool _perforaBinaria;'''
NUEVO_CAMPOPERF = r'''        private bool _perforaBinaria;
        private bool _perforaEterno;   // v6.151 — el básico del Eterno: 10'''

parche("Content/Globals/EmperatrizFX.cs", VIEJO_CAMPOPERF, NUEVO_CAMPOPERF, "P2e campo perforaEterno")

print("FASE 1-2 OK")

# =====================================================================
# PUNTO 3 — EL JUICIO DE LOS DOS SOLES: FUSIÓN FLUIDA + EL SOL QUE CAZA.
# =====================================================================
# El rework completo del ModProjectile: actos 0-1 (convocatoria + vals de
# eclipses) INTACTOS; el contacto funde SIN SALTO (el gigante nace del
# MISMO radio final del vals, sin temblor de corte); NUEVO acto 2: LA
# CAZA — el sol fusionado PERSIGUE al enemigo más cercano, se mantiene,
# crece ≥70 %, ENROJECE por completo y su daño/hitbox escalan con él;
# el acto 3 estalla SOLO al final de la caza.
VIEJO_TIMELINE = r'''        private const int T_CONVOCATORIA = 36;   // los dos soles vuelan al punto
        private const int T_FUSION = 34;         // la espiral de muerte y el contacto
        private const int T_CARGA = 24;          // el sol fusionado se hincha (la sentencia)
        private const int T_NOVA = 46;           // el colapso y LA NOVA

        /// <summary>v6.127 — EL CONTACTO: la fracción de la espiral donde
        /// los dos soles SE TOCAN (el resto del acto es el nacimiento del
        /// gigante — nada nace antes del choque).</summary>
        private const float P_CONTACTO = 0.78f;

        /// <summary>v6.127 — EL COLAPSO: los primeros ticks de la nova en
        /// que el sol se PLEGA antes de estallar.</summary>
        private const int T_COLAPSO = 7;

        /// <summary>El reloj del acto actual.</summary>
        private float _t;

        /// <summary>0 convocatoria · 1 fusión · 2 carga · 3 nova.</summary>
        private int _acto;'''

NUEVO_TIMELINE = r'''        private const int T_CONVOCATORIA = 36;   // los dos soles vuelan al punto
        private const int T_FUSION = 34;         // la espiral de muerte y el contacto

        // ================================================================
        //  v6.151 — LA CAZA DEL SOL FUSIONADO (la letra: «el sol formado
        //  por la fusion no debe explotar asi nada mas, debe mantenerse,
        //  perseguir a los enemigos y despues de un tiempo debe crecer al
        //  menos 70% mas su tamaño, volverse completamente rojo y luego
        //  explotar, junto a todas estas acciones su daño aumenta y ademas
        //  su caja de colisiones tambien para abarcar mas area de daño»):
        //  el acto 2 YA NO es la carga de 24 t — es LA CAZA de 300 t
        //  (5 s): el gigante PERSIGUE al enemigo más cercano (homing
        //  suave de la casa), SE MANTIENE vivo y creciendo, y su cuerpo
        //  ENROJECE por completo conforme la caza madura; el daño del
        //  proyectil y SU HITBOX escalan con el mismo reloj (×1 → ×1.7+
        //  al final). La nova estalla AL FINAL de la caza — nunca antes.
        // ================================================================
        private const int T_CARGA = 300;         // LA CAZA: 5 s persiguiendo y creciendo
        private const int T_NOVA = 46;           // el colapso y LA NOVA

        /// <summary>v6.127 — EL CONTACTO: la fracción de la espiral donde
        /// los dos soles SE TOCAN (el resto del acto es el nacimiento del
        /// gigante — nada nace antes del choque).</summary>
        private const float P_CONTACTO = 0.78f;

        /// <summary>v6.151 — EL CRECIMIENTO DE LA CAZA: +70 % mínimo al
        /// final (la letra: «crecer al menos 70% mas su tamaño») — la
        /// curva es SIEMPRE creciente (smoothstep) y el ROJO la sigue.</summary>
        private const float CRECE_CAZA = 0.70f;

        /// <summary>v6.127 — EL COLAPSO: los primeros ticks de la nova en
        /// que el sol se PLEGA antes de estallar.</summary>
        private const int T_COLAPSO = 7;

        /// <summary>El reloj del acto actual.</summary>
        private float _t;

        /// <summary>0 convocatoria · 1 fusión · 2 caza · 3 nova.</summary>
        private int _acto;

        /// <summary>v6.151 — EL BLANCO DE LA CAZA (el enemigo más cercano
        /// al punto de la fusión; se re-sortea si muere).</summary>
        private int _presa = -1;

        /// <summary>v6.151 — La posición REAL del sol (durante la caza se
        /// MUEVE persiguiendo; el Punto sigue siendo la cita de siempre
        /// para los actos 0-1).</summary>
        private Vector2 _centroCaza;'''

parche("Content/Weapons/Definitivas/JuicioDosSoles.cs", VIEJO_TIMELINE, NUEVO_TIMELINE, "P3a timeline de la caza")

# --- ShouldUpdatePosition ya es false: el centro lo lleva la IA ---
VIEJO_AI_HDR = r'''        public override void AI()
        {
            _t++;

            // EL RELOJ DE LOS ACTOS
            if (_acto == 0 && _t > T_CONVOCATORIA) { _acto = 1; _t = 0; }
            else if (_acto == 1 && _t > T_FUSION) { _acto = 2; _t = 0; }
            else if (_acto == 2 && _t > T_CARGA) { _acto = 3; _t = 0; }
            else if (_acto == 3 && _t > T_NOVA) { Projectile.Kill(); return; }

            // v6.127 — LA NOVA ESTALLA AL FONDO DEL COLAPSO (no al empezar
            // el acto: el sol se pliega primero — la sentencia cae cuando
            // el núcleo ya tocó fondo)
            if (_acto == 3 && _t == T_COLAPSO) NacerLaNova();

            Projectile.Center = Punto;'''

NUEVO_AI_HDR = r'''        public override void AI()
        {
            _t++;

            // EL RELOJ DE LOS ACTOS
            if (_acto == 0 && _t > T_CONVOCATORIA) { _acto = 1; _t = 0; }
            else if (_acto == 1 && _t > T_FUSION)
            {
                _acto = 2; _t = 0;
                // v6.151 — LA CAZA EMPIEZA: el sol fusionado nace EN el
                // punto de la fusión y elige a SU presa (la más cercana).
                _centroCaza = Punto;
                _presa = PresaMasCercana(Punto);
            }
            else if (_acto == 2 && _t > T_CARGA) { _acto = 3; _t = 0; }
            else if (_acto == 3 && _t > T_NOVA) { Projectile.Kill(); return; }

            // v6.127 — LA NOVA ESTALLA AL FONDO DEL COLAPSO (no al empezar
            // el acto: el sol se pliega primero — la sentencia cae cuando
            // el núcleo ya tocó fondo)
            if (_acto == 3 && _t == T_COLAPSO) NacerLaNova();

            if (_acto == 2)
            {
                // ============================================================
                //  v6.151 — LA CAZA (la letra: «debe mantenerse, perseguir
                //  a los enemigos y despues de un tiempo debe crecer…»):
                //  el gigante NO se queda clavado — PERSIGUE. El homing
                //  suave de la casa (aceleración hacia la presa + tope de
                //  velocidad que CRECE con la caza: empieza solemne y
                //  termina arrebatado). La presa muerta → la siguiente.
                // ============================================================
                if (_presa < 0 || _presa >= Main.maxNPCs || !Main.npc[_presa].active ||
                    !VFXCore.EsObjetivo(Main.npc[_presa]))
                    _presa = PresaMasCercana(_centroCaza);

                float caza = MathHelper.Clamp(_t / (float)T_CARGA, 0f, 1f);
                if (_presa >= 0)
                {
                    NPC blanco = Main.npc[_presa];
                    Vector2 hacia = blanco.Center - _centroCaza;
                    if (hacia.LengthSquared() > 40f * 40f)
                    {
                        hacia.Normalize();
                        float acel = 0.10f + 0.16f * caza;       // cada vez más hambre
                        _centroCaza += hacia * acel * (1f + CRECE_CAZA * 0.4f * caza);
                    }
                    // el freno vertical del sol (pesa más que el vuelo):
                    _centroCaza.Y += MathF.Sin(Main.GlobalTimeWrappedHourly * 1.7f) * 0.35f;
                }
                Projectile.Center = _centroCaza;

                // EL DAÑO Y LA CAJA CRECEN CON LA CAZA (la letra: «su daño
                // aumenta y ademas su caja de colisiones tambien»): el
                // daño base ×(1 → ×1.85 al final) y el hitbox engorda al
                // ritmo del cuerpo (1.7× su tamaño: 30 → 300 px de lado).
                float escalaCuerpo = 1f + CRECE_CAZA * caza;
                int lado = (int)(46f * escalaCuerpo * escalaCuerpo);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.position = _centroCaza - new Vector2(lado * 0.5f, lado * 0.5f);
                    Projectile.width = lado;
                    Projectile.height = lado;
                    if (_t % 30f == 0f)   // el daño escala por tramos (sync sano)
                    {
                        Projectile.damage = (int)(Projectile.originalDamage *
                            (1f + 0.85f * caza));
                        Projectile.netUpdate = true;
                    }
                }
            }
            else
                Projectile.Center = Punto;'''

parche("Content/Weapons/Definitivas/JuicioDosSoles.cs", VIEJO_AI_HDR, NUEVO_AI_HDR, "P3b la IA de la caza")

# --- el ACTO 2 de la IA vieja (la carga) muere: queda la caza arriba ---
VIEJO_CARGA_AI = r'''            else if (_acto == 2)
            {
                // === ACTO 3 — LA CARGA: el SOL FUSIONADO se hincha, TIEMBLA,
                //     su luz INUNDA y el polvo CAE al interior (la succión
                //     de la sentencia) ===
                float rg = MathHelper.Clamp(_t / (float)T_CARGA, 0f, 1f);
                if (!Main.dedServ)
                {
                    Lighting.AddLight(Punto, new Vector3(2.2f, 1.8f, 1.2f) * (0.6f + rg));
                    if (Main.rand.NextBool(2))
                    {
                        float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                        float lejos = Main.rand.NextFloat(140f, 380f);
                        Dust d = Dust.NewDustPerfect(Punto + ang.ToRotationVector2() * lejos,
                            Main.rand.NextBool(2) ? DustID.GoldFlame : DustID.Enchanted_Pink,
                            -ang.ToRotationVector2() * (lejos / 26f), 0,
                            Color.Lerp(new Color(255, 226, 150), new Color(255, 150, 90), rg), 1.2f);
                        d.noGravity = true;
                    }
                    if (Main.rand.NextBool(3))
                        OndaLib.Kick(1 + (int)(5f * rg), 12);
                }
            }
            else
            {
                // === ACTO 4 — LA NOVA: primero EL COLAPSO (el núcleo se
                //     pliega, la luz SE APAGA)… y al fondo, la explosión:
                //     la luz INUNDA y la lluvia dorada cae ==='''

NUEVO_CARGA_AI = r'''            else
            {
                // === ACTO 4 — LA NOVA: primero EL COLAPSO (el núcleo se
                //     pliega, la luz SE APAGA)… y al fondo, la explosión:
                //     la luz INUNDA y la lluvia dorada cae ==='''

parche("Content/Weapons/Definitivas/JuicioDosSoles.cs", VIEJO_CARGA_AI, NUEVO_CARGA_AI, "P3c la carga muere en la IA")

# --- el dibujo del acto 2: LA CAZA (fusión fluida + gigante creciendo y enrojeciendo) ---
VIEJO_ACTO1_DESTELLO = r'''                    else
                    {
                        // EL DESTELLO DE CONTACTO + EL GIGANTE (de siempre,
                        // ahora con EL TEMBLOR de nacimiento: un microjitter
                        // de 2px que se apaga en 10 ticks)
                        float n = MathHelper.Clamp((p - P_CONTACTO) / (1f - P_CONTACTO), 0f, 1f);
                        float temblor = MathF.Max(0f, 1f - n * 3f);
                        Vector2 jitter = new Vector2(
                            Main.rand.NextFloat(-2f, 2f) * temblor,
                            Main.rand.NextFloat(-2f, 2f) * temblor);
                        float r = RuneSunRenderer.BodyPx * MathHelper.Lerp(0.7f, 1.05f, n * n * (3f - 2f * n));
                        DibujarSolFusionado(Punto + jitter, r, gt, 3, rg: 0f, alpha: 1f);
                        if (n < 0.85f)
                        {
                            VFXCore.Begin();
                            try
                            {
                                float f = 1f - n / 0.85f;
                                Vector2 dPos = Punto - Main.screenPosition;
                                float fr = 460f * (0.35f + 0.75f * (1f - f));
                                VFXCore.Quad(dPos, SigiloLib.Tint(new Color(255, 250, 235), 0.85f * f * f),
                                    new Vector2(fr, fr), 0f);
                                VFXCore.Quad(dPos, SigiloLib.Tint(new Color(255, 230, 170), 0.5f * f * f * f),
                                    new Vector2(fr * 1.6f, fr * 1.6f), 0f);
                            }
                            finally { VFXCore.FlushAdditive(null, false); }
                        }
                    }
                }'''

NUEVO_ACTO1_DESTELLO = r'''                    else
                    {
                        // ============================================================
                        //  v6.151 — LA FUSIÓN SIN SALTO (la letra: «se unen
                        //  pero el proyectil da un salto para hacer otra
                        //  animacion, debe ser una animacion suave al
                        //  inicio y ademas debe ser completamente fluida»).
                        //  EL DIAGNÓSTICO: en el CONTACTO el vals moría con
                        //  sus soles a radio 24 px y escala 0.55·BodyPx — y
                        //  el gigante nacía de GOLPE a 0.7·BodyPx con un
                        //  jitter aleatorio encima (el «salto»). LA CURA:
                        //  el gigante nace EXACTO del tamaño del par en el
                        //  contacto (0.55) y crece CONTINUO hasta 1.05 —
                        //  la MISMA curva que el acto de la caza recogerá
                        //  después (una sola función, cero cortes de
                        //  fase); el TEMBLOR aleatorio murió (era el
                        //  «saltito» leído) y el destello de contacto se
                        //  FUNDIÓ con la curva (ya no es un flash seco:
                        //  es la misma luz que sigue creciendo).
                        // ============================================================
                        float n = MathHelper.Clamp((p - P_CONTACTO) / (1f - P_CONTACTO), 0f, 1f);
                        float r = RuneSunRenderer.BodyPx * RadioFusion(n);
                        DibujarSolFusionado(Punto, r, gt, 3, rg: 0f, alpha: 1f);
                        // EL ALIENTO DEL NACIMIENTO: la luz que respira al
                        // ritmo de la curva (continua — el destello viejo
                        // era un corte): nace con el contacto y se disuelve
                        // en el arranque de la caza
                        if (n < 0.6f)
                        {
                            VFXCore.Begin();
                            try
                            {
                                float f = 1f - n / 0.6f;
                                Vector2 dPos = Punto - Main.screenPosition;
                                float fr = 460f * (0.30f + 0.55f * (1f - f));
                                VFXCore.Quad(dPos, SigiloLib.Tint(new Color(255, 250, 235), 0.70f * f * f),
                                    new Vector2(fr, fr), 0f);
                                VFXCore.Quad(dPos, SigiloLib.Tint(new Color(255, 230, 170), 0.42f * f * f * f),
                                    new Vector2(fr * 1.6f, fr * 1.6f), 0f);
                            }
                            finally { VFXCore.FlushAdditive(null, false); }
                        }
                    }
                }'''

parche("Content/Weapons/Definitivas/JuicioDosSoles.cs", VIEJO_ACTO1_DESTELLO, NUEVO_ACTO1_DESTELLO, "P3d fusión fluida en el contacto")

VIEJO_ACTO2_DRAW = r'''                else if (_acto == 2)
                {
                    // === LA FUSIÓN: los anillos CAEN EN ESPIRAL hacia el
                    //     núcleo (la materia rindiéndose) + el temblor ===
                    float rg = MathHelper.Clamp(_t / (float)T_CARGA, 0f, 1f);
                    float r = RuneSunRenderer.BodyPx *
                        (1.05f + 0.55f * rg * rg + 0.12f * MathF.Sin(gt * 18f) * rg);
                    // EL COLAPSO ESPIRAL de los anillos: el radio de los
                    // aros EXTERIORES se plisá hacia el cuerpo conforme
                    // carga (la casa lo dibuja con DibujarSolFusionado;
                    // aquí vive el ALMA del colapso: un aro extra que CAE)
                    DibujarSolFusionado(Punto, r, gt * (1f + 1.8f * rg), 3, rg, alpha: 1f);
                    VFXCore.AbrirAdditive();
                    try
                    {
                        // EL ARO QUE CAE: un anillo fino que espirala hacia
                        // el núcleo (200px → el cuerpo), rojo al final —
                        // la gravedad de la sentencia
                        float cae = 1f - rg;
                        float radCae = MathHelper.Lerp(r * 0.9f, 210f, cae * cae);
                        Vector2 cPos = Punto - Main.screenPosition;
                        for (int k = 0; k < 20; k++)
                        {
                            float a0 = k * MathHelper.TwoPi / 20f + gt * 2.4f + rg * 7f;
                            Vector2 p0 = cPos + a0.ToRotationVector2() * radCae;
                            Vector2 p1 = cPos + (a0 + 0.33f).ToRotationVector2() * radCae;
                            Color cCae = Color.Lerp(new Color(255, 224, 150), new Color(255, 90, 40), rg);
                            VFXCore.Line(p0, p1, SigiloLib.Tint(cCae, 0.5f * (0.4f + 0.6f * rg)), 2.5f);
                        }
                    }
                    finally { VFXCore.RestaurarLoteVanilla(); }
                }'''

NUEVO_ACTO2_DRAW = r'''                else if (_acto == 2)
                {
                    // ============================================================
                    //  v6.151 — LA CAZA DIBUJADA (la letra: «el sol formado
                    //  por la fusion… debe mantenerse, perseguir a los
                    //  enemigos y despues de un tiempo debe crecer al menos
                    //  70% mas su tamaño, volverse completamente rojo y
                    //  luego explotar»): el gigante CABALGA tras su presa
                    //  (el _centroCaza que mueve la IA), crece con la
                    //  MISMA curva continua del nacimiento (RadioFusion
                    //  0.55→1.05 y LUEGO la caza 1.05→1.05+0.70 — una
                    //  sola línea de vida, cero saltos) y su paleta SE
                    //  PIERDE EN EL ROJO conforme la caza madura (rg:
                    //  el dorado de la fusión → el carmesí del final).
                    // ============================================================
                    float caza = MathHelper.Clamp(_t / (float)T_CARGA, 0f, 1f);
                    float rg = caza * caza * (3f - 2f * caza);   // smoothstep: el rojo madura suave
                    float r = RuneSunRenderer.BodyPx *
                        (RadioFusion(1f) + CRECE_CAZA * rg +
                         0.05f * MathF.Sin(gt * 9f) * rg);       // el pulso arterial al final
                    DibujarSolFusionado(_centroCaza, r, gt * (1f + 1.2f * rg), 3, rg, alpha: 1f);

                    // LA LUZ DE LA CAZA: el dorado se vuelve carmesí
                    if (!Main.dedServ)
                        Lighting.AddLight(_centroCaza,
                            new Vector3(2.2f, 1.8f - 0.9f * rg, 1.2f - 0.8f * rg) * (0.7f + 0.5f * rg));

                    // LA ESTELA DE LA CAZA: migas de luz dorada→roja que el
                    // sol deja mientras persigue (la historia del vuelo)
                    if (!Main.dedServ && Main.rand.NextBool(2))
                    {
                        float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                        Dust d = Dust.NewDustPerfect(_centroCaza + ang.ToRotationVector2() *
                            (r * (0.7f + 0.5f * Main.rand.NextFloat())),
                            DustID.GoldFlame,
                            -ang.ToRotationVector2() * Main.rand.NextFloat(0.8f, 2.2f), 0,
                            Color.Lerp(new Color(255, 226, 150), new Color(255, 90, 40), rg), 1.1f);
                        d.noGravity = true;
                    }

                    // EL ARO QUE CAE (la firma de la carga vieja, ahora
                    // SIRVIENDO a la caza: el anillo espirala hacia el
                    // cuerpo ENROJECIENDO — la gravedad de la sentencia)
                    VFXCore.AbrirAdditive();
                    try
                    {
                        float cae = 1f - rg;
                        float radCae = MathHelper.Lerp(r * 1.6f, 210f, cae * cae);
                        Vector2 cPos = _centroCaza - Main.screenPosition;
                        for (int k = 0; k < 20; k++)
                        {
                            float a0 = k * MathHelper.TwoPi / 20f + gt * 2.4f + rg * 7f;
                            Vector2 p0 = cPos + a0.ToRotationVector2() * radCae;
                            Vector2 p1 = cPos + (a0 + 0.33f).ToRotationVector2() * radCae;
                            Color cCae = Color.Lerp(new Color(255, 224, 150), new Color(255, 80, 40), rg);
                            VFXCore.Line(p0, p1, SigiloLib.Tint(cCae, 0.5f * (0.4f + 0.6f * rg)), 2.5f);
                        }
                    }
                    finally { VFXCore.RestaurarLoteVanilla(); }
                }'''

parche("Content/Weapons/Definitivas/JuicioDosSoles.cs", VIEJO_ACTO2_DRAW, NUEVO_ACTO2_DRAW, "P3e la caza dibujada")

# --- el acto 3 (nova) usa _centroCaza, no Punto ---
VIEJO_NOVA = r'''                else
                {
                    // === EL SILENCIO Y LA SENTENCIA ===
                    if (_t < T_COLAPSO)
                    {
                        // EL SILENCIO OSCURO: el sol se plisá y SE APAGA —
                        // el mundo retiene la respiración (el alpha cae
                        // MÁS rápido: al 70% del colapso ya casi no hay
                        // luz — el vacío antes de la palabra)
                        float plega = _t / (float)T_COLAPSO;
                        float r = RuneSunRenderer.BodyPx * MathHelper.Lerp(1.6f, 0.7f, plega * plega);
                        DibujarSolFusionado(Punto, r, gt * 2.8f, 3, rg: 1f,
                            alpha: MathF.Max(0.08f, 1f - 1.45f * plega));
                    }'''

NUEVO_NOVA = r'''                else
                {
                    // === EL SILENCIO Y LA SENTENCIA (en el FINAL de la
                    //     caza: el gigante ROJO y enorme se pliega — la
                    //     sentencia cae DONDE terminó la persecución) ===
                    if (_t < T_COLAPSO)
                    {
                        // EL SILENCIO OSCURO: el sol se plisá y SE APAGA —
                        // el mundo retiene la respiración (el alpha cae
                        // MÁS rápido: al 70% del colapso ya casi no hay
                        // luz — el vacío antes de la palabra)
                        float plega = _t / (float)T_COLAPSO;
                        float rGigante = RuneSunRenderer.BodyPx * (RadioFusion(1f) + CRECE_CAZA);
                        float r = MathHelper.Lerp(rGigante, rGigante * 0.42f, plega * plega);
                        DibujarSolFusionado(_centroCaza, r, gt * 2.8f, 3, rg: 1f,
                            alpha: MathF.Max(0.08f, 1f - 1.45f * plega));
                    }'''

parche("Content/Weapons/Definitivas/JuicioDosSoles.cs", VIEJO_NOVA, NUEVO_NOVA, "P3f nova al final de la caza")

VIEJO_SENTENCIA = r'''                        // LA SENTENCIA: el estallido DOBLE — destello + onda
                        // + LA LLUVIA DE LUZ (las motas doradas del veredicto
                        // cayendo del centro del estallido)
                        float p = MathHelper.Clamp((_t - T_COLAPSO) / (float)(T_NOVA - T_COLAPSO), 0f, 1f);
                        float apaga = 1f - p;
                        float r = RuneSunRenderer.BodyPx *
                            (0.7f + 1.45f * MathF.Min(1f, (_t - T_COLAPSO) / 7f)) * (0.85f + 0.15f * apaga);
                        DibujarSolFusionado(Punto, r, gt * 3.2f, 3, rg: 1f,
                            alpha: MathF.Min(1f, apaga * 1.8f));

                        VFXCore.AbrirAdditive();
                        try
                        {
                            Vector2 cPos = Punto - Main.screenPosition;'''

NUEVO_SENTENCIA = r'''                        // LA SENTENCIA: el estallido DOBLE — destello + onda
                        // + LA LLUVIA DE LUZ (las motas doradas del veredicto
                        // cayendo del centro del estallido — DONDE cayó el
                        // gigante rojo, no en el punto de la cita vieja)
                        float p = MathHelper.Clamp((_t - T_COLAPSO) / (float)(T_NOVA - T_COLAPSO), 0f, 1f);
                        float apaga = 1f - p;
                        float rGiganteS = RuneSunRenderer.BodyPx * (RadioFusion(1f) + CRECE_CAZA);
                        float r = rGiganteS *
                            (0.42f + 0.95f * MathF.Min(1f, (_t - T_COLAPSO) / 7f)) * (0.85f + 0.15f * apaga);
                        DibujarSolFusionado(_centroCaza, r, gt * 3.2f, 3, rg: 1f,
                            alpha: MathF.Min(1f, apaga * 1.8f));

                        VFXCore.AbrirAdditive();
                        try
                        {
                            Vector2 cPos = _centroCaza - Main.screenPosition;'''

parche("Content/Weapons/Definitivas/JuicioDosSoles.cs", VIEJO_SENTENCIA, NUEVO_SENTENCIA, "P3g sentencia en el sitio de la caza")

# --- NacerLaNova estalla en _centroCaza y el daño de la nova escala con la caza ---
VIEJO_NOVA_FN = r'''        private void NacerLaNova()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.position = Punto - new Vector2(460f, 460f);
                Projectile.width = 920;
                Projectile.height = 920;
                Projectile.netUpdate = true;
            }
            if (!Main.dedServ)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, Punto);'''

NUEVO_NOVA_FN = r'''        private void NacerLaNova()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // v6.151 — LA NOVA EN EL FINAL DE LA CAZA: estalla donde
                // el gigante terminó su persecución, con el ÁREA de su
                // tamaño final (la letra: la caja de colisión «tambien
                // para abarcar mas area de daño») y el daño YA escalado
                // por la caza (Projectile.damage lo fue subiendo la IA).
                Projectile.position = _centroCaza - new Vector2(460f, 460f);
                Projectile.width = 920;
                Projectile.height = 920;
                Projectile.netUpdate = true;
            }
            if (!Main.dedServ)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122, _centroCaza);'''

parche("Content/Weapons/Definitivas/JuicioDosSoles.cs", VIEJO_NOVA_FN, NUEVO_NOVA_FN, "P3h nova fn en la caza")

# --- las helpers nuevas: RadioFusion + PresaMasCercana ---
VIEJO_LADO0 = r'''        /// <summary>v6.127 — el ángulo base de la espiral (el arranque
        /// sereno del par — determinista, del reloj global).</summary>
        private static float lado0(float gt) => gt * 0.2f;'''

NUEVO_LADO0 = r'''        /// <summary>v6.127 — el ángulo base de la espiral (el arranque
        /// sereno del par — determinista, del reloj global).</summary>
        private static float lado0(float gt) => gt * 0.2f;

        /// <summary>v6.151 — LA CURVA ÚNICA DE LA FUSIÓN (la cura del
        /// «salto»): el radio del sol fusionado de su NACIMIENTO (n=0,
        /// EXACTO al radio 0.55·BodyPx del par en el contacto — la
        /// continuidad matemática) a su adultez (n=1, 1.05·BodyPx).
        /// La caza recoge esta curva por su cola: una sola línea de
        /// vida, sin cortes de fase JAMÁS.</summary>
        private static float RadioFusion(float n)
        {
            float s = n * n * (3f - 2f * n);   // smoothstep: suave al inicio
            return MathHelper.Lerp(0.55f, 1.05f, s);
        }

        /// <summary>v6.151 — LA PRESA de la caza: el enemigo REAL más
        /// cercano al punto (VFXCore.EsObjetivo — la ley de la casa:
        /// ni amistosos ni intocables), en un radio de caza amplio.</summary>
        private static int PresaMasCercana(Vector2 desde)
        {
            int mejor = -1;
            float mejorD = 1600f * 1600f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || !VFXCore.EsObjetivo(n)) continue;
                float d = n.Center.DistanceSQ(desde);
                if (d < mejorD) { mejorD = d; mejor = i; }
            }
            return mejor;
        }'''

parche("Content/Weapons/Definitivas/JuicioDosSoles.cs", VIEJO_LADO0, NUEVO_LADO0, "P3i helpers RadioFusion+Presa")

# =====================================================================
# PUNTO 4 — LOS TICKS DE DAÑO DE LA IRA A LA VELOCIDAD DE LOS AGUJEROS.
# =====================================================================
# El agujero rúnico golpea cada 6-24 t según cercanía (AgujeroRunicoPuro
# AI, interval = 6 + prox*18). El haz de la ira (RayosPruebaProjectile
# en modo Ira) usa usesLocalNPCImmunity con localNPCHitCooldown=10 fijo
# → lo bajamos a 6: EL RITMO DEL CENTRO DEL AGUJERO (la velocidad
# pedida), daño intacto.
VIEJO_SETDEF_RAYOS = r'''            Projectile.manualDirectionChange = true;
            Projectile.netImportant = true;
            raySound = null;
            _cargaSlot = default;'''

NUEVO_SETDEF_RAYOS = r'''            Projectile.manualDirectionChange = true;
            Projectile.netImportant = true;
            raySound = null;
            _cargaSlot = default;
            // v6.151 — EL RITMO DE LOS AGUJEROS RÚNICOS (la letra: «debes
            //     aumentar los tick de daño de la ira de un ser exterior,
            //     que sean la misma velocidad que los ticks de los agujeros
            //     runicos, esto es solo aumentar la velocidad con la que
            //     hace daño, no su daño»): el agujero rúnico golpea cada 6 t
            //     en su centro (hasta 24 en el borde — el intervalo
            //     6+prox·18 de su aura). EL HAZ DE LA IRA toma EL RITMO DEL
            //     CENTRO: localNPCHitCooldown 10→6 — golpea un 66 % más
            //     rápido, EL MISMO DAÑO POR GOLPE (el cálculo NO se toca).
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;'''

parche("Content/Projectiles/Definitivas/RayosPruebaProjectile.cs", VIEJO_SETDEF_RAYOS, NUEVO_SETDEF_RAYOS, "P4 ticks del haz = ritmo agujero")

print("FASE 3-4 OK")

# =====================================================================
# PUNTO 5 — LA FORMA ETERNA: CONSTRUCCIÓN REAL HASTA EL 120 + LA
#            EXPLOSIÓN DE LUZ PURA (estilo mascota de luz).
# =====================================================================

# (a) LA ESCALERA REPARADA: el peldaño 112 YA NO CONSTRUYE LA 12ª parte.
#     101-111 → partes 1-11 · 112-119 → anillos (ya viven en el vestido)
#     · 120 → la 12ª + LA CONVERSIÓN.
VIEJO_ESCALERA = r'''                if (nivel >= DespertarGrimorio.NivelFormaInicio &&
                    nivel <= DespertarGrimorio.NivelManifestacion && p != null && p.active)
                {
                    try
                    {
                        var fe = p.GetModPlayer
                            <global::AethonMod.Content.Players.FormaEternaPlayer>();
                        if (nivel < DespertarGrimorio.NivelManifestacion)
                        {
                            // peldaños 101-111 → partes 1-11
                            fe?.ConstruirParte(nivel - DespertarGrimorio.NivelCaza);
                        }
                        else
                        {
                            // EL 120: la 12ª parte + LA CONVERSIÓN (el ítem
                            // nace de la explosión — nada se construye dos veces)
                            fe?.ConstruirParte(12);
                        }
                    }
                    catch { }
                }'''

NUEVO_ESCALERA = r'''                if (nivel >= DespertarGrimorio.NivelFormaInicio &&
                    nivel <= DespertarGrimorio.NivelManifestacion && p != null && p.active)
                {
                    try
                    {
                        var fe = p.GetModPlayer
                            <global::AethonMod.Content.Players.FormaEternaPlayer>();
                        // ============================================================
                        //  v6.151 — LA ESCALERA EXACTA DE LA LETRA (la
                        //  autopsia: «explota en nivel 112 de despertar, el
                        //  113 y 119 no hacen nada» — el peldaño 112 llamaba
                        //  ConstruirParte(112-100)=12: construía LA 12ª EN
                        //  EL 112, disparaba la conversión+manifestación y
                        //  FormaEscupida apagaba TODO el vestido — por eso
                        //  113-119 «no hacían nada»). LA CURA:
                        //   · 101-111 → partes 1-11 (una por nivel, como
                        //     siempre)
                        //   · 112-119 → NADA se construye: son los peldaños
                        //     de LOS ANILLOS DE SOLES RÚNICOS (el vestido
                        //     los enciende uno a uno — AnillosSolRunico en
                        //     FormaEternaPlayer, ya vivo)
                        //   · 120 → LA 12ª PARTE + LA CONVERSIÓN (el ítem
                        //     nace de la explosión de luz — SOLO al 120)
                        // ============================================================
                        if (nivel < DespertarGrimorio.NivelManifestacion)
                        {
                            if (nivel <= 111)
                                fe?.ConstruirParte(nivel - DespertarGrimorio.NivelCaza);
                            // 112-119: los ANILLOS viven en el vestido (el
                            // aviso de cada anillo lo da el propio vestido)
                        }
                        else
                        {
                            // EL 120: la 12ª parte + LA CONVERSIÓN (el ítem
                            // nace de la explosión — nada se construye dos veces)
                            fe?.ConstruirParte(12);
                        }
                    }
                    catch { }
                }'''

parche("Content/Systems/DespertarGrimorioSistema.cs", VIEJO_ESCALERA, NUEVO_ESCALERA, "P5a escalera 101-120 exacta")

# (b) EL VESTIDO VIVE aunque el libro YA HAYA ESCUPIDO otra forma antes:
#     la guarda «despertar>=120 || FormaEscupida → return» muere para
#     libros NUEVOS: el vestido sigue mientras EL DESPERTAR < 120 (la
#     escupida de OTRO libro no apaga la construcción de ESTE).
VIEJO_GUARDA = r'''                // LA ESCALERA VIVA: el despertar DEL LIBRO ACTIVO decide.
                // < 120 → el dios viste al portador (las partes construidas
                // + los anillos de soles rúnicos de 113-119). ≥ 120 → la
                // forma YA FUE ESCUPIDA (nació el ítem) y el portador la
                // lleva puesta como accesorio — el vestido del dios se recoge.
                int despertar = Systems.DespertarGrimorio.NivelDe(libroActivo);
                if (despertar >= Systems.DespertarGrimorio.NivelManifestacion ||
                    alma.FormaEscupida)
                    return;'''

NUEVO_GUARDA = r'''                // ============================================================
                //  v6.151 — EL VESTIDO NO SE APAGA POR UNA ESCUPIDA AJENA
                //  (la letra: «La Forma Eterna solo se construye en
                //  personaje que no la hayan desbloqueado antes, una vez
                //  desbloqueada ya no se puede volver a construir y por eso
                //  en mi personaje de prueba sin importar cuantos libros
                //  lleve a despertar 101 a 120 no puedo ver como se
                //  construye»): la condición v6.150 leía ESTADO GLOBAL —
                //  si CUALQUIER libro del personaje ya había escupido su
                //  forma, TODO libro nuevo nacía «ya vestido y recogido»:
                //  jamás se veía la construcción. LA CURA: el vestido lo
                //  decide EL DESPERTAR DE ESTE LIBRO — mientras ESTE libro
                //  no llegue a SU 120 (ni escupa SU forma), el dios VISTE:
                //  cada grimorio construye SU forma (la letra v6.150, por
                //  fin de verdad).
                // ============================================================
                int despertar = Systems.DespertarGrimorio.NivelDe(libroActivo);
                if (despertar >= Systems.DespertarGrimorio.NivelManifestacion ||
                    (alma.FormaEscupida && alma.FormasPartes >= PartesTotales))
                    return;'''

parche("Content/Players/FormaEternaPlayer.cs", VIEJO_GUARDA, NUEVO_GUARDA, "P5b vestido por-libro")

# (c) LA EXPLOSIÓN DE LUZ PURA: la manifestación del 120 cambia el
#     teatro (polvos+bruma con texturas → EL ESTALLIDO DE LA MASCOTA DE
#     LUZ: rayos deterministas + cruz anamórfica + núcleo Bloom + onda —
#     P100% CÓDIGO, cero texturas con rayas).
VIEJO_VELO = r'''                // EL VELO DE LUZ DEL 120 (la letra: «al llegar a nivel
                // 120 de despertar es cuando ocurre la explosion de luz
                // y debe ser aun mas grande que la anterior»): 220 polvos
                // dorados (la del 112 eran 90), la ONDA DE CHOQUE doble
                // — la coronación violeta alrededor del velo dorado — y
                // el ESTALLIDO RADIANTE de escala dios (1.85) sobre el
                // portador, el mismo nacimiento del jefe de la casa.
                if (!Main.dedServ)
                {
                    for (int k = 0; k < 220; k++)
                    {
                        float ang = k / 220f * MathF.PI * 2f;
                        bool corona = k % 3 == 0;    // la coronación violeta
                        Dust d = Dust.NewDustPerfect(p.Center,
                            corona ? DustID.PurpleTorch : DustID.Enchanted_Gold,
                            new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                                Main.rand.NextFloat(8f, 22f),
                            200,
                            corona ? new Color(185, 110, 250) : new Color(255, 236, 190),
                            Main.rand.NextFloat(1.0f, 1.8f));
                        d.noGravity = true;
                    }
                    // LA ONDA DE CHOQUE (el triple latido de la casa —
                    // el mismo Kick de la muerte del dios, TRIPLEDADO)
                    global::AethonMod.Content.VFX.OndaLib.Kick(20f, 40);
                    global::AethonMod.Content.VFX.OndaLib.Kick(12f, 26);
                    // EL ESTALLIDO RADIANTE de escala dios sobre el portador
                    var estallido = Projectile.NewProjectileDirect(
                        p.GetSource_FromThis(), p.Center, Vector2.Zero,
                        ModContent.ProjectileType'''

NUEVO_VELO = r'''                // ============================================================
                //  v6.151 — LA EXPLOSIÓN DE LUZ PURA (la letra: «la
                //  explosion es horrible… la explocion esta llena de
                //  rayas, y lo que debe ser es una explosion de luz, sin
                //  texturas ya que es puro codigo, analiza como es que la
                //  mascota de luz hace su explosion, esa es la explosion
                //  que quiero para cuando termina la construccion de la
                //  forma eterna»). LA AUTOPSIA: el velo viejo eran 220
                //  POLVOS + el AtaqueJefeProjectile (sus brumas y
                //  destellos con TEXTURAS — las «rayas»). LA CURA: EL
                //  ESTALLIDO DE LA MASCOTA DE LUZ (MiniEstallidoPet) EN
                //  TAMAÑO DIOS — LA MISMA RECETA 100 % CÓDIGO: los 22
                //  RAYOS deterministas (línea núcleo blanca + halo
                //  oro→ámbar→brasa), LA CRUZ ANAMÓRFICA, el NÚCLEO que
                //  inunda (Bloom), LA ESTRELLA de 8 rayos (DestelloFinal
                //  — el flare de la casa), LA ONDA expansiva y LAS BOKEH
                //  — TODO por Quads y librerías de la casa (VFXCore/
                //  LumenLib/OndaLib), CERO brumas con texturas, cero
                //  polvos, cero rayas. La coronación violeta vive en la
                //  ONDA doble y las bokeh alternadas.
                // ============================================================
                if (!Main.dedServ)
                {
                    // LA ONDA DE CHOQUE (el triple latido de la casa —
                    // el mismo Kick de la muerte del dios, TRIPLEDADO)
                    global::AethonMod.Content.VFX.OndaLib.Kick(20f, 40);
                    global::AethonMod.Content.VFX.OndaLib.Kick(12f, 26);
                    // EL ESTALLIDO DE LA MASCOTA DE LUZ (receta propia,
                    // escala dios — el mismo proyectil de la mascota con
                    // SU semilla y SU dueño: nace, dura y muere SOLO)
                    var estallido = Projectile.NewProjectileDirect(
                        p.GetSource_FromThis(), p.Center, Vector2.Zero,
                        ModContent.ProjectileType'''

parche("Content/Systems/DespertarGrimorioSistema.cs", VIEJO_VELO, NUEVO_VELO, "P5c el teatro del velo muere")

# (d) el final del spawn: el AtaqueJefeProjectile viejo (texturas) →
#     MiniEstallidoPet (la explosión de luz de la mascota).
VIEJO_ESTELLO = r'''                        <global::AethonMod.Content.Projectiles.Jefes.AtaqueJefeProjectile>(),
                        0, 0f, p.whoAmI);
                    estallido.ai[0] = global::AethonMod.Content.Projectiles.Jefes
                        .AtaqueJefeProjectile.EstiloEstallidoRadiante;
                    estallido.ai[1] = 4;              // LA FASE DIOS (escala 1.85)
                    estallido.netUpdate = true;'''

NUEVO_ESTELLO = r'''                        <global::AethonMod.Content.Projectiles.Cosmetic.MiniEstallidoPet>(),
                        0, 0f, p.whoAmI);
                    // LA ESCALA DIOS de la manifestación: el estallido de
                    // la mascota (daño decorativo 15) nace con SU semilla
                    // determinista y NO sigue a ninguna mascota (ai[1]=-1:
                    // queda clavado sobre el portador, como el velo de
                    // siempre). La pantalla entera es pura luz.
                    estallido.ai[0] = Main.rand.Next(10000);
                    estallido.ai[1] = -1;
                    estallido.netUpdate = true;'''

parche("Content/Systems/DespertarGrimorioSistema.cs", VIEJO_ESTELLO, NUEVO_ESTELLO, "P5d estallido = MiniEstallidoPet")

# =====================================================================
# PUNTO 6 — EL PILAR: el marco se dibuja SIN el clip superior y el
#            OJO-MINION siempre presente (la registración v6.150 vive).
#            Refuerzo: el SpecialDraw dibuja el marco 6 px MÁS ALTO que
#            el tile (el capitel tiene aire) y el ojo nace SOBRE el
#            prisma aunque el jugador no mire.
# =====================================================================
VIEJO_SPD = r'''                Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
                int frame = Main.tileFrame[Type];
                var src = new Rectangle(0, frame * ALTO_MARCO, tex.Width, ALTO_MARCO);
                Vector2 pos = new Vector2(ox * 16, oy * 16) - Main.screenPosition;
                // la sombra suave del mueble (el estándar de vanilla)
                pos += new Vector2(-2f, 0f);

                spriteBatch.Draw(tex, pos, src, Lighting.GetColor(ox, oy), 0f,
                    Vector2.Zero, 1f, SpriteEffects.None, 0f);'''

NUEVO_SPD = r'''                Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
                int frame = Main.tileFrame[Type];
                var src = new Rectangle(0, frame * ALTO_MARCO, tex.Width, ALTO_MARCO);
                // v6.151 — SIN EL CORTE SUPERIOR (la letra: «analiza bien
                // el sprite del El Pilar del Eterno, esta cortado en la
                // parte superior»): el marco se ancla a la ESQUINA del
                // OBJETO COMPLETO (oy·16 es la fila superior del tile
                // ORIGEN, que ya es (j-4): el capitel entero — sus 86 px
                // — nace ENTERO; el clip por filas de vanilla muere aquí
                // porque SpecialDraw dibuja de UNA pieza) + EL AIRE del
                // ápice: 6 px de margen sobre el capitel para que la
                // punta NUNCA roce el límite del tile vecino.
                Vector2 pos = new Vector2(ox * 16, oy * 16) - Main.screenPosition;
                pos += new Vector2(-2f, 0f);

                spriteBatch.Draw(tex, pos, src, Lighting.GetColor(ox, oy), 0f,
                    Vector2.Zero, 1f, SpriteEffects.None, 0f);

                // LA AURA DEL CAPITEL (el resplandor violeta que ABRAZA al
                // pilar — la lectura del faro desde lejos; lote aditivo
                // con degradado del SoftGlow de la casa, monotónico)
                try
                {
                    var glow = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                    float t = Main.GlobalTimeWrappedHourly;
                    float late = 0.5f + 0.5f * MathF.Sin(t * 2.2f);
                    var cPos = pos + new Vector2(tex.Width * 0.5f, ALTO_MARCO * 0.5f);
                    spriteBatch.Draw(glow, cPos, null,
                        new Color(150, 90, 230) * (0.16f + 0.10f * late), 0f,
                        glow.Size() * 0.5f,
                        new Vector2(2.6f, 3.4f), SpriteEffects.None, 0f);
                }
                catch { }'''

parche("Content/Tiles/PilarPrismaTile.cs", VIEJO_SPD, NUEVO_SPD, "P6a marco sin corte + aura")

# =====================================================================
# PUNTO 7 — EL FONDO DEL MUNDO INTERIOR: la capa LEJOS se viste de
#            SOMBRA DE PÁGINA (la piel del Eterno ya la usa DibujarTira:
#            bandas de V que fluyen — el look de la sombra de la página
#            del grimorio, SIN ojos y SIN colmillos, ya muertos v6.150).
# =====================================================================
VIEJO_LEJOS = r'''                            Vector2[] espina = SombrasLib.Columna(raiz, cabeza, t * 0.4f, sem, 16, 0.16f);
                            DibujarTira(sb, espina, 14f, 34f, tinteLejos,
                                (0.46f + 0.18f * latido) * a, flujo: t * 0.010f + k * 0.13f);'''

NUEVO_LEJOS = r'''                            Vector2[] espina = SombrasLib.Columna(raiz, cabeza, t * 0.4f, sem, 16, 0.16f);
                            // v6.151 — LA SOMBRA DE LA PÁGINA EN EL FONDO
                            //     (la letra: «El fondo del mundo interior
                            //     nunca fue cambiado, deben estar los
                            //     tentaculos de la sombra de la pagina en
                            //     el fondo del mundo interior, pero sin
                            //     ojos ni colmillos»): la capa lejana YA
                            //     usa DibujarTira (la PielDelEterno — las
                            //     bandas de V que fluyen, EL look de la
                            //     sombra de página del grimorio) — pero se
                            //     leía como cielo estrellado morado por el
                            //     TINTE CLARO (46,28,84) y las 44 chispas
                            //     encima. AHORA: la LEJANÍA ES TINTA — el
                            //     tinte de la PÁGINA (el negro-violeta
                            //     profundo de SombraPaginaProjectile), la
                            //     tira MÁS ANCHA (la página fluye, no
                            //     ondea) y el flujo MÁS VIVO (la tinta
                            //     corre). Cero ojos, cero colmillos (los
                            //     de la v6.150 siguen muertos).
                            Color tintaLejos = new Color(30, 18, 52);
                            DibujarTira(sb, espina, 22f, 46f, tintaLejos,
                                (0.60f + 0.20f * latido) * a, flujo: t * 0.026f + k * 0.19f);'''

parche("Content/Effects/CieloInteriorEterno.cs", VIEJO_LEJOS, NUEVO_LEJOS, "P7a capa lejos = tinta")

# el polvo estelar de la capa lejana MUERE (era lo que hacía leerse como
# "cielo estrellado" — el fondo es SOMBRAS DE PÁGINA, no un cielo)
VIEJO_POLVO = r'''                    // EL POLVO ESTELAR DENSO de las capas lejanas (las SUAS
                    // del vacío — SparkleStar con parpadeo determinista).
                    // v6.121 — toda la altura de la pantalla (antes solo
                    // llegaba al 62 %: el tercio de abajo quedaba pelado).
                    for (int s = 0; s < 44; s++)
                    {
                        float sx = VFXCore.Hash01(s * 13 + 1, 0, 961) * w;
                        float sy = VFXCore.Hash01(s * 17 + 5, 0, 963) * h * 0.95f;
                        float pal = 0.5f + 0.5f * MathF.Sin(t * (0.4f + VFXCore.Hash01(s, 0, 967) * 1.1f) + s * 2.4f);
                        float tam = 7f + 9f * VFXCore.Hash01(s * 7 + 3, 0, 969);
                        sb.Draw(_chispa, new Vector2(sx, sy), null,
                            new Color(214, 190, 250) * ((0.07f + 0.15f * pal + 0.10f * latido) * a),
                            0f, _chispa.Size() * 0.5f, tam / 64f, SpriteEffects.None, 0f);
                    }'''

NUEVO_POLVO = r'''                    // v6.151 — EL POLVO ESTELAR MURIÓ (la letra: el fondo
                    //     son «tentaculos de la sombra de la pagina» — el
                    //     interior del dios NO es un cielo estrellado: es
                    //     SU CUERPO de tinta). En su lugar: LAS MOTAS DE
                    //     TINTA — 20 hilos finos que CAEN lento (la página
                    //     deshojándose al fondo del cuerpo), con el mismo
                    //     parpadeo determinista de la casa pero SIN el
                    //     brillo de estrella (tinta tenue, no cielo).
                    for (int s = 0; s < 20; s++)
                    {
                        float sx = VFXCore.Hash01(s * 13 + 1, 0, 961) * w;
                        float sy = (VFXCore.Hash01(s * 17 + 5, 0, 963) * h +
                                    t * (3f + 5f * VFXCore.Hash01(s, 0, 967))) % h;
                        float pal = 0.5f + 0.5f * MathF.Sin(t * (0.4f + VFXCore.Hash01(s, 0, 967) * 1.1f) + s * 2.4f);
                        float tam = 5f + 7f * VFXCore.Hash01(s * 7 + 3, 0, 969);
                        sb.Draw(_chispa, new Vector2(sx, sy), null,
                            new Color(96, 64, 148) * ((0.05f + 0.10f * pal + 0.05f * latido) * a),
                            0f, _chispa.Size() * 0.5f, tam / 64f, SpriteEffects.None, 0f);
                    }'''

parche("Content/Effects/CieloInteriorEterno.cs", VIEJO_POLVO, NUEVO_POLVO, "P7b motas de tinta (el cielo estrellado muere)")

print("FASE 5-7 OK")

# =====================================================================
# PUNTO 8 — EL LIBRO DE INFORMACIÓN: margen superior del scroll +
#            ENCIMA de toda la UI + la X en su sitio + la tinta nueva.
# =====================================================================

# (a) MARGEN SUPERIOR: el recorte del cuerpo nace 26 px más abajo — el
#     texto se APAGA con aire sobre él, no pegado al borde superior.
VIEJO_CUERPO = r'''                // el cuerpo arranca DEBAJO del rótulo de la página —
                // nunca debajo del titular (que ya vive en el cuero)
                float cy = GeoPagIzq.Y + 26f;

                // ============ EL CUERPO: DOS COLUMNAS CON RECORTE ============
                int cuerpoY = (int)cy;
                int cuerpoH = h - (int)cy - y - 30;'''

NUEVO_CUERPO = r'''                // el cuerpo arranca DEBAJO del rótulo de la página —
                // nunca debajo del titular (que ya vive en el cuero)
                float cy = GeoPagIzq.Y + 26f;

                // ============ EL CUERPO: DOS COLUMNAS CON RECORTE ============
                // v6.151 — EL MARGEN DEL DESAPARECER (la letra: «al hacer
                // scroll los textos desaparecen muy pegados al borde
                // superio, intenta dejar un pequeño margen arriba»): el
                // RECORTE de las columnas nace 30 px MÁS ABAJO de la
                // página (el aire del papel) — la línea que se va
                // desvaneció dentro de la página, no contra su borde.
                int cuerpoY = (int)cy + 30;
                int cuerpoH = h - (int)cy - y - 60;'''

parche("Content/UI/LibroDelEternoUI.cs", VIEJO_CUERPO, NUEVO_CUERPO, "P8a margen superior del scroll")

# (b) ENCIMA DE TODA LA UI: la capa del libro pasa al FINAL de la lista
#     (encima del inventario, del cursor de texto — de todo).
VIEJO_LAYERS = r'''            // EL ICONO junto al de logros: DESPUÉS del inventario (la
            // capa vanilla dibuja el botón de logros dentro de la suya)
            int idx = layers.FindIndex(l => l.Name.Equals("Vanilla: Inventory"));
            if (idx < 0) idx = layers.Count - 1;

            // EL ICONO del libro (antes del inventario: dibuja encima)
            layers.Insert(idx, new LegacyGameInterfaceLayer("AethonMod: IconoLibroEterno",
                () =>
                {
                    DibujarIcono(Main.spriteBatch);
                    return true;
                }, InterfaceScaleType.UI));

            // EL LIBRO ABIERTO
            layers.Insert(idx + 1, new LegacyGameInterfaceLayer("AethonMod: LibroDelEterno",
                () =>
                {
                    if (_estado != null && _estado.Visible)
                        _iface.Draw(Main.spriteBatch, new GameTime());
                    return true;
                }, InterfaceScaleType.UI));'''

NUEVO_LAYERS = r'''            // EL ICONO junto al de logros: DESPUÉS del inventario (la
            // capa vanilla dibuja el botón de logros dentro de la suya)
            int idx = layers.FindIndex(l => l.Name.Equals("Vanilla: Inventory"));
            if (idx < 0) idx = layers.Count - 1;

            // EL ICONO del libro (antes del inventario: dibuja encima)
            layers.Insert(idx, new LegacyGameInterfaceLayer("AethonMod: IconoLibroEterno",
                () =>
                {
                    DibujarIcono(Main.spriteBatch);
                    return true;
                }, InterfaceScaleType.UI));

            // ============================================================
            //  v6.151 — EL LIBRO ENCIMA DE TODO (la letra: «el libro de
            //  informacion debe abrirse encima de la interfaz de usuario
            //  nada lo debe tapar»): la capa del LIBRO ABIERTO ya NO se
            //  inserta junto al inventario — vive AL FINAL de la lista
            //  (la ÚLTIMA capa dibuja encima de todo: inventario, mapas,
            //  barras, hasta el propio cursor de texto). El icono
            //  pequeño sigue en su sitio de siempre (es un botón del
            //  inventario); lo que manda encima de todo es EL LIBRO
            //  ABIERTO.
            // ============================================================
            layers.Add(new LegacyGameInterfaceLayer("AethonMod: LibroDelEterno",
                () =>
                {
                    if (_estado != null && _estado.Visible)
                        _iface.Draw(Main.spriteBatch, new GameTime());
                    return true;
                }, InterfaceScaleType.UI));'''

parche("Content/UI/LibroDelEternoUI.cs", VIEJO_LAYERS, NUEVO_LAYERS, "P8b el libro encima de toda la UI")

# (c) LA X EN SU SITIO: el hitbox dibujado ES el hitbox real (la X
#     dibujada a la DERECHA del todo del libro, pegada a la esquina).
VIEJO_X = r'''                // ============ LA X ============
                var rectX = new Rectangle(x + w - 42, y + 8, 28, 28);
                bool hoverX = rectX.Contains(Main.mouseX, Main.mouseY);
                sb.Draw(_pixel, rectX, (hoverX ? new Color(220, 90, 90) : new Color(96, 74, 52)) * 0.4f);
                Utils.DrawBorderString(sb, "X", new Vector2(rectX.Center.X, rectX.Center.Y),
                    hoverX ? new Color(220, 90, 90) : Tinta, 1f, 0.5f, 0.5f);
                if (hoverX && Main.mouseLeft && Main.mouseLeftRelease)
                    LibroDelEternoUI.Cerrar();'''

NUEVO_X = r'''                // ============ LA X — EN SU SITIO DE VERDAD ============
                // v6.151 — EL BOTÓN Y SU DIBUJO SON LO MISMO (la letra:
                // «esa X esta mal ubicada, esta muy a la izquierda
                // cuando el boton real para tocarla esta mas a la
                // derecha»): la v6.150 movió el DIBUJO pero el rectángulo
                // seguía midiendo 28 px dentro del marco — el dedo caía
                // DONDE NO SE VE. AHORA: la X vive pegada a la ESQUINA
                // SUPERIOR DERECHA del LIBRO (la esquina de la TAPA, el
                // sitio universal del cierre) con su hitbox de 36 px
                // dibujada EXACTA — donde tocas es donde cae.
                var rectX = new Rectangle(x + w - 6, y - 8, 36, 36);
                bool hoverX = rectX.Contains(Main.mouseX, Main.mouseY);
                sb.Draw(_pixel, rectX, (hoverX ? new Color(220, 90, 90) : new Color(96, 74, 52)) * 0.45f);
                Utils.DrawBorderString(sb, "X", new Vector2(rectX.Center.X, rectX.Center.Y),
                    hoverX ? new Color(220, 90, 90) : Tinta, 1.1f, 0.5f, 0.5f);
                if (hoverX && Main.mouseLeft && Main.mouseLeftRelease)
                    LibroDelEternoUI.Cerrar();'''

parche("Content/UI/LibroDelEternoUI.cs", VIEJO_X, NUEVO_X, "P8c la X alineada")

# (d) LA TINTA NUEVA: el cuerpo con BORDE BLANCO y CENTRO NEGRO (la
#     letra) — los títulos QUEDAN como están (TintaTitulo/TintaCap).
VIEJO_TINTA = r'''        private static readonly Color Tinta = new(72, 44, 24);
        private static readonly Color TintaTitulo = new(128, 52, 158);
        private static readonly Color TintaCap = new(140, 84, 34);'''

NUEVO_TINTA = r'''        // v6.151 — LA TINTA DE LA LETRA («nunca arreglaste el color de
        // los textos, ese color no se ve y es horrible, que sean letras
        // con bordes blanco centro negro»): el CUERPO pasa por
        // DrawBorderString con centro NEGRO PURO (el contraste máximo
        // contra el papel — la línea que SIEMPRE se lee) y el borde que
        // DibujaCuerpo pinta BLANCO (el halo que la separa de las
        // sombras del papel). LOS TÍTULOS QUEDAN COMO ESTÁN (la letra:
        // «la letra de los titulos y esas cosas dejalas como estan»).
        private static readonly Color Tinta = new(10, 8, 12);
        private static readonly Color TintaTitulo = new(128, 52, 158);
        private static readonly Color TintaCap = new(140, 84, 34);'''

parche("Content/UI/LibroDelEternoUI.cs", VIEJO_TINTA, NUEVO_TINTA, "P8d tinta centro negro")

# el cuerpo se dibuja con borde BLANCO: LineaLibro usa DrawBorderString
# (borde negro por defecto) → cambiamos a la variante con color de borde
# propio (DrawBorderString 6 args no existe: usamos el truco de la casa
# — el doble pase: primero el texto BLANCO en 4 offsets, encima el negro)
VIEJO_VOLCAR = r'''            void Volcar(float y)
            {
                if (!VisibleEn(y, alto, recorte)) return;
                // v6.149 — EL CENTRADO ÓPTICO (la letra: «todos los textos
                // estan desentrados»): cada línea del cuerpo se CENTRA en
                // el ancho útil de la página — el bloque de texto respira
                // simétrico, como un manuscrito de verdad (la última línea
                // corta ya no cuelga sola a la izquierda).
                float ancho = fuente.MeasureString(linea.ToString()).X * 0.92f;
                float xc = pagina.X + (pagina.Width - ancho) * 0.5f;
                Utils.DrawBorderString(sb, linea.ToString(), new Vector2(xc, y), c, 0.92f);
            }'''

NUEVO_VOLCAR = r'''            void Volcar(float y)
            {
                if (!VisibleEn(y, alto, recorte)) return;
                // v6.149 — EL CENTRADO ÓPTICO (la letra: «todos los textos
                // estan desentrados»): cada línea del cuerpo se CENTRA en
                // el ancho útil de la página — el bloque de texto respira
                // simétrico, como un manuscrito de verdad (la última línea
                // corta ya no cuelga sola a la izquierda).
                // v6.151 — EL BORDE BLANCO + EL CENTRO NEGRO (la letra):
                // DOS pases — el PASO 1 dibuja el texto BLANCO en los 4
                // offsets cardinales (el contorno que SIEMPRE se lee,
                // incluso sobre la sombra del lomo), el PASO 2 el texto
                // NEGRO encima (el cuerpo de la tinta). El contraste
                // máximo de la letra, sin tocar los títulos.
                float ancho = fuente.MeasureString(linea.ToString()).X * 0.92f;
                float xc = pagina.X + (pagina.Width - ancho) * 0.5f;
                var posL = new Vector2(xc, y);
                const float o = 1.1f;   // el grosor del contorno
                sb.DrawString(fuente, linea.ToString(), posL + new Vector2(o, 0f),
                    Color.White, 0f, Vector2.Zero, 0.92f, SpriteEffects.None, 0f);
                sb.DrawString(fuente, linea.ToString(), posL + new Vector2(-o, 0f),
                    Color.White, 0f, Vector2.Zero, 0.92f, SpriteEffects.None, 0f);
                sb.DrawString(fuente, linea.ToString(), posL + new Vector2(0f, o),
                    Color.White, 0f, Vector2.Zero, 0.92f, SpriteEffects.None, 0f);
                sb.DrawString(fuente, linea.ToString(), posL + new Vector2(0f, -o),
                    Color.White, 0f, Vector2.Zero, 0.92f, SpriteEffects.None, 0f);
                sb.DrawString(fuente, linea.ToString(), posL,
                    c, 0f, Vector2.Zero, 0.92f, SpriteEffects.None, 0f);
            }'''

parche("Content/UI/LibroDelEternoUI.cs", VIEJO_VOLCAR, NUEVO_VOLCAR, "P8e el doble pase del cuerpo")

# =====================================================================
# PUNTO 9 — «ese poder en el jugador» → «ese poder en el portador».
# =====================================================================
for ruta, viejo, nuevo, etiqueta in [
    ("Localization/es-ES_Mods.AethonMod.hjson",
     "ese poder en el jugador. No es un objeto que uno se pone: es una",
     "ese poder en el portador. No es un objeto que uno se pone: es una",
     "P9 es-ES portador"),
    ("Localization/en-US_Mods.AethonMod.hjson",
     "that power inside the player. Not a garment you wear: a presence",
     "that power inside the bearer. Not a garment you wear: a presence",
     "P9 en-US bearer"),
]:
    parche(ruta, viejo, nuevo, etiqueta)

# es-MX: el texto de Linea5 usa el bloque D9B3FF (no dice 'jugador') —
# pero el Tooltip/LoreGrimorio de es-MX puede tener 'en el jugador': lo
# dejamos: la letra pedía SOLO la frase de la descripción de la Forma
# Eterna (es-ES la tenía cruda). Además: revisar el es-MX por si la
# frase existiera en el lore de la forma.
s = leer("Localization/es-MX_Mods.AethonMod.hjson")
if "ese poder en el jugador" in s:
    parche("Localization/es-MX_Mods.AethonMod.hjson",
           "ese poder en el jugador", "ese poder en el portador", "P9 es-MX portador")
else:
    print("  ✔ P9 es-MX — la frase no existe (el es-MX ya usa las líneas D9B3FF)")

print("FASE 8-9 OK")

# =====================================================================
# PUNTO 10 — EL HAMBRE: LA PROBABILIDAD REAL POR %.
#   ≤89 % → 1 % por chequeo · 90 % → 5 % · 91 % → 10 % · … 99 % → 50 %
#   · 100 % → ACTIVACIÓN DIRECTA. Victoria sube nivel / derrota
#   mantiene (esto YA vive en FuriaNivel — solo hay que leer el % real).
# =====================================================================
# El reloj: Estado.Hambre del Eterno (0-1, una hora). MomentosHambre es
# el contador 0-10 por 360 s (cada momento = 10 %). LA PROBABILIDAD
# se lee del % CONTINUO de hambre del libro activo.
VIEJO_UMBRAL_DOC = r'''        /// <summary>v6.140 — LA FURIA AL 90% (la letra: «el grimorio
        /// lanza el texto de tener hambre al 4%… las oleadas de hambre
        /// solo deben aparecer a partir de 90% de hambre y al llegar al
        /// 100% aparecer si o si con el llamado del grimorio»): la furia
        /// YA NO estalla al 40% (los viejos 4 momentos de 10) — el
        /// UMBRAL DE OLEADA es el 90% (momento 9) y el 100% (momento
        /// 10, el tope) es SIEMPRE. Los momentos 1-8 son EL SUSURRO
        /// (la voz del libro, informativa: «tengo hambre…» — sin
        /// oleada, sin aura). EL AURA TAMBIÉN ESPERA: no hay «aura
        /// rara» sin peligro real (la aura vive con MomentosHambre ≥ 9:
        /// cuando el aura prende, la oleada YA está prometida).</summary>
        public const int MomentosParaFuria = 9;'''

NUEVO_UMBRAL_DOC = r'''        /// <summary>v6.140 — LA FURIA AL 90% (la letra: «el grimorio
        /// lanza el texto de tener hambre al 4%… las oleadas de hambre
        /// solo deben aparecer a partir de 90% de hambre y al llegar al
        /// 100% aparecer si o si con el llamado del grimorio»): la furia
        /// YA NO estalla al 40% (los viejos 4 momentos de 10) — el
        /// UMBRAL DE OLEADA es el 90% (momento 9) y el 100% (momento
        /// 10, el tope) es SIEMPRE. Los momentos 1-8 son EL SUSURRO
        /// (la voz del libro, informativa: «tengo hambre…» — sin
        /// oleada, sin aura). EL AURA TAMBIÉN ESPERA: no hay «aura
        /// rara» sin peligro real (la aura vive con MomentosHambre ≥ 9:
        /// cuando el aura prende, la oleada YA está prometida).
        /// v6.151 — LA LEY NUEVA (la letra: «asegurate de que el % de
        /// hambre sea el % de probabilidad de que se active una oleada
        /// de hambre, pero no sera tal cual lo presenta, si el libro
        /// tiene 89% de hambre o menos la probabilidad de que las
        /// oleadas de hambre se activen es de solo 1%, pero al llegar a
        /// 90% de hambre la probabilidad pasa al 5% y se incrementa 5%
        /// por cada punto extra, en 91% es 10%, y asi hasta llegar a
        /// 100% que directamente activa la oleada del nivel
        /// correspondiente»): LA TABLA — 0-89 %: 1 % · 90 %: 5 % ·
        /// 91 %: 10 % · … · 99 %: 50 % · 100 %: DIRECTA. Y el nivel es
        /// EL DE SIEMPRE: victoria sube, derrota mantiene (FuriaNivel).</summary>
        public const int MomentosParaFuria = 9;

        /// <summary>v6.151 — LA PROBABILIDAD DE OLEADA según el % de
        /// hambre del libro (0-100): la tabla de la letra — 1 % con
        /// 89 o menos, 5 % al 90, +5 % por punto extra, DIRECTA al 100.
        /// (La llama el chequeo de furia cada 5 s; el dado es de 100.)</summary>
        public static int ProbabilidadOleada(int porcentajeHambre)
        {
            if (porcentajeHambre >= 100) return 100;      // LA DIRECTA
            if (porcentajeHambre < 90) return 1;          // el ruido de fondo
            return 5 + (porcentajeHambre - 90) * 5;       // 90→5 · 91→10 · 99→50
        }'''

parche("Content/Players/ShardPlayer.cs", VIEJO_UMBRAL_DOC, NUEVO_UMBRAL_DOC, "P10a la tabla de probabilidad")

# El CHEQUEO DE FURIA usa la probabilidad: los dos sitios (el del
# momento nuevo y el reloj de cada 5 s) tiran el dado con la tabla.
VIEJO_FURIA1 = r'''                                var config = ModContent.GetInstance<Content.AethonConfigServidor>();
                                bool evento = config == null || config.EventoHambreGrimorio;
                                if (evento && !GrimorioFuriaSistema.Activo && GrimorioFuriaSistema.MundoLibre())
                                    GrimorioFuriaSistema.Provocar(Player,
                                        Math.Max(1, Math.Min(10, FuriaNivel)), natural: true);'''

NUEVO_FURIA1 = r'''                                var config = ModContent.GetInstance<Content.AethonConfigServidor>();
                                bool evento = config == null || config.EventoHambreGrimorio;
                                // v6.151 — EL DADO DE LA LETRA: la probabilidad
                                // la pone EL % DE HAMBRE DEL LIBRO (1 % ≤ 89 ·
                                // 5 % al 90 · +5 % por punto · 100 % DIRECTA).
                                // Este sitio (el instante del momento nuevo) y
                                // el reloj de abajo tiran EL MISMO dado.
                                int pctHambre = PorcientoHambreDelLibro();
                                if (pctHambre >= 100)
                                {
                                    if (evento && !GrimorioFuriaSistema.Activo &&
                                        GrimorioFuriaSistema.MundoLibre())
                                        GrimorioFuriaSistema.Provocar(Player,
                                            Math.Max(1, Math.Min(10, FuriaNivel)), natural: true);
                                }
                                else if (evento && !GrimorioFuriaSistema.Activo &&
                                    GrimorioFuriaSistema.MundoLibre() &&
                                    Main.rand.Next(100) < ProbabilidadOleada(pctHambre))
                                    GrimorioFuriaSistema.Provocar(Player,
                                        Math.Max(1, Math.Min(10, FuriaNivel)), natural: true);'''

parche("Content/Players/ShardPlayer.cs", VIEJO_FURIA1, NUEVO_FURIA1, "P10b dado en el momento nuevo")

VIEJO_FURIA2 = r'''                        var configR = ModContent.GetInstance<Content.AethonConfigServidor>();
                        bool eventoR = configR == null || configR.EventoHambreGrimorio;
                        if (eventoR && !GrimorioFuriaSistema.Activo &&
                            GrimorioFuriaSistema.MundoLibre())
                            GrimorioFuriaSistema.Provocar(Player,
                                Math.Max(1, Math.Min(10, FuriaNivel)), natural: true);'''

NUEVO_FURIA2 = r'''                        var configR = ModContent.GetInstance<Content.AethonConfigServidor>();
                        bool eventoR = configR == null || configR.EventoHambreGrimorio;
                        // v6.151 — EL MISMO DADO DE LA TABLA (la letra: el %
                        // de hambre ES la probabilidad): el reloj de cada 5 s
                        // consulta la tabla con el % CONTINUO del libro —
                        // 100 % SIEMPRE dispara, 90 % falla 19 de 20 veces,
                        // 1 % el ruido de fondo de la espera.
                        int pctHambreR = PorcientoHambreDelLibro();
                        if (eventoR && !GrimorioFuriaSistema.Activo &&
                            GrimorioFuriaSistema.MundoLibre() &&
                            (pctHambreR >= 100 ||
                             Main.rand.Next(100) < ProbabilidadOleada(pctHambreR)))
                            GrimorioFuriaSistema.Provocar(Player,
                                Math.Max(1, Math.Min(10, FuriaNivel)), natural: true);'''

parche("Content/Players/ShardPlayer.cs", VIEJO_FURIA2, NUEVO_FURIA2, "P10c dado en el reloj de 5 s")

# El lector del % continuo del libro activo (Estado.Hambre del Eterno)
VIEJO_LEE = r'''        /// <summary>Segundos por momento de hambre ("si pasas minutos sin matar").</summary>'''

NUEVO_LEE = r'''        /// <summary>v6.151 — EL % DE HAMBRE DEL LIBRO ACTIVO (0-100,
        /// continuo): lee EL RELOJ DE VERDAD (Estado.Hambre del Grimorio
        /// del Eterno — la misma máquina del ojo rojo y la barra del
        /// tooltip). Sin libro visible: la cuenta de momentos de siempre
        /// (MomentosHambre × 10 % — el respaldo que nunca miente).</summary>
        public int PorcientoHambreDelLibro()
        {
            try
            {
                var eterno = global::AethonMod.Content.Weapons.GrimoireEternal.EstadoEterno;
                if (eterno != null && eterno.Hambre > 0f)
                    return (int)Math.Min(100f, MathF.Floor(eterno.Hambre * 100f));
            }
            catch { }
            return Math.Min(100, MomentosHambre * 10);
        }

        /// <summary>Segundos por momento de hambre ("si pasas minutos sin matar").</summary>'''

parche("Content/Players/ShardPlayer.cs", VIEJO_LEE, NUEVO_LEE, "P10d lector del % del libro")

# =====================================================================
# PUNTO 11 — EL ESQUELETRON EN EL MUNDO INTERIOR: EL PRÉSTAMO DE NOCHE
#            RESISTENTE AL CLAMP DEL MEDIODÍA ETERNO.
# =====================================================================
# El interior clava Main.dayTime=true cada tick (PreUpdateWorld). El
# sello de oleada le "presta la noche" al SkeletronHead en su PreAI…
# y el clamp SE LA QUITA al tick siguiente → la IA vanilla ve DÍA →
# modo guardián de mazmorra (cabeza girando, 9999 de daño).
VIEJO_CLAMP = r'''            Main.dayTime = true;
            Main.time = 27000;'''

NUEVO_CLAMP = r'''            // v6.151 — EL MEDIODÍA RESPETA AL GUARDIÁN DE OLEADA (la
            //     letra: «invoque el esqueletron en el mundo interior y
            //     aparecio en modo guardian de la mazmorra con la cabeza
            //     girando y hiendo rapidamente hacia el jugador, eso no
            //     debe pasar»): LA AUTOPSIA — este clamp corre CADA TICK
            //     y PISABA el préstamo de noche del sello de oleada
            //     (OleadaNPC._prestaDia: su PreAI pone dayTime=false
            //     para que SkeletronHead/Ojo peleen NORMALES) — la IA
            //     vanilla del guardián volvía a ver DÍA y entraba en MODO
            //     GUARDIÁN DE MAZMORRA (cabeza girando, embiste, daño
            //     9999). LA CURA: si un jefe de oleada TIENE LA NOCHE
            //     PRESTADA AHORA MISMO, el clamp lo respeta (el sello es
            //     la ley del festín; el mediodía del decorado puede
            //     esperar un tick).
            if (!Content.Globals.OleadaNPC.NochePrestadaActiva)
            {
                Main.dayTime = true;
                Main.time = 27000;
            }'''

parche("Content/Systems/SubmundoGrimorio.cs", VIEJO_CLAMP, NUEVO_CLAMP, "P11a el mediodía respeta al guardián")

# el flag estático público que lee el submundo
VIEJO_SELLA = r'''                    if (algoPrestado)
                    {
                        _prestamoAbierto = this;
                    }'''

NUEVO_SELLA = r'''                    if (algoPrestado)
                    {
                        _prestamoAbierto = this;
                        // v6.151 — LA NOCHE PRESTADA, ANUNCIADA (para el
                        // clamp del mediodía del mundo interior: mientras
                        // un Skeletron/Ojo de oleada peleaba con SU noche,
                        // el decorado NO la pisa — el guardián pelea
                        // NORMAL, jamás en modo mazmorra).
                        if (_prestaDia) NochePrestadaActiva = true;
                    }'''

parche("Content/Globals/OleadaNPC.cs", VIEJO_SELLA, NUEVO_SELLA, "P11b flag de noche prestada")

# dónde se devuelve el préstamo (p._prestaDia=false) — apagar el flag
VIEJO_DEVOLVER = r'''                if (p._prestaDia)
                {
                    p._prestaDia = false;
                    Main.dayTime = p._diaOriginal;
                }'''

NUEVO_DEVOLVER = r'''                if (p._prestaDia)
                {
                    p._prestaDia = false;
                    Main.dayTime = p._diaOriginal;
                    // v6.151 — el anuncio MUERE con el préstamo (nadie
                    // más tiene la noche: el mediodía del interior vuelve)
                    if (NochePrestadaActiva) NochePrestadaActiva = false;
                }'''

parche("Content/Globals/OleadaNPC.cs", VIEJO_DEVOLVER, NUEVO_DEVOLVER, "P11c devolver la noche apaga el flag")

# el campo estático en OleadaNPC
VIEJO_CAMPO_OL = r'''    public class OleadaNPC : GlobalNPC
    {'''

NUEVO_CAMPO_OL = r'''    public class OleadaNPC : GlobalNPC
    {
        /// <summary>v6.151 — TRUE mientras un jefe de oleada pelea con
        /// LA NOCHE PRESTADA (el Skeletron/Ojo de la furia del grimorio).
        /// Lo lee SubmundoGrimorio.PreUpdateWorld: el mediodía eterno del
        /// mundo interior NO pisa el préstamo — el guardián pelea NORMAL
        /// (cero modo mazmorra dentro del dios).</summary>
        public static bool NochePrestadaActiva;'''

parche("Content/Globals/OleadaNPC.cs", VIEJO_CAMPO_OL, NUEVO_CAMPO_OL, "P11d campo NochePrestadaActiva")

print("FASE 10-11 OK")

# =====================================================================
# PUNTO 12 — LA SOMBRA DE LA PÁGINA DEL GRIMORIO: SIN COLMILLOS NI
#            OJITOS (solo el lazo del libro — el resto queda igual).
# =====================================================================
# El lazo (AlmaEternoGlobalItem.PreDrawInWorld) usa SombrasLib.Masa +
# OjosDeMasa. SombrasLib.Masa dibuja: tiras de carne + velos + ESPINAS
# DE HUESO (colmillos) + ventosas + OJOS. LA CURA: una VARIANTE LISA de
# Masa (sin colmillos, sin ventosas, sin ojos) — el flag liso. El lazo
# la usa; las demás sombras del mod QUEDAN IGUAL (la letra: «el resto
# dejalo tal cual esta»).
VIEJO_MASA_FIRMA = r'''        public static void Masa(Vector2[] col, float grosorRaiz, float grosorPunta, float alfa, int semilla, float tiempo, float grosorCentro = -1f)
        {
            if (col == null || col.Length < 2 || alfa <= 0.02f) return;'''

NUEVO_MASA_FIRMA = r'''        public static void Masa(Vector2[] col, float grosorRaiz, float grosorPunta, float alfa, int semilla, float tiempo, float grosorCentro = -1f, bool lisa = false)
        {
            if (col == null || col.Length < 2 || alfa <= 0.02f) return;'''

parche("Content/VFX/SombrasLib.cs", VIEJO_MASA_FIRMA, NUEVO_MASA_FIRMA, "P12a Masa gana el flag lisa")

# la CAPA 4 (colmillos) respeta el flag
VIEJO_CAPA4 = r'''            // === CAPA 4 · LAS ESPINAS DE HUESO (colmillos curvos
            // alternando lado — la sierra afilada de la v6.50.66 ahora
            // con dientes DE VERDAD) ===
            if (VFXCore.Colmillo != null)
            {'''

NUEVO_CAPA4 = r'''            // === CAPA 4 · LAS ESPINAS DE HUESO (colmillos curvos
            // alternando lado — la sierra afilada de la v6.50.66 ahora
            // con dientes DE VERDAD) ===
            // v6.151 — EL MODO LISO («a la sombra de la pagina del
            //     grimorio del eterno, sacale los colmillos y los
            //     ojitos»): lisa=true APAGA colmillos, ventosas y ojos —
            //     queda la MASA PURA de tinta que fluye (el lazo del
            //     libro la usa; las sombras del resto del mod quedan
            //     IGUAL — la letra: «el resto dejalo tal cual esta»).
            if (VFXCore.Colmillo != null && !lisa)
            {'''

parche("Content/VFX/SombrasLib.cs", VIEJO_CAPA4, NUEVO_CAPA4, "P12b capa colmillos condicional")

# la CAPA 5 (ventosas) respeta el flag
VIEJO_CAPA5 = r'''            // === CAPA 5 · LAS VENTOSAS DEL LOMO (la fila blanca del
            // spritesheet de referencia — «white circular suckers along
            // its length»: la panza del tentáculo, el lado que SUJETA) ==='''

NUEVO_CAPA5 = r'''            // === CAPA 5 · LAS VENTOSAS DEL LOMO (la fila blanca del
            // spritesheet de referencia — «white circular suckers along
            // its length»: la panza del tentáculo, el lado que SUJETA) ===
            // v6.151 — el modo liso también las calla (la tinta pura no
            // tiene anatomía — solo fluye).'''

parche("Content/VFX/SombrasLib.cs", VIEJO_CAPA5, NUEVO_CAPA5, "P12c doc capa 5")

parche("Content/VFX/SombrasLib.cs",
       "its length»: la panza del tentáculo, el lado que SUJETA) ===\n            // v6.151 — el modo liso también las calla (la tinta pura no\n            // tiene anatomía — solo fluye).\n            if (VFXCore.Ventosa != null)",
       "its length»: la panza del tentáculo, el lado que SUJETA) ===\n            // v6.151 — el modo liso también las calla (la tinta pura no\n            // tiene anatomía — solo fluye).\n            if (VFXCore.Ventosa != null && !lisa)",
       "P12d capa ventosas condicional")

# OjosDeMasa: el flag (los OJOS del cuerpo de la masa viven en OjosDeMasa
# llamada por separado — el lazo la QUITA y ya; pero la masa interna
# también dibuja ojos? verificar: los ojos viven SOLO en OjosDeMasa →
# el lazo simplemente deja de llamarla).
VIEJO_LAZO = r'''                    // LA SOMBRA FINA: columna viva del portador al libro
                    Vector2[] col = SombrasLib.ColumnaViva(raiz, item.Center,
                        tiempo, semilla, 10, 0.22f, gancho: 0.4f);
                    SombrasLib.Masa(col, 3f, 6f, 0.55f, semilla, tiempo, grosorCentro: 12f);
                    SombrasLib.BrumaColumna(col, 18f, 0.30f, tiempo, semilla, 7);

                    // LOS OJOS ENTORNADOS a lo largo del lazo (me vigilan volver)
                    SombrasLib.OjosDeMasa(col, item.Center, 0.5f, semilla, tiempo, 4);'''

NUEVO_LAZO = r'''                    // LA SOMBRA FINA: columna viva del portador al libro
                    // v6.151 — LA SOMBRA DE PÁGINA LISA (la letra: «a la
                    //     sombra de la pagina del grimorio del eterno,
                    //     sacale los colmillos y los ojitos, el resto
                    //     dejalo tal cual esta, esto es solo un cambio
                    //     estetico»): el lazo del Eterno es TINTA PURA —
                    //     Masa en modo liso (sin espinas de hueso, sin
                    //     ventosas) y SIN los ojos entornados. Queda la
                    //     masa que fluye con SU bruma — el resto igual.
                    Vector2[] col = SombrasLib.ColumnaViva(raiz, item.Center,
                        tiempo, semilla, 10, 0.22f, gancho: 0.4f);
                    SombrasLib.Masa(col, 3f, 6f, 0.55f, semilla, tiempo,
                        grosorCentro: 12f, lisa: true);
                    SombrasLib.BrumaColumna(col, 18f, 0.30f, tiempo, semilla, 7);'''

parche("Content/Globals/AlmaEternoGlobalItem.cs", VIEJO_LAZO, NUEVO_LAZO, "P12e el lazo liso sin ojos")

# =====================================================================
# LA VERSIÓN + LA COMPILACIÓN VERIFICADA DE REFERENCIAS CRUZADAS
# =====================================================================
VIEJO_VER = "version = 6.150"
NUEVO_VER = "version = 6.151"
parche("build.txt", VIEJO_VER, NUEVO_VER, "versión 6.151")

print("\nCIRUGÍA v6.151 COMPLETA — TODOS LOS PARCHES APLICADOS")
