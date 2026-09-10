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
        public const ulong Trail       = 1UL << 9;
        public const ulong DieOnTile   = 1UL << 11;
        public const ulong EmitLight   = 1UL << 12;
    }

    /// <summary>
    /// Capas de render (prioridades numéricas).
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
    }
}
