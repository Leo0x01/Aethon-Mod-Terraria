using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Dusts;
using AethonMod.Content.Items.Accessories;
using AethonMod.Content.Projectiles;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Globals
{
    // ======================================================================
    //  AuraJuicioGlobalNPC — v6.104 — EL CORAZÓN DEL AURA DE JUICIO (el
    //  puerto completo de la quemadura del Warbanner of the Righteous).
    //
    //  LA LETRA DEL USUARIO: «investiga bien como funciona y como
    //  reacciona visualmente los enemigos, copia exactamente el codigo
    //  de Warbanner of the Righteous […] efectos vfx tecnica todo».
    //
    //  CÓMO REACCIONAN LOS ENEMIGOS (la letra del original, aprendida):
    //   · EL IMPÍO ENTRÓ AL CAMPO → la MARCA (180 ticks, refrescada
    //     dentro; fuera, sus últimos 3 segundos de fuego).
    //   · EL FUEGO ACELERA — cada tick dentro suma un STACK (0→300):
    //     el golpe pasa de cada 19 ticks a cada 2 (Remap(stacks, 0→300,
    //     15→2)) — “visualmente se nota cómo cada vez se quema más”.
    //   · EL BURST DE LOS 300 — al llegar al tope, 15 chispas doradas
    //     estallan del cuerpo (y el contador pasa de 300: no repite).
    //   · CUANTO MÁS CERCA, MÁS CALIENTE — la INTENSIDAD ×1 (borde) →
    //     ×3 (pegado): multiplica el daño del golpe, la luz dorada del
    //     cuerpo, la densidad de chispas y la violencia del repel.
    //   · EL CAMPO REPELE — intensidad > 2.5: los muy cercanos frenan
    //     ×0.75; y los RÁPIDOS (>5 px/t) con intensidad > 2.85 ven su
    //     velocidad INVERTIDA ×0.7 (solo no-jefes: el jefe no se
    //     intimida por un estandarte).
    //   · LAS CHISPAS SALEN DESPEDIDAS — nacen en el flanco que MIRA
    //     al portador y son EXPULSADAS alejándose (burnDirection), en
    //     abanico (RotatedByRandom) que converge al rumbo recto.
    //
    //  EL MODELO MP (como el original): la marca la SIEMBRA el
    //  UpdateAccessory de cada jugador EN SU MÁQUINA (los campos son
    //  locales, sin netUpdate); el GOLPE lo cobra SOLO el dueño
    //  (Dueño−1 == Main.myPlayer) a través del AuraJuicioDano — el
    //  cauce del motor difunde el daño y el debuff.
    // ======================================================================
    public class AuraJuicioGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        // ================================================================
        //  EL ESTADO POR IMPÍO (los campos del original, en español).
        // ================================================================

        /// <summary>LA MARCA: el impío arde por el estandarte.</summary>
        public bool Marcada;

        /// <summary>EL TIMER de la marca (180 ticks; refrescado dentro
        /// del campo; al morir, la marca se apaga).</summary>
        public int TimerQuemadura;

        /// <summary>LOS STACKS del fuego (0..300 — cada tick dentro
        /// suma; a 300 estalla; decaen ×0.9/frame en los últimos 60).</summary>
        public int StacksQuemadura;

        /// <summary>EL DAÑO BASE del golpe (15 × mejor clase ×
        /// multitarjet — la intensidad se aplica al cobrar).</summary>
        public int DanoQuemadura;

        /// <summary>LA DIRECCIÓN portador → impío (unitaria — por ahí
        /// salen expulsadas las chispas: hacia AFUERA del jugador).</summary>
        public Vector2 DireccionQuemadura = Vector2.UnitX;

        /// <summary>LA INTENSIDAD por cercanía (×1 borde → ×3 pegado).</summary>
        public float IntensidadQuemadura;

        /// <summary>EL SILENCIO VISUAL pedido por el portador
        /// (hideVisual del slot: el daño corre, el teatro calla).</summary>
        public bool OcultarEfectos;

        /// <summary>EL RITMO DEL GOLPE (cuenta atrás al siguiente golpe
        /// de fuego: 2..19 ticks según stacks e intensidad).</summary>
        public int TiempoGolpe;

        /// <summary>EL DUEÑO de la marca (player.whoAmI + 1; 0 = nadie).</summary>
        public int Dueño;

        // ================================================================
        //  LOS NÚMEROS DEL ORIGINAL (la tabla exacta).
        // ================================================================
        private const int StacksMaximos = 300;
        private const int RitmoMasRapido = 2;    // fastestBurnRate
        private const int RitmoMasLento = 15;    // slowestBurnRate

        /// <summary>¿El impío arde por el Aura de Juicio?</summary>
        public static bool TieneMarca(NPC npc) =>
            npc != null && npc.active &&
            npc.GetGlobalNPC<AuraJuicioGlobalNPC>().Marcada;

        // ================================================================
        //  EL MOTOR DE LA QUEMADURA — cada tick del impío marcado (en
        //  PostAI, el patrón de la casa: la IA ya escribió SU velocidad
        //  y el repel la pisa DESPUÉS).
        // ================================================================
        public override void PostAI(NPC npc)
        {
            if (Main.gameMenu || !npc.active || !Marcada)
                return;

            // EL PODER DEL FUEGO — los stacks aceleran el ritmo:
            // 0 stacks = cada 15 ticks; 300 = cada 2 (Remap del original).
            float poderQuemadura = Remap(StacksQuemadura, 0, StacksMaximos,
                RitmoMasLento, RitmoMasRapido);

            // EL BONUS DE TAMAÑO — los gigantes escupen chispas más
            // lejos (GetLerpValue(0, 170, mitad-mayor-del-hitbox)).
            float bonusTamano = 1f + Utils.GetLerpValue(0f, 170f,
                Math.Max(npc.Hitbox.Width / 2f, npc.Hitbox.Height / 2f));

            bool enPantalla = Main.netMode != NetmodeID.Server && !Main.dedServ && !Main.gameMenu;

            // ------------------------------------------------------------
            //  LA LUZ DORADA — el cuerpo del impío ARDE más cuanto más
            //  cerca del portador (Gold × 0.3 × intensidad).
            // ------------------------------------------------------------
            if (!OcultarEfectos && enPantalla)
                Lighting.AddLight(npc.Center, Color.Gold.ToVector3() * 0.3f * IntensidadQuemadura);

            // ------------------------------------------------------------
            //  EL BURST DE LOS 300 — el fuego MADURO estalla: 15 chispas
            //  doradas/naranjas salen despedidas del cuerpo (una sola
            //  vez: el contador pasa de 300 para no repetir).
            // ------------------------------------------------------------
            if (StacksQuemadura == StacksMaximos && !OcultarEfectos && enPantalla)
            {
                for (int i = 0; i < 15; i++)
                {
                    Color color = Main.rand.NextBool(2)
                        ? Color.Goldenrod
                        : Color.Lerp(Color.OrangeRed, Color.Orange, Main.rand.NextFloat());
                    Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) *
                                  Main.rand.NextFloat(2f, 6f);
                    Dust d = Dust.NewDustPerfect(npc.Center,
                        ModContent.DustType<PolvoLuzLuciernaga>(), vel, 0,
                        color, Main.rand.NextFloat(0.8f, 1.2f));
                    d.noGravity = true;
                    d.rotation = vel.ToRotation();   // el rumbo de convergencia
                }
                try
                {
                    SoundEngine.PlaySound(SoundID.Item116, npc.Center);
                }
                catch { }
                StacksQuemadura++;
            }

            // ------------------------------------------------------------
            //  EL REPEL DEL CAMPO — los muy cercanos sienten el peso del
            //  juicio: frenan ×0.75 y los RÁPIDOS rebotan (velocidad
            //  INVERTIDA ×0.7). SOLO no-jefes con empujabilidad (el
            //  original: el jefe no se deja intimidar).
            // ------------------------------------------------------------
            if (IntensidadQuemadura > 2.5f && !npc.boss && npc.knockBackResist > 0f &&
                Main.netMode != NetmodeID.MultiplayerClient)
            {
                npc.velocity *= 1f - 0.25f * Utils.GetLerpValue(2.5f, 3f, IntensidadQuemadura);
                if (npc.velocity.Length() > 5f && IntensidadQuemadura > 2.85f)
                    npc.velocity = -npc.velocity * 0.7f;
            }

            // ------------------------------------------------------------
            //  EL GOLPE DE QUEMADURA — el corazón del ritmo.
            // ------------------------------------------------------------
            if (TiempoGolpe <= 0)
            {
                // EL TEATRO — las chispas expulsadas del flanco que MIRA
                // al portador (más chispas cuanto más caliente: el nivel
                // ∝ (lento − poder) × intensidad, 1..6).
                if (!OcultarEfectos && enPantalla)
                {
                    int nivelParticulas = (int)(MathHelper.Clamp(
                        (RitmoMasLento - poderQuemadura) * 0.15f, 1f, 2f) * IntensidadQuemadura);
                    for (int s = 0; s < nivelParticulas; s++)
                    {
                        Color color = Main.rand.NextBool(2)
                            ? Color.Goldenrod
                            : Color.Lerp(Color.OrangeRed, Color.Orange, Main.rand.NextFloat());

                        // NACEN EN EL FLANCO QUE MIRA AL JUGADOR — los
                        // gigantes, a 220 px hacia él; los chicos, al centro.
                        Vector2 posChispa = npc.Center - DireccionQuemadura * 220f *
                            Utils.GetLerpValue(0f, 200f,
                                Math.Max(nPC_MitadMayor(npc), 0f));

                        // EXPULSADAS ALEJÁNDOSE DEL JUGADOR — el abanico
                        // (RotatedByRandom(0.6·intensidad)) que converge al
                        // rumbo recto (el PolvoLuzLuciernaga lo lleva en
                        // dust.rotation — la técnica de la casa).
                        float ajusteVel = Main.rand.NextFloat(2f, 7f) *
                            IntensidadQuemadura * bonusTamano;
                        Vector2 velFin = DireccionQuemadura * ajusteVel;
                        Vector2 velInicio = velFin.RotatedByRandom(0.6f * IntensidadQuemadura);

                        Dust d = Dust.NewDustPerfect(posChispa,
                            ModContent.DustType<PolvoLuzLuciernaga>(), velInicio, 0,
                            color, Main.rand.NextFloat(0.7f, 1.1f));
                        d.noGravity = true;
                        d.rotation = velFin.ToRotation();

                        // EL 50% EXTRA — el LightDust del original: una
                        // llama Torch dorada que acompaña a la chispa.
                        if (Main.rand.NextBool(2))
                        {
                            Dust extra = Dust.NewDustPerfect(posChispa, DustID.Torch,
                                velInicio * 0.7f, 0, color, Main.rand.NextFloat(0.6f, 1f));
                            extra.noGravity = true;
                        }
                    }
                }

                // EL COBRO — SOLO la máquina del dueño lanza el fantasma
                // (AuraJuicioDano: teletransporte + golpe del motor con
                // 50 AP y el mejor crit + las Llamas del Juicio 30 t).
                if (Dueño - 1 == Main.myPlayer && !Main.gameMenu)
                {
                    int danoGolpe = (int)(DanoQuemadura * IntensidadQuemadura);
                    if (danoGolpe > 0 && VFXCore.EsObjetivo(npc))
                    {
                        Projectile golpe = Projectile.NewProjectileDirect(
                            npc.GetSource_FromAI(), npc.Center, Vector2.Zero,
                            ModContent.ProjectileType<AuraJuicioDano>(), 0, 0f, Dueño - 1);
                        golpe.ai[0] = npc.whoAmI;        // el blanco
                        golpe.ai[1] = danoGolpe;         // el daño final
                        golpe.localAI[1] = Dueño - 1;    // el dueño (sus stats calzan el golpe)
                    }
                }

                // EL RITMO — cada 2..19 ticks: stacks aceleran, cercanía
                // frena ((3 − intensidad) × 4: pegado = golpe y golpe).
                TiempoGolpe = (int)(poderQuemadura + (3f - IntensidadQuemadura) * 4f);
            }
            TiempoGolpe--;

            // ------------------------------------------------------------
            //  EL DECAIMIENTO Y LA MUERTE — fuera del campo la marca
            //  vive sus últimos 180 ticks: en los últimos 60 los stacks
            //  se derriten ×0.9/frame; a 0, el fuego se apaga.
            // ------------------------------------------------------------
            TimerQuemadura--;
            if (TimerQuemadura <= 60)
                StacksQuemadura = (int)(StacksQuemadura * 0.9f);
            if (TimerQuemadura <= 0)
                ApagarLaMarca();
        }

        /// <summary>La mitad mayor del hitbox (helper legible).</summary>
        private static float nPC_MitadMayor(NPC npc) =>
            Math.Max(npc.Hitbox.Width / 2f, npc.Hitbox.Height / 2f);

        /// <summary>La marca muere: todo el estado a cero.</summary>
        private void ApagarLaMarca()
        {
            Marcada = false;
            TimerQuemadura = 0;
            StacksQuemadura = 0;
            DanoQuemadura = 0;
            IntensidadQuemadura = 0f;
            TiempoGolpe = 0;
            Dueño = 0;
        }

        /// <summary>El Remap acotado del original (compartido con el ítem).</summary>
        private static float Remap(float v, float in1, float in2, float out1, float out2)
        {
            float k = (v - in1) / (in2 - in1);
            if (k < 0f) k = 0f;
            else if (k > 1f) k = 1f;
            return out1 + (out2 - out1) * k;
        }

        // ================================================================
        //  EL RESPLANDOR DEL IMPÍO ARDIENDO — el tinte cálido ENCIMA del
        //  sprite (la luz vive en PostAI; esto es el “glow” que se VE:
        //  un quad aditivo SoftGlow dorado del tamaño del cuerpo ×1.3 ×
        //  intensidad — el patrón VFXCore de la casa).
        // ================================================================
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!Marcada || OcultarEfectos || Main.gameMenu || Main.netMode == NetmodeID.Server)
                return;
            try
            {
                VFXCore.CerrarLoteSiAbierto();

                float t = Main.GlobalTimeWrappedHourly;
                float late = 0.8f + 0.2f * MathF.Sin(t * 7f + npc.whoAmI * 1.3f);
                float intensidad = MathHelper.Clamp(IntensidadQuemadura, 1f, 3f);

                // EL TINTE CÁLIDO — el cuerpo ardiendo (×1.3 × intensidad,
                // alfa tímido: es TINTE, no faro).
                float tam = MathF.Max(npc.width, npc.height) * 1.3f * intensidad;
                Color tinte = Color.Lerp(Color.Goldenrod, Color.Orange, 0.5f + 0.5f * MathF.Sin(t * 3f));
                VFXCore.Begin();
                VFXCore.Quad(npc.Center, tinte * (0.16f * late),
                    new Vector2(tam, tam));
                VFXCore.FlushAdditive();
            }
            catch
            {
                try { VFXCore.CerrarLoteSiAbierto(); } catch { }
            }
            VFXCore.ReabrirLoteVanilla();
        }
    }
}
