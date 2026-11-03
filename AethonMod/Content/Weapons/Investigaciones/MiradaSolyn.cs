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

namespace AethonMod.Content.Weapons.Investigaciones
{
    // ======================================================================
    // ======================================================================
    //  v6.91 — EL PORT EXACTO (la letra: «el arma tiene picos, recuerda
    //  que es lo que sucede con los picos es algo historico que nunca
    //  eres capaz de reparar bien, si no sabes puedes copiar el codigo
    //  exacto con texturas y todo, ten en cuenta que es investigacion,
    //  sobre la textura a usar puedes usar cualquiera de las textura
    //  ruido que tenemos, para eso estan ahi, solo asegurate de que
    //  esten bien ubicadas»).
    //
    //  EL VEREDICTO: cuatro curas creativas (v6.89, v6.90) y los picos
    //  SIGUIERON — porque la suavidad del original NO vive en la
    //  geometría ni en las texturas: VIVE EN EL PIXEL SHADER, evaluado
    //  por PÍXEL. Esta versión es la orden del usuario ejecutada al
    //  pie de la letra: COPIA EXACTA del original —
    //  TheFifthCircle/WrathOfTheGodsPublic, el rayo tag-team de Solyn:
    //    · Content/NPCs/Bosses/Draedon/Projectiles/SolynProjectiles/
    //      SolynTagTeamBeam.cs (+ SolynTagTeamChargeUp.cs de la carga).
    //    · Assets/AutoloadedEffects/Shaders/Primitives/
    //      SolynTagTeamBeamShader.fx y PrimitiveBloomShader.fx (LOS DOS
    //      PIXEL SHADERS, portados a evaluación por VÉRTICE en CPU — la
    //      casa no carga .fxc propios, y la matemática es la misma).
    //    · Las texturas EXACTAS copiadas a Content/Effects/Procedural/
    //      Investigacion/: DendriticNoiseZoomedOut.png (el ruido del
    //      shader — idéntico byte a byte en NoxusBoss y Luminance),
    //      BloomCircleSmall.png y ShineFlare.png (el glow y el flare
    //      del muzzle — el MiscTexturesRegistry.ShineFlareTexture de
    //      Luminance). md5 verificadas contra los originales.
    //
    //  POR QUÉ ESTO NO PUEDE TENER PICOS (la fórmula, no una textura):
    //    · EL FILO DE BORDE — el shader del rayo termina con
    //      «* smoothstep(0.5, 0.4, d)» (SolynTagTeamBeamShader.fx:58):
    //      en el borde de la banda (d=0.5) el factor es CERO EXACTO ⇒
    //      rgb=0 ⇒ con blending de suma el borde NUNCA aporta. En el
    //      bloom: «pow(4y(1−y), 3) · color · 3.5» (:43-44) — 0³=0 en
    //      y=0/1, EXACTO también. Ninguna aproximación con textura
    //      radial/cúpula puede GARANTIZAR eso (el borde de un png nunca
    //      es matemáticamente cero tras el filtro bilineal) — por eso
    //      el histórico de picos: cada cura ponía el fundido en el
    //      dominio equivocado.
    //    · EL UMBRAL DEL RUIDO — smoothstep(0.3, 0.45, ruido) (:50):
    //      el 40 % oscuro del ruido NO se pinta (el «ruido crudo» de la
    //      autopsia era esto: el port v6.90 pintaba el ruido entero).
    //    · LA DIAGONAL — el ruido se muestrea en
    //      frac(coords·2 + (0, x·5) − 3t) (:47): cizallada diagonal y
    //      fluyendo a 3 u/s (la autopsia: «el ruido crudo y lento»).
    //    · LA GEOMETRÍA — 12 puntos de control Lerp(boca→punta) con el
    //      primero retraído 10 px (SolynTagTeamBeam.cs:182-183),
    //      re-muestreados por Luminance a 77 vértices (rayo) / 48
    //      (bloom) con CatmullRom — los anchos Y colores por VÉRTICE y
    //      el semiancho como offset (AssignVerticesRectangleTrail,
    //      Luminance decompilada).
    //
    //  EL MOTOR: una REJILLA de quads (nodos a lo largo × filas a lo
    //  ancho) con el shader evaluado EN CADA VÉRTICE — el nuevo
    //  RayoStrip.TrianguloColoresEnLote (color completo por vértice,
    //  BasicEffect + el lote soft-add de la casa). Las filas se
    //  concentran DONDE la fórmula se curva (el filo del borde, el
    //  brillo interior, el recolor dorado) — véase FilasHaz.
    //
    //  DESVIACIONES DOCUMENTADAS (las únicas):
    //    1. El blend: soft-add de la casa (dst += rgb·(1−dst)) en vez
    //       del estado-heredado del original — la lección v6.50.26 del
    //       «brillo que se rompe en los bordes»; rgb=0 aporta 0 igual.
    //    2. El color del VÉRTICE es byte: el bloom ×3.5 sin saturate
    //       del original satura aquí a 255 (solo el rojo del núcleo
    //       del bloom lo toca — y ahí el rayo ya pinta su núcleo
    //       blanco encima).
    //    3. Por vértice, no por píxel: la rejilla (25 filas × 77 nodos
    //       el rayo, 13 × 48 el bloom) aproxima la evaluación por píxel
    //       del original — con el filo del borde muestreado a 4 puntos
    //       de la smoothstep.
    //    4. La carga conserva SUS polvos de la casa (comportamiento,
    //       no render) y el aim del arma (no hay NPC Solyn que seguir).
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
    //  SolynCargaProj — LA CARGA (0,75 s): los destellos convergentes
    //  girando 2 vueltas enteras mientras convoca los polvos.
    //  v6.91: el PreDraw es EL PORT EXACTO del original
    //  (SolynTagTeamChargeUp.cs:131-145): 3 flares GIRANDO con la carga
    //  + 2 glows fijos, escala scale·MaxGleamScaleFactor·2, color
    //  melocotón con A=0 (lote aditivo — el original lo dibuja así).
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
            Projectile.timeLeft = 45;          // 0,75 s (Lifetime del original)
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

            // LA ROTACIÓN (ChargeUp.cs:89 EXACTA): EL GIRO de los
            // destellos — Pow(SmoothStep(0,1,r), 0.3)·4π, DOS VUELTAS
            // ENTERAS durante la carga (los flares del original giran con
            // ESTA rotación, no con el aim).
            float r = 1f - Projectile.timeLeft / 45f;
            float sm = r * r * (3f - 2f * r);
            Projectile.rotation = MathF.Pow(Math.Max(sm, 0f), 0.3f) * MathHelper.TwoPi * 2f;

            // LA CURVA DE LA ESCALA (ChargeUp.cs:88 EXACTA):
            // IL(0, 0.65, r)² · IL(1, 0.9, r) — el CUADRADO es SOLO de la
            // subida (la v6.89 cuadraba el producto entero: el apagado del
            // final también se cuadraba y el pop moría más brusco).
            float subida = Utils.GetLerpValue(0f, 0.65f, r, true);
            float apagado = Utils.GetLerpValue(1f, 0.9f, r, true);
            Projectile.scale = subida * subida * apagado;

            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                // EL POLVO CONVERGENTE (la referencia: 1/tick desde el
                // círculo de 200 px, v = 10, Wheat/HotPink).
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(200f, 200f),
                    Main.rand.NextBool() ? DustID.Electric : DustID.PortalBoltTrail,
                    Vector2.Zero, 0,
                    Main.rand.NextBool() ? Color.Wheat : Color.HotPink, 0.9f);
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
            if (!RecursosSolyn.AsegurarCargado() ||
                RecursosSolyn.Flare == null || RecursosSolyn.Glow == null) return false;

            // EL PREDRAW DEL ORIGINAL (ChargeUp.cs:131-145, byte a byte):
            // shineIntensity = scale · MaxGleamScaleFactor (0.45 — :45).
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float brillo = Projectile.scale * 0.45f;
            Color fogonazo = new Color(255, 174, 147, 0);   // LensFlareColor with { A = 0 }
            Texture2D flare = RecursosSolyn.Flare;
            Texture2D glow = RecursosSolyn.Glow;

            VFXCore.AbrirAdditive();
            try
            {
                // 3 flares GIRANDO con la carga (:139-140)…
                for (int i = 0; i < 3; i++)
                    Main.spriteBatch.Draw(flare, pos, null, fogonazo,
                        Projectile.rotation, flare.Size() * 0.5f,
                        brillo * 2f, SpriteEffects.None, 0f);
                // …y 2 glows FIJOS (:141-142).
                for (int i = 0; i < 2; i++)
                    Main.spriteBatch.Draw(glow, pos, null, fogonazo,
                        0f, glow.Size() * 0.5f,
                        brillo * 2f, SpriteEffects.None, 0f);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            finally { VFXCore.RestaurarLoteVanilla(); }
            return false;
        }
    }

    // ======================================================================
    // ======================================================================
    //  SolynRayoProj — EL RAYO (~1,9 s) — v6.91: EL PORT EXACTO del
    //  render de SolynTagTeamBeam.cs: la MISMA geometría, el MISMO
    //  muestreo de ruido y la MISMA curva de color — sus dos pixel
    //  shaders evaluados por vértice en CPU. SIN PICOS por FÓRMULA.
    // ======================================================================
    public class SolynRayoProj : ModProjectile
    {
        /// <summary>El alcance máximo (el original: 5.600 px — :50).</summary>
        private const float LargoMax = 5600f;

        /// <summary>El crecimiento por actualización (+175 — :103).</summary>
        private const float Crecimiento = 175f;

        /// <summary>La vida útil en actualizaciones (el original: 3,75 s ×
        /// MaxUpdates 2 = 225; el nuestro: 225 × extraUpdates 1 — el mismo
        /// número de actualizaciones).</summary>
        private const float VidaUtil = 225f;

        /// <summary>El ancho base del haz (el original: Projectile.width
        /// = 96 — :71; el semiancho de la banda).</summary>
        private const float AnchoBase = 96f;

        /// <summary>EL TIEMPO del haz (ai[1]: 0 → 225, una por actualización).</summary>
        private float Tiempo { get => Projectile.ai[1]; set => Projectile.ai[1] = value; }

        /// <summary>El largo actual (localAI[0] — viaja por el cable).</summary>
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
            Projectile.extraUpdates = 1;        // MaxUpdates 2 (el original :79)
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.aiStyle = -1;
            Projectile.hide = true;             // el original se esconde (:81)…
        }

        // …y dibuja SOBRE los jugadores (DrawBehind → overPlayers — :214-217).
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles,
            List<int> behindNPCs, List<int> behindProjectiles,
            List<int> overPlayers, List<int> overWiresUI)
            => overPlayers.Add(index);

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (owner == null || !owner.active)
            {
                Projectile.Kill();
                return;
            }

            // === LA DIRECCIÓN: barre tras el ratón (AngleLerp — el original
            //     usa TagTeamBeamMouseAimSpeed·0.01; aquí 0.15) ===
            float objetivo = (Main.MouseWorld - owner.Center).ToRotation();
            float actual = Projectile.ai[0];
            float delta = MathHelper.WrapAngle(objetivo - actual);
            float nuevo = actual + delta * 0.15f;
            if (Math.Abs(delta) > 0.0001f) Projectile.netUpdate = true;
            Projectile.ai[0] = nuevo;

            // === EL LARGO (crece +175/actualización hasta 5.600 — :103) ===
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
                // SmallTeslaArc del original :134-150: chispas en el filo).
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
        //  LAS MATEMÁTICAS DEL ORIGINAL (SolynTagTeamBeam.cs — byte a
        //  byte; v6.91: SIN el Clamp/Max extra de la v6.90, que el
        //  original no tiene y sus valores jamás necesitan).
        // ================================================================

        /// <summary>InverseLerp de Luminance (soporta rangos descendentes:
        /// v=a → 0 · v=b → 1, en cualquier dirección).</summary>
        private static float Inversa(float a, float b, float v)
            => MathHelper.Clamp((v - a) / (b - a), 0f, 1f);

        /// <summary>Convert01To010 de Luminance (decompilada, línea 1328):
        /// sin(π·saturate(x)) — la colina 0→1→0 REAL.</summary>
        private static float Colina(float x) => MathF.Sin(MathF.PI * MathHelper.Clamp(x, 0f, 1f));

        /// <summary>EL SEMIANCHO por posición (0..1) — la
        /// LaserWidthFunction del original (:152-162), EXACTA.</summary>
        private float AnchoLaser(float p)
        {
            // EL BULTO del arranque (:154): la colina del crecimiento × la
            // rampa del primer 5 % × 32.
            float bulto = Colina(Inversa(0.15f, 0.85f, Largo / LargoMax))
                * Inversa(0f, 0.05f, p) * 32f;

            // EL ANCHO IDEAL (:155): bulto + el latido ±6 a 90 rad/s + base.
            float ideal = bulto + MathF.Cos(Main.GlobalTimeWrappedHourly * 90f) * 6f + AnchoBase;

            // EL CIERRE (:156): los últimos 8 frames de vida desinflan TODO.
            float cierre = Inversa(0f, 8f, VidaUtil - Tiempo);

            // EL ARRANQUE REDONDO (:158-159): los primeros 5 % se abren en
            // PUNTA DE CÍRCULO — √(1,001 − x²).
            float arranque = Inversa(0.05f, 0.012f, p);
            arranque = MathF.Sqrt(1.001f - arranque * arranque);

            // EL RAMPO DEL CRECIMIENTO (:161): nace a 4 px de semiancho.
            return Utils.Remap(Largo, 0f, LargoMax, 4f, ideal) * cierre * arranque;
        }

        /// <summary>EL COLOR DEL NODO del rayo — la LaserColorFunction del
        /// original (:166-174) EXACTA: nace (x&lt;0,032) y muere (x&gt;0,81)
        /// transparente. El byte-quantizado del Color×float ES el del
        /// original (su vértice también es byte).</summary>
        private Color ColorLaser(float p)
        {
            float opLargo = Inversa(0f, 0.45f, Largo / LargoMax);   // :168
            float opArranque = Inversa(0f, 0.032f, p);              // :169
            float opFinal = Inversa(0.95f, 0.81f, p);               // :170
            return new Color(255, 45, 123) * (opLargo * opArranque * opFinal);  // :172-173
        }

        /// <summary>EL COLOR DEL NODO del bloom — la BloomColorFunction
        /// (:176) EXACTA: el bump 0,02→0,05 sube y 0,95→0,81 BAJA (la
        /// InverseLerpBump de Luminance: IL(a,b,x)·IL(d,c,x)) — la punta
        /// se APAGA, nunca se corta en seco.</summary>
        private Color ColorBloom(float p)
            => new Color(255, 10, 150)
             * (Inversa(0.02f, 0.05f, p) * Inversa(0.95f, 0.81f, p)) * 0.34f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // LA COLISIÓN (el original :219-226): ancho = AnchoLaser(0,25)·1,8,
            // largo 95 % — CheckAABBvLineCollision.
            Player owner = Main.player[Projectile.owner];
            if (owner == null || !owner.active) return false;
            float dir = Projectile.ai[0];
            Vector2 a = owner.Center + dir.ToRotationVector2() * 60f;
            Vector2 b = owner.Center + dir.ToRotationVector2() * (Largo * 0.95f);
            float punto = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                a, b, AnchoLaser(0.25f) * 1.8f, ref punto);
        }

        // ================================================================
        //  EL RENDER — LOS DOS PIXEL SHADERS DEL ORIGINAL, POR VÉRTICE
        // ================================================================

        /// <summary>El contrato del shader portado: el color de salida en
        /// el punto (x a lo largo, y a lo ancho) para el color del nodo.</summary>
        private delegate Color ShaderPorVertice(float x, float y, Color colorNodo);

        private static readonly ShaderPorVertice ShaderHaz = ColorHaz;
        private static readonly ShaderPorVertice ShaderBloom = ColorBloomV;

        /// <summary>EL PIXEL SHADER DEL RAYO — SolynTagTeamBeamShader.fx:39-58,
        /// evaluado por vértice en CPU. Cada línea cita su :línea del .fx.</summary>
        private static Color ColorHaz(float x, float y, Color colorNodo)
        {
            // globalTime: lo siembra ManagedShader.Apply con
            // Main.GlobalTimeWrappedHourly (Luminance decompilada).
            float t = Main.GlobalTimeWrappedHourly;

            // :47 — EL RUIDO DIAGONAL: frac(coords·2 + (0, x·5) − t·3) —
            // 2 repeticiones a lo ancho, cizalladura diagonal x·5 y flujo
            // de 3 u/s. tex2D = bilineal con envoltura (LinearWrap :195).
            float ruido = RecursosSolyn.Muestrear(
                Frac(x * 2f - t * 3f),
                Frac(y * 2f + x * 5f - t * 3f));

            // :50 — EL UMBRAL SUAVE: el 40 % oscuro del ruido NO se pinta.
            float opacidad = Smoothstep(0.3f, 0.45f, ruido);

            // :52-53 — LA DISTANCIA AL EJE y EL BRILLO INTERIOR (el núcleo
            // blanco): d→0 ⇒ lerp(0.01,0.98,ruido)/d → +∞ ⇒ saturate = 1
            // (así lo resuelve el HLSL; el guard lo replica).
            float d = MathF.Abs(y - 0.5f);
            float razon = d <= 0.0001f ? 1f : Saturar((0.01f + 0.97f * ruido) / d);
            float brilloInterior = razon * Smoothstep(0.25f, 0.12f, d);

            // :54 — EL FOGONAZO MELOCOTÓN del arranque (lensFlareColor).
            Vector4 arranque = RecursosSolyn.LensFlareV4 * Smoothstep(0.11f, 0.01f, x);

            // :56 — EL RECOLOR DORADO del centro hacia secondaryColor — el
            // lerp del original EXTRAPOLA (el factor llega a 1,1: sin
            // clamp, como el HLSL lerp).
            float mezcla = Smoothstep(0.3f, 0.18f, d) + ruido * 0.1f;
            Vector4 color = Vector4.Lerp(colorNodo.ToVector4(), RecursosSolyn.SecundariaV4, mezcla);

            // :58 — LA SUMA SATURADA × EL FILO DE BORDE:
            // smoothstep(0.5, 0.4, d) es CERO EXACTO en d=0,5 (el BORDE de
            // la banda) ⇒ rgb=0 ⇒ con blending de suma el borde NUNCA
            // aporta. ESTA es la cura del histórico de picos: el fundido
            // vive en la FÓRMULA, no en un png.
            Vector4 suma = color * opacidad + new Vector4(brilloInterior) + arranque;
            return AColor(Saturar4(suma) * Smoothstep(0.5f, 0.4f, d));
        }

        /// <summary>EL PIXEL SHADER DEL BLOOM — PrimitiveBloomShader.fx:35-45:
        /// la cúpula pow(4y(1−y), 3) · color · 3.5 — en y=0/1 la potencia de
        /// 0 es 0 EXACTO (QuadraticBump :30-33). SIN saturate, como el
        /// original — el techo de 255 del vértice es la desviación nº 2
        /// del encabezado (solo el rojo del núcleo del bloom lo toca).</summary>
        private static Color ColorBloomV(float x, float y, Color colorNodo)
        {
            float bulto = y * (4f - y * 4f);                 // QuadraticBump
            float k = bulto * bulto * bulto * 3.5f;          // pow³ · 3.5 (:44)
            return AColor(colorNodo.ToVector4() * k);
        }

        /// <summary>El Vector4 del shader → el Color byte del vértice (el
        /// alfa no pinta: el lote soft-add de la casa suma el rgb).</summary>
        private static Color AColor(Vector4 v)
            => new Color(
                (byte)Math.Clamp((int)(v.X * 255f + 0.5f), 0, 255),
                (byte)Math.Clamp((int)(v.Y * 255f + 0.5f), 0, 255),
                (byte)Math.Clamp((int)(v.Z * 255f + 0.5f), 0, 255), 255);

        /// <summary>saturate de HLSL — componente a componente.</summary>
        private static Vector4 Saturar4(Vector4 v)
            => new Vector4(Saturar(v.X), Saturar(v.Y), Saturar(v.Z), Saturar(v.W));

        private static float Saturar(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

        /// <summary>EL smoothstep de HLSL (soporta bordes invertidos — el
        /// original lo usa así: smoothstep(0.5, 0.4, d), etc.).</summary>
        private static float Smoothstep(float a, float b, float x)
        {
            float t = Saturar((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        /// <summary>frac de HLSL (negativos incluidos: x−floor(x) ∈ [0,1)).</summary>
        private static float Frac(float x) => x - MathF.Floor(x);

        // ================================================================
        //  LA REJILLA — la geometría del original llevada a vértices
        // ================================================================

        // LOS NODOS A LO LARGO (los pozos: 77 y 48 caben de sobra).
        private const int MaxNodos = 80;
        private static readonly Vector2[] _nodoPos = new Vector2[MaxNodos];
        private static readonly float[] _nodoSemi = new float[MaxNodos];
        private static readonly float[] _nodoRatio = new float[MaxNodos];
        private static readonly Vector2[] _nodoPerp = new Vector2[MaxNodos];
        private static readonly Color[] _nodoColor = new Color[MaxNodos];

        // LAS FILAS A LO ANCHO (la coords.y corregida 0..1 del shader): el
        // pixel shader del original evalúa por PÍXEL; la rejilla lo
        // aproxima con vértices DONDE la fórmula se curva —
        //   · y 0→0.10: EL FILO DEL BORDE (d 0.5→0.4, la smoothstep letal);
        //   · y 0.17→0.30: el recolor dorado (d 0.18→0.3) y el arranque
        //     del brillo interior (d 0.12→0.25);
        //   · el centro: plano, ralo.
        // Los EXTREMOS (y=0/1) son filas de SIEMPRE: ahí la fórmula da
        // rgb=0 EXACTO — el borde del haz es matemáticamente invisible.
        private static readonly float[] FilasHaz =
        {
            0f, 0.04f, 0.08f, 0.10f,
            0.17f, 0.22f, 0.26f, 0.30f,
            0.34f, 0.38f, 0.42f, 0.46f, 0.50f,
            0.54f, 0.58f, 0.62f, 0.66f, 0.70f, 0.74f, 0.78f, 0.83f,
            0.90f, 0.92f, 0.96f, 1f
        };

        // LAS FILAS DEL BLOOM: la cúpula pow(4y(1−y), 3) sube rápido del
        // borde y es plana en el centro.
        private static readonly float[] FilasBloom =
        {
            0f, 0.05f, 0.10f, 0.20f, 0.30f, 0.40f, 0.50f,
            0.60f, 0.70f, 0.80f, 0.90f, 0.95f, 1f
        };

        /// <summary>LA RESAMBLA EXACTA de AssignPointsRectangleTrail
        /// (Luminance decompilada): posición = lerp(cp[k], cp[k+1],
        /// frac((j/puntosCreados)·(cp−1)))… y después el PRIMERO y el
        /// ÚLTIMO añadidos tal cual (+2 vértices — por eso el ratio del
        /// vértice es i/(puntosCreados+1), que corre POR DELANTE de la
        /// fracción geométrica: exactamente así en el original). El
        /// semiancho es el OFFSET (±perp·semiancho) y la coords.y
        /// corregida del shader es nuestra fila y — la correspondencia
        /// 1:1 del arreglo de distorsión del shader (:45).</summary>
        private int LlenarNodos(Vector2[] cp, int puntosCreados, float multiplicadorAncho)
        {
            int n = puntosCreados + 2;
            int ultimo = cp.Length - 1;
            for (int i = 1; i <= puntosCreados; i++)
            {
                float s = ((i - 1) / (float)puntosCreados) * ultimo;
                int k = (int)s;
                if (k >= ultimo) k = ultimo - 1;    // guard de redondeo (jamás dispara)
                _nodoPos[i] = Vector2.Lerp(cp[k], cp[k + 1], s - k);
            }
            _nodoPos[0] = cp[0];
            _nodoPos[n - 1] = cp[ultimo];

            for (int i = 0; i < n; i++)
            {
                float ratio = i / (float)(n - 1);            // i/(puntosCreados+1)
                _nodoRatio[i] = ratio;
                _nodoSemi[i] = AnchoLaser(ratio) * multiplicadorAncho;

                // LA DIRECCIÓN por nodo (AssignVerticesRectangleTrail: la
                // del segmento que empieza; el último, la del que termina).
                Vector2 d = i < n - 1
                    ? _nodoPos[i + 1] - _nodoPos[i]
                    : _nodoPos[i] - _nodoPos[i - 1];
                d = d.SafeNormalize(Vector2.Zero);
                _nodoPerp[i] = new Vector2(-d.Y, d.X);
            }
            return n;
        }

        /// <summary>EMITE LA REJILLA: por cada segmento×fila un quad (2
        /// triángulos) con el color del SHADER en cada esquina — el
        /// rgb=0 EXACTO de las filas del borde viaja tal cual al
        /// vértice. Los quads degenerados (el nodo duplicado del
        /// original) y los muertos (la banda del cierre final) se
        /// saltan.</summary>
        private static void EmitirRejilla(int n, float[] filas, ShaderPorVertice shader)
        {
            for (int i = 0; i < n - 1; i++)
            {
                if ((_nodoPos[i + 1] - _nodoPos[i]).LengthSquared() < 0.0001f)
                    continue;                              // el nodo duplicado
                float s0 = _nodoSemi[i], s1 = _nodoSemi[i + 1];
                if (s0 <= 0.05f && s1 <= 0.05f) continue;  // la banda muerta
                float x0 = _nodoRatio[i], x1 = _nodoRatio[i + 1];
                Vector2 p0 = _nodoPos[i], p1 = _nodoPos[i + 1];
                Vector2 n0 = _nodoPerp[i], n1 = _nodoPerp[i + 1];
                Color cN0 = _nodoColor[i], cN1 = _nodoColor[i + 1];

                for (int r = 0; r < filas.Length - 1; r++)
                {
                    float y0 = filas[r], y1 = filas[r + 1];
                    // EL OFFSET: perp·semiancho·(2y−1) — la banda
                    // ±semiancho del original, y = la coords.y corregida.
                    Vector2 a0 = p0 + n0 * (s0 * (y0 * 2f - 1f));
                    Vector2 a1 = p0 + n0 * (s0 * (y1 * 2f - 1f));
                    Vector2 b0 = p1 + n1 * (s1 * (y0 * 2f - 1f));
                    Vector2 b1 = p1 + n1 * (s1 * (y1 * 2f - 1f));
                    Color ca0 = shader(x0, y0, cN0);
                    Color ca1 = shader(x0, y1, cN0);
                    Color cb0 = shader(x1, y0, cN1);
                    Color cb1 = shader(x1, y1, cN1);
                    if ((ca0.R | ca0.G | ca0.B) == 0 && (ca1.R | ca1.G | ca1.B) == 0 &&
                        (cb0.R | cb0.G | cb0.B) == 0 && (cb1.R | cb1.G | cb1.B) == 0)
                        continue;                          // todo el quad es borde/muerto
                    RayoStrip.TrianguloColoresEnLote(a0, a1, b1, ca0, ca1, cb1);
                    RayoStrip.TrianguloColoresEnLote(a0, b1, b0, ca0, cb1, cb0);
                }
            }
        }

        /// <summary>LA CAPA DE BLOOM — el original la pinta PRIMERO
        /// (:185-189): RenderTrail(…, 46) ⇒ 48 nodos, ancho ×1.9
        /// (BloomWidthFunction :164), el shader de la cúpula.</summary>
        private void DibujarBloom(Vector2[] cp)
        {
            int n = LlenarNodos(cp, 46, 1.9f);
            for (int i = 0; i < n; i++)
                _nodoColor[i] = ColorBloom(_nodoRatio[i]);
            EmitirRejilla(n, FilasBloom, ShaderBloom);
        }

        /// <summary>LA CAPA DEL RAYO — el original la pinta DESPUÉS
        /// (:191-198): RenderTrail(…, 75) ⇒ 77 nodos, el shader del
        /// ruido diagonal con su umbral, su recolor y su filo letal.</summary>
        private void DibujarHaz(Vector2[] cp)
        {
            int n = LlenarNodos(cp, 75, 1f);
            for (int i = 0; i < n; i++)
                _nodoColor[i] = ColorLaser(_nodoRatio[i]);
            EmitirRejilla(n, FilasHaz, ShaderHaz);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;
            if (!RecursosSolyn.AsegurarCargado() ||
                RecursosSolyn.Flare == null || RecursosSolyn.Glow == null) return false;

            Player owner = Main.player[Projectile.owner];
            if (owner == null || !owner.active) return false;

            // === LA GEOMETRÍA DEL ORIGINAL (:182-183) — 12 puntos de
            //     control Lerp(boca→punta) y el PRIMERO retraído 10 px
            //     TRAS la boca (laserPositions[0] −= velocity·10): el
            //     arranque redondo del haz TAPA el muzzle. ===
            Vector2 dir = Projectile.ai[0].ToRotationVector2();
            Vector2 raiz = Projectile.Center - Main.screenPosition;
            Vector2 punta = raiz + dir * Largo;
            Vector2[] cp = new Vector2[12];
            for (int i = 0; i < 12; i++)
                cp[i] = Vector2.Lerp(raiz, punta, i / 11f);
            cp[0] -= dir * 10f;

            // EL LOTE DE PRIMITIVAS DE LA CASA (SpriteBatch cerrado).
            RayoStrip.AbrirLote();
            try
            {
                DibujarBloom(cp);                   // primero el bloom (:185)
                DibujarHaz(cp);                     // después el rayo (:191)
                RayoStrip.VolcarTriangulos();       // el vaciado del pozo
            }
            catch { RayoStrip.VolcarTriangulos(); } // Volcar traga sus propias excepciones
            finally { RayoStrip.CerrarLote(); }

            // === EL MUZZLE DEL ORIGINAL (:200-209, byte a byte) ===
            VFXCore.AbrirAdditive();
            try
            {
                Vector2 pos = Projectile.Center - Main.screenPosition;
                // EL FACTOR MÁGICO (:180 — «the magic factor that makes
                // every electric shine effect so much better»): Cos01(t·85).
                float magico = 0.5f + 0.5f * MathF.Cos(Main.GlobalTimeWrappedHourly * 85f);
                float brillo = Inversa(0f, 12f, Tiempo)                // :201
                             * Inversa(0f, 7f, Projectile.timeLeft)
                             * MathHelper.Lerp(1f, 1.2f, magico)
                             * 0.45f;   // MaxGleamScaleFactor (ChargeUp.cs:45)
                Color fogonazo = new Color(255, 174, 147, 0);          // LensFlareColor with { A = 0 }
                Texture2D flare = RecursosSolyn.Flare;
                Texture2D glow = RecursosSolyn.Glow;
                for (int i = 0; i < 3; i++)                            // :206-207
                    Main.spriteBatch.Draw(flare, pos, null, fogonazo, 0f,
                        flare.Size() * 0.5f, brillo * 2f, SpriteEffects.None, 0f);
                for (int i = 0; i < 2; i++)                            // :208-209
                    Main.spriteBatch.Draw(glow, pos, null, fogonazo, 0f,
                        glow.Size() * 0.5f, brillo * 2f, SpriteEffects.None, 0f);
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            finally { VFXCore.RestaurarLoteVanilla(); }
            return false;
        }
    }

    // ======================================================================
    //  RecursosSolyn — LAS TEXTURAS DEL ORIGINAL, copiadas tal cual (la
    //  letra: «si no sabes puedes copiar el codigo exacto con texturas y
    //  todo») + LA CACHÉ CPU DEL RUIDO: el shader lo muestrea por píxel;
    //  el port por vértice lo lee de RAM (GetData UNA VEZ) y muestrea
    //  bilineal con envoltura — la réplica del sampler LinearWrap.
    // ======================================================================
    internal static class RecursosSolyn
    {
        private static Asset<Texture2D> _flare;    // ShineFlare (el flare del muzzle)
        private static Asset<Texture2D> _glow;     // BloomCircleSmall (el glow del muzzle)
        private static Asset<Texture2D> _ruido;    // DendriticNoiseZoomedOut (el ruido del shader)
        private static Color[] _ruidoDatos;
        private static int _ruidoAncho;
        private static int _ruidoAlto;

        /// <summary>El LensFlareColor (:55) como lo recibe el shader
        /// (:194 — ToVector4, alfa 1).</summary>
        public static readonly Vector4 LensFlareV4 = new Color(255, 174, 147).ToVector4();

        /// <summary>El secondaryColor dorado del shader (:193).</summary>
        public static readonly Vector4 SecundariaV4 = new Color(255, 196, 36).ToVector4();

        public static Texture2D Flare => _flare?.Value;
        public static Texture2D Glow => _glow?.Value;

        /// <summary>Carga las tres texturas (una vez) y vuelca el ruido a
        /// RAM. False solo si el asset falla (no debería: van con el mod).</summary>
        public static bool AsegurarCargado()
        {
            if (_flare == null)
            {
                try
                {
                    _flare = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/Investigacion/ShineFlare",
                        AssetRequestMode.ImmediateLoad);
                    _glow = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/Investigacion/BloomCircleSmall",
                        AssetRequestMode.ImmediateLoad);
                    _ruido = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/Investigacion/DendriticNoiseZoomedOut",
                        AssetRequestMode.ImmediateLoad);
                }
                catch { return false; }
            }
            if (_ruidoDatos == null)
            {
                Texture2D tex = _ruido?.Value;
                if (tex == null || tex.IsDisposed) return false;
                _ruidoAncho = tex.Width;
                _ruidoAlto = tex.Height;
                _ruidoDatos = new Color[_ruidoAncho * _ruidoAlto];
                try { tex.GetData(_ruidoDatos); }
                catch
                {
                    // EL PARACAÍDAS: gris plano — el haz vive aunque el
                    // ruido no (jamás debería disparar).
                    for (int i = 0; i < _ruidoDatos.Length; i++)
                        _ruidoDatos[i] = new Color(128, 128, 128, 255);
                }
            }
            return _flare.Value != null && _glow.Value != null;
        }

        /// <summary>EL SAMPLER DEL ORIGINAL (SolynTagTeamBeam.cs:195 —
        /// LinearWrap): bilineal con envoltura, réplica CPU del filtro
        /// (u,v ya con frac aplicado — el wrap es idempotente).</summary>
        public static float Muestrear(float u, float v)
        {
            int ancho = _ruidoAncho, alto = _ruidoAlto;
            float fx = u * ancho - 0.5f;
            float fy = v * alto - 0.5f;
            int x0 = (int)MathF.Floor(fx);
            int y0 = (int)MathF.Floor(fy);
            float tx = fx - x0;
            float ty = fy - y0;
            int xa = ((x0 % ancho) + ancho) % ancho;
            int ya = ((y0 % alto) + alto) % alto;
            int xb = (xa + 1) % ancho;
            int yb = (ya + 1) % alto;
            float s00 = _ruidoDatos[ya * ancho + xa].R;
            float s10 = _ruidoDatos[ya * ancho + xb].R;
            float s01 = _ruidoDatos[yb * ancho + xa].R;
            float s11 = _ruidoDatos[yb * ancho + xb].R;
            float arriba = s00 + (s10 - s00) * tx;
            float abajo = s01 + (s11 - s01) * tx;
            return (arriba + (abajo - arriba) * ty) * (1f / 255f);
        }
    }
}
