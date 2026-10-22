using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace AethonMod.Content.Bestiary
{
    // ======================================================================
    //  LAS DIEZ ESTRELLAS NEGRAS DEL GRIMORIO DORMIDO (v6.51.1) — LA
    //  LETRA: «su rareza debe ser al igual que el arma super unica y
    //  legendario, 5 estrellas amarillas no son suficientes, asi que
    //  ponle 10 estrellas negras brillantes y titilantes».
    //
    //  El catálogo del juego solo sabe contar hasta cinco estrellas
    //  amarillas (la fórmula de ContentSamples las CLAMPEA en 5) —
    //  así que el grimorio estrena SU PROPIA línea de rareza: DIEZ
    //  estrellas NEGRO-VIOLETAS que TITILAN (cada una a su compás:
    //  brillo, escala y latido propios — deterministas por índice) con
    //  su chispa de luz.
    //
    //  v6.51.2 — LA CIRUGÍA DEL CATÁLOGO (el fix del playtest: «sigue
    //  teniendo una rareza de 4 de 5 estrellas, no las 10 estrellas que
    //  pedí» — las amarillas venían del NPCPortraitInfoElement de
    //  vanilla, que GrimorioDormido.SetBestiary ahora sustituye por un
    //  retrato SIN estrellas): este elemento VIVE en la categoría del
    //  RETRATO (ICategorizedBestiaryInfoElement → Portrait — la MISMA
    //  zona donde estaban las amarillas, bajo el nombre) y con
    //  OrderPriority máxima va PRIMERO dentro de su categoría — lo
    //  primero que lee la ficha es lo que el grimorio ES.
    // ======================================================================
    public class EstrellasNegrasInfoElement : IBestiaryInfoElement,
        IProvideSearchFilterString, ICategorizedBestiaryInfoElement,
        IBestiaryPrioritizedElement
    {
        /// <summary>LA CATEGORÍA: el cuadro del RETRATO — donde vivían
        /// las cinco estrellas amarillas de vanilla.</summary>
        public UIBestiaryEntryInfoPage.BestiaryInfoCategory ElementCategory
            => UIBestiaryEntryInfoPage.BestiaryInfoCategory.Portrait;

        /// <summary>PRIMERA dentro de su categoría (las líneas de sabor
        /// van después de la fila de estrellas).</summary>
        public float OrderPriority => 10f;

        public UIElement ProvideUIElement(BestiaryUICollectionInfo info)
        {
            var elem = new EstrellasNegrasElemento();
            elem.Width.Set(0f, 1f);
            elem.Height.Set(30f, 0f);
            elem.IgnoresMouseInteraction = true;
            return elem;
        }

        public string GetSearchString(ref BestiaryUICollectionInfo info) => "";

        /// <summary>LA FILA DE DIEZ ESTRELLAS (el dibujo en persona).</summary>
        private class EstrellasNegrasElemento : UIElement
        {
            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                try
                {
                    CalculatedStyle dim = GetDimensions();
                    Vector2 centro = new Vector2(dim.X + dim.Width * 0.5f, dim.Y + dim.Height * 0.5f);

                    Texture2D estrella = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/Star").Value;
                    Texture2D glow = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                    if (estrella == null || glow == null) return;

                    float t = Main.GlobalTimeWrappedHourly;
                    const int total = 10;
                    const float paso = 22f;

                    for (int i = 0; i < total; i++)
                    {
                        // CADA ESTRELLA SU COMPÁS: el titileo (tres senos
                        // inconmensurables — el brillo NUNCA se repite igual).
                        float titila = 0.55f + 0.45f * MathF.Sin(
                            t * (1.3f + 0.17f * i) + i * 2.71f);
                        float escala = 0.85f + 0.30f * MathF.Sin(
                            t * (0.9f + 0.11f * i) + i * 1.13f);

                        Vector2 pos = centro + new Vector2((i - (total - 1) * 0.5f) * paso,
                            MathF.Sin(t * 0.8f + i * 1.9f) * 3f);

                        // EL HALO VIOLETA (la luz que EMANA de lo negro: sin
                        // él, la estrella negra no se leería sobre el panel).
                        spriteBatch.Draw(glow, pos, null,
                            new Color(120, 60, 190) * (0.30f * titila), 0f,
                            glow.Size() * 0.5f, (16f * escala) / glow.Size().X,
                            SpriteEffects.None, 0f);

                        // LA ESTRELLA NEGRA: negro-violeta profundo con su
                        // borde que arde (el negro puro sería un agujero —
                        // esto es un AGUJERO QUE BRILLA).
                        Color negra = new Color(26, 12, 44);
                        spriteBatch.Draw(estrella, pos, null, negra, 0f,
                            estrella.Size() * 0.5f, escala, SpriteEffects.None, 0f);
                        // EL BORDE TITILANTE (el rim vivo de la nada).
                        spriteBatch.Draw(estrella, pos, null,
                            new Color(178, 120, 235) * (0.65f * titila), 0f,
                            estrella.Size() * 0.5f, escala * 1.18f, SpriteEffects.None, 0f);

                        // LA CHISPA (el corazón de luz que late en el centro).
                        spriteBatch.Draw(glow, pos, null,
                            new Color(232, 210, 255) * (0.55f * titila), 0f,
                            glow.Size() * 0.5f, (6f + 3f * titila) / glow.Size().X,
                            SpriteEffects.None, 0f);
                    }
                }
                catch { }
            }
        }
    }

    // ======================================================================
    //  v6.51.3 — LA ETIQUETA ✦ LEGENDARIO SUPREMO ✦ DE LA FICHA (la
    //  letra: «creo que la etiqueta ✦ LEGENDARIO SUPREMO ✦ deberia estar
    //  mejor en el bestiario en la info del grimorio… ademas el color de
    //  eso tambien deben incluir negro con pequeños brillos en su nombre,
    //  me acabo de dar cuenta que los soles runicos tienen como un brillo
    //  especial, bueno es como ese brillo, este brilo esta en los soles
    //  runicos de 3 anillos en adelante»).
    //
    //  EL RÓTULO ES NEGRO (la rareza que se lee como AUSENCIA de color —
    //  el negro del grimorio) y a su alrededor nacen y mueren LOS
    //  DESTELLOS DE 4 PUNTAS de los soles rúnicos de 3 anillos+
    //  (RuneSunRenderer.DrawTwinkles, el brillo especial: dos cápsulas
    //  cruzadas de blanco incandescente + el corazón dorado): la MISMA
    //  firma visual del sol, destilada en el nombre de su rareza. Para
    //  que el NEGRO se lea sobre el panel azul de la ficha, la palabra
    //  lleva un SOPLADO violeta-oscuro detrás (el halo de la nada — el
    //  texto flota sobre su propia sombra).
    // ======================================================================
    public class EtiquetaSupremaInfoElement : IBestiaryInfoElement,
        IProvideSearchFilterString, ICategorizedBestiaryInfoElement,
        IBestiaryPrioritizedElement
    {
        /// <summary>LA CATEGORÍA: el cuadro del RETRATO — la PRIMERÍSIMA
        /// línea de la ficha (por encima hasta de las diez estrellas
        /// negras: primero SE ES, después se cuenta).</summary>
        public UIBestiaryEntryInfoPage.BestiaryInfoCategory ElementCategory
            => UIBestiaryEntryInfoPage.BestiaryInfoCategory.Portrait;

        /// <summary>PRIMERA de la categoría (las estrellas van debajo).</summary>
        public float OrderPriority => 11f;

        public UIElement ProvideUIElement(BestiaryUICollectionInfo info)
        {
            var elem = new EtiquetaSupremaElemento();
            elem.Width.Set(0f, 1f);
            elem.Height.Set(34f, 0f);
            elem.IgnoresMouseInteraction = true;
            return elem;
        }

        public string GetSearchString(ref BestiaryUICollectionInfo info) => "";

        /// <summary>EL RÓTULO EN PERSONE: el texto NEGRO + sus destellos.</summary>
        private class EtiquetaSupremaElemento : UIElement
        {
            /// <summary>La fuente del rótulo, medida UNA sola vez.</summary>
            private static Vector2 _medida = Vector2.Zero;

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                try
                {
                    CalculatedStyle dim = GetDimensions();
                    Vector2 centro = new Vector2(dim.X + dim.Width * 0.5f, dim.Y + dim.Height * 0.5f);

                    var fuente = Terraria.GameContent.FontAssets.MouseText.Value;
                    Texture2D glow = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/SoftGlow").Value;
                    if (fuente == null || glow == null) return;

                    string texto = "✦ LEGENDARIO SUPREMO ✦";
                    if (_medida == Vector2.Zero)
                        _medida = fuente.MeasureString(texto);
                    Vector2 origen = _medida * 0.5f;

                    float t = Main.GlobalTimeWrappedHourly;

                    // === LOS DESTELLOS DE 4 PUNTAS (DrawTwinkles, el brillo
                    //     especial de los soles de 3 anillos+ — portado al
                    //     espacio de la ficha): nacen, arden y mueren a lo
                    //     LARGO del rótulo, cada uno a su compás. ===
                    const int destellos = 7;
                    for (int i = 0; i < destellos; i++)
                    {
                        float vida = (t * 0.42f + i * 0.371f) % 1f;      // su reloj propio
                        float arde = MathF.Sin(vida * MathHelper.Pi);    // nace → arde → muere
                        if (arde < 0.08f) continue;

                        // Su sitio: repartido por el largo del rótulo (con su
                        // deriva fija — determinista, sin RNG).
                        float f = (i + 0.5f) / destellos;
                        float cabeceo = 0.5f * MathF.Sin(t * 0.7f + i * 1.9f);
                        Vector2 pos = centro + new Vector2(
                            (f - 0.5f) * (_medida.X + 26f),
                            (cabeceo - 0.28f) * _medida.Y +
                            MathF.Sin(t * 0.9f + i * 2.3f) * 3f);

                        float size = (7f + 4f * (i % 3)) * arde;
                        // LAS DOS CÁPSULAS CRUZADAS (el trazo del destello).
                        spriteBatch.Draw(glow, pos, null,
                            new Color(255, 248, 225) * (0.85f * arde), 0f,
                            glow.Size() * 0.5f,
                            new Vector2(size * 2.3f, 2.2f) / glow.Size(),
                            SpriteEffects.None, 0f);
                        spriteBatch.Draw(glow, pos, null,
                            new Color(255, 248, 225) * (0.85f * arde), 0f,
                            glow.Size() * 0.5f,
                            new Vector2(2.2f, size * 2.3f) / glow.Size(),
                            SpriteEffects.None, 0f);
                        // EL CORAZÓN DORADO (el núcleo del brillo solar).
                        spriteBatch.Draw(glow, pos, null,
                            new Color(255, 214, 120) * (0.70f * arde), 0f,
                            glow.Size() * 0.5f,
                            new Vector2(size * 1.15f, size * 1.15f) / glow.Size(),
                            SpriteEffects.None, 0f);
                    }

                    // === EL SOPLADO (la sombra violeta-oscuro que hace
                    //     LEGIBLE el negro sobre el panel de la ficha). ===
                    for (int s = 0; s < 4; s++)
                    {
                        Vector2 off = new Vector2(
                            MathF.Cos(s * MathHelper.PiOver2 + 0.4f),
                            MathF.Sin(s * MathHelper.PiOver2 + 0.4f)) * 2.2f;
                        spriteBatch.DrawString(fuente, texto, centro + off,
                            new Color(96, 48, 158) * 0.55f, 0f, origen, 1f,
                            SpriteEffects.None, 0f);
                    }

                    // === EL RÓTULO NEGRO (el color de la rareza del
                    //     grimorio: NEGRO con el vivo violeta del borde —
                    //     un negro que ARDE por fuera, como las estrellas
                    //     de su fila). ===
                    spriteBatch.DrawString(fuente, texto, centro,
                        new Color(18, 8, 30), 0f, origen, 1f,
                        SpriteEffects.None, 0f);
                }
                catch { }
            }
        }
    }
}
