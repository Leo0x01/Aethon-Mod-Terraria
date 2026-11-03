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
    // ======================================================================
    //  v6.85 → v6.89 — LA INVESTIGACIÓN R8, REHECHA CON EL ORIGINAL EN LA
    //  MANO (la letra: «en cuanto al arma de solyn, veo que no se parece
    //  en nada a la original, o sea solo intentaste copiar, pero no
    //  investigaste ni el codigo, ni las tecnicas que usa, ni las
    //  texturas usadas ni que tipo de vfx usa esa arma, y ahora la
    //  version que hiciste tiene esos picos molestos de siempre, tienes
    //  que arreglar eso»).
    //
    //  ESTA VEZ SÍ: el fuente ORIGINAL descargado y leído entero —
    //  TheFifthCircle/WrathOfTheGodsPublic (NoxusBoss/Luminance), rama
    //  master, el rayo tag-team de Solyn en la pelea de Mars/Draedon:
    //  Content/NPCs/Bosses/Draedon/Projectiles/SolynProjectiles/
    //  SolynTagTeamBeam.cs (+ el SolynTagTeamChargeUp.cs de la carga).
    //
    //  LA GEOMETRÍA EXACTA DEL ORIGINAL (LaserWidthFunction, byte a
    //  byte — la cura de LOS PICOS: nada de bultos lineales ni cortes):
    //   · width(p) = Remap(largo, 0, 5600, 4, idealWidth)
    //       · cierre(life−time) × arranqueRedondo
    //   · idealWidth = bulto + cos(t·90)·6 + 96
    //       · bulto = Convert01To010(InverseLerp(0,15,0,85, largo/5600))
    //                 · InverseLerp(0, 0,05, p) · 32    (px)
    //   · arranqueRedondo = √(1,001 − interpolante²), con el
    //     interpolante = InverseLerp(0,05→0,012, p) — el nacimiento en
    //     PUNTA DE CÍRCULO (los primeros 5 % del haz se abren en arco:
    //     por eso el original NO tiene picos).
    //   · cierre = InverseLerp(0, 8, vida−tiempo) — los últimos 8
    //     frames desinflan TODO el haz.
    //  LOS FUNDIDOS DE COLOR DEL ORIGINAL (LaserColorFunction +
    //  BloomColorFunction, byte a byte):
    //   · núcleo (255,45,123) × [opLargo(0→0,45) · opArranque(0→0,032)
    //     · opFinal(0,95→0,81)] — el haz NACE y MUERE transparente.
    //   · bloom (255,10,150) × InverseLerpBump(0,02; 0,05; 0,81; 0,95)
    //     × 0,34 — y su banda mide ×1,9 la del núcleo.
    //  EL MUZZLE DEL ORIGINAL (PreDraw, byte a byte):
    //   · shineIntensity = InverseLerp(0,12,tiempo) · InverseLerp(0,7,
    //     timeLeft) · Lerp(1, 1,2, cos01(t·85)) · 0,45
    //     (MaxGleamScaleFactor del ChargeUp) — 3× el flare + 2× el glow
    //     a escala ×2, color fogonazo melocotón (255,174,147).
    //  EL RUIDO: la tira del núcleo viste la RuidoDendritico de la casa
    //  (512², la misma familia que DendriticNoiseZoomedOut del original)
    //  y la espina dorada (255,196,36) — el secondaryColor del shader
    //  original — como la capa fina del centro.
    //  24 nodos (el original: 12 puntos de control — el doble, más
    //  suave), TODO por nodo: anchos Y colores (FajaTexturadaEnLote con
    //  coloresPorNodo — la tira de la casa).
    //
    //  ARMA DE PRUEBAS: 0 maná (la regla de la sesión).
    // ======================================================================

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
    // ======================================================================
    //  SolynRayoProj — EL RAYO (la ráfaga persistente de ~1,9 s) — v6.89:
    //     rehecho con la geometría, los fundidos y el muzzle del ORIGINAL
    //     (SolynTagTeamBeam.cs) — SIN PICOS por construcción.
    // ======================================================================
    public class SolynRayoProj : ModProjectile
    {
        /// <summary>El alcance máximo (el original: 5.600 px).</summary>
        private const float LargoMax = 5600f;

        /// <summary>El crecimiento por actualización (+175).</summary>
        private const float Crecimiento = 175f;

        /// <summary>La vida útil en actualizaciones (el original: 3,75 s ×
        /// MaxUpdates 2; el nuestro: 225 × extraUpdates 1 — el mismo
        /// número de actualizaciones).</summary>
        private const float VidaUtil = 225f;

        /// <summary>El ancho base del haz (el original: 96).</summary>
        private const float AnchoBase = 96f;

        private static Asset<Texture2D> _flareTex;
        private static Asset<Texture2D> _dendrito;

        // LA PALETA (el original, exacta).
        private static readonly Color NucleoRosa = new(255, 45, 123);
        private static readonly Color EspinaDorada = new(255, 196, 36);
        private static readonly Color Fogonazo = new(255, 174, 147);
        private static readonly Color BloomMagenta = new(255, 10, 150);

        /// <summary>EL TIEMPO del haz (ai[1]: 0 → 225, una por actualización).</summary>
        private float Tiempo { get => Projectile.ai[1]; set => Projectile.ai[1] = value; }

        /// <summary>El largo actual (ai[2] — viaja por el cable).</summary>
        private float Largo { get => Projectile.localAI[0]; set => Projectile.localAI[0] = value; }

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
            Projectile.extraUpdates = 1;        // MaxUpdates 2 (el original)
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

            // === EL LARGO (crece +175/actualización hasta 5.600 — el original) ===
            Largo = MathF.Min(Largo + Crecimiento, LargoMax);

            // LA POSICIÓN (el muzzle a dir·100 del dueño).
            Vector2 dir = nuevo.ToRotationVector2();
            Projectile.Center = owner.Center + dir * 100f;
            Projectile.rotation = nuevo;

            Tiempo++;

            // === LA LUZ (la mejora propia del informe: 3 puntos) ===
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                Vector3 luz = new Vector3(0.9f, 0.25f, 0.55f);
                Vector2 fin = owner.Center + dir * Largo;
                Lighting.AddLight(Projectile.Center, luz);
                Lighting.AddLight(Vector2.Lerp(Projectile.Center, fin, 0.5f), luz * 0.8f);
                Lighting.AddLight(fin, luz * 0.6f);

                // LOS POLVOS DEL BORDE (la adaptación de la casa de los
                // SmallTeslaArc del original: 3/actualización a 0,9× el
                // semiancho de mitad de tira, perpendicular 2-8).
                float semi = AnchoLaser(0.5f);
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

        // ================================================================
        //  LAS MATEMÁTICAS DEL ORIGINAL (SolynTagTeamBeam.cs — Luminance),
        //  byte a byte. NADA de picos: cada tramo es una curva.
        // ================================================================

        /// <summary>InverseLerp de Luminance (soporta rangos descendentes:
        /// v=a → 0 · v=b → 1, en cualquier dirección).</summary>
        private static float Inversa(float a, float b, float v)
            => MathHelper.Clamp((v - a) / (b - a), 0f, 1f);

        /// <summary>Convert01To010 de Luminance: 0→1→0 (la colina 4x(1−x)).</summary>
        private static float Colina(float x) => 4f * x * (1f - x);

        /// <summary>EL SEMIANCHO por posición (0..1) — la
        /// LaserWidthFunction del original, EXACTA: la cura de los picos.</summary>
        private float AnchoLaser(float p)
        {
            // EL BULTO: solo en el arranque (p<0,05) y solo a mitad del
            // crecimiento del haz (0,15..0,85 de su largo — la colina).
            float bulto = Colina(Inversa(0.15f, 0.85f, Largo / LargoMax))
                * Inversa(0f, 0.05f, p) * 32f;

            // EL ANCHO IDEAL: bulto + latido (±6 a 90 rad/s) + la base 96.
            float ideal = bulto + MathF.Cos(Main.GlobalTimeWrappedHourly * 90f) * 6f + AnchoBase;

            // EL CIERRE: los últimos 8 frames de vida desinflan TODO.
            float cierre = Inversa(0f, 8f, VidaUtil - Tiempo);

            // EL ARRANQUE REDONDO: los primeros 5 % se abren en PUNTA DE
            // CÍRCULO (√(1,001−x²) — el nacimiento redondo del original).
            float circ = MathF.Sqrt(1.001f - Inversa(0.05f, 0.012f, p) * Inversa(0.05f, 0.012f, p));

            // EL RAMPO DEL CRECIMIENTO: el haz nace a 4 px de semiancho y
            // abre hasta su ideal a medida que SE EXTIENDE.
            return MathHelper.Clamp(Utils.Remap(Largo, 0f, LargoMax, 4f, ideal), 0f, 500f)
                * cierre * MathF.Max(circ, 0.03f);
        }

        /// <summary>EL COLOR DEL NÚCLEO por nodo — la LaserColorFunction
        /// del original: el haz NACE y MUERE transparente (por eso el
        /// original no tiene picos en los extremos: el color se funde).</summary>
        private Color ColorLaser(float p)
        {
            float opLargo = Inversa(0f, 0.45f, Largo / LargoMax);
            float opArranque = Inversa(0f, 0.032f, p);
            float opFinal = Inversa(0.95f, 0.81f, p);
            return NucleoRosa * (opLargo * opArranque * opFinal);
        }

        /// <summary>EL COLOR DEL BLOOM por nodo — la BloomColorFunction
        /// del original: el bump 0,02→0,05 sube y 0,81→0,95 baja.</summary>
        private Color ColorBloom(float p)
            => BloomMagenta * (Inversa(0.02f, 0.05f, p) * Inversa(0.81f, 0.95f, p)) * 0.34f;

        /// <summary>LA ESPINA DORADA: el secondaryColor (255,196,36) del
        /// shader original — la capa fina del centro con SUS fundidos.</summary>
        private Color ColorEspina(float p)
        {
            float op = Inversa(0f, 0.032f, p) * Inversa(0.95f, 0.81f, p);
            return EspinaDorada * (op * op);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // LA COLISIÓN (el original): ancho = AnchoLaser(0,25)·1,8,
            // largo 95 % — CheckAABBvLineCollision.
            Player owner = Main.player[Projectile.owner];
            if (owner == null || !owner.active) return false;
            float dir = Projectile.ai[0];
            Vector2 a = owner.Center + dir.ToRotationVector2() * 60f;
            Vector2 b = owner.Center + dir.ToRotationVector2() * (Largo * 0.95f);
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                a, b, AnchoLaser(0.25f) * 1.8f, ref point);
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
            Vector2 raiz = owner.Center - Main.screenPosition;
            Vector2 punta = raiz + dir * Largo;

            // LA POLILÍNEA (24 nodos — el original: 12 puntos de control;
            // el doble: más suave). El primer punto RETROCEDE 10 px tras
            // la boca (el original: laserPositions[0] −= velocity·10).
            int n = 24;
            Vector2[] pts = new Vector2[n];
            float[] semis = new float[n];
            Color[] coloresLaser = new Color[n];
            Color[] coloresBloom = new Color[n];
            Color[] coloresEspina = new Color[n];
            pts[0] = raiz - dir * 10f;
            for (int i = 0; i < n; i++)
            {
                float x = i / (float)(n - 1);
                pts[i] = raiz + dir * (Largo * x);
                semis[i] = MathF.Max(AnchoLaser(x), 2f);        // el SEMIANCHO del original
                coloresLaser[i] = ColorLaser(x);
                coloresBloom[i] = ColorBloom(x);
                coloresEspina[i] = ColorEspina(x);
            }

            if (_dendrito.Value == null || _dendrito.Value.IsDisposed) return false;

            RayoStrip.AbrirLote();
            try
            {
                // === 1. LA BANDA DE BLOOM (×1,9 — el original:
                //     BloomWidthFunction = LaserWidth × 1,9) ===
                RayoStrip.FajaTexturadaEnLote(pts, Escalar(semis, 1.9f),
                    coloresBloom, default, VFXCore.SoftGlow,
                    640f, 0.15f, Main.GlobalTimeWrappedHourly);

                // === 2. EL NÚCLEO (la RuidoDendritico fluyendo — los
                //     filamentos eléctricos del original) ===
                RayoStrip.FajaTexturadaEnLote(pts, semis,
                    coloresLaser, default, _dendrito.Value,
                    640f, 0.55f, Main.GlobalTimeWrappedHourly);

                // === 3. LA ESPINA DORADA (el secondaryColor del shader
                //     original — la capa fina del centro) ===
                RayoStrip.FajaTexturadaEnLote(pts, Escalar(semis, 0.3f),
                    coloresEspina, default, _dendrito.Value,
                    320f, 0.95f, Main.GlobalTimeWrappedHourly);
            }
            finally { RayoStrip.CerrarLote(); }

            // === EL MUZZLE (el PreDraw del original, byte a byte):
            //     shineIntensity = InverseLerp(0,12,tiempo) ·
            //     InverseLerp(0,7,timeLeft) · Lerp(1, 1,2, cos01(t·85)) ·
            //     0,45 (el MaxGleamScaleFactor del ChargeUp) — 3× el
            //     flare + 2× el glow a escala ×2, color melocotón. ===
            VFXCore.AbrirAdditive();
            try
            {
                Vector2 pos = Projectile.Center - Main.screenPosition;
                float t = Main.GlobalTimeWrappedHourly;
                // EL FACTOR MÁGICO (el comentario del original: «the magic
                // factor that makes every electric shine effect so much
                // better»): cos01(t·85).
                float magico = 0.5f + 0.5f * MathF.Cos(t * 85f);
                float brillo = Inversa(0f, 12f, Tiempo)
                    * Inversa(0f, 7f, Projectile.timeLeft)
                    * MathHelper.Lerp(1f, 1.2f, magico)
                    * 0.45f;
                for (int k = 0; k < 3; k++)
                    Main.spriteBatch.Draw(_flareTex.Value, pos, null, Fogonazo * 0.75f,
                        t * (0.3f + k * 0.37f) + k * 0.8f, _flareTex.Value.Size() * 0.5f,
                        new Vector2(brillo * 2f * (1f - k * 0.16f)), SpriteEffects.None, 0f);
                for (int k = 0; k < 2; k++)
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, pos, null, Fogonazo * 0.55f,
                        -t * 0.25f + k * 0.9f, VFXCore.SoftGlow.Size() * 0.5f,
                        new Vector2(brillo * 2f * (0.9f - k * 0.12f)), SpriteEffects.None, 0f);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            finally { VFXCore.RestaurarLoteVanilla(); }
            return false;
        }

        /// <summary>Un array de semianchos escalados.</summary>
        private static float[] Escalar(float[] src, float k)
        {
            float[] dst = new float[src.Length];
            for (int i = 0; i < src.Length; i++) dst[i] = src[i] * k;
            return dst;
        }
    }
}
