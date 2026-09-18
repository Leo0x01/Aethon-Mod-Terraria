using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Buffs;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Players
{
    /// <summary>
    /// OcasoPlayer — v6.27 — EL GAUGE DEL OCASO DE AETHON.
    ///
    /// El patrón del Cosmic Destroyer (investigación v6.26, informe
    /// el informe de las armas supremas de referencia, lección 10): LA TRINIDAD
    /// carga → burst → lockout aplicada a un arma suprema:
    ///
    ///   · CARGA: cada impacto del proyectil NORMAL suma +3 al gauge
    ///     (tope 100 → 34 impactos ≈ 12 s de fuego — el ritmo del TSA).
    ///   · BURST: con el gauge lleno, CLIC DERECHO activa EL OCASO:
    ///     8 segundos (480 ticks) donde el arma dispara MUERTES DE
    ///     ESTRELLA a daño ×3 con ejecución bajo el 50% de vida.
    ///   · LOCKOUT: al agotarse el ocaso, SOBRECALENTADA — 120 ticks
    ///     (2 s) donde el arma NO dispara y el gauge está a cero.
    ///
    /// La UI (el aro medidor de 20 segmentos sobre la cabeza) la dibuja
    /// OcasoSystem en PostDrawInterface leyendo ESTE estado. El fade-out
    /// de visibilidad (−0.05/tick tras 20 ticks sin disparar) es la
    /// lección de UI ligera del Cosmic Destroyer.
    ///
    /// Nota MP: el mod es de pruebas en un jugador (TestingPlayer lo
    /// gatea); el gauge vive localmente en cada cliente.
    /// </summary>
    public class OcasoPlayer : ModPlayer
    {
        // === LOS NÚMEROS DEL PATRÓN (los del informe) ===
        public const float GaugeMax = 100f;
        public const int OcasoTicks = 480;      // 8 s de burst
        public const int OverheatTicks = 120;   // 2 s de lockout
        public const float GaugePorImpacto = 3f;

        // === EL ESTADO ===
        public float Gauge;
        public int OcasoTime;       // ticks restantes del modo ocaso
        public int OverheatTime;    // ticks restantes del lockout
        public float UiFade = 1f;   // visibilidad del aro medidor

        public bool OcasoActivo => OcasoTime > 0;
        public bool Sobrecalentado => OverheatTime > 0;

        /// <summary>El progreso 0..1 del modo ocaso (para el drenaje visual).</summary>
        public float OcasoT => OcasoTime > 0 ? 1f - OcasoTime / (float)OcasoTicks : 0f;

        // ==================================================================
        //  EL CICLO — la trinidad vive por TICK
        // ==================================================================

        public override void PreUpdate()
        {
            // === LOS BUFFS INDICADORES (se refrescan; el estado real es ESTE) ===
            if (OcasoActivo)
                Player.AddBuff(ModContent.BuffType<OcasoActivoBuff>(), OcasoTime + 2);
            else if (Sobrecalentado)
                Player.AddBuff(ModContent.BuffType<SobrecalentadoBuff>(), OverheatTime + 2);

            if (OcasoTime > 0)
            {
                OcasoTime--;

                // === EL FIN DEL OCASO: la trinidad pasa al lockout ===
                if (OcasoTime == 0)
                {
                    OverheatTime = OverheatTicks;
                    Gauge = 0f;
                    UiFade = 1f;

                    // EL CORTE nunca es en seco (la lección de las armas supremas de referencia): un
                    // lamento de vapor + destello tenue + sacudida pequeña.
                    if (Main.netMode != NetmodeID.Server)
                    {
                        ElCorteDelOcaso();
                    }
                }
            }
            else if (OverheatTime > 0)
            {
                OverheatTime--;
            }

            // === EL FADE DE LA UI (la lección del TSA: −0.05/tick) ===
            if (UiFade > 0f) UiFade -= 0.05f;
        }

        /// <summary>El lamento del corte del ocaso (FX de cliente).</summary>
        private void ElCorteDelOcaso()
        {
            OndaLib.Flash(new Color(255, 90, 40), 0.12f, 10);
            OndaLib.Kick(5f, 14);
            for (int i = 0; i < 10; i++)
            {
                float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Dust d = Dust.NewDustPerfect(Player.Center, DustID.Smoke,
                    new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                        Main.rand.NextFloat(0.6f, 1.8f),
                    90, new Color(120, 70, 60), 1.1f);
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        // ==================================================================
        //  LA API DEL ARMA
        // ==================================================================

        /// <summary>Suma carga al gauge (solo fuera del ocaso/lockout).
        /// Devuelve true si ESTE impacto fue el que LLENÓ el gauge.</summary>
        public bool AñadirGauge(float cantidad)
        {
            if (OcasoActivo || Sobrecalentado) return false;
            float antes = Gauge;
            Gauge = MathHelper.Clamp(Gauge + cantidad, 0f, GaugeMax);
            UiFade = 1f;

            if (antes < GaugeMax && Gauge >= GaugeMax)
            {
                // EL AVISO DE LISTO: un pulso de luz dorada + el sonido sutil.
                if (Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(
                        Terraria.ID.SoundID.Item4.WithPitchOffset(0.45f),
                        Player.Center);
                    for (int i = 0; i < 12; i++)
                    {
                        float ang = i / 12f * MathHelper.TwoPi;
                        Dust d = Dust.NewDustPerfect(Player.Center, DustID.GoldFlame,
                            new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 2.2f,
                            200, default, 0.6f);
                        d.noGravity = true;
                        d.fadeIn = 0f;
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>Intenta activar EL OCASO (clic derecho con el gauge lleno).</summary>
        public bool IntentarActivar()
        {
            if (Gauge < GaugeMax || OcasoActivo || Sobrecalentado) return false;

            OcasoTime = OcasoTicks;
            Gauge = 0f;
            UiFade = 1f;

            // === LA IGNICIÓN DEL OCASO (el telegraph de 20 ticks de la
            //     la lección de las armas supremas de referencia vive en el propio FX: Kick + Flash
            //     + la corona de chispas) ===
            if (Main.netMode != NetmodeID.Server)
            {
                OndaLib.Kick(11f, 22);
                OndaLib.Flash(new Color(255, 130, 45), 0.30f, 16);
                for (int i = 0; i < 36; i++)
                {
                    float ang = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                    Dust d = Dust.NewDustPerfect(Player.Center,
                        i % 2 == 0 ? DustID.GoldFlame : DustID.Torch,
                        new Vector2(MathF.Cos(ang), MathF.Sin(ang)) *
                            Main.rand.NextFloat(2.5f, 6.5f),
                        220, default, 0.9f);
                    d.noGravity = true;
                    d.fadeIn = 0f;
                }
                Lighting.AddLight(Player.Center, 1.4f, 0.9f, 1.6f);
            }

            Terraria.Audio.SoundEngine.PlaySound(
                Terraria.ID.SoundID.Item88.WithPitchOffset(-0.4f), Player.Center);
            return true;
        }

        /// <summary>El multiplicador de daño del modo ocaso (×3 — el del informe).</summary>
        public const float MultiplicadorOcaso = 3f;
    }
}
