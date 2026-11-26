#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""cirugia_v6177_aspiradora.py — la aspiradora unificada y pulida.

v6.177 (la letra):
  · «lo probe con el Devoramundos en las oleadas de hambre y no lo
    devora correctamente» — el cine debe esperar a que la EXTENSIÓN de
    la oleada termine de alargar la cadena (antes capturaba a mitad de
    la construcción).
  · «la animacion de devorar al Devoramundos esta bien, pero tiene que
    mejorar, uno de los tentaculos se queda medio rezagado con respecto
    a los otros» — el vaivén de las piezas llevaba FRECUENCIAS distintas
    (i%5, i%3): una pieza siempre leía más lenta que las demás. Ahora
    TODAS ondean a la MISMA frecuencia con SU fase propia — la manada se
    mueve junta (el patrón de la casa: fase por pieza, frecuencia una).
"""
import re

# ══ 1. GrimorioFuriaSistema: la sonda pública de la extensión ══════
R1 = 'AethonMod/Content/Systems/GrimorioFuriaSistema.cs'
s = open(R1, encoding='utf-8').read()
viejo = """        /// <summary>Pieza del Devorador nacida (la llama Marcar): el censo se refresca.</summary>
        public static void PiezaDevoradorNacio(float mult)"""
nuevo = """        /// <summary>v6.177 — ¿La EXTENSIÓN de la cadena del Devorador
        /// sigue pendiente? (la aspiradora lo consulta en LA VIGILIA:
        /// el cine NO captura al gusano mientras la oleada lo sigue
        /// alargando — la letra: «no lo devora correctamente»).</summary>
        public static bool ExtensionDevoradorPendiente => _pendCabeza >= 0;

        /// <summary>v6.177 — LA ASPIRADORA MANDA: cancela la extensión
        /// pendiente (el cine capturó al gusano ENTERO — que la oleada
        /// no le cosa piezas a mitad de flotación).</summary>
        public static void CancelarExtensionDevorador()
        {
            _pendCabeza = -1;
            _pendTicks = 0;
            _pendIntentos = 0;
        }

        /// <summary>Pieza del Devorador nacida (la llama Marcar): el censo se refresca.</summary>
        public static void PiezaDevoradorNacio(float mult)"""
assert viejo in s, 'PiezaDevoradorNacio no encontrada'
s = s.replace(viejo, nuevo, 1)
open(R1, 'w', encoding='utf-8', newline='\n').write(s)
print('OK GrimorioFuriaSistema: sonda + cancel de la extensión')

# ══ 2. AspiradoraDevoradorProjectile: vigilía + captura + vaivén ═══
R2 = 'AethonMod/Content/Projectiles/Sombras/AspiradoraDevoradorProjectile.cs'
s = open(R2, encoding='utf-8').read()

# 2a — LA VIGILIA espera a la extensión de la oleada
viejo = """                bool completo = _tickEstable >= 90;
                if (t >= T_ESPERA || (completo && t >= 120f))
                    Capturar();"""
nuevo = """                // v6.177 — LA EXTENSIÓN DE LA OLEADA TAMBIÉN CUENTA (la
                // letra: «no lo devora correctamente» en las oleadas): la
                // cadena NO está completa mientras la oleada la siga
                // alargando — el tope de 5 s de la letra manda igual.
                bool extensionPendiente =
                    Systems.GrimorioFuriaSistema.ExtensionDevoradorPendiente;
                bool completo = _tickEstable >= 90 && !extensionPendiente;
                if (t >= T_ESPERA || (completo && t >= 120f))
                    Capturar();"""
assert viejo in s, 'la vigilía no se encontró'
s = s.replace(viejo, nuevo, 1)

# 2b — LA CAPTURA cancela la extensión
viejo = """            _capturada = true;
            CosecharSecciones(cabeza);"""
nuevo = """            _capturada = true;
            // v6.177 — LA OLEADA SUELTA LA CADENA: el cine la captura
            // ENTERA — que la extensión no le cosa piezas a mitad de vuelo
            Systems.GrimorioFuriaSistema.CancelarExtensionDevorador();
            CosecharSecciones(cabeza);"""
assert viejo in s, 'la captura no se encontró'
s = s.replace(viejo, nuevo, 1)

# 2c — EL VAIVÉN UNIFORME (la letra: «uno de los tentaculos se queda
#     medio rezagado») — frecuencia UNA, fase por pieza
viejo = """                    // EL VAIVÉN INDIVIDUAL: cada pieza SU fase, SU periodo
                    float fase = i * 1.73f;
                    base_ += new Vector2(
                        MathF.Sin(t * (0.19f + 0.013f * (i % 5)) + fase) * 9f,
                        MathF.Cos(t * (0.16f + 0.011f * (i % 3)) + fase * 1.3f) * 7f) * sube;"""
nuevo = """                    // v6.177 — EL VAIVÉN DE LA MANADA (la letra: «uno de
                    // los tentaculos se queda medio rezagado con respecto
                    // a los otros»): las frecuencias DISTINTAS por pieza
                    // (i%5, i%3) hacían que unas piezas ondearan MÁS
                    // LENTO que otras — el tentáculo rezagado. Ahora TODAS
                    // ondean a la MISMA frecuencia con SU FASE propia
                    // (el patrón de la casa: la manada se mueve junta,
                    // cada cuerpo en su momento).
                    float fase = i * 1.73f;
                    base_ += new Vector2(
                        MathF.Sin(t * 0.19f + fase) * 9f,
                        MathF.Cos(t * 0.16f + fase * 1.3f) * 7f) * sube;"""
assert viejo in s, 'el vaivén no se encontró'
s = s.replace(viejo, nuevo, 1)

open(R2, 'w', encoding='utf-8', newline='\n').write(s)
print('OK AspiradoraDevoradorProjectile: vigilía + captura + vaivén uniforme')
