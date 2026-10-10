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
    /// GRIMORIONERVIOSOFLOTANTE — v6.50.91 — EL LIBRO QUE SALE A CAZAR (y el
    /// que SE ESCAPA). Dos letras del usuario:
    ///
    /// · LA CAZA: «este nuevo libro si tiene hambre y el jugador deja de
    ///   moverse por ejemplo 5 segundos ahora que es una prueba, el grimorio
    ///   sale y comienza a flotar encima del jugador mirando al jugador
    ///   hasta que algo se mueva cerca de el, cualquier criatura no hostil u
    ///   hostil sera atacada por el libro, todas menos los NPC que viven en
    ///   las casas» — con hambre por debajo del 50%, el grimorio SE MANIFIESTA
    ///   flotando encima del portador (la levitación del difunto Códice Vivo,
    ///   su homenaje), el OJO clavado en su dueño, esperando presas. Cuando
    ///   algo SE MUEVE cerca (≤480 px), del libro sale LA SOMBRA DE LA
    ///   PÁGINA (SombraPaginaCaza) a morderla: 10% por golpe, absorción,
    ///   −1% de hambre — hasta el 0% («saciado. Por ahora.»).
    ///
    /// · LA FUGA: «cuando su hambre llegue a 100, hagamos que escape del
    ///   jugador algo así a como se usa El Codice Vivo, que al atacar el
    ///   item hace una animación donde comienza a flotar» — con el hambre al
    ///   MÁXIMO el libro se desprende y HUYE: guarda distancia (380–640 px),
    ///   quiebra nervioso, el ojo ROJO al máximo y el cuerpo ROTO en tiras
    ///   (todo el repertorio del Nervioso). Vuelve cuando lo alimentan o lo
    ///   reinician (clic derecho).
    ///
    /// El ítem NO desaparece del inventario: lo que flota es su ESPÍRITU —
    /// el sprite real del libro con sus capas (ojo, iris rojo, párpados) y
    /// el halo del alma (violeta en caza, ROJO en fuga).
    /// </summary>
    public class GrimorioNerviosoFlotante : ModProjectile
    {
        internal const float MODO_CAZA = 0f;
        internal const float MODO_FUGA = 1f;
        internal const float MODO_VOLVER = 2f;

        // LA VELA: a cuántos px sobre el jugador flota el libro
        private const float ALTURA_VELA = 64f;
        // EL RADIO DE CAZA: «hasta que algo se mueva cerca de él»
        private const float RADIO_CAZA = 480f;
        // LA FUGA: la distancia que guarda el libro escapado
        private const float FUGA_MIN = 380f;
        private const float FUGA_MAX = 640f;
        // LA BUSCA: cada cuántos ticks mira si algo se mueve cerca
        private const int TICKS_BUSCA = 20;

        // el reloj de los quiebres de la fuga (vive en la máquina que corre la AI)
        private int _tHastaQuiebre = 30;

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

                if (Modo == MODO_CAZA)
                {
                    // la caza termina AL 0% («saciado»)…
                    if (hambre <= 0f) IrAVolver();
                }
                else if (Modo == MODO_FUGA)
                {
                    // …y la fuga termina cuando lo alimentan o lo reinician
                    if (hambre < 1f) IrAVolver();
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

                // el libro se INCLINA hacia su dueño — lo está mirando
                float lado = dueño.MountedCenter.X < Projectile.Center.X ? -1f : 1f;
                Projectile.rotation = MathF.Sin(t * 0.045f) * 0.12f + lado * 0.10f;

                // LA CAZA (autoridad): algo se mueve cerca → la sombra
                if (autoridad && t % TICKS_BUSCA == 0f && !SombraActiva())
                {
                    NPC presa = PresaCercana();
                    if (presa != null)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center,
                            Vector2.Zero, ModContent.ProjectileType<SombraPaginaCaza>(),
                            0, 0f, Projectile.owner, presa.whoAmI + 1, 0f, 0f);
                        Sonar(SoundID.Item122.WithPitchOffset(-0.3f).WithVolumeScale(0.6f), Projectile.Center);
                    }
                }
            }
            else if (Modo == MODO_FUGA)
            {
                // LA FUGA: guarda distancia del jugador (380–640 px) y
                // quiebra NERVIOSO — es un libro HARTO, no un pajarito
                Vector2 delJugador = Projectile.Center - dueño.MountedCenter;
                float dist = delJugador.Length();
                Vector2 rumbo = dist > 1f ? delJugador / dist : -Vector2.UnitY;
                // sesgo hacia arriba: en la superficie vuela, no se entierra
                Vector2 huida = new Vector2(rumbo.X * 1.25f, rumbo.Y - 0.35f);
                huida = huida.SafeNormalize(Vector2.UnitX);

                if (dist < FUGA_MIN)
                    Projectile.velocity += huida * 0.55f;          // ¡aléjate!
                else if (dist > FUGA_MAX)
                    Projectile.velocity -= huida * 0.30f;          // pero no te pierdas de vista

                // EL QUIEBRE: cada 30–55 t un tirón lateral al azar
                if (--_tHastaQuiebre <= 0)
                {
                    _tHastaQuiebre = 30 + Main.rand.Next(26);
                    float ang = rumbo.ToRotation() + (Main.rand.Next(2) == 0 ? 1f : -1f) * MathHelper.PiOver2;
                    Projectile.velocity += ang.ToRotationVector2() * 3.2f;
                }

                Projectile.velocity *= 0.94f;
                Projectile.rotation = MathF.Sin(t * 0.09f) * 0.18f + rumbo.X * 0.10f;
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
            bool fuga = Modo == MODO_FUGA;

            // === EL HALO DEL ESPÍRITU (violeta en caza, ROJO en fuga) ===
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                VFXCore.Begin();
                Color alma = fuga ? SombrasLib.Rojo : SombrasLib.Violeta;
                float pulso = 0.16f + 0.07f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.6f);
                VFXCore.Quad(Projectile.Center, SombrasLib.Alfa(alma, fuga ? pulso + 0.08f : pulso),
                    new Vector2(170f, 170f));
                VFXCore.FlushAdditive();
            }
            catch { VFXCore.CerrarLoteSiAbierto(); }
            VFXCore.ReabrirLoteVanilla();

            // === EL LIBRO — el sprite REAL del ítem con sus capas ===
            Texture2D tex = Terraria.GameContent.TextureAssets.Item[
                ModContent.ItemType<GrimorioHambrientoNervioso>()].Value;
            Rectangle frame = new Rectangle(0, 0, tex.Width, tex.Height);
            Vector2 origen = frame.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Color luz = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
            float esc = 1.15f;

            // EL CORRIMIENTO y EL GLITCH: en fuga tiembla, brinca Y se rompe
            // (todo el repertorio); en caza flota SERENO — el cazador paciente
            Vector2 corr = fuga ? GrimorioHambrientoNervioso.Corrimiento() * esc : Vector2.Zero;
            float offOjo = 0f;
            GrimorioHambrientoNervioso.Tira[] tiras = null;
            if (fuga)
            {
                tiras = GrimorioHambrientoNervioso.Layout(out offOjo);
                GrimorioHambrientoNervioso.DibujarTiras(Main.spriteBatch, tex,
                    pos + corr, frame, luz, Projectile.rotation, origen, esc, tiras);
            }
            else
            {
                Main.spriteBatch.Draw(tex, pos, frame, luz, Projectile.rotation, origen, esc,
                    SpriteEffects.None, 0f);
            }

            // EL PÁRPADO o EL IRIS — las capas del libro, mismo lenguaje
            if (estado.Fase > 0)
            {
                bool medio = estado.Fase > 7 || estado.Fase < 4;
                var parpado = ModContent.Request<Texture2D>(medio
                    ? "AethonMod/Content/Weapons/GrimorioHambriento_Medio"
                    : "AethonMod/Content/Weapons/GrimorioHambriento_Cerrado").Value;
                if (tiras != null && tiras.Length > 0)
                    GrimorioHambrientoNervioso.DibujarTiras(Main.spriteBatch, parpado,
                        pos + corr, frame, luz, Projectile.rotation, origen, esc, tiras);
                else
                    Main.spriteBatch.Draw(parpado, pos, frame, luz, Projectile.rotation, origen, esc,
                        SpriteEffects.None, 0f);
            }
            else
            {
                // LA MIRADA: en caza, clavada en su dueño («mirando al
                // jugador»); en fuga, los dardos ansiosos del Estado
                Vector2 mirada;
                if (fuga)
                {
                    mirada = estado.DespActual;
                }
                else
                {
                    Player dueño = Main.player[Projectile.owner];
                    Vector2 d = dueño.MountedCenter - Projectile.Center;
                    mirada = d.LengthSquared() > 4f
                        ? Vector2.Normalize(d) * new Vector2(3.5f, 2.8f) * 0.9f
                        : Vector2.Zero;
                }

                // el texel del ojo, ROTADO con el libro (la convención .84)
                Vector2 alOjo = (GrimorioHambrientoNervioso.OjoTexel + mirada - origen) * esc;
                Vector2 posOjo = pos + corr + alOjo.RotatedBy(Projectile.rotation)
                    + new Vector2(offOjo * esc, 0f).RotatedBy(Projectile.rotation);

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
