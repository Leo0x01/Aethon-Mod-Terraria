using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using global::AethonMod.Content.Effects;

namespace AethonMod.Content.Weapons.Investigaciones
{
    // ======================================================================
    //  v6.85 — LA INVESTIGACIÓN R8: «SOLYN, BUT A WEAPON» — EL RAYO DE
    //  ENERGÍA.
    //
    //  LA LETRA: «investiga el ataque de este mod Solyn, but a weapon,
    //  tambien es un rayo de energia y me gustaria que lo investigues
    //  bien, ya sabes, investiga la tecnica vfx, shader y texturas usadas
    //  para ese rayo de energia y crea un arma a consecuencia, asegurate
    //  de que sea igual o muy similar al ataque usado al mod».
    //
    //  EL HALLAZGO (informe /tmp/research/r8_solyn.md — el mod es de
    //  Arkadiy Debkin, evolucionado en «Wrath of the beams»; Solyn es el
    //  NPC estrella de Calamity: Wrath of the Gods; el tag-team beam de
    //  la pelea de Mars/Draedon). LOS PARÁMETROS EXACTOS (§d):
    //   · CANALIZADA: clic → carga 0,75 s (45 ticks: el destello estelar
    //     gira 2 vueltas enteras) → RÁFAGA PERSISTENTE ~1,9 s (225
    //     ticks, MaxUpdates 2) que BARRE tras el ratón (AngleLerp 0,15).
    //   · EL RAYO: alcance 5.600 px creciendo +175/actualización; banda
    //     base 96 px de semiancho con LATIDO ±6 px a 14,3 Hz (90 rad/s),
    //     bulto de 32 en el arranque, arranque redondeado
    //     sqrt(1.001−x²) y cierre de 8 frames.
    //   · LOS COLORES: núcleo rosa (255,45,123) · espina dorada
    ///    (255,196,36) · fogonazo melocotón (255,174,147) · bloom magenta
    //     (255,10,150)×0,34 · polvos Electric/PortalBoltTrail en
    //     HotPink/Wheat.
    //   · 3 CAPAS: la tira de bloom ×1,9 + el núcleo con RUIDO
    //     DENDRÍTICO fluyendo (los filamentos eléctricos) + la espina
    //     fina dorada; el muzzle: 3 flares de 4 puntas + 2 glows
    //     (melocotón).
    //
    //  LA VERSIÓN DE LA CASA: el shader dendrítico del original se
    //  recrea con RayoStrip.FajaTexturadaEnLote + la textura
    //  RuidoDendritico (512², ridge-noise tileable generado por la
    //  casa — el informe R8 prefiere REGENERAR en vez de copiar binarios
    //  ajenos): los filamentos fluyen raíz→punta con el parámetro flujo,
    //  la misma familia técnica sin dependencias de WoTG/Luminance.
    //
    //  ARMA DE PRUEBAS: 0 maná (la regla de la sesión).
    // ======================================================================

    /// <summary>EL ARMA (la mirada de Solyn).</summary>
    public class MiradaSolyn : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 800;               // el número de la referencia
            Item.DamageType = DamageClass.Magic;
            Item.width = 34; Item.height = 40;
            Item.useTime = 90; Item.useAnimation = 90;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;             // LA CANALIZACIÓN
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SolynCargaProj>();
            Item.shootSpeed = 1f;
            Item.mana = 0;                   // ARMA DE PRUEBAS: la regla de la sesión
            Item.noMelee = true;
            Item.ArmorPenetration = 500;     // la referencia (500)
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
        }

        public override bool CanUseItem(Player player)
        {
            int carga = ModContent.ProjectileType<SolynCargaProj>();
            int rayo = ModContent.ProjectileType<SolynRayoProj>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI &&
                    (p.type == carga || p.type == rayo))
                    return false;     // UNA mirada a la vez
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA CARGA NACE AL ALCANCE DEL RATÓN (dir·100 — la referencia).
            Vector2 dir = (Main.MouseWorld - player.Center).SafeNormalize(-Vector2.UnitY);
            Projectile.NewProjectile(source, player.Center + dir * 100f,
                Vector2.Zero, type, damage, knockback, player.whoAmI,
                dir.ToRotation(), 0f);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // ======================================================================
    //  SolynCargaProj — LA CARGA (0,75 s): el destello estelar que gira
    //  2 vueltas enteras mientras convoca los polvos convergentes.
    // ======================================================================
    public class SolynCargaProj : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/FlareEstrella4Puntas";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 45;          // 0,75 s
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (owner == null || !owner.active || owner.dead ||
                !owner.channel)
            {
                // EL CANAL SE ROMPIÓ: la carga muere SIN rayo.
                Projectile.Kill();
                return;
            }

            // LA CARGA SIGUE AL RATÓN (dir·100 — la referencia).
            Vector2 dir = (Main.MouseWorld - owner.Center).SafeNormalize(-Vector2.UnitY);
            Projectile.Center = owner.Center + dir * 100f;
            Projectile.rotation = dir.ToRotation();

            float r = 1f - Projectile.timeLeft / 45f;    // 0 → 1
            // LA CURVA DE LA ESCALA (la referencia: el pop elástico).
            float s = Utils.GetLerpValue(0f, 0.65f, r, true) *
                      Utils.GetLerpValue(1f, 0.9f, r, true);
            Projectile.scale = s * s;

            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                // EL POLVO CONVERGENTE (1/tick desde el círculo de 200 px,
                // v = 10, HotPink/Wheat — la referencia).
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2CircularEdge(200f, 200f),
                    Main.rand.NextBool() ? DustID.Electric : DustID.PortalBoltTrail,
                    Vector2.Zero, 0,
                    Main.rand.NextBool() ? Color.HotPink : Color.Wheat, 0.9f);
                Vector2 hacia = Projectile.Center - d.position;
                if (hacia.LengthSquared() > 1f) d.velocity = hacia / hacia.Length() * 10f;
                d.noGravity = true;
            }

            if (Projectile.timeLeft <= 1)
            {
                // === LA DESCARGA: el rayo nace (la referencia: al frame 2
                //     del rayo suena — aquí al nacer). ===
                if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item122.WithPitchOffset(-0.45f).WithVolumeScale(1.1f),
                        Projectile.Center);

                Projectile rayo = Projectile.NewProjectileDirect(
                    Projectile.GetSource_FromAI(), owner.Center + dir * 100f,
                    Vector2.Zero, ModContent.ProjectileType<SolynRayoProj>(),
                    Projectile.damage, 6f, Projectile.owner,
                    dir.ToRotation(), 0f);
                rayo.netUpdate = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;
            if (_flareTex == null)
                _flareTex = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/FlareEstrella4Puntas",
                    AssetRequestMode.ImmediateLoad);

            float r = 1f - Projectile.timeLeft / 45f;
            float s = Utils.GetLerpValue(0f, 0.65f, r, true) * Utils.GetLerpValue(1f, 0.9f, r, true);
            s *= s;
            // EL GIRO: SmoothStep^0,3 · 4π — DOS VUELTAS ENTERAS.
            float sm = r * r * (3f - 2f * r);
            float rot = MathF.Pow(Math.Max(sm, 0f), 0.3f) * MathHelper.TwoPi * 2f;

            Vector2 pos = Projectile.Center - Main.screenPosition;
            // LA CARGA PULSA blanca→dorado (el VLM del GIF: «carga
            // blanca/dorada pulsante»).
            Color c = Color.Lerp(Color.White, new Color(255, 196, 36), r);

            VFXCore.AbrirAdditive();
            try
            {
                Main.spriteBatch.Draw(_flareTex.Value, pos, null, c * 0.85f,
                    rot, _flareTex.Value.Size() * 0.5f,
                    new Vector2(0.55f * s + 0.1f), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(_flareTex.Value, pos, null, c * 0.5f,
                    rot + MathHelper.Pi / 4f, _flareTex.Value.Size() * 0.5f,
                    new Vector2(0.35f * s + 0.06f), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(VFXCore.GlowOrb, pos, null, Color.White * 0.7f,
                    0f, VFXCore.GlowOrb.Size() * 0.5f,
                    new Vector2(0.5f * s + 0.15f), SpriteEffects.None, 0f);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            finally { VFXCore.RestaurarLoteVanilla(); }
            return false;
        }

        private static Asset<Texture2D> _flareTex;
    }

    // ======================================================================
    //  SolynRayoProj — EL RAYO (la ráfaga persistente de ~1,9 s).
    // ======================================================================
    public class SolynRayoProj : ModProjectile
    {
        /// <summary>El alcance máximo (la referencia: 5.600 px).</summary>
        private const float LargoMax = 5600f;

        /// <summary>El crecimiento por actualización (+175).</summary>
        private const float Crecimiento = 175f;

        /// <summary>El latido (90 rad/s = 14,3 Hz; ±6 px).</summary>
        private const float Omega = 90f;

        private static Asset<Texture2D> _flareTex;
        private static Asset<Texture2D> _dendrito;

        // LA PALETA (la referencia exacta).
        private static readonly Color NucleoRosa = new(255, 45, 123);
        private static readonly Color EspinaDorada = new(255, 196, 36);
        private static readonly Color Fogonazo = new(255, 174, 147);
        private static readonly Color BloomMagenta = new(255, 10, 150);

        public override string Texture => "AethonMod/Content/Effects/FlareEstrella4Puntas";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 8000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 225;          // ~1,9 s
            Projectile.extraUpdates = 1;        // MaxUpdates 2 (la referencia)
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.aiStyle = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (owner == null || !owner.active)
            {
                Projectile.Kill();
                return;
            }

            // === LA DIRECCIÓN: barre tras el ratón (AngleLerp 0,15) ===
            float objetivo = (Main.MouseWorld - owner.Center).ToRotation();
            float actual = Projectile.ai[0];
            float delta = MathHelper.WrapAngle(objetivo - actual);
            float nuevo = actual + delta * 0.15f;
            if (Math.Abs(delta) > 0.0001f) Projectile.netUpdate = true;
            Projectile.ai[0] = nuevo;

            // === EL LARGO (crece +175/actualización hasta 5.600) ===
            Projectile.localAI[0] += Crecimiento;
            if (Projectile.localAI[0] > LargoMax) Projectile.localAI[0] = LargoMax;

            // LA POSICIÓN (el muzzle a dir·100 del dueño).
            Vector2 dir = nuevo.ToRotationVector2();
            Projectile.Center = owner.Center + dir * 100f;
            Projectile.rotation = nuevo;

            // === LA LUZ (la mejora propia del informe: 3 puntos) ===
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                Vector3 luz = new Vector3(0.9f, 0.25f, 0.55f);
                Vector2 fin = owner.Center + dir * Projectile.localAI[0];
                Lighting.AddLight(Projectile.Center, luz);
                Lighting.AddLight(Vector2.Lerp(Projectile.Center, fin, 0.5f), luz * 0.8f);
                Lighting.AddLight(fin, luz * 0.6f);

                // LOS POLVOS DEL BORDE (6/actualización en 0,9×semiancho a
                // mitad de tira, perpendicular 2-8 — la referencia).
                float semi = SemiAncho(0.5f);
                Vector2 borde = Vector2.Lerp(Projectile.Center, fin, 0.5f);
                Vector2 perp = new Vector2(-dir.Y, dir.X);
                for (int i = 0; i < 3; i++)
                {
                    int lado = i % 2 == 0 ? 1 : -1;
                    Dust d = Dust.NewDustPerfect(
                        borde + perp * (semi * 0.9f * lado),
                        Main.rand.NextBool() ? DustID.Electric : DustID.PortalBoltTrail,
                        perp * lado * Main.rand.NextFloat(2f, 8f), 0,
                        Main.rand.NextBool() ? Color.HotPink : Color.Wheat,
                        Main.rand.NextFloat(0.8f, 1.4f));
                    d.noGravity = true;
                }
            }
        }

        /// <summary>EL SEMIANCHO por posición (0..1) — la LaserWidthFunction
        /// de la referencia: base 96 + latido ±6·sen(90t) + bulto 32 al
        /// arranque + arranque redondo sqrt(1.001−x²) + el cierre de 8
        /// frames al final de la vida.</summary>
        private float SemiAncho(float x)
        {
            float t = Main.GlobalTimeWrappedHourly;
            float w = 96f;
            w += 6f * (float)Math.Sin(Omega * t);          // EL LATIDO (14,3 Hz)
            if (x < 0.09f) w += 32f * (1f - x / 0.09f);    // EL BULTO del arranque
            if (x < 0.055f) w *= (float)Math.Sqrt(1.001 - x * x);  // arranque redondo

            // EL CIERRE: los últimos 8 frames de la vida desinflan la banda.
            // EL CIERRE: los últimos 8 frames de la vida desinflan la banda
            // (el fade lineal de la referencia).
            return w * MathHelper.Clamp(Projectile.timeLeft / 8f, 0f, 1f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // LA COLISIÓN (la referencia): ancho = SemiAncho(0,25)·1,8,
            // largo 95 % — CheckAABBvLineCollision.
            Player owner = Main.player[Projectile.owner];
            if (owner == null || !owner.active) return false;
            float dir = Projectile.ai[0];
            Vector2 a = owner.Center + dir.ToRotationVector2() * 60f;
            Vector2 b = owner.Center + dir.ToRotationVector2() * (Projectile.localAI[0] * 0.95f);
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                a, b, SemiAncho(0.25f) * 1.8f, ref point);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;
            if (_flareTex == null)
                _flareTex = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/FlareEstrella4Puntas", AssetRequestMode.ImmediateLoad);
            if (_dendrito == null)
                _dendrito = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Effects/Procedural/RuidoDendritico",
                    AssetRequestMode.ImmediateLoad);

            Player owner = Main.player[Projectile.owner];
            if (owner == null || !owner.active) return false;

            Vector2 dir = Projectile.ai[0].ToRotationVector2();
            float largo = Projectile.localAI[0];
            Vector2 raiz = owner.Center - Main.screenPosition;
            Vector2 punta = raiz + dir * largo;

            // LA POLILÍNEA (24 nodos — la tira de la casa).
            int n = 24;
            Vector2[] pts = new Vector2[n];
            float[] semis = new float[n];
            for (int i = 0; i < n; i++)
            {
                float x = i / (float)(n - 1);
                pts[i] = raiz + dir * (largo * x);
                semis[i] = MathF.Max(SemiAncho(x) * 0.5f, 2f);
            }

            if (_dendrito.Value == null || _dendrito.Value.IsDisposed) return false;

            RayoStrip.AbrirLote();
            try
            {
                // === 1. LA TIRA DE BLOOM (×1,9 — magenta ×0,34) ===
                RayoStrip.FajaTexturadaEnLote(pts, ArraySemis(semis, 1.9f),
                    BloomMagenta * 0.34f, VFXCore.SoftGlow,
                    640f, 0.15f, Main.GlobalTimeWrappedHourly);

                // === 2. EL NÚCLEO (el RUIDO DENDRÍTICO fluyendo — los
                //     filamentos eléctricos de la referencia) ===
                RayoStrip.FajaTexturadaEnLote(pts, semis,
                    NucleoRosa * 0.85f, _dendrito.Value,
                    640f, 0.55f, Main.GlobalTimeWrappedHourly);

                // === 3. LA ESPINA DORADA (la fina del centro) ===
                RayoStrip.FajaTexturadaEnLote(pts, ArraySemis(semis, 0.3f),
                    EspinaDorada * 0.9f, _dendrito.Value,
                    320f, 0.95f, Main.GlobalTimeWrappedHourly);
            }
            finally { RayoStrip.CerrarLote(); }

            // === EL MUZZLE (3 flares + 2 glows melocotón) — fuera del
            //     lote de tiras, en el aditivo de la casa ===
            VFXCore.AbrirAdditive();
            try
            {
                Vector2 pos = Projectile.Center - Main.screenPosition;
                float t = Main.GlobalTimeWrappedHourly;
                float pulso = 0.45f * (1f + 0.2f * (float)Math.Cos(t * 85f));
                Color mc = Fogonazo;
                for (int k = 0; k < 3; k++)
                    Main.spriteBatch.Draw(_flareTex.Value, pos, null, mc * 0.7f,
                        t * (0.3f + k * 0.37f) + k * 0.8f, _flareTex.Value.Size() * 0.5f,
                        new Vector2(pulso * (1f - k * 0.18f)), SpriteEffects.None, 0f);
                for (int k = 0; k < 2; k++)
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, pos, null, mc * 0.5f,
                        -t * 0.25f + k * 0.9f, VFXCore.SoftGlow.Size() * 0.5f,
                        new Vector2(pulso * (0.9f - k * 0.15f)), SpriteEffects.None, 0f);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            finally { VFXCore.RestaurarLoteVanilla(); }
            return false;
        }

        /// <summary>Un array de semianchos escalados (el patrón ArraySemis
        /// del RC2 — aquí local).</summary>
        private static float[] ArraySemis(float[] src, float k)
        {
            float[] dst = new float[src.Length];
            for (int i = 0; i < src.Length; i++) dst[i] = src[i] * k;
            return dst;
        }
    }
}
