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
    //  v6.93 — LA RECREACIÓN (la sesión de LA LIMPIEZA DE COPYRIGHT; la
    //  letra: «todo nuestro proyecto debe ser libre de derechos de autor…
    //  todo lo que tenga derecho de autor debe ser recreado… asegúrate de
    //  que una vez limpiado no hayan fallos y de que todas las armas o
    //  efectos se mantengan iguales en el mod»).
    //
    //  LA HISTORIA: la v6.91 resolvió el histórico de los picos con un
    //  PORT EXACTO de código ajeno con copyright — la orden de aquella
    //  sesión («puedes copiar el codigo exacto con texturas y todo»),
    //  cumplida al pie de la letra. Esta sesión ordena lo contrario: el
    //  port MUERE y el arma NACE DE NUEVO, 100 % de la casa — MISMO
    //  ataque, MISMO aspecto, CERO letra ajena:
    //    · El COMPORTAMIENTO (fases, timings, daño/maná/criterio) se
    //      conserva tal cual: los SetDefaults son los de siempre.
    //    · El ASPECTO son NÚMEROS (anchuras, colores, umbrales): los
    //      números no son de nadie — son el efecto que el usuario manda
    //      mantener («todas las armas o efectos se mantengan iguales»).
    //    · El CÓDIGO es TODO nuestra letra: la geometría y la evaluación
    //      viven ahora en HazLib (la librería de haces de la casa) y las
    //      leyes propias del arma (la espina, el velo, el ruido) están
    //      reescritas de cero abajo — sin un solo símbolo del port.
    //
    //  EL ATAQUE (las fases, para el registro):
    //    1. LA CARGA (0,75 s): al mantener el clic nace MiradaCargaProj
    //       al alcance del ratón (dir·100): los destellos convergentes
    //       (el polvo que cae al círculo) mientras el flare estelar gira
    //       DOS VUELTAS ENTERAS (Pow(SmoothStep(r), 0.3)·4π) y crece
    //       IL(0,0.65,r)²·IL(1,0.9,r). Si el canal se rompe, la carga
    //       muere SIN rayo.
    //    2. LA DESCARGA (~1,9 s): al cumplir la carga suena el trueno y
    //       nace MiradaRayoProj: el rayo de 5.600 px que crece +175 por
    //       actualización, BARRE tras el ratón (lerp 0.15 del ángulo) y
    //       golpea en línea (ancho de ley·1.8, largo 95 %) — con la luz
    //       de 3 puntos y las chispas del filo. Muere solo (225
    //       actualizaciones, extraUpdates 1) desinflándose en los
    //       últimos 8 frames.
    //
    //  EL RENDER (todo sobre HazLib, la letra: «deberías crear una
    //  librería sobre los hazes… y así ya no tendremos más picos
    //  molestos»): dos capas de PAR (boca→punta) por UN draw call —
    //    · EL VELO (×1.9 de ancho, 44 nodos, FilasVelo): el halo de
    //      cúpula pow(4y(1−y), 3)·3.5 que envuelve la espina.
    //    · EL CUERPO (72 nodos, FilasNucleo): la espina FUCSIA con su
    //      ruido diagonal (frac(coords·2 + (0, x·5) − 3t)), su umbral
    //      suave (el 40 % oscuro no se pinta), su núcleo blanco
    //      (1/d), su fogonazo DE TINTA AZUL-NOCHE del arranque y su
    //      recolor DORADO DEL BROCHE del centro.
    //  v6.95 — LOS COLORES DE LA CASA (la letra: «ahora solo falta
    //  darle los colores de la casa, o sea los colores que usamos
    //  principalmente para el mod teniendo en cuenta los colores del
    //  libro»): la paleta del GRIMORIO vive en el rayo — ESPINA
    //  FUCSIA (240,0,192) del iris del ojo de la portada · VELO
    //  MORADO (170,80,255) del mod · FOGONAZO DE TINTA (96,128,255)
    //  azul-noche de las páginas al abrirse · CENTRO DORADO
    //  (255,200,80) del broche que cierra el libro. Mismo ataque,
    //  misma geometría, misma ley anti-picos: SOLO la tinta cambió.
    //  EL BORDE es LEY DE LA LIBRERÍA (Filo = smoothstep(0.5→0.4,
    //  |y−0.5|)): rgb=0 EXACTO en el filo — los picos de brillo son
    //  matemáticamente imposibles. Sobre el velo el filo es invisible
    //  (la cúpula ya es ~0 donde el filo actúa): el aspecto queda el
    //  de siempre.
    //
    //  LAS TEXTURAS (las de la casa, las de siempre): RuidoDendritico
    //  (el ruido del canvas, volcado a RAM y muestreado bilineal con
    //  envoltura), ResplandorChico (el glow del muzzle) y DestelloEstelar
    //  (el flare del muzzle) — las tres de Content/Effects/Procedural/
    //  Investigacion/, obra de la casa desde la v6.92.
    //
    //  CERO-GC: las leyes son MÉTODOS ESTÁTICOS cacheados en static
    //  readonly (una sola alocación al cargar el tipo); el estado que
    //  leen (el largo y el tiempo del rayo) vive en dos casillas
    //  estáticas que se refrescan al empezar cada hook — nada de
    //  closures por frame.
    // ======================================================================

    /// <summary>EL ARMA — La Mirada del Eterno: la carga estelar y la
    /// ráfaga que barre tras el ratón.</summary>
    public class MiradaDelEterno : ModItem
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
            Item.shoot = ModContent.ProjectileType<MiradaCargaProj>();
            Item.shootSpeed = 1f;
            Item.mana = 0;                   // ARMA DE PRUEBAS: la regla de la sesión
            Item.noMelee = true;
            Item.ArmorPenetration = 500;     // la referencia (500)
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
        }

        public override bool CanUseItem(Player player)
        {
            int carga = ModContent.ProjectileType<MiradaCargaProj>();
            int rayo = ModContent.ProjectileType<MiradaRayoProj>();
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
            // LA CARGA NACE AL ALCANCE DEL RATÓN (dir·100).
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
    //  MiradaCargaProj — LA CARGA (0,75 s): el destello estelar que gira
    //  dos vueltas enteras mientras convoca el polvo convergente.
    // ======================================================================
    public class MiradaCargaProj : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/FlareEstrella4Puntas";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 45;          // 0,75 s de carga
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

            // LA CARGA SIGUE AL RATÓN (dir·100).
            Vector2 dir = (Main.MouseWorld - owner.Center).SafeNormalize(-Vector2.UnitY);
            Projectile.Center = owner.Center + dir * 100f;

            // EL GIRO: dos vueltas enteras durante la carga — el ease
            // arraiga el arranque (Pow 0.3) y el smoothstep lo redondea.
            float r = 1f - Projectile.timeLeft / 45f;
            float suave = r * r * (3f - 2f * r);
            Projectile.rotation = MathF.Pow(MathF.Max(suave, 0f), 0.3f) * MathHelper.TwoPi * 2f;

            // LA ESCALA: el cuadrado es SOLO de la subida (0→0.65) y el
            // apagado desmonta el final (1→0.9) sin cuadrarse — el pop.
            float subida = MathHelper.Clamp((r - 0f) / 0.65f, 0f, 1f);
            float apagado = MathHelper.Clamp((r - 1f) / (0.9f - 1f), 0f, 1f);
            Projectile.scale = subida * subida * apagado;

            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                // EL POLVO CONVERGENTE: nace en el círculo de 200 px y
                // cae al centro a 10 u/s — el cielo que se pliega.
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(200f, 200f),
                    Main.rand.NextBool() ? DustID.Electric : DustID.PortalBoltTrail,
                    Vector2.Zero, 0,
                    Main.rand.NextBool() ? new Color(255, 200, 80) : new Color(240, 0, 192), 0.9f);
                Vector2 hacia = Projectile.Center - d.position;
                if (hacia.LengthSquared() > 1f) d.velocity = hacia / hacia.Length() * 10f;
                d.noGravity = true;
            }

            if (Projectile.timeLeft <= 1)
            {
                // === LA DESCARGA: el rayo nace y suena el trueno. ===
                if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item122.WithPitchOffset(-0.45f).WithVolumeScale(1.1f),
                        Projectile.Center);

                Projectile rayo = Projectile.NewProjectileDirect(
                    Projectile.GetSource_FromAI(), owner.Center + dir * 100f,
                    Vector2.Zero, ModContent.ProjectileType<MiradaRayoProj>(),
                    Projectile.damage, 6f, Projectile.owner,
                    dir.ToRotation(), 0f);
                rayo.netUpdate = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;
            try
            {
                if (!RecursosMirada.AsegurarCargado()) return false;
                Texture2D flare = RecursosMirada.Flare;
                Texture2D glow = RecursosMirada.Glow;
                if (flare == null || glow == null) return false;

                // TRES flares GIRANDO con la carga y DOS glows fijos —
                // el fogonazo FUCSIA del iris del libro, escala ·2.
                Vector2 pos = Projectile.Center - Main.screenPosition;
                float brillo = Projectile.scale * 0.45f;
                Color fogonazo = new Color(240, 0, 192, 0);
                Vector2 ojoFlare = flare.Size() * 0.5f;
                Vector2 ojoGlow = glow.Size() * 0.5f;

                VFXCore.AbrirAdditive();
                try
                {
                    for (int i = 0; i < 3; i++)
                        Main.spriteBatch.Draw(flare, pos, null, fogonazo,
                            Projectile.rotation, ojoFlare,
                            brillo * 2f, SpriteEffects.None, 0f);
                    for (int i = 0; i < 2; i++)
                        Main.spriteBatch.Draw(glow, pos, null, fogonazo,
                            0f, ojoGlow,
                            brillo * 2f, SpriteEffects.None, 0f);
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                finally { VFXCore.RestaurarLoteVanilla(); }
            }
            catch { }
            return false;
        }
    }

    // ======================================================================
    // ======================================================================
    //  MiradaRayoProj — EL RAYO (~1,9 s): la ráfaga de 5.600 px que barre
    //  tras el ratón. El render es TODO de la casa: el eje y el filo de
    //  borde los pone HazLib; las leyes propias (la espina y el velo)
    //  viven abajo como métodos estáticos — SIN PICOS por FÓRMULA.
    // ======================================================================
    public class MiradaRayoProj : ModProjectile
    {
        /// <summary>El alcance máximo del rayo.</summary>
        internal const float LargoMax = 5600f;

        /// <summary>El crecimiento por actualización.</summary>
        private const float Crecimiento = 175f;

        /// <summary>La vida útil en actualizaciones (225 × extraUpdates 1
        /// — el mismo número de actualizaciones de la referencia).</summary>
        private const float VidaUtil = 225f;

        /// <summary>El ancho base del haz (el semiancho de la espina).</summary>
        private const float AnchoBase = 96f;

        /// <summary>EL TIEMPO del haz (ai[1]: 0 → 225, una por actualización).</summary>
        private float Tiempo { get => Projectile.ai[1]; set => Projectile.ai[1] = value; }

        /// <summary>El largo actual (localAI[0] — viaja por el cable).</summary>
        private float Largo { get => Projectile.localAI[0]; set => Projectile.localAI[0] = value; }

        // ================================================================
        //  EL ESTADO DE LAS LEYES — dos casillas estáticas
        //
        //  Las leyes (anchura, tintes, vértices) son MÉTODOS ESTÁTICOS
        //  cacheados en static readonly (cero-GC: una alocación al cargar
        //  el tipo). El estado que leen — el largo y el tiempo del rayo —
        //  se refresca aquí: quien necesita una ley, primero refresca el
        //  presente (AI, Colliding y PreDraw lo hacen).
        // ================================================================

        private static float _leyLargo;
        private static float _leyTiempo;

        private void RefrescarLeyes()
        {
            _leyLargo = Largo;
            _leyTiempo = Tiempo;
        }

        /// <summary>v6.95 — EL REFRESCO DEL JEFE: el ataque hostil LA
        /// MIRADA DEL DIOS reutiliza ESTAS leyes (la letra: «también hay
        /// que darle la mirada del eterno como ataque medio raro» — el
        /// MISMO rayo, girado contra el portador): el jefe refresca el
        /// presente con SU largo y SU tiempo antes de dibujar.</summary>
        internal static void RefrescarPara(float largo, float tiempo)
        {
            _leyLargo = largo;
            _leyTiempo = tiempo;
        }

        // ================================================================
        //  LAS LEYES CACHEADAS — los delegados de HazLib
        // ================================================================

        // v6.95 — INTERNAL: el ataque hostil del jefe (LA MIRADA DEL
        // DIOS) reutiliza estas leyes cacheadas — el MISMO aspecto del
        // arma de la casa, sin duplicar una coma.
        internal static readonly HazLib.LeyAncho LeySemianchoCuerpo = SemianchoCuerpo;
        internal static readonly HazLib.LeyAncho LeySemianchoVelo = SemianchoVelo;
        internal static readonly HazLib.LeyNodo LeyTinteCuerpo = TinteCuerpo;
        internal static readonly HazLib.LeyNodo LeyTinteVelo = TinteVelo;
        internal static readonly HazLib.LeyVertice LeyVerticeCuerpo = VerticeCuerpo;
        internal static readonly HazLib.LeyVertice LeyVerticeVelo = VerticeVelo;

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
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.aiStyle = -1;
            Projectile.hide = true;             // se esconde…
        }

        // …y dibuja SOBRE los jugadores.
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

            // === LA DIRECCIÓN: barre tras el ratón (el ángulo persigue el
            //     del cursor con lerp 0.15 del delta envuelto). ===
            float objetivo = (Main.MouseWorld - owner.Center).ToRotation();
            float actual = Projectile.ai[0];
            float delta = MathHelper.WrapAngle(objetivo - actual);
            float nuevo = actual + delta * 0.15f;
            if (Math.Abs(delta) > 0.0001f) Projectile.netUpdate = true;
            Projectile.ai[0] = nuevo;

            // === EL LARGO: crece +175 por actualización hasta 5.600. ===
            Largo = MathF.Min(Largo + Crecimiento, LargoMax);

            // LA POSICIÓN (el muzzle a dir·100 del dueño).
            Vector2 dir = nuevo.ToRotationVector2();
            Projectile.Center = owner.Center + dir * 100f;
            Projectile.rotation = nuevo;

            Tiempo++;
            RefrescarLeyes();    // las leyes leen el presente

            // === LA LUZ (3 puntos: boca, medio y punta) y LAS CHISPAS DEL
            //     FILO (el borde del haz escupe polvo). La LUZ FUCSIA del
            //     iris del libro — el color de la casa. ===
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                Vector3 luz = new Vector3(0.92f, 0.08f, 0.78f);
                Vector2 fin = owner.Center + dir * Largo;
                Lighting.AddLight(Projectile.Center, luz);
                Lighting.AddLight(Vector2.Lerp(Projectile.Center, fin, 0.5f), luz * 0.8f);
                Lighting.AddLight(fin, luz * 0.6f);

                float semi = Semiancho(0.5f);
                Vector2 borde = Vector2.Lerp(Projectile.Center, fin, 0.5f);
                Vector2 perp = new Vector2(-dir.Y, dir.X);
                for (int i = 0; i < 3; i++)
                {
                    int lado = i % 2 == 0 ? 1 : -1;
                    Dust d = Dust.NewDustPerfect(
                        borde + perp * (semi * 0.9f * lado),
                        Main.rand.NextBool() ? DustID.Electric : DustID.PortalBoltTrail,
                        perp * lado * Main.rand.NextFloat(2f, 8f), 0,
                        Main.rand.NextBool() ? new Color(240, 0, 192) : new Color(255, 200, 80),
                        Main.rand.NextFloat(0.8f, 1.4f));
                    d.noGravity = true;
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // LA COLISIÓN: línea del 60 al 95 % del largo, ancho de ley
            // (0.25)·1.8 — el resto del rayo es teatro.
            Player owner = Main.player[Projectile.owner];
            if (owner == null || !owner.active) return false;
            RefrescarLeyes();
            float dir = Projectile.ai[0];
            Vector2 a = owner.Center + dir.ToRotationVector2() * 60f;
            Vector2 b = owner.Center + dir.ToRotationVector2() * (Largo * 0.95f);
            float punto = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                a, b, Semiancho(0.25f) * 1.8f, ref punto);
        }

        // ================================================================
        //  LAS LEYES DEL ASPECTO — el mismo look, nuestra letra
        // ================================================================

        /// <summary>Rampa inversa: v=a → 0, v=b → 1 (soporta rangos
        /// descendentes — la punta se apaga con 0.95→0.81).</summary>
        private static float Rampa(float a, float b, float v)
            => MathHelper.Clamp((v - a) / (b - a), 0f, 1f);

        /// <summary>La colina 0→1→0 (sin(π·x)) — el bulto del arranque.</summary>
        private static float Ola(float x)
            => MathF.Sin(MathF.PI * MathHelper.Clamp(x, 0f, 1f));

        /// <summary>EL SEMIANCHO de la espina en el nodo p — el bulto del
        /// arranque (la colina del crecimiento × la rampa del primer 5 % ×
        /// 32), el latido de 14 Hz (±6), el desinflado final (los últimos
        /// 8 frames), la PUNTA REDONDA (√(1.001−x²)) y el nacimiento a
        /// semiancho 4 que se abre al ideal (96 + bulto + latido).</summary>
        private static float Semiancho(float p)
        {
            float rel = _leyLargo / LargoMax;                       // el crecimiento
            float bulto = Ola(Rampa(0.15f, 0.85f, rel))
                * Rampa(0f, 0.05f, p) * 32f;
            float ideal = bulto + MathF.Cos(Main.GlobalTimeWrappedHourly * 90f) * 6f + AnchoBase;
            float cierre = Rampa(0f, 8f, VidaUtil - _leyTiempo);   // el desinflado
            float arranque = Rampa(0.05f, 0.012f, p);
            arranque = MathF.Sqrt(1.001f - arranque * arranque);    // la punta de círculo
            return Utils.Remap(_leyLargo, 0f, LargoMax, 4f, ideal) * cierre * arranque;
        }

        /// <summary>v6.95 — EL SEMIANCHO PÚBLICO DE LA CASA: el ataque
        /// hostil del jefe (LA MIRADA DEL DIOS) mide SU colisión con la
        /// MISMA ley del arma (el ancho vivo, no un número suelto).</summary>
        internal static float SemianchoDeCasa(float p) => Semiancho(p);

        private static float SemianchoCuerpo(float p) => Semiancho(p);

        /// <summary>El velo es la espina ×1.9 — el halo que la envuelve.</summary>
        private static float SemianchoVelo(float p) => Semiancho(p) * 1.9f;

        /// <summary>EL TINTE de la espina: FUCSIA DEL LIBRO (240,0,192 —
        /// el iris del ojo de la portada, el color de la casa) que nace
        /// (x<0.032) y muere (x>0.81) en transparente — la punta nunca
        /// se corta en seco.</summary>
        private static Color TinteCuerpo(float p)
        {
            float rel = _leyLargo / LargoMax;
            return new Color(240, 0, 192)
                * (Rampa(0f, 0.45f, rel) * Rampa(0f, 0.032f, p) * Rampa(0.95f, 0.81f, p));
        }

        /// <summary>EL TINTE del velo: MORADO DE LA CASA (170,80,255) al
        /// 34 % con el bump 0.02→0.05 — la aura que respira alrededor
        /// (el morado principal del mod envolviendo la espina fucsia).</summary>
        private static Color TinteVelo(float p)
            => new Color(170, 80, 255)
             * (Rampa(0.02f, 0.05f, p) * Rampa(0.95f, 0.81f, p)) * 0.34f;

        /// <summary>EL VÉRTICE de la espina — el INTERIOR del haz (el
        /// borde lo pone HazLib): el ruido diagonal cizarrado
        /// (frac(coords·2 + (0, x·5) − 3t)), el umbral suave (el 40 %
        /// oscuro no se pinta), el núcleo blanco (1/d), el fogonazo
        /// DE TINTA AZUL-NOCHE del arranque y el recolor dorado del
        /// centro (los colores del libro, la casa).</summary>
        private static Color VerticeCuerpo(float x, float y, Color tinte)
        {
            float t = Main.GlobalTimeWrappedHourly;

            // EL RUIDO DIAGONAL: 2 repeticiones a lo ancho, cizalladura
            // x·5 y flujo de 3 u/s — el hervidero que corre por la espina.
            float ruido = RecursosMirada.MuestrearRuido(
                Frac(x * 2f - t * 3f),
                Frac(y * 2f + x * 5f - t * 3f));

            // EL UMBRAL SUAVE: smoothstep(0.3, 0.45) — el grumo oscuro
            // del ruido jamás llega a pantalla.
            float opacidad = Smoothstep(0.3f, 0.45f, ruido);

            // EL NÚCLEO BLANCO: hacia el eje (d→0) el brillo interior se
            // dispara (1/d saturado) y se apaga al 25 % del semiancho.
            float d = MathF.Abs(y - 0.5f);
            float razon = d <= 0.0001f ? 1f : Saturar((0.01f + 0.97f * ruido) / d);
            float brilloInterior = razon * Smoothstep(0.25f, 0.12f, d);

            // EL FOGONAZO DE TINTA AZUL-NOCHE del arranque (solo el
            // primer 11 %) — la tinta del libro abriéndose.
            Vector4 arranque = RecursosMirada.Fogonazo * Smoothstep(0.11f, 0.01f, x);

            // EL RECOLOR DORADO: del centro hacia el borde la espina vira
            // al DORADO DEL BROCHE (el cierre dorado del libro) — el lerp
            // EXTRAPOLA (sin clamp).
            float mezcla = Smoothstep(0.3f, 0.18f, d) + ruido * 0.1f;
            Vector4 color = Vector4.Lerp(tinte.ToVector4(), RecursosMirada.Dorado, mezcla);

            // LA SUMA SATURADA — el filo de borde lo pone la librería
            // encima de esto.
            Vector4 suma = color * opacidad + new Vector4(brilloInterior) + arranque;
            return AColor(Saturar4(suma));
        }

        /// <summary>EL VÉRTICE del velo — la CÚPULA: pow(4y(1−y), 3)·3.5.
        /// En y=0/1 la potencia de 0 es 0 EXACTO: el halo nace y muere
        /// en el borde sin encender nada (el filo de HazLib encima es
        /// invisible — la cúpula ya es ~0 donde actúa).</summary>
        private static Color VerticeVelo(float x, float y, Color tinte)
        {
            float bulto = y * (4f - y * 4f);
            float k = bulto * bulto * bulto * 3.5f;
            return AColor(tinte.ToVector4() * k);
        }

        // ================================================================
        //  LOS AUXILIARES DE LA LEY — helpers numéricos propios
        // ================================================================

        /// <summary>El Vector4 de la ley → el Color byte del vértice (el
        /// alfa no pinta: el lote aditivo de la casa suma el rgb).</summary>
        private static Color AColor(Vector4 v)
            => new Color(Byte255(v.X), Byte255(v.Y), Byte255(v.Z), 255);

        private static byte Byte255(float f)
            => (byte)Math.Clamp((int)(f * 255f + 0.5f), 0, 255);

        /// <summary>saturate de HLSL, componente a componente.</summary>
        private static Vector4 Saturar4(Vector4 v)
            => new Vector4(Saturar(v.X), Saturar(v.Y), Saturar(v.Z), Saturar(v.W));

        private static float Saturar(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

        /// <summary>El smoothstep de HLSL (soporta bordes invertidos —
        /// smoothstep(0.3, 0.45) y smoothstep(0.5, 0.4) viven de esto).</summary>
        private static float Smoothstep(float a, float b, float x)
        {
            float t = Saturar((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        /// <summary>frac de HLSL (negativos incluidos: x−floor(x) ∈ [0,1)).</summary>
        private static float Frac(float x) => x - MathF.Floor(x);

        // ================================================================
        //  EL RENDER — dos capas de PAR sobre HazLib, UN draw call
        // ================================================================

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;
            try
            {
                if (!RecursosMirada.AsegurarCargado()) return false;
                Player owner = Main.player[Projectile.owner];
                if (owner == null || !owner.active) return false;
                RefrescarLeyes();

                // === EL EJE: boca→punta, con la boca retraída 10 px TRAS
                //     el muzzle — el arranque redondo TAPA el origen. ===
                Vector2 dir = Projectile.ai[0].ToRotationVector2();
                Vector2 raiz = Projectile.Center - Main.screenPosition;
                Vector2 punta = raiz + dir * Largo;
                Vector2 boca = raiz - dir * 10f;

                HazLib.Abrir();
                try
                {
                    // CAPA 1 — EL VELO (primero, debajo): ×1.9 de ancho,
                    // 44 nodos, la cúpula — el halo que envuelve la espina.
                    HazLib.PrepararPar(boca, punta, LeySemianchoVelo, 0f, 44);
                    HazLib.Dibujar(LeyTinteVelo, LeyVerticeVelo, HazLib.FilasVelo);

                    // CAPA 2 — EL CUERPO (después, encima): el ancho de
                    // ley, 72 nodos, el ruido diagonal — la espina rosa.
                    HazLib.PrepararPar(boca, punta, LeySemianchoCuerpo, 0f, 72);
                    HazLib.Dibujar(LeyTinteCuerpo, LeyVerticeCuerpo, HazLib.FilasNucleo);
                }
                finally { HazLib.Cerrar(); }

                // === EL MUZZLE: tres flares + dos glows con el LATIDO del
                //     factor mágico (Cos01(t·85)) — el fogonazo FUCSIA del
                //     iris del libro. ===
                VFXCore.AbrirAdditive();
                try
                {
                    Texture2D flare = RecursosMirada.Flare;
                    Texture2D glow = RecursosMirada.Glow;
                    if (flare == null || glow == null) return false;

                    Vector2 pos = Projectile.Center - Main.screenPosition;
                    float latido = 0.5f + 0.5f * MathF.Cos(Main.GlobalTimeWrappedHourly * 85f);
                    float brillo = Rampa(0f, 12f, _leyTiempo)        // el encendido
                                 * Rampa(0f, 7f, Projectile.timeLeft) // el apagado
                                 * MathHelper.Lerp(1f, 1.2f, latido)
                                 * 0.45f;
                    Color fogonazo = new Color(240, 0, 192, 0);
                    Vector2 ojoFlare = flare.Size() * 0.5f;
                    Vector2 ojoGlow = glow.Size() * 0.5f;
                    for (int i = 0; i < 3; i++)
                        Main.spriteBatch.Draw(flare, pos, null, fogonazo, 0f,
                            ojoFlare, brillo * 2f, SpriteEffects.None, 0f);
                    for (int i = 0; i < 2; i++)
                        Main.spriteBatch.Draw(glow, pos, null, fogonazo, 0f,
                            ojoGlow, brillo * 2f, SpriteEffects.None, 0f);
                }
                catch { VFXCore.CerrarLoteSiAbierto(); }
                finally { VFXCore.RestaurarLoteVanilla(); }
            }
            catch { }
            return false;
        }
    }

    // ======================================================================
    //  RecursosMirada — LAS TEXTURAS DE LA CASA y LA CACHÉ CPU DEL RUIDO:
    //  el ruido del canvas se vuelca a RAM UNA VEZ (GetData) y se
    //  muestrea bilineal con envoltura — la réplica del sampler en CPU.
    // ======================================================================
    internal static class RecursosMirada
    {
        private static Asset<Texture2D> _flare;    // DestelloEstelar — el flare del muzzle
        private static Asset<Texture2D> _glow;     // ResplandorChico — el glow del muzzle
        private static Asset<Texture2D> _ruido;    // RuidoDendritico — el ruido del canvas
        private static Color[] _ruidoRam;
        private static int _ruidoAncho;
        private static int _ruidoAlto;

        /// <summary>El color del fogonazo del arranque: TINTA AZUL-NOCHE
        /// luminosa (96,128,255) — la tinta de las páginas del libro
        /// (la casa: el arranque es la página que se abre).</summary>
        public static readonly Vector4 Fogonazo = new Color(96, 128, 255).ToVector4();

        /// <summary>El DORADO DEL BROCHE (255,200,80) — el cierre dorado
        /// del libro, el recolor del centro de la espina.</summary>
        public static readonly Vector4 Dorado = new Color(255, 200, 80).ToVector4();

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
                        "AethonMod/Content/Effects/Procedural/Investigacion/DestelloEstelar",
                        AssetRequestMode.ImmediateLoad);
                    _glow = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/Investigacion/ResplandorChico",
                        AssetRequestMode.ImmediateLoad);
                    _ruido = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Effects/Procedural/Investigacion/RuidoDendritico",
                        AssetRequestMode.ImmediateLoad);
                }
                catch { return false; }
            }
            if (_ruidoRam == null)
            {
                Texture2D tex = _ruido?.Value;
                if (tex == null || tex.IsDisposed) return false;
                _ruidoAncho = tex.Width;
                _ruidoAlto = tex.Height;
                _ruidoRam = new Color[_ruidoAncho * _ruidoAlto];
                try { tex.GetData(_ruidoRam); }
                catch
                {
                    // EL PARACAÍDAS: gris plano — el haz vive aunque el
                    // ruido no (jamás debería disparar).
                    for (int i = 0; i < _ruidoRam.Length; i++)
                        _ruidoRam[i] = new Color(128, 128, 128, 255);
                }
            }
            return _flare.Value != null && _glow.Value != null;
        }

        /// <summary>EL SAMPLER del ruido: bilineal con envoltura (u,v ya
        /// con frac aplicado — el wrap es idempotente). Lee el canal R de
        /// la caché en RAM y devuelve 0..1.</summary>
        public static float MuestrearRuido(float u, float v)
        {
            int w = _ruidoAncho, h = _ruidoAlto;
            float fx = u * w - 0.5f;
            float fy = v * h - 0.5f;
            int x0 = (int)MathF.Floor(fx);
            int y0 = (int)MathF.Floor(fy);
            int xa = Envolver(x0, w);
            int ya = Envolver(y0, h);
            int xb = (xa + 1) % w;
            int yb = (ya + 1) % h;
            float tx = fx - x0;
            float ty = fy - y0;

            Color s00 = _ruidoRam[ya * w + xa];
            Color s10 = _ruidoRam[ya * w + xb];
            Color s01 = _ruidoRam[yb * w + xa];
            Color s11 = _ruidoRam[yb * w + xb];
            float arriba = (s00.R + (s10.R - s00.R) * tx) * (1f / 255f);
            float abajo = (s01.R + (s11.R - s01.R) * tx) * (1f / 255f);
            return arriba + (abajo - arriba) * ty;
        }

        /// <summary>El wrap del sampler (negativos incluidos).</summary>
        private static int Envolver(int i, int n) => ((i % n) + n) % n;
    }
}
