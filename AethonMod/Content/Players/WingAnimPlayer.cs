using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// WingAnimPlayer — v6.06 — LA ANIMACIÓN PROCEDURAL DE LAS ALAS DE LUZ.
    ///
    /// Las dos alas "técnica coronas" (Horizonte de Sucesos y Anillo de
    /// Fotones) NO tienen spritesheet: su textura de equipo es un PNG en
    /// blanco y TODO su dibujado lo hace la biblioteca VFX desde sus capas
    /// de dibujado. Este ModPlayer es el CORAZÓN de su animación: un estado
    /// por ala (apertura + fase de aleteo) que reacciona con MUELLES suaves
    /// a cada acción del jugador:
    ///
    ///   - VOLANDO (salto presionado + tiempo de vuelo) → apertura 1.0 y
    ///     ciclo de aleteo continuo (fase avanza; el aleteo es una onda
    ///     continua, no frames — suavidad infinita).
    ///   - PLANEANDO (cayendo + salto presionado, sin tiempo) → apertura
    ///     0.85, aleteo lento y suave.
    ///   - CAYENDO → apertura 0.95, alas extendidas sin aletear.
    ///   - REPOSO → apertura 0.2: las alas se PLEGAN contra la espalda.
    ///
    /// El muelle (velocidad + rigidez + amortiguación) da el "overshoot"
    /// orgánico al abrir/cerrar. Los dusts y el sonido de aleteo se emiten
    /// SOLO cuando el ala es FUNCIONAL (no en vanidad pura).
    /// </summary>
    public class WingAnimPlayer : ModPlayer
    {
        // === ALAS DEL HORIZONTE DE SUCESOS ===
        /// <summary>Apertura 0..1 (0 plegada, 1 extendida).</summary>
        public float EhOpen;

        /// <summary>Velocidad del muelle de apertura.</summary>
        public float EhOpenVel;

        /// <summary>Fase del ciclo de aleteo (rad, avanza al volar).</summary>
        public float EhFlapPhase;

        /// <summary>Amplitud del aleteo (0 reposo .. 1 volando).</summary>
        public float EhFlapAmp;

        // === ALAS DEL ANILLO DE FOTONES ===
        public float PrOpen;
        public float PrOpenVel;
        public float PrFlapPhase;
        public float PrFlapAmp;

        // Slots de equipo cacheados (perezoso).
        private int _ehSlot = -1;
        private int _prSlot = -1;

        public override void PostUpdate()
        {
            if (Main.netMode == NetmodeID.Server) return;

            if (_ehSlot < 0)
                _ehSlot = EquipLoader.GetEquipSlot(Mod, "EventHorizonWings", EquipType.Wings);
            if (_prSlot < 0)
                _prSlot = EquipLoader.GetEquipSlot(Mod, "PhotonRingWings", EquipType.Wings);

            // Visible = el slot de alas VISUALES del jugador (funcional o vanidad).
            bool ehVisible = Player.wings == _ehSlot;
            bool prVisible = Player.wings == _prSlot;
            // Funcional = además da vuelo (wingsLogic apunta al mismo slot).
            bool ehFunc = ehVisible && Player.wingsLogic == _ehSlot;
            bool prFunc = prVisible && Player.wingsLogic == _prSlot;

            if (ehVisible)
                UpdateWing(ref EhOpen, ref EhOpenVel, ref EhFlapPhase, ref EhFlapAmp,
                    ehFunc, dustId: DustID.PinkTorch, dustColor: new Color(255, 60, 190));
            if (prVisible)
                UpdateWing(ref PrOpen, ref PrOpenVel, ref PrFlapPhase, ref PrFlapAmp,
                    prFunc, dustId: DustID.PinkCrystalShard, dustColor: new Color(255, 170, 215));
        }

        /// <summary>
        /// La máquina de estados de un ala de luz: muelle de apertura +
        /// fase de aleteo + sonido en cada ciclo + ascuas al volar.
        /// </summary>
        private void UpdateWing(ref float open, ref float vel, ref float phase, ref float amp,
            bool functional, int dustId, Color dustColor)
        {
            // --- estado objetivo según la acción del jugador ---
            bool flying = functional && Player.controlJump && Player.wingTime > 0f
                          && Player.jump == 0 && Player.velocity.Y != 0f;
            bool gliding = functional && Player.controlJump && Player.wingTime <= 0f
                           && Player.velocity.Y > 0f;
            bool falling = Player.velocity.Y != 0f;

            float target = flying ? 1f : gliding ? 0.85f : falling ? 0.95f : 0.2f;

            // --- muelle: rigidez + amortiguación (overshoot orgánico) ---
            vel += (target - open) * 0.02f;
            vel *= 0.88f;
            open += vel;
            if (open < -0.05f) open = -0.05f;
            if (open > 1.15f) open = 1.15f;

            // --- aleteo: amplitud easing + avance de fase ---
            float targetAmp = flying ? 1f : gliding ? 0.3f : 0f;
            amp += (targetAmp - amp) * 0.06f;

            if (flying || gliding)
            {
                float speed = flying ? 0.24f : 0.045f;
                phase += speed;
                if (phase >= MathHelper.TwoPi)
                {
                    phase -= MathHelper.TwoPi;
                    // Un flap completo → sonido de aleteo (solo funcional).
                    if (flying && functional)
                        SoundEngine.PlaySound(SoundID.Item32, Player.position);
                }
            }

            // --- ascuas al volar (solo funcional, cadencia moderada) ---
            if (functional && flying && Main.rand.NextBool(2))
            {
                float side = Main.rand.NextBool() ? -1f : 1f;
                Dust d = Dust.NewDustDirect(
                    Player.position + new Vector2(
                        Player.width * 0.5f + side * Main.rand.NextFloat(6f, 34f),
                        Player.height * 0.5f - 12f),
                    8, 8, dustId, 0f, 0f, 120, dustColor, Main.rand.NextFloat(0.8f, 1.4f));
                d.noGravity = true;
                d.noLight = false;
                d.velocity *= 0.3f;
                d.velocity.Y -= 0.4f;
                d.fadeIn = 1.15f;
            }
        }
    }
}
