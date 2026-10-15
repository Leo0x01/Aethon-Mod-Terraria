using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Cosmetic
{
    /// <summary>
    /// GrimorioMenorPet — v6.50.122 — LA SEGUNDA MASCOTA: EL GRIMORIO
    /// DEL ETERNO EN MINIATURA (la letra del usuario que desenredó los
    /// conceptos: «la mascota normal es una pequeña representación del
    /// grimorio del eterno… este sí debe tener los mini minions, toda
    /// la mecánica de molestar npc y etc»).
    ///
    /// NO ES la criatura de luz: el Aethon Menor es el JEFE en
    /// miniatura y su repertorio es ALUMBRAR, ESTALLAR y FUSIONARSE con
    /// su gemela oscura. ESTE es el LIBRO en miniatura — y su repertorio
    /// es el del tomo hambriento: vigila, se aburre, viaja al pueblo y
    /// MOLESTA a los vecinos con sus 3 MINI OJOS (v6.50.125: LOS
    /// MISMOS OJOS DEL GRIMORIO NORMAL EN MINIATURA — el sprite real del
    /// Ojo Eterno del arma a escala 0,27, con su iris que mira y sus
    /// párpados de textura; la letra: «los mismos minion que el grimorio
    /// normal, o sea ojos, pero mas pequeños») y su SOMBRILLA DE PÁGINA
    /// que muerde (1 de daño sin acreditación, tope 10 golpes, emotes,
    /// aburrimiento, el AFK del dueño). TODA esa máquina vive en
    /// ParejaCerebro — el cerebro que nació en la .117 montado en la
    /// cría de luz y que la .122 devolvió a su dueño verdadero: el libro
    /// (véase AethonMenorPareja.cs).
    ///
    /// EL CUERPO: el sprite REAL del Eterno (la familia del ojo: el
    /// mismo cuerpo del Nervioso) a escala 0.52, con su OJO VIVO — mira
    /// a su dueño, parpadea a su compás y durante las misiones CLAVA la
    /// mirada en su víctima (el contrato del GrimorioNerviosoFlotante,
    /// en miniatura) — más su ANILLO RÚNICO OSCURO violeta y el
    /// polvillo de sombra. NO es mascota de luz: su brillo es la brasa
    /// tenue de sus páginas, no una lámpara.
    ///
    /// LA VIDA: el patrón vanilla de toda mascota (la lección .121):
    /// timeLeft = 2 A CADA TICK — la muerte la decide SOLO el buff
    /// (GrimorioMenorBuff), jamás el reloj.
    /// </summary>
    public class GrimorioMenorPet : ModProjectile
    {
        // === EL CEREBRO (la máquina de travesuras — ParejaCerebro) ===
        private readonly ParejaCerebro.Estado _cerebro = new ParejaCerebro.Estado();

        public override string Texture => "AethonMod/Content/Weapons/GrimorioHambrientoNervioso";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            Main.projPet[Type] = true;                    // mascota: persiste
            ProjectileID.Sets.NeedsUUID[Type] = true;     // la instancia única del dueño
            // SIN ProjectileID.Sets.LightPet — NO es mascota de luz (ese
            // trono es del Aethon Menor): el libro es la sombra que
            // acompaña, no la lámpara que guía.
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 28;
            Projectile.friendly = true;         // las mascotas son friendly (no daña: damage 0)
            Projectile.damage = 0;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;     // la sombra atraviesa el mundo
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;     // tML la sincroniza
        }

        public override void AI()
        {
            // === LA VIDA DE LA MASCOTA (el patrón vanilla): el buff manda ===
            Player duenio = Main.player[Projectile.owner];
            if (duenio == null || !duenio.active || duenio.dead ||
                !duenio.HasBuff(ModContent.BuffType<global::AethonMod.Content.Buffs.GrimorioMenorBuff>()))
            {
                Projectile.Kill();
                return;
            }

            // LA CURA DE LA .121 (el patrón vanilla de TODA mascota: Wisp,
            // Corazón Carmesí, Fuego Fatuo): el reloj se refresca A CADA
            // TICK — la muerte la decide SOLO el buff, jamás el timeLeft.
            Projectile.timeLeft = 2;

            // v6.50.124 — LA EDAD LOCAL (el teatro del DESPERTAR: los 3
            // mini minions BROTAN de las páginas en los primeros 36 t —
            // la leen PosicionOjos/DibujarExtras; localAI no viaja en el
            // cable y no hace falta: cada pantalla florece la suya).
            Projectile.localAI[0] += 1f;

            float t = Main.GlobalTimeWrappedHourly;

            // === EL CEREBRO: el AFK del dueño, el viaje al pueblo, LAS DOS
            //     FORMAS de molestar y el tope de 10 golpes (ParejaCerebro
            //     — TODO determinista, el contrato de la casa: el servidor
            //     y todos los clientes evolucionan IGUAL). ===
            ParejaCerebro.Tick(Projectile, duenio, _cerebro);

            // === LA PERSECUCIÓN SUAVE: el hombro del dueño — el LADO
            //     ESPEJO del Aethon Menor (si llevas las dos mascotas, la
            //     cría de luz pasea un hombro y el libro el otro: cada
            //     quien su sitio). ===
            float lado = MathF.Sin(t * 0.35f + MathHelper.Pi) * 40f + duenio.direction * 22f;
            float bob = MathF.Sin(t * 1.75f + 0.8f) * 8f;
            Vector2 meta = duenio.Center + new Vector2(lado, -38f + bob);
            // Si está de misión (viajar / molestar / volver), la meta la
            // decide EL CEREBRO: el vecino, su órbita, o el lado del dueño.
            ParejaCerebro.ElegirMeta(Projectile, duenio, _cerebro, ref meta);

            // La búsqueda serena… y LA MISIÓN VUELA: en viaje/molestia/
            // vuelta la persecución es DIRECTA — crucero ~12 px/t (el
            // mismo muelle dual de la .117).
            Vector2 hacia = meta - Projectile.Center;
            float dist = hacia.Length();
            if (_cerebro.Modo == ParejaCerebro.MODO_NORMAL)
            {
                if (dist > 900f)            // el dueño se fue muy lejos: le alcanza volando
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, hacia * 0.05f, 0.20f);
                else
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, hacia * 0.045f, 0.10f);

                // El freno al llegar (flota, no vibra encima del punto).
                if (dist < 24f)
                    Projectile.velocity *= 0.80f;
            }
            else
            {
                Vector2 dir = dist > 0.001f ? hacia / dist : Vector2.Zero;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, dir * 12f, 0.10f);
                if (dist < 30f)
                    Projectile.velocity *= 0.85f;
            }

            // El libro se mece como un péndulo perezoso (la rotación sutil
            // del Grimorio Dormido — es SU familia).
            Projectile.rotation = MathF.Sin(t * 1.2f) * 0.07f;

            // === EL RESPLANDOR DE LAS PÁGINAS: una brasa violeta, TENUE
            //     (0.24/0.17/0.40 — menos de la quinta parte de la cría de
            //     luz): el libro NO es una lámpara; es el fulgor inquieto
            //     de sus páginas hambrientas. ===
            Lighting.AddLight(Projectile.Center, 0.24f, 0.17f, 0.40f);

            // === EL POLVILLO DE SOMBRA (el goteo del tomo — más ralo que
            //     el de la gemela oscura: el mini es pequeño) ===
            if (!Main.dedServ && Main.rand.NextBool(26))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + new Vector2(Main.rand.NextFloat(-7f, 7f),
                        Main.rand.NextFloat(-7f, 7f)),
                    DustID.Shadowflame,
                    new Vector2(Main.rand.NextFloat(-0.3f, 0.3f),
                        Main.rand.NextFloat(-0.7f, -0.2f)),
                    128, new Color(110, 40, 170), 0.55f);
                d.noGravity = true;
            }
        }

        // ==================================================================
        //  EL DIBUJADO — el cuerpo del Eterno en miniatura + el ojo vivo
        //  + los extras del cerebro (sombrilla y mini ojos, lote alfa;
        //  anillo rúnico oscuro e iris, lote aditivo). El contrato del
        //  bool de la casa: salir SIEMPRE con el lote vanilla ABIERTO.
        // ==================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                Vector2 c = Projectile.Center;

                // === 1. LOS EXTRAS DEL CEREBRO, LOTE ALFA: la sombrilla de
                //     página bajo el vecino (FORMA 1) y los discos NEGROS de
                //     los 3 mini ojos — lo negro NO vive en el aditivo
                //     (negro = suma 0 = invisible, la lección de la .66). ===
                ParejaCerebro.DibujarExtrasAlfa(Projectile, _cerebro);

                // === 2. EL HALO VIOLETA (la brasa de las páginas — el pulso
                //     del flotante grande, a escala mini) ===
                float pulso = 0.16f + 0.07f * MathF.Sin(t * 2.6f);
                VFXCore.Quad(c, new Color(110, 40, 170, 255) * pulso, new Vector2(64f, 64f), 0f, VFXCore.SoftGlow);
                VFXCore.Quad(c, new Color(150, 90, 225, 255) * (pulso * 0.8f), new Vector2(30f, 30f), 0f, VFXCore.SoftGlow);

                // === 3. LOS EXTRAS DEL CEREBRO, LOTE ADITIVO: el anillo
                //     rúnico oscuro y el iris violeta de los mini ojos — se
                //     SUMAN al lote del cuerpo (un solo volcado). ===
                ParejaCerebro.DibujarExtrasAditivos(Projectile, _cerebro);

                if (VFXCore.QuadCount > 0)
                {
                    // El contrato de la casa (v6.50.50): volar el lote y
                    // REABRIR el vanilla — jamás salir con el lote cerrado.
                    VFXCore.FlushAdditive(null, true);
                    VFXCore.ReabrirLoteVanilla();
                }
                else
                {
                    VFXCore.ReabrirLoteVanilla();
                }

                // === 4. EL CUERPO: el sprite REAL del Eterno (la familia
                //     del ojo: el mismo cuerpo del Nervioso) a escala 0.52 —
                //     con su OJO VIVO y su párpado (el contrato del
                //     GrimorioNerviosoFlotante, en miniatura). ===
                DibujarElLibro(t);

                // === 5. v6.50.125 — LOS TRES OJOS DE VERDAD: los mismos
                //     minions del grimorio normal (los Ojos del Eterno — el
                //     sprite real 56×54 del arma) en MINIATURA (escala 0,27;
                //     el arma los usa a 0,375), con su iris que MIRA y sus
                //     párpados de textura — la letra: «los mismos minion
                //     que el grimorio normal, o sea ojos, pero mas
                //     pequeños». ===
                ParejaCerebro.DibujarLosTresOjos(Projectile, _cerebro);

                return false;   // el cuerpo SE dibuja aquí (con su ojo)
            }
            catch
            {
                try { VFXCore.ReabrirLoteVanilla(); } catch { }
                return false;
            }
        }

        /// <summary>EL LIBRO EN MINIATURA: el cuerpo del Eterno a 0.52, el
        /// párpado que parpadea a su compás (determinista — el reloj del
        /// mundo) y la mirada: a su dueño normalmente, CLAVADA en la
        /// víctima durante las misiones (la vigilancia es la mirada).</summary>
        private void DibujarElLibro(float t)
        {
            Texture2D tex = Terraria.GameContent.TextureAssets.Item[
                ModContent.ItemType<global::AethonMod.Content.Weapons.GrimoireEternal>()].Value;
            if (tex == null) return;
            Rectangle frame = new Rectangle(0, 0, tex.Width, tex.Height);
            Vector2 origen = frame.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Color luz = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
            const float esc = 0.52f;

            // LA MIRADA: su dueño… salvo de misión — entonces CLAVADA en
            // la víctima (el mini grimorio acecha antes de morder).
            Vector2 objetivo = Main.player[Projectile.owner].MountedCenter;
            if (_cerebro.NpcObjetivo >= 0 && _cerebro.NpcObjetivo < Main.maxNPCs)
            {
                NPC victima = Main.npc[_cerebro.NpcObjetivo];
                if (victima != null && victima.active) objetivo = victima.Center;
            }
            Vector2 d = objetivo - Projectile.Center;
            Vector2 mirada = d.LengthSquared() > 4f
                ? Vector2.Normalize(d) * new Vector2(3.5f, 2.8f) * 0.9f
                : Vector2.Zero;

            Main.spriteBatch.Draw(tex, pos, frame, luz, Projectile.rotation, origen, esc,
                SpriteEffects.None, 0f);

            // EL PARPADEO (determinista — el reloj del mundo): cada ~5 s un
            // párpado de un cuarto de segundo (el libro sueña despierto).
            float parp = (t * 0.9f) % 5.2f;
            if (parp > 4.90f)
            {
                var parpado = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado").Value;
                Main.spriteBatch.Draw(parpado, pos, frame, luz, Projectile.rotation, origen, esc,
                    SpriteEffects.None, 0f);
            }
            else
            {
                // el texel del ojo, ROTADO con el libro (la convención .84)
                Vector2 alOjo = (global::AethonMod.Content.Weapons.GrimorioHambrientoNervioso.OjoTexel
                    + mirada - origen) * esc;
                Vector2 posOjo = pos + alOjo.RotatedBy(Projectile.rotation);

                var iris = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
                Main.spriteBatch.Draw(iris, posOjo, null, luz, Projectile.rotation,
                    iris.Size() * 0.5f, esc * global::AethonMod.Content.Weapons.GrimorioHambrientoNervioso.IrisEscala,
                    SpriteEffects.None, 0f);
            }
        }
    }
}
