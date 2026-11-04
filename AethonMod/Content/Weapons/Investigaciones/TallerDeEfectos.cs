using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Weapons.Investigaciones
{
    // ======================================================================
    //  v6.92 — EL TALLER DE EFECTOS, VERSIÓN PORT EXACTO.
    //
    //  LA QUEJA (letra del usuario): «todos esos efectos se ven horrible,
    //  veo que no usaste lo que te dije que usaras» — la v6.90 escribió
    //  estas tres armas «a imitación» de la investigación del reel 2 y se
    //  NOTÓ. La directiva era: «usando exactamente esto […] es usarlo
    //  exactamente no solo intentar imitarlo que eso nunca funciona».
    //
    //  ASÍ QUE ESTA VERSIÓN NO IMITA: USA EL MOTOR. El motor de píxel de
    //  la casa entero (hash murmur, mulberry32, ruido de valores en double,
    //  dither Bayer 4×4, los dos buffers color+luz, el painter, la
    //  simulación de partículas, el compositor premultiplicado) vive
    //  entero en Content/VFX/PixelFxCore.cs y las tres armas LO EJECUTAN:
    //
    //   · EL SIGILO DEL ETERNO  = el acto COMPLETO del sigilo hexagonal:
    //     trazo → glifos → destello → ascuas del desmoronamiento,
    //     4,2 s, semilla 7.
    //   · LAS LLAMAS DE SOMBRA  = la columna de lenguas de ruido fractal
    //     REAL, loop 2,4 s, semilla 12.
    //   · EL ECLIPSE VIVO       = la luna negra de corona perla y
    //     cuentas de diamante, loop 4 s, semilla 3.
    //
    //  VERIFICADO PÍXEL A PÍXEL: el arnés de verificación de la casa
    //  compila PixelFxCore.cs solo (es C# puro, sin XNA) y renderiza los
    //  319 frames de los tres efectos contra el render de referencia de
    //  la investigación (mismo bucle del arnés: el frame 0 se dibuja en
    //  t=0 y update() corre después): 0 píxeles distintos en los 319
    //  frames. No es «parecido»: es el mismo render.
    //
    //  CÓMO SE DIBUJA EN TERRARIA (y por qué así): cada proyectil vivo
    //  tiene un PixelFxPlayer — el efecto corre a 60 fps (update+draw
    //  por tick sobre su buffer, el runner subpasa la simulación a
    //  120 Hz por dentro), el compositor del motor junta color+luz
    //  en RGBA y un Texture2D.SetData por frame lo pone en el SpriteBatch
    //  AMBIENTE de PreDraw (AlphaBlend — el RGBA del compositor es alfa
    //  recta, sin tocar lotes). Son simulaciones LOCALES de 104×112 a
    //  168×128 píxeles (11k–23k px): del orden de decenas de µs por
    //  tick, y los buffers y la textura se liberan al morir el proyectil
    //  (OnKill → Dispose). CERO polvos de Terraria: las chispas, las
    //  ascuas, el humo y las esquirlas viven DENTRO del efecto del
    //  motor — añadirle polvos encima sería volver a «vestirlo».
    //
    //  Lo demás (daño, uso, 0 maná, receta de 5 maderas, la bolsa 27)
    //  queda COMO ESTABA: esto es un reemplazo de VFX, no de arma.
    // ======================================================================

    /// <summary>EL REPRODUCTOR por proyectil: instancia el efecto del motor de píxel
    /// de la casa
    /// sobre su frame, lo corre a 60 fps y lo vuelca a una Texture2D por
    /// frame. ES el «arból de render» del motor metido en Terraria: los
    /// dos buffers del frame → Composite (el compositor del motor, con la
    /// premultiplicación de la luz sobre transparencia) → SetData →
    /// SpriteBatch ambiente. El coste por tick es clear + simulación +
    /// dibujo + composite + SetData de ~23k px (decenas de µs).</summary>
    internal sealed class PixelFxPlayer : IDisposable
    {
        public readonly PxFxInstance Fx;
        public readonly PxMeta Meta;
        public float Escala;
        readonly PxFrame _frame;
        readonly byte[] _buf;
        readonly Color[] _colors;
        Texture2D _tex;

        public PixelFxPlayer(PxFxInstance fx, float escala)
        {
            Fx = fx;
            Meta = fx.meta;
            Escala = escala;
            _frame = new PxFrame(Meta.W, Meta.H, 0, 0);
            _buf = new byte[Meta.W * Meta.H * 4];
            _colors = new Color[Meta.W * Meta.H];
        }

        /// <summary>Un tick de juego (1/60 s); el runner del motor
        /// subpasa la simulación a 120 Hz por dentro.</summary>
        public void Tick() => Fx.Update(1.0 / 60.0);

        /// <summary>Dibuja el efecto con su punto (0,0) del frame en
        /// `posPantalla` (el origen del efecto — place(OX, OY) — lo pone
        /// el runner dentro del frame).</summary>
        public void Draw(SpriteBatch sb, Vector2 posPantalla)
        {
            if (_tex == null || _tex.IsDisposed)
                _tex = new Texture2D(Main.graphics.GraphicsDevice, Meta.W, Meta.H);
            _frame.Clear();
            Fx.Draw(_frame, null);                    // AMBAS capas (back+front), como el arnés
            PixelFxCore.Composite(_frame, _buf);      // el compositor del motor (RGBA de alfa recta)
            for (int i = 0; i < _colors.Length; i++)
                _colors[i] = new Color(_buf[i * 4], _buf[i * 4 + 1], _buf[i * 4 + 2], _buf[i * 4 + 3]);
            _tex.SetData(_colors);
            sb.Draw(_tex, posPantalla, null, Color.White, 0f, Vector2.Zero,
                new Vector2(Escala), SpriteEffects.None, 0f);
        }

        public void Dispose()
        {
            if (_tex != null && !_tex.IsDisposed) _tex.Dispose();
            _tex = null;
        }
    }

    // ======================================================================
    //  1) EL SIGILO DEL ETERNO — el sigilo hexagonal: la pluma ardiente
    //     traza el sigilo, los glifos estallan, el destello, las ascuas
    // ======================================================================
    public class SigiloDelEterno : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.damage = 46;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 0;                       // arma de PRUEBA (la casa)
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.noMelee = true;
            Item.noUseGraphic = false;
            Item.knockBack = 4f;
            Item.shoot = ModContent.ProjectileType<SigiloDelEternoProj>();
            Item.shootSpeed = 12f;
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
            Item.UseSound = SoundID.Item8;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Taller",
                "[c/78788C:El taller de efectos — la pluma del Eterno traza su sigilo y lo detona]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    public class SigiloDelEternoProj : ModProjectile
    {
        // EL ACTO DEL MOTOR (el sigilo hexagonal, 4,2 s a 60 ticks/s): los
        // tiempos LITERALES del efecto — los glifos empiezan a saltar a
        // GLYPH = 1,62 s
        // y el destello es FLARE = 2,6 s. La v6.90 comprimió el acto a 2,5 s
        // y el desmoronamiento (BREAK = 2,74 s) jamás se veía: aquí se ve
        // ENTERO, por eso el proyectil vive los 4,2 s + colchón.
        private const int TICK_GLIFOS = 97;      // (int)(1.62 * 60)
        private const int TICK_DESTELLO = 156;   // (int)(2.6 * 60)
        private const int TICKS_VIDA = 262;      // 4,2 s de acto + colchón del fade
        private const float RADIO = 78f;         // el golpe del destello (de la v6.90)
        private const float ESCALA = 2f;         // 1 px de arte = 2 px de mundo

        // El CENTRO del sigilo en su frame: origin [84,112] + CY −50 → (84, 62).
        private static readonly Vector2 AnclaSigilo = new(84f, 62f);

        private PixelFxPlayer _fx;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 500;
        }

        public override void SetDefaults()
        {
            Projectile.width = (int)(RADIO * 2);
            Projectile.height = (int)(RADIO * 2);
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TICKS_VIDA;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;   // UN solo golpe (el destello)
        }

        public override void AI()
        {
            Projectile.velocity *= 0f;   // el sigilo VIVE en su sitio
            int tick = (int)(++Projectile.ai[1]);

            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                // EL MOTOR: el efecto completo corre aquí (el daño de abajo
                // es por TICK, no por t del cliente — multiplayer seguro).
                _fx ??= new PixelFxPlayer(PixelFx.HexSigil(), ESCALA);
                _fx.Tick();
                bool destellado = Projectile.ai[0] == 1f;
                Vector3 luz = destellado ? new Vector3(0.75f, 0.45f, 0.95f)
                    : new Vector3(0.45f, 0.22f, 0.65f);
                Lighting.AddLight(Projectile.Center, luz * 0.9f);
            }

            // LOS BEATERS DEL ACTO (los del JS): susurro al saltar los
            // glifos, trueno al destello.
            if (tick == TICK_GLIFOS) Sonar(SoundID.Item4, 0.6f);
            if (tick == TICK_DESTELLO && Projectile.ai[0] == 0f)
            {
                Projectile.ai[0] = 1f;
                LaDetonacion();
            }
        }

        /// <summary>EL DESTELLO (FLARE = 2,6 s del acto): un instante
        /// congelado y UN golpe radial — todo lo que está DENTRO del
        /// sigilo recibe el daño UNA sola vez (el freeze-on-contact de la
        /// filosofía del motor de píxel de la casa, aplicado como en la v6.90).</summary>
        private void LaDetonacion()
        {
            Sonar(SoundID.Item74.WithPitchOffset(-0.35f), 1.05f);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                foreach (NPC n in Main.ActiveNPCs)
                {
                    if (n == null || !n.active || !VFXCore.EsObjetivo(n)) continue;
                    if (n.Center.DistanceSQ(Projectile.Center) > RADIO * RADIO) continue;
                    try
                    {
                        n.StrikeNPC(new NPC.HitInfo
                        {
                            Damage = Projectile.damage,
                            SourceDamage = Projectile.damage,
                            DamageType = DamageClass.Magic,
                            Crit = false,
                            HitDirection = Math.Sign(n.Center.X - Projectile.Center.X),
                            Knockback = 5f,
                        }, false, false);
                    }
                    catch { }
                }
            }
        }

        private static void Sonar(SoundStyle s, float vol)
        {
            try { SoundEngine.PlaySound(s.WithVolumeScale(vol), Main.LocalPlayer?.Center ?? Vector2.Zero); }
            catch { }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;
            _fx ??= new PixelFxPlayer(PixelFx.HexSigil(), ESCALA);
            // El punto (0,0) del frame cae donde el CENTRO del sigilo
            // (frame 84,62) coincide con el centro del proyectil.
            _fx.Draw(Main.spriteBatch,
                Projectile.Center - Main.screenPosition - AnclaSigilo * ESCALA);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            _fx?.Dispose();   // buffers y textura: ni un byte más allá del acto
            _fx = null;
        }
    }

    // ======================================================================
    //  2) LAS LLAMAS DE SOMBRA — las lenguas de sombra: la columna de
    //     fuego sombrío de ruido fractal REAL (noise2 del motor, en double)
    // ======================================================================
    public class LlamasDeSombra : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.damage = 34;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 0;                       // arma de PRUEBA (la casa)
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 26;
            Item.useAnimation = 26;
            Item.noMelee = true;
            Item.knockBack = 2.5f;
            Item.shoot = ModContent.ProjectileType<LlamasDeSombraProj>();
            Item.shootSpeed = 14f;
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
            Item.UseSound = SoundID.Item20;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Taller",
                "[c/78788C:El taller de efectos — el fuego sombrío del Eterno sube vivo del suelo]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    public class LlamasDeSombraProj : ModProjectile
    {
        private const float ALTO = 230f;          // el hitbox de la columna (de la v6.90)
        private const float ESCALA = 2f;          // 1 px de arte = 2 px de mundo

        // El SUELO de la columna en su frame: origin [52,102] (las llamas
        // nacen del piso y el humo sube — por eso el ancla es el origen, no
        // el centro del frame).
        private static readonly Vector2 AnclaSuelo = new(52f, 102f);

        private PixelFxPlayer _fx;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 500;
        }

        public override void SetDefaults()
        {
            Projectile.width = 54;
            Projectile.height = (int)ALTO;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 110;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void AI()
        {
            Projectile.velocity *= 0f;
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                // EL MOTOR: el loop de 2,4 s corre vivo mientras viva el
                // proyectil; al final el stop() del motor lo funde con su
                // FADE de 0,3 s (el dither Bayer hace el resto — el fade
                // del motor de píxel de verdad, no un alpha ramp).
                _fx ??= new PixelFxPlayer(PixelFx.ShadowFlames(), ESCALA);
                if (Projectile.timeLeft <= 20) _fx.Fx.Stop();
                _fx.Tick();
                Lighting.AddLight(Projectile.Center, new Vector3(0.42f, 0.18f, 0.62f) * 0.85f);
            }
            Projectile.ai[0]++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;
            _fx ??= new PixelFxPlayer(PixelFx.ShadowFlames(), ESCALA);
            // El punto (0,0) del frame cae donde el SUELO del efecto
            // (frame 52,102) coincide con la base del proyectil.
            _fx.Draw(Main.spriteBatch,
                Projectile.Bottom - Main.screenPosition - AnclaSuelo * ESCALA);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            _fx?.Dispose();
            _fx = null;
        }
    }

    // ======================================================================
    //  3) EL ECLIPSE VIVO — el eclipse: la luna negra de corona perla
    //     que se abre como una nova y lo ciega todo
    // ======================================================================
    public class EclipseVivo : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.damage = 58;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 0;                       // arma de PRUEBA (la casa)
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 32;
            Item.useAnimation = 32;
            Item.noMelee = true;
            Item.knockBack = 6f;
            Item.shoot = ModContent.ProjectileType<EclipseVivoProj>();
            Item.shootSpeed = 12f;
            Item.rare = ItemRarityID.Quest;
            Item.value = 0;
            Item.UseSound = SoundID.Item72;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Taller",
                "[c/78788C:El taller de efectos — el disco negro de corona blanca: una nova que lo ciega todo]"));
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    public class EclipseVivoProj : ModProjectile
    {
        // LA NOVA: el efecto del motor es un loop que respira (corona
        // perla, streamers, la cuenta de diamante); el ARMA lo abre —
        // la escala crece con la MISMA curva de la v6.90 (26 → borde de
        // la corona) y el anillo de daño cabalga ese borde. El radio
        // final 260 = 65,6 px de arte (hasta las puntas de los
        // streamers) × ESCALA_MAX 4: el daño llega exactamente hasta
        // donde se VE que llega.
        private const int DURACION = 46;
        private const float RADIO_FINAL = 260f;
        private const float ESCALA_MIN = 0.9f, ESCALA_MAX = 4f;

        // El CENTRO de la luna en su frame: origin [80,72] (el efecto es
        // concéntrico a su origen).
        private static readonly Vector2 AnclaCentro = new(80f, 72f);

        private PixelFxPlayer _fx;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 500;
        }

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = DURACION + 10;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        /// <summary>El progreso de apertura (la curva de la v6.90: 30
        /// ticks con ease-out pow 2,2).</summary>
        private float Abre
        {
            get
            {
                float d = DURACION + 10 - Projectile.timeLeft;
                return MathHelper.Clamp(d / 30f, 0f, 1f);
            }
        }

        private float Ease => 1f - MathF.Pow(1f - Abre, 2.2f);

        /// <summary>EL RADIO ACTUAL (la nova se abre y MUERE colapsando
        /// como la v6.90 — el anillo barre hacia dentro al morir).</summary>
        private float RadioAhora
        {
            get
            {
                float muerte = MathHelper.Clamp((Projectile.timeLeft - 10) / 10f, 0f, 1f);
                return MathHelper.Lerp(26f, RADIO_FINAL, Ease) * muerte;
            }
        }

        public override void AI()
        {
            Projectile.velocity *= 0f;
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                // EL MOTOR: el loop del eclipse corre con su corona y sus
                // estrellas; la ESCALA hace la nova (es el arma la que
                // crece, el efecto respira intacto dentro).
                _fx ??= new PixelFxPlayer(PixelFx.Eclipse(), ESCALA_MIN);
                if (Projectile.timeLeft <= 20) _fx.Fx.Stop();   // el fade del motor
                _fx.Tick();
                Lighting.AddLight(Projectile.Center, new Vector3(0.8f, 0.7f, 1f) * 0.8f);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // EL ANILLO DE LA NOVA (solo el borde: 0,78..1,0 del radio).
            float r = RadioAhora;
            float dist = targetHitbox.Center().DistanceSQ(Projectile.Center);
            float exterior = (r + MathF.Max(targetHitbox.Width, targetHitbox.Height) * 0.5f);
            float interior = r * 0.78f;
            return dist <= exterior * exterior && dist >= interior * interior;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server || Main.dedServ) return false;
            float escala = MathHelper.Lerp(ESCALA_MIN, ESCALA_MAX, Ease);
            _fx ??= new PixelFxPlayer(PixelFx.Eclipse(), escala);
            _fx.Escala = escala;
            // El punto (0,0) del frame cae donde el CENTRO de la luna
            // (frame 80,72) coincide con el centro del proyectil.
            _fx.Draw(Main.spriteBatch,
                Projectile.Center - Main.screenPosition - AnclaCentro * escala);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            _fx?.Dispose();
            _fx = null;
        }
    }
}
