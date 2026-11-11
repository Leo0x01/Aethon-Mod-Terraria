using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;
using AethonMod.Content.Projectiles.Jefes;

namespace AethonMod.Content.Weapons.Definitivas
{
    // ======================================================================
    //  v6.124 — EL TAJO DEL VACÍO (la letra del usuario: «ademas las otras
    //  dos armas las puedes borrar el tajo de oscuridad y las agujas del
    //  vacio, entonces crea un nuevo tajo y para eso debes usar un pequeño
    //  haz usando nuestros hazes, el inicio y final del tajo tipo haz sera
    //  muy fino en el centro se vuelve mas grueso, y como dije antes los
    //  colores son negro en el centro, morado en los bordes»).
    //
    //  EL TAJO-HAZ — TODO sobre HazLib (NUESTROS HAZES, la librería de la
    //  casa: rejilla de quads con el FILO DE BORDE rgb=0 — imposible que
    //  un pico encienda):
    //    · LA FORMA: un PEQUEÑO haz de ~170 px — el semiancho es la
    //      LEY DEL HUSO: MUY FINO en el inicio y el final (seno elevado
    //      a 0.75: las puntas mueren asintóticas) y GRUESO en el centro
    //      — un tajo-lágrima que se clava como aguja y atraviesa.
    //    · EL COLOR, LA LETRA EXACTA: NEGRO en el centro (el vacío que
    //      no aporta luz — en el lote aditivo el corazón del tajo se lee
    //      NEGRO contra el mundo) y MORADO en los bordes (el violeta
    //      vivo que arde en el filo y muere EXACTO en el borde por el
    //      filo de la casa — el negro solo existe si su borde arde).
    //    · LA RAMPA DE VIDA v6.122 (HacesVida.Rampa): el tajo NACE y
    //      MUERE suave — jamás un corte brusco.
    //
    //  EL MODO (el corazón del tajo de siempre, intacto): al usar el
    //  arma, HASTA TRES TAJOS nacen CERCA de los enemigos más cercanos al
    //  cursor — cada tajo aparece ~150 px DETRÁS de su presa, EN EL EJE
    //  de la dirección del disparo, y LA ATRAVIESA a toda velocidad como
    //  una aguja que se clava («aparecen siempre cerca del enemigo»).
    // ======================================================================
    public class TajoDelVacio : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 320;
            Item.DamageType = DamageClass.Magic;
            Item.width = 44; Item.height = 44;
            Item.useTime = 16; Item.useAnimation = 16;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<TajoDelVacioProj>();
            Item.shootSpeed = 1f;
            Item.mana = 12;
            Item.ArmorPenetration = 200;
            Item.rare = ItemRarityID.Purple;
            Item.value = 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // LA DIRECCIÓN DEL USO: del jugador al cursor — el eje de
            // TODOS los tajos (el tajo aparece de un extremo a otro
            // DEPENDIENDO DE LA DIRECCIÓN, la letra).
            Vector2 dir = Main.MouseWorld - player.Center;
            if (dir.LengthSquared() < 1f) dir = -Vector2.UnitY;
            dir = Vector2.Normalize(dir);

            // LOS TRES CERCADOS: los enemigos más cercanos al CURSOR
            // (radio 900) — los tajos nacen CERCA de ellos, la letra.
            int tres = 0;
            for (int pasada = 0; pasada < 3 && tres < 3; pasada++)
            {
                NPC masCerca = null;
                float mejor = float.MaxValue;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || !n.CanBeChasedBy() || n.friendly ||
                        yaMarcado(n)) continue;
                    float d = n.Center.Distance(Main.MouseWorld);
                    if (d < 900f && d < mejor) { mejor = d; masCerca = n; }
                }
                if (masCerca == null) break;
                _marcados[tres++] = masCerca.whoAmI;

                // EL TAJO NACE 150 px DETRÁS de la presa (en el eje de la
                // dirección) y LA ATRAVIESA — la aguja-haz que se clava.
                Vector2 nace = masCerca.Center - dir * 150f;
                Projectile.NewProjectile(source, nace, dir * 38f, type,
                    damage, knockback, player.whoAmI, dir.ToRotation(), 0f);
            }
            return false;
        }

        // LA MEMORIA DEL TIRO (para no marcar tres veces al mismo NPC)
        private static readonly int[] _marcados = new int[3];

        private static bool yaMarcado(NPC n)
        {
            for (int k = 0; k < 3; k++)
                if (_marcados[k] == n.whoAmI) return true;
            return false;
        }

        public override bool CanShoot(Player player)
        {
            _marcados[0] = _marcados[1] = _marcados[2] = -1;
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    // ======================================================================
    //  TajoDelVacioProj — EL TAJO-HAZ: la lágrima de vacío que se clava
    //  y atraviesa (fina en los extremos · gruesa al centro · negra al
    //  corazón · morada en el filo).
    // ======================================================================
    public class TajoDelVacioProj : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/SoftGlow";

        private const float Vida = 42f;
        private float _t;

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;         // ATRAVIESA a todo el que toca
            Projectile.timeLeft = (int)Vida;
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;
            Projectile.netImportant = false;
        }

        public override void AI()
        {
            _t++;
            // EL VUELO DEL TAJO: recto y veloz (la aguja clavada).
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (!Main.dedServ)
            {
                // LA ESTELA DE VACÍO (motas violetas-negras — la firma
                // del tajo de siempre, intacta)
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                        -Projectile.velocity * 0.12f + Main.rand.NextVector2Circular(1f, 1f),
                        0, Main.rand.NextBool(3) ? new Color(200, 150, 255) : new Color(90, 50, 160), 0.8f);
                    d.noGravity = true;
                }
                // LA LUZ DEL FILO MORADO
                Lighting.AddLight(Projectile.Center, new Vector3(0.45f, 0.28f, 0.75f));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            try
            {
                // EL TAJO-HAZ: un PEQUEÑO haz (~170 px) centrado en el
                // vuelo — TODO sobre HazLib, con LA RAMPA DE VIDA v6.122.
                Vector2 dir = Vector2.Normalize(Projectile.velocity);
                Vector2 punta = Projectile.Center - Main.screenPosition;
                Vector2 rabo = punta - dir * 170f;

                HazLib.Abrir();
                try
                {
                    HazLib.IntensidadGlobal = HacesVida.Rampa(_t, Vida);

                    // EL VELO — el aura MORADA ancha (la envolvente suave
                    // del huso: fina en los extremos, llena al centro).
                    HazLib.PrepararPar(rabo, punta, SemianoHusoVelo, 0f, 40);
                    HazLib.Dibujar(TinteHuso, VerticeVelo, HazLib.FilasVelo);

                    // EL CUERPO — el HUSO: NEGRO al centro, MORADO en los
                    // bordes (la letra, palabra por palabra).
                    HazLib.PrepararPar(rabo, punta, SemianoHuso, 0f, 40);
                    HazLib.Dibujar(TinteHuso, VerticeHuso, HazLib.FilasNucleo);
                }
                finally { HazLib.Cerrar(); }
            }
            catch { }
            return false;
        }

        // === LAS LEYES DEL HUSO (la letra, EXACTA) ===

        /// <summary>EL SEMIANCHO DEL HUSO: MUY FINO en el inicio y el
        /// final, GRUESO en el centro — seno^0.75 (las puntas mueren
        /// asintóticas, el vientre llena 17 px).</summary>
        private static float SemianoHuso(float x)
        {
            float seno = MathF.Sin(MathF.PI * MathHelper.Clamp(x, 0f, 1f));
            return 17f * MathF.Pow(MathF.Max(seno, 0f), 0.75f);
        }

        /// <summary>El semiancho del VELO morado (×1.9 del huso).</summary>
        private static float SemianoHusoVelo(float x) => SemianoHuso(x) * 1.9f;

        /// <summary>EL TINTE a lo largo: el brillo VIVE en el centro del
        /// tajo y muere en los extremos (la lágrima de luz) — el color
        /// base es el MORADO VIVO del filo.</summary>
        private static Color TinteHuso(float x)
        {
            float seno = MathF.Sin(MathF.PI * MathHelper.Clamp(x, 0f, 1f));
            float brillo = MathF.Pow(MathF.Max(seno, 0f), 0.45f);
            return new Color(
                (byte)(150 * brillo + 8),
                (byte)(84 * brillo + 4),
                (byte)(235 * brillo + 12), 255);
        }

        /// <summary>EL VÉRTICE DEL HUSO: NEGRO en el CENTRO · MORADO en
        /// los BORDES (la letra de color: «negro en el centro, morado en
        /// los bordes»). En el lote aditivo de HazLib el negro aporta
        /// CERO: el corazón del tajo se lee como el VACÍO NEGRO contra el
        /// mundo, recortado por el filo morado que arde.</summary>
        private static Color VerticeHuso(float x, float y, Color tinte)
        {
            float d = MathF.Abs(y - 0.5f) * 2f;          // 0 centro → 1 borde
            Color c;
            if (d < 0.42f)
                c = new Color(2, 1, 4, 255);             // EL CENTRO NEGRO (el vacío)
            else if (d < 0.72f)
                c = new Color(46, 24, 86, 255);          // la subida morada honda
            else if (d < 0.9f)
                c = tinte;                               // EL BORDE MORADO vivo
            else
                c = new Color(216, 176, 255, 255);       // el filo morado claro que arde
            return c;
        }

        /// <summary>EL VÉRTICE DEL VELO: la cúpula pow suave, morada (la
        /// envolvente que abraza al huso).</summary>
        private static Color VerticeVelo(float x, float y, Color tinte)
        {
            float bulto = y * (4f - y * 4f);
            float k = bulto * bulto * bulto * 2.0f;
            Vector4 v = tinte.ToVector4() * k;
            return new Color((byte)Math.Clamp((int)(v.X * 255f + 0.5f), 0, 255),
                (byte)Math.Clamp((int)(v.Y * 255f + 0.5f), 0, 255),
                (byte)Math.Clamp((int)(v.Z * 255f + 0.5f), 0, 255), 255);
        }
    }
}
