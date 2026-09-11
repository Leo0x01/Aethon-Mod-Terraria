using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace AethonMod.Content.Particles
{
    /// <summary>
    /// Representa una partícula individual. Es un struct para maximizar
    /// cache locality cuando se itera sobre miles de instancias.
    ///
    /// Basado en: "Librería de Partículas para Terraria - Referencia para IA"
    /// Sección 10.2: Diseño del struct ParticleData
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParticleData
    {
        // Posición en mundo (8 bytes)
        public Vector2 Position;
        // Velocidad por frame (8 bytes)
        public Vector2 Velocity;
        // Escala X/Y separadas para soportar estiramiento (8 bytes)
        public Vector2 Scale;
        // Color packed como uint (RGBA, 4 bytes)
        public uint PackedColor;
        // Color inicial (para lerp durante vida) (4 bytes)
        public uint PackedStartColor;
        // Color final (4 bytes)
        public uint PackedEndColor;
        // Rotación en radianes (4 bytes)
        public float Rotation;
        // Velocidad angular (4 bytes)
        public float RotationSpeed;
        // Tiempo restante de vida en ticks (4 bytes)
        public int TimeLeft;
        // Duración total para calcular progress 0-1 (4 bytes)
        public int Duration;
        // ID de textura (lookup en tabla) (2 bytes)
        public ushort TextureId;
        // Layer priority para orden de render (2 bytes)
        public ushort LayerPriority;
        // Bitmask de componentes activos (8 bytes)
        public ulong ComponentFlags;
        // Blend mode (0=Alpha, 1=Additive, 2=NonPremultiplied) (1 byte)
        public byte BlendMode;
        // Flags misceláneos (1 byte) — bit 0: active
        public byte Flags;
        // Datos custom para componentes (16 bytes, 4 floats)
        public float UserData0;
        public float UserData1;
        public float UserData2;
        public float UserData3;

        // Helper: ¿está activa?
        public bool IsActive => (Flags & 0x01) != 0;
        // Helper: progress de vida (1.0 = recién nacida, 0.0 = muerta)
        public float LifeProgress => Duration > 0 ? TimeLeft / (float)Duration : 0f;
        // Helper: activar/desactivar
        public void SetActive(bool active)
        {
            if (active) Flags |= 0x01;
            else Flags &= 0xFE;
        }
        // Helper: tiene componente activo
        public bool HasComponent(ulong flag) => (ComponentFlags & flag) != 0;
        // Helper: activar componente
        public void EnableComponent(ulong flag) => ComponentFlags |= flag;
        // Helper: desactivar componente
        public void DisableComponent(ulong flag) => ComponentFlags &= ~flag;
    }

    /// <summary>
    /// Bitmask de componentes. Cada componente tiene un bit asignado.
    /// (Sección 10.3 del libro de referencia)
    /// </summary>
    public static class ComponentFlag
    {
        public const ulong Gravity     = 1UL << 0;
        public const ulong FadeOut     = 1UL << 1;
        public const ulong FadeIn      = 1UL << 2;
        public const ulong ScaleDown   = 1UL << 3;
        public const ulong ScaleUp     = 1UL << 4;
        public const ulong Homing      = 1UL << 5;
        public const ulong Rotation    = 1UL << 6;
        public const ulong ColorShift  = 1UL << 7;
        public const ulong Orbit       = 1UL << 8;
        public const ulong Trail       = 1UL << 9;   // reservado (sección 49.2 roadmap)
        public const ulong BounceOnTile = 1UL << 10;
        public const ulong DieOnTile   = 1UL << 11;
        public const ulong EmitLight   = 1UL << 12;
        /// <summary>v5.90 — atracción hacia un PUNTO fijo (materia absorbida
        /// por el agujero negro): UserData0/1 = centro XY, UserData3 = fuerza
        /// (aceleración por tick). La partícula MUERE al llegar al centro.
        /// Con una velocidad inicial tangencial dibuja una espiral de infalling
        /// perfecta. Incompatible con Gravity/FadeIn/ScaleDown/ScaleUp/Orbit
        /// (usan los mismos slots de UserData).</summary>
        public const ulong PullTo      = 1UL << 13;
    }

    /// <summary>
    /// Capas de render (prioridades numéricas, sección 15.1 del libro).
    /// El render actual ocurre en un único pase en PostDrawTiles (detrás de
    /// proyectiles/NPCs/jugadores); el campo LayerPriority queda reservado para
    /// un pipeline multi-pase futuro.
    ///
    /// v5.86 — AboveLens: partículas de efectos propios del agujero negro que
    /// se dibujan ENCIMA de la lente gravitacional (las pinta el
    /// BlackHoleLensSystem tras compositar la distorsión, de modo que la lente
    /// quede DETRÁS de la animación del agujero y sus efectos).
    /// </summary>
    public static class LayerPriorities
    {
        public const ushort BelowTiles = 0;
        public const ushort AboveTiles = 100;
        public const ushort BeforeProjectiles = 200;
        public const ushort AfterProjectiles = 300;
        public const ushort BeforeNPCs = 400;
        public const ushort AfterNPCs = 500;
        public const ushort BeforePlayers = 600;
        public const ushort AfterPlayers = 700;
        public const ushort AboveAll = 900;
        /// <summary>Encima de la lente gravitacional (la pinta BlackHoleLensSystem).</summary>
        public const ushort AboveLens = 950;
    }

    /// <summary>
    /// IDs de las texturas built-in registradas por ParticleManager.OnModLoad
    /// (patrón TextureRegistry de la sección 21.2 del libro).
    /// </summary>
    public static class ParticleTex
    {
        /// <summary>Glow radial suave 64x64, ideal para blending aditivo.</summary>
        public const ushort SoftGlow = 0;
        /// <summary>Degradado lineal 64x8 para estelas y rayos.</summary>
        public const ushort Trail = 1;
        /// <summary>Estrella 32x32 para efectos mágicos.</summary>
        public const ushort Star = 2;
        /// <summary>Crescent 96x96 para slash effects.</summary>
        public const ushort Slash = 3;
        /// <summary>Espiral 64x64 para portales/vórtices.</summary>
        public const ushort Vortex = 4;
        /// <summary>Anillo 64x64 para shockwaves.</summary>
        public const ushort Ring = 5;
        /// <summary>Media luna 64x64.</summary>
        public const ushort Crescent = 6;
        /// <summary>Ruido procedural 128x128.</summary>
        public const ushort Noise = 7;
        /// <summary>Orbe 128x128 con sombreado 3D simulado.</summary>
        public const ushort GlowOrb = 8;
        /// <summary>Destello estrella 32x32.</summary>
        public const ushort SparkleStar = 9;
        /// <summary>Estela degradada 32x8.</summary>
        public const ushort TrailGlow = 10;
    }
}
