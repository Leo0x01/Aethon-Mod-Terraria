using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    //  TajoOscuroProj — v6.124 — LA AGUJA DEL TAJO CON SPRITE DE LA
    //  SFORJA (la letra: «todo proyectil debe ser creado con la
    //  superforja, en este caso con la nueva que sera creada SForja»).
    //
    //  LA AUTOPSIA de la v6.122: el tajo se dibujaba con bandas HazLib
    //  — proyectil SIN sprite propio. AHORA el filo ES UN SPRITE nacido
    //  de la receta `tajo` de tools/sforja.py: LA HOJA LLENA POR CAPAS
    //  con la letra de color EXACTA — CENTRO NEGRO · medios morados ·
    //  BORDES BLANCOS — y PUNTAS DE AGUJA en los extremos (el que se
    //  clava). El sprite se ESTIRA a lo largo del vuelo (300 px de
    //  cuerpo visible) con LA RAMPA DE VIDA v6.122 (nace y muere
    //  suave) y su estela sigue siendo la de la casa.
    // ======================================================================
    public class TajoOscuroProj : ModProjectile
    {
        // EL SPRITE DEL TAJO — la receta `tajo` de la SForja (120×36)
        public override string Texture => "AethonMod/Content/Effects/Procedural/TajoOscuroProj";

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
                // EL TAJO: el sprite de la SForja ESTIRADO a lo largo del
                // vuelo (el cuerpo entero de 300 px + la Rampa de Vida
                // v6.122 — nace y muere suave, jamás un corte brusco).
                Vector2 punta = Projectile.Center - Main.screenPosition;
                float rot = Projectile.velocity.ToRotation();
                float rampa = HacesVida.Rampa(_t, Vida);

                Main.instance.LoadProjectile(Projectile.type);
                Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
                if (tex != null)
                {
                    float escalaX = 300f / tex.Width;      // el cuerpo de 300 px
                    float escalaY = 1.0f;
                    Vector2 origen = new Vector2(tex.Width * 0.5f, tex.Height * 0.5f);
                    // EL NÚCLEO del tajo (lote alfa — el CENTRO NEGRO es
                    // negro de verdad, el aditivo lo lavaría)
                    Main.spriteBatch.Draw(tex, punta, null,
                        Color.White * rampa, rot, origen,
                        new Vector2(escalaX, escalaY), SpriteEffects.None, 0f);
                }

                // EL VELO MORADO del vuelo (aditivo — la estela que arde)
                VFXCore.AbrirAdditive();
                try
                {
                    if (tex != null)
                    {
                        Vector2 origen = new Vector2(tex.Width * 0.5f, tex.Height * 0.5f);
                        Main.spriteBatch.Draw(tex, punta, null,
                            new Color(150, 84, 235) * (0.30f * rampa), rot, origen,
                            new Vector2(300f / tex.Width * 1.35f, 1.55f),
                            SpriteEffects.None, 0f);
                    }
                }
                finally { VFXCore.RestaurarLoteVanilla(); }
            }
            catch { }
            return false;
        }
    }
}
