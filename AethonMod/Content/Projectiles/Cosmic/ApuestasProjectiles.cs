using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Players;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// MetronomoPulsarHalo — v6.42 — APUESTA 1: EL METRÓNOMO DE PÚLSAR.
    ///
    /// El COMPÁS vivo: un púlsar en miniatura que flota sobre la cabeza
    /// del portador mientras sostiene el arma. Late cada 30 ticks
    /// (0,5 s exactos — el periodo del arma) con un anillo que se
    /// contrae hacia el tic, un destello y un clic perceptible.
    ///
    /// LA LECTURA DEL RITMO (tres calificaciones):
    ///   · PERFECT (±3 ticks del tic, ±50 ms): crítico garantizado +
    ///     abanico doble (dos pulsos hermanos a ±0,18 rad, 60% daño).
    ///   · GOOD (±4..6 ticks): crítico garantizado, sin abanico.
    ///   · OFF: el pulso sale tal cual — y el combo vuelve a 0. El
    ///     ritmo PREMIA pero nunca PENALIZA (la lección de los juegos
    ///     de ritmo bien hechos: el compás es un regalo, no un castigo).
    ///
    /// EL FARO: 8 aciertos consecutivos (PERFECT o GOOD) y el púlsar
    /// se convierte en FARO durante 180 ticks (3 s, DOS rotaciones
    /// completas a 0,0698 rad/t): un haz de 900 px que barre el campo
    /// con la envolvente del faro y pica a 0,75× cada 6 ticks por NPC.
    /// Luego, 480 ticks de silencio (el compás se re-aprende).
    ///
    /// CONVENCIONES DE LA CASA: reloj por Main.GameUpdateCount (NUNCA
    /// GlobalTimeWrappedHourly — el render late distinto que el juego),
    /// cero Main.rand en el render, lote cerrado→cerrado, daño del faro
    /// solo en autoridad con EsObjetivo.
    /// </summary>
    public class MetronomoPulsarHalo : ModProjectile
    {
        // === EL COMPÁS ===
        public const int Periodo = 30;         // ticks por tic del metrónomo
        public const int VentanaPerfecta = 3;  // ±3 ticks = PERFECT
        public const int VentanaBuena = 6;     // ±6 ticks = GOOD
        public const int ComboParaFaro = 8;    // 8 aciertos → el faro
        public const int FaroTicks = 180;      // 3 s de faro
        public const int FaroEnfriamiento = 480;
        public const float FaroLargo = 900f;
        public const float FaroOmega = 0.0698f;   // rad/t — dos vueltas exactas

        /// <summary>El combo actual de aciertos al compás.</summary>
        private int _combo;

        /// <summary>Ticks restantes del FARO (0 = apagado).</summary>
        private int _faroTicks;

        /// <summary>Ángulo actual del haz del faro.</summary>
        private float _angFaro;

        /// <summary>Enfriamiento del faro (para no re-dispararlo al instante).</summary>
        private int _faroCd;

        /// <summary>Cooldowns de picotazo del faro por NPC (iframe 6).</summary>
        private readonly Dictionary<int, int> _picotazos = new();

        /// <summary>Semilla determinista por identidad.</summary>
        private int Seed => Math.Max(1, Projectile.identity + 419);

        /// <summary>La fase del compás (0..29) — el reloj de todo el arma.</summary>
        public static int FaseCompas => (int)(Main.GameUpdateCount % Periodo);

        /// <summary>La distancia (en ticks) al tic más cercano.</summary>
        public static int DistAlTic => Math.Min(FaseCompas, Periodo - FaseCompas);

        // === LA PALETA (el blanco-frío del radio de un púlsar). ===
        private static readonly Color NucleoBlanco = new(240, 248, 255);
        private static readonly Color GlowFrio = new(120, 190, 255);
        private static readonly Color AnilloTic = new(80, 160, 255);
        private static readonly Color ComboEncendido = new(255, 214, 130);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.tileCollide = false;
            Projectile.friendly = false;       // el halo NO golpea: es el compás
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.aiStyle = -1;
            Projectile.netImportant = false;   // compañero local del portador
            Projectile.hide = false;
        }

        public override bool? CanDamage() => false;
        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead ||
                duenio.HeldItem == null ||
                duenio.HeldItem.type != ModContent.ItemType<Weapons.Cosmic.MetronomoPulsar>())
            {
                // El arma se guardó: el compás se apaga con un último suspiro.
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 12);
                Projectile.velocity *= 0.9f;
                return;
            }

            Projectile.timeLeft = 60;   // vivo mientras el arma siga en la mano

            // === LA POSICIÓN: sobre la cabeza, meciéndose con el paso. ===
            float mecer = MathF.Sin(Main.GlobalTimeWrappedHourly * 1.6f) * 6f;
            Vector2 ancla = duenio.Center + new Vector2(0f, -66f + mecer);
            Projectile.Center = Vector2.Lerp(Projectile.Center, ancla, 0.18f);
            Projectile.velocity = Vector2.Zero;

            // === EL TIC: el destello y el clic del compás. ===
            if (FaseCompas == 0)
            {
                Lighting.AddLight(Projectile.Center, GlowFrio.ToVector3() * 1.2f);
                if (Main.netMode != NetmodeID.Server)
                    SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.22f, Pitch = 0.65f },
                        Projectile.Center);
            }
            else
                Lighting.AddLight(Projectile.Center, GlowFrio.ToVector3() * 0.55f);

            if (_faroCd > 0) _faroCd--;

            // === EL FARO: el premio del combo — dos vueltas de haz. ===
            if (_faroTicks > 0)
            {
                _faroTicks--;
                _angFaro += FaroOmega;

                // El haz pica a todo lo que cruza (solo autoridad).
                if (Main.myPlayer == Projectile.owner)
                {
                    Vector2 a = Projectile.Center;
                    Vector2 b = a + new Vector2(MathF.Cos(_angFaro), MathF.Sin(_angFaro)) * FaroLargo;
                    int dmg = Math.Max(1, (int)(Projectile.damage * 0.75f));
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                        if (_picotazos.TryGetValue(npc.whoAmI, out int hasta) && hasta > 0) continue;

                        // Distancia punto-segmento (la matemática de la casa).
                        Vector2 ab = b - a;
                        float t = Vector2.Dot(npc.Center - a, ab) / Math.Max(1f, ab.LengthSquared());
                        t = MathHelper.Clamp(t, 0f, 1f);
                        Vector2 masCercano = a + ab * t;
                        if (Vector2.DistanceSquared(npc.Center, masCercano) < 42f * 42f)
                        {
                            npc.SimpleStrikeNPC(dmg, npc.direction, false, 2f, DamageClass.Magic);
                            _picotazos[npc.whoAmI] = 6;
                        }
                    }
                }
                // Los iframe del picotazo envejecen.
                foreach (int k in new List<int>(_picotazos.Keys))
                {
                    _picotazos[k]--;
                    if (_picotazos[k] <= 0) _picotazos.Remove(k);
                }

                if (_faroTicks == 0)
                    _faroCd = FaroEnfriamiento;
            }
        }

        // ==================================================================
        //  LA SEÑAL DEL ARMA — la llama desde el Shoot del ítem: registra
        //  el acierto (o el fallo) al compás y enciende el faro al llegar
        //  a 8. Estática para que el arma la encuentre sin instanciarse.
        // ==================================================================
        public static void NotificarAcierto(Player player, bool alCompas, bool perfecta)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active &&
                    p.type == ModContent.ProjectileType<MetronomoPulsarHalo>() &&
                    p.owner == player.whoAmI)
                {
                    if (p.ModProjectile is MetronomoPulsarHalo halo)
                        halo.RegistrarAcierto(alCompas, perfecta);
                    return;
                }
            }
        }

        private void RegistrarAcierto(bool alCompas, bool perfecta)
        {
            if (_faroTicks > 0) return;    // el faro ya suena: el combo espera

            if (!alCompas) { _combo = 0; return; }

            _combo++;
            if (_combo >= ComboParaFaro && _faroCd <= 0)
            {
                _combo = 0;
                _faroTicks = FaroTicks;
                _angFaro = -MathHelper.PiOver2;   // nace apuntando al cielo
                _picotazos.Clear();

                if (Main.netMode != NetmodeID.Server)
                {
                    SoundEngine.PlaySound(SoundID.Item68 with { Volume = 0.55f, Pitch = 0.4f },
                        Projectile.Center);
                    PulsoLib.EmpujarPantalla(new Color(160, 210, 255), 0.35f, 14);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                Vector2 pos = Projectile.Center - Main.screenPosition;
                float time = Main.GlobalTimeWrappedHourly;
                int fase = FaseCompas;
                float hastaTic = fase / (float)Periodo;   // 0 (acaba de tic) → 1 (a punto de tic)

                // === EL ANILLO DEL COMPÁS (búfer de VFXCore — coords de MUNDO):
                //     se CONTRAE hacia el tic (la cuenta atrás visible — el
                //     jugador aprende el compás con los ojos). ===
                float anillo = 14f + 40f * hastaTic;
                float aAnillo = 0.35f + 0.5f * (1f - hastaTic);
                VFXCore.Quad(Projectile.Center, AnilloTic * aAnillo,
                    VFXCore.RingQuadSize(anillo), 0f, VFXCore.Ring);

                // EL ANILLO DEL ORIGEN DEL FARO (búfer).
                if (_faroTicks > 0)
                    VFXCore.Quad(Projectile.Center, GlowFrio * 0.7f,
                        VFXCore.RingQuadSize(34f + 10f * MathF.Sin(time * 5f)), 0f, VFXCore.Ring);

                VFXCore.FlushAdditive(null, false);

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                // === EL NÚCLEO: late con el compás (brillante justo tras el tic). ===
                float brillo = 0.55f + 0.45f * (1f - hastaTic);
                Texture2D glow = VFXCore.SoftGlow;
                Main.spriteBatch.Draw(glow, pos, null, GlowFrio * (0.5f * brillo),
                    0f, glow.Size() * 0.5f, new Vector2(46f, 46f) / glow.Size(),
                    SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(glow, pos, null, NucleoBlanco * (0.9f * brillo),
                    0f, glow.Size() * 0.5f, new Vector2(16f, 16f) / glow.Size(),
                    SpriteEffects.None, 0f);

                // === LOS 8 TRAZOS DEL COMBO: la cuenta hacia el faro. ===
                for (int k = 0; k < ComboParaFaro; k++)
                {
                    float ang = -MathHelper.PiOver2 + (k - (ComboParaFaro - 1) / 2f) * 0.42f;
                    Vector2 p = pos + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 30f;
                    bool encendido = k < _combo;
                    Color c = encendido
                        ? ComboEncendido * (0.85f + 0.15f * MathF.Sin(time * 9f + k))
                        : new Color(70, 110, 170) * 0.4f;
                    Main.spriteBatch.Draw(glow, p, null, c, ang,
                        glow.Size() * 0.5f, new Vector2(10f, 5f) / glow.Size(),
                        SpriteEffects.None, 0f);
                }

                // === EL FARO: el haz que barre el campo. ===
                if (_faroTicks > 0)
                {
                    float resto = _faroTicks / (float)FaroTicks;
                    Vector2 dir = new Vector2(MathF.Cos(_angFaro), MathF.Sin(_angFaro));
                    LumenLib.Ray(Main.spriteBatch, pos, dir, FaroLargo, 46f,
                        GlowFrio, 0.9f * resto, 0.5f + 0.5f * MathF.Sin(time * 7f));
                    LumenLib.Ray(Main.spriteBatch, pos, dir, FaroLargo, 16f,
                        NucleoBlanco, 0.95f * resto, 0.5f + 0.5f * MathF.Sin(time * 7f + 0.6f));
                }

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            if (wasActive)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }

    /// <summary>
    /// PulsoPulsarProjectile — v6.42 — EL PULSO DEL METRÓNOMO.
    ///
    /// El disparo del compás: una astilla de radio blanco-frío con su
    /// cola de fantasmas (EcosLib — la memoria de la casa). El arma le
    /// puso el crítico y el abanico EN EL NACIMIENTO según el compás;
    /// aquí solo vuela, brilla y muere con su pequeña onda.
    /// </summary>
    public class PulsoPulsarProjectile : ModProjectile
    {
        private EcosLib.Memoria _memoria;
        private int _edad;

        private int Seed => Math.Max(1, Projectile.identity + 613);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.tileCollide = true;
            Projectile.aiStyle = -1;
            _memoria = EcosLib.Crear();
        }

        public override void AI()
        {
            _edad++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Vector3(0.35f, 0.5f, 0.8f));
            EcosLib.Registrar(ref _memoria, Projectile.Center, Projectile.rotation);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 6; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueCrystalShard);
                float ang = i / 6f * MathHelper.TwoPi;
                d.velocity = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 1.8f;
                d.scale = 0.9f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                // LA COLA DE FANTASMAS (búfer de VFXCore + volcado).
                EcosLib.ColaHistoria(ref _memoria, 2, 6,
                    new Color(120, 190, 255), new Vector2(26f, 10f), 0.55f, 1.5f);
                VFXCore.FlushAdditive(null, false);

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                Texture2D glow = VFXCore.SoftGlow;
                Vector2 pos = Projectile.Center - Main.screenPosition;
                Main.spriteBatch.Draw(glow, pos, null, new Color(120, 190, 255) * 0.6f,
                    Projectile.rotation, glow.Size() * 0.5f,
                    new Vector2(40f, 14f) / glow.Size(), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(glow, pos, null, new Color(240, 248, 255) * 0.95f,
                    Projectile.rotation, glow.Size() * 0.5f,
                    new Vector2(22f, 7f) / glow.Size(), SpriteEffects.None, 0f);

                Main.spriteBatch.End();
            }
            catch
            {
                try { Main.spriteBatch.End(); } catch { }
            }

            if (wasActive)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
