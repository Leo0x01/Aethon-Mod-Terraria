using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Particles
{
    /// <summary>
    /// ParticleManager — orquestador central del sistema de partículas.
    /// Mantiene un buffer pre-asignado, actualiza todas las partículas cada frame,
    /// y las renderiza con batching por blend mode y frustum culling.
    ///
    /// Basado en: "Librería de Partículas para Terraria - Referencia para IA"
    /// Secciones 14 (orquestador), 16 (culling), 19.1 (ModSystem) y 12 (components).
    ///
    /// Los componentes se ejecutan inline con chequeos de bitmask en lugar del
    /// patrón IParticleComponent + diccionario: es más rápido (sección 3.4 del libro)
    /// y evita las trampas de structs-copia (sección 11.2).
    ///
    /// Componentes implementados:
    ///   Gravity    — UserData0 = gravedad (default 0.2)
    ///   FadeOut    — alpha decae en la segunda mitad de la vida (base PackedStartColor)
    ///   FadeIn     — alpha sube durante los primeros UserData0 ticks (default 10)
    ///   ScaleDown  — escala decae linealmente hacia 0 (base UserData1/UserData2)
    ///   ScaleUp    — escala crece linealmente desde 0 hasta UserData1/UserData2
    ///   ColorShift — interpola PackedStartColor → PackedEndColor durante la vida
    ///   Homing     — persigue un NPC: UserData0 = whoAmI (≤0 = NPC más cercano),
    ///                UserData3 = fuerza (default 0.1)
    ///   Orbit      — orbita un centro: UserData0/1 = centro XY, UserData2 = velocidad
    ///                angular (rad/tick, default 0.05), UserData3 = radio (0 = deriva
    ///                de la posición de spawn). La rotación avanza con RotationSpeed
    ///                para mantener estelas alineadas tangencialmente.
    ///   EmitLight  — emite luz del color de la partícula; la intensidad deriva de
    ///                la escala (no usa UserData → combinable con cualquier componente)
    /// </summary>
    public class ParticleManager : ModSystem
    {
        private const int DEFAULT_CAPACITY = 4000;

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

            // Registrar texturas built-in (IDs = constantes de ParticleTex)
            RegisterTexture("AethonMod/Content/Effects/Procedural/SoftGlow");     // ID 0
            RegisterTexture("AethonMod/Content/Effects/Procedural/Trail");         // ID 1
            RegisterTexture("AethonMod/Content/Effects/Procedural/Star");          // ID 2
            RegisterTexture("AethonMod/Content/Effects/Procedural/Slash");         // ID 3
            RegisterTexture("AethonMod/Content/Effects/Procedural/Vortex");        // ID 4
            RegisterTexture("AethonMod/Content/Effects/Procedural/Ring");          // ID 5
            RegisterTexture("AethonMod/Content/Effects/Procedural/Crescent");      // ID 6
            RegisterTexture("AethonMod/Content/Effects/Procedural/Noise");         // ID 7
            RegisterTexture("AethonMod/Content/Effects/GlowOrb");                  // ID 8
            RegisterTexture("AethonMod/Content/Effects/SparkleStar");              // ID 9
            RegisterTexture("AethonMod/Content/Effects/TrailGlow");                // ID 10
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
            if (_textures == null || _textureCount >= _textures.Length) return 0;
            try
            {
                _textures[_textureCount] = ModContent.Request<Texture2D>(path).Value;
                return (ushort)(_textureCount++);
            }
            catch
            {
                // v5.78: si la textura no existe, no crashear el mod
                return 0;
            }
        }

        /// <summary>Helper para empaquetar Color a uint.</summary>
        public static uint PackColor(Color c) =>
            (uint)c.R | ((uint)c.G << 8) | ((uint)c.B << 16) | ((uint)c.A << 24);

        /// <summary>Helper para desempaquetar uint a Color.</summary>
        public static Color UnpackColor(uint packed) => new Color(
            (byte)(packed & 0xFF),
            (byte)((packed >> 8) & 0xFF),
            (byte)((packed >> 16) & 0xFF),
            (byte)((packed >> 24) & 0xFF));

        /// <summary>
        /// Spawnea una partícula en el mundo. Rellena automáticamente los valores
        /// base de los componentes si el caller no los configuró:
        ///   - FadeOut/FadeIn/ColorShift → PackedStartColor = PackedColor
        ///   - ScaleDown/ScaleUp → UserData1/UserData2 = escala actual
        /// </summary>
        public static void Spawn(ParticleData data)
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server) return;
            if (_buffer == null) return;

            // Base de color para los fades y el color shift
            if ((data.HasComponent(ComponentFlag.FadeOut) ||
                 data.HasComponent(ComponentFlag.FadeIn) ||
                 data.HasComponent(ComponentFlag.ColorShift)) &&
                data.PackedStartColor == 0)
            {
                data.PackedStartColor = data.PackedColor;
            }

            // ColorShift sin color final: no hay shift
            if (data.HasComponent(ComponentFlag.ColorShift) && data.PackedEndColor == 0)
                data.PackedEndColor = data.PackedColor;

            // Base de escala para las animaciones de tamaño
            if (data.HasComponent(ComponentFlag.ScaleDown))
            {
                if (data.UserData1 == 0) data.UserData1 = data.Scale.X;
                if (data.UserData2 == 0) data.UserData2 = data.Scale.Y;
            }
            if (data.HasComponent(ComponentFlag.ScaleUp))
            {
                // UserData1/2 = escala FINAL; la partícula crece desde 0 hasta ella
                if (data.UserData1 == 0) data.UserData1 = data.Scale.X;
                if (data.UserData2 == 0) data.UserData2 = data.Scale.Y;
            }

            // Orbit sin velocidad angular: default 0.05 rad/tick
            if (data.HasComponent(ComponentFlag.Orbit) && data.UserData2 == 0)
                data.UserData2 = 0.05f;

            _buffer.TrySpawn(data);
        }

        /// <summary>
        /// API genérica de spawn por forma (sección 14 del libro): spawnea `count`
        /// copias del template posicionadas aleatoriamente según la shape, centradas
        /// en `center`.
        /// </summary>
        public static void SpawnShape(Vector2 center, ShapeDescriptor shape, int count, ParticleData template)
        {
            for (int i = 0; i < count; i++)
            {
                ParticleData p = template;
                p.Position = center + shape.GenerateRandomPoint();
                Spawn(p);
            }
        }

        /// <summary>Limpiador para tests / debug: desactiva todas las partículas.</summary>
        public static void Clear() => _buffer?.Clear();

        // ------------------------------------------------------------------
        //  UPDATE — iteración sobre el buffer con componentes inline
        // ------------------------------------------------------------------

        public override void PreUpdateDusts()
        {
            if (Terraria.ID.NetmodeID.Server == Main.netMode || _buffer == null) return;

            var particles = _buffer.RawData;
            for (int i = 0; i < _buffer.Capacity; i++)
            {
                ref ParticleData p = ref particles[i];
                if (!p.IsActive) continue;

                // === COMPONENTES DE COMPORTAMIENTO (modifican velocidad/posición) ===

                // Component: Gravity
                if (p.HasComponent(ComponentFlag.Gravity))
                    p.Velocity.Y += p.UserData0 != 0 ? p.UserData0 : 0.2f;

                // Component: Homing — persigue un NPC (sección 12.2 del libro)
                if (p.HasComponent(ComponentFlag.Homing))
                {
                    NPC target = FindHomingTarget(ref p);
                    if (target != null)
                    {
                        float strength = p.UserData3 != 0 ? p.UserData3 : 0.1f;
                        Vector2 toTarget = target.Center - p.Position;
                        float speed = p.Velocity.Length();
                        if (speed > 0.01f && toTarget.LengthSquared() > 1f)
                        {
                            toTarget.Normalize();
                            p.Velocity = Vector2.Lerp(p.Velocity, toTarget * speed, strength);
                        }
                    }
                }

                // Component: Orbit — orbita un centro (stateless: el ángulo se deriva
                // de la posición actual; la rotación avanza con RotationSpeed para
                // que las estelas alineadas tangencialmente lo sigan estando)
                bool orbited = false;
                if (p.HasComponent(ComponentFlag.Orbit) &&
                    (p.UserData0 != 0f || p.UserData1 != 0f))
                {
                    Vector2 center = new Vector2(p.UserData0, p.UserData1);
                    float angVel = p.UserData2;
                    float radius = p.UserData3;
                    Vector2 offset = p.Position - center;
                    if (radius <= 0f)
                    {
                        radius = offset.Length();
                        p.UserData3 = radius; // fija el radio en el primer tick
                    }
                    float angle = (float)Math.Atan2(offset.Y, offset.X);
                    angle += angVel;
                    p.Position = center + new Vector2(
                        (float)Math.Cos(angle) * radius,
                        (float)Math.Sin(angle) * radius);
                    orbited = true;
                }

                // Movimiento base (si Orbit no fijó la posición directamente)
                if (!orbited)
                    p.Position += p.Velocity;

                // Rotación
                p.Rotation += p.RotationSpeed;

                // === COMPONENTES VISUALES (modifican color/escala) ===

                // Component: ColorShift — interpola StartColor → EndColor (t: 0→1 durante la vida)
                if (p.HasComponent(ComponentFlag.ColorShift))
                {
                    float t = 1f - p.LifeProgress;
                    Color start = UnpackColor(p.PackedStartColor);
                    Color end = UnpackColor(p.PackedEndColor);
                    p.PackedColor = PackColor(Color.Lerp(start, end, t));
                }

                // Component: FadeIn — sube el alpha durante los primeros UserData0 ticks
                if (p.HasComponent(ComponentFlag.FadeIn))
                {
                    float fadeInTicks = p.UserData0 != 0 ? p.UserData0 : 10f;
                    float age = p.Duration - p.TimeLeft;
                    if (age < fadeInTicks)
                    {
                        float amount = age / fadeInTicks; // 0 -> 1
                        byte startAlpha = (byte)(p.PackedStartColor >> 24);
                        byte newAlpha = (byte)(startAlpha * amount);
                        p.PackedColor = (p.PackedColor & 0x00FFFFFF) | ((uint)newAlpha << 24);
                    }
                }

                // Component: FadeOut — decae el alpha en la segunda mitad de la vida
                // v5.74: usa PackedStartColor como alpha base (no el alpha actual
                // que decae exponencialmente cada frame)
                if (p.HasComponent(ComponentFlag.FadeOut))
                {
                    float progress = p.LifeProgress;
                    if (progress < 0.5f)
                    {
                        float fadeAmount = progress / 0.5f; // 1 -> 0
                        byte startAlpha = (byte)(p.PackedStartColor >> 24);
                        byte newAlpha = (byte)(startAlpha * fadeAmount);
                        p.PackedColor = (p.PackedColor & 0x00FFFFFF) | ((uint)newAlpha << 24);
                    }
                }

                // Component: ScaleDown — encoge hacia 0 (base UserData1/UserData2)
                if (p.HasComponent(ComponentFlag.ScaleDown))
                {
                    float progress = p.LifeProgress;
                    float startX = p.UserData1 != 0 ? p.UserData1 : 1f;
                    float startY = p.UserData2 != 0 ? p.UserData2 : 1f;
                    p.Scale.X = startX * progress;
                    p.Scale.Y = startY * progress;
                }

                // Component: ScaleUp — crece desde 0 hasta UserData1/UserData2
                // (ideal para anillos de shockwave expansivos)
                if (p.HasComponent(ComponentFlag.ScaleUp))
                {
                    float t = 1f - p.LifeProgress; // 0 -> 1 durante la vida
                    float endX = p.UserData1 != 0 ? p.UserData1 : 1f;
                    float endY = p.UserData2 != 0 ? p.UserData2 : 1f;
                    p.Scale.X = endX * t;
                    p.Scale.Y = endY * t;
                }

                // Component: EmitLight — la partícula ilumina su entorno.
                // Intensidad derivada de la escala (sin UserData: combinable con
                // FadeIn, Orbit, Homing...). 0.6 por unidad de escala media.
                if (p.HasComponent(ComponentFlag.EmitLight))
                {
                    float intensity = MathHelper.Clamp((p.Scale.X + p.Scale.Y) * 0.3f, 0.05f, 2f);
                    Color c = UnpackColor(p.PackedColor);
                    Lighting.AddLight(p.Position, new Vector3(c.R / 255f, c.G / 255f, c.B / 255f) * intensity);
                }

                // Decrementar tiempo de vida
                p.TimeLeft--;
                if (p.TimeLeft <= 0)
                {
                    _buffer.Kill(i);
                }
            }
        }

        /// <summary>
        /// Busca el objetivo de Homing: NPC explícito por whoAmI (UserData0 &gt; 0)
        /// o el NPC activo más cercano en un radio de 400px.
        /// </summary>
        private static NPC FindHomingTarget(ref ParticleData p)
        {
            int targetIdx = (int)p.UserData0;
            if (targetIdx > 0 && targetIdx < Main.npc.Length && Main.npc[targetIdx].active)
                return Main.npc[targetIdx];

            // Búsqueda del más cercano (UserData0 <= 0)
            NPC best = null;
            float bestDist = 400f * 400f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy()) continue;
                float dist = Vector2.DistanceSquared(npc.Center, p.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = npc;
                }
            }
            return best;
        }

        // ------------------------------------------------------------------
        //  RENDER — batching por blend mode + frustum culling (sección 16)
        // ------------------------------------------------------------------

        public override void PostDrawTiles()
        {
            if (Terraria.ID.NetmodeID.Server == Main.netMode || _buffer == null || _textures == null) return;

            var particles = _buffer.RawData;
            var sb = Main.spriteBatch;

            // v5.73: PostDrawTiles se llama cuando el spriteBatch NO está en Begin.
            // NO llamar sb.End() al inicio — solo Begin/End nuestros propios passes.

            try
            {
                // v5.74: pasar Main.GameViewMatrix.TransformationMatrix para respetar zoom
                var transform = Main.GameViewMatrix.TransformationMatrix;

                // Frustum culling (sección 16.1): margen generoso para partículas
                // grandes escaladas (hasta ~4x una textura de 128px)
                var bounds = new CameraBounds(Main.screenPosition,
                    new Vector2(Main.screenWidth, Main.screenHeight));
                const float cullMargin = 320f;

                // === Pass 1: AlphaBlend ===
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, transform);
                for (int i = 0; i < _buffer.Capacity; i++)
                {
                    ref ParticleData p = ref particles[i];
                    if (!p.IsActive || p.BlendMode != 0) continue;
                    if (!bounds.IsVisible(p.Position, cullMargin)) continue;
                    DrawParticle(sb, ref p);
                }
                sb.End();

                // === Pass 2: Additive ===
                sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, null, null, null, null, transform);
                for (int i = 0; i < _buffer.Capacity; i++)
                {
                    ref ParticleData p = ref particles[i];
                    if (!p.IsActive || p.BlendMode != 1) continue;
                    if (!bounds.IsVisible(p.Position, cullMargin)) continue;
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
