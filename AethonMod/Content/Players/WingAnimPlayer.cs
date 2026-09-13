using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// WingAnimPlayer — v6.08 — LA ANIMACIÓN PROCEDURAL DE LAS 8 ALAS DE LUZ.
    ///
    /// TODO el sistema de alas es ahora de luz (técnica de las coronas) y su
    /// textura de equipo es un PNG en blanco: TODO el dibujado lo hace la
    /// biblioteca VFX. Este ModPlayer es el CORAZÓN de la animación y ha
    /// sido reescrito con las MEJORAS pedidas:
    ///
    ///   1. PERSONALIDAD POR ESTILO (WingMotionProfile): cada ala tiene sus
    ///      frecuencias, aperturas objetivo y muelle propios — la mariposa
    ///      aletea LENTO y PROFUNDO, el hada VIBRA rápido y nunca para, el
    ///      eclipse es majestuoso...
    ///   2. GOLPE ASIMÉTRICO (StrokeAsymmetry): la mariposa baja el ala
    ///      RÁPIDO (el golpe que da empuje) y la sube LENTO — como el vuelo
    ///      real de una mariposa, no una onda sine simétrica.
    ///   3. ALWAYSFLUTTER: las alas de hada y nebulosa NUNCA dejan de
    ///      latir del todo (vibración sutil en reposo).
    ///   4. MUELLES por estilo: rigidez/amortiguación distintas (el cometa
    ///      responde nervioso, la nebulosa deriva perezosa).
    ///   5. Cadencia de sonido/dust por estilo (el hada suena cada 2 ciclos
    ///      porque su aleteo es casi 10×/segundo).
    ///
    /// Estados del jugador (detección intacta de v6.06): VOLANDO → apertura
    /// FlyOpen + ciclo continuo · PLANEANDO → GlideOpen + aleteo lento ·
    /// CAYENDO → FallOpen extendida · REPOSO → FoldOpen plegada.
    /// </summary>
    public class WingAnimPlayer : ModPlayer
    {
        /// <summary>El estado de animación de UN ala.</summary>
        public struct WingAnimState
        {
            /// <summary>Apertura 0..1+ (0 plegada, 1 extendida).</summary>
            public float Open;

            /// <summary>Velocidad del muelle de apertura.</summary>
            public float OpenVel;

            /// <summary>Fase del ciclo de aleteo (rad).</summary>
            public float FlapPhase;

            /// <summary>Amplitud del aleteo (0 reposo .. 1 volando).</summary>
            public float FlapAmp;

            /// <summary>Ciclos completados (cadencia de sonido del hada).</summary>
            public int CycleCount;
        }

        /// <summary>Un estado por estilo de ala (índice = WingStyles).</summary>
        private WingAnimState[] _states = new WingAnimState[WingStyles.Count];

        /// <summary>Acceso del renderizador al estado de un estilo.</summary>
        public ref WingAnimState State(int style) => ref _states[style];

        /// <summary>Slots de equipo cacheados por estilo (-1 sin resolver).</summary>
        private int[] _slots = new int[WingStyles.Count];

        private bool _slotsReady;

        private void ResolveSlots()
        {
            if (_slotsReady) return;
            for (int i = 0; i < WingStyles.Count; i++)
                _slots[i] = EquipLoader.GetEquipSlot(Mod, VFXWingSlots.ItemNames[i], EquipType.Wings);
            _slotsReady = true;
        }

        public override void Initialize()
        {
            // Reset al entrar a un mundo nuevo (tML reutiliza instancias).
            _states = new WingAnimState[WingStyles.Count];
            _slotsReady = false;
        }

        public override void PostUpdate()
        {
            if (Main.netMode == NetmodeID.Server) return;
            ResolveSlots();

            for (int style = 0; style < WingStyles.Count; style++)
            {
                if (Player.wings != _slots[style]) continue;

                // Visible = slot VISUAL equipado (funcional o vanidad).
                // Funcional = además da vuelo (wingsLogic apunta al mismo slot).
                bool functional = Player.wingsLogic == _slots[style];
                UpdateWing(style, functional);
            }
        }

        /// <summary>
        /// La máquina de estados de un ala de luz — ahora con la
        /// PERSONALIDAD de su estilo (WingMotionProfile).
        /// </summary>
        private void UpdateWing(int style, bool functional)
        {
            WingMotionProfile profile = WingStyles.Motion(style);
            ref WingAnimState st = ref _states[style];

            // --- estado objetivo según la acción del jugador ---
            bool flying = functional && Player.controlJump && Player.wingTime > 0f
                          && Player.jump == 0 && Player.velocity.Y != 0f;
            bool gliding = functional && Player.controlJump && Player.wingTime <= 0f
                           && Player.velocity.Y > 0f;
            bool falling = Player.velocity.Y != 0f;

            float target = flying ? profile.FlyOpen
                          : gliding ? profile.GlideOpen
                          : falling ? profile.FallOpen
                          : profile.FoldOpen;

            // --- MUELLE por estilo (overshoot orgánico calibrado) ---
            st.OpenVel += (target - st.Open) * profile.SpringStiffness;
            st.OpenVel *= profile.SpringDamping;
            st.Open += st.OpenVel;
            if (st.Open < -0.05f) st.Open = -0.05f;
            if (st.Open > 1.15f) st.Open = 1.15f;

            // --- amplitud del aleteo: easing hacia el objetivo ---
            // ALWAYSFLUTTER: nunca baja del todo (vibración sutil en reposo).
            float flutterFloor = profile.AlwaysFlutter ? 0.22f : 0f;
            float targetAmp = flying ? 1f : gliding ? 0.30f : flutterFloor;
            st.FlapAmp += (targetAmp - st.FlapAmp) * 0.06f;

            // --- avance de fase: GOLPE ASIMÉTRICO por estilo ---
            // La mariposa baja el ala RÁPIDO (empuje) y sube LENTO: el avance
            // de fase no es constante, escala con la posición del golpe.
            if (flying || gliding || profile.AlwaysFlutter)
            {
                float baseSpeed = flying ? profile.FlapSpeedFlying
                                : gliding ? profile.FlapSpeedGliding
                                : 0.085f;   // vibración de reposo (hada/nebulosa)

                float advance;
                if (profile.StrokeAsymmetry > 1f)
                {
                    // Mitad "abajo" (sin>0): rápida ×(1+a·0.35); mitad "arriba": lenta.
                    float s = (float)System.Math.Sin(st.FlapPhase);
                    float k = s > 0f ? (1f + (profile.StrokeAsymmetry - 1f) * 0.55f)
                                     : (1f - (profile.StrokeAsymmetry - 1f) * 0.30f);
                    advance = baseSpeed * k;
                }
                else
                {
                    advance = baseSpeed;
                }

                st.FlapPhase += advance;
                if (st.FlapPhase >= MathHelper.TwoPi)
                {
                    st.FlapPhase -= MathHelper.TwoPi;
                    st.CycleCount++;

                    // Sonido de aleteo: cada ciclo (o cada 2 para el hada, que
                    // aletea casi 10 veces por segundo — sonaría ametralladora).
                    bool soundNow = profile.SoundEveryCycle || (st.CycleCount & 1) == 0;
                    if (flying && functional && soundNow)
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
                    8, 8, profile.DustId, 0f, 0f, 120, profile.DustColor,
                    Main.rand.NextFloat(0.8f, 1.4f));
                d.noGravity = true;
                d.noLight = false;
                d.velocity *= 0.3f;
                d.velocity.Y -= 0.4f;
                d.fadeIn = 1.15f;
            }
        }
    }
}
