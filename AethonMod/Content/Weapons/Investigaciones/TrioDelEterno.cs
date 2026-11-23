using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AethonMod.Content.Weapons.Investigaciones
{
    // ======================================================================
    //  EL TRÍO DEL ETERNO — v6.159 — EL ARMA DE LAS TRES ARMAS.
    //
    //  LA LETRA: «debes crear una nueva arma, esta arma nueva no
    //  dispara nada, pero tendra una nueva mecanica, en la que se le
    //  pondran hasta 3 armas de terraria y entonces el arma nueva
    //  usara sus proyectiles para atacar, entendiste».
    //
    //  LA MECÁNICA: EL ATRIL DEL FUSIONADO — un atrio de tinta que
    //  SOSTIENE hasta TRES armas (cualquier arma de Terraria o de
    //  mods: la ranura las acepta enteras, con su daño y SU
    //  proyectil). EL TRÍO NO DISPARA NADA PROPIO: al atacar, el
    //  proyectil que sale es EL DE CADA ARMA INSERTADA, por turnos —
    //  la primera ranura dispara, luego la segunda, luego la tercera,
    //  y vuelta a empezar (el daño es EL DE CADA ARMA, respetando el
    //  escalado del portador; el libro de la casa no mete mano).
    //
    //   · Clic DERECHO sobre el Trío con un arma EN EL CURSOR → la
    //     INSERTA en la primera ranura libre (máx 3).
    //   · Clic DERECHO con el cursor VACÍO → EXTRAE la última arma
    //     insertada (vuelve al cursor, intacta).
    //   · Clic IZQUIERDO → el ataque: la vaina del arma del turno
    //     dispara SU proyectil hacia el cursor (Shoot de la casa
    //     clonando los parámetros de vanilla de ESA arma).
    //   · El tooltip ENSEÑA las tres ranuras (ocupadas y libres).
    //
    //  LA PERSISTENCIA: las tres armas viajan DENTRO del ítem
    //  (ItemIO.Save/Load en SaveData/LoadData + NetSend/NetReceive —
    //  el patrón del altar de la casa).
    // ======================================================================
    public class TrioDelEterno : ModItem
    {
        /// <summary>LAS TRES RANURAS — las armas insertadas (null/air =
        /// ranura libre). Viajan con el ítem.</summary>
        public Item[] Ranuras = new Item[3];

        /// <summary>El turno del ataque (0..2): qué ranura dispara.</summary>
        public int Turno;

        // ================================================================
        //  v6.161 — LA MESA DEL ALTAR (la letra: «mejora la ventana que
        //  aparece al tocar el altar, que esta ventana tenga un
        //  apartado donde se pueda usar para fusionar y desfusionar el
        //  arma el trio eterno»): EL TRÍO DEL JUGADOR LOCAL, leído por
        //  la ventana del altar (los TRES slots de fusión viven en el
        //  panel). El clic derecho del atril sigue vivo como camino
        //  corto — pero el altar es LA MESA de fusionar de verdad.
        // ================================================================

        /// <summary>EL TRÍO del inventario del portador (o null si no
        /// lleva ninguno).</summary>
        public static TrioDelEterno Del(Player p)
        {
            try
            {
                int tipo = ModContent.ItemType<TrioDelEterno>();
                if (p == null || !p.active) return null;
                Item mano = p.HeldItem;
                if (mano != null && !mano.IsAir && mano.type == tipo)
                    return mano.ModItem as TrioDelEterno;
                for (int i = 0; i < 58; i++)
                {
                    Item it = p.inventory[i];
                    if (it != null && !it.IsAir && it.type == tipo)
                        return it.ModItem as TrioDelEterno;
                }
            }
            catch { }
            return null;
        }

        /// <summary>¿ES UN ARMA VÁLIDA para el atril? v6.162 — SOLO
        /// ARMAS MÁGICAS (la letra: «como limitacion pon que solo se
        /// puedan usar armas magicas, no armas de invocador, no armas
        /// mele, no armas distancia, solo armas magicas»): daño > 0,
        /// que SU clase de daño sea Mágica, y no el propio Trío. Nada
        /// de espadas, arcos ni látigos.</summary>
        public static bool EsArmaValida(Item it)
        {
            int tipo = ModContent.ItemType<TrioDelEterno>();
            return it != null && !it.IsAir && it.damage > 0 && it.type != tipo &&
                it.DamageType == DamageClass.Magic;
        }

        public override string Texture => "AethonMod/Content/Weapons/Investigaciones/CetroDagasEterno";

        public override void SetStaticDefaults()
        {
            // EL NOMBRE del ítem en el tooltip (elDisplayName vive en el hjson).
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.damage = 0;                 // el daño es EL DE CADA ARMA
            Item.DamageType = DamageClass.Generic;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;             // el Trío no golpea por sí mismo
            Item.noUseGraphic = true;        // la vaina no se dibuja girando
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.LightPurple;
            // v6.162 — EL GATILLO DEL DISPARO (la autopsia del «no
            // dispara»): con shoot = None el hook Shoot JAMÁS corría
            // (el contrato de Terraria: Shoot solo vive con shoot > 0).
            // EL PORTAL: este proyectil invisible de 1 tick es SOLO la
            // puerta — al dispararlo, TrioDelEterno.Shoot corre y las
            // ARMAS DE LAS RANURAS hacen el disparo de verdad.
            Item.shoot = ModContent.ProjectileType
                <global::AethonMod.Content.Projectiles.Grimorio.PortalTrioProjectile>();
            Item.shootSpeed = 12f;
            Item.autoReuse = true;
            for (int i = 0; i < 3; i++)
                Ranuras[i] = new Item();
        }

        // === LA INSERCIÓN Y LA EXTRACCIÓN (viven SOLO en el altar del
        //     libro — v6.163: el arma no gestiona ranuras) ===

        public override bool AltFunctionUse(Player player) => false;

        public override bool CanRightClick() => false;

        // === EL ATAQUE: los proyectiles DE LAS ARMAS INSERTADAS ===

        /// <summary>0..2 = el arma de ESA ranura dispara sola · 3 =
        /// LAS TRES A LA VEZ (la marca del altar — por defecto ON).
        /// v6.163: la elección vive SOLO en el altar del libro.</summary>
        public int ModoDisparo = 3;

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // v6.163 — EL ARMA SOLO DISPARA: el selector murió aquí (la
            // letra: «quitar del arma la funcion de cambiar cual
            // disparar y dejamos esa funcion al altar»).

            // v6.162 — LA CURA DEL «NO DISPARA» (la letra: «cuando uso
            // el arma no funciona»): el Trío vivía con Item.shoot =
            // None — y EL CONTRATO DE TERRARIA es que Shoot SOLO corre
            // si Item.shoot > 0: las ranuras llenas y el clic muerto.
            // AHORA el disparador es PortalTrioProjectile (invisible,
            // 1 tick) y ESTE código corre de verdad. El portal que lo
            // trajo muere aquí: solo era la puerta.
            if (type == ModContent.ProjectileType
                    <global::AethonMod.Content.Projectiles.Grimorio.PortalTrioProjectile>())
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p != null && p.active && p.owner == player.whoAmI &&
                        p.type == type) p.Kill();
                }

            // v6.160 — LAS TRES A LA VEZ (la letra: «es al mismo tiempo,
            // las 3 armas se combinan y atacan a la vez») · v6.162 — Y
            // LA ELECCIÓN: ModoDisparo elige LA ranura (0..2) o las
            // TRES (3 — la marca del altar, por defecto).
            bool alguna = false;
            for (int idx = 0; idx < 3; idx++)
            {
                if (ModoDisparo < 3 && idx != ModoDisparo) continue;   // SOLO la elegida
                Item armaTurno = Ranuras[idx];
                if (armaTurno == null || armaTurno.IsAir || armaTurno.shoot <= 0) continue;
                alguna = true;

                // EL PROYECTIL DE ESA ARMA, con SUS números: el daño se
                // recalcula por el pipeline de vanilla del JUGADOR con ESA
                // arma (GetWeaponDamage respeta SU clase de daño y los
                // buffs del portador), el knockback igual, y la velocidad
                // de salida es la SUYA.
                int dano = player.GetWeaponDamage(armaTurno);
                float kb = player.GetWeaponKnockback(armaTurno, armaTurno.knockBack);
                float vel = MathHelper.Clamp(armaTurno.shootSpeed, 4f, 24f);
                Vector2 mira = Vector2.Normalize(Main.MouseWorld - player.MountedCenter);
                if (mira.HasNaNs()) mira = new Vector2(player.direction, 0f);

                // LA COMBINACIÓN: cada arma dispara con UNA PEQUEÑA CUÑA
                // de ángulo (−8° · 0° · +8°) — el abanico del trío que
                // nace del mismo punto: tres voces, una frase.
                float cuña = (idx - 1) * 0.14f;
                Vector2 velReal = mira.RotatedBy(cuña) * vel;

                var srcTurno = new Terraria.DataStructures.EntitySource_ItemUse(player, armaTurno);
                Projectile.NewProjectile(srcTurno, player.MountedCenter + mira * 24f,
                    velReal, armaTurno.shoot, dano, kb, player.whoAmI);
                // EL MANÁ DEL ARMA: cada disparo cobra SU coste de mana
                // (la letra: SOLO armas mágicas — y las mágicas cobran
                // maná; sin él, el trío sería mana gratis).
                if (armaTurno.mana > 0 && Main.netMode != NetmodeID.Server)
                {
                    int costo = player.statManaMax2 >= armaTurno.mana
                        ? armaTurno.mana : armaTurno.mana;
                    player.statMana -= costo;
                    if (player.statMana < 0) player.statMana = 0;
                    player.manaRegenDelay = (int)(180f * player.manaRegenDelayBonus);
                }
            }
            if (!alguna && Main.myPlayer == player.whoAmI)
            {
                // SIN ARMAS INSERTADAS: el atril está vacío — la voz del
                // libro lo dice (no spamea).
                Main.NewText(Language.GetTextValue("Mods.AethonMod.Items.TrioVacio"),
                    new Color(214, 152, 70));
            }
            return false;   // nada de vanilla: el disparo fue nuestro
        }

        // === EL CLIC DERECHO MURIÓ EN EL ARMA (v6.163 — la letra: «es
        //     mejor quitar del arma la funcion de cambiar cual disparar
        //     y dejamos esa funcion al altar»): la ELECCIÓN del arma
        //     vive SOLO en el altar del libro (clic en la ranura =
        //     elegirla · casilla «3» = las tres a la vez). El clic
        //     derecho del arma YA NO HACE NADA — el arma SOLO dispara. ===

        public override bool CanUseItem(Player player) => true;

        // (v6.162 — EL VIEJO CLIC DERECHO DE INSERTAR/EXTRAER MURIÓ: la
        //  letra mandó que el clic derecho SEA EL SELECTOR de arma y la
        //  extracción viva SOLO en el altar — ManejarInsercion fue
        //  jubilado.)

        // === EL TOOLTIP: las tres ranuras a la vista ===

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int ocupadas = 0;
            for (int i = 0; i < 3; i++)
                if (Ranuras[i] != null && !Ranuras[i].IsAir) ocupadas++;

            tooltips.Add(new TooltipLine(Mod, "TrioComo",
                Language.GetTextValue("Mods.AethonMod.Items.TrioComo")));
            for (int i = 0; i < 3; i++)
            {
                string contenido = (Ranuras[i] != null && !Ranuras[i].IsAir)
                    ? Ranuras[i].Name
                    : Language.GetTextValue("Mods.AethonMod.Items.TrioLibre");
                var linea = new TooltipLine(Mod, "TrioR" + i,
                    Language.GetTextValue("Mods.AethonMod.Items.TrioRanura", i + 1, contenido))
                { OverrideColor = (Ranuras[i] != null && !Ranuras[i].IsAir)
                    ? new Color(196, 110, 228) : new Color(168, 140, 104) };
                tooltips.Add(linea);
            }
            // v6.162 — EL MODO A LA VISTA · v6.163: la elección vive
            // en el ALTAR del libro (el arma solo dispara).
            tooltips.Add(new TooltipLine(Mod, "TrioModo",
                Language.GetTextValue(ModoDisparo >= 3
                    ? "Mods.AethonMod.Items.TrioModoTresTip"
                    : "Mods.AethonMod.Items.TrioModoUnaTip", ModoDisparo + 1))
            { OverrideColor = new Color(200, 160, 255) });
        }

        // === LA PERSISTENCIA: las tres armas viajan con el ítem ===

        public override void SaveData(TagCompound tag)
        {
            for (int i = 0; i < 3; i++)
                if (Ranuras[i] != null && !Ranuras[i].IsAir)
                    tag["trio" + i] = ItemIO.Save(Ranuras[i]);
            tag["trioModo"] = ModoDisparo;   // v6.162: el modo elegido viaja
        }

        public override void LoadData(TagCompound tag)
        {
            for (int i = 0; i < 3; i++)
            {
                Ranuras[i] = tag.ContainsKey("trio" + i)
                    ? ItemIO.Load(tag.Get<TagCompound>("trio" + i))
                    : new Item();
                if (Ranuras[i] == null) Ranuras[i] = new Item();
            }
            ModoDisparo = tag.ContainsKey("trioModo")
                ? System.Math.Clamp(tag.GetInt("trioModo"), 0, 3) : 3;
        }

        public override void NetSend(System.IO.BinaryWriter writer)
        {
            for (int i = 0; i < 3; i++)
            {
                writer.Write(Ranuras[i] != null && !Ranuras[i].IsAir);
                if (Ranuras[i] != null && !Ranuras[i].IsAir)
                    ItemIO.Send(Ranuras[i], writer, true);
            }
            writer.Write((byte)ModoDisparo);   // v6.162: el modo viaja por la red
        }

        public override void NetReceive(System.IO.BinaryReader reader)
        {
            for (int i = 0; i < 3; i++)
            {
                bool viva = reader.ReadBoolean();
                Ranuras[i] = viva ? ItemIO.Receive(reader, true, false) : new Item();
                if (Ranuras[i] == null) Ranuras[i] = new Item();
            }
            ModoDisparo = System.Math.Clamp((int)reader.ReadByte(), 0, 3);
        }

        public override void AddRecipes()
        {
            // LA VÍA DE LA CASA: receta de prueba (5 madera) + nada más —
            // es un arma de investigación del kit.
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
