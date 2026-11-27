using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using ReLogic.Content;

namespace AethonMod.Content.VFX
{
    // ======================================================================
    //  HazProyectadoLib — v6.178 — EL RAYO CONCENTRADO DE VERDAD.
    //
    //  LA LETRA: «el visor del proyectado no se ve como el arma real, se
    //  ve horrible y mal hecho, lo que me importa es el ataque que hace
    //  debes copiar su tecnica ya que esta tecnica es basica en muchos
    //  mods y puedes investigarlo para que notes que muchos mods famosos
    //  usan esa misma tecnica para crear rayos concentrados».
    //
    //  LA TÉCNICA (el plano del haz concentrado, reimplementada el
    //  100 % con código y texturas DE LA CASA — la técnica es básica
    //  en muchos mods famosos, y esta es la nuestra, v6.54.0):
    //  la misma línea dibujada en SEIS capas que se funden:
    //
    //    (1) LA ENVOLVENTE LEJANA — la rodaja del tubo estirada a lo
    //        largo de TODO el haz (ProyeccionHaz: perfil suave en Y) +
    //        sus DOS TAPAS redondas (ProyeccionBorde) que desvanecen
    //        los extremos — ×3.6 del ancho, el tinte EXTERIOR.
    //    (2) LA ENVOLVENTE MEDIA — lo mismo a ×1.9, el tinte MEDIO.
    //    (3) EL CUERPO QUE FLUYE — ProyeccionCuerpo (256×64, fBm
    //        TILEABLE en X) en TRAMOS ALINEADOS a los píxeles de la
    //        textura (la textura fluye SIN estirarse: se REPITE con su
    //        escala nativa y la costura jamás se ve) con los BORDES
    //        ONDULANTES del Contorno (3 octavas de seno que VIAJAN con
    //        el flujo + las puntas redondas de sqrt).
    //    (4) EL SEGUNDO FLUJO — ProyeccionFlujo a ×0.7 de altura y
    //        ×1.8 de velocidad, el hervor fino que cruza al cuerpo.
    //    (5) LOS BORDES QUE ARDEN — la rodaja del tubo pegada ARRIBA y
    //        ABAJO del contorno ondulante: el filo vivo del haz.
    //    (6) LAS DOS LUCES — el punto del extremo (el choque contra el
    //        mundo) y la estrella de la boca (el destello del origen).
    //
    //  LA GEOMETRÍA DEL CONTORNO (el corazón de la técnica): el ancho
    //  a la distancia d late con senos que dependen de (d/ancho −
    //  t·velocidad/ancho) — la onda VIAJA por el haz a la velocidad
    //  del flujo, y los extremos se cierran en curva sqrt (el haz nace
    //  y muere REDONDO, jamás cortado).
    //
    //  Contrato: coordenadas de PANTALLA (el llamador resta
    //  Main.screenPosition). Todo determinista (R5: cero azar).
    // ======================================================================
    public static class HazProyectadoLib
    {
        /// <summary>Los tres tintes del haz: el de afuera, el medio y el corazón.</summary>
        public struct Paleta
        {
            public Color Exterior;
            public Color Medio;
            public Color Nucleo;
        }

        /// <summary>EL HAZ ENTERO en una estructura: la línea, el ritmo y la vida.</summary>
        public struct Parametros
        {
            public Vector2 Inicio;        // pantalla
            public Vector2 Direccion;     // normalizada
            public float Largo;
            public float Ancho;
            public float Tiempo;
            public float Intensidad;      // 0..1 (el haz vivo)
            public float Destello;        // 0..1.5 (la boca que estalla)
            public bool Impacto;          // ¿el extremo pega contra algo?
            public float VelocidadFlujo;  // px/s del plasma
            public float Ondulacion;      // la altura de las olas del borde
            public float Turbulencia;     // la tercera octava (la fina)
            public Paleta Colores;
        }

        public const float FactorCuerpo = 1.5f;

        // === LAS TEXTURAS DE LA CASA (forjadas por tools/gen_proyeccion_v6178.py) ===
        private static Asset<Texture2D> _haz, _borde, _cuerpo, _flujo;
        private static bool _cargadas;

        private static void Cargar()
        {
            if (_cargadas) return;
            try
            {
                _haz = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/ProyeccionHaz", AssetRequestMode.AsyncLoad);
                _borde = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/ProyeccionBorde", AssetRequestMode.AsyncLoad);
                _cuerpo = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/ProyeccionCuerpo", AssetRequestMode.AsyncLoad);
                _flujo = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/ProyeccionFlujo", AssetRequestMode.AsyncLoad);
                _cargadas = true;
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("HazProyectadoLib", exRegistro); }
        }

        /// <summary>¿La textura base del cuerpo ya vive? (para caer al tirón de
        /// RayoStrip si el asset no cargó — el arma JAMÁS se rompe).</summary>
        public static bool TexturasVivas
        {
            get { Cargar(); return _haz != null && _haz.IsLoaded && _cuerpo != null && _cuerpo.IsLoaded; }
        }

        // ==================================================================
        //  EL DIBUJO ENTERO — las seis capas sobre la misma línea.
        // ==================================================================
        public static void Dibujar(Parametros p)
        {
            if (p.Intensidad <= 0.01f && p.Destello <= 0.01f) return;
            Cargar();
            if (_haz == null || !_haz.IsLoaded) return;   // el fallback del llamador manda

            Texture2D haz = _haz.Value, borde = _borde.Value;
            Texture2D cuerpo = _cuerpo.Value, flujo = _flujo.Value;
            // (el brillo de los extremos: el SoftGlow de la casa — el 128² radial)
            Texture2D brillo = VFXCore.SoftGlow;

            Vector2 perp = new(-p.Direccion.Y, p.Direccion.X);
            float rot = p.Direccion.ToRotation();
            Vector2 fin = p.Inicio + p.Direccion * p.Largo;

            if (p.Intensidad > 0.01f && p.Largo > 4f)
            {
                float w = p.Ancho * FactorCuerpo * p.Intensidad *
                          (1f + 0.04f * MathF.Sin(p.Tiempo * 0.9f));
                float repeticion = p.Ancho * 8f;
                float difuminado = w * 1.2f;

                // (1) + (2) LAS ENVOLVENTES — el tubo suave y sus tapas
                FranjaSuave(haz, borde, p.Inicio, p.Direccion, rot, p.Largo, w * 3.6f, difuminado,
                    p.Colores.Exterior * 0.5f);
                FranjaSuave(haz, borde, p.Inicio, p.Direccion, rot, p.Largo, w * 1.9f, difuminado,
                    p.Colores.Medio * 0.45f);

                // (3) EL CUERPO QUE FLUYE + (4) EL SEGUNDO FLUJO
                float n3 = MathHelper.Clamp(p.Intensidad * 1.5f, 0f, 1f);
                Desplazar(cuerpo, p.Inicio, perp, rot, in p, w, repeticion,
                    p.Tiempo * p.VelocidadFlujo, Color.White * n3);
                Desplazar(flujo, p.Inicio, perp, rot, in p, w, repeticion * 0.7f,
                    p.Tiempo * p.VelocidadFlujo * 1.8f,
                    new Color(255, 170, 160) * (0.35f * n3));

                // (5) LOS BORDES QUE ARDEN
                BrilloBordes(haz, p.Inicio, perp, rot, in p, w,
                    new Color(255, 235, 230) * (0.55f * n3));

                // (6a) LA LUZ DEL EXTREMO
                float escBrillo = w * 1.8f / brillo.Width;
                Main.spriteBatch.Draw(brillo, fin, null, p.Colores.Medio * (0.45f * n3),
                    0f, brillo.Size() * 0.5f, escBrillo, SpriteEffects.None, 0f);
                if (p.Impacto)
                {
                    float n4 = 1f + 0.15f * MathF.Sin(p.Tiempo * 0.8f);
                    Main.spriteBatch.Draw(brillo, fin, null, p.Colores.Medio * 0.8f,
                        0f, brillo.Size() * 0.5f, w * 2.4f * n4 / brillo.Width, SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(brillo, fin, null, p.Colores.Nucleo * 0.9f,
                        0f, brillo.Size() * 0.5f, w * 1.1f / brillo.Width, SpriteEffects.None, 0f);
                }
            }

            // (6b) LA ESTRELLA DE LA BOCA — el destello del origen
            if (p.Destello > 0.01f)
            {
                float d = (1f + 0.12f * MathF.Sin(p.Tiempo * 0.6f)) * p.Destello;
                float a = p.Ancho;
                Main.spriteBatch.Draw(brillo, p.Inicio, null, p.Colores.Exterior * (0.8f * d),
                    0f, brillo.Size() * 0.5f, a * 3f * d / brillo.Width, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(brillo, p.Inicio, null, p.Colores.Nucleo * (1f * d),
                    0f, brillo.Size() * 0.5f, a * 1.2f * d / brillo.Width, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(brillo, p.Inicio, null, p.Colores.Medio * (0.8f * d),
                    0f, brillo.Size() * 0.5f,
                    new Vector2(a * 5f * d / brillo.Width, a * 0.4f * d / brillo.Height),
                    SpriteEffects.None, 0f);
            }
        }

        // ==================================================================
        //  EL CONTORNO — el ancho ondulante a la distancia d (LA firma de
        //  la técnica): tres octavas de seno que viajan con el flujo, las
        //  puntas cerradas en sqrt (el haz nace y muere redondo).
        // ==================================================================
        private static void Contorno(in Parametros p, float w, float d,
            out float arriba, out float abajo)
        {
            float mitad = w * 0.5f;
            float vivo = 1f;
            if (d < mitad)
            {
                float e = 1f - d / mitad;
                vivo = MathF.Sqrt(MathF.Max(0f, 1f - e * e));          // la punta de arriba
            }
            if (d > p.Largo - mitad)
            {
                float e = 1f - (p.Largo - d) / mitad;
                vivo = MathF.Min(vivo, MathF.Sqrt(MathF.Max(0f, 1f - e * e)));   // la de abajo
            }
            float faseX = d / p.Ancho;
            float faseT = p.Tiempo * p.VelocidadFlujo / p.Ancho;
            float suave = Utils.GetLerpValue(0f, w * 1.2f, d, true);    // el arranque no ondula
            float olas = p.Ondulacion * suave;
            float s1 = 0.10f * MathF.Sin(0.9f * (faseX - faseT)) +
                        0.06f * MathF.Sin(2.3f * (faseX - faseT * 1.3f) + 1.7f) +
                        p.Turbulencia * 0.035f * MathF.Sin(5.1f * (faseX - faseT * 0.8f) + 0.4f);
            float s2 = 0.10f * MathF.Sin(0.9f * (faseX - faseT) + 2.1f) +
                        0.06f * MathF.Sin(2.6f * (faseX - faseT * 1.2f) + 0.3f) +
                        p.Turbulencia * 0.035f * MathF.Sin(4.7f * (faseX - faseT * 0.9f) + 2.9f);
            arriba = mitad * (1f + olas * s1) * vivo;
            abajo = mitad * (1f + olas * s2) * vivo;
        }

        // ==================================================================
        //  LA ENVOLVENTE — la rodaja estirada de punta a punta + las dos
        //  tapas que desvanecen los extremos hacia afuera.
        // ==================================================================
        private static void FranjaSuave(Texture2D haz, Texture2D borde, Vector2 inicio,
            Vector2 dir, float rot, float largo, float ancho, float difuminado, Color color)
        {
            Main.spriteBatch.Draw(haz, inicio, null, color, rot,
                new Vector2(0f, haz.Height * 0.5f),
                new Vector2(largo / haz.Width, ancho / haz.Height), SpriteEffects.None, 0f);
            Vector2 origen = new(borde.Width, borde.Height * 0.5f);
            Vector2 escala = new(difuminado / borde.Width, ancho / borde.Height);
            Main.spriteBatch.Draw(borde, inicio + dir, null, color, rot, origen, escala, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(borde, inicio + dir * (largo - 1f), null, color, rot + MathF.PI,
                origen, escala, SpriteEffects.None, 0f);
        }

        // ==================================================================
        //  EL FLUJO — la textura que corre a lo largo del haz en TRAMOS
        //  ALINEADOS a sus píxeles (se repite con su escala NATIVA: la
        //  costura nunca se ve) + los tramos finos de las puntas.
        // ==================================================================
        private static void Desplazar(Texture2D tex, Vector2 ini, Vector2 perp, float rot,
            in Parametros p, float w, float repeticion, float desplazamiento, Color color)
        {
            float k = tex.Width / repeticion;                  // px de mundo por px de textura
            float fase = -desplazamiento * k;
            float zona = ZonaPunta(in p, w);
            TramosAlineados(tex, ini, perp, rot, in p, w, k, fase, zona, p.Largo - zona, color);
            TramosFinos(tex, ini, perp, rot, in p, w, k, fase, 0f, zona, color);
            TramosFinos(tex, ini, perp, rot, in p, w, k, fase, p.Largo - zona, p.Largo, color);
        }

        private static float ZonaPunta(in Parametros p, float w)
            => MathF.Min(w * 0.55f, p.Largo * 0.5f);

        /// <summary>LOS TRAMOS ALINEADOS: los cortes caen en los límites de los
        /// píxeles de la textura — el muestreo por bandas no estira NADA.</summary>
        private static void TramosAlineados(Texture2D tex, Vector2 ini, Vector2 perp, float rot,
            in Parametros p, float w, float k, float fase, float d0, float d1, Color color)
        {
            if (d1 <= d0) return;
            int paso = Math.Max(1, (int)MathF.Round(4f * k));
            float desde = d0 * k + fase, hasta = d1 * k + fase;
            int c0 = (int)MathF.Floor(desde / paso), c1 = (int)MathF.Ceiling(hasta / paso);
            for (int i = c0; i < c1; i++)
            {
                float a = MathF.Max(i * paso, desde);
                float b = Mathf_Min((i + 1) * paso, hasta);
                if (b - a < 0.05f) continue;
                int sx = Modulo((int)MathF.Floor(a), tex.Width);
                int sw = Math.Clamp((int)MathF.Round(b - a), 1, tex.Width - sx);
                DibujarTramo(tex, ini, perp, rot, in p, w, (a - fase) / k, (b - a) / k, sx, sw, color);
            }
        }

        private static float Mathf_Min(float a, float b) => a < b ? a : b;

        /// <summary>LOS TRAMOS FINOS de las puntas: a 1 px de textura por tramo —
        /// el borde redondo se muestrea con su propia resolución.</summary>
        private static void TramosFinos(Texture2D tex, Vector2 ini, Vector2 perp, float rot,
            in Parametros p, float w, float k, float fase, float d0, float d1, Color color)
        {
            for (float d = d0; d < d1; d += 1f)
            {
                float largo = MathF.Min(1f, d1 - d);
                if (largo < 0.05f) continue;
                int sx = Modulo((int)MathF.Floor(d * k + fase), tex.Width);
                DibujarTramo(tex, ini, perp, rot, in p, w, d, largo, sx, 1, color);
            }
        }

        /// <summary>UN TRAMO del cuerpo: la banda de textura con el ancho del
        /// contorno ondulante a esa distancia (la textura es la carne;
        /// el contorno es la silueta).</summary>
        private static void DibujarTramo(Texture2D tex, Vector2 ini, Vector2 perp, float rot,
            in Parametros p, float w, float d, float largo, int sx, int sw, Color color)
        {
            Vector2 pos = ini + p.Direccion * d;
            if (pos.X < -600f || pos.Y < -600f ||
                pos.X > Main.screenWidth + 600f || pos.Y > Main.screenHeight + 600f) return;
            Contorno(in p, w, d + largo * 0.5f, out float arriba, out float abajo);
            float alto = arriba + abajo;
            if (alto < 0.5f) return;
            Vector2 centro = pos + perp * ((abajo - arriba) * 0.5f);
            Main.spriteBatch.Draw(tex, centro,
                new Rectangle?(new Rectangle(sx, 0, sw, tex.Height)), color, rot,
                new Vector2(0f, tex.Height * 0.5f),
                new Vector2(largo / sw, alto / tex.Height), SpriteEffects.None, 0f);
        }

        /// <summary>LOS BORDES QUE ARDEN: la rodaja del tubo pegada ARRIBA y
        /// ABAJO del contorno (el filo vivo que sigue a las olas).</summary>
        private static void BrilloBordes(Texture2D haz, Vector2 ini, Vector2 perp, float rot,
            in Parametros p, float w, Color color)
        {
            Vector2 origen = new(0f, haz.Height * 0.5f);
            var visor = new Rectangle(-600, -600, Main.screenWidth + 1200, Main.screenHeight + 1200);
            float zona = ZonaPunta(in p, w);
            float d = 0f;
            while (d < p.Largo)
            {
                float paso = MathF.Min(d < zona || d > p.Largo - zona ? 1f : 6f, p.Largo - d);
                float d0 = d;
                d += paso;
                if (paso < 0.05f) continue;
                Vector2 pos = ini + p.Direccion * d0;
                if (!visor.Contains((int)pos.X, (int)pos.Y)) continue;
                Contorno(in p, w, d0 + paso * 0.5f, out float arriba, out float abajo);
                float g = w * 0.7f * MathF.Min(1f, (arriba + abajo) / w);
                if (g < 0.5f) continue;
                Vector2 escala = new(paso / haz.Width, g / haz.Height);
                Main.spriteBatch.Draw(haz, pos - perp * arriba, null, color, rot, origen, escala, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(haz, pos + perp * abajo, null, color, rot, origen, escala, SpriteEffects.None, 0f);
            }
        }

        private static int Modulo(int x, int n) => (x % n + n) % n;
    }
}
