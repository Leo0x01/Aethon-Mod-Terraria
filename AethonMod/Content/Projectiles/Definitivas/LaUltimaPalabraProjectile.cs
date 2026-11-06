using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Systems;
using AethonMod.Content.VFX;
using AethonMod.Content.Weapons;
using AethonMod.Content.Weapons.Investigaciones;

namespace AethonMod.Content.Projectiles.Definitivas
{
    // ======================================================================
    //  LaUltimaPalabraProjectile — v6.100 — EL RITUAL EN TRES ACTOS.
    //
    //  LA LETRA DEL USUARIO: «crea 1 arma definitiva con todas las
    //  nuevas herramientas y tecnicas vfx» — este proyectil ES la
    //  demostración de que TODAS las herramientas de la casa caben en
    //  un solo gesto:
    //
    //   · ACTO I — LA CARGA (canal, hasta 140 t): EL LIBRO REAL del
    //     Eterno (GrimoireEternal, el sprite del ítem) se abre sobre
    //     el portador con vaivén e inclinación, EL CORAZÓN DE LA
    //     CORONA del motor de píxeles late CRECIENDO (escala 1.2→2.8),
    //     los 12 satélites orbitan ensanchándose, el disco del suelo
    //     respira y las chispas doradas suben (pantalla sacudida al
    //     máximo en el DECRETO).
    //   · ACTO II — EL DECRETO (26 t): LA PALABRA cruza el mapa (2400
    //     px o el primer muro +90) — el rayo de TRES FUNDAS (núcleo
    //     blanco 13 px, funda dorada 34 px viva, halo 66 px) con el
    //     VELO DE RUIDO procedural encima, la punta que viaja, el
    //     destello de 4 puntas en el origen; el cono de colisión
    //     propio (proyección + banda ±52 px) golpea TODO lo que cruza.
    //   · ACTO III — EL JUICIO (90 t en _fin): LA NOVA DOBLE — el
    //     NOVA ACTO del motor de píxeles + la FLOR DE FUEGO de
    //     PyraLib.Estallido (la rampa blanco→oro→naranja→rojo de la
    //     paleta del Juicio), la ONDA DE CHOQUE de OndaLib.Pulse, el
    //     anillo del radio vivo; a los 26 t EL CHARCO queda hirviendo
    //     (ruido + anillo de fuego + burbujas) 5 s y LAS ALMAS vuelan
    //     al portador por curvas Bézier dobles (SparkleStar). El
    //     daño: la onda exterior ×1 (radio hasta 320), el corazón
    //     ×0.35 cada 15 t (112 px).
    // ======================================================================
    public class LaUltimaPalabraProjectile : ModProjectile
    {
        /// <summary>EL ACTO: la carga (el libro abierto, canal).</summary>
        private const byte ACTO_CARGA = 0;

        /// <summary>EL ACTO: el decreto (el rayo cruzando el mapa).</summary>
        private const byte ACTO_DECRETO = 1;

        /// <summary>EL ACTO: el juicio (la nova y el charco).</summary>
        private const byte ACTO_JUICIO = 2;

        /// <summary>La carga máxima (ticks).</summary>
        private const int CARGA_MAX = 140;

        // El fin del decreto (donde cae el juicio).
        private Vector2 _fin;

        // El largo del decreto (px — hasta el muro o 2400).
        private float _largoDecreto;

        // LOS MOTORES de píxeles: el corazón de la carga + la nova.
        private PixelFxPlayer _motorCarga;
        private PixelFxPlayer _motorNova;

        // El próximo golpe por impío (GameUpdateCount).
        private static readonly Dictionary<int, int> _proximo = new();

        private static Texture2D[] _ruidoCache;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        /// <summary>EL TIEMPO del acto actual.</summary>
        private float T => Projectile.ai[0];

        /// <summary>EL RUMBO del decreto (unitario).</summary>
        private Vector2 Rumbo => new(Projectile.localAI[0], Projectile.localAI[1]);

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
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Player dueno = Main.player[Projectile.owner];
            if (dueno != null && dueno.active)
                Projectile.Center = dueno.MountedCenter;
        }

        public override void AI()
        {
            Projectile.ai[0] += 1f;
            Player dueno = Main.player[Projectile.owner];
            if (dueno == null || !dueno.active || dueno.dead)
            {
                Projectile.Kill();
                return;
            }

            byte acto = (byte)Projectile.ai[1];

            // LOS MOTORES laten en su acto (solo cliente).
            if (Main.netMode != NetmodeID.Server && !Main.dedServ)
            {
                if (acto == ACTO_CARGA)
                    MotorCarga().Tick();
                if (acto == ACTO_JUICIO)
                    MotorNova().Tick();
                if (Projectile.timeLeft <= 12)
                {
                    _motorCarga?.Fx.Stop();
                    _motorNova?.Fx.Stop();
                }
            }

            switch (acto)
            {
                // ========================================================
                //  ACTO I — LA CARGA.
                // ========================================================
                case ACTO_CARGA:
                {
                    Projectile.Center = dueno.MountedCenter;
                    Projectile.timeLeft = 4;

                    // EL DISPARO: al soltar (o cargar al máximo) SALE LA
                    // PALABRA hacia el cursor.
                    if (!dueno.channel || T >= CARGA_MAX)
                    {
                        Vector2 rumbo = Main.MouseWorld - Projectile.Center;
                        if (rumbo.LengthSquared() < 1f)
                            rumbo = dueno.direction > 0 ? Vector2.UnitX : -Vector2.UnitX;
                        rumbo.Normalize();
                        Projectile.localAI[0] = rumbo.X;
                        Projectile.localAI[1] = rumbo.Y;

                        // EL LARGO: 2400 px o el primer MURO (+90 de
                        // generosidad para que la nova no nazca dentro).
                        _largoDecreto = 2400f;
                        for (float d = 40f; d <= 2400f; d += 16f)
                        {
                            Vector2 paso = Projectile.Center + rumbo * d;
                            if (WorldGen.SolidTile((int)(paso.X / 16f), (int)(paso.Y / 16f), false))
                            {
                                _largoDecreto = d + 90f;
                                break;
                            }
                        }
                        _fin = Projectile.Center + rumbo * _largoDecreto;

                        // EL CAMBIO DE ACTO: el decreto nace.
                        Projectile.ai[1] = ACTO_DECRETO;
                        Projectile.ai[0] = 0f;
                        Projectile.timeLeft = 30;
                        if (Main.netMode != NetmodeID.Server)
                        {
                            SoundStyle salida = SoundID.Item74.WithPitchOffset(-0.35f);
                            SoundEngine.PlaySound(in salida, Projectile.Center);
                            OndaLib.Flash(new Color(255, 240, 200), 0.14f);
                            Pantalla.Sacudir(2.4f, 0.2f, "UltimaPalabraDecreto");
                        }
                    }
                    else
                    {
                        // LA CARGA VIVA: las chispas doradas suben del
                        // portador y la luz crece con la carga.
                        if (Main.rand.NextBool(3))
                            Dust.NewDustPerfect(
                                Projectile.Center + new Vector2(
                                    Main.rand.NextFloat(-150f, 150f),
                                    Main.rand.NextFloat(20f, 90f)),
                                57, new Vector2(0f, -Main.rand.NextFloat(1.2f, 2.6f)),
                                170, new Color(255, 226, 150), 0.7f).noGravity = true;
                        float carga = Math.Min(1f, T / CARGA_MAX);
                        if (Main.netMode != NetmodeID.Server)
                            Lighting.AddLight(Projectile.Center,
                                new Vector3(1f, 0.9f, 0.6f) * (0.8f + carga * 1.4f));
                    }
                    break;
                }

                // ========================================================
                //  ACTO II — EL DECRETO.
                // ========================================================
                case ACTO_DECRETO:
                {
                    // LA PALABRA VIAJA: el punto de impacto avanza del
                    // portador al fin en 22 ticks.
                    float avanza = MathF.Min(1f, T / 22f);
                    Projectile.Center = dueno.MountedCenter + Rumbo * (_largoDecreto * avanza);

                    // EL CONO DE COLISIÓN propio: TODO lo que esté en la
                    // banda del rayo ya disparado recibe el golpe (cada
                    // 12 t por impío — la palabra atraviesa, no martilla).
                    if (Projectile.owner == Main.myPlayer && T % 4f == 0f)
                    {
                        Vector2 origen = dueno.MountedCenter;
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            NPC n = Main.npc[i];
                            if (n == null || !n.active || !n.CanBeChasedBy())
                                continue;
                            Vector2 rel = n.Center - origen;
                            float proyeccion = Vector2.Dot(rel, Rumbo);
                            if (proyeccion < -n.width ||
                                proyeccion > _largoDecreto * avanza + 40f)
                                continue;
                            // LA BANDA: ±52 px + el 40% del ancho del impío.
                            if (MathF.Abs(rel.X * -Rumbo.Y + rel.Y * Rumbo.X)
                                > 52f + n.width * 0.4f)
                                continue;
                            if (_proximo.TryGetValue(n.whoAmI, out int hasta) &&
                                hasta > (int)Main.GameUpdateCount)
                                continue;
                            _proximo[n.whoAmI] = (int)Main.GameUpdateCount + 12;
                            GolpeMotor.Golpear(Projectile, n, Projectile.damage, 3f);
                        }
                    }

                    if (Main.netMode != NetmodeID.Server)
                        Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.92f, 0.65f) * 2.2f);

                    // LA LLEGADA: el juicio nace en el fin.
                    if (T >= 26f)
                    {
                        Projectile.Center = _fin;
                        Projectile.ai[1] = ACTO_JUICIO;
                        Projectile.ai[0] = 0f;
                        Projectile.timeLeft = 96;
                        if (Main.netMode != NetmodeID.Server)
                        {
                            SoundStyle boom = SoundID.Item74.WithPitchOffset(0.4f);
                            SoundEngine.PlaySound(in boom, Projectile.Center);
                            SoundStyle campana = SoundID.NPCHit53;
                            campana.Pitch = 0.55f;
                            campana.Volume = 0.8f;
                            SoundEngine.PlaySound(in campana, Projectile.Center);
                            OndaLib.Flash(new Color(255, 235, 180), 0.22f, 12);
                            Pantalla.Sacudir(5f, 0.3f, "UltimaPalabraJuicio");
                        }
                    }
                    break;
                }

                // ========================================================
                //  ACTO III — EL JUICIO.
                // ========================================================
                default:
                {
                    Projectile.velocity = Vector2.Zero;
                    float t = T;

                    // EL DAÑO DEL JUICIO (solo el dueño manda): la onda
                    // exterior crece (hasta 320 px) golpeando su frente ×1
                    // cada 40 t; el CORAZÓN (112 px) muele ×0.35 cada 15 t.
                    if (Projectile.owner == Main.myPlayer && !Main.gameMenu)
                    {
                        float radio = MathF.Min(320f, 40f + t * 14f);
                        float interior = MathF.Max(0f, radio - 120f);
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            NPC n = Main.npc[i];
                            if (n == null || !n.active || !n.CanBeChasedBy() || n.dontTakeDamage)
                                continue;
                            float dist = MathF.Sqrt(n.Center.DistanceSQ(Projectile.Center));
                            if (dist > radio)
                                continue;
                            if (_proximo.TryGetValue(n.whoAmI, out int hasta) &&
                                hasta > (int)Main.GameUpdateCount)
                                continue;
                            bool frente = dist >= interior;          // el borde de la onda
                            bool corazon = t > 30f && dist <= 112f;  // el charco hirviendo
                            if (frente || corazon)
                            {
                                _proximo[n.whoAmI] = (int)Main.GameUpdateCount + (frente ? 40 : 15);
                                GolpeMotor.Golpear(Projectile, n,
                                    frente ? Projectile.damage : (int)(Projectile.damage * 0.35f),
                                    frente ? 5f : 0f);
                            }
                        }
                    }

                    // EL RESPLANDOR y las brasas que suben del juicio.
                    if (Main.netMode != NetmodeID.Server)
                    {
                        Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.85f, 0.55f) * 1.8f);
                        if (Main.rand.NextBool(4) && t < 60f)
                        {
                            float ang = Main.rand.NextFloat(MathF.PI * 2f);
                            Dust.NewDustPerfect(
                                Projectile.Center
                                    + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.5f)
                                        * Main.rand.NextFloat(60f, 240f),
                                57, new Vector2(0f, Main.rand.NextFloat(1f, 3f)), 170,
                                Main.rand.NextBool() ? new Color(255, 226, 150) : new Color(255, 170, 60),
                                0.8f).noGravity = true;
                        }
                    }

                    if (T >= 90f)
                        Projectile.Kill();
                    break;
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            _motorCarga?.Dispose();
            _motorNova?.Dispose();
            _motorCarga = _motorNova = null;
            if (Projectile.owner == Main.myPlayer)
                _proximo.Clear();
        }

        /// <summary>EL MOTOR DE LA CARGA: el CorazonCorona — el corazón
        /// del libro latiendo, creciendo con la carga.</summary>
        private PixelFxPlayer MotorCarga()
        {
            if (_motorCarga == null)
                _motorCarga = new PixelFxPlayer(
                    PixelFxCasas.CorazonCorona(999,
                        PxColor.Rgb(255, 240, 200), PxColor.Rgb(230, 160, 50)),
                    2.1f);
            return _motorCarga;
        }

        /// <summary>EL MOTOR DEL JUICIO: la NovaActo — el acto entero de
        /// la nova de píxeles en el impacto.</summary>
        private PixelFxPlayer MotorNova()
        {
            if (_motorNova == null)
                _motorNova = new PixelFxPlayer(
                    PixelFxCasas.NovaActo(999,
                        PxColor.Rgb(255, 235, 180), PxColor.Rgb(255, 170, 60)),
                    2.6f);
            return _motorNova;
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

        private static Texture2D ItemTex(int tipo) => TextureAssets.Item[tipo].Value;

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
                byte acto = (byte)Projectile.ai[1];
                Color oro = new(255, 226, 150);
                Color marfil = new(255, 248, 225);
                Color naranja = new(255, 170, 60);

                switch (acto)
                {
                    // ====================================================
                    //  ACTO I — LA CARGA (el libro abierto sobre el hombro).
                    // ====================================================
                    case ACTO_CARGA:
                    {
                        float carga = Math.Min(1f, T / CARGA_MAX);

                        // EL CORAZÓN de píxeles CRECIENDO con la carga.
                        if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                        {
                            PixelFxPlayer motor = MotorCarga();
                            motor.Escala = 1.2f + carga * 1.6f;
                            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                                SamplerState.PointClamp, DepthStencilState.None,
                                RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                            motor.Draw(sb, dueno.MountedCenter - Main.screenPosition
                                - new Vector2(motor.Meta.Ox, motor.Meta.Oy) * motor.Escala);
                            sb.End();
                        }

                        Vector2 pies = dueno.MountedCenter - Main.screenPosition;
                        Vector2 sobreCabeza = pies
                            - new Vector2(0f, 64f + MathF.Sin(t * 2f) * 4f);

                        // EL LIBRO REAL — el sprite del Grimorio del Eterno
                        // flotando abierto (lote ambiente, con su vaivén).
                        // v6.101 — LA CURA DEL client.log: el End() huérfano
                        // que vivía aquí mataba el Acto I CADA FRAME — el
                        // bloque del motor de arriba (Begin→Draw→End) ya
                        // había cerrado el lote y este segundo End lanzaba
                        // «End was called, but Begin has not yet been
                        // called» (la excepción silenciosa del log). EL
                        // CONTRATO de la casa: CerrarLoteSiAbierto() deja
                        // el lote CERRADO y cada bloque abre/cierra lo
                        // SUYO — jamás un End sin su Begin.
                        Texture2D libro = ItemTex(ModContent.ItemType<GrimoireEternal>());
                        sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                            SamplerState.LinearClamp, DepthStencilState.None,
                            RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                        sb.Draw(libro, sobreCabeza, null, Color.White * 0.96f,
                            MathF.Sin(t * 1.6f) * 0.09f, Utils.Size(libro) * 0.5f,
                            2.6f + carga * 0.9f, SpriteEffects.None, 0f);
                        sb.End();

                        // EL ALIENTO del libro: el aura, la garganta roja
                        // latiendo, el foco de arriba, los 12 SATÉLITES
                        // orbitando ensanchándose y los anillos del suelo.
                        sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                            SamplerState.LinearClamp, DepthStencilState.None,
                            RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                        Capa(sb, Tex("GlowOrb"), sobreCabeza, oro * (0.35f + 0.4f * carga),
                            0f, (90f + 130f * carga) / 128f);
                        Capa(sb, Tex("GlowOrb"), sobreCabeza + new Vector2(0f, 6f),
                            new Color(255, 90, 60)
                                * (0.3f + 0.25f * MathF.Sin(t * 6f) * carga),
                            0f, 17f / 64f);
                        Vector2 foco = sobreCabeza - new Vector2(0f, 44f);
                        Capa(sb, Tex("GlowOrb"), foco, Color.White * (0.5f + 0.4f * carga),
                            0f, 0.1875f);
                        Capa(sb, Tex("GlowOrb"), foco, oro * 0.7f, 0f, 11f / 32f);
                        for (int i = 0; i < 12; i++)
                        {
                            float ang = t * (0.8f + 0.6f * carga) + i * MathF.PI * 2f / 12f;
                            Vector2 pos = pies + new Vector2(MathF.Cos(ang), MathF.Sin(ang) * 0.6f)
                                * (110f + 60f * carga);
                            Capa(sb, Tex("SparkleStar"), pos,
                                marfil * (0.35f + 0.4f * carga * MathF.Sin(t * 3f + i)),
                                ang, (10f + 6f * carga) / 32f);
                        }
                        Suelo(sb, Tex("GlowCircleWhite"), pies + new Vector2(0f, 26f),
                            oro * (0.08f + 0.1f * carga), 0f, 260f + 220f * carga, 90f);
                        Suelo(sb, Tex("MagicRing"), pies + new Vector2(0f, 26f),
                            marfil * (0.15f + 0.15f * carga), t * 1.1f,
                            250f + 210f * carga, 84f);
                        sb.End();
                        break;
                    }

                    // ====================================================
                    //  ACTO II — EL DECRETO (el rayo de tres fundas).
                    // ====================================================
                    case ACTO_DECRETO:
                    {
                        sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                            SamplerState.LinearClamp, DepthStencilState.None,
                            RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                        Vector2 origen = dueno.MountedCenter - Main.screenPosition - new Vector2(0f, 64f);
                        Vector2 punta = Projectile.Center - Main.screenPosition;
                        float ang = Rumbo.ToRotation();
                        float largo = Vector2.Distance(origen, punta);

                        // EL VELO DE RUIDO sobre todo el rayo.
                        Capa(sb, Ruido(7, 6.5f), (origen + punta) * 0.5f, oro * 0.1f,
                            ang, largo / 512f);
                        // LAS TRES FUNDAS: halo 66, funda dorada viva 34±8,
                        // núcleo blanco 13.
                        Rayo(sb, origen, ang, largo, 66f, oro * 0.32f);
                        Rayo(sb, origen, ang, largo, 34f + 8f * MathF.Sin(t * 22f), naranja * 0.85f);
                        Rayo(sb, origen, ang, largo, 13f, Color.White);
                        // LA PUNTA QUE VIAJA (el frente de la palabra).
                        float avanza = T / 26f % 1f;
                        Vector2 frente = origen + Rumbo * (largo * avanza);
                        Capa(sb, Tex("GlowOrb"), frente, marfil * 0.7f, ang, 13f / 32f);
                        Capa(sb, Tex("FlareEstrella4Puntas"), origen, marfil, t * 3f,
                            (110f + 20f * MathF.Sin(t * 14f)) / 256f);
                        Capa(sb, Tex("GlowOrb"), punta, oro * 0.5f, ang, 15f / 32f);
                        Capa(sb, Tex("SparkleStar"), punta, Color.White * 0.7f, t * 2f, 0.9375f);
                        sb.End();
                        break;
                    }

                    // ====================================================
                    //  ACTO III — EL JUICIO (la nova doble y el charco).
                    // ====================================================
                    default:
                    {
                        float vida = T / 90f;

                        // LA NOVA DE PÍXELES (el acto entero del motor).
                        if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                        {
                            PixelFxPlayer motor = MotorNova();
                            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                                SamplerState.PointClamp, DepthStencilState.None,
                                RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                            motor.Draw(sb, Projectile.Center - Main.screenPosition
                                - new Vector2(motor.Meta.Ox, motor.Meta.Oy) * motor.Escala);
                            sb.End();
                        }

                        sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                            SamplerState.LinearClamp, DepthStencilState.None,
                            RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                        Vector2 centro = Projectile.Center - Main.screenPosition;

                        // LA FLOR DE FUEGO — la nova de PyraLib con la rampa
                        // de la paleta del Juicio (blanco→oro→naranja→rojo).
                        Color[] rampa =
                        {
                            new Color(255, 250, 228),
                            new Color(255, 214, 118),
                            new Color(255, 152, 62),
                            new Color(196, 66, 30),
                        };
                        PyraLib.Estallido(sb, centro, 200f,
                            MathF.Min(1f, vida * 2.2f), rampa, 999, t, 1f - vida * 0.5f);

                        // LA ONDA DE CHOQUE.
                        OndaLib.Pulse(sb, centro, MathF.Min(1f, vida * 1.6f),
                            80f + vida * 380f, new Color(255, 232, 160),
                            0.6f * (1f - vida), 999);

                        // EL ANILLO DEL RADIO VIVO (la frontera del juicio).
                        float radio = MathF.Min(320f, 40f + T * 14f);
                        VFXCore.RingQuad(centro, radio, vida * 2f,
                            VFXCore.TintAlfa(new Color(255, 226, 150), 0.5f * (1f - vida)));

                        // EL CHARCO QUE HIERVE (desde el tick 26: queda 5 s).
                        if (T > 26f)
                        {
                            float apagarse = MathF.Min(1f, (90f - T) / 30f);
                            Suelo(sb, Ruido(9, 3.4f), centro, naranja * (0.12f * apagarse),
                                t * 0.2f, 224f, 70f);
                            Suelo(sb, Tex("Procedural/FireRing"), centro,
                                oro * (0.2f * apagarse), -t * 0.6f, 230f, 76f);
                            // LAS BURBUJAS del charco.
                            for (int i = 0; i < 5; i++)
                            {
                                float azar = Azar(i);
                                float brota = (t * 40f + azar * 70f) % 70f;
                                Vector2 pos = new(centro.X + (azar * 2f - 1f) * 92f,
                                    centro.Y - brota + 18f);
                                Capa(sb, Tex("GlowOrb"), pos,
                                    marfil * (0.3f * (1f - brota / 70f) * apagarse),
                                    0f, 5f / 64f);
                            }
                        }

                        // LAS ALMAS — 4 destellos que vuelan del juicio AL
                        // PORTADOR por curvas Bézier dobles (el pago).
                        if (T > 14f && T < 70f)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                float azar = Azar(i + 20);
                                float vuela = (t * 0.5f + azar) % 1f;
                                Vector2 cielo = centro
                                    + new Vector2((azar - 0.5f) * 400f, -160f - azar * 120f);
                                Vector2 portador = dueno.MountedCenter - Main.screenPosition;
                                Vector2 a = Vector2.Lerp(centro, cielo, vuela);
                                Vector2 b = Vector2.Lerp(cielo, portador, vuela);
                                Vector2 alma = Vector2.Lerp(a, b, vuela);
                                Capa(sb, Tex("SparkleStar"), alma,
                                    marfil * (0.55f * (1f - vuela * 0.4f)),
                                    t * 4f + i, 13f / 32f);
                            }
                        }

                        // EL FLASH del nacimiento (el primer tercio).
                        if (vida < 0.35f)
                        {
                            float flash = 1f - vida / 0.35f;
                            Capa(sb, Tex("GlowOrb"), centro, Color.White * (0.9f * flash),
                                0f, (60f * flash + 20f) / 128f);
                        }
                        sb.End();
                        break;
                    }
                }
            }
            catch
            {
                VFXCore.CerrarLoteSiAbierto();
            }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        /// <summary>UNA FUNDA del rayo (el orbe estirado).</summary>
        private static void Rayo(SpriteBatch sb, Vector2 origen, float ang,
            float largo, float grueso, Color color)
        {
            Texture2D tex = Tex("GlowOrb");
            sb.Draw(tex, origen + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (largo * 0.5f),
                null, color, ang, Utils.Size(tex) * 0.5f,
                new Vector2(largo / tex.Width, grueso / tex.Height),
                SpriteEffects.None, 0f);
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
