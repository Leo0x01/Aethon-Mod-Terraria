using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Investigaciones
{
    // ======================================================================
    //  FaroDelEclipseHaz — v6.174 → v6.177 — EL ATAQUE REDISEÑADO DE RAÍZ.
    //
    //  LA LETRA (sesión 30): «el faro del eclipse, bueno lo que tenias
    //  que rediseñar era su ataque o proyectil, asi que rediseñalo».
    //  (La v6.174 hizo un conducto continuo; la v6.176 la TORRE — el
    //  ataque seguía siendo el conducto de siempre.)
    //
    //  EL ATAQUE NUEVO — EL ECLIPSE QUE BARRE (el faro DE VERDAD):
    //   1. EL DISPARO: el faro lanza EL DISCO NEGRO (la luna del
    //      eclipse) hacia donde apuntas — vuela con su estela y golpea.
    //   2. EL NACIMIENTO: al llegar (o a los 300 px) se CLAVA en el
    //      aire, se ABRE y nace EL ECLIPSE: el disco negro rodeado por
    //      su CORONA PICADA (los picos de ámbar de la torre) — un
    //      eclipse de 3,5 s plantado en el cielo.
    //   3. EL BARRIDO DEL FARO: del centro del eclipse nace EL HAZ DEL
    //      FARO que GIRA (una vuelta cada ~1,2 s) barriendo TODO a su
    //      paso — el faro de verdad: su luz gira y golpea.
    //   4. LA CORONA VIVA: cada pico de la corona SE ENCIENDE cuando
    //      el haz lo cruza (la picada del eclipse late al ritmo del
    //      barrido) + las chispas orbitales.
    //   5. EL CIERRE: el eclipse IMPLOTA (el disco se traga su corona)
    //      y deja un destello final.
    //
    //  TODO con las librerías de la casa: el haz giratorio es LA PILA
    //  DE CAPAS (RayoStrip.Filamento ×4 sobre LA MISMA polilínea — la
    //  ley v6.56.9), la corona picada es VFXCore.Quad con perfil
    //  determinista (Hash01 — R22) y el disco negro vive en el lote
    //  ALPHA (la ley del negro: aditivo suma cero — para oscurecer se
    //  sale del aditivo).
    // ======================================================================
    public class FaroDelEclipseHaz : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        // === LA LÍNEA DE TIEMPO (60 t = 1 s) ===
        private const int Vuelo = 26;          // el disco vuela…
        private const float Distancia = 340f;   // …hasta ~340 px del faro
        private const int VidaEclipse = 210;    // el eclipse vive 3,5 s
        private const float LargoHaz = 640f;    // el haz del barrido
        private const float Giro = 0.088f;      // rad/t (~1 vuelta cada 1,2 s)

        // === LA PALETA DE LA TORRE (la v6.176) ===
        private static readonly Color Corona = new(255, 196, 96);
        private static readonly Color CoronaViva = new(255, 232, 170);
        private static readonly Color Nucleo = new(255, 250, 236);
        private static readonly Color Tinta = new(148, 72, 178);
        private static readonly Color Disco = new(14, 8, 22);

        private bool _clavado;
        private float _hastaElMuro;          // v6.178: LA PUNTA contra el mundo (para el choque)

        public override void SetStaticDefaults() { ProjectileID.Sets.DrawScreenCheckFluff[Type] = 900; }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = Vuelo + VidaEclipse + 40;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => !_clavado;

        public override void AI()
        {
            // LA FASE 1 — EL VUELO DEL DISCO: hacia donde se apuntó
            if (!_clavado)
            {
                Projectile.ai[0]++;
                if (Projectile.ai[0] >= Vuelo ||
                    Projectile.Center.DistanceSQ(Main.player[Projectile.owner].Center) > Distancia * Distancia)
                {
                    // EL NACIMIENTO DEL ECLIPSE: el disco se CLAVA
                    _clavado = true;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.ai[0] = 0;
                    Projectile.netUpdate = true;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item157.WithPitchOffset(-0.3f), Projectile.Center);
                    if (Main.netMode != NetmodeID.Server && !Main.dedServ)
                    {
                        for (int i = 0; i < 18; i++)
                        {
                            Dust d = Dust.NewDustPerfect(Projectile.Center, 90,
                                Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 8f),
                                0, default, 1.6f);
                            d.noGravity = true;
                        }
                    }
                }
                // el disco viaja con SU cometa (la estela de ámbar)
                if (Main.netMode != NetmodeID.Server && !Main.dedServ && Projectile.ai[0] % 3 == 0)
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, 269,
                        -Projectile.velocity * 0.06f, 0, new Color(255, 196, 96), 1.2f);
                    d.noGravity = true;
                }
                return;
            }

            // LA FASE 2 — EL ECLIPSE QUE BARRE
            Projectile.ai[0]++;
            int t = (int)Projectile.ai[0];
            if (t > VidaEclipse + 24)
            {
                // EL CIERRE: implosión + destello final
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item122.WithPitchOffset(0.4f), Projectile.Center);
                SombrasLib.PurgaVisualMuerte(Projectile.Center, 160f);
                Projectile.Kill();
                return;
            }
            // EL HAZ GIRA (el barrido del faro)
            Projectile.ai[2] += Giro;
            if (Projectile.ai[2] > MathHelper.TwoPi) Projectile.ai[2] -= MathHelper.TwoPi;

            // v6.178 — LA PUNTA CONTRA EL MUNDO (el escáner de vanilla,
            // el mismo del rayo continuo): ¿hasta dónde llega el barrido
            // antes de pegar contra el muro? — para el CHOQUE de punta.
            if (_clavado && t % 6 == 0)
            {
                float angTmp = Projectile.ai[2];
                Vector2 dirTmp = new((float)Math.Cos(angTmp), (float)Math.Sin(angTmp));
                float[] pasos = new float[3];
                Collision.LaserScan(Projectile.Center, dirTmp, 10f, LargoHaz, pasos);
                _hastaElMuro = (pasos[0] + pasos[1] + pasos[2]) / 3f;
            }

            // LA LUZ del haz (el pase de vanilla por la línea del barrido)
            float ang = Projectile.ai[2];
            Vector2 dir = new((float)Math.Cos(ang), (float)Math.Sin(ang));
            DelegateMethods.v3_1 = new Vector3(1.2f, 0.85f, 0.35f) * Intensidad();
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + dir * LargoHaz, 130f,
                DelegateMethods.CastLight);

            // EL TIC-TAC del faro: cada vuelta entera suena su campanada
            if (t > 20 && t % 74 == 0)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item15.WithPitchOffset(-0.7f).WithVolumeScale(0.6f),
                    Projectile.Center);

            // LAS CHISPAS ORBITALES (deterministas: R22)
            if (Main.netMode != NetmodeID.Server && !Main.dedServ && t % 4 == 0)
            {
                float fa = Main.GlobalTimeWrappedHourly * 1.8f + Projectile.whoAmI;
                Vector2 pos = Projectile.Center + new Vector2((float)Math.Cos(fa), (float)Math.Sin(fa) * 0.8f) * (46f + 12f * (float)Math.Sin(fa * 2f));
                Dust d = Dust.NewDustPerfect(pos, 269, Vector2.Zero, 0, Corona, 1.1f);
                d.noGravity = true;
            }

            // EL DISCO se hunde SUAVE mientras vive (la luna que se cierra)
            if (t > VidaEclipse)
                Projectile.scale = MathHelper.Lerp(1f, 0.05f, (t - VidaEclipse) / 24f);
            Projectile.netUpdate = t % 30 == 0;
        }

        private float Intensidad()
        {
            int t = (int)Projectile.ai[0];
            if (!_clavado) return 0f;
            // nace en 12 t… y se apaga en los últimos 20
            return MathHelper.Clamp(t / 12f, 0f, 1f) * MathHelper.Clamp((VidaEclipse + 8 - t) / 20f, 0f, 1f);
        }

        /// <summary>EL GOLPE: el disco que vuela + la LÍNEA del barrido.</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 c = targetHitbox.Center.ToVector2();
            if (!_clavado)
                return Vector2.Distance(c, Projectile.Center) < 24f + targetHitbox.Width * 0.3f;
            if (Intensidad() < 0.3f) return false;
            float ang = Projectile.ai[2];
            Vector2 dir = new((float)Math.Cos(ang), (float)Math.Sin(ang));
            Vector2 ab = dir * LargoHaz;
            float l2 = ab.LengthSquared();
            float tproj = l2 > 0.0001f ? MathHelper.Clamp(Vector2.Dot(c - Projectile.Center, ab) / l2, 0f, 1f) : 0f;
            float dist = Vector2.Distance(c, Projectile.Center + ab * tproj);
            return dist < 30f + Math.Max(targetHitbox.Width, targetHitbox.Height) * 0.35f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.dedServ) return;
            // LA CHISPA del cruce: el haz te marca
            for (int i = 0; i < 6; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center, 269,
                    Main.rand.NextVector2Circular(5f, 5f), 0, Corona, 1.4f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            float t = Main.GlobalTimeWrappedHourly;
            Vector2 centro = Projectile.Center - Main.screenPosition;
            float inten = _clavado ? Intensidad() : 1f;

            // ═══ (1) EL DISCO NEGRO — en el lote ALPHA (la ley del negro) ═══
            VFXCore.CerrarLoteSiAbierto();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise,
                null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                float rDisco = (_clavado ? 42f : 13f) * Projectile.scale;
                Main.spriteBatch.Draw(VFXCore.SoftGlow, centro, null,
                    Disco * (0.94f * Math.Max(0.35f, inten)), 0f, VFXCore.SoftGlow.Size() * 0.5f,
                    new Vector2(rDisco * 2f / 128f, rDisco * 2f / 128f), SpriteEffects.None, 0f);
                // EL FILDETE violeta del disco (el borde que ARDE por dentro)
                Main.spriteBatch.Draw(VFXCore.SoftGlow, centro, null,
                    Tinta * (0.34f * inten), 0f, VFXCore.SoftGlow.Size() * 0.5f,
                    new Vector2((rDisco + 7f) * 2f / 128f, (rDisco + 7f) * 2f / 128f), SpriteEffects.None, 0f);
            }
            finally { Main.spriteBatch.End(); }

            VFXCore.AbrirAdditive();
            try
            {
                // ═══ (2) LA CORONA PICADA — los 22 picos deterministas (R22)
                //     v6.178 — LA CURA DEL FANTASMA: los picos dibujaban con
                //     VFXCore.Quad (que solo ACUMULA al buffer — sin Flush
                //     NUNCA se veían: la corona del eclipse era INVISIBLE
                //     desde la v6.177, la mitad del faro faltante). Ahora
                //     dibujan DIRECTO al lote aditivo abierto. ═══
                if (_clavado)
                {
                    int picos = 22;
                    float angHaz = Projectile.ai[2];
                    for (int i = 0; i < picos; i++)
                    {
                        float a = i * MathHelper.TwoPi / picos +
                                  0.35f * VFXCore.Hash01(Projectile.whoAmI, i, 71);
                        float largo = (16f + 30f * VFXCore.Hash01(Projectile.whoAmI, i, 73)) *
                                      (0.85f + 0.3f * MathF.Sin(t * 2.2f + i * 1.7f));
                        // EL PICO se ENCIENDE cuando el haz lo CRUZA (la picada viva)
                        float cruce = MathHelper.Clamp(1f -
                            Math.Abs(MathHelper.WrapAngle(a - angHaz)) / 0.45f, 0f, 1f);
                        Color pico = Color.Lerp(Corona * 0.55f, CoronaViva, cruce);
                        float alpha = (0.45f + 0.55f * cruce) * inten;
                        Vector2 posPico = centro + new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) *
                                          (48f + largo * 0.5f);
                        Main.spriteBatch.Draw(VFXCore.SoftGlow, posPico, null,
                            pico * alpha, a, VFXCore.SoftGlow.Size() * 0.5f,
                            new Vector2(largo / 64f, (5f + 3f * VFXCore.Hash01(Projectile.whoAmI, i, 77)) / 64f),
                            SpriteEffects.None, 0f);
                    }
                }

                // ═══ (3) EL HAZ GIRATORIO — v6.178: LA TÉCNICA DEL RAYO
                //     DE VERDAD (la letra: «el ataque del faro tambien se
                //     ve horrible, parece que le faltan VFX y muchos VFX»):
                //     las SEIS CAPAS del plano del arma real + LA ESTELA
                //     DEL BARRIDO + LA CAMPANADA + EL CHOQUE DE PUNTA —
                //     el faro de verdad, con MUCHOS VFX. ═══
                if (_clavado && Intensidad() > 0.05f)
                {
                    float ang = Projectile.ai[2];
                    Vector2 dir = new((float)Math.Cos(ang), (float)Math.Sin(ang));
                    float largoVivo = _hastaElMuro > 4f ? MathF.Min(LargoHaz, _hastaElMuro) : LargoHaz;
                    bool choca = _hastaElMuro > 4f && _hastaElMuro < LargoHaz - 6f;

                    // (3a) LA ESTELA DEL BARRIDO — los fantasmas del haz en
                    //      los ángulos que YA barrió (el rastro de luz que
                    //      persigue al faro: el giro SE LEE)
                    for (int g = 1; g <= 5; g++)
                    {
                        float aG = ang - g * 0.14f * Math.Sign(Giro);
                        Vector2 dirG = new((float)Math.Cos(aG), (float)Math.Sin(aG));
                        float fG = (1f - g * 0.16f) * inten * 0.55f;
                        if (fG <= 0.03f) continue;
                        Main.spriteBatch.Draw(VFXCore.SoftGlow,
                            centro + dirG * (largoVivo * 0.5f), null,
                            Corona * (fG * 0.24f), aG, VFXCore.SoftGlow.Size() * 0.5f,
                            new Vector2(largoVivo / 64f * 0.96f, 0.55f + 0.5f * (1f - g * 0.16f)),
                            SpriteEffects.None, 0f);
                    }

                    // (3b) EL HAZ EN SÍ — la técnica de las seis capas (la
                    //      del arma real — HazProyectadoLib, la casa)
                    if (HazProyectadoLib.TexturasVivas)
                    {
                        HazProyectadoLib.Parametros p = default;
                        p.Inicio = centro;
                        p.Direccion = dir;
                        p.Largo = largoVivo;
                        p.Ancho = 66f;
                        p.Tiempo = t * 1.35f + Projectile.whoAmI;
                        p.Intensidad = inten;
                        p.Destello = 0.55f * inten;
                        p.Impacto = choca;
                        p.VelocidadFlujo = 14f;
                        p.Ondulacion = 0.7f;
                        p.Turbulencia = 0.6f;
                        p.Colores = new HazProyectadoLib.Paleta
                        { Exterior = Corona * 0.8f, Medio = Corona, Nucleo = CoronaViva };
                        HazProyectadoLib.Dibujar(p);
                    }
                    else
                    {
                        // EL FALLBACK de la pila de siempre
                        int nodos = 16;
                        Vector2[] pts = new Vector2[nodos];
                        for (int i = 0; i < nodos; i++)
                        {
                            float f = i / (float)(nodos - 1);
                            pts[i] = centro + dir * (largoVivo * f) +
                                     new Vector2(-dir.Y, dir.X) *
                                     (MathF.Sin(f * 9.4f + t * 3.1f + Projectile.whoAmI) * 4.2f *
                                      MathHelper.Clamp(f * 3f, 0f, 1f));
                        }
                        RayoStrip.FilamentoEnLote(pts, 66f, Corona * 0.45f, Corona * 0.25f,
                            0.26f * inten, StormTaper.Center, 0, 0, true, false, false);
                        RayoStrip.FilamentoEnLote(pts, 34f, Corona, Corona * 0.6f,
                            0.5f * inten, StormTaper.Center, 0, 0, true, false, false);
                        RayoStrip.FilamentoEnLote(pts, 18f, CoronaViva, Corona,
                            0.8f * inten, StormTaper.Center, 0, 0, true, false, false);
                        RayoStrip.FilamentoEnLote(pts, 7f, Nucleo, CoronaViva,
                            inten, StormTaper.Center, 0, 0, true, false, false);
                    }

                    // (3c) EL CHOQUE DE PUNTA: cuando el barrido pega contra
                    //      el mundo — la punta ARDE con su doble estallido
                    Vector2 punta = centro + dir * largoVivo;
                    if (choca)
                    {
                        float late = 1f + 0.22f * MathF.Sin(t * 11.3f);
                        Main.spriteBatch.Draw(VFXCore.SoftGlow, punta, null,
                            CoronaViva * (0.75f * inten), 0f, VFXCore.SoftGlow.Size() * 0.5f,
                            new Vector2(2.1f * late, 2.1f * late), SpriteEffects.None, 0f);
                        Main.spriteBatch.Draw(VFXCore.SoftGlow, punta, null,
                            Nucleo * (0.6f * inten), 0f, VFXCore.SoftGlow.Size() * 0.5f,
                            new Vector2(0.9f * late, 0.9f * late), SpriteEffects.None, 0f);
                        // las chispas del muro (deterministas: R22)
                        if ((int)Main.GameUpdateCount % 5 == 0)
                            for (int s = 0; s < 3; s++)
                            {
                                float aS = VFXCore.Hash01(Projectile.whoAmI, s, (int)(Main.GameUpdateCount / 5));
                                Vector2 vS = (-dir).RotatedBy((aS - 0.5f) * 2.4f) * (1.5f + 3f * aS);
                                Dust dS = Dust.NewDustPerfect(punta + dir * 6f, 269,
                                    vS, 0, Corona, 1.3f);
                                dS.noGravity = true;
                            }
                    }
                }

                // ═══ (4) LA CAMPANADA DE CADA VUELTA — el anillo que nace
                //     del centro cuando el haz pasa por arriba (el pulso
                //     del faro: cada vuelta se VE, no solo se oye) ═══
                if (_clavado && Intensidad() > 0.05f)
                {
                    float fase = Projectile.ai[2] / MathHelper.TwoPi;
                    float anillo = fase - MathF.Floor(fase);
                    if (anillo < 0.55f)
                    {
                        float progreso = anillo / 0.55f;
                        float r = 52f + progreso * 150f;
                        float a = (1f - progreso) * (1f - progreso) * 0.55f * inten;
                        VFXCore.RingQuad(centro, r, 0f, Corona * a);
                        VFXCore.RingQuad(centro, r * 0.72f, 0f, CoronaViva * (a * 0.5f));
                    }
                }

                // ═══ (4) LA BOCA DEL ECLIPSE — el corazón del disco ═══
                Main.spriteBatch.Draw(VFXCore.SoftGlow, centro, null,
                    Corona * (0.36f * inten), 0f, VFXCore.SoftGlow.Size() * 0.5f,
                    new Vector2(1.25f, 1.25f), SpriteEffects.None, 0f);
                if (!_clavado)
                {
                    // EL COMETA del vuelo (la luna con su cola)
                    Vector2 cola = centro - Projectile.velocity * 2.4f;
                    Main.spriteBatch.Draw(VFXCore.SoftGlow, cola, null,
                        Corona * 0.4f, 0f, VFXCore.SoftGlow.Size() * 0.5f,
                        new Vector2(0.8f, 0.8f), SpriteEffects.None, 0f);
                }
            }
            finally { VFXCore.RestaurarLoteVanilla(); }
            return false;
        }
    }
}
