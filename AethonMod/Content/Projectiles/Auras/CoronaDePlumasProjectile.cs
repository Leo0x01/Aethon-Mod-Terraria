using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Systems;
using AethonMod.Content.VFX;
using AethonMod.Content.Weapons.Investigaciones;

namespace AethonMod.Content.Projectiles.Auras
{
    // ======================================================================
    //  CoronaDePlumasProjectile — v6.100 — EL ARO DE PLUMAS VIVAS.
    //
    //  EL AURA CANALIZADA (la investigación de auras, familia 1): el
    //  aura vive SOLO mientras el portador sostiene el arma — el aro
    //  de SIETE PLUMAS orbita a 150 px mordiendo cada 18 ticks a los
    //  que pisan tu cielo. CADA MORDIDA ESCRIBE: la marca sube +15%
    //  el daño de la siguiente (hasta 5 marcas) — EL ANOTADO pica
    //  cada vez más.
    //
    //  EL VESTIDO (las herramientas de la sesión): EL NIDO COLMENA
    //  del motor de píxeles latiendo bajo el portador (la puerta del
    //  taller, semilla 233, blanco-verde de la Pluma) + el lote
    //  aditivo con las SIETE PLUMAS (Crescent girando con la órbita,
    //  tamaño respirando), la pluma que se DESPRENDE y sube, las
    //  chispas del nido y el disco del suelo.
    // ======================================================================
    public class CoronaDePlumasProjectile : ModProjectile
    {
        /// <summary>EL RADIO del aro (px).</summary>
        private const float RADIO = 150f;

        /// <summary>LA CADENCIA de la mordida (ticks).</summary>
        private const int CADENCIA = 18;

        /// <summary>LAS MARCAS máximas del ANOTADO.</summary>
        private const int MAX_MARCAS = 5;

        // EL ESTADO por impío: cuántas marcas lleva y cuándo muerde otra vez.
        private static readonly Dictionary<int, int> _marcas = new();
        private static readonly Dictionary<int, int> _proximo = new();

        // EL MOTOR DE PÍXELES (el nido bajo los pies).
        private PixelFxPlayer _motor;

        // El ítem que lo invocó (la corona se apaga si cambias de arma).
        private int _tipoItem;

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
        }

        public override void AI()
        {
            Player dueno = Main.player[Projectile.owner];
            // EL CANAL: la corona vive mientras el portador la sostenga
            // (y no muera ni entre al menú).
            if (dueno == null || !dueno.active || dueno.dead ||
                !dueno.channel || (_tipoItem != 0 && dueno.HeldItem.type != _tipoItem))
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = dueno.MountedCenter;
            Projectile.timeLeft = 2;

            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                Motor().Tick();
                if (Projectile.timeLeft <= 12)
                    Motor().Fx.Stop();
                Lighting.AddLight(Projectile.Center, new Vector3(0.62f, 0.96f, 0.86f) * 0.9f);
            }

            // ------------------------------------------------------------
            //  LAS MORDIDAS DEL ARO (solo el dueño manda).
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

                    if (!_proximo.TryGetValue(n.whoAmI, out int proximo) || proximo <= 0)
                    {
                        // LA MORDIDA: el daño crece con LAS MARCAS que ya
                        // lleva el anotado (+15% cada una, hasta 5).
                        _marcas.TryGetValue(n.whoAmI, out int marcas);
                        int dano = (int)(Projectile.damage * (1f + 0.15f * marcas));
                        _marcas[n.whoAmI] = Math.Min(MAX_MARCAS, marcas + 1);
                        GolpeMotor.Golpear(Projectile, n, dano);
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
            foreach (int k in _marcas.Keys)
            {
                NPC n = Main.npc[k];
                if (n == null || !n.active || n.life <= 0)
                    (muertos ??= new List<int>()).Add(k);
            }
            if (muertos == null)
                return;
            foreach (int k in muertos)
            {
                _marcas.Remove(k);
                _proximo.Remove(k);
            }
        }

        public override void OnSpawn(IEntitySource source)
        {
            // EL VÍNCULO con el ítem que la invocó (el canal se corta
            // si el portador cambia de arma).
            if (source is EntitySource_ItemUse uso && uso.Item != null)
                _tipoItem = uso.Item.type;
        }

        public override void OnKill(int timeLeft)
        {
            _motor?.Dispose();
            _motor = null;
            if (Projectile.owner == Main.myPlayer)
            {
                _marcas.Clear();
                _proximo.Clear();
            }
        }

        /// <summary>EL MOTOR: el NIDO COLMENA del motor de píxeles —
        /// las 7 celdas del nido encendiéndose bajo el portador.</summary>
        private PixelFxPlayer Motor()
        {
            if (_motor == null)
                _motor = new PixelFxPlayer(
                    PixelFxCasas.NidoColmena(233,
                        PxColor.Rgb(235, 250, 245), PxColor.Rgb(80, 210, 170)),
                    1.7f);
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
            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active)
                return false;

            try
            {
                VFXCore.CerrarLoteSiAbierto();
                SpriteBatch sb = Main.spriteBatch;
                float t = Main.GlobalTimeWrappedHourly;

                Vector2 centro = Projectile.Center - Main.screenPosition;
                Vector2 suelo = centro + new Vector2(0f, 26f);
                Color pluma = new(235, 250, 245);   // el blanco de la pluma
                Color nido = new(80, 210, 170);     // el verde del santuario

                // 1) EL MOTOR DE PÍXELES — el Nido Colmena (lote
                //    ambiente PointClamp, la alfa recta del compositor).
                if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                {
                    PixelFxPlayer motor = Motor();
                    sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                        SamplerState.PointClamp, DepthStencilState.None,
                        RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    motor.Draw(sb, dueno.MountedCenter - Main.screenPosition
                        - new Vector2(motor.Meta.Ox, motor.Meta.Oy) * motor.Escala);
                    sb.End();
                }

                // 2) EL LOTE ADITIVO — el aro.
                sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                // EL DISCO DEL SUELO (el cielo de la corona, elíptico).
                Suelo(sb, Tex("GlowCircleWhite"), suelo, nido * 0.1f, 0f, 330f, 105f);
                Suelo(sb, Ruido(3, 3.1f), suelo, pluma * 0.06f, t * 0.2f, 300f, 93f);

                // LAS SIETE PLUMAS — la media luna (Crescent) de cada una
                // girando con la órbita, RESPIRANDO de tamaño, con su orbe.
                for (int i = 0; i < 7; i++)
                {
                    float ang = t * 1.4f + i * MathF.PI * 2f / 7f;
                    Vector2 pos = centro + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.72f) * RADIO;
                    Capa(sb, Tex("Procedural/Crescent"), pos,
                        Color.Lerp(nido, pluma, 0.4f) * 0.65f, ang + MathF.PI / 2f,
                        (30f + 10f * MathF.Sin(t * 3f + i)) / 128f);
                    Capa(sb, Tex("GlowOrb"), pos, pluma * 0.45f, ang, 0.125f);
                }

                // LA PLUMA DESPRENDIDA — la que se sale del aro, se mece
                // y sube deshaciéndose en luz.
                float sube = t * 0.55f % 1f;
                int cual = (int)(t * 0.55f) % 7;
                float angPluma = t * 1.4f + cual * MathF.PI * 2f / 7f;
                Vector2 posSuelta = centro
                    + new Vector2(MathF.Cos(angPluma), MathF.Sin(angPluma) * 0.72f) * RADIO
                    + new Vector2(MathF.Sin(t * 3f) * 14f, sube * 90f);
                Capa(sb, Tex("Procedural/Crescent"), posSuelta,
                    pluma * (0.5f * (1f - sube)), t * 2.4f, 5f / 32f);

                // LAS CHISPAS DEL NIDO — cinco que suben del disco.
                for (int i = 0; i < 5; i++)
                {
                    float azar = Azar(i);
                    float altura = (t * 34f + azar * 80f) % 80f;
                    Vector2 pos = new(suelo.X + (azar * 2f - 1f) * RADIO * 0.85f,
                        suelo.Y - altura);
                    Capa(sb, Tex("SparkleStar"), pos,
                        pluma * (0.3f * (1f - altura / 80f)), -t * 2.2f,
                        (9f + 6f * Azar(i + 5)) / 32f);
                }

                // EL NIDO — el resplandor bajo el portador.
                Capa(sb, Tex("GlowCircleWhite"), suelo - new Vector2(0f, 6f),
                    pluma * 0.15f, 0f, 1.1875f);

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

        private static void Suelo(SpriteBatch sb, Texture2D tex, Vector2 pos,
            Color color, float rot, float anchoPx, float altoPx)
        {
            if (color.A != 0 && anchoPx > 0f)
                sb.Draw(tex, pos, null, color, rot, Utils.Size(tex) * 0.5f,
                    new Vector2(anchoPx / tex.Width, altoPx / tex.Height),
                    SpriteEffects.None, 0f);
        }
    }
}
