using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Globals;

namespace AethonMod.Content.Projectiles.Sombras
{
    /// <summary>
    /// PAGINAARRANCADAPROJECTILE — v6.50.71 — ARMA NUEVA 3: LA PÁGINA ARRANCADA.
    ///
    /// EL MARCO QUE CIÑE: alrededor del enemigo se traza un MARCO de
    /// sombra —con SUS RENGLONES violeta, su MARGEN ROJO y el número de
    /// página en la esquina: una hoja de cuaderno ENORME— y cada TIEMBLA
    /// (un desgarro cada 14 t) la hoja SE CIÑE más: arranca un bocado,
    /// marca el papel con zigzag blanco en las esquinas y el VELO interior
    /// oscurece al reo (lo que queda dentro SE VA VOLVIENDO PÁRRAFO).
    /// Cuando la hoja ya no puede encogerse: la página se ARRANCA — a 1 HP
    /// LA DEVORACIÓN, estilo 13 (EL ARREBATO).
    ///
    /// 100% código: ni un sprite. Cero dependencias.
    /// </summary>
    public class PaginaArrancadaProjectile : ModProjectile
    {
        private const byte FASE_TRAMA = 0;
        private const byte FASE_CIÑE = 1;
        private const byte FASE_DISIPA = 2;

        private NPC Presa => Projectile.ai[0] <= 0 ? null : Main.npc[(int)Projectile.ai[0] - 1];

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            float t = Projectile.ai[2];
            NPC presa = Presa;
            byte fase = (byte)Projectile.ai[1];

            bool presaValida = presa != null && presa.active && presa.life > 0;
            if (fase != FASE_DISIPA && !presaValida)
            {
                Projectile.ai[1] = FASE_DISIPA;
                Projectile.ai[2] = 0;
                fase = FASE_DISIPA;
                t = 0;
            }

            switch (fase)
            {
                case FASE_TRAMA:
                {
                    // LA HOJA SE TRAZA alrededor del reo (el marco nace GRANDE)
                    if (presa != null && presaValida)
                    {
                        Projectile.Center = presa.Center;
                        if (t == 2f) Sonar(SoundID.Item74.WithPitchOffset(-0.42f).WithVolumeScale(0.7f), Projectile.Center);
                        if (t >= 24f) { Projectile.ai[1] = FASE_CIÑE; Projectile.ai[2] = 0f; }
                    }
                    break;
                }
                case FASE_CIÑE:
                {
                    if (presa != null && presa.active && presa.life > 0)
                    {
                        Projectile.Center = presa.Center;

                        // LA MARCA CONTINUA (cada 7 t — la hoja lo tiene agarrado)
                        if (t % 7f == 0f && FaucesGlobalNPC.EsJefe(presa))
                            FaucesGlobalNPC.Marcar(presa, 13);

                        // EL TIEMBLA: un arranque cada 14 t — el drain cae con el desgarro
                        if (Main.netMode != NetmodeID.MultiplayerClient && t > 6f && t % 14f == 8f)
                        {
                            NPC dueñoPool = FaucesGlobalNPC.DueñoDelPool(presa);
                            if (dueñoPool != null && dueñoPool.active && dueñoPool.life > 1)
                            {
                                float quitar = MathF.Max(dueñoPool.life * 0.04f, 50f);
                                if (dueñoPool.life - quitar <= 1f)
                                {
                                    // === 1 HP: LA MUERTE SE DETIENE → EL ARREBATO ===
                                    FaucesGlobalNPC.Iniciar(dueñoPool, 13);
                                    Projectile.ai[1] = FASE_DISIPA;
                                    Projectile.ai[2] = 0f;
                                    Sonar(SoundID.Item74.WithPitchOffset(0.5f).WithVolumeScale(0.85f), Projectile.Center);
                                    break;
                                }
                                dueñoPool.life -= (int)quitar;
                                dueñoPool.netUpdate = true;
                                Sonar(SoundID.Item74.WithPitchOffset(0.22f).WithVolumeScale(0.6f), Projectile.Center);
                                Sonar(SoundID.NPCHit9.WithPitchOffset(-0.15f).WithVolumeScale(0.45f), Projectile.Center);
                            }
                        }
                        // sin arreglo posible (la chusma muere por el roce de la hoja)
                        if (t > 700f) { Projectile.ai[1] = FASE_DISIPA; Projectile.ai[2] = 0f; }
                    }
                    else { Projectile.ai[1] = FASE_DISIPA; Projectile.ai[2] = 0f; }
                    break;
                }
                case FASE_DISIPA:
                {
                    if (t == 1f) Sonar(SoundID.Item122.WithPitchOffset(0.3f).WithVolumeScale(0.3f), Projectile.Center);
                    if (t >= 30f) Projectile.Kill();
                    break;
                }
            }

            if (Main.netMode != NetmodeID.Server && Projectile.ai[1] == FASE_TRAMA && Main.rand.NextBool(8))
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(40, 40), 80, 80,
                    DustID.Shadowflame, 0f, -0.4f, 128, default, 0.5f);
                Main.dust[d].noGravity = true;
            }
        }

        /// <summary>Las medidas de la hoja: la elipse real del reo (como EL
        /// DEVORADOR — la lección del Rey Slime a medias) por la escala del
        /// momento (nace 1.78× y cada tiembla ciñe hasta 1.03×).</summary>
        private void MedidasHoja(NPC presa, byte fase, float t, out float ancho, out float alto)
        {
            float rx = MathHelper.Clamp(presa.width * 0.62f, 66f, 300f);
            float ry = MathHelper.Clamp(presa.height * 0.62f, 66f, 320f);

            float escala;
            if (fase == FASE_TRAMA) escala = MathHelper.Lerp(2.05f, 1.78f, SombrasLib.DeGolpe(t / 24f));
            else if (fase == FASE_CIÑE)
            {
                int tiemblas = Math.Min((int)(t / 14f), 5);
                escala = MathHelper.Lerp(1.78f, 1.03f, tiemblas / 5f);
                // EL RESORTE DEL TIEMBLA: la hoja REBOTA al arrancar
                float m = t % 14f - 8f;
                if (m >= 0f && m < 6f) escala *= 1f - 0.055f * MathF.Sin(m / 6f * MathF.PI);
            }
            else escala = 1.03f;

            ancho = rx * escala;
            alto = ry * escala;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // LA HOJA MUERDE lo que TOCA el marco: el rectángulo interior
            NPC presa = Presa;
            byte fase = (byte)Projectile.ai[1];
            if (presa == null || !presa.active || fase == FASE_TRAMA) return false;
            MedidasHoja(presa, fase, Projectile.ai[2], out float ancho, out float alto);
            Rectangle hoja = new((int)(Projectile.Center.X - ancho), (int)(Projectile.Center.Y - alto),
                (int)(ancho * 2f), (int)(alto * 2f));
            return targetHitbox.Intersects(hoja);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            NPC presa = Presa;
            if (presa == null || !presa.active) return false;

            VFXCore.CerrarLoteSiAbierto();
            try { DrawTodo(presa); }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }

        private void DrawTodo(NPC presa)
        {
            float tiempo = Main.GlobalTimeWrappedHourly;
            int semilla = Projectile.whoAmI * 29 + 7;
            byte fase = (byte)Projectile.ai[1];
            float t = Projectile.ai[2];
            float disipa = fase == FASE_DISIPA ? MathHelper.Clamp(1f - t / 30f, 0f, 1f) : 1f;

            MedidasHoja(presa, fase, t, out float ancho, out float alto);
            Vector2 c = Projectile.Center;
            Vector2 a = new(c.X - ancho, c.Y - alto);   // esquina sup-izq
            Vector2 b = new(c.X + ancho, c.Y + alto);   // esquina inf-der
            float traza = fase == FASE_TRAMA ? SombrasLib.DeGolpe(t / 24f) : 1f;

            // === EL VELO INTERIOR — lo que queda dentro SE VA VOLVIENDO
            // PÁRRAFO (oscurece con cada tiembla: el reo se imprime) ===
            int tiemblasHechos = fase == FASE_TRAMA ? 0 : Math.Min((int)(fase == FASE_CIÑE ? t / 14f : 5), 5);
            VFXCore.Begin();
            VFXCore.Quad(c, SombrasLib.Alfa(SombrasLib.Negro, (0.18f + 0.13f * tiemblasHechos) * disipa),
                new Vector2(ancho * 2f * traza, alto * 2f * traza), 0f, VFXCore.GlowOrb);
            VFXCore.FlushAlpha();

            // === LOS RENGLONES — la hoja de cuaderno (líneas violeta tenues
            // que cruzan el marco; con el velo encima, el reo cae AL PAPEL) ===
            VFXCore.Begin();
            int renglones = 6;
            for (int k = 1; k <= renglones; k++)
            {
                float f = k / (float)(renglones + 1);
                float y = MathHelper.Lerp(a.Y, b.Y, f);
                VFXCore.Line(new Vector2(a.X + 8f, y), new Vector2(b.X - 8f, y),
                    SombrasLib.Alfa(SombrasLib.Violeta, 0.11f * traza * disipa), 2f);
            }
            // EL MARGEN ROJO — la línea vertical del cuaderno
            float xM = MathHelper.Lerp(a.X, b.X, 0.24f);
            VFXCore.Line(new Vector2(xM, a.Y + 6f), new Vector2(xM, b.Y - 6f),
                SombrasLib.Alfa(SombrasLib.Rojo, 0.15f * traza * disipa), 2.5f);
            VFXCore.FlushAdditive();

            // === EL MARCO — cuatro barras gruesas de sombra + rim violeta ===
            VFXCore.Begin();
            Vector2[] esquinas = { new(a.X, a.Y), new(b.X, a.Y), new(b.X, b.Y), new(a.X, b.Y) };
            for (int k = 0; k < 4; k++)
            {
                Vector2 p0 = esquinas[k];
                Vector2 p1 = esquinas[(k + 1) % 4];
                // el trazo nace de la esquina sup-izq y SE DIBUJA alrededor
                // (la pluma que traza la hoja — TRAMA)
                float fK = MathHelper.Clamp(traza * 4f - k, 0f, 1f);
                if (fK <= 0f) break;
                Vector2 meta = Vector2.Lerp(p0, p1, fK);
                VFXCore.Line(p0, meta, SombrasLib.Alfa(SombrasLib.Negro, 0.95f * disipa), 20f);
                VFXCore.Line(p0, meta, SombrasLib.Alfa(SombrasLib.Violeta, 0.15f * disipa), 28f);
            }
            VFXCore.FlushAlpha(VFXCore.Pixel);

            // === LAS ESQUINAS — el número de página y sus sellos rúnicos ===
            VFXCore.Begin();
            for (int k = 0; k < 4; k++)
            {
                float sello = MathHelper.Clamp(traza * 4f - k, 0f, 1f);
                if (sello <= 0f) break;
                Vector2 e = esquinas[k];
                VFXCore.Quad(e, SombrasLib.Alfa(SombrasLib.Violeta, 0.4f * sello * disipa), new Vector2(34f, 34f));
                VFXCore.Quad(e, SombrasLib.Alfa(SombrasLib.Blanco, 0.55f * sello * disipa),
                    VFXCore.RingQuadSize(15f), 0f, VFXCore.Ring);
            }
            // el folio (abajo-derecha): la página que se está escribiendo
            VFXCore.Quad(new Vector2(b.X - 20f, b.Y + 14f), SombrasLib.Alfa(SombrasLib.Blanco, 0.5f * traza * disipa),
                new Vector2(6f, 10f));
            VFXCore.FlushAdditive();

            // === EL TIEMBLA — el zigzag blanco del desgarro en las esquinas
            // (arranca, marca el papel, rebota) ===
            if (fase == FASE_CIÑE)
            {
                float m = t % 14f - 8f;
                if (m >= 0f && m < 8f)
                {
                    float fuerza = MathF.Sin(m / 8f * MathF.PI) * disipa;
                    VFXCore.Begin();
                    for (int k = 0; k < 4; k++)
                    {
                        Vector2 e = esquinas[k];
                        Vector2 adentro = (c - e).SafeNormalize(Vector2.UnitX) * 26f;
                        // dos colmillitos de hueso por esquina: el papel RASGADO
                        for (int j = -1; j <= 1; j += 2)
                        {
                            Vector2 lado = new Vector2(-adentro.Y, adentro.X) * (j * 0.7f);
                            VFXCore.Line(e + adentro * 0.4f, e + adentro + lado,
                                SombrasLib.Alfa(SombrasLib.Blanco, 0.85f * fuerza), 3f);
                        }
                    }
                    VFXCore.FlushAdditive(VFXCore.Pixel);
                    SombrasLib.OndaChoque(c, 30f + 60f * (m / 8f), 0.35f * fuerza, roja: false);
                }
            }

            // === LA BRUMA DEL BORDE — la hoja EXHALA por sus cuatro lados ===
            if (traza > 0.9f)
            {
                Vector2[] perim = new Vector2[16];
                for (int k = 0; k < 16; k++)
                {
                    float f = k / 16f * 4f;
                    int ladoP = (int)f;
                    float fl = Frac(f);
                    Vector2 p0 = esquinas[ladoP % 4];
                    Vector2 p1 = esquinas[(ladoP + 1) % 4];
                    perim[k] = Vector2.Lerp(p0, p1, fl);
                }
                SombrasLib.BrumaColumna(perim, 22f, 0.38f * disipa, tiempo, semilla, 10);
            }

            // === EL ARREBATO (DISIPA): la hoja se ARRANCA — vuela al
            // portador encogiéndose, dejando un VODO blanco que se llena
            // de bruma donde estaba el reo ===
            if (fase == FASE_DISIPA)
            {
                Player portador = Main.player[Projectile.owner];
                float f = MathHelper.Clamp(t / 30f, 0f, 1f);
                Vector2 posHoja = Vector2.Lerp(c, portador.MountedCenter, SombrasLib.DeGolpe(f));
                float encoge = 1f - 0.8f * f;

                // el VOODO — el hueco blanco que deja la página arrancada
                if (t < 14f)
                {
                    VFXCore.Begin();
                    VFXCore.Quad(c, SombrasLib.Alfa(SombrasLib.Blanco, 0.5f * (1f - t / 14f) * disipa),
                        new Vector2(ancho * 2f, alto * 2f), 0.06f * t, VFXCore.Pixel);
                    VFXCore.FlushAlpha(VFXCore.Pixel);
                }

                // la hoja que vuela (marco reducido, girando un pelo)
                VFXCore.Begin();
                Vector2 mm = new(ancho * encoge, alto * encoge);
                Vector2 vA = posHoja - mm;
                Vector2 vB = posHoja + mm;
                Vector2[] vEsq = { new(vA.X, vA.Y), new(vB.X, vA.Y), new(vB.X, vB.Y), new(vA.X, vB.Y) };
                for (int k = 0; k < 4; k++)
                {
                    VFXCore.Line(vEsq[k], vEsq[(k + 1) % 4], SombrasLib.Alfa(SombrasLib.Negro, 0.9f * (1f - f)), 14f);
                }
                VFXCore.FlushAlpha(VFXCore.Pixel);
                SombrasLib.Bruma(posHoja, 40f * encoge, 0.5f * (1f - f), tiempo, semilla + 19,
                    (portador.MountedCenter - posHoja) * 0.02f);
            }
        }

        private static float Frac(float x) => x - MathF.Floor(x);

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
