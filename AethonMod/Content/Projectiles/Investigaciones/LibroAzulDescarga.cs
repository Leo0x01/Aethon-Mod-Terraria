using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Investigaciones
{
    // ======================================================================
    //  v6.88 — LA DESCARGA DEL LIBRO AZUL: el perno del arma del árbol.
    //
    //  Un dardo de tinta azul con estela de polvo helado: simple y
    //  limpio (la PRUEBA de que el árbol vive en el arma — el daño
    //  nace del nivel del Libro Azul: 12 + nivel·2 en el disparo).
    //  ai[0] lleva el NIVEL DEL ARMA (para el brillo: el perno arde
    //  más azul cuanto más crece el libro).
    // ======================================================================
    public class LibroAzulDescarga : ModProjectile
    {
        // v6.88.1 — LA TEXTURA POR CONVENCIÓN (la lección v6.84/v6.86
        // OTRA VEZ, cazada por audit_assets.py ANTES del release): tML
        // exige la textura de TODO ModProjectile al CARGAR (aunque
        // PreDraw devuelva false y jamás la dibuje) — sin ella:
        // MissingResourceException → el mod ENTERO se desactiva y TODO
        // queda invisible. El patrón de la casa (SolEnMiniaturaProj,
        // AureumSkyStrike): la textura del perno ES el flare de 4
        // puntas — el dibujo real lo hace PreDraw (la cápsula viva).
        public override string Texture => "AethonMod/Content/Effects/FlareEstrella4Puntas";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.tileCollide = true;
            Projectile.aiStyle = 0;
        }

        public override void AI()
        {
            // LA ESTELA: polvo azul helado (el rastro de tinta).
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center,
                    DustID.BlueFlare,
                    -Projectile.velocity * 0.15f + Main.rand.NextVector2Circular(0.6f, 0.6f),
                    140, new Color(120, 190, 255), 0.65f);
                d.noGravity = true;
            }
            // EL RUMBO ESTABLE (sin gravedad ni rozaduras).
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 1f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            try
            {
                // EL PERNO: una cápsula azul (viva con el nivel del arma:
                // ai[0] — más nivel, más núcleo blanco).
                int nivel = (int)Projectile.ai[0];
                float k = System.Math.Min(1f, 0.45f + nivel / 100f * 0.55f);
                Vector2 pos = Projectile.Center - Main.screenPosition;
                Vector2 dir = Projectile.velocity; dir.Normalize();
                for (int capa = 0; capa < 3; capa++)
                {
                    float ancho = capa switch { 0 => 7.5f, 1 => 4.2f, _ => 2.0f };
                    Color c = capa switch
                    {
                        0 => new Color(40, 90, 200, 150),
                        1 => new Color(110, 180, 255, 200),
                        _ => new Color(235, 248, 255, (int)(235f * k)),
                    };
                    global::AethonMod.Content.VFX.VFXCore.Capsule(pos + dir * 4f, 26f, ancho,
                        Projectile.velocity.ToRotation(), c);
                }
                return false;   // el sprite del proyectil NO: el perno ES el dibujo
            }
            catch { return false; }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // EL CHISPazo del impacto.
            if (!Main.dedServ)
                for (int i = 0; i < 8; i++)
                {
                    Dust d = Dust.NewDustPerfect(target.Center, DustID.BlueFlare,
                        Main.rand.NextVector2Circular(3f, 3f), 140,
                        new Color(140, 200, 255), 0.8f);
                    d.noGravity = true;
                }
        }

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
                for (int i = 0; i < 6; i++)
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueFlare,
                        Main.rand.NextVector2Circular(2.2f, 2.2f), 130,
                        new Color(120, 180, 255), 0.7f);
                    d.noGravity = true;
                }
        }
    }
}
