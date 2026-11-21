// ======================================================================
//  PORTALTRIOPROJECTILE — v6.162 — EL GATILLO INVISIBLE DEL TRÍO (la
// letra: «cuando uso el arma no funciona, o sea no dispara
// proyectiles» — la autopsia: el Trío vivía con Item.shoot = None y
// EL CONTRATO DE TERRARIA ES que el hook Shoot SOLO CORRE cuando
// Item.shoot > 0: las ranuras se llenaban pero el clic izquierdo
// NUNCA llegaba a dispararlas). LA CURA: el Trío ahora dispara ESTE
// proyectil-portal (invisible, 1 tick de vida, cero daño) — su ÚNICO
// trabajo es ABRIR LA PUERTA del hook Shoot: al volverse el
// disparador vivo, TrioDelEterno.Shoot corre y dispara LAS ARMAS DE
// LAS RANURAS con sus propios proyectiles y sus números. Este
// proyectil jamás se ve, jamás golpea y muere al instante.
// ======================================================================
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace AethonMod.Content.Projectiles.Grimorio
{
    public class PortalTrioProjectile : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.friendly = false;    // NO golpea: solo abre la puerta
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;        // nace y muere: el disparo real es de las ranuras
            Projectile.alpha = 255;         // invisible SIEMPRE
        }

        public override bool PreDraw(ref Color lightColor) => false;   // jamás se dibuja
    }
}
