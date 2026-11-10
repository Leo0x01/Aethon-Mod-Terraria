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
    //  v6.122 — EL Tajo de Oscuridad (la letra del usuario: «tambien
    //  quiero que crees usando la superforja un tajo de oscuridad, con
    //  centro negro, bordes blancos y medios morados, este tajo aparece
    //  de un extremo a otro dependiendo de la direccion, cuando se usa
    //  el arma aparecen tajos que como agujas se clavan y atraviezan a
    //  los enemigos, estos aparecen siempre cerca del enemigo»).
    //
    //  EL FILTRO DE LA OSCURIDAD — la letra de color, EXACTA:
    //    · EL CENTRO: NEGRO absoluto (la nada que corta).
    //    · LOS MEDIOS: MORADOS (violeta hondo arriba, violeta vivo
    //      abajo — la carne del vacío).
    //    · LOS BORDES: BLANCOS brillantes (el filo que arde).
    //  Todo dibujado sobre HazLib (el filo de borde rgb=0 de la casa:
    //  imposible que un pico encienda) con LA RAMPA DE VIDA v6.122
    //  (nace y muere suave — jamás un corte brusco).
    //
    //  EL MODO: al usar el arma, HASTA TRES TAJOS nacen CERCA de los
    //  enemigos (los más cercanos al cursor) y SE CLAVAN como agujas:
    //  cada tajo aparece ~150 px DETRÁS de su presa (en la línea de la
    //  dirección del disparo) y LA ATRAVIESA a toda velocidad — de un
    //  extremo a otro de la pantalla según la dirección, dañando todo
    //  lo que el filo toca (perforación total).
    // ======================================================================
    public class TajoOscuro : ModItem
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
            Item.shoot = ModContent.ProjectileType<TajoOscuroProj>();
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
            Vector2 dir = (Main.MouseWorld - player.Center);
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

                // EL Tajo NACE 150 px DETRÁS de la presa (en el eje de la
                // dirección) y LA ATRAVIESA — la aguja que se clava.
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
    //  TajoOscuroProj — LA AGUJA DEL TAJO: el filo largo que se clava y
    //  atraviesa (centro negro · medios morados · bordes blancos).
    // ======================================================================
    public class TajoOscuroProj : ModProjectile
    {
        public override string Texture => "AethonMod/Content/Effects/Procedural/Star";

        private const float Vida = 46f;
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
                // LA ESTELA DE LA OSCURIDAD (motas violetas-negras)
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                        -Projectile.velocity * 0.12f + Main.rand.NextVector2Circular(1f, 1f),
                        0, Main.rand.NextBool(3) ? new Color(220, 200, 255) : new Color(90, 50, 160), 0.8f);
                    d.noGravity = true;
                }
                // LA LUZ DEL FILO BLANCO
                Lighting.AddLight(Projectile.Center, new Vector3(0.5f, 0.3f, 0.8f));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return false;
            try
            {
                // EL TAJO: del rabo (300 px atrás) a la punta — TODO sobre
                // HazLib, con LA RAMPA DE VIDA v6.122.
                Vector2 punta = Projectile.Center - Main.screenPosition;
                Vector2 dir = Vector2.Normalize(Projectile.velocity);
                Vector2 rabo = punta - dir * 300f;

                HazLib.Abrir();
                try
                {
                    HazLib.IntensidadGlobal = HacesVida.Rampa(_t, Vida);

                    // EL VELO — los MEDIOS MORADOS (la envolvente ancha)
                    HazLib.PrepararPar(rabo, punta, SemianoTajoVelo, 0f, 34);
                    HazLib.Dibujar(TinteTajoVelo, VerticeTajoVelo, HazLib.FilasVelo);

                    // EL CUERPO — CENTRO NEGRO con BORDES BLANCOS
                    HazLib.PrepararPar(rabo, punta, SemianoTajo, 0f, 40);
                    HazLib.Dibujar(TinteTajo, VerticeTajo, HazLib.FilasNucleo);
                }
                finally { HazLib.Cerrar(); }
            }
            catch { }
            return false;
        }

        // === LAS LEYES DEL TAJO (la letra de color, EXACTA) ===

        /// <summary>El semiancho del cuerpo: fino en la punta (la aguja
        /// que se clava), lleno en el rabo — 14→26 px.</summary>
        private static float SemianoTajo(float x)
            => 15f + 11f * x;

        /// <summary>El semiancho del velo morado (×1.9 del cuerpo).</summary>
        private static float SemianoTajoVelo(float x)
            => (15f + 11f * x) * 1.9f;

        /// <summary>EL TINTE del cuerpo a lo largo: el charco blanco de
        /// la punta (donde se clava) vira al negro del centro.</summary>
        private static Color TinteTajo(float x)
            => Color.Lerp(new Color(20, 12, 32), new Color(8, 5, 14), x);

        /// <summary>EL TINTE del velo: los MEDIOS MORADOS.</summary>
        private static Color TinteTajoVelo(float x)
            => Color.Lerp(new Color(120, 70, 200), new Color(74, 40, 140), x);

        /// <summary>EL VÉRTICE del cuerpo: CENTRO NEGRO · medios
        /// morados · BORDES BLANCOS (la letra, palabra por palabra).</summary>
        private static Color VerticeTajo(float x, float y, Color tinte)
        {
            float d = MathF.Abs(y - 0.5f) * 2f;          // 0 centro → 1 borde
            Color c;
            if (d < 0.5f)
                c = new Color(8, 5, 14, 255);            // EL CENTRO NEGRO
            else if (d < 0.78f)
                c = new Color(58, 30, 104, 255);         // el medio morado hondo
            else if (d < 0.92f)
                c = new Color(150, 84, 235, 255);        // el medio morado vivo
            else
                c = new Color(255, 252, 245, 255);       // EL BORDE BLANCO
            // EL PUNTAZO: la punta del tajo ARDE blanca (donde se clava)
            float clava = 1f - MathHelper.Clamp(x / 0.12f, 0f, 1f);
            if (clava > 0f && d < 0.9f)
                c = Color.Lerp(c, new Color(255, 252, 245, 255), clava * 0.85f);
            return c;
        }

        /// <summary>EL VÉRTICE del velo: la cúpula pow suave, morada.</summary>
        private static Color VerticeTajoVelo(float x, float y, Color tinte)
        {
            float bulto = y * (4f - y * 4f);
            float k = bulto * bulto * bulto * 2.2f;
            Vector4 v = tinte.ToVector4() * k;
            return new Color((byte)Math.Clamp((int)(v.X * 255f + 0.5f), 0, 255),
                (byte)Math.Clamp((int)(v.Y * 255f + 0.5f), 0, 255),
                (byte)Math.Clamp((int)(v.Z * 255f + 0.5f), 0, 255), 255);
        }
    }
}
