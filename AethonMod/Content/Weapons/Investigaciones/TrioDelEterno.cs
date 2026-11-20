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
            Item.shoot = ProjectileID.None;  // el Trío NO dispara nada propio
            Item.shootSpeed = 0f;
            Item.autoReuse = true;
            for (int i = 0; i < 3; i++)
                Ranuras[i] = new Item();
        }

        // === LA INSERCIÓN Y LA EXTRACCIÓN (clic derecho) ===

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanRightClick() => false;

        public override bool? UseItem(Player player)
        {
            // EL CLIC DERECHO fuera de Shoot: insertar/extraer NO gasta
            // animación — se maneja aquí (Shoot solo corre en el izq).
            if (player.altFunctionUse == 2) return true;
            return base.UseItem(player);
        }

        // === EL ATAQUE: los proyectiles DE LAS ARMAS INSERTADAS ===

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // EL CLIC DERECHO NO dispara (es insertar/extraer).
            if (player.altFunctionUse == 2) return false;

            // LA RANURA DEL TURNO: la primera ARMADA desde el turno
            // actual (si una ranura está libre, se salta).
            int primera = -1;
            for (int k = 0; k < 3; k++)
            {
                int idx = (Turno + k) % 3;
                Item arma = Ranuras[idx];
                if (arma == null || arma.IsAir || arma.shoot <= 0) continue;
                primera = idx;
                break;
            }
            if (primera < 0)
            {
                // SIN ARMAS INSERTADAS: el atril está vacío — la voz del
                // libro lo dice una vez (no spamea).
                if (Main.myPlayer == player.whoAmI)
                    Main.NewText(Language.GetTextValue("Mods.AethonMod.Items.TrioVacio"),
                        new Color(255, 170, 130));
                return false;
            }

            Item armaTurno = Ranuras[primera];

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
            Vector2 velReal = mira * vel;

            // LA VOZ DE ESA ARMA: disparamos su proyectil con SU tipo,
            // SU sonido y SU uso (el patrón de la casa: NewProjectile
            // directo — el source lleva EL ARMA DEL TURNO).
            var srcTurno = new Terraria.DataStructures.EntitySource_ItemUse(player, armaTurno);
            Projectile.NewProjectile(srcTurno, player.MountedCenter + mira * 24f,
                velReal, armaTurno.shoot, dano, kb, player.whoAmI);

            // EL TURNO AVANZA a la SIGUIENTE ranura con arma (la del
            // disparo cede su sitio — el relevo del trío).
            Turno = (primera + 1) % 3;
            return false;   // nada de vanilla: el disparo fue nuestro
        }

        // === LA INSERCIÓN / EXTRACCIÓN vive en el clic derecho del inventario ===

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                // EL GESTO DE LA CASA: insertar/extraer con clic derecho
                // SOLO cuando el ítem está EN LA MANO (seleccionado).
                ManejarInsercion(player);
                return false;   // no gasta animación
            }
            return true;
        }

        /// <summary>EL CLIC DERECHO DEL ATRIL: si el cursor trae UN ARMA,
        /// se INSERTA en la primera ranura libre; si viene VACÍO, la
        /// última arma insertada SALE de vuelta al cursor.</summary>
        private void ManejarInsercion(Player player)
        {
            if (Main.myPlayer != player.whoAmI) return;
            Item mouse = Main.mouseItem;
            if (mouse != null && !mouse.IsAir)
            {
                // ¿ES UN ARMA? (daño > 0 y NO es el propio Trío — el
                // atril no se sostiene a sí mismo).
                int trio = ModContent.ItemType<TrioDelEterno>();
                if (mouse.damage <= 0 || mouse.type == trio)
                {
                    Main.NewText(Language.GetTextValue("Mods.AethonMod.Items.TrioNoArma"),
                        new Color(255, 170, 130));
                    return;
                }
                for (int i = 0; i < 3; i++)
                {
                    if (Ranuras[i] == null || Ranuras[i].IsAir)
                    {
                        Ranuras[i] = mouse.Clone();
                        mouse.TurnToAir();
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item85,
                            player.Center);   // el chasquido del atril
                        Main.NewText(Language.GetTextValue(
                            "Mods.AethonMod.Items.TrioInsertada", i + 1),
                            new Color(160, 230, 255));
                        return;
                    }
                }
                Main.NewText(Language.GetTextValue("Mods.AethonMod.Items.TrioLleno"),
                    new Color(255, 170, 130));
            }
            else
            {
                // EXTRACCIÓN: la ÚLTIMA arma insertada vuelve al cursor.
                for (int i = 2; i >= 0; i--)
                {
                    if (Ranuras[i] != null && !Ranuras[i].IsAir)
                    {
                        Main.mouseItem = Ranuras[i].Clone();
                        Ranuras[i] = new Item();
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item81,
                            player.Center);
                        return;
                    }
                }
            }
        }

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
                    ? new Color(160, 230, 255) : new Color(140, 140, 150) };
                tooltips.Add(linea);
            }
        }

        // === LA PERSISTENCIA: las tres armas viajan con el ítem ===

        public override void SaveData(TagCompound tag)
        {
            for (int i = 0; i < 3; i++)
                if (Ranuras[i] != null && !Ranuras[i].IsAir)
                    tag["trio" + i] = ItemIO.Save(Ranuras[i]);
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
        }

        public override void NetSend(System.IO.BinaryWriter writer)
        {
            for (int i = 0; i < 3; i++)
            {
                writer.Write(Ranuras[i] != null && !Ranuras[i].IsAir);
                if (Ranuras[i] != null && !Ranuras[i].IsAir)
                    ItemIO.Send(Ranuras[i], writer, true);
            }
        }

        public override void NetReceive(System.IO.BinaryReader reader)
        {
            for (int i = 0; i < 3; i++)
            {
                bool viva = reader.ReadBoolean();
                Ranuras[i] = viva ? ItemIO.Receive(reader, true, false) : new Item();
                if (Ranuras[i] == null) Ranuras[i] = new Item();
            }
        }

        public override void AddRecipes()
        {
            // LA VÍA DE LA CASA: receta de prueba (5 madera) + nada más —
            // es un arma de investigación del kit.
            CreateRecipe().AddIngredient(ItemID.Wood, 5).Register();
        }
    }
}
