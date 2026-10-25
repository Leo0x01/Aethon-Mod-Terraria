using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.Projectiles.Goozma;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Weapons.Goozma
{
    // ======================================================================
    //  v6.52.2 — LAS ARMAS DEL DIOS ANTIGUO (las copias de investigación
    //  de GOOZMA — Hunt of the Old God). Cuatro piezas + el material:
    //  EL RAYO DE FUSIÓN (el beam del video), EL LÁSER ARCOÍRIS (el
    //  láser curvo de los Goozmitas), EL DESTRUCTOR DE PRISMAS (el
    //  shuriken de 5 rayos) y EL TRUENO DE GELATINA (la descarga perlin)
    //  + LA MASA CROMÁTICA (el material arcoíris de todo el jefe).
    // ======================================================================

    /// <summary>
    /// EL RAYO DE FUSIÓN — la copia del ataque estrella del video: carga
    /// 70 t (la aguja arcoíris que late y las chispas que convergen) y
    /// dispara EL HAZ que barre en coseno — el arcoíris A LO LARGO del
    /// rayo, el ruido que fluye raíz→punta, la boca que florece cúbica.
    /// </summary>
    public class RayoDeFusion : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 85;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 230;
            Item.useAnimation = 230;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<RayoFusionProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 40;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.rare = ItemRarityID.Master;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item122;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            float ang = (Main.MouseWorld - player.MountedCenter).ToRotation();
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type,
                damage, knockback, player.whoAmI, 0f, ang, (Main.rand.NextBool() ? 1f : 0f));
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>
    /// EL LÁSER ARCOÍRIS — la copia del RainbowLaser: balas-láser CURVAS
    /// cuya estela es una pila de sprites con el arcoíris CORRIENDO a lo
    /// largo (el arma de los Goozmitas, a escala de jugador).
    /// </summary>
    public class LaserArcoiris : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 38;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 14;
            Item.useAnimation = 14;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<LaserArcoirisProjectile>();
            Item.shootSpeed = 14f;
            Item.mana = 7;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Master;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item12;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback,
                player.whoAmI, 0f, Main.rand.NextBool() ? 1f : 0f);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>
    /// EL DESTRUCTOR DE PRISMAS — la copia del PrismDestroyer: invoca
    /// TRES prismas que orbitan al jugador barriendo con 5 rayos de
    /// muerte en arcoíris HSL giratorio.
    /// </summary>
    public class DestructorDePrismas : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 26;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 150;
            Item.useAnimation = 150;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<PrismaDestructorProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 30;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Master;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item8;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            for (int i = 0; i < 3; i++)
                Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type,
                    damage, knockback, player.whoAmI, 0f, i * 2.1f);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>
    /// EL TRUENO DE GELATINA — la copia del GooLightning: descargas
    /// perlin arcoíris que saltan al cursor y golpean por línea.
    /// </summary>
    public class TruenoDeGelatina : ModItem
    {
        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.damage = 52;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 18;
            Item.useAnimation = 18;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<TruenoGelatinaProjectile>();
            Item.shootSpeed = 1f;
            Item.mana = 9;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Master;
            Item.value = Item.buyPrice(0, 0, 0, 0);
            Item.UseSound = SoundID.Item92;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL SALTO: del arma al punto del cursor (la longitud del original)
            Vector2 hasta = Main.MouseWorld;
            Vector2 delta = hasta - player.MountedCenter;
            if (delta.Length() > 1400f) hasta = player.MountedCenter + Vector2.Normalize(delta) * 1400f;
            Projectile.NewProjectile(source, player.MountedCenter, hasta - player.MountedCenter,
                type, damage, knockback, player.whoAmI);
            // LA LÍNEA DE DAÑO (el width 50 de los ticks 0-20 del original)
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n == null || !n.active || n.life <= 0 || n.friendly || n.townNPC) continue;
                    float punto = 0f;
                    if (Collision.CheckAABBvLineCollision(n.Hitbox.TopLeft(), n.Hitbox.Size(),
                        player.MountedCenter, hasta, 34f, ref punto))
                        n.StrikeNPC(new NPC.HitInfo
                        {
                            Damage = damage,
                            SourceDamage = damage,
                            DamageType = DamageClass.Magic,
                            Crit = false,
                            HitDirection = Math.Sign(delta.X),
                            Knockback = 4f,
                        }, false, true);
                }
            }
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }

    /// <summary>
    /// LA MASA CROMÁTICA — la copia del material de Goozma: el alma
    /// gelatinosa arcoíris (el «ChromaticMass» — la moneda de creación
    /// de todo su arsenal). Aquí: el material de pruebas con el icono
    /// arcoíris VIVO en el inventario.
    /// </summary>
    public class MasaCromatica : ModItem
    {
        public override string Texture => "AethonMod/Content/Weapons/Goozma/MasaCromatica";

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.maxStack = 999;
            Item.rare = ItemRarityID.Master;
            Item.value = Item.buyPrice(0, 1, 0, 0);
        }

        /// <summary>EL ARCOÍRIS VIVO en el inventario: el nombre se tiñe
        /// con el reloj de Goozma (la ventana del material late arcoíris).</summary>
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            foreach (TooltipLine linea in tooltips)
            {
                if (linea.Name == "ItemName" && linea.Mod == "Terraria")
                    linea.OverrideColor = ColorGoozma.ValueAt(Main.GlobalTimeWrappedHourly * 30f);
            }
        }

        public override bool PreDrawInInventory(SpriteBatch sb, Vector2 position, Rectangle frame,
            Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            // EL TINTE ARCOÍRIS del icono (el reloj de Goozma — vivo)
            sb.Draw(TextureAssets.Item[Type].Value, position, frame,
                ColorGoozma.ValueAt(Main.GlobalTimeWrappedHourly * 15f), 0f, origin, scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void PostUpdate()
        {
            if (Main.netMode != NetmodeID.Server)
                Lighting.AddLight(Item.position, ColorGoozma.ValueAt(Main.GlobalTimeWrappedHourly * 18f).ToVector3() * 0.4f);
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.Gel, 5).Register();
        }
    }
}
