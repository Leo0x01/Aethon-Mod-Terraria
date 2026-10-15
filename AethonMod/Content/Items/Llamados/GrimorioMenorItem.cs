using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AethonMod.Content.Items.Llamados
{
    // ======================================================================
    //  v6.50.122 — EL GRIMORIO MENOR: LA SEGUNDA MASCOTA (la miniatura
    //  del LIBRO — no confundir con el Aethon Menor, la miniatura del
    //  JEFE de luz: son DOS mascotas distintas, la letra del usuario
    //  que desenredó los conceptos).
    //
    //  Vive dentro de la Bolsa de la Cría junto a su hermana de luz —
    //  la bolsa llega en cada entrada al mundo (TestingPlayer, como las
    //  veintiuna bolsas del kit) y tras la primera victoria de la
    //  familia (la vía canónica de la .120).
    //
    //  v6.50.124 — DOS CURAS DE LA MISMA LETRA:
    //  (1) «el icono de su item no tiene animacion ni movimiento en los
    //      ojos» — EL OJO VIVO DEL ICONO: el icono (24×33) es el resize
    //      del libro del Nervioso y su esclerótica quedó VACÍA (el iris
    //      del arma/mascota siempre se dibuja ENCIMA, nunca viaja en la
    //      textura). AHORA el item dibuja la PUPILA cada frame encima
    //      del icono (PostDrawInInventory + PreDrawInWorld): MIRA de
    //      lado a lado (dos senos superpuestos, deterministas — el
    //      reloj del mundo), RESPIRA (±6%) y PARPADEA cerrando el ojo
    //      (el overlay _Cerrado: el MISMO resize LANCZOS del libro
    //      cerrado — alineado al píxel, verificación byte a byte en
    //      tools/gen_grimorio_menor_v650124.py + VLM SHIP).
    //  (2) la ranura de mascota — el flag vive en el BUFF
    //      (GrimorioMenorBuff.Main.vanityPet, verificado al IL del
    //      tML 2026.08.3.0: ItemSlot caso 19) — este ítem ya era
    //      canónico (buffType + shoot, el disparo lo engendra).
    // ======================================================================
    /// <summary>
    /// GrimorioMenorItem — EL GRIMORIO MENOR (la mascota minigrimorio).
    ///
    /// Invoca la miniatura viva del Grimorio del Eterno: flota a tu
    /// hombro con su ojo despierto (Y EL DEL ICONO TAMBIÉN), se aburre
    /// si te quedas quieto y vuela a MOLESTAR a los vecinos con sus
    /// mini minions y su sombrilla de página. SE EQUIPA en la ranura
    /// de mascota del equipo (vanityPet — la .124).
    /// </summary>
    public class GrimorioMenorItem : ModItem
    {
        // === EL OJO DEL ICONO (medido del PNG real: esclerótica blanca
        //     x 8-16, y 13-17 → centro (12, 15) — la verificación vive
        //     en el generador) ===
        private static readonly Vector2 OjoIcono = new(12.0f, 15.0f);

        /// <summary>La escala del iris del libro (0.2773) llevada a la
        /// escala del icono (24/36) → pupila ≈ 5,9 px.</summary>
        private const float IrisEscalaIcono =
            global::AethonMod.Content.Weapons.GrimorioHambriento.IRIS_ESC * (24f / 36f);

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 1;
            Item.consumable = false;              // reutilizable: es de pruebas
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.UseSound = SoundID.Item4;
            Item.rare = ItemRarityID.Quest;
            Item.value = Item.buyPrice(gold: 5);
            Item.buffType = ModContent.BuffType<global::AethonMod.Content.Buffs.GrimorioMenorBuff>();
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Cosmetic.GrimorioMenorPet>();
        }

        public override bool? UseItem(Player player)
        {
            // El buff nace con el uso (y AHORA sí vive: la mascota lo
            // sostiene Y el buff la re-engendra — el patrón canónico).
            if (player.whoAmI == Main.myPlayer)
                player.AddBuff(Item.buffType, 3600, true);
            return true;
        }

        // ================================================================
        //  EL OJO VIVO — la mirada y el párpado (deterministas: el reloj
        //  del mundo — todas las pantallas ven EL MISMO ojo)
        // ================================================================

        /// <summary>LA MIRADA: dos senos superpuestos por eje (el ojo
        /// deambula suave, sin repetir un ciclo obvio) — clampeado a lo
        /// que la esclerótica del icono permite (±0,95 / ±0,60 texels).</summary>
        private static Vector2 Mirada(float t)
        {
            float x = MathF.Sin(t * 0.55f) * 1.15f + MathF.Sin(t * 0.23f + 1.7f) * 0.55f;
            float y = MathF.Sin(t * 0.41f + 0.6f) * 0.80f + MathF.Sin(t * 0.17f) * 0.40f;
            return new Vector2(Math.Clamp(x, -0.95f, 0.95f), Math.Clamp(y, -0.60f, 0.60f));
        }

        /// <summary>EL PARPADEO: cada ~4,6 s el libro cierra el ojo
        /// ~0,22 s (el mismo compás del flotante grande — la familia).</summary>
        private static bool Parpadeando(float t)
        {
            float f = (t * 0.9f) % 4.6f;
            return f > 4.38f;
        }

        private static Texture2D TexIcono()
            => Terraria.GameContent.TextureAssets.Item[ModContent.ItemType<GrimorioMenorItem>()].Value;

        private static Texture2D TexCerrado()
            => ModContent.Request<Texture2D>(
                "AethonMod/Content/Items/Llamados/GrimorioMenorItem_Cerrado").Value;

        private static Texture2D TexIris()
            => ModContent.Request<Texture2D>(
                "AethonMod/Content/Weapons/GrimorioHambriento_Iris").Value;

        // ================================================================
        //  EL INVENTARIO — la pupila se dibuja ENCIMA del icono base
        //  (position = el CENTRO del sprite; origin = frame/2 — la
        //  semántica exacta del ItemSlot de ESTE tML, leída del parche).
        // ================================================================
        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position,
            Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (Main.dedServ) return;
            try
            {
                float t = Main.GlobalTimeWrappedHourly;

                // EL PÁRPADO: el icono CERRADO, con LOS MISMOS parámetros
                // del dibujado base — alineación perfecta por construcción.
                if (Parpadeando(t))
                {
                    spriteBatch.Draw(TexCerrado(), position, frame, drawColor,
                        0f, origin, scale, SpriteEffects.None, 0f);
                    return;
                }

                // LA PUPILA: el iris del libro en la esclerótica + la
                // mirada deambulante + la respiración (±6%).
                Texture2D iris = TexIris();
                Vector2 posOjo = position + (OjoIcono - origin) * scale + Mirada(t) * scale;
                float esc = IrisEscalaIcono * (1f + 0.06f * MathF.Sin(t * 2.2f));
                spriteBatch.Draw(iris, posOjo, null, drawColor,
                    0f, iris.Size() * 0.5f, esc * scale, SpriteEffects.None, 0f);

                // EL DESTELLO de la córnea: un puntito vivo arriba-izquierda
                // de la pupila (el brillo del ojo que TE mira).
                Texture2D base_ = TexIcono();
                Vector2 brillo = posOjo + new Vector2(-1.4f, -1.6f) * scale;
                spriteBatch.Draw(base_, brillo,
                    new Rectangle(11, 13, 1, 1), Color.White * 0.90f,
                    0f, Vector2.Zero, MathF.Max(1f, 1.35f * scale), SpriteEffects.None, 0f);
            }
            catch { }
        }

        // ================================================================
        //  EL MUNDO — el libro tirado en el suelo TAMBIÉN te mira: se
        //  dibuja ENTERO aquí (return false) con su propio vaivén, para
        //  que la pupila caiga EXACTA en la esclerótica (sin adivinar
        //  los parámetros del dibujado de drops de vanilla).
        // ================================================================
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor,
            Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return true;
            try
            {
                float t = Main.GlobalTimeWrappedHourly;
                Texture2D tex = TexIcono();

                // El vaivén del libro tirado (su propio bobbing — es un
                // libro VIVO incluso en el suelo).
                float bob = MathF.Sin(t * 2.2f + whoAmI * 1.3f) * 2.6f;
                float giro = MathF.Sin(t * 1.1f + whoAmI) * 0.06f;
                Vector2 centro = Item.Center - Main.screenPosition + new Vector2(0f, bob);

                spriteBatch.Draw(tex, centro, null, lightColor,
                    giro, tex.Size() * 0.5f, scale, SpriteEffects.None, 0f);

                if (Parpadeando(t))
                {
                    spriteBatch.Draw(TexCerrado(), centro, null, lightColor,
                        giro, tex.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                }
                else
                {
                    Texture2D iris = TexIris();
                    Vector2 posOjo = centro + (OjoIcono - tex.Size() * 0.5f) * scale
                        + Mirada(t) * scale + new Vector2(0f, bob * 0f);
                    // la pupila gira con el libro (la convención .84)
                    Vector2 alOjo = posOjo - centro;
                    posOjo = centro + alOjo.RotatedBy(giro);
                    float esc = IrisEscalaIcono * (1f + 0.06f * MathF.Sin(t * 2.2f));
                    spriteBatch.Draw(iris, posOjo, null, lightColor,
                        giro, iris.Size() * 0.5f, esc * scale, SpriteEffects.None, 0f);
                }
                return false;   // el libro tirado SE dibuja aquí (con su ojo)
            }
            catch
            {
                return true;    // ante cualquier duda, el dibujado de vanilla
            }
        }

        // v6.50.50/.122 — SIN RECETA: la mascota se ENTREGA al entrar al
        // mundo (la Bolsa de la Cría del kit + la vía canónica de la
        // victoria) — el mod es de PRUEBAS.
    }
}
