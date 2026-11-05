using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Systems;
using AethonMod.Content.VFX;
using AethonMod.Content.Weapons.Investigaciones;

namespace AethonMod.Content.Projectiles.Auras
{
    // ======================================================================
    //  DerrameDeTintaProjectile — v6.100 — EL CHARCO QUE HIERVE.
    //
    //  EL AURA ESTÁTICA (la investigación de auras, familia 2): se
    //  lanza y VIVE SOLO — el charco de tinta-sombra (7 tiles, radio
    //  112 px) HIERVE 10 segundos (600 ticks): muerde cada 15 ticks
    //  a los que lo pisan y la tinta les ATRAPA LAS PIERNAS (velocidad
    //  ×0.988 cada 6 ticks, acumulándose mientras pisen). Máximo 2
    //  charcos vivos por portador — el tercero SE BEBE al primero.
    //
    //  LA COREOGRAFÍA (las herramientas): LA CAÍDA — el charco cae del
    //  cielo como una gota de tinta (14 ticks de goteo con polvos) y
    //  al aterrizar suena el chapoteo con el ESTALLIDO en anillo; EL
    //  CHARCO ZONA del motor de píxeles (CharcoZona, semilla 411 —
    //  las burbujas y el géiser del taller) hirviendo en el suelo +
    //  el lote aditivo con el anillo mágico, el anillo de fuego, el
    //  ruido de la superficie y las 6 BURBUJAS que nacen del borde y
    //  se deshacen subiendo.
    // ======================================================================
    public class DerrameDeTintaProjectile : ModProjectile
    {
        /// <summary>EL RADIO del charco (px — 7 tiles).</summary>
        private const float RADIO = 112f;

        /// <summary>LA CADENCIA de la mordida (ticks).</summary>
        private const int CADENCIA = 15;

        /// <summary>LA CAÍDA de la gota (ticks).</summary>
        private const int CAIDA = 14;

        // El próximo mordisco por impío.
        private static readonly Dictionary<int, int> _proximo = new();

        // EL MOTOR DE PÍXELES (el charco hirviendo).
        private PixelFxPlayer _motor;

        private static Texture2D[] _ruidoCache;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 600;         // 10 segundos hirviendo
        }

        public override void OnSpawn(IEntitySource source)
        {
            // EL TOPE DE CASA: máximo 2 charcos vivos — el tercero se
            // bebe al más viejo (la tinta no se estanca).
            int vivos = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.type == Type &&
                    p.owner == Projectile.owner && p.whoAmI != Projectile.whoAmI)
                {
                    vivos++;
                    if (vivos >= 2)
                        p.Kill();
                }
            }
            Projectile.ai[1] = 1f;             // nace CAYENDO
        }

        public override void AI()
        {
            Projectile.ai[0] += 1f;
            float vida = Projectile.ai[0];

            // ------------------------------------------------------------
            //  LA CAÍDA — la gota de tinta baja del cielo goteando; al
            //  tocar (tick 14) EL CHAPOTEO: el golpe seco, el anillo.
            // ------------------------------------------------------------
            if (Projectile.ai[1] > 0f)
            {
                if (vida < CAIDA)
                {
                    if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
                        Dust.NewDustPerfect(
                            Projectile.Center + new Vector2(Main.rand.NextFloat(-8f, 8f),
                                -260f + vida * 19f),
                            27, new Vector2(0f, 2.5f), 140,
                            new Color(170, 110, 240), 0.6f).noGravity = false;
                    return;
                }
                Projectile.ai[1] = 0f;
                if (Main.netMode != NetmodeID.Server)
                {
                    SoundStyle chapoteo = SoundID.Item122.WithPitchOffset(-0.45f);
                    SoundEngine.PlaySound(in chapoteo, Projectile.Center);
                    for (int i = 0; i < 12; i++)
                    {
                        float ang = i * MathF.PI * 2f / 12f;
                        Dust.NewDustPerfect(Projectile.Center, 27,
                            new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.5f)
                                * Main.rand.NextFloat(2.5f, 5f),
                            150, new Color(170, 110, 240), 0.75f).noGravity = true;
                    }
                }
            }

            // ------------------------------------------------------------
            //  EL Hervor (solo cliente): el motor late y la luz violeta.
            // ------------------------------------------------------------
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                Motor().Tick();
                if (Projectile.timeLeft <= 12)
                    Motor().Fx.Stop();
                float apagarse = Math.Min(1f, Projectile.timeLeft / 40f);
                Lighting.AddLight(Projectile.Center,
                    new Vector3(0.66f, 0.43f, 0.94f) * 0.85f * apagarse);
            }

            // ------------------------------------------------------------
            //  LA MORDIDA DEL VERSO (solo el dueño manda).
            // ------------------------------------------------------------
            if (Projectile.owner == Main.myPlayer && !Main.gameMenu)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || !n.CanBeChasedBy() || n.dontTakeDamage)
                        continue;
                    if (n.Center.DistanceSQ(Projectile.Center) > RADIO * RADIO)
                        continue;

                    // LA TINTA ATRAPA LAS PIERNAS: cada 6 ticks pisando,
                    // el fango frena (acumulativo mientras pise).
                    if (Main.netMode != NetmodeID.MultiplayerClient && vida % 6f == 0f)
                        n.velocity *= 0.988f;

                    if (!_proximo.TryGetValue(n.whoAmI, out int proximo) || proximo <= 0)
                    {
                        GolpeMotor.Golpear(Projectile, n, Projectile.damage);
                        _proximo[n.whoAmI] = CADENCIA;
                    }
                    else
                    {
                        _proximo[n.whoAmI] = proximo - 1;
                    }
                }
            }

            // LA LIMPIEZA de los muertos (cada segundo).
            if (Main.GameUpdateCount % 60uL != 0)
                return;
            List<int> muertos = null;
            foreach (int k in _proximo.Keys)
            {
                NPC n = Main.npc[k];
                if (n == null || !n.active || n.life <= 0)
                    (muertos ??= new List<int>()).Add(k);
            }
            if (muertos == null)
                return;
            foreach (int k in muertos)
                _proximo.Remove(k);
        }

        public override void OnKill(int timeLeft)
        {
            _motor?.Dispose();
            _motor = null;
            if (Projectile.owner == Main.myPlayer)
                _proximo.Clear();
        }

        /// <summary>EL MOTOR: el CHARCO ZONA del motor de píxeles — las
        /// burbujas y el géiser del taller hirviendo en el suelo.</summary>
        private PixelFxPlayer Motor()
        {
            if (_motor == null)
                _motor = new PixelFxPlayer(
                    PixelFxCasas.CharcoZona(411,
                        PxColor.Rgb(170, 110, 240), PxColor.Rgb(35, 20, 55)),
                    1.9f);
            return _motor;
        }

        /// <summary>La lámina de ruido viva (el caché de las 20).</summary>
        private static Texture2D Ruido(int semilla, float vel)
        {
            _ruidoCache ??= new Texture2D[20];
            int i = (int)(Main.GlobalTimeWrappedHourly * vel + semilla * 0.37f) % 20;
            if (i < 0) i += 20;
            return _ruidoCache[i] ??= ModContent.Request<Texture2D>(
                "AethonMod/Content/Effects/Procedural/Ruido_" + i,
                AssetRequestMode.ImmediateLoad).Value;
        }

        private static Texture2D Tex(string ruta) =>
            ModContent.Request<Texture2D>("AethonMod/Content/Effects/" + ruta,
                AssetRequestMode.ImmediateLoad).Value;

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.gameMenu)
                return false;

            try
            {
                VFXCore.CerrarLoteSiAbierto();
                SpriteBatch sb = Main.spriteBatch;
                float t = Main.GlobalTimeWrappedHourly;
                float vida = Projectile.ai[0];
                float apagarse = Math.Min(1f, Projectile.timeLeft / 40f);
                Color tinta = new(170, 110, 240);
                Color lila = new(220, 190, 255);
                Vector2 centro = Projectile.Center - Main.screenPosition;

                // 1) LA CAÍDA — la gota que baja (crece al acercarse).
                if (Projectile.ai[1] > 0f)
                {
                    float caida = vida / CAIDA;
                    Vector2 pos = centro + new Vector2(0f, -260f * (1f - caida));
                    sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                        SamplerState.LinearClamp, DepthStencilState.None,
                        RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    Capa(sb, Tex("GlowOrb"), pos, tinta * 0.9f, 0f, (26f + 8f * caida) / 128f);
                    Capa(sb, Tex("GlowOrb"), pos, Color.White * 0.6f, 0f, 5f / 64f);
                    sb.End();
                    VFXCore.ReabrirLoteVanilla();
                    return false;
                }

                // 2) EL MOTOR DE PÍXELES — el Charco Zona hirviendo (el
                //    origen baja 18 px: el charco vive EN el suelo).
                if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                {
                    PixelFxPlayer motor = Motor();
                    sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                        SamplerState.PointClamp, DepthStencilState.None,
                        RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    motor.Draw(sb, centro - new Vector2(motor.Meta.Ox, motor.Meta.Oy + 18f) * motor.Escala);
                    sb.End();
                }

                // 3) EL LOTE ADITIVO — el charco.
                sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                float hervor = 0.8f + 0.2f * MathF.Sin(t * 2.6f);
                Capa(sb, Tex("MagicRing"), centro, tinta * (0.35f * hervor * apagarse),
                    t * 0.7f, 0.89687496f);
                Capa(sb, Tex("Procedural/FireRing"), centro, lila * (0.18f * hervor * apagarse),
                    -t * 0.5f, 0.9406251f);
                Capa(sb, Ruido(5, 2.8f), centro, tinta * (0.1f * apagarse),
                    t * 0.18f, 0.4375f);

                // LAS SEIS BURBUJAS — nacen del borde y se deshacen
                // subiendo (el hervor del charco).
                for (int i = 0; i < 6; i++)
                {
                    float azar = Azar(i);
                    float brota = (t * 1.3f + azar) % 1f;
                    float ang = azar * MathF.PI * 2f + t * 0.3f;
                    Vector2 pos = centro + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.42f) * RADIO
                        + new Vector2(-MathF.Cos(ang), -MathF.Sin(ang) * 0.42f) * (39.2f * brota)
                        - new Vector2(0f, 14f * MathF.Sin(brota * MathF.PI));
                    Capa(sb, Tex("GlowOrb"), pos, lila * (0.4f * (1f - brota) * apagarse),
                        0f, 9f / 128f);
                }

                sb.End();
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>El azar determinista de la casa.</summary>
        private static float Azar(int i)
        {
            float v = MathF.Sin(i * 12.9898f + 78.233f) * 43758.547f;
            return v - MathF.Floor(v);
        }

        private static void Capa(SpriteBatch sb, Texture2D tex, Vector2 pos,
            Color color, float rot, float esc)
        {
            if (color.A != 0 && esc > 0f)
                sb.Draw(tex, pos, null, color, rot, Utils.Size(tex) * 0.5f, esc,
                    SpriteEffects.None, 0f);
        }
    }
}
