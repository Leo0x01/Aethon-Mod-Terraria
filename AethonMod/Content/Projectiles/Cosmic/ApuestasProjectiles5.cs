using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmic
{
    /// <summary>
    /// PulsoEcoProjectile — v6.42 — APUESTA 5: EL ECO CUÁNTICO.
    ///
    /// EL PULSO QUE SE RECUERDA: cada disparo vuela 180 px (o hasta
    /// morder) y entonces DECOHIERE — se detiene como SEMILLA FANTASMAL
    /// (30 ticks de carga, re-apuntando cada 10 ticks al enemigo más
    /// cercano) y luego SE RE-EMITE como ECO de la siguiente
    /// generación: daño ×0,6 (el 60% de la física de decoherencia:
    /// 1−e⁻¹ = la pérdida natural de un T2), visual fantasmal del
    /// frío del vacío (LumenPalettes.VoidCold + el parpadeo de los
    /// espejos de EspectroLib).
    ///
    /// TRES generaciones (100% → 60% → 36% → 21,6%): 2,176× de daño
    /// total por disparo si toda la cadena encuentra a quién morder.
    /// Tope de 6 ecos vivos (la proliferación no se come el frame).
    ///
    /// LA RESONANCIA (clic derecho del arma): todas las semillas
    /// vivas disparan AL INSTANTE y a la vez — el coro cuántico.
    /// Enfriamiento 300 ticks.
    ///
    /// CONVENCIONES DE LA CASA: generación en ai[0], fase en ai[1],
    /// distancia en ai[2] (maxAI = 3); el re-emisión lo decide SOLO
    /// la autoridad; cero Main.rand en el render.
    /// </summary>
    public class PulsoEcoProjectile : ModProjectile
    {
        public const float DistanciaDecoherencia = 180f;
        public const int SemillaTicks = 30;
        public const float FactorGeneracion = 0.6f;
        public const int MaxGeneraciones = 3;
        public const int MaxEcosVivos = 6;

        private EspectroLib.Memoria _memoria;
        private NPC _presa;

        private int Seed => Math.Max(1, Projectile.identity + 1493);

        private int Generacion => (int)Projectile.ai[0];
        private float Fase
        {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;               // muerde varias veces: la cadena es NUESTRA
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.aiStyle = -1;
            _memoria = EspectroLib.Crear();
        }

        public override void AI()
        {
            if (Fase == 0f)
            {
                // === EL VUELO (recto y limpio — el eco no serpentea). ===
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.ai[2] += Projectile.velocity.Length();

                Color luz = Generacion == 0
                    ? new Color(0.9f, 0.85f, 0.55f)
                    : new Color(0.45f, 0.75f, 0.9f);
                Lighting.AddLight(Projectile.Center, luz.ToVector3() * 0.8f);
                EspectroLib.Registrar(ref _memoria, Projectile.Center, Projectile.rotation);

                // LA DECOHERENCIA: 180 px volados → la semilla.
                if (Projectile.ai[2] >= DistanciaDecoherencia)
                    Decoherir();
            }
            else if (Fase > 0f)
            {
                // === LA SEMILLA: el fantasma que carga el próximo eco. ===
                Projectile.velocity = Vector2.Zero;
                Lighting.AddLight(Projectile.Center, new Vector3(0.35f, 0.6f, 0.85f) * 0.6f);

                // El re-apuntado cada 10 ticks (la presa viva más cercana).
                if ((int)Fase % 10 == 0)
                    ReApuntar();

                Fase -= 1f;
                if (Fase <= 0f)
                    ReEmitir();
            }
            else
            {
                // === LA RESONANCIA: el coro dispara YA. ===
                Fase = 0f;
                ReApuntar();
                ReEmitir();
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // La mordida también decohiere: la semilla nace DONDE mordió.
            if (Fase == 0f)
                Decoherir();
        }

        private void Decoherir()
        {
            Fase = SemillaTicks;
            Projectile.velocity = Vector2.Zero;
            Projectile.ai[2] = 0f;
            _presa = null;
            ReApuntar();

            if (Main.netMode != NetmodeID.Server)
                SoundEngine.PlaySound(SoundID.Item152 with { Volume = 0.25f, Pitch = 0.3f },
                    Projectile.Center);
        }

        private void ReApuntar()
        {
            NPC mejor = null;
            float mejorD = float.MaxValue;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || !npc.CanBeChasedBy()) continue;
                float d = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (d < 900f * 900f && d < mejorD) { mejorD = d; mejor = npc; }
            }
            _presa = mejor;
        }

        /// <summary>
        /// LA RE-EMISIÓN: el eco de la siguiente generación (la autoridad
        /// decide — y transforma ESTE proyectil en el eco: la cadena no
        /// multiplica entidades, se HEREDA).
        /// </summary>
        private void ReEmitir()
        {
            if (Generacion >= MaxGeneraciones || ContarEcosVivos(Projectile.owner) >= MaxEcosVivos ||
                (_presa == null || !_presa.active))
            {
                // Sin presa o cadena agotada: el eco se disuelve con un
                // pequeño destello frío.
                Projectile.Kill();
                return;
            }

            Vector2 dir = (_presa.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
            Projectile.ai[0] = Generacion + 1;
            Projectile.damage = Math.Max(1, (int)(Projectile.damage * FactorGeneracion));
            Projectile.velocity = dir * 11f;
            Projectile.ai[2] = 0f;
            Fase = 0f;
            Projectile.netUpdate = true;
            _memoria = EspectroLib.Crear();

            if (Main.netMode != NetmodeID.Server)
                SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.3f, Pitch = 0.45f },
                    Projectile.Center);
        }

        /// <summary>Los ecos vivos del arma (generación > 0) de ESTE dueño.
        /// v6.50.2 — FIX (tope GLOBAL de 6 en MP): el conteo no filtraba por
        /// dueño y dos portadores del Eco Cuántico compartían el tope de 6
        /// ecos vivos (la proliferación de uno se comía la del otro). El
        /// doc-comment siempre dijo "de este dueño" — ahora el filtro existe.</summary>
        public static int ContarEcosVivos(int owner)
        {
            int n = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active &&
                    p.type == ModContent.ProjectileType<PulsoEcoProjectile>() &&
                    p.owner == owner &&
                    p.ai[0] > 0.5f)
                    n++;
            }
            return n;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 5; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueCrystalShard);
                float ang = i / 5f * MathHelper.TwoPi;
                d.velocity = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 1.4f;
                d.scale = 0.8f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            // v6.50.11 — sonda: cierra el lote del juego SOLO si hay un Begin
            // vivo (el try{End}catch disparaba una first-chance que tML 2026.07
            // registra como "Excepción silenciosa" — 27 stacks únicos en el
            // client.log del usuario, todas capturadas: ruido de diagnóstico).
            VFXCore.CerrarLoteSiAbierto();

            try
            {
                bool eco = Generacion > 0;

                // LA COLA: fantasmas del tinte de su generación.
                Color tinte = eco ? new Color(120, 200, 255) : new Color(255, 225, 140);
                if (Fase != 0f)
                {
                    // LA SEMILLA: quieta, parpadeando como un espejo que decide.
                    float parpadeo = EspectroLib.TempoEspejos(Main.GlobalTimeWrappedHourly, Seed);
                    VFXCore.Quad(Projectile.Center, tinte * (0.5f * parpadeo),
                        new Vector2(30f, 30f));
                    VFXCore.Quad(Projectile.Center, Color.White * (0.35f * parpadeo),
                        new Vector2(12f, 12f));
                    VFXCore.FlushAdditive(null, false);
                }
                else
                {
                    EspectroLib.ColaHistoria(ref _memoria, 2, 6, tinte,
                        new Vector2(24f, 10f), eco ? 0.5f : 0.6f, 1.4f);
                    VFXCore.FlushAdditive(null, false);
                }

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                Texture2D glow = VFXCore.SoftGlow;
                Vector2 pos = Projectile.Center - Main.screenPosition;

                if (Fase != 0f)
                {
                    // LA SEMILLA (el anillo que se contrae hacia el disparo —
                    //     pase DIRECTO sobre el lote abierto).
                    float t = Fase / SemillaTicks;
                    Texture2D ring = VFXCore.Ring;
                    float s = (8f + 26f * t) * 2.174f;
                    Main.spriteBatch.Draw(ring, pos, null, tinte * 0.6f, 0f,
                        ring.Size() * 0.5f, new Vector2(s, s) / ring.Size(),
                        SpriteEffects.None, 0f);
                }
                else
                {
                    float alfa = eco ? 0.55f : 0.95f;
                    Main.spriteBatch.Draw(glow, pos, null, tinte * (0.55f * alfa),
                        Projectile.rotation, glow.Size() * 0.5f,
                        new Vector2(34f, 12f) / glow.Size(), SpriteEffects.None, 0f);
                    Color nucleo = eco ? new Color(200, 235, 255) : new Color(255, 250, 225);
                    Main.spriteBatch.Draw(glow, pos, null, nucleo * alfa,
                        Projectile.rotation, glow.Size() * 0.5f,
                        new Vector2(18f, 7f) / glow.Size(), SpriteEffects.None, 0f);
                }

                Main.spriteBatch.End();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }

            // v6.50.11 — CURACIÓN: el lote sale SIEMPRE ABIERTO y vanilla (si
            // llegó cerrado por un mod ajeno, se cura — el restore condicional
            // devolvía el veneno y tML mataba al proyectil: active=false).
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
