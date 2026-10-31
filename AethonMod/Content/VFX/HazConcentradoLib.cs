using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.VFX
{
    // ====================================================================
    //  v6.56.9 — LOS SOLES RÚNICOS DEL RC2 (la auditoría de librerías,
    //  Task C: la casa decide el destino de HazConcentradoLib).
    //
    //  LA PURGA: el camino GPU portado a armas (Dibujar · AberracionCromatica
    //  · ObtenerShader · DiscoNegro · todos sus pozos de remuestreo, ~313
    //  líneas) MURIÓ con la purga de las 9 variantes de la v6.56.6 — sus
    //  consumidores (Alba/Veredicto/Juicio/Prisma/Partidor/Ira) ya no
    //  existen y la búsqueda negativa lo certificó (auditoría §3: «sin
    //  llamador NI externo NI interno»). Lo que VIVE aquí es lo que el RC2
    //  usa de verdad: LOS SOLES RÚNICOS DE LA CARGA Y DEL ESTALLIDO y el
    //  destello de 4 puntas (la estrella conducto).
    //
    //  LA IMPLOSIÓN (la letra del usuario, v6.56.9: «sobre los brillos de
    //  soles, no deben dar vuelta hacia el centro, lo que deben hacer es
    //  aparecer lejos y contraerse hacia el centro pero sin dar vueltas,
    //  como una implosión de brillos de soles»): LA CURA es la muerte del
    //  término de deriva angular — el ángulo de cada sol es SU HASH, FIJO
    //  de por vida (cero órbita, cero vuelta) y el radio NACE LEJOS
    //  (150..240 px) y CAE hacia el centro con ease-in (vida²: lento
    //  lejos, acelerando al caer — el tirón de la absorción). La lectura
    //  en pantalla: los soles APARECEN en el borde lejano del conjuro y
    //  se desploman rectos hacia la boca del arma. Con la carga el
    //  enjambre entero se APRIETA (la banda lejana encoge a ×0.55) y el
    //  hervidero aviva (los nacimientos aceleran) — la implosión crece
    //  hasta el disparo.
    //
    //  (v6.56.6, el origen de los soles: «debes usar los mismos brillos
    //  que usa el accesorio de El brillo de soles runicos» — LA RECETA ES
    //  DE BrilloSolarHalo: EL DESTELLO DE 4 PUNTAS, dos cápsulas cruzadas
    //  de blanco incandescente + el corazón dorado.)
    // ====================================================================
    public static class HazConcentradoLib
    {
        /// <summary>EL DESTELLO DE 4 PUNTAS — la firma del sol rúnico:
        /// dos cápsulas cruzadas de blanco incandescente + el corazón
        /// dorado. Se dibuja en el LOTE ADITIVO de VFXCore (el llamador
        /// abre Begin/FlushAdditive). Receta verbatim de BrilloSolarHalo.
        /// [size] en px: cápsulas 2.2·size, núcleo 1.5·size.
        /// [corazon] (v6.57) — LA LEY DEL CORAZÓN DORADO: la escala del
        /// corazón (1 = el calibre del accesorio). El usuario clavó el
        /// síntoma en el haz: «son como circulos amarillos que aparecen
        /// a veces y son demasiado grandes, no deben aparecer» — el
        /// corazón dorado es un Quad de glow radial a ×1.5 del tamaño:
        /// a escala de HAZ (soles de 30-40 px) se lee como un CÍRCULO
        /// AMARILLO gigante flotando en el conducto. Los haces lo pasan
        /// a 0 — queda la estrella blanca de 4 puntas pura.
        /// [colorCorazon] (v6.59) — LA CURA DE LOS BRILLITOS APAGADOS
        /// (la letra: «los brillitos ahora casi no se ven»): al quitar
        /// el corazón dorado (v6.57) los soles quedaron SOLO dos cápsulas
        /// de 1,6 px al 55 % — un titileo casi invisible. Sin devolver
        /// el círculo amarillo: (1) las cápsulas ARDEN de verdad (95 %
        /// y GROSOR QUE ESCALA con el tamaño: 0,055·size — el destello
        /// se LEE a cualquier escala) y (2) el corazón puede vestir OTRO
        /// color (blanco-violeta en el haz): un NÚCLEO pequeño que
        /// aviva el centro SIN leerse como círculo amarillo. El dorado
        /// por defecto NO cambia (el accesorio y el RC2 original siguen
        /// exactamente igual).</summary>
        public static void SolRunico4Puntas(Vector2 pos, float size, float arde,
            float doradoExtra = 0f, float corazon = 1f, Color? colorCorazon = null)
        {
            if (arde <= 0.03f || size <= 0.4f) return;
            // v6.59 — EL GROSOR VIVE: 0,055·size (mín. 1,8 px) — antes el
            // clamp fijo de 1,6 px dejaba los soles grandes casi filamentos.
            float fino = MathF.Max(1.8f, 0.055f * size) * arde;
            Color blanco = new Color(255, 248, 225) * (0.95f * arde);
            Color dorado = new Color(255, 214, 120) * ((0.50f + doradoExtra) * arde);
            VFXCore.Quad(pos, blanco, new Vector2(size * 2.2f, fino));
            VFXCore.Quad(pos, blanco, new Vector2(fino, size * 2.2f));
            if (corazon > 0.02f)
            {
                float lado = size * 1.5f * corazon;
                VFXCore.Quad(pos, colorCorazon ?? dorado, new Vector2(lado, lado));
            }
        }

        /// <summary>LA IMPLOSIÓN DE SOLES (v6.56.9): cada destello de 4
        /// puntas NACE LEJOS (por defecto 150..240 px, ángulo FIJO de su
        /// hash — NADA de orbitar) y se CONTRAE RECTO hacia el centro
        /// durante su vida (ease-in vida²: el tirón de la absorción). La
        /// banda lejana se aprieta con la carga (×1.15→×0.55) y el
        /// hervidero aviva. Pura función de (progreso, tiempo):
        /// determinista, sin estado.
        /// [radioLejos]/[radioVar] (v6.57) — LA LEJANÍA DEL NACIMIENTO:
        /// el usuario pidió soles «mas lejos del jugador» — las copias
        /// del RC2 nacen a 320..520 px (el original conserva 150/90:
        /// comportamiento intacto por defecto).
        /// [corazon] (v6.57) — LA LEY DEL CORAZÓN DORADO: 0 = sin corazón
        /// (el haz NO dibuja círculos amarillos). [colorCorazon] (v6.59)
        /// — el corazón pequeño que AVIVA sin leerse amarillo (el haz
        /// pasa blanco-violeta).</summary>
        public static void SolesRunicosCarga(Vector2 centro, float progreso, float tiempo,
            float radioLejos = 150f, float radioVar = 90f, float corazon = 1f,
            Color? colorCorazon = null)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return;
            progreso = MathHelper.Clamp(progreso, 0f, 1f);
            const int cuenta = 11;
            // LA BANDA LEJANA: nace lejos y TODO el enjambre cae adentro
            // conforme el conjuro madura (la implosión se aprieta).
            float banda = MathHelper.Lerp(1.15f, 0.55f, progreso);
            float velocidad = 0.42f + 1.5f * progreso;   // el hervidero aviva
            for (int i = 0; i < cuenta; i++)
            {
                float h = VFXCore.Hash01(31, 300 + i, 7);
                float vida = (tiempo * velocidad * 0.42f + h * 3f) % 1f;
                float arde = MathF.Sin(vida * MathHelper.Pi);
                if (arde < 0.05f) continue;

                // EL ÁNGULO ES FIJO (v6.56.9 — la letra: «sin dar
                // vueltas»): SU hash de nacimiento, CERO deriva angular.
                // El sol cae por la RECTA RADIAL de su ángulo.
                float ang = VFXCore.Hash01(31, 310 + i, 11) * MathHelper.TwoPi;

                // LA CONTRACCIÓN: lejos → cerca con ease-in (vida²) —
                // lento en el borde, acelerando hacia la boca: el tirón.
                float rLejos = (radioLejos + radioVar * VFXCore.Hash01(31, 320 + i, 13)) * banda;
                float caida = vida * vida;
                float dist = MathHelper.Lerp(rLejos, 8f, caida);

                Vector2 pos = centro + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * dist;
                float size = (0.30f + 0.25f * VFXCore.Hash01(31, 330 + i, 17))
                             * 34f * (0.55f + 0.45f * progreso);
                SolRunico4Puntas(pos, size, arde, corazon: corazon, colorCorazon: colorCorazon);
            }
        }

        /// <summary>EL ESTALLIDO DE SOLES: al disparar, los destellos de 4
        /// puntas salen EXPULSADOS en abanico por la dirección del rayo
        /// (±0.5 rad) y arden muriendo — el arma escupe lo que absorbió.
        /// [edad01] 0→1 durante los primeros ~22 ticks del disparo.
        /// [corazon] (v6.57) — LA LEY DEL CORAZÓN DORADO: 0 = sin corazón.
        /// [colorCorazon] (v6.59) — el corazón pequeño que AVIVA sin
        /// leerse amarillo.</summary>
        public static void SolesRunicosEstallido(Vector2 centro, float angulo, float edad01,
            float tiempo, float corazon = 1f, Color? colorCorazon = null)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return;
            edad01 = MathHelper.Clamp(edad01, 0f, 1f);
            const int cuenta = 16;
            for (int i = 0; i < cuenta; i++)
            {
                float a = angulo + (VFXCore.Hash01(77, 500 + i, 7) - 0.5f) * 1.0f;
                float v = 6f + 9f * VFXCore.Hash01(77, 510 + i, 11);
                float arr = 14f + 26f * VFXCore.Hash01(77, 520 + i, 13);
                float arde = MathF.Sin((1f - edad01) * MathHelper.Pi)
                             * (0.6f + 0.4f * VFXCore.Hash01(77, 530 + i, 17));
                Vector2 pos = centro + new Vector2(MathF.Cos(a), MathF.Sin(a))
                              * (arr + v * edad01 * 22f);
                float size = (0.34f + 0.30f * VFXCore.Hash01(77, 540 + i, 19)) * 34f;
                SolRunico4Puntas(pos, size, arde, corazon: corazon, colorCorazon: colorCorazon);
            }
        }
    }
}
