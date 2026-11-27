using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Projectiles.Jefes
{
    // ======================================================================
    //  PAREJASELLADOPROJECTILE — v6.184 — LA PAREJA DORADA/VIOLETA DEL
    //  SELLO, PROPIA DEL DIOS (la letra: «Aethon sellado tienes que
    //  programarlo y separar toda su logica del grimorio eterno, aunque
    //  son los mismos es una version jefe, son logicas separadas, si no
    //  separas sus logicas entonces sus ataques lo golpean a el mismo»).
    //
    //  LA AUTOPSIA: la pareja del Sellado disparaba el PROYECTIL VANILLA
    //  931 CON EL JUGADOR COMO DUEÑO — un proyectil "amigo" nacido del
    //  jefe: sus propios golpes se volvían contra ÉL. LA CURA: el dios
    //  sellado tiene SU pareja — proyectil HOSTIL de la casa, tinta
    //  dorada y violeta (los dos tonos del arma, el dibujo del dios):
    //  un filo de estrella que vuela a la presa y MUERE EN TIEMPO (R23).
    //
    //  ai[0] = 0 dorado · 1 violeta · ai[1] = edad.
    // ======================================================================
    public class ParejaSelladoProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private static readonly Color Oro = new(255, 206, 96);
        private static readonly Color Violeta = new(186, 108, 255);
        private static readonly Color Nucleo = new(255, 250, 226);

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.tileCollide = false;
            Projectile.friendly = false;      // HOSTIL: la lógica del JEFE
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 210;         // R23 — LA VIDA CONTADA
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            try
            {
                Projectile.ai[1]++;
                // el filo de la pareja gira sobre su rumbo (la firma del arma)
                Projectile.rotation += 0.22f;
                // los bordes del mundo se lo tragan
                if (Projectile.Center.X < 60f || Projectile.Center.X > (Main.maxTilesX - 4) * 16f ||
                    Projectile.Center.Y < 60f || Projectile.Center.Y > (Main.maxTilesY - 4) * 16f)
                    Projectile.Kill();
                // LA CHISPA (determinista en el draw — R22 en el filo)
                if (!Main.dedServ && Main.rand.NextBool(3))
                {
                    bool oro = Projectile.ai[0] < 0.5f;
                    var d = Dust.NewDustPerfect(Projectile.Center,
                        oro ? DustID.GoldFlame : DustID.Shadowflame,
                        new Vector2(0f, -0.2f), 130,
                        oro ? new Color(255, 206, 96) : new Color(168, 92, 240), 0.7f);
                    d.noGravity = true;
                }
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("ParejaSellado", exRegistro); }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                bool oro = Projectile.ai[0] < 0.5f;
                Color halo = oro ? Oro : Violeta;
                Vector2 pos = Projectile.Center - Main.screenPosition;
                Vector2[] pts = new Vector2[4];
                int seed = Projectile.whoAmI * 17;
                for (int i = 0; i < 4; i++)
                {
                    // LA ESTRELLA DE CUATRO PUNTAS (el filo del arma en
                    // miniatura — el compás del grimorio)
                    float ang = Projectile.rotation + i * MathHelper.PiOver2;
                    float largo = 15f + 3f * MathF.Sin(Main.GlobalTimeWrappedHourly * 9f + i);
                    pts[i] = pos + ang.ToRotationVector2() * largo;
                }
                for (int i = 0; i < 4; i++)
                {
                    Vector2 a = pts[i];
                    Vector2 b = pts[(i + 1) % 4];
                    Vector2 medio = (a + b) * 0.5f;
                    float largo = Vector2.Distance(a, b);
                    VFXCore.Quad(medio + Main.screenPosition, halo * 0.55f,
                        new Vector2(largo, 3.6f), (b - a).ToRotation());
                }
                // EL NÚCLEO BLANCO (la punta del filo)
                var glow = VFXCore.SoftGlow;
                if (glow != null)
                    Main.spriteBatch.Draw(glow, pos, null,
                        Nucleo * 0.85f, 0f, glow.Size() * 0.5f,
                        new Vector2(0.68f, 0.68f), SpriteEffects.None, 0f);
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("ParejaSellado.Draw", exRegistro); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }

    // ======================================================================
    //  ESTALLIDOSELLADOPROJECTILE — v6.184 — LA EXPLOSIÓN DE LUZ DEL DIOS
    //  SELLADO, LA CANÓNICA (la letra: «la explosion de luz real que
    //  debes usar es la explosion de luz de la mascota de luz Pequeño
    //  Recuerdo de Aethon… solo debes escalar su tamaño»).
    //
    //  LA EXPLOSIÓN DE LUZ DEL DIOS ya NO es el arsenal prestado del
    //  jefe Recuerdo (el EstiloEstallidoRadiante con su estrella
    //  cruzada de textura extraña): es LA EXPLOSIÓN DE LA MASCOTA —
    //  EstallidoLuzLib, la receta EXACTA del Pequeño Recuerdo de
    //  Aethon — a ESCALA DE DIOS (×2,2). Nace, ARDE y se disuelve:
    //  la recogida de 6 agujas, los 14 rayos, la cruz, el núcleo que
    //  inunda, la estrella, la onda y las bokeh.
    //
    //  HOSTIL (la lógica del JEFE — jamás golpea a su propio dios) y
    //  con la vida contada (R23).
    // ======================================================================
    public class EstallidoSelladoProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        /// <summary>LA ESCALA DE DIOS: la explosión de la mascota ×2,2.</summary>
        private const float ESCALA_DIOS = 2.2f;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 130;             // la hitbox del estallido
            Projectile.height = 130;
            Projectile.tileCollide = false;
            Projectile.friendly = false;       // HOSTIL: la lógica del JEFE
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 165;          // R23 — la vida del espectáculo
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            try
            {
                Projectile.ai[1]++;
                // EL NACIMIENTO (una sola vez: el estampido de la mascota,
                // a escala de dios)
                if (Projectile.ai[1] <= 1f && !Main.dedServ)
                    Terraria.Audio.SoundEngine.PlaySound(
                        SoundID.Item122.WithPitchOffset(-0.35f).WithVolumeScale(1.1f),
                        Projectile.Center);
                // LA DERIVA CORTA (el estallido AVANZA apenas y frena —
                // la firma de la mascota: la luz atraviesa despacio)
                Projectile.velocity *= 0.94f;
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("EstallidoSellado", exRegistro); }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server) return false;
            VFXCore.CerrarLoteSiAbierto();
            try
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    // LA EXPLOSIÓN CANÓNICA (la de la mascota — escalada)
                    EstallidoLuzLib.Dibujar(Main.spriteBatch,
                        Projectile.Center - Main.screenPosition,
                        Projectile.ai[1], ESCALA_DIOS, Projectile.whoAmI * 31 + 7);
                }
                finally { Main.spriteBatch.End(); }
            }
            catch (System.Exception exRegistro)
            { global::AethonMod.Content.Systems.RegistroSistema.Fallo("EstallidoSellado.Draw", exRegistro); }
            VFXCore.ReabrirLoteVanilla();
            return false;
        }
    }
}
