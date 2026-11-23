using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AethonMod.Content.VFX;

namespace AethonMod.Content.Weapons.Clicker
{
    // =====================================================================
    //  ClickerDelUmbral — v6.168 — EL CLICKER DEFINITIVO (la letra:
    //  «borra la bolsa de clicker […] y crea el arma clicker
    //  definitiva, esta vez se super creativo, tienes prohibido usar
    //  texturas del proyecto, usa la sforja y las librerias»).
    //
    //  EL CONCEPTO — EL UMBRAL: la pluma del Cronista sellada en un
    //  artefacto de tinta y ley. CADA CLIC no golpea: deja UNA MARCA
    //  DE TINTA sobre el enemigo más cercano al cursor (dentro del
    //  radio de contrato, como todo clicker de la casa). Las marcas
    //  ACUMULAN (se ven: la tinta corre del cielo a la víctima). Al
    //  OCTAVO clic, EL UMBRAL SE ROMPE: TODAS las marcas vivas
    //  detonan A LA VEZ en UNA CADENA — cada marca golpea, y cada
    //  golpe SALTA al enemigo marcado más cercano (la sentencia se
    //  propaga). MÁS MARCAS = MÁS CadenA = más daño total. El
    //  especial escala con lo que SIEMBRES: un solo enemigo marcado
    //  8 veces recibe las 8 detoniciones; 8 enemigos marcados
    //  reciben la cadena saltando entre ellos.
    //
    //  EL KIT COMPLETO (nada de «todos iguales» — cada pieza es
    //  ÚNICA de este arma):
    //   · EL RADIO DE CONTRATO: 300 px, estilo propio — EL MARGEN:
    //     finas líneas de tinta VERTICALES cayendo dentro del aro
    //     (la lluvia del umbral) + la pluma orbitando (la firma).
    //   · EL CLIC: la marca de tinta (un proyectil invisble que se
    //     clava y VIVE — la tinta gotea desde el cielo hacia la
    //     víctima mientras la marca siga viva).
    //   · EL COMBO (8): LA ROTURA DEL UMBRAL — flash blanco, las
    //     marcas detona en cadena con SALTO (la sentencia viaja).
    //   · LA CADENCIA VIVA: cada clic consecutivo EN EL MISMO
    //     ENEMIGO acelera el useTime (la tinta reconoce a su
    //     víctima: 3 clics seguidos al mismo blanco = 25 % más
    //     rápido, 6 = 50 % — el ritmo recompensa la constancia).
    //
    //  LAS TEXTURAS: CERO del proyecto — el ítem dibuja su sprite
    //  PROCEDURAL (una pluma de tinta: barras y glow con las
    //  librerías de la casa) y TODO el efecto vive en el lote
    //  aditivo (VFXCore). Libre de derecho de autor: 100 % de la
    //  casa, dibujado en runtime.
    // =====================================================================
    public class ClickerDelUmbral : ModItem
    {
        public const float RadioContrato = 300f;
        public const byte ClicsParaRomper = 8;
        public static readonly Color TintaUmbral = new(148, 72, 178);
        public static readonly Color TintaClara = new(216, 186, 255);

        public override string Texture => "AethonMod/Content/Weapons/Clicker/ClickerCasa";   // sprite fantasma (el dibujo es procedural)

        public override void SetStaticDefaults()
        {
            // EL REGISTRO EN LA CLASE CLICKER DE LA CASA (el radio + la firma)
            ClickerCasa.Registrar(ModContent.ItemType<ClickerDelUmbral>(), RadioContrato, TintaUmbral,
                (EstiloClicker)8);   // estilo 8: EL MARGEN (la lluvia del umbral)
        }

        public override void SetDefaults()
        {
            Item.damage = 42;
            Item.DamageType = DamageClass.Magic;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;          // el dibujo es PROCEDURAL
            Item.autoReuse = false;            // cada clic cuenta (la ley del clicker)
            Item.mana = 0;
            Item.rare = ItemRarityID.Red;
            Item.value = Terraria.Item.buyPrice(0, 30, 0, 0);
            Item.shoot = ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Clicker.MarcaDeTinta>();
        }

        public override bool CanUseItem(Player player)
        {
            // LA LEY DEL CONTRATO: solo se puede clickear DENTRO del radio
            // (la casa: el círculo es la regla visible).
            return Vector2.DistanceSquared(Main.MouseWorld, player.Center)
                <= RadioContrato * RadioContrato;
        }

        public override bool? UseItem(Player player)
        {
            // EL RITMO VIVO: el sonido seco del sello (cada clic SUENA).
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item69.WithPitchOffset(0.35f)
                .WithVolumeScale(0.35f), player.Center);
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo fuente,
            Vector2 posicion, Vector2 velocidad, int tipo, int dano, float retroceso)
        {
            // ============================================================
            //  EL CLIC: LA MARCA DE TINTA — no un golpe: una SENTENCIA
            //  diferida. La marca nace del CIELO sobre el enemigo más
            //  cercano al cursor (dentro del radio) y se le clava. Si
            //  no hay enemigo, la tinta cae al cursor misma (la marca
            //  en el suelo: el umbral también marca el aire).
            // ============================================================
            NPC blanco = BlancoMasCercanoAlCursor(player);
            Vector2 destino = blanco != null ? blanco.Center : Main.MouseWorld;

            var cj = player.GetModPlayer<ClickerJugador>();
            int clics = cj.RegistraClic(ModContent.ItemType<ClickerDelUmbral>());
            bool romper = clics % ClicsParaRomper == 0;

            int idx = Projectile.NewProjectile(fuente, destino, Vector2.Zero,
                ModContent.ProjectileType<global::AethonMod.Content.Projectiles.Clicker.MarcaDeTinta>(),
                dano, 0f, player.whoAmI,
                romper ? 1f : 0f,                    // ai[0]: ¿esta marca ROMPE el umbral?
                blanco != null ? blanco.whoAmI + 1 : 0f);   // ai[1]: su víctima
            if (idx >= 0 && Main.projectile[idx].active)
                Main.projectile[idx].originalDamage = Item.damage;
            return false;   // el disparo es NUESTRO (la marca)
        }

        /// <summary>EL BLANCO: el enemigo más cercano al CURSOR dentro
        /// del contrato (la marca cae donde MIRAS, no donde estás).</summary>
        public static NPC BlancoMasCercanoAlCursor(Player p)
        {
            NPC mejor = null;
            float mejorD = float.MaxValue;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.life <= 0 || n.friendly ||
                    n.townNPC || n.immortal || n.dontTakeDamage) continue;
                float d = Vector2.DistanceSquared(n.Center, Main.MouseWorld);
                float dJ = Vector2.DistanceSquared(n.Center, p.Center);
                if (dJ > RadioContrato * RadioContrato) continue;   // fuera del contrato
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
        }

        // ================================================================
        //  EL DIBUJO PROCEDURAL DEL ÍTEM (en la mano): LA PLUMA DE
        //  TINTA — una pluma estilográfica de la casa, dibujada con
        //  quads y glow del lote (SIN textura del proyecto).
        // ================================================================
        public override bool PreDrawInWorld(SpriteBatch sb, Color luz, Color alfa,
            ref float rot, ref float esc, int whoAmI)
        {
            DibujarLaPluma(sb, Item.position - Main.screenPosition + new Vector2(Item.width / 2f, Item.height / 2f + 6f), luz, rot, esc, false);
            return false;
        }

        public override bool PreDrawInInventory(SpriteBatch sb, Vector2 pos, Rectangle rec,
            Color luz, Color alfa, Vector2 origen, float esc)
        {
            DibujarLaPluma(sb, pos + new Vector2(0f, rec.Height * 0.12f), luz, 0f, esc * 0.72f, true);
            return false;
        }

        /// <summary>LA PLUMA: caña vertical violeta + punta de tinta +
        /// plumero de 3 trazos + el brillo del umbral. TODO con quads.</summary>
        private static void DibujarLaPluma(SpriteBatch sb, Vector2 centro, Color luz,
            float rot, float esc, bool inventario)
        {
            try
            {
                Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
                Texture2D glow = VFXCore.SoftGlow;
                float l = Math.Max(0.35f, (luz.R + luz.G + luz.B) / (3f * 255f));
                void Barra(Vector2 pos, Vector2 tam, Color c)
                    => sb.Draw(pixel, pos, null, c * l, rot, tam * 0.5f, esc, SpriteEffects.None, 0f);

                // LA CAÑA (el cuerpo de la pluma — violeta del umbral)
                Barra(centro, new Vector2(3.2f, 26f), TintaUmbral);
                Barra(centro - new Vector2(0f, 6f), new Vector2(2.4f, 14f), TintaUmbral * 0.85f);
                // LA PUNTA (el acero que toca el papel)
                Barra(centro + new Vector2(0f, 15f), new Vector2(1.8f, 7f), new Color(224, 214, 235));
                // EL PLUMERO (los tres trazos que respiran)
                for (int k = -1; k <= 1; k++)
                    Barra(centro - new Vector2(0f, 15f) + new Vector2(k * 2.2f, 0f),
                        new Vector2(1.6f, 10f), Color.Lerp(TintaClara, TintaUmbral, 0.35f));
                // EL BRILLO del umbral (la tinta viva en la punta)
                sb.Draw(glow, centro + new Vector2(0f, 15f), null,
                    VFXCore.TintAlfa(TintaClara, 0.55f * l), rot, glow.Size() * 0.5f,
                    0.28f * esc, SpriteEffects.None, 0f);
            }
            catch { }
        }
    }
}
