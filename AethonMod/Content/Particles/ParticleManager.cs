using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Particles
{
    /// <summary>
    /// ParticleManager — orquestador central del sistema de partículas.
    /// Mantiene un buffer pre-asignado, actualiza todas las partículas cada frame,
    /// y las renderiza con batching por blend mode.
    ///
    /// Basado en: "Librería de Partículas para Terraria - Referencia para IA"
    /// Secciones 14 y 19.1
    /// </summary>
    public class ParticleManager : ModSystem
    {
        private const int DEFAULT_CAPACITY = 2000;

        private static ParticleBuffer _buffer;
        private static Texture2D[] _textures;
        private static int _textureCount;

        public static int ActiveCount => _buffer?.ActiveCount ?? 0;
        public static int Capacity => _buffer?.Capacity ?? 0;

        public override void OnModLoad()
        {
            if (Terraria.ID.NetmodeID.Server == Main.netMode) return;
            _buffer = new ParticleBuffer(DEFAULT_CAPACITY);
            _textures = new Texture2D[64];
            _textureCount = 0;

            // Registrar texturas procedurales
            RegisterTexture("AethonMod/Content/Effects/Procedural/SoftGlow");   // ID 0
            RegisterTexture("AethonMod/Content/Effects/Procedural/Trail");       // ID 1
            RegisterTexture("AethonMod/Content/Effects/Procedural/Star");        // ID 2
            RegisterTexture("AethonMod/Content/Effects/Procedural/Slash");       // ID 3
            RegisterTexture("AethonMod/Content/Effects/Procedural/Vortex");      // ID 4
            RegisterTexture("AethonMod/Content/Effects/Procedural/Ring");        // ID 5
            RegisterTexture("AethonMod/Content/Effects/Procedural/Crescent");     // ID 6
            RegisterTexture("AethonMod/Content/Effects/Procedural/Noise");       // ID 7
        }

        public override void Unload()
        {
            _buffer = null;
            _textures = null;
            _textureCount = 0;
        }

        /// <summary>
        /// Registra una textura y devuelve su ID.
        /// </summary>
        public static ushort RegisterTexture(string path)
        {
            if (_textureCount >= _textures.Length) return 0;
            _textures[_textureCount] = ModContent.Request<Texture2D>(path).Value;
            return (ushort)(_textureCount++);
        }

        /// <summary>
        /// Helper para empaquetar Color a uint.
        /// </summary>
        public static uint PackColor(Color c) =>
            (uint)c.R | ((uint)c.G << 8) | ((uint)c.B << 16) | ((uint)c.A << 24);

        /// <summary>
        /// Helper para desempaquetar uint a Color.
        /// </summary>
        public static Color UnpackColor(uint packed) => new Color(
            (byte)(packed & 0xFF),
            (byte)((packed >> 8) & 0xFF),
            (byte)((packed >> 16) & 0xFF),
            (byte)((packed >> 24) & 0xFF));

        /// <summary>
        /// Spawnea una partícula en el mundo.
        /// </summary>
        public static void Spawn(ParticleData data)
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server) return;
            _buffer?.TrySpawn(data);
        }

        /// <summary>
        /// Actualiza todas las partículas activas.
        /// </summary>
        public override void PreUpdateDusts()
        {
            if (Terraria.ID.NetmodeID.Server == Main.netMode || _buffer == null) return;

            var particles = _buffer.RawData;
            for (int i = 0; i < _buffer.Capacity; i++)
            {
                ref ParticleData p = ref particles[i];
                if (!p.IsActive) continue;

                // Actualizar posición
                p.Position += p.Velocity;

                // Actualizar rotación
                p.Rotation += p.RotationSpeed;

                // Component: Gravity
                if (p.HasComponent(ComponentFlag.Gravity))
                {
                    p.Velocity.Y += p.UserData0 != 0 ? p.UserData0 : 0.2f;
                }

                // Component: FadeOut (empieza a fade al 50% de vida)
                if (p.HasComponent(ComponentFlag.FadeOut))
                {
                    float progress = p.LifeProgress;
                    if (progress < 0.5f)
                    {
                        float fadeAmount = progress / 0.5f;
                        byte currentAlpha = (byte)(p.PackedColor >> 24);
                        byte newAlpha = (byte)(currentAlpha * fadeAmount);
                        p.PackedColor = (p.PackedColor & 0x00FFFFFF) | ((uint)newAlpha << 24);
                    }
                }

                // Component: ScaleDown
                if (p.HasComponent(ComponentFlag.ScaleDown))
                {
                    float progress = p.LifeProgress;
                    p.Scale *= progress;
                }

                // Component: Rotation (ya aplicado arriba)

                // Decrementar tiempo de vida
                p.TimeLeft--;
                if (p.TimeLeft <= 0)
                {
                    _buffer.Kill(i);
                }
            }
        }

        /// <summary>
        /// Renderiza todas las partículas activas con batching por blend mode.
        /// </summary>
        public override void PostDrawTiles()
        {
            if (Terraria.ID.NetmodeID.Server == Main.netMode || _buffer == null || _textures == null) return;

            var particles = _buffer.RawData;
            var sb = Main.spriteBatch;

            // v5.73: PostDrawTiles se llama cuando el spriteBatch NO está en Begin.
            // NO llamar sb.End() al inicio — solo Begin/End nuestros propios passes.

            try
            {
                // === Pass 1: AlphaBlend ===
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                for (int i = 0; i < _buffer.Capacity; i++)
                {
                    ref ParticleData p = ref particles[i];
                    if (!p.IsActive || p.BlendMode != 0) continue;
                    DrawParticle(sb, ref p);
                }
                sb.End();

                // === Pass 2: Additive ===
                sb.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                for (int i = 0; i < _buffer.Capacity; i++)
                {
                    ref ParticleData p = ref particles[i];
                    if (!p.IsActive || p.BlendMode != 1) continue;
                    DrawParticle(sb, ref p);
                }
                sb.End();
            }
            catch
            {
                // Si algo falla con el estado del spriteBatch, asegurarse de restaurarlo
                try { sb.End(); } catch { }
            }
        }

        private void DrawParticle(SpriteBatch sb, ref ParticleData p)
        {
            if (p.TextureId >= _textureCount) return;
            Texture2D tex = _textures[p.TextureId];
            if (tex == null) return;

            Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
            Vector2 drawPos = p.Position - Main.screenPosition;
            Color color = UnpackColor(p.PackedColor);
            sb.Draw(tex, drawPos, null, color, p.Rotation, origin, p.Scale, SpriteEffects.None, 0f);
        }
    }
}
