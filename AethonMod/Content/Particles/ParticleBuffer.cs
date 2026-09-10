using System;

namespace AethonMod.Content.Particles
{
    /// <summary>
    /// Buffer pre-asignado de partículas con estrategia round-robin.
    /// Evita presión sobre el Garbage Collector al no asignar/liberar objetos.
    ///
    /// Basado en: "Librería de Partículas para Terraria - Referencia para IA"
    /// Sección 11: Buffers Pre-asignados y Gestión de Memoria
    /// </summary>
    public class ParticleBuffer
    {
        private readonly ParticleData[] _particles;
        private readonly int _capacity;
        private int _activeCount;
        private int _nextSlot; // Round-robin para buscar slots libres

        public int Capacity => _capacity;
        public int ActiveCount => _activeCount;
        public ParticleData[] RawData => _particles;

        public ParticleBuffer(int capacity)
        {
            _capacity = capacity;
            _particles = new ParticleData[capacity];
            _activeCount = 0;
            _nextSlot = 0;
        }

        /// <summary>
        /// Intenta spawnear una nueva partícula. Devuelve false si el buffer está lleno.
        /// </summary>
        public bool TrySpawn(ParticleData data)
        {
            // Buscar slot libre empezando en _nextSlot (round-robin)
            for (int i = 0; i < _capacity; i++)
            {
                int idx = (_nextSlot + i) % _capacity;
                if (!_particles[idx].IsActive)
                {
                    data.SetActive(true);
                    _particles[idx] = data;
                    _nextSlot = (idx + 1) % _capacity;
                    _activeCount++;
                    return true;
                }
            }
            return false; // Buffer lleno
        }

        /// <summary>
        /// Desactiva una partícula por índice.
        /// </summary>
        public void Kill(int index)
        {
            if (index >= 0 && index < _capacity && _particles[index].IsActive)
            {
                ref ParticleData p = ref _particles[index];
                p.SetActive(false);
                _activeCount--;
            }
        }

        /// <summary>
        /// Desactiva todas las partículas.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _capacity; i++)
            {
                ref ParticleData p = ref _particles[i];
                p.SetActive(false);
            }
            _activeCount = 0;
            _nextSlot = 0;
        }
    }
}
