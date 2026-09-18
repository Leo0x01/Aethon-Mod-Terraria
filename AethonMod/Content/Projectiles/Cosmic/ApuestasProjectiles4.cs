using System;
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
    /// GuardiaNovaProjectile — v6.42 — APUESTA 4: LA ÉGIDA DE NOVA (la guardia).
    ///
    /// LA GUARDIA ALZADA: 18 ticks de círculo rúnico dorado alrededor del
    /// portador que se despliega en los 3 primeros (la apertura legible).
    /// Los primeros 8 ticks son LA VENTANA PERFECTA: un golpe esquivable
    /// en esa ventana NO EXISTE (FreeDodge del jugador lo deshace) y en
    /// su lugar detona LA NOVA DEL PARRY (invulnerabilidad de 1 s + 3×
    /// daño en 240 px + empujón + quemadura).
    ///
    /// Si la ventana pasa sin parar nada, LA GUARDIA SE QUEBRÓ: el
    /// portador queda ATURDIDO 30 ticks (0,5 s — no puede usar ítems,
    /// el precio del parry fallido). Los ticks 9..18 son bloqueo tardío:
    /// el daño entra amortiguado ×0,3 (ModifyHurt).
    ///
    /// CONVENCIONES DE LA CASA: la lógica vive en ApuestasPlayer (el
    /// estado del jugador); este proyectil es la CARA de la guardia y
    /// el testigo del fallo.
    /// </summary>
    public class GuardiaNovaProjectile : ModProjectile
    {
        public const int GuardiaTotal = 18;
        public const int VentanaPerfecta = 8;

        private static readonly Color OroCuerpo = new(255, 200, 110);
        private static readonly Color OroPunta = new(255, 242, 200);

        private int _edad;

        private int Seed => Math.Max(1, Projectile.identity + 1163);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = GuardiaTotal;
            Projectile.aiStyle = -1;
            Projectile.netImportant = false;
        }

        public override bool? CanDamage() => false;
        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            _edad++;
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead) { Projectile.Kill(); return; }
            Projectile.Center = duenio.Center;
            Projectile.velocity = Vector2.Zero;
            Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.78f, 0.4f) * 0.55f);

            // === EL TESTIGO DEL FALLO: la guardia expiró sin parar nada. ===
            if (Projectile.timeLeft <= 1 && !duenio.GetModPlayer<ApuestasPlayer>().ParryHecho)
            {
                duenio.GetModPlayer<ApuestasPlayer>().Aturdido = 30;
                if (Main.netMode != NetmodeID.Server)
                {
                    SoundEngine.PlaySound(SoundID.Item86 with { Volume = 0.4f, Pitch = -0.5f },
                        Projectile.Center);
                    // EL LATIDO DEL ERROR: un empujón gris suave — el mundo se enfría.
                    PulsoLib.EmpujarPantalla(new Color(150, 150, 160), 0.22f, 10);
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
                Vector2 centro = Projectile.Center - Main.screenPosition;
                float time = Main.GlobalTimeWrappedHourly;

                // LA APERTURA: el círculo se despliega en 3 ticks y respira.
                float abrir = Math.Min(1f, _edad / 3f);
                float radio = 74f * abrir;
                float respirar = 1f + 0.04f * MathF.Sin(time * 6f);

                // EL ECO DE LA VENTANA: un segundo aro más fino, más rápido —
                // los DOS primeros ticks del aro son la ventana perfecta
                // hecha visible (late al doble).
                bool enVentana = _edad <= VentanaPerfecta;
                float fade = 1f - Math.Max(0f, (_edad - VentanaPerfecta) / 10f) * 0.5f;

                // === EL CÍRCULO RÚNICO (búfer de VFXCore — coords de MUNDO):
                //     el conjuro de la égida (8 glifos). ===
                SigiloLib.AnilloRunico(Projectile.Center, radio * 1.06f * respirar, radio * 0.62f,
                    -0.34f, time * 0.4f, 8, 1.5f, time, 3,
                    OroCuerpo * (0.8f * fade), OroPunta * (0.9f * fade), 0.9f * fade);

                // EL AVISO DE VENTANA (búfer): aro blanco fino que late al
                // doble mientras el parry es posible.
                if (enVentana)
                {
                    float latir = 0.55f + 0.45f * MathF.Sin(time * 12f);
                    VFXCore.Quad(Projectile.Center, Color.White * (0.5f * latir * fade),
                        VFXCore.RingQuadSize(radio * 0.82f), 0f, VFXCore.Ring);
                }

                VFXCore.FlushAdditive(null, false);

                // === EL ANILLO ENERGÉTICO (pase directo — coords de PANTALLA). ===
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                OrbitaLib.AnilloEnergia(centro, radio * respirar, time, Seed,
                    StormLib.FlickTick(time, 12f), front: false,
                    hot: OroPunta, mid: OroCuerpo, deep: new Color(160, 90, 30), bright: 0.9f * fade);

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
    /// AnilloEnfriamientoProjectile — v6.42 — EL ANILLO DEL ENFRIAMIENTO.
    ///
    /// La cuenta atrás VISIBLE de la égida: 480 ticks de anillo de
    /// energía que se CONTRAE alrededor del portador (de 64 px a 20)
    /// — cuando el anillo termina de cerrarse, la guardia vuelve a
    /// estar disponible. Los últimos 30 ticks brillan: el arma AVISA
    /// que ya puede parar.
    /// </summary>
    public class AnilloEnfriamientoProjectile : ModProjectile
    {
        public const int Enfriamiento = 480;

        private static readonly Color OroTenue = new(200, 150, 80);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Enfriamiento;
            Projectile.aiStyle = -1;
            Projectile.netImportant = false;
        }

        public override bool? CanDamage() => false;
        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead) { Projectile.Kill(); return; }
            Projectile.Center = duenio.Center;
            Projectile.velocity = Vector2.Zero;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            bool wasActive = true;
            try { Main.spriteBatch.End(); }
            catch { wasActive = false; }

            try
            {
                float t = 1f - Projectile.timeLeft / (float)Enfriamiento;   // 0→1
                float radio = MathHelper.Lerp(64f, 20f, t);
                float listo = Projectile.timeLeft < 30f ? 1f : 0f;
                float alpha = 0.28f + 0.35f * t + listo * 0.4f;
                float time = Main.GlobalTimeWrappedHourly;

                // EL AVISO DE LISTO (búfer): el anillo late fuerte al final.
                if (listo > 0f)
                    VFXCore.Quad(Projectile.Center, OroTenue * (0.4f * MathF.Abs(MathF.Sin(time * 8f))),
                        VFXCore.RingQuadSize(radio * 1.3f), 0f, VFXCore.Ring);
                VFXCore.FlushAdditive(null, false);

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                OrbitaLib.AnilloFino(Projectile.Center - Main.screenPosition, radio, 0f,
                    OroTenue * alpha);

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
    /// NovaParryProjectile — v6.42 — LA NOVA DEL PARRY.
    ///
    /// EL PREMIO: parar un golpe en la ventana perfecta detona esta
    /// nova — la onda expansiva dorada con aberración cromática (la
    /// escuela de las ondas de la casa), la flor de fuego de seis
    /// lengüetas y EL GOLPE: 3× el daño del arma en 240 px con empujón
    /// 9 y quemadura. El jugador queda invulnerable 60 ticks (el
    /// impulso del parry perfecto).
    /// </summary>
    public class NovaParryProjectile : ModProjectile
    {
        public const int Vida = 24;
        public const float RadioNova = 240f;

        private int Seed => Math.Max(1, Projectile.identity + 1231);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.aiStyle = -1;
            Projectile.netImportant = false;
        }

        public override bool? CanDamage() => false;
        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.85f, 0.5f) * 1.4f);
        }

        public override void OnKill(int timeLeft)
        {
            // EL GOLPE DE LA NOVA (la autoridad es quien paró — el jugador local).
            if (Main.myPlayer != Projectile.owner) return;
            int dmg = Math.Max(1, Projectile.damage * 3);
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                if (Vector2.Distance(npc.Center, Projectile.Center) > RadioNova) continue;
                npc.SimpleStrikeNPC(dmg, npc.Center.X < Projectile.Center.X ? -1 : 1,
                    Projectile.CritChance > 0, 9f, DamageClass.Melee);
                npc.AddBuff(BuffID.OnFire, 160);
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
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                float prog = 1f - Projectile.timeLeft / (float)Vida;
                Vector2 pos = Projectile.Center - Main.screenPosition;
                float time = Main.GlobalTimeWrappedHourly;

                // LA ONDA CON ABERRACIÓN: la escuela de las ondas cromáticas.
                OndaLib.Shock(Main.spriteBatch, pos, prog, RadioNova,
                    new Color(255, 214, 120), 1f, Seed, 12f, OndaFalloff.Quadratic, true);
                OndaLib.Shock(Main.spriteBatch, pos, prog * 0.85f, RadioNova * 0.8f,
                    Color.White, 0.8f, Seed + 7, 8f);

                // LA FLOR DE FUEGO: el estallido de la casa.
                PyraLib.Estallido(Main.spriteBatch, pos, RadioNova * 0.42f, prog,
                    PyraPalettes.SolarFire, Seed, time, 0.9f);

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
    /// EmbestidaNovaProjectile — v6.42 — EL EMBESTÓN DE LA ÉGIDA.
    ///
    /// El clic izquierdo: el golpe de escudo — una onda corta y dura
    /// (160 px) hacia donde mira el jugador, con el empujón 9 del
    /// bulldozer. Daño modesto (el escudo no es un arma de DPS: es
    /// el castigo de espacio personal).
    /// </summary>
    public class EmbestidaNovaProjectile : ModProjectile
    {
        public const int Vida = 14;
        public const float RadioEmbestida = 160f;

        private int Seed => Math.Max(1, Projectile.identity + 1381);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Vida;
            Projectile.aiStyle = -1;
            Projectile.netImportant = false;
        }

        public override bool? CanDamage() => false;
        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.8f, 0.45f) * 0.9f);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.myPlayer != Projectile.owner) return;
            int dmg = Math.Max(1, Projectile.damage);
            int dir = (int)Projectile.ai[0];
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || !VFXCore.EsObjetivo(npc)) continue;
                if (Vector2.Distance(npc.Center, Projectile.Center) > RadioEmbestida) continue;
                npc.SimpleStrikeNPC(dmg, dir, false, 9f, DamageClass.Melee);
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
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);

                float prog = 1f - Projectile.timeLeft / (float)Vida;
                OndaLib.Shock(Main.spriteBatch, Projectile.Center - Main.screenPosition, prog,
                    RadioEmbestida, new Color(255, 190, 100), 0.9f, Seed, 10f);

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
