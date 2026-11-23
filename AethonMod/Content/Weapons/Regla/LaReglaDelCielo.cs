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
    //  LaReglaDelCielo — v6.170 — LA KATANA DEL CIELO (la letra: «yo
    //  necesito tajos, tajos iguales a los de las katanas en los
    //  animes […] intenta recrealor en el arma la regla del cielo»).
    //
    //  LA RECONVERSIÓN: la línea vertical de la v6.168 MUERE — el
    //  ataque es LA MEDIA LUNA DEL KATANA DE ANIME (investigada: la
    //  «glowing crescent» cel-shaded de bordes afilados): un arco de
    //  ~110° que nace EN EL CURSOR, inclinado en DIAGONAL (~35°, el
    //  gesto del espadachín), con el filo blanco-caliente y el halo
    //  azul eléctrico, revelado de punta a punta en 4 ticks (LA
    //  VELOCIDAD de la hoja) y una vida ultracorta de 13 ticks (el
    //  corte OCURRE, no se desarrolla).
    //
    //  EL SENTIDO DE LA HOJA (la letra de la v6.168 vive): el tajo
    //  corre ARRIBA→ABAJO o ABAJO→ARRIBA al azar — el arco se voltea
    //  según el gesto.
    //
    //  EL SPRITE: LA REGLA — una katana de cielo (la hoja larga y
    //  delgada con su guarda estelar), forjada con LA SUPERFORJA de
    //  la casa (la letra: «siempre se te olvida usar la sforja para
    //  creacion de pixel art»).
    //
    //  EL DAÑO: la media luna corta UNA VEZ al nacer — todo lo que
    //  vive en la BANDA del arco (la boca de la luna) recibe el tajo
    //  (GolpeMotor, el contrato del Tajo Astral).
    // =====================================================================
    public class LaReglaDelCielo : ModItem
    {
        /// <summary>El radio de la media luna (px — «largas en
        /// longitud» viva como el ALCANCE del arco).</summary>
        private const float RADIO = 150f;

        /// <summary>v6.172 — EL CONTADOR DEL COMBO (0..3): al llegar a
        /// 4 usos el próximo tajo es EL CRUZADO (la X doble).</summary>
        private static int _combo;

        public override string Texture => "AethonMod/Content/Weapons/Regla/LaReglaDelCielo";

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
            // ============================================================
            //  LA SIEMBRA DE LA MEDIA LUNA: el tajo nace EN EL CURSOR
            //  (la letra v6.168 vive) con su geometría completa:
            //   · ai[1] = la INCLINACIÓN EXTRA hacia la presa ×1000
            //     (v6.171: SIN localAI[3] — es float[3], la lección
            //     de la casa; VIAJA por el cable vía NewProjectile)
            //   · ai[2] = el SENTIDO de la hoja (0 baja · 1 sube — al azar)
            //   · localAI[0..1] = el CENTRO del arco (el cursor)
            //   · localAI[2] = el RADIO
            // ============================================================
            bool sube = Main.rand.NextBool();
            // v6.172 — EL CONTADOR DEL COMBO (ai[0]): cada CUARTO uso
            // desata EL TAJO CRUZADO (la X del doble corte anime —
            // TajoLib.TajoCruzado). El contador vive en el ítem (es
            // el gesto local del portador).
            _combo = (_combo + 1) % 4;
            bool cruzado = _combo == 0;
            // LA ABRAZADA A LA PRESA: un blanco cerca del cursor
            // inclina el arco hacia él (±0.5 rad máximo) — en ai[1] ×1000
            float inclinacionExtra = 0f;
            NPC presa = PresaCerca(Main.MouseWorld, RADIO);
            if (presa != null)
            {
                Vector2 rel = presa.Center - Main.MouseWorld;
                if (rel.Length() > 24f)
                    inclinacionExtra = MathHelper.Clamp(
                        MathF.Atan2(rel.Y, rel.X) -
                        (sube ? -MathHelper.PiOver2 : MathHelper.PiOver2),
                        -0.5f, 0.5f);
            }
            int idx = Projectile.NewProjectile(fuente, Main.MouseWorld, Vector2.Zero,
                tipo, dano, 0f, player.whoAmI, cruzado ? 4f : 0f,
                inclinacionExtra * 1000f, sube ? 1f : 0f);
            if (idx >= 0 && idx < Main.maxProjectiles && Main.projectile[idx].active)
            {
                Projectile p = Main.projectile[idx];
                p.localAI[0] = Main.MouseWorld.X;
                p.localAI[1] = Main.MouseWorld.Y;
                p.localAI[2] = RADIO;
                p.originalDamage = Item.damage;
            }
            // EL SONIDO DEL DESEMBRAINE (el silbido corto de la hoja)
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item72.WithPitchOffset(0.55f)
                .WithVolumeScale(0.28f), player.Center);
            return false;
        }

        /// <summary>El enemigo más cercano al punto (dentro del radio
        /// del arco) — para la abrazada de la media luna.</summary>
        private static NPC PresaCerca(Vector2 punto, float radio)
        {
            NPC mejor = null;
            float mejorD = radio * radio;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n == null || !n.active || n.life <= 0 || n.friendly ||
                    n.townNPC || n.immortal || n.dontTakeDamage) continue;
                float d = Vector2.DistanceSquared(n.Center, punto);
                if (d < mejorD) { mejorD = d; mejor = n; }
            }
            return mejor;
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
    }
}
