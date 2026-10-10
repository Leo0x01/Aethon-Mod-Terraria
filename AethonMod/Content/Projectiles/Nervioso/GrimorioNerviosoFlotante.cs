using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Weapons;

namespace AethonMod.Content.Projectiles.Nervioso
{
    /// <summary>
    /// GRIMORIONERVIOSOFLOTANTE — v6.50.91 — EL LIBRO QUE SALE A CAZAR.
    /// La letra del usuario: «este nuevo libro si tiene hambre y el jugador
    /// deja de moverse por ejemplo 5 segundos ahora que es una prueba, el
    /// grimorio sale y comienza a flotar encima del jugador mirando al
    /// jugador hasta que algo se mueva cerca de el, cualquier criatura no
    /// hostil u hostil sera atacada por el libro, todas menos los NPC que
    /// viven en las casas» — el grimorio SE MANIFIESTA flotando encima del
    /// portador (la levitación del difunto Códice Vivo, su homenaje), el
    /// OJO clavado en su dueño, esperando presas. Cuando algo SE MUEVE
    /// cerca (≤480 px), del libro sale LA SOMBRA DE LA PÁGINA
    /// (SombraPaginaCaza) a morderla: 10% por golpe, absorción, −1% de
    /// hambre — hasta el 0% («saciado. Por ahora.»).
    ///
    /// v6.50.94 — LA FUGA MURIÓ (la letra: «esto de la fuga no tiene mucho
    /// sentido, el jugador se quedaria sin el libro, la fuga simplemente
    /// la quitamos el libro no se fuga, solo queda flotando cerca del
    /// jugador cazando por si mismo»): YA NO HAY MODO FUGA — con el hambre
    /// al 100% el espíritu se queda flotando cerca del jugador CAZANDO POR
    /// SU CUENTA. Y EL ESPÍRITU ÚNICO: si por cualquier vía nace un
    /// segundo espíritu del mismo dueño (la «copia» de la .93 — el sprite
    /// duplicado junto a la mano del que salían los ataques), SOBREVIVE
    /// UNO: el que caza; a igual modo, el más antiguo.
    ///
    /// El ítem NO desaparece del inventario: lo que flota es su ESPÍRITU —
    /// el sprite real del libro con sus capas (ojo, iris rojo, párpados) y
    /// el halo violeta del alma.
    /// </summary>
    public class GrimorioNerviosoFlotante : ModProjectile
    {
        internal const float MODO_CAZA = 0f;
        internal const float MODO_VOLVER = 2f;

        // LA VELA: a cuántos px sobre el jugador flota el libro
        private const float ALTURA_VELA = 64f;
        // EL RADIO DE CAZA: «hasta que algo se mueva cerca de él»
        private const float RADIO_CAZA = 480f;
        // LA BUSCA: cada cuántos ticks mira si algo se mueve cerca
        private const int TICKS_BUSCA = 20;

        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 48;
            Projectile.tileCollide = false;
            Projectile.friendly = false;    // el espíritu no golpea: la sombra muerde por él
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60 * 60 * 30;  // las condiciones lo matan, no el reloj
            Projectile.netImportant = true;
        }

        private float Modo => Projectile.ai[0];

        /// <summary>¿El jugador todavía lleva el libro? (si lo tira o lo
        /// guarda en un cofre, el espíritu se disuelve).</summary>
        private bool TieneElLibro(Player p)
        {
            int tipo = ModContent.ItemType<GrimorioHambrientoNervioso>();
            for (int i = 0; i < 58; i++)
                if (p.inventory[i] != null && p.inventory[i].type == tipo)
                    return true;
            return false;
        }

        /// <summary>¿Hay ya una sombra cazando para este libro? (una presa
        /// por vez — el festín del Nervioso es un arte individual).</summary>
        private bool SombraActiva()
        {
            int tipo = ModContent.ProjectileType<SombraPaginaCaza>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile pr = Main.projectile[i];
                if (pr != null && pr.active && pr.type == tipo && pr.owner == Projectile.owner)
                    return true;
            }
            return false;
        }

        /// <summary>v6.50.94 — ¿SOY UN DUPLICADO? El espíritu único: si hay
        /// OTRO espíritu de este dueño, gana el MODO_CAZA (el que caza);
        /// a igual modo, el de whoAmI más bajo (el más antiguo). El
        /// perdedor se disuelve — jamás dos libros flotando (la «copia» de
        /// la .93: dos sprites y los ataques saliendo de la equivocada).</summary>
        private bool SoyDuplicado()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (i == Projectile.whoAmI)
                    continue;
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.type != Projectile.type || p.owner != Projectile.owner)
                    continue;
                // él caza y yo no: él gana; ambos igual: gana el más antiguo
                if (p.ai[0] == MODO_CAZA && Projectile.ai[0] != MODO_CAZA) return true;
                if (p.ai[0] == Projectile.ai[0] && i < Projectile.whoAmI) return true;
            }
            return false;
        }

        /// <summary>LA PRESA: la criatura MÁS CERCANA QUE SE MUEVA alrededor
        /// del libro — hostil o no, TODAS («cualquier criatura no hostil u
        /// hostil») menos los NPC que viven en las casas.</summary>
        private NPC PresaCercana()
        {
            NPC mejor = null;
            float mejorD = RADIO_CAZA * RADIO_CAZA;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.life <= 0 || n.dontTakeDamage) continue;
                if (n.townNPC || NPCID.Sets.ActsLikeTownNPC[n.type]) continue;   // los vecinos, jamás
                if (n.velocity.LengthSquared() < 0.02f) continue;                 // «hasta que algo SE MUEVA»
                float d = Vector2.DistanceSquared(n.Center, Projectile.Center);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        /// <summary>EL CAMBIO DE MODO a VOLVER (con la despedida que toca).</summary>
        private void IrAVolver()
        {
            if (Modo == MODO_CAZA)
                GrimorioHambrientoNervioso.DecirLocal("Saciado",
                    new Color(150, 200, 160));   // …saciado. Por ahora.
            Projectile.ai[0] = MODO_VOLVER;
            Projectile.ai[1] = 0f;
            Projectile.netUpdate = true;
            Sonar(SoundID.Item122.WithPitchOffset(0.3f).WithVolumeScale(0.4f), Projectile.Center);
        }

        public override void AI()
        {
            Projectile.ai[1]++;
            float t = Projectile.ai[1];
            Player dueño = Main.player[Projectile.owner];
            bool autoridad = Main.netMode != NetmodeID.MultiplayerClient;
            float hambre = GrimorioHambrientoNervioso.EstadoCompartido.Hambre;

            // === LA RED DE SEGURIDAD (todas las máquinas): sin dueño, sin libro ===
            if (dueño == null || !dueño.active)
            {
                if (autoridad) { Projectile.Kill(); }
                return;
            }

            // === LA AUTORIDAD DECIDE LA VIDA DEL ESPÍRITU ===
            if (autoridad)
            {
                if (!TieneElLibro(dueño)) { Projectile.Kill(); return; }

                // v6.50.94 — EL ESPÍRITU ÚNICO (cada 30 t, barato): jamás
                // dos libros del mismo dueño — véase SoyDuplicado()
                if (t % 30f == 0f && SoyDuplicado()) { Projectile.Kill(); return; }

                if (Modo == MODO_CAZA)
                {
                    // la caza termina AL 0% («saciado»)… y YA NO HAY FUGA:
                    // al 100% el libro NO rompe — se queda flotando cerca
                    // del jugador, cazando por su cuenta (la letra .94:
                    // «la fuga simplemente la quitamos el libro no se fuga,
                    // solo queda flotando cerca del jugador cazando por si
                    // mismo»)
                    if (hambre <= 0f) IrAVolver();
                }
            }

            // === EL MOVIMIENTO ===
            if (Modo == MODO_CAZA)
            {
                // LA VELA (la levitación del Códice Vivo): muelle suave
                // encima del dueño + el vaivén que respiraba el difunto
                Vector2 destino = dueño.MountedCenter + new Vector2(0f, -ALTURA_VELA)
                    + new Vector2(MathF.Sin(t * 0.07f) * 10f, MathF.Sin(t * 0.11f) * 14f);
                Projectile.velocity += (destino - Projectile.Center) * 0.02f;
                Projectile.velocity *= 0.90f;

                // v6.50.92 — el libro se INCLINA hacia lo que MIRA: su
                // COMIDA mientras devora (la letra: «el libro debe mirar
                // lo que esta comiendo»), su dueño el resto de la vela
                Vector2 atento = GrimorioHambrientoNervioso.FocoDelFestin(Projectile.owner)
                    ?? dueño.MountedCenter;
                float lado = atento.X < Projectile.Center.X ? -1f : 1f;
                Projectile.rotation = MathF.Sin(t * 0.045f) * 0.12f + lado * 0.10f;

                // LA CAZA (autoridad): algo se mueve cerca → la sombra.
                // v6.50.94 — EL COMPÁS DE LA SOMBRA (la letra: «haciendo
                // que el libro en ese estado solo pueda lanzar su ataque
                // de la sombra de la pagina una vez cada 10 segundo o algo
                // asi, o poniendo por codigo que no puede lanzar un ataque
                // 2 segundos despues de terminar el primero»): UN ataque
                // cada 10 s (MarcarAtaque al NACER la sombra) y jamás
                // antes de 2 s de TERMINAR la anterior (SombraTerminada al
                // morir) — el re-lanzamiento en el instante de la muerte
                // murió. Y mientras el jugador dispara con el libro, el
                // espíritu NO lanza sombras por su cuenta (la caza
                // solitaria es del ausente: AFK o mucho tiempo sin
                // atacar criaturas con el libro).
                if (autoridad && t % TICKS_BUSCA == 0f && !SombraActiva() &&
                    SombraPaginaCaza.AtaqueListo(Projectile.owner) &&
                    !GrimorioHambrientoNervioso.JugadorAtacoReciente())
                {
                    NPC presa = PresaCercana();
                    if (presa != null)
                    {
                        SombraPaginaCaza.MarcarAtaque(Projectile.owner);
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center,
                            Vector2.Zero, ModContent.ProjectileType<SombraPaginaCaza>(),
                            0, 0f, Projectile.owner, presa.whoAmI + 1, 0f, 0f);
                        Sonar(SoundID.Item122.WithPitchOffset(-0.3f).WithVolumeScale(0.6f), Projectile.Center);
                    }
                }
            }
            else
            {
                // LA VUELTA: acelera hacia su dueño y se guarda al tocarlo
                Vector2 hacia = dueño.MountedCenter - Projectile.Center;
                if (hacia.Length() < 30f)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        int d = Dust.NewDust(Projectile.Center - new Vector2(14, 14), 28, 28,
                            DustID.Shadowflame, 0f, -0.2f, 128, default, 0.6f);
                        Main.dust[d].noGravity = true;
                    }
                    if (autoridad) Projectile.Kill();
                    return;
                }
                hacia = hacia.SafeNormalize(-Vector2.UnitY);
                Projectile.velocity += hacia * 0.90f;
                float vel = Projectile.velocity.Length();
                if (vel > 22f) Projectile.velocity *= 22f / vel;
                Projectile.rotation *= 0.90f;
            }

            // polvillo violeta de la levitación
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(9))
            {
                int d = Dust.NewDust(Projectile.Center - new Vector2(20, 20), 40, 40,
                    DustID.Shadowflame, 0f, -0.25f, 128, default, 0.5f);
                Main.dust[d].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;

            // LA MÁQUINA VIVA: aunque el libro duerma en la mochila (inventario
            // cerrado), sus efectos siguen corriendo — una vez por tick
            GrimorioHambrientoNervioso.PasoMaquina();

            float t = Projectile.ai[1];
            var estado = GrimorioHambrientoNervioso.EstadoCompartido;

            // === EL HALO DEL ESPÍRITU (violeta — el rojo de la fuga murió
            //     con ella: este libro ya no se va) ===
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                VFXCore.Begin();
                float pulso = 0.16f + 0.07f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.6f);
                VFXCore.Quad(Projectile.Center, SombrasLib.Alfa(SombrasLib.Violeta, pulso),
                    new Vector2(170f, 170f));
                VFXCore.FlushAdditive();
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();

            // === EL LIBRO — el sprite REAL del ítem con sus capas. El
            //     cazador flota SERENO (las tiras de glitch eran el
            //     repertorio de la fuga, y la fuga murió) ===
            Texture2D tex = Terraria.GameContent.TextureAssets.Item[
                ModContent.ItemType<GrimorioHambrientoNervioso>()].Value;
            Rectangle frame = new Rectangle(0, 0, tex.Width, tex.Height);
            Vector2 origen = frame.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Color luz = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
            float esc = 1.15f;

            Main.spriteBatch.Draw(tex, pos, frame, luz, Projectile.rotation, origen, esc,
                SpriteEffects.None, 0f);

            // EL PÁRPADO o EL IRIS — las capas del libro, mismo lenguaje
            if (estado.Fase > 0)
            {
                bool medio = estado.Fase > 7 || estado.Fase < 4;
                var parpado = ModContent.Request<Texture2D>(medio
                    ? "AethonMod/Content/Weapons/GrimorioHambriento_Medio"
                    : "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado").Value;
                Main.spriteBatch.Draw(parpado, pos, frame, luz, Projectile.rotation, origen, esc,
                    SpriteEffects.None, 0f);
            }
            else
            {
                // LA MIRADA: clavada en su dueño («mirando al jugador»)…
                // salvo mientras COME: entonces en su COMIDA (v6.50.92 —
                // la letra: «el libro debe mirar lo que esta comiendo»:
                // la presa que la sombra muerde, o las almas subiendo
                // mientras las absorbe)
                Vector2 objetivo = GrimorioHambrientoNervioso.FocoDelFestin(Projectile.owner)
                    ?? Main.player[Projectile.owner].MountedCenter;
                Vector2 d = objetivo - Projectile.Center;
                Vector2 mirada = d.LengthSquared() > 4f
                    ? Vector2.Normalize(d) * new Vector2(3.5f, 2.8f) * 0.9f
                    : Vector2.Zero;

                // el texel del ojo, ROTADO con el libro (la convención .84)
                Vector2 alOjo = (GrimorioHambrientoNervioso.OjoTexel + mirada - origen) * esc;
                Vector2 posOjo = pos + alOjo.RotatedBy(Projectile.rotation);

                var iris = ModContent.Request<Texture2D>(
                    "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;
                Main.spriteBatch.Draw(iris, posOjo, null, luz, Projectile.rotation,
                    iris.Size() * 0.5f, esc * GrimorioHambrientoNervioso.IrisEscala,
                    SpriteEffects.None, 0f);
                float rojo = estado.NivelRojo();
                if (rojo > 0f)
                {
                    var irisRojo = ModContent.Request<Texture2D>(
                        "AethonMod/Content/Weapons/GrimorioHambriento_IrisRojo").Value;
                    Main.spriteBatch.Draw(irisRojo, posOjo, null,
                        GrimorioHambrientoNervioso.AlfaCapa(rojo), Projectile.rotation,
                        irisRojo.Size() * 0.5f, esc * GrimorioHambrientoNervioso.IrisEscala,
                        SpriteEffects.None, 0f);
                }
            }

            return false;
        }

        private static void Sonar(Terraria.Audio.SoundStyle estilo, Vector2 pos)
        {
            try { Terraria.Audio.SoundEngine.PlaySound(estilo, pos); } catch { }
        }
    }
}
