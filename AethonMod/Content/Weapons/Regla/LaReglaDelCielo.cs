using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Weapons.Regla
{
    // =====================================================================
    //  LaReglaDelCielo — v6.168 — EL ARMA NUEVA (la letra: «crea una
    //  nueva arma, su ataque seran lineas en el aire a gran velocidad,
    //  lineas con glow, desenfoque y un poco de turbulencia al final,
    //  estas lineas empezaran con un brillo de arriba hacia abajo o de
    //  abajo hacia arriba de forma aleatoria y seran creadas en el
    //  cursor, seran largas en longitud y muy delgadas»).
    //
    //  LA LECTURA DE LA LETRA, PUNTO POR PUNTO:
    //   · «lineas en el aire a gran velocidad» — las líneas nacen YA
    //     EN EL CURSOR (no viajan hasta él) y su VIDA es corta: el
    //     «gran velocidad» es el LATIGAZO del brillo que las recorre.
    //   · «empezaran con un brillo de arriba hacia abajo o de abajo
    //     hacia arriba de forma aleatoria» — cada línea SUELTA su
    //     destello inicial en UNO de los dos extremos (al azar) y el
    //     brillo CORRE la línea en esa dirección antes de apagarse.
    //   · «creadas en el cursor» — nacen en Main.MouseWorld.
    //   · «largas en longitud y muy delgadas» — 320 px de alto por
    //     ~6 px de ancho (la regla del cielo).
    //   · «glow, desenfoque y un poco de turbulencia al final» — el
    //     CUERPO: núcleo blanco + glow aditivo de la casa; EL
    //     DESENFOQUE: dos capas de glow más ancho y más tenue; LA
    //     TURBULENCIA: el EXTREMO FINAL de la línea (donde murió el
    //     brillo) se ONDEA — la punta escribe un temblor de 3-4 px
    //     antes de disiparse en chispas.
    //
    //  EL DAÑO: la línea es UNA COLUMNA de golpe (como un rayo caído):
    //  golpea TODO lo que toca su rectángulo delgado al nacer (una
    //  pasada) — el arma es de CADENCIA (useTime 14) y maná barato.
    //  LIBRE DE DERECHOS: sin textura del proyecto — TODO procedural
    //  (el lote aditivo + MagicPixel), dibujado en runtime.
    // =====================================================================
    public class LaReglaDelCielo : ModItem
    {
        public override string Texture => "AethonMod/Content/Weapons/Clicker/ClickerCasa";   // fantasma (procedural)

        public override void SetDefaults()
        {
            Item.damage = 58;
            Item.DamageType = DamageClass.Magic;
            Item.useTime = 14;
            Item.useAnimation = 14;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.mana = 6;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Terraria.Item.buyPrice(0, 20, 0, 0);
            Item.shoot = ModContent.ProjectileType
                <global::AethonMod.Content.Projectiles.Regla.LineaDelCielo>();
            Item.shootSpeed = 1f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo fuente,
            Vector2 posicion, Vector2 velocidad, int tipo, int dano, float retroceso)
        {
            // LA LÍNEA NACE EN EL CURSOR (la letra exacta) — con su
            // dirección de brillo AL AZAR (arriba→abajo o abajo→arriba).
            int idx = Projectile.NewProjectile(fuente, Main.MouseWorld, Vector2.Zero,
                tipo, dano, 0f, player.whoAmI,
                Main.rand.NextBool() ? 0f : 1f);   // ai[0]: 0 = baja, 1 = sube
            if (idx >= 0 && Main.projectile[idx].active)
                Main.projectile[idx].originalDamage = Item.damage;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item72.WithPitchOffset(0.4f)
                .WithVolumeScale(0.30f), player.Center);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType
                    <global::AethonMod.Content.Items.Altar.ItemCorrupto>(), 15)
                .AddIngredient(ItemID.FallenStar, 8)
                .AddIngredient(ItemID.SoulofLight, 5)
                .AddTile(ModContent.TileType
                    <global::AethonMod.Content.Tiles.AltarOfrendasTile>())
                .Register();
        }

        // EL DIBUJO PROCEDURAL DEL ÍTEM: LA REGLA — una barra larga y
        // delgada con sus marcas de medición y el brilla del cielo.
        public override bool PreDrawInWorld(SpriteBatch sb, Color luz, Color alfa,
            ref float rot, ref float esc, int whoAmI)
        {
            DibujarLaRegla(sb, Item.position - Main.screenPosition +
                new Vector2(Item.width / 2f, Item.height / 2f + 8f), luz, rot, esc);
            return false;
        }

        public override bool PreDrawInInventory(SpriteBatch sb, Vector2 pos, Rectangle rec,
            Color luz, Color alfa, Vector2 origen, float esc)
        {
            DibujarLaRegla(sb, pos + new Vector2(0f, rec.Height * 0.10f), luz, 0f, esc * 0.55f);
            return false;
        }

        private static void DibujarLaRegla(SpriteBatch sb, Vector2 centro, Color luz,
            float rot, float esc)
        {
            try
            {
                Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
                Texture2D glow = VFXCore.SoftGlow;
                float l = Math.Max(0.35f, (luz.R + luz.G + luz.B) / (3f * 255f));
                void Barra(Vector2 pos, Vector2 tam, Color c)
                    => sb.Draw(pixel, pos, null, c * l, rot, tam * 0.5f, esc, SpriteEffects.None, 0f);
                // EL CUERPO: la regla larga y delgada (blanco-cielo)
                Barra(centro, new Vector2(3f, 34f), new Color(190, 230, 255));
                // LAS MARCAS de medición (los travesaños de la regla)
                for (int k = -2; k <= 2; k++)
                    Barra(centro + new Vector2(0f, k * 7f), new Vector2(k % 2 == 0 ? 7f : 5f, 1.6f),
                        new Color(120, 190, 240));
                // EL BRILLO del cielo (la línea viva)
                sb.Draw(glow, centro, null, VFXCore.TintAlfa(new Color(140, 210, 255), 0.40f * l),
                    rot, glow.Size() * 0.5f, 0.30f * esc, SpriteEffects.None, 0f);
            }
            catch { }
        }
    }
}
