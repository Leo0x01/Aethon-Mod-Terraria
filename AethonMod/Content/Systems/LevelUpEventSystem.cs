using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace AethonMod.Content.Systems
{
    /// <summary>
    /// Evento global de PRIMERA subida de nivel de un arma.
    ///
    /// Al dispararse:
    /// 1. Temblor de pantalla (camera shake manual, compatible con cualquier tModLoader).
    /// 2. Overlay fullscreen borroso + granulado (oscurece + ruido).
    /// 3. Texto de lore profundo centrado en pantalla, con fade in/out.
    /// 4. El tiempo del mundo avanza un día completo (día → noche → día) de forma visible.
    ///
    /// El evento solo dispara UNA vez por personaje (flag persistente en cada item).
    /// </summary>
    public class LevelUpEventSystem : ModSystem
    {
        // === Estado del evento ===
        public static bool IsActive = false;
        public static int Timer = 0;          // ticks transcurridos desde el inicio
        public const int Duration = 600;        // ~10 segundos a 60fps

        // === Offset de cámara manual (reemplaza Main.screenShake que no existe en todas las versiones) ===
        public static Vector2 ShakeOffset = Vector2.Zero;
        private static int _shakeIntensity = 0;

        // === Lore mostrado (centrado, multilinea) ===
        const string LoreEs =
            "El Fragmento Génesis despierta.\n\n" +
            "Antes de las estrellas, antes del tiempo,\n" +
            "Aethon, la Luz Primordial, libró una guerra\n" +
            "contra el vacío que devoraba todo nombre.\n\n" +
            "Tu arma es ahora un eco de esa guerra.\n" +
            "Cada nivel la acerca un paso más\n" +
            "al filo que partió la oscuridad primordial.\n\n" +
            "El cielo tiembla. Un día entero transcurre\n" +
            "mientras el Fragmento reconoce a su portador.";
        const string LoreEn =
            "The Genesis Shard awakens.\n\n" +
            "Before the stars, before time,\n" +
            "Aethon, the Primordial Light, waged war\n" +
            "against the void that devoured every name.\n\n" +
            "Your weapon is now an echo of that war.\n" +
            "Each level brings it one step closer\n" +
            "to the edge that split the primordial dark.\n\n" +
            "The sky trembles. A full day passes\n" +
            "as the Shard recognizes its bearer.";

        // === Sprites de grano (generados en runtime) ===
        private static Texture2D _grainTexture;
        private static Color[] _grainData;
        private const int GrainSize = 64;

        // === MagicPixel cacheado (textura 1x1 blanca de vanilla) ===
        // Usamos ModContent.Request en vez de TextureAssets.MagicPixel para evitar
        // dependencias de namespace y ser compatibles con cualquier version de tModLoader.
        private static Texture2D _magicPixel;

        public override void Load()
        {
            // No crear texturas en servidor dedicado (GraphicsDevice es null).
            if (Main.dedServ) return;
            _grainData = new Color[GrainSize * GrainSize];
            _grainTexture = new Texture2D(Main.graphics.GraphicsDevice, GrainSize, GrainSize);
            // Cachear MagicPixel (textura vanilla 1x1 blanca)
            _magicPixel = ModContent.Request<Texture2D>("Terraria/Images/MagicPixel").Value;
        }

        public override void Unload()
        {
            _grainTexture?.Dispose();
            _grainTexture = null;
            _grainData = null;
            _magicPixel = null;
            IsActive = false;
            Timer = 0;
            ShakeOffset = Vector2.Zero;
            _shakeIntensity = 0;
        }

        /// <summary>
        /// Dispara el evento. Solo llamado una vez por personaje.
        /// </summary>
        public static void Trigger()
        {
            if (IsActive) return;
            IsActive = true;
            Timer = 0;
            _shakeIntensity = 40; // intensidad inicial del temblor
            // Sonido cósmico
            Terraria.Audio.SoundEngine.PlaySound(SoundID.DD2_EtherianPortalOpen);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar);
        }

        public override void PostUpdateInput()
        {
            if (!IsActive) return;

            Timer++;

            // === Temblor de pantalla (offset manual, decae con el tiempo) ===
            if (Timer < Duration * 2 / 3)
            {
                int intensity = (int)(40f * (1f - (float)Timer / (Duration * 2f / 3f)));
                if (intensity < 4) intensity = 4;
                _shakeIntensity = intensity;
            }
            else
            {
                _shakeIntensity = 0;
            }
            // Offset aleatorio para simular el temblor
            if (_shakeIntensity > 0)
            {
                ShakeOffset = new Vector2(
                    Main.rand.NextFloat(-_shakeIntensity, _shakeIntensity),
                    Main.rand.NextFloat(-_shakeIntensity, _shakeIntensity));
            }
            else
            {
                ShakeOffset = Vector2.Zero;
            }

            // === Avance acelerado del tiempo (día → noche → día, ciclo completo) ===
            // En Terraria: día = 0..32400 (Main.dayTime=true), noche = 0..21600 (Main.dayTime=false).
            // Un ciclo completo = 54000 ticks. Lo distribuimos sobre Duration (600 frames).
            int timePerFrame = 54000 / Duration; // ~90 ticks/frame
            Main.time += timePerFrame;

            // Alternar día/noche correctamente
            if (Main.dayTime && Main.time >= 32400)
            {
                Main.time -= 32400;
                Main.dayTime = false;
            }
            else if (!Main.dayTime && Main.time >= 21600)
            {
                Main.time -= 21600;
                Main.dayTime = true;
            }

            // Sonido ambiental cada ~2s durante el evento
            if (Timer % 120 == 0 && Timer < Duration - 60)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4);
            }

            // Final del evento
            if (Timer >= Duration)
            {
                IsActive = false;
                Timer = 0;
                _shakeIntensity = 0;
                ShakeOffset = Vector2.Zero;
                // Asegurar que el día termine en punto razonable (mañana)
                Main.time = 0;
                Main.dayTime = true;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4);
            }
        }

        /// <summary>
        /// Modifica la posición de la cámara para aplicar el temblor.
        /// Se llama después de que la cámara vanilla ya se calculó.
        /// </summary>
        public override void ModifyScreenPosition()
        {
            if (IsActive && ShakeOffset != Vector2.Zero)
            {
                Main.screenPosition += ShakeOffset;
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (!IsActive) return;

            int layerIdx = layers.FindIndex(l => l.Name.Equals("Vanilla: Interface Logic 2"));
            if (layerIdx == -1) layerIdx = layers.Count;

            layers.Insert(layerIdx, new LegacyGameInterfaceLayer("AethonMod: LevelUpEvent",
                () =>
                {
                    DrawEventOverlay();
                    return true;
                }, InterfaceScaleType.UI));
        }

        /// <summary>
        /// Dibuja el overlay del evento: oscurecimiento + grano + texto de lore.
        /// </summary>
        private void DrawEventOverlay()
        {
            if (!IsActive) return;
            var sb = Main.spriteBatch;
            float progress = (float)Timer / Duration;

            // === Fade in (0..0.15) y fade out (0.85..1) ===
            float alpha;
            if (progress < 0.15f) alpha = progress / 0.15f;
            else if (progress > 0.85f) alpha = (1f - progress) / 0.15f;
            else alpha = 1f;
            if (alpha < 0f) alpha = 0f;
            if (alpha > 1f) alpha = 1f;

            // === 1. Oscurecimiento (vigneta oscura) ===
            int darkAlpha = (int)(180 * alpha);
            sb.Draw(_magicPixel,
                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                new Color(6, 4, 14, darkAlpha));

            // === 2. Grano (noise) ===
            for (int i = 0; i < _grainData.Length; i++)
            {
                byte v = (byte)Main.rand.Next(0, 80);
                _grainData[i] = new Color(v, v, v, (byte)(60 * alpha));
            }
            _grainTexture.SetData(_grainData);

            int tilesX = (Main.screenWidth / GrainSize) + 2;
            int tilesY = (Main.screenHeight / GrainSize) + 2;
            for (int x = 0; x < tilesX; x++)
            {
                for (int y = 0; y < tilesY; y++)
                {
                    sb.Draw(_grainTexture,
                        new Vector2(x * GrainSize, y * GrainSize),
                        Color.White);
                }
            }

            // === 3. Brillo dorado pulsante en el centro ===
            float pulse = 0.6f + 0.4f * (float)System.Math.Sin(Timer * 0.15f);
            int glowR = (int)(Main.screenHeight * 0.5f);
            for (int r = glowR; r > 0; r -= 20)
            {
                int a = (int)(8 * pulse * alpha * (1f - (float)r / glowR));
                if (a < 0) a = 0;
                sb.Draw(_magicPixel,
                    new Rectangle(Main.screenWidth / 2 - r, Main.screenHeight / 2 - r, r * 2, r * 2),
                    new Color(245, 196, 81, a));
            }

            // === 4. Texto de lore centrado ===
            bool isSpanish = Terraria.Localization.Language.ActiveCulture != null &&
                Terraria.Localization.Language.ActiveCulture.Name != null &&
                Terraria.Localization.Language.ActiveCulture.Name.StartsWith("es");
            string lore = isSpanish ? LoreEs : LoreEn;
            string[] lines = lore.Split('\n');
            float scale = 0.95f;
            Vector2 basePos = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
            float lineHeight = 22f;
            float totalH = lines.Length * lineHeight;
            Vector2 start = new Vector2(basePos.X, basePos.Y - totalH / 2f);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrEmpty(line)) continue;
                Vector2 size = Terraria.GameContent.FontAssets.MouseText.Value.MeasureString(line) * scale;
                Vector2 pos = new Vector2(start.X - size.X / 2f, start.Y + i * lineHeight);
                Utils.DrawBorderString(sb, line, pos + new Vector2(2, 2),
                    new Color(0, 0, 0, (int)(220 * alpha)), scale);
                Color textColor = new Color(245, 220, 160, (int)(255 * alpha));
                Utils.DrawBorderString(sb, line, pos, textColor, scale);
            }

            // === 5. Barra de progreso sutil en la parte inferior ===
            int barW = 400;
            int barH = 4;
            int barX = (Main.screenWidth - barW) / 2;
            int barY = Main.screenHeight - 60;
            sb.Draw(_magicPixel,
                new Rectangle(barX, barY, barW, barH),
                new Color(40, 30, 60, (int)(180 * alpha)));
            sb.Draw(_magicPixel,
                new Rectangle(barX, barY, (int)(barW * progress), barH),
                new Color(245, 196, 81, (int)(255 * alpha)));
        }
    }
}
